// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_assert, MonoMethod } from "./types";
import { NativePointer } from "./types/emscripten";
import {
    getU16, getI16,
    getU32_unaligned, getI32_unaligned, getF32_unaligned, getF64_unaligned,
} from "./memory";
import { WasmOpcode } from "./jiterpreter-opcodes";
import { MintOpcode, OpcodeInfo } from "./mintops";
import cwraps from "./cwraps";
import {
    MintOpcodePtr, WasmValtype, WasmBuilder,
    append_memset_dest, append_memmove_dest_src,
    try_append_memset_fast, try_append_memmove_fast,
    counters, getMemberOffset, JiterpMember
} from "./jiterpreter-support";
import {
    sizeOfDataItem,

    disabledOpcodes, countCallTargets,
    callTargetCounts, trapTraceErrors,
    trace, traceOnError, traceOnRuntimeError,
    emitPadding, traceBranchDisplacements,
    traceEip, nullCheckValidation,
    abortAtJittedLoopBodies, traceNullCheckOptimizations,
    nullCheckCaching, traceBackBranches,

    mostRecentOptions,

    BailoutReason,

    record_abort,
} from "./jiterpreter";

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

const enum JiterpSpecialOpcode {
    CNE_UN_R4 = 0xFFFF + 0,
    CGE_UN_R4 = 0xFFFF + 1,
    CLE_UN_R4 = 0xFFFF + 2,
    CNE_UN_R8 = 0xFFFF + 3,
    CGE_UN_R8 = 0xFFFF + 4,
    CLE_UN_R8 = 0xFFFF + 5,
}

// indexPlusOne so that ip[1] in the interpreter becomes getArgU16(ip, 1)
function getArgU16 (ip: MintOpcodePtr, indexPlusOne: number) {
    return getU16(<any>ip + (2 * indexPlusOne));
}

function getArgI16 (ip: MintOpcodePtr, indexPlusOne: number) {
    return getI16(<any>ip + (2 * indexPlusOne));
}

function getArgI32 (ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getI32_unaligned(src);
}

function getArgU32 (ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getU32_unaligned(src);
}

function getArgF32 (ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getF32_unaligned(src);
}

function getArgF64 (ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getF64_unaligned(src);
}

function get_imethod_data (frame: NativePointer, index: number) {
    // FIXME: Encoding this data directly into the trace will prevent trace reuse
    const iMethod = getU32_unaligned(<any>frame + getMemberOffset(JiterpMember.Imethod));
    const pData = getU32_unaligned(iMethod + getMemberOffset(JiterpMember.DataItems));
    const dataOffset = pData + (index * sizeOfDataItem);
    return getU32_unaligned(dataOffset);
}

function is_backward_branch_target (
    ip: MintOpcodePtr, startOfBody: MintOpcodePtr,
    backwardBranchTable: Uint16Array | null
) {
    if (!backwardBranchTable)
        return false;

    for (let i = 0; i < backwardBranchTable.length; i++) {
        const actualOffset = (backwardBranchTable[i] * 2) + <any>startOfBody;
        if (actualOffset === ip)
            return true;
    }

    return false;
}

