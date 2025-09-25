// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { ControllablePromise, PromiseCompletionSource } from "./types";

/// a unique symbol used to mark a promise as controllable
export const controllablePromiseSymbol = Symbol.for("ControllablePromise");

/// Creates a new promise together with a controller that can be used to resolve or reject that promise.
/// Optionally takes callbacks to be called immediately after a promise is resolved or rejected.
export function createPromiseCompletionSource<TResult>(afterResolve?: () => void, afterReject?: () => void): PromiseCompletionSource<TResult> {
    let promiseControl: PromiseCompletionSource<TResult> = null as unknown as PromiseCompletionSource<TResult>;
    const promise = new Promise<TResult>((resolve, reject) => {
        promiseControl = {
            isDone: false,
            promise: null as unknown as ControllablePromise<TResult>,
            resolve: (data: TResult | PromiseLike<TResult>) => {
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
            propagateFrom: (other: Promise<TResult>) => {
                other.then(promiseControl!.resolve).catch(promiseControl!.reject);
            }
        };
    });
    (<any>promiseControl).promise = promise;
    const controllablePromise = promise as ControllablePromise<TResult>;
    (controllablePromise as any)[controllablePromiseSymbol] = promiseControl;
    return promiseControl;
}

export function getPromiseCompletionSource<T>(promise: ControllablePromise<T>): PromiseCompletionSource<T>;
export function getPromiseCompletionSource<T>(promise: Promise<T>): PromiseCompletionSource<T> | undefined {
    return (promise as any)[controllablePromiseSymbol];
}

export function isControllablePromise<T>(promise: Promise<T>): promise is ControllablePromise<T> {
    return (promise as any)[controllablePromiseSymbol] !== undefined;
}

