// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CrossModuleHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<int> SmallCrossModuleAsync(int x)
    {
        return x * 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task SmallCrossModuleAsyncVoid()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<int> SmallCrossModuleValueTaskAsync(int x)
    {
        return x * 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SmallCrossModuleSync(int x)
    {
        return x * 3;
    }
}
