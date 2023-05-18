// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


let perThreadUnsettledPromiseCount = 0;

export function addUnsettledPromise() {
    perThreadUnsettledPromiseCount++;
}

export function settleUnsettledPromise() {
    perThreadUnsettledPromiseCount--;
}

/// Called from the C# threadpool worker loop to find out if there are any
/// unsettled JS promises that need to keep the worker alive
export function monoWasmEventLoopHasUnsettledInteropPromises(): boolean {
    return perThreadUnsettledPromiseCount > 0;
}
