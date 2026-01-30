// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

internal sealed class Program
{
    private readonly IlasmRootCommand _command;

    public Program(IlasmRootCommand command)
    {
        _command = command;
    }

    private T Get<T>(Argument<T> argument) => _command.Result.GetValue(argument)!;
    private T Get<T>(Option<T> option) => _command.Result.GetValue(option)!;

    public int Run()
    {
        Stopwatch? stopwatch = null;
        if (Get(_command.Clock))
        {
            stopwatch = Stopwatch.StartNew();
        }

        string? inputFile = Get(_command.InputFilePath);
        bool quiet = Get(_command.Quiet);

        if (!Get(_command.NoLogo) && !quiet)
        {
            Console.WriteLine(IlasmRootCommand.ProductName);
            Console.WriteLine();
        }

        if (string.IsNullOrEmpty(inputFile))
        {
            Console.Error.WriteLine("Error: No input file specified");
            return 1;
        }

        // Determine output file
        bool isDll = Get(_command.BuildDll);
        string? outputPath = Get(_command.OutputFilePath) ??
            $"{Path.GetFileNameWithoutExtension(inputFile)}{(isDll ? ".dll" : ".exe")}";

        // Validate input file exists
        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
            return 1;
        }

        int exitCode = 0;
        try
        {
            if (!quiet)
            {
                Console.WriteLine($"Assembling '{inputFile}' to {(isDll ? "DLL" : "EXE")} --> '{outputPath}'");
            }

            string content = File.ReadAllText(inputFile);
            var document = new SourceText(content, inputFile);

            // Build options
            var options = new Options
            {
                NoAutoInherit = Get(_command.NoAutoInherit),
            };

            // Apply PE header overrides from command line
            int subsystem = Get(_command.Subsystem);
            if (subsystem != 0)
            {
                options.Subsystem = (Subsystem)subsystem;
            }

            string? ssver = Get(_command.SubsystemVersion);
            if (!string.IsNullOrEmpty(ssver))
            {
                var parts = ssver.Split('.');
                if (parts.Length == 2 && ushort.TryParse(parts[0], out ushort ssvMajor) && ushort.TryParse(parts[1], out ushort ssvMinor))
                {
                    options.SubsystemVersion = (ssvMajor, ssvMinor);
                }
            }

            int alignment = Get(_command.Alignment);
            if (alignment != 0)
            {
                options.FileAlignment = alignment;
            }

            long imageBase = Get(_command.ImageBase);
            if (imageBase != 0)
            {
                options.ImageBase = imageBase;
            }

            int stackReserve = Get(_command.StackReserve);
            if (stackReserve != 0)
            {
                options.StackReserve = stackReserve;
            }

            int flags = Get(_command.Flags);
            if (flags != 0)
            {
                options.CorFlags = (CorFlags)flags;
            }

            // Target machine
            if (Get(_command.TargetX64) || Get(_command.Pe64))
            {
                options.Machine = Machine.Amd64;
            }
            else if (Get(_command.TargetArm))
            {
                options.Machine = Machine.Arm;
            }
            else if (Get(_command.TargetArm64))
            {
                options.Machine = Machine.Arm64;
            }

            // PE characteristics
            options.AppContainer = Get(_command.AppContainer);
            options.HighEntropyVA = Get(_command.HighEntropyVa);
            options.StripReloc = Get(_command.StripReloc);
            options.Prefer32Bit = Get(_command.Prefer32Bit);

            // Deterministic and metadata version
            options.Deterministic = Get(_command.Deterministic);
            options.MetadataVersion = Get(_command.MetadataVersion);

            // Debug options
            options.Debug = Get(_command.Debug);
            options.Pdb = Get(_command.Pdb);
            options.DebugMode = Get(_command.DebugMode);

            // Assembly options
            options.AssemblyName = Get(_command.AssemblyName);
            options.KeyFile = Get(_command.KeyFile);
            options.Optimize = Get(_command.Optimize);
            options.Fold = Get(_command.Fold);

            // Parse metadata stream version (format: major.minor)
            string? msvString = Get(_command.MetadataStreamVersion);
            if (msvString is not null)
            {
                string[] parts = msvString.Split('.');
                if (parts.Length == 2 && byte.TryParse(parts[0], out byte msvMajor) && byte.TryParse(parts[1], out byte msvMinor))
                {
                    options.MetadataStreamVersion = (msvMajor, msvMinor);
                }
                else
                {
                    throw new ArgumentException($"Invalid metadata stream version format: '{msvString}'. Expected format: major.minor (e.g., 2.0)");
                }
            }

            // Set up include path for #include directive resolution
            string? includePath = Get(_command.IncludePath);
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(inputFile)) ?? ".";

