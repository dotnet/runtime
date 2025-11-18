// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace HelloWorld;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World! " + DateTime.Now + " "+ args.Length);
        await Task.Delay(1000);
        Console.WriteLine("After Task.Delay() " + DateTime.Now);
    }
}
