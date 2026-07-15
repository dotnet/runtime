// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
              string extraArgs = "",
              bool readyToRun = false)
        {
            string extraProperties = "";
            string extraItems = "";
            if (readyToRun)
                EnableInTreeReadyToRun(config, ref extraProperties, ref extraItems);

            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "DotnetRun", extraProperties: extraProperties, extraItems: extraItems);
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

        // Opt the app into ReadyToRun using the in-tree crossgen2 toolchain (CoreCLR only). The
        // WBT-generated app projects don't import WasmApp.InTree.props, so the crossgen2 tool, wasm JIT
        // and reference-assembly paths it normally provides are injected here instead. This is a no-op
        // when not running under CoreCLR or when the in-tree crossgen2/JIT aren't present (e.g. Mono, or
        // on Helix), so the test still validates a normal build there.
        private static void EnableInTreeReadyToRun(Configuration config, ref string extraProperties, ref string extraItems)
        {
            if (!s_buildEnv.IsCoreClrRuntime)
                return;

            string? baseDir = Environment.GetEnvironmentVariable("BASE_DIR");
            if (string.IsNullOrEmpty(baseDir))
                return;

            string hostArch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            string jitName = isWindows ? $"clrjit_universal_wasm_{hostArch}.dll"
                            : isOSX ? $"libclrjit_universal_wasm_{hostArch}.dylib"
                            : $"libclrjit_universal_wasm_{hostArch}.so";

            string toolsDir = Path.Combine(baseDir, "coreclr", $"browser.wasm.{config}", hostArch);
            string crossgen2Path = Path.Combine(toolsDir, "crossgen2", isWindows ? "crossgen2.exe" : "crossgen2");
            string jitPath = Path.Combine(toolsDir, jitName);
            if (!File.Exists(crossgen2Path) || !File.Exists(jitPath))
                return;

            string referenceDir = Path.Combine(baseDir, "microsoft.netcore.app.runtime.browser-wasm", config.ToString(), "runtimes", "browser-wasm") + Path.DirectorySeparatorChar;

            // PublishTrimmed is required because the publish R2R hook crossgens the trimmed (linked) output.
            extraProperties +=
                $"""
                    <PublishTrimmed>true</PublishTrimmed>
                    <_WasmCrossgen2Path>{crossgen2Path}</_WasmCrossgen2Path>
                    <_WasmCrossgen2JitPath>{jitPath}</_WasmCrossgen2JitPath>
                    <_WasmReadyToRunReferenceAssembliesPath>{referenceDir}</_WasmReadyToRunReferenceAssembliesPath>
                    <_WasmCrossgen2ExtraArgs>--codegenopt:JitWasmNyiToR2RUnsupported=1 --codegenopt:JitWasmSimdNyiToR2RUnsupported=1 -O</_WasmCrossgen2ExtraArgs>
                """;
            extraItems +=
                """
                    <WasmReadyToRunAssembly Include="WasmBasicTestApp" />
                """;
        }
    }
}
