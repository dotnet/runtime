// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

class Test
{
    static int[] a = new int[10];

    static int[] A()
    {
        Console.WriteLine("A");
        return a;
    }

    static int F()
    {
        Console.WriteLine("F");
        return 1;
    }

    static int G()
    {
        Console.WriteLine("G");
        return 1;
    }

    public static int Main()
    {
        A()[F()] = G();
        return 100;
    }
}
