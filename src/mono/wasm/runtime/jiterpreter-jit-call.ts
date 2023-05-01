// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert, MonoType, MonoMethod } from "./types";
import { NativePointer, Int32Ptr, VoidPtr } from "./types/emscripten";
import { Module, runtimeHelpers } from "./globals";
import {
    getU8, getI32_unaligned, getU32_unaligned, setU32_unchecked
} from "./memory";
import { WasmOpcode } from "./jiterpreter-opcodes";
import {
    WasmValtype, WasmBuilder, addWasmFunctionPointer as addWasmFunctionPointer,
    _now, elapsedTimes, counters, getWasmFunctionTable, applyOptions,
    recordFailure, getOptions
} from "./jiterpreter-support";
import cwraps from "./cwraps";

// Controls miscellaneous diagnostic output.
const trace = 0;
const
    // Dumps all compiled wrappers
    dumpWrappers = false,
    // Compiled wrappers will have the full name of the target method instead of the short
    //  disambiguated name. This adds overhead for jit calls that never get compiled
    useFullNames = false;

/*
struct _JitCallInfo {
    gpointer addr; // 0
    gpointer extra_arg; // 4
    gpointer wrapper; // 8
    MonoMethodSignature *sig; // 12
    guint8 *arginfo; // 16
    gint32 res_size; // 20
    int ret_mt; // 24
    gboolean no_wrapper; // 28
#if HOST_BROWSER
    int hit_count;
    WasmJitCallThunk jiterp_thunk;
#endif
};
*/

const offsetOfAddr = 0,
    // offsetOfExtraArg = 4,
    offsetOfWrapper = 8,
    offsetOfSig = 12,
    offsetOfArgInfo = 16,
    offsetOfRetMt = 24,
    offsetOfNoWrapper = 28,
    JIT_ARG_BYVAL = 0;

const maxJitQueueLength = 6,
    maxSharedQueueLength = 12;
// sizeOfStackval = 8;

let trampBuilder: WasmBuilder;
let fnTable: WebAssembly.Table;
let wasmEhSupported: boolean | undefined = undefined;
let nextDisambiguateIndex = 0;
const fnCache: Array<Function | undefined> = [];
const targetCache: { [target: number]: TrampolineInfo } = {};
const jitQueue: TrampolineInfo[] = [];

class TrampolineInfo {
    method: MonoMethod;
    rmethod: VoidPtr;
    cinfo: VoidPtr;
    hasThisReference: boolean;
    hasReturnValue: boolean;
    noWrapper: boolean;
    // The number of managed arguments (not including the this-reference or return val address)
    paramCount: number;
    // The managed type of each argument, not including the this-reference
    paramTypes: MonoType[];
    // The interpreter stack offset of each argument, in bytes. Indexes are one-based if
    //  the method has a this-reference (thisp is arg 0) and zero-based for static methods.
    // The return value address is not in here either because it's always at a fixed location.
    argOffsets: number[];
    catchExceptions: boolean;
    target: number; // either cinfo->wrapper or cinfo->addr, depending
    addr: number; // always cinfo->addr
    wrapper: number; // always cinfo->wrapper
    name: string;
    result: number;
    queue: NativePointer[] = [];
    signature: VoidPtr;
    returnType: MonoType;
    wasmNativeReturnType: WasmValtype;
    wasmNativeSignature: WasmValtype[];
    enableDirect: boolean;

