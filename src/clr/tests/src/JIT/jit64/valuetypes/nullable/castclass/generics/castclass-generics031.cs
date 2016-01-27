// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)(ValueType)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)(ValueType)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static int Main()
    {
        NotEmptyStructConstrainedGenQA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


