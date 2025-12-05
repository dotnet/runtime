// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "fgwasm.h"
#include "algorithm.h"

//------------------------------------------------------------------------
//  WasmSuccessorEnumerator: Construct an instance of the enumerator.
//
//  Arguments:
//     comp       - Compiler instance
//     block      - The block whose successors are to be iterated
//     useProfile - If true, determines the order of successors visited using profile data
//
WasmSuccessorEnumerator::WasmSuccessorEnumerator(Compiler* comp, BasicBlock* block, const bool useProfile /* = false */)
    : m_block(block)
{
    m_numSuccs = 0;
    FgWasm::VisitWasmSuccs(
        comp, block,
        [this](BasicBlock* succ) {
        if (m_numSuccs < ArrLen(m_successors))
        {
            m_successors[m_numSuccs] = succ;
        }

        m_numSuccs++;
        return BasicBlockVisit::Continue;
    },
        useProfile);

    if (m_numSuccs > ArrLen(m_successors))
    {
        m_pSuccessors = new (comp, CMK_WasmCfgLowering) BasicBlock*[m_numSuccs];

        unsigned numSuccs = 0;
        FgWasm::VisitWasmSuccs(
            comp, block,
            [this, &numSuccs](BasicBlock* succ) {
            assert(numSuccs < m_numSuccs);
            m_pSuccessors[numSuccs++] = succ;
            return BasicBlockVisit::Continue;
        },
            useProfile);

        assert(numSuccs == m_numSuccs);
    }
}

//------------------------------------------------------------------------
// WasmDfs: run depth first search for wasm control flow codegen
//
// Arguments:
//   hasBlocksOnlyReachableViaEH - [out] set to true if there are non-funclet
//     blocks only reachable via EH
//
// Returns:
//   dfs tree
//
// Notes:
//   Does not follow exceptional or "runtime" flow. Funclets are
//   all considered reachable and are disjoint regions in the tree.
//
//   Invalidates any existing DFS, because we use the numbering
//   slots on BasicBlocks.
//
FlowGraphDfsTree* FgWasm::WasmDfs(bool& hasBlocksOnlyReachableViaEH)
{
    Compiler* const comp = Comp();
    comp->fgInvalidateDfsTree();

    BasicBlock** postOrder = new (comp, CMK_WasmCfgLowering) BasicBlock*[comp->fgBBcount];
    bool         hasCycle  = false;

    auto visitPreorder = [](BasicBlock* block, unsigned preorderNum) {
        block->bbPreorderNum  = preorderNum;
        block->bbPostorderNum = UINT_MAX;
    };

    auto visitPostorder = [=](BasicBlock* block, unsigned postorderNum) {
        block->bbPostorderNum = postorderNum;
        assert(postorderNum < comp->fgBBcount);
        postOrder[postorderNum] = block;
    };

    auto visitEdge = [&hasCycle](BasicBlock* block, BasicBlock* succ) {
        // Check if block -> succ is a backedge, in which case the flow
        // graph has a cycle.
        if ((succ->bbPreorderNum <= block->bbPreorderNum) && (succ->bbPostorderNum == UINT_MAX))
        {
            hasCycle = true;
        }
    };

    jitstd::vector<BasicBlock*> entryBlocks(comp->getAllocator(CMK_WasmCfgLowering));

    // We can ignore OSR/genReturnBB "entries"
    //
    assert(comp->fgEntryBB == nullptr);
    assert(comp->fgGlobalMorphDone);

    JITDUMP("Determining Wasm DFS entry points\n");

    // All funclets are entries. For now we assume finallys are funclets.
    //
    for (EHblkDsc* const ehDsc : EHClauses(comp))
    {
        JITDUMP(FMT_BB " is handler entry\n", ehDsc->ebdHndBeg->bbNum);
        entryBlocks.push_back(ehDsc->ebdHndBeg);
        if (ehDsc->HasFilter())
        {
            JITDUMP(FMT_BB " is filter entry\n", ehDsc->ebdFilter->bbNum);
            entryBlocks.push_back(ehDsc->ebdFilter);
        }
    }

    // Also consider any non-funclet entry block that is only reachable by
    // EH as an entry. Eventually we'll have to introduce some Wasm-appropriate
    // way for control to reach these blocks, at which point we should make this
    // manifest (either as Wasm EH, or via explicit control flow).
    //
    hasBlocksOnlyReachableViaEH = false;

    for (BasicBlock* const block : comp->Blocks())
    {
        bool onlyHasEHPreds = true;
        bool hasPreds       = false;
        for (BasicBlock* const pred : block->PredBlocks())
        {
            hasPreds = true;

            if (pred->KindIs(BBJ_EHCATCHRET, BBJ_EHFILTERRET, BBJ_EHFAULTRET))
            {
                continue;
            }

            onlyHasEHPreds = false;
            break;
        }

        // Blocks with no preds...?
        //
        if (hasPreds && onlyHasEHPreds)
        {
            JITDUMP(FMT_BB " is only reachable via EH\n", block->bbNum);
            entryBlocks.push_back(block);
            hasBlocksOnlyReachableViaEH = true;
        }
    }

    // Main entry is an entry. Add it last so it ends up first in the RPO.
    //
    JITDUMP(FMT_BB " is method entry\n", comp->fgFirstBB->bbNum);
    entryBlocks.push_back(comp->fgFirstBB);

    JITDUMP("Running Wasm DFS\n");
    JITDUMP("Entry blocks: ");
    for (BasicBlock* const entry : entryBlocks)
    {
        JITDUMP(" " FMT_BB, entry->bbNum);
    }
    JITDUMP("\n");

    unsigned numBlocks =
        comp->fgRunDfs<WasmSuccessorEnumerator, decltype(visitPreorder), decltype(visitPostorder), decltype(visitEdge),
                       /* useProfile */ true>(visitPreorder, visitPostorder, visitEdge, entryBlocks);

    // Build the dfs and traits
    //
    FlowGraphDfsTree* dfsTree = new (comp, CMK_WasmCfgLowering)
        FlowGraphDfsTree(comp, postOrder, numBlocks, hasCycle, /* useProfile */ true, /* forWasm */ true);

    return dfsTree;
}

//------------------------------------------------------------------------
//  Scc: Strongly Connected Component (in a flow graph)
//
// Notes:
//   Includes "nested" Sccs that are Sccs fully within the extent of the
//   current Scc that do not include Scc entry nodes
//
class Scc
{
private:

    FgWasm*              m_fgWasm;
    Compiler*            m_comp;
    FlowGraphDfsTree*    m_dfsTree;
    BitVecTraits*        m_traits;
    BitVec               m_blocks;
    BitVec               m_entries;
    jitstd::vector<Scc*> m_nested;
    unsigned             m_numIrr;

    // lowest common ancestor try index + 1, or 0 if method region
    unsigned m_enclosingTryIndex;
    // lowest common ancestor handler index + 1, or 0 if method region
    unsigned m_enclosingHndIndex;
    // total weight of all entry blocks
    weight_t m_entryWeight;
    // scc number
    unsigned m_num;

public:

