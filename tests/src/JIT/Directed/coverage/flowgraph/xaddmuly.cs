// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
internal class Test
{
    static public float x = 0x8000;
    static public float y = 0xF;
    public static int Main()
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
