// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { ControllablePromise, PromiseAndController, PromiseController } from "../types/internal";

/// a unique symbol used to mark a promise as controllable
export const promise_control_symbol = Symbol.for("wasm promise_control");

/// Creates a new promise together with a controller that can be used to resolve or reject that promise.
/// Optionally takes callbacks to be called immediately after a promise is resolved or rejected.
export function createPromiseController<T>(afterResolve?: () => void, afterReject?: () => void): PromiseAndController<T> {
    let promise_control: PromiseController<T> = null as unknown as PromiseController<T>;
    const promise = new Promise<T>(function (resolve, reject) {
        promise_control = {
            isDone: false,
            promise: null as unknown as Promise<T>,
            resolve: (data: T | PromiseLike<T>) => {
                if (!promise_control!.isDone) {
                    promise_control!.isDone = true;
                    resolve(data);
                    if (afterResolve) {
                        afterResolve();
                    }
                }
            },
            reject: (reason: any) => {
                if (!promise_control!.isDone) {
                    promise_control!.isDone = true;
                    reject(reason);
                    if (afterReject) {
                        afterReject();
                    }
                }
            }
        };
    });
    (<any>promise_control).promise = promise;
    const controllablePromise = promise as ControllablePromise<T>;
    (controllablePromise as any)[promise_control_symbol] = promise_control;
    return { promise: controllablePromise, promise_control: promise_control };
}

export function getPromiseController<T>(promise: ControllablePromise<T>): PromiseController<T>;
export function getPromiseController<T>(promise: Promise<T>): PromiseController<T> | undefined {
    return (promise as any)[promise_control_symbol];
}

export function isControllablePromise<T>(promise: Promise<T>): promise is ControllablePromise<T> {
    return (promise as any)[promise_control_symbol] !== undefined;
}

export function assertIsControllablePromise<T>(promise: Promise<T>): asserts promise is ControllablePromise<T> {
    mono_assert(isControllablePromise(promise), "Promise is not controllable");
}
