// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((char)o, Helper.Create(default(char)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((char?)o, Helper.Create(default(char)));
    }

    private static int Main()
    {
        char? s = Helper.Create(default(char));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


