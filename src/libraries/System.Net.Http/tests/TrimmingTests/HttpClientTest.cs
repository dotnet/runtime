// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync("https://www.microsoft.com");            
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine(result);

        const string quicDll = "System.Net.Quic.dll";
        var quicDllExists = File.Exists(Path.Combine(AppContext.BaseDirectory, quicDll));

        [SupportedOSPlatformGuard("linux")]
        [SupportedOSPlatformGuard("macOS")]
        [SupportedOSPlatformGuard("Windows")]
        private readonly bool _http3Enabled = (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        if (_http3Enabled)
        {
            Console.WriteLine($"Expected {quicDll} is {(quicDllExists ? "present - OK" : "missing - BAD")}.");
            return quicDllExists ? 100 : -1;
        }
        else
        {
            Console.WriteLine($"Unexpected {quicDll} is {(quicDllExists ? "present - BAD" : "missing - OK")}.");
            return quicDllExists ? -1 : 100;
        }
    }
}
