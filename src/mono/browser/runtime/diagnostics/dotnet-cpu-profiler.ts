// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import type { DiagnosticCommandOptions } from "../types";

import { commandStopTracing, commandSampleProfiler } from "./client-commands";
import { loaderHelpers, Module, runtimeHelpers } from "./globals";
import { serverSession, setup_js_client } from "./diag-js";
import { IDiagSession } from "./common";
import { MonoMethod } from "../types/internal";
import { mono_log_debug } from "./logging";

export function collectCpuSamples (options?:DiagnosticCommandOptions):Promise<Uint8Array[]> {
    if (!options) options = {};
    if (!serverSession) {
        throw new Error("No active JS diagnostic session");
    }
    if (!is_instrument_method_enabled()) {
        throw new Error("method instrumentation is not enabled, please set DOTNET_WasmPerfInstrumentation=\"1\" before runtime startup to enable it");
    }

    const onClosePromise = loaderHelpers.createPromiseController<Uint8Array[]>();
    function onSessionStart (session: IDiagSession): void {
        // stop tracing after period of monitoring
        Module.safeSetTimeout(() => {
            session.sendCommand(commandStopTracing(session.session_id));
        }, 1000 * (options?.durationSeconds ?? 60));
    }

    setup_js_client({
        onClosePromise:onClosePromise.promise_control,
        skipDownload:options.skipDownload,
        commandOnAdvertise: () => commandSampleProfiler(options.extraProviders || []),
        onSessionStart,
    });
    return onClosePromise.promise;
}

export function mono_wasm_instrument_method (method:MonoMethod):number {
    if (!is_instrument_method_enabled()) {
        return 0;
    }

    if (BuildConfiguration === "Debug") {
        const chars = runtimeHelpers.mono_wasm_method_get_name_ex(method);
        const methodName = runtimeHelpers.utf8ToString(chars);
        runtimeHelpers.free(chars as any);
        if (methodName[0] !== "A") {
            mono_log_debug(`skipped method ${methodName}`);
            return 0;
        }
        mono_log_debug(`instrumenting method ${methodName}`);
    }

    // TODO filter by method name, namespace, etc.
    return 1;
}

export function is_instrument_method_enabled (): boolean {
    const environmentVariables = runtimeHelpers.config.environmentVariables || {};
    const value = environmentVariables["DOTNET_WasmPerfInstrumentation"];
    return value == "1" || value == "true";
}

