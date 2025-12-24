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

    [HttpPost("register-me")]
    public async Task<PhilosopherWithForksIds> RegisterPhilosopher([FromBody] RegistrationDto registrationDto)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();

        _channel.NotifyWith(this, new ChannelRegistrationEvent(
            registrationDto.Name, registrationDto.uri));

        var item = await _channel.Reader.ReadAsync();
        return new PhilosopherWithForksIds
        {
            PhilosopherId = item.PhilosopherId,
            LeftForkId = item.LeftForkId,
            RightForkId = item.RightForkId
        };
    }

    [HttpPost("execute-command")]
    public async Task<bool> ExecuteCommand([FromBody] ForkCommandWithIdDto forkCommand)
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

    [HttpGet("health")]
    public string Health()
    {
        return "Service is health";
    }
}
