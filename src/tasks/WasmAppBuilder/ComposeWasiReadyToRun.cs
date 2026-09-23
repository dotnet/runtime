// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

public sealed class ComposeWasiReadyToRun : Task
{
    [Required, NotNull]
    public string? CompositePath { get; set; }

    public bool InspectOnly { get; set; }

    public string? ComponentPath { get; set; }
    public string? OutputDirectory { get; set; }
    public string? OutputPath { get; set; }
    public string? WasmToolsPath { get; set; }
    public string? WasmMergePath { get; set; }
    public string? WasmOptPath { get; set; }
    public ITaskItem[] ComponentStubs { get; set; } = Array.Empty<ITaskItem>();
    public string? StubOutputDirectory { get; set; }

    [Output]
    public int FunctionCount { get; private set; }

    [Output]
    public int PayloadSize { get; private set; }

    [Output]
    public ITaskItem[] FileWrites { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            WasiR2RComposition.InspectComposite(CompositePath, out int functionCount, out int payloadSize);
            FunctionCount = functionCount;
            PayloadSize = payloadSize;

            if (InspectOnly)
                return true;

            ValidateCompositionArguments();
            NormalizeCompositionPaths();
            File.Delete(OutputPath!);
            Directory.CreateDirectory(OutputDirectory!);
            DeleteUnbundledModules(OutputDirectory!);

            string unbundleOutput = Path.Combine(OutputDirectory!, "unbundle-output.wasm");
            Run(WasmToolsPath!, $"component unbundle {Quote(ComponentPath!)} --module-dir {Quote(OutputDirectory!)} -o {Quote(unbundleOutput)}");
            string hostModule = Directory.GetFiles(OutputDirectory!, "*module0*.wasm").OrderBy(path => path, StringComparer.Ordinal).FirstOrDefault()
                ?? throw new LogAsErrorException($"Unbundling {ComponentPath} produced no *module0*.wasm file in {OutputDirectory}.");

            WasiR2RComposition.InspectHost(
                hostModule,
                out int imageBase,
                out int imageCapacity,
                out int tableBase,
                out int reservedTableStart);
            int compositeTableEnd = checked(tableBase + FunctionCount);
            if (compositeTableEnd > reservedTableStart)
                throw new LogAsErrorException(
                    $"The composite needs table slots {tableBase}..{compositeTableEnd - 1}, " +
                    $"but the host's functions begin at {reservedTableStart}.");
            if (PayloadSize > imageCapacity)
                throw new LogAsErrorException(
                    $"The composite payload is {PayloadSize} bytes but the host staging buffer is only {imageCapacity} bytes.");

            Log.LogMessage(MessageImportance.High,
                $"WASI R2R composition: imageBase={imageBase} tableBase={tableBase} " +
                $"reservedSlots={reservedTableStart} compositeFuncs={FunctionCount} payload={PayloadSize} cap={imageCapacity}");

            string shimWatPath = Path.Combine(OutputDirectory!, "shim.wat");
            string shimPath = Path.Combine(OutputDirectory!, "shim.wasm");
            File.WriteAllText(shimWatPath, CreateShimWat(imageBase, tableBase, PayloadSize));
            Run(WasmToolsPath!, $"parse {Quote(shimWatPath)} -o {Quote(shimPath)}");
            Run(WasmToolsPath!, $"validate --features all {Quote(shimPath)}");

            string mergedPath = Path.Combine(OutputDirectory!, "merged.wasm");
            Run(WasmMergePath!,
                $"-g --all-features --enable-gc {Quote(hostModule)} webcil {Quote(shimPath)} webcil " +
                $"{Quote(CompositePath)} composite -o {Quote(mergedPath)}");

            string finalModulePath = Path.Combine(OutputDirectory!, "final.wasm");
            Run(WasmOptPath!,
                $"{Quote(mergedPath)} --all-features -g --simplify-globals -o {Quote(finalModulePath)}");

            WasiR2RComposition.ReplaceFirstCoreModule(ComponentPath!, finalModulePath, OutputPath!);
            Run(WasmToolsPath!, $"validate --features all {Quote(OutputPath!)}");

            List<ITaskItem> fileWrites = new() { new TaskItem(OutputPath!) };
            if (ComponentStubs.Length > 0)
            {
                Directory.CreateDirectory(StubOutputDirectory!);
                foreach (ITaskItem stub in ComponentStubs)
                {
                    string inputPath = stub.GetMetadata("FullPath");
                    string outputPath = Path.Combine(
                        StubOutputDirectory!,
                        Path.GetFileNameWithoutExtension(inputPath) + ".dll");
                    WasiR2RComposition.ExtractPassiveWebcilPayload(inputPath, outputPath);
                    TaskItem output = new(outputPath);
                    fileWrites.Add(output);
                }
            }

            FileWrites = fileWrites.ToArray();
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException ex)
        {
            DeleteFailedOutput();
            Log.LogError(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            DeleteFailedOutput();
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private void ValidateCompositionArguments()
    {
        RequireFile(ComponentPath, nameof(ComponentPath));
        RequireFile(WasmToolsPath, nameof(WasmToolsPath));
        RequireFile(WasmMergePath, nameof(WasmMergePath));
        RequireFile(WasmOptPath, nameof(WasmOptPath));
        if (string.IsNullOrEmpty(OutputDirectory))
            throw new LogAsErrorException($"{nameof(OutputDirectory)} is required.");
        if (string.IsNullOrEmpty(OutputPath))
            throw new LogAsErrorException($"{nameof(OutputPath)} is required.");
        if (ComponentStubs.Length > 0 && string.IsNullOrEmpty(StubOutputDirectory))
            throw new LogAsErrorException($"{nameof(StubOutputDirectory)} is required when component stubs are provided.");
    }

    private static void RequireFile(string? path, string propertyName)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new LogAsErrorException($"{propertyName} does not name an existing file: '{path}'.");
    }

    private void NormalizeCompositionPaths()
    {
        CompositePath = Path.GetFullPath(CompositePath);
        ComponentPath = Path.GetFullPath(ComponentPath!);
        OutputDirectory = Path.GetFullPath(OutputDirectory!);
        OutputPath = Path.GetFullPath(OutputPath!);
        WasmToolsPath = Path.GetFullPath(WasmToolsPath!);
        WasmMergePath = Path.GetFullPath(WasmMergePath!);
        WasmOptPath = Path.GetFullPath(WasmOptPath!);
        if (StubOutputDirectory is not null)
            StubOutputDirectory = Path.GetFullPath(StubOutputDirectory);
    }

    private static void DeleteUnbundledModules(string outputDirectory)
    {
        foreach (string path in Directory.GetFiles(outputDirectory, "*module0*.wasm"))
            File.Delete(path);
    }

    private static string CreateShimWat(int memoryBase, int tableBase, int payloadSize) =>
        FormattableString.Invariant($"""
            (module
              (import "composite" "patchWebcilHeader" (func $patchWebcilHeader (param i32 i32)))
              (global (export "__memory_base") i32 (i32.const {memoryBase}))
              (global (export "__table_base") i32 (i32.const {tableBase}))
              (func $start
                i32.const {memoryBase}
                i32.const {payloadSize}
                call $patchWebcilHeader)
              (start $start))
            """);

    private void Run(string tool, string arguments)
    {
        (int exitCode, string output) = Utils.TryRunProcess(
            Log, tool, arguments, workingDir: OutputDirectory, silent: true);
        if (exitCode != 0)
            throw new LogAsErrorException($"{Path.GetFileName(tool)} failed with exit code {exitCode}:{Environment.NewLine}{output}");
    }

    private void DeleteFailedOutput()
    {
        if (InspectOnly || string.IsNullOrEmpty(OutputPath))
            return;

        try
        {
            File.Delete(OutputPath);
        }
        catch (IOException ex)
        {
            Log.LogWarning($"Failed to delete incomplete WASI R2R output '{OutputPath}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.LogWarning($"Failed to delete incomplete WASI R2R output '{OutputPath}': {ex.Message}");
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