    constructor(
        method: MonoMethod, rmethod: VoidPtr, cinfo: VoidPtr,
        arg_offsets: VoidPtr, catch_exceptions: boolean
    ) {
        this.method = method;
        this.rmethod = rmethod;
        this.catchExceptions = catch_exceptions;
        this.cinfo = cinfo;
        this.addr = getU32_unaligned(<any>cinfo + offsetOfAddr);
        this.wrapper = getU32_unaligned(<any>cinfo + offsetOfWrapper);
        this.signature = <any>getU32_unaligned(<any>cinfo + offsetOfSig);
        this.noWrapper = getU8(<any>cinfo + offsetOfNoWrapper) !== 0;
        this.hasReturnValue = getI32_unaligned(<any>cinfo + offsetOfRetMt) !== -1;

        this.returnType = cwraps.mono_jiterp_get_signature_return_type(this.signature);
        this.paramCount = cwraps.mono_jiterp_get_signature_param_count(this.signature);
        this.hasThisReference = cwraps.mono_jiterp_get_signature_has_this(this.signature) !== 0;

        const ptr = cwraps.mono_jiterp_get_signature_params(this.signature);
        this.paramTypes = new Array(this.paramCount);
        for (let i = 0; i < this.paramCount; i++)
            this.paramTypes[i] = <any>getU32_unaligned(<any>ptr + (i * 4));

        // See initialize_arg_offsets for where this array is built
        const argOffsetCount = this.paramCount + (this.hasThisReference ? 1 : 0);
        this.argOffsets = new Array(this.paramCount);
        for (let i = 0; i < argOffsetCount; i++)
            this.argOffsets[i] = <any>getU32_unaligned(<any>arg_offsets + (i * 4));

        this.target = this.noWrapper ? this.addr : this.wrapper;
        this.result = 0;

        this.wasmNativeReturnType = this.returnType && this.hasReturnValue
            ? (wasmTypeFromCilOpcode as any)[cwraps.mono_jiterp_type_to_stind(this.returnType)]
            : WasmValtype.void;
        this.wasmNativeSignature = this.paramTypes.map(
            monoType => (wasmTypeFromCilOpcode as any)[cwraps.mono_jiterp_type_to_ldind(monoType)]
        );
        this.enableDirect = getOptions().directJitCalls &&
            !this.noWrapper &&
            this.wasmNativeReturnType &&
            (
                (this.wasmNativeSignature.length === 0) ||
                this.wasmNativeSignature.every(vt => vt)
            );

        if (this.enableDirect)
            this.target = this.addr;

        let suffix = this.target.toString(16);
        if (useFullNames) {
            const pMethodName = method ? cwraps.mono_wasm_method_get_full_name(method) : <any>0;
            try {
                suffix = Module.UTF8ToString(pMethodName);
            } finally {
                if (pMethodName)
                    Module._free(<any>pMethodName);
            }
        }

        // FIXME: Without doing this we occasionally get name collisions while jitting.
        const disambiguate = nextDisambiguateIndex++;
        this.name = `${this.enableDirect ? "jcp" : "jcw"}_${suffix}_${disambiguate.toString(16)}`;
    }
}

// this is cached replacements for Module.getWasmTableEntry();
// we could add <EmccExportedLibraryFunction Include="$getWasmTableEntry" /> and <EmccExportedRuntimeMethod Include="getWasmTableEntry" /> 
// if we need to export the original
function getWasmTableEntry(index: number) {
    let result = fnCache[index];
    if (!result) {
        if (index >= fnCache.length)
            fnCache.length = index + 1;

        if (!fnTable)
            fnTable = getWasmFunctionTable();
        fnCache[index] = result = fnTable.get(index);
    }
    return result;
}

export function mono_interp_invoke_wasm_jit_call_trampoline(
    thunkIndex: number, ret_sp: number, sp: number, ftndesc: number, thrown: NativePointer
) {
    const thunk = <Function>getWasmTableEntry(thunkIndex);
    try {
        thunk(ret_sp, sp, ftndesc, thrown);
    } catch (exc) {
        setU32_unchecked(thrown, 1);
    }
}

export function mono_interp_jit_wasm_jit_call_trampoline(
    method: MonoMethod, rmethod: VoidPtr, cinfo: VoidPtr,
    arg_offsets: VoidPtr, catch_exceptions: number
): void {
    // multiple cinfos can share the same target function, so for that scenario we want to
    //  use the same TrampolineInfo for all of them. if that info has already been jitted
    //  we want to immediately store its pointer into the cinfo, otherwise we add it to
    //  a queue inside the info object so that all the cinfos will get updated once a
    //  jit operation happens
    const cacheKey = getU32_unaligned(<any>cinfo + offsetOfAddr),
        existing = targetCache[cacheKey];
    if (existing) {
        if (existing.result > 0)
            cwraps.mono_jiterp_register_jit_call_thunk(<any>cinfo, existing.result);
        else {
            existing.queue.push(cinfo);
            // the jitQueue might never fill up if we have a bunch of cinfos that share
            //  the same target function, and they might never hit the call count threshold
            //  to flush the jit queue from the C side. since entering the queue at all
            //  requires hitting a minimum hit count on the C side, flush if we have too many
            //  shared cinfos all waiting for a JIT to happen.
            if (existing.queue.length > maxSharedQueueLength)
                mono_interp_flush_jitcall_queue();
        }
        return;
    }

    const info = new TrampolineInfo(
        method, rmethod, cinfo,
        arg_offsets, catch_exceptions !== 0
    );
    targetCache[cacheKey] = info;
    jitQueue.push(info);

    // we don't want the queue to get too long, both because jitting too many trampolines
    //  at once can hit the 4kb limit and because it makes it more likely that we will
    //  fail to jit them early enough
    if (jitQueue.length >= maxJitQueueLength)
        mono_interp_flush_jitcall_queue();
}