export function generate_wasm_body (
    frame: NativePointer, traceName: string, ip: MintOpcodePtr,
    startOfBody: MintOpcodePtr, endOfBody: MintOpcodePtr,
    builder: WasmBuilder, instrumentedTraceId: number,
    backwardBranchTable: Uint16Array | null
) : number {
    const abort = <MintOpcodePtr><any>0;
    let isFirstInstruction = true, inBranchBlock = false,
        firstOpcodeInBlock = true;
    let result = 0;
    const traceIp = ip;

    addressTakenLocals.clear();
    eraseInferredState();

    // Skip over the enter opcode
    ip += <any>(OpcodeInfo[MintOpcode.MINT_TIER_ENTER_JITERPRETER][1] * 2);
    let rip = ip;

    // Initialize eip, so that we will never return a 0 displacement
    // Otherwise we could return 0 in the scenario where none of our blocks executed
    // (This shouldn't happen though!)
    builder.ip_const(ip);
    builder.local("eip", WasmOpcode.set_local);

    // If a method contains backward branches we also need to wrap the whole trace in a loop
    //  that we can jump to the top of in order to begin executing the trace again
    // FIXME: It would be much more efficient to use br_table to dispatch to the appropriate
    //  branch block somehow but the code generation is tough due to WASM's IR
    if (backwardBranchTable) {
        builder.block(WasmValtype.void, WasmOpcode.loop);
    }

    // We wrap all instructions in a 'branch block' that is used
    //  when performing a branch and will be skipped over if the
    //  current instruction pointer does not match. This means
    //  that if ip points to a branch target we don't handle,
    //  the trace will automatically bail out at the end after
    //  skipping past all the branch targets
    builder.block();

    while (ip) {
        if (ip >= endOfBody) {
            record_abort(traceIp, ip, traceName, "end-of-body");
            break;
        }

        // HACK: Browsers set a limit of 4KB, we lower it slightly since a single opcode
        //  might generate a ton of code and we generate a bit of an epilogue after
        //  we finish
        const maxModuleSize = 3850;
        if (builder.size >= maxModuleSize - builder.bytesGeneratedSoFar) {
            // console.log(`trace too big, estimated size is ${builder.size + builder.bytesGeneratedSoFar}`);
            record_abort(traceIp, ip, traceName, "trace-too-big");
            break;
        }

        if (instrumentedTraceId && traceEip) {
            builder.i32_const(instrumentedTraceId);
            builder.ip_const(ip);
            builder.callImport("trace_eip");
        }

        let opcode = getU16(ip);
        const info = OpcodeInfo[opcode];
        mono_assert(info, () => `invalid opcode ${opcode}`);

        const opname = info[0];
        const _ip = ip;
        const isBackBranchTarget = builder.options.noExitBackwardBranches &&
            is_backward_branch_target(ip, startOfBody, backwardBranchTable),
            isForwardBranchTarget = builder.branchTargets.has(ip),
            needsEipCheck = isBackBranchTarget || isForwardBranchTarget ||
                // If a method contains backward branches, we also need to check eip at the first insn
                //  because a backward branch might target a point in the middle of the trace
                (isFirstInstruction && backwardBranchTable),
            needsFallthroughEipUpdate = needsEipCheck && !isFirstInstruction;
        let isLowValueOpcode = false,
            skipDregInvalidation = false;

        // We record the offset of each backward branch we encounter, so that later branch
        //  opcodes know that it's available by branching to the top of the dispatch loop
        if (isBackBranchTarget) {
            if (traceBackBranches)
                console.log(`${traceName} recording back branch target 0x${(<any>ip).toString(16)}`);
            builder.backBranchOffsets.push(ip);
        }

        if (needsEipCheck) {
            // If execution runs past the end of the current branch block, ensure
            //  that the instruction pointer is updated appropriately. This will
            //  also guarantee that the branch target block's comparison will
            //  succeed so that execution continues.
            // We make sure above that this isn't done for the start of the trace,
            //  otherwise loops will run forever and never terminate since after
            //  branching to the top of the loop we would blow away eip
            if (needsFallthroughEipUpdate) {
                builder.ip_const(rip);
                builder.local("eip", WasmOpcode.set_local);
            }
            append_branch_target_block(builder, ip);
            inBranchBlock = true;
            firstOpcodeInBlock = true;
            eraseInferredState();
        }

        isFirstInstruction = false;

        if (disabledOpcodes.indexOf(opcode) >= 0) {
            append_bailout(builder, ip, BailoutReason.Debugging);
            opcode = MintOpcode.MINT_NOP;
            // Intentionally leave the correct info in place so we skip the right number of bytes
        }

        switch (opcode) {
            case MintOpcode.MINT_INITLOCAL:
            case MintOpcode.MINT_INITLOCALS: {
                // FIXME: We should move the first entry point after initlocals if it exists
                const startOffsetInBytes = getArgU16(ip, 1),
                    sizeInBytes = getArgU16(ip, 2);
                append_memset_local(builder, startOffsetInBytes, 0, sizeInBytes);
                break;
            }
            case MintOpcode.MINT_LOCALLOC: {
                // dest
                append_ldloca(builder, getArgU16(ip, 1));
                // len
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                // frame
                builder.local("frame");
                builder.callImport("localloc");
                break;
            }
            case MintOpcode.MINT_INITOBJ: {
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_memset_dest(builder, 0, getArgU16(ip, 2));
                break;
            }
            case MintOpcode.MINT_CPBLK: {
                // size (FIXME: uint32 not int32)
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                builder.local("math_rhs32", WasmOpcode.tee_local);
                // if size is 0 then don't do anything
                builder.block(WasmValtype.void, WasmOpcode.if_); // if #1

                // stash dest then check for null
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                builder.local("temp_ptr", WasmOpcode.tee_local);
                builder.appendU8(WasmOpcode.i32_eqz);
                // stash src then check for null
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.local("math_lhs32", WasmOpcode.tee_local);
                builder.appendU8(WasmOpcode.i32_eqz);

                // now we memmove if both dest and src are valid. The stack currently has
                //  the eqz result for each pointer so we can stash a bailout inside of an if
                builder.appendU8(WasmOpcode.i32_or);
                builder.block(WasmValtype.void, WasmOpcode.if_); // if #2
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock(); // if #2

                // We passed the null check so now prepare the stack
                builder.local("temp_ptr");
                builder.local("math_lhs32");
                builder.local("math_rhs32");
                // wasm memmove with stack layout dest, src, count
                builder.appendU8(WasmOpcode.PREFIX_sat);
                builder.appendU8(10);
                builder.appendU8(0);
                builder.appendU8(0);
                builder.endBlock(); // if #1
                break;
            }
            case MintOpcode.MINT_INITBLK: {
                // FIXME: This will cause an erroneous bailout if dest and size are both 0
                //  but that really shouldn't ever happen, and it will only cause a slowdown
                // dest
                append_ldloc_cknull(builder, getArgU16(ip, 1), ip, true);
                // value
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                // size (FIXME: uint32 not int32)
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
                builder.appendU8(WasmOpcode.PREFIX_sat);
                builder.appendU8(11);
                builder.appendU8(0);
                break;
            }

            // Other conditional branch types are handled by the relop table.
            case MintOpcode.MINT_BRFALSE_I4_S:
            case MintOpcode.MINT_BRTRUE_I4_S:
            case MintOpcode.MINT_BRFALSE_I4_SP:
            case MintOpcode.MINT_BRTRUE_I4_SP:
            case MintOpcode.MINT_BRFALSE_I8_S:
            case MintOpcode.MINT_BRTRUE_I8_S:
            case MintOpcode.MINT_LEAVE_S:
            case MintOpcode.MINT_BR_S:
                if (!emit_branch(builder, ip, opcode))
                    ip = abort;
                break;

            case MintOpcode.MINT_CKNULL: {
                // if (locals[ip[2]]) locals[ip[1]] = locals[ip[2]] else throw
                const src = getArgU16(ip, 2),
                    dest = getArgU16(ip, 1);
                // locals[n] = cknull(locals[n]) is a common pattern, and we don't
                //  need to do the write for it since it can't change the value
                if (src !== dest) {
                    builder.local("pLocals");
                    append_ldloc_cknull(builder, src, ip, true);
                    append_stloc_tail(builder, dest, WasmOpcode.i32_store);
                } else {
                    append_ldloc_cknull(builder, src, ip, false);
                }
                // We will have bailed out if the object was null
                if (builder.allowNullCheckOptimization) {
                    if (traceNullCheckOptimizations)
                        console.log(`(0x${(<any>ip).toString(16)}) locals[${dest}] passed cknull`);
                    notNullSince.set(dest, <any>ip);
                }
                skipDregInvalidation = true;
                break;
            }

            case MintOpcode.MINT_TIER_ENTER_METHOD:
            case MintOpcode.MINT_TIER_PATCHPOINT: {
                // We need to make sure to notify the interpreter about tiering opcodes
                //  so that tiering up will still happen
                const iMethod = getU32_unaligned(<any>frame + getMemberOffset(JiterpMember.Imethod));
                builder.ptr_const(iMethod);
                // increase_entry_count will return 1 if we can continue, otherwise
                //  we need to bail out into the interpreter so it can perform tiering
                builder.callImport("entry");
                builder.block(WasmValtype.void, WasmOpcode.if_);
                append_bailout(builder, ip, BailoutReason.InterpreterTiering);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_TIER_ENTER_JITERPRETER:
                isLowValueOpcode = true;
                // If we hit an enter opcode and we're not currently in a branch block
                //  or the enter opcode is the first opcode in a branch block, this likely
                //  indicates that we've reached a loop body that was already jitted before
                //  we were, and we should stop our trace here.
                // Most loops have a prologue before them and having the loop body inside
                //  the prologue trace is not going to especially boost throughput, while it
                //  will make the prologue trace bigger (and thus slower to compile.)
                // We don't want to abort before our trace is long enough though, since that
                //  will result in decent trace candidates becoming nops which adds overhead
                //  and leaves us in the interp.
                if (
                    abortAtJittedLoopBodies &&
                    (result >= builder.options.minimumTraceLength) &&
                    // This is an unproductive heuristic if backward branches are on
                    !builder.options.noExitBackwardBranches
                ) {
                    if (!inBranchBlock || firstOpcodeInBlock) {
                        // Use mono_jiterp_trace_transfer to call the target trace recursively
                        // Ideally we would import the trace function to do a direct call instead
                        //  of an indirect one, but right now the import section is generated
                        //  before we generate the function body, so it would be non-trivial to
                        //  do this. It's still faster than returning to the interpreter main loop
                        const targetTrace = getArgU32(ip, 1);
                        builder.ip_const(ip);
                        builder.i32_const(targetTrace);
                        builder.local("frame");
                        builder.local("pLocals");
                        builder.callImport("transfer");
                        builder.appendU8(WasmOpcode.return_);
                        ip = abort;
                    }
                }
                break;

            case MintOpcode.MINT_TIER_PREPARE_JITERPRETER:
            case MintOpcode.MINT_TIER_NOP_JITERPRETER: // FIXME: Should we abort for NOPs like ENTERs?
            case MintOpcode.MINT_NOP:
            case MintOpcode.MINT_DEF:
            case MintOpcode.MINT_DUMMY_USE:
            case MintOpcode.MINT_IL_SEQ_POINT:
            case MintOpcode.MINT_TIER_PATCHPOINT_DATA:
            case MintOpcode.MINT_MONO_MEMORY_BARRIER:
            case MintOpcode.MINT_SDB_BREAKPOINT:
            case MintOpcode.MINT_SDB_INTR_LOC:
            case MintOpcode.MINT_SDB_SEQ_POINT:
                isLowValueOpcode = true;
                break;

            case MintOpcode.MINT_SAFEPOINT:
                append_safepoint(builder, ip);
                break;

            case MintOpcode.MINT_LDLOCA_S:
                // Pre-load locals for the store op
                builder.local("pLocals");
                // locals[ip[1]] = &locals[ip[2]]
                append_ldloca(builder, getArgU16(ip, 2));
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;

            case MintOpcode.MINT_LDSTR:
            case MintOpcode.MINT_LDFTN:
            case MintOpcode.MINT_LDFTN_ADDR:
            case MintOpcode.MINT_LDPTR: {
                // Pre-load locals for the store op
                builder.local("pLocals");

                // frame->imethod->data_items [ip [2]]
                let data = get_imethod_data(frame, getArgU16(ip, 2));
                if (opcode === MintOpcode.MINT_LDFTN)
                    data = <any>cwraps.mono_jiterp_imethod_to_ftnptr(<any>data);

                builder.ptr_const(data);

                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_CPOBJ_VT: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3));
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.ptr_const(klass);
                builder.callImport("value_copy");
                break;
            }
            case MintOpcode.MINT_CPOBJ_VT_NOREF: {
                const sizeBytes = getArgU16(ip, 3);
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                append_memmove_dest_src(builder, sizeBytes);
                break;
            }
            case MintOpcode.MINT_LDOBJ_VT: {
                const size = getArgU16(ip, 3);
                append_ldloca(builder, getArgU16(ip, 1), size, true);
                append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                append_memmove_dest_src(builder, size);
                break;
            }
            case MintOpcode.MINT_STOBJ_VT: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3));
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloca(builder, getArgU16(ip, 2), 0, true);
                builder.ptr_const(klass);
                builder.callImport("value_copy");
                break;
            }
            case MintOpcode.MINT_STOBJ_VT_NOREF: {
                const sizeBytes = getArgU16(ip, 3);
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloca(builder, getArgU16(ip, 2), 0, true);
                append_memmove_dest_src(builder, sizeBytes);
                break;
            }

            case MintOpcode.MINT_STRLEN: {
                builder.local("pLocals");
                append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.StringLength), 2);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_GETCHR: {
                builder.block();
                // index
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                // stash it, we'll be using it multiple times
                builder.local("math_lhs32", WasmOpcode.tee_local);
                // str
                append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                // get string length
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.StringLength), 2);
                // index < length
                builder.appendU8(WasmOpcode.i32_lt_s);
                // index >= 0
                builder.local("math_lhs32");
                builder.i32_const(0);
                builder.appendU8(WasmOpcode.i32_ge_s);
                // (index >= 0) && (index < length)
                builder.appendU8(WasmOpcode.i32_and);
                // If either of the index checks failed we will fall through to the bailout
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.StringOperationFailed);
                builder.endBlock();

                // The null check and range check both passed so we can load the character now
                // Pre-load destination for the stloc at the end (we can't do this inside the block above)
                builder.local("pLocals");
                // (index * 2) + offsetof(MonoString, chars) + pString
                builder.local("math_lhs32");
                builder.i32_const(2);
                builder.appendU8(WasmOpcode.i32_mul);
                builder.local("cknull_ptr");
                builder.appendU8(WasmOpcode.i32_add);
                // Load char
                builder.appendU8(WasmOpcode.i32_load16_u);
                builder.appendMemarg(getMemberOffset(JiterpMember.StringData), 1);
                // Store into result
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_GETITEM_SPAN:
            case MintOpcode.MINT_GETITEM_LOCALSPAN: {
                const elementSize = getArgI16(ip, 4);
                builder.block();
                // Load index and stash it in lhs32
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                builder.local("math_lhs32", WasmOpcode.tee_local);

                // Load address of the span structure
                if (opcode === MintOpcode.MINT_GETITEM_SPAN) {
                    // span = *(MonoSpanOfVoid *)locals[2]
                    append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                } else {
                    // span = (MonoSpanOfVoid)locals[2]
                    append_ldloca(builder, getArgU16(ip, 2), 0);
                    builder.local("cknull_ptr", WasmOpcode.tee_local);
                    cknullOffset = -1;
                }

                // length = span->length
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.SpanLength), 2);
                // index < length
                builder.appendU8(WasmOpcode.i32_lt_u);
                // index >= 0
                // FIXME: It would be nice to optimize this down to a single (index < length) comparison
                //  but interp.c doesn't do it - presumably because a span could be bigger than 2gb?
                builder.local("math_lhs32");
                builder.i32_const(0);
                builder.appendU8(WasmOpcode.i32_ge_s);
                // (index >= 0) && (index < length)
                builder.appendU8(WasmOpcode.i32_and);
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.SpanOperationFailed);
                builder.endBlock();

                // We successfully null checked and bounds checked. Now compute
                //  the address and store it to the destination
                builder.local("pLocals");

                // src = span->_reference + (index * element_size);
                builder.local("cknull_ptr");
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.SpanData), 2);

                builder.local("math_lhs32");
                builder.i32_const(elementSize);
                builder.appendU8(WasmOpcode.i32_mul);
                builder.appendU8(WasmOpcode.i32_add);

                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_INTRINS_SPAN_CTOR: {
                // if (len < 0) bailout
                builder.block();
                // int len = LOCAL_VAR (ip [3], gint32);
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                builder.local("math_rhs32", WasmOpcode.tee_local);
                builder.i32_const(0);
                builder.appendU8(WasmOpcode.i32_ge_s);
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.SpanOperationFailed);
                builder.endBlock();
                // gpointer span = locals + ip [1];
                append_ldloca(builder, getArgU16(ip, 1), 16, true);
                builder.local("math_lhs32", WasmOpcode.tee_local);
                // *(gpointer*)span = ptr;
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.appendU8(WasmOpcode.i32_store);
                builder.appendMemarg(0, 0);
                // *(gint32*)((gpointer*)span + 1) = len;
                builder.local("math_lhs32");
                builder.local("math_rhs32");
                builder.appendU8(WasmOpcode.i32_store);
                builder.appendMemarg(4, 0);
                break;
            }

            case MintOpcode.MINT_LD_DELEGATE_METHOD_PTR: {
                // FIXME: ldloca invalidation size
                append_ldloca(builder, getArgU16(ip, 1), 8, true);
                append_ldloca(builder, getArgU16(ip, 2), 8, true);
                builder.callImport("ld_del_ptr");
                break;
            }
            case MintOpcode.MINT_LDTSFLDA: {
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                // This value is unsigned but I32 is probably right
                builder.ptr_const(getArgI32(ip, 2));
                builder.callImport("ldtsflda");
                break;
            }
            case MintOpcode.MINT_INTRINS_UNSAFE_BYTE_OFFSET:
                builder.local("pLocals");
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.appendU8(WasmOpcode.i32_sub);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            case MintOpcode.MINT_INTRINS_GET_TYPE:
                builder.block();
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport("gettype");
                // bailout if gettype failed
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock();
                break;
            case MintOpcode.MINT_INTRINS_ENUM_HASFLAG: {
                const klass = get_imethod_data(frame, getArgU16(ip, 4));
                builder.ptr_const(klass);
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                append_ldloca(builder, getArgU16(ip, 3), 0);
                builder.callImport("hasflag");
                break;
            }
            case MintOpcode.MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF: {
                const offset = getMemberOffset(JiterpMember.ArrayData);
                builder.local("pLocals");
                append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                builder.i32_const(offset);
                builder.appendU8(WasmOpcode.i32_add);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }
            case MintOpcode.MINT_INTRINS_GET_HASHCODE:
                builder.local("pLocals");
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport("hashcode");
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            case MintOpcode.MINT_INTRINS_TRY_GET_HASHCODE:
                builder.local("pLocals");
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport("try_hash");
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            case MintOpcode.MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE:
                builder.local("pLocals");
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport("hascsize");
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;

            case MintOpcode.MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII: {
                builder.local("pLocals");
                // valueA (cache in lhs32, we need it again later)
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.local("math_lhs32", WasmOpcode.tee_local);
                // valueB
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                // compute differentBits = (valueA ^ valueB) << 2
                builder.appendU8(WasmOpcode.i32_xor);
                builder.i32_const(2);
                builder.appendU8(WasmOpcode.i32_shl);
                builder.local("math_rhs32", WasmOpcode.set_local);
                // compute indicator
                builder.local("math_lhs32");
                builder.i32_const(0x00050005);
                builder.appendU8(WasmOpcode.i32_add);
                builder.i32_const(0x00A000A0);
                builder.appendU8(WasmOpcode.i32_or);
                builder.i32_const(0x001A001A);
                builder.appendU8(WasmOpcode.i32_add);
                builder.i32_const(-8388737); // 0xFF7FFF7F == 4286578559U == -8388737
                builder.appendU8(WasmOpcode.i32_or);
                // result = (differentBits & indicator) == 0
                builder.local("math_rhs32");
                builder.appendU8(WasmOpcode.i32_and);
                builder.appendU8(WasmOpcode.i32_eqz);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_ARRAY_RANK:
            case MintOpcode.MINT_ARRAY_ELEMENT_SIZE: {
                builder.block();
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport(opcode === MintOpcode.MINT_ARRAY_RANK ? "array_rank" : "a_elesize");
                // If the array was null we will bail out, otherwise continue
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_CASTCLASS:
            case MintOpcode.MINT_ISINST:
            case MintOpcode.MINT_CASTCLASS_COMMON:
            case MintOpcode.MINT_ISINST_COMMON:
            case MintOpcode.MINT_CASTCLASS_INTERFACE:
            case MintOpcode.MINT_ISINST_INTERFACE: {
                builder.block();
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                // klass
                builder.ptr_const(get_imethod_data(frame, getArgU16(ip, 3)));
                // opcode
                builder.i32_const(opcode);
                builder.callImport("cast");
                // if cast operation succeeded, skip the bailout
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.CastFailed);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_BOX:
            case MintOpcode.MINT_BOX_VT: {
                // MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];
                builder.ptr_const(get_imethod_data(frame, getArgU16(ip, 3)));
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.i32_const(opcode === MintOpcode.MINT_BOX_VT ? 1 : 0);
                builder.callImport("box");
                break;
            }
            case MintOpcode.MINT_UNBOX: {
                builder.block();
                // MonoClass *c = (MonoClass*)frame->imethod->data_items [ip [3]];
                builder.ptr_const(get_imethod_data(frame, getArgU16(ip, 3)));
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport("try_unbox");
                // If the unbox operation succeeded, continue, otherwise bailout
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.UnboxFailed);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_NEWSTR: {
                builder.block();
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.callImport("newstr");
                // If the newstr operation succeeded, continue, otherwise bailout
                // Note that this assumes the newstr operation will fail again when the interpreter does it
                //  (the only reason for a newstr to fail I can think of is an out-of-memory condition)
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.AllocFailed);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_NEWOBJ_INLINED: {
                builder.block();
                // MonoObject *o = mono_gc_alloc_obj (vtable, m_class_get_instance_size (vtable->klass));
                append_ldloca(builder, getArgU16(ip, 1), 4, true);
                builder.ptr_const(get_imethod_data(frame, getArgU16(ip, 2)));
                // LOCAL_VAR (ip [1], MonoObject*) = o;
                builder.callImport("newobj_i");
                // If the newobj operation succeeded, continue, otherwise bailout
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.AllocFailed);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_NEWOBJ_VT_INLINED: {
                const ret_size = getArgU16(ip, 3);
                // memset (this_vt, 0, ret_size);
                append_ldloca(builder, getArgU16(ip, 2), ret_size, true);
                append_memset_dest(builder, 0, ret_size);
                // LOCAL_VAR (ip [1], gpointer) = this_vt;
                builder.local("pLocals");
                append_ldloca(builder, getArgU16(ip, 2), ret_size, true);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_NEWOBJ:
            case MintOpcode.MINT_NEWOBJ_VT:
            case MintOpcode.MINT_CALLVIRT_FAST:
            case MintOpcode.MINT_CALL: {
                if (countCallTargets) {
                    const targetImethod = get_imethod_data(frame, getArgU16(ip, 3));
                    const targetMethod = <MonoMethod><any>getU32_unaligned(targetImethod);
                    const count = callTargetCounts[<any>targetMethod];
                    if (typeof (count) === "number")
                        callTargetCounts[<any>targetMethod] = count + 1;
                    else
                        callTargetCounts[<any>targetMethod] = 1;
                }
                if (builder.branchTargets.size > 0) {
                    // We generate a bailout instead of aborting, because we don't want calls
                    //  to abort the entire trace if we have branch support enabled - the call
                    //  might be infrequently hit and as a result it's worth it to keep going.
                    append_bailout(builder, ip, BailoutReason.Call);
                    isLowValueOpcode = true;
                } else {
                    // We're in a block that executes unconditionally, and no branches have been
                    //  executed before now so the trace will always need to bail out into the
                    //  interpreter here. No point in compiling more.
                    ip = abort;
                }
                break;
            }

            // TODO: Verify that this isn't worse. I think these may only show up in wrappers?
            // case MintOpcode.MINT_JIT_CALL:
            case MintOpcode.MINT_CALLI:
            case MintOpcode.MINT_CALLI_NAT:
            case MintOpcode.MINT_CALLI_NAT_DYNAMIC:
            case MintOpcode.MINT_CALLI_NAT_FAST:
            case MintOpcode.MINT_CALL_DELEGATE:
                // See comments for MINT_CALL
                if (builder.branchTargets.size > 0) {
                    append_bailout(builder, ip,
                        opcode == MintOpcode.MINT_CALL_DELEGATE
                            ? BailoutReason.CallDelegate
                            : BailoutReason.Call
                    );
                    isLowValueOpcode = true;
                } else {
                    ip = abort;
                }
                break;

            // Unlike regular rethrow which will only appear in catch blocks,
            //  MONO_RETHROW appears to show up in other places, so it's worth conditional bailout
            case MintOpcode.MINT_MONO_RETHROW:
            case MintOpcode.MINT_THROW:
                // As above, only abort if this throw happens unconditionally.
                // Otherwise, it may be in a branch that is unlikely to execute
                if (builder.branchTargets.size > 0) {
                    append_bailout(builder, ip, BailoutReason.Throw);
                    isLowValueOpcode = true;
                } else {
                    ip = abort;
                }
                break;

            case MintOpcode.MINT_ENDFINALLY:
                // This one might make sense to partially implement, but the jump target
                //  is computed at runtime which would make it hard to figure out where
                //  we need to put branch targets. Not worth just doing a conditional
                //  bailout since finally blocks always run
                ip = abort;
                break;

            case MintOpcode.MINT_RETHROW:
            case MintOpcode.MINT_PROF_EXIT:
            case MintOpcode.MINT_PROF_EXIT_VOID:
                ip = abort;
                break;

            // Generating code for these is kind of complex due to the intersection of JS and int64,
            //  and it would bloat the implementation so we handle them all in C instead and match
            //  the interp implementation. Most of these are rare in runtime tests or browser bench
            case MintOpcode.MINT_CONV_OVF_I4_I8:
            case MintOpcode.MINT_CONV_OVF_U4_I8:
            case MintOpcode.MINT_CONV_OVF_I4_U8:
            case MintOpcode.MINT_CONV_OVF_I4_R8:
            case MintOpcode.MINT_CONV_OVF_I8_R8:
            case MintOpcode.MINT_CONV_OVF_I4_R4:
            case MintOpcode.MINT_CONV_OVF_I8_R4:
            case MintOpcode.MINT_CONV_OVF_U4_I4:
                builder.block();
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 8, true);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.i32_const(opcode);
                builder.callImport("conv");
                // If the conversion succeeded, continue, otherwise bailout
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.Overflow); // could be underflow but awkward to tell
                builder.endBlock();
                break;

            /*
             *  The native conversion opcodes for these are not specified for nan/inf, and v8
             *  chooses to throw, so we have to do some tricks to identify non-finite values
             *  and substitute INTnn_MIN, like clang would.
             *  This attempts to reproduce what clang does in -O3 with no special flags set:
             *
             *  f64 -> i64
             *
             *  block
             *  local.get       0
             *  f64.abs
             *  f64.const       0x1p63
             *  f64.lt
             *  i32.eqz
             *  br_if           0                               # 0: down to label0
             *  local.get       0
             *  i64.trunc_f64_s
             *  return
             *  end_block                               # label0:
             *  i64.const       -9223372036854775808
             *
             *  f32 -> i32
             *
             *  block
             *  local.get       0
             *  f32.abs
             *  f32.const       0x1p31
             *  f32.lt
             *  i32.eqz
             *  br_if           0                               # 0: down to label3
             *  local.get       0
             *  i32.trunc_f32_s
             *  return
             *  end_block                               # label3:
             *  i32.const       -2147483648
             */
            case MintOpcode.MINT_CONV_I4_R4:
            case MintOpcode.MINT_CONV_I4_R8:
            case MintOpcode.MINT_CONV_I8_R4:
            case MintOpcode.MINT_CONV_I8_R8: {
                const isF32 = (opcode === MintOpcode.MINT_CONV_I4_R4) ||
                        (opcode === MintOpcode.MINT_CONV_I8_R4),
                    isI64 = (opcode === MintOpcode.MINT_CONV_I8_R4) ||
                        (opcode === MintOpcode.MINT_CONV_I8_R8),
                    limit = isI64
                        ? 9223372036854775807 // this will round up to 0x1p63
                        : 2147483648, // this is 0x1p31 exactly
                    tempLocal = isF32 ? "temp_f32" : "temp_f64";

                // Pre-load locals for the result store at the end
                builder.local("pLocals");

                // Load src
                append_ldloc(builder, getArgU16(ip, 2), isF32 ? WasmOpcode.f32_load : WasmOpcode.f64_load);
                builder.local(tempLocal, WasmOpcode.tee_local);

                // Detect whether the value is within the representable range for the target type
                builder.appendU8(isF32 ? WasmOpcode.f32_abs : WasmOpcode.f64_abs);
                builder.appendU8(isF32 ? WasmOpcode.f32_const : WasmOpcode.f64_const);
                if (isF32)
                    builder.appendF32(limit);
                else
                    builder.appendF64(limit);
                builder.appendU8(isF32 ? WasmOpcode.f32_lt : WasmOpcode.f64_lt);

                // Select value via an if block that returns the result
                builder.block(isI64 ? WasmValtype.i64 : WasmValtype.i32, WasmOpcode.if_);
                // Value in range so truncate it to the appropriate type
                builder.local(tempLocal);
                builder.appendU8(floatToIntTable[opcode]);
                builder.appendU8(WasmOpcode.else_);
                // Value out of range so load the appropriate boundary value
                builder.appendU8(isI64 ? WasmOpcode.i64_const : WasmOpcode.i32_const);
                builder.appendBoundaryValue(isI64 ? 64 : 32, -1);
                builder.endBlock();

                append_stloc_tail(builder, getArgU16(ip, 1), isI64 ? WasmOpcode.i64_store : WasmOpcode.i32_store);

                break;
            }

            case MintOpcode.MINT_ADD_MUL_I4_IMM:
            case MintOpcode.MINT_ADD_MUL_I8_IMM: {
                const isI32 = opcode === MintOpcode.MINT_ADD_MUL_I4_IMM;
                builder.local("pLocals");
                append_ldloc(builder, getArgU16(ip, 2), isI32 ? WasmOpcode.i32_load : WasmOpcode.i64_load);
                const rhs = getArgI16(ip, 3),
                    multiplier = getArgI16(ip, 4);
                if (isI32)
                    builder.i32_const(rhs);
                else
                    builder.i52_const(rhs);
                builder.appendU8(isI32 ? WasmOpcode.i32_add : WasmOpcode.i64_add);
                if (isI32)
                    builder.i32_const(multiplier);
                else
                    builder.i52_const(multiplier);
                builder.appendU8(isI32 ? WasmOpcode.i32_mul : WasmOpcode.i64_mul);
                append_stloc_tail(builder, getArgU16(ip, 1), isI32 ? WasmOpcode.i32_store : WasmOpcode.i64_store);
                break;
            }

            case MintOpcode.MINT_MONO_CMPXCHG_I4:
                builder.local("pLocals");
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load); // dest
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load); // newVal
                append_ldloc(builder, getArgU16(ip, 4), WasmOpcode.i32_load); // expected
                builder.callImport("cmpxchg_i32");
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            case MintOpcode.MINT_MONO_CMPXCHG_I8:
                // because i64 values can't pass through JS cleanly (c.f getRawCwrap and
                // EMSCRIPTEN_KEEPALIVE), we pass addresses of newVal, expected and the return value
                // to the helper function.  The "dest" for the compare-exchange is already a
                // pointer, so load it normally
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load); // dest
                append_ldloca(builder, getArgU16(ip, 3), 0); // newVal
                append_ldloca(builder, getArgU16(ip, 4), 0); // expected
                append_ldloca(builder, getArgU16(ip, 1), 8, true); // oldVal
                builder.callImport("cmpxchg_i64");
                break;

            case MintOpcode.MINT_FMA:
            case MintOpcode.MINT_FMAF: {
                const isF32 = (opcode === MintOpcode.MINT_FMAF),
                    loadOp = isF32 ? WasmOpcode.f32_load : WasmOpcode.f64_load,
                    storeOp = isF32 ? WasmOpcode.f32_store : WasmOpcode.f64_store;

                builder.local("pLocals");

                // LOCAL_VAR (ip [1], double) = fma (LOCAL_VAR (ip [2], double), LOCAL_VAR (ip [3], double), LOCAL_VAR (ip [4], double));
                append_ldloc(builder, getArgU16(ip, 2), loadOp);
                append_ldloc(builder, getArgU16(ip, 3), loadOp);
                append_ldloc(builder, getArgU16(ip, 4), loadOp);

                builder.callImport(isF32 ? "fmaf" : "fma");

                append_stloc_tail(builder, getArgU16(ip, 1), storeOp);
                break;
            }

            default:
                if (
                    (
                        (opcode >= MintOpcode.MINT_RET) &&
                        (opcode <= MintOpcode.MINT_RET_U2)
                    ) ||
                    (
                        (opcode >= MintOpcode.MINT_RET_I4_IMM) &&
                        (opcode <= MintOpcode.MINT_RET_I8_IMM)
                    )
                ) {
                    if ((builder.branchTargets.size > 0) || trapTraceErrors || builder.options.countBailouts) {
                        append_bailout(builder, ip, BailoutReason.Return);
                        isLowValueOpcode = true;
                    } else
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_LDC_I4_M1) &&
                    (opcode <= MintOpcode.MINT_LDC_R8)
                ) {
                    if (!emit_ldc(builder, ip, opcode))
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_MOV_I4_I1) &&
                    (opcode <= MintOpcode.MINT_MOV_8_4)
                ) {
                    if (!emit_mov(builder, ip, opcode))
                        ip = abort;
                } else if (
                    // binops
                    (opcode >= MintOpcode.MINT_ADD_I4) &&
                    (opcode <= MintOpcode.MINT_CLT_UN_R8)
                ) {
                    if (!emit_binop(builder, ip, opcode))
                        ip = abort;
                } else if (unopTable[opcode]) {
                    if (!emit_unop(builder, ip, opcode))
                        ip = abort;
                } else if (relopbranchTable[opcode]) {
                    if (!emit_relop_branch(builder, ip, opcode))
                        ip = abort;
                } else if (
                    // instance ldfld/stfld
                    (opcode >= MintOpcode.MINT_LDFLD_I1) &&
                    (opcode <= MintOpcode.MINT_STFLD_R8_UNALIGNED)
                ) {
                    if (!emit_fieldop(builder, frame, ip, opcode))
                        ip = abort;
                } else if (
                    // static ldfld/stfld
                    (opcode >= MintOpcode.MINT_LDSFLD_I1) &&
                    (opcode <= MintOpcode.MINT_LDTSFLDA)
                ) {
                    if (!emit_sfieldop(builder, frame, ip, opcode))
                        ip = abort;
                } else if (
                    // indirect load/store
                    (opcode >= MintOpcode.MINT_LDIND_I1) &&
                    (opcode <= MintOpcode.MINT_STIND_OFFSET_IMM_I8)
                ) {
                    if (!emit_indirectop(builder, ip, opcode))
                        ip = abort;
                } else if (
                    // math intrinsics
                    (opcode >= MintOpcode.MINT_ASIN) &&
                    (opcode <= MintOpcode.MINT_MAXF)
                ) {
                    if (!emit_math_intrinsic(builder, ip, opcode))
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_LDELEM_I1) &&
                    (opcode <= MintOpcode.MINT_LDLEN)
                ) {
                    if (!emit_arrayop(builder, frame, ip, opcode))
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_BRFALSE_I4_SP) &&
                    (opcode <= MintOpcode.MINT_BLT_UN_I8_IMM_SP)
                ) {
                    // NOTE: This elseif comes last so that specific safepoint branch
                    //  types can be handled by emit_branch or emit_relop_branch,
                    //  to only perform a conditional bailout
                    // complex safepoint branches, just generate a bailout
                    if (builder.branchTargets.size > 0) {
                        append_bailout(builder, ip, BailoutReason.ComplexBranch);
                        isLowValueOpcode = true;
                    } else
                        ip = abort;
                } else {
                    ip = abort;
                }
                break;
        }

        if (ip) {
            if (!skipDregInvalidation && builder.allowNullCheckOptimization) {
                // Invalidate cached values for all the instruction's destination registers.
                // This should have already happened, but it's possible there are opcodes where
                //  our invalidation is incorrect so it's best to do this for safety reasons
                const firstDreg = <any>ip + 2;
                for (let r = 0; r < info[2]; r++) {
                    const dreg = getU16(firstDreg + (r * 2));
                    invalidate_local(dreg);
                }
            }

            if ((trace > 1) || traceOnError || traceOnRuntimeError || mostRecentOptions!.dumpTraces || instrumentedTraceId) {
                let stmtText = `${(<any>ip).toString(16)} ${opname} `;
                const firstDreg = <any>ip + 2;
                const firstSreg = firstDreg + (info[2] * 2);
                // print sregs
                for (let r = 0; r < info[3]; r++) {
                    if (r !== 0)
                        stmtText += ", ";
                    stmtText += getU16(firstSreg + (r * 2));
                }

                // print dregs
                if (info[2] > 0)
                    stmtText += " -> ";
                for (let r = 0; r < info[2]; r++) {
                    if (r !== 0)
                        stmtText += ", ";
                    stmtText += getU16(firstDreg + (r * 2));
                }

                builder.traceBuf.push(stmtText);
            }

            if (!isLowValueOpcode)
                result++;

            ip += <any>(info[1] * 2);
            if (<any>ip <= (<any>endOfBody))
                rip = ip;
            // For debugging
            if (emitPadding)
                builder.appendU8(WasmOpcode.nop);
        } else {
            if (instrumentedTraceId)
                console.log(`instrumented trace ${traceName} aborted for opcode ${opname} @${_ip}`);
            record_abort(traceIp, _ip, traceName, opcode);
        }
    }

    if (emitPadding)
        builder.appendU8(WasmOpcode.nop);

    // Ensure that if execution runs past the end of our last branch block, we
    //  update eip appropriately so that we will return the right ip
    builder.ip_const(rip);
    builder.local("eip", WasmOpcode.set_local);

    // We need to close any open blocks before generating our closing ret,
    //  because wasm would allow branching past the ret otherwise
    while (builder.activeBlocks > 0)
        builder.endBlock();

    // Now we generate a ret at the end of the function body so it's Valid(tm)
    // When branching is enabled, we will have potentially updated eip due to a
    //  branch and then executed forward without ever finding it, so we want to
    //  return the branch target and ensure that the interpreter starts running
    //  from there.
    builder.local("eip");
    builder.appendU8(WasmOpcode.return_);
    builder.appendU8(WasmOpcode.end);

    return result;
}

