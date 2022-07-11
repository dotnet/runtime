// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function network_wasm_online(): boolean {
    return globalThis.navigator && globalThis.navigator.onLine;
}