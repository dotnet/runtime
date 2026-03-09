// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SSA Web Splitting
//
// This phase runs after SSA construction. For each local in SSA form, it examines
// PHI nodes to determine which SSA definitions are connected — i.e., they flow into
// the same PHI. Connected defs belong to the same "web." If a local has multiple
// disjoint webs, this phase splits it into separate locals (one per web), which can
// improve register allocation and enable downstream optimizations.
//
// The phase makes exactly two walks over the IR:
//   Walk 1 — Build webs: scan PHI statements and union-find the connected SSA defs
//            for all locals simultaneously.
//   Walk 2 — Rewrite: update all local references and SSA numbers for all split
//            locals in a single pass.
//
// Between the walks, per-local analysis determines connected components, creates
// new locals, and builds SSA remap tables (no IR walk required).

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "compiler.h"

//------------------------------------------------------------------------
// DisjointSet: Simple union-find with path compression and union by rank.
//
// Element identifiers are unsigned integers in [0, count).
//
class DisjointSet
{
    unsigned* m_parent;
    unsigned* m_rank;
    unsigned  m_count;

public:
    DisjointSet(CompAllocator alloc, unsigned count)
        : m_count(count)
    {
        m_parent = alloc.allocate<unsigned>(count);
        m_rank   = alloc.allocate<unsigned>(count);
        for (unsigned i = 0; i < count; i++)
        {
            m_parent[i] = i;
            m_rank[i]   = 0;
        }
    }

    unsigned Find(unsigned x)
    {
        assert(x < m_count);
        while (m_parent[x] != x)
        {
            m_parent[x] = m_parent[m_parent[x]]; // path halving
            x           = m_parent[x];
        }
        return x;
    }

    void Union(unsigned x, unsigned y)
    {
        unsigned rx = Find(x);
        unsigned ry = Find(y);
        if (rx == ry)
        {
            return;
        }

        if (m_rank[rx] < m_rank[ry])
        {
            m_parent[rx] = ry;
        }
        else if (m_rank[rx] > m_rank[ry])
        {
            m_parent[ry] = rx;
        }
        else
        {
            m_parent[ry] = rx;
            m_rank[rx]++;
        }
    }
};

//------------------------------------------------------------------------
// SsaWebRemap: Per-local remap information produced by web analysis and
// consumed by the IR rewrite pass.
//
struct SsaWebRemap
{
    unsigned  ssaCount;   // number of SSA defs in the original local
    unsigned* targetLcl;  // [ssaIndex] -> target local number
    unsigned* targetSsa;  // [ssaIndex] -> target SSA number
};

//------------------------------------------------------------------------
// LivenessCopyPair: Records that a new split local's liveness bits should
// be copied from an original local's liveness bits.
//
struct LivenessCopyPair
{
    unsigned origVarIndex; // tracked index of the original local
    unsigned newVarIndex;  // tracked index of the new split local
};

