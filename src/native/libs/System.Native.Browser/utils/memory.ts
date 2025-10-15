// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { CharPtr, MemOffset, NumberOrPointer, VoidPtr } from "../types";
import { Module, dotnetAssert, dotnetLogger } from "./cross-module";

const max_int64_big = BigInt("9223372036854775807");
const min_int64_big = BigInt("-9223372036854775808");
const sharedArrayBufferDefined = typeof SharedArrayBuffer !== "undefined";

export function assertIntInRange(value: Number, min: Number, max: Number) {
    dotnetAssert.check(Number.isSafeInteger(value), () => `Value is not an integer: ${value} (${typeof (value)})`);
    dotnetAssert.check(value >= min && value <= max, () => `Overflow: value ${value} is out of ${min} ${max} range`);
}

/** note: boolean is 8 bits not 32 bits when inside a structure or array */
export function setHeapB32(offset: MemOffset, value: number | boolean): void {
    const boolValue = !!value;
    if (typeof (value) === "number")
        assertIntInRange(value, 0, 1);
    Module.HEAP32[<any>offset >>> 2] = boolValue ? 1 : 0;
}

export function setHeapB8(offset: MemOffset, value: number | boolean): void {
    const boolValue = !!value;
    if (typeof (value) === "number")
        assertIntInRange(value, 0, 1);
    Module.HEAPU8[<any>offset] = boolValue ? 1 : 0;
}

export function setHeapU8(offset: MemOffset, value: number): void {
    assertIntInRange(value, 0, 0xFF);
    Module.HEAPU8[<any>offset] = value;
}

export function setHeapU16(offset: MemOffset, value: number): void {
    assertIntInRange(value, 0, 0xFFFF);
    Module.HEAPU16[<any>offset >>> 1] = value;
}

// does not check for growable heap
export function setHeapU16_local(localView: Uint16Array, offset: MemOffset, value: number): void {
    assertIntInRange(value, 0, 0xFFFF);
    localView[<any>offset >>> 1] = value;
}

// does not check for overflow nor growable heap
export function setHeapU16_unchecked(offset: MemOffset, value: number): void {
    Module.HEAPU16[<any>offset >>> 1] = value;
}

