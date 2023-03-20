// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import MonoWasmThreads from "consts:monoWasmThreads";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_WORKER, INTERNAL, Module, runtimeHelpers } from "./imports";
import { replaceEmscriptenPThreadLibrary } from "./pthreads/shared/emscripten-replacements";
import { DotnetModuleConfigImports, EarlyReplacements } from "./types";
import { TypedArray } from "./types/emscripten";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;

export function init_polyfills(replacements: EarlyReplacements): void {

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

    // require replacement
    const imports = Module.imports = (Module.imports || {}) as DotnetModuleConfigImports;
    const requireWrapper = (wrappedRequire: Function) => (name: string) => {
        const resolved = (<any>Module.imports)[name];
        if (resolved) {
            return resolved;
        }
        return wrappedRequire(name);
    };
    if (imports.require) {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(imports.require));
    }
    else if (replacements.require) {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper(replacements.require));
    } else if (replacements.requirePromise) {
        runtimeHelpers.requirePromise = replacements.requirePromise.then(require => requireWrapper(require));
    } else {
        runtimeHelpers.requirePromise = replacements.requirePromise = Promise.resolve(requireWrapper((name: string) => {
            throw new Error(`Please provide Module.imports.${name} or Module.imports.require`);
        }));
    }

    // script location
    runtimeHelpers.scriptDirectory = replacements.scriptDirectory = detectScriptDirectory(replacements);
    Module.mainScriptUrlOrBlob = replacements.scriptUrl;// this is needed by worker threads
    if (BuildConfiguration === "Debug") {
        console.debug(`MONO_WASM: starting script ${replacements.scriptUrl}`);
        console.debug(`MONO_WASM: starting in ${runtimeHelpers.scriptDirectory}`);
    }
    if (Module.__locateFile === Module.locateFile) {
        // above it's our early version from dotnet.es6.pre.js, we could replace it with better
        Module.locateFile = runtimeHelpers.locateFile = (path) => {
            if (isPathAbsolute(path)) return path;
            return runtimeHelpers.scriptDirectory + path;
        };
    } else {
        // we use what was given to us
        runtimeHelpers.locateFile = Module.locateFile!;
    }

    // prefer fetch_like over global fetch for assets
    replacements.fetch = runtimeHelpers.fetch_like = imports.fetch || fetch_like;

    // misc
    replacements.noExitRuntime = ENVIRONMENT_IS_WEB;

    // threads
    if (MonoWasmThreads) {
        if (replacements.pthreadReplacements) {
            replaceEmscriptenPThreadLibrary(replacements.pthreadReplacements);
        }
    }

    // memory
    const originalUpdateMemoryViews = replacements.updateMemoryViews;
    runtimeHelpers.updateMemoryViews = replacements.updateMemoryViews = () => {
        originalUpdateMemoryViews();
    };
}

export async function init_polyfills_async(): Promise<void> {
    if (ENVIRONMENT_IS_NODE) {
        // wait for locateFile setup on NodeJs
        INTERNAL.require = await runtimeHelpers.requirePromise;
        if (globalThis.performance === dummyPerformance) {
            const { performance } = INTERNAL.require("perf_hooks");
            globalThis.performance = performance;
        }

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

const dummyPerformance = {
    now: function () {
        return Date.now();
    }
};

export async function fetch_like(url: string, init?: RequestInit): Promise<Response> {
    const imports = Module.imports as DotnetModuleConfigImports;
    const hasFetch = typeof (globalThis.fetch) === "function";
    try {
        if (typeof (imports.fetch) === "function") {
            return imports.fetch(url, init || { credentials: "same-origin" });
        }
        else if (ENVIRONMENT_IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (!node_fs) {
                const node_require = await runtimeHelpers.requirePromise;
                node_url = node_require("url");
                node_fs = node_require("fs");
            }
            if (isFileUrl) {
                url = node_url.fileURLToPath(url);
            }

            const arrayBuffer = await node_fs.promises.readFile(url);
            return <Response><any>{
                ok: true,
                headers: [],
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer)
            };
        }
        else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
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
            status: 500,
            statusText: "ERR28: " + e,
            arrayBuffer: () => { throw e; },
            json: () => { throw e; }
        };
    }
    throw new Error("No fetch implementation available");
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

export function detectScriptDirectory(replacements: EarlyReplacements): string {
    if (ENVIRONMENT_IS_WORKER) {
        // Check worker, not web, since window could be polyfilled
        replacements.scriptUrl = self.location.href;
    }
    if (!replacements.scriptUrl) {
        // probably V8 shell in non ES6
        replacements.scriptUrl = "./dotnet.js";
    }
    replacements.scriptUrl = normalizeFileUrl(replacements.scriptUrl);
    return normalizeDirectoryUrl(replacements.scriptUrl);
}

const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path: string): boolean {
    if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }

    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}
