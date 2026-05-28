// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NativeLibraryTests : WasmTemplateTestsBase
    {
        public NativeLibraryTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public Task ProjectWithNativeReference(Configuration config, bool aot) =>
            ProjectWithNativeReferenceCore(config, aot);

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        public Task ProjectWithNativeReference_AOT(Configuration config, bool aot) =>
            ProjectWithNativeReferenceCore(config, aot);

        private async Task ProjectWithNativeReferenceCore(Configuration config, bool aot)
        {
            string objectFilename = "native-lib.o";
            string extraItems = $"<NativeFileReference Include=\"{objectFilename}\" />";
            string extraProperties = "<WasmBuildNative>true</WasmBuildNative>";

            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "AppUsingNativeLib-a", extraItems: extraItems, extraProperties: extraProperties);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", objectFilename), Path.Combine(_projectDir, objectFilename));
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir, overwrite: true);
            DeleteFile(Path.Combine(_projectDir, "Common", "Program.cs"));
            // The AppUsingNativeLib program does not use JS interop, so the JS interop assembly
            // would be linked away by the trimmer (CoreCLR-Wasm) and the template main.js (which
            // calls getAssemblyExports) would fail at startup.
            ReplaceMainJsWithMinimalRunMain();

            (string _, string buildOutput) = PublishProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: true);
            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun"));

            Assert.Contains(output.TestOutput, m => m.Contains("print_line: 100"));
            Assert.Contains(output.TestOutput, m => m.Contains("from pinvoke: 142"));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
        public Task ProjectUsingSkiaSharp(Configuration config, bool aot) =>
            ProjectUsingSkiaSharpCore(config, aot);

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
        public Task ProjectUsingSkiaSharp_AOT(Configuration config, bool aot) =>
            ProjectUsingSkiaSharpCore(config, aot);

        private async Task ProjectUsingSkiaSharpCore(Configuration config, bool aot)
        {
            string prefix = $"AppUsingSkiaSharp";
            string extraItems = @$"
                                {GetSkiaSharpReferenceItems()}
                                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
                            ";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, prefix, extraItems: extraItems);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "SkiaSharp.cs"));

            PublishProject(info, config, new PublishOptions(AOT: aot));
            BrowserRunOptions runOptions = new(config, ExtraArgs: "mono.png");
            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));
            Assert.Contains(output.TestOutput, m => m.Contains("Size: 26462 Height: 599, Width: 499"));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public Task ProjectUsingBrowserNativeCrypto(Configuration config, bool aot) =>
            ProjectUsingBrowserNativeCryptoCore(config, aot);

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        public Task ProjectUsingBrowserNativeCrypto_AOT(Configuration config, bool aot) =>
            ProjectUsingBrowserNativeCryptoCore(config, aot);

        private async Task ProjectUsingBrowserNativeCryptoCore(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "AppUsingBrowserNativeCrypto");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "NativeCrypto.cs"));

            (string _, string buildOutput) = PublishProject(info, config, new PublishOptions(AOT: aot));
            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));

            string hash = "Hashed: 24 95 141 179 34 113 254 37 245 97 166 252 147 139 46 38 67 6 236 48 78 218 81 128 7 209 118 72 38 56 25 105";
            Assert.Contains(output.TestOutput, m => m.Contains(hash));

            string cryptoInitMsg = "MONO_WASM: Initializing Crypto WebWorker";
            Assert.All(output.TestOutput, m => Assert.DoesNotContain(cryptoInitMsg, m));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        public Task ProjectWithNativeLibrary(Configuration config, bool aot) =>
            ProjectWithNativeLibraryCore(config, aot);

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        [TestCategory("native-mono")]
        public Task ProjectWithNativeLibrary_AOT(Configuration config, bool aot) =>
            ProjectWithNativeLibraryCore(config, aot);

        private async Task ProjectWithNativeLibraryCore(Configuration config, bool aot)
        {
            string extraItems = "<NativeLibrary Include=\"native-lib.o\" />\n<NativeLibrary Include=\"DoesNotExist.o\" />";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "AppUsingNativeLib-a", extraItems: extraItems);
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir, overwrite: true);
            DeleteFile(Path.Combine(_projectDir, "Common", "Program.cs"));
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir, "native-lib.o"));
            // The AppUsingNativeLib program does not use JS interop, so the JS interop assembly
            // would be linked away by the trimmer (CoreCLR-Wasm) and the template main.js (which
            // calls getAssemblyExports) would fail at startup.
            ReplaceMainJsWithMinimalRunMain();

            (string _, string buildOutput) = PublishProject(info, config, new PublishOptions(AOT: aot), isNativeBuild: true);
            RunResult output = await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));

            Assert.Contains(output.TestOutput, m => m.Contains("print_line: 100"));
            Assert.Contains(output.TestOutput, m => m.Contains("from pinvoke: 142"));
        }
    }
}
