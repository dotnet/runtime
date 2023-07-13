// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { js_owned_gc_handle_symbol, teardown_managed_proxy } from "./gc-handles";
import { Module, runtimeHelpers } from "./globals";
import { getF32, getF64, getI16, getI32, getI64Big, getU16, getU32, getU8, setF32, setF64, setI16, setI32, setI64Big, setU16, setU32, setU8, localHeapViewF64, localHeapViewI32, localHeapViewU8 } from "./memory";
import { mono_wasm_new_external_root } from "./roots";
import { GCHandle, JSHandle, MonoObject, MonoString, GCHandleNull, JSMarshalerArguments, JSFunctionSignature, JSMarshalerType, JSMarshalerArgument, MarshalerToJs, MarshalerToCs, WasmRoot, MarshalerType } from "./types/internal";
import { CharPtr, TypedArray, VoidPtr } from "./types/emscripten";

export const cs_to_js_marshalers = new Map<MarshalerType, MarshalerToJs>();
export const js_to_cs_marshalers = new Map<MarshalerType, MarshalerToCs>();
export const bound_cs_function_symbol = Symbol.for("wasm bound_cs_function");
export const bound_js_function_symbol = Symbol.for("wasm bound_js_function");
export const imported_js_function_symbol = Symbol.for("wasm imported_js_function");

/**
 * JSFunctionSignature is pointer to [
 *      Version: number,
 *      ArgumentCount: number,
 *      exc:  { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
 *      res:  { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
 *      arg1: { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
 *      arg2: { jsType: JsTypeFlags, type:MarshalerType, restype:MarshalerType, arg1type:MarshalerType, arg2type:MarshalerType, arg3type:MarshalerType}
 *      ...
 *      ] 
 * 
 * Layout of the call stack frame buffers is array of JSMarshalerArgument
 * JSMarshalerArguments is pointer to [
 *      exc:  {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      res:  {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      arg1: {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      arg2: {type:MarshalerType, handle: IntPtr, data: Int64|Ref*|Void* },
 *      ...
 *      ]
 */


export const JavaScriptMarshalerArgSize = 16;
export const JSMarshalerTypeSize = 32;
export const JSMarshalerSignatureHeaderSize = 4 + 4; // without Exception and Result

export function alloc_stack_frame(size: number): JSMarshalerArguments {
    const args = Module.stackAlloc(JavaScriptMarshalerArgSize * size) as any;
    mono_assert(args && (<any>args) % 8 == 0, "Arg alignment");
    const exc = get_arg(args, 0);
    set_arg_type(exc, MarshalerType.None);
    const res = get_arg(args, 1);
    set_arg_type(res, MarshalerType.None);
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

export function get_sig(signature: JSFunctionSignature, index: number): JSMarshalerType {
    mono_assert(signature, "Null signatures");
    return <any>signature + (index * JSMarshalerTypeSize) + JSMarshalerSignatureHeaderSize;
}

export function get_signature_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU32(sig);
}

export function get_signature_res_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 16);
}

export function get_signature_custom_code(sig: JSMarshalerType): CharPtr {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 8);
}

export function get_signature_custom_code_len(sig: JSMarshalerType): number {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 12);
}

export function get_signature_arg1_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 20);
}

export function get_signature_arg2_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 24);
}

export function get_signature_arg3_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null sig");
    return <any>getU32(<any>sig + 28);
}

export function get_signature_argument_count(signature: JSFunctionSignature): number {
    mono_assert(signature, "Null signatures");
    return <any>getI32(<any>signature + 4);
}

export function get_signature_version(signature: JSFunctionSignature): number {
    mono_assert(signature, "Null signatures");
    return <any>getI32(signature);
}

export function get_sig_type(sig: JSMarshalerType): MarshalerType {
    mono_assert(sig, "Null signatures");
    return <any>getU32(sig);
}

export function get_arg_type(arg: JSMarshalerArgument): MarshalerType {
    mono_assert(arg, "Null arg");
    const type = getU32(<any>arg + 12);
    return <any>type;
}

