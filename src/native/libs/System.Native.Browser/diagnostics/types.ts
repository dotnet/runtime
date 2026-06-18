// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseCompletionSource, VoidPtr } from "../types";

export interface IDiagnosticConnection {
    send(message: Uint8Array): number;
    poll(): number;
    recv(buffer: VoidPtr, bytesToRead: number): number;
    close(): number;
}

// [hi,lo]
export type SessionId = [number, number];

export interface IDiagnosticSession {
    sessionId: SessionId;
    store(message: Uint8Array): number;
    sendCommand(message: Uint8Array): void;
}

export interface IDiagnosticClient {
    skipDownload?: boolean;
    onClosePromise: PromiseCompletionSource<Uint8Array[]>;
    commandOnAdvertise(): Uint8Array;
    onSessionStart?(session: IDiagnosticSession): void;
    onData?(session: IDiagnosticSession, message: Uint8Array): void;
    onClose?(messages: Uint8Array[]): void;
    onError?(session: IDiagnosticSession, message: Uint8Array): void;
}

export type FnClientProvider = (scenarioName: string) => IDiagnosticClient;


export type ProviderV2 = {
    keywords: [number, Keywords],
    logLevel: number,
    providerName: string,
    arguments: string | null
}

export type PayloadV2 = {
    circularBufferMB: number,
    format: number,
    requestRundown: boolean,
    providers: ProviderV2[]
}

