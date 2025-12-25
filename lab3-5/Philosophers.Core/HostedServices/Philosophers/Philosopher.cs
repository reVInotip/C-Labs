using Interface;
using Interface.Channel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Core.Utils;
using Philosophers.Services.Channels.Items;
using Philosophers.Services.Utils;
using Philosophers.Services.Channels.Events;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Philosophers.Tests")]

namespace Philosophers.Core.HostedServices.Philosophers;


public abstract class PhilosopherService : BackgroundService, IPhilosopher
{
    private readonly IStrategy _philosopherStrategy;
    private readonly ILogger<PhilosopherService> _logger;
    private readonly IChannel<PhilosopherToAnalyzerChannelItem> _channelToAnalyzer;
    private readonly IChannel<PhilosopherToPrinterChannelItem> _channelToPrinter;

    private PhilosopherStates _state;
    private readonly int _eatingTime;
    private readonly int _takeForkTime;
    private readonly int _thinkingTime;
    private int _stateTimer;
    private readonly Lock _lockObject = new ();

    public string Name { get; set; }
    public int CountEatingFood { get; private set; }
    public int HungryTime { get; private set; }

    public IFork LeftFork { get; private set; }
    public IFork RightFork { get; private set; }

    public PhilosopherService(
        ILogger<PhilosopherService> logger,
        IStrategy philosopherStrategy,
        IOptions<PhilosopherConfiguration> options,
        IForksFactory forksFactory,
        IChannel<PhilosopherToAnalyzerChannelItem> channelToAnalyzer,
        IChannel<PhilosopherToPrinterChannelItem> channelToPrinter)
    {
        _logger = logger;
        _philosopherStrategy = philosopherStrategy;

        _channelToAnalyzer = channelToAnalyzer;
        _channelToAnalyzer.SendMeItem += SendInfoToAnalyzer;

        _channelToPrinter = channelToPrinter;
        _channelToPrinter.SendMeItem += SendInfoToPrinter;
        _channelToPrinter.SendMeItemBy += SendFinalStatsToAnalyzer;

        var random = new Random();

        Name = "undefined";
        _eatingTime = random.Next(options.Value.EatingTimeMin, options.Value.EatingTimeMax);
        _takeForkTime = random.Next(options.Value.TakeForkTimeMin, options.Value.TakeForkTimeMax);
        _thinkingTime = random.Next(options.Value.ThinkingTimeMin, options.Value.ThinkingTimeMax);

        var forks = forksFactory.Create();
        LeftFork = forks.Item1;
        RightFork = forks.Item2;
    }

    private async void SendInfoToPrinter(object? sender, EventArgs e)
    {
        var item = new PhilosopherToPrinterChannelItem(
            GetInfoString(),
            LeftFork.GetInfoString(),
            RightFork.GetInfoString()            
        );
        await _channelToPrinter.Writer.WriteAsync(item);
    }

    private async void SendFinalStatsToAnalyzer(object? sender, IChannelEventArgs e)
    {
        double simulationTime = ((ChannelScoresEvent)e).SimulationTime;
        var item = new PhilosopherToPrinterChannelItem(
            GetScoreString(simulationTime),
            LeftFork.GetScoreString(simulationTime),
            RightFork.GetScoreString(simulationTime)
        );
        await _channelToPrinter.Writer.WriteAsync(item);
    }

    private async void SendInfoToAnalyzer(object? sender, EventArgs e)
    {
        PhilosopherToAnalyzerChannelItem item;
        lock (_lockObject)
        {
            item = new PhilosopherToAnalyzerChannelItem(
                _state == PhilosopherStates.Eating,
                LeftFork.Owner == null,
                RightFork.Owner == null
            );
        }

        await _channelToAnalyzer.Writer.WriteAsync(item);
    }

    public string GetInfoString()
    {
        string stateInfo;
        lock (_lockObject)
        {
            stateInfo = _state switch
            {
                PhilosopherStates.Thinking => $"Thinking ({_stateTimer} ms)",
                PhilosopherStates.Hungry => $"Hungry ({_stateTimer} ms)",
                PhilosopherStates.Eating => $"Eating ({_stateTimer} ms)",
                PhilosopherStates.TakeLeftFork => $"Taking Left Fork ({_stateTimer} ms)",
                PhilosopherStates.TakeRightFork => $"Taking Right Fork ({_stateTimer} ms)",
                _ => _state.ToString()
            };
        }

        return String.Format($"{Name}: {stateInfo}, meals: {CountEatingFood}");
    }

