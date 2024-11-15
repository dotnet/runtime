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
        [BuildAndRun(config: "Release", aot: true)]
        public async Task ProjectWithNativeReference(string config, bool aot)
        {
            string objectFilename = "native-lib.o";
            string extraItems = $"<NativeFileReference Include=\"{objectFilename}\" />";
            string extraProperties = "<WasmBuildNative>true</WasmBuildNative>";

            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "AppUsingNativeLib-a", "App", extraItems: extraItems, extraProperties: extraProperties);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", objectFilename), Path.Combine(_projectDir!, objectFilename));
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir!, overwrite: true);
            DeleteFile(Path.Combine(_projectDir!, "Common", "Program.cs"));

            bool isPublish = true;
            (string _, string buildOutput) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                            IsPublish: isPublish
                        ));
            RunResult output = await RunForPublishWithWebServer(new(config, TestScenario: "DotnetRun"));

            Assert.Contains(output.TestOutput, m => m.Contains("print_line: 100"));
            Assert.Contains(output.TestOutput, m => m.Contains("from pinvoke: 142"));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(config: "Release", aot: true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
        public async Task ProjectUsingSkiaSharp(string config, bool aot)
        {
            string prefix = $"AppUsingSkiaSharp";
            string extraItems = @$"
                                {GetSkiaSharpReferenceItems()}
                                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
                            ";
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", prefix, "App", extraItems: extraItems);
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "SkiaSharp.cs"));

            bool isPublish = true;
            BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish: isPublish),
                            IsPublish: isPublish
                        ));

            RunOptions runOptions = new(info.Configuration, ExtraArgs: "mono.png");
            RunResult output = await RunForPublishWithWebServer(new(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));
            Assert.Contains(output.TestOutput, m => m.Contains("Size: 26462 Height: 599, Width: 499"));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(config: "Release", aot: true)]
        public async Task ProjectUsingBrowserNativeCrypto(string config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "AppUsingBrowserNativeCrypto", "App");
            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "NativeCrypto.cs"));

            bool isPublish = true;
            (string _, string buildOutput) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish
                        ));

            RunResult output = await RunForPublishWithWebServer(new(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));

            string hash = "Hashed: 24 95 141 179 34 113 254 37 245 97 166 252 147 139 46 38 67 6 236 48 78 218 81 128 7 209 118 72 38 56 25 105";
            Assert.Contains(output.TestOutput, m => m.Contains(hash));

            string cryptoInitMsg = "MONO_WASM: Initializing Crypto WebWorker";
            Assert.All(output.TestOutput, m => Assert.DoesNotContain(cryptoInitMsg, m));
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(config: "Release", aot: true)]
        public async Task ProjectWithNativeLibrary(string config, bool aot)
        {
            string extraItems = "<NativeLibrary Include=\"native-lib.o\" />\n<NativeLibrary Include=\"DoesNotExist.o\" />";
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "AppUsingNativeLib-a", "App", extraItems: extraItems);
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir!, overwrite: true);
            DeleteFile(Path.Combine(_projectDir!, "Common", "Program.cs"));
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir!, "native-lib.o"));

            bool isPublish = true;
            (string _, string buildOutput) = BuildProject(info,
                        new BuildOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                            IsPublish: isPublish
                        ));
            RunResult output = await RunForPublishWithWebServer(new(config, TestScenario: "DotnetRun", ExpectedExitCode: 0));

            Assert.Contains(output.TestOutput, m => m.Contains("print_line: 100"));
            Assert.Contains(output.TestOutput, m => m.Contains("from pinvoke: 142"));
        }
    }
}
