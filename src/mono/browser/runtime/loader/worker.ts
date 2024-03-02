// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoConfigInternal, PThreadInfo, WorkerToMainMessageType, monoMessageSymbol } from "../types/internal";
import { MonoConfig } from "../types";
import { deep_merge_config, normalizeConfig } from "./config";
import { ENVIRONMENT_IS_WEB, loaderHelpers, runtimeHelpers } from "./globals";
import { mono_log_debug } from "./logging";

export function setupPreloadChannelToMainThread() {
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", (event) => {
        const config = JSON.parse(event.data.config) as MonoConfig;
        const monoThreadInfo = JSON.parse(event.data.monoThreadInfo) as PThreadInfo;
        onMonoConfigReceived(config, monoThreadInfo);
        workerPort.close();
        mainPort.close();
    }, { once: true });
    workerPort.start();
    // ask for config even before WASM is loaded
    self.postMessage({
        [monoMessageSymbol]: {
            monoCmd: WorkerToMainMessageType.preload,
            port: mainPort
        }
    }, [mainPort]);
}

let workerMonoConfigReceived = false;

// called when the main thread sends us the mono config
function onMonoConfigReceived(config: MonoConfigInternal, monoThreadInfo: PThreadInfo): void {
    if (workerMonoConfigReceived) {
        mono_log_debug("mono config already received");
        return;
    }
    deep_merge_config(loaderHelpers.config, config);
    runtimeHelpers.monoThreadInfo = monoThreadInfo;
    normalizeConfig();
    mono_log_debug("mono config received");
    workerMonoConfigReceived = true;
    loaderHelpers.afterConfigLoaded.promise_control.resolve(loaderHelpers.config);

    if (ENVIRONMENT_IS_WEB && config.forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
        loaderHelpers.setup_proxy_console("worker-idle", console, globalThis.location.origin);
    }
}

