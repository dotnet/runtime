// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace TestStackOverflow
{
    public class Program
    {
        static void TestStackOverflow(string testName, string testArgs, out List<string> stderrLines)
        {
            Console.WriteLine($"Running {testName} test({testArgs})");
            List<string> lines = new List<string>();

            Process testProcess = new Process();

            testProcess.StartInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            testProcess.StartInfo.Arguments = $"{Path.Combine(Directory.GetCurrentDirectory(), "..", testName, $"{testName}.dll")} {testArgs}";
            testProcess.StartInfo.UseShellExecute = false;
            testProcess.StartInfo.RedirectStandardError = true;
            testProcess.StartInfo.Environment.Add("DOTNET_DbgEnableMiniDump", "0");
            bool endOfStackTrace = false;
            
            testProcess.ErrorDataReceived += (sender, line) => 
            {
                Console.WriteLine($"\"{line.Data}\"");
                if (!endOfStackTrace && !string.IsNullOrEmpty(line.Data))
                {
                    // Store lines only till the end of the stack trace.
                    // In the CI it can also contain lines with createdump info.
                    if (line.Data.StartsWith("Stack overflow.") ||
                        line.Data.StartsWith("Repeated ") ||
                        line.Data.StartsWith("------") ||
                        line.Data.StartsWith("   at "))
                    {
                        lines.Add(line.Data);
                    }
                    else
                    {
                        endOfStackTrace = true;
                    }
                }
            };

            testProcess.Start();
            testProcess.BeginErrorReadLine();
            testProcess.WaitForExit();
            testProcess.CancelErrorRead();

            stderrLines = lines;

            int[] expectedExitCodes;
            if ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX))
            {
                expectedExitCodes = new int[] { 128 + 6};
            }
            else
            {
                expectedExitCodes = new int[] { unchecked((int)0xC00000FD), unchecked((int)0x800703E9) };
            }

            if (!Array.Exists(expectedExitCodes, code => testProcess.ExitCode == code))
            {
                string separator = string.Empty;
                StringBuilder expectedListBuilder = new StringBuilder();
                Array.ForEach(expectedExitCodes, code => {
                    expectedListBuilder.Append($"{separator}0x{code:X8}");
                    separator = " or ";
                });
                throw new Exception($"Exit code: 0x{testProcess.ExitCode:X8}, expected {expectedListBuilder.ToString()}");
            }

            if (lines[0] != "Stack overflow.")
            {
                throw new Exception("Missing \"Stack overflow.\" at the first line");
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
            if ((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) &&
                ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX)))
            {
                // Disabled on Unix ARM64 due to https://github.com/dotnet/runtime/issues/13519
                // The current stack probing doesn't move the stack pointer and so the runtime sometimes cannot
                // recognize the underlying sigsegv as stack overflow when it probes too far from SP.
                return;
            }

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
            if ((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) &&
                ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX)))
            {
                // Disabled on Unix ARM64 due to https://github.com/dotnet/runtime/issues/13519
                // The current stack probing doesn't move the stack pointer and so the runtime sometimes cannot
                // recognize the underlying sigsegv as stack overflow when it probes too far from SP.
                return;
            }

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
