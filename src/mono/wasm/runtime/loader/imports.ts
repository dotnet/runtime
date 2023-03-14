import type { DotnetModule, RuntimeHelpers } from "../types";
import type { EmscriptenModule, EmscriptenModuleInternal } from "../types/emscripten";

// duplicate from emscripten
export const ENVIRONMENT_IS_WEB = typeof window == "object";
export const ENVIRONMENT_IS_WORKER = typeof importScripts == "function";
export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;
export let Module: EmscriptenModule & DotnetModule & EmscriptenModuleInternal = undefined as any;
export let runtimeHelpers: RuntimeHelpers;
export let INTERNAL: any;

export function setImports(
    module: EmscriptenModule & DotnetModule & EmscriptenModuleInternal,
    helpers: RuntimeHelpers
) {
    Module = module;
    INTERNAL = module.INTERNAL;
    runtimeHelpers = helpers;
}