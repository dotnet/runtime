// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class SatelliteAssembliesTests : BuildTestBase
    {
        public SatelliteAssembliesTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> SatelliteAssemblyTestData(bool aot, bool relinking, RunHost host)
            => ConfigWithAOTData(aot)
                    .Multiply(
                        new object?[] { relinking, "es-ES", "got: hola" },
                        new object?[] { relinking, null,    "got: hello" },
                        new object?[] { relinking, "ja-JP", "got: \u3053\u3093\u306B\u3061\u306F" })
                    .WithRunHosts(host)
                    .UnwrapItemsAsArrays();

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61725", TestPlatforms.Windows)]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false, RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true,  RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false, RunHost.All })]
        public void ResourcesFromMainAssembly(BuildArgs buildArgs,
                                              bool nativeRelink,
                                              string? argCulture,
                                              string expectedOutput,
                                              RunHost host,
                                              string id)
        {
            string projectName = $"sat_asm_from_main_asm";
            // Release+publish defaults to native relinking
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT && buildArgs.Config != "Release";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: nativeRelink ? $"<WasmBuildNative>true</WasmBuildNative>" : string.Empty);

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () =>
                                {
                                    Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "resx"), Path.Combine(_projectDir!, "resx"));
                                    CreateProgramForCultureTest(_projectDir!, $"{projectName}.resx.words", "TestClass");
                                },
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));

            string output = RunAndTestWasmApp(
                                buildArgs, expectedExitCode: 42,
                                args: argCulture,
                                host: host, id: id);

            Assert.Contains(expectedOutput, output);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/61725", TestPlatforms.Windows)]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false, RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true,  RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false, RunHost.All })]
        public void ResourcesFromProjectReference(BuildArgs buildArgs,
                                                  bool nativeRelink,
                                                  string? argCulture,
                                                  string expectedOutput,
                                                  RunHost host,
                                                  string id)
        {
            string projectName = $"SatelliteAssemblyFromProjectRef";
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT;

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: $"<WasmBuildNative>{(nativeRelink ? "true" : "false")}</WasmBuildNative>",
                                        extraItems: $"<ProjectReference Include=\"..\\LibraryWithResources\\LibraryWithResources.csproj\" />");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                                InitProject: () =>
                                {
                                    string rootDir = _projectDir!;
                                    _projectDir = Path.Combine(rootDir, projectName);

                                    Directory.CreateDirectory(_projectDir);
                                    Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "SatelliteAssemblyFromProjectRef"), rootDir);
                                    CreateProgramForCultureTest(_projectDir, "LibraryWithResources.resx.words", "LibraryWithResources.Class1");
                                }));

            string output = RunAndTestWasmApp(buildArgs,
                                              expectedExitCode: 42,
                                              args: argCulture,
                                              host: host, id: id);

            Assert.Contains(expectedOutput, output);
        }

#pragma warning disable xUnit1026
        [Theory]
        [BuildAndRun(aot: true, host: RunHost.None)]
        public void CheckThatSatelliteAssembliesAreNotAOTed(BuildArgs buildArgs, string id)
        {
            string projectName = $"check_sat_asm_not_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: $@"
                                            <EmccCompileOptimizationFlag>-O0</EmccCompileOptimizationFlag>
                                            <EmccLinkOptimizationFlag>-O0</EmccLinkOptimizationFlag>",
                                        extraItems: $"<EmbeddedResource Include=\"{BuildEnvironment.RelativeTestAssetsPath}resx\\*\" />");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => CreateProgramForCultureTest(_projectDir!, $"{projectName}.words", "TestClass"),
                                DotnetWasmFromRuntimePack: false));

            var bitCodeFileNames = Directory.GetFileSystemEntries(Path.Combine(_projectDir!, "obj"), "*.dll.bc", SearchOption.AllDirectories)
                                    .Select(path => Path.GetFileName(path))
                                    .ToArray();

            // sanity check, in case we change file extensions
            Assert.Contains($"{projectName}.dll.bc", bitCodeFileNames);

            Assert.Empty(bitCodeFileNames.Where(file => file.EndsWith(".resources.dll.bc")));
        }
#pragma warning restore xUnit1026

        private void CreateProgramForCultureTest(string dir, string resourceName, string typeName)
            => File.WriteAllText(Path.Combine(dir, "Program.cs"),
                                s_cultureResourceTestProgram
                                    .Replace("##RESOURCE_NAME##", resourceName)
                                    .Replace("##TYPE_NAME##", typeName));

        private const string s_resourcesProjectTemplate =
            @$"<Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>{DefaultTargetFramework}</TargetFramework>
                <OutputType>Exe</OutputType>
                <WasmGenerateRunV8Script>true</WasmGenerateRunV8Script>
                <WasmMainJSPath>test-main.js</WasmMainJSPath>
                ##EXTRA_PROPERTIES##
              </PropertyGroup>
              <ItemGroup>
                ##EXTRA_ITEMS##
              </ItemGroup>
              ##INSERT_AT_END##
            </Project>";

        private static string s_cultureResourceTestProgram = @"
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace ResourcesTest
{
    public class TestClass
    {
        public static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                string cultureToTest = args[0];
                var newCulture = new CultureInfo(cultureToTest);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
            }

            var currentCultureName = Thread.CurrentThread.CurrentCulture.Name;

            var rm = new ResourceManager(""##RESOURCE_NAME##"", typeof(##TYPE_NAME##).Assembly);
            Console.WriteLine($""For '{currentCultureName}' got: {rm.GetString(""hello"")}"");

            return 42;
        }
    }
}";
    }
}
