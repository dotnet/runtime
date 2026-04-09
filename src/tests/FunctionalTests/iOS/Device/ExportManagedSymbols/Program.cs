// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint="exposed_managed_method")]
    public static int ManagedMethod() => 42;

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Done!");
        await Task.Delay(5000);

        return 42;
    }
}
