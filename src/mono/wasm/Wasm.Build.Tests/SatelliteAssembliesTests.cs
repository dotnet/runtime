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
    public class SatelliteAssembliesTests : TestMainJsTestBase
    {
        public SatelliteAssembliesTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> SatelliteAssemblyTestData(bool aot, bool relinking, RunHost host)
            => ConfigWithAOTData(aot)
                    .Multiply(
                        new object?[] { relinking, "es-ES" },
                        new object?[] { relinking, null },
                        new object?[] { relinking, "ja-JP" })
                    .WithRunHosts(host)
                    .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false, RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true,  RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false, RunHost.All })]
        public void ResourcesFromMainAssembly(BuildArgs buildArgs,
                                              bool nativeRelink,
                                              string? argCulture,
                                              RunHost host,
                                              string id)
        {
            string projectName = $"sat_asm_from_main_asm";
            // Release+publish defaults to native relinking
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT && buildArgs.Config != "Release";

            string extraProperties = (nativeRelink ? $"<WasmBuildNative>true</WasmBuildNative>" : string.Empty)
                                        // make ASSERTIONS=1 so that we test with it
                                        + $"<EmccCompileOptimizationFlag>-O0 -sASSERTIONS=1</EmccCompileOptimizationFlag>"
                                        + $"<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: extraProperties);

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () =>
                                {
                                    Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "resx"), Path.Combine(_projectDir!, "resx"));
                                    CreateProgramForCultureTest(_projectDir!, $"{projectName}.resx.words", "TestClass");
                                },
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));

            RunAndTestWasmApp(
                            buildArgs, expectedExitCode: 42,
                            args: argCulture,
                            host: host, id: id,
                            // check that downloading assets doesn't have timing race conditions
                            extraXHarnessMonoArgs: host is RunHost.Chrome ? "--fetch-random-delay=200" : string.Empty);
        }

        [Theory]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false, RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true,  RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false, RunHost.All })]
        public void ResourcesFromProjectReference(BuildArgs buildArgs,
                                                  bool nativeRelink,
                                                  string? argCulture,
                                                  RunHost host,
                                                  string id)
        {
            string projectName = $"SatelliteAssemblyFromProjectRef";
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT;

            string extraProperties = $"<WasmBuildNative>{(nativeRelink ? "true" : "false")}</WasmBuildNative>"
                                        // make ASSERTIONS=1 so that we test with it
                                        + $"<EmccCompileOptimizationFlag>-O0 -sASSERTIONS=1</EmccCompileOptimizationFlag>"
                                        + $"<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: extraProperties,
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
                                    Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, projectName), rootDir);

                                    // D.B.* used for wasm projects should be moved next to the wasm project, so it doesn't
                                    // affect the non-wasm library project
                                    File.Move(Path.Combine(rootDir, "Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
                                    File.Move(Path.Combine(rootDir, "Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
                                    if (UseWBTOverridePackTargets)
                                        File.Move(Path.Combine(rootDir, "WasmOverridePacks.targets"), Path.Combine(_projectDir, "WasmOverridePacks.targets"));

                                    CreateProgramForCultureTest(_projectDir, "LibraryWithResources.resx.words", "LibraryWithResources.Class1");

                                    // The root D.B* should be empty
                                    File.WriteAllText(Path.Combine(rootDir, "Directory.Build.props"), "<Project />");
                                    File.WriteAllText(Path.Combine(rootDir, "Directory.Build.targets"), "<Project />");
                                }));

            RunAndTestWasmApp(buildArgs,
                              expectedExitCode: 42,
                              args: argCulture,
                              host: host, id: id);
        }

#pragma warning disable xUnit1026
        [Theory]
        [BuildAndRun(host: RunHost.None, aot: true)]
        public void CheckThatSatelliteAssembliesAreNotAOTed(BuildArgs buildArgs, string id)
        {
            string projectName = $"check_sat_asm_not_aot";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                                        projectTemplate: s_resourcesProjectTemplate,
                                        extraProperties: $@"
                                            <EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>
                                            <EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>
                                            <WasmDedup>false</WasmDedup>", // -O0 can cause aot-instances.dll to blow up, and fail to compile, and it is not really needed here
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
                <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
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
            string expected;
            if (args.Length == 1)
            {
                string cultureToTest = args[0];
                var newCulture = new CultureInfo(cultureToTest);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;

                if (cultureToTest == ""es-ES"")
                    expected = ""hola"";
                else if (cultureToTest == ""ja-JP"")
                    expected = ""\u3053\u3093\u306B\u3061\u306F"";
                else
                    throw new Exception(""Cannot determine the expected output for {cultureToTest}"");

            } else {
                expected = ""hello"";
            }

            var currentCultureName = Thread.CurrentThread.CurrentCulture.Name;

            var rm = new ResourceManager(""##RESOURCE_NAME##"", typeof(##TYPE_NAME##).Assembly);
            Console.WriteLine($""For '{currentCultureName}' got: {rm.GetString(""hello"")}"");

            return rm.GetString(""hello"") == expected ? 42 : -1;
        }
    }
}";
    }
}
