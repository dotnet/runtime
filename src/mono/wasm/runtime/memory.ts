import { Module } from "./imports";
import { VoidPtr, NativePointer, ManagedPointer } from "./types/emscripten";
import * as cuint64 from "./cuint64";

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

export function setU8(offset: _MemOffset, value: number): void {
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: _MemOffset, value: number): void {
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32(offset: _MemOffset, value: _NumberOrPointer): void {
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setI8(offset: _MemOffset, value: number): void {
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: _MemOffset, value: number): void {
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32(offset: _MemOffset, value: _NumberOrPointer): void {
    Module.HEAP32[<any>offset >>> 2] = <number><any>value;
}

// NOTE: Accepts a number, not a BigInt, so values over Number.MAX_SAFE_INTEGER will be corrupted
export function setI64(offset: _MemOffset, value: number): void {
    Module.setValue(<VoidPtr><any>offset, value, "i64");
}

export function setF32(offset: _MemOffset, value: number): void {
    Module.HEAPF32[<any>offset >>> 2] = value;
}

export function setF64(offset: _MemOffset, value: number): void {
    Module.HEAPF64[<any>offset >>> 3] = value;
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

// NOTE: Returns a number, not a BigInt. This means values over Number.MAX_SAFE_INTEGER will be corrupted
export function getI64(offset: _MemOffset): number {
    return Module.getValue(<number><any>offset, "i64");
}

export function getF32(offset: _MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: _MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
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

