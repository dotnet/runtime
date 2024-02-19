// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { js_owned_gc_handle_symbol, teardown_managed_proxy } from "./gc-handles";
import { Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { getF32, getF64, getI16, getI32, getI64Big, getU16, getU32, getU8, setF32, setF64, setI16, setI32, setI64Big, setU16, setU32, setU8, localHeapViewF64, localHeapViewI32, localHeapViewU8, _zero_region, getB32, setB32, forceThreadMemoryViewRefresh } from "./memory";
import { mono_wasm_new_external_root } from "./roots";
import { GCHandle, JSHandle, MonoObject, MonoString, GCHandleNull, JSMarshalerArguments, JSFunctionSignature, JSMarshalerType, JSMarshalerArgument, MarshalerToJs, MarshalerToCs, WasmRoot, MarshalerType } from "./types/internal";
import { TypedArray, VoidPtr } from "./types/emscripten";
import { utf16ToString } from "./strings";
import { get_managed_stack_trace } from "./managed-exports";

export const cs_to_js_marshalers = new Map<MarshalerType, MarshalerToJs>();
export const js_to_cs_marshalers = new Map<MarshalerType, MarshalerToCs>();
export const bound_cs_function_symbol = Symbol.for("wasm bound_cs_function");
export const bound_js_function_symbol = Symbol.for("wasm bound_js_function");
export const imported_js_function_symbol = Symbol.for("wasm imported_js_function");
export const proxy_debug_symbol = Symbol.for("wasm proxy_debug");

export const JavaScriptMarshalerArgSize = 32;
// keep in sync with JSMarshalerArgumentImpl offsets
const enum JSMarshalerArgumentOffsets {
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
}
export const JSMarshalerTypeSize = 32;
// keep in sync with JSFunctionBinding.JSBindingType
const enum JSBindingTypeOffsets {
    Type = 0,
    ResultMarshalerType = 16,
    Arg1MarshalerType = 20,
    Arg2MarshalerType = 24,
    Arg3MarshalerType = 28,
}
export const JSMarshalerSignatureHeaderSize = 4 * 8; // without Exception and Result
// keep in sync with JSFunctionBinding.JSBindingHeader
const enum JSBindingHeaderOffsets {
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

export function alloc_stack_frame(size: number): JSMarshalerArguments {
    if (WasmEnableThreads) {
        forceThreadMemoryViewRefresh();
    }
    const bytes = JavaScriptMarshalerArgSize * size;
    const args = Module.stackAlloc(bytes) as any;
    _zero_region(args, bytes);
    set_args_context(args);
    return args;
}

export function get_arg(args: JSMarshalerArguments, index: number): JSMarshalerArgument {
    mono_assert(args, "Null args");
    return <any>args + (index * JavaScriptMarshalerArgSize);
}

export function is_args_exception(args: JSMarshalerArguments): boolean {
    mono_assert(args, "Null args");
    const exceptionType = get_arg_type(<any>args);
    return exceptionType !== MarshalerType.None;
}

export function is_receiver_should_free(args: JSMarshalerArguments): boolean {
    if (WasmEnableThreads) return false;
    mono_assert(args, "Null args");
    return getB32(<any>args + JSMarshalerArgumentOffsets.ReceiverShouldFree);
}

export function set_receiver_should_free(args: JSMarshalerArguments): void {
    mono_assert(args, "Null args");
    setB32(<any>args + JSMarshalerArgumentOffsets.ReceiverShouldFree, true);
}

export function set_args_context(args: JSMarshalerArguments): void {
    if (!WasmEnableThreads) return;
    mono_assert(args, "Null args");
    const exc = get_arg(args, 0);
    const res = get_arg(args, 1);
    set_arg_proxy_context(exc);
    set_arg_proxy_context(res);
}

export function get_sig(signature: JSFunctionSignature, index: number): JSMarshalerType {
    mono_assert(signature, "Null signatures");
    return <any>signature + (index * JSMarshalerTypeSize) + JSMarshalerSignatureHeaderSize;
}

export function get_signature_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU8(<any>sig + JSBindingTypeOffsets.Type);
}

export function get_signature_res_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU8(<any>sig + JSBindingTypeOffsets.ResultMarshalerType);
}

export function get_signature_arg1_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU8(<any>sig + JSBindingTypeOffsets.Arg1MarshalerType);
}

export function get_signature_arg2_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU8(<any>sig + JSBindingTypeOffsets.Arg2MarshalerType);
}

