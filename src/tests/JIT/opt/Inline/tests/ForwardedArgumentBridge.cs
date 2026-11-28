// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public sealed class ForwardedArgumentBox<T> { }
public enum ForwardedArgumentMode : byte { First }
public sealed class ForwardedArgumentTag { }

public static class ForwardedArgumentBridge
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type Wrap<T>() => typeof(ForwardedArgumentBox<T>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Kept<T>() => typeof(T);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type Named<T>() => Kept<Action<T, TimeSpan, ForwardedArgumentMode, ForwardedArgumentTag>>();
}
