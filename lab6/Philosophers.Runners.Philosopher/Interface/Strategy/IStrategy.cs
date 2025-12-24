using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface.Strategy;

public interface IStrategy
{
    Task TakeFork(IPhilosopher philosopher, CancellationToken token);
    Task LockFork(IPhilosopher philosopher, CancellationToken token);
    Task LockRightFork(IPhilosopher philosopher, CancellationToken token);
    Task LockLeftFork(IPhilosopher philosopher, CancellationToken token);
    Task TakeRightFork(IPhilosopher philosopher, CancellationToken token);
    Task TakeLeftFork(IPhilosopher philosopher, CancellationToken token);
    Task UnlockForks(IPhilosopher philosopher, CancellationToken token);
    Task PutForks(IPhilosopher philosopher, CancellationToken token);
    bool IsLeftHanded();
}
