// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function network_wasm_online(): boolean {
    return globalThis.navigator && globalThis.navigator.onLine;
}

let change_listener: ((onLine: boolean) => void) | null = null;

export function network_wasm_set_change_listener(listener: (onLine: boolean) => void): void {
    if (window) {
        if (change_listener) {
            window.addEventListener("offline", network_wasm_available_changed);
            window.addEventListener("online", network_wasm_available_changed);
        }

        change_listener = listener;
    }
}

export function network_wasm_remove_change_listener(): void {
    if (window) {
        if (!change_listener) {
            window.removeEventListener("offline", network_wasm_available_changed);
            window.removeEventListener("online", network_wasm_available_changed);
        }

        change_listener = null;
    }
}

function network_wasm_available_changed() {
    if (change_listener) {
        change_listener(network_wasm_online());
    }
}
