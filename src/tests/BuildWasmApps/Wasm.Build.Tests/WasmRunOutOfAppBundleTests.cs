// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmRunOutOfAppBundleTests : BuildTestBase
{
    public WasmRunOutOfAppBundleTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
    {}

    [Theory]
    [BuildAndRun]
    public void RunOutOfAppBundle(BuildArgs buildArgs, RunHost host, string id)
    {
        buildArgs = buildArgs with { ProjectName = $"outofappbundle_{buildArgs.Config}_{buildArgs.AOT}" };
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                            DotnetWasmFromRuntimePack: !(buildArgs.AOT || buildArgs.Config == "Release")));

        string binDir = GetBinDir(baseDir: _projectDir!, config: buildArgs.Config);
        string appBundleDir = Path.Combine(binDir, "AppBundle");
        string outerDir = Path.GetFullPath(Path.Combine(appBundleDir, ".."));

        if (host is RunHost.Chrome)
        {
            string indexHtmlPath = Path.Combine(appBundleDir, "index.html");
            // Delete the original one, so we don't use that by accident
            if (File.Exists(indexHtmlPath))
                File.Delete(indexHtmlPath);

            indexHtmlPath = Path.Combine(outerDir, "index.html");
            if (!File.Exists(indexHtmlPath))
            {
                var html = @"<html><body><script type=""module"" src=""./AppBundle/test-main.js""></script></body></html>";
                File.WriteAllText(indexHtmlPath, html);
            }
        }

        RunAndTestWasmApp(buildArgs,
                            expectedExitCode: 42,
                            host: host,
                            id: id,
                            bundleDir: outerDir,
                            jsRelativePath: "./AppBundle/test-main.js");
    }
}
