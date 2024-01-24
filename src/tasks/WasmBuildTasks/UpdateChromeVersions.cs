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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using System.Xml;

using MBU = Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public partial class UpdateChromeVersions : MBU.Task
{
    private const string s_fetchReleasesUrl = "https://chromiumdash.appspot.com/fetch_releases?channel={0}&num=1";
    private const string s_snapshotBaseUrl = $"https://storage.googleapis.com/chromium-browser-snapshots";
    private const int s_versionCheckThresholdDays = 1;

    private static readonly HttpClient s_httpClient = new();

    [GeneratedRegex("#define V8_BUILD_NUMBER (\\d+)")]
    private static partial Regex V8BuildNumberRegex();

    public string Channel { get; set; } = "stable";

    [Required, NotNull]
    public string[] OSIdentifiers { get; set; } = Array.Empty<string>();

    // Used in https://commondatastorage.googleapis.com/chromium-browser-snapshots/index.html?prefix=Linux_x64/
    [Required, NotNull]
    public string[] OSPrefixes { get; set; } = Array.Empty<string>();

    [Required, NotNull]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    [Required, NotNull]
    public string ChromeVersionsPath { get; set; } = string.Empty;

    public int MaxMajorVersionsToCheck { get; set; } = 2;

    // start at the branch position found in all.json, and try to
    // download chrome, and chromedriver. If not found, then try up to
    // MaxBranchPositionsToCheck lower versions
    public int MaxBranchPositionsToCheck { get; set; } = 75;

    [Output]
    public bool VersionsChanged { get; set; }

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

            XmlDocument chromeVersionsXmlDoc = new XmlDocument();
            chromeVersionsXmlDoc.Load(ChromeVersionsPath);
            var osInfo = OSIdentifiers.Zip(OSPrefixes, (num, str) => new { Identifier = num, Prefix = str });
            foreach (var info in osInfo)
            {
                (ChromeVersionSpec version, string baseUrl) = await FindVersionFromChromiumDash(info.Prefix, info.Identifier).ConfigureAwait(false);
                bool hasMajorChanges = AreVersionsChanged(chromeVersionsXmlDoc, version, baseUrl);
                if (hasMajorChanges)
                {
                    VersionsChanged = UpdateChromeVersionsFile(chromeVersionsXmlDoc, version, baseUrl);
                }
            }
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    private static bool AreVersionsChanged(XmlDocument xmlDoc, ChromeVersionSpec version, string baseUrl)
    {
        if (string.Equals(version.os, "Linux", StringComparison.OrdinalIgnoreCase))
        {
            string linuxChromeBaseSnapshotUrl = GetNodeValue(xmlDoc, "linux_ChromeBaseSnapshotUrl");
            string linuxV8Version = GetNodeValue(xmlDoc, "linux_V8Version");
            return version.v8_version != linuxV8Version ||
                baseUrl != linuxChromeBaseSnapshotUrl;
        }
        else if (string.Equals(version.os, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            string winChromeBaseSnapshotUrl = GetNodeValue(xmlDoc, "win_ChromeBaseSnapshotUrl");
            string winV8Version = GetNodeValue(xmlDoc, "win_V8Version");
            return version.v8_version != winV8Version ||
                baseUrl != winChromeBaseSnapshotUrl;
        }
        throw new Exception($"UpdateChromeVersions task was used with unknown OS: {version.os}");
    }

    private static string GetNodeValue(XmlDocument xmlDoc, string nodeName)
    {
        XmlNode? node = xmlDoc.SelectSingleNode($"/Project/PropertyGroup/{nodeName}");
        return node?.InnerText ?? string.Empty;
    }

    private bool UpdateChromeVersionsFile(XmlDocument xmlDoc, ChromeVersionSpec version, string baseUrl)
    {
        if (string.Equals(version.os, "Linux", StringComparison.OrdinalIgnoreCase))
        {
            UpdateNodeValue(xmlDoc, "linux_ChromeVersion", version.version);
            UpdateNodeValue(xmlDoc, "linux_ChromeRevision", version.branch_base_position);
            UpdateNodeValue(xmlDoc, "linux_ChromeBaseSnapshotUrl", baseUrl);
            UpdateNodeValue(xmlDoc, "linux_V8Version", version.v8_version);
        }
        else if (string.Equals(version.os, "Windows", StringComparison.OrdinalIgnoreCase))
        {
            UpdateNodeValue(xmlDoc, "win_ChromeVersion", version.version);
            UpdateNodeValue(xmlDoc, "win_ChromeRevision", version.branch_base_position);
            UpdateNodeValue(xmlDoc, "win_ChromeBaseSnapshotUrl", baseUrl);
            UpdateNodeValue(xmlDoc, "win_V8Version", version.v8_version);
        }
        else
        {
            throw new Exception($"UpdateChromeVersions task was used with unknown OS: {version.os}");
        }
        xmlDoc.Save(ChromeVersionsPath);
        return true;
    }

    private static void UpdateNodeValue(XmlDocument xmlDoc, string nodeName, string newValue)
    {
        XmlNode? node = xmlDoc.SelectSingleNode($"/Project/PropertyGroup/{nodeName}");
        if (node == null)
        {
            throw new Exception($"Node '{nodeName}' not found in the XML file.");
        }
        node.InnerText = newValue;
    }

    private async Task<(ChromeVersionSpec version, string baseSnapshotUrl)> FindVersionFromChromiumDash(string osPrefix, string osIdentifier)
    {
        using Stream stream = await GetDownloadFileStreamAsync("fetch_releases.json", string.Format(s_fetchReleasesUrl, Channel)).ConfigureAwait(false);

        ChromiumDashRelease[]? releases = await JsonSerializer
                                                    .DeserializeAsync<ChromiumDashRelease[]>(stream)
                                                    .ConfigureAwait(false);
        if (releases is null)
            throw new LogAsErrorException($"Failed to read chrome versions from {s_fetchReleasesUrl}");

        ChromiumDashRelease? foundRelease = releases.Where(rel => string.Equals(rel.platform, osIdentifier, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
        if (foundRelease is null)
        {
            string availablePlatformIds = string.Join(", ", releases.Select(rel => rel.platform).Distinct().Order());
            throw new LogAsErrorException($"Unknown Platform '{osIdentifier}'. Platforms found " +
                                            $"in fetch_releases.json: {availablePlatformIds}");
        }

        string foundV8Version = await FindV8VersionFromChromeVersion(foundRelease.version).ConfigureAwait(false);
        ChromeVersionSpec versionSpec = new(os: foundRelease.platform,
                                            channel: Channel,
                                            version: foundRelease.version,
                                            branch_base_position: foundRelease.chromium_main_branch_position.ToString(),
                                            v8_version: foundV8Version);
        string? baseSnapshotUrl = await FindSnapshotUrlFromBasePositionAsync(osPrefix, versionSpec, throwIfNotFound: true)
                                        .ConfigureAwait(false);
        return (versionSpec, baseSnapshotUrl!);

        /*
         * Taken from jsvu:
         * url: `https://raw.githubusercontent.com/v8/v8/${major}.${minor}-lkgr/include/v8-version.h`,
         * then regex: /#define V8_BUILD_NUMBER (\d+)/
         */
        async Task<string> FindV8VersionFromChromeVersion(string chromeVersion)
        {
            // 117.0.493.123
            try
            {
                Version ver = new(chromeVersion);
                int majorForV8 = (int)(ver.Major/10);
                int minorForV8 = ver.Major % 10;

                string lkgUrl = $"https://raw.githubusercontent.com/v8/v8/{majorForV8}.{minorForV8}-lkgr/include/v8-version.h";
                Log.LogMessage(MessageImportance.Low, $"Checking {lkgUrl} ...");
                string v8VersionHContents = await s_httpClient.GetStringAsync(lkgUrl).ConfigureAwait(false);

                var m = V8BuildNumberRegex().Match(v8VersionHContents);
                if (!m.Success)
                    throw new LogAsErrorException($"Failed to find v8 build number at {lkgUrl}: {v8VersionHContents}");

                if (!int.TryParse(m.Groups[1].ToString(), out int buildNumber))
                    throw new LogAsErrorException($"Failed to parse v8 build number {m.Groups[1]}");

                return $"{majorForV8}.{minorForV8}.{buildNumber}";
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is ArgumentOutOfRangeException)
            {
                throw new LogAsErrorException($"Failed to parse chrome version '{chromeVersion}' to extract the milestone: {ex.Message}");
            }
        }
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
                using Stream stream = await s_httpClient.GetStreamAsync(url).ConfigureAwait(false);
                using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fs).ConfigureAwait(false);
            }
            catch (HttpRequestException hre)
            {
                throw new LogAsErrorException($"Failed to download {url}: {hre.Message}");
            }
        }

        return File.OpenRead(filePath);
    }

    private async Task<string?> FindSnapshotUrlFromBasePositionAsync(string osPrefix, ChromeVersionSpec version, bool throwIfNotFound = true)
    {
        string baseUrl = $"{s_snapshotBaseUrl}/{osPrefix}";

        int branchPosition = int.Parse(version.branch_base_position);
        for (int i = 0; i < MaxBranchPositionsToCheck; i++)
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

    private sealed record ChromiumDashRelease(string channel, int chromium_main_branch_position, string milesone, string platform, string version);

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
