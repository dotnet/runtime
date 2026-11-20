// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal class Createdump
{
    private const string ExternalCreatedumpMarker = "External createdump invoked";

#if LINKED_CREATEDUMP
    private const bool LinkedCreatedump = true;
#else
    private const bool LinkedCreatedump = false;
#endif

    private static int Main(string[] args)
    {
        if (args.Length != 0)
        {
            if (args[0] == "crash")
            {
                Environment.FailFast("Createdump selection test");
            }

            // If bootstrap dispatch is broken, do not crash again on the sentinel argument.
            return 42;
        }

        string processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Environment.ProcessPath is null.");
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT")
            ?? throw new InvalidOperationException("CORE_ROOT must point to the runtime test payload containing createdump.");
        string externalCreatedump = Path.GetFullPath(Path.Combine(coreRoot, "createdump"));
        if (!File.Exists(externalCreatedump))
        {
            throw new FileNotFoundException("The standalone createdump executable is required for this test.", externalCreatedump);
        }

        string directory = Path.Combine(Path.GetTempPath(), "NativeAotCreatedump-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string executable = Path.Combine(directory, "test_app");
            File.Copy(processPath, executable);
            File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            // Print a marker before executing the real tool, so successful dump generation
            // alone cannot hide an accidental fallback from the linked implementation.
            WriteExternalHelper(directory);

            RunScenario(executable, externalCreatedump, "automatic", expectLinked: LinkedCreatedump);

#if LINKED_CREATEDUMP
            string overrideDirectory = Path.Combine(directory, "external");
            Directory.CreateDirectory(overrideDirectory);
            WriteExternalHelper(overrideDirectory);
            RunScenario(executable, externalCreatedump, "override", expectLinked: false, toolDirectory: overrideDirectory);
#endif

            return 100;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void WriteExternalHelper(string directory)
    {
        string helper = Path.Combine(directory, "createdump");
        File.WriteAllText(helper,
            "#!/bin/sh\n" +
            $"printf '%s\\n' '{ExternalCreatedumpMarker}'\n" +
            "exec \"$CREATEDUMP_TEST_TOOL\" \"$@\"\n");
        File.SetUnixFileMode(helper, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static void RunScenario(
        string executable,
        string externalCreatedump,
        string scenario,
        bool expectLinked,
        string toolDirectory = null)
    {
        string directory = Path.Combine(Path.GetDirectoryName(executable), scenario);
        Directory.CreateDirectory(directory);
        var startInfo = new ProcessStartInfo(executable, ["crash"])
        {
            InheritedHandles = [],
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
        startInfo.Environment["DOTNET_EnableDiagnostics"] = "0";
        startInfo.Environment["DOTNET_DbgMiniDumpType"] = "4";
        startInfo.Environment["DOTNET_DbgMiniDumpName"] = Path.Combine(directory, "dump.%e.%p");
        startInfo.Environment["DOTNET_DbgCreateDumpToolPath"] = toolDirectory;
        startInfo.Environment["DOTNET_CreateDumpLogToFile"] = null;
        startInfo.Environment["DOTNET_CreateDumpDiagnostics"] = "0";
        startInfo.Environment["DOTNET_CreateDumpVerboseDiagnostics"] = "0";
        startInfo.Environment["DOTNET_EnableCrashReport"] = "0";
        startInfo.Environment["DOTNET_EnableCrashReportOnly"] = "0";
        startInfo.Environment["DOTNET_DbgDisablePagemapUse"] = "0";
        startInfo.Environment["CREATEDUMP_TEST_TOOL"] = externalCreatedump;

        ProcessTextOutput output = Process.RunAndCaptureText(startInfo, TimeSpan.FromMinutes(2));
        Console.WriteLine($"{scenario}: {output.StandardOutput}{output.StandardError}");
        // SIGABRT is 6 on the supported Linux targets, but has no named PosixSignal value.
        if (output.ExitStatus.Signal != (PosixSignal)6)
        {
            throw new Exception($"{scenario}: expected SIGABRT, got exit code {output.ExitStatus.ExitCode}, signal {output.ExitStatus.Signal}.");
        }

        // Check if the external createdump tool was invoked.
        bool externalCreatedumpInvoked = output.StandardOutput.Contains(ExternalCreatedumpMarker, StringComparison.Ordinal);
        if (externalCreatedumpInvoked == expectLinked)
        {
            throw new Exception($"{scenario}: expected {(expectLinked ? "linked" : "external")} createdump.");
        }

        if (!output.StandardOutput.Contains("Dump successfully written", StringComparison.Ordinal) ||
            output.StandardError.Contains("[createdump]", StringComparison.Ordinal))
        {
            throw new Exception($"{scenario}: createdump did not report successful dump generation without errors.");
        }

        string dump = Path.Combine(directory, $"dump.test_app.{output.ProcessId}");
        ValidateDumpHeader(dump);
        Console.WriteLine($"{scenario}: {(expectLinked ? "linked" : "external")} createdump successfully wrote an ELF core dump.");
    }

    private static void ValidateDumpHeader(string dump)
    {
        const int ElfHeaderSize = 64;
        const byte ElfClass64 = 2;
        const byte ElfDataLsb = 1;
        const ushort EtCore = 4;
        const ushort EmX86_64 = 62;
        const ushort EmAArch64 = 183;
        ReadOnlySpan<byte> elfMagic = "\u007fELF"u8;

        using FileStream stream = File.OpenRead(dump);
        byte[] header = new byte[ElfHeaderSize];
        stream.ReadExactly(header);

        // Check the header identity here; the diagnostics tests validate the full dump contents.
        if (!header.AsSpan(0, elfMagic.Length).SequenceEqual(elfMagic))
        {
            throw new InvalidDataException("The dump does not have ELF magic.");
        }
        if (header[4] != ElfClass64 || header[5] != ElfDataLsb)
        {
            throw new InvalidDataException($"Expected little-endian ELF64, got class {header[4]} and data encoding {header[5]}.");
        }
        if (BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(16)) != EtCore)
        {
            throw new InvalidDataException("The ELF file is not ET_CORE.");
        }

        ushort expectedMachine = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => EmX86_64,
            Architecture.Arm64 => EmAArch64,
            _ => throw new PlatformNotSupportedException("This test requires Linux x64 or Arm64."),
        };
        ushort actualMachine = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(18));
        if (actualMachine != expectedMachine)
        {
            throw new InvalidDataException($"Expected ELF machine {expectedMachine}, got {actualMachine}.");
        }
        if (stream.Length <= header.Length)
        {
            throw new InvalidDataException("The dump contains no data beyond the ELF header.");
        }
    }
}
