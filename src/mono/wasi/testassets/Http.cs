// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

// keep in sync with src\mono\sample\wasi\http-p2\Program.cs
public static class WasiMainWrapper
{
    public static async Task<int> MainAsync(string[] args)
    {
        Console.WriteLine("Hello, Wasi Console!");
        for (int i = 0; i < args.Length; i ++)
            Console.WriteLine($"args[{i}] = {args[i]}");

        using HttpClient client = new();
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "dotnet WASI unit test");
        
        var query="https://corefx-net-http11.azurewebsites.net/Echo.ashx";
        var json = await client.GetStringAsync(query);

        Console.WriteLine();
        Console.WriteLine("GET "+query);
        Console.WriteLine();
        Console.WriteLine(json);

        return 42;
    }

    public static int Main(string[] args)
    {
        return PollWasiEventLoopUntilResolved((Thread)null!, MainAsync(args));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolved")]
        static extern int PollWasiEventLoopUntilResolved(Thread t, Task<int> mainTask);
    }
}
