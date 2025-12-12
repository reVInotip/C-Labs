using System;
using System.Diagnostics;
using System.Threading;
using Interface;

namespace Services.Utils;

public class Fork : IFork
{
    private bool _isTaken;
    private bool _isLocked;
    private readonly Lock _lockObject = new();
    private Stopwatch _stopwatch = new ();
    
    public int Id { get; set; }
    public IPhilosopher? Owner { get; private set; }
    private IPhilosopher? _locker;
    public long UsedTime { get; private set; }
    public long AvailableTime { get; private set; }
    public long BlockTime { get; private set; }

    public Fork()
    {
        UsedTime = 0; //ms
        BlockTime = 0; //ms
        AvailableTime = 0; //ms

        _stopwatch.Start();
    }

    public void TryTake(IPhilosopher philosopher)
    {
        lock (_lockObject)
        {
            if (_isTaken || (_isLocked && (_locker != philosopher)))
                return;

            _stopwatch.Stop();
            BlockTime += _stopwatch.ElapsedMilliseconds;

            _isTaken = true;
            Owner = philosopher;

            _stopwatch = Stopwatch.StartNew();
            return;
        }
    }

    public void TryLock(IPhilosopher philosopher)
    {
        lock (_lockObject)
        {
            if (_isTaken || _isLocked)
                return;

            _stopwatch.Stop();
            AvailableTime += _stopwatch.ElapsedMilliseconds;

            _isLocked = true;
            _locker = philosopher;

            _stopwatch = Stopwatch.StartNew();
            return;
        }
    }

    public bool IsLockedBy(IPhilosopher philosopher)
    {
        lock (_lockObject)
        {
            return _isLocked && _locker == philosopher;
        }
    }

    public bool IsTakenBy(IPhilosopher philosopher)
    {
        lock (_lockObject)
        {
            return _isTaken && Owner == philosopher;
        }
    }

    public void UnlockFork()
    {
        lock (_lockObject)
        {
            _isLocked = false;
            _locker = null;
        }
    }

    public void Put()
    {
        lock (_lockObject)
        {
            _stopwatch.Stop();
            UsedTime += _stopwatch.ElapsedMilliseconds;

            _isTaken = false;
            Owner = null;

            _isLocked = false;
            _locker = null;

            _stopwatch = Stopwatch.StartNew();
        }
    }

    public string GetInfoString()
    {
        lock (_lockObject)
        {
            string status = _isTaken ? $"In Use (by {Owner?.Name})" : "Available";
            return String.Format($"Fork-{Id}: {status}");
        }
    }

    public string GetScoreString(double simulationTime)
    {
        lock (_lockObject)
        {
            double usedPercent = simulationTime > 0 ? (UsedTime / simulationTime) * 100 : 0;
            double availablePercent = simulationTime > 0 ? (AvailableTime / simulationTime) * 100 : 0;
            double blockPercent = simulationTime > 0 ? (BlockTime / simulationTime) * 100 : 0;
            return String.Format($"Fork-{Id}: used {usedPercent:F1}%, available {availablePercent:F1}%, blocked {blockPercent:F1}%");
        }
    }
}