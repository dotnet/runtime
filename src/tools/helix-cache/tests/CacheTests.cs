// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace HelixCache.Tests;

public sealed class CacheTests
{
    [Fact]
    public void RoundTripPreservesFrozenInputsAndResults()
    {
        using CacheFixture f = new();
        LookupEntry miss = f.Lookup();
        Assert.Equal(LookupStatus.Miss, miss.Status);
        Assert.NotEqual(f.PayloadDirectory, miss.Execution.PayloadPath);
        JsonObject fingerprint = JsonNode.Parse(File.ReadAllText(Path.Combine(f.Root, "lookup.json")))!
            ["entries"]![0]!["execution"]!["inputs"]!["environmentFingerprint"]!.AsObject();
        Assert.Equal(f.Queues[0].QueueId, (string?)fingerprint["queueId"]);
        Assert.Equal("AMD64", (string?)fingerprint["architecture"]);
        Assert.Equal("linux", (string?)fingerprint["operatingSystemGroup"]);
        Assert.Contains("container", fingerprint.Select(p => p.Key));
        f.CreateResults(miss.Execution);
        f.Store(miss.Execution);
        LookupEntry hit = f.Lookup();
        Assert.Equal(LookupStatus.Hit, hit.Status);
        Assert.Equal(miss.Execution.Key, hit.Execution.Key);
        Assert.Equal(File.ReadAllBytes(Path.Combine(f.Root, "results/tests.xml")),
            File.ReadAllBytes(Path.Combine(hit.ResultsPath!, "tests.xml")));
        File.WriteAllText(Path.Combine(f.PayloadDirectory, "run.sh"), "changed");
        Assert.Equal("#!/bin/sh\nexit 0\n", File.ReadAllText(Path.Combine(miss.Execution.PayloadPath, "run.sh")));
        Assert.Equal(LookupStatus.Miss, f.Lookup().Status);
    }

