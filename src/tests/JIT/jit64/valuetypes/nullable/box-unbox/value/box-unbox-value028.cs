// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        NotEmptyStructConstrainedGen<int>? s = Helper.Create(default(NotEmptyStructConstrainedGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


