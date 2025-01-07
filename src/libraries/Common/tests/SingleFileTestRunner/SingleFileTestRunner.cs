// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

// @TODO medium-to-longer term, we should try to get rid of the special-unicorn-single-file runner in favor of making the real runner work for single file.
// https://github.com/dotnet/runtime/issues/70432
public class SingleFileTestRunner : XunitTestFramework
{
    private SingleFileTestRunner(IMessageSink messageSink)
    : base(messageSink) { }

    public static int Main(string[] args)
    {
        var asm = typeof(SingleFileTestRunner).Assembly;
        Console.WriteLine("Running assembly:" + asm.FullName);

        // The current RemoteExecutor implementation is not compatible with the SingleFileTestRunner.
        Environment.SetEnvironmentVariable("DOTNET_REMOTEEXECUTOR_SUPPORTED", "0");

        // To detect ReadyToRun testing mode, we set a constant in
        // eng/testing/tests.singlefile.targets, which we use in the following
        // preprocessor directive. In the case that it is defined, we set an
        // environment variable that we consume later to implement
        // PlatformDetection.IsReadyToRunCompiled. This last value is used for the
        // [ActiveIssue] annotations designed to exclude tests from running.

#if TEST_READY_TO_RUN_COMPILED
        Environment.SetEnvironmentVariable("TEST_READY_TO_RUN_MODE" ,"1");
#endif

        var diagnosticSink = new ConsoleDiagnosticMessageSink();
        var testsFinished = new TaskCompletionSource();
        var testSink = new TestMessageSink();

#pragma warning disable CS0618 // Delegating*Sink types are marked obsolete
        var summarySink = new DelegatingExecutionSummarySink(testSink,
            () => false,
            (completed, summary) => Console.WriteLine($"Tests run: {summary.Total}, Errors: {summary.Errors}, Failures: {summary.Failed}, Skipped: {summary.Skipped}. Time: {TimeSpan.FromSeconds((double)summary.Time).TotalSeconds}s"));
        var resultsXmlAssembly = new XElement("assembly");
        var resultsSink = new DelegatingXmlCreationSink(summarySink, resultsXmlAssembly);
#pragma warning restore CS0618

        testSink.Execution.TestSkippedEvent += args => { Console.WriteLine($"[SKIP] {args.Message.Test.DisplayName}"); };
        testSink.Execution.TestFailedEvent += args => { Console.WriteLine($"[FAIL] {args.Message.Test.DisplayName}{Environment.NewLine}{Xunit.ExceptionUtility.CombineMessages(args.Message)}{Environment.NewLine}{Xunit.ExceptionUtility.CombineStackTraces(args.Message)}"); };

        testSink.Execution.TestAssemblyFinishedEvent += args =>
        {
            Console.WriteLine($"Finished {args.Message.TestAssembly.Assembly}{Environment.NewLine}");
            testsFinished.SetResult();
        };

        var assemblyConfig = new TestAssemblyConfiguration()
        {
            // Turn off pre-enumeration of theories, since there is no theory selection UI in this runner
            PreEnumerateTheories = false,
        };

        var xunitTestFx = new SingleFileTestRunner(diagnosticSink);
        var asmInfo = Reflector.Wrap(asm);
        var asmName = asm.GetName();

        var discoverySink = new TestDiscoverySink();
        var discoverer = xunitTestFx.CreateDiscoverer(asmInfo);
        discoverer.Find(false, discoverySink, TestFrameworkOptions.ForDiscovery(assemblyConfig));
        discoverySink.Finished.WaitOne();

        string xmlResultFileName = null;
        XunitFilters filters = new XunitFilters();
        // Quick hack wo much validation to get args that are passed (notrait, xml)
        Dictionary<string, List<string>> noTraits = new Dictionary<string, List<string>>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-notrait", StringComparison.OrdinalIgnoreCase))
            {
                var traitKeyValue = args[i + 1].Split("=", StringSplitOptions.TrimEntries);

                if (!noTraits.TryGetValue(traitKeyValue[0], out List<string> values))
                {
                    noTraits.Add(traitKeyValue[0], values = new List<string>());
                }

                values.Add(traitKeyValue[1]);
                i++;
            }

            if (args[i].Equals("-xml", StringComparison.OrdinalIgnoreCase))
            {
                xmlResultFileName = args[i + 1].Trim();
                i++;
            }

            if (args[i].Equals("-class", StringComparison.OrdinalIgnoreCase))
            {
                filters.IncludedClasses.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-noclass", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-class-", StringComparison.OrdinalIgnoreCase))
            {
                filters.ExcludedClasses.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-method", StringComparison.OrdinalIgnoreCase))
            {
                filters.IncludedMethods.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-nomethod", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-method-", StringComparison.OrdinalIgnoreCase))
            {
                filters.ExcludedMethods.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-namespace", StringComparison.OrdinalIgnoreCase))
            {
                filters.IncludedNamespaces.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-nonamespace", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-namespace-", StringComparison.OrdinalIgnoreCase))
            {
                filters.ExcludedNamespaces.Add(args[i + 1].Trim());
                i++;
            }

            if (args[i].Equals("-parallel", StringComparison.OrdinalIgnoreCase))
            {
                string parallelismArg = args[i + 1].Trim().ToLower();
                var (parallelizeAssemblies, parallelizeTestCollections) = parallelismArg switch
                {
                    "all" => (true, true),
                    "assemblies" => (true, false),
                    "collections" => (false, true),
                    "none" => (false, false),
                    _ => throw new ArgumentException($"Unknown parallelism option '{parallelismArg}'.")
                };

                assemblyConfig.ParallelizeAssembly = parallelizeAssemblies;
                assemblyConfig.ParallelizeTestCollections = parallelizeTestCollections;
                i++;
            }
        }

        foreach (KeyValuePair<string, List<string>> kvp in noTraits)
        {
            filters.ExcludedTraits.Add(kvp.Key, kvp.Value);
        }

        var filteredTestCases = discoverySink.TestCases.Where(filters.Filter).ToList();
        var executor = xunitTestFx.CreateExecutor(asmName);
        executor.RunTests(filteredTestCases, resultsSink, TestFrameworkOptions.ForExecution(assemblyConfig));

        resultsSink.Finished.WaitOne();

        // Helix need to see results file in the drive to detect if the test has failed or not
        if(xmlResultFileName != null)
        {
            resultsXmlAssembly.Save(xmlResultFileName);
        }

        var failed = resultsSink.ExecutionSummary.Failed > 0 || resultsSink.ExecutionSummary.Errors > 0;
        return failed ? 1 : 0;
    }
}

// This is about running on desktop FX, which we don't do
#pragma warning disable xUnit3000
internal class ConsoleDiagnosticMessageSink : IMessageSink
{
    public bool OnMessage(IMessageSinkMessage message)
    {
        if (message is IDiagnosticMessage diagnosticMessage)
        {
            return true;
        }
        return false;
    }
}
#pragma warning restore xUnit3000
