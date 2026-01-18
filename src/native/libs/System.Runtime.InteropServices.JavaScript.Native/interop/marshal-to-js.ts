// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { dotnetBrowserUtilsExports, dotnetLoaderExports, dotnetApi, dotnetAssert, dotnetLogger, Module } from "./cross-module";

import type { BoundMarshalerToJs, JSHandle, JSMarshalerArgument, JSMarshalerArguments, JSMarshalerType, MarshalerToCs, MarshalerToJs, TypedArray } from "./types";
import { GCHandleNull, JavaScriptMarshalerArgSize, MarshalerType } from "./types";
import {
    arrayElementSize, csToJsMarshalers, jsinteropDoc, getMarshalerToCsByType,
    getArg, getArgBool, getArgDate, getArgElementType, getArgF32, getArgF64, getArgGcHandle, getArgI16, getArgI32, getArgI52, getArgI64Big, getArgIntptr,
    getArgJsHandle, getArgLength, getArgType, getArgU16, getArgU8,
    getMarshalerToJsByType, getSignatureArg1Type, getSignatureArg2Type, getSignatureArg3Type, getSignatureResType,
    setArgType, setJsHandle,
} from "./marshal";
import { marshalExceptionToCs } from "./marshal-to-cs";
import { lookupJsOwnedObject, getJsHandleFromJSObject, getJSObjectFromJSHandle, registerWithJsvHandle, releaseCSOwnedObject, setupManagedProxy, teardownManagedProxy, proxyDebugSymbol } from "./gc-handles";
import { assertRuntimeRunning, fixupPointer, isRuntimeRunning } from "./utils";
import { ArraySegment, ManagedError, ManagedObject, MemoryViewType, Span } from "./marshaled-types";
import { callDelegate } from "./managed-exports";

export function initializeMarshalersToJs(): void {
    if (csToJsMarshalers.size == 0) {
        csToJsMarshalers.set(MarshalerType.Array, _marshalArrayToJs);
        csToJsMarshalers.set(MarshalerType.Span, _marshalSpanToJs);
        csToJsMarshalers.set(MarshalerType.ArraySegment, _marshalArraySegmentToJs);
        csToJsMarshalers.set(MarshalerType.Boolean, _marshalBoolToJs);
        csToJsMarshalers.set(MarshalerType.Byte, _marshalByteToJs);
        csToJsMarshalers.set(MarshalerType.Char, _marshalCharToJs);
        csToJsMarshalers.set(MarshalerType.Int16, _marshalInt16ToJs);
        csToJsMarshalers.set(MarshalerType.Int32, marshalInt32ToJs);
        csToJsMarshalers.set(MarshalerType.Int52, _marshalInt52ToJs);
        csToJsMarshalers.set(MarshalerType.BigInt64, _marshalBigint64ToJs);
        csToJsMarshalers.set(MarshalerType.Single, _marshalFloatToJs);
        csToJsMarshalers.set(MarshalerType.IntPtr, _marshalIntptrToJs);
        csToJsMarshalers.set(MarshalerType.Double, _marshalDoubleToJs);
        csToJsMarshalers.set(MarshalerType.String, marshalStringToJs);
        csToJsMarshalers.set(MarshalerType.Exception, marshalExceptionToJs);
        csToJsMarshalers.set(MarshalerType.JSException, marshalExceptionToJs);
        csToJsMarshalers.set(MarshalerType.JSObject, _marshalJsObjectToJs);
        csToJsMarshalers.set(MarshalerType.Object, _marshalCsObjectToJs);
        csToJsMarshalers.set(MarshalerType.DateTime, _marshalDatetimeToJs);
        csToJsMarshalers.set(MarshalerType.DateTimeOffset, _marshalDatetimeToJs);
        csToJsMarshalers.set(MarshalerType.Task, marshalTaskToJs);
        csToJsMarshalers.set(MarshalerType.TaskRejected, marshalTaskToJs);
        csToJsMarshalers.set(MarshalerType.TaskResolved, marshalTaskToJs);
        csToJsMarshalers.set(MarshalerType.TaskPreCreated, beginMarshalTaskToJs);
        csToJsMarshalers.set(MarshalerType.Action, _marshalDelegateToJs);
        csToJsMarshalers.set(MarshalerType.Function, _marshalDelegateToJs);
        csToJsMarshalers.set(MarshalerType.None, _marshalNullToJs);
        csToJsMarshalers.set(MarshalerType.Void, _marshalNullToJs);
        csToJsMarshalers.set(MarshalerType.Discard, _marshalNullToJs);
        csToJsMarshalers.set(MarshalerType.DiscardNoWait, _marshalNullToJs);
    }
}

