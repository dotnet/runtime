// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// @ts-check

/**
 * @typedef { import("../../../wasm/runtime/diagnostics-mock").MockEnvironment } MockEnvironment
 * @typedef { import("../../../wasm/runtime/diagnostics-mock").MockScriptConnection } MockScriptConnection
 * @typedef { import("../../../wasm/runtime/diagnostics-mock").PromiseAndController } PromiseAndController
 * @typedef { import("../../../wasm/runtime/diagnostics-mock").PromiseAndController<number> } PromiseAndControllerNumber
 * @typedef { import("../../../wasm/runtime/diagnostics-mock").PromiseAndController<void> } PromiseAndControllerVoid
 */

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
            try {
                await conn.waitForSend(env.expectAdvertise);
                conn.reply(env.command.makeEventPipeCollectTracing2({
                    circularBufferMB: 256,
                    format: 1,
                    requestRundown: true,
                    providers: [
                        {
                            keywords: [0, 0],
                            logLevel: 5,
                            provider_name: "WasmHello",
                            filter_data: "EventCounterIntervalSec=1"
                        },
                        {
                            keywords: [0, 61440],
                            logLevel: 4,
                            provider_name: "Microsoft-DotNETCore-SampleProfiler",
                            filter_data: null
                        },
                        {
                            keywords: [
                                -1051734851,
                                20
                            ],
                            logLevel: 4,
                            provider_name: "Microsoft-Windows-DotNETRuntime",
                            filter_data: null
                        }
                    ]
                }));
                let sessionID = undefined;
                const buffer = new SharedArrayBuffer(2_000_000);
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
            await Promise.all([conn.waitForSend(env.expectAdvertise), sessionStarted.promise]);
            conn.reply(env.command.makeProcessResumeRuntime());
            runtimeResumed.promise_control.resolve();
        },
        async (conn) => {
            await Promise.all([conn.waitForSend(env.expectAdvertise), runtimeResumed.promise, sessionStarted.promise, fibonacciDone.promise]);
            const sessionID = await sessionStarted.promise;
            await env.delay(5000);
            conn.reply(env.command.makeEventPipeStopTracing({ sessionID }));
        },
        async (conn) => {
            await conn.waitForSend(env.expectAdvertise);
            await waitForever.promise;
        }
    ];
};

export default script;
