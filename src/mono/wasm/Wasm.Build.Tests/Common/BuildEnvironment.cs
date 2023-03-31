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
        public bool                             IsWorkload                    { get; init; }
        public string                           DefaultBuildArgs              { get; init; }
        public IDictionary<string, string>      EnvVars                       { get; init; }
        public string                           DirectoryBuildPropsContents   { get; init; }
        public string                           DirectoryBuildTargetsContents { get; init; }
        public string                           LogRootPath                   { get; init; }

        public string                           WorkloadPacksDir              { get; init; }
        public string                           BuiltNuGetsPath               { get; init; }

        public bool UseWebcil { get; init; }

        public static readonly string           RelativeTestAssetsPath = @"..\testassets\";
        public static readonly string           TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
        public static readonly string           TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");
        public static readonly string           TmpPath = Path.Combine(AppContext.BaseDirectory, "wbt");

        public static readonly string           DefaultRuntimeIdentifier =
#if TARGET_WASI
                                                    "wasi-wasm";
#else
                                                    "browser-wasm";
#endif

        private static readonly Dictionary<string, string> s_runtimePackVersions = new();

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
                string sdkDirName = string.IsNullOrEmpty(EnvironmentVariables.SdkDirName) ? "dotnet-latest" : EnvironmentVariables.SdkDirName;
                string probePath = Path.Combine(Path.GetDirectoryName(typeof(BuildEnvironment).Assembly.Location)!,
                                                "..",
                                                "..",
                                                "..",
                                                sdkDirName);
                if (Directory.Exists(probePath))
                    sdkForWorkloadPath = Path.GetFullPath(probePath);
                else
                    throw new Exception($"Environment variable SDK_FOR_WORKLOAD_TESTING_PATH not set, and could not find it at {probePath}");
            }
            if (!Directory.Exists(sdkForWorkloadPath))
                throw new Exception($"Could not find SDK_FOR_WORKLOAD_TESTING_PATH={sdkForWorkloadPath}");

            sdkForWorkloadPath = Path.GetFullPath(sdkForWorkloadPath);

            // FIXME:
            foreach (string verStr in new[] { "8", "7", "6" })
            {
                string versionValue = Environment.GetEnvironmentVariable($"RUNTIME_PACK_VER{verStr}") ?? string.Empty;
                s_runtimePackVersions[$"net{verStr}.0"] = versionValue;
            }

            DefaultBuildArgs = string.Empty;
            WorkloadPacksDir = Path.Combine(sdkForWorkloadPath, "packs");
            EnvVars = new Dictionary<string, string>();
            bool workloadInstalled = EnvironmentVariables.SdkHasWorkloadInstalled != null && EnvironmentVariables.SdkHasWorkloadInstalled == "true";
            if (workloadInstalled)
            {
                DirectoryBuildPropsContents = s_directoryBuildPropsForWorkloads;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForWorkloads;
                IsWorkload = true;
            }
            else
            {
                DirectoryBuildPropsContents = s_directoryBuildPropsForLocal;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForLocal;
            }

            UseWebcil = EnvironmentVariables.UseWebcil;

            if (EnvironmentVariables.BuiltNuGetsPath is null || !Directory.Exists(EnvironmentVariables.BuiltNuGetsPath))
                throw new Exception($"Cannot find 'BUILT_NUGETS_PATH={EnvironmentVariables.BuiltNuGetsPath}'");

            BuiltNuGetsPath = EnvironmentVariables.BuiltNuGetsPath;

            // `runtime` repo's build environment sets these, and they
            // mess up the build for the test project, which is using a different
            // dotnet
            EnvVars["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
            EnvVars["EM_WORKAROUND_PYTHON_BUG_34780"] = "1";

            // helps with debugging
            EnvVars["WasmNativeStrip"] = "false";

            DotNet = Path.Combine(sdkForWorkloadPath!, "dotnet");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DotNet += ".exe";

            if (!string.IsNullOrEmpty(EnvironmentVariables.TestLogPath))
            {
                LogRootPath = Path.GetFullPath(EnvironmentVariables.TestLogPath);
                if (!Directory.Exists(LogRootPath))
                {
                    Directory.CreateDirectory(LogRootPath);
                }
            }
            else
            {
                LogRootPath = Environment.CurrentDirectory;
            }

            if (Directory.Exists(TmpPath))
                Directory.Delete(TmpPath, recursive: true);
            Directory.CreateDirectory(TmpPath);
        }

        // FIXME: error checks
        public string GetRuntimePackVersion(string tfm = BuildTestBase.DefaultTargetFramework) => s_runtimePackVersions[tfm];
        public string GetRuntimePackDir(string tfm = BuildTestBase.DefaultTargetFramework)
            => Path.Combine(WorkloadPacksDir, $"Microsoft.NETCore.App.Runtime.Mono.{DefaultRuntimeIdentifier}", GetRuntimePackVersion(tfm));
        public string GetRuntimeNativeDir(string tfm = BuildTestBase.DefaultTargetFramework)
            => Path.Combine(GetRuntimePackDir(tfm), "runtimes", DefaultRuntimeIdentifier, "native");

        protected static string s_directoryBuildPropsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.targets"));

        protected static string s_directoryBuildPropsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.targets"));
    }
}
