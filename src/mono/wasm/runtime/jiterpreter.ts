// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert, MonoMethod } from "./types";
import { NativePointer } from "./types/emscripten";
import { Module } from "./imports";
import {
    getU16
} from "./memory";
import { WasmOpcode } from "./jiterpreter-opcodes";
import { MintOpcode, OpcodeInfo } from "./mintops";
import cwraps from "./cwraps";
import {
    MintOpcodePtr, WasmValtype, WasmBuilder, addWasmFunctionPointer,
    _now, elapsedTimes, shortNameBase,
    counters, getRawCwrap, importDef,
    JiterpreterOptions, getOptions, recordFailure
} from "./jiterpreter-support";
import {
    generate_wasm_body
} from "./jiterpreter-trace-generator";

// Controls miscellaneous diagnostic output.
export const trace = 0;

export const
    // Record a trace of all managed interpreter opcodes then dump it to console
    //  if an error occurs while compiling the output wasm
    traceOnError = false,
    // Record trace but dump it when the trace has a runtime error instead
    //  requires trapTraceErrors to work and will slow trace compilation +
    //  increase memory usage
    traceOnRuntimeError = false,
    // Trace the method name, location and reason for each abort
    traceAbortLocations = false,
    // Count the number of times a given method is seen as a call target, then
    //  dump a list of the most common ones when dumping stats
    countCallTargets = false,
    // Trace when encountering branches
    traceBranchDisplacements = false,
    // Trace when we reject something for being too small
    traceTooSmall = false,
    // Wraps traces in a JS function that will trap errors and log the trace responsible.
    // Very expensive!!!!
    trapTraceErrors = false,
    // Emit a wasm nop between each managed interpreter opcode
    emitPadding = false,
    // Generate compressed names for imports so that modules have more space for code
    compressImportNames = true,
    // Always grab method full names
    useFullNames = false,
    // Use the mono_debug_count() API (set the COUNT=n env var) to limit the number of traces to compile
    useDebugCount = false;

export const callTargetCounts : { [method: number] : number } = {};

export let mostRecentTrace : InstrumentedTraceState | undefined;
export let mostRecentOptions : JiterpreterOptions | undefined = undefined;

export const disabledOpcodes : Array<MintOpcode> = [
];

export const instrumentedMethodNames : Array<string> = [
    // "System.Collections.Generic.Stack`1<System.Reflection.Emit.LocalBuilder>& System.Collections.Generic.Dictionary`2<System.Type, System.Collections.Generic.Stack`1<System.Reflection.Emit.LocalBuilder>>:FindValue (System.Type)"
];

export class InstrumentedTraceState {
    name: string;
    eip: MintOpcodePtr;
    operand1: number | undefined;
    operand2: number | undefined;

    constructor (name: string) {
        this.name = name;
        this.eip = <any>0;
    }
}

export class TraceInfo {
    ip: MintOpcodePtr;
    name: string | undefined;
    abortReason: string | undefined;
    fnPtr: Number | undefined;

    constructor (ip: MintOpcodePtr) {
        this.ip = ip;
    }
}

export const instrumentedTraces : { [key: number]: InstrumentedTraceState } = {};
export let nextInstrumentedTraceId = 1;
export let countLimitedPrintCounter = 10;
export const abortCounts : { [key: string] : number } = {};
export const traceInfo : { [key: string] : TraceInfo } = {};

export const // offsetOfStack = 12,
    offsetOfImethod = 4,
    offsetOfDataItems = 20,
    sizeOfDataItem = 4,
    // HACK: Typically we generate ~12 bytes of extra gunk after the function body so we are
    //  subtracting 20 from the maximum size to make sure we don't produce too much
    // Also subtract some more size since the wasm we generate for one opcode could be big
    // WASM implementations only allow compiling 4KB of code at once :-)
    maxModuleSize = 4000 - 160,
    // While stats are enabled, dump concise stats every N traces so that it's clear a long-running
    //  task isn't frozen if it's jitting lots of traces
    autoDumpInterval = 500;

/*
struct MonoVTable {
	MonoClass  *klass; // 0
	MonoGCDescriptor gc_descr; // 4
	MonoDomain *domain; // 8
	gpointer    type; // 12
	guint8     *interface_bitmap; // 16
	guint32     max_interface_id; // 20
	guint8      rank; // 21
	guint8      initialized; // 22
	guint8      flags;
*/

