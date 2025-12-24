using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Interface;

public interface IPhilosopher
{
    int Id { get; set; }
    int LeftForkId { get; set; }
    int RightForkId { get; set;}
    int HavingForkId { get; set; }
    int TryingToTakeForkId { get; set; }
}
