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
    public static int F(int from, int to, int cons)
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
    public static int G(int from, int to, Vector<int> cons)
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Consume<T>(T t)
    {
        return t;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int cons = 42;
        int iterations = 1_000_000;
        int expected = iterations * Vector<int>.Count * cons;

        Assert.Equal(expected, F(0, iterations, cons));
        Assert.Equal(expected, G(0, iterations, Vector.Create<int>(cons)));
    }  
}
