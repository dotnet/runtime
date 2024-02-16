// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import type { EmscriptenReplacements } from "./types/internal";
import type { TypedArray } from "./types/emscripten";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WORKER, INTERNAL, Module, loaderHelpers, runtimeHelpers } from "./globals";
import { replaceEmscriptenPThreadLibrary } from "./pthreads/shared/emscripten-replacements";

const dummyPerformance = {
    now: function () {
        return Date.now();
    }
};

export function initializeReplacements(replacements: EmscriptenReplacements): void {
    // performance.now() is used by emscripten and doesn't work in JSC
    if (typeof globalThis.performance === "undefined") {
        globalThis.performance = dummyPerformance as any;
    }
    replacements.require = INTERNAL.require;

    // script location
    replacements.scriptDirectory = loaderHelpers.scriptDirectory;
    if (Module.locateFile === Module.__locateFile) {
        Module.locateFile = loaderHelpers.locateFile;
    }

    // prefer fetch_like over global fetch for assets
    replacements.fetch = loaderHelpers.fetch_like;

    // misc
    replacements.ENVIRONMENT_IS_WORKER = ENVIRONMENT_IS_WORKER;

    // threads
    if (WasmEnableThreads && replacements.modulePThread) {
        replaceEmscriptenPThreadLibrary(replacements.modulePThread);
    }
}

export async function init_polyfills_async(): Promise<void> {
    // v8 shell doesn't have Event and EventTarget
    if (WasmEnableThreads && typeof globalThis.Event === "undefined") {
        globalThis.Event = class Event {
            readonly type: string;
            constructor(type: string) {
                this.type = type;
            }
        } as any;
    }
    if (WasmEnableThreads && typeof globalThis.EventTarget === "undefined") {
        globalThis.EventTarget = class EventTarget {
            private subscribers = new Map<string, Array<{ listener: EventListenerOrEventListenerObject, oneShot: boolean }>>();
            addEventListener(type: string, listener: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions) {
                if (listener === undefined || listener == null)
                    return;
                let oneShot = false;
                if (options !== undefined) {
                    for (const [k, v] of Object.entries(options)) {
                        if (k === "once") {
                            oneShot = v ? true : false;
                            continue;
                        }
                        throw new Error(`FIXME: addEventListener polyfill doesn't implement option '${k}'`);
                    }
                }
                if (!this.subscribers.has(type)) {
                    this.subscribers.set(type, []);
                }
                const listeners = this.subscribers.get(type);
                if (listeners === undefined) {
                    throw new Error("can't happen");
                }
                listeners.push({ listener, oneShot });
            }
            removeEventListener(type: string, listener: EventListenerOrEventListenerObject | null, options?: boolean | EventListenerOptions) {
                if (listener === undefined || listener == null)
                    return;
                if (options !== undefined) {
                    throw new Error("FIXME: removeEventListener polyfill doesn't implement options");
                }
                if (!this.subscribers.has(type)) {
                    return;
                }
                const subscribers = this.subscribers.get(type);
                if (subscribers === undefined)
                    return;
                let index = -1;
                const n = subscribers.length;
                for (let i = 0; i < n; ++i) {
                    if (subscribers[i].listener === listener) {
                        index = i;
                        break;
                    }
                }
                if (index > -1) {
                    subscribers.splice(index, 1);
                }
            }
            dispatchEvent(event: Event) {
                if (!this.subscribers.has(event.type)) {
                    return true;
                }
                let subscribers = this.subscribers.get(event.type);
                if (subscribers === undefined) {
                    return true;
                }
                let needsCopy = false;
                for (const sub of subscribers) {
                    if (sub.oneShot) {
                        needsCopy = true;
                        break;
                    }
                }
                if (needsCopy) {
                    subscribers = subscribers.slice(0);
                }
                for (const sub of subscribers) {
                    const listener = sub.listener;
                    if (sub.oneShot) {
                        this.removeEventListener(event.type, listener);
                    }
                    if (typeof listener === "function") {
                        listener.call(this, event);
                    } else {
                        listener.handleEvent(event);
                    }
                }
                return true;
            }
        };
    }
    if (ENVIRONMENT_IS_NODE) {
        // wait for locateFile setup on NodeJs
        if (globalThis.performance === dummyPerformance) {
            const { performance } = INTERNAL.require("perf_hooks");
            globalThis.performance = performance;
        }
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        INTERNAL.process = await import(/*! webpackIgnore: true */"process");

        if (!globalThis.crypto) {
            globalThis.crypto = <any>{};
        }
        if (!globalThis.crypto.getRandomValues) {
            let nodeCrypto: any = undefined;
            try {
                nodeCrypto = INTERNAL.require("node:crypto");
            } catch (err: any) {
                // Noop, error throwing polyfill provided bellow
            }

            if (!nodeCrypto) {
                globalThis.crypto.getRandomValues = () => {
                    throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.");
                };
            } else if (nodeCrypto.webcrypto) {
                globalThis.crypto = nodeCrypto.webcrypto;
            } else if (nodeCrypto.randomBytes) {
                globalThis.crypto.getRandomValues = (buffer: TypedArray) => {
                    if (buffer) {
                        buffer.set(nodeCrypto.randomBytes(buffer.length));
                    }
                };
            }
        }
    }
    runtimeHelpers.subtle = globalThis.crypto?.subtle;
}


