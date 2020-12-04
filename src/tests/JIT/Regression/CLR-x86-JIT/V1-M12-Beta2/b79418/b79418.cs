// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class PInvokeTest
{

    static String foo = "foo";
    static String bar = "bar";

    public static int Main(String[] args)
    {
        if (foo == bar)
            foo = "foo";
        return 100;
    }
}
