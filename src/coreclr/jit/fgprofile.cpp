// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Profile Support

//------------------------------------------------------------------------
// fgHaveProfileData: check if profile data is available
//
// Returns:
//   true if so
//
// Note:
//   This now returns true for inlinees. We might consider preserving the
//   old behavior for crossgen, since crossgen BBINSTRs still do inlining
//   and don't instrument the inlinees.
//
//   Thus if BBINSTR and BBOPT do the same inlines (which can happen)
//   profile data for an inlinee (if available) will not fully reflect
//   the behavior of the inlinee when called from this method.
//
//   If this inlinee was not inlined by the BBINSTR run then the
//   profile data for the inlinee will reflect this method's influence.
//
//   * for ALWAYS_INLINE and FORCE_INLINE cases it is unlikely we'll find
//     any profile data, as BBINSTR and BBOPT callers will both inline;
//     only indirect callers will invoke the instrumented version to run.
//   * for DISCRETIONARY_INLINE cases we may or may not find relevant
//     data, depending, but chances are the data is relevant.
//
//  TieredPGO data comes from Tier0 methods, which currently do not do
//  any inlining; thus inlinee profile data should be available and
//  representative.
//
bool Compiler::fgHaveProfileData()
{
    if (compIsForImportOnly())
    {
        return false;
    }

    return (fgPgoSchema != nullptr);
}

//------------------------------------------------------------------------
// fgComputeProfileScale: determine how much scaling to apply
//   to raw profile count data.
//
// Notes:
//   Scaling is only needed for inlinees, and the results of this
//   computation are recorded in fields of impInlineInfo.
//
void Compiler::fgComputeProfileScale()
{
    // Only applicable to inlinees
    assert(compIsForInlining());

    // Have we already determined the scale?
    if (impInlineInfo->profileScaleState != InlineInfo::ProfileScaleState::UNDETERMINED)
    {
        return;
    }

    // No, not yet -- try and compute the scale.
    JITDUMP("Computing inlinee profile scale:\n");

    // Call site has profile weight?
    //
    // Todo: handle case of unprofiled caller invoking profiled callee.
    //
    const BasicBlock* callSiteBlock = impInlineInfo->iciBlock;
    if (!callSiteBlock->hasProfileWeight())
    {
        JITDUMP("   ... call site not profiled\n");
        impInlineInfo->profileScaleState = InlineInfo::ProfileScaleState::UNAVAILABLE;
        return;
    }

    const BasicBlock::weight_t callSiteWeight = callSiteBlock->bbWeight;

    // Call site has zero count?
    //
    // Todo: perhaps retain some semblance of callee profile data,
    // possibly scaled down severely.
    //
    if (callSiteWeight == 0)
    {
        JITDUMP("   ... zero call site count\n");
        impInlineInfo->profileScaleState = InlineInfo::ProfileScaleState::UNAVAILABLE;
        return;
    }

    // Callee has profile data?
    //
    if (!fgHaveProfileData())
    {
        JITDUMP("   ... no callee profile data\n");
        impInlineInfo->profileScaleState = InlineInfo::ProfileScaleState::UNAVAILABLE;
        return;
    }

    // Find callee's unscaled entry weight.
    //
    // Ostensibly this should be fgCalledCount for the callee, but that's not available
    // as it requires some analysis.
    //
    // For most callees it will be the same as the entry block count.
    //
    if (!fgFirstBB->hasProfileWeight())
    {
        JITDUMP("   ... no callee profile data for entry block\n");
        impInlineInfo->profileScaleState = InlineInfo::ProfileScaleState::UNAVAILABLE;
        return;
    }

    // Note when/if we do normalization this may need to change.
    //
    BasicBlock::weight_t calleeWeight = fgFirstBB->bbWeight;

    // We should generally be able to assume calleeWeight >= callSiteWeight.
    // If this isn't so, perhaps something is wrong with the profile data
    // collection or retrieval.
    //
    if (calleeWeight < callSiteWeight)
    {
        JITDUMP("   ... callee entry count %f is less than call site count %f\n", calleeWeight, callSiteWeight);
        impInlineInfo->profileScaleState = InlineInfo::ProfileScaleState::UNAVAILABLE;
        return;
    }

    // Hence, scale is always in the range (0.0...1.0] -- we are always scaling down callee counts.
    //
    const double scale                = ((double)callSiteWeight) / calleeWeight;
    impInlineInfo->profileScaleFactor = scale;
    impInlineInfo->profileScaleState  = InlineInfo::ProfileScaleState::KNOWN;

    JITDUMP("   call site count %f callee entry count %f scale %f\n", callSiteWeight, calleeWeight, scale);
}

//------------------------------------------------------------------------
// fgGetProfileWeightForBasicBlock: obtain profile data for a block
//
// Arguments:
//   offset       - IL offset of the block
//   weightWB     - [OUT] weight obtained
//
// Returns:
//   true if data was found
//
bool Compiler::fgGetProfileWeightForBasicBlock(IL_OFFSET offset, BasicBlock::weight_t* weightWB)
{
    noway_assert(weightWB != nullptr);
    BasicBlock::weight_t weight = 0;

#ifdef DEBUG
    unsigned hashSeed = fgStressBBProf();
    if (hashSeed != 0)
    {
        unsigned hash = (info.compMethodHash() * hashSeed) ^ (offset * 1027);

        // We need to especially stress the procedure splitting codepath.  Therefore
        // one third the time we should return a weight of zero.
        // Otherwise we should return some random weight (usually between 0 and 288).
        // The below gives a weight of zero, 44% of the time

        if (hash % 3 == 0)
        {
            weight = 0;
        }
        else if (hash % 11 == 0)
        {
            weight = (BasicBlock::weight_t)(hash % 23) * (hash % 29) * (hash % 31);
        }
        else
        {
            weight = (BasicBlock::weight_t)(hash % 17) * (hash % 19);
        }

        // The first block is never given a weight of zero
        if ((offset == 0) && (weight == 0))
        {
            weight = (BasicBlock::weight_t)1 + (hash % 5);
        }

        *weightWB = weight;
        return true;
    }
#endif // DEBUG

    if (!fgHaveProfileData())
    {
        return false;
    }

    for (UINT32 i = 0; i < fgPgoSchemaCount; i++)
    {
        if ((fgPgoSchema[i].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount) &&
            ((IL_OFFSET)fgPgoSchema[i].ILOffset == offset))
        {
            *weightWB = (BasicBlock::weight_t) * (uint32_t*)(fgPgoData + fgPgoSchema[i].Offset);
            return true;
        }
    }

    *weightWB = 0;
    return true;
}

typedef jitstd::vector<ICorJitInfo::PgoInstrumentationSchema> Schema;

//------------------------------------------------------------------------
// Instrumentor: base class for count and class instrumentation
//
class Instrumentor
{
protected:
    Compiler* m_comp;
    unsigned  m_schemaCount;
    unsigned  m_instrCount;

protected:
    Instrumentor(Compiler* comp) : m_comp(comp), m_schemaCount(0), m_instrCount(0)
    {
    }

public:
    virtual bool ShouldProcess(BasicBlock* block)
    {
        return false;
    }
    virtual void Prepare(bool preImport)
    {
    }
    virtual void BuildSchemaElements(BasicBlock* block, Schema& schema)
    {
    }
    virtual void Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory)
    {
    }
    virtual void InstrumentMethodEntry(Schema& schema, BYTE* profileMemory)
    {
    }
    virtual void SuppressProbes()
    {
    }
    unsigned SchemaCount()
    {
        return m_schemaCount;
    }
    unsigned InstrCount()
    {
        return m_instrCount;
    }
};

//------------------------------------------------------------------------
// NonInstrumentor: instrumentor that does not instrument anything
//
class NonInstrumentor : public Instrumentor
{
public:
    NonInstrumentor(Compiler* comp) : Instrumentor(comp)
    {
    }
};

//------------------------------------------------------------------------
// BlockCountInstrumentor: instrumentor that adds a counter to each
//   non-internal imported basic block
//
class BlockCountInstrumentor : public Instrumentor
{
public:
    BlockCountInstrumentor(Compiler* comp) : Instrumentor(comp)
    {
    }
    bool ShouldProcess(BasicBlock* block) override
    {
        return ((block->bbFlags & (BBF_INTERNAL | BBF_IMPORTED)) == BBF_IMPORTED);
    }
    void Prepare(bool isPreImport) override;
    void BuildSchemaElements(BasicBlock* block, Schema& schema) override;
    void Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory) override;
    void InstrumentMethodEntry(Schema& schema, BYTE* profileMemory) override;
};

//------------------------------------------------------------------------
// BlockCountInstrumentor::Prepare: prepare for count instrumentation
//
// Arguments:
//   preImport - true if this is the prepare call that happens before
//      importation
//
void BlockCountInstrumentor::Prepare(bool preImport)
{
    if (preImport)
    {
        return;
    }

#ifdef DEBUG
    // Set schema index to invalid value
    //
    for (BasicBlock* block = m_comp->fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        block->bbCountSchemaIndex = -1;
    }
#endif
}

//------------------------------------------------------------------------
// BlockCountInstrumentor::BuildSchemaElements: create schema elements for a block counter
//
// Arguments:
//   block -- block to instrument
//   schema -- schema that we're building
//
void BlockCountInstrumentor::BuildSchemaElements(BasicBlock* block, Schema& schema)
{
    // Remember the schema index for this block.
    //
    assert(block->bbCountSchemaIndex == -1);
    block->bbCountSchemaIndex = (int)schema.size();

    // Assign the current block's IL offset into the profile data
    // (make sure IL offset is sane)
    //
    IL_OFFSET offset = block->bbCodeOffs;
    assert((int)offset >= 0);

    ICorJitInfo::PgoInstrumentationSchema schemaElem;
    schemaElem.Count               = 1;
    schemaElem.Other               = 0;
    schemaElem.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount;
    schemaElem.ILOffset            = offset;
    schemaElem.Offset              = 0;

    schema.push_back(schemaElem);

    m_schemaCount++;
}

//------------------------------------------------------------------------
// BlockCountInstrumentor::Instrument: add counter probe to block
//
// Arguments:
//   block -- block of interest
//   schema -- instrumentation schema
//   profileMemory -- profile data slab
//
void BlockCountInstrumentor::Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory)
{
    const int schemaIndex = (int)block->bbCountSchemaIndex;

    assert(block->bbCodeOffs == (IL_OFFSET)schema[schemaIndex].ILOffset);
    assert(schema[schemaIndex].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount);
    size_t addrOfCurrentExecutionCount = (size_t)(schema[schemaIndex].Offset + profileMemory);

    // Read Basic-Block count value
    GenTree* valueNode =
        m_comp->gtNewIndOfIconHandleNode(TYP_INT, addrOfCurrentExecutionCount, GTF_ICON_BBC_PTR, false);

    // Increment value by 1
    GenTree* rhsNode = m_comp->gtNewOperNode(GT_ADD, TYP_INT, valueNode, m_comp->gtNewIconNode(1));

    // Write new Basic-Block count value
    GenTree* lhsNode = m_comp->gtNewIndOfIconHandleNode(TYP_INT, addrOfCurrentExecutionCount, GTF_ICON_BBC_PTR, false);
    GenTree* asgNode = m_comp->gtNewAssignNode(lhsNode, rhsNode);

    m_comp->fgNewStmtAtBeg(block, asgNode);

    m_instrCount++;
}

//------------------------------------------------------------------------
// BlockCountInstrumentor::InstrumentMethodEntry: add any special method entry instrumentation
//
// Arguments:
//   schema -- instrumentation schema
//   profileMemory -- profile data slab
//
// Notes:
//   When prejitting, add the method entry callback node
//
void BlockCountInstrumentor::InstrumentMethodEntry(Schema& schema, BYTE* profileMemory)
{
    Compiler::Options& opts = m_comp->opts;
    Compiler::Info&    info = m_comp->info;

    // Nothing to do, if not prejitting.
    //
    if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        return;
    }

    // Find the address of the entry block's counter.
    //
    BasicBlock* const block            = m_comp->fgFirstBB;
    const int         firstSchemaIndex = block->bbCountSchemaIndex;
    assert(block->bbCodeOffs == (IL_OFFSET)schema[firstSchemaIndex].ILOffset);
    assert(schema[firstSchemaIndex].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount);
    size_t addrOfFirstExecutionCount = (size_t)(schema[firstSchemaIndex].Offset + profileMemory);

    GenTree* arg;

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        mdMethodDef currentMethodToken = info.compCompHnd->getMethodDefFromMethod(info.compMethodHnd);

        CORINFO_RESOLVED_TOKEN resolvedToken;
        resolvedToken.tokenContext = MAKE_METHODCONTEXT(info.compMethodHnd);
        resolvedToken.tokenScope   = info.compScopeHnd;
        resolvedToken.token        = currentMethodToken;
        resolvedToken.tokenType    = CORINFO_TOKENKIND_Method;

        info.compCompHnd->resolveToken(&resolvedToken);

        arg = m_comp->impTokenToHandle(&resolvedToken);
    }
    else
