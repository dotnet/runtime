// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions, RuntimeAPI } from "./types";
import { deserializeWorkerError, serializeWorkerError } from "./worker-errors";
import type { WorkerError } from "./worker-errors";

export interface LocalRuntimeAPI {
    loadLazyAssembly(assemblyName: string): Promise<boolean>;
    loadSatelliteAssemblies(cultures: string[]): Promise<void>;
    collectCpuSamples: RuntimeAPI["collectCpuSamples"];
    collectMetrics: RuntimeAPI["collectMetrics"];
    collectGcDump: RuntimeAPI["collectGcDump"];
    collectPgoTrace: RuntimeAPI["collectPgoTrace"];
    connectDSRouter: RuntimeAPI["connectDSRouter"];
    GetDotNetRuntimeContractDescriptor(): number;
    GetDotNetRuntimeHeap(pointer: number, length: number): Uint8Array;
}

type Collection = "collectCpuSamples" | "collectMetrics" | "collectGcDump" | "collectPgoTrace";
type LocalOperation =
    | { operation: "loadLazyAssembly"; assemblyName: string }
    | { operation: "loadSatelliteAssemblies"; cultures: string[] }
    | { operation: Collection; options?: DiagnosticCommandOptions };
export type WorkerLocalRequest = LocalOperation & { type: "local"; port: MessagePort };
type LocalResult = boolean | Uint8Array[] | undefined;
type LocalReply = { type: "result"; result: LocalResult } | { type: "error"; error: WorkerError };

export function createWorkerLocal(
    worker: Worker, signal: AbortSignal,
    synchronous: Pick<LocalRuntimeAPI, "connectDSRouter" | "GetDotNetRuntimeContractDescriptor" | "GetDotNetRuntimeHeap">,
): LocalRuntimeAPI {
    function request(operation: LocalOperation): Promise<LocalResult> {
        return new Promise((resolve, reject) => {
            signal.throwIfAborted();
            const { port1, port2 } = new MessageChannel();
            const close = () => {
                signal.removeEventListener("abort", abort);
                port1.close();
                port2.close();
            };
            const abort = () => {
                close();
                reject(signal.reason);
            };
            port1.onmessage = (event: MessageEvent<LocalReply>) => {
                close();
                if (event.data.type === "error") reject(deserializeWorkerError(event.data.error));
                else resolve(event.data.result);
            };
            port1.onmessageerror = () => {
                close();
                reject(new Error("runInWorker: invalid LOCAL response"));
            };
            signal.addEventListener("abort", abort, { once: true });
            try {
                worker.postMessage({ ...operation, type: "local", port: port2 } satisfies WorkerLocalRequest, [port2]);
            } catch (error) {
                close();
                reject(error);
            }
        });
    }

    async function collect(operation: Collection, options?: DiagnosticCommandOptions): Promise<Uint8Array[]> {
        const result = await request({ operation, options: { ...options, skipDownload: true } });
        if (!Array.isArray(result) || !result.every(chunk => chunk instanceof Uint8Array)) {
            throw new Error("runInWorker: invalid LOCAL diagnostic data");
        }
        return result;
    }

    return {
        ...synchronous,
        async loadLazyAssembly(assemblyName) {
            const result = await request({ operation: "loadLazyAssembly", assemblyName });
            if (typeof result !== "boolean") throw new Error("runInWorker: invalid LOCAL assembly result");
            return result;
        },
        async loadSatelliteAssemblies(cultures) {
            const result = await request({ operation: "loadSatelliteAssemblies", cultures });
            if (result !== undefined) throw new Error("runInWorker: invalid LOCAL satellite result");
        },
        collectCpuSamples: options => collect("collectCpuSamples", options),
        collectMetrics: options => collect("collectMetrics", options),
        collectGcDump: options => collect("collectGcDump", options),
        collectPgoTrace: options => collect("collectPgoTrace", options),
    };
}

export async function serveWorkerLocal(request: WorkerLocalRequest, local: LocalRuntimeAPI): Promise<void> {
    try {
        let result: LocalResult;
        switch (request.operation) {
            case "loadLazyAssembly": result = await local.loadLazyAssembly(request.assemblyName); break;
            case "loadSatelliteAssemblies": await local.loadSatelliteAssemblies(request.cultures); result = undefined; break;
            default: result = await local[request.operation](request.options); break;
        }
        const buffers = Array.isArray(result)
            ? [...new Set(result.map(chunk => chunk.buffer).filter(buffer => buffer instanceof ArrayBuffer))]
            : [];
        request.port.postMessage({ type: "result", result } satisfies LocalReply, buffers);
    } catch (error) {
        request.port.postMessage({ type: "error", error: serializeWorkerError(error) } satisfies LocalReply);
    } finally {
        request.port.close();
    }
}
