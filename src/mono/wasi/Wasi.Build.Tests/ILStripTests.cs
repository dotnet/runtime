// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;
using Wasm.Build.Tests;

#nullable enable

namespace Wasi.Build.Tests;

public class ILStripTests : BuildTestBase
{
    public ILStripTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory]
    [InlineData("", /*expectILStripping*/ true, /*singleFileBundle*/false)] // Default case
    [InlineData("", /*expectILStripping*/ true, /*singleFileBundle*/true)] // Default case
    [InlineData("false", /*expectILStripping*/ false, /*singleFileBundle*/false)] // the opposite of the default case
    [InlineData("false", /*expectILStripping*/ false, /*singleFileBundle*/true)] // the opposite of the default case
    public void WasmStripILAfterAOT_TestDefaultAndOverride(string stripILAfterAOT, bool expectILStripping, bool singleFileBundle)
    {
        string config = "Release";
        string id = $"{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
        string projectName = Path.GetFileNameWithoutExtension(projectFile);

        string extraProperties = "<RunAOTCompilation>true</RunAOTCompilation>";
        if (singleFileBundle)
            extraProperties += "<WasmSingleFileBundle>true</WasmSingleFileBundle>";
        if (!string.IsNullOrEmpty(stripILAfterAOT))
            extraProperties += $"<WasmStripILAfterAOT>{stripILAfterAOT}</WasmStripILAfterAOT>";
        AddItemsPropertiesToProject(projectFile, extraProperties);

        var buildArgs = new BuildArgs(projectName, config, AOT: true, ProjectFileContents: id, ExtraBuildArgs: null);
        buildArgs = ExpandBuildArgs(buildArgs);

        BuildProject(buildArgs,
                    id: id,
                    new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: false,
                        CreateProject: false,
                        Publish: true,
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        UseCache: false));

        string runArgs = $"run --no-silent --no-build -c {config}";
        new RunCommand(s_buildEnv, _testOutput, label: id)
                .WithWorkingDirectory(_projectDir!)
                .ExecuteWithCapturedOutput(runArgs)
                .EnsureSuccessful();

        string frameworkDir = singleFileBundle ? "" : Path.Combine(_projectDir!, "bin", config, DefaultTargetFramework, "wasi-wasm", "AppBundle", "managed");
        string objBuildDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFramework, "wasi-wasm", "wasm", "for-publish");
        TestWasmStripILAfterAOTOutput(objBuildDir, frameworkDir, expectILStripping, singleFileBundle, _testOutput);
    }

    private static void TestWasmStripILAfterAOTOutput(string objBuildDir, string appBundleFrameworkDir, bool expectILStripping, bool singleFileBundle, ITestOutputHelper testOutput)
    {
        string origAssemblyDir = Path.Combine(objBuildDir, "aot-in");
        string strippedAssemblyDir = Path.Combine(objBuildDir, "stripped");
        Assert.True(Directory.Exists(origAssemblyDir), $"Could not find the original AOT input assemblies dir: {origAssemblyDir}");
        if (expectILStripping)
            Assert.True(Directory.Exists(strippedAssemblyDir), $"Could not find the stripped assemblies dir: {strippedAssemblyDir}");
        else
            Assert.False(Directory.Exists(strippedAssemblyDir), $"Expected {strippedAssemblyDir} to not exist");

        string assemblyToExamine = "System.Private.CoreLib.dll";
        string originalAssembly = Path.Combine(objBuildDir, origAssemblyDir, assemblyToExamine);
        string strippedAssembly = Path.Combine(objBuildDir, strippedAssemblyDir, assemblyToExamine);
        string includedAssembly = Path.Combine(appBundleFrameworkDir, assemblyToExamine);

        Assert.True(File.Exists(originalAssembly), $"Expected {nameof(originalAssembly)} {originalAssembly} to exist");
        if (!singleFileBundle)
            Assert.True(File.Exists(includedAssembly), $"Expected {nameof(includedAssembly)} {includedAssembly} to exist");
        if (expectILStripping)
            Assert.True(File.Exists(strippedAssembly), $"Expected {nameof(strippedAssembly)} {strippedAssembly} to exist");
        else
            Assert.False(File.Exists(strippedAssembly), $"Expected {strippedAssembly} to not exist");

        string compressedOriginalAssembly = Utils.GZipCompress(originalAssembly);
        string? compressedIncludedAssembly = null;
        FileInfo compressedOriginalAssembly_fi = new FileInfo(compressedOriginalAssembly);
        FileInfo? compressedincludedAssembly_fi = null;

        testOutput.WriteLine ($"compressedOriginalAssembly_fi: {compressedOriginalAssembly_fi.Length}, {compressedOriginalAssembly}");
        if (!singleFileBundle)
        {
            compressedIncludedAssembly = Utils.GZipCompress(includedAssembly)!;
            compressedincludedAssembly_fi = new FileInfo(compressedIncludedAssembly);
            testOutput.WriteLine ($"compressedincludedAssembly_fi: {compressedincludedAssembly_fi.Length}, {compressedIncludedAssembly}");
        }

        if (expectILStripping)
        {
            string compressedStrippedAssembly = Utils.GZipCompress(strippedAssembly);
            FileInfo compressedStrippedAssembly_fi = new FileInfo(compressedStrippedAssembly);
            testOutput.WriteLine ($"compressedStrippedAssembly_fi: {compressedStrippedAssembly_fi.Length}, {compressedStrippedAssembly}");

            // original > stripped assembly
            Assert.True(compressedOriginalAssembly_fi.Length > compressedStrippedAssembly_fi.Length,
                        $"Expected original assembly ({compressedOriginalAssembly}) size ({compressedOriginalAssembly_fi.Length}) " +
                        $"to be bigger than the stripped assembly ({compressedStrippedAssembly}) size ({compressedStrippedAssembly_fi.Length})");

            if (!singleFileBundle)
            {
                // included == stripped assembly
                Assert.True(compressedincludedAssembly_fi!.Length == compressedStrippedAssembly_fi.Length,
                            $"Expected included assembly ({compressedIncludedAssembly}) size ({compressedincludedAssembly_fi.Length}) " +
                            $"to be the same as the stripped assembly ({compressedStrippedAssembly}) size ({compressedStrippedAssembly_fi.Length})");
            }
        }
        else
        {
            if (!singleFileBundle)
            {
                // original == included assembly
                Assert.True(compressedincludedAssembly_fi!.Length == compressedOriginalAssembly_fi.Length,
                            $"Expected included assembly ({compressedIncludedAssembly}) size ({compressedincludedAssembly_fi.Length}) " +
                            $"to be the same as the original assembly ({compressedOriginalAssembly}) size ({compressedOriginalAssembly_fi.Length})");
            }
        }
    }
}