export function bindArgMarshalToJs(sig: JSMarshalerType, marshalerType: MarshalerType, index: number): BoundMarshalerToJs | undefined {
    if (marshalerType === MarshalerType.None || marshalerType === MarshalerType.Void || marshalerType === MarshalerType.Discard || marshalerType === MarshalerType.DiscardNoWait) {
        return undefined;
    }

    let resMarshaler: MarshalerToJs | undefined = undefined;
    let arg1Marshaler: MarshalerToCs | undefined = undefined;
    let arg2Marshaler: MarshalerToCs | undefined = undefined;
    let arg3Marshaler: MarshalerToCs | undefined = undefined;

    arg1Marshaler = getMarshalerToCsByType(getSignatureArg1Type(sig));
    arg2Marshaler = getMarshalerToCsByType(getSignatureArg2Type(sig));
    arg3Marshaler = getMarshalerToCsByType(getSignatureArg3Type(sig));
    const marshalerTypeRes = getSignatureResType(sig);
    resMarshaler = getMarshalerToJsByType(marshalerTypeRes);
    if (marshalerType === MarshalerType.Nullable) {
        // nullable has nested type information, it's stored in res slot of the signature. The marshaler is the same as for non-nullable primitive type.
        marshalerType = marshalerTypeRes;
    }
    const converter = getMarshalerToJsByType(marshalerType)!;
    const elementType = getSignatureArg1Type(sig);

    const argOffset = index * JavaScriptMarshalerArgSize;
    return (args: JSMarshalerArguments) => {
        return converter(<any>args + argOffset, elementType, resMarshaler, arg1Marshaler, arg2Marshaler, arg3Marshaler);
    };
}

function _marshalBoolToJs(arg: JSMarshalerArgument): boolean | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgBool(arg);
}

function _marshalByteToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgU8(arg);
}

function _marshalCharToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgU16(arg);
}

function _marshalInt16ToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgI16(arg);
}

export function marshalInt32ToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgI32(arg);
}

function _marshalInt52ToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgI52(arg);
}

function _marshalBigint64ToJs(arg: JSMarshalerArgument): bigint | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgI64Big(arg);
}

function _marshalFloatToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgF32(arg);
}

function _marshalDoubleToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgF64(arg);
}

function _marshalIntptrToJs(arg: JSMarshalerArgument): number | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return getArgIntptr(arg);
}

function _marshalNullToJs(): null {
    return null;
}

function _marshalDatetimeToJs(arg: JSMarshalerArgument): Date | null {
    const type = getArgType(arg);
    if (type === MarshalerType.None) {
        return null;
    }
    return getArgDate(arg);
}

// NOTE: at the moment, this can't dispatch async calls (with Task/Promise return type). Therefore we don't have to worry about pre-created Task.
function _marshalDelegateToJs(arg: JSMarshalerArgument, _?: MarshalerType, resConverter?: MarshalerToJs, arg1Converter?: MarshalerToCs, arg2Converter?: MarshalerToCs, arg3Converter?: MarshalerToCs): Function | null {
    const type = getArgType(arg);
    if (type === MarshalerType.None) {
        return null;
    }

    const gcHandle = getArgGcHandle(arg);
    let result = lookupJsOwnedObject(gcHandle);
    if (result === null || result === undefined) {
        // this will create new Function for the C# delegate
        result = (arg1Js: any, arg2Js: any, arg3Js: any): any => {
            dotnetAssert.check(!result.isDisposed, "Delegate is disposed and should not be invoked anymore.");
            // arg numbers are shifted by one, the real first is a gc handle of the callback
            return callDelegate(gcHandle, arg1Js, arg2Js, arg3Js, resConverter, arg1Converter, arg2Converter, arg3Converter);
        };
        result.dispose = () => {
            if (!result.isDisposed) {
                result.isDisposed = true;
                teardownManagedProxy(result, gcHandle);
            }
        };
        result.isDisposed = false;
        if (BuildConfiguration === "Debug") {
            (result as any)[proxyDebugSymbol] = `C# Delegate with GCHandle ${gcHandle}`;
        }
        setupManagedProxy(result, gcHandle);
    }

    return result;
}

