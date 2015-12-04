// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static int Main()
    {
        ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


