// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoConfig, APIType } from "./types";

import { mono_wasm_get_assembly_exports } from "./invoke-cs";
import { mono_wasm_set_module_imports } from "./invoke-js";
import { getB32, getF32, getF64, getI16, getI32, getI52, getI64Big, getI8, getU16, getU32, getU52, getU8, setB32, setF32, setF64, setI16, setI32, setI52, setI64Big, setI8, setU16, setU32, setU52, setU8 } from "./memory";
import { mono_run_main, mono_run_main_and_exit } from "./run";
import { mono_wasm_setenv } from "./startup";
import { runtimeHelpers } from "./globals";

export function export_api(): any {
    const api: APIType = {
        runMain: mono_run_main,
        runMainAndExit: mono_run_main_and_exit,
        setEnvironmentVariable: mono_wasm_setenv,
        getAssemblyExports: mono_wasm_get_assembly_exports,
        setModuleImports: mono_wasm_set_module_imports,
        getConfig: (): MonoConfig => {
            return runtimeHelpers.config;
        },
        setHeapB32: setB32,
        setHeapU8: setU8,
        setHeapU16: setU16,
        setHeapU32: setU32,
        setHeapI8: setI8,
        setHeapI16: setI16,
        setHeapI32: setI32,
        setHeapI52: setI52,
        setHeapU52: setU52,
        setHeapI64Big: setI64Big,
        setHeapF32: setF32,
        setHeapF64: setF64,
        getHeapB32: getB32,
        getHeapU8: getU8,
        getHeapU16: getU16,
        getHeapU32: getU32,
        getHeapI8: getI8,
        getHeapI16: getI16,
        getHeapI32: getI32,
        getHeapI52: getI52,
        getHeapU52: getU52,
        getHeapI64Big: getI64Big,
        getHeapF32: getF32,
        getHeapF64: getF64,
    };
    return api;
}
