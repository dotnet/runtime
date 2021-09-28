// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, MONO } from '../runtime'
import { toBase64StringImpl, _base64_to_uint8 } from './base64'
import cwraps from './cwraps'
import { mono_wasm_load_bytes_into_heap } from './init'

var commands_received: any;
var _call_function_res_cache: any = {}
var _next_call_function_res_id = 0;

export function mono_wasm_runtime_ready() {
    MONO.mono_wasm_runtime_is_ready = true;

    // FIXME: where should this go?
    _next_call_function_res_id = 0;
    _call_function_res_cache = {};

    // DO NOT REMOVE - magic debugger init function
    if ((<any>globalThis).dotnetDebugger)
        debugger;
    else
        console.debug("mono_wasm_runtime_ready", "fe00e07a-5519-4dfe-b35a-f867dbaf2e28");
}

export function mono_wasm_fire_debugger_agent_message() {
    // eslint-disable-next-line no-debugger
    debugger;
}

export function mono_wasm_add_dbg_command_received(res_ok: boolean, id: number, buffer: number, buffer_len: number) {
    const assembly_data = new Uint8Array(Module.HEAPU8.buffer, buffer, buffer_len);
    const base64String = toBase64StringImpl(assembly_data);
    const buffer_obj = {
        res_ok,
        res: {
            id,
            value: base64String
        }
    }
    commands_received = buffer_obj;
}

export function mono_wasm_send_dbg_command_with_parms(id: number, command_set: number, command: number, command_parameters: any, length: number, valtype: number, newvalue: number) {
    const dataHeap = mono_wasm_load_bytes_into_heap(_base64_to_uint8(command_parameters));
    cwraps.mono_wasm_send_dbg_command_with_parms(id, command_set, command, dataHeap, length, valtype, newvalue.toString());

    const { res_ok, res } = commands_received;
    if (!res_ok)
        throw new Error(`Failed on mono_wasm_invoke_method_debugger_agent_with_parms`);
    return res;
}

export function mono_wasm_send_dbg_command(id: number, command_set: number, command: number, command_parameters: any) {
    var dataHeap = mono_wasm_load_bytes_into_heap(_base64_to_uint8(command_parameters));
    cwraps.mono_wasm_send_dbg_command(id, command_set, command, dataHeap, command_parameters.length);

    const { res_ok, res } = commands_received;
    if (!res_ok)
        throw new Error(`Failed on mono_wasm_send_dbg_command`);
    return res;

}

export function mono_wasm_get_dbg_command_info() {
    const { res_ok, res } = commands_received;
    if (!res_ok)
        throw new Error(`Failed on mono_wasm_get_dbg_command_info`);
    return res;
}

export function mono_wasm_debugger_resume() {
}

export function mono_wasm_detach_debugger() {
    cwraps.mono_wasm_set_is_debugger_attached(false);
}

/**
 * Raises an event for the debug proxy
 */
export function mono_wasm_raise_debug_event(event: WasmEvent, args = {}) {
    if (typeof event !== 'object')
        throw new Error(`event must be an object, but got ${JSON.stringify(event)}`);

    if (event.eventName === undefined)
        throw new Error(`event.eventName is a required parameter, in event: ${JSON.stringify(event)}`);

    if (typeof args !== 'object')
        throw new Error(`args must be an object, but got ${JSON.stringify(args)}`);

    console.debug('mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae', JSON.stringify(event), JSON.stringify(args));
}

// Used by the debugger to enumerate loaded dlls and pdbs
export function mono_wasm_get_loaded_files() {
    cwraps.mono_wasm_set_is_debugger_attached(true);
    return MONO.loaded_files;
}

function _create_proxy_from_object_id(objectId: string, details: any) {
    if (objectId.startsWith('dotnet:array:')) {
        const ret = details.map((p: any) => p.value);
        return ret;
    }

    const proxy: any = {};
    Object.keys(details).forEach(p => {
        var prop = details[p];
        if (prop.get !== undefined) {
            Object.defineProperty(proxy,
                prop.name,
                {
                    get() {
                        return mono_wasm_send_dbg_command(-1, prop.get.commandSet, prop.get.command, prop.get.buffer);
                    },
                    set: function (newValue) {
                        mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue); return commands_received.res_ok;
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
                        mono_wasm_send_dbg_command_with_parms(-1, prop.set.commandSet, prop.set.command, prop.set.buffer, prop.set.length, prop.set.valtype, newValue); return commands_received.res_ok;
                    }
                }
            );
        } else {
            proxy[prop.name] = prop.value;
        }
    });
    return proxy;
}

export function mono_wasm_call_function_on(request: CallRequest) {
    if (request.arguments != undefined && !Array.isArray(request.arguments))
        throw new Error(`"arguments" should be an array, but was ${request.arguments}`);

    const objId = request.objectId;
    const details = request.details;
    let proxy;

    if (objId.startsWith('dotnet:cfo_res:')) {
        if (objId in _call_function_res_cache)
            proxy = _call_function_res_cache[objId];
        else
            throw new Error(`Unknown object id ${objId}`);
    } else {
        proxy = _create_proxy_from_object_id(objId, details);
    }

    const fn_args = request.arguments != undefined ? request.arguments.map(a => JSON.stringify(a.value)) : [];
    const fn_eval_str = `var fn = ${request.functionDeclaration}; fn.call (proxy, ...[${fn_args}]);`;

    const local_eval = eval; // https://rollupjs.org/guide/en/#avoiding-eval
    const fn_res = local_eval(fn_eval_str);
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


function _get_cfo_res_details(objectId: string, args: any) {
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

    let res_details: any[] = [];
    Object.keys(descriptors).forEach(k => {
        let new_obj;
        let prop_desc = descriptors[k];
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
                value: Object.assign({ type: (typeof prop_desc.value), description: '' + prop_desc.value },
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

export function mono_wasm_get_details(objectId: string, args = {}) {
    return _get_cfo_res_details(`dotnet:cfo_res:${objectId}`, args);
}

function _cache_call_function_res(obj: any) {
    const id = `dotnet:cfo_res:${_next_call_function_res_id++}`;
    _call_function_res_cache[id] = obj;
    return id;
}

export function mono_wasm_release_object(objectId: string) {
    if (objectId in _call_function_res_cache)
        delete _call_function_res_cache[objectId];
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
