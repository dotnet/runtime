// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifdef TARGET_AMD64
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
                compiler->GetEmitter()->emitDispRegSet(gcRegGCrefSetCur);
                printf(" => ");
            }
            printRegMaskInt(gcRegGCrefSetNew);
            compiler->GetEmitter()->emitDispRegSet(gcRegGCrefSetNew);
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
                compiler->GetEmitter()->emitDispRegSet(gcRegByrefSetCur);
                printf(" => ");
            }
            printRegMaskInt(gcRegByrefSetNew);
            compiler->GetEmitter()->emitDispRegSet(gcRegByrefSetNew);
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
    // This set of registers are going to hold REFs.
    // Make sure they were not holding BYREFs.
    assert((gcRegByrefSetCur & regMask) == 0);

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

    regMaskTP gcRegByrefSetNew = gcRegByrefSetCur & ~(regMask & ~regSet->GetMaskVars());
    regMaskTP gcRegGCrefSetNew = gcRegGCrefSetCur & ~(regMask & ~regSet->GetMaskVars());

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

//------------------------------------------------------------------------
// gcIsWriteBarrierCandidate: Get the write barrier kind for the given store.
//
// Arguments:
//    store - The STOREIND node in question
//
// Return Value:
//    Form of the write barrier that will need to be used when generating
//    code for "store".
//
GCInfo::WriteBarrierForm GCInfo::gcIsWriteBarrierCandidate(GenTreeStoreInd* store)
{
    if (!store->TypeIs(TYP_REF))
    {
        // Note that byref values cannot be in the managed heap.
        return WBF_NoBarrier;
    }

    // Ignore any assignments of NULL.
    if ((store->Data()->GetVN(VNK_Liberal) == ValueNumStore::VNForNull()) || store->Data()->IsIntegralConst(0))
    {
        return WBF_NoBarrier;
    }

    if ((store->gtFlags & GTF_IND_TGT_NOT_HEAP) != 0)
    {
        // This indirection is not from to the heap.
        // This case occurs for stack-allocated objects.
        return WBF_NoBarrier;
    }

    WriteBarrierForm wbf = gcWriteBarrierFormFromTargetAddress(store->Addr());

    if (wbf == WBF_BarrierUnknown)
    {
        wbf = ((store->gtFlags & GTF_IND_TGT_HEAP) != 0) ? WBF_BarrierUnchecked : WBF_BarrierChecked;
    }

    return wbf;
}

//------------------------------------------------------------------------
// gcWriteBarrierFormFromTargetAddress: Get the write barrier form from address.
//
// This method deconstructs "tgtAddr" to find out if it is "based on" a TYP_REF
// address, allowing an unchecked barrier to be used, or an address of a local,
// in which case no barrier is needed.
//
// Arguments:
//    tgtAddr - The target address of the store
//
// Return Value:
//    The write barrier form to use for "STOREIND<ref>(tgtAddr, ...)".
//
GCInfo::WriteBarrierForm GCInfo::gcWriteBarrierFormFromTargetAddress(GenTree* tgtAddr)
{
    // We will assume there is no point in trying to deconstruct a TYP_I_IMPL address.
    if (tgtAddr->TypeGet() == TYP_I_IMPL)
    {
        return GCInfo::WBF_BarrierUnknown;
    }

    // Otherwise...
    assert(tgtAddr->TypeGet() == TYP_BYREF);
    bool simplifiedExpr = true;
    while (simplifiedExpr)
    {
        simplifiedExpr = false;

        tgtAddr = tgtAddr->gtSkipReloadOrCopy();

        // For additions, one of the operands is a byref or a ref (and the other is not).  Follow this down to its
        // source.
        while (tgtAddr->OperIs(GT_ADD, GT_LEA))
        {
            if (tgtAddr->OperGet() == GT_ADD)
            {
                GenTree*  addOp1     = tgtAddr->AsOp()->gtGetOp1();
                GenTree*  addOp2     = tgtAddr->AsOp()->gtGetOp2();
                var_types addOp1Type = addOp1->TypeGet();
                var_types addOp2Type = addOp2->TypeGet();
                if (addOp1Type == TYP_BYREF || addOp1Type == TYP_REF)
                {
                    assert(addOp2Type != TYP_BYREF && addOp2Type != TYP_REF);
                    tgtAddr        = addOp1;
                    simplifiedExpr = true;
                }
                else if (addOp2Type == TYP_BYREF || addOp2Type == TYP_REF)
                {
                    tgtAddr        = addOp2;
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
        unsigned   lclNum = tgtAddr->AsLclVar()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);

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
    }

    if (tgtAddr->TypeGet() == TYP_REF)
    {
        return GCInfo::WBF_BarrierUnchecked;
    }

    // Otherwise, we have no information.
    return GCInfo::WBF_BarrierUnknown;
}

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

    assert(compiler->IsFullPtrRegMapRequired());

    /* Allocate a new entry and initialize it */

    regPtrNext = new (compiler, CMK_GC) regPtrDsc;

    regPtrNext->rpdIsThis = false;

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

#ifdef JIT32_GCENCODER

/*****************************************************************************
 *
 *  Compute the various counts that get stored in the info block header.
 */

void GCInfo::gcCountForHeader(UNALIGNED unsigned int* pUntrackedCount, UNALIGNED unsigned int* pVarPtrTableSize)
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    bool         keepThisAlive  = false; // did we track "this" in a synchronized method?
    unsigned int untrackedCount = 0;

    // Count the untracked locals and non-enregistered args.

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // Field local of a PROMOTION_TYPE_DEPENDENT struct must have been
            // reported through its parent local
            continue;
        }

        if (varTypeIsGC(varDsc->TypeGet()))
        {
            if (!gcIsUntrackedLocalOrNonEnregisteredArg(varNum, &keepThisAlive))
            {
                continue;
            }

#ifdef DEBUG
            if (compiler->verbose)
            {
                int offs = varDsc->GetStackOffset();

                printf("GCINFO: untrckd %s lcl at [%s", varTypeGCstring(varDsc->TypeGet()),
                       compiler->GetEmitter()->emitGetFrameReg());

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

            untrackedCount++;
        }
        else if ((varDsc->TypeGet() == TYP_STRUCT) && varDsc->lvOnFrame)
        {
            untrackedCount += varDsc->GetLayout()->GetGCPtrCount();
        }
    }

    // Also count spill temps that hold pointers.

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
                   compiler->GetEmitter()->emitGetFrameReg());

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

        untrackedCount++;
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("GCINFO: untrckVars = %u\n", untrackedCount);
    }
