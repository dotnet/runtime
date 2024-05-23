// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code to optimize induction variables in loops based on
// scalar evolution analysis (see scev.h and scev.cpp for more information
// about the scalar evolution analysis).
//
// Currently the only optimization done is widening of primary induction
// variables from 32 bits into 64 bits. This is generally only profitable on
// x64 that does not allow zero extension of 32-bit values in addressing modes
// (in contrast, arm64 does have the capability of including zero extensions in
// addressing modes). For x64 this saves a zero extension for every array
// access inside the loop, in exchange for some widening or narrowing stores
// outside the loop:
//   - To make sure the new widened IV starts at the right value it is
//   initialized to the value of the narrow IV outside the loop (either in the
//   preheader or at the def location of the narrow IV). Usually the start
//   value is a constant, in which case the widened IV is just initialized to
//   the constant value.
//   - If the narrow IV is used after the loop we need to store it back from
//   the widened IV in the exits. We depend on liveness sets to figure out
//   which exits to insert IR into.
//
// These steps ensure that the wide IV has the right value to begin with and
// the old narrow IV still has the right value after the loop. Additionally,
// we must replace every use of the narrow IV inside the loop with the widened
// IV. This is done by a traversal of the IR inside the loop. We do not
// actually widen the uses of the IV; rather, we keep all uses and defs as
// 32-bit, which the backend is able to handle efficiently on x64. Because of
// this we do not need to worry about overflow.
//

#include "jitpch.h"
#include "scev.h"

// Data structure that keeps track of local occurrences inside loops.
class LoopLocalOccurrences
{
    struct Occurrence
    {
        BasicBlock*          Block;
        Statement*           Statement;
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
//   TFunc - Functor of type bool(Block*, Statement*, GenTree*)
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
    if (!VisitOccurrences(loop, lclNum, [](BasicBlock* block, Statement* stmt, GenTree* tree) {
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

    // Currently we only do IV widening which generally is only profitable for
    // x64 because arm64 addressing modes can include the zero/sign-extension
    // of the index for free.
#if defined(TARGET_XARCH) && defined(TARGET_64BIT)
    m_dfsTree = fgComputeDfs();
    m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);

    LoopLocalOccurrences loopLocals(m_loops);

    ScalarEvolutionContext scevContext(this);
    JITDUMP("Optimizing induction variables:\n");

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Processing ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        int numWidened = 0;

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
            if (lclDsc->lvIsStructField && loopLocals.HasAnyOccurrences(loop, lclDsc->lvParentLcl))
            {
                JITDUMP("  V%02u is a struct field whose parent local V%02u has occurrences inside the loop\n", lclNum,
                        lclDsc->lvParentLcl);
                continue;
            }

            if (optWidenPrimaryIV(loop, lclNum, addRec, &loopLocals))
            {
                numWidened++;
                changed = true;
            }
        }

        Metrics.WidenedIVs += numWidened;
        if (numWidened > 0)
        {
            Metrics.LoopsIVWidened++;
        }
    }

    fgInvalidateDfsTree();
#endif

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
