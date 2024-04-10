// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

public partial class Program
{
    private static TaskCompletionSource<int>? TestTcs;

    public static async Task Main()
    {
        Program.TestTcs = new TaskCompletionSource<int>();

        string url = GetUrl();
        string transport = GetQueryParam("transport");
        string message = GetQueryParam("message");
        SignalRTest test = new();
        await test.Run(url, transport, message);

        int delayInMin = 2;
        await Task.WhenAny(
            Program.TestTcs!.Task,
            Task.Delay(TimeSpan.FromMinutes(delayInMin)));

        if (!Program.TestTcs!.Task.IsCompleted)
            throw new TimeoutException($"Test timed out after waiting {delayInMin} minutes for process to exit.");

        int result = Program.TestTcs!.Task.Result;
        if (result != 0)
            throw new Exception($"WasmBrowser finished with non-success code: {result}");
    }

    [JSImport("Program.getQueryParam", "main.js")]
    private static partial string GetQueryParam(string prameterName);

    [JSImport("Program.getUrl", "main.js")]
    private static partial string GetUrl();

    public static void SetResult(int value) => Program.TestTcs?.SetResult(value);
}
