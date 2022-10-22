// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

using MBU = Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.WebAssembly.Build.Tasks;

public class GetChromeVersions : MBU.Task
{
    private const string s_allJsonUrl = "http://omahaproxy.appspot.com/all.json";
    private const string s_snapshotBaseUrl = $"https://storage.googleapis.com/chromium-browser-snapshots";
    private const int s_versionCheckThresholdDays = 3;
    private const int s_numBranchPositionsToTry = 30;
    private static readonly HttpClient s_httpClient = new();

    public string Channel { get; set; } = "stable";

    [Required, NotNull]
    public string OSIdentifier { get; set; } = string.Empty;

    // Used in https://commondatastorage.googleapis.com/chromium-browser-snapshots/index.html?prefix=Linux_x64/
    [Required, NotNull]
    public string? OSPrefix { get; set; }

    [Required, NotNull]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    // start at the branch position found in all.json, and try to
    // download chrome, and chromedriver. If not found, then try up to
    // MaxLowerBranchPositionsToCheck lower versions
    public int MaxLowerBranchPositionsToCheck { get; set; } = 10;

    [Output]
    public string ChromeVersion { get; set; } = string.Empty;
    [Output]
    public string BranchPosition { get; set; } = string.Empty;
    [Output]
    public string BaseSnapshotUrl { get; set; } = string.Empty;
    [Output]
    public string V8Version { get; set; } = string.Empty;

    public override bool Execute() => ExecuteInternalAsync().Result;

    private async Task<bool> ExecuteInternalAsync()
    {
        if (!Directory.Exists(IntermediateOutputPath))
        {
            Log.LogError($"Cannot find {nameof(IntermediateOutputPath)}={IntermediateOutputPath}");
            return false;
        }

        try
        {
            ChromeVersionSpec version = await GetChromeVersionAsync().ConfigureAwait(false);
            BaseSnapshotUrl = await GetChromeUrlsAsync(version).ConfigureAwait(false);
            ChromeVersion = version.version;
            V8Version = version.v8_version;
            BranchPosition = version.branch_base_position;

            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    private async Task<string> GetChromeUrlsAsync(ChromeVersionSpec version)
    {
        string baseUrl = $"{s_snapshotBaseUrl}/{OSPrefix}";

        int branchPosition = int.Parse(version.branch_base_position);
        for (int i = 0; i < s_numBranchPositionsToTry; i++)
        {
            string branchUrl = $"{baseUrl}/{branchPosition}";
            string url = $"{branchUrl}/REVISIONS";

            Log.LogMessage(MessageImportance.Low, $"Checking if {url} exists ..");
            HttpResponseMessage response = await s_httpClient
                                                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                                    .ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Log.LogMessage(MessageImportance.Low, $"Found {url}");
                return branchUrl;
            }

            branchPosition += 1;
        }

        throw new LogAsErrorException($"Could not find a chrome snapshot folder under {baseUrl}, " +
                                        $"for branch positions {version.branch_base_position} to " +
                                        $"{branchPosition}, for version {version.version}.");
    }

    private async Task<ChromeVersionSpec> GetChromeVersionAsync()
    {
        using Stream stream = await GetAllJsonContentsAsync().ConfigureAwait(false);

        PerOSVersions[]? perOSVersions = await JsonSerializer
                                                    .DeserializeAsync<PerOSVersions[]>(stream)
                                                    .ConfigureAwait(false);
        if (perOSVersions is null)
            throw new LogAsErrorException($"Failed to read chrome versions from {s_allJsonUrl}");

        PerOSVersions[] foundOSVersions = perOSVersions.Where(osv => osv.os == OSIdentifier).ToArray();
        if (foundOSVersions.Length == 0)
        {
            string availableOSIds = string.Join(", ", perOSVersions.Select(osv => osv.os).Distinct().Order());
            throw new LogAsErrorException($"Unknown OS identifier '{OSIdentifier}'. OS identifiers found " +
                                            $"in all.json: {availableOSIds}");
        }

        ChromeVersionSpec[] foundChromeVersions = foundOSVersions
                                                    .SelectMany(osv => osv.versions)
                                                    .Where(cv => cv.channel == Channel)
                                                    .ToArray();

        if (foundChromeVersions.Length == 0)
        {
            string availableChannels = string.Join(", ", perOSVersions
                                                            .SelectMany(osv => osv.versions)
                                                            .Select(cv => cv.channel)
                                                            .Distinct()
                                                            .Order());
            throw new LogAsErrorException($"Unknown chrome channel '{Channel}'. Channels found in all.json: " +
                                            availableChannels);
        }

        return foundChromeVersions[0];
    }

    private async Task<Stream> GetAllJsonContentsAsync()
    {
        string allJsonPath = Path.Combine(IntermediateOutputPath, "all.json");

        // Don't download all.json on every build, instead do that
        // only every few days
        if (File.Exists(allJsonPath) &&
            (DateTime.UtcNow - File.GetLastWriteTimeUtc(allJsonPath)).TotalDays < s_versionCheckThresholdDays)
        {
            Log.LogMessage(MessageImportance.Low,
                                $"{s_allJsonUrl} will not be downloaded again, as ${allJsonPath} " +
                                $"is less than {s_versionCheckThresholdDays} old.");
        }
        else
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"Downloading {s_allJsonUrl} ...");
                Stream stream = await s_httpClient.GetStreamAsync(s_allJsonUrl).ConfigureAwait(false);
                using FileStream fs = File.OpenWrite(allJsonPath);
                await stream.CopyToAsync(fs).ConfigureAwait(false);
            }
            catch (HttpRequestException hre)
            {
                throw new LogAsErrorException($"Failed to download {s_allJsonUrl}: {hre.Message}");
            }
        }

        return File.OpenRead(allJsonPath);
    }


    private sealed record PerOSVersions(string os, ChromeVersionSpec[] versions);
    private sealed record ChromeVersionSpec(string os, string channel, string version, string branch_base_position, string v8_version);
}
