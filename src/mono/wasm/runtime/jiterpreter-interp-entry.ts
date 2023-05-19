// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoMethod, MonoType } from "./types/internal";
import { NativePointer } from "./types/emscripten";
import { Module } from "./globals";
import {
    setI32, getU32_unaligned, _zero_region
} from "./memory";
import { WasmOpcode } from "./jiterpreter-opcodes";
import cwraps from "./cwraps";
import {
    WasmValtype, WasmBuilder, addWasmFunctionPointer,
    _now, elapsedTimes, counters, getRawCwrap, importDef,
    getWasmFunctionTable, recordFailure, getOptions,
    JiterpreterOptions, getMemberOffset, JiterpMember
} from "./jiterpreter-support";
import { mono_log_error, mono_log_info } from "./logging";

// Controls miscellaneous diagnostic output.
const trace = 0;
const
    // Dumps all compiled wrappers
    dumpWrappers = false;

/*
typedef struct {
    InterpMethod *rmethod;
    gpointer this_arg;
    gpointer res;
    gpointer args [16];
    gpointer *many_args;
} InterpEntryData;

typedef struct {
    InterpMethod *rmethod; // 0
    ThreadContext *context; // 4
    gpointer orig_domain; // 8
    gpointer attach_cookie; // 12
    int params_count; // 16
} JiterpEntryDataHeader;
*/

const
    maxInlineArgs = 16,
    // just allocate a bunch of extra space
    sizeOfJiterpEntryData = 64;

const maxJitQueueLength = 4,
    queueFlushDelayMs = 10;

let trampBuilder: WasmBuilder;
let trampImports: Array<[string, string, Function]> | undefined;
let fnTable: WebAssembly.Table;
let jitQueueTimeout = 0;
const jitQueue: TrampolineInfo[] = [];
const infoTable: { [ptr: number]: TrampolineInfo } = {};

/*
const enum WasmReftype {
    funcref = 0x70,
    externref = 0x6F,
}
*/

function getTrampImports() {
    if (trampImports)
        return trampImports;

    trampImports = [
        importDef("interp_entry_prologue", getRawCwrap("mono_jiterp_interp_entry_prologue")),
        importDef("interp_entry", getRawCwrap("mono_jiterp_interp_entry")),
        importDef("unbox", getRawCwrap("mono_jiterp_object_unbox")),
        importDef("stackval_from_data", getRawCwrap("mono_jiterp_stackval_from_data")),
    ];

    return trampImports;
}

class TrampolineInfo {
    imethod: number;
    method: MonoMethod;
    paramTypes: Array<NativePointer>;

    argumentCount: number;
    hasThisReference: boolean;
    unbox: boolean;
    hasReturnValue: boolean;
    name: string;
    traceName: string;

    defaultImplementation: number;
    result: number;
    hitCount: number;

    constructor(
        imethod: number, method: MonoMethod, argumentCount: number, pParamTypes: NativePointer,
        unbox: boolean, hasThisReference: boolean, hasReturnValue: boolean, name: string,
        defaultImplementation: number
    ) {
        this.imethod = imethod;
        this.method = method;
        this.argumentCount = argumentCount;
        this.unbox = unbox;
        this.hasThisReference = hasThisReference;
        this.hasReturnValue = hasReturnValue;
        this.name = name;
        this.paramTypes = new Array(argumentCount);
        for (let i = 0; i < argumentCount; i++)
            this.paramTypes[i] = <any>getU32_unaligned(<any>pParamTypes + (i * 4));
        this.defaultImplementation = defaultImplementation;
        this.result = 0;
        let subName = name;
        if (!subName) {
            subName = `${this.imethod.toString(16)}_${this.hasThisReference ? "i" : "s"}${this.hasReturnValue ? "_r" : ""}_${this.argumentCount}`;
        } else {
            // truncate the real method name so that it doesn't make the module too big. this isn't a big deal for module-per-function,
            //  but since we jit in groups now we need to keep the sizes reasonable. we keep the tail end of the name
            //  since it is likely to contain the method name and/or signature instead of type and noise
            const maxLength = 24;
            if (subName.length > maxLength)
                subName = subName.substring(subName.length - maxLength, subName.length);
            subName = `${this.imethod.toString(16)}_${subName}`;
        }
        this.traceName = subName;
        this.hitCount = 0;
    }
}