// does not check for overflow nor growable heap
export function setHeapU32_unchecked(offset: MemOffset, value: NumberOrPointer): void {
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setHeapU32(offset: MemOffset, value: NumberOrPointer): void {
    assertIntInRange(<any>value, 0, 0xFFFF_FFFF);
    Module.HEAPU32[<any>offset >>> 2] = <number><any>value;
}

export function setHeapI8(offset: MemOffset, value: number): void {
    assertIntInRange(value, -0x80, 0x7F);
    Module.HEAP8[<any>offset] = value;
}

export function setHeapI16(offset: MemOffset, value: number): void {
    assertIntInRange(value, -0x8000, 0x7FFF);
    Module.HEAP16[<any>offset >>> 1] = value;
}

export function setHeapI32_unchecked(offset: MemOffset, value: number): void {
    Module.HEAP32[<any>offset >>> 2] = value;
}

export function setHeapI32(offset: MemOffset, value: number): void {
    assertIntInRange(<any>value, -0x8000_0000, 0x7FFF_FFFF);
    Module.HEAP32[<any>offset >>> 2] = value;
}

/**
 * Throws for values which are not 52 bit integer. See Number.isSafeInteger()
 */
export function setHeapI52(offset: MemOffset, value: number): void {
    dotnetAssert.check(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    throw new Error("WASM-TODO");
}

/**
 * Throws for values which are not 52 bit integer or are negative. See Number.isSafeInteger().
 */
export function setHeapU52(offset: MemOffset, value: number): void {
    dotnetAssert.check(Number.isSafeInteger(value), () => `Value is not a safe integer: ${value} (${typeof (value)})`);
    dotnetAssert.check(value >= 0, "Can't convert negative Number into UInt64");
    throw new Error("WASM-TODO");
}

export function setHeapI64Big(offset: MemOffset, value: bigint): void {
    dotnetAssert.check(typeof value === "bigint", () => `Value is not an bigint: ${value} (${typeof (value)})`);
    dotnetAssert.check(value >= min_int64_big && value <= max_int64_big, () => `Overflow: value ${value} is out of ${min_int64_big} ${max_int64_big} range`);

    Module.HEAP64[<any>offset >>> 3] = value;
}

export function setHeapF32(offset: MemOffset, value: number): void {
    dotnetAssert.check(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
    Module.HEAPF32[<any>offset >>> 2] = value;
}

export function setHeapF64(offset: MemOffset, value: number): void {
    dotnetAssert.check(typeof value === "number", () => `Value is not a Number: ${value} (${typeof (value)})`);
    Module.HEAPF64[<any>offset >>> 3] = value;
}

export function getHeapB32(offset: MemOffset): boolean {
    const value = (Module.HEAPU32[<any>offset >>> 2]);
    if (value > 1 && !(getHeapB32 as any).warnDirtyBool) {
        (getHeapB32 as any).warnDirtyBool = true;
        dotnetLogger.warn(`getB32: value at ${offset} is not a boolean, but a number: ${value}`);
    }
    return !!value;
}

export function getHeapB8(offset: MemOffset): boolean {
    return !!(Module.HEAPU8[<any>offset]);
}

export function getHeapU8(offset: MemOffset): number {
    return Module.HEAPU8[<any>offset];
}

export function getHeapU16(offset: MemOffset): number {
    return Module.HEAPU16[<any>offset >>> 1];
}

// does not check for growable heap
export function getHeapU16_local(localView: Uint16Array, offset: MemOffset): number {
    return localView[<any>offset >>> 1];
}

export function getHeapU32(offset: MemOffset): number {
    return Module.HEAPU32[<any>offset >>> 2];
}

// does not check for growable heap
export function getHeapU32_local(localView: Uint32Array, offset: MemOffset): number {
    return localView[<any>offset >>> 2];
}

export function getHeapI8(offset: MemOffset): number {
    return Module.HEAP8[<any>offset];
}

export function getHeapI16(offset: MemOffset): number {
    return Module.HEAP16[<any>offset >>> 1];
}

// does not check for growable heap
export function getHeapI16_local(localView: Int16Array, offset: MemOffset): number {
    return localView[<any>offset >>> 1];
}

export function getHeapI32(offset: MemOffset): number {
    return Module.HEAP32[<any>offset >>> 2];
}

// does not check for growable heap
export function getHeapI32_local(localView: Int32Array, offset: MemOffset): number {
    return localView[<any>offset >>> 2];
}

/**
 * Throws for Number.MIN_SAFE_INTEGER > value > Number.MAX_SAFE_INTEGER
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function getHeapI52(offset: MemOffset): number {
    throw new Error("WASM-TODO");
}

/**
 * Throws for 0 > value > Number.MAX_SAFE_INTEGER
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function getHeapU52(offset: MemOffset): number {
    throw new Error("WASM-TODO");
}

export function getHeapI64Big(offset: MemOffset): bigint {
    return Module.HEAP64[<any>offset >>> 3];
}

export function getHeapF32(offset: MemOffset): number {
    return Module.HEAPF32[<any>offset >>> 2];
}

export function getHeapF64(offset: MemOffset): number {
    return Module.HEAPF64[<any>offset >>> 3];
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewI8(): Int8Array {
    return Module.HEAP8;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewI16(): Int16Array {
    return Module.HEAP16;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewI32(): Int32Array {
    return Module.HEAP32;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewI64Big(): BigInt64Array {
    return Module.HEAP64;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewU8(): Uint8Array {
    return Module.HEAPU8;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewU16(): Uint16Array {
    return Module.HEAPU16;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewU32(): Uint32Array {
    return Module.HEAPU32;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewF32(): Float32Array {
    return Module.HEAPF32;
}

// returns memory view which is valid within current synchronous call stack
export function localHeapViewF64(): Float64Array {
    return Module.HEAPF64;
}

export function copyBytes(srcPtr: VoidPtr, dstPtr: VoidPtr, bytes: number): void {
    const heap = localHeapViewU8();
    heap.copyWithin(dstPtr as any, srcPtr as any, srcPtr as any + bytes);
}

export function isSharedArrayBuffer(buffer: any): buffer is SharedArrayBuffer {
    // BEWARE: In some cases, `instanceof SharedArrayBuffer` returns false even though buffer is an SAB.
    // Patch adapted from https://github.com/emscripten-core/emscripten/pull/16994
    // See also https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol/toStringTag
    return sharedArrayBufferDefined && buffer[Symbol.toStringTag] === "SharedArrayBuffer";
}

// does not check for growable heap
export function getU16Local(localView: Uint16Array, offset: MemOffset): number {
    return localView[<any>offset >>> 1];
}

// does not check for growable heap
export function setU16Local(localView: Uint16Array, offset: MemOffset, value: number): void {
    assertIntInRange(value, 0, 0xFFFF);
    localView[<any>offset >>> 1] = value;
}

// When threading is enabled, TextDecoder does not accept a view of a
// SharedArrayBuffer, we must make a copy of the array first.
// See https://github.com/whatwg/encoding/issues/172
export function viewOrCopy(view: Uint8Array, start: CharPtr, end: CharPtr): Uint8Array {
    // this condition should be eliminated by rollup on non-threading builds
    const needsCopy = isSharedArrayBuffer(view.buffer);
    return needsCopy
        ? view.slice(<any>start, <any>end)
        : view.subarray(<any>start, <any>end);
}

export function zeroRegion(byteOffset: VoidPtr, sizeBytes: number): void {
    localHeapViewU8().fill(0, <any>byteOffset, <any>byteOffset + sizeBytes);
}
