// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
namespace XUnitWrapperLibrary;

public class TestSummary
{
    public readonly record struct TestResult
    {
        public readonly string Name;
        public readonly string ContainingTypeName;
        public readonly string MethodName;
        public readonly TimeSpan Duration;
        public readonly Exception? Exception;
        public readonly string? SkipReason;
        public readonly string? Output;

        public TestResult(string name,
                          string containingTypeName,
                          string methodName,
                          TimeSpan duration,
                          Exception? exception,
                          string? skipReason,
                          string? output)
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
                                 ? $"<output><![CDATA[{XmlConvert.EncodeName(Output)}]]></output>"
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

    public int PassedTests { get; private set; }
    public int FailedTests { get; private set; }
    public int SkippedTests { get; private set; }
    public int TotalTests { get; private set; }

    private readonly List<TestResult> _testResults = new();
    private DateTime _testRunStart = DateTime.Now;

    public void WriteHeaderToTempLog(string assemblyName, StreamWriter tempLogSw)
    {
        // We are writing down both, date and time, in the same field here because
        // it's much simpler to parse later on in the XUnitLogChecker.
        tempLogSw.WriteLine("<assembly\n"
                        + $"    name=\"{assemblyName}\"\n"
                        + $"    test-framework=\"XUnitWrapperGenerator-generated-runner\"\n"
                        + $"    run-date-time=\"{_testRunStart.ToString("yyyy-MM-dd HH:mm:ss")}\">");
    }

    public void WriteFooterToTempLog(StreamWriter tempLogSw)
    {
        tempLogSw.WriteLine("</assembly>");
    }

    public void ReportPassedTest(string name,
                                 string containingTypeName,
                                 string methodName,
                                 TimeSpan duration,
                                 string output,
                                 StreamWriter tempLogSw,
                                 StreamWriter statsCsvSw)
    {
        PassedTests++;
        TotalTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, null, null, output);
        _testResults.Add(result);

        statsCsvSw.WriteLine($"{TotalTests},{PassedTests},{FailedTests},{SkippedTests}");
        tempLogSw.WriteLine(result.ToXmlString());
        statsCsvSw.Flush();
        tempLogSw.Flush();
    }

    public void ReportFailedTest(string name,
                                 string containingTypeName,
                                 string methodName,
                                 TimeSpan duration,
                                 Exception ex,
                                 string output,
                                 StreamWriter tempLogSw,
                                 StreamWriter statsCsvSw)
    {
        FailedTests++;
        TotalTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, ex, null, output);
        _testResults.Add(result);

        statsCsvSw.WriteLine($"{TotalTests},{PassedTests},{FailedTests},{SkippedTests}");
        tempLogSw.WriteLine(result.ToXmlString());
        statsCsvSw.Flush();
        tempLogSw.Flush();
    }

    public void ReportSkippedTest(string name,
                                  string containingTypeName,
                                  string methodName,
                                  TimeSpan duration,
                                  string reason,
                                  StreamWriter tempLogSw,
                                  StreamWriter statsCsvSw)
    {
        SkippedTests++;
        TotalTests++;
        var result = new TestResult(name, containingTypeName, methodName, duration, null, reason, null);
        _testResults.Add(result);

        statsCsvSw.WriteLine($"{TotalTests},{PassedTests},{FailedTests},{SkippedTests}");
        tempLogSw.WriteLine(result.ToXmlString());
        statsCsvSw.Flush();
        tempLogSw.Flush();
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
    run-date=""{_testRunStart.ToString("yyyy-MM-dd")}""
    run-time=""{_testRunStart.ToString("HH:mm:ss")}""
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
