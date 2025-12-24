using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;
using InterfaceContracts.Channel;
using MassTransit;
using Services.Channels;
using Services.Channels.Items;
namespace Services.Consumers;

public class CommandsConsumer(
    IChannel<CommandChannelItem> commandsChannel
) : IConsumer<ForkCommandWithIdDto>
{
    private readonly IChannel<CommandChannelItem> _commandsChannel = commandsChannel;

    public async Task Consume(ConsumeContext<ForkCommandWithIdDto> context)
    {
        await _commandsChannel.Writer.WriteAsync(
            new CommandChannelItem(
                context.Message.ForkCommands,
                context.Message.PhilosopherId,
                context.Message.ForkId
            ));
    }
}
