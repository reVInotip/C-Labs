using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Interface;

namespace Services.Network;

public class CompletionCoordinator
{
    private readonly PhilosophersStorage _storage;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<CompletionCoordinator> _logger;
    private readonly IPhilosopherNetwork _network;
    private int _activeServices = 0;

    public CompletionCoordinator(
        IHostApplicationLifetime lifetime,
        PhilosophersStorage storage,
        IPhilosopherNetwork network,
        ILogger<CompletionCoordinator> logger)
    {
        _storage = storage;
        _lifetime = lifetime;
        _logger = logger;
        _network = network;
    }

    public void RegisterService(string serviceName)
    {
        var count = Interlocked.Increment(ref _activeServices);
    }

    public async Task CompleteService(string serviceName)
    {
        var count = Interlocked.Decrement(ref _activeServices);
        if (count == 0)
        {
            _logger.LogWarning("All services completed. Stopping host...");

            foreach (var philosopher in _storage)
            {
                await _network.Stop(philosopher.Uri);
            }
            _lifetime.StopApplication();
        }
    }
}
