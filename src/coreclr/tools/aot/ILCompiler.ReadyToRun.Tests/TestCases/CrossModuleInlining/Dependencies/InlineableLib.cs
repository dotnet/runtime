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
