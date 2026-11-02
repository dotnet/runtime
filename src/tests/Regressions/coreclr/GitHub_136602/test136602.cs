// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_136602
{
    private const int InlineArrayLength = 256;

    private static class Witness
    {
        public static readonly object Value = new();
    }

    private static class StaticData
    {
        public static readonly LargeValue Value = new(Witness.Value);
    }

    private struct LargeValue
    {
        public LargeInlineArray Padding;
        public object Reference;

        public LargeValue(object reference)
        {
            Padding = default;
            Reference = reference;
        }
    }

    [InlineArray(InlineArrayLength)]
    private struct LargeInlineArray
    {
        private nuint _element0;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.False(IsReferenceStale());

        for (int i = 0; i < 3; i++)
        {
            AllocateGarbage();
            GC.Collect();
        }

        Assert.False(IsReferenceStale());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsReferenceStale() => !ReferenceEquals(StaticData.Value.Reference, Witness.Value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateGarbage()
    {
        object[] garbage = new object[100_000];

        for (int i = 0; i < garbage.Length; i++)
        {
            garbage[i] = new object();
        }

        GC.KeepAlive(garbage);
    }
}
