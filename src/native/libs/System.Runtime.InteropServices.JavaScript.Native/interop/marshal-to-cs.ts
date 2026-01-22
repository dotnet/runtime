// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { dotnetBrowserUtilsExports, dotnetApi, dotnetAssert, Module } from "./cross-module";

import type { BoundMarshalerToCs, JSMarshalerArgument, JSMarshalerArguments, JSMarshalerType, MarshalerToCs, MarshalerToJs, TypedArray } from "./types";
import { JavaScriptMarshalerArgSize, MarshalerType } from "./types";
import {
    arrayElementSize, getArg, getSignatureArg1Type, getSignatureArg2Type, getSignatureArg3Type, getSignatureResType,
    getArgType, getArgGcHandle, getMarshalerToJsByType,
    jsToCsMarshalers, jsInteropState,
    setArgBool, setArgDate, setArgElementType, setArgF32, setArgF64, setArgI16, setArgI32, setArgI52, setArgI64Big, setArgIntptr, setArgLength, setArgProxyContext,
    setArgType, setArgU16, setArgU8, setGcHandle, setJsHandle,
    getMarshalerToCsByType,
    jsinteropDoc,
} from "./marshal";
import { assertNotDisposed, csOwnedJsHandleSymbol, jsOwnedGcHandleSymbol, getJsHandleFromJSObject, allocGcvHandle, setupManagedProxy, boundJsFunctionSymbol, proxyDebugSymbol } from "./gc-handles";
import { fixupPointer } from "./utils";
import { ArraySegment, ManagedError, ManagedObject, MemoryViewType, PromiseHolder, Span } from "./marshaled-types";
import { isThenable } from "./cancelable-promise";

export function initializeMarshalersToCs(): void {
    if (jsToCsMarshalers.size == 0) {
        jsToCsMarshalers.set(MarshalerType.Array, marshalArrayToCs);
        jsToCsMarshalers.set(MarshalerType.Span, _marshalSpanToCs);
        jsToCsMarshalers.set(MarshalerType.ArraySegment, _marshalArraySegmentToCs);
        jsToCsMarshalers.set(MarshalerType.Boolean, marshalBoolToCs);
        jsToCsMarshalers.set(MarshalerType.Byte, _marshalByteToCs);
        jsToCsMarshalers.set(MarshalerType.Char, _marshalCharToCs);
        jsToCsMarshalers.set(MarshalerType.Int16, _marshalInt16ToCs);
        jsToCsMarshalers.set(MarshalerType.Int32, _marshalInt32ToCs);
        jsToCsMarshalers.set(MarshalerType.Int52, _marshalInt52ToCs);
        jsToCsMarshalers.set(MarshalerType.BigInt64, _marshalBigint64ToCs);
        jsToCsMarshalers.set(MarshalerType.Double, _marshalDoubleToCs);
        jsToCsMarshalers.set(MarshalerType.Single, _marshalFloatToCs);
        jsToCsMarshalers.set(MarshalerType.IntPtr, marshalIntptrToCs);
        jsToCsMarshalers.set(MarshalerType.DateTime, _marshalDateTimeToCs);
        jsToCsMarshalers.set(MarshalerType.DateTimeOffset, _marshalDateTimeOffsetToCs);
        jsToCsMarshalers.set(MarshalerType.String, marshalStringToCs);
        jsToCsMarshalers.set(MarshalerType.Exception, marshalExceptionToCs);
        jsToCsMarshalers.set(MarshalerType.JSException, marshalExceptionToCs);
        jsToCsMarshalers.set(MarshalerType.JSObject, marshalJsObjectToCs);
        jsToCsMarshalers.set(MarshalerType.Object, marshalCsObjectToCs);
        jsToCsMarshalers.set(MarshalerType.Task, marshalTaskToCs);
        jsToCsMarshalers.set(MarshalerType.TaskResolved, marshalTaskToCs);
        jsToCsMarshalers.set(MarshalerType.TaskRejected, marshalTaskToCs);
        jsToCsMarshalers.set(MarshalerType.Action, _marshalFunctionToCs);
        jsToCsMarshalers.set(MarshalerType.Function, _marshalFunctionToCs);
        jsToCsMarshalers.set(MarshalerType.None, _marshalNullToCs);// also void
        jsToCsMarshalers.set(MarshalerType.Discard, _marshalNullToCs);// also void
        jsToCsMarshalers.set(MarshalerType.Void, _marshalNullToCs);// also void
        jsToCsMarshalers.set(MarshalerType.DiscardNoWait, _marshalNullToCs);// also void
    }
}

