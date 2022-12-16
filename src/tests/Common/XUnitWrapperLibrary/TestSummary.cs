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
                                 : string.Empty

            if (Exception is not null)
            {
                // Append the exception here :)
            }
            else if (SkipReason is not null)
            {
                testResultSb.Append($@" result=""Skip""><reason><![CDATA[");

                testResultSb.Append(!string.IsNullOrWhiteSpace(SkipReason)
                                    ? SkipReason
                                    : "No Known Skip Reason");

                testResultSb.AppendLine("]]</reason></test>");
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
    private string _assemblyName = "NoSpecifiedAssembly";

    // public TestSummary(string assemblyName)
    // {
    //     _assemblyName = assemblyName;
    // }

    public void ReportPassedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string output, StreamWriter tempLogSw)
    {
        PassedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, null, output));
    }

    public void ReportFailedTest(string name, string containingTypeName, string methodName, TimeSpan duration, Exception ex, string output, StreamWriter tempLogSw)
    {
        FailedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, ex, null, output));
    }

    public void ReportSkippedTest(string name, string containingTypeName, string methodName, TimeSpan duration, string reason, StreamWriter tempLogSw)
    {
        SkippedTests++;
        _testResults.Add(new TestResult(name, containingTypeName, methodName, duration, null, reason, null));
    }

    // public void OpenTempLog()
    // {
    //     using (StreamWriter sw = File.CreateText($"{_assemblyName}_templog.xml"))
    //     {
    //         sw.WriteLine("<tests>");
    //     }
    // }

    // public void CloseTempLog()
    // {
    //     using (StreamWriter sw = File.AppendText($"{_assemblyName}_templog.xml"))
    //     {
    //         sw.WriteLine("</tests>");
    //     }
    // }

    // NOTE: This will likely change with the existence of the temp log.
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
                string exceptionMessage = test.Exception.Message;
                if (test.Exception is System.Reflection.TargetInvocationException tie)
                {
                    if (tie.InnerException != null)
                    {
                        exceptionMessage = $"{exceptionMessage} \n INNER EXCEPTION--\n {tie.InnerException.GetType()}--\n{tie.InnerException.Message}--\n{tie.InnerException.StackTrace}";
                    }
                }
                if (string.IsNullOrWhiteSpace(exceptionMessage))
                {
                    exceptionMessage = "NoExceptionMessage";
                }

                string? stackTrace = test.Exception.StackTrace;
                if (string.IsNullOrWhiteSpace(stackTrace))
                {
                    stackTrace = "NoStackTrace";
                }
                resultsFile.AppendLine($@"result=""Fail""><failure exception-type=""{test.Exception.GetType()}""><message><![CDATA[{exceptionMessage}]]></message><stack-trace><![CDATA[{stackTrace}]]></stack-trace></failure>{outputElement}</test>");
            }
            else if (test.SkipReason is not null)
            {
                resultsFile.AppendLine($@"result=""Skip""><reason><![CDATA[{(!string.IsNullOrWhiteSpace(test.SkipReason) ? test.SkipReason : "No Known Skip Reason")}]]></reason></test>");
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
