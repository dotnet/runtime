// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          BasicBlock                                       XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "jitstd/algorithm.h"

#if MEASURE_BLOCK_SIZE
/* static  */
size_t BasicBlock::s_Size;
/* static */
size_t BasicBlock::s_Count;
#endif // MEASURE_BLOCK_SIZE

#ifdef DEBUG
// The max # of tree nodes in any BB
/* static */
unsigned BasicBlock::s_nMaxTrees;
#endif // DEBUG

#ifdef DEBUG
FlowEdge* ShuffleHelper(unsigned hash, FlowEdge* res)
{
    FlowEdge* head = res;
    for (FlowEdge *prev = nullptr; res != nullptr; prev = res, res = res->getNextPredEdge())
    {
        unsigned blkHash = (hash ^ (res->getSourceBlock()->bbNum << 16) ^ res->getSourceBlock()->bbNum);
        if (((blkHash % 1879) & 1) && prev != nullptr)
        {
            // Swap res with head.
            prev->setNextPredEdge(head);
            FlowEdge* const resNext  = res->getNextPredEdge();
            FlowEdge* const headNext = head->getNextPredEdge();
            head->setNextPredEdge(resNext);
            res->setNextPredEdge(headNext);
            std::swap(head, res);
        }
    }
    return head;
}

unsigned SsaStressHashHelper()
{
    // hash = 0: turned off, hash = 1: use method hash, hash = *: use custom hash.
    unsigned hash = JitConfig.JitSsaStress();

    if (hash == 0)
    {
        return hash;
    }
    if (hash == 1)
    {
        return JitTls::GetCompiler()->info.compMethodHash();
    }
    return ((hash >> 16) == 0) ? ((hash << 16) | hash) : hash;
}
#endif

//------------------------------------------------------------------------
//  AllSuccessorEnumerator: Construct an instance of the enumerator.
//
//  Arguments:
//     comp  - Compiler instance
//     block - The block whose successors are to be iterated
//
AllSuccessorEnumerator::AllSuccessorEnumerator(Compiler* comp, BasicBlock* block) : m_block(block)
{
    m_numSuccs = 0;
    block->VisitAllSuccs(comp, [this](BasicBlock* succ) {
        if (m_numSuccs < ArrLen(m_successors))
        {
            m_successors[m_numSuccs] = succ;
        }

        m_numSuccs++;
        return BasicBlockVisit::Continue;
    });

    if (m_numSuccs > ArrLen(m_successors))
    {
        m_pSuccessors = new (comp, CMK_BasicBlock) BasicBlock*[m_numSuccs];

        unsigned numSuccs = 0;
        block->VisitAllSuccs(comp, [this, &numSuccs](BasicBlock* succ) {
            assert(numSuccs < m_numSuccs);
            m_pSuccessors[numSuccs++] = succ;
            return BasicBlockVisit::Continue;
        });

        assert(numSuccs == m_numSuccs);
    }
}

FlowEdge* Compiler::BlockPredsWithEH(BasicBlock* blk)
{
    BlockToFlowEdgeMap* ehPreds = GetBlockToEHPreds();
    FlowEdge*           res;
    if (ehPreds->Lookup(blk, &res))
    {
        return res;
    }

    res = blk->bbPreds;
    unsigned tryIndex;
    if (bbIsExFlowBlock(blk, &tryIndex))
    {
        // Find the first block of the try.
        EHblkDsc*   ehblk    = ehGetDsc(tryIndex);
        BasicBlock* tryStart = ehblk->ebdTryBeg;
        for (BasicBlock* const tryStartPredBlock : tryStart->PredBlocks())
        {
            res = new (this, CMK_FlowEdge) FlowEdge(tryStartPredBlock, res);

#if MEASURE_BLOCK_SIZE
            genFlowNodeCnt += 1;
            genFlowNodeSize += sizeof(FlowEdge);
#endif // MEASURE_BLOCK_SIZE
        }

        // Now add all blocks handled by this handler (except for second blocks of BBJ_CALLFINALLY/BBJ_ALWAYS pairs;
        // these cannot cause transfer to the handler...)
        // TODO-Throughput: It would be nice if we could iterate just over the blocks in the try, via
        // something like:
        //   for (BasicBlock* bb = ehblk->ebdTryBeg; bb != ehblk->ebdTryLast->bbNext; bb = bb->bbNext)
        //     (plus adding in any filter blocks outside the try whose exceptions are handled here).
        // That doesn't work, however: funclets have caused us to sometimes split the body of a try into
        // more than one sequence of contiguous blocks.  We need to find a better way to do this.
        for (BasicBlock* const bb : Blocks())
        {
            if (bbInExnFlowRegions(tryIndex, bb) && !bb->isBBCallAlwaysPairTail())
            {
                res = new (this, CMK_FlowEdge) FlowEdge(bb, res);

#if MEASURE_BLOCK_SIZE
                genFlowNodeCnt += 1;
                genFlowNodeSize += sizeof(FlowEdge);
#endif // MEASURE_BLOCK_SIZE
            }
        }

        if (ehblk->HasFinallyOrFaultHandler() && (ehblk->ebdHndBeg == blk))
        {
            // block is a finally or fault handler; all enclosing filters are predecessors
            unsigned enclosing = ehblk->ebdEnclosingTryIndex;
            while (enclosing != EHblkDsc::NO_ENCLOSING_INDEX)
            {
                EHblkDsc* enclosingDsc = ehGetDsc(enclosing);
                if (enclosingDsc->HasFilter())
                {
                    for (BasicBlock* filterBlk = enclosingDsc->ebdFilter; filterBlk != enclosingDsc->ebdHndBeg;
                         filterBlk             = filterBlk->bbNext)
                    {
                        res = new (this, CMK_FlowEdge) FlowEdge(filterBlk, res);

                        assert(filterBlk->VisitEHSecondPassSuccs(this, [blk](BasicBlock* succ) {
                            return succ == blk ? BasicBlockVisit::Abort : BasicBlockVisit::Continue;
                        }) == BasicBlockVisit::Abort);
                    }
                }

                enclosing = enclosingDsc->ebdEnclosingTryIndex;
            }
        }

#ifdef DEBUG
        unsigned hash = SsaStressHashHelper();
        if (hash != 0)
        {
            res = ShuffleHelper(hash, res);
        }
#endif // DEBUG
        ehPreds->Set(blk, res);
    }
    return res;
}

