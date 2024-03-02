// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoMethod } from "./types/internal";
import { NativePointer } from "./types/emscripten";
import {
    getU16, getI16,
    getU32_unaligned, getI32_unaligned, getF32_unaligned, getF64_unaligned, localHeapViewU8,
} from "./memory";
import {
    WasmOpcode, WasmSimdOpcode, WasmValtype,
    getOpcodeName, MintOpArgType
} from "./jiterpreter-opcodes";
import {
    MintOpcode, SimdInfo,
    SimdIntrinsic2, SimdIntrinsic3, SimdIntrinsic4
} from "./mintops";
import cwraps from "./cwraps";
import {
    OpcodeInfoType, JiterpMember, BailoutReason,
    JiterpCounter,
} from "./jiterpreter-enums";
import {
    MintOpcodePtr, WasmBuilder,
    append_memset_dest, append_bailout, append_exit,
    append_memmove_dest_src, try_append_memset_fast,
    try_append_memmove_fast, getOpcodeTableValue,
    getMemberOffset, isZeroPageReserved, CfgBranchType,
    append_safepoint, modifyCounter, simdFallbackCounters,
} from "./jiterpreter-support";
import {
    sizeOfDataItem, sizeOfV128, sizeOfStackval,

    disabledOpcodes, countCallTargets,
    callTargetCounts,
    trace, traceOnError,
    emitPadding, traceBranchDisplacements,
    traceEip, nullCheckValidation,
    traceNullCheckOptimizations,
    nullCheckCaching, traceBackBranches,
    maxCallHandlerReturnAddresses,

    mostRecentOptions,

    record_abort,
} from "./jiterpreter";
import {
    ldcTable, OpRec3, OpRec4,
    floatToIntTable, unopTable,
    binopTable, intrinsicFpBinops,
    relopbranchTable, mathIntrinsicTable,
    simdCreateLoadOps, simdCreateSizes,
    simdCreateStoreOps, simdShiftTable,
    bitmaskTable, createScalarTable,
    simdExtractTable, simdReplaceTable,
    simdLoadTable, simdStoreTable,
} from "./jiterpreter-tables";
import { mono_log_error, mono_log_info } from "./logging";
import { mono_assert, runtimeHelpers } from "./globals";

// indexPlusOne so that ip[1] in the interpreter becomes getArgU16(ip, 1)
function getArgU16(ip: MintOpcodePtr, indexPlusOne: number) {
    return getU16(<any>ip + (2 * indexPlusOne));
}

function getArgI16(ip: MintOpcodePtr, indexPlusOne: number) {
    return getI16(<any>ip + (2 * indexPlusOne));
}

function getArgI32(ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getI32_unaligned(src);
}

function getArgF32(ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getF32_unaligned(src);
}

function getArgF64(ip: MintOpcodePtr, indexPlusOne: number) {
    const src = <any>ip + (2 * indexPlusOne);
    return getF64_unaligned(src);
}

function get_imethod(frame: NativePointer) {
    // FIXME: Encoding this data directly into the trace will prevent trace reuse
    const iMethod = getU32_unaligned(<any>frame + getMemberOffset(JiterpMember.Imethod));
    return iMethod;
}

function get_imethod_data(frame: NativePointer, index: number) {
    // FIXME: Encoding this data directly into the trace will prevent trace reuse
    const pData = getU32_unaligned(get_imethod(frame) + getMemberOffset(JiterpMember.DataItems));
    const dataOffset = pData + (index * sizeOfDataItem);
    return getU32_unaligned(dataOffset);
}

function get_imethod_clause_data_offset(frame: NativePointer, index: number) {
    // FIXME: Encoding this data directly into the trace will prevent trace reuse
    const pData = getU32_unaligned(get_imethod(frame) + getMemberOffset(JiterpMember.ClauseDataOffsets));
    const dataOffset = pData + (index * sizeOfDataItem);
    return getU32_unaligned(dataOffset);
}

function is_backward_branch_target(
    ip: MintOpcodePtr, startOfBody: MintOpcodePtr,
    backwardBranchTable: Uint16Array | null
) {
    if (!backwardBranchTable)
        return false;

    // TODO: sort the table and exploit that for faster scan. Not important yet
    for (let i = 0; i < backwardBranchTable.length; i++) {
        const actualOffset = (backwardBranchTable[i] * 2) + <any>startOfBody;
        if (actualOffset === ip)
            return true;
    }

    return false;
}

type KnownConstantValue = number | Uint8Array;
const knownConstantValues = new Map<number, KnownConstantValue>();

function get_known_constant_value(builder: WasmBuilder, localOffset: number): KnownConstantValue | undefined {
    if (isAddressTaken(builder, localOffset))
        return undefined;

    return knownConstantValues.get(localOffset);
}

// Perform a quick scan through the opcodes potentially in this trace to build a table of
//  backwards branch targets, compatible with the layout of the old one that was generated in C.
// We do this here to match the exact way that the jiterp calculates branch targets, since
//  there were previously corner cases where jiterp and interp disagreed.
export function generateBackwardBranchTable(
    ip: MintOpcodePtr, startOfBody: MintOpcodePtr, sizeOfBody: MintOpcodePtr,
): Uint16Array | null {
    const endOfBody = <any>startOfBody + <any>sizeOfBody;
    // TODO: Cache this table object instance and reuse it to reduce gc pressure?
    const table : number[] = [];
    // IP of the start of the trace in U16s, relative to startOfBody.
    const rbase16 = (<any>ip - <any>startOfBody) / 2;

    // FIXME: This will potentially scan the entire method and record branches that won't
    //  ever run since the trace compilation will end before we reach them.
    while (ip < endOfBody) {
        // IP of the current opcode in U16s, relative to startOfBody. This is what the back branch table uses
        const rip16 = (<any>ip - <any>startOfBody) / 2;
        const opcode = <MintOpcode>getU16(ip);
        // HACK
        if (opcode === MintOpcode.MINT_SWITCH)
            break;

        const opLengthU16 = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Length);
        // Any opcode with a branch argtype will have a decoded displacement, even if we don't
        //  implement the opcode. Everything else will return undefined here and be skipped
        const displacement = getBranchDisplacement(ip, opcode);
        if (typeof (displacement) !== "number") {
            ip += <any>(opLengthU16 * 2);
            continue;
        }

        // These checks shouldn't fail unless memory is corrupted or something is wrong with the decoder.
        // We don't want to cause decoder bugs to make the application exit, though - graceful degradation.
        if (displacement === 0) {
            mono_log_info(`opcode @${ip} branch target is self. aborting backbranch table generation`);
            break;
        }

        // Only record *backward* branches
        // We will filter this down further in the Cfg because it takes note of which branches it sees,
        //  but it is also beneficial to have a null table (further down) due to seeing no potential
        //  back branch targets at all, as it allows the Cfg to skip additional code generation entirely
        //  if it knows there will never be any backwards branches in a given trace
        if (displacement < 0) {
            const rtarget16 = rip16 + (displacement);
            if (rtarget16 < 0) {
                mono_log_info(`opcode @${ip}'s displacement of ${displacement} goes before body: ${rtarget16}. aborting backbranch table generation`);
                break;
            }

            // If the relative target is before the start of the trace, don't record it.
            // The trace will be unable to successfully branch to it so it would just make the table bigger.
            if (rtarget16 >= rbase16)
                table.push(rtarget16);
        }

        switch (opcode) {
            case MintOpcode.MINT_CALL_HANDLER:
            case MintOpcode.MINT_CALL_HANDLER_S:
                // While this formally isn't a backward branch target, we want to record
                //  the offset of its following instruction so that the jiterpreter knows
                //  to generate the necessary dispatch code to enable branching back to it.
                table.push(rip16 + opLengthU16);
                break;
        }

        ip += <any>(opLengthU16 * 2);
    }

    if (table.length <= 0)
        return null;
    // Not important yet, so not doing it
    // table.sort((a, b) => a - b);
    return new Uint16Array(table);
}

