// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { dotnetAssertType, EmscriptenModuleInternal, JSEngineType, dotnetLoggerType, LoaderExports, InternalExchange, RuntimeExports, RuntimeExportsTable, BrowserHostExportsTable, BrowserHostExports, InteropJavaScriptExports, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    export const Module:EmscriptenModuleInternal;
    export const dotnetAssert:dotnetAssertType;
    export const dotnetLogger:dotnetLoggerType;
    export const dotnetJSEngine:JSEngineType;
    export const dotnetLoaderExports:LoaderExports;
    export const dotnetTabIJSE:(hostNativeExports:InteropJavaScriptExports) => InteropJavaScriptExportsTable;
    export const dotnetTabRE:(hostNativeExports:RuntimeExports) => RuntimeExportsTable;
    export const dotnetTabBHE:(hostNativeExports:BrowserHostExports) => BrowserHostExportsTable;
    export const dotnetTabNBE:(hostNativeExports:NativeBrowserExports) => NativeBrowserExportsTable;
    export const dotnetTabLE:(hostNativeExports:BrowserHostExports) => BrowserHostExportsTable;
    export const dotnetSetInternals:(internals:Partial<InternalExchange>) => void;
    export const dotnetUpdateAllInternals:() => void;
    export const dotnetUpdateModuleInternals:() => void;
}
