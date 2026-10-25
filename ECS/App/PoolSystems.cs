using AbstractGoF.Creational;
using System.Collections.Generic;

namespace ECS.App
{
    internal class PoolSystems : Singleton<PoolSystems>
    {
        private List<object> allSystems = new List<object>();

    }
}
