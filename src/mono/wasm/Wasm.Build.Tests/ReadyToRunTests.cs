// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.NET.WebAssembly.Webcil;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    // CoreCLR browser-wasm ships framework ReadyToRun images as webcil-in-wasm; a non-zero R2R table
    // size in the framework webcil is the marker that R2R was produced and staged. See dotnet/runtime#121257.
    public class ReadyToRunTests : WasmTemplateTestsBase
    {
        public ReadyToRunTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [BuildAndRun(aot: false, config: Configuration.Release)]
        [TestCategory("no-workload")]
        public void FrameworkAssembliesAreReadyToRun(Configuration config, bool aot)
        {
            // A build (not publish) stages the prebuilt framework R2R images from the runtime pack; it does
            // not run per-app crossgen2, so it does not depend on the SDK-side wasm crossgen resolution.
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "r2r_on",
                extraProperties: "<PublishReadyToRun>true</PublishReadyToRun>");
            BuildProject(info, config, new BuildOptions());

            AssertFrameworkAssemblyReadyToRun("System.Private.CoreLib", config, expectReadyToRun: true);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsCoreClrRuntime))]
        [BuildAndRun(aot: false, config: Configuration.Release)]
        [TestCategory("no-workload")]
        public void FrameworkAssembliesAreNotReadyToRunWhenDisabled(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "r2r_off",
                extraProperties: "<PublishReadyToRun>false</PublishReadyToRun>");
            BuildProject(info, config, new BuildOptions());

            AssertFrameworkAssemblyReadyToRun("System.Private.CoreLib", config, expectReadyToRun: false);
        }

        private void AssertFrameworkAssemblyReadyToRun(string assemblyName, Configuration config, bool expectReadyToRun)
        {
            // Build stages the framework webcils under obj/<config>/<tfm>/webcil (physically copied to
            // wwwroot/_framework only on publish), so read the staged image directly.
            string webcil = Path.Combine(_projectDir, "obj", config.ToString(), DefaultTargetFramework, "webcil", $"{assemblyName}.wasm");
            Assert.True(File.Exists(webcil), $"Expected staged webcil '{webcil}' to exist.");

            using FileStream stream = File.OpenRead(webcil);
            bool ok = WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out string? failureReason);
            Assert.True(ok, failureReason);

            if (expectReadyToRun)
                Assert.True(tableSize > 0, $"Expected a ReadyToRun table in '{webcil}', but the R2R table size was 0.");
            else
                Assert.Equal(0, tableSize);
        }
    }
}
