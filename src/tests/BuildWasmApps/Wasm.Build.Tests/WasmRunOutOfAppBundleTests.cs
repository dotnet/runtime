// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmRunOutOfAppBundleTests : BuildTestBase
    {
        public WasmRunOutOfAppBundleTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {}

        [Theory]
        [BuildAndRun(aot: false, host: RunHost.V8)]
        [BuildAndRun(aot: true, host: RunHost.V8)]
        [BuildAndRun(aot: false, host: RunHost.Chrome)]
        [BuildAndRun(aot: true, host: RunHost.Chrome)]
        [BuildAndRun(aot: false, host: RunHost.NodeJS)]
        [BuildAndRun(aot: true, host: RunHost.NodeJS)]
        public void RunOutOfAppBundle(BuildArgs buildArgs, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"outofappbundle_{buildArgs.Config}_{buildArgs.AOT}" };
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                DotnetWasmFromRuntimePack: !(buildArgs.AOT || buildArgs.Config == "Release"),
                                UseCache: false));

            string binDir = GetBinDir(baseDir: _projectDir!, config: buildArgs.Config);
            string baseBundleDir = Path.Combine(binDir, "AppBundle");
            string tmpBundleDir = Path.Combine(binDir, "AppBundleTmp");
            string deepBundleDir = Path.Combine(baseBundleDir, "AppBundle");

            Directory.Move(baseBundleDir, tmpBundleDir);
            Directory.CreateDirectory(baseBundleDir);

            // Create $binDir/AppBundle/AppBundle
            Directory.Move(tmpBundleDir, deepBundleDir);

            if (host == RunHost.Chrome)
            {
                string indexHtmlPath = Path.Combine(baseBundleDir, "index.html");
                if (!File.Exists(indexHtmlPath))
                {
                    var html = @"<html><body><script type=""module"" src=""AppBundle/test-main.js""></script></body></html>";
                    File.WriteAllText(indexHtmlPath, html);
                }
            }

            RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id, extraXHarnessMonoArgs: "--deep-work-dir");
        }
    }
}
