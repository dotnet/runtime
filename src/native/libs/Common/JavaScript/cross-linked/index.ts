// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange, InternalExchangeSubscriber, RuntimeAPI, BrowserUtilsExports, VoidPtr, RuntimeExports } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`

export declare const dotnetApi: RuntimeAPI;
export declare const dotnetAssert: AssertType;
export declare const dotnetLogger: LoggerType;
export declare const dotnetLoaderExports: LoaderExports;
export declare const dotnetRuntimeExports: RuntimeExports;
export declare const dotnetBrowserUtilsExports: BrowserUtilsExports;
export declare const dotnetUpdateInternals: (internals?: Partial<InternalExchange>, subscriber?: InternalExchangeSubscriber) => void;
export declare const dotnetUpdateInternalsSubscriber: (internals: InternalExchange) => void;

export declare function _GetDotNetRuntimeContractDescriptor(): void;
export declare function _SystemJS_ExecuteTimerCallback(): void;
export declare function _SystemJS_ExecuteBackgroundJobCallback(): void;
export declare function _BrowserHost_InitializeCoreCLR(): number;
export declare function _BrowserHost_ExecuteAssembly(mainAssemblyNamePtr: number, argsLength: number, argsPtr: number): number;
export declare function _wasm_load_icu_data(dataPtr: VoidPtr): number;

export declare const DOTNET: any;
export declare const BROWSER_HOST: any;

// ambient in the emscripten closure
export declare const Module: EmscriptenModuleInternal;
export declare const ENVIRONMENT_IS_NODE: boolean;
export declare const ENVIRONMENT_IS_SHELL: boolean;
export declare const ENVIRONMENT_IS_WEB: boolean;
export declare const ENVIRONMENT_IS_WORKER: boolean;
export declare const ENVIRONMENT_IS_SIDECAR: boolean;

export declare function ExitStatus(exitCode: number): number;

export declare function _emscripten_force_exit(exitCode: number): void;
export declare function _exit(exitCode: number, implicit?: boolean): void;
export declare function safeSetTimeout(func: Function, timeout: number): number;
export declare function maybeExit(): void;
export declare function exitJS(status: number, implicit?: boolean | number): void;
