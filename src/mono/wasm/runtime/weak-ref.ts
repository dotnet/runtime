// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WeakRefInternal } from "./types/internal";

export const _use_weak_ref = typeof globalThis.WeakRef === "function";

export function create_weak_ref<T extends object>(js_obj: T): WeakRefInternal<T> {
    if (_use_weak_ref) {
        return new WeakRef(js_obj);
    }
    else {
        // this is trivial WeakRef replacement, which holds strong refrence, instead of weak one, when the browser doesn't support it
        return <any>{
            deref: () => {
                return js_obj;
            },
            dispose: () => {
                js_obj = null!;
            }
        };
    }
}
