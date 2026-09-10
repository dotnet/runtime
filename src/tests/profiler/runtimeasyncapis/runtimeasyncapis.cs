// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

internal static class RuntimeAsyncApis
{
    private static readonly Guid ProfilerGuid = new("9A20F5D8-47B1-4E63-82CD-1F7690AB34E2");

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
        {
            return RunTest();
        }

        return ProfilerTestRunner.Run(
            profileePath: Assembly.GetExecutingAssembly().Location,
            testName: "RuntimeAsyncApis",
            profilerClsid: ProfilerGuid,
            profileeOptions: ProfileeOptions.OptimizationSensitive,
            envVars: new Dictionary<string, string>
            {
                ["DOTNET_RuntimeAsync"] = "1"
            });
    }

    private static async Task<int> Work()
    {
        await Task.Yield();
        throw new InvalidOperationException("Runtime Async profiler exception");
    }

    private static async Task<int> RunAsync()
    {
        try
        {
            await Work();
        }
        catch (InvalidOperationException)
        {
            return 100;
        }

        return 1;
    }

    private static int RunTest() => RunAsync().GetAwaiter().GetResult();
}
