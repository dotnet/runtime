import { Module } from "./imports";
import { assert } from "./types";
import { VoidPtr, NativePointer, ManagedPointer } from "./types/emscripten";

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

function is_bingint_supported() {
    return typeof BigInt !== "undefined" && typeof BigInt64Array !== "undefined";
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

/**
 * Throws for values which are not integer. See Number.isInteger()
 */
export function setI52(offset: _MemOffset, value: number): void {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    assert(Number.isSafeInteger(value), "Int64 value out of JavaScript Number safe integer range");
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

export function setI64Big(offset: _MemOffset, value: bigint): void {
    assert(is_bingint_supported(), "BigInt is not supported.");
    HEAPI64[<any>offset >>> 3] = value;
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

/**
 * Throws for  Number.MIN_SAFE_INTEGER < value < Number.MAX_SAFE_INTEGER
 */
export function getI52(offset: _MemOffset): number {
    // 52 bits = 0x1F_FFFF_FFFF_FFFF
    const hi = Module.HEAPU32[1 + (<any>offset >>> 2)];
    const lo = Module.HEAPU32[<any>offset >>> 2];
    const sign = hi & 0x8000_0000;
    const exp = hi & 0x7FE0_0000;
    if (sign) {
        assert(exp === 0x7FE0_0000, "Int64 value out of JavaScript Number safe integer range");
        const nhi = (hi & 0x000F_FFFF) ^ 0x000F_FFFF;
        const nlo = lo ^ 0xFFFF_FFFF;
        return -1 - ((nhi * 0x1_0000_0000) + nlo);
    }
    else {
        assert(exp === 0, "Int64 value out of JavaScript Number safe integer range");
        return (hi * 0x1_0000_0000) + lo;
    }
}

export function getI64Big(offset: _MemOffset): bigint {
    assert(is_bingint_supported(), "BigInt is not supported.");
    return HEAPI64[<any>offset >>> 3];
}

export function getF32(offset: _MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: _MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}

export function afterUpdateGlobalBufferAndViews(buffer: Buffer): void {
    if (is_bingint_supported()) {
        HEAPI64 = new BigInt64Array(buffer);
    }
}