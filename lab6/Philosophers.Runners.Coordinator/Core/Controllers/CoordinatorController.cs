using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Interface;
using DataContracts;
using InterfaceContracts.Channel;
using Services.Channels.Items;
using Services;

namespace Core.Controllers;

[ApiController]
[Route("[controller]")]
public class CoordinatorController(
    ILogger<CoordinatorController> logger,
    IChannel<ApplicationStopItem> stoppingChannel,
    PhilosophersStates states,
    IRetranslationService retranslationService) : Controller
{
    private readonly ILogger<CoordinatorController> _logger = logger;
    private readonly IRetranslationService _retranslationService = retranslationService;
    private readonly IChannel<ApplicationStopItem> _stoppingChannel = stoppingChannel;
    private readonly PhilosophersStates _states = states;

    [HttpPost("register-me")]
    public async Task<PhilosopherWithForksIds> RegisterPhilosopher([FromForm] string name)
    {
        _logger.LogInformation($"Handle registration event from {name}");

        var clientIp = HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();

        var originUri = $"{Request.Scheme}://{clientIp}:8080/Philosopher/";
        var result = await _retranslationService.RetranslateRegistration(name, originUri);
        _logger.LogInformation($"Registration result {result!.PhilosopherId} {result.LeftForkId} {result.RightForkId}");

        _states.Add(result!.PhilosopherId, result.LeftForkId, result.RightForkId);

        return result;
    }

    [HttpGet("health")]
    public string Health()
    {
        return "Service is health";
    }

    [HttpGet]
    public async Task<PhilosopherInfo?> GetPhilosopherInfo(string originUri)
    {
        _logger.LogInformation("Handle getting philosopher info");
        var result = await _retranslationService.RetranslateGetPhilosopherInfo(originUri);
        return result;
    }

    [HttpGet("stats")]
    public async Task<PhilosopherInfo?> GetPhilosopherStats(double simulationTime, string originUri)
    {
        _logger.LogInformation("Handle getting philosopher stats");
        var result = await _retranslationService.RetranslateGetPhilosopherStats(simulationTime, originUri);
        return result;
    }

    [HttpGet("stop")]
    public async Task StopApplication(string originUri)
    {
        _logger.LogInformation("Handle stop philosopher philosopher");
        await _retranslationService.RetranslateStopApplication(originUri);
    }

    [HttpGet("stop-me")]
    public async Task StopApplication()
    {
        _logger.LogInformation("Handle stop request");
        _stoppingChannel.Notify(this);

        await _stoppingChannel.Reader.ReadAsync();
    }
}
