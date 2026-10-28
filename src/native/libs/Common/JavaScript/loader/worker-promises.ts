// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseCompletionSource } from "./types";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { allocatePromiseId, WorkerResultKind } from "./shared-signaling";
import type { WorkerValue, WorkerValueType } from "./worker-values";
import { validateWorkerValue } from "./worker-values";
import { MarshalerType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";

export type WorkerPromiseMessage = {
    type: "async-import" | "async-export";
    requestId: number;
} & ({ result: WorkerValue } | { error: string });

export function createWorkerPromises() {
    const reserved = new Set<number>();
    const pending = new Map<number, { completion: PromiseCompletionSource<WorkerValue>, type: WorkerValueType }>();
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
        promise(id: number, type: WorkerValueType = MarshalerType.Int32): Promise<WorkerValue> {
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
            if ("error" in message) completion.reject(new Error(message.error));
            else {
                try {
                    completion.resolve(validateWorkerValue(message.result, type));
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
    returnType: WorkerValueType = MarshalerType.Int32,
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
    try {
        // Native Promise brand checking also accepts promises from another realm.
        // Arbitrary thenables may complete inline, before the native Task holder is safe.
        Reflect.apply(Promise.prototype.then, value, [(result: unknown) => {
            try {
                complete({ type, requestId, result: validateWorkerValue(result, returnType) });
            } catch (error) {
                complete({ type, requestId, error: error instanceof Error ? error.message : String(error) });
            }
        }, (error: unknown) => complete({ type, requestId, error: error instanceof Error ? error.message : String(error) })]);
    } catch (error) {
        active.delete(requestId);
        throw error;
    }
    return WorkerResultKind.Promise;
}
