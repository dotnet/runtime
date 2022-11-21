// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert } from "./types";
import { NativePointer, Int32Ptr } from "./types/emscripten";
import { Module } from "./imports";
import {
    getU8, getI32, getU32, setU32_unchecked
} from "./memory";
import { WasmOpcode } from "./jiterpreter-opcodes";
import {
    WasmValtype, WasmBuilder, addWasmFunctionPointer as addWasmFunctionPointer,
    _now, elapsedTimes, counters, getWasmFunctionTable, applyOptions, recordFailure
} from "./jiterpreter-support";
import cwraps from "./cwraps";

// Controls miscellaneous diagnostic output.
const trace = 0;
const
    // Dumps all compiled wrappers
    dumpWrappers = false;

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

const offsetOfArgInfo = 16,
    offsetOfRetMt = 24;

const maxJitQueueLength = 4,
    maxSharedQueueLength = 12,
    flushParamThreshold = 7;

let trampBuilder : WasmBuilder;
let fnTable : WebAssembly.Table;
let wasmEhSupported : boolean | undefined = undefined;
const fnCache : Array<Function | undefined> = [];
const targetCache : { [target: number] : TrampolineInfo } = {};
const jitQueue : TrampolineInfo[] = [];

class TrampolineInfo {
    rmethod: NativePointer;
    cinfo: NativePointer;
    hasThisReference: boolean;
    hasReturnValue: boolean;
    paramCount: number;
    argOffsets: number[];
    catchExceptions: boolean;
    target: number;
    name: string;
    result: number;
    queue: NativePointer[] = [];

    constructor (
        rmethod: NativePointer, cinfo: NativePointer, has_this: boolean, param_count: number,
        arg_offsets: NativePointer, catch_exceptions: boolean, func: number
    ) {
        this.rmethod = rmethod;
        this.cinfo = cinfo;
        this.hasThisReference = has_this;
        this.paramCount = param_count;
        this.catchExceptions = catch_exceptions;
        this.argOffsets = new Array(param_count);
        this.hasReturnValue = getI32(<any>cinfo + offsetOfRetMt) !== -1;
        for (let i = 0, c = param_count + (has_this ? 1 : 0); i < c; i++)
            this.argOffsets[i] = <any>getU32(<any>arg_offsets + (i * 4));
        this.target = func;
        this.name = `jitcall_${func.toString(16)}`;
        this.result = 0;
    }
}

function getWasmTableEntry (index: number) {
    let result = fnCache[index];
    if (!result) {
        if (index >= fnCache.length)
            fnCache.length = index + 1;
        fnCache[index] = result = fnTable.get(index);
    }
    return result;
}

export function mono_interp_invoke_wasm_jit_call_trampoline (
    thunkIndex: number, extra_arg: number,
    ret_sp: number, sp: number, thrown: NativePointer
) {
    // FIXME: It's impossible to get emscripten to export this for some reason
    // const thunk = <Function>Module.getWasmTableEntry(thunkIndex);
    const thunk = <Function>getWasmTableEntry(thunkIndex);
    try {
        thunk(extra_arg, ret_sp, sp, thrown);
    } catch (exc) {
        setU32_unchecked(thrown, 1);
    }
}

export function mono_interp_jit_wasm_jit_call_trampoline (
    rmethod: NativePointer, cinfo: NativePointer, func: number,
    has_this: number, param_count: number,
    arg_offsets: NativePointer, catch_exceptions: number
) : void {
    // multiple cinfos can share the same target function, so for that scenario we want to
    //  use the same TrampolineInfo for all of them. if that info has already been jitted
    //  we want to immediately store its pointer into the cinfo, otherwise we add it to
    //  a queue inside the info object so that all the cinfos will get updated once a
    //  jit operation happens
    const existing = targetCache[func];
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
        rmethod, cinfo, has_this !== 0, param_count,
        arg_offsets, catch_exceptions !== 0, func
    );
    targetCache[func] = info;
    jitQueue.push(info);

    // we don't want the queue to get too long, both because jitting too many trampolines
    //  at once can hit the 4kb limit and because it makes it more likely that we will
    //  fail to jit them early enough
    // HACK: we also want to flush the queue when we get a function with many parameters,
    //  since it's going to generate a lot more code and push us closer to 4kb
    if ((info.paramCount >= flushParamThreshold) || (jitQueue.length >= maxJitQueueLength))
        mono_interp_flush_jitcall_queue();
}

// pure wasm implementation of do_jit_call_indirect (using wasm EH). see do-jit-call.wat / do-jit-call.wasm
const doJitCall16 =
    "0061736d01000000010b0260017f0060037f7f7f00021d020169066d656d6f727902000001690b6a69745f63616c6c5f636200000302010107180114646f5f6a69745f63616c6c5f696e64697265637400010a1301110006402001100019200241013602000b0b";
let doJitCallModule : WebAssembly.Module | undefined = undefined;