// pure wasm implementation of do_jit_call_indirect (using wasm EH). see do-jit-call.wat / do-jit-call.wasm
const doJitCall16 =
    "0061736d01000000010b0260017f0060037f7f7f00021d020169066d656d6f727902000001690b6a69745f63616c6c5f636200000302010107180114646f5f6a69745f63616c6c5f696e64697265637400010a1301110006402001100019200241013602000b0b";
let doJitCallModule: WebAssembly.Module | undefined = undefined;

function getIsWasmEhSupported(): boolean {
    if (wasmEhSupported !== undefined)
        return wasmEhSupported;

    // Probe whether the current environment can handle wasm exceptions
    try {
        // Load and compile the wasm version of do_jit_call_indirect. This serves as a way to probe for wasm EH
        const bytes = new Uint8Array(doJitCall16.length / 2);
        for (let i = 0; i < doJitCall16.length; i += 2)
            bytes[i / 2] = parseInt(doJitCall16.substring(i, i + 2), 16);

        counters.bytesGenerated += bytes.length;
        doJitCallModule = new WebAssembly.Module(bytes);
        wasmEhSupported = true;
    } catch (exc) {
        console.log("MONO_WASM: Disabling WASM EH support due to JIT failure", exc);
        wasmEhSupported = false;
    }

    return wasmEhSupported;
}

// this is the generic entry point for do_jit_call that is registered by default at runtime startup.
// its job is to do initialization for the optimized do_jit_call path, which will either use a jitted
//  wasm trampoline or will use a specialized JS function.
export function mono_jiterp_do_jit_call_indirect(
    jit_call_cb: number, cb_data: VoidPtr, thrown: Int32Ptr
): void {
    mono_assert(!runtimeHelpers.storeMemorySnapshotPending, "Attempting to set function into table during creation of memory snapshot");
    const table = getWasmFunctionTable();
    const jitCallCb = table.get(jit_call_cb);

    // This should perform better than the regular mono_llvm_cpp_catch_exception because the call target
    //  is statically known, not being pulled out of a table.
    const do_jit_call_indirect_js = function (unused: number, _cb_data: VoidPtr, _thrown: Int32Ptr) {
        try {
            jitCallCb(_cb_data);
        } catch (exc) {
            setU32_unchecked(_thrown, 1);
        }
    };

    let failed = !getIsWasmEhSupported();
    if (!failed) {
        // Wasm EH is supported which means doJitCallModule was loaded and compiled.
        // Now that we have jit_call_cb, we can instantiate it.
        try {
            const instance = new WebAssembly.Instance(doJitCallModule!, {
                i: {
                    jit_call_cb: jitCallCb,
                    memory: (<any>Module).asm.memory
                }
            });
            const impl = instance.exports.do_jit_call_indirect;
            if (typeof (impl) !== "function")
                throw new Error("Did not find exported do_jit_call handler");

            // We successfully instantiated it so we can register it as the new do_jit_call handler
            const result = addWasmFunctionPointer(impl);
            cwraps.mono_jiterp_update_jit_call_dispatcher(result);
            failed = false;
        } catch (exc) {
            console.error("MONO_WASM: failed to compile do_jit_call handler", exc);
            failed = true;
        }
        // If wasm EH support was detected, a native wasm implementation of the dispatcher was already registered.
    }

    if (failed) {
        try {
            const result = Module.addFunction(do_jit_call_indirect_js, "viii");
            cwraps.mono_jiterp_update_jit_call_dispatcher(result);
        } catch {
            // CSP policy or some other problem could break Module.addFunction, so in that case, pass 0
            // This will cause the runtime to use mono_llvm_cpp_catch_exception
            cwraps.mono_jiterp_update_jit_call_dispatcher(0);
        }
    }

    do_jit_call_indirect_js(jit_call_cb, cb_data, thrown);
}

