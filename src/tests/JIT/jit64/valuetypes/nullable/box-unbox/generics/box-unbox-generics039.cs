// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace box_unbox_generics039;
public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterface)(object)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterface?)(object)o, Helper.Create(default(ImplementTwoInterface)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


