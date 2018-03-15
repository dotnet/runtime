// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                     Register Requirements for ARM                         XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the ARM  architecture.                                                   XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

void LinearScan::BuildLclHeap(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(info->dstCount == 1);

    // Need a variable number of temp regs (see genLclHeap() in codegenarm.cpp):
    // Here '-' means don't care.
    //
    //  Size?                   Init Memory?    # temp regs
    //   0                          -               0
    //   const and <=4 str instr    -             hasPspSym ? 1 : 0
    //   const and <PageSize        No            hasPspSym ? 1 : 0
    //   >4 ptr words               Yes           hasPspSym ? 2 : 1
    //   Non-const                  Yes           hasPspSym ? 2 : 1
    //   Non-const                  No            hasPspSym ? 2 : 1

    bool hasPspSym;
#if FEATURE_EH_FUNCLETS
    hasPspSym = (compiler->lvaPSPSym != BAD_VAR_NUM);
#else
    hasPspSym = false;
#endif

    GenTree* size = tree->gtOp.gtOp1;
    if (size->IsCnsIntOrI())
    {
        assert(size->isContained());
        info->srcCount = 0;

        size_t sizeVal = size->gtIntCon.gtIconVal;
        if (sizeVal == 0)
        {
            info->internalIntCount = 0;
        }
        else
        {
            sizeVal                          = AlignUp(sizeVal, STACK_ALIGN);
            size_t cntStackAlignedWidthItems = (sizeVal >> STACK_ALIGN_SHIFT);

            // For small allocations up to 4 store instructions
            if (cntStackAlignedWidthItems <= 4)
            {
                info->internalIntCount = 0;
            }
            else if (!compiler->info.compInitMem)
            {
                // No need to initialize allocated stack space.
                if (sizeVal < compiler->eeGetPageSize())
                {
                    info->internalIntCount = 0;
                }
                else
                {
                    info->internalIntCount = 1;
                }
            }
            else
            {
                info->internalIntCount = 1;
            }

            if (hasPspSym)
            {
                info->internalIntCount++;
            }
        }
    }
    else
    {
        // target (regCnt) + tmp + [psp]
        info->srcCount         = 1;
        info->internalIntCount = hasPspSym ? 2 : 1;
        appendLocationInfoToList(size);
    }

    // If we are needed in temporary registers we should be sure that
    // it's different from target (regCnt)
    if (info->internalIntCount > 0)
    {
        info->isInternalRegDelayFree = true;
    }
}

