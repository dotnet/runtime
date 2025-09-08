// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((int)o, Helper.Create(default(int)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((int?)o, Helper.Create(default(int)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int? s = Helper.Create(default(int));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