    // Factor out traits? Parent links?
    //
    Scc(FgWasm* fgWasm, BasicBlock* block)
        : m_fgWasm(fgWasm)
        , m_comp(fgWasm->Comp())
        , m_dfsTree(fgWasm->GetDfsTree())
        , m_traits(fgWasm->GetTraits())
        , m_blocks(BitVecOps::UninitVal())
        , m_entries(BitVecOps::UninitVal())
        , m_nested(fgWasm->Comp()->getAllocator(CMK_WasmSccTransform))
        , m_numIrr(0)
        , m_enclosingTryIndex(0)
        , m_enclosingHndIndex(0)
        , m_entryWeight(0)
        , m_num(fgWasm->GetNextSccNum())
    {
        m_blocks  = BitVecOps::MakeEmpty(m_traits);
        m_entries = BitVecOps::MakeEmpty(m_traits);
        Add(block);
    }

    void Add(BasicBlock* block)
    {
        BitVecOps::AddElemD(m_traits, m_blocks, block->bbPostorderNum);
    }

    void Finalize()
    {
        ComputeEntries();
        FindNested();
    }

    void ComputeEntries()
    {
        JITDUMP("Scc %u has %u blocks\n", m_num, BitVecOps::Count(m_traits, m_blocks));

        BitVecOps::Iter iterator(m_traits, m_blocks);
        unsigned int    poNum;
        bool            isFirstEntry = true;

        while (iterator.NextElem(&poNum))
        {
            BasicBlock* const block = m_dfsTree->GetPostOrder(poNum);

            // cfpt's cannot be scc entries
            //
            if (block->isBBCallFinallyPairTail())
            {
                continue;
            }

            bool isEntry = false;

            for (BasicBlock* pred : block->PredBlocks())
            {
                if (pred->KindIs(BBJ_EHCATCHRET, BBJ_EHFILTERRET, BBJ_EHFAULTRET))
                {
                    // Ignore EHCATCHRET preds (requires exceptional flow)
                    continue;
                }

                if (BitVecOps::IsMember(m_traits, m_blocks, pred->bbPostorderNum))
                {
                    // Pred is in the scc, so not an entry edge
                    continue;
                }

                if (BitVecOps::TryAddElemD(m_traits, m_entries, block->bbPostorderNum))
                {
                    JITDUMP(FMT_BB " is scc %u entry via " FMT_BB "\n", block->bbNum, m_num, pred->bbNum);
                    isEntry = true;

                    m_entryWeight += block->bbWeight;

                    if (isFirstEntry)
                    {
                        m_enclosingTryIndex = block->bbTryIndex;
                        m_enclosingHndIndex = block->bbHndIndex;
                        isFirstEntry        = false;
                    }
                    else
                    {
                        // We expect all Scc headers to be in the same handler region
                        assert(m_enclosingHndIndex == block->bbHndIndex);

                        // But possibly different try regions
                        m_enclosingTryIndex = m_comp->bbFindInnermostCommonTryRegion(m_enclosingTryIndex, block);
                    }
                }
            }
        }

        JITDUMPEXEC(Dump());
    }

    unsigned NumEntries()
    {
        return BitVecOps::Count(m_traits, m_entries);
    }

    unsigned NumBlocks()
    {
        return BitVecOps::Count(m_traits, m_blocks);
    }

    BitVec InternalBlocks()
    {
        return BitVecOps::Diff(m_traits, m_blocks, m_entries);
    }

    bool IsIrr()
    {
        return NumEntries() > 1;
    }

    unsigned NumIrr()
    {
        m_numIrr = IsIrr();

        for (Scc* nested : m_nested)
        {
            m_numIrr += nested->NumIrr();
        }

        return m_numIrr;
    }

    // Weight of all the flow into entry blocks
    //
    weight_t TotalEntryWeight()
    {
        return m_entryWeight;
    }

    void Dump(int indent = 0)
    {
        BitVecOps::Iter iterator(m_traits, m_blocks);
        unsigned int    poNum;
        bool            first = true;
        while (iterator.NextElem(&poNum))
        {
            if (first)
            {
                JITDUMP("%*c", indent, ' ');

                if (NumEntries() > 1)
                {
                    JITDUMP("[irrd (%u)] ", NumBlocks());
                }
                else
                {
                    JITDUMP("[loop (%u)] ", NumBlocks());
                }
            }
            else
            {
                JITDUMP(", ");
            }
            first = false;

            BasicBlock* const block   = m_dfsTree->GetPostOrder(poNum);
            bool              isEntry = BitVecOps::IsMember(m_traits, m_entries, poNum);
            JITDUMP(FMT_BB "%s", block->bbNum, isEntry ? "e" : "");
        }
        JITDUMP("\n");
    }

    void DumpDot()
    {
        JITDUMP("digraph SCC_%u {\n", m_num);
        BitVecOps::Iter iterator(m_traits, m_blocks);
        unsigned int    poNum;
        bool            first = true;
        while (iterator.NextElem(&poNum))
        {
            BasicBlock* const block   = m_dfsTree->GetPostOrder(poNum);
            bool              isEntry = BitVecOps::IsMember(m_traits, m_entries, poNum);

            JITDUMP(FMT_BB "%s;", block->bbNum, isEntry ? " [style=filled]" : "");

            // Show entry edges
            //
            if (isEntry)
            {
                for (BasicBlock* pred : block->PredBlocks())
                {
                    if (pred->KindIs(BBJ_EHCATCHRET, BBJ_EHFILTERRET, BBJ_EHFAULTRET))
                    {
                        // Ignore EHCATCHRET preds (requires exceptional flow)
                        continue;
                    }

                    if (BitVecOps::IsMember(m_traits, m_blocks, pred->bbPostorderNum))
                    {
                        // Pred is in the scc, so not an entry edge
                        continue;
                    }

                    JITDUMP(FMT_BB " -> " FMT_BB ";\n", pred->bbNum, block->bbNum);
                }
            }

            WasmSuccessorEnumerator successors(m_comp, block, /* useProfile */ true);
            for (BasicBlock* const succ : successors)
            {
                JITDUMP(FMT_BB " -> " FMT_BB ";\n", block->bbNum, succ->bbNum);
            }
        }
        JITDUMP("}\n");
    }

    void DumpAll(int indent = 0)
    {
        Dump(indent);

        for (Scc* child : m_nested)
        {
            child->DumpAll(indent + 3);
        }
    }

