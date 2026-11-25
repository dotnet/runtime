// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_130248;

public static class Runtime_130248
{
    private static int s_value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ThrowIfZero(int value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Id(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M0()
    {
        // Based on the Fuzzlyn-generated repro in dotnet/runtime#130248.
        int value = 1;
        value--;

        try
        {
            s_value = Id(value);
        }
        catch (Exception) when (ThrowIfZero(value))
        {
            ThrowIfZero(value);
        }
    }

    [Fact]
    public static void TestEntryPoint() => M0();
}