    public string GetScoreString(double simulationTime)
    {
        string result;
        lock (_lockObject)
        {
            double throughput = simulationTime > 0 ? CountEatingFood / simulationTime : 0;
            double hungryPercentage = simulationTime > 0 ? (HungryTime / simulationTime) * 100 : 0;
            result = String.Format($"{Name}: throughput {throughput:F4} meals/ms, " +
                            $"hungry {HungryTime} ms ({hungryPercentage:F1}%)");
        }

        return result;
    }

    public bool IsEating()
    {
        lock (_lockObject)
        {
            return _state == PhilosopherStates.Eating;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channelToAnalyzer.RegisterPublisher(this);
            _channelToPrinter.RegisterPublisher(this);
            await Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await ProcessState();
                    }
                },  stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Philosophers service shutdown!");
        }
        catch (Exception)
        {
            throw new ApplicationException("Operation cancelled not normally");
        }
        finally
        {
            _philosopherStrategy.PutForks(this);
        }

        return;
    }

    private async Task ProcessState()
    {

        switch (_state)
        {
            case PhilosopherStates.Thinking:
                await ProcessThinkingState();
                break;
            case PhilosopherStates.Hungry:
                await ProcessHungryState();
                break;
            case PhilosopherStates.TakeLeftFork:
                await ProcessTakingLeftForkState();
                break;
            case PhilosopherStates.TakeRightFork:
                await ProcessTakingRightForkState();
                break;
            case PhilosopherStates.Eating:
                await ProcessEatingState();
                break;
        }
    }

    private async Task ProcessThinkingState()
    {
        while (_stateTimer < _thinkingTime)
        {
            //Console.WriteLine("Th {0}, {1}", _stateTimer, _eatingTime);
            await Task.Delay(_thinkingTime / 8);
            Interlocked.Add(ref _stateTimer, _thinkingTime / 8);
        }

        lock (_lockObject)
        {
            _stateTimer = 0;
            _state = PhilosopherStates.Hungry;
        }
    }

    private async Task ProcessHungryState()
    {
        _philosopherStrategy.LockFork(this);

        if (_philosopherStrategy.IsForkLocked(this))
        {
            await Task.Delay(_takeForkTime);

            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            _philosopherStrategy.TakeFork(this);

            if (_philosopherStrategy.HasLeftFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.TakeLeftFork;
                    _stateTimer = 0;
                }
            }
            else if (_philosopherStrategy.HasRightFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.TakeRightFork;
                    _stateTimer = 0;
                }
            }

            _philosopherStrategy.UnlockForks(this);
        }
    }

    private async Task ProcessTakingLeftForkState()
    {
        _philosopherStrategy.LockRightFork(this);

        if (_philosopherStrategy.IsForkLocked(this))
        {
            await Task.Delay(_takeForkTime);
            
            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            _philosopherStrategy.TakeRightFork(this);

            if (_philosopherStrategy.HasRightFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.Eating;
                    _stateTimer = 0;
                }
            }
            _philosopherStrategy.UnlockForks(this);
        }
    }

    private async Task ProcessTakingRightForkState()
    {
        _philosopherStrategy.LockLeftFork(this);

        if (_philosopherStrategy.IsForkLocked(this))
        {
            await Task.Delay(_takeForkTime);

            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            _philosopherStrategy.TakeLeftFork(this);

            if (_philosopherStrategy.HasLeftFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.Eating;
                    _stateTimer = 0;
                }
            }

            _philosopherStrategy.UnlockForks(this);
        }
    }

    private async Task ProcessEatingState()
    {
        while (_stateTimer < _eatingTime)
        {
            await Task.Delay(_eatingTime / 8);
            Interlocked.Add(ref _stateTimer, _eatingTime / 8);
        }

        _philosopherStrategy.PutForks(this);

        lock (_lockObject)
        {
            CountEatingFood++;

            _state = PhilosopherStates.Thinking;
            _stateTimer = 0;
        }
    }
}
