// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Templates.Tests
{
    public class NativeBuildTests : WasmTemplateTestsBase
    {
        public NativeBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BuildWithUndefinedNativeSymbol(bool allowUndefined)
        {
            string id = $"UndefinedNativeSymbol_{(allowUndefined ? "allowed" : "disabled")}_{GetRandomId()}";

            string code = @"
                using System;
                using System.Runtime.InteropServices;

                call();
                return 42;

                [DllImport(""undefined_xyz"")] static extern void call();
            ";

            string projectPath = CreateWasmTemplateProject(id);

            AddItemsPropertiesToProject(
                projectPath,
                extraItems: @$"<NativeFileReference Include=""undefined_xyz.c"" />",
                extraProperties: allowUndefined ? $"<WasmAllowUndefinedSymbols>true</WasmAllowUndefinedSymbols>" : null
            );

            File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), code);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "undefined-symbol.c"), Path.Combine(_projectDir!, "undefined_xyz.c"));

            using DotNetCommand cmd = new DotNetCommand(s_buildEnv, _testOutput);
            CommandResult result = cmd.WithWorkingDirectory(_projectDir!)
                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                    .ExecuteWithCapturedOutput("build", "-c Release");

            if (allowUndefined)
            {
                Assert.True(result.ExitCode == 0, "Expected build to succeed");
            }
            else
            {
                Assert.False(result.ExitCode == 0, "Expected build to fail");
                Assert.Contains("undefined symbol: sgfg", result.Output);
                Assert.Contains("Use '-p:WasmAllowUndefinedSymbols=true' to allow undefined symbols", result.Output);
            }
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public async Task ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(string config)
        {
            string id = $"dllimport_incompatible_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, template: "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            string nativeSourceFilename = "incompatible_type.c";
            string nativeCode = "void call_needing_marhsal_ilgen(void *x) {}";
            File.WriteAllText(path: Path.Combine(_projectDir!, nativeSourceFilename), nativeCode);

            AddItemsPropertiesToProject(
                projectFile,
                extraItems: "<NativeFileReference Include=\"" + nativeSourceFilename + "\" />"
            );

            UpdateBrowserMainJs();
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "marshal_ilgen_test.cs"),
                                    Path.Combine(_projectDir!, "Program.cs"),
                                    overwrite: true);

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);
            BuildTemplateProject(buildArgs, id: id, new BuildProjectOptions(
                                    AssertAppBundle: false,
                                    CreateProject: false,
                                    HasV8Script: false,
                                    MainJS: "main.mjs",
                                    Publish: false,
                                    TargetFramework: DefaultTargetFramework,
                                    IsBrowserProject: true)
                                );
            string runOutput = await RunBuiltBrowserApp(config, projectFile);

            Assert.Contains("call_needing_marhsal_ilgen got called", runOutput);
        }
    }
}
