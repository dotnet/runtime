// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WeakRefInternal } from "./types";

export const useWeakRef = typeof globalThis.WeakRef === "function";

export function createWeakRef<T extends object>(jsObj: T): WeakRefInternal<T> {
    if (useWeakRef) {
        return new WeakRef(jsObj);
    } else {
        // this is trivial WeakRef replacement, which holds strong reference, instead of weak one, when the browser doesn't support it
        return createStrongRef(jsObj);
    }
}

export function createStrongRef<T extends object>(jsObj: T): WeakRefInternal<T> {
    return <any>{
        deref: () => {
            return jsObj;
        },
        dispose: () => {
            jsObj = null!;
        }
    };
}
