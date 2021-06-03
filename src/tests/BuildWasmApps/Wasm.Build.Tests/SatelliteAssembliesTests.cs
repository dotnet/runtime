// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                        new object?[] { relinking, "ja-JP", "got: こんにちは" })
                    .WithRunHosts(host)
                    .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ false, RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ false, /*relinking*/ true,  RunHost.All })]
        [MemberData(nameof(SatelliteAssemblyTestData), parameters: new object[] { /*aot*/ true,  /*relinking*/ false, RunHost.All })]
        public void TestResourcesAccessibleFromSatelliteAssemblies(BuildArgs buildArgs,
                                                                   bool nativeRelink,
                                                                   string? argCulture,
                                                                   string expectedOutput,
                                                                   RunHost host,
                                                                   string id)
        {
            string resxPath = Path.Combine(s_testAssetsPath, "resx");
            string projectName = $"sat_asm_{buildArgs.Config}_{buildArgs.AOT}";
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT;

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, $"<WasmBuildNative>{(nativeRelink ? "true" : "false")}</WasmBuildNative>");

            BuildProject(buildArgs,
                        initProject: () => CreateProjectForCultureTest(projectName, _projectDir!, resxPath),
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                        id: id);

            string output = RunAndTestWasmApp(
                                buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                                args: argCulture,
                                test: output => {},
                                host: host, id: id);

            Assert.Contains(expectedOutput, output);
        }

#pragma warning disable xUnit1026
        [Theory]
        [BuildAndRun(host: RunHost.All, aot: true)]
        public void CheckThatSatelliteAssembliesAreNotAOTed(BuildArgs buildArgs, RunHost host, string id)
        {
            string resxPath = Path.Combine(s_testAssetsPath, "resx");
            string projectName = $"sat_asm_{buildArgs.Config}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                            extraProperties: @"
                                <EmccCompileOptimizationFlag>-O0</EmccCompileOptimizationFlag>
                                <EmccLinkOptimizationFlag>-O0</EmccLinkOptimizationFlag>");

            BuildProject(buildArgs,
                        initProject: () => CreateProjectForCultureTest(projectName, _projectDir!, resxPath),
                        id: id);

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                Assert.True(false, $"Test bug: could not get the build product in the cache");

            var bitCodeFiles = Directory.GetFileSystemEntries(product!.BuildPath, "*.dll.bc", SearchOption.AllDirectories);

            // sanity check, in case we change file extensions
            Assert.Contains($"{projectName}.dll.bc", bitCodeFiles);

            Assert.Empty(bitCodeFiles.Where(file => file.EndsWith(".resources.dll.bc")));
        }
#pragma warning restore xUnit1026

        private static void CreateProjectForCultureTest(string projectName, string projectDir, string resxPath)
        {
            string program = s_cultureResourceTestProgram
                                .Replace("##RESOURCE_NAME##", $"{projectName}.words");

            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), program);

            foreach(var file in Directory.GetFileSystemEntries(resxPath, "words*"))
                File.Copy(file, Path.Combine(projectDir, Path.GetFileName(file)));
        }

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

            var rm = new ResourceManager(""##RESOURCE_NAME##"", typeof(TestClass).Assembly);
            Console.WriteLine($""For '{currentCultureName}' got: {rm.GetString(""hello"")}"");

            return 42;
        }
    }
}";
    }
}
