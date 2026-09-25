using System;
using System.Runtime.CompilerServices;

public static class InlineableLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValue() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetString() => "Hello from InlineableLib";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Add(int a, int b) => a + b;
}

public class InlineableInstance
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineableInstance()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetValue() => 42;
}

public struct InlineableValueType
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetValue() => 42;
}
