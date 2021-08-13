// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for AMD64                        XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the AMD64 architecture.                                                  XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_XARCH

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// BuildNode: Build the RefPositions for for a node
//
// Arguments:
//    treeNode - the node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
// Preconditions:
//    LSRA Has been initialized.
//
// Postconditions:
//    RefPositions have been built for all the register defs and uses required
//    for this node.
//
int LinearScan::BuildNode(GenTree* tree)
{
    assert(!tree->isContained());
    int       srcCount;
    int       dstCount      = 0;
    regMaskTP killMask      = RBM_NONE;
    bool      isLocalDefUse = false;

    // Reset the build-related members of LinearScan.
    clearBuildState();

    // Set the default dstCount. This may be modified below.
    if (tree->IsValue())
    {
        dstCount = 1;
        if (tree->IsUnusedValue())
        {
            isLocalDefUse = true;
        }
    }
    else
    {
        dstCount = 0;
    }

    // floating type generates AVX instruction (vmovss etc.), set the flag
    if (varTypeUsesFloatReg(tree->TypeGet()))
    {
        SetContainsAVXFlags();
    }

    switch (tree->OperGet())
    {
        default:
            srcCount = BuildSimple(tree);
            break;

        case GT_LCL_VAR:
            // We make a final determination about whether a GT_LCL_VAR is a candidate or contained
            // after liveness. In either case we don't build any uses or defs. Otherwise, this is a
            // load of a stack-based local into a register and we'll fall through to the general
            // local case below.
            if (checkContainedOrCandidateLclVar(tree->AsLclVar()))
            {
                return 0;
            }
            FALLTHROUGH;

        case GT_LCL_FLD:
        {
            srcCount = 0;
#ifdef FEATURE_SIMD
            // Need an additional register to read upper 4 bytes of Vector3.
            if (tree->TypeGet() == TYP_SIMD12)
            {
                // We need an internal register different from targetReg in which 'tree' produces its result
                // because both targetReg and internal reg will be in use at the same time.
                buildInternalFloatRegisterDefForNode(tree, allSIMDRegs());
                setInternalRegsDelayFree = true;
                buildInternalRegisterUses();
            }
#endif
            BuildDef(tree);
        }
        break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            if (tree->IsMultiRegLclVar() && isCandidateMultiRegLclVar(tree->AsLclVar()))
            {
                dstCount = compiler->lvaGetDesc(tree->AsLclVar()->GetLclNum())->lvFieldCnt;
            }
            srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_FIELD_LIST:
            // These should always be contained. We don't correctly allocate or
            // generate code for a non-contained GT_FIELD_LIST.
            noway_assert(!"Non-contained GT_FIELD_LIST");
            srcCount = 0;
            break;

        case GT_LIST:
        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_START_PREEMPTGC:
            // This kills GC refs in callee save regs
            srcCount = 0;
            assert(dstCount == 0);
            BuildDefsWithKills(tree, 0, RBM_NONE, RBM_NONE);
            break;

        case GT_PROF_HOOK:
            srcCount = 0;
            assert(dstCount == 0);
            killMask = getKillSetForProfilerHook();
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
        {
            srcCount = 0;
            assert(dstCount == 1);
            assert(!tree->IsReuseRegVal());
            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
        }
        break;

#if !defined(TARGET_64BIT)

        case GT_LONG:
            assert(tree->IsUnusedValue()); // Contained nodes are already processed, only unused GT_LONG can reach here.
            // An unused GT_LONG node needs to consume its sources, but need not produce a register.
            tree->gtType = TYP_VOID;
            tree->ClearUnusedValue();
            isLocalDefUse = false;
            srcCount      = 2;
            dstCount      = 0;
            BuildUse(tree->gtGetOp1());
            BuildUse(tree->gtGetOp2());
            break;

#endif // !defined(TARGET_64BIT)

        case GT_BOX:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
            srcCount = 0;
            unreached();
            break;

        case GT_RETURN:
            srcCount = BuildReturn(tree);
            killMask = getKillSetForReturn();
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        case GT_RETFILT:
            assert(dstCount == 0);
            if (tree->TypeGet() == TYP_VOID)
            {
                srcCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);
                srcCount = 1;
                BuildUse(tree->gtGetOp1(), RBM_INTRET);
            }
            break;

        // A GT_NOP is either a passthrough (if it is void, or if it has
        // a child), but must be considered to produce a dummy value if it
        // has a type but no child
        case GT_NOP:
            srcCount = 0;
            assert((tree->gtGetOp1() == nullptr) || tree->isContained());
            if (tree->TypeGet() != TYP_VOID && tree->gtGetOp1() == nullptr)
            {
                assert(dstCount == 1);
                BuildUse(tree->gtGetOp1());
                BuildDef(tree);
            }
            else
            {
                assert(dstCount == 0);
            }
            break;

        case GT_KEEPALIVE:
            assert(dstCount == 0);
            srcCount = BuildOperandUses(tree->gtGetOp1());
            break;

        case GT_JTRUE:
        {
            srcCount = 0;
            assert(dstCount == 0);
            GenTree* cmp = tree->gtGetOp1();
            assert(!cmp->IsValue());
        }
        break;

        case GT_JCC:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_SETCC:
            srcCount = 0;
            assert(dstCount == 1);
            // This defines a byte value (note that on x64 allByteRegs() is defined as RBM_ALLINT).
            BuildDef(tree, allByteRegs());
            break;

        case GT_JMP:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            srcCount = 0;
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            srcCount = 0;
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_SWITCH_TABLE:
        {
            assert(dstCount == 0);
            buildInternalIntRegisterDefForNode(tree);
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalRegisterUses();
            assert(srcCount == 2);
        }
        break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            srcCount = 0;
            break;

#if !defined(TARGET_64BIT)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif
        case GT_ADD:
        case GT_SUB:
        case GT_AND:
        case GT_OR:
        case GT_XOR:
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_BT:
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(dstCount == 0);
            break;

        case GT_RETURNTRAP:
        {
            // This just turns into a compare of its child with an int + a conditional call.
            RefPosition* internalDef = buildInternalIntRegisterDefForNode(tree);
            srcCount                 = BuildOperandUses(tree->gtGetOp1());
            buildInternalRegisterUses();
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
        }
        break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            srcCount = BuildModDiv(tree->AsOp());
            break;

#if defined(TARGET_X86)
        case GT_MUL_LONG:
            dstCount = 2;
            FALLTHROUGH;
#endif
        case GT_MUL:
        case GT_MULHI:
            srcCount = BuildMul(tree->AsOp());
            break;

        case GT_INTRINSIC:
            srcCount = BuildIntrinsic(tree->AsOp());
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            srcCount = BuildSIMD(tree->AsSIMD());
            break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            srcCount = BuildHWIntrinsic(tree->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
            assert(dstCount == 1);
            srcCount = BuildCast(tree->AsCast());
            break;

        case GT_BITCAST:
            assert(dstCount == 1);
            if (!tree->gtGetOp1()->isContained())
            {
                BuildUse(tree->gtGetOp1());
                srcCount = 1;
            }
            else
            {
                srcCount = 0;
            }
            BuildDef(tree);
            break;

        case GT_NEG:
            // TODO-XArch-CQ:
            // SSE instruction set doesn't have an instruction to negate a number.
            // The recommended way is to xor the float/double number with a bitmask.
            // The only way to xor is using xorps or xorpd both of which operate on
            // 128-bit operands.  To hold the bit-mask we would need another xmm
            // register or a 16-byte aligned 128-bit data constant. Right now emitter
            // lacks the support for emitting such constants or instruction with mem
            // addressing mode referring to a 128-bit operand. For now we use an
            // internal xmm register to load 32/64-bit bitmask from data section.
            // Note that by trading additional data section memory (128-bit) we can
            // save on the need for an internal register and also a memory-to-reg
            // move.
            //
            // Note: another option to avoid internal register requirement is by
            // lowering as GT_SUB(0, src).  This will generate code different from
            // Jit64 and could possibly result in compat issues (?).
            if (varTypeIsFloating(tree))
            {

                RefPosition* internalDef = buildInternalFloatRegisterDefForNode(tree, internalFloatRegCandidates());
                srcCount                 = BuildOperandUses(tree->gtGetOp1());
                buildInternalRegisterUses();
            }
            else
            {
                srcCount = BuildOperandUses(tree->gtGetOp1());
            }
            BuildDef(tree);
            break;

        case GT_NOT:
            srcCount = BuildOperandUses(tree->gtGetOp1());
            BuildDef(tree);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
#ifdef TARGET_X86
        case GT_LSH_HI:
        case GT_RSH_LO:
#endif
            srcCount = BuildShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
            srcCount = BuildCmp(tree);
            break;

        case GT_CKFINITE:
        {
            assert(dstCount == 1);
            RefPosition* internalDef = buildInternalIntRegisterDefForNode(tree);
            srcCount                 = BuildOperandUses(tree->gtGetOp1());
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_CMPXCHG:
        {
            srcCount = 3;
            assert(dstCount == 1);

            // Comparand is preferenced to RAX.
            // The remaining two operands can be in any reg other than RAX.
            BuildUse(tree->AsCmpXchg()->gtOpLocation, allRegs(TYP_INT) & ~RBM_RAX);
            BuildUse(tree->AsCmpXchg()->gtOpValue, allRegs(TYP_INT) & ~RBM_RAX);
            BuildUse(tree->AsCmpXchg()->gtOpComparand, RBM_RAX);
            BuildDef(tree, RBM_RAX);
        }
        break;

        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        {
            // TODO-XArch-Cleanup: We should make the indirection explicit on these nodes so that we don't have
            // to special case them.
            // These tree nodes will have their op1 marked as isDelayFree=true.
            // That is, op1's reg remains in use until the subsequent instruction.
            GenTree* addr = tree->gtGetOp1();
            GenTree* data = tree->gtGetOp2();
            assert(!addr->isContained());
            RefPosition* addrUse = BuildUse(addr);
            setDelayFree(addrUse);
            tgtPrefUse = addrUse;
            assert(!data->isContained());
            BuildUse(data);
            srcCount = 2;
            assert(dstCount == 1);
            BuildDef(tree);
        }
        break;

        case GT_PUTARG_REG:
            srcCount = BuildPutArgReg(tree->AsUnOp());
            break;

        case GT_CALL:
            srcCount = BuildCall(tree->AsCall());
            if (tree->AsCall()->HasMultiRegRetVal())
            {
                dstCount = tree->AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
            }
            break;

        case GT_ADDR:
        {
            // For a GT_ADDR, the child node should not be evaluated into a register
            GenTree* child = tree->gtGetOp1();
            assert(!isCandidateLocalRef(child));
            assert(child->isContained());
            assert(dstCount == 1);
            srcCount = 0;
        }
        break;

#if !defined(FEATURE_PUT_STRUCT_ARG_STK)
        case GT_OBJ:
#endif
        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            srcCount = 0;
            break;

#ifdef FEATURE_PUT_STRUCT_ARG_STK
        case GT_PUTARG_STK:
            srcCount = BuildPutArgStk(tree->AsPutArgStk());
            break;
#endif // FEATURE_PUT_STRUCT_ARG_STK

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
            srcCount = BuildBlockStore(tree->AsBlk());
            break;

        case GT_INIT_VAL:
            // Always a passthrough of its child's value.
            assert(!"INIT_VAL should always be contained");
            srcCount = 0;
            break;

        case GT_LCLHEAP:
            srcCount = BuildLclHeap(tree);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS

            // Consumes arrLen & index - has no result
            assert(dstCount == 0);
            srcCount = BuildOperandUses(tree->AsBoundsChk()->gtIndex);
            srcCount += BuildOperandUses(tree->AsBoundsChk()->gtArrLen);
            break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM after Lowering.");
            srcCount = 0;
            break;

        case GT_ARR_INDEX:
        {
            srcCount = 2;
            assert(dstCount == 1);
            assert(!tree->AsArrIndex()->ArrObj()->isContained());
            assert(!tree->AsArrIndex()->IndexExpr()->isContained());
            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            RefPosition* arrObjUse = BuildUse(tree->AsArrIndex()->ArrObj());
            setDelayFree(arrObjUse);
            BuildUse(tree->AsArrIndex()->IndexExpr());
            BuildDef(tree);
        }
        break;

        case GT_ARR_OFFSET:
        {
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            assert(dstCount == 1);
            srcCount                 = 0;
            RefPosition* internalDef = nullptr;
            if (tree->AsArrOffs()->gtOffset->isContained())
            {
                srcCount = 2;
            }
            else
            {
                // Here we simply need an internal register, which must be different
                // from any of the operand's registers, but may be the same as targetReg.
                srcCount    = 3;
                internalDef = buildInternalIntRegisterDefForNode(tree);
                BuildUse(tree->AsArrOffs()->gtOffset);
            }
            BuildUse(tree->AsArrOffs()->gtIndex);
            BuildUse(tree->AsArrOffs()->gtArrObj);
            if (internalDef != nullptr)
            {
                buildInternalRegisterUses();
            }
            BuildDef(tree);
        }
        break;

        case GT_LEA:
            // The LEA usually passes its operands through to the GT_IND, in which case it will
            // be contained, but we may be instantiating an address, in which case we set them here.
            srcCount = 0;
            assert(dstCount == 1);
            if (tree->AsAddrMode()->HasBase())
            {
                srcCount++;
                BuildUse(tree->AsAddrMode()->Base());
            }
            if (tree->AsAddrMode()->HasIndex())
            {
                srcCount++;
                BuildUse(tree->AsAddrMode()->Index());
            }
            BuildDef(tree);
            break;

        case GT_STOREIND:
            if (compiler->codeGen->gcInfo.gcIsWriteBarrierStoreIndNode(tree))
            {
                srcCount = BuildGCWriteBarrier(tree);
                break;
            }
            srcCount = BuildIndir(tree->AsIndir());
            break;

        case GT_NULLCHECK:
        {
            assert(dstCount == 0);
            // If we have a contained address on a nullcheck, we transform it to
            // an unused GT_IND, since we require a target register.
            BuildUse(tree->gtGetOp1());
            srcCount = 1;
            break;
        }

        case GT_IND:
            srcCount = BuildIndir(tree->AsIndir());
            assert(dstCount == 1);
            break;

        case GT_CATCH_ARG:
            srcCount = 0;
            assert(dstCount == 1);
            BuildDef(tree, RBM_EXCEPTION_OBJECT);
            break;

#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:
            srcCount = 0;
            assert(dstCount == 0);
            break;
#endif

        case GT_CLS_VAR:
            // These nodes are eliminated by rationalizer.
            JITDUMP("Unexpected node %s in Lower.\n", GenTree::OpName(tree->OperGet()));
            unreached();
            break;

        case GT_INDEX_ADDR:
        {
            assert(dstCount == 1);
            RefPosition* internalDef = nullptr;
#ifdef TARGET_64BIT
            // On 64-bit we always need a temporary register:
            //   - if the index is `native int` then we need to load the array
            //     length into a register to widen it to `native int`
            //   - if the index is `int` (or smaller) then we need to widen
            //     it to `long` to peform the address calculation
            internalDef = buildInternalIntRegisterDefForNode(tree);
#else  // !TARGET_64BIT
            assert(!varTypeIsLong(tree->AsIndexAddr()->Index()->TypeGet()));
            switch (tree->AsIndexAddr()->gtElemSize)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                    break;

                default:
                    internalDef = buildInternalIntRegisterDefForNode(tree);
                    break;
            }
#endif // !TARGET_64BIT
            srcCount = BuildBinaryUses(tree->AsOp());
            if (internalDef != nullptr)
            {
                buildInternalRegisterUses();
            }
            BuildDef(tree);
        }
        break;

    } // end switch (tree->OperGet())

    // We need to be sure that we've set srcCount and dstCount appropriately.
    // Not that for XARCH, the maximum number of registers defined is 2.
    assert((dstCount < 2) || ((dstCount == 2) && tree->IsMultiRegNode()));
    assert(isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (dstCount != 0));
    assert(dstCount == tree->GetRegisterDstCount(compiler));
    return srcCount;
}

