// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


let _per_thread_unsettled_promise_count = 0;

export function addUnsettledPromise() {
    _per_thread_unsettled_promise_count++;
}

export function settleUnsettledPromise() {
    _per_thread_unsettled_promise_count--;
}

/// Called from the C# threadpool worker loop to find out if there are any
/// unsettled JS promises that need to keep the worker alive
export function mono_wasm_eventloop_has_unsettled_interop_promises(): boolean {
    return _per_thread_unsettled_promise_count > 0;
}
