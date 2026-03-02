// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, NativeBrowserExports, NativeBrowserExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

import { _ems_ } from "../../Common/JavaScript/ems-ambient";
import GitHash from "consts:gitHash";

export { SystemJS_RandomBytes } from "./crypto";
export { SystemJS_GetLocaleInfo } from "./globalization-locale";
export { SystemJS_RejectMainPromise, SystemJS_ResolveMainPromise, SystemJS_MarkAsyncMain, SystemJS_ConsoleClear } from "./main";
export { SystemJS_ScheduleTimer, SystemJS_ScheduleBackgroundJob, SystemJS_ScheduleFinalization } from "./scheduling";

export const gitHash = GitHash;
export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");

    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== _ems_.DOTNET.gitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, DOTNET: ${_ems_.DOTNET.gitHash}`);
    }

    internals[InternalExchangeIndex.NativeBrowserExportsTable] = nativeBrowserExportsToTable({
        getWasmMemory,
        getWasmTable,
    });
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);

    setupEmscripten();

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function nativeBrowserExportsToTable(map: NativeBrowserExports): NativeBrowserExportsTable {
        // keep in sync with nativeBrowserExportsFromTable()
        return [
            map.getWasmMemory,
            map.getWasmTable,
        ];
    }

    function getWasmMemory(): WebAssembly.Memory {
        return _ems_.wasmMemory;
    }

    function getWasmTable(): WebAssembly.Table {
        return _ems_.wasmTable;
    }

    function setupEmscripten() {
        _ems_.Module.preInit = [() => {
            if (_ems_.dotnetApi.getConfig) {
                const virtualWorkingDirectory = _ems_.dotnetApi.getConfig().virtualWorkingDirectory;
                _ems_.FS.createPath("/", virtualWorkingDirectory!, true, true);
                _ems_.FS.chdir(virtualWorkingDirectory!);
            }

            const orig_funcs_on_exit = _ems_.___funcs_on_exit;
            // it would be better to use addOnExit(), but it's called too late.
            _ems_.___funcs_on_exit = () => {
                // this will prevent more timers (like finalizer) to get scheduled during thread destructor
                if (_ems_.dotnetBrowserUtilsExports.abortBackgroundTimers) {
                    _ems_.dotnetBrowserUtilsExports.abortBackgroundTimers();
                }
                _ems_.EXITSTATUS = _ems_._BrowserHost_ShutdownDotnet(_ems_.EXITSTATUS || 0);
                orig_funcs_on_exit();
            };

        }, ...(_ems_.Module.preInit || [])];
    }
}
