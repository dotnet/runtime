// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_64375
{
    [Fact]
    public static int TestEntryPoint()
    {
        var a = new StructWithFloats { FloatOne = 1, FloatThree = 2 };

        return Problem(&a) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(StructWithFloats* p1)
    {
        var a = new Vector2(p1->FloatOne, p1->FloatTwo);
        var b = new Vector2(p1->FloatThree, p1->FloatFour);

        return a == b;
    }

    struct StructWithFloats
    {
        public float FloatOne;
        public float FloatTwo;
        public float FloatThree;
        public float FloatFour;
    }
}
