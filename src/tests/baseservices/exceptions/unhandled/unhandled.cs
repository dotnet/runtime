// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TestUnhandledException
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 0)
            {
                throw new Exception("Test");
            }

            List<string> lines = new List<string>();

            Process testProcess = new Process();

            // We don't need to trigger createdump logic.
            testProcess.StartInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");

            testProcess.StartInfo.FileName = Environment.ProcessPath;
            testProcess.StartInfo.Arguments = Environment.CommandLine + " throw";
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
                Console.WriteLine($"Wrong exit code 0x{testProcess.ExitCode:X8}");
                return 101;
            }

            int exceptionStackFrameLine = 1;
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                if (lines[0] != "Unhandled Exception:")
                {
                    Console.WriteLine("Missing Unhandled exception header");
                    return 102;
                }
                if (lines[1] != "System.Exception: Test")
                {
                    Console.WriteLine("Missing exception type and message");
                    return 103;
                }

                exceptionStackFrameLine = 2;
            }
            else
            {
                if (lines[0] != "Unhandled exception. System.Exception: Test")
                {
                    Console.WriteLine("Missing Unhandled exception header");
                    return 102;
                }

            }

            if (!lines[exceptionStackFrameLine].TrimStart().StartsWith("at TestUnhandledException.Program.Main"))
            {
                Console.WriteLine("Missing exception source frame");
                return 103;
            }

            return 100;
        }
    }
}
