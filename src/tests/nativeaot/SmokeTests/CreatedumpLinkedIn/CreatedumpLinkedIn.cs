// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Tests the linked-in createdump functionality for NativeAOT on Linux.
/// Standard executables with CreatedumpSupport enabled link in a crash dump writer.
/// On crash, the process re-executes itself with a GUID sentinel to generate
/// an ELF core dump via ptrace.
///
/// Test flow:
///   Parent (no args)  → launches child with --crash
///   Child (--crash)   → crashes via null pointer dereference
///   NativeAOT runtime → detects crash, forks, re-execs self with sentinel
///   Re-exec'd process → writes ELF core dump to specified path
///   Parent            → verifies memory segments, thread notes, filtering, and fallback dispatch
/// </summary>
class CreatedumpLinkedIn
{
    const uint PtLoad = 1;
    const uint PtNote = 4;
    const uint NtPrStatus = 1;
    const uint NtFpRegSet = 2;
    const uint NtPrPsInfo = 3;
    const uint NtAuxV = 6;
    const uint NtFile = 0x46494c45;
    const uint NtSigInfo = 0x53494749;
    const int DeletedMappingProbeOffset = 128;

    // ELF magic bytes: 0x7f 'E' 'L' 'F'
    static readonly byte[] ElfMagic = { 0x7f, 0x45, 0x4c, 0x46 };
    static readonly byte[] DeletedMappingPattern = { 0x46, 0x4c, 0x45, 0x7f };
    static MemoryMappedFile? s_deletedMappingFile;
    static MemoryMappedViewAccessor? s_deletedMappingView;

    struct NoteCounts
    {
        public int PrStatus;
        public int FpRegSet;
        public int PrPsInfo;
        public int AuxV;
        public int File;
        public int SigInfo;
    }

    static unsafe int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--crash")
        {
            return CrashChild();
        }

