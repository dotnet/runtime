/* eslint-disable no-console */
/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import cwraps from "../cwraps";
import { mono_assert } from "../globals";
import { mono_log_info } from "../logging";
import { getU32 } from "../memory";
import { utf8ToString } from "../strings";
import type { VoidPtr, ManagedPointer, CharPtr } from "../types/emscripten";

const nameFromKind = ["unknown", "def", "gtd", "ginst", "gparam", "array", "pointer"];
const handleTypes = ["weak", "weak_track", "normal", "pinned", "weak_fields"];

const tabId = (Math.random() * 100000).toFixed(0);
const globalChannel : BroadcastChannel | null = globalThis["BroadcastChannel"] ? new BroadcastChannel(".NET Runtime Diagnostics") : null,
    tabChannel : BroadcastChannel | null = globalThis["BroadcastChannel"] ? new BroadcastChannel(`.NET Runtime Diagnostics|${tabId}`) : null;
if (globalChannel)
    globalChannel.addEventListener("message", channel_message);
if (tabChannel)
    tabChannel.addEventListener("message", tabChannel_message);

function channel_message (evt: MessageEvent) {
    console.log("channel_message", evt, evt.data);
    const data = evt.data;
    if ((typeof (data) !== "object") || (typeof (data.sender) !== "string") || (typeof (data.cmd) !== "string"))
        return;

    switch (data.cmd) {
        case "whosThere":
            globalChannel!.postMessage({ cmd: "iAmHere", sender: tabId, version: ProductVersion });
            break;
    }
}

function tabChannel_message (evt: MessageEvent) {
    console.log("tabChannel_message", evt, evt.data);
    const data = evt.data;
    if ((typeof (data) !== "object") || (typeof (data.sender) !== "string") || (typeof (data.cmd) !== "string"))
        return;

    switch (data.cmd) {
        case "takeSnapshot":
            cwraps.mono_wasm_perform_heapshot();
            break;
    }
}

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
    heapshot_log(
        rank > 0
            ? `new array: ${klass} ${elementKlass}[] ${kindName} ${className}[${",".repeat(rank - 1)}]`
            : `new class: ${klass} ${kindName} ${className}`
    );
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr): void {
    // mono_log_info(`  ${pObj} klass=${klass} size=${size} refs=${numRefs}`);
}

export function mono_wasm_heapshot_gchandle (pObj: ManagedPointer, handleType: number): void {
    // HACK
    if (handleType == 1)
        return;
    const handleTypeName = handleTypes[handleType] || "unknown";
    heapshot_log(`  ${handleTypeName} handle -> ${pObj}`);
}

export function mono_wasm_heapshot_roots (pObjs: VoidPtr, numObjs: number, source: number, rootType: number, msg: CharPtr): void {
    const message = msg ? utf8ToString(msg) : "";
    heapshot_log(`root type=${rootType} source=${source} x${numObjs} objects ${message}`);
    for (let i = 0; i < numObjs; i++) {
        const obj = getU32(<any>pObjs + (i * 4));
        heapshot_log(`  ${obj}`);
    }
}

export function mono_wasm_heapshot_start (): void {
    heapshotStartedWhen = performance.now();
    heapshot_log("heapshot starting");
}

function pagesToMegabytes (pages: number) {
    return (pages * 65536 / (1024 * 1024)).toFixed(1);
}

function bytesToMegabytes (bytes: number) {
    return (bytes / (1024 * 1024)).toFixed(1);
}

function heapshot_log (text: string) {
    mono_log_info(text);
    if (tabChannel)
        tabChannel.postMessage({ cmd: "heapshotText", sender: tabId, text });
}

export function mono_wasm_heapshot_stats (
    pagesInUse: number, pagesFree: number, pagesUnknown: number,
    largestFreeChunk: number, largeObjectHeapSize: number, sgenHeapCapacity: number
): void {
    const totalPages = pagesInUse + pagesFree;
    const line1 = `mwpm: ${pagesToMegabytes(totalPages)}MB allocated; ${(pagesInUse / totalPages * 100.0).toFixed(1)}% in use; largest free block: ${pagesToMegabytes(largestFreeChunk)}MB; unknown pages: ${pagesToMegabytes(pagesUnknown)}MB`;
    const line2 = `sgen: heap capacity ${bytesToMegabytes(sgenHeapCapacity)}MB; LOS size ${bytesToMegabytes(largeObjectHeapSize)}MB`;
    heapshot_log(line1);
    heapshot_log(line2);
}

export function mono_wasm_heapshot_end (): void {
    const elapsedMs = performance.now() - heapshotStartedWhen;
    heapshot_log(`heapshot finished after ${elapsedMs.toFixed(1)}msec`);
}
