// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

[assembly: SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", RuntimeConfiguration.Checked)]
[assembly: ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]

