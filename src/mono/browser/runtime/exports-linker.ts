// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_imports, mono_wasm_threads_imports } from "./exports-binding";
import gitHash from "consts:gitHash";

export function export_linker_indexes_as_code(): string {
    const indexByName: any = {
        mono_wasm_imports: {},
        mono_wasm_threads_imports: {},
    };
    let idx = 0;
    for (const wi of mono_wasm_imports) {
        indexByName.mono_wasm_imports[wi.name] = idx;
        idx++;
    }
    for (const wi of mono_wasm_threads_imports) {
        indexByName.mono_wasm_threads_imports[wi.name] = idx;
        idx++;
    }
    return `
    var gitHash = "${gitHash}";
    var methodIndexByName = ${JSON.stringify(indexByName, null, 2)};
    injectDependencies();
    `;
}

// this is running during runtime compile time inside rollup process. 
(globalThis as any).export_linker_indexes_as_code = export_linker_indexes_as_code;