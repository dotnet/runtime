// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange, InternalExchangeSubscriber, RuntimeAPI, BrowserUtilsExports, VoidPtr, RuntimeExports } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`

type emAmbientSymbolsType = {
    dotnetApi: RuntimeAPI;
    dotnetAssert: AssertType;
    dotnetLogger: LoggerType;
    dotnetLoaderExports: LoaderExports;
    dotnetRuntimeExports: RuntimeExports;
    dotnetBrowserUtilsExports: BrowserUtilsExports;

    dotnetUpdateInternals: (internals?: Partial<InternalExchange>, subscriber?: InternalExchangeSubscriber) => void;
    dotnetUpdateInternalsSubscriber: (internals: InternalExchange) => void;

    _GetDotNetRuntimeContractDescriptor: () => void;
    _SystemJS_ExecuteTimerCallback: () => void;
    _SystemJS_ExecuteBackgroundJobCallback: () => void;
    _BrowserHost_InitializeCoreCLR: () => number;
    _BrowserHost_ExecuteAssembly: (mainAssemblyNamePtr: number, argsLength: number, argsPtr: number) => number;
    _wasm_load_icu_data: (dataPtr: VoidPtr) => number;

    DOTNET: any;
    DOTNET_INTEROP: any;
    BROWSER_HOST: any;

    Module: EmscriptenModuleInternal;
    ENVIRONMENT_IS_NODE: boolean;
    ENVIRONMENT_IS_SHELL: boolean;
    ENVIRONMENT_IS_WEB: boolean;
    ENVIRONMENT_IS_WORKER: boolean;
    ENVIRONMENT_IS_SIDECAR: boolean;
    ABORT: boolean;
    EXITSTATUS: number;

    ExitStatus: (exitCode: number) => number;
    _emscripten_force_exit: (exitCode: number) => void;
    _exit: (exitCode: number, implicit?: boolean) => void;
    safeSetTimeout: (func: Function, timeout: number) => number;
    maybeExit: () => void;
    exitJS: (status: number, implicit?: boolean | number) => void;
    runtimeKeepalivePop: () => void;
    runtimeKeepalivePush: () => void;
}

const _ems_: emAmbientSymbolsType = globalThis as any;
//export default emAmbientSymbols;
export { _ems_ };
