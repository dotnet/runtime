// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        ARM/ARM64 Code Generator Common Code               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARMARCH_ // This file is ONLY used for ARM and ARM64 architectures

#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"

//------------------------------------------------------------------------
// genSetRegToIcon: Generate code that will set the given register to the integer constant.
//
void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
}

//---------------------------------------------------------------------
// genIntrinsic - generate code for a given intrinsic
//
// Arguments
//    treeNode - the GT_INTRINSIC node
//
// Return value:
//    None
//
void CodeGen::genIntrinsic(GenTreePtr treeNode)
{
    // Both operand and its result must be of the same floating point type.
    GenTreePtr srcNode = treeNode->gtOp.gtOp1;
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // Right now only Abs/Round/Sqrt are treated as math intrinsics.
    //
    switch (treeNode->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Abs:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_ABS, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Round:
            NYI_ARM("genIntrinsic for round - not implemented yet");
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_ROUND, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Sqrt:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_SQRT, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
}

//---------------------------------------------------------------------
// genPutArgStk - generate code for a GT_PUTARG_STK node
//
// Arguments
//    treeNode - the GT_PUTARG_STK node
//
// Return value:
//    None
//
void CodeGen::genPutArgStk(GenTreePutArgStk* treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);
    var_types  targetType = treeNode->TypeGet();
    GenTreePtr source     = treeNode->gtOp1;
    emitter*   emit       = getEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut    = (unsigned)-1;
    unsigned argOffsetMax = (unsigned)-1; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->gtSlotNum * TARGET_POINTER_SIZE;

#ifdef DEBUG
    fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(treeNode->gtCall, treeNode);
    assert(curArgTabEntry);
    assert(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
#endif // DEBUG

    // Whether to setup stk arg in incoming or out-going arg area?
    // Fast tail calls implemented as epilog+jmp = stk arg is setup in incoming arg area.
    // All other calls - stk arg is setup in out-going arg area.
    if (treeNode->putInIncomingArgArea())
    {
        NYI_ARM("genPutArgStk: fast tail call");

#ifdef _TARGET_ARM64_
        varNumOut    = getFirstArgWithStackSlot();
        argOffsetMax = compiler->compArgSize;
#if FEATURE_FASTTAILCALL
        // This must be a fast tail call.
        assert(treeNode->gtCall->IsFastTailCall());

        // Since it is a fast tail call, the existence of first incoming arg is guaranteed
        // because fast tail call requires that in-coming arg area of caller is >= out-going
        // arg area required for tail call.
        LclVarDsc* varDsc = &(compiler->lvaTable[varNumOut]);
        assert(varDsc != nullptr);
#endif // FEATURE_FASTTAILCALL
#endif // _TARGET_ARM64_
    }
    else
    {
        varNumOut    = compiler->lvaOutgoingArgSpaceVar;
        argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    }

    bool isStruct = (targetType == TYP_STRUCT) || (source->OperGet() == GT_FIELD_LIST);

    if (!isStruct) // a normal non-Struct argument
    {
        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);
            NYI_ARM("genPutArgStk: contained zero source");

#ifdef _TARGET_ARM64_
            emit->emitIns_S_R(storeIns, storeAttr, REG_ZR, varNumOut, argOffsetOut);
#endif // _TARGET_ARM64_
        }
        else
        {
            genConsumeReg(source);
            emit->emitIns_S_R(storeIns, storeAttr, source->gtRegNum, varNumOut, argOffsetOut);
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in Lower

        if (source->OperGet() == GT_FIELD_LIST)
        {
            // Deal with the multi register passed struct args.
            GenTreeFieldList* fieldListPtr = source->AsFieldList();

            // Evaluate each of the GT_FIELD_LIST items into their register
            // and store their register into the outgoing argument area
            for (; fieldListPtr != nullptr; fieldListPtr = fieldListPtr->Rest())
            {
                GenTreePtr nextArgNode = fieldListPtr->gtOp.gtOp1;
                genConsumeReg(nextArgNode);

                regNumber reg  = nextArgNode->gtRegNum;
                var_types type = nextArgNode->TypeGet();
                emitAttr  attr = emitTypeSize(type);

                // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
                // argument area
                emit->emitIns_S_R(ins_Store(type), attr, reg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(attr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }
        }
        else // We must have a GT_OBJ or a GT_LCL_VAR
        {
            noway_assert((source->OperGet() == GT_LCL_VAR) || (source->OperGet() == GT_OBJ));

            NYI_ARM("genPutArgStk: GT_OBJ or GT_LCL_VAR source of struct type");

#ifdef _TARGET_ARM64_

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // We will copy this struct to the stack, possibly using a ldp instruction
            // Setup loReg and hiReg from the internal registers that we reserved in lower.
            //
            regNumber loReg   = treeNode->ExtractTempReg();
            regNumber hiReg   = treeNode->GetSingleTempReg();
            regNumber addrReg = REG_NA;

            GenTreeLclVarCommon* varNode  = nullptr;
            GenTreePtr           addrNode = nullptr;

            if (source->OperGet() == GT_LCL_VAR)
            {
                varNode = source->AsLclVarCommon();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                addrNode = source->gtOp.gtOp1;

                // addrNode can either be a GT_LCL_VAR_ADDR or an address expression
                //
                if (addrNode->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We have a GT_OBJ(GT_LCL_VAR_ADDR)
                    //
                    // We will treat this case the same as above
                    // (i.e if we just had this GT_LCL_VAR directly as the source)
                    // so update 'source' to point this GT_LCL_VAR_ADDR node
                    // and continue to the codegen for the LCL_VAR node below
                    //
                    varNode  = addrNode->AsLclVarCommon();
                    addrNode = nullptr;
                }
            }

            // Either varNode or addrNOde must have been setup above,
            // the xor ensures that only one of the two is setup, not both
            assert((varNode != nullptr) ^ (addrNode != nullptr));

            BYTE     gcPtrs[MAX_ARG_REG_COUNT] = {}; // TYPE_GC_NONE = 0
            unsigned gcPtrCount;                     // The count of GC pointers in the struct
            int      structSize;
            bool     isHfa;

            // This is the varNum for our load operations,
            // only used when we have a multireg struct with a LclVar source
            unsigned varNumInp = BAD_VAR_NUM;

            // Setup the structSize, isHFa, and gcPtrCount
            if (varNode != nullptr)
            {
                varNumInp = varNode->gtLclNum;
                assert(varNumInp < compiler->lvaCount);
                LclVarDsc* varDsc = &compiler->lvaTable[varNumInp];

                assert(varDsc->lvType == TYP_STRUCT);
                assert(varDsc->lvOnFrame);   // This struct also must live in the stack frame
                assert(!varDsc->lvRegister); // And it can't live in a register (SIMD)

                structSize = varDsc->lvSize(); // This yields the roundUp size, but that is fine
                                               // as that is how much stack is allocated for this LclVar
                isHfa      = varDsc->lvIsHfa();
                gcPtrCount = varDsc->lvStructGcCount;
                for (unsigned i = 0; i < gcPtrCount; ++i)
                    gcPtrs[i]   = varDsc->lvGcLayout[i];
            }
            else // addrNode is used
            {
                assert(addrNode != nullptr);

                // Generate code to load the address that we need into a register
                genConsumeAddress(addrNode);
                addrReg = addrNode->gtRegNum;

                CORINFO_CLASS_HANDLE objClass = source->gtObj.gtClass;

                structSize = compiler->info.compCompHnd->getClassSize(objClass);
                isHfa      = compiler->IsHfa(objClass);
                gcPtrCount = compiler->info.compCompHnd->getClassGClayout(objClass, &gcPtrs[0]);
            }

            bool hasGCpointers = (gcPtrCount > 0); // true if there are any GC pointers in the struct

            // If we have an HFA we can't have any GC pointers,
            // if not then the max size for the the struct is 16 bytes
            if (isHfa)
            {
                noway_assert(gcPtrCount == 0);
            }
            else
            {
                noway_assert(structSize <= 2 * TARGET_POINTER_SIZE);
            }

            noway_assert(structSize <= MAX_PASS_MULTIREG_BYTES);

            // For a 16-byte structSize with GC pointers we will use two ldr and two str instructions
            //             ldr     x2, [x0]
            //             ldr     x3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //
            // For a 16-byte structSize with no GC pointers we will use a ldp and two str instructions
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //
            // For a 32-byte structSize with no GC pointers we will use two ldp and four str instructions
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #32]
            //             str     x3, [sp, #40]
            //
            // Note that when loading from a varNode we currently can't use the ldp instruction
            // TODO-ARM64-CQ: Implement support for using a ldp instruction with a varNum (see emitIns_R_S)
            //

            int      remainingSize = structSize;
            unsigned structOffset  = 0;
            unsigned nextIndex     = 0;

            while (remainingSize >= 2 * TARGET_POINTER_SIZE)
            {
                var_types type0 = compiler->getJitGCType(gcPtrs[nextIndex + 0]);
                var_types type1 = compiler->getJitGCType(gcPtrs[nextIndex + 1]);

                if (hasGCpointers)
                {
                    // We have GC pointers, so use two ldr instructions
                    //
                    // We must do it this way because we can't currently pass or track
                    // two different emitAttr values for a ldp instruction.

                    // Make sure that the first load instruction does not overwrite the addrReg.
                    //
                    if (loReg != addrReg)
                    {
                        if (varNode != nullptr)
                        {
                            // Load from our varNumImp source
                            emit->emitIns_R_S(ins_Load(type0), emitTypeSize(type0), loReg, varNumInp, 0);
                            emit->emitIns_R_S(ins_Load(type1), emitTypeSize(type1), hiReg, varNumInp,
                                              TARGET_POINTER_SIZE);
                        }
                        else
                        {
                            // Load from our address expression source
                            emit->emitIns_R_R_I(ins_Load(type0), emitTypeSize(type0), loReg, addrReg, structOffset);
                            emit->emitIns_R_R_I(ins_Load(type1), emitTypeSize(type1), hiReg, addrReg,
                                                structOffset + TARGET_POINTER_SIZE);
                        }
                    }
                    else // loReg == addrReg
                    {
                        assert(varNode == nullptr); // because addrReg is REG_NA when varNode is non-null
                        assert(hiReg != addrReg);
                        // Load from our address expression source
                        emit->emitIns_R_R_I(ins_Load(type1), emitTypeSize(type1), hiReg, addrReg,
                                            structOffset + TARGET_POINTER_SIZE);
                        emit->emitIns_R_R_I(ins_Load(type0), emitTypeSize(type0), loReg, addrReg, structOffset);
                    }
                }
                else // our struct has no GC pointers
                {
                    if (varNode != nullptr)
                    {
                        // Load from our varNumImp source, currently we can't use a ldp instruction to do this
                        emit->emitIns_R_S(ins_Load(type0), emitTypeSize(type0), loReg, varNumInp, 0);
                        emit->emitIns_R_S(ins_Load(type1), emitTypeSize(type1), hiReg, varNumInp, TARGET_POINTER_SIZE);
                    }
                    else
                    {
                        // Use a ldp instruction

                        // Load from our address expression source
                        emit->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, loReg, hiReg, addrReg, structOffset);
                    }
                }

                // Emit two store instructions to store the two registers into the outgoing argument area
                emit->emitIns_S_R(ins_Store(type0), emitTypeSize(type0), loReg, varNumOut, argOffsetOut);
                emit->emitIns_S_R(ins_Store(type1), emitTypeSize(type1), hiReg, varNumOut,
                                  argOffsetOut + TARGET_POINTER_SIZE);
                argOffsetOut += (2 * TARGET_POINTER_SIZE); // We stored 16-bytes of the struct
                assert(argOffsetOut <= argOffsetMax);      // We can't write beyound the outgoing area area

                remainingSize -= (2 * TARGET_POINTER_SIZE); // We loaded 16-bytes of the struct
                structOffset += (2 * TARGET_POINTER_SIZE);
                nextIndex += 2;
            }

            // For a 12-byte structSize we will we will generate two load instructions
            //             ldr     x2, [x0]
            //             ldr     w3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     w3, [sp, #24]
            //
            // When the first instruction has a loReg that is the same register as the addrReg,
            //  we set deferLoad to true and issue the intructions in the reverse order
            //             ldr     x3, [x2, #8]
            //             ldr     x2, [x2]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //

            var_types nextType = compiler->getJitGCType(gcPtrs[nextIndex]);
            emitAttr  nextAttr = emitTypeSize(nextType);
            regNumber curReg   = loReg;

            bool      deferLoad   = false;
            var_types deferType   = TYP_UNKNOWN;
            emitAttr  deferAttr   = EA_PTRSIZE;
            int       deferOffset = 0;

            while (remainingSize > 0)
            {
                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    remainingSize -= TARGET_POINTER_SIZE;

                    if ((curReg == addrReg) && (remainingSize != 0))
                    {
                        deferLoad   = true;
                        deferType   = nextType;
                        deferAttr   = emitTypeSize(nextType);
                        deferOffset = structOffset;
                    }
                    else // the typical case
                    {
                        if (varNode != nullptr)
                        {
                            // Load from our varNumImp source
                            emit->emitIns_R_S(ins_Load(nextType), nextAttr, curReg, varNumInp, structOffset);
                        }
                        else
                        {
                            // Load from our address expression source
                            emit->emitIns_R_R_I(ins_Load(nextType), nextAttr, curReg, addrReg, structOffset);
                        }
                        // Emit a store instruction to store the register into the outgoing argument area
                        emit->emitIns_S_R(ins_Store(nextType), nextAttr, curReg, varNumOut, argOffsetOut);
                        argOffsetOut += EA_SIZE_IN_BYTES(nextAttr);
                        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
                    }
                    curReg = hiReg;
                    structOffset += TARGET_POINTER_SIZE;
                    nextIndex++;
                    nextType = compiler->getJitGCType(gcPtrs[nextIndex]);
                    nextAttr = emitTypeSize(nextType);
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    int loadSize  = remainingSize;
                    remainingSize = 0;

                    // We should never have to do a non-pointer sized load when we have a LclVar source
                    assert(varNode == nullptr);

                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(varTypeIsGC(nextType) == false);

                    var_types loadType = TYP_UINT;
                    if (loadSize == 1)
                    {
                        loadType = TYP_UBYTE;
                    }
                    else if (loadSize == 2)
                    {
                        loadType = TYP_USHORT;
                    }
                    else
                    {
                        // Need to handle additional loadSize cases here
                        noway_assert(loadSize == 4);
                    }

                    instruction loadIns  = ins_Load(loadType);
                    emitAttr    loadAttr = emitAttr(loadSize);

                    // When deferLoad is false, curReg can be the same as addrReg
                    // because the last instruction is allowed to overwrite addrReg.
                    //
                    noway_assert(!deferLoad || (curReg != addrReg));

                    emit->emitIns_R_R_I(loadIns, loadAttr, curReg, addrReg, structOffset);

                    // Emit a store instruction to store the register into the outgoing argument area
                    emit->emitIns_S_R(ins_Store(loadType), loadAttr, curReg, varNumOut, argOffsetOut);
                    argOffsetOut += EA_SIZE_IN_BYTES(loadAttr);
                    assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
                }
            }

            if (deferLoad)
            {
                // We should never have to do a deferred load when we have a LclVar source
                assert(varNode == nullptr);

                curReg = addrReg;

                // Load from our address expression source
                emit->emitIns_R_R_I(ins_Load(deferType), deferAttr, curReg, addrReg, deferOffset);

                // Emit a store instruction to store the register into the outgoing argument area
                emit->emitIns_S_R(ins_Store(nextType), nextAttr, curReg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(nextAttr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }

#endif // _TARGET_ARM64_
        }
    }
}

//----------------------------------------------------------------------------------
// genMultiRegCallStoreToLocal: store multi-reg return value of a call node to a local
//
// Arguments:
//    treeNode  -  Gentree of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
// Assumption:
//    The child of store is a multi-reg call node.
//    genProduceReg() on treeNode is made by caller of this routine.
//
void CodeGen::genMultiRegCallStoreToLocal(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_VAR);

#if defined(_TARGET_ARM_)
    // Longs are returned in two return registers on Arm32.
    assert(varTypeIsLong(treeNode));
#elif defined(_TARGET_ARM64_)
    // Structs of size >=9 and <=16 are returned in two return registers on ARM64 and HFAs.
    assert(varTypeIsStruct(treeNode));
#endif // _TARGET_*

    // Assumption: current implementation requires that a multi-reg
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted.
    unsigned   lclNum = treeNode->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[lclNum]);
    noway_assert(varDsc->lvIsMultiRegRet);

    GenTree*     op1       = treeNode->gtGetOp1();
    GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
    GenTreeCall* call      = actualOp1->AsCall();
    assert(call->HasMultiRegRetVal());

    genConsumeRegs(op1);

    ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
    unsigned        regCount     = pRetTypeDesc->GetReturnRegCount();

    if (treeNode->gtRegNum != REG_NA)
    {
        // Right now the only enregistrable multi-reg return types supported are SIMD types.
        assert(varTypeIsSIMD(treeNode));
        NYI("GT_STORE_LCL_VAR of a SIMD enregisterable struct");
    }
    else
    {
        // Stack store
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type = pRetTypeDesc->GetReturnRegType(i);
            regNumber reg  = call->GetRegNumByIdx(i);
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
                if (reloadReg != REG_NA)
                {
                    reg = reloadReg;
                }
            }

            assert(reg != REG_NA);
            getEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);
            offset += genTypeSize(type);
        }

        varDsc->lvRegNum = REG_STK;
    }
}

