// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool flag = false;
        for (int i = 1; i <= -1; i++)
        {
            flag = true;
        }

        if (flag)
        {
            Console.WriteLine("FAIL"); return 101;
        }
        else
        {
            Console.WriteLine("PASS"); return 100;
        }
    }
}
