// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelixCache;

internal sealed record HelixQueue
{
    public required string QueueId { get; init; }
    public string? Architecture { get; init; }
    public string? OperatingSystemGroup { get; init; }
    public bool IsAvailable { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Metadata { get; init; } = [];
}

internal sealed record HelixJob
{
    public required string Name { get; init; }
    public required string QueueId { get; init; }
    public string? QueueAlias { get; init; }
}

internal sealed record HelixWorkItem
{
    public required string Job { get; init; }
    public required string Name { get; init; }
    public required string State { get; init; }
    public int? ExitCode { get; init; }
    public string? MachineName { get; init; }
    public string? ConsoleOutputUri { get; init; }
    public HelixFailure? FailureReason { get; init; }
    public List<HelixFailure>? Errors { get; init; }
}

internal sealed class HelixFailure
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Details { get; init; } = [];
}

internal sealed class HelixFile
{
    public required string Name { get; init; }
    public required string Link { get; init; }
}

[JsonSourceGenerationOptions(AllowDuplicateProperties = false, RespectNullableAnnotations = true)]
[JsonSerializable(typeof(List<HelixQueue>))]
[JsonSerializable(typeof(HelixJob))]
[JsonSerializable(typeof(HelixWorkItem))]
[JsonSerializable(typeof(List<HelixFile>))]
internal sealed partial class HelixJsonContext : JsonSerializerContext;
