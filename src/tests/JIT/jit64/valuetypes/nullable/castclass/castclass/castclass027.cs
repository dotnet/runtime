// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((NotEmptyStructGen<int>)(ValueType)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructGen<int>?)(ValueType)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        NotEmptyStructGen<int>? s = Helper.Create(default(NotEmptyStructGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


