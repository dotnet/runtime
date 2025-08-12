// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public static class ArtifactWriter
{
    public static bool PersistFileIfChanged<T>(this Task task, T manifest, string artifactPath, JsonTypeInfo<T> serializer)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(manifest, serializer);
        return PersistFileIfChanged(task, data, artifactPath);
    }

    public static bool PersistFileIfChanged(this Task task, byte[] data, string artifactPath)
    {
        var newHash = ComputeHash(data);
        var fileExists = File.Exists(artifactPath);
        var existingManifestHash = fileExists ? ComputeHash(artifactPath) : null;

        if (!fileExists)
        {
            task.Log.LogMessage(MessageImportance.Low, $"Creating artifact because artifact file '{artifactPath}' does not exist.");
            File.WriteAllBytes(artifactPath, data);
            return true;
        }
        else if (!string.Equals(newHash, existingManifestHash, StringComparison.Ordinal))
        {
            task.Log.LogMessage(MessageImportance.Low, $"Updating artifact because artifact version '{newHash}' is different from existing artifact hash '{existingManifestHash}'.");
            File.WriteAllBytes(artifactPath, data);
            return true;
        }
        else
        {
            task.Log.LogMessage(MessageImportance.Low, $"Skipping artifact updated because artifact version '{existingManifestHash}' has not changed.");
            return false;
        }
    }

    private static string ComputeHash(string artifactPath) => ComputeHash(File.ReadAllBytes(artifactPath));

    private static string ComputeHash(byte[] data)
    {
#if NET6_0_OR_GREATER
        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
#else
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(data));
#endif
    }
}
