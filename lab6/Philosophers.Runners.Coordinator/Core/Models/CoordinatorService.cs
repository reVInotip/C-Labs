using Interface;
using MassTransit;
using InterfaceContracts.Channel;
using Services.Channels;
using Services.Channels.Items;
using DataContracts;
using Services;
using System.Text;
using System.Text.Json;

namespace Core.Models;

public class CoordinatorService : BackgroundService, ICoordinator
{
    readonly IBus _bus;
    private readonly IChannel<CommandChannelItem> _commandsChannel;
    private readonly IChannel<ApplicationStopItem> _stoppingChannel;
    private readonly PhilosophersStates _states;
    private CancellationToken _stoppingToken;
    private readonly ILogger<CoordinatorService> _logger;
    private bool _isStopRequested = false;

    public CoordinatorService(
        IBus bus,
        IChannel<CommandChannelItem> commandsChannel,
        PhilosophersStates states,
        IChannel<ApplicationStopItem> stoppingChannel,
        ILogger<CoordinatorService> logger
    )
    {
        _bus = bus;
        _commandsChannel = commandsChannel;
        _states = states;

        _logger = logger;

        _stoppingChannel = stoppingChannel;
        _stoppingChannel.SendMeItem += StoppingRequested;
    }

    private void StoppingRequested(object? sender, EventArgs e)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
        source.Cancel();

        _isStopRequested = true;
    }

    private void CheckStopRequests()
    {
        if (_isStopRequested) throw new OperationCanceledException("Stop by manager");
    }

    override protected async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CheckStopRequests();
                var command = await _commandsChannel.Reader.ReadAsync(stoppingToken);
                bool ok = await _states.TryToSwitchState(command);

                await _bus.Publish(new CommandExecutionResult(command.PhilosopherId, ok), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Coordinator service shutdown!");
        }
        finally
        {
            await _stoppingChannel.Writer.WriteAsync(new ApplicationStopItem());
        }
    }
}
