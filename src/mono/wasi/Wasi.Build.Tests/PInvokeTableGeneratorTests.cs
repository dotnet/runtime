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

    [Theory]
    [MemberData(nameof(TestDataForConsolePublishAndRunRelease))]
    public void InteropSupportForUnmanagedEntryPointWithoutDelegate(bool singleFileBundle, bool aot)
    {
        if (aot)
        {
            // Active issue https://github.com/dotnet/runtime/issues/101276
            return;
        }

        string id = $"Release_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "Native.cs"), Path.Combine(_projectDir!, "Program.cs"), true);
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native.c"), Path.Combine(_projectDir!, "native.c")!, true);
        
        // workaround for https://github.com/dotnet/runtime/issues/106627
        File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "ILLink.Native.Descriptors.xml"), Path.Combine(_projectDir!, "ILLink.Native.Descriptors.xml")!, true);

        string extraProperties = @"<WasmNativeStrip>false</WasmNativeStrip>
                                   <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                                   <PublishTrimmed>true</PublishTrimmed>
                                   <AssemblyName>Wasi.Native.Test</AssemblyName>";
        if (aot)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation><_WasmDevel>false</_WasmDevel>";
        if (singleFileBundle)
            extraProperties += "<WasmSingleFileBundle>true</WasmSingleFileBundle>";

        string itemsProperties = @"<NativeFileReference Include=""native.c"" />
                                   <TrimmerRootDescriptor Include=""$(MSBuildThisFileDirectory)ILLink.Native.Descriptors.xml"" />";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties, extraItems: itemsProperties);
        var buildArgs = new BuildArgs(projectName, Config: "Release", AOT: true, ProjectFileContents: id, ExtraBuildArgs: null);
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
                                    .ExecuteWithCapturedOutput($"run --no-silent --no-build -c Release")
                                    .EnsureSuccessful();
        Assert.Contains("MyExport(123) -> 42", res.Output);
    }
}
