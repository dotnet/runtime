// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

[assembly: ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
[assembly: ActiveIssue("https://github.com/dotnet/runtime/issues/74667", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLinuxArm64))]
[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Net.Requests is not supported on Browser.")]
