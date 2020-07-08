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
    public static void c(ref S s1, ref int i)
    {
        if (i < 10)
        {
            i++;
            c(ref s1, ref i);
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
