using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataContracts;

public class PhilosopherWithForksIds
{
    public int PhilosopherId { get; set; }
    public int LeftForkId { get; set; }
    public int RightForkId { get; set; }
}
