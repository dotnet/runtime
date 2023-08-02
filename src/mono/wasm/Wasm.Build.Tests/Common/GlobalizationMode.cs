// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;
public enum GlobalizationMode
{
    Sharded,         // chosen based on locale
    Invariant,       // no icu
    FullIcu,         // full icu data: icudt.dat is loaded
    PredefinedIcu,   // user set WasmIcuDataFileName/BlazorIcuDataFileName value and we are loading that file
    Hybrid           // reduced icu, missing data is provided by platform-native functions (web api for wasm)
};
