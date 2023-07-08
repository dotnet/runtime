// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((uint)o, Helper.Create(default(uint)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((uint?)o, Helper.Create(default(uint)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        uint? s = Helper.Create(default(uint));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


