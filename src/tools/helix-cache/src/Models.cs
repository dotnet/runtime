// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelixCache;

internal enum PayloadKind
{
    [JsonStringEnumMemberName("directory")]
    Directory,
    [JsonStringEnumMemberName("archive")]
    Archive,
}

internal enum LookupStatus
{
    [JsonStringEnumMemberName("miss")]
    Miss,
    [JsonStringEnumMemberName("hit")]
    Hit,
}

internal enum OutcomeComparison
{
    [JsonStringEnumMemberName("not-applicable")]
    NotApplicable,
    [JsonStringEnumMemberName("match")]
    Match,
    [JsonStringEnumMemberName("mismatch")]
    Mismatch,
}

internal sealed record Submission
{
    public required int SchemaVersion { get; init; }
    public required List<WorkItem> WorkItems { get; init; }

    internal void Validate()
    {
        Cache.ValidateVersion(SchemaVersion);
        HashSet<string> names = new(StringComparer.Ordinal);
        if (WorkItems.Count == 0)
        {
            throw new InvalidDataException("A submission must contain at least one work item.");
        }
        foreach (WorkItem item in WorkItems)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Name) || !names.Add(item.Name))
            {
                throw new InvalidDataException("Work items must be nonnull and have unique, nonempty names.");
            }
            item.Validate();
        }
    }
}

internal sealed record WorkItem
{
    public required string Name { get; init; }
    public required List<string> Queues { get; init; }
    public required Payload Payload { get; init; }
    public List<Payload> CorrelationPayloads { get; init; } = [];
    public required string Command { get; init; }
    public List<string> PreCommands { get; init; } = [];
    public List<string> PostCommands { get; init; } = [];
    public string? CommandGeneratorDigest { get; init; }
    public Dictionary<string, string> Environment { get; init; } = [];
    public required int TimeoutSeconds { get; init; }
    public required int ExpectedExitCode { get; init; }

    internal void Validate()
    {
        ValidateExecution(Command, TimeoutSeconds, PreCommands, PostCommands, Environment, CommandGeneratorDigest);
        if (Queues.Count == 0 || Queues.Any(string.IsNullOrWhiteSpace) ||
            Queues.Distinct(StringComparer.Ordinal).Count() != Queues.Count)
        {
            throw new InvalidDataException($"Work item '{Name}' requires unique, nonempty target queues.");
        }
        Payload.Validate();
        foreach (Payload payload in CorrelationPayloads)
        {
            if (payload is null)
            {
                throw new InvalidDataException($"Work item '{Name}' has a null correlation payload.");
            }
            payload.Validate();
        }
    }

    internal static void ValidateExecution(string command, int timeoutSeconds, List<string> preCommands,
        List<string> postCommands, Dictionary<string, string> environment, string? commandGeneratorDigest)
    {
        if (string.IsNullOrWhiteSpace(command) || timeoutSeconds <= 0 ||
            preCommands.Any(c => c is null) || postCommands.Any(c => c is null) ||
            environment.Any(p => string.IsNullOrEmpty(p.Key) || p.Value is null))
        {
            throw new InvalidDataException("Execution requires a nonempty command, positive timeout, nonnull commands and environment values.");
        }
        if (commandGeneratorDigest is not null && !Cache.IsDigest(commandGeneratorDigest))
        {
            throw new InvalidDataException("A command generator digest must be a lowercase SHA-256 digest.");
        }
    }
}

internal sealed record Payload
{
    public required string Path { get; init; }
    public string Destination { get; init; } = "";

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            throw new InvalidDataException("A payload requires a nonempty path.");
        }
        Snapshot.ValidateRelativePath(Destination, allowEmpty: true);
    }
}

internal sealed record PayloadIdentity
{
    public required string Digest { get; init; }
    public required string Destination { get; init; }
    public required PayloadKind Kind { get; init; }

    internal void Validate()
    {
        if (!Cache.IsDigest(Digest) || Kind is not (PayloadKind.Directory or PayloadKind.Archive))
        {
            throw new InvalidDataException("A payload identity requires a SHA-256 digest and a directory or archive kind.");
        }
        Snapshot.ValidateRelativePath(Destination, allowEmpty: true);
    }
}

