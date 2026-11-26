// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using ConstrainedCalls.External;

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

// Instantiations over types from an assembly outside the version bubble.
public sealed class ExternalMarkerSequence
{
    public Enumerator<ExternalMarker> GetEnumerator() => default;
}

public sealed class ExternalValueSequence
{
    public Enumerator<ExternalValue> GetEnumerator() => default;
}

public interface IGenericMethod
{
    void Invoke<U>();
}

// 'constrained. GenericMethodStruct callvirt IGenericMethod::Invoke<string>' resolves to the shared
// GenericMethodStruct.Invoke<__Canon> body, which takes its method dictionary as an instantiation argument.
public struct GenericMethodStruct : IGenericMethod
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Invoke<U>() => ResolvedConstrainedCall.DisposeCount++;
}

public static class ResolvedConstrainedCall
{
    public static int DisposeCount;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvokeConstrained<T>(T value) where T : IGenericMethod => value.Invoke<string>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InvokeGenericMethod() => InvokeConstrained(new GenericMethodStruct());

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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvokeConstrainedExternal<T>(T value) where T : IGenericMethod => value.Invoke<ExternalMarker>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InvokeGenericMethodExternal() => InvokeConstrainedExternal(new GenericMethodStruct());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IterateExternalMarker()
    {
        foreach (ExternalMarker item in new ExternalMarkerSequence()) { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IterateExternalValue()
    {
        foreach (ExternalValue item in new ExternalValueSequence()) { }
    }
}
