// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

class Class1
{

    static int Main()
    {
        Console.WriteLine("Note that this is a test to verify that the implementation stays buggy");
        object o = new short[3];
        if (o is char[])
        {
            Console.WriteLine("Whidbey behavior");
            Console.WriteLine("Test FAILED");
            return 101;
        }
        else
        {
            Console.WriteLine("Everett behavior");
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
    }
}
