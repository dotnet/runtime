// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Just the minimal info we can pull from an IPC message
export interface BinaryProtocolCommand {
    commandSet: number;
    command: number;
    payload: Uint8Array;
}

export function isBinaryProtocolCommand(x: object): x is BinaryProtocolCommand {
    return "commandSet" in x && "command" in x && "payload" in x;
}

export interface ParseResultBase {
    readonly success: boolean;
}

export interface ParseResultOk extends ParseResultBase {
    readonly success: true;
}

export interface ParseResultFail extends ParseResultBase {
    readonly success: false;
    readonly error: string;
}


export const enum CommandSetId {
    Reserved = 0,
    Dump = 1,
    EventPipe = 2,
    Profiler = 3,
    Process = 4,
    /* future*/

    // replies
    Server = 0xFF,
}

export const enum EventPipeCommandId {
    StopTracing = 1,
    CollectTracing = 2,
    CollectTracing2 = 3,
}

export const enum ProcessCommandId {
    ProcessInfo = 0,
    ResumeRuntime = 1,
    ProcessEnvironment = 2,
    ProcessInfo2 = 4,
}

export const enum ServerCommandId {
    OK = 0,
    Error = 0xFF,
}
