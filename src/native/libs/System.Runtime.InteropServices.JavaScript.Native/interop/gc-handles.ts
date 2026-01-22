// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { dotnetAssert, dotnetLogger, dotnetLoaderExports } from "./cross-module";

import type { GCHandle, JSHandle, WeakRefInternal } from "./types";
import { GCHandleNull } from "./types";
import { assertJsInterop, isRuntimeRunning } from "./utils";
import { useWeakRef, createStrongRef, createWeakRef } from "./weak-ref";
import { releaseJsOwnedObjectByGcHandle } from "./managed-exports";

export const boundCsFunctionSymbol = Symbol.for("wasm bound_cs_function");
export const boundJsFunctionSymbol = Symbol.for("wasm bound_js_function");
export const importedJsFunctionSymbol = Symbol.for("wasm imported_js_function");
export const proxyDebugSymbol = Symbol.for("wasm proxyDebug");
export const promiseHolderSymbol = Symbol.for("wasm promise_holder");

let forceDisposeProxiesInProgress = false;

const useFinalizationRegistry = typeof globalThis.FinalizationRegistry === "function";
let jsOwnedObjectRegistry: FinalizationRegistry<any>;

// this is array, not map. We maintain list of gaps in _JsHandleFreeList so that it could be as compact as possible
// 0th element is always null, because JSHandle == 0 is invalid handle.
const _CsOwnedObjectsByJsHandle: any[] = [null];
const _CsOwnedObjectsByJsvHandle: any[] = [null];
const _JsHandleFreeList: JSHandle[] = [];
let _nextJSHandle = 1;

export const jsOwnedObjectTable = new Map<GCHandle, WeakRefInternal<any>>();

const _GcvHandleFreeList: GCHandle[] = [];
let nextGcvHandle = -2;

export const jsImportWrapperByFnHandle: Function[] = <any>[null];// 0th slot is dummy, main thread we free them on shutdown. On web worker thread we free them when worker is detached.
export const exportsByAssembly: Map<string, any> = new Map();

// GCVHandle is like GCHandle, but it's not tracked and allocated by the coreCLR GC, but just by JS.
// It's used when we need to create GCHandle-like identity ahead of time, before calling coreCLR.
// they have negative values, so that they don't collide with GCHandles.
export function allocGcvHandle(): GCHandle {
    const gcvHandle = _GcvHandleFreeList.length ? _GcvHandleFreeList.pop() : nextGcvHandle--;
    return gcvHandle as any;
}

export function freeGcvHandle(gcvHandle: GCHandle): void {
    _GcvHandleFreeList.push(gcvHandle);
}

export function isJsvHandle(jsHandle: JSHandle): boolean {
    return (jsHandle as any) < -1;
}

export function isJsHandle(jsHandle: JSHandle): boolean {
    return (jsHandle as any) > 0;
}

export function isGcvHandle(gcHandle: GCHandle): boolean {
    return (gcHandle as any) < -1;
}

// NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
if (useFinalizationRegistry) {
    jsOwnedObjectRegistry = new globalThis.FinalizationRegistry(_jsOwnedObjectFinalized);
}

export const jsOwnedGcHandleSymbol = Symbol.for("wasm jsOwnedGcHandle");
export const csOwnedJsHandleSymbol = Symbol.for("wasm cs_owned_jsHandle");
export const doNotForceDispose = Symbol.for("wasm doNotForceDispose");


export function getJSObjectFromJSHandle(jsHandle: JSHandle): any {
    if (isJsHandle(jsHandle))
        return _CsOwnedObjectsByJsHandle[<any>jsHandle];
    if (isJsvHandle(jsHandle))
        return _CsOwnedObjectsByJsvHandle[0 - <any>jsHandle];
    return null;
}

export function getJsHandleFromJSObject(jsObj: any): JSHandle {
    assertJsInterop();
    if (jsObj[csOwnedJsHandleSymbol]) {
        return jsObj[csOwnedJsHandleSymbol];
    }
    const jsHandle = _JsHandleFreeList.length ? _JsHandleFreeList.pop() : _nextJSHandle++;

    // note _cs_owned_objects_by_jsHandle is list, not Map. That's why we maintain _jsHandle_free_list.
    _CsOwnedObjectsByJsHandle[<any>jsHandle] = jsObj;

    if (Object.isExtensible(jsObj)) {
        const isPrototype = typeof jsObj === "function" && Object.prototype.hasOwnProperty.call(jsObj, "prototype");
        if (!isPrototype) {
            jsObj[csOwnedJsHandleSymbol] = jsHandle;
        }
    }
    // else
    //   The consequence of not adding the csOwnedJsHandleSymbol is, that we could have multiple JSHandles and multiple proxy instances.
    //   Throwing exception would prevent us from creating any proxy of non-extensible things.
    //   If we have weakmap instead, we would pay the price of the lookup for all proxies, not just non-extensible objects.

    return jsHandle as JSHandle;
}

