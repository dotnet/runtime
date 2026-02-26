// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Metadata about the environment in which a set of dumps was generated.
/// Stored as <c>dump-info.json</c> at the root of each version directory
/// (e.g., <c>artifacts/dumps/cdac/local/dump-info.json</c>).
/// </summary>
public sealed class DumpInfo
{
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("arch")]
    public string Arch { get; set; } = string.Empty;

    /// <summary>
    /// Attempts to load <c>dump-info.json</c> from the given version directory.
    /// Returns <c>null</c> if the file does not exist or cannot be parsed.
    /// </summary>
    public static DumpInfo? TryLoad(string versionDirectory)
    {
        string path = Path.Combine(versionDirectory, "dump-info.json");
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, DumpInfoContext.Default.DumpInfo);
    }

    /// <summary>
    /// Saves this <see cref="DumpInfo"/> as <c>dump-info.json</c> in the given directory.
    /// </summary>
    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "dump-info.json");
        string json = JsonSerializer.Serialize(this, DumpInfoContext.Default.DumpInfo);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Creates a <see cref="DumpInfo"/> describing the current machine.
    /// </summary>
    public static DumpInfo ForCurrentMachine()
    {
        string os = OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "osx"
            : OperatingSystem.IsFreeBSD() ? "freebsd"
            : "unknown";

        string arch = RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.X86 => "x86",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            System.Runtime.InteropServices.Architecture.Arm => "arm",
            _ => "unknown",
        };

        return new DumpInfo { Os = os, Arch = arch };
    }
}

[JsonSerializable(typeof(DumpInfo))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class DumpInfoContext : JsonSerializerContext
{
}
