// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, LoggerType, LoaderExports, InternalExchange, InternalExchangeSubscriber, RuntimeAPI, BrowserUtilsExports, NativePointer, CharPtr, VoidPtr } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    export const dotnetApi: RuntimeAPI;
    export const dotnetAssert:AssertType;
    export const dotnetLogger:LoggerType;
    export const dotnetLoaderExports:LoaderExports;
    export const dotnetBrowserUtilsExports:BrowserUtilsExports;
    export const dotnetUpdateInternals:(internals?:Partial<InternalExchange>, subscriber?:InternalExchangeSubscriber) => void;
    export const dotnetUpdateInternalsSubscriber:(internals:InternalExchange) => void;

    // ambient in the emscripten closure
    export const Module:EmscriptenModuleInternal;
    export const ENVIRONMENT_IS_NODE: boolean;
    export const ENVIRONMENT_IS_SHELL:boolean;
    export const ENVIRONMENT_IS_WEB: boolean;
    export const ENVIRONMENT_IS_WORKER:boolean;
    export const ENVIRONMENT_IS_SIDECAR: boolean;

    export const VoidPtrNull: VoidPtr;
    export const CharPtrNull: CharPtr;
    export const NativePointerNull: NativePointer;

}
