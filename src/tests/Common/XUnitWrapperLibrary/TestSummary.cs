// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Text;
namespace XUnitWrapperLibrary;

public class TestSummary
{
    readonly record struct TestResult(string Name, string ContainingTypeName, string MethodName, TimeSpan Duration, Exception? Exception, string? SkipReason);

    private int _numPassed = 0;

    private int _numFailed = 0;

    private int _numSkipped = 0;

    private readonly List<TestResult> _testResults = new();

    private DateTime _testRunStart = DateTime.Now;

    public void ReportPassedTest(string name, string containingTypeName, string methodName, TimeSpan duration)
    {
        _numPassed++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, null));
    }

    public void ReportFailedTest(string name, string containingTypeName, string methodName, TimeSpan duration, Exception ex)
    {
        _numFailed++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, ex, null));
    }

    public void ReportSkippedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string reason)
    {
        _numSkipped++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, reason));
    }

    public string GetTestResultOutput()
    {
        double totalRunSeconds = (DateTime.Now - _testRunStart).TotalSeconds;
        // using StringBuilder here for simplicity of loaded IL.
        StringBuilder resultsFile = new();
        resultsFile.AppendLine("<assemblies>");
        resultsFile.AppendLine($@"
<assembly
    name=""""
    test-framework=""XUnitWrapperGenerator-generated-runner""
    run-date=""{_testRunStart.ToString("yyy-mm-dd")}""
    run-time=""{_testRunStart.ToString("hh:mm:ss")}""
    time=""{totalRunSeconds}""
    total=""{_testResults.Count}""
    passed=""{_numPassed}""
    failed=""{_numFailed}""
    skipped=""{_numSkipped}""
    errors=""0"">");

        resultsFile.AppendLine($@"
<collection
    name=""Collection""
    time=""{totalRunSeconds}""
    total=""{_testResults.Count}""
    passed=""{_numPassed}""
    failed=""{_numFailed}""
    skipped=""{_numSkipped}""
    errors=""0""
>");

        foreach (var test in _testResults)
        {
            resultsFile.Append($@"<test name=""{test.Name}"" type=""{test.ContainingTypeName}"" method=""{test.MethodName}"" time=""{test.Duration}"" ");
            if (test.Exception is not null)
            {
                resultsFile.AppendLine($@"result=""Fail""><failure exception-type=""{test.Exception.GetType()}""><message><![CDATA[{test.Exception.Message}]]></message><stack-trace><![CDATA[{test.Exception.StackTrace}]]></stack-trace></failure></test>");
            }
            else if (test.SkipReason is not null)
            {
                resultsFile.AppendLine($@"result=""Skip""><reason><![CDATA[{test.SkipReason}]]></reason></test>");
            }
            else
            {
                resultsFile.AppendLine(@" result=""Pass"" />");
            }
        }

        resultsFile.AppendLine("</collection>");
        resultsFile.AppendLine("</assembly>");
        resultsFile.AppendLine("</assemblies>");

        return resultsFile.ToString();
    }
}
