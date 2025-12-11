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
using Core.Models.Utils;

namespace Core.Models;

public class Waiter : BackgroundService, IWaiter
{
    private readonly PhilosophersStorage _storage;
    private readonly IChannel<PhilosopherWithForksIdsChannelItem> _registrationChannel;
    private readonly IChannel<CommandAnswerChannelItem> _commandAnswerChannel;
    private readonly IChannel<ForkCommandWithIdChannelItem> _commandChannel;
    private readonly IForksFactory _forksFactory;
    private readonly ILogger<Waiter> _logger;

    public Waiter
    (  
        IChannel<PhilosopherWithForksIdsChannelItem> registrationChannel,
        IChannel<CommandAnswerChannelItem> commandAnswerChannel,
        IChannel<ForkCommandWithIdChannelItem> commandChannel,
        ILogger<Waiter> logger,
        IForksFactory forksFactory,
        PhilosophersStorage storage
    )
    {
        _registrationChannel = registrationChannel;
        _commandAnswerChannel = commandAnswerChannel;
        _commandChannel = commandChannel;
        _logger = logger;
        _forksFactory = forksFactory;
        _storage = storage;

        registrationChannel.SendMeItemBy += PhilosopherWantToRegister;
    }

    public void PhilosopherWantToRegister(object? sender, IChannelEventArgs data)
    {
        var name = ((ChannelRegistrationEvent)data).Name;
        var uri =((ChannelRegistrationEvent)data).Uri;

        var philosopher = IPhilosopher.Create();
        philosopher.LeftFork = _forksFactory.Create();
        philosopher.RightFork = _forksFactory.Create();
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
                        fork.TryLock(philosopher);

                        await _commandAnswerChannel.Writer.WriteAsync(
                            new CommandAnswerChannelItem(fork.IsLockedBy(philosopher)),
                            stoppingToken);
                        break;
                    case ForkCommandsDto.Take:
                        fork.TryTake(philosopher);

                        await _commandAnswerChannel.Writer.WriteAsync(
                            new CommandAnswerChannelItem(fork.IsTakenBy(philosopher)),
                            stoppingToken);
                        break;
                    case ForkCommandsDto.Put:
                        fork.Put();
                        break;
                    case ForkCommandsDto.Unlock:
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
