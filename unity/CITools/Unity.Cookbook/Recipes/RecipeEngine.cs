// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Platforms;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class RecipeEngine: RecipeBase
{
    public const string JobName = "Yamato Files Update-to-Date";

    private readonly DesktopPlatformSet _platformSet;

    public RecipeEngine(DesktopPlatformSet platformSet)
    {
        _platformSet = platformSet;
    }

    protected override ISet<Job> LoadJobs()
    {
        var platform = _platformSet.MacOS;
        var dotnetExecutable = platform.RunsOnWindows() ? "dotnet.cmd" : "dotnet.sh";
        return new[]
        {
            JobBuilder.Create(JobName)
                .WithAgent(platform.Agent with { Flavor = FlavorType.BuildSmall})
                .WithBlockCommand(block =>
                    block
                        .WithLine("cd unity/CITools")
                        .WithLine($"../../{dotnetExecutable} run --project Unity.Cookbook/Unity.Cookbook.csproj")
                        .WithLine($"../../{dotnetExecutable} run --project CheckYamatoFiles/CheckYamatoFiles.csproj"))
                .Build()
        }.ToHashSet();
    }
}