//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_ARR_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTreePtr oper)
{
#ifdef FEATURE_SIMD
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK || oper->OperGet() == GT_SIMD_CHK);
#else  // !FEATURE_SIMD
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK);
#endif // !FEATURE_SIMD

    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTreePtr arrLen    = bndsChk->gtArrLen;
    GenTreePtr arrIndex  = bndsChk->gtIndex;
    GenTreePtr arrRef    = NULL;
    int        lenOffset = 0;

    GenTree*     src1;
    GenTree*     src2;
    emitJumpKind jmpKind;

    genConsumeRegs(arrIndex);
    genConsumeRegs(arrLen);

    if (arrIndex->isContainedIntOrIImmed())
    {
        // To encode using a cmp immediate, we place the
        //  constant operand in the second position
        src1    = arrLen;
        src2    = arrIndex;
        jmpKind = genJumpKindForOper(GT_LE, CK_UNSIGNED);
    }
    else
    {
        src1    = arrIndex;
        src2    = arrLen;
        jmpKind = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    }

    getEmitter()->emitInsBinary(INS_cmp, EA_4BYTE, src1, src2);
    genJumpToThrowHlpBlk(jmpKind, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLowerBound: Returns the offset from the Array object to the
//   lower bound for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayLowerBound(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * (dimension + rank);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLength: Returns the offset from the Array object to the
//   size for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayDimensionSize(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * dimension;
}

