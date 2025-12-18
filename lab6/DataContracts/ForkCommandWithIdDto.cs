using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataContracts;

public class ForkCommandWithIdDto
{
    public ForkCommandsDto ForkCommands { get; set; }
    public int PhilosopherId { get; set; }
    public int ForkId { get; set; }
}
