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

public class SimulationManager : BackgroundService, ISimulationManager
{
    private readonly PhilosophersStorage _storage;
    private readonly ILogger<SimulationManager> _logger;
    private readonly CompletionCoordinator _coordinator;
    private readonly IPhilosopherNetwork _network;
    private readonly IForksFactory _forksFactory;
    private int _step = 0;
    private readonly Stopwatch _stopwatch = new();
    private readonly int _steps;
    private readonly int _countPhilosophers = 0;

    public SimulationManager
    (  
        IOptions<SimulationManagerConfiguration> options,
        IOptions<ServicesConfigurations> servicesOptions,
        ILogger<SimulationManager> logger,
        IForksFactory forksFactory,
        IPhilosopherNetwork network,
        PhilosophersStorage storage,
        CompletionCoordinator coordinator
    )
    {
        _logger = logger;
        _coordinator = coordinator;
        _steps = options.Value.Steps;
        _countPhilosophers = servicesOptions.Value.CountPhilosophers;
        _forksFactory = forksFactory;
        _storage = storage;
        _network = network;
    }

    private async Task PrintInfo()
    {
        Console.Clear();
        Console.WriteLine("==============STEP{0}==============", _step);

        foreach (var philosopher in _storage)
        {
            var info = await _network.GetInfo(philosopher.Uri);
            Console.WriteLine(info?.Info);

            Console.WriteLine(" |- Left Fork: " + philosopher.LeftFork.GetInfoString());
            Console.WriteLine(" |- Right Fork: " + philosopher.RightFork.GetInfoString());
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _coordinator.RegisterService(GetType().Name);

            _stopwatch.Start();
            while (!stoppingToken.IsCancellationRequested && _step < _steps)
            {
                await Task.Delay(2000, stoppingToken);
                if (_storage.Count < _countPhilosophers)
                    continue;

                ++_step;

                await PrintInfo();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Simulation manager shutdown");
        }
        catch (Exception e)
        {
            _logger.LogCritical($"Unexpected exception from {e.Source}.\nMessage: {e.Message}.\nStack Trace: {e.StackTrace}.");
        }
        finally
        {
            _stopwatch.Stop();

            Console.Clear();
            Console.WriteLine("==============Scores==============");

            foreach (var philosopher in _storage)
            {
                var info = await _network.GetStats(philosopher.Uri, _stopwatch.ElapsedMilliseconds);
                Console.WriteLine(info?.Info);

                Console.WriteLine(" |- Left Fork: " + philosopher.LeftFork.GetScoreString(_stopwatch.ElapsedMilliseconds));
                Console.WriteLine(" |- Right Fork: " + philosopher.RightFork.GetScoreString(_stopwatch.ElapsedMilliseconds));
            }
            await _coordinator.CompleteService(GetType().Name);
        }

        return;
    }
}
