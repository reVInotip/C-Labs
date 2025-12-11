using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface.Strategy;

public interface IStrategy
{
    Task<ForkType> TakeFork(IPhilosopher philosopher);
    Task<bool> LockFork(IPhilosopher philosopher);
    Task<bool> LockRightFork(IPhilosopher philosopher);
    Task<bool> LockLeftFork(IPhilosopher philosopher);
    Task<bool> TakeRightFork(IPhilosopher philosopher);
    Task<bool> TakeLeftFork(IPhilosopher philosopher);
    Task UnlockForks(IPhilosopher philosopher);
    Task PutForks(IPhilosopher philosopher);
}
