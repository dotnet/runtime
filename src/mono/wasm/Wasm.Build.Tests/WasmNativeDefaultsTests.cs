// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmNativeDefaultsTests : BuildTestBase
    {
        public WasmNativeDefaultsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static TheoryData<string, string, bool, bool, bool> DefaultsTestData(bool forPublish)
        {
            TheoryData<string, string, bool, bool, bool> data = new()
            {
                /* relink by default for publish+Release */
                { "Release",   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      true },
                /* NO relink by default for publish+Release, when not trimming */
                { "Release",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ false,   /*build*/ false, /*publish*/      false },

                /* WasmNativeStrip=false should cause relink */
                { "Debug",     "<WasmNativeStrip>false</WasmNativeStrip>", /*aot*/ false,   /*build*/ true, /*publish*/       true },
                { "Release",   "<WasmNativeStrip>false</WasmNativeStrip>", /*aot*/ false,   /*build*/ true, /*publish*/       true },
                /* WasmNativeStrip=true should not trigger relinking */
                { "Debug",     "<WasmNativeStrip>true</WasmNativeStrip>",  /*aot*/ false,   /*build*/ false, /*publish*/      false },
                { "Release",   "<WasmNativeStrip>true</WasmNativeStrip>",  /*aot*/ false,   /*build*/ false, /*publish*/      true },
                /* When not trimming, and no-aot, we don't relink. But WasmNativeStrip=false should still trigger it*/
                { "Release",   "<WasmNativeStrip>false</WasmNativeStrip><PublishTrimmed>false</PublishTrimmed>",
                                                                       /*aot*/ false,   /*build*/ true,  /*publish*/      true }
            };

            if (!forPublish)
            {
                /* Debug config, when building does trigger relinking */
                data.Add("Debug",     "",                                         /*aot*/ false,   /*build*/ false,  /*publish*/      true);
            }

            if (forPublish)
            {
                /* NO relink by default for publish+Debug */
                data.Add("Debug",   "",                                           /*aot*/ false,   /*build*/ false, /*publish*/      false);

                /* AOT */
                data.Add( "Release",   "",                                         /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add( "Debug",     "",                                         /*aot*/ true,    /*build*/ false, /*publish*/      true);

                // FIXME: separate test
                //     { "Release",   "<RunAOTCompilationAfterBuild>true</RunAOTCompilationAfterBuild>",
                //  /*aot*/ true,    /*build*/ true, /*publish*/      true },

                /* AOT not affected by trimming */
                data.Add("Release",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
                data.Add("Debug",     "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: true)]
        public void DefaultsWithPublish(string config, string extraProperties, bool aot, bool buildValue, bool publishValue)
        {
            string output = CheckWasmNativeDefaultValue("native_defaults_publish", config, extraProperties, aot, dotnetWasmFromRuntimePack: !publishValue, publish: true);

            // for build
            Assert.DoesNotContain($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            // for publish
            Assert.Contains($"** WasmBuildNative: '{publishValue.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }

        [Theory]
        [MemberData(nameof(DefaultsTestData), parameters: false)]
        public void DefaultsWithBuild(string config, string extraProperties, bool aot, bool buildValue, bool publishValue)
        {
            string output = CheckWasmNativeDefaultValue("native_defaults_publish", config, extraProperties, aot, dotnetWasmFromRuntimePack: !publishValue, publish: false);

            // for build
            Assert.Contains($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains("Stopping the build", output);
        }

        /*
         * build/publish: if buildnative for any reason .. then strip=false
         *
         *
         */
        [Theory]
        //[MemberData(nameof(DefaultsTestData), parameters: false)]
        [InlineData("Debug", "", false, true)]
        [InlineData("Release", "", false, true)]
        [InlineData("Debug", "<WasmBuildNative>true</WasmBuildNative>", true, false)]
        [InlineData("Release", "<WasmBuildNative>true</WasmBuildNative>", true, true)]
        // Explicitly setting WasmNativeStrip=false
        [InlineData("Debug", "<WasmNativeStrip>false</WasmNativeStrip>", true, false)]
        [InlineData("Release", "<WasmNativeStrip>false</WasmNativeStrip>", true, false)]
        public void WasmNativeStripDefaultWithBuild(string config, string extraProperties, bool expectedWasmBuildNative, bool expectedWasmNativeStrip)
        {
            string output = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, dotnetWasmFromRuntimePack: !expectedWasmBuildNative, publish: false);

            // for build
            Assert.Contains($"** WasmNativeStrip: '{expectedWasmNativeStrip.ToString().ToLower()}'", output);
            Assert.Contains($"** WasmBuildNative: '{expectedWasmBuildNative.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains("Stopping the build", output);
        }

        [Theory]
        //[MemberData(nameof(DefaultsTestData), parameters: false)]
        [InlineData("Debug", "", false, true)]
        [InlineData("Release", "", true, true)]
        [InlineData("Debug", "<WasmBuildNative>true</WasmBuildNative>", true, false)]
        // Explicitly setting WasmNativeStrip=false
        [InlineData("Debug", "<WasmNativeStrip>false</WasmNativeStrip>", true, false)]
        [InlineData("Release", "<WasmNativeStrip>false</WasmNativeStrip>", true, false)]
        // with aot
        [InlineData("Debug", "<RunAOTCompilation>true</RunAOTCompilation>", true, false)]
        [InlineData("Release", "<RunAOTCompilation>true</RunAOTCompilation>", true, true)]
        public void WasmNativeStripDefaultWithPublish(string config, string extraProperties, bool expectedWasmBuildNative, bool expectedWasmNativeStrip)
        {
            string output = CheckWasmNativeDefaultValue("native_strip_defaults", config, extraProperties, aot: false, dotnetWasmFromRuntimePack: !expectedWasmBuildNative, publish: true);

            // for build
            Assert.Contains($"** WasmNativeStrip: '{expectedWasmNativeStrip.ToString().ToLower()}'", output);
            Assert.Contains($"** WasmBuildNative: '{expectedWasmBuildNative.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }

        [Theory]
        /* always relink */
        [InlineData("Release", "",   /*build*/ true, /*publish*/ true)]
        [InlineData("Debug",   "",   /*build*/ true, /*publish*/ true)]
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

            // for build
            Assert.DoesNotContain($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            // for publish
            Assert.Contains($"** WasmBuildNative: '{publishValue.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
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
                <Target Name=""PrintWasmBuildNative"" AfterTargets=""_SetWasmNativeStripDefault"">
                    <Message Text=""** WasmNativeStrip: '$(WasmNativeStrip)'"" Importance=""High"" />
                    <Message Text=""** WasmBuildNative: '$(WasmBuildNative)', WasmBuildingForNestedPublish: '$(WasmBuildingForNestedPublish)'"" Importance=""High"" />
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
                                                id: Path.GetRandomFileName(),
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
