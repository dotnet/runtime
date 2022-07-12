// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function network_wasm_online(): boolean {
    return globalThis.navigator && globalThis.navigator.onLine;
}

export function network_wasm_add_change_listener(listener: (onLine: boolean) => void): void {
    if (window) {
        window.addEventListener("offline", () => listener(network_wasm_online()));
        window.addEventListener("online", () => listener(network_wasm_online()));
    }
}
