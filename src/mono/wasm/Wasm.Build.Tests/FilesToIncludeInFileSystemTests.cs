// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class FilesToIncludeInFileSystemTests : WasmTemplateTestsBase
{
    public FilesToIncludeInFileSystemTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    public static IEnumerable<object?[]> LoadFilesToVfsData()
    {
        if (!EnvironmentVariables.UseJavascriptBundler)
            yield return new object?[] { false };
        
        yield return new object?[] { true };
    }

    [Theory, TestCategory("bundler-friendly")]
    [MemberData(nameof(LoadFilesToVfsData))]
    public async Task LoadFilesToVfs(bool publish)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "FilesToIncludeInFileSystemTest");

        if (publish)
            PublishProject(info, config, new PublishOptions());
        else
            BuildProject(info, config, new BuildOptions());
        
        BrowserRunOptions runOptions = new(
            config,
            TestScenario: "FilesToIncludeInFileSystemTest"
        );
        RunResult result = publish
            ? await RunForPublishWithWebServer(runOptions)
            : await RunForBuildWithDotnetRun(runOptions);

        Assert.Contains(result.TestOutput, m => m.Contains("'/myfiles/Vfs1.txt' exists 'True' with content 'Vfs1.txt'"));
        Assert.Contains(result.TestOutput, m => m.Contains("'/myfiles/Vfs2.txt' exists 'True' with content 'Vfs2.txt'"));
        Assert.Contains(result.TestOutput, m => m.Contains("'/subdir/subsubdir/Vfs3.txt' exists 'True' with content 'Vfs3.txt'"));
    }
}
