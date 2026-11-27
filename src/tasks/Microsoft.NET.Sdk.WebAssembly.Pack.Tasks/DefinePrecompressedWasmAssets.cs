// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class DefinePrecompressedWasmAssets : Task
{
    public ITaskItem[] Representations { get; set; } = [];
    public ITaskItem[] Assets { get; set; } = [];
    public ITaskItem[] Endpoints { get; set; } = [];
    public ITaskItem[] FrameworkAssets { get; set; } = [];
    public ITaskItem[] MaterializedFrameworkAssets { get; set; } = [];
    [Output] public ITaskItem[] GzipAssets { get; set; } = [];

    public override bool Execute()
    {
        if (FrameworkAssets.Length != MaterializedFrameworkAssets.Length)
        {
            Log.LogError("Cannot map precompressed runtime assets: framework materialization returned a different asset count.");
            return false;
        }
        var materialized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < FrameworkAssets.Length; i++)
        {
            ITaskItem source = FrameworkAssets[i];
            ITaskItem destination = MaterializedFrameworkAssets[i];
            if (source.GetMetadata("RelativePath") != destination.GetMetadata("RelativePath") ||
                source.GetMetadata("Integrity") != destination.GetMetadata("Integrity"))
            {
                Log.LogError($"Cannot map materialized framework asset '{source.ItemSpec}' to '{destination.ItemSpec}'.");
                return false;
            }
            materialized.Add(Path.GetFullPath(source.ItemSpec), Path.GetFullPath(destination.ItemSpec));
        }
        ILookup<string, ITaskItem> assetsByPath = Assets.ToLookup(a => Path.GetFullPath(a.ItemSpec), StringComparer.OrdinalIgnoreCase);
        var result = new List<ITaskItem>();
        foreach (ITaskItem representation in Representations)
        {
            string expected = Path.GetFullPath(representation.GetMetadata("ExpectedAsset"));
            if (materialized.TryGetValue(expected, out string? mapped))
                expected = mapped;
            ITaskItem[] matches = assetsByPath[expected].Where(a =>
                a.GetMetadata("AssetRole") != "Alternative" &&
                a.GetMetadata("Integrity") == representation.GetMetadata("Integrity") &&
                a.GetMetadata("Fingerprint") == representation.GetMetadata("Fingerprint")).ToArray();
            if (matches.Length != 1)
            {
                Log.LogMessage(MessageImportance.Low, $"Not registering precompressed '{representation.ItemSpec}': found {matches.Length} matching assets at '{expected}'.");
                continue;
            }
            ITaskItem original = matches[0];

            string relative = original.GetMetadata("RelativePath");
            bool fingerprintedEndpoint = false;
            if (!relative.Contains("{fingerprint"))
            {
                foreach (ITaskItem endpoint in Endpoints.Where(e => e.GetMetadata("AssetFile") == original.ItemSpec))
                {
                    string properties = endpoint.GetMetadata("EndpointProperties");
                    if (properties.Length == 0)
                        continue;
                    using JsonDocument document = JsonDocument.Parse(properties);
                    if (!document.RootElement.EnumerateArray().Any(p => p.GetProperty("Name").GetString() == "fingerprint" &&
                        p.GetProperty("Value").GetString() == original.GetMetadata("Fingerprint")))
                        continue;
                    relative = endpoint.ItemSpec;
                    fingerprintedEndpoint = true;
                    string basePath = original.GetMetadata("BasePath").Trim('/');
                    if (basePath.Length != 0 && relative.StartsWith(basePath + "/", StringComparison.Ordinal))
                        relative = relative.Substring(basePath.Length + 1);
                    break;
                }
            }

            string fingerprint = original.GetMetadata("Fingerprint");
            relative = relative.Replace("{fingerprint}", "{fingerprint=" + fingerprint + "}");
            if (fingerprintedEndpoint && !relative.Contains("{fingerprint"))
                relative = relative.Replace("." + fingerprint + ".", "#[.{fingerprint=" + fingerprint + "}]!.");
            var gzip = new TaskItem(representation.ItemSpec, original.CloneCustomMetadata());
            gzip.SetMetadata("ContentRoot", Path.GetDirectoryName(representation.ItemSpec) + Path.DirectorySeparatorChar);
            // Materialization strips fingerprint tokens, so keep the encoded identity literal.
            gzip.SetMetadata("RelativePath", relative + "." + representation.GetMetadata("GzipFingerprint") + ".gz");
            gzip.SetMetadata("OriginalItemSpec", representation.ItemSpec);
            gzip.SetMetadata("RelatedAsset", original.ItemSpec);
            gzip.SetMetadata("AssetRole", "Alternative");
            gzip.SetMetadata("AssetTraitName", "Content-Encoding");
            gzip.SetMetadata("AssetTraitValue", "gzip");
            gzip.SetMetadata("SourceType", "Framework");
            gzip.SetMetadata("FileLength", "");
            gzip.SetMetadata("LastWriteTime", "");
            gzip.SetMetadata("CopyToOutputDirectory", "Never");
            gzip.SetMetadata("CopyToPublishDirectory", "Never");
            PrecompressedWasmAssets.ApplyContentMetadata(gzip, representation, "Gzip");
            result.Add(gzip);
        }
        GzipAssets = result.ToArray();
        return true;
    }
}
