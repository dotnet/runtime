// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import * as memory from "../../memory";
import { VoidPtr } from "../../types/emscripten";
import { threads_c_functions as cwraps } from "../../cwraps";
import type { EventPipeSessionIDImpl } from "./types";

const sizeOfInt32 = 4;

export interface EventPipeCreateSessionOptions {
    rundownRequested: boolean;
    bufferSizeInMB: number;
    providers: string;
}

type SessionType =
    {
        type: "file";
        filePath: string
    }
    | {
        type: "stream";
        stream: VoidPtr
    };


function createSessionWithPtrCB(sessionIdOutPtr: VoidPtr, options: EventPipeCreateSessionOptions, sessionType: SessionType): false | EventPipeSessionIDImpl {
    memory.setI32(sessionIdOutPtr, 0);
    let tracePath: string | null;
    let ipcStreamAddr: VoidPtr;
    if (sessionType.type === "file") {
        tracePath = sessionType.filePath;
        ipcStreamAddr = 0 as unknown as VoidPtr;
    } else {
        tracePath = null;
        ipcStreamAddr = sessionType.stream;
    }
    if (!cwraps.mono_wasm_event_pipe_enable(tracePath, ipcStreamAddr, options.bufferSizeInMB, options.providers, options.rundownRequested, sessionIdOutPtr)) {
        return false;
    } else {
        return memory.getU32(sessionIdOutPtr);
    }
}

export function createEventPipeStreamingSession(ipcStreamAddr: VoidPtr, options: EventPipeCreateSessionOptions): EventPipeSessionIDImpl | false {
    return memory.withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, { type: "stream", stream: ipcStreamAddr });
}

export function createEventPipeFileSession(tracePath: string, options: EventPipeCreateSessionOptions): EventPipeSessionIDImpl | false {
    return memory.withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, { type: "file", filePath: tracePath });
}
