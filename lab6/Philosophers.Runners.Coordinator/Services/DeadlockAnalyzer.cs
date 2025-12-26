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

        int[] forks = new int[eating.Count + thinking.Count + working.Count];

        for (int i = 0; i < forks.Count(); ++i)
        {
            forks[i] = 0;
        }

        foreach (var item in working)
        {
            var philosopher = item.Value;
            if (philosopher.HavingForkId >= 0)
                ++forks[philosopher.HavingForkId];
            
            if (philosopher.TryingToTakeForkId >= 0)
                ++forks[philosopher.TryingToTakeForkId];
        }

        foreach (var item in eating)
        {
            var philosopher = item.Value;
            ++forks[philosopher.LeftForkId];
            ++forks[philosopher.RightForkId];
        }

        foreach (var f in forks)
        {
            if (f == 0)
                return false;
        }

        _logger.LogError("DEADLOCK DETECTED!!!");
        return true;
    }
}
