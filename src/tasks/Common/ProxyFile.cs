// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;

#nullable enable

internal sealed class ProxyFile
{
    public string TargetFile { get; }
    public string TempFile   { get; }
    private FileCache _cache;

    public ProxyFile(string targetFile, FileCache cache)
    {
        _cache = cache;
        this.TargetFile = targetFile;
        this.TempFile = _cache.Enabled ? targetFile + ".tmp" : targetFile;
    }

    public bool CopyOutputFileIfChanged()
    {
        if (!_cache.Enabled)
            return true;

        if (!File.Exists(TempFile))
            throw new LogAsErrorException($"Could not find the temporary file {TempFile} for target file {TargetFile}. Look for any errors/warnings generated earlier in the build.");

        try
        {
            if (!_cache.ShouldCopy(this, out string? cause))
            {
                _cache.Log.LogMessage(MessageImportance.Low, $"Skipping copying over {TargetFile} as the contents are unchanged");
                return false;
            }

            if (File.Exists(TargetFile))
                File.Delete(TargetFile);

            File.Copy(TempFile, TargetFile);

            _cache.Log.LogMessage(MessageImportance.Low, $"Copying {TempFile} to {TargetFile} because {cause}");
            return true;
        }
        finally
        {
            _cache.Log.LogMessage(MessageImportance.Low, $"Deleting temp file {TempFile}");
            File.Delete(TempFile);
        }
    }
}
