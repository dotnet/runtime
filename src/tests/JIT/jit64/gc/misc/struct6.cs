// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

struct S
{
    public String str;
}

class Test
{
    public static void c(S s1, S s2, S s3, S s4, S s5)
    {
        Console.WriteLine(s1.str + s2.str + s3.str + s4.str + s5.str);
    }

    public static int Main()
    {
        S sM1, sM2, sM3, sM4, sM5;

        sM1.str = "test";
        sM2.str = "test2";
        sM3.str = "test3";
        sM4.str = "test4";
        sM5.str = "test5";
        c(sM1, sM2, sM3, sM4, sM5);
        return 100;
    }
}