//------------------------------------------------------------------------
// BuildNode: Set the register requirements for RA.
//
// Notes:
//    Takes care of annotating the register requirements
//    for every TreeNodeInfo struct that maps to each tree node.
//
// Preconditions:
//    LSRA has been initialized and there is a TreeNodeInfo node
//    already allocated and initialized for every tree in the IR.
//
// Postconditions:
//    Every TreeNodeInfo instance has the right annotations on register
//    requirements needed by LSRA to build the Interval Table (source,
//    destination and internal [temp] register counts).
//
void LinearScan::BuildNode(GenTree* tree)
{
    TreeNodeInfo* info         = currentNodeInfo;
    unsigned      kind         = tree->OperKind();
    RegisterType  registerType = TypeGet(tree);

    if (tree->isContained())
    {
        info->dstCount = 0;
        assert(info->srcCount == 0);
        return;
    }

    // Set the default dstCount. This may be modified below.
    if (tree->IsValue())
    {
        info->dstCount = 1;
        if (tree->IsUnusedValue())
        {
            info->isLocalDefUse = true;
        }
    }
    else
    {
        info->dstCount = 0;
    }

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child
            info->srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtOp.gtOp1 == nullptr)
            {
                assert(info->dstCount == 1);
            }
            else
            {
                assert(info->dstCount == 0);
            }
            break;

        case GT_INTRINSIC:
        {
            // TODO-ARM: Implement other type of intrinsics (round, sqrt and etc.)
            // Both operand and its result must be of the same floating point type.
            op1 = tree->gtOp.gtOp1;
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());
            appendLocationInfoToList(op1);

            switch (tree->gtIntrinsic.gtIntrinsicId)
            {
                case CORINFO_INTRINSIC_Abs:
                case CORINFO_INTRINSIC_Sqrt:
                    info->srcCount = 1;
                    assert(info->dstCount == 1);
                    break;
                default:
                    NYI_ARM("LinearScan::Build for GT_INTRINSIC");
                    break;
            }
        }
        break;

        case GT_CAST:
        {
            assert(info->dstCount == 1);

            // Non-overflow casts to/from float/double are done using SSE2 instructions
            // and that allow the source operand to be either a reg or memop. Given the
            // fact that casts from small int to float/double are done as two-level casts,
            // the source operand is always guaranteed to be of size 4 or 8 bytes.
            var_types castToType = tree->CastToType();
            GenTree*  castOp     = tree->gtCast.CastOp();
            var_types castOpType = castOp->TypeGet();
            info->srcCount       = GetOperandInfo(castOp);
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                castOpType = genUnsignedType(castOpType);
            }

            if (varTypeIsLong(castOpType))
            {
                assert((castOp->OperGet() == GT_LONG) && castOp->isContained());
                info->srcCount = 2;
            }

            // FloatToIntCast needs a temporary register
            if (varTypeIsFloating(castOpType) && varTypeIsIntOrI(tree))
            {
                info->setInternalCandidates(this, RBM_ALLFLOAT);
                info->internalFloatCount     = 1;
                info->isInternalRegDelayFree = true;
            }

            Lowering::CastInfo castInfo;

            // Get information about the cast.
            Lowering::getCastDescription(tree, &castInfo);

            if (castInfo.requiresOverflowCheck)
            {
                var_types srcType = castOp->TypeGet();
                emitAttr  cmpSize = EA_ATTR(genTypeSize(srcType));

                // If we cannot store data in an immediate for instructions,
                // then we will need to reserve a temporary register.

                if (!castInfo.signCheckOnly) // In case of only sign check, temp regs are not needeed.
                {
                    if (castInfo.unsignedSource || castInfo.unsignedDest)
                    {
                        // check typeMask
                        bool canStoreTypeMask = emitter::emitIns_valid_imm_for_alu(castInfo.typeMask);
                        if (!canStoreTypeMask)
                        {
                            info->internalIntCount = 1;
                        }
                    }
                    else
                    {
                        // For comparing against the max or min value
                        bool canStoreMaxValue =
                            emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, INS_FLAGS_DONT_CARE);
                        bool canStoreMinValue =
                            emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, INS_FLAGS_DONT_CARE);

                        if (!canStoreMaxValue || !canStoreMinValue)
                        {
                            info->internalIntCount = 1;
                        }
                    }
                }
            }
        }
        break;

        case GT_JTRUE:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_JMP:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            info->srcCount = 0;
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            break;

        case GT_SWITCH_TABLE:
            assert(info->dstCount == 0);
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == 2);
            break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            break;

        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
        case GT_ADD:
        case GT_SUB:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtOp.gtOp1->TypeGet() == tree->gtOp.gtOp2->TypeGet());

                assert(info->dstCount == 1);
                info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
                assert(info->srcCount == 2);
                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            assert(info->dstCount == 1);
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == (tree->gtOp.gtOp2->isContained() ? 1 : 2));
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            info->srcCount = 1;
            assert(info->dstCount == 0);
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_MUL:
            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                info->internalIntCount       = 1;
                info->isInternalRegDelayFree = true;
            }
            __fallthrough;

        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
        {
            assert(info->dstCount == 1);
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == 2);
        }
        break;

        case GT_MUL_LONG:
            info->dstCount = 2;
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == 2);
            break;

        case GT_FIELD_LIST:
            // These should always be contained. We don't correctly allocate or
            // generate code for a non-contained GT_FIELD_LIST.
            noway_assert(!"Non-contained GT_FIELD_LIST");
            break;

        case GT_LIST:
        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_LONG:
            assert(tree->IsUnusedValue()); // Contained nodes are already processed, only unused GT_LONG can reach here.
                                           // An unused GT_LONG doesn't produce any registers.
            tree->gtType = TYP_VOID;
            tree->ClearUnusedValue();
            info->isLocalDefUse = false;

            // An unused GT_LONG node needs to consume its sources, but need not produce a register.
            info->srcCount = 2;
            info->dstCount = 0;
            appendLocationInfoToList(tree->gtGetOp1());
            appendLocationInfoToList(tree->gtGetOp2());
            break;

        case GT_CNS_DBL:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            if (tree->TypeGet() == TYP_FLOAT)
            {
                // An int register for float constant
                info->internalIntCount = 1;
            }
            else
            {
                // TYP_DOUBLE
                assert(tree->TypeGet() == TYP_DOUBLE);

                // Two int registers for double constant
                info->internalIntCount = 2;
            }
            break;

        case GT_RETURN:
            BuildReturn(tree);
            break;

        case GT_RETFILT:
            assert(info->dstCount == 0);
            if (tree->TypeGet() == TYP_VOID)
            {
                info->srcCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);

                info->srcCount = 1;
                info->setSrcCandidates(this, RBM_INTRET);
                LocationInfoListNode* locationInfo = getLocationInfo(tree->gtOp.gtOp1);
                locationInfo->info.setSrcCandidates(this, RBM_INTRET);
                useList.Append(locationInfo);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            // Consumes arrLen & index - has no result
            info->srcCount = 2;
            assert(info->dstCount == 0);
            appendLocationInfoToList(tree->AsBoundsChk()->gtIndex);
            appendLocationInfoToList(tree->AsBoundsChk()->gtArrLen);
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_ARR_INDEX:
        {
            info->srcCount = 2;
            assert(info->dstCount == 1);
            info->internalIntCount       = 1;
            info->isInternalRegDelayFree = true;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            LocationInfoListNode* arrObjInfo = getLocationInfo(tree->AsArrIndex()->ArrObj());
            arrObjInfo->info.isDelayFree     = true;
            useList.Append(arrObjInfo);
            useList.Append(getLocationInfo(tree->AsArrIndex()->IndexExpr()));
            info->hasDelayFreeSrc = true;
        }
        break;

        case GT_ARR_OFFSET:

            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            assert(info->dstCount == 1);

            if (tree->gtArrOffs.gtOffset->isContained())
            {
                info->srcCount = 2;
            }
            else
            {
                // Here we simply need an internal register, which must be different
                // from any of the operand's registers, but may be the same as targetReg.
                info->internalIntCount = 1;
                info->srcCount         = 3;
                appendLocationInfoToList(tree->AsArrOffs()->gtOffset);
            }
            appendLocationInfoToList(tree->AsArrOffs()->gtIndex);
            appendLocationInfoToList(tree->AsArrOffs()->gtArrObj);
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea    = tree->AsAddrMode();
            int              offset = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount and dstCount here.
            info->srcCount = 0;
            assert(info->dstCount == 1);
            if (lea->HasBase())
            {
                info->srcCount++;
                appendLocationInfoToList(tree->AsAddrMode()->Base());
            }
            if (lea->HasIndex())
            {
                info->srcCount++;
                appendLocationInfoToList(tree->AsAddrMode()->Index());
            }

            // An internal register may be needed too; the logic here should be in sync with the
            // genLeaInstruction()'s requirements for a such register.
            if (lea->HasBase() && lea->HasIndex())
            {
                if (offset != 0)
                {
                    // We need a register when we have all three: base reg, index reg and a non-zero offset.
                    info->internalIntCount = 1;
                }
            }
            else if (lea->HasBase())
            {
                if (!emitter::emitIns_valid_imm_for_add(offset, INS_FLAGS_DONT_CARE))
                {
                    // We need a register when we have an offset that is too large to encode in the add instruction.
                    info->internalIntCount = 1;
                }
            }
        }
        break;

        case GT_NEG:
            info->srcCount = 1;
            assert(info->dstCount == 1);
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_NOT:
            info->srcCount = 1;
            assert(info->dstCount == 1);
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
        case GT_LSH_HI:
        case GT_RSH_LO:
            BuildShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_CMP:
            BuildCmp(tree);
            break;

        case GT_CKFINITE:
            info->srcCount = 1;
            assert(info->dstCount == 1);
            info->internalIntCount = 1;
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_CALL:
            BuildCall(tree->AsCall());
            break;

        case GT_ADDR:
        {
            // For a GT_ADDR, the child node should not be evaluated into a register
            GenTree* child = tree->gtOp.gtOp1;
            assert(!isCandidateLocalRef(child));
            assert(child->isContained());
            assert(info->dstCount == 1);
            info->srcCount = 0;
        }
        break;

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
            BuildBlockStore(tree->AsBlk());
            break;

        case GT_INIT_VAL:
            // Always a passthrough of its child's value.
            assert(!"INIT_VAL should always be contained");
            break;

        case GT_LCLHEAP:
            BuildLclHeap(tree);
            break;

        case GT_STOREIND:
        {
            assert(info->dstCount == 0);
            GenTree* src = tree->gtOp.gtOp2;

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                info->srcCount = 2;
                BuildGCWriteBarrier(tree);
                break;
            }

            BuildIndir(tree->AsIndir());
            // No contained source on ARM.
            assert(!src->isContained());
            info->srcCount++;
            appendLocationInfoToList(src);
        }
        break;

        case GT_NULLCHECK:
            // It requires a internal register on ARM, as it is implemented as a load
            assert(info->dstCount == 0);
            assert(!tree->gtGetOp1()->isContained());
            info->srcCount         = 1;
            info->internalIntCount = 1;
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_IND:
            assert(info->dstCount == 1);
            info->srcCount = 1;
            BuildIndir(tree->AsIndir());
            break;

        case GT_CATCH_ARG:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            info->setDstCandidates(this, RBM_EXCEPTION_OBJECT);
            break;

        case GT_CLS_VAR:
            info->srcCount = 0;
            // GT_CLS_VAR, by the time we reach the backend, must always
            // be a pure use.
            // It will produce a result of the type of the
            // node, and use an internal register for the address.

            assert(info->dstCount == 1);
            assert((tree->gtFlags & (GTF_VAR_DEF | GTF_VAR_USEASG)) == 0);
            info->internalIntCount = 1;
            break;

        case GT_COPY:
            info->srcCount = 1;
