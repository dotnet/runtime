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
            string config = "Release";
            string code = @"
                using System;
                using System.Runtime.InteropServices;

                call();
                return 42;

                [DllImport(""undefined_xyz"")] static extern void call();
            ";

            string extraItems = @$"<NativeFileReference Include=""undefined_xyz.c"" />";
            string extraProperties = allowUndefined ? $"<WasmAllowUndefinedSymbols>true</WasmAllowUndefinedSymbols>" : "";
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot: false,
                $"UndefinedNativeSymbol_{(allowUndefined ? "allowed" : "disabled")}",
                extraItems: extraItems,
                extraProperties: extraProperties
            );
            UpdateFile("Program.cs", code);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "undefined-symbol.c"), Path.Combine(_projectDir!, "undefined_xyz.c"));

            bool isPublish = false;
            (string _, string buildOutput) = BuildTemplateProject(info,
                new BuildProjectOptions(
                    config,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                    ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                    IsPublish: isPublish,
                    ExpectSuccess: allowUndefined
            ));

            if (!allowUndefined)
            {
                Assert.Contains("undefined symbol: sgfg", buildOutput);
                Assert.Contains("Use '-p:WasmAllowUndefinedSymbols=true' to allow undefined symbols", buildOutput);
            }
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public async Task ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(string config)
        {
            string nativeSourceFilename = "incompatible_type.c";
            string extraItems = "<NativeFileReference Include=\"" + nativeSourceFilename + "\" />";
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot: false,
                "dllimport_incompatible",
                extraItems: extraItems
            );
            string nativeCode = "void call_needing_marhsal_ilgen(void *x) {}";
            File.WriteAllText(path: Path.Combine(_projectDir!, nativeSourceFilename), nativeCode);
            UpdateBrowserMainJs();
            ReplaceFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "marshal_ilgen_test.cs"));

            bool isPublish = false;
            (string _, string buildOutput) = BuildTemplateProject(info,
                new BuildProjectOptions(
                    config,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                    ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                    IsPublish: isPublish,
                    AssertAppBundle: false
            ));

            var runOutput = await RunForBuildWithDotnetRun(new(info.Configuration, ExpectedExitCode: 42));
            Assert.Contains("call_needing_marhsal_ilgen got called", runOutput.TestOutput);
        }
    }
}
