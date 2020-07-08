// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

struct S
{
#pragma warning disable 0414
    public String str2;
#pragma warning restore 0414
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
        sM.str2 = "";
        sM2.str2 = "";
        sM3.str2 = "";
        sM4.str2 = "";
        c(sM, sM2, sM3, sM4);
        return 100;
    }
}
