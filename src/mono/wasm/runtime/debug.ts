// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { INTERNAL, Module, MONO, runtimeHelpers } from "./imports";
import { toBase64StringImpl } from "./base64";
import cwraps from "./cwraps";
import { VoidPtr, CharPtr } from "./types/emscripten";

const commands_received : any = new Map<number, CommandResponse>();
const wasm_func_map = new Map<number, string>();
commands_received.remove = function (key: number) : CommandResponse { const value = this.get(key); this.delete(key); return value;};
let _call_function_res_cache: any = {};
let _next_call_function_res_id = 0;
let _debugger_buffer_len = -1;
let _debugger_buffer: VoidPtr;

const regexes:any[] = [];

// V8
//   at <anonymous>:wasm-function[1900]:0x83f63
//   at dlfree (<anonymous>:wasm-function[18739]:0x2328ef)
regexes.push(/at (?<replaceSection>[^:()]+:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)((?![^)a-fA-F\d])|$)/);

//# 5: WASM [009712b2], function #111 (''), pc=0x7c16595c973 (+0x53), pos=38740 (+11)
regexes.push(/(?:WASM \[[\da-zA-Z]+\], (?<replaceSection>function #(?<funcNum>[\d]+) \(''\)))/);

//# chrome
//# at http://127.0.0.1:63817/dotnet.wasm:wasm-function[8963]:0x1e23f4
regexes.push(/(?<replaceSection>[a-z]+:\/\/[^ )]*:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)/);

//# <?>.wasm-function[8962]
regexes.push(/(?<replaceSection><[^ >]+>[.:]wasm-function\[(?<funcNum>[0-9]+)\])/);

export function mono_wasm_runtime_ready(): void {
    runtimeHelpers.mono_wasm_runtime_is_ready = true;

    // FIXME: where should this go?
    _next_call_function_res_id = 0;
    _call_function_res_cache = {};
    _debugger_buffer_len = -1;

    // DO NOT REMOVE - magic debugger init function
    if ((<any>globalThis).dotnetDebugger)
        // eslint-disable-next-line no-debugger
        debugger;
    else
        console.debug("mono_wasm_runtime_ready", "fe00e07a-5519-4dfe-b35a-f867dbaf2e28");

    _readSymbolMapFile("dotnet.js.symbols");
}

export function mono_wasm_fire_debugger_agent_message(): void {
    // eslint-disable-next-line no-debugger
    debugger;
}

export function mono_wasm_add_dbg_command_received(res_ok: boolean, id: number, buffer: number, buffer_len: number): void {
    const assembly_data = new Uint8Array(Module.HEAPU8.buffer, buffer, buffer_len);
    const base64String = toBase64StringImpl(assembly_data);
    const buffer_obj = {
        res_ok,
        res: {
            id,
            value: base64String
        }
    };
    if (commands_received.has(id))
        console.warn("Addind an id that already exists in commands_received");
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
    for (let i = 0; i < byteCharacters.length; i++) {
        Module.HEAPU8[<any>_debugger_buffer + i] = byteCharacters.charCodeAt(i);
    }
}

export function mono_wasm_send_dbg_command_with_parms(id: number, command_set: number, command: number, command_parameters: string, length: number, valtype: number, newvalue: number): CommandResponseResult {
    mono_wasm_malloc_and_set_debug_buffer(command_parameters);
    cwraps.mono_wasm_send_dbg_command_with_parms(id, command_set, command, _debugger_buffer, length, valtype, newvalue.toString());

    const { res_ok, res } = commands_received.remove(id);
    if (!res_ok)
        throw new Error("Failed on mono_wasm_invoke_method_debugger_agent_with_parms");
    return res;
}

export function mono_wasm_send_dbg_command(id: number, command_set: number, command: number, command_parameters: string): CommandResponseResult {
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
    //nothing
}

export function mono_wasm_detach_debugger(): void {
    cwraps.mono_wasm_set_is_debugger_attached(false);
}

export function mono_wasm_change_debugger_log_level(level: number): void {
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

    console.debug("mono_wasm_debug_event_raised:aef14bca-5519-4dfe-b35a-f867abc123ae", JSON.stringify(event), JSON.stringify(args));
}

// Used by the debugger to enumerate loaded dlls and pdbs
export function mono_wasm_get_loaded_files(): string[] {
    cwraps.mono_wasm_set_is_debugger_attached(true);
    return MONO.loaded_files;
}

function _create_proxy_from_object_id(objectId: string, details: any) {
    if (objectId.startsWith("dotnet:array:")) {
        let ret: Array<any>;
        if (details.items === undefined) {
            ret = details.map ((p: any) => p.value);
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
    const message = Module.UTF8ToString(message_ptr);

    if (INTERNAL["logging"] && typeof INTERNAL.logging["debugger"] === "function") {
        INTERNAL.logging.debugger(level, message);
        return;
    }

    console.debug(`Debugger.Debug: ${message}`);
}

function _readSymbolMapFile(filename: string): void {
    try {
        const res = Module.FS_readFile(filename, {flags: "r", encoding: "utf8"});
        res.split(/[\r\n]/).forEach((line: string) => {
            const parts:string[] = line.split(/:/);
            if (parts.length < 2)
                return;

            parts[1] = parts.splice(1).join(":");
            wasm_func_map.set(Number(parts[0]), parts[1]);
        });

        console.debug(`Loaded ${wasm_func_map.size} symbols`);
    } catch (error:any) {
        if (error.errno == 44) // NOENT
            console.debug(`Could not find symbols file ${filename}. Ignoring.`);
        else
            console.log(`Error loading symbol file ${filename}: ${JSON.stringify(error)}`);
        return;
    }
}

export function mono_wasm_symbolicate_string(message: string): string {
    try {
        if (wasm_func_map.size == 0)
            return message;

        const origMessage = message;

        for (let i = 0; i < regexes.length; i ++)
        {
            const newRaw = message.replace(new RegExp(regexes[i], "g"), (substring, ...args) => {
                const groups = args.find(arg => {
                    return typeof(arg) == "object" && arg.replaceSection !== undefined;
                });

                if (groups === undefined)
                    return substring;

                const funcNum = groups.funcNum;
                const replaceSection = groups.replaceSection;
                const name = wasm_func_map.get(Number(funcNum));

                if (name === undefined)
                    return substring;

                return substring.replace(replaceSection, `${name} (${replaceSection})`);
            });

            if (newRaw !== origMessage)
                return newRaw;
        }

        return origMessage;
    } catch (error) {
        console.debug(`failed to symbolicate: ${error}`);
        return message;
    }
}

export function mono_wasm_stringify_as_error_with_stack(err: Error | string): string {
    let errObj: any = err;
    if (!(err instanceof Error))
        errObj = new Error(err);

    // Error
    return mono_wasm_symbolicate_string(errObj.stack);
}

export function mono_wasm_trace_logger(log_domain_ptr: CharPtr, log_level_ptr: CharPtr, message_ptr: CharPtr, fatal: number, user_data: VoidPtr): void {
    const origMessage = Module.UTF8ToString(message_ptr);
    const isFatal = !!fatal;
    const domain = Module.UTF8ToString(log_domain_ptr);
    const dataPtr = user_data;
    const log_level = Module.UTF8ToString(log_level_ptr);

    const message = `[MONO] ${origMessage}`;

    if (INTERNAL["logging"] && typeof INTERNAL.logging["trace"] === "function") {
        INTERNAL.logging.trace(domain, log_level, message, isFatal, dataPtr);
        return;
    }

    switch (log_level) {
        case "critical":
        case "error":
            console.error(mono_wasm_stringify_as_error_with_stack(message));
            break;
        case "warning":
            console.warn(message);
            break;
        case "message":
            console.log(message);
            break;
        case "info":
            console.info(message);
            break;
        case "debug":
            console.debug(message);
            break;
        default:
            console.log(message);
            break;
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
