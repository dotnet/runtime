// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import cwraps from "./cwraps";
import type { EventPipeSessionID, DiagnosticOptions, EventPipeSession, EventPipeSessionOptions, EventPipeSessionAutoStopOptions } from "./types";
import type { VoidPtr } from "./types/emscripten";
import * as memory from "./memory";

const sizeOfInt32 = 4;

type EventPipeSessionIDImpl = number;


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
    protected _state: State;
    private _sessionID: EventPipeSessionIDImpl;
    private _tracePath: string; // VFS file path to the trace file

    get sessionID(): bigint { return BigInt(this._sessionID); }
    get isIPCStreamingSession(): boolean {
        return false;
    }

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

// an EventPipeSession that starts at runtime startup
class StartupEventPipeFileSession extends EventPipeFileSession implements EventPipeSession {
    readonly _on_stop_callback: null | ((session: EventPipeSession) => void);
    constructor(sessionID: EventPipeSessionIDImpl, tracePath: string, on_stop_callback?: (session: EventPipeSession) => void) {
        super(sessionID, tracePath);
        // By the time we create the JS object, it's already running
        this._state = State.Started;
        this._on_stop_callback = on_stop_callback ?? null;
    }

    stop = () => {
        super.stop();
        if (this._on_stop_callback !== null) {
            const cb = this._on_stop_callback;
            setTimeout(cb, 0, this);
        }
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
    getStartupSessions(): (EventPipeSession | null)[];
}

let startup_session_configs: (EventPipeSessionOptions & EventPipeSessionAutoStopOptions)[] | null = null;
let startup_sessions: (EventPipeSession | null)[] | null = null;

export function mono_wasm_event_pipe_early_startup_callback(): void {
    if (startup_session_configs === null || startup_session_configs.length == 0) {
        return;
    }
    startup_sessions = startup_session_configs.map(config => createAndStartEventPipeSession(config));
    startup_session_configs = null;
}

function postIPCStreamingSessionStarted(sessionID: EventPipeSessionID): void {
    // TODO: For IPC streaming sessions this is the place to send back an acknowledgement with the session ID
}

function createAndStartEventPipeSession(options: (EventPipeSessionOptions & EventPipeSessionAutoStopOptions)): EventPipeSession | null {
    const session = createEventPipeSession(options);
    if (session === null) {
        return null;
    }

    if (session.isIPCStreamingSession) {
        postIPCStreamingSessionStarted(session.sessionID);
    }
    session.start();
    return session;
}

function createEventPipeSession(options?: EventPipeSessionOptions): EventPipeSession | null {
    // The session trace is saved to a file in the VFS. The file name doesn't matter,
    // but we'd like it to be distinct from other traces.
    const tracePath = `/trace-${totalSessions++}.nettrace`;

    const success = memory.withStackAlloc(sizeOfInt32, createSessionWithPtrCB, options, tracePath);

    if (success === false)
        return null;
    const sessionID = success;

    const session = new EventPipeFileSession(sessionID, tracePath);
    return session;
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
    createEventPipeSession: createEventPipeSession,
    getStartupSessions(): (EventPipeSession | null)[] {
        return Array.from(startup_sessions || []);
    },
};

export function mono_wasm_init_diagnostics(config?: DiagnosticOptions): void {
    const sessions = config?.sessions ?? [];
    startup_session_configs = sessions;
}

export default diagnostics;
