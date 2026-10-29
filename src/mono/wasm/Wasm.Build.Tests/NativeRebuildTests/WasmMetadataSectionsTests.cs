// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Microsoft.NET.Sdk.WebAssembly;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    // Tests for the WebAssembly tool-conventions `producers` and `build_id` custom sections:
    //   - emitted into dotnet.native.wasm when the native runtime is re-linked, and
    //   - emitted into every webcil module (independent of native re-link).
    //
    // Workload-free cases validate managed stamping of the runtime-pack binary. Native re-link cases
    // run in workload lanes and also validate that wasm-ld accepts the producers object and Emscripten preserves it.
    [TestCategory("native")]
    public class WasmMetadataSectionsTests : NativeRebuildTestsBase
    {
        // Arbitrary even-length lowercase hex; $(WasmNativeBuildId) is normalized to lowercase.
        private const string PinnedBuildId = "1a2b3c4d5e6f7788";

        public WasmMetadataSectionsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        // The producers `processed-by` tool recorded in dotnet.native.wasm depends on the runtime engine.
        private static string NativeRuntimeProducer => IsCoreClrRuntime ? "CoreCLR" : "Mono";

        [Theory]
        [InlineData(Configuration.Release)]
        [InlineData(Configuration.Debug)]
        public void NativeWasm_Relink_WithBuildId_EmitsProducersAndBuildId(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_relink_id");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmBuildNative=true -p:WasmNativeBuildId={PinnedBuildId}"),
                isNativeBuild: true);

            string wasm = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), "dotnet.native");
            AssertProducers(wasm, NativeRuntimeProducer);
            Assert.Equal(PinnedBuildId, GetBuildIdHex(wasm));
            AssertBuildIdInBootConfig(config);
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData(Configuration.Release)]
        public void NativeWasm_WithoutRelink_WithBuildId_EmitsBuildId(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_no_relink_id");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmNativeBuildId={PinnedBuildId}"),
                isNativeBuild: false);

            string wasm = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), "dotnet.native");
            Assert.Equal(PinnedBuildId, GetBuildIdHex(wasm));
            AssertBuildIdInBootConfig(config);
        }

        [Theory]
        [InlineData(Configuration.Release, true)]
        [InlineData(Configuration.Release, false)]
        public void NativeWasm_Relink_WithoutBuildId_EmitsConfiguredProducers(Configuration config, bool emitProducers)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, $"meta_relink_noid_{emitProducers}");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmBuildNative=true -p:WasmEmitProducersSection={emitProducers}"),
                isNativeBuild: true);

            string wasm = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), "dotnet.native");
            if (emitProducers)
                AssertProducers(wasm, NativeRuntimeProducer);
            else
                Assert.Null(ReadCustomSection(wasm, "producers"));

            // No build_id is emitted unless $(WasmNativeBuildId) is set.
            Assert.Null(ReadCustomSection(wasm, "build_id"));
        }

        [TestCategory("no-workload")]
        [ConditionalTheory(typeof(BuildTestBase), nameof(UseWebcil))]
        [InlineData(Configuration.Release)]
        public void Webcil_EmitsProducersAndHashBuildId(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_webcil");
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: "-p:_WasmDevel=true"),
                isNativeBuild: false);

            string webcil = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), info.ProjectName);
            AssertProducers(webcil, "WebCIL");

            // Without an explicit id, each webcil gets a SHA-256 content hash (32 bytes).
            byte[]? buildId = ReadBuildId(webcil);
            Assert.NotNull(buildId);
            Assert.Equal(32, buildId!.Length);
        }

        [TestCategory("no-workload")]
        [ConditionalTheory(typeof(BuildTestBase), nameof(UseWebcil))]
        [InlineData(Configuration.Release)]
        public void Webcil_WithBuildId_UsesPinnedBuildId(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_webcil_id");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmNativeBuildId={PinnedBuildId}"),
                isNativeBuild: false);

            string webcil = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), info.ProjectName);
            AssertProducers(webcil, "WebCIL");
            Assert.Equal(PinnedBuildId, GetBuildIdHex(webcil));
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData(Configuration.Release)]
        public void InvalidBuildId_FailsWithoutWorkload(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_invalid_id");
            (_, string output) = PublishProject(info, config,
                new PublishOptions(ExpectSuccess: false, ExtraMSBuildArgs: "-p:_WasmDevel=true -p:WasmNativeBuildId=not-hex"),
                isNativeBuild: false);

            Assert.Contains("WasmNativeBuildId 'not-hex' is not a valid hex string.", output);
        }

        // -------------------- helpers --------------------

        private static string FindWasmAsset(string frameworkDirectory, string baseName)
        {
            string nonFingerprintedPath = Path.Combine(frameworkDirectory, $"{baseName}.wasm");
            if (File.Exists(nonFingerprintedPath))
                return nonFingerprintedPath;

            return Assert.Single(Directory.GetFiles(frameworkDirectory, $"{baseName}.*.wasm"));
        }

        private void AssertBuildIdInBootConfig(Configuration config)
        {
            string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true);
            WasmSdkBasedProjectProvider provider = GetProvider<WasmSdkBasedProjectProvider>();
            BootJsonData bootJson = provider.GetBootJson(provider.GetBootConfigPath(frameworkDirectory));
            Assert.Equal(PinnedBuildId, bootJson.buildId);
        }

        private static void AssertProducers(string wasmPath, string expectedProcessedBy)
        {
            byte[]? producers = ReadCustomSection(wasmPath, "producers");
            Assert.NotNull(producers);

            // The section holds length-prefixed ASCII field/value strings; a substring scan is enough.
            string text = Encoding.Latin1.GetString(producers!);
            Assert.Contains("C#", text);
            Assert.Contains(".NET", text);
            Assert.Contains(expectedProcessedBy, text);
        }

        private static string? GetBuildIdHex(string wasmPath)
        {
            byte[]? bytes = ReadBuildId(wasmPath);
            if (bytes is null)
                return null;

            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // The build_id payload is a WebAssembly vec(u8): a ULEB128 length followed by that many bytes.
        private static byte[]? ReadBuildId(string wasmPath)
        {
            byte[]? payload = ReadCustomSection(wasmPath, "build_id");
            if (payload is null)
                return null;

            int pos = 0;
            uint len = ReadULEB128(payload, ref pos);
            return payload[pos..(pos + (int)len)];
        }

        // Returns the payload of the named custom section (bytes following the section name), or null.
        private static byte[]? ReadCustomSection(string wasmPath, string sectionName)
        {
            byte[] bytes = File.ReadAllBytes(wasmPath);
            byte[]? found = null;
            int pos = 8; // skip the 4-byte magic and 4-byte version
            while (pos < bytes.Length)
            {
                byte id = bytes[pos++];
                uint size = ReadULEB128(bytes, ref pos);
                int payloadStart = pos;
                int payloadEnd = payloadStart + (int)size;
                if (id == 0) // custom section
                {
                    int p = payloadStart;
                    uint nameLen = ReadULEB128(bytes, ref p);
                    string name = Encoding.UTF8.GetString(bytes, p, (int)nameLen);
                    p += (int)nameLen;
                    if (name == sectionName)
                        found = bytes[p..payloadEnd]; // spec allows one; keep the last if duplicated
                }
                pos = payloadEnd;
            }
            return found;
        }

        private static uint ReadULEB128(byte[] bytes, ref int pos)
        {
            uint value = 0;
            int shift = 0;
            while (true)
            {
                byte b = bytes[pos++];
                value |= (uint)(b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }
            return value;
        }
    }
}
