// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

public class Bug
{
    public static int Main(String[] arguments)
    {
        double d1 = 0;
        d1 = -d1;
        Console.WriteLine(1 / d1);
        Object d2 = d1;
        double d3 = (double)d2;
        Console.WriteLine(1 / d3);
        return 100;
    }
}
