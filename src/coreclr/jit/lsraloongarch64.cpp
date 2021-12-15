// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for LOONGARCH64                  XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the LOONGARCH64 architecture.                                            XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_LOONGARCH64

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
    regMaskTP dstCandidates = RBM_NONE;
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

        case GT_STORE_LCL_VAR:
            if (tree->IsMultiRegLclVar() && isCandidateMultiRegLclVar(tree->AsLclVar()))
            {
                dstCount = compiler->lvaGetDesc(tree->AsLclVar()->GetLclNum())->lvFieldCnt;
            }
            FALLTHROUGH;

        case GT_STORE_LCL_FLD:
            srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_FIELD_LIST:
            // These should always be contained. We don't correctly allocate or
            // generate code for a non-contained GT_FIELD_LIST.
            noway_assert(!"Non-contained GT_FIELD_LIST");
            srcCount = 0;
            break;

        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_PROF_HOOK:
            srcCount = 0;
            assert(dstCount == 0);
            killMask = getKillSetForProfilerHook();
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        case GT_START_PREEMPTGC:
            // This kills GC refs in callee save regs
            srcCount = 0;
            assert(dstCount == 0);
            BuildDefsWithKills(tree, 0, RBM_NONE, RBM_NONE);
            break;

        case GT_CNS_DBL:
        {
            GenTreeDblCon* dblConst   = tree->AsDblCon();
            double         constValue = dblConst->AsDblCon()->gtDconVal;

            if ((constValue == (double)(int)constValue) && (-2048 <= constValue) && (constValue <= 2047))
            {
                // Directly encode constant to instructions.
            }
            else
            {
                // Reserve int to load constant from memory (IF_LARGELDC)
                buildInternalIntRegisterDefForNode(tree);
                buildInternalRegisterUses();
            }
        }
            FALLTHROUGH;

        case GT_CNS_INT:
        {
            srcCount = 0;
            assert(dstCount == 1);
            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
        }
        break;

        case GT_BOX:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
            srcCount = 0;
            assert(dstCount == 0);
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

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child.
            srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtGetOp1() == nullptr)
            {
                assert(dstCount == 1);
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
            srcCount = 0;
            assert(dstCount == 0);
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
            buildInternalIntRegisterDefForNode(tree);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(dstCount == 0);
            break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            srcCount = 0;
            break;

        case GT_ADD:
        case GT_SUB:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtGetOp1()->TypeGet() == tree->gtGetOp2()->TypeGet());
            }

            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                buildInternalIntRegisterDefForNode(tree);
                setInternalRegsDelayFree = true;
            }
            FALLTHROUGH;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalRegisterUses();
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            BuildUse(tree->gtGetOp1());
            srcCount = 1;
            assert(dstCount == 0);
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        //case GT_MOD:
        //case GT_UMOD:
        //    NYI_IF(varTypeIsFloating(tree->TypeGet()), "FP Remainder in LOONGARCH64");
        //    assert(!"Shouldn't see an integer typed GT_MOD node in LOONGARCH64");
        //    srcCount = 0;
        //    break;

        case GT_MUL:
        case GT_MOD:
        case GT_UMOD:
        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
        {
            if (emitActualTypeSize(tree) == EA_4BYTE)
            {
                // We need two registers: tmpRegOp1 and tmpRegOp2
                buildInternalIntRegisterDefForNode(tree);
                buildInternalIntRegisterDefForNode(tree);
            }

            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalRegisterUses();
            assert(dstCount == 1);
            BuildDef(tree);
        }
        break;

        case GT_INTRINSIC:
        {
            noway_assert((tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Abs) ||
                         (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Ceiling) ||
                         (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Floor) ||
                         (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Round) ||
                         (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Sqrt));

            // Both operand and its result must be of the same floating point type.
            GenTree* op1 = tree->gtGetOp1();
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());

            BuildUse(op1);
            srcCount = 1;
            assert(dstCount == 1);
            BuildDef(tree);
        }
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

        case GT_NEG:
        case GT_NOT:
            BuildUse(tree->gtGetOp1());
            srcCount = 1;
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_JCMP:
            if (!varTypeIsFloating(tree->gtGetOp1()))
            {
                // We need two registers: tmpRegOp1 and tmpRegOp2
                buildInternalIntRegisterDefForNode(tree);
                buildInternalIntRegisterDefForNode(tree);
                buildInternalRegisterUses();
            }
            srcCount = BuildCmp(tree);
            break;

        case GT_CKFINITE:
            srcCount = 1;
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            BuildUse(tree->gtGetOp1());
            BuildDef(tree);
            buildInternalRegisterUses();
            break;

        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* cmpXchgNode = tree->AsCmpXchg();
            srcCount                    = cmpXchgNode->gtOpComparand->isContained() ? 2 : 3;
            assert(dstCount == 1);

            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // For LOONGARCH exclusives requires a single internal register
                buildInternalIntRegisterDefForNode(tree);
            }

            // For LOONGARCH exclusives the lifetime of the addr and data must be extended because
            // it may be used multiple during retries

            // For LOONGARCH atomic cas the lifetime of the addr and data must be extended to prevent
            // them being reused as the target register which must be destroyed early

            RefPosition* locationUse = BuildUse(tree->AsCmpXchg()->gtOpLocation);
            setDelayFree(locationUse);
            RefPosition* valueUse = BuildUse(tree->AsCmpXchg()->gtOpValue);
            setDelayFree(valueUse);
            if (!cmpXchgNode->gtOpComparand->isContained())
            {
                RefPosition* comparandUse = BuildUse(tree->AsCmpXchg()->gtOpComparand);

                // For LOONGARCH exclusives the lifetime of the comparand must be extended because
                // it may be used used multiple during retries
                if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
                {
                    setDelayFree(comparandUse);
                }
            }

            // Internals may not collide with target
            setInternalRegsDelayFree = true;
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        {
            assert(dstCount == (tree->TypeGet() == TYP_VOID) ? 0 : 1);
            srcCount = tree->gtGetOp2()->isContained() ? 1 : 2;

            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // GT_XCHG requires a single internal register; the others require two.
                buildInternalIntRegisterDefForNode(tree);
                if (tree->OperGet() != GT_XCHG)
                {
                    buildInternalIntRegisterDefForNode(tree);
                }
            }
            else if (tree->OperIs(GT_XAND))
            {
                // for ldclral we need an internal register.
                buildInternalIntRegisterDefForNode(tree);
            }

            assert(!tree->gtGetOp1()->isContained());
            RefPosition* op1Use = BuildUse(tree->gtGetOp1());
            RefPosition* op2Use = nullptr;
            if (!tree->gtGetOp2()->isContained())
            {
                op2Use = BuildUse(tree->gtGetOp2());
            }

            // For LOONGARCH exclusives the lifetime of the addr and data must be extended because
            // it may be used used multiple during retries
            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // Internals may not collide with target
                if (dstCount == 1)
                {
                    setDelayFree(op1Use);
                    if (op2Use != nullptr)
                    {
                        setDelayFree(op2Use);
                    }
                    setInternalRegsDelayFree = true;
                }
                buildInternalRegisterUses();
            }
            if (dstCount == 1)
            {
                BuildDef(tree);
            }
        }
        break;

