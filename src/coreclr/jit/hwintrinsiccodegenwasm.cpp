// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX               Wasm hardware intrinsic Code Generator                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    // emitIns_Lane
    // emitIns_Memarg_Lane

    const HWIntrinsic info(node);
    genConsumeMultiOpOperands(node);

    if (info.codeGenIsTableDriven())
    {
        instruction const ins = HWIntrinsicInfo::lookupIns(info.id, info.baseType, m_compiler);
        assert(ins != INS_invalid);
        switch (info.category)
        {
            case HW_Category_SIMD:
            {
                GetEmitter()->emitIns(ins);
                break;
            }
            case HW_Category_IMM:
            {
                if (info.needsJumpTableFallback())
                {
                    genHWIntrinsicJumpTableFallback(node, info);
                }
                else
                {
                    GetEmitter()->emitIns_Lane(ins, info.GetImmediateLaneOperand());
                }
                break;
            }
            default:
            {
                NYI_WASM_SIMD("CodeGen::genHWIntrinsic: Unsupported category for table-driven intrinsic");
            }
        }
    }
    else
    {
        NYI_WASM_SIMD("!codeGenIsTableDriven");
    }

    WasmProduceReg(node);
}

// genHWIntrinsicJumpTableFallback: Generates a jump table for a given hardware intrinsic node.
// Arguments:
//   node - The hardware intrinsic node
//   info - Hardware intrinsic info about the node
//
// Notes:
/* The structure emitted here is a series of nested blocks that looks like the following:
        (block $outer
          (block $inner
             (block $(N-1)
                ...
                (block $1
                    (block $0
                        (br_table $0 $1 ... $N-1 $inner)
                    )
                    {(local.get lcl_num(arg)) for each non-immediate operand}
                    <op> imm=0
                    br $outer
                )
                  ...
             )
             {(local.get lcl_num(arg)) for each non-immediate operand}
             <op> imm=N-1
             br $outer
          )
          unreachable
        )
   Essentially, we create a block for each possible value of the immediate, and dispatch according to the immediate
   value. In each case of the jump table, we re-materialize the non-immediate operands, whose result values are assigned
   to locals by regalloc. Note that it is safe to emit these blocks mid-instruction stream, since we can logically think
   of them as one macro "instruction" whose result is the same as the underlying instruction we are wrapping. There
   aren't any GC safepoints in any of the generated cases here, but if we have to add any calls/throws/etc. in any of
   the generated cases, this approach will need to change.
*/
void CodeGen::genHWIntrinsicJumpTableFallback(GenTreeHWIntrinsic* node, HWIntrinsic info)
{
    assert(info.category == HW_Category_IMM || info.category == HW_Category_MemoryLoad ||
           info.category == HW_Category_MemoryStore);

    int               simdSize      = node->GetSimdSize();
    instruction const ins           = HWIntrinsicInfo::lookupIns(info.id, info.baseType, m_compiler);
    int               immUpperBound = HWIntrinsicInfo::lookupImmUpperBound(info.id, simdSize, info.baseType);
    WasmValueType     resultType    = ActualTypeToWasmValueType(genActualType(node->TypeGet()));

    GenTree*  immOp  = node->GetImmOp();
    regNumber immReg = GetMultiUseOperandReg(immOp);

    // Drop all incoming operands to actually consume them, they
    // will be unusable in the jump table and we will need to re-materialize them
    // on the value stack for each case in the table.
    for (size_t i = 0; i < node->GetOperandCount(); i++)
    {
        GetEmitter()->emitIns(INS_drop);
    }

    auto getNonImmediateOperands = [=]() {
        for (size_t i = 1; i <= node->GetOperandCount(); i++)
        {
            GenTree* op = node->Op(i);
            if (op != immOp)
            {
                // All of the operands should have been marked MultiplyUsed in lower,
                // and so RA should have assigned them locals which prior codegen should
                // have local.tee'd them into.
                regNumber reg = GetMultiUseOperandReg(op);
                GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(op), WasmRegToIndex(reg));
            }
        }
    };

    genEmitBeginBlock(resultType); // emit $outer block
    genEmitBeginBlock();           // emit unreachable $inner block
    {
        // emit `immUpperBound+1` blocks. The iteration order of the loop doesn't matter here,
        // we just need to stage the right number of blocks on the control flow stack.
        for (int i = 0; i <= immUpperBound; i++)
        {
            // emit a block for each case
            genEmitBeginBlock();
        }

        // In the innermost block, load the immediate value to branch to the appropriate case block
        GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(immOp), WasmRegToIndex(immReg));

        // cases are 0 ... immUpperBound, default, where the last case is the default which branches to the unreachable
        // inner block. Case 0 -> imm = 0, Case 1 -> imm = 1, and so on.
        int caseCount = immUpperBound + 1;
        GetEmitter()->emitIns_I(INS_br_table, EA_4BYTE, caseCount);
        for (int caseNum = 0; caseNum <= immUpperBound; caseNum++)
        {
            GetEmitter()->emitIns_J(INS_label, EA_4BYTE, caseNum, nullptr);
        }
        // emit default case
        GetEmitter()->emitIns_J(INS_label, EA_4BYTE, immUpperBound + 1, nullptr);

        assert(FitsIn<uint8_t>(immUpperBound));
        for (int i = 0; i <= immUpperBound; i++)
        {
            genEmitEndBlock(); // End block for case i. The handling of case i follows

            getNonImmediateOperands();
            switch (info.category)
            {
                case HW_Category_IMM:
                {
                    GetEmitter()->emitIns_Lane(ins, static_cast<uint8_t>(i));
                    break;
                }
                default:
                {
                    NYI_WASM_SIMD(
                        "CodeGen::genHWIntrinsicJumpTableFallback: Unsupported category for jump table intrinsic");
                }
            }
            // proper branch depth is immUpperBound + 1 - i; The $inner block accounts for the + 1.
            GetEmitter()->emitIns_J(INS_br, EA_4BYTE, static_cast<cnsval_ssize_t>(immUpperBound + 1 - i),
                                    nullptr); // br $outer
        }
    }
    genEmitEndBlock(); // end $inner block
    GetEmitter()->emitIns(INS_unreachable);
    genEmitEndBlock(); // end $outer block
}

#endif // FEATURE_HW_INTRINSICS
