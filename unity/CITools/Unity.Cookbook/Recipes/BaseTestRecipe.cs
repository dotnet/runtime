// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Attributes;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Platforms;
using Unity.Cookbook.Modules;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public abstract class BaseTestRecipe : RecipeBase
{
    private readonly DesktopPlatformSet _platformSet;

    public BaseTestRecipe(DesktopPlatformSet platformSet)
    {
        _platformSet = platformSet;
    }

    protected abstract string DisplayName { get; }

    protected abstract string BuildScriptTestArgumentValue { get; }

    protected override ISet<Job> LoadJobs()
    {
        var jobs = Data.StandardJobConfigurations
            .Select(CreateTestJob)
            .ToArray();

        return jobs
            .Append(this.AllJob($"All {DisplayName} Tests", jobs))
            .ToHashSet();
    }

    Job CreateTestJob(JobConfiguration configuration)
        => CreateTestJob(_platformSet.Platforms[configuration.SystemType], configuration.Architecture, configuration.Configuration);

    Job CreateTestJob(Platform platform, Architecture architecture, Configuration configuration)
    {
        return JobBuilder.Create($"Test - {DisplayName} - {platform.System.JobDisplayName()} {architecture} {configuration}")
            .WithAgent(platform.Agent)
            .WithCommands(
                $"{Utils.BuildScriptForPlatform(platform)} --arch={architecture} --config={configuration} --test={BuildScriptTestArgumentValue}")
            .WithTags(platform.System.ToString())
            // The arm64 tests are currently broken and should not be included by the top level test jobs
            .WithConditional(
                architecture != Architecture.arm64,
                b => b
                    .WithTags(GroupingTag.TestJob),
                b => b)
            .WithDependencies(new Dependency(nameof(Build), platform.BuildJobName(architecture, configuration)))
            .Build();
    }
}