#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
            srcCount = BuildPutArgSplit(tree->AsPutArgSplit());
            dstCount = tree->AsPutArgSplit()->gtNumRegs;
            break;
#endif // FEATURE _SPLIT_ARG

        case GT_PUTARG_STK:
            srcCount = BuildPutArgStk(tree->AsPutArgStk());
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
            BuildDef(tree);
        }
        break;

        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            srcCount = 0;
            break;

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
        {
            assert(dstCount == 1);

            // Need a variable number of temp regs (see genLclHeap() in codegenamd64.cpp):
            // Here '-' means don't care.
            //
            //  Size?                   Init Memory?    # temp regs
            //   0                          -               0
            //   const and <=6 ptr words    -               0
            //   const and <PageSize        No              0
            //   >6 ptr words               Yes             0
            //   Non-const                  Yes             0
            //   Non-const                  No              2
            //

            GenTree* size = tree->gtGetOp1();
            if (size->IsCnsIntOrI())
            {
                assert(size->isContained());
                srcCount = 0;

                size_t sizeVal = size->AsIntCon()->gtIconVal;

                if (sizeVal != 0)
                {
                    // Compute the amount of memory to properly STACK_ALIGN.
                    // Note: The Gentree node is not updated here as it is cheap to recompute stack aligned size.
                    // This should also help in debugging as we can examine the original size specified with
                    // localloc.
                    sizeVal         = AlignUp(sizeVal, STACK_ALIGN);
                    size_t stpCount = sizeVal / (REGSIZE_BYTES * 2);

                    // For small allocations up to 4 'stp' instructions (i.e. 16 to 64 bytes of localloc)
                    //
                    if (stpCount <= 4)
                    {
                        // Need no internal registers
                    }
                    else if (!compiler->info.compInitMem)
                    {
                        // No need to initialize allocated stack space.
                        if (sizeVal < compiler->eeGetPageSize())
                        {
                            // Need no internal registers
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
            else
            {
                srcCount = 1;
                if (!compiler->info.compInitMem)
                {
                    buildInternalIntRegisterDefForNode(tree);
                    buildInternalIntRegisterDefForNode(tree);
                }
            }

            if (!size->isContained())
            {
                BuildUse(size);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
        {
            GenTreeBoundsChk* node = tree->AsBoundsChk();
            // Consumes arrLen & index - has no result
            assert(dstCount == 0);
            srcCount = BuildOperandUses(node->GetIndex());
            srcCount += BuildOperandUses(node->GetArrayLength());
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_ARR_INDEX:
        {
            srcCount = 2;
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            setInternalRegsDelayFree = true;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            RefPosition* arrObjUse = BuildUse(tree->AsArrIndex()->ArrObj());
            setDelayFree(arrObjUse);
            BuildUse(tree->AsArrIndex()->IndexExpr());
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            srcCount = 2;
            if (!tree->AsArrOffs()->gtOffset->isContained())
            {
                BuildUse(tree->AsArrOffs()->gtOffset);
                srcCount++;
            }
            BuildUse(tree->AsArrOffs()->gtIndex);
            BuildUse(tree->AsArrOffs()->gtArrObj);
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            int      cns   = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount here.
            srcCount = 0;
            if (base != nullptr)
            {
                srcCount++;
                BuildUse(base);
            }
            if (index != nullptr)
            {
                srcCount++;
                BuildUse(index);
            }
            assert(dstCount == 1);

            // On LOONGARCH64 we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // LOONGARCH64 does not support both Index and offset so we need an internal register
                buildInternalIntRegisterDefForNode(tree);
            }
            else if (!((-2048 <= cns) && (cns <= 2047)))
            {
                // This offset can't be contained in the add instruction, so we need an internal register
                buildInternalIntRegisterDefForNode(tree);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_STOREIND:
        {
            assert(dstCount == 0);

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierStoreIndNode(tree))
            {
                srcCount = BuildGCWriteBarrier(tree);
                break;
            }

            srcCount = BuildIndir(tree->AsIndir());
            if (!tree->gtGetOp2()->isContained())
            {
                BuildUse(tree->gtGetOp2());
                srcCount++;
            }
        }
        break;

        case GT_NULLCHECK:
        case GT_IND:
            assert(dstCount == (tree->OperIs(GT_NULLCHECK) ? 0 : 1));
            srcCount = BuildIndir(tree->AsIndir());
            break;

        case GT_CATCH_ARG:
            srcCount = 0;
            assert(dstCount == 1);
            BuildDef(tree, RBM_EXCEPTION_OBJECT);
            break;

        case GT_CLS_VAR:
            srcCount = 0;
            // GT_CLS_VAR, by the time we reach the backend, must always
            // be a pure use.
            // It will produce a result of the type of the
            // node, and use an internal register for the address.

            assert(dstCount == 1);
            assert((tree->gtFlags & (GTF_VAR_DEF | GTF_VAR_USEASG)) == 0);
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        case GT_INDEX_ADDR:
            assert(dstCount == 1);
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

    } // end switch (tree->OperGet())

    if (tree->IsUnusedValue() && (dstCount != 0))
    {
        isLocalDefUse = true;
    }
    // We need to be sure that we've set srcCount and dstCount appropriately
    assert((dstCount < 2) || tree->IsMultiRegNode());
    assert(isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (dstCount != 0));
    assert(dstCount == tree->GetRegisterDstCount(compiler));
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
assert(!"unimplemented on LOONGARCH yet");
#if 0
    int srcCount = 0;
    // Only SIMDIntrinsicInit can be contained
    if (simdTree->isContained())
    {
        assert(simdTree->gtSIMDIntrinsicID == SIMDIntrinsicInit);
    }
    int dstCount = simdTree->IsValue() ? 1 : 0;
    assert(dstCount == 1);

    bool buildUses = true;

    GenTree* op1 = simdTree->gtGetOp1();
    GenTree* op2 = simdTree->gtGetOp2();

    switch (simdTree->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
        case SIMDIntrinsicCast:
        case SIMDIntrinsicSqrt:
        case SIMDIntrinsicAbs:
        case SIMDIntrinsicConvertToSingle:
        case SIMDIntrinsicConvertToInt32:
        case SIMDIntrinsicConvertToDouble:
        case SIMDIntrinsicConvertToInt64:
        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            // No special handling required.
            break;

        case SIMDIntrinsicGetItem:
        {
            op1 = simdTree->gtGetOp1();
            op2 = simdTree->gtGetOp2();

            // We have an object and an index, either of which may be contained.
            bool setOp2DelayFree = false;
            if (!op2->IsCnsIntOrI() && (!op1->isContained() || op1->OperIsLocal()))
            {
                // If the index is not a constant and the object is not contained or is a local
                // we will need a general purpose register to calculate the address
                // internal register must not clobber input index
                // TODO-Cleanup: An internal register will never clobber a source; this code actually
                // ensures that the index (op2) doesn't interfere with the target.
                buildInternalIntRegisterDefForNode(simdTree);
                setOp2DelayFree = true;
            }
            srcCount += BuildOperandUses(op1);
            if (!op2->isContained())
            {
                RefPosition* op2Use = BuildUse(op2);
                if (setOp2DelayFree)
                {
                    setDelayFree(op2Use);
                }
                srcCount++;
            }

            if (!op2->IsCnsIntOrI() && (!op1->isContained()))
            {
                // If vector is not already in memory (contained) and the index is not a constant,
                // we will use the SIMD temp location to store the vector.
                compiler->getSIMDInitTempVarNum();
            }
            buildUses = false;
        }
        break;

        case SIMDIntrinsicAdd:
        case SIMDIntrinsicSub:
        case SIMDIntrinsicMul:
        case SIMDIntrinsicDiv:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseAndNot:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicBitwiseXor:
        case SIMDIntrinsicMin:
        case SIMDIntrinsicMax:
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicGreaterThan:
        case SIMDIntrinsicLessThanOrEqual:
        case SIMDIntrinsicGreaterThanOrEqual:
            // No special handling required.
            break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
        case SIMDIntrinsicNarrow:
        {
            // Op1 will write to dst before Op2 is free
            BuildUse(op1);
            RefPosition* op2Use = BuildUse(op2);
            setDelayFree(op2Use);
            srcCount  = 2;
            buildUses = false;
            break;
        }

        case SIMDIntrinsicInitN:
        {
            var_types baseType = simdTree->gtSIMDBaseType;
            srcCount           = (short)(simdTree->gtSIMDSize / genTypeSize(baseType));
            if (varTypeIsFloating(simdTree->gtSIMDBaseType))
            {
                // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
                buildInternalFloatRegisterDefForNode(simdTree);
            }

            for (GenTree* operand : simdTree->Operands())
            {
                assert(operand->TypeIs(baseType));
                assert(!operand->isContained());

                BuildUse(operand);
            }

            buildUses = false;
            break;
        }

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            buildInternalFloatRegisterDefForNode(simdTree);
            break;

        case SIMDIntrinsicDotProduct:
            buildInternalFloatRegisterDefForNode(simdTree);
            break;

        case SIMDIntrinsicSelect:
            // TODO-LOONGARCH64-CQ Allow lowering to see SIMDIntrinsicSelect so we can generate BSL VC, VA, VB
            // bsl target register must be VC.  Reserve a temp in case we need to shuffle things.
            // This will require a different approach, as GenTreeSIMD has only two operands.
            assert(!"SIMDIntrinsicSelect not yet supported");
            buildInternalFloatRegisterDefForNode(simdTree);
            break;

        case SIMDIntrinsicInitArrayX:
        case SIMDIntrinsicInitFixed:
        case SIMDIntrinsicCopyToArray:
        case SIMDIntrinsicCopyToArrayX:
        case SIMDIntrinsicNone:
        case SIMDIntrinsicGetCount:
        case SIMDIntrinsicGetOne:
        case SIMDIntrinsicGetZero:
        case SIMDIntrinsicGetAllOnes:
        case SIMDIntrinsicGetX:
        case SIMDIntrinsicGetY:
        case SIMDIntrinsicGetZ:
        case SIMDIntrinsicGetW:
        case SIMDIntrinsicInstEquals:
        case SIMDIntrinsicHWAccel:
        case SIMDIntrinsicWiden:
        case SIMDIntrinsicInvalid:
            assert(!"These intrinsics should not be seen during register allocation");
            __fallthrough;

        default:
            noway_assert(!"Unimplemented SIMD node type.");
            unreached();
    }
    if (buildUses)
    {
        assert(!op1->OperIs(GT_LIST));
        assert(srcCount == 0);
        srcCount = BuildOperandUses(op1);
        if ((op2 != nullptr) && !op2->isContained())
        {
            srcCount += BuildOperandUses(op2);
        }
    }
    assert(internalCount <= MaxInternalCount);
    buildInternalRegisterUses();
    if (dstCount == 1)
    {
        BuildDef(simdTree);
    }
    else
    {
        assert(dstCount == 0);
    }
    return srcCount;
#endif
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsic.h"
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
assert(!"unimplemented on LOONGARCH yet");
#if 0
    NamedIntrinsic intrinsicID = intrinsicTree->gtHWIntrinsicId;
    int            numArgs     = HWIntrinsicInfo::lookupNumArgs(intrinsicTree);

    GenTree* op1      = intrinsicTree->gtGetOp1();
    GenTree* op2      = intrinsicTree->gtGetOp2();
    GenTree* op3      = nullptr;
    int      srcCount = 0;

    if ((op1 != nullptr) && op1->OperIsList())
    {
        // op2 must be null, and there must be at least two more arguments.
        assert(op2 == nullptr);
        noway_assert(op1->AsArgList()->Rest() != nullptr);
        noway_assert(op1->AsArgList()->Rest()->Rest() != nullptr);
        assert(op1->AsArgList()->Rest()->Rest()->Rest() == nullptr);
        op2 = op1->AsArgList()->Rest()->Current();
        op3 = op1->AsArgList()->Rest()->Rest()->Current();
        op1 = op1->AsArgList()->Current();
    }

    bool op2IsDelayFree = false;
    bool op3IsDelayFree = false;

    // Create internal temps, and handle any other special requirements.
    switch (HWIntrinsicInfo::lookup(intrinsicID).form)
    {
        case HWIntrinsicInfo::Sha1HashOp:
            assert((numArgs == 3) && (op2 != nullptr) && (op3 != nullptr));
            if (!op2->isContained())
            {
                assert(!op3->isContained());
                op2IsDelayFree           = true;
                op3IsDelayFree           = true;
                setInternalRegsDelayFree = true;
            }
            buildInternalFloatRegisterDefForNode(intrinsicTree);
            break;
        case HWIntrinsicInfo::SimdTernaryRMWOp:
            assert((numArgs == 3) && (op2 != nullptr) && (op3 != nullptr));
            if (!op2->isContained())
            {
                assert(!op3->isContained());
                op2IsDelayFree = true;
                op3IsDelayFree = true;
            }
            break;
        case HWIntrinsicInfo::Sha1RotateOp:
            buildInternalFloatRegisterDefForNode(intrinsicTree);
            break;

        case HWIntrinsicInfo::SimdExtractOp:
        case HWIntrinsicInfo::SimdInsertOp:
            if (!op2->isContained())
            {
                // We need a temp to create a switch table
                buildInternalIntRegisterDefForNode(intrinsicTree);
            }
            break;

        default:
            break;
    }

    // Next, build uses
    if (numArgs > 3)
    {
        srcCount = 0;
        assert(!op2IsDelayFree && !op3IsDelayFree);
        assert(op1->OperIs(GT_LIST));
        {
            for (GenTreeArgList* list = op1->AsArgList(); list != nullptr; list = list->Rest())
            {
                srcCount += BuildOperandUses(list->Current());
            }
        }
        assert(srcCount == numArgs);
    }
    else
    {
        if (op1 != nullptr)
        {
            srcCount += BuildOperandUses(op1);
            if (op2 != nullptr)
            {
                srcCount += (op2IsDelayFree) ? BuildDelayFreeUses(op2) : BuildOperandUses(op2);
                if (op3 != nullptr)
                {
                    srcCount += (op3IsDelayFree) ? BuildDelayFreeUses(op3) : BuildOperandUses(op3);
                }
            }
        }
    }
    buildInternalRegisterUses();

    // Now defs
    if (intrinsicTree->IsValue())
    {
        BuildDef(intrinsicTree);
    }

    return srcCount;
#endif
}
#endif

//------------------------------------------------------------------------
// BuildIndir: Specify register requirements for address expression
//                       of an indirection operation.
//
// Arguments:
//    indirTree - GT_IND, GT_STOREIND or block gentree node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    // struct typed indirs are expected only on rhs of a block copy,
    // but in this case they must be contained.
    assert(indirTree->TypeGet() != TYP_STRUCT);

    GenTree* addr  = indirTree->Addr();
    GenTree* index = nullptr;
    int      cns   = 0;

    if (addr->isContained())
    {
        if (addr->OperGet() == GT_LEA)
        {
            GenTreeAddrMode* lea = addr->AsAddrMode();
            index                = lea->Index();
            cns                  = lea->Offset();

            // On LOONGARCH we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // LOONGARCH does not support both Index and offset so we need an internal register
                buildInternalIntRegisterDefForNode(indirTree);
            }
            else if (!((-2048 <= cns) && (cns <= 2047)))
            {
                // This offset can't be contained in the ldr/str instruction, so we need an internal register
                buildInternalIntRegisterDefForNode(indirTree);
            }
        }
    }

#ifdef FEATURE_SIMD
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // If indirTree is of TYP_SIMD12, addr is not contained. See comment in LowerIndir().
        assert(!addr->isContained());

        // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
        // To assemble the vector properly we would need an additional int register
        buildInternalIntRegisterDefForNode(indirTree);
    }
#endif // FEATURE_SIMD

    int srcCount = BuildIndirUses(indirTree);
    buildInternalRegisterUses();

    if (!indirTree->OperIs(GT_STOREIND, GT_NULLCHECK))
    {
        BuildDef(indirTree);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildCall: Set the NodeInfo for a call.
//
// Arguments:
//    call - The call node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildCall(GenTreeCall* call)
{
    bool            hasMultiRegRetVal = false;
    const ReturnTypeDesc* retTypeDesc = nullptr;
    regMaskTP       dstCandidates     = RBM_NONE;

    int srcCount = 0;
    int dstCount = 0;
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

    GenTree*  ctrlExpr           = call->gtControlExpr;
    regMaskTP ctrlExprCandidates = RBM_NONE;
    if (call->gtCallType == CT_INDIRECT)
    {
        // either gtControlExpr != null or gtCallAddr != null.
        // Both cannot be non-null at the same time.
        assert(ctrlExpr == nullptr);
        assert(call->gtCallAddr != nullptr);
        ctrlExpr = call->gtCallAddr;
    }

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        // we should never see a gtControlExpr whose type is void.
        assert(ctrlExpr->TypeGet() != TYP_VOID);

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            // Fast tail call - make sure that call target is always computed in T9(LOONGARCH64)
            // so that epilog sequence can generate "jr t9" to achieve fast tail call.
            ctrlExprCandidates = RBM_FASTTAILCALL_TARGET;
        }
    }
    else if (call->IsR2ROrVirtualStubRelativeIndir())
    {
        buildInternalIntRegisterDefForNode(call);
    }

    RegisterType registerType = call->TypeGet();

// Set destination candidates for return value of the call.

    if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        dstCandidates = retTypeDesc->GetABIReturnRegs();
    }
    else if (varTypeUsesFloatArgReg(registerType))
    {
        dstCandidates = RBM_FLOATRET;
    }
    else if (registerType == TYP_LONG)
    {
        dstCandidates = RBM_LNGRET;
    }
    else
    {
        dstCandidates = RBM_INTRET;
    }

    // First, count reg args
    // Each register argument corresponds to one source.
    bool callHasFloatRegArgs = false;

    for (GenTreeCall::Use& arg : call->LateArgs())
    {
        GenTree* argNode = arg.GetNode();

#ifdef DEBUG
        // During Build, we only use the ArgTabEntry for validation,
        // as getting it is rather expensive.
        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        regNumber      argReg         = curArgTabEntry->GetRegNum();
        assert(curArgTabEntry != nullptr);
#endif

        if (argNode->gtOper == GT_PUTARG_STK)
        {
            // late arg that is not passed in a register
            assert(curArgTabEntry->GetRegNum() == REG_STK);
            // These should never be contained.
            assert(!argNode->isContained());
            continue;
        }

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(argNode->isContained());

            // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
#ifdef DEBUG
                assert(use.GetNode()->OperIs(GT_PUTARG_REG));
#endif
                BuildUse(use.GetNode(), genRegMask(use.GetNode()->GetRegNum()));
                srcCount++;
            }
        }
#if FEATURE_ARG_SPLIT
        else if (argNode->OperGet() == GT_PUTARG_SPLIT)
        {
            unsigned regCount = argNode->AsPutArgSplit()->gtNumRegs;
            assert(regCount == curArgTabEntry->numRegs);
            for (unsigned int i = 0; i < regCount; i++)
            {
                BuildUse(argNode, genRegMask(argNode->AsPutArgSplit()->GetRegNumByIdx(i)), i);
            }
            srcCount += regCount;
        }
#endif // FEATURE_ARG_SPLIT
        else
        {
            assert(argNode->OperIs(GT_PUTARG_REG));
            assert(argNode->GetRegNum() == argReg);
            HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
            {
                BuildUse(argNode, genRegMask(argNode->GetRegNum()));
                srcCount++;
            }
        }
    }

    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    for (GenTreeCall::Use& use : call->Args())
    {
        GenTree* arg = use.GetNode();

        // Skip arguments that have been moved to the Late Arg list
        if ((arg->gtFlags & GTF_LATE_ARG) == 0)
        {
#ifdef DEBUG
            fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, arg);
            assert(curArgTabEntry != nullptr);
#endif
#if FEATURE_ARG_SPLIT
            // PUTARG_SPLIT nodes must be in the gtCallLateArgs list, since they
            // define registers used by the call.
            assert(arg->OperGet() != GT_PUTARG_SPLIT);
#endif // FEATURE_ARG_SPLIT
            if (arg->gtOper == GT_PUTARG_STK)
            {
                assert(curArgTabEntry->GetRegNum() == REG_STK);
            }
            else
            {
                assert(!arg->IsValue() || arg->IsUnusedValue());
            }
        }
    }

    // If it is a fast tail call, it is already preferenced to use IP0.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by LOONGARCH64 ABI.
        ctrlExprCandidates = allRegs(TYP_INT) & ~(RBM_ARG_REGS);
    }

    if (ctrlExpr != nullptr)
    {
        BuildUse(ctrlExpr, ctrlExprCandidates);
        srcCount++;
    }

    buildInternalRegisterUses();

    // Now generate defs and kills.
    regMaskTP killMask = getKillSetForCall(call);
    BuildDefsWithKills(call, dstCount, dstCandidates, killMask);
    return srcCount;
}