const addressTakenLocals : Set<number> = new Set();
const notNullSince : Map<number, number> = new Map();
let cknullOffset = -1;

function eraseInferredState () {
    cknullOffset = -1;
    notNullSince.clear();
}

function invalidate_local (offset: number) {
    if (cknullOffset === offset)
        cknullOffset = -1;
    notNullSince.delete(offset);
}

function invalidate_local_range (start: number, bytes: number) {
    for (let i = 0; i < bytes; i += 1)
        invalidate_local(start + i);
}

function append_branch_target_block (builder: WasmBuilder, ip: MintOpcodePtr) {
    builder.endBlock();
    // Create a new branch block that conditionally executes depending on the eip local
    // FIXME: For methods containing backward branches, we will have one of these compares
    //  at the top of the trace and pay the cost of it on every entry even though it will
    //  always pass. If we never generate any backward branches during compilation, we should
    //  patch it out
    builder.local("eip");
    builder.ip_const(ip);
    builder.appendU8(WasmOpcode.i32_eq);
    builder.block(WasmValtype.void, WasmOpcode.if_);
}

function append_ldloc (builder: WasmBuilder, offset: number, opcode: WasmOpcode) {
    builder.local("pLocals");
    builder.appendU8(opcode);
    // stackval is 8 bytes, but pLocals might not be 8 byte aligned so we use 4
    // wasm spec prohibits alignment higher than natural alignment, just to be annoying
    const alignment = (opcode > WasmOpcode.f64_load) ? 0 : 2;
    builder.appendMemarg(offset, alignment);
}

// You need to have pushed pLocals onto the stack *before* the value you intend to store
// Wasm store opcodes are shaped like xNN.store [offset] [alignment],
//  where the offset+alignment pair is referred to as a 'memarg' by the spec.
// The actual store operation is equivalent to `pBase[offset] = value` (alignment has no
//  observable impact on behavior, other than causing compilation failures if out of range)
function append_stloc_tail (builder: WasmBuilder, offset: number, opcode: WasmOpcode) {
    builder.appendU8(opcode);
    // stackval is 8 bytes, but pLocals might not be 8 byte aligned so we use 4
    // wasm spec prohibits alignment higher than natural alignment, just to be annoying
    const alignment = (opcode > WasmOpcode.f64_store) ? 0 : 2;
    builder.appendMemarg(offset, alignment);
    invalidate_local(offset);
}

// Pass bytesInvalidated=0 if you are reading from the local and the address will never be
//  used for writes
// Pass transient=true if the address will not persist after use (so it can't be used to later
//  modify the contents of this local)
function append_ldloca (builder: WasmBuilder, localOffset: number, bytesInvalidated?: number, transient?: boolean) {
    if (typeof (bytesInvalidated) !== "number")
        bytesInvalidated = 512;
    // FIXME: We need to know how big this variable is so we can invalidate the whole space it occupies
    if (bytesInvalidated > 0)
        invalidate_local_range(localOffset, bytesInvalidated);
    if ((bytesInvalidated > 0) && (transient !== true))
        addressTakenLocals.add(localOffset);
    builder.lea("pLocals", localOffset);
}

function append_memset_local (builder: WasmBuilder, localOffset: number, value: number, count: number) {
    invalidate_local_range(localOffset, count);

    // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
    if (try_append_memset_fast(builder, localOffset, value, count, false))
        return;

    // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
    append_ldloca(builder, localOffset, count, true);
    append_memset_dest(builder, value, count);
}

function append_memmove_local_local (builder: WasmBuilder, destLocalOffset: number, sourceLocalOffset: number, count: number) {
    invalidate_local_range(destLocalOffset, count);

    if (try_append_memmove_fast(builder, destLocalOffset, sourceLocalOffset, count, false))
        return true;

    // spec: pop n, pop s, pop d, copy n bytes from s to d
    append_ldloca(builder, destLocalOffset, count, true);
    append_ldloca(builder, sourceLocalOffset, 0);
    append_memmove_dest_src(builder, count);
}

