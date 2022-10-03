// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import type { PromiseAndController } from "../../promise-controller";
import type {
    RemoveCommandSetAndId,
    EventPipeCommandCollectTracing2,
    EventPipeCommandStopTracing,
} from "../server_pthread/protocol-client-commands";


export type FilterPredicate = (data: ArrayBuffer) => boolean;

export interface MockScriptConnection {
    waitForSend(filter: FilterPredicate): Promise<void>;
    waitForSend<T>(filter: FilterPredicate, extract: (data: ArrayBuffer) => T): Promise<T>;
    reply(data: ArrayBuffer): void;
}

interface MockEnvironmentCommand {
    makeEventPipeCollectTracing2(payload: RemoveCommandSetAndId<EventPipeCommandCollectTracing2>): Uint8Array;
    makeEventPipeStopTracing(payload: RemoveCommandSetAndId<EventPipeCommandStopTracing>): Uint8Array;
    makeProcessResumeRuntime(): Uint8Array;
}

interface MockEnvironmentReply {

    expectOk(extraPayload?: number): FilterPredicate;

    extractOkSessionID(data: ArrayBuffer): number;

}

export interface MockEnvironment {
    createPromiseController<T>(): PromiseAndController<T>;
    delay: (ms: number) => Promise<void>;
    command: MockEnvironmentCommand;
    reply: MockEnvironmentReply;
    expectAdvertise: FilterPredicate;
}
