// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmNativeDefaultsTests : TestMainJsTestBase
    {
        public WasmNativeDefaultsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static TheoryData<string, string, bool, bool, bool> SettingDifferentFromValuesInRuntimePack(bool forPublish)
        {
            List<(string propertyName, bool defaultValueInRuntimePack)> defaults = new()
            {
                ("WasmEnableLegacyJsInterop", true),
                ("WasmEnableSIMD", true),
                ("WasmEnableExceptionHandling", true),
                ("InvariantTimezone", false),
                ("InvariantGlobalization", false),
                // ("WasmNativeStrip", true) -- tested separately because it has special handling in targets
            };

            TheoryData<string, string, bool, bool, bool> data = new();

            string[] configs = new[] { "Debug", "Release" };
            foreach (var defaultPair in defaults)
            {
                foreach (string config in configs)
                {
                    // Config=Release always causes relinking when publishing
                    bool publishValue = forPublish && config == "Release" ? true : false;
                    // Setting the default value from the runtime pack shouldn't trigger relinking
                    data.Add(config, $"<{defaultPair.propertyName}>{defaultPair.defaultValueInRuntimePack}</{defaultPair.propertyName}>",
                                        /*aot*/ false, /*build*/ false, /*publish*/ publishValue);
                    // Leaving the property unset, so checking the default
                    data.Add(config, "", /*aot*/ false, /*build*/ false, /*publish*/ publishValue);

                    // Setting the !default value should trigger relinking
                    data.Add(config, $"<{defaultPair.propertyName}>{!defaultPair.defaultValueInRuntimePack}</{defaultPair.propertyName}>",
                                        /*aot*/ false, /*build*/ true, /*publish*/ true);
                }
            }

            return data;
        }
        public static TheoryData<string, string, bool, bool, bool> DefaultsTestData(bool forPublish)
        {
            TheoryData<string, string, bool, bool, bool> data = new()
            {
                /* relink by default for publish+Release */
                { "Release",   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      true },
                /* NO relink by default for publish+Release, when not trimming */
                { "Release",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ false,   /*build*/ false, /*publish*/      false },

                /* When not trimming, and no-aot, we don't relink. But WasmNativeStrip=false should still trigger it*/
                // { "Release",   "<WasmNativeStrip>false</WasmNativeStrip><PublishTrimmed>false</PublishTrimmed>",
                                                                    //    /*aot*/ false,   /*build*/ true,  /*publish*/      true }
            };

            if (!forPublish)
            {
                /* Debug config, when building does trigger relinking */
                data.Add("Debug",     "",                                         /*aot*/ false,   /*build*/ false,  /*publish*/      true);
            }

            if (forPublish)
            {
                /* NO relink by default for publish+Debug */
                data.Add("Debug",   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      false);

                /* AOT */
                data.Add("Release",   "",                                       /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add("Debug",     "",                                       /*aot*/ true,    /*build*/ false, /*publish*/      true);

                // FIXME: separate test
                //     { "Release",   "<RunAOTCompilationAfterBuild>true</RunAOTCompilationAfterBuild>",
                //  /*aot*/ true,    /*build*/ true, /*publish*/      true },

                /* AOT not affected by trimming */
                data.Add("Release", "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add("Debug",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: false)]
        [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: false)]
        public void DefaultsWithBuild(string config, string extraProperties, bool aot, bool expectWasmBuildNativeForBuild, bool expectWasmBuildNativeForPublish)
        {
            string output = CheckWasmNativeDefaultValue("native_defaults_build", config, extraProperties, aot, dotnetWasmFromRuntimePack: !expectWasmBuildNativeForPublish, publish: false);

            bool expectedWasmNativeStripValue = true;
            if (/*isBuild && */ expectWasmBuildNativeForBuild && config == "Debug")
                expectedWasmNativeStripValue = false;

            // bool expectedWasmNativeStripValue = !(wasmBuildNativeForBuild && config == "Debug");
            // for build
            Assert.Contains($"** WasmBuildNative: '{expectWasmBuildNativeForBuild.ToString().ToLower()}', WasmNativeStrip: '{expectedWasmNativeStripValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains("Stopping the build", output);
        }

#pragma warning disable xUnit1026 // For unused *buildValue* parameter
        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: true)]
        [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: true)]
        public void DefaultsWithPublish(string config, string extraProperties, bool aot, bool expectWasmBuildNativeForBuild, bool expectWasmBuildNativeForPublish)
        {
            string output = CheckWasmNativeDefaultValue("native_defaults_publish", config, extraProperties, aot, dotnetWasmFromRuntimePack: !expectWasmBuildNativeForPublish, publish: true);

            // for build
            // Assert.DoesNotContain($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmNativeStrip: 'true', WasmBuildingForNestedPublish: ''", output);
            // for publish
            Assert.Contains($"** WasmBuildNative: '{expectWasmBuildNativeForPublish.ToString().ToLower()}', WasmNativeStrip: 'true', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }
#pragma warning restore xunit1026

        public static TheoryData<string, string, bool, bool> SetWasmNativeStripExplicitlyTestData(bool publish) => new()
        {
            {"Debug", "<WasmNativeStrip>true</WasmNativeStrip>", false, true },
            {"Release", "<WasmNativeStrip>true</WasmNativeStrip>", publish, true },
            {"Debug", "<WasmNativeStrip>false</WasmNativeStrip>", true, false },
            {"Release", "<WasmNativeStrip>false</WasmNativeStrip>", true, false }
        };

        public static TheoryData<string, string, bool, bool> SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData() => new()
        {
            { "Debug",   "<WasmNativeStrip>false</WasmNativeStrip><WasmEnableLegacyJsInterop>false</WasmEnableLegacyJsInterop>", true, false },
            { "Release", "<WasmNativeStrip>false</WasmNativeStrip><WasmEnableLegacyJsInterop>false</WasmEnableLegacyJsInterop>", true, false },
            { "Debug",   "<WasmNativeStrip>true</WasmNativeStrip><WasmEnableLegacyJsInterop>false</WasmEnableLegacyJsInterop>", true, true },
            { "Release", "<WasmNativeStrip>true</WasmNativeStrip><WasmEnableLegacyJsInterop>false</WasmEnableLegacyJsInterop>", true, true }
        };

        [Theory]
        [MemberData(nameof(SetWasmNativeStripExplicitlyTestData), parameters: /*publish*/ false)]
        [MemberData(nameof(SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData))]
        public void WasmNativeStripDefaultWithBuild(string config, string extraProperties, bool expectedWasmBuildNativeValue, bool expectedWasmNativeStripValue)
        {
            string output = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, dotnetWasmFromRuntimePack: !expectedWasmBuildNativeValue, publish: false);

            Assert.Contains($"** WasmBuildNative: '{expectedWasmBuildNativeValue.ToString().ToLower()}', WasmNativeStrip: '{expectedWasmNativeStripValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains("Stopping the build", output);
        }

        [Theory]
        [MemberData(nameof(SetWasmNativeStripExplicitlyTestData), parameters: /*publish*/ true)]
        [MemberData(nameof(SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData))]
        public void WasmNativeStripDefaultWithPublish(string config, string extraProperties, bool expectedWasmBuildNativeValue, bool expectedWasmNativeStripValue)
        {
            string output = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, dotnetWasmFromRuntimePack: !expectedWasmBuildNativeValue, publish: true);

            Assert.Contains($"** WasmBuildNative: '{expectedWasmBuildNativeValue.ToString().ToLower()}', WasmNativeStrip: '{expectedWasmNativeStripValue.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }

        [Theory]
        /* always relink */
        [InlineData("Debug",   "",   /*build*/ true, /*publish*/ true)]
        [InlineData("Release", "",   /*build*/ true, /*publish*/ true)]
        [InlineData("Release", "<PublishTrimmed>false</PublishTrimmed>", /*build*/ true, /*publish*/ true)]
        public void WithNativeReference(string config, string extraProperties, bool buildValue, bool publishValue)
        {
            string nativeLibPath = Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o");
            string nativeRefItem = @$"<NativeFileReference Include=""{nativeLibPath}"" />";
            string output = CheckWasmNativeDefaultValue("native_defaults_publish",
                                                        config,
                                                        extraProperties,
                                                        aot: false,
                                                        dotnetWasmFromRuntimePack: !publishValue,
                                                        publish: true,
                                                        extraItems: nativeRefItem);

            // for build - FIXME:
             Assert.DoesNotContain($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            // for publish
            Assert.Contains($"** WasmBuildNative: '{publishValue.ToString().ToLower()}', WasmNativeStrip: 'true', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }

        private string CheckWasmNativeDefaultValue(string projectName,
                                                   string config,
                                                   string extraProperties,
                                                   bool aot,
                                                   bool dotnetWasmFromRuntimePack,
                                                   bool publish,
                                                   string extraItems = "")
        {
            // builds with -O0
            extraProperties += "<_WasmDevel>true</_WasmDevel>";

            string printValueTarget = @"
                <Target Name=""PrintWasmBuildNative"" AfterTargets=""_BeforeWasmBuildApp"">
                    <Message Text=""** WasmBuildNative: '$(WasmBuildNative)', WasmNativeStrip: '$(WasmNativeStrip)', WasmBuildingForNestedPublish: '$(WasmBuildingForNestedPublish)'"" Importance=""High"" />
                " + (publish
                        ? @"<Error Text=""Stopping the build"" Condition=""$(WasmBuildingForNestedPublish) == 'true'"" />"
                        : @"<Error Text=""Stopping the build"" />")
                + "</Target>";

            BuildArgs buildArgs = new(ProjectName: projectName, Config: config, AOT: aot, string.Empty, null);
            buildArgs = ExpandBuildArgs(buildArgs,
                                        extraProperties: extraProperties,
                                        extraItems: extraItems,
                                        insertAtEnd: printValueTarget);

            (_, string output) = BuildProject(buildArgs,
                                                id: GetRandomId(),
                                                new BuildProjectOptions(
                                                    InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                                    DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                                                    ExpectSuccess: false,
                                                    UseCache: false,
                                                    BuildOnlyAfterPublish: false,
                                                    Publish: publish));

            return output;
        }
    }
}
