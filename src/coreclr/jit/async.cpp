#include "jitpch.h"
#include "jitstd/algorithm.h"
#include "async.h"

PhaseStatus Compiler::TransformAsync2()
{
    assert(compIsAsync2());

    Async2Transformation transformation(this);
    return transformation.Run();
}

class AsyncLiveness
{
    Compiler*              m_comp;
    bool                   m_hasLiveness;
    TreeLifeUpdater<false> m_updater;
    unsigned               m_numVars;

public:
    AsyncLiveness(Compiler* comp, bool hasLiveness)
        : m_comp(comp), m_hasLiveness(hasLiveness), m_updater(comp), m_numVars(comp->lvaCount)
    {
    }

    void StartBlock(BasicBlock* block)
    {
        if (!m_hasLiveness)
            return;

        VarSetOps::Assign(m_comp, m_comp->compCurLife, block->bbLiveIn);
    }

    void Update(GenTree* node)
    {
        if (!m_hasLiveness)
            return;

        m_updater.UpdateLife(node);
    }

    bool IsLocalCaptureUnnecessary(unsigned lclNum)
    {
#if FEATURE_FIXED_OUT_ARGS
        if (lclNum == m_comp->lvaOutgoingArgSpaceVar)
        {
            return true;
        }
#endif

        if (lclNum == m_comp->lvaGSSecurityCookie)
        {
            // Initialized in prolog
            return true;
        }

#ifdef FEATURE_EH_FUNCLETS
        if (lclNum == m_comp->lvaPSPSym)
        {
            // Initialized in prolog
            return true;
        }
#else
        if (lclNum == m_comp->lvaShadowSPslotsVar)
        {
            // Only expected to be live in handlers
            return true;
        }
#endif

        if (lclNum == m_comp->lvaRetAddrVar)
        {
            return true;
        }

        if (lclNum == m_comp->lvaAsyncContinuationArg)
        {
            return true;
        }

        return false;
    }

    bool IsLive(unsigned lclNum)
    {
        if (IsLocalCaptureUnnecessary(lclNum))
        {
            return false;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(lclNum);

        if ((dsc->TypeGet() == TYP_BYREF) || ((dsc->TypeGet() == TYP_STRUCT) && dsc->GetLayout()->HasGCByRef()))
        {
            // Even if these are address exposed we expect them to be dead at
            // suspension points. TODO: It would be good to somehow verify these
            // aren't obviously live, if the JIT creates live ranges that span a
            // suspension point then this makes it quite hard to diagnose that.
            return false;
        }

        if (!m_hasLiveness)
        {
            return true;
        }

        if (dsc->lvRefCnt(RCS_NORMAL) == 0)
        {
            return false;
        }

        Compiler::lvaPromotionType promoType = m_comp->lvaGetPromotionType(dsc);
        if (promoType == Compiler::PROMOTION_TYPE_INDEPENDENT)
        {
            // Independently promoted structs are handled only through their
            // fields.
            return false;
        }

        if (promoType == Compiler::PROMOTION_TYPE_DEPENDENT)
        {
            // Dependently promoted structs are handled only through the base
            // struct local.
            //
            // A dependently promoted struct is live either if it has significant
            // padding (since we do not track liveness for the significant
            // padding), or if all its fields are dead.

            if (dsc->lvAnySignificantPadding)
            {
                // We could technically only save the padding and the live fields
                // in this case, but that's a lot of complexity for not a lot of
                // gain.
                return true;
            }

            for (unsigned i = 0; i < dsc->lvFieldCnt; i++)
            {
                LclVarDsc* fieldDsc = m_comp->lvaGetDesc(dsc->lvFieldLclStart + i);
                if (!fieldDsc->lvTracked || VarSetOps::IsMember(m_comp, m_comp->compCurLife, fieldDsc->lvVarIndex))
                {
                    return true;
                }
            }

            return false;
        }

        if (dsc->lvIsStructField && (m_comp->lvaGetParentPromotionType(dsc) == Compiler::PROMOTION_TYPE_DEPENDENT))
        {
            return false;
        }

        return !dsc->lvTracked || VarSetOps::IsMember(m_comp, m_comp->compCurLife, dsc->lvVarIndex);
    }

    void GetLiveLocals(jitstd::vector<Async2Transformation::LiveLocalInfo>& liveLocals)
    {
        for (unsigned lclNum = 0; lclNum < m_numVars; lclNum++)
        {
            if (IsLive(lclNum))
            {
                liveLocals.push_back(Async2Transformation::LiveLocalInfo(lclNum));
            }
        }
    }
};

PhaseStatus Async2Transformation::Run()
{
    ArrayStack<BasicBlock*> worklist(m_comp->getAllocator(CMK_Async2));

    for (BasicBlock* block : m_comp->Blocks())
    {
        for (GenTree* tree : LIR::AsRange(block))
        {
            if (tree->IsCall() && tree->AsCall()->IsAsync2())
            {
                JITDUMP(FMT_BB " contains await(s)\n", block->bbNum);
                worklist.Push(block);
                break;
            }
        }
    }

    JITDUMP("Found %d blocks with awaits\n", worklist.Height());

    if (worklist.Height() <= 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    m_resumeStub = m_comp->info.compCompHnd->getAsyncResumptionStub();
    m_comp->info.compCompHnd->getFunctionFixedEntryPoint(m_resumeStub, false, &m_resumeStubLookup);

    m_returnedContinuationVar = m_comp->lvaGrabTemp(false DEBUGARG("returned continuation"));
    m_comp->lvaGetDesc(m_returnedContinuationVar)->lvType = TYP_REF;
    m_newContinuationVar                                  = m_comp->lvaGrabTemp(false DEBUGARG("new continuation"));
    m_comp->lvaGetDesc(m_newContinuationVar)->lvType      = TYP_REF;

    m_comp->info.compCompHnd->getAsync2Info(&m_async2Info);
    m_objectClsHnd = m_comp->info.compCompHnd->getBuiltinClass(CLASSID_SYSTEM_OBJECT);
    m_byteClsHnd   = m_comp->info.compCompHnd->getBuiltinClass(CLASSID_SYSTEM_BYTE);

    if (m_comp->opts.OptimizationEnabled())
    {
        m_comp->lvaComputeRefCounts(true, false);
        m_comp->fgLocalVarLiveness();
        VarSetOps::AssignNoCopy(m_comp, m_comp->compCurLife, VarSetOps::MakeEmpty(m_comp));
    }

    AsyncLiveness liveness(m_comp, m_comp->opts.OptimizationEnabled());

    jitstd::vector<GenTree*> defs(m_comp->getAllocator(CMK_Async2));

    for (int i = 0; i < worklist.Height(); i++)
    {
        assert(defs.size() == 0);

        BasicBlock* block = worklist.Bottom(i);
        liveness.StartBlock(block);

        bool any;
        do
        {
            any = false;
            for (GenTree* tree : LIR::AsRange(block))
            {
                tree->VisitOperands([&defs](GenTree* op) {
                    if (op->IsValue())
                    {
                        for (size_t i = defs.size(); i > 0; i--)
                        {
                            if (op == defs[i - 1])
                            {
                                defs[i - 1] = defs[defs.size() - 1];
                                defs.erase(defs.begin() + (defs.size() - 1), defs.end());
                                break;
                            }
                        }
                    }

                    return GenTree::VisitResult::Continue;
                });

                liveness.Update(tree);

                if (tree->IsCall() && tree->AsCall()->IsAsync2() && !tree->AsCall()->IsTailCall())
                {
                    Transform(block, tree->AsCall(), defs, liveness, &block);
                    defs.clear();
                    any = true;
                    break;
                }

                if (tree->IsValue() && !tree->IsUnusedValue())
                {
                    defs.push_back(tree);
                }
            }
        } while (any);
    }

    CreateResumptionSwitch();

    return PhaseStatus::MODIFIED_EVERYTHING;
}

void Async2Transformation::Transform(
    BasicBlock* block, GenTreeCall* call, jitstd::vector<GenTree*>& defs, AsyncLiveness& life, BasicBlock** pRemainder)
{
#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("Processing call [%06u]\n", Compiler::dspTreeID(call));
        printf("  %zu live LIR edges\n", defs.size());

        if (defs.size() > 0)
        {
            const char* sep = "    ";
            for (GenTree* tree : defs)
            {
                printf("%s[%06u] (%s)", sep, Compiler::dspTreeID(tree), varTypeName(tree->TypeGet()));
                sep = ", ";
            }

            printf("\n");
        }
    }
#endif

    m_liveLocals.clear();

    life.GetLiveLocals(m_liveLocals);
    LiftLIREdges(block, call, defs, m_liveLocals);

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  %zu live locals\n", m_liveLocals.size());

        if (m_liveLocals.size() > 0)
        {
            const char* sep = "    ";
            for (LiveLocalInfo& inf : m_liveLocals)
            {
                printf("%sV%02u (%s)", sep, inf.LclNum, varTypeName(m_comp->lvaGetDesc(inf.LclNum)->TypeGet()));
                sep = ", ";
            }

            printf("\n");
        }
    }
#endif

