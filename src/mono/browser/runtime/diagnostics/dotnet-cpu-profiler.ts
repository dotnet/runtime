// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { commandStopTracing, commandSampleProfiler } from "./client-commands";
import { loaderHelpers, Module, runtimeHelpers } from "./globals";
import { serverSession, setup_js_client } from "./diag-js";
import { IDiagSession } from "./common";

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
let filter: boolean | undefined = undefined;
export function is_instrument_method_enabled (): boolean {
    if (filter !== undefined) {
        return filter!;
    }

    const environmentVariables = runtimeHelpers.config.environmentVariables || {};
    const value = environmentVariables["DOTNET_WasmPerfInstrumentation"];

    if (value == null || value == undefined || value == "0" || value == "false" || value == "") {
        filter = false;
    } else {
        filter = true;
    }
    return filter;
}
