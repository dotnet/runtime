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

#ifdef _TARGET_ARM_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

int LinearScan::BuildLclHeap(GenTree* tree)
{
    int srcCount = 0;

    // Need a variable number of temp regs (see genLclHeap() in codegenarm.cpp):
    // Here '-' means don't care.
    //
    //  Size?                   Init Memory?    # temp regs
    //   0                          -               0
    //   const and <=4 str instr    -               0
    //   const and <PageSize        No              0
    //   >4 ptr words               Yes             1
    //   Non-const                  Yes             1
    //   Non-const                  No              1
    //
    // If the outgoing argument space is too large to encode in an "add/sub sp, icon"
    // instruction, we also need a temp (we can use the same temp register needed
    // for the other cases above, if there are multiple conditions that require a
    // temp register).

    GenTree* size = tree->gtGetOp1();
    int      internalIntCount;
    if (size->IsCnsIntOrI())
    {
        assert(size->isContained());
        srcCount = 0;

        size_t sizeVal = size->gtIntCon.gtIconVal;
        if (sizeVal == 0)
        {
            internalIntCount = 0;
        }
        else
        {
            sizeVal          = AlignUp(sizeVal, STACK_ALIGN);
            size_t pushCount = sizeVal / REGSIZE_BYTES;

            // For small allocations we use up to 4 push instructions
            if (pushCount <= 4)
            {
                internalIntCount = 0;
            }
            else if (!compiler->info.compInitMem)
            {
                // No need to initialize allocated stack space.
                if (sizeVal < compiler->eeGetPageSize())
                {
                    internalIntCount = 0;
                }
                else
                {
                    internalIntCount = 1;
                }
            }
            else
            {
                internalIntCount = 1;
            }
        }
    }
    else
    {
        // target (regCnt) + tmp
        srcCount         = 1;
        internalIntCount = 1;
        BuildUse(size);
    }

    // If we have an outgoing argument space, we are going to probe that SP change, and we require
    // a temporary register for doing the probe. Note also that if the outgoing argument space is
    // large enough that it can't be directly encoded in SUB/ADD instructions, we also need a temp
    // register to load the large sized constant into a register.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        internalIntCount = 1;
    }

    // If we are needed in temporary registers we should be sure that
    // it's different from target (regCnt)
    if (internalIntCount > 0)
    {
        setInternalRegsDelayFree = true;
        for (int i = 0; i < internalIntCount; i++)
        {
            buildInternalIntRegisterDefForNode(tree);
        }
    }
    buildInternalRegisterUses();
    BuildDef(tree);
    return srcCount;
}