    void FindNested()
    {
        unsigned entryCount = BitVecOps::Count(m_traits, m_entries);
        assert(entryCount > 0);

        BitVec   nestedBlocks = InternalBlocks();
        unsigned nestedCount  = BitVecOps::Count(m_traits, nestedBlocks);

        // Only entries
        if (nestedCount == 0) // < 2...?
        {
            return;
        }

        JITDUMP("Scc %u  has %u non-entry blocks. Scc Graph:\n", m_num, nestedCount);
        DumpDot();
        JITDUMP("\nLooking for nested SCCs in SCC %u\n", m_num);

        // Build a new postorder for the nested blocks
        //
        BasicBlock** postOrder = new (m_comp, CMK_WasmSccTransform) BasicBlock*[nestedCount];

        auto visitPreorder = [](BasicBlock* block, unsigned preorderNum) {};

        auto visitPostorder = [&postOrder](BasicBlock* block, unsigned postorderNum) {
            postOrder[postorderNum] = block;
        };

        auto visitEdge = [](BasicBlock* block, BasicBlock* succ) {};

#ifdef DEBUG
        // Dump subgraph as dot
        //
        if (m_comp->verbose)
        {
            JITDUMP("digraph scc_%u_nested_subgraph%u {\n", m_num, nestedCount);
            BitVecOps::Iter iterator(m_traits, nestedBlocks);
            unsigned int    poNum;
            bool            first = true;
            while (iterator.NextElem(&poNum))
            {
                BasicBlock* const block = m_dfsTree->GetPostOrder(poNum);

                JITDUMP(FMT_BB ";\n", block->bbNum);

                WasmSuccessorEnumerator successors(m_comp, block, /* useProfile */ true);
                for (BasicBlock* const succ : successors)
                {
                    JITDUMP(FMT_BB " -> " FMT_BB ";\n", block->bbNum, succ->bbNum);
                }
            }

            JITDUMP("}\n");
        }
#endif

        unsigned numBlocks =
            m_fgWasm->WasmRunSubgraphDfs<decltype(visitPreorder), decltype(visitPostorder), decltype(visitEdge),
                                         /* useProfile */ true>(visitPreorder, visitPostorder, visitEdge, nestedBlocks);

        if (numBlocks != nestedCount)
        {
            JITDUMP("Eh? numBlocks %u nestedCount %u\n", numBlocks, nestedCount);
        }
        assert(numBlocks == nestedCount);

        // Use that to find the nested Sccs
        //
        ArrayStack<Scc*> nestedSccs(m_comp->getAllocator(CMK_WasmSccTransform));
        m_fgWasm->WasmFindSccsCore(nestedBlocks, nestedSccs, postOrder, nestedCount);

        const unsigned nNested = nestedSccs.Height();

        if (nNested == 0)
        {
            return;
        }

        for (unsigned i = 0; i < nNested; i++)
        {
            Scc* const nestedScc = nestedSccs.Bottom(i);
            m_nested.push_back(nestedScc);
        }

        JITDUMP("\n <-- nested in Scc %u... \n", m_num);
    }

    unsigned EnclosingTryIndex()
    {
        return m_enclosingTryIndex;
    }

    unsigned EnclosingHndIndex()
    {
        return m_enclosingHndIndex;
    }

    //-----------------------------------------------------------------------------
    // TransformViaSwitchDispatch: modify Scc into a reducible loop
    //
    // Notes:
    //
    //   A multi-entry Scc is modified as follows:
    //   * each Scc header block (header) is given an integer index (header number)
    //   * a new BBJ_SWITCH header block (dispatcher) is created
    //   * a new control local (controlVarNum) is allocated.
    //   * each flow edge that targets one of the headers is split(**)
    //   * In the split block the control var is assigned the target header's number
    //   * Flow from the split block is modified to flow to the dispatcher
    //   * The switch in the dispatcher transfers control to the headers based on on the control var value.
    //
    //   ** if we have an edge pred->header such that pred has no other successors
    //      we hoist the assingnment into pred.
    //
    //   TODO: if the source of an edge to a header is dominated by that header,
    //   the edge can be left as is. (requires dominators)
    //
    bool TransformViaSwitchDispatch()
    {
        bool           modified   = false;
        const unsigned numHeaders = NumEntries();

        if (numHeaders > 1)
        {
            JITDUMP("Transforming Scc via switch dispatch: ");
            JITDUMPEXEC(Dump());

            // We're making changes
            //
            modified = true;

            // Split edges, rewire flow, and add control var assignments
            //
            const unsigned   controlVarNum = m_comp->lvaGrabTemp(/* shortLifetime */ false DEBUGARG("Scc control var"));
            LclVarDsc* const controlVarDsc = m_comp->lvaGetDesc(controlVarNum);
            controlVarDsc->lvType          = TYP_INT;
            BasicBlock*      dispatcher    = nullptr;
            FlowEdge** const succs         = new (m_comp, CMK_FlowEdge) FlowEdge*[numHeaders];
            FlowEdge** const cases         = new (m_comp, CMK_FlowEdge) FlowEdge*[numHeaders];
            unsigned         headerNumber  = 0;
            BitVecOps::Iter  iterator(m_traits, m_entries);
            unsigned int     poHeaderNumber = 0;
            weight_t         netLikelihood  = 0.0;

            while (iterator.NextElem(&poHeaderNumber))
            {
                BasicBlock* const header = m_dfsTree->GetPostOrder(poHeaderNumber);
                if (dispatcher == nullptr)
                {
                    if ((EnclosingTryIndex() > 0) || (EnclosingHndIndex() > 0))
                    {
                        const bool inTry = ((EnclosingTryIndex() != 0) && (EnclosingHndIndex() == 0)) ||
                                           (EnclosingTryIndex() < EnclosingHndIndex());
                        if (inTry)
                        {
                            JITDUMP("Dispatch header needs to go in try of EH#%02u ...\n", EnclosingTryIndex() - 1);
                        }
                        else
                        {
                            JITDUMP("Dispatch header needs to go in handler of EH#%02u ...\n", EnclosingHndIndex() - 1);
                        }
                    }
                    else
                    {
                        JITDUMP("Dispatch header needs to go in method region\n");
                    }
                    dispatcher = m_comp->fgNewBBinRegion(BBJ_SWITCH, EnclosingTryIndex(), EnclosingHndIndex(),
                                                         /* nearBlk */ nullptr);
                    dispatcher->setBBProfileWeight(TotalEntryWeight());
                }

                JITDUMP("Fixing flow for preds of header " FMT_BB "\n", header->bbNum);
                weight_t headerWeight = header->bbWeight;

                for (FlowEdge* const f : header->PredEdgesEditing())
                {
                    assert(f->getDestinationBlock() == header);
                    BasicBlock* const pred          = f->getSourceBlock();
                    BasicBlock*       transferBlock = nullptr;

                    // Note we can actually sink the control var store into pred if
                    // pred does not also have some other SCC header as a successor.
                    // The assignment may end up partially dead, but likely avoiding a branch
                    // is preferable; the assignment should be cheap.
                    //
                    // For now we just check if the pred has only this header as successor.
                    // We also don't putcode into BBJ_CALLFINALLYRET (note that restriction
                    // is perhaps no longer needed).
                    //
                    if (pred->HasTarget() && (pred->GetTarget() == header) && !pred->isBBCallFinallyPairTail())
                    {
                        // Note this handles BBJ_EHCATCHRET which is the only expected case
                        // of an unsplittable pred edge.
                        //
                        transferBlock = pred;
                    }
                    else
                    {
                        assert(!pred->KindIs(BBJ_EHCATCHRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_EHFINALLYRET));
                        transferBlock = m_comp->fgSplitEdge(pred, header);
                    }

                    GenTree* const   targetIndex     = m_comp->gtNewIconNode(headerNumber);
                    GenTree* const   storeControlVar = m_comp->gtNewStoreLclVarNode(controlVarNum, targetIndex);
                    Statement* const assignStmt      = m_comp->fgNewStmtNearEnd(transferBlock, storeControlVar);

                    m_comp->gtSetStmtInfo(assignStmt);
                    m_comp->fgSetStmtSeq(assignStmt);

                    m_comp->fgReplaceJumpTarget(transferBlock, header, dispatcher);
                }

                FlowEdge* const dispatchToHeaderEdge = m_comp->fgAddRefPred(header, dispatcher);

                // Since all flow to header now goes through dispatch, we know the likelihood
                // of the dispatch targets. If all profile data is zero just divide evenly.
                //
                if ((headerNumber + 1) == numHeaders)
                {
                    dispatchToHeaderEdge->setLikelihood(max(0.0, (1.0 - netLikelihood)));
                }
                else if (TotalEntryWeight() > 0)
                {
                    dispatchToHeaderEdge->setLikelihood(headerWeight / TotalEntryWeight());
                }
                else
                {
                    dispatchToHeaderEdge->setLikelihood(1.0 / numHeaders);
                }

                netLikelihood += dispatchToHeaderEdge->getLikelihood();

                succs[headerNumber] = dispatchToHeaderEdge;
                cases[headerNumber] = dispatchToHeaderEdge;

                headerNumber++;
            }

            // Create the dispatch switch... really there should be no default but for now we'll have one.
            //
            JITDUMP("Dispatch header is " FMT_BB "; %u cases\n", dispatcher->bbNum, numHeaders);
            BBswtDesc* const swtDesc =
                new (m_comp, CMK_BasicBlock) BBswtDesc(succs, numHeaders, cases, numHeaders, true);
            dispatcher->SetSwitch(swtDesc);

            GenTree* const   controlVar = m_comp->gtNewLclvNode(controlVarNum, TYP_INT);
            GenTree* const   switchNode = m_comp->gtNewOperNode(GT_SWITCH, TYP_VOID, controlVar);
            Statement* const switchStmt = m_comp->fgNewStmtAtEnd(dispatcher, switchNode);

            m_comp->gtSetStmtInfo(switchStmt);
            m_comp->fgSetStmtSeq(switchStmt);
        }

        // Handle nested Sccs
        //
        for (Scc* const nested : m_nested)
        {
            modified |= nested->TransformViaSwitchDispatch();
        }

        return modified;
    }
};

