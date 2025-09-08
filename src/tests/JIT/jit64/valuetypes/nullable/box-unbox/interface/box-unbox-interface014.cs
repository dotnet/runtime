// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((long)o, Helper.Create(default(long)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((long?)o, Helper.Create(default(long)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long? s = Helper.Create(default(long));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


