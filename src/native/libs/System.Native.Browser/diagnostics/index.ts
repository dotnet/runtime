// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticsExportsTable, InternalExchange, DiagnosticsExports } from "./types";
import { InternalExchangeIndex } from "../types";

import GitHash from "consts:gitHash";

import { dotnetApi, dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import { registerExit } from "./exit";
import { installNativeSymbols, symbolicateStackTrace } from "./symbolicate";
import { installLoggingProxy } from "./console-proxy";
import { collectMetrics } from "./dotnet-counters";
import { collectGcDump } from "./dotnet-gcdump";
import { collectCpuSamples } from "./dotnet-cpu-profiler";
import { connectDSRouter, ds_rt_websocket_close, ds_rt_websocket_create, ds_rt_websocket_poll, ds_rt_websocket_recv, ds_rt_websocket_send, initializeDS } from "./diagnostic-server";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");

    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== GitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, Diagnostics: ${GitHash}`);
    }

    internals[InternalExchangeIndex.DiagnosticsExportsTable] = diagnosticsExportsToTable({
        symbolicateStackTrace,
        installNativeSymbols,
        ds_rt_websocket_create,
        ds_rt_websocket_send,
        ds_rt_websocket_poll,
        ds_rt_websocket_recv,
        ds_rt_websocket_close,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    registerExit();
    installLoggingProxy();
    initializeDS();

    dotnetApi.collectCpuSamples = collectCpuSamples;
    dotnetApi.collectMetrics = collectMetrics;
    dotnetApi.collectGcDump = collectGcDump;
    dotnetApi.connectDSRouter = connectDSRouter;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function diagnosticsExportsToTable(map: DiagnosticsExports): DiagnosticsExportsTable {
        // keep in sync with diagnosticsExportsFromTable()
        return [
            map.symbolicateStackTrace,
            map.installNativeSymbols,
            map.ds_rt_websocket_create,
            map.ds_rt_websocket_send,
            map.ds_rt_websocket_poll,
            map.ds_rt_websocket_recv,
            map.ds_rt_websocket_close,
        ];
    }
}
