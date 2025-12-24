using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;

namespace Services;

public class Philosopher : IPhilosopher
{
    public int Id { get; set; }
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
    public int HavingForkId { get; set; }
    public int TryingToTakeForkId { get; set; }
}