//------------------------------------------------------------------------
// fgSsaWebSplit: Split locals whose SSA webs are disjoint into separate locals.
//
// Returns:
//   PhaseStatus indicating whether any IR was modified.
//
PhaseStatus Compiler::fgSsaWebSplit()
{
    CompAllocator alloc = getAllocator(CMK_SSA);

    // Capture lvaCount before we start adding new locals.
    unsigned const lclCountBefore = lvaCount;

    //------------------------------------------------------------------------
    // Step 1: Allocate a DisjointSet for every SSA local with >1 def.
    //------------------------------------------------------------------------

    DisjointSet** dsets = alloc.allocate<DisjointSet*>(lclCountBefore);
    memset(dsets, 0, lclCountBefore * sizeof(DisjointSet*));

    for (unsigned lclNum = 0; lclNum < lclCountBefore; lclNum++)
    {
        LclVarDsc* const varDsc = lvaGetDesc(lclNum);
        if (!varDsc->lvInSsa)
        {
            continue;
        }

        // Skip promoted struct fields — their SSA defs are stored as composite
        // SSA on the parent struct's store nodes, not as direct local references,
        // so our IR rewrite pass cannot find or update them.
        if (varDsc->lvIsStructField)
        {
            continue;
        }

        // Skip small-type locals that use normalize-on-load (params, OSR-exposed
        // locals, etc.). Split locals are temps that use normalize-on-store, and
        // mixing normalization modes causes type assertion failures in lowering.
        if (varTypeIsSmall(varDsc->TypeGet()) && varDsc->lvNormalizeOnLoad())
        {
            continue;
        }

        unsigned const ssaCount = varDsc->lvPerSsaData.GetCount();
        if (ssaCount <= 1)
        {
            continue;
        }

        dsets[lclNum] = new (alloc) DisjointSet(alloc, ssaCount);
    }

    //------------------------------------------------------------------------
    // Step 2 — Walk 1: Scan all blocks for PHI statements. For each PHI,
    // union the PHI def with each of its arguments. This processes all locals
    // at once, so we visit the IR only once.
    //------------------------------------------------------------------------

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTreeLclVar* const store  = stmt->GetRootNode()->AsLclVar();
            unsigned const       lclNum = store->GetLclNum();

            if (lclNum >= lclCountBefore)
            {
                continue;
            }

            DisjointSet* const dset = dsets[lclNum];
            if (dset == nullptr)
            {
                continue;
            }

            unsigned const phiDefSsaNum = store->GetSsaNum();
            if (phiDefSsaNum == SsaConfig::RESERVED_SSA_NUM)
            {
                continue;
            }

            unsigned const phiDefIndex = phiDefSsaNum - SsaConfig::FIRST_SSA_NUM;

            GenTreePhi* const phi = store->Data()->AsPhi();
            for (GenTreePhi::Use& use : phi->Uses())
            {
                GenTreePhiArg* const phiArg    = use.GetNode()->AsPhiArg();
                assert(phiArg->GetLclNum() == lclNum);
                unsigned const       argSsaNum = phiArg->GetSsaNum();
                if (argSsaNum == SsaConfig::RESERVED_SSA_NUM)
                {
                    continue;
                }

                unsigned const argIndex = argSsaNum - SsaConfig::FIRST_SSA_NUM;
                dset->Union(phiDefIndex, argIndex);
            }
        }
    }

    //------------------------------------------------------------------------
    // Step 3: For each local with a DisjointSet, also union partial defs
    // (these are available from SSA metadata, no IR walk needed). Then count
    // components, create new locals, and build remap tables.
    //
    // Process locals in order of ascending tracked index (lvVarIndex) so that
    // the most important locals (highest weighted ref count) are split first.
    // This matters because each split consumes VarSet capacity, and we want
    // high-value locals to benefit from splitting before capacity runs out.
    // Only process locals that existed when the phase started; newly added
    // split locals are not candidates for further splitting.
    //------------------------------------------------------------------------

    SsaWebRemap** remapTable = alloc.allocate<SsaWebRemap*>(lclCountBefore);
    memset(remapTable, 0, lclCountBefore * sizeof(SsaWebRemap*));
    bool madeChanges = false;

    // Liveness copy pairs — populated during step 3, applied during step 4.
    // The maximum number of new tracked locals is bounded by VarSet capacity.
    unsigned const       bitsPerSizeT   = (unsigned)(sizeof(size_t) * 8);
    unsigned const       varSetCapacity = lvaTrackedCountInSizeTUnits * bitsPerSizeT;
    unsigned const       maxNewTracked  = (varSetCapacity > lvaTrackedCount) ? (varSetCapacity - lvaTrackedCount) : 0;
    LivenessCopyPair*    livenessCopies = alloc.allocate<LivenessCopyPair>(maxNewTracked > 0 ? maxNewTracked : 1);
    unsigned             livenessCopyCount = 0;

    // Capture the tracked count before we start adding new tracked locals.
    unsigned const trackedCountBefore = lvaTrackedCount;

    for (unsigned trackedIdx = 0; trackedIdx < trackedCountBefore; trackedIdx++)
    {
        unsigned const      lclNum = lvaTrackedIndexToLclNum(trackedIdx);
        DisjointSet* const  dset   = (lclNum < lclCountBefore) ? dsets[lclNum] : nullptr;
        if (dset == nullptr)
        {
            continue;
        }

        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        unsigned const ssaCount = varDsc->lvPerSsaData.GetCount();

        // Union partial defs with the SSA def they read from.
        for (unsigned i = 0; i < ssaCount; i++)
        {
            unsigned const useDefSsaNum =
                varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM + i)->GetUseDefSsaNum();
            if (useDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
            {
                unsigned const useDefIndex = useDefSsaNum - SsaConfig::FIRST_SSA_NUM;
                if (useDefIndex < ssaCount)
                {
                    dset->Union(i, useDefIndex);
                }
            }
        }

        // Count distinct components and assign a component ID to each SSA def.
        // The component containing SSA def index 0 (the initial/entry def)
        // is assigned ID 0 so it stays on the original local.
        unsigned* componentId     = alloc.allocate<unsigned>(ssaCount);
        unsigned* rootToComponent = alloc.allocate<unsigned>(ssaCount);
        for (unsigned i = 0; i < ssaCount; i++)
        {
            rootToComponent[i] = UINT_MAX;
        }

        unsigned const rootOfFirst   = dset->Find(0);
        rootToComponent[rootOfFirst] = 0;
        componentId[0]               = 0;
        unsigned numComponents       = 1;

        for (unsigned i = 1; i < ssaCount; i++)
        {
            unsigned const root = dset->Find(i);
            if (rootToComponent[root] == UINT_MAX)
            {
                rootToComponent[root] = numComponents++;
            }
            componentId[i] = rootToComponent[root];
        }

        if (numComponents <= 1)
        {
            continue;
        }

        // Merge components that have no uses into another component. In pruned
        // SSA, FIRST_SSA_NUM (the initial/entry def) typically has no uses;
        // retbuf defs via LCL_ADDR may also have no direct SSA uses. There is
        // no benefit to giving a useless component its own local.
        bool merged = false;
        bool* componentHasUses = alloc.allocate<bool>(numComponents);
        memset(componentHasUses, 0, numComponents * sizeof(bool));
        for (unsigned i = 0; i < ssaCount; i++)
        {
            if (varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM + i)->GetNumUses() > 0)
            {
                componentHasUses[componentId[i]] = true;
            }
        }

        // Find a merge target — prefer component 0, otherwise pick the first
        // component that has uses. If no component has uses, use component 0.
        unsigned mergeTarget = 0;
        for (unsigned c = 0; c < numComponents; c++)
        {
            if (componentHasUses[c])
            {
                mergeTarget = c;
                break;
            }
        }

        for (unsigned i = 0; i < ssaCount; i++)
        {
            if (!componentHasUses[componentId[i]] && componentId[i] != mergeTarget)
            {
                componentId[i] = mergeTarget;
                merged         = true;
            }
        }

        if (merged)
        {
            // Re-count components after merging.
            numComponents = 0;
            for (unsigned i = 0; i < ssaCount; i++)
            {
                rootToComponent[i] = UINT_MAX;
            }
            for (unsigned i = 0; i < ssaCount; i++)
            {
                unsigned const cid = componentId[i];
                if (rootToComponent[cid] == UINT_MAX)
                {
                    rootToComponent[cid] = numComponents++;
                }
                componentId[i] = rootToComponent[cid];
            }

            if (numComponents <= 1)
            {
                continue;
            }
        }

        // Check VarSet capacity — we need room for numComponents-1 new tracked locals.
        unsigned const numNewTracked = numComponents - 1;
        if (lvaTrackedCount + numNewTracked > varSetCapacity)
        {
            JITDUMP("V%02u has %u disjoint SSA webs but insufficient VarSet capacity (%u + %u > %u) -- skipping\n",
                    lclNum, numComponents, lvaTrackedCount, numNewTracked, varSetCapacity);
            continue;
        }

        JITDUMP("V%02u has %u disjoint SSA webs -- splitting\n", lclNum, numComponents);