export function get_signature_arg3_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU8(<any>sig + JSBindingTypeOffsets.Arg2MarshalerType);
}

export function get_signature_argument_count(signature: JSFunctionSignature): number {
    mono_assert(signature, "Null signatures");
    return <any>getI32(<any>signature + JSBindingHeaderOffsets.ArgumentCount);
}

export function get_signature_version(signature: JSFunctionSignature): number {
    mono_assert(signature, "Null signatures");
    return <any>getI32(<any>signature + JSBindingHeaderOffsets.Version);
}

export function get_signature_handle(signature: JSFunctionSignature): number {
    mono_assert(signature, "Null signatures");
    return <any>getI32(<any>signature + JSBindingHeaderOffsets.ImportHandle);
}

export function get_signature_function_name(signature: JSFunctionSignature): string | null {
    mono_assert(signature, "Null signatures");
    const functionNameOffset = <any>getI32(<any>signature + JSBindingHeaderOffsets.FunctionNameOffset);
    if (functionNameOffset === 0) return null;
    const functionNameLength = <any>getI32(<any>signature + JSBindingHeaderOffsets.FunctionNameLength);
    mono_assert(functionNameOffset, "Null name");
    return utf16ToString(<any>signature + functionNameOffset, <any>signature + functionNameOffset + functionNameLength);
}

export function get_signature_module_name(signature: JSFunctionSignature): string | null {
    mono_assert(signature, "Null signatures");
    const moduleNameOffset = <any>getI32(<any>signature + JSBindingHeaderOffsets.ModuleNameOffset);
    if (moduleNameOffset === 0) return null;
    const moduleNameLength = <any>getI32(<any>signature + JSBindingHeaderOffsets.ModuleNameLength);
    return utf16ToString(<any>signature + moduleNameOffset, <any>signature + moduleNameOffset + moduleNameLength);
}

export function get_sig_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null signatures");
    return <any>getU8(sig);
}

export function get_arg_type(arg: JSMarshalerArgument): MarshalerType {
    mono_assert(arg, "Null arg");
    const type = getU8(<any>arg + JSMarshalerArgumentOffsets.Type);
    return <any>type;
}

export function get_arg_element_type(arg: JSMarshalerArgument): MarshalerType {
    mono_assert(arg, "Null arg");
    const type = getU8(<any>arg + JSMarshalerArgumentOffsets.ElementType);
    return <any>type;
}

export function set_arg_type(arg: JSMarshalerArgument, type: MarshalerType): void {
    mono_assert(arg, "Null arg");
    setU8(<any>arg + JSMarshalerArgumentOffsets.Type, type);
}

export function set_arg_element_type(arg: JSMarshalerArgument, type: MarshalerType): void {
    mono_assert(arg, "Null arg");
    setU8(<any>arg + JSMarshalerArgumentOffsets.ElementType, type);
}

export function get_arg_bool(arg: JSMarshalerArgument): boolean {
    mono_assert(arg, "Null arg");
    return getB32(<any>arg);
}

export function get_arg_u8(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getU8(<any>arg);
}

export function get_arg_u16(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getU16(<any>arg);
}

export function get_arg_i16(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getI16(<any>arg);
}

export function get_arg_i32(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getI32(<any>arg);
}

export function get_arg_intptr(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getI32(<any>arg);
}

export function get_arg_i52(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    // we know that the range check and conversion from Int64 was be done on C# side
    return getF64(<any>arg);
}

export function get_arg_i64_big(arg: JSMarshalerArgument): bigint {
    mono_assert(arg, "Null arg");
    return getI64Big(<any>arg);
}

export function get_arg_date(arg: JSMarshalerArgument): Date {
    mono_assert(arg, "Null arg");
    const unixTime = getF64(<any>arg);
    const date = new Date(unixTime);
    return date;
}

export function get_arg_f32(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getF32(<any>arg);
}

export function get_arg_f64(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return getF64(<any>arg);
}

export function set_arg_bool(arg: JSMarshalerArgument, value: boolean): void {
    mono_assert(arg, "Null arg");
    mono_check(typeof value === "boolean", () => `Value is not a Boolean: ${value} (${typeof (value)})`);
    setB32(<any>arg, value ? 1 : 0);
}

export function set_arg_u8(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setU8(<any>arg, value);
}

export function set_arg_u16(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setU16(<any>arg, value);
}

export function set_arg_i16(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setI16(<any>arg, value);
}

export function set_arg_i32(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg, value);
}

