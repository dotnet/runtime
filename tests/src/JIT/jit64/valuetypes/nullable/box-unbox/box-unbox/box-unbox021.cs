// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((EmptyStruct)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((EmptyStruct?)o, Helper.Create(default(EmptyStruct)));
    }

    private static int Main()
    {
        EmptyStruct? s = Helper.Create(default(EmptyStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


