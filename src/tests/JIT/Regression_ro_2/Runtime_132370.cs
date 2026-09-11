// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// When a loop containing a switch is unrolled, the switch value becomes a constant in each
// unrolled copy. Switch peeling would then "steal" the wrong operand of the compare it creates
// (sequencing the compare can swap its operands when both are constants), so every copy of the
// switch ended up dispatching on the dominant case value instead of on its own value.

public class Runtime_132370
{
    private readonly struct V3
    {
        public readonly long X;
        public readonly long Y;
        public readonly long Z;

        public V3(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Get(V3 value, int axis) => axis switch
        {
            0 => value.X,
            1 => value.Y,
            2 => value.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(axis)),
        };
    }

    private static bool IsZero(V3 value)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            if (V3.Get(value, axis) != 0)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountZero(V3[] values)
    {
        int zero = 0;

        for (int i = 0; i < values.Length; i++)
        {
            if (IsZero(values[i]))
            {
                zero++;
            }
        }

        return zero;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // A mixture of values is needed so that case 0 is the dominant case in the profile.
        V3[] values = new V3[256];
        for (int i = 0; i < values.Length - 1; i++)
        {
            values[i] = new V3(1, 0, 0);
        }

        values[^1] = new V3(0, 0, 1);

        // None of the vectors is the zero vector, so this must always be 0. The bad code only
        // showed up once IsZero reached tier1 with profile data, so keep calling it, yielding in
        // between so that the background compilation has a chance to happen. In practice this
        // fails on the ~7th outer iteration when the JIT is broken.
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                Assert.Equal(0, CountZero(values));
            }

            Thread.Sleep(16);
        }
    }
}
