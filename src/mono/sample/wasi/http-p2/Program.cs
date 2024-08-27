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
    public static async Task<int> MainAsync(string[] args)
    {
        await Task.Delay(100);
        GC.Collect(); // test that Pollable->Task is not collected until resolved

        using HttpClient impatientClient = new();
        impatientClient.DefaultRequestHeaders.Add("User-Agent", "dotnet WASI unit test");
        impatientClient.Timeout = TimeSpan.FromMilliseconds(10);
        try {
            await impatientClient.GetAsync("https://corefx-net-http11.azurewebsites.net/Echo.ashx?delay10sec");
            throw new Exception("request should have timed out");
        } catch (TaskCanceledException) {
            Console.WriteLine("1st impatientClient was canceled as expected");
            // The /slow-hello endpoint takes 10 seconds to return a
            // response, whereas we've set a 100ms timeout, so this is
            // expected.
        }

        var cts = new CancellationTokenSource(10);
        try {
            await impatientClient.GetAsync("https://corefx-net-http11.azurewebsites.net/Echo.ashx?delay10sec", cts.Token);
            throw new Exception("request should have timed out");
        } catch (TaskCanceledException tce) {
            if (cts.Token != tce.CancellationToken)
            {
                throw new Exception("Different CancellationToken");
            }
            Console.WriteLine("2nd impatientClient was canceled as expected");
            // The /slow-hello endpoint takes 10 seconds to return a
            // response, whereas we've set a 100ms timeout, so this is
            // expected.
        }

        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "dotnet WASI unit test");
        
        var query="https://corefx-net-http11.azurewebsites.net/Echo.ashx";
        var json = await client.GetStringAsync(query);

        Console.WriteLine();
        Console.WriteLine("GET "+query);
        Console.WriteLine();
        Console.WriteLine(json);

        GC.Collect();
        return 0;
    }

    public static int Main(string[] args)
    {
        return PollWasiEventLoopUntilResolved((Thread)null!, MainAsync(args));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolved")]
        static extern int PollWasiEventLoopUntilResolved(Thread t, Task<int> mainTask);
    }

}
