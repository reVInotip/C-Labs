using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;

namespace Interface;

public interface IRetranslationService
{
    Task<bool> RetranslateCommand(ForkCommandsDto Command, int PhilosopherId, int ForkId);
    Task<PhilosopherWithForksIds?> RetranslateRegistration(string name, string uri);
    Task<PhilosopherInfo?> RetranslateGetPhilosopherInfo(string uri);
    Task<PhilosopherInfo?> RetranslateGetPhilosopherStats(double simulationTime, string originUri);
    Task RetranslateStopApplication(string originUri);
}
