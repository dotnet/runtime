// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public struct StructConstrainedBox<T> where T : struct
{
    private T _value;

    public StructConstrainedBox(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetValueHashCode() => _value.GetHashCode();
}

public static class StructConstrainedGenerics
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int StructConstrained<T>(T value) where T : struct => value.GetHashCode();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int UnmanagedConstrained<T>(T value) where T : unmanaged => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineableStructConstrained<T>(T value) where T : struct => value is char ? 1 : 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InlineableUnmanagedConstrained<T>(T value) where T : unmanaged => value is char ? 1 : 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallStructConstrained(int value) => StructConstrained(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallUnmanagedConstrained(int value) => UnmanagedConstrained(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallStructConstrainedBox(int value) => new StructConstrainedBox<int>(value).GetValueHashCode();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInlineableStructConstrained(char value) => InlineableStructConstrained(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInlineableUnmanagedConstrained(char value) => InlineableUnmanagedConstrained(value);
}
