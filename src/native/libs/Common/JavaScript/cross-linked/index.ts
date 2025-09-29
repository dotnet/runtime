// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    export const dotnetAssert:AssertType;
    export const dotnetLogger:LoggerType;
    export const dotnetLoaderExports:LoaderExports;
    export const dotnetSetInternals:(internals:Partial<InternalExchange>) => void;
    export const dotnetUpdateAllInternals:() => void;
    export const dotnetUpdateModuleInternals:() => void;

    // ambient in the emscripten closure
    export const Module:EmscriptenModuleInternal;
    export const ENVIRONMENT_IS_NODE: boolean;
    export const ENVIRONMENT_IS_SHELL:boolean;
    export const ENVIRONMENT_IS_WEB: boolean;
    export const ENVIRONMENT_IS_WORKER:boolean;
    export const ENVIRONMENT_IS_SIDECAR: boolean;
}
