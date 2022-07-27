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
        string tmpBundleDirName = "AppBundleTmp";
        string tmpBundleDir = Path.Combine(binDir, tmpBundleDirName);

        if (host == RunHost.Chrome)
        {
            Directory.Move(appBundleDir, tmpBundleDir);
            Directory.CreateDirectory(appBundleDir);
            // Create $binDir/AppBundle/AppBundle
            Directory.Move(tmpBundleDir, Path.Combine(appBundleDir, "AppBundle"));

            string indexHtmlPath = Path.Combine(appBundleDir, "index.html");
            if (!File.Exists(indexHtmlPath))
            {
                var html = @"<html><body><script type=""module"" src=""./AppBundle/test-main.js""></script></body></html>";
                File.WriteAllText(indexHtmlPath, html);
            }
        } else {
            CopyAllFiles(appBundleDir, tmpBundleDir);
        }

        RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id, jsRelativePath: $"../{tmpBundleDirName}/test-main.js");

        // Restore AppBundle Dir
        if (host == RunHost.Chrome)
        {
            Directory.Move(Path.Combine(appBundleDir, "AppBundle"), tmpBundleDir);
            Directory.Delete(appBundleDir, true);
            Directory.Move(tmpBundleDir, appBundleDir);
        } else {
            Directory.Delete(tmpBundleDir, true);
        }
    }

    private void CopyAllFiles(string srcDir, string destDir)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        foreach (var file in Directory.GetFiles(srcDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }

        foreach (var directory in Directory.GetDirectories(srcDir))
        {
            CopyAllFiles(directory, Path.Combine(destDir, Path.GetFileName(directory)));
        }
    }
}
