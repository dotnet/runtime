// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { assert_js_interop, js_import_wrapper_by_fn_handle } from "./invoke-js";
import { mono_log_info, mono_log_warn } from "./logging";
import { bound_cs_function_symbol, imported_js_function_symbol, proxy_debug_symbol } from "./marshal";
import { GCHandle, GCHandleNull, JSHandle, WeakRefInternal } from "./types/internal";
import { _use_weak_ref, create_weak_ref } from "./weak-ref";
import { exportsByAssembly } from "./invoke-cs";

const _use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
let _js_owned_object_registry: FinalizationRegistry<any>;

// this is array, not map. We maintain list of gaps in _js_handle_free_list so that it could be as compact as possible
// 0th element is always null, because JSHandle == 0 is invalid handle.
const _cs_owned_objects_by_js_handle: any[] = [null];
const _cs_owned_objects_by_jsv_handle: any[] = [null];
const _js_handle_free_list: JSHandle[] = [];
let _next_js_handle = 1;

export const _js_owned_object_table = new Map<GCHandle, WeakRefInternal<any>>();

const _gcv_handle_free_list: GCHandle[] = [];
let _next_gcv_handle = -2;

// GCVHandle is like GCHandle, but it's not tracked and allocated by the mono GC, but just by JS.
// It's used when we need to create GCHandle-like identity ahead of time, before calling Mono.
// they have negative values, so that they don't collide with GCHandles.
export function alloc_gcv_handle(): GCHandle {
    const gcv_handle = _gcv_handle_free_list.length ? _gcv_handle_free_list.pop() : _next_gcv_handle--;
    return gcv_handle as any;
}

export function free_gcv_handle(gcv_handle: GCHandle): void {
    _gcv_handle_free_list.push(gcv_handle);
}

export function is_jsv_handle(js_handle: JSHandle): boolean {
    return (js_handle as any) < -1;
}

export function is_js_handle(js_handle: JSHandle): boolean {
    return (js_handle as any) > 0;
}

export function is_gcv_handle(gc_handle: GCHandle): boolean {
    return (gc_handle as any) < -1;
}

// NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
if (_use_finalization_registry) {
    _js_owned_object_registry = new globalThis.FinalizationRegistry(_js_owned_object_finalized);
}

export const js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
export const cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");
export const do_not_force_dispose = Symbol.for("wasm do_not_force_dispose");


export function mono_wasm_get_jsobj_from_js_handle(js_handle: JSHandle): any {
    if (is_js_handle(js_handle))
        return _cs_owned_objects_by_js_handle[<any>js_handle];
    if (is_jsv_handle(js_handle))
        return _cs_owned_objects_by_jsv_handle[0 - <any>js_handle];
    return null;
}

export function mono_wasm_get_js_handle(js_obj: any): JSHandle {
    assert_js_interop();
    if (js_obj[cs_owned_js_handle_symbol]) {
        return js_obj[cs_owned_js_handle_symbol];
    }
    const js_handle = _js_handle_free_list.length ? _js_handle_free_list.pop() : _next_js_handle++;

    // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
    _cs_owned_objects_by_js_handle[<any>js_handle] = js_obj;

    if (Object.isExtensible(js_obj)) {
        js_obj[cs_owned_js_handle_symbol] = js_handle;
    }
    // else
    //   The consequence of not adding the cs_owned_js_handle_symbol is, that we could have multiple JSHandles and multiple proxy instances.
    //   Throwing exception would prevent us from creating any proxy of non-extensible things.
    //   If we have weakmap instead, we would pay the price of the lookup for all proxies, not just non-extensible objects.

    return js_handle as JSHandle;
}

export function register_with_jsv_handle(js_obj: any, jsv_handle: JSHandle) {
    assert_js_interop();
    // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
    _cs_owned_objects_by_jsv_handle[0 - <any>jsv_handle] = js_obj;

    if (Object.isExtensible(js_obj)) {
        js_obj[cs_owned_js_handle_symbol] = jsv_handle;
    }
}

