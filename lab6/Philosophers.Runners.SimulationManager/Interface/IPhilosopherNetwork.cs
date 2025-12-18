using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface;

public interface IPhilosopherNetwork
{
    Task<PhilosopherInfo?> GetInfo(string uri);
    Task<PhilosopherInfo?> GetStats(string uri, double simulationTime);
    Task<PhilosopherAction?> GetAction(string uri);
    Task Stop(string uri);
}
