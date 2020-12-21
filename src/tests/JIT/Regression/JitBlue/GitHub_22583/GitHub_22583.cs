// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

// Test case where a type-equvalent delegate is assigned

class X
{
    static int F() => 3;

    public static int Main()
    {
        XD x = F;
        XD y = Lib.GetDelegate();
        return x() + y() + 64;
    }
    
}