#ifdef DEBUG
        if (verbose)
        {
            for (unsigned c = 0; c < numComponents; c++)
            {
                JITDUMP("  Web %u: {", c);
                bool first = true;
                for (unsigned i = 0; i < ssaCount; i++)
                {
                    if (componentId[i] == c)
                    {
                        JITDUMP("%s%u", first ? "" : ", ", SsaConfig::FIRST_SSA_NUM + i);
                        first = false;
                    }
                }
                JITDUMP("}\n");
            }
        }
#endif

        madeChanges = true;

        // Ensure lvaTrackedToVarNum has room.
        if (lvaTrackedCount + numNewTracked > lvaTrackedToVarNumSize)
        {
            unsigned const  newSize = lvaTrackedCount + numNewTracked;
            unsigned* const newArr  = getAllocator(CMK_LvaTable).allocate<unsigned>(newSize);
            memcpy(newArr, lvaTrackedToVarNum, lvaTrackedToVarNumSize * sizeof(unsigned));
            lvaTrackedToVarNumSize = newSize;
            lvaTrackedToVarNum     = newArr;
        }

        // Create new locals for components 1..numComponents-1.
        unsigned* componentLclNum = alloc.allocate<unsigned>(numComponents);
        componentLclNum[0]        = lclNum;

        var_types const    lclType   = varDsc->TypeGet();
        ClassLayout* const lclLayout = varTypeIsStruct(lclType) ? varDsc->GetLayout() : nullptr;

        for (unsigned c = 1; c < numComponents; c++)
        {
            unsigned const newLclNum =
                lvaGrabTemp(false DEBUGARG(printfAlloc("SSA web split from V%02u (web %u)", lclNum, c)));

            // lvaGrabTemp may reallocate lvaTable, so refresh varDsc.
            varDsc = lvaGetDesc(lclNum);

            LclVarDsc* const newVarDsc = lvaGetDesc(newLclNum);
            newVarDsc->lvInSsa         = true;

            // Copy struct layout first (may override lvType), then set the
            // actual type from the original local — this preserves types
            // changed by earlier phases (e.g., mask conversion).
            if (lclLayout != nullptr)
            {
                lvaSetStruct(newLclNum, lclLayout, false);
            }
            newVarDsc->lvType = lclType;

            // Copy flags that downstream phases rely on for correctness.
            newVarDsc->lvHasLdAddrOp = varDsc->lvHasLdAddrOp;

            // Copy ref-type class handle info for GDV/devirtualization.
            if (lclType == TYP_REF && varDsc->lvClassHnd != NO_CLASS_HANDLE)
            {
                newVarDsc->lvClassHnd     = varDsc->lvClassHnd;
                newVarDsc->lvClassIsExact = varDsc->lvClassIsExact;
            }

            if (varDsc->lvDoNotEnregister)
            {
                lvaSetVarDoNotEnregister(newLclNum DEBUGARG(varDsc->GetDoNotEnregReason()));
            }

#ifdef DEBUG
            if (varDsc->IsDefinedViaAddress())
            {
                newVarDsc->SetDefinedViaAddress(true);
            }
#endif

            newVarDsc->lvTracked = 1;
            assert(lvaTrackedCount <= USHRT_MAX);
            newVarDsc->lvVarIndex               = static_cast<unsigned short>(lvaTrackedCount);
            lvaTrackedToVarNum[lvaTrackedCount] = newLclNum;
            lvaTrackedCount++;

            componentLclNum[c] = newLclNum;

            JITDUMP("  Web %u -> V%02u (tracked idx %u)\n", c, newLclNum, newVarDsc->lvVarIndex);
        }

        // Record liveness copy pairs — the actual liveness update is deferred
        // to step 4 so we don't need a separate block walk per split local.
        if (varDsc->lvTracked)
        {
            unsigned const origVarIndex = varDsc->lvVarIndex;
            for (unsigned c = 1; c < numComponents; c++)
            {
                assert(livenessCopyCount < maxNewTracked);
                livenessCopies[livenessCopyCount].origVarIndex = origVarIndex;
                livenessCopies[livenessCopyCount].newVarIndex  = lvaGetDesc(componentLclNum[c])->lvVarIndex;
                livenessCopyCount++;
            }
        }

        // Save m_useDefSsaNum before resetting SSA data.
        unsigned* oldUseDefSsaNum = alloc.allocate<unsigned>(ssaCount);
        for (unsigned i = 0; i < ssaCount; i++)
        {
            oldUseDefSsaNum[i] = varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM + i)->GetUseDefSsaNum();
        }

        // Reset SSA data on the original local.
        varDsc->lvPerSsaData.Reset();

        // For new locals (components 1+), allocate a dummy initial entry def at
        // FIRST_SSA_NUM. VN expects FIRST_SSA_NUM to be the initial/entry def
        // and will set its block to fgFirstBB; the SSA checker tolerates the
        // block mismatch for FIRST_SSA_NUM.
        for (unsigned c = 1; c < numComponents; c++)
        {
            LclVarDsc* const targetDsc = lvaGetDesc(componentLclNum[c]);
            targetDsc->lvPerSsaData.AllocSsaNum(alloc);
        }

        // Allocate fresh SSA defs for all components.
        unsigned* newSsaNum = alloc.allocate<unsigned>(ssaCount);
        for (unsigned i = 0; i < ssaCount; i++)
        {
            unsigned const   targetLcl = componentLclNum[componentId[i]];
            LclVarDsc* const targetDsc = lvaGetDesc(targetLcl);

            newSsaNum[i] = targetDsc->lvPerSsaData.AllocSsaNum(alloc);
        }

        // Remap m_useDefSsaNum for partial defs.
        for (unsigned i = 0; i < ssaCount; i++)
        {
            unsigned const oldUseDef = oldUseDefSsaNum[i];
            if (oldUseDef == SsaConfig::RESERVED_SSA_NUM)
            {
                continue;
            }

            unsigned const useDefIndex = oldUseDef - SsaConfig::FIRST_SSA_NUM;
            if (useDefIndex >= ssaCount)
            {
                continue;
            }

            unsigned const   targetLcl = componentLclNum[componentId[i]];
            LclVarDsc* const targetDsc = lvaGetDesc(targetLcl);
            LclSsaVarDsc* const ssaDef = targetDsc->GetPerSsaData(newSsaNum[i]);
            ssaDef->SetUseDefSsaNum(newSsaNum[useDefIndex]);
        }

        // Build per-local remap table consumed by the rewrite walk.
        SsaWebRemap* remap = alloc.allocate<SsaWebRemap>(1);
        remap->ssaCount    = ssaCount;
        remap->targetLcl   = alloc.allocate<unsigned>(ssaCount);
        remap->targetSsa   = alloc.allocate<unsigned>(ssaCount);

        for (unsigned i = 0; i < ssaCount; i++)
        {
            remap->targetLcl[i] = componentLclNum[componentId[i]];
            remap->targetSsa[i] = newSsaNum[i];
        }

        remapTable[lclNum] = remap;
    }

    if (!madeChanges)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    //------------------------------------------------------------------------
    // Step 4 — Walk 2: Rewrite all IR nodes for split locals in a single pass,
    // populate SSA def metadata (block, defNode, use counts) from the IR, and
    // copy liveness bits from original locals to their split locals.
    //------------------------------------------------------------------------

    for (BasicBlock* const block : Blocks())
    {
        // Copy liveness from original locals to new split locals. This is a
        // conservative overapproximation — the original's liveness is a superset
        // of any split local's liveness. Without this, downstream phases (e.g.,
        // IV opts) that check bbLiveIn would incorrectly see split locals as dead.
        for (unsigned i = 0; i < livenessCopyCount; i++)
        {
            if (VarSetOps::IsMember(this, block->bbLiveIn, livenessCopies[i].origVarIndex))
            {
                VarSetOps::AddElemD(this, block->bbLiveIn, livenessCopies[i].newVarIndex);
            }
            if (VarSetOps::IsMember(this, block->bbLiveOut, livenessCopies[i].origVarIndex))
            {
                VarSetOps::AddElemD(this, block->bbLiveOut, livenessCopies[i].newVarIndex);
            }
        }

        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                if (!tree->OperIsAnyLocal())
                {
                    continue;
                }

                GenTreeLclVarCommon* const lclNode = tree->AsLclVarCommon();
                unsigned const             lclNum  = lclNode->GetLclNum();

                if (lclNum >= lclCountBefore)
                {
                    continue;
                }

                SsaWebRemap* const remap = remapTable[lclNum];
                if (remap == nullptr)
                {
                    continue;
                }

                unsigned const ssaNum = lclNode->GetSsaNum();
                if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
                {
                    continue;
                }

                unsigned const ssaIndex = ssaNum - SsaConfig::FIRST_SSA_NUM;
                if (ssaIndex >= remap->ssaCount)
                {
                    continue;
                }

                unsigned const targetLcl = remap->targetLcl[ssaIndex];
                unsigned const targetSsa = remap->targetSsa[ssaIndex];

                // SetLclNum resets SSA info, so always set SSA number after.
                lclNode->SetLclNum(targetLcl);
                lclNode->SetSsaNum(targetSsa);

                LclVarDsc* const    targetDsc = lvaGetDesc(targetLcl);
                LclSsaVarDsc* const ssaDef    = targetDsc->GetPerSsaData(targetSsa);

                if (lclNode->OperIsLocalStore())
                {
                    ssaDef->SetBlock(block);
                    ssaDef->SetDefNode(lclNode);

                    // For partial defs (GTF_VAR_USEASG), the use-def SSA number
                    // represents a use of the prior SSA definition. Count it.
                    unsigned const useDefSsaNum = ssaDef->GetUseDefSsaNum();
                    if (useDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        LclSsaVarDsc* const useDefDsc = targetDsc->GetPerSsaData(useDefSsaNum);
                        useDefDsc->AddUse(block);
                    }
                }
                else if ((lclNode->gtFlags & GTF_VAR_DEF) != 0)
                {
                    // GT_LCL_ADDR defs (retbuf pattern) — set block but not
                    // defNode, since SetDefNode asserts OperIsLocalStore.
                    ssaDef->SetBlock(block);

                    // LCL_ADDR defs can also be partial defs with use-def SSA.
                    unsigned const useDefSsaNum = ssaDef->GetUseDefSsaNum();
                    if (useDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        LclSsaVarDsc* const useDefDsc = targetDsc->GetPerSsaData(useDefSsaNum);
                        useDefDsc->AddUse(block);
                    }
                }
                else if (tree->OperIs(GT_PHI_ARG))
                {
                    ssaDef->AddPhiUse(block);
                }
                else
                {
                    ssaDef->AddUse(block);
                }
            }
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}
