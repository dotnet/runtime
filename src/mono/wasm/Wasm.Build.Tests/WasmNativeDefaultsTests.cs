// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmNativeDefaultsTests : WasmTemplateTestsBase
    {
        private static Regex s_regex = new("\\*\\* WasmBuildNative:.*");
        public WasmNativeDefaultsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static TheoryData<Configuration, string, bool, bool, bool> SettingDifferentFromValuesInRuntimePack(bool forPublish)
        {
            List<(string propertyName, bool defaultValueInRuntimePack)> defaults = new()
            {
                ("WasmEnableSIMD", true),
                ("WasmEnableExceptionHandling", true),
                ("InvariantTimezone", false),
                ("InvariantGlobalization", false),
                // ("WasmNativeStrip", true) -- tested separately because it has special handling in targets
                // ("RunAOTCompilation", false) -- tested separately as it changes build behavior significantly
            };

            TheoryData<Configuration, string, bool, bool, bool> data = new();

            var configs = new[] { Configuration.Debug, Configuration.Release };
            foreach (var defaultPair in defaults)
            {
                foreach (Configuration config in configs)
                {
                    // Configuration=Release always causes relinking when publishing
                    bool publishValue = forPublish && config == Configuration.Release ? true : false;
                    // Setting the default value from the runtime pack shouldn't trigger relinking
                    data.Add(config, $"<{defaultPair.propertyName}>{defaultPair.defaultValueInRuntimePack.ToString().ToLower()}</{defaultPair.propertyName}>",
                                        /*aot*/ false, /*build*/ false, /*publish*/ publishValue);
                    // Leaving the property unset, so checking the default
                    data.Add(config, "", /*aot*/ false, /*build*/ false, /*publish*/ publishValue);

                    // Setting the !default value should trigger relinking
                    data.Add(config, $"<{defaultPair.propertyName}>{(!defaultPair.defaultValueInRuntimePack).ToString().ToLower()}</{defaultPair.propertyName}>",
                                        /*aot*/ false, /*build*/ true, /*publish*/ true);
                }
            }

            return data;
        }

        public static TheoryData<Configuration, string, bool, bool, bool> DefaultsTestData(bool forPublish)
        {
            TheoryData<Configuration, string, bool, bool, bool> data = new()
            {
                /* relink by default for publish+Release */
                { Configuration.Release,   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      true },
                /* NO relink by default for publish+Release, when not trimming */
                { Configuration.Release,   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ false,   /*build*/ false, /*publish*/      false },

                /* When not trimming, and no-aot, we don't relink. But WasmNativeStrip=false should still trigger it*/
                // { Configuration.Release,   "<WasmNativeStrip>false</WasmNativeStrip><PublishTrimmed>false</PublishTrimmed>",
                                                                    //    /*aot*/ false,   /*build*/ true,  /*publish*/      true }
            };

            if (!forPublish)
            {
                /* Debug config, when building does trigger relinking */
                data.Add(Configuration.Debug,     "",                                         /*aot*/ false,   /*build*/ false,  /*publish*/      true);
            }

            if (forPublish)
            {
                /* NO relink by default for publish+Debug */
                data.Add(Configuration.Debug,   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      false);

                /* AOT */
                data.Add(Configuration.Release,   "",                                       /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add(Configuration.Debug,     "",                                       /*aot*/ true,    /*build*/ false, /*publish*/      true);

                // FIXME: separate test
                //     { Configuration.Release,   "<RunAOTCompilationAfterBuild>true</RunAOTCompilationAfterBuild>",
                //  /*aot*/ true,    /*build*/ true, /*publish*/      true },

                /* AOT not affected by trimming */
                data.Add(Configuration.Release, "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add(Configuration.Debug,   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
            }

            return data;
        }

#pragma warning disable xUnit1026 // For unused *buildValue*, and *publishValue* parameters
        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: false)]
        [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: false)]
        public void DefaultsWithBuild(Configuration config, string extraProperties, bool aot, bool expectWasmBuildNativeForBuild, bool expectWasmBuildNativeForPublish)
        {
            (string output, string? line) = CheckWasmNativeDefaultValue("native_defaults_build", config, extraProperties, aot, expectWasmBuildNativeForBuild, isPublish: false);

            InferAndCheckPropertyValues(line, isPublish: false, wasmBuildNative: expectWasmBuildNativeForBuild, config: config);
        }

        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: true)]
        [MemberData(nameof(SettingDifferentFromValuesInRuntimePack), parameters: true)]
        public void DefaultsWithPublish(Configuration config, string extraProperties, bool aot, bool expectWasmBuildNativeForBuild, bool expectWasmBuildNativeForPublish)
        {
            (string output, string? line) = CheckWasmNativeDefaultValue("native_defaults_publish", config, extraProperties, aot, expectWasmBuildNativeForPublish, isPublish: true);

            InferAndCheckPropertyValues(line, isPublish: true, wasmBuildNative: expectWasmBuildNativeForPublish, config: config);
        }
