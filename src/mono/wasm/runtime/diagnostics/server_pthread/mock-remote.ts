
import { mock, MockScriptEngine } from "../mock";
import { PromiseController } from "../../promise-utils";

function expectAdvertise(data: string | ArrayBuffer) { return data === "ADVR"; }

const scriptPC = new PromiseController();
const scriptPCunfulfilled = new PromiseController();

const script: ((engine: MockScriptEngine) => Promise<void>)[] = [
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        engine.reply("start session");
        scriptPC.resolve();
    },
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        await scriptPC.promise;
        engine.reply("resume");
        // engine.close();
    },
    async (engine) => {
        await engine.waitForSend(expectAdvertise);
        await scriptPCunfulfilled.promise;
    }
];

/// a mock script that simulates the initial part of the diagnostic server protocol
export const mockScript = mock(script, { trace: true });