//-----------------------------------------------------------------------------
// WasmFindSccs: find strongly connected components in the flow graph
//
// Arguments:
//   sccs [out] -- top level Sccs in the flow graph
//
// Returns:
//   true if the flow graph was modified
//
void FgWasm::WasmFindSccs(ArrayStack<Scc*>& sccs)
{
    assert(m_dfsTree->IsForWasm());
    BitVec allBlocks = BitVecOps::MakeFull(&m_traits);
    WasmFindSccsCore(allBlocks, sccs, m_dfsTree->GetPostOrder(), m_dfsTree->GetPostOrderCount());
    unsigned numIrreducible = 0;

    if (sccs.Height() > 0)
    {
        JITDUMP("\n*** Sccs\n");

        for (int i = 0; i < sccs.Height(); i++)
        {
            Scc* const scc = sccs.Bottom(i);
            scc->DumpAll();

            numIrreducible += scc->NumIrr();
        }
    }
    else
    {
        JITDUMP("\n*** No Sccs\n");
    }

    if (numIrreducible > 0)
    {
        JITDUMP("\n*** %u total Irreducible!\n", numIrreducible);
    }
}

//-----------------------------------------------------------------------------
// WasmFindSccsCore: find strongly connected components in a subgraph
//
// Arguments:
//   subset  - bv describing the subgraph
//   sccs    - [out] collection of Sccs found in this subgraph
//   postorder - array of BasicBlock* in postorder
//   postorderCount - size of the array
//
// Notes:
//   Uses Kosaraju's algorithm: we walk the reverse graph starting
//   from each block in reverse postorder.
//
void FgWasm::WasmFindSccsCore(BitVec& subset, ArrayStack<Scc*>& sccs, BasicBlock** postorder, unsigned postorderCount)
{
    SccMap              map(Comp()->getAllocator(CMK_WasmSccTransform));
    BitVecTraits* const traits = GetTraits();

    // TODO: if we had a BV iter that worked from highest set
    // bit to lowest, we could iterate the subset directly
    // and avoid searching here.
    //
    for (unsigned i = 0; i < postorderCount; i++)
    {
        unsigned const    rpoNum = postorderCount - i - 1;
        BasicBlock* const block  = postorder[rpoNum];

        if (!BitVecOps::IsMember(traits, subset, block->bbPostorderNum))
        {
            continue;
        }

        AssignBlockToScc(block, block, subset, sccs, map);
    }

    for (int i = 0; i < sccs.Height(); i++)
    {
        Scc* const scc = sccs.Bottom(i);
        scc->Finalize();
    }
}

//-----------------------------------------------------------------------------
// AssignBlockToScc: assign block to an SCC, then recursively assign its predecessors
//
// Arguments:
//   block   - block to assign
//   root    - root block of the SCC
//   subset  - bv describing the subgraph
//   sccs    - [out] collection of Sccs found in this subgraph
//   map     - map from block to SCC
//
// Notes:
//   Most blocks won't be in an Scc. So initially we map a block to a null entry in the map.
//   If we then get a second block in that Scc, we allocate an Scc instance and add both blocks.
//
//   This does a reverse-graph walk. Consider expressing this non-recursively.
//
void FgWasm::AssignBlockToScc(BasicBlock* block, BasicBlock* root, BitVec& subset, ArrayStack<Scc*>& sccs, SccMap& map)
{
    BitVecTraits* const traits = GetTraits();

    // Ignore blocks not in the subset
    //
    // This might be too restrictive. Consider
    //
    // X -> A; X -> B;
    // A -> B; A -> C;
    // B -> A; B -> B;
    //
    // We find the A,B Scc. Its non-header subset is empty.
    //
    // Thus we fail to find the B self loop "nested" inside.
    //
    // Might need to be: "ignore in-edges from outside the set, or from
    // non-dominated edges in the set...?" So we'd ignore A->B and B->A,
    // but not B->B.
    //
    // However I think we still find all nested Sccs, since those cannot
    // share a header with the outer Scc?
    //
    if (!BitVecOps::IsMember(traits, subset, block->bbPostorderNum))
    {
        return;
    }

    // If we've assigned u an scc, no more work needed.
    //
    if (map.Lookup(block))
    {
        return;
    }

    JITDUMP("Scc-reverse graph: visiting " FMT_BB " with root " FMT_BB "\n", block->bbNum, root->bbNum);

    // Else see if there's an Scc for root
    //
    Scc* scc   = nullptr;
    bool found = map.Lookup(root, &scc);

    if (found)
    {
        assert(block != root);

        if (scc == nullptr)
        {
            // We haven't yet created an SCC object. Now's the time.
            //
            JITDUMP("Root has been visited; forming SCC with root " FMT_BB "\n", root->bbNum);
            scc = new (Comp(), CMK_WasmSccTransform) Scc(this, root);
            map.Set(root, scc, SccMap::SetKind::Overwrite);
            sccs.Push(scc);
        }

        JITDUMP("Adding " FMT_BB " to SCC with root " FMT_BB "\n", block->bbNum, root->bbNum);
        scc->Add(block);
    }

    // Indicate we've visited the block
    //
    map.Set(block, scc);

    // Walk block's preds looking for more Scc members
    //
    // Do not walk back out of a handler
    //
    if (Comp()->bbIsHandlerBeg(block))
    {
        return;
    }

    // Do not walk back into a finally,
    // instead skip to the call finally.
    //
    if (block->isBBCallFinallyPairTail())
    {
        AssignBlockToScc(block->Prev(), root, subset, sccs, map);
        return;
    }

    // Else walk preds...
    //
    for (BasicBlock* const pred : block->PredBlocks())
    {
        // Do not walk back into a catch or filter.
        //
        if (pred->KindIs(BBJ_EHCATCHRET, BBJ_EHFILTERRET, BBJ_EHFAULTRET))
        {
            continue;
        }

        JITDUMP("Scc-reverse graph: walking back from " FMT_BB " to " FMT_BB ", with root " FMT_BB "\n", block->bbNum,
                pred->bbNum, root->bbNum);

        AssignBlockToScc(pred, root, subset, sccs, map);
    }
}

