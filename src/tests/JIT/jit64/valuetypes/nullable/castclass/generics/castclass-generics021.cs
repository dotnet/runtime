// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace castclass_generics021;
public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((EmptyStruct)(ValueType)(object)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((EmptyStruct?)(ValueType)(object)o, Helper.Create(default(EmptyStruct)));
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


