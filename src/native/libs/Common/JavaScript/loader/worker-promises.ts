// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseCompletionSource } from "./types";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { allocatePromiseId, WorkerResultKind } from "./shared-signaling";
import type { WorkerValue, WorkerReturnType } from "./worker-values";
import { validateWorkerValue, decodeWorkerValue } from "./worker-values";
import type { MemoryViewManager } from "./shared-signaling";
import { MarshalerType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
import { workerReferences } from "./worker-references";

export type WorkerPromiseMessage = {
    type: "async-import" | "async-export";
    requestId: number;
} & ({ result: WorkerValue } | { encodedResult: string | null } | { error: string } | { encodedError: string });

export function createWorkerPromises(memory?: MemoryViewManager) {
    const reserved = new Set<number>();
    const pending = new Map<number, { completion: PromiseCompletionSource<WorkerValue>, type: WorkerReturnType }>();
    let stopped: Error | undefined;
    return {
        reserve(): number {
            if (stopped) throw stopped;
            let id: number;
            do {
                id = allocatePromiseId();
            } while (reserved.has(id));
            reserved.add(id);
            return id;
        },
        promise(id: number, type: WorkerReturnType = MarshalerType.Int32): Promise<WorkerValue> {
            if (stopped) throw stopped;
            if (!reserved.has(id) || pending.has(id)) throw new Error("withWorker(): invalid asynchronous request");
            const release = (): void => {
                pending.delete(id);
                reserved.delete(id);
            };
            const completion = createPromiseCompletionSource<WorkerValue>(release, release);
            pending.set(id, { completion, type });
            return completion.promise;
        },
        release(id: number): void {
            reserved.delete(id);
        },
        complete(message: WorkerPromiseMessage): void {
            const request = pending.get(message.requestId);
            if (!request) return; // Late completion after shutdown.
            const { completion, type } = request;
            pending.delete(message.requestId);
            reserved.delete(message.requestId);
            if ("encodedError" in message) {
                try {
                    if (!memory) throw new Error("withWorker(): exception transport is missing");
                    completion.reject(workerReferences(memory).decode(message.encodedError));
                } catch (error) {
                    completion.reject(error);
                }
            } else if ("error" in message) completion.reject(new Error(message.error));
            else {
                try {
                    if ("encodedResult" in message) {
                        if (!memory) throw new Error("withWorker(): value transport is missing");
                        completion.resolve(decodeWorkerValue(memory, message.encodedResult, type));
                    } else completion.resolve(validateWorkerValue(message.result, type));
                } catch (error) {
                    completion.reject(error);
                }
            }
        },
        stop(error: Error): void {
            stopped = error;
            for (const { completion } of pending.values()) completion.reject(error);
            pending.clear();
            reserved.clear();
        },
    };
}

export function captureWorkerPromise(
    value: unknown, requestId: number, type: WorkerPromiseMessage["type"],
    active: Set<number>, post: (message: WorkerPromiseMessage) => void,
    onFailure: (error: Error) => void,
    returnType: WorkerReturnType = MarshalerType.Int32,
    encode?: (value: WorkerValue) => string | null,
    encodeError?: (error: unknown) => string,
): WorkerResultKind {
    if (value === null || value === undefined) return WorkerResultKind.Null;
    if ((typeof value !== "object" && typeof value !== "function") || !("then" in value) || typeof value.then !== "function") {
        throw new Error("Value is not a Promise");
    }
    active.add(requestId);
    function complete(message: WorkerPromiseMessage): void {
        if (!active.delete(requestId)) return;
        try {
            post(message);
        } catch (error) {
            onFailure(error instanceof Error ? error : new Error(String(error)));
        }
    }
    function reject(error: unknown): void {
        if (!active.has(requestId)) return;
        try {
            complete(encodeError ? { type, requestId, encodedError: encodeError(error) }
                : { type, requestId, error: error instanceof Error ? error.message : String(error) });
        } catch (error) {
            active.delete(requestId);
            onFailure(error instanceof Error ? error : new Error(String(error)));
        }
    }
    try {
        // Native Promise brand checking also accepts promises from another realm.
        // Arbitrary thenables may complete inline, before the native Task holder is safe.
        Reflect.apply(Promise.prototype.then, value, [(result: unknown) => {
            if (!active.has(requestId)) return;
            try {
                const value = validateWorkerValue(result, returnType);
                complete(encode ? { type, requestId, encodedResult: encode(value) } : { type, requestId, result: value });
            } catch (error) {
                reject(error);
            }
        }, reject]);
    } catch (error) {
        active.delete(requestId);
        throw error;
    }
    return WorkerResultKind.Promise;
}
