// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ImplementAllInterface<int>)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ImplementAllInterface<int>?)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ImplementAllInterface<int>? s = Helper.Create(default(ImplementAllInterface<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


