using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interface;

public interface IDeadlockAnalyzer
{
    bool CheckDeadlock(
        Dictionary<int, IPhilosopher> eating,
        Dictionary<int, IPhilosopher> working,
        Dictionary<int, IPhilosopher> thinking
    );
}