            SourceText LoadIncludedDocument(string path)
            {
                // Try the path as-is first
                if (File.Exists(path))
                {
                    return new SourceText(File.ReadAllText(path), path);
                }

                // Try relative to the base directory
                string fullPath = Path.Combine(baseDir, path);
                if (File.Exists(fullPath))
                {
                    return new SourceText(File.ReadAllText(fullPath), fullPath);
                }

                // Try the include path if specified
                if (!string.IsNullOrEmpty(includePath))
                {
                    fullPath = Path.Combine(includePath, path);
                    if (File.Exists(fullPath))
                    {
                        return new SourceText(File.ReadAllText(fullPath), fullPath);
                    }
                }

                throw new FileNotFoundException($"Include file not found: {path}");
            }

            // Compile
            var compiler = new DocumentCompiler();
            var (diagnostics, peBuilder) = compiler.Compile(
                document,
                LoadIncludedDocument,
                _ => default!,
                options);

            // Report diagnostics
            bool hasErrors = false;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                    Console.Error.WriteLine($"Error: {diagnostic.Location}: {diagnostic.Message}");
                }
                else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                {
                    Console.WriteLine($"Warning: {diagnostic.Location}: {diagnostic.Message}");
                }
            }

            if (hasErrors || peBuilder is null)
            {
                Console.Error.WriteLine("***** FAILURE *****");
                return 1;
            }

            // Write output
            using var outputStream = File.Create(outputPath);
            var blobBuilder = new BlobBuilder();
            peBuilder.Serialize(blobBuilder);

            // Post-process: patch metadata stream version if specified
            if (options.MetadataStreamVersion is var (major, minor))
            {
                PatchMetadataStreamVersion(blobBuilder, major, minor);
            }

            blobBuilder.WriteContentTo(outputStream);

            if (!quiet)
            {
                Console.WriteLine("Operation completed successfully");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            exitCode = 1;
        }

        if (stopwatch is not null)
        {
            stopwatch.Stop();
            Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds} ms");
        }

        return exitCode;
    }

    /// <summary>
    /// Patches the metadata stream version in the serialized PE.
    /// The #~ or #- stream header contains MajorVersion and MinorVersion at offset 4-5 after Reserved.
    /// </summary>
    private static void PatchMetadataStreamVersion(BlobBuilder blobBuilder, byte majorVersion, byte minorVersion)
    {
        // Convert to byte array for patching
        byte[] peBytes = blobBuilder.ToArray();

        // Find the metadata signature "BSJB" (0x424A5342)
        int metadataOffset = -1;
        for (int i = 0; i < peBytes.Length - 4; i++)
        {
            if (peBytes[i] == 0x42 && peBytes[i + 1] == 0x53 && peBytes[i + 2] == 0x4A && peBytes[i + 3] == 0x42)
            {
                metadataOffset = i;
                break;
            }
        }

        if (metadataOffset < 0)
        {
            return; // No metadata found
        }

        // Parse STORAGESIGNATURE to find streams
        // Structure: Signature(4) + MajorVersion(2) + MinorVersion(2) + ExtraData(4) + VersionLength(4) + VersionString(variable)
        int pos = metadataOffset + 12; // Skip to VersionLength
        int versionLength = BitConverter.ToInt32(peBytes, pos);
        pos += 4 + versionLength;

        // Align to 4-byte boundary
        pos = (pos + 3) & ~3;

        // Now at STORAGEHEADER: Flags(1) + Padding(1) + Streams(2)
        int streamCount = BitConverter.ToInt16(peBytes, pos + 2);
        pos += 4;

        // Iterate through stream headers to find #~ or #-
        for (int i = 0; i < streamCount; i++)
        {
            int streamOffset = BitConverter.ToInt32(peBytes, pos);
            _ = BitConverter.ToInt32(peBytes, pos + 4); // streamSize - not used but must skip
            pos += 8;

            // Read stream name (null-terminated, 4-byte aligned)
            int nameStart = pos;
            while (peBytes[pos] != 0)
            {
                pos++;
            }
            string streamName = System.Text.Encoding.ASCII.GetString(peBytes, nameStart, pos - nameStart);
            pos++; // Skip null terminator

            // Align to 4-byte boundary
            pos = (pos + 3) & ~3;

            // Check if this is the #~ or #- stream
            if (streamName == "#~" || streamName == "#-")
            {
                // The stream starts at metadataOffset + streamOffset
                // Stream header: Reserved(4) + MajorVersion(1) + MinorVersion(1) + ...
                int versionOffset = metadataOffset + streamOffset + 4;
                peBytes[versionOffset] = majorVersion;
                peBytes[versionOffset + 1] = minorVersion;
                break;
            }
        }

        // Clear and rewrite the blob builder with patched bytes
        blobBuilder.Clear();
        blobBuilder.WriteBytes(peBytes);
    }

    private static int Main(string[] args) =>
        new IlasmRootCommand()
            .Parse(args)
            .Invoke();
}
