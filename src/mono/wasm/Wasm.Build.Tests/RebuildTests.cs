// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class RebuildTests : WasmTemplateTestsBase
    {
        public RebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false, config: "Debug")]
        public async Task NoOpRebuild(string config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "rebuild", "App");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);
            bool isPublish = true;
            BuildProject(info,
                new BuildOptions(
                    info.Configuration,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                    ExpectedFileType: GetExpectedFileType(info, isPublish),
                    IsPublish: isPublish
            ));

            RunOptions runOptions = new(info.Configuration, TestScenario: "DotnetRun", ExpectedExitCode: 42);
            await RunForPublishWithWebServer(runOptions);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));

            // artificial delay to have new enough timestamps
            await Task.Delay(5000);

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");

            // no-op Rebuild
            BuildProject(info,
                new BuildOptions(
                    info.Configuration,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                    ExpectedFileType: GetExpectedFileType(info, isPublish),
                    IsPublish: isPublish,
                    UseCache: false
            ));

            await RunForPublishWithWebServer(runOptions);
        }
    }
}
