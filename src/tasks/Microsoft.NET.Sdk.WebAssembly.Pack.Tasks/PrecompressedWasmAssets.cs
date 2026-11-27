// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

internal static class PrecompressedWasmAssets
{
    internal const string ManifestPath = "data/PrecompressedAssets.xml";
    internal const string NativePath = "runtimes/browser-wasm/native/";

    internal static string ResolvePath(string root, string relative)
    {
        string directory = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (Path.IsPathRooted(relative) || !path.StartsWith(directory, StringComparison.Ordinal))
            throw new InvalidDataException($"Invalid precompressed runtime asset path '{relative}'.");
        return path;
    }

    internal static string ContentStamp(ITaskItem asset) => string.Join("|",
        new[] { "Kind", "SourcePath", "WebcilVersion", "Mvid", "HasIL", "Length", "Integrity", "GzipIntegrity" }
            .Select(asset.GetMetadata));

    internal static ITaskItem[] Read(string directory)
    {
        string path = Path.Combine(directory, ManifestPath);
        XElement root = XElement.Load(path);
        if (root.Name != "PrecompressedAssets" || (string?)root.Attribute("Version") != "1")
            throw new InvalidDataException($"Unsupported precompressed runtime asset manifest '{path}'.");

        var sources = new HashSet<string>(StringComparer.Ordinal);
        var runtimePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ITaskItem>();
        foreach (XElement element in root.Elements("Asset"))
        {
            string gzipPath = (string?)element.Attribute("GzipPath") ?? "";
            var item = new TaskItem(ResolvePath(directory, gzipPath));
            foreach (XAttribute attribute in element.Attributes())
                item.SetMetadata(attribute.Name.LocalName, attribute.Value);
            item.SetMetadata("NuGetPackageId", (string?)root.Attribute("PackageId") ?? "");
            item.SetMetadata("NuGetPackageVersion", (string?)root.Attribute("PackageVersion") ?? "");
            string source = item.GetMetadata("SourcePath");
            ResolvePath(directory, source);
            if (!sources.Add(source) || item.GetMetadata("Kind") is not ("Native" or "ReadyToRun" or "Webcil"))
                throw new InvalidDataException($"Invalid or duplicate precompressed runtime asset '{source}' in '{path}'.");
            if (item.GetMetadata("Kind") != "Webcil")
            {
                string runtimePath = ResolvePath(directory, item.GetMetadata("RuntimePath"));
                if (!runtimePaths.Add(runtimePath))
                    throw new InvalidDataException($"Duplicate precompressed runtime asset path '{item.GetMetadata("RuntimePath")}' in '{path}'.");
                if (!item.GetMetadata("RuntimePath").StartsWith(NativePath, StringComparison.Ordinal))
                    throw new InvalidDataException($"Invalid precompressed native runtime path '{item.GetMetadata("RuntimePath")}' in '{path}'.");
            }
            result.Add(item);
        }
        return result.ToArray();
    }

    internal static string Hash(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(bytes));
    }

    internal static void Describe(ITaskItem item, byte[] bytes, string prefix = "")
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(bytes);
        item.SetMetadata(prefix + "Integrity", Convert.ToBase64String(hash));
        item.SetMetadata(prefix + "Fingerprint", FileHasher.ToBase36(hash));
        item.SetMetadata(prefix + "Length", bytes.LongLength.ToString(CultureInfo.InvariantCulture));
    }

    internal static void ApplyContentMetadata(ITaskItem item, ITaskItem representation, string prefix = "")
    {
        item.SetMetadata("Integrity", representation.GetMetadata(prefix + "Integrity"));
        item.SetMetadata("Fingerprint", representation.GetMetadata(prefix + "Fingerprint"));
    }

    internal static (Guid Mvid, bool HasIL) ReadAssembly(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        return (reader.GetGuid(reader.GetModuleDefinition().Mvid),
            reader.MethodDefinitions.Any(handle => reader.GetMethodDefinition(handle).RelativeVirtualAddress > 0));
    }

    internal static bool HasEncoded(ITaskItem item)
    {
        // Trust restored package contents and reuse the manifest hashes.
        var file = new FileInfo(item.ItemSpec);
        return file.Exists && file.Length.ToString(CultureInfo.InvariantCulture) == item.GetMetadata("GzipLength");
    }

    internal static string[] Materialize(IReadOnlyList<(ITaskItem Asset, string Output)> assets, string outputDirectory)
    {
        string cachePath = Path.Combine(outputDirectory, "precompressed.assets.cache");
        var previous = File.Exists(cachePath)
            ? XElement.Load(cachePath).Elements("Asset").ToDictionary(e => (string)e.Attribute("Path")!, e => (string)e.Attribute("Stamp")!, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var pending = new List<(ITaskItem Asset, string Output)>();
        foreach ((ITaskItem asset, string output) in assets)
        {
            string stamp = Stamp(asset, output);
            if (!File.Exists(output) || !previous.TryGetValue(output, out string? old) || old != stamp)
                pending.Add((asset, output));
        }

        foreach (string directory in pending.Select(a => Path.GetDirectoryName(a.Output)!).Append(outputDirectory).Distinct(StringComparer.Ordinal))
            Directory.CreateDirectory(directory);

        Parallel.ForEach(pending, entry =>
        {
            string temporary = entry.Output + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream input = File.OpenRead(entry.Asset.ItemSpec))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                using (FileStream output = File.Create(temporary))
                    gzip.CopyTo(output);
                if (new FileInfo(temporary).Length.ToString(CultureInfo.InvariantCulture) != entry.Asset.GetMetadata("Length"))
                    throw new InvalidDataException($"Invalid decoded runtime asset '{entry.Asset.ItemSpec}'.");
                Utils.MoveIfDifferent(temporary, entry.Output);
            }
            finally
            {
                File.Delete(temporary);
            }
        });

        foreach ((ITaskItem asset, string output) in assets)
            previous[output] = Stamp(asset, output);
        var current = new XElement("Assets", previous
            .Where(entry => File.Exists(entry.Key))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new XElement("Asset", new XAttribute("Path", entry.Key), new XAttribute("Stamp", entry.Value))));
        WriteIfDifferent(cachePath, current);
        return assets.Select(a => a.Output).Append(cachePath).ToArray();

        static string Stamp(ITaskItem asset, string output)
        {
            var file = new FileInfo(output);
            return ContentStamp(asset) + "|" +
                (file.Exists ? file.Length + "|" + file.LastWriteTimeUtc.Ticks : "");
        }
    }

    internal static void WriteIfDifferent(string path, XElement document)
    {
        string content = document.ToString() + Environment.NewLine;
        if (!File.Exists(path) || File.ReadAllText(path) != content)
            File.WriteAllText(path, content);
    }
}
