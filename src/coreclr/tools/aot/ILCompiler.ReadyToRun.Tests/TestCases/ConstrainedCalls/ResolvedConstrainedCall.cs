// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace ConstrainedCalls;

public sealed class Marker { }

// foreach over a struct enumerator emits 'constrained. Enumerator<Marker> callvirt IDisposable::Dispose',
// which crossgen2 resolves to the shared Enumerator<__Canon>.Dispose body.
public struct Enumerator<T> : IDisposable
{
    public T Current => default!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool MoveNext() => false;

    public void Dispose() => ResolvedConstrainedCall.DisposeCount++;
}

public struct NoInliningEnumerator<T> : IDisposable
{
    public T Current => default!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool MoveNext() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Dispose() => ResolvedConstrainedCall.DisposeCount++;
}

public sealed class Sequence
{
    public Enumerator<Marker> GetEnumerator() => default;
}

public sealed class NoInliningSequence
{
    public NoInliningEnumerator<Marker> GetEnumerator() => default;
}

public static class ResolvedConstrainedCall
{
    public static int DisposeCount;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Iterate()
    {
        foreach (Marker item in new Sequence()) { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IterateNoInlining()
    {
        foreach (Marker item in new NoInliningSequence()) { }
    }
}
