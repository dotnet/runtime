// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// A Promise<T> that guards against multiple-resolve, multiple-reject, reject-after-accept and accept-after-reject.
class GuardedPromise<T> extends Promise<T> {
    constructor(executor: (resolve: (value: T | PromiseLike<T>) => void, reject: (reason?: any) => void) => void) {
        super((resolve, reject) => {
            let resolved = false;
            let rejected = false;
            executor((value: T | PromiseLike<T>) => {
                if (resolved) {
                    throw new Error("Promise resolved more than once");
                }
                if (rejected) {
                    throw new Error("Can not resolve a Promise after it has been rejected");
                }
                resolved = true;
                resolve(value);
            }, (reason: any) => {
                if (resolved) {
                    throw new Error("Can not reject a Promise after it has been resolved");
                }
                if (rejected) {
                    throw new Error("Promise rejected more than once");
                }
                rejected = true;
                reject(reason);
            });
        });
    }
}

export default GuardedPromise;