//------------------------------------------------------------------------
// BuildPutArgStk: Set the NodeInfo for a GT_PUTARG_STK node
//
// Arguments:
//    argNode - a GT_PUTARG_STK node
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
//    Set the child node(s) to be contained when we have a multireg arg
//
int LinearScan::BuildPutArgStk(GenTreePutArgStk* argNode)
{
    assert(argNode->gtOper == GT_PUTARG_STK);

    GenTree* putArgChild = argNode->gtGetOp1();

    int srcCount = 0;

    // Do we have a TYP_STRUCT argument (or a GT_FIELD_LIST), if so it must be a multireg pass-by-value struct
    if (putArgChild->TypeIs(TYP_STRUCT) || putArgChild->OperIs(GT_FIELD_LIST))
    {
        // We will use store instructions that each write a register sized value

        if (putArgChild->OperIs(GT_FIELD_LIST))
        {
            assert(putArgChild->isContained());
            // We consume all of the items in the GT_FIELD_LIST
            for (GenTreeFieldList::Use& use : putArgChild->AsFieldList()->Uses())
            {
                BuildUse(use.GetNode());
                srcCount++;
            }
        }
        else
        {
            // We can use a ldp/stp sequence so we need two internal registers for LOONGARCH64; one for ARM.
            buildInternalIntRegisterDefForNode(argNode);

            if (putArgChild->OperGet() == GT_OBJ)
            {
                assert(putArgChild->isContained());
                GenTree* objChild = putArgChild->gtGetOp1();
                if (objChild->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We will generate all of the code for the GT_PUTARG_STK, the GT_OBJ and the GT_LCL_VAR_ADDR
                    // as one contained operation, and there are no source registers.
                    //
                    assert(objChild->isContained());
                }
                else
                {
                    // We will generate all of the code for the GT_PUTARG_STK and its child node
                    // as one contained operation
                    //
                    srcCount = BuildOperandUses(objChild);
                }
            }
            else
            {
                // No source registers.
                putArgChild->OperIs(GT_LCL_VAR);
            }
        }
    }
    else
    {
        assert(!putArgChild->isContained());
        srcCount = BuildOperandUses(putArgChild);
    }
    buildInternalRegisterUses();
    return srcCount;
}

