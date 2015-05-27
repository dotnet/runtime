// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Regression test for consecutive compare optimization.

using System;
internal class Foo
{
    public static int Main()
    {
        string s1 = "NonNull";
        string s2 = null;

        if ((s1 == null) == (s2 == null))
        {
            Console.WriteLine("Fail");
            return 1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
