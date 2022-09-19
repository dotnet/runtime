// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface ProtocolClientCommandBase {
    command_set: string;
    command: string;
}

export interface ProcessClientCommandBase extends ProtocolClientCommandBase {
    command_set: "Process"
}

export interface EventPipeClientCommandBase extends ProtocolClientCommandBase {
    command_set: "EventPipe"
}

export type ProcessCommand =
    | ProcessCommandResumeRuntime
    ;

export type EventPipeCommand =
    | EventPipeCommandCollectTracing2
    | EventPipeCommandStopTracing
    ;

export interface ProcessCommandResumeRuntime extends ProcessClientCommandBase {
    command: "ResumeRuntime"
}

export interface EventPipeCommandCollectTracing2 extends EventPipeClientCommandBase {
    command: "CollectTracing2";
    circularBufferMB: number;
    format: number;
    requestRundown: boolean;
    providers: EventPipeCollectTracingCommandProvider[];
}

export interface EventPipeCommandStopTracing extends EventPipeClientCommandBase {
    command: "StopTracing";
    sessionID: number;// FIXME: this is 64-bits in the protocol
}

export interface EventPipeCollectTracingCommandProvider {
    keywords: [number, number];  // lo,hi.  FIXME: this is ugly
    logLevel: number;
    provider_name: string;
    filter_data: string;
}

export type RemoveCommandSetAndId<T extends ProtocolClientCommandBase> = Omit<T, "command_set" | "command">;

export type ProtocolClientCommand = ProcessCommand | EventPipeCommand;

export function isDiagnosticCommandBase(x: object): x is ProtocolClientCommandBase {
    return typeof x === "object" && "command_set" in x && "command" in x;
}

export function isProcessCommand(x: object): x is ProcessClientCommandBase {
    return isDiagnosticCommandBase(x) && x.command_set === "Process";
}

export function isEventPipeCommand(x: object): x is EventPipeClientCommandBase {
    return isDiagnosticCommandBase(x) && x.command_set === "EventPipe";
}

export function isProcessCommandResumeRuntime(x: ProcessClientCommandBase): x is ProcessCommandResumeRuntime {
    return isProcessCommand(x) && x.command === "ResumeRuntime";
}

export function isEventPipeCollectTracingCommandProvider(x: object): x is EventPipeCollectTracingCommandProvider {
    return typeof x === "object" && "keywords" in x && "logLevel" in x && "provider_name" in x && "filter_data" in x;
}

export function isEventPipeCommandCollectTracing2(x: object): x is EventPipeCommandCollectTracing2 {
    return isEventPipeCommand(x) && x.command === "CollectTracing2" && "circularBufferMB" in x &&
        "format" in x && "requestRundown" in x && "providers" in x &&
        Array.isArray((<any>x).providers) && (<any>x).providers.every(isEventPipeCollectTracingCommandProvider);
}

export function isEventPipeCommandStopTracing(x: object): x is EventPipeCommandStopTracing {
    return isEventPipeCommand(x) && x.command === "StopTracing" && "sessionID" in x;
}