#pragma warning restore xunit1026

        public static TheoryData<Configuration, string, bool, bool> SetWasmNativeStripExplicitlyTestData(bool publish) => new()
        {
            {Configuration.Debug, "<WasmNativeStrip>true</WasmNativeStrip>",    /*wasmBuildNative*/ false,   /*wasmNativeStrip*/ true },
            {Configuration.Release, "<WasmNativeStrip>true</WasmNativeStrip>",  /*wasmBuildNative*/ publish, /*wasmNativeStrip*/ true },
            {Configuration.Debug, "<WasmNativeStrip>false</WasmNativeStrip>",   /*wasmBuildNative*/ true,    /*wasmNativeStrip*/ false },
            {Configuration.Release, "<WasmNativeStrip>false</WasmNativeStrip>", /*wasmBuildNative*/ true,    /*wasmNativeStrip*/ false }
        };

        public static TheoryData<Configuration, string, bool, bool> SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData() => new()
        {
            { Configuration.Debug,   "<WasmNativeStrip>false</WasmNativeStrip><InvariantTimezone>true</InvariantTimezone>", true, false },
            { Configuration.Release, "<WasmNativeStrip>false</WasmNativeStrip><InvariantTimezone>true</InvariantTimezone>", true, false },
            { Configuration.Debug,   "<WasmNativeStrip>true</WasmNativeStrip><InvariantTimezone>true</InvariantTimezone>", true, true },
            { Configuration.Release, "<WasmNativeStrip>true</WasmNativeStrip><InvariantTimezone>true</InvariantTimezone>", true, true }
        };

        [Theory]
        [MemberData(nameof(SetWasmNativeStripExplicitlyTestData), parameters: /*publish*/ false)]
        [MemberData(nameof(SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData))]
        public void WasmNativeStripDefaultWithBuild(Configuration config, string extraProperties, bool expectedWasmBuildNativeValue, bool expectedWasmNativeStripValue)
        {
            (string output, string? line) = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, expectedWasmBuildNativeValue, isPublish: false);

            CheckPropertyValues(line,
                                wasmBuildNative: expectedWasmBuildNativeValue,
                                wasmNativeStrip: expectedWasmNativeStripValue,
                                wasmNativeDebugSymbols: true,
                                wasmBuildingForNestedPublish: null);
        }

        [Theory]
        [MemberData(nameof(SetWasmNativeStripExplicitlyTestData), parameters: /*publish*/ true)]
        [MemberData(nameof(SetWasmNativeStripExplicitlyWithWasmBuildNativeTestData))]
        public void WasmNativeStripDefaultWithPublish(Configuration config, string extraProperties, bool expectedWasmBuildNativeValue, bool expectedWasmNativeStripValue)
        {
            (string output, string? line) = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, expectedWasmBuildNativeValue, isPublish: true);

            CheckPropertyValues(line,
                                wasmBuildNative: expectedWasmBuildNativeValue,
                                wasmNativeStrip: expectedWasmNativeStripValue,
                                wasmNativeDebugSymbols: true,
                                wasmBuildingForNestedPublish: true);
        }

        [Theory]
        /* always relink */
        [InlineData(Configuration.Debug,   "",   /*publish*/ false)]
        [InlineData(Configuration.Debug,   "",   /*publish*/ true)]
        [InlineData(Configuration.Release, "",   /*publish*/ false)]
        [InlineData(Configuration.Release, "",   /*publish*/ true)]
        [InlineData(Configuration.Release, "<PublishTrimmed>false</PublishTrimmed>", /*publish*/ true)]
        public void WithNativeReference(Configuration config, string extraProperties, bool publish)
        {
            string nativeLibPath = Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o");
            string nativeRefItem = @$"<NativeFileReference Include=""{nativeLibPath}"" />";
            (string output, string? line) = CheckWasmNativeDefaultValue("native_defaults_publish",
                                                        config,
                                                        extraProperties,
                                                        aot: false,
                                                        nativeBuild: true,
                                                        isPublish: publish,
                                                        extraItems: nativeRefItem);

            InferAndCheckPropertyValues(line, isPublish: publish, wasmBuildNative: true, config: config);
        }

        private (string, string?) CheckWasmNativeDefaultValue(string projectPrefix,
                                                   Configuration config,
                                                   string extraProperties,
                                                   bool aot,
                                                   bool nativeBuild,
                                                   bool isPublish,
                                                   string extraItems = "")
        {
            // builds with -O0
            extraProperties += "<_WasmDevel>true</_WasmDevel>";

            string printValueTarget = @"
                <Target Name=""PrintWasmBuildNative"" AfterTargets=""PrepareInputsForWasmBuild"">
                    <Message Text=""** WasmBuildNative: '$(WasmBuildNative)', WasmNativeStrip: '$(WasmNativeStrip)', WasmNativeDebugSymbols: '$(WasmNativeDebugSymbols)', WasmBuildingForNestedPublish: '$(WasmBuildingForNestedPublish)'"" Importance=""High"" />
                " + (isPublish
                        ? @"<Error Text=""Stopping the build"" Condition=""$(WasmBuildingForNestedPublish) == 'true'"" />"
                        : @"<Error Text=""Stopping the build"" />")
                + "</Target>";
            ProjectInfo info = CopyTestAsset(
                    config,
                    aot,
                    TestAsset.WasmBasicTestApp,
                    projectPrefix,
                    extraProperties: extraProperties,
                    extraItems: extraItems,
                    insertAtEnd: printValueTarget);
            UpdateFile(Path.Combine("Common", "Program.cs"), s_mainReturns42);

            (string _, string output) = isPublish ?
                PublishProject(info, config, new PublishOptions(ExpectSuccess: false, AOT: aot), nativeBuild) :
                BuildProject(info, config, new BuildOptions(ExpectSuccess: false, AOT: aot), nativeBuild);
            Assert.Contains("Stopping the build", output);

            Match m = s_regex.Match(output);
            Assert.Equal(1, m.Groups.Count);
            return (output, m.Success ? m.Groups[0]?.ToString() : null);
        }

        private void InferAndCheckPropertyValues(string? line, bool isPublish, bool wasmBuildNative, Configuration config)
        {
            bool expectedWasmNativeStripValue;
            if (!isPublish && wasmBuildNative && config == Configuration.Debug)
                expectedWasmNativeStripValue = false;
            else
                expectedWasmNativeStripValue = true;

            CheckPropertyValues(line, wasmBuildNative, expectedWasmNativeStripValue, /*wasmNativeDebugSymbols*/false, isPublish);
        }

        private void CheckPropertyValues(string? line, bool wasmBuildNative, bool wasmNativeStrip, bool wasmNativeDebugSymbols, bool? wasmBuildingForNestedPublish)
        {
            Assert.NotNull(line);
            string expected = $"** WasmBuildNative: '{wasmBuildNative.ToString().ToLower()}', " +
                            $"WasmNativeStrip: '{wasmNativeStrip.ToString().ToLower()}', " +
                            $"WasmNativeDebugSymbols: '{wasmNativeDebugSymbols.ToString().ToLower()}', " +
                            $"WasmBuildingForNestedPublish: '{(wasmBuildingForNestedPublish.HasValue && wasmBuildingForNestedPublish == true ? "true" : "")}'";
            Assert.Contains(expected, line);
        }
    }
}
