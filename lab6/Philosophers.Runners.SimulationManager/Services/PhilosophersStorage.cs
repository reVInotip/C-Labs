using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Interface;

namespace Services;

public class PhilosophersStorage : IEnumerable<IPhilosopher>
{
    private readonly List<IPhilosopher> _philosophers = [];
    private ReaderWriterLockSlim _listLock = new();

    public void Insert(int index, IPhilosopher item)
    {
        _listLock.EnterWriteLock();
        try
        {
            _philosophers.Insert(index, item);
        }
        finally
        {
            _listLock.ExitWriteLock();
        }
    }

    public IPhilosopher Get(int index)
    {
        _listLock.EnterReadLock();
        try
        {
            return _philosophers[index];
        }
        finally
        {
            _listLock.ExitReadLock();
        }
    }

    public IEnumerator<IPhilosopher> GetEnumerator()
    {
        List<IPhilosopher> snapshot;

        _listLock.EnterWriteLock();
        snapshot = new List<IPhilosopher>(_philosophers);
        _listLock.ExitWriteLock();

        foreach (var p in snapshot)
            yield return p;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
