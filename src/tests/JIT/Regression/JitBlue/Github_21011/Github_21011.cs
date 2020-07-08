// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class Test
{
    public static int Main()
    {
        Test test = new Test();
        test.GetPair();
        return 100;
    }

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    internal KeyValuePair<uint, float>? GetPair()
    {
        KeyValuePair<uint,float>? result = new KeyValuePair<uint,float>?();
        return result;
    }
}
