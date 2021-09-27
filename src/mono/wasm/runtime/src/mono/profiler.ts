// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from '../runtime'

// Initialize the AOT profiler with OPTIONS.
// Requires the AOT profiler to be linked into the app.
// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
// send_to defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
// DumpAotProfileData stores the data into Module.aot_profile_data.
//
export function mono_wasm_init_aot_profiler(options: AOTProfilerOptions) {
    if (options == null)
        options = {}
    if (!('write_at' in options))
        options.write_at = 'Interop/Runtime::StopProfile';
    if (!('send_to' in options))
        options.send_to = 'Interop/Runtime::DumpAotProfileData';
    var arg = "aot:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
    Module.ccall('mono_wasm_load_profiler_aot', null, ['string'], [arg]);
}

// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
// send_to defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
// DumpCoverageProfileData stores the data into Module.coverage_profile_data.
export function mono_wasm_init_coverage_profiler(options: CoverageProfilerOptions) {
    if (options == null)
        options = {}
    if (!('write_at' in options))
        options.write_at = 'WebAssembly.Runtime::StopProfile';
    if (!('send_to' in options))
        options.send_to = 'WebAssembly.Runtime::DumpCoverageProfileData';
    var arg = "coverage:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
    Module.ccall('mono_wasm_load_profiler_coverage', null, ['string'], [arg]);
}

export type AOTProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpAotProfileData' (DumpAotProfileData stores the data into Module.aot_profile_data.)
}
export type CoverageProfilerOptions = {
    write_at?: string, // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::StopProfile'
    send_to?: string // should be in the format <CLASS>::<METHODNAME>, default: 'WebAssembly.Runtime::DumpCoverageProfileData' (DumpCoverageProfileData stores the data into Module.coverage_profile_data.)
}