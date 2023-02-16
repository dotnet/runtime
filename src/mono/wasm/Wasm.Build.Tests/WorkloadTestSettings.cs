// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

#nullable enable

namespace Workloads.Testing;

public class WorkloadTestSettings
{
    public const string TestLogPathVarName = "TEST_LOG_PATH";
    public const string SkipProjectCleanupVarName = "SKIP_PROJECT_CLEANUP";
    public const string XHarnessCliPathVarName = "XHARNESS_CLI_PATH";
    public const string BuiltNuGetsPathVarName = "BUILT_NUGETS_PATH";
    public const string SdksForTestingRootPathVarName = "SDKS_FOR_TESTING_ROOT_PATH";
    public const string ShowBuildOutputVarName = "SHOW_BUILD_OUTPUT";
    public const string DefaultTargetFrameworkVarName = "DEFAULT_TARGET_FRAMEWORK";
    public const string DefaultDotnetIdVarName = "DEFAULT_DOTNET_ID";

    public IConfigurationRoot Configuration { get; init; }

    public WorkloadTestSettings(string testSettingsPath)
    {
        var builder = new ConfigurationBuilder()
                        .AddInMemoryCollection(ReadTestSettings(testSettingsPath))
                        .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    public string? GetValue(string varName){
        Console.WriteLine ($"GetValue: {varName}");
        return Configuration.GetValue<string?>(varName, null);
    }

    public string? GetFilePath(string varName, bool throwIfFileDoesNotExist = true)
    {
        string? path = Configuration.GetValue<string>(varName);
        if (!string.IsNullOrEmpty(path) && !File.Exists(path) && throwIfFileDoesNotExist)
            throw new InvalidDataException($"Could not find file for {varName} at '{path}'.");

        return path;
    }

    public string GetDirectoryPath(string varName)
    {
        string? path = Configuration.GetValue<string>(varName);
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            throw new InvalidDataException($"Could not find directory for {varName} at '{path}'.");

        return path;
    }

    private static Dictionary<string, string?> ReadTestSettings(string settingsPath)
    {
        if (!File.Exists(settingsPath))
            throw new FileNotFoundException($"Cannot find test settings file at {settingsPath}");

        Dictionary<string, string?> dict = new();
        foreach (string line in File.ReadAllLines(settingsPath))
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            string[] kvp = line.Split('=', 2);
            string key = kvp[0];
            string? value = kvp.Length > 1 ? kvp[1] : null;

            dict[key] = value;
        }

        return dict;
    }
}
