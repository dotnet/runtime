// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class Publish : RecipeBase
{
    private readonly DesktopPlatformSet _platformSet;

    public Publish(DesktopPlatformSet platformSet)
    {
        _platformSet = platformSet;
    }

    protected override ISet<Job> LoadJobs()
    {
        return new []
        {
            CreatePublishJob("public"),
            CreatePublishJob("testing")
        }.ToHashSet();
    }

    Job CreatePublishJob(string stevedoreTarget)
    {
        return JobBuilder.Create($"Publish To Stevedore ({stevedoreTarget})")
            .WithAgent(_platformSet.Platforms[SystemType.Ubuntu].Agent)
            .WithCommands(
                "curl -sSo StevedoreUpload \"$STEVEDORE_UPLOAD_TOOL_LINUX_X64_URL\"",
                "chmod +x StevedoreUpload",
                $"./StevedoreUpload --version-len=12 --repo={stevedoreTarget} --version=\"$GIT_REVISION\" artifacts/unity/*"
            )
            .WithDependencies(new Dependency(nameof(AllTests), AllTests.BuildPublishedJobName))
            .Build();
    }
}
