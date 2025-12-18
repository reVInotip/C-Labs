using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterfaceContracts.Channel;

namespace Services.Channels.Events;

public record ChannelRegistrationEvent(string Name, string Uri) : IChannelEventArgs;