export function registerWithJsvHandle(jsObj: any, jsvHandle: JSHandle) {
    assertJsInterop();
    // note _cs_owned_objects_by_jsHandle is list, not Map. That's why we maintain _jsHandle_free_list.
    _CsOwnedObjectsByJsvHandle[0 - <any>jsvHandle] = jsObj;

    if (Object.isExtensible(jsObj)) {
        jsObj[csOwnedJsHandleSymbol] = jsvHandle;
    }
}

// note: in MT, this is called from locked JSProxyContext. Don't call anything that would need locking.
export function releaseCSOwnedObject(jsHandle: JSHandle): void {
    let obj: any;
    if (isJsHandle(jsHandle)) {
        obj = _CsOwnedObjectsByJsHandle[<any>jsHandle];
        _CsOwnedObjectsByJsHandle[<any>jsHandle] = undefined;
        _JsHandleFreeList.push(jsHandle);
    } else if (isJsvHandle(jsHandle)) {
        obj = _CsOwnedObjectsByJsvHandle[0 - <any>jsHandle];
        _CsOwnedObjectsByJsvHandle[0 - <any>jsHandle] = undefined;
        // see free list in JSProxyContext.FreeJSVHandle
    }
    dotnetAssert.check(obj !== undefined && obj !== null, "ObjectDisposedException");
    if (typeof obj[csOwnedJsHandleSymbol] !== "undefined") {
        obj[csOwnedJsHandleSymbol] = undefined;
    }
}

export function setupManagedProxy(owner: any, gcHandle: GCHandle): void {
    assertJsInterop();
    // keep the gcHandle so that we could easily convert it back to original C# object for roundtrip
    owner[jsOwnedGcHandleSymbol] = gcHandle;

    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    if (useFinalizationRegistry) {
        // register for GC of the C# object after the JS side is done with the object
        jsOwnedObjectRegistry.register(owner, gcHandle, owner);
    }

    // register for instance reuse
    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    const wr = createWeakRef(owner);
    jsOwnedObjectTable.set(gcHandle, wr);
}

export function upgradeManagedProxyToStrongRef(owner: any, gcHandle: GCHandle): void {
    const sr = createStrongRef(owner);
    if (useFinalizationRegistry) {
        jsOwnedObjectRegistry.unregister(owner);
    }
    jsOwnedObjectTable.set(gcHandle, sr);
}

export function teardownManagedProxy(owner: any, gcHandle: GCHandle, skipManaged?: boolean): void {
    assertJsInterop();
    // The JS object associated with this gcHandle has been collected by the JS GC.
    // As such, it's not possible for this gcHandle to be invoked by JS anymore, so
    //  we can release the tracking weakref (it's null now, by definition),
    //  and tell the C# side to stop holding a reference to the managed object.
    // "The FinalizationRegistry callback is called potentially multiple times"
    if (owner) {
        gcHandle = owner[jsOwnedGcHandleSymbol];
        owner[jsOwnedGcHandleSymbol] = GCHandleNull;
        if (useFinalizationRegistry) {
            jsOwnedObjectRegistry.unregister(owner);
        }
    }
    if (gcHandle !== GCHandleNull && jsOwnedObjectTable.delete(gcHandle) && !skipManaged) {
        if (isRuntimeRunning() && !forceDisposeProxiesInProgress) {
            releaseJsOwnedObjectByGcHandle(gcHandle);
        }
    }
    if (isGcvHandle(gcHandle)) {
        freeGcvHandle(gcHandle);
    }
}

export function assertNotDisposed(result: any): GCHandle {
    const gcHandle = result[jsOwnedGcHandleSymbol];
    dotnetAssert.check(gcHandle != GCHandleNull, "ObjectDisposedException");
    return gcHandle;
}

function _jsOwnedObjectFinalized(gcHandle: GCHandle): void {
    if (!isRuntimeRunning()) {
        // We're shutting down, so don't bother doing anything else.
        return;
    }
    teardownManagedProxy(null, gcHandle);
}

export function lookupJsOwnedObject(gcHandle: GCHandle): any {
    if (!gcHandle)
        return null;
    const wr = jsOwnedObjectTable.get(gcHandle);
    if (wr) {
        // this could be null even before _jsOwnedObjectFinalized was called
        // TODO: are there race condition consequences ?
        return wr.deref();
    }
    return null;
}

