// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

    /// <summary>
    /// Further self-installing R2R images merged alongside <see cref="CompositePath"/>, such as the prebuilt
    /// non-composite System.Private.CoreLib image. Optional <c>RegistryName</c> metadata is the name the
    /// runtime requests the image by; it defaults to the image's file name with a <c>.dll</c> extension.
    /// </summary>
    public ITaskItem[] AdditionalImages { get; set; } = Array.Empty<ITaskItem>();
    public string? StubOutputDirectory { get; set; }

    /// <summary>
    /// Runs wasm-opt over the composed module. Off by default so crossgen2's output reaches the app as
    /// emitted; optimizing also needs several times more memory for large composites.
    /// </summary>
    public bool Optimize { get; set; }

    [Output]
    public int FunctionCount { get; private set; }

    [Output]
    public int TableSlotCount { get; private set; }

    [Output]
    public int PayloadSize { get; private set; }

    [Output]
    public ITaskItem[] FileWrites { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            WasiR2RComposition.InspectComposite(
                CompositePath,
                out int functionCount,
                out int tableSlotCount,
                out int payloadSize);
            FunctionCount = functionCount;
            TableSlotCount = tableSlotCount;
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
                out int reservedTableStart,
                out int registryBase,
                out int registryCapacity,
                out int registryEntrySize);
            int nameCapacity = registryEntrySize - 8;
            if (nameCapacity <= 0)
                throw new LogAsErrorException($"The host reports an invalid R2R registry entry size {registryEntrySize}.");

            List<ImageLayout> images = LayOutImages(imageBase, tableBase, nameCapacity);
            if (images.Count > registryCapacity)
                throw new LogAsErrorException(
                    $"{images.Count} R2R images were supplied, but the host registry holds at most {registryCapacity}.");
            ImageLayout last = images[images.Count - 1];
            int tableEnd = checked(last.TableBase + last.TableSlots);
            if (tableEnd > reservedTableStart)
                throw new LogAsErrorException(
                    $"The R2R images need table slots {tableBase}..{tableEnd - 1}, " +
                    $"but the host's functions begin at {reservedTableStart}.");
            long imageEnd = (long)last.MemoryBase + last.PayloadSize - imageBase;
            if (imageEnd > imageCapacity)
                throw new LogAsErrorException(
                    $"The R2R image payloads need {imageEnd} bytes but the host staging buffer is only {imageCapacity} bytes.");

            foreach (ImageLayout image in images)
            {
                Log.LogMessage(MessageImportance.High,
                    $"WASI R2R composition: image{image.Index} '{image.RegistryName}' memoryBase={image.MemoryBase} " +
                    $"tableBase={image.TableBase} tableSlots={image.TableSlots} payload={image.PayloadSize}");
            }
            Log.LogMessage(MessageImportance.High,
                $"WASI R2R composition: images={images.Count} bufferUsed={imageEnd}/{imageCapacity} " +
                $"tableUsed={tableEnd - tableBase}/{reservedTableStart - tableBase}");

            // Every self-installing image imports webcil.__memory_base/__table_base and exports
            // patchWebcilHeader. Give each image uniquely named base imports and patch export: wasm-merge
            // resolves by name and, under --skip-export-conflicts, keeps only the first export of a name, so
            // shared names would leave the later images' imports unresolved.
            StringBuilder mergeArgs = new($"-g --all-features --enable-gc --skip-export-conflicts {Quote(hostModule)} webcil");
            foreach (ImageLayout image in images)
            {
                string basesWatPath = Path.Combine(OutputDirectory!, $"bases{image.Index}.wat");
                string basesPath = Path.Combine(OutputDirectory!, $"bases{image.Index}.wasm");
                File.WriteAllText(basesWatPath, FormattableString.Invariant($"""
                    (module
                      (global (export "memory_base_{image.Index}") i32 (i32.const {image.MemoryBase}))
                      (global (export "table_base_{image.Index}") i32 (i32.const {image.TableBase})))
                    """));
                Run(WasmToolsPath!, $"parse {Quote(basesWatPath)} -o {Quote(basesPath)}");

                string renamedPath = Path.Combine(OutputDirectory!, $"image{image.Index}.wasm");
                WasiR2RComposition.RenameImportsAndExports(
                    image.Path,
                    renamedPath,
                    new Dictionary<(string Module, string Field), (string Module, string Field)>
                    {
                        [("webcil", "__memory_base")] = ($"bases{image.Index}", $"memory_base_{image.Index}"),
                        [("webcil", "__table_base")] = ($"bases{image.Index}", $"table_base_{image.Index}"),
                    },
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["patchWebcilHeader"] = $"patchWebcilHeader_{image.Index}",
                    });
                mergeArgs.Append(FormattableString.Invariant($" {Quote(basesPath)} bases{image.Index} {Quote(renamedPath)} image{image.Index}"));
            }

            string startWatPath = Path.Combine(OutputDirectory!, "start.wat");
            string startPath = Path.Combine(OutputDirectory!, "start.wasm");
            File.WriteAllText(startWatPath, CreateStartWat(images, registryBase, registryEntrySize, nameCapacity));
            Run(WasmToolsPath!, $"parse {Quote(startWatPath)} -o {Quote(startPath)}");
            Run(WasmToolsPath!, $"validate --features all {Quote(startPath)}");
            mergeArgs.Append($" {Quote(startPath)} start");

            string mergedPath = Path.Combine(OutputDirectory!, "merged.wasm");
            mergeArgs.Append($" -o {Quote(mergedPath)}");
            Run(WasmMergePath!, mergeArgs.ToString());

            // Merging turns the images' imported base globals into module-defined globals, which engines
            // only accept in constant expressions with the extended-const proposal. Fold them to constants.
            string finalModulePath = Path.Combine(OutputDirectory!, "final.wasm");
            if (Optimize)
            {
                Run(WasmOptPath!,
                    $"{Quote(mergedPath)} --all-features -g --simplify-globals -o {Quote(finalModulePath)}");
            }
            else
            {
                WasiR2RComposition.FoldConstantGlobalReads(mergedPath, finalModulePath);
            }

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
                    WasiR2RComposition.ExtractComponentStubPayload(inputPath, outputPath);
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
        if (Optimize)
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
        if (Optimize)
            WasmOptPath = Path.GetFullPath(WasmOptPath!);
        if (StubOutputDirectory is not null)
            StubOutputDirectory = Path.GetFullPath(StubOutputDirectory);
    }

    private static void DeleteUnbundledModules(string outputDirectory)
    {
        foreach (string path in Directory.GetFiles(outputDirectory, "*module0*.wasm"))
            File.Delete(path);
    }

    private sealed record ImageLayout(int Index, string Path, string RegistryName, int MemoryBase, int TableBase, int TableSlots, int PayloadSize);

    // Lays the images out back to back: payloads in the host staging buffer (16-byte aligned, matching the
    // buffer's own alignment) and functions in the reserved table range.
    private List<ImageLayout> LayOutImages(int imageBase, int tableBase, int nameCapacity)
    {
        List<(string Path, string RegistryName, int TableSlots, int PayloadSize)> inputs = new()
        {
            // Component stubs name their owner by the file name crossgen2 wrote, so the composite's own file
            // name is the one the host must answer to.
            (CompositePath, Path.GetFileName(CompositePath), TableSlotCount, PayloadSize),
        };
        foreach (ITaskItem item in AdditionalImages)
        {
            string path = Path.GetFullPath(item.ItemSpec);
            RequireFile(path, nameof(AdditionalImages));
            string registryName = item.GetMetadata("RegistryName");
            if (string.IsNullOrEmpty(registryName))
                registryName = Path.GetFileNameWithoutExtension(path) + ".dll";
            WasiR2RComposition.InspectComposite(path, out _, out int slots, out int payload);
            inputs.Add((path, registryName, slots, payload));
        }

        HashSet<string> names = new(StringComparer.Ordinal);
        List<ImageLayout> images = new();
        long memoryBase = imageBase;
        long nextTableBase = tableBase;
        foreach ((string path, string registryName, int slots, int payload) in inputs)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(registryName);
            if (nameBytes.Length == 0 || Array.IndexOf(nameBytes, (byte)0) >= 0)
                throw new LogAsErrorException($"The R2R image name '{registryName}' cannot be recorded in the host.");
            if (nameBytes.Length >= nameCapacity)
                throw new LogAsErrorException(
                    $"The R2R image name '{registryName}' is {nameBytes.Length} UTF-8 bytes, but the host records at most {nameCapacity - 1}.");
            if (!names.Add(registryName))
                throw new LogAsErrorException($"More than one R2R image is named '{registryName}'.");

            images.Add(new ImageLayout(images.Count, path, registryName, checked((int)memoryBase), checked((int)nextTableBase), slots, payload));
            memoryBase = (memoryBase + payload + 15) & ~15L;
            nextTableBase += slots;
        }
        return images;
    }

    // The start module imports the host's memory (the merge names the host "webcil") so its active segment
    // fills the host's image registry, and calls each image's patchWebcilHeader to record where it landed.
    private static string CreateStartWat(List<ImageLayout> images, int registryBase, int registryEntrySize, int nameCapacity)
    {
        StringBuilder wat = new();
        wat.AppendLine("(module");
        wat.AppendLine("  (import \"webcil\" \"memory\" (memory 0))");
        foreach (ImageLayout image in images)
            wat.AppendLine(FormattableString.Invariant($"  (import \"image{image.Index}\" \"patchWebcilHeader_{image.Index}\" (func $patch{image.Index} (param i32 i32)))"));

        StringBuilder registry = new();
        foreach (ImageLayout image in images)
        {
            byte[] entry = new byte[registryEntrySize];
            Encoding.UTF8.GetBytes(image.RegistryName, 0, image.RegistryName.Length, entry, 0);
            BitConverter.GetBytes(image.MemoryBase).CopyTo(entry, nameCapacity);
            BitConverter.GetBytes(image.PayloadSize).CopyTo(entry, nameCapacity + 4);
            foreach (byte b in entry)
                registry.Append('\\').Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }
        wat.AppendLine(FormattableString.Invariant($"  (data (i32.const {registryBase}) \"{registry}\")"));

        wat.AppendLine("  (func $start");
        foreach (ImageLayout image in images)
        {
            wat.AppendLine(FormattableString.Invariant($"    i32.const {image.MemoryBase}"));
            wat.AppendLine(FormattableString.Invariant($"    i32.const {image.PayloadSize}"));
            wat.AppendLine(FormattableString.Invariant($"    call $patch{image.Index}"));
        }
        wat.AppendLine("  )");
        wat.AppendLine("  (start $start))");
        return wat.ToString();
    }

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
