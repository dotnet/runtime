// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import { INTERNAL, Module, loaderHelpers, runtimeHelpers } from "./globals";
import { toBase64StringImpl } from "./base64";
import cwraps from "./cwraps";
import { VoidPtr, CharPtr } from "./types/emscripten";
import { mono_log_warn } from "./logging";
import { forceThreadMemoryViewRefresh, localHeapViewU8 } from "./memory";
import { utf8ToString } from "./strings";
const commands_received: any = new Map<number, CommandResponse>();
commands_received.remove = function (key: number): CommandResponse { const value = this.get(key); this.delete(key); return value; };
let _call_function_res_cache: any = {};
let _next_call_function_res_id = 0;
let _debugger_buffer_len = -1;
let _debugger_buffer: VoidPtr;
let _assembly_name_str: string; //keep this variable, it's used by BrowserDebugProxy
let _entrypoint_method_token: number; //keep this variable, it's used by BrowserDebugProxy

export function mono_wasm_runtime_ready(): void {
    INTERNAL.mono_wasm_runtime_is_ready = runtimeHelpers.mono_wasm_runtime_is_ready = true;

    // FIXME: where should this go?
    _next_call_function_res_id = 0;
    _call_function_res_cache = {};
    _debugger_buffer_len = -1;

    // DO NOT REMOVE - magic debugger init function
    if ((<any>globalThis).dotnetDebugger)
        // eslint-disable-next-line no-debugger
        debugger;
}

export function mono_wasm_fire_debugger_agent_message_with_data_to_pause(base64String: string): void {
    //keep this console.assert, otherwise optimization will remove the assignments
    // eslint-disable-next-line no-console
    console.assert(true, `mono_wasm_fire_debugger_agent_message_with_data ${base64String}`);
    // eslint-disable-next-line no-debugger
    debugger;
}

export function mono_wasm_fire_debugger_agent_message_with_data(data: number, len: number): void {
    const base64String = toBase64StringImpl(new Uint8Array(localHeapViewU8().buffer, data, len));
    mono_wasm_fire_debugger_agent_message_with_data_to_pause(base64String);
}

export function mono_wasm_add_dbg_command_received(res_ok: boolean, id: number, buffer: number, buffer_len: number): void {
    const dbg_command = new Uint8Array(localHeapViewU8().buffer, buffer, buffer_len);
    const base64String = toBase64StringImpl(dbg_command);
    const buffer_obj = {
        res_ok,
        res: {
            id,
            value: base64String
        }
    };
    if (commands_received.has(id))
        mono_log_warn(`Adding an id (${id}) that already exists in commands_received`);
    commands_received.set(id, buffer_obj);
}

function mono_wasm_malloc_and_set_debug_buffer(command_parameters: string) {
    if (command_parameters.length > _debugger_buffer_len) {
        if (_debugger_buffer)
            Module._free(_debugger_buffer);
        _debugger_buffer_len = Math.max(command_parameters.length, _debugger_buffer_len, 256);
        _debugger_buffer = Module._malloc(_debugger_buffer_len);
    }
    const byteCharacters = atob(command_parameters);
    const heapU8 = localHeapViewU8();
    for (let i = 0; i < byteCharacters.length; i++) {
        heapU8[<any>_debugger_buffer + i] = byteCharacters.charCodeAt(i);
    }
}

export function mono_wasm_send_dbg_command_with_parms(id: number, command_set: number, command: number, command_parameters: string, length: number, valtype: number, newvalue: number): CommandResponseResult {
    forceThreadMemoryViewRefresh();

    mono_wasm_malloc_and_set_debug_buffer(command_parameters);
    cwraps.mono_wasm_send_dbg_command_with_parms(id, command_set, command, _debugger_buffer, length, valtype, newvalue.toString());

    const { res_ok, res } = commands_received.remove(id);
    if (!res_ok)
        throw new Error("Failed on mono_wasm_invoke_method_debugger_agent_with_parms");
    return res;
}

export function mono_wasm_send_dbg_command(id: number, command_set: number, command: number, command_parameters: string): CommandResponseResult {
    forceThreadMemoryViewRefresh();

    mono_wasm_malloc_and_set_debug_buffer(command_parameters);
    cwraps.mono_wasm_send_dbg_command(id, command_set, command, _debugger_buffer, command_parameters.length);

    const { res_ok, res } = commands_received.remove(id);

    if (!res_ok)
        throw new Error("Failed on mono_wasm_send_dbg_command");
    return res;

}

export function mono_wasm_get_dbg_command_info(): CommandResponseResult {
    const { res_ok, res } = commands_received.remove(0);

    if (!res_ok)
        throw new Error("Failed on mono_wasm_get_dbg_command_info");
    return res;
}

