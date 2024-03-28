// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class PInvokeTableGeneratorTests : BuildTestBase
{
    public PInvokeTableGeneratorTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Fact]
    public void InteropSupportForUnmanagedEntryPointWithoutDelegate()
    {
        string config = "Release";
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasinative");

        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        var buildArgs = new BuildArgs(projectName, config, AOT: true, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);
        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: true
                        ));

        CommandResult res = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {config}")
                                    .EnsureSuccessful();
        Assert.Contains("MyExport(123) -> 42", res.Output);
    }
}
