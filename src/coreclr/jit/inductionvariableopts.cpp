// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code to analyze how the value of induction variables
// evolve (scalar evolution analysis) and to do optimizations based on it.
// Currently the only optimization done is IV widening.
// The scalar evolution analysis is inspired by "Michael Wolfe. 1992. Beyond
// induction variables." and also by LLVM's scalar evolution.

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

//------------------------------------------------------------------------
// optIsIVWideningProfitable: Check to see if IV widening is profitable.
//
// Parameters:
//   lclNum           - The primary induction variable
//   initBlock        - The block in where the new IV would be initialized
//   initedToConstant - Whether or not the new IV will be initialized to a constant
//   loop             - The loop
//   ivUses           - Statements in which "lclNum" appears will be added to this list
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
bool Compiler::optIsIVWideningProfitable(unsigned                lclNum,
                                         BasicBlock*             initBlock,
                                         bool                    initedToConstant,
                                         FlowGraphNaturalLoop*   loop,
                                         ArrayStack<Statement*>& ivUses)
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

    loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* stmt : block->NonPhiStatements())
        {
            bool hasUse        = false;
            int  numExtensions = 0;
            for (GenTree* node : stmt->TreeList())
            {
                if (!node->OperIs(GT_CAST))
                {
                    hasUse |= node->OperIsLocal() && (node->AsLclVarCommon()->GetLclNum() == lclNum);
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

            if (hasUse)
            {
                ivUses.Push(stmt);
            }

            if (numExtensions > 0)
            {
                JITDUMP("  Found %d zero extensions in " FMT_STMT "\n", numExtensions, stmt->GetID());

                savedSize += numExtensions * ExtensionSize;
                savedCost += numExtensions * block->getBBWeight(this) * ExtensionCost;
            }
        }

        return BasicBlockVisit::Continue;
    });

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
            : GenTreeVisitor(comp), m_lclNum(lclNum), m_ssaNum(ssaNum), m_newLclNum(newLclNum)
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
//   SSA num, particularly because it does not follow into joins.
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
    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_XARCH) && defined(TARGET_64BIT)
    m_dfsTree = fgComputeDfs();
    m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);

    ScalarEvolutionContext scevContext(this);
    JITDUMP("Widening primary induction variables:\n");
    ArrayStack<Statement*> ivUses(getAllocator(CMK_LoopIVOpts));
    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Processing ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        for (Statement* stmt : loop->GetHeader()->Statements())
        {
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            JITDUMP("\n");

            DISPSTMT(stmt);

            GenTreeLclVarCommon* lcl    = stmt->GetRootNode()->AsLclVarCommon();
            LclVarDsc*           lclDsc = lvaGetDesc(lcl);
            if (lclDsc->TypeGet() != TYP_INT)
            {
                JITDUMP("  Type is %s, no widening to be done\n", varTypeName(lclDsc->TypeGet()));
                continue;
            }

            // If the IV is not enregisterable then uses/defs are going to go
            // to stack regardless. This check also filters out IVs that may be
            // live into exceptional exits since those are always marked DNER.
            if (lclDsc->lvDoNotEnregister)
            {
                JITDUMP("  V%02u is marked DNER\n", lcl->GetLclNum());
                continue;
            }

            Scev* scev = scevContext.Analyze(loop->GetHeader(), stmt->GetRootNode());
            if (scev == nullptr)
            {
                JITDUMP("  Could not analyze header PHI\n");
                continue;
            }

            scev = scevContext.Simplify(scev);
            JITDUMP("  => ");
            DBEXEC(verbose, scev->Dump(this));
            JITDUMP("\n");
            if (!scev->OperIs(ScevOper::AddRec))
            {
                JITDUMP("  Not an addrec\n");
                continue;
            }

            ScevAddRec* addRec = (ScevAddRec*)scev;

            JITDUMP("  V%02u is a primary induction variable in " FMT_LP "\n", lcl->GetLclNum(), loop->GetIndex());

            if (!optCanSinkWidenedIV(lcl->GetLclNum(), loop))
            {
                continue;
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

            ivUses.Reset();
            if (!optIsIVWideningProfitable(lcl->GetLclNum(), initBlock, initToConstant, loop, ivUses))
            {
                continue;
            }

            changed = true;

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
                    while ((insertInitAfter->GetNextStmt() != nullptr) &&
                           insertInitAfter->GetNextStmt()->IsPhiDefnStmt())
                    {
                        insertInitAfter = insertInitAfter->GetNextStmt();
                    }
                }
            }

            Statement* initStmt  = nullptr;
            unsigned   newLclNum = lvaGrabTemp(false DEBUGARG(printfAlloc("Widened IV V%02u", lcl->GetLclNum())));
            INDEBUG(lclDsc = nullptr);
            assert(startLocal->LclNum == lcl->GetLclNum());

            if (initBlock != preheader)
            {
                JITDUMP("Adding initialization of new widened local to same block as reaching def outside loop, " FMT_BB
                        "\n",
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
                initVal = gtNewCastNode(TYP_LONG, gtNewLclvNode(lcl->GetLclNum(), TYP_INT), true, TYP_LONG);
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

            JITDUMP("  Replacing uses of V%02u with widened version V%02u\n", lcl->GetLclNum(), newLclNum);

            if (initStmt != nullptr)
            {
                JITDUMP("    Replacing on the way to the loop\n");
                optBestEffortReplaceNarrowIVUses(lcl->GetLclNum(), startLocal->SsaNum, newLclNum, initBlock,
                                                 initStmt->GetNextStmt());
            }

            JITDUMP("    Replacing in the loop; %d statements with appearences\n", ivUses.Height());
            for (int i = 0; i < ivUses.Height(); i++)
            {
                Statement* stmt = ivUses.Bottom(i);
                JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lcl->GetLclNum(), newLclNum,
                        dspTreeID(stmt->GetRootNode()));
                DISPSTMT(stmt);
                JITDUMP("\n");
                optReplaceWidenedIV(lcl->GetLclNum(), SsaConfig::RESERVED_SSA_NUM, newLclNum, stmt);
            }

            optSinkWidenedIV(lcl->GetLclNum(), newLclNum, loop);
        }
    }

    fgInvalidateDfsTree();
#endif

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
