// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NiceIO;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Platforms;
using Unity.Cookbook.Modules;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class Build  : RecipeBase
{
    public const string BuildAllJobName = "Build All";

    private readonly DesktopPlatformSet _platformSet;

    public Build(DesktopPlatformSet platformSet)
    {
        _platformSet = platformSet;
    }

    protected override ISet<Job> LoadJobs()
    {
        var jobs = Data.StandardJobConfigurations
            .Select(CreateBuildJob)
            .Append(CreateFatOSXBuild())
            .ToArray();

        return jobs
            .Append(this.AllJob(BuildAllJobName, jobs))
            .ToHashSet();
    }

    Job CreateBuildJob(JobConfiguration configuration)
        => CreateBuildJob(_platformSet.Platforms[configuration.SystemType], configuration.Architecture, configuration.Configuration, configuration.ExcludeFromPublishing);

    static Job CreateBuildJob(Platform platform, Architecture architecture, Configuration configuration, bool excludeFromPublishing)
    {
        return JobBuilder.Create(platform.BuildJobName(architecture, configuration))
            .WithAgent(platform.Agent)
            .WithCommands(
                $"{Utils.BuildScriptForPlatform(platform)} --arch={architecture} --config={configuration}")
            .WithEnvironmentVariable("ARTIFACT_FILENAME", platform.ArtifactFileName(architecture))
            .WithBuildArtifacts()
            .WithTags(GroupingTag.BuildJob, configuration.ToString(), platform.System.ToString())
            .WithConditional(excludeFromPublishing, b =>
            {
                return b.WithTags(GroupingTag.ExcludeFromPublishing);
            },
                b => b)
            .Build();
    }

    Job CreateFatOSXBuild()
    {
        var configuration = Configuration.Release;
        var platform = _platformSet.Platforms[SystemType.MacOS];
        return JobBuilder.Create($"Generate {platform.System.JobDisplayName()} x64+arm64")
            .WithAgent(platform.Agent)
            .WithBuildArtifacts()
            .WithCommands(Utils.BuildScriptForPlatform(platform).ToNPath().Parent.Combine("generate_osx_x64_arm64.sh").ToString(SlashMode.Forward))
            .WithDependencies(
                new Dependency(this, platform.BuildJobName(Architecture.x64, configuration)),
                new Dependency(this, platform.BuildJobName(Architecture.arm64, configuration)))
            .WithTags(GroupingTag.BuildJob, configuration.ToString(), platform.System.ToString())
            .WithEnvironmentVariable("ARTIFACT_FILENAME", platform.ArtifactFileName("x64arm64"))
            .Build();
    }
}
