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

    public TestConfiguration() { }

    public TestConfiguration(string runtimeVersion)
    {
        RuntimeVersion = runtimeVersion;
    }

    public override string ToString() => RuntimeVersion;

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(RuntimeVersion), RuntimeVersion);
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        RuntimeVersion = info.GetValue<string>(nameof(RuntimeVersion));
    }
}