//------------------------------------------------------------------------
// genCodeForArrIndex: Generates code to bounds check the index for one dimension of an array reference,
//                     producing the effective index by subtracting the lower bound.
//
// Arguments:
//    arrIndex - the node for which we're generating code
//
// Return Value:
//    None.
//
void CodeGen::genCodeForArrIndex(GenTreeArrIndex* arrIndex)
{
    emitter*   emit      = getEmitter();
    GenTreePtr arrObj    = arrIndex->ArrObj();
    GenTreePtr indexNode = arrIndex->IndexExpr();
    regNumber  arrReg    = genConsumeReg(arrObj);
    regNumber  indexReg  = genConsumeReg(indexNode);
    regNumber  tgtReg    = arrIndex->gtRegNum;
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values.

    regNumber tmpReg = arrIndex->GetSingleTempReg();
    assert(tgtReg != tmpReg);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    var_types elemType = arrIndex->gtArrElemType;
    unsigned  offset;

    offset = genOffsetOfMDArrayLowerBound(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R_R(INS_sub, EA_4BYTE, tgtReg, indexReg, tmpReg);

    offset = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R(INS_cmp, EA_4BYTE, tgtReg, tmpReg);

    emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    genJumpToThrowHlpBlk(jmpGEU, SCK_RNGCHK_FAIL);

    genProduceReg(arrIndex);
}

//------------------------------------------------------------------------
// genCodeForArrOffset: Generates code to compute the flattened array offset for
//    one dimension of an array reference:
//        result = (prevDimOffset * dimSize) + effectiveIndex
//    where dimSize is obtained from the arrObj operand
//
// Arguments:
//    arrOffset - the node for which we're generating code
//
// Return Value:
//    None.
//
// Notes:
//    dimSize and effectiveIndex are always non-negative, the former by design,
//    and the latter because it has been normalized to be zero-based.

void CodeGen::genCodeForArrOffset(GenTreeArrOffs* arrOffset)
{
    GenTreePtr offsetNode = arrOffset->gtOffset;
    GenTreePtr indexNode  = arrOffset->gtIndex;
    regNumber  tgtReg     = arrOffset->gtRegNum;

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*  emit      = getEmitter();
        regNumber offsetReg = genConsumeReg(offsetNode);
        regNumber indexReg  = genConsumeReg(indexNode);
        regNumber arrReg    = genConsumeReg(arrOffset->gtArrObj);
        noway_assert(offsetReg != REG_NA);
        noway_assert(indexReg != REG_NA);
        noway_assert(arrReg != REG_NA);

        regNumber tmpReg = arrOffset->GetSingleTempReg();

        unsigned  dim      = arrOffset->gtCurrDim;
        unsigned  rank     = arrOffset->gtArrRank;
        var_types elemType = arrOffset->gtArrElemType;
        unsigned  offset   = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);

        // Load tmpReg with the dimension size and evaluate
        // tgtReg = offsetReg*tmpReg + indexReg.
        emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset);
        emit->emitIns_R_R_R_R(INS_MULADD, EA_PTRSIZE, tgtReg, tmpReg, offsetReg, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        if (indexReg != tgtReg)
        {
            inst_RV_RV(INS_mov, tgtReg, indexReg, TYP_INT);
        }
    }
    genProduceReg(arrOffset);
}

