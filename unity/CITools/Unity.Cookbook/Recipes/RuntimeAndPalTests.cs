// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class RuntimeAndPalTests : BaseTestRecipe
{
    public RuntimeAndPalTests(DesktopPlatformSet platformSet)
    : base(platformSet)
    {
    }

    protected override string DisplayName => "Runtime & Pal";

    protected override string BuildScriptTestArgumentValue => "runtime,pal";

    protected override Job CreateTestJob(Platform platform, Architecture architecture, Configuration configuration, bool excludeFromTesting)
    {
        string testArgs = platform.System == SystemType.MacOS ? "runtime" : BuildScriptTestArgumentValue;

        return JobBuilder.Create($"Test - {DisplayName} - {platform.System.JobDisplayName()} {architecture} {configuration}")
            .WithAgent(platform.Agent)
            .WithCommands(
                $"{Utils.BuildScriptForPlatform(platform)} --arch={architecture} --config={configuration} --test={testArgs}")
            .WithTags(platform.System.ToString())
            .WithTags(GroupingTag.TestJob)
            .WithConditional(excludeFromTesting,
                b => b
                    .WithTags(GroupingTag.ExcludeFromTesting),
                b => b)
            .WithDependencies(new Dependency(nameof(Build), platform.BuildJobName(architecture, configuration)))
            .Build();
    }
}
