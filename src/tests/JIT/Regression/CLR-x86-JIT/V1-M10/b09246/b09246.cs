// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
public class MyClass
{
    //extern modifier
    [Fact]
    public static int TestEntryPoint()
    {
        bool b = true;
        int exitcode = 0;
        b &= false;
        Console.WriteLine(b);
        b = b & false;
        Console.WriteLine(b);
        exitcode = b ? 1 : 100;
        b = false;
        Console.WriteLine(b);
        if (exitcode == 100)
            Console.WriteLine("Test passed.");
        else
            Console.WriteLine("Test failed.");
        return (exitcode);
    }
}
