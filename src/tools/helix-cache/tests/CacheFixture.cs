// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace HelixCache.Tests;

internal sealed class CacheFixture : IDisposable
{
    internal const int ArchiveFileMode = 0x81A4;
    internal const int ArchiveExecutableMode = 0x81ED;
    internal const int ArchiveDirectoryMode = 0x41ED;
    internal const int ArchiveLinkMode = 0xA1FF;
    private const UnixFileMode ExecutableMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
    internal const string FirstJob = "00000000-0000-0000-0000-000000000001";
    internal const string SecondJob = "00000000-0000-0000-0000-000000000002";
    internal const string WorkItem = "shared";
    internal const string Report = """
        <assemblies><assembly name="/worker/Suite.dll" total="2" passed="1" failed="0" skipped="1">
        <collection><test name="one" result="Pass"/><test name="two" result="Skip"/></collection>
        </assembly></assemblies>
        """;

    internal string Root { get; } = Directory.CreateTempSubdirectory("helix-cache-test-").FullName;
    internal string CacheDirectory { get; set; }
    internal string PayloadDirectory { get; }
    internal string Archive { get; }
    internal List<HelixQueue> Queues { get; }
    internal Submission Submission { get; set; }
    internal WorkItem Item
    {
        get => Submission.WorkItems[0];
        set => Submission.WorkItems[0] = value;
    }

    internal CacheFixture()
    {
        CacheDirectory = Path.Combine(Root, "cache");
        PayloadDirectory = Directory.CreateDirectory(Path.Combine(Root, "payload")).FullName;
        File.SetUnixFileMode(PayloadDirectory, ExecutableMode);
        File.WriteAllText(Path.Combine(PayloadDirectory, "run.sh"), "#!/bin/sh\nexit 0\n");
        File.SetUnixFileMode(Path.Combine(PayloadDirectory, "run.sh"), ExecutableMode);
        Archive = Path.Combine(Root, "shared.zip");
        WriteArchive(Archive, [("shared.txt", "shared payload", ArchiveFileMode)]);
        Queues = [new HelixQueue
        {
            QueueId = "linux.test.open", Architecture = "AMD64", OperatingSystemGroup = "linux", IsAvailable = true,
            Metadata = new Dictionary<string, JsonElement>
            {
                ["GalleryImage"] = JsonSerializer.SerializeToElement(new { Version = "1" }),
                ["Artifacts"] = JsonSerializer.SerializeToElement(new[] { new { Id = "prepare", Parameters = new { version = "1" } } }),
            },
        }];
        Submission = new Submission
        {
            SchemaVersion = 1,
            WorkItems = [new WorkItem
            {
                Name = "suite", Queues = [Queues[0].QueueId], Payload = new Payload { Path = "payload" },
                CorrelationPayloads = [new Payload { Path = "shared.zip", Destination = "runtime" }],
                Command = "./run.sh", Environment = new() { ["SECOND"] = "2", ["FIRST"] = "1" },
                TimeoutSeconds = 60, ExpectedExitCode = 0,
            }],
        };
    }

    internal static void WriteArchive(string path, (string Name, string Content, int Mode)[] entries, bool repack = false)
    {
        using FileStream file = File.Create(path);
        using ZipArchive archive = new(file, ZipArchiveMode.Create);
        foreach ((string name, string content, int mode) in repack ? entries.Reverse() : entries)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, repack ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(repack ? 2025 : 2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            entry.ExternalAttributes = mode << 16;
            using StreamWriter writer = new(entry.Open());
            writer.Write(content);
        }
    }

    internal string Write<T>(string name, T value, JsonTypeInfo<T> type)
    {
        string path = Path.Combine(Root, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, type));
        return path;
    }

    internal static T Read<T>(string path, JsonTypeInfo<T> type) =>
        JsonSerializer.Deserialize(File.ReadAllText(path), type)!;

    internal LookupEntry Lookup(bool success = true)
    {
        Invoke(success, "lookup", Write("submission.json", Submission, CacheJsonContext.Default.Submission),
            "--cache", CacheDirectory, "--output", Path.Combine(Root, "lookup.json"),
            "--queue-info", Write("queues.json", Queues, HelixJsonContext.Default.ListHelixQueue));
        return success ? Read(Path.Combine(Root, "lookup.json"), CacheJsonContext.Default.LookupResult).Entries[0] : null!;
    }

    internal string CreateResults(ExecutionRecord execution, string jobId = FirstJob)
    {
        string results = Directory.CreateDirectory(Path.Combine(Root, "results")).FullName;
        Write("results/result.json", new ResultReceipt
        {
            SchemaVersion = 1,
            Completed = true,
            InfrastructureSucceeded = true,
            ExitCode = 0,
            JobId = jobId,
            WorkItemName = execution.Inputs.Name,
            TargetQueue = execution.Inputs.TargetQueue,
            ResultFiles = ["tests.xml"],
        }, CacheJsonContext.Default.ResultReceipt);
        File.WriteAllText(Path.Combine(results, "tests.xml"), Report);
        return results;
    }

    internal void Store(ExecutionRecord execution, bool success = true) =>
        Invoke(success, "store", Write("execution.json", execution, CacheJsonContext.Default.ExecutionRecord),
            "--results", Path.Combine(Root, "results"), "--cache", CacheDirectory);

    internal string Prepare(string name = "shadow", params string[] payloads)
    {
        string output = Path.Combine(Root, name);
        ShadowValidation.Prepare(new Cache(CacheDirectory), output, payloads.Length == 0 ? [Archive] : [.. payloads],
            Archive, Queues[0].QueueId, Archive, new LinuxEnvironment(Queues));
        return output;
    }

    internal static LookupEntry ShadowLookup(string output) =>
        Assert.Single(Read(Path.Combine(output, "lookup.json"), CacheJsonContext.Default.LookupResult).Entries);

    internal static string ShadowResults(string output, string name = WorkItem) => Path.Combine(output, "work-items", name, "results");

    internal string Invoke(bool success, params string[] arguments)
    {
        string? executable = Environment.GetEnvironmentVariable("HELIX_CACHE_EXECUTABLE");
        if (executable is null)
        {
            DirectoryInfo? directory = new(Environment.CurrentDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                directory = directory.Parent;
            }
            Assert.NotNull(directory);
            executable = Path.Combine(directory.FullName, "artifacts/tools/helix-cache/helix-cache");
        }
        ProcessStartInfo start = new(executable) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The native cache process timed out.");
        }
        string error = stderr.GetAwaiter().GetResult();
        Assert.True(process.ExitCode == (success ? 0 : 1), error);
        if (!success)
        {
            Assert.Contains("helix-cache:", error);
        }
        return stdout.GetAwaiter().GetResult();
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
