// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HelixCache;

internal static class ShadowValidation
{
    internal const string WorkItem = "System.Memory.Data.Tests";
    internal const string Command = "bash ./RunTests.sh --runtime-path \"$HELIX_CORRELATION_PAYLOAD\"";
    internal const string PostCommand = "cp testResults.xml \"$HELIX_WORKITEM_UPLOAD_ROOT/testResults.xml\"";

    internal static LookupEntry Prepare(Cache cache, string output, string payload, string runtime, string queue, string helixSdk,
        LinuxEnvironment environment)
    {
        payload = Path.GetFullPath(payload);
        runtime = Path.GetFullPath(runtime);
        foreach (string archive in new[] { payload, runtime })
        {
            if (!File.Exists(archive) || !archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The shadow submitter requires an existing ZIP archive: {archive}.");
            }
        }
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) || File.Exists(output))
        {
            throw new IOException($"The shadow output directory already exists: {output}.");
        }
        Submission submission = new()
        {
            SchemaVersion = Cache.ProtocolVersion,
            WorkItems = [new WorkItem
            {
                Name = WorkItem,
                Queues = [queue],
                Payload = new Payload { Path = payload },
                CorrelationPayloads = [new Payload { Path = runtime }],
                Command = Command,
                PostCommands = [PostCommand],
                CommandGeneratorDigest = Snapshot.HashFile(helixSdk),
                TimeoutSeconds = 900,
                ExpectedExitCode = 0,
            }],
        };
        LookupResult lookup = cache.Lookup(submission, output, environment);
        LookupEntry entry = lookup.Entries.Single();

        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "submission.json"), JsonSerializer.Serialize(submission, CacheJsonContext.Default.Submission));
        File.WriteAllText(Path.Combine(output, "lookup.json"), JsonSerializer.Serialize(lookup, CacheJsonContext.Default.LookupResult));
        File.WriteAllText(Path.Combine(output, "execution.json"), JsonSerializer.Serialize(entry.Execution, CacheJsonContext.Default.ExecutionRecord));
        WriteSubmissionProperties(output, entry.Execution);
        return entry;
    }

    internal static async Task<ShadowSummary> CollectAsync(Cache cache, string output, TimeSpan wait, HelixClient helix,
        CancellationToken cancellationToken = default)
    {
        output = Path.GetFullPath(output);
        LookupResult lookup = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(output, "lookup.json")),
            CacheJsonContext.Default.LookupResult) ?? throw new InvalidDataException("Missing shadow lookup result.");
        Cache.ValidateVersion(lookup.SchemaVersion);
        if (lookup.Entries.Count != 1 || lookup.Entries[0] is not LookupEntry entry ||
            entry.Status is not (LookupStatus.Hit or LookupStatus.Miss))
        {
            throw new InvalidDataException("Expected exactly one shadow lookup entry with hit or miss status.");
        }
        ExecutionRecord execution = entry.Execution;
        execution.Validate();
        TestRun? cached = entry.Status == LookupStatus.Hit ? cache.ReadResults(execution) : null;
        if (!Guid.TryParse(File.ReadAllText(Path.Combine(output, "job-id.txt")).Trim(), out Guid job))
        {
            throw new InvalidDataException("Expected a Helix job GUID in job-id.txt.");
        }
        string jobId = job.ToString();
        ShadowSummary summary = new() { Key = execution.Key, Lookup = entry.Status, JobId = jobId };
        string summaryPath = Path.Combine(output, "summary.json");
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, CacheJsonContext.Default.ShadowSummary));

        HelixJob jobDetails = await helix.GetJobAsync(jobId, cancellationToken);
        string submittedQueue = string.IsNullOrEmpty(jobDetails.QueueAlias) ? jobDetails.QueueId : jobDetails.QueueAlias;
        if (jobDetails.Name != jobId ||
            !string.Equals(submittedQueue, execution.Inputs.EnvironmentFingerprint.QueueId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Helix returned a different job or target queue.");
        }
        HelixWorkItem item = await helix.WaitForCompletionAsync(jobId, execution.Inputs.Name, wait, cancellationToken);
        File.WriteAllText(Path.Combine(output, "work-item.json"), JsonSerializer.Serialize(item, HelixJsonContext.Default.HelixWorkItem));
        if (item.State is not ("Finished" or "Passed") || item.FailureReason is not null ||
            item.Errors is { Count: > 0 } || item.ExitCode is null || item.ExitCode != execution.Inputs.ExpectedExitCode)
        {
            throw new InvalidDataException($"Helix work item did not complete successfully; see {Path.Combine(output, "work-item.json")}.");
        }

        string results = Path.Combine(output, "results");
        await helix.DownloadResultsAsync(item, results, cancellationToken);
        ResultReceipt receipt = new()
        {
            SchemaVersion = Cache.ProtocolVersion,
            Completed = true,
            InfrastructureSucceeded = true,
            ExitCode = item.ExitCode.Value,
            JobId = jobId,
            WorkItemName = item.Name,
            TargetQueue = execution.Inputs.TargetQueue,
            ResultFiles = ["testResults.xml"],
        };
        ExecutionProvenance provenance = new()
        {
            JobId = jobId,
            QueueAlias = jobDetails.QueueAlias,
            ResolvedQueueId = jobDetails.QueueId,
            MachineName = item.MachineName,
            State = item.State,
        };
        File.WriteAllText(Path.Combine(results, "result.json"), JsonSerializer.Serialize(receipt, CacheJsonContext.Default.ResultReceipt));
        File.WriteAllText(Path.Combine(results, "provenance.json"), JsonSerializer.Serialize(provenance, CacheJsonContext.Default.ExecutionProvenance));

        TestRun fresh = Results.Read(results, execution);
        summary = summary with
        {
            Tests = fresh.Summary.Total,
            CachedJobId = cached?.Receipt.JobId,
            Comparison = cached is null ? OutcomeComparison.NotApplicable :
                cached.HasSameOutcomes(fresh) ? OutcomeComparison.Match : OutcomeComparison.Mismatch,
        };
        if (summary.Comparison == OutcomeComparison.Mismatch)
        {
            File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, CacheJsonContext.Default.ShadowSummary));
            throw new InvalidDataException($"Cached and fresh test outcomes differ; see {summaryPath}.");
        }
        cache.Store(execution, results);
        summary = summary with { Stored = true };
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, CacheJsonContext.Default.ShadowSummary));
        return summary;
    }

    private static void WriteSubmissionProperties(string output, ExecutionRecord execution)
    {
        XElement properties = new("PropertyGroup");
        foreach ((string name, string value) in new Dictionary<string, string>
        {
            ["ShadowName"] = execution.Inputs.Name,
            ["ShadowQueue"] = execution.Inputs.TargetQueue,
            ["ShadowPayload"] = execution.PayloadPath,
            ["ShadowRuntime"] = execution.CorrelationPayloadPaths[0],
            ["ShadowCommand"] = execution.Inputs.Command,
            ["ShadowPostCommand"] = execution.Inputs.PostCommands[0],
            ["ShadowCommandGeneratorDigest"] = execution.Inputs.CommandGeneratorDigest!,
            ["ShadowTimeout"] = TimeSpan.FromSeconds(execution.Inputs.TimeoutSeconds).ToString("c", CultureInfo.InvariantCulture),
        })
        {
            string escaped = value;
            foreach (char character in "%$@();'*?")
            {
                escaped = escaped.Replace(character.ToString(), $"%{(int)character:X2}", StringComparison.Ordinal);
            }
            properties.Add(new XElement(name, escaped));
        }
        new XDocument(new XElement("Project", properties)).Save(Path.Combine(output, "submission.props"));
    }
}
