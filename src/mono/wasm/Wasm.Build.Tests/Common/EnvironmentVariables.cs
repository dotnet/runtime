// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Wasm.Build.Tests
{
    internal static class EnvironmentVariables
    {
        internal static readonly string? SdkForWorkloadTestingPath = Environment.GetEnvironmentVariable("SDK_FOR_WORKLOAD_TESTING_PATH");
        internal static readonly string? SdkHasWorkloadInstalled   = Environment.GetEnvironmentVariable("SDK_HAS_WORKLOAD_INSTALLED");
        internal static readonly string? WorkloadPacksVersion      = Environment.GetEnvironmentVariable("WORKLOAD_PACKS_VER");
        internal static readonly string? TestLogPath               = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
        internal static readonly string? SkipProjectCleanup        = Environment.GetEnvironmentVariable("SKIP_PROJECT_CLEANUP");
        internal static readonly string? XHarnessCliPath           = Environment.GetEnvironmentVariable("XHARNESS_CLI_PATH");
        internal static readonly string? BuiltNuGetsPath           = Environment.GetEnvironmentVariable("BUILT_NUGETS_PATH");
        internal static readonly string? BrowserPathForTests       = Environment.GetEnvironmentVariable("BROWSER_PATH_FOR_TESTS");
        internal static readonly string? V8PathForTests            = Environment.GetEnvironmentVariable("V8_PATH_FOR_TESTS");
        internal static readonly bool    IsRunningOnCI             = Environment.GetEnvironmentVariable("IS_RUNNING_ON_CI") is "true";
        internal static readonly bool    ShowBuildOutput           = IsRunningOnCI || Environment.GetEnvironmentVariable("SHOW_BUILD_OUTPUT") is not null;
        internal static readonly bool UseWebcil                    = Environment.GetEnvironmentVariable("USE_WEBCIL_FOR_TESTS") is "true";
        internal static readonly string? SdkDirName                = Environment.GetEnvironmentVariable("SDK_DIR_NAME");
        internal static readonly string? WasiSdkPath               = Environment.GetEnvironmentVariable("WASI_SDK_PATH");
    }
}