//------------------------------------------------------------------------
// indirForm: Make a temporary indir we can feed to pattern matching routines
//    in cases where we don't want to instantiate all the indirs that happen.
//
GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
{
    GenTreeIndir i(GT_IND, type, base, nullptr);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

//------------------------------------------------------------------------
// intForm: Make a temporary int we can feed to pattern matching routines
//    in cases where we don't want to instantiate.
//
GenTreeIntCon CodeGen::intForm(var_types type, ssize_t value)
{
    GenTreeIntCon i(type, value);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

//------------------------------------------------------------------------
// genCodeForShift: Generates the code sequence for a GenTree node that
// represents a bit shift or rotate operation (<<, >>, >>>, rol, ror).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//
void CodeGen::genCodeForShift(GenTreePtr tree)
{
    var_types   targetType = tree->TypeGet();
    genTreeOps  oper       = tree->OperGet();
    instruction ins        = genGetInsForOper(oper, targetType);
    emitAttr    size       = emitTypeSize(tree);

    assert(tree->gtRegNum != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTreePtr operand = tree->gtGetOp1();
    GenTreePtr shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        getEmitter()->emitIns_R_R_R(ins, size, tree->gtRegNum, operand->gtRegNum, shiftBy->gtRegNum);
    }
    else
    {
        unsigned immWidth   = emitter::getBitWidth(size); // For ARM64, immWidth will be set to 32 or 64
        ssize_t  shiftByImm = shiftBy->gtIntCon.gtIconVal & (immWidth - 1);

        getEmitter()->emitIns_R_R_I(ins, size, tree->gtRegNum, operand->gtRegNum, shiftByImm);
    }

    genProduceReg(tree);
}

// Generate code for a CpBlk node by the means of the VM memcpy helper call
// Preconditions:
// a) The size argument of the CpBlk is not an integer constant
// b) The size argument is a constant but is larger than CPBLK_MOVS_LIMIT bytes.
void CodeGen::genCodeForCpBlk(GenTreeBlk* cpBlkNode)
{
    // Make sure we got the arguments of the cpblk operation in the right registers
    unsigned   blockSize = cpBlkNode->Size();
    GenTreePtr dstAddr   = cpBlkNode->Addr();
    assert(!dstAddr->isContained());

    genConsumeBlockOp(cpBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

#ifdef _TARGET_ARM64_
    if (blockSize != 0)
    {
        assert(blockSize > CPBLK_UNROLL_LIMIT);
    }
#endif // _TARGET_ARM64_

    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);
}

// Generates code for InitBlk by calling the VM memset helper function.
// Preconditions:
// a) The size argument of the InitBlk is not an integer constant.
// b) The size argument of the InitBlk is >= INITBLK_STOS_LIMIT bytes.
void CodeGen::genCodeForInitBlk(GenTreeBlk* initBlkNode)
{
    // Make sure we got the arguments of the initblk operation in the right registers
    unsigned   size    = initBlkNode->Size();
    GenTreePtr dstAddr = initBlkNode->Addr();
    GenTreePtr initVal = initBlkNode->Data();
    if (initVal->OperIsInitVal())
    {
        initVal = initVal->gtGetOp1();
    }

    assert(!dstAddr->isContained());
    assert(!initVal->isContained());
    if (initBlkNode->gtOper == GT_STORE_DYN_BLK)
    {
        assert(initBlkNode->AsDynBlk()->gtDynamicSize->gtRegNum == REG_ARG_2);
    }
    else
    {
        assert(initBlkNode->gtRsvdRegs == RBM_ARG_2);
    }

#ifdef _TARGET_ARM64_
    if (size != 0)
    {
        assert(size > INITBLK_UNROLL_LIMIT);
    }
#endif // _TARGET_ARM64_

    genConsumeBlockOp(initBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);
    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
}

//------------------------------------------------------------------------
// genRegCopy: Generate a register copy.
//
void CodeGen::genRegCopy(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_COPY);

    var_types targetType = treeNode->TypeGet();
    regNumber targetReg  = treeNode->gtRegNum;
    assert(targetReg != REG_NA);

    GenTree* op1 = treeNode->gtOp.gtOp1;

    // Check whether this node and the node from which we're copying the value have the same
    // register type.
    // This can happen if (currently iff) we have a SIMD vector type that fits in an integer
    // register, in which case it is passed as an argument, or returned from a call,
    // in an integer register and must be copied if it's in an xmm register.

    if (varTypeIsFloating(treeNode) != varTypeIsFloating(op1))
    {
        NYI_ARM("genRegCopy floating point");
#ifdef _TARGET_ARM64_
        inst_RV_RV(INS_fmov, targetReg, genConsumeReg(op1), targetType);
#endif // _TARGET_ARM64_
    }
    else
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, genConsumeReg(op1), targetType);
    }

    if (op1->IsLocal())
    {
        // The lclVar will never be a def.
        // If it is a last use, the lclVar will be killed by genConsumeReg(), as usual, and genProduceReg will
        // appropriately set the gcInfo for the copied value.
        // If not, there are two cases we need to handle:
        // - If this is a TEMPORARY copy (indicated by the GTF_VAR_DEATH flag) the variable
        //   will remain live in its original register.
        //   genProduceReg() will appropriately set the gcInfo for the copied value,
        //   and genConsumeReg will reset it.
        // - Otherwise, we need to update register info for the lclVar.

        GenTreeLclVarCommon* lcl = op1->AsLclVarCommon();
        assert((lcl->gtFlags & GTF_VAR_DEF) == 0);

        if ((lcl->gtFlags & GTF_VAR_DEATH) == 0 && (treeNode->gtFlags & GTF_VAR_DEATH) == 0)
        {
            LclVarDsc* varDsc = &compiler->lvaTable[lcl->gtLclNum];

            // If we didn't just spill it (in genConsumeReg, above), then update the register info
            if (varDsc->lvRegNum != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));

                gcInfo.gcMarkRegSetNpt(genRegMask(op1->gtRegNum));

                genUpdateVarReg(varDsc, treeNode);

                // The new location is going live
                genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCallInstruction: Produce code for a GT_CALL node
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    IL_OFFSETX ilOffset = BAD_IL_OFFSET;

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Consume all the arg regs
    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode->gtSkipReloadOrCopy());
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            GenTreeArgList* argListPtr   = argNode->AsArgList();
            unsigned        iterationNum = 0;
            regNumber       argReg       = curArgTabEntry->regNum;
            for (; argListPtr != nullptr; argListPtr = argListPtr->Rest(), iterationNum++)
            {
                GenTreePtr putArgRegNode = argListPtr->gtOp.gtOp1;
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);

                if (putArgRegNode->gtRegNum != argReg)
                {
                    inst_RV_RV(ins_Move_Extend(putArgRegNode->TypeGet(), putArgRegNode->InReg()), argReg,
                               putArgRegNode->gtRegNum);
                }

                argReg = genRegArgNext(argReg);
            }
        }
        else
        {
            regNumber argReg = curArgTabEntry->regNum;
            genConsumeReg(argNode);
            if (argNode->gtRegNum != argReg)
            {
                inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), argNode->InReg()), argReg, argNode->gtRegNum);
            }
        }

        // In the case of a varargs call,
        // the ABI dictates that if we have floating point args,
        // we must pass the enregistered arguments in both the
        // integer and floating point registers so, let's do that.
        if (call->IsVarargs() && varTypeIsFloating(argNode))
        {
            NYI_ARM("CodeGen - IsVarargs");
            NYI_ARM64("CodeGen - IsVarargs");
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

#if defined(_TARGET_ARM_)
        const regNumber tmpReg = call->ExtractTempReg();
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, regThis, 0);
#elif defined(_TARGET_ARM64_)
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, regThis, 0);
#endif // _TARGET_*
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCallAddr;
        methHnd = nullptr;
    }
    else
    {
        methHnd = call->gtCallMethHnd;
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (callType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    // If fast tail call, then we are done.  In this case we setup the args (both reg args
    // and stack args in incoming arg area) and call target.  Epilog sequence would
    // generate "br <reg>".
    if (call->IsFastTailCall())
    {
        // Don't support fast tail calling JIT helpers
        assert(callType != CT_HELPER);

        // Fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
        assert(target != nullptr);

        genConsumeReg(target);

        NYI_ARM("fast tail call");

#ifdef _TARGET_ARM64_
        // Use IP0 as the call target register.
        if (target->gtRegNum != REG_IP0)
        {
            inst_RV_RV(INS_mov, REG_IP0, target->gtRegNum);
        }
#endif // _TARGET_ARM64_

        return;
    }

    // For a pinvoke to unmanaged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (call->IsUnmanaged())
    {
        genDefineTempLabel(genCreateTempLabel());
    }

    // Determine return value size(s).
    ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr        retSize       = EA_PTRSIZE;
    emitAttr        secondRetSize = EA_UNKNOWN;

    if (call->HasMultiRegRetVal())
    {
        retSize       = emitTypeSize(pRetTypeDesc->GetReturnRegType(0));
        secondRetSize = emitTypeSize(pRetTypeDesc->GetReturnRegType(1));
    }
    else
    {
        assert(!varTypeIsStruct(call));

        if (call->gtType == TYP_REF || call->gtType == TYP_ARRAY)
        {
            retSize = EA_GCREF;
        }
        else if (call->gtType == TYP_BYREF)
        {
            retSize = EA_BYREF;
        }
    }

    // We need to propagate the IL offset information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2ILOffsetMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2ILOffsetMap->Lookup(call, &ilOffset);
    }

    if (target != nullptr)
    {
        // A call target can not be a contained indirection
        assert(!target->isContainedIndir());

        genConsumeReg(target);

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->gtRegNum));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset, target->gtRegNum);
    }
    else
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);

        void* addr = nullptr;
        if (callType == CT_HELPER)
        {
            // Direct call to a helper method.
            CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(methHnd);
            noway_assert(helperNum != CORINFO_HELP_UNDEF);

            void* pAddr = nullptr;
            addr        = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);

            if (addr == nullptr)
            {
                addr = pAddr;
            }
        }
        else
        {
            // Direct call to a non-virtual user function.
            CORINFO_ACCESS_FLAGS aflags = CORINFO_ACCESS_ANY;
            if (call->IsSameThis())
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);
            }

            if ((call->NeedsNullCheck()) == 0)
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);
            }

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo, aflags);

            addr = addrInfo.addr;
        }

        assert(addr != nullptr);

