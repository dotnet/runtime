// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class PrecompressWasmRuntimeAssets : Task
{
    [Required] public ITaskItem[] Files { get; set; } = [];
    [Required] public string OutputPath { get; set; } = "";
    [Required] public string PackageId { get; set; } = "";
    [Required] public string PackageVersion { get; set; } = "";
    public int WebcilVersion { get; set; } = 1;
    [Output] public ITaskItem[] CompressedFiles { get; set; } = [];
    [Output] public ITaskItem[] ReplacedFiles { get; set; } = [];
    [Output] public string[] FileWrites { get; set; } = [];

    public override bool Execute()
    {
        var files = Files.ToDictionary(file => (file.GetMetadata("TargetPath").TrimEnd('/', '\\') + "/" + Path.GetFileName(file.ItemSpec)).Replace('\\', '/'), StringComparer.Ordinal);
        var compressed = new List<ITaskItem>();
        var replaced = new List<ITaskItem>();
        var manifest = new XElement("PrecompressedAssets", new XAttribute("Version", 1),
            new XAttribute("PackageId", PackageId), new XAttribute("PackageVersion", PackageVersion));
        var writes = new List<string>();
        string temporaryWebcil = Path.Combine(OutputPath, "converted.wasm");
        Directory.CreateDirectory(OutputPath);

        foreach (KeyValuePair<string, ITaskItem> file in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            string path = file.Key;
            string name = Path.GetFileName(path);
            bool r2r = path.StartsWith(PrecompressedWasmAssets.NativePath + "r2r/", StringComparison.Ordinal) && path.EndsWith(".wasm", StringComparison.Ordinal);
            bool native = path == PrecompressedWasmAssets.NativePath + "dotnet.native.wasm" ||
                path.StartsWith(PrecompressedWasmAssets.NativePath, StringComparison.Ordinal) &&
                path.Substring(PrecompressedWasmAssets.NativePath.Length).IndexOf('/') < 0 &&
                (name.StartsWith("dotnet.", StringComparison.Ordinal) && name != "dotnet.js" &&
                    (name.EndsWith(".js", StringComparison.Ordinal) || name.EndsWith(".mjs", StringComparison.Ordinal) || name.EndsWith(".map", StringComparison.Ordinal)) ||
                 name.StartsWith("icudt", StringComparison.Ordinal) && name.EndsWith(".dat", StringComparison.Ordinal));
            bool managed = path.StartsWith("runtimes/browser-wasm/lib/", StringComparison.Ordinal) &&
                path.EndsWith(".dll", StringComparison.Ordinal) && !path.EndsWith(".resources.dll", StringComparison.Ordinal);
            if (!r2r && !native && !managed)
                continue;

            var asset = new TaskItem(file.Value);
            asset.SetMetadata("Kind", r2r ? "ReadyToRun" : native ? "Native" : "Webcil");
            asset.SetMetadata("RuntimePath", managed ? "" : path);
            asset.SetMetadata("SourcePath", path);
            byte[] bytes;
            if (managed || r2r)
            {
                string sourcePath = r2r
                    ? files.Keys.SingleOrDefault(p => p.StartsWith("runtimes/browser-wasm/lib/", StringComparison.Ordinal) && Path.GetFileName(p) == Path.ChangeExtension(name, ".dll"))
                        ?? throw new InvalidDataException($"Missing source assembly for prebuilt ReadyToRun image '{path}'.")
                    : path;
                (Guid mvid, bool hasIL) = PrecompressedWasmAssets.ReadAssembly(files[sourcePath].ItemSpec);
                if (managed && hasIL)
                    continue;
                asset.SetMetadata("SourcePath", sourcePath);
                asset.SetMetadata("Mvid", mvid.ToString());
                asset.SetMetadata("HasIL", hasIL.ToString());
                asset.SetMetadata("WebcilVersion", WebcilVersion.ToString(CultureInfo.InvariantCulture));
                if (managed)
                {
                    WebcilConverter.FromPortableExecutable(file.Value.ItemSpec, temporaryWebcil, WebcilVersion).ConvertToWebcil();
                    bytes = File.ReadAllBytes(temporaryWebcil);
                    File.Delete(temporaryWebcil);
                }
                else
                {
                    bytes = File.ReadAllBytes(file.Value.ItemSpec);
                }
                using var stream = new MemoryStream(bytes, writable: false);
                if (!WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int table, out string? reason))
                    throw new InvalidDataException($"Invalid WebCIL image '{path}': {reason}");
                stream.Position = 0;
                using var webcil = new WebcilReader(stream);
                MetadataReader reader = webcil.GetMetadataReader();
                if ((table > 0) != r2r || reader.GetGuid(reader.GetModuleDefinition().Mvid) != mvid)
                    throw new InvalidDataException($"WebCIL image '{path}' does not match the module version or ReadyToRun flavor of '{sourcePath}'.");
            }
            else
            {
                bytes = File.ReadAllBytes(file.Value.ItemSpec);
            }

            string gzipRelative = "precompressed/" + (managed ? Path.ChangeExtension(path, ".wasm") : path) + ".gz";
            string gzipPath = PrecompressedWasmAssets.ResolvePath(OutputPath, gzipRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(gzipPath)!);
            using var encoded = new MemoryStream();
            using (var gzip = new GZipStream(encoded, CompressionLevel.Optimal, leaveOpen: true))
                gzip.Write(bytes, 0, bytes.Length);
            byte[] gzipBytes = encoded.ToArray();
            File.WriteAllBytes(gzipPath, gzipBytes);
            PrecompressedWasmAssets.Describe(asset, bytes);
            PrecompressedWasmAssets.Describe(asset, gzipBytes, "Gzip");
            asset.SetMetadata("GzipPath", gzipRelative);
            var element = new XElement("Asset");
            foreach (string key in new[] { "Kind", "RuntimePath", "SourcePath", "GzipPath", "Mvid", "HasIL", "WebcilVersion", "Length", "Integrity", "Fingerprint", "GzipLength", "GzipIntegrity", "GzipFingerprint" })
                element.SetAttributeValue(key, asset.GetMetadata(key));
            manifest.Add(element);
            var packaged = new TaskItem(gzipPath);
            packaged.SetMetadata("TargetPath", Path.GetDirectoryName(gzipRelative)!.Replace('\\', '/'));
            compressed.Add(packaged);
            writes.Add(gzipPath);
            if (!managed)
                replaced.Add(file.Value);
        }

        string manifestPath = Path.Combine(OutputPath, PrecompressedWasmAssets.ManifestPath);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        PrecompressedWasmAssets.WriteIfDifferent(manifestPath, manifest);
        var manifestFile = new TaskItem(manifestPath);
        manifestFile.SetMetadata("TargetPath", "data");
        compressed.Add(manifestFile);
        writes.Add(manifestPath);
        CompressedFiles = compressed.ToArray();
        ReplacedFiles = replaced.ToArray();
        FileWrites = writes.ToArray();
        return true;
    }
}
