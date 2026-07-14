// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfigInternal } from "./types";
import { dotnetApi } from "./cross-module";
import { ENVIRONMENT_IS_WEB } from "./per-module";

let theConsoleApi: any = null;
let consoleWebSocket: WebSocket | undefined = undefined;
const methods = ["log", "debug", "info", "warn", "error", "trace"];
let originalConsoleMethods: { [key: string]: any } = {};

export function installLoggingProxy() {
    const loaderConfig = dotnetApi.getConfig() as LoaderConfigInternal;
    if (ENVIRONMENT_IS_WEB && loaderConfig.forwardConsole && typeof globalThis.WebSocket != "undefined") {
        setupProxyConsole(globalThis.console, globalThis.location.origin);
    }
}

function setupProxyConsole(console: Console, origin: string): void {
    theConsoleApi = console as any;
    originalConsoleMethods = {
        ...console
    };

    const consoleUrl = `${origin}/console`.replace("https://", "wss://").replace("http://", "ws://");

    consoleWebSocket = new WebSocket(consoleUrl);
    consoleWebSocket.addEventListener("error", logWSError);
    consoleWebSocket.addEventListener("close", logWSClose);

    setupWS();
}

export function teardownProxyConsole(message?: string) {
    let counter = 30;
    const stopWhenWSBufferEmpty = () => {
        if (!consoleWebSocket) {
            if (message && originalConsoleMethods) {
                originalConsoleMethods.log(message);
            }
        } else if (consoleWebSocket.bufferedAmount === 0 || counter === 0) {
            if (message) {
                // tell xharness WasmTestMessagesProcessor we are done.
                // note this sends last few bytes into the same WS
                if (consoleWebSocket && consoleWebSocket.readyState === WebSocket.OPEN) {
                    consoleWebSocket.send(message);
                } else {
                    originalConsoleMethods.log(message);
                }
            }
            setupOriginal();

            consoleWebSocket.removeEventListener("error", logWSError);
            consoleWebSocket.removeEventListener("close", logWSClose);
            if (consoleWebSocket.readyState === WebSocket.OPEN || consoleWebSocket.readyState === WebSocket.CONNECTING) {
                consoleWebSocket.close(1000, message);
            }
            (consoleWebSocket as any) = undefined;
        } else {
            counter--;
            globalThis.setTimeout(stopWhenWSBufferEmpty, 100);
        }
    };
    stopWhenWSBufferEmpty();
}

function proxyConsoleMethod(level: string) {
    return function proxy(...args: any[]) {
        if (!consoleWebSocket || consoleWebSocket.readyState !== WebSocket.OPEN) {
            originalConsoleMethods[level](...args);
            return;
        }
        try {
            let payload = args[0];
            if (payload === undefined) payload = "undefined";
            else if (payload === null) payload = "null";
            else if (typeof payload === "function") payload = payload.toString();
            else if (typeof payload !== "string") {
                try {
                    payload = JSON.stringify(payload);
                } catch (e) {
                    payload = payload.toString();
                }
            }
            consoleWebSocket.send(JSON.stringify({
                method: `console.${level}`,
                payload: payload,
                arguments: args.slice(1)
            }));
        } catch (err) {
            originalConsoleMethods.error(`proxyConsole failed: ${err}`);
        }
    };
}

function logWSError(event: Event) {
    originalConsoleMethods.error(`proxy console websocket error: ${event}`, event);
    setupOriginal();
}

function logWSClose(event: Event) {
    originalConsoleMethods.debug(`proxy console websocket closed: ${event}`, event);
}

function setupWS() {
    for (const m of methods) {
        theConsoleApi[m] = proxyConsoleMethod(m);
    }
}

function setupOriginal() {
    for (const m of methods) {
        theConsoleApi[m] = originalConsoleMethods[m];
    }
}
