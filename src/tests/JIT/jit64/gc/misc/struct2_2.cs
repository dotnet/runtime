// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
#pragma warning disable 0414
    public String str2;
#pragma warning restore 0414
    public String str;
}

public class Test_struct2_2
{
    private static void c(S s1, S s2)
    {
        Console.WriteLine(s1.str + s2.str);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S sM, sM2;

        sM.str = "test";
        sM2.str = "test2";
        sM.str2 = "test";
        sM2.str2 = "test2";
        c(sM, sM2);
        return 100;
    }
}
