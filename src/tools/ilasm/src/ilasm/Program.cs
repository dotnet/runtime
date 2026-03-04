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
using System.Text;

namespace ILAssembler;

internal sealed class Program
{
    private readonly IlasmRootCommand _command;

    public Program(IlasmRootCommand command)
    {
        _command = command;
    }

    public int Run()
    {
        Stopwatch? stopwatch = null;
        if (Get(_command.Clock))
        {
            stopwatch = Stopwatch.StartNew();
        }

        string[]? inputFiles = _command.Result.GetValue(_command.InputFilePaths);
        bool quiet = Get(_command.Quiet);

        if (!Get(_command.NoLogo) && !quiet)
        {
            Console.WriteLine(IlasmRootCommand.ProductName);
            Console.WriteLine();
        }

        if (inputFiles is null || inputFiles.Length == 0)
        {
            Console.Error.WriteLine("Error: No input file specified");
            return 1;
        }

        // Validate all input files exist
        foreach (string file in inputFiles)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Error: Input file not found: {file}");
                return 1;
            }
        }

        // Determine output file (based on first input file)
        bool isDll = Get(_command.BuildDll);
        string? outputPath = Get(_command.OutputFilePath) ??
            $"{Path.GetFileNameWithoutExtension(inputFiles[0])}{(isDll ? ".dll" : ".exe")}";

        int exitCode = 0;
        try
        {
            // Report each file being assembled
            foreach (string file in inputFiles)
            {
                if (!quiet)
                {
                    Console.WriteLine($"Assembling '{file}' to {(isDll ? "DLL" : "EXE")} --> '{outputPath}'");
                }
            }

            // Concatenate all input files
            var contentBuilder = new StringBuilder();
            foreach (string file in inputFiles)
            {
                contentBuilder.AppendLine(File.ReadAllText(file));
            }
            string content = contentBuilder.ToString();

            // Use the first file as the primary document for source tracking
            var document = new SourceText(content, inputFiles[0]);

            // Build options
            bool errorTolerant = Get(_command.ErrorTolerant);
            var options = new Options
            {
                NoAutoInherit = Get(_command.NoAutoInherit),
                ErrorTolerant = errorTolerant,
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

            // Set up include path for #include directive resolution
            string? includePath = Get(_command.IncludePath);
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(inputFiles[0])) ?? ".";

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

            byte[] LoadResource(string path)
            {
                // Try the path as-is first
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }

                // Try relative to the base directory
                string fullPath = Path.Combine(baseDir, path);
                if (File.Exists(fullPath))
                {
                    return File.ReadAllBytes(fullPath);
                }

                throw new FileNotFoundException($"Resource file not found: {path}");
            }

            // Compile
            var compiler = new DocumentCompiler();
            var (diagnostics, peBuilder) = compiler.Compile(
                document,
                LoadIncludedDocument,
                LoadResource,
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

            // In error-tolerant mode, continue even with errors
            if (peBuilder is null)
            {
                Console.Error.WriteLine("***** FAILURE *****");
                return 1;
            }

            if (hasErrors && !errorTolerant)
            {
                Console.Error.WriteLine("***** FAILURE *****");
                return 1;
            }

            // Write output
            using var outputStream = File.Create(outputPath);
            var blobBuilder = new BlobBuilder();
            peBuilder.Serialize(blobBuilder);
            blobBuilder.WriteContentTo(outputStream);

            if (hasErrors)
            {
                if (!quiet)
                {
                    Console.WriteLine("Output file contains errors");
                }
            }
            else if (!quiet)
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

    private T Get<T>(Argument<T> argument) => _command.Result.GetValue(argument)!;

    private T Get<T>(Option<T> option) => _command.Result.GetValue(option)!;

    private static int Main(string[] args) =>
        new IlasmRootCommand()
            .Parse(args)
            .Invoke();
}
