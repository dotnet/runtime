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
    public static void c(S s1)
    {
        Console.WriteLine(s1.str);
    }

    public static int Main()
    {
        S sM;

        sM.str = "test";
        sM.str2 = "";
        c(sM);
        return 100;
    }
}
