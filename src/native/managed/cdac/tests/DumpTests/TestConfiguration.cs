// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Describes which dump to load for a parameterized dump test.
/// Implements <see cref="IXunitSerializable"/> so xUnit can serialize it
/// across test method invocations and display it in test explorer.
/// </summary>
public sealed class TestConfiguration : IXunitSerializable
{
    /// <summary>
    /// The runtime version identifier (e.g., "local", "net10.0").
    /// Maps to a subdirectory under the dump root.
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// The R2R mode for the dump (e.g., "r2r", "jit").
    /// Determines which subdirectory under the dump type to load from.
    /// "r2r" means the debuggee ran with ReadyToRun enabled;
    /// "jit" means it ran with DOTNET_ReadyToRun=0.
    /// </summary>
    public string R2RMode { get; set; } = "r2r";

    /// <summary>
    /// The platform that produced the dump (e.g., "windows_x64", "linux_arm64").
    /// Null for local runs where the host and dump source are the same.
    /// </summary>
    public string? DumpSource { get; set; }

    public TestConfiguration() { }

    public TestConfiguration(string runtimeVersion, string r2rMode, string? dumpSource = null)
    {
        RuntimeVersion = runtimeVersion;
        R2RMode = r2rMode;
        DumpSource = dumpSource;
    }

    public override string ToString() =>
        DumpSource is not null ? $"{RuntimeVersion}/{R2RMode} ({DumpSource})" : $"{RuntimeVersion}/{R2RMode}";

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(RuntimeVersion), RuntimeVersion);
        info.AddValue(nameof(R2RMode), R2RMode);
        info.AddValue(nameof(DumpSource), DumpSource, typeof(string));
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        RuntimeVersion = info.GetValue<string>(nameof(RuntimeVersion));
        R2RMode = info.GetValue<string>(nameof(R2RMode));
        DumpSource = info.GetValue<string>(nameof(DumpSource));
    }
}
