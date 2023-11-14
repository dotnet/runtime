// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace TestStackOverflow
{
    public class Program
    {
        const string EnableMiniDumpEnvVar = "DOTNET_DbgEnableMiniDump";

        const string MainSignature = ".Main(System.String[])";

        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                string testName = args[0];
                string[] testArgs = Array.Empty<string>();
                switch (testName)
                {
                    case "stackoverflow":
                        Assert.True(args.Length >= 2);
                        testArgs = args[1..].ToArray();
                        StackOverflow.Run(testArgs);
                        break;
                    case "stackoverflow3":
                        StackOverflow3.Run();
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid arguments to 'stackoverflowtester' process. Test '{testName}' does not exist");
                }
                throw new InvalidOperationException($"Invalid arguments to 'stackoverflowtester' process. Test '{testName}' with arguments '{testArgs}' should have thrown an exception.");
            }

            int exitCode = 101;
            try
            {
                TestStackOverflowSmallFrameMainThread();
                exitCode++;
                TestStackOverflowLargeFrameMainThread();
                exitCode++;
                TestStackOverflowSmallFrameSecondaryThread();
                exitCode++;
                TestStackOverflowLargeFrameSecondaryThread();
                exitCode++;
                TestStackOverflow3();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return exitCode;
            }

            return 100;
        }

        [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "We want an empty string for location if the test is running as single file")]
        static bool TestStackOverflow(string testName, string testArgs, out string[] stderrLines, out bool checkStackFrame)
        {
            List<string> lines = new List<string>();

            string thisAssemblyPath = typeof(Program).Assembly.Location;
            bool isSingleFile = TestLibrary.Utilities.IsSingleFile;

            Process testProcess = new Process();
            // Always use whatever runner started this test, or the exe if we're running single file / NativeAOT
            testProcess.StartInfo.FileName = Environment.ProcessPath;
            // We want the path to this assembly in CoreCLR and the empty string for single file / NativeAOT
            testProcess.StartInfo.Arguments = $"{typeof(Program).Assembly.Location} {testName} {testArgs}";
            Console.WriteLine($"Running {testName} {testArgs}");
            testProcess.StartInfo.Environment.Add(EnableMiniDumpEnvVar, "0");
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
            checkStackFrame = !isSingleFile;

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
            if (isSingleFile)
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

        public static void TestStackOverflowSmallFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "smallframe main", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent(MainSignature, lines[(lines.Length-1)..]);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Run(System.String[])", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC()", lines);
        }

        public static void TestStackOverflowLargeFrameMainThread()
        {
            TestStackOverflow("stackoverflow", "largeframe main", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            if (!lines[lines.Length - 1].EndsWith(MainSignature))
            {
                throw new Exception("Missing \"Main\" method frame at the last line");
            }
            AssertStackFramePresent(MainSignature, lines[(lines.Length-1)..]);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Run(System.String[])", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.Test(Boolean)", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionA2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionB2()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow.InfiniteRecursionC2()", lines);
        }

        public static void TestStackOverflowSmallFrameSecondaryThread()
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

        public static void TestStackOverflowLargeFrameSecondaryThread()
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

        public static void TestStackOverflow3()
        {
            TestStackOverflow("stackoverflow3", "", out string[] lines, out bool checkStackFrame);

            if (!checkStackFrame)
            {
                return;
            }

            AssertStackFramePresent(MainSignature, lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow3.Run()", lines);
            AssertStackFramePresent("at TestStackOverflow.StackOverflow3.Execute(System.String)", lines);
        }
    }
}