// when we arrive here from UninstallWebWorkerInterop, the C# will unregister the handles too.
// when called from elsewhere, C# side could be unbalanced!!
export function forceDisposeProxies(disposeMethods: boolean, verbose: boolean): void {
    let keepSomeCsAlive = false;
    let keepSomeJsAlive = false;
    forceDisposeProxiesInProgress = true;

    let doneImports = 0;
    let doneExports = 0;
    let doneGCHandles = 0;
    let doneJSHandles = 0;
    // dispose all proxies to C# objects
    const gcHandles = [...jsOwnedObjectTable.keys()];
    for (const gcHandle of gcHandles) {
        const wr = jsOwnedObjectTable.get(gcHandle);
        const obj = wr && wr.deref();
        if (useFinalizationRegistry && obj) {
            jsOwnedObjectRegistry.unregister(obj);
        }

        if (obj) {
            const keepAlive = typeof obj[doNotForceDispose] === "boolean" && obj[doNotForceDispose];
            if (verbose) {
                const proxyDebug = BuildConfiguration === "Debug" ? obj[proxyDebugSymbol] : undefined;
                if (BuildConfiguration === "Debug" && proxyDebug) {
                    dotnetLogger.warn(`${proxyDebug} ${typeof obj} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                } else {
                    dotnetLogger.warn(`Proxy of C# ${typeof obj} with GCHandle ${gcHandle} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                }
            }
            if (!keepAlive) {
                const promiseControl = dotnetLoaderExports.getPromiseCompletionSource(obj);
                if (promiseControl) {
                    promiseControl.reject(new Error("Process is being terminated."));
                }
                if (typeof obj.dispose === "function") {
                    obj.dispose();
                }
                if (obj[jsOwnedGcHandleSymbol] === gcHandle) {
                    obj[jsOwnedGcHandleSymbol] = GCHandleNull;
                }
                if (!useWeakRef && wr) wr.dispose!();
                doneGCHandles++;
            } else {
                keepSomeCsAlive = true;
            }
        }
    }
    if (!keepSomeCsAlive) {
        jsOwnedObjectTable.clear();
        if (useFinalizationRegistry) {
            jsOwnedObjectRegistry = new globalThis.FinalizationRegistry(_jsOwnedObjectFinalized);
        }
    }
    const freeJsHandle = (jsHandle: number, list: any[]): void => {
        const obj = list[jsHandle];
        const keepAlive = obj && typeof obj[doNotForceDispose] === "boolean" && obj[doNotForceDispose];
        if (!keepAlive) {
            list[jsHandle] = undefined;
        }
        if (obj) {
            if (verbose) {
                const proxyDebug = BuildConfiguration === "Debug" ? obj[proxyDebugSymbol] : undefined;
                if (BuildConfiguration === "Debug" && proxyDebug) {
                    dotnetLogger.warn(`${proxyDebug} ${typeof obj} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                } else {
                    dotnetLogger.warn(`Proxy of JS ${typeof obj} with JSHandle ${jsHandle} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                }
            }
            if (!keepAlive) {
                const promiseControl = dotnetLoaderExports.getPromiseCompletionSource(obj);
                if (promiseControl) {
                    promiseControl.reject(new Error("Process is being terminated."));
                }
                if (typeof obj.dispose === "function") {
                    obj.dispose();
                }
                if (obj[csOwnedJsHandleSymbol] === jsHandle) {
                    obj[csOwnedJsHandleSymbol] = undefined;
                }
                doneJSHandles++;
            } else {
                keepSomeJsAlive = true;
            }
        }
    };
    // dispose all proxies to JS objects
    for (let jsHandle = 0; jsHandle < _CsOwnedObjectsByJsHandle.length; jsHandle++) {
        freeJsHandle(jsHandle, _CsOwnedObjectsByJsHandle);
    }
    for (let jsvHandle = 0; jsvHandle < _CsOwnedObjectsByJsvHandle.length; jsvHandle++) {
        freeJsHandle(jsvHandle, _CsOwnedObjectsByJsvHandle);
    }
    if (!keepSomeJsAlive) {
        _CsOwnedObjectsByJsHandle.length = 1;
        _CsOwnedObjectsByJsvHandle.length = 1;
        _nextJSHandle = 1;
        _JsHandleFreeList.length = 0;
    }
    _GcvHandleFreeList.length = 0;
    nextGcvHandle = -2;

    if (disposeMethods) {
        // dispose all [JSImport]
        for (const boundFn of jsImportWrapperByFnHandle) {
            if (boundFn) {
                const closure = (<any>boundFn)[importedJsFunctionSymbol];
                if (closure) {
                    closure.disposed = true;
                    doneImports++;
                }
            }
        }
        jsImportWrapperByFnHandle.length = 1;

        // dispose all [JSExport]
        const assemblyExports = [...exportsByAssembly.values()];
        for (const assemblyExport of assemblyExports) {
            for (const exportName in assemblyExport) {
                const boundFn = assemblyExport[exportName];
                const closure = boundFn[boundCsFunctionSymbol];
                if (closure) {
                    closure.disposed = true;
                    doneExports++;
                }
            }
        }
        exportsByAssembly.clear();
    }
    dotnetLogger.info(`forceDisposeProxies done: ${doneImports} imports, ${doneExports} exports, ${doneGCHandles} GCHandles, ${doneJSHandles} JSHandles.`);
}
