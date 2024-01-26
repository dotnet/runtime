// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Microsoft.DotNet.XHarness.Common;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.XHarness.TestRunners.Xunit;

internal class XsltIdGenerator
{
    // NUnit3 xml does not have schema, there is no much info about it, most examples just have incremental IDs.
    private int _seed = 1000;
    public int GenerateHash() => _seed++;
}

internal class MyXUnitTestRunner : MyXunitTestRunnerBase
{
    private readonly TestMessageSink _messageSink;

    public int? MaxParallelThreads { get; set; }

    private XElement _assembliesElement;

    public AppDomainSupport AppDomainSupport { get; set; } = AppDomainSupport.Denied;
    protected override string ResultsFileName { get; set; } = "TestResults.xUnit.xml";

    public MyXUnitTestRunner(LogWriter logger) : base(logger)
    {
        _messageSink = new TestMessageSink();

        _messageSink.Diagnostics.DiagnosticMessageEvent += HandleDiagnosticMessage;
        _messageSink.Diagnostics.ErrorMessageEvent += HandleDiagnosticErrorMessage;

        _messageSink.Discovery.DiscoveryCompleteMessageEvent += HandleDiscoveryCompleteMessage;
        _messageSink.Discovery.TestCaseDiscoveryMessageEvent += HandleDiscoveryTestCaseMessage;

        _messageSink.Runner.TestAssemblyDiscoveryFinishedEvent += HandleTestAssemblyDiscoveryFinished;
        _messageSink.Runner.TestAssemblyDiscoveryStartingEvent += HandleTestAssemblyDiscoveryStarting;
        _messageSink.Runner.TestAssemblyExecutionFinishedEvent += HandleTestAssemblyExecutionFinished;
        _messageSink.Runner.TestAssemblyExecutionStartingEvent += HandleTestAssemblyExecutionStarting;
        _messageSink.Runner.TestExecutionSummaryEvent += HandleTestExecutionSummary;

        _messageSink.Execution.AfterTestFinishedEvent += (MessageHandlerArgs<IAfterTestFinished> args) => HandleEvent("AfterTestFinishedEvent", args, HandleAfterTestFinished);
        _messageSink.Execution.AfterTestStartingEvent += (MessageHandlerArgs<IAfterTestStarting> args) => HandleEvent("AfterTestStartingEvent", args, HandleAfterTestStarting);
        _messageSink.Execution.BeforeTestFinishedEvent += (MessageHandlerArgs<IBeforeTestFinished> args) => HandleEvent("BeforeTestFinishedEvent", args, HandleBeforeTestFinished);
        _messageSink.Execution.BeforeTestStartingEvent += (MessageHandlerArgs<IBeforeTestStarting> args) => HandleEvent("BeforeTestStartingEvent", args, HandleBeforeTestStarting);
        _messageSink.Execution.TestAssemblyCleanupFailureEvent += (MessageHandlerArgs<ITestAssemblyCleanupFailure> args) => HandleEvent("TestAssemblyCleanupFailureEvent", args, HandleTestAssemblyCleanupFailure);
        _messageSink.Execution.TestAssemblyFinishedEvent += (MessageHandlerArgs<ITestAssemblyFinished> args) => HandleEvent("TestAssemblyFinishedEvent", args, HandleTestAssemblyFinished);
        _messageSink.Execution.TestAssemblyStartingEvent += (MessageHandlerArgs<ITestAssemblyStarting> args) => HandleEvent("TestAssemblyStartingEvent", args, HandleTestAssemblyStarting);
        _messageSink.Execution.TestCaseCleanupFailureEvent += (MessageHandlerArgs<ITestCaseCleanupFailure> args) => HandleEvent("TestCaseCleanupFailureEvent", args, HandleTestCaseCleanupFailure);
        _messageSink.Execution.TestCaseFinishedEvent += (MessageHandlerArgs<ITestCaseFinished> args) => HandleEvent("TestCaseFinishedEvent", args, HandleTestCaseFinished);
        _messageSink.Execution.TestCaseStartingEvent += (MessageHandlerArgs<ITestCaseStarting> args) => HandleEvent("TestStartingEvent", args, HandleTestCaseStarting);
        _messageSink.Execution.TestClassCleanupFailureEvent += (MessageHandlerArgs<ITestClassCleanupFailure> args) => HandleEvent("TestClassCleanupFailureEvent", args, HandleTestClassCleanupFailure);
        _messageSink.Execution.TestClassConstructionFinishedEvent += (MessageHandlerArgs<ITestClassConstructionFinished> args) => HandleEvent("TestClassConstructionFinishedEvent", args, HandleTestClassConstructionFinished);
        _messageSink.Execution.TestClassConstructionStartingEvent += (MessageHandlerArgs<ITestClassConstructionStarting> args) => HandleEvent("TestClassConstructionStartingEvent", args, HandleTestClassConstructionStarting);
        _messageSink.Execution.TestClassDisposeFinishedEvent += (MessageHandlerArgs<ITestClassDisposeFinished> args) => HandleEvent("TestClassDisposeFinishedEvent", args, HandleTestClassDisposeFinished);
        _messageSink.Execution.TestClassDisposeStartingEvent += (MessageHandlerArgs<ITestClassDisposeStarting> args) => HandleEvent("TestClassDisposeStartingEvent", args, HandleTestClassDisposeStarting);
        _messageSink.Execution.TestClassFinishedEvent += (MessageHandlerArgs<ITestClassFinished> args) => HandleEvent("TestClassFinishedEvent", args, HandleTestClassFinished);
        _messageSink.Execution.TestClassStartingEvent += (MessageHandlerArgs<ITestClassStarting> args) => HandleEvent("TestClassStartingEvent", args, HandleTestClassStarting);
        _messageSink.Execution.TestCleanupFailureEvent += (MessageHandlerArgs<ITestCleanupFailure> args) => HandleEvent("TestCleanupFailureEvent", args, HandleTestCleanupFailure);
        _messageSink.Execution.TestCollectionCleanupFailureEvent += (MessageHandlerArgs<ITestCollectionCleanupFailure> args) => HandleEvent("TestCollectionCleanupFailureEvent", args, HandleTestCollectionCleanupFailure);
        _messageSink.Execution.TestCollectionFinishedEvent += (MessageHandlerArgs<ITestCollectionFinished> args) => HandleEvent("TestCollectionFinishedEvent", args, HandleTestCollectionFinished);
        _messageSink.Execution.TestCollectionStartingEvent += (MessageHandlerArgs<ITestCollectionStarting> args) => HandleEvent("TestCollectionStartingEvent", args, HandleTestCollectionStarting);
        _messageSink.Execution.TestFailedEvent += (MessageHandlerArgs<ITestFailed> args) => HandleEvent("TestFailedEvent", args, HandleTestFailed);
        _messageSink.Execution.TestFinishedEvent += (MessageHandlerArgs<ITestFinished> args) => HandleEvent("TestFinishedEvent", args, HandleTestFinished);
        _messageSink.Execution.TestMethodCleanupFailureEvent += (MessageHandlerArgs<ITestMethodCleanupFailure> args) => HandleEvent("TestMethodCleanupFailureEvent", args, HandleTestMethodCleanupFailure);
        _messageSink.Execution.TestMethodFinishedEvent += (MessageHandlerArgs<ITestMethodFinished> args) => HandleEvent("TestMethodFinishedEvent", args, HandleTestMethodFinished);
        _messageSink.Execution.TestMethodStartingEvent += (MessageHandlerArgs<ITestMethodStarting> args) => HandleEvent("TestMethodStartingEvent", args, HandleTestMethodStarting);
        _messageSink.Execution.TestOutputEvent += (MessageHandlerArgs<ITestOutput> args) => HandleEvent("TestOutputEvent", args, HandleTestOutput);
        _messageSink.Execution.TestPassedEvent += (MessageHandlerArgs<ITestPassed> args) => HandleEvent("TestPassedEvent", args, HandleTestPassed);
        _messageSink.Execution.TestSkippedEvent += (MessageHandlerArgs<ITestSkipped> args) => HandleEvent("TestSkippedEvent", args, HandleTestSkipped);
        _messageSink.Execution.TestStartingEvent += (MessageHandlerArgs<ITestStarting> args) => HandleEvent("TestStartingEvent", args, HandleTestStarting);
    }

