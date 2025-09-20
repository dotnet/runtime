// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalApis, NativeExports, RuntimeAPI } from "./types";
import { nativeExportsToTable, setInternals, updateInternals, updateInternalsImpl } from "./cross-module";

import { exit, runMain, runMainAndExit, setEnvironmentVariable, registerDllBytes } from "./host";
import {
    setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
    getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
    localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
    isSharedArrayBuffer,
} from "./memory";

export async function initialize(internals: InternalApis): Promise<void> {
    const runtimeApiFunctions: Partial<RuntimeAPI> = {
        runMain,
        runMainAndExit,
        setEnvironmentVariable,
        exit,
        setHeapB32, setHeapB8, setHeapU8, setHeapU16, setHeapU32, setHeapI8, setHeapI16, setHeapI32, setHeapI52, setHeapU52, setHeapI64Big, setHeapF32, setHeapF64,
        getHeapB32, getHeapB8, getHeapU8, getHeapU16, getHeapU32, getHeapI8, getHeapI16, getHeapI32, getHeapI52, getHeapU52, getHeapI64Big, getHeapF32, getHeapF64,
        localHeapViewI8, localHeapViewI16, localHeapViewI32, localHeapViewI64Big, localHeapViewU8, localHeapViewU16, localHeapViewU32, localHeapViewF32, localHeapViewF64,
    };

    const nativeExports: NativeExports = {
        registerDllBytes,
        isSharedArrayBuffer
    };
    setInternals(internals);
    Object.assign(internals.runtimeApi, runtimeApiFunctions);
    Object.assign(nativeExports, nativeExports);
    internals.nativeExportsTable = [...nativeExportsToTable(nativeExports)];
    internals.updates.push(updateInternalsImpl);
    updateInternals();
}

export * from "./host";
export * from "./memory";
