// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

internal sealed class RuntimePackAssets(string location, Func<string, Stream?> open)
{
    private Dictionary<string, string>? _gzipPaths;

    public RuntimePackAssets(string directory)
        : this(directory, path =>
        {
            string fullPath = Path.Combine(directory, path);
            return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        })
    {
    }

    public RuntimePackAssets(string packagePath, ZipArchive package)
        : this(packagePath, path => package.GetEntry(path)?.Open())
    {
    }

    public static string GetNativePath(string name)
        => $"runtimes/{BuildEnvironment.DefaultRuntimeIdentifier}/native/{name.Replace('\\', '/')}";

    public byte[] ReadBytes(string path)
        => TryReadBytes(path) ?? throw MissingAsset(path);

    public byte[]? TryReadBytes(string path)
    {
        using Stream? source = TryOpen(path);
        if (source is null)
            return null;

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return buffer.ToArray();
    }

    public void AssertFileSize(string path, string actualPath, string? label = null, bool same = true)
    {
        using Stream source = TryOpen(path) ?? throw MissingAsset(path);
        long length = source.CanSeek ? source.Length : GetDecodedLength(source);
        TestUtils.AssertFileSize($"{location}/{path}", length, actualPath, label, same);
    }

    private Stream? TryOpen(string path)
    {
        if (open(path) is Stream raw)
            return raw;

        if (!(_gzipPaths ??= ReadGzipPaths()).TryGetValue(path, out string? gzipPath))
            return null;

        Stream encoded = open(gzipPath) ?? throw MissingAsset(gzipPath);
        return new GZipStream(encoded, CompressionMode.Decompress);
    }

    private Dictionary<string, string> ReadGzipPaths()
    {
        using Stream? manifest = open("data/PrecompressedAssets.xml");
        if (manifest is null)
            return new(StringComparer.Ordinal);

        XElement root = XElement.Load(manifest);
        Assert.True(root.Name == "PrecompressedAssets" && (string?)root.Attribute("Version") == "1",
            $"Unexpected precompressed asset manifest in '{location}'.");
        return root.Elements("Asset")
            .Where(asset => (string?)asset.Attribute("Kind") is "Native" or "ReadyToRun")
            .ToDictionary(asset => asset.Attribute("RuntimePath")!.Value, asset => asset.Attribute("GzipPath")!.Value, StringComparer.Ordinal);
    }

    private XunitException MissingAsset(string path)
        => new($"Expected to find '{path}' in runtime pack '{location}'.");

    private static long GetDecodedLength(Stream source)
    {
        byte[] buffer = new byte[8192];
        long length = 0;
        int read;
        while ((read = source.Read(buffer)) != 0)
            length += read;
        return length;
    }
}
