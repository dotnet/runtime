// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_log_warn } from "../logging";
import { utf16ToString } from "../strings";

export {
    mono_wasm_main_thread_ptr,
    mono_wasm_pthread_ptr, update_thread_info, isMonoThreadMessage, monoThreadInfo,
} from "./shared";
export { mono_wasm_install_js_worker_interop, mono_wasm_uninstall_js_worker_interop } from "./worker-interop";
export {
    mono_wasm_dump_threads, postCancelThreads,
    populateEmscriptenPool, mono_wasm_init_threads,
    waitForThread, replaceEmscriptenPThreadUI, terminateAllThreads,
} from "./ui-thread";
export {
    mono_wasm_pthread_on_pthread_attached, mono_wasm_pthread_on_pthread_unregistered,
    mono_wasm_pthread_on_pthread_registered, mono_wasm_pthread_set_name, currentWorkerThreadEvents,
    dotnetPthreadCreated, initWorkerThreadEvents, replaceEmscriptenTLSInit, pthread_self
} from "./worker-thread";

export { mono_wasm_start_deputy_thread_async } from "./deputy-thread";
export { mono_wasm_start_io_thread_async } from "./io-thread";

export function mono_wasm_warn_about_blocking_wait (ptr: number, length: number) {
    const warning = utf16ToString(ptr, ptr + (length * 2));
    mono_log_warn(warning);
}
