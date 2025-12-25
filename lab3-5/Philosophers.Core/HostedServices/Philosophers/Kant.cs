using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using Interface.Channel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Philosophers.Services.Utils;
using Philosophers.Services.Channels.Items;

namespace Philosophers.Core.HostedServices.Philosophers;

public class Kant : PhilosopherService
{
    public Kant(
        ILogger<PhilosopherService> logger,
        IStrategy philosopherStrategy,
        IOptions<PhilosopherConfiguration> options,
        IForksFactory forksFactory,
        IChannel<PhilosopherToAnalyzerChannelItem> channelToAnalyzer,
        IChannel<PhilosopherToPrinterChannelItem> channelToPrinter)
    : base(logger, philosopherStrategy, options, forksFactory, channelToAnalyzer, channelToPrinter)
    {
        Name = "Кант";
    }
}
