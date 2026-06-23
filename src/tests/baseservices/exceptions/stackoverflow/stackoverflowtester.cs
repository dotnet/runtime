// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using TestLibrary;

namespace TestStackOverflow
{
    public class Program
    {
        static void TestStackOverflow(string testName, string testArgs, out List<string> stderrLines)
        {
            Console.WriteLine($"Running {testName} test({testArgs})");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            startInfo.Arguments = $"{Path.Combine(Directory.GetCurrentDirectory(), "..", testName, $"{testName}.dll")} {testArgs}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.Environment.Add("DOTNET_DbgEnableMiniDump", "0");
            startInfo.Environment.Add("DOTNET_LogStackOverflowExit", "1");

            ProcessTextOutput result = Process.RunAndCaptureText(startInfo);

            List<string> lines = new List<string>();
            bool endOfStackTrace = false;
            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string data = rawLine.TrimEnd('\r');
                Console.WriteLine($"\"{data}\"");
                if (!endOfStackTrace && !string.IsNullOrEmpty(data))
                {
                    // Store lines only till the end of the stack trace.
                    // In the CI it can also contain lines with createdump info.
                    if (data.StartsWith("Stack overflow.") ||
                        data.StartsWith("Repeated ") ||
                        data.StartsWith("------") ||
                        data.StartsWith("   at "))
                    {
                        lines.Add(data);
                    }
                    else if (!data.StartsWith("@"))
                    {
                        endOfStackTrace = true;
                    }
                }
            }

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

            if (!Array.Exists(expectedExitCodes, code => result.ExitStatus.ExitCode == code))
            {
                string separator = string.Empty;
                StringBuilder expectedListBuilder = new StringBuilder();
                Array.ForEach(expectedExitCodes, code => {
                    expectedListBuilder.Append($"{separator}0x{code:X8}");
                    separator = " or ";
                });
                throw new Exception($"Exit code: 0x{result.ExitStatus.ExitCode:X8}, expected {expectedListBuilder.ToString()}");
            }

            if (lines[0] != "Stack overflow.")
            {
                throw new Exception("Missing \"Stack overflow.\" at the first line");
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/84911", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
        [ActiveIssue("Specific to CoreCLR", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/110173", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/84911", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
        [ActiveIssue("Specific to CoreCLR", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/110173", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]
        [Fact]
        public static void TestStackOverflowLargeFrameMainThread()
        {
            if (((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) || (RuntimeInformation.ProcessArchitecture == Architecture.X64) || (RuntimeInformation.ProcessArchitecture == Architecture.RiscV64) ||
                (RuntimeInformation.ProcessArchitecture == Architecture.LoongArch64) || (RuntimeInformation.ProcessArchitecture == Architecture.Arm)) &&
                ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX)))
            {
                // Disabled on Unix RISCV64 and LoongArch64, similar to ARM64.
                // LoongArch64 hit this issue on Alpine. TODO: implement stack probing using helpers.
                // Disabled on Unix ARM64 due to https://github.com/dotnet/runtime/issues/13519
                // The current stack probing doesn't move the stack pointer and so the runtime sometimes cannot
                // recognize the underlying sigsegv as stack overflow when it probes too far from SP.
                // Disabled on Unix X64/Arm due to https://github.com/dotnet/runtime/issues/110173 which needs investigation.
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/84911", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
        [ActiveIssue("Specific to CoreCLR", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/110173", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/84911", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
        [ActiveIssue("Specific to CoreCLR", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/110173", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]
        [Fact]
        public static void TestStackOverflowLargeFrameSecondaryThread()
        {
            if (((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) || (RuntimeInformation.ProcessArchitecture == Architecture.X64) || (RuntimeInformation.ProcessArchitecture == Architecture.RiscV64) ||
                (RuntimeInformation.ProcessArchitecture == Architecture.LoongArch64) || (RuntimeInformation.ProcessArchitecture == Architecture.Arm)) &&
                ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX)))
            {
                // Disabled on Unix RISCV64 and LoongArch64, similar to ARM64.
                // LoongArch64 hit this issue on Alpine. TODO: implement stack probing using helpers.
                // Disabled on Unix ARM64 due to https://github.com/dotnet/runtime/issues/13519
                // The current stack probing doesn't move the stack pointer and so the runtime sometimes cannot
                // recognize the underlying sigsegv as stack overflow when it probes too far from SP.
                // Disabled on Unix X64/Arm due to https://github.com/dotnet/runtime/issues/110173 which needs investigation.
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/84911", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/110173", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]
        [Fact]
        public static void TestStackOverflow3()
        {
            if ((RuntimeInformation.ProcessArchitecture == Architecture.Arm) || ((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) && (Environment.OSVersion.Platform == PlatformID.Unix)))
            {
                // Disabled on ARM due to https://github.com/dotnet/runtime/issues/107184
                // Disabled on Unix ARM64 due to https://github.com/dotnet/runtime/issues/110173 which needs investigation.
                return;
            }

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
