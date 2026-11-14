// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JSHandle, RuntimeExportsTable } from "./types";
import type { VoidPtr } from "../types";
import type { WorkerRuntimeContext } from "../../Common/JavaScript/loader/worker-native";
import { NativeOperation, RuntimeOperation } from "../../Common/JavaScript/loader/worker-native";
import { deserializeWorkerError, serializeWorkerError } from "../../Common/JavaScript/loader/worker-errors";
import { dotnetApi, dotnetBrowserUtilsExports, Module } from "./cross-module";
import { bindJSImportST, getProperty, getTypeOfProperty, hasProperty, invokeJSFunction, invokeJSImportST, setProperty } from "./invoke-js";
import { cancelPromise } from "./cancelable-promise";
import { forceDisposeProxies, getJSObjectFromJSHandle, getProxyCounts, lookupJsOwnedObject, releaseCSOwnedObject } from "./gc-handles";
import { resolveOrRejectPromise } from "./marshal-to-js";
import { getArg, getArgJsHandle, getSignatureFunctionName, getSignatureHandle, getSignatureModuleName } from "./marshal";
import { abortInteropTimers } from "./scheduling";

export function initializeErrorExchange(context: WorkerRuntimeContext): void {
    context.getErrorSnapshot = (handle, output) => {
        const value = JSON.stringify(serializeWorkerError(getJSObjectFromJSHandle(handle), true));
        const pointer = dotnetBrowserUtilsExports.stringToUTF16Ptr(value);
        const view = dotnetApi.localHeapViewU32();
        view[output >>> 2] = Number(pointer);
        view[(output >>> 2) + 1] = value.length * 2;
    };
    context.getRemoteError = handle => {
        const output = Module._malloc(8);
        let pointer = 0;
        try {
            dotnetApi.setHeapU32(output, 0);
            context.invoke(context.role === "ui" ? NativeOperation.GetErrorSnapshot : RuntimeOperation.GetErrorSnapshot,
                handle, Number(output));
            pointer = dotnetApi.getHeapU32(output);
            const length = dotnetApi.localHeapViewU32()[(Number(output) >>> 2) + 1];
            const value = dotnetBrowserUtilsExports.utf16ToString(pointer, pointer + length);
            return deserializeWorkerError(JSON.parse(value));
        } finally {
            if (pointer) Module._free(pointer as unknown as VoidPtr);
            Module._free(output);
        }
    };
}

export function createWorkerInterop(context: WorkerRuntimeContext): RuntimeExportsTable {
    const imports = new Map<number, { local: boolean; property: boolean }>();
    const propertyFunctions = new Set(["getProperty", "setProperty", "hasProperty", "getTypeOfProperty"]);
    Object.assign(dotnetApi.INTERNAL.TARGET ??= {}, { getProperty, setProperty, hasProperty, getTypeOfProperty });
    const call = context.invoke;
    const owns = (handle: JSHandle) => Number(handle) > 0 ? (Number(handle) & 1) === 0 : getJSObjectFromJSHandle(handle) != null;

    return [
        signature => {
            const name = getSignatureFunctionName(signature)!;
            const module = getSignatureModuleName(signature);
            const local = !module && name.startsWith("INTERNAL.") && !name.startsWith("INTERNAL.TARGET.");
            const property = !module && name.startsWith("INTERNAL.TARGET.") && propertyFunctions.has(name.substring("INTERNAL.TARGET.".length));
            let error = local || property ? Number(bindJSImportST(signature)) : 0;
            if (!error && !local) error = call(RuntimeOperation.BindJSImport, Number(signature));
            if (!error) imports.set(getSignatureHandle(signature), { local, property });
            return error as unknown as VoidPtr;
        },
        (handle, args) => {
            const entry = imports.get(Number(handle));
            if (entry?.local || entry?.property && owns(getArgJsHandle(getArg(args, 2)))) {
                invokeJSImportST(handle, args);
            } else call(RuntimeOperation.InvokeJSImport, Number(handle), Number(args));
        },
        handle => {
            if (owns(handle)) releaseCSOwnedObject(handle);
            else call(RuntimeOperation.ReleaseCSOwnedObject, Number(handle));
        },
        args => {
            const handle = getArgJsHandle(getArg(args, 2));
            if (owns(handle)) resolveOrRejectPromise(args);
            else call(RuntimeOperation.ResolveOrRejectPromise, Number(args));
        },
        handle => {
            if (lookupJsOwnedObject(handle)) cancelPromise(handle);
            else call(RuntimeOperation.CancelPromise, Number(handle));
        },
        (handle, args) => {
            if (owns(handle)) invokeJSFunction(handle, args);
            else call(RuntimeOperation.InvokeJSFunction, Number(handle), Number(args));
        },
        (methods, verbose) => {
            forceDisposeProxies(methods, verbose);
            call(RuntimeOperation.ForceDisposeProxies, Number(methods), Number(verbose));
        },
        () => {
            abortInteropTimers();
            call(RuntimeOperation.AbortInteropTimers);
        },
    ];
}

export function getCombinedProxyCounts(context: WorkerRuntimeContext): number[] {
    const pointer = Module._malloc(20);
    try {
        context.invoke(RuntimeOperation.GetProxyCounts, Number(pointer));
        const view = dotnetApi.localHeapViewI32();
        return getProxyCounts().map((count, index) => count + view[(Number(pointer) >>> 2) + index]);
    } finally {
        Module._free(pointer);
    }
}
