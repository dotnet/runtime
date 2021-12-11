import {
    JSHandle, MonoArray, MonoObject, MarshalType,
    MonoString, VoidPtrNull, MonoType, MonoTypeNull
} from "./types";
import { VoidPtr } from "./types/emscripten";
import { mono_wasm_new_root, mono_wasm_new_external_root, WasmRoot } from "./roots";
import { _unbox_mono_obj_root, mono_array_to_js_array } from "./cs-to-js";
import { _js_to_mono_obj } from "./js-to-cs";
import { conv_string, js_string_to_mono_string_new } from "./strings";
import { extract_js_obj_root_with_possible_converter, unbox_struct_at_address } from "./custom-marshaler";
import { mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";
import { MONO, BINDING, INTERNAL } from "./imports";
import {
    getI32,
    getF32,
    getU32,
    getF64,
    setU32,
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
    fn : Function;
    error? : never;
};

type ResolveJSFunctionErrorResult = {
    fn? : never;
    error : InvokeJSResult;
};

type ResolveJSFunctionResult = ResolveJSFunctionSuccessResult | ResolveJSFunctionErrorResult;

const _invoke_js_function_cache = new Map<MonoString, ResolveJSFunctionResult>();

export function _resolve_js_function_by_qualified_name (pInternedFunctionName : MonoString) : ResolveJSFunctionResult {
    let res : ResolveJSFunctionResult | undefined = _invoke_js_function_cache.get(pInternedFunctionName);
    if (res === undefined) {
        const str = conv_string(pInternedFunctionName);
        if (!str)
            res = { error: InvokeJSResult.InvalidFunctionName };
        else
            res = _walk_global_scope_to_find_function(str);
        _invoke_js_function_cache.set(pInternedFunctionName, res);
    }
    return res;
}

function try_get_special_named_object (name : string) {
    switch (name) {
        case "MONO":
            return MONO;
        case "BINDING":
            return BINDING;
        case "INTERNAL":
            return INTERNAL;
        default:
            return undefined;
    }
}

export function _walk_global_scope_to_find_function (str : string) : ResolveJSFunctionResult {
    let scope : any = globalThis;
    let fn = scope[str];

    if (typeof (fn) !== "function") {
        const parts = str.split(".");
        for (let i = 0; i < parts.length; i++) {
            if (!scope)
                return { error: InvokeJSResult.FunctionNotFound };

            const part = parts[i];
            let newScope = undefined;
            if (i === 0)
                newScope = try_get_special_named_object(part);
            if (!newScope)
                newScope = scope[parts[i]];

            scope = newScope;
        }

        fn = scope;
    }

    if (typeof (fn) !== "function")
        return { error : InvokeJSResult.FunctionNotFound };
    else
        return { fn };
}

class UnboxError extends Error {
    errorCode : InvokeJSResult;

    constructor(errorCode : InvokeJSResult) {
        super();
        this.errorCode = errorCode;
    }
}

export function _unbox_function_argument_from_heap_for_invoke (
    index : number, pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr,
    root : WasmRoot<MonoObject> | undefined
) : any {
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
            try {
                if (marshalType === MarshalType.VT)
                    return unbox_struct_at_address(pData, typeHandle);
                else
                    return extract_js_obj_root_with_possible_converter(<WasmRoot<MonoObject>>root, typeHandle, VoidPtrNull);
            } catch (exc : any) {
                console.error(`Uncaught error when unboxing invoke argument #${index} of type ${marshalType} at address ${(<WasmRoot<MonoObject>>root).value}: ${exc}\r\n${exc.stack}`);
                throw new UnboxError(InvokeJSResult.InternalError);
            }
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

function get_object_address_indirect (offset : number) : WasmRoot<MonoObject> {
    const ptrLocation = getU32(offset);
    const result = mono_wasm_new_external_root<MonoObject>(<VoidPtr><any>ptrLocation);
    return result;
}

function _allocate_root_automatically (index : number, pMarshalTypes : VoidPtr, pArguments : VoidPtr) : WasmRoot<MonoObject> | undefined {
    return marshal_type_needs_root.has(getU32(<any>pMarshalTypes + (index * 4)))
        ? get_object_address_indirect(<any>pArguments + (index * 4))
        : undefined;
}

function _handle_thunk_error (exc : any) : InvokeJSResult {
    if (exc instanceof UnboxError)
        return exc.errorCode;
    console.error("invoked function threw unhandled error", exc);
    return InvokeJSResult.FunctionThrewException;
}

function _invoke_thunk_0 (
    fn : Function
) : InvokeJSResult {
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

function _invoke_thunk_1 (
    fn : Function,
    pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr
) : InvokeJSResult {
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

function _invoke_thunk_2 (
    fn : Function,
    pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr
) : InvokeJSResult {
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

function _invoke_thunk_3 (
    fn : Function,
    pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr
) : InvokeJSResult {
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

function _unbox_arguments_and_invoke_js_function (
    fn : Function, argumentCount : number,
    pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr
) : InvokeJSResult {
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

export function _invoke_js_function_by_qualified_name_impl (
    pInternedFunctionName : MonoString, argumentCount : number,
    pMarshalTypes : VoidPtr, pTypeHandles : VoidPtr, pArguments : VoidPtr
) : InvokeJSResult | any {
    const resolved = _resolve_js_function_by_qualified_name(pInternedFunctionName);
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


export function _JSObject_Invoke (js_handle : JSHandle, method_name : string, pRecord : VoidPtr) : void {
    const pArguments = pRecord, pResult = <any>pRecord + 4,
        pErrorMessage = <any>pRecord + 8, pErrorStack = <any>pRecord + 12;

    try {
        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj)
            throw new Error(`Invalid js object handle ${js_handle}`);

        const method = obj[method_name];
        if (typeof (method) !== "function")
            throw new Error(`Member ${method_name} of object was not a function`);

        const args = mono_array_to_js_array(<MonoArray><any>getU32(pArguments));
        const jsResult = method.apply(obj, args);
        const resultPtr = <number><any>_js_to_mono_obj(true, jsResult);
        setU32(pResult, resultPtr);
        setU32(pErrorMessage, 0);
        setU32(pErrorStack, 0);
    } catch (exc : any) {
        setU32(pResult, 0);
        setU32(pErrorMessage, js_string_to_mono_string_new(exc.message));
        setU32(pErrorStack, js_string_to_mono_string_new(exc.stack));
    }
}

export function _JSObject_GetProperty (js_handle : JSHandle, property_name : string, pRecord : VoidPtr) : void {
    const pResult = <any>pRecord, pErrorMessage = <any>pRecord + 4, pErrorStack = <any>pRecord + 8;

    try {
        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj)
            throw new Error(`Invalid js object handle ${js_handle}`);

        const value = obj[property_name];
        const resultPtr = _js_to_mono_obj(true, value);
        setU32(pResult, resultPtr);
        setU32(pErrorMessage, 0);
        setU32(pErrorStack, 0);
    } catch (exc : any) {
        setU32(pResult, 0);
        setU32(pErrorMessage, <number><any>js_string_to_mono_string_new(exc.message));
        setU32(pErrorStack, <number><any>js_string_to_mono_string_new(exc.stack));
    }
}

export function _JSObject_SetProperty (js_handle : JSHandle, property_name : string, pRecord : VoidPtr) : void {
    const pValue = pRecord, pErrorMessage = <any>pRecord + 4, pErrorStack = <any>pRecord + 8,
        pCreate = <any>pRecord + 12;

    const root = mono_wasm_new_root<MonoObject>(<MonoObject><any>getU32(pValue));
    try {
        const value = _unbox_mono_obj_root(root);

        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (!obj)
            throw new Error(`Invalid js object handle ${js_handle}`);

        const createIfNotExist = !!getU32(pCreate);
        if (createIfNotExist || Object.prototype.hasOwnProperty.call(obj, property_name))
            obj[property_name] = value;

        setU32(pErrorMessage, 0);
        setU32(pErrorStack, 0);
    } catch (exc : any) {
        setU32(pErrorMessage, js_string_to_mono_string_new(exc.message));
        setU32(pErrorStack, js_string_to_mono_string_new(exc.stack));
    } finally {
        root.release();
    }
}