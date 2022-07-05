import MonoWasmThreads from "consts:monoWasmThreads";
import { ENVIRONMENT_IS_ESM, ENVIRONMENT_IS_NODE, Module, requirePromise } from "./imports";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;

export async function init_polyfills(): Promise<void> {
    // performance.now() is used by emscripten and doesn't work in JSC
    if (typeof globalThis.performance === "undefined") {
        if (ENVIRONMENT_IS_NODE && ENVIRONMENT_IS_ESM) {
            const node_require = await requirePromise;
            const { performance } = node_require("perf_hooks");
            globalThis.performance = performance;
        } else {
            globalThis.performance = {
                now: function () {
                    return Date.now();
                }
            } as any;
        }
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
                const subscribers = this.subscribers.get(event.type);
                if (subscribers === undefined) {
                    return true;
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
}

export async function fetch_like(url: string): Promise<Response> {
    try {
        if (ENVIRONMENT_IS_NODE) {
            if (!node_fs) {
                const node_require = await requirePromise;
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

export function readAsync_like(url: string, onload: Function, onerror: Function): void {
    fetch_like(url).then((res: Response) => {
        onload(res.arrayBuffer());
    }).catch((err) => {
        onerror(err);
    });
}
