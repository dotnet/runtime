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
