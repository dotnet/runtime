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
    private const string s_v8CanaryBaseUrl = "https://storage.googleapis.com/chromium-v8/official/canary";
    private const int s_versionCheckThresholdDays = 1;

    // The V8 used for testing must support the standardized exnref exception-handling proposal.
    // Chrome milestones can map to older V8 builds, so never downgrade the pinned V8 below this
    // floor. See https://github.com/dotnet/runtime/issues/129849
    private static readonly Version s_minV8Version = new(15, 1, 103);

    private static readonly HttpClient s_httpClient = new();

    [GeneratedRegex("#define V8_BUILD_NUMBER (\\d+)")]
    private static partial Regex V8BuildNumberRegex { get; }

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

    [Required, NotNull]
    public string EnvVarsForPRPath { get; set; } = string.Empty;

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
            List<ChromeVersionSpec> versions = new();
            foreach (var info in osInfo)
            {
                (ChromeVersionSpec version, string baseUrl) = await FindVersionFromChromiumDash(info.Prefix, info.Identifier).ConfigureAwait(false);
                versions.Add(version);
                bool hasMajorChanges = AreVersionsChanged(chromeVersionsXmlDoc, version, baseUrl);
                if (hasMajorChanges)
                {
                    VersionsChanged = UpdateChromeVersionsFile(chromeVersionsXmlDoc, version, baseUrl);
                }
            }
            if (VersionsChanged)
            {
                UpdateEnvVarsForPRFile(chromeVersionsXmlDoc, versions);
            }
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    private bool AreVersionsChanged(XmlDocument xmlDoc, ChromeVersionSpec version, string baseUrl)
    {
        string nodePrefix = GetNodePrefix(version.os);

        string existingChromeVersion = GetNodeValue(xmlDoc, $"{nodePrefix}_ChromeVersion");
        if (IsVersionDowngrade(existingChromeVersion, version.version))
        {
            Log.LogMessage(MessageImportance.High,
                $"Skipping {version.os}: candidate Chrome version {version.version} is older than existing {existingChromeVersion}.");
            return false;
        }

        string existingChromeBaseSnapshotUrl = GetNodeValue(xmlDoc, $"{nodePrefix}_ChromeBaseSnapshotUrl");
        string existingV8Version = GetNodeValue(xmlDoc, $"{nodePrefix}_V8Version");

        bool chromeChanged = version.version != existingChromeVersion;
        bool snapshotUrlChanged = baseUrl != existingChromeBaseSnapshotUrl;
        // A V8 downgrade must not count as a change: we never lower the V8 version (see UpdateChromeVersionsFile).
        bool v8Upgraded = version.v8_version != existingV8Version && !IsVersionDowngrade(existingV8Version, version.v8_version);

        return chromeChanged || snapshotUrlChanged || v8Upgraded;
    }

    private static string GetNodePrefix(string os) => os switch
    {
        _ when string.Equals(os, "Linux", StringComparison.OrdinalIgnoreCase) => "linux",
        _ when string.Equals(os, "Windows", StringComparison.OrdinalIgnoreCase) => "win",
        _ when string.Equals(os, "Mac", StringComparison.OrdinalIgnoreCase) => "macos",
        _ => throw new Exception($"UpdateChromeVersions task was used with unknown OS: {os}")
    };

    // Returns true only when both values parse and the candidate is strictly older than the existing one.
    private static bool IsVersionDowngrade(string existing, string candidate) =>
        Version.TryParse(existing, out Version? existingVersion) &&
        Version.TryParse(candidate, out Version? candidateVersion) &&
        candidateVersion < existingVersion;

    private static string GetNodeValue(XmlDocument xmlDoc, string nodeName)
    {
        XmlNode? node = xmlDoc.SelectSingleNode($"/Project/PropertyGroup/{nodeName}");
        return node?.InnerText ?? string.Empty;
    }

    private bool UpdateChromeVersionsFile(XmlDocument xmlDoc, ChromeVersionSpec version, string baseUrl)
    {
        string nodePrefix = GetNodePrefix(version.os);

        UpdateNodeValue(xmlDoc, $"{nodePrefix}_ChromeVersion", version.version);
        UpdateNodeValue(xmlDoc, $"{nodePrefix}_ChromeRevision", version.branch_base_position);
        UpdateNodeValue(xmlDoc, $"{nodePrefix}_ChromeBaseSnapshotUrl", baseUrl);

        // Never downgrade the V8 version. V8 tracks the Chrome milestone, but a manually pinned
        // (or otherwise higher) V8 version must not be reverted to an older one by the bot.
        string existingV8Version = GetNodeValue(xmlDoc, $"{nodePrefix}_V8Version");
        if (IsVersionDowngrade(existingV8Version, version.v8_version))
        {
            Log.LogMessage(MessageImportance.High,
                $"Keeping existing {version.os} V8 version {existingV8Version}: candidate {version.v8_version} is older.");
        }
        else
        {
            UpdateNodeValue(xmlDoc, $"{nodePrefix}_V8Version", version.v8_version);
        }

        xmlDoc.Save(ChromeVersionsPath);
        return true;
    }

    private void UpdateEnvVarsForPRFile(XmlDocument xmlDoc, List<ChromeVersionSpec> versions)
    {
        using StreamWriter writer = new StreamWriter(EnvVarsForPRPath);
        foreach (var version in versions)
        {
            if (string.Equals(version.os, "Linux", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteLine($"CHROME_LINUX_VER={GetNodeValue(xmlDoc, "linux_ChromeVersion")}");
            }
            else if (string.Equals(version.os, "Windows", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteLine($"CHROME_WIN_VER={GetNodeValue(xmlDoc, "win_ChromeVersion")}");
            }
            else if (string.Equals(version.os, "Mac", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteLine($"CHROME_MAC_VER={GetNodeValue(xmlDoc, "macos_ChromeVersion")}");
            }
            else
            {
                throw new Exception($"UpdateEnvVarsForPRFile task was used with unknown OS: {version.os}");
            }
        }
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

        // Never downgrade V8 below the exnref-capable floor, even if the Chrome milestone maps to an older build.
        if (Version.TryParse(foundV8Version, out Version? candidateV8Version) && candidateV8Version < s_minV8Version)
        {
            string existingV8Version = GetExistingV8Version(osIdentifier);
            Log.LogWarning($"Computed V8 version {foundV8Version} for {osIdentifier} is below the minimum " +
                            $"exnref-capable version {s_minV8Version} — keeping the existing V8 version {existingV8Version}.");
            foundV8Version = existingV8Version;
            if (string.IsNullOrEmpty(foundV8Version))
                throw new LogAsErrorException($"Computed V8 version for {osIdentifier} is below {s_minV8Version} and no existing version found in {ChromeVersionsPath}");
        }
        // Validate that a prebuilt V8 binary exists on the CDN that jsvu uses for downloading.
        else if (!await IsV8BinaryAvailableAsync(osPrefix, foundV8Version).ConfigureAwait(false))
        {
            string v8BinaryUrl = GetV8BinaryUrl(osPrefix, foundV8Version);
            Log.LogWarning($"V8 binary not available at {v8BinaryUrl} — keeping the existing V8 version for {osIdentifier}.");

            // Fall back to the existing V8 version from BrowserVersions.props
            foundV8Version = GetExistingV8Version(osIdentifier);
            if (string.IsNullOrEmpty(foundV8Version))
                throw new LogAsErrorException($"V8 binary for {osIdentifier} not available on CDN and no existing version found in {ChromeVersionsPath}");
        }

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

                var m = V8BuildNumberRegex.Match(v8VersionHContents);
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

    private string GetExistingV8Version(string osIdentifier)
    {
        XmlDocument existingDoc = new XmlDocument();
        existingDoc.Load(ChromeVersionsPath);
        string existingV8VersionNodeName = string.Equals(osIdentifier, "linux", StringComparison.OrdinalIgnoreCase)
            ? "linux_V8Version"
            : string.Equals(osIdentifier, "windows", StringComparison.OrdinalIgnoreCase)
                ? "win_V8Version"
                : string.Equals(osIdentifier, "Mac", StringComparison.OrdinalIgnoreCase)
                    ? "macos_V8Version"
                    : throw new LogAsErrorException($"Unknown OS identifier '{osIdentifier}' for V8 version fallback");
        return GetNodeValue(existingDoc, existingV8VersionNodeName);
    }

    private static string GetV8BinaryUrl(string osPrefix, string v8Version)
    {
        string jsvuPlatform = osPrefix switch
        {
            "Linux_x64" => "linux64",
            "Win_x64" => "win32",
            "Mac_Arm" => "mac-arm64",
            _ => throw new ArgumentException($"Unknown OS prefix '{osPrefix}' for V8 binary URL")
        };
        return $"{s_v8CanaryBaseUrl}/v8-{jsvuPlatform}-rel-{v8Version}.zip";
    }

    private async Task<bool> IsV8BinaryAvailableAsync(string osPrefix, string v8Version)
    {
        string url = GetV8BinaryUrl(osPrefix, v8Version);
        Log.LogMessage(MessageImportance.Low, $"Checking if V8 binary exists at {url} ...");
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Head, url);
            using HttpResponseMessage response = await s_httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (HttpRequestException hre)
        {
            Log.LogWarning($"Failed to check V8 binary availability at {url}: {hre.Message}");
            return false;
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
                Log.LogMessage(MessageImportance.Low, $"Found url = {url} with branchUrl = ${branchUrl}");
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
