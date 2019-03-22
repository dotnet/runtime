// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          GCInfo                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "gcinfo.h"
#include "emit.h"
#include "jitgcinfo.h"

#ifdef _TARGET_AMD64_
#include "gcinfoencoder.h" //this includes a LOT of other files too
#endif

/*****************************************************************************/
/*****************************************************************************/

/*****************************************************************************/

extern int JITGcBarrierCall;

/*****************************************************************************/

#if MEASURE_PTRTAB_SIZE
/* static */ size_t GCInfo::s_gcRegPtrDscSize   = 0;
/* static */ size_t GCInfo::s_gcTotalPtrTabSize = 0;
#endif // MEASURE_PTRTAB_SIZE

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          GCInfo                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

GCInfo::GCInfo(Compiler* theCompiler) : compiler(theCompiler)
{
    regSet         = nullptr;
    gcVarPtrList   = nullptr;
    gcVarPtrLast   = nullptr;
    gcRegPtrList   = nullptr;
    gcRegPtrLast   = nullptr;
    gcPtrArgCnt    = 0;
    gcCallDescList = nullptr;
    gcCallDescLast = nullptr;
#ifdef JIT32_GCENCODER
    gcEpilogTable = nullptr;
#else  // !JIT32_GCENCODER
    m_regSlotMap   = nullptr;
    m_stackSlotMap = nullptr;
#endif // JIT32_GCENCODER
}

/*****************************************************************************/
/*****************************************************************************
 *  Reset tracking info at the start of a basic block.
 */