let mostRecentOptions: JiterpreterOptions | undefined = undefined;

export function mono_interp_record_interp_entry(imethod: number) {
    // clear the unbox bit
    imethod = imethod & ~0x1;

    const info = infoTable[imethod];
    // This shouldn't happen but it's not worth crashing over
    if (!info)
        return;

    if (!mostRecentOptions)
        mostRecentOptions = getOptions();

    info.hitCount++;
    if (info.hitCount === mostRecentOptions!.interpEntryFlushThreshold)
        flush_wasm_entry_trampoline_jit_queue();
    else if (info.hitCount !== mostRecentOptions!.interpEntryHitCount)
        return;

    jitQueue.push(info);
    if (jitQueue.length >= maxJitQueueLength)
        flush_wasm_entry_trampoline_jit_queue();
    else
        ensure_jit_is_scheduled();
}

// returns function pointer
export function mono_interp_jit_wasm_entry_trampoline(
    imethod: number, method: MonoMethod, argumentCount: number, pParamTypes: NativePointer,
    unbox: boolean, hasThisReference: boolean, hasReturnValue: boolean, name: NativePointer,
    defaultImplementation: number
): number {
    // HACK
    if (argumentCount > maxInlineArgs)
        return 0;

    const info = new TrampolineInfo(
        imethod, method, argumentCount, pParamTypes,
        unbox, hasThisReference, hasReturnValue, Module.UTF8ToString(<any>name),
        defaultImplementation
    );
    if (!fnTable)
        fnTable = getWasmFunctionTable();

    // We start by creating a function pointer for this interp_entry trampoline, but instead of
    //  compiling it right away, we make it point to the default implementation for that signature
    // This gives us time to wait before jitting it so we can jit multiple trampolines at once.
    // Some entry wrappers are also only called a few dozen times, so it's valuable to wait
    //  until a wrapper is called a lot before wasting time/memory jitting it.
    const defaultImplementationFn = fnTable.get(defaultImplementation);
    info.result = addWasmFunctionPointer(defaultImplementationFn);

    infoTable[imethod] = info;
    return info.result;
}

function ensure_jit_is_scheduled() {
    if (jitQueueTimeout > 0)
        return;

    if (typeof (globalThis.setTimeout) !== "function")
        return;

    // We only want to wait a short period of time before jitting the trampolines.
    // In practice the queue should fill up pretty fast during startup, and we just
    //  want to make sure we catch the last few stragglers with this timeout handler.
    // Note that in console JS runtimes this means we will never automatically flush
    //  the queue unless it fills up, which is unfortunate but not fixable since
    //  there is no realistic way to efficiently maintain a hit counter for these trampolines
    jitQueueTimeout = globalThis.setTimeout(() => {
        jitQueueTimeout = 0;
        flush_wasm_entry_trampoline_jit_queue();
    }, queueFlushDelayMs);
}

