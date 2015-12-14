// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ushort?)o) == null;
    }

    private static int Main()
    {
        ushort? s = null;

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s) && BoxUnboxToNQGen(s) && BoxUnboxToQGen(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


