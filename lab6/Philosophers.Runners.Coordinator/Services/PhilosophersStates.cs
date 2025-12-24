using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using Services.Channels.Items;
using DataContracts;
using Microsoft.Extensions.Logging;

namespace Services;

public class PhilosophersStates(
    IDeadlockAnalyzer deadlockAnalyzer,
    IRetranslationService retranslationService,
    ILogger<PhilosophersStates> logger
)
{
    private readonly Dictionary<int, IPhilosopher> _eating = [];
    private readonly Dictionary<int, IPhilosopher> _takingFirstFork = [];
    private readonly Dictionary<int, IPhilosopher> _thinking = [];
    private readonly IDeadlockAnalyzer _deadlockAnalyzer = deadlockAnalyzer;
    private readonly ILogger<PhilosophersStates> _logger = logger;
    private readonly IRetranslationService _retranslationService = retranslationService;

    public void Add(int philosopherId, int leftForkId, int rightForkId)
    {
        _thinking.Add(philosopherId, new Philosopher()
            {
                Id = philosopherId,
                LeftForkId = leftForkId,
                RightForkId = rightForkId,
                HavingForkId = -1,
                TryingToTakeForkId = -1,
            });
    }

    public async Task<bool> TryToSwitchState(CommandChannelItem command)
    {
        int philosopherId = command.PhilosopherId;
        IPhilosopher item;
        bool ok;

        switch (command.Command)
        {
            case ForkCommandsDto.Lock:
                _logger.LogDebug($"Start processing command: {command}");
                int forkId = _thinking.ContainsKey(philosopherId) ?  command.ForkId : -1;
                
                if (_thinking.ContainsKey(philosopherId) || _takingFirstFork.ContainsKey(philosopherId))
                {
                    item = Update(philosopherId);
                    item!.TryingToTakeForkId = command.ForkId;
                    
                    if (_deadlockAnalyzer.CheckDeadlock(
                        _eating, _takingFirstFork, _thinking))
                    {
                        _logger.LogDebug($"Command: {command} rejected by deadlock analyzer");
                        item.TryingToTakeForkId = -1;
                        Rollback(philosopherId);
                        return false;
                    }

                    _logger.LogDebug($"Retranslate command: {command}");
                    ok = await _retranslationService.RetranslateCommand(
                        command.Command,
                        command.PhilosopherId,
                        command.ForkId
                    );

                    if (ok)
                    {
                        _logger.LogDebug($"Command: {command} succeeded");
                        item.TryingToTakeForkId = -1;
                        item.HavingForkId = forkId;
                        return true;
                    }

                    _logger.LogDebug($"Command: {command} rejected by manager");
                    item.TryingToTakeForkId = -1;
                    Rollback(philosopherId);
                    return false;
                }

                _logger.LogDebug($"Unexpected command: {command} from Philosopher with id {philosopherId}");
                return false;
            case ForkCommandsDto.Take:
                _logger.LogDebug($"Start processing command: {command}");
                if (_takingFirstFork.ContainsKey(philosopherId) || _eating.ContainsKey(philosopherId))
                {
                    _logger.LogDebug($"Retranslate command: {command}");
                    ok = await _retranslationService.RetranslateCommand(
                        command.Command,
                        command.PhilosopherId,
                        command.ForkId
                    );

                    return ok;
                }

                _logger.LogDebug($"Unexpected command: {command} from Philosopher with id {philosopherId}");
                return false;
            case ForkCommandsDto.Put or ForkCommandsDto.Unlock:
                _logger.LogDebug($"Start processing command: {command}");
                if (_takingFirstFork.ContainsKey(philosopherId) || _eating.ContainsKey(philosopherId))
                {
                    Rollback(philosopherId);
                    
                    _logger.LogDebug($"Retranslate command: {command}");
                    ok = await _retranslationService.RetranslateCommand(
                        command.Command,
                        command.PhilosopherId,
                        command.ForkId
                    );
                    return ok;
                }

                _logger.LogDebug($"Unexpected command: {command} from Philosopher with id {philosopherId}");
                return false;
            
            default:
                _logger.LogError($"Unexpected command: {command} from Philosopher with id {philosopherId}");
                return false;
        }
    }

    private void Rollback(int philosopherId)
    {
        IPhilosopher item;
        
        if (_eating.ContainsKey(philosopherId))
        {
            item = _eating[philosopherId];
            _eating.Remove(philosopherId);
            _takingFirstFork.Add(philosopherId, item);
        }
        else if (_takingFirstFork.ContainsKey(philosopherId))
        {
            item = _takingFirstFork[philosopherId];
            _takingFirstFork.Remove(philosopherId);
            _thinking.Add(philosopherId, item);
        }
    }

    private IPhilosopher? Update(int philosopherId)
    {
        IPhilosopher? item = null;
        
        if (_thinking.ContainsKey(philosopherId))
        {
            item = _thinking[philosopherId];
            _thinking.Remove(philosopherId);
            _takingFirstFork.Add(philosopherId, item);
        }
        else if (_takingFirstFork.ContainsKey(philosopherId))
        {
            item = _takingFirstFork[philosopherId];
            _takingFirstFork.Remove(philosopherId);
            _eating.Add(philosopherId, item);
        }

        return item;
    }
}
