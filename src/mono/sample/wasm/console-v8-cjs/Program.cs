// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Threading.Tasks;

public class Test
{
    static Vector128<byte> TestSIMD()
    {
        return WasmBase.Constant(0xff11ff22ff33ff44, 0xff55ff66ff77ff88);
    }

    public static async Task<int> Main(string[] args)
    {
        var v = TestSIMD();
        await Task.Delay(1);
        Console.WriteLine("Hello World!");
        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine($"args[{i}] = {args[i]}");
        }
        return 0;
    }
}