    public void AddFilter(XUnitFilter filter)
    {
        if (filter != null)
        {
            _filters.Add(filter);
        }
    }
    public void SetFilters(List<XUnitFilter> newFilters)
    {
        if (newFilters == null)
        {
            _filters = null;
            return;
        }

        if (_filters == null)
        {
            _filters = new XUnitFiltersCollection();
        }

        _filters.AddRange(newFilters);
    }

    private void HandleEvent<T>(string name, MessageHandlerArgs<T> args, Action<MessageHandlerArgs<T>> actualHandler) where T : class, IMessageSinkMessage
    {
        try
        {
            actualHandler(args);
        }
        catch (Exception ex)
        {
            OnError($"Handler for event {name} failed with exception");
            OnError(ex.ToString());
        }
    }

    private void HandleTestStarting(MessageHandlerArgs<ITestStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        if (Environment.GetEnvironmentVariable("XHARNESS_LOG_TEST_START") != null)
        {
            OnInfo($"\t[STRT] {args.Message.Test.DisplayName}");
        }

        OnDebug("Test starting");
        LogTestDetails(args.Message.Test, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);
    }

    private void HandleTestSkipped(MessageHandlerArgs<ITestSkipped> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        RaiseTestSkippedCase(args.Message, args.Message.TestCases, args.Message.TestCase);
    }