//-----------------------------------------------------------------------------
// WasmTransformSccs: transform SCCs into reducible flow
//
// Arguments:
//   sccs - SCCs to transform
//
// Returns:
//   true if flow was modified (sccs was not empty)
//
// Notes:
//   Currently recurses to handle "nested" sccs before. Might be more sensible
//   to have a flat list of SCCs. If so we should transform these as outer to
//   inner.
//
bool FgWasm::WasmTransformSccs(ArrayStack<Scc*>& sccs)
{
    bool modified = false;

    for (int i = 0; i < sccs.Height(); i++)
    {
        Scc* const scc = sccs.Bottom(i);
        modified |= scc->TransformViaSwitchDispatch();
    }

    return modified;
}

//-----------------------------------------------------------------------------
// fgWasmTransformSccs: transform SCCs into reducible flow
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::fgWasmTransformSccs()
{
    FgWasm                  fgWasm(this);
    bool                    hasBlocksOnlyReachableViaEH = false;
    FlowGraphDfsTree* const dfsTree                     = fgWasm.WasmDfs(hasBlocksOnlyReachableViaEH);
    fgWasm.SetDfsAndTraits(dfsTree);

    if (hasBlocksOnlyReachableViaEH)
    {
        JITDUMP("\nThere are blocks only reachable via EH, bailing out for now\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    FlowGraphNaturalLoops* const loops       = FlowGraphNaturalLoops::Find(dfsTree);
    bool                         transformed = false;

    // If there are irreducible loops, transform them.
    //
    if (loops->ImproperLoopHeaders() > 0)
    {
        JITDUMP("\nThere are irreducible loops.\n");
        ArrayStack<Scc*> sccs(getAllocator(CMK_WasmSccTransform));
        fgWasm.WasmFindSccs(sccs);
        assert(!sccs.Empty());
        transformed = fgWasm.WasmTransformSccs(sccs);
        assert(transformed);

#ifdef DEBUG
        // Rebuild DFS and loops; verify no improper headers remain.
        // We should not have altered the EH reachability of any block.
        //
        bool              hasBlocksOnlyReachableViaEH2 = false;
        FlowGraphDfsTree* dfsTree2                     = fgWasm.WasmDfs(hasBlocksOnlyReachableViaEH2);
        assert(!hasBlocksOnlyReachableViaEH2);
        FlowGraphNaturalLoops* loops2 = FlowGraphNaturalLoops::Find(dfsTree2);
        assert(loops2->ImproperLoopHeaders() == 0);
#endif
    }

    return transformed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgWasmControlFlow: determine how to emit control flow instructions for wasm
//
// Notes:
//
// Wasm Control Flow: naive algorithm (no if/else)
//
// * We consider only normal flow here, so eg callfinally just proceeds to the callfinally ret
// * Funclets have been identified and separated (though this is not strictly required). With
//   suitable RPO we can model funclet flow disjointly from main method flow
// * A prior pass has removed all irreducible flow.
//
// First we build a (normal flow) loop aware RPO.
//
// Each loop creates a Wasm LOOP/END. Since all loops are reducible and the body is compact the entry
// is the first lexical block and the extent is the lexically last block. The only back-edges are loop back edges.
//
// Each non-contiguous forward branch potentially creates a block. The only trick is figuring out how to
// arrange the block begins so we have proper nesting of Wasm blocks and Wasm loops.
//
// Since we have linear order of basic blocks, each non-contiguous forward branch can be characterized
// by the source and destination basic block indices in the order. Eg [0, 4]. So an interval begins at
// the start of the first block and ends at the start of the second.
//
// Each basic block start may be the end of some loops and /or a block. Or both. Note multiple
// blocks that end at the same point are not necessary.
//
// We walk the LaRPO from front to back.
// * If we see a loop head, we record a loop interval [x,y]. This extent cannot be altered.
// * If we see a noncontiguous branch (or switch), we record a block interval [a,b]. Here
//   b must remain fixed but we can increase a as needed to accomplish nesting.
//   For switches we will create multiple [a,b0], [a, b1]...
//
// If a forward branch targets a block that already has an interval ending at that block, we do
// not need a new interval for the branch. Because we're walking front to back, we will have already
// recorded an interval that starts earlier.
//
// We then scan the intervals in non-decreasing start order, lookin for earlier intervals that contain
// the start of the current interval but not the end. When we find one, the start of the current interval
// will need to decrease so the earlier interval can nest inside. That is, if we have a:[0, 4] and b:[2,6] we
// will need to decrease the start of b to match a and then reorder, and emit them as b:[0,6], a[0,4].
//
// To save some time we also create a union-find like setup to identify the first interval in a set of
// conflicting intervals. Say we have a:[0,4] b:[2,6] c:[5,7]. When we see that b conflicts with a,
// we note 'a' as the conflict "chain" for b, and also track the conflict extent in a. Then when
// we scan intervals for c, we see it conflicts with the chain starting at a, and we add it to the chain.
// The net result is a:[0,4(7)], b:[2,6]-->a, c:[5,7]-->a.
//
// Then we order on their conflict chain start and end extent, and so would emit c:[0,7], b:[0,6], a:[0,4]
//
// We then can use the properly ordered and nested intervals to track the control stack depth as we
// traverse the blocks in loop-aware RPO order, and emit the proper Wasm control flow.
//
// In what follows we co-opt the bbPreorderNum slot of each block to instead hold the index of the
// block in the loop-aware RPO.
//
// Still TODO
// * Blocks only reachable via EH
// * proper handling of BR_TABLE defaults
// * tail calls (RETURN_CALL)
// * Rethink need for BB0 (have m_end refer to end of last block in range, not start of first block after)
// * During LaRPO formation, remember the position of the last block in the loop
// * Compatibility of LaRPO with try region layout constraints (if any)
//
PhaseStatus Compiler::fgWasmControlFlow()
{
    // -----------------------------------------------
    // (1) Build loop-aware RPO layout
    //
    // We don't install our DFS tree as "the" DFS tree as it is non-standard.
    //
    FgWasm            fgWasm(this);
    bool              hasBlocksOnlyReachableViaEH = false;
    FlowGraphDfsTree* dfsTree                     = fgWasm.WasmDfs(hasBlocksOnlyReachableViaEH);

    if (hasBlocksOnlyReachableViaEH)
    {
        JITDUMP("\nThere are blocks only reachable via EH, bailing out for now\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    assert(dfsTree->IsForWasm());
    FlowGraphNaturalLoops* loops = FlowGraphNaturalLoops::Find(dfsTree);

    // We should have transformed these away earlier
    //
    assert(loops->ImproperLoopHeaders() == 0);

    // Our interval ends are at the starts of blocks, so we need a block that
    // comes after all existing blocks. So allocate one extra slot.
    //
    const unsigned dfsCount = dfsTree->GetPostOrderCount();
    JITDUMP("\nCreating loop-aware RPO (%u blocks)\n", dfsCount);

    BasicBlock** const initialLayout = new (this, CMK_WasmCfgLowering) BasicBlock*[dfsCount + 1];

    // Note this DFS includes funclets, they should each be contiguous and appear after
    // the main method in the order.
    //
    unsigned numBlocks     = 0;
    auto     addToSequence = [initialLayout, &numBlocks](BasicBlock* block) {
        JITDUMP("%03u " FMT_BB "\n", numBlocks, block->bbNum);
        // Set the block's ordinal.
        block->bbPreorderNum       = numBlocks;
        initialLayout[numBlocks++] = block;
    };

    fgVisitBlocksInLoopAwareRPO(dfsTree, loops, addToSequence);
    assert(numBlocks == dfsCount);

    // Splice in a fake BB0
    //
    BasicBlock bb0;
    INDEBUG(bb0.bbNum = 0;);
    bb0.bbPreorderNum        = numBlocks;
    bb0.bbPostorderNum       = dfsTree->GetPostOrderCount();
    initialLayout[numBlocks] = &bb0;

    // -----------------------------------------------
    // (2) Build the intervals
    //
    // Allocate interval and scratch vectors. We'll use the scratch vector to keep track of
    // block intervals that end at a certain point.
    //
    fgWasmIntervals = new (this, CMK_WasmCfgLowering) jitstd::vector<WasmInterval*>(getAllocator(CMK_WasmCfgLowering));
    jitstd::vector<WasmInterval*> scratch(numBlocks, nullptr, getAllocator(CMK_WasmCfgLowering));

    for (unsigned int cursor = 0; cursor < numBlocks; cursor++)
    {
        BasicBlock* const block = initialLayout[cursor];

        // See if we entered any loops
        //
        FlowGraphNaturalLoop* const loop = loops->GetLoopByHeader(block);

        if (loop != nullptr)
        {
            // Find the loop's lexical extent given our ordering
            // (maybe memoize this during loop finding...)
            //
            // Note that cursor may end up pointing at BB0
            //
            unsigned endCursor = cursor;
            while ((endCursor < numBlocks) && loop->ContainsBlock(initialLayout[endCursor]))
            {
                endCursor++;
            }

            WasmInterval* const loopInterval = WasmInterval::NewLoop(this, block, initialLayout[endCursor]);

            // We assume here that a block is only the header of one loop.
            //
            fgWasmIntervals->push_back(loopInterval);
        }

        // Now see where block branches to...
        //
        WasmSuccessorEnumerator successors(this, block, /* useProfile */ true);
        for (BasicBlock* const succ : successors)
        {
            unsigned const succNum = succ->bbPreorderNum;

            // We ignore back edges; they don't inspire blocks.
            //
            if (succNum <= cursor)
            {
                JITDUMP("Backedge " FMT_BB "[%u] -> " FMT_BB "[%u]\n", block->bbNum, cursor, succ->bbNum, succNum);

                // The backedge target should be a loop header.
                // (TODO: scan loop stack to verify the loop is on the stack?)
                //
                // Note we currently bail out way above if there are any irreducible loops.
                //
                assert(loops->GetLoopByHeader(succ) != nullptr);
                continue;
            }

            // Branch to next needs no block, unless this is a switch
            // (eventually when we leave the default on the switch we can remove this).
            //
            if ((succNum == (cursor + 1)) && !block->KindIs(BBJ_SWITCH))
            {
                continue;
            }

            // Branch to cold block needs no block (presumably something EH related).
            // Eventually we need to case these out and handle them better.
            //
            if (succNum >= numBlocks)
            {
                continue;
            }

            // See if we already have a block that ends at this point and starts before.
            //
            WasmInterval* const existingBlock = scratch[succNum];

            if (existingBlock != nullptr)
            {
                // If so we don't need to track this branch.
                //
                JITDUMP("Subsumed " FMT_BB "[%u] -> " FMT_BB "[%u]\n", block->bbNum, cursor, succ->bbNum, succNum);
                assert(existingBlock->Start() <= cursor);
                continue;
            }

            // Non-contiguous, non-subsumed forward branch
            //
            WasmInterval* const branch = WasmInterval::NewBlock(this, block, initialLayout[succNum]);
            fgWasmIntervals->push_back(branch);

            // Remember an interval end here
            //
            scratch[succNum] = branch;
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        // Display the raw intervals...
        //
        JITDUMP("\n-------------- Initial set of wasm intervals\n");
        for (WasmInterval* interval : *fgWasmIntervals)
        {
            JITDUMPEXEC(interval->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // -----------------------------------------------
    // (3) Find intervals that overlap
    //
    // See if this interval conflicts with any other. If so,
    // add the interval to that intervals conflict set, and return
    // the conflict set for further resolution.
    //
    // Since this is only looking at prior intervals it could be
    // merged with (2) above.
    //
    auto resolve = [this](WasmInterval* const current) {
        for (WasmInterval* prior : *fgWasmIntervals)
        {
            // We only need to consider intervals that start at the same point or earlier.
            //
            if (prior == current)
            {
                break;
            }

            // We should be walking in non-decreasing start order
            //
            assert(prior->Start() <= current->Start());

            // We may have chained this previous interval to another even earlier.
            // Find the head of that chain.
            //
            WasmInterval* const priorChain = prior->FetchAndUpdateChain();
            assert(priorChain->Start() <= current->Start());

            // See if the current interval starts at or inside
            // the chain interval and ends outside.
            //
            if ((current->Start() < priorChain->ChainEnd()) && (current->End() > priorChain->ChainEnd()))
            {
                current->SetChain(priorChain);
                break;
            }

            // See if the current interval starts at or inside
            // the prior interval and ends outside.
            //
            if ((current->Start() < prior->End()) && (current->End() > prior->End()))
            {
                // Note we chain to the chain interval, not the prior interval
                //
                // Say we have [0,3] [1,4] [2,6] [3,5].
                //
                // Examining [1,4], we see a conflict with [0,3], and so we chain [1,4] to [0,3].
                //  (and the "chain end of [0,3] is now [0,4])
                // Examining [2,6], we see a conflict with [0,3], and so we chain [2,6] to [0,3].
                //  (and the "chain end of [0,3] is now [0,6])
                //
                // When examining [3,5] we don't see a conflict with [0,6] or [0,3].
                // But there is a conflict with [1,4], which is chained to [0,3]
                // so we chain [3,5] to [0,3] instead of to [1,4].
                //
                // And after sorting we then emit [0,6] [0,5] [0,4] [0,3]
                //
                current->SetChain(priorChain);
                break;
            }
        }
    };

    for (WasmInterval* interval : *fgWasmIntervals)
    {
        resolve(interval);
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\n-------------- After finding conflicts\n");
        for (WasmInterval* iv : *fgWasmIntervals)
        {
            JITDUMPEXEC(iv->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // (4) Sort to put intervals in proper nesting order
    //
    // Sort by chain start index (ascending) then actual end index (descending) then isLoop
    //
    auto comesBefore = [](WasmInterval* i1, WasmInterval* i2) {
        WasmInterval* const chain1 = i1->Chain();
        WasmInterval* const chain2 = i2->Chain();

        // Lowest chain start
        //
        if (chain1->Start() < chain2->Start())
        {
            return true;
        }

        if (chain2->Start() < chain1->Start())
        {
            return false;
        }

        // Highest end
        //
        if (i1->End() > i2->End())
        {
            return true;
        }

        if (i2->End() > i1->End())
        {
            return false;
        }

        // Tiebreaker
        //
        if (i1->IsLoop())
        {
            return true;
        }

        return false;
    };

    jitstd::sort(fgWasmIntervals->begin(), fgWasmIntervals->end(), comesBefore);

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\n-------------- After sorting\n");
        for (WasmInterval* interval : *fgWasmIntervals)
        {
            JITDUMPEXEC(interval->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // (5) Reorder the blocks
    //
    // Todo: verify this ordering is compatible with our EH story.
    //
    // If we use an explicit EH state marker, likely we do not need
    // to keep the try region contiguous.
    //
    // If we need contiguous trys we can likely create a try-aware
    // loop-aware RPO, since both regions will be single-entry.
    // The main trick is to figure out both the extent of the try region
    // and if there is a block that is both try entry and loop header,
    // which is nested in which.
    //
    JITDUMP("Reordering block list\n");
    BasicBlock* lastBlock = nullptr;

    for (unsigned int cursor = 0; cursor < numBlocks; cursor++)
    {
        BasicBlock* const block = initialLayout[cursor];

        if (cursor == 0)
        {
            assert(block == fgFirstBB);
            lastBlock = block;
        }
        else
        {
            fgUnlinkBlock(block);
            fgInsertBBafter(lastBlock, block);
            lastBlock = block;
        }

        // If BBJ_COND true target is branch to next,
        // reverse the condition
        //
        if (block->KindIs(BBJ_COND))
        {
            const unsigned trueNum  = block->GetTrueTarget()->bbPreorderNum;
            const unsigned falseNum = block->GetFalseTarget()->bbPreorderNum;

            // We don't expect degenerate BBJ_COND
            //
            assert(trueNum != falseNum);

            // If the true target is the next block, reverse the branch
            //
            const bool reverseCondition = trueNum == (cursor + 1);

            if (reverseCondition)
            {
                JITDUMP("Reversing condition in " FMT_BB " to allow fall through to " FMT_BB "\n", block->bbNum,
                        block->GetTrueTarget()->bbNum);

                GenTree* const test = block->GetLastLIRNode();
                assert(test->OperIs(GT_JTRUE));
                {
                    GenTree* const cond = gtReverseCond(test->AsOp()->gtOp1);
                    // Ensure `gtReverseCond` did not create a new node.
                    assert(cond == test->AsOp()->gtOp1);
                    test->AsOp()->gtOp1 = cond;
                }

                // Rewire the flow
                //
                std::swap(block->TrueEdgeRef(), block->FalseEdgeRef());
            }
            else
            {
                JITDUMP("NOT Reversing condition in " FMT_BB "\n", block->bbNum);
            }
        }
    }

    JITDUMPEXEC(fgDumpWasmControlFlow());
    JITDUMPEXEC(fgDumpWasmControlFlowDot());

    return PhaseStatus::MODIFIED_EVERYTHING;
}

#ifdef DEBUG

//------------------------------------------------------------------------
//  fgDumpWasmControlFlow: show (roughly) what the WASM control flow looks like
//
//  Notes:
//    Assumes blocks have been reordered
//
void Compiler::fgDumpWasmControlFlow()
{
    if (!verbose)
    {
        return;
    }

    ArrayStack<WasmInterval*> activeIntervals(getAllocator(CMK_WasmCfgLowering));
    unsigned                  wasmCursor = 0;

    for (BasicBlock* const block : Blocks())
    {
        unsigned const cursor = block->bbPreorderNum;
        JITDUMP("Before " FMT_BB " at %u stack is:", block->bbNum, cursor);

        if (activeIntervals.Empty())
        {
            JITDUMP("empty");
        }
        else
        {
            for (int i = 0; i < activeIntervals.Height(); i++)
            {
                JITDUMP(" [%u,%u]", activeIntervals.Top(i)->Start(), activeIntervals.Top(i)->End());
            }
        }
        JITDUMP("\n");

        // Close intervals that end here (at most two, block and/or loop)
        //
        while (!activeIntervals.Empty() && (activeIntervals.Top()->End() == cursor))
        {
            JITDUMP("END    (%u)%s\n", activeIntervals.Top()->End(), activeIntervals.Top()->IsLoop() ? " LOOP" : "");
            activeIntervals.Pop();
        }

        // Open intervals that start here or earlier
        //
        if (wasmCursor < fgWasmIntervals->size())
        {
            WasmInterval* interval = fgWasmIntervals->at(wasmCursor);
            WasmInterval* chain    = interval->Chain();

            while (chain->Start() <= cursor)
            {
                JITDUMP("%s (%u)\n", interval->IsLoop() ? "LOOP " : "BLOCK", interval->End());

                wasmCursor++;
                activeIntervals.Push(interval);

                if (wasmCursor >= fgWasmIntervals->size())
                {
                    break;
                }

                interval = fgWasmIntervals->at(wasmCursor);
                chain    = interval->Chain();
            }
        }

        JITDUMP("  " FMT_BB "\n", block->bbNum);

        // Compute the depth of the block ending at targetNum
        // or (if isBackedge) the loop starting at targetNum
        //
        auto findDepth = [&activeIntervals](unsigned targetNum, bool isBackedge, unsigned& match) {
            int const h = activeIntervals.Height();

            for (int i = 0; i < h; i++)
            {
                WasmInterval* const ii = activeIntervals.Top(i);
                match                  = 0;

                if (isBackedge)
                {
                    // loops bind to start
                    match = ii->Start();
                }
                else
                {
                    // blocks bind to end
                    match = ii->End();
                }

                if ((match == targetNum) && (isBackedge == ii->IsLoop()))
                {
                    return i;
                }
            }

            JITDUMP("Could not find %u%s in active control stack\n", targetNum, isBackedge ? " (backedge)" : "");
            assert(!"Can't find target in control stack");

            return ~0;
        };

        // This somewhat duplicates the logic in WasmSuccessorEnumerator.
        //
        switch (block->GetKind())
        {
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
            case BBJ_EHFILTERRET:
            case BBJ_EHCATCHRET:
            {
                JITDUMP("RETURN\n");
                break;
            }

            case BBJ_THROW:
            {
                JITDUMP("THROW\n");
                break;
            }

            case BBJ_CALLFINALLY:
            {
                // no-op (implied fall through to tail, if it exists)
                //
                if (!block->isBBCallFinallyPair())
                {
                    JITDUMP("UNREACHED\n");
                }
                break;
            }

            case BBJ_ALWAYS:
            case BBJ_CALLFINALLYRET:
            {
                unsigned const succNum = block->GetTarget()->bbPreorderNum;

                if (succNum == (cursor + 1))
                {
                    JITDUMP("FALLTHROUGH\n");
                }
                else
                {
                    bool const isBackedge = succNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(succNum, isBackedge, blockNum);
                    JITDUMP("BR %d (%u)%s\n", depth, blockNum, isBackedge ? "be" : "");
                }

                break;
            }

            case BBJ_COND:
            {
                const unsigned trueNum  = block->GetTrueTarget()->bbPreorderNum;
                const unsigned falseNum = block->GetFalseTarget()->bbPreorderNum;

                if (trueNum == falseNum)
                {
                    JITDUMP("FALLTHROUGH\n");
                    break;
                }

                // If the true target is the next block, we are in a bind, since
                // we need to branch to it, but may not have induced a block.
                //
                // We could anticipate this above and induce a block like we do for switches.
                //
                // Or we can just reverse the branch condition here; I think this should be viable.
                // (eg invoke the core part of optOptimizePostLayout).
                //
                const bool reverseCondition = trueNum == (cursor + 1);

                if (reverseCondition)
                {
                    JITDUMP("FALLTHROUGH-inv\n");
                }
                else
                {
                    bool const isBackedge = trueNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(trueNum, isBackedge, blockNum);
                    JITDUMP("BR_IF %d (%u)%s\n", depth, blockNum, isBackedge ? "be" : "");
                }

                if (falseNum == (cursor + 1))
                {
                    JITDUMP("FALLTHROUGH\n");
                }
                else
                {
                    bool const isBackedge = falseNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(falseNum, isBackedge, blockNum);
                    JITDUMP("BR%s %d (%u)%s\n", reverseCondition ? "_IF-inv" : "", depth, blockNum,
                            isBackedge ? "be" : "");
                }

                break;
            }

            case BBJ_SWITCH:
            {
                BBswtDesc* const desc      = block->GetSwitchTargets();
                unsigned const   caseCount = desc->GetCaseCount();

                // BR_TABLE supports a default case, so we need to ensure
                // that wasm lower does not remove it.
                //
                // For now, we expect non-wasm lower has made the default case check explicit
                // and so our BR_TABLE emission is deficient.
                //
                assert(!desc->HasDefaultCase());

                if (caseCount == 0)
                {
                    JITDUMP("FALLTHROUGH\n");
                    break;
                }

                JITDUMP("BR_TABLE");

                for (unsigned caseNum = 0; caseNum < caseCount; caseNum++)
                {
                    BasicBlock* const caseTarget    = desc->GetCase(caseNum)->getDestinationBlock();
                    unsigned const    caseTargetNum = caseTarget->bbPreorderNum;

                    bool const isBackedge = caseTargetNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(caseTargetNum, isBackedge, blockNum);
                    JITDUMP("%s %d (%u)%s", caseNum > 0 ? "," : "", depth, blockNum, isBackedge ? "be" : "");
                }

                JITDUMP("\n");
                break;
            }

            default:
            {
                assert(!"Unexpected block kind");
            }
        }

        JITDUMP("\n");
    }

    // We should have closed out all intervals unless there are loops
    // that end at the end of the method.
    //
    while (!activeIntervals.Empty())
    {
        WasmInterval* const i = activeIntervals.Pop();
        JITDUMP("END    (%u)%s\n", i->End(), i->IsLoop() ? " LOOP" : "");
    }
}

//------------------------------------------------------------------------
//  fgDumpWasmControlFlowDot: show (roughly) what the WASM control flow looks like
//    using dot markup
//
void Compiler::fgDumpWasmControlFlowDot()
{
    if (!verbose)
    {
        return;
    }

    ArrayStack<WasmInterval*> activeIntervals(getAllocator(CMK_WasmCfgLowering));
    unsigned                  wasmCursor = 0;
    JITDUMP("\ndigraph WASM {\n");

    for (BasicBlock* const block : Blocks())
    {
        unsigned const cursor = block->bbPreorderNum;

        // Close intervals that end here (at most two, block and/or loop)
        //
        while (!activeIntervals.Empty() && (activeIntervals.Top()->End() == cursor))
        {
            JITDUMP("  }\n");
            activeIntervals.Pop();
        }

        // Open intervals that start here
        //
        if (wasmCursor < fgWasmIntervals->size())
        {
            WasmInterval* interval = fgWasmIntervals->at(wasmCursor);
            WasmInterval* chain    = interval->Chain();

            while (chain->Start() <= cursor)
            {
                JITDUMP("  subgraph cluster_%u_%u%s {\n", chain->Start(), interval->End(),
                        interval->IsLoop() ? "_loop" : "");

                if (interval->IsLoop())
                {
                    JITDUMP("    color=red;\n");
                }
                else
                {
                    JITDUMP("    color=black;\n");
                }

                wasmCursor++;
                activeIntervals.Push(interval);

                if (wasmCursor >= fgWasmIntervals->size())
                {
                    break;
                }

                interval = fgWasmIntervals->at(wasmCursor);
                chain    = interval->Chain();
            }
        }

        JITDUMP("    " FMT_BB ";\n", block->bbNum);
    }

    // Close remaining intervals
    //
    while (!activeIntervals.Empty())
    {
        activeIntervals.Pop();
        JITDUMP("  }\n");
    }

    // Now list all the branches
    //
    for (BasicBlock* const block : Blocks())
    {
        if (block->KindIs(BBJ_CALLFINALLY))
        {
            if (block->isBBCallFinallyPair())
            {
                JITDUMP("   " FMT_BB " -> " FMT_BB " [style=dotted];\n", block->bbNum, block->Next()->bbNum);
            }
        }
        else
        {
            for (BasicBlock* const succ : block->Succs())
            {
                JITDUMP("   " FMT_BB " -> " FMT_BB ";\n", block->bbNum, succ->bbNum);
            }
        }
    }

    JITDUMP("}\n");
}

#endif // DEBUG
