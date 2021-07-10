// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Wasm.Build.Tests
{
    public class BuildEnvironment
    {
        public string                           DotNet                        { get; init; }
        public string                           RuntimePackDir                { get; init; }
        public bool                             IsWorkload                    { get; init; }
        public string                           DefaultBuildArgs              { get; init; }
        public IDictionary<string, string>      EnvVars                       { get; init; }
        public string                           DirectoryBuildPropsContents   { get; init; }
        public string                           DirectoryBuildTargetsContents { get; init; }
        public string                           RuntimeNativeDir              { get; init; }
        public string                           LogRootPath                   { get; init; }

        public static readonly string           RelativeTestAssetsPath = @"..\testassets\";
        public static readonly string           TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
        public static readonly string           TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");

        private static string s_runtimeConfig = "Release";

        public BuildEnvironment()
        {
            DirectoryInfo? solutionRoot = new (AppContext.BaseDirectory);
            while (solutionRoot != null)
            {
                if (File.Exists(Path.Combine(solutionRoot.FullName, "NuGet.config")))
                {
                    break;
                }

                solutionRoot = solutionRoot.Parent;
            }

            string? sdkForWorkloadPath = EnvironmentVariables.SdkForWorkloadTestingPath;
            if (!string.IsNullOrEmpty(sdkForWorkloadPath))
            {
                DotNet = Path.Combine(sdkForWorkloadPath, "dotnet");
                var workloadPacksVersion = EnvironmentVariables.WorkloadPacksVersion;
                if (string.IsNullOrEmpty(workloadPacksVersion))
                    throw new Exception($"Cannot test with workloads without WORKLOAD_PACKS_VER environment variable being set");

                RuntimePackDir = Path.Combine(sdkForWorkloadPath, "packs", "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", workloadPacksVersion);
                DirectoryBuildPropsContents = s_directoryBuildPropsForWorkloads;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForWorkloads;
                EnvVars = new Dictionary<string, string>()
                {
                    // `runtime` repo's build environment sets these, and they
                    // mess up the build for the test project, which is using a different
                    // dotnet
                    ["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath,
                    ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
                    ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
                    ["MSBuildSDKsPath"] = string.Empty,
                    ["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}"
                };

                var appRefDir = EnvironmentVariables.AppRefDir;
                if (string.IsNullOrEmpty(appRefDir))
                    throw new Exception($"Cannot test with workloads without AppRefDir environment variable being set");

                DefaultBuildArgs = $" /p:AppRefDir={appRefDir}";
                IsWorkload = true;
            }
            else
            {
                string emsdkPath;
                if (solutionRoot == null)
                {
                    string? buildDir = EnvironmentVariables.WasmBuildSupportDir;
                    if (buildDir == null || !Directory.Exists(buildDir))
                        throw new Exception($"Could not find the solution root, or a build dir: {buildDir}");

                    emsdkPath = Path.Combine(buildDir, "emsdk");
                    RuntimePackDir = Path.Combine(buildDir, "microsoft.netcore.app.runtime.browser-wasm");
                    DefaultBuildArgs = $" /p:WasmBuildSupportDir={buildDir} /p:EMSDK_PATH={emsdkPath} ";
                }
                else
                {
                    string artifactsBinDir = Path.Combine(solutionRoot.FullName, "artifacts", "bin");
                    RuntimePackDir = Path.Combine(artifactsBinDir, "microsoft.netcore.app.runtime.browser-wasm", s_runtimeConfig);

                    if (string.IsNullOrEmpty(EnvironmentVariables.EMSDK_PATH))
                        emsdkPath = Path.Combine(solutionRoot.FullName, "src", "mono", "wasm", "emsdk");
                    else
                        emsdkPath = EnvironmentVariables.EMSDK_PATH;

                    DefaultBuildArgs = $" /p:RuntimeSrcDir={solutionRoot.FullName} /p:RuntimeConfig={s_runtimeConfig} /p:EMSDK_PATH={emsdkPath} ";
                }

                IsWorkload = false;
                DotNet = "dotnet";
                EnvVars = new Dictionary<string, string>()
                {
                    ["EMSDK_PATH"] = emsdkPath
                };

                DirectoryBuildPropsContents = s_directoryBuildPropsForLocal;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForLocal;
            }

            RuntimeNativeDir = Path.Combine(RuntimePackDir, "runtimes", "browser-wasm", "native");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DotNet += ".exe";

            if (!string.IsNullOrEmpty(EnvironmentVariables.TestLogPath))
            {
                LogRootPath = EnvironmentVariables.TestLogPath;
                if (!Directory.Exists(LogRootPath))
                {
                    Directory.CreateDirectory(LogRootPath);
                }
            }
            else
            {
                LogRootPath = Environment.CurrentDirectory;
            }
        }

        protected static string s_directoryBuildPropsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.targets"));

        protected static string s_directoryBuildPropsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.targets"));
    }
}
