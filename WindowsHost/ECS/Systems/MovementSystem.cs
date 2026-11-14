using ECS.App;
using ECS.App.ISystem;
using WindowsHost.ECS.DataComponent;
using System.Diagnostics;
using System.Collections.Specialized;

namespace WindowsHost.ECS.Systems
{
    internal struct MovementSystem : ISystemUpdate
    {
        public void Update(ref ManagerECS manager)
        {
            var sw = Stopwatch.StartNew();



            //if (manager.TryGetCount<Position>(out uint posCount))
            //{
            //    for (uint i = 0; i < posCount; i++)
            //    {
            //        ref var pos = ref manager.GetComponentDataRef<Position>(i);

            //        if (manager.HaveAdjacentComponentData<Position, Velocity>(i))
            //        {
            //            ref var vel = ref manager.GetAdjacentComponentDataRef<Position, Velocity>(i);

            //            pos.X += vel.VX;
            //            pos.Y += vel.VY;
            //            pos.Z += vel.VZ;

            //            vel.VX *= 0.99f;
            //            vel.VY *= 0.99f;
            //            vel.VZ *= 0.99f;

            //        }
            //    }
            //}


            if (manager.TryGetFilter<Position, Velocity>(out var filter))
            {
                var count = filter.Count;
                for (var i = 0; i < count; i++)
                {
                    ref var pos = ref filter.GetFirst(i);
                    ref var vel = ref filter.GetSecond(i);

                    pos.X += vel.VX;
                    pos.Y += vel.VY;
                    pos.Z += vel.VZ;

                    vel.VX *= 0.99f;
                    vel.VY *= 0.99f;
                    vel.VZ *= 0.99f;
                }
            }

            sw.Stop();

            //Console.WriteLine($"MovementSystem: {sw.Elapsed.TotalMilliseconds:F3}ms");
        }
    }
}