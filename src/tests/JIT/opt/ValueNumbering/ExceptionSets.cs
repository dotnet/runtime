// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable CS0253 // Possible unintended reference comparison

public class ExceptionSets
{
    private struct ForwardingState
    {
        public int Padding;
        public int Value;
    }

    private struct LargeForwardingState
    {
        public int Padding;
        public int Value;
        public long First;
        public long Second;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            TestObjGetType(null, 0);
            return 101;
        }
        catch (NullReferenceException) { }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestObjGetType(object a, int i)
    {
        var fls = false;
        var c1 = i == 0;
        var c2 = c1;

        if (((a.GetType() == a) & fls) | (i == 0))
        {
            return true;
        }

        return c2;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, -1)]
    [InlineData(-7, 1)]
    [InlineData(7, 4)]
    [InlineData(7, 100)]
    [InlineData(int.MaxValue, 4)]
    public static unsafe void ForwardedLoadStaysAfterStore(int input, int count)
    {
        const int InitialValue = 999;
        var state = new ForwardingState { Padding = 13, Value = InitialValue };
        int iterations = Math.Max(count, 0);

        Assert.Equal(unchecked(input * iterations), SumThroughAlias(ref state, input, count));
        Assert.Equal(iterations > 0 ? input : InitialValue, state.Value);
        Assert.Equal(13, state.Padding);

        int[] values = { InitialValue, InitialValue };
        fixed (int* pointer = values)
        {
            Assert.Equal(unchecked(input * iterations), SumThroughIndex(pointer, 1, input, count));
        }
        Assert.Equal(InitialValue, values[0]);
        Assert.Equal(iterations > 0 ? input : InitialValue, values[1]);

        state.Value = InitialValue;
        Assert.Equal(unchecked(input * iterations * 3), SumThroughNestedLoops(ref state, input, count));
        Assert.Equal(iterations > 0 ? input : InitialValue, state.Value);

        state.Value = InitialValue;
        Assert.Equal(input, StoreInDoWhileAndRead(ref state, input, count));
        Assert.Equal(input, state.Value);

        var large = new LargeForwardingState { Padding = 13, Value = InitialValue };
        Assert.Equal(unchecked(input * iterations + (iterations > 0 ? input : InitialValue)),
            SumThroughByValueStruct(large, input, count));
        Assert.Equal(InitialValue, large.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public static unsafe void ForwardedLoadPreservesExceptions(int input)
    {
        Assert.Throws<NullReferenceException>(() => StoreAndRead(ref Unsafe.NullRef<int>(), input));

        int destination = 999;
        Assert.Throws<NullReferenceException>(() => StoreBeforeNullLoad(ref destination, input));
        Assert.Equal(input, destination);

        int value = 999;
        int* pointer = &value;
        Assert.Equal(input, StoreThroughSnapshots(&pointer, input));
        Assert.Equal(input, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumThroughAlias(ref ForwardingState state, int input, int count)
    {
        ref int alias = ref state.Value;
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            state.Value = input;
            sum += alias;
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumThroughByValueStruct(LargeForwardingState state, int input, int count)
    {
        ref int alias = ref Unsafe.AsRef(in state.Value);
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            alias = input;
            sum += alias;
        }

        return unchecked(sum + ReadValue(ref alias));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadValue(ref int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int SumThroughIndex(int* pointer, nint index, int input, int count)
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            pointer[index] = input;
            sum += pointer[index];
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumThroughNestedLoops(ref ForwardingState state, int input, int count)
    {
        ref int alias = ref state.Value;
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            state.Value = input;
            for (int j = 0; j < 3; j++)
            {
                sum += alias;
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StoreAndRead(ref int destination, int input)
    {
        destination = input;
        return destination;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StoreInDoWhileAndRead(ref ForwardingState state, int input, int count)
    {
        do
        {
            state.Value = input;
            count--;
        }
        while (count > 0);

        return state.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int StoreBeforeNullLoad(ref int destination, int input)
    {
        destination = input;
        return Unsafe.NullRef<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int StoreThroughSnapshots(int** source, int input)
    {
        int* first = *source;
        int* second = *source;
        *first = input;
        return *second;
    }
}
