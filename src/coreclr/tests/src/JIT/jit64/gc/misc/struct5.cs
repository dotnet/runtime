// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

struct S
{
    public String str;
}

class Test
{
    public static void c(S s1, S s2, S s3, S s4)
    {
        Console.WriteLine(s1.str + s2.str + s3.str + s4.str);
    }

    public static int Main()
    {
        S sM, sM2, sM3, sM4;

        sM.str = "test";
        sM2.str = "test2";
        sM3.str = "test3";
        sM4.str = "test4";
        c(sM, sM2, sM3, sM4);
        return 100;
    }
}
