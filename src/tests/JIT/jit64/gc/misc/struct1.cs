// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

struct S
{
    public String str;
}

class Test_struct1
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
