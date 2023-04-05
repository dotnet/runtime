// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;

namespace Unity.Cookbook.Modules;

static class MiscModule
{
    public static Job AllJob(this IRecipe parent, string name, params Job[] jobs)
        => JobBuilder.Create(name)
            .WithDependencies(jobs.ToDependencies(parent))
            .Build();

    public static IJobBuilder WithBuildArtifacts(this IJobBuilder builder)
        => builder
            .WithArtifact(new Artifact("7z-archives", "artifacts\\unity\\**"))
            .WithArtifact(new Artifact("build", "artifacts\\bin\\**"));
}
