// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class SatelliteAssembliesTests : WasmTemplateTestsBase
    {
        public SatelliteAssembliesTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> SatelliteAssemblyTestData(bool aot, bool relinking)
            => ConfigWithAOTData(aot)
                    .Multiply(
                        new object?[] { relinking, "es-ES" },
                        new object?[] { relinking, null },
                        new object?[] { relinking, "ja-JP" })
                    .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                    .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false })]
        public async Task ResourcesFromMainAssembly(Configuration config, bool aot, bool nativeRelink, string? argCulture)
        {
            string prefix = $"sat_asm_from_main_asm";
            string extraProperties = (nativeRelink ? $"<WasmBuildNative>true</WasmBuildNative>" : string.Empty)
                                        // make ASSERTIONS=1 so that we test with it
                                        + $"<EmccCompileOptimizationFlag>-O0 -sASSERTIONS=1</EmccCompileOptimizationFlag>"
                                        + $"<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraProperties: extraProperties);
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "resx"), Path.Combine(_projectDir, "resx"));
            CreateProgramForCultureTest($"{info.ProjectName}.resx.words", "TestClass");

            (_, string output) = PublishProject(info, config, new PublishOptions(UseCache: false, AOT: aot), isNativeBuild: nativeRelink ? true : null);
            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: 42,
                Locale: argCulture ?? "en-US",
                // check that downloading assets doesn't have timing race conditions
                ExtraArgs: "--fetch-random-delay=200"
            ));
        }

        [Theory]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ true })]
        public async Task ResourcesFromProjectReference(Configuration config, bool aot, bool nativeRelink, string? argCulture)
        {
            string prefix = $"SatelliteAssemblyFromProjectRef";
            string extraProperties = $"<WasmBuildNative>{(nativeRelink ? "true" : "false")}</WasmBuildNative>"
                                        // make ASSERTIONS=1 so that we test with it
                                        + $"<EmccCompileOptimizationFlag>-O0 -sASSERTIONS=1</EmccCompileOptimizationFlag>"
                                        + $"<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>";
            string extraItems = $"<ProjectReference Include=\"..\\LibraryWithResources\\LibraryWithResources.csproj\" />";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraProperties: extraProperties, extraItems: extraItems);
            // D.B.* used for wasm projects should be moved next to the wasm project, so it doesn't
            // affect the non-wasm library project
            File.Move(Path.Combine(_projectDir, "..", "Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
            File.Move(Path.Combine(_projectDir, "..", "Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
            if (UseWBTOverridePackTargets)
                File.Move(Path.Combine(BuildEnvironment.TestDataPath, "WasmOverridePacks.targets"), Path.Combine(_projectDir, "WasmOverridePacks.targets"));
            Utils.DirectoryCopy(
                Path.Combine(BuildEnvironment.TestAssetsPath, "SatelliteAssemblyFromProjectRef/LibraryWithResources"),
                Path.Combine(_projectDir, "..", "LibraryWithResources"));
            CreateProgramForCultureTest("LibraryWithResources.resx.words", "LibraryWithResources.Class1");
            // move src/mono/wasm/testassets/SatelliteAssemblyFromProjectRef/LibraryWithResources to the test project
            // The root D.B* should be empty
            File.WriteAllText(Path.Combine(_projectDir, "..", "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(_projectDir, "..", "Directory.Build.targets"), "<Project />");
            // NativeFilesType dotnetWasmFileType = nativeRelink ? NativeFilesType.Relinked : aot ? NativeFilesType.AOT : NativeFilesType.FromRuntimePack;
            
            PublishProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: nativeRelink);

            await RunForPublishWithWebServer(
                new BrowserRunOptions(Configuration: config, TestScenario: "DotnetRun", ExpectedExitCode: 42, Locale: argCulture ?? "en-US"));
        }

#pragma warning disable xUnit1026
        [Theory]
        [BuildAndRun(aot: true, config: Configuration.Release)]
        public void CheckThatSatelliteAssembliesAreNotAOTed(Configuration config, bool aot)
        {
            string extraProperties = $@"<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>
                                        <EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>
                                        <WasmDedup>false</WasmDedup>"; // -O0 can cause aot-instances.dll to blow up, and fail to compile, and it is not really needed here
            string extraItems = $"<EmbeddedResource Include=\"{BuildEnvironment.RelativeTestAssetsPath}resx\\*\" />";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "check_sat_asm_not_aot", extraProperties: extraProperties, extraItems: extraItems);
            CreateProgramForCultureTest($"{info.ProjectName}.words", "TestClass");

            PublishProject(info, config, new PublishOptions(AOT: aot));

            var bitCodeFileNames = Directory.GetFileSystemEntries(Path.Combine(_projectDir, "obj"), "*.dll.bc", SearchOption.AllDirectories)
                                    .Select(path => Path.GetFileName(path))
                                    .ToArray();

            // sanity check, in case we change file extensions
            Assert.Contains($"{info.ProjectName}.dll.bc", bitCodeFileNames);

            Assert.DoesNotContain(bitCodeFileNames, file => file.EndsWith(".resources.dll.bc"));
        }
#pragma warning restore xUnit1026

        private void CreateProgramForCultureTest(string resourceName, string typeName)
        {
            string programRelativePath = Path.Combine("Common", "Program.cs");
            ReplaceFile(programRelativePath, Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "CultureResource.cs"));
            var replacements = new Dictionary<string, string> {
                {"##RESOURCE_NAME##", resourceName},
                {"##TYPE_NAME##", typeName}
            };
            UpdateFile(programRelativePath, replacements);
        }
    }
}