#endif
    {
        arg = m_comp->gtNewIconEmbMethHndNode(info.compMethodHnd);
    }

    GenTreeCall::Use* args = m_comp->gtNewCallArgs(arg);
    GenTree*          call = m_comp->gtNewHelperCallNode(CORINFO_HELP_BBT_FCN_ENTER, TYP_VOID, args);

    // Read Basic-Block count value
    GenTree* valueNode = m_comp->gtNewIndOfIconHandleNode(TYP_INT, addrOfFirstExecutionCount, GTF_ICON_BBC_PTR, false);

    // Compare Basic-Block count value against zero
    GenTree*   relop = m_comp->gtNewOperNode(GT_NE, TYP_INT, valueNode, m_comp->gtNewIconNode(0, TYP_INT));
    GenTree*   colon = new (m_comp, GT_COLON) GenTreeColon(TYP_VOID, m_comp->gtNewNothingNode(), call);
    GenTree*   cond  = m_comp->gtNewQmarkNode(TYP_VOID, relop, colon);
    Statement* stmt  = m_comp->gtNewStmt(cond);

    m_comp->fgEnsureFirstBBisScratch();
    m_comp->fgInsertStmtAtEnd(block, stmt);
}

//------------------------------------------------------------------------
// SpanningTreeVisitor: abstract class for computations done while
//   evolving a spanning tree.
//
class SpanningTreeVisitor
{
public:
    // To save visitors a bit of work, we also note
    // for non-tree edges whether the edge postdominates
    // the source, dominates the target, or is a critical edge.
    //
    enum class EdgeKind
    {
        Unknown,
        PostdominatesSource,
        DominatesTarget,
        CriticalEdge
    };

    virtual void Badcode()                     = 0;
    virtual void VisitBlock(BasicBlock* block) = 0;
    virtual void VisitTreeEdge(BasicBlock* source, BasicBlock* target) = 0;
    virtual void VisitNonTreeEdge(BasicBlock* source, BasicBlock* target, EdgeKind kind) = 0;
};

//------------------------------------------------------------------------
// WalkSpanningTree: evolve a "maximal cost" depth first spanning tree,
//   invoking the visitor as each edge is classified, or each node is first
//   discovered.
//
// Arguments:
//    visitor - visitor to notify
//
// Notes:
//   We only have rudimentary weights at this stage, and so in practice
//   we use a depth-first spanning tree (DFST) where we try to steer
//   the DFS to preferentially visit "higher" cost edges.
//
//   Since instrumentation happens after profile incorporation
//   we could in principle use profile weights to steer the DFS or to build
//   a true maximum weight tree. However we are relying on being able to
//   rebuild the exact same spanning tree "later on" when doing a subsequent
//   profile reconstruction. So, we restrict ourselves to just using
//   information apparent in the IL.
//
void Compiler::WalkSpanningTree(SpanningTreeVisitor* visitor)
{
    // Inlinee compilers build their blocks in the root compiler's
    // graph. So for BlockSets and NumSucc, we use the root compiler instance.
    //
    Compiler* const comp = impInlineRoot();
    comp->NewBasicBlockEpoch();

    // We will track visited or queued nodes with a bit vector.
    //
    BlockSet marked = BlockSetOps::MakeEmpty(comp);

    // And nodes to visit with a bit vector and stack.
    //
    ArrayStack<BasicBlock*> stack(getAllocator(CMK_Pgo));

    // Scratch vector for visiting successors of blocks with
    // multiple successors.
    //
    // Bit vector to track progress through those successors.
    //
    ArrayStack<BasicBlock*> scratch(getAllocator(CMK_Pgo));
    BlockSet                processed = BlockSetOps::MakeEmpty(comp);

    // Push the method entry and all EH handler region entries on the stack.
    // (push method entry last so it's visited first).
    //
    // Note inlinees are "contaminated" with root method EH structures.
    // We know the inlinee itself doesn't have EH, so we only look at
    // handlers for root methods.
    //
    // If we ever want to support inlining methods with EH, we'll
    // have to revisit this.
    //
    if (!compIsForInlining())
    {
        EHblkDsc* HBtab = compHndBBtab;
        unsigned  XTnum = 0;

        for (; XTnum < compHndBBtabCount; XTnum++, HBtab++)
        {
            BasicBlock* hndBegBB = HBtab->ebdHndBeg;
            stack.Push(hndBegBB);
            BlockSetOps::AddElemD(comp, marked, hndBegBB->bbNum);
        }
    }

    stack.Push(fgFirstBB);
    BlockSetOps::AddElemD(comp, marked, fgFirstBB->bbNum);

    unsigned nBlocks = 0;

    while (!stack.Empty())
    {
        BasicBlock* const block = stack.Pop();

        // Visit the block.
        //
        assert(BlockSetOps::IsMember(comp, marked, block->bbNum));
        visitor->VisitBlock(block);
        nBlocks++;

        switch (block->bbJumpKind)
        {
            case BBJ_CALLFINALLY:
            {
                // Just queue up the continuation block,
                // unless the finally doesn't return, in which
                // case we really should treat this block as a throw,
                // and so this block would get instrumented.
                //
                // Since our keying scheme is IL based and this
                // block has no IL offset, we'd need to invent
                // some new keying scheme. For now we just
                // ignore this (rare) case.
                //
                if (block->isBBCallAlwaysPair())
                {
                    // This block should be the only pred of the continuation.
                    //
                    BasicBlock* const target = block->bbNext;
                    assert(!BlockSetOps::IsMember(comp, marked, target->bbNum));
                    visitor->VisitTreeEdge(block, target);
                    stack.Push(target);
                    BlockSetOps::AddElemD(comp, marked, target->bbNum);
                }
            }
            break;

            case BBJ_RETURN:
            case BBJ_THROW:
            {
                // Pseudo-edge back to method entry.
                //
                // Note if the throw is caught locally this will over-state the profile
                // count for method entry. But we likely don't care too much about
                // profiles for methods that throw lots of exceptions.
                //
                BasicBlock* const target = fgFirstBB;
                assert(BlockSetOps::IsMember(comp, marked, target->bbNum));
                visitor->VisitNonTreeEdge(block, target, SpanningTreeVisitor::EdgeKind::PostdominatesSource);
            }
            break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
            case BBJ_LEAVE:
            {
                // See if we're leaving an EH handler region.
                //
                bool           isInTry     = false;
                unsigned const regionIndex = ehGetMostNestedRegionIndex(block, &isInTry);

                if (isInTry)
                {
                    // No, we're leaving a try or catch, not a handler.
                    // Treat this as a normal edge.
                    //
                    BasicBlock* const target = block->bbJumpDest;

                    // In some bad IL cases we may not have a target.
                    // In others we may see something other than LEAVE be most-nested in a try.
                    //
                    if (target == nullptr)
                    {
                        JITDUMP("No jump dest for " FMT_BB ", suspect bad code\n", block->bbNum);
                        visitor->Badcode();
                    }
                    else if (block->bbJumpKind != BBJ_LEAVE)
                    {
                        JITDUMP("EH RET in " FMT_BB " most-nested in try, suspect bad code\n", block->bbNum);
                        visitor->Badcode();
                    }
                    else
                    {
                        if (BlockSetOps::IsMember(comp, marked, target->bbNum))
                        {
                            visitor->VisitNonTreeEdge(block, target,
                                                      SpanningTreeVisitor::EdgeKind::PostdominatesSource);
                        }
                        else
                        {
                            visitor->VisitTreeEdge(block, target);
                            stack.Push(target);
                            BlockSetOps::AddElemD(comp, marked, target->bbNum);
                        }
                    }
                }
                else
                {
                    // Pseudo-edge back to handler entry.
                    //
                    EHblkDsc* const   dsc    = ehGetBlockHndDsc(block);
                    BasicBlock* const target = dsc->ebdHndBeg;
                    assert(BlockSetOps::IsMember(comp, marked, target->bbNum));
                    visitor->VisitNonTreeEdge(block, target, SpanningTreeVisitor::EdgeKind::PostdominatesSource);
                }
            }
            break;

            default:
            {
                // If this block is a control flow fork, we want to
                // preferentially visit critical edges first; if these
                // edges end up in the DFST then instrumentation will
                // require edge splitting.
                //
                // We also want to preferentially visit edges to rare
                // successors last, if this block is non-rare.
                //
                // It's not immediately clear if we should pass comp or this
                // to NumSucc here (for inlinees).
                //
                // It matters for FINALLYRET and for SWITCHES. Currently
                // we handle the first one specially, and it seems possible
                // things will just work for switches either way, but it
                // might work a bit better using the root compiler.
                //
                const unsigned numSucc = block->NumSucc(comp);

                if (numSucc == 1)
                {
                    // Not a fork. Just visit the sole successor.
                    //
                    BasicBlock* const target = block->GetSucc(0, comp);
                    if (BlockSetOps::IsMember(comp, marked, target->bbNum))
                    {
                        // We can't instrument in the call always pair tail block
                        // so treat this as a critical edge.
                        //
                        visitor->VisitNonTreeEdge(block, target,
                                                  block->isBBCallAlwaysPairTail()
                                                      ? SpanningTreeVisitor::EdgeKind::CriticalEdge
                                                      : SpanningTreeVisitor::EdgeKind::PostdominatesSource);
                    }
                    else
                    {
                        visitor->VisitTreeEdge(block, target);
                        stack.Push(target);
                        BlockSetOps::AddElemD(comp, marked, target->bbNum);
                    }
                }
                else
                {
                    // A block with multiple successors.
                    //
                    // Because we're using a stack up above, we work in reverse
                    // order of "cost" here --  so we first consider rare,
                    // then normal, then critical.
                    //
                    // That is, all things being equal we'd prefer to
                    // have critical edges be tree edges, and
                    // edges from non-rare to rare be non-tree edges.
                    //
                    scratch.Reset();
                    BlockSetOps::ClearD(comp, processed);

                    for (unsigned i = 0; i < numSucc; i++)
                    {
                        BasicBlock* const succ = block->GetSucc(i, comp);
                        scratch.Push(succ);
                    }

                    // Rare successors of non-rare blocks
                    //
                    for (unsigned i = 0; i < numSucc; i++)
                    {
                        BasicBlock* const target = scratch.Top(i);

                        if (BlockSetOps::IsMember(comp, processed, i))
                        {
                            continue;
                        }

                        if (block->isRunRarely() || !target->isRunRarely())
                        {
                            continue;
                        }

                        BlockSetOps::AddElemD(comp, processed, i);

                        if (BlockSetOps::IsMember(comp, marked, target->bbNum))
                        {
                            visitor->VisitNonTreeEdge(block, target,
                                                      target->bbRefs > 1
                                                          ? SpanningTreeVisitor::EdgeKind::CriticalEdge
                                                          : SpanningTreeVisitor::EdgeKind::DominatesTarget);
                        }
                        else
                        {
                            visitor->VisitTreeEdge(block, target);
                            stack.Push(target);
                            BlockSetOps::AddElemD(comp, marked, target->bbNum);
                        }
                    }

                    // Non-critical edges
                    //
                    for (unsigned i = 0; i < numSucc; i++)
                    {
                        BasicBlock* const target = scratch.Top(i);

                        if (BlockSetOps::IsMember(comp, processed, i))
                        {
                            continue;
                        }

                        if (target->bbRefs != 1)
                        {
                            continue;
                        }

                        BlockSetOps::AddElemD(comp, processed, i);

                        if (BlockSetOps::IsMember(comp, marked, target->bbNum))
                        {
                            visitor->VisitNonTreeEdge(block, target, SpanningTreeVisitor::EdgeKind::DominatesTarget);
                        }
                        else
                        {
                            visitor->VisitTreeEdge(block, target);
                            stack.Push(target);
                            BlockSetOps::AddElemD(comp, marked, target->bbNum);
                        }
                    }

                    // Critical edges
                    //
                    for (unsigned i = 0; i < numSucc; i++)
                    {
                        BasicBlock* const target = scratch.Top(i);

                        if (BlockSetOps::IsMember(comp, processed, i))
                        {
                            continue;
                        }

                        BlockSetOps::AddElemD(comp, processed, i);

                        if (BlockSetOps::IsMember(comp, marked, target->bbNum))
                        {
                            visitor->VisitNonTreeEdge(block, target, SpanningTreeVisitor::EdgeKind::CriticalEdge);
                        }
                        else
                        {
                            visitor->VisitTreeEdge(block, target);
                            stack.Push(target);
                            BlockSetOps::AddElemD(comp, marked, target->bbNum);
                        }
                    }

                    // Verify we processed each successor.
                    //
                    assert(numSucc == BlockSetOps::Count(comp, processed));
                }
            }
            break;
        }
    }
}

//------------------------------------------------------------------------
// EfficientEdgeCountInstrumentor: instrumentor that adds a counter to
//   selective edges.
//
// Based on "Optimally Profiling and Tracing Programs,"
// Ball and Larus PLDI '92.
//
class EfficientEdgeCountInstrumentor : public Instrumentor, public SpanningTreeVisitor
{
private:
    // A particular edge probe. These are linked
    // on the source block via bbSparseProbeList.
    //
    struct Probe
    {
        BasicBlock* target;
        Probe*      next;
        int         schemaIndex;
        EdgeKind    kind;
    };

    Probe* NewProbe(BasicBlock* source, BasicBlock* target)
    {
        Probe* p                  = new (m_comp, CMK_Pgo) Probe();
        p->target                 = target;
        p->kind                   = EdgeKind::Unknown;
        p->schemaIndex            = -1;
        p->next                   = (Probe*)source->bbSparseProbeList;
        source->bbSparseProbeList = p;
        m_probeCount++;

        return p;
    }

    void NewSourceProbe(BasicBlock* source, BasicBlock* target)
    {
        JITDUMP("[%u] New probe for " FMT_BB " -> " FMT_BB " [source]\n", m_probeCount, source->bbNum, target->bbNum);
        Probe* p = NewProbe(source, target);
        p->kind  = EdgeKind::PostdominatesSource;
    }

