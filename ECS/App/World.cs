using AbstractGoF.Creational;
using ECS.App.ISystem;
using ECS.App.NativeStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App
{
    public class World : IDisposable
    {
        private ManagerECS _manager;
        private NativeHashSet<Entity> _entities;
        private List<ISystemUpdate> _systemsUpdate = new List<ISystemUpdate>();
        private List<ISystemEnter> _systemsEnter = new List<ISystemEnter>();

        public World()
        {
            _manager = new ManagerECS(16);
            _entities = new NativeHashSet<Entity>(16);
        }

        public void Update()
        {
            foreach (var system in _systemsUpdate)
            {
                system.Update(ref _manager);
            }
        }

        public bool TryAddSystem<T>()
        {
            if (Singleton<PoolSystems>.Instance.TryGetSystem<T>(out var system))
            {
                if (system is ISystemUpdate systemUpdate)
                    _systemsUpdate.Add(systemUpdate);
                if (system is ISystemEnter systemEnter)
                    _systemsEnter.Add(systemEnter);
                return true;
            }
            return false;
        }

        public Entity CreateEntity<T>(params T[] componentData) where T : unmanaged
        {
            var entity = new Entity((uint)_entities.Count);
            _entities.Add(entity);

            if (componentData != null)
                foreach (var component in componentData)
                    AddComponent(entity, component);

            return entity;
        }

        public void AddComponent<T>(Entity entity, T componentData) where T : unmanaged
        {
            _manager.AddComponent<T>(entity, componentData);
            //ComponentConnector<T>
        }

        public void Dispose()
        {
            _manager.Dispose();
            _entities.Dispose();
        }
    }
}
