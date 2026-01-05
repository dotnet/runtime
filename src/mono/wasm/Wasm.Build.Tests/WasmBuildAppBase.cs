// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmBuildAppBase : WasmTemplateTestsBase
    {
        public static IEnumerable<object?[]> MainMethodTestData(bool aot)
            => ConfigWithAOTData(aot)
                .Where(item => !(item.ElementAt(0) is Configuration config && config == Configuration.Debug && item.ElementAt(1) is bool aotValue && aotValue))
                .UnwrapItemsAsArrays();

        public WasmBuildAppBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
        {
        }

        protected async Task TestMain(string projectName,
              string programText,
              Configuration config,
              bool aot,
              bool? isNativeBuild = null,
              int expectedExitCode = 42,
              string expectedOutput = "Hello, World!",
              string runtimeConfigContents = "",
              string extraArgs = "")
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "DotnetRun");
            UpdateFile(Path.Combine("Common", "Program.cs"), programText);
            if (!string.IsNullOrEmpty(runtimeConfigContents))
            {
                UpdateFile("runtimeconfig.template.json", new Dictionary<string, string> { {  "}\n}", runtimeConfigContents } });
            }
            PublishProject(info, config, new PublishOptions(AOT: aot, ExtraMSBuildArgs: extraArgs), isNativeBuild: isNativeBuild);
            RunResult result = await RunForPublishWithWebServer(new BrowserRunOptions(
                config,
                TestScenario: "DotnetRun",
                ExpectedExitCode: expectedExitCode)
            );
            Assert.Contains(result.ConsoleOutput, m => m.Contains(expectedOutput));
        }
    }
}
