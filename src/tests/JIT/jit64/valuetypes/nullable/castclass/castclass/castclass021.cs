// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace castclass021;
public class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((EmptyStruct)(ValueType)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((EmptyStruct?)(ValueType)o, Helper.Create(default(EmptyStruct)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        EmptyStruct? s = Helper.Create(default(EmptyStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


