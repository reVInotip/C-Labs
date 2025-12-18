using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services.Channels.Items;
using InterfaceContracts.Channel;
using Interface;
using Services.Channels.Events;
using Services;
using DataContracts;
using Microsoft.Extensions.Options;

namespace Core.Models;

public class Waiter : BackgroundService, IWaiter
{
    private readonly PhilosophersStorage _storage;
    private readonly IChannel<PhilosopherWithForksIdsChannelItem> _registrationChannel;
    private readonly IChannel<CommandAnswerChannelItem> _commandAnswerChannel;
    private readonly IChannel<ForkCommandWithIdChannelItem> _commandChannel;
    private readonly IPhilosophersFactory _philosophersFactory;
    private readonly ILogger<Waiter> _logger;
    private readonly int _countPhilosophers = 0;

    public Waiter
    (  
        IChannel<PhilosopherWithForksIdsChannelItem> registrationChannel,
        IChannel<CommandAnswerChannelItem> commandAnswerChannel,
        IChannel<ForkCommandWithIdChannelItem> commandChannel,
        IOptions<ServicesConfigurations> servicesOptions,
        ILogger<Waiter> logger,
        IPhilosophersFactory philosophersFactory,
        PhilosophersStorage storage
    )
    {
        _registrationChannel = registrationChannel;
        _commandAnswerChannel = commandAnswerChannel;
        _commandChannel = commandChannel;
        _logger = logger;
        _philosophersFactory = philosophersFactory;
        _storage = storage;
        _countPhilosophers = servicesOptions.Value.CountPhilosophers;

        registrationChannel.SendMeItemBy += PhilosopherWantToRegister;
    }

    public void PhilosopherWantToRegister(object? sender, IChannelEventArgs data)
    {
        _logger.LogInformation("Registration handler");

        var name = ((ChannelRegistrationEvent)data).Name;
        var uri =((ChannelRegistrationEvent)data).Uri;

        var philosopher = _philosophersFactory.Create();
        philosopher.Name = name;
        philosopher.Uri = uri;

        _storage.Insert(philosopher.Id, philosopher);

        var item = new PhilosopherWithForksIdsChannelItem(
            philosopher.Id,
            philosopher.LeftFork.Id,
            philosopher.RightFork.Id);

        var task = _registrationChannel.Writer.WriteAsync(item);
        task.AsTask().Wait();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_storage.Count < _countPhilosophers)
                    continue;

                var command = await _commandChannel.Reader.ReadAsync(stoppingToken);
                var philosopher = _storage.Get(command.PhilosopherId);

                IFork fork;
                if (philosopher.LeftFork.Id == command.ForkId)
                    fork = philosopher.LeftFork;
                else if (philosopher.RightFork.Id == command.ForkId)
                    fork = philosopher.RightFork;
                else
                    throw new ApplicationException("Bad fork id in command");

                switch (command.Command)
                {
                    case ForkCommandsDto.Lock:
                        //_logger.LogWarning(command.Command.ToString());
                        fork.TryLock(philosopher);

                        await _commandAnswerChannel.Writer.WriteAsync(
                            new CommandAnswerChannelItem(fork.IsLockedBy(philosopher)),
                            stoppingToken);
                        break;
                    case ForkCommandsDto.Take:
                        //_logger.LogWarning(command.Command.ToString());
                        fork.TryTake(philosopher);

                        await _commandAnswerChannel.Writer.WriteAsync(
                            new CommandAnswerChannelItem(fork.IsTakenBy(philosopher)),
                            stoppingToken);
                        break;
                    case ForkCommandsDto.Put:
                        //_logger.LogWarning(command.Command.ToString());
                        fork.Put();
                        break;
                    case ForkCommandsDto.Unlock:
                        //_logger.LogWarning(command.Command.ToString());
                        fork.UnlockFork();
                        break;
                }
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

        return;
    }
}
