// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HelixCache;

internal sealed class HelixClient(HttpClient client)
{
    private const string Api = "https://helix.dot.net/api/2019-06-17";

    internal async Task<List<HelixQueue>> GetQueuesAsync(CancellationToken cancellationToken) =>
        await client.GetFromJsonAsync($"{Api}/info/queues", HelixJsonContext.Default.ListHelixQueue, cancellationToken)
            ?? throw new InvalidDataException("Helix returned no queue metadata.");

    internal async Task<HelixJob> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
        await client.GetFromJsonAsync($"{Api}/jobs/{jobId}/details", HelixJsonContext.Default.HelixJob, cancellationToken)
            ?? throw new InvalidDataException("Helix returned no job details.");

    internal async Task<HelixWorkItem> WaitForCompletionAsync(string jobId, string name, TimeSpan wait,
        CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(wait);
        try
        {
            while (true)
            {
                HelixWorkItem item = await client.GetFromJsonAsync(
                    $"{Api}/jobs/{jobId}/workitems/{Uri.EscapeDataString(name)}",
                    HelixJsonContext.Default.HelixWorkItem, timeout.Token)
                    ?? throw new InvalidDataException("Helix returned no work item.");
                if (item.Job != jobId || item.Name != name)
                {
                    throw new InvalidDataException("Helix returned a different job or work item.");
                }
                if (item.State is "Finished" or "Passed" or "Failed")
                {
                    return item;
                }
                if (item.State is not ("Unscheduled" or "Waiting" or "Running"))
                {
                    throw new InvalidDataException($"Unexpected Helix work item state: {item.State}.");
                }
                await Task.Delay(TimeSpan.FromSeconds(15), timeout.Token);
            }
        }
        catch (OperationCanceledException ex) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Helix job {jobId} did not complete within {wait.TotalSeconds} seconds.", ex);
        }
    }

    internal async Task DownloadResultsAsync(HelixWorkItem item, string directory, CancellationToken cancellationToken)
    {
        List<HelixFile> files = await client.GetFromJsonAsync(
            $"{Api}/jobs/{item.Job}/workitems/{Uri.EscapeDataString(item.Name)}/files?latestOnly=true",
            HelixJsonContext.Default.ListHelixFile, cancellationToken)
            ?? throw new InvalidDataException("Helix returned no work item file list.");
        if (files.Any(file => file is null))
        {
            throw new InvalidDataException("Helix work item files must not contain null.");
        }
        List<HelixFile> reports = files.Where(file => file.Name == "testResults.xml").ToList();
        if (reports.Count != 1)
        {
            throw new InvalidDataException($"Expected exactly one uploaded testResults.xml; found {reports.Count}.");
        }
        if (Directory.Exists(directory) || File.Exists(directory))
        {
            throw new IOException($"The result download directory already exists: {directory}.");
        }
        Directory.CreateDirectory(directory);
        await DownloadAsync(reports[0].Link, Path.Combine(directory, "testResults.xml"), cancellationToken);
        if (!string.IsNullOrEmpty(item.ConsoleOutputUri))
        {
            await DownloadAsync(item.ConsoleOutputUri, Path.Combine(directory, "console.log"), cancellationToken);
        }
    }

    private async Task DownloadAsync(string uri, string path, CancellationToken cancellationToken)
    {
        using Stream source = await client.GetStreamAsync(uri, cancellationToken);
        using FileStream destination = new(path, FileMode.CreateNew, FileAccess.Write);
        await source.CopyToAsync(destination, cancellationToken);
    }
}
