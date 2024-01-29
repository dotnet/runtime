// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;
using Xunit.Sdk;

public class WasmTestRunner : WasmApplicationEntryPoint
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine($"No args given");
            return -1;
        }

        var testAssembly = args[0];
        var excludedTraits = new List<string>();
        var includedTraits = new List<string>();
        var includedNamespaces = new List<string>();
        var includedClasses = new List<string>();
        var includedMethods = new List<string>();
        var backgroundExec = false;
        var isThreadless = true;

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
                case "-backgroundExec":
                    backgroundExec = true;
                    break;
                case "-threads":
                    isThreadless = false;
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
            IncludedMethods = includedMethods,
            IsThreadless = isThreadless
        };

        if (OperatingSystem.IsBrowser())
        {
            await Task.Yield();
        }

        if (backgroundExec)
        {
            return await Task.Run(() => runner.Run());
        }

        return await runner.Run();
    }
}
