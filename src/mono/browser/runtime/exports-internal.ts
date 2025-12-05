// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { MonoObjectNull, type MonoObject } from "./types/internal";
import cwraps, { profiler_c_functions, threads_c_functions as twraps } from "./cwraps";
import { monoWasmSendDbgCommandWithParms, monoWasmSendDbgCommand, monoWasmGetDbgCommandInfo, monoWasmGetDetails, monoWasmReleaseObject, monoWasmCallFunctionOn, monoWasmDebuggerResume, monoWasmDetachDebugger, monoWasmRaiseDebugEvent, monoWasmChangeDebuggerLogLevel, monoWasmDebuggerAttached } from "./debug";
import { httpWasmSupportsStreamingRequest, httpWasmSupportsStreamingResponse, httpWasmCreateController, httpWasmAbort, httpWasmTransformStreamWrite, httpWasmTransformStreamClose, httpWasmFetch, httpWasmFetchStream, httpWasmFetchBytes, httpWasmGetResponseHeaderNames, httpWasmGetResponseHeaderValues, httpWasmGetResponseBytes, httpWasmGetResponseLength, httpWasmGetStreamedResponseBytes, httpWasmGetResponseType, httpWasmGetResponseStatus } from "./http";
import { exportedRuntimeAPI, Module, runtimeHelpers } from "./globals";
import { getProperty, setProperty, hasProperty, getTypeofProperty, getGlobalThis, dynamicImport } from "./invoke-js";
import { monoWasmStringifyAsErrorWithStack } from "./logging";
import { wsWasmCreate, wsWasmOpen, wsWasmSend, wsWasmReceive, wsWasmClose, wsWasmAbort, wsGetState } from "./web-socket";
import { monoWasmGetLoadedFiles } from "./assets";
import { jiterpreterDumpStats } from "./jiterpreter";
import { interpPgoLoadData, interpPgoSaveData } from "./interp-pgo";
import { getOptions, applyOptions } from "./jiterpreter-support";
import { monoWasmGcLock, monoWasmGcUnlock } from "./gc-lock";
import { loadLazyAssembly } from "./lazyLoading";
import { loadSatelliteAssemblies } from "./satelliteAssemblies";
import { forceDisposeProxies } from "./gc-handles";
import { monoWasmGetFuncIdToNameMappings } from "./logging";
import { monoStringToStringUnsafe } from "./strings";
import { monoWasmBindCsFunction } from "./invoke-cs";

import { monoWasmDumpThreads } from "./pthreads";

export function export_internal (): any {
    return {
        // tests
        monoWasmExit: (exit_code: number) => {
            Module.err("early exit " + exit_code);
        },
        forceDisposeProxies,
        monoWasmDumpThreads: WasmEnableThreads ? monoWasmDumpThreads : undefined,

        // with mono_wasm_debugger_log and mono_wasm_trace_logger
        logging: undefined,

        monoWasmStringifyAsErrorWithStack,

        // used in debugger DevToolsHelper.cs
        monoWasmGetLoadedFiles,
        monoWasmSendDbgCommandWithParms,
        monoWasmSendDbgCommand,
        monoWasmGetDbgCommandInfo,
        monoWasmGetDetails,
        monoWasmReleaseObject,
        monoWasmCallFunctionOn,
        monoWasmDebuggerResume,
        monoWasmDetachDebugger,
        monoWasmRaiseDebugEvent,
        monoWasmChangeDebuggerLogLevel,
        monoWasmDebuggerAttached,
        monoWasmRuntimeIsReady: runtimeHelpers.monoWasmRuntimeIsReady,
        monoWasmGetFuncIdToNameMappings,

        // interop
        getProperty,
        setProperty,
        hasProperty,
        getTypeofProperty,
        getGlobalThis,
        getDotnetInstance: () => exportedRuntimeAPI,
        dynamicImport,
        monoWasmBindCsFunction,

        // BrowserWebSocket
        wsWasmCreate,
        wsWasmOpen,
        wsWasmSend,
        wsWasmReceive,
        wsWasmClose,
        wsWasmAbort,
        wsGetState,

        // BrowserHttpHandler
        httpWasmSupportsStreamingRequest,
        httpWasmSupportsStreamingResponse,
        httpWasmCreateController,
        httpWasmGetResponseType,
        httpWasmGetResponseStatus,
        httpWasmAbort,
        httpWasmTransformStreamWrite,
        httpWasmTransformStreamClose,
        httpWasmFetch,
        httpWasmFetchStream,
        httpWasmFetchBytes,
        httpWasmGetResponseHeaderNames,
        httpWasmGetResponseHeaderValues,
        httpWasmGetResponseBytes,
        httpWasmGetResponseLength,
        httpWasmGetStreamedResponseBytes,

        // jiterpreter
        jiterpreterDumpStats,
        jiterpreter_apply_options: applyOptions,
        jiterpreter_get_options: getOptions,

        // interpreter pgo
        interpPgoLoadData,
        interpPgoSaveData,

        // Blazor GC Lock support
        monoWasmGcLock,
        monoWasmGcUnlock,

        // Blazor legacy replacement
        monoObjectAsBoolOrNullUnsafe,
        monoStringToStringUnsafe,

        loadLazyAssembly,
        loadSatelliteAssemblies
    };
}

export function cwraps_internal (internal: any): void {
    Object.assign(internal, {
        monoWasmExit: cwraps.monoWasmExit,
        mono_wasm_profiler_init_aot: profiler_c_functions.mono_wasm_profiler_init_aot,
        mono_wasm_profiler_init_browser_devtools: profiler_c_functions.mono_wasm_profiler_init_browser_devtools,
        mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
        mono_wasm_print_thread_dump: WasmEnableThreads ? twraps.mono_wasm_print_thread_dump : undefined,
    });
}

/* @deprecated not GC safe, legacy support for Blazor */
export function monoObjectAsBoolOrNullUnsafe (obj: MonoObject): boolean | null {
    // TODO https://github.com/dotnet/runtime/issues/100411
    // after Blazor stops using monoObjectAsBoolOrNullUnsafe

    if (obj === MonoObjectNull) {
        return null;
    }
    const res = cwraps.mono_wasm_read_as_bool_or_null_unsafe(obj);
    if (res === 0) {
        return false;
    }
    if (res === 1) {
        return true;
    }
    return null;
}
