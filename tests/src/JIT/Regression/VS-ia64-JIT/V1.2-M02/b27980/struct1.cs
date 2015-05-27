// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

struct S
{
    public String str;
}


class Test
{
    public static void c(S s1)
    {
        GC.Collect();
        Console.WriteLine(s1.str);
        GC.Collect();
    }

    public static int Main()
    {
        S sM;

        sM.str = "test";
        c(sM);
        return 100;
    }
}
