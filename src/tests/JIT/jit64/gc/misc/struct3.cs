// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public String str;
}

public class Test_struct3
{
    private static void c(S s1, S s2, S s3)
    {
        Console.WriteLine(s1.str + s2.str + s3.str);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S sM;

        sM.str = "test";
        c(sM, sM, sM);
        return 100;
    }
}
