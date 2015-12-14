// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct)(ValueType)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)(ValueType)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static int Main()
    {
        WithMultipleGCHandleStruct? s = Helper.Create(default(WithMultipleGCHandleStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


