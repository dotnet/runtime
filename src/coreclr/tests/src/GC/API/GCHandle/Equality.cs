// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public class Equality
{
    public bool RunTest()
    {
        Object o = new Object();
        GCHandle gc = GCHandle.Alloc(o);
        GCHandle gc2 = GCHandle.Alloc(o);
        GCHandle gc3 = gc;

        if (gc.Equals(null))
        {
            Console.WriteLine("Equals null failed");
            return false;
        }

        if (gc.Equals(new Object()))
        {
            Console.WriteLine("Equals new Object failed");
            return false;
        }

        if (gc.Equals(gc2))
        {
            Console.WriteLine("Equals GCHandle 1 failed");
            return false;
        }

        if (!gc.Equals(gc3))
        {
            Console.WriteLine("Equals GCHandle 2 failed");
            return false;
        }


        if (gc == gc2)
        {
            Console.WriteLine("== GCHandle 1 failed");
            return false;
        }

        if (!(gc == gc3))
        {
            Console.WriteLine("== GCHandle 2 failed");
            return false;
        }

        if (gc.GetHashCode() == gc2.GetHashCode())
        {
            Console.WriteLine("GetHashCode 1 failed");
            return false;
        }

        if (gc.GetHashCode() != gc3.GetHashCode())
        {
            Console.WriteLine("GetHashCode 2 failed");
            return false;
        }


        if (!(gc != gc2))
        {
            Console.WriteLine("!= GCHandle 1 failed");
            return false;
        }

        if (gc != gc3)
        {
            Console.WriteLine("!= GCHandle 2 failed");
            return false;
        }

        return true;
    }


    public static int Main()
    {
        Equality e = new Equality();


        if (e.RunTest())
        {
            Console.WriteLine();
            Console.WriteLine("Test Passed");
            return 100;
        }

        Console.WriteLine();
        Console.WriteLine("Test Failed");
        return 1;
    }
}
