// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> Main(string[] args)
    {
        await Task.Delay(1);
        Console.WriteLine("Hello From Wasm!");
        return 42;
    }
}