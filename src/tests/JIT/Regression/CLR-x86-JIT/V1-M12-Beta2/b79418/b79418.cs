// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class PInvokeTest
{

    static String foo = "foo";
    static String bar = "bar";

    [Fact]
    public static int TestEntryPoint()
    {
        if (foo == bar)
            foo = "foo";
        return 100;
    }
}