//------------------------------------------------------------------------
// getTgtPrefOperands: Identify whether the operands of an Op should be preferenced to the target.
//
// Arguments:
//    tree    - the node of interest.
//    prefOp1 - a bool "out" parameter indicating, on return, whether op1 should be preferenced to the target.
//    prefOp2 - a bool "out" parameter indicating, on return, whether op2 should be preferenced to the target.
//
// Return Value:
//    This has two "out" parameters for returning the results (see above).
//
// Notes:
//    The caller is responsible for initializing the two "out" parameters to false.
//
void LinearScan::getTgtPrefOperands(GenTreeOp* tree, bool& prefOp1, bool& prefOp2)
{
    // If op2 of a binary-op gets marked as contained, then binary-op srcCount will be 1.
    // Even then we would like to set isTgtPref on Op1.
    if (tree->OperIsBinary() && isRMWRegOper(tree))
    {
        GenTree* op1 = tree->gtGetOp1();
        GenTree* op2 = tree->gtGetOp2();

        // If we have a read-modify-write operation, we want to preference op1 to the target,
        // if it is not contained.
        if (!op1->isContained() && !op1->OperIs(GT_LIST))
        {
            prefOp1 = true;
        }

        // Commutative opers like add/mul/and/or/xor could reverse the order of operands if it is safe to do so.
        // In that case we will preference both, to increase the chance of getting a match.
        if (tree->OperIsCommutative() && op2 != nullptr && !op2->isContained())
        {
            prefOp2 = true;
        }
    }
}

//------------------------------------------------------------------------------
// isRMWRegOper: Can this binary tree node be used in a Read-Modify-Write format
//
// Arguments:
//    tree      - a binary tree node
//
// Return Value:
//    Returns true if we can use the read-modify-write instruction form
//
// Notes:
//    This is used to determine whether to preference the source to the destination register.
//
bool LinearScan::isRMWRegOper(GenTree* tree)
{
    // TODO-XArch-CQ: Make this more accurate.
    // For now, We assume that most binary operators are of the RMW form.
    assert(tree->OperIsBinary());

    if (tree->OperIsCompare() || tree->OperIs(GT_CMP) || tree->OperIs(GT_BT))
    {
        return false;
    }

    switch (tree->OperGet())
    {
        // These Opers either support a three op form (i.e. GT_LEA), or do not read/write their first operand
        case GT_LEA:
        case GT_STOREIND:
        case GT_ARR_INDEX:
        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_SWITCH_TABLE:
        case GT_LOCKADD:
#ifdef TARGET_X86
        case GT_LONG:
#endif
            return false;

        case GT_ADD:
        case GT_SUB:
        case GT_DIV:
        {
            return !varTypeIsFloating(tree->TypeGet()) || !compiler->canUseVexEncoding();
        }

        // x86/x64 does support a three op multiply when op2|op1 is a contained immediate
        case GT_MUL:
        {
            if (varTypeIsFloating(tree->TypeGet()))
            {
                return !compiler->canUseVexEncoding();
            }
            return (!tree->gtGetOp2()->isContainedIntOrIImmed() && !tree->gtGetOp1()->isContainedIntOrIImmed());
        }

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return tree->isRMWHWIntrinsic(compiler);
#endif // FEATURE_HW_INTRINSICS

        default:
            return true;
    }
}

