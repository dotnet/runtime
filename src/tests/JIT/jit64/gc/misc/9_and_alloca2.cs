// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class T
{
    [Fact]
    public static int TestEntryPoint()
    {
        string s1 = "a";
        string s2 = "b";
        string s3 = "c";
        string s4 = "d";
        string s5 = "e";
        string s6 = "f";
        string s7 = "g";
        string s8 = "h";
        string s9 = "i";
        string s10 = "j";
        string s11 = "k";

        foo(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11);
        return 100;
    }

    internal static void foo(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10, string s11)
    {
        Console.WriteLine(s8);
        Console.WriteLine(s9);
        Console.WriteLine(s10 + s11);
    }
}
