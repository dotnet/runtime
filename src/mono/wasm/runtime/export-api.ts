import diagnostics from "./diagnostics";
import { APIType } from "./exports";
import { runtimeHelpers } from "./imports";
import { mono_wasm_get_assembly_exports } from "./invoke-cs";
import { getB32, getF32, getF64, getI16, getI32, getI52, getI64Big, getI8, getU16, getU32, getU52, getU8, setB32, setF32, setF64, setI16, setI32, setI52, setI64Big, setI8, setU16, setU32, setU52, setU8 } from "./memory";
import { mono_run_main, mono_run_main_and_exit } from "./run";
import { mono_wasm_setenv } from "./startup";
import { MonoConfig } from "./types";

export function export_api(): any {
    const api: APIType = {
        runMain: mono_run_main,
        runMainAndExit: mono_run_main_and_exit,
        setEnvironmentVariable: mono_wasm_setenv,
        getAssemblyExports: mono_wasm_get_assembly_exports,
        getConfig: (): MonoConfig => {
            return runtimeHelpers.config;
        },
        applyConfig: (config: MonoConfig) => {
            // merge
            Object.assign(runtimeHelpers.config, config);
        },
        memory: {
            setB32,
            setU8,
            setU16,
            setU32,
            setI8,
            setI16,
            setI32,
            setI52,
            setU52,
            setI64Big,
            setF32,
            setF64,
            getB32,
            getU8,
            getU16,
            getU32,
            getI8,
            getI16,
            getI32,
            getI52,
            getU52,
            getI64Big,
            getF32,
            getF64,
        },
        diagnostics,
    };
    return api;
}