// Non-virtual direct call to known addresses
#ifdef _TARGET_ARM_
        if (!arm_Valid_Imm_For_BL((ssize_t)addr))
        {
            regNumber tmpReg = call->GetSingleTempReg();
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, tmpReg, (ssize_t)addr);
            genEmitCall(emitter::EC_INDIR_R, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) NULL, retSize, ilOffset, tmpReg);
        }
        else
#endif // _TARGET_ARM_
        {
            genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr,
                        retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset);
        }

#if 0 && defined(_TARGET_ARM64_)
        // Use this path if you want to load an absolute call target using 
        //  a sequence of movs followed by an indirect call (blr instruction)

        // Load the call target address in x16
        instGen_Set_Reg_To_Imm(EA_8BYTE, REG_IP0, (ssize_t) addr);

        // indirect call to constant address in IP0
        genEmitCall(emitter::EC_INDIR_R,
                    methHnd, 
                    INDEBUG_LDISASM_COMMA(sigInfo)
                    nullptr, //addr
                    retSize,
                    secondRetSize,
                    ilOffset,
                    REG_IP0);
#endif
    }

    // if it was a pinvoke we may have needed to get the address of a label
    if (genPendingCallLabel)
    {
        assert(call->IsUnmanaged());
        genDefineTempLabel(genPendingCallLabel);
        genPendingCallLabel = nullptr;
    }

    // Update GC info:
    // All Callee arg registers are trashed and no longer contain any GC pointers.
    // TODO-Bug?: As a matter of fact shouldn't we be killing all of callee trashed regs here?
    // For now we will assert that other than arg regs gc ref/byref set doesn't contain any other
    // registers from RBM_CALLEE_TRASH
    assert((gcInfo.gcRegGCrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    assert((gcInfo.gcRegByrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    gcInfo.gcRegGCrefSetCur &= ~RBM_ARG_REGS;
    gcInfo.gcRegByrefSetCur &= ~RBM_ARG_REGS;

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
        regNumber returnReg;

        if (call->HasMultiRegRetVal())
        {
            assert(pRetTypeDesc != nullptr);
            unsigned regCount = pRetTypeDesc->GetReturnRegCount();

            // If regs allocated to call node are different from ABI return
            // regs in which the call has returned its result, move the result
            // to regs allocated to call node.
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types regType      = pRetTypeDesc->GetReturnRegType(i);
                returnReg              = pRetTypeDesc->GetABIReturnReg(i);
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                if (returnReg != allocatedReg)
                {
                    inst_RV_RV(ins_Copy(regType), allocatedReg, returnReg, regType);
                }
            }
        }
        else
        {
#ifdef _TARGET_ARM_
            if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
            {
                // The CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
                // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
                returnReg = REG_PINVOKE_TCB;
            }
            else
#endif // _TARGET_ARM_
                if (varTypeIsFloating(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->gtRegNum != returnReg)
            {
                inst_RV_RV(ins_Copy(returnType), call->gtRegNum, returnReg, returnType);
            }
        }

        genProduceReg(call);
    }

    // If there is nothing next, that means the result is thrown away, so this value is not live.
    // However, for minopts or debuggable code, we keep it live to support managed return value debugging.
    if ((call->gtNext == nullptr) && !compiler->opts.MinOpts() && !compiler->opts.compDbgCode)
    {
        gcInfo.gcMarkRegSetNpt(RBM_INTRET);
    }
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The treeNode must have an assigned register.
//    For a signed convert from byte, the source must be in a byte-addressable register.
//    Neither the source nor target type can be a floating point type.
//
// TODO-ARM64-CQ: Allow castOp to be a contained node without an assigned register.
//
void CodeGen::genIntToIntCast(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_CAST);

    GenTreePtr castOp = treeNode->gtCast.CastOp();
    emitter*   emit   = getEmitter();

    var_types dstType     = treeNode->CastToType();
    var_types srcType     = genActualType(castOp->TypeGet());
    emitAttr  movSize     = emitActualTypeSize(dstType);
    bool      movRequired = false;

#ifdef _TARGET_ARM_
    if (varTypeIsLong(srcType))
    {
        genLongToIntCast(treeNode);
        return;
    }
#endif // _TARGET_ARM_

    regNumber targetReg = treeNode->gtRegNum;
    regNumber sourceReg = castOp->gtRegNum;

    // For Long to Int conversion we will have a reserved integer register to hold the immediate mask
    regNumber tmpReg = (treeNode->AvailableTempRegCount() == 0) ? REG_NA : treeNode->GetSingleTempReg();

    assert(genIsValidIntReg(targetReg));
    assert(genIsValidIntReg(sourceReg));

    instruction ins = INS_invalid;

    genConsumeReg(castOp);
    Lowering::CastInfo castInfo;

    // Get information about the cast.
    Lowering::getCastDescription(treeNode, &castInfo);

    if (castInfo.requiresOverflowCheck)
    {
        emitAttr cmpSize = EA_ATTR(genTypeSize(srcType));

        if (castInfo.signCheckOnly)
        {
            // We only need to check for a negative value in sourceReg
            emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, 0);
            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
            noway_assert(genTypeSize(srcType) == 4 || genTypeSize(srcType) == 8);
            // This is only interesting case to ensure zero-upper bits.
            if ((srcType == TYP_INT) && (dstType == TYP_ULONG))
            {
                // cast to TYP_ULONG:
                // We use a mov with size=EA_4BYTE
                // which will zero out the upper bits
                movSize     = EA_4BYTE;
                movRequired = true;
            }
        }
        else if (castInfo.unsignedSource || castInfo.unsignedDest)
        {
            // When we are converting from/to unsigned,
            // we only have to check for any bits set in 'typeMask'

            noway_assert(castInfo.typeMask != 0);
#if defined(_TARGET_ARM_)
            if (arm_Valid_Imm_For_Instr(INS_tst, castInfo.typeMask, INS_FLAGS_DONT_CARE))
            {
                emit->emitIns_R_I(INS_tst, cmpSize, sourceReg, castInfo.typeMask);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMask);
                emit->emitIns_R_R(INS_tst, cmpSize, sourceReg, tmpReg);
            }
#elif defined(_TARGET_ARM64_)
            emit->emitIns_R_I(INS_tst, cmpSize, sourceReg, castInfo.typeMask);
#endif // _TARGET_ARM*
            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
        }
        else
        {
            // For a narrowing signed cast
            //
            // We must check the value is in a signed range.

            // Compare with the MAX

            noway_assert((castInfo.typeMin != 0) && (castInfo.typeMax != 0));

#if defined(_TARGET_ARM_)
            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, INS_FLAGS_DONT_CARE))
#elif defined(_TARGET_ARM64_)
            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, cmpSize))
