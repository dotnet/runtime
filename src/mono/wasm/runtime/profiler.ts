// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import { AOTProfilerOptions, CoverageProfilerOptions } from "./types";

// Initialize the AOT profiler with OPTIONS.
// Requires the AOT profiler to be linked into the app.
// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
// send_to defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
// DumpAotProfileData stores the data into INTERNAL.aot_profile_data.
//
export function mono_wasm_init_aot_profiler(options: AOTProfilerOptions): void {
    if (options == null)
        options = {};
    if (!("write_at" in options))
        options.write_at = "Interop/Runtime::StopProfile";
    if (!("send_to" in options))
        options.send_to = "Interop/Runtime::DumpAotProfileData";
    const arg = "aot:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
    Module.ccall("mono_wasm_load_profiler_aot", null, ["string"], [arg]);
}

// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
// send_to defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
// DumpCoverageProfileData stores the data into INTERNAL.coverage_profile_data.
export function mono_wasm_init_coverage_profiler(options: CoverageProfilerOptions): void {
    if (options == null)
        options = {};
    if (!("write_at" in options))
        options.write_at = "WebAssembly.Runtime::StopProfile";
    if (!("send_to" in options))
        options.send_to = "WebAssembly.Runtime::DumpCoverageProfileData";
    const arg = "coverage:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
    Module.ccall("mono_wasm_load_profiler_coverage", null, ["string"], [arg]);
}
