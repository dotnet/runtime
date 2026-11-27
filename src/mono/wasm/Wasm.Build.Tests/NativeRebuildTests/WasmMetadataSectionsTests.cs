// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.NET.WebAssembly.Webcil;
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
    // Workload-free cases validate metadata already present in runtime-pack files. Native re-link cases
    // run in workload lanes and also validate that Emscripten assembles the producers source and preserves it.
    [TestCategory("native")]
    [TestCategory("coreclr")]
    public class WasmMetadataSectionsTests : NativeRebuildTestsBase
    {
        public WasmMetadataSectionsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [InlineData(Configuration.Release, false)]
        [InlineData(Configuration.Debug, true)]
        public void NativeWasm_Relink_EmitsProducersAndBuildId(Configuration config, bool runWasmOpt)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_relink_id",
                extraItems: runWasmOpt
                    ? """
                      <WasmOptConfigurationFlags Include="-O$(TestWasmOptLevel)" />
                      <WasmOptConfigurationFlags Include="--output-source-map=&quot;native source.map&quot;" Condition="'$(TestWasmOptLevel)' == '2'" />
                      <WasmOptConfigurationFlags Include="-osm &quot;native source.map&quot;" Condition="'$(TestWasmOptLevel)' == 'z'" />
                      """
                    : "");
            string extraArgs = $"-p:_WasmDevel=true -p:WasmBuildNative=true -p:WasmRunWasmOpt={runWasmOpt} -p:WasmEmitSymbolMap={runWasmOpt}";
            string frameworkDir = Publish("-p:TestWasmOptLevel=2");
            if (runWasmOpt)
            {
                (string initialId, string initialMap) = ReadOptimizedArtifacts(frameworkDir);
                Directory.Delete(frameworkDir, recursive: true);
                (string nextId, string nextMap) = ReadOptimizedArtifacts(Publish("-p:TestWasmOptLevel=z"));
                Assert.NotEqual(initialMap, nextMap);
                Assert.NotEqual(initialId, nextId);
            }
            else
            {
                Assert.NotEmpty(Assert.IsType<byte[]>(ReadBuildId(FindWasmAsset(frameworkDir, "dotnet.native"))));
            }

            string Publish(string optimizerArgs)
            {
                PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: $"{extraArgs} {optimizerArgs}", UseCache: false, AssertAppBundle: !runWasmOpt),
                    isNativeBuild: true);
                string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true);
                AssertProducers(FindWasmAsset(frameworkDirectory, "dotnet.native"), "CoreCLR",
                    expectedVersion: s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework));
                return frameworkDirectory;
            }

            (string Id, string SymbolMap) ReadOptimizedArtifacts(string frameworkDirectory)
            {
                string relinkDir = Path.Combine(GetObjDir(config), "wasm", "for-publish");
                string publishedWasm = FindWasmAsset(frameworkDirectory, "dotnet.native");
                Assert.Equal(File.ReadAllBytes(Path.Combine(relinkDir, "dotnet.native.wasm")), File.ReadAllBytes(publishedWasm));
                string symbolMap = File.ReadAllText(Assert.Single(Directory.GetFiles(frameworkDirectory, "dotnet.native*.js.symbols")));
                Assert.NotEmpty(symbolMap);
                Assert.Equal(File.ReadAllText(Path.Combine(relinkDir, "dotnet.native.js.symbols")), symbolMap);
                string buildId = AssertContentBuildId(publishedWasm, optimized: true);
                using JsonDocument sourceMap = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(_projectDir, "native source.map")));
                Assert.Equal(buildId, sourceMap.RootElement.GetProperty("debugId").GetString());
                return (buildId, symbolMap);
            }
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NativeWasm_WithoutRelink_UsesOnlyRuntimePackOutputAssets(bool publish)
        {
            const Configuration Config = Configuration.Release;
            const string ExtraArgs = "-p:_WasmDevel=true";
            ProjectInfo info = CopyTestAsset(Config, aot: false, TestAsset.WasmBasicTestApp, $"meta_no_relink_{publish}");
            string frameworkDir;
            if (publish)
            {
                PublishProject(info, Config, new PublishOptions(ExtraMSBuildArgs: ExtraArgs), isNativeBuild: false);
                frameworkDir = GetBinFrameworkDir(Config, forPublish: true);
                string publishDir = Path.Combine(GetBuildPaths(Config, forPublish: true).BinDir, "publish");
                Assert.Empty(Directory.EnumerateFiles(publishDir, "dotnet-producers.s", SearchOption.AllDirectories));
            }
            else
            {
                BuildProject(info, Config, new BuildOptions(ExtraMSBuildArgs: ExtraArgs), isNativeBuild: false);
                frameworkDir = WasmSdkBasedProjectProvider.GetMaterializedFrameworkDir(GetObjDir(Config));
                Assert.Empty(Directory.EnumerateFiles(frameworkDir, "dotnet-producers.s"));
            }

            string wasm = FindWasmAsset(frameworkDir, "dotnet.native");
            byte[]? buildId = ReadBuildId(wasm);
            Assert.NotNull(buildId);
            Assert.NotEmpty(buildId!);
        }

        [Theory]
        [InlineData(Configuration.Release, true)]
        [InlineData(Configuration.Release, false)]
        public void NativeWasm_Relink_EmitsConfiguredProducers(Configuration config, bool emitProducers)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, $"meta_relink_noid_{emitProducers}");
            PublishProject(info, config,
                new PublishOptions(ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmBuildNative=true -p:WasmEmitProducersSection={emitProducers}"),
                isNativeBuild: true);

            string wasm = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), "dotnet.native");
            if (emitProducers)
                AssertProducers(wasm, "CoreCLR");
            else
                Assert.Null(ReadCustomSection(wasm, "producers"));

            byte[]? buildId = ReadBuildId(wasm);
            Assert.NotNull(buildId);
            Assert.NotEmpty(buildId!);
        }

        [Theory]
        [InlineData(Configuration.Release)]
        public void NativeWasm_Relink_MissingRuntimePackProducersSource_Succeeds(Configuration config)
        {
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "meta_relink_missing_producers");
            string missingSource = Path.Combine(_projectDir, "missing-dotnet-producers.s");
            PublishProject(info, config,
                new PublishOptions(
                    AssertAppBundle: false,
                    ExtraMSBuildArgs: $"-p:_WasmDevel=true -p:WasmBuildNative=true -p:_WasmProducersSourcePath=\"{missingSource}\""),
                isNativeBuild: true);

            string wasm = FindWasmAsset(GetBinFrameworkDir(config, forPublish: true), "dotnet.native");
            Assert.Null(ReadCustomSection(wasm, "producers"));
            Assert.NotEmpty(Assert.IsType<byte[]>(ReadBuildId(wasm)));
        }

        [TestCategory("no-workload")]
        [ConditionalTheory(typeof(BuildTestBase), nameof(UseWebcil))]
        [InlineData(null)]
        [InlineData("9.9.9-app")]
        public void Webcil_EmitsProducersAndHashBuildId(string? appProductVersion)
        {
            const Configuration Config = Configuration.Release;
            string extraArgs = appProductVersion is null ? "-p:_WasmDevel=true" : $"-p:_WasmDevel=true -p:ProductVersion={appProductVersion}";
            ProjectInfo info = CopyTestAsset(Config, aot: false, TestAsset.WasmBasicTestApp, $"meta_webcil_{appProductVersion ?? "default"}");
            string converterVersion = typeof(WebcilWasmWrapper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

            BuildProject(info, Config, new BuildOptions(ExtraMSBuildArgs: extraArgs), isNativeBuild: false);
            string buildWebcil = FindWasmAsset(Path.Combine(GetObjDir(Config), "webcil"), info.ProjectName);
            AssertProducers(buildWebcil, "WebCIL", expectedVersion: converterVersion);

            PublishProject(info, Config, new PublishOptions(ExtraMSBuildArgs: extraArgs, UseCache: false), isNativeBuild: false);
            string frameworkDirectory = GetBinFrameworkDir(Config, forPublish: true);
            string webcil = FindWasmAsset(frameworkDirectory, info.ProjectName);
            AssertProducers(webcil, "WebCIL", expectedVersion: converterVersion);

            // Each webcil gets a SHA-256 content hash (32 bytes).
            byte[]? buildId = ReadBuildId(webcil);
            Assert.NotNull(buildId);
            Assert.Equal(32, buildId!.Length);

            string frameworkWebcil = FindWasmAsset(frameworkDirectory, "System.Private.CoreLib");
            AssertProducers(frameworkWebcil, "WebCIL");
            string? frameworkBuildId = GetBuildIdHex(frameworkWebcil);
            Assert.NotNull(frameworkBuildId);
            Assert.Equal(64, frameworkBuildId!.Length);
            Assert.NotEqual(GetBuildIdHex(webcil), frameworkBuildId);
        }

        [TestCategory("no-workload")]
        [Theory]
        [InlineData(Configuration.Release)]
        public void PreviousTargetFramework_IgnoresWebcilMetadata(Configuration config)
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
                    ExtraMSBuildArgs: "-p:_WasmDevel=true"),
                isNativeBuild: false);

            string frameworkDirectory = GetBinFrameworkDir(config, forPublish: true, PreviousTargetFramework);

            if (UseWebcil)
            {
                string webcil = FindWasmAsset(frameworkDirectory, info.ProjectName);
                Assert.Null(ReadCustomSection(webcil, "producers"));
                Assert.Null(ReadCustomSection(webcil, "build_id"));
            }

        }

        [TestCategory("no-workload")]
        [Fact]
        public void RuntimePack_ContainsProducersAssemblySource()
        {
            string runtimePackVersion = s_buildEnv.GetRuntimePackVersion(DefaultTargetFramework);
            string packagePath = Path.Combine(
                s_buildEnv.BuiltNuGetsPath,
                $"Microsoft.NETCore.App.Runtime.browser-wasm.{runtimePackVersion}.nupkg");

            using ZipArchive package = ZipFile.OpenRead(packagePath);
            var runtimePack = new RuntimePackAssets(packagePath, package);
            ZipArchiveEntry? entry = package.GetEntry("runtimes/browser-wasm/native/dotnet-producers.s");
            Assert.NotNull(entry);
            using var reader = new StreamReader(entry.Open());
            string source = reader.ReadToEnd();
            Assert.Contains(".section .custom_section.producers", source);
            Assert.DoesNotContain(".ascii \"C#\"", source);
            Assert.Contains(".ascii \"CoreCLR\"", source);
            Assert.Contains(".ascii \".NET\"", source);
            Assert.Contains($".ascii \"{runtimePackVersion}\"", source);
            Assert.Null(package.GetEntry("runtimes/browser-wasm/native/dotnet-producers.o"));

            byte[] r2rCoreLib = runtimePack.ReadBytes(RuntimePackAssets.GetNativePath("r2r/System.Private.CoreLib.wasm"));
            AssertProducers(r2rCoreLib, "WebCIL", requireVersion: true);

            byte[] nativeWasmBytes = runtimePack.ReadBytes(RuntimePackAssets.GetNativePath("dotnet.native.wasm"));
            AssertProducers(nativeWasmBytes, "CoreCLR", expectedVersion: runtimePackVersion);
            byte[]? nativeBuildId = ReadBuildId(nativeWasmBytes);
            Assert.NotNull(nativeBuildId);
            Assert.NotEmpty(nativeBuildId!);
        }

        // -------------------- helpers --------------------

        private static string FindWasmAsset(string frameworkDirectory, string baseName)
        {
            string nonFingerprintedPath = Path.Combine(frameworkDirectory, $"{baseName}.wasm");
            if (File.Exists(nonFingerprintedPath))
                return nonFingerprintedPath;

            return Assert.Single(Directory.GetFiles(frameworkDirectory, $"{baseName}.*.wasm"));
        }

        private static void AssertProducers(string wasmPath, string expectedProcessedBy, string? expectedVersion = null)
            => AssertProducers(File.ReadAllBytes(wasmPath), expectedProcessedBy, expectedVersion: expectedVersion);

        private static void AssertProducers(byte[] wasm, string expectedProcessedBy, bool requireVersion = false, string? expectedVersion = null)
        {
            byte[]? producers = ReadCustomSection(wasm, "producers");
            Assert.NotNull(producers);

            AssertProducer(producers!, "processed-by", expectedProcessedBy, requireVersion, expectedVersion);
            AssertProducer(producers!, "sdk", ".NET", requireVersion, expectedVersion);
        }

        private static void AssertProducer(byte[] producers, string expectedField, string expectedName, bool requireVersion, string? expectedVersion)
        {
            int pos = 0;
            bool found = false;
            uint fieldCount = ReadULEB128(producers, ref pos);
            for (uint fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
            {
                string field = ReadName(producers, ref pos);
                uint valueCount = ReadULEB128(producers, ref pos);
                for (uint valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    string name = ReadName(producers, ref pos);
                    string version = ReadName(producers, ref pos);
                    Assert.NotEqual("C#", name);

                    if (field == expectedField && name == expectedName)
                    {
                        if (expectedVersion is not null)
                            Assert.Equal(expectedVersion, version);
                        else if (requireVersion)
                            Assert.NotEmpty(version);
                        found = true;
                    }
                }
            }

            Assert.True(found, $"Producer '{expectedField}={expectedName}' was not found.");
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

        internal static string AssertContentBuildId(string wasmPath, bool optimized)
        {
            byte[] module = File.ReadAllBytes(wasmPath);
            byte[] payload = Assert.IsType<byte[]>(ReadCustomSection(module, "build_id", out int buildIdStart));
            Assert.Equal(module.Length, buildIdStart + GetBuildIdSectionLength(module, buildIdStart));

            int position = 0;
            Assert.Equal((uint)SHA256.HashSizeInBytes, ReadULEB128(payload, ref position));
            byte[] id = payload[position..];
            int hashedLength = buildIdStart;
            if (!optimized)
            {
                Assert.NotNull(ReadCustomSection(module, "producers", out hashedLength));
            }
            Assert.Equal(SHA256.HashData(module.AsSpan(0, hashedLength)), id);
            return Convert.ToHexStringLower(id);

            static int GetBuildIdSectionLength(byte[] module, int start)
            {
                int position = start + 1;
                uint size = ReadULEB128(module, ref position);
                return position - start + checked((int)size);
            }
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

        private static byte[]? ReadBuildId(byte[] wasm)
        {
            byte[]? payload = ReadCustomSection(wasm, "build_id");
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
            => ReadCustomSection(bytes, sectionName, out _);

        private static byte[]? ReadCustomSection(byte[] bytes, string sectionName, out int sectionStart)
        {
            byte[]? found = null;
            sectionStart = -1;
            int pos = 8; // skip the 4-byte magic and 4-byte version
            while (pos < bytes.Length)
            {
                int start = pos;
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
                    {
                        Assert.Null(found);
                        found = bytes[p..payloadEnd];
                        sectionStart = start;
                    }
                }
                pos = payloadEnd;
            }
            return found;
        }

        private static string ReadName(byte[] bytes, ref int pos)
        {
            uint length = ReadULEB128(bytes, ref pos);
            string value = Encoding.UTF8.GetString(bytes, pos, checked((int)length));
            pos += checked((int)length);
            return value;
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