    void NewTargetProbe(BasicBlock* source, BasicBlock* target)
    {
        JITDUMP("[%u] New probe for " FMT_BB " -> " FMT_BB " [target]\n", m_probeCount, source->bbNum, target->bbNum);

        Probe* p = NewProbe(source, target);
        p->kind  = EdgeKind::DominatesTarget;
    }

    void NewEdgeProbe(BasicBlock* source, BasicBlock* target)
    {
        JITDUMP("[%u] New probe for " FMT_BB " -> " FMT_BB " [edge]\n", m_probeCount, source->bbNum, target->bbNum);

        Probe* p = NewProbe(source, target);
        p->kind  = EdgeKind::CriticalEdge;

        m_edgeProbeCount++;
    }

    unsigned m_blockCount;
    unsigned m_probeCount;
    unsigned m_edgeProbeCount;
    bool     m_badcode;

public:
    EfficientEdgeCountInstrumentor(Compiler* comp)
        : Instrumentor(comp)
        , SpanningTreeVisitor()
        , m_blockCount(0)
        , m_probeCount(0)
        , m_edgeProbeCount(0)
        , m_badcode(false)
    {
    }
    void Prepare(bool isPreImport) override;
    bool ShouldProcess(BasicBlock* block) override
    {
        return ((block->bbFlags & BBF_IMPORTED) == BBF_IMPORTED);
    }
    void BuildSchemaElements(BasicBlock* block, Schema& schema) override;
    void Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory) override;

    void Badcode() override
    {
        m_badcode = true;
    }

    void VisitBlock(BasicBlock* block) override
    {
        m_blockCount++;
        block->bbSparseProbeList = nullptr;
    }

    void VisitTreeEdge(BasicBlock* source, BasicBlock* target) override
    {
    }

    void VisitNonTreeEdge(BasicBlock* source, BasicBlock* target, SpanningTreeVisitor::EdgeKind kind) override
    {
        switch (kind)
        {
            case EdgeKind::PostdominatesSource:
                NewSourceProbe(source, target);
                break;
            case EdgeKind::DominatesTarget:
                NewTargetProbe(source, target);
                break;
            case EdgeKind::CriticalEdge:
                NewEdgeProbe(source, target);
                break;
            default:
                assert(!"unexpected kind");
                break;
        }
    }
};

//------------------------------------------------------------------------
// EfficientEdgeCountInstrumentor:Prepare: analyze the flow graph to
//   determine which edges should be instrumented.
//
// Arguments:
//   preImport - true if this is the prepare call that happens before
//      importation
//
// Notes:
//   Build a (maximum weight) spanning tree and designate the non-tree
//   edges as the ones needing instrumentation.
//
//   For non-critical edges, instrumentation happens in either the
//   predecessor or successor blocks.
//
//   Note we may only schematize and instrument a subset of the full
//   set of instrumentation envisioned here, if the method is partially
//   imported, as subsequent "passes" will bypass un-imported blocks.
//
//   It might be preferable to export the full schema but only
//   selectively instrument; this would make merging and importing
//   of data simpler, as all schemas for a method would agree, no
//   matter what importer-level opts were applied.
//
void EfficientEdgeCountInstrumentor::Prepare(bool preImport)
{
    if (!preImport)
    {
        // If we saw badcode in the preimport prepare, we would expect
        // compilation to blow up in the importer. So if we end up back
        // here postimport with badcode set, something is wrong.
        //
        assert(!m_badcode);
        return;
    }

    JITDUMP("\nEfficientEdgeCountInstrumentor: preparing for instrumentation\n");
    m_comp->WalkSpanningTree(this);
    JITDUMP("%u blocks, %u probes (%u on critical edges)\n", m_blockCount, m_probeCount, m_edgeProbeCount);
}

//------------------------------------------------------------------------
// EfficientEdgeCountInstrumentor:BuildSchemaElements: create schema
//   elements for the probes
//
// Arguments:
//   block -- block to instrument
//   schema -- schema that we're building
//
// Todo: if required to have special entry probe, we must also
//  instrument method entry with a block count.
//
void EfficientEdgeCountInstrumentor::BuildSchemaElements(BasicBlock* block, Schema& schema)
{
    // Walk the bbSparseProbeList, emitting one schema element per...
    //
    for (Probe* probe = (Probe*)block->bbSparseProbeList; probe != nullptr; probe = probe->next)
    {
        // Probe is for the edge from block to target.
        //
        BasicBlock* const target = probe->target;

        // Remember the schema index for this probe
        //
        assert(probe->schemaIndex == -1);
        probe->schemaIndex = (int)schema.size();

        // Assign the current block's IL offset into the profile data.
        // Use the "other" field to hold the target block IL offset.
        //
        int32_t sourceOffset = (int32_t)block->bbCodeOffs;
        int32_t targetOffset = (int32_t)target->bbCodeOffs;

        // We may see empty BBJ_NONE BBF_INTERNAL blocks that were added
        // by fgNormalizeEH.
        //
        // We'll use their bbNum in place of IL offset, and set
        // a high bit as a "flag"
        //
        if ((block->bbFlags & BBF_INTERNAL) == BBF_INTERNAL)
        {
            sourceOffset = block->bbNum | IL_OFFSETX_CALLINSTRUCTIONBIT;
        }

        if ((target->bbFlags & BBF_INTERNAL) == BBF_INTERNAL)
        {
            targetOffset = target->bbNum | IL_OFFSETX_CALLINSTRUCTIONBIT;
        }

        ICorJitInfo::PgoInstrumentationSchema schemaElem;
        schemaElem.Count               = 1;
        schemaElem.Other               = targetOffset;
        schemaElem.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::EdgeIntCount;
        schemaElem.ILOffset            = sourceOffset;
        schemaElem.Offset              = 0;

        schema.push_back(schemaElem);

        m_schemaCount++;
    }
}

//------------------------------------------------------------------------
// EfficientEdgeCountInstrumentor::Instrument: add counter probes for edges
//   originating from block
//
// Arguments:
//   block -- block of interest
//   schema -- instrumentation schema
//   profileMemory -- profile data slab
//
void EfficientEdgeCountInstrumentor::Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory)
{
    // Inlinee compilers build their blocks in the root compiler's
    // graph. So for NumSucc, we use the root compiler instance.
    //
    Compiler* const comp = m_comp->impInlineRoot();

    // Walk the bbSparseProbeList, adding instrumentation.
    //
    for (Probe* probe = (Probe*)block->bbSparseProbeList; probe != nullptr; probe = probe->next)
    {
        // Probe is for the edge from block to target.
        //
        BasicBlock* const target = probe->target;

        // Retrieve the schema index for this probe
        //
        const int schemaIndex = probe->schemaIndex;

        // Sanity checks.
        //
        assert((schemaIndex >= 0) && (schemaIndex < (int)schema.size()));
        assert(schema[schemaIndex].InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::EdgeIntCount);

        size_t addrOfCurrentExecutionCount = (size_t)(schema[schemaIndex].Offset + profileMemory);

        // Determine where to place the probe.
        //
        BasicBlock* instrumentedBlock = nullptr;

        switch (probe->kind)
        {
            case EdgeKind::PostdominatesSource:
                instrumentedBlock = block;
                break;
            case EdgeKind::DominatesTarget:
                instrumentedBlock = probe->target;
                break;
            case EdgeKind::CriticalEdge:
            {
#ifdef DEBUG
                // Verify the edge still exists.
                //
                const unsigned numSucc = block->NumSucc(comp);
                bool           found   = false;
                for (unsigned i = 0; i < numSucc && !found; i++)
                {
                    found = (target == block->GetSucc(i, comp));
                }
                assert(found);
#endif
                instrumentedBlock = m_comp->fgSplitEdge(block, probe->target);
                instrumentedBlock->bbFlags |= BBF_IMPORTED;
            }
            break;

            default:
                unreached();
        }

        assert(instrumentedBlock != nullptr);

        // Place the probe

        // Read Basic-Block count value
        GenTree* valueNode =
            m_comp->gtNewIndOfIconHandleNode(TYP_INT, addrOfCurrentExecutionCount, GTF_ICON_BBC_PTR, false);

        // Increment value by 1
        GenTree* rhsNode = m_comp->gtNewOperNode(GT_ADD, TYP_INT, valueNode, m_comp->gtNewIconNode(1));

        // Write new Basic-Block count value
        GenTree* lhsNode =
            m_comp->gtNewIndOfIconHandleNode(TYP_INT, addrOfCurrentExecutionCount, GTF_ICON_BBC_PTR, false);
        GenTree* asgNode = m_comp->gtNewAssignNode(lhsNode, rhsNode);

        m_comp->fgNewStmtAtBeg(instrumentedBlock, asgNode);

        m_instrCount++;
    }
}

//------------------------------------------------------------------------
// ClassProbeVisitor: invoke functor on each virtual call in a tree
//
template <class TFunctor>
class ClassProbeVisitor final : public GenTreeVisitor<ClassProbeVisitor<TFunctor>>
{
public:
    enum
    {
        DoPreOrder = true
    };

    TFunctor& m_functor;
    Compiler* m_compiler;

    ClassProbeVisitor(Compiler* compiler, TFunctor& functor)
        : GenTreeVisitor<ClassProbeVisitor>(compiler), m_functor(functor), m_compiler(compiler)
    {
    }
    Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* const node = *use;
        if (node->IsCall())
        {
            GenTreeCall* const call = node->AsCall();
            if (call->IsVirtual() && (call->gtCallType != CT_INDIRECT))
            {
                m_functor(m_compiler, call);
            }
        }

        return Compiler::WALK_CONTINUE;
    }
};

//------------------------------------------------------------------------
// BuildClassProbeSchemaGen: functor that creates class probe schema elements
//
class BuildClassProbeSchemaGen
{
private:
    Schema&   m_schema;
    unsigned& m_schemaCount;

public:
    BuildClassProbeSchemaGen(Schema& schema, unsigned& schemaCount) : m_schema(schema), m_schemaCount(schemaCount)
    {
    }

    void operator()(Compiler* compiler, GenTreeCall* call)
    {
        ICorJitInfo::PgoInstrumentationSchema schemaElem;
        schemaElem.Count = 1;
        schemaElem.Other = ICorJitInfo::ClassProfile::CLASS_FLAG;
        if (call->IsVirtualStub())
        {
            schemaElem.Other |= ICorJitInfo::ClassProfile::INTERFACE_FLAG;
        }
        else
        {
            assert(call->IsVirtualVtable());
        }

        schemaElem.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramCount;
        schemaElem.ILOffset            = jitGetILoffs(call->gtClassProfileCandidateInfo->ilOffset);
        schemaElem.Offset              = 0;

        m_schema.push_back(schemaElem);

        // Re-using ILOffset and Other fields from schema item for TypeHandleHistogramCount
        schemaElem.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramTypeHandle;
        schemaElem.Count               = ICorJitInfo::ClassProfile::SIZE;
        m_schema.push_back(schemaElem);

        m_schemaCount++;
    }
};

//------------------------------------------------------------------------
// ClassProbeInserter: functor that adds class probe instrumentation
//
class ClassProbeInserter
{
    Schema&   m_schema;
    BYTE*     m_profileMemory;
    int*      m_currentSchemaIndex;
    unsigned& m_instrCount;

public:
    ClassProbeInserter(Schema& schema, BYTE* profileMemory, int* pCurrentSchemaIndex, unsigned& instrCount)
        : m_schema(schema)
        , m_profileMemory(profileMemory)
        , m_currentSchemaIndex(pCurrentSchemaIndex)
        , m_instrCount(instrCount)
    {
    }

    void operator()(Compiler* compiler, GenTreeCall* call)
    {
        JITDUMP("Found call [%06u] with probe index %d and ilOffset 0x%X\n", compiler->dspTreeID(call),
                call->gtClassProfileCandidateInfo->probeIndex, call->gtClassProfileCandidateInfo->ilOffset);

        // We transform the call from (CALLVIRT obj, ... args ...) to
        // to
        //      (CALLVIRT
        //        (COMMA
        //          (ASG tmp, obj)
        //          (COMMA
        //            (CALL probe_fn tmp, &probeEntry)
        //            tmp)))
        //         ... args ...)
        //

        assert(call->gtCallThisArg->GetNode()->TypeGet() == TYP_REF);

        // Sanity check that we're looking at the right schema entry
        //
        assert(m_schema[*m_currentSchemaIndex].ILOffset == (int32_t)call->gtClassProfileCandidateInfo->ilOffset);
        assert(m_schema[*m_currentSchemaIndex].InstrumentationKind ==
               ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramCount);

        // Figure out where the table is located.
        //
        BYTE* classProfile = m_schema[*m_currentSchemaIndex].Offset + m_profileMemory;
        *m_currentSchemaIndex += 2; // There are 2 schema entries per class probe

        // Grab a temp to hold the 'this' object as it will be used three times
        //
        unsigned const tmpNum             = compiler->lvaGrabTemp(true DEBUGARG("class profile tmp"));
        compiler->lvaTable[tmpNum].lvType = TYP_REF;

        // Generate the IR...
        //
        GenTree* const          classProfileNode = compiler->gtNewIconNode((ssize_t)classProfile, TYP_I_IMPL);
        GenTree* const          tmpNode          = compiler->gtNewLclvNode(tmpNum, TYP_REF);
        GenTreeCall::Use* const args             = compiler->gtNewCallArgs(tmpNode, classProfileNode);
        GenTree* const helperCallNode = compiler->gtNewHelperCallNode(CORINFO_HELP_CLASSPROFILE, TYP_VOID, args);
        GenTree* const tmpNode2       = compiler->gtNewLclvNode(tmpNum, TYP_REF);
        GenTree* const callCommaNode  = compiler->gtNewOperNode(GT_COMMA, TYP_REF, helperCallNode, tmpNode2);
        GenTree* const tmpNode3       = compiler->gtNewLclvNode(tmpNum, TYP_REF);
        GenTree* const asgNode = compiler->gtNewOperNode(GT_ASG, TYP_REF, tmpNode3, call->gtCallThisArg->GetNode());
        GenTree* const asgCommaNode = compiler->gtNewOperNode(GT_COMMA, TYP_REF, asgNode, callCommaNode);

        // Update the call
        //
        call->gtCallThisArg->SetNode(asgCommaNode);

        JITDUMP("Modified call is now\n");
        DISPTREE(call);

        // Restore the stub address on the call
        //
        call->gtStubCallStubAddr = call->gtClassProfileCandidateInfo->stubAddr;

        m_instrCount++;
    }
};

