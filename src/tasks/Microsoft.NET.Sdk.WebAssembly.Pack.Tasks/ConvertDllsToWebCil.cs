// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public class ConvertDllsToWebcil : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public string IntermediateOutputPath { get; set; }

    [Required]
    public bool IsEnabled { get; set; }

    public int WebcilVersion { get; set; }

    /// <summary>
    /// Optional prebuilt ReadyToRun webcil-in-wasm images that replace the plain Webcil conversion
    /// for the matching assembly (matched by file name without extension). ItemSpec is the path to
    /// the R2R .wasm. The payload/table sizes are read from the staged image itself and surfaced via
    /// <see cref="WebcilSizes"/>; no size metadata is required on these items.
    /// </summary>
    public ITaskItem[] R2RWebcilCandidates { get; set; }

    [Output]
    public ITaskItem[] WebcilCandidates { get; set; }

    /// <summary>
    /// Payload/table sizes for each produced webcil, keyed by the logical assembly name as it
    /// appears in the boot config: the file name (e.g. "System.Console.dll"), or
    /// "{culture}/{name}.dll" for satellite assemblies so that same-named satellites in different
    /// cultures don't collide. Lets the boot config carry the sizes so the runtime loader doesn't
    /// buffer/parse the wasm. PayloadSize is set for every webcil; TableSize is non-zero only for
    /// R2R images.
    /// </summary>
    [Output]
    public ITaskItem[] WebcilSizes { get; set; }

    /// <summary>
    /// Files from shared locations (runtime pack, NuGet cache) that need Framework
    /// SourceType materialization to get unique per-project Identity.
    /// When <see cref="IsEnabled"/> is true, this is non-DLL items without
    /// WasmNativeBuildOutput metadata (DLLs are converted to webcil, making them
    /// per-project already). When <see cref="IsEnabled"/> is false, DLLs are also
    /// included since they retain their shared paths without conversion.
    /// Items with WasmNativeBuildOutput metadata are always excluded — they're
    /// already unique per project.
    /// </summary>
    [Output]
    public ITaskItem[] PassThroughCandidates { get; set; }

    protected readonly List<string> _fileWrites = new();

    private Dictionary<string, ITaskItem> _r2rByName;
    private readonly List<ITaskItem> _webcilSizes = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    public override bool Execute()
    {
        var webcilCandidates = new List<ITaskItem>();
        var passThroughCandidates = new List<ITaskItem>();

        if (!IsEnabled)
        {
            // When webcil is disabled, no conversion occurs. All candidates pass
            // through unchanged as WebcilCandidates (backward compat for publish).
            // All candidates (DLLs and non-DLLs) without WasmNativeBuildOutput
            // metadata are also pass-through candidates for Framework materialization.
            // Unlike the enabled path (where DLLs are converted to webcil and become
            // per-project), disabled DLLs retain their shared NuGet cache paths and
            // need materialization to get unique per-project Identity.
            WebcilCandidates = Candidates;
            foreach (var candidate in Candidates)
            {
                if (string.IsNullOrEmpty(candidate.GetMetadata("WasmNativeBuildOutput")))
                {
                    passThroughCandidates.Add(candidate);
                }
            }
            PassThroughCandidates = passThroughCandidates.ToArray();
            return true;
        }

        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        string tmpDir = Path.Combine(IntermediateOutputPath, Guid.NewGuid().ToString("N"));
        if (!Directory.Exists(tmpDir))
            Directory.CreateDirectory(tmpDir);

        _r2rByName = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
        if (R2RWebcilCandidates != null)
        {
            foreach (var r2r in R2RWebcilCandidates)
                _r2rByName[Path.GetFileNameWithoutExtension(r2r.ItemSpec)] = r2r;
        }

        for (int i = 0; i < Candidates.Length; i++)
        {
            var candidate = Candidates[i];
            var extension = candidate.GetMetadata("Extension");

            if (extension != ".dll")
            {
                // Non-DLL files always appear in WebcilCandidates (backward compat
                // for publish and other callers that only consume WebcilCandidates).
                webcilCandidates.Add(candidate);

                // Additionally classify shared framework files as pass-throughs.
                // Items with WasmNativeBuildOutput metadata are per-project native
                // build outputs (e.g. dotnet.native.wasm from obj/wasm/for-build/)
                // that don't need Framework materialization.
                bool isNativeBuildOutput = !string.IsNullOrEmpty(candidate.GetMetadata("WasmNativeBuildOutput"));
                if (!isNativeBuildOutput)
                {
                    passThroughCandidates.Add(candidate);
                }
                continue;
            }

            try
            {
                TaskItem webcilItem = ConvertDll(tmpDir, candidate);
                webcilCandidates.Add(webcilItem);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to convert '{candidate.ItemSpec}' to webcil: {ex.Message}");
                return false;
            }
        }

        Directory.Delete(tmpDir, true);

        WebcilCandidates = webcilCandidates.ToArray();
        PassThroughCandidates = passThroughCandidates.ToArray();
        WebcilSizes = _webcilSizes.ToArray();
        return true;
    }

    private TaskItem ConvertDll(string tmpDir, ITaskItem candidate)
    {
        var dllFilePath = candidate.ItemSpec;
        var webcilFileName = Path.GetFileNameWithoutExtension(dllFilePath) + Utils.WebcilInWasmExtension;
        bool isCulture = candidate.GetMetadata("AssetTraitName") == "Culture";
        string culture = isCulture ? candidate.GetMetadata("AssetTraitValue") : null;
        string candidatePath = isCulture
            ? Path.Combine(OutputPath, culture)
            : OutputPath;

        string finalWebcil = Path.Combine(candidatePath, webcilFileName);

        if (_r2rByName != null && _r2rByName.TryGetValue(Path.GetFileNameWithoutExtension(dllFilePath), out var r2rReplacement))
        {
            return StageR2RWebcil(tmpDir, candidate, r2rReplacement, webcilFileName, candidatePath, finalWebcil, culture);
        }

        if (Utils.IsNewerThan(dllFilePath, finalWebcil))
        {
            var tmpWebcil = Path.Combine(tmpDir, webcilFileName);
            var logAdapter = new Microsoft.WebAssembly.Build.Tasks.LogAdapter(Log);
            var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: dllFilePath, outputPath: tmpWebcil, logger: logAdapter, webcilVersion: WebcilVersion);
            webcilWriter.ConvertToWebcil();

            if (!Directory.Exists(candidatePath))
                Directory.CreateDirectory(candidatePath);

            if (Utils.MoveIfDifferent(tmpWebcil, finalWebcil))
                Log.LogMessage(MessageImportance.Low, $"Generated {finalWebcil} .");
            else
                Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebcil} as the contents are unchanged.");
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"Skipping {dllFilePath} as it is older than the output file {finalWebcil}");
        }

        _fileWrites.Add(finalWebcil);

        var webcilItem = new TaskItem(finalWebcil, candidate.CloneCustomMetadata());
        webcilItem.SetMetadata("RelativePath", Path.ChangeExtension(candidate.GetMetadata("RelativePath"), Utils.WebcilInWasmExtension));
        webcilItem.SetMetadata("OriginalItemSpec", finalWebcil);

        if (webcilItem.GetMetadata("AssetTraitName") == "Culture")
        {
            string relatedAsset = webcilItem.GetMetadata("RelatedAsset");
            relatedAsset = Path.ChangeExtension(relatedAsset, Utils.WebcilInWasmExtension);
            webcilItem.SetMetadata("RelatedAsset", relatedAsset);
            Log.LogMessage(MessageImportance.Low, $"Changing related asset of {webcilItem} to {relatedAsset}.");
        }

        RecordWebcilSize(finalWebcil, culture);
        return webcilItem;
    }

    // Stage a prebuilt R2R webcil-in-wasm image in place of converting the .dll. The produced item
    // mirrors ConvertDll's output (same path, RelativePath, OriginalItemSpec) so downstream
    // fingerprinting/integrity is computed from the R2R bytes.
    private TaskItem StageR2RWebcil(string tmpDir, ITaskItem candidate, ITaskItem r2r, string webcilFileName, string candidatePath, string finalWebcil, string culture)
    {
        string r2rPath = r2r.ItemSpec;

        if (Utils.IsNewerThan(r2rPath, finalWebcil))
        {
            if (!Directory.Exists(candidatePath))
                Directory.CreateDirectory(candidatePath);

            var tmpWebcil = Path.Combine(tmpDir, webcilFileName);
            File.Copy(r2rPath, tmpWebcil, overwrite: true);

            if (Utils.MoveIfDifferent(tmpWebcil, finalWebcil))
                Log.LogMessage(MessageImportance.Low, $"Staged R2R webcil {finalWebcil} from {r2rPath} .");
            else
                Log.LogMessage(MessageImportance.Low, $"Skipped staging R2R webcil {finalWebcil} as the contents are unchanged.");
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"Skipping {r2rPath} as it is older than the output file {finalWebcil}");
        }

        _fileWrites.Add(finalWebcil);

        var webcilItem = new TaskItem(finalWebcil, candidate.CloneCustomMetadata());
        webcilItem.SetMetadata("RelativePath", Path.ChangeExtension(candidate.GetMetadata("RelativePath"), Utils.WebcilInWasmExtension));
        webcilItem.SetMetadata("OriginalItemSpec", finalWebcil);

        RecordWebcilSize(finalWebcil, culture);
        return webcilItem;
    }

    // Parses the produced webcil's data segment 0 and records payloadSize/tableSize keyed by the
    // logical assembly name (".dll" file name, or "{culture}/{name}.dll" for satellites) so that
    // GenerateWasmBootJson can emit them into the boot config without re-parsing. The runtime loader
    // requires payloadSize for every webcil-in-wasm assembly, so failing to read it is a build error
    // rather than a silent skip.
    private void RecordWebcilSize(string webcilPath, string culture)
    {
        if (!TryReadWebcilSizes(webcilPath, out int payloadSize, out int tableSize, out string failureReason))
        {
            Log.LogError($"Could not read the Webcil payload/table sizes from '{webcilPath}' ({failureReason}). The runtime loader requires payloadSize for every webcil-in-wasm assembly.");
            return;
        }

        // Key by the logical assembly name (".dll"), not the produced ".wasm" file name: the boot
        // config lists webcil assemblies under their logical ".dll" name, and GenerateWasmBootJson
        // looks these sizes up by that name (resourceName). Using ".wasm" here would never match.
        // Keying by ".dll" also avoids colliding with same-stem assets (e.g. a "X.pdb" never matches
        // "X.dll").
        string fileName = Path.ChangeExtension(Path.GetFileName(webcilPath), ".dll");
        string key = string.IsNullOrEmpty(culture) ? fileName : culture + "/" + fileName;
        var item = new TaskItem(key);
        item.SetMetadata("PayloadSize", payloadSize.ToString(CultureInfo.InvariantCulture));
        item.SetMetadata("TableSize", tableSize.ToString(CultureInfo.InvariantCulture));
        _webcilSizes.Add(item);
    }

    // Reads payloadSize and tableSize from data segment 0 of a Webcil-in-wasm image without
    // instantiating it. tableSize > 0 indicates a ReadyToRun image. The data section is the last
    // wasm section, so for R2R images (large code sections) it can start well beyond the first few
    // KB; this streams through the section headers, seeking past each body, instead of reading a
    // fixed prefix. All multi-byte integers in the wasm binary format are little-endian and are read
    // as such regardless of host endianness. See docs/design/mono/webcil.md.
    private static bool TryReadWebcilSizes(string path, out int payloadSize, out int tableSize, out string failureReason)
    {
        payloadSize = 0;
        tableSize = 0;
        failureReason = null;
        try
        {
            using var fs = File.OpenRead(path);

            byte[] header = new byte[8];
            if (!TryFill(fs, header, 8)
                || ReadUInt32LE(header, 0) != 0x6d736100 /* \0asm */
                || ReadUInt32LE(header, 4) != 1 /* wasm version */)
            {
                failureReason = "not a WebAssembly module (missing '\\0asm' magic or unexpected version)";
                return false;
            }

            while (true)
            {
                int sectionCode = fs.ReadByte();
                if (sectionCode < 0)
                {
                    failureReason = "reached end of file without finding a data section";
                    return false;
                }
                if (!TryReadULEB128(fs, out uint sectionSize))
                {
                    failureReason = "malformed section size (truncated ULEB128)";
                    return false;
                }

                if (sectionCode == 11 /* data section */)
                {
                    if (!TryReadULEB128(fs, out uint segmentCount) || segmentCount < 1)
                    {
                        failureReason = "data section has no segments";
                        return false;
                    }
                    if (fs.ReadByte() != 1 /* passive segment */)
                    {
                        failureReason = "data segment 0 is not a passive segment";
                        return false;
                    }
                    if (!TryReadULEB128(fs, out uint dataLength) || dataLength < 4)
                    {
                        failureReason = "data segment 0 is too small to hold a payload size";
                        return false;
                    }

                    int want = dataLength >= 8 ? 8 : 4;
                    byte[] sizes = new byte[8];
                    if (!TryFill(fs, sizes, want))
                    {
                        failureReason = "data segment 0 was truncated before the sizes could be read";
                        return false;
                    }

                    payloadSize = (int)ReadUInt32LE(sizes, 0);
                    tableSize = want == 8 ? (int)ReadUInt32LE(sizes, 4) : 0;
                    return true;
                }

                fs.Seek(sectionSize, SeekOrigin.Current);
            }
        }
        catch (Exception ex)
        {
            failureReason = ex.Message;
            return false;
        }
    }

    private static bool TryFill(Stream stream, byte[] buffer, int count)
    {
        int read = 0;
        while (read < count)
        {
            int r = stream.Read(buffer, read, count - read);
            if (r == 0)
                return false;
            read += r;
        }
        return true;
    }

    private static uint ReadUInt32LE(byte[] bytes, int offset)
        => (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));

    private static bool TryReadULEB128(Stream stream, out uint value)
    {
        value = 0;
        int shift = 0;
        while (true)
        {
            if (shift >= 35)
                return false;
            int b = stream.ReadByte();
            if (b < 0)
                return false;
            value |= (uint)(b & 0x7f) << shift;
            if ((b & 0x80) == 0)
                return true;
            shift += 7;
        }
    }
}
