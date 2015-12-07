// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((MarshalAsStruct)(object)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((MarshalAsStruct?)(object)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static int Main()
    {
        MarshalAsStruct? s = Helper.Create(default(MarshalAsStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


