// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Finders;

namespace Unity.Cookbook.Recipes;

public class AllTests : AggregateRecipeBase
{
    public const string BuildPublishedJobName = "Build Published";
    readonly IFinder _finder;

    public AllTests(IFinder finder)
    {
        _finder = finder;
    }

    protected override ISet<Job> LoadJobs()
    {
        var platformJobs = new Job[]
        {
            CreateTestingGroupingJob("All Windows Tests", SystemType.Windows.ToString()),
            CreateTestingGroupingJob("All Linux Tests", SystemType.Ubuntu.ToString()),
            CreateTestingGroupingJob("All OSX Tests", SystemType.MacOS.ToString()),
        };

        var buildPublishingJobs = new Job[]
        {
            CreateBuildGroupingJob(BuildPublishedJobName, Configuration.Release.ToString()),
        };

        var all = new List<Job>();
        all.AddRange(platformJobs);
        all.AddRange(buildPublishingJobs);

        all.Add(
            JobBuilder.Create("All CoreCLR Tests")
                .WithDependencies(platformJobs.ToDependencies(this))
                .WithDependencies(new Dependency(nameof(RecipeEngine), RecipeEngine.JobName))
                .WithPullRequestTrigger(pr =>
                    pr.ExcludeDraft().And()
                        .WithTargetBranch(GlobalSettings.BaseBranchName))
                .Build());

        return all.ToHashSet();
    }

    protected virtual Job CreateTestingGroupingJob(string jobName, string platformTag)
    {
        return JobBuilder.Create(jobName)
            .WithDependencies(_finder
                .Find(job =>
                {
                    if (!job.Tags.Contains(GroupingTag.TestJob) || !job.Tags.Contains(platformTag))
                        return false;

                    if (job.Tags.Contains(GroupingTag.ExcludeFromTesting))
                        return false;

                    return true;
                }))
            .Build();
    }

    protected virtual Job CreateBuildGroupingJob(string jobName, string tag)
    {
        return JobBuilder.Create(jobName)
            .WithDependencies(_finder
                .Find(job =>
                {
                    if (!job.Tags.Contains(GroupingTag.BuildJob) || !job.Tags.Contains(tag))
                        return false;

                    if (job.Tags.Contains(GroupingTag.ExcludeFromPublishing))
                        return false;

                    return true;
                }))
            .Build();
    }
}
