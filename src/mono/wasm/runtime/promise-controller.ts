// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import {
    mono_assert
} from "./types";

/// a unique symbol used to mark a promise as controllable
export const promise_control_symbol = Symbol.for("wasm promise_control");

/// A PromiseController encapsulates a Promise together with easy access to its resolve and reject functions.
/// It's a bit like a TaskCompletionSource in .NET
export interface PromiseController<T = any> {
    isDone: boolean;
    readonly promise: Promise<T>;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}

/// A Promise<T> with a controller attached
export interface ControllablePromise<T = any> extends Promise<T> {
    [promise_control_symbol]: PromiseController<T>;
}

/// Just a pair of a promise and its controller
export interface PromiseAndController<T> {
    promise: ControllablePromise<T>;
    promise_control: PromiseController<T>;
}

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
    controllablePromise[promise_control_symbol] = promise_control;
    return { promise: controllablePromise, promise_control: promise_control };
}

export function getPromiseController<T>(promise: ControllablePromise<T>): PromiseController<T>;
export function getPromiseController<T>(promise: Promise<T>): PromiseController<T> | undefined {
    return (promise as ControllablePromise<T>)[promise_control_symbol];
}

export function isControllablePromise<T>(promise: Promise<T>): promise is ControllablePromise<T> {
    return (promise as ControllablePromise<T>)[promise_control_symbol] !== undefined;
}

export function assertIsControllablePromise<T>(promise: Promise<T>): asserts promise is ControllablePromise<T> {
    mono_assert(isControllablePromise(promise), "Promise is not controllable");
}