// Support for building RefPositions for RMW nodes.
int LinearScan::BuildRMWUses(GenTreeOp* node, regMaskTP candidates)
{
    int       srcCount      = 0;
    GenTree*  op1           = node->gtOp1;
    GenTree*  op2           = node->gtGetOp2IfPresent();
    regMaskTP op1Candidates = candidates;
    regMaskTP op2Candidates = candidates;

#ifdef TARGET_X86
    if (varTypeIsByte(node))
    {
        regMaskTP byteCandidates = (candidates == RBM_NONE) ? allByteRegs() : (candidates & allByteRegs());
        if (!op1->isContained())
        {
            assert(byteCandidates != RBM_NONE);
            op1Candidates = byteCandidates;
        }
        if (node->OperIsCommutative() && !op2->isContained())
        {
            assert(byteCandidates != RBM_NONE);
            op2Candidates = byteCandidates;
        }
    }
#endif // TARGET_X86

    bool prefOp1 = false;
    bool prefOp2 = false;
    getTgtPrefOperands(node, prefOp1, prefOp2);
    assert(!prefOp2 || node->OperIsCommutative());

    // Determine which operand, if any, should be delayRegFree. Normally, this would be op2,
    // but if we have a commutative operator and op1 is a contained memory op, it would be op1.
    // We need to make the delayRegFree operand remain live until the op is complete, by marking
    // the source(s) associated with op2 as "delayFree".
    // Note that if op2 of a binary RMW operator is a memory op, even if the operator
    // is commutative, codegen cannot reverse them.
    // TODO-XArch-CQ: This is not actually the case for all RMW binary operators, but there's
    // more work to be done to correctly reverse the operands if they involve memory
    // operands.  Also, we may need to handle more cases than GT_IND, especially once
    // we've modified the register allocator to not require all nodes to be assigned
    // a register (e.g. a spilled lclVar can often be referenced directly from memory).
    // Note that we may have a null op2, even with 2 sources, if op1 is a base/index memory op.
    GenTree* delayUseOperand = op2;
    if (node->OperIsCommutative())
    {
        if (op1->isContained() && op2 != nullptr)
        {
            delayUseOperand = op1;
        }
        else if (!op2->isContained() || op2->IsCnsIntOrI())
        {
            // If we have a commutative operator and op2 is not a memory op, we don't need
            // to set delayRegFree on either operand because codegen can swap them.
            delayUseOperand = nullptr;
        }
    }
    else if (op1->isContained())
    {
        delayUseOperand = nullptr;
    }
    if (delayUseOperand != nullptr)
    {
        assert(!prefOp1 || delayUseOperand != op1);
        assert(!prefOp2 || delayUseOperand != op2);
    }

    // Build first use
    if (prefOp1)
    {
        assert(!op1->isContained());
        tgtPrefUse = BuildUse(op1, op1Candidates);
        srcCount++;
    }
    else if (delayUseOperand == op1)
    {
        srcCount += BuildDelayFreeUses(op1, op2, op1Candidates);
    }
    else
    {
        srcCount += BuildOperandUses(op1, op1Candidates);
    }
    // Build second use
    if (op2 != nullptr)
    {
        if (prefOp2)
        {
            assert(!op2->isContained());
            tgtPrefUse2 = BuildUse(op2, op2Candidates);
            srcCount++;
        }
        else if (delayUseOperand == op2)
        {
            srcCount += BuildDelayFreeUses(op2, op1, op2Candidates);
        }
        else
        {
            srcCount += BuildOperandUses(op2, op2Candidates);
        }
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildShiftRotate: Set the NodeInfo for a shift or rotate.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildShiftRotate(GenTree* tree)
{
    // For shift operations, we need that the number
    // of bits moved gets stored in CL in case
    // the number of bits to shift is not a constant.
    int       srcCount      = 0;
    GenTree*  shiftBy       = tree->gtGetOp2();
    GenTree*  source        = tree->gtGetOp1();
    regMaskTP srcCandidates = RBM_NONE;
    regMaskTP dstCandidates = RBM_NONE;

    // x64 can encode 8 bits of shift and it will use 5 or 6. (the others are masked off)
    // We will allow whatever can be encoded - hope you know what you are doing.
    if (shiftBy->isContained())
    {
        assert(shiftBy->OperIsConst());
    }
    else
    {
        srcCandidates = allRegs(TYP_INT) & ~RBM_RCX;
        dstCandidates = allRegs(TYP_INT) & ~RBM_RCX;
    }

    // Note that Rotate Left/Right instructions don't set ZF and SF flags.
    //
    // If the operand being shifted is 32-bits then upper three bits are masked
    // by hardware to get actual shift count.  Similarly for 64-bit operands
    // shift count is narrowed to [0..63].  If the resulting shift count is zero,
    // then shift operation won't modify flags.
    //
    // TODO-CQ-XARCH: We can optimize generating 'test' instruction for GT_EQ/NE(shift, 0)
    // if the shift count is known to be non-zero and in the range depending on the
    // operand size.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
    // The first operand of a GT_LSH_HI and GT_RSH_LO oper is a GT_LONG so that
    // we can have a three operand form.
    if (tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO)
    {
        assert((source->OperGet() == GT_LONG) && source->isContained());

        GenTree* sourceLo = source->gtGetOp1();
        GenTree* sourceHi = source->gtGetOp2();
        assert(!sourceLo->isContained() && !sourceHi->isContained());
        RefPosition* sourceLoUse = BuildUse(sourceLo, srcCandidates);
        RefPosition* sourceHiUse = BuildUse(sourceHi, srcCandidates);

        if (!tree->isContained())
        {
            if (tree->OperGet() == GT_LSH_HI)
            {
                setDelayFree(sourceLoUse);
            }
            else
            {
                setDelayFree(sourceHiUse);
            }
        }
    }
    else
#endif
        if (!source->isContained())
    {
        tgtPrefUse = BuildUse(source, srcCandidates);
        srcCount++;
    }
    else
    {
        srcCount += BuildOperandUses(source, srcCandidates);
    }
    if (!tree->isContained())
    {
        if (!shiftBy->isContained())
        {
            srcCount += BuildDelayFreeUses(shiftBy, source, RBM_RCX);
            buildKillPositionsForNode(tree, currentLoc + 1, RBM_RCX);
        }
        BuildDef(tree, dstCandidates);
    }
    else
    {
        if (!shiftBy->isContained())
        {
            srcCount += BuildOperandUses(shiftBy, RBM_RCX);
            buildKillPositionsForNode(tree, currentLoc + 1, RBM_RCX);
        }
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildCall: Set the NodeInfo for a call.
//
// Arguments:
//    call      - The call node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildCall(GenTreeCall* call)
{
    bool                  hasMultiRegRetVal = false;
    const ReturnTypeDesc* retTypeDesc       = nullptr;
    int                   srcCount          = 0;
    int                   dstCount          = 0;
    regMaskTP             dstCandidates     = RBM_NONE;

    assert(!call->isContained());
    if (call->TypeGet() != TYP_VOID)
    {
        hasMultiRegRetVal = call->HasMultiRegRetVal();
        if (hasMultiRegRetVal)
        {
            // dst count = number of registers in which the value is returned by call
            retTypeDesc = call->GetReturnTypeDesc();
            dstCount    = retTypeDesc->GetReturnRegCount();
        }
        else
        {
            dstCount = 1;
        }
    }

    GenTree* ctrlExpr = call->gtControlExpr;
    if (call->gtCallType == CT_INDIRECT)
    {
        ctrlExpr = call->gtCallAddr;
    }

    RegisterType registerType = regType(call);

    // Set destination candidates for return value of the call.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
    if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        // The x86 CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
        // TCB in REG_PINVOKE_TCB. AMD64/ARM64 use the standard calling convention. fgMorphCall() sets the
        // correct argument registers.
        dstCandidates = RBM_PINVOKE_TCB;
    }
    else
#endif // TARGET_X86
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        dstCandidates = retTypeDesc->GetABIReturnRegs();
        assert((int)genCountBits(dstCandidates) == dstCount);
    }
    else if (varTypeUsesFloatReg(registerType))
    {
#ifdef TARGET_X86
        // The return value will be on the X87 stack, and we will need to move it.
        dstCandidates = allRegs(registerType);
#else  // !TARGET_X86
        dstCandidates                     = RBM_FLOATRET;
#endif // !TARGET_X86
    }
    else if (registerType == TYP_LONG)
    {
        dstCandidates = RBM_LNGRET;
    }
    else
    {
        dstCandidates = RBM_INTRET;
    }

    // number of args to a call =
    // callRegArgs + (callargs - placeholders, setup, etc)
    // there is an explicit thisPtr but it is redundant

    bool callHasFloatRegArgs = false;

    // First, determine internal registers.
    // We will need one for any float arguments to a varArgs call.
    for (GenTreeCall::Use& use : call->LateArgs())
    {
        GenTree* argNode = use.GetNode();
        if (argNode->OperIsPutArgReg())
        {
            HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
        }
        else if (argNode->OperGet() == GT_FIELD_LIST)
        {
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                assert(use.GetNode()->OperIsPutArgReg());
                HandleFloatVarArgs(call, use.GetNode(), &callHasFloatRegArgs);
            }
        }
    }

    // Now, count reg args
    for (GenTreeCall::Use& use : call->LateArgs())
    {
        // By this point, lowering has ensured that all call arguments are one of the following:
        // - an arg setup store
        // - an arg placeholder
        // - a nop
        // - a copy blk
        // - a field list
        // - a put arg
        //
        // Note that this property is statically checked by LinearScan::CheckBlock.
        GenTree* argNode = use.GetNode();

        // Each register argument corresponds to one source.
        if (argNode->OperIsPutArgReg())
        {
            srcCount++;
            BuildUse(argNode, genRegMask(argNode->GetRegNum()));
        }
#ifdef UNIX_AMD64_ABI
        else if (argNode->OperGet() == GT_FIELD_LIST)
        {
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                assert(use.GetNode()->OperIsPutArgReg());
                srcCount++;
                BuildUse(use.GetNode(), genRegMask(use.GetNode()->GetRegNum()));
            }
        }
#endif // UNIX_AMD64_ABI

#ifdef DEBUG
        // In DEBUG only, check validity with respect to the arg table entry.

        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        if (curArgTabEntry->GetRegNum() == REG_STK)
        {
            // late arg that is not passed in a register
            assert(argNode->gtOper == GT_PUTARG_STK);

#ifdef FEATURE_PUT_STRUCT_ARG_STK
            // If the node is TYP_STRUCT and it is put on stack with
            // putarg_stk operation, we consume and produce no registers.
            // In this case the embedded Obj node should not produce
            // registers too since it is contained.
            // Note that if it is a SIMD type the argument will be in a register.
            if (argNode->TypeGet() == TYP_STRUCT)
            {
                assert(argNode->gtGetOp1() != nullptr && argNode->gtGetOp1()->OperGet() == GT_OBJ);
                assert(argNode->gtGetOp1()->isContained());
            }
#endif // FEATURE_PUT_STRUCT_ARG_STK
            continue;
        }
#ifdef UNIX_AMD64_ABI
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(argNode->isContained());
            assert(varTypeIsStruct(argNode) || curArgTabEntry->isStruct);

            unsigned regIndex = 0;
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                const regNumber argReg = curArgTabEntry->GetRegNum(regIndex);
                assert(use.GetNode()->GetRegNum() == argReg);
                regIndex++;
            }
        }
        else
