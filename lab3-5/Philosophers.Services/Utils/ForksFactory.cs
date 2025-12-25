using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Interface;
using Microsoft.Extensions.Options;

namespace Philosophers.Services.Utils;

public class ForksFactory : IForksFactory
{
    private readonly Type _forkType;
    private readonly int _countPhilosophers;
    private readonly IFork[] _forks;
    private int _index = 0;
    
    public ForksFactory(IOptions<PhilosopherConfiguration> options)
    {
        _countPhilosophers = options.Value.CountPhilosophers;
        _forkType = FindForkType();
        
        // Создаем N вилок для N философов
        _forks = new IFork[_countPhilosophers];
        for (int i = 0; i < _countPhilosophers; i++)
        {
            var fork = (IFork)Activator.CreateInstance(_forkType)!;
            _forks[i] = fork;
        }
    }

    public (IFork, IFork) Create()
    {
        var left = _forks[(_index + 1) % _countPhilosophers];
        var right = _forks[_index];
        ++_index;
        return (left, right);
    }

    private Type FindForkType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(IFork).IsAssignableFrom(type) 
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
        //return typeof(Fork);
    }
}