export function generateWasmBody(
    frame: NativePointer, traceName: string, ip: MintOpcodePtr,
    startOfBody: MintOpcodePtr, endOfBody: MintOpcodePtr,
    builder: WasmBuilder, instrumentedTraceId: number,
    backwardBranchTable: Uint16Array | null
): number {
    const abort = <MintOpcodePtr><any>0;
    let isFirstInstruction = true, isConditionallyExecuted = false,
        containsSimd = false,
        pruneOpcodes = false, hasEmittedUnreachable = false;
    let result = 0,
        prologueOpcodeCounter = 0,
        conditionalOpcodeCounter = 0;
    eraseInferredState();

    // Skip over the enter opcode
    const enterSizeU16 = cwraps.mono_jiterp_get_opcode_info(MintOpcode.MINT_TIER_ENTER_JITERPRETER, OpcodeInfoType.Length);
    ip += <any>(enterSizeU16 * 2);
    let rip = ip;

    builder.cfg.entry(ip);

    while (ip) {
        // This means some code went 'ip = abort; continue'
        if (!ip)
            break;

        builder.cfg.ip = ip;

        if (ip >= endOfBody) {
            record_abort(builder.traceIndex, ip, traceName, "end-of-body");
            if (instrumentedTraceId)
                mono_log_info(`instrumented trace ${traceName} exited at end of body @${(<any>ip).toString(16)}`);
            break;
        }

        // HACK: Browsers set a limit of 4KB, we lower it slightly since a single opcode
        //  might generate a ton of code and we generate a bit of an epilogue after
        //  we finish
        const maxBytesGenerated = 3840,
            spaceLeft = maxBytesGenerated - builder.bytesGeneratedSoFar - builder.cfg.overheadBytes;
        if (builder.size >= spaceLeft) {
            // mono_log_info(`trace too big, estimated size is ${builder.size + builder.bytesGeneratedSoFar}`);
            record_abort(builder.traceIndex, ip, traceName, "trace-too-big");
            if (instrumentedTraceId)
                mono_log_info(`instrumented trace ${traceName} exited because of size limit at @${(<any>ip).toString(16)} (spaceLeft=${spaceLeft}b)`);
            break;
        }

        if (instrumentedTraceId && traceEip) {
            builder.i32_const(instrumentedTraceId);
            builder.ip_const(ip);
            builder.callImport("trace_eip");
        }

        let opcode = getU16(ip);
        const numSregs = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Sregs),
            numDregs = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Dregs),
            opLengthU16 = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Length);

        const isSimdIntrins = (opcode >= MintOpcode.MINT_SIMD_INTRINS_P_P) &&
            (opcode <= MintOpcode.MINT_SIMD_INTRINS_P_PPP);
        const simdIntrinsArgCount = isSimdIntrins
            ? opcode - MintOpcode.MINT_SIMD_INTRINS_P_P + 2
            : 0;
        const simdIntrinsIndex = isSimdIntrins
            ? getArgU16(ip, 1 + simdIntrinsArgCount)
            : 0;

        mono_assert((opcode >= 0) && (opcode < MintOpcode.MINT_LASTOP), () => `invalid opcode ${opcode}`);

        const opname = isSimdIntrins
            ? SimdInfo[simdIntrinsArgCount][simdIntrinsIndex]
            : getOpcodeName(opcode);
        const _ip = ip;
        const isBackBranchTarget = builder.options.noExitBackwardBranches &&
            is_backward_branch_target(ip, startOfBody, backwardBranchTable),
            isForwardBranchTarget = builder.branchTargets.has(ip),
            startBranchBlock = isBackBranchTarget || isForwardBranchTarget ||
                // If a method contains backward branches, we also need to check eip at the first insn
                //  because a backward branch might target a point in the middle of the trace
                (isFirstInstruction && backwardBranchTable),
            // We want to approximate the number of unconditionally executed instructions along with
            //  the ones that were probably conditionally executed by the time we reached the exit point
            // We don't know the exact path that would have taken us to a given point, but it's a reasonable
            //  guess that methods dense with branches are more likely to take a complex path to reach
            //  a given exit
            exitOpcodeCounter = conditionalOpcodeCounter + prologueOpcodeCounter +
                builder.branchTargets.size;
        let skipDregInvalidation = false,
            opcodeValue = getOpcodeTableValue(opcode);

        // We record the offset of each backward branch we encounter, so that later branch
        //  opcodes know that it's available by branching to the top of the dispatch loop
        if (isBackBranchTarget) {
            if (traceBackBranches > 1)
                mono_log_info(`${traceName} recording back branch target 0x${(<any>ip).toString(16)}`);
            builder.backBranchOffsets.push(ip);
        }

        if (startBranchBlock) {
            // We've reached a branch target so we need to stop pruning opcodes, since
            //  we are no longer in a dead zone that execution can't reach
            pruneOpcodes = false;
            hasEmittedUnreachable = false;
            // If execution runs past the end of the current branch block, ensure
            //  that the instruction pointer is updated appropriately. This will
            //  also guarantee that the branch target block's comparison will
            //  succeed so that execution continues.
            // We make sure above that this isn't done for the start of the trace,
            //  otherwise loops will run forever and never terminate since after
            //  branching to the top of the loop we would blow away eip
            append_branch_target_block(builder, ip, isBackBranchTarget);
            isConditionallyExecuted = true;
            eraseInferredState();
            // Monitoring wants an opcode count that is a measurement of how many opcodes
            //  we definitely executed, so we want to ignore any opcodes that might
            //  have been skipped due to forward branching. This gives us an approximation
            //  of that by only counting how far we are from the most recent branch target
            conditionalOpcodeCounter = 0;
        }

        // Handle the _OUTSIDE_BRANCH_BLOCK table entries
        if ((opcodeValue < -1) && isConditionallyExecuted)
            opcodeValue = (opcodeValue === -2) ? 2 : 0;

        isFirstInstruction = false;

        if (opcode === MintOpcode.MINT_SWITCH) {
            // HACK: This opcode breaks all our table-based parsing and will cause the trace compiler to hang
            //  if it encounters a switch inside of a pruning region, so we need to let the normal code path
            //  run even if pruning is on
        } else if (disabledOpcodes.indexOf(opcode) >= 0) {
            append_bailout(builder, ip, BailoutReason.Debugging);
            opcode = MintOpcode.MINT_NOP;
            // Intentionally leave the correct info in place so we skip the right number of bytes
        } else if (pruneOpcodes) {
            opcode = MintOpcode.MINT_NOP;
        }

        switch (opcode) {
            case MintOpcode.MINT_NOP: {
                // This typically means the current opcode was disabled or pruned
                if (pruneOpcodes) {
                    // We emit an unreachable opcode so that if execution somehow reaches a pruned opcode, we will abort
                    // This should be impossible anyway but it's also useful to have pruning visible in the wasm
                    // FIXME: Ideally we would stop generating opcodes after the first unreachable, but that causes v8 to hang
                    if (!hasEmittedUnreachable)
                        builder.appendU8(WasmOpcode.unreachable);
                    // Each unreachable opcode could generate a bunch of native code in a bad wasm jit so generate nops after it
                    hasEmittedUnreachable = true;
                }
                break;
            }
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
            case MintOpcode.MINT_ZEROBLK: {
                // dest
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                // value
                builder.i32_const(0);
                // count
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                // memset
                builder.appendU8(WasmOpcode.PREFIX_sat);
                builder.appendU8(11);
                builder.appendU8(0);
                break;
            }
            case MintOpcode.MINT_ZEROBLK_IMM: {
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_memset_dest(builder, 0, getArgU16(ip, 2));
                break;
            }
            case MintOpcode.MINT_CPBLK: {
                const sizeOffset = getArgU16(ip, 3),
                    srcOffset = getArgU16(ip, 2),
                    destOffset = getArgU16(ip, 1),
                    constantSize = get_known_constant_value(builder, sizeOffset);

                if (constantSize !== 0) {
                    if (typeof (constantSize) !== "number") {
                        // size (FIXME: uint32 not int32)
                        append_ldloc(builder, sizeOffset, WasmOpcode.i32_load);
                        builder.local("count", WasmOpcode.tee_local);
                        // if size is 0 then don't do anything
                        builder.block(WasmValtype.void, WasmOpcode.if_); // if size
                    } else {
                        // Store the count into the local in case the unroll fails
                        builder.i32_const(constantSize);
                        builder.local("count", WasmOpcode.set_local);
                    }

                    // stash dest then check for null
                    append_ldloc(builder, destOffset, WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    builder.appendU8(WasmOpcode.i32_eqz);
                    // stash src then check for null
                    append_ldloc(builder, srcOffset, WasmOpcode.i32_load);
                    builder.local("src_ptr", WasmOpcode.tee_local);
                    builder.appendU8(WasmOpcode.i32_eqz);

                    // now we memmove if both dest and src are valid. The stack currently has
                    //  the eqz result for each pointer so we can stash a bailout inside of an if
                    builder.appendU8(WasmOpcode.i32_or);
                    builder.block(WasmValtype.void, WasmOpcode.if_); // if null
                    append_bailout(builder, ip, BailoutReason.NullCheck);
                    builder.endBlock(); // if null

                    if (
                        (typeof (constantSize) !== "number") ||
                        !try_append_memmove_fast(builder, 0, 0, constantSize, false, "dest_ptr", "src_ptr")
                    ) {
                        // We passed the null check so now prepare the stack
                        builder.local("dest_ptr");
                        builder.local("src_ptr");
                        builder.local("count");
                        // wasm memmove with stack layout dest, src, count
                        builder.appendU8(WasmOpcode.PREFIX_sat);
                        builder.appendU8(10);
                        builder.appendU8(0);
                        builder.appendU8(0);
                    }

                    if (typeof (constantSize) !== "number")
                        builder.endBlock(); // if size
                }
                break;
            }
            case MintOpcode.MINT_INITBLK: {
                const sizeOffset = getArgU16(ip, 3),
                    valueOffset = getArgU16(ip, 2),
                    destOffset = getArgU16(ip, 1);

                // TODO: Handle constant size initblks. Not sure if they matter though
                // FIXME: This will cause an erroneous bailout if dest and size are both 0
                //  but that really shouldn't ever happen, and it will only cause a slowdown
                // dest
                append_ldloc_cknull(builder, destOffset, ip, true);
                // value
                append_ldloc(builder, valueOffset, WasmOpcode.i32_load);
                // size (FIXME: uint32 not int32)
                append_ldloc(builder, sizeOffset, WasmOpcode.i32_load);
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
                if (!emit_branch(builder, ip, frame, opcode))
                    ip = abort;
                else
                    isConditionallyExecuted = true;
                break;

            case MintOpcode.MINT_BR_S:
            case MintOpcode.MINT_CALL_HANDLER:
            case MintOpcode.MINT_CALL_HANDLER_S:
                if (!emit_branch(builder, ip, frame, opcode))
                    ip = abort;
                else {
                    // Technically incorrect, but the instructions following this one may not be executed
                    //  since we might have skipped over them.
                    // FIXME: Identify when we should actually set the conditionally executed flag, perhaps
                    //  by doing a simple static flow analysis based on the displacements. Update heuristic too!
                    isConditionallyExecuted = true;
                }
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
                        mono_log_info(`(0x${(<any>ip).toString(16)}) locals[${dest}] passed cknull`);
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
                opcodeValue = 0;
                break;

            case MintOpcode.MINT_SAFEPOINT:
                append_safepoint(builder, ip);
                break;

            case MintOpcode.MINT_LDLOCA_S: {
                // Pre-load locals for the store op
                builder.local("pLocals");
                // locals[ip[1]] = &locals[ip[2]]
                const offset = getArgU16(ip, 2),
                    flag = isAddressTaken(builder, offset);
                if (!flag)
                    mono_log_error(`${traceName}: Expected local ${offset} to have address taken flag`);
                append_ldloca(builder, offset);
                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

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
                append_ldloca(builder, getArgU16(ip, 1), size);
                append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                append_memmove_dest_src(builder, size);
                break;
            }
            case MintOpcode.MINT_STOBJ_VT: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3));
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.ptr_const(klass);
                builder.callImport("value_copy");
                break;
            }
            case MintOpcode.MINT_STOBJ_VT_NOREF: {
                const sizeBytes = getArgU16(ip, 3);
                append_ldloc(builder, getArgU16(ip, 1), WasmOpcode.i32_load);
                append_ldloca(builder, getArgU16(ip, 2), 0);
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
                builder.local("index", WasmOpcode.tee_local);

                /*
                const constantIndex = get_known_constant_value(getArgU16(ip, 3));
                if (typeof (constantIndex) === "number")
                    console.log(`getchr in ${builder.functions[0].name} with constant index ${constantIndex}`);
                */

                // str
                let ptrLocal = "cknull_ptr";
                if (builder.options.zeroPageOptimization && isZeroPageReserved()) {
                    // load string ptr and stash it
                    // if the string ptr is null, the length check will fail and we will bail out,
                    //  so the null check is not necessary
                    modifyCounter(JiterpCounter.NullChecksFused, 1);
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    ptrLocal = "src_ptr";
                    builder.local(ptrLocal, WasmOpcode.tee_local);
                } else
                    append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);

                // current stack layout is [index, ptr]
                // get string length
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.StringLength), 2);
                // current stack layout is [index, length]
                // index < length
                builder.appendU8(WasmOpcode.i32_lt_s);
                // index >= 0
                builder.local("index");
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
                builder.local("index");
                builder.i32_const(2);
                builder.appendU8(WasmOpcode.i32_mul);
                builder.local(ptrLocal);
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
                // Load index and stash it
                append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
                builder.local("index", WasmOpcode.tee_local);

                // Load address of the span structure
                let ptrLocal = "cknull_ptr";
                if (opcode === MintOpcode.MINT_GETITEM_SPAN) {
                    // span = *(MonoSpanOfVoid *)locals[2]
                    append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                } else {
                    // span = (MonoSpanOfVoid)locals[2]
                    append_ldloca(builder, getArgU16(ip, 2), 0);
                    ptrLocal = "src_ptr";
                    builder.local(ptrLocal, WasmOpcode.tee_local);
                }

                // length = span->length
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.SpanLength), 2);
                // index < length
                builder.appendU8(WasmOpcode.i32_lt_u);
                // index >= 0
                // FIXME: It would be nice to optimize this down to a single (index < length) comparison
                //  but interp.c doesn't do it - presumably because a span could be bigger than 2gb?
                builder.local("index");
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
                builder.local(ptrLocal);
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(getMemberOffset(JiterpMember.SpanData), 2);

                builder.local("index");
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
                builder.local("count", WasmOpcode.tee_local);
                builder.i32_const(0);
                builder.appendU8(WasmOpcode.i32_ge_s);
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.SpanOperationFailed);
                builder.endBlock();
                // gpointer span = locals + ip [1];
                append_ldloca(builder, getArgU16(ip, 1), 16);
                builder.local("dest_ptr", WasmOpcode.tee_local);
                // *(gpointer*)span = ptr;
                append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                builder.appendU8(WasmOpcode.i32_store);
                builder.appendMemarg(0, 0);
                // *(gint32*)((gpointer*)span + 1) = len;
                builder.local("dest_ptr");
                builder.local("count");
                builder.appendU8(WasmOpcode.i32_store);
                builder.appendMemarg(4, 0);
                break;
            }

            case MintOpcode.MINT_LD_DELEGATE_METHOD_PTR: {
                // FIXME: ldloca invalidation size
                append_ldloca(builder, getArgU16(ip, 1), 8);
                append_ldloca(builder, getArgU16(ip, 2), 8);
                builder.callImport("ld_del_ptr");
                break;
            }
            case MintOpcode.MINT_LDTSFLDA: {
                append_ldloca(builder, getArgU16(ip, 1), 4);
                // This value is unsigned but I32 is probably right
                builder.ptr_const(getArgI32(ip, 2));
                builder.callImport("ldtsflda");
                break;
            }
            case MintOpcode.MINT_INTRINS_GET_TYPE:
                builder.block();
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4);
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
                append_ldloca(builder, getArgU16(ip, 1), 4);
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
                append_ldloca(builder, getArgU16(ip, 1), 4);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.callImport(opcode === MintOpcode.MINT_ARRAY_RANK ? "array_rank" : "a_elesize");
                // If the array was null we will bail out, otherwise continue
                builder.appendU8(WasmOpcode.br_if);
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock();
                break;
            }

            case MintOpcode.MINT_CASTCLASS_INTERFACE:
            case MintOpcode.MINT_ISINST_INTERFACE: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3)),
                    isSpecialInterface = cwraps.mono_jiterp_is_special_interface(klass),
                    bailoutOnFailure = (opcode === MintOpcode.MINT_CASTCLASS_INTERFACE),
                    destOffset = getArgU16(ip, 1);
                if (!klass) {
                    record_abort(builder.traceIndex, ip, traceName, "null-klass");
                    ip = abort;
                    continue;
                }

                builder.block(); // depth x -> 0 (opcode block)

                if (builder.options.zeroPageOptimization && isZeroPageReserved()) {
                    // Null check fusion is possible, so (obj->vtable) will be 0 for !obj
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    modifyCounter(JiterpCounter.NullChecksFused, 1);
                } else {
                    builder.block(); // depth 0 -> 1 (null check block)
                    // src
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    // Null ptr check: If the ptr is non-null, skip this block
                    builder.appendU8(WasmOpcode.br_if);
                    builder.appendULeb(0);
                    builder.local("pLocals");
                    builder.i32_const(0);
                    append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                    // at the end of this block (depth 0) we skip to the end of the opcode block (depth 1)
                    //  because we successfully zeroed the destination register
                    builder.appendU8(WasmOpcode.br);
                    builder.appendULeb(1);
                    builder.endBlock(); // depth 1 -> 0 (end null check block)
                    // Put ptr back on the stack
                    builder.local("dest_ptr");
                }

                // the special interface version signature is (obj, vtable, klass), but
                //  the fast signature is (vtable, klass)
                if (isSpecialInterface) {
                    // load a second copy of obj to build the helper arglist (obj, vtable, klass)
                    builder.local("dest_ptr");
                }

                builder.appendU8(WasmOpcode.i32_load); // obj->vtable
                builder.appendMemarg(getMemberOffset(JiterpMember.VTable), 0); // fixme: alignment

                builder.ptr_const(klass);
                builder.callImport(isSpecialInterface ? "imp_iface_s" : "imp_iface");

                if (bailoutOnFailure) {
                    // generate a 1 for null ptrs so we don't bail out and instead write the 0
                    //  to the destination
                    builder.local("dest_ptr");
                    builder.appendU8(WasmOpcode.i32_eqz);
                    builder.appendU8(WasmOpcode.i32_or);
                }

                builder.block(WasmValtype.void, WasmOpcode.if_); // if cast succeeded
                builder.local("pLocals");
                builder.local("dest_ptr");
                append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                builder.appendU8(WasmOpcode.else_); // else cast failed
                if (bailoutOnFailure) {
                    // so bailout
                    append_bailout(builder, ip, BailoutReason.CastFailed);
                } else {
                    // this is isinst, so write 0 to destination instead
                    builder.local("pLocals");
                    builder.i32_const(0);
                    append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                }
                builder.endBlock(); // endif

                builder.endBlock(); // depth 0 -> x (end opcode block)

                break;
            }

            case MintOpcode.MINT_CASTCLASS_COMMON:
            case MintOpcode.MINT_ISINST_COMMON:
            case MintOpcode.MINT_CASTCLASS:
            case MintOpcode.MINT_ISINST: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3)),
                    canDoFastCheck = (opcode === MintOpcode.MINT_CASTCLASS_COMMON) ||
                        (opcode === MintOpcode.MINT_ISINST_COMMON),
                    bailoutOnFailure = (opcode === MintOpcode.MINT_CASTCLASS) ||
                        (opcode === MintOpcode.MINT_CASTCLASS_COMMON),
                    destOffset = getArgU16(ip, 1);
                if (!klass) {
                    record_abort(builder.traceIndex, ip, traceName, "null-klass");
                    ip = abort;
                    continue;
                }

                builder.block(); // depth x -> 0 (opcode block)

                if (builder.options.zeroPageOptimization && isZeroPageReserved()) {
                    // Null check fusion is possible, so (obj->vtable)->klass will be 0 for !obj
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    modifyCounter(JiterpCounter.NullChecksFused, 1);
                } else {
                    builder.block(); // depth 0 -> 1 (null check block)
                    // src
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    // Null ptr check: If the ptr is non-null, skip this block
                    builder.appendU8(WasmOpcode.br_if);
                    builder.appendULeb(0);
                    builder.local("pLocals");
                    builder.i32_const(0);
                    append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                    // at the end of this block (depth 0) we skip to the end of the opcode block (depth 1)
                    //  because we successfully zeroed the destination register
                    builder.appendU8(WasmOpcode.br);
                    builder.appendULeb(1);
                    builder.endBlock(); // depth 1 -> 0 (end null check block)
                    // Put ptr back on the stack
                    builder.local("dest_ptr");
                }

                // If we're here the null check passed and we now need to type-check
                builder.appendU8(WasmOpcode.i32_load); // obj->vtable
                builder.appendMemarg(getMemberOffset(JiterpMember.VTable), 0); // fixme: alignment
                builder.appendU8(WasmOpcode.i32_load); // (obj->vtable)->klass
                builder.appendMemarg(getMemberOffset(JiterpMember.VTableKlass), 0); // fixme: alignment
                // Stash obj->vtable->klass so we can do a fast has_parent check later
                if (canDoFastCheck)
                    builder.local("src_ptr", WasmOpcode.tee_local);
                builder.i32_const(klass);
                builder.appendU8(WasmOpcode.i32_eq);
                builder.block(WasmValtype.void, WasmOpcode.if_); // if A

                // Fast type-check passed (exact match), so store the ptr and continue
                builder.local("pLocals");
                builder.local("dest_ptr");
                append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);

                // Fast type-check failed, so call the helper function
                builder.appendU8(WasmOpcode.else_); // else A

                if (canDoFastCheck) {
                    // Fast path for ISINST_COMMON/CASTCLASS_COMMON. We know klass is a simple type
                    //  so all we need to do is a parentage check.
                    builder.local("src_ptr"); // obj->vtable->klass
                    builder.ptr_const(klass);
                    builder.callImport("hasparent");

                    if (bailoutOnFailure) {
                        // generate a 1 for null ptrs so we don't bail out and instead write the 0
                        //  to the destination
                        builder.local("dest_ptr");
                        builder.appendU8(WasmOpcode.i32_eqz);
                        builder.appendU8(WasmOpcode.i32_or);
                    }

                    builder.block(WasmValtype.void, WasmOpcode.if_); // if B
                    // mono_class_has_parent_fast returned 1 so *destination = obj
                    builder.local("pLocals");
                    builder.local("dest_ptr");
                    append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                    builder.appendU8(WasmOpcode.else_); // else B
                    // mono_class_has_parent_fast returned 0
                    if (bailoutOnFailure) {
                        // so bailout
                        append_bailout(builder, ip, BailoutReason.CastFailed);
                    } else {
                        // this is isinst, so write 0 to destination instead
                        builder.local("pLocals");
                        builder.i32_const(0);
                        append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);
                    }
                    builder.endBlock(); // endif B
                } else {
                    // Slow path for ISINST/CASTCLASS, handles things like generics and nullable.
                    // &dest
                    append_ldloca(builder, getArgU16(ip, 1), 4);
                    // src
                    builder.local("dest_ptr");
                    // klass
                    builder.ptr_const(klass);
                    // opcode
                    builder.i32_const(opcode);
                    builder.callImport("castv2");

                    // We don't need to do an explicit null check because mono_jiterp_cast_v2 does it

                    // Check whether the cast operation failed
                    builder.appendU8(WasmOpcode.i32_eqz);
                    builder.block(WasmValtype.void, WasmOpcode.if_); // if B
                    // Cast failed so bail out
                    append_bailout(builder, ip, BailoutReason.CastFailed);
                    builder.endBlock(); // endif B
                }

                builder.endBlock(); // endif A

                builder.endBlock(); // depth 0 -> x (end opcode block)

                break;
            }

            case MintOpcode.MINT_BOX:
            case MintOpcode.MINT_BOX_VT: {
                // MonoVTable *vtable = (MonoVTable*)frame->imethod->data_items [ip [3]];
                builder.ptr_const(get_imethod_data(frame, getArgU16(ip, 3)));
                // dest, src
                append_ldloca(builder, getArgU16(ip, 1), 4);
                append_ldloca(builder, getArgU16(ip, 2), 0);
                builder.i32_const(opcode === MintOpcode.MINT_BOX_VT ? 1 : 0);
                builder.callImport("box");
                break;
            }

            case MintOpcode.MINT_UNBOX: {
                const klass = get_imethod_data(frame, getArgU16(ip, 3)),
                    // The type check needs to examine the boxed value's rank and element class
                    elementClassOffset = getMemberOffset(JiterpMember.ClassElementClass),
                    destOffset = getArgU16(ip, 1),
                    // Get the class's element class, which is what we will actually type-check against
                    elementClass = getU32_unaligned(klass + elementClassOffset);

                if (!klass || !elementClass) {
                    record_abort(builder.traceIndex, ip, traceName, "null-klass");
                    ip = abort;
                    continue;
                }

                if (builder.options.zeroPageOptimization && isZeroPageReserved()) {
                    // Null check fusion is possible, so (obj->vtable)->klass will be 0 for !obj
                    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                    modifyCounter(JiterpCounter.NullChecksFused, 1);
                } else {
                    append_ldloc_cknull(builder, getArgU16(ip, 2), ip, true);
                    builder.local("dest_ptr", WasmOpcode.tee_local);
                }

                // Fetch the object's klass so we can perform a type check
                builder.appendU8(WasmOpcode.i32_load); // obj->vtable
                builder.appendMemarg(getMemberOffset(JiterpMember.VTable), 0); // fixme: alignment
                builder.appendU8(WasmOpcode.i32_load); // (obj->vtable)->klass
                builder.appendMemarg(getMemberOffset(JiterpMember.VTableKlass), 0); // fixme: alignment

                // Stash obj->vtable->klass, then check klass->element_class == expected
                builder.local("src_ptr", WasmOpcode.tee_local);
                builder.appendU8(WasmOpcode.i32_load);
                builder.appendMemarg(elementClassOffset, 0);
                builder.i32_const(elementClass);
                builder.appendU8(WasmOpcode.i32_eq);

                // Check klass->rank == 0
                builder.local("src_ptr");
                builder.appendU8(WasmOpcode.i32_load8_u); // rank is a uint8
                builder.appendMemarg(getMemberOffset(JiterpMember.ClassRank), 0);
                builder.appendU8(WasmOpcode.i32_eqz);

                // (element_class == expected) && (rank == 0)
                builder.appendU8(WasmOpcode.i32_and);

                builder.block(WasmValtype.void, WasmOpcode.if_); // if type check passed

                // Type-check passed, so now compute the address of the object's data
                //  and store the address
                builder.local("pLocals");
                builder.local("dest_ptr");
                builder.i32_const(getMemberOffset(JiterpMember.BoxedValueData));
                builder.appendU8(WasmOpcode.i32_add);
                append_stloc_tail(builder, destOffset, WasmOpcode.i32_store);

                builder.appendU8(WasmOpcode.else_); // else type check failed

                //
                append_bailout(builder, ip, BailoutReason.UnboxFailed);

                builder.endBlock(); // endif A

                break;
            }

            case MintOpcode.MINT_NEWSTR: {
                builder.block();
                append_ldloca(builder, getArgU16(ip, 1), 4);
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
                append_ldloca(builder, getArgU16(ip, 1), 4);
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
                if (isConditionallyExecuted) {
                    // We generate a bailout instead of aborting, because we don't want calls
                    //  to abort the entire trace if we have branch support enabled - the call
                    //  might be infrequently hit and as a result it's worth it to keep going.
                    append_exit(builder, ip, exitOpcodeCounter, BailoutReason.Call);
                    pruneOpcodes = true;
                    opcodeValue = 0;
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
                if (isConditionallyExecuted) {
                    append_exit(builder, ip, exitOpcodeCounter,
                        opcode == MintOpcode.MINT_CALL_DELEGATE
                            ? BailoutReason.CallDelegate
                            : BailoutReason.Call
                    );
                    pruneOpcodes = true;
                } else {
                    ip = abort;
                }
                break;

            // Unlike regular rethrow which will only appear in catch blocks,
            //  MONO_RETHROW appears to show up in other places, so it's worth conditional bailout
            case MintOpcode.MINT_MONO_RETHROW:
            case MintOpcode.MINT_THROW:
                // Not an exit, because throws are by definition unlikely
                // We shouldn't make optimization decisions based on them.
                append_bailout(builder, ip, BailoutReason.Throw);
                pruneOpcodes = true;
                break;

            // These are generated in place of regular LEAVEs inside of the body of a catch clause.
            // We can safely assume that during normal execution, catch clauses won't be running.
            case MintOpcode.MINT_LEAVE_CHECK:
            case MintOpcode.MINT_LEAVE_S_CHECK:
                append_bailout(builder, ip, BailoutReason.LeaveCheck);
                pruneOpcodes = true;
                break;

            case MintOpcode.MINT_ENDFINALLY: {
                if (
                    (builder.callHandlerReturnAddresses.length > 0) &&
                    (builder.callHandlerReturnAddresses.length <= maxCallHandlerReturnAddresses)
                ) {
                    // mono_log_info(`endfinally @0x${(<any>ip).toString(16)}. return addresses:`, builder.callHandlerReturnAddresses.map(ra => (<any>ra).toString(16)));
                    // FIXME: Clean this codegen up
                    // Load ret_ip
                    const clauseIndex = getArgU16(ip, 1),
                        clauseDataOffset = get_imethod_clause_data_offset(frame, clauseIndex);
                    builder.local("pLocals");
                    builder.appendU8(WasmOpcode.i32_load);
                    builder.appendMemarg(clauseDataOffset, 0);
                    // Stash it in a variable because we're going to need to use it multiple times
                    builder.local("index", WasmOpcode.set_local);
                    // Do a bunch of trivial comparisons to see if ret_ip is one of our expected return addresses,
                    //  and if it is, generate a branch back to the dispatcher at the top
                    for (let r = 0; r < builder.callHandlerReturnAddresses.length; r++) {
                        const ra = builder.callHandlerReturnAddresses[r];
                        builder.local("index");
                        builder.ptr_const(ra);
                        builder.appendU8(WasmOpcode.i32_eq);
                        builder.cfg.branch(ra, ra < ip, CfgBranchType.Conditional);
                    }
                    // If none of the comparisons succeeded we won't have branched anywhere, so bail out
                    // This shouldn't happen during non-exception-handling execution unless the trace doesn't
                    //  contain the CALL_HANDLER that led here
                    append_bailout(builder, ip, BailoutReason.UnexpectedRetIp);
                    // FIXME: prune opcodes?
                } else {
                    ip = abort;
                }
                break;
            }

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
                append_ldloca(builder, getArgU16(ip, 1), 8);
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
                append_ldloca(builder, getArgU16(ip, 1), 8); // oldVal
                builder.callImport("cmpxchg_i64");
                break;

            case MintOpcode.MINT_LOG2_I4:
            case MintOpcode.MINT_LOG2_I8: {
                const isI64 = (opcode === MintOpcode.MINT_LOG2_I8);

                builder.local("pLocals");

                append_ldloc(builder, getArgU16(ip, 2), isI64 ? WasmOpcode.i64_load : WasmOpcode.i32_load);
                if (isI64)
                    builder.i52_const(1);
                else
                    builder.i32_const(1);
                builder.appendU8(isI64 ? WasmOpcode.i64_or : WasmOpcode.i32_or);
                builder.appendU8(isI64 ? WasmOpcode.i64_clz : WasmOpcode.i32_clz);
                if (isI64)
                    builder.appendU8(WasmOpcode.i32_wrap_i64);
                builder.i32_const(isI64 ? 63 : 31);
                builder.appendU8(WasmOpcode.i32_xor);

                append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
                break;
            }

            case MintOpcode.MINT_SHL_AND_I4:
            case MintOpcode.MINT_SHL_AND_I8: {
                const isI32 = (opcode === MintOpcode.MINT_SHL_AND_I4),
                    loadOp = isI32 ? WasmOpcode.i32_load : WasmOpcode.i64_load,
                    storeOp = isI32 ? WasmOpcode.i32_store : WasmOpcode.i64_store;

                builder.local("pLocals");

                append_ldloc(builder, getArgU16(ip, 2), loadOp);
                append_ldloc(builder, getArgU16(ip, 3), loadOp);
                if (isI32)
                    builder.i32_const(31);
                else
                    builder.i52_const(63);
                builder.appendU8(isI32 ? WasmOpcode.i32_and : WasmOpcode.i64_and);
                builder.appendU8(isI32 ? WasmOpcode.i32_shl : WasmOpcode.i64_shl);

                append_stloc_tail(builder, getArgU16(ip, 1), storeOp);
                break;
            }

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
                    if (isConditionallyExecuted || builder.options.countBailouts) {
                        // Not an exit, because returns are normal and we don't want to make them more expensive.
                        // FIXME: Or do we want to record them? Early conditional returns might reduce the value of a trace,
                        //  but the main problem is more likely to be calls early in traces. Worth testing later.
                        append_bailout(builder, ip, BailoutReason.Return);
                        pruneOpcodes = true;
                    } else
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_LDC_I4_M1) &&
                    (opcode <= MintOpcode.MINT_LDC_R8)
                ) {
                    if (!emit_ldc(builder, ip, opcode))
                        ip = abort;
                    else
                        skipDregInvalidation = true;
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
                    if (!emit_relop_branch(builder, ip, frame, opcode))
                        ip = abort;
                    else
                        isConditionallyExecuted = true;
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
                        // FIXME: Try to reduce the number of these
                        append_exit(builder, ip, exitOpcodeCounter, BailoutReason.ComplexBranch);
                        pruneOpcodes = true;
                    } else
                        ip = abort;
                } else if (
                    (opcode >= MintOpcode.MINT_SIMD_V128_LDC) &&
                    (opcode <= MintOpcode.MINT_SIMD_INTRINS_P_PPP)
                ) {
                    if (!emit_simd(builder, ip, opcode, opname, simdIntrinsArgCount, simdIntrinsIndex))
                        ip = abort;
                    else {
                        containsSimd = true;
                        // We need to do dreg invalidation differently for simd, especially to handle ldc
                        skipDregInvalidation = true;
                    }
                } else if (opcodeValue === 0) {
                    // This means it was explicitly marked as no-value in the opcode value table
                    //  so we can just skip over it. This is done for things like nops.
                } else {
                    /*
                    if (opcodeValue > 0)
                        mono_log_info(`JITERP: aborting trace for opcode ${opname} with value ${opcodeValue}`);
                    */
                    ip = abort;
                }
                break;
        }

        if (ip) {
            if (!skipDregInvalidation) {
                // Invalidate cached values for all the instruction's destination registers.
                // This should have already happened, but it's possible there are opcodes where
                //  our invalidation is incorrect so it's best to do this for safety reasons
                const firstDreg = <any>ip + 2;
                for (let r = 0; r < numDregs; r++) {
                    const dreg = getU16(firstDreg + (r * 2));
                    invalidate_local(dreg);
                }
            }

            if ((trace > 1) || traceOnError || mostRecentOptions!.dumpTraces || instrumentedTraceId) {
                let stmtText = `${(<any>ip).toString(16)} ${opname} `;
                const firstDreg = <any>ip + 2;
                const firstSreg = firstDreg + (numDregs * 2);
                // print sregs
                for (let r = 0; r < numSregs; r++) {
                    if (r !== 0)
                        stmtText += ", ";
                    stmtText += getU16(firstSreg + (r * 2));
                }

                // print dregs
                if (numDregs > 0)
                    stmtText += " -> ";
                for (let r = 0; r < numDregs; r++) {
                    if (r !== 0)
                        stmtText += ", ";
                    stmtText += getU16(firstDreg + (r * 2));
                }

                builder.traceBuf.push(stmtText);
            }

            if (opcodeValue > 0) {
                if (isConditionallyExecuted)
                    conditionalOpcodeCounter++;
                else
                    prologueOpcodeCounter++;
                result += opcodeValue;
            } else if (opcodeValue < 0) {
                // mono_log_info(`JITERP: opcode ${opname} did not abort but had value ${opcodeValue}`);
            }

            ip += <any>(opLengthU16 * 2);
            if (<any>ip <= (<any>endOfBody))
                rip = ip;
            // For debugging
            if (emitPadding)
                builder.appendU8(WasmOpcode.nop);
        } else {
            if (instrumentedTraceId)
                mono_log_info(`instrumented trace ${traceName} aborted for opcode ${opname} @${(<any>_ip).toString(16)}`);
            record_abort(builder.traceIndex, _ip, traceName, opcode);
        }
    }

    if (emitPadding)
        builder.appendU8(WasmOpcode.nop);

    // We need to close any open blocks before generating our closing ret,
    //  because wasm would allow branching past the ret otherwise
    while (builder.activeBlocks > 0)
        builder.endBlock();

    builder.cfg.exitIp = rip;

    // mono_log_info(`estimated size: ${builder.size + builder.cfg.overheadBytes + builder.bytesGeneratedSoFar}`);

    // HACK: Traces containing simd will be *much* shorter than non-simd traces,
    //  which will cause both the heuristic and our length requirement outside
    //  to reject them. For now, just add a big constant to the length
    if (containsSimd)
        result += 10240;
    return result;
}