export function mono_wasm_debugger_resume(): void {
    forceThreadMemoryViewRefresh();
}

export function mono_wasm_detach_debugger(): void {
    forceThreadMemoryViewRefresh();
    cwraps.mono_wasm_set_is_debugger_attached(false);
}

export function mono_wasm_change_debugger_log_level(level: number): void {
    forceThreadMemoryViewRefresh();
    cwraps.mono_wasm_change_debugger_log_level(level);
}

/**
 * Raises an event for the debug proxy
 */
export function mono_wasm_raise_debug_event(event: WasmEvent, args = {}): void {
    if (typeof event !== "object")
        throw new Error(`event must be an object, but got ${JSON.stringify(event)}`);

    if (event.eventName === undefined)
        throw new Error(`event.eventName is a required parameter, in event: ${JSON.stringify(event)}`);

    if (typeof args !== "object")
        throw new Error(`args must be an object, but got ${JSON.stringify(args)}`);

    // eslint-disable-next-line no-console
    console.debug("mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae", JSON.stringify(event), JSON.stringify(args));
}

export function mono_wasm_wait_for_debugger(): Promise<void> {
    return new Promise<void>((resolve) => {
        const interval = setInterval(() => {
            if (runtimeHelpers.waitForDebugger != 1) {
                return;
            }
            clearInterval(interval);
            resolve();
        }, 100);
    });
}

export function mono_wasm_debugger_attached(): void {
    if (runtimeHelpers.waitForDebugger == -1)
        runtimeHelpers.waitForDebugger = 1;
    forceThreadMemoryViewRefresh();
    cwraps.mono_wasm_set_is_debugger_attached(true);
}

export function mono_wasm_set_entrypoint_breakpoint(entrypoint_method_token: number): void {
    //keep these assignments, these values are used by BrowserDebugProxy
    _assembly_name_str = loaderHelpers.config.mainAssemblyName + ".dll";
    _entrypoint_method_token = entrypoint_method_token;
    //keep this console.assert, otherwise optimization will remove the assignments
    // eslint-disable-next-line no-console
    console.assert(true, `Adding an entrypoint breakpoint ${_assembly_name_str} at method token  ${_entrypoint_method_token}`);
    // eslint-disable-next-line no-debugger
    debugger;

    forceThreadMemoryViewRefresh();
}

function _create_proxy_from_object_id(objectId: string, details: any) {
    if (objectId.startsWith("dotnet:array:")) {
        let ret: Array<any>;
        if (details.items === undefined) {
            ret = details.map((p: any) => p.value);
            return ret;
        }
        if (details.dimensionsDetails === undefined || details.dimensionsDetails.length === 1) {
            ret = details.items.map((p: any) => p.value);
            return ret;
        }
    }

    const proxy: any = {};
    Object.keys(details).forEach(p => {
        const prop = details[p];
        if (prop.get !== undefined) {
            Object.defineProperty(proxy,
                prop.name,
                {
                    get() {
                        return mono_wasm_send_dbg_command(prop.get.id, prop.get.commandSet, prop.get.command, prop.get.buffer);
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(prop.set.id, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue); return true;
                    }
                }
            );
        } else if (prop.set !== undefined) {
            Object.defineProperty(proxy,
                prop.name,
                {
                    get() {
                        return prop.value;
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(prop.set.id, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue); return true;
                    }
                }
            );
        } else {
            proxy[prop.name] = prop.value;
        }
    });
    return proxy;
}

export function mono_wasm_call_function_on(request: CallRequest): CFOResponse {
    forceThreadMemoryViewRefresh();

    if (request.arguments != undefined && !Array.isArray(request.arguments))
        throw new Error(`"arguments" should be an array, but was ${request.arguments}`);

    const objId = request.objectId;
    const details = request.details;
    let proxy: any = {};

    if (objId.startsWith("dotnet:cfo_res:")) {
        if (objId in _call_function_res_cache)
            proxy = _call_function_res_cache[objId];
        else
            throw new Error(`Unknown object id ${objId}`);
    } else {
        proxy = _create_proxy_from_object_id(objId, details);
    }

    const fn_args = request.arguments != undefined ? request.arguments.map(a => JSON.stringify(a.value)) : [];

    const fn_body_template = `const fn = ${request.functionDeclaration}; return fn.apply(proxy, [${fn_args}]);`;
    const fn_defn = new Function("proxy", fn_body_template);
    const fn_res = fn_defn(proxy);

    if (fn_res === undefined)
        return { type: "undefined" };

    if (Object(fn_res) !== fn_res) {
        if (typeof (fn_res) == "object" && fn_res == null)
            return { type: typeof (fn_res), subtype: `${fn_res}`, value: null };
        return { type: typeof (fn_res), description: `${fn_res}`, value: `${fn_res}` };
    }

    if (request.returnByValue && fn_res.subtype == undefined)
        return { type: "object", value: fn_res };

    if (Object.getPrototypeOf(fn_res) == Array.prototype) {

        const fn_res_id = _cache_call_function_res(fn_res);

        return {
            type: "object",
            subtype: "array",
            className: "Array",
            description: `Array(${fn_res.length})`,
            objectId: fn_res_id
        };
    }
    if (fn_res.value !== undefined || fn_res.subtype !== undefined) {
        return fn_res;
    }

    if (fn_res == proxy)
        return { type: "object", className: "Object", description: "Object", objectId: objId };
    const fn_res_id = _cache_call_function_res(fn_res);
    return { type: "object", className: "Object", description: "Object", objectId: fn_res_id };
}