/*
struct InterpFrame {
	InterpFrame    *parent; // 0
	InterpMethod   *imethod; // 4
	stackval       *retval; // 8
	stackval       *stack; // 12
	InterpFrame    *next_free; // 16
	InterpState state; // 20
};

struct InterpMethod {
       MonoMethod *method;
       InterpMethod *next_jit_code_hash;

       // Sort pointers ahead of integers to minimize padding for alignment.

       unsigned short *code;
       MonoPIFunc func;
       MonoExceptionClause *clauses; // num_clauses
       void **data_items;
*/

export const enum BailoutReason {
    Unknown,
    InterpreterTiering,
    NullCheck,
    VtableNotInitialized,
    Branch,
    BackwardBranch,
    ConditionalBranch,
    ConditionalBackwardBranch,
    ComplexBranch,
    ArrayLoadFailed,
    StringOperationFailed,
    DivideByZero,
    Overflow,
    Return,
    Call,
    Throw,
    AllocFailed,
    SpanOperationFailed,
    CastFailed,
    SafepointBranchTaken,
    UnboxFailed,
    CallDelegate,
    Debugging
}

export const BailoutReasonNames = [
    "Unknown",
    "InterpreterTiering",
    "NullCheck",
    "VtableNotInitialized",
    "Branch",
    "BackwardBranch",
    "ConditionalBranch",
    "ConditionalBackwardBranch",
    "ComplexBranch",
    "ArrayLoadFailed",
    "StringOperationFailed",
    "DivideByZero",
    "Overflow",
    "Return",
    "Call",
    "Throw",
    "AllocFailed",
    "SpanOperationFailed",
    "CastFailed",
    "SafepointBranchTaken",
    "UnboxFailed",
    "CallDelegate",
    "Debugging"
];

export let traceBuilder : WasmBuilder;
export let traceImports : Array<[string, string, Function]> | undefined;

export let _wrap_trace_function: Function;

export const mathOps1 = [
    "acos",
    "cos",
    "sin",
    "asin",
    "tan",
    "atan"
];

function getTraceImports () {
    if (traceImports)
        return traceImports;

    traceImports = [
        importDef("bailout", getRawCwrap("mono_jiterp_trace_bailout")),
        importDef("copy_pointer", getRawCwrap("mono_wasm_copy_managed_pointer")),
        importDef("array_length", getRawCwrap("mono_wasm_array_length_ref")),
        importDef("array_address", getRawCwrap("mono_jiterp_array_get_element_address_with_size_ref")),
        importDef("entry", getRawCwrap("mono_jiterp_increase_entry_count")),
        importDef("value_copy", getRawCwrap("mono_jiterp_value_copy")),
        importDef("strlen", getRawCwrap("mono_jiterp_strlen_ref")),
        importDef("getchr", getRawCwrap("mono_jiterp_getchr_ref")),
        importDef("getspan", getRawCwrap("mono_jiterp_getitem_span")),
        importDef("gettype", getRawCwrap("mono_jiterp_gettype_ref")),
        importDef("cast", getRawCwrap("mono_jiterp_cast_ref")),
        importDef("try_unbox", getRawCwrap("mono_jiterp_try_unbox_ref")),
        importDef("box", getRawCwrap("mono_jiterp_box_ref")),
        importDef("localloc", getRawCwrap("mono_jiterp_localloc")),
        ["ckovr_i4", "overflow_check_i4", getRawCwrap("mono_jiterp_overflow_check_i4")],
        ["ckovr_u4", "overflow_check_i4", getRawCwrap("mono_jiterp_overflow_check_u4")],
        ["rem", "mathop_dd_d", getRawCwrap("mono_jiterp_fmod")],
        ["atan2", "mathop_dd_d", getRawCwrap("mono_jiterp_atan2")],
        ["newobj_i", "newobj_i", getRawCwrap("mono_jiterp_try_newobj_inlined")],
        ["ld_del_ptr", "ld_del_ptr", getRawCwrap("mono_jiterp_ld_delegate_method_ptr")],
        ["ldtsflda", "ldtsflda", getRawCwrap("mono_jiterp_ldtsflda")],
        ["conv_ovf", "conv_ovf", getRawCwrap("mono_jiterp_conv_ovf")],
        ["relop_fp", "relop_fp", getRawCwrap("mono_jiterp_relop_fp")],
        ["safepoint", "safepoint", getRawCwrap("mono_jiterp_auto_safepoint")],
        ["hashcode", "hashcode", getRawCwrap("mono_jiterp_get_hashcode")],
        ["hascsize", "hascsize", getRawCwrap("mono_jiterp_object_has_component_size")],
        ["hasflag", "hasflag", getRawCwrap("mono_jiterp_enum_hasflag")],
        ["array_rank", "array_rank", getRawCwrap("mono_jiterp_get_array_rank")],
    ];

    if (instrumentedMethodNames.length > 0) {
        traceImports.push(["trace_eip", "trace_eip", trace_current_ip]);
        traceImports.push(["trace_args", "trace_eip", trace_operands]);
    }

    for (let i = 0; i < mathOps1.length; i++) {
        const mop = mathOps1[i];
        traceImports.push([mop, "mathop_d_d", (<any>Math)[mop]]);
    }

    return traceImports;
}