//------------------------------------------------------------------------
// SuppressProbesFunctor: functor that resets IR back to the state
//   it had if there were no class probes.
//
class SuppressProbesFunctor
{
private:
    unsigned& m_cleanupCount;

public:
    SuppressProbesFunctor(unsigned& cleanupCount) : m_cleanupCount(cleanupCount)
    {
    }

    void operator()(Compiler* compiler, GenTreeCall* call)
    {
        // Restore the stub address on the call
        //
        call->gtStubCallStubAddr = call->gtClassProfileCandidateInfo->stubAddr;

        m_cleanupCount++;
    }
};

//------------------------------------------------------------------------
// ClassProbeInstrumentor: instrumentor that adds a class probe to each
//   virtual call in the basic block
//
class ClassProbeInstrumentor : public Instrumentor
{
public:
    ClassProbeInstrumentor(Compiler* comp) : Instrumentor(comp)
    {
    }
    bool ShouldProcess(BasicBlock* block) override
    {
        return ((block->bbFlags & (BBF_INTERNAL | BBF_IMPORTED)) == BBF_IMPORTED);
    }
    void Prepare(bool isPreImport) override;
    void BuildSchemaElements(BasicBlock* block, Schema& schema) override;
    void Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory) override;
    void SuppressProbes() override;
};

//------------------------------------------------------------------------
// ClassProbeInstrumentor::Prepare: prepare for class instrumentation
//
// Arguments:
//   preImport - true if this is the prepare call that happens before
//      importation
//
void ClassProbeInstrumentor::Prepare(bool isPreImport)
{
    if (isPreImport)
    {
        return;
    }

#ifdef DEBUG
    // Set schema index to invalid value
    //
    for (BasicBlock* block = m_comp->fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        block->bbClassSchemaIndex = -1;
    }
#endif
}

//------------------------------------------------------------------------
// ClassProbeInstrumentor::BuildSchemaElements: create schema elements for a class probe
//
// Arguments:
//   block -- block to instrument
//   schema -- schema that we're building
//
void ClassProbeInstrumentor::BuildSchemaElements(BasicBlock* block, Schema& schema)
{
    if ((block->bbFlags & BBF_HAS_CLASS_PROFILE) == 0)
    {
        return;
    }

    // Remember the schema index for this block.
    //
    block->bbClassSchemaIndex = (int)schema.size();

    // Scan the statements and identify the class probes
    //
    BuildClassProbeSchemaGen                    schemaGen(schema, m_schemaCount);
    ClassProbeVisitor<BuildClassProbeSchemaGen> visitor(m_comp, schemaGen);
    for (Statement* stmt : block->Statements())
    {
        visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    }
}

//------------------------------------------------------------------------
// ClassProbeInstrumentor::Instrument: add class probes to block
//
// Arguments:
//   block -- block of interest
//   schema -- instrumentation schema
//   profileMemory -- profile data slab
//
void ClassProbeInstrumentor::Instrument(BasicBlock* block, Schema& schema, BYTE* profileMemory)
{
    if ((block->bbFlags & BBF_HAS_CLASS_PROFILE) == 0)
    {
        return;
    }

    // Would be nice to avoid having to search here by tracking
    // candidates more directly.
    //
    JITDUMP("Scanning for calls to profile in " FMT_BB "\n", block->bbNum);

    // Scan the statements and add class probes
    //
    int classSchemaIndex = block->bbClassSchemaIndex;
    assert((classSchemaIndex >= 0) && (classSchemaIndex < (int)schema.size()));

    ClassProbeInserter                    insertProbes(schema, profileMemory, &classSchemaIndex, m_instrCount);
    ClassProbeVisitor<ClassProbeInserter> visitor(m_comp, insertProbes);
    for (Statement* stmt : block->Statements())
    {
        visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    }
}

