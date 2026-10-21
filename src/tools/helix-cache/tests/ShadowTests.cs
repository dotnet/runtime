// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
    public async Task ColdStoreAndRestoredEntriesCompareActualOutcomes()
    {
        using CacheFixture f = new();
        string first = f.Prepare();
        await Collect(f, first, CacheFixture.FirstJob);
        ShadowSummary cold = CacheFixture.Read(Path.Combine(first, "summary.json"), CacheJsonContext.Default.ShadowSummary);
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
        ShadowSummary summary = CacheFixture.Read(Path.Combine(second, "summary.json"), CacheJsonContext.Default.ShadowSummary);
        Assert.Equal(OutcomeComparison.Match, summary.Comparison);
        Assert.Equal(CacheFixture.FirstJob, summary.CachedJobId);
        Assert.Equal(CacheFixture.SecondJob, summary.JobId);
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
        ShadowSummary summary = CacheFixture.Read(Path.Combine(second, "summary.json"), CacheJsonContext.Default.ShadowSummary);
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
        Assert.False(Directory.Exists(Path.Combine(output, "results")));
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
            Assert.False(Directory.Exists(Path.Combine(output, "results")));
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
        Assert.False(Directory.Exists(Path.Combine(output, "results")));
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
        Assert.False(Directory.Exists(Path.Combine(second, "results")));
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
            Name = wrongName ? "other" : ShadowValidation.WorkItem,
        };
        using HttpClient client = Client(new HelixJob { Name = CacheFixture.FirstJob, QueueId = "queue" }, item);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new HelixClient(client).WaitForCompletionAsync(CacheFixture.FirstJob, ShadowValidation.WorkItem, TimeSpan.FromMinutes(1)));
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
                CacheFixture.FirstJob, ShadowValidation.WorkItem, TimeSpan.FromMinutes(1), cancellation.Token));
        }
        else
        {
            await Assert.ThrowsAsync<TimeoutException>(() => new HelixClient(client).WaitForCompletionAsync(
                CacheFixture.FirstJob, ShadowValidation.WorkItem, TimeSpan.FromMilliseconds(20)));
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
            CacheFixture.FirstJob, ShadowValidation.WorkItem, TimeSpan.FromMinutes(1));
        Assert.Equal("Finished", completed.State);
        Assert.Equal(2, requests);
    }

    [Fact]
    public async Task TransportTimeoutIsNotReportedAsTheJobDeadline()
    {
        using HttpClient client = new(new Stub((_, _) => throw new TaskCanceledException("HTTP request timed out")));
        TaskCanceledException error = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            new HelixClient(client).WaitForCompletionAsync(CacheFixture.FirstJob, ShadowValidation.WorkItem, TimeSpan.FromMinutes(1)));
        Assert.Equal("HTTP request timed out", error.Message);
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
        Name = ShadowValidation.WorkItem,
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