function wrap_trace_function (
    f: Function, name: string, traceBuf: any,
    base: MintOpcodePtr, instrumentedTraceId: number
) {
    const tup = instrumentedTraces[instrumentedTraceId];
    if (instrumentedTraceId)
        console.log(`instrumented ${tup.name}`);

    if (!_wrap_trace_function) {
        // If we used a regular closure, the js console would print the entirety of
        //  dotnet.js when printing an error stack trace, which is... not helpful
        const js = `return function trace_enter (locals) {
            let threw = true;
            try {
                let result = trace(locals);
                threw = false;
                return result;
            } finally {
                if (threw) {
                    let msg = "Unhandled error in trace '" + name + "'";
                    if (tup) {
                        msg += " at offset " + (tup.eip + base).toString(16);
                        msg += " with most recent operands " + tup.operand1.toString(16) + ", " + tup.operand2.toString(16);
                    }
                    console.error(msg);
                    if (traceBuf) {
                        for (let i = 0, l = traceBuf.length; i < l; i++)
                            console.log(traceBuf[i]);
                    }
                }
            }
        };`;
        _wrap_trace_function = new Function("trace", "name", "traceBuf", "tup", "base", js);
    }
    return _wrap_trace_function(
        f, name, traceBuf, instrumentedTraces[instrumentedTraceId], base
    );
}