#ifdef _TARGET_ARM_
            // This case currently only occurs for double types that are passed as TYP_LONG;
            // actual long types would have been decomposed by now.
            if (tree->TypeGet() == TYP_LONG)
            {
                info->dstCount = 2;
            }
            else
#endif
            {
                assert(info->dstCount == 1);
            }
            appendLocationInfoToList(tree->gtOp.gtOp1);
            break;

        case GT_PUTARG_SPLIT:
            BuildPutArgSplit(tree->AsPutArgSplit());
            break;

        case GT_PUTARG_STK:
            BuildPutArgStk(tree->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            BuildPutArgReg(tree->AsUnOp());
            break;

        case GT_BITCAST:
        {
            info->srcCount = 1;
            assert(info->dstCount == 1);
            LocationInfoListNode* locationInfo = getLocationInfo(tree->gtOp.gtOp1);
            useList.Append(locationInfo);
            regNumber argReg  = tree->gtRegNum;
            regMaskTP argMask = genRegMask(argReg);

            // If type of node is `long` then it is actually `double`.
            // The actual `long` types must have been transformed as a field list with two fields.
            if (tree->TypeGet() == TYP_LONG)
            {
                info->dstCount++;
                assert(genRegArgNext(argReg) == REG_NEXT(argReg));
                argMask |= genRegMask(REG_NEXT(argReg));
            }

            info->setDstCandidates(this, argMask);
            info->setSrcCandidates(this, argMask);
        }
        break;

        default:
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::OpName(tree->OperGet()));
            NYIRAW(message);
#else
            NYI_ARM("BuildNode default case");
#endif
        case GT_LCL_FLD:
        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_PHYSREG:
        case GT_CLS_VAR_ADDR:
        case GT_IL_OFFSET:
        case GT_CNS_INT:
        case GT_LABEL:
        case GT_PINVOKE_PROLOG:
        case GT_JCC:
        case GT_SETCC:
        case GT_MEMORYBARRIER:
        case GT_OBJ:
            BuildSimple(tree);
            break;

        case GT_INDEX_ADDR:
            info->dstCount         = 1;
            info->internalIntCount = 1;
            info->srcCount         = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == 2);
            break;
    } // end switch (tree->OperGet())

    if (tree->IsUnusedValue() && (info->dstCount != 0))
    {
        info->isLocalDefUse = true;
    }
    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || tree->IsMultiRegNode());
    assert(info->isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (info->dstCount != 0));
    assert(info->dstCount == tree->GetRegisterDstCount());
}

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