#endif // UNIX_AMD64_ABI
        {
            const regNumber argReg = curArgTabEntry->GetRegNum();
            assert(argNode->GetRegNum() == argReg);
        }
#endif // DEBUG
    }

#ifdef DEBUG
    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    for (GenTreeCall::Use& use : call->Args())
    {
        GenTree* arg = use.GetNode();
        if (!(arg->gtFlags & GTF_LATE_ARG) && !arg)
        {
            if (arg->IsValue() && !arg->isContained())
            {
                assert(arg->IsUnusedValue());
            }
        }
    }
#endif // DEBUG

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        regMaskTP ctrlExprCandidates = RBM_NONE;

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            assert(!ctrlExpr->isContained());
            // Fast tail call - make sure that call target is always computed in RAX
            // so that epilog sequence can generate "jmp rax" to achieve fast tail call.
            ctrlExprCandidates = RBM_RAX;
        }
#ifdef TARGET_X86
        else if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT))
        {
            // On x86, we need to generate a very specific pattern for indirect VSD calls:
            //
            //    3-byte nop
            //    call dword ptr [eax]
            //
            // Where EAX is also used as an argument to the stub dispatch helper. Make
            // sure that the call target address is computed into EAX in this case.
            assert(ctrlExpr->isIndir() && ctrlExpr->isContained());
            ctrlExprCandidates = RBM_VIRTUAL_STUB_TARGET;
        }
#endif // TARGET_X86

#if FEATURE_VARARG
        // If it is a fast tail call, it is already preferenced to use RAX.
        // Therefore, no need set src candidates on call tgt again.
        if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall())
        {
            // Don't assign the call target to any of the argument registers because
            // we will use them to also pass floating point arguments as required
            // by Amd64 ABI.
            ctrlExprCandidates = allRegs(TYP_INT) & ~(RBM_ARG_REGS);
        }
#endif // !FEATURE_VARARG
        srcCount += BuildOperandUses(ctrlExpr, ctrlExprCandidates);
    }

    buildInternalRegisterUses();

    // Now generate defs and kills.
    regMaskTP killMask = getKillSetForCall(call);
    BuildDefsWithKills(call, dstCount, dstCandidates, killMask);
    return srcCount;
}

//------------------------------------------------------------------------
// BuildBlockStore: Build the RefPositions for a block store node.
//
// Arguments:
//    blkNode - The block store node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildBlockStore(GenTreeBlk* blkNode)
{
    GenTree* dstAddr = blkNode->Addr();
    GenTree* src     = blkNode->Data();
    unsigned size    = blkNode->Size();

    GenTree* srcAddrOrFill = nullptr;

    regMaskTP dstAddrRegMask = RBM_NONE;
    regMaskTP srcRegMask     = RBM_NONE;
    regMaskTP sizeRegMask    = RBM_NONE;

    RefPosition* internalIntDef = nullptr;
#ifdef TARGET_X86
    bool internalIsByte = false;
#endif

    if (blkNode->OperIsInitBlkOp())
    {
        if (src->OperIs(GT_INIT_VAL))
        {
            assert(src->isContained());
            src = src->AsUnOp()->gtGetOp1();
        }

        srcAddrOrFill = src;

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindUnroll:
            {
#ifdef TARGET_AMD64
                const bool canUse16BytesSimdMov = !blkNode->IsOnHeapAndContainsReferences();
                const bool willUseSimdMov       = canUse16BytesSimdMov && (size >= 16);
#else
                const bool willUseSimdMov = (size >= 16);
#endif
                if (willUseSimdMov)
                {
                    buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                    SetContainsAVXFlags();
                }

#ifdef TARGET_X86
                if ((size & 1) != 0)
                {
                    // We'll need to store a byte so a byte register is needed on x86.
                    srcRegMask = allByteRegs();
                }
#endif
            }
            break;

            case GenTreeBlk::BlkOpKindRepInstr:
                dstAddrRegMask = RBM_RDI;
                srcRegMask     = RBM_RAX;
                sizeRegMask    = RBM_RCX;
                break;

#ifdef TARGET_AMD64
            case GenTreeBlk::BlkOpKindHelper:
                dstAddrRegMask = RBM_ARG_0;
                srcRegMask     = RBM_ARG_1;
                sizeRegMask    = RBM_ARG_2;
                break;
#endif

            default:
                unreached();
        }
    }
    else
    {
        if (src->OperIs(GT_IND))
        {
            assert(src->isContained());
            srcAddrOrFill = src->AsIndir()->Addr();
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindRepInstr)
            {
                // We need the size of the contiguous Non-GC-region to be in RCX to call rep movsq.
                sizeRegMask = RBM_RCX;
            }

            // The srcAddr must be in a register.  If it was under a GT_IND, we need to subsume all of its
            // sources.
            dstAddrRegMask = RBM_RDI;
            srcRegMask     = RBM_RSI;
        }
        else
        {
            switch (blkNode->gtBlkOpKind)
            {
                case GenTreeBlk::BlkOpKindUnroll:
                    if ((size % XMM_REGSIZE_BYTES) != 0)
                    {
                        regMaskTP regMask = allRegs(TYP_INT);
#ifdef TARGET_X86
                        if ((size & 1) != 0)
                        {
                            // We'll need to store a byte so a byte register is needed on x86.
                            regMask        = allByteRegs();
                            internalIsByte = true;
                        }
#endif
                        internalIntDef = buildInternalIntRegisterDefForNode(blkNode, regMask);
                    }

                    if (size >= XMM_REGSIZE_BYTES)
                    {
                        buildInternalFloatRegisterDefForNode(blkNode, internalFloatRegCandidates());
                        SetContainsAVXFlags();
                    }
                    break;

                case GenTreeBlk::BlkOpKindRepInstr:
                    dstAddrRegMask = RBM_RDI;
                    srcRegMask     = RBM_RSI;
                    sizeRegMask    = RBM_RCX;
                    break;

#ifdef TARGET_AMD64
                case GenTreeBlk::BlkOpKindHelper:
                    dstAddrRegMask = RBM_ARG_0;
                    srcRegMask     = RBM_ARG_1;
                    sizeRegMask    = RBM_ARG_2;
                    break;
#endif

                default:
                    unreached();
            }
        }

        if ((srcAddrOrFill == nullptr) && (srcRegMask != RBM_NONE))
        {
            // This is a local source; we'll use a temp register for its address.
            assert(src->isContained() && src->OperIs(GT_LCL_VAR, GT_LCL_FLD));
            buildInternalIntRegisterDefForNode(blkNode, srcRegMask);
        }
    }

    if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (sizeRegMask != RBM_NONE))
    {
        // Reserve a temp register for the block size argument.
        buildInternalIntRegisterDefForNode(blkNode, sizeRegMask);
    }

    int useCount = 0;

    if (!dstAddr->isContained())
    {
        useCount++;
        BuildUse(dstAddr, dstAddrRegMask);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        useCount += BuildAddrUses(dstAddr);
    }

    if (srcAddrOrFill != nullptr)
    {
        if (!srcAddrOrFill->isContained())
        {
            useCount++;
            BuildUse(srcAddrOrFill, srcRegMask);
        }
        else if (srcAddrOrFill->OperIsAddrMode())
        {
            useCount += BuildAddrUses(srcAddrOrFill);
        }
    }

    if (blkNode->OperIs(GT_STORE_DYN_BLK))
    {
        useCount++;
        BuildUse(blkNode->AsDynBlk()->gtDynamicSize, sizeRegMask);
    }

#ifdef TARGET_X86
    // If we require a byte register on x86, we may run into an over-constrained situation
    // if we have BYTE_REG_COUNT or more uses (currently, it can be at most 4, if both the
    // source and destination have base+index addressing).
    // This is because the byteable register requirement doesn't "reserve" a specific register,
    // and it would be possible for the incoming sources to all be occupying the byteable
    // registers, leaving none free for the internal register.
    // In this scenario, we will require rax to ensure that it is reserved and available.
    // We need to make that modification prior to building the uses for the internal register,
    // so that when we create the use we will also create the RefTypeFixedRef on the RegRecord.
    // We don't expect a useCount of more than 3 for the initBlk case, so we haven't set
    // internalIsByte in that case above.
    assert((useCount < BYTE_REG_COUNT) || !blkNode->OperIsInitBlkOp());
    if (internalIsByte && (useCount >= BYTE_REG_COUNT))
    {
        noway_assert(internalIntDef != nullptr);
        internalIntDef->registerAssignment = RBM_RAX;
    }
