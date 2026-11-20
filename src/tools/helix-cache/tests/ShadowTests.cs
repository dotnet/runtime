// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HelixCache.Tests;

public sealed class ShadowTests
{
    [Fact]
    public async Task SeveralSuitesShareOneJobAndCompareMixedHitsAndMisses()
    {
        using CacheFixture f = new();
        string secondPayload = Path.Combine(f.Root, "Second.Tests.zip");
        File.Copy(f.Archive, secondPayload);
        string first = f.Prepare("first", f.Archive, secondPayload);
        Dictionary<string, string> reports = SuiteReports(CacheFixture.WorkItem, "Second.Tests");
        ShadowSummary cold = await CollectBatch(f, first, CacheFixture.FirstJob, reports);
        Assert.Equal(2, cold.WorkItems.Count);
        Assert.All(cold.WorkItems, item =>
        {
            Assert.Equal(LookupStatus.Miss, item.Lookup);
            Assert.Equal(OutcomeComparison.NotApplicable, item.Comparison);
            Assert.Equal(2, item.Tests);
            Assert.True(item.Stored);
        });

        string restored = Path.Combine(f.Root, "restored-cache");
        Snapshot.Copy(Path.Combine(f.CacheDirectory, "entries"), Path.Combine(restored, "entries"));
        Directory.Move(f.CacheDirectory, Path.Combine(f.Root, "previous-cache"));
        f.CacheDirectory = restored;
        CacheFixture.WriteArchive(secondPayload, [("changed.txt", "changed input", CacheFixture.ArchiveFileMode)]);
        string second = f.Prepare("second", f.Archive, secondPayload);
        ShadowSummary mixed = await CollectBatch(f, second, CacheFixture.SecondJob, reports);
        Assert.Equal(CacheFixture.SecondJob, mixed.JobId);
        Assert.Equal(LookupStatus.Hit, mixed.WorkItems[0].Lookup);
        Assert.Equal(OutcomeComparison.Match, mixed.WorkItems[0].Comparison);
        Assert.Equal(CacheFixture.FirstJob, mixed.WorkItems[0].CachedJobId);
        Assert.Equal(cold.WorkItems[0].Key, mixed.WorkItems[0].Key);
        Assert.Equal(LookupStatus.Miss, mixed.WorkItems[1].Lookup);
        Assert.Equal(OutcomeComparison.NotApplicable, mixed.WorkItems[1].Comparison);
        Assert.Null(mixed.WorkItems[1].CachedJobId);
        Assert.NotEqual(cold.WorkItems[1].Key, mixed.WorkItems[1].Key);
        Assert.All(mixed.WorkItems, item =>
        {
            Assert.True(item.Stored);
            string results = CacheFixture.ShadowResults(second, item.Name);
            Assert.Equal(reports[item.Name], File.ReadAllText(Path.Combine(results, "testResults.xml")));
            ResultReceipt receipt = CacheFixture.Read(Path.Combine(results, "result.json"), CacheJsonContext.Default.ResultReceipt);
            Assert.Equal(item.Name, receipt.WorkItemName);
            Assert.Equal(CacheFixture.SecondJob, receipt.JobId);
            ExecutionProvenance provenance = CacheFixture.Read(Path.Combine(results, "provenance.json"), CacheJsonContext.Default.ExecutionProvenance);
            Assert.Equal(item.Name + "-worker", provenance.MachineName);
        });
        LookupResult lookup = CacheFixture.Read(Path.Combine(second, "lookup.json"), CacheJsonContext.Default.LookupResult);
        Assert.Equal(CacheFixture.FirstJob, new Cache(f.CacheDirectory).ReadResults(lookup.Entries[0].Execution).Receipt.JobId);
        Assert.Equal(CacheFixture.SecondJob, new Cache(f.CacheDirectory).ReadResults(lookup.Entries[1].Execution).Receipt.JobId);
    }