// Loads the specified i32 value and then bails out if it is null, leaving it in the cknull_ptr local.
function append_ldloc_cknull (builder: WasmBuilder, localOffset: number, ip: MintOpcodePtr, leaveOnStack: boolean) {
    const optimize = builder.allowNullCheckOptimization &&
        !addressTakenLocals.has(localOffset) &&
        notNullSince.has(localOffset);

    if (optimize) {
        counters.nullChecksEliminated++;
        if (nullCheckCaching && (cknullOffset === localOffset)) {
            if (traceNullCheckOptimizations)
                console.log(`(0x${(<any>ip).toString(16)}) cknull_ptr == locals[${localOffset}], not null since 0x${notNullSince.get(localOffset)!.toString(16)}`);
            if (leaveOnStack)
                builder.local("cknull_ptr");
        } else {
            // console.log(`skipping null check for ${localOffset}`);
            append_ldloc(builder, localOffset, WasmOpcode.i32_load);
            builder.local("cknull_ptr", leaveOnStack ? WasmOpcode.tee_local : WasmOpcode.set_local);
            if (traceNullCheckOptimizations)
                console.log(`(0x${(<any>ip).toString(16)}) cknull_ptr := locals[${localOffset}] (fresh load, already null checked at 0x${notNullSince.get(localOffset)!.toString(16)})`);
            cknullOffset = localOffset;
        }

        if (nullCheckValidation) {
            builder.local("cknull_ptr");
            append_ldloc(builder, localOffset, WasmOpcode.i32_load);
            builder.i32_const(builder.base);
            builder.i32_const(ip);
            builder.callImport("notnull");
        }
        return;
    }

    builder.block();
    append_ldloc(builder, localOffset, WasmOpcode.i32_load);
    builder.local("cknull_ptr", WasmOpcode.tee_local);
    builder.appendU8(WasmOpcode.br_if);
    builder.appendULeb(0);
    append_bailout(builder, ip, BailoutReason.NullCheck);
    builder.endBlock();
    if (leaveOnStack)
        builder.local("cknull_ptr");

    if (
        !addressTakenLocals.has(localOffset) &&
        builder.allowNullCheckOptimization
    ) {
        notNullSince.set(localOffset, <any>ip);
        if (traceNullCheckOptimizations)
            console.log(`(0x${(<any>ip).toString(16)}) cknull_ptr := locals[${localOffset}] (fresh load, fresh null check)`);
        cknullOffset = localOffset;
    } else
        cknullOffset = -1;
}

const ldcTable : { [opcode: number]: [WasmOpcode, number] } = {
    [MintOpcode.MINT_LDC_I4_M1]: [WasmOpcode.i32_const, -1],
    [MintOpcode.MINT_LDC_I4_0]:  [WasmOpcode.i32_const, 0 ],
    [MintOpcode.MINT_LDC_I4_1]:  [WasmOpcode.i32_const, 1 ],
    [MintOpcode.MINT_LDC_I4_2]:  [WasmOpcode.i32_const, 2 ],
    [MintOpcode.MINT_LDC_I4_3]:  [WasmOpcode.i32_const, 3 ],
    [MintOpcode.MINT_LDC_I4_4]:  [WasmOpcode.i32_const, 4 ],
    [MintOpcode.MINT_LDC_I4_5]:  [WasmOpcode.i32_const, 5 ],
    [MintOpcode.MINT_LDC_I4_6]:  [WasmOpcode.i32_const, 6 ],
    [MintOpcode.MINT_LDC_I4_7]:  [WasmOpcode.i32_const, 7 ],
    [MintOpcode.MINT_LDC_I4_8]:  [WasmOpcode.i32_const, 8 ],
};

function emit_ldc (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    let storeType = WasmOpcode.i32_store;

    const tableEntry = ldcTable[opcode];
    if (tableEntry) {
        builder.local("pLocals");
        builder.appendU8(tableEntry[0]);
        builder.appendLeb(tableEntry[1]);
    } else {
        switch (opcode) {
            case MintOpcode.MINT_LDC_I4_S:
                builder.local("pLocals");
                builder.i32_const(getArgI16(ip, 2));
                break;
            case MintOpcode.MINT_LDC_I4:
                builder.local("pLocals");
                builder.i32_const(getArgI32(ip, 2));
                break;
            case MintOpcode.MINT_LDC_I8_0:
                builder.local("pLocals");
                builder.i52_const(0);
                storeType = WasmOpcode.i64_store;
                break;
            case MintOpcode.MINT_LDC_I8:
                builder.local("pLocals");
                builder.appendU8(WasmOpcode.i64_const);
                builder.appendLebRef(<any>ip + (2 * 2), true);
                storeType = WasmOpcode.i64_store;
                break;
            case MintOpcode.MINT_LDC_I8_S:
                builder.local("pLocals");
                builder.i52_const(getArgI16(ip, 2));
                storeType = WasmOpcode.i64_store;
                break;
            case MintOpcode.MINT_LDC_R4:
                builder.local("pLocals");
                builder.appendU8(WasmOpcode.f32_const);
                builder.appendF32(getArgF32(ip, 2));
                storeType = WasmOpcode.f32_store;
                break;
            case MintOpcode.MINT_LDC_R8:
                builder.local("pLocals");
                builder.appendU8(WasmOpcode.f64_const);
                builder.appendF64(getArgF64(ip, 2));
                storeType = WasmOpcode.f64_store;
                break;
            default:
                return false;
        }
    }

    // spec: pop c, pop i, i[offset]=c
    builder.appendU8(storeType);
    // These are constants being stored into locals and are always at least 4 bytes
    //  so we can use a 4 byte alignment (8 would be nice if we could guarantee
    //  that locals are 8-byte aligned)
    const localOffset = getArgU16(ip, 1);
    builder.appendMemarg(localOffset, 2);
    invalidate_local(localOffset);

    return true;
}

function emit_mov (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    let loadOp = WasmOpcode.i32_load, storeOp = WasmOpcode.i32_store;
    switch (opcode) {
        case MintOpcode.MINT_MOV_I4_I1:
            loadOp = WasmOpcode.i32_load8_s;
            break;
        case MintOpcode.MINT_MOV_I4_U1:
            loadOp = WasmOpcode.i32_load8_u;
            break;
        case MintOpcode.MINT_MOV_I4_I2:
            loadOp = WasmOpcode.i32_load16_s;
            break;
        case MintOpcode.MINT_MOV_I4_U2:
            loadOp = WasmOpcode.i32_load16_u;
            break;
        case MintOpcode.MINT_MOV_1:
            loadOp = WasmOpcode.i32_load8_u;
            storeOp = WasmOpcode.i32_store8;
            break;
        case MintOpcode.MINT_MOV_2:
            loadOp = WasmOpcode.i32_load16_u;
            storeOp = WasmOpcode.i32_store16;
            break;
        case MintOpcode.MINT_MOV_4:
            break;
        case MintOpcode.MINT_MOV_8:
            loadOp = WasmOpcode.i64_load;
            storeOp = WasmOpcode.i64_store;
            break;
        case MintOpcode.MINT_MOV_VT: {
            const sizeBytes = getArgU16(ip, 3);
            append_memmove_local_local(builder, getArgU16(ip, 1), getArgU16(ip, 2), sizeBytes);
            return true;
        }
        case MintOpcode.MINT_MOV_8_2:
            append_memmove_local_local(builder, getArgU16(ip, 1), getArgU16(ip, 2), 8);
            append_memmove_local_local(builder, getArgU16(ip, 3), getArgU16(ip, 4), 8);
            return true;
        case MintOpcode.MINT_MOV_8_3:
            append_memmove_local_local(builder, getArgU16(ip, 1), getArgU16(ip, 2), 8);
            append_memmove_local_local(builder, getArgU16(ip, 3), getArgU16(ip, 4), 8);
            append_memmove_local_local(builder, getArgU16(ip, 5), getArgU16(ip, 6), 8);
            return true;
        case MintOpcode.MINT_MOV_8_4:
            append_memmove_local_local(builder, getArgU16(ip, 1), getArgU16(ip, 2), 8);
            append_memmove_local_local(builder, getArgU16(ip, 3), getArgU16(ip, 4), 8);
            append_memmove_local_local(builder, getArgU16(ip, 5), getArgU16(ip, 6), 8);
            append_memmove_local_local(builder, getArgU16(ip, 7), getArgU16(ip, 8), 8);
            return true;
        default:
            return false;
    }

    // i
    builder.local("pLocals");

    // c = LOCAL_VAR (ip [2], argtype2)
    append_ldloc(builder, getArgU16(ip, 2), loadOp);
    append_stloc_tail(builder, getArgU16(ip, 1), storeOp);

    return true;
}

function append_vtable_initialize (builder: WasmBuilder, pVtable: NativePointer, ip: MintOpcodePtr) {
    // TODO: Actually initialize the vtable instead of just checking and bailing out?
    builder.block();
    // FIXME: This will prevent us from reusing traces between runs since the vtables can move
    // We could bake the offset of the flag into this but it's nice to have the vtable ptr
    //  in the trace as a constant visible in the wasm
    builder.ptr_const(<any>pVtable);
    builder.appendU8(WasmOpcode.i32_load8_u);
    builder.appendMemarg(getMemberOffset(JiterpMember.VtableInitialized), 0);
    builder.appendU8(WasmOpcode.br_if);
    builder.appendULeb(0);
    append_bailout(builder, ip, BailoutReason.VtableNotInitialized);
    builder.endBlock();
}

function emit_fieldop (
    builder: WasmBuilder, frame: NativePointer,
    ip: MintOpcodePtr, opcode: MintOpcode
) : boolean {
    const isLoad = (
        (opcode >= MintOpcode.MINT_LDFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDFLDA_UNSAFE)
    ) || (
        (opcode >= MintOpcode.MINT_LDSFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDSFLD_W)
    );

    const objectOffset = getArgU16(ip, isLoad ? 2 : 1),
        fieldOffset = getArgU16(ip, 3),
        localOffset = getArgU16(ip, isLoad ? 1 : 2);

    // Check this before potentially emitting a cknull
    const notNull = builder.allowNullCheckOptimization &&
        !addressTakenLocals.has(objectOffset) &&
        notNullSince.has(objectOffset);

    if (
        (opcode !== MintOpcode.MINT_LDFLDA_UNSAFE) &&
        (opcode !== MintOpcode.MINT_STFLD_O)
    )
        append_ldloc_cknull(builder, objectOffset, ip, false);

    let setter = WasmOpcode.i32_store,
        getter = WasmOpcode.i32_load;

    switch (opcode) {
        case MintOpcode.MINT_LDFLD_I1:
            getter = WasmOpcode.i32_load8_s;
            break;
        case MintOpcode.MINT_LDFLD_U1:
            getter = WasmOpcode.i32_load8_u;
            break;
        case MintOpcode.MINT_LDFLD_I2:
            getter = WasmOpcode.i32_load16_s;
            break;
        case MintOpcode.MINT_LDFLD_U2:
            getter = WasmOpcode.i32_load16_u;
            break;
        case MintOpcode.MINT_LDFLD_O:
        case MintOpcode.MINT_STFLD_I4:
        case MintOpcode.MINT_LDFLD_I4:
            // default
            break;
        case MintOpcode.MINT_STFLD_R4:
        case MintOpcode.MINT_LDFLD_R4:
            getter = WasmOpcode.f32_load;
            setter = WasmOpcode.f32_store;
            break;
        case MintOpcode.MINT_STFLD_R8:
        case MintOpcode.MINT_LDFLD_R8:
            getter = WasmOpcode.f64_load;
            setter = WasmOpcode.f64_store;
            break;
        case MintOpcode.MINT_STFLD_I1:
        case MintOpcode.MINT_STFLD_U1:
            setter = WasmOpcode.i32_store8;
            break;
        case MintOpcode.MINT_STFLD_I2:
        case MintOpcode.MINT_STFLD_U2:
            setter = WasmOpcode.i32_store16;
            break;
        case MintOpcode.MINT_LDFLD_I8:
        case MintOpcode.MINT_STFLD_I8:
            getter = WasmOpcode.i64_load;
            setter = WasmOpcode.i64_store;
            break;
        case MintOpcode.MINT_STFLD_O: {
            /*
             * Writing a ref-type field has to call an import to perform the write barrier anyway,
             *  and technically it should use a different kind of barrier from copy_pointer. So
             *  we define a special import that is responsible for performing the whole stfld_o
             *  operation with as little trace-side overhead as possible
             * Previously the pseudocode looked like:
             *  cknull_ptr = *(MonoObject *)&locals[objectOffset];
             *  if (!cknull_ptr) bailout;
             *  copy_pointer(cknull_ptr + fieldOffset, *(MonoObject *)&locals[localOffset])
             * The null check optimization also allows us to safely omit the bailout check
             *  if we know that the target object isn't null. Even if the target object were
             *  somehow null in this case (bad! shouldn't be possible!) it won't be a crash
             *  because the implementation of stfld_o does its own null check.
             */
            if (!notNull)
                builder.block();

            builder.local("pLocals");
            builder.i32_const(fieldOffset);
            builder.i32_const(objectOffset); // dest
            builder.i32_const(localOffset); // src
            builder.callImport("stfld_o");

            if (!notNull) {
                builder.appendU8(WasmOpcode.br_if);
                builder.appendLeb(0);
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock();
            } else {
                if (traceNullCheckOptimizations)
                    console.log(`(0x${(<any>ip).toString(16)}) locals[${objectOffset}] not null since 0x${notNullSince.get(objectOffset)!.toString(16)}`);

                builder.appendU8(WasmOpcode.drop);
                counters.nullChecksEliminated++;

                if (nullCheckValidation) {
                    // cknull_ptr was not used here so all we can do is verify that the target object is not null
                    append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
                    append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
                    builder.i32_const(builder.base);
                    builder.i32_const(ip);
                    builder.callImport("notnull");
                }
            }
            return true;
        }
        case MintOpcode.MINT_LDFLD_VT: {
            const sizeBytes = getArgU16(ip, 4);
            // dest
            append_ldloca(builder, localOffset, sizeBytes, true);
            // src
            builder.local("cknull_ptr");
            builder.i32_const(fieldOffset);
            builder.appendU8(WasmOpcode.i32_add);
            append_memmove_dest_src(builder, sizeBytes);
            return true;
        }
        case MintOpcode.MINT_STFLD_VT: {
            const klass = get_imethod_data(frame, getArgU16(ip, 4));
            // dest = (char*)o + ip [3]
            builder.local("cknull_ptr");
            builder.i32_const(fieldOffset);
            builder.appendU8(WasmOpcode.i32_add);
            // src = locals + ip [2]
            append_ldloca(builder, localOffset, 0);
            builder.ptr_const(klass);
            builder.callImport("value_copy");
            return true;
        }
        case MintOpcode.MINT_STFLD_VT_NOREF: {
            const sizeBytes = getArgU16(ip, 4);
            // dest
            builder.local("cknull_ptr");
            builder.i32_const(fieldOffset);
            builder.appendU8(WasmOpcode.i32_add);
            // src
            append_ldloca(builder, localOffset, 0);
            append_memmove_dest_src(builder, sizeBytes);
            return true;
        }

        case MintOpcode.MINT_LDFLDA_UNSAFE:
        case MintOpcode.MINT_LDFLDA:
            builder.local("pLocals");
            // cknull_ptr isn't always initialized here
            append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
            builder.i32_const(fieldOffset);
            builder.appendU8(WasmOpcode.i32_add);
            append_stloc_tail(builder, localOffset, setter);
            return true;

        default:
            return false;
    }

    if (isLoad)
        builder.local("pLocals");

    builder.local("cknull_ptr");

    if (isLoad) {
        builder.appendU8(getter);
        builder.appendMemarg(fieldOffset, 0);
        append_stloc_tail(builder, localOffset, setter);
        return true;
    } else {
        append_ldloc(builder, localOffset, getter);
        builder.appendU8(setter);
        builder.appendMemarg(fieldOffset, 0);
        return true;
    }
}

