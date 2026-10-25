using ECS.App.ISystem;
using ECS.App.NativeStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App
{
    public class World
    {
        private ManagerECS manager;
        private NativeList<Entity> _entities;

        private List<ISystemUpdate> systemsUpdate;
        private List<ISystemEnter> systemsEnter;

        public void Update()
        {
            foreach (var system in systemsUpdate)
            {
                system.Update();
            }
        }

        public void AddSystem<T>() where T : ISystemUpdate
        {

            //if (system is ISystemUpdate systemUpdate)
            //    systemsUpdate.Add(systemUpdate);


        }
    }
}
