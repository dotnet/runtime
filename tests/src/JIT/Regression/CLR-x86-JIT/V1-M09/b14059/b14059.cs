// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class test
{
    static byte by = 13;
    public static int Main(string[] args)
    {
        byte by1 = (byte)(-by);

        Console.WriteLine(by1);
        return 100;
    }
}
