using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

internal static class ThreadlessXunitTestRunner
{
    public static async Task RunAsync(params Assembly[] testAssemblies)
    {
        long failedTests = 0;
        long passedTests = 0;
        long skippedTests = 0;
        long executedTests = 0;
        long totalTests = 0;

        var configuration = new TestAssemblyConfiguration() { ShadowCopy = false, ParallelizeAssembly = false, ParallelizeTestCollections = false, MaxParallelThreads = 1, PreEnumerateTheories = false };
        var discoveryOptions = TestFrameworkOptions.ForDiscovery(configuration);
        var discoverySink = new TestDiscoverySink();
        var diagnosticSink = new ConsoleDiagnosticMessageSink();
        var testOptions = TestFrameworkOptions.ForExecution(configuration);
        var testSink = new TestMessageSink();

        var totalSummary = new ExecutionSummary();
        foreach (var testAssembly in testAssemblies)
        {
            var testAssemblyName = testAssembly.GetName().Name;
            var controller = new Xunit2(AppDomainSupport.Denied, new NullSourceInformationProvider(), testAssemblyName, shadowCopy: false, diagnosticMessageSink: diagnosticSink, verifyTestAssemblyExists: false);

            discoveryOptions.SetSynchronousMessageReporting(true);
            testOptions.SetSynchronousMessageReporting(true);

            Console.WriteLine($"Discovering: {testAssemblyName} (method display = {discoveryOptions.GetMethodDisplayOrDefault()}, method display options = {discoveryOptions.GetMethodDisplayOptionsOrDefault()})");
            var testAssemblyInfo = new ReflectionAssemblyInfo(testAssembly);
            var discoverer = new ThreadlessXunitDiscoverer(testAssemblyInfo, new NullSourceInformationProvider(), discoverySink);

            discoverer.FindWithoutThreads(includeSourceInformation: false, discoverySink, discoveryOptions);
            var testCasesToRun = discoverySink.TestCases;
            Console.WriteLine($"Discovered:  {testAssemblyName} (found {testCasesToRun.Count} of {discoverySink.TestCases.Count} test cases)");

            var summaryTaskSource = new TaskCompletionSource<ExecutionSummary>();
            var completionSink = new CompletionCallbackExecutionSink(new DelegatingExecutionSummarySink(testSink), summary => summaryTaskSource.SetResult(summary));

            testSink.Execution.TestPassedEvent += args =>
            {
                Console.WriteLine($"[PASS] {args.Message.Test.DisplayName}");
                passedTests++;
            };
            testSink.Execution.TestSkippedEvent += args =>
            {
                Console.WriteLine($"[SKIP] {args.Message.Test.DisplayName}");
                skippedTests++;
            };
            testSink.Execution.TestFailedEvent += args =>
            {
                Console.WriteLine($"[FAIL] {args.Message.Test.DisplayName}{Environment.NewLine}{Xunit.ExceptionUtility.CombineMessages(args.Message)}{Environment.NewLine}{Xunit.ExceptionUtility.CombineStackTraces(args.Message)}");
                failedTests++;
            };
            testSink.Execution.TestFinishedEvent += args => executedTests++;

            testSink.Execution.TestAssemblyStartingEvent += args => { Console.WriteLine($"Starting:    {testAssemblyName}"); };
            testSink.Execution.TestAssemblyFinishedEvent += args => { Console.WriteLine($"Finished:    {testAssemblyName}"); };

            controller.RunTests(testCasesToRun, completionSink, testOptions);

            totalSummary = Combine(totalSummary, await summaryTaskSource.Task);
        }
        totalTests = totalSummary.Total;
        Console.WriteLine($"{Environment.NewLine}=== TEST EXECUTION SUMMARY ==={Environment.NewLine}Total: {totalSummary.Total}, Errors: 0, Failed: {totalSummary.Failed}, Skipped: {totalSummary.Skipped}, Time: {TimeSpan.FromSeconds((double)totalSummary.Time).TotalSeconds}s{Environment.NewLine}");
    }

    private static ExecutionSummary Combine(ExecutionSummary aggregateSummary, ExecutionSummary assemblySummary)
    {
        return new ExecutionSummary
        {
            Total = aggregateSummary.Total + assemblySummary.Total,
            Failed = aggregateSummary.Failed + assemblySummary.Failed,
            Skipped = aggregateSummary.Skipped + assemblySummary.Skipped,
            Errors = aggregateSummary.Errors + assemblySummary.Errors,
            Time = aggregateSummary.Time + assemblySummary.Time
        };
    }
}

internal class ThreadlessXunitDiscoverer : XunitTestFrameworkDiscoverer
{
    public ThreadlessXunitDiscoverer(IAssemblyInfo assemblyInfo, ISourceInformationProvider sourceProvider, IMessageSink diagnosticMessageSink)
        : base(assemblyInfo, sourceProvider, diagnosticMessageSink)
    {
    }

    public void FindWithoutThreads(bool includeSourceInformation, IMessageSink discoveryMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions)
    {
        using (var messageBus = new SynchronousMessageBus(discoveryMessageSink))
        {
            foreach (var type in AssemblyInfo.GetTypes(includePrivateTypes: false).Where(IsValidTestClass))
            {
                var testClass = CreateTestClass(type);
                if (!FindTestsForType(testClass, includeSourceInformation, messageBus, discoveryOptions))
                {
                    break;
                }
            }

            messageBus.QueueMessage(new Xunit.Sdk.DiscoveryCompleteMessage());
        }
    }
}

internal class ConsoleDiagnosticMessageSink : Xunit.Sdk.LongLivedMarshalByRefObject, IMessageSink
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

internal class CompletionCallbackExecutionSink : Xunit.Sdk.LongLivedMarshalByRefObject, IExecutionSink
{
    private readonly Action<ExecutionSummary> _completionCallback;
    private readonly IExecutionSink _innerSink;

    public ExecutionSummary ExecutionSummary => _innerSink.ExecutionSummary;
    public ManualResetEvent Finished => _innerSink.Finished;

    public CompletionCallbackExecutionSink(IExecutionSink innerSink, Action<ExecutionSummary> completionCallback)
    {
        _innerSink = innerSink;
        _completionCallback = completionCallback;
    }

    public void Dispose() => _innerSink.Dispose();

    public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
    {
        var result = _innerSink.OnMessageWithTypes(message, messageTypes);
        message.Dispatch<ITestAssemblyFinished>(messageTypes, args => _completionCallback(ExecutionSummary));
        return result;
    }
}