export function bindArgMarshalToCs(sig: JSMarshalerType, marshalerType: MarshalerType, index: number): BoundMarshalerToCs | undefined {
    if (marshalerType === MarshalerType.None || marshalerType === MarshalerType.Void || marshalerType === MarshalerType.Discard || marshalerType === MarshalerType.DiscardNoWait) {
        return undefined;
    }
    let resMarshaler: MarshalerToCs | undefined = undefined;
    let arg1Marshaler: MarshalerToJs | undefined = undefined;
    let arg2Marshaler: MarshalerToJs | undefined = undefined;
    let arg3Marshaler: MarshalerToJs | undefined = undefined;

    arg1Marshaler = getMarshalerToJsByType(getSignatureArg1Type(sig));
    arg2Marshaler = getMarshalerToJsByType(getSignatureArg2Type(sig));
    arg3Marshaler = getMarshalerToJsByType(getSignatureArg3Type(sig));
    const marshalerTypeRes = getSignatureResType(sig);
    resMarshaler = getMarshalerToCsByType(marshalerTypeRes);
    if (marshalerType === MarshalerType.Nullable) {
        // nullable has nested type information, it's stored in res slot of the signature. The marshaler is the same as for non-nullable primitive type.
        marshalerType = marshalerTypeRes;
    }
    const converter = getMarshalerToCsByType(marshalerType)!;
    const elementType = getSignatureArg1Type(sig);

    const argOffset = index * JavaScriptMarshalerArgSize;
    return (args: JSMarshalerArguments, value: any) => {
        converter(<any>args + argOffset, value, elementType, resMarshaler, arg1Marshaler, arg2Marshaler, arg3Marshaler);
    };
}

export function marshalBoolToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Boolean);
        setArgBool(arg, value);
    }
}

function _marshalByteToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Byte);
        setArgU8(arg, value);
    }
}

function _marshalCharToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Char);
        setArgU16(arg, value);
    }
}

function _marshalInt16ToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Int16);
        setArgI16(arg, value);
    }
}

function _marshalInt32ToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Int32);
        setArgI32(arg, value);
    }
}

function _marshalInt52ToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Int52);
        setArgI52(arg, value);
    }
}

function _marshalBigint64ToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.BigInt64);
        setArgI64Big(arg, value);
    }
}

function _marshalDoubleToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Double);
        setArgF64(arg, value);
    }
}

function _marshalFloatToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.Single);
        setArgF32(arg, value);
    }
}

export function marshalIntptrToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.IntPtr);
        setArgIntptr(arg, value);
    }
}

function _marshalDateTimeToCs(arg: JSMarshalerArgument, value: Date): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        dotnetAssert.check(value instanceof Date, "Value is not a Date");
        setArgType(arg, MarshalerType.DateTime);
        setArgDate(arg, value);
    }
}

function _marshalDateTimeOffsetToCs(arg: JSMarshalerArgument, value: Date): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        dotnetAssert.check(value instanceof Date, "Value is not a Date");
        setArgType(arg, MarshalerType.DateTimeOffset);
        setArgDate(arg, value);
    }
}

export function marshalStringToCs(arg: JSMarshalerArgument, value: string) {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        setArgType(arg, MarshalerType.String);
        dotnetAssert.check(typeof value === "string", "Value is not a String");
        _marshalStringToCsImpl(arg, value);
    }
}

function _marshalStringToCsImpl(arg: JSMarshalerArgument, value: string) {
    const bufferLen = value.length * 2;
    const buffer = Module._malloc(bufferLen);// together with Marshal.FreeHGlobal
    dotnetBrowserUtilsExports.stringToUTF16(buffer as any, buffer as any + bufferLen, value);
    setArgIntptr(arg, buffer);
    setArgLength(arg, value.length);
}

function _marshalNullToCs(arg: JSMarshalerArgument) {
    setArgType(arg, MarshalerType.None);
}

