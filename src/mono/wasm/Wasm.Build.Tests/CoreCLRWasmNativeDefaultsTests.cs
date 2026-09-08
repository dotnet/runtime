// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
            ProjectInfo info = CopyTestAsset(
                Configuration.Debug,
                aot: false,
                TestAsset.WasmBasicTestApp,
                "coreclr_sdk_crossgen2",
                extraProperties: $$"""
                    <PublishReadyToRun>false</PublishReadyToRun>
                    <WasmBuildNative>true</WasmBuildNative>
                    <WasmBuildOnlyAfterPublish>{{publish}}</WasmBuildOnlyAfterPublish>
                    """,
                insertAtEnd: $$"""
                    <Target Name="CheckCrossgen2PackRequest" BeforeTargets="ProcessFrameworkReferences">
                        <Error Condition="'$(RequiresCrossgen2Pack)' != 'true'"
                               Text="Expected RequiresCrossgen2Pack=true before restore." />
                    </Target>
                    <Target Name="CheckSdkCrossgen2" AfterTargets="_CoreCLRGenerateManagedToNative">
                        <Error Condition="'{{publish}}' == 'true' and '$(WasmBuildingForNestedPublish)' != 'true'"
                               Text="Expected helper generation during nested publish." />
                        <Error Condition="'$(PublishReadyToRun)' == 'true'"
                               Text="Expected PublishReadyToRun=false." />
                        <Error Condition="'$(Crossgen2InBuildDir)' != '' or '$(Crossgen2ToolPath)' != ''"
                               Text="Expected SDK resolution without crossgen2 path overrides." />
                        <Error Condition="'@(Crossgen2Tool)' == '' or '$(PortableCallHelpersGeneratorPath)' != '@(Crossgen2Tool)'"
                               Text="Expected the generator path to match the SDK Crossgen2Tool item." />
                        <Error Condition="!Exists('$(_WasmPInvokeTablePath)') or !Exists('$(_WasmReversePInvokeTablePath)') or !Exists('$(_WasmInterpToNativeTablePath)')"
                               Text="A generated call-helper table is missing." />
                        <Error Text="Stopping after validating SDK crossgen2" />
                    </Target>
                    """);

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
                extraProperties: $$"""
                    <PublishReadyToRun>false</PublishReadyToRun>
                    <RequiresCrossgen2Pack>false</RequiresCrossgen2Pack>
                    <WasmBuildNative>true</WasmBuildNative>
                    <WasmBuildOnlyAfterPublish>{{publish}}</WasmBuildOnlyAfterPublish>
                    """);

            string output = publish
                ? PublishProject(info, Configuration.Debug, new PublishOptions(ExpectSuccess: false)).buildOutput
                : BuildProject(info, Configuration.Debug, new BuildOptions(ExpectSuccess: false)).buildOutput;

            Assert.Contains("Could not resolve crossgen2. Update the .NET SDK and restore the project, or set $(PortableCallHelpersGeneratorPath) to a crossgen2 executable.", output);
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
}