function emit_sfieldop (
    builder: WasmBuilder, frame: NativePointer,
    ip: MintOpcodePtr, opcode: MintOpcode
) : boolean {
    const isLoad = (
        (opcode >= MintOpcode.MINT_LDFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDFLDA_UNSAFE)
    ) || (
        (opcode >= MintOpcode.MINT_LDSFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDSFLD_W)
    );

    const localOffset = getArgU16(ip, 1),
        pVtable = get_imethod_data(frame, getArgU16(ip, 2)),
        pStaticData = get_imethod_data(frame, getArgU16(ip, 3));

    append_vtable_initialize(builder, <any>pVtable, ip);

    let setter = WasmOpcode.i32_store,
        getter = WasmOpcode.i32_load;

    switch (opcode) {
        case MintOpcode.MINT_LDSFLD_I1:
            getter = WasmOpcode.i32_load8_s;
            break;
        case MintOpcode.MINT_LDSFLD_U1:
            getter = WasmOpcode.i32_load8_u;
            break;
        case MintOpcode.MINT_LDSFLD_I2:
            getter = WasmOpcode.i32_load16_s;
            break;
        case MintOpcode.MINT_LDSFLD_U2:
            getter = WasmOpcode.i32_load16_u;
            break;
        case MintOpcode.MINT_LDSFLD_O:
        case MintOpcode.MINT_STSFLD_I4:
        case MintOpcode.MINT_LDSFLD_I4:
            // default
            break;
        case MintOpcode.MINT_STSFLD_R4:
        case MintOpcode.MINT_LDSFLD_R4:
            getter = WasmOpcode.f32_load;
            setter = WasmOpcode.f32_store;
            break;
        case MintOpcode.MINT_STSFLD_R8:
        case MintOpcode.MINT_LDSFLD_R8:
            getter = WasmOpcode.f64_load;
            setter = WasmOpcode.f64_store;
            break;
        case MintOpcode.MINT_STSFLD_I1:
        case MintOpcode.MINT_STSFLD_U1:
            setter = WasmOpcode.i32_store8;
            break;
        case MintOpcode.MINT_STSFLD_I2:
        case MintOpcode.MINT_STSFLD_U2:
            setter = WasmOpcode.i32_store16;
            break;
        case MintOpcode.MINT_LDSFLD_I8:
        case MintOpcode.MINT_STSFLD_I8:
            getter = WasmOpcode.i64_load;
            setter = WasmOpcode.i64_store;
            break;
        case MintOpcode.MINT_STSFLD_O:
            // dest
            builder.ptr_const(pStaticData);
            // src
            append_ldloca(builder, localOffset, 0);
            // FIXME: Use mono_gc_wbarrier_set_field_internal
            builder.callImport("copy_pointer");
            return true;
        case MintOpcode.MINT_LDSFLD_VT: {
            const sizeBytes = getArgU16(ip, 4);
            // dest
            append_ldloca(builder, localOffset, sizeBytes, true);
            // src
            builder.ptr_const(pStaticData);
            append_memmove_dest_src(builder, sizeBytes);
            return true;
        }

        case MintOpcode.MINT_LDSFLDA:
            builder.local("pLocals");
            builder.ptr_const(pStaticData);
            append_stloc_tail(builder, localOffset, setter);
            return true;

        default:
            return false;
    }

    if (isLoad) {
        builder.local("pLocals");
        builder.ptr_const(pStaticData);
        builder.appendU8(getter);
        builder.appendMemarg(0, 0);
        append_stloc_tail(builder, localOffset, setter);
        return true;
    } else {
        builder.ptr_const(pStaticData);
        append_ldloc(builder, localOffset, getter);
        builder.appendU8(setter);
        builder.appendMemarg(0, 0);
        return true;
    }
}

// operator, loadOperator, storeOperator
type OpRec3 = [WasmOpcode, WasmOpcode, WasmOpcode];
// operator, lhsLoadOperator, rhsLoadOperator, storeOperator
type OpRec4 = [WasmOpcode, WasmOpcode, WasmOpcode, WasmOpcode];

const floatToIntTable : { [opcode: number]: WasmOpcode } = {
    [MintOpcode.MINT_CONV_I4_R4]: WasmOpcode.i32_trunc_s_f32,
    [MintOpcode.MINT_CONV_I8_R4]: WasmOpcode.i64_trunc_s_f32,
    [MintOpcode.MINT_CONV_I4_R8]: WasmOpcode.i32_trunc_s_f64,
    [MintOpcode.MINT_CONV_I8_R8]: WasmOpcode.i64_trunc_s_f64,
};

// thanks for making this as complex as possible, typescript
const unopTable : { [opcode: number]: OpRec3 | undefined } = {
    [MintOpcode.MINT_CEQ0_I4]:    [WasmOpcode.i32_eqz,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD1_I4]:    [WasmOpcode.i32_add,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SUB1_I4]:    [WasmOpcode.i32_sub,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_NEG_I4]:     [WasmOpcode.i32_sub,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_NOT_I4]:     [WasmOpcode.i32_xor,   WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_ADD1_I8]:    [WasmOpcode.i64_add,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SUB1_I8]:    [WasmOpcode.i64_sub,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_NEG_I8]:     [WasmOpcode.i64_sub,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_NOT_I8]:     [WasmOpcode.i64_xor,   WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_ADD_I4_IMM]: [WasmOpcode.i32_add,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_I4_IMM]: [WasmOpcode.i32_mul,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_I8_IMM]: [WasmOpcode.i64_add,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_MUL_I8_IMM]: [WasmOpcode.i64_mul,   WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_NEG_R4]:     [WasmOpcode.f32_neg,   WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_NEG_R8]:     [WasmOpcode.f64_neg,   WasmOpcode.f64_load, WasmOpcode.f64_store],

    [MintOpcode.MINT_CONV_R4_I4]: [WasmOpcode.f32_convert_s_i32, WasmOpcode.i32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_CONV_R8_I4]: [WasmOpcode.f64_convert_s_i32, WasmOpcode.i32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R_UN_I4]: [WasmOpcode.f64_convert_u_i32, WasmOpcode.i32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R4_I8]: [WasmOpcode.f32_convert_s_i64, WasmOpcode.i64_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_CONV_R8_I8]: [WasmOpcode.f64_convert_s_i64, WasmOpcode.i64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R_UN_I8]: [WasmOpcode.f64_convert_u_i64, WasmOpcode.i64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R8_R4]: [WasmOpcode.f64_promote_f32,   WasmOpcode.f32_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_CONV_R4_R8]: [WasmOpcode.f32_demote_f64,    WasmOpcode.f64_load, WasmOpcode.f32_store],

    [MintOpcode.MINT_CONV_I8_I4]: [WasmOpcode.nop,               WasmOpcode.i64_load32_s, WasmOpcode.i64_store],
    [MintOpcode.MINT_CONV_I8_U4]: [WasmOpcode.nop,               WasmOpcode.i64_load32_u, WasmOpcode.i64_store],

    [MintOpcode.MINT_CONV_U1_I4]: [WasmOpcode.i32_and,           WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_U2_I4]: [WasmOpcode.i32_and,           WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I1_I4]: [WasmOpcode.i32_shr_s,         WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I2_I4]: [WasmOpcode.i32_shr_s,         WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CONV_U1_I8]: [WasmOpcode.i32_and,           WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_U2_I8]: [WasmOpcode.i32_and,           WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I1_I8]: [WasmOpcode.i32_shr_s,           WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CONV_I2_I8]: [WasmOpcode.i32_shr_s,           WasmOpcode.i64_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_SHL_I4_IMM]:     [WasmOpcode.i32_shl,       WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHL_I8_IMM]:     [WasmOpcode.i64_shl,       WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_I4_IMM]:     [WasmOpcode.i32_shr_s,     WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_I8_IMM]:     [WasmOpcode.i64_shr_s,     WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_UN_I4_IMM]:  [WasmOpcode.i32_shr_u,     WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_UN_I8_IMM]:  [WasmOpcode.i64_shr_u,     WasmOpcode.i64_load, WasmOpcode.i64_store],
};

// HACK: Generating correct wasm for these is non-trivial so we hand them off to C.
// The opcode specifies whether the operands need to be promoted first.
const intrinsicFpBinops : { [opcode: number] : WasmOpcode } = {
    [MintOpcode.MINT_CEQ_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CEQ_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CNE_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CNE_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CGT_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGT_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CGE_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGE_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CGT_UN_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CGT_UN_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CLT_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLT_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CLT_UN_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLT_UN_R8]: WasmOpcode.nop,
    [MintOpcode.MINT_CLE_R4]: WasmOpcode.f64_promote_f32,
    [MintOpcode.MINT_CLE_R8]: WasmOpcode.nop,
    [JiterpSpecialOpcode.CGE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CLE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CNE_UN_R4]: WasmOpcode.f64_promote_f32,
    [JiterpSpecialOpcode.CGE_UN_R8]: WasmOpcode.nop,
    [JiterpSpecialOpcode.CLE_UN_R8]: WasmOpcode.nop,
    [JiterpSpecialOpcode.CNE_UN_R8]: WasmOpcode.nop,
};

const binopTable : { [opcode: number]: OpRec3 | OpRec4 | undefined } = {
    [MintOpcode.MINT_ADD_I4]:    [WasmOpcode.i32_add,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_OVF_I4]:[WasmOpcode.i32_add,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_ADD_OVF_UN_I4]:[WasmOpcode.i32_add,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SUB_I4]:    [WasmOpcode.i32_sub,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_I4]:    [WasmOpcode.i32_mul,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_OVF_I4]:[WasmOpcode.i32_mul,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_MUL_OVF_UN_I4]:[WasmOpcode.i32_mul,WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_DIV_I4]:    [WasmOpcode.i32_div_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_DIV_UN_I4]: [WasmOpcode.i32_div_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_REM_I4]:    [WasmOpcode.i32_rem_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_REM_UN_I4]: [WasmOpcode.i32_rem_u, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_AND_I4]:    [WasmOpcode.i32_and,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_OR_I4]:     [WasmOpcode.i32_or,    WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_XOR_I4]:    [WasmOpcode.i32_xor,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHL_I4]:    [WasmOpcode.i32_shl,   WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_I4]:    [WasmOpcode.i32_shr_s, WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_SHR_UN_I4]: [WasmOpcode.i32_shr_u, WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_ADD_I8]:    [WasmOpcode.i64_add,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SUB_I8]:    [WasmOpcode.i64_sub,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_MUL_I8]:    [WasmOpcode.i64_mul,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_DIV_I8]:    [WasmOpcode.i64_div_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_REM_I8]:    [WasmOpcode.i64_rem_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_DIV_UN_I8]: [WasmOpcode.i64_div_u, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_REM_UN_I8]: [WasmOpcode.i64_rem_u, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_AND_I8]:    [WasmOpcode.i64_and,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_OR_I8]:     [WasmOpcode.i64_or,    WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_XOR_I8]:    [WasmOpcode.i64_xor,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHL_I8]:    [WasmOpcode.i64_shl,   WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_I8]:    [WasmOpcode.i64_shr_s, WasmOpcode.i64_load, WasmOpcode.i64_store],
    [MintOpcode.MINT_SHR_UN_I8]: [WasmOpcode.i64_shr_u, WasmOpcode.i64_load, WasmOpcode.i64_store],

    [MintOpcode.MINT_ADD_R4]:    [WasmOpcode.f32_add,   WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_SUB_R4]:    [WasmOpcode.f32_sub,   WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_MUL_R4]:    [WasmOpcode.f32_mul,   WasmOpcode.f32_load, WasmOpcode.f32_store],
    [MintOpcode.MINT_DIV_R4]:    [WasmOpcode.f32_div,   WasmOpcode.f32_load, WasmOpcode.f32_store],

    [MintOpcode.MINT_ADD_R8]:    [WasmOpcode.f64_add,   WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_SUB_R8]:    [WasmOpcode.f64_sub,   WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_MUL_R8]:    [WasmOpcode.f64_mul,   WasmOpcode.f64_load, WasmOpcode.f64_store],
    [MintOpcode.MINT_DIV_R8]:    [WasmOpcode.f64_div,   WasmOpcode.f64_load, WasmOpcode.f64_store],

    [MintOpcode.MINT_CEQ_I4]:    [WasmOpcode.i32_eq,    WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CNE_I4]:    [WasmOpcode.i32_ne,    WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLT_I4]:    [WasmOpcode.i32_lt_s,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_I4]:    [WasmOpcode.i32_gt_s,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_I4]:    [WasmOpcode.i32_le_s,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_I4]:    [WasmOpcode.i32_ge_s,  WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CLT_UN_I4]: [WasmOpcode.i32_lt_u,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_UN_I4]: [WasmOpcode.i32_gt_u,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_UN_I4]: [WasmOpcode.i32_le_u,  WasmOpcode.i32_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_UN_I4]: [WasmOpcode.i32_ge_u,  WasmOpcode.i32_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CEQ_I8]:    [WasmOpcode.i64_eq,    WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CNE_I8]:    [WasmOpcode.i64_ne,    WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLT_I8]:    [WasmOpcode.i64_lt_s,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_I8]:    [WasmOpcode.i64_gt_s,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_I8]:    [WasmOpcode.i64_le_s,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_I8]:    [WasmOpcode.i64_ge_s,  WasmOpcode.i64_load, WasmOpcode.i32_store],

    [MintOpcode.MINT_CLT_UN_I8]: [WasmOpcode.i64_lt_u,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGT_UN_I8]: [WasmOpcode.i64_gt_u,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CLE_UN_I8]: [WasmOpcode.i64_le_u,  WasmOpcode.i64_load, WasmOpcode.i32_store],
    [MintOpcode.MINT_CGE_UN_I8]: [WasmOpcode.i64_ge_u,  WasmOpcode.i64_load, WasmOpcode.i32_store],

};

