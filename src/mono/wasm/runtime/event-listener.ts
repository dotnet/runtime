// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root } from "./roots";
import { isChromium, prevent_timer_throttling } from "./scheduling";
import { JSHandle, GCHandle, MonoString, MonoStringNull } from "./types";
import { _wrap_delegate_gc_handle_as_function } from "./cs-to-js";
import { mono_wasm_get_jsobj_from_js_handle, _js_owned_object_finalized, _lookup_js_owned_object, _use_finalization_registry } from "./gc-handles";
import { wrap_error } from "./method-calls";
import { conv_string } from "./strings";

const listener_registration_count_symbol = Symbol.for("wasm listener_registration_count");

export function mono_wasm_add_event_listener(js_handle: JSHandle, name: MonoString, listener_gc_handle: GCHandle, optionsHandle: JSHandle): MonoString {
    const nameRoot = mono_wasm_new_root(name);
    try {
        const sName = conv_string(nameRoot.value);

        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj)
            throw new Error("ERR09: Invalid JS object handle for '" + sName + "'");

        const throttling = isChromium || obj.constructor.name !== "WebSocket"
            ? undefined
            : prevent_timer_throttling;

        const listener = _wrap_delegate_gc_handle_as_function(listener_gc_handle, throttling) as ListenerExtension;
        if (!listener)
            throw new Error("ERR10: Invalid listener gc_handle");

        const options = optionsHandle
            ? mono_wasm_get_jsobj_from_js_handle(optionsHandle)
            : null;

        if (!_use_finalization_registry) {
            // we are counting registrations because same delegate could be registered into multiple sources
            listener[listener_registration_count_symbol] = listener[listener_registration_count_symbol] ? listener[listener_registration_count_symbol] + 1 : 1;
        }

        if (options)
            obj.addEventListener(sName, listener, options);
        else
            obj.addEventListener(sName, listener);
        return MonoStringNull;
    } catch (ex) {
        return wrap_error(null, ex);
    } finally {
        nameRoot.release();
    }
}

export function mono_wasm_remove_event_listener(js_handle: JSHandle, name: MonoString, listener_gc_handle: GCHandle, capture: boolean): MonoString {
    const nameRoot = mono_wasm_new_root(name);
    try {

        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj)
            throw new Error("ERR11: Invalid JS object handle");
        const listener = _lookup_js_owned_object(listener_gc_handle) as ListenerExtension;
        // Removing a nonexistent listener should not be treated as an error
        if (!listener)
            return MonoStringNull;
        const sName = conv_string(nameRoot.value);

        obj.removeEventListener(sName, listener, !!capture);
        // We do not manually remove the listener from the delegate registry here,
        //  because that same delegate may have been used as an event listener for
        //  other events or event targets. The GC will automatically clean it up
        //  and trigger the FinalizationRegistry handler if it's unused

        // When FinalizationRegistry is not supported by this browser, we cleanup manuall after unregistration
        if (!_use_finalization_registry) {
            listener[listener_registration_count_symbol]--;
            if (listener[listener_registration_count_symbol] === 0) {
                _js_owned_object_finalized(listener_gc_handle);
            }
        }

        return MonoStringNull;
    } catch (ex) {
        return wrap_error(null, ex);
    } finally {
        nameRoot.release();
    }
}

type ListenerExtension = Function & {
    [listener_registration_count_symbol]: number
}