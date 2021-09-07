// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BlazorWasmBuildPublishTests : BuildTestBase
    {
        public BlazorWasmBuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
            _enablePerTestCleanup = true;
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsNotUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void DefaultTemplate_WithoutWorkload(string config)
        {
            string id = $"blz_no_workload_{config}";
            CreateBlazorWasmTemplateProject(id);

            // Build
            BuildInternal(id, config, publish: false);
            string binFrameworkDir = Path.Combine(_projectDir!, "bin", config, "net6.0", "wwwroot", "_framework");
            AssertBlazorBootJson(config, isPublish: false, binFrameworkDir);

            // Publish
            BuildInternal(id, config, publish: true);
            binFrameworkDir = Path.Combine(_projectDir!, "bin", config, "net6.0", "publish", "wwwroot", "_framework");
            AssertBlazorBootJson(config, isPublish: true, binFrameworkDir);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void DefaultTemplate_NoAOT_WithWorkload(string config)
        {
            string id = $"blz_no_aot_{config}";
            CreateBlazorWasmTemplateProject(id);

            BlazorBuild(id, config, NativeFilesType.FromRuntimePack);
            if (config == "Release")
            {
                // relinking in publish for Release config
                BlazorPublish(id, config, NativeFilesType.Relinked);
            }
            else
            {
                BlazorPublish(id, config, NativeFilesType.FromRuntimePack);
            }
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void DefaultTemplate_AOT_InProjectFile(string config)
        {
            string id = $"blz_aot_prj_file_{config}";
            string projectFile = CreateBlazorWasmTemplateProject(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");

            // No relinking, no AOT
            BlazorBuild(id, config, NativeFilesType.FromRuntimePack);

            // will aot
            BlazorPublish(id, config, NativeFilesType.AOT);

            // build again
            BlazorBuild(id, config, NativeFilesType.FromRuntimePack);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void DefaultTemplate_AOT_OnlyWithPublishCommandLine_Then_PublishNoAOT(string config)
        {
            string id = $"blz_aot_pub_{config}";
            CreateBlazorWasmTemplateProject(id);

            // No relinking, no AOT
            BlazorBuild(id, config, NativeFilesType.FromRuntimePack);

            // AOT=true only for the publish command line, similar to what
            // would happen when setting it in Publish dialog for VS
            BlazorPublish(id, config, expectedFileType: NativeFilesType.AOT, "-p:RunAOTCompilation=true");

            // publish again, no AOT
            BlazorPublish(id, config, NativeFilesType.Relinked);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void WithNativeReference_AOTInProjectFile(string config)
        {
            string id = $"blz_nativeref_aot_{config}";
            string projectFile = CreateProjectWithNativeReference(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>");

            BlazorBuild(id, config, expectedFileType: NativeFilesType.Relinked);

            BlazorPublish(id, config, expectedFileType: NativeFilesType.AOT);

            // will relink
            BlazorBuild(id, config, expectedFileType: NativeFilesType.Relinked);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void WithNativeReference_AOTOnCommandLine(string config)
        {
            string id = $"blz_nativeref_aot_{config}";
            CreateProjectWithNativeReference(id);

            BlazorBuild(id, config, expectedFileType: NativeFilesType.Relinked);

            BlazorPublish(id, config, expectedFileType: NativeFilesType.AOT, "-p:RunAOTCompilation=true");

            // no aot!
            BlazorPublish(id, config, expectedFileType: NativeFilesType.Relinked);
        }

        private (CommandResult, string) BlazorBuild(string id, string config, NativeFilesType expectedFileType, params string[] extraArgs)
        {
            var res = BuildInternal(id, config, publish: false, extraArgs);
            AssertDotNetNativeFiles(expectedFileType, config, forPublish: false);
            AssertBlazorBundle(config, isPublish: false, dotnetWasmFromRuntimePack: expectedFileType == NativeFilesType.FromRuntimePack);

            return res;
        }

        private (CommandResult, string) BlazorPublish(string id, string config, NativeFilesType expectedFileType, params string[] extraArgs)
        {
            var res = BuildInternal(id, config, publish: true, extraArgs);
            AssertDotNetNativeFiles(expectedFileType, config, forPublish: true);
            AssertBlazorBundle(config, isPublish: true, dotnetWasmFromRuntimePack: expectedFileType == NativeFilesType.FromRuntimePack);
            return res;
        }

        private (CommandResult, string) BuildInternal(string id, string config, bool publish=false, params string[] extraArgs)
        {
            string label = publish ? "publish" : "build";
            Console.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

            string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
            string[] combinedArgs = new[]
            {
                label, // same as the command name
                $"-bl:{logPath}",
                $"-p:Configuration={config}",
                "-p:BlazorEnableCompression=false",
                "-p:_WasmDevel=true"
            }.Concat(extraArgs).ToArray();

            CommandResult res = new DotNetCommand(s_buildEnv)
                                        .WithWorkingDirectory(_projectDir!)
                                        .ExecuteWithCapturedOutput(combinedArgs)
                                        .EnsureSuccessful();

            return (res, logPath);
        }

        private void AssertDotNetNativeFiles(NativeFilesType type, string config, bool forPublish)
        {
            string label = forPublish ? "publish" : "build";
            string objBuildDir = Path.Combine(_projectDir!, "obj", config, "net6.0", "wasm", forPublish ? "for-publish" : "relink");
            string binFrameworkDir = Path.Combine(_projectDir!, "bin", config, "net6.0", forPublish ? "publish" : "", "wwwroot", "_framework");

            string srcDir = type switch
            {
                NativeFilesType.FromRuntimePack => s_buildEnv.RuntimeNativeDir,
                NativeFilesType.Relinked => objBuildDir,
                NativeFilesType.AOT => objBuildDir,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            AssertSameFile(Path.Combine(srcDir, "dotnet.wasm"), Path.Combine(binFrameworkDir, "dotnet.wasm"), label);

            // find dotnet*js
            string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir)
                                    .Where(p => Path.GetFileName(p).StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                    Path.GetFileName(p).EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                                    .SingleOrDefault();

            Assert.True(!string.IsNullOrEmpty(dotnetJsPath), $"[{label}] Expected to find dotnet*js in {binFrameworkDir}");
            AssertSameFile(Path.Combine(srcDir, "dotnet.js"), dotnetJsPath!, label);

            if (type != NativeFilesType.FromRuntimePack)
            {
                // check that the files are *not* from runtime pack
                AssertNotSameFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.wasm"), Path.Combine(binFrameworkDir, "dotnet.wasm"), label);
                AssertNotSameFile(Path.Combine(s_buildEnv.RuntimeNativeDir, "dotnet.js"), dotnetJsPath!, label);
            }
        }

        private string CreateProjectWithNativeReference(string id)
        {
            CreateBlazorWasmTemplateProject(id);

            string extraItems = @$"
                <PackageReference Include=""SkiaSharp"" Version=""2.80.3"" />
                <PackageReference Include=""SkiaSharp.NativeAssets.WebAssembly"" Version=""2.80.3"" />

                <NativeFileReference Include=""$(SkiaSharpStaticLibraryPath)\2.0.9\*.a"" />
                <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
            ";
            string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
            AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

            return projectFile;
        }

    }

    internal enum NativeFilesType { FromRuntimePack, Relinked, AOT };
}