void GCInfo::gcResetForBB()
{
    gcRegGCrefSetCur = RBM_NONE;
    gcRegByrefSetCur = RBM_NONE;
    VarSetOps::AssignNoCopy(compiler, gcVarPtrSetCur, VarSetOps::MakeEmpty(compiler));
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Print the changes in the gcRegGCrefSetCur sets.
 */

void GCInfo::gcDspGCrefSetChanges(regMaskTP gcRegGCrefSetNew DEBUGARG(bool forceOutput))
{
    if (compiler->verbose)
    {
        if (forceOutput || (gcRegGCrefSetCur != gcRegGCrefSetNew))
        {
            printf("\t\t\t\t\t\t\tGC regs: ");
            if (gcRegGCrefSetCur == gcRegGCrefSetNew)
            {
                printf("(unchanged) ");
            }
            else
            {
                printRegMaskInt(gcRegGCrefSetCur);
                compiler->getEmitter()->emitDispRegSet(gcRegGCrefSetCur);
                printf(" => ");
            }
            printRegMaskInt(gcRegGCrefSetNew);
            compiler->getEmitter()->emitDispRegSet(gcRegGCrefSetNew);
            printf("\n");
        }
    }
}

/*****************************************************************************
 *
 *  Print the changes in the gcRegByrefSetCur sets.
 */

void GCInfo::gcDspByrefSetChanges(regMaskTP gcRegByrefSetNew DEBUGARG(bool forceOutput))
{
    if (compiler->verbose)
    {
        if (forceOutput || (gcRegByrefSetCur != gcRegByrefSetNew))
        {
            printf("\t\t\t\t\t\t\tByref regs: ");
            if (gcRegByrefSetCur == gcRegByrefSetNew)
            {
                printf("(unchanged) ");
            }
            else
            {
                printRegMaskInt(gcRegByrefSetCur);
                compiler->getEmitter()->emitDispRegSet(gcRegByrefSetCur);
                printf(" => ");
            }
            printRegMaskInt(gcRegByrefSetNew);
            compiler->getEmitter()->emitDispRegSet(gcRegByrefSetNew);
            printf("\n");
        }
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Mark the set of registers given by the specified mask as holding
 *  GCref pointer values.
 */

void GCInfo::gcMarkRegSetGCref(regMaskTP regMask DEBUGARG(bool forceOutput))
{
#ifdef DEBUG
    if (compiler->compRegSetCheckLevel == 0)
    {
        // This set of registers are going to hold REFs.
        // Make sure they were not holding BYREFs.
        assert((gcRegByrefSetCur & regMask) == 0);
    }
#endif

    regMaskTP gcRegByrefSetNew = gcRegByrefSetCur & ~regMask; // Clear it if set in Byref mask
    regMaskTP gcRegGCrefSetNew = gcRegGCrefSetCur | regMask;  // Set it in GCref mask

    INDEBUG(gcDspGCrefSetChanges(gcRegGCrefSetNew, forceOutput));
    INDEBUG(gcDspByrefSetChanges(gcRegByrefSetNew));

    gcRegByrefSetCur = gcRegByrefSetNew;
    gcRegGCrefSetCur = gcRegGCrefSetNew;
}

/*****************************************************************************
 *
 *  Mark the set of registers given by the specified mask as holding
 *  Byref pointer values.
 */

void GCInfo::gcMarkRegSetByref(regMaskTP regMask DEBUGARG(bool forceOutput))
{
    regMaskTP gcRegByrefSetNew = gcRegByrefSetCur | regMask;  // Set it in Byref mask
    regMaskTP gcRegGCrefSetNew = gcRegGCrefSetCur & ~regMask; // Clear it if set in GCref mask

    INDEBUG(gcDspGCrefSetChanges(gcRegGCrefSetNew));
    INDEBUG(gcDspByrefSetChanges(gcRegByrefSetNew, forceOutput));

    gcRegByrefSetCur = gcRegByrefSetNew;
    gcRegGCrefSetCur = gcRegGCrefSetNew;
}

/*****************************************************************************
 *
 *  Mark the set of registers given by the specified mask as holding
 *  non-pointer values.
 */

void GCInfo::gcMarkRegSetNpt(regMaskTP regMask DEBUGARG(bool forceOutput))
{
    /* NOTE: don't unmark any live register variables */

    regMaskTP gcRegByrefSetNew = gcRegByrefSetCur & ~(regMask & ~regSet->rsMaskVars);
    regMaskTP gcRegGCrefSetNew = gcRegGCrefSetCur & ~(regMask & ~regSet->rsMaskVars);

    INDEBUG(gcDspGCrefSetChanges(gcRegGCrefSetNew, forceOutput));
    INDEBUG(gcDspByrefSetChanges(gcRegByrefSetNew, forceOutput));

    gcRegByrefSetCur = gcRegByrefSetNew;
    gcRegGCrefSetCur = gcRegGCrefSetNew;
}

/*****************************************************************************
 *
 *  Mark the specified register as now holding a value of the given type.
 */

void GCInfo::gcMarkRegPtrVal(regNumber reg, var_types type)
{
    regMaskTP regMask = genRegMask(reg);

    switch (type)
    {
        case TYP_REF:
            gcMarkRegSetGCref(regMask);
            break;
        case TYP_BYREF:
            gcMarkRegSetByref(regMask);
            break;
        default:
            gcMarkRegSetNpt(regMask);
            break;
    }
}

/*****************************************************************************/

GCInfo::WriteBarrierForm GCInfo::gcIsWriteBarrierCandidate(GenTree* tgt, GenTree* assignVal)
{
    /* Are we storing a GC ptr? */

    if (!varTypeIsGC(tgt->TypeGet()))
    {
        return WBF_NoBarrier;
    }

    /* Ignore any assignments of NULL */

    // 'assignVal' can be the constant Null or something else (LclVar, etc..)
    //  that is known to be null via Value Numbering.
    if (assignVal->GetVN(VNK_Liberal) == ValueNumStore::VNForNull())
    {
        return WBF_NoBarrier;
    }

    if (assignVal->gtOper == GT_CNS_INT && assignVal->gtIntCon.gtIconVal == 0)
    {
        return WBF_NoBarrier;
    }

    /* Where are we storing into? */

    tgt = tgt->gtEffectiveVal();

    switch (tgt->gtOper)
    {

        case GT_STOREIND:
        case GT_IND: /* Could be the managed heap */
            if (tgt->TypeGet() == TYP_BYREF)
            {
                // Byref values cannot be in managed heap.
                // This case occurs for Span<T>.
                return WBF_NoBarrier;
            }
            if (tgt->gtFlags & GTF_IND_TGT_NOT_HEAP)
            {
                // This indirection is not from to the heap.
                // This case occurs for stack-allocated objects.
                return WBF_NoBarrier;
            }
            return gcWriteBarrierFormFromTargetAddress(tgt->gtOp.gtOp1);

        case GT_LEA:
            return gcWriteBarrierFormFromTargetAddress(tgt->AsAddrMode()->Base());

        case GT_ARR_ELEM: /* Definitely in the managed heap */
        case GT_CLS_VAR:
            return WBF_BarrierUnchecked;

        case GT_LCL_VAR: /* Definitely not in the managed heap  */
        case GT_LCL_FLD:
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            return WBF_NoBarrier;

        default:
            break;
    }

    assert(!"Missing case in gcIsWriteBarrierCandidate");

    return WBF_NoBarrier;
}

bool GCInfo::gcIsWriteBarrierStoreIndNode(GenTree* op)
{
    assert(op->OperIs(GT_STOREIND));

    return gcIsWriteBarrierCandidate(op, op->gtOp.gtOp2) != WBF_NoBarrier;
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Initialize the non-register pointer variable tracking logic.
 */

void GCInfo::gcVarPtrSetInit()
{
    VarSetOps::AssignNoCopy(compiler, gcVarPtrSetCur, VarSetOps::MakeEmpty(compiler));

    /* Initialize the list of lifetime entries */
    gcVarPtrList = gcVarPtrLast = nullptr;
}

/*****************************************************************************
 *
 *  Allocate a new pointer register set / pointer argument entry and append
 *  it to the list.
 */

GCInfo::regPtrDsc* GCInfo::gcRegPtrAllocDsc()
{
    regPtrDsc* regPtrNext;

    assert(compiler->genFullPtrRegMap);

    /* Allocate a new entry and initialize it */

    regPtrNext = new (compiler, CMK_GC) regPtrDsc;

    regPtrNext->rpdIsThis = FALSE;

    regPtrNext->rpdOffs = 0;
    regPtrNext->rpdNext = nullptr;

    // Append the entry to the end of the list.
    if (gcRegPtrLast == nullptr)
    {
        assert(gcRegPtrList == nullptr);
        gcRegPtrList = gcRegPtrLast = regPtrNext;
    }
    else
    {
        assert(gcRegPtrList != nullptr);
        gcRegPtrLast->rpdNext = regPtrNext;
        gcRegPtrLast          = regPtrNext;
    }

#if MEASURE_PTRTAB_SIZE
    s_gcRegPtrDscSize += sizeof(*regPtrNext);
#endif

    return regPtrNext;
}

/*****************************************************************************
 *
 *  Compute the various counts that get stored in the info block header.
 */

void GCInfo::gcCountForHeader(UNALIGNED unsigned int* untrackedCount, UNALIGNED unsigned int* varPtrTableSize)
{
    unsigned   varNum;
    LclVarDsc* varDsc;
    varPtrDsc* varTmp;

    bool         thisKeptAliveIsInUntracked = false; // did we track "this" in a synchronized method?
    unsigned int count                      = 0;

    /* Count the untracked locals and non-enregistered args */

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varTypeIsGC(varDsc->TypeGet()))
        {
            if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
            {
                // Field local of a PROMOTION_TYPE_DEPENDENT struct must have been
                // reported through its parent local
                continue;
            }

            /* Do we have an argument or local variable? */
            if (!varDsc->lvIsParam)
            {
                if (varDsc->lvTracked || !varDsc->lvOnFrame)
                {
                    continue;
                }
            }
            else
            {
                /* Stack-passed arguments which are not enregistered
                 * are always reported in this "untracked stack
                 * pointers" section of the GC info even if lvTracked==true
                 */

                /* Has this argument been fully enregistered? */
                CLANG_FORMAT_COMMENT_ANCHOR;

                if (!varDsc->lvOnFrame)
                {
                    /* if a CEE_JMP has been used, then we need to report all the arguments
                       even if they are enregistered, since we will be using this value
                       in JMP call.  Note that this is subtle as we require that
                       argument offsets are always fixed up properly even if lvRegister
                       is set */
                    if (!compiler->compJmpOpUsed)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!varDsc->lvOnFrame)
                    {
                        /* If this non-enregistered pointer arg is never
                         * used, we don't need to report it
                         */
                        assert(varDsc->lvRefCnt() == 0);
                        continue;
                    }
                    else if (varDsc->lvIsRegArg && varDsc->lvTracked)
                    {
                        /* If this register-passed arg is tracked, then
                         * it has been allocated space near the other
                         * pointer variables and we have accurate life-
                         * time info. It will be reported with
                         * gcVarPtrList in the "tracked-pointer" section
                         */

                        continue;
                    }
                }
            }

#if !defined(JIT32_GCENCODER) || !defined(WIN64EXCEPTIONS)
            // For x86/WIN64EXCEPTIONS, "this" must always be in untracked variables
            // so we cannot have "this" in variable lifetimes
            if (compiler->lvaIsOriginalThisArg(varNum) && compiler->lvaKeepAliveAndReportThis())
            {
                // Encoding of untracked variables does not support reporting
                // "this". So report it as a tracked variable with a liveness
                // extending over the entire method.

                thisKeptAliveIsInUntracked = true;
                continue;
            }
#endif

#ifdef DEBUG
            if (compiler->verbose)
            {
                int offs = varDsc->lvStkOffs;

                printf("GCINFO: untrckd %s lcl at [%s", varTypeGCstring(varDsc->TypeGet()),
                       compiler->genEmitter->emitGetFrameReg());

                if (offs < 0)
                {
                    printf("-%02XH", -offs);
                }
                else if (offs > 0)
                {
                    printf("+%02XH", +offs);
                }

                printf("]\n");
            }
#endif

            count++;
        }
        else if (varDsc->lvType == TYP_STRUCT && varDsc->lvOnFrame && (varDsc->lvExactSize >= TARGET_POINTER_SIZE))
        {
            unsigned slots  = compiler->lvaLclSize(varNum) / TARGET_POINTER_SIZE;
            BYTE*    gcPtrs = compiler->lvaGetGcLayout(varNum);

            // walk each member of the array
            for (unsigned i = 0; i < slots; i++)
            {
                if (gcPtrs[i] != TYPE_GC_NONE)
                { // count only gc slots
                    count++;
                }
            }
        }
    }

    /* Also count spill temps that hold pointers */

    assert(regSet->tmpAllFree());
    for (TempDsc* tempThis = regSet->tmpListBeg(); tempThis != nullptr; tempThis = regSet->tmpListNxt(tempThis))
    {
        if (varTypeIsGC(tempThis->tdTempType()) == false)
        {
            continue;
        }

#ifdef DEBUG
        if (compiler->verbose)
        {
            int offs = tempThis->tdTempOffs();

            printf("GCINFO: untrck %s Temp at [%s", varTypeGCstring(varDsc->TypeGet()),
                   compiler->genEmitter->emitGetFrameReg());

            if (offs < 0)
            {
                printf("-%02XH", -offs);
            }
            else if (offs > 0)
            {
                printf("+%02XH", +offs);
            }

            printf("]\n");
        }
#endif

        count++;
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("GCINFO: untrckVars = %u\n", count);
    }
#endif

    *untrackedCount = count;

    /* Count the number of entries in the table of non-register pointer
       variable lifetimes. */

    count = 0;

    if (thisKeptAliveIsInUntracked)
    {
        count++;
    }

    if (gcVarPtrList)
    {
        /* We'll use a delta encoding for the lifetime offsets */

        for (varTmp = gcVarPtrList; varTmp; varTmp = varTmp->vpdNext)
        {
            /* Special case: skip any 0-length lifetimes */

            if (varTmp->vpdBegOfs == varTmp->vpdEndOfs)
            {
                continue;
            }

            count++;
        }
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("GCINFO: trackdLcls = %u\n", count);
    }
#endif

    *varPtrTableSize = count;
}

#ifdef JIT32_GCENCODER
/*****************************************************************************
 *
 *  Shutdown the 'pointer value' register tracking logic and save the necessary
 *  info (which will be used at runtime to locate all pointers) at the specified
 *  address. The number of bytes written to 'destPtr' must be identical to that
 *  returned from gcPtrTableSize().
 */

BYTE* GCInfo::gcPtrTableSave(BYTE* destPtr, const InfoHdr& header, unsigned codeSize, size_t* pArgTabOffset)
{
    /* Write the tables to the info block */

    return destPtr + gcMakeRegPtrTable(destPtr, -1, header, codeSize, pArgTabOffset);
}
#endif

/*****************************************************************************
 *
 *  Initialize the 'pointer value' register/argument tracking logic.
 */

void GCInfo::gcRegPtrSetInit()
{
    gcRegGCrefSetCur = gcRegByrefSetCur = 0;

    if (compiler->genFullPtrRegMap)
    {
        gcRegPtrList = gcRegPtrLast = nullptr;
    }
    else
    {
        /* Initialize the 'call descriptor' list */
        gcCallDescList = gcCallDescLast = nullptr;
    }
}

#ifdef JIT32_GCENCODER

/*****************************************************************************
 *
 *  Helper passed to genEmitter.emitGenEpilogLst() to generate
 *  the table of epilogs.
 */

/* static */ size_t GCInfo::gcRecordEpilog(void* pCallBackData, unsigned offset)
{
    GCInfo* gcInfo = (GCInfo*)pCallBackData;

    assert(gcInfo);

    size_t result = encodeUDelta(gcInfo->gcEpilogTable, offset, gcInfo->gcEpilogPrevOffset);

    if (gcInfo->gcEpilogTable)
        gcInfo->gcEpilogTable += result;

    gcInfo->gcEpilogPrevOffset = offset;

    return result;
}

#endif // JIT32_GCENCODER

GCInfo::WriteBarrierForm GCInfo::gcWriteBarrierFormFromTargetAddress(GenTree* tgtAddr)
{
    // If we store through an int to a GC_REF field, we'll assume that needs to use a checked barriers.
    if (tgtAddr->TypeGet() == TYP_I_IMPL)
    {
        return GCInfo::WBF_BarrierChecked; // Why isn't this GCInfo::WBF_BarrierUnknown?
    }

    // Otherwise...
    assert(tgtAddr->TypeGet() == TYP_BYREF);
    bool simplifiedExpr = true;
    while (simplifiedExpr)
    {
        simplifiedExpr = false;

        tgtAddr = tgtAddr->gtSkipReloadOrCopy();

        while (tgtAddr->OperGet() == GT_ADDR && tgtAddr->gtOp.gtOp1->OperGet() == GT_IND)
        {
            tgtAddr        = tgtAddr->gtOp.gtOp1->gtOp.gtOp1;
            simplifiedExpr = true;
            assert(tgtAddr->TypeGet() == TYP_BYREF);
        }
        // For additions, one of the operands is a byref or a ref (and the other is not).  Follow this down to its
        // source.
        while (tgtAddr->OperGet() == GT_ADD || tgtAddr->OperGet() == GT_LEA)
        {
            if (tgtAddr->OperGet() == GT_ADD)
            {
                if (tgtAddr->gtOp.gtOp1->TypeGet() == TYP_BYREF || tgtAddr->gtOp.gtOp1->TypeGet() == TYP_REF)
                {
                    assert(!(tgtAddr->gtOp.gtOp2->TypeGet() == TYP_BYREF || tgtAddr->gtOp.gtOp2->TypeGet() == TYP_REF));
                    tgtAddr        = tgtAddr->gtOp.gtOp1;
                    simplifiedExpr = true;
                }
                else if (tgtAddr->gtOp.gtOp2->TypeGet() == TYP_BYREF || tgtAddr->gtOp.gtOp2->TypeGet() == TYP_REF)
                {
                    tgtAddr        = tgtAddr->gtOp.gtOp2;
                    simplifiedExpr = true;
                }
                else
                {
                    // We might have a native int. For example:
                    //        const     int    0
                    //    +         byref
                    //        lclVar    int    V06 loc5  // this is a local declared "valuetype VType*"
                    return GCInfo::WBF_BarrierUnknown;
                }
            }
            else
            {
                // Must be an LEA (i.e., an AddrMode)
                assert(tgtAddr->OperGet() == GT_LEA);
                tgtAddr = tgtAddr->AsAddrMode()->Base();
                if (tgtAddr->TypeGet() == TYP_BYREF || tgtAddr->TypeGet() == TYP_REF)
                {
                    simplifiedExpr = true;
                }
                else
                {
                    // We might have a native int.
                    return GCInfo::WBF_BarrierUnknown;
                }
            }
        }
    }
    if (tgtAddr->IsLocalAddrExpr() != nullptr)
    {
        // No need for a GC barrier when writing to a local variable.
        return GCInfo::WBF_NoBarrier;
    }
    if (tgtAddr->OperGet() == GT_LCL_VAR)
    {
        unsigned lclNum = tgtAddr->AsLclVar()->GetLclNum();

        LclVarDsc* varDsc = &compiler->lvaTable[lclNum];

        // Instead of marking LclVar with 'lvStackByref',
        // Consider decomposing the Value Number given to this LclVar to see if it was
        // created using a GT_ADDR(GT_LCLVAR)  or a GT_ADD( GT_ADDR(GT_LCLVAR), Constant)

        // We may have an internal compiler temp created in fgMorphCopyBlock() that we know
        // points at one of our stack local variables, it will have lvStackByref set to true.
        //
        if (varDsc->lvStackByref)
        {
            assert(varDsc->TypeGet() == TYP_BYREF);
            return GCInfo::WBF_NoBarrier;
        }

        // We don't eliminate for inlined methods, where we (can) know where the "retBuff" points.
        if (!compiler->compIsForInlining() && lclNum == compiler->info.compRetBuffArg)
        {
            assert(compiler->info.compRetType == TYP_STRUCT); // Else shouldn't have a ret buff.

            // Are we assured that the ret buff pointer points into the stack of a caller?
            if (compiler->info.compRetBuffDefStack)
            {
#if 0
                // This is an optional debugging mode.  If the #if 0 above is changed to #if 1,
                // every barrier we remove for stores to GC ref fields of a retbuff use a special
                // helper that asserts that the target is not in the heap.
#ifdef DEBUG
                return WBF_NoBarrier_CheckNotHeapInDebug;
#else
                return WBF_NoBarrier;
#endif
#else  // 0
                return GCInfo::WBF_NoBarrier;
#endif // 0
            }
        }
    }
    if (tgtAddr->TypeGet() == TYP_REF)
    {
        return GCInfo::WBF_BarrierUnchecked;
    }
    // Otherwise, we have no information.
    return GCInfo::WBF_BarrierUnknown;
}

//------------------------------------------------------------------------
// gcUpdateForRegVarMove: Update the masks when a variable is moved
//
// Arguments:
//    srcMask - The register mask for the register(s) from which it is being moved
//    dstMask - The register mask for the register(s) to which it is being moved
//    type    - The type of the variable
//
// Return Value:
//    None
//
// Notes:
//    This is called during codegen when a var is moved due to an LSRA_ASG.
//    It is also called by LinearScan::recordVarLocationAtStartOfBB() which is in turn called by
//    CodeGen::genCodeForBBList() at the block boundary.

void GCInfo::gcUpdateForRegVarMove(regMaskTP srcMask, regMaskTP dstMask, LclVarDsc* varDsc)
{
    var_types type    = varDsc->TypeGet();
    bool      isGCRef = (type == TYP_REF);
    bool      isByRef = (type == TYP_BYREF);

    if (srcMask != RBM_NONE)
    {
        regSet->RemoveMaskVars(srcMask);
        if (isGCRef)
        {
            assert((gcRegByrefSetCur & srcMask) == 0);
            gcRegGCrefSetCur &= ~srcMask;
            gcRegGCrefSetCur |= dstMask; // safe if no dst, i.e. RBM_NONE
        }
        else if (isByRef)
        {
            assert((gcRegGCrefSetCur & srcMask) == 0);
            gcRegByrefSetCur &= ~srcMask;
            gcRegByrefSetCur |= dstMask; // safe if no dst, i.e. RBM_NONE
        }
    }
    else if (isGCRef || isByRef)
    {
        // In this case, we are moving it from the stack to a register,
        // so remove it from the set of live stack gc refs
        VarSetOps::RemoveElemD(compiler, gcVarPtrSetCur, varDsc->lvVarIndex);
    }
    if (dstMask != RBM_NONE)
    {
        regSet->AddMaskVars(dstMask);
        // If the source is a reg, then the gc sets have been set appropriately
        // Otherwise, we have to determine whether to set them
        if (srcMask == RBM_NONE)
        {
            if (isGCRef)
            {
                gcRegGCrefSetCur |= dstMask;
            }
            else if (isByRef)
            {
                gcRegByrefSetCur |= dstMask;
            }
        }
    }
    else if (isGCRef || isByRef)
    {
        VarSetOps::AddElemD(compiler, gcVarPtrSetCur, varDsc->lvVarIndex);
    }
}

/*****************************************************************************/
/*****************************************************************************/
