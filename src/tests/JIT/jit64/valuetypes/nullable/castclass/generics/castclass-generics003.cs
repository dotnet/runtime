// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((byte)(ValueType)(object)o, Helper.Create(default(byte)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((byte?)(ValueType)(object)o, Helper.Create(default(byte)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        byte? s = Helper.Create(default(byte));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


