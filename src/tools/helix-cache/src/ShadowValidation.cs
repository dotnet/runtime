// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal const string Command = "bash ./RunTests.sh --runtime-path \"$HELIX_CORRELATION_PAYLOAD\"";
    internal const string PostCommand = "cp testResults.xml \"$HELIX_WORKITEM_UPLOAD_ROOT/testResults.xml\"";

    internal static LookupResult Prepare(Cache cache, string output, List<string> payloads, string runtime, string queue, string helixSdk,
        LinuxEnvironment environment)
    {
        payloads = payloads.Select(Path.GetFullPath).ToList();
        runtime = Path.GetFullPath(runtime);
        foreach (string archive in payloads.Append(runtime))
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
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (string payload in payloads)
        {
            string name = Path.GetFileNameWithoutExtension(payload);
            ValidateName(name);
            if (!names.Add(name))
            {
                throw new InvalidDataException($"Duplicate shadow work item name: {name}.");
            }
        }
        if (payloads.Count == 0)
        {
            throw new InvalidDataException("A shadow submission requires at least one test payload.");
        }
        string sdkDigest = Snapshot.HashFile(helixSdk);
        Submission submission = new()
        {
            SchemaVersion = Cache.ProtocolVersion,
            WorkItems = payloads.Select(payload => new WorkItem
            {
                Name = Path.GetFileNameWithoutExtension(payload),
                Queues = [queue],
                Payload = new Payload { Path = payload },
                CorrelationPayloads = [new Payload { Path = runtime }],
                Command = Command,
                PostCommands = [PostCommand],
                CommandGeneratorDigest = sdkDigest,
                TimeoutSeconds = 900,
                ExpectedExitCode = 0,
            }).ToList(),
        };
        LookupResult lookup = cache.Lookup(submission, output, environment);

        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "submission.json"), JsonSerializer.Serialize(submission, CacheJsonContext.Default.Submission));
        File.WriteAllText(Path.Combine(output, "lookup.json"), JsonSerializer.Serialize(lookup, CacheJsonContext.Default.LookupResult));
        foreach (LookupEntry entry in lookup.Entries)
        {
            string directory = Directory.CreateDirectory(Path.Combine(output, "work-items", entry.Execution.Inputs.Name)).FullName;
            File.WriteAllText(Path.Combine(directory, "execution.json"),
                JsonSerializer.Serialize(entry.Execution, CacheJsonContext.Default.ExecutionRecord));
        }
        WriteSubmissionProperties(output, lookup);
        return lookup;
    }

    internal static async Task<ShadowSummary> CollectAsync(Cache cache, string output, TimeSpan wait, HelixClient helix,
        CancellationToken cancellationToken = default)
    {
        output = Path.GetFullPath(output);
        LookupResult lookup = JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(output, "lookup.json")),
            CacheJsonContext.Default.LookupResult) ?? throw new InvalidDataException("Missing shadow lookup result.");
        Cache.ValidateVersion(lookup.SchemaVersion);
        if (lookup.Entries.Count == 0)
        {
            throw new InvalidDataException("Expected at least one shadow lookup entry.");
        }
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (LookupEntry entry in lookup.Entries)
        {
            if (entry is null || entry.Status is not (LookupStatus.Hit or LookupStatus.Miss))
            {
                throw new InvalidDataException("Expected shadow lookup entries with hit or miss status.");
            }
            entry.Execution.Validate();
            ValidateName(entry.Execution.Inputs.Name);
            if (!names.Add(entry.Execution.Inputs.Name))
            {
                throw new InvalidDataException($"Duplicate shadow work item name: {entry.Execution.Inputs.Name}.");
            }
        }
        TestRun?[] cached = lookup.Entries.Select(entry =>
            entry.Status == LookupStatus.Hit ? cache.ReadResults(entry.Execution) : null).ToArray();
        if (!Guid.TryParse(File.ReadAllText(Path.Combine(output, "job-id.txt")).Trim(), out Guid job))
        {
            throw new InvalidDataException("Expected a Helix job GUID in job-id.txt.");
        }
        string jobId = job.ToString();
        ShadowSummary summary = new()
        {
            JobId = jobId,
            WorkItems = lookup.Entries.Select(entry => new ShadowWorkItemSummary
            {
                Name = entry.Execution.Inputs.Name,
                Key = entry.Execution.Key,
                Lookup = entry.Status,
            }).ToList(),
        };
        string summaryPath = Path.Combine(output, "summary.json");
        WriteSummary();

        HelixJob jobDetails = await helix.GetJobAsync(jobId, cancellationToken);
        string submittedQueue = string.IsNullOrEmpty(jobDetails.QueueAlias) ? jobDetails.QueueId : jobDetails.QueueAlias;
        if (jobDetails.Name != jobId ||
            lookup.Entries.Any(entry => !string.Equals(submittedQueue,
                entry.Execution.Inputs.EnvironmentFingerprint.QueueId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("Helix returned a different job or target queue.");
        }
        Stopwatch elapsed = Stopwatch.StartNew();
        for (int i = 0; i < lookup.Entries.Count; i++)
        {
            ExecutionRecord execution = lookup.Entries[i].Execution;
            TimeSpan remaining = wait - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException("Timed out waiting for the shadow job.");
            }
            HelixWorkItem item = await helix.WaitForCompletionAsync(jobId, execution.Inputs.Name, remaining, cancellationToken);
            string directory = Directory.CreateDirectory(Path.Combine(output, "work-items", execution.Inputs.Name)).FullName;
            string workItemPath = Path.Combine(directory, "work-item.json");
            File.WriteAllText(workItemPath, JsonSerializer.Serialize(item, HelixJsonContext.Default.HelixWorkItem));
            if (item.State is not ("Finished" or "Passed") || item.FailureReason is not null ||
                item.Errors is { Count: > 0 } || item.ExitCode is null || item.ExitCode != execution.Inputs.ExpectedExitCode)
            {
                throw new InvalidDataException($"Helix work item did not complete successfully; see {workItemPath}.");
            }

            string results = Path.Combine(directory, "results");
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
            TestRun? previous = cached[i];
            summary.WorkItems[i] = summary.WorkItems[i] with
            {
                Tests = fresh.Summary.Total,
                CachedJobId = previous?.Receipt.JobId,
                Comparison = previous is null ? OutcomeComparison.NotApplicable :
                    previous.HasSameOutcomes(fresh) ? OutcomeComparison.Match : OutcomeComparison.Mismatch,
            };
            WriteSummary();
            if (summary.WorkItems[i].Comparison == OutcomeComparison.Mismatch)
            {
                throw new InvalidDataException($"Cached and fresh test outcomes differ for {execution.Inputs.Name}; see {summaryPath}.");
            }
        }
        for (int i = 0; i < lookup.Entries.Count; i++)
        {
            ExecutionRecord execution = lookup.Entries[i].Execution;
            cache.Store(execution, Path.Combine(output, "work-items", execution.Inputs.Name, "results"));
            summary.WorkItems[i] = summary.WorkItems[i] with { Stored = true };
            WriteSummary();
        }
        return summary;

        void WriteSummary() => File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, CacheJsonContext.Default.ShadowSummary));
    }

    private static void ValidateName(string name)
    {
        if (name.Length == 0 || name is "." or ".." || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-')))
        {
            throw new InvalidDataException($"Shadow work item names must use ASCII letters, digits, '.', '_' or '-' and cannot be empty, '.' or '..': {name}.");
        }
    }

    private static void WriteSubmissionProperties(string output, LookupResult lookup)
    {
        ExecutionRecord first = lookup.Entries[0].Execution;
        XElement properties = new("PropertyGroup",
            Property("ShadowQueue", first.Inputs.TargetQueue),
            Property("ShadowRuntime", first.CorrelationPayloadPaths[0]),
            Property("ShadowCommandGeneratorDigest", first.Inputs.CommandGeneratorDigest!));
        XElement items = new("ItemGroup");
        foreach (LookupEntry entry in lookup.Entries)
        {
            ExecutionRecord execution = entry.Execution;
            items.Add(new XElement("ShadowWorkItem", new XAttribute("Include", execution.Inputs.Name),
                Property("PayloadArchive", execution.PayloadPath),
                Property("Command", execution.Inputs.Command),
                Property("PostCommands", execution.Inputs.PostCommands[0]),
                Property("Timeout", TimeSpan.FromSeconds(execution.Inputs.TimeoutSeconds).ToString("c", CultureInfo.InvariantCulture))));
        }
        new XDocument(new XElement("Project", properties, items)).Save(Path.Combine(output, "submission.props"));

        static XElement Property(string name, string value)
        {
            string escaped = value;
            foreach (char character in "%$@();'*?")
            {
                escaped = escaped.Replace(character.ToString(), $"%{(int)character:X2}", StringComparison.Ordinal);
            }
            return new XElement(name, escaped);
        }
    }
}
