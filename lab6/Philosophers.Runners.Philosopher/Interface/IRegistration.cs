using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface;

public interface IRegistration
{
    Task<PhilosopherWithForksIds?> Registration(string name);
}