#endif

    *pUntrackedCount = untrackedCount;

    // Count the number of entries in the table of non-register pointer variable lifetimes.

    unsigned int varPtrTableSize = 0;

    if (keepThisAlive)
    {
        varPtrTableSize++;
    }

    if (gcVarPtrList != nullptr)
    {
        // We'll use a delta encoding for the lifetime offsets.

        for (varPtrDsc* varTmp = gcVarPtrList; varTmp != nullptr; varTmp = varTmp->vpdNext)
        {
            // Special case: skip any 0-length lifetimes.

            if (varTmp->vpdBegOfs == varTmp->vpdEndOfs)
            {
                continue;
            }

            varPtrTableSize++;
        }
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("GCINFO: trackdLcls = %u\n", varPtrTableSize);
    }
#endif

    *pVarPtrTableSize = varPtrTableSize;
}

//------------------------------------------------------------------------
// gcIsUntrackedLocalOrNonEnregisteredArg: Check if this varNum with GC type
// corresponds to an untracked local or argument that was not fully enregistered.
//
//
// Arguments:
//   varNum - the variable number to check;
//   pKeepThisAlive - if !FEATURE_EH_FUNCLETS and the argument != nullptr remember
//   if `this` should be kept alive and considered tracked.
//
// Return value:
//   true if it an untracked pointer value.
//
bool GCInfo::gcIsUntrackedLocalOrNonEnregisteredArg(unsigned varNum, bool* pKeepThisAlive)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

    assert(!compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc));
    assert(varTypeIsGC(varDsc->TypeGet()));

    // Do we have an argument or local variable?
    if (!varDsc->lvIsParam)
    {
        // If is pinned, it must be an untracked local.
        assert(!varDsc->lvPinned || !varDsc->lvTracked);

        if (varDsc->lvTracked || !varDsc->lvOnFrame)
        {
            return false;
        }
    }
    else
    {
        // Stack-passed arguments which are not enregistered are always reported in this "untracked stack pointers"
        // section of the GC info even if lvTracked==true.

        // Has this argument been fully enregistered?
        if (!varDsc->lvOnFrame)
        {
            // If a CEE_JMP has been used, then we need to report all the arguments even if they are enregistered, since
            // we will be using this value in JMP call.  Note that this is subtle as we require that argument offsets
            // are always fixed up properly even if lvRegister is set .
            if (!compiler->compJmpOpUsed)
            {
                return false;
            }
        }
        else if (varDsc->lvIsRegArg && varDsc->lvTracked)
        {
            // If this register-passed arg is tracked, then it has been allocated space near the other pointer variables
            // and we have accurate life-time info. It will be reported with gcVarPtrList in the "tracked-pointer"
            // section.
            return false;
        }
    }

#if !defined(FEATURE_EH_FUNCLETS)
    if (compiler->lvaIsOriginalThisArg(varNum) && compiler->lvaKeepAliveAndReportThis())
    {
        // "this" is in the untracked variable area, but encoding of untracked variables does not support reporting
        // "this". So report it as a tracked variable with a liveness extending over the entire method.
        //
        // TODO-x86-Cleanup: the semantic here is not clear, it would be useful to check different cases and
        // add a description where "this" is saved and how it is tracked in each of them:
        // 1) when FEATURE_EH_FUNCLETS defined (x86 Linux);
        // 2) when FEATURE_EH_FUNCLETS not defined, lvaKeepAliveAndReportThis == true, compJmpOpUsed == true;
        // 3) when there is regPtrDsc for "this", but keepThisAlive == true;
        // etc.

        if (pKeepThisAlive != nullptr)
        {
            *pKeepThisAlive = true;
        }
        return false;
    }
#endif // !FEATURE_EH_FUNCLETS
    return true;
}

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

#endif // JIT32_GCENCODER

/*****************************************************************************
 *
 *  Initialize the 'pointer value' register/argument tracking logic.
 */

void GCInfo::gcRegPtrSetInit()
{
    gcRegGCrefSetCur = gcRegByrefSetCur = 0;

    if (compiler->IsFullPtrRegMapRequired())
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