function flush_wasm_entry_trampoline_jit_queue() {
    if (jitQueue.length <= 0)
        return;

    // If the function signature contains types that need stackval_from_data, that'll use
    //  some constant slots, so make some extra space
    const constantSlots = (4 * jitQueue.length) + 1;
    let builder = trampBuilder;
    if (!builder) {
        trampBuilder = builder = new WasmBuilder(constantSlots);

        builder.defineType(
            "unbox",
            {
                "pMonoObject": WasmValtype.i32,
            },
            WasmValtype.i32, true
        );
        builder.defineType(
            "interp_entry_prologue",
            {
                "pData": WasmValtype.i32,
                "this_arg": WasmValtype.i32,
            },
            WasmValtype.i32, true
        );
        builder.defineType(
            "interp_entry",
            {
                "pData": WasmValtype.i32,
                "res": WasmValtype.i32,
            },
            WasmValtype.void, true
        );
        builder.defineType(
            "stackval_from_data",
            {
                "type": WasmValtype.i32,
                "result": WasmValtype.i32,
                "value": WasmValtype.i32
            },
            WasmValtype.void, true
        );
    } else
        builder.clear(constantSlots);

    if (builder.options.wasmBytesLimit <= counters.bytesGenerated) {
        jitQueue.length = 0;
        return;
    }

    const started = _now();
    let compileStarted = 0;
    let rejected = true, threw = false;

    try {
        // Magic number and version
        builder.appendU32(0x6d736100);
        builder.appendU32(1);

        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];

            const sig: any = {};
            if (info.hasThisReference)
                sig["this_arg"] = WasmValtype.i32;
            if (info.hasReturnValue)
                sig["res"] = WasmValtype.i32;
            for (let i = 0; i < info.argumentCount; i++)
                sig[`arg${i}`] = WasmValtype.i32;
            sig["rmethod"] = WasmValtype.i32;

            // Function type for compiled traces
            builder.defineType(
                info.traceName, sig, WasmValtype.void, false
            );
        }

        builder.generateTypeSection();

        // Import section
        const trampImports = getTrampImports();
        builder.compressImportNames = true;

        // Emit function imports
        for (let i = 0; i < trampImports.length; i++) {
            mono_assert(trampImports[i], () => `trace #${i} missing`);
            builder.defineImportedFunction("i", trampImports[i][0], trampImports[i][1], true, trampImports[i][2]);
        }

        // Assign import indices so they get emitted in the import section
        for (let i = 0; i < trampImports.length; i++)
            builder.markImportAsUsed(trampImports[i][0]);

        builder._generateImportSection();

        // Function section
        builder.beginSection(3);
        builder.appendULeb(jitQueue.length);
        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];
            // Function type for our compiled trace
            mono_assert(builder.functionTypes[info.traceName], "func type missing");
            builder.appendULeb(builder.functionTypes[info.traceName][0]);
        }

        // Export section
        builder.beginSection(7);
        builder.appendULeb(jitQueue.length);
        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];
            builder.appendName(info.traceName);
            builder.appendU8(0);
            // Imports get added to the function index space, so we need to add
            //  the count of imported functions to get the index of our compiled trace
            builder.appendULeb(builder.importedFunctionCount + i);
        }

        // Code section
        builder.beginSection(10);
        builder.appendULeb(jitQueue.length);
        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];
            builder.beginFunction(info.traceName, {
                "sp_args": WasmValtype.i32,
                "need_unbox": WasmValtype.i32,
                "scratchBuffer": WasmValtype.i32,
            });

            const ok = generate_wasm_body(builder, info);
            if (!ok)
                throw new Error(`Failed to generate ${info.traceName}`);

            builder.appendU8(WasmOpcode.end);
            builder.endFunction(true);
        }

        builder.endSection();

        compileStarted = _now();
        const buffer = builder.getArrayView();
        if (trace > 0)
            mono_log_info(`jit queue generated ${buffer.length} byte(s) of wasm`);
        counters.bytesGenerated += buffer.length;
        const traceModule = new WebAssembly.Module(buffer);
        const wasmImports = builder.getWasmImports();

        const traceInstance = new WebAssembly.Instance(traceModule, wasmImports);

        // Now that we've jitted the trampolines, go through and fix up the function pointers
        //  to point to the new jitted trampolines instead of the default implementations
        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];

            // Get the exported trampoline
            const fn = traceInstance.exports[info.traceName];
            // Patch the function pointer for this function to use the trampoline now
            fnTable.set(info.result, fn);

            rejected = false;
            counters.entryWrappersCompiled++;
        }
    } catch (exc: any) {
        threw = true;
        rejected = false;
        // console.error(`${traceName} failed: ${exc} ${exc.stack}`);
        // HACK: exc.stack is enormous garbage in v8 console
        mono_log_error(`interp_entry code generation failed: ${exc}`);
        recordFailure();
    } finally {
        const finished = _now();
        if (compileStarted) {
            elapsedTimes.generation += compileStarted - started;
            elapsedTimes.compilation += finished - compileStarted;
        } else {
            elapsedTimes.generation += finished - started;
        }

        if (threw || (!rejected && ((trace >= 2) || dumpWrappers))) {
            mono_log_info(`// ${jitQueue.length} trampolines generated, blob follows //`);
            let s = "", j = 0;
            try {
                if (builder.inSection)
                    builder.endSection();
            } catch {
                // eslint-disable-next-line @typescript-eslint/no-extra-semi
                ;
            }

            const buf = builder.getArrayView();
            for (let i = 0; i < buf.length; i++) {
                const b = buf[i];
                if (b < 0x10)
                    s += "0";
                s += b.toString(16);
                s += " ";
                if ((s.length % 10) === 0) {
                    mono_log_info(`${j}\t${s}`);
                    s = "";
                    j = i + 1;
                }
            }
            mono_log_info(`${j}\t${s}`);
            mono_log_info("// end blob //");
        } else if (rejected && !threw) {
            mono_log_error("failed to generate trampoline for unknown reason");
        }

        jitQueue.length = 0;
    }
}

