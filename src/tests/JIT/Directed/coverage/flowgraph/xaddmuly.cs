// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class Test_xaddmuly
{
    static public float x = 0x8000;
    static public float y = 0xF;
    [Fact]
    public static int TestEntryPoint()
    {
        x += y * x;
        x += y * x;
        Console.WriteLine("x: {0}, y: {1}", x, y);
        if ((x - 8388608) < 0.01 && (y - 15) < 0.01)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