        return RunParent();
    }

    static int RunParent()
    {
        string processPath = Environment.ProcessPath!;
        string dumpDir = Path.Combine(Path.GetTempPath(), "createdump_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dumpDir);
        string dumpPathTemplate = Path.Combine(dumpDir, "coredump.%d.%%");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("--crash");

            // Enable mini dump generation with a known output path.
            // DbgMiniDumpType=2 (WithHeap) uses optimized filtering:
            // writable memory + main exe + shared library ELF headers.
            startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
            startInfo.Environment["DOTNET_DbgMiniDumpName"] = dumpPathTemplate;
            startInfo.Environment["DOTNET_DbgMiniDumpType"] = "2";

            Console.WriteLine($"Launching child process: {processPath} --crash");
            Console.WriteLine($"Dump path template: {dumpPathTemplate}");

            using Process child = Process.Start(startInfo)!;

            // Read stdout/stderr asynchronously to avoid deadlock.
            // ReadToEnd() blocks until the pipe closes; if we read one
            // stream synchronously, the child can block writing to the
            // other stream (full pipe buffer), causing a deadlock.
            var stdoutTask = child.StandardOutput.ReadToEndAsync();
            var stderrTask = child.StandardError.ReadToEndAsync();

            bool exited = child.WaitForExit(60_000);

            if (!exited)
            {
                Console.WriteLine("FAIL: Child process did not exit within timeout.");
                child.Kill();
                return 1;
            }

            string stdout = stdoutTask.GetAwaiter().GetResult();
            string stderr = stderrTask.GetAwaiter().GetResult();

            Console.WriteLine($"Child exit code: {child.ExitCode}");
            if (!string.IsNullOrEmpty(stdout))
                Console.WriteLine($"Child stdout: {stdout}");
            if (!string.IsNullOrEmpty(stderr))
                Console.WriteLine($"Child stderr: {stderr}");

            if (child.ExitCode == 0)
            {
                Console.WriteLine("FAIL: Crash child exited successfully.");
                return 1;
            }

            // Verify that the linked-in createdump path was used (not an external binary).
            if (!stderr.Contains("[createdump]"))
            {
                Console.WriteLine("FAIL: Child stderr does not contain '[createdump]' marker. " +
                    "The linked-in createdump path may not have been used.");
                return 1;
            }

            if (!TryGetDeletedMappingProbe(stdout, out ulong deletedMappingProbe))
            {
                Console.WriteLine("FAIL: Child did not report the deleted mapping probe address.");
                return 1;
            }

            string dumpFile = Path.Combine(dumpDir, $"coredump.{child.Id}.%");
            if (!File.Exists(dumpFile))
            {
                Console.WriteLine("FAIL: Expected dump file was not created: " + dumpFile);
                return 1;
            }

            string[] dumpFiles = Directory.GetFiles(dumpDir, "coredump.*");
            if (dumpFiles.Length != 1)
            {
                Console.WriteLine($"FAIL: Expected one dump file, found {dumpFiles.Length}.");
                return 1;
            }

            FileInfo fileInfo = new FileInfo(dumpFile);
            Console.WriteLine($"Found {fileInfo.Name} ({fileInfo.Length} bytes)");
            if (!ValidateElfCore(dumpFile, deletedMappingProbe))
            {
                return 1;
            }

            if (!ValidateExternalCreatedumpDispatch(processPath, dumpDir, customOverride: false) ||
                !ValidateExternalCreatedumpDispatch(processPath, dumpDir, customOverride: true))
            {
                return 1;
            }

            Console.WriteLine("PASS: Linked and external createdump dispatch generated valid results.");
            return 100;
        }
        finally
        {
            try
            {
                Directory.Delete(dumpDir, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup
            }
        }
    }

    static bool ValidateElfCore(string dumpFile, ulong deletedMappingProbe)
    {
        const int ElfHeaderSize = 64;
        // Elf64_Phdr is 56 bytes
        const int ProgramHeaderSize = 56;

        using FileStream stream = File.OpenRead(dumpFile);
        if (stream.Length < ElfHeaderSize)
        {
            Console.WriteLine($"FAIL: Dump file is too small ({stream.Length} bytes).");
            return false;
        }

        byte[] header = new byte[ElfHeaderSize];
        stream.ReadExactly(header);

        if (!header.AsSpan(0, ElfMagic.Length).SequenceEqual(ElfMagic))
        {
            Console.WriteLine("FAIL: Dump file does not have ELF magic.");
            return false;
        }
        if (header[4] != 2 || header[5] != 1)
        {
            Console.WriteLine($"FAIL: Expected a little-endian ELF64 file, got class {header[4]} and data encoding {header[5]}.");
            return false;
        }
        if (BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(16)) != 4)
        {
            Console.WriteLine("FAIL: Dump file is not ET_CORE.");
            return false;
        }

        ulong programHeaderOffset = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(32));
        ushort programHeaderEntrySize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(54));
        ushort programHeaderCount = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(56));
        if (programHeaderEntrySize != ProgramHeaderSize || programHeaderCount == 0)
        {
            Console.WriteLine($"FAIL: Invalid program header table: entry size {programHeaderEntrySize}, count {programHeaderCount}.");
            return false;
        }

        bool hasLoad = false;
        bool hasExpectedDeletedMappingContents = false;
        bool hasExpectedNtFilePageSize = false;
        int noteSegmentCount = 0;
        NoteCounts noteCounts = default;
        ulong expectedPageSize = (ulong)Environment.SystemPageSize;
        byte[] programHeader = new byte[ProgramHeaderSize];

        for (int i = 0; i < programHeaderCount; i++)
        {
            ulong entryOffset = programHeaderOffset + (ulong)i * programHeaderEntrySize;
            if (entryOffset > (ulong)stream.Length - ProgramHeaderSize)
            {
                Console.WriteLine("FAIL: Program header table extends past the end of the dump.");
                return false;
            }

            stream.Position = (long)entryOffset;
            stream.ReadExactly(programHeader);

            uint type = BinaryPrimitives.ReadUInt32LittleEndian(programHeader);
            ulong segmentOffset = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(8));
            ulong virtualAddress = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(16));
            ulong fileSize = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(32));
            ulong memorySize = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(40));
            ulong alignment = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(48));
            if (segmentOffset > (ulong)stream.Length || fileSize > (ulong)stream.Length - segmentOffset)
            {
                Console.WriteLine($"FAIL: Segment {i} extends past the end of the dump.");
                return false;
            }

            if (type == PtLoad)
            {
                if (fileSize == 0 || memorySize < fileSize)
                {
                    Console.WriteLine($"FAIL: Invalid PT_LOAD sizes: file size {fileSize}, memory size {memorySize}.");
                    return false;
                }

                if (alignment != expectedPageSize)
                {
                    Console.WriteLine($"FAIL: PT_LOAD alignment {alignment} does not match system page size {expectedPageSize}.");
                    return false;
                }

                hasLoad = true;
                bool containsDeletedMappingProbe =
                    deletedMappingProbe >= virtualAddress &&
                    deletedMappingProbe - virtualAddress <= fileSize &&
                    (ulong)DeletedMappingPattern.Length <= fileSize - (deletedMappingProbe - virtualAddress);
                if (containsDeletedMappingProbe)
                {
                    ulong patternOffset = segmentOffset + (deletedMappingProbe - virtualAddress);
                    byte[] actualPattern = new byte[DeletedMappingPattern.Length];
                    stream.Position = checked((long)patternOffset);
                    stream.ReadExactly(actualPattern);
                    if (!actualPattern.AsSpan().SequenceEqual(DeletedMappingPattern))
                    {
                        Console.WriteLine("FAIL: Deleted mapping contents do not match the expected pattern.");
                        return false;
                    }

                    hasExpectedDeletedMappingContents = true;
                }
            }
            else if (type == PtNote)
            {
                noteSegmentCount++;
                if (fileSize > int.MaxValue)
                {
                    Console.WriteLine("FAIL: ELF note segment is too large.");
                    return false;
                }

                byte[] notes = new byte[(int)fileSize];
                stream.Position = (long)segmentOffset;
                stream.ReadExactly(notes);
                if (!ReadNotes(
                    notes,
                    expectedPageSize,
                    ref hasExpectedNtFilePageSize,
                    ref noteCounts))
                {
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"FAIL: Unexpected program header type {type}.");
                return false;
            }
        }

        if (!hasLoad ||
            !hasExpectedDeletedMappingContents ||
            !hasExpectedNtFilePageSize ||
            noteSegmentCount != 1 ||
            noteCounts.PrPsInfo != 1 ||
            noteCounts.AuxV != 1 ||
            noteCounts.File != 1 ||
            noteCounts.PrStatus == 0 ||
            noteCounts.FpRegSet > noteCounts.PrStatus ||
            noteCounts.SigInfo != 1)
        {
            Console.WriteLine(
                $"FAIL: Missing required ELF content: PT_LOAD={hasLoad}, " +
                $"deleted mapping contents={hasExpectedDeletedMappingContents}, NT_FILE page size={hasExpectedNtFilePageSize}, " +
                $"PT_NOTE={noteSegmentCount}, NT_PRPSINFO={noteCounts.PrPsInfo}, NT_AUXV={noteCounts.AuxV}, " +
                $"NT_FILE={noteCounts.File}, NT_PRSTATUS={noteCounts.PrStatus}, " +
                $"NT_FPREGSET={noteCounts.FpRegSet}, NT_SIGINFO={noteCounts.SigInfo}.");
            return false;
        }

        return true;
    }

    static bool ReadNotes(
        byte[] notes,
        ulong expectedPageSize,
        ref bool hasExpectedNtFilePageSize,
        ref NoteCounts counts)
    {
        int offset = 0;
        while (offset <= notes.Length - 12)
        {
            uint nameSize = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset));
            uint dataSize = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset + 4));
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset + 8));
            ulong nextOffset = (ulong)offset + 12 + Align4(nameSize) + Align4(dataSize);
            if (nextOffset > (ulong)notes.Length)
            {
                Console.WriteLine("FAIL: Malformed ELF note segment.");
                return false;
            }

            bool isCoreNote =
                nameSize == 5 &&
                notes[offset + 12] == (byte)'C' &&
                notes[offset + 13] == (byte)'O' &&
                notes[offset + 14] == (byte)'R' &&
                notes[offset + 15] == (byte)'E' &&
                notes[offset + 16] == 0;
            if (!isCoreNote)
            {
                Console.WriteLine("FAIL: Unexpected ELF note owner.");
                return false;
            }

            if (dataSize == 0)
            {
                Console.WriteLine($"FAIL: ELF note type 0x{type:X} has an empty payload.");
                return false;
            }

            switch (type)
            {
                case NtPrStatus:
                    counts.PrStatus++;
                    break;

                case NtFpRegSet:
                    counts.FpRegSet++;
                    break;

                case NtPrPsInfo:
                    counts.PrPsInfo++;
                    break;

                case NtAuxV:
                    counts.AuxV++;
                    break;

                case NtFile:
                    counts.File++;
                    if (dataSize < 16)
                    {
                        Console.WriteLine("FAIL: NT_FILE payload is too small.");
                        return false;
                    }

                    int descriptionOffset = checked(offset + 12 + (int)Align4(nameSize));
                    ulong pageSize = BinaryPrimitives.ReadUInt64LittleEndian(notes.AsSpan(descriptionOffset + 8));
                    hasExpectedNtFilePageSize |= pageSize == expectedPageSize;
                    break;

                case NtSigInfo:
                    counts.SigInfo++;
                    break;

                default:
                    Console.WriteLine($"FAIL: Unexpected ELF note type 0x{type:X}.");
                    return false;
            }

            offset = (int)nextOffset;
        }

        if (offset != notes.Length)
        {
            Console.WriteLine("FAIL: ELF note segment has trailing data.");
            return false;
        }

        return true;
    }

    static ulong Align4(uint value) => ((ulong)value + 3) & ~3UL;

    static bool TryGetDeletedMappingProbe(string stdout, out ulong address)
    {
        const string Prefix = "Deleted mapping probe: 0x";
        int start = stdout.IndexOf(Prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            address = 0;
            return false;
        }

        start += Prefix.Length;
        int end = stdout.IndexOfAny(new[] { '\r', '\n' }, start);
        string value = end < 0 ? stdout[start..] : stdout[start..end];
        return ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }

    static bool ValidateExternalCreatedumpDispatch(
        string processPath,
        string testDirectory,
        bool customOverride)
    {
        string helperDirectory = customOverride
            ? Path.Combine(testDirectory, "external")
            : Path.GetDirectoryName(processPath)!;
        Directory.CreateDirectory(helperDirectory);
        string helperPath = Path.Combine(helperDirectory, "createdump");
        const string HelperContents = "#!/bin/sh\nprintf '[external-createdump] %s\\n' \"$*\" >&2\n";
        if (File.Exists(helperPath))
        {
            if (File.ReadAllText(helperPath) == HelperContents)
            {
                File.Delete(helperPath);
            }
            else
            {
                Console.WriteLine($"FAIL: Test helper path already exists: {helperPath}");
                return false;
            }
        }

        File.WriteAllText(helperPath, HelperContents);
        File.SetUnixFileMode(
            helperPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("--crash");
            startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
            startInfo.Environment["DOTNET_DbgMiniDumpType"] = customOverride ? "2" : "1";
            if (customOverride)
            {
                startInfo.Environment["DOTNET_DbgCreateDumpToolPath"] = helperDirectory;
            }
            else
            {
                startInfo.Environment["DOTNET_EnableCrashReportOnly"] = "1";
                startInfo.Environment["DOTNET_CreateDumpLogToFile"] = Path.Combine(testDirectory, "createdump.log");
            }

            using Process child = Process.Start(startInfo)!;
            Task<string> stdoutTask = child.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(60_000))
            {
                Console.WriteLine("FAIL: External-createdump child did not exit within timeout.");
                child.Kill();
                return false;
            }

            string stdout = stdoutTask.GetAwaiter().GetResult();
            string stderr = stderrTask.GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(stdout))
            {
                Console.WriteLine($"External-createdump child stdout: {stdout}");
            }
            if (!string.IsNullOrEmpty(stderr))
            {
                Console.WriteLine($"External-createdump child stderr: {stderr}");
            }

            if (child.ExitCode == 0)
            {
                Console.WriteLine("FAIL: External-createdump crash child exited successfully.");
                return false;
            }

            bool hasExpectedArguments = customOverride
                ? stderr.Contains("--withheap")
                : stderr.Contains("--normal") &&
                  stderr.Contains("--crashreportonly") &&
                  stderr.Contains("--logtofile");
            if (!stderr.Contains("[external-createdump]") || !hasExpectedArguments)
            {
                Console.WriteLine("FAIL: External createdump did not receive the expected options.");
                return false;
            }
            if (stderr.Contains("[createdump]", StringComparison.Ordinal))
            {
                Console.WriteLine("FAIL: Linked createdump was used instead of the required external helper.");
                return false;
            }

            return true;
        }
        finally
        {
            File.Delete(helperPath);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int CrashChild()
    {
        string mappingPath = Path.Combine(Path.GetTempPath(), "createdump_mapping_" + Path.GetRandomFileName());
        int pageSize = Environment.SystemPageSize;
        int probeOffset = checked(pageSize + DeletedMappingProbeOffset);
        byte[] mappingContents = new byte[checked(pageSize * 2)];
        DeletedMappingPattern.CopyTo(mappingContents.AsSpan(probeOffset));
        File.WriteAllBytes(mappingPath, mappingContents);
        s_deletedMappingFile = MemoryMappedFile.CreateFromFile(
            mappingPath,
            FileMode.Open,
            mapName: null,
            capacity: 0,
            MemoryMappedFileAccess.Read);
        s_deletedMappingView = s_deletedMappingFile.CreateViewAccessor(
            offset: 0,
            size: 0,
            MemoryMappedFileAccess.Read);
        File.Delete(mappingPath);

        byte* mappingPointer = null;
        s_deletedMappingView.SafeMemoryMappedViewHandle.AcquirePointer(ref mappingPointer);
        ulong probeAddress = (ulong)(mappingPointer + probeOffset);
        Console.WriteLine($"Deleted mapping probe: 0x{probeAddress:X}");
        Console.WriteLine("Child: About to crash via null pointer dereference.");

        // Force a SIGSEGV by writing to address zero.
        *(int*)0 = 42;

        // Should not reach here.
        return 1;
    }
}
