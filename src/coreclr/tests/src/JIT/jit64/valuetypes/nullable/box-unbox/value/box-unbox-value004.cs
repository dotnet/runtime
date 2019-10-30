// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((sbyte)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((sbyte?)o, Helper.Create(default(sbyte)));
    }

    private static int Main()
    {
        sbyte? s = Helper.Create(default(sbyte));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