function _marshalFunctionToCs(arg: JSMarshalerArgument, value: Function, _?: MarshalerType, resConverter?: MarshalerToCs, arg1Converter?: MarshalerToJs, arg2Converter?: MarshalerToJs, arg3Converter?: MarshalerToJs): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
        return;
    }
    dotnetAssert.check(value && value instanceof Function, "Value is not a Function");

    // TODO: we could try to cache value -> existing JSHandle
    const wrapper: any = function delegateWrapper(args: JSMarshalerArguments) {
        const exc = getArg(args, 0);
        const res = getArg(args, 1);
        const arg1 = getArg(args, 2);
        const arg2 = getArg(args, 3);
        const arg3 = getArg(args, 4);

        const previousPendingSynchronousCall = jsInteropState.isPendingSynchronousCall;
        try {
            dotnetAssert.check(!wrapper.isDisposed, "Function is disposed and should not be invoked anymore.");

            let arg1Js: any = undefined;
            let arg2Js: any = undefined;
            let arg3Js: any = undefined;
            if (arg1Converter) {
                arg1Js = arg1Converter(arg1);
            }
            if (arg2Converter) {
                arg2Js = arg2Converter(arg2);
            }
            if (arg3Converter) {
                arg3Js = arg3Converter(arg3);
            }
            jsInteropState.isPendingSynchronousCall = true; // this is always synchronous call for now
            const resJs = value(arg1Js, arg2Js, arg3Js);
            if (resConverter) {
                resConverter(res, resJs);
            }

        } catch (ex) {
            marshalExceptionToCs(exc, ex);
        } finally {
            jsInteropState.isPendingSynchronousCall = previousPendingSynchronousCall;
        }
    };

    wrapper[boundJsFunctionSymbol] = true;
    wrapper.isDisposed = false;
    wrapper.dispose = () => {
        wrapper.isDisposed = true;
    };
    const boundFunctionHandle = getJsHandleFromJSObject(wrapper)!;
    if (BuildConfiguration === "Debug") {
        const anyValue = value as any;
        if (anyValue[proxyDebugSymbol] === undefined) {
            wrapper[proxyDebugSymbol] = `Proxy of JS Function with JSHandle ${boundFunctionHandle}`;
        } else {
            wrapper[proxyDebugSymbol] = anyValue[proxyDebugSymbol];
        }
    }
    setJsHandle(arg, boundFunctionHandle);
    setArgType(arg, MarshalerType.Function);//TODO or action ?
}

export function marshalTaskToCs(arg: JSMarshalerArgument, value: Promise<any>, _?: MarshalerType, resConverter?: MarshalerToCs) {
    const handleIsPreallocated = getArgType(arg) == MarshalerType.TaskPreCreated;
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    }
    dotnetAssert.check(isThenable(value), "Value is not a Promise");

    const gcHandle = handleIsPreallocated ? getArgGcHandle(arg) : allocGcvHandle();
    if (!handleIsPreallocated) {
        setGcHandle(arg, gcHandle);
        setArgType(arg, MarshalerType.Task);
    }

    const holder = new PromiseHolder(value, gcHandle, resConverter || marshalCsObjectToCs);
    setupManagedProxy(holder, gcHandle);

    if (BuildConfiguration === "Debug") {
        (holder as any)[proxyDebugSymbol] = `PromiseHolder with GCHandle ${gcHandle}`;
    }

    value.then(data => holder.resolve(data), reason => holder.reject(reason));
}

export function marshalExceptionToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else if (value instanceof ManagedError) {
        setArgType(arg, MarshalerType.Exception);
        // this is managed exception round-trip
        const gcHandle = assertNotDisposed(value);
        setGcHandle(arg, gcHandle);
    } else {
        dotnetAssert.fastCheck(typeof value === "object" || typeof value === "string", () => `Value is not an Error ${typeof value}`);
        setArgType(arg, MarshalerType.JSException);
        const message = value.toString();
        _marshalStringToCsImpl(arg, message);
        const knownJsHandle = value[csOwnedJsHandleSymbol];
        if (knownJsHandle) {
            setJsHandle(arg, knownJsHandle);
        } else {
            const jsHandle = getJsHandleFromJSObject(value)!;
            if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
                value[proxyDebugSymbol] = `JS Error with JSHandle ${jsHandle}`;
            }
            setJsHandle(arg, jsHandle);
        }
    }
}

