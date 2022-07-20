import MonoWasmThreads from "consts:monoWasmThreads";
import { ENVIRONMENT_IS_ESM, ENVIRONMENT_IS_NODE, INTERNAL, Module, runtimeHelpers } from "./imports";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;

export function init_polyfills(): void {
    // performance.now() is used by emscripten and doesn't work in JSC
    if (typeof globalThis.performance === "undefined") {
        globalThis.performance = dummyPerformance as any;
    }
    if (typeof globalThis.URL === "undefined") {
        globalThis.URL = class URL {
            private url;
            constructor(url: string) {
                this.url = url;
            }
            toString() {
                return this.url;
            }
        } as any;
    }
    // v8 shell doesn't have Event and EventTarget
    if (MonoWasmThreads && typeof globalThis.Event === "undefined") {
        globalThis.Event = class Event {
            readonly type: string;
            constructor(type: string) {
                this.type = type;
            }
        } as any;
    }
    if (MonoWasmThreads && typeof globalThis.EventTarget === "undefined") {
        globalThis.EventTarget = class EventTarget {
            private listeners = new Map<string, Array<EventListenerOrEventListenerObject>>();
            addEventListener(type: string, listener: EventListenerOrEventListenerObject | null, options?: boolean | AddEventListenerOptions) {
                if (listener === undefined || listener == null)
                    return;
                if (options !== undefined)
                    throw new Error("FIXME: addEventListener polyfill doesn't implement options");
                if (!this.listeners.has(type)) {
                    this.listeners.set(type, []);
                }
                const listeners = this.listeners.get(type);
                if (listeners === undefined) {
                    throw new Error("can't happen");
                }
                listeners.push(listener);
            }
            removeEventListener(type: string, listener: EventListenerOrEventListenerObject | null, options?: boolean | EventListenerOptions) {
                if (listener === undefined || listener == null)
                    return;
                if (options !== undefined) {
                    throw new Error("FIXME: removeEventListener polyfill doesn't implement options");
                }
                if (!this.listeners.has(type)) {
                    return;
                }
                const listeners = this.listeners.get(type);
                if (listeners === undefined)
                    return;
                const index = listeners.indexOf(listener);
                if (index > -1) {
                    listeners.splice(index, 1);
                }
            }
            dispatchEvent(event: Event) {
                if (!this.listeners.has(event.type)) {
                    return true;
                }
                const listeners = this.listeners.get(event.type);
                if (listeners === undefined) {
                    return true;
                }
                for (const listener of listeners) {
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
}

export async function init_polyfills_async(): Promise<void> {
    if (ENVIRONMENT_IS_NODE && ENVIRONMENT_IS_ESM) {
        // wait for locateFile setup on NodeJs
        INTERNAL.require = await runtimeHelpers.requirePromise;
        if (globalThis.performance === dummyPerformance) {
            const { performance } = INTERNAL.require("perf_hooks");
            globalThis.performance = performance;
        }
    }
}

const dummyPerformance = {
    now: function () {
        return Date.now();
    }
};

export async function fetch_like(url: string): Promise<Response> {
    try {
        if (ENVIRONMENT_IS_NODE) {
            if (!node_fs) {
                const node_require = await runtimeHelpers.requirePromise;
                node_url = node_require("url");
                node_fs = node_require("fs");
            }
            if (url.startsWith("file://")) {
                url = node_url.fileURLToPath(url);
            }

            const arrayBuffer = await node_fs.promises.readFile(url);
            return <Response><any>{
                ok: true,
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer)
            };
        }
        else if (typeof (globalThis.fetch) === "function") {
            return globalThis.fetch(url, { credentials: "same-origin" });
        }
        else if (typeof (read) === "function") {
            // note that it can't open files with unicode names, like Stra<unicode char - Latin Small Letter Sharp S>e.xml
            // https://bugs.chromium.org/p/v8/issues/detail?id=12541
            const arrayBuffer = new Uint8Array(read(url, "binary"));
            return <Response><any>{
                ok: true,
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(Module.UTF8ArrayToString(arrayBuffer, 0, arrayBuffer.length))
            };
        }
    }
    catch (e: any) {
        return <Response><any>{
            ok: false,
            url,
            arrayBuffer: () => { throw e; },
            json: () => { throw e; }
        };
    }
    throw new Error("No fetch implementation available");
}
