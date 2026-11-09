using ECS.App;
using WindowsHost.ECS.DataComponent;
using WindowsHost.ECS.Systems;
using System;
using System.Diagnostics;

namespace WindowsHost.States
{
    internal class StateECSTest : AppState
    {
        private World mainWorld;
        private int updateCount = 0;
        private const int MAX_UPDATES = 1000;
        private const int COUNT_ENTITIES = 100_000;//100_000
        private Stopwatch sw = new Stopwatch();

        public override void OnEnter(AppStateMachine stateMachine)
        {
            base.OnEnter(stateMachine);

            mainWorld = new World();

            mainWorld.TryAddSystem<TestSystem>();
            mainWorld.TryAddSystem<MovementSystem>();

            Console.WriteLine("=== ECS Stress Test ===");
            Console.WriteLine("Creating 100,000 entities with multiple components...");

            sw.Start();

            for (int i = 0; i < COUNT_ENTITIES; i++)
            {
                var entity = mainWorld.CreateEntity(
                    new Position { X = i, Y = i * 2, Z = i * 3 }
                );
                mainWorld.AddComponent(entity, new Velocity { VX = 0.1f * i, VY = 0.2f * i, VZ = 0.3f * i });

                if (i % 10 == 0)
                {
                    mainWorld.AddComponent(entity, new Health { Current = 100, Max = 100 });
                }
            }

            sw.Stop();
            Console.WriteLine($"Created {COUNT_ENTITIES} entities in {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine("Starting update loop...\n");

            sw.Restart();
        }

        public override void OnUpdate(AppStateMachine stateMachine)
        {
            base.OnUpdate(stateMachine);

            if (updateCount >= MAX_UPDATES)
            {
                sw.Stop();
                Console.WriteLine($"\n=== Test Complete ===");
                Console.WriteLine($"Processed {MAX_UPDATES} updates in {sw.Elapsed.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Average time per update: {sw.Elapsed.TotalMilliseconds / MAX_UPDATES:F3}ms");
                Console.WriteLine($"Performance: {COUNT_ENTITIES / (sw.Elapsed.TotalMilliseconds / MAX_UPDATES):F0} entities/ms");
                Console.WriteLine("\nFirst 10 entities after 1000 updates:");

                ref var manager = ref mainWorld.GetManager();

                if (manager.TryGetCount<Position>(out uint count))
                {
                    for (uint i = 0; i < Math.Min(10, count); i++)
                    {
                        ref var pos = ref manager.GetComponentDataRef<Position>(i);
                        Console.Write($"Entity {i,5}: {pos}");

                        if (manager.HaveAdjacentComponentData<Position, Velocity>(i))
                        {
                            ref var vel = ref manager.GetAdjacentComponentDataRef<Position, Velocity>(i);
                            Console.Write($" {vel}");
                        }
                        else
                        {
                            Console.Write(" [No Velocity]");
                        }

                        if (manager.HaveAdjacentComponentData<Position, Health>(i))
                        {
                            ref var health = ref manager.GetAdjacentComponentDataRef<Position, Health>(i);
                            Console.Write($" {health}");
                        }

                        Console.WriteLine();
                    }
                }

                stateMachine.Stop();
                return;
            }

            mainWorld.Update();
            updateCount++;

            if (updateCount % 100 == 0)
            {
                Console.WriteLine($"Update {updateCount}/{MAX_UPDATES} completed ({sw.Elapsed.TotalMilliseconds:F2}ms)");
            }
        }

        public override void OnExit(AppStateMachine stateMachine)
        {
            base.OnExit(stateMachine);
            mainWorld.Dispose();
        }
    }
}