//------------------------------------------------------------------------
// ClassProbeInstrumentor::SuppressProbes: clean up if we're not instrumenting
//
// Notes:
//   Currently we're hijacking the gtCallStubAddr of the call node to hold
//   a pointer to the profile candidate info.
//
//   We must undo this, if not instrumenting.
//
void ClassProbeInstrumentor::SuppressProbes()
{
    unsigned                                 cleanupCount = 0;
    SuppressProbesFunctor                    suppressProbes(cleanupCount);
    ClassProbeVisitor<SuppressProbesFunctor> visitor(m_comp, suppressProbes);

    for (BasicBlock* block = m_comp->fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        if ((block->bbFlags & BBF_HAS_CLASS_PROFILE) == 0)
        {
            continue;
        }

        for (Statement* stmt : block->Statements())
        {
            visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }

    assert(cleanupCount == m_comp->info.compClassProbeCount);
}

//------------------------------------------------------------------------
// fgPrepareToInstrumentMethod: prepare for instrumentation
//
// Notes:
//   Runs before importation, so instrumentation schemes can get a pure
//   look at the flowgraph before any internal blocks are added.
//
// Returns:
//   appropriate phase status
//
PhaseStatus Compiler::fgPrepareToInstrumentMethod()
{
    noway_assert(!compIsForInlining());

    // Choose instrumentation technology.
    //
    // Currently, OSR is incompatible with edge profiling. So if OSR is enabled,
    // always do block profiling.
    //
    // Note this incompatibility only exists for methods that actually have
    // patchpoints, but we won't know that until we import.
    //
    const bool methodMayHavePatchpoints =
        (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0) && (JitConfig.TC_OnStackReplacement() > 0));

    if ((JitConfig.JitEdgeProfiling() > 0) && !methodMayHavePatchpoints)
    {
        fgCountInstrumentor = new (this, CMK_Pgo) EfficientEdgeCountInstrumentor(this);
    }
    else
    {
        if (JitConfig.JitEdgeProfiling() > 0)
        {
            JITDUMP("OSR and edge profiling not yet compatible; using block profiling\n");
        }

        fgCountInstrumentor = new (this, CMK_Pgo) BlockCountInstrumentor(this);
    }

    if (JitConfig.JitClassProfiling() > 0)
    {
        fgClassInstrumentor = new (this, CMK_Pgo) ClassProbeInstrumentor(this);
    }
    else
    {
        fgClassInstrumentor = new (this, CMK_Pgo) NonInstrumentor(this);
    }

    // Make pre-import preparations.
    //
    const bool isPreImport = true;
    fgCountInstrumentor->Prepare(isPreImport);
    fgClassInstrumentor->Prepare(isPreImport);

    return PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgInstrumentMethod: add instrumentation probes to the method
//
// Returns:
//   appropriate phase status
//
// Note:
//
//   By default this instruments each non-internal block with
//   a counter probe.
//
//   Optionally adds class probes to virtual and interface calls.
//
//   Probe structure is described by a schema array, which is created
//   here based on flowgraph and IR structure.
//
PhaseStatus Compiler::fgInstrumentMethod()
{
    noway_assert(!compIsForInlining());

    // Make post-importpreparations.
    //
    const bool isPreImport = false;
    fgCountInstrumentor->Prepare(isPreImport);
    fgClassInstrumentor->Prepare(isPreImport);

    // Walk the flow graph to build up the instrumentation schema.
    //
    Schema schema(getAllocator(CMK_Pgo));
    for (BasicBlock* block = fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        if (fgCountInstrumentor->ShouldProcess(block))
        {
            fgCountInstrumentor->BuildSchemaElements(block, schema);
        }

        if (fgClassInstrumentor->ShouldProcess(block))
        {
            fgClassInstrumentor->BuildSchemaElements(block, schema);
        }
    }

    // Verify we created schema for the calls needing class probes.
    // (we counted those when importing)
    //
    assert(fgClassInstrumentor->SchemaCount() == info.compClassProbeCount);

    // Optionally, if there were no class probes and only one count probe,
    // suppress instrumentation.
    //
    if ((JitConfig.JitMinimalProfiling() > 0) && (fgCountInstrumentor->SchemaCount() == 1) &&
        (fgClassInstrumentor->SchemaCount() == 0))
    {
        JITDUMP("Not instrumenting method: only one counter, and no class probes\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    JITDUMP("Instrumenting method: %d count probes and %d class probes\n", fgCountInstrumentor->SchemaCount(),
            fgClassInstrumentor->SchemaCount());

    assert(schema.size() > 0);

    // Allocate the profile buffer
    //
    BYTE* profileMemory;

    HRESULT res = info.compCompHnd->allocPgoInstrumentationBySchema(info.compMethodHnd, schema.data(),
                                                                    (UINT32)schema.size(), &profileMemory);

    JITDUMP("Instrumentation data base address is %p\n", dspPtr(profileMemory));

    // Deal with allocation failures.
    //
    if (!SUCCEEDED(res))
    {
        JITDUMP("Unable to instrument: schema allocation failed: 0x%x\n", res);

        // The E_NOTIMPL status is returned when we are profiling a generic method from a different assembly
        //
        if (res != E_NOTIMPL)
        {
            noway_assert(!"Error: unexpected hresult from allocPgoInstrumentationBySchema");
            return PhaseStatus::MODIFIED_NOTHING;
        }

        // Do any cleanup we might need to do...
        //
        fgCountInstrumentor->SuppressProbes();
        fgClassInstrumentor->SuppressProbes();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Add the instrumentation code
    //
    for (BasicBlock* block = fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        if (fgCountInstrumentor->ShouldProcess(block))
        {
            fgCountInstrumentor->Instrument(block, schema, profileMemory);
        }

        if (fgClassInstrumentor->ShouldProcess(block))
        {
            fgClassInstrumentor->Instrument(block, schema, profileMemory);
        }
    }

    // Verify we instrumented everthing we created schemas for.
    //
    assert(fgCountInstrumentor->InstrCount() == fgCountInstrumentor->SchemaCount());
    assert(fgClassInstrumentor->InstrCount() == fgClassInstrumentor->SchemaCount());

    // Add any special entry instrumentation. This does not
    // use the schema mechanism.
    //
    fgCountInstrumentor->InstrumentMethodEntry(schema, profileMemory);
    fgClassInstrumentor->InstrumentMethodEntry(schema, profileMemory);

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// fgIncorporateProfileData: add block/edge profile data to the flowgraph
//
// Returns:
//   appropriate phase status
//
PhaseStatus Compiler::fgIncorporateProfileData()
{
    // Are we doing profile stress?
    //
    if (fgStressBBProf() > 0)
    {
        JITDUMP("JitStress -- incorporating random profile data\n");
        fgIncorporateBlockCounts();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    // Do we have profile data?
    //
    if (!fgHaveProfileData())
    {
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT))
        {
            JITDUMP("BBOPT set, but no profile data available (hr=%08x)\n", fgPgoQueryResult);
        }
        else
        {
            JITDUMP("BBOPT not set\n");
        }
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Summarize profile data
    //
    JITDUMP("Have profile data: %d schema records (schema at %p, data at %p)\n", fgPgoSchemaCount, dspPtr(fgPgoSchema),
            dspPtr(fgPgoData));

    fgNumProfileRuns      = 0;
    unsigned otherRecords = 0;

    for (UINT32 iSchema = 0; iSchema < fgPgoSchemaCount; iSchema++)
    {
        switch (fgPgoSchema[iSchema].InstrumentationKind)
        {
            case ICorJitInfo::PgoInstrumentationKind::NumRuns:
                fgNumProfileRuns += fgPgoSchema[iSchema].Other;
                break;

            case ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount:
                fgPgoBlockCounts++;
                break;

            case ICorJitInfo::PgoInstrumentationKind::EdgeIntCount:
                fgPgoEdgeCounts++;
                break;

            case ICorJitInfo::PgoInstrumentationKind::TypeHandleHistogramCount:
                fgPgoClassProfiles++;
                break;

            default:
                otherRecords++;
                break;
        }
    }

    if (fgNumProfileRuns == 0)
    {
        fgNumProfileRuns = 1;
    }

    JITDUMP("Profile summary: %d runs, %d block probes, %d edge probes, %d class profiles, %d other records\n",
            fgNumProfileRuns, fgPgoBlockCounts, fgPgoEdgeCounts, fgPgoClassProfiles, otherRecords);

    const bool haveBlockCounts = fgPgoBlockCounts > 0;
    const bool haveEdgeCounts  = fgPgoEdgeCounts > 0;

    // We expect one or the other but not both.
    //
    assert(haveBlockCounts != haveEdgeCounts);

    if (haveBlockCounts)
    {
        fgIncorporateBlockCounts();
    }
    else if (haveEdgeCounts)
    {
        fgIncorporateEdgeCounts();
    }

    // Now that we have profile data, compute the profile scale for inlinees,
    // if we haven'd done so already.
    //
    if (compIsForInlining())
    {
        fgComputeProfileScale();
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// fgSetProfileWeight: set profile weight for a block
//
// Arguments:
//   block -- block in question
//   profileWeight -- raw profile weight (not accounting for inlining)
//
// Notes:
//   Does inlinee scaling.
//   Handles handler entry special case.
//
void Compiler::fgSetProfileWeight(BasicBlock* block, BasicBlock::weight_t profileWeight)
{
    // Scale count appropriately for inlinees.
    //
    if (compIsForInlining())
    {
        if (impInlineInfo->profileScaleState == InlineInfo::ProfileScaleState::KNOWN)
        {
            double scaledWeight = impInlineInfo->profileScaleFactor * profileWeight;
            profileWeight       = (BasicBlock::weight_t)scaledWeight;
        }
    }

    block->setBBProfileWeight(profileWeight);

    if (profileWeight == BB_ZERO_WEIGHT)
    {
        block->bbSetRunRarely();
    }
    else
    {
        block->bbFlags &= ~BBF_RUN_RARELY;
    }

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
    // Handle a special case -- some handler entries can't have zero profile count.
    //
    if (this->bbIsHandlerBeg(block) && block->isRunRarely())
    {
        JITDUMP("Suppressing zero count for " FMT_BB " as it is a handler entry\n", block->bbNum);
        block->makeBlockHot();
    }
#endif
}

//------------------------------------------------------------------------
// fgIncorporateBlockCounts: read block count based profile data
//   and set block weights
//
// Notes:
//   Count data for inlinees is scaled (usually down).
//
//   Since we are now running before the importer, we do not know which
//   blocks will be imported, and we should not see any internal blocks.
//
// Todo:
//   Normalize counts.
//
//   Take advantage of the (likely) correspondence between block order
//   and schema order?
//
//   Find some other mechanism for handling cases where handler entry
//   blocks must be in the hot section.
//
void Compiler::fgIncorporateBlockCounts()
{
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        BasicBlock::weight_t profileWeight;

        if (fgGetProfileWeightForBasicBlock(block->bbCodeOffs, &profileWeight))
        {
            fgSetProfileWeight(block, profileWeight);
        }
    }
}

//------------------------------------------------------------------------
// EfficientEdgeCountReconstructor: reconstruct block counts from sparse
//   edge counts.
//
// Notes:
//    The algorithm is conceptually simple, but requires a bit of bookkeeping.
//
//    First, we should have a correspondence between the edge count schema
//    entries and the non-tree edges of the spanning tree.
//
//    The instrumentation schema may be partial, if any importer folding was
//    done. Say for instance we have a method that is ISA sensitive to x64 and
//    arm64, and we instrument on x64 and are now jitting on arm64. If so
//    there may be missing schema entries. If we are confident the IL and
//    jit IL to block computations are the same, these missing entries can
//    safely be presumed to be zero.
//
//    Second, we need to be able to reason about the sets of known and
//    unknown edges that are incoming and outgoing from any block. These
//    may not quite be the edges we'd see from iterating successors or
//    building pred lists, because we create special pseudo-edges during
//    instrumentation. So, we also need to build up data structures
//    keeping track of those.
//
//    Solving is done in four steps:
//    * Prepare
//      *  walk the blocks setting up per block info, and a map
//         for block schema keys to blocks.
//      * walk the schema to create info for the known edges, and
//         a map from edge schema keys to edges.
//    * Evolve Spanning Tree
//      * for non-tree edges, presume any missing edge is zero
//        (and hence, can be ignored during the solving process
//      * for tree edges, verify there is no schema entry, and
//        add in an unknown count edge.
//    * Solve
//      * repeatedly walk blocks, looking for blocks where all
//        incoming or outgoing edges are known. This determines
//        the block counts.
//      * for blocks with known counts, look for cases where just
//        one incoming or outgoing edge is unknown, and solve for
//        them.
//    * Propagate
//      * update block counts. bail if there were errors.
//        * mark rare blocks, and special case handler entries
//        * (eventually) try "fixing" counts
//      * (eventually) set edge likelihoods
//      * (eventually) normalize
//
//   If we've done everything right, the solving is guaranteed to
//   converge.
//
//   Along the way we may find edges with negative counts; this
//   is an indication that the count data is not self-consistent.
//
class EfficientEdgeCountReconstructor : public SpanningTreeVisitor
{
private:
    Compiler*     m_comp;
    CompAllocator m_allocator;
    unsigned      m_blocks;
    unsigned      m_edges;
    unsigned      m_unknownBlocks;
    unsigned      m_unknownEdges;
    unsigned      m_zeroEdges;

    // Map a block into its schema key.
    //
    static int32_t BlockToKey(BasicBlock* block)
    {
        int32_t key = (int32_t)block->bbCodeOffs;
        if ((block->bbFlags & BBF_INTERNAL) == BBF_INTERNAL)
        {
            key = block->bbNum | IL_OFFSETX_CALLINSTRUCTIONBIT;
        }

        return key;
    }

    // Map correlating block keys to blocks.
    //
    typedef JitHashTable<int32_t, JitSmallPrimitiveKeyFuncs<int32_t>, BasicBlock*> KeyToBlockMap;
    KeyToBlockMap m_keyToBlockMap;

    // Key for finding an edge based on schema info.
    //
    struct EdgeKey
    {
        int32_t const m_sourceKey;
        int32_t const m_targetKey;

        EdgeKey(int32_t sourceKey, int32_t targetKey) : m_sourceKey(sourceKey), m_targetKey(targetKey)
        {
        }

        EdgeKey(BasicBlock* sourceBlock, BasicBlock* targetBlock)
            : m_sourceKey(BlockToKey(sourceBlock)), m_targetKey(BlockToKey(targetBlock))
        {
        }

        static bool Equals(const EdgeKey& e1, const EdgeKey& e2)
        {
            return (e1.m_sourceKey == e2.m_sourceKey) && (e1.m_targetKey == e2.m_targetKey);
        }

        static unsigned GetHashCode(const EdgeKey& e)
        {
            return (unsigned)(e.m_sourceKey ^ (e.m_targetKey << 16));
        }
    };

    // Per edge info
    //
    struct Edge
    {
        BasicBlock::weight_t m_weight;
        BasicBlock*          m_sourceBlock;
        BasicBlock*          m_targetBlock;
        Edge*                m_nextOutgoingEdge;
        Edge*                m_nextIncomingEdge;
        bool                 m_weightKnown;

        Edge(BasicBlock* source, BasicBlock* target)
            : m_weight(BB_ZERO_WEIGHT)
            , m_sourceBlock(source)
            , m_targetBlock(target)
            , m_nextOutgoingEdge(nullptr)
            , m_nextIncomingEdge(nullptr)
            , m_weightKnown(false)
        {
        }
    };

    // Map for correlating EdgeIntCount schema entries with edges
    //
    typedef JitHashTable<EdgeKey, EdgeKey, Edge*> EdgeKeyToEdgeMap;
    EdgeKeyToEdgeMap m_edgeKeyToEdgeMap;

    // Per block data
    //
    struct BlockInfo
    {
        BasicBlock::weight_t m_weight;
        Edge*                m_incomingEdges;
        Edge*                m_outgoingEdges;
        int                  m_incomingUnknown;
        int                  m_outgoingUnknown;
        bool                 m_weightKnown;

        BlockInfo()
            : m_weight(BB_ZERO_WEIGHT)
            , m_incomingEdges(nullptr)
            , m_outgoingEdges(nullptr)
            , m_incomingUnknown(0)
            , m_outgoingUnknown(0)
            , m_weightKnown(false)
        {
        }
    };

    // Map a block to its info
    //
    BlockInfo* BlockToInfo(BasicBlock* block)
    {
        assert(block->bbSparseCountInfo != nullptr);
        return (BlockInfo*)block->bbSparseCountInfo;
    }

    // Set up block info for a block.
    //
    void SetBlockInfo(BasicBlock* block, BlockInfo* info)
    {
        assert(block->bbSparseCountInfo == nullptr);
        block->bbSparseCountInfo = info;
    }

    // Flags for noting and handling various error cases.
    //
    bool m_badcode;
    bool m_mismatch;
    bool m_negativeCount;
    bool m_failedToConverge;

public:
    EfficientEdgeCountReconstructor(Compiler* comp)
        : SpanningTreeVisitor()
        , m_comp(comp)
        , m_allocator(comp->getAllocator(CMK_Pgo))
        , m_blocks(0)
        , m_edges(0)
        , m_unknownBlocks(0)
        , m_unknownEdges(0)
        , m_zeroEdges(0)
        , m_keyToBlockMap(m_allocator)
        , m_edgeKeyToEdgeMap(m_allocator)
        , m_badcode(false)
        , m_mismatch(false)
        , m_negativeCount(false)
        , m_failedToConverge(false)
    {
    }

    void Prepare();
    void Solve();
    void Propagate();

    void Badcode() override
    {
        m_badcode = true;
    }

    void NegativeCount()
    {
        m_negativeCount = true;
    }

    void Mismatch()
    {
        m_mismatch = true;
    }

    void FailedToConverge()
    {
        m_failedToConverge = true;
    }

    void VisitBlock(BasicBlock*) override
    {
    }

    void VisitTreeEdge(BasicBlock* source, BasicBlock* target) override
    {
        // Tree edges should not be in the schema.
        //
        // If they are, we have somekind of mismatch between instrumentation and
        // reconstruction. Flag this.
        //
        EdgeKey key(source, target);

        if (m_edgeKeyToEdgeMap.Lookup(key))
        {
            JITDUMP("Did not expect tree edge " FMT_BB " -> " FMT_BB " to be present in the schema (key %08x, %08x)\n",
                    source->bbNum, target->bbNum, key.m_sourceKey, key.m_targetKey);

            Mismatch();
            return;
        }

        Edge* const edge = new (m_allocator) Edge(source, target);
        m_edges++;
        m_unknownEdges++;

        BlockInfo* const sourceInfo = BlockToInfo(source);
        edge->m_nextOutgoingEdge    = sourceInfo->m_outgoingEdges;
        sourceInfo->m_outgoingEdges = edge;
        sourceInfo->m_outgoingUnknown++;

        BlockInfo* const targetInfo = BlockToInfo(target);
        edge->m_nextIncomingEdge    = targetInfo->m_incomingEdges;
        targetInfo->m_incomingEdges = edge;
        targetInfo->m_incomingUnknown++;

        JITDUMP(" ... unknown edge " FMT_BB " -> " FMT_BB "\n", source->bbNum, target->bbNum);
    }

    void VisitNonTreeEdge(BasicBlock* source, BasicBlock* target, SpanningTreeVisitor::EdgeKind kind) override
    {
        // We may have this edge in the schema, and so already added this edge to the map.
        //
        // If not, assume we have a partial schema. We could add a zero count edge,
        // but such edges don't impact the solving algorithm, so we can omit them.
        //
        EdgeKey key(source, target);
        Edge*   edge = nullptr;

        if (m_edgeKeyToEdgeMap.Lookup(key, &edge))
        {
            BlockInfo* const sourceInfo = BlockToInfo(source);
            edge->m_nextOutgoingEdge    = sourceInfo->m_outgoingEdges;
            sourceInfo->m_outgoingEdges = edge;

            BlockInfo* const targetInfo = BlockToInfo(target);
            edge->m_nextIncomingEdge    = targetInfo->m_incomingEdges;
            targetInfo->m_incomingEdges = edge;
        }
        else
        {
            // Because the count is zero, we can just pretend this edge doesn't exist.
            //
            JITDUMP("Schema is missing non-tree edge " FMT_BB " -> " FMT_BB ", will presume zero\n", source->bbNum,
                    target->bbNum);
            m_zeroEdges++;
        }
    }
};

//------------------------------------------------------------------------
// EfficientEdgeCountReconstructor::Prepare: set up mapping information and
//    prepare for spanning tree walk and solver
//
void EfficientEdgeCountReconstructor::Prepare()
{
    // Create per-block info, and set up the key to block map.
    //
    for (BasicBlock* block = m_comp->fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        m_keyToBlockMap.Set(BlockToKey(block), block);
        BlockInfo* const info = new (m_allocator) BlockInfo();
        SetBlockInfo(block, info);

        // No block counts are known, initially.
        //
        m_blocks++;
        m_unknownBlocks++;
    }

    // Create edges for schema entries with edge counts, and set them up in
    // the edge key to edge map.
    //
    for (UINT32 iSchema = 0; iSchema < m_comp->fgPgoSchemaCount; iSchema++)
    {
        const ICorJitInfo::PgoInstrumentationSchema& schemaEntry = m_comp->fgPgoSchema[iSchema];
        switch (schemaEntry.InstrumentationKind)
        {
            case ICorJitInfo::PgoInstrumentationKind::EdgeIntCount:
            {
                // Optimization TODO: if profileCount is zero, we can just ignore this edge
                // and the right things will happen.
                //
                uint32_t const profileCount = *(uint32_t*)(m_comp->fgPgoData + schemaEntry.Offset);
                {
                    BasicBlock::weight_t const weight = (BasicBlock::weight_t)profileCount;

                    // Find the blocks.
                    //
                    BasicBlock* sourceBlock = nullptr;

                    if (!m_keyToBlockMap.Lookup(schemaEntry.ILOffset, &sourceBlock))
                    {
                        JITDUMP("Could not find source block for schema entry %d (IL offset/key %08x\n", iSchema,
                                schemaEntry.ILOffset);
                    }

                    BasicBlock* targetBlock = nullptr;

                    if (!m_keyToBlockMap.Lookup(schemaEntry.Other, &targetBlock))
                    {
                        JITDUMP("Could not find target block for schema entry %d (IL offset/key %08x\n", iSchema,
                                schemaEntry.ILOffset);
                    }

                    if ((sourceBlock == nullptr) || (targetBlock == nullptr))
                    {
                        // Looks like there is skew between schema and graph.
                        //
                        Mismatch();
                        continue;
                    }

                    Edge* const edge = new (m_allocator) Edge(sourceBlock, targetBlock);

                    JITDUMP("... adding known edge " FMT_BB " -> " FMT_BB ": weight %0f\n", edge->m_sourceBlock->bbNum,
                            edge->m_targetBlock->bbNum, weight);

                    edge->m_weightKnown = true;
                    edge->m_weight      = weight;

                    EdgeKey edgeKey(schemaEntry.ILOffset, schemaEntry.Other);
                    m_edgeKeyToEdgeMap.Set(edgeKey, edge);

                    m_edges++;
                }
            }
            break;

            default:
                break;
        }
    }
}

//------------------------------------------------------------------------
// EfficientEdgeCountReconstructor::Solve: solve for missing edge and block counts
//
void EfficientEdgeCountReconstructor::Solve()
{
    // If issues arose earlier, then don't try solving.
    //
    if (m_badcode || m_mismatch)
    {
        JITDUMP("... not solving because of the %s\n", m_badcode ? "badcode" : "mismatch")
        return;
    }

    unsigned       nPasses = 0;
    unsigned const nLimit  = 10;

    JITDUMP("\nSolver: %u blocks, %u unknown; %u edges, %u unknown, %u zero (and so ignored)\n", m_blocks,
            m_unknownBlocks, m_edges, m_unknownEdges, m_zeroEdges);

    while ((m_unknownBlocks > 0) && (nPasses < nLimit))
    {
        nPasses++;
        JITDUMP("\nPass [%u]: %u unknown blocks, %u unknown edges\n", nPasses, m_unknownBlocks, m_unknownEdges);

        // TODO: no point walking all the blocks here, we should find a way to just walk
        // the subset with unknown counts or edges.
        //
        // The ideal solver order is likely reverse postorder over the depth-first spanning tree.
        // We approximate it here by running from last node to first.
        //
        for (BasicBlock* block = m_comp->fgLastBB; (block != nullptr); block = block->bbPrev)
        {
            BlockInfo* const info = BlockToInfo(block);

            // Try and determine block weight.
            //
            if (!info->m_weightKnown)
            {
                JITDUMP(FMT_BB ": %u incoming unknown, %u outgoing unknown\n", block->bbNum, info->m_incomingUnknown,
                        info->m_outgoingUnknown);

                BasicBlock::weight_t weight      = BB_ZERO_WEIGHT;
                bool                 weightKnown = false;
                if (info->m_incomingUnknown == 0)
                {
                    JITDUMP(FMT_BB ": all incoming edge weights known, summming...\n", block->bbNum);
                    for (Edge* edge = info->m_incomingEdges; edge != nullptr; edge = edge->m_nextIncomingEdge)
                    {
                        if (!edge->m_weightKnown)
                        {
                            JITDUMP("... odd, expected " FMT_BB " -> " FMT_BB " to have known weight\n",
                                    edge->m_sourceBlock->bbNum, edge->m_targetBlock->bbNum);
                        }
                        assert(edge->m_weightKnown);
                        JITDUMP("  " FMT_BB " -> " FMT_BB " has weight %0f\n", edge->m_sourceBlock->bbNum,
                                edge->m_targetBlock->bbNum, edge->m_weight);
                        weight += edge->m_weight;
                    }
                    JITDUMP(FMT_BB ": all incoming edge weights known, sum is %0f\n", block->bbNum, weight);
                    weightKnown = true;
                }
                else if (info->m_outgoingUnknown == 0)
                {
                    JITDUMP(FMT_BB ": all outgoing edge weights known, summming...\n", block->bbNum);
                    for (Edge* edge = info->m_outgoingEdges; edge != nullptr; edge = edge->m_nextOutgoingEdge)
                    {
                        if (!edge->m_weightKnown)
                        {
                            JITDUMP("... odd, expected " FMT_BB " -> " FMT_BB " to have known weight\n",
                                    edge->m_sourceBlock->bbNum, edge->m_targetBlock->bbNum);
                        }
                        assert(edge->m_weightKnown);
                        JITDUMP("  " FMT_BB " -> " FMT_BB " has weight %0f\n", edge->m_sourceBlock->bbNum,
                                edge->m_targetBlock->bbNum, edge->m_weight);
                        weight += edge->m_weight;
                    }
                    JITDUMP(FMT_BB ": all outgoing edge weights known, sum is %0f\n", block->bbNum, weight);
                    weightKnown = true;
                }

                if (weightKnown)
                {
                    info->m_weight      = weight;
                    info->m_weightKnown = true;
                    assert(m_unknownBlocks > 0);
                    m_unknownBlocks--;
                }
            }

            // If we still don't know the block weight, move on to the next block.
            //
            if (!info->m_weightKnown)
            {
                continue;
            }

            // If we know the block weight, see if we can resolve any edge weights.
            //
            if (info->m_incomingUnknown == 1)
            {
                BasicBlock::weight_t weight       = BB_ZERO_WEIGHT;
                Edge*                resolvedEdge = nullptr;
                for (Edge* edge = info->m_incomingEdges; edge != nullptr; edge = edge->m_nextIncomingEdge)
                {
                    if (edge->m_weightKnown)
                    {
                        weight += edge->m_weight;
                    }
                    else
                    {
                        assert(resolvedEdge == nullptr);
                        resolvedEdge = edge;
                    }
                }

                assert(resolvedEdge != nullptr);

                weight = info->m_weight - weight;

                JITDUMP(FMT_BB " -> " FMT_BB
                               ": target block weight and all other incoming edge weights known, so weight is %0f\n",
                        resolvedEdge->m_sourceBlock->bbNum, resolvedEdge->m_targetBlock->bbNum, weight);

                // If we arrive at a negative count for this edge, set it to zero.
                //
                if (weight < 0)
                {
                    JITDUMP(" .... weight was negative, setting to zero\n");
                    NegativeCount();
                    weight = 0;
                }

                resolvedEdge->m_weight      = weight;
                resolvedEdge->m_weightKnown = true;

                // Update source and target info.
                //
                assert(BlockToInfo(resolvedEdge->m_sourceBlock)->m_outgoingUnknown > 0);
                BlockToInfo(resolvedEdge->m_sourceBlock)->m_outgoingUnknown--;
                info->m_incomingUnknown--;
                assert(m_unknownEdges > 0);
                m_unknownEdges--;
            }

            if (info->m_outgoingUnknown == 1)
            {
                BasicBlock::weight_t weight       = BB_ZERO_WEIGHT;
                Edge*                resolvedEdge = nullptr;
                for (Edge* edge = info->m_outgoingEdges; edge != nullptr; edge = edge->m_nextOutgoingEdge)
                {
                    if (edge->m_weightKnown)
                    {
                        weight += edge->m_weight;
                    }
                    else
                    {
                        assert(resolvedEdge == nullptr);
                        resolvedEdge = edge;
                    }
                }

                assert(resolvedEdge != nullptr);

                weight = info->m_weight - weight;

                JITDUMP(FMT_BB " -> " FMT_BB
                               ": source block weight and all other outgoing edge weights known, so weight is %0f\n",
                        resolvedEdge->m_sourceBlock->bbNum, resolvedEdge->m_targetBlock->bbNum, weight);

                // If we arrive at a negative count for this edge, set it to zero.
                //
                if (weight < 0)
                {
                    JITDUMP(" .... weight was negative, setting to zero\n");
                    NegativeCount();
                    weight = 0;
                }

                resolvedEdge->m_weight      = weight;
                resolvedEdge->m_weightKnown = true;

                // Update source and target info.
                //
                info->m_outgoingUnknown--;
                assert(BlockToInfo(resolvedEdge->m_targetBlock)->m_incomingUnknown > 0);
                BlockToInfo(resolvedEdge->m_targetBlock)->m_incomingUnknown--;
                assert(m_unknownEdges > 0);
                m_unknownEdges--;
            }
        }
    }

    if (m_unknownBlocks == 0)
    {
        JITDUMP("\nSolver: converged in %u passes\n", nPasses);
    }
    else
    {
        JITDUMP("\nSolver: failed to converge in %u passes, %u blocks and %u edges remain unsolved\n", nPasses,
                m_unknownBlocks, m_unknownEdges);
        FailedToConverge();
    }
}

//------------------------------------------------------------------------
// EfficientEdgeCountReconstructor::Propagate: actually set block weights.
//
// Notes:
//    For inlinees, weights are scaled appropriately.
//
void EfficientEdgeCountReconstructor::Propagate()
{
    // We don't expect mismatches or convergence failures.
    //
    assert(!m_mismatch);
    assert(!m_failedToConverge);

    // If any issues arose during reconstruction, don't set weights.
    //
    if (m_badcode || m_mismatch || m_failedToConverge)
    {
        JITDUMP("... discarding profile data because of %s\n",
                m_badcode ? "badcode" : m_mismatch ? "mismatch" : "failed to converge");

        // Make sure nothing else in the jit looks at the profile data.
        //
        m_comp->fgPgoSchema = nullptr;

        return;
    }

    if (m_comp->compIsForInlining())
    {
        // Tentatively set first block's profile to compute inlinee profile scale.
        //
        BlockInfo* const info = BlockToInfo(m_comp->fgFirstBB);
        assert(info->m_weightKnown);

        m_comp->fgSetProfileWeight(m_comp->fgFirstBB, info->m_weight);
        m_comp->fgComputeProfileScale();
    }

    // Set weight on all blocks (will reset entry weight for inlinees based
    // on above computed scale).
    //
    for (BasicBlock* block = m_comp->fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        BlockInfo* const info = BlockToInfo(block);
        assert(info->m_weightKnown);

        m_comp->fgSetProfileWeight(block, info->m_weight);
    }
}

//------------------------------------------------------------------------
// fgIncorporateEdgeCounts: read sparse edge count based profile data
//   and set block weights
//
// Notes:
//   Because edge counts are sparse, we need to solve for the missing
//   edge counts; in the process, we also determine block counts.
//
// Todo:
//   Normalize counts.
//   Since we have edge weights here, we might as well set them
//   (or likelihoods)
//
void Compiler::fgIncorporateEdgeCounts()
{
    JITDUMP("\nReconstructing block counts from sparse edge instrumentation\n");

    EfficientEdgeCountReconstructor e(this);
    e.Prepare();
    WalkSpanningTree(&e);
    e.Solve();
    e.Propagate();
}

bool flowList::setEdgeWeightMinChecked(BasicBlock::weight_t newWeight, BasicBlock::weight_t slop, bool* wbUsedSlop)
{
    bool result = false;
    if ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin))
    {
        flEdgeWeightMin = newWeight;
        result          = true;
    }
    else if (slop > 0)
    {
        // We allow for a small amount of inaccuracy in block weight counts.
        if (flEdgeWeightMax < newWeight)
        {
            // We have already determined that this edge's weight
            // is less than newWeight, so we just allow for the slop
            if (newWeight <= (flEdgeWeightMax + slop))
            {
                result = true;

                if (flEdgeWeightMax != 0)
                {
                    // We will raise flEdgeWeightMin and Max towards newWeight
                    flEdgeWeightMin = flEdgeWeightMax;
                    flEdgeWeightMax = newWeight;
                }

                if (wbUsedSlop != nullptr)
                {
                    *wbUsedSlop = true;
                }
            }
        }
        else
        {
            assert(flEdgeWeightMin > newWeight);

            // We have already determined that this edge's weight
            // is more than newWeight, so we just allow for the slop
            if ((newWeight + slop) >= flEdgeWeightMin)
            {
                result = true;

                assert(flEdgeWeightMax != 0);

                // We will lower flEdgeWeightMin towards newWeight
                flEdgeWeightMin = newWeight;

                if (wbUsedSlop != nullptr)
                {
                    *wbUsedSlop = true;
                }
            }
        }

        // If we are returning true then we should have adjusted the range so that
        // the newWeight is in new range [Min..Max] or fgEdjeWeightMax is zero.
        // Also we should have set wbUsedSlop to true.
        if (result == true)
        {
            assert((flEdgeWeightMax == 0) || ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin)));

            if (wbUsedSlop != nullptr)
            {
                assert(*wbUsedSlop == true);
            }
        }
    }

#if DEBUG
    if (result == false)
    {
        result = false; // break here
    }
#endif // DEBUG

    return result;
}

bool flowList::setEdgeWeightMaxChecked(BasicBlock::weight_t newWeight, BasicBlock::weight_t slop, bool* wbUsedSlop)
{
    bool result = false;
    if ((newWeight >= flEdgeWeightMin) && (newWeight <= flEdgeWeightMax))
    {
        flEdgeWeightMax = newWeight;
        result          = true;
    }
    else if (slop > 0)
    {
        // We allow for a small amount of inaccuracy in block weight counts.
        if (flEdgeWeightMax < newWeight)
        {
            // We have already determined that this edge's weight
            // is less than newWeight, so we just allow for the slop
            if (newWeight <= (flEdgeWeightMax + slop))
            {
                result = true;

                if (flEdgeWeightMax != 0)
                {
                    // We will allow this to raise flEdgeWeightMax towards newWeight
                    flEdgeWeightMax = newWeight;
                }

                if (wbUsedSlop != nullptr)
                {
                    *wbUsedSlop = true;
                }
            }
        }
        else
        {
            assert(flEdgeWeightMin > newWeight);

            // We have already determined that this edge's weight
            // is more than newWeight, so we just allow for the slop
            if ((newWeight + slop) >= flEdgeWeightMin)
            {
                result = true;

                assert(flEdgeWeightMax != 0);

                // We will allow this to lower flEdgeWeightMin and Max towards newWeight
                flEdgeWeightMax = flEdgeWeightMin;
                flEdgeWeightMin = newWeight;

                if (wbUsedSlop != nullptr)
                {
                    *wbUsedSlop = true;
                }
            }
        }

        // If we are returning true then we should have adjusted the range so that
        // the newWeight is in new range [Min..Max] or fgEdjeWeightMax is zero
        // Also we should have set wbUsedSlop to true, unless it is NULL
        if (result == true)
        {
            assert((flEdgeWeightMax == 0) || ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin)));

            assert((wbUsedSlop == nullptr) || (*wbUsedSlop == true));
        }
    }

