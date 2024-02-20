// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoMethod } from "./types/internal";
import { NativePointer } from "./types/emscripten";
import { Module, mono_assert, runtimeHelpers } from "./globals";
import { getU16 } from "./memory";
import { WasmValtype, WasmOpcode, getOpcodeName } from "./jiterpreter-opcodes";
import { MintOpcode } from "./mintops";
import cwraps from "./cwraps";
import {
    MintOpcodePtr, WasmBuilder, addWasmFunctionPointer,
    _now, isZeroPageReserved,
    getRawCwrap, importDef, JiterpreterOptions, getOptions, recordFailure,
    getCounter, modifyCounter,
    simdFallbackCounters, getWasmFunctionTable
} from "./jiterpreter-support";
import {
    BailoutReasonNames, BailoutReason,
    JiterpreterTable, JiterpCounter,
} from "./jiterpreter-enums";
import {
    generateWasmBody, generateBackwardBranchTable
} from "./jiterpreter-trace-generator";
import { mono_jiterp_free_method_data_interp_entry } from "./jiterpreter-interp-entry";
import { mono_jiterp_free_method_data_jit_call } from "./jiterpreter-jit-call";
import { mono_log_error, mono_log_info, mono_log_warn } from "./logging";
import { utf8ToString } from "./strings";

// Controls miscellaneous diagnostic output.
export const trace = 0;

export const
    // Record a trace of all managed interpreter opcodes then dump it to console
    //  if an error occurs while compiling the output wasm
    traceOnError = false,
    // Trace the method name, location and reason for each abort
    traceAbortLocations = false,
    // Count the number of times a given method is seen as a call target, then
    //  dump a list of the most common ones when dumping stats
    countCallTargets = false,
    // Trace when encountering branches
    traceBranchDisplacements = false,
    // Trace when we reject something for being too small
    traceTooSmall = false,
    // For instrumented methods, trace their exact IP during execution
    traceEip = false,
    // When eliminating a null check, replace it with a runtime 'not null' assertion
    //  that will print a diagnostic message if the value is actually null or if
    //  the value does not match the value on the native interpreter stack in memory
    nullCheckValidation = false,
    // Cache null-checked pointers in cknull_ptr between instructions. Incredibly fragile
    //  for some reason I have not been able to identify
    nullCheckCaching = true,
    // Print diagnostic information to the console when performing null check optimizations
    traceNullCheckOptimizations = false,
    // Print diagnostic information when generating backward branches
    // 1 = failures only, 2 = full detail
    traceBackBranches = 0,
    // Enable generating conditional backward branches for ENDFINALLY opcodes if we saw some CALL_HANDLER
    //  opcodes previously, up to this many potential return addresses. If a trace contains more potential
    //  return addresses than this we will not emit code for the ENDFINALLY opcode
    maxCallHandlerReturnAddresses = 3,
    // Controls how many individual items (traces, bailouts, etc) are shown in the breakdown
    //  at the end of a run when stats are enabled. The N highest ranking items will be shown.
    summaryStatCount = 30,
    // Emit a wasm nop between each managed interpreter opcode
    emitPadding = false,
    // Generate compressed names for imports so that modules have more space for code
    compressImportNames = true,
    // Always grab method full names
    useFullNames = false,
    // Use the mono_debug_count() API (set the COUNT=n env var) to limit the number of traces to compile
    useDebugCount = false,
    // Web browsers limit synchronous module compiles to 4KB
    maxModuleSize = 4080;

export const callTargetCounts: { [method: number]: number } = {};

export let mostRecentTrace: InstrumentedTraceState | undefined;
export let mostRecentOptions: JiterpreterOptions | undefined = undefined;

// You can disable an opcode for debugging purposes by adding it to this list,
//  instead of aborting the trace it will insert a bailout instead. This means that you will
//  have trace code generated as if the opcode were otherwise enabled
export const disabledOpcodes: Array<MintOpcode> = [
];

// Detailed output and/or instrumentation will happen when a trace is jitted if the method fullname has a match
// Having any items in this list will add some overhead to the jitting of *all* traces
// These names can be substrings and instrumentation will happen if the substring is found in the full name
export const instrumentedMethodNames: Array<string> = [
];

export class InstrumentedTraceState {
    name: string;
    eip: MintOpcodePtr;
    operand1: number | undefined;
    operand2: number | undefined;

    constructor(name: string) {
        this.name = name;
        this.eip = <any>0;
    }
}

