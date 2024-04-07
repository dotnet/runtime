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

                exceptionStackFrameLine = 2;
            }
            else
            {
                if (unhandledType == "main")
                {
                    if (lines[0] != "Unhandled exception. System.Exception: Test")
                    {
                        throw new Exception("Missing Unhandled exception header");
                    }
                }
                else if (unhandledType == "foreign")
                {
                    if (!lines[0].StartsWith("Unhandled exception. System.DllNotFoundException:"))
                    {
                        throw new Exception("Missing Unhandled exception header");
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
        }

        [Fact]
        public static void TestEntryPoint()
        {
            RunExternalProcess("main", "unhandled.dll");
            RunExternalProcess("foreign", "unhandled.dll");
	    File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            RunExternalProcess("missingdependency", "unhandledmissingdependency.dll");
        }
    }
}
