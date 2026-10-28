// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./cross-module";
import type { VoidPtr } from "./types";
import { MemoryViewManager } from "./shared-signaling";
import { MarshalerType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";

export type WorkerValue = number | string | null;
export type WorkerValueType = MarshalerType.Int32 | MarshalerType.String;
const ownedStrings = new Map<number, VoidPtr>();

export function validateWorkerValue(value: unknown, type: WorkerValueType): WorkerValue {
    if (value === null || value === undefined) return null;
    if (type === MarshalerType.String) {
        if (typeof value !== "string") throw new Error("withWorker(): value must be a String");
        return value;
    }
    if (type !== MarshalerType.Int32 || typeof value !== "number" || !Number.isInteger(value) || value < -0x8000_0000 || value > 0x7fff_ffff) {
        throw new Error("withWorker(): value must be an Int32");
    }
    return value;
}

export function allocateWorkerString(length: number): number {
    if (!Number.isInteger(length) || length < 0 || length > 0x7fff_ffff) {
        throw new Error("withWorker(): invalid string length");
    }
    if (length === 0) return 0;
    const pointer = Module._malloc(length * 2);
    const address = Number(pointer);
    if (!address) throw new Error("withWorker(): unable to allocate string transport");
    ownedStrings.set(address, pointer);
    return address;
}

export function freeWorkerString(pointer: number): void {
    if (!pointer) return;
    const allocation = ownedStrings.get(pointer);
    if (!allocation) throw new Error("withWorker(): string allocation is not owned");
    ownedStrings.delete(pointer);
    Module._free(allocation);
}

function stringView(memory: MemoryViewManager, pointer: number, length: number): Uint16Array {
    const views = memory.refreshIfNeeded();
    if (!Number.isInteger(length) || length < 0 || length > 0x7fff_ffff
        || pointer < 0 || pointer % 2 !== 0 || (length > 0 && !pointer)
        || pointer + length * 2 > views.u8.byteLength) {
        throw new Error("withWorker(): invalid string transport range");
    }
    return views.u16.subarray(pointer / 2, pointer / 2 + length);
}

export function writeWorkerString(memory: MemoryViewManager, pointer: number, value: string): void {
    const target = stringView(memory, pointer, value.length);
    for (let i = 0; i < value.length; i++) target[i] = value.charCodeAt(i);
}

export function readWorkerString(memory: MemoryViewManager, pointer: number, length: number): string | null {
    if (length === -1) return null;
    const source = stringView(memory, pointer, length);
    let result = "";
    // Decode code units, not Unicode scalar values: preserve embedded NULs and lone surrogates.
    for (let i = 0; i < source.length; i += 8192) {
        result += String.fromCharCode(...source.subarray(i, Math.min(i + 8192, source.length)));
    }
    return result;
}
