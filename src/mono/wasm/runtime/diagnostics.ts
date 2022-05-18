// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import cwraps from "./cwraps";
import type { EventPipeSessionOptions } from "./types";
import type { VoidPtr } from "./types/emscripten";
import * as memory from "./memory";

const sizeOfInt32 = 4;

export type EventPipeSessionID = bigint;
type EventPipeSessionIDImpl = number;

/// An EventPipe session object represents a single diagnostic tracing session that is collecting
/// events from the runtime and managed libraries.  There may be multiple active sessions at the same time.
/// Each session subscribes to a number of providers and will collect events from the time that start() is called, until stop() is called.
/// Upon completion the session saves the events to a file on the VFS.
/// The data can then be retrieved as Blob.
export interface EventPipeSession {
    // session ID for debugging logging only
    get sessionID(): EventPipeSessionID;
    start(): void;
    stop(): void;
    getTraceBlob(): Blob;
}

// internal session state of the JS instance
enum State {
    Initialized,
    Started,
    Done,
}

function start_streaming(sessionID: EventPipeSessionIDImpl): void {
    cwraps.mono_wasm_event_pipe_session_start_streaming(sessionID);
}

function stop_streaming(sessionID: EventPipeSessionIDImpl): void {
    cwraps.mono_wasm_event_pipe_session_disable(sessionID);
}

/// An EventPipe session that saves the event data to a file in the VFS.
class EventPipeFileSession implements EventPipeSession {
    private _state: State;
    private _sessionID: EventPipeSessionIDImpl;
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): bigint { return BigInt(this._sessionID); }

    constructor(sessionID: EventPipeSessionIDImpl, tracePath: string) {
        this._state = State.Initialized;
        this._sessionID = sessionID;
        this._tracePath = tracePath;
        console.debug(`EventPipe session ${this.sessionID} created`);
    }

    start = () => {
        if (this._state !== State.Initialized) {
            throw new Error(`EventPipe session ${this.sessionID} already started`);
        }
        this._state = State.Started;
        start_streaming(this._sessionID);
        console.debug(`EventPipe session ${this.sessionID} started`);
    }

    stop = () => {
        if (this._state !== State.Started) {
            throw new Error(`cannot stop an EventPipe session in state ${this._state}, not 'Started'`);
        }
        this._state = State.Done;
        stop_streaming(this._sessionID);
        console.debug(`EventPipe session ${this.sessionID} stopped`);
    }

    getTraceBlob = () => {
        if (this._state !== State.Done) {
            throw new Error(`session is in state ${this._state}, not 'Done'`);
        }
        const data = Module.FS_readFile(this._tracePath, { encoding: "binary" }) as Uint8Array;
        return new Blob([data], { type: "application/octet-stream" });
    }
}

// a conter for the number of sessions created
let totalSessions = 0;

function createSessionWithPtrCB(sessionIdOutPtr: VoidPtr, options: EventPipeSessionOptions | undefined, tracePath: string): false | number {
    const defaultRundownRequested = true;
    const defaultProviders = "";
    const defaultBufferSizeInMB = 1;

    const rundown = options?.collectRundownEvents ?? defaultRundownRequested;

    memory.setI32(sessionIdOutPtr, 0);
    if (!cwraps.mono_wasm_event_pipe_enable(tracePath, defaultBufferSizeInMB, defaultProviders, rundown, sessionIdOutPtr)) {
        return false;
    } else {
        return memory.getI32(sessionIdOutPtr);
    }
}

export interface Diagnostics {
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null;
}

/// APIs for working with .NET diagnostics from JavaScript.
export const diagnostics: Diagnostics = {
    /// Creates a new EventPipe session that will collect trace events from the runtime and managed libraries.
    /// Use the options to control the kinds of events to be collected.
    /// Multiple sessions may be created and started at the same time.
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
        // The session trace is saved to a file in the VFS. The file name doesn't matter,
        // but we'd like it to be distinct from other traces.
        const tracePath = `/trace-${totalSessions++}.nettrace`;

        const success = memory.withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, tracePath);

        if (success === false)
            return null;
        const sessionID = success;

        const session = new EventPipeFileSession(sessionID, tracePath);
        return session;
    },
};

export default diagnostics;
