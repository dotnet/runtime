// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseCompletionSource } from "./types";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { allocatePromiseId, WorkerResultKind } from "./shared-signaling";

export type WorkerPromiseMessage = {
    type: "async-import" | "async-export";
    requestId: number;
} & ({ result: number | null } | { error: string });

export function validateWorkerScalar(value: unknown): number | null {
    if (value === null || value === undefined) return null;
    if (typeof value !== "number" || !Number.isInteger(value) || value < -0x8000_0000 || value > 0x7fff_ffff) {
        throw new Error("withWorker(): asynchronous result must be an Int32");
    }
    return value;
}

export function createWorkerPromises() {
    const reserved = new Set<number>();
    const pending = new Map<number, PromiseCompletionSource<number | null>>();
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
        promise(id: number): Promise<number | null> {
            if (stopped) throw stopped;
            if (!reserved.has(id) || pending.has(id)) throw new Error("withWorker(): invalid asynchronous request");
            const release = (): void => {
                pending.delete(id);
                reserved.delete(id);
            };
            const completion = createPromiseCompletionSource<number | null>(release, release);
            pending.set(id, completion);
            return completion.promise;
        },
        release(id: number): void {
            reserved.delete(id);
        },
        complete(message: WorkerPromiseMessage): void {
            const completion = pending.get(message.requestId);
            if (!completion) return; // Late completion after shutdown.
            pending.delete(message.requestId);
            reserved.delete(message.requestId);
            if ("error" in message) completion.reject(new Error(message.error));
            else {
                try {
                    completion.resolve(validateWorkerScalar(message.result));
                } catch (error) {
                    completion.reject(error);
                }
            }
        },
        stop(error: Error): void {
            stopped = error;
            for (const completion of pending.values()) completion.reject(error);
            pending.clear();
            reserved.clear();
        },
    };
}

export function captureWorkerPromise(
    value: unknown, requestId: number, type: WorkerPromiseMessage["type"],
    active: Set<number>, post: (message: WorkerPromiseMessage) => void,
    onFailure: (error: Error) => void,
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
                complete({ type, requestId, result: validateWorkerScalar(result) });
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
