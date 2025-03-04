// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, VoidPtr } from "./types/emscripten";

import { ENVIRONMENT_IS_WEB, mono_assert, runtimeHelpers } from "./globals";
import { MonoMethod, AOTProfilerOptions, BrowserProfilerOptions, LogProfilerOptions } from "./types/internal";
import { profiler_c_functions as cwraps } from "./cwraps";
import { utf8ToString } from "./strings";
import { free } from "./memory";

// Initialize the AOT profiler with OPTIONS.
// Requires the AOT profiler to be linked into the app.
// options = { writeAt: "<METHODNAME>", sendTo: "<METHODNAME>" }
// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
// writeAt defaults to 'WebAssembly.Runtime::StopProfile'.
// sendTo defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
// DumpAotProfileData stores the data into INTERNAL.aotProfileData.
//
export function mono_wasm_init_aot_profiler (options: AOTProfilerOptions): void {
    mono_assert(runtimeHelpers.emscriptenBuildOptions.enableAotProfiler, "AOT profiler is not enabled, please use <WasmProfilers>aot;</WasmProfilers> in your project file.");
    if (options == null)
        options = {};
    if (!("writeAt" in options))
        options.writeAt = "System.Runtime.InteropServices.JavaScript.JavaScriptExports::StopProfile";
    if (!("sendTo" in options))
        options.sendTo = "Interop/Runtime::DumpAotProfileData";
    const arg = "aot:write-at-method=" + options.writeAt + ",send-to-method=" + options.sendTo;
    cwraps.mono_wasm_profiler_init_aot(arg);
}

export function mono_wasm_init_browser_profiler (options: BrowserProfilerOptions): void {
    mono_assert(runtimeHelpers.emscriptenBuildOptions.enableBrowserProfiler, "Browser profiler is not enabled, please use <WasmProfilers>browser;</WasmProfilers> in your project file.");
    if (options == null)
        options = {};
    let arg = "browser:";
    if (typeof options.callSpec === "string") {
        arg += `callspec=${options.callSpec},`;
    }
    if (typeof options.sampleIntervalMs === "number") {
        arg += `interval=${options.sampleIntervalMs},`;
    }
    cwraps.mono_wasm_profiler_init_browser(arg);
}

export function mono_wasm_init_log_profiler (options: LogProfilerOptions): void {
    mono_assert(runtimeHelpers.emscriptenBuildOptions.enableLogProfiler, "Log profiler is not enabled, please use <WasmProfilers>log;</WasmProfilers> in your project file.");
    mono_assert(options.takeHeapshot, "Log profiler is not enabled, the takeHeapshot method must be defined in LogProfilerOptions.takeHeapshot");
    cwraps.mono_wasm_profiler_init_log( (options.configuration || "log:alloc,output=output.mlpd") + `,take-heapshot-method=${options.takeHeapshot}`);
}

export const enum MeasuredBlock {
    emscriptenStartup = "mono.emscriptenStartup",
    instantiateWasm = "mono.instantiateWasm",
    preInit = "mono.preInit",
    preInitWorker = "mono.preInitWorker",
    preRun = "mono.preRun",
    preRunWorker = "mono.preRunWorker",
    onRuntimeInitialized = "mono.onRuntimeInitialized",
    postRun = "mono.postRun",
    postRunWorker = "mono.postRunWorker",
    startRuntime = "mono.startRuntime",
    loadRuntime = "mono.loadRuntime",
    bindingsInit = "mono.bindingsInit",
    bindJsFunction = "mono.bindJsFunction:",
    bindCsFunction = "mono.bindCsFunction:",
    callJsFunction = "mono.callJsFunction:",
    callCsFunction = "mono.callCsFunction:",
    getAssemblyExports = "mono.getAssemblyExports:",
    instantiateAsset = "mono.instantiateAsset:",
}

export type TimeStamp = {
    __brand: "TimeStamp"
}

export function startMeasure (): TimeStamp {
    if (runtimeHelpers.enablePerfMeasure) {
        return globalThis.performance.now() as any;
    }
    return undefined as any;
}

export function endMeasure (start: TimeStamp, block: string, id?: string) {
    if (runtimeHelpers.enablePerfMeasure && start) {
        // API is slightly different between web and Nodejs
        const options = ENVIRONMENT_IS_WEB
            ? { start: start as any }
            : { startTime: start as any };
        const name = id ? `${block}${id} ` : block;
        globalThis.performance.measure(name, options);
    }
}

export function mono_wasm_profiler_now (): number {
    return globalThis.performance.now();
}

export function mono_wasm_profiler_free_method (method: MonoMethod): void {
    methodNames.delete(method as any);
}

const methodNames: Map<number, string> = new Map();
export function mono_wasm_profiler_record (method: MonoMethod, start: number): void {
    const options = ENVIRONMENT_IS_WEB
        ? { start: start }
        : { startTime: start };
    let methodName = methodNames.get(method as any);
    if (!methodName) {
        const chars = cwraps.mono_wasm_method_get_name_ex(method);
        methodName = utf8ToString(chars);
        methodNames.set(method as any, methodName);
        free(chars as any);
    }
    globalThis.performance.measure(methodName, options);
}

/* eslint-disable @typescript-eslint/no-unused-vars */
export function ds_rt_websocket_create (urlPtr :CharPtr):number {
    throw new Error("TODO");
}

export function ds_rt_websocket_send (client_socket :number, buffer:VoidPtr, bytes_to_write:number):number {
    throw new Error("TODO");
}

export function ds_rt_websocket_poll (client_socket :number):number {
    throw new Error("TODO");
}

export function ds_rt_websocket_recv (client_socket :number, buffer:VoidPtr, bytes_to_read:number):number {
    throw new Error("TODO");
}

export function ds_rt_websocket_close (client_socket :number):number {
    throw new Error("TODO");
}
