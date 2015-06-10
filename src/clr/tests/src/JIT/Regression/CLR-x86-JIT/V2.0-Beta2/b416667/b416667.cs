// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
