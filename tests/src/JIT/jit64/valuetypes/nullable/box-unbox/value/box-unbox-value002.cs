// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((bool)o, Helper.Create(default(bool)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((bool?)o, Helper.Create(default(bool)));
    }

    private static int Main()
    {
        bool? s = Helper.Create(default(bool));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


