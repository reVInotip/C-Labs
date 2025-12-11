using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Options;
using Interface;

namespace Services;

public class ForksFactory(IOptions<ServicesConfigurations> options) : IForksFactory
{
    private readonly int _countPhilosophers = options.Value.CountPhilosophers;
    private readonly List<IFork> _forks = [];
    private int _countCalls = 0;
    private Type? _loadedType = null;
    private int _genericId = 0;
    private Lock _lock = new();

    public IFork Create()
    {
        lock (_lock)
        {
            if (_loadedType == null)
            {
                var philosophers = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch (ReflectionTypeLoadException e)
                        {
                            return e.Types.Where(t => t != null)!;
                        }
                    })
                    .Where(t => typeof(IFork).IsAssignableFrom(t)
                            && t.IsClass
                            && !t.IsAbstract)
                    .ToArray();

                _loadedType = philosophers[0];
            }
            ++_countCalls;

            if (_countCalls == 1)
            {
                var fork = (IFork)(_loadedType.GetConstructor(System.Type.EmptyTypes)?.Invoke(null));
                fork!.Id = _genericId;
                ++_genericId;

                _forks.Add(fork);
            }

            if (_countCalls == _countPhilosophers)
            {
                _countCalls = 0;
                var fork = _forks.First();
                _forks.Clear();

                return fork;
            }

            var globalFork = (IFork)(_loadedType.GetConstructor(System.Type.EmptyTypes)?.Invoke(null));
            globalFork!.Id = _genericId;
            ++_genericId;

            _forks.Add(globalFork);

            return _forks[_countCalls - 1];
        }
    }
}
