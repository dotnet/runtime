// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

// OSR method has a local Vector<T>

public class VectorT
{
    // Vector<T> is not live into the loop, but created
    // during the loop.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int VectorCreatedInLoop(int from, int to, int cons)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            var vec = Vector.Create<int>(cons);
            result += Vector.Sum(vec);
        }
        return result;
    }

    // Vector<T> is live into the loop.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LiveVectorArgument(int from, int to, Vector<int> cons)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            // Force cons to live-in.
            var vec = Consume<Vector<int>>(cons);

            result += Vector.Sum(vec);
        }
        return result;
    }

    // Vector<T> is live into the loop from a local.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LiveVectorLocal(int from, int to, int cons)
    {
        Vector<int> vec = Vector.Create<int>(cons);
        int result = 0;
        for (int i = from; i < to; i++)
        {
            result += Vector.Sum(vec);
        }
        return result;
    }

    // Vector<T> is live into the loop from an address-exposed local, with outgoing stack arguments.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LiveVectorWithStackArguments(int from, int to, int cons)
    {
        Vector<int> vec = Vector.Create<int>(cons);
        int result = 0;
        for (int i = from; i < to; i++)
        {
            result += SumWithStackArguments(ref vec, 1, 2, 3, 4, 5, 6, 7, 8);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Consume<T>(T t)
    {
        return t;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithStackArguments(ref Vector<int> value, int a, int b, int c, int d,
        int e, int f, int g, int h)
    {
        return Vector.Sum(value) + a + b + c + d + e + f + g + h;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int cons = 42;
        int iterations = 100_000;
        int expected = iterations * Vector<int>.Count * cons;

        Assert.Equal(expected, VectorCreatedInLoop(0, iterations, cons));
        Assert.Equal(expected, LiveVectorArgument(0, iterations, Vector.Create<int>(cons)));
        Assert.Equal(expected, LiveVectorLocal(0, iterations, cons));
        Assert.Equal(expected + iterations * 36, LiveVectorWithStackArguments(0, iterations, cons));
    }
}
