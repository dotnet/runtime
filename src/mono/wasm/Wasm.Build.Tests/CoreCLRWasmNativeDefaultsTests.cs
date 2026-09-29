// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    // Covers the static native.wasm.targets contract: a property whose value differs from the one
    // baked into the runtime pack must force a native relink, and an explicit WasmBuildNative=false
    // in that situation must fail the build rather than silently produce an app whose configuration
    // does not match the prebuilt dotnet.native.wasm.
    [TestCategory("workload")]
    [TestCategory("coreclr")]
    public class CoreCLRWasmNativeDefaultsTests : WasmTemplateTestsBase
    {
        private static readonly Regex s_regex = new("\\*\\* WasmBuildNative:.*");
        private static readonly Regex s_r2rDirectoryRegex = new("\\*\\* WasmPublishR2RDir: '([^']*)'");

        public CoreCLRWasmNativeDefaultsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static TheoryData<string, bool> PropertiesThatTriggerRelinking() => new()
        {
            // property value matching the runtime pack -> no relink
            { "<InvariantGlobalization>false</InvariantGlobalization>", false },
            { "<InvariantTimezone>false</InvariantTimezone>", false },
            { "<EnableDiagnostics>false</EnableDiagnostics>", false },
            // property left unset -> no relink
            { "", false },
            // casing must not matter, MSBuild string comparison is case-insensitive
            { "<InvariantGlobalization>False</InvariantGlobalization>", false },
            // property differing from the runtime pack -> relink
            { "<InvariantGlobalization>true</InvariantGlobalization>", true },
            { "<InvariantTimezone>true</InvariantTimezone>", true },
            { "<EnableDiagnostics>true</EnableDiagnostics>", true },
            { "<WasmInitialHeapSize>67108864</WasmInitialHeapSize>", true },
            // EmccInitialHeapSize is the documented browser property and EmccTotalMemory its legacy
            // alias; both must be normalized into WasmInitialHeapSize before the registry compares.
            { "<EmccInitialHeapSize>67108864</EmccInitialHeapSize>", true },
            { "<EmccTotalMemory>67108864</EmccTotalMemory>", true },
            { "<EmccInitialHeapSize>33554432</EmccInitialHeapSize>", false },
            { "<EmccTotalMemory>33554432</EmccTotalMemory>", false },
            { "<EmccMaximumHeapSize>1073741824</EmccMaximumHeapSize>", true },
            // memory values matching the runtime pack must not relink
            { "<WasmInitialHeapSize>33554432</WasmInitialHeapSize>", false },
            { "<EmccStackSize>2MB</EmccStackSize>", false },
            // comparison is textual, so a numerically equal but differently spelled size counts as
            // a mismatch. That only costs an unnecessary relink, never a mismatched binary.
            { "<EmccStackSize>2097152</EmccStackSize>", true },
            // WasmPerformanceInstrumentation would force a relink, but that defaulting is temporarily
            // disabled in BrowserWasmApp.CoreCLR.targets pending https://github.com/dotnet/runtime/issues/132772,
            // so it currently does not relink.
            { "<WasmPerformanceInstrumentation>all</WasmPerformanceInstrumentation>", false },
        };

        [Theory]
        [MemberData(nameof(PropertiesThatTriggerRelinking))]
        public void PropertyDifferentFromRuntimePackTriggersRelinking(string extraProperties, bool expectWasmBuildNative)
        {
            string? line = BuildAndGetWasmBuildNativeLine("coreclr_native_defaults", extraProperties, expectSuccess: true);

            Assert.NotNull(line);
            Assert.Contains($"** WasmBuildNative: '{(expectWasmBuildNative ? "true" : "")}'", line);
        }

        [Theory]
        [InlineData("<InvariantGlobalization>true</InvariantGlobalization>")]
        [InlineData("<InvariantTimezone>true</InvariantTimezone>")]
        public void ExplicitWasmBuildNativeFalseWithMismatchErrors(string extraProperties)
        {
            (string output, string? _) = BuildAndGetOutput(
                "coreclr_native_defaults_error",
                extraProperties + "<WasmBuildNative>false</WasmBuildNative>",
                expectSuccess: false);

            Assert.Contains("WasmBuildNative is required", output);
            Assert.Contains("but WasmBuildNative is already set to 'false'", output);
        }

        [Fact]
        public void ExplicitWasmBuildNativeFalseWithoutMismatchIsAllowed()
        {
            string? line = BuildAndGetWasmBuildNativeLine(
                "coreclr_native_defaults_nobuild",
                "<WasmBuildNative>false</WasmBuildNative>",
                expectSuccess: true);

            Assert.NotNull(line);
            Assert.Contains("** WasmBuildNative: 'false'", line);
        }

        // Mirrors the Mono path's WithNativeReference test: a project that references a native
        // object file always needs a relink to embed it, regardless of whether any tracked
        // property differs from the runtime pack.
        [Fact]
        public void NativeFileReferenceTriggersRelinking()
        {
            string nativeLibPath = Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o");
            string extraItems = @$"<NativeFileReference Include=""{nativeLibPath}"" />";

            string? line = BuildAndGetWasmBuildNativeLine(
                "coreclr_native_defaults_nativeref",
                extraProperties: "",
                extraItems: extraItems,
                expectSuccess: true);

            Assert.NotNull(line);
            Assert.Contains("** WasmBuildNative: 'true'", line);
        }

        [Fact]
        public void PublishReadyToRunDirectoryMatchesSdkOutputCasing()
        {
            Configuration config = Configuration.Debug;
            string printValueTarget = """
                <Target Name="PrintWasmPublishR2RDir"
                        DependsOnTargets="_WasmCoreClrSelectR2RDirectories">
                    <Message Text="** WasmPublishR2RDir: '$(_WasmPublishR2RDir)'" Importance="High" />
                    <Error Text="Stopping after validating the R2R directory" />
                </Target>
                """;

            ProjectInfo info = CopyTestAsset(
                config,
                aot: false,
                TestAsset.WasmBasicTestApp,
                "coreclr_r2r_directory",
                extraProperties: """
                    <PublishReadyToRun>true</PublishReadyToRun>
                    <PublishTrimmed>true</PublishTrimmed>
                    """,
                insertAtEnd: printValueTarget);

            (string _, string output) = BuildProject(
                info,
                config,
                new BuildOptions(
                    ExpectSuccess: false,
                    ExtraMSBuildArgs: "-t:PrintWasmPublishR2RDir"));

            Assert.Contains("Stopping after validating the R2R directory", output);
            Match match = s_r2rDirectoryRegex.Match(output);
            Assert.True(match.Success, output);
            Assert.Equal(Path.Combine(GetObjDir(config), "R2R") + Path.DirectorySeparatorChar, match.Groups[1].Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NativeRelinkResolvesCrossgen2WithoutReadyToRun(bool publish)
        {
            string targetsFile = $"{nameof(NativeRelinkResolvesCrossgen2WithoutReadyToRun)}.Build.targets";
            ProjectInfo info = CopyTestAsset(
                Configuration.Debug,
                aot: false,
                TestAsset.WasmBasicTestApp,
                "coreclr_sdk_crossgen2",
                extraProperties: $$"""
                    <PublishReadyToRun>false</PublishReadyToRun>
                    <WasmBuildNative>true</WasmBuildNative>
                    <_WasmBuildTestExpectNestedPublish>{{publish}}</_WasmBuildTestExpectNestedPublish>
                    """,
                insertAtEnd: $"""<Import Project="{targetsFile}" />""");
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, targetsFile), Path.Combine(_projectDir, targetsFile));

            // Run the generator, then stop before native compilation.
            string output = publish
                ? PublishProject(info, Configuration.Debug, new PublishOptions(ExpectSuccess: false)).buildOutput
                : BuildProject(info, Configuration.Debug, new BuildOptions(ExpectSuccess: false)).buildOutput;

            Assert.Contains("Stopping after validating SDK crossgen2", output);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NativeRelinkWithoutCrossgen2PackReportsMissingGenerator(bool publish)
        {
            ProjectInfo info = CopyTestAsset(
                Configuration.Debug,
                aot: false,
                TestAsset.WasmBasicTestApp,
                "coreclr_missing_crossgen2",
                extraProperties: """
                    <PublishReadyToRun>false</PublishReadyToRun>
                    <RequiresCrossgen2Pack>false</RequiresCrossgen2Pack>
                    <WasmBuildNative>true</WasmBuildNative>
                    """);

            string output = publish
                ? PublishProject(info, Configuration.Debug, new PublishOptions(ExpectSuccess: false)).buildOutput
                : BuildProject(info, Configuration.Debug, new BuildOptions(ExpectSuccess: false)).buildOutput;

            Assert.Contains("Could not resolve crossgen2. Update the .NET SDK and restore the project, or set $(Crossgen2Path) to a crossgen2 executable.", output);
            Assert.DoesNotContain("NETSDK1094", output);
        }

        private string? BuildAndGetWasmBuildNativeLine(string projectPrefix, string extraProperties, bool expectSuccess)
            => BuildAndGetOutput(projectPrefix, extraProperties, extraItems: "", expectSuccess).line;

        private string? BuildAndGetWasmBuildNativeLine(string projectPrefix, string extraProperties, string extraItems, bool expectSuccess)
            => BuildAndGetOutput(projectPrefix, extraProperties, extraItems, expectSuccess).line;

        private (string output, string? line) BuildAndGetOutput(string projectPrefix, string extraProperties, bool expectSuccess)
            => BuildAndGetOutput(projectPrefix, extraProperties, extraItems: "", expectSuccess);

        private (string output, string? line) BuildAndGetOutput(string projectPrefix, string extraProperties, string extraItems, bool expectSuccess)
        {
            Configuration config = Configuration.Debug;

            // Print the computed value and stop before actually relinking - the decision is what is
            // under test here, and a real emcc link would make these cases prohibitively slow.
            string printValueTarget = @"
                <Target Name=""PrintWasmBuildNative"" AfterTargets=""_CoreCLRSetWasmBuildNativeDefaults"">
                    <Message Text=""** WasmBuildNative: '$(WasmBuildNative)'"" Importance=""High"" />
                    <Error Text=""Stopping the build"" />
                </Target>";

            ProjectInfo info = CopyTestAsset(
                    config,
                    aot: false,
                    TestAsset.WasmBasicTestApp,
                    projectPrefix,
                    extraProperties: extraProperties,
                    extraItems: extraItems,
                    insertAtEnd: printValueTarget);
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (string _, string output) = BuildProject(info, config, new BuildOptions(ExpectSuccess: false));

            Match m = s_regex.Match(output);
            string? line = m.Success ? m.Groups[0]?.ToString() : null;

            if (expectSuccess)
            {
                // the build is expected to reach the print target and be stopped by it
                Assert.Contains("Stopping the build", output);
            }

            return (output, line);
        }
    }

    internal sealed class NativeWasmSymbolMapInfo
    {
        public required int ImportedFunctionCount { get; init; }
        public required int DefinedFunctionCount { get; init; }
        public required int CodeFunctionCount { get; init; }
        public required IReadOnlyDictionary<int, string> Symbols { get; init; }
        public required IReadOnlyDictionary<string, int> FunctionExports { get; init; }
    }

    internal static class NativeWasmSymbolMapValidator
    {
        private const byte FunctionExternalKind = 0;
        private const byte TableExternalKind = 1;
        private const byte MemoryExternalKind = 2;
        private const byte GlobalExternalKind = 3;
        private const byte TagExternalKind = 4;

        public static NativeWasmSymbolMapInfo Validate(
            string wasmPath,
            string symbolsPath,
            string? expectedWasmIntegrity = null,
            string? expectedSymbolsIntegrity = null)
            => Validate(
                File.ReadAllBytes(wasmPath),
                File.ReadAllBytes(symbolsPath),
                expectedWasmIntegrity,
                expectedSymbolsIntegrity);

        public static NativeWasmSymbolMapInfo Validate(
            byte[] wasmBytes,
            byte[] symbolsBytes,
            string? expectedWasmIntegrity = null,
            string? expectedSymbolsIntegrity = null)
        {
            ValidateIntegrity(wasmBytes, expectedWasmIntegrity, "Wasm");
            ValidateIntegrity(symbolsBytes, expectedSymbolsIntegrity, "symbol map");

            NativeWasmSymbolMapInfo module = ReadModule(wasmBytes);
            Dictionary<int, string> symbols = ReadSymbols(symbolsBytes);
            int expectedFunctionCount = checked(module.ImportedFunctionCount + module.DefinedFunctionCount);

            if (module.CodeFunctionCount != module.DefinedFunctionCount)
            {
                throw new InvalidDataException(
                    $"Wasm function section contains {module.DefinedFunctionCount} entries, " +
                    $"but its code section contains {module.CodeFunctionCount}.");
            }

            if (symbols.Count != expectedFunctionCount)
            {
                throw new InvalidDataException(
                    $"Symbol map contains {symbols.Count} entries for {module.ImportedFunctionCount} imported and " +
                    $"{module.DefinedFunctionCount} defined functions.");
            }

            for (int expectedIndex = 0; expectedIndex < expectedFunctionCount; expectedIndex++)
            {
                if (!symbols.ContainsKey(expectedIndex))
                {
                    throw new InvalidDataException(
                        $"Symbol map expected absolute function index {expectedIndex}, " +
                        $"including {module.ImportedFunctionCount} function imports.");
                }
            }

            return new NativeWasmSymbolMapInfo
            {
                ImportedFunctionCount = module.ImportedFunctionCount,
                DefinedFunctionCount = module.DefinedFunctionCount,
                CodeFunctionCount = module.CodeFunctionCount,
                Symbols = symbols,
                FunctionExports = module.FunctionExports
            };
        }

        public static string ComputeIntegrity(byte[] bytes)
            => $"sha256-{Convert.ToBase64String(SHA256.HashData(bytes))}";

        private static void ValidateIntegrity(byte[] bytes, string? expectedIntegrity, string artifactName)
        {
            if (expectedIntegrity is null)
                return;

            string actualIntegrity = ComputeIntegrity(bytes);
            if (!string.Equals(actualIntegrity, expectedIntegrity, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"{artifactName} SHA-256 mismatch. Expected '{expectedIntegrity}', actual '{actualIntegrity}'.");
            }
        }

        private static NativeWasmSymbolMapInfo ReadModule(byte[] wasmBytes)
        {
            ReadOnlySpan<byte> image = wasmBytes;
            if (image.Length < 8 ||
                BinaryPrimitives.ReadUInt32LittleEndian(image) != 0x6D736100 ||
                BinaryPrimitives.ReadUInt32LittleEndian(image.Slice(4)) != 1)
            {
                throw new InvalidDataException("Invalid WebAssembly module header.");
            }

            int importedFunctionCount = 0;
            int definedFunctionCount = -1;
            int codeFunctionCount = -1;
            Dictionary<string, int> functionExports = new(StringComparer.Ordinal);
            int offset = 8;

            while (offset < image.Length)
            {
                byte sectionId = ReadByte(image, ref offset, image.Length);
                uint sectionSize = ReadUleb32(image, ref offset, image.Length);
                int sectionEnd = checked(offset + (int)sectionSize);
                if (sectionEnd > image.Length)
                    throw new InvalidDataException($"WebAssembly section {sectionId} extends past the end of the module.");

                switch (sectionId)
                {
                    case 2:
                        importedFunctionCount = ReadFunctionImports(image, ref offset, sectionEnd);
                        break;
                    case 3:
                        definedFunctionCount = checked((int)ReadUleb32(image, ref offset, sectionEnd));
                        break;
                    case 7:
                        functionExports = ReadFunctionExports(image, ref offset, sectionEnd);
                        break;
                    case 10:
                        codeFunctionCount = checked((int)ReadUleb32(image, ref offset, sectionEnd));
                        break;
                }

                offset = sectionEnd;
            }

            if (definedFunctionCount < 0 || codeFunctionCount < 0)
                throw new InvalidDataException("WebAssembly module is missing its function or code section.");

            return new NativeWasmSymbolMapInfo
            {
                ImportedFunctionCount = importedFunctionCount,
                DefinedFunctionCount = definedFunctionCount,
                CodeFunctionCount = codeFunctionCount,
                Symbols = new Dictionary<int, string>(),
                FunctionExports = functionExports
            };
        }

        private static int ReadFunctionImports(ReadOnlySpan<byte> image, ref int offset, int end)
        {
            uint importCount = ReadUleb32(image, ref offset, end);
            int functionCount = 0;
            for (uint i = 0; i < importCount; i++)
            {
                ReadName(image, ref offset, end);
                ReadName(image, ref offset, end);
                byte kind = ReadByte(image, ref offset, end);
                switch (kind)
                {
                    case FunctionExternalKind:
                        ReadUleb32(image, ref offset, end);
                        functionCount++;
                        break;
                    case TableExternalKind:
                        ReadByte(image, ref offset, end);
                        SkipLimits(image, ref offset, end);
                        break;
                    case MemoryExternalKind:
                        SkipLimits(image, ref offset, end);
                        break;
                    case GlobalExternalKind:
                        ReadByte(image, ref offset, end);
                        ReadByte(image, ref offset, end);
                        break;
                    case TagExternalKind:
                        ReadByte(image, ref offset, end);
                        ReadUleb32(image, ref offset, end);
                        break;
                    default:
                        throw new InvalidDataException($"Unknown WebAssembly import kind {kind}.");
                }
            }

            return functionCount;
        }

        private static Dictionary<string, int> ReadFunctionExports(
            ReadOnlySpan<byte> image,
            ref int offset,
            int end)
        {
            uint exportCount = ReadUleb32(image, ref offset, end);
            Dictionary<string, int> functionExports = new(StringComparer.Ordinal);
            for (uint i = 0; i < exportCount; i++)
            {
                string name = ReadName(image, ref offset, end);
                byte kind = ReadByte(image, ref offset, end);
                int index = checked((int)ReadUleb32(image, ref offset, end));
                if (kind == FunctionExternalKind)
                    functionExports.Add(name, index);
            }

            return functionExports;
        }

        private static Dictionary<int, string> ReadSymbols(byte[] symbolsBytes)
        {
            Dictionary<int, string> symbols = new();
            string[] lines = Encoding.UTF8.GetString(symbolsBytes)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                int separator = line.IndexOf(':');
                if (separator <= 0 || !int.TryParse(line.AsSpan(0, separator), out int index))
                    throw new InvalidDataException($"Invalid Emscripten symbol map entry '{line}'.");

                if (!symbols.TryAdd(index, line[(separator + 1)..]))
                    throw new InvalidDataException($"Duplicate function index {index} in the Emscripten symbol map.");
            }

            return symbols;
        }

        private static void SkipLimits(ReadOnlySpan<byte> image, ref int offset, int end)
        {
            uint flags = ReadUleb32(image, ref offset, end);
            ReadUleb32(image, ref offset, end);
            if ((flags & 1) != 0)
                ReadUleb32(image, ref offset, end);
        }

        private static string ReadName(ReadOnlySpan<byte> image, ref int offset, int end)
        {
            int length = checked((int)ReadUleb32(image, ref offset, end));
            if (length > end - offset)
                throw new InvalidDataException("WebAssembly name extends past the end of its section.");

            string value = Encoding.UTF8.GetString(image.Slice(offset, length));
            offset += length;
            return value;
        }

        private static uint ReadUleb32(ReadOnlySpan<byte> image, ref int offset, int end)
        {
            uint value = 0;
            int shift = 0;
            while (shift < 35)
            {
                byte current = ReadByte(image, ref offset, end);
                value |= (uint)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                    return value;

                shift += 7;
            }

            throw new InvalidDataException("Invalid WebAssembly ULEB128 value.");
        }

        private static byte ReadByte(ReadOnlySpan<byte> image, ref int offset, int end)
        {
            if ((uint)offset >= (uint)end)
                throw new InvalidDataException("Unexpected end of WebAssembly section.");

            return image[offset++];
        }
    }
}
