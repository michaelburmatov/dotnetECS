using ECS.App.NativeStructures;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ECS.App.Connectors
{
    internal class ConnectorData
    {
        //private NativeArray<uint, uint> _route;

        //public void AddComponent<T>(Entity entity, T componentData) where T : unmanaged
        //{
        //    var container = GetOrCreateContainer<T>();
        //    container[entity] = componentData;
        //}
        //public bool TryGetComponent<T>(Entity entity, out T componentData) where T : unmanaged
        //{
        //    var container = GetOrCreateContainer<T>();
        //    return container.TryGetValue(entity, out componentData);
        //}

        //private static IntPtr GetTypeHandle<T>() => typeof(T).TypeHandle.Value;

        //private NativeDictionary<Tto, Tfore> GetOrCreateContainer<T>() where T : unmanaged
        //{
        //    var typeHandle = GetTypeHandle<T>();

        //    if (_containersConnections.TryGetValue(typeHandle, out var ptr))
        //    {
        //        var handleFinded = GCHandle.FromIntPtr(ptr);
        //        return (NativeDictionary<Entity, T>)handleFinded.Target;
        //    }

        //    var container = new NativeDictionary<Entity, T>();
        //    var handle = GCHandle.Alloc(container, GCHandleType.Normal);
        //    var containerPtr = GCHandle.ToIntPtr(handle);

        //    _containersConnections.Add(typeHandle, containerPtr);

        //    return container;
        //}
    }
}