//------------------------------------------------------------------------
// checkPredListOrder: see if pred list is properly ordered
//
// Returns:
//    false if pred list is not in increasing bbNum order.
//
bool BasicBlock::checkPredListOrder()
{
    unsigned lastBBNum = 0;
    for (BasicBlock* const predBlock : PredBlocks())
    {
        const unsigned bbNum = predBlock->bbNum;
        if (bbNum <= lastBBNum)
        {
            assert(bbNum != lastBBNum);
            return false;
        }
        lastBBNum = bbNum;
    }
    return true;
}

//------------------------------------------------------------------------
// ensurePredListOrder: ensure all pred list entries appear in increasing
//    bbNum order.
//
// Arguments:
//    compiler - current compiler instance
//
void BasicBlock::ensurePredListOrder(Compiler* compiler)
{
    // First, check if list is already in order.
    //
    if (checkPredListOrder())
    {
        return;
    }

    reorderPredList(compiler);
    assert(checkPredListOrder());
}

//------------------------------------------------------------------------
// reorderPredList: relink pred list in increasing bbNum order.
//
// Arguments:
//    compiler - current compiler instance
//
void BasicBlock::reorderPredList(Compiler* compiler)
{
    // Count number or entries.
    //
    int count = 0;
    for (FlowEdge* const pred : PredEdges())
    {
        count++;
    }

    // If only 0 or 1 entry, nothing to reorder.
    //
    if (count < 2)
    {
        return;
    }

    // Allocate sort vector if needed.
    //
    if (compiler->fgPredListSortVector == nullptr)
    {
        CompAllocator allocator        = compiler->getAllocator(CMK_FlowEdge);
        compiler->fgPredListSortVector = new (allocator) jitstd::vector<FlowEdge*>(allocator);
    }

    jitstd::vector<FlowEdge*>* const sortVector = compiler->fgPredListSortVector;
    sortVector->clear();

    // Fill in the vector from the list.
    //
    for (FlowEdge* const pred : PredEdges())
    {
        sortVector->push_back(pred);
    }

    // Sort by increasing bbNum
    //
    struct FlowEdgeBBNumCmp
    {
        bool operator()(const FlowEdge* f1, const FlowEdge* f2)
        {
            return f1->getSourceBlock()->bbNum < f2->getSourceBlock()->bbNum;
        }
    };

    jitstd::sort(sortVector->begin(), sortVector->end(), FlowEdgeBBNumCmp());

    // Rethread the list.
    //
    FlowEdge* last = nullptr;

    for (FlowEdge* current : *sortVector)
    {
        if (last == nullptr)
        {
            bbPreds = current;
        }
        else
        {
            last->setNextPredEdge(current);
        }

        last = current;
    }

    last->setNextPredEdge(nullptr);

    // Note bbLastPred is only used transiently, during
    // initial pred list construction.
    //
    if (!compiler->fgPredsComputed)
    {
        bbLastPred = last;
    }
}

#ifdef DEBUG

//------------------------------------------------------------------------
// dspBlockILRange(): Display the block's IL range as [XXX...YYY), where XXX and YYY might be "???" for BAD_IL_OFFSET.
//
void BasicBlock::dspBlockILRange() const
{
    if (bbCodeOffs != BAD_IL_OFFSET)
    {
        printf("[%03X..", bbCodeOffs);
    }
    else
    {
        printf("[???"
               "..");
    }

    if (bbCodeOffsEnd != BAD_IL_OFFSET)
    {
        // brace-matching editor workaround for following line: (
        printf("%03X)", bbCodeOffsEnd);
    }
    else
    {
        // brace-matching editor workaround for following line: (
        printf("???"
               ")");
    }
}

//------------------------------------------------------------------------
// dspFlags: Print out the block's flags
//
void BasicBlock::dspFlags()
{
    if (bbFlags & BBF_MARKED)
    {
        printf("m ");
    }
    if (bbFlags & BBF_REMOVED)
    {
        printf("del ");
    }
    if (bbFlags & BBF_DONT_REMOVE)
    {
        printf("keep ");
    }
    if (bbFlags & BBF_IMPORTED)
    {
        printf("i ");
    }
    if (bbFlags & BBF_INTERNAL)
    {
        printf("internal ");
    }
    if (bbFlags & BBF_FAILED_VERIFICATION)
    {
        printf("failV ");
    }
    if (bbFlags & BBF_TRY_BEG)
    {
        printf("try ");
    }
    if (bbFlags & BBF_RUN_RARELY)
    {
        printf("rare ");
    }
    if (bbFlags & BBF_LOOP_HEAD)
    {
        printf("Loop ");
    }
    if (bbFlags & BBF_LOOP_CALL0)
    {
        printf("Loop0 ");
    }
    if (bbFlags & BBF_LOOP_CALL1)
    {
        printf("Loop1 ");
    }
    if (bbFlags & BBF_HAS_LABEL)
    {
        printf("label ");
    }
    if (bbFlags & BBF_HAS_JMP)
    {
        printf("jmp ");
    }
    if (bbFlags & BBF_HAS_CALL)
    {
        printf("hascall ");
    }
    if (bbFlags & BBF_GC_SAFE_POINT)
    {
        printf("gcsafe ");
    }
    if (bbFlags & BBF_FUNCLET_BEG)
    {
        printf("flet ");
    }
    if (bbFlags & BBF_HAS_IDX_LEN)
    {
        printf("idxlen ");
    }
    if (bbFlags & BBF_HAS_MD_IDX_LEN)
    {
        printf("mdidxlen ");
    }
    if (bbFlags & BBF_HAS_NEWOBJ)
    {
        printf("newobj ");
    }
    if (bbFlags & BBF_HAS_NULLCHECK)
    {
        printf("nullcheck ");
    }
#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    if (bbFlags & BBF_FINALLY_TARGET)
    {
        printf("ftarget ");
    }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    if (bbFlags & BBF_BACKWARD_JUMP)
    {
        printf("bwd ");
    }
    if (bbFlags & BBF_BACKWARD_JUMP_TARGET)
    {
        printf("bwd-target ");
    }
    if (bbFlags & BBF_BACKWARD_JUMP_SOURCE)
    {
        printf("bwd-src ");
    }
    if (bbFlags & BBF_PATCHPOINT)
    {
        printf("ppoint ");
    }
    if (bbFlags & BBF_PARTIAL_COMPILATION_PATCHPOINT)
    {
        printf("pc-ppoint ");
    }
    if (bbFlags & BBF_RETLESS_CALL)
    {
        printf("retless ");
    }
    if (bbFlags & BBF_LOOP_PREHEADER)
    {
        printf("LoopPH ");
    }
    if (bbFlags & BBF_COLD)
    {
        printf("cold ");
    }
    if (bbFlags & BBF_PROF_WEIGHT)
    {
        printf("IBC ");
    }
    if (bbFlags & BBF_IS_LIR)
    {
        printf("LIR ");
    }
    if (bbFlags & BBF_KEEP_BBJ_ALWAYS)
    {
        printf("KEEP ");
    }
    if (bbFlags & BBF_CLONED_FINALLY_BEGIN)
    {
        printf("cfb ");
    }
    if (bbFlags & BBF_CLONED_FINALLY_END)
    {
        printf("cfe ");
    }
    if (bbFlags & BBF_LOOP_ALIGN)
    {
        printf("align ");
    }
    if (bbFlags & BBF_HAS_MDARRAYREF)
    {
        printf("mdarr ");
    }
}

