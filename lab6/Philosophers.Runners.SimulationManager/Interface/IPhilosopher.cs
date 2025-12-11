using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Interface;

public interface IPhilosopher
{
    static private Type? _loadedType = null;
    static private int _genericId = 0;
    static private Lock _lock = new();

    static IPhilosopher Create()
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
                    .Where(t => typeof(IPhilosopher).IsAssignableFrom(t)
                            && t.IsClass
                            && !t.IsAbstract)
                    .ToArray();

                _loadedType = philosophers[0];
            }

            var philosopher = (IPhilosopher)(_loadedType.GetConstructor(System.Type.EmptyTypes)?.Invoke(null));
            philosopher!.Id = _genericId;
            ++_genericId;

            return philosopher;
        }
    }
    int Id { get; set; }
    string Name { get; set; }
    IFork LeftFork { get; set; }
    IFork RightFork { get; set; }
    string Uri { get; set; }
}
