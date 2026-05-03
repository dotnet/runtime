// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =============================================================================================
// Promote allocations inside class constructors (.cctor) that are stored into
// `static readonly` fields to the frozen object heap.
//
// Algorithm (run after PHASE_OPTIMIZE_INDEX_CHECKS; SSA + VN are still live):
//
//   1. Bail unless this is a cctor and the VM has set CORJIT_FLAG_FROZEN_ALLOC_ALLOWED.
//   2. Build a fresh DFS + natural-loops view; bail if any cycles -- frozen objects
//      are never collected, so we must prove each promoted alloc executes at most once.
//   3. Walk every block/statement/tree. For each `STORE_IND` whose address resolves
//      to a non-shared `static readonly` field of the cctor's own class:
//        - Peel `GT_BOX` and trivial commas.
//        - If the value is a direct allocator helper call, capture it.
//        - Else if it's a single-SSA-def `LCL_VAR`, follow one SSA edge and capture
//          the call from the defining `STORE_LCL_VAR`.
//        - Otherwise (or on multi-store to the same field) block the field.
//   4. Rewrite each surviving allocation to use the corresponding `*_MAYBEFROZEN`
//      helper, preserving args, side-effect flags, and VN where possible.
//
// Bail-outs for type-specific cases (arrays of refs with len > 0, GC-pointer-containing
// structs, custom alignment, oversized objects) are deferred to the VM allocator, which
// falls back to the regular GC heap when frozen allocation is impossible.
// =============================================================================================

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

namespace
{
bool IsPromotableObjectAllocHelper(CorInfoHelpFunc helper)
{
    switch (helper)
    {
        case CORINFO_HELP_NEWFAST:
        case CORINFO_HELP_NEWSFAST:
        case CORINFO_HELP_NEWSFAST_ALIGN8:
        case CORINFO_HELP_NEWSFAST_ALIGN8_VC:
#ifdef FEATURE_READYTORUN
        case CORINFO_HELP_READYTORUN_NEW:
#endif
            return true;
        default:
            return false;
    }
}

bool IsPromotableArrayAllocHelper(CorInfoHelpFunc helper)
{
    switch (helper)
    {
        case CORINFO_HELP_NEWARR_1_DIRECT:
        case CORINFO_HELP_NEWARR_1_PTR:
        case CORINFO_HELP_NEWARR_1_VC:
        case CORINFO_HELP_NEWARR_1_ALIGN8:
#ifdef FEATURE_READYTORUN
        case CORINFO_HELP_READYTORUN_NEWARR_1:
#endif
            return true;
        default:
            return false;
    }
}

struct AllocLocation
{
    GenTreeCall* call  = nullptr;
    BasicBlock*  block = nullptr;
};

// Recognise an allocator helper call on the value side of an stsfld store.
// Peels GT_BOX, trivial commas (via gtEffectiveVal), and follows a single SSA
// edge from a LCL_VAR to its defining STORE_LCL_VAR.
AllocLocation TryGetAllocCallFromStsfldValue(Compiler* compiler, BasicBlock* stsfldBlock, GenTree* value)
{
    AllocLocation result;

    // box int + stsfld is imported as STORE_IND(field_addr, GT_BOX(LCL_VAR(boxTemp))).
    if (value->OperIs(GT_BOX))
    {
        value = value->AsBox()->BoxOp();
    }

    value = value->gtEffectiveVal();

    if (value->IsCall())
    {
        result.call  = value->AsCall();
        result.block = stsfldBlock;
        return result;
    }

    if (!value->OperIs(GT_LCL_VAR))
    {
        return result;
    }

    GenTreeLclVar* lcl    = value->AsLclVar();
    unsigned       ssaNum = lcl->GetSsaNum();
    if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return result;
    }

    LclVarDsc* lclDsc = compiler->lvaGetDesc(lcl);
    if (!lclDsc->lvInSsa)
    {
        return result;
    }

    LclSsaVarDsc* ssaDef = lclDsc->GetPerSsaData(ssaNum);
    if (ssaDef == nullptr)
    {
        return result;
    }

    GenTreeLclVarCommon* defNode = ssaDef->GetDefNode();
    if ((defNode == nullptr) || !defNode->OperIs(GT_STORE_LCL_VAR))
    {
        return result;
    }

    GenTree* defValue = defNode->AsLclVarCommon()->Data();
    if (defValue == nullptr)
    {
        return result;
    }

    defValue = defValue->gtEffectiveVal();
    if (!defValue->IsCall())
    {
        return result;
    }

    result.call  = defValue->AsCall();
    result.block = ssaDef->GetBlock();
    return result;
}

