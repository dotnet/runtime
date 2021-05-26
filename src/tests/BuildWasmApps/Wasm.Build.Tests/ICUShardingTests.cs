// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using System.Globalization;

#nullable enable

namespace Wasm.Build.Tests
{
    public class ICUShardingTests : BuildTestBase
    {
        public ICUShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> ICUShardingTestData_EFIGS(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { "en", "en-GB", false},
                    new object?[] { "es", "fr-FR", false},
                    new object?[] { "en", "zh", true},
                    new object?[] {"es-ES", "am-ET", true})
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        public static IEnumerable<object?[]> ICUShardingTestData_CJK(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { "zh", "zh-Hans", false},
                    new object?[] { "ko", "en-US", false},
                    new object?[] { "ja-JP", "es-ES", true},
                    new object?[] { "ja-JP", "fr-FR", true})
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        public static IEnumerable<object?[]> ICUShardingTestData_no_CJK(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { "am-ET", "de-DE", false},
                    new object?[] { "am-ET", "vi-VN", false},
                    new object?[] { "am-ET", "ko", true},
                    new object?[] { "am-ET", "ja-JP", true})
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(ICUShardingTestData_EFIGS), parameters: new object[] { true, RunHost.All })]
        [MemberData(nameof(ICUShardingTestData_EFIGS), parameters: new object[] { false, RunHost.All })]
        [MemberData(nameof(ICUShardingTestData_CJK), parameters: new object[] { true, RunHost.All })]
        [MemberData(nameof(ICUShardingTestData_CJK), parameters: new object[] { false, RunHost.All })]
        [MemberData(nameof(ICUShardingTestData_no_CJK), parameters: new object[] { true, RunHost.All })]
        [MemberData(nameof(ICUShardingTestData_no_CJK), parameters: new object[] { false, RunHost.All })]
        public void ShardingTests(BuildArgs buildArgs, string defaultCulture, string testCulture, bool expectToThrow, RunHost host, string id)
            => TestICUSharding(buildArgs, defaultCulture, testCulture, expectToThrow, host, id);
        
        void TestICUSharding(BuildArgs buildArgs,
                             string defaultCulture,
                             string testCulture,
                             bool expectToThrow,
                             RunHost host,
                             string id,
                             string projectContents="",
                             bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"sharding_{defaultCulture}_{testCulture}";
            string programText = $@"
                using System;
                using System.Globalization;
                using System.Text;

                public class TestClass {{
                    public static int Main()
                    {{
                        try {{
                            var culture = new CultureInfo(""{testCulture}"", false);
                            string s = new string( new char[] {{'\u0063', '\u0301', '\u0327', '\u00BE'}});
                            string normalized = s.Normalize();
                            Console.WriteLine($""{{culture.NativeName}} - {{culture.NumberFormat.CurrencySymbol}} - {{culture.DateTimeFormat.FullDateTimePattern}} - {{culture.CompareInfo.LCID}} - {{normalized.IsNormalized(NormalizationForm.FormC)}}"");
                        }}
                        catch (CultureNotFoundException e){{
                            Console.WriteLine($""Culture Not Found {{e.Message}}"");
                        }}
                        
                        return 42;
                    }}
                }}";

            buildArgs = buildArgs with { ProjectName = projectName, ProjectFileContents = programText };
            buildArgs = GetBuildArgsWith(buildArgs, extraProperties: $"<EnableSharding>true</EnableSharding><ICUDefaultCulture>{defaultCulture}</ICUDefaultCulture>");
            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack);

            var culture = CultureInfo.GetCultureInfo(testCulture);

            string expectedOutputString = expectToThrow == true
                                            ? "Culture Not Found"
                                            : $"{culture.NativeName} - {culture.NumberFormat.CurrencySymbol} - {culture.DateTimeFormat.FullDateTimePattern} - {culture.CompareInfo.LCID} - True";

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                test: output => Assert.Contains(expectedOutputString, output), host: host, id: id);
        }
    }
}