// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let theConsoleApi: any = null;
let consoleWebSocket: WebSocket | undefined = undefined;
const methods = ["log", "debug", "info", "warn", "error", "trace"];
let originalConsoleMethods: { [key: string]: any } = {};

function proxyConsoleMethod(prefix: string, func: any, asJson: boolean) {
    return function proxy(...args: any[]) {
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

            if (asJson) {
                func(JSON.stringify({
                    method: prefix,
                    payload: payload,
                    arguments: args.slice(1)
                }));
            } else {
                func([prefix + payload, ...args.slice(1)]);
            }
        } catch (err) {
            originalConsoleMethods.error(`proxyConsole failed: ${err}`);
        }
    };
}

export function setupProxyConsole(console: Console, origin: string): void {
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
        } else if (consoleWebSocket.bufferedAmount == 0 || counter == 0) {
            if (message) {
                // tell xharness WasmTestMessagesProcessor we are done.
                // note this sends last few bytes into the same WS
                consoleWebSocket.send(message);
            }
            setupOriginal();

            consoleWebSocket.removeEventListener("error", logWSError);
            consoleWebSocket.removeEventListener("close", logWSClose);
            consoleWebSocket.close(1000, message);
            (consoleWebSocket as any) = undefined;
        } else {
            counter--;
            globalThis.setTimeout(stopWhenWSBufferEmpty, 100);
        }
    };
    stopWhenWSBufferEmpty();
}

function send(msg: string) {
    if (consoleWebSocket && consoleWebSocket.readyState === WebSocket.OPEN) {
        consoleWebSocket.send(msg);
    } else {
        originalConsoleMethods.log(msg);
    }
}

function logWSError(event: Event) {
    originalConsoleMethods.error(`proxy console websocket error: ${event}`, event);
}

function logWSClose(event: Event) {
    originalConsoleMethods.debug(`proxy console websocket closed: ${event}`, event);
}

function setupWS() {
    for (const m of methods) {
        theConsoleApi[m] = proxyConsoleMethod(`console.${m}`, send, true);
    }
}

function setupOriginal() {
    for (const m of methods) {
        theConsoleApi[m] = proxyConsoleMethod(`console.${m}`, originalConsoleMethods.log, false);
    }
}