#if DEBUG
    if (result == false)
    {
        result = false; // break here
    }
#endif // DEBUG

    return result;
}

//------------------------------------------------------------------------
// setEdgeWeights: Sets the minimum lower (flEdgeWeightMin) value
//                  and the maximum upper (flEdgeWeightMax) value
//                 Asserts that the max value is greater or equal to the min value
//
// Arguments:
//    theMinWeight - the new minimum lower (flEdgeWeightMin)
//    theMaxWeight - the new maximum upper (flEdgeWeightMin)
//
void flowList::setEdgeWeights(BasicBlock::weight_t theMinWeight, BasicBlock::weight_t theMaxWeight)
{
    assert(theMinWeight <= theMaxWeight);

    flEdgeWeightMin = theMinWeight;
    flEdgeWeightMax = theMaxWeight;
}

//-------------------------------------------------------------
// fgComputeBlockAndEdgeWeights: determine weights for blocks
//   and optionally for edges
//
void Compiler::fgComputeBlockAndEdgeWeights()
{
    JITDUMP("*************** In fgComputeBlockAndEdgeWeights()\n");

    const bool usingProfileWeights = fgIsUsingProfileWeights();

    fgModified             = false;
    fgHaveValidEdgeWeights = false;
    fgCalledCount          = BB_UNITY_WEIGHT;

#if DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    const BasicBlock::weight_t returnWeight = fgComputeMissingBlockWeights();

    if (usingProfileWeights)
    {
        fgComputeCalledCount(returnWeight);
    }
    else
    {
        JITDUMP(" -- no profile data, so using default called count\n");
    }

    fgComputeEdgeWeights();
}

