using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interface;

public interface IFork
{
    int Id { get; set; }
    Task<bool> TryTake(IPhilosopher philosopher);
    Task<bool> TryLock(IPhilosopher philosopher);
    Task UnlockFork(IPhilosopher philosopher);
    Task Put(IPhilosopher philosopher);
}
