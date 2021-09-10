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
        [InlineData("Debug", true)]
        [InlineData("Debug", false)]
        [InlineData("Release", true)]
        [InlineData("Release", false)]
        public void NativeBuild_WithDeployOnBuild_UsedByVS(string config, bool nativeRelink)
        {
            string id = $"blz_deploy_on_build_{config}_{nativeRelink}";
            string projectFile = CreateProjectWithNativeReference(id);
            AddItemsPropertiesToProject(projectFile, extraProperties: nativeRelink ? string.Empty : "<RunAOTCompilation>true</RunAOTCompilation>");

            // build with -p:DeployOnBuild=true, and that will trigger a publish
            BuildInternal(id, config, publish: false, "-p:DeployOnBuild=true");

            var expectedFileType = nativeRelink ? NativeFilesType.Relinked : NativeFilesType.AOT;
            AssertDotNetNativeFiles(expectedFileType, config, forPublish: true);
            AssertBlazorBundle(config, isPublish: true, dotnetWasmFromRuntimePack: false);
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

    public enum NativeFilesType { FromRuntimePack, Relinked, AOT };
}
