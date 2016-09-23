using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;

using static Microsoft.DotNet.Cli.Build.FS;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using static Microsoft.DotNet.Cli.Build.Utils;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.Host.Build
{
    public class TestTargets
    {
        public static readonly string[] TestProjects = new[]
        {
            "HostActivationTests",
            "Microsoft.Extensions.DependencyModel.Tests"
        };

        [Target(
            nameof(PrepareTargets.Init),
            nameof(RestoreTestAssets),
            nameof(RestoreTests),
            nameof(BuildTests),
            nameof(RunTests))]
        public static BuildTargetResult Test(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult RestoreTestAssets(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;
            CleanBinObj(c, Path.Combine(Dirs.RepoRoot, "TestAssets"));

            dotnet.Restore(
                    "--fallbacksource", Dirs.CorehostLocalPackages,
                    "--fallbacksource", Dirs.CorehostDummyPackages,
                    "--disable-parallel")
                .WorkingDirectory(Path.Combine(Dirs.RepoRoot, "TestAssets"))
                .Execute()
                .EnsureSuccessful();

            return c.Success();
        }

        [Target]
        public static BuildTargetResult RestoreTests(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;
            CleanBinObj(c, Path.Combine(Dirs.RepoRoot, "test"));

            dotnet.Restore("--disable-parallel")
                .WorkingDirectory(Path.Combine(Dirs.RepoRoot, "test"))
                .Execute()
                .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        public static BuildTargetResult BuildTests(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;

            var configuration = c.BuildContext.Get<string>("Configuration");

            foreach (var testProject in TestProjects)
            {
                c.Info($"Building tests: {testProject}");
                dotnet.Build("--configuration", configuration)
                    .WorkingDirectory(Path.Combine(Dirs.RepoRoot, "test", testProject))
                    .Execute()
                    .EnsureSuccessful();
            }
            return c.Success();
        }

        [Target]
        public static BuildTargetResult RunTests(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;

            var configuration = c.BuildContext.Get<string>("Configuration");
            
            var failingTests = RunDotnetTestOnTestProjects(c, dotnet, configuration);
            if (failingTests.Any())
            {
                foreach (var project in failingTests)
                {
                    c.Error($"{project} failed");
                }
                return c.Failed("Tests failed!");
            }

            return c.Success();
        }

        private static List<string> RunDotnetTestOnTestProjects(BuildTargetContext c, DotNetCli dotnet, string configuration)
        {
            var failingTests = new List<string>();

            foreach (var project in TestProjects)
            {
                c.Info($"Running tests in: {project}");

                var result = dotnet.Test("--configuration", configuration, "-xml", $"{project}-testResults.xml", "-notrait", "category=failing")
                    .WorkingDirectory(Path.Combine(Dirs.RepoRoot, "test", project))
                    .EnvironmentVariable("PATH", $"{dotnet.BinPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}")
                    .EnvironmentVariable("TEST_ARTIFACTS", Dirs.TestArtifacts)
                    .Execute();

                if (result.ExitCode != 0)
                {
                    failingTests.Add(project);
                }
            }

            return failingTests;
        }
    }
}
