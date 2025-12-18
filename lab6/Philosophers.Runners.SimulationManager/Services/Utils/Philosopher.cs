using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;

namespace Services.Utils;

public class Philosopher : IPhilosopher
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required IFork LeftFork { get; set; }
    public required IFork RightFork { get; set; }
    public required string Uri { get; set; }
}