//-------------------------------------------------------------
// fgComputeMissingBlockWeights: determine weights for blocks
//   that were not profiled and do not yet have weights.
//
// Returns:
//   sum of weights for all return and throw blocks in the method

BasicBlock::weight_t Compiler::fgComputeMissingBlockWeights()
{
    BasicBlock*          bSrc;
    BasicBlock*          bDst;
    unsigned             iterations = 0;
    bool                 changed;
    bool                 modified = false;
    BasicBlock::weight_t returnWeight;

    // If we have any blocks that did not have profile derived weight
    // we will try to fix their weight up here
    //
    modified = false;
    do // while (changed)
    {
        changed      = false;
        returnWeight = 0;
        iterations++;

        for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
        {
            if (!bDst->hasProfileWeight() && (bDst->bbPreds != nullptr))
            {
                BasicBlock* bOnlyNext;

                // This block does not have a profile derived weight
                //
                BasicBlock::weight_t newWeight = BB_MAX_WEIGHT;

                if (bDst->countOfInEdges() == 1)
                {
                    // Only one block flows into bDst
                    bSrc = bDst->bbPreds->getBlock();

                    // Does this block flow into only one other block
                    if (bSrc->bbJumpKind == BBJ_NONE)
                    {
                        bOnlyNext = bSrc->bbNext;
                    }
                    else if (bSrc->bbJumpKind == BBJ_ALWAYS)
                    {
                        bOnlyNext = bSrc->bbJumpDest;
                    }
                    else
                    {
                        bOnlyNext = nullptr;
                    }

                    if ((bOnlyNext == bDst) && bSrc->hasProfileWeight())
                    {
                        // We know the exact weight of bDst
                        newWeight = bSrc->bbWeight;
                    }
                }

                // Does this block flow into only one other block
                if (bDst->bbJumpKind == BBJ_NONE)
                {
                    bOnlyNext = bDst->bbNext;
                }
                else if (bDst->bbJumpKind == BBJ_ALWAYS)
                {
                    bOnlyNext = bDst->bbJumpDest;
                }
                else
                {
                    bOnlyNext = nullptr;
                }

                if ((bOnlyNext != nullptr) && (bOnlyNext->bbPreds != nullptr))
                {
                    // Does only one block flow into bOnlyNext
                    if (bOnlyNext->countOfInEdges() == 1)
                    {
                        noway_assert(bOnlyNext->bbPreds->getBlock() == bDst);

                        // We know the exact weight of bDst
                        newWeight = bOnlyNext->bbWeight;
                    }
                }

                if ((newWeight != BB_MAX_WEIGHT) && (bDst->bbWeight != newWeight))
                {
                    changed        = true;
                    modified       = true;
                    bDst->bbWeight = newWeight;
                    if (newWeight == 0)
                    {
                        bDst->bbFlags |= BBF_RUN_RARELY;
                    }
                    else
                    {
                        bDst->bbFlags &= ~BBF_RUN_RARELY;
                    }
                }
            }

            // Sum up the weights of all of the return blocks and throw blocks
            // This is used when we have a back-edge into block 1
            //
            if (bDst->hasProfileWeight() && ((bDst->bbJumpKind == BBJ_RETURN) || (bDst->bbJumpKind == BBJ_THROW)))
            {
                returnWeight += bDst->bbWeight;
            }
        }
    }
    // Generally when we synthesize profile estimates we do it in a way where this algorithm will converge
    // but downstream opts that remove conditional branches may create a situation where this is not the case.
    // For instance a loop that becomes unreachable creates a sort of 'ring oscillator' (See test b539509)
    while (changed && iterations < 10);

#if DEBUG
    if (verbose && modified)
    {
        printf("fgComputeMissingBlockWeights() adjusted the weight of some blocks\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif

    return returnWeight;
}

//-------------------------------------------------------------
// fgComputeCalledCount: when profile information is in use,
//   compute fgCalledCount
//
// Argument:
//   returnWeight - sum of weights for all return and throw blocks

void Compiler::fgComputeCalledCount(BasicBlock::weight_t returnWeight)
{
    // When we are not using profile data we have already setup fgCalledCount
    // only set it here if we are using profile data
    assert(fgIsUsingProfileWeights());

    BasicBlock* firstILBlock = fgFirstBB; // The first block for IL code (i.e. for the IL code at offset 0)

    // Do we have an internal block as our first Block?
    if (firstILBlock->bbFlags & BBF_INTERNAL)
    {
        // Skip past any/all BBF_INTERNAL blocks that may have been added before the first real IL block.
        //
        while (firstILBlock->bbFlags & BBF_INTERNAL)
        {
            firstILBlock = firstILBlock->bbNext;
        }
        // The 'firstILBlock' is now expected to have a profile-derived weight
        assert(firstILBlock->hasProfileWeight());
    }

    // If the first block only has one ref then we use it's weight for fgCalledCount.
    // Otherwise we have backedge's into the first block, so instead we use the sum
    // of the return block weights for fgCalledCount.
    //
    // If the profile data has a 0 for the returnWeight
    // (i.e. the function never returns because it always throws)
    // then just use the first block weight rather than 0.
    //
    if ((firstILBlock->countOfInEdges() == 1) || (returnWeight == 0))
    {
        assert(firstILBlock->hasProfileWeight()); // This should always be a profile-derived weight
        fgCalledCount = firstILBlock->bbWeight;
    }
    else
    {
        fgCalledCount = returnWeight;
    }

    // If we allocated a scratch block as the first BB then we need
    // to set its profile-derived weight to be fgCalledCount
    if (fgFirstBBisScratch())
    {
        fgFirstBB->setBBProfileWeight(fgCalledCount);
        if (fgFirstBB->bbWeight == 0)
        {
            fgFirstBB->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            fgFirstBB->bbFlags &= ~BBF_RUN_RARELY;
        }
    }

#if DEBUG
    if (verbose)
    {
        printf("We are using the Profile Weights and fgCalledCount is %.0f.\n", fgCalledCount);
    }
#endif
}

//-------------------------------------------------------------
// fgComputeEdgeWeights: compute edge weights from block weights

void Compiler::fgComputeEdgeWeights()
{
    const bool isOptimizing        = opts.OptimizationEnabled();
    const bool usingProfileWeights = fgIsUsingProfileWeights();

    if (!isOptimizing || !usingProfileWeights)
    {
        JITDUMP(" -- not optimizing or no profile data, so not computing edge weights\n");
        return;
    }

    BasicBlock*          bSrc;
    BasicBlock*          bDst;
    flowList*            edge;
    BasicBlock::weight_t slop;
    unsigned             goodEdgeCountCurrent     = 0;
    unsigned             goodEdgeCountPrevious    = 0;
    bool                 inconsistentProfileData  = false;
    bool                 hasIncompleteEdgeWeights = false;
    bool                 usedSlop                 = false;
    unsigned             numEdges                 = 0;
    unsigned             iterations               = 0;

    // Now we will compute the initial flEdgeWeightMin and flEdgeWeightMax values
    for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
    {
        BasicBlock::weight_t bDstWeight = bDst->bbWeight;

        // We subtract out the called count so that bDstWeight is
        // the sum of all edges that go into this block from this method.
        //
        if (bDst == fgFirstBB)
        {
            bDstWeight -= fgCalledCount;
        }

        for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
        {
            bool assignOK = true;

            bSrc = edge->getBlock();
            // We are processing the control flow edge (bSrc -> bDst)

            numEdges++;

            //
            // If the bSrc or bDst blocks do not have exact profile weights
            // then we must reset any values that they currently have
            //

            if (!bSrc->hasProfileWeight() || !bDst->hasProfileWeight())
            {
                edge->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT);
            }

            slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;
            switch (bSrc->bbJumpKind)
            {
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                case BBJ_NONE:
                case BBJ_CALLFINALLY:
                    // We know the exact edge weight
                    assignOK &= edge->setEdgeWeightMinChecked(bSrc->bbWeight, slop, &usedSlop);
                    assignOK &= edge->setEdgeWeightMaxChecked(bSrc->bbWeight, slop, &usedSlop);
                    break;

                case BBJ_COND:
                case BBJ_SWITCH:
                case BBJ_EHFINALLYRET:
                case BBJ_EHFILTERRET:
                    if (edge->edgeWeightMax() > bSrc->bbWeight)
                    {
                        // The maximum edge weight to block can't be greater than the weight of bSrc
                        assignOK &= edge->setEdgeWeightMaxChecked(bSrc->bbWeight, slop, &usedSlop);
                    }
                    break;

                default:
                    // We should never have an edge that starts from one of these jump kinds
                    noway_assert(!"Unexpected bbJumpKind");
                    break;
            }

            // The maximum edge weight to block can't be greater than the weight of bDst
            if (edge->edgeWeightMax() > bDstWeight)
            {
                assignOK &= edge->setEdgeWeightMaxChecked(bDstWeight, slop, &usedSlop);
            }

            if (!assignOK)
            {
                // Here we have inconsistent profile data
                inconsistentProfileData = true;
                // No point in continuing
                goto EARLY_EXIT;
            }
        }
    }

    fgEdgeCount = numEdges;

    iterations = 0;

    do
    {
        iterations++;
        goodEdgeCountPrevious    = goodEdgeCountCurrent;
        goodEdgeCountCurrent     = 0;
        hasIncompleteEdgeWeights = false;

        for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
        {
            for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
            {
                bool assignOK = true;

                // We are processing the control flow edge (bSrc -> bDst)
                bSrc = edge->getBlock();

                slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;
                if (bSrc->bbJumpKind == BBJ_COND)
                {
                    BasicBlock::weight_t diff;
                    flowList*            otherEdge;
                    if (bSrc->bbNext == bDst)
                    {
                        otherEdge = fgGetPredForBlock(bSrc->bbJumpDest, bSrc);
                    }
                    else
                    {
                        otherEdge = fgGetPredForBlock(bSrc->bbNext, bSrc);
                    }
                    noway_assert(edge->edgeWeightMin() <= edge->edgeWeightMax());
                    noway_assert(otherEdge->edgeWeightMin() <= otherEdge->edgeWeightMax());

                    // Adjust edge->flEdgeWeightMin up or adjust otherEdge->flEdgeWeightMax down
                    diff = bSrc->bbWeight - (edge->edgeWeightMin() + otherEdge->edgeWeightMax());
                    if (diff > 0)
                    {
                        assignOK &= edge->setEdgeWeightMinChecked(edge->edgeWeightMin() + diff, slop, &usedSlop);
                    }
                    else if (diff < 0)
                    {
                        assignOK &=
                            otherEdge->setEdgeWeightMaxChecked(otherEdge->edgeWeightMax() + diff, slop, &usedSlop);
                    }

                    // Adjust otherEdge->flEdgeWeightMin up or adjust edge->flEdgeWeightMax down
                    diff = bSrc->bbWeight - (otherEdge->edgeWeightMin() + edge->edgeWeightMax());
                    if (diff > 0)
                    {
                        assignOK &=
                            otherEdge->setEdgeWeightMinChecked(otherEdge->edgeWeightMin() + diff, slop, &usedSlop);
                    }
                    else if (diff < 0)
                    {
                        assignOK &= edge->setEdgeWeightMaxChecked(edge->edgeWeightMax() + diff, slop, &usedSlop);
                    }

                    if (!assignOK)
                    {
                        // Here we have inconsistent profile data
                        inconsistentProfileData = true;
                        // No point in continuing
                        goto EARLY_EXIT;
                    }
#ifdef DEBUG
                    // Now edge->flEdgeWeightMin and otherEdge->flEdgeWeightMax) should add up to bSrc->bbWeight
                    diff = bSrc->bbWeight - (edge->edgeWeightMin() + otherEdge->edgeWeightMax());
                    assert(((-slop) <= diff) && (diff <= slop));

                    // Now otherEdge->flEdgeWeightMin and edge->flEdgeWeightMax) should add up to bSrc->bbWeight
                    diff = bSrc->bbWeight - (otherEdge->edgeWeightMin() + edge->edgeWeightMax());
                    assert(((-slop) <= diff) && (diff <= slop));
#endif // DEBUG
                }
            }
        }

        for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
        {
            BasicBlock::weight_t bDstWeight = bDst->bbWeight;

            if (bDstWeight == BB_MAX_WEIGHT)
            {
                inconsistentProfileData = true;
                // No point in continuing
                goto EARLY_EXIT;
            }
            else
            {
                // We subtract out the called count so that bDstWeight is
                // the sum of all edges that go into this block from this method.
                //
                if (bDst == fgFirstBB)
                {
                    bDstWeight -= fgCalledCount;
                }

                BasicBlock::weight_t minEdgeWeightSum = 0;
                BasicBlock::weight_t maxEdgeWeightSum = 0;

                // Calculate the sums of the minimum and maximum edge weights
                for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
                {
                    // We are processing the control flow edge (bSrc -> bDst)
                    bSrc = edge->getBlock();

                    maxEdgeWeightSum += edge->edgeWeightMax();
                    minEdgeWeightSum += edge->edgeWeightMin();
                }

                // maxEdgeWeightSum is the sum of all flEdgeWeightMax values into bDst
                // minEdgeWeightSum is the sum of all flEdgeWeightMin values into bDst

                for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
                {
                    bool assignOK = true;

                    // We are processing the control flow edge (bSrc -> bDst)
                    bSrc = edge->getBlock();
                    slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;

                    // otherMaxEdgesWeightSum is the sum of all of the other edges flEdgeWeightMax values
                    // This can be used to compute a lower bound for our minimum edge weight
                    noway_assert(maxEdgeWeightSum >= edge->edgeWeightMax());
                    BasicBlock::weight_t otherMaxEdgesWeightSum = maxEdgeWeightSum - edge->edgeWeightMax();

                    // otherMinEdgesWeightSum is the sum of all of the other edges flEdgeWeightMin values
                    // This can be used to compute an upper bound for our maximum edge weight
                    noway_assert(minEdgeWeightSum >= edge->edgeWeightMin());
                    BasicBlock::weight_t otherMinEdgesWeightSum = minEdgeWeightSum - edge->edgeWeightMin();

                    if (bDstWeight >= otherMaxEdgesWeightSum)
                    {
                        // minWeightCalc is our minWeight when every other path to bDst takes it's flEdgeWeightMax value
                        BasicBlock::weight_t minWeightCalc =
                            (BasicBlock::weight_t)(bDstWeight - otherMaxEdgesWeightSum);
                        if (minWeightCalc > edge->edgeWeightMin())
                        {
                            assignOK &= edge->setEdgeWeightMinChecked(minWeightCalc, slop, &usedSlop);
                        }
                    }

                    if (bDstWeight >= otherMinEdgesWeightSum)
                    {
                        // maxWeightCalc is our maxWeight when every other path to bDst takes it's flEdgeWeightMin value
                        BasicBlock::weight_t maxWeightCalc =
                            (BasicBlock::weight_t)(bDstWeight - otherMinEdgesWeightSum);
                        if (maxWeightCalc < edge->edgeWeightMax())
                        {
                            assignOK &= edge->setEdgeWeightMaxChecked(maxWeightCalc, slop, &usedSlop);
                        }
                    }

                    if (!assignOK)
                    {
                        // Here we have inconsistent profile data
                        inconsistentProfileData = true;
                        // No point in continuing
                        goto EARLY_EXIT;
                    }

                    // When flEdgeWeightMin equals flEdgeWeightMax we have a "good" edge weight
                    if (edge->edgeWeightMin() == edge->edgeWeightMax())
                    {
                        // Count how many "good" edge weights we have
                        // Each time through we should have more "good" weights
                        // We exit the while loop when no longer find any new "good" edges
                        goodEdgeCountCurrent++;
                    }
                    else
                    {
                        // Remember that we have seen at least one "Bad" edge weight
                        // so that we will repeat the while loop again
                        hasIncompleteEdgeWeights = true;
                    }
                }
            }
        }

        assert(!inconsistentProfileData); // Should use EARLY_EXIT when it is false.

        if (numEdges == goodEdgeCountCurrent)
        {
            noway_assert(hasIncompleteEdgeWeights == false);
            break;
        }

    } while (hasIncompleteEdgeWeights && (goodEdgeCountCurrent > goodEdgeCountPrevious) && (iterations < 8));

EARLY_EXIT:;

#ifdef DEBUG
    if (verbose)
    {
        if (inconsistentProfileData)
        {
            printf("fgComputeEdgeWeights() found inconsistent profile data, not using the edge weights\n");
        }
        else
        {
            if (hasIncompleteEdgeWeights)
            {
                printf("fgComputeEdgeWeights() was able to compute exact edge weights for %3d of the %3d edges, using "
                       "%d passes.\n",
                       goodEdgeCountCurrent, numEdges, iterations);
            }
            else
            {
                printf("fgComputeEdgeWeights() was able to compute exact edge weights for all of the %3d edges, using "
                       "%d passes.\n",
                       numEdges, iterations);
            }

            fgPrintEdgeWeights();
        }
    }
#endif // DEBUG

    fgSlopUsedInEdgeWeights  = usedSlop;
    fgRangeUsedInEdgeWeights = false;

    // See if any edge weight are expressed in [min..max] form

    for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
    {
        if (bDst->bbPreds != nullptr)
        {
            for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
            {
                bSrc = edge->getBlock();
                // This is the control flow edge (bSrc -> bDst)

                if (edge->edgeWeightMin() != edge->edgeWeightMax())
                {
                    fgRangeUsedInEdgeWeights = true;
                    break;
                }
            }
            if (fgRangeUsedInEdgeWeights)
            {
                break;
            }
        }
    }

    fgHaveValidEdgeWeights = !inconsistentProfileData;
    fgEdgeWeightsComputed  = true;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// fgDebugCheckProfileData: verify profile data is self-consistent
//   (or nearly so)
//
// Notes:
//   For each profiled block, check that the flow of counts into
//   the block matches the flow of counts out of the block.
//
//   We ignore EH flow as we don't have explicit edges and generally
//   we expect EH edge counts to be small, so errors from ignoring
//   them should be rare.
//
void Compiler::fgDebugCheckProfileData()
{
    // We can't check before we have pred lists built.
    //
    assert(fgComputePredsDone);

    JITDUMP("Checking Profile Data\n");
    unsigned             problemBlocks    = 0;
    unsigned             unprofiledBlocks = 0;
    unsigned             profiledBlocks   = 0;
    bool                 entryProfiled    = false;
    bool                 exitProfiled     = false;
    BasicBlock::weight_t entryWeight      = 0;
    BasicBlock::weight_t exitWeight       = 0;

    // Verify each profiled block.
    //
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (!block->hasProfileWeight())
        {
            unprofiledBlocks++;
            continue;
        }

        // There is some profile data to check.
        //
        profiledBlocks++;

        // Currently using raw counts. Consider using normalized counts instead?
        //
        BasicBlock::weight_t blockWeight = block->bbWeight;

        bool verifyIncoming = true;
        bool verifyOutgoing = true;

        // First, look for blocks that require special treatment.

        // Entry blocks
        //
        if (block == fgFirstBB)
        {
            entryWeight += blockWeight;
            entryProfiled  = true;
            verifyIncoming = false;
        }

        // Exit blocks
        //
        if ((block->bbJumpKind == BBJ_RETURN) || (block->bbJumpKind == BBJ_THROW))
        {
            exitWeight += blockWeight;
            exitProfiled   = true;
            verifyOutgoing = false;
        }

        // Handler entries
        //
        if (block->hasEHBoundaryIn())
        {
            verifyIncoming = false;
        }

        // Handler exits
        //
        if (block->hasEHBoundaryOut())
        {
            verifyOutgoing = false;
        }

        // We generally expect that the incoming flow, block weight and outgoing
        // flow should all match.
        //
        // But we have two edge counts... so for now we simply check if the block
        // count falls within the [min,max] range.
        //
        if (verifyIncoming)
        {
            BasicBlock::weight_t incomingWeightMin = 0;
            BasicBlock::weight_t incomingWeightMax = 0;
            bool                 foundPreds        = false;

            for (flowList* predEdge = block->bbPreds; predEdge != nullptr; predEdge = predEdge->flNext)
            {
                incomingWeightMin += predEdge->edgeWeightMin();
                incomingWeightMax += predEdge->edgeWeightMax();
                foundPreds = true;
            }

            if (!foundPreds)
            {
                // Might need to tone this down as we could see unreachable blocks?
                problemBlocks++;
                JITDUMP("  " FMT_BB " - expected to see predecessors\n", block->bbNum);
            }
            else
            {
                if (incomingWeightMin > incomingWeightMax)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - incoming min %d > incoming max %d\n", block->bbNum, incomingWeightMin,
                            incomingWeightMax);
                }
                else if (blockWeight < incomingWeightMin)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - block weight %d < incoming min %d\n", block->bbNum, blockWeight,
                            incomingWeightMin);
                }
                else if (blockWeight > incomingWeightMax)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - block weight %d > incoming max %d\n", block->bbNum, blockWeight,
                            incomingWeightMax);
                }
            }
        }

        if (verifyOutgoing)
        {
            const unsigned numSuccs = block->NumSucc();

            if (numSuccs == 0)
            {
                problemBlocks++;
                JITDUMP("  " FMT_BB " - expected to see successors\n", block->bbNum);
            }
            else
            {
                BasicBlock::weight_t outgoingWeightMin = 0;
                BasicBlock::weight_t outgoingWeightMax = 0;

                // Walking successor edges is a bit wonky. Seems like it should be easier.
                // Note this can also fail to enumerate all the edges, if we have a multigraph
                //
                int missingEdges = 0;

                for (unsigned i = 0; i < numSuccs; i++)
                {
                    BasicBlock* succBlock = block->GetSucc(i);
                    flowList*   succEdge  = nullptr;

                    for (flowList* edge = succBlock->bbPreds; edge != nullptr; edge = edge->flNext)
                    {
                        if (edge->getBlock() == block)
                        {
                            succEdge = edge;
                            break;
                        }
                    }

                    if (succEdge == nullptr)
                    {
                        missingEdges++;
                        JITDUMP("  " FMT_BB " can't find successor edge to " FMT_BB "\n", block->bbNum,
                                succBlock->bbNum);
                    }
                    else
                    {
                        outgoingWeightMin += succEdge->edgeWeightMin();
                        outgoingWeightMax += succEdge->edgeWeightMax();
                    }
                }

                if (missingEdges > 0)
                {
                    JITDUMP("  " FMT_BB " - missing %d successor edges\n", block->bbNum, missingEdges);
                    problemBlocks++;
                }
                if (outgoingWeightMin > outgoingWeightMax)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - outgoing min %d > outgoing max %d\n", block->bbNum, outgoingWeightMin,
                            outgoingWeightMax);
                }
                else if (blockWeight < outgoingWeightMin)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - block weight %d < outgoing min %d\n", block->bbNum, blockWeight,
                            outgoingWeightMin);
                }
                else if (blockWeight > outgoingWeightMax)
                {
                    problemBlocks++;
                    JITDUMP("  " FMT_BB " - block weight %d > outgoing max %d\n", block->bbNum, blockWeight,
                            outgoingWeightMax);
                }
            }
        }
    }

    // Verify overall input-output balance.
    //
    if (entryProfiled && exitProfiled)
    {
        if (entryWeight != exitWeight)
        {
            problemBlocks++;
            JITDUMP("  Entry %d exit %d mismatch\n", entryWeight, exitWeight);
        }
    }

    // Sum up what we discovered.
    //
    if (problemBlocks == 0)
    {
        if (profiledBlocks == 0)
        {
            JITDUMP("No blocks were profiled, so nothing to check\n");
        }
        else
        {
            JITDUMP("Profile is self-consistent (%d profiled blocks, %d unprofiled)\n", profiledBlocks,
                    unprofiledBlocks);
        }
    }
    else
    {
        JITDUMP("Profile is NOT self-consistent, found %d problems (%d profiled blocks, %d unprofiled)\n",
                problemBlocks, profiledBlocks, unprofiledBlocks);

        if (JitConfig.JitProfileChecks() == 2)
        {
            assert(!"Inconsistent profile");
        }
    }
}

#endif // DEBUG
