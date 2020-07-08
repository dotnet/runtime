// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

public class Test
{
    public static void M<T>(T t)
    {
        System.Type type = t.GetType();
        Console.WriteLine(type);
    }

    public static int Main()
    {
        M("Hello"); // Works fine
        M(3); // CLR crashes
        return 100;
    }
}