// note: in MT, this is called from locked JSProxyContext. Don't call anything that would need locking.
export function mono_wasm_release_cs_owned_object(js_handle: JSHandle): void {
    let obj: any;
    if (is_js_handle(js_handle)) {
        obj = _cs_owned_objects_by_js_handle[<any>js_handle];
        _cs_owned_objects_by_js_handle[<any>js_handle] = undefined;
        _js_handle_free_list.push(js_handle);
    }
    else if (is_jsv_handle(js_handle)) {
        obj = _cs_owned_objects_by_jsv_handle[0 - <any>js_handle];
        _cs_owned_objects_by_jsv_handle[0 - <any>js_handle] = undefined;
        // see free list in JSProxyContext.FreeJSVHandle
    }
    mono_assert(obj !== undefined && obj !== null, "ObjectDisposedException");
    if (typeof obj[cs_owned_js_handle_symbol] !== "undefined") {
        obj[cs_owned_js_handle_symbol] = undefined;
    }
}

export function setup_managed_proxy(owner: any, gc_handle: GCHandle): void {
    assert_js_interop();
    // keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
    owner[js_owned_gc_handle_symbol] = gc_handle;

    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    if (_use_finalization_registry) {
        // register for GC of the C# object after the JS side is done with the object
        _js_owned_object_registry.register(owner, gc_handle, owner);
    }

    // register for instance reuse
    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    const wr = create_weak_ref(owner);
    _js_owned_object_table.set(gc_handle, wr);
}

export function teardown_managed_proxy(owner: any, gc_handle: GCHandle, skipManaged?: boolean): void {
    assert_js_interop();
    // The JS object associated with this gc_handle has been collected by the JS GC.
    // As such, it's not possible for this gc_handle to be invoked by JS anymore, so
    //  we can release the tracking weakref (it's null now, by definition),
    //  and tell the C# side to stop holding a reference to the managed object.
    // "The FinalizationRegistry callback is called potentially multiple times"
    if (owner) {
        gc_handle = owner[js_owned_gc_handle_symbol];
        owner[js_owned_gc_handle_symbol] = GCHandleNull;
        if (_use_finalization_registry) {
            _js_owned_object_registry.unregister(owner);
        }
    }
    if (gc_handle !== GCHandleNull && _js_owned_object_table.delete(gc_handle) && !skipManaged) {
        runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle(gc_handle);
    }
    if (is_gcv_handle(gc_handle)) {
        free_gcv_handle(gc_handle);
    }
}

export function assert_not_disposed(result: any): GCHandle {
    const gc_handle = result[js_owned_gc_handle_symbol];
    mono_check(gc_handle != GCHandleNull, "ObjectDisposedException");
    return gc_handle;
}

function _js_owned_object_finalized(gc_handle: GCHandle): void {
    if (loaderHelpers.is_exited()) {
        // We're shutting down, so don't bother doing anything else.
        return;
    }
    teardown_managed_proxy(null, gc_handle);
}

export function _lookup_js_owned_object(gc_handle: GCHandle): any {
    if (!gc_handle)
        return null;
    const wr = _js_owned_object_table.get(gc_handle);
    if (wr) {
        // this could be null even before _js_owned_object_finalized was called
        // TODO: are there race condition consequences ?
        return wr.deref();
    }
    return null;
}

export function assertNoProxies(): void {
    if (!WasmEnableThreads) return;
    mono_assert(_js_owned_object_table.size === 0, "There should be no proxies on this thread.");
    mono_assert(_cs_owned_objects_by_js_handle.length === 1, "There should be no proxies on this thread.");
    mono_assert(_cs_owned_objects_by_jsv_handle.length === 1, "There should be no proxies on this thread.");
    mono_assert(exportsByAssembly.size === 0, "There should be no exports on this thread.");
    mono_assert(js_import_wrapper_by_fn_handle.length === 1, "There should be no imports on this thread.");
}