    for (LiveLocalInfo& inf : m_liveLocals)
    {
        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        if ((dsc->TypeGet() == TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            ClassLayout* layout = dsc->GetLayout();
            assert(!layout->HasGCByRef());

            if (layout->IsBlockLayout())
            {
                inf.Alignment   = 1;
                inf.DataSize    = layout->GetSize();
                inf.GCDataCount = 0;
            }
            else
            {
                inf.Alignment = m_comp->info.compCompHnd->getClassAlignmentRequirement(layout->GetClassHandle());
                if ((layout->GetGCPtrCount() * TARGET_POINTER_SIZE) == layout->GetSize())
                {
                    inf.DataSize = 0;
                }
                else
                {
                    inf.DataSize = layout->GetSize();
                }

                inf.GCDataCount = layout->GetGCPtrCount();
            }
        }
        else if (dsc->TypeGet() == TYP_REF)
        {
            inf.Alignment   = TARGET_POINTER_SIZE;
            inf.DataSize    = 0;
            inf.GCDataCount = 1;
        }
        else
        {
            assert(dsc->TypeGet() != TYP_BYREF);

            inf.Alignment   = genTypeAlignments[dsc->TypeGet()];
            inf.DataSize    = genTypeSize(dsc);
            inf.GCDataCount = 0;
        }
    }

    jitstd::sort(m_liveLocals.begin(), m_liveLocals.end(), [](const LiveLocalInfo& lhs, const LiveLocalInfo& rhs) {
        if (lhs.Alignment == rhs.Alignment)
        {
            // Prefer lowest local num first for same alignment.
            return lhs.LclNum < rhs.LclNum;
        }

        // Otherwise prefer highest alignment first.
        return lhs.Alignment > rhs.Alignment;
    });

    unsigned dataSize    = 0;
    unsigned gcRefsCount = 0;

    // Result value is currently always stored boxed in the continuation as it
    // needs to be written by the resumption stub and by the asynchronous
    // callbacks. If we specialized the continuation type we could avoid this.
    if (call->gtReturnType != TYP_VOID)
        gcRefsCount++;

    unsigned exceptionGCDataIndex = UINT_MAX;
    if (block->hasTryIndex())
        exceptionGCDataIndex = gcRefsCount++;

    // For OSR, we store the patchpoint information at the beginning of the data
    // (null in the tier0 version):
    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
        dataSize += sizeof(int);

    for (LiveLocalInfo& inf : m_liveLocals)
    {
        dataSize = roundUp(dataSize, inf.Alignment);

        inf.DataOffset  = dataSize;
        inf.GCDataIndex = gcRefsCount;

        dataSize += inf.DataSize;
        gcRefsCount += inf.GCDataCount;
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  Continuation layout (%u bytes, %u GC pointers):\n", dataSize, gcRefsCount);
        for (LiveLocalInfo& inf : m_liveLocals)
        {
            printf("    +%03u (GC@+%02u) V%02u: %u bytes, %u GC pointers\n", inf.DataOffset, inf.GCDataIndex,
                   inf.LclNum, inf.DataSize, inf.GCDataCount);
        }
    }
#endif

    unsigned stateNum = (unsigned)m_resumptionBBs.size();
    JITDUMP("  Assigned state %u\n", stateNum);

    GenTreeLclVarCommon* storeResultNode = nullptr;
    GenTree*             insertAfter     = call;

    if (!call->TypeIs(TYP_VOID) && !call->IsUnusedValue())
    {
        assert(call->gtNext != nullptr);
        if (!call->gtNext->OperIsLocalStore() || (call->gtNext->Data() != call))
        {
            LIR::Use use;
            bool     gotUse = LIR::AsRange(block).TryGetUse(call, &use);
            assert(gotUse);

            use.ReplaceWithLclVar(m_comp);
        }
        else
        {
            // We will split after the store, but we still have to update liveness for it.
            life.Update(call->gtNext);
        }

        assert(call->gtNext->OperIsLocalStore() && (call->gtNext->Data() == call));
        storeResultNode = call->gtNext->AsLclVarCommon();
        insertAfter     = call->gtNext;
    }

    GenTree* continuationArg = new (m_comp, GT_ASYNC_CONTINUATION) GenTree(GT_ASYNC_CONTINUATION, TYP_REF);
    continuationArg->SetHasOrderingSideEffect();

    GenTree* storeContinuation = m_comp->gtNewStoreLclVarNode(m_returnedContinuationVar, continuationArg);
    LIR::AsRange(block).InsertAfter(insertAfter, continuationArg, storeContinuation);

    GenTree* null                 = m_comp->gtNewNull();
    GenTree* returnedContinuation = m_comp->gtNewLclvNode(m_returnedContinuationVar, TYP_REF);
    GenTree* neNull               = m_comp->gtNewOperNode(GT_NE, TYP_INT, returnedContinuation, null);
    GenTree* jtrue                = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);