internal sealed class EnvironmentFingerprint
{
    public required string QueueId { get; init; }
    public required string Architecture { get; init; }
    public required string OperatingSystemGroup { get; init; }
    public string? Container { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Provisioning { get; init; } = [];
}

internal sealed record ExecutionInputs
{
    public required string Name { get; init; }
    public required string TargetQueue { get; init; }
    public required EnvironmentFingerprint EnvironmentFingerprint { get; init; }
    public required PayloadIdentity Payload { get; init; }
    public required List<PayloadIdentity> CorrelationPayloads { get; init; }
    public required string Command { get; init; }
    public required List<string> PreCommands { get; init; }
    public required List<string> PostCommands { get; init; }
    public string? CommandGeneratorDigest { get; init; }
    public required Dictionary<string, string> Environment { get; init; }
    public required int TimeoutSeconds { get; init; }
    public required int ExpectedExitCode { get; init; }
}

internal sealed record ExecutionRecord
{
    public required int SchemaVersion { get; init; }
    public required string Key { get; init; }
    public required ExecutionInputs Inputs { get; init; }
    public required string PayloadPath { get; init; }
    public required List<string> CorrelationPayloadPaths { get; init; }

    internal void Validate()
    {
        Cache.ValidateVersion(SchemaVersion);
        WorkItem.ValidateExecution(Inputs.Command, Inputs.TimeoutSeconds, Inputs.PreCommands, Inputs.PostCommands, Inputs.Environment,
            Inputs.CommandGeneratorDigest);
        Inputs.Payload.Validate();
        foreach (PayloadIdentity payload in Inputs.CorrelationPayloads)
        {
            if (payload is null)
            {
                throw new InvalidDataException("Execution correlation payload identities must not contain null.");
            }
            payload.Validate();
        }
        if (!Cache.IsDigest(Key) || ExecutionKey.Create(Inputs) != Key ||
            string.IsNullOrWhiteSpace(Inputs.Name) || string.IsNullOrWhiteSpace(Inputs.TargetQueue) ||
            string.IsNullOrWhiteSpace(PayloadPath) || CorrelationPayloadPaths.Any(string.IsNullOrWhiteSpace) ||
            CorrelationPayloadPaths.Count != Inputs.CorrelationPayloads.Count)
        {
            throw new InvalidDataException($"Execution record failed validation: {Key}.");
        }
    }
}

internal sealed class LookupResult
{
    public int SchemaVersion { get; init; } = Cache.ProtocolVersion;
    public List<LookupEntry> Entries { get; init; } = [];
}

internal sealed class LookupEntry
{
    public required LookupStatus Status { get; init; }
    public required ExecutionRecord Execution { get; init; }
    public string? ResultsPath { get; init; }
}

internal sealed record ResultReceipt
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

internal sealed record ShadowSummary
{
    public required string Key { get; init; }
    public required LookupStatus Lookup { get; init; }
    public required string JobId { get; init; }
    public string Mode { get; init; } = "shadow";
    public int? Tests { get; init; }
    public OutcomeComparison? Comparison { get; init; }
    public string? CachedJobId { get; init; }
    public bool Stored { get; init; }
}

internal sealed class ExecutionProvenance
{
    public string Kind { get; init; } = "helix-execution";
    public required string JobId { get; init; }
    public string? QueueAlias { get; init; }
    public required string ResolvedQueueId { get; init; }
    public string? MachineName { get; init; }
    public required string State { get; init; }
}

[JsonSourceGenerationOptions(
    AllowDuplicateProperties = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(Submission))]
[JsonSerializable(typeof(ExecutionRecord))]
[JsonSerializable(typeof(ExecutionInputs))]
[JsonSerializable(typeof(LookupResult))]
[JsonSerializable(typeof(ResultReceipt))]
[JsonSerializable(typeof(StoredEntry))]
[JsonSerializable(typeof(ShadowSummary))]
[JsonSerializable(typeof(ExecutionProvenance))]
internal sealed partial class CacheJsonContext : JsonSerializerContext;
