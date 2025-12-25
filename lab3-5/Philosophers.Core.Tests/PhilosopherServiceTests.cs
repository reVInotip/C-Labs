using System;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Interface;
using Interface.Channel;
using Interface.Strategy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Philosophers.Core.HostedServices.Philosophers;
using Philosophers.Core.Utils;
using Philosophers.Services.Channels.Items;
using Philosophers.Services.Utils;
using Xunit;

namespace Philosophers.Core.Tests;

public class PhilosopherServiceTests
{
    // Utility: вызываем приватный ProcessState()
    private static void CallProcessState(PhilosopherService p)
    {
        var mi = typeof(PhilosopherService)
            .GetMethod("ProcessState", BindingFlags.NonPublic | BindingFlags.Instance);
        Task? task = (Task?) mi!.Invoke(p, null);
        task?.Wait();
    }

    // Utility: установка приватного _state
    private static void SetState(PhilosopherService p, PhilosopherStates state)
    {
        typeof(PhilosopherService)
            .GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(p, state);
    }

    private static PhilosopherStates GetState(PhilosopherService p)
    {
        return (PhilosopherStates)typeof(PhilosopherService)
            .GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(p)!;
    }

    private static PhilosopherService CreatePhilosopher(
        IStrategy strategy,
        IFork leftFork,
        IFork rightFork)
    {
        // Настройки времени — минимальные и фиксированные
        var config = Options.Create(new PhilosopherConfiguration
        {
            ThinkingTimeMin = 8,
            ThinkingTimeMax = 8,
            EatingTimeMin = 8,
            EatingTimeMax = 8,
            TakeForkTimeMin = 8,
            TakeForkTimeMax = 8
        });

        // Каналы — mock, но Writer должен быть рабочим
        var analyzerCh = new Mock<IChannel<PhilosopherToAnalyzerChannelItem>>();
        analyzerCh.Setup(c => c.Writer)
            .Returns(Channel.CreateUnbounded<PhilosopherToAnalyzerChannelItem>().Writer);

        var printerCh = new Mock<IChannel<PhilosopherToPrinterChannelItem>>();
        printerCh.Setup(c => c.Writer)
            .Returns(Channel.CreateUnbounded<PhilosopherToPrinterChannelItem>().Writer);

        // Фабрика вилок
        var factory = new Mock<IForksFactory>();
        factory.SetupSequence(f => f.Create())
            .Returns((leftFork, rightFork));

        // Логгер
        var logger = new Mock<ILogger<PhilosopherService>>();

        // Создаём конкретный минимальный наследник, чтобы запустить конструктор
        return new TestPhilosopherService(
            logger.Object,
            strategy,
            config,
            factory.Object,
            analyzerCh.Object,
            printerCh.Object);
    }

    // ------------------------------------------
    // Внутренний тестовый потомок PhilosopherService (Humble объект)
    // Нужен так как экземпляр PhilosopherService нельзя создать напрямую
    // ------------------------------------------
    private class TestPhilosopherService : PhilosopherService
    {
        public TestPhilosopherService(
            ILogger<PhilosopherService> logger,
            IStrategy strategy,
            IOptions<PhilosopherConfiguration> config,
            IForksFactory factory,
            IChannel<PhilosopherToAnalyzerChannelItem> analyzer,
            IChannel<PhilosopherToPrinterChannelItem> printer)
            : base(logger, strategy, config, factory, analyzer, printer)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //
            // нужно просто чтобы компилятор не ругался на
            // то что метод async, но не использует await
            //
            await Task.Delay(1, stoppingToken);

