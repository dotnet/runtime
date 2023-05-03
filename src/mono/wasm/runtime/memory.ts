// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import monoWasmThreads from "consts:monoWasmThreads";
import { mono_assert, MemOffset, NumberOrPointer } from "./types";
import { VoidPtr, CharPtr } from "./types/emscripten";
import cwraps, { I52Error } from "./cwraps";
import { Module, runtimeHelpers } from "./globals";

const alloca_stack: Array<VoidPtr> = [];
const alloca_buffer_size = 32 * 1024;
let alloca_base: VoidPtr, alloca_offset: VoidPtr, alloca_limit: VoidPtr;

function _ensure_allocated(): void {
    if (alloca_base)
        return;
    alloca_base = Module._malloc(alloca_buffer_size);
    alloca_offset = alloca_base;
    alloca_limit = <VoidPtr>(<any>alloca_base + alloca_buffer_size);
}

const max_int64_big = BigInt("9223372036854775807");
const min_int64_big = BigInt("-9223372036854775808");

export function temp_malloc(size: number): VoidPtr {
    _ensure_allocated();
    if (!alloca_stack.length)
        throw new Error("No temp frames have been created at this point");

    const result = alloca_offset;
    alloca_offset += <any>size;
    if (alloca_offset >= alloca_limit)
        throw new Error("Out of temp storage space");
    return result;
}

export function _create_temp_frame(): void {
    _ensure_allocated();
    alloca_stack.push(alloca_offset);
}

export function _release_temp_frame(): void {
    if (!alloca_stack.length)
        throw new Error("No temp frames have been created at this point");

    alloca_offset = <VoidPtr>alloca_stack.pop();
}


