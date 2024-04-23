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

struct LocalOccurrence
{
    BasicBlock* Block;
    Statement*  Stmt;

    LocalOccurrence(BasicBlock* block, Statement* stmt)
        : Block(block)
        , Stmt(stmt)
    {
    }
};

//------------------------------------------------------------------------
// optIsIVWideningProfitable: Check to see if IV widening is profitable.
//
// Parameters:
//   lclNum           - The primary induction variable
//   initBlock        - The block in where the new IV would be initialized
//   initedToConstant - Whether or not the new IV will be initialized to a constant
//   loop             - The loop
//   loopOccurrences  - IR locations where "lclNum" occurs
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
bool Compiler::optIsIVWideningProfitable(unsigned                     lclNum,
                                         BasicBlock*                  initBlock,
                                         bool                         initedToConstant,
                                         FlowGraphNaturalLoop*        loop,
                                         ArrayStack<LocalOccurrence>& loopOccurrences)
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

    for (int i = 0; i < loopOccurrences.Height(); i++)
    {
        LocalOccurrence& occurrence = loopOccurrences.BottomRef(i);
        BasicBlock*      block      = occurrence.Block;
        Statement*       stmt       = occurrence.Stmt;

        int numExtensions = 0;
        for (GenTree* node : stmt->TreeList())
        {
            if (!node->OperIs(GT_CAST))
            {
                continue;
            }

            GenTreeCast* cast = node->AsCast();
            if ((cast->gtCastType != TYP_LONG) || !cast->IsUnsigned() || cast->gtOverflow())
            {
                continue;
            }

            GenTree* op = cast->CastOp();
            if (!op->OperIs(GT_LCL_VAR) || (op->AsLclVarCommon()->GetLclNum() != lclNum))
            {
                continue;
            }

            // If this is already the source of a store then it is going to be
            // free in our backends regardless.
            GenTree* parent = node->gtGetParent(nullptr);
            if ((parent != nullptr) && parent->OperIs(GT_STORE_LCL_VAR))
            {
                continue;
            }

            numExtensions++;
        }

        if (numExtensions > 0)
        {
            JITDUMP("  Found %d zero extensions in " FMT_STMT "\n", numExtensions, stmt->GetID());

            savedSize += numExtensions * ExtensionSize;
            savedCost += numExtensions * block->getBBWeight(this) * ExtensionCost;
        }
    }

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

template <typename TFunctor>
void Compiler::optFindLocalOccurrences(BasicBlock* block, unsigned lclNum, TFunctor func)
{
    for (Statement* stmt : block->NonPhiStatements())
    {
        int numExtensions = 0;
        for (GenTree* node : stmt->TreeList())
        {
            if (node->OperIsLocal() && (node->AsLclVarCommon()->GetLclNum() == lclNum))
            {
                // TODO-Bug: Disallow promoted fields or handle them specially?
                func(stmt);
                break;
            }
        }
    }
}

bool Compiler::optWidenPrimaryIV(FlowGraphNaturalLoop*        loop,
                                 unsigned                     lclNum,
                                 ScevAddRec*                  addRec,
                                 ArrayStack<LocalOccurrence>& loopOccurrences)
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

    if (!optIsIVWideningProfitable(lclNum, initBlock, initToConstant, loop, loopOccurrences))
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

    JITDUMP("    Replacing in the loop; %d statements with appearences\n", loopOccurrences.Height());
    for (int i = 0; i < loopOccurrences.Height(); i++)
    {
        Statement* stmt = loopOccurrences.BottomRef(i).Stmt;
        JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lclNum, newLclNum, dspTreeID(stmt->GetRootNode()));
        DISPSTMT(stmt);
        JITDUMP("\n");
        optReplaceWidenedIV(lclNum, SsaConfig::RESERVED_SSA_NUM, newLclNum, stmt);
    }

    optSinkWidenedIV(lclNum, newLclNum, loop);
    return true;
}

struct IVUseInfo;

struct IVUseListNode
{
    BasicBlock*    Block;
    Statement*     Stmt;
    GenTree*       Tree;
    IVUseListNode* Next;
    IVUseInfo*     Parent;
};

struct IVUseInfo
{
    ScevAddRec*    IV;
    IVUseListNode* Uses = nullptr;
    // LclNum of the IV if this IV is a primary IV.
    unsigned PrimaryLclNum          = BAD_VAR_NUM;
    bool     TriedStrengthReduction = false;

    IVUseInfo(ScevAddRec* iv)
        : IV(iv)
    {
    }
};

