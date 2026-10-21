// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class Async2ObjectsWithYields
{
    internal static async Task<int> A(object n)
    {
        // use string equality so that JIT would not think of hoisting "(int)n"
        // also to produce some amout of garbage
        if (n.ToString() != 0.ToString())
        {
            return await A((int)n - 1) + (int)n;
        }

        await Task.Yield();
        return 0;
    }

    private struct MixedElement
    {
        public object? First;
        public nint Scalar;
        public object? Second;
    }

    [InlineArray(2048)]
    private struct VeryLargeMixedElements
    {
        private MixedElement _element0;
    }

    private struct VeryLargeAsyncResult
    {
        public nint Prefix;
        public VeryLargeMixedElements Elements;
    }

    private static async Task<VeryLargeAsyncResult> GetVeryLargeResult(
        object first,
        object second)
    {
        await Task.Yield();
        VeryLargeAsyncResult result = default;
        result.Prefix = 42;
        result.Elements[0] = new MixedElement { First = first, Scalar = 101, Second = second };
        result.Elements[2047] = new MixedElement { First = second, Scalar = 202, Second = first };
        return result;
    }

    private static async Task<bool> ValidateVeryLargeResult(
        object first,
        object second)
    {
        VeryLargeAsyncResult large = await GetVeryLargeResult(first, second);
        GC.Collect();
        return (large.Prefix == 42) &&
            ReferenceEquals(large.Elements[0].First, first) &&
            ReferenceEquals(large.Elements[0].Second, second) &&
            ReferenceEquals(large.Elements[2047].First, second) &&
            ReferenceEquals(large.Elements[2047].Second, first);
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> AsyncEntry()
    {
        object result = 0;
        for (int i = 0; i < 20; i++)
        {
            var tsk = A(i);
            await Task.Yield();
            GC.Collect();
            result = await tsk;
        }

        object first = new();
        object second = new();
        if (!await ValidateVeryLargeResult(first, second))
        {
            return -1;
        }

        // the result should be 20 * (20 - 1) => 190
        return (int)result - 90;
    }

    [Fact]
    public static async Task<int> Test()
    {
        return (int)(await AsyncEntry());
    }
}
