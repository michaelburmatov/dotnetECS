using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App
{
    internal struct ComponentDataActivator<T> where T : unmanaged
    {
        public bool isActive;
        public T componentData;
    }
}
