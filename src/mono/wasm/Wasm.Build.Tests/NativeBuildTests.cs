// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeBuildTests : WasmTemplateTestsBase
    {
        public NativeBuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [InlineData(Configuration.Release)]
        [TestCategory("native"), TestCategory("workload")]
        public async Task NativeRelinkWithoutManagedTimers(Configuration config)
        {
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser, config, "native_relink_without_timers",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative><PublishTrimmed>true</PublishTrimmed><WasmRunInWorker>false</WasmRunInWorker>");
            ReplaceFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "SimpleSourceChange.cs"));
            ReplaceMainJsWithMinimalRunMain();

            PublishProject(info, config, new PublishOptions(AssertAppBundle: false), isNativeBuild: true);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 55));
        }

        [Theory]
        [BuildAndRun]
        [TestCategory("wasm-opt")]
        public async Task SimpleNativeBuild(Configuration config, bool aot)
        {
            Assert.False(aot);
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                "simple_native_build",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            
            UpdateBrowserProgramFile();
            // The replacement program does not use JS interop, so the JS interop assembly would be
            // linked away by the trimmer (CoreCLR-Wasm) and the template main.js (which calls
            // getAssemblyExports) would fail at startup.
            ReplaceMainJsWithMinimalRunMain();

            (string _, string buildOutput) = PublishProject(info, config, isNativeBuild: true);
            if (EnvironmentVariables.UseWasmOpt)
                Assert.Contains("Running wasm-opt with", buildOutput);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
        }

        [Theory]
        [BuildAndRun]
        [TestCategory("native")]
        [SkipOnPlatform(TestPlatforms.AnyUnix, "The cmd.exe quoting behavior this covers is Windows-specific.")]
        public async Task NativeBuildWithSpecialCharsInTempPath(Configuration config, bool aot)
        {
            Assert.False(aot);
            // Regression test for https://github.com/dotnet/runtime/issues/120327.
            // Native compilation runs the compiler through a temporary batch file created under the
            // temp directory. Windows user profile names can contain parentheses (e.g. "John(US)"),
            // which puts parentheses in %TEMP%. `cmd /c "<path>"` then stripped the quotes around
            // that path and mis-parsed it at the first '(', failing the native build with
            // "'C:\Users\John' is not recognized as an internal or external command".
            // The unicode chars additionally cover the UTF-8 (chcp 65001) handling in the same
            // RunShellCommand path, which exists so non-ASCII (e.g. GB18030) temp/user paths work.
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                "parens_temp",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>");

            UpdateBrowserProgramFile();
            ReplaceMainJsWithMinimalRunMain();

            string tempWithParens = Path.Combine(BuildEnvironment.TmpPath, $"tmp ({GetRandomId()}) {s_unicodeChars}");
            Directory.CreateDirectory(tempWithParens);
            var envVars = new Dictionary<string, string>
            {
                ["TMP"] = tempWithParens,
                ["TEMP"] = tempWithParens,
            };

            PublishProject(info, config, new PublishOptions(ExtraBuildEnvironmentVariables: envVars), isNativeBuild: true);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
        }

        [Fact]
        public async Task ZipArchiveInteropTest()
        {
            Configuration config = Configuration.Debug;
            ProjectInfo info = CopyTestAsset(config, false, TestAsset.WasmBasicTestApp, "ZipArchiveInteropTest", extraProperties: "<WasmBuildNative>true</WasmBuildNative>");
            BuildProject(info, config, new BuildOptions(AssertAppBundle: false));
            BuildPaths paths = GetBuildPaths(config, forPublish: false);
            Assert.DoesNotContain("System_Security_Cryptography", File.ReadAllText(Path.Combine(paths.ObjWasmDir, "callhelpers-pinvoke.cpp")));
            RunResult result = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, TestScenario: "ZipArchiveInteropTest"));
            Assert.Collection(
                result.TestOutput,
                m => Assert.Equal("Zip file created successfully.", m)
            );
        }
    }
}
