// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelixCache;

internal sealed class Submission
{
    public required int SchemaVersion { get; init; }
    public required List<WorkItem> WorkItems { get; init; }
}

internal sealed class WorkItem
{
    public required string Name { get; init; }
    public required List<string> Queues { get; init; }
    public required Payload Payload { get; init; }
    public List<Payload> CorrelationPayloads { get; init; } = [];
    public required string Command { get; init; }
    public List<string> PreCommands { get; init; } = [];
    public List<string> PostCommands { get; init; } = [];
    public Dictionary<string, string> Environment { get; init; } = [];
    public required int TimeoutSeconds { get; init; }
    public required int ExpectedExitCode { get; init; }
}

internal sealed class Payload
{
    public required string Path { get; init; }
    public string Destination { get; init; } = "";
}

internal sealed class PayloadIdentity
{
    public required string Digest { get; init; }
    public required string Destination { get; init; }
    public required string Kind { get; init; }
}

internal sealed class ExecutionInputs
{
    public required string Name { get; init; }
    public required string TargetQueue { get; init; }
    public required JsonElement EnvironmentFingerprint { get; init; }
    public required PayloadIdentity Payload { get; init; }
    public required List<PayloadIdentity> CorrelationPayloads { get; init; }
    public required string Command { get; init; }
    public required List<string> PreCommands { get; init; }
    public required List<string> PostCommands { get; init; }
    public required Dictionary<string, string> Environment { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required int ExpectedExitCode { get; init; }
}

internal sealed class ExecutionRecord
{
    public required int SchemaVersion { get; init; }
    public required string Key { get; init; }
    public required ExecutionInputs Inputs { get; init; }
    public required string PayloadPath { get; init; }
    public required List<string> CorrelationPayloadPaths { get; init; }
}

internal sealed class LookupResult
{
    public int SchemaVersion { get; init; } = Cache.ProtocolVersion;
    public List<LookupEntry> Entries { get; init; } = [];
}

internal sealed class LookupEntry
{
    public required string Status { get; init; }
    public required ExecutionRecord Execution { get; init; }
    public string? ResultsPath { get; init; }
}

internal sealed class ResultReceipt
{
    public required int SchemaVersion { get; init; }
    public required bool Completed { get; init; }
    public required bool InfrastructureSucceeded { get; init; }
    public required int ExitCode { get; init; }
    public required string JobId { get; init; }
    public required string WorkItemName { get; init; }
    public required string TargetQueue { get; init; }
    public required List<string> ResultFiles { get; init; }
}

internal sealed class TestSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Skipped { get; set; }
}

internal sealed class StoredEntry
{
    public required int SchemaVersion { get; init; }
    public required ExecutionRecord Execution { get; init; }
    public required DateTimeOffset CreatedUtc { get; init; }
    public required string ResultsDigest { get; init; }
    public required TestSummary Summary { get; init; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Submission))]
[JsonSerializable(typeof(ExecutionRecord))]
[JsonSerializable(typeof(ExecutionInputs))]
[JsonSerializable(typeof(LookupResult))]
[JsonSerializable(typeof(ResultReceipt))]
[JsonSerializable(typeof(StoredEntry))]
internal sealed partial class CacheJsonContext : JsonSerializerContext;
