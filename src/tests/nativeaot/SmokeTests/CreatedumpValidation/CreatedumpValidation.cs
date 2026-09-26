// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

internal static class CreatedumpValidation
{
    private const int Pass = 100;
    private const int Fail = 1;
    private const int TimeoutMilliseconds = 90_000;

    private const uint PtLoad = 1;
    private const uint PtNote = 4;
    private const uint PfRead = 4;
    private const ushort EtCore = 4;
    private const ushort EmX86_64 = 62;
    private const ushort EmAArch64 = 183;
    private const byte ElfClass64 = 2;
    private const byte ElfDataLsb = 1;
    private const int ElfHeaderSize = 64;
    private const int ProgramHeaderSize = 56;
    private const int PrPsInfoPidOffset = 24;

    private const uint NtPrStatus = 1;
    private const uint NtFpRegSet = 2;
    private const uint NtPrPsInfo = 3;
    private const uint NtAuxV = 6;
    private const uint NtFile = 0x46494c45;
    private const uint NtSigInfo = 0x53494749;

    private const ulong AtNull = 0;
    private const ulong AtPhdr = 3;
    private const ulong AtPageSize = 6;

    private const ulong SpecialDiagInfoAddress = 0x00007ffffff10000;
    private const ulong SpecialDiagInfoSize = 0x1000;
    private const int SpecialDiagInfoVersion = 2;
    private const int SpecialDiagExceptionRecordOffset = 24;
    private const int SpecialDiagRuntimeBaseOffset = 32;

    private const uint StatusStackBufferOverrun = 0xC0000409;
    private const uint ExceptionNoncontinuable = 1;
    private const ulong FastFailExceptionDotNetAot = 0x48;
    private const uint InvalidOperationHResult = 0x80131509;
    private const int ExceptionRecordSize = 152;
    private const int ExceptionRecordParameterCountOffset = 24;
    private const int ExceptionRecordInformationOffset = 32;

    private const int DeletedMappingProbeOffset = 128;
    private const string ExceptionMessage = "CreatedumpValidation managed exception payload 8A41C43D";
    private const string ExternalMarkerEnvironmentVariable = "CREATEDUMP_VALIDATION_EXTERNAL_MARKER";
    private const string ExternalHelperEnvironmentVariable = "CREATEDUMP_VALIDATION_EXTERNAL_HELPER";

    private static readonly byte[] ElfMagic = { 0x7f, 0x45, 0x4c, 0x46 };
    private static readonly byte[] DeletedMappingPattern = { 0x43, 0x44, 0x55, 0x4d, 0x50, 0x8a, 0x41, 0xc4 };
    private static readonly byte[] SpecialDiagSignature = Encoding.ASCII.GetBytes("DIAGINFOHEADER");

    private static MemoryMappedFile? s_deletedMappingFile;
    private static MemoryMappedViewAccessor? s_deletedMappingView;

    private readonly struct LoadSegment
    {
        public LoadSegment(uint flags, ulong fileOffset, ulong virtualAddress, ulong fileSize, ulong memorySize)
        {
            Flags = flags;
            FileOffset = fileOffset;
            VirtualAddress = virtualAddress;
            FileSize = fileSize;
            MemorySize = memorySize;
        }

        public uint Flags { get; }
        public ulong FileOffset { get; }
        public ulong VirtualAddress { get; }
        public ulong FileSize { get; }
        public ulong MemorySize { get; }
    }

    private struct NoteSummary
    {
        public int PrStatus;
        public int FpRegSet;
        public int PrPsInfo;
        public int AuxV;
        public int File;
        public int SigInfo;
        public bool AuxVHasExpectedPageSize;
        public bool FileHasExpectedPageSize;
        public bool HasProgramHeaders;
        public bool HasExecutableFileName;
        public bool HasDeletedMappingFileName;
        public int Signal;
    }

    private sealed class ExternalCreatedumpScope : IDisposable
    {
        private const string WrapperContents =
            "#!/bin/sh\n" +
            ": > \"$" + ExternalMarkerEnvironmentVariable + "\"\n" +
            "exec \"$" + ExternalHelperEnvironmentVariable + "\" \"$@\"\n";

        private readonly string _defaultHelperPath;
        private readonly string? _originalHelperBackup;
        private readonly UnixFileMode? _originalHelperMode;

