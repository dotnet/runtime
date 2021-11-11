// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

struct S
{
    public String str;
}

class Test_struct9
{
    public static void c(ref S s1, ref int i)
    {
        if (i < 10)
        {
            S sM;
            int i2;

            sM = s1;
            i2 = i + 1;
            c(ref sM, ref i2);
        }
        Console.WriteLine(s1.str);
    }

    public static int Main()
    {
        S sM;
        int i = 0;

        sM.str = "test";
        c(ref sM, ref i);
        return 100;
    }
}
