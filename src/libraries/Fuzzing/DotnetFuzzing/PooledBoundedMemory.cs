// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace DotnetFuzzing;

internal sealed class PooledBoundedMemory<T> : IDisposable where T : unmanaged
{
    // Default libFuzzer max_len for inputs is 4096.
    private const int MaxLength = 4096 * 2;

    private static readonly PooledBoundedMemory<T>?[] s_memoryWithPoisonBefore = new PooledBoundedMemory<T>?[MaxLength + 1];
    private static readonly PooledBoundedMemory<T>?[] s_memoryWithPoisonAfter = new PooledBoundedMemory<T>?[MaxLength + 1];

    private readonly BoundedMemory<T> _memory;
    private readonly PooledBoundedMemory<T>?[]? _pool;

    private PooledBoundedMemory(PooledBoundedMemory<T>?[]? pool, int elementCount, PoisonPagePlacement placement)
    {
        _pool = pool;
        _memory = BoundedMemory.Allocate<T>(elementCount, placement);
    }

    public BoundedMemory<T> InnerMemory => _memory;
    public Memory<T> Memory => _memory.Memory;
    public Span<T> Span => _memory.Span;

    public void Dispose()
    {
        if (_pool is null ||
            Interlocked.CompareExchange(ref _pool[_memory.Length], this, null) is not null)
        {
            _memory.Dispose();
        }
    }

    public static PooledBoundedMemory<T> Rent(int elementCount, PoisonPagePlacement placement)
    {
        if ((uint)elementCount >= MaxLength)
        {
            return new PooledBoundedMemory<T>(null, elementCount, placement);
        }

        PooledBoundedMemory<T>?[] pool = placement switch
        {
            PoisonPagePlacement.Before => s_memoryWithPoisonBefore,
            PoisonPagePlacement.After => s_memoryWithPoisonAfter,
            _ => throw new ArgumentOutOfRangeException(nameof(placement))
        };

        return
            Interlocked.Exchange(ref pool[elementCount], null) ??
            new PooledBoundedMemory<T>(pool, elementCount, placement);
    }

    public static PooledBoundedMemory<T> Rent(ReadOnlySpan<T> data, PoisonPagePlacement placement)
    {
        PooledBoundedMemory<T> memory = Rent(data.Length, placement);
        data.CopyTo(memory.Span);
        return memory;
    }
}
