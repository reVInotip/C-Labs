using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using Microsoft.Extensions.Logging;

namespace Services;

public class DeadlockAnalyzer(
    ILogger<DeadlockAnalyzer> logger
) : IDeadlockAnalyzer
{
    private readonly ILogger<DeadlockAnalyzer> _logger = logger;

    public bool CheckDeadlock(
        Dictionary<int, IPhilosopher> eating,
        Dictionary<int, IPhilosopher> working,
        Dictionary<int, IPhilosopher> thinking
    )
    {
        if (eating.Count != 0 || thinking.Count != 0)
        {
            return false;
        }

        bool[] forks = new bool[eating.Count + thinking.Count + working.Count];

        for (int i = 0; i < forks.Count(); ++i)
        {
            forks[i] = false;
        }

        for (int i = 0; i < working.Count; ++i)
        {
            if (working[i].HavingForkId > 0)
                forks[working[i].HavingForkId] = true;

            if (working[i].TryingToTakeForkId > 0)
                forks[working[i].TryingToTakeForkId] = true;
        }

        foreach (var f in forks)
        {
            if (!f)
                return false;
        }

        _logger.LogError("DEADLOCK DETECTED!!!");
        return true;
    }
}
