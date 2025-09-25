// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserHostExports, RuntimeAPI } from "./types";
import { InternalExchangeIndex } from "./types";
import { } from "./cross-linked"; // ensure ambient symbols are declared

import { exit, runMain, runMainAndExit, setEnvironmentVariable, registerDllBytes } from "./host";
import {
    setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
    getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
    localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
    isSharedArrayBuffer,
} from "./memory";

export function dotnetInitializeModule(internals: InternalExchange): void {
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        runMain,
        runMainAndExit,
        setEnvironmentVariable,
        exit,
        setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
        getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
        localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
    };

    const hostNativeExportsLocal: BrowserHostExports = {
        registerDllBytes,
        isSharedArrayBuffer
    };
    dotnetSetInternals(internals);
    Object.assign(internals[InternalExchangeIndex.RuntimeAPI], runtimeApiLocal);
    internals[InternalExchangeIndex.BrowserHostExportsTable] = dotnetTabBHE(hostNativeExportsLocal);
    const updates = internals[InternalExchangeIndex.InternalUpdatesCallbacks];
    if (!updates.includes(dotnetUpdateModuleInternals)) updates.push(dotnetUpdateModuleInternals);
    dotnetUpdateAllInternals();
}

export { BrowserHost_ExternalAssemblyProbe, BrowserHost_ResolveMain, BrowserHost_RejectMain } from "./host";
