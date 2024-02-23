// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import type {
    DiagnosticOptions,
} from "./shared/types";
import { is_nullish } from "../types/internal";
import type { VoidPtr } from "../types/emscripten";
import { getController, startDiagnosticServer } from "./browser/controller";
import * as memory from "../memory";
import { mono_log_warn } from "../logging";
import { mono_assert, runtimeHelpers } from "../globals";


// called from C on the main thread
export function mono_wasm_event_pipe_early_startup_callback(): void {
    if (WasmEnableThreads) {
        return;
    }
}

// Initialization flow
///   * The runtime calls configure_diagnostics with options from MonoConfig
///   * We start the diagnostic server which connects to the host and waits for some configurations (an IPC CollectTracing command)
///   * The host sends us the configurations and we push them onto the startup_session_configs array and let the startup resume
///   * The runtime calls mono_wasm_initA_diagnostics with any options from MonoConfig
///   * The runtime C layer calls mono_wasm_event_pipe_early_startup_callback during startup once native EventPipe code is initialized
///   * We start all the sessiosn in startup_session_configs and allow them to start streaming
///   * The IPC sessions first send an IPC message with the session ID and then they start streaming
////  * If the diagnostic server gets more commands it will send us a message through the serverController and we will start additional sessions

let suspendOnStartup = false;
let diagnosticsServerEnabled = false;

let diagnosticsInitialized = false;

export async function mono_wasm_init_diagnostics(): Promise<void> {
    if (!WasmEnableThreads) return;
    if (diagnosticsInitialized) return;

    const options = diagnostic_options_from_environment();
    if (!options)
        return;
    diagnosticsInitialized = true;
    if (!is_nullish(options?.server)) {
        if (options.server.connectUrl === undefined || typeof (options.server.connectUrl) !== "string") {
            throw new Error("server.connectUrl must be a string");
        }
        const url = options.server.connectUrl;
        const suspend = boolsyOption(options.server.suspend);
        const controller = await startDiagnosticServer(url);
        if (controller) {
            diagnosticsServerEnabled = true;
            if (suspend) {
                suspendOnStartup = true;
            }
        }
    }
}

function boolsyOption(x: string | boolean): boolean {
    if (x === true || x === false)
        return x;
    if (typeof x === "string") {
        if (x === "true")
            return true;
        if (x === "false")
            return false;
    }
    throw new Error(`invalid option: "${x}", should be true, false, or "true" or "false"`);
}

/// Parse environment variables for diagnostics configuration
///
/// The environment variables are:
///  * DOTNET_DiagnosticPorts
///
function diagnostic_options_from_environment(): DiagnosticOptions | null {
    const val = runtimeHelpers.config.environmentVariables ? runtimeHelpers.config.environmentVariables["DOTNET_DiagnosticPorts"] : undefined;
    if (is_nullish(val))
        return null;
    // TODO: consider also parsing the DOTNET_EnableEventPipe and DOTNET_EventPipeOutputPath, DOTNET_EvnetPipeConfig variables
    // to configure the startup sessions that will dump output to the VFS.
    return diagnostic_options_from_ports_spec(val);
}

/// Parse a DOTNET_DiagnosticPorts string and return a DiagnosticOptions object.
/// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-port#configure-additional-diagnostic-ports
function diagnostic_options_from_ports_spec(val: string): DiagnosticOptions | null {
    if (val === "")
        return null;
    const ports = val.split(";");
    if (ports.length === 0)
        return null;
    if (ports.length !== 1) {
        mono_log_warn("multiple diagnostic ports specified, only the last one will be used");
    }
    const portSpec = ports[ports.length - 1];
    const components = portSpec.split(",");
    if (components.length < 1 || components.length > 3) {
        mono_log_warn("invalid diagnostic port specification, should be of the form <port>[,<connect>],[<nosuspend|suspend>]");
        return null;
    }
    const uri: string = components[0];
    let connect = true;
    let suspend = true;
    // the C Diagnostic Server goes through these parts in reverse, do the same here.
    for (let i = components.length - 1; i >= 1; i--) {
        const component = components[i];
        switch (component.toLowerCase()) {
            case "nosuspend":
                suspend = false;
                break;
            case "suspend":
                suspend = true;
                break;
            case "listen":
                connect = false;
                break;
            case "connect":
                connect = true;
                break;
            default:
                mono_log_warn(`invalid diagnostic port specification component: ${component}`);
                break;
        }
    }
    if (!connect) {
        mono_log_warn("this runtime does not support listening on a diagnostic port; no diagnostic server started");
        return null;
    }
    return {
        server: {
            connectUrl: uri,
            suspend: suspend,
        }
    };

}

export function mono_wasm_diagnostic_server_on_runtime_server_init(out_options: VoidPtr): void {
    mono_assert(WasmEnableThreads, "The diagnostic server requires threads to be enabled during build time.");
    if (diagnosticsServerEnabled) {
        /* called on the main thread when the runtime is sufficiently initialized */
        const controller = getController();
        controller.postServerAttachToRuntime();
        // FIXME: is this really the best place to do this?
        memory.setI32(out_options, suspendOnStartup ? 1 : 0);
    }
}

