// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace TestStackOverflow
{
    public class Program : IDisposable
    {
        const string TestNameEnvVar = "STACKOVERFLOWTESTER_TESTNAME";
        const string TestArgsEnvVar = "STACKOVERFLOWTESTER_TESTARGS";
        const string EnableMiniDumpEnvVar = "DOTNET_DbgEnableMiniDump";

        static bool s_checkedEnv = false;
        public Program()
        {
            // Called before every non-static [Fact]
            if (s_checkedEnv)
            {
                return;
            }
            // Use the environment variable to determine if we should trigger a stack overflow
            string? testName = Environment.GetEnvironmentVariable(TestNameEnvVar);
            string? testArgs = Environment.GetEnvironmentVariable(TestArgsEnvVar);
            Console.WriteLine(testName);
            if (testName != null)
            {
                switch (testName)
                {
                    case "stackoverflow":
                        Assert.NotNull(testArgs);
                        StackOverflow.Run(testArgs.Split(' '));
                        break;
                    case "stackoverflow3":
                        StackOverflow3.Run();
                        break;
                }
                throw new InvalidOperationException($"Invalid test. Test {testName} with arguments '{testArgs}' should have thrown an exception.");
            }
            s_checkedEnv = true;
        }

        public void Dispose() { }

        [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "We want an empty string for location if the test is running as single file")]
        static bool TestStackOverflow(string testName, string testArgs, out string[] stderrLines, out bool checkStackFrame)
        {
            List<string> lines = new List<string>();

            Process testProcess = new Process();
            // Always use whatever runner started this test, or the exe if we're running single file / NativeAOT
            testProcess.StartInfo.FileName = Environment.ProcessPath;
            // We want this assembly in CoreCLR and empty string for single file / NativeAOT
            testProcess.StartInfo.Arguments = typeof(Program).Assembly.Location;
            testProcess.StartInfo.Environment.Add(EnableMiniDumpEnvVar, "0");
            // Set the environment so the subprocess will trigger a stack overflow
            testProcess.StartInfo.Environment.Add(TestNameEnvVar, testName);
            testProcess.StartInfo.Environment.Add(TestArgsEnvVar, testArgs);
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

            stderrLines = lines.ToArray();

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

            throw new Exception($"Missing \"{expectedMessage}\" at the first line of stderr");
        }

        public static void AssertStackFramePresent(string stackFrame, ReadOnlySpan<string> lines)
        {
            foreach(var line in lines)
            {
                if (line.EndsWith(stackFrame))
                    return;
            }
            throw new Exception($"Missing \"{stackFrame}\" from stack trace");
        }

        [Fact]
        public void TestStackOverflowSmallFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "smallframe main", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent(".Main()", lines[(lines.Length-1)..]);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Run(System.String[])", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC()", lines);
        }

        [Fact]
        public void TestStackOverflowLargeFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "largeframe main", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            if (!lines[lines.Length - 1].EndsWith(".Main()"))
            {
                throw new Exception("Missing \"Main\" method frame at the last line");
            }
            AssertStackFramePresent(".Main()", lines[(lines.Length-1)..]);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Run(System.String[])", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionA2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC2()", lines);
        }

        [Fact]
        public void TestStackOverflowSmallFrameSecondaryThread()
        {
            TestStackOverflow("stackoverflow", "smallframe secondary", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionA()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC()", lines);
        }

        [Fact]
        public void TestStackOverflowLargeFrameSecondaryThread()
        {
            TestStackOverflow("stackoverflow", "largeframe secondary", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionA2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC2()", lines);
        }

        [Fact]
        public void TestStackOverflow3()
        {
            TestStackOverflow("stackoverflow3", "", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent(".Main()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow3.Run()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow3.Execute(System.String)", lines);
        }
    }
}
