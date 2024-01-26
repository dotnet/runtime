// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;
using Xunit.Sdk;

public class SimpleWasmTestRunner : WasmApplicationEntryPoint
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
                    break;
                default:
                    throw new ArgumentException($"Invalid argument '{option}'.");
            }
        }

        WasmApplicationEntryPointBase? runner = null;
        if (args.Contains("-threads"))
        {
            Console.WriteLine("Using MultiThreadedTestRunner");
            runner = new MultiThreadedTestRunner()
            {
                TestAssembly = testAssembly,
                ExcludedTraits = excludedTraits,
                IncludedTraits = includedTraits,
                IncludedNamespaces = includedNamespaces,
                IncludedClasses = includedClasses,
                IncludedMethods = includedMethods
            };
        }
        else
        {
            Console.WriteLine("Using SimpleWasmTestRunner");
            runner = new SimpleWasmTestRunner()
            {
                TestAssembly = testAssembly,
                ExcludedTraits = excludedTraits,
                IncludedTraits = includedTraits,
                IncludedNamespaces = includedNamespaces,
                IncludedClasses = includedClasses,
                IncludedMethods = includedMethods
            };
        }

        if (OperatingSystem.IsBrowser())
        {
            await Task.Yield();
        }
        if (backgroundExec)
        {
            await Task.Run(async () => await runner.RunAsync());
            return runner.LastRunHadFailedTests ? 1 : 0;
        }
        
        await runner.RunAsync();
        return runner.LastRunHadFailedTests ? 1 : 0;
    }
}

class MultiThreadedTestRunner : WasmApplicationEntryPointBase
{
    public virtual string TestAssembly { get; set; } = "";
    public virtual IEnumerable<string> ExcludedTraits { get; set; } = Array.Empty<string>();
    public virtual IEnumerable<string> IncludedTraits { get; set; } = Array.Empty<string>();
    public virtual IEnumerable<string> IncludedClasses { get; set; } = Array.Empty<string>();
    public virtual IEnumerable<string> IncludedMethods { get; set; } = Array.Empty<string>();
    public virtual IEnumerable<string> IncludedNamespaces { get; set; } = Array.Empty<string>();

    protected override bool IsXunit => true;

    protected override TestRunner GetTestRunner(LogWriter logWriter)
    {
        var runner = new MyXUnitTestRunner(logWriter);
        //var xUnitTestRunnerType = typeof(XunitTestRunnerBase).Assembly.GetType("Microsoft.DotNet.XHarness.TestRunners.Xunit.XUnitTestRunner");
        //var runner = (XunitTestRunnerBase)xUnitTestRunnerType!.GetConstructors().First().Invoke(new[] { logWriter });

        //ConfigureRunnerFilters(runner, ApplicationOptions.Current);
        var configureRunnerFiltersMethod = typeof(ApplicationEntryPoint).GetMethod("ConfigureRunnerFilters", BindingFlags.Static | BindingFlags.NonPublic);
        configureRunnerFiltersMethod!.Invoke(null, new object[] { runner, ApplicationOptions.Current });

        runner.SkipCategories(ExcludedTraits);
        runner.SkipCategories(IncludedTraits, isExcluded: false);
        foreach (var cls in IncludedClasses)
        {
            runner.SkipClass(cls, false);
        }
        foreach (var method in IncludedMethods)
        {
            runner.SkipMethod(method, false);
        }
        foreach (var ns in IncludedNamespaces)
        {
            runner.SkipNamespace(ns, isExcluded: false);
        }

        return runner;
    }

    protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies()
        => new[] { new TestAssemblyInfo(Assembly.LoadFrom(TestAssembly), TestAssembly) };
}