export function mono_interp_flush_jitcall_queue(): void {
    if (jitQueue.length === 0)
        return;

    let builder = trampBuilder;
    if (!builder) {
        trampBuilder = builder = new WasmBuilder(0);
        // Function type for compiled trampolines
        builder.defineType(
            "trampoline",
            {
                "ret_sp": WasmValtype.i32,
                "sp": WasmValtype.i32,
                "ftndesc": WasmValtype.i32,
                "thrown": WasmValtype.i32,
            }, WasmValtype.void, true
        );
    } else
        builder.clear(0);

    if (builder.options.wasmBytesLimit <= counters.bytesGenerated) {
        jitQueue.length = 0;
        return;
    }

    if (builder.options.enableWasmEh) {
        if (!getIsWasmEhSupported()) {
            // The user requested to enable wasm EH but it's not supported, so turn the option back off
            applyOptions(<any>{ enableWasmEh: false });
            builder.options.enableWasmEh = false;
        }
    }

    const started = _now();
    let compileStarted = 0;
    let rejected = true, threw = false;

    const trampImports: Array<[string, string, Function | number]> = [
    ];

    try {
        if (!fnTable)
            fnTable = getWasmFunctionTable();

        // Magic number and version
        builder.appendU32(0x6d736100);
        builder.appendU32(1);

        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];

            const sig: any = {};

            if (info.enableDirect) {
                if (info.hasThisReference)
                    sig["this"] = WasmValtype.i32;

                for (let j = 0; j < info.wasmNativeSignature.length; j++)
                    sig[`arg${j}`] = info.wasmNativeSignature[j];

                sig["rgctx"] = WasmValtype.i32;
            } else {
                const actualParamCount = (info.hasThisReference ? 1 : 0) +
                    (info.hasReturnValue ? 1 : 0) + info.paramCount;

                for (let j = 0; j < actualParamCount; j++)
                    sig[`arg${j}`] = WasmValtype.i32;

                sig["ftndesc"] = WasmValtype.i32;
            }

            builder.defineType(
                info.name, sig, info.enableDirect ? info.wasmNativeReturnType : WasmValtype.void, false
            );

            const callTarget = getWasmTableEntry(info.target);
            mono_assert(typeof (callTarget) === "function", () => `expected call target to be function but was ${callTarget}`);
            trampImports.push([info.name, info.name, callTarget]);
        }

        builder.generateTypeSection();
        builder.compressImportNames = true;

        // Emit function imports
        for (let i = 0; i < trampImports.length; i++)
            builder.defineImportedFunction("i", trampImports[i][0], trampImports[i][1], true, false, trampImports[i][2]);
        builder._generateImportSection();

        // Function section
        builder.beginSection(3);
        builder.appendULeb(jitQueue.length);
        // Function type for our compiled trampoline
        mono_assert(builder.functionTypes["trampoline"], "func type missing");

        for (let i = 0; i < jitQueue.length; i++)
            builder.appendULeb(builder.functionTypes["trampoline"][0]);

        // Export section
        builder.beginSection(7);
        builder.appendULeb(jitQueue.length);

        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];
            builder.appendName(info.name);
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
            builder.beginFunction("trampoline", { "old_sp": WasmValtype.i32 });

            const ok = generate_wasm_body(builder, info);
            // FIXME
            if (!ok)
                throw new Error(`Failed to generate ${info.name}`);
            builder.appendU8(WasmOpcode.end);
            builder.endFunction(true);
        }

        builder.endSection();

        compileStarted = _now();
        const buffer = builder.getArrayView();
        if (trace > 0)
            console.log(`do_jit_call queue flush generated ${buffer.length} byte(s) of wasm`);
        counters.bytesGenerated += buffer.length;
        const traceModule = new WebAssembly.Module(buffer);

        const traceInstance = new WebAssembly.Instance(traceModule, {
            i: builder.getImportedFunctionTable(),
            c: <any>builder.getConstants(),
            m: { h: (<any>Module).asm.memory }
        });

        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];

            // Get the exported trace function
            const jitted = <Function>traceInstance.exports[info.name];
            const idx = addWasmFunctionPointer(jitted);
            if (!idx)
                throw new Error("add_function_pointer returned a 0 index");
            else if (trace >= 2)
                console.log(`${info.name} -> fn index ${idx}`);

            info.result = idx;
            cwraps.mono_jiterp_register_jit_call_thunk(<any>info.cinfo, idx);
            for (let j = 0; j < info.queue.length; j++)
                cwraps.mono_jiterp_register_jit_call_thunk(<any>info.queue[j], idx);

            if (info.enableDirect)
                counters.directJitCallsCompiled++;
            counters.jitCallsCompiled++;
            info.queue.length = 0;
            rejected = false;
        }
    } catch (exc: any) {
        threw = true;
        rejected = false;
        // console.error(`${traceName} failed: ${exc} ${exc.stack}`);
        // HACK: exc.stack is enormous garbage in v8 console
        console.error(`MONO_WASM: jit_call code generation failed: ${exc}`);
        recordFailure();
    } finally {
        const finished = _now();
        if (compileStarted) {
            elapsedTimes.generation += compileStarted - started;
            elapsedTimes.compilation += finished - compileStarted;
        } else {
            elapsedTimes.generation += finished - started;
        }

        if (threw || rejected) {
            for (let i = 0; i < jitQueue.length; i++) {
                const info = jitQueue[i];
                info.result = -1;
            }
        }

        // FIXME
        if (threw || (!rejected && ((trace >= 2) || dumpWrappers))) {
            console.log(`// MONO_WASM: ${jitQueue.length} jit call wrappers generated, blob follows //`);
            for (let i = 0; i < jitQueue.length; i++)
                console.log(`// #${i} === ${jitQueue[i].name} hasThis=${jitQueue[i].hasThisReference} hasRet=${jitQueue[i].hasReturnValue} wasmArgTypes=${jitQueue[i].wasmNativeSignature}`);

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
                    console.log(`${j}\t${s}`);
                    s = "";
                    j = i + 1;
                }
            }
            console.log(`${j}\t${s}`);
            console.log("// end blob //");
        } else if (rejected && !threw) {
            console.error("MONO_WASM: failed to generate trampoline for unknown reason");
        }

        jitQueue.length = 0;
    }
}