#endif

    buildInternalRegisterUses();
    regMaskTP killMask = getKillSetForBlockStore(blkNode);
    BuildDefsWithKills(blkNode, 0, RBM_NONE, killMask);

    return useCount;
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
//------------------------------------------------------------------------
// BuildPutArgStk: Set the NodeInfo for a GT_PUTARG_STK.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildPutArgStk(GenTreePutArgStk* putArgStk)
{
    int srcCount = 0;
    if (putArgStk->gtOp1->gtOper == GT_FIELD_LIST)
    {
        assert(putArgStk->gtOp1->isContained());

        RefPosition* simdTemp   = nullptr;
        RefPosition* intTemp    = nullptr;
        unsigned     prevOffset = putArgStk->GetStackByteSize();
        // We need to iterate over the fields twice; once to determine the need for internal temps,
        // and once to actually build the uses.
        for (GenTreeFieldList::Use& use : putArgStk->gtOp1->AsFieldList()->Uses())
        {
            GenTree* const  fieldNode   = use.GetNode();
            const var_types fieldType   = fieldNode->TypeGet();
            const unsigned  fieldOffset = use.GetOffset();

#ifdef TARGET_X86
            assert(fieldType != TYP_LONG);
#endif // TARGET_X86

#if defined(FEATURE_SIMD)
            // Note that we need to check the GT_FIELD_LIST type, not 'fieldType'. This is because the
            // GT_FIELD_LIST will be TYP_SIMD12 whereas the fieldType might be TYP_SIMD16 for lclVar, where
            // we "round up" to 16.
            if ((use.GetType() == TYP_SIMD12) && (simdTemp == nullptr))
            {
                simdTemp = buildInternalFloatRegisterDefForNode(putArgStk);
            }
#endif // defined(FEATURE_SIMD)

#ifdef TARGET_X86
            if (putArgStk->gtPutArgStkKind == GenTreePutArgStk::Kind::Push)
            {
                // We can treat as a slot any field that is stored at a slot boundary, where the previous
                // field is not in the same slot. (Note that we store the fields in reverse order.)
                const bool fieldIsSlot = ((fieldOffset % 4) == 0) && ((prevOffset - fieldOffset) >= 4);
                if (intTemp == nullptr)
                {
                    intTemp = buildInternalIntRegisterDefForNode(putArgStk);
                }
                if (!fieldIsSlot && varTypeIsByte(fieldType))
                {
                    // If this field is a slot--i.e. it is an integer field that is 4-byte aligned and takes up 4 bytes
                    // (including padding)--we can store the whole value rather than just the byte. Otherwise, we will
                    // need a byte-addressable register for the store. We will enforce this requirement on an internal
                    // register, which we can use to copy multiple byte values.
                    intTemp->registerAssignment &= allByteRegs();
                }
            }
#endif // TARGET_X86

            prevOffset = fieldOffset;
        }

        for (GenTreeFieldList::Use& use : putArgStk->gtOp1->AsFieldList()->Uses())
        {
            GenTree* const fieldNode = use.GetNode();
            if (!fieldNode->isContained())
            {
                BuildUse(fieldNode);
                srcCount++;
            }
        }
        buildInternalRegisterUses();

        return srcCount;
    }

    GenTree*  src  = putArgStk->gtOp1;
    var_types type = src->TypeGet();

#if defined(FEATURE_SIMD) && defined(TARGET_X86)
    // For PutArgStk of a TYP_SIMD12, we need an extra register.
    if (putArgStk->isSIMD12())
    {
        buildInternalFloatRegisterDefForNode(putArgStk, internalFloatRegCandidates());
        BuildUse(putArgStk->gtOp1);
        srcCount = 1;
        buildInternalRegisterUses();
        return srcCount;
    }
#endif // defined(FEATURE_SIMD) && defined(TARGET_X86)

    if (type != TYP_STRUCT)
    {
        return BuildSimple(putArgStk);
    }

    ClassLayout* layout = src->AsObj()->GetLayout();

    ssize_t size = putArgStk->GetStackByteSize();
    switch (putArgStk->gtPutArgStkKind)
    {
        case GenTreePutArgStk::Kind::Push:
        case GenTreePutArgStk::Kind::PushAllSlots:
        case GenTreePutArgStk::Kind::Unroll:
            // If we have a remainder smaller than XMM_REGSIZE_BYTES, we need an integer temp reg.
            if (!layout->HasGCPtr() && (size & (XMM_REGSIZE_BYTES - 1)) != 0)
            {
                regMaskTP regMask = allRegs(TYP_INT);
                buildInternalIntRegisterDefForNode(putArgStk, regMask);
            }

#ifdef TARGET_X86
            if (size >= 8)
#else  // !TARGET_X86
            if (size >= XMM_REGSIZE_BYTES)
#endif // !TARGET_X86
            {
                // If we have a buffer larger than or equal to XMM_REGSIZE_BYTES on x64/ux,
                // or larger than or equal to 8 bytes on x86, reserve an XMM register to use it for a
                // series of 16-byte loads and stores.
                buildInternalFloatRegisterDefForNode(putArgStk, internalFloatRegCandidates());
                SetContainsAVXFlags();
            }
            break;

        case GenTreePutArgStk::Kind::RepInstr:
            buildInternalIntRegisterDefForNode(putArgStk, RBM_RDI);
            buildInternalIntRegisterDefForNode(putArgStk, RBM_RCX);
            buildInternalIntRegisterDefForNode(putArgStk, RBM_RSI);
            break;

        default:
            unreached();
    }

    srcCount = BuildOperandUses(src);
    buildInternalRegisterUses();

#ifdef TARGET_X86
    // There are only 4 (BYTE_REG_COUNT) byteable registers on x86. If we require a byteable internal register,
    // we must have less than BYTE_REG_COUNT sources.
    // If we have BYTE_REG_COUNT or more sources, and require a byteable internal register, we need to reserve
    // one explicitly (see BuildBlockStore()).
    assert(srcCount < BYTE_REG_COUNT);
#endif

    return srcCount;
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

//------------------------------------------------------------------------
// BuildLclHeap: Set the NodeInfo for a GT_LCLHEAP.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildLclHeap(GenTree* tree)
{
    int srcCount = 1;

    // Need a variable number of temp regs (see genLclHeap() in codegenamd64.cpp):
    // Here '-' means don't care.
    //
    //     Size?                    Init Memory?         # temp regs
    //      0                            -                  0 (returns 0)
    //      const and <=6 reg words      -                  0 (pushes '0')
    //      const and >6 reg words       Yes                0 (pushes '0')
    //      const and <PageSize          No                 0 (amd64) 1 (x86)
    //                                                        (x86:tmpReg for sutracting from esp)
    //      const and >=PageSize         No                 2 (regCnt and tmpReg for subtracing from sp)
    //      Non-const                    Yes                0 (regCnt=targetReg and pushes '0')
    //      Non-const                    No                 2 (regCnt and tmpReg for subtracting from sp)
    //
    // Note: Here we don't need internal register to be different from targetReg.
    // Rather, require it to be different from operand's reg.

    GenTree* size = tree->gtGetOp1();
    if (size->IsCnsIntOrI())
    {
        assert(size->isContained());
        srcCount       = 0;
        size_t sizeVal = size->AsIntCon()->gtIconVal;

        if (sizeVal == 0)
        {
            buildInternalIntRegisterDefForNode(tree);
        }
        else
        {
            // Compute the amount of memory to properly STACK_ALIGN.
            // Note: The Gentree node is not updated here as it is cheap to recompute stack aligned size.
            // This should also help in debugging as we can examine the original size specified with localloc.
            sizeVal = AlignUp(sizeVal, STACK_ALIGN);

            // For small allocations up to 6 pointer sized words (i.e. 48 bytes of localloc)
            // we will generate 'push 0'.
            assert((sizeVal % REGSIZE_BYTES) == 0);
            size_t cntRegSizedWords = sizeVal / REGSIZE_BYTES;
            if (cntRegSizedWords > 6)
            {
                if (!compiler->info.compInitMem)
                {
                    // No need to initialize allocated stack space.
                    if (sizeVal < compiler->eeGetPageSize())
                    {
#ifdef TARGET_X86
                        // x86 needs a register here to avoid generating "sub" on ESP.
                        buildInternalIntRegisterDefForNode(tree);
#endif
                    }
                    else
                    {
                        // We need two registers: regCnt and RegTmp
                        buildInternalIntRegisterDefForNode(tree);
                        buildInternalIntRegisterDefForNode(tree);
                    }
                }
            }
        }
    }
    else
    {
        if (!compiler->info.compInitMem)
        {
            buildInternalIntRegisterDefForNode(tree);
            buildInternalIntRegisterDefForNode(tree);
        }
        BuildUse(size);
    }
    buildInternalRegisterUses();
    BuildDef(tree);
    return srcCount;
}

//------------------------------------------------------------------------
// BuildModDiv: Set the NodeInfo for GT_MOD/GT_DIV/GT_UMOD/GT_UDIV.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildModDiv(GenTree* tree)
{
    GenTree*  op1           = tree->gtGetOp1();
    GenTree*  op2           = tree->gtGetOp2();
    regMaskTP dstCandidates = RBM_NONE;
    int       srcCount      = 0;

    if (varTypeIsFloating(tree->TypeGet()))
    {
        return BuildSimple(tree);
    }

    // Amd64 Div/Idiv instruction:
    //    Dividend in RAX:RDX  and computes
    //    Quotient in RAX, Remainder in RDX

    if (tree->OperGet() == GT_MOD || tree->OperGet() == GT_UMOD)
    {
        // We are interested in just the remainder.
        // RAX is used as a trashable register during computation of remainder.
        dstCandidates = RBM_RDX;
    }
    else
    {
        // We are interested in just the quotient.
        // RDX gets used as trashable register during computation of quotient
        dstCandidates = RBM_RAX;
    }

#ifdef TARGET_X86
    if (op1->OperGet() == GT_LONG)
    {
        assert(op1->isContained());

        // To avoid reg move would like to have op1's low part in RAX and high part in RDX.
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();
        assert(!loVal->isContained() && !hiVal->isContained());

        assert(op2->IsCnsIntOrI());
        assert(tree->OperGet() == GT_UMOD);

        // This situation also requires an internal register.
        buildInternalIntRegisterDefForNode(tree);

        BuildUse(loVal, RBM_EAX);
        BuildUse(hiVal, RBM_EDX);
        srcCount = 2;
    }
    else
#endif
    {
        // If possible would like to have op1 in RAX to avoid a register move.
        RefPosition* op1Use = BuildUse(op1, RBM_EAX);
        tgtPrefUse          = op1Use;
        srcCount            = 1;
    }

    srcCount += BuildDelayFreeUses(op2, op1, allRegs(TYP_INT) & ~(RBM_RAX | RBM_RDX));

    buildInternalRegisterUses();

    regMaskTP killMask = getKillSetForModDiv(tree->AsOp());
    BuildDefsWithKills(tree, 1, dstCandidates, killMask);
    return srcCount;
}

//------------------------------------------------------------------------
// BuildIntrinsic: Set the NodeInfo for a GT_INTRINSIC.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildIntrinsic(GenTree* tree)
{
    // Both operand and its result must be of floating point type.
    GenTree* op1 = tree->gtGetOp1();
    assert(varTypeIsFloating(op1));
    assert(op1->TypeGet() == tree->TypeGet());
    RefPosition* internalFloatDef = nullptr;

    switch (tree->AsIntrinsic()->gtIntrinsicName)
    {
        case NI_System_Math_Abs:
            // Abs(float x) = x & 0x7fffffff
            // Abs(double x) = x & 0x7ffffff ffffffff

            // In case of Abs we need an internal register to hold mask.

            // TODO-XArch-CQ: avoid using an internal register for the mask.
            // Andps or andpd both will operate on 128-bit operands.
            // The data section constant to hold the mask is a 64-bit size.
            // Therefore, we need both the operand and mask to be in
            // xmm register. When we add support in emitter to emit 128-bit
            // data constants and instructions that operate on 128-bit
            // memory operands we can avoid the need for an internal register.
            internalFloatDef = buildInternalFloatRegisterDefForNode(tree, internalFloatRegCandidates());
            break;

        case NI_System_Math_Ceiling:
        case NI_System_Math_Floor:
        case NI_System_Math_Round:
        case NI_System_Math_Sqrt:
            break;

        default:
            // Right now only Sqrt/Abs are treated as math intrinsics
            noway_assert(!"Unsupported math intrinsic");
            unreached();
            break;
    }
    assert(tree->gtGetOp2IfPresent() == nullptr);
    int srcCount;
    if (op1->isContained())
    {
        srcCount = BuildOperandUses(op1);
    }
    else
    {
        tgtPrefUse = BuildUse(op1);
        srcCount   = 1;
    }
    if (internalFloatDef != nullptr)
    {
        buildInternalRegisterUses();
    }
    BuildDef(tree);
    return srcCount;
}

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// BuildSIMD: Set the NodeInfo for a GT_SIMD tree.
//
// Arguments:
//    tree       - The GT_SIMD node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildSIMD(GenTreeSIMD* simdTree)
{
    // All intrinsics have a dstCount of 1
    assert(simdTree->IsValue());

    bool      buildUses     = true;
    regMaskTP dstCandidates = RBM_NONE;

    if (simdTree->isContained())
    {
        // Only SIMDIntrinsicInit can be contained
        assert(simdTree->gtSIMDIntrinsicID == SIMDIntrinsicInit);
    }
    SetContainsAVXFlags(simdTree->GetSimdSize());
    GenTree* op1      = simdTree->gtGetOp1();
    GenTree* op2      = simdTree->gtGetOp2();
    int      srcCount = 0;

    switch (simdTree->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
        {
            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's,
            // or a float constant with 16 or 32 byte simdType (AVX case)
            //
            // Note that for small int base types, the initVal has been constructed so that
            // we can use the full int value.
            CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(TARGET_64BIT)
            if (op1->OperGet() == GT_LONG)
            {
                assert(op1->isContained());
                GenTree* op1lo = op1->gtGetOp1();
                GenTree* op1hi = op1->gtGetOp2();

                if (op1lo->isContained())
                {
                    srcCount = 0;
                    assert(op1hi->isContained());
                    assert((op1lo->IsIntegralConst(0) && op1hi->IsIntegralConst(0)) ||
                           (op1lo->IsIntegralConst(-1) && op1hi->IsIntegralConst(-1)));
                }
                else
                {
                    srcCount = 2;
                    buildInternalFloatRegisterDefForNode(simdTree);
                    setInternalRegsDelayFree = true;
                }

                if (srcCount == 2)
                {
                    BuildUse(op1lo, RBM_EAX);
                    BuildUse(op1hi, RBM_EDX);
                }
                buildUses = false;
            }
#endif // !defined(TARGET_64BIT)
        }
        break;

        case SIMDIntrinsicInitN:
        {
            var_types baseType = simdTree->GetSimdBaseType();
            srcCount           = (short)(simdTree->GetSimdSize() / genTypeSize(baseType));
            // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
            buildInternalFloatRegisterDefForNode(simdTree);
            int initCount = 0;
            for (GenTree* list = op1; list != nullptr; list = list->gtGetOp2())
            {
                assert(list->OperGet() == GT_LIST);
                GenTree* listItem = list->gtGetOp1();
                assert(listItem->TypeGet() == baseType);
                assert(!listItem->isContained());
                BuildUse(listItem);
                initCount++;
            }
            assert(initCount == srcCount);
            buildUses = false;
        }
        break;

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            break;

        case SIMDIntrinsicSub:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseOr:
            break;

        case SIMDIntrinsicEqual:
            break;

        case SIMDIntrinsicCast:
            break;

        case SIMDIntrinsicConvertToSingle:
            if (simdTree->GetSimdBaseType() == TYP_UINT)
            {
                // We need an internal register different from targetReg.
                setInternalRegsDelayFree = true;
                buildInternalFloatRegisterDefForNode(simdTree);
                buildInternalFloatRegisterDefForNode(simdTree);
                // We also need an integer register.
                buildInternalIntRegisterDefForNode(simdTree);
            }
            break;

        case SIMDIntrinsicConvertToInt32:
            break;

        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            if (varTypeIsIntegral(simdTree->GetSimdBaseType()))
            {
                // We need an internal register different from targetReg.
                setInternalRegsDelayFree = true;
                buildInternalFloatRegisterDefForNode(simdTree);
            }
            break;

        case SIMDIntrinsicConvertToInt64:
            // We need an internal register different from targetReg.
            setInternalRegsDelayFree = true;
            buildInternalFloatRegisterDefForNode(simdTree);
            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                buildInternalFloatRegisterDefForNode(simdTree);
            }
            // We also need an integer register.
            buildInternalIntRegisterDefForNode(simdTree);
            break;

        case SIMDIntrinsicConvertToDouble:
            // We need an internal register different from targetReg.
            setInternalRegsDelayFree = true;
            buildInternalFloatRegisterDefForNode(simdTree);
#ifdef TARGET_X86
            if (simdTree->GetSimdBaseType() == TYP_LONG)
            {
                buildInternalFloatRegisterDefForNode(simdTree);
                buildInternalFloatRegisterDefForNode(simdTree);
            }
            else
#endif
                if ((compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported) ||
                    (simdTree->GetSimdBaseType() == TYP_ULONG))
            {
                buildInternalFloatRegisterDefForNode(simdTree);
            }
            // We also need an integer register.
            buildInternalIntRegisterDefForNode(simdTree);
            break;

        case SIMDIntrinsicNarrow:
            // We need an internal register different from targetReg.
            setInternalRegsDelayFree = true;
            buildInternalFloatRegisterDefForNode(simdTree);
            if ((compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported) && (simdTree->GetSimdBaseType() != TYP_DOUBLE))
            {
                buildInternalFloatRegisterDefForNode(simdTree);
            }
            break;

        case SIMDIntrinsicShuffleSSE2:
            // Second operand is an integer constant and marked as contained.
            assert(simdTree->gtGetOp2()->isContainedIntOrIImmed());
            break;

        default:
            noway_assert(!"Unimplemented SIMD node type.");
            unreached();
    }
    if (buildUses)
    {
        assert(!op1->OperIs(GT_LIST));
        assert(srcCount == 0);
        // This is overly conservative, but is here for zero diffs.
        srcCount = BuildRMWUses(simdTree);
    }
    buildInternalRegisterUses();
    BuildDef(simdTree, dstCandidates);
    return srcCount;
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
//------------------------------------------------------------------------
// BuildHWIntrinsic: Set the NodeInfo for a GT_HWINTRINSIC tree.
//
// Arguments:
//    tree       - The GT_HWINTRINSIC node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildHWIntrinsic(GenTreeHWIntrinsic* intrinsicTree)
{
    NamedIntrinsic      intrinsicId = intrinsicTree->gtHWIntrinsicId;
    var_types           baseType    = intrinsicTree->GetSimdBaseType();
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                 numArgs     = HWIntrinsicInfo::lookupNumArgs(intrinsicTree);

    // Set the AVX Flags if this instruction may use VEX encoding for SIMD operations.
    // Note that this may be true even if the ISA is not AVX (e.g. for platform-agnostic intrinsics
    // or non-AVX intrinsics that will use VEX encoding if it is available on the target).
    if (intrinsicTree->isSIMD())
    {
        SetContainsAVXFlags(intrinsicTree->GetSimdSize());
    }

    GenTree* op1    = intrinsicTree->gtGetOp1();
    GenTree* op2    = intrinsicTree->gtGetOp2();
    GenTree* op3    = nullptr;
    GenTree* lastOp = nullptr;

    int srcCount = 0;
    int dstCount = intrinsicTree->IsValue() ? 1 : 0;

    regMaskTP dstCandidates = RBM_NONE;

    if (op1 == nullptr)
    {
        assert(op2 == nullptr);
        assert(numArgs == 0);
    }
    else
    {
        if (op1->OperIsList())
        {
            assert(op2 == nullptr);
            assert(numArgs >= 3);

            GenTreeArgList* argList = op1->AsArgList();

            op1     = argList->Current();
            argList = argList->Rest();

            op2     = argList->Current();
            argList = argList->Rest();

            op3 = argList->Current();

            while (argList->Rest() != nullptr)
            {
                argList = argList->Rest();
            }

            lastOp  = argList->Current();
            argList = argList->Rest();

            assert(argList == nullptr);
        }
        else if (op2 != nullptr)
        {
            assert(numArgs == 2);
            lastOp = op2;
        }
        else
        {
            assert(numArgs == 1);
            lastOp = op1;
        }

        assert(lastOp != nullptr);

        bool buildUses = true;

        if ((category == HW_Category_IMM) && !HWIntrinsicInfo::NoJmpTableImm(intrinsicId))
        {
            if (HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && !lastOp->isContainedIntOrIImmed())
            {
                assert(!lastOp->IsCnsIntOrI());

                // We need two extra reg when lastOp isn't a constant so
                // the offset into the jump table for the fallback path
                // can be computed.
                buildInternalIntRegisterDefForNode(intrinsicTree);
                buildInternalIntRegisterDefForNode(intrinsicTree);
            }
        }

        // Determine whether this is an RMW operation where op2+ must be marked delayFree so that it
        // is not allocated the same register as the target.
        bool isRMW = intrinsicTree->isRMWHWIntrinsic(compiler);

        // Create internal temps, and handle any other special requirements.
        // Note that the default case for building uses will handle the RMW flag, but if the uses
        // are built in the individual cases, buildUses is set to false, and any RMW handling (delayFree)
        // must be handled within the case.
        switch (intrinsicId)
        {
            case NI_Vector128_CreateScalarUnsafe:
            case NI_Vector128_ToScalar:
            case NI_Vector256_CreateScalarUnsafe:
            case NI_Vector256_ToScalar:
            {
                assert(numArgs == 1);

                if (varTypeIsFloating(baseType))
                {
                    if (op1->isContained())
                    {
                        srcCount += BuildOperandUses(op1);
                    }
                    else
                    {
                        // We will either be in memory and need to be moved
                        // into a register of the appropriate size or we
                        // are already in an XMM/YMM register and can stay
                        // where we are.

                        tgtPrefUse = BuildUse(op1);
                        srcCount += 1;
                    }

                    buildUses = false;
                }
                break;
            }

            case NI_Vector128_GetElement:
            case NI_Vector256_GetElement:
            {
                assert(numArgs == 2);

                if (!op2->OperIsConst() && !op1->isContained())
                {
                    // If the index is not a constant or op1 is in register,
                    // we will use the SIMD temp location to store the vector.
                    compiler->getSIMDInitTempVarNum();
                }
                break;
            }

            case NI_Vector128_ToVector256:
            case NI_Vector128_ToVector256Unsafe:
            case NI_Vector256_GetLower:
            {
                assert(numArgs == 1);

                if (op1->isContained())
                {
                    srcCount += BuildOperandUses(op1);
                }
                else
                {
                    // We will either be in memory and need to be moved
                    // into a register of the appropriate size or we
                    // are already in an XMM/YMM register and can stay
                    // where we are.

                    tgtPrefUse = BuildUse(op1);
                    srcCount += 1;
                }

                buildUses = false;
                break;
            }

            case NI_SSE2_MaskMove:
            {
                assert(numArgs == 3);
                assert(!isRMW);

                // MaskMove hardcodes the destination (op3) in DI/EDI/RDI
                srcCount += BuildOperandUses(op1);
                srcCount += BuildOperandUses(op2);
                srcCount += BuildOperandUses(op3, RBM_EDI);

                buildUses = false;
                break;
            }

            case NI_SSE41_BlendVariable:
            {
                assert(numArgs == 3);

                if (!compiler->canUseVexEncoding())
                {
                    assert(isRMW);

                    // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
                    tgtPrefUse = BuildUse(op1);

                    srcCount += 1;
                    srcCount += op2->isContained() ? BuildOperandUses(op2) : BuildDelayFreeUses(op2, op1);
                    srcCount += BuildDelayFreeUses(op3, op1, RBM_XMM0);

                    buildUses = false;
                }
                break;
            }

            case NI_SSE41_Extract:
            {
                assert(!varTypeIsFloating(baseType));

#ifdef TARGET_X86
                if (varTypeIsByte(baseType))
                {
                    dstCandidates = allByteRegs();
                }
#endif
                break;
            }

#ifdef TARGET_X86
            case NI_SSE42_Crc32:
            case NI_SSE42_X64_Crc32:
            {
                // TODO-XArch-Cleanup: Currently we use the BaseType to bring the type of the second argument
                // to the code generator. We may want to encode the overload info in another way.

                assert(numArgs == 2);
                assert(isRMW);

                // CRC32 may operate over "byte" but on x86 only RBM_BYTE_REGS can be used as byte registers.
                tgtPrefUse = BuildUse(op1);

                srcCount += 1;
                srcCount += BuildDelayFreeUses(op2, op1, varTypeIsByte(baseType) ? allByteRegs() : RBM_NONE);

                buildUses = false;
                break;
            }
#endif // TARGET_X86

            case NI_BMI2_MultiplyNoFlags:
            case NI_BMI2_X64_MultiplyNoFlags:
            {
                assert(numArgs == 2 || numArgs == 3);
                srcCount += BuildOperandUses(op1, RBM_EDX);
                srcCount += BuildOperandUses(op2);
                if (numArgs == 3)
                {
                    // op3 reg should be different from target reg to
                    // store the lower half result after executing the instruction
                    srcCount += BuildDelayFreeUses(op3, op1);
                    // Need a internal register different from the dst to take the lower half result
                    buildInternalIntRegisterDefForNode(intrinsicTree);
                    setInternalRegsDelayFree = true;
                }
                buildUses = false;
                break;
            }

            case NI_FMA_MultiplyAdd:
            case NI_FMA_MultiplyAddNegated:
            case NI_FMA_MultiplyAddNegatedScalar:
            case NI_FMA_MultiplyAddScalar:
            case NI_FMA_MultiplyAddSubtract:
            case NI_FMA_MultiplySubtract:
            case NI_FMA_MultiplySubtractAdd:
            case NI_FMA_MultiplySubtractNegated:
            case NI_FMA_MultiplySubtractNegatedScalar:
            case NI_FMA_MultiplySubtractScalar:
            {
                assert(numArgs == 3);
                assert(isRMW);

                const bool copiesUpperBits = HWIntrinsicInfo::CopiesUpperBits(intrinsicId);

                // Intrinsics with CopyUpperBits semantics cannot have op1 be contained
                assert(!copiesUpperBits || !op1->isContained());

                if (op2->isContained())
                {
                    // 132 form: op1 = (op1 * op3) + [op2]

                    tgtPrefUse = BuildUse(op1);

                    srcCount += 1;
                    srcCount += BuildOperandUses(op2);
                    srcCount += BuildDelayFreeUses(op3, op1);
                }
                else if (op1->isContained())
                {
                    // 231 form: op3 = (op2 * op3) + [op1]

                    tgtPrefUse = BuildUse(op3);

                    srcCount += BuildOperandUses(op1);
                    srcCount += BuildDelayFreeUses(op2, op1);
                    srcCount += 1;
                }
                else
                {
                    // 213 form: op1 = (op2 * op1) + [op3]

                    tgtPrefUse = BuildUse(op1);
                    srcCount += 1;

                    if (copiesUpperBits)
                    {
                        srcCount += BuildDelayFreeUses(op2, op1);
                    }
                    else
                    {
                        tgtPrefUse2 = BuildUse(op2);
                        srcCount += 1;
                    }

                    srcCount += op3->isContained() ? BuildOperandUses(op3) : BuildDelayFreeUses(op3, op1);
                }

                buildUses = false;
                break;
            }

            case NI_AVXVNNI_MultiplyWideningAndAdd:
            case NI_AVXVNNI_MultiplyWideningAndAddSaturate:
            {
                assert(numArgs == 3);

                tgtPrefUse = BuildUse(op1);
                srcCount += 1;
                srcCount += BuildDelayFreeUses(op2, op1);
                srcCount += op3->isContained() ? BuildOperandUses(op3) : BuildDelayFreeUses(op3, op1);

                buildUses = false;
                break;
            }

            case NI_AVX2_GatherVector128:
            case NI_AVX2_GatherVector256:
            {
                assert(numArgs == 3);
                assert(!isRMW);

                // Any pair of the index, mask, or destination registers should be different
                srcCount += BuildOperandUses(op1);
                srcCount += BuildDelayFreeUses(op2, op1);

                // op3 should always be contained
                assert(op3->isContained());

                // get a tmp register for mask that will be cleared by gather instructions
                buildInternalFloatRegisterDefForNode(intrinsicTree, allSIMDRegs());
                setInternalRegsDelayFree = true;

                buildUses = false;
                break;
            }

            case NI_AVX2_GatherMaskVector128:
            case NI_AVX2_GatherMaskVector256:
            {
                assert(numArgs == 5);
                assert(!isRMW);
                assert(intrinsicTree->gtGetOp1()->OperIsList());

                GenTreeArgList* argList = intrinsicTree->gtGetOp1()->AsArgList()->Rest()->Rest()->Rest();
                GenTree*        op4     = argList->Current();

                // Any pair of the index, mask, or destination registers should be different
                srcCount += BuildOperandUses(op1);
                srcCount += BuildDelayFreeUses(op2, op1);
                srcCount += BuildDelayFreeUses(op3, op1);
                srcCount += BuildDelayFreeUses(op4, op1);

                // op5 should always be contained
                assert(argList->Rest()->Current()->isContained());

                // get a tmp register for mask that will be cleared by gather instructions
                buildInternalFloatRegisterDefForNode(intrinsicTree, allSIMDRegs());
                setInternalRegsDelayFree = true;

                buildUses = false;
                break;
            }

            default:
            {
                assert((intrinsicId > NI_HW_INTRINSIC_START) && (intrinsicId < NI_HW_INTRINSIC_END));
                break;
            }
        }

        if (buildUses)
        {
            assert((numArgs > 0) && (numArgs < 4));

            if (intrinsicTree->OperIsMemoryLoadOrStore())
            {
                srcCount += BuildAddrUses(op1);
            }
            else if (isRMW && !op1->isContained())
            {
                tgtPrefUse = BuildUse(op1);
                srcCount += 1;
            }
            else
            {
                srcCount += BuildOperandUses(op1);
            }

            if (op2 != nullptr)
            {
                if (op2->OperIs(GT_HWINTRINSIC) && op2->AsHWIntrinsic()->OperIsMemoryLoad() && op2->isContained())
                {
                    srcCount += BuildAddrUses(op2->gtGetOp1());
                }
                else if (isRMW)
                {
                    if (!op2->isContained() && HWIntrinsicInfo::IsCommutative(intrinsicId))
                    {
                        // When op2 is not contained and we are commutative, we can set op2
                        // to also be a tgtPrefUse. Codegen will then swap the operands.

                        tgtPrefUse2 = BuildUse(op2);
                        srcCount += 1;
                    }
                    else if (!op2->isContained() || varTypeIsArithmetic(intrinsicTree->TypeGet()))
                    {
                        // When op2 is not contained or if we are producing a scalar value
                        // we need to mark it as delay free because the operand and target
                        // exist in the same register set.
                        srcCount += BuildDelayFreeUses(op2, op1);
                    }
                    else
                    {
                        // When op2 is contained and we are not producing a scalar value we
                        // have no concerns of overwriting op2 because they exist in different
                        // register sets.

                        srcCount += BuildOperandUses(op2);
                    }
                }
                else
                {
                    srcCount += BuildOperandUses(op2);
                }

                if (op3 != nullptr)
                {
                    srcCount += isRMW ? BuildDelayFreeUses(op3, op1) : BuildOperandUses(op3);
                }
            }
        }

        buildInternalRegisterUses();
    }

    if (dstCount == 1)
    {
        BuildDef(intrinsicTree, dstCandidates);
    }
    else
    {
        assert(dstCount == 0);
    }

    return srcCount;
}
#endif

//------------------------------------------------------------------------
// BuildCast: Set the NodeInfo for a GT_CAST.
//
// Arguments:
//    cast - The GT_CAST node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildCast(GenTreeCast* cast)
{
    GenTree* src = cast->gtGetOp1();

    const var_types srcType  = genActualType(src->TypeGet());
    const var_types castType = cast->gtCastType;

    regMaskTP candidates = RBM_NONE;
#ifdef TARGET_X86
    if (varTypeIsByte(castType))
    {
        candidates = allByteRegs();
    }

    assert(!varTypeIsLong(srcType) || (src->OperIs(GT_LONG) && src->isContained()));
#else
    // Overflow checking cast from TYP_(U)LONG to TYP_UINT requires a temporary
    // register to extract the upper 32 bits of the 64 bit source register.
    if (cast->gtOverflow() && varTypeIsLong(srcType) && (castType == TYP_UINT))
    {
        // Here we don't need internal register to be different from targetReg,
        // rather require it to be different from operand's reg.
        buildInternalIntRegisterDefForNode(cast);
    }
#endif

    int srcCount = BuildOperandUses(src, candidates);
    buildInternalRegisterUses();
    BuildDef(cast, candidates);
    return srcCount;
}

//-----------------------------------------------------------------------------------------
// BuildIndir: Specify register requirements for address expression of an indirection operation.
//
// Arguments:
//    indirTree    -   GT_IND or GT_STOREIND gentree node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    // struct typed indirs are expected only on rhs of a block copy,
    // but in this case they must be contained.
    assert(indirTree->TypeGet() != TYP_STRUCT);

#ifdef FEATURE_SIMD
    RefPosition* internalFloatDef = nullptr;
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // If indirTree is of TYP_SIMD12, addr is not contained. See comment in LowerIndir().
        assert(!indirTree->Addr()->isContained());

        // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
        // To assemble the vector properly we would need an additional
        // XMM register.
        internalFloatDef = buildInternalFloatRegisterDefForNode(indirTree);

        // In case of GT_IND we need an internal register different from targetReg and
        // both of the registers are used at the same time.
        if (indirTree->OperGet() == GT_IND)
        {
            setInternalRegsDelayFree = true;
        }
    }
