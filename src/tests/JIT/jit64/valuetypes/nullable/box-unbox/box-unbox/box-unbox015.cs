// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((UIntPtr)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((UIntPtr?)o, Helper.Create(default(UIntPtr)));
    }

    private static int Main()
    {
        UIntPtr? s = Helper.Create(default(UIntPtr));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


