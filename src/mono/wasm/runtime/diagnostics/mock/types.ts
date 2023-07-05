// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import type { PromiseAndController } from "../../types/internal";
import type {
    RemoveCommandSetAndId,
    EventPipeCommandCollectTracing2,
    EventPipeCommandStopTracing,
} from "../server_pthread/protocol-client-commands";


export type FilterPredicate = (data: ArrayBuffer) => boolean;

export interface MockScriptConnection {
    waitForSend(filter: FilterPredicate): Promise<void>;
    waitForSend<T>(filter: FilterPredicate, extract: (data: ArrayBuffer) => T): Promise<T>;
    processSend(onMessage: (data: ArrayBuffer) => any): Promise<void>;
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
    postMessageToBrowser(message: any, transferable?: Transferable[]): void;
    addEventListenerFromBrowser(cmd: string, listener: (data: any) => void): void;
    createPromiseController<T>(): PromiseAndController<T>;
    delay: (ms: number) => Promise<void>;
    command: MockEnvironmentCommand;
    reply: MockEnvironmentReply;
    expectAdvertise: FilterPredicate;
}
