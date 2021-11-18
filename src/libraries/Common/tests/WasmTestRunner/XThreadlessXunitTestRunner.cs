// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace WasmTestRunner
{
    internal class XThreadlessXunitTestRunner
    {
        public static async Task<int> Run(string assemblyFileName, bool printXml, XunitFilters filters)
        {
            var configuration = new TestAssemblyConfiguration() { ShadowCopy = false, ParallelizeAssembly = false, ParallelizeTestCollections = false, MaxParallelThreads = 1, PreEnumerateTheories = false };
            var discoveryOptions = TestFrameworkOptions.ForDiscovery(configuration);
            var discoverySink = new TestDiscoverySink();
            var diagnosticSink = new ConsoleDiagnosticMessageSink();
            var testOptions = TestFrameworkOptions.ForExecution(configuration);
            var testSink = new TestMessageSink();
            var controller = new Xunit2(AppDomainSupport.Denied, new NullSourceInformationProvider(), assemblyFileName, configFileName: null, shadowCopy: false, shadowCopyFolder: null, diagnosticMessageSink: diagnosticSink, verifyTestAssemblyExists: false);

            discoveryOptions.SetSynchronousMessageReporting(true);
            testOptions.SetSynchronousMessageReporting(true);

            Console.WriteLine($"Discovering: {assemblyFileName} (method display = {discoveryOptions.GetMethodDisplayOrDefault()}, method display options = {discoveryOptions.GetMethodDisplayOptionsOrDefault()})");
            var assembly = Assembly.LoadFrom(assemblyFileName);
            var assemblyInfo = new global::Xunit.Sdk.ReflectionAssemblyInfo(assembly);
            var discoverer = new ThreadlessXunitDiscoverer(assemblyInfo, new NullSourceInformationProvider(), discoverySink);

            discoverer.FindWithoutThreads(includeSourceInformation: false, discoverySink, discoveryOptions);
            discoverySink.Finished.WaitOne();
            var testCasesToRun = discoverySink.TestCases.Where(filters.Filter).ToList();
            Console.WriteLine($"Discovered:  {assemblyFileName} (found {testCasesToRun.Count} of {discoverySink.TestCases.Count} test cases)");

            var resultsXmlAssembly = new XElement("assembly");

            var summarySink = new DelegatingExecutionSummarySink(testSink, () => false, (completed, summary) =>
            {
                Console.WriteLine($"{Environment.NewLine}=== TEST EXECUTION SUMMARY ==={Environment.NewLine}Total: {summary.Total}, Errors: 0, Failed: {summary.Failed}, Skipped: {summary.Skipped}, Time: {TimeSpan.FromSeconds((double)summary.Time).TotalSeconds}s{Environment.NewLine}");
            });
            var resultsSink = new DelegatingXmlCreationSink(summarySink, resultsXmlAssembly);

            if (Environment.GetEnvironmentVariable("XHARNESS_LOG_TEST_START") != null)
            {
                testSink.Execution.TestStartingEvent += args => { Console.WriteLine($"[STRT] {args.Message.Test.DisplayName}"); };
            }
            testSink.Execution.TestPassedEvent += args => { Console.WriteLine($"[PASS] {args.Message.Test.DisplayName}"); };
            testSink.Execution.TestSkippedEvent += args => { Console.WriteLine($"[SKIP] {args.Message.Test.DisplayName}"); };
            testSink.Execution.TestFailedEvent += args => { Console.WriteLine($"[FAIL] {args.Message.Test.DisplayName}{Environment.NewLine}{ExceptionUtility.CombineMessages(args.Message)}{Environment.NewLine}{ExceptionUtility.CombineStackTraces(args.Message)}"); };

            testSink.Execution.TestAssemblyStartingEvent += args => { Console.WriteLine($"Starting:    {assemblyFileName}"); };
            testSink.Execution.TestAssemblyFinishedEvent += args => { Console.WriteLine($"Finished:    {assemblyFileName}"); };
            try
            {
                Console.WriteLine("Before RunTests " + DateTime.Now);
                controller.RunTests(testCasesToRun, resultsSink, testOptions);
                Console.WriteLine("After RunTests " + DateTime.Now);

                while (!resultsSink.Finished.WaitOne(0))
                {
                    await Task.Delay(1);
                }
                Console.WriteLine("After WaitOne " + DateTime.Now);

                if (printXml)
                {
                    Console.WriteLine("Before testResults.xml");
                    Console.WriteLine($"STARTRESULTXML");
                    var resultsXml = new XElement("assemblies");
                    resultsXml.Add(resultsXmlAssembly);

                    int chunks = 0;
                    using (var ms = new System.IO.MemoryStream())
                    {
                        resultsXml.Save(ms);

                        ms.Position = 0;

                        // Console.WriteLine("Before copy to output");
                        // Console.Out.Flush();

                        using (var output = Console.OpenStandardOutput())
                        {
                            byte[] buffer = new byte[1024];
                            int read;
                            while ((read = ms.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                // Console.WriteLine($"Read {read}");
                                output.Write(buffer, 0, read);
                                // Console.WriteLine($"Written {read}");
                                chunks++;
                            }

                            output.Flush();
                        }

                        // Console.WriteLine($"After copy to output (in {chunks} chunks)");
                        Console.Out.Flush();
                    }

                    // resultsXml.Save(Console.Out);
                    Console.WriteLine();
                    Console.WriteLine($"ENDRESULTXML");
                    Console.WriteLine($"After testResults.xml (in {chunks} chunks)");
                    Console.Out.Flush();
                }

                var failed = resultsSink.ExecutionSummary.Failed > 0 || resultsSink.ExecutionSummary.Errors > 0;
                return failed ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("XThreadlessXunitTestRunner failed " + ex);
                return 2;
            }
        }
    }

    internal class ThreadlessXunitDiscoverer : global::Xunit.Sdk.XunitTestFrameworkDiscoverer
    {
        public ThreadlessXunitDiscoverer(IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyInfo, sourceProvider, diagnosticMessageSink)
        {
        }

        public void FindWithoutThreads(bool includeSourceInformation, IMessageSink discoveryMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions)
        {
            using (var messageBus = new global::Xunit.Sdk.SynchronousMessageBus(discoveryMessageSink))
            {
                foreach (var type in AssemblyInfo.GetTypes(includePrivateTypes: false).Where(IsValidTestClass))
                {
                    var testClass = CreateTestClass(type);
                    if (!FindTestsForType(testClass, includeSourceInformation, messageBus, discoveryOptions))
                    {
                        break;
                    }
                }

                messageBus.QueueMessage(new global::Xunit.Sdk.DiscoveryCompleteMessage());
            }
        }
    }

    internal class ConsoleDiagnosticMessageSink : global::Xunit.Sdk.LongLivedMarshalByRefObject, IMessageSink
    {
        public bool OnMessage(IMessageSinkMessage message)
        {
            if (message is IDiagnosticMessage diagnosticMessage)
            {
                Console.WriteLine(diagnosticMessage.Message);
            }

            return true;
        }
    }
}
