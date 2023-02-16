// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Workloads.Testing;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BuildEnvironment
    {
        public string                           DotNet                        => _testProfile.DotNet;
        public bool                             IsWorkload                    => _testProfile.HasWorkload;
        public string                           DefaultBuildArgs              => _testProfile.DefaultBuildArgs;
        public IDictionary<string, string>      EnvVars                       => _testProfile.EnvVars;
        public string                           DirectoryBuildPropsContents   { get; init; }
        public string                           DirectoryBuildTargetsContents { get; init; }
        public string                           LogRootPath                   => TestConfiguration.Instance.LogRootPath;

        public string                           WorkloadPacksDir              => _testProfile.WorkloadPacksDir;
        public string                           BuiltNuGetsPath               => TestConfiguration.Instance.BuiltNuGetsPath;

        public bool UseWebcil { get; init; }
        private readonly TestProfile _testProfile;

        public static string           RelativeTestAssetsPath => TestConfiguration.RelativeTestAssetsPath;
        public static string           TestAssetsPath => TestConfiguration.TestAssetsPath;
        public static string           TestDataPath => TestConfiguration.TestDataPath;
        public static string           TmpPath => TestConfiguration.TmpPath;

        public BuildEnvironment(TestProfile? testProfile = null)
        {
            _testProfile ??= TestConfiguration.Instance.GetTestProfileByDotnetId("latest");
            if (IsWorkload)
            {
                DirectoryBuildPropsContents = s_directoryBuildPropsForWorkloads;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForWorkloads;
            }
            else
            {
                DirectoryBuildPropsContents = s_directoryBuildPropsForLocal;
                DirectoryBuildTargetsContents = s_directoryBuildTargetsForLocal;
            }

            UseWebcil = EnvironmentVariables.UseWebcil;

            // FIXME: move to settings, and make it required?
            if (TestConfiguration.Instance.BuiltNuGetsPath is null || !Directory.Exists(TestConfiguration.Instance.BuiltNuGetsPath))
                throw new Exception($"Cannot find 'BUILT_NUGETS_PATH={EnvironmentVariables.BuiltNuGetsPath}'");

            _testProfile.EnvVars["EM_WORKAROUND_PYTHON_BUG_34780"] = "1";
            // helps with debugging
            _testProfile.EnvVars["WasmNativeStrip"] = "false";
        }

        // FIXME: error checks
        public string GetRuntimePackVersion(string tfm = BuildTestBase.DefaultTargetFramework) => _testProfile.GetRuntimePackVersion(tfm);
        public string GetRuntimePackDir(string tfm = BuildTestBase.DefaultTargetFramework) => _testProfile.GetRuntimePackDir(tfm);
        public string GetRuntimeNativeDir(string tfm = BuildTestBase.DefaultTargetFramework) => _testProfile.GetRuntimeNativeDir(tfm);

        protected static string s_directoryBuildPropsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForWorkloads = File.ReadAllText(Path.Combine(TestDataPath, "Workloads.Directory.Build.targets"));

        protected static string s_directoryBuildPropsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.props"));
        protected static string s_directoryBuildTargetsForLocal = File.ReadAllText(Path.Combine(TestDataPath, "Local.Directory.Build.targets"));
    }
}