    private void HandleTestPassed(MessageHandlerArgs<ITestPassed> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        PassedTests++;
        OnInfo($"\t[PASS] {args.Message.TestCase.DisplayName}");
        LogTestDetails(args.Message.Test, log: OnDebug);
        LogTestOutput(args.Message, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
        // notify the completion of the test
        OnTestCompleted((
            TestName: args.Message.Test.DisplayName,
            TestResult: TestResult.Passed
        ));
    }

    private void HandleTestOutput(MessageHandlerArgs<ITestOutput> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo(args.Message.Output);
    }

    private void HandleTestMethodStarting(MessageHandlerArgs<ITestMethodStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDebug("Test method starting");
        LogTestMethodDetails(args.Message.TestMethod.Method, log: OnDebug);
        LogTestClassDetails(args.Message.TestMethod.TestClass, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestMethodFinished(MessageHandlerArgs<ITestMethodFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDebug("Test method finished");
        LogTestMethodDetails(args.Message.TestMethod.Method, log: OnDebug);
        LogTestClassDetails(args.Message.TestMethod.TestClass, log: OnDebug);
        LogSummary(args.Message, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestMethodCleanupFailure(MessageHandlerArgs<ITestMethodCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError($"Test method cleanup failure{GetAssemblyInfo(args.Message.TestAssembly)}");
        LogTestMethodDetails(args.Message.TestMethod.Method, log: OnError);
        LogTestClassDetails(args.Message.TestMethod.TestClass, log: OnError);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleTestFinished(MessageHandlerArgs<ITestFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        ExecutedTests++;
        OnDiagnostic("Test finished");
        LogTestDetails(args.Message.Test, log: OnDiagnostic);
        LogTestOutput(args.Message, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);
    }

    private void HandleTestFailed(MessageHandlerArgs<ITestFailed> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        FailedTests++;
        string assemblyInfo = GetAssemblyInfo(args.Message.TestAssembly);
        var sb = new StringBuilder($"\t[FAIL] {args.Message.TestCase.DisplayName}");
        LogTestDetails(args.Message.Test, OnError, sb);
        sb.AppendLine();
        if (!string.IsNullOrEmpty(assemblyInfo))
        {
            sb.AppendLine($"   Assembly: {assemblyInfo}");
        }

        LogSourceInformation(args.Message.TestCase.SourceInformation, OnError, sb);
        LogFailureInformation(args.Message, OnError, sb);
        sb.AppendLine();
        LogTestOutput(args.Message, OnError, sb);
        sb.AppendLine();
        if (args.Message.TestCase.Traits != null && args.Message.TestCase.Traits.Count > 0)
        {
            foreach (var kvp in args.Message.TestCase.Traits)
            {
                string message = $"   Test trait name: {kvp.Key}";
                OnError(message);
                sb.AppendLine(message);

                foreach (string v in kvp.Value)
                {
                    message = $"      value: {v}";
                    OnError(message);
                    sb.AppendLine(message);
                }
            }
            sb.AppendLine();
        }
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);

        FailureInfos.Add(new TestFailureInfo
        {
            TestName = args.Message.Test?.DisplayName,
            Message = sb.ToString()
        });
        OnInfo($"\t[FAIL] {args.Message.Test?.TestCase.DisplayName}");
        OnInfo(sb.ToString());
        OnTestCompleted((
            TestName: args.Message.Test?.DisplayName,
            TestResult: TestResult.Failed
        ));
    }

    private void HandleTestCollectionStarting(MessageHandlerArgs<ITestCollectionStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"\n{args.Message.TestCollection.DisplayName}");
        OnDebug("Test collection starting");
        LogTestCollectionDetails(args.Message.TestCollection, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestCollectionFinished(MessageHandlerArgs<ITestCollectionFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDebug("Test collection finished");
        LogSummary(args.Message, log: OnDebug);
        LogTestCollectionDetails(args.Message.TestCollection, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestCollectionCleanupFailure(MessageHandlerArgs<ITestCollectionCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError("Error during test collection cleanup");
        LogTestCollectionDetails(args.Message.TestCollection, log: OnError);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnError);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleTestCleanupFailure(MessageHandlerArgs<ITestCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError($"Test cleanup failure{GetAssemblyInfo(args.Message.TestAssembly)}");
        LogTestDetails(args.Message.Test, log: OnError);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnError);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleTestClassStarting(MessageHandlerArgs<ITestClassStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test class starting");
        LogTestClassDetails(args.Message.TestClass, log: OnDiagnostic);
    }

    private void HandleTestClassFinished(MessageHandlerArgs<ITestClassFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDebug("Test class finished");
        OnInfo($"{args.Message.TestClass.Class.Name} {args.Message.ExecutionTime} ms");
        LogTestClassDetails(args.Message.TestClass, OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestClassDisposeStarting(MessageHandlerArgs<ITestClassDisposeStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test class dispose starting");
        LogTestDetails(args.Message.Test, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestClassDisposeFinished(MessageHandlerArgs<ITestClassDisposeFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test class dispose finished");
        LogTestDetails(args.Message.Test, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestClassConstructionStarting(MessageHandlerArgs<ITestClassConstructionStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test class construction starting");
        LogTestDetails(args.Message.Test, OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);
    }

    private void HandleTestClassConstructionFinished(MessageHandlerArgs<ITestClassConstructionFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test class construction finished");
        LogTestDetails(args.Message.Test, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);
    }

    private void HandleTestClassCleanupFailure(MessageHandlerArgs<ITestClassCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError($"Test class cleanup error{GetAssemblyInfo(args.Message.TestAssembly)}");
        LogTestClassDetails(args.Message.TestClass, log: OnError);
        LogTestCollectionDetails(args.Message.TestCollection, log: OnError);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnError);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleTestCaseStarting(MessageHandlerArgs<ITestCaseStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic("Test case starting");
        ReportTestCase("   Starting", args.Message.TestCase, log: OnDiagnostic);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDiagnostic);
    }

    private void HandleTestCaseFinished(MessageHandlerArgs<ITestCaseFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDebug("Test case finished executing");
        ReportTestCase("   Finished", args.Message.TestCase, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnDebug);
        LogSummary(args.Message, log: OnDebug);
    }

    private void HandleTestCaseCleanupFailure(MessageHandlerArgs<ITestCaseCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError("Test case cleanup failure");
        ReportTestCase("   Failed", args.Message.TestCase, log: OnError);
        ReportTestCases("   Associated", args.Message.TestCases, args.Message.TestCase, OnError);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleTestAssemblyStarting(MessageHandlerArgs<ITestAssemblyStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"[Test environment: {args.Message.TestEnvironment}]");
        OnInfo($"[Test framework: {args.Message.TestFrameworkDisplayName}]");
        LogAssemblyInformation(args.Message, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDebug);
    }

    private void HandleTestAssemblyFinished(MessageHandlerArgs<ITestAssemblyFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        TotalTests = args.Message.TestsRun; // HACK: We are not counting correctly all the tests
        OnDebug("Execution process for assembly finished");
        LogAssemblyInformation(args.Message, log: OnDebug);
        LogSummary(args.Message, log: OnDebug);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnDiagnostic);
    }

    private void HandleTestAssemblyCleanupFailure(MessageHandlerArgs<ITestAssemblyCleanupFailure> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnError("Assembly cleanup failure");
        LogAssemblyInformation(args.Message, OnError);
        ReportTestCases("   Associated", args.Message.TestCases, log: OnError);
        LogFailureInformation(args.Message, log: OnError);
    }

    private void HandleBeforeTestStarting(MessageHandlerArgs<IBeforeTestStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        // notify that a method is starting
        OnTestStarted(args.Message.Test.DisplayName);
        OnDiagnostic($"'Before' method for test '{args.Message.Test.DisplayName}' starting");
    }

    private void HandleBeforeTestFinished(MessageHandlerArgs<IBeforeTestFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic($"'Before' method for test '{args.Message.Test.DisplayName}' finished");
    }

    private void HandleAfterTestStarting(MessageHandlerArgs<IAfterTestStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic($"'After' method for test '{args.Message.Test.DisplayName}' starting");
    }

    private void HandleAfterTestFinished(MessageHandlerArgs<IAfterTestFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic($"'After' method for test '{args.Message.Test.DisplayName}' finished");
    }

    private void HandleTestExecutionSummary(MessageHandlerArgs<ITestExecutionSummary> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo("All tests finished");
        OnInfo($"    Elapsed time: {args.Message.ElapsedClockTime}");

        if (args.Message.Summaries == null || args.Message.Summaries.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, ExecutionSummary> summary in args.Message.Summaries)
        {
            OnInfo(string.Empty);
            OnInfo($" Assembly: {summary.Key}");
            LogSummary(summary.Value, log: OnDebug);
        }
    }

    private void HandleTestAssemblyExecutionStarting(MessageHandlerArgs<ITestAssemblyExecutionStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"Execution starting for assembly {args.Message.Assembly.AssemblyFilename}");
    }

    private void HandleTestAssemblyExecutionFinished(MessageHandlerArgs<ITestAssemblyExecutionFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"Execution finished for assembly {args.Message.Assembly.AssemblyFilename}");
        LogSummary(args.Message.ExecutionSummary, log: OnDebug);
    }

    private void HandleTestAssemblyDiscoveryStarting(MessageHandlerArgs<ITestAssemblyDiscoveryStarting> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"Discovery for assembly {args.Message.Assembly.AssemblyFilename} starting");
        OnInfo($"   Will use AppDomain: {args.Message.AppDomain.YesNo()}");
    }

    private void HandleTestAssemblyDiscoveryFinished(MessageHandlerArgs<ITestAssemblyDiscoveryFinished> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo($"Discovery for assembly {args.Message.Assembly.AssemblyFilename} finished");
        OnInfo($"   Test cases discovered: {args.Message.TestCasesDiscovered}");
        OnInfo($"   Test cases to run: {args.Message.TestCasesToRun}");
    }

    private void HandleDiagnosticMessage(MessageHandlerArgs<IDiagnosticMessage> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnDiagnostic(args.Message.Message);
    }

    private void HandleDiagnosticErrorMessage(MessageHandlerArgs<IErrorMessage> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        LogFailureInformation(args.Message);
    }

    private void HandleDiscoveryCompleteMessage(MessageHandlerArgs<IDiscoveryCompleteMessage> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        OnInfo("Discovery complete");
    }

    private void HandleDiscoveryTestCaseMessage(MessageHandlerArgs<ITestCaseDiscoveryMessage> args)
    {
        if (args == null || args.Message == null)
        {
            return;
        }

        ITestCase singleTestCase = args.Message.TestCase;
        ReportTestCases("Discovered", args.Message.TestCases, log: OnInfo, ignore: (ITestCase tc) => tc == singleTestCase);
        ReportTestCase("Discovered", singleTestCase, log: OnInfo);
    }

    private void RaiseTestSkippedCase(ITestResultMessage message, IEnumerable<ITestCase> testCases, ITestCase testCase)
    {
        SkippedTests++;
        OnInfo($"\t[IGNORED] {testCase.DisplayName}");
        LogTestDetails(message.Test, log: OnDebug);
        LogTestOutput(message, log: OnDiagnostic);
        ReportTestCases("   Associated", testCases, log: OnDiagnostic);
        // notify that the test completed because it was skipped
        OnTestCompleted((
            TestName: message.Test.DisplayName,
            TestResult: TestResult.Skipped
        ));
    }

    private void ReportTestCases(string verb, IEnumerable<ITestCase> testCases, ITestCase ignoreTestCase, Action<string> log = null) => ReportTestCases(verb, testCases, log, (ITestCase tc) => ignoreTestCase == tc);

    private void ReportTestCases(string verb, IEnumerable<ITestCase> testCases, Action<string> log = null, Func<ITestCase, bool> ignore = null)
    {
        if (testCases == null)
        {
            return;
        }

        foreach (ITestCase tc in testCases)
        {
            if (ignore != null && ignore(tc))
            {
                continue;
            }

            ReportTestCase(verb, tc, log);
        }
    }

    private void ReportTestCase(string verb, ITestCase testCase, Action<string> log = null)
    {
        if (testCase == null)
        {
            return;
        }

        EnsureLogger(log)($"{verb} test case: {testCase.DisplayName}");
    }

    private void LogAssemblyInformation(ITestAssemblyMessage message, Action<string> log = null, StringBuilder sb = null)
    {
        if (message == null)
        {
            return;
        }

        do_log($"[Assembly name: {message.TestAssembly.Assembly.Name}]", log, sb);
        do_log($"[Assembly path: {message.TestAssembly.Assembly.AssemblyPath}]", OnDiagnostic, sb);
    }

    private void LogFailureInformation(IFailureInformation info, Action<string> log = null, StringBuilder sb = null)
    {
        if (info == null)
        {
            return;
        }

        string message = ExceptionUtility.CombineMessages(info);
        do_log($"   Exception messages: {message}", log, sb);

        string traces = ExceptionUtility.CombineStackTraces(info);
        do_log($"   Exception stack traces: {traces}", log, sb);
    }

    private Action<string> EnsureLogger(Action<string> log) => log ?? OnInfo;

#pragma warning disable IDE0060 // Remove unused parameter
    private static void LogTestMethodDetails(IMethodInfo method, Action<string> log = null, StringBuilder sb = null)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // log = EnsureLogger(log);
        // log ($"   Test method name: {method.Type.Name}.{method.Name}");
    }

    private void LogTestOutput(ITestFinished test, Action<string> log = null, StringBuilder sb = null) => LogTestOutput(test.ExecutionTime, test.Output, log, sb);

    private void LogTestOutput(ITestResultMessage test, Action<string> log = null, StringBuilder sb = null) => LogTestOutput(test.ExecutionTime, test.Output, log, sb);

    private void LogTestOutput(decimal executionTime, string output, Action<string> log = null, StringBuilder sb = null)
    {
        do_log($"   Execution time: {executionTime}", log, sb);
        if (!string.IsNullOrEmpty(output))
        {
            do_log(" **** Output start ****", log, sb);
            foreach (string line in output.Split('\n'))
            {
                do_log(line, log, sb);
            }

            do_log(" **** Output end ****", log, sb);
        }
    }

    private void LogTestCollectionDetails(ITestCollection collection, Action<string> log = null, StringBuilder sb = null) => do_log($"   Test collection: {collection.DisplayName}", log, sb);

    private void LogTestClassDetails(ITestClass klass, Action<string> log = null, StringBuilder sb = null)
    {
        do_log($"   Class name: {klass.Class.Name}", log, sb);
        do_log($"   Class assembly: {klass.Class.Assembly.Name}", OnDebug, sb);
        do_log($"   Class assembly path: {klass.Class.Assembly.AssemblyPath}", OnDebug, sb);
    }

    private void LogTestDetails(ITest test, Action<string> log = null, StringBuilder sb = null)
    {
        do_log($"   Test name: {test.DisplayName}", log, sb);
        if (string.Compare(test.DisplayName, test.TestCase.DisplayName, StringComparison.Ordinal) != 0)
        {
            do_log($"   Test case: {test.TestCase.DisplayName}", log, sb);
        }
    }

    private void LogSummary(IFinishedMessage summary, Action<string> log = null, StringBuilder sb = null)
    {
        do_log($"   Time: {summary.ExecutionTime}", log, sb);
        do_log($"   Total tests run: {summary.TestsRun}", log, sb);
        do_log($"   Skipped tests: {summary.TestsSkipped}", log, sb);
        do_log($"   Failed tests: {summary.TestsFailed}", log, sb);
    }

    private void LogSummary(ExecutionSummary summary, Action<string> log = null, StringBuilder sb = null)
    {
        do_log($"   Time: {summary.Time}", log, sb);
        do_log($"   Total tests run: {summary.Total}", log, sb);
        do_log($"   Total errors: {summary.Errors}", log, sb);
        do_log($"   Skipped tests: {summary.Skipped}", log, sb);
        do_log($"   Failed tests: {summary.Failed}", log, sb);
    }

    private void LogSourceInformation(ISourceInformation source, Action<string> log = null, StringBuilder sb = null)
    {
        if (source == null || string.IsNullOrEmpty(source.FileName))
        {
            return;
        }

        string location = source.FileName;
        if (source.LineNumber != null && source.LineNumber >= 0)
        {
            location += $":{source.LineNumber}";
        }

        do_log($"   Source: {location}", log, sb);
        sb?.AppendLine();
    }

    private static string GetAssemblyInfo(ITestAssembly assembly)
    {
        string name = assembly?.Assembly?.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return $" [{name}]";
    }

    private void do_log(string message, Action<string> log = null, StringBuilder sb = null)
    {
        log = EnsureLogger(log);

        if (sb != null)
        {
            sb.Append(message);
        }

        log(message);
    }

    public override async Task Run(IEnumerable<TestAssemblyInfo> testAssemblies)
    {
        if (testAssemblies == null)
        {
            throw new ArgumentNullException(nameof(testAssemblies));
        }

        if (_filters != null && _filters.Count > 0)
        {
            do_log("Configured filters:");
            foreach (XUnitFilter filter in _filters)
            {
                do_log($"  {filter}");
            }
        }

        _assembliesElement = new XElement("assemblies");
        Action<string> log = LogExcludedTests ? (s) => do_log(s) : (Action<string>)null;
        foreach (TestAssemblyInfo assemblyInfo in testAssemblies)
        {
            if (assemblyInfo == null || assemblyInfo.Assembly == null)
            {
                continue;
            }

            if (_filters.AssemblyFilters.Any() && _filters.IsExcluded(assemblyInfo, log))
            {
                continue;
            }

            if (string.IsNullOrEmpty(assemblyInfo.FullPath))
            {
                OnWarning($"Assembly '{assemblyInfo.Assembly}' cannot be found on the filesystem. xUnit requires access to actual on-disk file.");
                continue;
            }

            OnInfo($"Assembly: {assemblyInfo.Assembly} ({assemblyInfo.FullPath})");
            XElement assemblyElement = null;
            try
            {
                OnAssemblyStart(assemblyInfo.Assembly);
                assemblyElement = await Run(assemblyInfo.Assembly, assemblyInfo.FullPath).ConfigureAwait(false);
            }
            catch (FileNotFoundException ex)
            {
                OnWarning($"Assembly '{assemblyInfo.Assembly}' using path '{assemblyInfo.FullPath}' cannot be found on the filesystem. xUnit requires access to actual on-disk file.");
                OnWarning($"Exception is '{ex}'");
            }
            finally
            {
                OnAssemblyFinish(assemblyInfo.Assembly);
                if (assemblyElement != null)
                {
                    _assembliesElement.Add(assemblyElement);
                }
            }
        }

        LogFailureSummary();
        TotalTests += FilteredTests; // ensure that we do have in the total run the excluded ones.
    }

    public override string WriteResultsToFile(XmlResultJargon jargon)
    {
        if (_assembliesElement == null)
        {
            return string.Empty;
        }
        // remove all the empty nodes
        _assembliesElement.Descendants().Where(e => e.Name == "collection" && !e.Descendants().Any()).Remove();
        string outputFilePath = GetResultsFilePath();
        var settings = new XmlWriterSettings { Indent = true };
        using (var xmlWriter = XmlWriter.Create(outputFilePath, settings))
        {
            switch (jargon)
            {
                case XmlResultJargon.TouchUnit:
                case XmlResultJargon.NUnitV2:
                    Transform_Results("NUnitXml.xslt", _assembliesElement, xmlWriter);
                    break;
                case XmlResultJargon.NUnitV3:
                    Transform_Results("NUnit3Xml.xslt", _assembliesElement, xmlWriter);
                    break;
                default: // xunit as default, includes when we got Missing
                    _assembliesElement.Save(xmlWriter);
                    break;
            }
        }

        return outputFilePath;
    }
    public override void WriteResultsToFile(TextWriter writer, XmlResultJargon jargon)
    {
        if (_assembliesElement == null)
        {
            return;
        }
        // remove all the empty nodes
        _assembliesElement.Descendants().Where(e => e.Name == "collection" && !e.Descendants().Any()).Remove();
        var settings = new XmlWriterSettings { Indent = true };
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        {
            switch (jargon)
            {
                case XmlResultJargon.TouchUnit:
                case XmlResultJargon.NUnitV2:
                    try
                    {
                        Transform_Results("NUnitXml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine(e);
                    }
                    break;
                case XmlResultJargon.NUnitV3:
                    try
                    {
                        Transform_Results("NUnit3Xml.xslt", _assembliesElement, xmlWriter);
                    }
                    catch (Exception e)
                    {
                        writer.WriteLine(e);
                    }
                    break;
                default: // xunit as default, includes when we got Missing
                    _assembliesElement.Save(xmlWriter);
                    break;
            }
        }
    }

    private void Transform_Results(string xsltResourceName, XElement element, XmlWriter writer)
    {
        var xmlTransform = new System.Xml.Xsl.XslCompiledTransform();
        var name = GetType().Assembly.GetManifestResourceNames().Where(a => a.EndsWith(xsltResourceName, StringComparison.Ordinal)).FirstOrDefault();
        if (name == null)
        {
            return;
        }

        using (var xsltStream = GetType().Assembly.GetManifestResourceStream(name))
        {
            if (xsltStream == null)
            {
                throw new Exception($"Stream with name {name} cannot be found! We have {GetType().Assembly.GetManifestResourceNames()[0]}");
            }
            // add the extension so that we can get the hash from the name of the test
            // Create an XsltArgumentList.
            var xslArg = new XsltArgumentList();

            var generator = new XsltIdGenerator();
            xslArg.AddExtensionObject("urn:hash-generator", generator);

            using (var xsltReader = XmlReader.Create(xsltStream))
            using (var xmlReader = element.CreateReader())
            {
                xmlTransform.Load(xsltReader);
                xmlTransform.Transform(xmlReader, xslArg, writer);
            }
        }
    }

    protected virtual Stream GetConfigurationFileStream(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        string path = assembly.Location?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        path = Path.Combine(path, ".xunit.runner.json");
        if (!File.Exists(path))
        {
            return null;
        }

        return File.OpenRead(path);
    }

    protected virtual TestAssemblyConfiguration GetConfiguration(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        Stream configStream = GetConfigurationFileStream(assembly);
        if (configStream != null)
        {
            using (configStream)
            {
                return ConfigReader.Load(configStream);
            }
        }

        return null;
    }

    protected virtual ITestFrameworkDiscoveryOptions GetFrameworkOptionsForDiscovery(TestAssemblyConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return TestFrameworkOptions.ForDiscovery(configuration);
    }

    protected virtual ITestFrameworkExecutionOptions GetFrameworkOptionsForExecution(TestAssemblyConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return TestFrameworkOptions.ForExecution(configuration);
    }

    private async Task<XElement> Run(Assembly assembly, string assemblyPath)
    {
        using (var frontController = new XunitFrontController(AppDomainSupport, assemblyPath, null, false))
        {
            using (var discoverySink = new TestDiscoverySink())
            {
                var configuration = GetConfiguration(assembly) ?? new TestAssemblyConfiguration() { PreEnumerateTheories = false };
                ITestFrameworkDiscoveryOptions discoveryOptions = GetFrameworkOptionsForDiscovery(configuration);
                discoveryOptions.SetSynchronousMessageReporting(true);
                Logger.OnDebug($"Starting test discovery in the '{assembly}' assembly");
                frontController.Find(false, discoverySink, discoveryOptions);
                Logger.OnDebug($"Test discovery in assembly '{assembly}' completed");
                discoverySink.Finished.WaitOne();

                if (discoverySink.TestCases == null || discoverySink.TestCases.Count == 0)
                {
                    Logger.Info("No test cases discovered");
                    return null;
                }

                TotalTests += discoverySink.TestCases.Count;
                List<ITestCase> testCases;
                if (_filters != null && _filters.TestCaseFilters.Any())
                {
                    Action<string> log = LogExcludedTests ? (s) => do_log(s) : (Action<string>)null;
                    testCases = discoverySink.TestCases.Where(
                        tc => !_filters.IsExcluded(tc, log)).ToList();
                    FilteredTests += discoverySink.TestCases.Count - testCases.Count;
                }
                else
                {
                    testCases = discoverySink.TestCases;
                }

                var summaryTaskSource = new TaskCompletionSource<ExecutionSummary>();
                var resultsXmlAssembly = new XElement("assembly");
                var resultsSink = new DelegatingXmlCreationSink(new DelegatingExecutionSummarySink(_messageSink), resultsXmlAssembly);
                var completionSink = new CompletionCallbackExecutionSink(resultsSink, summary => summaryTaskSource.SetResult(summary));

                ITestFrameworkExecutionOptions executionOptions = GetFrameworkOptionsForExecution(configuration);
                executionOptions.SetDisableParallelization(!RunInParallel);
                executionOptions.SetSynchronousMessageReporting(true);
                executionOptions.SetMaxParallelThreads(MaxParallelThreads);

                frontController.RunTests(testCases, completionSink, executionOptions);
                await summaryTaskSource.Task.ConfigureAwait(false);

                return resultsXmlAssembly;
            }
        }
    }
}