const notNullSince: Map<number, number> = new Map();
let cknullOffset = -1;

function eraseInferredState() {
    cknullOffset = -1;
    notNullSince.clear();
    knownConstantValues.clear();
}

function invalidate_local(offset: number) {
    if (cknullOffset === offset)
        cknullOffset = -1;
    notNullSince.delete(offset);
    knownConstantValues.delete(offset);
}

function invalidate_local_range(start: number, bytes: number) {
    for (let i = 0; i < bytes; i += 1)
        invalidate_local(start + i);
}

function append_branch_target_block(builder: WasmBuilder, ip: MintOpcodePtr, isBackBranchTarget: boolean) {
    builder.cfg.startBranchBlock(ip, isBackBranchTarget);
}

function computeMemoryAlignment(offset: number, opcodeOrPrefix: WasmOpcode, simdOpcode?: WasmSimdOpcode) {
    // First, compute the best possible alignment
    let alignment = 0;
    if (offset % 16 === 0)
        alignment = 4;
    else if (offset % 8 === 0)
        alignment = 3;
    else if (offset % 4 === 0)
        alignment = 2;
    else if (offset % 2 === 0)
        alignment = 1;

    // stackval is 8 bytes. interp aligns the stack to 16 bytes for v128.
    // wasm spec prohibits alignment higher than natural alignment, just to be annoying
    switch (opcodeOrPrefix) {
        case WasmOpcode.PREFIX_simd:
            // For loads that aren't a regular v128 load, assume weird things might be happening with alignment
            alignment = (
                (simdOpcode === WasmSimdOpcode.v128_load) ||
                (simdOpcode === WasmSimdOpcode.v128_store)
            ) ? Math.min(alignment, 4) : 0;
            break;
        case WasmOpcode.i64_load:
        case WasmOpcode.f64_load:
        case WasmOpcode.i64_store:
        case WasmOpcode.f64_store:
            alignment = Math.min(alignment, 3);
            break;
        case WasmOpcode.i64_load32_s:
        case WasmOpcode.i64_load32_u:
        case WasmOpcode.i64_store32:
        case WasmOpcode.i32_load:
        case WasmOpcode.f32_load:
        case WasmOpcode.i32_store:
        case WasmOpcode.f32_store:
            alignment = Math.min(alignment, 2);
            break;
        case WasmOpcode.i64_load16_s:
        case WasmOpcode.i64_load16_u:
        case WasmOpcode.i32_load16_s:
        case WasmOpcode.i32_load16_u:
        case WasmOpcode.i64_store16:
        case WasmOpcode.i32_store16:
            alignment = Math.min(alignment, 1);
            break;
        case WasmOpcode.i64_load8_s:
        case WasmOpcode.i64_load8_u:
        case WasmOpcode.i32_load8_s:
        case WasmOpcode.i32_load8_u:
        case WasmOpcode.i64_store8:
        case WasmOpcode.i32_store8:
            alignment = 0;
            break;
        default:
            alignment = 0;
            break;
    }

    return alignment;
}

