// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code to optimize induction variables in loops based on
// scalar evolution analysis (see scev.h and scev.cpp for more information
// about the scalar evolution analysis).
//
// Currently the following optimizations are done:
//
// IV widening:
//   This widens primary induction variables from 32 bits into 64 bits. This is
//   generally only profitable on x64 that does not allow zero extension of
//   32-bit values in addressing modes (in contrast, arm64 does have the
//   capability of including zero extensions in addressing modes). For x64 this
//   saves a zero extension for every array access inside the loop, in exchange
//   for some widening or narrowing stores outside the loop:
//     - To make sure the new widened IV starts at the right value it is
//     initialized to the value of the narrow IV outside the loop (either in
//     the preheader or at the def location of the narrow IV). Usually the
//     start value is a constant, in which case the widened IV is just
//     initialized to the constant value.
//     - If the narrow IV is used after the loop we need to store it back from
//     the widened IV in the exits. We depend on liveness sets to figure out
//     which exits to insert IR into.
//
//   These steps ensure that the wide IV has the right value to begin with and
//   the old narrow IV still has the right value after the loop. Additionally,
//   we must replace every use of the narrow IV inside the loop with the widened
//   IV. This is done by a traversal of the IR inside the loop. We do not
//   actually widen the uses of the IV; rather, we keep all uses and defs as
//   32-bit, which the backend is able to handle efficiently on x64. Because of
//   this we do not need to worry about overflow.
//
// Loop reversing:
//   This converts loops that are up-counted into loops that are down-counted.
//   Down-counted loops can generally do their IV update and compare in a
//   single instruction, bypassing the need to do a separate comparison with a
//   bound.
//
// Strength reduction (disabled):
//   This changes the stride of primary IVs in a loop to avoid more expensive
//   multiplications inside the loop. Commonly the primary IVs are only used
//   for indexing memory at some element size, which can end up with these
//   multiplications.
//
//   Strength reduction frequently relies on reversing the loop to remove the
//   last non-multiplied use of the primary IV.
//

#include "jitpch.h"
#include "scev.h"

// Data structure that keeps track of local occurrences inside loops.
class LoopLocalOccurrences
{
    struct Occurrence
    {
        BasicBlock*          Block;
        struct Statement*    Statement;
        GenTreeLclVarCommon* Node;
        Occurrence*          Next;
    };

    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, Occurrence*> LocalToOccurrenceMap;

    FlowGraphNaturalLoops* m_loops;
    // For every loop, we track all occurrences exclusive to that loop.
    // Occurrences in descendant loops are not kept in their ancestor's maps.
    LocalToOccurrenceMap** m_maps;
    // Blocks whose IR we have visited to find local occurrences in.
    BitVec m_visitedBlocks;

    LocalToOccurrenceMap* GetOrCreateMap(FlowGraphNaturalLoop* loop);

    template <typename TFunc>
    bool VisitLoopNestMaps(FlowGraphNaturalLoop* loop, TFunc& func);
public:
    LoopLocalOccurrences(FlowGraphNaturalLoops* loops);

    template <typename TFunc>
    bool VisitOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum, TFunc func);

    bool HasAnyOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum);

    template <typename TFunc>
    bool VisitStatementsWithOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum, TFunc func);

    void Invalidate(FlowGraphNaturalLoop* loop);
};

LoopLocalOccurrences::LoopLocalOccurrences(FlowGraphNaturalLoops* loops)
    : m_loops(loops)
{
    Compiler* comp = loops->GetDfsTree()->GetCompiler();
    m_maps = loops->NumLoops() == 0 ? nullptr : new (comp, CMK_LoopOpt) LocalToOccurrenceMap* [loops->NumLoops()] {};
    BitVecTraits poTraits = loops->GetDfsTree()->PostOrderTraits();
    m_visitedBlocks       = BitVecOps::MakeEmpty(&poTraits);
}

//------------------------------------------------------------------------------
// LoopLocalOccurrences:GetOrCreateMap:
//   Get or create the map of occurrences exclusive to a single loop.
//
// Parameters:
//   loop - The loop
//
// Returns:
//   Map of occurrences.
//
// Remarks:
//   As a precondition occurrences of all descendant loops must already have
//   been found.
//
LoopLocalOccurrences::LocalToOccurrenceMap* LoopLocalOccurrences::GetOrCreateMap(FlowGraphNaturalLoop* loop)
{
    LocalToOccurrenceMap* map = m_maps[loop->GetIndex()];
    if (map != nullptr)
    {
        return map;
    }

    BitVecTraits poTraits = m_loops->GetDfsTree()->PostOrderTraits();

#ifdef DEBUG
    // As an invariant the map contains only the locals exclusive to each loop
    // (i.e. occurrences inside descendant loops are not contained in ancestor
    // loop maps). Double check that we've already computed the child maps to
    // make sure we do not visit descendant blocks below.
    for (FlowGraphNaturalLoop* child = loop->GetChild(); child != nullptr; child = child->GetSibling())
    {
        assert(BitVecOps::IsMember(&poTraits, m_visitedBlocks, child->GetHeader()->bbPostorderNum));
    }
#endif

    Compiler* comp           = m_loops->GetDfsTree()->GetCompiler();
    map                      = new (comp, CMK_LoopOpt) LocalToOccurrenceMap(comp->getAllocator(CMK_LoopOpt));
    m_maps[loop->GetIndex()] = map;

    loop->VisitLoopBlocksReversePostOrder([=, &poTraits](BasicBlock* block) {
        if (!BitVecOps::TryAddElemD(&poTraits, m_visitedBlocks, block->bbPostorderNum))
        {
            return BasicBlockVisit::Continue;
        }

        for (Statement* stmt : block->NonPhiStatements())
        {
            for (GenTree* node : stmt->TreeList())
            {
                if (!node->OperIsAnyLocal())
                {
                    continue;
                }

                GenTreeLclVarCommon* lcl        = node->AsLclVarCommon();
                Occurrence**         occurrence = map->LookupPointerOrAdd(lcl->GetLclNum(), nullptr);

                Occurrence* newOccurrence = new (comp, CMK_LoopOpt) Occurrence;
                newOccurrence->Block      = block;
                newOccurrence->Statement  = stmt;
                newOccurrence->Node       = lcl;
                newOccurrence->Next       = *occurrence;
                *occurrence               = newOccurrence;
            }
        }

        return BasicBlockVisit::Continue;
    });

    return map;
}

//------------------------------------------------------------------------------
// LoopLocalOccurrences:VisitLoopNestMaps:
//   Visit all occurrence maps of the specified loop nest.
//
// Type parameters:
//   TFunc - bool(LocalToOccurrenceMap*) functor that returns true to continue
//           the visit and false to abort.
//
// Parameters:
//   loop - Root loop of the nest.
//   func - Functor instance
//
// Returns:
//   True if the visit completed; false if "func" returned false for any map.
//
template <typename TFunc>
bool LoopLocalOccurrences::VisitLoopNestMaps(FlowGraphNaturalLoop* loop, TFunc& func)
{
    for (FlowGraphNaturalLoop* child = loop->GetChild(); child != nullptr; child = child->GetSibling())
    {
        if (!VisitLoopNestMaps(child, func))
        {
            return false;
        }
    }

    return func(GetOrCreateMap(loop));
}

//------------------------------------------------------------------------------
// LoopLocalOccurrences:VisitOccurrences:
//   Visit all occurrences of the specified local inside the loop.
//
// Type parameters:
//   TFunc - Functor of type bool(Block*, Statement*, GenTreeLclVarCommon*)
//
// Parameters:
//   loop   - The loop
//   lclNum - The local whose occurrences to visit
//   func   - Functor instance. Return true to continue the visit, and
//            false to abort it.
//
// Returns:
//   True if the visit completed and false if it was aborted by the functor
//   returning false.
//
template <typename TFunc>
bool LoopLocalOccurrences::VisitOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum, TFunc func)
{
    auto visitor = [=, &func](LocalToOccurrenceMap* map) {
        Occurrence* occurrence;
        if (!map->Lookup(lclNum, &occurrence))
        {
            return true;
        }

        assert(occurrence != nullptr);

        do
        {
            if (!func(occurrence->Block, occurrence->Statement, occurrence->Node))
            {
                return false;
            }

            occurrence = occurrence->Next;
        } while (occurrence != nullptr);

        return true;
    };

    return VisitLoopNestMaps(loop, visitor);
}

//------------------------------------------------------------------------------
// LoopLocalOccurrences:HasAnyOccurrences:
//   Check if this loop has any occurrences of the specified local.
//
// Parameters:
//   loop   - The loop
//   lclNum - Local to check occurrences of
//
// Returns:
//   True if it does.
//
// Remarks:
//   Does not take promotion into account.
//
bool LoopLocalOccurrences::HasAnyOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum)
{
    if (!VisitOccurrences(loop, lclNum, [](BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* tree) {
        return false;
    }))
    {
        return true;
    }

    return false;
}

//------------------------------------------------------------------------------
// LoopLocalOccurrences:VisitStatementsWithOccurrences:
//   Visit all statements with occurrences of the specified local inside
//   the loop.
//
// Type parameters:
//   TFunc - Functor of type bool(Block*, Statement*)
//
// Parameters:
//   loop   - The loop
//   lclNum - The local whose occurrences to visit
//   func   - Functor instance. Return true to continue the visit, and
//            false to abort it.
//
// Returns:
//   True if the visit completed and false if it was aborted by the functor
//   returning false.
//
// Remarks:
//   A statement with multiple occurrences of the local is only visited
//   once.
//
template <typename TFunc>
bool LoopLocalOccurrences::VisitStatementsWithOccurrences(FlowGraphNaturalLoop* loop, unsigned lclNum, TFunc func)
{
    auto visitor = [=, &func](LocalToOccurrenceMap* map) {
        Occurrence* occurrence;
        if (!map->Lookup(lclNum, &occurrence))
        {
            return true;
        }

        assert(occurrence != nullptr);

        while (true)
        {
            if (!func(occurrence->Block, occurrence->Statement))
            {
                return false;
            }

            Statement* curStmt = occurrence->Statement;
            while (true)
            {
                occurrence = occurrence->Next;

                if (occurrence == nullptr)
                {
                    return true;
                }

                if (occurrence->Statement != curStmt)
                {
                    break;
                }
            }
        }

        return true;
    };

    return VisitLoopNestMaps(loop, visitor);
}