    LIR::AsRange(block).InsertAfter(storeContinuation, null, returnedContinuation, neNull, jtrue);
    BasicBlock* remainder = m_comp->fgSplitBlockAfterNode(block, jtrue);
    *pRemainder           = remainder;

    assert(block->KindIs(BBJ_NONE) && block->NextIs(remainder));

    if (m_lastSuspensionBB == nullptr)
    {
        m_lastSuspensionBB = m_comp->fgLastBBInMainFunction();
    }

    BasicBlock* retBB = m_comp->fgNewBBafter(BBJ_RETURN, m_lastSuspensionBB, false);
    retBB->bbSetRunRarely();
    retBB->clearTryIndex();
    retBB->clearHndIndex();
    m_lastSuspensionBB = retBB;

    block->SetJumpKindAndTarget(BBJ_COND, retBB DEBUGARG(m_comp));

    m_comp->fgAddRefPred(retBB, block);

    // Allocate continuation
    returnedContinuation           = m_comp->gtNewLclvNode(m_returnedContinuationVar, TYP_REF);
    GenTree*     gcRefsCountNode   = m_comp->gtNewIconNode((ssize_t)gcRefsCount, TYP_I_IMPL);
    GenTree*     dataSizeNode      = m_comp->gtNewIconNode((ssize_t)dataSize, TYP_I_IMPL);
    GenTreeCall* allocContinuation = m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION, TYP_REF,
                                                                 returnedContinuation, gcRefsCountNode, dataSizeNode);

    m_comp->compCurBB = retBB;
    m_comp->fgMorphTree(allocContinuation);

    LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, allocContinuation));

    GenTree* storeNewContinuation = m_comp->gtNewStoreLclVarNode(m_newContinuationVar, allocContinuation);
    LIR::AsRange(retBB).InsertAtEnd(storeNewContinuation);

    // Fill in 'Resume'
    GenTree* newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned resumeOffset    = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationResumeFldHnd);
    GenTree* resumeStubAddr  = CreateResumptionStubAddrTree();
    GenTree* storeResume     = StoreAtOffset(newContinuation, resumeOffset, resumeStubAddr);
    LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResume));

    // Fill in 'state'
    newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned stateOffset  = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationStateFldHnd);
    GenTree* stateNumNode = m_comp->gtNewIconNode((ssize_t)stateNum, TYP_I_IMPL);
    GenTree* storeState   = StoreAtOffset(newContinuation, stateOffset, stateNumNode);
    LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, storeState));

    // Fill in 'flags'
    unsigned continuationFlags = 0;
    if (call->gtReturnType != TYP_VOID)
        continuationFlags |= CORINFO_CONTINUATION_RESULT_IN_GCDATA;
    if (block->hasTryIndex())
        continuationFlags |= CORINFO_CONTINUATION_NEEDS_EXCEPTION;

    newContinuation      = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned flagsOffset = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationFlagsFldHnd);
    GenTree* flagsNode   = m_comp->gtNewIconNode((ssize_t)continuationFlags, TYP_INT);
    GenTree* storeFlags  = StoreAtOffset(newContinuation, flagsOffset, flagsNode);
    LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, storeFlags));

    // Fill in GC pointers
    if (gcRefsCount > 0)
    {
        unsigned objectArrLclNum = GetGCDataArrayVar();

        newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned gcDataOffset = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationGCDataFldHnd);
        GenTree* gcDataInd    = LoadFromOffset(newContinuation, gcDataOffset, TYP_REF);
        GenTree* storeAllocedObjectArr = m_comp->gtNewStoreLclVarNode(objectArrLclNum, gcDataInd);
        LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, storeAllocedObjectArr));

        for (LiveLocalInfo& inf : m_liveLocals)
        {
            if (inf.GCDataCount <= 0)
            {
                continue;
            }

            LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);
            if (dsc->TypeGet() == TYP_REF)
            {
                GenTree* value     = m_comp->gtNewLclvNode(inf.LclNum, TYP_REF);
                GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
                GenTree* store =
                    StoreAtOffset(objectArr, OFFSETOF__CORINFO_Array__data + (inf.GCDataIndex * TARGET_POINTER_SIZE),
                                  value);
                LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
            }
            else
            {
                assert((dsc->TypeGet() == TYP_STRUCT) || dsc->IsImplicitByRef());
                ClassLayout* layout   = dsc->GetLayout();
                unsigned     numSlots = layout->GetSlotCount();
                for (unsigned i = 0; i < numSlots; i++)
                {
                    var_types gcPtrType = layout->GetGCPtrType(i);
                    assert((gcPtrType == TYP_I_IMPL) || (gcPtrType == TYP_REF));
                    if (gcPtrType != TYP_REF)
                    {
                        continue;
                    }

                    GenTree* value;
                    if (dsc->IsImplicitByRef())
                    {
                        GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                        value             = LoadFromOffset(baseAddr, i * TARGET_POINTER_SIZE, TYP_REF);
                    }
                    else
                    {
                        value = m_comp->gtNewLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE);
                    }

                    GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
                    unsigned offset    = OFFSETOF__CORINFO_Array__data + ((inf.GCDataIndex + i) * TARGET_POINTER_SIZE);
                    GenTree* store     = StoreAtOffset(objectArr, offset, value);
                    LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, store));

                    if (inf.DataSize > 0)
                    {
                        // Null out the GC field in preparation of storing the rest.
                        GenTree* null = m_comp->gtNewNull();

                        if (dsc->IsImplicitByRef())
                        {
                            GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                            store             = StoreAtOffset(baseAddr, i * TARGET_POINTER_SIZE, null);
                        }
                        else
                        {
                            store = m_comp->gtNewStoreLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE, null);
                        }

                        LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
                    }
                }

                m_comp->lvaSetVarDoNotEnregister(inf.LclNum DEBUGARG(DoNotEnregisterReason::LocalField));
            }
        }
    }

    // Store data in byte[]
    if (dataSize > 0)
    {
        unsigned byteArrLclNum = GetDataArrayVar();

        GenTree* newContinuation     = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned dataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationDataFldHnd);
        GenTree* dataOffsetNode      = m_comp->gtNewIconNode((ssize_t)dataOffset, TYP_I_IMPL);
        GenTree* dataAddr            = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation, dataOffsetNode);
        GenTree* dataInd             = m_comp->gtNewIndir(TYP_REF, dataAddr, GTF_IND_NONFAULTING);
        GenTree* storeAllocedByteArr = m_comp->gtNewStoreLclVarNode(byteArrLclNum, dataInd);
        LIR::AsRange(retBB).InsertAtEnd(newContinuation, dataOffsetNode, dataAddr, dataInd, storeAllocedByteArr);

        if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
        {
            GenTree* ilOffsetToStore;
            if (m_comp->doesMethodHavePatchpoints())
                ilOffsetToStore = m_comp->gtNewIconNode(-1);
            else
                ilOffsetToStore = m_comp->gtNewIconNode((int)m_comp->info.compILEntry);

            GenTree* byteArr               = m_comp->gtNewLclvNode(byteArrLclNum, TYP_REF);
            unsigned offset                = OFFSETOF__CORINFO_Array__data;
            GenTree* storePatchpointOffset = StoreAtOffset(byteArr, offset, ilOffsetToStore);
            LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, storePatchpointOffset));
        }

        // Fill in data
        for (LiveLocalInfo& inf : m_liveLocals)
        {
            if (inf.DataSize <= 0)
            {
                continue;
            }

            LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

            GenTree* byteArr = m_comp->gtNewLclvNode(byteArrLclNum, TYP_REF);
            unsigned offset  = OFFSETOF__CORINFO_Array__data + inf.DataOffset;

            GenTree* value;
            if (dsc->IsImplicitByRef())
            {
                GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                value             = m_comp->gtNewBlkIndir(dsc->GetLayout(), baseAddr, GTF_IND_NONFAULTING);
            }
            else
            {
                value = m_comp->gtNewLclvNode(inf.LclNum, genActualType(dsc->TypeGet()));
            }

            GenTree* store;
            if ((dsc->TypeGet() == TYP_STRUCT) || dsc->IsImplicitByRef())
            {
                GenTree* cns  = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
                GenTree* addr = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, byteArr, cns);
                // This is to heap, but all GC refs are nulled out already, so we can skip the write barrier.
                // TODO-CQ: Backend does not care about GTF_IND_TGT_NOT_HEAP for STORE_BLK.
                store = m_comp->gtNewStoreBlkNode(dsc->GetLayout(), addr, value,
                                                  GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP);
            }
            else
            {
                store = StoreAtOffset(byteArr, offset, value);
            }

            LIR::AsRange(retBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
        }
    }

    newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    GenTree* ret    = m_comp->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, newContinuation);
    LIR::AsRange(retBB).InsertAtEnd(newContinuation, ret);

    JITDUMP("  Created suspension BB " FMT_BB "\n", retBB->bbNum);

    BasicBlock* prevResumptionBB;
    if (m_resumptionBBs.size() == 0)
    {
        prevResumptionBB = m_comp->fgLastBBInMainFunction();
    }
    else
    {
        prevResumptionBB = m_resumptionBBs[m_resumptionBBs.size() - 1];
    }

    BasicBlock* resumeBB = m_comp->fgNewBBafter(BBJ_ALWAYS, prevResumptionBB, true, remainder);
    resumeBB->bbSetRunRarely();
    resumeBB->clearTryIndex();
    resumeBB->clearHndIndex();
    resumeBB->bbFlags |= BBF_ASYNC_RESUMPTION;

    m_comp->fgAddRefPred(remainder, resumeBB);

    if (dataSize > 0)
    {
        unsigned byteArrLclNum = GetDataArrayVar();

        GenTree* newContinuation     = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned dataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationDataFldHnd);
        GenTree* dataOffsetNode      = m_comp->gtNewIconNode((ssize_t)dataOffset, TYP_I_IMPL);
        GenTree* dataAddr            = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation, dataOffsetNode);
        GenTree* dataInd             = m_comp->gtNewIndir(TYP_REF, dataAddr, GTF_IND_NONFAULTING);
        GenTree* storeAllocedByteArr = m_comp->gtNewStoreLclVarNode(byteArrLclNum, dataInd);
        LIR::AsRange(resumeBB).InsertAtEnd(newContinuation, dataOffsetNode, dataAddr, dataInd, storeAllocedByteArr);

        // Copy data
        for (LiveLocalInfo& inf : m_liveLocals)
        {
            if (inf.DataSize <= 0)
            {
                continue;
            }

            LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

            GenTree* byteArr = m_comp->gtNewLclvNode(byteArrLclNum, TYP_REF);
            unsigned offset  = OFFSETOF__CORINFO_Array__data + inf.DataOffset;
            GenTree* cns     = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
            GenTree* addr    = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, byteArr, cns);

            GenTree* value;
            if ((dsc->TypeGet() == TYP_STRUCT) || dsc->IsImplicitByRef())
            {
                value = m_comp->gtNewBlkIndir(dsc->GetLayout(), addr, GTF_IND_NONFAULTING);
            }
            else
            {
                value = m_comp->gtNewIndir(dsc->TypeGet(), addr, GTF_IND_NONFAULTING);
            }

            GenTree* store;
            if (dsc->IsImplicitByRef())
            {
                GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                // TODO-CQ: Incoming data has no non-null GC refs, so this does not need write barriers.
                // Backend does not handle GTF_IND_TGT_NOT_HEAP for STORE_BLK.
                store = m_comp->gtNewStoreBlkNode(dsc->GetLayout(), baseAddr, value,
                                                  GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP);
            }
            else
            {
                store = m_comp->gtNewStoreLclVarNode(inf.LclNum, value);
            }

            LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
        }
    }

    if (gcRefsCount > 0)
    {
        unsigned objectArrLclNum = GetGCDataArrayVar();

        newContinuation           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned gcDataOffset     = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationGCDataFldHnd);
        GenTree* gcDataOffsetNode = m_comp->gtNewIconNode((ssize_t)gcDataOffset, TYP_I_IMPL);
        GenTree* gcDataAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation, gcDataOffsetNode);
        GenTree* gcDataInd        = m_comp->gtNewIndir(TYP_REF, gcDataAddr, GTF_IND_NONFAULTING);
        GenTree* storeAllocedObjectArr = m_comp->gtNewStoreLclVarNode(objectArrLclNum, gcDataInd);
        LIR::AsRange(resumeBB).InsertAtEnd(newContinuation, gcDataOffsetNode, gcDataAddr, gcDataInd,
                                           storeAllocedObjectArr);

        // Copy GC pointers
        for (LiveLocalInfo& inf : m_liveLocals)
        {
            if (inf.GCDataCount <= 0)
            {
                continue;
            }

            LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);
            if (dsc->TypeGet() == TYP_REF)
            {
                GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
                unsigned offset    = OFFSETOF__CORINFO_Array__data + (inf.GCDataIndex * TARGET_POINTER_SIZE);
                GenTree* value     = LoadFromOffset(objectArr, offset, TYP_REF);
                GenTree* store     = m_comp->gtNewStoreLclVarNode(inf.LclNum, value);

                LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
            }
            else
            {
                assert((dsc->TypeGet() == TYP_STRUCT) || dsc->IsImplicitByRef());
                ClassLayout* layout   = dsc->GetLayout();
                unsigned     numSlots = layout->GetSlotCount();
                for (unsigned i = 0; i < numSlots; i++)
                {
                    var_types gcPtrType = layout->GetGCPtrType(i);
                    assert((gcPtrType == TYP_I_IMPL) || (gcPtrType == TYP_REF));
                    if (gcPtrType != TYP_REF)
                    {
                        continue;
                    }

                    GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
                    unsigned offset    = OFFSETOF__CORINFO_Array__data + ((inf.GCDataIndex + i) * TARGET_POINTER_SIZE);
                    GenTree* value     = LoadFromOffset(objectArr, offset, TYP_REF);
                    GenTree* store;
                    if (dsc->IsImplicitByRef())
                    {
                        GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                        store             = StoreAtOffset(baseAddr, i * TARGET_POINTER_SIZE, value);
                        // Implicit byref args are never on heap today, skip write barriers.
                        // TODO-CQ: Remove this once all implicit byrefs are TYP_I_IMPL typed.
                        store->gtFlags |= GTF_IND_TGT_NOT_HEAP;
                    }
                    else
                    {
                        store = m_comp->gtNewStoreLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE, value);
                    }

                    LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
                }
            }
        }

        BasicBlock* storeResultBB = resumeBB;
        if (exceptionGCDataIndex != UINT_MAX)
        {
            BasicBlock* rethrowExceptionBB = m_comp->fgNewBBinRegion(BBJ_THROW, block, /* jumpDest */ nullptr,
                                                                     /* runRarely */ true, /* insertAtEnd */ true);
            resumeBB->SetJumpKindAndTarget(BBJ_COND, rethrowExceptionBB DEBUGARG(m_comp));
            m_comp->fgAddRefPred(rethrowExceptionBB, resumeBB);
            m_comp->fgRemoveRefPred(remainder, resumeBB);

            storeResultBB = m_comp->fgNewBBafter(BBJ_ALWAYS, resumeBB, true, remainder);
            m_comp->fgAddRefPred(remainder, storeResultBB);
            m_comp->fgAddRefPred(storeResultBB, resumeBB);

            // Check if we have an exception.
            unsigned exceptionLclNum = GetExceptionVar();
            GenTree* objectArr       = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
            unsigned exceptionOffset = OFFSETOF__CORINFO_Array__data + exceptionGCDataIndex * TARGET_POINTER_SIZE;
            GenTree* exceptionInd    = LoadFromOffset(objectArr, exceptionOffset, TYP_REF);
            GenTree* storeException  = m_comp->gtNewStoreLclVarNode(exceptionLclNum, exceptionInd);
            LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeException));

            GenTree* exception = m_comp->gtNewLclVarNode(exceptionLclNum, TYP_REF);
            GenTree* null      = m_comp->gtNewNull();
            GenTree* neNull    = m_comp->gtNewOperNode(GT_NE, TYP_INT, exception, null);
            GenTree* jtrue     = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
            LIR::AsRange(resumeBB).InsertAtEnd(exception, null, neNull, jtrue);

            exception                     = m_comp->gtNewLclVarNode(exceptionLclNum, TYP_REF);
            GenTreeCall* rethrowException = m_comp->gtNewHelperCallNode(CORINFO_HELP_THROWEXACT, TYP_VOID, exception);

            m_comp->compCurBB = rethrowExceptionBB;
            m_comp->fgMorphTree(rethrowException);

            LIR::AsRange(rethrowExceptionBB).InsertAtEnd(LIR::SeqTree(m_comp, rethrowException));

            storeResultBB->bbFlags |= BBF_ASYNC_RESUMPTION;
            JITDUMP("Added " FMT_BB " to rethrow exception at suspension point\n", rethrowExceptionBB->bbNum);
        }

        // Copy call return value.
        if (storeResultNode != nullptr)
        {
            GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
            GenTree* resultBox = LoadFromOffset(objectArr, OFFSETOF__CORINFO_Array__data, TYP_REF);

            LclVarDsc* resultLcl = m_comp->lvaGetDesc(storeResultNode);
            assert((resultLcl->TypeGet() == TYP_STRUCT) == storeResultNode->TypeIs(TYP_STRUCT));

            // TODO-TP: We can use liveness to avoid generating a lot of this IR.
            if (storeResultNode->TypeIs(TYP_STRUCT))
            {
                if (m_comp->lvaGetPromotionType(resultLcl) != Compiler::PROMOTION_TYPE_INDEPENDENT)
                {
                    GenTree* boxDataOffset = m_comp->gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                    GenTree* boxDataAddr   = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, resultBox, boxDataOffset);
                    GenTree* boxData =
                        m_comp->gtNewBlkIndir(storeResultNode->GetLayout(m_comp), boxDataAddr, GTF_IND_NONFAULTING);
                    GenTree* storeResult;
                    if (storeResultNode->OperIs(GT_STORE_LCL_VAR))
                    {
                        storeResult = m_comp->gtNewStoreLclVarNode(storeResultNode->GetLclNum(), boxData);
                    }
                    else
                    {
                        storeResult = m_comp->gtNewStoreLclFldNode(storeResultNode->GetLclNum(), TYP_STRUCT,
                                                                   storeResultNode->GetLayout(m_comp),
                                                                   storeResultNode->GetLclOffs(), boxData);
                    }

                    LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResult));
                }
                else
                {
                    if (resultLcl->lvFieldCnt > 1)
                    {
                        unsigned resultBoxVar   = GetResultBoxVar();
                        GenTree* storeResultBox = m_comp->gtNewStoreLclVarNode(resultBoxVar, resultBox);
                        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResultBox));

                        resultBox = m_comp->gtNewLclVarNode(resultBoxVar, TYP_REF);
                    }

                    assert(storeResultNode->OperIs(GT_STORE_LCL_VAR));
                    for (unsigned i = 0; i < resultLcl->lvFieldCnt; i++)
                    {
                        unsigned   fieldLclNum = resultLcl->lvFieldLclStart + i;
                        LclVarDsc* fieldDsc    = m_comp->lvaGetDesc(fieldLclNum);

                        unsigned fldOffset = TARGET_POINTER_SIZE + fieldDsc->lvFldOffset;
                        GenTree* value     = LoadFromOffset(resultBox, fldOffset, fieldDsc->TypeGet());
                        GenTree* store     = m_comp->gtNewStoreLclVarNode(fieldLclNum, value);
                        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));

                        if (i + 1 != resultLcl->lvFieldCnt)
                        {
                            resultBox = m_comp->gtCloneExpr(resultBox);
                        }
                    }
                }
            }
            else
            {
                GenTree* value;
                if (call->gtReturnType == TYP_REF)
                {
                    value = resultBox;
                }
                else
                {
                    value = LoadFromOffset(resultBox, TARGET_POINTER_SIZE, call->gtReturnType);
                }

                GenTree* storeResult;
                if (storeResultNode->OperIs(GT_STORE_LCL_VAR))
                {
                    storeResult = m_comp->gtNewStoreLclVarNode(storeResultNode->GetLclNum(), value);
                }
                else
                {
                    storeResult = m_comp->gtNewStoreLclFldNode(storeResultNode->GetLclNum(), storeResultNode->TypeGet(),
                                                               storeResultNode->GetLclOffs(), value);
                }

                LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResult));
            }
        }
    }

    m_resumptionBBs.push_back(resumeBB);
    JITDUMP("  Created resumption BB " FMT_BB "\n", resumeBB->bbNum);
}

