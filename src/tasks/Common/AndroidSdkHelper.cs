// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;

internal sealed class AndroidSdkHelper
{
    private readonly string _androidSdkPath;
    private readonly string _buildToolsPath;
    private readonly string _buildApiLevel;

    public AndroidSdkHelper(
        string? androidSdkPath,
        string? buildApiLevel,
        string? buildToolsVersion)
    {
        if (string.IsNullOrEmpty(androidSdkPath))
            androidSdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(androidSdkPath) || !Directory.Exists(androidSdkPath))
            throw new ArgumentException($"Android SDK='{androidSdkPath}' was not found or empty (can be set via ANDROID_SDK_ROOT envvar).");

        _androidSdkPath = androidSdkPath;

        // Try to get the latest API level if not specified
        if (string.IsNullOrEmpty(buildApiLevel))
            buildApiLevel = GetLatestApiLevel(_androidSdkPath);

        _buildApiLevel = buildApiLevel;

        // Try to get the latest build-tools version if not specified
        if (string.IsNullOrEmpty(buildToolsVersion))
            buildToolsVersion = GetLatestBuildTools(_androidSdkPath);

        _buildToolsPath = Path.Combine(_androidSdkPath, "build-tools", buildToolsVersion);

        if (!Directory.Exists(_buildToolsPath))
            throw new ArgumentException($"{_buildToolsPath} was not found.");
    }

    public string AndroidJarPath => Path.Combine(_androidSdkPath, "platforms", $"android-{_buildApiLevel}", "android.jar");

    public string BuildApiLevel => _buildApiLevel;

    public bool HasD8 => File.Exists(D8Path);
    public string D8Path => GetToolPath("d8", isBatToolOnWindows: true);
    public string DxPath => GetToolPath("dx", isBatToolOnWindows: true);
    public string AaptPath => GetToolPath("aapt");
    public string ZipalignPath => GetToolPath("zipalign");
    public string ApksignerPath => GetToolPath("apksigner", isBatToolOnWindows: true);

    private string GetToolPath(string tool, bool isBatToolOnWindows = false)
        => Path.Combine(_buildToolsPath, tool + (Utils.IsWindows() && isBatToolOnWindows ? ".bat" : ""));

    /// <summary>
    /// Scan android SDK for api levels (ignore preview versions)
    /// </summary>
    private static string GetLatestApiLevel(string androidSdkDir)
    {
        return Directory.GetDirectories(Path.Combine(androidSdkDir, "platforms"))
            .Select(file => int.TryParse(Path.GetFileName(file).Replace("android-", ""), out int apiLevel) ? apiLevel : -1)
            .OrderByDescending(v => v)
            .FirstOrDefault()
            .ToString();
    }

    /// <summary>
    /// Scan android SDK for build tools (ignore preview versions)
    /// </summary>
    private static string GetLatestBuildTools(string androidSdkPath)
    {
        string? buildTools = Directory.GetDirectories(Path.Combine(androidSdkPath, "build-tools"))
            .Select(Path.GetFileName)
            .Where(file => !file!.Contains('-'))
            .Select(file => { Version.TryParse(Path.GetFileName(file), out Version? version); return version; })
            .OrderByDescending(v => v)
            .FirstOrDefault()?.ToString();

        if (string.IsNullOrEmpty(buildTools))
            throw new ArgumentException($"Android SDK ({androidSdkPath}) doesn't contain build-tools.");

        return buildTools;
    }
}
