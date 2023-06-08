// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructQA)(ValueType)(object)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructQA?)(ValueType)(object)o, Helper.Create(default(NotEmptyStructQA)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        NotEmptyStructQA? s = Helper.Create(default(NotEmptyStructQA));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


