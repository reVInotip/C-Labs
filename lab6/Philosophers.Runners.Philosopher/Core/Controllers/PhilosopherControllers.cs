using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InterfaceContracts.Channel;
using Services.Channels.Items;
using Services.Channels.Events;
using DataContracts;
using System.Threading.Tasks;

namespace Core.Controllers;

[ApiController]
[Route("[controller]")]
public class PhilosopherController(
    ILogger<PhilosopherController> logger,
    IChannel<PhilosopherToControllerChannelItem> channel,
    IChannel<PhilosopherActionItem> actionChannel,
    IChannel<ApplicationStopItem> stoppingChannel) : Controller
{
    private readonly ILogger<PhilosopherController> _logger = logger;
    private readonly IChannel<PhilosopherToControllerChannelItem> _channel = channel;
    private readonly IChannel<PhilosopherActionItem> _actionChannel = actionChannel;
    private readonly IChannel<ApplicationStopItem> _stoppingChannel = stoppingChannel;

    [HttpGet]
    public async Task<PhilosopherInfo> GetPhilosopherInfo()
    {
        _channel.Notify(this);

        var item = await _channel.Reader.ReadAsync();
        return new PhilosopherInfo
        {
            Info = item.PhilosopherInfo,
            Id = item.Id
        };
    }

    [HttpGet("stats")]
    public async Task<PhilosopherInfo> GetPhilosopherStats(double simulationTime)
    {
        _channel.NotifyWith(this, new ChannelScoresEvent() {SimulationTime = simulationTime});

        var item = await _channel.Reader.ReadAsync();
        return new PhilosopherInfo
        {
            Info = item.PhilosopherInfo,
            Id = item.Id
        };
    }

    [HttpGet("action")]
    public async Task<PhilosopherAction> GetPhilosopherAction()
    {
        _actionChannel.Notify(this);

        var item = await _actionChannel.Reader.ReadAsync();
        return new PhilosopherAction
        {
            IAmEating = item.iAmEating
        };
    }

    [HttpGet("stop")]
    public async Task StopApplication()
    {
        _stoppingChannel.Notify(this);

        await _stoppingChannel.Reader.ReadAsync();
    }
}
