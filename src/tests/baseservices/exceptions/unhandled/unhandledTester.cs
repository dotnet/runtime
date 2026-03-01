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
            List<string> lines = new List<string>();

            Process testProcess = new Process();

            testProcess.StartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            testProcess.StartInfo.Arguments = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assembly) + " " + unhandledType;
            testProcess.StartInfo.RedirectStandardError = true;
            // Disable creating dump since the target process is expected to fail with an unhandled exception
            testProcess.StartInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
            testProcess.ErrorDataReceived += (sender, line) => 
            {
                Console.WriteLine($"\"{line.Data}\"");
                if (!string.IsNullOrEmpty(line.Data))
                {
                    lines.Add(line.Data);
                }
            };

            testProcess.Start();
            testProcess.BeginErrorReadLine();
            testProcess.WaitForExit();
            Console.WriteLine($"Test process {assembly} with argument {unhandledType} exited");
            testProcess.CancelErrorRead();

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

            if (!Array.Exists(expectedExitCodes, code => testProcess.ExitCode == code))
            {
                string separator = string.Empty;
                StringBuilder expectedListBuilder = new StringBuilder();
                Array.ForEach(expectedExitCodes, code =>
                {
                    expectedListBuilder.Append($"{separator}0x{code:X8}");
                    separator = " or ";
                });
                throw new Exception($"Wrong exit code: 0x{testProcess.ExitCode:X8}, expected {expectedListBuilder}");
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
