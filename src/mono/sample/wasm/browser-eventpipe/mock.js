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
    return [
        async (conn) => {
            await conn.waitForSend(env.expectAdvertise);
            conn.reply(env.command.makeEventPipeCollectTracing2({
                circularBufferMB: 1,
                format: 1,
                requestRundown: true,
                providers: [
                    {
                        keywords: [0, 0],
                        logLevel: 5,
                        provider_name: "WasmHello",
                        filter_data: "EventCounterIntervalSec=1"
                    }
                ]
            }));
            const sessionID = await conn.waitForSend(env.reply.expectOk(4), env.reply.extractOkSessionID);
            sessionStarted.promise_control.resolve(sessionID);
        },
        async (conn) => {
            await Promise.all([conn.waitForSend(env.expectAdvertise), sessionStarted.promise]);
            conn.reply(env.command.makeProcessResumeRuntime());
            runtimeResumed.promise_control.resolve();
        },
        async (conn) => {
            await Promise.all([conn.waitForSend(env.expectAdvertise), runtimeResumed.promise, sessionStarted.promise]);
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