// Returns the field handle if `store` writes to the start of a non-shared static
// readonly field that the importer pre-registered for this cctor; otherwise NO_FIELD_HANDLE.
CORINFO_FIELD_HANDLE GetStaticReadonlyFieldFromStoreInd(Compiler* compiler, GenTreeIndir* store)
{
    if (compiler->m_cctorFinalStaticFields == nullptr)
    {
        return NO_FIELD_HANDLE;
    }

    GenTree*  baseAddr = nullptr;
    FieldSeq* fldSeq   = nullptr;
    ssize_t   offset   = 0;
    if (!store->Addr()->IsFieldAddr(compiler, &baseAddr, &fldSeq, &offset))
    {
        return NO_FIELD_HANDLE;
    }

    if ((fldSeq == nullptr) || !fldSeq->IsStaticField() || fldSeq->IsSharedStaticField() || (offset != 0))
    {
        return NO_FIELD_HANDLE;
    }

    CORINFO_FIELD_HANDLE fld = fldSeq->GetFieldHandle();
    bool                 dummy;
    if ((fld == NO_FIELD_HANDLE) || !compiler->m_cctorFinalStaticFields->Lookup(fld, &dummy))
    {
        return NO_FIELD_HANDLE;
    }

    return fld;
}

// allocCall == nullptr means "blocked" (multi-store or non-promotable store).
struct FieldPromoteInfo
{
    GenTreeCall* allocCall    = nullptr;
    BasicBlock*  allocBlock   = nullptr;
    bool         isArrayAlloc = false;
};

typedef JitHashTable<CORINFO_FIELD_HANDLE, JitPtrKeyFuncs<CORINFO_FIELD_STRUCT_>, FieldPromoteInfo> FieldMap;

void BlockField(FieldMap& map, CORINFO_FIELD_HANDLE fld)
{
    map.Set(fld, FieldPromoteInfo{}, FieldMap::Overwrite);
}
} // anonymous namespace