    [Theory]
    [InlineData("remote")]
    [InlineData("report")]
    [InlineData("mismatch")]
    public async Task LaterSuiteFailuresPreventAdmissionOfTheWholeBatch(string failure)
    {
        using CacheFixture f = new();
        string secondPayload = Path.Combine(f.Root, "Second.Tests.zip");
        File.Copy(f.Archive, secondPayload);
        string seeded = f.Prepare("seed", secondPayload);
        await CollectBatch(f, seeded, CacheFixture.FirstJob, SuiteReports("Second.Tests"));
        string output = f.Prepare("batch", f.Archive, secondPayload);
        Dictionary<string, string> reports = SuiteReports(CacheFixture.WorkItem, "Second.Tests");
        if (failure is "report" or "mismatch")
        {
            reports["Second.Tests"] = failure == "report" ? "<assemblies/>" :
                reports["Second.Tests"].Replace("Second.Tests.one", "different", StringComparison.Ordinal);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CollectBatch(f, output, CacheFixture.SecondJob, reports, failure == "remote" ? "Second.Tests" : null));
        ShadowSummary summary = CacheFixture.Read(Path.Combine(output, "summary.json"), CacheJsonContext.Default.ShadowSummary);
        Assert.All(summary.WorkItems, item => Assert.False(item.Stored));
        Assert.Equal(OutcomeComparison.NotApplicable, summary.WorkItems[0].Comparison);
        if (failure == "mismatch")
        {
            Assert.Equal(OutcomeComparison.Mismatch, summary.WorkItems[1].Comparison);
        }
        Assert.Single(Directory.EnumerateFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories));
        LookupResult lookup = CacheFixture.Read(Path.Combine(output, "lookup.json"), CacheJsonContext.Default.LookupResult);
        Assert.Equal(CacheFixture.FirstJob, new Cache(f.CacheDirectory).ReadResults(lookup.Entries[1].Execution).Receipt.JobId);
        Assert.Equal(LookupStatus.Miss,
            Assert.Single(ShadowValidation.Prepare(new Cache(f.CacheDirectory), Path.Combine(f.Root, "retry"),
                [f.Archive], f.Archive, f.Queues[0].QueueId, f.Archive, new LinuxEnvironment(f.Queues)).Entries).Status);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unsafe")]
    [InlineData("status")]
    public async Task InvalidLaterLookupEntriesAreRejectedBeforeContactingHelix(string invalid)
    {
        using CacheFixture f = new();
        string secondPayload = Path.Combine(f.Root, "Second.Tests.zip");
        File.Copy(f.Archive, secondPayload);
        string output = f.Prepare("batch", f.Archive, secondPayload);
        LookupResult lookup = CacheFixture.Read(Path.Combine(output, "lookup.json"), CacheJsonContext.Default.LookupResult);
        ExecutionRecord execution = lookup.Entries[1].Execution;
        ExecutionInputs inputs = execution.Inputs with
        {
            Name = invalid == "duplicate" ? CacheFixture.WorkItem : invalid == "unsafe" ? "../escape" : execution.Inputs.Name,
        };
        lookup.Entries[1] = new LookupEntry
        {
            Status = invalid == "status" ? (LookupStatus)123 : LookupStatus.Miss,
            Execution = execution with { Inputs = inputs, Key = ExecutionKey.Create(inputs) },
        };
        File.WriteAllText(Path.Combine(output, "lookup.json"), JsonSerializer.Serialize(lookup, CacheJsonContext.Default.LookupResult));
        File.WriteAllText(Path.Combine(output, "job-id.txt"), CacheFixture.FirstJob);
        using HttpClient client = new(new Stub((_, _) => throw new InvalidOperationException("Unexpected HTTP request")));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            ShadowValidation.CollectAsync(new Cache(f.CacheDirectory), output, TimeSpan.FromMinutes(1), new HelixClient(client)));
        Assert.All(lookup.Entries, item => Assert.False(Directory.Exists(CacheFixture.ShadowResults(output, item.Execution.Inputs.Name))));
        Assert.Empty(Directory.EnumerateFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ColdStoreAndRestoredEntriesCompareActualOutcomes()
    {
        using CacheFixture f = new();
        string first = f.Prepare();
        await Collect(f, first, CacheFixture.FirstJob);
        ShadowWorkItemSummary cold = Assert.Single(
            CacheFixture.Read(Path.Combine(first, "summary.json"), CacheJsonContext.Default.ShadowSummary).WorkItems);
        Assert.Equal(LookupStatus.Miss, cold.Lookup);
        Assert.Equal(2, cold.Tests);
        string restored = Path.Combine(f.Root, "restored-cache");
        Snapshot.Copy(Path.Combine(f.CacheDirectory, "entries"), Path.Combine(restored, "entries"));
        Directory.Move(f.CacheDirectory, Path.Combine(f.Root, "previous-cache"));
        f.CacheDirectory = restored;
        string second = f.Prepare("second");
        Assert.Equal(LookupStatus.Hit, CacheFixture.ShadowLookup(second).Status);
        string reordered = """
            <assemblies><assembly name="/different/worker/Suite.dll" time="123" total="2" passed="1" failed="0" skipped="1">
            <collection><test name="two" result="Skip"/><test name="one" result="Pass"/></collection>
            </assembly></assemblies>
            """;
        await Collect(f, second, CacheFixture.SecondJob, report: reordered);
        ShadowSummary batch = CacheFixture.Read(Path.Combine(second, "summary.json"), CacheJsonContext.Default.ShadowSummary);
        ShadowWorkItemSummary summary = Assert.Single(batch.WorkItems);
        Assert.Equal(OutcomeComparison.Match, summary.Comparison);
        Assert.Equal(CacheFixture.FirstJob, summary.CachedJobId);
        Assert.Equal(CacheFixture.SecondJob, batch.JobId);
        Assert.Equal(cold.Key, summary.Key);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("skipReason")]
    [InlineData("duplicate")]
    public async Task MismatchesDoNotOverwriteCachedProvenance(string change)
    {
        using CacheFixture f = new();
        string first = f.Prepare();
        await Collect(f, first, CacheFixture.FirstJob);
        string second = f.Prepare("second");
        string changed = change switch
        {
            "name" => CacheFixture.Report.Replace("name=\"one\"", "name=\"different-test\"", StringComparison.Ordinal),
            "skipReason" => CacheFixture.Report.Replace("<test name=\"two\" result=\"Skip\"/>",
                "<test name=\"two\" result=\"Skip\"><reason><message>different</message></reason></test>", StringComparison.Ordinal),
            "duplicate" => CacheFixture.Report.Replace("total=\"2\" passed=\"1\"", "total=\"3\" passed=\"2\"", StringComparison.Ordinal)
                .Replace("<collection>", "<collection><test name=\"one\" result=\"Pass\"/>", StringComparison.Ordinal),
            _ => throw new InvalidOperationException(),
        };
        InvalidDataException error = await Assert.ThrowsAsync<InvalidDataException>(
            () => Collect(f, second, CacheFixture.SecondJob, report: changed));
        Assert.Contains("outcomes differ", error.Message);
        ShadowWorkItemSummary summary = Assert.Single(
            CacheFixture.Read(Path.Combine(second, "summary.json"), CacheJsonContext.Default.ShadowSummary).WorkItems);
        Assert.Equal(OutcomeComparison.Mismatch, summary.Comparison);
        Assert.False(summary.Stored);
        Assert.Equal(CacheFixture.FirstJob, summary.CachedJobId);
        string cached = CacheFixture.ShadowLookup(second).ResultsPath!;
        Assert.Equal(CacheFixture.FirstJob,
            CacheFixture.Read(Path.Combine(cached, "result.json"), CacheJsonContext.Default.ResultReceipt).JobId);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("failureReason")]
    [InlineData("errors")]
    [InlineData("exitCode")]
    [InlineData("missingExitCode")]
    public async Task UnsuccessfulRemoteCompletionsAreNotAdmitted(string failure)
    {
        using CacheFixture f = new();
        string output = f.Prepare();
        HelixWorkItem item = PassedItem(CacheFixture.FirstJob);
        item = failure switch
        {
            "failed" => item with { State = "Failed" },
            "failureReason" => item with { FailureReason = new HelixFailure() },
            "errors" => item with { Errors = [new HelixFailure()] },
            "exitCode" => item with { ExitCode = 1 },
            "missingExitCode" => item with { ExitCode = null },
            _ => throw new InvalidOperationException(),
        };
        InvalidDataException error = await Assert.ThrowsAsync<InvalidDataException>(
            () => Collect(f, output, CacheFixture.FirstJob, item: item));
        Assert.Contains("did not complete", error.Message);
        Assert.False(Directory.Exists(CacheFixture.ShadowResults(output)));
        Assert.Empty(Directory.EnumerateFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("linux.test.open.rt", "LINUX.TEST.OPEN", false, true)]
    [InlineData("linux.test.open", null, false, true)]
    [InlineData("linux.test.open", "", false, true)]
    [InlineData("linux.test.open", "other", false, false)]
    [InlineData("other", "other", false, false)]
    [InlineData("other", null, false, false)]
    [InlineData("linux.test.open.rt", "linux.test.open", true, false)]
    public async Task QueueAliasesAndJobIdentityAreValidatedAndRetained(
        string queueId, string? alias, bool wrongJob, bool accepted)
    {
        using CacheFixture f = new();
        string output = f.Prepare();
        HelixJob job = new()
        {
            Name = wrongJob ? CacheFixture.SecondJob : CacheFixture.FirstJob,
            QueueId = queueId,
            QueueAlias = alias,
        };
        if (!accepted)
        {
            InvalidDataException error = await Assert.ThrowsAsync<InvalidDataException>(
                () => Collect(f, output, CacheFixture.FirstJob, job: job));
            Assert.Contains("job or target queue", error.Message);
            Assert.False(Directory.Exists(CacheFixture.ShadowResults(output)));
            Assert.Empty(Directory.EnumerateFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories));
            return;
        }
        await Collect(f, output, CacheFixture.FirstJob, job: job);
        string entry = Directory.GetFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories)[0];
        string results = Path.Combine(Path.GetDirectoryName(entry)!, "results");
        ExecutionProvenance provenance = CacheFixture.Read(Path.Combine(results, "provenance.json"), CacheJsonContext.Default.ExecutionProvenance);
        Assert.Equal(queueId, provenance.ResolvedQueueId);
        Assert.Equal(alias, provenance.QueueAlias);
        Assert.Equal("linux.test.open",
            CacheFixture.Read(Path.Combine(results, "result.json"), CacheJsonContext.Default.ResultReceipt).TargetQueue);
    }