//------------------------------------------------------------------------
// Invalidate: Invalidate all information about locals in the specified loop
// and its child loops.
//
// Parameters:
//   loop - The loop
//
void LoopLocalOccurrences::Invalidate(FlowGraphNaturalLoop* loop)
{
    for (FlowGraphNaturalLoop* child = loop->GetChild(); child != nullptr; child = child->GetSibling())
    {
        Invalidate(child);
    }

    if (m_maps[loop->GetIndex()] != nullptr)
    {
        m_maps[loop->GetIndex()] = nullptr;

        BitVecTraits poTraits = m_loops->GetDfsTree()->PostOrderTraits();
        loop->VisitLoopBlocks([=, &poTraits](BasicBlock* block) {
            BitVecOps::RemoveElemD(&poTraits, m_visitedBlocks, block->bbPostorderNum);
            return BasicBlockVisit::Continue;
        });
    }
}

//------------------------------------------------------------------------
// optCanSinkWidenedIV: Check to see if we are able to sink a store to the old
// local into the exits of a loop if we decide to widen.
//
// Parameters:
//   lclNum - The primary induction variable
//   loop   - The loop
//
// Returns:
//   True if we can sink a store to the old local after widening.
//
// Remarks:
//   This handles the situation where the primary induction variable is used
//   after the loop. In those cases we need to store the widened local back
//   into the old one in the exits where the IV variable is live.
//
//   We are able to sink when none of the exits are critical blocks, in the
//   sense that all their predecessors must come from inside the loop. Loop
//   exit canonicalization guarantees this for regular exit blocks. It is not
//   guaranteed for exceptional exits, but we do not expect to widen IVs that
//   are live into exceptional exits since those are marked DNER which makes it
//   unprofitable anyway.
//
//   Note that there may be natural loops that have not had their regular exits
//   canonicalized at the time when IV opts run, in particular if RBO/assertion
//   prop makes a previously unnatural loop natural. This function accounts for
//   and rejects these cases.
//
bool Compiler::optCanSinkWidenedIV(unsigned lclNum, FlowGraphNaturalLoop* loop)
{
    LclVarDsc* dsc = lvaGetDesc(lclNum);

    BasicBlockVisit result = loop->VisitRegularExitBlocks([=](BasicBlock* exit) {
        if (!VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            JITDUMP("  Exit " FMT_BB " does not need a sink; V%02u is not live-in\n", exit->bbNum, lclNum);
            return BasicBlockVisit::Continue;
        }

        for (BasicBlock* pred : exit->PredBlocks())
        {
            if (!loop->ContainsBlock(pred))
            {
                JITDUMP("  Cannot safely sink widened version of V%02u into exit " FMT_BB " of " FMT_LP
                        "; it has a non-loop pred " FMT_BB "\n",
                        lclNum, exit->bbNum, loop->GetIndex(), pred->bbNum);
                return BasicBlockVisit::Abort;
            }
        }

        return BasicBlockVisit::Continue;
    });

#ifdef DEBUG
    // We currently do not expect to ever widen IVs that are live into
    // exceptional exits. Such IVs are expected to have been marked DNER
    // previously (EH write-thru is only for single def locals) which makes it
    // unprofitable. If this ever changes we need some more expansive handling
    // here.
    loop->VisitLoopBlocks([=](BasicBlock* block) {
        block->VisitAllSuccs(this, [=](BasicBlock* succ) {
            if (!loop->ContainsBlock(succ) && bbIsHandlerBeg(succ))
            {
                assert(!VarSetOps::IsMember(this, succ->bbLiveIn, dsc->lvVarIndex) &&
                       "Candidate IV for widening is live into exceptional exit");
            }

            return BasicBlockVisit::Continue;
        });

        return BasicBlockVisit::Continue;
    });
#endif

    return result != BasicBlockVisit::Abort;
}

//------------------------------------------------------------------------
// optIsIVWideningProfitable: Check to see if IV widening is profitable.
//
// Parameters:
//   lclNum           - The primary induction variable
//   initBlock        - The block in where the new IV would be initialized
//   initedToConstant - Whether or not the new IV will be initialized to a constant
//   loop             - The loop
//   loopLocals       - Data structure tracking local uses inside the loop
//
//
// Returns:
//   True if IV widening is profitable.
//
// Remarks:
//   IV widening is generally profitable when it allows us to remove casts
//   inside the loop. However, it may also introduce other reg-reg moves:
//     1. We may need to store the narrow IV into the wide one in the
//     preheader. This is necessary when the start value is not constant. If
//     the start value _is_ constant then we assume that the constant store to
//     the narrow local will be a DCE'd.
//     2. We need to store the wide IV back into the narrow one in each of
//     the exits where the narrow IV is live-in.
//
bool Compiler::optIsIVWideningProfitable(unsigned              lclNum,
                                         BasicBlock*           initBlock,
                                         bool                  initedToConstant,
                                         FlowGraphNaturalLoop* loop,
                                         LoopLocalOccurrences* loopLocals)
{
    for (FlowGraphNaturalLoop* otherLoop : m_loops->InReversePostOrder())
    {
        if (otherLoop == loop)
            continue;

        for (Statement* stmt : otherLoop->GetHeader()->Statements())
        {
            if (!stmt->IsPhiDefnStmt())
                break;

            if (stmt->GetRootNode()->AsLclVarCommon()->GetLclNum() == lclNum)
            {
                JITDUMP("  V%02u has a phi [%06u] in " FMT_LP "'s header " FMT_BB "\n", lclNum,
                        dspTreeID(stmt->GetRootNode()), otherLoop->GetIndex(), otherLoop->GetHeader()->bbNum);
                // TODO-CQ: We can legally widen these cases, but LSRA is
                // unhappy about some of the lifetimes we create when we do
                // this. This particularly affects cloned loops.
                return false;
            }
        }
    }

    const weight_t ExtensionCost = 2;
    const int      ExtensionSize = 3;

    weight_t savedCost = 0;
    int      savedSize = 0;

    auto measure = [=, &savedCost, &savedSize](BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* lcl) {
        GenTree* parent = lcl->gtGetParent(nullptr);
        if ((parent == nullptr) || !parent->OperIs(GT_CAST))
        {
            return true;
        }

        GenTreeCast* cast = parent->AsCast();
        if ((cast->gtCastType != TYP_LONG) || !cast->IsUnsigned() || cast->gtOverflow())
        {
            return true;
        }

        // If this is already the source of a store then it is going to be
        // free in our backends regardless.
        parent = cast->gtGetParent(nullptr);
        if ((parent != nullptr) && parent->OperIs(GT_STORE_LCL_VAR))
        {
            return true;
        }

        savedSize += ExtensionSize;
        savedCost += block->getBBWeight(this) * ExtensionCost;
        return true;
    };

    loopLocals->VisitOccurrences(loop, lclNum, measure);

    if (!initedToConstant)
    {
        // We will need to store the narrow IV into the wide one in the init
        // block. We only cost this when init value is not a constant since
        // otherwise we assume that constant initialization of the narrow local
        // will be DCE'd.
        savedSize -= ExtensionSize;
        savedCost -= initBlock->getBBWeight(this) * ExtensionCost;
    }

    // Now account for the cost of sinks.
    LclVarDsc* dsc = lvaGetDesc(lclNum);
    loop->VisitRegularExitBlocks([&](BasicBlock* exit) {
        if (VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            savedSize -= ExtensionSize;
            savedCost -= exit->getBBWeight(this) * ExtensionCost;
        }
        return BasicBlockVisit::Continue;
    });

    const weight_t ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT = 2;
    weight_t       cycleImprovementPerInvoc                      = savedCost / fgFirstBB->getBBWeight(this);

    JITDUMP("  Estimated cycle improvement: " FMT_WT " cycles per invocation\n", cycleImprovementPerInvoc);
    JITDUMP("  Estimated size improvement: %d bytes\n", savedSize);

    if ((cycleImprovementPerInvoc > 0) &&
        ((cycleImprovementPerInvoc * ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT) >= -savedSize))
    {
        JITDUMP("    Widening is profitable (cycle improvement)\n");
        return true;
    }

    const weight_t ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT = 0.01;

    if ((savedSize > 0) && ((savedSize * ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT) >= -cycleImprovementPerInvoc))
    {
        JITDUMP("  Widening is profitable (size improvement)\n");
        return true;
    }

    JITDUMP("  Widening is not profitable\n");
    return false;
}

//------------------------------------------------------------------------
// optSinkWidenedIV: Create stores back to the narrow IV in the exits where
// that is necessary.
//
// Parameters:
//   lclNum    - Narrow version of primary induction variable
//   newLclNum - Wide version of primary induction variable
//   loop      - The loop
//
// Returns:
//   True if any store was created in any exit block.
//
void Compiler::optSinkWidenedIV(unsigned lclNum, unsigned newLclNum, FlowGraphNaturalLoop* loop)
{
    LclVarDsc* dsc = lvaGetDesc(lclNum);
    loop->VisitRegularExitBlocks([=](BasicBlock* exit) {
        if (!VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            return BasicBlockVisit::Continue;
        }

        GenTree*   narrowing = gtNewCastNode(TYP_INT, gtNewLclvNode(newLclNum, TYP_LONG), false, TYP_INT);
        GenTree*   store     = gtNewStoreLclVarNode(lclNum, narrowing);
        Statement* newStmt   = fgNewStmtFromTree(store);
        JITDUMP("Narrow IV local V%02u live into exit block " FMT_BB "; sinking a narrowing\n", lclNum, exit->bbNum);
        DISPSTMT(newStmt);
        fgInsertStmtAtBeg(exit, newStmt);

        return BasicBlockVisit::Continue;
    });
}

