// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class HybridGlobalizationTests : BuildTestBase
    {
        public HybridGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> HybridGlobalizationTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AOT_HybridGlobalizationTests(BuildArgs buildArgs, RunHost host, string id)
            => TestHybridGlobalizationTests(buildArgs, host, id);

        [Theory]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void RelinkingWithoutAOT(BuildArgs buildArgs, RunHost host, string id)
            => TestHybridGlobalizationTests(buildArgs, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private void TestHybridGlobalizationTests(BuildArgs buildArgs, RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"hybrid";
            extraProperties = $"{extraProperties}<HybridGlobalization>true</HybridGlobalization>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            string programText = """
                using System;
                using System.Globalization;

                try
                {
                    CompareInfo compareInfo = new CultureInfo("es-ES").CompareInfo;
                    int shouldBeEqual = compareInfo.Compare("A\u0300", "\u00C0", CompareOptions.None);
                    if (shouldBeEqual != 0)
                    {
                        return 1;
                    }
                    int shouldThrow = compareInfo.Compare("A\u0300", "\u00C0", CompareOptions.IgnoreNonSpace);
                    Console.WriteLine($""Did not throw as expected but returned {shouldThrow} as a result. Using CompareOptions.IgnoreNonSpace option alone should be unavailable in HybridGlobalization mode."");
                }
                catch (PlatformNotSupportedException pnse)
                {
                    Console.WriteLine($"HybridGlobalization works, thrown exception as expected: {pnse}.");
                    return 42;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HybridGlobalization failed, unexpected exception was thrown: {ex}.");
                    return 2;
                }
                return 3;
            """;

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                                GlobalizationMode: GlobalizationMode.Hybrid));

            string output = RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("HybridGlobalization works, thrown exception as expected", output);
        }
    }
}
