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
    /// the R2R .wasm; optional metadata TableSize/PayloadSize are propagated onto the produced
    /// webcil item as WasmR2RTableSize/WasmR2RPayloadSize.
    /// </summary>
    public ITaskItem[] R2RWebcilCandidates { get; set; }

    [Output]
    public ITaskItem[] WebcilCandidates { get; set; }

    /// <summary>
    /// Payload/table sizes for each produced webcil, keyed by file name (e.g. "System.Console.wasm").
    /// Lets the boot config carry the sizes so the runtime loader doesn't buffer/parse the wasm or
    /// call getWebcilSize. PayloadSize is set for every webcil; TableSize is set (non-zero) for R2R.
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
        string candidatePath = candidate.GetMetadata("AssetTraitName") == "Culture"
            ? Path.Combine(OutputPath, candidate.GetMetadata("AssetTraitValue"))
            : OutputPath;

        string finalWebcil = Path.Combine(candidatePath, webcilFileName);

        if (_r2rByName != null && _r2rByName.TryGetValue(Path.GetFileNameWithoutExtension(dllFilePath), out var r2rReplacement))
        {
            return StageR2RWebcil(tmpDir, candidate, r2rReplacement, webcilFileName, candidatePath, finalWebcil);
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

        RecordWebcilSize(finalWebcil);
        return webcilItem;
    }

    // Stage a prebuilt R2R webcil-in-wasm image in place of converting the .dll. The produced item
    // mirrors ConvertDll's output (same path, RelativePath, OriginalItemSpec) so downstream
    // fingerprinting/integrity is computed from the R2R bytes.
    private TaskItem StageR2RWebcil(string tmpDir, ITaskItem candidate, ITaskItem r2r, string webcilFileName, string candidatePath, string finalWebcil)
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

        RecordWebcilSize(finalWebcil);
        return webcilItem;
    }

    // Parses the produced webcil's data segment 0 and records payloadSize/tableSize keyed by file
    // name, so GenerateWasmBootJson can emit them into the boot config without re-parsing.
    private void RecordWebcilSize(string webcilPath)
    {
        if (!TryReadWebcilSizes(webcilPath, out int payloadSize, out int tableSize))
        {
            Log.LogMessage(MessageImportance.Low, $"Could not read Webcil sizes from {webcilPath}.");
            return;
        }

        var item = new TaskItem(Path.GetFileName(webcilPath));
        item.SetMetadata("PayloadSize", payloadSize.ToString(CultureInfo.InvariantCulture));
        item.SetMetadata("TableSize", tableSize.ToString(CultureInfo.InvariantCulture));
        _webcilSizes.Add(item);
    }

    // Reads payloadSize and tableSize from data segment 0 of a Webcil-in-wasm image without
    // instantiating it. tableSize > 0 indicates a ReadyToRun image. Only a small prefix is read
    // (the sizes live at the start of the data section). See docs/design/mono/webcil.md.
    private static bool TryReadWebcilSizes(string path, out int payloadSize, out int tableSize)
    {
        payloadSize = 0;
        tableSize = 0;
        try
        {
            byte[] bytes;
            using (var fs = File.OpenRead(path))
            {
                int len = (int)Math.Min(fs.Length, 4096);
                bytes = new byte[len];
                int read = 0;
                while (read < len)
                {
                    int r = fs.Read(bytes, read, len - read);
                    if (r == 0)
                        break;
                    read += r;
                }
            }

            if (bytes.Length < 8
                || BitConverter.ToUInt32(bytes, 0) != 0x6d736100 /* \0asm */
                || BitConverter.ToUInt32(bytes, 4) != 1 /* wasm version */)
            {
                return false;
            }

            int offset = 8;
            while (offset < bytes.Length)
            {
                byte sectionCode = bytes[offset++];
                if (!TryReadULEB128(bytes, ref offset, out uint sectionSize))
                    return false;

                if (sectionCode == 11 /* data section */)
                {
                    int p = offset;
                    if (!TryReadULEB128(bytes, ref p, out uint segmentCount) || segmentCount < 1)
                        return false;
                    if (p >= bytes.Length || bytes[p++] != 1 /* passive */)
                        return false;
                    if (!TryReadULEB128(bytes, ref p, out uint dataLength) || dataLength < 4)
                        return false;
                    if (p + 4 > bytes.Length)
                        return false;

                    payloadSize = (int)BitConverter.ToUInt32(bytes, p);
                    tableSize = dataLength >= 8 && p + 8 <= bytes.Length ? (int)BitConverter.ToUInt32(bytes, p + 4) : 0;
                    return true;
                }

                offset += (int)sectionSize;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadULEB128(byte[] bytes, ref int offset, out uint value)
    {
        value = 0;
        int shift = 0;
        while (true)
        {
            if (offset >= bytes.Length || shift >= 35)
                return false;
            byte b = bytes[offset++];
            value |= (uint)(b & 0x7f) << shift;
            if ((b & 0x80) == 0)
                return true;
            shift += 7;
        }
    }
}
