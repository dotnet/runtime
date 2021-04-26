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

        public static IEnumerable<object?[]> ICUShardingTestData(bool enableSharding, RunHost host)
            => ConfigWithAOTData(false)
                .Multiply(
                    new object?[] { enableSharding, "en", "en-GB", false},      //EFIGS
                    new object?[] { enableSharding, "en", "zh", true},          //EFIGS
                    new object?[] { enableSharding, "es-ES", "am-ET", true},    //EFIGS
                    new object?[] { enableSharding, "es", "fr-FR", true},       //EFIGS
                    new object?[] { enableSharding, "zh", "zh-Hans", false},    //CJK
                    new object?[] { enableSharding, "ko", "zh-Hans", false},    //CJK
                    new object?[] { enableSharding, "ja-JP", "es-ES", true},    //CJK //this one should have thrown
                    new object?[] { enableSharding, "am-ET", "de-DE", false},   //no_CJK
                    new object?[] { enableSharding, "am-ET", "ko", true},       //no_CJK
                    new object?[] { enableSharding, "am-ET", "ja-JP", true},    //no_CJK
                    new object?[] { enableSharding, "am-ET", "vi-VN", false},   //no_CJK
                    new object?[] { enableSharding, null, "zh-Hans", true})     //ALL
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(ICUShardingTestData), parameters: new object[] { true, RunHost.All })]
        public void ShardingTests(BuildArgs buildArgs, bool enableSharding, string defaultCulture, string testCulture, bool expectToThrow, RunHost host, string id)
            => TestICUSharding(buildArgs, enableSharding, defaultCulture, testCulture, expectToThrow, host, id);
        
        void TestICUSharding(BuildArgs buildArgs,
                             bool enableSharding,
                             string defaultCulture,
                             string testCulture,
                             bool expectToThrow,
                             RunHost host,
                             string id,
                             bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"sharding_{enableSharding}_{defaultCulture}_{testCulture}";
            string programText = $@"
                using System;
                using System.Globalization;
                using System.Threading.Tasks;

                public class TestClass {{
                    public static int Main()
                    {{
                        try {{
                            var culture = new CultureInfo(""{testCulture}"", false);
                            Console.WriteLine(culture.Name);
                        }}
                        catch {{
                            Console.WriteLine(""Culture Not Found"");
                        }}
                        
                        return 42;
                    }}
                }}";

            buildArgs = buildArgs with { ProjectName = projectName, ProjectFileContents = programText };
            buildArgs = GetBuildArgsWith(buildArgs, extraProperties: $"<EnableSharding>{enableSharding}</EnableSharding><ICUDefaultCulture>{defaultCulture}</ICUDefaultCulture>");
            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                        id: id,
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack);

            string expectedOutputString = expectToThrow == true || enableSharding == false
                                            ? "Culture Not Found"
                                            : $"{CultureInfo.GetCultureInfo(testCulture).Name}";

            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                test: output => Assert.Contains(expectedOutputString, output), host: host, id: id);
        }
    }
}