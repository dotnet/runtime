// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HelixCache;

internal sealed class Cache
{
    internal const int ProtocolVersion = 1;
    private readonly string _root;

    internal Cache(string directory) => _root = Path.GetFullPath(directory);

    internal LookupResult Lookup(Submission submission, string baseDirectory, LinuxEnvironment environment)
    {
        submission.Validate();
        Dictionary<string, EnvironmentFingerprint> fingerprints = submission.WorkItems.SelectMany(item => item.Queues)
            .Distinct(StringComparer.Ordinal).ToDictionary(target => target, environment.Resolve, StringComparer.Ordinal);
        Dictionary<string, Snapshot> snapshots = new(StringComparer.Ordinal);
        LookupResult result = new();
        foreach (WorkItem item in submission.WorkItems)
        {
            Snapshot payload = Capture(item.Payload, baseDirectory, snapshots);
            List<Snapshot> correlation = item.CorrelationPayloads.Select(p => Capture(p, baseDirectory, snapshots)).ToList();

            foreach (string target in item.Queues)
            {
                ExecutionInputs inputs = new()
                {
                    Name = item.Name,
                    TargetQueue = target,
                    EnvironmentFingerprint = fingerprints[target],
                    Payload = Identity(item.Payload, payload),
                    CorrelationPayloads = item.CorrelationPayloads.Select((p, i) => Identity(p, correlation[i])).ToList(),
                    Command = item.Command,
                    PreCommands = [.. item.PreCommands],
                    PostCommands = [.. item.PostCommands],
                    CommandGeneratorDigest = item.CommandGeneratorDigest,
                    Environment = new(item.Environment, StringComparer.Ordinal),
                    TimeoutSeconds = item.TimeoutSeconds,
                    ExpectedExitCode = item.ExpectedExitCode,
                };
                ExecutionRecord execution = new()
                {
                    SchemaVersion = ProtocolVersion,
                    Key = ExecutionKey.Create(inputs),
                    Inputs = inputs,
                    PayloadPath = payload.Path,
                    CorrelationPayloadPaths = correlation.Select(p => p.Path).ToList(),
                };
                string entry = EntryPath(execution.Key);
                bool hit = Directory.Exists(entry);
                if (hit)
                {
                    ValidateEntry(entry, execution.Key);
                }
                result.Entries.Add(new LookupEntry
                {
                    Status = hit ? LookupStatus.Hit : LookupStatus.Miss,
                    Execution = execution,
                    ResultsPath = hit ? Path.Combine(entry, "results") : null,
                });
            }
        }
        return result;
    }

    internal void Store(ExecutionRecord execution, string results)
    {
        execution.Validate();
        VerifySnapshot(execution.PayloadPath, execution.Inputs.Payload.Digest);
        for (int i = 0; i < execution.CorrelationPayloadPaths.Count; i++)
        {
            VerifySnapshot(execution.CorrelationPayloadPaths[i], execution.Inputs.CorrelationPayloads[i].Digest);
        }
        results = Path.GetFullPath(results);
        Snapshot.EnsureOutside(results, _root);
        using TemporaryDirectory temporary = new(Path.Combine(_root, "tmp"));
        Snapshot.EnsureOutside(results, temporary.Path);
        string stagedResults = Path.Combine(temporary.Path, "results");
        Snapshot.Copy(results, stagedResults);
        TestSummary summary = Results.Read(stagedResults, execution).Summary;
        StoredEntry entry = new()
        {
            SchemaVersion = ProtocolVersion,
            Execution = execution,
            CreatedUtc = DateTimeOffset.UtcNow,
            ResultsDigest = Snapshot.Hash(stagedResults),
            Summary = summary,
        };
        using (FileStream file = File.Create(Path.Combine(temporary.Path, "entry.json")))
        {
            JsonSerializer.Serialize(file, entry, CacheJsonContext.Default.StoredEntry);
            file.Flush(flushToDisk: true);
        }
        string final = EntryPath(execution.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(final)!);
        try
        {
            Directory.Move(temporary.Path, final);
        }
        catch (IOException) when (Directory.Exists(final))
        {
            ValidateEntry(final, execution.Key);
        }
    }

    internal TestRun ReadResults(ExecutionRecord execution)
    {
        execution.Validate();
        return ValidateEntry(EntryPath(execution.Key), execution.Key);
    }

    private Snapshot Capture(Payload payload, string baseDirectory, Dictionary<string, Snapshot> snapshots)
    {
        string path = Path.GetFullPath(payload.Path, baseDirectory);
        if (!snapshots.TryGetValue(path, out Snapshot? snapshot))
        {
            Snapshot.EnsureOutside(path, _root);
            snapshot = Snapshot.Capture(path, _root);
            snapshots.Add(path, snapshot);
        }
        return snapshot;
    }

    private static PayloadIdentity Identity(Payload payload, Snapshot snapshot) => new()
    {
        Digest = snapshot.Digest,
        Destination = payload.Destination,
        Kind = snapshot.Kind,
    };

    private static TestRun ValidateEntry(string directory, string key)
    {
        using FileStream file = File.OpenRead(Path.Combine(directory, "entry.json"));
        StoredEntry entry = JsonSerializer.Deserialize(file, CacheJsonContext.Default.StoredEntry)
            ?? throw new InvalidDataException($"Cache entry failed validation: {directory}.");
        ValidateVersion(entry.SchemaVersion);
        entry.Execution.Validate();
        string results = Path.Combine(directory, "results");
        if (entry.Execution.Key != key || Snapshot.Hash(results) != entry.ResultsDigest)
        {
            throw new InvalidDataException($"Cache entry failed validation: {directory}.");
        }
        TestRun run = Results.Read(results, entry.Execution);
        if (run.Summary.Total != entry.Summary.Total || run.Summary.Passed != entry.Summary.Passed || run.Summary.Skipped != entry.Summary.Skipped)
        {
            throw new InvalidDataException($"Cache entry failed validation: {directory}.");
        }
        return run;
    }

    private static void VerifySnapshot(string path, string digest)
    {
        if (Snapshot.Hash(path) != digest)
        {
            throw new InvalidDataException($"Snapshot failed validation: {path}.");
        }
    }

    private string EntryPath(string key) => Path.Combine(_root, "entries", "v1", key[..2], key);

    internal static void ValidateVersion(int version)
    {
        if (version != ProtocolVersion)
        {
            throw new InvalidDataException($"Unsupported schema version {version}; expected {ProtocolVersion}.");
        }
    }

    internal static bool IsDigest(string value) =>
        value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
