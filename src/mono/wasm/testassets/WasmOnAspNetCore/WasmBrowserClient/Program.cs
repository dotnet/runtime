// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Shared;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Expected url origin and href passed as arguments");

        SignalRTest test = new();
        Console.WriteLine($"arg0: {args[0]}, arg1: {args[1]}");
        int result = await test.Run(origin: args[0], fullUrl: args[1]);
        if (result != 0)
            throw new Exception($"WasmBrowser finished with non-success code: {result}");
    }
}
