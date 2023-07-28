// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((float)(ValueType)(object)o, Helper.Create(default(float)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((float?)(ValueType)(object)o, Helper.Create(default(float)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float? s = Helper.Create(default(float));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


