// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class test
{
    static byte by = 13;
    public static int Main(string[] args)
    {
        byte by1 = (byte)(by >> 1);
        byte by2 = (byte)(by >> 1);

        Console.WriteLine(by1);
        Console.WriteLine(by2);
        return 100;
    }
}
