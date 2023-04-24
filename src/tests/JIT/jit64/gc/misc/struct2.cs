// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public String str;
}

public class Test_struct2
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
        c(sM, sM2);
        return 100;
    }
}
