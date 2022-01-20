// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    MonoObject, MarshalType,
    MonoString, MonoType, MonoTypeNull
} from "./types";
import { VoidPtr } from "./types/emscripten";
import { mono_wasm_new_external_root, WasmRoot } from "./roots";
import { _unbox_mono_obj_root } from "./cs-to-js";
import { conv_string } from "./strings";
import {
    getI32,
    getF32,
    getU32,
    getF64,
} from "./memory";

// Must match equivalent in Runtime.cs and driver.c
export enum InvokeJSResult {
    Success = 0,
    InvalidFunctionName,
    FunctionNotFound,
    InvalidArgumentCount,
    InvalidArgumentType,
    MissingArgumentType,
    NullArgumentPointer,
    FunctionHadReturnValue,
    FunctionThrewException,
    InternalError,
}

type ResolveJSFunctionSuccessResult = {
    fn: Function;
    error?: never;
};

type ResolveJSFunctionErrorResult = {
    fn?: never;
    error: InvokeJSResult;
};

type ResolveJSFunctionResult = ResolveJSFunctionSuccessResult | ResolveJSFunctionErrorResult;

const _invoke_js_function_cache = new Map<MonoString, ResolveJSFunctionResult>();

export function _resolve_js_function_by_id(function_id: MonoString): ResolveJSFunctionResult {
    let res: ResolveJSFunctionResult | undefined = _invoke_js_function_cache.get(function_id);
    if (res === undefined) {
        const js_function_name = conv_string(function_id);
        if (!js_function_name)
            res = { error: InvokeJSResult.InvalidFunctionName };
        else
            res = _walk_global_scope_to_find_function(js_function_name);
        _invoke_js_function_cache.set(function_id, res);
    }
    return res;
}

export function _walk_global_scope_to_find_function(str: string): ResolveJSFunctionResult {
    let scope: any = globalThis;
    let fn = scope[str];

    if (typeof (fn) !== "function") {
        const parts = str.split(".");
        for (let i = 0; i < parts.length; i++) {
            if (!scope)
                return { error: InvokeJSResult.FunctionNotFound };

            const part = parts[i];
            let newScope = undefined;
            if (!newScope)
                newScope = scope[part];

            scope = newScope;
        }

        fn = scope;
    }

    if (typeof (fn) !== "function")
        return { error: InvokeJSResult.FunctionNotFound };
    else
        return { fn };
}

class UnboxError extends Error {
    errorCode: InvokeJSResult;

    constructor(errorCode: InvokeJSResult) {
        super();
        this.errorCode = errorCode;
    }
}

export function _unbox_function_argument_from_heap_for_invoke(
    index: number, pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr,
    root: WasmRoot<MonoObject> | undefined
): any {
    const typeHandle = <MonoType><any>getU32(<any>pTypeHandles + (index * 4));
    if (typeHandle === MonoTypeNull)
        return undefined;

    const marshalType = getU32(<any>pMarshalTypes + (index * 4));
    const pData = root
        ? <VoidPtr><any>(root.value)
        : <VoidPtr><any>getU32(<any>pArguments + (index * 4));

    // console.log(`index=${index}, marshalType=${marshalType}, typeHandle=${typeHandle}, pData=${pData}`);

    switch (marshalType) {
        case MarshalType.INT:
            return getI32(pData);
        case MarshalType.POINTER:
        case MarshalType.UINT32:
            return getU32(pData);
        case MarshalType.FP32:
            return getF32(pData);
        case MarshalType.FP64:
            return getF64(pData);
        case MarshalType.BOOL:
            return (getI32(pData) !== 0);
        case MarshalType.CHAR:
            return String.fromCharCode(getI32(pData));
        case MarshalType.DELEGATE:
        case MarshalType.TASK:
        case MarshalType.OBJECT:
        case MarshalType.URI:
        case MarshalType.VT:
            return _unbox_mono_obj_root(<WasmRoot<MonoObject>>root);
        case MarshalType.STRING:
        case MarshalType.STRING_INTERNED:
            return conv_string(<MonoString><any>((<WasmRoot<MonoObject>>root).value));
        default:
            console.error(`Unbox/convert not implemented for marshal type ${marshalType}`);
            throw new UnboxError(InvokeJSResult.InvalidArgumentType);
    }
}

const marshal_type_needs_root = new Set<MarshalType>(
    [
        MarshalType.DELEGATE,
        MarshalType.TASK,
        MarshalType.OBJECT,
        MarshalType.URI,
        MarshalType.STRING,
        MarshalType.STRING_INTERNED,
    ]
);

function get_object_address_indirect(offset: number): WasmRoot<MonoObject> {
    const ptrLocation = getU32(offset);
    const result = mono_wasm_new_external_root<MonoObject>(<VoidPtr><any>ptrLocation);
    return result;
}

