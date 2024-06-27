// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public static class TestOutput
{
    public static void WriteLine(string message)
    {
        Console.WriteLine("TestOutput -> " + message);
    }

    public static void WriteLine(object message)
    {
        Console.Write("TestOutput -> ");
        Console.WriteLine(message);
    }
}