        public ExternalCreatedumpScope(string processPath, string testDirectory)
        {
            string applicationDirectory = Path.GetDirectoryName(processPath)
                ?? throw new InvalidOperationException("The process path has no directory.");

            _defaultHelperPath = Path.Combine(applicationDirectory, "createdump");
            RealHelperPath = Path.Combine(testDirectory, "createdump-real");
            ForcedHelperDirectory = Path.Combine(testDirectory, "forced-external");
            Directory.CreateDirectory(ForcedHelperDirectory);

            string sourceHelper;
            if (File.Exists(_defaultHelperPath))
            {
                _originalHelperBackup = Path.Combine(testDirectory, "createdump-original");
                _originalHelperMode = File.GetUnixFileMode(_defaultHelperPath);
                File.Copy(_defaultHelperPath, _originalHelperBackup, overwrite: true);
                sourceHelper = _originalHelperBackup;
                File.Delete(_defaultHelperPath);
            }
            else
            {
                string? coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
                sourceHelper = coreRoot is null ? string.Empty : Path.Combine(coreRoot, "createdump");
                if (!File.Exists(sourceHelper))
                {
                    throw new FileNotFoundException(
                        $"External createdump was not found next to the test or under CORE_ROOT ('{coreRoot ?? "<unset>"}').",
                        sourceHelper);
                }
            }

            try
            {
                File.Copy(sourceHelper, RealHelperPath, overwrite: true);
                File.SetUnixFileMode(
                    RealHelperPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

                WriteWrapper(_defaultHelperPath);
                WriteWrapper(Path.Combine(ForcedHelperDirectory, "createdump"));
            }
            catch
            {
                RestoreDefaultHelper();
                throw;
            }
        }

        public string RealHelperPath { get; }
        public string ForcedHelperDirectory { get; }

        public void Dispose() => RestoreDefaultHelper();

        private void RestoreDefaultHelper()
        {
            File.Delete(_defaultHelperPath);
            if (_originalHelperBackup is not null)
            {
                File.Copy(_originalHelperBackup, _defaultHelperPath, overwrite: true);
                File.SetUnixFileMode(_defaultHelperPath, _originalHelperMode!.Value);
            }
        }

        private static void WriteWrapper(string path)
        {
            File.WriteAllText(path, WrapperContents);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--crash")
        {
            return CrashChild();
        }

        try
        {
            return RunParent();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"FAIL: {exception}");
            return Fail;
        }
    }