function _allocate_root_automatically(index: number, pMarshalTypes: VoidPtr, pArguments: VoidPtr): WasmRoot<MonoObject> | undefined {
    return marshal_type_needs_root.has(getU32(<any>pMarshalTypes + (index * 4)))
        ? get_object_address_indirect(<any>pArguments + (index * 4))
        : undefined;
}

function _handle_thunk_error(exc: any): InvokeJSResult {
    if (exc instanceof UnboxError)
        return exc.errorCode;
    console.error("invoked function threw unhandled error", exc);
    return InvokeJSResult.FunctionThrewException;
}

function _invoke_thunk_0(
    fn: Function
): InvokeJSResult {
    try {
        const invokeResult = fn();

        if (typeof (invokeResult) !== "undefined")
            return InvokeJSResult.FunctionHadReturnValue;
        else
            return InvokeJSResult.Success;
    } catch (exc) {
        return _handle_thunk_error(exc);
    }
}

function _invoke_thunk_1(
    fn: Function,
    pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr
): InvokeJSResult {
    const root0 = _allocate_root_automatically(0, pMarshalTypes, pArguments);

    try {
        const arg0 = _unbox_function_argument_from_heap_for_invoke(0, pMarshalTypes, pTypeHandles, pArguments, root0);

        const invokeResult = fn(arg0);

        if (typeof (invokeResult) !== "undefined")
            return InvokeJSResult.FunctionHadReturnValue;
        else
            return InvokeJSResult.Success;
    } catch (exc) {
        return _handle_thunk_error(exc);
    } finally {
        if (root0)
            root0.release();
    }
}

function _invoke_thunk_2(
    fn: Function,
    pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr
): InvokeJSResult {
    const root0 = _allocate_root_automatically(0, pMarshalTypes, pArguments),
        root1 = _allocate_root_automatically(1, pMarshalTypes, pArguments);

    try {
        const arg0 = _unbox_function_argument_from_heap_for_invoke(0, pMarshalTypes, pTypeHandles, pArguments, root0),
            arg1 = _unbox_function_argument_from_heap_for_invoke(1, pMarshalTypes, pTypeHandles, pArguments, root1);

        const invokeResult = fn(arg0, arg1);

        if (typeof (invokeResult) !== "undefined")
            return InvokeJSResult.FunctionHadReturnValue;
        else
            return InvokeJSResult.Success;
    } catch (exc) {
        return _handle_thunk_error(exc);
    } finally {
        if (root0)
            root0.release();
        if (root1)
            root1.release();
    }
}

function _invoke_thunk_3(
    fn: Function,
    pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr
): InvokeJSResult {
    const root0 = _allocate_root_automatically(0, pMarshalTypes, pArguments),
        root1 = _allocate_root_automatically(1, pMarshalTypes, pArguments),
        root2 = _allocate_root_automatically(2, pMarshalTypes, pArguments);

    try {
        const arg0 = _unbox_function_argument_from_heap_for_invoke(0, pMarshalTypes, pTypeHandles, pArguments, root0),
            arg1 = _unbox_function_argument_from_heap_for_invoke(1, pMarshalTypes, pTypeHandles, pArguments, root1),
            arg2 = _unbox_function_argument_from_heap_for_invoke(2, pMarshalTypes, pTypeHandles, pArguments, root2);

        const invokeResult = fn(arg0, arg1, arg2);

        if (typeof (invokeResult) !== "undefined")
            return InvokeJSResult.FunctionHadReturnValue;
        else
            return InvokeJSResult.Success;
    } catch (exc) {
        return _handle_thunk_error(exc);
    } finally {
        if (root0)
            root0.release();
        if (root1)
            root1.release();
        if (root2)
            root2.release();
    }
}

function _unbox_arguments_and_invoke_js_function(
    fn: Function, argumentCount: number,
    pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr
): InvokeJSResult {
    switch (argumentCount) {
        case 0:
            return _invoke_thunk_0(fn);
        case 1:
            return _invoke_thunk_1(fn, pMarshalTypes, pTypeHandles, pArguments);
        case 2:
            return _invoke_thunk_2(fn, pMarshalTypes, pTypeHandles, pArguments);
        case 3:
            return _invoke_thunk_3(fn, pMarshalTypes, pTypeHandles, pArguments);
        default:
            throw new Error(`Unsupported argument count ${argumentCount} (expected [0 - 3])`);
    }
}

export function mono_wasm_invoke_js_function_impl(
    pInternedFunctionName: MonoString, argumentCount: number,
    pMarshalTypes: VoidPtr, pTypeHandles: VoidPtr, pArguments: VoidPtr
): InvokeJSResult | any {
    const resolved = _resolve_js_function_by_id(pInternedFunctionName);
    if (resolved.error)
        return resolved.error;

    const result = _unbox_arguments_and_invoke_js_function(
        <Function>resolved.fn, argumentCount,
        pMarshalTypes, pTypeHandles, pArguments
    );

    if (typeof (result) !== "number")
        return InvokeJSResult.InternalError;
    else
        return result;
}