/*****************************************************************************
 *
 *  Display the bbPreds basic block list (the block predecessors).
 *  Returns the number of characters printed.
 */

unsigned BasicBlock::dspPreds()
{
    unsigned count = 0;
    for (FlowEdge* const pred : PredEdges())
    {
        if (count != 0)
        {
            printf(",");
            count += 1;
        }
        printf(FMT_BB, pred->getSourceBlock()->bbNum);
        count += 4;

        // Account for %02u only handling 2 digits, but we can display more than that.
        unsigned digits = CountDigits(pred->getSourceBlock()->bbNum);
        if (digits > 2)
        {
            count += digits - 2;
        }

        // Does this predecessor have an interesting dup count? If so, display it.
        if (pred->getDupCount() > 1)
        {
            printf("(%u)", pred->getDupCount());
            count += 2 + CountDigits(pred->getDupCount());
        }
    }
    return count;
}

//------------------------------------------------------------------------
// dspSuccs: Display the basic block successors.
//
// Arguments:
//    compiler - compiler instance; passed to NumSucc(Compiler*) -- see that function for implications.
//
void BasicBlock::dspSuccs(Compiler* compiler)
{
    bool first = true;

    // If this is a switch, we don't want to call `Succs(Compiler*)` because it will eventually call
    // `GetSwitchDescMap()`, and that will have the side-effect of allocating the unique switch descriptor map
    // and/or compute this switch block's unique succ set if it is not present. Debug output functions should
    // never have an effect on codegen. We also don't want to assume the unique succ set is accurate, so we
    // compute it ourselves here.
    if (bbJumpKind == BBJ_SWITCH)
    {
        // Create a set with all the successors. Don't use BlockSet, so we don't need to worry
        // about the BlockSet epoch.
        unsigned     bbNumMax = compiler->fgBBNumMax;
        BitVecTraits bitVecTraits(bbNumMax + 1, compiler);
        BitVec       uniqueSuccBlocks(BitVecOps::MakeEmpty(&bitVecTraits));
        for (BasicBlock* const bTarget : SwitchTargets())
        {
            BitVecOps::AddElemD(&bitVecTraits, uniqueSuccBlocks, bTarget->bbNum);
        }
        BitVecOps::Iter iter(&bitVecTraits, uniqueSuccBlocks);
        unsigned        bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            // Note that we will output switch successors in increasing numerical bbNum order, which is
            // not related to their order in the bbJumpSwt->bbsDstTab table.
            printf("%s" FMT_BB, first ? "" : ",", bbNum);
            first = false;
        }
    }
    else
    {
        for (BasicBlock* const succ : Succs(compiler))
        {
            printf("%s" FMT_BB, first ? "" : ",", succ->bbNum);
            first = false;
        }
    }
}

// Display a compact representation of the bbJumpKind, that is, where this block branches.
// This is similar to code in Compiler::fgTableDispBasicBlock(), but doesn't have that code's requirements to align
// things strictly.
void BasicBlock::dspJumpKind()
{
    switch (bbJumpKind)
    {
        case BBJ_EHFINALLYRET:
            printf(" (finret)");
            break;

        case BBJ_EHFAULTRET:
            printf(" (falret)");
            break;

        case BBJ_EHFILTERRET:
            printf(" (fltret)");
            break;

        case BBJ_EHCATCHRET:
            printf(" -> " FMT_BB " (cret)", bbJumpDest->bbNum);
            break;

        case BBJ_THROW:
            printf(" (throw)");
            break;

        case BBJ_RETURN:
            printf(" (return)");
            break;

        case BBJ_NONE:
            // For fall-through blocks, print nothing.
            break;

        case BBJ_ALWAYS:
            if (bbFlags & BBF_KEEP_BBJ_ALWAYS)
            {
                printf(" -> " FMT_BB " (ALWAYS)", bbJumpDest->bbNum);
            }
            else
            {
                printf(" -> " FMT_BB " (always)", bbJumpDest->bbNum);
            }
            break;

        case BBJ_LEAVE:
            printf(" -> " FMT_BB " (leave)", bbJumpDest->bbNum);
            break;

        case BBJ_CALLFINALLY:
            printf(" -> " FMT_BB " (callf)", bbJumpDest->bbNum);
            break;

        case BBJ_COND:
            printf(" -> " FMT_BB " (cond)", bbJumpDest->bbNum);
            break;

        case BBJ_SWITCH:
        {
            printf(" ->");

            const unsigned     jumpCnt = bbJumpSwt->bbsCount;
            BasicBlock** const jumpTab = bbJumpSwt->bbsDstTab;

            for (unsigned i = 0; i < jumpCnt; i++)
            {
                printf("%c" FMT_BB, (i == 0) ? ' ' : ',', jumpTab[i]->bbNum);

                const bool isDefault = bbJumpSwt->bbsHasDefault && (i == jumpCnt - 1);
                if (isDefault)
                {
                    printf("[def]");
                }

                const bool isDominant = bbJumpSwt->bbsHasDominantCase && (i == bbJumpSwt->bbsDominantCase);
                if (isDominant)
                {
                    printf("[dom(" FMT_WT ")]", bbJumpSwt->bbsDominantFraction);
                }
            }

            printf(" (switch)");
        }
        break;

        default:
            unreached();
            break;
    }
}

