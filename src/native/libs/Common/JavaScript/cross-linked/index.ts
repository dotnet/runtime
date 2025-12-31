// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange, InternalExchangeSubscriber, RuntimeAPI, BrowserUtilsExports, VoidPtr, RuntimeExports } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    const dotnetApi: RuntimeAPI;
    const dotnetAssert: AssertType;
    const dotnetLogger: LoggerType;
    const dotnetLoaderExports: LoaderExports;
    const dotnetRuntimeExports: RuntimeExports;
    const dotnetBrowserUtilsExports: BrowserUtilsExports;
    const dotnetUpdateInternals: (internals?: Partial<InternalExchange>, subscriber?: InternalExchangeSubscriber) => void;
    const dotnetUpdateInternalsSubscriber: (internals: InternalExchange) => void;

    function _GetDotNetRuntimeContractDescriptor(): void;
    function _SystemJS_ExecuteTimerCallback(): void;
    function _SystemJS_ExecuteBackgroundJobCallback(): void;
    function _BrowserHost_InitializeCoreCLR(): number;
    function _BrowserHost_ExecuteAssembly(mainAssemblyNamePtr: number, argsLength: number, argsPtr: number): number;
    function _wasm_load_icu_data(dataPtr: VoidPtr): number;

    const DOTNET: any;
    const BROWSER_HOST: any;

    // ambient in the emscripten closure
    const Module: EmscriptenModuleInternal;
    const ENVIRONMENT_IS_NODE: boolean;
    const ENVIRONMENT_IS_SHELL: boolean;
    const ENVIRONMENT_IS_WEB: boolean;
    const ENVIRONMENT_IS_WORKER: boolean;
    const ENVIRONMENT_IS_SIDECAR: boolean;

    let ABORT: boolean;
    let EXITSTATUS: number;
    function ExitStatus(exitCode: number): number;

    function _emscripten_force_exit(exitCode: number): void;
    function _exit(exitCode: number, implicit?: boolean): void;
    function safeSetTimeout(func: Function, timeout: number): number;
    function maybeExit(): void;
    function exitJS(status: number, implicit?: boolean | number): void;
}