export function marshalJsObjectToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === undefined || value === null) {
        setArgType(arg, MarshalerType.None);
        setArgProxyContext(arg);
    } else {
        // if value was ManagedObject, it would be double proxied, but the C# signature requires that
        dotnetAssert.fastCheck(value[jsOwnedGcHandleSymbol] === undefined, () => `JSObject proxy of ManagedObject proxy is not supported. ${jsinteropDoc}`);
        dotnetAssert.fastCheck(typeof value === "function" || typeof value === "object", () => `JSObject proxy of ${typeof value} is not supported`);

        setArgType(arg, MarshalerType.JSObject);
        const jsHandle = getJsHandleFromJSObject(value)!;
        if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
            value[proxyDebugSymbol] = `JS Object with JSHandle ${jsHandle}`;
        }
        setJsHandle(arg, jsHandle);
    }
}

export function marshalCsObjectToCs(arg: JSMarshalerArgument, value: any): void {
    if (value === undefined || value === null) {
        setArgType(arg, MarshalerType.None);
        setArgProxyContext(arg);
    } else {
        const gcHandle = value[jsOwnedGcHandleSymbol];
        const jsType = typeof (value);
        if (gcHandle === undefined) {
            if (jsType === "string" || jsType === "symbol") {
                setArgType(arg, MarshalerType.String);
                _marshalStringToCsImpl(arg, value);
            } else if (jsType === "number") {
                setArgType(arg, MarshalerType.Double);
                setArgF64(arg, value);
            } else if (jsType === "bigint") {
                // we do it because not all bigint values could fit into Int64
                throw new Error("NotImplementedException: bigint");
            } else if (jsType === "boolean") {
                setArgType(arg, MarshalerType.Boolean);
                setArgBool(arg, value);
            } else if (value instanceof Date) {
                setArgType(arg, MarshalerType.DateTime);
                setArgDate(arg, value);
            } else if (value instanceof Error) {
                marshalExceptionToCs(arg, value);
            } else if (value instanceof Uint8Array) {
                marshalArrayToCsImpl(arg, value, MarshalerType.Byte);
            } else if (value instanceof Float64Array) {
                marshalArrayToCsImpl(arg, value, MarshalerType.Double);
            } else if (value instanceof Int32Array) {
                marshalArrayToCsImpl(arg, value, MarshalerType.Int32);
            } else if (Array.isArray(value)) {
                marshalArrayToCsImpl(arg, value, MarshalerType.Object);
            } else if (value instanceof Int16Array
                || value instanceof Int8Array
                || value instanceof Uint8ClampedArray
                || value instanceof Uint16Array
                || value instanceof Uint32Array
                || value instanceof Float32Array
            ) {
                throw new Error("NotImplementedException: TypedArray");
            } else if (isThenable(value)) {
                marshalTaskToCs(arg, value);
            } else if (value instanceof Span) {
                throw new Error("NotImplementedException: Span");
            } else if (jsType == "object") {
                const jsHandle = getJsHandleFromJSObject(value);
                setArgType(arg, MarshalerType.JSObject);
                if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
                    value[proxyDebugSymbol] = `JS Object with JSHandle ${jsHandle}`;
                }
                setJsHandle(arg, jsHandle);
            } else {
                throw new Error(`JSObject proxy is not supported for ${jsType} ${value}`);
            }
        } else {
            assertNotDisposed(value);
            if (value instanceof ArraySegment) {
                throw new Error("NotImplementedException: ArraySegment. " + jsinteropDoc);
            } else if (value instanceof ManagedError) {
                setArgType(arg, MarshalerType.Exception);
                setGcHandle(arg, gcHandle);
            } else if (value instanceof ManagedObject) {
                setArgType(arg, MarshalerType.Object);
                setGcHandle(arg, gcHandle);
            } else {
                throw new Error("NotImplementedException " + jsType + ". " + jsinteropDoc);
            }
        }
    }
}

export function marshalArrayToCs(arg: JSMarshalerArgument, value: Array<any> | TypedArray | undefined | null, elementType?: MarshalerType): void {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");
    marshalArrayToCsImpl(arg, value, elementType);
}

