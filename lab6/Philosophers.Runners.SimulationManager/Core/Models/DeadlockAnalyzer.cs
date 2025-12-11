using System.Diagnostics;
using Microsoft.Extensions.Options;
using InterfaceContracts.Channel;
using Interface;
using Services.Channels.Items;
using Services;
using Services.Network;
using Core.Models.Utils;
using Services.Channels.Events;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace Core.Models;

public class DeadlockAnalyzer : BackgroundService, IDeadlockAnalyzer
{
    private readonly PhilosophersStorage _storage;
    private readonly ILogger<DeadlockAnalyzer> _logger;
    private readonly IPhilosopherNetwork _network;
    private readonly IForksFactory _forksFactory;

    public DeadlockAnalyzer
    (
        ILogger<DeadlockAnalyzer> logger,
        IForksFactory forksFactory,
        IPhilosopherNetwork network,
        PhilosophersStorage storage
    )
    {
        _logger = logger;
        _forksFactory = forksFactory;
        _storage = storage;
        _network = network;
    }

    private async Task<bool> IsDeadlock()
    {
        bool isCheckAnyPhilosopher = false;

        foreach (var philosopher in _storage)
        {
            isCheckAnyPhilosopher = true;
            var info = await _network.GetAction(philosopher.Uri);

            var iAmEating = info?.IAmEating ?? false;
            var leftForkIsFree = philosopher.LeftFork.Owner == null;
            var rightForkIsFree = philosopher.RightFork.Owner == null;

            if (leftForkIsFree || rightForkIsFree)
            {
                return false;
            }

            if (iAmEating)
            {
                return false;
            }
        }

        return isCheckAnyPhilosopher;
    }

    public async Task Analyze(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await IsDeadlock())
            {
                _logger.LogCritical("DEADLOCK DETECTED!");
                return;
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Analyze(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Deadlock analyzer shutdown!");
        }
        catch (Exception e)
        {
            _logger.LogCritical($"Unexpected exception from {e.Source}.\nMessage: {e.Message}.\nStack Trace: {e.StackTrace}.");
        }

        return;
    }
}