GenTreeIndir* Async2Transformation::LoadFromOffset(GenTree* base, unsigned offset, var_types type)
{
    assert(base->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL));
    GenTree*      cns      = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types     addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*      addr     = m_comp->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeIndir* load     = m_comp->gtNewIndir(type, addr, GTF_IND_NONFAULTING);
    return load;
}

GenTreeStoreInd* Async2Transformation::StoreAtOffset(GenTree* base, unsigned offset, GenTree* value)
{
    assert(base->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL));
    GenTree*         cns      = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types        addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*         addr     = m_comp->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeStoreInd* store    = m_comp->gtNewStoreIndNode(value->TypeGet(), addr, value, GTF_IND_NONFAULTING);
    return store;
}

unsigned Async2Transformation::GetDataArrayVar()
{
    if (m_dataArrayVar == BAD_VAR_NUM)
    {
        m_dataArrayVar                             = m_comp->lvaGrabTemp(false DEBUGARG("byte[] for continuation"));
        m_comp->lvaGetDesc(m_dataArrayVar)->lvType = TYP_REF;
    }

    return m_dataArrayVar;
}

unsigned Async2Transformation::GetGCDataArrayVar()
{
    if (m_gcDataArrayVar == BAD_VAR_NUM)
    {
        m_gcDataArrayVar                             = m_comp->lvaGrabTemp(false DEBUGARG("object[] for continuation"));
        m_comp->lvaGetDesc(m_gcDataArrayVar)->lvType = TYP_REF;
    }

    return m_gcDataArrayVar;
}