//------------------------------------------------------------------------
// optReplaceWidenedIV: Replace uses of the narrow IV with the wide IV in the
// specified statement.
//
// Parameters:
//   lclNum    - Narrow version of primary induction variable
//   newLclNum - Wide version of primary induction variable
//   stmt      - The statement to replace uses in.
//
void Compiler::optReplaceWidenedIV(unsigned lclNum, unsigned ssaNum, unsigned newLclNum, Statement* stmt)
{
    struct ReplaceVisitor : GenTreeVisitor<ReplaceVisitor>
    {
    private:
        unsigned m_lclNum;
        unsigned m_ssaNum;
        unsigned m_newLclNum;

        bool IsLocal(GenTreeLclVarCommon* tree)
        {
            return (tree->GetLclNum() == m_lclNum) &&
                   ((m_ssaNum == SsaConfig::RESERVED_SSA_NUM) || (tree->GetSsaNum() == m_ssaNum));
        }

    public:
        bool MadeChanges = false;

        enum
        {
            DoPreOrder = true,
        };

        ReplaceVisitor(Compiler* comp, unsigned lclNum, unsigned ssaNum, unsigned newLclNum)
            : GenTreeVisitor(comp)
            , m_lclNum(lclNum)
            , m_ssaNum(ssaNum)
            , m_newLclNum(newLclNum)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if (node->OperIs(GT_CAST))
            {
                GenTreeCast* cast = node->AsCast();
                if ((cast->gtCastType == TYP_LONG) && cast->IsUnsigned() && !cast->gtOverflow())
                {
                    GenTree* op = cast->CastOp();
                    if (op->OperIs(GT_LCL_VAR) && IsLocal(op->AsLclVarCommon()))
                    {
                        *use        = m_compiler->gtNewLclvNode(m_newLclNum, TYP_LONG);
                        MadeChanges = true;
                        return fgWalkResult::WALK_SKIP_SUBTREES;
                    }
                }
            }
            else if (node->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_FLD) &&
                     IsLocal(node->AsLclVarCommon()))
            {
                switch (node->OperGet())
                {
                    case GT_LCL_VAR:
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        // No cast needed -- the backend allows TYP_INT uses of TYP_LONG locals.
                        break;
                    case GT_STORE_LCL_VAR:
                    {
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        node->gtType = TYP_LONG;
                        node->AsLclVarCommon()->Data() =
                            m_compiler->gtNewCastNode(TYP_LONG, node->AsLclVarCommon()->Data(), true, TYP_LONG);
                        break;
                    }
                    case GT_LCL_FLD:
                    case GT_STORE_LCL_FLD:
                        assert(!"Unexpected field use for local not marked as DNER");
                        break;
                    default:
                        break;
                }

                MadeChanges = true;
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    ReplaceVisitor visitor(this, lclNum, ssaNum, newLclNum);
    visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    if (visitor.MadeChanges)
    {
        gtSetStmtInfo(stmt);
        fgSetStmtSeq(stmt);
        JITDUMP("New tree:\n", dspTreeID(stmt->GetRootNode()));
        DISPTREE(stmt->GetRootNode());
        JITDUMP("\n");
    }
    else
    {
        JITDUMP("No replacements made\n");
    }
}

//------------------------------------------------------------------------
// optBestEffortReplaceNarrowIVUses: Try to find and replace uses of the specified
// SSA def with a new local.
//
// Parameters:
//   lclNum    - Previous local
//   ssaNum    - Previous local SSA num
//   newLclNum - New local to replace with
//   block     - Block to replace in
//   firstStmt - First statement in "block" to start replacing in
//
// Remarks:
//   This function is best effort; it might not find all uses of the provided
//   SSA num, particularly because it does not follow into joins. Note that we
//   only use this to replace uses of the narrow IV outside the loop; inside
//   the loop we do ensure that all uses/defs are replaced.
//   Keeping it best-effort outside the loop is ok; there is no correctness
//   issue since we do not invalidate the value of the old narrow IV in any
//   way, but it may mean we end up leaving the narrow IV live concurrently
//   with the new widened IV, increasing register pressure.
//
void Compiler::optBestEffortReplaceNarrowIVUses(
    unsigned lclNum, unsigned ssaNum, unsigned newLclNum, BasicBlock* block, Statement* firstStmt)
{
    JITDUMP("Replacing V%02u -> V%02u in " FMT_BB " starting at " FMT_STMT "\n", lclNum, newLclNum, block->bbNum,
            firstStmt == nullptr ? 0 : firstStmt->GetID());

    for (Statement* stmt = firstStmt; stmt != nullptr; stmt = stmt->GetNextStmt())
    {
        JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lclNum, newLclNum, dspTreeID(stmt->GetRootNode()));
        DISPSTMT(stmt);
        JITDUMP("\n");

        optReplaceWidenedIV(lclNum, ssaNum, newLclNum, stmt);
    }

    block->VisitRegularSuccs(this, [=](BasicBlock* succ) {
        if (succ->GetUniquePred(this) == block)
        {
            optBestEffortReplaceNarrowIVUses(lclNum, ssaNum, newLclNum, succ, succ->firstStmt());
        }

        return BasicBlockVisit::Continue;
    });
}

//------------------------------------------------------------------------
// optWidenIVs: Widen IVs of the specified loop.
//
// Parameters:
//   scevContext - Context for scalar evolution
//   loop        - The loop
//   loopLocals  - Data structure for locals occurrences
//
// Returns:
//   True if any primary IV was widened.
//
bool Compiler::optWidenIVs(ScalarEvolutionContext& scevContext,
                           FlowGraphNaturalLoop*   loop,
                           LoopLocalOccurrences*   loopLocals)
{
    JITDUMP("Considering primary IVs of " FMT_LP " for widening\n", loop->GetIndex());

    unsigned numWidened = 0;
    for (Statement* stmt : loop->GetHeader()->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        JITDUMP("\n");
        DISPSTMT(stmt);

        Scev* scev = scevContext.Analyze(loop->GetHeader(), stmt->GetRootNode());
        if (scev == nullptr)
        {
            JITDUMP("  Could not analyze header PHI\n");
            continue;
        }

        JITDUMP("  => ");
        DBEXEC(verbose, scev->Dump(this));
        JITDUMP("\n");
        if (!scev->OperIs(ScevOper::AddRec))
        {
            JITDUMP("  Not an addrec\n");
            continue;
        }

        ScevAddRec* addRec = (ScevAddRec*)scev;

        unsigned   lclNum = stmt->GetRootNode()->AsLclVarCommon()->GetLclNum();
        LclVarDsc* lclDsc = lvaGetDesc(lclNum);
        JITDUMP("  V%02u is a primary induction variable in " FMT_LP "\n", lclNum, loop->GetIndex());

        assert(!lclDsc->lvPromoted);

        // For a struct field with occurrences of the parent local we won't
        // be able to do much.
        if (lclDsc->lvIsStructField && loopLocals->HasAnyOccurrences(loop, lclDsc->lvParentLcl))
        {
            JITDUMP("  V%02u is a struct field whose parent local V%02u has occurrences inside the loop\n", lclNum,
                    lclDsc->lvParentLcl);
            continue;
        }

        if (optWidenPrimaryIV(loop, lclNum, addRec, loopLocals))
        {
            numWidened++;
        }
    }

    Metrics.WidenedIVs += numWidened;
    return numWidened > 0;
}

//------------------------------------------------------------------------
// optWidenPrimaryIV: Attempt to widen a primary IV.
//
// Parameters:
//   loop       - The loop
//   lclNum     - The primary IV
//   addRec     - The add recurrence for the primary IV
//   loopLocals - Data structure for locals occurrences
//
bool Compiler::optWidenPrimaryIV(FlowGraphNaturalLoop* loop,
                                 unsigned              lclNum,
                                 ScevAddRec*           addRec,
                                 LoopLocalOccurrences* loopLocals)
{
    LclVarDsc* lclDsc = lvaGetDesc(lclNum);
    if (lclDsc->TypeGet() != TYP_INT)
    {
        JITDUMP("  Type is %s, no widening to be done\n", varTypeName(lclDsc->TypeGet()));
        return false;
    }

    // If the IV is not enregisterable then uses/defs are going to go
    // to stack regardless. This check also filters out IVs that may be
    // live into exceptional exits since those are always marked DNER.
    if (lclDsc->lvDoNotEnregister)
    {
        JITDUMP("  V%02u is marked DNER\n", lclNum);
        return false;
    }

    if (!optCanSinkWidenedIV(lclNum, loop))
    {
        return false;
    }

    // Now figure out where we are going to init the widened version of the IV.
    // We prefer to put it in the same spot as the narrow IV was initialized.
    // Find that now.
    // Start value should always be an SSA use from outside the loop
    // since we only widen primary IVs.
    assert(addRec->Start->OperIs(ScevOper::Local));

    ScevLocal*    startLocal     = (ScevLocal*)addRec->Start;
    int64_t       startConstant  = 0;
    bool          initToConstant = startLocal->GetConstantValue(this, &startConstant);
    LclSsaVarDsc* startSsaDsc    = lclDsc->GetPerSsaData(startLocal->SsaNum);

    BasicBlock* preheader = loop->EntryEdge(0)->getSourceBlock();
    BasicBlock* initBlock = preheader;
    if ((startSsaDsc->GetBlock() != nullptr) && (startSsaDsc->GetDefNode() != nullptr))
    {
        initBlock = startSsaDsc->GetBlock();
    }

    if (!optIsIVWideningProfitable(lclNum, initBlock, initToConstant, loop, loopLocals))
    {
        return false;
    }

    Statement* insertInitAfter = nullptr;
    if (initBlock != preheader)
    {
        GenTree* narrowInitRoot = startSsaDsc->GetDefNode();
        while (true)
        {
            GenTree* parent = narrowInitRoot->gtGetParent(nullptr);
            if (parent == nullptr)
                break;

            narrowInitRoot = parent;
        }

        for (Statement* stmt : initBlock->Statements())
        {
            if (stmt->GetRootNode() == narrowInitRoot)
            {
                insertInitAfter = stmt;
                break;
            }
        }

        assert(insertInitAfter != nullptr);

        if (insertInitAfter->IsPhiDefnStmt())
        {
            while ((insertInitAfter->GetNextStmt() != nullptr) && insertInitAfter->GetNextStmt()->IsPhiDefnStmt())
            {
                insertInitAfter = insertInitAfter->GetNextStmt();
            }
        }
    }

    Statement* initStmt  = nullptr;
    unsigned   newLclNum = lvaGrabTemp(false DEBUGARG(printfAlloc("Widened IV V%02u", lclNum)));
    INDEBUG(lclDsc = nullptr);
    assert(startLocal->LclNum == lclNum);

    if (initBlock != preheader)
    {
        JITDUMP("Adding initialization of new widened local to same block as reaching def outside loop, " FMT_BB "\n",
                initBlock->bbNum);
    }
    else
    {
        JITDUMP("Adding initialization of new widened local to preheader " FMT_BB "\n", initBlock->bbNum);
    }

    GenTree* initVal;
    if (initToConstant)
    {
        initVal = gtNewIconNode((int64_t)(uint32_t)startConstant, TYP_LONG);
    }
    else
    {
        initVal = gtNewCastNode(TYP_LONG, gtNewLclvNode(lclNum, TYP_INT), true, TYP_LONG);
    }

    GenTree* widenStore = gtNewTempStore(newLclNum, initVal);
    initStmt            = fgNewStmtFromTree(widenStore);
    if (insertInitAfter != nullptr)
    {
        fgInsertStmtAfter(initBlock, insertInitAfter, initStmt);
    }
    else
    {
        fgInsertStmtNearEnd(initBlock, initStmt);
    }

    DISPSTMT(initStmt);
    JITDUMP("\n");

    JITDUMP("  Replacing uses of V%02u with widened version V%02u\n", lclNum, newLclNum);

    if (initStmt != nullptr)
    {
        JITDUMP("    Replacing on the way to the loop\n");
        optBestEffortReplaceNarrowIVUses(lclNum, startLocal->SsaNum, newLclNum, initBlock, initStmt->GetNextStmt());
    }

    JITDUMP("    Replacing inside the loop\n");

    auto replace = [this, lclNum, newLclNum](BasicBlock* block, Statement* stmt) {
        JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lclNum, newLclNum, dspTreeID(stmt->GetRootNode()));
        DISPSTMT(stmt);
        JITDUMP("\n");
        optReplaceWidenedIV(lclNum, SsaConfig::RESERVED_SSA_NUM, newLclNum, stmt);
        return true;
    };

    loopLocals->VisitStatementsWithOccurrences(loop, lclNum, replace);

    optSinkWidenedIV(lclNum, newLclNum, loop);
    loopLocals->Invalidate(loop);
    return true;
}