// To perform direct jit calls we have to emulate the semantics of generated AOT wrappers
// Wrappers are generated in CIL, so we work in CIL as well and reuse some of the generator

// Only the subset of CIL opcodes used by the wrapper generator in mini-generic-sharing.c
const enum CilOpcodes {
    NOP = 0,

    LDIND_I1 = 0x46,
    LDIND_U1,
    LDIND_I2,
    LDIND_U2,
    LDIND_I4,
    LDIND_U4,
    LDIND_I8,
    LDIND_I,
    LDIND_R4,
    LDIND_R8,
    LDIND_REF,
    STIND_REF = 0x51,
    STIND_I1,
    STIND_I2,
    STIND_I4,
    STIND_I8,
    STIND_R4,
    STIND_R8,
    STIND_I = 0xDF,

    LDOBJ = 0x71,
    STOBJ = 0x81,

    DUMMY_BYREF = 0xFFFF // Placeholder for byref pointers that don't need an indirect op
}

// Maps a CIL ld/st opcode to the wasm type that will represent it
// We intentionally leave some opcodes out in order to disable direct calls
//  for wrappers that use that opcode.
const wasmTypeFromCilOpcode = {
    [CilOpcodes.DUMMY_BYREF]: WasmValtype.i32,

    [CilOpcodes.LDIND_I1]: WasmValtype.i32,
    [CilOpcodes.LDIND_U1]: WasmValtype.i32,
    [CilOpcodes.LDIND_I2]: WasmValtype.i32,
    [CilOpcodes.LDIND_U2]: WasmValtype.i32,
    [CilOpcodes.LDIND_I4]: WasmValtype.i32,
    [CilOpcodes.LDIND_U4]: WasmValtype.i32,
    [CilOpcodes.LDIND_I8]: WasmValtype.i64,
    [CilOpcodes.LDIND_I]: WasmValtype.i32,
    [CilOpcodes.LDIND_R4]: WasmValtype.f32,
    [CilOpcodes.LDIND_R8]: WasmValtype.f64,
    [CilOpcodes.LDIND_REF]: WasmValtype.i32,
    [CilOpcodes.STIND_REF]: WasmValtype.i32,
    [CilOpcodes.STIND_I1]: WasmValtype.i32,
    [CilOpcodes.STIND_I2]: WasmValtype.i32,
    [CilOpcodes.STIND_I4]: WasmValtype.i32,
    [CilOpcodes.STIND_I8]: WasmValtype.i64,
    [CilOpcodes.STIND_R4]: WasmValtype.f32,
    [CilOpcodes.STIND_R8]: WasmValtype.f64,
    [CilOpcodes.STIND_I]: WasmValtype.i32,
};

