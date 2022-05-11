// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import cwraps from "./cwraps";
import type { EventPipeSessionOptions } from "./types";
import type { VoidPtr } from "./types/emscripten";
import * as memory from "./memory";
import { toBase64StringImpl } from "./base64";
import type { CUInt64 } from "./cuint64";
import * as cuint64 from "./cuint64";

const sizeOfCUInt64 = 8;

/// An EventPipe session object represents a single diagnostic tracing session that is collecting
/// events from the runtime and managed libraries.  There may be multiple active sessions at the same time.
/// Each session subscribes to a number of providers and will collect events from the time that start() is called, until stop() is called.
/// Upon completion the session saves the events to a file on the VFS.
/// The data can then be retrieved as Blob or as a data URI (prefer Blob).
export interface EventPipeSession {
    get sessionID(): bigint;
    start(): void;
    stop(): void;
    getTraceBlob(): Blob;
    getTraceDataURI(): string;
}

// internal session state of the JS instance
enum State {
    Initialized,
    Started,
    Done,
}

function withCUIn64Ptr<TRes> (x: CUInt64, f: (ptr: VoidPtr) => TRes): TRes {
    const tmp = Module._malloc (sizeOfCUInt64);
    try {
        memory.setCU64 (tmp, x);
        return f (tmp);
    } finally {
        Module._free (tmp);
    }
}

function start_streaming (sessionID: CUInt64): void {
    withCUIn64Ptr (sessionID, (ptr) => {
        cwraps.mono_wasm_event_pipe_session_start_streaming (ptr);
    });
}

function stop_streaming (sessionID: CUInt64): void {
    withCUIn64Ptr (sessionID, (ptr) => {
        cwraps.mono_wasm_event_pipe_session_disable (ptr);
    });
}

/// An EventPipe session that saves the event data to a file in the VFS.
class EventPipeFileSession implements EventPipeSession {
    private _state: State;
    private _sessionID: CUInt64; // integer session ID
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): bigint { return cuint64.toBigInt (this._sessionID); }

    constructor (sessionID: CUInt64, tracePath: string) {
        this._state = State.Initialized;
        this._sessionID = sessionID;
        this._tracePath = tracePath;
        console.debug (`EventPipe session ${sessionID} started`);
    }

    start = () => {
        if (this._state !== State.Initialized) {
            throw new Error(`EventPipe session ${this.sessionID} already started`);
        }
        this._state = State.Started;
        start_streaming (this._sessionID);
        console.debug (`EventPipe session ${this.sessionID} started`);
    }

    stop = () => {
        if (this._state !== State.Started) {
            throw new Error(`cannot stop an EventPipe session in state ${this._state}, not 'Started'`);
        }
        this._state = State.Done;
        stop_streaming (this._sessionID);
        console.debug (`EventPipe session ${this.sessionID} stopped`);
    }

    getTraceBlob = () => {
        if (this._state !== State.Done) {
            throw new Error (`session is in state ${this._state}, not 'Done'`);
        }
        const data = Module.FS_readFile(this._tracePath, { encoding: "binary" }) as Uint8Array;
        return new Blob([data], { type: "application/octet-stream" });
    }

    getTraceDataURI = () => {
        if (this._state !== State.Done) {
            throw new Error (`session is in state ${this._state}, not 'Done'`);
        }
        const data = Module.FS_readFile(this._tracePath, { encoding: "binary" }) as Uint8Array;
        return `data:application/octet-stream;base64,${toBase64StringImpl(data)}`;
    }
}

export interface Diagnostics {
    createEventPipeSession (options?: EventPipeSessionOptions): EventPipeSession | null;
}

export const defaultOutputPath = "/trace.nettrace";

function computeTracePath (tracePath? : string | (() => string | null | undefined)): string {
    if (tracePath === undefined) {
        return defaultOutputPath;
    }
    if (typeof tracePath === "function")
        return tracePath() ?? defaultOutputPath;
    return tracePath;
}

/// APIs for working with .NET diagnostics from JavaScript.
export const diagnostics: Diagnostics = {
    /// Creates a new EventPipe session that will collect trace events from the runtime and managed libraries.
    /// Use the options to control the output file and the level of detail.
    /// Note that if you use multiple sessions at the same time, you should specify a unique 'traceFilePath' for each session.
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
        const defaultRundownRequested = true;
        const defaultProviders = "";
        const defaultBufferSizeInMB = 1;

        const tracePath = computeTracePath(options?.traceFilePath);
        const rundown = options?.collectRundownEvents ?? defaultRundownRequested;

        const [success, sessionID] = withCUIn64Ptr (cuint64.zero, (ptr) => {
            if (!cwraps.mono_wasm_event_pipe_enable(tracePath, defaultBufferSizeInMB, defaultProviders, rundown, ptr)) {
                return [false, cuint64.zero];
            } else {
                return [true, memory.getCU64 (ptr)];
            }});

        if (!success)
            return null;

        const session = new EventPipeFileSession(sessionID, tracePath);
        return session;
    },
};

export default diagnostics;
