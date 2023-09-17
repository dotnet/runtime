// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

interface IFoo {
    void foo();
}

public class My
{
    [Fact]
    public static int TestEntryPoint()
    {
        try {
           IFoo s = null;
           s.foo();        
        }
        catch (NullReferenceException) {
            Console.WriteLine("Successfully caught a null reference exception.");
            return 100;
        }
        
        Console.WriteLine("Failed!!");
        return -1;
    }
}