    [Fact]
    public void RepeatedLookupsOnOneCacheCaptureChangedInputs()
    {
        using CacheFixture f = new();
        Cache cache = new(f.CacheDirectory);
        LinuxEnvironment environment = new(f.Queues);
        string before = Assert.Single(cache.Lookup(f.Submission, f.Root, environment).Entries).Execution.Key;
        File.WriteAllText(Path.Combine(f.PayloadDirectory, "run.sh"), "changed");
        string after = Assert.Single(cache.Lookup(f.Submission, f.Root, environment).Entries).Execution.Key;
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void WholeSubmissionIsValidatedBeforeCapturingInputs()
    {
        using CacheFixture f = new();
        f.Submission.WorkItems.Add(f.Item with { Name = "invalid", TimeoutSeconds = 0 });
        Assert.Throws<InvalidDataException>(() => new Cache(f.CacheDirectory).Lookup(f.Submission, f.Root, new LinuxEnvironment(f.Queues)));
        Assert.False(Directory.Exists(f.CacheDirectory));
    }

    [Fact]
    public void OpaqueProvisioningPreservesNullArrayValues()
    {
        using CacheFixture f = new();
        f.Queues[0].Metadata["PreparedImage"] = JsonSerializer.Deserialize<JsonElement>("""{"FutureField":[null,1]}""");
        LookupEntry miss = f.Lookup();
        f.CreateResults(miss.Execution);
        f.Store(miss.Execution);
        Assert.Equal(LookupStatus.Hit, f.Lookup().Status);
        f.Queues[0].Metadata["PreparedImage"] = JsonSerializer.Deserialize<JsonElement>("""{"FutureField":[null,2]}""");
        Assert.NotEqual(miss.Execution.Key, f.Lookup().Execution.Key);
    }

    [Theory]
    [InlineData("command")]
    [InlineData("preCommands")]
    [InlineData("postCommands")]
    [InlineData("timeout")]
    [InlineData("exitCode")]
    [InlineData("environment")]
    public void ExecutionChangesInvalidateKey(string field)
    {
        using CacheFixture f = new();
        string baseline = f.Lookup().Execution.Key;
        f.Item = field switch
        {
            "command" => f.Item with { Command = "./run.sh other" },
            "preCommands" => f.Item with { PreCommands = ["export STRESS=1"] },
            "postCommands" => f.Item with { PostCommands = ["echo done"] },
            "timeout" => f.Item with { TimeoutSeconds = 61 },
            "exitCode" => f.Item with { ExpectedExitCode = 100 },
            "environment" => f.Item with { Environment = new() { ["FIRST"] = "changed" } },
            _ => throw new InvalidOperationException(),
        };
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
    }

    [Theory]
    [InlineData("GalleryImage", """{"Version":"2"}""")]
    [InlineData("Artifacts", """[{"Id":"prepare","Parameters":{"version":"2"}}]""")]
    [InlineData("PreparedImage", """{"Version":"new","FutureField":{"b":2,"a":1}}""")]
    public void ImageAndProvisioningChangesInvalidateKey(string field, string value)
    {
        using CacheFixture f = new();
        string baseline = f.Lookup().Execution.Key;
        f.Queues[0].Metadata[field] = JsonSerializer.Deserialize<JsonElement>(value);
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
    }

    [Fact]
    public void ObjectOrderingLiveQueueStateAndTimestampsDoNotChangeKey()
    {
        using CacheFixture f = new();
        f.Queues[0].Metadata["PreparedImage"] = JsonSerializer.Deserialize<JsonElement>("""{"b":{"z":2,"a":1},"a":null}""");
        string baseline = f.Lookup().Execution.Key;
        f.Queues[0].Metadata["PreparedImage"] = JsonSerializer.Deserialize<JsonElement>("""{"a":null,"b":{"a":1,"z":2}}""");
        f.Queues[0].Metadata["QueueDepth"] = JsonSerializer.SerializeToElement(300);
        f.Queues[0].Metadata["IsInProtectedMode"] = JsonSerializer.SerializeToElement(true);
        f.Item = f.Item with { Environment = new() { ["FIRST"] = "1", ["SECOND"] = "2" } };
        File.SetLastWriteTimeUtc(Path.Combine(f.PayloadDirectory, "run.sh"), DateTime.UnixEpoch);
        Assert.Equal(baseline, f.Lookup().Execution.Key);
    }

    [Fact]
    public void RelocationDoesNotChangeKeyButModesAndEmptyDirectoriesDo()
    {
        using CacheFixture f = new();
        string baseline = f.Lookup().Execution.Key;
        string relocated = Path.Combine(f.Root, "relocated");
        Directory.Move(f.PayloadDirectory, relocated);
        f.Item = f.Item with { Payload = f.Item.Payload with { Path = relocated } };
        Assert.Equal(baseline, f.Lookup().Execution.Key);
        string script = Path.Combine(relocated, "run.sh");
        UnixFileMode mode = File.GetUnixFileMode(script);
        File.SetUnixFileMode(script, mode & ~UnixFileMode.UserExecute);
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
        File.SetUnixFileMode(script, mode);
        Directory.CreateDirectory(Path.Combine(relocated, "empty"));
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
    }

    [Fact]
    public void QueuesAndPinnedContainersArePartitioned()
    {
        using CacheFixture f = new();
        string target = "(Debian.Amd64)linux.test.open@registry/image:tag@sha256:" + new string('a', 64);
        f.Item.Queues.Add(target);
        LookupEntry first = f.Lookup();
        LookupResult lookup = CacheFixture.Read(Path.Combine(f.Root, "lookup.json"), CacheJsonContext.Default.LookupResult);
        Assert.Equal(2, lookup.Entries.Count);
        Assert.NotEqual(first.Execution.Key, lookup.Entries[1].Execution.Key);
        Assert.Equal(target[(target.IndexOf('@') + 1)..], lookup.Entries[1].Execution.Inputs.EnvironmentFingerprint.Container);
        f.CreateResults(first.Execution);
        f.Store(first.Execution);
        f.Lookup();
        lookup = CacheFixture.Read(Path.Combine(f.Root, "lookup.json"), CacheJsonContext.Default.LookupResult);
        Assert.Equal([LookupStatus.Hit, LookupStatus.Miss], lookup.Entries.Select(e => e.Status));
    }

    [Theory]
    [InlineData("linux.test.open@registry/image:latest")]
    [InlineData("(alias)linux.test.open")]
    public void UnpinnedOrMalformedContainerTargetsAreRejected(string target)
    {
        using CacheFixture f = new();
        f.Item = f.Item with { Queues = [target] };
        f.Lookup(success: false);
    }

    [Fact]
    public void SharedPayloadBytesDestinationAndOverlayOrderAffectKey()
    {
        using CacheFixture f = new();
        string baseline = f.Lookup().Execution.Key;
        File.WriteAllText(f.Archive, "new archive");
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
        File.WriteAllText(f.Archive, "opaque archive bytes");
        f.Item.CorrelationPayloads[0] = f.Item.CorrelationPayloads[0] with { Destination = "other" };
        Assert.NotEqual(baseline, f.Lookup().Execution.Key);
        f.Item.CorrelationPayloads.Add(new Payload { Path = f.Archive, Destination = "second" });
        string before = f.Lookup().Execution.Key;
        f.Item.CorrelationPayloads.Reverse();
        Assert.NotEqual(before, f.Lookup().Execution.Key);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("infrastructure")]
    [InlineData("exitCode")]
    [InlineData("workItem")]
    [InlineData("queue")]
    [InlineData("job")]
    [InlineData("empty")]
    [InlineData("traversal")]
    [InlineData("duplicate")]
    public void UnsuccessfulReceiptsAreNotStored(string field)
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.CreateResults(execution);
        ResultReceipt receipt = CacheFixture.Read(Path.Combine(f.Root, "results/result.json"), CacheJsonContext.Default.ResultReceipt);
        receipt = field switch
        {
            "completed" => receipt with { Completed = false },
            "infrastructure" => receipt with { InfrastructureSucceeded = false },
            "exitCode" => receipt with { ExitCode = 1 },
            "workItem" => receipt with { WorkItemName = "other" },
            "queue" => receipt with { TargetQueue = "other" },
            "job" => receipt with { JobId = "not-a-guid" },
            "empty" => receipt with { ResultFiles = [] },
            "traversal" => receipt with { ResultFiles = ["../outside.xml"] },
            "duplicate" => receipt with { ResultFiles = ["tests.xml", "tests.xml"] },
            _ => throw new InvalidOperationException(),
        };
        f.Write("results/result.json", receipt, CacheJsonContext.Default.ResultReceipt);
        f.Store(execution, success: false);
        Assert.Equal(LookupStatus.Miss, f.Lookup().Status);
    }

