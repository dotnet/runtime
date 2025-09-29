// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserUtilsExports, RuntimeAPI, BrowserUtilsExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";
import { } from "./cross-module"; // ensure ambient symbols are declared

import {
    setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
    getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
    localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
} from "./memory";
import { stringToUTF16, stringToUTF16Ptr, utf16ToString } from "./strings";
import { exit, setEnvironmentVariable } from "./host";
import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "../utils/cross-module";

export function dotnetInitializeModule(internals: InternalExchange): void {
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        setEnvironmentVariable,
        exit,
        setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
        getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
        localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
    };
    Object.assign(internals[InternalExchangeIndex.RuntimeAPI], runtimeApiLocal);

    internals[InternalExchangeIndex.BrowserUtilsExportsTable] = browserUtilsExportsToTable({
        utf16ToString,
        stringToUTF16,
        stringToUTF16Ptr,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
    function browserUtilsExportsToTable(map:BrowserUtilsExports):BrowserUtilsExportsTable {
        // keep in sync with browserUtilsExportsFromTable()
        return [
            map.utf16ToString,
            map.stringToUTF16,
            map.stringToUTF16Ptr,
        ];
    }
}

// see also `reserved` in `rollup.config.defines.js`
export * as cross from "./cross-module";