// when we arrive here from UninstallWebWorkerInterop, the C# will unregister the handles too.
// when called from elsewhere, C# side could be unbalanced!!
export function forceDisposeProxies(disposeMethods: boolean, verbose: boolean): void {
    let keepSomeCsAlive = false;
    let keepSomeJsAlive = false;

    let doneImports = 0;
    let doneExports = 0;
    let doneGCHandles = 0;
    let doneJSHandles = 0;
    // dispose all proxies to C# objects
    const gc_handles = [..._js_owned_object_table.keys()];
    for (const gc_handle of gc_handles) {
        const wr = _js_owned_object_table.get(gc_handle);
        const obj = wr && wr.deref();
        if (_use_finalization_registry && obj) {
            _js_owned_object_registry.unregister(obj);
        }

        if (obj) {
            const keepAlive = typeof obj[do_not_force_dispose] === "boolean" && obj[do_not_force_dispose];
            if (verbose) {
                const proxy_debug = BuildConfiguration === "Debug" ? obj[proxy_debug_symbol] : undefined;
                if (BuildConfiguration === "Debug" && proxy_debug) {
                    mono_log_warn(`${proxy_debug} ${typeof obj} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                } else {
                    mono_log_warn(`Proxy of C# ${typeof obj} with GCHandle ${gc_handle} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                }
            }
            if (!keepAlive) {
                const promise_control = loaderHelpers.getPromiseController(obj);
                if (promise_control) {
                    promise_control.reject(new Error("WebWorker which is origin of the Task is being terminated."));
                }
                if (typeof obj.dispose === "function") {
                    obj.dispose();
                }
                if (obj[js_owned_gc_handle_symbol] === gc_handle) {
                    obj[js_owned_gc_handle_symbol] = GCHandleNull;
                }
                if (!_use_weak_ref && wr) wr.dispose!();
                doneGCHandles++;
            } else {
                keepSomeCsAlive = true;
            }
        }
    }
    if (!keepSomeCsAlive) {
        _js_owned_object_table.clear();
        if (_use_finalization_registry) {
            _js_owned_object_registry = new globalThis.FinalizationRegistry(_js_owned_object_finalized);
        }
    }
    const free_js_handle = (js_handle: number, list: any[]): void => {
        const obj = list[js_handle];
        const keepAlive = obj && typeof obj[do_not_force_dispose] === "boolean" && obj[do_not_force_dispose];
        if (!keepAlive) {
            list[js_handle] = undefined;
        }
        if (obj) {
            if (verbose) {
                const proxy_debug = BuildConfiguration === "Debug" ? obj[proxy_debug_symbol] : undefined;
                if (BuildConfiguration === "Debug" && proxy_debug) {
                    mono_log_warn(`${proxy_debug} ${typeof obj} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                } else {
                    mono_log_warn(`Proxy of JS ${typeof obj} with JSHandle ${js_handle} was still alive. ${keepAlive ? "keeping" : "disposing"}.`);
                }
            }
            if (!keepAlive) {
                const promise_control = loaderHelpers.getPromiseController(obj);
                if (promise_control) {
                    promise_control.reject(new Error("WebWorker which is origin of the Task is being terminated."));
                }
                if (typeof obj.dispose === "function") {
                    obj.dispose();
                }
                if (obj[cs_owned_js_handle_symbol] === js_handle) {
                    obj[cs_owned_js_handle_symbol] = undefined;
                }
                doneJSHandles++;
            } else {
                keepSomeJsAlive = true;
            }
        }
    };
    // dispose all proxies to JS objects
    for (let js_handle = 0; js_handle < _cs_owned_objects_by_js_handle.length; js_handle++) {
        free_js_handle(js_handle, _cs_owned_objects_by_js_handle);
    }
    for (let jsv_handle = 0; jsv_handle < _cs_owned_objects_by_jsv_handle.length; jsv_handle++) {
        free_js_handle(jsv_handle, _cs_owned_objects_by_jsv_handle);
    }
    if (!keepSomeJsAlive) {
        _cs_owned_objects_by_js_handle.length = 1;
        _cs_owned_objects_by_jsv_handle.length = 1;
        _next_js_handle = 1;
        _js_handle_free_list.length = 0;
    }
    _gcv_handle_free_list.length = 0;
    _next_gcv_handle = -2;

    if (disposeMethods) {
        // dispose all [JSImport]
        for (const bound_fn of js_import_wrapper_by_fn_handle) {
            if (bound_fn) {
                const closure = (<any>bound_fn)[imported_js_function_symbol];
                if (closure) {
                    closure.disposed = true;
                    doneImports++;
                }
            }
        }
        js_import_wrapper_by_fn_handle.length = 1;

        // dispose all [JSExport]
        const assemblyExports = [...exportsByAssembly.values()];
        for (const assemblyExport of assemblyExports) {
            for (const exportName in assemblyExport) {
                const bound_fn = assemblyExport[exportName];
                const closure = bound_fn[bound_cs_function_symbol];
                if (closure) {
                    closure.disposed = true;
                    doneExports++;
                }
            }
        }
        exportsByAssembly.clear();
    }
    mono_log_info(`forceDisposeProxies done: ${doneImports} imports, ${doneExports} exports, ${doneGCHandles} GCHandles, ${doneJSHandles} JSHandles.`);
}