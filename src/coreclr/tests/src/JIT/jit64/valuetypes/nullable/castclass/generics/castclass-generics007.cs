// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((int)(ValueType)(object)o, Helper.Create(default(int)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((int?)(ValueType)(object)o, Helper.Create(default(int)));
    }

    private static int Main()
    {
        int? s = Helper.Create(default(int));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