//------------------------------------------------------------------------
// optVisitBoundingExitingBlocks: Visit all the exiting BBJ_COND blocks of the
// loop that dominate all the loop's backedges. These exiting blocks bound the
// trip count of the loop.
//
// Parameters:
//   loop - The loop
//   func - The functor, of type void(BasicBlock*).
//
template <typename TFunctor>
void Compiler::optVisitBoundingExitingCondBlocks(FlowGraphNaturalLoop* loop, TFunctor func)
{
    BasicBlock* dominates = nullptr;

    for (FlowEdge* backEdge : loop->BackEdges())
    {
        if (dominates == nullptr)
        {
            dominates = backEdge->getSourceBlock();
        }
        else
        {
            dominates = m_domTree->Intersect(dominates, backEdge->getSourceBlock());
        }
    }

    bool changed = false;
    while ((dominates != nullptr) && loop->ContainsBlock(dominates))
    {
        if (dominates->KindIs(BBJ_COND) &&
            (!loop->ContainsBlock(dominates->GetTrueTarget()) || !loop->ContainsBlock(dominates->GetFalseTarget())))
        {
            // 'dominates' is an exiting block that dominates all backedges.
            func(dominates);
        }

        dominates = dominates->bbIDom;
    }
}

//------------------------------------------------------------------------
// optMakeLoopDownwardsCounted: Transform a loop to be downwards counted if
// profitable and legal.
//
// Parameters:
//   scevContext - Context for scalar evolution
//   loop        - Loop to transform
//   loopLocals  - Data structure that tracks occurrences of locals in the loop
//
// Returns:
//   True if the loop was made downwards counted; otherwise false.
//
bool Compiler::optMakeLoopDownwardsCounted(ScalarEvolutionContext& scevContext,
                                           FlowGraphNaturalLoop*   loop,
                                           LoopLocalOccurrences*   loopLocals)
{
    JITDUMP("Checking if we should make " FMT_LP " downwards counted\n", loop->GetIndex());

    bool changed = false;
    optVisitBoundingExitingCondBlocks(loop, [=, &scevContext, &changed](BasicBlock* exiting) {
        JITDUMP("  Considering exiting block " FMT_BB "\n", exiting->bbNum);
        changed |= optMakeExitTestDownwardsCounted(scevContext, loop, exiting, loopLocals);
    });

    return changed;
}

//------------------------------------------------------------------------
// optMakeExitTestDownwardsCounted:
//   Try to modify the condition of a specific BBJ_COND exiting block to be on
//   a downwards counted IV if profitable.
//
// Parameters:
//   scevContext - SCEV context
//   loop        - The specific loop
//   exiting     - Exiting block
//   loopLocals  - Data structure tracking local uses
//
// Returns:
//   True if any modification was made.
//
bool Compiler::optMakeExitTestDownwardsCounted(ScalarEvolutionContext& scevContext,
                                               FlowGraphNaturalLoop*   loop,
                                               BasicBlock*             exiting,
                                               LoopLocalOccurrences*   loopLocals)
{
    // Note: keep the heuristics here in sync with
    // `StrengthReductionContext::IsUseExpectedToBeRemoved`.

    assert(exiting->KindIs(BBJ_COND));

    Statement* jtrueStmt = exiting->lastStmt();
    GenTree*   jtrue     = jtrueStmt->GetRootNode();
    assert(jtrue->OperIs(GT_JTRUE));
    GenTree* cond = jtrue->gtGetOp1();

    if (!optCanAndShouldChangeExitTest(cond, /* dump */ true))
    {
        return false;
    }

    // Making a loop downwards counted is profitable if there is a primary IV
    // that has no uses outside the loop test (and mutating itself). Check that
    // now.
    ArrayStack<unsigned> removableLocals(getAllocator(CMK_LoopOpt));

    for (Statement* stmt : loop->GetHeader()->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        unsigned candidateLclNum = stmt->GetRootNode()->AsLclVarCommon()->GetLclNum();

        if (optPrimaryIVHasNonLoopUses(candidateLclNum, loop, loopLocals))
        {
            continue;
        }

        bool hasUseInTest      = false;
        auto checkRemovableUse = [=, &hasUseInTest](BasicBlock* block, Statement* stmt) {
            if (stmt == jtrueStmt)
            {
                hasUseInTest = true;
                // Use is inside the loop test that we know we can change (from
                // calling optCanAndShouldChangeExitTest above)
                return true;
            }

            if (optIsUpdateOfIVWithoutSideEffects(stmt->GetRootNode(), candidateLclNum))
            {
                return true;
            }

            return false;
        };

        if (!loopLocals->VisitStatementsWithOccurrences(loop, candidateLclNum, checkRemovableUse))
        {
            // Aborted means we found a non-removable use
            continue;
        }

        if (!hasUseInTest)
        {
            // This one we can remove, but we expect it to be removable even without this transformation.
            continue;
        }

        JITDUMP("  Expecting to be able to remove V%02u by making this loop reverse counted\n", candidateLclNum);
        removableLocals.Push(candidateLclNum);
    }

    bool checkProfitability = !compStressCompile(STRESS_DOWNWARDS_COUNTED_LOOPS, 50);
    if (checkProfitability && (removableLocals.Height() <= 0))
    {
        JITDUMP("  Found no potentially removable locals when making this loop downwards counted\n");
        return false;
    }

    if (loop->MayExecuteBlockMultipleTimesPerIteration(exiting))
    {
        JITDUMP("  Exiting block may be executed multiple times per iteration; cannot place decrement in it\n");
        return false;
    }

    Scev* backedgeCount = scevContext.ComputeExitNotTakenCount(exiting);
    if (backedgeCount == nullptr)
    {
        JITDUMP("  Could not compute backedge count -- not a counted loop\n");
        return false;
    }

    BasicBlock* preheader = loop->GetPreheader();
    assert(preheader != nullptr);

    // We are interested in phrasing the test as (--x == 0). That requires us
    // to add one to the computed backedge count, giving us the trip count of
    // the loop. We do not need to worry about overflow here (even with
    // wraparound we have the right behavior).
    Scev* tripCount = scevContext.Simplify(
        scevContext.NewBinop(ScevOper::Add, backedgeCount, scevContext.NewConstant(backedgeCount->Type, 1)));
    GenTree* tripCountNode = scevContext.Materialize(tripCount);
    if (tripCountNode == nullptr)
    {
        JITDUMP("  Could not materialize trip count into IR\n");
        return false;
    }

    JITDUMP("  Converting " FMT_LP " into a downwards loop\n", loop->GetIndex());

    unsigned tripCountLcl = lvaGrabTemp(false DEBUGARG("Trip count IV"));
    GenTree* store        = gtNewTempStore(tripCountLcl, tripCountNode);

    Statement* newStmt = fgNewStmtFromTree(store);
    fgInsertStmtAtEnd(preheader, newStmt);

    JITDUMP("  Inserted initialization of tripcount local\n\n");
    DISPSTMT(newStmt);

    genTreeOps exitOp = GT_EQ;
    if (loop->ContainsBlock(exiting->GetTrueTarget()))
    {
        exitOp = GT_NE;
    }

    GenTree* negOne = tripCount->TypeIs(TYP_LONG) ? gtNewLconNode(-1) : gtNewIconNode(-1, tripCount->Type);
    GenTree* decremented =
        gtNewOperNode(GT_ADD, tripCount->Type, gtNewLclVarNode(tripCountLcl, tripCount->Type), negOne);

    store = gtNewTempStore(tripCountLcl, decremented);

    newStmt = fgNewStmtFromTree(store);
    fgInsertStmtNearEnd(exiting, newStmt);

    JITDUMP("\n  Inserted decrement of tripcount local\n\n");
    DISPSTMT(newStmt);

    // Update the test.
    cond->SetOper(exitOp);
    cond->AsOp()->gtOp1 = gtNewLclVarNode(tripCountLcl, tripCount->Type);
    cond->AsOp()->gtOp2 = gtNewZeroConNode(tripCount->Type);

    gtSetStmtInfo(jtrueStmt);
    fgSetStmtSeq(jtrueStmt);

    JITDUMP("\n  Updated exit test:\n");
    DISPSTMT(jtrueStmt);
    JITDUMP("\n");

    loopLocals->Invalidate(loop);
    return true;
}

