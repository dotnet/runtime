// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ByteE)o, Helper.Create(default(ByteE)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ByteE?)o, Helper.Create(default(ByteE)));
    }

    private static int Main()
    {
        ByteE? s = Helper.Create(default(ByteE));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


