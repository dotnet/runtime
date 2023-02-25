// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias Library1;
extern alias Library2;

using System;

public static class Use
{
    public static int Main()
    {
        int result = 100;
        Console.WriteLine(Library1.Library.Name);
        if (Library1.Library.Name != null)
            result++;
        Console.WriteLine(Library2.Library.Name);
        if (Library2.Library.Name != null)
            result++;

        Console.WriteLine("Expected 2 static constructors to be run, actual={0}", result - 100);
        return result - 2;
    }
}
