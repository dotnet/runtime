// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Api.Triggers.Recurring;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook.Recipes;

public class Update : RecipeBase
{
    private readonly DesktopPlatformSet _platformSet;

    public Update(DesktopPlatformSet platformSet)
    {
        _platformSet = platformSet;
    }

    protected override ISet<Job> LoadJobs()
    {
        return new[]
        {
            JobBuilder.Create("Update from upstream")
                .WithAgent(_platformSet.Platforms[SystemType.Ubuntu].Agent)
                .WithCommands(
                    "curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg",
                    " echo \"deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main\" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null",
                    "sudo apt update",
                    "sudo apt install gh",
                    "git config user.email \"dotnet-vm-team-devs@unity3d.com\"",
                    "git config user.name \"Unity coreclr Bot\"",
                    ".yamato/scripts/update_from_upstream.sh"
                    )
                .WithScheduleTrigger(new Schedule(GlobalSettings.BaseBranchName, "weekly"))
                .Build()
        }.ToHashSet();
    }
}
