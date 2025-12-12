using Microsoft.Extensions.Options;
using Core.Models.Utils;
using Interface;
using Interface.Strategy;
using InterfaceContracts.Channel;
using Services.Channels.Events;
using Services.Channels.Items;

namespace Core.Models;

public class PhilosopherService : BackgroundService, IPhilosopher
{
    private readonly IStrategy _philosopherStrategy;
    private readonly ILogger<PhilosopherService> _logger;
    private readonly IChannel<PhilosopherToControllerChannelItem> _channelToManager;
    private readonly IChannel<PhilosopherActionItem> _actionChannel;
    private readonly IRegistration _registration;
    private readonly IChannel<ApplicationStopItem> _stoppingChannel;

    private PhilosopherStates _state;
    private readonly int _eatingTime;
    private readonly int _takeForkTime;
    private readonly int _thinkingTime;
    private int _stateTimer;
    private CancellationToken _stoppingToken;
    private bool _isStopRequested = false;
    private readonly Lock _lockObject = new ();

    public string Name { get; set; }
    public int CountEatingFood { get; private set; }
    public int HungryTime { get; private set; }
    public int Id { get; private set; }

    public IFork LeftFork { get; private set; }
    public IFork RightFork { get; private set; }

    public PhilosopherService(
        ILogger<PhilosopherService> logger,
        IStrategy philosopherStrategy,
        IOptions<PhilosopherConfiguration> options,
        IChannel<PhilosopherToControllerChannelItem> channelToManager,
        IChannel<PhilosopherActionItem> actionChannel,
        IRegistration registration,
        IChannel<ApplicationStopItem> stoppingChannel,
        IEnumerable<IFork> forks)
    {
        _logger = logger;
        _philosopherStrategy = philosopherStrategy;

        _channelToManager = channelToManager;
        _channelToManager.SendMeItem += SendInfoToController;
        _channelToManager.SendMeItemBy += SendFinalStatsToController;

        _actionChannel = actionChannel;
        _actionChannel.SendMeItem += SendActionToController;

        _stoppingChannel = stoppingChannel;
        _stoppingChannel.SendMeItem += StoppingRequested;

        _registration = registration;

        var random = new Random();

        Name = options.Value.Name;
        _eatingTime = random.Next(options.Value.EatingTimeMin, options.Value.EatingTimeMax);
        _takeForkTime = random.Next(options.Value.TakeForkTimeMin, options.Value.TakeForkTimeMax);
        _thinkingTime = random.Next(options.Value.ThinkingTimeMin, options.Value.ThinkingTimeMax);

        LeftFork = forks.ElementAt(0);
        RightFork = forks.ElementAt(1);
    }

    private void StoppingRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("Stopping requested");
        var source = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
        source.Cancel();

