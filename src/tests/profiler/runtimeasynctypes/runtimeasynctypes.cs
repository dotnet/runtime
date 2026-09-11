// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

internal static class RuntimeAsyncTypes
{
    private struct Payload
    {
        public int Value;
    }

    private static readonly Guid ProfilerGuid = new("7F4E1A63-92C5-4D81-AB37-560EC294F108");
    private static readonly TaskCompletionSource<int> Gate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
        {
            return RunTest();
        }

        return ProfilerTestRunner.Run(
            profileePath: Assembly.GetExecutingAssembly().Location,
            testName: "RuntimeAsyncTypes",
            profilerClsid: ProfilerGuid,
            envVars: new Dictionary<string, string>
            {
                ["DOTNET_RuntimeAsync"] = "1"
            });
    }

    private static async Task<int> Suspended(Payload[] payload)
    {
        await Gate.Task;
        return payload[0].Value;
    }

    private static int RunTest()
    {
        const int ContinuationCount = 32;
        Task<int>[] suspended = new Task<int>[ContinuationCount];
        for (int i = 0; i < suspended.Length; i++)
        {
            Payload[] payload = new Payload[16];
            payload[0].Value = i;
            suspended[i] = Suspended(payload);
        }

        for (int i = 0; i < 10; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        }

        Gate.SetResult(0);
        Task.WaitAll(suspended);
        return 100;
    }
}
