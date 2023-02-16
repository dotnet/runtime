// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Workloads.Testing;

#nullable enable

namespace Wasm.Build.Tests;

public sealed class TestConfiguration
{
    private static Lazy<TestConfiguration> s_lazyInstance = new();
    public static TestConfiguration Instance => s_lazyInstance.Value;

    // private DotnetSdk[] DotnetSdks { get; init; }
    public TestProfile[] TestProfiles { get; init; }
    public string DefaultTargetFramework { get; private set; }

    public bool    SkipProjectCleanup  { get; private set; }

    public string XHarnessCliPath     { get; private set; }

    public string BuiltNuGetsPath     { get; private set; }

    public string SdksForTestingRootPath { get; private set; }

    public bool    ShowBuildOutput     { get; private set; }

    public string LogRootPath { get; private set; }
    public string? BrowserPathForTests { get; init; }

    // FIXME: make this a static class?
    private WorkloadTestSettings _testSettings;
    private string _defaultDotnetId;

    private const string BrowserPathForTestsVarName = "BROWSER_PATH_FOR_TESTS";

    public static readonly string RelativeTestAssetsPath = @"..\testassets\";
    public static readonly string TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
    public static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");
    public static readonly string TmpPath = Path.Combine(AppContext.BaseDirectory, "wbt");

    public TestProfile GetTestProfileByDotnetId(string id)
        => TestProfiles.FirstOrDefault(tp => string.Compare(tp.Sdk.Id, id ?? _defaultDotnetId, StringComparison.InvariantCultureIgnoreCase) == 0)
            ?? throw new KeyNotFoundException($"Cannot find any dotnet environment with id '{id}'");

    public TestConfiguration()
    {
        Console.WriteLine($"loading settings");
        string testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.txt");
        _testSettings = new WorkloadTestSettings(testSettingsPath);

        // Validate settings
        _defaultDotnetId = _testSettings.GetValue(WorkloadTestSettings.DefaultDotnetIdVarName) ?? "latest"; // FIXME: use default in base.cs
        DefaultTargetFramework = _testSettings.GetValue(WorkloadTestSettings.DefaultTargetFrameworkVarName) ?? "net8.0"; // FIXME: use default in base.cs
        SkipProjectCleanup = _testSettings.GetValue(WorkloadTestSettings.SkipProjectCleanupVarName) is not null;
        XHarnessCliPath = _testSettings.GetValue(WorkloadTestSettings.XHarnessCliPathVarName) ?? "xharness";
        ShowBuildOutput = _testSettings.GetValue(WorkloadTestSettings.ShowBuildOutputVarName) is not null;

        BuiltNuGetsPath = _testSettings.GetDirectoryPath(WorkloadTestSettings.BuiltNuGetsPathVarName);
        SdksForTestingRootPath = GetSdkRootPath();

        string? testLogPath = _testSettings.GetValue(WorkloadTestSettings.TestLogPathVarName);
        if (!string.IsNullOrEmpty(testLogPath))
        {
            LogRootPath = Path.GetFullPath(testLogPath);
            if (!Directory.Exists(LogRootPath))
                Directory.CreateDirectory(LogRootPath);
        }
        else
        {
            LogRootPath = Environment.CurrentDirectory;
        }

        BrowserPathForTests = _testSettings.GetValue(BrowserPathForTestsVarName);
        if (!string.IsNullOrEmpty(BrowserPathForTests) && !File.Exists(BrowserPathForTests))
            throw new Exception($"Cannot find {BrowserPathForTestsVarName}={BrowserPathForTests}");

        if (Directory.Exists(TmpPath))
            Directory.Delete(TmpPath, recursive: true);
        Directory.CreateDirectory(TmpPath);

        string? sdkForTestingRootPath = _testSettings.GetValue(WorkloadTestSettings.SdksForTestingRootPathVarName);
        if (string.IsNullOrEmpty(sdkForTestingRootPath))
            throw new Exception($"{WorkloadTestSettings.SdksForTestingRootPathVarName} is not set in test settings");

        string dotnetsInstallJsonPath = Path.Combine(sdkForTestingRootPath, "sdks-for-testing.manifest.json");
        SdksForTestingManifest? dotnetsForTesting =
                    JsonSerializer.Deserialize<SdksForTestingManifest>(
                                    File.ReadAllText(dotnetsInstallJsonPath),
                                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                                    {
                                        AllowTrailingCommas = true,
                                        ReadCommentHandling = JsonCommentHandling.Skip,
                                        PropertyNameCaseInsensitive = true
                                    });

        if (dotnetsForTesting is null)
            throw new InvalidDataException($"Failed to read dotnet installs manifest from {dotnetsInstallJsonPath}");

        Console.WriteLine ($"** Got #{dotnetsForTesting.Sdks.Length} sdks");
        foreach (var sdk in dotnetsForTesting.Sdks) {
            Console.WriteLine ($"denv: {sdk}");
        }

        // DotnetSdks = dotnetsForTesting.Sdks;
        (TestProfiles, _) = GetTestProfiles(SdksForTestingRootPath, dotnetsForTesting.Sdks);

        string GetSdkRootPath()
        {
            string? probePath = _testSettings.GetValue(WorkloadTestSettings.SdksForTestingRootPathVarName);
            if (string.IsNullOrEmpty(probePath))
            {
                // Is this a "local run?
                probePath = Path.Combine(Path.GetDirectoryName(typeof(TestConfiguration).Assembly.Location)!,
                                                "..",
                                                "..",
                                                "..",
                                                "..");
                if (!Directory.Exists(probePath))
                    throw new Exception($"{WorkloadTestSettings.SdksForTestingRootPathVarName} is not set, and could not fallback to artifacts path: {probePath}");
            }
            else if (!Directory.Exists(probePath))
            {
                throw new Exception($"Could not find {WorkloadTestSettings.SdksForTestingRootPathVarName}={probePath}");
            }

            return Path.GetFullPath(probePath);
        }
    }

    private (TestProfile[], string) GetTestProfiles(string sdkRootPath, DotnetSdk[] sdks)
    {
        List<TestProfile> testProfiles = new();
        foreach (DotnetSdk sdk in sdks)
        {
            string sdkFullPath = Path.Combine(sdkRootPath, sdk.RelativePath);
            if (!Directory.Exists(sdkFullPath))
                throw new DirectoryNotFoundException($"Could not find sdk (id: {sdk.Id}) at {sdkFullPath} using root path {sdkRootPath}, and relative path {sdk.RelativePath}");

            var EnvVars = new Dictionary<string, string>();
            EnvVars["DOTNET_INSTALL_DIR"] = sdkFullPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["PATH"] = $"{sdkFullPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";

            // Works around an issue in msbuild due to which
            // second, and subsequent builds fail without any details
            // in the logs
            EnvVars["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
            string DefaultBuildArgs = " /nr:false";

            testProfiles.Add(new TestProfile(sdk with { FullPath = sdkFullPath }, DefaultBuildArgs, EnvVars));
            Console.WriteLine ($"- new TestPRofile: {testProfiles[^1].Sdk}");
        }

        return (testProfiles.ToArray(), sdkRootPath);
    }
}
