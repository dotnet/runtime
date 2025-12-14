// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { } from "../../../libs/Common/JavaScript/cross-linked";
import type { VoidPtr } from "./types";

declare global {
    export const BROWSER_HOST: any;
    export function _BrowserHost_ExecuteAssembly(mainAssemblyNamePtr: number, argsLength: number, argsPtr: number): number;
    export function _wasm_load_icu_data(dataPtr: VoidPtr): number;
}
