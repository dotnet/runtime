// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef DEBUG
flowList* ShuffleHelper(unsigned hash, flowList* res)
{
    flowList* head = res;
    for (flowList *prev = nullptr; res != nullptr; prev = res, res = res->flNext)
    {
        unsigned blkHash = (hash ^ (res->flBlock->bbNum << 16) ^ res->flBlock->bbNum);
        if (((blkHash % 1879) & 1) && prev != nullptr)
        {
            // Swap res with head.
            prev->flNext = head;
            jitstd::swap(head->flNext, res->flNext);
            jitstd::swap(head, res);
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

EHSuccessorIter::EHSuccessorIter(Compiler* comp, BasicBlock* block)
    : m_comp(comp)
    , m_block(block)
    , m_curRegSucc(nullptr)
    , m_curTry(comp->ehGetBlockExnFlowDsc(block))
    , m_remainingRegSuccs(block->NumSucc(comp))
{
    // If "block" is a "leave helper" block (the empty BBJ_ALWAYS block that pairs with a
    // preceding BBJ_CALLFINALLY block to implement a "leave" IL instruction), then no exceptions
    // can occur within it, so clear m_curTry if it's non-null.
    if (m_curTry != nullptr)
    {
        BasicBlock* beforeBlock = block->bbPrev;
        if (beforeBlock != nullptr && beforeBlock->isBBCallAlwaysPair())
        {
            m_curTry = nullptr;
        }
    }

    if (m_curTry == nullptr && m_remainingRegSuccs > 0)
    {
        // Examine the successors to see if any are the start of try blocks.
        FindNextRegSuccTry();
    }
}

void EHSuccessorIter::FindNextRegSuccTry()
{
    assert(m_curTry == nullptr);

    // Must now consider the next regular successor, if any.
    while (m_remainingRegSuccs > 0)
    {
        m_remainingRegSuccs--;
        m_curRegSucc = m_block->GetSucc(m_remainingRegSuccs, m_comp);
        if (m_comp->bbIsTryBeg(m_curRegSucc))
        {
            assert(m_curRegSucc->hasTryIndex()); // Since it is a try begin.
            unsigned newTryIndex = m_curRegSucc->getTryIndex();

            // If the try region started by "m_curRegSucc" (represented by newTryIndex) contains m_block,
            // we've already yielded its handler, as one of the EH handler successors of m_block itself.
            if (m_comp->bbInExnFlowRegions(newTryIndex, m_block))
            {
                continue;
            }

            // Otherwise, consider this try.
            m_curTry = m_comp->ehGetDsc(newTryIndex);
            break;
        }
    }
}

void EHSuccessorIter::operator++(void)
{
    assert(m_curTry != nullptr);
    if (m_curTry->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
    {
        m_curTry = m_comp->ehGetDsc(m_curTry->ebdEnclosingTryIndex);

        // If we've gone over into considering try's containing successors,
        // then the enclosing try must have the successor as its first block.
        if (m_curRegSucc == nullptr || m_curTry->ebdTryBeg == m_curRegSucc)
        {
            return;
        }

        // Otherwise, give up, try the next regular successor.
        m_curTry = nullptr;
    }
    else
    {
        m_curTry = nullptr;
    }

    // We've exhausted all try blocks.
    // See if there are any remaining regular successors that start try blocks.
    FindNextRegSuccTry();
}

BasicBlock* EHSuccessorIter::operator*()
{
    assert(m_curTry != nullptr);
    return m_curTry->ExFlowBlock();
}

flowList* Compiler::BlockPredsWithEH(BasicBlock* blk)
{
    BlockToFlowListMap* ehPreds = GetBlockToEHPreds();
    flowList*           res;
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
        for (flowList* tryStartPreds = tryStart->bbPreds; tryStartPreds != nullptr;
             tryStartPreds           = tryStartPreds->flNext)
        {
            res = new (this, CMK_FlowList) flowList(tryStartPreds->flBlock, res);

#if MEASURE_BLOCK_SIZE
            genFlowNodeCnt += 1;
            genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE
        }

        // Now add all blocks handled by this handler (except for second blocks of BBJ_CALLFINALLY/BBJ_ALWAYS pairs;
        // these cannot cause transfer to the handler...)
        BasicBlock* prevBB = nullptr;

        // TODO-Throughput: It would be nice if we could iterate just over the blocks in the try, via
        // something like:
        //   for (BasicBlock* bb = ehblk->ebdTryBeg; bb != ehblk->ebdTryLast->bbNext; bb = bb->bbNext)
        //     (plus adding in any filter blocks outside the try whose exceptions are handled here).
        // That doesn't work, however: funclets have caused us to sometimes split the body of a try into
        // more than one sequence of contiguous blocks.  We need to find a better way to do this.
        for (BasicBlock *bb = fgFirstBB; bb != nullptr; prevBB = bb, bb = bb->bbNext)
        {
            if (bbInExnFlowRegions(tryIndex, bb) && (prevBB == nullptr || !prevBB->isBBCallAlwaysPair()))
            {
                res = new (this, CMK_FlowList) flowList(bb, res);

#if MEASURE_BLOCK_SIZE
                genFlowNodeCnt += 1;
                genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE
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

#ifdef DEBUG

//------------------------------------------------------------------------
// dspBlockILRange(): Display the block's IL range as [XXX...YYY), where XXX and YYY might be "???" for BAD_IL_OFFSET.
//
void BasicBlock::dspBlockILRange()
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
    if (bbFlags & BBF_VISITED)
    {
        printf("v ");
    }
    if (bbFlags & BBF_MARKED)
    {
        printf("m ");
    }
    if (bbFlags & BBF_CHANGED)
    {
        printf("! ");
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
    if (bbFlags & BBF_NEEDS_GCPOLL)
    {
        printf("poll ");
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
    if (bbFlags & BBF_JMP_TARGET)
    {
        printf("target ");
    }
    if (bbFlags & BBF_HAS_JMP)
    {
        printf("jmp ");
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
    if (bbFlags & BBF_HAS_NEWARRAY)
    {
        printf("new[] ");
    }
    if (bbFlags & BBF_HAS_NEWOBJ)
    {
        printf("newobj ");
    }
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (bbFlags & BBF_FINALLY_TARGET)
    {
        printf("ftarget ");
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    if (bbFlags & BBF_BACKWARD_JUMP)
    {
        printf("bwd ");
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
#ifdef LEGACY_BACKEND
    if (bbFlags & BBF_FORWARD_SWITCH)
    {
        printf("fswitch ");
    }
#else  // !LEGACY_BACKEND
    if (bbFlags & BBF_IS_LIR)
    {
        printf("LIR ");
    }
#endif // LEGACY_BACKEND
    if (bbFlags & BBF_KEEP_BBJ_ALWAYS)
    {
        printf("KEEP ");
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
    for (flowList* pred = bbPreds; pred != nullptr; pred = pred->flNext)
    {
        if (count != 0)
        {
            printf(",");
            count += 1;
        }
        printf("BB%02u", pred->flBlock->bbNum);
        count += 4;

        // Account for %02u only handling 2 digits, but we can display more than that.
        unsigned digits = CountDigits(pred->flBlock->bbNum);
        if (digits > 2)
        {
            count += digits - 2;
        }

        // Does this predecessor have an interesting dup count? If so, display it.
        if (pred->flDupCount > 1)
        {
            printf("(%u)", pred->flDupCount);
            count += 2 + CountDigits(pred->flDupCount);
        }
    }
    return count;
}

/*****************************************************************************
 *
 *  Display the bbCheapPreds basic block list (the block predecessors).
 *  Returns the number of characters printed.
 */

unsigned BasicBlock::dspCheapPreds()
{
    unsigned count = 0;
    for (BasicBlockList* pred = bbCheapPreds; pred != nullptr; pred = pred->next)
    {
        if (count != 0)
        {
            printf(",");
            count += 1;
        }
        printf("BB%02u", pred->block->bbNum);
        count += 4;

        // Account for %02u only handling 2 digits, but we can display more than that.
        unsigned digits = CountDigits(pred->block->bbNum);
        if (digits > 2)
        {
            count += digits - 2;
        }
    }
    return count;
}

/*****************************************************************************
 *
 *  Display the basic block successors.
 *  Returns the count of successors.
 */

unsigned BasicBlock::dspSuccs(Compiler* compiler)
{
    unsigned numSuccs = NumSucc(compiler);
    unsigned count    = 0;
    for (unsigned i = 0; i < numSuccs; i++)
    {
        printf("%s", (count == 0) ? "" : ",");
        printf("BB%02u", GetSucc(i, compiler)->bbNum);
        count++;
    }
    return count;
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

        case BBJ_EHFILTERRET:
            printf(" (fltret)");
            break;

        case BBJ_EHCATCHRET:
            printf(" -> BB%02u (cret)", bbJumpDest->bbNum);
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
                printf(" -> BB%02u (ALWAYS)", bbJumpDest->bbNum);
            }
            else
            {
                printf(" -> BB%02u (always)", bbJumpDest->bbNum);
            }
            break;

        case BBJ_LEAVE:
            printf(" -> BB%02u (leave)", bbJumpDest->bbNum);
            break;

        case BBJ_CALLFINALLY:
            printf(" -> BB%02u (callf)", bbJumpDest->bbNum);
            break;

        case BBJ_COND:
            printf(" -> BB%02u (cond)", bbJumpDest->bbNum);
            break;

        case BBJ_SWITCH:
            printf(" ->");

            unsigned jumpCnt;
            jumpCnt = bbJumpSwt->bbsCount;
            BasicBlock** jumpTab;
            jumpTab = bbJumpSwt->bbsDstTab;
            do
            {
                printf("%cBB%02u", (jumpTab == bbJumpSwt->bbsDstTab) ? ' ' : ',', (*jumpTab)->bbNum);
            } while (++jumpTab, --jumpCnt);

            printf(" (switch)");
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
    printf("BB%02u ", bbNum);
    dspBlockILRange();
    if (showKind)
    {
        dspJumpKind();
    }
    if (showPreds)
    {
        printf(", preds={");
        if (compiler->fgCheapPredsValid)
        {
            dspCheapPreds();
        }
        else
        {
            dspPreds();
        }
        printf("} succs={");
        dspSuccs(compiler);
        printf("}");
    }
    if (showFlags)
    {
        printf(" flags=0x%08x: ", bbFlags);
        dspFlags();
    }
    printf("\n");
}

#endif // DEBUG

// Allocation function for HeapPhiArg.
void* BasicBlock::HeapPhiArg::operator new(size_t sz, Compiler* comp)
{
    return comp->compGetMem(sz, CMK_HeapPhiArg);
}

void BasicBlock::CloneBlockState(Compiler* compiler, BasicBlock* to, const BasicBlock* from)
{
    assert(to->bbTreeList == nullptr);

    to->bbFlags  = from->bbFlags;
    to->bbWeight = from->bbWeight;
    BlockSetOps::AssignAllowUninitRhs(compiler, to->bbReach, from->bbReach);
    to->copyEHRegion(from);
    to->bbCatchTyp    = from->bbCatchTyp;
    to->bbRefs        = from->bbRefs;
    to->bbStkTempsIn  = from->bbStkTempsIn;
    to->bbStkTempsOut = from->bbStkTempsOut;
    to->bbStkDepth    = from->bbStkDepth;
    to->bbCodeOffs    = from->bbCodeOffs;
    to->bbCodeOffsEnd = from->bbCodeOffsEnd;
    VarSetOps::AssignAllowUninitRhs(compiler, to->bbScope, from->bbScope);
#if FEATURE_STACK_FP_X87
    to->bbFPStateX87 = from->bbFPStateX87;
#endif // FEATURE_STACK_FP_X87
    to->bbNatLoopNum = from->bbNatLoopNum;
#ifdef DEBUG
    to->bbLoopNum     = from->bbLoopNum;
    to->bbTgtStkDepth = from->bbTgtStkDepth;
#endif // DEBUG

    for (GenTreePtr fromStmt = from->bbTreeList; fromStmt != nullptr; fromStmt = fromStmt->gtNext)
    {
        compiler->fgInsertStmtAtEnd(to,
                                    compiler->fgNewStmtFromTree(compiler->gtCloneExpr(fromStmt->gtStmt.gtStmtExpr)));
    }
}

// LIR helpers
void BasicBlock::MakeLIR(GenTree* firstNode, GenTree* lastNode)
{
#ifdef LEGACY_BACKEND
    unreached();
#else  // !LEGACY_BACKEND
    assert(!IsLIR());
    assert((firstNode == nullptr) == (lastNode == nullptr));
    assert((firstNode == lastNode) || firstNode->Precedes(lastNode));

    m_firstNode = firstNode;
    m_lastNode  = lastNode;
    bbFlags |= BBF_IS_LIR;
#endif // LEGACY_BACKEND
}

bool BasicBlock::IsLIR()
{
#ifdef LEGACY_BACKEND
    return false;
#else  // !LEGACY_BACKEND
    const bool isLIR = (bbFlags & BBF_IS_LIR) != 0;
    assert((bbTreeList == nullptr) || ((isLIR) == !bbTreeList->IsStatement()));
    return isLIR;
#endif // LEGACY_BACKEND
}

//------------------------------------------------------------------------
// firstStmt: Returns the first statement in the block
//
// Arguments:
//    None.
//
// Return Value:
//    The first statement in the block's bbTreeList.
//
GenTreeStmt* BasicBlock::firstStmt()
{
    if (bbTreeList == nullptr)
    {
        return nullptr;
    }

    return bbTreeList->AsStmt();
}

//------------------------------------------------------------------------
// lastStmt: Returns the last statement in the block
//
// Arguments:
//    None.
//
// Return Value:
//    The last statement in the block's bbTreeList.
//
GenTreeStmt* BasicBlock::lastStmt()
{
    if (bbTreeList == nullptr)
    {
        return nullptr;
    }

    GenTree* result = bbTreeList->gtPrev;
    assert(result && result->gtNext == nullptr);
    return result->AsStmt();
}


//------------------------------------------------------------------------
// BasicBlock::firstNode: Returns the first node in the block.
//
GenTree* BasicBlock::firstNode()
{
    return IsLIR() ? bbTreeList : Compiler::fgGetFirstNode(firstStmt()->gtStmtExpr);
}

//------------------------------------------------------------------------
// BasicBlock::lastNode: Returns the last node in the block.
//
GenTree* BasicBlock::lastNode()
{
    return IsLIR() ? m_lastNode : lastStmt()->gtStmtExpr;
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

BasicBlock* BasicBlock::GetUniquePred(Compiler* compiler)
{
    if ((bbPreds == nullptr) || (bbPreds->flNext != nullptr) || (this == compiler->fgFirstBB))
    {
        return nullptr;
    }
    else
    {
        return bbPreds->flBlock;
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

BasicBlock* BasicBlock::GetUniqueSucc()
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
BasicBlock::HeapPhiArg* BasicBlock::EmptyHeapPhiDef = (BasicBlock::HeapPhiArg*)0x1;

unsigned PtrKeyFuncs<BasicBlock>::GetHashCode(const BasicBlock* ptr)
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

bool BasicBlock::isEmpty()
{
    if (!IsLIR())
    {
        return (this->FirstNonPhiDef() == nullptr);
    }

    for (GenTree* node : LIR::AsRange(this).NonPhiNodes())
    {
        if (node->OperGet() != GT_IL_OFFSET)
        {
            return false;
        }
    }

    return true;
}