function append_stackval_from_data(
    builder: WasmBuilder, imethod: number, type: MonoType, valueName: string, argIndex: number
) {
    const rawSize = cwraps.mono_jiterp_type_get_raw_value_size(type);
    const offset = cwraps.mono_jiterp_get_arg_offset(imethod, 0, argIndex);

    switch (rawSize) {
        case 256: {
            // Copy pointers directly
            builder.local("sp_args");
            builder.local(valueName);

            builder.appendU8(WasmOpcode.i32_store);
            builder.appendMemarg(offset, 2);
            break;
        }

        case -1:
        case -2:
        case 1:
        case 2:
        case 4: {
            // De-reference small primitives and then store them directly
            builder.local("sp_args");
            builder.local(valueName);

            switch (rawSize) {
                case -1:
                    builder.appendU8(WasmOpcode.i32_load8_u);
                    builder.appendMemarg(0, 0);
                    break;
                case 1:
                    builder.appendU8(WasmOpcode.i32_load8_s);
                    builder.appendMemarg(0, 0);
                    break;
                case -2:
                    builder.appendU8(WasmOpcode.i32_load16_u);
                    builder.appendMemarg(0, 0);
                    break;
                case 2:
                    builder.appendU8(WasmOpcode.i32_load16_s);
                    builder.appendMemarg(0, 0);
                    break;
                case 4:
                    builder.appendU8(WasmOpcode.i32_load);
                    builder.appendMemarg(0, 2);
                    break;
                // FIXME: 8-byte ints (unaligned)
                // FIXME: 4 and 8-byte floats (unaligned)
            }

            builder.appendU8(WasmOpcode.i32_store);
            builder.appendMemarg(offset, 2);
            break;
        }

        default: {
            // Call stackval_from_data to copy the value
            builder.ptr_const(type);
            // result
            builder.local("sp_args");
            // apply offset
            builder.i32_const(offset);
            builder.appendU8(WasmOpcode.i32_add);
            // value
            builder.local(valueName);

            builder.callImport("stackval_from_data");
            break;
        }
    }
}

