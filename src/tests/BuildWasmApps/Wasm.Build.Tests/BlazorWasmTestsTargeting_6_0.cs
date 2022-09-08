// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BlazorWasmTestsTargeting_6_0 : BuildTestBase
    {
        /*
            - bl67
                - build
                - build+relink

                - publish
                    - net6
                    - net7
                - publish+aot
                    - net6
                    - net7
                - publish+relink
                    - net6
                    - net7

            - bl6
            - bl7
        */
        private const string s_net60_ProjectName = "Blazor_net60";
        // private const string s_net70_ProjectName = "Blazor_net70";
        private const string s_net60_70_ProjectName = "Blazor_net60_70";

        public BlazorWasmTestsTargeting_6_0(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        // [Theory, TestCategory("workload-6"), TestCategory("workload-67")]
        // [InlineData("Debug")]
        // [InlineData("Release")]
        // public void NativeRef_EmitsWarningBecauseItRequiresWorkload(string config)
        // {
        //     CommandResult res = PublishForRequiresWorkloadTest(s_net60_ProjectName, config, extraItems: "<NativeFileReference Include=\"native-lib.o\" />");
        //     res.EnsureSuccessful();
        //     Assert.Matches("warning : .*but the native references won't be linked in", res.Output);
        // }

        // [ConditionalTheory(typeof(BuildTestBase), nameof(DoesNotHaveWorkload6))]
        [Theory]
        [Trait("category", "none")]
        [Trait("category", "net7")]
        // [WorkloadVariantSpecific(nameof(DoesNotHaveWorkload6))]
        [InlineData(s_net60_ProjectName, "Debug", /*aot*/false, "")]
        [InlineData(s_net60_ProjectName, "Debug", /*aot*/true, "")]
        [InlineData(s_net60_ProjectName, "Release", /*aot*/false, "")]
        [InlineData(s_net60_ProjectName, "Release", /*aot*/true, "")]
        // Multi-targeting ones will fail because net6 isn't available
        // FIXME: add same for -f net7.0 + does-not-have-7
        // FIXME: separate the multi-targeting cases into separate tests
        [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/false, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/true, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Release", /*aot*/false, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Release", /*aot*/true, "-f net6.0")]
        public void NativePublish_FailsBecauseItRequiresWorkload6(string assetName, string config, bool aot, string extraArgs)
        {
            CommandResult res = PublishForRequiresWorkloadTest(assetName, config,
                                    aot: aot,
                                    relinking: !aot,
                                    extraArgs: extraArgs,
                                    expectSuccess: false);

            if (assetName == s_net60_70_ProjectName)
            {
                // multi-targeting
                // 7, none
                if (IsWorkloads7Only)
                    Assert.Contains("following workloads must be installed: wasm-tools-net6", res.Output);
                else if (IsNotUsingWorkloads)
                    Assert.Contains("following workloads must be installed: wasm-tools ", res.Output);
            }
            else
            {
                // publishing a 6.0 project
                Assert.Contains("following workloads must be installed: wasm-tools-net6", res.Output);
            }
        }

        // FIXME: multi-targeting projects will only build with all the workloads installed!

        [Theory]
        // [WorkloadVariantSpecific(WorkloadSetupVariant.WithPrevious)]
        [InlineData(s_net60_ProjectName, "Debug", /*aot*/false, "")]
        [InlineData(s_net60_ProjectName, "Debug", /*aot*/true, "")]
        [InlineData(s_net60_ProjectName, "Release", /*aot*/false, "")]
        [InlineData(s_net60_ProjectName, "Release", /*aot*/true, "")]
        // [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/false, "-f net6.0")]
        // [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/true, "-f net6.0")]
        // [InlineData(s_net60_70_ProjectName, "Release", /*aot*/false, "-f net6.0")]
        // [InlineData(s_net60_70_ProjectName, "Release", /*aot*/true, "-f net6.0")]
        public void NativePublish_WorksWithWorkload6Installed(string assetName, string config, bool aot, string extraArgs)
        {
            CommandResult res = PublishForRequiresWorkloadTest(assetName, config,
                                    aot: aot,
                                    relinking: !aot,
                                    extraArgs: extraArgs);
            // res.EnsureSuccessful();

            // BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT));
            // Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
            //FIXME: run this
        }
        [Theory]
        // [WorkloadVariantSpecific(WorkloadSetupVariant.net6_7)]
        [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/false, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Debug", /*aot*/true, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Release", /*aot*/false, "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Release", /*aot*/true, "-f net6.0")]
        public void NativePublish_MultiTargeting_WorksWhenAllWorkloadsAreInstalled(string assetName, string config, bool aot, string extraArgs)
        {
            CommandResult res = PublishForRequiresWorkloadTest(assetName, config,
                                    aot: aot,
                                    relinking: !aot,
                                    extraArgs: extraArgs);
            // res.EnsureSuccessful();

            // BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT));
            // Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
            //FIXME: run this
        }

        [Theory]
        // [Theory, WorkloadVariantSpecific(WorkloadSetupVariant.WithoutPrevious)]
        [InlineData(s_net60_ProjectName, "Debug", "")]
        [InlineData(s_net60_ProjectName, "Release", "")]
        [InlineData(s_net60_70_ProjectName, "Debug", "-f net6.0")]
        [InlineData(s_net60_70_ProjectName, "Release", "-f net6.0")]
        public void NonNativePublish_WorksWithoutWorkload6Installed(string assetName, string config, string extraArgs)
        {
            CommandResult res = PublishForRequiresWorkloadTest(assetName, config, extraArgs: extraArgs);
            // res.EnsureSuccessful();

            // BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT));
            // Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
            //FIXME: run this
        }

        // [Theory, TestCategory("no-workload")]
        // [InlineData("Debug")]
        // [InlineData("Release")]
        // public void AOT_And_NativeRef_FailBecauseTheyRequireWorkload(string config)
        // {
        //     CommandResult res = PublishForRequiresWorkloadTest(config,
        //                             extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>",
        //                             extraItems: "<NativeFileReference Include=\"native-lib.o\" />");

        //     Assert.NotEqual(0, res.ExitCode);
        //     Assert.Contains("following workloads must be installed: wasm-tools", res.Output);
        // }

        private CommandResult PublishForRequiresWorkloadTest(string assetName,
                                                             string config,
                                                             bool aot = false,
                                                             bool relinking = false,
                                                             string extraItems = "",
                                                             string extraProperties = "",
                                                             string extraArgs = "",
                                                             bool expectSuccess = true)
        {
            string id = $"{assetName}_{config}_{Path.GetRandomFileName()}";
            (_, string projectFile) = CreateBlazorWasmProjectFromTestAssets(assetName, id);

            if (aot)
                extraProperties += "<RunAOTCompilation>true</RunAOTCompilation>";
            else if (relinking)
                extraProperties += "<WasmBuildNative>true</WasmBuildNative>";

            AddItemsPropertiesToProject(projectFile,
                                        extraProperties: extraProperties,
                                        extraItems: extraItems);

            var res = BlazorPublish(new BlazorBuildOptions
                                    (
                                        id,
                                        config,
                                        aot ? NativeFilesType.AOT
                                            : (relinking ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack),
                                        TargetFramework: "net6.0",
                                        ExpectSuccess: expectSuccess
                                    ), extraArgs);
            return res.Item1;

            // string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}.binlog");
            // return new DotNetCommand(s_buildEnv, _testOutput)
            //                 .WithWorkingDirectory(_projectDir!)
            //                 .ExecuteWithCapturedOutput("publish",
            //                                             $"-bl:{publishLogPath}",
            //                                             $"-p:Configuration={config}");
        }

        // [Theory]
        // [InlineData("Debug")]
        // [InlineData("Release")]
        // public void Net50Projects_NativeReference(string config)
        //     => BuildNet50Project(config, aot: false, expectError: true, @"<NativeFileReference Include=""native-lib.o"" />");

        // public static TheoryData<string, bool, bool> Net50TestData = new()
        // {
        //     { "Debug", /*aot*/ true, /*expectError*/ true },
        //     { "Debug", /*aot*/ false, /*expectError*/ false },
        //     { "Release", /*aot*/ true, /*expectError*/ true },
        //     { "Release", /*aot*/ false, /*expectError*/ false }
        // };

        // [Theory]
        // [MemberData(nameof(Net50TestData))]
        // public void Net50Projects_AOT(string config, bool aot, bool expectError)
        //     => BuildNet50Project(config, aot: aot, expectError: expectError);

        // private void BuildNet50Project(string config, bool aot, bool expectError, string? extraItems=null)
        // {
        //     (string id, string projectFile) = CreateBlazorWasmProjectFromTestAssets(
        //                                             "Blazor_net50", $"Blazor_net50_{config}_{aot}_{Path.GetRandomFileName()}");

        //     AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

        //     string logPath = Path.Combine(s_buildEnv.LogRootPath, id);
        //     string publishLogPath = Path.Combine(logPath, $"{id}.binlog");
        //     CommandResult result = new DotNetCommand(s_buildEnv, _testOutput)
        //                                     .WithWorkingDirectory(_projectDir!)
        //                                     .ExecuteWithCapturedOutput("publish",
        //                                                                $"-bl:{publishLogPath}",
        //                                                                (aot ? "-p:RunAOTCompilation=true" : ""),
        //                                                                $"-p:Configuration={config}");

        //     if (expectError)
        //     {
        //         result.EnsureExitCode(1);
        //         Assert.Contains("are only supported for projects targeting net6.0+", result.Output);
        //     }
        //     else
        //     {
        //         result.EnsureSuccessful();
        //         Assert.Contains("** UsingBrowserRuntimeWorkload: 'false'", result.Output);

        //         string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish: true, framework: "net5.0");
        //         AssertBlazorBootJson(config, isPublish: true, binFrameworkDir: binFrameworkDir);
        //         // dotnet.wasm here would be from 5.0 nuget like:
        //         // /Users/radical/.nuget/packages/microsoft.netcore.app.runtime.browser-wasm/5.0.9/runtimes/browser-wasm/native/dotnet.wasm
        //     }
        // }
    }
}
