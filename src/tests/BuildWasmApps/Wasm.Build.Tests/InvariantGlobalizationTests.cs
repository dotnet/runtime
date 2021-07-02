// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantGlobalizationTests : BuildTestBase
    {
        public InvariantGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantGlobalizationTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AOT_InvariantGlobalization(BuildArgs buildArgs, bool? invariantGlobalization, RunHost host, string id)
            => TestInvariantGlobalization(buildArgs, invariantGlobalization, host, id);

        // TODO: What else should we use to verify a relinked build?
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void RelinkingWithoutAOT(BuildArgs buildArgs, bool? invariantGlobalization, RunHost host, string id)
            => TestInvariantGlobalization(buildArgs, invariantGlobalization, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private void TestInvariantGlobalization(BuildArgs buildArgs, bool? invariantGlobalization,
                                                        RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            if (invariantGlobalization != null)
                extraProperties = $"{extraProperties}<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            string programText = @"
                using System;
                using System.Globalization;
                using System.Threading.Tasks;

                public class TestClass {
                    public static int Main()
                    {
                        CultureInfo culture;
                        try
                        {
                            culture = new CultureInfo(""es-ES"", false);
                        // https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
                        }
                        catch
                        {
                            culture = new CultureInfo("""", false);
                        }
                        Console.WriteLine($""{culture.LCID == CultureInfo.InvariantCulture.LCID} - {culture.NativeName}"");
                        return 42;
                    }
                }";

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                        hasIcudt: invariantGlobalization == null || invariantGlobalization.Value == false);

            string expectedOutputString = invariantGlobalization == true
                                            ? "True - Invariant Language (Invariant Country)"
                                            : "False - es (ES)";
            RunAndTestWasmApp(buildArgs, expectedExitCode: 42,
                                test: output => Assert.Contains(expectedOutputString, output), host: host, id: id);
        }
    }
}
