import MonoWasmThreads from "consts:monoWasmThreads";

import { threads_c_functions as cwraps } from "../../cwraps";
import { forceThreadMemoryViewRefresh, getI32, withStackAlloc } from "../../memory";
import { Module, mono_assert, runtimeHelpers } from "../../globals";
import { Thread, waitForThread } from ".";
import { mono_log_info } from "../../logging";
import { start_mono_vm } from "../../startup";
import { pthread_self } from "../worker";
import { MonoThreadMessage, mono_wasm_install_js_worker_interop } from "../shared";
import { mono_run_main_impl } from "../../run";

export let deputyThread: Thread | undefined;

export async function startDeputyThread(): Promise<void> {
    if (!MonoWasmThreads) {
        return;
    }
    const sizeOfPthreadT = 4;
    const result: number | undefined = withStackAlloc(sizeOfPthreadT, (pthreadIdPtr) => {
        if (!cwraps.mono_wasm_create_deputy_thread(pthreadIdPtr))
            return undefined;
        const pthreadId = getI32(pthreadIdPtr);
        return pthreadId;
    });
    mono_assert(result, "failed to create deputy thread");
    // have to wait until the message port is created
    deputyThread = await waitForThread(result);
    // wait until mono VM started
    await runtimeHelpers.afterStartMonoVM.promise;
}

export async function deputy_run_main(main_assembly_name: string, args: string[]) {
    deputyThread?.postMessageToWorker({
        type: "deputy",
        cmd: "run_main",
        main_assembly_name,
        args
    });
    return runtimeHelpers.runMainResult.promise;
}

export function mono_wasm_setup_deputy_thread() {
    mono_assert(MonoWasmThreads, "Expected MT build");
    if (globalThis.setInterval) globalThis.setInterval(() => {
        mono_log_info("Deputy thread is alive!");
    }, 3000);

    runtimeHelpers.isDeputyThread = true;

    //TODO pop on exit
    Module.runtimeKeepalivePush();

    pthread_self.addEventListenerFromBrowser(async (event: MessageEvent<MonoThreadMessage>) => {
        if (event.data.type == "deputy" && event.data.cmd == "run_main") {
            try {
                forceThreadMemoryViewRefresh();
                const { main_assembly_name, args } = event.data as any;
                const result = await mono_run_main_impl(main_assembly_name, args);
                pthread_self.postMessageToBrowser({
                    type: "deputy",
                    cmd: "run_main_result",
                    result
                });
            }
            catch (ex: any) {
                pthread_self.postMessageToBrowser({
                    type: "deputy",
                    cmd: "run_main_error",
                    reason: "" + ex
                });
            }
        }
    });


    // because this is synchronous method, we will postpone mono startup to the next event loop
    Module.safeSetTimeout(async () => {
        try {
            forceThreadMemoryViewRefresh();
            await start_mono_vm();

            mono_wasm_install_js_worker_interop(1);
            runtimeHelpers.javaScriptExports.install_synchronization_context();

            // tell UI thread that we are ready
            pthread_self.postMessageToBrowser({
                type: "deputy",
                cmd: "ready"
            });
        }
        catch (ex: any) {
            pthread_self.postMessageToBrowser({
                type: "deputy",
                cmd: "abort",
                reason: "" + ex
            });
            runtimeHelpers.abort(ex);
        }
    }, 0);
}