function _get_cfo_res_details(objectId: string, args: any): ValueAsJsonString {
    if (!(objectId in _call_function_res_cache))
        throw new Error(`Could not find any object with id ${objectId}`);

    const real_obj = _call_function_res_cache[objectId];

    const descriptors = Object.getOwnPropertyDescriptors(real_obj);
    if (args.accessorPropertiesOnly) {
        Object.keys(descriptors).forEach(k => {
            if (descriptors[k].get === undefined)
                Reflect.deleteProperty(descriptors, k);
        });
    }

    const res_details: any[] = [];
    Object.keys(descriptors).forEach(k => {
        let new_obj;
        const prop_desc = descriptors[k];
        if (typeof prop_desc.value == "object") {
            // convert `{value: { type='object', ... }}`
            // to      `{ name: 'foo', value: { type='object', ... }}
            new_obj = Object.assign({ name: k }, prop_desc);
        } else if (prop_desc.value !== undefined) {
            // This is needed for values that were not added by us,
            // thus are like { value: 5 }
            // instead of    { value: { type = 'number', value: 5 }}
            //
            // This can happen, for eg., when `length` gets added for arrays
            // or `__proto__`.
            new_obj = {
                name: k,
                // merge/add `type` and `description` to `d.value`
                value: Object.assign({ type: (typeof prop_desc.value), description: "" + prop_desc.value },
                    prop_desc)
            };
        } else if (prop_desc.get !== undefined) {
            // The real_obj has the actual getter. We are just returning a placeholder
            // If the caller tries to run function on the cfo_res object,
            // that accesses this property, then it would be run on `real_obj`,
            // which *has* the original getter
            new_obj = {
                name: k,
                get: {
                    className: "Function",
                    description: `get ${k} () {}`,
                    type: "function"
                }
            };
        } else {
            new_obj = { name: k, value: { type: "symbol", value: "<Unknown>", description: "<Unknown>" } };
        }

        res_details.push(new_obj);
    });

    return { __value_as_json_string__: JSON.stringify(res_details) };
}

type ValueAsJsonString = {
    __value_as_json_string__: string;
}

export function mono_wasm_get_details(objectId: string, args = {}): ValueAsJsonString {
    forceThreadMemoryViewRefresh();
    return _get_cfo_res_details(`dotnet:cfo_res:${objectId}`, args);
}

function _cache_call_function_res(obj: any) {
    const id = `dotnet:cfo_res:${_next_call_function_res_id++}`;
    _call_function_res_cache[id] = obj;
    return id;
}

export function mono_wasm_release_object(objectId: string): void {
    if (objectId in _call_function_res_cache)
        delete _call_function_res_cache[objectId];
}

export function mono_wasm_debugger_log(level: number, message_ptr: CharPtr): void {
    forceThreadMemoryViewRefresh();
    const message = utf8ToString(message_ptr);

    if (INTERNAL["logging"] && typeof INTERNAL.logging["debugger"] === "function") {
        INTERNAL.logging.debugger(level, message);
        return;
    }

    if (BuildConfiguration === "Debug") {
        // eslint-disable-next-line no-console
        console.debug(`Debugger.Debug: ${message}`);
    }
}

type CallDetails = {
    value: string
}

type CallArgs = {
    value: string
}

type CallRequest = {
    arguments: undefined | Array<CallArgs>,
    objectId: string,
    details: CallDetails[],
    functionDeclaration: string
    returnByValue: boolean,
}

type WasmEvent = {
    eventName: string, // - name of the event being raised
    [i: string]: any, // - arguments for the event itself
}

type CommandResponseResult = {
    id: number,
    value: string
}
type CommandResponse = {
    res_ok: boolean,
    res: CommandResponseResult
};

type CFOResponse = {
    type: string,
    subtype?: string,
    value?: string | null,
    className?: string,
    description?: string,
    objectId?: string
}