    private static int RunParent()
    {
        string processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Environment.ProcessPath is null.");
        string testDirectory = Path.Combine(Path.GetTempPath(), "createdump_validation_" + Path.GetRandomFileName());
        Directory.CreateDirectory(testDirectory);

        ExternalCreatedumpScope? externalCreatedump = null;
        try
        {
            externalCreatedump = new ExternalCreatedumpScope(processPath, testDirectory);

            bool automaticUsedExternal = RunScenario(
                processPath,
                testDirectory,
                externalCreatedump,
                scenarioName: "automatic",
                forceExternal: false);

            bool forcedUsedExternal = RunScenario(
                processPath,
                testDirectory,
                externalCreatedump,
                scenarioName: "forced-external",
                forceExternal: true);

            if (!forcedUsedExternal)
            {
                throw new InvalidDataException("The forced-external scenario did not invoke the external createdump helper.");
            }

            Console.WriteLine($"Automatic implementation: {(automaticUsedExternal ? "external" : "linked")}");
            Console.WriteLine("PASS: automatic and forced-external dumps contain the expected process, signal, memory, and managed exception data.");
            return Pass;
        }
        finally
        {
            externalCreatedump?.Dispose();
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private static bool RunScenario(
        string processPath,
        string testDirectory,
        ExternalCreatedumpScope externalCreatedump,
        string scenarioName,
        bool forceExternal)
    {
        string scenarioDirectory = Path.Combine(testDirectory, scenarioName);
        Directory.CreateDirectory(scenarioDirectory);
        string markerPath = Path.Combine(scenarioDirectory, "external.marker");
        string dumpPathTemplate = Path.Combine(scenarioDirectory, "coredump.%d.%%");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--crash");
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "1";
        startInfo.Environment["DOTNET_DbgMiniDumpName"] = dumpPathTemplate;
        startInfo.Environment["DOTNET_DbgMiniDumpType"] = "4";
        startInfo.Environment["DOTNET_EnableCrashReport"] = "0";
        startInfo.Environment["DOTNET_EnableCrashReportOnly"] = "0";
        startInfo.Environment["DOTNET_CreateDumpDiagnostics"] = "0";
        startInfo.Environment["DOTNET_CreateDumpVerboseDiagnostics"] = "0";
        startInfo.Environment.Remove("DOTNET_CreateDumpLogToFile");
        startInfo.Environment[ExternalMarkerEnvironmentVariable] = markerPath;
        startInfo.Environment[ExternalHelperEnvironmentVariable] = externalCreatedump.RealHelperPath;

        if (forceExternal)
        {
            startInfo.Environment["DOTNET_DbgCreateDumpToolPath"] = externalCreatedump.ForcedHelperDirectory;
        }
        else
        {
            startInfo.Environment.Remove("DOTNET_DbgCreateDumpToolPath");
        }

        Console.WriteLine($"Launching {scenarioName}: {processPath} --crash");
        Console.WriteLine($"Dump path template: {dumpPathTemplate}");

        using Process child = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the crash child.");
        Task<string> stdoutTask = child.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = child.StandardError.ReadToEndAsync();

        if (!child.WaitForExit(TimeoutMilliseconds))
        {
            child.Kill(entireProcessTree: true);
            throw new TimeoutException($"The {scenarioName} crash child did not exit within {TimeoutMilliseconds} ms.");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();
        Console.WriteLine($"{scenarioName} child exit code: {child.ExitCode}");
        if (stdout.Length != 0)
        {
            Console.WriteLine($"{scenarioName} stdout: {stdout}");
        }
        if (stderr.Length != 0)
        {
            Console.WriteLine($"{scenarioName} stderr: {stderr}");
        }

        if (child.ExitCode == 0)
        {
            throw new InvalidOperationException($"The {scenarioName} crash child exited successfully.");
        }

        bool usedExternal = File.Exists(markerPath);
        if (forceExternal && !usedExternal)
        {
            throw new InvalidOperationException("The forced-external scenario did not execute the external createdump wrapper.");
        }

        string combinedOutput = stdout + stderr;
        if (!combinedOutput.Contains("[createdump]", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The {scenarioName} child output has no createdump status marker.");
        }
        if (usedExternal && !combinedOutput.Contains("Dump successfully written", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("External createdump did not report successful dump generation.");
        }

        ulong deletedMappingProbe = ParseHexOutputValue(stdout, "DELETED_MAPPING_PROBE=0x");
        string deletedMappingFile = ParseOutputValue(stdout, "DELETED_MAPPING_FILE=");
        string dumpFile = Path.Combine(scenarioDirectory, $"coredump.{child.Id}.%");
        string[] dumpFiles = Directory.GetFiles(scenarioDirectory, "coredump.*");
        if (dumpFiles.Length != 1 || !string.Equals(dumpFiles[0], dumpFile, StringComparison.Ordinal))
        {
            throw new FileNotFoundException(
                $"Expected exactly '{dumpFile}', but found: {string.Join(", ", dumpFiles)}");
        }

        ValidateElfCore(dumpFile, processPath, deletedMappingProbe, deletedMappingFile, child.Id);
        return usedExternal;
    }

    private static void ValidateElfCore(
        string dumpFile,
        string processPath,
        ulong deletedMappingProbe,
        string deletedMappingFile,
        int expectedPid)
    {
        using FileStream stream = File.OpenRead(dumpFile);
        byte[] header = ReadBytes(stream, 0, ElfHeaderSize);

        if (!header.AsSpan(0, ElfMagic.Length).SequenceEqual(ElfMagic))
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

        ushort machine = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(18));
        ushort expectedMachine = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => EmX86_64,
            Architecture.Arm64 => EmAArch64,
            _ => throw new PlatformNotSupportedException($"Unsupported architecture {RuntimeInformation.ProcessArchitecture}."),
        };
        if (machine != expectedMachine)
        {
            throw new InvalidDataException($"ELF machine {machine} does not match expected machine {expectedMachine}.");
        }

        ulong programHeaderOffset = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(32));
        ushort programHeaderEntrySize = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(54));
        ushort programHeaderCount = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(56));
        if (programHeaderEntrySize != ProgramHeaderSize || programHeaderCount == 0)
        {
            throw new InvalidDataException(
                $"Invalid program-header table: offset {programHeaderOffset}, entry size {programHeaderEntrySize}, count {programHeaderCount}.");
        }

        List<LoadSegment> loadSegments = new List<LoadSegment>(programHeaderCount);
        NoteSummary noteSummary = default;
        int noteSegmentCount = 0;

        for (int index = 0; index < programHeaderCount; index++)
        {
            ulong entryOffset = checked(programHeaderOffset + (ulong)index * programHeaderEntrySize);
            byte[] programHeader = ReadBytes(stream, entryOffset, ProgramHeaderSize);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(programHeader);
            uint flags = BinaryPrimitives.ReadUInt32LittleEndian(programHeader.AsSpan(4));
            ulong fileOffset = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(8));
            ulong virtualAddress = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(16));
            ulong fileSize = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(32));
            ulong memorySize = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(40));
            ulong alignment = BinaryPrimitives.ReadUInt64LittleEndian(programHeader.AsSpan(48));

            ValidateFileRange(stream, fileOffset, fileSize, $"program header {index}");

            if (type == PtLoad)
            {
                if (fileSize == 0 || memorySize < fileSize)
                {
                    throw new InvalidDataException($"Invalid PT_LOAD sizes at index {index}: file {fileSize}, memory {memorySize}.");
                }
                if (alignment != (ulong)Environment.SystemPageSize)
                {
                    throw new InvalidDataException(
                        $"PT_LOAD alignment {alignment} does not match page size {Environment.SystemPageSize}.");
                }
                if (fileOffset % alignment != virtualAddress % alignment)
                {
                    throw new InvalidDataException($"PT_LOAD {index} has incongruent file and virtual addresses.");
                }

                loadSegments.Add(new LoadSegment(flags, fileOffset, virtualAddress, fileSize, memorySize));
            }
            else if (type == PtNote)
            {
                noteSegmentCount++;
                byte[] notes = ReadBytes(stream, fileOffset, checked((int)fileSize));
                ReadNotes(notes, processPath, deletedMappingFile, expectedPid, ref noteSummary);
            }
            else
            {
                throw new InvalidDataException($"Unexpected program-header type {type} at index {index}.");
            }
        }

        if (loadSegments.Count == 0 || noteSegmentCount != 1)
        {
            throw new InvalidDataException($"Expected PT_LOAD segments and one PT_NOTE segment; found {loadSegments.Count} and {noteSegmentCount}.");
        }

        if (noteSummary.PrPsInfo != 1 ||
            noteSummary.AuxV != 1 ||
            noteSummary.File != 1 ||
            noteSummary.PrStatus == 0 ||
            noteSummary.FpRegSet == 0 ||
            noteSummary.FpRegSet > noteSummary.PrStatus ||
            noteSummary.SigInfo != 1 ||
            noteSummary.Signal != 6 ||
            !noteSummary.AuxVHasExpectedPageSize ||
            !noteSummary.FileHasExpectedPageSize ||
            !noteSummary.HasProgramHeaders ||
            !noteSummary.HasExecutableFileName ||
            noteSummary.HasDeletedMappingFileName)
        {
            throw new InvalidDataException(
                $"Unexpected notes: PRSTATUS={noteSummary.PrStatus}, FPREGSET={noteSummary.FpRegSet}, " +
                $"PRPSINFO={noteSummary.PrPsInfo}, AUXV={noteSummary.AuxV}, FILE={noteSummary.File}, " +
                $"SIGINFO={noteSummary.SigInfo}, signal={noteSummary.Signal}, auxvPageSize={noteSummary.AuxVHasExpectedPageSize}, " +
                $"filePageSize={noteSummary.FileHasExpectedPageSize}, " +
                $"phdr={noteSummary.HasProgramHeaders}, executable={noteSummary.HasExecutableFileName}, " +
                $"deletedName={noteSummary.HasDeletedMappingFileName}.");
        }

        byte[] deletedPattern = ReadVirtualMemory(stream, loadSegments, deletedMappingProbe, DeletedMappingPattern.Length);
        if (!deletedPattern.AsSpan().SequenceEqual(DeletedMappingPattern))
        {
            throw new InvalidDataException("The deleted mapping contents do not match the expected pattern.");
        }

        ValidateSpecialDiagnostics(stream, loadSegments);
        Console.WriteLine(
            $"Validated {Path.GetFileName(dumpFile)}: {loadSegments.Count} PT_LOAD segments, " +
            $"{noteSummary.PrStatus} threads, managed exception record and crash JSON present.");
    }

    private static void ReadNotes(
        byte[] notes,
        string processPath,
        string deletedMappingFile,
        int expectedPid,
        ref NoteSummary summary)
    {
        int offset = 0;
        while (offset <= notes.Length - 12)
        {
            uint nameSize = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset));
            uint dataSize = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset + 4));
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(notes.AsSpan(offset + 8));
            ulong descriptionOffset = checked((ulong)offset + 12 + Align4(nameSize));
            ulong nextOffset = checked(descriptionOffset + Align4(dataSize));
            if (nextOffset > (ulong)notes.Length)
            {
                throw new InvalidDataException("An ELF note extends past the note segment.");
            }

            if (nameSize != 5 || !notes.AsSpan(offset + 12, 5).SequenceEqual("CORE\0"u8))
            {
                throw new InvalidDataException("Unexpected ELF note owner.");
            }
            if (dataSize == 0)
            {
                throw new InvalidDataException($"ELF note 0x{type:X} has an empty payload.");
            }

            ReadOnlySpan<byte> description = notes.AsSpan(checked((int)descriptionOffset), checked((int)dataSize));
            switch (type)
            {
                case NtPrStatus:
                    summary.PrStatus++;
                    break;

                case NtFpRegSet:
                    summary.FpRegSet++;
                    break;

                case NtPrPsInfo:
                    summary.PrPsInfo++;
                    if (description.Length < PrPsInfoPidOffset + sizeof(int))
                    {
                        throw new InvalidDataException("NT_PRPSINFO is too small.");
                    }
                    int pid = BinaryPrimitives.ReadInt32LittleEndian(description.Slice(PrPsInfoPidOffset));
                    if (pid != expectedPid)
                    {
                        throw new InvalidDataException($"NT_PRPSINFO PID {pid} does not match crashed process {expectedPid}.");
                    }
                    break;

                case NtAuxV:
                    summary.AuxV++;
                    ReadAuxv(description, ref summary);
                    break;

                case NtFile:
                    summary.File++;
                    ReadNtFile(description, processPath, deletedMappingFile, ref summary);
                    break;

                case NtSigInfo:
                    summary.SigInfo++;
                    if (description.Length < 12)
                    {
                        throw new InvalidDataException("NT_SIGINFO is too small.");
                    }
                    summary.Signal = BinaryPrimitives.ReadInt32LittleEndian(description);
                    break;

                default:
                    throw new InvalidDataException($"Unexpected ELF note type 0x{type:X}.");
            }

            offset = checked((int)nextOffset);
        }

        if (offset != notes.Length)
        {
            throw new InvalidDataException("The ELF note segment has trailing bytes.");
        }
    }

    private static void ReadAuxv(ReadOnlySpan<byte> description, ref NoteSummary summary)
    {
        const int EntrySize = 16;
        if (description.Length % EntrySize != 0)
        {
            throw new InvalidDataException("NT_AUXV does not contain complete ELF64 entries.");
        }

        bool foundNull = false;
        for (int offset = 0; offset < description.Length; offset += EntrySize)
        {
            ulong type = BinaryPrimitives.ReadUInt64LittleEndian(description.Slice(offset));
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(description.Slice(offset + 8));
            if (type == AtNull)
            {
                foundNull = true;
                break;
            }
            if (type == AtPageSize)
            {
                summary.AuxVHasExpectedPageSize |= value == (ulong)Environment.SystemPageSize;
            }
            else if (type == AtPhdr)
            {
                summary.HasProgramHeaders |= value != 0;
            }
        }

        if (!foundNull)
        {
            throw new InvalidDataException("NT_AUXV has no AT_NULL terminator.");
        }
    }

    private static void ReadNtFile(
        ReadOnlySpan<byte> description,
        string processPath,
        string deletedMappingFile,
        ref NoteSummary summary)
    {
        const int HeaderSize = 16;
        const int EntrySize = 24;
        if (description.Length < HeaderSize)
        {
            throw new InvalidDataException("NT_FILE is too small.");
        }

        ulong count = BinaryPrimitives.ReadUInt64LittleEndian(description);
        ulong pageSize = BinaryPrimitives.ReadUInt64LittleEndian(description.Slice(8));
        summary.FileHasExpectedPageSize = pageSize == (ulong)Environment.SystemPageSize;
        if (count == 0 || count > int.MaxValue)
        {
            throw new InvalidDataException($"NT_FILE has invalid entry count {count}.");
        }

        int namesOffset = checked(HeaderSize + checked((int)count) * EntrySize);
        if (namesOffset > description.Length)
        {
            throw new InvalidDataException("NT_FILE entries extend past the payload.");
        }

        string executableName = Path.GetFileName(processPath);
        string deletedName = Path.GetFileName(deletedMappingFile);
        int current = namesOffset;
        for (ulong index = 0; index < count; index++)
        {
            int terminator = description.Slice(current).IndexOf((byte)0);
            if (terminator < 0)
            {
                throw new InvalidDataException("NT_FILE contains an unterminated filename.");
            }

            string fileName = Encoding.UTF8.GetString(description.Slice(current, terminator));
            if (Path.GetFileName(fileName).Equals(executableName, StringComparison.Ordinal))
            {
                summary.HasExecutableFileName = true;
            }
            if (Path.GetFileName(fileName).Equals(deletedName, StringComparison.Ordinal))
            {
                summary.HasDeletedMappingFileName = true;
            }
            current += terminator + 1;
        }
    }

    private static void ValidateSpecialDiagnostics(FileStream stream, List<LoadSegment> loadSegments)
    {
        LoadSegment? specialDiagnosticsSegment = null;
        foreach (LoadSegment segment in loadSegments)
        {
            if (segment.VirtualAddress == SpecialDiagInfoAddress)
            {
                if (specialDiagnosticsSegment is not null)
                {
                    throw new InvalidDataException("The dump has multiple special diagnostics PT_LOAD segments.");
                }
                specialDiagnosticsSegment = segment;
            }
        }

        if (specialDiagnosticsSegment is not LoadSegment diagnosticsSegment ||
            diagnosticsSegment.Flags != PfRead ||
            diagnosticsSegment.FileSize != SpecialDiagInfoSize ||
            diagnosticsSegment.MemorySize != SpecialDiagInfoSize)
        {
            throw new InvalidDataException("The special diagnostics PT_LOAD segment is missing or invalid.");
        }

        byte[] header = ReadVirtualMemory(stream, loadSegments, SpecialDiagInfoAddress, 40);
        if (!header.AsSpan(0, SpecialDiagSignature.Length).SequenceEqual(SpecialDiagSignature) ||
            header[SpecialDiagSignature.Length] != 0)
        {
            throw new InvalidDataException("The special diagnostics signature is missing.");
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(16)) != SpecialDiagInfoVersion)
        {
            throw new InvalidDataException("The special diagnostics version is incorrect.");
        }

        ulong exceptionRecordAddress = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(SpecialDiagExceptionRecordOffset));
        ulong runtimeBaseAddress = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(SpecialDiagRuntimeBaseOffset));
        if (exceptionRecordAddress == 0 || runtimeBaseAddress == 0)
        {
            throw new InvalidDataException(
                $"Special diagnostics has invalid addresses: exception=0x{exceptionRecordAddress:X}, runtime=0x{runtimeBaseAddress:X}.");
        }

        byte[] runtimeHeader = ReadVirtualMemory(stream, loadSegments, runtimeBaseAddress, ElfMagic.Length);
        if (!runtimeHeader.AsSpan().SequenceEqual(ElfMagic))
        {
            throw new InvalidDataException("RuntimeBaseAddress does not point to an ELF image.");
        }

        byte[] exceptionRecord = ReadVirtualMemory(stream, loadSegments, exceptionRecordAddress, ExceptionRecordSize);
        uint exceptionCode = BinaryPrimitives.ReadUInt32LittleEndian(exceptionRecord);
        uint exceptionFlags = BinaryPrimitives.ReadUInt32LittleEndian(exceptionRecord.AsSpan(4));
        ulong nestedRecord = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(8));
        ulong exceptionAddress = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(16));
        uint parameterCount = BinaryPrimitives.ReadUInt32LittleEndian(exceptionRecord.AsSpan(ExceptionRecordParameterCountOffset));
        ulong fastFailCode = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(ExceptionRecordInformationOffset));
        ulong exceptionHResult = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(ExceptionRecordInformationOffset + 8));
        ulong triageBufferAddress = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(ExceptionRecordInformationOffset + 16));
        ulong triageBufferSize = BinaryPrimitives.ReadUInt64LittleEndian(exceptionRecord.AsSpan(ExceptionRecordInformationOffset + 24));

        if (exceptionCode != StatusStackBufferOverrun ||
            exceptionFlags != ExceptionNoncontinuable ||
            nestedRecord != 0 ||
            exceptionAddress == 0 ||
            parameterCount != 4 ||
            fastFailCode != FastFailExceptionDotNetAot ||
            exceptionHResult != InvalidOperationHResult ||
            triageBufferAddress == 0 ||
            triageBufferSize == 0 ||
            triageBufferSize > 1024 * 1024)
        {
            throw new InvalidDataException(
                $"Unexpected fail-fast record: code=0x{exceptionCode:X8}, flags={exceptionFlags}, nested=0x{nestedRecord:X}, " +
                $"address=0x{exceptionAddress:X}, params={parameterCount}, fastFail=0x{fastFailCode:X}, " +
                $"hr=0x{exceptionHResult:X}, triage=0x{triageBufferAddress:X}+{triageBufferSize}.");
        }

        byte[] triageBytes = ReadVirtualMemory(stream, loadSegments, triageBufferAddress, checked((int)triageBufferSize));
        string triageJson = Encoding.UTF8.GetString(triageBytes);
        if (!triageJson.StartsWith('{') ||
            !triageJson.EndsWith('}') ||
            !triageJson.Contains("\"reason\":1", StringComparison.Ordinal) ||
            !triageJson.Contains(ExceptionMessage, StringComparison.Ordinal) ||
            !triageJson.Contains("System.InvalidOperationException", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Crash JSON does not contain the expected managed exception data: {triageJson}");
        }
    }

    private static byte[] ReadVirtualMemory(
        FileStream stream,
        List<LoadSegment> loadSegments,
        ulong virtualAddress,
        int size)
    {
        ulong unsignedSize = checked((ulong)size);
        foreach (LoadSegment segment in loadSegments)
        {
            if (virtualAddress >= segment.VirtualAddress &&
                virtualAddress - segment.VirtualAddress <= segment.FileSize &&
                unsignedSize <= segment.FileSize - (virtualAddress - segment.VirtualAddress))
            {
                ulong fileOffset = checked(segment.FileOffset + virtualAddress - segment.VirtualAddress);
                return ReadBytes(stream, fileOffset, size);
            }
        }

        throw new InvalidDataException($"Virtual range 0x{virtualAddress:X}+0x{size:X} is absent from PT_LOAD data.");
    }

    private static byte[] ReadBytes(FileStream stream, ulong offset, int size)
    {
        ValidateFileRange(stream, offset, checked((ulong)size), "read");
        byte[] bytes = new byte[size];
        stream.Position = checked((long)offset);
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void ValidateFileRange(FileStream stream, ulong offset, ulong size, string description)
    {
        ulong fileLength = checked((ulong)stream.Length);
        if (offset > fileLength || size > fileLength - offset)
        {
            throw new InvalidDataException($"The {description} range 0x{offset:X}+0x{size:X} extends past the dump.");
        }
    }

    private static ulong Align4(uint value) => ((ulong)value + 3) & ~3UL;

    private static string ParseOutputValue(string output, string prefix)
    {
        int start = output.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidDataException($"Child output does not contain '{prefix}'.");
        }

        start += prefix.Length;
        int end = output.IndexOfAny(new[] { '\r', '\n' }, start);
        return (end < 0 ? output[start..] : output[start..end]).Trim();
    }

    private static ulong ParseHexOutputValue(string output, string prefix)
    {
        string value = ParseOutputValue(output, prefix);
        if (!ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong result))
        {
            throw new InvalidDataException($"'{value}' is not a hexadecimal address.");
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int CrashChild()
    {
        string mappingPath = Path.Combine(Path.GetTempPath(), "createdump_deleted_" + Path.GetRandomFileName());
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
        ulong probeAddress = checked((ulong)(mappingPointer + probeOffset));
        Console.WriteLine($"DELETED_MAPPING_PROBE=0x{probeAddress:X}");
        Console.WriteLine($"DELETED_MAPPING_FILE={mappingPath}");
        Console.WriteLine("Throwing the unhandled managed exception.");
        Console.Out.Flush();

        throw new InvalidOperationException(ExceptionMessage);
    }
}
