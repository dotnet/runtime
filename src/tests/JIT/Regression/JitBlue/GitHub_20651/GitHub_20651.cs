// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class X
{
    static string s = "hello, world";

    static string[,] G()
    {
        string[,] strings = new string[3,3];
        strings[0,0] = s;
        return strings;
    }

    // Ensure GTF_CALL flag is propagated to MD array accessor
    [Fact]
    public static int TestEntryPoint()
    {
        int c = G()[0,0].GetHashCode();
        int v = s.GetHashCode();
        return c == v ? 100 : -1;
    }
}