function initialize_builder (builder: WasmBuilder) {
    // Function type for compiled traces
    builder.defineType(
        "trace", {
            "frame": WasmValtype.i32,
            "pLocals": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "bailout", {
            "ip": WasmValtype.i32,
            "reason": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "copy_pointer", {
            "dest": WasmValtype.i32,
            "src": WasmValtype.i32
        }, WasmValtype.void, true
    );
    builder.defineType(
        "value_copy", {
            "dest": WasmValtype.i32,
            "src": WasmValtype.i32,
            "klass": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "array_length", {
            "ppArray": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "array_address", {
            "ppArray": WasmValtype.i32,
            "elementSize": WasmValtype.i32,
            "index": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "entry", {
            "imethod": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "strlen", {
            "ppString": WasmValtype.i32,
            "pResult": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "getchr", {
            "ppString": WasmValtype.i32,
            "pIndex": WasmValtype.i32,
            "pResult": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "getspan", {
            "destination": WasmValtype.i32,
            "span": WasmValtype.i32,
            "index": WasmValtype.i32,
            "element_size": WasmValtype.i32
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "overflow_check_i4", {
            "lhs": WasmValtype.i32,
            "rhs": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "mathop_d_d", {
            "value": WasmValtype.f64,
        }, WasmValtype.f64, true
    );
    builder.defineType(
        "mathop_dd_d", {
            "lhs": WasmValtype.f64,
            "rhs": WasmValtype.f64,
        }, WasmValtype.f64, true
    );
    builder.defineType(
        "trace_eip", {
            "traceId": WasmValtype.i32,
            "eip": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "newobj_i", {
            "ppDestination": WasmValtype.i32,
            "vtable": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "localloc", {
            "destination": WasmValtype.i32,
            "len": WasmValtype.i32,
            "frame": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "ld_del_ptr", {
            "ppDestination": WasmValtype.i32,
            "ppSource": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "ldtsflda", {
            "ppDestination": WasmValtype.i32,
            "offset": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "gettype", {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "cast", {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "klass": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "try_unbox", {
            "klass": WasmValtype.i32,
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "box", {
            "vtable": WasmValtype.i32,
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "vt": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "conv_ovf", {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "relop_fp", {
            "lhs": WasmValtype.f64,
            "rhs": WasmValtype.f64,
            "opcode": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "safepoint", {
            "frame": WasmValtype.i32,
            "ip": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "hashcode", {
            "ppObj": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "hascsize", {
            "ppObj": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
    builder.defineType(
        "hasflag", {
            "klass": WasmValtype.i32,
            "dest": WasmValtype.i32,
            "sp1": WasmValtype.i32,
            "sp2": WasmValtype.i32,
        }, WasmValtype.void, true
    );
    builder.defineType(
        "array_rank", {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
        }, WasmValtype.i32, true
    );
}

// returns function id
function generate_wasm (
    frame: NativePointer, methodName: string, ip: MintOpcodePtr,
    startOfBody: MintOpcodePtr, sizeOfBody: MintOpcodePtr,
    methodFullName: string | undefined
) : number {
    // Pre-allocate a decent number of constant slots - this adds fixed size bloat
    //  to the trace but will make the actual pointer constants in the trace smaller
    // If we run out of constant slots it will transparently fall back to i32_const
    // For System.Runtime.Tests we only run out of slots ~50 times in 9100 test cases
    const constantSlotCount = 8;

    let builder = traceBuilder;
    if (!builder) {
        traceBuilder = builder = new WasmBuilder(constantSlotCount);
        initialize_builder(builder);
    } else
        builder.clear(constantSlotCount);

    mostRecentOptions = builder.options;

    // skip jiterpreter_enter
    // const _ip = ip;
    const traceOffset = <any>ip - <any>startOfBody;
    const endOfBody = <any>startOfBody + <any>sizeOfBody;
    const traceName = `${methodName}:${(traceOffset).toString(16)}`;

    if (useDebugCount) {
        if (cwraps.mono_jiterp_debug_count() === 0) {
            if (countLimitedPrintCounter-- >= 0)
                console.log(`COUNT limited: ${methodFullName || methodName} @${(traceOffset).toString(16)}`);
            return 0;
        }
    }

    const started = _now();
    let compileStarted = 0;
    let rejected = true, threw = false;

    const instrument = methodFullName && (instrumentedMethodNames.indexOf(methodFullName) >= 0);
    const instrumentedTraceId = instrument ? nextInstrumentedTraceId++ : 0;
    if (instrument) {
        console.log(`instrumenting: ${methodFullName}`);
        instrumentedTraces[instrumentedTraceId] = new InstrumentedTraceState(methodFullName);
    }
    const compress = compressImportNames && !instrument;

    try {
        // Magic number and version
        builder.appendU32(0x6d736100);
        builder.appendU32(1);

        builder.generateTypeSection();

        // Import section
        const traceImports = getTraceImports();

        // Emit function imports
        for (let i = 0; i < traceImports.length; i++) {
            mono_assert(traceImports[i], () => `trace #${i} missing`);
            const wasmName = compress ? i.toString(shortNameBase) : undefined;
            builder.defineImportedFunction("i", traceImports[i][0], traceImports[i][1], wasmName);
        }

        builder.generateImportSection();

        // Function section
        builder.beginSection(3);
        builder.appendULeb(1);
        // Function type for our compiled trace
        mono_assert(builder.functionTypes["trace"], "func type missing");
        builder.appendULeb(builder.functionTypes["trace"][0]);

        // Export section
        builder.beginSection(7);
        builder.appendULeb(1);
        builder.appendName(traceName);
        builder.appendU8(0);
        // Imports get added to the function index space, so we need to add
        //  the count of imported functions to get the index of our compiled trace
        builder.appendULeb(builder.importedFunctionCount + 0);

        // Code section
        builder.beginSection(10);
        builder.appendULeb(1);
        builder.beginFunction("trace", {
            "eip": WasmValtype.i32,
            "temp_ptr": WasmValtype.i32,
            "cknull_ptr": WasmValtype.i32,
            "math_lhs32": WasmValtype.i32,
            "math_rhs32": WasmValtype.i32,
            "math_lhs64": WasmValtype.i64,
            "math_rhs64": WasmValtype.i64
            // "tempi64": WasmValtype.i64
        });

        if (emitPadding) {
            builder.appendU8(WasmOpcode.nop);
            builder.appendU8(WasmOpcode.nop);
        }

        builder.base = ip;
        if (getU16(ip) !== MintOpcode.MINT_TIER_PREPARE_JITERPRETER)
            throw new Error(`Expected *ip to be MINT_TIER_PREPARE_JITERPRETER but was ${getU16(ip)}`);

        // TODO: Call generate_wasm_body before generating any of the sections and headers.
        // This will allow us to do things like dynamically vary the number of locals, in addition
        //  to using global constants and figuring out how many constant slots we need in advance
        //  since a long trace might need many slots and that bloats the header.
        const opcodes_processed = generate_wasm_body(
            frame, traceName, ip, endOfBody, builder,
            instrumentedTraceId
        );
        const keep = (opcodes_processed >= mostRecentOptions.minimumTraceLength);

        if (!keep) {
            const ti = traceInfo[<any>ip];
            if (ti && (ti.abortReason === "end-of-body"))
                ti.abortReason = "trace-too-small";

            if (traceTooSmall && (opcodes_processed > 1))
                console.log(`${traceName} too small: ${opcodes_processed} opcodes, ${builder.current.size} wasm bytes`);
            return 0;
        }

        builder.appendU8(WasmOpcode.end);
        builder.endSection();

        compileStarted = _now();
        const buffer = builder.getArrayView();
        if (trace > 0)
            console.log(`${(<any>(builder.base)).toString(16)} ${methodFullName || traceName} generated ${buffer.length} byte(s) of wasm`);
        counters.bytesGenerated += buffer.length;
        const traceModule = new WebAssembly.Module(buffer);

        const imports : any = {
        };
        // Place our function imports into the import dictionary
        for (let i = 0; i < traceImports.length; i++) {
            const ifn = traceImports[i][2];
            const iname = traceImports[i][0];
            if (!ifn || (typeof (ifn) !== "function"))
                throw new Error(`Import '${iname}' not found or not a function`);
            const wasmName = compress ? i.toString(shortNameBase) : iname;
            imports[wasmName] = ifn;
        }

        const traceInstance = new WebAssembly.Instance(traceModule, {
            i: imports,
            c: <any>builder.getConstants(),
            m: { h: (<any>Module).asm.memory },
        });

        // Get the exported trace function
        const fn = traceInstance.exports[traceName];

        // FIXME: Before threading can be supported, we will need to ensure that
        //  once we assign a function pointer index to a given trace, the trace is
        //  broadcast to all the JS workers and compiled + installed at the appropriate
        //  index in every worker's function pointer table. This also means that we
        //  would need to fill empty slots with a dummy function when growing the table
        //  so that any erroneous ENTERs will skip the opcode instead of crashing due
        //  to calling a null function pointer.
        // Table grow operations will need to be synchronized between workers somehow,
        //  probably by storing the table size in a volatile global or something so that
        //  we know the range of indexes available to us and can ensure that threads
        //  independently jitting traces will not stomp on each other and all threads
        //  have a globally consistent view of which function pointer maps to each trace.
        rejected = false;
        const idx =
            trapTraceErrors
                ? Module.addFunction(
                    wrap_trace_function(
                        <any>fn, methodFullName || methodName, traceOnRuntimeError ? builder.traceBuf : undefined,
                        builder.base, instrumentedTraceId
                    ), "iii"
                )
                : addWasmFunctionPointer(<any>fn);
        if (!idx)
            throw new Error("add_function_pointer returned a 0 index");
        else if (trace >= 2)
            console.log(`${traceName} -> fn index ${idx}`);

        // Ensure that a bit of ongoing diagnostic output is printed for very long-running test
        //  suites or benchmarks if you've enabled stats
        if (builder.options.enableStats && counters.tracesCompiled && (counters.tracesCompiled % autoDumpInterval) === 0)
            jiterpreter_dump_stats(false, true);

        return idx;
    } catch (exc: any) {
        threw = true;
        rejected = false;
        console.error(`MONO_WASM: ${traceName} code generation failed: ${exc} ${exc.stack}`);
        recordFailure();
        return 0;
    } finally {
        const finished = _now();
        if (compileStarted) {
            elapsedTimes.generation += compileStarted - started;
            elapsedTimes.compilation += finished - compileStarted;
        } else {
            elapsedTimes.generation += finished - started;
        }

        if (threw || (!rejected && ((trace >= 2) || mostRecentOptions!.dumpTraces)) || instrument) {
            if (threw || (trace >= 3) || mostRecentOptions!.dumpTraces || instrument) {
                for (let i = 0; i < builder.traceBuf.length; i++)
                    console.log(builder.traceBuf[i]);
            }

            console.log(`// MONO_WASM: ${traceName} generated, blob follows //`);
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
        }
    }
}

export function trace_current_ip (traceId: number, eip: MintOpcodePtr) {
    const tup = instrumentedTraces[traceId];
    if (!tup)
        throw new Error(`Unrecognized instrumented trace id ${traceId}`);
    tup.eip = eip;
    mostRecentTrace = tup;
}

export function trace_operands (a: number, b: number) {
    if (!mostRecentTrace)
        throw new Error("No trace active");
    mostRecentTrace.operand1 = a >>> 0;
    mostRecentTrace.operand2 = b >>> 0;
}

export function record_abort (traceIp: MintOpcodePtr, ip: MintOpcodePtr, traceName: string, reason: string | MintOpcode) {
    if (typeof (reason) === "number") {
        cwraps.mono_jiterp_adjust_abort_count(reason, 1);
        reason = OpcodeInfo[<any>reason][0];
    } else {
        let abortCount = abortCounts[reason];
        if (typeof (abortCount) !== "number")
            abortCount = 1;
        else
            abortCount++;

        abortCounts[reason] = abortCount;
    }

    if ((traceAbortLocations && (reason !== "end-of-body")) || (trace >= 2))
        console.log(`abort ${traceIp} ${traceName}@${ip} ${reason}`);

    traceInfo[<any>traceIp].abortReason = reason;
}

const JITERPRETER_TRAINING = 0;
const JITERPRETER_NOT_JITTED = 1;

export function mono_interp_tier_prepare_jiterpreter (
    frame: NativePointer, method: MonoMethod, ip: MintOpcodePtr,
    startOfBody: MintOpcodePtr, sizeOfBody: MintOpcodePtr
) : number {
    mono_assert(ip, "expected instruction pointer");
    if (!mostRecentOptions)
        mostRecentOptions = getOptions();

    // FIXME: We shouldn't need this check
    if (!mostRecentOptions.enableTraces)
        return JITERPRETER_NOT_JITTED;
    else if (mostRecentOptions.wasmBytesLimit <= counters.bytesGenerated)
        return JITERPRETER_NOT_JITTED;

    let info = traceInfo[<any>ip];

    if (!info)
        traceInfo[<any>ip] = info = new TraceInfo(ip);

    counters.traceCandidates++;
    let methodFullName: string | undefined;
    if (trapTraceErrors || mostRecentOptions.estimateHeat || (instrumentedMethodNames.length > 0) || useFullNames) {
        const pMethodName = cwraps.mono_wasm_method_get_full_name(method);
        methodFullName = Module.UTF8ToString(pMethodName);
        Module._free(<any>pMethodName);
    }
    const methodName = Module.UTF8ToString(cwraps.mono_wasm_method_get_name(method));
    info.name = methodFullName || methodName;
    const fnPtr = generate_wasm(
        frame, methodName, ip, startOfBody, sizeOfBody, methodFullName
    );

    if (fnPtr) {
        counters.tracesCompiled++;
        // FIXME: These could theoretically be 0 or 1, in which case the trace
        //  will never get invoked. Oh well
        info.fnPtr = fnPtr;
        return fnPtr;
    } else {
        return mostRecentOptions.estimateHeat ? JITERPRETER_TRAINING : JITERPRETER_NOT_JITTED;
    }
}

export function jiterpreter_dump_stats (b?: boolean, concise?: boolean) {
    if (!mostRecentOptions || (b !== undefined))
        mostRecentOptions = getOptions();

    if (!mostRecentOptions.enableStats && (b !== undefined))
        return;

    console.log(`// generated: ${counters.bytesGenerated} wasm bytes; ${counters.tracesCompiled} traces (${counters.traceCandidates} candidates, ${(counters.tracesCompiled / counters.traceCandidates * 100).toFixed(1)}%); ${counters.jitCallsCompiled} jit_calls (${(counters.directJitCallsCompiled / counters.jitCallsCompiled * 100).toFixed(1)}% direct); ${counters.entryWrappersCompiled} interp_entries`);
    console.log(`// time spent: ${elapsedTimes.generation | 0}ms generating, ${elapsedTimes.compilation | 0}ms compiling wasm`);
    if (concise)
        return;

    if (mostRecentOptions.countBailouts) {
        for (let i = 0; i < BailoutReasonNames.length; i++) {
            const bailoutCount = cwraps.mono_jiterp_get_trace_bailout_count(i);
            if (bailoutCount)
                console.log(`// traces bailed out ${bailoutCount} time(s) due to ${BailoutReasonNames[i]}`);
        }
    }

    if (mostRecentOptions.estimateHeat) {
        // FIXME: estimateHeat
        /*
        const counts : { [key: string] : number } = {};
        const traces = Object.values(traceInfo);

        for (let i = 0; i < traces.length; i++) {
            const info = traces[i];
            if (!info.abortReason)
                continue;
            else if (info.abortReason === "end-of-body")
                continue;

            if (counts[info.abortReason])
                counts[info.abortReason] += info.hitCount;
            else
                counts[info.abortReason] = info.hitCount;
        }

        if (countCallTargets) {
            console.log("// hottest call targets:");
            const targetPointers = Object.keys(callTargetCounts);
            targetPointers.sort((l, r) => callTargetCounts[Number(r)] - callTargetCounts[Number(l)]);
            for (let i = 0, c = Math.min(20, targetPointers.length); i < c; i++) {
                const targetMethod = Number(targetPointers[i]) | 0;
                const pMethodName = cwraps.mono_wasm_method_get_full_name(<any>targetMethod);
                const targetMethodName = Module.UTF8ToString(pMethodName);
                const hitCount = callTargetCounts[<any>targetMethod];
                Module._free(<any>pMethodName);
                console.log(`${targetMethodName} ${hitCount}`);
            }
        }

        traces.sort((l, r) => r.hitCount - l.hitCount);
        console.log("// hottest failed traces:");
        for (let i = 0, c = 0; i < traces.length && c < 20; i++) {
            // this means the trace has a low hit count and we don't know its identity. no value in
            //  logging it.
            if (!traces[i].name)
                continue;
            // Filter out noisy methods that we don't care about optimizing
            if (traces[i].name!.indexOf("Xunit.") >= 0)
                continue;

            // FIXME: A single hot method can contain many failed traces. This creates a lot of noise
            //  here and also likely indicates the jiterpreter would add a lot of overhead to it
            // Filter out aborts that aren't meaningful since it is unlikely to ever make sense
            //  to fix them, either because they are rarely used or because putting them in
            //  traces would not meaningfully improve performance
            if (traces[i].abortReason) {
                if (traces[i].abortReason!.startsWith("mono_icall_") ||
                    traces[i].abortReason!.startsWith("ret."))
                    continue;

                switch (traces[i].abortReason) {
                    // not feasible to fix
                    case "trace-too-small":
                    case "call":
                    case "callvirt.fast":
                    case "calli.nat.fast":
                    case "calli.nat":
                    case "call.delegate":
                    case "newobj":
                    case "newobj_vt":
                    case "newobj_slow":
                    case "switch":
                    case "call_handler.s":
                    case "rethrow":
                    case "endfinally":
                    case "end-of-body":
                    case "ret":
                        continue;

                    // not worth implementing / too difficult
                    case "intrins_ordinal_ignore_case_ascii":
                    case "intrins_marvin_block":
                    case "intrins_ascii_chars_to_uppercase":
                    case "newarr":
                        continue;
                }
            }

            c++;
            console.log(`${traces[i].name} @${traces[i].ip} (${traces[i].hitCount} hits) ${traces[i].abortReason}`);
        }

        const tuples : Array<[string, number]> = [];
        for (const k in counts)
            tuples.push([k, counts[k]]);

        tuples.sort((l, r) => r[1] - l[1]);

        console.log("// heat:");
        for (let i = 0; i < tuples.length; i++)
            console.log(`// ${tuples[i][0]}: ${tuples[i][1]}`);
        */
    } else {
        for (let i = 0; i < MintOpcode.MINT_LASTOP; i++) {
            const opname = OpcodeInfo[<any>i][0];
            const count = cwraps.mono_jiterp_adjust_abort_count(i, 0);
            if (count > 0)
                abortCounts[opname] = count;
            else
                delete abortCounts[opname];
        }

        const keys = Object.keys(abortCounts);
        keys.sort((l, r) => abortCounts[r] - abortCounts[l]);
        for (let i = 0; i < keys.length; i++)
            console.log(`// ${keys[i]}: ${abortCounts[keys[i]]} abort(s)`);
    }

    if ((typeof(globalThis.setTimeout) === "function") && (b !== undefined))
        setTimeout(
            () => jiterpreter_dump_stats(b),
            15000
        );
}
