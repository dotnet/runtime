// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Text;
namespace XUnitWrapperLibrary;

public class TestSummary
{
    readonly record struct TestResult(string Name, string ContainingTypeName, string MethodName, TimeSpan Duration, Exception? Exception, string? SkipReason, string? Output);

    public int PassedTests { get; private set; } = 0;
    public int FailedTests { get; private set; } = 0;
    public int SkippedTests { get; private set; } = 0;

    private readonly List<TestResult> _testResults = new();

    private DateTime _testRunStart = DateTime.Now;

    public void ReportPassedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string output)
    {
        PassedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, null, output));
    }

    public void ReportFailedTest(string name, string containingTypeName, string methodName, TimeSpan duration, Exception ex, string output)
    {
        FailedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, ex, null, output));
    }

    public void ReportSkippedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string reason)
    {
        SkippedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, reason, null));
    }

    public string GetTestResultOutput(string assemblyName)
    {
        double totalRunSeconds = (DateTime.Now - _testRunStart).TotalSeconds;
        // using StringBuilder here for simplicity of loaded IL.
        StringBuilder resultsFile = new();
        resultsFile.AppendLine("<assemblies>");
        resultsFile.AppendLine($@"
<assembly
    name=""{assemblyName}""
    test-framework=""XUnitWrapperGenerator-generated-runner""
    run-date=""{_testRunStart.ToString("yyy-mm-dd")}""
    run-time=""{_testRunStart.ToString("hh:mm:ss")}""
    time=""{totalRunSeconds}""
    total=""{_testResults.Count}""
    passed=""{PassedTests}""
    failed=""{FailedTests}""
    skipped=""{SkippedTests}""
    errors=""0"">");

        resultsFile.AppendLine($@"
<collection
    name=""Collection""
    time=""{totalRunSeconds}""
    total=""{_testResults.Count}""
    passed=""{PassedTests}""
    failed=""{FailedTests}""
    skipped=""{SkippedTests}""
    errors=""0""
>");

        foreach (var test in _testResults)
        {
            resultsFile.Append($@"<test name=""{test.Name}"" type=""{test.ContainingTypeName}"" method=""{test.MethodName}"" time=""{test.Duration.TotalSeconds:F6}"" ");
            string outputElement = !string.IsNullOrWhiteSpace(test.Output) ? $"<output><![CDATA[{test.Output}]]></output>" : string.Empty;
            if (test.Exception is not null)
            {
                resultsFile.AppendLine($@"result=""Fail""><failure exception-type=""{test.Exception.GetType()}""><message><![CDATA[{test.Exception.Message}]]></message><stack-trace><![CDATA[{test.Exception.StackTrace}]]></stack-trace></failure>{outputElement}</test>");
            }
            else if (test.SkipReason is not null)
            {
                resultsFile.AppendLine($@"result=""Skip""><reason><![CDATA[{test.SkipReason}]]></reason></test>");
            }
            else
            {
                resultsFile.AppendLine($@" result=""Pass"">{outputElement}</test>");
            }
        }

        resultsFile.AppendLine("</collection>");
        resultsFile.AppendLine("</assembly>");
        resultsFile.AppendLine("</assemblies>");

        return resultsFile.ToString();
    }
}
