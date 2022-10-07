// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function isSharedArrayBuffer(x: unknown): x is SharedArrayBuffer {
    // N.B. don't use `x instanceof SharedArrayBuffer`.  If the SAB was created by another worker,
    // and then sent in a message to the current one, then it will be an instance of that worker's SharedArrayBuffer,
    // not the current globalThis.SharedArrayBuffer
    return typeof SharedArrayBuffer !== "undefined" && x?.constructor?.name === "SharedArrayBuffer";
}