        _isStopRequested = true;
    }

    private async void SendActionToController(object? sender, EventArgs e)
    {
        _logger.LogInformation("Send action to controller");
        var iAmEating = _state == PhilosopherStates.Eating;

        await _actionChannel.Writer.WriteAsync(new PhilosopherActionItem(iAmEating));
    }

    private async void SendFinalStatsToController(object? sender, IChannelEventArgs e)
    {
        _logger.LogInformation("Send final stats to controller");
        double simulationTime = ((ChannelScoresEvent)e).SimulationTime;
        var item = new PhilosopherToControllerChannelItem(
            GetScoreString(simulationTime),
            Id
        );
        await _channelToManager.Writer.WriteAsync(item);
    }

    private async void SendInfoToController(object? sender, EventArgs e)
    {
        _logger.LogInformation("Send info to controller");
        var item = new PhilosopherToControllerChannelItem(
            GetInfoString(),
            Id
        );

        await _channelToManager.Writer.WriteAsync(item);
    }

    public string GetInfoString()
    {
        string stateInfo;
        lock (_lockObject)
        {
            stateInfo = _state switch
            {
                PhilosopherStates.Thinking => $"Thinking ({_stateTimer} ms)",
                PhilosopherStates.Hungry => $"Hungry ({_stateTimer} ms)",
                PhilosopherStates.Eating => $"Eating ({_stateTimer} ms)",
                PhilosopherStates.TakeLeftFork => $"Taking Left Fork ({_stateTimer} ms)",
                PhilosopherStates.TakeRightFork => $"Taking Right Fork ({_stateTimer} ms)",
                _ => _state.ToString()
            };
        }

        return String.Format($"{Name}: {stateInfo}, meals: {CountEatingFood}");
    }

    public string GetScoreString(double simulationTime)
    {
        string result;
        lock (_lockObject)
        {
            double throughput = simulationTime > 0 ? CountEatingFood / simulationTime : 0;
            double hungryPercentage = simulationTime > 0 ? (HungryTime / simulationTime) * 100 : 0;
            result = String.Format($"{Name}: throughput {throughput:F4} meals/ms, " +
                            $"hungry {HungryTime} ms ({hungryPercentage:F1}%)");
        }

        return result;
    }

    private async Task PreRunInitialization()
    {
        var registrationInfo = await _registration.Registration(Name);
        Id = registrationInfo!.PhilosopherId;
        LeftFork.Id = registrationInfo!.LeftForkId;
        RightFork.Id = registrationInfo!.RightForkId;
    }

    private void CheckStopRequests()
    {
        if (_isStopRequested) throw new OperationCanceledException("Stop by manager");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        try
        {
            await PreRunInitialization();
            await Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        CheckStopRequests();
                        await ProcessState();
                    }
                },  stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Philosophers service shutdown!");
        }
        finally
        {
            await _philosopherStrategy.PutForks(this);
            await _stoppingChannel.Writer.WriteAsync(new ApplicationStopItem());
        }

        return;
    }

    private async Task ProcessState()
    {
        switch (_state)
        {
            case PhilosopherStates.Thinking:
                await ProcessThinkingState();
                break;
            case PhilosopherStates.Hungry:
                await ProcessHungryState();
                break;
            case PhilosopherStates.TakeLeftFork:
                await ProcessTakingLeftForkState();
                break;
            case PhilosopherStates.TakeRightFork:
                await ProcessTakingRightForkState();
                break;
            case PhilosopherStates.Eating:
                await ProcessEatingState();
                break;
        }
    }

    private async Task ProcessThinkingState()
    {
        while (_stateTimer < _thinkingTime)
        {
            //Console.WriteLine("Th {0}, {1}", _stateTimer, _eatingTime);
            await Task.Delay(_thinkingTime / 8);
            Interlocked.Add(ref _stateTimer, _thinkingTime / 8);
        }

        lock (_lockObject)
        {
            _stateTimer = 0;
            _state = PhilosopherStates.Hungry;
        }
    }

    private async Task ProcessHungryState()
    {
        if (await _philosopherStrategy.LockFork(this))
        {
            await Task.Delay(_takeForkTime);

            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            var type = await _philosopherStrategy.TakeFork(this);

            if (type == DataContracts.ForkType.Left)
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.TakeLeftFork;
                    _stateTimer = 0;
                }
            }
            else if (type == DataContracts.ForkType.Right)
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.TakeRightFork;
                    _stateTimer = 0;
                }
            }
        }
    }

    private async Task ProcessTakingLeftForkState()
    {
        if (await _philosopherStrategy.LockRightFork(this))
        {
            await Task.Delay(_takeForkTime);
            
            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            if (await _philosopherStrategy.TakeRightFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.Eating;
                    _stateTimer = 0;
                }
            }
        }
    }

    private async Task ProcessTakingRightForkState()
    {
        if (await _philosopherStrategy.LockLeftFork(this))
        {
            await Task.Delay(_takeForkTime);

            lock (_lockObject)
            {
                HungryTime += _takeForkTime;
            }
            Interlocked.Add(ref _stateTimer, _takeForkTime);

            if (await _philosopherStrategy.TakeLeftFork(this))
            {
                lock (_lockObject)
                {
                    _state = PhilosopherStates.Eating;
                    _stateTimer = 0;
                }
            }
        }
    }

    private async Task ProcessEatingState()
    {
        while (_stateTimer < _eatingTime)
        {
            await Task.Delay(_eatingTime / 8);
            Interlocked.Add(ref _stateTimer, _eatingTime / 8);
        }

        await _philosopherStrategy.PutForks(this);

        lock (_lockObject)
        {
            CountEatingFood++;

            _state = PhilosopherStates.Thinking;
            _stateTimer = 0;
        }
    }
}
