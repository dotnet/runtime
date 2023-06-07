import { MonoConfig } from "../types";
import { MonoConfigInternal } from "../types/internal";
import { deep_merge_config, normalizeConfig } from "./config";
import { ENVIRONMENT_IS_WEB, loaderHelpers } from "./globals";
import { mono_log_debug } from "./logging";

export const monoSymbol = "__mono_message_please_dont_collide__"; //Symbol("mono");

export function setupPreloadChannelToMainThread() {
    const channel = new MessageChannel();
    const workerPort = channel.port1;
    const mainPort = channel.port2;
    workerPort.addEventListener("message", (event) => {
        const config = JSON.parse(event.data.config) as MonoConfig;
        onMonoConfigReceived(config);
        workerPort.close();
        mainPort.close();
    }, { once: true });
    workerPort.start();
    self.postMessage(makePreloadMonoMessage(mainPort), [mainPort]);
}

let workerMonoConfigReceived = false;

// called when the main thread sends us the mono config
function onMonoConfigReceived(config: MonoConfigInternal): void {
    if (workerMonoConfigReceived) {
        mono_log_debug("mono config already received");
        return;
    }

    deep_merge_config(loaderHelpers.config, config);
    normalizeConfig();
    mono_log_debug("mono config received");
    workerMonoConfigReceived = true;
    loaderHelpers.afterConfigLoaded.promise_control.resolve(loaderHelpers.config);

    if (ENVIRONMENT_IS_WEB && config.forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
        loaderHelpers.setup_proxy_console("pthread-worker", console, self.location.href);
    }
}

export function makePreloadMonoMessage<TPort>(port: TPort): any {
    return {
        [monoSymbol]: {
            monoCmd: WorkerMonoCommandType.preload,
            port
        }
    };
}

const enum WorkerMonoCommandType {
    channelCreated = "channel_created",
    preload = "preload",
}
