using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterfaceContracts.Channel;

namespace Services.Channels.Items;

public record PhilosopherWithForksIdsChannelItem(int PhilosopherId, int LeftForkId, int RightForkId) : IChannelItem;
