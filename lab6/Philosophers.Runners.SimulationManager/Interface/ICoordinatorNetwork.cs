using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface;

public interface ICoordinatorNetwork
{
    Task<PhilosopherInfo?> GetInfo(string originUri);
    Task<PhilosopherInfo?> GetStats(string originUri, double simulationTime);
    Task Stop(string originUri);
    Task Stop();
}
