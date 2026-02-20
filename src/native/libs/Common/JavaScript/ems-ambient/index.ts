// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { EmsAmbientSymbolsType } from "../types";

// we want to use the cross-module symbols defined in closure of dotnet.native.js
// which are installed there by libSystem.Native.Browser.Utils.footer.js
// see also `reserved` in `rollup.config.defines.js`

const _ems_: EmsAmbientSymbolsType = globalThis as any;
//export default emAmbientSymbols;
export { _ems_ };