export class TraceInfo {
    ip: MintOpcodePtr;
    index: number; // used to look up hit count
    name: string | undefined;
    abortReason: string | undefined;
    fnPtr: number | undefined;
    bailoutCounts: { [code: number]: number } | undefined;
    bailoutCount: number | undefined;
    isVerbose: boolean;

    constructor(ip: MintOpcodePtr, index: number, isVerbose: number) {
        this.ip = ip;
        this.index = index;
        this.isVerbose = !!isVerbose;
    }

    get hitCount() {
        return cwraps.mono_jiterp_get_trace_hit_count(this.index);
    }
}

export const instrumentedTraces: { [key: number]: InstrumentedTraceState } = {};
export let nextInstrumentedTraceId = 1;
export let countLimitedPrintCounter = 10;
export const abortCounts: { [key: string]: number } = {};
export const traceInfo: { [key: string]: TraceInfo } = {};

export const
    sizeOfDataItem = 4,
    sizeOfObjectHeader = 8,
    sizeOfV128 = 16,
    sizeOfStackval = 8,
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

export let traceBuilder: WasmBuilder;
export let traceImports: Array<[string, string, Function]> | undefined;

const mathOps1d =
    [
        "asin",
        "acos",
        "atan",
        "asinh",
        "acosh",
        "atanh",
        "cos",
        "sin",
        "tan",
        "cosh",
        "sinh",
        "tanh",
        "exp",
        "log",
        "log2",
        "log10",
        "cbrt",
    ], mathOps2d = [
        "fmod",
        "atan2",
        "pow",
    ], mathOps1f = [
        "asinf",
        "acosf",
        "atanf",
        "asinhf",
        "acoshf",
        "atanhf",
        "cosf",
        "sinf",
        "tanf",
        "coshf",
        "sinhf",
        "tanhf",
        "expf",
        "logf",
        "log2f",
        "log10f",
        "cbrtf",
    ], mathOps2f = [
        "fmodf",
        "atan2f",
        "powf",
    ];

function recordBailout(ip: number, traceIndex: number, reason: BailoutReason) {
    cwraps.mono_jiterp_trace_bailout(reason);
    // Counting these is not meaningful and messes up the end of run statistics
    if (reason === BailoutReason.Return)
        return ip;

    const info = traceInfo[traceIndex];
    if (!info) {
        mono_log_error(`trace info not found for ${traceIndex}`);
        return;
    }
    let table = info.bailoutCounts;
    if (!table)
        info.bailoutCounts = table = {};
    const counter = table[reason];
    if (!counter)
        table[reason] = 1;
    else
        table[reason] = counter + 1;
    if (!info.bailoutCount)
        info.bailoutCount = 1;
    else
        info.bailoutCount++;
    return ip;
}

function getTraceImports() {
    if (traceImports)
        return traceImports;

    traceImports = [
        importDef("bailout", recordBailout),
        importDef("copy_ptr", getRawCwrap("mono_wasm_copy_managed_pointer")),
        importDef("entry", getRawCwrap("mono_jiterp_increase_entry_count")),
        importDef("value_copy", getRawCwrap("mono_jiterp_value_copy")),
        importDef("gettype", getRawCwrap("mono_jiterp_gettype_ref")),
        importDef("castv2", getRawCwrap("mono_jiterp_cast_v2")),
        importDef("hasparent", getRawCwrap("mono_jiterp_has_parent_fast")),
        importDef("imp_iface", getRawCwrap("mono_jiterp_implements_interface")),
        importDef("imp_iface_s", getRawCwrap("mono_jiterp_implements_special_interface")),
        importDef("box", getRawCwrap("mono_jiterp_box_ref")),
        importDef("localloc", getRawCwrap("mono_jiterp_localloc")),
        ["ckovr_i4", "overflow_check_i4", getRawCwrap("mono_jiterp_overflow_check_i4")],
        ["ckovr_u4", "overflow_check_i4", getRawCwrap("mono_jiterp_overflow_check_u4")],
        importDef("newobj_i", getRawCwrap("mono_jiterp_try_newobj_inlined")),
        importDef("newstr", getRawCwrap("mono_jiterp_try_newstr")),
        importDef("ld_del_ptr", getRawCwrap("mono_jiterp_ld_delegate_method_ptr")),
        importDef("ldtsflda", getRawCwrap("mono_jiterp_ldtsflda")),
        importDef("conv", getRawCwrap("mono_jiterp_conv")),
        importDef("relop_fp", getRawCwrap("mono_jiterp_relop_fp")),
        importDef("safepoint", getRawCwrap("mono_jiterp_do_safepoint")),
        importDef("hashcode", getRawCwrap("mono_jiterp_get_hashcode")),
        importDef("try_hash", getRawCwrap("mono_jiterp_try_get_hashcode")),
        importDef("hascsize", getRawCwrap("mono_jiterp_object_has_component_size")),
        importDef("hasflag", getRawCwrap("mono_jiterp_enum_hasflag")),
        importDef("array_rank", getRawCwrap("mono_jiterp_get_array_rank")),
        ["a_elesize", "array_rank", getRawCwrap("mono_jiterp_get_array_element_size")],
        importDef("stfld_o", getRawCwrap("mono_jiterp_set_object_field")),
        importDef("cmpxchg_i32", getRawCwrap("mono_jiterp_cas_i32")),
        importDef("cmpxchg_i64", getRawCwrap("mono_jiterp_cas_i64")),
        importDef("stelem_ref", getRawCwrap("mono_jiterp_stelem_ref")),
        importDef("fma", getRawCwrap("fma")),
        importDef("fmaf", getRawCwrap("fmaf")),
    ];

    if (instrumentedMethodNames.length > 0) {
        traceImports.push(["trace_eip", "trace_eip", trace_current_ip]);
        traceImports.push(["trace_args", "trace_eip", trace_operands]);
    }

    if (nullCheckValidation)
        traceImports.push(importDef("notnull", assert_not_null));

    const pushMathOps = (list: string[], type: string) => {
        for (let i = 0; i < list.length; i++) {
            const mop = list[i];
            traceImports!.push([mop, type, getRawCwrap(mop)]);
        }
    };

    pushMathOps(mathOps1f, "mathop_f_f");
    pushMathOps(mathOps2f, "mathop_ff_f");
    pushMathOps(mathOps1d, "mathop_d_d");
    pushMathOps(mathOps2d, "mathop_dd_d");

    return traceImports;
}