const relopbranchTable : { [opcode: number]: [comparisonOpcode: MintOpcode, immediateOpcode: WasmOpcode | false, isSafepoint: boolean] | MintOpcode | undefined } = {
    [MintOpcode.MINT_BEQ_I4_S]:         MintOpcode.MINT_CEQ_I4,
    [MintOpcode.MINT_BNE_UN_I4_S]:      MintOpcode.MINT_CNE_I4,
    [MintOpcode.MINT_BGT_I4_S]:         MintOpcode.MINT_CGT_I4,
    [MintOpcode.MINT_BGT_UN_I4_S]:      MintOpcode.MINT_CGT_UN_I4,
    [MintOpcode.MINT_BLT_I4_S]:         MintOpcode.MINT_CLT_I4,
    [MintOpcode.MINT_BLT_UN_I4_S]:      MintOpcode.MINT_CLT_UN_I4,
    [MintOpcode.MINT_BGE_I4_S]:         MintOpcode.MINT_CGE_I4,
    [MintOpcode.MINT_BGE_UN_I4_S]:      MintOpcode.MINT_CGE_UN_I4,
    [MintOpcode.MINT_BLE_I4_S]:         MintOpcode.MINT_CLE_I4,
    [MintOpcode.MINT_BLE_UN_I4_S]:      MintOpcode.MINT_CLE_UN_I4,

    [MintOpcode.MINT_BEQ_I4_SP]:        [MintOpcode.MINT_CEQ_I4, false, true],
    [MintOpcode.MINT_BNE_UN_I4_SP]:     [MintOpcode.MINT_CNE_I4, false, true],
    [MintOpcode.MINT_BGT_I4_SP]:        [MintOpcode.MINT_CGT_I4, false, true],
    [MintOpcode.MINT_BGT_UN_I4_SP]:     [MintOpcode.MINT_CGT_UN_I4, false, true],
    [MintOpcode.MINT_BLT_I4_SP]:        [MintOpcode.MINT_CLT_I4, false, true],
    [MintOpcode.MINT_BLT_UN_I4_SP]:     [MintOpcode.MINT_CLT_UN_I4, false, true],
    [MintOpcode.MINT_BGE_I4_SP]:        [MintOpcode.MINT_CGE_I4, false, true],
    [MintOpcode.MINT_BGE_UN_I4_SP]:     [MintOpcode.MINT_CGE_UN_I4, false, true],
    [MintOpcode.MINT_BLE_I4_SP]:        [MintOpcode.MINT_CLE_I4, false, true],
    [MintOpcode.MINT_BLE_UN_I4_SP]:     [MintOpcode.MINT_CLE_UN_I4, false, true],

    [MintOpcode.MINT_BEQ_I4_IMM_SP]:    [MintOpcode.MINT_CEQ_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BNE_UN_I4_IMM_SP]: [MintOpcode.MINT_CNE_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGT_I4_IMM_SP]:    [MintOpcode.MINT_CGT_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGT_UN_I4_IMM_SP]: [MintOpcode.MINT_CGT_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLT_I4_IMM_SP]:    [MintOpcode.MINT_CLT_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLT_UN_I4_IMM_SP]: [MintOpcode.MINT_CLT_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGE_I4_IMM_SP]:    [MintOpcode.MINT_CGE_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BGE_UN_I4_IMM_SP]: [MintOpcode.MINT_CGE_UN_I4, WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLE_I4_IMM_SP]:    [MintOpcode.MINT_CLE_I4,    WasmOpcode.i32_const, true],
    [MintOpcode.MINT_BLE_UN_I4_IMM_SP]: [MintOpcode.MINT_CLE_UN_I4, WasmOpcode.i32_const, true],

    [MintOpcode.MINT_BEQ_I8_S]:         MintOpcode.MINT_CEQ_I8,
    [MintOpcode.MINT_BNE_UN_I8_S]:      MintOpcode.MINT_CNE_I8,
    [MintOpcode.MINT_BGT_I8_S]:         MintOpcode.MINT_CGT_I8,
    [MintOpcode.MINT_BGT_UN_I8_S]:      MintOpcode.MINT_CGT_UN_I8,
    [MintOpcode.MINT_BLT_I8_S]:         MintOpcode.MINT_CLT_I8,
    [MintOpcode.MINT_BLT_UN_I8_S]:      MintOpcode.MINT_CLT_UN_I8,
    [MintOpcode.MINT_BGE_I8_S]:         MintOpcode.MINT_CGE_I8,
    [MintOpcode.MINT_BGE_UN_I8_S]:      MintOpcode.MINT_CGE_UN_I8,
    [MintOpcode.MINT_BLE_I8_S]:         MintOpcode.MINT_CLE_I8,
    [MintOpcode.MINT_BLE_UN_I8_S]:      MintOpcode.MINT_CLE_UN_I8,

    [MintOpcode.MINT_BEQ_I8_IMM_SP]:    [MintOpcode.MINT_CEQ_I8,    WasmOpcode.i64_const, true],
    // FIXME: Missing compare opcode
    // [MintOpcode.MINT_BNE_UN_I8_IMM_SP]: [MintOpcode.MINT_CNE_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGT_I8_IMM_SP]:    [MintOpcode.MINT_CGT_I8,    WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGT_UN_I8_IMM_SP]: [MintOpcode.MINT_CGT_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLT_I8_IMM_SP]:    [MintOpcode.MINT_CLT_I8,    WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLT_UN_I8_IMM_SP]: [MintOpcode.MINT_CLT_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGE_I8_IMM_SP]:    [MintOpcode.MINT_CGE_I8,    WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BGE_UN_I8_IMM_SP]: [MintOpcode.MINT_CGE_UN_I8, WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLE_I8_IMM_SP]:    [MintOpcode.MINT_CLE_I8,    WasmOpcode.i64_const, true],
    [MintOpcode.MINT_BLE_UN_I8_IMM_SP]: [MintOpcode.MINT_CLE_UN_I8, WasmOpcode.i64_const, true],

    [MintOpcode.MINT_BEQ_R4_S]:         MintOpcode.MINT_CEQ_R4,
    [MintOpcode.MINT_BNE_UN_R4_S]:      <any>JiterpSpecialOpcode.CNE_UN_R4,
    [MintOpcode.MINT_BGT_R4_S]:         MintOpcode.MINT_CGT_R4,
    [MintOpcode.MINT_BGT_UN_R4_S]:      MintOpcode.MINT_CGT_UN_R4,
    [MintOpcode.MINT_BLT_R4_S]:         MintOpcode.MINT_CLT_R4,
    [MintOpcode.MINT_BLT_UN_R4_S]:      MintOpcode.MINT_CLT_UN_R4,
    [MintOpcode.MINT_BGE_R4_S]:         MintOpcode.MINT_CGE_R4,
    [MintOpcode.MINT_BGE_UN_R4_S]:      <any>JiterpSpecialOpcode.CGE_UN_R4,
    [MintOpcode.MINT_BLE_R4_S]:         MintOpcode.MINT_CLE_R4,
    [MintOpcode.MINT_BLE_UN_R4_S]:      <any>JiterpSpecialOpcode.CLE_UN_R4,

    [MintOpcode.MINT_BEQ_R8_S]:         MintOpcode.MINT_CEQ_R8,
    [MintOpcode.MINT_BNE_UN_R8_S]:      <any>JiterpSpecialOpcode.CNE_UN_R8,
    [MintOpcode.MINT_BGT_R8_S]:         MintOpcode.MINT_CGT_R8,
    [MintOpcode.MINT_BGT_UN_R8_S]:      MintOpcode.MINT_CGT_UN_R8,
    [MintOpcode.MINT_BLT_R8_S]:         MintOpcode.MINT_CLT_R8,
    [MintOpcode.MINT_BLT_UN_R8_S]:      MintOpcode.MINT_CLT_UN_R8,
    [MintOpcode.MINT_BGE_R8_S]:         MintOpcode.MINT_CGE_R8,
    [MintOpcode.MINT_BGE_UN_R8_S]:      <any>JiterpSpecialOpcode.CGE_UN_R8,
    [MintOpcode.MINT_BLE_R8_S]:         MintOpcode.MINT_CLE_R8,
    [MintOpcode.MINT_BLE_UN_R8_S]:      <any>JiterpSpecialOpcode.CLE_UN_R8,
};

function emit_binop (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    // operands are popped right to left, which means you build the arg list left to right
    let lhsLoadOp : WasmOpcode, rhsLoadOp : WasmOpcode, storeOp : WasmOpcode,
        lhsVar = "math_lhs32", rhsVar = "math_rhs32",
        info : OpRec3 | OpRec4 | undefined,
        operandsCached = false;

    const intrinsicFpBinop = intrinsicFpBinops[opcode];
    if (intrinsicFpBinop) {
        builder.local("pLocals");
        const isF64 = intrinsicFpBinop == WasmOpcode.nop;
        append_ldloc(builder, getArgU16(ip, 2), isF64 ? WasmOpcode.f64_load : WasmOpcode.f32_load);
        if (!isF64)
            builder.appendU8(intrinsicFpBinop);
        append_ldloc(builder, getArgU16(ip, 3), isF64 ? WasmOpcode.f64_load : WasmOpcode.f32_load);
        if (!isF64)
            builder.appendU8(intrinsicFpBinop);
        builder.i32_const(<any>opcode);
        builder.callImport("relop_fp");
        append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
        return true;
    }

    switch (opcode) {
        case MintOpcode.MINT_REM_R4:
        case MintOpcode.MINT_REM_R8:
            return emit_math_intrinsic(builder, ip, opcode);

        default: {
            info = binopTable[<any>opcode];
            if (!info)
                return false;
            if (info.length > 3) {
                lhsLoadOp = info[1];
                rhsLoadOp = info[2];
                storeOp = info[3]!;
            } else {
                lhsLoadOp = rhsLoadOp = info[1];
                storeOp = info[2];
            }
        }
    }

    switch (opcode) {
        case MintOpcode.MINT_DIV_I4:
        case MintOpcode.MINT_DIV_I8:
        case MintOpcode.MINT_DIV_UN_I4:
        case MintOpcode.MINT_DIV_UN_I8:
        case MintOpcode.MINT_REM_I4:
        case MintOpcode.MINT_REM_I8:
        case MintOpcode.MINT_REM_UN_I4:
        case MintOpcode.MINT_REM_UN_I8: {
            const is64 = (opcode === MintOpcode.MINT_DIV_UN_I8) ||
                (opcode === MintOpcode.MINT_REM_UN_I8) ||
                (opcode === MintOpcode.MINT_DIV_I8) ||
                (opcode === MintOpcode.MINT_REM_I8);
            lhsVar = is64 ? "math_lhs64" : "math_lhs32";
            rhsVar = is64 ? "math_rhs64" : "math_rhs32";

            builder.block();
            append_ldloc(builder, getArgU16(ip, 2), lhsLoadOp);
            builder.local(lhsVar, WasmOpcode.set_local);
            append_ldloc(builder, getArgU16(ip, 3), rhsLoadOp);
            builder.local(rhsVar, WasmOpcode.tee_local);
            operandsCached = true;
            // br_if requires an i32 so to do our divide by zero check on an i64
            //  we do i64_eqz and then i32_eqz to invert the flag
            if (is64) {
                builder.appendU8(WasmOpcode.i64_eqz);
                builder.appendU8(WasmOpcode.i32_eqz);
            }
            // If rhs is zero we want to bailout because it's a divide by zero.
            // A nonzero divisor will cause us to skip past this bailout
            builder.appendU8(WasmOpcode.br_if);
            builder.appendULeb(0);
            append_bailout(builder, ip, BailoutReason.DivideByZero);
            builder.endBlock();

            // Also perform overflow check for signed division operations
            if (
                (opcode === MintOpcode.MINT_DIV_I4) ||
                (opcode === MintOpcode.MINT_REM_I4) ||
                (opcode === MintOpcode.MINT_DIV_I8) ||
                (opcode === MintOpcode.MINT_REM_I8)
            ) {
                builder.block();
                builder.local(rhsVar);
                // If rhs is -1 and lhs is INTnn_MIN this is an overflow
                if (is64)
                    builder.i52_const(-1);
                else
                    builder.i32_const(-1);
                builder.appendU8(is64 ? WasmOpcode.i64_ne : WasmOpcode.i32_ne);
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                // rhs was -1 since the previous br_if didn't execute. Now check lhs.
                builder.local(lhsVar);
                // INTnn_MIN
                builder.appendU8(is64 ? WasmOpcode.i64_const : WasmOpcode.i32_const);
                builder.appendBoundaryValue(is64 ? 64 : 32, -1);
                builder.appendU8(is64 ? WasmOpcode.i64_ne : WasmOpcode.i32_ne);
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.Overflow);
                builder.endBlock();
            }
            break;
        }

        case MintOpcode.MINT_ADD_OVF_I4:
        case MintOpcode.MINT_ADD_OVF_UN_I4:
        case MintOpcode.MINT_MUL_OVF_I4:
        case MintOpcode.MINT_MUL_OVF_UN_I4:
            // Perform overflow check before the operation
            append_ldloc(builder, getArgU16(ip, 2), lhsLoadOp);
            builder.local(lhsVar, WasmOpcode.tee_local);
            append_ldloc(builder, getArgU16(ip, 3), rhsLoadOp);
            builder.local(rhsVar, WasmOpcode.tee_local);
            builder.i32_const(opcode);
            builder.callImport(
                (
                    (opcode === MintOpcode.MINT_ADD_OVF_UN_I4) ||
                    (opcode === MintOpcode.MINT_MUL_OVF_UN_I4)
                )
                    ? "ckovr_u4"
                    : "ckovr_i4"
            );
            builder.block(WasmValtype.void, WasmOpcode.if_);
            append_bailout(builder, ip, BailoutReason.Overflow);
            builder.endBlock();
            operandsCached = true;
            break;
    }

    // i
    builder.local("pLocals");

    // c = (lhs op rhs)
    if (operandsCached) {
        builder.local(lhsVar);
        builder.local(rhsVar);
    } else {
        append_ldloc(builder, getArgU16(ip, 2), lhsLoadOp);
        append_ldloc(builder, getArgU16(ip, 3), rhsLoadOp);
    }
    builder.appendU8(info[0]);

    append_stloc_tail(builder, getArgU16(ip, 1), storeOp);

    return true;
}

function emit_unop (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    // operands are popped right to left, which means you build the arg list left to right
    const info = unopTable[<any>opcode];
    if (!info)
        return false;
    const loadOp = info[1];
    const storeOp = info[2];

    // i
    if ((opcode < MintOpcode.MINT_CONV_OVF_I1_I4) ||
        (opcode > MintOpcode.MINT_CONV_OVF_U8_R8))
        builder.local("pLocals");

    // c = (op value)
    switch (opcode) {
        case MintOpcode.MINT_ADD1_I4:
        case MintOpcode.MINT_SUB1_I4:
            // We implement this as binary 'x +/- 1', the table already has i32_add so we just
            //  need to emit a 1 constant
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i32_const(1);
            break;
        case MintOpcode.MINT_NEG_I4:
            // there's no negate operator so we generate '0 - x'
            builder.i32_const(0);
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            break;
        case MintOpcode.MINT_NOT_I4:
            // there's no not operator so we generate 'x xor -1'
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i32_const(-1);
            break;

        case MintOpcode.MINT_CONV_U1_I4:
        case MintOpcode.MINT_CONV_U1_I8:
            // For (unsigned char) cast of i32/i64 we do an & 255
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            if (loadOp === WasmOpcode.i64_load)
                builder.appendU8(WasmOpcode.i32_wrap_i64);
            builder.i32_const(0xFF);
            break;
        case MintOpcode.MINT_CONV_U2_I4:
        case MintOpcode.MINT_CONV_U2_I8:
            // For (unsigned short) cast of i32/i64 we do an & 65535
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            if (loadOp === WasmOpcode.i64_load)
                builder.appendU8(WasmOpcode.i32_wrap_i64);
            builder.i32_const(0xFFFF);
            break;
        case MintOpcode.MINT_CONV_I1_I4:
        case MintOpcode.MINT_CONV_I1_I8:
            // For (char) cast of i32 we do (val << 24) >> 24
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            if (loadOp === WasmOpcode.i64_load)
                builder.appendU8(WasmOpcode.i32_wrap_i64);
            builder.i32_const(24);
            builder.appendU8(WasmOpcode.i32_shl);
            builder.i32_const(24);
            break;
        case MintOpcode.MINT_CONV_I2_I4:
        case MintOpcode.MINT_CONV_I2_I8:
            // For (char) cast of i32 we do (val << 16) >> 16
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            if (loadOp === WasmOpcode.i64_load)
                builder.appendU8(WasmOpcode.i32_wrap_i64);
            builder.i32_const(16);
            builder.appendU8(WasmOpcode.i32_shl);
            builder.i32_const(16);
            break;

        case MintOpcode.MINT_ADD1_I8:
        case MintOpcode.MINT_SUB1_I8:
            // We implement this as binary 'x +/- 1', the table already has i32_add so we just
            //  need to emit a 1 constant
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i52_const(1);
            break;
        case MintOpcode.MINT_NEG_I8:
            // there's no negate operator so we generate '0 - x'
            builder.i52_const(0);
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            break;
        case MintOpcode.MINT_NOT_I8:
            // there's no not operator so we generate 'x xor -1'
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i52_const(-1);
            break;

        case MintOpcode.MINT_ADD_I4_IMM:
        case MintOpcode.MINT_MUL_I4_IMM:
        case MintOpcode.MINT_SHL_I4_IMM:
        case MintOpcode.MINT_SHR_I4_IMM:
        case MintOpcode.MINT_SHR_UN_I4_IMM:
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i32_const(getArgI16(ip, 3));
            break;

        case MintOpcode.MINT_ADD_I8_IMM:
        case MintOpcode.MINT_MUL_I8_IMM:
        case MintOpcode.MINT_SHL_I8_IMM:
        case MintOpcode.MINT_SHR_I8_IMM:
        case MintOpcode.MINT_SHR_UN_I8_IMM:
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i52_const(getArgI16(ip, 3));
            break;

        default:
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            break;
    }

    if (info[0] !== WasmOpcode.nop)
        builder.appendU8(info[0]);

    append_stloc_tail(builder, getArgU16(ip, 1), storeOp);

    return true;
}

