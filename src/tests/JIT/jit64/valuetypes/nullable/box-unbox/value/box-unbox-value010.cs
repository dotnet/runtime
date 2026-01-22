// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace box_unbox_value010;
public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ulong)o, Helper.Create(default(ulong)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ulong?)o, Helper.Create(default(ulong)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        ulong? s = Helper.Create(default(ulong));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


