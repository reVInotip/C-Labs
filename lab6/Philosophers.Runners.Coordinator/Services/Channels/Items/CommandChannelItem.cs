using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;
using InterfaceContracts.Channel;

namespace Services.Channels.Items;

public record CommandChannelItem(ForkCommandsDto Command, int PhilosopherId, int ForkId) : IChannelItem;
