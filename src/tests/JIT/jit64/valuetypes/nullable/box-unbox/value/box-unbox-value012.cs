// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((double)o, Helper.Create(default(double)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((double?)o, Helper.Create(default(double)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        double? s = Helper.Create(default(double));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


