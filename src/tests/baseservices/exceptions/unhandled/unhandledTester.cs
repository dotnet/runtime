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
        // When expectedDumpExceptionCodes is set, the crash writes a minidump that must contain an
        // exception stream with one of the expected exception codes.
        static void RunExternalProcess(string unhandledType, string assembly, uint[] expectedDumpExceptionCodes = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");
            startInfo.Arguments = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assembly) + " " + unhandledType;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            // Disable crash diagnostics since the target process is expected to fail with an unhandled exception
            startInfo.Environment.Remove("DOTNET_DbgEnableMiniDump");
            startInfo.Environment.Remove("DOTNET_EnableCrashReport");

            string dumpPath = null;
            if (expectedDumpExceptionCodes != null)
            {
                dumpPath = Path.Combine(Path.GetTempPath(), $"unhandled-{Guid.NewGuid():N}.dmp");
                startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
                startInfo.Environment["DOTNET_DbgMiniDumpType"] = "1"; // MiniDumpNormal keeps the dump small
                startInfo.Environment["DOTNET_DbgMiniDumpName"] = dumpPath;
            }

            ProcessTextOutput result = Process.RunAndCaptureText(startInfo);
            Console.WriteLine($"Test process {assembly} with argument {unhandledType} exited");

            List<string> lines = new List<string>();
            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                Console.WriteLine($"\"{line}\"");
                // createdump messages are not part of the runtime output being verified
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("[createdump]"))
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

            if (dumpPath != null)
            {
                try
                {
                    VerifyMinidumpExceptionStream(dumpPath, expectedDumpExceptionCodes);
                }
                finally
                {
                    File.Delete(dumpPath);
                }
            }

            Console.WriteLine("Test process exited with expected error code and produced expected output");
        }

        // Minidump format constants (see minidumpapiset.h)
        private const uint MinidumpSignature = 0x504D444D; // 'MDMP'
        private const uint ExceptionStreamType = 6;        // MINIDUMP_STREAM_TYPE.ExceptionStream

        // Checks that the minidump has an exception stream identifying the crashing thread and the exception
        static void VerifyMinidumpExceptionStream(string dumpPath, uint[] expectedExceptionCodes)
        {
            if (!File.Exists(dumpPath))
            {
                throw new Exception($"Dump file {dumpPath} was not created");
            }
            Console.WriteLine($"Verifying dump {dumpPath} ({new FileInfo(dumpPath).Length} bytes)");

            using FileStream stream = File.OpenRead(dumpPath);
            using BinaryReader reader = new BinaryReader(stream);

            // MINIDUMP_HEADER: Signature, Version, NumberOfStreams, StreamDirectoryRva, ...
            uint signature = reader.ReadUInt32();
            if (signature != MinidumpSignature)
            {
                throw new Exception($"Invalid minidump signature 0x{signature:X8}");
            }
            reader.ReadUInt32(); // Version
            uint numberOfStreams = reader.ReadUInt32();
            uint streamDirectoryRva = reader.ReadUInt32();

            // MINIDUMP_DIRECTORY entries: StreamType, Location.DataSize, Location.Rva
            uint exceptionStreamRva = 0;
            stream.Position = streamDirectoryRva;
            for (uint i = 0; i < numberOfStreams; i++)
            {
                uint streamType = reader.ReadUInt32();
                reader.ReadUInt32(); // DataSize
                uint rva = reader.ReadUInt32();
                if (streamType == ExceptionStreamType)
                {
                    exceptionStreamRva = rva;
                }
            }
            if (exceptionStreamRva == 0)
            {
                throw new Exception("Dump has no exception stream");
            }

            // MINIDUMP_EXCEPTION_STREAM: ThreadId, __alignment, MINIDUMP_EXCEPTION { ExceptionCode, ... }, ThreadContext
            stream.Position = exceptionStreamRva;
            uint threadId = reader.ReadUInt32();
            reader.ReadUInt32(); // __alignment
            uint exceptionCode = reader.ReadUInt32();
            Console.WriteLine($"Dump exception stream: thread {threadId}, exception code 0x{exceptionCode:X8}");
            if (threadId == 0)
            {
                throw new Exception("Dump exception stream has no crashing thread id");
            }
            if (!Array.Exists(expectedExceptionCodes, code => code == exceptionCode))
            {
                throw new Exception($"Wrong dump exception code: 0x{exceptionCode:X8}, expected {string.Join(" or ", Array.ConvertAll(expectedExceptionCodes, code => $"0x{code:X8}"))}");
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

            if (OperatingSystem.IsWindows() && !TestLibrary.Utilities.IsMonoRuntime && !TestLibrary.Utilities.IsNativeAot)
            {
                // The minidump must record the crash: an access violation (or the managed exception raised for
                // it when the null check is done in software) and the managed exception of the software throw.
                RunExternalProcess("mainhardware", "unhandled.dll", new uint[] { 0xC0000005, 0xE0434352 });
                RunExternalProcess("main", "unhandled.dll", new uint[] { 0xE0434352 });
            }
        }
    }
}
