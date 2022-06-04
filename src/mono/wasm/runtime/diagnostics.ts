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

const eventLevel = {
    LogAlways: 0,
    Critical: 1,
    Error: 2,
    Warning: 3,
    Informational: 4,
    Verbose: 5,
} as const;

type EventLevel = typeof eventLevel;

type UnnamedProviderConfiguration = Partial<{
    keyword_mask: string | 0;
    level: number;
    args: string;
}>

/// The configuration for an individual provider.  Each provider configuration has the name of the provider,
/// the level of events to collect, and a string containing a 32-bit hexadecimal mask (without an "0x" prefix) of
/// the "keywords" to filter a subset of the events. The keyword mask may be the number 0 or "" to skips the filtering.
/// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/well-known-event-providers for a list of known providers.
/// Additional providers may be added by applications or libraries that implement an EventSource subclass.
/// See https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing.eventsource?view=net-6.0
///
/// Some providers also have an "args" string in an arbitrary format.  For example the EventSource providers that
/// include EventCounters have a "EventCounterIntervalSec=NNN" argument that specified how often the counters of
/// the event source should be polled.
export interface ProviderConfiguration extends UnnamedProviderConfiguration {
    name: string;
}

const runtimeProviderName = "Microsoft-Windows-DotNETRuntime";
const runtimePrivateProviderName = "Microsoft-Windows-DotNETRuntimePrivate";
const sampleProfilerProviderName = "Microsoft-DotNETCore-SampleProfiler";

const runtimeProviderDefault: ProviderConfiguration = {
    name: runtimeProviderName,
    keyword_mask: "4c14fccbd",
    level: eventLevel.Verbose,
};

const runtimePrivateProviderDefault: ProviderConfiguration = {
    name: runtimePrivateProviderName,
    keyword_mask: "4002000b",
    level: eventLevel.Verbose,
};

const sampleProfilerProviderDefault: ProviderConfiguration = {
    name: sampleProfilerProviderName,
    keyword_mask: "0",
    level: eventLevel.Verbose,
};

/// A helper class to create EventPipeSessionOptions
export class SessionOptionsBuilder {
    private _rundown?: boolean;
    private _providers: ProviderConfiguration[];
    /// Create  an empty builder.  Prefer to use SesssionOptionsBuilder.Empty
    constructor() {
        this._providers = [];
    }
    /// Gets a builder with no providers.
    static get Empty(): SessionOptionsBuilder { return new SessionOptionsBuilder(); }
    /// Gets a builder with default providers and rundown events enabled.
    /// See https://docs.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe#trace-using-environment-variables
    static get DefaultProviders(): SessionOptionsBuilder {
        return this.Empty.addRuntimeProvider().addRuntimePrivateProvider().addSampleProfilerProvider();
    }
    /// Change whether to collect rundown events.
    /// Certain providers may need rundown events to be collected in order to provide useful diagnostic information.
    setRundownEnabled(enabled: boolean): SessionOptionsBuilder {
        this._rundown = enabled;
        return this;
    }
    /// Add a provider configuration to the builder.
    addProvider(provider: ProviderConfiguration): SessionOptionsBuilder {
        this._providers.push(provider);
        return this;
    }
    /// Add the Microsoft-Windows-DotNETRuntime provider.  Use override options to change the event level or keyword mask.
    /// The default is { keyword_mask: "4c14fccbd", level: eventLevel.Verbose }
    addRuntimeProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...runtimeProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Add the Microsoft-Windows-DotNETRuntimePrivate provider. Use override options to change the event level or keyword mask.
    /// The default is { keyword_mask: "4002000b", level: eventLevel.Verbose}
    addRuntimePrivateProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...runtimePrivateProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Add the Microsoft-DotNETCore-SampleProfiler. Use override options to change the event level or keyword mask.
    // The default is { keyword_mask: 0, level: eventLevel.Verbose }
    addSampleProfilerProvider(overrideOptions?: UnnamedProviderConfiguration): SessionOptionsBuilder {
        const options = { ...sampleProfilerProviderDefault, ...overrideOptions };
        this._providers.push(options);
        return this;
    }
    /// Create an EventPipeSessionOptions from the builder.
    build(): EventPipeSessionOptions {
        const providers = this._providers.map(p => {
            const name = p.name;
            const keyword_mask = "" + (p?.keyword_mask ?? "");
            const level = p?.level ?? eventLevel.Verbose;
            const args = p?.args ?? "";
            const maybeArgs = args != "" ? `:${args}` : "";
            return `${name}:${keyword_mask}:${level}${maybeArgs}`;
        });
        return {
            collectRundownEvents: this._rundown,
            providers: providers.join(",")
        };
    }
}

// a conter for the number of sessions created
let totalSessions = 0;

function createSessionWithPtrCB(sessionIdOutPtr: VoidPtr, options: EventPipeSessionOptions | undefined, tracePath: string): false | number {
    const defaultRundownRequested = true;
    const defaultProviders = ""; // empty string means use the default providers
    const defaultBufferSizeInMB = 1;

    const rundown = options?.collectRundownEvents ?? defaultRundownRequested;
    const providers = options?.providers ?? defaultProviders;

    memory.setI32(sessionIdOutPtr, 0);
    if (!cwraps.mono_wasm_event_pipe_enable(tracePath, defaultBufferSizeInMB, providers, rundown, sessionIdOutPtr)) {
        return false;
    } else {
        return memory.getI32(sessionIdOutPtr);
    }
}

export interface Diagnostics {
    EventLevel: EventLevel;
    SessionOptionsBuilder: typeof SessionOptionsBuilder;

    createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null;
}


/// APIs for working with .NET diagnostics from JavaScript.
export const diagnostics: Diagnostics = {
    /// An enumeration of the level (higher value means more detail):
    /// LogAlways: 0,
    /// Critical: 1,
    /// Error: 2,
    /// Warning: 3,
    /// Informational: 4,
    /// Verbose: 5,
    EventLevel: eventLevel,
    /// A builder for creating an EventPipeSessionOptions instance.
    SessionOptionsBuilder: SessionOptionsBuilder,
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
