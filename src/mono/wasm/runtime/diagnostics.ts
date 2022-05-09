import { Module } from './imports';
import cwraps from './cwraps';
import type { EventPipeSessionOptions } from './types';
import type { Int32Ptr, VoidPtr } from './types/emscripten';
import * as memory from './memory';

export interface EventPipeSession {
    get sessionID(): number;
    stop(): void;
    saveTrace(): string;

}

// internal JS state
enum State {
    Initialized,
    Started,
    Done,
}

/// An EventPipe session in the runtime.  There may be multiple sessions.
class EventPipeFileSession implements EventPipeSession {
    private state: State;
    private _sessionID: number; // integer session ID
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): number { return this._sessionID; }

    constructor (sessionID: number, tracePath: string) {
        this.state = State.Initialized;
        this._sessionID = sessionID;
        this._tracePath = tracePath;
        console.debug (`EventPipe session ${sessionID} started`);
    }

    start = () => {
        if (this.state !== State.Initialized) {
            throw new Error(`EventPipe session ${this._sessionID} already started`);
        }
        this.state = State.Started;
        cwraps.mono_wasm_event_pipe_session_start_streaming (this._sessionID);
        console.debug (`EventPipe session ${this._sessionID} started`);
    }

    stop = () => {
        if (this.state !== State.Started) {
            throw new Error(`cannot stop an EventPipe session in state ${this.state}, not 'Started'`);
        }
        this.state = State.Done;
        cwraps.mono_wasm_event_pipe_session_disable (this._sessionID);
        console.debug (`EventPipe session ${this._sessionID} stopped`);
    }

    saveTrace = () => {
        if (this.state !== State.Done) {
            throw new Error (`session is in state ${this.state}, not 'Done'`);
        }
        console.debug (`session ${this._sessionID} trace in ${this._tracePath}`);
        return this._tracePath;
    }


}


export interface Diagnostics {
    createEventPipeSession (options?: EventPipeSessionOptions): EventPipeSession | null;
}

export const defaultOutputPath: string = '/trace.nettrace';

export const diagnostics: Diagnostics = {
    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
        const rundownRequested = false;
        const defaultProviders = '';
        const defaultBufferSizeInMB = 1;

        let sessionIdPtr: Int32Ptr = Module._malloc(4) as unknown as Int32Ptr;

        try {
            const tracePath = options?.traceFilePath ?? defaultOutputPath
            if (!cwraps.mono_wasm_event_pipe_enable(tracePath,
                                            defaultBufferSizeInMB,
                                            defaultProviders,
                                            rundownRequested,
                                            sessionIdPtr))
            return null;

            const sessionID = memory.getI32(sessionIdPtr);

            const session = new EventPipeFileSession (sessionID, tracePath);
            return session;
        } finally {
            Module._free(sessionIdPtr as unknown as VoidPtr);
        }
    },
};

export default diagnostics;
