// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import { AOTProfilerOptions, CoverageProfilerOptions } from "./types";

// Initialize the AOT profiler with OPTIONS.
// Requires the AOT profiler to be linked into the app.
// options = { writeAt: "<METHODNAME>", sendTo: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// writeAt defaults to 'WebAssembly.Runtime::StopProfile'.
// sendTo defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
// DumpAotProfileData stores the data into INTERNAL.aotProfileData.
//
export function mono_wasm_init_aot_profiler(options: AOTProfilerOptions): void {
    if (options == null)
        options = {};
    if (!("writeAt" in options))
        options.writeAt = "System.Runtime.InteropServices.JavaScript.JavaScriptExports::StopProfile";
    if (!("sendTo" in options))
        options.sendTo = "Interop/Runtime::DumpAotProfileData";
    const arg = "aot:write-at-method=" + options.writeAt + ",send-to-method=" + options.sendTo;
    Module.ccall("mono_wasm_load_profiler_aot", null, ["string"], [arg]);
}

// options = { writeAt: "<METHODNAME>", sendTo: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// writeAt defaults to 'WebAssembly.Runtime::StopProfile'.
// sendTo defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
// DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.
export function mono_wasm_init_coverage_profiler(options: CoverageProfilerOptions): void {
    if (options == null)
        options = {};
    if (!("writeAt" in options))
        options.writeAt = "WebAssembly.Runtime::StopProfile";
    if (!("sendTo" in options))
        options.sendTo = "WebAssembly.Runtime::DumpCoverageProfileData";
    const arg = "coverage:write-at-method=" + options.writeAt + ",send-to-method=" + options.sendTo;
    Module.ccall("mono_wasm_load_profiler_coverage", null, ["string"], [arg]);
}
