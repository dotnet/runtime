// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticCommandOptions } from "../types";

import { SessionId } from "./common";
import { runtimeHelpers } from "./globals";

// ADVR_V1\0
export const advert1 = [65, 68, 86, 82, 95, 86, 49, 0,];
// DOTNET_IPC_V1\0
export const dotnet_IPC_V1 = [68, 79, 84, 78, 69, 84, 95, 73, 80, 67, 95, 86, 49, 0];

// this file contains the IPC commands that are sent by client (like dotnet-trace) to the diagnostic server (like Mono VM in the browser)
// just formatting bytes, no sessions management here


export function advertise () {
    // xxxxxxxx-xxxx-4xxx-xxxx-xxxxxxxxxxxx
    const uuid = new Uint8Array(16);
    globalThis.crypto.getRandomValues(uuid);
    uuid[7] = (uuid[7] & 0xf) | 0x40;// version 4

    const pid = runtimeHelpers.SystemJS_GetCurrentProcessId();

    return Uint8Array.from([
        ...advert1,
        ...uuid,
        ...serializeUint64([0, pid]),
        0, 0// future
    ]);
}

export function commandStopTracing (sessionID:SessionId) {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.EventPipe, EventPipeCommandId.StopTracing, computeMessageByteLength(8)),
        ...serializeUint64(sessionID),
    ]);
}

export function commandResumeRuntime () {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.Process, ProcessCommandId.ResumeRuntime, computeMessageByteLength(0)),
    ]);
}

export function commandProcessInfo3 () {
    return Uint8Array.from([
        ...serializeHeader(CommandSetId.Process, ProcessCommandId.ProcessInfo3, computeMessageByteLength(0)),
    ]);
}

export function commandGcHeapDump (options:DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB:options.circularBufferMB || 256,
        format: 1,
        requestRundown: true,
        providers: [
            {
                keywords: [
                    0x0000_0000,
                    Keywords.GCHeapSnapshot, // 0x1980001
                    // GC_HEAP_DUMP_VTABLE_CLASS_REF_KEYWORD 0x8000000
                    // GC_FINALIZATION_KEYWORD               0x1000000
                    // GC_HEAP_COLLECT_KEYWORD               0x0800000
                    // GC_KEYWORD                            0x0000001
                ],
                logLevel: 5,
                provider_name: "Microsoft-Windows-DotNETRuntime",
                arguments: null
            },
            ...options.extraProviders || [],
        ]
    });
}

function uuidv4 () {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ globalThis.crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}

export function commandCounters (options:DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB:options.circularBufferMB || 256,
        format: 1,
        requestRundown: false,
        providers: [
            {
                keywords: [0, Keywords.GCHandle],
                logLevel: 4,
                provider_name: "System.Diagnostics.Metrics",
                arguments: `SessionId=SHARED;Metrics=System.Runtime;RefreshInterval=${options.intervalSeconds || 1};MaxTimeSeries=1000;MaxHistograms=10;ClientId=${uuidv4()};`,
            },
            ...options.extraProviders || [],
        ]
    });
}

export function commandSampleProfiler (options:DiagnosticCommandOptions) {
    return commandCollectTracing2({
        circularBufferMB:options.circularBufferMB || 256,
        format: 1,
        requestRundown: true,
        providers: [
            {
                keywords: [
                    0x0000_0000,
                    0x0000_0000,
                ],
                logLevel: 4,
                provider_name: "Microsoft-DotNETCore-SampleProfiler",
                arguments: null
            },
            ...options.extraProviders || [],
        ]
    });
}

function commandCollectTracing2 (payload2:PayloadV2) {
    const payloadLength = computeCollectTracing2PayloadByteLength(payload2);
    const messageLength = computeMessageByteLength(payloadLength);
    const message = [
        ...serializeHeader(CommandSetId.EventPipe, EventPipeCommandId.CollectTracing2, messageLength),
        ...serializeUint32(payload2.circularBufferMB),
        ...serializeUint32(payload2.format),
        ...serializeUint8(payload2.requestRundown ? 1 : 0),
        ...serializeUint32(payload2.providers.length),
    ];
    for (const provider of payload2.providers) {
        message.push(...serializeUint64(provider.keywords));
        message.push(...serializeUint32(provider.logLevel));
        message.push(...serializeString(provider.provider_name));
        message.push(...serializeString(provider.arguments));
    }
    return Uint8Array.from(message);
}

const enum Keywords {
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
    //     Log events associated with the threadpoo, and other threading events.
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
    SupressNGen = 0x40000,
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

const enum EventPipeCommandId {
    StopTracing = 1,
    CollectTracing = 2,
    CollectTracing2 = 3,
    CollectTracing3 = 4,
    CollectTracing4 = 5,
}

const enum ProcessCommandId {
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

function serializeMagic () {
    return Uint8Array.from(dotnet_IPC_V1);
}

function serializeUint8 (value:number) {
    return Uint8Array.from([value]);
}

function serializeUint16 (value:number) {
    return new Uint8Array(Uint16Array.from([value]).buffer);
}

function serializeUint32 (value:number) {
    return new Uint8Array(Uint32Array.from([value]).buffer);
}

function serializeUint64 (value:[number, number]) {
    // value == [hi, lo]
    return new Uint8Array(Uint32Array.from([value[1], value[0]]).buffer);
}

function serializeString (value:string|null) {
    const message = [];
    if (value === null || value === undefined || value === "") {
        message.push(...serializeUint32(1));
        message.push(...serializeUint16(0));
    } else {
        const len = value.length;
        const hasNul = value[len - 1] === "\0";
        message.push(...serializeUint32(len + (hasNul ? 0 : 1)));
        for (let i = 0; i < len; i++) {
            message.push(...serializeUint16(value.charCodeAt(i)));
        }
        if (!hasNul) {
            message.push(...serializeUint16(0));
        }
    }
    return message;
}

function computeStringByteLength (s:string|null) {
    if (s === undefined || s === null || s === "")
        return 4 + 2; // just length of empty zero terminated string
    return 4 + 2 * s.length + 2; // length + UTF16 + null
}

function computeMessageByteLength (payloadLength:number) {
    const fullHeaderSize = 14 + 2 // magic, len
        + 1 + 1 // commandSet, command
        + 2; // reserved ;
    return fullHeaderSize + payloadLength;
}

function serializeHeader (commandSet:CommandSetId, command:ServerCommandId|EventPipeCommandId|ProcessCommandId, len:number) {
    return Uint8Array.from([
        ...serializeMagic(),
        ...serializeUint16(len),
        ...serializeUint8(commandSet),
        ...serializeUint8(command),
        ...serializeUint16(0), // reserved*/
    ]);
}

function computeCollectTracing2PayloadByteLength (payload2:PayloadV2) {
    let len = 0;
    len += 4; // circularBufferMB
    len += 4; // format
    len += 1; // requestRundown
    len += 4; // providers length
    for (const provider of payload2.providers) {
        len += 8; // keywords
        len += 4; // level
        len += computeStringByteLength(provider.provider_name);
        len += computeStringByteLength(provider.arguments);
    }
    return len;
}

type ProviderV2 ={
    keywords: [ number, Keywords ],
    logLevel: number,
    provider_name: string,
    arguments: string|null
}

type PayloadV2 = {
    circularBufferMB: number,
    format: number,
    requestRundown: boolean,
    providers: ProviderV2[]
}
