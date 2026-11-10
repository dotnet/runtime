// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
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
    // run in workload lanes and also validate that Emscripten assembles the producers source and preserves it.
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

            string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true);
            string frameworkWebcil = FindWasmAsset(frameworkDirectory, "System.Private.CoreLib");
            string? frameworkBuildId = GetBuildIdHex(frameworkWebcil);
            Assert.NotNull(frameworkBuildId);
            Assert.Equal(64, frameworkBuildId!.Length);
            Assert.NotEqual(GetBuildIdHex(webcil), frameworkBuildId);
        }

        [TestCategory("no-workload")]
        [ConditionalTheory(typeof(BuildTestBase), nameof(UseWebcil))]
        [InlineData(Configuration.Release)]
        public void Webcil_WithNativeBuildId_UsesPinnedBuildId(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_webcil_id");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmNativeBuildId=0X{PinnedBuildId.ToUpperInvariant()}"),
                isNativeBuild: false);

            string webcil = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), info.ProjectName);
            AssertProducers(webcil, "WebCIL");
            string? webcilBuildId = GetBuildIdHex(webcil);
            Assert.Equal(PinnedBuildId, webcilBuildId);

            string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true);
            string frameworkWebcil = FindWasmAsset(frameworkDirectory, "System.Private.CoreLib");
            string? frameworkBuildId = GetBuildIdHex(frameworkWebcil);
            Assert.Equal(PinnedBuildId, frameworkBuildId);

            string nativeWasm = FindWasmAsset(frameworkDirectory, "dotnet.native");
            Assert.Equal(PinnedBuildId, GetBuildIdHex(nativeWasm));
            AssertBuildIdInBootConfig(config);
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData("not-hex", "non_hex")]
        [InlineData("0x", "empty_prefix")]
        [InlineData("aa0xbb", "embedded_prefix")]
        [InlineData("123", "odd_length")]
        public void InvalidBuildId_FailsWithoutWorkload(string buildId, string testId)
        {
            const Configuration config = Configuration.Release;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, $"meta_invalid_id_{testId}");
            (_, string output) = PublishProject(
                info,
                config,
                new PublishOptions(ExpectSuccess: false, ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmNativeBuildId={buildId}"),
                isNativeBuild: false);

            Assert.Contains($"WasmNativeBuildId '{buildId}' is not a valid hex string.", output);
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData(Configuration.Release)]
        public void PreviousTargetFramework_IgnoresMetadataProperties(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_previous_tfm");
            string testRoot = Directory.GetParent(_projectDir)!.FullName;
            foreach (string projectFile in Directory.EnumerateFiles(testRoot, "*.csproj", SearchOption.AllDirectories))
            {
                string contents = File.ReadAllText(projectFile);
                File.WriteAllText(projectFile, contents.Replace(DefaultTargetFramework, PreviousTargetFramework));
            }

            PublishProject(info, config,
                new PublishOptions(
                    TargetFramework: PreviousTargetFramework,
                    AssertAppBundle: false,
                    ExtraMSBuildArgs: "-p:_WasmDevel=true -p:WasmNativeBuildId=not-hex"),
                isNativeBuild: false);

            string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true, PreviousTargetFramework);
            string nativeWasm = FindWasmAsset(frameworkDirectory, "dotnet.native");
            Assert.Null(ReadCustomSection(nativeWasm, "build_id"));

            if (UseWebcil)
            {
                string webcil = FindWasmAsset(frameworkDirectory, info.ProjectName);
                Assert.Null(ReadCustomSection(webcil, "producers"));
                Assert.Null(ReadCustomSection(webcil, "build_id"));
            }

            WasmSdkBasedProjectProvider provider = GetProvider<WasmSdkBasedProjectProvider>();
            BootJsonData bootJson = provider.GetBootJson(provider.GetBootConfigPath(frameworkDirectory));
            Assert.Null(bootJson.buildId);
        }

        [TestCategory("no-workload")]
        [Fact]
        public void RuntimePack_ContainsProducersAssemblySource()
        {
            string runtimePackVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
            string runtimePackName = IsCoreClrRuntime
                ? "Microsoft.NETCore.App.Runtime.browser-wasm"
                : "Microsoft.NETCore.App.Runtime.Mono.browser-wasm";
            string packagePath = Path.Combine(
                s_buildEnv.BuiltNuGetsPath,
                $"{runtimePackName}.{runtimePackVersion}.nupkg");

            using ZipArchive package = ZipFile.OpenRead(packagePath);
            ZipArchiveEntry? entry = package.GetEntry("runtimes/browser-wasm/native/dotnet-producers.s");
            Assert.NotNull(entry);
            using var reader = new StreamReader(entry.Open());
            string source = reader.ReadToEnd();
            Assert.Contains(".section .custom_section.producers", source);
            Assert.DoesNotContain(".ascii \"C#\"", source);
            Assert.Contains($".ascii \"{NativeRuntimeProducer}\"", source);
            Assert.Contains(".ascii \".NET\"", source);
            Assert.Contains($".ascii \"{runtimePackVersion}\"", source);
            Assert.Null(package.GetEntry("runtimes/browser-wasm/native/dotnet-producers.o"));
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
            => AssertProducers(File.ReadAllBytes(wasmPath), expectedProcessedBy);

        private static void AssertProducers(byte[] wasm, string expectedProcessedBy)
        {
            byte[]? producers = ReadCustomSection(wasm, "producers");
            Assert.NotNull(producers);

            // The section holds length-prefixed ASCII field/value strings; a substring scan is enough.
            string text = Encoding.Latin1.GetString(producers!);
            Assert.DoesNotContain("C#", text);
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
            => ReadCustomSection(File.ReadAllBytes(wasmPath), sectionName);

        private static byte[]? ReadCustomSection(byte[] bytes, string sectionName)
        {
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