export function set_arg_intptr(arg: JSMarshalerArgument, value: VoidPtr): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg, <any>value);
}

export function set_arg_i52(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    mono_check(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    // we know that conversion to Int64 would be done on C# side
    setF64(<any>arg, value);
}

export function set_arg_i64_big(arg: JSMarshalerArgument, value: bigint): void {
    mono_assert(arg, "Null arg");
    setI64Big(<any>arg, value);
}

export function set_arg_date(arg: JSMarshalerArgument, value: Date): void {
    mono_assert(arg, "Null arg");
    // getTime() is always UTC
    const unixTime = value.getTime();
    setF64(<any>arg, unixTime);
}

export function set_arg_f64(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setF64(<any>arg, value);
}

export function set_arg_f32(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    setF32(<any>arg, value);
}

export function get_arg_js_handle(arg: JSMarshalerArgument): JSHandle {
    mono_assert(arg, "Null arg");
    return <any>getI32(<any>arg + JSMarshalerArgumentOffsets.JSHandle);
}

export function set_arg_proxy_context(arg: JSMarshalerArgument): void {
    if (!WasmEnableThreads) return;
    mono_assert(arg, "Null arg");
    setI32(<any>arg + JSMarshalerArgumentOffsets.ContextHandle, <any>runtimeHelpers.proxyGCHandle);
}

export function set_js_handle(arg: JSMarshalerArgument, jsHandle: JSHandle): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg + JSMarshalerArgumentOffsets.JSHandle, <any>jsHandle);
    set_arg_proxy_context(arg);
}

export function get_arg_gc_handle(arg: JSMarshalerArgument): GCHandle {
    mono_assert(arg, "Null arg");
    return <any>getI32(<any>arg + JSMarshalerArgumentOffsets.GCHandle);
}

export function set_gc_handle(arg: JSMarshalerArgument, gcHandle: GCHandle): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg + JSMarshalerArgumentOffsets.GCHandle, <any>gcHandle);
    set_arg_proxy_context(arg);
}

export function get_string_root(arg: JSMarshalerArgument): WasmRoot<MonoString> {
    mono_assert(arg, "Null arg");
    return mono_wasm_new_external_root<MonoString>(<any>arg);
}

export function get_arg_length(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return <any>getI32(<any>arg + JSMarshalerArgumentOffsets.Length);
}

export function set_arg_length(arg: JSMarshalerArgument, size: number): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg + JSMarshalerArgumentOffsets.Length, size);
}

export function set_root(arg: JSMarshalerArgument, root: WasmRoot<MonoObject>): void {
    mono_assert(arg, "Null arg");
    setU32(<any>arg + 0, root.get_address());
}

export interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}

export class ManagedObject implements IDisposable {
    dispose(): void {
        teardown_managed_proxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (<any>this)[js_owned_gc_handle_symbol] === GCHandleNull;
    }

    toString(): string {
        return `CsObject(gc_handle: ${(<any>this)[js_owned_gc_handle_symbol]})`;
    }
}

export class ManagedError extends Error implements IDisposable {
    private superStack: any;
    private managed_stack: any;
    constructor(message: string) {
        super(message);
        this.superStack = Object.getOwnPropertyDescriptor(this, "stack"); // this works on Chrome
        Object.defineProperty(this, "stack", {
            get: this.getManageStack,
        });
    }

    getSuperStack() {
        if (this.superStack) {
            if (this.superStack.value !== undefined)
                return this.superStack.value;
            if (this.superStack.get !== undefined)
                return this.superStack.get.call(this);
        }
        return super.stack; // this works on FF
    }

    getManageStack() {
        if (this.managed_stack) {
            return this.managed_stack;
        }
        if (!loaderHelpers.is_runtime_running()) {
            this.managed_stack = "... omitted managed stack trace.\n" + this.getSuperStack();
            return this.managed_stack;
        }
        if (!WasmEnableThreads || runtimeHelpers.proxyGCHandle) {
            const gc_handle = (<any>this)[js_owned_gc_handle_symbol];
            if (gc_handle !== GCHandleNull) {
                const managed_stack = get_managed_stack_trace(gc_handle);
                if (managed_stack) {
                    this.managed_stack = managed_stack + "\n" + this.getSuperStack();
                    return this.managed_stack;
                }
            }
        }
        return this.getSuperStack();
    }

    dispose(): void {
        teardown_managed_proxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (<any>this)[js_owned_gc_handle_symbol] === GCHandleNull;
    }
}