//------------------------------------------------------------------------
// fgPromoteCctorAllocsToFrozenHeap: Promote stsfld allocations to the frozen
// object heap. See file header for the algorithm.
//
PhaseStatus Compiler::fgPromoteCctorAllocsToFrozenHeap()
{
    assert((info.compFlags & FLG_CCTOR) == FLG_CCTOR);
    assert(opts.jitFlags->IsSet(JitFlags::JIT_FLAG_FROZEN_ALLOC_ALLOWED));
    assert(opts.OptimizationEnabled());

    if (eeIsSharedInst(info.compClassHnd))
    {
        JITDUMP("cctor on shared generic instance -- bailing out\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Bail entirely if the method has any cycles. Frozen allocations can't be
    // collected, so we must prove each promoted alloc executes at most once.
    FlowGraphDfsTree*      dfsTree = fgComputeDfs();
    FlowGraphNaturalLoops* loops   = FlowGraphNaturalLoops::Find(dfsTree);
    if ((loops->NumLoops() > 0) || (loops->ImproperLoopHeaders() > 0))
    {
        JITDUMP("cctor has loops/irreducible cycles -- bailing out\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    FieldMap candidates(getAllocator(CMK_Generic));

    // Pass 1: collect candidates.
    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                if (!tree->OperIs(GT_STOREIND) || !tree->TypeIs(TYP_REF))
                {
                    continue;
                }

                GenTreeIndir*        store = tree->AsIndir();
                CORINFO_FIELD_HANDLE fld   = GetStaticReadonlyFieldFromStoreInd(this, store);
                if (fld == NO_FIELD_HANDLE)
                {
                    continue;
                }

                // If we've already seen any store to this field, block it.
                if (candidates.LookupPointer(fld) != nullptr)
                {
                    BlockField(candidates, fld);
                    continue;
                }

                AllocLocation loc   = TryGetAllocCallFromStsfldValue(this, block, store->Data());
                GenTreeCall*  alloc = loc.call;
                if ((alloc == nullptr) || !alloc->IsHelperCall())
                {
                    BlockField(candidates, fld);
                    continue;
                }

                CorInfoHelpFunc helperNum = alloc->GetHelperNum();
                bool            isArr     = IsPromotableArrayAllocHelper(helperNum);
                bool            isObj     = IsPromotableObjectAllocHelper(helperNum);
                if (!isArr && !isObj)
                {
                    BlockField(candidates, fld);
                    continue;
                }

                // We need the class handle to rebuild the call (R2R only). The
                // importer/ObjectAllocator save it on every supported helper call.
                if (alloc->compileTimeHelperArgumentHandle == nullptr)
                {
                    BlockField(candidates, fld);
                    continue;
                }

                FieldPromoteInfo cand;
                cand.allocCall    = alloc;
                cand.allocBlock   = loc.block;
                cand.isArrayAlloc = isArr;
                candidates.Set(fld, cand);
            }
        }
    }

    // Pass 2: rewrite surviving candidates.
    //
    // For helpers whose call shape already matches MAYBEFROZEN (plain NEWFAST*,
    // NEWARR_1_*) we mutate `gtCallMethHnd` in-place; this preserves ABI/SSA/VN.
    //
    // For READYTORUN_NEW/READYTORUN_NEWARR_1 the type handle is in an R2R cell
    // rather than a user arg, so we rebuild the call with the (typeHandle [, length])
    // shape that MAYBEFROZEN expects and re-run `fgMorphArgs`.
    bool madeChanges = false;
    for (FieldMap::Node* const node : FieldMap::KeyValueIteration(&candidates))
    {
        const FieldPromoteInfo& candidate = node->GetValue();
        if (candidate.allocCall == nullptr)
        {
            continue;
        }

        GenTreeCall*         origCall = candidate.allocCall;
        CORINFO_CLASS_HANDLE clsHnd   = (CORINFO_CLASS_HANDLE)origCall->compileTimeHelperArgumentHandle;
        assert(clsHnd != NO_CLASS_HANDLE);

        // Mirror impTokenToHandle(..., mustRestoreHandle=true) so the class is
        // loaded before this code runs in AOT scenarios.
        info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(clsHnd);

        CorInfoHelpFunc origHelper = origCall->GetHelperNum();
        CorInfoHelpFunc newHelper =
            candidate.isArrayAlloc ? CORINFO_HELP_NEWARR_1_MAYBEFROZEN : CORINFO_HELP_NEWFAST_MAYBEFROZEN;

        bool needsRebuild = false;
#ifdef FEATURE_READYTORUN
        needsRebuild = (origHelper == CORINFO_HELP_READYTORUN_NEW) || (origHelper == CORINFO_HELP_READYTORUN_NEWARR_1);
#endif

        if (!needsRebuild)
        {
            origCall->gtCallMethHnd = eeFindHelper(newHelper);
        }
        else
        {
            // Locate the original call's containing statement; the alloc may
            // live in a different statement (and even block) than the stsfld.
            Statement* allocStmt = nullptr;
            for (Statement* const stmt : candidate.allocBlock->Statements())
            {
                if (gtFindLink(stmt, origCall).result != nullptr)
                {
                    allocStmt = stmt;
                    break;
                }
            }
            assert(allocStmt != nullptr);

            GenTree* lengthTree = nullptr;
            if (candidate.isArrayAlloc)
            {
                assert(origCall->gtArgs.CountUserArgs() == 1);
                lengthTree = origCall->gtArgs.GetUserArgByIndex(0)->GetNode();
            }
            else
            {
                assert(origCall->gtArgs.CountUserArgs() == 0);
            }

            GenTree*     mtTree  = gtNewIconEmbClsHndNode(clsHnd);
            GenTreeCall* newCall = candidate.isArrayAlloc
                                       ? gtNewHelperCallNode(newHelper, TYP_REF, mtTree, lengthTree)
                                       : gtNewHelperCallNode(newHelper, TYP_REF, mtTree);

            newCall->compileTimeHelperArgumentHandle = (CORINFO_GENERIC_HANDLE)clsHnd;
            if ((origCall->gtCallMoreFlags & GTF_CALL_M_ALLOC_SIDE_EFFECTS) != 0)
            {
                newCall->gtCallMoreFlags |= GTF_CALL_M_ALLOC_SIDE_EFFECTS;
            }
            // Same semantics as the original (returns a fresh ref of class T) -
            // reuse the VN so downstream consumers stay coherent.
            newCall->gtVNPair = origCall->gtVNPair;

            FindLinkData link = gtFindLink(allocStmt, origCall);
            assert(link.result != nullptr);
            *link.result = newCall;

            // fgMorphArgs determines ABI info and inserts PUTARG nodes that
            // lowering expects on the freshly-built call.
            fgMorphArgs(newCall);

            gtUpdateStmtSideEffects(allocStmt);
            fgSetStmtSeq(allocStmt);
        }

        madeChanges = true;

        JITDUMP("Promoted cctor allocation [%06u] for field %p (%s) to frozen-heap helper\n",
                dspTreeID(origCall), dspPtr((void*)node->GetKey()), candidate.isArrayAlloc ? "array" : "object");
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
