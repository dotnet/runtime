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
            Configuration config = Configuration.Release;
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
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "undefined-symbol.c"), Path.Combine(_projectDir, "undefined_xyz.c"));
            var buildOptions = new BuildOptions(ExpectSuccess: allowUndefined, AssertAppBundle: false);
            (string _, string buildOutput) = BuildProject(info, config, buildOptions, isNativeBuild: true);

            if (!allowUndefined)
            {
                Assert.Contains("undefined symbol: sgfg", buildOutput);
                Assert.Contains("Use '-p:WasmAllowUndefinedSymbols=true' to allow undefined symbols", buildOutput);
            }
        }

        [Theory]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public async Task ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(Configuration config)
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
            File.WriteAllText(path: Path.Combine(_projectDir, nativeSourceFilename), nativeCode);
            UpdateBrowserMainJs();
            ReplaceFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "marshal_ilgen_test.cs"));

            (string _, string buildOutput) = BuildProject(info, config, new BuildOptions(AssertAppBundle: false), isNativeBuild: true);
            var runOutput = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, ExpectedExitCode: 42));
            Assert.Contains("call_needing_marhsal_ilgen got called", runOutput.TestOutput);
        }
    }
}
