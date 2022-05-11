// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// Define a type that can hold a 64 bit integer value from Emscripten.
/// Import this module with 'import * as cuint64 from "./cuint64";'
/// and 'import type { CUInt64 } from './cuint64';
export type CUInt64 = readonly [number, number];

export function toBigInt (x: CUInt64): bigint {
    return BigInt(x[0]) | BigInt(x[1]) << BigInt(32);
}

export function fromBigInt (x: bigint): CUInt64 {
    if (x < BigInt(0))
        throw new Error(`${x} is not a valid 64 bit integer`);
    if (x > BigInt(0xFFFFFFFFFFFFFFFF))
        throw new Error(`${x} is not a valid 64 bit integer`);
    const low = Number(x & BigInt(0xFFFFFFFF));
    const high = Number(x >> BigInt(32));
    return [low, high];
}

export function dangerousToNumber (x: CUInt64): number {
    return x[0] | x[1] << 32;
}

export function fromNumber (x: number): CUInt64 {
    if (x < 0)
        throw new Error(`${x} is not a valid 64 bit integer`);
    if ((x >> 32) > 0xFFFFFFFF)
        throw new Error(`${x} is not a valid 64 bit integer`);
    if (Math.trunc(x) != x)
        throw new Error(`${x} is not a valid 64 bit integer`);
    return [x & 0xFFFFFFFF, x >> 32];
}

export function pack32 (lo: number, hi: number): CUInt64 {
    return [lo, hi];
}

export function unpack32 (x: CUInt64): [number, number] {
    return [x[0], x[1]];
}

export const zero: CUInt64 = [0, 0];



