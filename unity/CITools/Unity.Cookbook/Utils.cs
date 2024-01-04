// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;

namespace Unity.Cookbook;

static class Utils
{
    public static string BuildScriptForPlatform(Platform platform)
    {
        const string name = "build_yamato";
        var extension = platform.RunsOnWindows() ? "cmd" : "sh";
        return $".yamato/scripts/{name}.{extension}";
    }

    public static string JobDisplayName(this SystemType system)
    {
        switch (system)
        {
            case SystemType.Ubuntu:
                return "Linux";
            case SystemType.MacOS:
                return "OSX";
            default:
                return system.ToString();
        }
    }

    public static string BuildJobName(this Platform platform, Architecture architecture, Configuration configuration)
        => $"Build {platform.System.JobDisplayName()} {architecture} {configuration}";

    public static string ArtifactFileName(this Platform platform, Architecture architecture)
        => platform.ArtifactFileName(architecture.ToString());

    public static string ArtifactFileName(this Platform platform, string postFix)
        => $"{GlobalSettings.BaseArtifactName}-{platform.System.JobDisplayName().ToLower()}-{postFix}.7z";
}
