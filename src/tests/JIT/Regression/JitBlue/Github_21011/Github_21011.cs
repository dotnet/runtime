// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Xunit;

public class Test_Github_21011
{
    [Fact]
    public static int TestEntryPoint()
    {
        Test_Github_21011 test = new Test_Github_21011();
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
