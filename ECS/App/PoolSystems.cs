using AbstractGoF.Creational;
using ECS.App.ISystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PoolSystems : Singleton<PoolSystems>
{
    private Dictionary<Type, object> allSystems = new Dictionary<Type, object>();

    private PoolSystems()
    {
        LoadSystems();
    }

    private void LoadSystems()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    //.Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => !t.IsAbstract)
                    .Where(t => typeof(ISystemUpdate).IsAssignableFrom(t) &&
                               typeof(ISystemEnter).IsAssignableFrom(t));

                foreach (var type in types)
                {
                    if (!allSystems.ContainsKey(type))
                    {
                        var instance = Activator.CreateInstance(type);
                        allSystems[type] = instance;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }
    }

    public bool TryGetSystem<T>(out T result)
    {
        var type = typeof(T);

        if (allSystems.TryGetValue(type, out var system))
        {
            if (system is T clear)
            {
                result = clear;
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool TryGetSystem(Type systemType, out object result)
    {
        return allSystems.TryGetValue(systemType, out result);
    }
}