using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interface;

public interface IFork
{
    int Id { get; set; }
    Task Take(IPhilosopher philosopher, CancellationToken token);
    Task Lock(IPhilosopher philosopher, CancellationToken token);
    Task UnlockFork(IPhilosopher philosopher, CancellationToken token);
    Task Put(IPhilosopher philosopher, CancellationToken token);
}
