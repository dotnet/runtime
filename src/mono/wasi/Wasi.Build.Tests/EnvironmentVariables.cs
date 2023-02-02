// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Wasm.Build.Tests
{
    internal static class WasiEnvironmentVariables
    {
        internal static readonly string? WasiSdkPath = Environment.GetEnvironmentVariable("WASI_SDK_PATH");
    }
}
