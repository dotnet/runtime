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
        using HttpClient client = new();

        // send a request, but ignore its result
        try
        {
            await client.GetAsync("https://www.microsoft.com");
        }
        catch { }

        Type decompressionHandler = GetHttpType("System.Net.Http.DecompressionHandler");

        // DecompressionHandler should have been trimmed since AutomaticDecompression was not used
        if (decompressionHandler is not null)
        {
            return -1;
        }

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetHttpType(string name) =>
        typeof(HttpClient).Assembly.GetType(name, throwOnError: false);
}
