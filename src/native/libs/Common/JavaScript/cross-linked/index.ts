// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange, InternalExchangeSubscriber, RuntimeAPI, BrowserUtilsExports, NativePointer, CharPtr, VoidPtr, RuntimeExports } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    export const dotnetApi: RuntimeAPI;
    export const dotnetAssert: AssertType;
    export const dotnetLogger: LoggerType;
    export const dotnetLoaderExports: LoaderExports;
    export const dotnetRuntimeExports: RuntimeExports;
    export const dotnetBrowserUtilsExports: BrowserUtilsExports;
    export const dotnetUpdateInternals: (internals?: Partial<InternalExchange>, subscriber?: InternalExchangeSubscriber) => void;
    export const dotnetUpdateInternalsSubscriber: (internals: InternalExchange) => void;

    export function _GetDotNetRuntimeContractDescriptor(): void;
    export function _SystemJS_ExecuteTimerCallback(): void;
    export function _SystemJS_ExecuteBackgroundJobCallback(): void;
    export function _BrowserHost_InitializeCoreCLR(): number;
    export function _BrowserHost_ExecuteAssembly(mainAssemblyNamePtr: number, argsLength: number, argsPtr: number): number;
    export function _wasm_load_icu_data(dataPtr: VoidPtr): number;

    export const VoidPtrNull: VoidPtr;
    export const CharPtrNull: CharPtr;
    export const NativePointerNull: NativePointer;

    export const DOTNET: any;
    export const BROWSER_HOST: any;

    // ambient in the emscripten closure
    export const Module: EmscriptenModuleInternal;
    export const ENVIRONMENT_IS_NODE: boolean;
    export const ENVIRONMENT_IS_SHELL: boolean;
    export const ENVIRONMENT_IS_WEB: boolean;
    export const ENVIRONMENT_IS_WORKER: boolean;
    export const ENVIRONMENT_IS_SIDECAR: boolean;

    export let ABORT: boolean;
    export let EXITSTATUS: number;
    export function ExitStatus(exitCode: number): number;

    export function _emscripten_force_exit(exitCode: number): void;
    export function _exit(exitCode: number, implicit?: boolean): void;
    export function safeSetTimeout(func: Function, timeout: number): number;
    export function maybeExit(): void;
    export function exitJS(status: number, implicit?: boolean | number): void;
}

export const __dummy = 0;
