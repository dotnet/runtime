// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace TestStackOverflow
{
    public class Program: IDisposable
    {
        const string TestNameEnvVar = "STACKOVERFLOWTESTER_TESTNAME";
        const string TestArgsEnvVar = "STACKOVERFLOWTESTER_TESTARGS";

        public Program() {
            string? testName = Environment.GetEnviromentVariable(TestNameEnvVar);
            string[]? testArgs = Environment.GetEnviromentVariable(TestArgsEnvVar)?.Split(' ');
            if (testName != null)
            {
                switch(testName)
                {
                    "stackoverflow":
                        Assert.NotNull(testArgs);
                        StackOverflow.Run(testArgs);
                        break;
                    "stackoverflow3":
                        StackOverflow3.Run();
                        break;
                }
                throw new InvalidOperationException($"Invalid test. Test {testName} with arguments '{testArgs.Join(' ')}' should have thrown an exception.");
            }
        }

        public void Dispose() {}

        const string ThisProjectName = "stackoverflowtester";

        static Process GetNativeAotProcess(string testName, string testArgs)
        {
            Process testProcess = new Process();
            return testProcess;
        }

        static bool TestStackOverflow(string testName, string testArgs, out List<string> stderrLines, out bool checkStackFrame)
        {
            Console.WriteLine($"Running with args: {testName} {testArgs}");
            testArgs = $"{testName} {testArgs}";
            List<string> lines = new List<string>();

            Process testProcess = new Process();
            testProcess.StartInfo.Environment.Add("DOTNET_DbgEnableMiniDump", "0");
            testProcess.StartInfo.Environment.Add(TestNameEnvVar, testName);
            testProcess.StartInfo.Environment.Add(, testArgs);
            // Always use whatever runner started this test
            testProcess.StartInfo.FileName = Environment.ProcessPath;
            if (!TestLibrary.Utilities.IsNativeAot)
            {
                testProcess.StartInfo.Arguments = $"{typeof(Program).Assembly.Location}";
            }

            testProcess.StartInfo.UseShellExecute = false;
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

            stderrLines = lines;
            // NativeAOT doesn't provide a stack trace on stack overflow
            checkStackFrame = !TestLibrary.Utilities.IsNativeAot;

            int[] expectedExitCodes;
            if ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX))
            {
                expectedExitCodes = new int[] { 128 + 6 };
            }
            else
            {
                expectedExitCodes = new int[] { unchecked((int)0xC00000FD), unchecked((int)0x800703E9) };
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
                throw new Exception($"Exit code: 0x{testProcess.ExitCode:X8}, expected {expectedListBuilder.ToString()}");
            }

            string expectedMessage;
            if (TestLibrary.Utilities.IsNativeAot)
            {
                expectedMessage = "Process is terminating due to StackOverflowException.";
            }
            else
            {
                expectedMessage = "Stack overflow.";
            }

            if (lines.Count > 0 && lines[0] == expectedMessage)
            {
                return true;
            }
            else
            {
                throw new Exception($"Missing \"{expectedMessage}\" at the first line");
            }
        }

        [Fact]
        public static void TestStackOverflowSmallFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "smallframe main", out List<string> lines);

            if (!lines[lines.Count - 1].EndsWith(".Main(System.String[])"))
            {
                throw new Exception("Missing \"Main\" method frame at the last line");
            }

            if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.Test(Boolean)")))
            {
                throw new Exception("Missing \"Test\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA()")))
            {
                throw new Exception("Missing \"InfiniteRecursionA\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB()")))
            {
                throw new Exception("Missing \"InfiniteRecursionB\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC()")))
            {
                throw new Exception("Missing \"InfiniteRecursionC\" method frame");
            }
        }

        [Fact]
        public static void TestStackOverflowLargeFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "largeframe main", out List<string> lines);

            if (!lines[lines.Count - 1].EndsWith("at TestStackOverflow.Program.Main(System.String[])"))
            {
                throw new Exception("Missing \"Main\" method frame at the last line");
            }

            if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.Test(Boolean)")))
            {
                throw new Exception("Missing \"Test\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionA2\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionB2\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionC2\" method frame");
            }
        }

        [Fact]
        public static void TestStackOverflowSmallFrameSecondaryThread()
        {
            TestStackOverflow("stackoverflow", "smallframe secondary", out List<string> lines);

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.Test(Boolean)")))
            {
                throw new Exception("Missing \"TestStackOverflow.Program.Test\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA()")))
            {
                throw new Exception("Missing \"InfiniteRecursionA\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB()")))
            {
                throw new Exception("Missing \"InfiniteRecursionB\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC()")))
            {
                throw new Exception("Missing \"InfiniteRecursionC\" method frame");
            }
        }

        [Fact]
        public static void TestStackOverflowLargeFrameSecondaryThread()
        {
            TestStackOverflow("stackoverflow", "largeframe secondary", out List<string> lines);

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.Test(Boolean)")))
            {
                throw new Exception("Missing \"TestStackOverflow.Program.Test\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionA2\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.InfiniteRecursionB2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionB2\" method frame");
            }

            if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.InfiniteRecursionC2()")))
            {
                throw new Exception("Missing \"InfiniteRecursionC2\" method frame");
            }
        }

        [Fact]
        public static void TestStackOverflow3()
        {
            TestStackOverflow("stackoverflow3", "", out List<string> lines);

            if (!lines[lines.Count - 1].EndsWith("at TestStackOverflow3.Program.Main()"))
            {
                throw new Exception("Missing \"Main\" method frame at the last line");
            }

            if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow3.Program.Execute(System.String)")))
            {
                throw new Exception("Missing \"Execute\" method frame");
            }

        }
    }
}
