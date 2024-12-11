// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// @ts-check

/**
 * @typedef { import("../../../browser/runtime/diagnostics-mock").MockEnvironment } MockEnvironment
 * @typedef { import("../../../browser/runtime/diagnostics-mock").MockScriptConnection } MockScriptConnection
 * @typedef { import("../../../browser/runtime/diagnostics-mock").PromiseAndController } PromiseAndController
 * @typedef { import("../../../browser/runtime/diagnostics-mock").PromiseAndController<number> } PromiseAndControllerNumber
 * @typedef { import("../../../browser/runtime/diagnostics-mock").PromiseAndController<void> } PromiseAndControllerVoid
 */

// "Microsoft-Windows-DotNETRuntime"
const DotNETRuntimeKeywords = {
    GC_KEYWORD: 0x1n,
    GC_HANDLE_KEYWORD: 0x2n,
    LOADER_KEYWORD: 0x8n,
    JIT_KEYWORD: 0x10n,
    APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD: 0x800n,
    CONTENTION_KEYWORD: 0x4000n,
    EXCEPTION_KEYWORD: 0x8000n,
    THREADING_KEYWORD: 0x10000n,
    TYPE_KEYWORD: 0x80000n,
    GC_HEAP_DUMP_KEYWORD: 0x100000n,
    GC_ALLOCATION_KEYWORD: 0x200000n,
    GC_MOVES_KEYWORD: 0x400000n,
    GC_HEAP_COLLECT_KEYWORD: 0x800000n,
    GC_HEAP_AND_TYPE_NAMES_KEYWORD: 0x1000000n,
    GC_FINALIZATION_KEYWORD: 0x1000000n,
    GC_RESIZE_KEYWORD: 0x2000000n,
    GC_ROOT_KEYWORD: 0x4000000n,
    GC_HEAP_DUMP_VTABLE_CLASS_REF_KEYWORD: 0x8000000n,
    METHOD_TRACING_KEYWORD: 0x20000000n,
    TYPE_DIAGNOSTIC_KEYWORD: 0x8000000000n,
    TYPE_LOADING_KEYWORD: 0x8000000000n,
    MONITOR_KEYWORD: 0x10000000000n,
    METHOD_INSTRUMENTATION_KEYWORD: 0x40000000000n,
}

/**
 * @returns {[number,number]}
 * */
function lo_hi(value) {
    return [Number(value & 0xFFFFFFFFn), Number(value >> 32n)]
}

/**
 * @param {MockEnvironment} env
 * @returns {((conn: MockScriptConnection) => Promise<void>)[]}
 * */
function script(env) {
    /** @type { PromiseAndControllerNumber } */
    const sessionStarted = env.createPromiseController();
    /** @type { PromiseAndControllerVoid } */
    const runtimeResumed = env.createPromiseController();
    /** @type { PromiseAndControllerVoid } */
    const waitForever = env.createPromiseController();

    /** @type { PromiseAndControllerVoid } */
    const fibonacciDone = env.createPromiseController();
    env.addEventListenerFromBrowser("fibonacci-done", (event) => {
        fibonacciDone.promise_control.resolve();
    });

    return [
        async (conn) => {
            await Promise.all([conn.waitForSend(env.expectAdvertise)]);
            console.warn("resuming runtime");
            conn.reply(env.command.makeProcessResumeRuntime());
            runtimeResumed.promise_control.resolve();
        },
        async (conn) => {
            try {
                await conn.waitForSend(env.expectAdvertise);
                await runtimeResumed.promise;
                console.warn("session start");
                conn.reply(env.command.makeEventPipeCollectTracing2({
                    circularBufferMB: 256,
                    format: 1,
                    requestRundown: false,
                    providers: [
                        {
                            keywords: [0xFFFFFFFF, 0xFFFFFFFF],
                            logLevel: 4,
                            provider_name: "Microsoft-Windows-DotNETRuntime",
                            filter_data: null
                        },
                        {
                            keywords: [0xFFFFFFFF, 0xFFFFFFFF],
                            logLevel: 4,
                            provider_name: "Microsoft-Windows-DotNETRuntimePrivate",
                            filter_data: null
                        }
                    ]
                }));
                let sessionID = undefined;
                const buffer = new SharedArrayBuffer(20_000_000);
                const view = new Uint8Array(buffer);
                let length = 0;
                await conn.processSend((bytes) => {
                    if (sessionID === undefined) {
                        // first block is just a session handshake
                        if (!env.reply.expectOk(4)) {
                            throw new Error("bad data");
                        }
                        sessionID = env.reply.extractOkSessionID(bytes)
                        sessionStarted.promise_control.resolve(sessionID);
                        env.postMessageToBrowser({ cmd: "collecting" });
                    }
                    else {
                        const bytesView = new Uint8Array(bytes)
                        view.set(bytesView, length);
                        length += bytesView.byteLength;
                    }
                });
                console.warn("final totalBytesStreamed " + length);
                env.postMessageToBrowser({ cmd: "collected", buffer, length });
            }
            catch (err) {
                console.error(err)
            }
        },
        async (conn) => {
            await Promise.all([conn.waitForSend(env.expectAdvertise), runtimeResumed.promise, sessionStarted.promise, fibonacciDone.promise]);
            const sessionID = await sessionStarted.promise;
            console.warn("delaying 5 seconds before stopping tracing");
            await env.delay(5000);
            console.warn("delaying done, stopping tracing");
            conn.reply(env.command.makeEventPipeStopTracing({ sessionID }));
        },
        async (conn) => {
            await conn.waitForSend(env.expectAdvertise);
            await waitForever.promise;
        }
    ];
};

export default script;
