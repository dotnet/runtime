using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.Common;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using XUnitWrapperLibrary;

namespace XHarnessRunnerLibrary;

public sealed class GeneratedTestRunner : TestRunner
{
    private string _assemblyName;
    private TestFilter.ISearchClause? _filter;
    private Func<TestFilter?, TestSummary> _runTestsCallback;
    private Dictionary<string, string> _testExclusionTable;

    private readonly Boolean _writeBase64TestResults;

    public GeneratedTestRunner(
        LogWriter logger, 
        Func<TestFilter?, TestSummary> runTestsCallback, 
        string assemblyName,
        Dictionary<string, string> testExclusionTable,
        bool writeBase64TestResults)
        : base(logger)
    {
        _assemblyName = assemblyName;
        _runTestsCallback = runTestsCallback;
        _testExclusionTable = testExclusionTable;
        _writeBase64TestResults = writeBase64TestResults;

        ResultsFileName = $"{_assemblyName}.testResults.xml";
    }

    public TestSummary LastTestRun { get; private set; } = new();

    protected override string ResultsFileName { get; set; }

    public override Task Run(IEnumerable<TestAssemblyInfo> testAssemblies)
    {
        LastTestRun = _runTestsCallback(new TestFilter(_filter, _testExclusionTable));
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
        string lastTestResults = LastTestRun.GetTestResultOutput(_assemblyName);
        if (_writeBase64TestResults)
        {
            byte[] encodedBytes = Encoding.Unicode.GetBytes(lastTestResults);
            string base64Results = Convert.ToBase64String(encodedBytes);
            writer.WriteLine($"STARTRESULTXML {encodedBytes.Length} {base64Results} ENDRESULTXML");
        }
        else
        {
            writer.WriteLine(lastTestResults);
        }
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
