using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterfaceContracts.Channel;

namespace Services.Channels.Items;

public record CommandAnswerChannelItem(bool Ok) : IChannelItem;
