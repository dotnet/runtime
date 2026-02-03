// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        TestOutput.WriteLine("Hello from WasmBrowserRunMainOnly!");
        await Task.Delay(1);
        return 0;
    }
}