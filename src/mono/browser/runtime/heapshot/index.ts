/* eslint-disable no-console */
/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import cwraps from "../cwraps";
import { loaderHelpers } from "../globals";
import { mono_log_info } from "../logging";
import { getU32 } from "../memory";
import { utf8ToString } from "../strings";
import { BlobBuilder } from "../jiterpreter-support";
import type { VoidPtr, ManagedPointer, CharPtr } from "../types/emscripten";

const packetBuilderCapacity = 65536;
const packetFlushThreshold = 32768;
// FIXME: It would be ideal if we didn't have to actually retain the whole string
const stringTable = new Map<string, number>();
const incompletePackets = new Map<string, BlobBuilder>();
const formatVersion = 1;

const nameFromKind = ["unknown", "def", "gtd", "ginst", "gparam", "array", "pointer"];
const handleTypes = ["weak", "weak_track", "normal", "pinned", "weak_fields"];

let totalObjects = 0, totalRefs = 0, totalClasses = 0, totalAssemblies = 0;
let mostRecentObjectPointer : ManagedPointer = <any>0;
let heapshotStartedWhen = performance.now();

const tabId = (Math.random() * 100000).toFixed(0);
const globalChannel : BroadcastChannel | null = globalThis["BroadcastChannel"] ? new BroadcastChannel(".NET Runtime Diagnostics") : null,
    tabChannel : BroadcastChannel | null = globalThis["BroadcastChannel"] ? new BroadcastChannel(`.NET Runtime Diagnostics|${tabId}`) : null;
if (globalChannel)
    globalChannel.addEventListener("message", channel_message);
if (tabChannel)
    tabChannel.addEventListener("message", tabChannel_message);

function channel_message (evt: MessageEvent) {
    const data = evt.data;
    if ((typeof (data) !== "object") || (typeof (data.sender) !== "string") || (typeof (data.cmd) !== "string"))
        return;

    console.log("channel_message", data.cmd);
    switch (data.cmd) {
        case "whosThere": {
            let title = "unknown";
            if (globalThis["document"] && globalThis["document"]["title"])
                title = globalThis.document.title;
            globalChannel!.postMessage({ cmd: "iAmHere", sender: tabId, version: ProductVersion, running: loaderHelpers.is_runtime_running(), title });
            break;
        }
    }
}

function tabChannel_message (evt: MessageEvent) {
    const data = evt.data;
    if ((typeof (data) !== "object") || (typeof (data.sender) !== "string") || (typeof (data.cmd) !== "string"))
        return;

    console.log("tabChannel_message", data.cmd);
    switch (data.cmd) {
        case "takeSnapshot":
            cwraps.mono_wasm_perform_heapshot();
            break;
    }
}

export function mono_wasm_heapshot_assembly (assembly: VoidPtr, pName: CharPtr) {
    const builder = getBuilder("ASSM");
    totalAssemblies += 1;
    builder.appendU32(<any>assembly);
    // No point in using the string table for this
    builder.appendName(utf8ToString(pName));
}

export function mono_wasm_heapshot_class (klass: VoidPtr, elementKlass: VoidPtr, assembly: VoidPtr, pNamespace: CharPtr, pName: CharPtr, rank: number, kind: number, numGps: number, pGp: VoidPtr): void {
    const builder = getBuilder("TYPE");
    const kindName = nameFromKind[kind] || "unknown";
    totalClasses += 1;
    builder.appendU32(<any>klass);
    builder.appendU32(<any>elementKlass);
    builder.appendU32(<any>assembly);
    builder.appendULeb(rank);
    builder.appendULeb(getStringTableIndex(kindName));
    builder.appendULeb(utf8ToStringTableIndex(pNamespace));
    // We use the string table for names because each generic instance will have the same name
    builder.appendULeb(utf8ToStringTableIndex(pName));
    builder.appendULeb(numGps);
    for (let i = 0; i < numGps; i++) {
        const gp = getU32(<any>pGp + (i * 4));
        builder.appendU32(gp);
    }
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr): void {
    // The object header and its refs are stored in separate streams
    if (pObj !== mostRecentObjectPointer) {
        // If we flush the object header chunk we want to flush the current refs chunk so that we don't
        //  have an object's refs span across multiple chunks unless it's unavoidable
        if (autoFlush("OBJH"))
            flush("REFS");

        totalObjects += 1;
        const objBuilder = getBuilder("OBJH");
        objBuilder.appendU32(<any>pObj);
        objBuilder.appendU32(<any>klass);
        objBuilder.appendULeb(size);
        mostRecentObjectPointer = pObj;
    }

    totalRefs += numRefs;
    if (numRefs < 1)
        return;

    const refBuilder = getBuilder("REFS");
    refBuilder.appendU32(<any>pObj);
    refBuilder.appendULeb(numRefs);
    for (let i = 0; i < numRefs; i++) {
        const pRef = getU32(<any>pRefs + (i * 4));
        refBuilder.appendU32(pRef);
    }
}

export function mono_wasm_heapshot_gchandle (pObj: ManagedPointer, handleType: number): void {
    const handleTypeName = handleTypes[handleType] || "unknown";
    const builder = getBuilder("GCHL");
    builder.appendULeb(getStringTableIndex(handleTypeName));
    builder.appendU32(<any>pObj);
}

function getStringTableIndex (text: string) {
    if (text.length === 0)
        return 0;

    let index = stringTable.get(text);
    if (!index) {
        index = (stringTable.size + 1);
        stringTable.set(text, index);
        const builder = getBuilder("STBL");
        builder.appendULeb(index);
        builder.appendName(text);
    }

    return index >>> 0;
}