// Maps a CIL ld/st opcode to the wasm opcode to perform it, if any
const wasmOpcodeFromCilOpcode = {
    [CilOpcodes.LDIND_I1]: WasmOpcode.i32_load8_s,
    [CilOpcodes.LDIND_U1]: WasmOpcode.i32_load8_u,
    [CilOpcodes.LDIND_I2]: WasmOpcode.i32_load16_s,
    [CilOpcodes.LDIND_U2]: WasmOpcode.i32_load16_u,
    [CilOpcodes.LDIND_I4]: WasmOpcode.i32_load,
    [CilOpcodes.LDIND_U4]: WasmOpcode.i32_load,
    [CilOpcodes.LDIND_I8]: WasmOpcode.i64_load,
    [CilOpcodes.LDIND_I]: WasmOpcode.i32_load,
    [CilOpcodes.LDIND_R4]: WasmOpcode.f32_load,
    [CilOpcodes.LDIND_R8]: WasmOpcode.f64_load,
    [CilOpcodes.LDIND_REF]: WasmOpcode.i32_load, // TODO: Memory barrier?

    [CilOpcodes.STIND_REF]: WasmOpcode.i32_store, // Memory barrier not needed
    [CilOpcodes.STIND_I1]: WasmOpcode.i32_store8,
    [CilOpcodes.STIND_I2]: WasmOpcode.i32_store16,
    [CilOpcodes.STIND_I4]: WasmOpcode.i32_store,
    [CilOpcodes.STIND_I8]: WasmOpcode.i64_store,
    [CilOpcodes.STIND_R4]: WasmOpcode.f32_store,
    [CilOpcodes.STIND_R8]: WasmOpcode.f64_store,
    [CilOpcodes.STIND_I]: WasmOpcode.i32_store,
};

function append_ldloc(builder: WasmBuilder, offsetBytes: number, opcode: WasmOpcode) {
    builder.local("sp");
    builder.appendU8(opcode);
    builder.appendMemarg(offsetBytes, 0);
}

function append_ldloca(builder: WasmBuilder, offsetBytes: number) {
    builder.local("sp");
    builder.i32_const(offsetBytes);
    builder.appendU8(WasmOpcode.i32_add);
}

