// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssertType, EmscriptenModuleInternal, JSEngineType, LoggerType, LoaderExports, InternalExchange, RuntimeExports, RuntimeExportsTable, BrowserHostExportsTable, BrowserHostExports, InteropJavaScriptExports, InteropJavaScriptExportsTable, NativeBrowserExports, NativeBrowserExportsTable } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.footer.js
// see also `reserved` in `rollup.config.defines.js`
declare global {
    export const Assert:AssertType;
    export const Logger:LoggerType;
    export const Module:EmscriptenModuleInternal;
    export const netJSEngine:JSEngineType;
    export const netLoaderExports:LoaderExports;
    export const netTabulateIJSE:(hostNativeExports:InteropJavaScriptExports) => InteropJavaScriptExportsTable;
    export const netTabulateRE:(hostNativeExports:RuntimeExports) => RuntimeExportsTable;
    export const netTabulateBHE:(hostNativeExports:BrowserHostExports) => BrowserHostExportsTable;
    export const netTabulateNBE:(hostNativeExports:NativeBrowserExports) => NativeBrowserExportsTable;
    export const netTabulateLE:(hostNativeExports:BrowserHostExports) => BrowserHostExportsTable;
    export const netSetInternals:(internals:Partial<InternalExchange>) => void;
    export const netUpdateAllInternals:() => void;
    export const netUpdateModuleInternals:() => void;
}