function emit_branch (
    builder: WasmBuilder, ip: MintOpcodePtr,
    opcode: MintOpcode, displacement?: number
) : boolean {
    const info = OpcodeInfo[opcode];
    const isSafepoint = (opcode >= MintOpcode.MINT_BRFALSE_I4_SP) &&
        (opcode <= MintOpcode.MINT_BLT_UN_I8_IMM_SP);
    eraseInferredState();

    // If the branch is taken we bail out to allow the interpreter to do it.
    // So for brtrue, we want to do 'cond == 0' to produce a bailout only
    //  when the branch will be taken (by skipping the bailout in this block)
    // When branches are enabled, instead we set eip and then break out of
    //  the current branch block and execution proceeds forward to find the
    //  branch target (if possible), bailing out at the end otherwise
    switch (opcode) {
        case MintOpcode.MINT_LEAVE:
        case MintOpcode.MINT_LEAVE_S:
        case MintOpcode.MINT_BR:
        case MintOpcode.MINT_BR_S: {
            displacement = ((opcode === MintOpcode.MINT_LEAVE) || (opcode === MintOpcode.MINT_BR))
                ? getArgI32(ip, 1)
                : getArgI16(ip, 1);
            if (traceBranchDisplacements)
                console.log(`br.s @${ip} displacement=${displacement}`);
            const destination = <any>ip + (displacement * 2);

            if (displacement <= 0) {
                if (builder.backBranchOffsets.indexOf(destination) >= 0) {
                    // We found a backward branch target we can branch to, so we branch out
                    //  to the top of the loop body
                    // append_safepoint(builder, ip);
                    if (traceBackBranches)
                        console.log(`performing backward branch to 0x${destination.toString(16)}`);
                    builder.ip_const(destination);
                    builder.local("eip", WasmOpcode.set_local);
                    builder.appendU8(WasmOpcode.br);
                    builder.appendULeb(1);
                    counters.backBranchesEmitted++;
                    return true;
                } else {
                    if (traceBackBranches)
                        console.log(`back branch target 0x${destination.toString(16)} not found`);
                    // FIXME: Should there be a safepoint here?
                    append_bailout(builder, destination, displacement > 0 ? BailoutReason.Branch : BailoutReason.BackwardBranch);
                    counters.backBranchesNotEmitted++;
                    return true;
                }
            } else {
                // Simple branches are enabled and this is a forward branch. We
                //  don't need to wrap things in a block here, we can just exit
                //  the current branch block after updating eip
                builder.branchTargets.add(destination);
                builder.ip_const(destination);
                builder.local("eip", WasmOpcode.set_local);
                builder.appendU8(WasmOpcode.br);
                builder.appendULeb(0);
                return true;
            }
        }

        case MintOpcode.MINT_BRTRUE_I4_S:
        case MintOpcode.MINT_BRFALSE_I4_S:
        case MintOpcode.MINT_BRTRUE_I4_SP:
        case MintOpcode.MINT_BRFALSE_I4_SP:
        case MintOpcode.MINT_BRTRUE_I8_S:
        case MintOpcode.MINT_BRFALSE_I8_S: {
            const is64 = (opcode === MintOpcode.MINT_BRTRUE_I8_S) ||
                (opcode === MintOpcode.MINT_BRFALSE_I8_S);
            // Wrap the conditional branch in a block so we can skip the
            //  actual branch at the end of it
            builder.block();

            displacement = getArgI16(ip, 2);
            append_ldloc(builder, getArgU16(ip, 1), is64 ? WasmOpcode.i64_load : WasmOpcode.i32_load);
            if (
                (opcode === MintOpcode.MINT_BRTRUE_I4_S) ||
                (opcode === MintOpcode.MINT_BRTRUE_I4_SP)
            )
                builder.appendU8(WasmOpcode.i32_eqz);
            else if (opcode === MintOpcode.MINT_BRTRUE_I8_S)
                builder.appendU8(WasmOpcode.i64_eqz);
            else if (opcode === MintOpcode.MINT_BRFALSE_I8_S) {
                // do (i64 == 0) == 0 because br_if can only branch on an i32 operand
                builder.appendU8(WasmOpcode.i64_eqz);
                builder.appendU8(WasmOpcode.i32_eqz);
            }
            break;
        }

        default: {
            // relop branches already had the branch condition loaded by the caller,
            //  so we don't need to load anything. After the condition was loaded, we
            //  treat it like a brtrue
            if (relopbranchTable[opcode] === undefined)
                throw new Error(`Unsupported relop branch opcode: ${opcode}`);

            if (info[1] !== 4)
                throw new Error(`Unsupported long branch opcode: ${info[0]}`);

            builder.appendU8(WasmOpcode.i32_eqz);
            break;
        }
    }

    if (!displacement)
        throw new Error("Branch had no displacement");
    else if (traceBranchDisplacements)
        console.log(`${info[0]} @${ip} displacement=${displacement}`);

    const destination = <any>ip + (displacement * 2);

    // We generate a conditional branch that will skip past the rest of this
    //  tiny branch dispatch block to avoid performing the branch
    builder.appendU8(WasmOpcode.br_if);
    builder.appendULeb(0);

    if (displacement < 0) {
        if (isSafepoint)
            append_safepoint(builder, ip);

        if (builder.backBranchOffsets.indexOf(destination) >= 0) {
            // We found a backwards branch target we can reach via our outer trace loop, so
            //  we update eip and branch out to the top of the loop block
            if (traceBackBranches)
                console.log(`performing conditional backward branch to 0x${destination.toString(16)}`);
            builder.ip_const(destination);
            builder.local("eip", WasmOpcode.set_local);
            builder.appendU8(WasmOpcode.br);
            // break out 3 levels, because the current stack layout is
            // loop {
            //   branch target block {
            //     branch dispatch block {
            // and we want to target the loop in order to jump to the top of it
            builder.appendULeb(2);
            counters.backBranchesEmitted++;
        } else {
            if (traceBackBranches)
                console.log(`back branch target 0x${destination.toString(16)} not found`);
            // We didn't find a loop to branch to, so bail out
            append_bailout(builder, destination, BailoutReason.BackwardBranch);
            counters.backBranchesNotEmitted++;
        }
    } else {
        // Do a safepoint *before* changing our IP, if necessary
        if (isSafepoint)
            append_safepoint(builder, ip);
        // Branching is enabled, so set eip and exit the current branch block
        builder.branchTargets.add(destination);
        builder.ip_const(destination);
        builder.local("eip", WasmOpcode.set_local);
        builder.appendU8(WasmOpcode.br);
        // The branch block encloses this tiny branch dispatch block, so break
        //  out two levels
        builder.appendULeb(1);
    }

    builder.endBlock();
    return true;
}

function emit_relop_branch (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    const relopBranchInfo = relopbranchTable[opcode];
    if (!relopBranchInfo)
        return false;

    const relop = Array.isArray(relopBranchInfo)
        ? relopBranchInfo[0]
        : relopBranchInfo;

    const relopInfo = binopTable[relop];
    const intrinsicFpBinop = intrinsicFpBinops[relop];

    if (!relopInfo && !intrinsicFpBinop)
        return false;

    // We have to wrap the computation of the branch condition inside the
    //  branch block because opening blocks destroys the contents of the
    //  wasm execution stack for some reason
    builder.block();
    const displacement = getArgI16(ip, 3);
    if (traceBranchDisplacements)
        console.log(`relop @${ip} displacement=${displacement}`);

    const operandLoadOp = relopInfo
        ? relopInfo[1]
        : (
            intrinsicFpBinop === WasmOpcode.nop
                ? WasmOpcode.f64_load
                : WasmOpcode.f32_load
        );

    append_ldloc(builder, getArgU16(ip, 1), operandLoadOp);
    // Promote f32 lhs to f64 if necessary
    if (!relopInfo && (intrinsicFpBinop !== WasmOpcode.nop))
        builder.appendU8(intrinsicFpBinop);

    // Compare with immediate
    if (Array.isArray(relopBranchInfo) && relopBranchInfo[1]) {
        // For i8 immediates we need to generate an i64.const even though
        //  the immediate is 16 bits, so we store the relevant opcode
        //  in the relop branch info table
        builder.appendU8(relopBranchInfo[1]);
        builder.appendLeb(getArgI16(ip, 2));
    } else
        append_ldloc(builder, getArgU16(ip, 2), operandLoadOp);

    // Promote f32 rhs to f64 if necessary
    if (!relopInfo && (intrinsicFpBinop != WasmOpcode.nop))
        builder.appendU8(intrinsicFpBinop);

    if (relopInfo) {
        builder.appendU8(relopInfo[0]);
    } else {
        builder.i32_const(<any>relop);
        builder.callImport("relop_fp");
    }

    return emit_branch(builder, ip, opcode, displacement);
}

const mathIntrinsicTable : { [opcode: number] : [isUnary: boolean, isF32: boolean, opcodeOrFuncName: WasmOpcode | string] } = {
    [MintOpcode.MINT_SQRT]:     [true, false,  WasmOpcode.f64_sqrt],
    [MintOpcode.MINT_SQRTF]:    [true, true,   WasmOpcode.f32_sqrt],
    [MintOpcode.MINT_CEILING]:  [true, false,  WasmOpcode.f64_ceil],
    [MintOpcode.MINT_CEILINGF]: [true, true,   WasmOpcode.f32_ceil],
    [MintOpcode.MINT_FLOOR]:    [true, false,  WasmOpcode.f64_floor],
    [MintOpcode.MINT_FLOORF]:   [true, true,   WasmOpcode.f32_floor],
    [MintOpcode.MINT_ABS]:      [true, false,  WasmOpcode.f64_abs],
    [MintOpcode.MINT_ABSF]:     [true, true,   WasmOpcode.f32_abs],

    [MintOpcode.MINT_ACOS]:     [true, false,  "acos"],
    [MintOpcode.MINT_ACOSF]:    [true, true,   "acosf"],
    [MintOpcode.MINT_ACOSH]:    [true, false,  "acosh"],
    [MintOpcode.MINT_ACOSHF]:   [true, true,   "acoshf"],
    [MintOpcode.MINT_COS]:      [true, false,  "cos"],
    [MintOpcode.MINT_COSF]:     [true, true,   "cosf"],
    [MintOpcode.MINT_ASIN]:     [true, false,  "asin"],
    [MintOpcode.MINT_ASINF]:    [true, true,   "asinf"],
    [MintOpcode.MINT_ASINH]:    [true, false,  "asinh"],
    [MintOpcode.MINT_ASINHF]:   [true, true,   "asinhf"],
    [MintOpcode.MINT_SIN]:      [true, false,  "sin"],
    [MintOpcode.MINT_SINF]:     [true, true,   "sinf"],
    [MintOpcode.MINT_ATAN]:     [true, false,  "atan"],
    [MintOpcode.MINT_ATANF]:    [true, true,   "atanf"],
    [MintOpcode.MINT_ATANH]:    [true, false,  "atanh"],
    [MintOpcode.MINT_ATANHF]:   [true, true,   "atanhf"],
    [MintOpcode.MINT_TAN]:      [true, false,  "tan"],
    [MintOpcode.MINT_TANF]:     [true, true,   "tanf"],
    [MintOpcode.MINT_CBRT]:     [true, false,  "cbrt"],
    [MintOpcode.MINT_CBRTF]:    [true, true,   "cbrtf"],
    [MintOpcode.MINT_EXP]:      [true, false,  "exp"],
    [MintOpcode.MINT_EXPF]:     [true, true,   "expf"],
    [MintOpcode.MINT_LOG]:      [true, false,  "log"],
    [MintOpcode.MINT_LOGF]:     [true, true,   "logf"],
    [MintOpcode.MINT_LOG2]:     [true, false,  "log2"],
    [MintOpcode.MINT_LOG2F]:    [true, true,   "log2f"],
    [MintOpcode.MINT_LOG10]:    [true, false,  "log10"],
    [MintOpcode.MINT_LOG10F]:   [true, true,   "log10f"],

    [MintOpcode.MINT_MIN]:      [false, false,  WasmOpcode.f64_min],
    [MintOpcode.MINT_MINF]:     [false, true,   WasmOpcode.f32_min],
    [MintOpcode.MINT_MAX]:      [false, false,  WasmOpcode.f64_max],
    [MintOpcode.MINT_MAXF]:     [false, true,   WasmOpcode.f32_max],

    [MintOpcode.MINT_ATAN2]:    [false, false, "atan2"],
    [MintOpcode.MINT_ATAN2F]:   [false, true,  "atan2f"],
    [MintOpcode.MINT_POW]:      [false, false, "pow"],
    [MintOpcode.MINT_POWF]:     [false, true,  "powf"],
    [MintOpcode.MINT_REM_R8]:   [false, false, "fmod"],
    [MintOpcode.MINT_REM_R4]:   [false, true,  "fmodf"],
};

function emit_math_intrinsic (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    let isUnary : boolean, isF32 : boolean, name: string | undefined;
    let wasmOp : WasmOpcode | undefined;
    const destOffset = getArgU16(ip, 1),
        srcOffset = getArgU16(ip, 2),
        rhsOffset = getArgU16(ip, 3);

    const tableEntry = mathIntrinsicTable[opcode];
    if (tableEntry) {
        isUnary = tableEntry[0];
        isF32 = tableEntry[1];
        if (typeof (tableEntry[2]) === "string")
            name = tableEntry[2];
        else
            wasmOp = tableEntry[2];
    } else {
        return false;
    }

    // Pre-load locals for the stloc at the end
    builder.local("pLocals");

    if (isUnary) {
        append_ldloc(builder, srcOffset, isF32 ? WasmOpcode.f32_load : WasmOpcode.f64_load);
        if (wasmOp) {
            builder.appendU8(wasmOp);
        } else if (name) {
            builder.callImport(name);
        } else
            throw new Error("internal error");
        append_stloc_tail(builder, destOffset, isF32 ? WasmOpcode.f32_store : WasmOpcode.f64_store);
        return true;
    } else {
        append_ldloc(builder, srcOffset, isF32 ? WasmOpcode.f32_load : WasmOpcode.f64_load);
        append_ldloc(builder, rhsOffset, isF32 ? WasmOpcode.f32_load : WasmOpcode.f64_load);

        if (wasmOp) {
            builder.appendU8(wasmOp);
        } else if (name) {
            builder.callImport(name);
        } else
            throw new Error("internal error");

        append_stloc_tail(builder, destOffset, isF32 ? WasmOpcode.f32_store : WasmOpcode.f64_store);
        return true;
    }
}

