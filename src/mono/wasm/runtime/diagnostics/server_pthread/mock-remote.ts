// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mock, MockScriptEngine } from "../mock";
import { PromiseController } from "../../promise-utils";

function expectAdvertise(data: string | ArrayBuffer) { return data === "ADVR_V1"; }

const scriptPC = new PromiseController();
const scriptPCunfulfilled = new PromiseController();

const script: ((engine: MockScriptEngine) => Promise<void>)[] = [
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        engine.reply(JSON.stringify({
            command_set: "EventPipe", command: "CollectTracing2",
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
        scriptPC.resolve();
    },
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        await scriptPC.promise;
        engine.reply(JSON.stringify({ "command_set": "Process", "command": "ResumeRuntime" }));
        // engine.close();
    },
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        await scriptPCunfulfilled.promise;
    }
];

/// a mock script that simulates the initial part of the diagnostic server protocol
export const mockScript = mock(script, { trace: true });
