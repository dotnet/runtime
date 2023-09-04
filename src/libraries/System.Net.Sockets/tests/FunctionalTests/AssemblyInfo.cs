// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

[assembly: SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets is not supported on Browser")]