#endif // _TARGET_*
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMax);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMax);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpGT = genJumpKindForOper(GT_GT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpGT, SCK_OVERFLOW);

// Compare with the MIN

#if defined(_TARGET_ARM_)
            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, INS_FLAGS_DONT_CARE))
#elif defined(_TARGET_ARM64_)
            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, cmpSize))
#endif // _TARGET_*
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMin);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMin);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
        }
        ins = INS_mov;
    }
    else // Non-overflow checking cast.
    {
        if (genTypeSize(srcType) == genTypeSize(dstType))
        {
            ins = INS_mov;
        }
        else
        {
            var_types extendType = TYP_UNKNOWN;

            // If we need to treat a signed type as unsigned
            if ((treeNode->gtFlags & GTF_UNSIGNED) != 0)
            {
                extendType  = genUnsignedType(srcType);
                movSize     = emitTypeSize(extendType);
                movRequired = true;
            }
            else
            {
                if (genTypeSize(srcType) < genTypeSize(dstType))
                {
                    extendType = srcType;
#ifdef _TARGET_ARM_
                    movSize = emitTypeSize(srcType);
#endif // _TARGET_ARM_
                    if (srcType == TYP_UINT)
                    {
#ifdef _TARGET_ARM64_
                        // If we are casting from a smaller type to
                        // a larger type, then we need to make sure the
                        // higher 4 bytes are zero to gaurentee the correct value.
                        // Therefore using a mov with EA_4BYTE in place of EA_8BYTE
                        // will zero the upper bits
                        movSize = EA_4BYTE;
#endif // _TARGET_ARM64_
                        movRequired = true;
                    }
                }
                else // (genTypeSize(srcType) > genTypeSize(dstType))
                {
                    extendType = dstType;
#if defined(_TARGET_ARM_)
                    movSize = emitTypeSize(dstType);
#elif defined(_TARGET_ARM64_)
                    if (dstType == TYP_INT)
                    {
                        movSize = EA_8BYTE; // a sxtw instruction requires EA_8BYTE
                    }
#endif // _TARGET_*
                }
            }

            ins = ins_Move_Extend(extendType, castOp->InReg());
        }
    }

    // We should never be generating a load from memory instruction here!
    assert(!emit->emitInsIsLoad(ins));

    if ((ins != INS_mov) || movRequired || (targetReg != sourceReg))
    {
        emit->emitIns_R_R(ins, movSize, targetReg, sourceReg);
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genFloatToFloatCast: Generate code for a cast between float and double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    The cast is between float and double.
//
void CodeGen::genFloatToFloatCast(GenTreePtr treeNode)
{
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

#if defined(_TARGET_ARM_)

    if (srcType != dstType)
    {
        instruction insVcvt = (srcType == TYP_FLOAT) ? INS_vcvt_f2d  // convert Float to Double
                                                     : INS_vcvt_d2f; // convert Double to Float

        getEmitter()->emitIns_R_R(insVcvt, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        getEmitter()->emitIns_R_R(INS_vmov, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

#elif defined(_TARGET_ARM64_)

    if (srcType != dstType)
    {
        insOpts cvtOption = (srcType == TYP_FLOAT) ? INS_OPTS_S_TO_D  // convert Single to Double
                                                   : INS_OPTS_D_TO_S; // convert Double to Single

        getEmitter()->emitIns_R_R(INS_fcvt, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum, cvtOption);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        getEmitter()->emitIns_R_R(INS_mov, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

#endif // _TARGET_*

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCreateAndStoreGCInfo: Create and record GC Info for the function.
//
void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize,
                                      unsigned prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) AllowZeroAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder != nullptr);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // We keep the call count for the second call to gcMakeRegPtrTable() below.
    unsigned callCnt = 0;

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS, &callCnt);

    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();

    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK, &callCnt);

#ifdef _TARGET_ARM64_

    if (compiler->opts.compDbgEnC)
    {
        // what we have to preserve is called the "frame header" (see comments in VM\eetwain.cpp)
        // which is:
        //  -return address
        //  -saved off RBP
        //  -saved 'this' pointer and bool for synchronized methods

        // 4 slots for RBP + return address + RSI + RDI
        int preservedAreaSize = 4 * REGSIZE_BYTES;

        if (compiler->info.compFlags & CORINFO_FLG_SYNCH)
        {
            if (!(compiler->info.compFlags & CORINFO_FLG_STATIC))
                preservedAreaSize += REGSIZE_BYTES;

            preservedAreaSize += 1; // bool for synchronized methods
        }

        // Used to signal both that the method is compiled for EnC, and also the size of the block at the top of the
        // frame
        gcInfoEncoder->SetSizeOfEditAndContinuePreservedArea(preservedAreaSize);
    }

#endif // _TARGET_ARM64_

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}

//-------------------------------------------------------------------------------------------
// genJumpKindsForTree:  Determine the number and kinds of conditional branches
//                       necessary to implement the given GT_CMP node
//
// Arguments:
//   cmpTree           - (input) The GenTree node that is used to set the Condition codes
//                     - The GenTree Relop node that was used to set the Condition codes
//   jmpKind[2]        - (output) One or two conditional branch instructions
//   jmpToTrueLabel[2] - (output) On Arm64 both branches will always branch to the true label
//
// Return Value:
//    Sets the proper values into the array elements of jmpKind[] and jmpToTrueLabel[]
//
// Assumptions:
//    At least one conditional branch instruction will be returned.
//    Typically only one conditional branch is needed
//     and the second jmpKind[] value is set to EJ_NONE
//
void CodeGen::genJumpKindsForTree(GenTreePtr cmpTree, emitJumpKind jmpKind[2], bool jmpToTrueLabel[2])
{
    // On ARM both branches will always branch to the true label
    jmpToTrueLabel[0] = true;
    jmpToTrueLabel[1] = true;

    // For integer comparisons just use genJumpKindForOper
    if (!varTypeIsFloating(cmpTree->gtOp.gtOp1->gtEffectiveVal()))
    {
        CompareKind compareKind = ((cmpTree->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;
        jmpKind[0]              = genJumpKindForOper(cmpTree->gtOper, compareKind);
        jmpKind[1]              = EJ_NONE;
    }
    else // We have a Floating Point Compare operation
    {
        assert(cmpTree->OperIsCompare());

        // For details on this mapping, see the ARM Condition Code table
        // at section A8.3   in the ARMv7 architecture manual or
        // at section C1.2.3 in the ARMV8 architecture manual.

        // We must check the GTF_RELOP_NAN_UN to find out
        // if we need to branch when we have a NaN operand.
        //
        if ((cmpTree->gtFlags & GTF_RELOP_NAN_UN) != 0)
        {
            // Must branch if we have an NaN, unordered
            switch (cmpTree->gtOper)
            {
                case GT_EQ:
                    jmpKind[0] = EJ_eq; // branch or set when equal (and no NaN's)
                    jmpKind[1] = EJ_vs; // branch or set when we have a NaN
                    break;

                case GT_NE:
                    jmpKind[0] = EJ_ne; // branch or set when not equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LT:
                    jmpKind[0] = EJ_lt; // branch or set when less than (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LE:
                    jmpKind[0] = EJ_le; // branch or set when less than or equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GT:
                    jmpKind[0] = EJ_hi; // branch or set when greater than (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GE:
                    jmpKind[0] = EJ_hs; // branch or set when greater than or equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                default:
                    unreached();
            }
        }
        else // ((cmpTree->gtFlags & GTF_RELOP_NAN_UN) == 0)
        {
            // Do not branch if we have an NaN, unordered
            switch (cmpTree->gtOper)
            {
                case GT_EQ:
                    jmpKind[0] = EJ_eq; // branch or set when equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_NE:
                    jmpKind[0] = EJ_gt; // branch or set when greater than (and no NaN's)
                    jmpKind[1] = EJ_lo; // branch or set when less than (and no NaN's)
                    break;

                case GT_LT:
                    jmpKind[0] = EJ_lo; // branch or set when less than (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LE:
                    jmpKind[0] = EJ_ls; // branch or set when less than or equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GT:
                    jmpKind[0] = EJ_gt; // branch or set when greater than (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GE:
                    jmpKind[0] = EJ_ge; // branch or set when greater than or equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                default:
                    unreached();
            }
        }
    }
}

//------------------------------------------------------------------------
// genCodeForJumpTrue: Generates code for jmpTrue statement.
//
// Arguments:
//    tree - The GT_JTRUE tree node.
//
// Return Value:
//    None
//
void CodeGen::genCodeForJumpTrue(GenTreePtr tree)
{
    GenTree* cmp = tree->gtOp.gtOp1->gtEffectiveVal();
    assert(cmp->OperIsCompare());
    assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

    // Get the "kind" and type of the comparison.  Note that whether it is an unsigned cmp
    // is governed by a flag NOT by the inherent type of the node
    emitJumpKind jumpKind[2];
    bool         branchToTrueLabel[2];
    genJumpKindsForTree(cmp, jumpKind, branchToTrueLabel);
    assert(jumpKind[0] != EJ_NONE);

    // On ARM the branches will always branch to the true label
    assert(branchToTrueLabel[0]);
    inst_JMP(jumpKind[0], compiler->compCurBB->bbJumpDest);

    if (jumpKind[1] != EJ_NONE)
    {
        // the second conditional branch always has to be to the true label
        assert(branchToTrueLabel[1]);
        inst_JMP(jumpKind[1], compiler->compCurBB->bbJumpDest);
    }
}

#endif // _TARGET_ARMARCH_

#endif // !LEGACY_BACKEND
