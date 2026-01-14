// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, NativeBrowserExports, NativeBrowserExportsTable } from "../types";
import { InternalExchangeIndex } from "../types";

import { _ems_ } from "../../Common/JavaScript/ems-ambient";
import GitHash from "consts:gitHash";

export { SystemJS_RandomBytes } from "./crypto";
export { SystemJS_GetLocaleInfo } from "./globalization-locale";
export { SystemJS_RejectMainPromise, SystemJS_ResolveMainPromise, SystemJS_ConsoleClear } from "./main";
export { SystemJS_ScheduleTimer, SystemJS_ScheduleBackgroundJob } from "./timer";

export const gitHash = GitHash;
export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");

    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== _ems_.DOTNET.gitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, DOTNET: ${_ems_.DOTNET.gitHash}`);
    }

    internals[InternalExchangeIndex.NativeBrowserExportsTable] = nativeBrowserExportsToTable({
    });
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function nativeBrowserExportsToTable(map: NativeBrowserExports): NativeBrowserExportsTable {
        // keep in sync with nativeBrowserExportsFromTable()
        return [
        ];
    }
}
