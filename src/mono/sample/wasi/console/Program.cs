// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

public class Test
{
    public static int Main(string[] args)
    {
        Console.WriteLine("");
        Console.WriteLine($"Hello World! Got {args.Length} args");
        foreach (string arg in args)
            Console.WriteLine ($"arg: {arg}");
        Console.WriteLine("");
        return 0;
    }
}
