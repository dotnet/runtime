using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.Common;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using XUnitWrapperLibrary;

namespace XHarnessRunnerLibrary;

public sealed class GeneratedTestRunner : TestRunner
{
    string _assemblyName;
    TestFilter.ISearchClause? _filter;
    Func<TestFilter?, TestSummary> _runTestsCallback;
    public GeneratedTestRunner(LogWriter logger, Func<TestFilter?, TestSummary> runTestsCallback, string assemblyName)
        :base(logger)
    {
        _assemblyName = assemblyName;
        _runTestsCallback = runTestsCallback;
        ResultsFileName = $"{_assemblyName}.testResults.xml";
    }

    public TestSummary LastTestRun { get; private set; } = new();

    protected override string ResultsFileName { get; set; }

    public override Task Run(IEnumerable<TestAssemblyInfo> testAssemblies)
    {
        LastTestRun = _runTestsCallback(_filter is not null ? new TestFilter(_filter) : null);
        PassedTests = LastTestRun.PassedTests;
        FailedTests = LastTestRun.FailedTests;
        SkippedTests = LastTestRun.SkippedTests;
        ExecutedTests = PassedTests + FailedTests;
        TotalTests = ExecutedTests + SkippedTests;
        return Task.CompletedTask;
    }

    public override string WriteResultsToFile(XmlResultJargon xmlResultJargon)
    {
        Debug.Assert(xmlResultJargon == XmlResultJargon.xUnit);
        File.WriteAllText(ResultsFileName, LastTestRun.GetTestResultOutput(_assemblyName));
        return ResultsFileName;
    }

    public override void WriteResultsToFile(TextWriter writer, XmlResultJargon jargon)
    {
        Debug.Assert(jargon == XmlResultJargon.xUnit);
        writer.WriteLine(LastTestRun.GetTestResultOutput(_assemblyName));
    }

    public override void SkipTests(IEnumerable<string> tests)
    {
        foreach (string test in tests)
        {
            var testNameClause = new TestFilter.NotClause(new TestFilter.NameClause(TestFilter.TermKind.DisplayName, test, true));
            _filter = _filter is null ? testNameClause : new TestFilter.AndClause(_filter, testNameClause);
        }
    }

    public override void SkipCategories(IEnumerable<string> categories)
    {
    }

    public override void SkipMethod(string method, bool isExcluded)
    {
        TestFilter.ISearchClause methodClause = new TestFilter.NameClause(TestFilter.TermKind.FullyQualifiedName, method, true);
        if (isExcluded)
        {
            methodClause = new TestFilter.NotClause(methodClause);
            _filter = _filter is null ? methodClause : new TestFilter.AndClause(_filter, methodClause);
        }
        else
        {
            _filter = _filter is null ? methodClause : new TestFilter.OrClause(_filter, methodClause);
        }
    }

    public override void SkipClass(string className, bool isExcluded)
    {
        throw new NotImplementedException();
    }
}