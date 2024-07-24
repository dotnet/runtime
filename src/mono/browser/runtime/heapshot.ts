/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert } from "./globals";
import { mono_log_info } from "./logging";
import { getU32 } from "./memory";
import type { VoidPtr, ManagedPointer } from "./types/emscripten";

let currentHeapshot : any = {};
const objectEdges = new Map<ManagedPointer, ManagedPointer[]>();
const addressToIndexMap = new Map<ManagedPointer, number>();
const indexToAddressMap = new Map<number, ManagedPointer>();
const unrootedObjects = new Set<ManagedPointer>();

const absurdNodeIndexMagicNumber = 7;

export function mono_wasm_heapshot_start () : void {
    mono_log_info("heapshot started");
    // unless you exactly match what the chrome devtools expect, they will crash when loading a snapshot.
    // vs code's snapshot loader is even worse. have fun!
    currentHeapshot = {
        "snapshot": {
            "meta": {
                "node_fields": [
                    "type",
                    "name",
                    "id",
                    "self_size",
                    "edge_count",
                    "trace_node_id",
                    "detachedness",
                ],
                "node_types": [
                    [
                        "hidden",
                        "array",
                        "string",
                        "object",
                        "code",
                        "closure",
                        "regexp",
                        "number",
                        "native",
                        "synthetic",
                    ],
                    "string",
                    "number",
                    "number",
                    "number",
                    "number",
                    "number",
                ],
                "edge_fields": [
                    "type",
                    "name_or_index",
                    "to_node"
                ],
                "edge_types": [
                    [
                        "context",
                        "element",
                        "property",
                        "internal",
                        "hidden",
                        "shortcut",
                        "weak",
                    ],
                    "string_or_number",
                    "node"
                ],
                "trace_function_info_fields": [],
                "trace_node_fields": [],
                "sample_fields": [],
            },
            "node_count": 0,
            "edge_count": 0,
            "trace_function_count": 0,
            "root_index": 0,
        },
        "nodes": [],
        "edges": [],
        "strings": ["", "Object"],
        "trace_function_infos": [],
        "trace_tree": [],
        "samples": [],
        "locations": [],
    };
}

function pushObject (name: string, pObj: ManagedPointer, size: number, edges: ManagedPointer[]): number {
    const index = currentHeapshot.snapshot.node_count;
    addressToIndexMap.set(pObj, index);
    indexToAddressMap.set(index, pObj);
    objectEdges.set(pObj, edges);
    unrootedObjects.add(pObj);
    currentHeapshot.snapshot.node_count++;
    currentHeapshot.nodes.push(3, 1, pObj, size, edges.length, 0, 0);
    return index;
}

const noEdges : ManagedPointer[] = [];

export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr) : void {
    const edges = numRefs ? new Array(numRefs) : noEdges;
    for (let i = 0; i < numRefs; i++)
        edges[i] = getU32(<any>pRefs + (i * 4));
    const index = pushObject("unknown", pObj, size, edges);
}

function generateEdges (edges: ManagedPointer[]) {
    for (const edge of edges) {
        unrootedObjects.delete(edge);
        currentHeapshot.snapshot.edge_count++;
        const targetIndex = addressToIndexMap.get(edge);
        mono_assert(targetIndex !== undefined, "Found unrecognized address in edges");
        currentHeapshot.edges.push(0, 0, targetIndex * absurdNodeIndexMagicNumber);
    }
}

export function mono_wasm_heapshot_end () : void {
    for (let i = 0; i < currentHeapshot.snapshot.node_count; i++) {
        const pObj = indexToAddressMap.get(i);
        if (!pObj)
            continue;
        const edges = objectEdges.get(pObj);
        if (!edges)
            continue;
        generateEdges(edges);
    }

    objectEdges.clear();

    const unrootedEdges = Array.from(unrootedObjects);
    mono_log_info("generating root node with " + unrootedObjects.size + " edges");
    currentHeapshot.snapshot.root_index = pushObject("root", <any>0, 0, unrootedEdges);
    generateEdges(unrootedEdges);
    mono_log_info("heapshot finished");
    mono_log_info(JSON.stringify(currentHeapshot));
    currentHeapshot = {};
}
