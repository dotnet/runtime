// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

[assembly: ActiveIssue("https://github.com/dotnet/runtime/issues/35917", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Drawing.Common is not supported on Browser")]
