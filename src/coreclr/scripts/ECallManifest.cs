// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json.Serialization;

namespace CoreCLR.Tools;

internal sealed class ECallManifest
{
    public required int Version { get; set; }
    public required string? TargetOS { get; set; }
    public required ECallManifestClass?[]? Classes { get; set; }
}

internal sealed class ECallManifestClass
{
    public required string? Name { get; set; }
    public required string? Namespace { get; set; }
    public required ECallManifestEntry?[]? Entries { get; set; }
}

internal sealed class ECallManifestEntry
{
    public required string? Name { get; set; }
    public required string? Signature { get; set; }
    public required string? Symbol { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ECallManifest))]
internal partial class ECallManifestJsonContext : JsonSerializerContext
{
}
