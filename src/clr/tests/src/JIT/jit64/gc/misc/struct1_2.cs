// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
