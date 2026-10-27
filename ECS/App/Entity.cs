using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.App
{
    public struct Entity
    {
        public readonly uint id;
        public Entity(uint id)
        {
            this.id = id;
        }
    }
}
