// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantTimezoneTests : WasmTemplateTestsBase
    {
        public InvariantTimezoneTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantTimezoneTestData(bool aot)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, })]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ true })]
        [TestCategory("native"), TestCategory("mono")]
        public async Task AOT_InvariantTimezone(Configuration config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone);

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false })]
        [TestCategory("native")]
        public async Task RelinkingWithoutAOT(Configuration config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone, isNativeBuild: true);

        [Fact]
        [TestCategory("native")]
        public async Task CustomTimezoneDataIsEmbeddedAndRebuilt()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "custom_timezone",
                extraProperties: "<WasmCustomTimezoneDataDirectory>custom timezone data</WasmCustomTimezoneDataDirectory>");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "CustomTimezone.cs"));
            string directory = Path.Combine(_projectDir, "custom timezone data");
            WriteTimezoneData(directory, 123);

            var options = new PublishOptions(UseCache: false);
            PublishProject(info, config, options, isNativeBuild: true);
            await CheckCustomTimezone("02:03:00", added: false);

            string objectPath = Path.Combine(GetBuildPaths(config, forPublish: true).ObjWasmDir, "timezone", "timezone-data.o");
            Assert.True(File.Exists(objectPath), $"Expected timezone object at {objectPath}");
            DateTime timestamp = File.GetLastWriteTimeUtc(objectPath);
            PublishProject(info, config, options, isNativeBuild: true);
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(objectPath));

            WriteTimezoneData(directory, -234);
            PublishProject(info, config, options, isNativeBuild: true);
            await CheckCustomTimezone("-03:54:00", added: false);

            File.Copy(Path.Combine(directory, "Custom", "Test"), Path.Combine(directory, "Custom", "Added"));
            File.AppendAllText(Path.Combine(directory, "zone.tab"), "XX\t+0000+00000\tCustom/Added\n");
            PublishProject(info, config, options, isNativeBuild: true);
            await CheckCustomTimezone("-03:54:00", added: true);

            File.Delete(Path.Combine(directory, "Custom", "Added"));
            File.WriteAllText(Path.Combine(directory, "zone.tab"), "XX\t+0000+00000\tCustom/Test\n");
            PublishProject(info, config, options, isNativeBuild: true);
            await CheckCustomTimezone("-03:54:00", added: false);

            string alternateDirectory = Path.Combine(_projectDir, "alternate timezone data");
            WriteTimezoneData(alternateDirectory, 345);
            PublishProject(info, config, options with { ExtraMSBuildArgs = "-p:WasmCustomTimezoneDataDirectory=\"alternate timezone data\"" }, isNativeBuild: true);
            await CheckCustomTimezone("05:45:00", added: false);

            PublishProject(info, config, options with { ExtraMSBuildArgs = "-p:WasmCustomTimezoneDataDirectory= -p:WasmBuildNative=true" }, isNativeBuild: true);
            RunResult restored = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42));
            Assert.Contains("Asia/Tokyo: 09:00:00", restored.TestOutput);
            Assert.Contains("Custom/Test: not found", restored.TestOutput);

            async Task CheckCustomTimezone(string offset, bool added)
            {
                RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42));
                Assert.Contains($"Custom/Test: {offset}", result.TestOutput);
                Assert.Contains("Enumerated Custom/Test: True", result.TestOutput);
                Assert.Contains($"Enumerated Custom/Added: {added}", result.TestOutput);
                Assert.Contains(added ? $"Custom/Added: {offset}" : "Custom/Added: not found", result.TestOutput);
                Assert.Contains("Asia/Tokyo: not found", result.TestOutput);
                Assert.Contains("UTC: 00:00:00", result.TestOutput);
            }
        }

        [Theory]
        [InlineData("<WasmBuildNative>false</WasmBuildNative>", "requires native relinking", true, null)]
        [InlineData("<InvariantTimezone>true</InvariantTimezone>", "cannot be used with InvariantTimezone=true", true, null)]
        [InlineData("", "must contain zone.tab and compiled TZif files", true, null)]
        [InlineData("", "does not exist", false, null)]
        [InlineData("", "must use only letters, digits", true, "bad,name")]
        [TestCategory("native")]
        public void CustomTimezoneDataRejectsInvalidConfiguration(string extraProperties, string error, bool createDirectory, string? invalidFile)
        {
            ProjectInfo info = CopyTestAsset(Configuration.Debug, false, TestAsset.WasmBasicTestApp, "invalid_custom_timezone",
                extraProperties: $"<WasmCustomTimezoneDataDirectory>timezone-data</WasmCustomTimezoneDataDirectory>{extraProperties}");
            string directory = Path.Combine(_projectDir, "timezone-data");
            if (createDirectory)
                Directory.CreateDirectory(directory);
            if (invalidFile is not null)
            {
                WriteTimezoneData(directory, 123);
                File.WriteAllText(Path.Combine(directory, invalidFile), "");
            }
            (string _, string output) = PublishProject(info, Configuration.Debug, new PublishOptions(ExpectSuccess: false));
            Assert.Contains(error, output);
        }

        private static void WriteTimezoneData(string directory, int offsetMinutes)
        {
            Directory.CreateDirectory(Path.Combine(directory, "Custom"));
            byte[] data = new byte[54];
            "TZif"u8.CopyTo(data);
            BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(36), 1);
            BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(40), 4);
            BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(44), offsetMinutes * 60);
            "TST\0"u8.CopyTo(data.AsSpan(50));
            File.WriteAllBytes(Path.Combine(directory, "Custom", "Test"), data);
            File.WriteAllText(Path.Combine(directory, "zone.tab"), "XX\t+0000+00000\tCustom/Test\n");
        }

        private async Task TestInvariantTimezone(Configuration config, bool aot, bool? invariantTimezone, bool? isNativeBuild = null)
        {
            string extraProperties = isNativeBuild == true ? "<WasmBuildNative>true</WasmBuildNative>" : "";
            if (invariantTimezone != null)
            {
                extraProperties = $"{extraProperties}<InvariantTimezone>{invariantTimezone}</InvariantTimezone>";
            }
            if (invariantTimezone == true)
            {
                if (isNativeBuild == false)
                    throw new System.ArgumentException("InvariantTimezone=true requires a native build");
                // -p:InvariantTimezone=true triggers native build, isNativeBuild is not undefined anymore
                isNativeBuild = true;
            }

            string prefix = $"invariant_{invariantTimezone?.ToString() ?? "unset"}";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraProperties: extraProperties);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "InvariantTimezone.cs"));
            PublishProject(info, config, isNativeBuild: isNativeBuild);

            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 42));
            Assert.Contains(output.TestOutput, m => m.Contains("UTC BaseUtcOffset is 0"));
            if (invariantTimezone == true)
            {
                Assert.Contains(output.TestOutput, m => m.Contains("Could not find Asia/Tokyo"));
            }
            else
            {
                Assert.Contains(output.TestOutput, m => m.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00"));
            }
        }
    }
}
