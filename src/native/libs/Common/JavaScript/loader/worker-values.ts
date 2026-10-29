// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./cross-module";
import type { VoidPtr } from "./types";
import {
    MemoryViewManager, argumentEntryOffset, RING_ENTRY_ARGUMENT_COUNT,
    ARGUMENT_ENTRY_TYPE, ARGUMENT_ENTRY_STRING, ARGUMENT_ENTRY_STRING_LENGTH,
} from "./shared-signaling";
import type { WorkerReturnType, WorkerValueType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
import { isWorkerValueType, JavaScriptMarshalerArgSize, JSMarshalerArgumentOffsets, MarshalerType, WorkerMaxArgumentCount } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
export type { WorkerReturnType, WorkerValueType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";

export type WorkerValue = number | boolean | string | null | undefined;
const ownedStrings = new Map<number, VoidPtr>();

export function readWorkerArgumentTypes(signal: Int32Array, entry: number): WorkerValueType[] {
    const count = Atomics.load(signal, entry + RING_ENTRY_ARGUMENT_COUNT);
    if (count < 0 || count > WorkerMaxArgumentCount) throw new Error("withWorker(): invalid argument count");
    const types: WorkerValueType[] = [];
    for (let index = 0; index < count; index++) {
        const type = Atomics.load(signal, argumentEntryOffset(entry, index) + ARGUMENT_ENTRY_TYPE);
        if (!isWorkerValueType(type)) throw new Error("withWorker(): unsupported argument type");
        types.push(type);
    }
    return types;
}

export function prepareWorkerArguments(
    signal: Int32Array, entry: number, memory: MemoryViewManager, pointer: number,
    values: unknown[], types: WorkerValueType[], writePrimitives = true,
): void {
    if (types.length > WorkerMaxArgumentCount) throw new Error("withWorker(): too many arguments");
    Atomics.store(signal, entry + RING_ENTRY_ARGUMENT_COUNT, types.length);
    for (let index = 0; index < types.length; index++) {
        const value = validateWorkerValue(values[index], types[index]);
        const argument = argumentEntryOffset(entry, index);
        Atomics.store(signal, argument + ARGUMENT_ENTRY_TYPE, types[index]);
        Atomics.store(signal, argument + ARGUMENT_ENTRY_STRING, 0);
        Atomics.store(signal, argument + ARGUMENT_ENTRY_STRING_LENGTH, typeof value === "string" ? value.length : -1);
        if (writePrimitives && types[index] !== MarshalerType.String) {
            writeWorkerPrimitive(memory, pointer + (index + 2) * JavaScriptMarshalerArgSize, value, types[index]);
        }
    }
}

export function allocateWorkerArgumentStrings(signal: Int32Array, entry: number, own: (pointer: number) => void): void {
    const types = readWorkerArgumentTypes(signal, entry);
    for (let index = 0; index < types.length; index++) {
        if (types[index] !== MarshalerType.String) continue;
        const argument = argumentEntryOffset(entry, index);
        const length = Atomics.load(signal, argument + ARGUMENT_ENTRY_STRING_LENGTH);
        if (length === -1) continue;
        const pointer = allocateWorkerString(length);
        if (pointer) own(pointer);
        Atomics.store(signal, argument + ARGUMENT_ENTRY_STRING, pointer);
    }
}

export function writeWorkerArgumentStrings(
    signal: Int32Array, entry: number, memory: MemoryViewManager, values: unknown[],
): void {
    const types = readWorkerArgumentTypes(signal, entry);
    for (let index = 0; index < types.length; index++) {
        const value = values[index];
        if (types[index] === MarshalerType.String && typeof value === "string") {
            writeWorkerString(memory, Atomics.load(signal, argumentEntryOffset(entry, index) + ARGUMENT_ENTRY_STRING) >>> 0, value);
        }
    }
}

export function readWorkerArguments(signal: Int32Array, entry: number, memory: MemoryViewManager, pointer: number): WorkerValue[] {
    const types = readWorkerArgumentTypes(signal, entry);
    return types.map((type, index) => {
        const argument = argumentEntryOffset(entry, index);
        return type === MarshalerType.String
            ? readWorkerString(memory, Atomics.load(signal, argument + ARGUMENT_ENTRY_STRING) >>> 0,
                Atomics.load(signal, argument + ARGUMENT_ENTRY_STRING_LENGTH))
            : readWorkerPrimitive(memory, pointer + (index + 2) * JavaScriptMarshalerArgSize, type);
    });
}

export function validateWorkerValue(value: unknown, type: WorkerReturnType): WorkerValue {
    if (type === MarshalerType.Void) return undefined;
    if (value === null || value === undefined) return null;
    if (type === MarshalerType.Boolean) {
        if (typeof value !== "boolean") throw new Error("withWorker(): value must be a Boolean");
        return value;
    }
    if (type === MarshalerType.Single || type === MarshalerType.Double) {
        if (typeof value !== "number") throw new Error("withWorker(): value must be a Number");
        return type === MarshalerType.Single ? Math.fround(value) : value;
    }
    if (type === MarshalerType.String) {
        if (typeof value !== "string") throw new Error("withWorker(): value must be a String");
        return value;
    }
    if (type !== MarshalerType.Int32 || typeof value !== "number" || !Number.isInteger(value) || value < -0x8000_0000 || value > 0x7fff_ffff) {
        throw new Error("withWorker(): value must be an Int32");
    }
    return value;
}

function primitiveViews(memory: MemoryViewManager, pointer: number) {
    const views = memory.refreshIfNeeded();
    if (!Number.isInteger(pointer) || pointer < 0 || pointer % 8 !== 0 || pointer + JavaScriptMarshalerArgSize > views.u8.byteLength) {
        throw new Error("withWorker(): invalid primitive argument frame");
    }
    return views;
}

export function writeWorkerPrimitive(memory: MemoryViewManager, pointer: number, value: unknown, type: WorkerValueType): void {
    const result = validateWorkerValue(value, type);
    const views = primitiveViews(memory, pointer);
    views.u8[pointer + JSMarshalerArgumentOffsets.Type] = result === null ? MarshalerType.None : type;
    if (result === null) return;
    switch (type) {
        case MarshalerType.Boolean:
            views.u8[pointer] = result ? 1 : 0;
            break;
        case MarshalerType.Int32:
            views.i32[pointer / 4] = Number(result);
            break;
        case MarshalerType.Single:
            views.f32[pointer / 4] = Number(result);
            break;
        case MarshalerType.Double:
            views.f64[pointer / 8] = Number(result);
            break;
        default:
            throw new Error("withWorker(): invalid primitive value type");
    }
}

export function readWorkerPrimitive(memory: MemoryViewManager, pointer: number, type: WorkerValueType): number | boolean | null {
    const views = primitiveViews(memory, pointer);
    const actualType = views.u8[pointer + JSMarshalerArgumentOffsets.Type];
    if (actualType === MarshalerType.None) return null;
    if (actualType !== type) throw new Error("withWorker(): invalid primitive result type");
    switch (type) {
        case MarshalerType.Boolean: return views.u8[pointer] !== 0;
        case MarshalerType.Int32: return views.i32[pointer / 4];
        case MarshalerType.Single: return views.f32[pointer / 4];
        case MarshalerType.Double: return views.f64[pointer / 8];
        default: throw new Error("withWorker(): invalid primitive value type");
    }
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
