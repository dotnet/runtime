// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;
using System.Runtime.CompilerServices;

public class WasmTestRunner : WasmApplicationEntryPoint
{
    protected int MaxParallelThreadsFromArg { get; set; }
    protected override int? MaxParallelThreads => RunInParallel ? MaxParallelThreadsFromArg : base.MaxParallelThreads;

#if TARGET_WASI
    public static int Main(string[] args)
    {
        return PollWasiEventLoopUntilResolved((Thread)null!, MainAsync(args));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolved")]
        static extern T PollWasiEventLoopUntilResolved<T>(Thread t, Task<T> mainTask);
    }


#else
    public static Task<int> Main(string[] args)
    {
        return MainAsync(args);
    }
#endif

    public static async Task<int> MainAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"No args given");
            return -1;
        }

        var runner = new WasmTestRunner();

        runner.TestAssembly = args[0];

        var excludedTraits = new List<string>();
        var includedTraits = new List<string>();
        var includedNamespaces = new List<string>();
        var includedClasses = new List<string>();
        var includedMethods = new List<string>();
        var untilFailed = false;

        for (int i = 1; i < args.Length; i++)
        {
            var option = args[i];
            switch (option)
            {
                case "-notrait":
                    excludedTraits.Add(args[i + 1]);
                    i++;
                    break;
                case "-trait":
                    includedTraits.Add(args[i + 1]);
                    i++;
                    break;
                case "-namespace":
                    includedNamespaces.Add(args[i + 1]);
                    i++;
                    break;
                case "-class":
                    includedClasses.Add(args[i + 1]);
                    i++;
                    break;
                case "-method":
                    includedMethods.Add(args[i + 1]);
                    i++;
                    break;
                case "-untilFailed":
                    untilFailed = true;
                    break;
                case "-threads":
                    runner.IsThreadless = false;
                    break;
                case "-parallelThreads":
                    runner.MaxParallelThreadsFromArg = Math.Max(1, int.Parse(args[i + 1]));
                    runner.RunInParallel = runner.MaxParallelThreadsFromArg > 1;
                    i++;
                    break;
                case "-verbosity":
                    runner.MinimumLogLevel = Enum.Parse<MinimumLogLevel>(args[i + 1]);
                    i++;
                    break;
                default:
                    throw new ArgumentException($"Invalid argument '{option}'.");
            }
        }

        runner.ExcludedTraits = excludedTraits;
        runner.IncludedTraits = includedTraits;
        runner.IncludedNamespaces = includedNamespaces;
        runner.IncludedClasses = includedClasses;
        runner.IncludedMethods = includedMethods;

        if (OperatingSystem.IsBrowser())
        {
            await Task.Yield();
        }

        var res = 0;
        do
        {
            res = await runner.Run();
        }
        while(res == 0 && untilFailed);

        return res;
    }

    public override Task RunAsync()
    {
        if (RunInParallel)
            Console.WriteLine($"Running in parallel with {MaxParallelThreads} threads.");

        return base.RunAsync();
    }
}
