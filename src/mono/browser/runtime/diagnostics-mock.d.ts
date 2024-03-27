//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//!
//! This is generated file, see src/mono/wasm/runtime/rollup.config.js

//! This is not considered public API with backward compatibility guarantees. 

interface PromiseController<T = any> {
    isDone: boolean;
    readonly promise: Promise<T>;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}
interface ControllablePromise<T = any> extends Promise<T> {
    __brand: "ControllablePromise";
}
interface PromiseAndController<T> {
    promise: ControllablePromise<T>;
    promise_control: PromiseController<T>;
}

interface ProtocolClientCommandBase {
    command_set: string;
    command: string;
}
interface EventPipeClientCommandBase extends ProtocolClientCommandBase {
    command_set: "EventPipe";
}
interface EventPipeCommandCollectTracing2 extends EventPipeClientCommandBase {
    command: "CollectTracing2";
    circularBufferMB: number;
    format: number;
    requestRundown: boolean;
    providers: EventPipeCollectTracingCommandProvider[];
}
interface EventPipeCommandStopTracing extends EventPipeClientCommandBase {
    command: "StopTracing";
    sessionID: number;
}
interface EventPipeCollectTracingCommandProvider {
    keywords: [number, number];
    logLevel: number;
    provider_name: string;
    filter_data: string | null;
}
type RemoveCommandSetAndId<T extends ProtocolClientCommandBase> = Omit<T, "command_set" | "command">;

type FilterPredicate = (data: ArrayBuffer) => boolean;
interface MockScriptConnection {
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
interface MockEnvironment {
    postMessageToBrowser(message: any, transferable?: Transferable[]): void;
    addEventListenerFromBrowser(cmd: string, listener: (data: any) => void): void;
    createPromiseController<T>(): PromiseAndController<T>;
    delay: (ms: number) => Promise<void>;
    command: MockEnvironmentCommand;
    reply: MockEnvironmentReply;
    expectAdvertise: FilterPredicate;
}

export { MockEnvironment, MockScriptConnection, PromiseAndController };