export class TaskHolder {
    constructor(public promise: Promise<any>, public resolveOrReject: (type: MarshalerType, jsHandle: JSHandle, argInner: JSMarshalerArgument) => void) {
    }
}

export function marshalTaskToJs(arg: JSMarshalerArgument, _?: MarshalerType, resConverter?: MarshalerToJs): Promise<any> | null {
    const type = getArgType(arg);
    // this path is used only when Task is passed as argument to JSImport and virtual JSHandle would be used
    dotnetAssert.check(type != MarshalerType.TaskPreCreated, "Unexpected Task type: TaskPreCreated");

    // if there is synchronous result, return it
    const promise = tryMarshalSyncTaskToJs(arg, type, resConverter);
    if (promise !== false) {
        return promise;
    }

    const jsvHandle = getArgJsHandle(arg);
    const holder = createTaskHolder(resConverter);
    registerWithJsvHandle(holder, jsvHandle);
    if (BuildConfiguration === "Debug") {
        (holder as any)[proxyDebugSymbol] = `TaskHolder with JSVHandle ${jsvHandle}`;
    }

    return holder.promise;
}

export function beginMarshalTaskToJs(arg: JSMarshalerArgument, _?: MarshalerType, resConverter?: MarshalerToJs): Promise<any> | null {
    // this path is used when Task is returned from JSExport/call_entry_point
    const holder = createTaskHolder(resConverter);
    const jsHandle = getJsHandleFromJSObject(holder);
    if (BuildConfiguration === "Debug") {
        (holder as any)[proxyDebugSymbol] = `TaskHolder with JSHandle ${jsHandle}`;
    }
    setJsHandle(arg, jsHandle);
    setArgType(arg, MarshalerType.TaskPreCreated);
    return holder.promise;
}

export function endMarshalTaskToJs(args: JSMarshalerArguments, resConverter: MarshalerToJs | undefined, eagerPromise: Promise<any> | null) {
    // this path is used when Task is returned from JSExport/call_entry_point
    const res = getArg(args, 1);
    const type = getArgType(res);

    // if there is no synchronous result, return eagerPromise we created earlier
    if (type === MarshalerType.TaskPreCreated) {
        return eagerPromise;
    }

    // otherwise drop the eagerPromise's handle
    const jsHandle = getJsHandleFromJSObject(eagerPromise);
    releaseCSOwnedObject(jsHandle);

    // get the synchronous result
    const promise = tryMarshalSyncTaskToJs(res, type, resConverter);

    // make sure we got the result
    dotnetAssert.fastCheck(promise !== false, () => `Expected synchronous result, got: ${type}`);

    return promise;
}

function tryMarshalSyncTaskToJs(arg: JSMarshalerArgument, type: MarshalerType, resConverter?: MarshalerToJs): Promise<any> | null | false {
    if (type === MarshalerType.None) {
        return null;
    }
    if (type === MarshalerType.TaskRejected) {
        return Promise.reject(marshalExceptionToJs(arg));
    }
    if (type === MarshalerType.TaskResolved) {
        const elementType = getArgElementType(arg);
        if (elementType === MarshalerType.Void) {
            return Promise.resolve();
        }
        // this will change the type to the actual type of the result
        setArgType(arg, elementType);
        if (!resConverter) {
            // when we arrived here from _marshalCsObjectToJs
            resConverter = csToJsMarshalers.get(elementType);
        }
        dotnetAssert.fastCheck(resConverter, () => `Unknown subConverter for type ${elementType}. ${jsinteropDoc}`);

        const val = resConverter(arg);
        return Promise.resolve(val);
    }
    return false;
}

function createTaskHolder(resConverter?: MarshalerToJs) {
    const pcs = dotnetLoaderExports.createPromiseCompletionSource<any>();
    const holder = new TaskHolder(pcs.promise, (type, jsHandle, argInner) => {
        if (type === MarshalerType.TaskRejected) {
            const reason = marshalExceptionToJs(argInner);
            pcs.reject(reason);
        } else if (type === MarshalerType.TaskResolved) {
            const type = getArgType(argInner);
            if (type === MarshalerType.Void) {
                pcs.resolve(undefined);
            } else {
                if (!resConverter) {
                    // when we arrived here from _marshalCsObjectToJs
                    resConverter = csToJsMarshalers.get(type);
                }
                dotnetAssert.fastCheck(resConverter, () => `Unknown subConverter for type ${type}. ${jsinteropDoc}`);

                const jsValue = resConverter!(argInner);
                pcs.resolve(jsValue);
            }
        } else {
            dotnetAssert.fastCheck(false, () => `Unexpected type ${type}`);
        }
        releaseCSOwnedObject(jsHandle);
    });
    return holder;
}

