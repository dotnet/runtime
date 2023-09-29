// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Templates.Tests
{
    public class NativeBuildTests : WasmTemplateTestBase
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

            CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
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
        public void ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(string config)
        {
            string id = $"dllimport_incompatible_{GetRandomId()}";
            string projectPath = CreateWasmTemplateProject(id, template: "wasmconsole");

            string nativeSourceFilename = "incompatible_type.c";
            string nativeCode = "void call_needing_marhsal_ilgen(void *x) {}";
            File.WriteAllText(path: Path.Combine(_projectDir!, nativeSourceFilename), nativeCode);

            AddItemsPropertiesToProject(
                projectPath,
                extraItems: "<NativeFileReference Include=\"" + nativeSourceFilename + "\" />"
            );

            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "marshal_ilgen_test.cs"),
                                    Path.Combine(_projectDir!, "Program.cs"),
                                    overwrite: true);

            CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("build", $"-c {config} -bl");

            Assert.True(result.ExitCode == 0, "Expected build to succeed");

            CommandResult res = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!)
                                        .ExecuteWithCapturedOutput($"run --no-silent --no-build -c {config}")
                                        .EnsureSuccessful();
            Assert.Contains("Hello, Console!", res.Output);
            Assert.Contains("Hello, World! Greetings from node version", res.Output);
        }
    }
}
