// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);
    
    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");

        var culture = new CultureInfo("es-ES", false);
        // https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
        int result = culture.LCID == 0x1000 && culture.NativeName == "Invariant Language (Invariant Country)" ? 42 : 1;

        Console.WriteLine("Done!");
        await Task.Delay(5000);
        
        return result;
    }
}