export function marshalArrayToCsImpl(arg: JSMarshalerArgument, value: Array<any> | TypedArray | undefined | null, elementType: MarshalerType): void {
    if (value === null || value === undefined) {
        setArgType(arg, MarshalerType.None);
    } else {
        const elementSize = arrayElementSize(elementType);
        dotnetAssert.fastCheck(elementSize != -1, () => `Element type ${elementType} not supported`);
        const length = value.length;
        const bufferLength = elementSize * length;
        const bufferPtr = Module._malloc(bufferLength) as any;
        if (elementType == MarshalerType.String) {
            dotnetAssert.check(Array.isArray(value), "Value is not an Array");
            dotnetBrowserUtilsExports.zeroRegion(bufferPtr, bufferLength);
            for (let index = 0; index < length; index++) {
                const elementArg = getArg(<any>bufferPtr, index);
                marshalStringToCs(elementArg, value[index]);
            }
        } else if (elementType == MarshalerType.Object) {
            dotnetAssert.check(Array.isArray(value), "Value is not an Array");
            dotnetBrowserUtilsExports.zeroRegion(bufferPtr, bufferLength);
            for (let index = 0; index < length; index++) {
                const elementArg = getArg(<any>bufferPtr, index);
                marshalCsObjectToCs(elementArg, value[index]);
            }
        } else if (elementType == MarshalerType.JSObject) {
            dotnetAssert.check(Array.isArray(value), "Value is not an Array");
            dotnetBrowserUtilsExports.zeroRegion(bufferPtr, bufferLength);
            for (let index = 0; index < length; index++) {
                const elementArg = getArg(bufferPtr, index);
                marshalJsObjectToCs(elementArg, value[index]);
            }
        } else if (elementType == MarshalerType.Byte) {
            dotnetAssert.check(Array.isArray(value) || value instanceof Uint8Array, "Value is not an Array or Uint8Array");
            const bufferOffset = fixupPointer(bufferPtr, 0);
            const targetView = dotnetApi.localHeapViewU8().subarray(bufferOffset, bufferOffset + length);
            targetView.set(value);
        } else if (elementType == MarshalerType.Int32) {
            dotnetAssert.check(Array.isArray(value) || value instanceof Int32Array, "Value is not an Array or Int32Array");
            const bufferOffset = fixupPointer(bufferPtr, 2);
            const targetView = dotnetApi.localHeapViewI32().subarray(bufferOffset, bufferOffset + length);
            targetView.set(value);
        } else if (elementType == MarshalerType.Double) {
            dotnetAssert.check(Array.isArray(value) || value instanceof Float64Array, "Value is not an Array or Float64Array");
            const bufferOffset = fixupPointer(bufferPtr, 3);
            const targetView = dotnetApi.localHeapViewF64().subarray(bufferOffset, bufferOffset + length);
            targetView.set(value);
        } else {
            throw new Error("not implemented");
        }
        setArgIntptr(arg, bufferPtr);
        setArgType(arg, MarshalerType.Array);
        setArgElementType(arg, elementType);
        setArgLength(arg, value.length);
    }
}

function _marshalSpanToCs(arg: JSMarshalerArgument, value: Span, elementType?: MarshalerType): void {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");
    dotnetAssert.check(!value.isDisposed, "ObjectDisposedException");
    checkViewType(elementType, value._viewType);

    setArgType(arg, MarshalerType.Span);
    setArgIntptr(arg, value._pointer);
    setArgLength(arg, value.length);
}

// this only supports round-trip
function _marshalArraySegmentToCs(arg: JSMarshalerArgument, value: ArraySegment, elementType?: MarshalerType): void {
    dotnetAssert.check(!!elementType, "Expected valid elementType parameter");
    const gcHandle = assertNotDisposed(value);
    dotnetAssert.check(gcHandle, "Only roundtrip of ArraySegment instance created by C#");
    checkViewType(elementType, value._viewType);
    setArgType(arg, MarshalerType.ArraySegment);
    setArgIntptr(arg, value._pointer);
    setArgLength(arg, value.length);
    setGcHandle(arg, gcHandle);
}

function checkViewType(elementType: MarshalerType, viewType: MemoryViewType) {
    if (elementType == MarshalerType.Byte) {
        dotnetAssert.check(MemoryViewType.Byte == viewType, "Expected MemoryViewType.Byte");
    } else if (elementType == MarshalerType.Int32) {
        dotnetAssert.check(MemoryViewType.Int32 == viewType, "Expected MemoryViewType.Int32");
    } else if (elementType == MarshalerType.Double) {
        dotnetAssert.check(MemoryViewType.Double == viewType, "Expected MemoryViewType.Double");
    } else {
        throw new Error(`NotImplementedException ${elementType} `);
    }
}

