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
    private readonly Dictionary<string, Snapshot> _snapshots = new(StringComparer.Ordinal);

    internal Cache(string directory) => _root = Path.GetFullPath(directory);

    internal LookupResult Lookup(Submission submission, string baseDirectory, LinuxEnvironment environment)
    {
        JsonFile.ValidateVersion(submission.SchemaVersion);
        JsonFile.Require(submission.WorkItems.Count > 0, nameof(submission.WorkItems));
        LookupResult result = new();
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (WorkItem item in submission.WorkItems)
        {
            JsonFile.Require(!string.IsNullOrWhiteSpace(item.Name) && names.Add(item.Name), nameof(item.Name));
            JsonFile.Require(!string.IsNullOrWhiteSpace(item.Command), nameof(item.Command));
            JsonFile.Require(item.TimeoutSeconds > 0, nameof(item.TimeoutSeconds));
            JsonFile.Require(item.Queues.Count > 0 && item.Queues.Distinct(StringComparer.Ordinal).Count() == item.Queues.Count, nameof(item.Queues));
            JsonFile.Require(item.PreCommands.All(c => c is not null) && item.PostCommands.All(c => c is not null) &&
                item.Environment.All(p => !string.IsNullOrEmpty(p.Key) && p.Value is not null), nameof(item.Environment));
            Snapshot payload = Capture(item.Payload, baseDirectory);
            List<Snapshot> correlation = item.CorrelationPayloads.Select(p => Capture(p, baseDirectory)).ToList();

            foreach (string target in item.Queues)
            {
                JsonFile.Require(!string.IsNullOrWhiteSpace(target), nameof(item.Queues));
                ExecutionInputs inputs = new()
                {
                    Name = item.Name,
                    TargetQueue = target,
                    EnvironmentFingerprint = environment.Resolve(target),
                    Payload = Identity(item.Payload, payload),
                    CorrelationPayloads = item.CorrelationPayloads.Select((p, i) => Identity(p, correlation[i])).ToList(),
                    Command = item.Command,
                    PreCommands = item.PreCommands,
                    PostCommands = item.PostCommands,
                    Environment = item.Environment,
                    TimeoutSeconds = item.TimeoutSeconds,
                    ExpectedExitCode = item.ExpectedExitCode,
                };
                ExecutionRecord execution = new()
                {
                    SchemaVersion = ProtocolVersion,
                    Key = Key(inputs),
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
                    Status = hit ? "hit" : "miss",
                    Execution = execution,
                    ResultsPath = hit ? Path.Combine(entry, "results") : null,
                });
            }
        }
        return result;
    }

    internal void Store(ExecutionRecord execution, string results)
    {
        ValidateRecord(execution);
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
        TestSummary summary = Results.Validate(stagedResults, execution);
        StoredEntry entry = new()
        {
            SchemaVersion = ProtocolVersion,
            Execution = execution,
            CreatedUtc = DateTimeOffset.UtcNow,
            ResultsDigest = Snapshot.Hash(stagedResults),
            Summary = summary,
        };
        JsonFile.Write(Path.Combine(temporary.Path, "entry.json"), entry, CacheJsonContext.Default.StoredEntry);
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

    private Snapshot Capture(Payload payload, string baseDirectory)
    {
        JsonFile.Require(!string.IsNullOrWhiteSpace(payload.Path), nameof(payload.Path));
        JsonFile.RelativePath(payload.Destination, allowEmpty: true);
        string path = Path.GetFullPath(payload.Path, baseDirectory);
        if (!_snapshots.TryGetValue(path, out Snapshot? snapshot))
        {
            Snapshot.EnsureOutside(path, _root);
            snapshot = Snapshot.Capture(path, _root);
            _snapshots.Add(path, snapshot);
        }
        return snapshot;
    }

    private static PayloadIdentity Identity(Payload payload, Snapshot snapshot) => new()
    {
        Digest = snapshot.Digest,
        Destination = payload.Destination,
        Kind = snapshot.Kind,
    };

    private void ValidateEntry(string directory, string key)
    {
        StoredEntry entry = JsonFile.Read(Path.Combine(directory, "entry.json"), CacheJsonContext.Default.StoredEntry);
        JsonFile.ValidateVersion(entry.SchemaVersion);
        ValidateRecord(entry.Execution);
        string results = Path.Combine(directory, "results");
        if (entry.Execution.Key != key || Snapshot.Hash(results) != entry.ResultsDigest)
        {
            throw new InvalidDataException(SR.Get("Corrupt", directory));
        }
        TestSummary summary = Results.Validate(results, entry.Execution);
        if (summary.Total != entry.Summary.Total || summary.Passed != entry.Summary.Passed || summary.Skipped != entry.Summary.Skipped)
        {
            throw new InvalidDataException(SR.Get("Corrupt", directory));
        }
    }

    private static void ValidateRecord(ExecutionRecord execution)
    {
        JsonFile.ValidateVersion(execution.SchemaVersion);
        if (!IsDigest(execution.Key) || Key(execution.Inputs) != execution.Key ||
            execution.CorrelationPayloadPaths.Count != execution.Inputs.CorrelationPayloads.Count)
        {
            throw new InvalidDataException(SR.Get("Corrupt", execution.Key));
        }
    }

    private static void VerifySnapshot(string path, string digest)
    {
        if (Snapshot.Hash(path) != digest)
        {
            throw new InvalidDataException(SR.Get("Corrupt", path));
        }
    }

    private string EntryPath(string key) => Path.Combine(_root, "entries", "v1", key[..2], key);

    private static string Key(ExecutionInputs inputs) =>
        JsonFile.Digest(JsonSerializer.SerializeToElement(inputs, CacheJsonContext.Default.ExecutionInputs));

    internal static bool IsDigest(string value) =>
        value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
