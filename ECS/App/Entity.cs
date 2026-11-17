using System;

namespace ECS.App
{
    public struct Entity : IEquatable<Entity>
    {
        public readonly uint id;
        public Entity(uint id) => this.id = id;
        public bool Equals(Entity other) => id == other.id;
        public override bool Equals(object obj) => obj is Entity e && Equals(e);
        public override int GetHashCode() => (int)id;
    }
}