// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_74774
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Problem(new() { FirstLngValue = 1, SecondLngValue = 2 }) != 3 ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Problem(MultiRegStruct a)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Call(MultiRegStruct a, MultiRegStruct b, MultiRegStruct c, MultiRegStruct d) => d.FirstLngValue + d.SecondLngValue;

        return Call(default, default, default, a);
    }

    struct MultiRegStruct
    {
        public long FirstLngValue;
        public long SecondLngValue;
    }
}
