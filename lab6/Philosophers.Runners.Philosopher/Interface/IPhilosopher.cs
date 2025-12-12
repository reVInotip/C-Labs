using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interface;

public interface IPhilosopher
{
    int Id { get; }
    IFork LeftFork { get; }
    IFork RightFork { get; }
    int CountEatingFood { get; }
    int HungryTime { get; }
    string Name { get; protected internal set; }
}
