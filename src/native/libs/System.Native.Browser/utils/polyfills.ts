// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function initPolyfills(): void {
    if (typeof globalThis.WeakRef !== "function") {
        class WeakRefPolyfill<T> {
            private _value: T | undefined;

            constructor(value: T) {
                this._value = value;
            }

            deref(): T | undefined {
                return this._value;
            }
        }
        globalThis.WeakRef = WeakRefPolyfill as any;
    }
}