export function get_arg_element_type(arg: JSMarshalerArgument): MarshalerType {
    mono_assert(arg, "Null arg");
    const type = getU32(<any>arg + 4);
    return <any>type;
}

export function set_arg_type(arg: JSMarshalerArgument, type: MarshalerType): void {
    mono_assert(arg, "Null arg");
    setU32(<any>arg + 12, type);
}

export function set_arg_element_type(arg: JSMarshalerArgument, type: MarshalerType): void {
    mono_assert(arg, "Null arg");
    setU32(<any>arg + 4, type);
}

export function get_arg_b8(arg: JSMarshalerArgument): boolean {
    mono_assert(arg, "Null arg");
    return !!getU8(<any>arg);
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
    return getU32(<any>arg);
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

export function set_arg_b8(arg: JSMarshalerArgument, value: boolean): void {
    mono_assert(arg, "Null arg");
    mono_assert(typeof value === "boolean", () => `Value is not a Boolean: ${value} (${typeof (value)})`);
    setU8(<any>arg, value ? 1 : 0);
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
    setU32(<any>arg, <any>value);
}

export function set_arg_i52(arg: JSMarshalerArgument, value: number): void {
    mono_assert(arg, "Null arg");
    mono_assert(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
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
    return <any>getU32(<any>arg + 4);
}

export function set_js_handle(arg: JSMarshalerArgument, jsHandle: JSHandle): void {
    mono_assert(arg, "Null arg");
    setU32(<any>arg + 4, <any>jsHandle);
}

export function get_arg_gc_handle(arg: JSMarshalerArgument): GCHandle {
    mono_assert(arg, "Null arg");
    return <any>getU32(<any>arg + 4);
}

export function set_gc_handle(arg: JSMarshalerArgument, gcHandle: GCHandle): void {
    mono_assert(arg, "Null arg");
    setU32(<any>arg + 4, <any>gcHandle);
}

export function get_string_root(arg: JSMarshalerArgument): WasmRoot<MonoString> {
    mono_assert(arg, "Null arg");
    return mono_wasm_new_external_root<MonoString>(<any>arg);
}

export function get_arg_length(arg: JSMarshalerArgument): number {
    mono_assert(arg, "Null arg");
    return <any>getI32(<any>arg + 8);
}

export function set_arg_length(arg: JSMarshalerArgument, size: number): void {
    mono_assert(arg, "Null arg");
    setI32(<any>arg + 8, size);
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
    constructor(message: string) {
        super(message);
        this.superStack = Object.getOwnPropertyDescriptor(this, "stack"); // this works on Chrome
        Object.defineProperty(this, "stack", {
            get: this.getManageStack,
        });
    }

    getSuperStack() {
        if (this.superStack) {
            return this.superStack.value;
        }
        return super.stack; // this works on FF
    }

    getManageStack() {
        const gc_handle = (<any>this)[js_owned_gc_handle_symbol];
        if (gc_handle) {
            const managed_stack = runtimeHelpers.javaScriptExports.get_managed_stack_trace(gc_handle);
            if (managed_stack) {
                return managed_stack + "\n" + this.getSuperStack();
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
    return <any>getU32(<any>sig + 8);
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
        mono_assert(!this.isDisposed, "ObjectDisposedException");
        const targetView = this._unsafe_create_view();
        mono_assert(source && targetView && source.constructor === targetView.constructor, () => `Expected ${targetView.constructor}`);
        targetView.set(source, targetOffset);
        // TODO consider memory write barrier
    }

    copyTo(target: TypedArray, sourceOffset?: number): void {
        mono_assert(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        mono_assert(target && sourceView && target.constructor === sourceView.constructor, () => `Expected ${sourceView.constructor}`);
        const trimmedSource = sourceView.subarray(sourceOffset);
        // TODO consider memory read barrier
        target.set(trimmedSource);
    }

    slice(start?: number, end?: number): TypedArray {
        mono_assert(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        // TODO consider memory read barrier
        return sourceView.slice(start, end);
    }

    get length(): number {
        mono_assert(!this.isDisposed, "ObjectDisposedException");
        return this._length;
    }

    get byteLength(): number {
        mono_assert(!this.isDisposed, "ObjectDisposedException");
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