#endif // FEATURE_SIMD

    regMaskTP indirCandidates = RBM_NONE;
    int       srcCount        = BuildIndirUses(indirTree, indirCandidates);
    if (indirTree->gtOper == GT_STOREIND)
    {
        GenTree* source = indirTree->gtGetOp2();
        if (indirTree->AsStoreInd()->IsRMWMemoryOp())
        {
            // Because 'source' is contained, we haven't yet determined its special register requirements, if any.
            // As it happens, the Shift or Rotate cases are the only ones with special requirements.
            assert(source->isContained() && source->OperIsRMWMemOp());

            if (source->OperIsShiftOrRotate())
            {
                srcCount += BuildShiftRotate(source);
            }
            else
            {
                regMaskTP srcCandidates = RBM_NONE;

#ifdef TARGET_X86
                // Determine if we need byte regs for the non-mem source, if any.
                // Note that BuildShiftRotate (above) will handle the byte requirement as needed,
                // but STOREIND isn't itself an RMW op, so we have to explicitly set it for that case.

                GenTree*      nonMemSource = nullptr;
                GenTreeIndir* otherIndir   = nullptr;

                if (indirTree->AsStoreInd()->IsRMWDstOp1())
                {
                    otherIndir = source->gtGetOp1()->AsIndir();
                    if (source->OperIsBinary())
                    {
                        nonMemSource = source->gtGetOp2();
                    }
                }
                else if (indirTree->AsStoreInd()->IsRMWDstOp2())
                {
                    otherIndir   = source->gtGetOp2()->AsIndir();
                    nonMemSource = source->gtGetOp1();
                }
                if ((nonMemSource != nullptr) && !nonMemSource->isContained() && varTypeIsByte(indirTree))
                {
                    srcCandidates = RBM_BYTE_REGS;
                }
                if (otherIndir != nullptr)
                {
                    // Any lclVars in the addressing mode of this indirection are contained.
                    // If they are marked as lastUse, transfer the last use flag to the store indir.
                    GenTree* base    = otherIndir->Base();
                    GenTree* dstBase = indirTree->Base();
                    CheckAndMoveRMWLastUse(base, dstBase);
                    GenTree* index    = otherIndir->Index();
                    GenTree* dstIndex = indirTree->Index();
                    CheckAndMoveRMWLastUse(index, dstIndex);
                }
#endif // TARGET_X86

                srcCount += BuildBinaryUses(source->AsOp(), srcCandidates);
            }
        }
        else
        {
#ifdef TARGET_X86
            if (varTypeIsByte(indirTree) && !source->isContained())
            {
                BuildUse(source, allByteRegs());
                srcCount++;
            }
            else
#endif
            {
                srcCount += BuildOperandUses(source);
            }
        }
    }

