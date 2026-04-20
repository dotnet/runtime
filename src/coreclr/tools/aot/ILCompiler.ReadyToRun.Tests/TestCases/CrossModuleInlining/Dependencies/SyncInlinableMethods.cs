using System;
using System.Runtime.CompilerServices;

public static class SyncInlinableMethods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValue() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetString() => "Hello from SyncInlinableMethods";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Add(int a, int b) => a + b;
}