void Compiler::optScoreNewPrimaryIV(FlowGraphNaturalLoop*  loop,
                                    ArrayStack<IVUseInfo>& ivs,
                                    const IVUseInfo&       iv,
                                    BasicBlock*            stepUpdateBlock,
                                    weight_t*              cycleImprovement,
                                    weight_t*              sizeImprovement)
{
    *cycleImprovement = 0;
    *sizeImprovement  = 0;

    for (IVUseListNode* use = iv.Uses; use != nullptr; use = use->Next)
    {
        // We remove the cost of the tree and add a use of a local (-1).
        *cycleImprovement += (use->Tree->GetCostEx() - 1) * use->Block->getBBWeight(this);
        *sizeImprovement += use->Tree->GetCostSz() - 1;
    }

    // Now cost adding a new initialization of this IV in the preheader.
    BasicBlock* preheader       = loop->EntryEdge(0)->getSourceBlock();
    weight_t    preheaderWeight = preheader->getBBWeight(this);
    if (iv.IV->Start->OperIs(ScevOper::Constant, ScevOper::Local))
    {
        // Cost as 1 cycle, 3 bytes for storing
        *cycleImprovement -= 1 * preheaderWeight;
        *sizeImprovement -= 3;
    }
    else
    {
        // TODO: Proper costing
        *cycleImprovement -= 1000 * preheaderWeight;
        *sizeImprovement -= 1000;
    }

    // Now cost the update
    weight_t stepBlockWeight = stepUpdateBlock->getBBWeight(this);
    if (iv.IV->Step->OperIs(ScevOper::Constant))
    {
        // Cost as 3 cycles, 3 bytes for updating
        *cycleImprovement -= 3 * stepBlockWeight;
        *sizeImprovement -= 3;
    }
    else
    {
        *cycleImprovement -= 1000 * stepBlockWeight;
        *sizeImprovement -= 1000;
    }

    *cycleImprovement /= fgFirstBB->getBBWeight(this);
}

