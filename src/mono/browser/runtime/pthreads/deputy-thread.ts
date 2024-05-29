// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { mono_log_error, mono_log_info } from "../logging";
import { monoThreadInfo, postMessageToMain, update_thread_info } from "./shared";
import { Module, loaderHelpers, runtimeHelpers } from "../globals";
import { start_runtime } from "../startup";
import { MainToWorkerMessageType, WorkerToMainMessageType } from "../types/internal";
import { forceThreadMemoryViewRefresh } from "../memory";
import { install_main_synchronization_context } from "../managed-exports";
import { pthread_self } from "./worker-thread";

export function mono_wasm_start_deputy_thread_async () {
    if (!WasmEnableThreads) return;

    if (BuildConfiguration === "Debug" && globalThis.setInterval) globalThis.setInterval(() => {
        mono_log_info("Deputy thread is alive!");
    }, 3000);

    try {
        monoThreadInfo.isDeputy = true;
        monoThreadInfo.threadName = "Managed Main Deputy";
        update_thread_info();
        postMessageToMain({
            monoCmd: WorkerToMainMessageType.deputyCreated,
            info: monoThreadInfo,
        });
        Module.runtimeKeepalivePush();
        Module.safeSetTimeout(async () => {
            try {
                forceThreadMemoryViewRefresh();

                pthread_self.addEventListenerFromBrowser((message) => {
                    if (message.data.cmd == MainToWorkerMessageType.allAssetsLoaded) {
                        runtimeHelpers.allAssetsInMemory.promise_control.resolve();
                    }
                });

                await start_runtime();

                postMessageToMain({
                    monoCmd: WorkerToMainMessageType.deputyStarted,
                    info: monoThreadInfo,
                });

                await runtimeHelpers.allAssetsInMemory.promise;

                runtimeHelpers.proxyGCHandle = install_main_synchronization_context(runtimeHelpers.config.jsThreadBlockingMode!);

                postMessageToMain({
                    monoCmd: WorkerToMainMessageType.deputyReady,
                    info: monoThreadInfo,
                    deputyProxyGCHandle: runtimeHelpers.proxyGCHandle,
                });
            } catch (err) {
                postMessageToMain({
                    monoCmd: WorkerToMainMessageType.deputyFailed,
                    info: monoThreadInfo,
                    error: "mono_wasm_start_deputy_thread_async() failed" + err,
                });
                mono_log_error("mono_wasm_start_deputy_thread_async() failed", err);
                loaderHelpers.mono_exit(1, err);
                throw err;
            }
        }, 0);
    } catch (err) {
        mono_log_error("mono_wasm_start_deputy_thread_async() failed", err);
        loaderHelpers.mono_exit(1, err);
        throw err;
    }

    // same as emscripten_exit_with_live_runtime()
    throw "unwind";
}