function append_ldloc(builder: WasmBuilder, offset: number, opcodeOrPrefix: WasmOpcode, simdOpcode?: WasmSimdOpcode) {
    builder.local("pLocals");
    mono_assert(opcodeOrPrefix >= WasmOpcode.i32_load, () => `Expected load opcode but got ${opcodeOrPrefix}`);
    builder.appendU8(opcodeOrPrefix);
    if (simdOpcode !== undefined) {
        // This looks wrong but I assure you it's correct.
        builder.appendULeb(simdOpcode);
    } else if (opcodeOrPrefix === WasmOpcode.PREFIX_simd) {
        throw new Error("PREFIX_simd ldloc without a simdOpcode");
    }
    const alignment = computeMemoryAlignment(offset, opcodeOrPrefix, simdOpcode);
    builder.appendMemarg(offset, alignment);
}

// You need to have pushed pLocals onto the stack *before* the value you intend to store
// Wasm store opcodes are shaped like xNN.store [offset] [alignment],
//  where the offset+alignment pair is referred to as a 'memarg' by the spec.
// The actual store operation is equivalent to `pBase[offset] = value` (alignment has no
//  observable impact on behavior, other than causing compilation failures if out of range)
function append_stloc_tail(builder: WasmBuilder, offset: number, opcodeOrPrefix: WasmOpcode, simdOpcode?: WasmSimdOpcode) {
    mono_assert(opcodeOrPrefix >= WasmOpcode.i32_store, () => `Expected store opcode but got ${opcodeOrPrefix}`);
    builder.appendU8(opcodeOrPrefix);
    if (simdOpcode !== undefined) {
        // This looks wrong but I assure you it's correct.
        builder.appendULeb(simdOpcode);
    }
    const alignment = computeMemoryAlignment(offset, opcodeOrPrefix, simdOpcode);
    builder.appendMemarg(offset, alignment);
    invalidate_local(offset);
    // HACK: Invalidate the second stack slot used by a simd vector
    if (simdOpcode !== undefined)
        invalidate_local(offset + 8);
}

