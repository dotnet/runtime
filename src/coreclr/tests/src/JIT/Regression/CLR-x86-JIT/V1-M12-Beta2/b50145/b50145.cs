// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class test
{
    static float f1(float x, float y)
    {
        x -= x * y;
        return x;
    }
    public static int Main()
    {
        // expected: 2 - 2 * 3 = -4
        // with bug: 2 * (1 + 3) = 8

        float result = f1(2, 3);

        System.Console.WriteLine(result);

        if (result != -4.0)
        {
            System.Console.WriteLine("FAILED");
            return 1;
        }
        return 100;
    }
}
