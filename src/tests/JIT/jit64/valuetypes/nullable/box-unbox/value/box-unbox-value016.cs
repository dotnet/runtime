// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace box_unbox_value016;
public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((Guid)o, Helper.Create(default(Guid)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((Guid?)o, Helper.Create(default(Guid)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        Guid? s = Helper.Create(default(Guid));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


