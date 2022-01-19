import { Module } from "./imports";
import { VoidPtr, NativePointer, ManagedPointer } from "./types/emscripten";

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

type DotnetMemOffset = number | NativePointer;
type DotnetMemValue = number | NativePointer | ManagedPointer;

export function setU8(offset: DotnetMemOffset, value: number): void {
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: DotnetMemOffset, value: number): void {
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32 (offset: DotnetMemOffset, value: DotnetMemValue) : void {
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setI8(offset: DotnetMemOffset, value: number): void {
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: DotnetMemOffset, value: number): void {
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32(offset: DotnetMemOffset, value: number): void {
    Module.HEAP32[<any>offset >>> 2] = value;
}

// NOTE: Accepts a number, not a BigInt, so values over Number.MAX_SAFE_INTEGER will be corrupted
export function setI64(offset: DotnetMemOffset, value: number): void {
    Module.setValue(<VoidPtr><any>offset, value, "i64");
}

export function setF32(offset: DotnetMemOffset, value: number): void {
    Module.HEAPF32[<any>offset >>> 2] = value;
}

export function setF64(offset: DotnetMemOffset, value: number): void {
    Module.HEAPF64[<any>offset >>> 3] = value;
}


export function getU8(offset: DotnetMemOffset): number {
    return Module.HEAPU8[<any>offset];
}

export function getU16(offset: DotnetMemOffset): number {
    return Module.HEAPU16[<any>offset >>> 1];
}

export function getU32(offset: DotnetMemOffset): number {
    return Module.HEAPU32[<any>offset >>> 2];
}

export function getI8(offset: DotnetMemOffset): number {
    return Module.HEAP8[<any>offset];
}

export function getI16(offset: DotnetMemOffset): number {
    return Module.HEAP16[<any>offset >>> 1];
}

export function getI32(offset: DotnetMemOffset): number {
    return Module.HEAP32[<any>offset >>> 2];
}

// NOTE: Returns a number, not a BigInt. This means values over Number.MAX_SAFE_INTEGER will be corrupted
export function getI64(offset: DotnetMemOffset): number {
    return Module.getValue(<number><any>offset, "i64");
}

export function getF32(offset: DotnetMemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getF64(offset: DotnetMemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}
