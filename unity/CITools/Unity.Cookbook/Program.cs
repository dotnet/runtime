// See https://aka.ms/new-console-template for more information

using BuildDriver;
using NiceIO;
using RecipeEngine;
using RecipeEngine.Api.Logging;
using RecipeEngine.Platforms.Loaders;
using Unity.Cookbook.Platforms;

namespace Unity.Cookbook
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var repoRoot = Paths.RepoRoot;
            Directory.SetCurrentDirectory(repoRoot);

            // Delete the existing .yml files.  RecipeEngine does not delete old files.  Therefore deleting the files is necessary
            // in order to handle renaming of a recipe or deleting of a recipe.
            repoRoot.Combine(".yamato").Files("*.yml").Delete();

            return await EngineFactory
                .Create(o =>
                {
                    o.LogLevel = LogLevel.Debug;
                })
                .WithPlatforms()
                .WithDependency<DesktopPlatformSet>()
                .ScanAll()
                .GenerateAsync();
        }
    }
}

