// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

using MBU = Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public class GetChromeVersions : MBU.Task
{
    private const string s_allJsonUrl = "http://omahaproxy.appspot.com/all.json";
    private const string s_snapshotBaseUrl = $"https://storage.googleapis.com/chromium-browser-snapshots";
    private const string s_historyUrl = $"https://omahaproxy.appspot.com/history";
    private const string s_depsUrlPrefix = $"https://omahaproxy.appspot.com/deps.json?version=";
    private const int s_versionCheckThresholdDays = 1;

    // start at the branch position found in all.json, and try to
    // download chrome, and chromedriver. If not found, then try up to
    // s_numBranchPositionsToTry lower versions
    private const int s_numBranchPositionsToTry = 50;
    private static readonly HttpClient s_httpClient = new();

    public string Channel { get; set; } = "stable";

    [Required, NotNull]
    public string OSIdentifier { get; set; } = string.Empty;

    // Used in https://commondatastorage.googleapis.com/chromium-browser-snapshots/index.html?prefix=Linux_x64/
    [Required, NotNull]
    public string? OSPrefix { get; set; }

    [Required, NotNull]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    public int MaxMajorVersionsToCheck { get; set; } = 2;

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
            (ChromeVersionSpec version, string baseUrl) = await FindVersionFromHistoryAsync().ConfigureAwait(false);
            // For using all.json, use this instead:
            // (ChromeVersionSpec version, string baseUrl) = await FindVersionFromAllJsonAsync().ConfigureAwait(false);

            BaseSnapshotUrl = baseUrl;
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

    private async Task<(ChromeVersionSpec versionSpec, string baseSnapshotUrl)> FindVersionFromHistoryAsync()
    {
        List<string> versionsTried = new();
        int numMajorVersionsTried = 0;

        string curMajorVersion = string.Empty;
        await foreach (string version in GetVersionsAsync())
        {
            string majorVersion = version[..version.IndexOf('.')];
            if (curMajorVersion != majorVersion)
            {
                if (numMajorVersionsTried >= MaxMajorVersionsToCheck)
                    break;
                if (numMajorVersionsTried > 0)
                    Log.LogMessage(MessageImportance.High, $"Trying older major version {majorVersion} ..");
                curMajorVersion = majorVersion;
                numMajorVersionsTried += 1;
            }
            versionsTried.Add(version);

            bool isFirstVersion = versionsTried.Count == 1;
            string depsUrl = s_depsUrlPrefix + version;

            Log.LogMessage(MessageImportance.Low, $"Trying to get details for version {version} from {depsUrl}");
            Stream depsStream = await s_httpClient.GetStreamAsync(depsUrl).ConfigureAwait(false);

            ChromeDepsVersionSpec? depsVersionSpec = await JsonSerializer
                                                        .DeserializeAsync<ChromeDepsVersionSpec>(depsStream)
                                                        .ConfigureAwait(false);
            if (depsVersionSpec is null)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not get deps for version {version} from {depsUrl}.");
                continue;
            }

            if (!isFirstVersion)
                Log.LogMessage(MessageImportance.Normal, $"Trying to find snapshot url for version {version} ..");

            ChromeVersionSpec versionSpec = depsVersionSpec.ToChromeVersionSpec(OSIdentifier, Channel);
            string? baseUrl = await FindSnapshotUrlFromBasePositionAsync(versionSpec, throwIfNotFound: false).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (numMajorVersionsTried > 1)
                    Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}Using an *older major* version of chrome: {versionSpec.version} . Failed to find snapshots for other {OSIdentifier} {Channel} versions: {string.Join(", ", versionsTried[..^1])} .{Environment.NewLine}");

                return (versionSpec, baseUrl);
            }

            Log.LogMessage(MessageImportance.Low, $"Could not find snapshot url for version {version} .");
        }

        throw new LogAsErrorException($"Could not find a snapshot for {OSIdentifier} {Channel}. Versions tried: {string.Join(", ", versionsTried)} . A fixed version+url can be set in eng/testing/ProvisioningVersions.props .");

        async IAsyncEnumerable<string> GetVersionsAsync()
        {
            Stream stream = await GetDownloadFileStreamAsync("history.csv", s_historyUrl).ConfigureAwait(false);
            using StreamReader sr = new(stream);
            string prefix = $"{OSIdentifier},{Channel},";
            int lineNum = 0;

            /*
                win,stable,113.0.5672.93,2023-05-08 20:43:02.119774
                win64,stable,113.0.5672.93,2023-05-08 20:43:01.624636
                mac_arm64,stable,113.0.5672.92,2023-05-08 20:43:01.067910
            */
            while (true)
            {
                string? line = await sr.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                lineNum++;
                if (!line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                string[] parts = line.Split(',', 4);
                yield return parts.Length >= 4
                                ? parts[2]
                                : throw new LogAsErrorException($"Failed to find at least 4 fields in history csv at line #{lineNum}: {line}");
            }
        }
    }

    private async Task<(ChromeVersionSpec versionSpec, string baseSnapshotUrl)> FindVersionFromAllJsonAsync()
    {
        using Stream stream = await GetDownloadFileStreamAsync("all.json", s_allJsonUrl).ConfigureAwait(false);

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

        string? baseSnapshotUrl = await FindSnapshotUrlFromBasePositionAsync(foundChromeVersions[0], throwIfNotFound: true).ConfigureAwait(false);
        return (foundChromeVersions[0], baseSnapshotUrl!);
    }

    private async Task<Stream> GetDownloadFileStreamAsync(string filename, string url)
    {
        string filePath = Path.Combine(IntermediateOutputPath, filename);

        // Don't download all.json on every build, instead do that
        // only every few days
        if (File.Exists(filePath) &&
            (DateTime.UtcNow - File.GetLastWriteTimeUtc(filePath)).TotalDays < s_versionCheckThresholdDays)
        {
            Log.LogMessage(MessageImportance.Low,
                                $"{url} will not be downloaded again, as ${filePath} " +
                                $"is less than {s_versionCheckThresholdDays} old.");
        }
        else
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"Downloading {url} ...");
                Stream stream = await s_httpClient.GetStreamAsync(url).ConfigureAwait(false);
                using FileStream fs = File.OpenWrite(filePath);
                await stream.CopyToAsync(fs).ConfigureAwait(false);
            }
            catch (HttpRequestException hre)
            {
                throw new LogAsErrorException($"Failed to download {url}: {hre.Message}");
            }
        }

        return File.OpenRead(filePath);
    }

    private async Task<string?> FindSnapshotUrlFromBasePositionAsync(ChromeVersionSpec version, bool throwIfNotFound = true)
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

        string message = $"Could not find a chrome snapshot folder under {baseUrl}, " +
                            $"for branch positions {version.branch_base_position} to " +
                            $"{branchPosition}, for version {version.version}. " +
                            "A fixed version+url can be set in eng/testing/ProvisioningVersions.props .";
        if (throwIfNotFound)
            throw new LogAsErrorException(message);
        else
            Log.LogMessage(MessageImportance.Low, message);

        return null;
    }

    private sealed record PerOSVersions(string os, ChromeVersionSpec[] versions);
    private sealed record ChromeVersionSpec(string os, string channel, string version, string branch_base_position, string v8_version);
    private sealed record ChromeDepsVersionSpec(string chromium_version, string chromium_base_position, string v8_version)
    {
        public ChromeVersionSpec ToChromeVersionSpec(string os, string channel) => new
        (
            os,
            channel,
            chromium_version,
            chromium_base_position,
            v8_version
        );
    }

}