unsigned Async2Transformation::GetResultBoxVar()
{
    if (m_resultBoxVar == BAD_VAR_NUM)
    {
        m_resultBoxVar = m_comp->lvaGrabTemp(false DEBUGARG("object for resuming result box"));
        m_comp->lvaGetDesc(m_resultBoxVar)->lvType = TYP_REF;
    }

    return m_resultBoxVar;
}

unsigned Async2Transformation::GetExceptionVar()
{
    if (m_exceptionVar == BAD_VAR_NUM)
    {
        m_exceptionVar = m_comp->lvaGrabTemp(false DEBUGARG("object for resuming exception"));
        m_comp->lvaGetDesc(m_exceptionVar)->lvType = TYP_REF;
    }

    return m_exceptionVar;
}

GenTree* Async2Transformation::CreateResumptionStubAddrTree()
{
    switch (m_resumeStubLookup.accessType)
    {
        case IAT_VALUE:
        {
            return CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
        }
        case IAT_PVALUE:
        {
            GenTree* tree = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            tree          = m_comp->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            return tree;
        }
        case IAT_PPVALUE:
        {
            noway_assert(!"Unexpected IAT_PPVALUE");
            return nullptr;
        }
        case IAT_RELPVALUE:
        {
            GenTree* addr = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            GenTree* tree = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            tree          = m_comp->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            tree          = m_comp->gtNewOperNode(GT_ADD, TYP_I_IMPL, tree, addr);
            return tree;
        }
        default:
        {
            noway_assert(!"Bad accessType");
            return nullptr;
        }
    }
}

