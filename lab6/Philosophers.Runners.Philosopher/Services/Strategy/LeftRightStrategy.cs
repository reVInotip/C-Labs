using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Interface;
using Interface.Strategy;

namespace Services.Strategy;

public class LeftRightStrategy(
    IOptions<ServicesConfiguration> options
) : ILeftRightStrategy
{
    private readonly bool _isLeftHanded = options.Value.IsLeftHanded;

    public bool IsLeftHanded()
    {
        return _isLeftHanded;
    }

    public async Task TakeFork(IPhilosopher philosopher, CancellationToken token)
    {   
        if (_isLeftHanded)
            await philosopher.LeftFork.Take(philosopher, token);
        else
            await philosopher.RightFork.Take(philosopher, token);
    }

    public async Task TakeRightFork(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.RightFork.Take(philosopher, token);
    }

    public async Task TakeLeftFork(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.LeftFork.Take(philosopher, token);
    }

    public async Task LockFork(IPhilosopher philosopher, CancellationToken token)
    {
        if (_isLeftHanded)
            await philosopher.LeftFork.Lock(philosopher, token);
        else
            await philosopher.RightFork.Lock(philosopher, token);
    }

    public async Task LockRightFork(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.RightFork.Lock(philosopher, token);
    }

    public async Task LockLeftFork(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.LeftFork.Lock(philosopher, token);
    }

    public async Task UnlockForks(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.LeftFork.UnlockFork(philosopher, token);
        await philosopher.RightFork.UnlockFork(philosopher, token);
    }

    public async Task PutForks(IPhilosopher philosopher, CancellationToken token)
    {
        await philosopher.LeftFork.Put(philosopher, token);
        await philosopher.RightFork.Put(philosopher, token);
    }
}
