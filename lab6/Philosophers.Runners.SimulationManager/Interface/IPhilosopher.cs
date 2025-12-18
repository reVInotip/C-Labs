using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Interface;

public interface IPhilosopher
{
    int Id { get; set; }
    string Name { get; set; }
    IFork LeftFork { get; set; }
    IFork RightFork { get; set; }
    string Uri { get; set; }
}