//------------------------------------------------------------------------
// optStrengthReduce: Find derived IVs in "ivs" to turn into primary IVs.
//
// Parameters:
//   loop - The loop with the IVs
//   ivs  - Information about IVs used inside the loop
//
void Compiler::optStrengthReduce(FlowGraphNaturalLoop*   loop,
                                 ScalarEvolutionContext& scevContext,
                                 ArrayStack<IVUseInfo>&  ivs)
{
    JITDUMP("Found %d different induction variables. Evaluating whether to strength reduce derived IVs.\n",
            ivs.Height());

    if (loop->BackEdges().size() != 1)
    {
        // TODO-CQ: With dominators we can still find a suitable block to place
        // the update (i.e. any block that dominates all latches).
        JITDUMP("  ..no, has %zu backedges\n", loop->BackEdges().size());
        return;
    }

    BasicBlock* latch = loop->BackEdge(0)->getSourceBlock();
    // If the only latch is also an exiting block then we know that it
    // post-dominates all blocks within the loop, so placing the update in that
    // block will always be correct. Otherwise bail.
    // TODO-CQ: With dominators we can also do better here.
    if (!latch->KindIs(BBJ_COND) ||
        (!latch->TrueTargetIs(loop->GetHeader()) && !latch->FalseTargetIs(loop->GetHeader())))
    {
        JITDUMP("  ..no, latch does not post-dominate loop\n");
        return;
    }

    for (int iteration = 1;; iteration++)
    {
        if (iteration > 1)
        {
            JITDUMP("\n  Iteration %d\n", iteration + 1);
        }

        IVUseInfo* bestIV               = nullptr;
        weight_t   bestCycleImprovement = 0;
        weight_t   bestSizeImprovement  = 0;

        for (int i = 0; i < ivs.Height(); i++)
        {
            IVUseInfo& iv = ivs.BottomRef(i);

#ifdef DEBUG
            if (verbose)
            {
                printf("  [%d]: %s ", i, varTypeName(iv.IV->Type));
                iv.IV->Dump(this);
                int numUses = 0;
                for (IVUseListNode* node = iv.Uses; node != nullptr; node = node->Next)
                {
                    numUses++;
                }

                if (iv.PrimaryLclNum != BAD_VAR_NUM)
                    printf(" (primary IV V%02u,", iv.PrimaryLclNum);
                else
                    printf(" (derived IV,");

                printf(" %d uses)", numUses);

                const char* sep = "\n    Uses: ";
                for (IVUseListNode* node = iv.Uses; node != nullptr; node = node->Next)
                {
                    printf("%s[%06u]", sep, dspTreeID(node->Tree));
                    sep = " ";
                }
                printf("\n");
            }
#endif

            if (iv.PrimaryLclNum != BAD_VAR_NUM)
            {
                continue;
            }

            if (iv.TriedStrengthReduction)
            {
                JITDUMP("    Skipped; already tried\n");
                continue;
            }

            weight_t cycleImprovement;
            weight_t sizeImprovement;
            optScoreNewPrimaryIV(loop, ivs, iv, latch, &cycleImprovement, &sizeImprovement);

            const weight_t ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT = 2;
            const weight_t ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT = 0.01;

            JITDUMP("    Estimated cycle improvement: " FMT_WT " cycles per invocation\n", cycleImprovement);
            JITDUMP("    Estimated size improvement: " FMT_WT " bytes\n", sizeImprovement);

            if ((cycleImprovement > 0) &&
                ((cycleImprovement * ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT) >= -sizeImprovement))
            {
                JITDUMP("    Candidate for new primary IV (cycle improvement)\n");
            }
            else if ((sizeImprovement > 0) &&
                     ((sizeImprovement * ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT) >= -cycleImprovement))
            {
                JITDUMP("    Candidate for new primary IV (size improvement)\n");
            }
            else if (compStressCompile(STRESS_STRENGTH_REDUCTION_COST, 15))
            {
                JITDUMP("    Candidate for new primary IV (stress)\n");
            }
            else
            {
                continue;
            }

            if ((bestIV == nullptr) || (cycleImprovement > bestCycleImprovement) ||
                ((fabs(cycleImprovement - bestCycleImprovement) < 0.01) && (sizeImprovement > bestSizeImprovement)))
            {
                bestIV               = &iv;
                bestCycleImprovement = cycleImprovement;
                sizeImprovement      = bestSizeImprovement;
            }
        }

        if (bestIV == nullptr)
        {
            break;
        }

        JITDUMP("\n  Introducing primary IV for %s ", varTypeName(bestIV->IV->Type));
        DBEXEC(VERBOSE, bestIV->IV->Dump(this));
        JITDUMP("\n");

        // TODO-CQ: For other cases it's non-trivial to know if we can
        // materialize the value as IR in the step block.
        if (!bestIV->IV->Step->OperIs(ScevOper::Constant))
        {
            JITDUMP("      Skipping: step value is not a constant\n");
            bestIV->TriedStrengthReduction = true;
            continue;
        }

        // TODO-CQ: We currently store the step update right before the latch
        // condition. If the latch condition has any uses, then we cannot
        // replace those. We should be able to generatelize this to save the
        // "pre-increment" value if necesary (same logic is needed for more
        // general handling). Or we could simplify leave it alone. This just
        // bails out in these cases.
        bool hasAnyLatchUses = false;
        for (IVUseListNode* node = bestIV->Uses; node != nullptr; node = node->Next)
        {
            if (node->Stmt == latch->lastStmt())
            {
                hasAnyLatchUses = true;
                break;
            }
        }

        if (hasAnyLatchUses)
        {
            JITDUMP("      Skipping: latch has use of IV\n");
            bestIV->TriedStrengthReduction = true;
            continue;
        }

        BasicBlock* preheader = loop->EntryEdge(0)->getSourceBlock();
        GenTree*    initValue = scevContext.Materialize(bestIV->IV->Start);
        if (initValue == nullptr)
        {
            JITDUMP("      Skipping: init value could not be materialized\n");
            bestIV->TriedStrengthReduction = true;
            continue;
        }

        GenTree* stepValue = scevContext.Materialize(bestIV->IV->Step);
        assert(stepValue != nullptr);

        unsigned   newPrimaryIV = lvaGrabTemp(false DEBUGARG("Strength reduced derived IV"));
        GenTree*   initStore    = gtNewTempStore(newPrimaryIV, initValue);
        Statement* initStmt     = fgNewStmtFromTree(initStore);
        fgInsertStmtNearEnd(preheader, initStmt);

        JITDUMP("      Inserting init statement in preheader " FMT_BB "\n", preheader->bbNum);
        DISPSTMT(initStmt);

        GenTree* nextValue =
            gtNewOperNode(GT_ADD, stepValue->TypeGet(), gtNewLclVarNode(newPrimaryIV, bestIV->IV->Type), stepValue);
        GenTree*   stepStore = gtNewTempStore(newPrimaryIV, nextValue);
        Statement* stepStmt  = fgNewStmtFromTree(stepStore);
        fgInsertStmtNearEnd(latch, stepStmt);

        JITDUMP("      Inserting step statement in latch " FMT_BB "\n", latch->bbNum);
        DISPSTMT(stepStmt);

        // Replace uses.
        for (IVUseListNode* node = bestIV->Uses; node != nullptr; node = node->Next)
        {
            GenTree* newUse = gtNewLclVarNode(newPrimaryIV, bestIV->IV->Type);

            JITDUMP("\n      Replacing use [%06u] with [%06u]. Before:\n", dspTreeID(node->Tree), dspTreeID(newUse));
            DISPSTMT(node->Stmt);

            GenTree** use = nullptr;
            if (node->Stmt->GetRootNode() == node->Tree)
            {
                use = node->Stmt->GetRootNodePointer();
            }
            else
            {
                node->Tree->gtGetParent(&use);
                assert(use != nullptr);
            }

            GenTree* sideEffects = nullptr;
            gtExtractSideEffList(node->Tree, &sideEffects);
            if (sideEffects != nullptr)
            {
                *use = gtNewOperNode(GT_COMMA, newUse->TypeGet(), sideEffects, newUse);
            }
            else
            {
                *use = newUse;
            }
            JITDUMP("\n      After:\n\n");
            DISPSTMT(node->Stmt);

            gtSetStmtInfo(node->Stmt);
            fgSetStmtSeq(node->Stmt);
            gtUpdateStmtSideEffects(node->Stmt);
        }

        break;
    }
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

    m_dfsTree = fgComputeDfs();
    m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);

    ScalarEvolutionContext scevContext(this);
    JITDUMP("Optimizing induction variables:\n");
    ArrayStack<LocalOccurrence> ivOccurrences(getAllocator(CMK_LoopIVOpts));
    ArrayStack<IVUseInfo>       allIVs(getAllocator(CMK_LoopIVOpts));

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Processing ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        allIVs.Reset();
        int numWidened         = 0;
        int numStrengthReduced = 0;

        loop->VisitLoopBlocks([this, loop, &scevContext, &allIVs](BasicBlock* block) {
            for (Statement* stmt : block->Statements())
            {
                DISPSTMT(stmt);

                // Iterate it backwards to add uses in a post-order so that
                // changing the first uses before the latter ones does not
                // affect them.
                for (GenTree* tree = stmt->GetRootNode(); tree != nullptr; tree = tree->gtPrev)
                {
                    JITDUMP("  [%06u] => ", dspTreeID(tree));

                    Scev* scev = scevContext.Analyze(block, tree);
                    if (scev == nullptr)
                    {
                        JITDUMP("Cannot analyze\n");
                        continue;
                    }

                    DBEXEC(verbose, scev->Dump(this));
                    JITDUMP(" => ");

                    Scev* simplifiedScev = scevContext.Simplify(scev);

                    DBEXEC(verbose, simplifiedScev->Dump(this));
                    JITDUMP("\n");

                    if (!simplifiedScev->OperIs(ScevOper::AddRec))
                    {
                        continue;
                    }

                    IVUseInfo* foundIV = nullptr;
                    for (int j = 0; j < allIVs.Height(); j++)
                    {
                        IVUseInfo& iv = allIVs.BottomRef(j);
                        if (Scev::Equals(simplifiedScev, allIVs.BottomRef(j).IV))
                        {
                            foundIV = &iv;
                            break;
                        }
                    }

                    if (foundIV == nullptr)
                    {
                        allIVs.Push(IVUseInfo(static_cast<ScevAddRec*>(simplifiedScev)));
                        foundIV = &allIVs.TopRef(0);
                    }

                    if (tree->IsPhiDefn() && (block == loop->GetHeader()))
                    {
                        if (foundIV->PrimaryLclNum == BAD_VAR_NUM)
                        {
                            foundIV->PrimaryLclNum = tree->AsLclVarCommon()->GetLclNum();
                        }

                        continue;
                    }

                    if (tree->IsPhiNode() || tree->OperIsLocalStore())
                    {
                        continue;
                    }

                    IVUseListNode* newNode = new (this, CMK_LoopIVOpts) IVUseListNode;
                    newNode->Block         = block;
                    newNode->Stmt          = stmt;
                    newNode->Tree          = tree;
                    newNode->Next          = foundIV->Uses;
                    newNode->Parent        = nullptr;
                    foundIV->Uses          = newNode;
                }

                JITDUMP("\n");
            }

            return BasicBlockVisit::Continue;
        });

        if (loop->ExitEdges().size() == 1)
        {
            JITDUMP("Loop has one exit edge\n");
            BasicBlock* exiting = loop->ExitEdge(0)->getSourceBlock();
            if (exiting->KindIs(BBJ_COND))
            {
                Statement* lastStmt = exiting->lastStmt();
                GenTree*   lastExpr = lastStmt->GetRootNode();
                assert(lastExpr->OperIs(GT_JTRUE));
                GenTree* cond = lastExpr->gtGetOp1();
                if (cond->OperIsCompare() && varTypeIsIntegralOrI(cond->gtGetOp1()))
                {
                    Scev* op1 = scevContext.Analyze(exiting, cond->gtGetOp1());
                    Scev* op2 = scevContext.Analyze(exiting, cond->gtGetOp2());
                    if ((op1 != nullptr) && (op2 != nullptr) && !varTypeIsGC(op1->Type) && !varTypeIsGC(op2->Type))
                    {
                        Scev*      a      = nullptr;
                        Scev*      b      = nullptr;
                        genTreeOps exitOp = loop->ContainsBlock(exiting->GetTrueTarget())
                                                ? GenTree::ReverseRelop(cond->gtOper)
                                                : cond->gtOper;
                        if (exitOp == GT_LT)
                        {
                            a = op1;
                            b = scevContext.NewBinop(ScevOper::Add, op2, scevContext.NewConstant(op2->Type, -1));
                        }
                        else if (exitOp == GT_LE)
                        {
                            a = op1;
                            b = op2;
                        }
                        else if (exitOp == GT_GT)
                        {
                            a = op2;
                            b = scevContext.NewBinop(ScevOper::Add, op1, scevContext.NewConstant(op2->Type, -1));
                        }
                        else if (exitOp == GT_GE)
                        {
                            a = op2;
                            b = op1;
                        }

                        if ((a != nullptr) && (b != nullptr))
                        {
                            Scev* sub =
                                scevContext.NewBinop(ScevOper::Add, a,
                                                     scevContext.NewBinop(ScevOper::Mul, b,
                                                                          scevContext.NewConstant(b->Type, -1)));
                            Scev* simplifiedSub = scevContext.Simplify(sub);
                            JITDUMP("Trip count subtraction is: ");
                            DBEXEC(VERBOSE, sub->Dump(this));
                            JITDUMP(" => ");
                            DBEXEC(VERBOSE, simplifiedSub->Dump(this));
                            JITDUMP("\n");

                            if (simplifiedSub->OperIs(ScevOper::AddRec))
                            {
                                ScevAddRec* tripCountAddRec = (ScevAddRec*)simplifiedSub;
                                int64_t     stepCns;
                                if (tripCountAddRec->Step->GetConstantValue(this, &stepCns) && (stepCns < 0))
                                {
                                    JITDUMP("Trip count is %s", stepCns != -1 ? "(" : "");
                                    DBEXEC(VERBOSE, tripCountAddRec->Start->Dump(this));
                                    if (stepCns != -1)
                                    {
                                        JITDUMP(") / %d\n", (int)-stepCns);
                                    }
                                    else
                                    {
                                        JITDUMP("\n");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        optStrengthReduce(loop, scevContext, allIVs);
        changed = true;

        // for (int i = 0; i < allIVs.Height(); i++)
        //{
        //     const IVUseInfo& info = allIVs.Bottom(i);
        //     if (info.PrimaryLclNum != BAD_VAR_NUM)
        //     {
        //         for (IVUseListNode* node = info.Uses; node != nullptr; node = node->Next)
        //         {
        //             JITDUMP("Checking [%06u]\n", node->Tree->gtTreeID);
        //             assert(node->Tree->OperIsScalarLocal() && (node->Tree->AsLclVarCommon()->GetLclNum() ==
        //             info.PrimaryLclNum));
        //         }
        //     }
        // }

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

            if (!scev->OperIs(ScevOper::AddRec))
            {
                JITDUMP("  Not an addrec\n");
                continue;
            }

            JITDUMP("  => ");
            DBEXEC(verbose, scev->Dump(this));
            JITDUMP("\n");

            ScevAddRec* addRec = (ScevAddRec*)scev;

            GenTreeLclVarCommon* lcl = stmt->GetRootNode()->AsLclVarCommon();
            JITDUMP("  V%02u is a primary induction variable in " FMT_LP "\n", lcl->GetLclNum(), loop->GetIndex());

            Scev* simplifiedAddRec = scevContext.Simplify(addRec);

            ivOccurrences.Reset();
            loop->VisitLoopBlocks([=, &ivOccurrences](BasicBlock* block) {
                optFindLocalOccurrences(block, lcl->GetLclNum(), [=, &ivOccurrences](Statement* stmt) {
                    ivOccurrences.Emplace(block, stmt);
                });

                return BasicBlockVisit::Continue;
            });

            if (optWidenPrimaryIV(loop, lcl->GetLclNum(), addRec, ivOccurrences))
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

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
