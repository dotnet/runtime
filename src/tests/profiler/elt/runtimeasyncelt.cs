// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Profiler.Tests;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TestLibrary;

internal static class RuntimeAsyncELT
{
    private static readonly Guid ProfilerGuid = new("D1C7A5B2-6E04-4D7F-9A31-2B84C6F0E915");

    public static int Main(string[] args)
    {
        if (!PlatformDetection.IsICorProfilerEnterLeaveHooksEnabled)
        {
            return 100;
        }

        if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
        {
            return RunTest();
        }

        return ProfilerTestRunner.Run(
            profileePath: Assembly.GetExecutingAssembly().Location,
            testName: "RuntimeAsyncELT",
            profilerClsid: ProfilerGuid,
            profileeOptions: ProfileeOptions.OptimizationSensitive,
            envVars: new Dictionary<string, string>
            {
                ["DOTNET_RuntimeAsync"] = "1"
            });
    }

    private static async Task<long> Work(int iterations)
    {
        long sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            await Task.Yield();
            sum += i;
        }
        return sum;
    }

    private static async Task WorkVoid(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await Task.Yield();
        }
    }

    private static async Task<double> WorkDouble(int iterations)
    {
        double sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            await Task.Yield();
            sum += i + 0.5;
        }
        return sum;
    }

    private static async Task<int> RunAsync()
    {
        long total = 0;
        for (int i = 0; i < 5; i++)
        {
            total += await Work(4);
        }

        await WorkVoid(2);
        double floatingResult = await WorkDouble(2);

        return total == 30 && floatingResult == 2.0 ? 100 : 1;
    }

    private static int RunTest() => RunAsync().GetAwaiter().GetResult();
}
