// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementAllInterface<int>?)o) == null;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ImplementAllInterface<int>? s = null;

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s) && BoxUnboxToNQGen(s) && BoxUnboxToQGen(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