#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(indirTree))
    {
        SetContainsAVXFlags(genTypeSize(indirTree->TypeGet()));
    }
    buildInternalRegisterUses();
#endif // FEATURE_SIMD

#ifdef TARGET_X86
    // There are only BYTE_REG_COUNT byteable registers on x86. If we have a source that requires
    // such a register, we must have no more than BYTE_REG_COUNT sources.
    // If we have more than BYTE_REG_COUNT sources, and require a byteable register, we need to reserve
    // one explicitly (see BuildBlockStore()).
    // (Note that the assert below doesn't count internal registers because we only have
    // floating point internal registers, if any).
    assert(srcCount <= BYTE_REG_COUNT);
#endif

    if (indirTree->gtOper != GT_STOREIND)
    {
        BuildDef(indirTree);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildMul: Set the NodeInfo for a multiply.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildMul(GenTree* tree)
{
    assert(tree->OperIsMul());
    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    // Only non-floating point mul has special requirements
    if (varTypeIsFloating(tree->TypeGet()))
    {
        return BuildSimple(tree);
    }

    int       srcCount      = BuildBinaryUses(tree->AsOp());
    int       dstCount      = 1;
    regMaskTP dstCandidates = RBM_NONE;

    bool isUnsignedMultiply    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool requiresOverflowCheck = tree->gtOverflowEx();

    // There are three forms of x86 multiply:
    // one-op form:     RDX:RAX = RAX * r/m
    // two-op form:     reg *= r/m
    // three-op form:   reg = r/m * imm

    // This special widening 32x32->64 MUL is not used on x64
    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_X86)
    if (tree->OperGet() != GT_MUL_LONG)
#endif
    {
        assert((tree->gtFlags & GTF_MUL_64RSLT) == 0);
    }

    // We do use the widening multiply to implement
    // the overflow checking for unsigned multiply
    //
    if (isUnsignedMultiply && requiresOverflowCheck)
    {
        // The only encoding provided is RDX:RAX = RAX * rm
        //
        // Here we set RAX as the only destination candidate
        // In LSRA we set the kill set for this operation to RBM_RAX|RBM_RDX
        //
        dstCandidates = RBM_RAX;
    }
    else if (tree->OperGet() == GT_MULHI)
    {
        // Have to use the encoding:RDX:RAX = RAX * rm. Since we only care about the
        // upper 32 bits of the result set the destination candidate to REG_RDX.
        dstCandidates = RBM_RDX;
    }
#if defined(TARGET_X86)
    else if (tree->OperGet() == GT_MUL_LONG)
    {
        // have to use the encoding:RDX:RAX = RAX * rm
        dstCandidates = RBM_RAX | RBM_RDX;
        dstCount      = 2;
    }
#endif
    GenTree* containedMemOp = nullptr;
    if (op1->isContained() && !op1->IsCnsIntOrI())
    {
        assert(!op2->isContained() || op2->IsCnsIntOrI());
        containedMemOp = op1;
    }
    else if (op2->isContained() && !op2->IsCnsIntOrI())
    {
        containedMemOp = op2;
    }
    regMaskTP killMask = getKillSetForMul(tree->AsOp());
    BuildDefsWithKills(tree, dstCount, dstCandidates, killMask);
    return srcCount;
}

//------------------------------------------------------------------------------
// SetContainsAVXFlags: Set ContainsAVX flag when it is floating type, set
// Contains256bitAVX flag when SIMD vector size is 32 bytes
//
// Arguments:
//    isFloatingPointType   - true if it is floating point type
//    sizeOfSIMDVector      - SIMD Vector size
//
void LinearScan::SetContainsAVXFlags(unsigned sizeOfSIMDVector /* = 0*/)
{
    if (compiler->canUseVexEncoding())
    {
        compiler->compExactlyDependsOn(InstructionSet_AVX);
        compiler->GetEmitter()->SetContainsAVX(true);
        if (sizeOfSIMDVector == 32)
        {
            compiler->GetEmitter()->SetContains256bitAVX(true);
        }
    }
}

#endif // TARGET_XARCH
