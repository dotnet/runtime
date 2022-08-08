// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

#nullable enable

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
            if (string.IsNullOrEmpty(sdkForWorkloadPath))
            {
                // Is this a "local run?
                string probePath = Path.Combine(Path.GetDirectoryName(typeof(BuildEnvironment).Assembly.Location)!,
                                                "..",
                                                "..",
                                                "..",
                                                "dotnet-workload");
                if (Directory.Exists(probePath))
                    sdkForWorkloadPath = Path.GetFullPath(probePath);
                else
                    throw new Exception($"Environment variable SDK_FOR_WORKLOAD_TESTING_PATH not set, and could not find it at {probePath}");
            }
            if (!Directory.Exists(sdkForWorkloadPath))
                throw new Exception($"Could not find SDK_FOR_WORKLOAD_TESTING_PATH={sdkForWorkloadPath}");

            EnvVars = new Dictionary<string, string>();
            bool workloadInstalled = EnvironmentVariables.SdkHasWorkloadInstalled != null && EnvironmentVariables.SdkHasWorkloadInstalled == "true";
            if (workloadInstalled)
            {
                var workloadPacksVersion = EnvironmentVariables.WorkloadPacksVersion;
                if (string.IsNullOrEmpty(workloadPacksVersion))
                    throw new Exception($"Cannot test with workloads without WORKLOAD_PACKS_VER environment variable being set");

                RuntimePackDir = Path.Combine(sdkForWorkloadPath, "packs", "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", workloadPacksVersion);
                DirectoryBuildPropsContents = s_directoryBuildPropsForWorkloads;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForWorkloads;

                var appRefDir = EnvironmentVariables.AppRefDir;
                if (string.IsNullOrEmpty(appRefDir))
                    throw new Exception($"Cannot test with workloads without AppRefDir environment variable being set");

                DefaultBuildArgs = $" /p:AppRefDir={appRefDir}";
                IsWorkload = true;
            }
            else
            {
                RuntimePackDir = "/dont-check-runtime-pack-dir-for-no-workloads-case";
                var appRefDir = EnvironmentVariables.AppRefDir;
                if (string.IsNullOrEmpty(appRefDir))
                    throw new Exception($"Cannot test with workloads without AppRefDir environment variable being set");

                DefaultBuildArgs = $" /p:AppRefDir={appRefDir}";
                DirectoryBuildPropsContents = s_directoryBuildPropsForLocal;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForLocal;
            }

            // `runtime` repo's build environment sets these, and they
            // mess up the build for the test project, which is using a different
            // dotnet
            EnvVars["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["MSBuildSDKsPath"] = string.Empty;
            EnvVars["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
            EnvVars["EM_WORKAROUND_PYTHON_BUG_34780"] = "1";

            // helps with debugging
            EnvVars["WasmNativeStrip"] = "false";

            if (OperatingSystem.IsWindows())
            {
                EnvVars["WasmCachePath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                                        ".emscripten-cache");
            }

            RuntimeNativeDir = Path.Combine(RuntimePackDir, "runtimes", "browser-wasm", "native");
            DotNet = Path.Combine(sdkForWorkloadPath!, "dotnet");
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

        protected static string s_directoryBuildPropsForBlazorLocal = File.ReadAllText(Path.Combine(TestDataPath, "Blazor.Local.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForBlazorLocal = File.ReadAllText(Path.Combine(TestDataPath, "Blazor.Local.Directory.Build.targets"));
    }
}