            // Не запускаем поток — тестируем только ProcessState()
        }
    }

    // ============================================================
    //                     ТЕСТЫ СОСТОЯНИЙ
    // ============================================================

    [Fact]
    public void Thinking_Transitions_To_Hungry()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();
        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);
        
        SetState(p, PhilosopherStates.Thinking);

        // Act
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.Hungry, GetState(p));
    }

    [Fact]
    public void Hungry_Transitions_To_TakeLeftFork()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();

        // Fork locked + HasLeftFork = true
        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);
        strategy.Setup(s => s.HasLeftFork(It.IsAny<IPhilosopher>())).Returns(true);

        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);

        SetState(p, PhilosopherStates.Hungry);

        // Act
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.TakeLeftFork, GetState(p));
    }

    [Fact]
    public void Hungry_Transitions_To_TakeRightFork()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();

        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);
        strategy.Setup(s => s.HasLeftFork(It.IsAny<IPhilosopher>())).Returns(false);
        strategy.Setup(s => s.HasRightFork(It.IsAny<IPhilosopher>())).Returns(true);

        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);

        SetState(p, PhilosopherStates.Hungry);

        // Act
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.TakeRightFork, GetState(p));
    }

    [Fact]
    public void TakeLeftFork_Transitions_To_Eating()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();

        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);
        strategy.Setup(s => s.HasRightFork(It.IsAny<IPhilosopher>())).Returns(true);

        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);

        SetState(p, PhilosopherStates.TakeLeftFork);

        // Act
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.Eating, GetState(p));
    }

    [Fact]
    public void TakeRightFork_Transitions_To_Eating()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();

        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);
        strategy.Setup(s => s.HasLeftFork(It.IsAny<IPhilosopher>())).Returns(true);

        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);

        SetState(p, PhilosopherStates.TakeRightFork);

        // Act
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.Eating, GetState(p));
    }

    [Fact]
    public void Eating_Transitions_To_Thinking_And_Increments_Food()
    {
        // Arrange
        var strategy = new Mock<IStrategy>();

        var left = new Mock<IFork>();
        var right = new Mock<IFork>();

        var p = CreatePhilosopher(strategy.Object, left.Object, right.Object);

        SetState(p, PhilosopherStates.Eating);

        // Act
        // Внутренний _stateTimer должен достигнуть eatingTime
        CallProcessState(p);

        // Accept
        Assert.Equal(PhilosopherStates.Thinking, GetState(p));
        Assert.Equal(1, p.CountEatingFood);
    }

    [Fact]
    public async Task TwoPhilosophers_Cannot_Take_The_Same_Fork_At_The_Same_Time()
    {
        // Arrange
        // Вилка одна — создаём Mock
        var fork = new Mock<IFork>();
        IPhilosopher? currentOwner = null;
        object sync = new object();

        // Эмуляция потокобезопасного IFork.TryTake
        fork.Setup(f => f.TryTake(It.IsAny<IPhilosopher>()))
            .Callback<IPhilosopher>(p =>
            {
                lock (sync)
                {
                    currentOwner ??= p;
                }
            });

        fork.Setup(f => f.IsTakenBy(It.IsAny<IPhilosopher>()))
            .Returns<IPhilosopher>(p => currentOwner == p);

        // Стратегия просто вызывает TryTake на вилке
        var strategy = new Mock<IStrategy>();
        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);
        strategy.Setup(s => s.HasLeftFork(It.IsAny<IPhilosopher>()))
                .Returns<IPhilosopher>(p => fork.Object.IsTakenBy(p));

        strategy.Setup(s => s.TakeFork(It.IsAny<IPhilosopher>()))
                .Callback<IPhilosopher>(p => fork.Object.TryTake(p));

        var p1 = CreatePhilosopher(strategy.Object, fork.Object, fork.Object);
        var p2 = CreatePhilosopher(strategy.Object, fork.Object, fork.Object);

        SetState(p1, PhilosopherStates.Hungry);
        SetState(p2, PhilosopherStates.Hungry);

        // Act
        // Барьер чтобы стартовать ровно одновременно
        var barrier = new Barrier(3);

        var t1 = Task.Run(() =>
        {
            barrier.SignalAndWait();
            CallProcessState(p1);
        });

        var t2 = Task.Run(() =>
        {
            barrier.SignalAndWait();
            CallProcessState(p2);
        });

        barrier.SignalAndWait();
        await Task.WhenAll(t1, t2);

        // Accept
        // Проверяем, что владелец ровно один
        int owners =
            (fork.Object.IsTakenBy(p1) ? 1 : 0) +
            (fork.Object.IsTakenBy(p2) ? 1 : 0);

        Assert.Equal(1, owners);
    }

    [Fact]
    public async Task MultipleHungryTransitions_DoNotCause_DoubleLock()
    {
        // Arrange
        int lockCounter = 0;
        object syncObj = new object();

        var leftFork = new Mock<IFork>();
        var rightFork = new Mock<IFork>();

        // Любая блокировка увеличивает счётчик
        leftFork.Setup(f => f.TryLock(It.IsAny<IPhilosopher>()))
            .Callback<IPhilosopher>(p =>
            {
                lock (syncObj)
                    lockCounter++;
            });

        rightFork.Setup(f => f.TryLock(It.IsAny<IPhilosopher>()))
            .Callback<IPhilosopher>(p =>
            {
                lock (syncObj)
                    lockCounter++;
            });

        // Стратегия может блокировать обе вилки
        var strategy = new Mock<IStrategy>();
        strategy.Setup(s => s.IsForkLocked(It.IsAny<IPhilosopher>())).Returns(true);

        strategy.Setup(s => s.LockFork(It.IsAny<IPhilosopher>()))
                .Callback<IPhilosopher>(p => leftFork.Object.TryLock(p));
        strategy.Setup(s => s.LockLeftFork(It.IsAny<IPhilosopher>()))
                .Callback<IPhilosopher>(p => leftFork.Object.TryLock(p));
        strategy.Setup(s => s.LockRightFork(It.IsAny<IPhilosopher>()))
                .Callback<IPhilosopher>(p => rightFork.Object.TryLock(p));

        strategy.Setup(s => s.TakeFork(It.IsAny<IPhilosopher>()))
                .Callback<IPhilosopher>(p => leftFork.Object.TryTake(p));

        strategy.Setup(s => s.HasLeftFork(It.IsAny<IPhilosopher>()))
                .Returns(true);

        var philosophers = new[]
        {
            CreatePhilosopher(strategy.Object, leftFork.Object, rightFork.Object),
            CreatePhilosopher(strategy.Object, leftFork.Object, rightFork.Object),
            CreatePhilosopher(strategy.Object, leftFork.Object, rightFork.Object),
            CreatePhilosopher(strategy.Object, leftFork.Object, rightFork.Object)
        };

        foreach (var p in philosophers)
            SetState(p, PhilosopherStates.Hungry);

        // Act
        // Запускаем 20 параллельных попыток обработать состояние
        var tasks = new Task[20];
        for (int i = 0; i < 20; i++)
        {
            var p = philosophers[i % 4];
            tasks[i] = Task.Run(() => CallProcessState(p));
        }

        await Task.WhenAll(tasks);

        // Accept
        // Double-lock = неожиданное повышение счётчика
        Assert.True(lockCounter <= 20 * 2, $"lockCounter too large: {lockCounter}");
    }
}

