// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// An EventPipe session object represents a single diagnostic tracing session that is collecting
/// events from the runtime and managed libraries.  There may be multiple active sessions at the same time.
/// Each session subscribes to a number of providers and will collect events from the time that start() is called, until stop() is called.
/// Upon completion the session saves the events to a file on the VFS.
/// The data can then be retrieved as Blob.
import { EventPipeSessionID, EventPipeSessionOptions } from "../../types";
import { EventPipeSessionIDImpl } from "../shared/types";
import { createEventPipeFileSession } from "../shared/create-session";
import { Module } from "../../imports";
import cwraps from "../../cwraps";

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

/// An EventPipe session that saves the event data to a file in the VFS.
class EventPipeFileSession implements EventPipeSession {
    protected _state: State;
    private _sessionID: EventPipeSessionIDImpl;
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): bigint { return BigInt(this._sessionID); }

    constructor(sessionID: EventPipeSessionIDImpl, tracePath: string) {
        this._state = State.Initialized;
        this._sessionID = sessionID;
        this._tracePath = tracePath;
        console.debug(`MONO_WASM: EventPipe session ${this.sessionID} created`);
    }

    start = () => {
        if (this._state !== State.Initialized) {
            throw new Error(`MONO_WASM: EventPipe session ${this.sessionID} already started`);
        }
        this._state = State.Started;
        start_streaming(this._sessionID);
        console.debug(`MONO_WASM: EventPipe session ${this.sessionID} started`);
    };

    stop = () => {
        if (this._state !== State.Started) {
            throw new Error(`cannot stop an EventPipe session in state ${this._state}, not 'Started'`);
        }
        this._state = State.Done;
        stop_streaming(this._sessionID);
        console.debug(`MONO_WASM: EventPipe session ${this.sessionID} stopped`);
    };

    getTraceBlob = () => {
        if (this._state !== State.Done) {
            throw new Error(`session is in state ${this._state}, not 'Done'`);
        }
        const data = Module.FS_readFile(this._tracePath, { encoding: "binary" }) as Uint8Array;
        return new Blob([data], { type: "application/octet-stream" });
    };
}

function start_streaming(sessionID: EventPipeSessionIDImpl): void {
    cwraps.mono_wasm_event_pipe_session_start_streaming(sessionID);
}

function stop_streaming(sessionID: EventPipeSessionIDImpl): void {
    cwraps.mono_wasm_event_pipe_session_disable(sessionID);
}

// a conter for the number of sessions created
let totalSessions = 0;

export function makeEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
    const defaultRundownRequested = true;
    const defaultProviders = ""; // empty string means use the default providers
    const defaultBufferSizeInMB = 1;

    const rundown = options?.collectRundownEvents ?? defaultRundownRequested;
    const providers = options?.providers ?? defaultProviders;

    // The session trace is saved to a file in the VFS. The file name doesn't matter,
    // but we'd like it to be distinct from other traces.
    const tracePath = `/trace-${totalSessions++}.nettrace`;

    const sessionOptions = {
        rundownRequested: rundown,
        providers: providers,
        bufferSizeInMB: defaultBufferSizeInMB,
    };

    const success = createEventPipeFileSession(tracePath, sessionOptions);

    if (success === false)
        return null;
    const sessionID = success;

    return new EventPipeFileSession(sessionID, tracePath);
}