function utf8ToStringTableIndex (pText: CharPtr) {
    if (!pText)
        return 0;

    const text = utf8ToString(pText);
    return getStringTableIndex(text);
}

export function mono_wasm_heapshot_roots (pObjs: VoidPtr, numObjs: number, source: number, rootType: number, msg: CharPtr): void {
    const message = msg ? utf8ToString(msg) : "";
    const builder = getBuilder("ROOT");
    builder.appendULeb(utf8ToStringTableIndex(msg));
    builder.appendULeb(source);
    builder.appendULeb(rootType);
    builder.appendULeb(numObjs);
    for (let i = 0; i < numObjs; i++) {
        const obj = getU32(<any>pObjs + (i * 4));
        builder.appendU32(obj);
    }
}

export function mono_wasm_heapshot_start (): void {
    stringTable.clear();
    for (const kvp of incompletePackets)
        kvp[1].clear();
    totalObjects = totalRefs = totalClasses = totalAssemblies = 0;
    mostRecentObjectPointer = <any>0;

    heapshotStartedWhen = performance.now();
    if (tabChannel)
        tabChannel.postMessage({ cmd: "heapshotStart", version: formatVersion });
    heapshotLog("heapshot starting");
}

function pagesToMegabytes (pages: number) {
    return (pages * 65536 / (1024 * 1024)).toFixed(1);
}

function bytesToMegabytes (bytes: number) {
    return (bytes / (1024 * 1024)).toFixed(1);
}

function getBuilder (chunkId: string) {
    let result = incompletePackets.get(chunkId);
    if (!result) {
        result = new BlobBuilder(packetBuilderCapacity);
        incompletePackets.set(chunkId, result);
    } else
        autoFlush(chunkId);
    return result;
}

function autoFlush (chunkId: string) {
    const builder = incompletePackets.get(chunkId);
    if (builder && (builder.size >= packetFlushThreshold)) {
        heapshotPacket(chunkId, builder.getArrayView(false));
        builder.clear();
        return true;
    }
    return false;
}

function flush (chunkId?: string) {
    if (chunkId) {
        const builder = incompletePackets.get(chunkId);
        if (builder && (builder.size > 0)) {
            heapshotPacket(chunkId, builder.getArrayView(false));
            builder.clear();
        }
        return;
    }

    for (const kvp of incompletePackets) {
        if (kvp[1].size < 1)
            continue;

        flush(kvp[0]);
    }
}

function heapshotLog (text: string) {
    mono_log_info(text);
    if (tabChannel)
        tabChannel.postMessage({ cmd: "heapshotText", text });
}

function heapshotPacket (chunkId: string, chunk: Uint8Array) {
    mono_log_info(`heapshot packet ${chunkId} ${chunk.length}b`);
    if (!tabChannel)
        return;

    // HACK: Workaround for bug in Chromium structured clone algorithm that copies the whole arraybuffer
    if (chunk.buffer.byteLength !== (chunk.BYTES_PER_ELEMENT * chunk.length))
        chunk = chunk.slice();

    tabChannel.postMessage({ cmd: "heapshotPacket", chunkId: chunkId, chunk: chunk });
}

function heapshotCounter (name: string, value: number) {
    const builder = getBuilder("CNTR");
    // Using the stringtable here would just make the format harder to decode.
    builder.appendName(name);
    builder.appendF64(value);
}

export function mono_wasm_heapshot_stats (
    pagesInUse: number, pagesFree: number, pagesUnknown: number,
    largestFreeChunk: number, largeObjectHeapSize: number, sgenHeapCapacity: number
): void {
    const totalPages = pagesInUse + pagesFree;
    const line1 = `mwpm: ${pagesToMegabytes(totalPages)}MB allocated; ${(pagesInUse / totalPages * 100.0).toFixed(1)}% in use; largest free block: ${pagesToMegabytes(largestFreeChunk)}MB; unknown pages: ${pagesToMegabytes(pagesUnknown)}MB`;
    const line2 = `sgen: heap capacity ${bytesToMegabytes(sgenHeapCapacity)}MB; LOS size ${bytesToMegabytes(largeObjectHeapSize)}MB`;
    heapshotLog(line1);
    heapshotLog(line2);
    heapshotCounter("mwpm/pages-in-use", pagesInUse);
    heapshotCounter("mwpm/pages-free", pagesFree);
    heapshotCounter("mwpm/pages-unknown", pagesUnknown);
    heapshotCounter("mwpm/largest-free-chunk", largestFreeChunk);
    heapshotCounter("sgen/heap-capacity", sgenHeapCapacity);
    heapshotCounter("sgen/los-size", largeObjectHeapSize);
}

export function mono_wasm_heapshot_end (): void {
    heapshotCounter("snapshot/num-strings", stringTable.size);
    heapshotCounter("snapshot/num-objects", totalObjects);
    heapshotCounter("snapshot/num-refs", totalRefs);
    heapshotCounter("snapshot/num-classes", totalClasses);
    heapshotCounter("snapshot/num-assemblies", totalAssemblies);
    flush();
    const elapsedMs = performance.now() - heapshotStartedWhen;
    heapshotLog(`heapshot finished after ${elapsedMs.toFixed(1)}msec`);
    if (tabChannel)
        tabChannel.postMessage({ cmd: "heapshotEnd" });
}