export function get_signature_marshaler(signature: JSFunctionSignature, index: number): JSHandle {
    mono_assert(signature, "Null signatures");
    const sig = get_sig(signature, index);
    return <any>getU32(<any>sig + JSBindingHeaderOffsets.ImportHandle);
}


export function array_element_size(element_type: MarshalerType): number {
    return element_type == MarshalerType.Byte ? 1
        : element_type == MarshalerType.Int32 ? 4
            : element_type == MarshalerType.Int52 ? 8
                : element_type == MarshalerType.Double ? 8
                    : element_type == MarshalerType.String ? JavaScriptMarshalerArgSize
                        : element_type == MarshalerType.Object ? JavaScriptMarshalerArgSize
                            : element_type == MarshalerType.JSObject ? JavaScriptMarshalerArgSize
                                : -1;
}

export const enum MemoryViewType {
    Byte = 0,
    Int32 = 1,
    Double = 2,
}

abstract class MemoryView implements IMemoryView {
    protected constructor(public _pointer: VoidPtr, public _length: number, public _viewType: MemoryViewType) {
    }

    abstract dispose(): void;
    abstract get isDisposed(): boolean;

    _unsafe_create_view(): TypedArray {
        // this view must be short lived so that it doesn't fail after wasm memory growth
        // for that reason we also don't give the view out to end user and provide set/slice/copyTo API instead
        const view = this._viewType == MemoryViewType.Byte ? new Uint8Array(localHeapViewU8().buffer, <any>this._pointer, this._length)
            : this._viewType == MemoryViewType.Int32 ? new Int32Array(localHeapViewI32().buffer, <any>this._pointer, this._length)
                : this._viewType == MemoryViewType.Double ? new Float64Array(localHeapViewF64().buffer, <any>this._pointer, this._length)
                    : null;
        if (!view) throw new Error("NotImplementedException");
        return view;
    }

    set(source: TypedArray, targetOffset?: number): void {
        mono_check(!this.isDisposed, "ObjectDisposedException");
        const targetView = this._unsafe_create_view();
        mono_check(source && targetView && source.constructor === targetView.constructor, () => `Expected ${targetView.constructor}`);
        targetView.set(source, targetOffset);
        // TODO consider memory write barrier
    }

    copyTo(target: TypedArray, sourceOffset?: number): void {
        mono_check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        mono_check(target && sourceView && target.constructor === sourceView.constructor, () => `Expected ${sourceView.constructor}`);
        const trimmedSource = sourceView.subarray(sourceOffset);
        // TODO consider memory read barrier
        target.set(trimmedSource);
    }

    slice(start?: number, end?: number): TypedArray {
        mono_check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        // TODO consider memory read barrier
        return sourceView.slice(start, end);
    }

    get length(): number {
        mono_check(!this.isDisposed, "ObjectDisposedException");
        return this._length;
    }

    get byteLength(): number {
        mono_check(!this.isDisposed, "ObjectDisposedException");
        return this._viewType == MemoryViewType.Byte ? this._length
            : this._viewType == MemoryViewType.Int32 ? this._length << 2
                : this._viewType == MemoryViewType.Double ? this._length << 3
                    : 0;
    }
}

export interface IMemoryView extends IDisposable {
    /**
     * copies elements from provided source to the wasm memory.
     * target has to have the elements of the same type as the underlying C# array.
     * same as TypedArray.set()
     */
    set(source: TypedArray, targetOffset?: number): void;
    /**
     * copies elements from wasm memory to provided target.
     * target has to have the elements of the same type as the underlying C# array.
     */
    copyTo(target: TypedArray, sourceOffset?: number): void;
    /**
     * same as TypedArray.slice()
     */
    slice(start?: number, end?: number): TypedArray;

    get length(): number;
    get byteLength(): number;
}

export class Span extends MemoryView {
    private is_disposed = false;
    public constructor(pointer: VoidPtr, length: number, viewType: MemoryViewType) {
        super(pointer, length, viewType);
    }
    dispose(): void {
        this.is_disposed = true;
    }
    get isDisposed(): boolean {
        return this.is_disposed;
    }
}

export class ArraySegment extends MemoryView {
    public constructor(pointer: VoidPtr, length: number, viewType: MemoryViewType) {
        super(pointer, length, viewType);
    }

    dispose(): void {
        teardown_managed_proxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (<any>this)[js_owned_gc_handle_symbol] === GCHandleNull;
    }
}
