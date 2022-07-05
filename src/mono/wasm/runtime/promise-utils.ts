// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// Make a promise that resolves after a given number of milliseconds.
export function delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

/// A PromiseController encapsulates a Promise together with easy access to its resolve and reject functions.
/// It's a bit like a CancelationTokenSource in .NET
export class PromiseController<T = void> {
    readonly promise: Promise<T>;
    readonly resolve: (value: T | PromiseLike<T>) => void;
    readonly reject: (reason: any) => void;
    constructor() {
        let rs: (value: T | PromiseLike<T>) => void = undefined as any;
        let rj: (reason: any) => void = undefined as any;
        this.promise = new Promise<T>((resolve, reject) => {
            rs = resolve;
            rj = reject;
        });
        this.resolve = rs;
        this.reject = rj;
    }
}