function initialize_builder(builder: WasmBuilder) {
    // Function type for compiled traces
    builder.defineType(
        "trace",
        {
            "frame": WasmValtype.i32,
            "pLocals": WasmValtype.i32,
            "cinfo": WasmValtype.i32,
            "ip": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "bailout",
        {
            "retval": WasmValtype.i32,
            "base": WasmValtype.i32,
            "reason": WasmValtype.i32
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "copy_ptr",
        {
            "dest": WasmValtype.i32,
            "src": WasmValtype.i32
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "value_copy",
        {
            "dest": WasmValtype.i32,
            "src": WasmValtype.i32,
            "klass": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "entry",
        {
            "imethod": WasmValtype.i32
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "strlen",
        {
            "ppString": WasmValtype.i32,
            "pResult": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "getchr",
        {
            "ppString": WasmValtype.i32,
            "pIndex": WasmValtype.i32,
            "pResult": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "getspan",
        {
            "destination": WasmValtype.i32,
            "span": WasmValtype.i32,
            "index": WasmValtype.i32,
            "element_size": WasmValtype.i32
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "overflow_check_i4",
        {
            "lhs": WasmValtype.i32,
            "rhs": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "mathop_d_d",
        {
            "value": WasmValtype.f64,
        },
        WasmValtype.f64, true
    );
    builder.defineType(
        "mathop_dd_d",
        {
            "lhs": WasmValtype.f64,
            "rhs": WasmValtype.f64,
        },
        WasmValtype.f64, true
    );
    builder.defineType(
        "mathop_f_f",
        {
            "value": WasmValtype.f32,
        },
        WasmValtype.f32, true
    );
    builder.defineType(
        "mathop_ff_f",
        {
            "lhs": WasmValtype.f32,
            "rhs": WasmValtype.f32,
        },
        WasmValtype.f32, true
    );
    builder.defineType(
        "fmaf",
        {
            "x": WasmValtype.f32,
            "y": WasmValtype.f32,
            "z": WasmValtype.f32,
        },
        WasmValtype.f32, true
    );
    builder.defineType(
        "fma",
        {
            "x": WasmValtype.f64,
            "y": WasmValtype.f64,
            "z": WasmValtype.f64,
        },
        WasmValtype.f64, true
    );
    builder.defineType(
        "trace_eip",
        {
            "traceId": WasmValtype.i32,
            "eip": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "newobj_i",
        {
            "ppDestination": WasmValtype.i32,
            "vtable": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "newstr",
        {
            "ppDestination": WasmValtype.i32,
            "length": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "localloc",
        {
            "destination": WasmValtype.i32,
            "len": WasmValtype.i32,
            "frame": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "ld_del_ptr",
        {
            "ppDestination": WasmValtype.i32,
            "ppSource": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "ldtsflda",
        {
            "ppDestination": WasmValtype.i32,
            "offset": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "gettype",
        {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "castv2",
        {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "klass": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "hasparent",
        {
            "klass": WasmValtype.i32,
            "parent": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "imp_iface",
        {
            "vtable": WasmValtype.i32,
            "klass": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "imp_iface_s",
        {
            "obj": WasmValtype.i32,
            "vtable": WasmValtype.i32,
            "klass": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "box",
        {
            "vtable": WasmValtype.i32,
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "vt": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "conv",
        {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
            "opcode": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "relop_fp",
        {
            "lhs": WasmValtype.f64,
            "rhs": WasmValtype.f64,
            "opcode": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "safepoint",
        {
            "frame": WasmValtype.i32,
            "ip": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "hashcode",
        {
            "ppObj": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "try_hash",
        {
            "ppObj": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "hascsize",
        {
            "ppObj": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "hasflag",
        {
            "klass": WasmValtype.i32,
            "dest": WasmValtype.i32,
            "sp1": WasmValtype.i32,
            "sp2": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "array_rank",
        {
            "destination": WasmValtype.i32,
            "source": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "stfld_o",
        {
            "locals": WasmValtype.i32,
            "fieldOffsetBytes": WasmValtype.i32,
            "targetLocalOffsetBytes": WasmValtype.i32,
            "sourceLocalOffsetBytes": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "notnull",
        {
            "ptr": WasmValtype.i32,
            "expected": WasmValtype.i32,
            "traceIp": WasmValtype.i32,
            "ip": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "cmpxchg_i32",
        {
            "dest": WasmValtype.i32,
            "newVal": WasmValtype.i32,
            "expected": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "cmpxchg_i64",
        {
            "dest": WasmValtype.i32,
            "newVal": WasmValtype.i32,
            "expected": WasmValtype.i32,
            "oldVal": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "stelem_ref",
        {
            "o": WasmValtype.i32,
            "aindex": WasmValtype.i32,
            "ref": WasmValtype.i32,
        },
        WasmValtype.i32, true
    );
    builder.defineType(
        "simd_p_p",
        {
            "arg0": WasmValtype.i32,
            "arg1": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "simd_p_pp",
        {
            "arg0": WasmValtype.i32,
            "arg1": WasmValtype.i32,
            "arg2": WasmValtype.i32,
        },
        WasmValtype.void, true
    );
    builder.defineType(
        "simd_p_ppp",
        {
            "arg0": WasmValtype.i32,
            "arg1": WasmValtype.i32,
            "arg2": WasmValtype.i32,
            "arg3": WasmValtype.i32,
        },
        WasmValtype.void, true
    );

    const traceImports = getTraceImports();

    // Pre-define function imports as persistent
    for (let i = 0; i < traceImports.length; i++) {
        mono_assert(traceImports[i], () => `trace #${i} missing`);
        builder.defineImportedFunction("i", traceImports[i][0], traceImports[i][1], true, traceImports[i][2]);
    }
}

function assert_not_null(
    value: number, expectedValue: number, traceIndex: number, ip: MintOpcodePtr
) {
    if (value && (value === expectedValue))
        return;
    const info = traceInfo[traceIndex];
    throw new Error(`expected non-null value ${expectedValue} but found ${value} in trace ${info.name} @ 0x${(<any>ip).toString(16)}`);
}

// returns function id
function generate_wasm(
    frame: NativePointer, methodName: string, ip: MintOpcodePtr,
    startOfBody: MintOpcodePtr, sizeOfBody: MintOpcodePtr,
    traceIndex: number, methodFullName: string | undefined,
    backwardBranchTable: Uint16Array | null, presetFunctionPointer: number
): number {
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
                mono_log_info(`COUNT limited: ${methodFullName || methodName} @${(traceOffset).toString(16)}`);
            return 0;
        }
    }

    const started = _now();
    let compileStarted = 0;
    let rejected = true, threw = false;

    const ti = traceInfo[traceIndex];
    const instrument = ti.isVerbose || (methodFullName && (
        instrumentedMethodNames.findIndex(
            (filter) => methodFullName.indexOf(filter) >= 0
        ) >= 0
    ));
    mono_assert(!instrument || methodFullName, "Expected methodFullName if trace is instrumented");
    const instrumentedTraceId = instrument ? nextInstrumentedTraceId++ : 0;
    if (instrument) {
        mono_log_info(`instrumenting: ${methodFullName}`);
        instrumentedTraces[instrumentedTraceId] = new InstrumentedTraceState(methodFullName!);
    }
    builder.compressImportNames = compressImportNames && !instrument;

    try {
        // Magic number and version
        builder.appendU32(0x6d736100);
        builder.appendU32(1);

        builder.generateTypeSection();

        const traceLocals: any = {
            "disp": WasmValtype.i32,
            "cknull_ptr": WasmValtype.i32,
            "dest_ptr": WasmValtype.i32,
            "src_ptr": WasmValtype.i32,
            "memop_dest": WasmValtype.i32,
            "memop_src": WasmValtype.i32,
            "index": WasmValtype.i32,
            "count": WasmValtype.i32,
            "math_lhs32": WasmValtype.i32,
            "math_rhs32": WasmValtype.i32,
            "math_lhs64": WasmValtype.i64,
            "math_rhs64": WasmValtype.i64,
            "temp_f32": WasmValtype.f32,
            "temp_f64": WasmValtype.f64,
            "backbranched": WasmValtype.i32,
        };
        if (builder.options.enableSimd) {
            traceLocals["v128_zero"] = WasmValtype.v128;
            traceLocals["math_lhs128"] = WasmValtype.v128;
            traceLocals["math_rhs128"] = WasmValtype.v128;
        }

        let keep = true,
            traceValue = 0;
        builder.defineFunction(
            {
                type: "trace",
                name: traceName,
                export: true,
                locals: traceLocals
            }, () => {
                if (emitPadding) {
                    builder.appendU8(WasmOpcode.nop);
                    builder.appendU8(WasmOpcode.nop);
                }

                builder.base = ip;
                builder.traceIndex = traceIndex;
                builder.frame = frame;
                switch (getU16(ip)) {
                    case MintOpcode.MINT_TIER_PREPARE_JITERPRETER:
                    case MintOpcode.MINT_TIER_NOP_JITERPRETER:
                    case MintOpcode.MINT_TIER_MONITOR_JITERPRETER:
                    case MintOpcode.MINT_TIER_ENTER_JITERPRETER:
                        break;
                    default:
                        throw new Error(`Expected *ip to be a jiterpreter opcode but it was ${getU16(ip)}`);
                }

                builder.cfg.initialize(startOfBody, backwardBranchTable, instrument ? 1 : 0);

                // TODO: Call generateWasmBody before generating any of the sections and headers.
                // This will allow us to do things like dynamically vary the number of locals, in addition
                //  to using global constants and figuring out how many constant slots we need in advance
                //  since a long trace might need many slots and that bloats the header.
                traceValue = generateWasmBody(
                    frame, traceName, ip, startOfBody, endOfBody,
                    builder, instrumentedTraceId, backwardBranchTable
                );

                keep = (traceValue >= mostRecentOptions!.minimumTraceValue);

                return builder.cfg.generate();
            }
        );

        builder.emitImportsAndFunctions(false);

        if (!keep) {
            if (ti && (ti.abortReason === "end-of-body"))
                ti.abortReason = "trace-too-small";

            if (traceTooSmall && (traceValue > 1))
                mono_log_info(`${traceName} too small: value=${traceValue}, ${builder.current.size} wasm bytes`);
            return 0;
        }

        compileStarted = _now();
        const buffer = builder.getArrayView();
        // mono_log_info(`bytes generated: ${buffer.length}`);

        if (trace > 0)
            mono_log_info(`${(<any>(builder.base)).toString(16)} ${methodFullName || traceName} generated ${buffer.length} byte(s) of wasm`);
        modifyCounter(JiterpCounter.BytesGenerated, buffer.length);

        if (buffer.length >= maxModuleSize) {
            mono_log_warn(`Jiterpreter generated too much code (${buffer.length} bytes) for trace ${traceName}. Please report this issue.`);
            return 0;
        }

        const traceModule = new WebAssembly.Module(buffer);
        const wasmImports = builder.getWasmImports();
        const traceInstance = new WebAssembly.Instance(traceModule, wasmImports);

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

        let idx: number;
        if (presetFunctionPointer) {
            const fnTable = getWasmFunctionTable();
            fnTable.set(presetFunctionPointer, fn);
            idx = presetFunctionPointer;
        } else {
            idx = addWasmFunctionPointer(JiterpreterTable.Trace, <any>fn);
        }
        if (trace >= 2)
            mono_log_info(`${traceName} -> fn index ${idx}`);

        // Ensure that a bit of ongoing diagnostic output is printed for very long-running test
        //  suites or benchmarks if you've enabled stats
        const tracesCompiled = getCounter(JiterpCounter.TracesCompiled);
        if (builder.options.enableStats && tracesCompiled && (tracesCompiled % autoDumpInterval) === 0)
            jiterpreter_dump_stats(false, true);

        return idx;
    } catch (exc: any) {
        threw = true;
        rejected = false;
        mono_log_error(`${methodFullName || traceName} code generation failed: ${exc} ${exc.stack}`);
        recordFailure();
        return 0;
    } finally {
        const finished = _now();
        if (compileStarted) {
            modifyCounter(JiterpCounter.ElapsedGenerationMs, compileStarted - started);
            modifyCounter(JiterpCounter.ElapsedCompilationMs, finished - compileStarted);
        } else {
            modifyCounter(JiterpCounter.ElapsedGenerationMs, finished - started);
        }

        if (threw || (!rejected && ((trace >= 2) || mostRecentOptions!.dumpTraces)) || instrument) {
            if (threw || (trace >= 3) || mostRecentOptions!.dumpTraces || instrument) {
                for (let i = 0; i < builder.traceBuf.length; i++)
                    mono_log_info(builder.traceBuf[i]);
            }

            mono_log_info(`// ${methodFullName || traceName} generated, blob follows //`);
            let s = "", j = 0;
            try {
                // We may have thrown an uncaught exception while inside a block,
                //  so we need to pop it for getArrayView to work.
                while (builder.activeBlocks > 0)
                    builder.endBlock();

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
        }
    }
}

export function trace_current_ip(traceId: number, eip: MintOpcodePtr) {
    const tup = instrumentedTraces[traceId];
    if (!tup)
        throw new Error(`Unrecognized instrumented trace id ${traceId}`);
    tup.eip = eip;
    mostRecentTrace = tup;
}

export function trace_operands(a: number, b: number) {
    if (!mostRecentTrace)
        throw new Error("No trace active");
    mostRecentTrace.operand1 = a >>> 0;
    mostRecentTrace.operand2 = b >>> 0;
}

export function record_abort(traceIndex: number, ip: MintOpcodePtr, traceName: string, reason: string | MintOpcode) {
    if (typeof (reason) === "number") {
        cwraps.mono_jiterp_adjust_abort_count(reason, 1);
        reason = getOpcodeName(reason);
    } else {
        let abortCount = abortCounts[reason];
        if (typeof (abortCount) !== "number")
            abortCount = 1;
        else
            abortCount++;

        abortCounts[reason] = abortCount;
    }

    if ((traceAbortLocations && (reason !== "end-of-body")) || (trace >= 2))
        mono_log_info(`abort #${traceIndex} ${traceName}@${ip} ${reason}`);

    traceInfo[traceIndex].abortReason = reason;
}

const JITERPRETER_TRAINING = 0;
const JITERPRETER_NOT_JITTED = 1;

export function mono_interp_tier_prepare_jiterpreter(
    frame: NativePointer, method: MonoMethod, ip: MintOpcodePtr, index: number,
    startOfBody: MintOpcodePtr, sizeOfBody: MintOpcodePtr, isVerbose: number,
    presetFunctionPointer: number
): number {
    mono_assert(ip, "expected instruction pointer");
    if (!mostRecentOptions)
        mostRecentOptions = getOptions();

    // FIXME: We shouldn't need this check
    if (!mostRecentOptions.enableTraces)
        return JITERPRETER_NOT_JITTED;
    else if (mostRecentOptions.wasmBytesLimit <= getCounter(JiterpCounter.BytesGenerated))
        return JITERPRETER_NOT_JITTED;

    let info = traceInfo[index];

    if (!info)
        traceInfo[index] = info = new TraceInfo(ip, index, isVerbose);

    modifyCounter(JiterpCounter.TraceCandidates, 1);
    let methodFullName: string | undefined;
    if (
        mostRecentOptions.estimateHeat ||
        (instrumentedMethodNames.length > 0) || useFullNames ||
        info.isVerbose
    ) {
        const pMethodName = cwraps.mono_wasm_method_get_full_name(method);
        methodFullName = utf8ToString(pMethodName);
        Module._free(<any>pMethodName);
    }
    const methodName = utf8ToString(cwraps.mono_wasm_method_get_name(method));
    info.name = methodFullName || methodName;

    let backwardBranchTable = mostRecentOptions.noExitBackwardBranches
        ? generateBackwardBranchTable(ip, startOfBody, sizeOfBody)
        : null;

    // If we're compiling a trace that doesn't start at the beginning of a method,
    //  it's possible all the backward branch targets precede it, so we won't want to
    //  actually wrap it in a loop and have the eip check at the beginning.
    if (backwardBranchTable && (ip !== startOfBody)) {
        const threshold = (<any>ip - <any>startOfBody) / 2;
        let foundReachableBranchTarget = false;
        for (let i = 0; i < backwardBranchTable.length; i++) {
            if (backwardBranchTable[i] > threshold) {
                foundReachableBranchTarget = true;
                break;
            }
        }
        // We didn't find any backward branch targets we can reach from inside this trace,
        //  so null out the table.
        if (!foundReachableBranchTarget)
            backwardBranchTable = null;
    }

    const fnPtr = generate_wasm(
        frame, methodName, ip, startOfBody,
        sizeOfBody, index, methodFullName,
        backwardBranchTable, presetFunctionPointer
    );

    if (fnPtr) {
        modifyCounter(JiterpCounter.TracesCompiled, 1);
        // FIXME: These could theoretically be 0 or 1, in which case the trace
        //  will never get invoked. Oh well
        info.fnPtr = fnPtr;
        return fnPtr;
    } else {
        return mostRecentOptions.estimateHeat ? JITERPRETER_TRAINING : JITERPRETER_NOT_JITTED;
    }
}

// NOTE: This will potentially be called once for every trace entry point
//  in a given method, not just once per method
export function mono_jiterp_free_method_data_js(
    method: MonoMethod, imethod: number, traceIndex: number
) {
    // TODO: Uninstall the trace function pointer from the function pointer table,
    //  so that the compiled trace module can be freed by the browser eventually
    // Release the trace info object, if present
    delete traceInfo[traceIndex];
    // Remove any AOT data and queue entries associated with the method
    mono_jiterp_free_method_data_interp_entry(imethod);
    mono_jiterp_free_method_data_jit_call(method);
}

export function jiterpreter_dump_stats(b?: boolean, concise?: boolean) {
    if (!runtimeHelpers.runtimeReady) {
        return;
    }
    if (!mostRecentOptions || (b !== undefined))
        mostRecentOptions = getOptions();

    if (!mostRecentOptions.enableStats && (b !== undefined))
        return;

    const backBranchesEmitted = getCounter(JiterpCounter.BackBranchesEmitted),
        backBranchesNotEmitted = getCounter(JiterpCounter.BackBranchesNotEmitted),
        nullChecksEliminated = getCounter(JiterpCounter.NullChecksEliminated),
        nullChecksFused = getCounter(JiterpCounter.NullChecksFused),
        jitCallsCompiled = getCounter(JiterpCounter.JitCallsCompiled),
        directJitCallsCompiled = getCounter(JiterpCounter.DirectJitCallsCompiled),
        entryWrappersCompiled = getCounter(JiterpCounter.EntryWrappersCompiled),
        tracesCompiled = getCounter(JiterpCounter.TracesCompiled),
        traceCandidates = getCounter(JiterpCounter.TraceCandidates),
        bytesGenerated = getCounter(JiterpCounter.BytesGenerated),
        elapsedGenerationMs = getCounter(JiterpCounter.ElapsedGenerationMs),
        elapsedCompilationMs = getCounter(JiterpCounter.ElapsedCompilationMs);

    const backBranchHitRate = (backBranchesEmitted / (backBranchesEmitted + backBranchesNotEmitted)) * 100,
        tracesRejected = cwraps.mono_jiterp_get_rejected_trace_count(),
        nullChecksEliminatedText = mostRecentOptions.eliminateNullChecks ? nullChecksEliminated.toString() : "off",
        nullChecksFusedText = (mostRecentOptions.zeroPageOptimization ? nullChecksFused.toString() + (isZeroPageReserved() ? "" : " (disabled)") : "off"),
        backBranchesEmittedText = mostRecentOptions.enableBackwardBranches ? `emitted: ${backBranchesEmitted}, failed: ${backBranchesNotEmitted} (${backBranchHitRate.toFixed(1)}%)` : ": off",
        directJitCallsText = jitCallsCompiled ? (
            mostRecentOptions.directJitCalls ? `direct jit calls: ${directJitCallsCompiled} (${(directJitCallsCompiled / jitCallsCompiled * 100).toFixed(1)}%)` : "direct jit calls: off"
        ) : "";

    mono_log_info(`// jitted ${bytesGenerated} bytes; ${tracesCompiled} traces (${(tracesCompiled / traceCandidates * 100).toFixed(1)}%) (${tracesRejected} rejected); ${jitCallsCompiled} jit_calls; ${entryWrappersCompiled} interp_entries`);
    mono_log_info(`// cknulls eliminated: ${nullChecksEliminatedText}, fused: ${nullChecksFusedText}; back-branches ${backBranchesEmittedText}; ${directJitCallsText}`);
    mono_log_info(`// time: ${elapsedGenerationMs | 0}ms generating, ${elapsedCompilationMs | 0}ms compiling wasm.`);
    if (concise)
        return;

    if (mostRecentOptions.countBailouts) {
        const traces = Object.values(traceInfo);
        traces.sort((lhs, rhs) => (rhs.bailoutCount || 0) - (lhs.bailoutCount || 0));
        for (let i = 0; i < BailoutReasonNames.length; i++) {
            const bailoutCount = cwraps.mono_jiterp_get_trace_bailout_count(i);
            if (bailoutCount)
                mono_log_info(`// traces bailed out ${bailoutCount} time(s) due to ${BailoutReasonNames[i]}`);
        }

        for (let i = 0, c = 0; i < traces.length && c < summaryStatCount; i++) {
            const trace = traces[i];
            if (!trace.bailoutCount)
                continue;
            c++;
            mono_log_info(`${trace.name}: ${trace.bailoutCount} bailout(s)`);
            for (const k in trace.bailoutCounts)
                mono_log_info(`  ${BailoutReasonNames[<any>k]} x${trace.bailoutCounts[<any>k]}`);
        }
    }

    if (mostRecentOptions.estimateHeat) {
        const counts: { [key: string]: number } = {};
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
            mono_log_info("// hottest call targets:");
            const targetPointers = Object.keys(callTargetCounts);
            targetPointers.sort((l, r) => callTargetCounts[Number(r)] - callTargetCounts[Number(l)]);
            for (let i = 0, c = Math.min(summaryStatCount, targetPointers.length); i < c; i++) {
                const targetMethod = Number(targetPointers[i]) | 0;
                const pMethodName = cwraps.mono_wasm_method_get_full_name(<any>targetMethod);
                const targetMethodName = utf8ToString(pMethodName);
                const hitCount = callTargetCounts[<any>targetMethod];
                Module._free(<any>pMethodName);
                mono_log_info(`${targetMethodName} ${hitCount}`);
            }
        }

        traces.sort((l, r) => r.hitCount - l.hitCount);
        mono_log_info("// hottest failed traces:");
        for (let i = 0, c = 0; i < traces.length && c < summaryStatCount; i++) {
            // this means the trace has a low hit count and we don't know its identity. no value in
            //  logging it.
            if (!traces[i].name)
                continue;
            // This means the trace did compile and just aborted later on
            if (traces[i].fnPtr)
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
                    case "trace-too-big":
                    case "call":
                    case "callvirt.fast":
                    case "calli.nat.fast":
                    case "calli.nat":
                    case "call.delegate":
                    case "newobj":
                    case "newobj_vt":
                    case "newobj_slow":
                    case "switch":
                    case "rethrow":
                    case "end-of-body":
                    case "ret":
                        continue;

                    // not worth implementing / too difficult
                    case "intrins_marvin_block":
                    case "intrins_ascii_chars_to_uppercase":
                        continue;
                }
            }

            c++;
            mono_log_info(`${traces[i].name} @${traces[i].ip} (${traces[i].hitCount} hits) ${traces[i].abortReason}`);
        }

        const tuples: Array<[string, number]> = [];
        for (const k in counts)
            tuples.push([k, counts[k]]);

        tuples.sort((l, r) => r[1] - l[1]);

        mono_log_info("// heat:");
        for (let i = 0; i < tuples.length; i++)
            mono_log_info(`// ${tuples[i][0]}: ${tuples[i][1]}`);
    } else {
        for (let i = 0; i < MintOpcode.MINT_LASTOP; i++) {
            const opname = getOpcodeName(i);
            const count = cwraps.mono_jiterp_adjust_abort_count(i, 0);
            if (count > 0)
                abortCounts[opname] = count;
            else
                delete abortCounts[opname];
        }

        const keys = Object.keys(abortCounts);
        keys.sort((l, r) => abortCounts[r] - abortCounts[l]);
        for (let i = 0; i < keys.length; i++)
            mono_log_info(`// ${keys[i]}: ${abortCounts[keys[i]]} abort(s)`);
    }

    for (const k in simdFallbackCounters)
        mono_log_info(`// simd ${k}: ${simdFallbackCounters[k]} fallback insn(s)`);

    if ((typeof (globalThis.setTimeout) === "function") && (b !== undefined))
        setTimeout(
            () => jiterpreter_dump_stats(b),
            15000
        );
}