function assert_int_in_range(value: Number, min: Number, max: Number) {
    mono_assert(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    mono_assert(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
}

export function _zero_region(byteOffset: VoidPtr, sizeBytes: number): void {
    Module.HEAP8.fill(0, <any>byteOffset, <any>byteOffset + sizeBytes);
}

export function setB32(offset: MemOffset, value: number | boolean): void {
    const boolValue = !!value;
    if (typeof (value) === "number")
        assert_int_in_range(value, 0, 1);
    Module.HEAP32[<any>offset >>> 2] = boolValue ? 1 : 0;
}

export function setU8(offset: MemOffset, value: number): void {
    assert_int_in_range(value, 0, 0xFF);
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: MemOffset, value: number): void {
    assert_int_in_range(value, 0, 0xFFFF);
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU16_unchecked(offset: MemOffset, value: number): void {
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32_unchecked(offset: MemOffset, value: NumberOrPointer): void {
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setU32(offset: MemOffset, value: NumberOrPointer): void {
    assert_int_in_range(<any>value, 0, 0xFFFF_FFFF);
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setI8(offset: MemOffset, value: number): void {
    assert_int_in_range(value, -0x80, 0x7F);
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: MemOffset, value: number): void {
    assert_int_in_range(value, -0x8000, 0x7FFF);
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32_unchecked(offset: MemOffset, value: number): void {
    Module.HEAP32[<any>offset >>> 2] = value;
}

export function setI32(offset: MemOffset, value: number): void {
    assert_int_in_range(<any>value, -0x8000_0000, 0x7FFF_FFFF);
    Module.HEAP32[<any>offset >>> 2] = value;
}

function autoThrowI52(error: I52Error) {
    if (error === I52Error.NONE)
        return;

    switch (error) {
        case I52Error.NON_INTEGRAL:
            throw new Error("value was not an integer");
        case I52Error.OUT_OF_RANGE:
            throw new Error("value out of range");
        default:
            throw new Error("unknown internal error");
    }
}

/**
 * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
 */
export function setI52(offset: MemOffset, value: number): void {
    mono_assert(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    const error = cwraps.mono_wasm_f64_to_i52(<any>offset, value);
    autoThrowI52(error);
}

/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
export function setU52(offset: MemOffset, value: number): void {
    mono_assert(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    mono_assert(value >= 0, "Can't convert negative Number into UInt64");
    const error = cwraps.mono_wasm_f64_to_u52(<any>offset, value);
    autoThrowI52(error);
}

export function setI64Big(offset: MemOffset, value: bigint): void {
    mono_assert(typeof value === "bigint", () => `Value is not an bigint: ${value} (${typeof (value)})`);
    mono_assert(value >= min_int64_big && value <= max_int64_big, () => `Overflow: value ${value} is out of ${min_int64_big} ${max_int64_big} range`);

    Module.HEAP64[<any>offset >>> 3] = value;
}

export function setF32(offset: MemOffset, value: number): void {
    mono_assert(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
    Module.HEAPF32[<any>offset >>> 2] = value;
}

export function setF64(offset: MemOffset, value: number): void {
    mono_assert(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
    Module.HEAPF64[<any>offset >>> 3] = value;
}


export function getB32(offset: MemOffset): boolean {
    return !!(Module.HEAP32[<any>offset >>> 2]);
}

export function getU8(offset: MemOffset): number {
    return Module.HEAPU8[<any>offset];
}

export function getU16(offset: MemOffset): number {
    return Module.HEAPU16[<any>offset >>> 1];
}

export function getU32(offset: MemOffset): number {
    return Module.HEAPU32[<any>offset >>> 2];
}

export function getI32_unaligned(offset: MemOffset): number {
    return cwraps.mono_wasm_get_i32_unaligned(<any>offset);
}

export function getU32_unaligned(offset: MemOffset): number {
    return cwraps.mono_wasm_get_i32_unaligned(<any>offset) >>> 0;
}

export function getF32_unaligned(offset: MemOffset): number {
    return cwraps.mono_wasm_get_f32_unaligned(<any>offset);
}

export function getF64_unaligned(offset: MemOffset): number {
    return cwraps.mono_wasm_get_f64_unaligned(<any>offset);
}

export function getI8(offset: MemOffset): number {
    return Module.HEAP8[<any>offset];
}

export function getI16(offset: MemOffset): number {
    return Module.HEAP16[<any>offset >>> 1];
}

export function getI32(offset: MemOffset): number {
    return Module.HEAP32[<any>offset >>> 2];
}

/**
 * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
export function getI52(offset: MemOffset): number {
    const result = cwraps.mono_wasm_i52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
    const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
    autoThrowI52(error);
    return result;
}

/**
 * Throws for 0 > value > Number.MAX_SAFE_INTEGER
 */
export function getU52(offset: MemOffset): number {
    const result = cwraps.mono_wasm_u52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
    const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
    autoThrowI52(error);
    return result;
}

export function getI64Big(offset: MemOffset): bigint {
    return Module.HEAP64[<any>offset >>> 3];
}

export function getF32(offset: MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}

/// Allocates a new buffer of the given size on the Emscripten stack and passes a pointer to it to the callback.
/// Returns the result of the callback.  As usual with stack allocations, the buffer is freed when the callback returns.
/// Do not attempt to use the stack pointer after the callback is finished.
export function withStackAlloc<TResult>(bytesWanted: number, f: (ptr: VoidPtr) => TResult): TResult;
export function withStackAlloc<T1, TResult>(bytesWanted: number, f: (ptr: VoidPtr, ud1: T1) => TResult, ud1: T1): TResult;
export function withStackAlloc<T1, T2, TResult>(bytesWanted: number, f: (ptr: VoidPtr, ud1: T1, ud2: T2) => TResult, ud1: T1, ud2: T2): TResult;
export function withStackAlloc<T1, T2, T3, TResult>(bytesWanted: number, f: (ptr: VoidPtr, ud1: T1, ud2: T2, ud3: T3) => TResult, ud1: T1, ud2: T2, ud3: T3): TResult;
export function withStackAlloc<T1, T2, T3, TResult>(bytesWanted: number, f: (ptr: VoidPtr, ud1?: T1, ud2?: T2, ud3?: T3) => TResult, ud1?: T1, ud2?: T2, ud3?: T3): TResult {
    const sp = Module.stackSave();
    const ptr = Module.stackAlloc(bytesWanted);
    try {
        return f(ptr, ud1, ud2, ud3);
    } finally {
        Module.stackRestore(sp);
    }
}

// @bytes must be a typed array. space is allocated for it in the native heap
//  and it is copied to that location. returns the address of the allocation.
export function mono_wasm_load_bytes_into_heap(bytes: Uint8Array): VoidPtr {
    const memoryOffset = Module._malloc(bytes.length);
    const heapBytes = new Uint8Array(Module.HEAPU8.buffer, <any>memoryOffset, bytes.length);
    heapBytes.set(bytes);
    return memoryOffset;
}

export function getEnv(name: string): string | null {
    let charPtr: CharPtr = <any>0;
    try {
        charPtr = cwraps.mono_wasm_getenv(name);
        if (<any>charPtr === 0)
            return null;
        else return Module.UTF8ToString(charPtr);
    } finally {
        if (charPtr) Module._free(<any>charPtr);
    }
}

const BuiltinAtomics = globalThis.Atomics;

export const Atomics = monoWasmThreads ? {
    storeI32(offset: MemOffset, value: number): void {

        BuiltinAtomics.store(Module.HEAP32, <any>offset >>> 2, value);
    },
    notifyI32(offset: MemOffset, count: number): void {
        BuiltinAtomics.notify(Module.HEAP32, <any>offset >>> 2, count);
    }
} : {
    storeI32: setI32,
    notifyI32: () => { /*empty*/ }
};
