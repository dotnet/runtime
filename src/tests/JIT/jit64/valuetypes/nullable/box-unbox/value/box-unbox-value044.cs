// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((WithOnlyFXTypeStruct)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((WithOnlyFXTypeStruct?)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    private static int Main()
    {
        WithOnlyFXTypeStruct? s = Helper.Create(default(WithOnlyFXTypeStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


