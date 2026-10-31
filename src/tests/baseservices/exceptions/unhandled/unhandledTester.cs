// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Xunit;
using TestLibrary;

namespace TestUnhandledExceptionTester
{
    public class Program
    {
        static void RunExternalProcess(string unhandledType, string assembly)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun"),
                [Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assembly), unhandledType]);
            startInfo.InheritedHandles = [];
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            // Disable crash diagnostics since the target process is expected to fail with an unhandled exception
            startInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
            startInfo.Environment.Remove("DOTNET_EnableCrashReport");

            ProcessTextOutput result = Process.RunAndCaptureText(startInfo);
            Console.WriteLine($"Test process {assembly} with argument {unhandledType} exited");

            List<string> lines = new List<string>();
            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                Console.WriteLine($"\"{line}\"");
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }
            }

            int[] expectedExitCodes;
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                expectedExitCodes = new[] { 1 };
            }
            else if (!OperatingSystem.IsWindows())
            {
                expectedExitCodes = new[] { 128 + 6 }; // SIGABRT
            }
            else if (TestLibrary.Utilities.IsNativeAot)
            {
                expectedExitCodes = new[] { unchecked((int)0xC0000409) };
            }
            else
            {
                if (unhandledType.EndsWith("hardware"))
                {
                    // Null reference exception code
                    expectedExitCodes = new[] { unchecked((int)0xC0000005), unchecked((int)0xE0434352) };
                }
                else if (unhandledType == "collecteddelegate")
                {
                    // Fail fast exit code
                    expectedExitCodes = new[] { unchecked((int)0x80131623) };
                }
                else
                {
                    expectedExitCodes = new[] { unchecked((int)0xE0434352) };
                }
            }

            if (!Array.Exists(expectedExitCodes, code => result.ExitStatus.ExitCode == code))
            {
                string separator = string.Empty;
                StringBuilder expectedListBuilder = new StringBuilder();
                Array.ForEach(expectedExitCodes, code =>
                {
                    expectedListBuilder.Append($"{separator}0x{code:X8}");
                    separator = " or ";
                });
                throw new Exception($"Wrong exit code: 0x{result.ExitStatus.ExitCode:X8}, expected {expectedListBuilder}");
            }

            int exceptionStackFrameLine = 1;
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                if (lines[0] != "Unhandled Exception:")
                {
                    throw new Exception("Missing Unhandled exception header");
                }
                if (unhandledType == "main")
                {
                    if (lines[1] != "System.Exception: Test")
                    {
                        throw new Exception("Missing exception type and message");
                    }
                }
                else if (unhandledType == "foreign")
                {
                    if (lines[1] != "System.EntryPointNotFoundException: HelloCpp")
                    {
                        throw new Exception("Missing exception type and message");
                    }
                }
                else if (unhandledType.EndsWith("hardware"))
                {
                    if (!lines[1].StartsWith("System.NullReferenceException: Object reference not set to an instance of an object"))
                    {
                        throw new Exception("Missing exception type and message");
                    }
                }

                exceptionStackFrameLine = 2;
            }
            else
            {
                if (unhandledType == "main" || unhandledType == "secondary")
                {
                    if (lines[0] != "Unhandled exception. System.Exception: Test")
                    {
                        throw new Exception("Missing Unhandled exception header");
                    }
                }
                if (unhandledType == "mainthreadinterrupted" || unhandledType == "secondarythreadinterrupted")
                {
                    if (lines[0] != "Unhandled exception. System.Threading.ThreadInterruptedException: Test")
                    {
                        throw new Exception("Missing Unhandled exception header");
                    }
                }
                else if (unhandledType == "foreign")
                {
                    if (!lines[0].StartsWith("Unhandled exception. System.DllNotFoundException:") &&
                        !lines[0].StartsWith("Unhandled exception. System.EntryPointNotFoundException: Unable to find an entry point named 'HelloCpp'"))
                    {
                        throw new Exception("Missing Unhandled exception header");
                    }
                }
                else if (unhandledType == "collecteddelegate")
                {
                    if (lines[1] != "A callback was made on a garbage collected delegate of type 'System.Private.CoreLib!System.Action::Invoke'.")
                    {
                        throw new Exception("Missing collected delegate diagnostic");
                    }
                }
            }

            if (unhandledType == "main")
            {
                if (!lines[exceptionStackFrameLine].TrimStart().StartsWith("at TestUnhandledException.Program.Main"))
                {
                    throw new Exception("Missing exception source frame");
                }
            }
            else if (unhandledType == "secondary")
            {
                if (!lines[exceptionStackFrameLine].TrimStart().StartsWith("at TestUnhandledException.Program."))
                {
                    throw new Exception("Missing exception source frame");
                }
            }

            Console.WriteLine("Test process exited with expected error code and produced expected output");
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/80356", typeof(PlatformDetection), nameof(PlatformDetection.IsOSX), nameof(PlatformDetection.IsX64Process))]
        [ActiveIssue("System.Diagnostics.Process is not supported", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [ActiveIssue("Test expects being run with corerun", typeof(Utilities), nameof(Utilities.IsNativeAot))]
        [SkipOnMono("Tests CoreCLR-specific fatal-error reporting.")]
        [Theory]
        [InlineData("A", 1100, false, "utf-8")]
        [InlineData("\u00E9", 1100, false, "utf-8")]
        [InlineData("\u20AC", 4, false, "utf-8")]
        [InlineData("\u20AC", 167, false, "utf-8")]
        [InlineData("\u20AC", 168, false, "utf-8")]
        [InlineData("\u20AC", 1100, false, "utf-8")]
        [InlineData("\uD83D\uDE00", 1100, false, "utf-8")]
        [InlineData("\u20AC", 4, true, "utf-8")]
        [InlineData("\u20AC", 1100, true, "utf-8")]
        [InlineData("A", 1100, false, "us-ascii")]
        public static void FailFast_UnicodeReport_IsWritten(string text, int repetitions, bool exceptionMessage, string encodingName)
        {
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string message = new StringBuilder().Insert(0, text, repetitions)
                .Append(encoding.CodePage == Encoding.UTF8.CodePage ? "\uD83D\uDE00TAIL" : "TAIL").ToString();
            ProcessStartInfo startInfo = new ProcessStartInfo(
                Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun"),
                [Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unhandled.dll"),
                 "failfastreport", message, exceptionMessage.ToString(), encodingName])
            {
                InheritedHandles = [],
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = encoding
            };
            startInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
            startInfo.Environment.Remove("DOTNET_EnableCrashReport");

            // Windows console code pages are shared with child processes.
            Encoding outputEncoding = Console.OutputEncoding;
            try
            {
                Console.WriteLine($"FailFast report: text={text}, repetitions={repetitions}, exceptionMessage={exceptionMessage}, encoding={encodingName}");
                ProcessTextOutput result = Process.RunAndCaptureText(startInfo, TimeSpan.FromMinutes(1));
                Assert.NotEqual(0, result.ExitStatus.ExitCode);
                Assert.Contains(message, result.StandardError);
            }
            finally
            {
                Console.OutputEncoding = outputEncoding;
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/80356", typeof(PlatformDetection), nameof(PlatformDetection.IsOSX), nameof(PlatformDetection.IsX64Process))]
        [ActiveIssue("System.Diagnostics.Process is not supported", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [ActiveIssue("Test expects being run with corerun", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [Fact]
        public static void TestEntryPoint()
        {
            RunExternalProcess("main", "unhandled.dll");
            RunExternalProcess("mainhardware", "unhandled.dll");
            RunExternalProcess("mainthreadinterrupted", "unhandled.dll");
            RunExternalProcess("secondary", "unhandled.dll");
            RunExternalProcess("secondaryhardware", "unhandled.dll");
            RunExternalProcess("secondarythreadinterrupted", "unhandled.dll");
            RunExternalProcess("foreign", "unhandled.dll");
            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            RunExternalProcess("missingdependency", "unhandledmissingdependency.dll");
            if (!TestLibrary.Utilities.IsMonoRuntime && !TestLibrary.Utilities.IsNativeAot)
                RunExternalProcess("collecteddelegate", "collecteddelegate.dll");
        }
    }
}
