/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert } from "./globals";
import { mono_log_info } from "./logging";
import { getU32 } from "./memory";
import { utf8ToString } from "./strings";
import type { VoidPtr, ManagedPointer, CharPtr } from "./types/emscripten";

const nameFromKind = ["unknown", "def", "gtd", "ginst", "gparam", "array", "pointer"];
const handleTypes = ["weak", "weak_track", "normal", "pinned", "weak_fields"];

let heapshotStartedWhen = performance.now();

export function mono_wasm_heapshot_class (klass: VoidPtr, elementKlass: VoidPtr, pNamespace: CharPtr, pName: CharPtr, rank: number, kind: number, numGps: number, pGp: VoidPtr): void {
    const kindName = nameFromKind[kind] || "unknown";
    let className = `${utf8ToString(pNamespace)}.${utf8ToString(pName)}`;
    if (numGps) {
        className += "<";
        for (let i = 0; i < numGps; i++) {
            if (i > 0)
                className += ", ";
            const gt = getU32(<any>pGp + (i * 4));
            className += gt;
        }
        className += ">";
    }
    mono_log_info(
        rank > 0
            ? `new array: ${klass} ${elementKlass}[] ${kindName} ${className}[${",".repeat(rank - 1)}]`
            : `new class: ${klass} ${kindName} ${className}`
    );
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr): void {
    mono_log_info(`  ${pObj} klass=${klass} size=${size} refs=${numRefs}`);
}

export function mono_wasm_heapshot_gchandle (pObj: ManagedPointer, handleType: number): void {
    const handleTypeName = handleTypes[handleType] || "unknown";
    mono_log_info(`  ${handleTypeName} handle -> ${pObj}`);
}

export function mono_wasm_heapshot_roots (pObjs: VoidPtr, numObjs: number, source: number, rootType: number, msg: CharPtr): void {
    const message = msg ? utf8ToString(msg) : "";
    mono_log_info(`root type=${rootType} source=${source} x${numObjs} objects ${message}`);
    for (let i = 0; i < numObjs; i++) {
        const obj = getU32(<any>pObjs + (i * 4));
        mono_log_info(`  ${obj}`);
    }
}

export function mono_wasm_heapshot_start (): void {
    heapshotStartedWhen = performance.now();
    mono_log_info("heapshot starting");
}

function pagesToMegabytes (pages: number) {
    return (pages * 65536 / (1024 * 1024)).toFixed(1);
}

function bytesToMegabytes (bytes: number) {
    return (bytes / (1024 * 1024)).toFixed(1);
}

export function mono_wasm_heapshot_stats (
    pagesInUse: number, pagesFree: number, pagesUnknown: number,
    largestFreeChunk: number, largeObjectHeapSize: number, sgenHeapCapacity: number
): void {
    const totalPages = pagesInUse + pagesFree;
    mono_log_info(`mwpm: ${pagesToMegabytes(totalPages)}MB allocated; ${(pagesInUse / totalPages * 100.0).toFixed(1)}% in use; largest free block: ${pagesToMegabytes(largestFreeChunk)}MB; unknown pages: ${pagesToMegabytes(pagesUnknown)}MB`);
    mono_log_info(`sgen: heap capacity ${bytesToMegabytes(sgenHeapCapacity)}MB; LOS size ${bytesToMegabytes(largeObjectHeapSize)}MB; gchandle counts:`);
}

export function mono_wasm_heapshot_end (): void {
    const elapsedMs = performance.now() - heapshotStartedWhen;
    mono_log_info(`heapshot finished after ${elapsedMs.toFixed(1)}msec`);
}
