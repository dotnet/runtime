// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BlazorWasmTests : BuildTestBase
    {
        public BlazorWasmTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        // TODO: invariant case?

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug", false)]
        [InlineData("Debug", true)] // just aot
        [InlineData("Release", false)] // should re-link
        [InlineData("Release", true)]
        public void PublishTemplateProject(string config, bool aot)
        {
            string id = $"blazorwasm_{config}_aot_{aot}";
            InitPaths(id);
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
            Directory.CreateDirectory(_projectDir);
            Directory.CreateDirectory(Path.Combine(_projectDir, ".nuget"));

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "nuget6.config"), Path.Combine(_projectDir, "nuget.config"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id);

            new DotNetCommand(s_buildEnv, useDefaultArgs: false)
                    .WithWorkingDirectory(_projectDir)
                    .ExecuteWithCapturedOutput("new blazorwasm")
                    .EnsureSuccessful();

            string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
            new DotNetCommand(s_buildEnv)
                    .WithWorkingDirectory(_projectDir)
                    .ExecuteWithCapturedOutput("publish", $"-bl:{publishLogPath}", aot ? "-p:RunAOTCompilation=true" : "", $"-p:Configuration={config}")
                    .EnsureSuccessful();

            //TODO: validate the build somehow?
            // compare dotnet.wasm?
            // relinking - dotnet.wasm should be smaller
            //
            // playwright?
        }

        public static TheoryData<string, bool, bool> Net50TestData = new()
        {
            { "Debug", /*aot*/ true, /*expectError*/ true },
            { "Debug", /*aot*/ false, /*expectError*/ false },
            { "Release", /*aot*/ true, /*expectError*/ true },
            { "Release", /*aot*/ false, /*expectError*/ false }
        };

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsNotUsingWorkloads))]
        [MemberData(nameof(Net50TestData))]
        public void Net50ProjectsWithNoPacksInstalled(string config, bool aot, bool expectError)
            => BuildNet50Project(config, aot, expectError);

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [MemberData(nameof(Net50TestData))]
        public void Net50ProjectsWithPacksInstalled(string config, bool aot, bool expectError)
            => BuildNet50Project(config, aot, expectError);

        private void BuildNet50Project(string config, bool aot, bool errorExpected)
        {
            string id = $"Blazor_net50_{config}_{aot}";
            InitPaths(id);
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
            Directory.CreateDirectory(_projectDir);
            Directory.CreateDirectory(Path.Combine(_projectDir, ".nuget"));

            string directoryBuildTargets = @"<Project>
                <Target Name=""PrintAllProjects"" BeforeTargets=""Build"">
                    <Message Text=""** UsingBrowserRuntimeWorkload: '$(UsingBrowserRuntimeWorkload)'"" Importance=""High"" />
                </Target>
            </Project>";

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "nuget6.config"), Path.Combine(_projectDir, "nuget.config"));
            File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(_projectDir, "Directory.Build.targets"), directoryBuildTargets);

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id);
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "Blazor_net50"), Path.Combine(_projectDir!));

            string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
            CommandResult result = new DotNetCommand(s_buildEnv)
                                            .WithWorkingDirectory(_projectDir)
                                            .ExecuteWithCapturedOutput("publish",
                                                                       $"-bl:{publishLogPath}",
                                                                       (aot ? "-p:RunAOTCompilation=true" : ""),
                                                                       $"-p:Configuration={config}");

            if (errorExpected)
            {
                result.EnsureExitCode(1);
                Assert.Contains("** UsingBrowserRuntimeWorkload: 'false'", result.Output);
                Assert.Contains("error : WebAssembly workloads (required for AOT) are only supported for projects targeting net6.0+", result.Output);
            }
            else
            {
                result.EnsureSuccessful();
                Assert.Contains("** UsingBrowserRuntimeWorkload: 'false'", result.Output);
            }
        }
    }
}
