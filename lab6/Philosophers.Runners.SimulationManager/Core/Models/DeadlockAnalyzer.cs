using System.Diagnostics;
using Microsoft.Extensions.Options;
using InterfaceContracts.Channel;
using Interface;
using Services.Channels.Items;
using Services;
using Services.Network;
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
    private readonly int _countPhilosophers = 0;

    public DeadlockAnalyzer
    (
        ILogger<DeadlockAnalyzer> logger,
        IOptions<ServicesConfigurations> servicesOptions,
        IForksFactory forksFactory,
        IPhilosopherNetwork network,
        PhilosophersStorage storage
    )
    {
        _logger = logger;
        _forksFactory = forksFactory;
        _countPhilosophers = servicesOptions.Value.CountPhilosophers;
        _storage = storage;
        _network = network;
    }

    private async Task<bool> IsDeadlock()
    {
        if (_storage.Count < _countPhilosophers)
            return false;

        foreach (var philosopher in _storage)
        {
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

        return true;
    }

    public async Task Analyze(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await IsDeadlock())
            {
                _logger.LogCritical("DEADLOCK DETECTED!");
            }

            await Task.Delay(2000, stoppingToken);
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