GenTree* Async2Transformation::CreateFunctionTargetAddr(CORINFO_METHOD_HANDLE       methHnd,
                                                        const CORINFO_CONST_LOOKUP& lookup)
{
    GenTree* con = m_comp->gtNewIconHandleNode((size_t)lookup.addr, GTF_ICON_FTN_ADDR);
    INDEBUG(con->AsIntCon()->gtTargetHandle = (size_t)methHnd);
    return con;
}

void Async2Transformation::LiftLIREdges(BasicBlock*                    block,
                                        GenTree*                       beyond,
                                        jitstd::vector<GenTree*>&      defs,
                                        jitstd::vector<LiveLocalInfo>& liveLocals)
{
    if (defs.size() <= 0)
    {
        return;
    }

    for (GenTree* tree : defs)
    {
        // TODO-CQ: Breaks our recognition of how the call is stored.
        // if (tree->OperIs(GT_LCL_VAR))
        //{
        //    LclVarDsc* dsc = m_comp->lvaGetDesc(tree->AsLclVarCommon());
        //    if (!dsc->IsAddressExposed())
        //    {
        //        // No interference by IR invariants.
        //        LIR::AsRange(block).Remove(tree);
        //        LIR::AsRange(block).InsertAfter(beyond, tree);
        //        continue;
        //    }
        //}

        LIR::Use use;
        bool     gotUse = LIR::AsRange(block).TryGetUse(tree, &use);
        assert(gotUse); // Defs list should not contain unused values.

        unsigned newLclNum = use.ReplaceWithLclVar(m_comp);
        liveLocals.push_back(LiveLocalInfo(newLclNum));
        GenTree* newUse = use.Def();
        LIR::AsRange(block).Remove(newUse);
        LIR::AsRange(block).InsertBefore(use.User(), newUse);
    }
}