#if FEATURE_ARG_SPLIT
//------------------------------------------------------------------------
// BuildPutArgSplit: Set the NodeInfo for a GT_PUTARG_SPLIT node
//
// Arguments:
//    argNode - a GT_PUTARG_SPLIT node
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
//    Set the child node(s) to be contained
//
int LinearScan::BuildPutArgSplit(GenTreePutArgSplit* argNode)
{
    int srcCount = 0;
    assert(argNode->gtOper == GT_PUTARG_SPLIT);

    GenTree* putArgChild = argNode->gtGetOp1();

    // Registers for split argument corresponds to source
    int dstCount = argNode->gtNumRegs;

    regNumber argReg  = argNode->GetRegNum();
    regMaskTP argMask = RBM_NONE;
    regMaskTP argMaskArr[MAX_REG_ARG] = {RBM_NONE};

    for (unsigned i = 0; i < dstCount; i++)
    {
        argMaskArr[i] = genRegMask(argNode->GetRegNumByIdx(i));
        argMask |= argMaskArr[i];
    }

    if (putArgChild->OperGet() == GT_FIELD_LIST)
    {
        // Generated code:
        // 1. Consume all of the items in the GT_FIELD_LIST (source)
        // 2. Store to target slot and move to target registers (destination) from source
        //
        unsigned sourceRegCount = 0;

        // To avoid redundant moves, have the argument operand computed in the
        // register in which the argument is passed to the call.

        for (GenTreeFieldList::Use& use : putArgChild->AsFieldList()->Uses())
        {
            GenTree* node = use.GetNode();
            assert(!node->isContained());
            // The only multi-reg nodes we should see are OperIsMultiRegOp()
            assert(!node->IsMultiRegNode());

            // Consume all the registers, setting the appropriate register mask for the ones that
            // go into registers.
            // (sourceRegCount < argNode->gtNumRegs)
            BuildUse(node, argMaskArr[sourceRegCount], 0);
            sourceRegCount++;
        }
        srcCount += sourceRegCount;
        assert(putArgChild->isContained());
    }
    else
    {
        assert(putArgChild->TypeGet() == TYP_STRUCT);
        assert(putArgChild->OperGet() == GT_OBJ);

        // We can use a ldr/str sequence so we need an internal register
        buildInternalIntRegisterDefForNode(argNode, allRegs(TYP_INT) & ~argMask);

        GenTree* objChild = putArgChild->gtGetOp1();
        if (objChild->OperGet() == GT_LCL_VAR_ADDR)
        {
            // We will generate all of the code for the GT_PUTARG_SPLIT, the GT_OBJ and the GT_LCL_VAR_ADDR
            // as one contained operation
            //
            assert(objChild->isContained());
        }
        else
        {
            srcCount = BuildIndirUses(putArgChild->AsIndir());
        }
        assert(putArgChild->isContained());
    }
    buildInternalRegisterUses();
    assert((argMask != RBM_NONE) && ((int)genCountBits(argMask) == dstCount));
    for (int i = 0; i < dstCount; i++)
    {
        BuildDef(argNode, argMaskArr[i], i);
    }
    return srcCount;
}
#endif // FEATURE_ARG_SPLIT

