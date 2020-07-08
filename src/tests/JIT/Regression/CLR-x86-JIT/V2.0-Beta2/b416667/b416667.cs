// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
public class CMain
{
    public static int Count = 0;
    public static int Main(String[] args)
    {
        String s;
        s = Gen<String>.x;
        // we expect the Gen<T>.cctor to fire only once!
        if (1 == Count)
        {
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        else
        {
            Console.WriteLine("Test FAILED");
            return 101;
        }
    }
}

public class Gen<T>
{

    public static T x;
    static Gen()
    {
        CMain.Count++;
        Console.WriteLine("cctor.  Type: {0}", typeof(T).ToString());
    }
}
