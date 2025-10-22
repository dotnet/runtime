// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { ControllablePromise, PromiseController } from "./types";

/// a unique symbol used to mark a promise as controllable
export const promiseControlSymbol = Symbol.for("wasm promise control");

// WASM-TODO: PromiseCompletionSource

/// Creates a new promise together with a controller that can be used to resolve or reject that promise.
/// Optionally takes callbacks to be called immediately after a promise is resolved or rejected.
export function createPromiseController<T>(afterResolve?: () => void, afterReject?: () => void): PromiseController<T> {
    let promiseControl: PromiseController<T> = null as unknown as PromiseController<T>;
    const promise = new Promise<T>((resolve, reject) => {
        promiseControl = {
            isDone: false,
            promise: null as unknown as ControllablePromise<T>,
            resolve: (data: T | PromiseLike<T>) => {
                if (!promiseControl!.isDone) {
                    promiseControl!.isDone = true;
                    resolve(data);
                    if (afterResolve) {
                        afterResolve();
                    }
                }
            },
            reject: (reason: any) => {
                if (!promiseControl!.isDone) {
                    promiseControl!.isDone = true;
                    reject(reason);
                    if (afterReject) {
                        afterReject();
                    }
                }
            },
            propagateFrom: (other: Promise<T>) => {
                other.then(promiseControl!.resolve).catch(promiseControl!.reject);
            }
        };
    });
    (<any>promiseControl).promise = promise;
    const controllablePromise = promise as ControllablePromise<T>;
    (controllablePromise as any)[promiseControlSymbol] = promiseControl;
    return promiseControl;
}

export function getPromiseController<T>(promise: ControllablePromise<T>): PromiseController<T>;
export function getPromiseController<T>(promise: Promise<T>): PromiseController<T> | undefined {
    return (promise as any)[promiseControlSymbol];
}

export function isControllablePromise<T>(promise: Promise<T>): promise is ControllablePromise<T> {
    return (promise as any)[promiseControlSymbol] !== undefined;
}

