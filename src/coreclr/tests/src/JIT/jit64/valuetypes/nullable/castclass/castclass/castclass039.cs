// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((ImplementTwoInterface)(ValueType)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementTwoInterface?)(ValueType)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static int Main()
    {
        ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


