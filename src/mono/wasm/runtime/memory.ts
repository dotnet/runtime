import { Module } from "./imports";
import { mono_assert } from "./types";
import { VoidPtr, NativePointer, ManagedPointer } from "./types/emscripten";
import * as cuint64 from "./cuint64";

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

const is_bingint_supported = typeof BigInt !== "undefined" && typeof BigInt64Array !== "undefined";

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

function is_int_in_range(value: Number, min: Number, max: Number) {
    mono_assert(typeof value === "number", () => `Value is not integer but ${typeof value}`);
    mono_assert(Number.isInteger(value), "Value is not integer but float");
    mono_assert(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
}

export function setB32(offset: _MemOffset, value: number | boolean): void {
    mono_assert(typeof value === "boolean", () => `Value is not boolean but ${typeof value}`);
    Module.HEAP32[<any>offset >>> 2] = <any>!!value;
}

export function setU8(offset: _MemOffset, value: number): void {
    is_int_in_range(value, 0, 0xFF);
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: _MemOffset, value: number): void {
    is_int_in_range(value, 0, 0xFFFF);
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32(offset: _MemOffset, value: _NumberOrPointer): void {
    is_int_in_range(<any>value, 0, 0xFFFF_FFFF);
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setI8(offset: _MemOffset, value: number): void {
    is_int_in_range(value, -0x80, 0x7F);
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: _MemOffset, value: number): void {
    is_int_in_range(value, -0x8000, 0x7FFF);
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32(offset: _MemOffset, value: number): void {
    is_int_in_range(<any>value, -0x8000_0000, 0x7FFF_FFFF);
    Module.HEAP32[<any>offset >>> 2] = value;
}

/**
 * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
 */
export function setI52(offset: _MemOffset, value: number): void {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    mono_assert(!Number.isNaN(value), "Can't convert Number.Nan into Int64");
    mono_assert(Number.isSafeInteger(value), "Overflow: value out of Number.isSafeInteger range");
    let hi: number;
    let lo: number;
    if (value < 0) {
        value = -1 - value;
        hi = 0x8000_0000 + ((value >>> 32) ^ 0x001F_FFFF);
        lo = (value & 0xFFFF_FFFF) ^ 0xFFFF_FFFF;
    }
    else {
        hi = value >>> 32;
        lo = value & 0xFFFF_FFFF;
    }
    Module.HEAPU32[1 + <any>offset >>> 2] = hi;
    Module.HEAPU32[<any>offset >>> 2] = lo;
}

/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
export function setU52(offset: _MemOffset, value: number): void {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    mono_assert(!Number.isNaN(value), "Can't convert Number.Nan into UInt64");
    mono_assert(Number.isSafeInteger(value), "Overflow: value out of Number.isSafeInteger range");
    mono_assert(value >= 0, "Can't convert negative Number into UInt64");
    const hi = value >>> 32;
    const lo = value & 0xFFFF_FFFF;
    Module.HEAPU32[1 + <any>offset >>> 2] = hi;
    Module.HEAPU32[<any>offset >>> 2] = lo;
}

export function setI64Big(offset: _MemOffset, value: bigint): void {
    mono_assert(is_bingint_supported, "BigInt is not supported.");
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
 * Throws for  Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
export function getI52(offset: _MemOffset): number {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    const hi = Module.HEAPU32[1 + (<any>offset >>> 2)];
    const lo = Module.HEAPU32[<any>offset >>> 2];
    const sign = hi & 0x8000_0000;
    const exp = hi & 0x7FE0_0000;
    if (sign) {
        mono_assert(exp === 0x7FE0_0000, "Overflow: value out of Number.isSafeInteger range");
        const nhi = (hi & 0x000F_FFFF) ^ 0x000F_FFFF;
        const nlo = lo ^ 0xFFFF_FFFF;
        return -1 - ((nhi * 0x1_0000_0000) + nlo);
    }
    else {
        mono_assert(exp === 0, "Overflow: value out of Number.isSafeInteger range");
        return (hi * 0x1_0000_0000) + lo;
    }
}

/**
 * Throws for  Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
export function getU52(offset: _MemOffset): number {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    const hi = Module.HEAPU32[1 + (<any>offset >>> 2)];
    const lo = Module.HEAPU32[<any>offset >>> 2];
    const exp_sign = hi & 0xFFE0_0000;
    mono_assert(exp_sign === 0, "Overflow: value out of Number.isSafeInteger range");
    return (hi * 0x1_0000_0000) + lo;
}

export function getI64Big(offset: _MemOffset): bigint {
    mono_assert(is_bingint_supported, "BigInt is not supported.");
    return HEAPI64[<any>offset >>> 3];
}

export function getF32(offset: _MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: _MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}

export function afterUpdateGlobalBufferAndViews(buffer: Buffer): void {
    if (is_bingint_supported) {
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
    setU32(offset, lo);
    setU32(<any>offset + 4, hi);
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

