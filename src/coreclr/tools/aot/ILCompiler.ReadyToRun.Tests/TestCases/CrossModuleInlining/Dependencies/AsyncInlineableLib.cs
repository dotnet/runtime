using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncInlineableLib
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> GetValueAsync() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<string> GetStringAsync() => "Hello from async";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValueSync() => 42;
}
