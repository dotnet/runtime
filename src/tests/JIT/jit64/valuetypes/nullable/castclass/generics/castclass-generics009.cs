// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace castclass_generics009;
public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((long)(ValueType)(object)o, Helper.Create(default(long)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((long?)(ValueType)(object)o, Helper.Create(default(long)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        long? s = Helper.Create(default(long));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


