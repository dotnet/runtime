// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public async Task AOT_InvariantTimezone(string config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone);

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false })]
        public async Task RelinkingWithoutAOT(string config, bool aot, bool? invariantTimezone)
            => await TestInvariantTimezone(config, aot, invariantTimezone, isNativeBuild: true);

        private async Task TestInvariantTimezone(string config, bool aot, bool? invariantTimezone, bool isNativeBuild = false)
        {
            string extraProperties = isNativeBuild ? "<WasmBuildNative>true</WasmBuildNative>" : "";
            if (invariantTimezone != null)
                extraProperties = $"{extraProperties}<InvariantTimezone>{invariantTimezone}</InvariantTimezone>";

            string prefix = $"invariant_{invariantTimezone?.ToString() ?? "unset"}";
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, prefix, extraProperties: extraProperties);
            ReplaceFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "Wasm.Buid.Tests.Programs", "InvariantTimezone.cs"));
            UpdateBrowserMainJs();

            bool isPublish = true;
            // invariantTimezone triggers native build
            isNativeBuild = isNativeBuild || invariantTimezone == true;
            BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish, isNativeBuild: isNativeBuild),
                            IsPublish: isPublish
                        ));

            RunResult output = await RunForPublishWithWebServer(new(info.Configuration, ExpectedExitCode: 42));
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
