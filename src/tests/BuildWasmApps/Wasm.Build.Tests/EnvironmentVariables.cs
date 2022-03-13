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
        internal static readonly string? AppRefDir                 = Environment.GetEnvironmentVariable("AppRefDir");
        internal static readonly string? WasmBuildSupportDir       = Environment.GetEnvironmentVariable("WasmBuildSupportDir");
        internal static readonly string? EMSDK_PATH                = Environment.GetEnvironmentVariable("EMSDK_PATH");
        internal static readonly string? TestLogPath               = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
        internal static readonly string? SkipProjectCleanup        = Environment.GetEnvironmentVariable("SKIP_PROJECT_CLEANUP");
        internal static readonly string? XHarnessCliPath           = Environment.GetEnvironmentVariable("XHARNESS_CLI_PATH");
    }
}
