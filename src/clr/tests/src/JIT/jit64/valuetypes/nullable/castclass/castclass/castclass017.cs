// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((GCHandle)(ValueType)o, Helper.Create(default(GCHandle)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((GCHandle?)(ValueType)o, Helper.Create(default(GCHandle)));
    }

    private static int Main()
    {
        GCHandle? s = Helper.Create(default(GCHandle));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


