import { Module, runtimeHelpers } from "./imports";
import { mono_assert } from "./types";
import { VoidPtr, NativePointer, ManagedPointer } from "./types/emscripten";
import * as cuint64 from "./cuint64";
import cwraps, { I52Error } from "./cwraps";

const alloca_stack: Array<VoidPtr> = [];
const alloca_buffer_size = 32 * 1024;
let alloca_base: VoidPtr, alloca_offset: VoidPtr, alloca_limit: VoidPtr;
let HEAPI64: BigInt64Array = <any>null;

function _ensure_allocated(): void {
    if (alloca_base)
        return;
    alloca_base = Module._malloc(alloca_buffer_size);
    alloca_offset = alloca_base;
    alloca_limit = <VoidPtr>(<any>alloca_base + alloca_buffer_size);
}

const is_bigint_supported = typeof BigInt !== "undefined" && typeof BigInt64Array !== "undefined";

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

type _MemOffset = number | VoidPtr | NativePointer | ManagedPointer;
type _NumberOrPointer = number | VoidPtr | NativePointer | ManagedPointer;

function assert_int_in_range(value: Number, min: Number, max: Number) {
    mono_assert(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    mono_assert(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
}

export function _zero_region(byteOffset: VoidPtr, sizeBytes: number): void {
    if (((<any>byteOffset % 4) === 0) && ((sizeBytes % 4) === 0))
        Module.HEAP32.fill(0, <any>byteOffset >>> 2, sizeBytes >>> 2);
    else
        Module.HEAP8.fill(0, <any>byteOffset, sizeBytes);
}

export function setB32(offset: _MemOffset, value: number | boolean): void {
    const boolValue = !!value;
    if (typeof (value) === "number")
        assert_int_in_range(value, 0, 1);
    Module.HEAP32[<any>offset >>> 2] = boolValue ? 1 : 0;
}

export function setU8(offset: _MemOffset, value: number): void {
    assert_int_in_range(value, 0, 0xFF);
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: _MemOffset, value: number): void {
    assert_int_in_range(value, 0, 0xFFFF);
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32_unchecked(offset: _MemOffset, value: _NumberOrPointer): void {
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setU32(offset: _MemOffset, value: _NumberOrPointer): void {
    assert_int_in_range(<any>value, 0, 0xFFFF_FFFF);
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setI8(offset: _MemOffset, value: number): void {
    assert_int_in_range(value, -0x80, 0x7F);
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: _MemOffset, value: number): void {
    assert_int_in_range(value, -0x8000, 0x7FFF);
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32_unchecked(offset: _MemOffset, value: number): void {
    Module.HEAP32[<any>offset >>> 2] = value;
}

export function setI32(offset: _MemOffset, value: number): void {
    assert_int_in_range(<any>value, -0x8000_0000, 0x7FFF_FFFF);
    Module.HEAP32[<any>offset >>> 2] = value;
}

function autoThrowI52 (error: I52Error) {
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
export function setI52(offset: _MemOffset, value: number): void {
    mono_assert(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    const error = cwraps.mono_wasm_f64_to_i52(<any>offset, value);
    autoThrowI52(error);
}

/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
export function setU52(offset: _MemOffset, value: number): void {
    mono_assert(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    mono_assert(value >= 0, "Can't convert negative Number into UInt64");
    const error = cwraps.mono_wasm_f64_to_u52(<any>offset, value);
    autoThrowI52(error);
}

export function setI64Big(offset: _MemOffset, value: bigint): void {
    mono_assert(is_bigint_supported, "BigInt is not supported.");
    HEAPI64[<any>offset >>> 3] = value;
}

export function setF32(offset: _MemOffset, value: number): void {
    Module.HEAPF32[<any>offset >>> 2] = value;
}

export function setF64(offset: _MemOffset, value: number): void {
    Module.HEAPF64[<any>offset >>> 3] = value;
}


export function getB32(offset: _MemOffset): boolean {
    return !!(Module.HEAP32[<any>offset >>> 2]);
}

export function getU8(offset: _MemOffset): number {
    return Module.HEAPU8[<any>offset];
}

export function getU16(offset: _MemOffset): number {
    return Module.HEAPU16[<any>offset >>> 1];
}

export function getU32(offset: _MemOffset): number {
    return Module.HEAPU32[<any>offset >>> 2];
}

export function getI8(offset: _MemOffset): number {
    return Module.HEAP8[<any>offset];
}

export function getI16(offset: _MemOffset): number {
    return Module.HEAP16[<any>offset >>> 1];
}

export function getI32(offset: _MemOffset): number {
    return Module.HEAP32[<any>offset >>> 2];
}

/**
 * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
export function getI52(offset: _MemOffset): number {
    const result = cwraps.mono_wasm_i52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
    const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
    autoThrowI52(error);
    return result;
}

/**
 * Throws for 0 > value > Number.MAX_SAFE_INTEGER
 */
export function getU52(offset: _MemOffset): number {
    const result = cwraps.mono_wasm_u52_to_f64(<any>offset, runtimeHelpers._i52_error_scratch_buffer);
    const error = getI32(runtimeHelpers._i52_error_scratch_buffer);
    autoThrowI52(error);
    return result;
}

export function getI64Big(offset: _MemOffset): bigint {
    mono_assert(is_bigint_supported, "BigInt is not supported.");
    return HEAPI64[<any>offset >>> 3];
}

export function getF32(offset: _MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: _MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}

export function afterUpdateGlobalBufferAndViews(buffer: Buffer): void {
    if (is_bigint_supported) {
        HEAPI64 = new BigInt64Array(buffer);
    }
}

export function getCU64(offset: _MemOffset): cuint64.CUInt64 {
    const lo = getU32(offset);
    const hi = getU32(<any>offset + 4);
    return cuint64.pack32(lo, hi);
}

export function setCU64(offset: _MemOffset, value: cuint64.CUInt64): void {
    const [lo, hi] = cuint64.unpack32(value);
    setU32_unchecked(offset, lo);
    setU32_unchecked(<any>offset + 4, hi);
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

