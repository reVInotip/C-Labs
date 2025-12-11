using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using InterfaceContracts.Channel;
using Services.Channels.Items;
using DataContracts;
using Services.Channels.Events;

namespace Core.Controllers;

[ApiController]
[Route("[controller]")]
public class SimulationManagerController(
    ILogger<SimulationManagerController> logger,
    IChannel<PhilosopherWithForksIdsChannelItem> channel,
    IChannel<CommandAnswerChannelItem> commandAnswerChannel,
    IChannel<ForkCommandWithIdChannelItem> commandChannel) : Controller
{
    private readonly ILogger<SimulationManagerController> _logger = logger;
    private readonly IChannel<PhilosopherWithForksIdsChannelItem> _channel = channel;
    private readonly IChannel<ForkCommandWithIdChannelItem> _commandChannel = commandChannel;
    private readonly IChannel<CommandAnswerChannelItem> _commandAnswerChannel = commandAnswerChannel;

    [HttpGet("register-me")]
    public async Task<PhilosopherWithForksIds> RegisterPhilosopher([FromForm] string name)
    {
        var fullUri = $"{Request.Scheme}://{Request.Host}";

        _channel.NotifyWith(this, new ChannelRegistrationEvent(name, fullUri));

        var item = await _channel.Reader.ReadAsync();
        return new PhilosopherWithForksIds
        {
            PhilosopherId = item.PhilosopherId,
            LeftForkId = item.LeftForkId,
            RightForkId = item.RightForkId
        };
    }

    [HttpPost("put-or-unlock-fork")]
    public async Task PutOrUnlockFork([FromBody] ForkCommandWithIdDto forkCommand)
    {
        var item = new ForkCommandWithIdChannelItem
        (
            forkCommand.ForkCommands,
            forkCommand.PhilosopherId,
            forkCommand.ForkId
        );

        await _commandChannel.Writer.WriteAsync(item);
    }

    [HttpPost("lock-or-take-fork")]
    public async Task<bool> LockOrTakeFork([FromBody] ForkCommandWithIdDto forkCommand)
    {
        var item = new ForkCommandWithIdChannelItem
        (
            forkCommand.ForkCommands,
            forkCommand.PhilosopherId,
            forkCommand.ForkId
        );

        await _commandChannel.Writer.WriteAsync(item);

        var answer = await _commandAnswerChannel.Reader.ReadAsync();
        return answer.Ok;
    }
}
