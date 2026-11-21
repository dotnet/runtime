// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class StageWasmSharedMemory : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; } = [];

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public bool AllCandidates { get; set; }

    [Output]
    public ITaskItem[] StagedCandidates { get; set; } = [];

    [Output]
    public string[] FileWrites { get; set; } = [];

    public override bool Execute()
    {
        var candidates = new List<ITaskItem>();
        var writes = new List<string>();
        foreach (ITaskItem candidate in Candidates)
        {
            if (!AllCandidates && Path.GetFileName(candidate.ItemSpec) != "dotnet.native.wasm")
            {
                candidates.Add(candidate);
                continue;
            }

            try
            {
                byte[] shared = WasmMemory.MakeShared(File.ReadAllBytes(candidate.ItemSpec));
                string output = Path.Combine(OutputPath, Path.GetFileName(candidate.ItemSpec));
                if (string.Equals(Path.GetFullPath(output), Path.GetFullPath(candidate.ItemSpec), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Shared assets must be staged separately from their source.");
                Directory.CreateDirectory(OutputPath);
                if (!File.Exists(output) || !File.ReadAllBytes(output).AsSpan().SequenceEqual(shared))
                    File.WriteAllBytes(output, shared);

                var staged = new TaskItem(output, candidate.CloneCustomMetadata());
                staged.RemoveMetadata("Fingerprint");
                staged.RemoveMetadata("Integrity");
                staged.SetMetadata("ContentRoot", Path.GetFullPath(OutputPath) + Path.DirectorySeparatorChar);
                staged.SetMetadata("OriginalItemSpec", output);
                staged.SetMetadata("WasmNativeBuildOutput", "true");
                candidates.Add(staged);
                writes.Add(output);
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                Log.LogError($"Cannot stage shared WebAssembly asset '{candidate.ItemSpec}': {error.Message}");
            }
        }
        StagedCandidates = candidates.ToArray();
        FileWrites = writes.ToArray();

        return !Log.HasLoggedErrors;
    }
}
