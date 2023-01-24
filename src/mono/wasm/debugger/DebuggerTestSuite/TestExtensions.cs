// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.WebAssembly.Diagnostics;
using Xunit;

namespace DebuggerTests;

internal static class TestExtensions
{
    public static void AssertOk(this Result res, string prefix = "")
        => Assert.True(res.IsOk, $"{prefix}: Expected Ok result but got {res}");

    public static void AssertErr(this Result res, string prefix = "")
        => Assert.False(res.IsOk, $"{prefix}: Expected error but got {res}");
}
