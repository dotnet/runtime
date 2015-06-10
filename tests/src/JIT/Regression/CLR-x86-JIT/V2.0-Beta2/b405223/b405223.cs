// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
