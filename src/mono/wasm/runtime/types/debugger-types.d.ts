// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************************************
 This file just acts as a set of object definitions to help the TSC compiler understand 
 the various namespaces that we use. These namespaces are not defined explicitly until
 the dotnet.js file is merged, so we pretend they exist by defining them here.

 THIS FILE IS NOT INCLUDED IN DOTNET.JS. ALL CODE HERE WILL BE IGNORED DURING THE BUILD
********************************************************************************************/

// VARIOUS C FUNCTIONS THAT WE CALL INTO ////////////////////////////////////////////////////
interface DEBUG_C_FUNCS {
    mono_wasm_send_dbg_command (a: number, b:number, c: number, d: number, e: number): boolean;
    mono_wasm_send_dbg_command_with_parms (a: number, b:number, c: number, d: number, e: number, f: number, g: string): boolean;
    mono_wasm_set_is_debugger_attached (a: boolean): void;
}

interface DEBUG_VARS {
    _c_fn_table: {
        [fn: string]: Function
    };
    commands_received:{
        res_ok: number,
        res: CommandResult
    };
    _call_function_res_cache: object;
    _next_call_function_res_id: number;
    _next_id_var: number;
    var_info: []; // always an empty list - Can it be removed?
}

// NAMESPACES ///////////////////////////////////////////////////////////////////////////////
// Added in mono-types.d.ts

// OTHER TYPES ///////////////////////////////////////////////////////////////////////

type Proxy = {
    name: string,
    get: () => ((id: number, command_set: number, command: number, command_parameters: string) => CommandResult),
    set: (newValue: number) => ((id: number, command_set: number, command: number, command_parameters: string, length: number, valtype: number, newvalue: string) => CommandResult)
    [i: string]: any
}

type CallRequest = {
    arguments: undefined | Array<CallArgs>,
    objectId: string,
    details: CallDetails[],
    functionDeclaration: string
    returnByValue: boolean,
}

type CallDetails = {
    value: string
};

type CallArgs = {
    value: string
};

type CallResult = {
    type: string,
    subtype?: string,
    className?: string,
    description?: string,
    objectId?: string,
    value?: any
}

type ChromeDevToolsArgs = {
    ownProperties?: boolean,
    objectId?: string,
    generatePreview?: boolean,
    accessorPropertiesOnly?: boolean
}

type CommandResult = {
    id: number,
    value: string
};
