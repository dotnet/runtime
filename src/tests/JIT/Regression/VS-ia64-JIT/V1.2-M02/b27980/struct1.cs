// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public String str;
}


public class Test_struct1
{
    static void c(S s1)
    {
        GC.Collect();
        Console.WriteLine(s1.str);
        GC.Collect();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S sM;

        sM.str = "test";
        c(sM);
        return 100;
    }
}
