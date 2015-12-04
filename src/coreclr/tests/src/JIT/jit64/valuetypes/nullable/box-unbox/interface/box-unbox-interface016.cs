// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((float)o, Helper.Create(default(float)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((float?)o, Helper.Create(default(float)));
    }

    private static int Main()
    {
        float? s = Helper.Create(default(float));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