function generate_wasm_body(
    builder: WasmBuilder, info: TrampolineInfo
): boolean {
    let stack_index = 0;

    // If wasm EH is enabled we will perform the call inside a catch-all block and set a flag
    //  if it throws any exception
    if (builder.options.enableWasmEh)
        builder.block(WasmValtype.void, WasmOpcode.try_);

    // Wrapper signature: [thisptr], [&retval], &arg0, ..., &funcdef
    // Desired stack layout for direct calls: [&retval], [thisptr], arg0, ..., &rgctx

    /*
        if (sig->ret->type != MONO_TYPE_VOID)
            // Load return address
            mono_mb_emit_ldarg (mb, sig->hasthis ? 1 : 0);
    */
    // The return address comes first for direct calls so we can write into it after the call
    if (info.hasReturnValue && info.enableDirect)
        builder.local("ret_sp");

    /*
        if (sig->hasthis)
            mono_mb_emit_ldarg (mb, 0);
    */
    if (info.hasThisReference) {
        // The this-reference is always the first argument
        // Note that currently info.argOffsets[0] will always be 0, but it's best to
        //  read it from the array in case this behavior changes later.
        append_ldloc(builder, info.argOffsets[0], WasmOpcode.i32_load);
        stack_index++;
    }

    // Indirect passes the return address as the first post-this argument
    if (info.hasReturnValue && !info.enableDirect)
        builder.local("ret_sp");

    for (let i = 0; i < info.paramCount; i++) {
        // FIXME: STACK_ADD_BYTES does alignment, but we probably don't need to?
        const svalOffset = info.argOffsets[stack_index + i];
        const argInfoOffset = getU32_unaligned(<any>info.cinfo + offsetOfArgInfo) + i;
        const argInfo = getU8(argInfoOffset);

        if (argInfo == JIT_ARG_BYVAL) {
            // pass the first four bytes of the stackval data union,
            //  which is 'p' where pointers live
            append_ldloc(builder, svalOffset, WasmOpcode.i32_load);
        } else if (info.enableDirect) {
            // The wrapper call convention is byref for all args. Now we convert it to the native calling convention
            const loadCilOp = cwraps.mono_jiterp_type_to_ldind(info.paramTypes[i]);
            mono_assert(loadCilOp, () => `No load opcode for ${info.paramTypes[i]}`);

            /*
                if (m_type_is_byref (sig->params [i])) {
                    mono_mb_emit_ldarg (mb, args_start + i);
                } else {
                    ldind_op = mono_type_to_ldind (sig->params [i]);
                    mono_mb_emit_ldarg (mb, args_start + i);
                    // FIXME:
                    if (ldind_op == CEE_LDOBJ)
                        mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type_internal (sig->params [i]));
                    else
                        mono_mb_emit_byte (mb, ldind_op);
            */

            if (loadCilOp === CilOpcodes.DUMMY_BYREF) {
                // pass the address of the stackval data union
                append_ldloca(builder, svalOffset);
            } else {
                const loadWasmOp = (wasmOpcodeFromCilOpcode as any)[loadCilOp];
                if (!loadWasmOp) {
                    console.error(`No wasm load op for arg #${i} type ${info.paramTypes[i]} cil opcode ${loadCilOp}`);
                    return false;
                }

                // FIXME: LDOBJ is not implemented
                append_ldloc(builder, svalOffset, loadWasmOp);
            }
        } else {
            // pass the address of the stackval data union
            append_ldloca(builder, svalOffset);
        }
    }

    /*
    // Rgctx arg
    mono_mb_emit_ldarg (mb, args_start + sig->param_count);
    mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P);
    mono_mb_emit_byte (mb, CEE_ADD);
    mono_mb_emit_byte (mb, CEE_LDIND_I);
    */

    // We have to pass the ftndesc through from do_jit_call because the target function needs
    //  a rgctx value, which is not constant for a given wrapper if the target function is shared
    //  for multiple InterpMethods. We pass ftndesc instead of rgctx so that we can pass the
    //  address to gsharedvt wrappers without having to do our own stackAlloc
    builder.local("ftndesc");
    if (info.enableDirect || info.noWrapper) {
        // Native calling convention wants an rgctx, not a ftndesc. The rgctx
        //  lives at offset 4 in the ftndesc, after the call target
        builder.appendU8(WasmOpcode.i32_load);
        builder.appendMemarg(4, 0);
    }

    /*
    // Method to call
    mono_mb_emit_ldarg (mb, args_start + sig->param_count);
    mono_mb_emit_byte (mb, CEE_LDIND_I);
    mono_mb_emit_calli (mb, normal_sig);
    */

    builder.callImport(info.name);

    /*
    if (sig->ret->type != MONO_TYPE_VOID) {
        // Store return value
        stind_op = mono_type_to_stind (sig->ret);
        // FIXME:
        if (stind_op == CEE_STOBJ)
            mono_mb_emit_op (mb, CEE_STOBJ, mono_class_from_mono_type_internal (sig->ret));
        else if (stind_op == CEE_STIND_REF)
            // Avoid write barriers, the vret arg points to the stack
            mono_mb_emit_byte (mb, CEE_STIND_I);
        else
            mono_mb_emit_byte (mb, stind_op);
    }
    */

    // The stack should now contain [ret_sp, retval], so write retval through the return address
    if (info.hasReturnValue && info.enableDirect) {
        const storeCilOp = cwraps.mono_jiterp_type_to_stind(info.returnType);
        const storeWasmOp = (wasmOpcodeFromCilOpcode as any)[storeCilOp];
        if (!storeWasmOp) {
            console.error(`No wasm store op for return type ${info.returnType} cil opcode ${storeCilOp}`);
            return false;
        }

        // FIXME: STOBJ is not implemented
        // NOTE: We don't need a write barrier because the return address is on the interp stack
        builder.appendU8(storeWasmOp);
        builder.appendMemarg(0, 0);
    }

    // If the call threw a JS or wasm exception, set the thrown flag
    if (builder.options.enableWasmEh) {
        builder.appendU8(WasmOpcode.catch_all);
        builder.local("thrown");
        builder.i32_const(1);
        builder.appendU8(WasmOpcode.i32_store);
        builder.appendMemarg(0, 2);

        builder.endBlock();
    }

    builder.appendU8(WasmOpcode.return_);

    return true;
}