void BasicBlock::dspBlockHeader(Compiler* compiler,
                                bool      showKind /*= true*/,
                                bool      showFlags /*= false*/,
                                bool      showPreds /*= true*/)
{
    printf(FMT_BB " ", bbNum);
    dspBlockILRange();
    if (showKind)
    {
        dspJumpKind();
    }
    if (showPreds)
    {
        printf(", preds={");
        dspPreds();
        printf("} succs={");
        dspSuccs(compiler);
        printf("}");
    }
    if (showFlags)
    {
        const unsigned lowFlags  = (unsigned)bbFlags;
        const unsigned highFlags = (unsigned)(bbFlags >> 32);
        printf(" flags=0x%08x.%08x: ", highFlags, lowFlags);
        dspFlags();
    }
    printf("\n");
}

const char* BasicBlock::dspToString(int blockNumPadding /* = 0 */)
{
    static char buffers[3][64]; // static array of 3 to allow 3 concurrent calls in one printf()
    static int  nextBufferIndex = 0;

    auto& buffer    = buffers[nextBufferIndex];
    nextBufferIndex = (nextBufferIndex + 1) % ArrLen(buffers);
    _snprintf_s(buffer, ArrLen(buffer), ArrLen(buffer), FMT_BB "%*s [%04u]", bbNum, blockNumPadding, "", bbID);
    return buffer;
}

#endif // DEBUG

// Allocation function for MemoryPhiArg.
void* BasicBlock::MemoryPhiArg::operator new(size_t sz, Compiler* comp)
{
    return comp->getAllocator(CMK_MemoryPhiArg).allocate<char>(sz);
}

//------------------------------------------------------------------------
// CloneBlockState: Try to populate `to` block with a copy of `from` block's statements, replacing
//                  uses of local `varNum` with IntCns `varVal`.
//
// Arguments:
//    compiler - Jit compiler instance
//    to - New/empty block to copy statements into
//    from - Block to copy statements from
//    varNum - lclVar uses with lclNum `varNum` will be replaced; can be ~0 to indicate no replacement.
//    varVal - If replacing uses of `varNum`, replace them with int constants with value `varVal`.
//
// Return Value:
//    Cloning may fail because this routine uses `gtCloneExpr` for cloning and it can't handle all
//    IR nodes.  If cloning of any statement fails, `false` will be returned and block `to` may be
//    partially populated.  If cloning of all statements succeeds, `true` will be returned and
//    block `to` will be fully populated.
//
// Note:
//    Leaves block ref count at zero, and pred edge list empty.
//
bool BasicBlock::CloneBlockState(
    Compiler* compiler, BasicBlock* to, const BasicBlock* from, unsigned varNum, int varVal)
{
    assert(to->bbStmtList == nullptr);
    to->bbFlags  = from->bbFlags;
    to->bbWeight = from->bbWeight;
    BlockSetOps::AssignAllowUninitRhs(compiler, to->bbReach, from->bbReach);
    to->copyEHRegion(from);
    to->bbCatchTyp    = from->bbCatchTyp;
    to->bbStkTempsIn  = from->bbStkTempsIn;
    to->bbStkTempsOut = from->bbStkTempsOut;
    to->bbStkDepth    = from->bbStkDepth;
    to->bbCodeOffs    = from->bbCodeOffs;
    to->bbCodeOffsEnd = from->bbCodeOffsEnd;
    VarSetOps::AssignAllowUninitRhs(compiler, to->bbScope, from->bbScope);
    to->bbNatLoopNum = from->bbNatLoopNum;
#ifdef DEBUG
    to->bbTgtStkDepth = from->bbTgtStkDepth;
#endif // DEBUG

    for (Statement* const fromStmt : from->Statements())
    {
        GenTree* newExpr = compiler->gtCloneExpr(fromStmt->GetRootNode(), GTF_EMPTY, varNum, varVal);
        if (!newExpr)
        {
            // gtCloneExpr doesn't handle all opcodes, so may fail to clone a statement.
            // When that happens, it returns nullptr; abandon the rest of this block and
            // return `false` to the caller to indicate that cloning was unsuccessful.
            return false;
        }
        compiler->fgInsertStmtAtEnd(to, compiler->fgNewStmtFromTree(newExpr, fromStmt->GetDebugInfo()));
    }
    return true;
}

// LIR helpers
void BasicBlock::MakeLIR(GenTree* firstNode, GenTree* lastNode)
{
    assert(!IsLIR());
    assert((firstNode == nullptr) == (lastNode == nullptr));
    assert((firstNode == lastNode) || firstNode->Precedes(lastNode));

    m_firstNode = firstNode;
    m_lastNode  = lastNode;
    bbFlags |= BBF_IS_LIR;
}

bool BasicBlock::IsLIR() const
{
    assert(isValid());
    const bool isLIR = ((bbFlags & BBF_IS_LIR) != 0);
    return isLIR;
}

//------------------------------------------------------------------------
// firstStmt: Returns the first statement in the block
//
// Arguments:
//    None.
//
// Return Value:
//    The first statement in the block's bbStmtList.
//
Statement* BasicBlock::firstStmt() const
{
    return bbStmtList;
}

//------------------------------------------------------------------------
// lastStmt: Returns the last statement in the block
//
// Arguments:
//    None.
//
// Return Value:
//    The last statement in the block's bbStmtList.
//
Statement* BasicBlock::lastStmt() const
{
    if (bbStmtList == nullptr)
    {
        return nullptr;
    }

    Statement* result = bbStmtList->GetPrevStmt();
    assert(result != nullptr && result->GetNextStmt() == nullptr);
    return result;
}

//------------------------------------------------------------------------
// BasicBlock::lastNode: Returns the last node in the block.
//
GenTree* BasicBlock::lastNode() const
{
    return IsLIR() ? m_lastNode : lastStmt()->GetRootNode();
}

//------------------------------------------------------------------------
// GetUniquePred: Returns the unique predecessor of a block, if one exists.
// The predecessor lists must be accurate.
//
// Arguments:
//    None.
//
// Return Value:
//    The unique predecessor of a block, or nullptr if there is no unique predecessor.
//
// Notes:
//    If the first block has a predecessor (which it may have, if it is the target of
//    a backedge), we never want to consider it "unique" because the prolog is an
//    implicit predecessor.

BasicBlock* BasicBlock::GetUniquePred(Compiler* compiler) const
{
    assert(compiler->fgPredsComputed);

    if ((bbPreds == nullptr) || (bbPreds->getNextPredEdge() != nullptr) || (this == compiler->fgFirstBB))
    {
        return nullptr;
    }
    else
    {
        return bbPreds->getSourceBlock();
    }
}