export const enum Keywords {
    None = 0,
    All = 0xFFFF_FFFF,
    //
    // Summary:
    //     Logging when garbage collections and finalization happen.
    GC = 1,
    //
    // Summary:
    //     Events when GC handles are set or destroyed.
    GCHandle = 2,
    Binder = 4,
    //
    // Summary:
    //     Logging when modules actually get loaded and unloaded.
    Loader = 8,
    //
    // Summary:
    //     Logging when Just in time (JIT) compilation occurs.
    Jit = 0x10,
    //
    // Summary:
    //     Logging when precompiled native (NGEN) images are loaded.
    NGen = 0x20,
    //
    // Summary:
    //     Indicates that on attach or module load , a rundown of all existing methods should
    //     be done
    StartEnumeration = 0x40,
    //
    // Summary:
    //     Indicates that on detach or process shutdown, a rundown of all existing methods
    //     should be done
    StopEnumeration = 0x80,
    //
    // Summary:
    //     Events associated with validating security restrictions.
    Security = 0x400,
    //
    // Summary:
    //     Events for logging resource consumption on an app-domain level granularity
    AppDomainResourceManagement = 0x800,
    //
    // Summary:
    //     Logging of the internal workings of the Just In Time compiler. This is fairly
    //     verbose. It details decisions about interesting optimization (like inlining and
    //     tail call)
    JitTracing = 0x1000,
    //
    // Summary:
    //     Log information about code thunks that transition between managed and unmanaged
    //     code.
    Interop = 0x2000,
    //
    // Summary:
    //     Log when lock contention occurs. (Monitor.Enters actually blocks)
    Contention = 0x4000,
    //
    // Summary:
    //     Log exception processing.
    Exception = 0x8000,
    //
    // Summary:
    //     Log events associated with the threadpool, and other threading events.
    Threading = 0x10000,
    //
    // Summary:
    //     Dump the native to IL mapping of any method that is JIT compiled. (V4.5 runtimes
    //     and above).
    JittedMethodILToNativeMap = 0x20000,
    //
    // Summary:
    //     If enabled will suppress the rundown of NGEN events on V4.0 runtime (has no effect
    //     on Pre-V4.0 runtimes).
    OverrideAndSuppressNGenEvents = 0x40000,
    //
    // Summary:
    //     Enables the 'BulkType' event
    Type = 0x80000,
    //
    // Summary:
    //     Enables the events associated with dumping the GC heap
    GCHeapDump = 0x100000,
    //
    // Summary:
    //     Enables allocation sampling with the 'fast'. Sample to limit to 100 allocations
    //     per second per type. This is good for most detailed performance investigations.
    //     Note that this DOES update the allocation path to be slower and only works if
    //     the process start with this on.
    GCSampledObjectAllocationHigh = 0x200000,
    //
    // Summary:
    //     Enables events associate with object movement or survival with each GC.
    GCHeapSurvivalAndMovement = 0x400000,
    //
    // Summary:
    //     Triggers a GC. Can pass a 64 bit value that will be logged with the GC Start
    //     event so you know which GC you actually triggered.
    GCHeapCollect = 0x800000,
    //
    // Summary:
    //     Indicates that you want type names looked up and put into the events (not just
    //     meta-data tokens).
    GCHeapAndTypeNames = 0x1000000,
    //
    // Summary:
    //     Enables allocation sampling with the 'slow' rate, Sample to limit to 5 allocations
    //     per second per type. This is reasonable for monitoring. Note that this DOES update
    //     the allocation path to be slower and only works if the process start with this
    //     on.
    GCSampledObjectAllocationLow = 0x2000000,
    //
    // Summary:
    //     Turns on capturing the stack and type of object allocation made by the .NET Runtime.
    //     This is only supported after V4.5.3 (Late 2014) This can be very verbose and
    //     you should seriously using GCSampledObjectAllocationHigh instead (and GCSampledObjectAllocationLow
    //     for production scenarios).
    GCAllObjectAllocation = 0x2200000,
    //
    // Summary:
    //     This suppresses NGEN events on V4.0 (where you have NGEN PDBs), but not on V2.0
    //     (which does not know about this bit and also does not have NGEN PDBS).
    // eslint-disable-next-line @typescript-eslint/no-duplicate-enum-values
    SuppressNGen = 0x40000,
    //
    // Summary:
    //     TODO document
    PerfTrack = 0x20000000,
    //
    // Summary:
    //     Also log the stack trace of events for which this is valuable.
    Stack = 0x40000000,
    //
    // Summary:
    //     This allows tracing work item transfer events (thread pool enqueue/dequeue/ioenqueue/iodequeue/a.o.)
    ThreadTransfer = 0x80000000,
    //
    // Summary:
    //     .NET Debugger events
    Debugger = 0x100000000,
    //
    // Summary:
    //     Events intended for monitoring on an ongoing basis.
    Monitoring = 0x200000000,
    //
    // Summary:
    //     Events that will dump PDBs of dynamically generated assemblies to the ETW stream.
    Codesymbols = 0x400000000,
    //
    // Summary:
    //     Events that provide information about compilation.
    Compilation = 0x1000000000,
    //
    // Summary:
    //     Diagnostic events for diagnosing compilation and pre-compilation features.
    CompilationDiagnostic = 0x2000000000,
    //
    // Summary:
    //     Diagnostic events for capturing token information for events that express MethodID
    MethodDiagnostic = 0x4000000000,
    //
    // Summary:
    //     Diagnostic events for diagnosing issues involving the type loader.
    TypeDiagnostic = 0x8000000000,
    //
    // Summary:
    //     Events for wait handle waits.
    WaitHandle = 0x40000000000,
    //
    // Summary:
    //     Recommend default flags (good compromise on verbosity).
    Default = 0x14C14FCCBD,
    //
    // Summary:
    //     What is needed to get symbols for JIT compiled code.
    JITSymbols = 0x60098,
    //
    // Summary:
    //     This provides the flags commonly needed to take a heap .NET Heap snapshot with
    //     ETW.
    GCHeapSnapshot = 0x1980001
}

export const enum CommandSetId {
    Reserved = 0,
    Dump = 1,
    EventPipe = 2,
    Profiler = 3,
    Process = 4,

    // replies
    Server = 0xFF,
}

export const enum EventPipeCommandId {
    StopTracing = 1,
    CollectTracing = 2,
    CollectTracing2 = 3,
    CollectTracing3 = 4,
    CollectTracing4 = 5,
}

export const enum ProcessCommandId {
    ProcessInfo = 0,
    ResumeRuntime = 1,
    ProcessEnvironment = 2,
    SetEnvVar = 3,
    ProcessInfo2 = 4,
    EnablePerfmap = 5,
    DisablePerfmap = 6,
    ApplyStartupHook = 7,
    ProcessInfo3 = 8,
}

export const enum ServerCommandId {
    OK = 0,
    Error = 0xFF,
}

export * from "../types";
