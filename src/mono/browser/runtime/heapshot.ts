/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert } from "./globals";
import { mono_log_info } from "./logging";
import { getU32 } from "./memory";
import { utf8ToString } from "./strings";
import type { VoidPtr, ManagedPointer, CharPtr } from "./types/emscripten";

const nameFromKind = {
    1: "def",
    2: "gtd",
    3: "ginst",
    4: "gparam",
    5: "array",
    6: "pointer",
};

export function mono_wasm_heapshot_class (klass: VoidPtr, pNamespace: CharPtr, pName: CharPtr, kind: number, numGps: number, pGp: VoidPtr): void {
    const kindName = nameFromKind || "unknown";
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
    mono_log_info(`new class: ${klass} ${kindName} ${className}`);
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr): void {
    mono_log_info(`  ${pObj} klass=${klass} size=${size} refs=${numRefs}`);
}

export function mono_wasm_heapshot_start (): void {
    mono_log_info("heapshot starting");
}

export function mono_wasm_heapshot_end (): void {
    mono_log_info("heapshot finished");
}
