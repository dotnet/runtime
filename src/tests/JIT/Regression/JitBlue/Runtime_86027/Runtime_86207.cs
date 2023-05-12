// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_86027
{
    [Fact]
    public static int TestRotateLeft1()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateLeft(firstOp[0], 1) != -2802470453797399680)
        {
            return 0;
        }

        return 100;
    }

    [Fact]
    public static int TestRotateLeft32()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateLeft(firstOp[0], 32) != 3886722753596608508)
        {
            return 0;
        }

        return 100;
    }

    [Fact]
    public static int TestRotateLeft33()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateLeft(firstOp[0], 33) != 7773445507193217016)
        {
            return 0;
        }

        return 100;
    }

    [Fact]
    public static int TestRotateRight1()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateRight(firstOp[0], 1) != 3911068404978037984)
        {
            return 0;
        }

        return 100;
    }

    [Fact]
    public static int TestRotateRight32()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateRight(firstOp[0], 32) != 3886722753596608508)
        {
            return 0;
        }

        return 100;
    }

    [Fact]
    public static int TestRotateRight33()
    {
        long[] firstOp = new long[] { 7822136809956075968, 1 };

        if (long.RotateRight(firstOp[0], 33) != 1943361376798304254)
        {
            return 0;
        }

        return 100;
    }
}
