// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static int Main()
    {
        ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


