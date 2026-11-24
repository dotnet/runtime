// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CanonicalActivation;

public sealed class Marker;

public struct Enumerator<T> : IDisposable
{
    public T Current => default!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool MoveNext() => false;

    public void Dispose() => Test.DisposeCount++;
}

public sealed class Sequence
{
    public Enumerator<Marker> GetEnumerator() => default;
}

public static class Test
{
    internal static int DisposeCount;

    public static int Run()
    {
        DisposeCount = 0;
        foreach (Marker item in new Sequence())
        {
        }

        return DisposeCount;
    }
}
