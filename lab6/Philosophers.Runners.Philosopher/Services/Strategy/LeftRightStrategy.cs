using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Interface;
using Interface.Strategy;
using DataContracts;

namespace Services.Strategy;

public class LeftRightStrategy : ILeftRightStrategy
{
    private bool _isLeftHanded;

    public LeftRightStrategy(IOptions<ServicesConfiguration> options)
    {
        _isLeftHanded = options.Value.IsLeftHanded;
    }

    public async Task<ForkType> TakeFork(IPhilosopher philosopher)
    {   
        if (_isLeftHanded && await philosopher.LeftFork.TryTake(philosopher))
        {
            return ForkType.Left;
        }

        if (await philosopher.RightFork.TryTake(philosopher))
        {
            return ForkType.Right;
        }

        return ForkType.None;
    }

    public async Task<bool> TakeRightFork(IPhilosopher philosopher)
    {
        return await philosopher.RightFork.TryTake(philosopher);
    }

    public async Task<bool> TakeLeftFork(IPhilosopher philosopher)
    {
        return await philosopher.LeftFork.TryTake(philosopher);
    }

    public async Task<bool> LockFork(IPhilosopher philosopher)
    {
        if (_isLeftHanded)
        {
            return await philosopher.LeftFork.TryLock(philosopher);
        }

        return await philosopher.RightFork.TryLock(philosopher);
    }

    public async Task<bool> LockRightFork(IPhilosopher philosopher)
    {
        return await philosopher.RightFork.TryLock(philosopher);
    }

    public async Task<bool> LockLeftFork(IPhilosopher philosopher)
    {
        return await philosopher.LeftFork.TryLock(philosopher);
    }

    public async Task UnlockForks(IPhilosopher philosopher)
    {
        await philosopher.LeftFork.UnlockFork(philosopher);
        await philosopher.RightFork.UnlockFork(philosopher);
    }

    public async Task PutForks(IPhilosopher philosopher)
    {
        await philosopher.LeftFork.Put(philosopher);
        await philosopher.RightFork.Put(philosopher);
    }
}