//------------------------------------------------------------------------
// BuildBlockStore: Build the RefPositions for a block store node.
//
// Arguments:
//    blkNode       - The block store node of interest
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
                break;

            case GenTreeBlk::BlkOpKindHelper:
                assert(!src->isContained());
                dstAddrRegMask = RBM_ARG_0;
                srcRegMask     = RBM_ARG_1;
                sizeRegMask    = RBM_ARG_2;
                break;

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
            // We don't need to materialize the struct size but we still need
            // a temporary register to perform the sequence of loads and stores.
            // We can't use the special Write Barrier registers, so exclude them from the mask
            regMaskTP internalIntCandidates =
                allRegs(TYP_INT) & ~(RBM_WRITE_BARRIER_DST_BYREF | RBM_WRITE_BARRIER_SRC_BYREF);
            buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);

            if (size >= 2 * REGSIZE_BYTES)
            {
                // We will use ldp/stp to reduce code size and improve performance
                // so we need to reserve an extra internal register
                buildInternalIntRegisterDefForNode(blkNode, internalIntCandidates);
            }

            // If we have a dest address we want it in RBM_WRITE_BARRIER_DST_BYREF.
            dstAddrRegMask = RBM_WRITE_BARRIER_DST_BYREF;

            // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
            // Otherwise, if it is a local, codegen will put its address in REG_WRITE_BARRIER_SRC_BYREF,
            // which is killed by a StoreObj (and thus needn't be reserved).
            if (srcAddrOrFill != nullptr)
            {
                assert(!srcAddrOrFill->isContained());
                srcRegMask = RBM_WRITE_BARRIER_SRC_BYREF;
            }
        }
        else
        {
            switch (blkNode->gtBlkOpKind)
            {
                case GenTreeBlk::BlkOpKindUnroll:
                    buildInternalIntRegisterDefForNode(blkNode);
                    break;

                case GenTreeBlk::BlkOpKindHelper:
                    dstAddrRegMask = RBM_ARG_0;
                    if (srcAddrOrFill != nullptr)
                    {
                        assert(!srcAddrOrFill->isContained());
                        srcRegMask = RBM_ARG_1;
                    }
                    sizeRegMask = RBM_ARG_2;
                    break;

                default:
                    unreached();
            }
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
        useCount += BuildAddrUses(dstAddr->AsAddrMode()->Base());
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
            useCount += BuildAddrUses(srcAddrOrFill->AsAddrMode()->Base());
        }
    }

    if (blkNode->OperIs(GT_STORE_DYN_BLK))
    {
        useCount++;
        BuildUse(blkNode->AsDynBlk()->gtDynamicSize, sizeRegMask);
    }

    buildInternalRegisterUses();
    regMaskTP killMask = getKillSetForBlockStore(blkNode);
    BuildDefsWithKills(blkNode, 0, RBM_NONE, killMask);
    return useCount;
}

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

    // Overflow checking cast from TYP_LONG to TYP_INT requires a temporary register to
    // store the min and max immediate values that cannot be encoded in the CMP instruction.
    if (cast->gtOverflow() && varTypeIsLong(srcType) && !cast->IsUnsigned() && (castType == TYP_INT))
    {
        buildInternalIntRegisterDefForNode(cast);
    }

    int srcCount = BuildOperandUses(src);
    buildInternalRegisterUses();
    BuildDef(cast);
    return srcCount;
}

#endif // TARGET_LOONGARCH64
