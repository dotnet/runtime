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
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "AppUsingNativeLib-a",
                extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                extraItems: "<NativeFileReference Include=\"native-lib.o\" />");            
            
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir!, overwrite: true);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir!, "native-lib.o"));
            UpdateBrowserMainJs();

            bool isPublish = true;
            (string _, string buildOutput) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                            IsPublish: isPublish
                        ));
            RunOptions runOptions = new(config);
            string output = await RunForPublishWithWebServer(runOptions);

            Assert.Contains("print_line: 100", output);
            Assert.Contains("from pinvoke: 142", output);
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(aot: true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
        public void ProjectUsingSkiaSharp(ProjectInfo buildArgs, RunHost host, string id)
        {
            string projectName = $"AppUsingSkiaSharp";
            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs,
                            extraItems: @$"
                                {GetSkiaSharpReferenceItems()}
                                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
                            ");

            string programText = @"
using System;
using SkiaSharp;

public class Test
{
    public static int Main()
    {
        using SKFileStream skfs = new SKFileStream(""mono.png"");
        using SKImage img = SKImage.FromEncodedData(skfs);

        Console.WriteLine ($""Size: {skfs.Length} Height: {img.Height}, Width: {img.Width}"");
        return 0;
    }
}";

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: false));

            string output = RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 0,
                                test: output => {},
                                host: host, id: id,
                                args: "mono.png");

            Assert.Contains("Size: 26462 Height: 599, Width: 499", output);
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(config: "Release", aot: true)]
        public async Task ProjectUsingBrowserNativeCrypto(string config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, "AppUsingBrowserNativeCrypto");
            
            UpdateFile("Program.cs", Path.Combine(BuildEnvironment.TestAssetsPath, "Wasm.Buid.Tests.Programs", "NativeCrypto.cs"));
            UpdateBrowserMainJs();

            bool isPublish = true;
            (string _, string buildOutput) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish
                        ));

            RunOptions runOptions = new(info.Configuration);
            string output = await RunForPublishWithWebServer(runOptions);
            
            Assert.Contains(
                "Hashed: 24 95 141 179 34 113 254 37 245 97 166 252 147 139 46 38 67 6 236 48 78 218 81 128 7 209 118 72 38 56 25 105",
                output);

            string cryptoInitMsg = "MONO_WASM: Initializing Crypto WebWorker";
            Assert.DoesNotContain(cryptoInitMsg, output);
        }

        [Theory]
        [BuildAndRun(aot: false)]
        [BuildAndRun(config: "Release", aot: true)]
        public async Task ProjectWithNativeLibrary(string config, bool aot)
        {
                ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot,
                "AppUsingNativeLib-a",
                extraItems: "<NativeLibrary Include=\"native-lib.o\" />\n<NativeLibrary Include=\"DoesNotExist.o\" />");            
            
            Utils.DirectoryCopy(Path.Combine(BuildEnvironment.TestAssetsPath, "AppUsingNativeLib"), _projectDir!, overwrite: true);
            File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "native-libs", "native-lib.o"), Path.Combine(_projectDir!, "native-lib.o"));
            UpdateBrowserMainJs();

            bool isPublish = true;
            (string _, string buildOutput) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish, isNativeBuild: true),
                            IsPublish: isPublish
                        ));
            RunOptions runOptions = new(config);
            string output = await RunForPublishWithWebServer(runOptions);

            Assert.Contains("print_line: 100", output);
            Assert.Contains("from pinvoke: 142", output);
        }
    }
}