export function marshalStringToJs(arg: JSMarshalerArgument): string | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    const buffer = getArgIntptr(arg);
    const len = getArgLength(arg) * 2;
    const value = dotnetBrowserUtilsExports.utf16ToString(<any>buffer, <any>buffer + len);
    Module._free(buffer as any);
    return value;
}

export function marshalExceptionToJs(arg: JSMarshalerArgument): Error | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    if (type == MarshalerType.JSException) {
        // this is JSException roundtrip
        const jsHandle = getArgJsHandle(arg);
        const jsObj = getJSObjectFromJSHandle(jsHandle);
        return jsObj;
    }

    const gcHandle = getArgGcHandle(arg);
    let result = lookupJsOwnedObject(gcHandle);
    if (result === null || result === undefined) {
        // this will create new ManagedError
        const message = marshalStringToJs(arg);
        result = new ManagedError(message!);

        if (BuildConfiguration === "Debug") {
            (result as any)[proxyDebugSymbol] = `C# Exception with GCHandle ${gcHandle}`;
        }
        setupManagedProxy(result, gcHandle);
    }

    return result;
}

function _marshalJsObjectToJs(arg: JSMarshalerArgument): any {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    const jsHandle = getArgJsHandle(arg);
    const jsObj = getJSObjectFromJSHandle(jsHandle);
    dotnetAssert.fastCheck(jsObj !== undefined, () => `JS object JSHandle ${jsHandle} was not found`);
    return jsObj;
}

function _marshalCsObjectToJs(arg: JSMarshalerArgument): any {
    const marshalerType = getArgType(arg);
    if (marshalerType == MarshalerType.None) {
        return null;
    }
    if (marshalerType == MarshalerType.JSObject) {
        const jsHandle = getArgJsHandle(arg);
        const jsObj = getJSObjectFromJSHandle(jsHandle);
        return jsObj;
    }

    if (marshalerType == MarshalerType.Array) {
        const elementType = getArgElementType(arg);
        return _marshalArrayToJs_impl(arg, elementType);
    }

    if (marshalerType == MarshalerType.Object) {
        const gcHandle = getArgGcHandle(arg);
        if (gcHandle === GCHandleNull) {
            return null;
        }

        // see if we have js owned instance for this gcHandle already
        let result = lookupJsOwnedObject(gcHandle);

        // If the JS object for this gcHandle was already collected (or was never created)
        if (!result) {
            result = new ManagedObject();
            if (BuildConfiguration === "Debug") {
                (result as any)[proxyDebugSymbol] = `C# Object with GCHandle ${gcHandle}`;
            }
            setupManagedProxy(result, gcHandle);
        }

        return result;
    }

    // other types
    const converter = csToJsMarshalers.get(marshalerType);
    dotnetAssert.fastCheck(converter, () => `Unknown converter for type ${marshalerType}. ${jsinteropDoc}`);
    return converter(arg);
}

function _marshalArrayToJs(arg: JSMarshalerArgument, elementType?: MarshalerType): Array<any> | TypedArray | null {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");
    return _marshalArrayToJs_impl(arg, elementType);
}

