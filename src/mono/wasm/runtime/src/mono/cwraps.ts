// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from '../runtime'

const fn_signatures: [ident: string, returnType: string | null, argTypes?: string[], opts?: any][] = [
    ["mono_wasm_register_root", "number", ["number", "number", "string"]],
    ["mono_wasm_deregister_root", null, ["number"]],
    ["mono_wasm_string_get_data", null, ['number', 'number', 'number', 'number']],
    ['mono_wasm_set_is_debugger_attached', 'void', ['bool']],
    ['mono_wasm_send_dbg_command', 'bool', ['number', 'number', 'number', 'number', 'number']],
    ['mono_wasm_send_dbg_command_with_parms', 'bool', ['number', 'number', 'number', 'number', 'number', 'number', 'string']],
    ['mono_wasm_setenv', null, ['string', 'string']],
    ['mono_wasm_parse_runtime_options', null, ['number', 'number']],
    ['mono_wasm_strdup', 'number', ['string']],
    ['mono_background_exec', null, []],
    ["mono_set_timeout_exec", null, ['number']],
    ['mono_wasm_load_icu_data', 'number', ['number']],
    ['mono_wasm_get_icudt_name', 'string', ['string']],
    ['mono_wasm_add_assembly', 'number', ['string', 'number', 'number']],
    ['mono_wasm_add_satellite_assembly', 'void', ['string', 'string', 'number', 'number']],
    ['mono_wasm_load_runtime', null, ['string', 'number']],
    ['mono_wasm_exit', null, ['number']],
]

export interface t_Cwraps {
    mono_wasm_register_root(start: CharPtr, size: number, name: CharPtr): number;
    mono_wasm_deregister_root(addr: CharPtr): void;
    mono_wasm_string_get_data(string: MonoString, outChars: CharPtrPtr, outLengthBytes: Int32Ptr, outIsInterned: Int32Ptr): void;
    mono_wasm_set_is_debugger_attached(value: boolean): void;
    mono_wasm_send_dbg_command(id: number, command_set: number, command: number, data: VoidPtr, size: number): boolean;
    mono_wasm_send_dbg_command_with_parms(id: number, command_set: number, command: number, data: VoidPtr, size: number, valtype: number, newvalue: CharPtr): boolean;
    mono_wasm_setenv(name: string, value: string): void;
    mono_wasm_strdup(value: string): number;
    mono_wasm_parse_runtime_options(length: number, argv: VoidPtr): void;
    mono_background_exec(): void;
    mono_set_timeout_exec(id: number): void;
    mono_wasm_load_icu_data(offset: VoidPtr): number;
    mono_wasm_get_icudt_name(name: string): string;
    mono_wasm_add_assembly(name: CharPtr, data: VoidPtr, size: number): number;
    mono_wasm_add_satellite_assembly(name: CharPtr, culture: CharPtr, data: VoidPtr, size: number): void;
    mono_wasm_load_runtime(unused: CharPtr, debug_level: number): void;
    mono_wasm_exit(exit_code: number): number;
}

const wrapped_c_functions: t_Cwraps = <any>{}
for (let sig of fn_signatures) {
    const wf: any = wrapped_c_functions;
    // lazy init on first run
    wf[sig[0]] = function () {
        const fce = Module.cwrap(sig[0], sig[1], sig[2], sig[3])
        wf[sig[0]] = fce;
        return fce.apply(undefined, arguments);
    };
}

export default wrapped_c_functions;