// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(IEmpty o)
    {
        return Helper.Compare((ImplementTwoInterface)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToQ(IEmpty o)
    {
        return Helper.Compare((ImplementTwoInterface?)o, Helper.Create(default(ImplementTwoInterface)));
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


