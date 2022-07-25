// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.XHarness.TestRunners.Xunit;

public class SimpleWasmTestRunner : WasmApplicationEntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        var testAssembly = args[0];
        var excludedTraits = new List<string>();
        var includedTraits = new List<string>();
        var includedNamespaces = new List<string>();
        var includedClasses = new List<string>();
        var includedMethods = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            var option = args[i];
            switch (option)
            {
                case "-notrait":
                    excludedTraits.Add (args[i + 1]);
                    i++;
                    break;
                case "-trait":
                    includedTraits.Add (args[i + 1]);
                    i++;
                    break;
                case "-namespace":
                    includedNamespaces.Add (args[i + 1]);
                    i++;
                    break;
                case "-class":
                    includedClasses.Add (args[i + 1]);
                    i++;
                    break;
                case "-method":
                    includedMethods.Add (args[i + 1]);
                    i++;
                    break;
                default:
                    throw new ArgumentException($"Invalid argument '{option}'.");
            }
        }

        var runner = new SimpleWasmTestRunner()
        {
            TestAssembly = testAssembly,
            ExcludedTraits = excludedTraits,
            IncludedTraits = includedTraits,
            IncludedNamespaces = includedNamespaces,
            IncludedClasses = includedClasses,
            IncludedMethods = includedMethods
        };

        int result = await runner.Run();

        // if the execution doesn't finish on the main thread, the test runner
        // doesn't exit correctly and the test time outs
        await SwitchToMainThreadAsync();

        return result;
    }

    private static async Task SwitchToMainThreadAsync()
    {
        // TODO: is there a better way to do this? this works but it feels very hacky...
        // it would be best to ensure switching threads in the mono driver so that it works for _all_
        // WASM programs running on Mono
        while (Thread.CurrentThread.IsBackground)
        {
            await Task.Delay(1);
        }
    }
}