//------------------------------------------------------------------------
// BuildShiftLongCarry: Set the node info for GT_LSH_HI or GT_RSH_LO.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
// Note: these operands have uses that interfere with the def and need the special handling.
//
int LinearScan::BuildShiftLongCarry(GenTree* tree)
{
    assert(tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO);

    int      srcCount = 2;
    GenTree* source   = tree->gtOp.gtOp1;
    assert((source->OperGet() == GT_LONG) && source->isContained());

    GenTree* sourceLo = source->gtGetOp1();
    GenTree* sourceHi = source->gtGetOp2();
    GenTree* shiftBy  = tree->gtGetOp2();
    assert(!sourceLo->isContained() && !sourceHi->isContained());
    RefPosition* sourceLoUse = BuildUse(sourceLo);
    RefPosition* sourceHiUse = BuildUse(sourceHi);

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
        if (!shiftBy->isContained())
        {
            BuildUse(shiftBy);
            srcCount++;
        }
        BuildDef(tree);
    }
    else
    {
        if (!shiftBy->isContained())
        {
            BuildUse(shiftBy);
            srcCount++;
        }
    }
    return srcCount;
}

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
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        {
            // We handle tracked variables differently from non-tracked ones.  If it is tracked,
            // we will simply add a use of the tracked variable at its parent/consumer.
            // Otherwise, for a use we need to actually add the appropriate references for loading
            // or storing the variable.
            //
            // A tracked variable won't actually get used until the appropriate ancestor tree node
            // is processed, unless this is marked "isLocalDefUse" because it is a stack-based argument
            // to a call or an orphaned dead node.
            //
            LclVarDsc* const varDsc = &compiler->lvaTable[tree->AsLclVarCommon()->gtLclNum];
            if (isCandidateVar(varDsc))
            {
                INDEBUG(dumpNodeInfo(tree, dstCandidates, 0, 1));
                return 0;
            }
            srcCount = 0;
            BuildDef(tree);
        }
        break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child
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

        case GT_INTRINSIC:
        {
            // TODO-ARM: Implement other type of intrinsics (round, sqrt and etc.)
            // Both operand and its result must be of the same floating point type.
            GenTree* op1 = tree->gtGetOp1();
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());
            BuildUse(op1);
            srcCount = 1;

            switch (tree->gtIntrinsic.gtIntrinsicId)
            {
                case CORINFO_INTRINSIC_Abs:
                case CORINFO_INTRINSIC_Sqrt:
                    assert(dstCount == 1);
                    BuildDef(tree);
                    break;
                default:
                    unreached();
                    break;
            }
        }
        break;

        case GT_CAST:
            assert(dstCount == 1);
            srcCount = BuildCast(tree->AsCast());
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
            assert(dstCount == 0);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(srcCount == 2);
            break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            srcCount = 0;
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
                assert(tree->gtGetOp1()->TypeGet() == tree->gtGetOp2()->TypeGet());

                assert(dstCount == 1);
                srcCount = BuildBinaryUses(tree->AsOp());
                assert(srcCount == 2);
                BuildDef(tree);
                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            assert(dstCount == 1);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(srcCount == (tree->gtGetOp2()->isContained() ? 1 : 2));
            BuildDef(tree);
            break;

        case GT_LSH_HI:
        case GT_RSH_LO:
            assert(dstCount == 1);
            srcCount = BuildShiftLongCarry(tree);
            assert(srcCount == (tree->gtOp.gtOp2->isContained() ? 2 : 3));
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            srcCount = 1;
            assert(dstCount == 0);
            BuildUse(tree->gtGetOp1());
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        case GT_MUL:
            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                setInternalRegsDelayFree = true;
                buildInternalIntRegisterDefForNode(tree);
            }
            __fallthrough;

        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
        {
            assert(dstCount == 1);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(srcCount == 2);
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_MUL_LONG:
            dstCount = 2;
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(srcCount == 2);
            BuildDefs(tree, 2);
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
        case GT_PROF_HOOK:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_START_PREEMPTGC:
            // This kills GC refs in callee save regs
            srcCount = 0;
            assert(dstCount == 0);
            BuildDefsWithKills(tree, 0, RBM_NONE, RBM_NONE);
            break;

        case GT_LONG:
            assert(tree->IsUnusedValue()); // Contained nodes are already processed, only unused GT_LONG can reach here.
                                           // An unused GT_LONG doesn't produce any registers.
            tree->gtType = TYP_VOID;
            tree->ClearUnusedValue();
            isLocalDefUse = false;

            // An unused GT_LONG node needs to consume its sources, but need not produce a register.
            srcCount = 2;
            dstCount = 0;
            BuildUse(tree->gtGetOp1());
            BuildUse(tree->gtGetOp2());
            break;

        case GT_CNS_DBL:
            if (tree->TypeGet() == TYP_FLOAT)
            {
                // An int register for float constant
                buildInternalIntRegisterDefForNode(tree);
            }
            else
            {
                // TYP_DOUBLE
                assert(tree->TypeGet() == TYP_DOUBLE);

                // Two int registers for double constant
                buildInternalIntRegisterDefForNode(tree);
                buildInternalIntRegisterDefForNode(tree);
            }
            __fallthrough;

        case GT_CNS_INT:
        {
            srcCount = 0;
            assert(dstCount == 1);
            buildInternalRegisterUses();
            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
        }
        break;

        case GT_RETURN:
            srcCount = BuildReturn(tree);
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

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            // Consumes arrLen & index - has no result
            srcCount = 2;
            assert(dstCount == 0);
            BuildUse(tree->AsBoundsChk()->gtIndex);
            BuildUse(tree->AsBoundsChk()->gtArrLen);
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
            assert(dstCount == 1);

            if (tree->gtArrOffs.gtOffset->isContained())
            {
                srcCount = 2;
            }
            else
            {
                // Here we simply need an internal register, which must be different
                // from any of the operand's registers, but may be the same as targetReg.
                buildInternalIntRegisterDefForNode(tree);
                srcCount = 3;
                BuildUse(tree->AsArrOffs()->gtOffset);
            }
            BuildUse(tree->AsArrOffs()->gtIndex);
            BuildUse(tree->AsArrOffs()->gtArrObj);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea    = tree->AsAddrMode();
            int              offset = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount and dstCount here.
            srcCount = 0;
            assert(dstCount == 1);
            if (lea->HasBase())
            {
                srcCount++;
                BuildUse(tree->AsAddrMode()->Base());
            }
            if (lea->HasIndex())
            {
                srcCount++;
                BuildUse(tree->AsAddrMode()->Index());
            }

            // An internal register may be needed too; the logic here should be in sync with the
            // genLeaInstruction()'s requirements for a such register.
            if (lea->HasBase() && lea->HasIndex())
            {
                if (offset != 0)
                {
                    // We need a register when we have all three: base reg, index reg and a non-zero offset.
                    buildInternalIntRegisterDefForNode(tree);
                }
            }
            else if (lea->HasBase())
            {
                if (!emitter::emitIns_valid_imm_for_add(offset, INS_FLAGS_DONT_CARE))
                {
                    // We need a register when we have an offset that is too large to encode in the add instruction.
                    buildInternalIntRegisterDefForNode(tree);
                }
            }
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_NEG:
            srcCount = 1;
            assert(dstCount == 1);
            BuildUse(tree->gtGetOp1());
            BuildDef(tree);
            break;

        case GT_NOT:
            srcCount = 1;
            assert(dstCount == 1);
            BuildUse(tree->gtGetOp1());
            BuildDef(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_CMP:
            srcCount = BuildCmp(tree);
            break;

        case GT_CKFINITE:
            srcCount = 1;
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            BuildUse(tree->gtGetOp1());
            buildInternalRegisterUses();
            BuildDef(tree);
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

        case GT_STOREIND:
        {
            assert(dstCount == 0);
            GenTree* src = tree->gtGetOp2();

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierStoreIndNode(tree))
            {
                srcCount = BuildGCWriteBarrier(tree);
                break;
            }

            srcCount = BuildIndir(tree->AsIndir());
            // No contained source on ARM.
            assert(!src->isContained());
            srcCount++;
            BuildUse(src);
        }
        break;

        case GT_NULLCHECK:
            // It requires a internal register on ARM, as it is implemented as a load
            assert(dstCount == 0);
            assert(!tree->gtGetOp1()->isContained());
            srcCount = 1;
            buildInternalIntRegisterDefForNode(tree);
            BuildUse(tree->gtGetOp1());
            buildInternalRegisterUses();
            break;

        case GT_IND:
            assert(dstCount == 1);
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

        case GT_COPY:
            srcCount = 1;
#ifdef _TARGET_ARM_
            // This case currently only occurs for double types that are passed as TYP_LONG;
            // actual long types would have been decomposed by now.
            if (tree->TypeGet() == TYP_LONG)
            {
                dstCount = 2;
            }
            else
#endif
            {
                assert(dstCount == 1);
            }
            BuildUse(tree->gtGetOp1());
            BuildDefs(tree, dstCount);
            break;

        case GT_PUTARG_SPLIT:
            srcCount = BuildPutArgSplit(tree->AsPutArgSplit());
            dstCount = tree->AsPutArgSplit()->gtNumRegs;
            break;

        case GT_PUTARG_STK:
            srcCount = BuildPutArgStk(tree->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            srcCount = BuildPutArgReg(tree->AsUnOp());
            dstCount = tree->AsMultiRegOp()->GetRegCount();
            break;

        case GT_BITCAST:
        {
            srcCount = 1;
            assert(dstCount == 1);
            regNumber argReg  = tree->gtRegNum;
            regMaskTP argMask = genRegMask(argReg);

            // If type of node is `long` then it is actually `double`.
            // The actual `long` types must have been transformed as a field list with two fields.
            if (tree->TypeGet() == TYP_LONG)
            {
                dstCount++;
                assert(genRegArgNext(argReg) == REG_NEXT(argReg));
                argMask |= genRegMask(REG_NEXT(argReg));
                dstCount = 2;
            }

            BuildUse(tree->gtGetOp1());
            BuildDefs(tree, dstCount, argMask);
        }
        break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
        case GT_PHYSREG:
        case GT_CLS_VAR_ADDR:
        case GT_IL_OFFSET:
        case GT_LABEL:
        case GT_PINVOKE_PROLOG:
        case GT_JCC:
        case GT_SETCC:
        case GT_MEMORYBARRIER:
        case GT_OBJ:
            srcCount = BuildSimple(tree);
            break;

        case GT_INDEX_ADDR:
            dstCount = 1;
            buildInternalIntRegisterDefForNode(tree);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(srcCount == 2);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        default:
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::OpName(tree->OperGet()));
            NYIRAW(message);
#endif
            unreached();
    } // end switch (tree->OperGet())

    // We need to be sure that we've set srcCount and dstCount appropriately
    assert((dstCount < 2) || tree->IsMultiRegNode());
    assert(isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (dstCount != 0));
    assert(dstCount == tree->GetRegisterDstCount());
    INDEBUG(dumpNodeInfo(tree, dstCandidates, srcCount, dstCount));
    return srcCount;
}

#endif // _TARGET_ARM_
