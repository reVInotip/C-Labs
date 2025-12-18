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
    private readonly Dictionary<int, IPhilosopher> _philosophers = [];
    private ReaderWriterLockSlim _listLock = new();

    public int Count => _philosophers.Count;

    public void Insert(int index, IPhilosopher item)
    {
        _listLock.EnterWriteLock();
        _philosophers.Add(index, item);
        _listLock.ExitWriteLock();
    }

    public IPhilosopher Get(int index)
    {
        _listLock.EnterReadLock();
        var item = _philosophers[index];
        _listLock.ExitReadLock();

        return item;
    }

    public IEnumerator<IPhilosopher> GetEnumerator()
    {
        List<IPhilosopher> snapshot;

        _listLock.EnterWriteLock();
        snapshot = new List<IPhilosopher>(_philosophers.Values);
        _listLock.ExitWriteLock();

        foreach (var p in snapshot)
            yield return p;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
