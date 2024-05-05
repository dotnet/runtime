// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static unsafe class Runtime_101034
{
    [Fact]
    public static void Test()
    {
        StructWithTwoShorts x = new() { ShortOne = -1, ShortTwo = -1 };
        StructWithThreeBytes y = default;
        Problem(&x, &y);
        Assert.True(y.ByteThree == 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static StructWithTwoShorts Problem(StructWithTwoShorts* s, StructWithThreeBytes* p)
    {
        StructWithTwoShorts y = *s;

        int z = 255;
        ForceILLocal(&z);
        if (z == 255)
        {
            y.ShortOne = 255;
        }
        else
        {
            y.ShortOne = 1;
        }

        if (p != null)
        {
            int x = 255;
            *p = Unsafe.As<int, StructWithThreeBytes>(ref x);
        }

        return y;
    }

    private static void ForceILLocal(void* x) { }

    struct StructWithTwoShorts
    {
        public short ShortOne;
        public short ShortTwo;
    }

    struct StructWithThreeBytes
    {
        public byte ByteOne;
        public byte ByteTwo;
        public byte ByteThree;
    }
}
