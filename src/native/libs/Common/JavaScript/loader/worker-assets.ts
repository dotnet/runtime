// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { Asset, AssetEntryInternal } from "./types";
import { loadResourceFetch, resolveModuleResource } from "./assets";
import { deserializeWorkerError, serializeWorkerError } from "./worker-errors";
import type { WorkerError } from "./worker-errors";

type WorkerAsset = Pick<AssetEntryInternal, "name" | "behavior" | "resolvedUrl" | "hash" | "cache" | "useCredentials" | "workerAssetId">;
export interface WorkerResourceRequest {
    type: "resource";
    asset: WorkerAsset;
    module: boolean;
    port: MessagePort;
}
type WorkerResourceResult =
    | { type: "url"; url: string }
    | {
        type: "response"; body: ReadableStream<Uint8Array> | null; status: number; statusText: string;
        headers: [string, string][]; url: string; redirected: boolean; responseType: ResponseType; resolvedUrl?: string;
    };
type WorkerResourceReply = WorkerResourceResult | { type: "error"; error: WorkerError };

function requestResource(asset: AssetEntryInternal, module: boolean): Promise<WorkerResourceResult> {
    return new Promise((resolve, reject) => {
        const { port1, port2 } = new MessageChannel();
        const close = () => {
            port1.close();
            port2.close();
        };
        port1.onmessage = (event: MessageEvent<WorkerResourceReply>) => {
            close();
            const result = event.data;
            if (result.type === "error") reject(deserializeWorkerError(result.error));
            else resolve(result);
        };
        port1.onmessageerror = () => {
            close();
            reject(new Error("runInWorker: invalid resource response"));
        };
        const request: WorkerResourceRequest = {
            type: "resource", module, port: port2,
            asset: {
                name: asset.name, behavior: asset.behavior, resolvedUrl: asset.resolvedUrl,
                hash: asset.hash, cache: asset.cache, useCredentials: asset.useCredentials,
                workerAssetId: asset.workerAssetId,
            },
        };
        try {
            self.postMessage(request, { transfer: [port2] });
        } catch (error) {
            close();
            reject(error);
        }
    });
}

export async function resolveWorkerModule(asset: AssetEntryInternal): Promise<string> {
    const result = await requestResource(asset, true);
    if (result.type !== "url") throw new Error("runInWorker: expected a module URL");
    return result.url;
}

export async function loadWorkerResource(asset: AssetEntryInternal): Promise<Response> {
    const result = await requestResource(asset, false);
    if (result.type !== "response") throw new Error("runInWorker: expected a resource response");
    asset.resolvedUrl = result.resolvedUrl;
    const response = result.status === 0 ? Response.error() : new Response(result.body, {
        status: result.status, statusText: result.statusText, headers: result.headers,
    });
    Object.defineProperties(response, {
        url: { value: result.url }, redirected: { value: result.redirected }, type: { value: result.responseType },
    });
    return response;
}

export async function serveWorkerResource(request: WorkerResourceRequest, signal: AbortSignal, assets?: ReadonlyMap<number, Asset>): Promise<void> {
    try {
        signal.throwIfAborted();
        const source = request.asset.workerAssetId === undefined ? undefined : assets?.get(request.asset.workerAssetId);
        const asset = source ? Object.assign(source, request.asset) : request.asset;
        if (request.module) {
            request.port.postMessage({ type: "url", url: resolveModuleResource(asset) } satisfies WorkerResourceReply);
        } else {
            const response = await loadResourceFetch(asset, signal);
            if (signal.aborted) {
                await response.body?.cancel(signal.reason);
                return;
            }
            const headers: [string, string][] = [];
            response.headers.forEach((value, name) => headers.push([name, value]));
            const reply: WorkerResourceReply = {
                type: "response", body: response.body, status: response.status, statusText: response.statusText,
                headers, url: response.url,
                redirected: response.redirected, responseType: response.type, resolvedUrl: asset.resolvedUrl,
            };
            request.port.postMessage(reply, response.body ? [response.body] : []);
        }
    } catch (error) {
        if (!signal.aborted) {
            request.port.postMessage({ type: "error", error: serializeWorkerError(error) } satisfies WorkerResourceReply);
        }
    } finally {
        request.port.close();
    }
}