// Pass bytesInvalidated=0 if you are reading from the local and the address will never be
//  used for writes
// Pass transient=true if the address will not persist after use (so it can't be used to later
//  modify the contents of this local)
function append_ldloca(builder: WasmBuilder, localOffset: number, bytesInvalidated?: number) {
    if (typeof (bytesInvalidated) !== "number")
        bytesInvalidated = 512;
    // FIXME: We need to know how big this variable is so we can invalidate the whole space it occupies
    if (bytesInvalidated > 0)
        invalidate_local_range(localOffset, bytesInvalidated);
    builder.lea("pLocals", localOffset);
}

function append_memset_local(builder: WasmBuilder, localOffset: number, value: number, count: number) {
    invalidate_local_range(localOffset, count);

    // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
    if (try_append_memset_fast(builder, localOffset, value, count, false))
        return;

    // spec: pop n, pop val, pop d, fill from d[0] to d[n] with value val
    append_ldloca(builder, localOffset, count);
    append_memset_dest(builder, value, count);
}

function append_memmove_local_local(builder: WasmBuilder, destLocalOffset: number, sourceLocalOffset: number, count: number) {
    invalidate_local_range(destLocalOffset, count);

    if (try_append_memmove_fast(builder, destLocalOffset, sourceLocalOffset, count, false))
        return true;

    // spec: pop n, pop s, pop d, copy n bytes from s to d
    append_ldloca(builder, destLocalOffset, count);
    append_ldloca(builder, sourceLocalOffset, 0);
    append_memmove_dest_src(builder, count);
}

function isAddressTaken(builder: WasmBuilder, localOffset: number) {
    return cwraps.mono_jiterp_is_imethod_var_address_taken(<any>get_imethod(builder.frame), localOffset) !== 0;
}

// Loads the specified i32 value and then bails out if it is null, leaving it in the cknull_ptr local.
function append_ldloc_cknull(builder: WasmBuilder, localOffset: number, ip: MintOpcodePtr, leaveOnStack: boolean) {
    const optimize = builder.allowNullCheckOptimization &&
        notNullSince.has(localOffset) &&
        !isAddressTaken(builder, localOffset);

    if (optimize) {
        modifyCounter(JiterpCounter.NullChecksEliminated, 1);
        if (nullCheckCaching && (cknullOffset === localOffset)) {
            if (traceNullCheckOptimizations)
                mono_log_info(`(0x${(<any>ip).toString(16)}) cknull_ptr == locals[${localOffset}], not null since 0x${notNullSince.get(localOffset)!.toString(16)}`);
            if (leaveOnStack)
                builder.local("cknull_ptr");
        } else {
            // mono_log_info(`skipping null check for ${localOffset}`);
            append_ldloc(builder, localOffset, WasmOpcode.i32_load);
            builder.local("cknull_ptr", leaveOnStack ? WasmOpcode.tee_local : WasmOpcode.set_local);
            if (traceNullCheckOptimizations)
                mono_log_info(`(0x${(<any>ip).toString(16)}) cknull_ptr := locals[${localOffset}] (fresh load, already null checked at 0x${notNullSince.get(localOffset)!.toString(16)})`);
            cknullOffset = localOffset;
        }

        if (nullCheckValidation) {
            builder.local("cknull_ptr");
            append_ldloc(builder, localOffset, WasmOpcode.i32_load);
            builder.i32_const(builder.traceIndex);
            builder.i32_const(ip);
            builder.callImport("notnull");
        }
        return;
    }

    append_ldloc(builder, localOffset, WasmOpcode.i32_load);
    builder.local("cknull_ptr", WasmOpcode.tee_local);
    builder.appendU8(WasmOpcode.i32_eqz);
    builder.block(WasmValtype.void, WasmOpcode.if_);
    append_bailout(builder, ip, BailoutReason.NullCheck);
    builder.endBlock();
    if (leaveOnStack)
        builder.local("cknull_ptr");

    if (
        builder.allowNullCheckOptimization &&
        !isAddressTaken(builder, localOffset)
    ) {
        notNullSince.set(localOffset, <any>ip);
        if (traceNullCheckOptimizations)
            mono_log_info(`(0x${(<any>ip).toString(16)}) cknull_ptr := locals[${localOffset}] (fresh load, fresh null check)`);
        cknullOffset = localOffset;
    } else
        cknullOffset = -1;
}

