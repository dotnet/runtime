// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((IntPtr)o, Helper.Create(default(IntPtr)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((IntPtr?)o, Helper.Create(default(IntPtr)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        IntPtr? s = Helper.Create(default(IntPtr));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


