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
