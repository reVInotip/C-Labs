using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Options;
using Interface;
using Services.Utils;

namespace Services;

public class PhilosophersFactory : IPhilosophersFactory
{
    private readonly Type _philosopherType;
    private readonly int _countPhilosophers;
    private IPhilosopher[] _philosophers;
    private int _currentIndex = 0;
    private readonly object _lock = new();

    public PhilosophersFactory(
        IOptions<ServicesConfigurations> options,
        IForksFactory forksFactory)
    {
        _countPhilosophers = options.Value.CountPhilosophers;
        _philosopherType = FindPhilosopherType();
        
        // Создаем N вилок для N философов
        _philosophers = new IPhilosopher[_countPhilosophers];
        for (int i = 0; i < _countPhilosophers; i++)
        {
            var philosopher = (IPhilosopher)Activator.CreateInstance(_philosopherType)!;
            philosopher.Id = i;
            philosopher.LeftFork = forksFactory.GetFork((i + 1) % _countPhilosophers);
            philosopher.RightFork = forksFactory.GetFork(i);
            _philosophers[i] = philosopher;
        }
    }

    public IPhilosopher Create()
    {
        lock (_lock)
        {
            // Возвращаем вилку по кругу
            var philosopher = _philosophers[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _countPhilosophers;
            return philosopher;
        }
    }

    private Type FindPhilosopherType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(IPhilosopher).IsAssignableFrom(type) 
                        && type.IsClass 
                        && !type.IsAbstract 
                        && type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        return type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Пропускаем сборки, которые не можем загрузить
                continue;
            }
        }
        
        throw new InvalidOperationException($"No implementation of {nameof(IFork)} found in loaded assemblies");
        //return typeof(Philosopher);
    }
}
