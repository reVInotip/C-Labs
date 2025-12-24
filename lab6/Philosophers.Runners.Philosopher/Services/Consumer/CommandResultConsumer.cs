using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;
using Interface;
using InterfaceContracts.Channel;
using MassTransit;
using Microsoft.Extensions.Logging;
using Services.Channels.Items;

namespace Services.Consumer;

public class CommandResultConsumer(
    IChannel<CommandExecutionResultItem> commandsChannel
) : IConsumer<CommandExecutionResult>
{
    private readonly IChannel<CommandExecutionResultItem> _commandsChannel = commandsChannel;

    public async Task Consume(ConsumeContext<CommandExecutionResult> context)
    {
        await _commandsChannel.Writer.WriteAsync(new CommandExecutionResultItem(
            context.Message.PhilosopherId,
            context.Message.ok
        ));
    }
}