function generate_wasm_body(
    builder: WasmBuilder, info: TrampolineInfo
): boolean {
    // FIXME: This is not thread-safe, but the alternative of alloca makes the trampoline
    //  more expensive
    // The solution is likely to put the address of the scratch buffer in a global that we provide
    //  at module instantiation time, so each thread can malloc its own copy of the buffer
    //  and then pass it in when instantiating instead of compiling the constant into the module
    // FIXME: Pre-allocate these buffers and their constant slots at the start before we
    //  generate function bodies, so that even if we run out of constant slots for MonoType we
    //  will always have put the buffers in a constant slot. This will be necessary for thread safety
    const scratchBuffer = <any>Module._malloc(sizeOfJiterpEntryData);
    _zero_region(scratchBuffer, sizeOfJiterpEntryData);

    // Initialize the parameter count in the data blob. This is used to calculate the new value of sp
    //  before entering the interpreter
    setI32(
        scratchBuffer + getMemberOffset(JiterpMember.ParamsCount),
        info.paramTypes.length + (info.hasThisReference ? 1 : 0)
    );

    // the this-reference may be a boxed struct that needs to be unboxed, for example calling
    //  methods like object.ToString on structs will end up with the unbox flag set
    // instead of passing an extra 'unbox' argument to every wrapper, though, the flag is hidden
    //  inside the rmethod/imethod parameter in the lowest bit (1), so we need to check it
    if (info.hasThisReference) {
        builder.block();
        // Find the unbox-this-reference flag in rmethod
        builder.local("rmethod");
        builder.i32_const(0x1);
        builder.appendU8(WasmOpcode.i32_and);
        // If the flag is not set (rmethod & 0x1) == 0 then skip the unbox operation
        builder.appendU8(WasmOpcode.i32_eqz);
        builder.appendU8(WasmOpcode.br_if);
        builder.appendULeb(0);
        // otherwise, the flag was set, so unbox the this reference and update the local
        builder.local("this_arg");
        builder.callImport("unbox");
        builder.local("this_arg", WasmOpcode.set_local);
        builder.endBlock();
    }

    // Populate the scratch buffer containing call data
    builder.ptr_const(scratchBuffer);
    builder.local("scratchBuffer", WasmOpcode.tee_local);

    builder.local("rmethod");
    // Clear the unbox-this-reference flag if present (see above) so that rmethod is a valid ptr
    builder.i32_const(~0x1);
    builder.appendU8(WasmOpcode.i32_and);

    // Store the cleaned up rmethod value into the data.rmethod field of the scratch buffer
    builder.appendU8(WasmOpcode.i32_store);
    builder.appendMemarg(getMemberOffset(JiterpMember.Rmethod), 0); // data.rmethod

    // prologue takes data->rmethod and initializes data->context, then returns a value for sp_args
    // prologue also performs thread attach
    builder.local("scratchBuffer");
    // prologue takes this_arg so it can handle delegates
    if (info.hasThisReference)
        builder.local("this_arg");
    else
        builder.i32_const(0);
    builder.callImport("interp_entry_prologue");
    builder.local("sp_args", WasmOpcode.set_local);

    /*
    if (sig->hasthis) {
        sp_args->data.p = data->this_arg;
        sp_args++;
    }
    */

    if (info.hasThisReference) {
        // null type for raw ptr copy
        append_stackval_from_data(builder, info.imethod, <any>0, "this_arg", 0);
    }

    /*
    for (i = 0; i < sig->param_count; ++i) {
        if (m_type_is_byref (sig->params [i])) {
            sp_args->data.p = params [i];
            sp_args++;
        } else {
            int size = stackval_from_data (sig->params [i], sp_args, params [i], FALSE);
            sp_args = STACK_ADD_BYTES (sp_args, size);
        }
    }
    */

    for (let i = 0; i < info.paramTypes.length; i++) {
        const type = <any>info.paramTypes[i];
        append_stackval_from_data(builder, info.imethod, type, `arg${i}`, i + (info.hasThisReference ? 1 : 0));
    }

    builder.local("scratchBuffer");
    if (info.hasReturnValue)
        builder.local("res");
    else
        builder.i32_const(0);
    builder.callImport("interp_entry");
    builder.appendU8(WasmOpcode.return_);

    return true;
}