    [Fact]
    public void MissingQueueAliasIsSupportedByTypedDeserialization()
    {
        HelixJob job = JsonSerializer.Deserialize("""{"Name":"job","QueueId":"queue"}""", HelixJsonContext.Default.HelixJob)!;
        Assert.Null(job.QueueAlias);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ExactlyOneUploadedReportIsRequired(int count)
    {
        using CacheFixture f = new();
        string output = f.Prepare();
        List<HelixFile> files = [];
        for (int i = 0; i < count; i++)
        {
            files.Add(new HelixFile { Name = "testResults.xml", Link = "https://example.invalid/report" });
        }
        InvalidDataException error = await Assert.ThrowsAsync<InvalidDataException>(
            () => Collect(f, output, CacheFixture.FirstJob, files: files));
        Assert.Contains("exactly one", error.Message);
        Assert.False(Directory.Exists(CacheFixture.ShadowResults(output)));
    }

    [Theory]
    [InlineData("<assemblies/>")]
    [InlineData("""<assemblies><assembly name="Suite.dll" total="1" passed="1" failed="0" skipped="0"><collection><test result="Pass"/></collection></assembly></assemblies>""")]
    public async Task UploadedXmlMustPassNativeAdmission(string report)
    {
        using CacheFixture f = new();
        string output = f.Prepare();
        await Assert.ThrowsAsync<InvalidDataException>(() => Collect(f, output, CacheFixture.FirstJob, report: report));
        Assert.Empty(Directory.EnumerateFiles(f.CacheDirectory, "entry.json", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task CorruptionBetweenPrepareAndCollectIsNotOverwritten()
    {
        using CacheFixture f = new();
        string first = f.Prepare();
        await Collect(f, first, CacheFixture.FirstJob);
        string second = f.Prepare("second");
        string cachedReport = Path.Combine(CacheFixture.ShadowLookup(second).ResultsPath!, "testResults.xml");
        File.WriteAllText(cachedReport, "corrupt");
        await Assert.ThrowsAsync<InvalidDataException>(() => Collect(f, second, CacheFixture.SecondJob));
        Assert.False(Directory.Exists(CacheFixture.ShadowResults(second)));
        Assert.Equal("corrupt", File.ReadAllText(cachedReport));
    }

    [Theory]
    [InlineData("Canceled", false, false)]
    [InlineData("Passed", true, false)]
    [InlineData("Passed", false, true)]
    public async Task PollingRejectsUnknownStatesAndWrongIdentity(string state, bool wrongJob, bool wrongName)
    {
        HelixWorkItem item = PassedItem(CacheFixture.FirstJob) with
        {
            State = state,
            Job = wrongJob ? CacheFixture.SecondJob : CacheFixture.FirstJob,
            Name = wrongName ? "other" : CacheFixture.WorkItem,
        };
        using HttpClient client = Client(new HelixJob { Name = CacheFixture.FirstJob, QueueId = "queue" }, item);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new HelixClient(client).WaitForCompletionAsync(CacheFixture.FirstJob, CacheFixture.WorkItem, TimeSpan.FromMinutes(1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PollingHonorsTimeoutAndCallerCancellation(bool cancelCaller)
    {
        using HttpClient client = new(new Stub(async (_, cancellation) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
            throw new InvalidOperationException();
        }));
        using CancellationTokenSource cancellation = new();
        if (cancelCaller)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new HelixClient(client).WaitForCompletionAsync(
                CacheFixture.FirstJob, CacheFixture.WorkItem, TimeSpan.FromMinutes(1), cancellation.Token));
        }
        else
        {
            await Assert.ThrowsAsync<TimeoutException>(() => new HelixClient(client).WaitForCompletionAsync(
                CacheFixture.FirstJob, CacheFixture.WorkItem, TimeSpan.FromMilliseconds(20)));
        }
    }

    [Fact]
    public async Task PollingWaitsForTerminalCompletion()
    {
        int requests = 0;
        using HttpClient client = new(new Stub((_, _) =>
        {
            HelixWorkItem item = PassedItem(CacheFixture.FirstJob) with { State = ++requests == 1 ? "Running" : "Finished" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(item, HelixJsonContext.Default.HelixWorkItem),
            });
        }));
        HelixWorkItem completed = await new HelixClient(client).WaitForCompletionAsync(
            CacheFixture.FirstJob, CacheFixture.WorkItem, TimeSpan.FromMinutes(1));
        Assert.Equal("Finished", completed.State);
        Assert.Equal(2, requests);
    }

    [Fact]
    public async Task TransportTimeoutIsNotReportedAsTheJobDeadline()
    {
        using HttpClient client = new(new Stub((_, _) => throw new TaskCanceledException("HTTP request timed out")));
        TaskCanceledException error = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            new HelixClient(client).WaitForCompletionAsync(CacheFixture.FirstJob, CacheFixture.WorkItem, TimeSpan.FromMinutes(1)));
        Assert.Equal("HTTP request timed out", error.Message);
    }

    private static Dictionary<string, string> SuiteReports(params string[] names) =>
        names.ToDictionary(name => name, name => CacheFixture.Report.Replace("name=\"one\"", $"name=\"{name}.one\"", StringComparison.Ordinal));

    private static async Task<ShadowSummary> CollectBatch(CacheFixture f, string output, string jobId,
        Dictionary<string, string> reports, string? failingSuite = null)
    {
        File.WriteAllText(Path.Combine(output, "job-id.txt"), jobId);
        int jobRequests = 0;
        using HttpClient client = new(new Stub((request, _) =>
        {
            string path = request.RequestUri!.AbsolutePath;
            string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            HttpContent content;
            if (path.EndsWith("/details", StringComparison.Ordinal))
            {
                jobRequests++;
                content = JsonContent.Create(new HelixJob { Name = jobId, QueueId = f.Queues[0].QueueId }, HelixJsonContext.Default.HelixJob);
            }
            else if (path.StartsWith("/reports/", StringComparison.Ordinal))
            {
                content = new StringContent(reports[segments[^1]], Encoding.UTF8, "application/xml");
            }
            else if (path.EndsWith("/files", StringComparison.Ordinal))
            {
                content = JsonContent.Create(new List<HelixFile>
                {
                    new() { Name = "testResults.xml", Link = $"https://example.invalid/reports/{segments[^2]}" },
                }, HelixJsonContext.Default.ListHelixFile);
            }
            else
            {
                string name = segments[^1];
                Assert.Contains(name, reports.Keys);
                content = JsonContent.Create(PassedItem(jobId) with
                {
                    Name = name,
                    MachineName = name + "-worker",
                    State = name == failingSuite ? "Failed" : "Passed",
                }, HelixJsonContext.Default.HelixWorkItem);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }));
        ShadowSummary summary = await ShadowValidation.CollectAsync(new Cache(f.CacheDirectory), output, TimeSpan.FromMinutes(1), new HelixClient(client));
        Assert.Equal(1, jobRequests);
        return summary;
    }

    private static async Task Collect(CacheFixture f, string output, string jobId,
        HelixJob? job = null, HelixWorkItem? item = null, List<HelixFile>? files = null, string? report = null)
    {
        File.WriteAllText(Path.Combine(output, "job-id.txt"), jobId + "\n");
        using HttpClient client = Client(job ?? new HelixJob { Name = jobId, QueueId = f.Queues[0].QueueId },
            item ?? PassedItem(jobId), files, report);
        await ShadowValidation.CollectAsync(new Cache(f.CacheDirectory), output, TimeSpan.FromMinutes(1), new HelixClient(client));
    }

    private static HelixWorkItem PassedItem(string jobId) => new()
    {
        Job = jobId,
        Name = CacheFixture.WorkItem,
        State = "Passed",
        ExitCode = 0,
        MachineName = "test-worker",
    };

    private static HttpClient Client(HelixJob job, HelixWorkItem item, List<HelixFile>? files = null, string? report = null) =>
        new(new Stub((request, _) =>
        {
            HttpContent content = request.RequestUri!.AbsolutePath switch
            {
                string path when path.EndsWith("/details", StringComparison.Ordinal) =>
                    JsonContent.Create(job, HelixJsonContext.Default.HelixJob),
                string path when path.EndsWith("/files", StringComparison.Ordinal) =>
                    JsonContent.Create(files ?? [new HelixFile { Name = "testResults.xml", Link = "https://example.invalid/report" }],
                        HelixJsonContext.Default.ListHelixFile),
                "/report" => new StringContent(report ?? CacheFixture.Report, Encoding.UTF8, "application/xml"),
                string path when path.Contains("/workitems/", StringComparison.Ordinal) =>
                    JsonContent.Create(item, HelixJsonContext.Default.HelixWorkItem),
                _ => throw new InvalidOperationException($"Unexpected HTTP request: {request.RequestUri}"),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }));

    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
