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

        [Theory]
        /* relink by default for publish+Release */
        [InlineData("Release",   "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      true)]
        /* NO relink by default for publish+Release, even when not trimming */
        [InlineData("Release",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ false,   /*build*/ false, /*publish*/      false)]

        [InlineData("Debug",     "",                                         /*aot*/ false,   /*build*/ false, /*publish*/      false)]

        /* AOT */
        [InlineData("Release",   "",                                         /*aot*/ true,    /*build*/ false, /*publish*/      true)]
        [InlineData("Debug",     "",                                         /*aot*/ true,    /*build*/ false, /*publish*/      true)]
        // FIXME: separate test
        // [InlineData("Release",   "<RunAOTCompilationAfterBuild>true</RunAOTCompilationAfterBuild>",
                                                                            //  /*aot*/ true,    /*build*/ true, /*publish*/      true)]

        /* AOT not affected by trimming */
        [InlineData("Release",   "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true)]
        [InlineData("Debug",     "<PublishTrimmed>false</PublishTrimmed>",   /*aot*/ true,    /*build*/ false, /*publish*/      true)]
        public void Defaults(string config, string extraProperties, bool aot, bool buildValue, bool publishValue)
        {
            string output = CheckWasmNativeDefaultValue("native_defaults_publish", config, extraProperties, aot, dotnetWasmFromRuntimePack: !publishValue);

            Assert.Contains($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains($"** WasmBuildNative: '{publishValue.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
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
                                                        extraItems: nativeRefItem);

            Assert.Contains($"** WasmBuildNative: '{buildValue.ToString().ToLower()}', WasmBuildingForNestedPublish: ''", output);
            Assert.Contains($"** WasmBuildNative: '{publishValue.ToString().ToLower()}', WasmBuildingForNestedPublish: 'true'", output);
            Assert.Contains("Stopping the build", output);
        }

        private string CheckWasmNativeDefaultValue(string projectName,
                                                   string config,
                                                   string extraProperties,
                                                   bool aot,
                                                   bool dotnetWasmFromRuntimePack,
                                                   string extraItems = "")
        {
            // builds with -O0
            extraProperties += "<_WasmDevel>true</_WasmDevel>";

            string printValueTarget = @"
                <Target Name=""PrintWasmBuildNative"" AfterTargets=""_SetWasmBuildNativeDefaults"">
                    <Message Text=""** WasmBuildNative: '$(WasmBuildNative)', WasmBuildingForNestedPublish: '$(WasmBuildingForNestedPublish)'"" Importance=""High"" />
                    <Error Text=""Stopping the build"" Condition=""$(WasmBuildingForNestedPublish) == 'true'"" />
                </Target>";

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
                                                    BuildOnlyAfterPublish: false));

            return output;
        }
    }
}
