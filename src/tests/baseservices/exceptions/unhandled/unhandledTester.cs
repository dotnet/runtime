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

namespace TestUnhandledExceptionTester
{
    public class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            List<string> lines = new List<string>();

            Process testProcess = new Process();

            testProcess.StartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            testProcess.StartInfo.Arguments = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unhandled.dll");
            testProcess.StartInfo.RedirectStandardError = true;
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
            testProcess.CancelErrorRead();

            int expectedExitCode;
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                expectedExitCode = 1;
            }
            else if (!OperatingSystem.IsWindows())
            {
                expectedExitCode = 128 + 6;
            }
            else if (TestLibrary.Utilities.IsNativeAot)
            {
                expectedExitCode = unchecked((int)0xC0000409);
            }
            else
            {
                expectedExitCode = unchecked((int)0xE0434352);
            }

            if (expectedExitCode != testProcess.ExitCode)
            {
                throw new Exception($"Wrong exit code 0x{testProcess.ExitCode:X8}, expected 0x{expectedExitCode:X8}");
            }

            int exceptionStackFrameLine = 1;
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                if (lines[0] != "Unhandled Exception:")
                {
                    throw new Exception("Missing Unhandled exception header");
                }
                if (lines[1] != "System.Exception: Test")
                {
                    throw new Exception("Missing exception type and message");
                }

                exceptionStackFrameLine = 2;
            }
            else
            {
                if (lines[0] != "Unhandled exception. System.Exception: Test")
                {
                    throw new Exception("Missing Unhandled exception header");
                }

            }

            if (!lines[exceptionStackFrameLine].TrimStart().StartsWith("at TestUnhandledException.Program.Main"))
            {
                throw new Exception("Missing exception source frame");
            }
        }
    }
}
