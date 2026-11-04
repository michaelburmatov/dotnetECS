# dotnetECS

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-6.0%2B-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-10.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)

High-performance Entity Component System (ECS) implementation for .NET with zero-allocation hot paths and manual memory management.

---

## 🚀 Features

- **Zero-allocation hot paths** — `Entity` and `ManagerECS` implemented as value types (`struct`)
- **Unmanaged component constraint** — eliminates GC overhead for component data
- **Native memory structures** — custom `NativeList<T>`, `NativeDictionary<TKey, TValue>`, `NativeHashSet<T>`
- **GCHandle-based memory management** — direct control over heap allocations
- **Reflection-based system discovery** — automatic loading and registration of systems
- **Aggressive inlining** — `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on critical paths
- **System pooling** — singleton `PoolSystems` caches system instances
- **Proper resource cleanup** — full `IDisposable` implementation across the stack

---

## 🏗️ Architecture

World (IDisposable)
├── ManagerECS (struct) — component storage & access
│ ├── NativeDictionary<IntPtr, IntPtr> — type-handle → container mapping
│ └── ConnectorsContainer — entity ↔ component relationships
├── NativeHashSet<Entity> — entity registry
├── List<ISystemUpdate> — update-loop systems
└── List<ISystemEnter> — entity-enter systems
ManagedContainer<T> where T : unmanaged
└── NativeList<ComponentDataBucket<T>>
└── ComponentDataBucket<T> { isActive, componentData, entityParentIndex }

### Core Concepts

| Concept | Type | Description |
|---------|------|-------------|
| **Entity** | `struct` | Lightweight wrapper around `uint id` |
| **Component** | `struct where T : unmanaged` | Pure data, no behavior |
| **System** | `class : ISystemUpdate` | Logic that processes components |
| **World** | `class : IDisposable` | Top-level coordinator |

---

## 📦 Quick Start

### Clone

```bash
git clone https://github.com/michaelburmatov/dotnetECS.git
cd dotnetECS
```

### Define Components

```csharp
using ECS.App;
using ECS.App.ISystem;

public struct Position
{
    public float X, Y;
}

public struct Velocity
{
    public float DX, DY;
}
```

### Define a System

```csharp
public class MovementSystem : ISystemUpdate
{
    public void Update(ref ManagerECS manager)
    {
        if (!manager.TryGetCount<Position>(out var count))
            return;

        for (uint i = 0; i < count; i++)
        {
            if (manager.TryGetComponentData<Position>(out var pos, i))
            {
                // Mutate via reference for performance
                ref var posRef = ref manager.GetDataRef<Position>(i);
                posRef.X += posRef.DX;  // assuming Velocity at same index
                posRef.Y += posRef.DY;
            }
        }
    }
}
```

> **Note:** `MovementSystem` is auto-discovered by `PoolSystems` via reflection — no manual registration needed.

### Run a World

```csharp
using var world = new World();

world.TryAddSystem<MovementSystem>();

var e1 = world.CreateEntity(
    new Position { X = 0, Y = 0 },
    new Velocity { DX = 1, DY = 1 }
);

var e2 = world.CreateEntity(
    new Position { X = 10, Y = 10 },
    new Velocity { DX = -1, DY = 0 }
);

while (true)
{
    world.Update();
}
```

---

## ⚡ Performance Characteristics

| Operation | Complexity | Notes |
|-----------|------------|-------|
| Entity creation | O(1) | Pre-allocated capacity |
| Component access | O(1) | Type-handle lookup in `NativeDictionary` |
| System update | O(n × m) | n systems × m entities |
| Heap allocations (hot path) | ≈ 0 | `struct` types avoid boxing |
| GC pressure | Near-zero | `unmanaged` constraint keeps data off GC heap |

---

## 🔧 Technical Details

### Memory Management

- `ManagerECS` stores container pointers via `GCHandle.Alloc(container, GCHandleType.Normal)`
- Type identity resolved through `typeof(T).TypeHandle.Value` — zero string allocations
- All native structures implement `IDisposable` and must be cleaned up via `World.Dispose()`

### System Auto-Discovery

`PoolSystems` (singleton) scans all loaded assemblies on startup:

```csharp
// Simplified flow
var types = assembly.GetTypes()
    .Where(t => !t.IsAbstract)
    .Where(t => typeof(ISystemUpdate).IsAssignableFrom(t));
```

Systems are instantiated once and cached — zero reflection cost at runtime.

### Type Safety

- Compile-time `where T : unmanaged` enforces blittable components
- Safe access via `TryGet*` methods returning `bool` success flags
- Direct access via `GetDataRef<T>(index)` returning `ref T` for performance-critical code

---

## 📋 Requirements

- .NET 6.0+
- C# 10.0+
- `System.Reflection` (for auto-discovery)

---

## 🤝 Contributing

This is a personal pet project exploring ECS architecture patterns in .NET. Contributions are welcome:

- 🐛 **Bugs** — open an issue with reproduction steps
- 💡 **Features** — open an issue for discussion before implementing
- ⚡ **Optimizations** — PRs with benchmarks are highly appreciated

---

## 📝 License

MIT — see [LICENSE](LICENSE) for details.

---

## 👤 Author

**Michael Burmatov**

- 📧 Email: [michaelburmatov@gmail.com](mailto:michaelburmatov@gmail.com)
- 💬 Telegram: [@michaelburmatov](https://t.me/michaelburmatov)
- 🐙 GitHub: [@michaelburmatov](https://github.com/michaelburmatov)

---

## 🇷🇺 Краткое описание на русском

Собственная реализация паттерна **Entity Component System** для .NET с фокусом на производительность.

**Ключевые особенности:**
- Нулевые аллокации в горячих путях (`struct`-типы для `Entity` и `ManagerECS`)
- Ограничение `unmanaged` на компоненты — минимум нагрузки на GC
- Собственные нативные коллекции (`NativeList`, `NativeDictionary`, `NativeHashSet`)
- Управление памятью через `GCHandle` и `TypeHandle`
- Автообнаружение систем через `Reflection`
- Пул систем на базе `Singleton`

Проект создан для глубокого понимания ECS-архитектуры и низкоуровневой оптимизации в C#.
```
