// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((UIntPtr)(ValueType)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((UIntPtr?)(ValueType)o, Helper.Create(default(UIntPtr)));
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


