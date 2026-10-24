using ECS.App.NativeStructures;
using System;

public static class Program
{
    public static void Main()
    {
        using (var array = new NativeArray<int>(8, clearMemory: true))
        {
            array[0] = 42;
            array[1] = 77;

            Console.WriteLine(array[0]);
        }

        using (var list = new NativeList<int>(8))
        {
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.RemoveAtSwapBack(0);

            Console.WriteLine(list.Length);
        }

        using (var set = new NativeHashSet<int>(16))
        {
            set.Add(10);
            set.Add(20);

            Console.WriteLine(set.Contains(10));
            Console.WriteLine(set.Contains(99));
        }

        using (var map = new NativeDictionary<int, float>(16))
        {
            map.Add(1, 1.5f);
            //map[2] = 2.5f;
            //map[1] = 10f;

            if (map.TryGetValue(1, out float value))
            {
                Console.WriteLine(value);
            }
        }
    }
}