    [Theory]
    [InlineData("<assemblies/>")]
    [InlineData("""<assemblies><assembly total="0" passed="0" failed="0" skipped="0"/></assemblies>""")]
    [InlineData("""<assemblies><assembly total="1" passed="1" failed="0" skipped="0"/></assemblies>""")]
    [InlineData("""<assemblies><assembly total="1" passed="0" failed="1" skipped="0"><collection><test result="Fail"/></collection></assembly></assemblies>""")]
    [InlineData("""<assemblies><assembly total="1" passed="1" failed="0" skipped="0"><errors><error/></errors><collection><test result="Pass"/></collection></assembly></assemblies>""")]
    [InlineData("""<!DOCTYPE assemblies [<!ENTITY x SYSTEM "file:///etc/passwd">]><assemblies>&x;</assemblies>""")]
    [InlineData("""<assemblies><assembly name="Suite.dll" total="1" passed="1" failed="0" skipped="0"><collection><test result="Pass"/></collection></assembly></assemblies>""")]
    [InlineData("""<assemblies><assembly name="Suite.dll" total="1" passed="1" failed="0" skipped="0"><test name="misplaced" result="Pass"/></assembly></assemblies>""")]
    [InlineData("""<assemblies><errors><error/></errors><assembly name="Suite.dll" total="1" passed="1" failed="0" skipped="0"><collection><test name="one" result="Pass"/></collection></assembly></assemblies>""")]
    public void ReportsMustBeCompleteNonemptyAndPassing(string report)
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.CreateResults(execution);
        File.WriteAllText(Path.Combine(f.Root, "results/tests.xml"), report);
        f.Store(execution, success: false);
        Assert.Equal(LookupStatus.Miss, f.Lookup().Status);
    }

    [Fact]
    public void CorruptResultsAreAnErrorRatherThanAHitOrMiss()
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.CreateResults(execution);
        f.Store(execution);
        string results = f.Lookup().ResultsPath!;
        File.WriteAllText(Path.Combine(results, "tests.xml"), "corrupted");
        f.Lookup(success: false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ForgedKeysAndModifiedSnapshotsCannotBeStored(bool forgeKey)
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.CreateResults(execution);
        if (forgeKey)
        {
            execution = execution with { Key = new string('0', 64) };
        }
        else
        {
            File.WriteAllText(Path.Combine(execution.PayloadPath, "run.sh"), "changed");
        }
        f.Store(execution, success: false);
    }

    [Fact]
    public void ResultsCannotContainTheirStagingDirectory()
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.Invoke(false, "store", f.Write("execution.json", execution, CacheJsonContext.Default.ExecutionRecord),
            "--results", Path.Combine(f.CacheDirectory, "tmp"), "--cache", f.CacheDirectory);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(f.CacheDirectory, "tmp")));
        Assert.Equal(LookupStatus.Miss, f.Lookup().Status);
    }

    [Fact]
    public async Task DuplicateStoresAreAtomicAndIdempotent()
    {
        using CacheFixture f = new();
        ExecutionRecord execution = f.Lookup().Execution;
        f.CreateResults(execution);
        string record = f.Write("execution.json", execution, CacheJsonContext.Default.ExecutionRecord);
        Task[] operations = Enumerable.Range(0, 2).Select(_ => Task.Run(() => f.Invoke(true,
            "store", record, "--results", Path.Combine(f.Root, "results"), "--cache", f.CacheDirectory))).ToArray();
        await Task.WhenAll(operations);
        Assert.Equal(LookupStatus.Hit, f.Lookup().Status);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(f.CacheDirectory, "tmp")));
    }

    [Theory]
    [InlineData("architecture")]
    [InlineData("os")]
    [InlineData("availability")]
    public void UnsupportedQueuesAreRejected(string field)
    {
        using CacheFixture f = new();
        f.Queues[0] = field switch
        {
            "architecture" => f.Queues[0] with { Architecture = "ARM64" },
            "os" => f.Queues[0] with { OperatingSystemGroup = "windows" },
            "availability" => f.Queues[0] with { IsAvailable = false },
            _ => throw new InvalidOperationException(),
        };
        f.Lookup(success: false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinksAndNestedCachesAreRejected(bool link)
    {
        using CacheFixture f = new();
        if (link)
        {
            File.CreateSymbolicLink(Path.Combine(f.PayloadDirectory, "link"), f.Archive);
        }
        else
        {
            f.CacheDirectory = Path.Combine(f.PayloadDirectory, "cache");
        }
        f.Lookup(success: false);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("empty")]
    [InlineData("nullItem")]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("nullCommand")]
    [InlineData("timeout")]
    [InlineData("nullCorrelation")]
    public void InvalidManifestsAreRejected(string mutation)
    {
        using CacheFixture f = new();
        JsonNode manifest = JsonNode.Parse(JsonSerializer.Serialize(f.Submission, CacheJsonContext.Default.Submission))!;
        JsonNode item = manifest["workItems"]![0]!;
        switch (mutation)
        {
            case "version": manifest["schemaVersion"] = 2; break;
            case "empty": manifest["workItems"] = new JsonArray(); break;
            case "nullItem": manifest["workItems"]![0] = null; break;
            case "unknown": manifest["unrecognized"] = true; break;
            case "missing": item.AsObject().Remove("expectedExitCode"); break;
            case "nullCommand": item["command"] = null; break;
            case "timeout": item["timeoutSeconds"] = 0; break;
            case "nullCorrelation": item["correlationPayloads"]![0] = null; break;
            default: throw new InvalidOperationException();
        }
        string path = Path.Combine(f.Root, "invalid.json");
        File.WriteAllText(path, manifest.ToJsonString());
        f.Invoke(false, "lookup", path, "--cache", f.CacheDirectory, "--output", Path.Combine(f.Root, "lookup.json"),
            "--queue-info", f.Write("queues.json", f.Queues, HelixJsonContext.Default.ListHelixQueue));
    }

    [Theory]
    [InlineData("""{"schemaVersion":1,"schemaVersion":1,"workItems":[]}""")]
    [InlineData("""{"schemaVersion":1,"workItems":[],"workItems":[]}""")]
    public void DuplicateJsonPropertiesAreRejected(string manifest)
    {
        using CacheFixture f = new();
        string path = Path.Combine(f.Root, "duplicate.json");
        File.WriteAllText(path, manifest);
        f.Invoke(false, "lookup", path, "--cache", f.CacheDirectory, "--output", Path.Combine(f.Root, "lookup.json"),
            "--queue-info", f.Write("queues.json", f.Queues, HelixJsonContext.Default.ListHelixQueue));
    }

    [Fact]
    public void CliRejectsMissingAndDuplicateOptions()
    {
        using CacheFixture f = new();
        f.Invoke(false, "store", "missing", "--cache", f.CacheDirectory);
        f.Invoke(false, "lookup", "missing", "--cache", f.CacheDirectory, "--cache", f.CacheDirectory);
        f.Invoke(false, "collect", "--cache", f.CacheDirectory, "--output", f.Root, "--wait-seconds", "0");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("lookup --help")]
    [InlineData("store -h")]
    [InlineData("prepare --help")]
    [InlineData("collect --help")]
    public void HelpDoesNotRequireInputsOrCreateCache(string arguments)
    {
        using CacheFixture f = new();
        Assert.Contains("Usage: helix-cache", f.Invoke(true, arguments.Split(' ')));
        Assert.False(Directory.Exists(f.CacheDirectory));
    }

    [Theory]
    [InlineData("lookup", "Missing manifest")]
    [InlineData("lookup --cache cache --output output", "Missing manifest")]
    [InlineData("lookup missing.json --cache cache --output output --unknown value", "Unknown option '--unknown'")]
    [InlineData("lookup missing.json --cache cache --cache cache", "Duplicate option '--cache'")]
    [InlineData("store missing.json --cache cache", "Missing required option '--results'")]
    [InlineData("collect --cache cache --output output --wait-seconds -1", "--wait-seconds")]
    [InlineData("collect --cache --output output", "Expected an option and value")]
    public void CommandLineErrorsIdentifyTheInvalidArgument(string arguments, string diagnostic)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => CommandLine.Parse(arguments.Split(' ')));
        Assert.Contains(diagnostic, error.Message);
    }

    [Fact]
    public void NativePrepareWritesFrozenSubmissionProperties()
    {
        using CacheFixture f = new();
        string output = Path.Combine(f.Root, "native-shadow");
        f.Invoke(true, "prepare", "--cache", f.CacheDirectory, "--output", output, "--payload", f.Archive,
            "--runtime", f.Archive, "--queue", f.Queues[0].QueueId,
            "--queue-info", f.Write("queues.json", f.Queues, HelixJsonContext.Default.ListHelixQueue));
        LookupEntry entry = CacheFixture.ShadowLookup(output);
        Assert.Equal(LookupStatus.Miss, entry.Status);
        Assert.NotEqual(f.Archive, entry.Execution.PayloadPath);
        Assert.Equal(ShadowValidation.Command, entry.Execution.Inputs.Command);
        Assert.Contains("%24", File.ReadAllText(Path.Combine(output, "submission.props")));
    }
}
