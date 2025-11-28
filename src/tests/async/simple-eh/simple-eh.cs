// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2SimpleEH
{
    [Fact]
    public static void TestThrowAfterYield()
    {
        Task.Run(AsyncEntry).Wait();
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task AsyncEntry()
    {
        int result = await Handler();
        Assert.Equal(42, result);
    }

    public static async Task<int> Handler()
    {
        try
        {
            return await Throw(42);
        }
        catch (IntegerException ex)
        {
            return ex.Value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> Throw(int value)
    {
        await Task.Yield();
        throw new IntegerException(value);
    }

    public class IntegerException : Exception
    {
        public int Value;
        public IntegerException(int value) => Value = value;
    }

    [Fact]
    public static int TestDefinesIntButThrows()
    {
        return TestDefinesIntButThrowsAsync().GetAwaiter().GetResult();
    }

    private static async Task<int> TestDefinesIntButThrowsAsync()
    {
        int x = GetValue();
        try
        {
            x = await IntThrows();
        }
        catch
        {
        }

        return x;
    }

    private static async Task<int> IntThrows()
    {
        await Task.Yield();
        throw new Exception();
    }

    private struct S { public long A, B, C, D; }

    [Fact]
    public static int TestDefinesSButThrows()
    {
        return TestDefinesSButThrowsAsync().GetAwaiter().GetResult();
    }

    private static async Task<int> TestDefinesSButThrowsAsync()
    {
        S x = new S { A = GetValue(), B = GetValue() + 1, C = GetValue() + 2, D = GetValue() + 3 };
        try
        {
            x = await SThrows();
        }
        catch
        {
        }

        return (int)x.A;
    }

    private static async Task<S> SThrows()
    {
        await Task.Yield();
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetValue() => 100;
}