void Async2Transformation::CreateResumptionSwitch()
{
    BasicBlock* newEntryBB = m_comp->bbNewBasicBlock(BBJ_NONE);
    JITDUMP("New entry BB: " FMT_BB "\n", newEntryBB->bbNum);

    if (m_comp->fgFirstBB->hasProfileWeight())
    {
        newEntryBB->inheritWeight(m_comp->fgFirstBB);
    }
    m_comp->fgFirstBB->bbRefs--;

    FlowEdge* edge = m_comp->fgAddRefPred(m_comp->fgFirstBB, newEntryBB);
    edge->setLikelihood(1);

    m_comp->fgInsertBBbefore(m_comp->fgFirstBB, newEntryBB);

    // If previous first BB was a scratch BB, then we must add a new scratch BB
    // to create IR before the switch.
    m_comp->fgFirstBBScratch = nullptr;

    GenTree* continuationArg = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
    GenTree* null            = m_comp->gtNewNull();
    GenTree* neNull          = m_comp->gtNewOperNode(GT_NE, TYP_INT, continuationArg, null);
    GenTree* jtrue           = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
    LIR::AsRange(newEntryBB).InsertAtEnd(continuationArg, null, neNull, jtrue);

    if (m_resumptionBBs.size() == 1)
    {
        newEntryBB->SetJumpKindAndTarget(BBJ_COND, m_resumptionBBs[0] DEBUGARG(m_comp));
        m_comp->fgAddRefPred(m_resumptionBBs[0], newEntryBB);
    }
    else if (m_resumptionBBs.size() == 2)
    {
        BasicBlock* condBB = m_comp->fgNewBBbefore(BBJ_COND, m_resumptionBBs[0], true, m_resumptionBBs[1]);
        condBB->bbSetRunRarely();
        newEntryBB->SetJumpKindAndTarget(BBJ_COND, condBB DEBUGARG(m_comp));

        JITDUMP("New cond BB: " FMT_BB "\n", condBB->bbNum);

        m_comp->fgAddRefPred(condBB, newEntryBB);
        m_comp->fgAddRefPred(m_resumptionBBs[0], condBB);
        m_comp->fgAddRefPred(m_resumptionBBs[1], condBB);

        continuationArg          = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationStateFldHnd);
        GenTree* stateOffsetNode = m_comp->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_comp->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* zero            = m_comp->gtNewZeroConNode(TYP_INT);
        GenTree* stateNeZero     = m_comp->gtNewOperNode(GT_NE, TYP_INT, stateInd, zero);
        GenTree* jtrue           = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, stateNeZero);

        LIR::AsRange(condBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, zero, stateNeZero,
                                         jtrue);
    }
    else
    {
        BasicBlock* switchBB = m_comp->fgNewBBbefore(BBJ_SWITCH, m_resumptionBBs[0], true);
        switchBB->bbSetRunRarely();
        newEntryBB->SetJumpKindAndTarget(BBJ_COND, switchBB DEBUGARG(m_comp));

        JITDUMP("New switch BB: " FMT_BB "\n", switchBB->bbNum);

        m_comp->fgAddRefPred(switchBB, newEntryBB);

        continuationArg          = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationStateFldHnd);
        GenTree* stateOffsetNode = m_comp->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_comp->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* switchNode      = m_comp->gtNewOperNode(GT_SWITCH, TYP_VOID, stateInd);

        LIR::AsRange(switchBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, switchNode);

        m_comp->fgHasSwitch = true;

        //// Default case. TODO-CQ: Support bbsHasDefault = false before lowering.
        m_resumptionBBs.push_back(m_resumptionBBs[0]);
        BBswtDesc* swtDesc     = new (m_comp, CMK_BasicBlock) BBswtDesc;
        swtDesc->bbsCount      = (unsigned)m_resumptionBBs.size();
        swtDesc->bbsHasDefault = true;
        swtDesc->bbsDstTab     = m_resumptionBBs.data();

        switchBB->SetSwitchKindAndTarget(swtDesc);

        for (BasicBlock* bb : m_resumptionBBs)
        {
            m_comp->fgAddRefPred(bb, switchBB);
        }
    }

    if (m_comp->doesMethodHavePatchpoints())
    {
        // If we have patchpoints then first check if we need to resume in the OSR version.
        BasicBlock* callHelperBB = m_comp->fgNewBBafter(BBJ_THROW, m_comp->fgLastBBInMainFunction(), false);
        callHelperBB->bbSetRunRarely();
        callHelperBB->clearTryIndex();
        callHelperBB->clearHndIndex();

        BasicBlock* checkILOffsetBB = m_comp->fgNewBBbefore(BBJ_COND, newEntryBB->GetJumpDest(), true, callHelperBB);
        m_comp->fgRemoveRefPred(newEntryBB->GetJumpDest(), newEntryBB);
        newEntryBB->SetJumpDest(checkILOffsetBB);

        m_comp->fgAddRefPred(checkILOffsetBB, newEntryBB);
        m_comp->fgAddRefPred(checkILOffsetBB->Next(), checkILOffsetBB);
        m_comp->fgAddRefPred(checkILOffsetBB->GetJumpDest(), checkILOffsetBB);

        // We need to dispatch to the OSR version if the IL offset is non-negative.
        continuationArg           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfData     = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationDataFldHnd);
        GenTree* dataArr          = LoadFromOffset(continuationArg, offsetOfData, TYP_REF);
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Array__data;
        GenTree* ilOffset         = LoadFromOffset(dataArr, offsetOfIlOffset, TYP_INT);
        unsigned ilOffsetLclNum   = m_comp->lvaGrabTemp(false DEBUGARG("IL offset for tier0 OSR method"));
        m_comp->lvaGetDesc(ilOffsetLclNum)->lvType = TYP_INT;
        GenTree* storeIlOffset                     = m_comp->gtNewStoreLclVarNode(ilOffsetLclNum, ilOffset);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(LIR::SeqTree(m_comp, storeIlOffset));

        ilOffset        = m_comp->gtNewLclvNode(ilOffsetLclNum, TYP_INT);
        GenTree* zero   = m_comp->gtNewIconNode(0);
        GenTree* geZero = m_comp->gtNewOperNode(GT_GE, TYP_INT, ilOffset, zero);
        GenTree* jtrue  = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, geZero);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(ilOffset, zero, geZero, jtrue);

        ilOffset                = m_comp->gtNewLclvNode(ilOffsetLclNum, TYP_INT);
        GenTreeCall* callHelper = m_comp->gtNewHelperCallNode(CORINFO_HELP_RESUME_OSR, TYP_VOID, ilOffset);
        callHelper->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;

        m_comp->compCurBB = callHelperBB;
        m_comp->fgMorphTree(callHelper);

        LIR::AsRange(callHelperBB).InsertAtEnd(LIR::SeqTree(m_comp, callHelper));
    }
    else if (m_comp->opts.IsOSR())
    {
        // If the tier-0 version resumed and then transitioned to the OSR
        // version by normal means then we will see a non-zero continuation
        // here that belongs to the tier0 method. In that case we should just
        // ignore it, so create a BB that jumps back.
        BasicBlock* checkILOffsetBB =
            m_comp->fgNewBBbefore(BBJ_COND, newEntryBB->GetJumpDest(), true, newEntryBB->Next());
        m_comp->fgRemoveRefPred(newEntryBB->GetJumpDest(), newEntryBB);
        newEntryBB->SetJumpDest(checkILOffsetBB);

        m_comp->fgAddRefPred(checkILOffsetBB, newEntryBB);
        m_comp->fgAddRefPred(checkILOffsetBB->Next(), checkILOffsetBB);
        m_comp->fgAddRefPred(checkILOffsetBB->GetJumpDest(), checkILOffsetBB);

        continuationArg           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfData     = m_comp->info.compCompHnd->getFieldOffset(m_async2Info.continuationDataFldHnd);
        GenTree* dataArr          = LoadFromOffset(continuationArg, offsetOfData, TYP_REF);
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Array__data;
        GenTree* ilOffset         = LoadFromOffset(dataArr, offsetOfIlOffset, TYP_INT);
        GenTree* zero             = m_comp->gtNewIconNode(0);
        GenTree* ltZero           = m_comp->gtNewOperNode(GT_LT, TYP_INT, ilOffset, zero);
        GenTree* jtrue            = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, ltZero);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(LIR::SeqTree(m_comp, jtrue));
    }
}
