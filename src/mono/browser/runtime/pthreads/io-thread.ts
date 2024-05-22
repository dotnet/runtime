// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { mono_log_error, mono_log_info } from "../logging";
import { monoThreadInfo, postMessageToMain, update_thread_info } from "./shared";
import { Module, loaderHelpers } from "../globals";
import { WorkerToMainMessageType } from "../types/internal";
import { threads_c_functions as tcwraps } from "../cwraps";

export function mono_wasm_start_io_thread_async () {
    if (!WasmEnableThreads) return;


    if (BuildConfiguration === "Debug" && globalThis.setInterval) globalThis.setInterval(() => {
        mono_log_info("I/O thread is alive!");
    }, 3000);

    try {
        monoThreadInfo.isIo = true;
        monoThreadInfo.threadName = "JS I/O Thread";
        update_thread_info();
        tcwraps.mono_wasm_register_io_thread();
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.ioStarted,
            info: monoThreadInfo,
        });
        Module.runtimeKeepalivePush();
    } catch (err) {
        mono_log_error("mono_wasm_start_io_thread_async() failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }

    // same as emscripten_exit_with_live_runtime()
    throw "unwind";
}
