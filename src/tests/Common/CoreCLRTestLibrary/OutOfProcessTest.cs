// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace TestLibrary
{
    public sealed class OutOfProcessTestSkippedException : Exception
    {
        public OutOfProcessTestSkippedException(string message)
            : base(message)
        {
        }
    }

    public static class OutOfProcessTest
    {
        private const string OutOfProcessPlanFileEnvironmentVariable = "__TestOutOfProcessPlanFile";
        private const string OutOfProcessResultFileSuffix = ".outofprocess-result";
        private const string OutOfProcessResultFormatVersion = "2";
        private const string OutOfProcessResultPassed = "Pass";
        private const string OutOfProcessResultSkipped = "Skip";
        private const string OutOfProcessResultTokenEnvironmentVariable = "__TestOutOfProcessResultToken";
        private const string OutOfProcessStatusFileEnvironmentVariable = "__TestOutOfProcessStatusFile";

        internal static bool runningInWindows;
        internal static string reportBase;
        internal static string testBinaryBase;
        internal static string helixUploadRoot;

        static OutOfProcessTest()
        {
            reportBase = Directory.GetCurrentDirectory();
            testBinaryBase = AppContext.BaseDirectory;
            helixUploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (!String.IsNullOrEmpty(helixUploadRoot))
            {
                reportBase = Path.Combine(Path.GetFullPath(helixUploadRoot), "Reports");
            }

            if (String.IsNullOrEmpty(reportBase))
            {
                reportBase = Path.Combine(testBinaryBase, "Reports");
            }
            else
            {
                reportBase = Path.GetFullPath(reportBase);
            }
        }

        public static string? OutOfProcessPlanFile
        {
            get
            {
                string? planFile = Environment.GetEnvironmentVariable(OutOfProcessPlanFileEnvironmentVariable);
                return String.IsNullOrEmpty(planFile) ? null : planFile;
            }
        }

        public static bool IsUsingPrecomputedResults =>
            !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutOfProcessResultTokenEnvironmentVariable));

        public static bool OutOfProcessTestsSupported =>
            IsUsingPrecomputedResults
            || (!OperatingSystem.IsIOS()
                && !OperatingSystem.IsTvOS()
                && !OperatingSystem.IsAndroid()
                && !OperatingSystem.IsBrowser()
                && !OperatingSystem.IsWasi());

        public static void RunOutOfProcessTest(string assemblyPath, string testPathPrefix)
        {
            if (IsUsingPrecomputedResults)
            {
                ImportPrecomputedResult(assemblyPath, testPathPrefix);
                return;
            }

            int ret = -100;
            string baseDir = AppContext.BaseDirectory;
            string outputDir = System.IO.Path.GetFullPath(Path.Combine(reportBase, Path.GetDirectoryName(assemblyPath) ?? string.Empty));
            string outputFile = Path.Combine(outputDir, "output.txt");
            string errorFile = Path.Combine(outputDir, "error.txt");
            string statusFile = Path.Combine(outputDir, Path.GetFileName(assemblyPath) + ".outofprocess-status");
            string testExecutable = null;
            string skipReason = null;
            Exception infraEx = null;

            try
            {
                CoreclrTestWrapperLib wrapper = new CoreclrTestWrapperLib();

                string testScriptPath = assemblyPath;
                if (testPathPrefix != null)
                    testScriptPath = Path.Combine(testPathPrefix, testScriptPath);

                if (OperatingSystem.IsWindows())
                {
                    testExecutable = Path.Combine(baseDir, Path.ChangeExtension(testScriptPath, ".cmd"));
                }
                else
                {
                    testExecutable = Path.Combine(baseDir, Path.ChangeExtension(testScriptPath.Replace("\\", "/"), ".sh"));
                }

                if (!File.Exists(testExecutable))
                {
                    throw new OutOfProcessTestSkippedException(
                        $"Test executable '{testExecutable}' was not found on this platform.");
                }

                System.IO.Directory.CreateDirectory(outputDir);
                File.Delete(statusFile);

                string previousStatusFile = Environment.GetEnvironmentVariable(OutOfProcessStatusFileEnvironmentVariable);
                try
                {
                    Environment.SetEnvironmentVariable(OutOfProcessStatusFileEnvironmentVariable, statusFile);
                    ret = wrapper.RunTest(testExecutable, outputFile, errorFile, Assembly.GetEntryAssembly()!.FullName!, testBinaryBase, outputDir);
                }
                finally
                {
                    Environment.SetEnvironmentVariable(OutOfProcessStatusFileEnvironmentVariable, previousStatusFile);
                }

                if (File.Exists(statusFile))
                {
                    skipReason = File.ReadAllText(statusFile).TrimEnd('\r', '\n');
                    File.Delete(statusFile);
                }
            }
            catch (OutOfProcessTestSkippedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                infraEx = ex;
            }

            if (infraEx != null)
            {
                Assert.Fail("Test Infrastructure Failure: " + infraEx.ToString());
            }
            else
            {
                List<string> testOutput = new List<string>();

                try
                {
                    testOutput.AddRange(System.IO.File.ReadAllLines(errorFile));
                }
                catch (Exception ex)
                {
                    testOutput.Add("Unable to read error file: " + errorFile);
                    testOutput.Add(ex.ToString());
                }

                testOutput.Add(string.Empty);
                testOutput.Add("Return code:      " + ret);
                testOutput.Add("Raw output file:      " + outputFile);
                testOutput.Add("Raw output:");

                try
                {
                    testOutput.AddRange(System.IO.File.ReadAllLines(outputFile));
                }
                catch (Exception ex)
                {
                    testOutput.Add("Unable to read output file: " + outputFile);
                    testOutput.Add(ex.ToString());
                }

                testOutput.Add("To run the test:");
                testOutput.Add("Set up CORE_ROOT and run.");
                testOutput.Add("> " + testExecutable);

                var unicodeControlCharsRegex = new Regex("%5C%5Cp{C}+");

                // Remove all characters that have no visual or spatial representation.
                for (int i = 0; i < testOutput.Count; i++)
                {
                    string line = testOutput[i];
                    line = unicodeControlCharsRegex.Replace(line, string.Empty);
                    testOutput[i] = line;
                }

                foreach (string line in testOutput)
                {
                    Console.WriteLine(line);
                }

                Assert.True(ret == CoreclrTestWrapperLib.EXIT_SUCCESS_CODE, string.Join(Environment.NewLine, testOutput));

                if (skipReason is not null)
                {
                    throw new OutOfProcessTestSkippedException(
                        skipReason.Length != 0 ? skipReason : "Out-of-process test was skipped.");
                }
            }
        }

        private static void ImportPrecomputedResult(string assemblyPath, string testPathPrefix)
        {
            string resultToken = Environment.GetEnvironmentVariable(OutOfProcessResultTokenEnvironmentVariable);
            if (String.IsNullOrEmpty(resultToken))
            {
                Assert.Fail($"Test Infrastructure Failure: Environment variable '{OutOfProcessResultTokenEnvironmentVariable}' is not set.");
            }

            string testAssemblyPath = assemblyPath;
            if (testPathPrefix != null)
            {
                testAssemblyPath = Path.Combine(testPathPrefix, testAssemblyPath);
            }

            if (!OperatingSystem.IsWindows())
            {
                testAssemblyPath = testAssemblyPath.Replace("\\", "/");
            }

            string resultFile = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), testAssemblyPath)) + OutOfProcessResultFileSuffix;
            if (!File.Exists(resultFile))
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' was not found.");
            }

            using StreamReader resultReader = File.OpenText(resultFile);
            string formatVersion = resultReader.ReadLine();
            string actualResultToken = resultReader.ReadLine();
            string exitCodeText = resultReader.ReadLine();
            string resultStatus = resultReader.ReadLine();
            string skipReason = resultReader.ReadLine();
            string output = resultReader.ReadToEnd();

            if (!String.Equals(formatVersion, OutOfProcessResultFormatVersion, StringComparison.Ordinal))
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' has unsupported format version '{formatVersion}'.");
            }

            if (!String.Equals(actualResultToken, resultToken, StringComparison.Ordinal))
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' is stale or belongs to another run.");
            }

            if (!Int32.TryParse(exitCodeText, out int exitCode))
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' contains invalid exit code '{exitCodeText}'.");
            }

            if (!String.Equals(resultStatus, OutOfProcessResultPassed, StringComparison.Ordinal)
                && !String.Equals(resultStatus, OutOfProcessResultSkipped, StringComparison.Ordinal))
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' contains invalid result status '{resultStatus}'.");
            }

            if (skipReason is null)
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' does not contain a skip reason line.");
            }

            if (String.Equals(resultStatus, OutOfProcessResultPassed, StringComparison.Ordinal)
                && skipReason.Length != 0)
            {
                Assert.Fail($"Test Infrastructure Failure: Out-of-process result file '{resultFile}' contains a skip reason for a passing test.");
            }

            Console.WriteLine($"Out-of-process result file: {resultFile}");
            Console.Write(output);
            if (output.Length != 0 && output[output.Length - 1] != '\n')
            {
                Console.WriteLine();
            }
            Console.WriteLine($"Return code:      {exitCode}");

            Assert.True(exitCode == CoreclrTestWrapperLib.EXIT_SUCCESS_CODE,
                        $"Out-of-process wrapper failed with exit code {exitCode}.{Environment.NewLine}{output}");

            if (String.Equals(resultStatus, OutOfProcessResultSkipped, StringComparison.Ordinal))
            {
                throw new OutOfProcessTestSkippedException(
                    skipReason.Length != 0 ? skipReason : "Out-of-process test was skipped.");
            }
        }
    }
}
