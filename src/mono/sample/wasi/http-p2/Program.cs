// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

// keep in sync with src\mono\wasi\testassets\Http.cs
public static class WasiMainWrapper
{
    public static async Task<int> MainAsync()
    {
        using HttpClient client = new();
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        
        var query="https://api.github.com/orgs/dotnet/repos?per_page=1";
        var json = await client.GetStringAsync(query);

        Console.WriteLine();
        Console.WriteLine("GET "+query);
        Console.WriteLine();
        Console.WriteLine(json);

        return 0;
    }

    public static int Main(string[] args)
    {
        var task = MainAsync();
        while (!task.IsCompleted)
        {
            CallDispatchWasiEventLoop((Thread)null!);
        }
        var exception = task.Exception;
        if (exception is not null)
        {
            throw exception;
        }
        return task.Result;

    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "DispatchWasiEventLoop")]
    private static extern void CallDispatchWasiEventLoop(Thread t);
}
