// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
namespace XUnitWrapperLibrary;

public class TestSummary
{
    // readonly record struct TestResult(string Name, string ContainingTypeName, string MethodName, TimeSpan Duration, Exception? Exception, string? SkipReason, string? Output);
    readonly record struct TestResult
    {
        readonly string Name;
        readonly string ContainingTypeName;
        readonly string MethodName;
        readonly TimeSpan Duration;
        readonly Exception? Exception;
        readonly string? SkipReason;
        readonly string? Output;

        public TestResult(string name, string containingTypeName, string methodName, TimeSpan duration, Exception? exception, string? skipReason, string? output)
        {
            Name = name;
            ContainingTypeName = containingTypeName;
            MethodName = methodName;
            Duration = duration;
            Exception = exception;
            SkipReason = skipReason;
            Output = output;
        }

        public string ToXmlString()
        {
            var testResultSb = new StringBuilder();
            testResultSb.Append($@"<test name=""{Name}"" type=""{ContainingTypeName}"""
                              + $@" method=""{MethodName}"" time=""{Duration.TotalSeconds:F6}""");

            string outputElement = !string.IsNullOrWhiteSpace(Output)
                                 ? $"<output><![CDATA[{Output}]]></output>"
                                 : string.Empty;

            if (Exception is not null)
            {
                string? message = Exception.Message;

                if (Exception is System.Reflection.TargetInvocationException tie)
                {
                    if (tie.InnerException is not null)
                    {
                        message = $"{message}\n INNER EXCEPTION--\n"
                            + $"{tie.InnerException.GetType()}--\n"
                            + $"{tie.InnerException.Message}--\n"
                            + $"{tie.InnerException.StackTrace}";
                    }
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "NoExceptionMessage";
                }

                testResultSb.Append($@" result=""Fail"">"
                                  + $@"<failure exception-type=""{Exception.GetType()}"">"
                                  + $"<message><![CDATA[{message}]]></message>"
                                  + "<stack-trace><![CDATA[");

                testResultSb.Append(!string.IsNullOrWhiteSpace(Exception.StackTrace)
                                    ? Exception.StackTrace
                                    : "NoStackTrace");

                testResultSb.AppendLine($"]]></stack-trace></failure>{outputElement}</test>");
            }
            else if (SkipReason is not null)
            {
                testResultSb.Append($@" result=""Skip""><reason><![CDATA[");

                testResultSb.Append(!string.IsNullOrWhiteSpace(SkipReason)
                                    ? SkipReason
                                    : "No Known Skip Reason");

                testResultSb.AppendLine("]]></reason></test>");
            }
            else
            {
                testResultSb.AppendLine($@" result=""Pass"">{outputElement}</test>");
            }

            return testResultSb.ToString();
        }
    }

    public int PassedTests { get; private set; } = 0;
    public int FailedTests { get; private set; } = 0;
    public int SkippedTests { get; private set; } = 0;

    private readonly List<TestResult> _testResults = new();
    private DateTime _testRunStart = DateTime.Now;

    public void ReportPassedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string output, StreamWriter tempLogSw)
    {
        PassedTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, null, null, output);
        _testResults.Add(result);
        tempLogSw.WriteLine(result.ToXmlString());
    }

    public void ReportFailedTest(string name, string containingTypeName, string methodName, TimeSpan duration, Exception ex, string output, StreamWriter tempLogSw)
    {
        FailedTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, ex, null, output);
        _testResults.Add(result);
        tempLogSw.WriteLine(result.ToXmlString());
    }

    public void ReportSkippedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string reason, StreamWriter tempLogSw)
    {
        SkippedTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, null, reason, null);
        _testResults.Add(result);
        tempLogSw.WriteLine(result.ToXmlString());
    }

    // NOTE: This will likely change or be removed altogether with the existence of the temp log.
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
            resultsFile.AppendLine(test.ToXmlString());
        }

        resultsFile.AppendLine("</collection>");
        resultsFile.AppendLine("</assembly>");
        resultsFile.AppendLine("</assemblies>");

        return resultsFile.ToString();
    }
}
