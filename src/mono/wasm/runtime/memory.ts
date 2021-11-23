import { Module } from "./imports";

const _temp_mallocs: Array<Array<VoidPtr> | null> = [];

export function temp_malloc(size: number): VoidPtr {
    if (!_temp_mallocs || !_temp_mallocs.length)
        throw new Error("No temp frames have been created at this point");

    const frame = _temp_mallocs[_temp_mallocs.length - 1] || [];
    const result = Module._malloc(size);
    frame.push(result);
    _temp_mallocs[_temp_mallocs.length - 1] = frame;
    return result;
}

export function _create_temp_frame(): void {
    _temp_mallocs.push(null);
}

export function _release_temp_frame(): void {
    if (!_temp_mallocs.length)
        throw new Error("No temp frames have been created at this point");

    const frame = _temp_mallocs.pop();
    if (!frame)
        return;

    for (let i = 0, l = frame.length; i < l; i++)
        Module._free(frame[i]);
}

type _MemOffset = number | VoidPtr | NativePointer;

export function setU8(offset: _MemOffset, value: number): void {
    Module.HEAPU8[<any>offset] = value;
}

export function setU16(offset: _MemOffset, value: number): void {
    Module.HEAPU16[<any>offset >>> 1] = value;
}

export function setU32(offset: _MemOffset, value: number): void {
    Module.HEAPU32[<any>offset >>> 2] = value;
}

export function setI8(offset: _MemOffset, value: number): void {
    Module.HEAP8[<any>offset] = value;
}

export function setI16(offset: _MemOffset, value: number): void {
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setI32(offset: _MemOffset, value: number): void {
    Module.HEAP32[<any>offset >>> 2] = value;
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
