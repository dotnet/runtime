// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_64208
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (Method0() != null)
        {
            return 101;
        }

        return 100;
    }

    public struct S1
    {
        public string string_0;
    }

    static S1 s_s1_32 = new S1();

    public static S1 LeafMethod15() => s_s1_32;

    public static string Method0()
    {
        return LeafMethod15().string_0;
    }
}