//------------------------------------------------------------------------
// GetUniqueSucc: Returns the unique successor of a block, if one exists.
// Only considers BBJ_ALWAYS and BBJ_NONE block types.
//
// Arguments:
//    None.
//
// Return Value:
//    The unique successor of a block, or nullptr if there is no unique successor.
//
BasicBlock* BasicBlock::GetUniqueSucc() const
{
    if (bbJumpKind == BBJ_ALWAYS)
    {
        return bbJumpDest;
    }
    else if (bbJumpKind == BBJ_NONE)
    {
        return bbNext;
    }
    else
    {
        return nullptr;
    }
}

// Static vars.
BasicBlock::MemoryPhiArg* BasicBlock::EmptyMemoryPhiDef = (BasicBlock::MemoryPhiArg*)0x1;

unsigned JitPtrKeyFuncs<BasicBlock>::GetHashCode(const BasicBlock* ptr)
{
#ifdef DEBUG
    unsigned hash = SsaStressHashHelper();
    if (hash != 0)
    {
        return (hash ^ (ptr->bbNum << 16) ^ ptr->bbNum);
    }
#endif
    return ptr->bbNum;
}

//------------------------------------------------------------------------
// isEmpty: check if block is empty or contains only ignorable statements
//
// Return Value:
//    True if block is empty, or contains only PHI stores,
//    or contains zero or more PHI stores followed by NOPs.
//
bool BasicBlock::isEmpty() const
{
    if (!IsLIR())
    {
        for (Statement* const stmt : NonPhiStatements())
        {
            if (!stmt->GetRootNode()->OperIs(GT_NOP))
            {
                return false;
            }
        }
    }
    else
    {
        for (GenTree* node : LIR::AsRange(this))
        {
            if (node->OperGet() != GT_IL_OFFSET)
            {
                return false;
            }
        }
    }

    return true;
}

//------------------------------------------------------------------------
// isValid: Checks that the basic block doesn't mix statements and LIR lists.
//
// Return Value:
//    True if it a valid basic block.
//
bool BasicBlock::isValid() const
{
    const bool isLIR = ((bbFlags & BBF_IS_LIR) != 0);
    if (isLIR)
    {
        // Should not have statements in LIR.
        return (bbStmtList == nullptr);
    }
    else
    {
        // Should not have tree list before LIR.
        return (GetFirstLIRNode() == nullptr);
    }
}

Statement* BasicBlock::FirstNonPhiDef() const
{
    Statement* stmt = firstStmt();
    while ((stmt != nullptr) && stmt->IsPhiDefnStmt())
    {
        stmt = stmt->GetNextStmt();
    }

    return stmt;
}

Statement* BasicBlock::FirstNonPhiDefOrCatchArgStore() const
{
    Statement* stmt = FirstNonPhiDef();
    if (stmt == nullptr)
    {
        return nullptr;
    }
    GenTree* tree = stmt->GetRootNode();
    if (tree->OperIs(GT_STORE_LCL_VAR) && tree->AsLclVar()->Data()->OperIs(GT_CATCH_ARG))
    {
        stmt = stmt->GetNextStmt();
    }
    return stmt;
}

/*****************************************************************************
 *
 *  Can a BasicBlock be inserted after this without altering the flowgraph
 */

bool BasicBlock::bbFallsThrough() const
{
    switch (bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_EHFINALLYRET:
        case BBJ_EHFAULTRET:
        case BBJ_EHFILTERRET:
        case BBJ_EHCATCHRET:
        case BBJ_RETURN:
        case BBJ_ALWAYS:
        case BBJ_LEAVE:
        case BBJ_SWITCH:
            return false;

        case BBJ_NONE:
        case BBJ_COND:
            return true;

        case BBJ_CALLFINALLY:
            return ((bbFlags & BBF_RETLESS_CALL) == 0);

        default:
            assert(!"Unknown bbJumpKind in bbFallsThrough()");
            return true;
    }
}

