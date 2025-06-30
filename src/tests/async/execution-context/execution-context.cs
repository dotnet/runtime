// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2ExecutionContext
{
    [Fact]
    public static void TestEntryPoint()
    {
        Test().GetAwaiter().GetResult();
    }

    public static AsyncLocal<long?> s_local = new AsyncLocal<long?>();
    private static async Task Test()
    {
        s_local.Value = 42;
        await ChangeThenReturn();
        Assert.Equal(42, s_local.Value);

        try
        {
            s_local.Value = 43;
            await ChangeThenThrow();
        }
        catch (Exception)
        {
            Assert.Equal(43, s_local.Value);
        }

        s_local.Value = 44;
        await ChangeThenReturnInlined();
        Assert.Equal(44, s_local.Value);

        try
        {
            s_local.Value = 45;
            await ChangeThenThrowInlined();
        }
        catch (Exception)
        {
            Assert.Equal(45, s_local.Value);
        }

        s_local.Value = 46;
        await Task.Yield();
        Assert.Equal(46, s_local.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeThenThrow()
    {
        s_local.Value = 123;
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ChangeThenReturn()
    {
        s_local.Value = 123;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ChangeThenThrowInlined()
    {
        s_local.Value = 123;
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ChangeThenReturnInlined()
    {
        s_local.Value = 123;
    }
}
