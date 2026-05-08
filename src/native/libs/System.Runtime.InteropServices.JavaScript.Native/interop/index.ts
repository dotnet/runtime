// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, RuntimeAPI, RuntimeExports, RuntimeExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";

import GitHash from "consts:gitHash";

import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import {
    bindJSImportST, dynamicImport, getDotnetInstance, getGlobalThis, getProperty, getTypeOfProperty, hasProperty,
    invokeJSFunction, invokeJSImportST, setModuleImports, setProperty
} from "./invoke-js";
import { bindCsFunction, getAssemblyExports } from "./invoke-cs";
import { initializeMarshalersToJs, resolveOrRejectPromise } from "./marshal-to-js";
import { initializeMarshalersToCs } from "./marshal-to-cs";
import { forceDisposeProxies, releaseCSOwnedObject } from "./gc-handles";
import { cancelPromise } from "./cancelable-promise";
import { loadLazyAssembly, loadSatelliteAssemblies } from "./lazy";
import { jsInteropState } from "./marshal";
import { initializeScheduling, abortInteropTimers } from "./scheduling";
import { wsAbort, wsClose, wsCreate, wsGetState, wsOpen, wsReceive, wsSend } from "./web-socket";
import {
    httpSupportsStreamingRequest, httpSupportsStreamingResponse, httpCreateController, httpGetResponseType,
    httpGetResponseStatus, httpAbort, httpTransformStreamWrite, httpTransformStreamClose, httpFetch,
    httpFetchStream, httpFetchBytes, httpGetResponseHeaderNames, httpGetResponseHeaderValues, httpGetResponseBytes,
    httpGetResponseLength, httpGetStreamedResponseBytes,
} from "./http";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== GitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, Runtime: ${GitHash}`);
    }

    const runtimeApiLocal: Partial<RuntimeAPI> = {
        getAssemblyExports,
        setModuleImports,
    };
    Object.assign(runtimeApi, runtimeApiLocal);
    Object.assign(runtimeApi.INTERNAL, {
        hasProperty,
        getTypeOfProperty,
        getProperty,
        setProperty,
        getGlobalThis,
        getDotnetInstance,
        dynamicImport,
        bindCsFunction,
        loadSatelliteAssemblies,
        loadLazyAssembly,

        // WebSocket
        wsCreate,
        wsOpen,
        wsSend,
        wsReceive,
        wsClose,
        wsAbort,
        wsGetState,

        // HTTP
        httpSupportsStreamingRequest,
        httpSupportsStreamingResponse,
        httpCreateController,
        httpGetResponseType,
        httpGetResponseStatus,
        httpAbort,
        httpTransformStreamWrite,
        httpTransformStreamClose,
        httpFetch,
        httpFetchStream,
        httpFetchBytes,
        httpGetResponseHeaderNames,
        httpGetResponseHeaderValues,
        httpGetResponseBytes,
        httpGetResponseLength,
        httpGetStreamedResponseBytes,
    });

    internals[InternalExchangeIndex.RuntimeExportsTable] = runtimeExportsToTable({
        bindJSImportST,
        invokeJSImportST,
        releaseCSOwnedObject,
        resolveOrRejectPromise,
        cancelPromise,
        invokeJSFunction,
        forceDisposeProxies,
        abortInteropTimers,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    initializeMarshalersToJs();
    initializeMarshalersToCs();
    initializeScheduling();
    jsInteropState.isInitialized = true;
    jsInteropState.enablePerfMeasure = globalThis.performance && typeof globalThis.performance.measure === "function";

    function runtimeExportsToTable(map: RuntimeExports): RuntimeExportsTable {
        // keep in sync with runtimeExportsFromTable()
        return [
            map.bindJSImportST,
            map.invokeJSImportST,
            map.releaseCSOwnedObject,
            map.resolveOrRejectPromise,
            map.cancelPromise,
            map.invokeJSFunction,
            map.forceDisposeProxies,
            map.abortInteropTimers,
        ];
    }
}
