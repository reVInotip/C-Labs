using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Options;
using Interface;
using Services.Utils;

namespace Services;

public class ForksFactory : IForksFactory
{
    private readonly Type _forkType;
    private readonly int _countPhilosophers;
    private IFork[] _forks;
    
    public ForksFactory(IOptions<ServicesConfigurations> options)
    {
        _countPhilosophers = options.Value.CountPhilosophers;
        _forkType = FindForkType();
        
        // Создаем N вилок для N философов
        _forks = new IFork[_countPhilosophers];
        for (int i = 0; i < _countPhilosophers; i++)
        {
            var fork = (IFork)Activator.CreateInstance(_forkType)!;
            fork.Id = i;
            _forks[i] = fork;
        }
    }
    
    public IFork GetFork(int index)
    {
        return _forks[index];
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