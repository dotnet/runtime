// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { runtimeHelpers } from "./globals";
import { GCHandle, GCHandleNull, JSHandle, JSHandleDisposed, JSHandleNull, mono_assert } from "./types";
import { create_weak_ref } from "./weak-ref";

const _use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
let _js_owned_object_registry: FinalizationRegistry<any>;

// this is array, not map. We maintain list of gaps in _js_handle_free_list so that it could be as compact as possible
const _cs_owned_objects_by_js_handle: any[] = [];
const _js_handle_free_list: JSHandle[] = [];
let _next_js_handle = 1;

export const _js_owned_object_table = new Map();

// NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
if (_use_finalization_registry) {
    _js_owned_object_registry = new globalThis.FinalizationRegistry(_js_owned_object_finalized);
}

export const js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
export const cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");


export function mono_wasm_get_jsobj_from_js_handle(js_handle: JSHandle): any {
    if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
        return _cs_owned_objects_by_js_handle[<any>js_handle];
    return null;
}

export function get_js_obj(js_handle: JSHandle): any {
    if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
        return mono_wasm_get_jsobj_from_js_handle(js_handle);
    return null;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_get_js_handle(js_obj: any): JSHandle {
    if (js_obj[cs_owned_js_handle_symbol]) {
        return js_obj[cs_owned_js_handle_symbol];
    }
    const js_handle = _js_handle_free_list.length ? _js_handle_free_list.pop() : _next_js_handle++;
    // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
    _cs_owned_objects_by_js_handle[<number>js_handle!] = js_obj;

    if (Object.isExtensible(js_obj)) {
        js_obj[cs_owned_js_handle_symbol] = js_handle;
    }
    // else
    //   The consequence of not adding the cs_owned_js_handle_symbol is, that we could have multiple JSHandles and multiple proxy instances.
    //   Throwing exception would prevent us from creating any proxy of non-extensible things.
    //   If we have weakmap instead, we would pay the price of the lookup for all proxies, not just non-extensible objects.

    return js_handle as JSHandle;
}

export function mono_wasm_release_cs_owned_object(js_handle: JSHandle): void {
    const obj = _cs_owned_objects_by_js_handle[<any>js_handle];
    if (typeof obj !== "undefined" && obj !== null) {
        // if this is the global object then do not
        // unregister it.
        if (globalThis === obj)
            return;

        if (typeof obj[cs_owned_js_handle_symbol] !== "undefined") {
            obj[cs_owned_js_handle_symbol] = undefined;
        }

        _cs_owned_objects_by_js_handle[<any>js_handle] = undefined;
        _js_handle_free_list.push(js_handle);
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function setup_managed_proxy(result: any, gc_handle: GCHandle): void {
    // keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
    result[js_owned_gc_handle_symbol] = gc_handle;

    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    if (_use_finalization_registry) {
        // register for GC of the C# object after the JS side is done with the object
        _js_owned_object_registry.register(result, gc_handle, result);
    }

    // register for instance reuse
    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    const wr = create_weak_ref(result);
    _js_owned_object_table.set(gc_handle, wr);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function teardown_managed_proxy(result: any, gc_handle: GCHandle): void {
    // The JS object associated with this gc_handle has been collected by the JS GC.
    // As such, it's not possible for this gc_handle to be invoked by JS anymore, so
    //  we can release the tracking weakref (it's null now, by definition),
    //  and tell the C# side to stop holding a reference to the managed object.
    // "The FinalizationRegistry callback is called potentially multiple times"
    if (result) {
        gc_handle = result[js_owned_gc_handle_symbol];
        result[js_owned_gc_handle_symbol] = GCHandleNull;
        if (_use_finalization_registry) {
            _js_owned_object_registry.unregister(result);
        }
    }
    if (gc_handle !== GCHandleNull && _js_owned_object_table.delete(gc_handle)) {
        runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle(gc_handle);
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function assert_not_disposed(result: any): GCHandle {
    const gc_handle = result[js_owned_gc_handle_symbol];
    mono_assert(gc_handle != GCHandleNull, "ObjectDisposedException");
    return gc_handle;
}

function _js_owned_object_finalized(gc_handle: GCHandle): void {
    teardown_managed_proxy(null, gc_handle);
}

export function _lookup_js_owned_object(gc_handle: GCHandle): any {
    if (!gc_handle)
        return null;
    const wr = _js_owned_object_table.get(gc_handle);
    if (wr) {
        return wr.deref();
        // TODO: could this be null before _js_owned_object_finalized was called ?
        // TODO: are there race condition consequences ?
    }
    return null;
}