//------------------------------------------------------------------------
// optCanAndShouldChangeExitTest:
//   Check if the exit test can be rephrased to a downwards counted exit test
//   (being compared to zero).
//
// Parameters:
//   cond - The exit test
//   dump - Whether to JITDUMP the reason for the decisions
//
// Returns:
//   True if the exit test can be changed.
//
bool Compiler::optCanAndShouldChangeExitTest(GenTree* cond, bool dump)
{
    if ((cond->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        // This would be possible if the IV use is not part of the side effect,
        // in which case we could extract them. However, these cases turn out
        // to be never analyzable for us even if we tried to do that, so just
        // do the easy check here.
        if (dump)
        {
            JITDUMP("  No; exit node has side effects\n");
        }

        return false;
    }

    bool checkProfitability = !compStressCompile(STRESS_DOWNWARDS_COUNTED_LOOPS, 50);

    if (checkProfitability && cond->OperIsCompare() &&
        (cond->gtGetOp1()->IsIntegralConst(0) || cond->gtGetOp2()->IsIntegralConst(0)))
    {
        if (dump)
        {
            JITDUMP("  No; operand of condition [%06u] is already 0\n", dspTreeID(cond));
        }

        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// optPrimaryIVHasNonLoopUses:
//   Check if a primary IV may have uses of the primary IV that we do not
//   reason about.
//
// Parameters:
//   lclNum     - The primary IV
//   loop       - The loop
//   loopLocals - Data structure tracking local uses
//
// Returns:
//   True if the primary IV may have non-loop uses (or if it is a field with
//   uses of the parent struct).
//
bool Compiler::optPrimaryIVHasNonLoopUses(unsigned lclNum, FlowGraphNaturalLoop* loop, LoopLocalOccurrences* loopLocals)
{
    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    if (varDsc->lvIsStructField && loopLocals->HasAnyOccurrences(loop, varDsc->lvParentLcl))
    {
        return true;
    }

    if (varDsc->lvDoNotEnregister)
    {
        // This filters out locals that may be live into exceptional exits.
        return true;
    }

    BasicBlockVisit visitResult = loop->VisitRegularExitBlocks([=](BasicBlock* block) {
        if (VarSetOps::IsMember(this, block->bbLiveIn, varDsc->lvVarIndex))
        {
            return BasicBlockVisit::Abort;
        }

        return BasicBlockVisit::Continue;
    });

    if (visitResult == BasicBlockVisit::Abort)
    {
        // Live into an exit.
        // TODO-CQ: In some cases it may be profitable to materialize the final value after the loop.
        // This requires analysis on whether the required expressions are available there
        // (and whether it doesn't extend their lifetimes too much).
        return true;
    }

    return false;
}

struct CursorInfo
{
    BasicBlock* Block;
    Statement*  Stmt;
    GenTree*    Tree;
    ScevAddRec* IV;

    CursorInfo(BasicBlock* block, Statement* stmt, GenTree* tree, ScevAddRec* iv)
        : Block(block)
        , Stmt(stmt)
        , Tree(tree)
        , IV(iv)
    {
    }
};

class StrengthReductionContext
{
    Compiler*               m_comp;
    ScalarEvolutionContext& m_scevContext;
    FlowGraphNaturalLoop*   m_loop;
    LoopLocalOccurrences&   m_loopLocals;

    ArrayStack<Scev*>         m_backEdgeBounds;
    SimplificationAssumptions m_simplAssumptions;
    ArrayStack<CursorInfo>    m_cursors1;
    ArrayStack<CursorInfo>    m_cursors2;
    ArrayStack<CursorInfo>    m_storesToRemove;

    void        InitializeSimplificationAssumptions();
    bool        InitializeCursors(GenTreeLclVarCommon* primaryIVLcl, ScevAddRec* primaryIV);
    bool        IsUseExpectedToBeRemoved(BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* tree);
    void        AdvanceCursors(ArrayStack<CursorInfo>* cursors, ArrayStack<CursorInfo>* nextCursors);
    void        ExpandStoredCursors(ArrayStack<CursorInfo>* cursors, ArrayStack<CursorInfo>* otherCursors);
    bool        CheckAdvancedCursors(ArrayStack<CursorInfo>* cursors, ScevAddRec** nextIV);
    bool        StaysWithinManagedObject(ArrayStack<CursorInfo>* cursors, ScevAddRec* addRec);
    bool        TryReplaceUsesWithNewPrimaryIV(ArrayStack<CursorInfo>* cursors, ScevAddRec* iv);
    BasicBlock* FindUpdateInsertionPoint(ArrayStack<CursorInfo>* cursors);

    bool StressProfitability()
    {
        return m_comp->compStressCompile(Compiler::STRESS_STRENGTH_REDUCTION_PROFITABILITY, 50);
    }

public:
    StrengthReductionContext(Compiler*               comp,
                             ScalarEvolutionContext& scevContext,
                             FlowGraphNaturalLoop*   loop,
                             LoopLocalOccurrences&   loopLocals)
        : m_comp(comp)
        , m_scevContext(scevContext)
        , m_loop(loop)
        , m_loopLocals(loopLocals)
        , m_backEdgeBounds(comp->getAllocator(CMK_LoopIVOpts))
        , m_cursors1(comp->getAllocator(CMK_LoopIVOpts))
        , m_cursors2(comp->getAllocator(CMK_LoopIVOpts))
        , m_storesToRemove(comp->getAllocator(CMK_LoopIVOpts))
    {
    }

    bool TryStrengthReduce();
};

//------------------------------------------------------------------------
// TryStrengthReduce: Check for legal and profitable derived IVs to introduce
// new primary IVs for.
//
// Returns:
//   True if any new primary IV was introduced; otherwise false.
//
bool StrengthReductionContext::TryStrengthReduce()
{
    JITDUMP("Considering " FMT_LP " for strength reduction...\n", m_loop->GetIndex());

    if ((JitConfig.JitEnableStrengthReduction() == 0) &&
        !m_comp->compStressCompile(Compiler::STRESS_STRENGTH_REDUCTION, 50))
    {
        JITDUMP("  Disabled: no stress mode\n");
        return false;
    }

    // Compute information about the loop used to simplify SCEVs.
    InitializeSimplificationAssumptions();

    JITDUMP("  Considering primary IVs\n");

    // We strength reduce only candidates where we see that we'll be able to
    // remove all uses of a primary IV by introducing a different primary IV.
    //
    // The algorithm here works in the following way: we process each primary
    // IV in turn. For every primary IV, we create a 'cursor' pointing to every
    // use of that primary IV. We then continuously advance each cursor to the
    // parent node as long as all cursors represent the same derived IV. Once we
    // find out that the cursors are no longer the same derived IV we stop.
    //
    // We keep two lists here so that we can keep track of the most advanced
    // cursor where all cursors pointed to the same derived IV, in which case
    // we can strength reduce.

    bool strengthReducedAny = false;
    for (Statement* stmt : m_loop->GetHeader()->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        DISPSTMT(stmt);

        GenTreeLclVarCommon* primaryIVLcl = stmt->GetRootNode()->AsLclVarCommon();
        Scev*                candidate    = m_scevContext.Analyze(m_loop->GetHeader(), primaryIVLcl);
        if (candidate == nullptr)
        {
            JITDUMP("  Could not analyze header PHI\n");
            continue;
        }

        candidate = m_scevContext.Simplify(candidate, m_simplAssumptions);

        JITDUMP("  => ");
        DBEXEC(m_comp->verbose, candidate->Dump(m_comp));

        JITDUMP("\n");
        if (!candidate->OperIs(ScevOper::AddRec))
        {
            JITDUMP("  Not an addrec\n");
            continue;
        }

        if (m_comp->optPrimaryIVHasNonLoopUses(primaryIVLcl->GetLclNum(), m_loop, &m_loopLocals))
        {
            // We won't be able to remove this primary IV
            JITDUMP("  Has non-loop uses\n");
            continue;
        }

        ScevAddRec* primaryIV = static_cast<ScevAddRec*>(candidate);

        if (!InitializeCursors(primaryIVLcl, primaryIV))
        {
            continue;
        }

        ArrayStack<CursorInfo>* cursors     = &m_cursors1;
        ArrayStack<CursorInfo>* nextCursors = &m_cursors2;

        int         derivedLevel = 0;
        ScevAddRec* currentIV    = primaryIV;

        while (true)
        {
            JITDUMP("  Advancing cursors to be %d-derived\n", derivedLevel + 1);

            // Advance cursors and store the result in 'nextCursors'
            AdvanceCursors(cursors, nextCursors);

            // Verify that all cursors still represent the same IV
            ScevAddRec* nextIV = nullptr;
            if (!CheckAdvancedCursors(nextCursors, &nextIV))
            {
                break;
            }

            ExpandStoredCursors(nextCursors, cursors);

            assert(nextIV != nullptr);

            if (varTypeIsGC(nextIV->Type) && !StaysWithinManagedObject(nextCursors, nextIV))
            {
                JITDUMP(
                    "    Next IV computes a GC pointer that we cannot prove to be inside a managed object. Bailing.\n",
                    varTypeName(nextIV->Type));
                break;
            }

            derivedLevel++;
            std::swap(cursors, nextCursors);
            currentIV = nextIV;
        }

        if (derivedLevel <= 0)
        {
            continue;
        }

        JITDUMP("  All uses of primary IV V%02u are used to compute a %d-derived IV ", primaryIVLcl->GetLclNum(),
                derivedLevel);
        DBEXEC(VERBOSE, currentIV->Dump(m_comp));
        JITDUMP("\n");

        if (!StressProfitability())
        {
            if (Scev::Equals(currentIV->Step, primaryIV->Step))
            {
                JITDUMP("    Skipping: Candidate has same step as primary IV\n");
                continue;
            }

            // Leave widening up to widening.
            int64_t newIVStep;
            int64_t primaryIVStep;
            if (currentIV->Step->TypeIs(TYP_LONG) && primaryIV->Step->TypeIs(TYP_INT) &&
                currentIV->Step->GetConstantValue(m_comp, &newIVStep) &&
                primaryIV->Step->GetConstantValue(m_comp, &primaryIVStep) &&
                (int32_t)newIVStep == (int32_t)primaryIVStep)
            {
                JITDUMP("    Skipping: Candidate has same widened step as primary IV\n");
                continue;
            }
        }

        if (TryReplaceUsesWithNewPrimaryIV(cursors, currentIV))
        {
            strengthReducedAny = true;
            m_loopLocals.Invalidate(m_loop);
        }
    }

    return strengthReducedAny;
}

//------------------------------------------------------------------------
// InitializeSimplificationAssumptions: Compute assumptions that can be used
// when simplifying SCEVs.
//
void StrengthReductionContext::InitializeSimplificationAssumptions()
{
    m_comp->optVisitBoundingExitingCondBlocks(m_loop, [=](BasicBlock* exiting) {
        Scev* exitNotTakenCount = m_scevContext.ComputeExitNotTakenCount(exiting);
        if (exitNotTakenCount != nullptr)
        {
            m_backEdgeBounds.Push(exitNotTakenCount);
        }
    });

    m_simplAssumptions.BackEdgeTakenBound    = m_backEdgeBounds.Data();
    m_simplAssumptions.NumBackEdgeTakenBound = static_cast<unsigned>(m_backEdgeBounds.Height());

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  Bound on backedge taken count is ");
        if (m_simplAssumptions.NumBackEdgeTakenBound == 0)
        {
            printf("<unknown>\n");
        }

        const char* pref = m_simplAssumptions.NumBackEdgeTakenBound > 1 ? "min(" : "";
        for (unsigned i = 0; i < m_simplAssumptions.NumBackEdgeTakenBound; i++)
        {
            printf("%s", pref);
            m_simplAssumptions.BackEdgeTakenBound[i]->Dump(m_comp);
        }

        printf("%s\n", m_simplAssumptions.NumBackEdgeTakenBound > 1 ? ")" : "");
    }
#endif
}

//------------------------------------------------------------------------
// InitializeCursors: Reset and initialize both cursor lists with information about all
// uses of the specified primary IV.
//
// Parameters:
//   primaryIVLcl - Local representing a candidate primary IV for strength reduction
//   primaryIV    - SCEV for the candidate
//
// Returns:
//   True if all uses were analyzed and cursors could be introduced for them
//   all; otherwise false.
//
// Remarks:
//   A cursor is created for a use when it represents the same value as the
//   primary IV passed. The function will allow mismatching uses if the use is
//   expected to be removed in the downwards loop transformation. Otherwise the
//   function will fail.
//
//   It is not a correctness requirement that we remove all uses; if we end up
//   not doing so (e.g. because a cursor was not created by this function),
//   then we may just end up with extra primary IVs in the loop.
//
bool StrengthReductionContext::InitializeCursors(GenTreeLclVarCommon* primaryIVLcl, ScevAddRec* primaryIV)
{
    m_cursors1.Reset();
    m_cursors2.Reset();

    auto visitor = [=](BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* tree) {
        if (IsUseExpectedToBeRemoved(block, stmt, tree))
        {
            // If we do strength reduction we expect to be able to remove this
            // use; do not create a cursor for it.
            return true;
        }

        if (!tree->OperIs(GT_LCL_VAR))
        {
            return false;
        }

        if (tree->GetSsaNum() != primaryIVLcl->GetSsaNum())
        {
            // Most likely a post-incremented use of the primary IV; we
            // could replace these as well, but currently we only handle
            // the cases where we expect the use to be removed.
            return false;
        }

        Scev* iv = m_scevContext.Analyze(block, tree);
        if (iv == nullptr)
        {
            // May not be able to analyze the use if it's mistyped (e.g.
            // LCL_VAR<byref>(TYP_I_IMPL LclVarDsc)), or an int use of a long
            // local.
            // Just bail on these cases.
            return false;
        }

        // If we _did_ manage to analyze it then we expect it to be the same IV
        // as the primary IV.
        assert(Scev::Equals(m_scevContext.Simplify(iv, m_simplAssumptions), primaryIV));

        m_cursors1.Emplace(block, stmt, tree, primaryIV);
        m_cursors2.Emplace(block, stmt, tree, primaryIV);
        return true;
    };

    if (!m_loopLocals.VisitOccurrences(m_loop, primaryIVLcl->GetLclNum(), visitor) || (m_cursors1.Height() <= 0))
    {
        JITDUMP("  Could not create cursors for all loop uses of primary IV\n");
        return false;
    }

    ExpandStoredCursors(&m_cursors1, &m_cursors2);

    JITDUMP("  Found %d cursors using primary IV V%02u\n", m_cursors1.Height(), primaryIVLcl->GetLclNum());

#ifdef DEBUG
    if (m_comp->verbose)
    {
        for (int i = 0; i < m_cursors1.Height(); i++)
        {
            CursorInfo& cursor = m_cursors1.BottomRef(i);
            printf("    [%d] [%06u]: ", i, Compiler::dspTreeID(cursor.Tree));
            cursor.IV->Dump(m_comp);
            printf("\n");
        }
    }
#endif

    return true;
}

//------------------------------------------------------------------------
// IsUseExpectedToBeRemoved: Check if a use of a primary IV is expected to be
// removed if we strength reduce other uses of the primary IV.
//
// Parameters:
//   block - Block containing the use
//   stmt  - Statement containing the use
//   tree  - Actual use of the primary IV
//
// Returns:
//   True if the use is expected to be removable.
//
bool StrengthReductionContext::IsUseExpectedToBeRemoved(BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* tree)
{
    unsigned primaryIVLclNum = tree->GetLclNum();
    if (m_comp->optIsUpdateOfIVWithoutSideEffects(stmt->GetRootNode(), tree->GetLclNum()))
    {
        // Removal of unused IVs will get rid of this.
        return true;
    }

    bool isInsideExitTest =
        block->KindIs(BBJ_COND) && (stmt == block->lastStmt()) &&
        (!m_loop->ContainsBlock(block->GetTrueTarget()) || !m_loop->ContainsBlock(block->GetFalseTarget()));

    if (isInsideExitTest)
    {
        // The downwards loop transformation may be able to remove this use.
        // Here we duplicate some of the logic from
        // optMakeExitTestDownwardsCounted to predict whether that will happen.
        GenTree* jtrue = block->lastStmt()->GetRootNode();
        GenTree* cond  = jtrue->gtGetOp1();

        // Is the exit test changeable?
        if (!m_comp->optCanAndShouldChangeExitTest(cond, /* dump */ false))
        {
            return false;
        }

        // Does the exit dominate all backedges such that we can place IV
        // updates before it?
        for (FlowEdge* edge : m_loop->BackEdges())
        {
            if (!m_comp->m_domTree->Dominates(block, edge->getSourceBlock()))
            {
                return false;
            }
        }

        // Will the exit only run once per iteration?
        if (m_loop->MayExecuteBlockMultipleTimesPerIteration(block))
        {
            return false;
        }

        // Can we compute the trip count from the exit test?
        if (m_scevContext.ComputeExitNotTakenCount(block) == nullptr)
        {
            return false;
        }

        // If all of those things are true, we are most likely going to be able
        // to convert the exit test to a down-counting one after we have removed the other uses of the IV.
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// AdvanceCursors: Advance cursors stored in "cursors" and store the advanced
// result in "nextCursors".
//
// Parameters:
//   cursors     - [in] List of current cursors. Unmodified.
//   nextCursors - [in, out] List of next cursors. The "Tree" and "IV" fields
//                 of these cursors will be updated to point to the next derived
//                 IV.
//
void StrengthReductionContext::AdvanceCursors(ArrayStack<CursorInfo>* cursors, ArrayStack<CursorInfo>* nextCursors)
{
    for (int i = 0; i < cursors->Height(); i++)
    {
        CursorInfo& cursor     = cursors->BottomRef(i);
        CursorInfo& nextCursor = nextCursors->BottomRef(i);

        assert((nextCursor.Block == cursor.Block) && (nextCursor.Stmt == cursor.Stmt));

        nextCursor.Tree = cursor.Tree;
        do
        {
            GenTree* cur    = nextCursor.Tree;
            nextCursor.Tree = cur->gtGetParent(nullptr);

            if ((nextCursor.Tree == nullptr) ||
                (nextCursor.Tree->OperIs(GT_COMMA) && (nextCursor.Tree->gtGetOp1() == cur)))
            {
                nextCursor.IV = nullptr;
                break;
            }

            Scev* parentIV = m_scevContext.Analyze(nextCursor.Block, nextCursor.Tree);
            if (parentIV == nullptr)
            {
                nextCursor.IV = nullptr;
                break;
            }

            parentIV = m_scevContext.Simplify(parentIV, m_simplAssumptions);
            assert(parentIV != nullptr);
            if (!parentIV->OperIs(ScevOper::AddRec))
            {
                nextCursor.IV = nullptr;
                break;
            }

            nextCursor.IV = static_cast<ScevAddRec*>(parentIV);
        } while (Scev::Equals(nextCursor.IV, cursor.IV));
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        for (int i = 0; i < nextCursors->Height(); i++)
        {
            CursorInfo& nextCursor = nextCursors->BottomRef(i);
            printf("    [%d] [%06u]: ", i, nextCursor.Tree == nullptr ? 0 : Compiler::dspTreeID(nextCursor.Tree));
            if (nextCursor.IV == nullptr)
            {
                printf("<null IV>");
            }
            else
            {
                nextCursor.IV->Dump(m_comp);
            }
            printf("\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// ExpandStoredCursors: For every cursor that is the source to a store, expand
// the sets of cursors to contain the destination local's uses if possible.
//
// Parameters:
//   cursors      - [in, out] List of current cursors to expand.
//   otherCursors - [in, out] List of next cursors.
//
void StrengthReductionContext::ExpandStoredCursors(ArrayStack<CursorInfo>* cursors,
                                                   ArrayStack<CursorInfo>* otherCursors)
{
    for (int i = 0; i < cursors->Height(); i++)
    {
        while (true)
        {
            CursorInfo* cursor = &cursors->BottomRef(i);
            GenTree*    cur    = cursor->Tree;

            GenTree* parent = cur->gtGetParent(nullptr);
            if ((parent == nullptr) || (parent->OperIs(GT_COMMA) && (parent->gtGetOp1() == cur)))
            {
                break;
            }

            if (parent->OperIs(GT_STORE_LCL_VAR) && (parent->AsLclVarCommon()->Data() == cur) &&
                ((cur->gtFlags & GTF_SIDE_EFFECT) == 0))
            {
                GenTreeLclVarCommon* storedLcl = parent->AsLclVarCommon();
                if (storedLcl->HasSsaIdentity() &&
                    !m_comp->optPrimaryIVHasNonLoopUses(storedLcl->GetLclNum(), m_loop, &m_loopLocals))
                {
                    int         numCreated  = 0;
                    ScevAddRec* cursorIV    = cursor->IV;
                    BasicBlock* cursorBlock = cursor->Block;
                    Statement*  cursorStmt  = cursor->Stmt;
                    cursor = nullptr; // Cannot use this below since we may add elements to "cursors" here.

                    auto createExtraCursor = [=, &numCreated](BasicBlock* block, Statement* stmt,
                                                              GenTreeLclVarCommon* use) {
                        if (use == parent)
                        {
                            return true;
                        }

                        if (!use->OperIs(GT_LCL_VAR) || (use->GetSsaNum() != storedLcl->GetSsaNum()))
                        {
                            return false;
                        }

                        Scev* iv = m_scevContext.Analyze(block, use);
                        if (iv == nullptr)
                        {
                            return false;
                        }

                        iv = m_scevContext.Simplify(iv, m_simplAssumptions);
                        assert(iv != nullptr);
                        if (!Scev::Equals(iv, cursorIV))
                        {
                            return false;
                        }

                        // Note: cannot use "cursor" after this point.
                        cursors->Emplace(block, stmt, use, cursorIV);
                        otherCursors->Emplace(block, stmt, use, cursorIV);
                        numCreated++;
                        return true;
                    };

                    if (m_loopLocals.VisitOccurrences(m_loop, storedLcl->GetLclNum(), createExtraCursor))
                    {
                        // We created cursors for all uses. Remove the IV that
                        // was feeding into the store from the list.
                        m_storesToRemove.Emplace(cursorBlock, cursorStmt, parent, nullptr);
                        std::swap(cursors->BottomRef(i), cursors->TopRef(0));
                        std::swap(otherCursors->BottomRef(i), otherCursors->TopRef(0));
                        cursors->Pop();
                        otherCursors->Pop();
                        i--;
                        break;
                    }

                    cursors->Pop(numCreated);
                    otherCursors->Pop(numCreated);
                }

                break;
            }

            Scev* parentIV = m_scevContext.Analyze(cursor->Block, parent);
            if (parentIV == nullptr)
            {
                break;
            }

            parentIV = m_scevContext.Simplify(parentIV, m_simplAssumptions);
            assert(parentIV != nullptr);
            if (!Scev::Equals(parentIV, cursor->IV))
            {
                break;
            }

            cursor->Tree = parent;
        }
    }
}

//------------------------------------------------------------------------
// CheckAdvancedCursors: Check whether the specified advanced cursors still
// represent a valid set of cursors to introduce a new primary IV for.
//
// Parameters:
//   cursors      - List of cursors that were advanced.
//   nextIV       - [out] The next derived IV from the subset of advanced
//                  cursors to now consider strength reducing.
//
// Returns:
//   True if all cursors still represent a common derived IV and would be
//   replacable by a new primary IV computing it.
//
// Remarks:
//   This function may remove cursors from m_cursors1 and m_cursors2 if it
//   decides to no longer consider some cursors for strength reduction.
//
bool StrengthReductionContext::CheckAdvancedCursors(ArrayStack<CursorInfo>* cursors, ScevAddRec** nextIV)
{
    *nextIV = nullptr;

    for (int i = 0; i < cursors->Height(); i++)
    {
        CursorInfo& cursor = cursors->BottomRef(i);

        if ((cursor.IV != nullptr) && ((*nextIV == nullptr) || Scev::Equals(cursor.IV, *nextIV)))
        {
            *nextIV = cursor.IV;
            continue;
        }

        JITDUMP("    [%d] does not match; will not advance\n", i);
        return false;
    }

    return *nextIV != nullptr;
}

//------------------------------------------------------------------------
// StaysWithinManagedObject: Check whether the specified GC-pointer add-rec can
// be guaranteed to be inside the same managed object for the whole loop.
//
// Parameters:
//   cursors - Cursors pointing to next uses that correspond to the specific add-rec.
//   addRec  - The add recurrence
//
// Returns:
//   True if we were able to prove so.
//
bool StrengthReductionContext::StaysWithinManagedObject(ArrayStack<CursorInfo>* cursors, ScevAddRec* addRec)
{
    ValueNumPair addRecStartVNP = m_scevContext.MaterializeVN(addRec->Start);
    if (!addRecStartVNP.BothDefined())
    {
        return false;
    }

    ValueNumPair   addRecStartBase    = addRecStartVNP;
    target_ssize_t offsetLiberal      = 0;
    target_ssize_t offsetConservative = 0;
    m_comp->vnStore->PeelOffsets(addRecStartBase.GetLiberalAddr(), &offsetLiberal);
    m_comp->vnStore->PeelOffsets(addRecStartBase.GetConservativeAddr(), &offsetConservative);

    if (offsetLiberal != offsetConservative)
    {
        return false;
    }

    target_ssize_t offset = offsetLiberal;

    // We only support objects here (targeting array/strings). To strength
    // reduce Span<T> accesses we need additional properties on the range
    // designated by a Span<T> that we currently do not specify, or we need to
    // prove that the byref we may form in the IV update would have been formed
    // anyway by the loop.
    if ((m_comp->vnStore->TypeOfVN(addRecStartBase.GetConservative()) != TYP_REF) ||
        (m_comp->vnStore->TypeOfVN(addRecStartBase.GetLiberal()) != TYP_REF))
    {
        return false;
    }

    // Now use the fact that we keep ARR_ADDRs in the IR when we have
    // array/string accesses.
    GenTreeArrAddr* arrAddr = nullptr;
    for (int i = 0; i < cursors->Height(); i++)
    {
        CursorInfo& cursor = cursors->BottomRef(i);
        if (cursor.Tree->gtEffectiveVal()->OperIs(GT_ARR_ADDR))
        {
            arrAddr = cursor.Tree->gtEffectiveVal()->AsArrAddr();
            break;
        }
    }

    if (arrAddr == nullptr)
    {
        return false;
    }

    unsigned arrElemSize = arrAddr->GetElemType() == TYP_STRUCT
                               ? m_comp->typGetObjLayout(arrAddr->GetElemClassHandle())->GetSize()
                               : genTypeSize(arrAddr->GetElemType());

    int64_t stepCns;
    if (!addRec->Step->GetConstantValue(m_comp, &stepCns) || ((unsigned)stepCns > arrElemSize))
    {
        return false;
    }

    BasicBlock* preheader = m_loop->EntryEdge(0)->getSourceBlock();
    if (!m_comp->optAssertionVNIsNonNull(addRecStartBase.GetConservative(), preheader->bbAssertionOut))
    {
        return false;
    }

    // We have a non-null object as the base. Check that the 'start' offset
    // looks fine. TODO: We could also use assertions on the length of the
    // array/string. E.g. if we know the length of the array is > 3, then we
    // can allow the add rec to have a later start. Maybe range check can be
    // used?
    if ((offset < 0) || (offset > arrAddr->GetFirstElemOffset()))
    {
        return false;
    }

    // Now see if we have a bound that guarantees that we iterate fewer times
    // than the array/string's length.
    ValueNum arrLengthVN = m_comp->vnStore->VNForFunc(TYP_INT, VNF_ARR_LENGTH, addRecStartBase.GetLiberal());

    for (int i = 0; i < m_backEdgeBounds.Height(); i++)
    {
        Scev* bound = m_backEdgeBounds.Bottom(i);
        if (!bound->TypeIs(TYP_INT))
        {
            // Currently cannot handle bounds that aren't 32 bit.
            continue;
        }

        // In some cases VN is powerful enough to evaluate bound <
        // ARR_LENGTH(vn) directly.
        ValueNumPair boundVN = m_scevContext.MaterializeVN(bound);
        if (boundVN.GetLiberal() != ValueNumStore::NoVN)
        {
            ValueNum relop = m_comp->vnStore->VNForFunc(TYP_INT, VNF_LT_UN, boundVN.GetLiberal(), arrLengthVN);
            if (m_scevContext.EvaluateRelop(relop) == RelopEvaluationResult::True)
            {
                return true;
            }
        }

        // In common cases VN cannot prove the above, so try a little bit
        // harder. In this case we know the bound doesn't overflow
        // (conceptually the bound is an arbitrary precision integer and a
        // negative bound does not make sense).
        int64_t boundOffset;
        Scev*   boundBase = bound->PeelAdditions(&boundOffset);

        boundOffset = static_cast<int32_t>(boundOffset);
        if (boundOffset >= 0)
        {
            // If we take the backedge >= the array length times, then we would
            // advance the addrec past the end.
            continue;
        }

        // Now try to prove that boundBase <= ARR_LENGTH(vn). Commonly
        // boundBase == ARR_LENGTH(vn) exactly, but it may also have a
        // dominating subsuming compare before the loop due to loop cloning.
        ValueNumPair boundBaseVN = m_scevContext.MaterializeVN(boundBase);
        if (boundBaseVN.GetLiberal() != ValueNumStore::NoVN)
        {
            ValueNum relop = m_comp->vnStore->VNForFunc(TYP_INT, VNF_LE, boundBaseVN.GetLiberal(), arrLengthVN);
            if (m_scevContext.EvaluateRelop(relop) == RelopEvaluationResult::True)
            {
                return true;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------
// TryReplaceUsesWithNewPrimaryIV: Perform final sanity checks before
// introducing a new primary IV and replacing the uses represented by the
// specified cursors with it.
//
// Parameters:
//   cursors - List of cursors representing uses to replace
//   iv      - IV to introduce a primary IV for
//
// Returns:
//   True if the IV was introduced and uses were rewritten.
//
bool StrengthReductionContext::TryReplaceUsesWithNewPrimaryIV(ArrayStack<CursorInfo>* cursors, ScevAddRec* iv)
{
    int64_t stepCns;
    if (!iv->Step->GetConstantValue(m_comp, &stepCns))
    {
        // For other cases it's non-trivial to know if we can materialize
        // the value as IR in the step block.
        JITDUMP("    Skipping: step value is not a constant\n");
        return false;
    }

    BasicBlock* insertionPoint = FindUpdateInsertionPoint(cursors);
    if (insertionPoint == nullptr)
    {
        JITDUMP("    Skipping: could not find a legal insertion point for the new IV update\n");
        return false;
    }

    BasicBlock* preheader = m_loop->EntryEdge(0)->getSourceBlock();
    GenTree*    initValue = m_scevContext.Materialize(iv->Start);
    if (initValue == nullptr)
    {
        JITDUMP("    Skipping: init value could not be materialized\n");
        return false;
    }

    JITDUMP("    Strength reducing\n");

    GenTree* stepValue = m_scevContext.Materialize(iv->Step);
    assert(stepValue != nullptr);

    unsigned   newPrimaryIV = m_comp->lvaGrabTemp(false DEBUGARG("Strength reduced derived IV"));
    GenTree*   initStore    = m_comp->gtNewTempStore(newPrimaryIV, initValue);
    Statement* initStmt     = m_comp->fgNewStmtFromTree(initStore);
    m_comp->fgInsertStmtNearEnd(preheader, initStmt);

    JITDUMP("    Inserting init statement in preheader " FMT_BB "\n", preheader->bbNum);
    DISPSTMT(initStmt);

    GenTree* nextValue =
        m_comp->gtNewOperNode(GT_ADD, iv->Type, m_comp->gtNewLclVarNode(newPrimaryIV, iv->Type), stepValue);
    GenTree*   stepStore = m_comp->gtNewTempStore(newPrimaryIV, nextValue);
    Statement* stepStmt  = m_comp->fgNewStmtFromTree(stepStore);
    m_comp->fgInsertStmtNearEnd(insertionPoint, stepStmt);

    JITDUMP("    Inserting step statement in " FMT_BB "\n", insertionPoint->bbNum);
    DISPSTMT(stepStmt);

    // Replace uses.
    for (int i = 0; i < cursors->Height(); i++)
    {
        CursorInfo& cursor = cursors->BottomRef(i);
        GenTree*    newUse = m_comp->gtNewLclVarNode(newPrimaryIV, iv->Type);

        JITDUMP("    Replacing use [%06u] with [%06u]. Before:\n", Compiler::dspTreeID(cursor.Tree),
                Compiler::dspTreeID(newUse));
        DISPSTMT(cursor.Stmt);

        GenTree** use = nullptr;
        if (cursor.Stmt->GetRootNode() == cursor.Tree)
        {
            use = cursor.Stmt->GetRootNodePointer();
        }
        else
        {
            cursor.Tree->gtGetParent(&use);
            assert(use != nullptr);
        }

        GenTree* sideEffects = nullptr;
        m_comp->gtExtractSideEffList(cursor.Tree, &sideEffects);
        if (sideEffects != nullptr)
        {
            *use = m_comp->gtNewOperNode(GT_COMMA, newUse->TypeGet(), sideEffects, newUse);
        }
        else
        {
            *use = newUse;
        }
        JITDUMP("\n      After:\n\n");
        DISPSTMT(cursor.Stmt);

        m_comp->gtSetStmtInfo(cursor.Stmt);
        m_comp->fgSetStmtSeq(cursor.Stmt);
        m_comp->gtUpdateStmtSideEffects(cursor.Stmt);
    }

    if (m_storesToRemove.Height() > 0)
    {
        JITDUMP("    Removing useless stores\n");
        for (int i = 0; i < m_storesToRemove.Height(); i++)
        {
            CursorInfo& toRemove = m_storesToRemove.BottomRef(i);
            JITDUMP("      Removing [%06u]\n", Compiler::dspTreeID(toRemove.Tree));
            toRemove.Tree->AsLclVarCommon()->Data() =
                m_comp->gtNewZeroConNode(genActualType(toRemove.Tree->AsLclVarCommon()->Data()->TypeGet()));
            m_comp->gtSetStmtInfo(toRemove.Stmt);
            m_comp->fgSetStmtSeq(toRemove.Stmt);
            m_comp->gtUpdateStmtSideEffects(toRemove.Stmt);
        }
    }

    return true;
}

//------------------------------------------------------------------------
// FindUpdateInsertionPoint: Find a block at which to insert the "self-update"
// of a new primary IV introduced by strength reduction.
//
// Parameters:
//   cursors - The list of cursors pointing to uses that are being replaced by
//             the new IV
//
// Returns:
//   Basic block; the insertion point is the end (before a potential
//   terminator) of this basic block. May return null if no insertion point
//   could be found.
//
BasicBlock* StrengthReductionContext::FindUpdateInsertionPoint(ArrayStack<CursorInfo>* cursors)
{
    // Find insertion point. It needs to post-dominate all uses we are going to
    // replace and it needs to dominate all backedges.
    // TODO-CQ: Canonicalizing backedges would make this simpler and work in
    // more cases.

    BasicBlock* insertionPoint = nullptr;
    for (FlowEdge* backEdge : m_loop->BackEdges())
    {
        if (insertionPoint == nullptr)
        {
            insertionPoint = backEdge->getSourceBlock();
        }
        else
        {
            insertionPoint = m_comp->m_domTree->Intersect(insertionPoint, backEdge->getSourceBlock());
        }
    }

    while ((insertionPoint != nullptr) && m_loop->ContainsBlock(insertionPoint) &&
           m_loop->MayExecuteBlockMultipleTimesPerIteration(insertionPoint))
    {
        insertionPoint = insertionPoint->bbIDom;
    }

    if ((insertionPoint == nullptr) || !m_loop->ContainsBlock(insertionPoint))
    {
        return nullptr;
    }

    for (int i = 0; i < cursors->Height(); i++)
    {
        CursorInfo& cursor = cursors->BottomRef(i);

        if (insertionPoint == cursor.Block)
        {
            if (insertionPoint->HasTerminator() && (cursor.Stmt == insertionPoint->lastStmt()))
            {
                return nullptr;
            }
        }
        else
        {
            if (!m_loop->IsPostDominatedOnLoopIteration(cursor.Block, insertionPoint))
            {
                return nullptr;
            }
        }
    }

    return insertionPoint;
}

//------------------------------------------------------------------------
// optRemoveUnusedIVs: Remove IVs that are only used for self-updates.
//
// Parameters:
//   loop       - The loop
//   loopLocals - Locals of the loop
//
// Returns:
//   True if any primary IV was removed.
//
bool Compiler::optRemoveUnusedIVs(FlowGraphNaturalLoop* loop, LoopLocalOccurrences* loopLocals)
{
    JITDUMP("  Now looking for unnecessary primary IVs\n");

    unsigned numRemoved = 0;
    for (Statement* stmt : loop->GetHeader()->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        unsigned lclNum = stmt->GetRootNode()->AsLclVarCommon()->GetLclNum();
        JITDUMP("  V%02u", lclNum);
        if (optPrimaryIVHasNonLoopUses(lclNum, loop, loopLocals))
        {
            JITDUMP(" has non-loop uses, cannot remove\n");
            continue;
        }

        auto visit = [=](BasicBlock* block, Statement* stmt) {
            return optIsUpdateOfIVWithoutSideEffects(stmt->GetRootNode(), lclNum);
        };

        if (!loopLocals->VisitStatementsWithOccurrences(loop, lclNum, visit))
        {
            JITDUMP(" has essential uses, cannot remove\n");
            continue;
        }

        JITDUMP(" has no essential uses and will be removed\n", lclNum);
        auto remove = [=](BasicBlock* block, Statement* stmt) {
            JITDUMP("  Removing " FMT_STMT "\n", stmt->GetID());
            fgRemoveStmt(block, stmt);
            return true;
        };

        loopLocals->VisitStatementsWithOccurrences(loop, lclNum, remove);
        numRemoved++;
        loopLocals->Invalidate(loop);
    }

    Metrics.UnusedIVsRemoved += numRemoved;
    return numRemoved > 0;
}

//------------------------------------------------------------------------
// optIsUpdateOfIVWithoutSideEffects: Check if a tree is an update of a
// specific local with no other side effects.
//
// Returns:
//   True if so.
//
bool Compiler::optIsUpdateOfIVWithoutSideEffects(GenTree* tree, unsigned lclNum)
{
    if (!tree->OperIsLocalStore())
    {
        return false;
    }

    GenTreeLclVarCommon* store = tree->AsLclVarCommon();
    if (store->GetLclNum() != lclNum)
    {
        // Store that uses the local as a source; this primary IV is used
        return false;
    }

    if ((store->Data()->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        // Primary IV may be used inside a side effect
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// optInductionVariables: Try and optimize induction variables in the method.
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optInductionVariables()
{
    JITDUMP("*************** In optInductionVariables()\n");

#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitEnableInductionVariableOptsRange());

    if (!s_range.Contains(info.compMethodHash()))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    if (!fgMightHaveNaturalLoops)
    {
        JITDUMP("  Skipping since this method has no natural loops\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool changed = false;

    optReachableBitVecTraits = nullptr;
    m_dfsTree                = fgComputeDfs();
    m_domTree                = FlowGraphDominatorTree::Build(m_dfsTree);
    m_loops                  = FlowGraphNaturalLoops::Find(m_dfsTree);

    LoopLocalOccurrences loopLocals(m_loops);

    ScalarEvolutionContext scevContext(this);
    JITDUMP("Optimizing induction variables:\n");

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Processing ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        // We may not have preheaders here since RBO/assertion prop may have changed
        // the flow graph
        BasicBlock* preheader = loop->GetPreheader();
        if (preheader == nullptr)
        {
            JITDUMP("  No preheader; skipping\n");
            continue;
        }

        StrengthReductionContext strengthReductionContext(this, scevContext, loop, loopLocals);
        if (strengthReductionContext.TryStrengthReduce())
        {
            Metrics.LoopsStrengthReduced++;
            changed = true;
        }

        if (optMakeLoopDownwardsCounted(scevContext, loop, &loopLocals))
        {
            Metrics.LoopsMadeDownwardsCounted++;
            changed = true;
        }

        // IV widening is generally only profitable for x64 because arm64
        // addressing modes can include the zero/sign-extension of the index
        // for free.
#if defined(TARGET_XARCH) && defined(TARGET_64BIT)
        if (optWidenIVs(scevContext, loop, &loopLocals))
        {
            Metrics.LoopsIVWidened++;
            changed = true;
        }
#endif

        if (optRemoveUnusedIVs(loop, &loopLocals))
        {
            changed = true;
        }
    }

    fgInvalidateDfsTree();

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
