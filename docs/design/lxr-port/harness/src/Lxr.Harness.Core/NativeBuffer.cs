// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lxr.Harness.Core;

/// <summary>
/// A fixed-size buffer of unmanaged elements held <em>outside</em> the GC heap.
///
/// The harness pre-sizes its per-operation bookkeeping before a run so that the measured region
/// allocates nothing. That is necessary but not sufficient: a pre-sized <em>managed</em> array is
/// still charged against the very heap the experiment pins, so at a heap factor of 1.3x the
/// apparatus competes with the workload for the budget under study - and at realistic arrival rates
/// it exhausts it outright before the first operation runs. Instrumentation must not be charged to
/// the subject's heap budget, otherwise the heap-factor label does not describe what ran.
///
/// Elements are constrained to <c>unmanaged</c>, so the buffer contains no references and the
/// collector never has to trace it.
/// </summary>
public sealed unsafe class NativeBuffer<T> : IDisposable
    where T : unmanaged
{
    private void* _pointer;

    public NativeBuffer(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Length = length;
        ByteCount = (long)length * sizeof(T);
        // Zeroed, because callers read fields (notably Phase) that a run may never write.
        _pointer = length == 0 ? null : NativeMemory.AllocZeroed((nuint)length, (nuint)sizeof(T));
    }

    public int Length { get; }

    /// <summary>Bytes held off the GC heap, reported so a run's working set stays interpretable.</summary>
    public long ByteCount { get; }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is outside a buffer of {Length}.");
            }

            return ref Unsafe.Add(ref Unsafe.AsRef<T>(_pointer), index);
        }
    }

    /// <summary>
    /// The whole buffer as a span. Only valid while this instance is alive; callers must not let the
    /// span outlive a <see cref="Dispose"/>.
    /// </summary>
    public Span<T> AsSpan()
    {
        ObjectDisposedException.ThrowIf(_pointer is null && Length != 0, this);
        return new Span<T>(_pointer, Length);
    }

    public void Dispose()
    {
        if (_pointer is not null)
        {
            NativeMemory.Free(_pointer);
            _pointer = null;
        }

        GC.SuppressFinalize(this);
    }

    ~NativeBuffer()
    {
        if (_pointer is not null)
        {
            NativeMemory.Free(_pointer);
            _pointer = null;
        }
    }
}
