// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static int Main()
    {
        NotEmptyStructConstrainedGen<int>? s = Helper.Create(default(NotEmptyStructConstrainedGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