function emit_ldc(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
    let storeType = WasmOpcode.i32_store;
    let value: number | undefined;

    const tableEntry = ldcTable[opcode];
    if (tableEntry) {
        builder.local("pLocals");
        builder.appendU8(tableEntry[0]);
        value = tableEntry[1];
        builder.appendLeb(value);
    } else {
        switch (opcode) {
            case MintOpcode.MINT_LDC_I4_S:
                builder.local("pLocals");
                value = getArgI16(ip, 2);
                builder.i32_const(value);
                break;
            case MintOpcode.MINT_LDC_I4:
                builder.local("pLocals");
                value = getArgI32(ip, 2);
                builder.i32_const(value);
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

    if (typeof (value) === "number")
        knownConstantValues.set(localOffset, value);
    else
        knownConstantValues.delete(localOffset);

    return true;
}

function emit_mov(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
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

function append_vtable_initialize(builder: WasmBuilder, pVtable: NativePointer, ip: MintOpcodePtr) {
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

function emit_fieldop(
    builder: WasmBuilder, frame: NativePointer,
    ip: MintOpcodePtr, opcode: MintOpcode
): boolean {
    const isLoad = (
        (opcode >= MintOpcode.MINT_LDFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDFLDA_UNSAFE)
    ) ||
        (
            (opcode >= MintOpcode.MINT_LDSFLD_I1) &&
            (opcode <= MintOpcode.MINT_LDSFLD_W)
        );

    const objectOffset = getArgU16(ip, isLoad ? 2 : 1),
        fieldOffset = getArgU16(ip, 3),
        localOffset = getArgU16(ip, isLoad ? 1 : 2);

    // Check this before potentially emitting a cknull
    const notNull = builder.allowNullCheckOptimization &&
        notNullSince.has(objectOffset) &&
        !isAddressTaken(builder, objectOffset);

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
             *  and technically it should use a different kind of barrier from copy_ptr. So
             *  we define a special import that is responsible for performing the whole stfld_o
             *  operation with as little trace-side overhead as possible
             * Previously the pseudocode looked like:
             *  cknull_ptr = *(MonoObject *)&locals[objectOffset];
             *  if (!cknull_ptr) bailout;
             *  copy_ptr(cknull_ptr + fieldOffset, *(MonoObject *)&locals[localOffset])
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
                builder.appendULeb(0);
                append_bailout(builder, ip, BailoutReason.NullCheck);
                builder.endBlock();
            } else {
                if (traceNullCheckOptimizations)
                    mono_log_info(`(0x${(<any>ip).toString(16)}) locals[${objectOffset}] not null since 0x${notNullSince.get(objectOffset)!.toString(16)}`);

                builder.appendU8(WasmOpcode.drop);
                modifyCounter(JiterpCounter.NullChecksEliminated, 1);

                if (nullCheckValidation) {
                    // cknull_ptr was not used here so all we can do is verify that the target object is not null
                    append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
                    append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
                    builder.i32_const(builder.traceIndex);
                    builder.i32_const(ip);
                    builder.callImport("notnull");
                }
            }
            return true;
        }
        case MintOpcode.MINT_LDFLD_VT: {
            const sizeBytes = getArgU16(ip, 4);
            // dest
            append_ldloca(builder, localOffset, sizeBytes);
            // src
            builder.local("cknull_ptr");
            if (fieldOffset !== 0) {
                builder.i32_const(fieldOffset);
                builder.appendU8(WasmOpcode.i32_add);
            }
            append_memmove_dest_src(builder, sizeBytes);
            return true;
        }
        case MintOpcode.MINT_STFLD_VT: {
            const klass = get_imethod_data(frame, getArgU16(ip, 4));
            // dest = (char*)o + ip [3]
            builder.local("cknull_ptr");
            if (fieldOffset !== 0) {
                builder.i32_const(fieldOffset);
                builder.appendU8(WasmOpcode.i32_add);
            }
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
            if (fieldOffset !== 0) {
                builder.i32_const(fieldOffset);
                builder.appendU8(WasmOpcode.i32_add);
            }
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
            if (fieldOffset !== 0) {
                builder.i32_const(fieldOffset);
                builder.appendU8(WasmOpcode.i32_add);
            }
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

function emit_sfieldop(
    builder: WasmBuilder, frame: NativePointer,
    ip: MintOpcodePtr, opcode: MintOpcode
): boolean {
    const isLoad = (
        (opcode >= MintOpcode.MINT_LDFLD_I1) &&
        (opcode <= MintOpcode.MINT_LDFLDA_UNSAFE)
    ) ||
        (
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
            builder.callImport("copy_ptr");
            return true;
        case MintOpcode.MINT_LDSFLD_VT: {
            const sizeBytes = getArgU16(ip, 4);
            // dest
            append_ldloca(builder, localOffset, sizeBytes);
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

function emit_binop(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
    // operands are popped right to left, which means you build the arg list left to right
    let lhsLoadOp: WasmOpcode, rhsLoadOp: WasmOpcode, storeOp: WasmOpcode,
        lhsVar = "math_lhs32", rhsVar = "math_rhs32",
        info: OpRec3 | OpRec4 | undefined,
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

function emit_unop(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
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
        case MintOpcode.MINT_ROL_I4_IMM:
        case MintOpcode.MINT_ROR_I4_IMM:
            append_ldloc(builder, getArgU16(ip, 2), loadOp);
            builder.i32_const(getArgI16(ip, 3));
            break;

        case MintOpcode.MINT_ADD_I8_IMM:
        case MintOpcode.MINT_MUL_I8_IMM:
        case MintOpcode.MINT_SHL_I8_IMM:
        case MintOpcode.MINT_SHR_I8_IMM:
        case MintOpcode.MINT_SHR_UN_I8_IMM:
        case MintOpcode.MINT_ROL_I8_IMM:
        case MintOpcode.MINT_ROR_I8_IMM:
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

function append_call_handler_store_ret_ip(
    builder: WasmBuilder, ip: MintOpcodePtr,
    frame: NativePointer, opcode: MintOpcode
) {
    const shortOffset = (opcode === MintOpcode.MINT_CALL_HANDLER_S),
        retIp = shortOffset ? <any>ip + (3 * 2) : <any>ip + (4 * 2),
        clauseIndex = getU16(retIp - 2),
        clauseDataOffset = get_imethod_clause_data_offset(frame, clauseIndex);

    // note: locals here is unsigned char *, not stackval *, so clauseDataOffset is in bytes
    // *(const guint16**)(locals + frame->imethod->clause_data_offsets [clause_index]) = ret_ip;
    builder.local("pLocals");
    builder.ptr_const(retIp);
    builder.appendU8(WasmOpcode.i32_store);
    builder.appendMemarg(clauseDataOffset, 0); // FIXME: 32-bit alignment?

    // mono_log_info(`call_handler @0x${(<any>ip).toString(16)} retIp=0x${retIp.toString(16)}`);
    builder.callHandlerReturnAddresses.push(retIp);
}

function getBranchDisplacement(
    ip: MintOpcodePtr, opcode: MintOpcode
) : number | undefined {
    const opArgType = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.OpArgType),
        payloadOffset = cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Sregs),
        payloadAddress = <any>ip + 2 + (payloadOffset * 2);

    let result : number;
    switch (opArgType) {
        case MintOpArgType.MintOpBranch:
            result = getI32_unaligned(payloadAddress);
            break;
        case MintOpArgType.MintOpShortBranch:
            result = getI16(payloadAddress);
            break;
        case MintOpArgType.MintOpShortAndShortBranch:
            result = getI16(payloadAddress + 2);
            break;
        default:
            return undefined;
    }

    if (traceBranchDisplacements)
        mono_log_info(`${getOpcodeName(opcode)} @${ip} displacement=${result}`);

    return result;
}

function emit_branch(
    builder: WasmBuilder, ip: MintOpcodePtr,
    frame: NativePointer, opcode: MintOpcode
): boolean {
    const isSafepoint = (opcode >= MintOpcode.MINT_BRFALSE_I4_SP) &&
        (opcode <= MintOpcode.MINT_BLT_UN_I8_IMM_SP);

    const displacement = getBranchDisplacement(ip, opcode);
    if (typeof (displacement) !== "number")
        return false;

    // If the branch is taken we bail out to allow the interpreter to do it.
    // So for brtrue, we want to do 'cond == 0' to produce a bailout only
    //  when the branch will be taken (by skipping the bailout in this block)
    // When branches are enabled, instead we set eip and then break out of
    //  the current branch block and execution proceeds forward to find the
    //  branch target (if possible), bailing out at the end otherwise
    switch (opcode) {
        case MintOpcode.MINT_CALL_HANDLER:
        case MintOpcode.MINT_CALL_HANDLER_S:
        case MintOpcode.MINT_BR:
        case MintOpcode.MINT_BR_S: {
            const isCallHandler = (opcode === MintOpcode.MINT_CALL_HANDLER) ||
                (opcode === MintOpcode.MINT_CALL_HANDLER_S);

            const destination = <any>ip + (displacement * 2);

            if (displacement <= 0) {
                if (builder.backBranchOffsets.indexOf(destination) >= 0) {
                    // We found a backward branch target we can branch to, so we branch out
                    //  to the top of the loop body
                    // append_safepoint(builder, ip);
                    if (traceBackBranches > 1)
                        mono_log_info(`performing backward branch to 0x${destination.toString(16)}`);
                    if (isCallHandler)
                        append_call_handler_store_ret_ip(builder, ip, frame, opcode);
                    builder.cfg.branch(destination, true, CfgBranchType.Unconditional);
                    modifyCounter(JiterpCounter.BackBranchesEmitted, 1);
                    return true;
                } else {
                    if (destination < builder.cfg.entryIp) {
                        if ((traceBackBranches > 1) || (builder.cfg.trace > 1))
                            mono_log_info(`${getOpcodeName(opcode)} target 0x${destination.toString(16)} before start of trace`);
                    } else if ((traceBackBranches > 0) || (builder.cfg.trace > 0))
                        mono_log_info(`0x${(<any>ip).toString(16)} ${getOpcodeName(opcode)} target 0x${destination.toString(16)} not found in list ` +
                            builder.backBranchOffsets.map(bbo => "0x" + (<any>bbo).toString(16)).join(", ")
                        );

                    cwraps.mono_jiterp_boost_back_branch_target(destination);
                    // FIXME: Should there be a safepoint here?
                    append_bailout(builder, destination, BailoutReason.BackwardBranch);
                    modifyCounter(JiterpCounter.BackBranchesNotEmitted, 1);
                    return true;
                }
            } else {
                // Simple branches are enabled and this is a forward branch. We
                //  don't need to wrap things in a block here, we can just exit
                //  the current branch block after updating eip
                builder.branchTargets.add(destination);
                if (isCallHandler)
                    append_call_handler_store_ret_ip(builder, ip, frame, opcode);
                builder.cfg.branch(destination, false, CfgBranchType.Unconditional);
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

            // Load the condition

            append_ldloc(builder, getArgU16(ip, 1), is64 ? WasmOpcode.i64_load : WasmOpcode.i32_load);
            if (
                (opcode === MintOpcode.MINT_BRFALSE_I4_S) ||
                (opcode === MintOpcode.MINT_BRFALSE_I4_SP)
            )
                builder.appendU8(WasmOpcode.i32_eqz);
            else if (opcode === MintOpcode.MINT_BRFALSE_I8_S) {
                builder.appendU8(WasmOpcode.i64_eqz);
            } else if (opcode === MintOpcode.MINT_BRTRUE_I8_S) {
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
                throw new Error(`Unsupported relop branch opcode: ${getOpcodeName(opcode)}`);

            if (cwraps.mono_jiterp_get_opcode_info(opcode, OpcodeInfoType.Length) !== 4)
                throw new Error(`Unsupported long branch opcode: ${getOpcodeName(opcode)}`);

            break;
        }
    }

    const destination = <any>ip + (displacement * 2);

    if (displacement < 0) {
        if (builder.backBranchOffsets.indexOf(destination) >= 0) {
            // We found a backwards branch target we can reach via our outer trace loop, so
            //  we update eip and branch out to the top of the loop block
            if (traceBackBranches > 1)
                mono_log_info(`performing conditional backward branch to 0x${destination.toString(16)}`);
            builder.cfg.branch(destination, true, isSafepoint ? CfgBranchType.SafepointConditional : CfgBranchType.Conditional);
            modifyCounter(JiterpCounter.BackBranchesEmitted, 1);
        } else {
            if (destination < builder.cfg.entryIp) {
                if ((traceBackBranches > 1) || (builder.cfg.trace > 1))
                    mono_log_info(`${getOpcodeName(opcode)} target 0x${destination.toString(16)} before start of trace`);
            } else if ((traceBackBranches > 0) || (builder.cfg.trace > 0))
                mono_log_info(`0x${(<any>ip).toString(16)} ${getOpcodeName(opcode)} target 0x${destination.toString(16)} not found in list ` +
                    builder.backBranchOffsets.map(bbo => "0x" + (<any>bbo).toString(16)).join(", ")
                );
            // We didn't find a loop to branch to, so bail out
            cwraps.mono_jiterp_boost_back_branch_target(destination);
            builder.block(WasmValtype.void, WasmOpcode.if_);
            append_bailout(builder, destination, BailoutReason.BackwardBranch);
            builder.endBlock();
            modifyCounter(JiterpCounter.BackBranchesNotEmitted, 1);
        }
    } else {
        // Branching is enabled, so set eip and exit the current branch block
        builder.branchTargets.add(destination);
        builder.cfg.branch(destination, false, isSafepoint ? CfgBranchType.SafepointConditional : CfgBranchType.Conditional);
    }

    return true;
}

function emit_relop_branch(
    builder: WasmBuilder, ip: MintOpcodePtr,
    frame: NativePointer, opcode: MintOpcode
): boolean {
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

    return emit_branch(builder, ip, frame, opcode);
}

function emit_math_intrinsic(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
    let isUnary: boolean, isF32: boolean, name: string | undefined;
    let wasmOp: WasmOpcode | undefined;
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

function emit_indirectop(builder: WasmBuilder, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
    const isLoad = (opcode >= MintOpcode.MINT_LDIND_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I8);
    const isAddMul = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_ADD_MUL_IMM_I8)
    );
    const isOffset = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_IMM_I8)
    ) ||
        (
            (opcode >= MintOpcode.MINT_STIND_OFFSET_I1) &&
            (opcode <= MintOpcode.MINT_STIND_OFFSET_IMM_I8)
        ) || isAddMul;
    const isImm = (
        (opcode >= MintOpcode.MINT_LDIND_OFFSET_IMM_I1) &&
        (opcode <= MintOpcode.MINT_LDIND_OFFSET_IMM_I8)
    ) ||
        (
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

    let getter: WasmOpcode, setter = WasmOpcode.i32_store;
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
        builder.callImport("copy_ptr");
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

function append_getelema1(
    builder: WasmBuilder, ip: MintOpcodePtr,
    objectOffset: number, indexOffset: number, elementSize: number
) {
    builder.block();

    /*
    const constantIndex = get_known_constant_value(indexOffset);
    if (typeof (constantIndex) === "number")
        console.log(`getelema1 in ${builder.functions[0].name} with constant index ${constantIndex}`);
    */

    // load index for check
    append_ldloc(builder, indexOffset, WasmOpcode.i32_load);
    // stash it since we need it twice
    builder.local("index", WasmOpcode.tee_local);

    let ptrLocal = "cknull_ptr";
    if (builder.options.zeroPageOptimization && isZeroPageReserved()) {
        // load array ptr and stash it
        // if the array ptr is null, the length check will fail and we will bail out
        modifyCounter(JiterpCounter.NullChecksFused, 1);
        append_ldloc(builder, objectOffset, WasmOpcode.i32_load);
        ptrLocal = "src_ptr";
        builder.local(ptrLocal, WasmOpcode.tee_local);
    } else
        // array null check
        append_ldloc_cknull(builder, objectOffset, ip, true);

    // current stack layout is [index, ptr]
    // load array length
    builder.appendU8(WasmOpcode.i32_load);
    builder.appendMemarg(getMemberOffset(JiterpMember.ArrayLength), 2);
    // current stack layout is [index, length]
    // check index < array.length, unsigned. if index is negative it will be interpreted as
    //  a massive value which is naturally going to be bigger than array.length. interp.c
    //  exploits this property so we can too
    // for a null array pointer array.length will also be zero thanks to the zero page optimization
    builder.appendU8(WasmOpcode.i32_lt_u);
    // bailout unless (index < array.length)
    builder.appendU8(WasmOpcode.br_if);
    builder.appendULeb(0);
    append_bailout(builder, ip, BailoutReason.ArrayLoadFailed);
    builder.endBlock();

    // We did a null check and bounds check so we can now compute the actual address
    builder.local(ptrLocal);
    builder.i32_const(getMemberOffset(JiterpMember.ArrayData));
    builder.appendU8(WasmOpcode.i32_add);

    builder.local("index");
    if (elementSize != 1) {
        builder.i32_const(elementSize);
        builder.appendU8(WasmOpcode.i32_mul);
    }
    builder.appendU8(WasmOpcode.i32_add);
    // append_getelema1 leaves the address on the stack
}

function emit_arrayop(builder: WasmBuilder, frame: NativePointer, ip: MintOpcodePtr, opcode: MintOpcode): boolean {
    const isLoad = ((opcode <= MintOpcode.MINT_LDELEMA_TC) && (opcode >= MintOpcode.MINT_LDELEM_I1)) ||
        (opcode === MintOpcode.MINT_LDLEN),
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
            // note: zero page optimization is not valid here since we want to throw on null
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
            builder.appendULeb(0);
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
        case MintOpcode.MINT_STELEM_VT_NOREF: {
            const elementSize = getArgU16(ip, 5);
            // dest
            append_getelema1(builder, ip, objectOffset, indexOffset, elementSize);
            // src
            append_ldloca(builder, valueOffset, 0);
            append_memmove_dest_src(builder, elementSize);
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

let wasmSimdSupported: boolean | undefined;

function getIsWasmSimdSupported(): boolean {
    if (wasmSimdSupported !== undefined)
        return wasmSimdSupported;

    wasmSimdSupported = runtimeHelpers.featureWasmSimd === true;
    if (!wasmSimdSupported)
        mono_log_info("Disabling Jiterpreter SIMD");

    return wasmSimdSupported;
}

function get_import_name(
    builder: WasmBuilder, typeName: string,
    functionPtr: number
): string {
    const name = `${typeName}_${functionPtr.toString(16)}`;
    if (typeof (builder.importedFunctions[name]) !== "object")
        builder.defineImportedFunction("s", name, typeName, false, functionPtr);

    return name;
}

function emit_simd(
    builder: WasmBuilder, ip: MintOpcodePtr,
    opcode: MintOpcode, opname: string,
    argCount: number, index: number
): boolean {
    // First, if compiling an intrinsic attempt to emit the special vectorized implementation
    // We only do this if SIMD is enabled since we'll be using the v128 opcodes.
    if (builder.options.enableSimd && getIsWasmSimdSupported()) {
        switch (argCount) {
            case 2:
                if (emit_simd_2(builder, ip, <SimdIntrinsic2>index))
                    return true;
                break;
            case 3:
                if (emit_simd_3(builder, ip, <SimdIntrinsic3>index))
                    return true;
                break;
            case 4:
                if (emit_simd_4(builder, ip, <SimdIntrinsic4>index))
                    return true;
                break;
        }
    }

    // Fall back to a mix of non-vectorized wasm and the interpreter's implementation of the opcodes
    switch (opcode) {
        case MintOpcode.MINT_SIMD_V128_LDC: {
            if (builder.options.enableSimd && getIsWasmSimdSupported()) {
                builder.local("pLocals");
                const view = localHeapViewU8().slice(<any>ip + 4, <any>ip + 4 + sizeOfV128);
                builder.v128_const(view);
                append_simd_store(builder, ip);
                knownConstantValues.set(getArgU16(ip, 1), view);
            } else {
                // dest
                append_ldloca(builder, getArgU16(ip, 1), sizeOfV128);
                // src (ip + 2)
                builder.ptr_const(<any>ip + 4);
                append_memmove_dest_src(builder, sizeOfV128);
            }
            return true;
        }
        case MintOpcode.MINT_SIMD_V128_I1_CREATE:
        case MintOpcode.MINT_SIMD_V128_I2_CREATE:
        case MintOpcode.MINT_SIMD_V128_I4_CREATE:
        case MintOpcode.MINT_SIMD_V128_I8_CREATE: {
            // These opcodes pack a series of locals into a vector
            const elementSize = simdCreateSizes[opcode],
                numElements = sizeOfV128 / elementSize,
                destOffset = getArgU16(ip, 1),
                srcOffset = getArgU16(ip, 2),
                loadOp = simdCreateLoadOps[opcode],
                storeOp = simdCreateStoreOps[opcode];
            for (let i = 0; i < numElements; i++) {
                builder.local("pLocals");
                // load element from stack slot
                append_ldloc(builder, srcOffset + (i * sizeOfStackval), loadOp);
                // then store to destination element
                append_stloc_tail(builder, destOffset + (i * elementSize), storeOp);
            }
            return true;
        }
        case MintOpcode.MINT_SIMD_INTRINS_P_P: {
            simdFallbackCounters[opname] = (simdFallbackCounters[opname] || 0) + 1;
            // res
            append_ldloca(builder, getArgU16(ip, 1), sizeOfV128);
            // src
            append_ldloca(builder, getArgU16(ip, 2), 0);
            const importName = get_import_name(builder, "simd_p_p", <any>cwraps.mono_jiterp_get_simd_intrinsic(1, index));
            builder.callImport(importName);
            return true;
        }
        case MintOpcode.MINT_SIMD_INTRINS_P_PP: {
            simdFallbackCounters[opname] = (simdFallbackCounters[opname] || 0) + 1;
            // res
            append_ldloca(builder, getArgU16(ip, 1), sizeOfV128);
            // src
            append_ldloca(builder, getArgU16(ip, 2), 0);
            append_ldloca(builder, getArgU16(ip, 3), 0);
            const importName = get_import_name(builder, "simd_p_pp", <any>cwraps.mono_jiterp_get_simd_intrinsic(2, index));
            builder.callImport(importName);
            return true;
        }
        case MintOpcode.MINT_SIMD_INTRINS_P_PPP: {
            simdFallbackCounters[opname] = (simdFallbackCounters[opname] || 0) + 1;
            // res
            append_ldloca(builder, getArgU16(ip, 1), sizeOfV128);
            // src
            append_ldloca(builder, getArgU16(ip, 2), 0);
            append_ldloca(builder, getArgU16(ip, 3), 0);
            append_ldloca(builder, getArgU16(ip, 4), 0);
            const importName = get_import_name(builder, "simd_p_ppp", <any>cwraps.mono_jiterp_get_simd_intrinsic(3, index));
            builder.callImport(importName);
            return true;
        }
        default:
            mono_log_info(`jiterpreter emit_simd failed for ${opname}`);
            return false;
    }
}

function append_simd_store(builder: WasmBuilder, ip: MintOpcodePtr) {
    append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_store);
}

function append_simd_2_load(builder: WasmBuilder, ip: MintOpcodePtr, loadOp?: WasmSimdOpcode) {
    builder.local("pLocals");
    // This || is harmless since v128_load is 0
    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, loadOp || WasmSimdOpcode.v128_load);
}

function append_simd_3_load(builder: WasmBuilder, ip: MintOpcodePtr) {
    builder.local("pLocals");
    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
    // FIXME: Can rhs be a scalar? We handle shifts separately already
    append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
}

function append_simd_4_load(builder: WasmBuilder, ip: MintOpcodePtr) {
    builder.local("pLocals");
    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
    append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
    append_ldloc(builder, getArgU16(ip, 4), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
}

function emit_simd_2(builder: WasmBuilder, ip: MintOpcodePtr, index: SimdIntrinsic2): boolean {
    const simple = <WasmSimdOpcode>cwraps.mono_jiterp_get_simd_opcode(1, index);
    if (simple >= 0) {
        if (simdLoadTable.has(index)) {
            // Indirect load, so v1 is T** and res is Vector128*
            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
            builder.appendSimd(simple, true);
            builder.appendMemarg(0, 0);
            append_simd_store(builder, ip);
        } else {
            append_simd_2_load(builder, ip);
            builder.appendSimd(simple);
            append_simd_store(builder, ip);
        }
        return true;
    }

    const bitmask = bitmaskTable[index];
    if (bitmask) {
        append_simd_2_load(builder, ip);
        builder.appendSimd(bitmask);
        append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
        return true;
    }

    switch (index) {
        case SimdIntrinsic2.V128_I1_CREATE_SCALAR:
        case SimdIntrinsic2.V128_I2_CREATE_SCALAR:
        case SimdIntrinsic2.V128_I4_CREATE_SCALAR:
        case SimdIntrinsic2.V128_I8_CREATE_SCALAR: {
            const tableEntry = createScalarTable[index];
            builder.local("pLocals");
            // Make a zero vector
            builder.v128_const(0);
            // Load the scalar value
            append_ldloc(builder, getArgU16(ip, 2), tableEntry[0]);
            // Replace the first lane
            builder.appendSimd(tableEntry[1]);
            builder.appendU8(0);
            // Store result
            append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_store);
            return true;
        }

        case SimdIntrinsic2.V128_I1_CREATE:
            append_simd_2_load(builder, ip, WasmSimdOpcode.v128_load8_splat);
            append_simd_store(builder, ip);
            return true;
        case SimdIntrinsic2.V128_I2_CREATE:
            append_simd_2_load(builder, ip, WasmSimdOpcode.v128_load16_splat);
            append_simd_store(builder, ip);
            return true;
        case SimdIntrinsic2.V128_I4_CREATE:
            append_simd_2_load(builder, ip, WasmSimdOpcode.v128_load32_splat);
            append_simd_store(builder, ip);
            return true;
        case SimdIntrinsic2.V128_I8_CREATE:
            append_simd_2_load(builder, ip, WasmSimdOpcode.v128_load64_splat);
            append_simd_store(builder, ip);
            return true;

        default:
            return false;
    }
}

function emit_simd_3(builder: WasmBuilder, ip: MintOpcodePtr, index: SimdIntrinsic3): boolean {
    const simple = <WasmSimdOpcode>cwraps.mono_jiterp_get_simd_opcode(2, index);
    if (simple >= 0) {
        const isShift = simdShiftTable.has(index),
            extractTup = simdExtractTable[index];

        if (isShift) {
            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.i32_load);
            builder.appendSimd(simple);
            append_simd_store(builder, ip);
        } else if (Array.isArray(extractTup)) {
            const lane = get_known_constant_value(builder, getArgU16(ip, 3)),
                laneCount = extractTup[0];
            if (typeof (lane) !== "number") {
                mono_log_error(`${builder.functions[0].name}: Non-constant lane index passed to ExtractScalar`);
                return false;
            } else if ((lane >= laneCount) || (lane < 0)) {
                mono_log_error(`${builder.functions[0].name}: ExtractScalar index ${lane} out of range (0 - ${laneCount - 1})`);
                return false;
            }

            // load vec onto stack and then emit extract + lane imm
            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.appendSimd(simple);
            builder.appendU8(lane);
            // Store using the opcode from the tuple
            append_stloc_tail(builder, getArgU16(ip, 1), extractTup[1]);
        } else {
            append_simd_3_load(builder, ip);
            builder.appendSimd(simple);
            append_simd_store(builder, ip);
        }
        return true;
    }

    switch (index) {
        case SimdIntrinsic3.StoreANY:
            // Indirect store where args are [V128**, V128*]
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.appendSimd(WasmSimdOpcode.v128_store);
            builder.appendMemarg(0, 0);
            return true;
        case SimdIntrinsic3.V128_BITWISE_EQUALITY:
        case SimdIntrinsic3.V128_BITWISE_INEQUALITY:
            append_simd_3_load(builder, ip);
            // FIXME: i64x2_ne and i64x2_any_true?
            builder.appendSimd(WasmSimdOpcode.i64x2_eq);
            builder.appendSimd(WasmSimdOpcode.i64x2_all_true);
            if (index === SimdIntrinsic3.V128_BITWISE_INEQUALITY)
                builder.appendU8(WasmOpcode.i32_eqz);
            append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
            return true;
        case SimdIntrinsic3.V128_R4_FLOAT_EQUALITY:
        case SimdIntrinsic3.V128_R8_FLOAT_EQUALITY: {
            /*
            Vector128<T> result = Vector128.Equals(lhs, rhs) | ~(Vector128.Equals(lhs, lhs) | Vector128.Equals(rhs, rhs));
            return result.AsInt32() == Vector128<int>.AllBitsSet;
            */
            const isR8 = index === SimdIntrinsic3.V128_R8_FLOAT_EQUALITY,
                eqOpcode = isR8 ? WasmSimdOpcode.f64x2_eq : WasmSimdOpcode.f32x4_eq;
            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.local("math_lhs128", WasmOpcode.tee_local);
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.local("math_rhs128", WasmOpcode.tee_local);
            builder.appendSimd(eqOpcode);
            builder.local("math_lhs128");
            builder.local("math_lhs128");
            builder.appendSimd(eqOpcode);
            builder.local("math_rhs128");
            builder.local("math_rhs128");
            builder.appendSimd(eqOpcode);
            builder.appendSimd(WasmSimdOpcode.v128_or);
            builder.appendSimd(WasmSimdOpcode.v128_not);
            builder.appendSimd(WasmSimdOpcode.v128_or);
            builder.appendSimd(isR8 ? WasmSimdOpcode.i64x2_all_true : WasmSimdOpcode.i32x4_all_true);
            append_stloc_tail(builder, getArgU16(ip, 1), WasmOpcode.i32_store);
            return true;
        }
        case SimdIntrinsic3.V128_I1_SHUFFLE: {
            // Detect a constant indices vector and turn it into a const. This allows
            //  v8 to use a more optimized implementation of the swizzle opcode
            const indicesOffset = getArgU16(ip, 3),
                constantIndices = get_known_constant_value(builder, indicesOffset);

            // Pre-load destination ptr
            builder.local("pLocals");
            // Load vec
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);

            if (typeof (constantIndices) === "object") {
                // HACK: Use the known constant vector directly instead of loading it from memory.
                builder.appendSimd(WasmSimdOpcode.v128_const);
                builder.appendBytes(constantIndices);
            } else {
                // Load the indices from memory
                append_ldloc(builder, indicesOffset, WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            }

            // we now have two vectors on the stack, the values and the byte indices
            builder.appendSimd(WasmSimdOpcode.i8x16_swizzle);
            append_simd_store(builder, ip);
            return true;
        }
        case SimdIntrinsic3.V128_I2_SHUFFLE:
        case SimdIntrinsic3.V128_I4_SHUFFLE:
            // FIXME: I8
            return emit_shuffle(builder, ip, index === SimdIntrinsic3.V128_I2_SHUFFLE ? 8 : 4);
        default:
            return false;
    }

    return false;
}

// implement i16 and i32 shuffles on top of wasm's only shuffle opcode by expanding the
//  element shuffle indices into byte indices
function emit_shuffle(builder: WasmBuilder, ip: MintOpcodePtr, elementCount: number): boolean {
    const elementSize = 16 / elementCount,
        indicesOffset = getArgU16(ip, 3),
        constantIndices = get_known_constant_value(builder, indicesOffset);
    mono_assert((elementSize === 2) || (elementSize === 4), "Unsupported shuffle element size");

    // Pre-load destination ptr
    builder.local("pLocals");
    // Load vec
    append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
    if (typeof (constantIndices) === "object") {
        // HACK: We have a known constant shuffle vector with char or int indices. Expand it to
        //  byte indices and then embed a new constant in the trace.
        const newShuffleVector = new Uint8Array(sizeOfV128),
            nativeIndices = (elementSize === 2)
                ? new Uint16Array(constantIndices.buffer, constantIndices.byteOffset, elementCount)
                : new Uint32Array(constantIndices.buffer, constantIndices.byteOffset, elementCount);
        for (let i = 0, k = 0; i < elementCount; i++, k += elementSize) {
            const elementIndex = nativeIndices[i];
            for (let j = 0; j < elementSize; j++)
                newShuffleVector[k + j] = (elementIndex * elementSize) + j;
        }
        // console.log(`shuffle w/element size ${elementSize} with constant indices ${nativeIndices} (${constantIndices}) -> byte indices ${newShuffleVector}`);
        builder.appendSimd(WasmSimdOpcode.v128_const);
        builder.appendBytes(newShuffleVector);
    } else {
        // Load indices (in chars)
        append_ldloc(builder, indicesOffset, WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
        // There's no direct narrowing opcode for i32 -> i8, so we have to do two steps :(
        if (elementCount === 4) {
            // i32{lane0 ... lane3} -> i16{lane0 ... lane3, 0 ...}
            builder.v128_const(0);
            builder.appendSimd(WasmSimdOpcode.i16x8_narrow_i32x4_u);
        }
        // Load a zero vector (narrow takes two vectors)
        builder.v128_const(0);
        // i16{lane0 ... lane7} -> i8{lane0 ... lane7, 0 ...}
        builder.appendSimd(WasmSimdOpcode.i8x16_narrow_i16x8_u);
        // i8{0, 1, 2, 3 ...} -> i8{0, 0, 1, 1, 2, 2, 3, 3 ...}
        builder.appendSimd(WasmSimdOpcode.v128_const);
        for (let i = 0; i < elementCount; i++) {
            for (let j = 0; j < elementSize; j++)
                builder.appendU8(i);
        }
        builder.appendSimd(WasmSimdOpcode.i8x16_swizzle);
        // multiply indices by 2 to scale from char indices to byte indices
        builder.i32_const(elementCount === 4 ? 2 : 1);
        builder.appendSimd(WasmSimdOpcode.i8x16_shl);
        // now add 1 to the secondary lane of each char
        builder.appendSimd(WasmSimdOpcode.v128_const);
        for (let i = 0; i < elementCount; i++) {
            for (let j = 0; j < elementSize; j++)
                builder.appendU8(j);
        }
    }
    // we now have two vectors on the stack, the values and the byte indices
    builder.appendSimd(WasmSimdOpcode.i8x16_swizzle);
    append_simd_store(builder, ip);
    return true;
}

function emit_simd_4(builder: WasmBuilder, ip: MintOpcodePtr, index: SimdIntrinsic4): boolean {
    const simple = <WasmSimdOpcode>cwraps.mono_jiterp_get_simd_opcode(3, index);
    if (simple >= 0) {
        // [lane count, value load opcode]
        const rtup = simdReplaceTable[index],
            stup = simdStoreTable[index];
        if (Array.isArray(rtup)) {
            const laneCount = rtup[0],
                lane = get_known_constant_value(builder, getArgU16(ip, 3));
            if (typeof (lane) !== "number") {
                mono_log_error(`${builder.functions[0].name}: Non-constant lane index passed to ReplaceScalar`);
                return false;
            } else if ((lane >= laneCount) || (lane < 0)) {
                mono_log_error(`${builder.functions[0].name}: ReplaceScalar index ${lane} out of range (0 - ${laneCount - 1})`);
                return false;
            }

            // arrange stack as [vec, value] and then write replace + lane imm
            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            append_ldloc(builder, getArgU16(ip, 4), rtup[1]);
            builder.appendSimd(simple);
            builder.appendU8(lane);
            append_simd_store(builder, ip);
        } else if (Array.isArray(stup)) {
            // Indirect store where args are [Scalar**, V128*]
            const laneCount = stup[0],
                lane = get_known_constant_value(builder, getArgU16(ip, 4));
            if (typeof (lane) !== "number") {
                mono_log_error(`${builder.functions[0].name}: Non-constant lane index passed to store method`);
                return false;
            } else if ((lane >= laneCount) || (lane < 0)) {
                mono_log_error(`${builder.functions[0].name}: Store lane ${lane} out of range (0 - ${laneCount - 1})`);
                return false;
            }
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.i32_load);
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.appendSimd(simple);
            builder.appendMemarg(0, 0);
            builder.appendU8(lane);
        } else {
            append_simd_4_load(builder, ip);
            builder.appendSimd(simple);
            append_simd_store(builder, ip);
        }
        return true;
    }

    switch (index) {
        case SimdIntrinsic4.V128_CONDITIONAL_SELECT:
            builder.local("pLocals");
            // Wasm spec: result = ior𝑁(iand𝑁(𝑖1, 𝑖3), iand𝑁(𝑖2, inot𝑁(𝑖3)))
            // Our opcode: *arg0 = (*arg2 & *arg1) | (*arg3 & ~*arg1)
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            append_ldloc(builder, getArgU16(ip, 4), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.appendSimd(WasmSimdOpcode.v128_bitselect);
            append_simd_store(builder, ip);
            return true;
        case SimdIntrinsic4.ShuffleD1: {
            const indices = get_known_constant_value(builder, getArgU16(ip, 4));
            if (typeof (indices) !== "object") {
                mono_log_error(`${builder.functions[0].name}: Non-constant indices passed to PackedSimd.Shuffle`);
                return false;
            }
            for (let i = 0; i < 32; i++) {
                const lane = indices[i];
                if ((lane < 0) || (lane > 31)) {
                    mono_log_error(`${builder.functions[0].name}: Shuffle lane index #${i} (${lane}) out of range (0 - 31)`);
                    return false;
                }
            }

            builder.local("pLocals");
            append_ldloc(builder, getArgU16(ip, 2), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            append_ldloc(builder, getArgU16(ip, 3), WasmOpcode.PREFIX_simd, WasmSimdOpcode.v128_load);
            builder.appendSimd(WasmSimdOpcode.i8x16_shuffle);
            builder.appendBytes(indices);
            append_simd_store(builder, ip);
            return true;
        }
        default:
            return false;
    }
}
