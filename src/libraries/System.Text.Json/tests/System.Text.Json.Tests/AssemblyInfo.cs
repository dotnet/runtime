// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

[assembly: ActiveIssue("Interpreter with debug runtime can be very slow", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter), nameof(PlatformDetection.IsDebugRuntime))]
