// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class Program
{
    static int Main()
    {
        var map = new Dictionary<string, bool?> { { "foo", true } };
        return (Test(map) == true) ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool? Test(Dictionary<string, bool?> map)
    {
        return map["foo"];
    }
}
