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

        await Task.Delay(100);
        GC.Collect(); // test that Pollable->Task is not collected until resolved

        var ctsDelay = new CancellationTokenSource(10);
        try {
            await Task.Delay(1000, ctsDelay.Token);
            throw new Exception("delay should have timed out");
        } catch (TaskCanceledException tce) {
            if (ctsDelay.Token != tce.CancellationToken)
            {
                throw new Exception("Different CancellationToken for delay");
            }
            Console.WriteLine("impatient delay was canceled as expected");
        }

        using HttpClient impatientClient1 = new();
        impatientClient1.DefaultRequestHeaders.Add("User-Agent", "dotnet WASI unit test");
        impatientClient1.Timeout = TimeSpan.FromMilliseconds(10);
        try {
            await impatientClient1.GetAsync("https://corefx-net-http11.azurewebsites.net/Echo.ashx?delay10sec");
            throw new Exception("request should have timed out");
        } catch (TaskCanceledException) {
            Console.WriteLine("1st impatientClient was canceled as expected");
        }

        GC.Collect();

        using HttpClient impatientClient2 = new();
        impatientClient2.DefaultRequestHeaders.Add("User-Agent", "dotnet WASI unit test");
        var cts = new CancellationTokenSource(10);
        try {
            // in reality server side doesn't delay because it doesn't implement it. So 10ms is bit fragile.
            // TODO: remove this once we have real WASI HTTP library unit tests with local server loop in Xharness.
            // https://github.com/dotnet/xharness/pull/1244
            await impatientClient2.GetAsync("https://corefx-net-http11.azurewebsites.net/Echo.ashx?delay10sec", cts.Token);
            throw new Exception("request should have timed out");
        } catch (TaskCanceledException tce) {
            if (cts.Token != tce.CancellationToken)
            {
                throw new Exception("Different CancellationToken");
            }
            Console.WriteLine("2nd impatientClient was canceled as expected");
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

        return 42;
    }

    public static int Main(string[] args)
    {
        return PollWasiEventLoopUntilResolved((Thread)null!, MainAsync(args));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolved")]
        static extern T PollWasiEventLoopUntilResolved<T>(Thread t, Task<T> mainTask);
    }
}
