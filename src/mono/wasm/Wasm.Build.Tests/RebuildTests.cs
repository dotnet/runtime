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
        [BuildAndRun(aot: false, config: Configuration.Debug)]
        public async Task NoOpRebuild(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild");
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);
            PublishProject(info, config);

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun", ExpectedExitCode: 42);
            await RunForPublishWithWebServer(runOptions);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));

            // artificial delay to have new enough timestamps
            await Task.Delay(5000);

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");

            // no-op Rebuild
            PublishProject(info, config, new PublishOptions(UseCache: false));
            await RunForPublishWithWebServer(runOptions);
        }
    }
}
