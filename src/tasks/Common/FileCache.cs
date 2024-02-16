// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

internal sealed class FileCache
{
    private CompilerCache? _newCache;
    private CompilerCache? _oldCache;
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    public bool Enabled { get; }
    public TaskLoggingHelper Log { get; }

    public FileCache(string? cacheFilePath, TaskLoggingHelper log)
    {
        Log = log;
        if (string.IsNullOrEmpty(cacheFilePath))
        {
            Log.LogMessage(MessageImportance.Low, $"Disabling cache, because CacheFilePath is not set");
            return;
        }

        Enabled = true;
        if (File.Exists(cacheFilePath))
        {
            _oldCache = (CompilerCache?)JsonSerializer.Deserialize(File.ReadAllText(cacheFilePath),
                                                                    typeof(CompilerCache),
                                                                    s_jsonOptions);
        }

        _oldCache ??= new();
        _newCache = new(_oldCache.FileHashes);
    }

    public bool UpdateAndCheckHasFileChanged(string filePath, string newHash)
    {
        if (!Enabled)
            throw new InvalidOperationException("Cache is not enabled. Make sure the cache file path is set");

        _newCache!.FileHashes[filePath] = newHash;
        return !_oldCache!.FileHashes.TryGetValue(filePath, out string? oldHash) || oldHash != newHash;
    }

    public bool ShouldCopy(ProxyFile proxyFile, [NotNullWhen(true)] out string? cause)
    {
        if (!Enabled)
            throw new InvalidOperationException("Cache is not enabled. Make sure the cache file path is set");

        cause = null;

        string newHash = Utils.ComputeHash(proxyFile.TempFile);
        _newCache!.FileHashes[proxyFile.TargetFile] = newHash;

        if (!File.Exists(proxyFile.TargetFile))
        {
            cause = $"the output file didn't exist";
            return true;
        }

        string? oldHash;
        if (!_oldCache!.FileHashes.TryGetValue(proxyFile.TargetFile, out oldHash))
            oldHash = Utils.ComputeHash(proxyFile.TargetFile);

        if (oldHash != newHash)
        {
            cause = $"hash for the file changed";
            return true;
        }

        return false;
    }

    public bool Save(string? cacheFilePath)
    {
        if (!Enabled || string.IsNullOrEmpty(cacheFilePath))
            return false;

        var json = JsonSerializer.Serialize (_newCache, s_jsonOptions);
        File.WriteAllText(cacheFilePath!, json);
        return true;
    }

    public ProxyFile NewFile(string targetFile) => new ProxyFile(targetFile, this);
}
