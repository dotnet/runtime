import { Module } from "./imports";
import cwraps from "./cwraps";
import type { EventPipeSessionOptions } from "./types";
import type { Int32Ptr, VoidPtr } from "./types/emscripten";
import * as memory from "./memory";

/// An EventPipe session object represents a single diagnostic tracing session that is collecting
/// events from the runtime and managed libraries.  There may be multiple active sessions at the same time.
/// Each session subscribes to a number of providers and will collect events from the time that start() is called, until stop() is called.
/// Upon completion the session saves the events to a file on the VFS.
export interface EventPipeSession {
    get sessionID(): number;
    start(): void;
    stop(): void;
    saveTrace(): string;

}

// internal session state of the JS instance
enum State {
    Initialized,
    Started,
    Done,
}

/// An EventPipe session in the runtime.  There may be multiple sessions.
class EventPipeFileSession implements EventPipeSession {
    private _state: State;
    private _sessionID: number; // integer session ID
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): number { return this._sessionID; }

    constructor (sessionID: number, tracePath: string) {
        this._state = State.Initialized;
        this._sessionID = sessionID;
        this._tracePath = tracePath;
        console.debug (`EventPipe session ${sessionID} started`);
    }

    start = () => {
        if (this._state !== State.Initialized) {
            throw new Error(`EventPipe session ${this._sessionID} already started`);
        }
        this._state = State.Started;
        cwraps.mono_wasm_event_pipe_session_start_streaming (this._sessionID);
        console.debug (`EventPipe session ${this._sessionID} started`);
    }

    stop = () => {
        if (this._state !== State.Started) {
            throw new Error(`cannot stop an EventPipe session in state ${this._state}, not 'Started'`);
        }
        this._state = State.Done;
        cwraps.mono_wasm_event_pipe_session_disable (this._sessionID);
        console.debug (`EventPipe session ${this._sessionID} stopped`);
    }

    saveTrace = () => {
        if (this._state !== State.Done) {
            throw new Error (`session is in state ${this._state}, not 'Done'`);
        }
        console.debug (`session ${this._sessionID} trace in ${this._tracePath}`);
        return this._tracePath;
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

export const diagnostics: Diagnostics = {
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
        const defaultRundownRequested = true;
        const defaultProviders = "";
        const defaultBufferSizeInMB = 1;

        const sessionIdPtr = Module._malloc(4) as unknown as Int32Ptr;

        try {
            const tracePath = computeTracePath(options?.traceFilePath);
            const rundown = options?.collectRundownEvents ?? defaultRundownRequested;
            if (!cwraps.mono_wasm_event_pipe_enable(tracePath, defaultBufferSizeInMB, defaultProviders, rundown, sessionIdPtr))
                return null;

            const sessionID = memory.getI32(sessionIdPtr);

            const session = new EventPipeFileSession(sessionID, tracePath);
            return session;
        } finally {
            Module._free(sessionIdPtr as unknown as VoidPtr);
        }
    },
};

export default diagnostics;
