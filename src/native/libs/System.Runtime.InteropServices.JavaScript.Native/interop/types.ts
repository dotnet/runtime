// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { NativePointer, JSMarshalerArguments, CSFnHandle } from "../types";

export interface JSFunctionSignature extends NativePointer {
    __brand: "JSFunctionSignatures"
}

export interface JSMarshalerType extends NativePointer {
    __brand: "JSMarshalerType"
}

export interface JSMarshalerArgument extends NativePointer {
    __brand: "JSMarshalerArgument"
}

export type PThreadPtr = {
    __brand: "PThreadPtr" // like pthread_t in C
}
export type GCHandle = {
    __brand: "GCHandle"
}
export type JSHandle = {
    __brand: "JSHandle"
}
export type JSFnHandle = {
    __brand: "JSFnHandle"
}
export interface JSFunctionSignature extends NativePointer {
    __brand: "JSFunctionSignatures"
}
export type TimeStamp = {
    __brand: "TimeStamp"
}

export type WeakRefInternal<T extends object> = WeakRef<T> & {
    dispose?: () => void
}

export const JSHandleDisposed: JSHandle = <JSHandle><any>-1;
export const JSHandleNull: JSHandle = <JSHandle><any>0;
export const GCHandleNull: GCHandle = <GCHandle><any>0;
export const GCHandleInvalid: GCHandle = <GCHandle><any>-1;

export type MarshalerToJs = (arg: JSMarshalerArgument, elementType?: MarshalerType, resConverter?: MarshalerToJs, arg1Converter?: MarshalerToCs, arg2Converter?: MarshalerToCs, arg3Converter?: MarshalerToCs) => any;
export type MarshalerToCs = (arg: JSMarshalerArgument, value: any, elementType?: MarshalerType, resConverter?: MarshalerToCs, arg1Converter?: MarshalerToJs, arg2Converter?: MarshalerToJs, arg3Converter?: MarshalerToJs) => void;
export type BoundMarshalerToJs = (args: JSMarshalerArguments) => any;
export type BoundMarshalerToCs = (args: JSMarshalerArguments, value: any) => void;
// please keep in sync with src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\MarshalerType.cs
export const enum MarshalerType {
    None = 0,
    Void = 1,
    Discard,
    Boolean,
    Byte,
    Char,
    Int16,
    Int32,
    Int52,
    BigInt64,
    Double,
    Single,
    IntPtr,
    JSObject,
    Object,
    String,
    Exception,
    DateTime,
    DateTimeOffset,

    Nullable,
    Task,
    Array,
    ArraySegment,
    Span,
    Action,
    Function,
    DiscardNoWait,

    // only on runtime
    JSException,
    TaskResolved,
    TaskRejected,
    TaskPreCreated,
}

export type WrappedJSFunction = (args: JSMarshalerArguments) => void;

export type BindingClosureJS = {
    fn: Function,
    fqn: string,
    isDisposed: boolean,
    argsCount: number,
    argMarshalers: (BoundMarshalerToJs)[],
    resConverter: BoundMarshalerToCs | undefined,
    hasCleanup: boolean,
    isDiscardNoWait: boolean,
    isAsync: boolean,
    argCleanup: (Function | undefined)[]
}

export type BindingClosureCS = {
    fullyQualifiedName: string,
    argsCount: number,
    methodHandle: CSFnHandle,
    argMarshalers: (BoundMarshalerToCs)[],
    resConverter: BoundMarshalerToJs | undefined,
    isAsync: boolean,
    isDiscardNoWait: boolean,
    isDisposed: boolean,
}


// TODO-WASM: drop mono prefixes, move the type
export const enum MeasuredBlock {
    emscriptenStartup = "mono.emscriptenStartup",
    instantiateWasm = "mono.instantiateWasm",
    preRun = "mono.preRun",
    preRunWorker = "mono.preRunWorker",
    onRuntimeInitialized = "mono.onRuntimeInitialized",
    postRun = "mono.postRun",
    postRunWorker = "mono.postRunWorker",
    startRuntime = "mono.startRuntime",
    loadRuntime = "mono.loadRuntime",
    bindingsInit = "mono.bindingsInit",
    bindJsFunction = "mono.bindJsFunction:",
    bindCsFunction = "mono.bindCsFunction:",
    callJsFunction = "mono.callJsFunction:",
    callCsFunction = "mono.callCsFunction:",
    getAssemblyExports = "mono.getAssemblyExports:",
    instantiateAsset = "mono.instantiateAsset:",
}

export const JavaScriptMarshalerArgSize = 32;
// keep in sync with JSMarshalerArgumentImpl offsets
export const enum JSMarshalerArgumentOffsets {
    /* eslint-disable @typescript-eslint/no-duplicate-enum-values */
    BooleanValue = 0,
    ByteValue = 0,
    CharValue = 0,
    Int16Value = 0,
    Int32Value = 0,
    Int64Value = 0,
    SingleValue = 0,
    DoubleValue = 0,
    IntPtrValue = 0,
    JSHandle = 4,
    GCHandle = 4,
    Length = 8,
    Type = 12,
    ElementType = 13,
    ContextHandle = 16,
    ReceiverShouldFree = 20,
    CallerNativeTID = 24,
    SyncDoneSemaphorePtr = 28,
}
export const JSMarshalerTypeSize = 32;
// keep in sync with JSFunctionBinding.JSBindingType
export const enum JSBindingTypeOffsets {
    Type = 0,
    ResultMarshalerType = 16,
    Arg1MarshalerType = 20,
    Arg2MarshalerType = 24,
    Arg3MarshalerType = 28,
}
export const JSMarshalerSignatureHeaderSize = 4 * 8; // without Exception and Result
// keep in sync with JSFunctionBinding.JSBindingHeader
export const enum JSBindingHeaderOffsets {
    Version = 0,
    ArgumentCount = 4,
    ImportHandle = 8,
    FunctionNameOffset = 16,
    FunctionNameLength = 20,
    ModuleNameOffset = 24,
    ModuleNameLength = 28,
    Exception = 32,
    Result = 64,
}

export * from "../types";
