// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

public class P
{
    public static int Main()
    {
        int[,] x = new int[5, 5];
        for (int i = 0; i < 5; ++i)
            for (int j = 0; j < 5; ++j)
                x[i, j] = 7;

        Console.WriteLine("PASS");
        return 100;
    }
}