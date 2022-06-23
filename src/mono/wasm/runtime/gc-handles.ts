// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import corebindings from "./corebindings";
import { GCHandle, JSHandle, JSHandleDisposed, JSHandleNull, MonoObjectRef } from "./types";
import { setI32_unchecked } from "./memory";
import { create_weak_ref } from "./weak-ref";

export const _use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
export let _js_owned_object_registry: FinalizationRegistry<any>;

// this is array, not map. We maintain list of gaps in _js_handle_free_list so that it could be as compact as possible
const _cs_owned_objects_by_js_handle: any[] = [];
const _js_handle_free_list: JSHandle[] = [];
let _next_js_handle = 1;

const _js_owned_object_table = new Map();

// NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
if (_use_finalization_registry) {
    _js_owned_object_registry = new globalThis.FinalizationRegistry(_js_owned_object_finalized);
}

export const js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
export const cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");


export function get_js_owned_object_by_gc_handle_ref(gc_handle: GCHandle, result: MonoObjectRef): void {
    if (!gc_handle) {
        setI32_unchecked(result, 0);
        return;
    }
    // this is always strong gc_handle
    corebindings._get_js_owned_object_by_gc_handle_ref(gc_handle, result);
}

export function mono_wasm_get_jsobj_from_js_handle(js_handle: JSHandle): any {
    if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
        return _cs_owned_objects_by_js_handle[<any>js_handle];
    return null;
}

// when should_add_in_flight === true, the JSObject would be temporarily hold by Normal gc_handle, so that it would not get collected during transition to the managed stack.
// its InFlight gc_handle would be freed when the instance arrives to managed side via Interop.Runtime.ReleaseInFlight
export function get_cs_owned_object_by_js_handle_ref(js_handle: JSHandle, should_add_in_flight: boolean, result: MonoObjectRef): void {
    if (js_handle === JSHandleNull || js_handle === JSHandleDisposed) {
        setI32_unchecked(result, 0);
        return;
    }
    corebindings._get_cs_owned_object_by_js_handle_ref(js_handle, should_add_in_flight ? 1 : 0, result);
}

export function get_js_obj(js_handle: JSHandle): any {
    if (js_handle !== JSHandleNull && js_handle !== JSHandleDisposed)
        return mono_wasm_get_jsobj_from_js_handle(js_handle);
    return null;
}

export function _js_owned_object_finalized(gc_handle: GCHandle): void {
    // The JS object associated with this gc_handle has been collected by the JS GC.
    // As such, it's not possible for this gc_handle to be invoked by JS anymore, so
    //  we can release the tracking weakref (it's null now, by definition),
    //  and tell the C# side to stop holding a reference to the managed object.
    // "The FinalizationRegistry callback is called potentially multiple times"
    if (_js_owned_object_table.delete(gc_handle)) {
        corebindings._release_js_owned_object_by_gc_handle(gc_handle);
    }
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

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _register_js_owned_object(gc_handle: GCHandle, js_obj: any): void {
    const wr = create_weak_ref(js_obj);
    _js_owned_object_table.set(gc_handle, wr);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_get_js_handle(js_obj: any): JSHandle {
    if (js_obj[cs_owned_js_handle_symbol]) {
        return js_obj[cs_owned_js_handle_symbol];
    }
    const js_handle = _js_handle_free_list.length ? _js_handle_free_list.pop() : _next_js_handle++;
    // note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
    _cs_owned_objects_by_js_handle[<number>js_handle!] = js_obj;
    js_obj[cs_owned_js_handle_symbol] = js_handle;
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
