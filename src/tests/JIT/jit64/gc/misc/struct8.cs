// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public String str;
}

public class Test_struct8
{
    private static void c(ref S s1, ref int i)
    {
        if (i < 10)
        {
            i++;
            c(ref s1, ref i);
        }
        Console.WriteLine(s1.str);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        S sM;
        int i = 0;

        sM.str = "test";
        c(ref sM, ref i);
        return 100;
    }
}
