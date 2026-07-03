// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace castclass_generics041;
public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        ImplementTwoInterfaceGen<int>? s = Helper.Create(default(ImplementTwoInterfaceGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


