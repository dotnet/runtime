// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