function emit_indirectop (builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    const isLoad = (opcode >= MintOpcode.MINT_LDIND_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I8);
    const isAddMul = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I8)
    );
    const isOffset = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_IMM_I8)
    ) || (
        (opcode >= MintOpcode.MINT_STIND_OFFSET_I1) &&
        (opcode <= MintOpcode.MINT_STIND_OFFSET_IMM_I8)
    ) || isAddMul;
    const isImm = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_IMM_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_IMM_I8)
    ) || (
        (opcode >= MintOpcode.MINT_STIND_OFFSET_IMM_I1) &&
        (opcode <= MintOpcode.MINT_STIND_OFFSET_IMM_I8)
    ) || isAddMul;

    let valueVarIndex, addressVarIndex, offsetVarIndex = -1, constantOffset = 0,
        constantMultiplier = 1;
    if (isAddMul) {
        valueVarIndex = getArgU16(ip, 1);
        addressVarIndex = getArgU16(ip, 2);
        offsetVarIndex = getArgU16(ip, 3);
        constantOffset = getArgI16(ip, 4);
        constantMultiplier = getArgI16(ip, 5);
    } else if (isOffset) {
        if (isImm) {
            if (isLoad) {
                valueVarIndex = getArgU16(ip, 1);
                addressVarIndex = getArgU16(ip, 2);
                constantOffset = getArgI16(ip, 3);
            } else {
                valueVarIndex = getArgU16(ip, 2);
                addressVarIndex = getArgU16(ip, 1);
                constantOffset = getArgI16(ip, 3);
            }
        } else {
            if (isLoad) {
                valueVarIndex = getArgU16(ip, 1);
                addressVarIndex = getArgU16(ip, 2);
                offsetVarIndex = getArgU16(ip, 3);
            } else {
                valueVarIndex = getArgU16(ip, 3);
                addressVarIndex = getArgU16(ip, 1);
                offsetVarIndex = getArgU16(ip, 2);
            }
        }
    } else if (isLoad) {
        addressVarIndex = getArgU16(ip, 2);
        valueVarIndex = getArgU16(ip, 1);
    } else {
        addressVarIndex = getArgU16(ip, 1);
        valueVarIndex = getArgU16(ip, 2);
    }

    let getter : WasmOpcode, setter = WasmOpcode.i32_store;
    switch (opcode) {
        case MintOpcode.MINT_LDIND_I1:
        case MintOpcode.MINT_LDIND_OFFSET_I1:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_I1:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I1:
            getter = WasmOpcode.i32_load8_s;
            break;
        case MintOpcode.MINT_LDIND_U1:
        case MintOpcode.MINT_LDIND_OFFSET_U1:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_U1:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_U1:
            getter = WasmOpcode.i32_load8_u;
            break;
        case MintOpcode.MINT_LDIND_I2:
        case MintOpcode.MINT_LDIND_OFFSET_I2:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_I2:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I2:
            getter = WasmOpcode.i32_load16_s;
            break;
        case MintOpcode.MINT_LDIND_U2:
        case MintOpcode.MINT_LDIND_OFFSET_U2:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_U2:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_U2:
            getter = WasmOpcode.i32_load16_u;
            break;
        case MintOpcode.MINT_STIND_I1:
        case MintOpcode.MINT_STIND_OFFSET_I1:
        case MintOpcode.MINT_STIND_OFFSET_IMM_I1:
            getter = WasmOpcode.i32_load;
            setter = WasmOpcode.i32_store8;
            break;
        case MintOpcode.MINT_STIND_I2:
        case MintOpcode.MINT_STIND_OFFSET_I2:
        case MintOpcode.MINT_STIND_OFFSET_IMM_I2:
            getter = WasmOpcode.i32_load;
            setter = WasmOpcode.i32_store16;
            break;
        case MintOpcode.MINT_LDIND_I4:
        case MintOpcode.MINT_LDIND_OFFSET_I4:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_I4:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I4:
        case MintOpcode.MINT_STIND_I4:
        case MintOpcode.MINT_STIND_OFFSET_I4:
        case MintOpcode.MINT_STIND_OFFSET_IMM_I4:
        case MintOpcode.MINT_STIND_REF:
            getter = WasmOpcode.i32_load;
            break;
        case MintOpcode.MINT_LDIND_R4:
        case MintOpcode.MINT_STIND_R4:
            getter = WasmOpcode.f32_load;
            setter = WasmOpcode.f32_store;
            break;
        case MintOpcode.MINT_LDIND_R8:
        case MintOpcode.MINT_STIND_R8:
            getter = WasmOpcode.f64_load;
            setter = WasmOpcode.f64_store;
            break;
        case MintOpcode.MINT_LDIND_I8:
        case MintOpcode.MINT_LDIND_OFFSET_I8:
        case MintOpcode.MINT_LDIND_OFFSET_IMM_I8:
        case MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I8:
        case MintOpcode.MINT_STIND_I8:
        case MintOpcode.MINT_STIND_OFFSET_I8:
        case MintOpcode.MINT_STIND_OFFSET_IMM_I8:
            getter = WasmOpcode.i64_load;
            setter = WasmOpcode.i64_store;
            break;
        default:
            return false;
    }

    append_ldloc_cknull(builder, addressVarIndex, ip, false);

    if (isLoad) {
        // pre-load pLocals for the store operation
        builder.local("pLocals");
        // Load address
        builder.local("cknull_ptr");
        // For ldind_offset we need to load an offset from another local
        //  and then add it to the null checked address
        if (isAddMul) {
            // ptr = (char*)ptr + (LOCAL_VAR (ip [3], mono_i) + (gint16)ip [4]) * (gint16)ip [5];
            append_ldloc(builder, offsetVarIndex, WasmOpcode.i32_load);
            if (constantOffset !== 0) {
                builder.i32_const(constantOffset);
                builder.appendU8(WasmOpcode.i32_add);
                constantOffset = 0;
            }
            if (constantMultiplier !== 1) {
                builder.i32_const(constantMultiplier);
                builder.appendU8(WasmOpcode.i32_mul);
            }
            builder.appendU8(WasmOpcode.i32_add);
        } else if (isOffset && offsetVarIndex >= 0) {
            append_ldloc(builder, offsetVarIndex, WasmOpcode.i32_load);
            builder.appendU8(WasmOpcode.i32_add);
        } else if (constantOffset < 0) {
            // wasm memarg offsets are unsigned, so do a signed add
            builder.i32_const(constantOffset);
            builder.appendU8(WasmOpcode.i32_add);
            constantOffset = 0;
        }
        // Load value from loaded address
        builder.appendU8(getter);
        builder.appendMemarg(constantOffset, 0);

        append_stloc_tail(builder, valueVarIndex, setter);
    } else if (opcode === MintOpcode.MINT_STIND_REF) {
        // Load destination address
        builder.local("cknull_ptr");
        // Load address of value so that copy_managed_pointer can grab it
        append_ldloca(builder, valueVarIndex, 0);
        builder.callImport("copy_pointer");
    } else {
        // Pre-load address for the store operation
        builder.local("cknull_ptr");
        // For ldind_offset we need to load an offset from another local
        //  and then add it to the null checked address
        if (isOffset && offsetVarIndex >= 0) {
            append_ldloc(builder, offsetVarIndex, WasmOpcode.i32_load);
            builder.appendU8(WasmOpcode.i32_add);
        } else if (constantOffset < 0) {
            // wasm memarg offsets are unsigned, so do a signed add
            builder.i32_const(constantOffset);
            builder.appendU8(WasmOpcode.i32_add);
            constantOffset = 0;
        }
        // Load value and then write to address
        append_ldloc(builder, valueVarIndex, getter);
        builder.appendU8(setter);
        builder.appendMemarg(constantOffset, 0);
    }
    return true;
}

function append_getelema1 (
    builder: WasmBuilder, ip: MintOpcodePtr,
    objectOffset: number, indexOffset: number, elementSize: number
) {
    builder.block();

    // load index for check
    append_ldloc(builder, indexOffset, WasmOpcode.i32_load);
    // stash it since we need it twice
    builder.local("math_lhs32", WasmOpcode.tee_local);
    // array null check
    append_ldloc_cknull(builder, objectOffset, ip, true);
    // load array length
    builder.appendU8(WasmOpcode.i32_load);
    builder.appendMemarg(getMemberOffset(JiterpMember.ArrayLength), 2);
    // check index < array.length, unsigned. if index is negative it will be interpreted as
    //  a massive value which is naturally going to be bigger than array.length. interp.c
    //  exploits this property so we can too
    builder.appendU8(WasmOpcode.i32_lt_u);
    // bailout unless (index < array.length)
    builder.appendU8(WasmOpcode.br_if);
    builder.appendLeb(0);
    append_bailout(builder, ip, BailoutReason.ArrayLoadFailed);
    builder.endBlock();

    // We did a null check and bounds check so we can now compute the actual address
    builder.local("cknull_ptr");
    builder.i32_const(getMemberOffset(JiterpMember.ArrayData));
    builder.appendU8(WasmOpcode.i32_add);

    builder.local("math_lhs32");
    builder.i32_const(elementSize);
    builder.appendU8(WasmOpcode.i32_mul);
    builder.appendU8(WasmOpcode.i32_add);
    // append_getelema1 leaves the address on the stack
}

function emit_arrayop (builder: WasmBuilder, frame: NativePointer, ip: MintOpcodePtr, opcode: MintOpcode) : boolean {
    const isLoad = (
            (opcode <= MintOpcode.MINT_LDELEMA_TC) &&
            (opcode >= MintOpcode.MINT_LDELEM_I1)
        ) || (opcode === MintOpcode.MINT_LDLEN),
        objectOffset = getArgU16(ip, isLoad ? 2 : 1),
        valueOffset = getArgU16(ip, isLoad ? 1 : 3),
        indexOffset = getArgU16(ip, isLoad ? 3 : 2);

    let elementGetter: WasmOpcode,
        elementSetter = WasmOpcode.i32_store,
        elementSize: number;

    switch (opcode) {
        case MintOpcode.MINT_LDLEN: {
            builder.local("pLocals");
            // array null check
            append_ldloc_cknull(builder, objectOffset, ip, true);
            // load array length
            builder.appendU8(WasmOpcode.i32_load);
            builder.appendMemarg(getMemberOffset(JiterpMember.ArrayLength), 2);
            append_stloc_tail(builder, valueOffset, WasmOpcode.i32_store);
            return true;
        }
        case MintOpcode.MINT_LDELEMA1: {
            // Pre-load destination for the element address at the end
            builder.local("pLocals");

            elementSize = getArgU16(ip, 4);
            append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);

            append_stloc_tail(builder, valueOffset, WasmOpcode.i32_store);
            return true;
        }
        case MintOpcode.MINT_STELEM_REF: {
            builder.block();
            // array
            append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
            // index
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
            // value
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
            builder.callImport("stelem_ref");
            builder.appendU8(WasmOpcode.br_if);
            builder.appendLeb(0);
            append_bailout(builder, ip, BailoutReason.ArrayStoreFailed);
            builder.endBlock();
            return true;
        }
        case MintOpcode.MINT_LDELEM_REF:
            elementSize = 4;
            elementGetter = WasmOpcode.i32_load;
            break;
        case MintOpcode.MINT_LDELEM_I1:
            elementSize = 1;
            elementGetter = WasmOpcode.i32_load8_s;
            break;
        case MintOpcode.MINT_LDELEM_U1:
            elementSize = 1;
            elementGetter = WasmOpcode.i32_load8_u;
            break;
        case MintOpcode.MINT_STELEM_U1:
        case MintOpcode.MINT_STELEM_I1:
            elementSize = 1;
            elementGetter = WasmOpcode.i32_load;
            elementSetter = WasmOpcode.i32_store8;
            break;
        case MintOpcode.MINT_LDELEM_I2:
            elementSize = 2;
            elementGetter = WasmOpcode.i32_load16_s;
            break;
        case MintOpcode.MINT_LDELEM_U2:
            elementSize = 2;
            elementGetter = WasmOpcode.i32_load16_u;
            break;
        case MintOpcode.MINT_STELEM_U2:
        case MintOpcode.MINT_STELEM_I2:
            elementSize = 2;
            elementGetter = WasmOpcode.i32_load;
            elementSetter = WasmOpcode.i32_store16;
            break;
        case MintOpcode.MINT_LDELEM_U4:
        case MintOpcode.MINT_LDELEM_I4:
        case MintOpcode.MINT_STELEM_I4:
            elementSize = 4;
            elementGetter = WasmOpcode.i32_load;
            break;
        case MintOpcode.MINT_LDELEM_R4:
        case MintOpcode.MINT_STELEM_R4:
            elementSize = 4;
            elementGetter = WasmOpcode.f32_load;
            elementSetter = WasmOpcode.f32_store;
            break;
        case MintOpcode.MINT_LDELEM_I8:
        case MintOpcode.MINT_STELEM_I8:
            elementSize = 8;
            elementGetter = WasmOpcode.i64_load;
            elementSetter = WasmOpcode.i64_store;
            break;
        case MintOpcode.MINT_LDELEM_R8:
        case MintOpcode.MINT_STELEM_R8:
            elementSize = 8;
            elementGetter = WasmOpcode.f64_load;
            elementSetter = WasmOpcode.f64_store;
            break;
        case MintOpcode.MINT_LDELEM_VT: {
            const elementSize = getArgU16(ip, 4);
            // dest
            builder.local("pLocals");
            builder.i32_const(getArgU16(ip, 1));
            builder.appendU8(WasmOpcode.i32_add);
            // src
            append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);
            // memcpy (locals + ip [1], src_addr, size);
            append_memmove_dest_src(builder, elementSize);
            invalidate_local_range(getArgU16(ip, 1), elementSize);
            return true;
        }
        case MintOpcode.MINT_STELEM_VT: {
            const elementSize = getArgU16(ip, 5),
                klass = get_imethod_data(frame, getArgU16(ip, 4));
            // dest
            append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);
            // src
            append_ldloca(builder, valueOffset, 0);
            builder.ptr_const(klass);
            builder.callImport("value_copy");
            return true;
        }
        default:
            return false;
    }

    if (isLoad) {
        // Pre-load destination for the value at the end
        builder.local("pLocals");

        // Get address of the element, then load it
        append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);
        builder.appendU8(elementGetter);
        builder.appendMemarg(0, 0);

        append_stloc_tail(builder, valueOffset, elementSetter);
    } else {
        // Get address of the element first as our destination
        append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);
        append_ldloc(builder, valueOffset, elementGetter);

        builder.appendU8(elementSetter);
        builder.appendMemarg(0, 0);
    }
    return true;
}

function append_bailout (builder: WasmBuilder, ip: MintOpcodePtr, reason: BailoutReason) {
    builder.ip_const(ip);
    if (builder.options.countBailouts) {
        builder.i32_const(reason);
        builder.callImport("bailout");
    }
    builder.appendU8(WasmOpcode.return_);
}

function append_safepoint (builder: WasmBuilder, ip: MintOpcodePtr) {
    // Check whether a safepoint is required
    builder.ptr_const(cwraps.mono_jiterp_get_polling_required_address());
    builder.appendU8(WasmOpcode.i32_load);
    builder.appendMemarg(0, 2);
    // If the polling flag is set we call mono_jiterp_do_safepoint()
    builder.block(WasmValtype.void, WasmOpcode.if_);
    builder.local("frame");
    // Not ip_const, because we can't pass relative IP to do_safepoint
    builder.i32_const(ip);
    builder.callImport("safepoint");
    builder.endBlock();
}