//------------------------------------------------------------------------
// NumSucc: Returns the count of block successors. See the declaration comment for details.
//
// Arguments:
//    None.
//
// Return Value:
//    Count of block successors.
//
unsigned BasicBlock::NumSucc() const
{
    switch (bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFINALLYRET:
        case BBJ_EHFAULTRET:
        case BBJ_EHFILTERRET:
            return 0;

        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_LEAVE:
        case BBJ_NONE:
            return 1;

        case BBJ_COND:
            if (bbJumpDest == bbNext)
            {
                return 1;
            }
            else
            {
                return 2;
            }

        case BBJ_SWITCH:
            return bbJumpSwt->bbsCount;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// GetSucc: Returns the requested block successor. See the declaration comment for details.
//
// Arguments:
//    i - index of successor to return. 0 <= i <= NumSucc().
//
// Return Value:
//    Requested successor block
//
BasicBlock* BasicBlock::GetSucc(unsigned i) const
{
    assert(i < NumSucc()); // Index bounds check.
    switch (bbJumpKind)
    {
        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_LEAVE:
            return bbJumpDest;

        case BBJ_NONE:
            return bbNext;

        case BBJ_COND:
            if (i == 0)
            {
                return bbNext;
            }
            else
            {
                assert(i == 1);
                return bbJumpDest;
            }

        case BBJ_SWITCH:
            return bbJumpSwt->bbsDstTab[i];

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// NumSucc: Returns the count of block successors. See the declaration comment for details.
//
// Arguments:
//    comp - Compiler instance
//
// Return Value:
//    Count of block successors.
//
unsigned BasicBlock::NumSucc(Compiler* comp)
{
    assert(comp != nullptr);

    switch (bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFAULTRET:
            return 0;

        case BBJ_EHFINALLYRET:
            // We may call this method before we realize we have invalid IL. Tolerate.
            //
            if (!hasHndIndex())
            {
                return 0;
            }
            return comp->fgNSuccsOfFinallyRet(this);

        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
        case BBJ_LEAVE:
        case BBJ_NONE:
            return 1;

        case BBJ_COND:
            if (bbJumpDest == bbNext)
            {
                return 1;
            }
            else
            {
                return 2;
            }

        case BBJ_SWITCH:
        {
            Compiler::SwitchUniqueSuccSet sd = comp->GetDescriptorForSwitch(this);
            return sd.numDistinctSuccs;
        }

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// GetSucc: Returns the requested block successor. See the declaration comment for details.
//
// Arguments:
//    i - index of successor to return. 0 <= i <= NumSucc(comp).
//    comp - Compiler instance
//
// Return Value:
//    Requested successor block
//
BasicBlock* BasicBlock::GetSucc(unsigned i, Compiler* comp)
{
    assert(comp != nullptr);

    assert(i < NumSucc(comp)); // Index bounds check.
    switch (bbJumpKind)
    {
        case BBJ_EHFILTERRET:
        {
            // Handler is the (sole) normal successor of the filter.
            assert(comp->fgFirstBlockOfHandler(this) == bbJumpDest);
            return bbJumpDest;
        }

        case BBJ_EHFINALLYRET:
            // Note: the following call is expensive.
            return comp->fgSuccOfFinallyRet(this, i);

        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_LEAVE:
            return bbJumpDest;

        case BBJ_NONE:
            return bbNext;

        case BBJ_COND:
            if (i == 0)
            {
                return bbNext;
            }
            else
            {
                assert(i == 1);
                return bbJumpDest;
            }

        case BBJ_SWITCH:
        {
            Compiler::SwitchUniqueSuccSet sd = comp->GetDescriptorForSwitch(this);
            assert(i < sd.numDistinctSuccs); // Range check.
            return sd.nonDuplicates[i];
        }

        default:
            unreached();
    }
}

void BasicBlock::InitVarSets(Compiler* comp)
{
    VarSetOps::AssignNoCopy(comp, bbVarUse, VarSetOps::MakeEmpty(comp));
    VarSetOps::AssignNoCopy(comp, bbVarDef, VarSetOps::MakeEmpty(comp));
    VarSetOps::AssignNoCopy(comp, bbLiveIn, VarSetOps::MakeEmpty(comp));
    VarSetOps::AssignNoCopy(comp, bbLiveOut, VarSetOps::MakeEmpty(comp));
    VarSetOps::AssignNoCopy(comp, bbScope, VarSetOps::MakeEmpty(comp));

    bbMemoryUse     = emptyMemoryKindSet;
    bbMemoryDef     = emptyMemoryKindSet;
    bbMemoryLiveIn  = emptyMemoryKindSet;
    bbMemoryLiveOut = emptyMemoryKindSet;
}

// Returns true if the basic block ends with GT_JMP
bool BasicBlock::endsWithJmpMethod(Compiler* comp) const
{
    if (comp->compJmpOpUsed && (bbJumpKind == BBJ_RETURN) && (bbFlags & BBF_HAS_JMP))
    {
        GenTree* lastNode = this->lastNode();
        assert(lastNode != nullptr);
        return lastNode->OperGet() == GT_JMP;
    }

    return false;
}

// Returns true if the basic block ends with either
//  i) GT_JMP or
// ii) tail call (implicit or explicit)
//
// Params:
//    comp              - Compiler instance
//    fastTailCallsOnly - Only consider fast tail calls excluding tail calls via helper.
//
bool BasicBlock::endsWithTailCallOrJmp(Compiler* comp, bool fastTailCallsOnly /*=false*/) const
{
    GenTree* tailCall                       = nullptr;
    bool     tailCallsConvertibleToLoopOnly = false;
    return endsWithJmpMethod(comp) ||
           endsWithTailCall(comp, fastTailCallsOnly, tailCallsConvertibleToLoopOnly, &tailCall);
}

//------------------------------------------------------------------------------
// endsWithTailCall : Check if the block ends with a tail call.
//
// Arguments:
//    comp                            - compiler instance
//    fastTailCallsOnly               - check for fast tail calls only
//    tailCallsConvertibleToLoopOnly  - check for tail calls convertible to loop only
//    tailCall                        - a pointer to a tree that will be set to the call tree if the block
//                                      ends with a tail call and will be set to nullptr otherwise.
//
// Return Value:
//    true if the block ends with a tail call; false otherwise.
//
// Notes:
//    At most one of fastTailCallsOnly and tailCallsConvertibleToLoopOnly flags can be true.
//
bool BasicBlock::endsWithTailCall(Compiler* comp,
                                  bool      fastTailCallsOnly,
                                  bool      tailCallsConvertibleToLoopOnly,
                                  GenTree** tailCall) const
{
    assert(!fastTailCallsOnly || !tailCallsConvertibleToLoopOnly);
    *tailCall   = nullptr;
    bool result = false;

    // Is this a tail call?
    // The reason for keeping this under RyuJIT is so as not to impact existing Jit32 x86 and arm
    // targets.
    if (comp->compTailCallUsed)
    {
        if (fastTailCallsOnly || tailCallsConvertibleToLoopOnly)
        {
            // Only fast tail calls or only tail calls convertible to loops
            result = (bbFlags & BBF_HAS_JMP) && (bbJumpKind == BBJ_RETURN);
        }
        else
        {
            // Fast tail calls, tail calls convertible to loops, and tails calls dispatched via helper
            result = (bbJumpKind == BBJ_THROW) || ((bbFlags & BBF_HAS_JMP) && (bbJumpKind == BBJ_RETURN));
        }

        if (result)
        {
            GenTree* lastNode = this->lastNode();
            if (lastNode->OperGet() == GT_CALL)
            {
                GenTreeCall* call = lastNode->AsCall();
                if (tailCallsConvertibleToLoopOnly)
                {
                    result = call->IsTailCallConvertibleToLoop();
                }
                else if (fastTailCallsOnly)
                {
                    result = call->IsFastTailCall();
                }
                else
                {
                    result = call->IsTailCall();
                }

                if (result)
                {
                    *tailCall = call;
                }
            }
            else
            {
                result = false;
            }
        }
    }

    return result;
}

//------------------------------------------------------------------------------
// endsWithTailCallConvertibleToLoop : Check if the block ends with a tail call convertible to loop.
//
// Arguments:
//    comp  -  compiler instance
//    tailCall  -  a pointer to a tree that will be set to the call tree if the block
//                 ends with a tail call convertible to loop and will be set to nullptr otherwise.
//
// Return Value:
//    true if the block ends with a tail call convertible to loop.
//
bool BasicBlock::endsWithTailCallConvertibleToLoop(Compiler* comp, GenTree** tailCall) const
{
    bool fastTailCallsOnly              = false;
    bool tailCallsConvertibleToLoopOnly = true;
    return endsWithTailCall(comp, fastTailCallsOnly, tailCallsConvertibleToLoopOnly, tailCall);
}

/*****************************************************************************
 *
 *  Allocate a basic block but don't append it to the current BB list.
 */

BasicBlock* Compiler::bbNewBasicBlock(BBjumpKinds jumpKind)
{
    BasicBlock* block;

    /* Allocate the block descriptor and zero it out */
    assert(fgSafeBasicBlockCreation);

    block = new (this, CMK_BasicBlock) BasicBlock;

#if MEASURE_BLOCK_SIZE
    BasicBlock::s_Count += 1;
    BasicBlock::s_Size += sizeof(*block);
#endif

#ifdef DEBUG
    // fgLookupBB() is invalid until fgInitBBLookup() is called again.
    fgBBs = (BasicBlock**)0xCDCD;
#endif

    // TODO-Throughput: The following memset is pretty expensive - do something else?
    // Note that some fields have to be initialized to 0 (like bbFPStateX87)
    memset((void*)block, 0, sizeof(*block));

    // scopeInfo needs to be able to differentiate between blocks which
    // correspond to some instrs (and so may have some LocalVarInfo
    // boundaries), or have been inserted by the JIT
    block->bbCodeOffs    = BAD_IL_OFFSET;
    block->bbCodeOffsEnd = BAD_IL_OFFSET;

#ifdef DEBUG
    block->bbID = compBasicBlockID++;
#endif

    /* Give the block a number, set the ancestor count and weight */

    ++fgBBcount;
    block->bbNum = ++fgBBNumMax;

    if (compRationalIRForm)
    {
        block->bbFlags |= BBF_IS_LIR;
    }

    block->bbRefs   = 1;
    block->bbWeight = BB_UNITY_WEIGHT;

    block->bbStkTempsIn  = NO_BASE_TMP;
    block->bbStkTempsOut = NO_BASE_TMP;

    block->bbEntryState = nullptr;

    /* Record the jump kind in the block */

    block->bbJumpKind = jumpKind;

    if (jumpKind == BBJ_THROW)
    {
        block->bbSetRunRarely();
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("New Basic Block %s created.\n", block->dspToString());
    }
#endif

    // We will give all the blocks var sets after the number of tracked variables
    // is determined and frozen.  After that, if we dynamically create a basic block,
    // we will initialize its var sets.
    if (fgBBVarSetsInited)
    {
        VarSetOps::AssignNoCopy(this, block->bbVarUse, VarSetOps::MakeEmpty(this));
        VarSetOps::AssignNoCopy(this, block->bbVarDef, VarSetOps::MakeEmpty(this));
        VarSetOps::AssignNoCopy(this, block->bbLiveIn, VarSetOps::MakeEmpty(this));
        VarSetOps::AssignNoCopy(this, block->bbLiveOut, VarSetOps::MakeEmpty(this));
        VarSetOps::AssignNoCopy(this, block->bbScope, VarSetOps::MakeEmpty(this));
    }
    else
    {
        VarSetOps::AssignNoCopy(this, block->bbVarUse, VarSetOps::UninitVal());
        VarSetOps::AssignNoCopy(this, block->bbVarDef, VarSetOps::UninitVal());
        VarSetOps::AssignNoCopy(this, block->bbLiveIn, VarSetOps::UninitVal());
        VarSetOps::AssignNoCopy(this, block->bbLiveOut, VarSetOps::UninitVal());
        VarSetOps::AssignNoCopy(this, block->bbScope, VarSetOps::UninitVal());
    }

    block->bbMemoryUse     = emptyMemoryKindSet;
    block->bbMemoryDef     = emptyMemoryKindSet;
    block->bbMemoryLiveIn  = emptyMemoryKindSet;
    block->bbMemoryLiveOut = emptyMemoryKindSet;

    for (MemoryKind memoryKind : allMemoryKinds())
    {
        block->bbMemorySsaPhiFunc[memoryKind] = nullptr;
        block->bbMemorySsaNumIn[memoryKind]   = 0;
        block->bbMemorySsaNumOut[memoryKind]  = 0;
    }

    // Make sure we reserve a NOT_IN_LOOP value that isn't a legal table index.
    static_assert_no_msg(BasicBlock::MAX_LOOP_NUM < BasicBlock::NOT_IN_LOOP);

    block->bbNatLoopNum = BasicBlock::NOT_IN_LOOP;

    block->bbPreorderNum  = 0;
    block->bbPostorderNum = 0;

    return block;
}

//------------------------------------------------------------------------
// isBBCallAlwaysPair: Determine if this is the first block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair
//
// Return Value:
//    True iff "this" is the first block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair
//    -- a block corresponding to an exit from the try of a try/finally.
//
// Notes:
//    In the flow graph, this becomes a block that calls the finally, and a second, immediately
//    following empty block (in the bbNext chain) to which the finally will return, and which
//    branches unconditionally to the next block to be executed outside the try/finally.
//    Note that code is often generated differently than this description. For example, on ARM,
//    the target of the BBJ_ALWAYS is loaded in LR (the return register), and a direct jump is
//    made to the 'finally'. The effect is that the 'finally' returns directly to the target of
//    the BBJ_ALWAYS. A "retless" BBJ_CALLFINALLY is one that has no corresponding BBJ_ALWAYS.
//    This can happen if the finally is known to not return (e.g., it contains a 'throw'). In
//    that case, the BBJ_CALLFINALLY flags has BBF_RETLESS_CALL set. Note that ARM never has
//    "retless" BBJ_CALLFINALLY blocks due to a requirement to use the BBJ_ALWAYS for
//    generating code.
//
bool BasicBlock::isBBCallAlwaysPair() const
{
#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    if (this->bbJumpKind == BBJ_CALLFINALLY)
#else
    if ((this->bbJumpKind == BBJ_CALLFINALLY) && !(this->bbFlags & BBF_RETLESS_CALL))
#endif
    {
#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        // On ARM, there are no retless BBJ_CALLFINALLY.
        assert(!(this->bbFlags & BBF_RETLESS_CALL));
#endif
        // Some asserts that the next block is a BBJ_ALWAYS of the proper form.
        assert(this->bbNext != nullptr);
        assert(this->bbNext->bbJumpKind == BBJ_ALWAYS);
        assert(this->bbNext->bbFlags & BBF_KEEP_BBJ_ALWAYS);
        assert(this->bbNext->isEmpty());

        return true;
    }
    else
    {
        return false;
    }
}

//------------------------------------------------------------------------
// isBBCallAlwaysPairTail: Determine if this is the last block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair
//
// Return Value:
//    True iff "this" is the last block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair
//    -- a block corresponding to an exit from the try of a try/finally.
//
// Notes:
//    See notes on isBBCallAlwaysPair(), above.
//
bool BasicBlock::isBBCallAlwaysPairTail() const
{
    return (bbPrev != nullptr) && bbPrev->isBBCallAlwaysPair();
}

//------------------------------------------------------------------------
// hasEHBoundaryIn: Determine if this block begins at an EH boundary.
//
// Return Value:
//    True iff the block is the target of an EH edge; false otherwise.
//
// Notes:
//    For the purposes of this method (and its callers), an EH edge is one on
//    which the EH flow model requires that all lclVars must be reloaded from
//    the stack before use, since control flow may transfer to this block through
//    control flow that is not reflected in the flowgraph.
//    Note that having a predecessor in a different EH region doesn't require
//    that lclVars must be reloaded from the stack. That's only required when
//    this block might be entered via flow that is not represented by an edge
//    in the flowgraph.
//
bool BasicBlock::hasEHBoundaryIn() const
{
    bool returnVal = (bbCatchTyp != BBCT_NONE);
    if (!returnVal)
    {
#if FEATURE_EH_FUNCLETS
        assert((bbFlags & BBF_FUNCLET_BEG) == 0);
#endif // FEATURE_EH_FUNCLETS
    }
    return returnVal;
}

//------------------------------------------------------------------------
// hasEHBoundaryOut: Determine if this block ends in an EH boundary.
//
// Return Value:
//    True iff the block ends in an exception boundary that requires that no lclVars
//    are live in registers; false otherwise.
//
// Notes:
//    We may have a successor in a different EH region, but it is OK to have lclVars
//    live in registers if any successor is a normal flow edge. That's because the
//    EH write-thru semantics ensure that we always have an up-to-date value on the stack.
//
bool BasicBlock::hasEHBoundaryOut() const
{
    bool returnVal = KindIs(BBJ_EHFILTERRET, BBJ_EHFINALLYRET, BBJ_EHFAULTRET);

#if FEATURE_EH_FUNCLETS
    if (bbJumpKind == BBJ_EHCATCHRET)
    {
        returnVal = true;
    }
#endif // FEATURE_EH_FUNCLETS

    return returnVal;
}

//------------------------------------------------------------------------
// BBswtDesc copy ctor: copy a switch descriptor
//
// Arguments:
//    comp - compiler instance
//    other - existing switch descriptor to copy
//
BBswtDesc::BBswtDesc(Compiler* comp, const BBswtDesc* other)
    : bbsDstTab(nullptr)
    , bbsCount(other->bbsCount)
    , bbsDominantCase(other->bbsDominantCase)
    , bbsDominantFraction(other->bbsDominantFraction)
    , bbsHasDefault(other->bbsHasDefault)
    , bbsHasDominantCase(other->bbsHasDominantCase)
{
    // Allocate and fill in a new dst tab
    //
    bbsDstTab = new (comp, CMK_BasicBlock) BasicBlock*[bbsCount];
    for (unsigned i = 0; i < bbsCount; i++)
    {
        bbsDstTab[i] = other->bbsDstTab[i];
    }
}

//------------------------------------------------------------------------
// unmarkLoopAlign: Unmarks the LOOP_ALIGN flag from the block and reduce the
//                  loop alignment count.
//
// Arguments:
//    compiler - Compiler instance
//    reason - Reason to print in JITDUMP
//
void BasicBlock::unmarkLoopAlign(Compiler* compiler DEBUG_ARG(const char* reason))
{
    // Make sure we unmark and count just once.
    if (isLoopAlign())
    {
        compiler->loopAlignCandidates--;
        bbFlags &= ~BBF_LOOP_ALIGN;
        JITDUMP("Unmarking LOOP_ALIGN from " FMT_BB ". Reason= %s.\n", bbNum, reason);
    }
}

//------------------------------------------------------------------------
// getCalledCount: get the value used to normalized weights for this method
//
// Arguments:
//    compiler - Compiler instance
//
// Notes:
//   If we don't have profile data then getCalledCount will return BB_UNITY_WEIGHT (100)
//   otherwise it returns the number of times that profile data says the method was called.

// static
weight_t BasicBlock::getCalledCount(Compiler* comp)
{
    // when we don't have profile data then fgCalledCount will be BB_UNITY_WEIGHT (100)
    weight_t calledCount = comp->fgCalledCount;

    // If we haven't yet reach the place where we setup fgCalledCount it could still be zero
    // so return a reasonable value to use until we set it.
    //
    if (calledCount == 0)
    {
        if (comp->fgIsUsingProfileWeights())
        {
            // When we use profile data block counts we have exact counts,
            // not multiples of BB_UNITY_WEIGHT (100)
            calledCount = 1;
        }
        else
        {
            calledCount = comp->fgFirstBB->bbWeight;

            if (calledCount == 0)
            {
                calledCount = BB_UNITY_WEIGHT;
            }
        }
    }
    return calledCount;
}

//------------------------------------------------------------------------
// getBBWeight: get the normalized weight of this block
//
// Arguments:
//    compiler - Compiler instance
//
// Notes:
//    with profie data: number of expected executions of this block, given
//    one call to the method
//
weight_t BasicBlock::getBBWeight(Compiler* comp)
{
    if (this->bbWeight == BB_ZERO_WEIGHT)
    {
        return BB_ZERO_WEIGHT;
    }
    else
    {
        weight_t calledCount = getCalledCount(comp);

        // Normalize the bbWeight.
        //
        weight_t fullResult = (this->bbWeight / calledCount) * BB_UNITY_WEIGHT;

        return fullResult;
    }
}

//------------------------------------------------------------------------
// bbStackDepthOnEntry: return depth of IL stack at block entry
//
unsigned BasicBlock::bbStackDepthOnEntry() const
{
    return (bbEntryState ? bbEntryState->esStackDepth : 0);
}

//------------------------------------------------------------------------
// bbSetStack: update IL stack for block entry
//
// Arguments;
//   stack - new stack for block
//
void BasicBlock::bbSetStack(StackEntry* stack)
{
    assert(bbEntryState);
    assert(stack);
    bbEntryState->esStack = stack;
}

//------------------------------------------------------------------------
// bbStackOnEntry: fetch IL stack for block entry
//
StackEntry* BasicBlock::bbStackOnEntry() const
{
    assert(bbEntryState);
    return bbEntryState->esStack;
}
