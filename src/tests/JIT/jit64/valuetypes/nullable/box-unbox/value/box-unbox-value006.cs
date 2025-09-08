// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ushort)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ushort?)o, Helper.Create(default(ushort)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ushort? s = Helper.Create(default(ushort));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