function _marshalArrayToJs_impl(arg: JSMarshalerArgument, elementType: MarshalerType): Array<any> | TypedArray | null {
    const type = getArgType(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    const elementSize = arrayElementSize(elementType);
    dotnetAssert.fastCheck(elementSize != -1, () => `Element type ${elementType} not supported`);
    const bufferPtr = getArgIntptr(arg);
    const length = getArgLength(arg);
    let result: Array<any> | TypedArray | null = null;
    if (elementType == MarshalerType.String) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const elementArg = getArg(<any>bufferPtr, index);
            result[index] = marshalStringToJs(elementArg);
        }
    } else if (elementType == MarshalerType.Object) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const elementArg = getArg(<any>bufferPtr, index);
            result[index] = _marshalCsObjectToJs(elementArg);
        }
    } else if (elementType == MarshalerType.JSObject) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const elementArg = getArg(<any>bufferPtr, index);
            result[index] = _marshalJsObjectToJs(elementArg);
        }
    } else if (elementType == MarshalerType.Byte) {
        const bufferOffset = fixupPointer(bufferPtr, 0);
        const sourceView = dotnetApi.localHeapViewU8().subarray(bufferOffset, bufferOffset + length);
        result = sourceView.slice();//copy
    } else if (elementType == MarshalerType.Int32) {
        const bufferOffset = fixupPointer(bufferPtr, 2);
        const sourceView = dotnetApi.localHeapViewI32().subarray(bufferOffset, bufferOffset + length);
        result = sourceView.slice();//copy
    } else if (elementType == MarshalerType.Double) {
        const bufferOffset = fixupPointer(bufferPtr, 3);
        const sourceView = dotnetApi.localHeapViewF64().subarray(bufferOffset, bufferOffset + length);
        result = sourceView.slice();//copy
    } else {
        throw new Error(`NotImplementedException ${elementType}. ${jsinteropDoc}`);
    }
    Module._free(<any>bufferPtr);
    return result;
}

function _marshalSpanToJs(arg: JSMarshalerArgument, elementType?: MarshalerType): Span {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");

    const bufferPtr = getArgIntptr(arg);
    const length = getArgLength(arg);
    let result: Span | null = null;
    if (elementType == MarshalerType.Byte) {
        result = new Span(<any>bufferPtr, length, MemoryViewType.Byte);
    } else if (elementType == MarshalerType.Int32) {
        result = new Span(<any>bufferPtr, length, MemoryViewType.Int32);
    } else if (elementType == MarshalerType.Double) {
        result = new Span(<any>bufferPtr, length, MemoryViewType.Double);
    } else {
        throw new Error(`NotImplementedException ${elementType}. ${jsinteropDoc}`);
    }
    return result;
}

function _marshalArraySegmentToJs(arg: JSMarshalerArgument, elementType?: MarshalerType): ArraySegment {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");

    const bufferPtr = getArgIntptr(arg);
    const length = getArgLength(arg);
    let result: ArraySegment | null = null;
    if (elementType == MarshalerType.Byte) {
        result = new ArraySegment(<any>bufferPtr, length, MemoryViewType.Byte);
    } else if (elementType == MarshalerType.Int32) {
        result = new ArraySegment(<any>bufferPtr, length, MemoryViewType.Int32);
    } else if (elementType == MarshalerType.Double) {
        result = new ArraySegment(<any>bufferPtr, length, MemoryViewType.Double);
    } else {
        throw new Error(`NotImplementedException ${elementType}. ${jsinteropDoc}`);
    }
    const gcHandle = getArgGcHandle(arg);
    if (BuildConfiguration === "Debug") {
        (result as any)[proxyDebugSymbol] = `C# ArraySegment with GCHandle ${gcHandle}`;
    }
    setupManagedProxy(result, gcHandle);

    return result;
}

export function resolveOrRejectPromise(args: JSMarshalerArguments): void {
    if (!isRuntimeRunning()) {
        dotnetLogger.debug("This promise resolution/rejection can't be propagated to managed code, mono runtime already exited.");
        return;
    }
    args = fixupPointer(args, 0);
    const exc = getArg(args, 0);
    // TODO-WASM const receiver_should_free = WasmEnableThreads && is_receiver_should_free(args);
    try {
        assertRuntimeRunning();

        const res = getArg(args, 1);
        const argHandle = getArg(args, 2);
        const argValue = getArg(args, 3);

        const type = getArgType(argHandle);
        const jsHandle = getArgJsHandle(argHandle);

        const holder = getJSObjectFromJSHandle(jsHandle) as TaskHolder;
        dotnetAssert.fastCheck(holder, () => `Cannot find Promise for JSHandle ${jsHandle}`);

        holder.resolveOrReject(type, jsHandle, argValue);
        /* TODO-WASM if (receiver_should_free) {
            // this works together with AllocHGlobal in JSFunctionBinding.ResolveOrRejectPromise
            free(args as any);
        } else {*/
        setArgType(res, MarshalerType.Void);
        setArgType(exc, MarshalerType.None);
        //}

    } catch (ex: any) {
        /* TODO-WASM if (receiver_should_free) {
            mono_assert(false, () => `Failed to resolve or reject promise ${ex}`);
        }*/
        marshalExceptionToCs(exc, ex);
    }
}