function getIsWasmEhSupported () : boolean {
    if (wasmEhSupported !== undefined)
        return wasmEhSupported;

    // Probe whether the current environment can handle wasm exceptions
    try {
        // Load and compile the wasm version of do_jit_call_indirect. This serves as a way to probe for wasm EH
        const bytes = new Uint8Array(doJitCall16.length / 2);
        for (let i = 0; i < doJitCall16.length; i += 2)
            bytes[i / 2] = parseInt(doJitCall16.substring(i, i + 2), 16);

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
export function mono_jiterp_do_jit_call_indirect (
    jit_call_cb: number, cb_data: NativePointer, thrown: Int32Ptr
) : void {
    const table = getWasmFunctionTable();
    const jitCallCb = table.get(jit_call_cb);

    // This should perform better than the regular mono_llvm_cpp_catch_exception because the call target
    //  is statically known, not being pulled out of a table.
    const do_jit_call_indirect_js = function (unused: number, _cb_data: NativePointer, _thrown: Int32Ptr) {
        try {
            jitCallCb(_cb_data);
        } catch {
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

            // console.log("registering wasm jit call dispatcher");
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
        // console.log("registering JS jit call dispatcher");
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

export function mono_interp_flush_jitcall_queue () : void {
    if (jitQueue.length === 0)
        return;

    let builder = trampBuilder;
    if (!builder)
        trampBuilder = builder = new WasmBuilder();
    else
        builder.clear();

    if (builder.options.enableWasmEh) {
        if (!getIsWasmEhSupported()) {
            // The user requested to enable wasm EH but it's not supported, so turn the option back off
            applyOptions(<any>{enableWasmEh: false});
            builder.options.enableWasmEh = false;
        }
    }

    const started = _now();
    let compileStarted = 0;
    let rejected = true, threw = false;

    const trampImports : Array<[string, string, Function]> = [];

    try {
        if (!fnTable)
            fnTable = getWasmFunctionTable();

        // Magic number and version
        builder.appendU32(0x6d736100);
        builder.appendU32(1);

        // Function type for compiled trampolines
        builder.defineType(
            "trampoline", {
                "extra_arg": WasmValtype.i32,
                "ret_sp": WasmValtype.i32,
                "sp": WasmValtype.i32,
                "thrown": WasmValtype.i32,
            }, WasmValtype.void
        );

        for (let i = 0; i < jitQueue.length; i++) {
            const info = jitQueue[i];
            const ctn = `fn${info.target.toString(16)}`;

            const actualParamCount = (info.hasThisReference ? 1 : 0) + (info.hasReturnValue ? 1 : 0) + info.paramCount;
            const sig : any = {};
            for (let j = 0; j < actualParamCount; j++)
                sig[`arg${j}`] = WasmValtype.i32;
            sig["extra_arg"] = WasmValtype.i32;
            builder.defineType(
                ctn, sig, WasmValtype.void
            );

            const callTarget = getWasmTableEntry(info.target);
            mono_assert(typeof (callTarget) === "function", () => `expected call target to be function but was ${callTarget}`);
            trampImports.push([ctn, ctn, callTarget]);
        }

        builder.generateTypeSection();

        const compress = true;
        // Emit function imports
        for (let i = 0; i < trampImports.length; i++) {
            const wasmName = compress ? i.toString(16) : undefined;
            builder.defineImportedFunction("i", trampImports[i][0], trampImports[i][1], wasmName);
        }
        builder.generateImportSection();

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
            builder.beginFunction("trampoline", {});

            const ok = generate_wasm_body(builder, info);
            // FIXME
            if (!ok)
                throw new Error(`Failed to generate ${info.name}`);
            builder.appendU8(WasmOpcode.end);
        }

        builder.endSection();

        compileStarted = _now();
        const buffer = builder.getArrayView();
        if (trace > 0)
            console.log(`do_jit_call queue flush generated ${buffer.length} byte(s) of wasm`);
        const traceModule = new WebAssembly.Module(buffer);

        const imports : any = {
            h: (<any>Module).asm.memory
        };
        // Place our function imports into the import dictionary
        for (let i = 0; i < trampImports.length; i++) {
            const wasmName = compress ? i.toString(16) : trampImports[i][0];
            imports[wasmName] = trampImports[i][2];
        }

        const traceInstance = new WebAssembly.Instance(traceModule, {
            i: imports
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

function append_ldloc (builder: WasmBuilder, offset: number, opcode: WasmOpcode) {
    builder.local("sp");
    builder.appendU8(opcode);
    builder.appendMemarg(offset, 2);
}

const JIT_ARG_BYVAL = 0;

function generate_wasm_body (
    builder: WasmBuilder, info: TrampolineInfo
) : boolean {
    let stack_index = 0;

    if (builder.options.enableWasmEh)
        builder.block(WasmValtype.void, WasmOpcode.try_);

    if (info.hasThisReference) {
        append_ldloc(builder, 0, WasmOpcode.i32_load);
        stack_index++;
    }

    /* return address */
    if (info.hasReturnValue)
        builder.local("ret_sp");

    for (let i = 0; i < info.paramCount; i++) {
        // FIXME: STACK_ADD_BYTES does alignment, but we probably don't need to?
        const svalOffset = info.argOffsets[stack_index + i];
        const argInfoOffset = getU32(<any>info.cinfo + offsetOfArgInfo) + i;
        const argInfo = getU8(argInfoOffset);
        if (argInfo == JIT_ARG_BYVAL) {
            // pass the first four bytes of the stackval data union,
            //  which is 'p' where pointers live
            builder.local("sp");
            builder.appendU8(WasmOpcode.i32_load);
            builder.appendMemarg(svalOffset, 2);
        } else {
            // pass the address of the stackval data union
            builder.local("sp");
            builder.i32_const(svalOffset);
            builder.appendU8(WasmOpcode.i32_add);
        }
    }

    builder.local("extra_arg");
    builder.callImport(`fn${info.target.toString(16)}`);

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
