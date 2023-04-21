// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((MarshalAsStruct)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((MarshalAsStruct?)o, Helper.Create(default(MarshalAsStruct)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        MarshalAsStruct? s = Helper.Create(default(MarshalAsStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


