// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

class LocalSequencer final : public GenTreeVisitor<LocalSequencer>
{
    GenTree* m_prevNode;

public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    LocalSequencer(Compiler* comp)
        : GenTreeVisitor(comp)
        , m_prevNode(nullptr)
    {
    }

    //-------------------------------------------------------------------
    // Start: Start sequencing a statement. Must be called before other members
    // are called for a specified statement.
    //
    // Arguments:
    //     stmt - the statement
    //
    void Start(Statement* stmt)
    {
        // We use the root node as a 'sentinel' node that will keep the head
        // and tail of the sequenced list.
        GenTree* rootNode = stmt->GetRootNode();
        rootNode->gtPrev  = nullptr;
        rootNode->gtNext  = nullptr;
        m_prevNode        = rootNode;
    }

    //-------------------------------------------------------------------
    // Finish: Finish sequencing a statement. Should be called after sub nodes
    // of the statement have been visited and sequenced.
    //
    // Arguments:
    //     stmt - the statement
    //
    void Finish(Statement* stmt)
    {
        GenTree* rootNode = stmt->GetRootNode();

        GenTree* firstNode = rootNode->gtNext;
        GenTree* lastNode  = m_prevNode;

        if (firstNode == nullptr)
        {
            lastNode = nullptr;
        }
        else
        {
            // Clear the links on the sentinel in case it didn't end up in the list.
            if (rootNode != lastNode)
            {
                assert(rootNode->gtPrev == nullptr);
                rootNode->gtNext = nullptr;
            }

            lastNode->gtNext  = nullptr;
            firstNode->gtPrev = nullptr;
        }

        stmt->SetTreeList(firstNode);
        stmt->SetTreeListEnd(lastNode);
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* node = *use;
        if (node->OperIsAnyLocal())
        {
            SequenceLocal(node->AsLclVarCommon());
        }

        if (node->IsCall())
        {
            SequenceCall(node->AsCall());
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    //-------------------------------------------------------------------
    // SequenceLocal: Add a local to the list.
    //
    // Arguments:
    //     lcl - the local
    //
    void SequenceLocal(GenTreeLclVarCommon* lcl)
    {
        lcl->gtPrev        = m_prevNode;
        m_prevNode->gtNext = lcl;
        m_prevNode         = lcl;
    }

    //-------------------------------------------------------------------
    // SequenceCall: Post-process a call that may define a local.
    //
    // Arguments:
    //     call - the call
    //
    // Remarks:
    //     calls may also define a local that we would like to see
    //     after all other operands of the call have been evaluated.
    //
    void SequenceCall(GenTreeCall* call)
    {
        if (call->IsOptimizingRetBufAsLocal())
        {
            // Correct the point at which the definition of the retbuf local appears.
            MoveNodeToEnd(m_compiler->gtCallGetDefinedRetBufLclAddr(call));
        }
    }

    //-------------------------------------------------------------------
    // Sequence: Fully sequence a statement.
    //
    // Arguments:
    //     stmt - The statement
    //
    void Sequence(Statement* stmt)
    {
        Start(stmt);
        WalkTree(stmt->GetRootNodePointer(), nullptr);
        Finish(stmt);
    }

private:
    //-------------------------------------------------------------------
    // MoveNodeToEnd: Move a node from its current position in the linked list
    // to the end.
    //
    // Arguments:
    //     node - The node
    //
    void MoveNodeToEnd(GenTreeLclVarCommon* node)
    {
        if ((m_prevNode == node) || (node->gtNext == nullptr))
        {
            return;
        }

        GenTree* prev = node->gtPrev;
        GenTree* next = node->gtNext;

        assert(prev != nullptr); // Should have sentinel always, even as the first local.
        prev->gtNext = next;
        next->gtPrev = prev;

        SequenceLocal(node);
    }
};

//-------------------------------------------------------------------
// fgSequenceLocals: Sequence the locals in a statement.
//
// Arguments:
//     stmt - The statement.
//
// Remarks:
//     This is the locals-only (see fgNodeThreading) counterpart to fgSetStmtSeq.
//
void Compiler::fgSequenceLocals(Statement* stmt)
{
    assert(fgNodeThreading == NodeThreading::AllLocals);
    LocalSequencer seq(this);
    seq.Sequence(stmt);
}

struct LocalEqualsLocalAddrAssertion
{
    // Local num on the LHS
    unsigned DestLclNum;
    // Local num on the RHS (having its adress taken)
    unsigned AddressLclNum;
    // Offset into RHS local
    unsigned AddressOffset;

    LocalEqualsLocalAddrAssertion(unsigned destLclNum, unsigned addressLclNum, unsigned addressOffset)
        : DestLclNum(destLclNum)
        , AddressLclNum(addressLclNum)
        , AddressOffset(addressOffset)
    {
    }

#ifdef DEBUG
    void Print() const
    {
        printf("V%02u = &V%02u[+%03u]", DestLclNum, AddressLclNum, AddressOffset);
    }
#endif
};

struct AssertionKeyFuncs
{
    static bool Equals(const LocalEqualsLocalAddrAssertion& lhs, const LocalEqualsLocalAddrAssertion rhs)
    {
        return (lhs.DestLclNum == rhs.DestLclNum) && (lhs.AddressLclNum == rhs.AddressLclNum) &&
               (lhs.AddressOffset == rhs.AddressOffset);
    }

    static unsigned GetHashCode(const LocalEqualsLocalAddrAssertion& val)
    {
        unsigned hash = val.DestLclNum;
        hash ^= val.AddressLclNum + 0x9e3779b9 + (hash << 19) + (hash >> 13);
        hash ^= val.AddressOffset + 0x9e3779b9 + (hash << 19) + (hash >> 13);
        return hash;
    }
};

typedef JitHashTable<LocalEqualsLocalAddrAssertion, AssertionKeyFuncs, unsigned> AssertionToIndexMap;

class LocalEqualsLocalAddrAssertions
{
    Compiler*                                 m_comp;
    ArrayStack<LocalEqualsLocalAddrAssertion> m_assertions;
    AssertionToIndexMap                       m_map;
    uint64_t*                                 m_lclAssertions;
    uint64_t*                                 m_outgoingAssertions;
    BitVec                                    m_localsToExpose;

public:
    uint64_t CurrentAssertions = 0;

    LocalEqualsLocalAddrAssertions(Compiler* comp)
        : m_comp(comp)
        , m_assertions(comp->getAllocator(CMK_LocalAddressVisitor))
        , m_map(comp->getAllocator(CMK_LocalAddressVisitor))
    {
        m_lclAssertions =
            comp->lvaCount == 0 ? nullptr : new (comp, CMK_LocalAddressVisitor) uint64_t[comp->lvaCount]{};
        m_outgoingAssertions = new (comp, CMK_LocalAddressVisitor) uint64_t[comp->m_dfsTree->GetPostOrderCount()]{};

        BitVecTraits localsTraits(comp->lvaCount, comp);
        m_localsToExpose = BitVecOps::MakeEmpty(&localsTraits);
    }

    //------------------------------------------------------------------------
    // GetLocalsToExpose: Get the bit vector of locals that were marked to be
    // exposed.
    //
    BitVec_ValRet_T GetLocalsToExpose()
    {
        return m_localsToExpose;
    }

    //------------------------------------------------------------------------
    // IsMarkedForExposure: Check if a specific local is marked to be exposed.
    //
    // Return Value:
    //   True if so.
    //
    bool IsMarkedForExposure(unsigned lclNum)
    {
        BitVecTraits traits(m_comp->lvaCount, m_comp);
        if (BitVecOps::IsMember(&traits, m_localsToExpose, lclNum))
        {
            return true;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(lclNum);
        if (dsc->lvIsStructField && BitVecOps::IsMember(&traits, m_localsToExpose, dsc->lvParentLcl))
        {
            return true;
        }

        return false;
    }

    //-------------------------------------------------------------------
    // StartBlock: Start a new block by computing incoming assertions for the
    // block.
    //
    // Arguments:
    //   block - The block
    //
    void StartBlock(BasicBlock* block)
    {
        if ((m_assertions.Height() == 0) || (block->bbPreds == nullptr) || m_comp->bbIsHandlerBeg(block))
        {
            CurrentAssertions = 0;
            return;
        }

        CurrentAssertions = UINT64_MAX;
        for (BasicBlock* pred : block->PredBlocks())
        {
            assert(m_comp->m_dfsTree->Contains(pred));
            if (pred->bbPostorderNum <= block->bbPostorderNum)
            {
                CurrentAssertions = 0;
                break;
            }

            CurrentAssertions &= m_outgoingAssertions[pred->bbPostorderNum];
        }

#ifdef DEBUG
        if (CurrentAssertions != 0)
        {
            JITDUMP(FMT_BB " incoming assertions:\n", block->bbNum);
            uint64_t assertions = CurrentAssertions;
            do
            {
                uint32_t index = BitOperations::BitScanForward(assertions);
                JITDUMP("  A%02u: ", index);
                DBEXEC(VERBOSE, m_assertions.BottomRef((int)index).Print());
                JITDUMP("\n");

                assertions ^= uint64_t(1) << index;
            } while (assertions != 0);
        }
#endif
    }

    //-------------------------------------------------------------------
    // EndBlock: End a block by recording its final outgoing assertions.
    //
    // Arguments:
    //     block - The block
    //
    void EndBlock(BasicBlock* block)
    {
        m_outgoingAssertions[block->bbPostorderNum] = CurrentAssertions;
    }

    //-------------------------------------------------------------------
    // OnExposed: Mark that a local is having its address taken.
    //
    // Arguments:
    //   lclNum - The local
    //
    void OnExposed(unsigned lclNum)
    {
        JITDUMP("On exposed: V%02u\n", lclNum);
        BitVecTraits localsTraits(m_comp->lvaCount, m_comp);
        BitVecOps::AddElemD(&localsTraits, m_localsToExpose, lclNum);
    }

    //-------------------------------------------------------------------
    // Record: Record an assertion about the specified local.
    //
    // Arguments:
    //   dstLclNum - Destination local
    //   srcLclNum - Local having its address taken
    //   srcOffs   - Offset into the source local of the address being taken
    //
    void Record(unsigned dstLclNum, unsigned srcLclNum, unsigned srcOffs)
    {
        LocalEqualsLocalAddrAssertion assertion(dstLclNum, srcLclNum, srcOffs);

        unsigned index;
        if (m_assertions.Height() >= 64)
        {
            if (!m_map.Lookup(assertion, &index))
            {
                JITDUMP("Out of assertion space; dropping assertion ");
                DBEXEC(VERBOSE, assertion.Print());
                JITDUMP("\n");
                return;
            }
        }
        else
        {
            unsigned* pIndex = m_map.LookupPointerOrAdd(assertion, UINT_MAX);
            if (*pIndex == UINT_MAX)
            {
                index   = (unsigned)m_assertions.Height();
                *pIndex = index;
                m_assertions.Push(assertion);
                m_lclAssertions[dstLclNum] |= uint64_t(1) << index;

                JITDUMP("Adding new assertion A%02u ", index);
                DBEXEC(VERBOSE, assertion.Print());
                JITDUMP("\n");
            }
            else
            {
                index = *pIndex;
                JITDUMP("Adding existing assertion A%02u ", index);
                DBEXEC(VERBOSE, assertion.Print());
                JITDUMP("\n");
            }
        }

        CurrentAssertions |= uint64_t(1) << index;
    }

    //-------------------------------------------------------------------
    // Clear: Clear active assertions about the specified local.
    //
    // Arguments:
    //   dstLclNum - Destination local
    //
    void Clear(unsigned dstLclNum)
    {
        CurrentAssertions &= ~m_lclAssertions[dstLclNum];
    }

    //-----------------------------------------------------------------------------------
    // GetCurrentAssertion:
    //   Get the current assertion about a local's value.
    //
    // Arguments:
    //    lclNum - The local
    //
    // Return Value:
    //    Assertion, or nullptr if there is no current assertion.
    //
    const LocalEqualsLocalAddrAssertion* GetCurrentAssertion(unsigned lclNum)
    {
        uint64_t curAssertion = CurrentAssertions & m_lclAssertions[lclNum];
        assert(genMaxOneBit(curAssertion));
        if (curAssertion == 0)
        {
            return nullptr;
        }

        return &m_assertions.BottomRef(BitOperations::BitScanForward(curAssertion));
    }

    //-----------------------------------------------------------------------------------
    // GetLocalsWithAssertions:
    //   Get a bit vector of all locals that have assertions about their value.
    //
    // Return Value:
    //   Bit vector of locals.
    //
    BitVec_ValRet_T GetLocalsWithAssertions()
    {
        BitVecTraits localsTraits(m_comp->lvaCount, m_comp);
        BitVec       result(BitVecOps::MakeEmpty(&localsTraits));

        for (int i = 0; i < m_assertions.Height(); i++)
        {
            BitVecOps::AddElemD(&localsTraits, result, m_assertions.BottomRef(i).DestLclNum);
        }

        return result;
    }
};

class LocalAddressVisitor final : public GenTreeVisitor<LocalAddressVisitor>
{
    // During tree traversal every GenTree node produces a "value" that represents:
    //   - the address of local variable memory location, including an offset as well.
    //   - an unknown value - the result of a node we don't know how to process. This
    //     also includes the result of TYP_VOID nodes (or any other nodes that don't
    //     actually produce values in IR) in order to support the invariant that every
    //     node produces a value.
    //
    // Each value is processed ("escaped") when visiting (in post-order) its parent.
    //
    class Value
    {
        GenTree** m_use;
        unsigned  m_lclNum;
        unsigned  m_offset;
        INDEBUG(bool m_consumed);

    public:
        // Produce an unknown value associated with the specified node.
        Value(GenTree** use)
            : m_use(use)
            , m_lclNum(BAD_VAR_NUM)
            , m_offset(0)
#ifdef DEBUG
            , m_consumed(false)
#endif // DEBUG
        {
        }

        // Get the use for the node that produced this value.
        GenTree** Use() const
        {
            return m_use;
        }

        // Get the node that produced this value.
        GenTree* Node() const
        {
            return *m_use;
        }

        // Does this value represent "unknown"?
        bool IsUnknown() const
        {
            return !IsAddress();
        }

        // Does this value represent a local address?
        bool IsAddress() const
        {
            return m_lclNum != BAD_VAR_NUM;
        }

        // Get the address's variable number.
        unsigned LclNum() const
        {
            assert(IsAddress());
            return m_lclNum;
        }

        // Get the address's byte offset.
        unsigned Offset() const
        {
            assert(IsAddress());
            return m_offset;
        }

        bool IsSameAddress(const Value& other) const
        {
            assert(IsAddress() && other.IsAddress());

            return ((LclNum() == other.LclNum()) && (Offset() == other.Offset()));
        }

        //------------------------------------------------------------------------
        // Address: Produce an address value from a LCL_ADDR node.
        //
        // Arguments:
        //    lclAddr - a GT_LCL_ADDR node that defines the address
        //
        // Notes:
        //   - (lclnum, lclOffs) => ADDRESS(lclNum, offset)
        //
        void Address(GenTreeLclFld* lclAddr)
        {
            assert(IsUnknown() && lclAddr->OperIs(GT_LCL_ADDR));
            m_lclNum = lclAddr->GetLclNum();
            m_offset = lclAddr->GetLclOffs();
        }

        void Address(unsigned lclNum, unsigned lclOffs)
        {
            assert(IsUnknown());
            m_lclNum = lclNum;
            m_offset = lclOffs;
        }

        //------------------------------------------------------------------------
        // AddOffset: Produce an address value from an address value.
        //
        // Arguments:
        //    val       - the input value
        //    addOffset - the offset to add
        //
        // Return Value:
        //    `true` if the value was consumed. `false` if the input value
        //    cannot be consumed because it is not an address or because
        //    the offset overflowed. In this case the caller is expected
        //    to escape the input value.
        //
        // Notes:
        //   - ADDRESS(lclNum, offset) => ADDRESS(lclNum, offset + offs)
        //   - UNKNOWN => UNKNOWN
        //
        bool AddOffset(Value& val, unsigned addOffset)
        {
            assert(IsUnknown());

            if (val.IsAddress())
            {
                ClrSafeInt<unsigned> newOffset = ClrSafeInt<unsigned>(val.m_offset) + ClrSafeInt<unsigned>(addOffset);

                if (newOffset.IsOverflow())
                {
                    return false;
                }

                m_lclNum = val.m_lclNum;
                m_offset = newOffset.Value();
            }

            INDEBUG(val.Consume());
            return true;
        }

#ifdef DEBUG
        void Consume()
        {
            assert(!m_consumed);
            // Mark the value as consumed so that PopValue can ensure that values
            // aren't popped from the stack without being processed appropriately.
            m_consumed = true;
        }

        bool IsConsumed()
        {
            return m_consumed;
        }
#endif // DEBUG
    };

    enum class IndirTransform
    {
        Nop,
        BitCast,
        NarrowCast,
#ifdef FEATURE_HW_INTRINSICS
        GetElement,
        WithElement,
#endif // FEATURE_HW_INTRINSICS
        LclVar,
        LclFld
    };

    ArrayStack<Value>               m_valueStack;
    bool                            m_stmtModified    = false;
    bool                            m_madeChanges     = false;
    bool                            m_propagatedAddrs = false;
    LocalSequencer*                 m_sequencer;
    LocalEqualsLocalAddrAssertions* m_lclAddrAssertions;

public:
    enum
    {
        DoPreOrder        = true,
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    LocalAddressVisitor(Compiler* comp, LocalSequencer* sequencer, LocalEqualsLocalAddrAssertions* assertions)
        : GenTreeVisitor<LocalAddressVisitor>(comp)
        , m_valueStack(comp->getAllocator(CMK_LocalAddressVisitor))
        , m_sequencer(sequencer)
        , m_lclAddrAssertions(assertions)
    {
    }

    bool MadeChanges() const
    {
        return m_madeChanges;
    }

    bool PropagatedAnyAddresses() const
    {
        return m_propagatedAddrs;
    }

    void VisitStmt(Statement* stmt)
    {
#ifdef DEBUG
        if (m_compiler->verbose)
        {
            printf("LocalAddressVisitor visiting statement:\n");
            m_compiler->gtDispStmt(stmt);
        }
#endif // DEBUG

        m_stmtModified = false;

        if (m_sequencer != nullptr)
        {
            m_sequencer->Start(stmt);
        }

        WalkTree(stmt->GetRootNodePointer(), nullptr);

        // If we have an address on the stack then we don't need to do anything.
        // The address tree isn't actually used and it will be discarded during
        // morphing. So just mark any value as consumed to keep PopValue happy.
        INDEBUG(TopValue(0).Consume());

        PopValue();
        assert(m_valueStack.Empty());
        m_madeChanges |= m_stmtModified;

        if (m_sequencer != nullptr)
        {
            if (m_stmtModified)
            {
                m_sequencer->Sequence(stmt);
            }
            else
            {
                m_sequencer->Finish(stmt);
            }
        }

#ifdef DEBUG
        if (m_compiler->verbose)
        {
            if (m_stmtModified)
            {
                printf("LocalAddressVisitor modified statement:\n");
                m_compiler->gtDispStmt(stmt);
            }

            printf("\n");
        }
#endif // DEBUG
    }

    void VisitBlock(BasicBlock* block)
    {
        // Make the current basic block address available globally
        m_compiler->compCurBB = block;

        if (m_lclAddrAssertions != nullptr)
        {
            m_lclAddrAssertions->StartBlock(block);
        }

        for (Statement* const stmt : block->Statements())
        {
#ifdef FEATURE_SIMD
            if (m_compiler->opts.OptimizationEnabled() && stmt->GetRootNode()->TypeIs(TYP_FLOAT) &&
                stmt->GetRootNode()->OperIsStore())
            {
                m_madeChanges |= m_compiler->fgMorphCombineSIMDFieldStores(block, stmt);
            }
#endif

            VisitStmt(stmt);
        }

        // We could check for GT_JMP inside the visitor, but this node is very
        // rare so keeping it here avoids pessimizing the hot code.
        if (block->endsWithJmpMethod(m_compiler))
        {
            // GT_JMP has implicit uses of all arguments.
            for (unsigned lclNum = 0; lclNum < m_compiler->info.compArgsCount; lclNum++)
            {
                UpdateEarlyRefCount(lclNum, nullptr, nullptr);
            }
        }

        if (m_lclAddrAssertions != nullptr)
        {
            m_lclAddrAssertions->EndBlock(block);
        }
    }

    // Morph promoted struct fields and count local occurrences.
    //
    // Also create and push the value produced by the visited node. This is done here
    // rather than in PostOrderVisit because it makes it easy to handle nodes with an
    // arbitrary number of operands - just pop values until the value corresponding
    // to the visited node is encountered.
    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* const node = *use;

        switch (node->OperGet())
        {
            case GT_IND:
            case GT_BLK:
            case GT_STOREIND:
            case GT_STORE_BLK:
                if (MorphStructField(node->AsIndir(), user))
                {
                    goto LOCAL_NODE;
                }
                break;

            case GT_FIELD_ADDR:
                if (MorphStructFieldAddress(node, 0) != BAD_VAR_NUM)
                {
                    goto LOCAL_NODE;
                }
                break;

            case GT_LCL_FLD:
            case GT_STORE_LCL_FLD:
                MorphLocalField(node->AsLclVarCommon(), user);
                goto LOCAL_NODE;

            case GT_LCL_VAR:
            case GT_LCL_ADDR:
            case GT_STORE_LCL_VAR:
            LOCAL_NODE:
            {
                unsigned const   lclNum = node->AsLclVarCommon()->GetLclNum();
                LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

                UpdateEarlyRefCount(lclNum, node, user);

                if (varDsc->lvIsStructField)
                {
                    // Promoted field, increase count for the parent lclVar.
                    //
                    assert(!m_compiler->lvaIsImplicitByRefLocal(lclNum));
                    unsigned parentLclNum = varDsc->lvParentLcl;
                    UpdateEarlyRefCount(parentLclNum, node, user);
                }

                if (varDsc->lvPromoted)
                {
                    // Promoted struct, increase count for each promoted field.
                    //
                    for (unsigned childLclNum = varDsc->lvFieldLclStart;
                         childLclNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++childLclNum)
                    {
                        UpdateEarlyRefCount(childLclNum, node, user);
                    }
                }
            }
            break;

            case GT_QMARK:
                return HandleQMarkSubTree(use);

            default:
                break;
        }

        PushValue(use);
        return Compiler::WALK_CONTINUE;
    }

    // Evaluate a node. Since this is done in postorder, the node's operands have already been
    // evaluated and are available on the value stack. The value produced by the visited node
    // is left on the top of the evaluation stack.
    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* node = *use;

        switch (node->OperGet())
        {
            case GT_STORE_LCL_FLD:
                if (node->IsPartialLclFld(m_compiler))
                {
                    node->gtFlags |= GTF_VAR_USEASG;
                }
                FALLTHROUGH;

            case GT_STORE_LCL_VAR:
            {
                assert(TopValue(0).Node() == node->AsLclVarCommon()->Data());
                if (m_lclAddrAssertions != nullptr)
                {
                    HandleLocalStoreAssertions(node->AsLclVarCommon(), TopValue(0));
                }

                EscapeValue(TopValue(0), node);
                PopValue();

                SequenceLocal(node->AsLclVarCommon());
                break;
            }

            case GT_LCL_VAR:
                if (m_lclAddrAssertions != nullptr)
                {
                    HandleLocalAssertions(node->AsLclVarCommon(), TopValue(0));
                }

                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_LCL_FLD:
                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_LCL_ADDR:
                assert(TopValue(0).Node() == node);

                TopValue(0).Address(node->AsLclFld());
                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_ADD:
                assert(TopValue(2).Node() == node);
                assert(TopValue(1).Node() == node->gtGetOp1());
                assert(TopValue(0).Node() == node->gtGetOp2());

                if (node->gtGetOp2()->IsCnsIntOrI())
                {
                    ssize_t offset = node->gtGetOp2()->AsIntCon()->IconValue();
                    if (FitsIn<unsigned>(offset) && TopValue(2).AddOffset(TopValue(1), static_cast<unsigned>(offset)))
                    {
                        INDEBUG(TopValue(0).Consume());
                        PopValue();
                        PopValue();
                        break;
                    }
                }

                EscapeValue(TopValue(0), node);
                PopValue();
                EscapeValue(TopValue(0), node);
                PopValue();
                break;

            case GT_SUB:
            {
                Value& rhs = TopValue(0);
                Value& lhs = TopValue(1);
                if (m_compiler->opts.OptimizationEnabled() && lhs.IsAddress() && rhs.IsAddress() &&
                    (lhs.LclNum() == rhs.LclNum()) && (rhs.Offset() <= lhs.Offset()) &&
                    FitsIn<int>(lhs.Offset() - rhs.Offset()))
                {
                    // TODO-Bug: Due to inlining we may end up with incorrectly typed SUB trees here.
                    assert(node->TypeIs(TYP_I_IMPL, TYP_BYREF));

                    ssize_t result = (ssize_t)(lhs.Offset() - rhs.Offset());
                    node->BashToConst(result, TYP_I_IMPL);
                    INDEBUG(lhs.Consume());
                    INDEBUG(rhs.Consume());
                    PopValue();
                    PopValue();
                    m_stmtModified = true;
                    break;
                }

                EscapeValue(TopValue(0), node);
                PopValue();
                EscapeValue(TopValue(0), node);
                PopValue();
                break;
            }

            case GT_FIELD_ADDR:
                if (node->AsFieldAddr()->IsInstance())
                {
                    assert(TopValue(1).Node() == node);
                    assert(TopValue(0).Node() == node->AsFieldAddr()->GetFldObj());

                    if (!TopValue(1).AddOffset(TopValue(0), node->AsFieldAddr()->gtFldOffset))
                    {
                        // The field object did not represent an address, or the latter overflowed.
                        EscapeValue(TopValue(0), node);
                    }

                    PopValue();
                }
                else
                {
                    assert(TopValue(0).Node() == node);
                }
                break;

            case GT_STOREIND:
            case GT_STORE_BLK:
            {
                assert(TopValue(2).Node() == node);
                assert(TopValue(1).Node() == node->AsIndir()->Addr());
                assert(TopValue(0).Node() == node->AsIndir()->Data());

                // Data value always escapes.
                EscapeValue(TopValue(0), node);

                if (node->AsIndir()->IsVolatile() || !TopValue(1).IsAddress())
                {
                    // Volatile indirections must not be removed so the address, if any, must be escaped.
                    EscapeValue(TopValue(1), node);
                }
                else
                {
                    // This consumes the address.
                    ProcessIndirection(use, TopValue(1), user);

                    if ((m_lclAddrAssertions != nullptr) && (*use)->OperIsLocalStore())
                    {
                        HandleLocalStoreAssertions((*use)->AsLclVarCommon(), TopValue(0));
                    }
                }

                PopValue();
                PopValue();
                break;
            }

            case GT_BLK:
            case GT_IND:
                assert(TopValue(1).Node() == node);
                assert(TopValue(0).Node() == node->AsIndir()->Addr());

                if (node->AsIndir()->IsVolatile() || !TopValue(0).IsAddress())
                {
                    // Volatile indirections must not be removed so the address, if any, must be escaped.
                    EscapeValue(TopValue(0), node);
                }
                else
                {
                    ProcessIndirection(use, TopValue(0), user);

                    if ((m_lclAddrAssertions != nullptr) && (*use)->OperIs(GT_LCL_VAR))
                    {
                        HandleLocalAssertions((*use)->AsLclVarCommon(), TopValue(1));
                    }
                }

                PopValue();
                break;

            case GT_RETURN:
                if (TopValue(0).Node() != node)
                {
                    assert(TopValue(1).Node() == node);
                    assert(TopValue(0).Node() == node->gtGetOp1());
                    GenTreeUnOp* ret    = node->AsUnOp();
                    GenTree*     retVal = ret->gtGetOp1();
                    if (retVal->OperIs(GT_LCL_VAR))
                    {
                        // TODO-1stClassStructs: this block is a temporary workaround to keep diffs small,
                        // having `doNotEnreg` affect block init and copy transformations that affect many methods.
                        // I have a change that introduces more precise and effective solution for that, but it would
                        // be merged separately.
                        GenTreeLclVar* lclVar = retVal->AsLclVar();
                        unsigned       lclNum = lclVar->GetLclNum();
                        if (!m_compiler->compMethodReturnsMultiRegRetType() &&
                            !m_compiler->lvaIsImplicitByRefLocal(lclVar->GetLclNum()))
                        {
                            LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
                            if (varDsc->lvFieldCnt > 1)
                            {
                                m_compiler->lvaSetVarDoNotEnregister(
                                    lclNum DEBUGARG(DoNotEnregisterReason::BlockOpRet));
                            }
                        }
                    }

                    EscapeValue(TopValue(0), node);
                    PopValue();
                }
                break;

            case GT_CALL:
                while (TopValue(0).Node() != node)
                {
                    EscapeValue(TopValue(0), node);
                    PopValue();
                }

                SequenceCall(node->AsCall());
                break;

            case GT_EQ:
            case GT_NE:
            {
                // If we see &lcl EQ/NE null, rewrite to 0/1 comparison
                // to reduce overall address exposure.
                //
                assert(TopValue(2).Node() == node);
                assert(TopValue(1).Node() == node->AsOp()->gtOp1);
                assert(TopValue(0).Node() == node->AsOp()->gtOp2);

                Value& lhs = TopValue(1);
                Value& rhs = TopValue(0);

                if ((lhs.IsAddress() && rhs.Node()->IsIntegralConst(0)) ||
                    (rhs.IsAddress() && lhs.Node()->IsIntegralConst(0)))
                {
                    JITDUMP("Rewriting known address vs null comparison [%06u]\n", m_compiler->dspTreeID(node));
                    *lhs.Use()     = m_compiler->gtNewIconNode(0);
                    *rhs.Use()     = m_compiler->gtNewIconNode(1);
                    m_stmtModified = true;

                    INDEBUG(TopValue(0).Consume());
                    INDEBUG(TopValue(1).Consume());
                    PopValue();
                    PopValue();
                }
                else if (lhs.IsAddress() && rhs.IsAddress())
                {
                    JITDUMP("Rewriting known address vs address comparison [%06u]\n", m_compiler->dspTreeID(node));
                    bool isSameAddress = lhs.IsSameAddress(rhs);
                    *lhs.Use()         = m_compiler->gtNewIconNode(0);
                    *rhs.Use()         = m_compiler->gtNewIconNode(isSameAddress ? 0 : 1);
                    m_stmtModified     = true;

                    INDEBUG(TopValue(0).Consume());
                    INDEBUG(TopValue(1).Consume());
                    PopValue();
                    PopValue();
                }
                else
                {
                    EscapeValue(TopValue(0), node);
                    PopValue();
                    EscapeValue(TopValue(0), node);
                    PopValue();
                }

                break;
            }

            case GT_NULLCHECK:
            {
                assert(TopValue(1).Node() == node);
                assert(TopValue(0).Node() == node->AsOp()->gtOp1);
                Value& op = TopValue(0);
                if (op.IsAddress())
                {
                    JITDUMP("Bashing nullcheck of local [%06u] to NOP\n", m_compiler->dspTreeID(node));
                    node->gtBashToNOP();
                    INDEBUG(TopValue(0).Consume());
                    PopValue();
                    m_stmtModified = true;
                }
                else
                {
                    EscapeValue(TopValue(0), node);
                    PopValue();
                }
                break;
            }

            default:
                while (TopValue(0).Node() != node)
                {
                    EscapeValue(TopValue(0), node);
                    PopValue();
                }
                break;
        }

        assert(TopValue(0).Node() == *use);
        return Compiler::WALK_CONTINUE;
    }

private:
    //------------------------------------------------------------------------
    // HandleQMarkSubTree: Process a sub-tree rooted at a GT_QMARK.
    //
    // Arguments:
    //   use - the use of the qmark
    //
    // Returns:
    //   The walk result.
    //
    // Remarks:
    //   GT_QMARK needs special handling due to the conditional nature of it.
    //   Particularly when we are optimizing and propagating LCL_ADDRs we need
    //   to take care that assertions created inside the conditionally executed
    //   parts are handled appropriately. This function inlines the pre and
    //   post-order visit logic here to make that handling work.
    //
    fgWalkResult HandleQMarkSubTree(GenTree** use)
    {
        assert((*use)->OperIs(GT_QMARK));
        GenTreeQmark* qmark = (*use)->AsQmark();

        // We have to inline the pre/postorder visit here to handle
        // assertions properly.
        assert(!qmark->IsReverseOp());
        if (WalkTree(&qmark->gtOp1, qmark) == Compiler::WALK_ABORT)
        {
            return Compiler::WALK_ABORT;
        }

        if (m_lclAddrAssertions != nullptr)
        {
            uint64_t origAssertions = m_lclAddrAssertions->CurrentAssertions;

            if (WalkTree(&qmark->gtOp2->AsOp()->gtOp1, qmark->gtOp2) == Compiler::WALK_ABORT)
            {
                return Compiler::WALK_ABORT;
            }

            uint64_t op1Assertions                 = m_lclAddrAssertions->CurrentAssertions;
            m_lclAddrAssertions->CurrentAssertions = origAssertions;

            if (WalkTree(&qmark->gtOp2->AsOp()->gtOp2, qmark->gtOp2) == Compiler::WALK_ABORT)
            {
                return Compiler::WALK_ABORT;
            }

            uint64_t op2Assertions                 = m_lclAddrAssertions->CurrentAssertions;
            m_lclAddrAssertions->CurrentAssertions = op1Assertions & op2Assertions;
        }
        else
        {
            if ((WalkTree(&qmark->gtOp2->AsOp()->gtOp1, qmark->gtOp2) == Compiler::WALK_ABORT) ||
                (WalkTree(&qmark->gtOp2->AsOp()->gtOp2, qmark->gtOp2) == Compiler::WALK_ABORT))
            {
                return Compiler::WALK_ABORT;
            }
        }

        assert(TopValue(0).Node() == qmark->gtGetOp2()->gtGetOp2());
        assert(TopValue(1).Node() == qmark->gtGetOp2()->gtGetOp1());
        assert(TopValue(2).Node() == qmark->gtGetOp1());

        EscapeValue(TopValue(0), qmark->gtGetOp2());
        PopValue();

        EscapeValue(TopValue(0), qmark->gtGetOp2());
        PopValue();

        EscapeValue(TopValue(0), qmark);
        PopValue();

        PushValue(use);
        return Compiler::WALK_SKIP_SUBTREES;
    }

    void PushValue(GenTree** use)
    {
        m_valueStack.Push(use);
    }

    Value& TopValue(unsigned index)
    {
        return m_valueStack.TopRef(index);
    }

    void PopValue()
    {
        assert(TopValue(0).IsConsumed());
        m_valueStack.Pop();
    }

    //------------------------------------------------------------------------
    // EscapeValue: Process an escaped value
    //
    // Arguments:
    //    val - the escaped address value
    //    user - the node that uses the escaped value
    //
    void EscapeValue(Value& val, GenTree* user)
    {
        if (val.IsAddress())
        {
            EscapeAddress(val, user);
        }
        else
        {
            INDEBUG(val.Consume();)
        }
    }

    //------------------------------------------------------------------------
    // EscapeAddress: Process an escaped address value
    //
    // Arguments:
    //    val - the escaped address value
    //    user - the node that uses the address value
    //
    void EscapeAddress(Value& val, GenTree* user)
    {
        assert(val.IsAddress());

        unsigned   lclNum = val.LclNum();
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        GenTreeFlags defFlag            = GTF_EMPTY;
        GenTreeCall* callUser           = user->IsCall() ? user->AsCall() : nullptr;
        bool         hasHiddenStructArg = false;
        if (m_compiler->opts.compJitOptimizeStructHiddenBuffer && (callUser != nullptr) &&
            m_compiler->IsValidLclAddr(lclNum, val.Offset()))
        {
            // We will only attempt this optimization for locals that do not
            // later turn into indirections.
            bool isSuitableLocal =
                varTypeIsStruct(varDsc) && !m_compiler->lvaIsImplicitByRefLocal(lclNum) &&
                (!varDsc->lvIsStructField || !m_compiler->lvaIsImplicitByRefLocal(varDsc->lvParentLcl));
#ifdef TARGET_X86
            if (m_compiler->lvaIsArgAccessedViaVarArgsCookie(lclNum))
            {
                isSuitableLocal = false;
            }
#endif // TARGET_X86

            if (isSuitableLocal && callUser->gtArgs.HasRetBuffer() &&
                (val.Node() == callUser->gtArgs.GetRetBufferArg()->GetNode()))
            {
                m_compiler->lvaSetHiddenBufferStructArg(lclNum);
                hasHiddenStructArg = true;
                callUser->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG_LCLOPT;
                defFlag = GTF_VAR_DEF;

                if ((val.Offset() != 0) ||
                    (varDsc->lvExactSize() != m_compiler->typGetObjLayout(callUser->gtRetClsHnd)->GetSize()))
                {
                    defFlag |= GTF_VAR_USEASG;
                }
            }
        }

        if (!hasHiddenStructArg)
        {
            unsigned exposedLclNum = varDsc->lvIsStructField ? varDsc->lvParentLcl : lclNum;

            if (m_lclAddrAssertions != nullptr)
            {
                m_lclAddrAssertions->OnExposed(exposedLclNum);
            }
            else
            {
                m_compiler->lvaSetVarAddrExposed(exposedLclNum DEBUGARG(AddressExposedReason::ESCAPE_ADDRESS));
            }
        }

#ifdef TARGET_64BIT
        // If the address of a variable is passed in a call and the allocation size of the variable
        // is 32 bits we will quirk the size to 64 bits. Some PInvoke signatures incorrectly specify
        // a ByRef to an INT32 when they actually write a SIZE_T or INT64. There are cases where
        // overwriting these extra 4 bytes corrupts some data (such as a saved register) that leads
        // to A/V. Whereas previously the JIT64 codegen did not lead to an A/V.
        if ((callUser != nullptr) && !varDsc->lvIsParam && !varDsc->lvIsStructField && genActualTypeIsInt(varDsc))
        {
            varDsc->lvQuirkToLong = true;
            JITDUMP("Adding a quirk for the storage size of V%02u of type %s\n", val.LclNum(),
                    varTypeName(varDsc->TypeGet()));
        }
#endif // TARGET_64BIT

        MorphLocalAddress(val.Node(), lclNum, val.Offset());
        val.Node()->gtFlags |= defFlag;

        INDEBUG(val.Consume();)
    }

    //------------------------------------------------------------------------
    // ProcessIndirection: Process an indirection node
    //
    // Arguments:
    //    use  - indirection's use
    //    val  - the address value
    //    user - node that uses the indirection
    //
    void ProcessIndirection(GenTree** use, Value& val, GenTree* user)
    {
        assert(val.IsAddress());

        // It is possible for the indirection to be wider than the lclvar
        // (e.g. *(long*)&int32Var) or to have a field offset that pushes the indirection
        // past the end of the lclvar memory location. In such cases we cannot do anything
        // so the lclvar needs to be address exposed.
        //
        // More importantly, if the lclvar is a promoted struct field then the parent lclvar
        // also needs to be address exposed so we get dependent struct promotion. Code like
        // *(long*)&int32Var has undefined behavior and it's practically useless but reading,
        // say, 2 consecutive Int32 struct fields as Int64 has more practical value.

        GenTree*   node      = *use;
        unsigned   lclNum    = val.LclNum();
        unsigned   offset    = val.Offset();
        LclVarDsc* varDsc    = m_compiler->lvaGetDesc(lclNum);
        unsigned   indirSize = node->AsIndir()->Size();
        bool       isWide;

        // TODO-Cleanup: delete "indirSize == 0", use "Compiler::IsValidLclAddr".
        if ((indirSize == 0) || ((offset + indirSize) > UINT16_MAX))
        {
            // If we can't figure out the indirection size then treat it as a wide indirection.
            // Additionally, treat indirections with large offsets as wide: local field nodes
            // and the emitter do not support them.
            isWide = true;
        }
        else
        {
            ClrSafeInt<unsigned> endOffset = ClrSafeInt<unsigned>(offset) + ClrSafeInt<unsigned>(indirSize);

            if (endOffset.IsOverflow())
            {
                isWide = true;
            }
            else
            {
                isWide = endOffset.Value() > m_compiler->lvaLclExactSize(lclNum);
            }
        }

        if (isWide)
        {
            unsigned exposedLclNum = varDsc->lvIsStructField ? varDsc->lvParentLcl : lclNum;
            if (m_lclAddrAssertions != nullptr)
            {
                m_lclAddrAssertions->OnExposed(exposedLclNum);
            }
            else
            {
                m_compiler->lvaSetVarAddrExposed(exposedLclNum DEBUGARG(AddressExposedReason::WIDE_INDIR));
            }

            MorphLocalAddress(node->AsIndir()->Addr(), lclNum, offset);
            node->gtFlags |= GTF_GLOB_REF; // GLOB_REF may not be set already in the "large offset" case.
        }
        else
        {
            MorphLocalIndir(use, lclNum, offset, user);
        }

        INDEBUG(val.Consume());
    }

    //------------------------------------------------------------------------
    // MorphLocalAddress: Change a tree that represents a local variable address
    //    to a single LCL_VAR_ADDR or LCL_FLD_ADDR node.
    //
    // Arguments:
    //    addr   - The address tree
    //    lclNum - Local number of the variable in question
    //    offset - Offset for the address
    //
    void MorphLocalAddress(GenTree* addr, unsigned lclNum, unsigned offset)
    {
        assert(addr->TypeIs(TYP_BYREF, TYP_I_IMPL));
        assert(m_compiler->lvaVarAddrExposed(lclNum) ||
               ((m_lclAddrAssertions != nullptr) && m_lclAddrAssertions->IsMarkedForExposure(lclNum)) ||
               m_compiler->lvaGetDesc(lclNum)->IsHiddenBufferStructArg());

        if (m_compiler->IsValidLclAddr(lclNum, offset))
        {
            addr->ChangeOper(GT_LCL_ADDR);
            addr->AsLclFld()->SetLclNum(lclNum);
            addr->AsLclFld()->SetLclOffs(offset);
        }
        else
        {
            // The offset was too large to store in a LCL_FLD_ADDR node, use ADD(LCL_VAR_ADDR, offset) instead.
            addr->ChangeOper(GT_ADD);
            addr->AsOp()->gtOp1 = m_compiler->gtNewLclVarAddrNode(lclNum);
            addr->AsOp()->gtOp2 = m_compiler->gtNewIconNode(offset, TYP_I_IMPL);
        }

        // Local address nodes never have side effects (nor any other flags, at least at this point).
        addr->gtFlags  = GTF_EMPTY;
        m_stmtModified = true;
    }

    //------------------------------------------------------------------------
    // MorphLocalIndir: Change a tree that represents indirect access to a
    //    local variable to a canonical shape (one of "IndirTransform"s).
    //
    // Arguments:
    //    use    - indirection's use
    //    lclNum - local's number
    //    offset - access' offset
    //    user   - indirection's user
    //
    void MorphLocalIndir(GenTree** use, unsigned lclNum, unsigned offset, GenTree* user)
    {
        GenTree*             indir     = *use;
        ClassLayout*         layout    = indir->OperIs(GT_BLK, GT_STORE_BLK) ? indir->AsBlk()->GetLayout() : nullptr;
        IndirTransform       transform = SelectLocalIndirTransform(indir->AsIndir(), lclNum, offset, user);
        LclVarDsc*           varDsc    = m_compiler->lvaGetDesc(lclNum);
        GenTreeLclVarCommon* lclNode   = nullptr;
        bool                 isDef     = indir->OperIs(GT_STOREIND, GT_STORE_BLK);

        switch (transform)
        {
            case IndirTransform::Nop:
                indir->gtBashToNOP();
                m_stmtModified = true;
                return;

            case IndirTransform::BitCast:
                indir->ChangeOper(GT_BITCAST);
                lclNode = indir->gtGetOp1()->BashToLclVar(m_compiler, lclNum);
                break;

            case IndirTransform::NarrowCast:
                assert(varTypeIsIntegral(indir));
                assert(varTypeIsIntegral(varDsc));
                assert(genTypeSize(varDsc) >= genTypeSize(indir));
                assert(!isDef);

                lclNode = indir->gtGetOp1()->BashToLclVar(m_compiler, lclNum);
                *use    = m_compiler->gtNewCastNode(genActualType(indir), lclNode, false, indir->TypeGet());
                break;

#ifdef FEATURE_HW_INTRINSICS
                // We have three cases we want to handle:
                // 1. Vector2/3/4 and Quaternion where we have 2-4x float fields
                // 2. Plane where we have 1x Vector3 and 1x float field
                // 3. Accesses of halves of larger SIMD types

            case IndirTransform::GetElement:
            {
                GenTree*  hwiNode     = nullptr;
                var_types elementType = indir->TypeGet();
                lclNode               = indir->gtGetOp1()->BashToLclVar(m_compiler, lclNum);

                switch (elementType)
                {
                    case TYP_FLOAT:
                    {
                        // Handle case 1 or the float field of case 2
                        GenTree* indexNode = m_compiler->gtNewIconNode(offset / genTypeSize(elementType));
                        hwiNode            = m_compiler->gtNewSimdGetElementNode(elementType, lclNode, indexNode,
                                                                                 CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        break;
                    }

                    case TYP_SIMD12:
                    {
                        // Handle the Vector3 field of case 2
                        assert(genTypeSize(varDsc) == 16);
                        hwiNode = m_compiler->gtNewSimdHWIntrinsicNode(elementType, lclNode, NI_Vector128_AsVector3,
                                                                       CORINFO_TYPE_FLOAT, 16);
                        break;
                    }

                    case TYP_SIMD8:
#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
                    case TYP_SIMD16:
                    case TYP_SIMD32:
#endif
                    {
                        // Handle case 3
                        assert(genTypeSize(elementType) * 2 == genTypeSize(varDsc));
                        if (offset == 0)
                        {
                            hwiNode = m_compiler->gtNewSimdGetLowerNode(elementType, lclNode, CORINFO_TYPE_FLOAT,
                                                                        genTypeSize(varDsc));
                        }
                        else
                        {
                            assert(offset == genTypeSize(elementType));
                            hwiNode = m_compiler->gtNewSimdGetUpperNode(elementType, lclNode, CORINFO_TYPE_FLOAT,
                                                                        genTypeSize(varDsc));
                        }

                        break;
                    }
                    default:
                        unreached();
                }

                indir = hwiNode;
                *use  = hwiNode;
            }
            break;

            case IndirTransform::WithElement:
            {
                GenTree*  hwiNode     = nullptr;
                var_types elementType = indir->TypeGet();
                GenTree*  simdLclNode = m_compiler->gtNewLclVarNode(lclNum);
                GenTree*  elementNode = indir->AsIndir()->Data();

                switch (elementType)
                {
                    case TYP_FLOAT:
                    {
                        // Handle case 1 or the float field of case 2
                        GenTree* indexNode = m_compiler->gtNewIconNode(offset / genTypeSize(elementType));
                        hwiNode =
                            m_compiler->gtNewSimdWithElementNode(varDsc->TypeGet(), simdLclNode, indexNode, elementNode,
                                                                 CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        break;
                    }

                    case TYP_SIMD12:
                    {
                        // Handle the Vector3 field of case 2
                        assert(varDsc->TypeGet() == TYP_SIMD16);

                        // We effectively inverse the operands here and take elementNode as the main value and
                        // simdLclNode[3] as the new value. This gives us a new TYP_SIMD16 with all elements in the
                        // right spots

                        elementNode = m_compiler->gtNewSimdHWIntrinsicNode(TYP_SIMD16, elementNode,
                                                                           NI_Vector128_AsVector128Unsafe,
                                                                           CORINFO_TYPE_FLOAT, 12);

                        GenTree* indexNode1 = m_compiler->gtNewIconNode(3, TYP_INT);
                        simdLclNode         = m_compiler->gtNewSimdGetElementNode(TYP_FLOAT, simdLclNode, indexNode1,
                                                                                  CORINFO_TYPE_FLOAT, 16);

                        GenTree* indexNode2 = m_compiler->gtNewIconNode(3, TYP_INT);
                        hwiNode = m_compiler->gtNewSimdWithElementNode(TYP_SIMD16, elementNode, indexNode2, simdLclNode,
                                                                       CORINFO_TYPE_FLOAT, 16);
                        break;
                    }

                    case TYP_SIMD8:
#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
                    case TYP_SIMD16:
                    case TYP_SIMD32:
#endif
                    {
                        // Handle case 3
                        assert(genTypeSize(elementType) * 2 == genTypeSize(varDsc));
                        if (offset == 0)
                        {
                            hwiNode = m_compiler->gtNewSimdWithLowerNode(varDsc->TypeGet(), simdLclNode, elementNode,
                                                                         CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        }
                        else
                        {
                            assert(offset == genTypeSize(elementType));
                            hwiNode = m_compiler->gtNewSimdWithUpperNode(varDsc->TypeGet(), simdLclNode, elementNode,
                                                                         CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        }

                        break;
                    }

                    default:
                        unreached();
                }

                indir->ChangeType(varDsc->TypeGet());
                indir->ChangeOper(GT_STORE_LCL_VAR);
                indir->AsLclVar()->SetLclNum(lclNum);
                indir->AsLclVar()->Data() = hwiNode;
                lclNode                   = indir->AsLclVarCommon();
            }
            break;
#endif // FEATURE_HW_INTRINSICS

            case IndirTransform::LclVar:
                // TODO-ADDR: use "BashToLclVar" here.
                if (indir->TypeGet() != varDsc->TypeGet())
                {
                    assert(genTypeSize(indir) == genTypeSize(varDsc)); // BOOL <-> UBYTE.
                    indir->ChangeType(varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc));
                }
                if (isDef)
                {
                    GenTree* value = indir->Data();
                    indir->ChangeOper(GT_STORE_LCL_VAR);
                    indir->AsLclVar()->Data() = value;
                }
                else
                {
                    indir->ChangeOper(GT_LCL_VAR);
                }
                indir->AsLclVar()->SetLclNum(lclNum);
                lclNode = indir->AsLclVarCommon();
                break;

            case IndirTransform::LclFld:
                if (isDef)
                {
                    GenTree* value = indir->Data();
                    indir->ChangeOper(GT_STORE_LCL_FLD);
                    indir->AsLclFld()->Data() = value;
                }
                else
                {
                    indir->ChangeOper(GT_LCL_FLD);
                }
                indir->AsLclFld()->SetLclNum(lclNum);
                indir->AsLclFld()->SetLclOffs(offset);
                indir->AsLclFld()->SetLayout(layout);
                lclNode = indir->AsLclVarCommon();

                if (!indir->TypeIs(TYP_STRUCT))
                {
                    // The general invariant in the compiler is that whoever creates a LCL_FLD node after local morph
                    // must mark the associated local DNER. We break this invariant here, for STRUCT fields, to allow
                    // global morph to transform these into enregisterable LCL_VARs, applying DNER otherwise.
                    m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                }
                break;

            default:
                unreached();
        }

        GenTreeFlags lclNodeFlags = GTF_EMPTY;

        if (isDef)
        {
            lclNodeFlags |= (indir->AsLclVarCommon()->Data()->gtFlags & GTF_ALL_EFFECT);
            lclNodeFlags |= (GTF_ASG | GTF_VAR_DEF);

            if (indir->IsPartialLclFld(m_compiler))
            {
                lclNodeFlags |= GTF_VAR_USEASG;
            }
        }

        lclNode->gtFlags = lclNodeFlags;
        m_stmtModified   = true;
    }

    //------------------------------------------------------------------------
    // SelectLocalIndirTransform: Select the transformation appropriate for an
    //    indirect access of a local variable.
    //
    // Arguments:
    //    indir  - the indirection node
    //    lclNum - the local's number
    //    offset - access' offset
    //    user   - indirection's user node
    //
    // Return Value:
    //    The transformation the caller should perform on this indirection.
    //
    IndirTransform SelectLocalIndirTransform(GenTreeIndir* indir, unsigned lclNum, unsigned offset, GenTree* user)
    {
        // We don't expect indirections that cannot be turned into local nodes here.
        assert((offset <= UINT16_MAX) && !indir->IsVolatile());

        bool isDef = indir->OperIs(GT_STOREIND, GT_STORE_BLK);
        if (!isDef && IsUnused(indir, user))
        {
            return IndirTransform::Nop;
        }

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        if (indir->TypeGet() != TYP_STRUCT)
        {
            if (indir->TypeGet() == varDsc->TypeGet())
            {
                return IndirTransform::LclVar;
            }

            // For small stores we can ignore the signed/unsigned diff.
            if (isDef && (varTypeToSigned(indir) == varTypeToSigned(varDsc)))
            {
                assert(varTypeIsSmall(indir));
                return IndirTransform::LclVar;
            }

            if (m_compiler->opts.OptimizationDisabled())
            {
                return IndirTransform::LclFld;
            }

#ifdef FEATURE_HW_INTRINSICS
            if (varTypeIsSIMD(varDsc))
            {
                // We have three cases we want to handle:
                // 1. Vector2/3/4 and Quaternion where we have 2-4x float fields
                // 2. Plane where we have 1x Vector3 and 1x float field
                // 3. Accesses of halves of larger SIMD types

                if (indir->TypeIs(TYP_FLOAT))
                {
                    if (((offset % genTypeSize(TYP_FLOAT)) == 0) && m_compiler->IsBaselineSimdIsaSupported())
                    {
                        return isDef ? IndirTransform::WithElement : IndirTransform::GetElement;
                    }
                }
                else if (indir->TypeIs(TYP_SIMD12))
                {
                    if ((offset == 0) && (varDsc->TypeGet() == TYP_SIMD16) && m_compiler->IsBaselineSimdIsaSupported())
                    {
                        return isDef ? IndirTransform::WithElement : IndirTransform::GetElement;
                    }
                }
#ifdef TARGET_ARM64
                else if (indir->TypeIs(TYP_SIMD8))
                {
                    if ((varDsc->TypeGet() == TYP_SIMD16) && ((offset % 8) == 0) &&
                        m_compiler->IsBaselineSimdIsaSupported())
                    {
                        return isDef ? IndirTransform::WithElement : IndirTransform::GetElement;
                    }
                }
#endif
#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
                else if (((indir->TypeIs(TYP_SIMD16) &&
                           m_compiler->compOpportunisticallyDependsOn(InstructionSet_AVX)) ||
                          (indir->TypeIs(TYP_SIMD32) &&
                           m_compiler->IsBaselineVector512IsaSupportedOpportunistically())) &&
                         (genTypeSize(indir) * 2 == genTypeSize(varDsc)) && ((offset % genTypeSize(indir)) == 0))
                {
                    return isDef ? IndirTransform::WithElement : IndirTransform::GetElement;
                }
#endif // FEATURE_SIMD && TARGET_XARCH
            }
#endif // FEATURE_HW_INTRINSICS

            if ((!isDef) && (offset == 0))
            {
                if (varTypeIsIntegral(indir) && varTypeIsIntegral(varDsc))
                {
                    return IndirTransform::NarrowCast;
                }

                if ((genTypeSize(indir) == genTypeSize(varDsc)) && (genTypeSize(indir) <= TARGET_POINTER_SIZE) &&
                    (varTypeIsFloating(indir) || varTypeIsFloating(varDsc)))
                {
                    return IndirTransform::BitCast;
                }
            }

            return IndirTransform::LclFld;
        }

        if (varDsc->TypeGet() != TYP_STRUCT)
        {
            return IndirTransform::LclFld;
        }

        if ((offset == 0) && ClassLayout::AreCompatible(indir->AsBlk()->GetLayout(), varDsc->GetLayout()))
        {
            return IndirTransform::LclVar;
        }

        return IndirTransform::LclFld;
    }

    //------------------------------------------------------------------------
    // MorphStructField: Reduces indirect access to a promoted local (e.g.
    //    IND(FIELD_ADDR(LCL_ADDR))) to a GT_LCL_VAR that references the field.
    //
    // Arguments:
    //    node - the GT_IND/GT_BLK node
    //    user - "node"'s user
    //
    // Return Value:
    //    Whether the indirection node was replaced with a local one.
    //
    bool MorphStructField(GenTreeIndir* node, GenTree* user)
    {
        GenTree* addr = node->Addr();
        if (node->IsVolatile() && (!addr->OperIs(GT_FIELD_ADDR) || ((addr->gtFlags & GTF_FLD_DEREFERENCED) == 0)))
        {
            // TODO-Bug: transforming volatile indirections like this is not legal. The above condition is a quirk.
            return false;
        }

        unsigned fieldLclNum = MorphStructFieldAddress(addr, node->Size());
        if (fieldLclNum == BAD_VAR_NUM)
        {
            return false;
        }

        LclVarDsc* fieldVarDsc = m_compiler->lvaGetDesc(fieldLclNum);
        var_types  fieldType   = fieldVarDsc->TypeGet();
        assert(fieldType != TYP_STRUCT); // Promoted LCL_VAR can't have a struct type.

        if (node->TypeGet() == fieldType)
        {
            if (node->OperIs(GT_STOREIND, GT_STORE_BLK))
            {
                GenTree* value = node->Data();
                node->ChangeOper(GT_STORE_LCL_VAR);
                node->AsLclVar()->Data() = value;
                node->gtFlags |= GTF_VAR_DEF;
            }
            else
            {
                node->ChangeOper(GT_LCL_VAR);
                node->gtFlags &= (GTF_NODE_MASK | GTF_DONT_CSE); // TODO-ASG-Cleanup: delete this zero-diff quirk.
            }
            node->AsLclVar()->SetLclNum(fieldLclNum);
            node->gtType = fieldType;

            return true;
        }

        return false;
    }

    //------------------------------------------------------------------------
    // MorphStructFieldAddress: Replace FIELD_ADDR(LCL_ADDR) with LCL_ADDR
    //    that references a promoted field.
    //
    // Arguments:
    //    node       - the address node
    //    accessSize - load/store size if known, zero otherwise
    //
    // Return Value:
    //    Local number for the promoted field if the replacement was successful,
    //    BAD_VAR_NUM otherwise.
    //
    unsigned MorphStructFieldAddress(GenTree* node, unsigned accessSize)
    {
        unsigned offset       = 0;
        bool     isSpanLength = false;
        GenTree* addr         = node;
        if (addr->OperIs(GT_FIELD_ADDR) && addr->AsFieldAddr()->IsInstance())
        {
            offset       = addr->AsFieldAddr()->gtFldOffset;
            isSpanLength = addr->AsFieldAddr()->IsSpanLength();
            addr         = addr->AsFieldAddr()->GetFldObj();
        }

        if (addr->IsLclVarAddr())
        {
            const LclVarDsc* varDsc = m_compiler->lvaGetDesc(addr->AsLclVarCommon());

            if (varDsc->lvPromoted)
            {
                unsigned fieldLclNum = m_compiler->lvaGetFieldLocal(varDsc, offset);
                if (fieldLclNum == BAD_VAR_NUM)
                {
                    // Access a promoted struct's field with an offset that doesn't correspond to any field.
                    // It can happen if the struct was cast to another struct with different offsets.
                    return BAD_VAR_NUM;
                }

                LclVarDsc* fieldVarDsc = m_compiler->lvaGetDesc(fieldLclNum);

                // Span's Length is never negative unconditionally
                if (isSpanLength && (accessSize == genTypeSize(TYP_INT)))
                {
                    fieldVarDsc->SetIsNeverNegative(true);
                }

                // Retargeting the indirection to reference the promoted field would make it "wide", exposing
                // the whole parent struct (with all of its fields).
                if (accessSize > genTypeSize(fieldVarDsc))
                {
                    return BAD_VAR_NUM;
                }

                JITDUMP("Replacing the field in promoted struct with local var V%02u\n", fieldLclNum);
                m_stmtModified = true;

                node->ChangeOper(GT_LCL_ADDR);
                node->AsLclFld()->SetLclNum(fieldLclNum);
                node->AsLclFld()->SetLclOffs(0);

                return fieldLclNum;
            }
        }

        return BAD_VAR_NUM;
    }

    //------------------------------------------------------------------------
    // MorphLocalField: Replaces a local field-based promoted struct field access
    //    with a GT_LCL_VAR/GT_STORE_LCL_VAR that references the struct field.
    //
    // Arguments:
    //    node - the GT_LCL_FLD/GT_STORE_LCL_FLD node
    //    user - the node that uses the field
    //
    // Notes:
    //    This does not do anything if the field access does not denote
    //    involved a promoted struct local.
    //    If the local field offset does not have a corresponding promoted struct
    //    field then no transformation is done and struct local's enregistration
    //    is disabled.
    //
    void MorphLocalField(GenTreeLclVarCommon* node, GenTree* user)
    {
        assert(node->OperIs(GT_LCL_FLD, GT_STORE_LCL_FLD));

        unsigned   lclNum = node->AsLclFld()->GetLclNum();
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        if (varDsc->lvPromoted)
        {
            unsigned fldOffset   = node->AsLclFld()->GetLclOffs();
            unsigned fieldLclNum = m_compiler->lvaGetFieldLocal(varDsc, fldOffset);

            if (fieldLclNum != BAD_VAR_NUM)
            {
                LclVarDsc* fldVarDsc = m_compiler->lvaGetDesc(fieldLclNum);
                var_types  fieldType = fldVarDsc->TypeGet();

                if (node->TypeGet() == fieldType)
                {
                    // There is an existing sub-field we can use.
                    node->SetLclNum(fieldLclNum);

                    if (node->OperIs(GT_STORE_LCL_FLD))
                    {
                        node->SetOper(GT_STORE_LCL_VAR);
                        node->gtFlags &= ~GTF_VAR_USEASG;
                    }
                    else
                    {
                        node->SetOper(GT_LCL_VAR);
                    }

                    JITDUMP("Replacing the GT_LCL_FLD in promoted struct with local var V%02u\n", fieldLclNum);
                }
            }
        }

        // If we haven't replaced the field, make sure to set DNER on the local.
        if (!node->OperIsScalarLocal())
        {
            m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        else
        {
            m_stmtModified = true;
        }
    }

    //-----------------------------------------------------------------------------------
    // HandleLocalStoreAssertions:
    //   Handle clearing and generating assertions for a local store with the
    //   specified data value.
    //
    // Argument:
    //    store - The local store
    //    data  - Value representing data
    //
    void HandleLocalStoreAssertions(GenTreeLclVarCommon* store, Value& data)
    {
        m_lclAddrAssertions->Clear(store->GetLclNum());

        if (data.IsAddress() && store->OperIs(GT_STORE_LCL_VAR))
        {
            LclVarDsc* dsc = m_compiler->lvaGetDesc(store);
            // TODO-CQ: We currently don't handle promoted fields, but that has
            // no impact since practically all promoted structs end up with
            // lvHasLdAddrOp set.
            if (!dsc->lvPromoted && !dsc->lvIsStructField && !dsc->lvHasLdAddrOp)
            {
                m_lclAddrAssertions->Record(store->GetLclNum(), data.LclNum(), data.Offset());
            }
        }
    }

    //-----------------------------------------------------------------------------------
    // HandleLocalAssertions:
    //   Try to refine the specified "addr" value based on assertions about a specified local.
    //
    // Argument:
    //    lcl   - The local node
    //    value - Value of local; will be modified to be an address if an assertion could be found
    //
    void HandleLocalAssertions(GenTreeLclVarCommon* lcl, Value& value)
    {
        assert(lcl->OperIs(GT_LCL_VAR));
        if (!lcl->TypeIs(TYP_I_IMPL, TYP_BYREF))
        {
            return;
        }

        const LocalEqualsLocalAddrAssertion* assertion = m_lclAddrAssertions->GetCurrentAssertion(lcl->GetLclNum());
        if (assertion != nullptr)
        {
            JITDUMP("Using assertion ");
            DBEXEC(VERBOSE, assertion->Print());
            JITDUMP("\n");
            value.Address(assertion->AddressLclNum, assertion->AddressOffset);
            m_propagatedAddrs = true;
        }
    }

public:
    //------------------------------------------------------------------------
    // UpdateEarlyRefCount: updates the ref count for locals
    //
    // Arguments:
    //    lclNum - the local number to update the count for.
    //    node   - local node representing the reference
    //    user   - "node"'s user
    //
    // Notes:
    //    fgRetypeImplicitByRefArgs checks the ref counts when it decides to undo promotions.
    //    fgForwardSub may use ref counts to decide when to forward sub.
    //
    void UpdateEarlyRefCount(unsigned lclNum, GenTree* node, GenTree* user)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        // Note we don't need accurate counts when the values are large.
        //
        varDsc->incLvRefCntSaturating(1, RCS_EARLY);

        if (!m_compiler->lvaIsImplicitByRefLocal(lclNum))
        {
            return;
        }

        // See if this struct is an argument to a call. This information is recorded
        // via the weighted early ref count for the local, and feeds the undo promotion
        // heuristic.
        //
        // It can be approximate, so the pattern match below need not be exhaustive.
        // But the pattern should at least subset the implicit byref cases that are
        // handled in fgCanFastTailCall and fgMakeOutgoingStructArgCopy.
        //
        if ((node != nullptr) && node->OperIs(GT_LCL_VAR) && (user != nullptr) && user->IsCall())
        {
            JITDUMP("LocalAddressVisitor incrementing weighted ref count from " FMT_WT " to " FMT_WT
                    " for implicit by-ref V%02d arg passed to call\n",
                    varDsc->lvRefCntWtd(RCS_EARLY), varDsc->lvRefCntWtd(RCS_EARLY) + 1, lclNum);
            varDsc->incLvRefCntWtd(1, RCS_EARLY);
        }
    }

private:
    //------------------------------------------------------------------------
    // IsUnused: is the given node unused?
    //
    // Arguments:
    //    node - the node in question
    //    user - "node"'s user
    //
    // Return Value:
    //    If "node" is a root of the statement, or the first operand of a comma,
    //    "true", otherwise, "false".
    //
    static bool IsUnused(GenTree* node, GenTree* user)
    {
        return (user == nullptr) || (user->OperIs(GT_COMMA) && (user->AsOp()->gtGetOp1() == node));
    }

    void SequenceLocal(GenTreeLclVarCommon* lcl)
    {
        if (m_sequencer != nullptr)
        {
            m_sequencer->SequenceLocal(lcl);
        }
    }

    void SequenceCall(GenTreeCall* call)
    {
        if (m_sequencer != nullptr)
        {
            m_sequencer->SequenceCall(call);
        }
    }
};

//------------------------------------------------------------------------
// fgMarkAddressExposedLocals: Traverses the entire method and marks address
//    exposed locals.
//
// Returns:
//    Suitable phase status
//
// Notes:
//    Trees such as IND(ADDR(LCL_VAR)), that morph is expected to fold
//    to just LCL_VAR, do not result in the involved local being marked
//    address exposed.
//
PhaseStatus Compiler::fgMarkAddressExposedLocals()
{
    bool madeChanges = false;

    if (opts.OptimizationDisabled())
    {
        LocalAddressVisitor visitor(this, nullptr, nullptr);
        for (BasicBlock* const block : Blocks())
        {
            visitor.VisitBlock(block);
        }

        madeChanges = visitor.MadeChanges();
    }
    else
    {
        LocalEqualsLocalAddrAssertions  assertions(this);
        LocalEqualsLocalAddrAssertions* pAssertions = &assertions;

#ifdef DEBUG
        static ConfigMethodRange s_range;
        s_range.EnsureInit(JitConfig.JitEnableLocalAddrPropagationRange());
        if (!s_range.Contains(info.compMethodHash()))
        {
            pAssertions = nullptr;
        }
#endif

        LocalSequencer      sequencer(this);
        LocalAddressVisitor visitor(this, &sequencer, pAssertions);
        for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
        {
            visitor.VisitBlock(m_dfsTree->GetPostOrder(i - 1));
        }

        madeChanges = visitor.MadeChanges();

        madeChanges |= fgExposeUnpropagatedLocals(visitor.PropagatedAnyAddresses(), &assertions);
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#ifdef FEATURE_SIMD
//-----------------------------------------------------------------------------------
// fgMorphCombineSIMDFieldStores:
//    If the store of the input stmt is a read for simd vector X Field, then this
//    function will keep reading next few stmts based on the vector size(2, 3, 4).
//    If the next stmts stores are located contiguous and values are also located
//    contiguous, then we replace those statements with one store.
//
// Argument:
//    block - BasicBlock*. block which stmt belongs to
//    stmt  - Statement*. the stmt node we want to check
//
// Return Value:
//    Whether the stores were successfully coalesced.
//
bool Compiler::fgMorphCombineSIMDFieldStores(BasicBlock* block, Statement* stmt)
{
    GenTree* store = stmt->GetRootNode();
    assert(store->OperIsStore());

    GenTree*  prevValue    = store->Data();
    unsigned  index        = 0;
    var_types simdBaseType = store->TypeGet();
    unsigned  simdSize     = 0;
    GenTree*  simdLclAddr  = getSIMDStructFromField(prevValue, &index, &simdSize, true);

    if ((simdLclAddr == nullptr) || (index != 0) || (simdBaseType != TYP_FLOAT))
    {
        // if the value is not from a SIMD vector field X, then there is no need to check further.
        return false;
    }

    var_types  simdType        = getSIMDTypeForSize(simdSize);
    int        storeCount      = simdSize / genTypeSize(simdBaseType) - 1;
    int        remainingStores = storeCount;
    GenTree*   prevStore       = store;
    Statement* curStmt         = stmt->GetNextStmt();
    Statement* lastStmt        = stmt;

    while (curStmt != nullptr && remainingStores > 0)
    {
        if (!curStmt->GetRootNode()->OperIsStore())
        {
            break;
        }

        GenTree* curStore = curStmt->GetRootNode();
        GenTree* curValue = curStore->Data();

        if (!areArgumentsContiguous(prevStore, curStore) || !areArgumentsContiguous(prevValue, curValue))
        {
            break;
        }

        remainingStores--;
        prevStore = curStore;
        prevValue = curValue;

        lastStmt = curStmt;
        curStmt  = curStmt->GetNextStmt();
    }

    if (remainingStores > 0)
    {
        // if the left store number is bigger than zero, then this means that the stores
        // are not assigning to the contiguous memory locations from same vector.
        return false;
    }

    JITDUMP("\nFound contiguous stores from a SIMD vector to memory.\n");
    JITDUMP("From " FMT_BB ", " FMT_STMT " to " FMT_STMT "\n", block->bbNum, stmt->GetID(), lastStmt->GetID());

    for (int i = 0; i < storeCount; i++)
    {
        fgRemoveStmt(block, stmt->GetNextStmt());
    }

    GenTree* fullValue = gtNewLclvNode(simdLclAddr->AsLclVarCommon()->GetLclNum(), simdType);
    GenTree* fullStore;
    if (store->OperIs(GT_STORE_LCL_FLD))
    {
        store->gtType             = simdType;
        store->AsLclFld()->Data() = fullValue;
        if (!store->IsPartialLclFld(this))
        {
            store->gtFlags &= ~GTF_VAR_USEASG;
        }

        fullStore = store;
    }
    else
    {
        GenTree* dstAddr = CreateAddressNodeForSimdHWIntrinsicCreate(store, simdBaseType, simdSize);
        fullStore        = gtNewStoreIndNode(simdType, dstAddr, fullValue);
    }

    JITDUMP("\n" FMT_BB " " FMT_STMT " (before):\n", block->bbNum, stmt->GetID());
    DISPSTMT(stmt);

    stmt->SetRootNode(fullStore);

    JITDUMP("\nReplaced " FMT_BB " " FMT_STMT " (after):\n", block->bbNum, stmt->GetID());
    DISPSTMT(stmt);

    return true;
}
#endif // FEATURE_SIMD

//-----------------------------------------------------------------------------------
// fgExposeUnpropagatedLocals:
//   Expose the final set of locals that were computed to have their address
//   taken. Deletes LCL_ADDR nodes used as the data source of a store if the
//   destination can be proven to not be read, and avoid exposing these if
//   possible.
//
// Argument:
//    propagatedAny - Whether any LCL_ADDR values were propagated
//    assertions    - Data structure tracking LCL_ADDR assertions
//
// Return Value:
//    True if any changes were made to the IR; otherwise false.
//
bool Compiler::fgExposeUnpropagatedLocals(bool propagatedAny, LocalEqualsLocalAddrAssertions* assertions)
{
    if (!propagatedAny)
    {
        fgExposeLocalsInBitVec(assertions->GetLocalsToExpose());
        return false;
    }

    BitVecTraits localsTraits(lvaCount, this);
    BitVec       unreadLocals = assertions->GetLocalsWithAssertions();

    struct Store
    {
        struct Statement*    Statement;
        GenTreeLclVarCommon* Tree;
    };

    ArrayStack<Store> stores(getAllocator(CMK_LocalAddressVisitor));

    for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_dfsTree->GetPostOrder(i - 1);

        for (Statement* stmt : block->Statements())
        {
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                if (!BitVecOps::IsMember(&localsTraits, unreadLocals, lcl->GetLclNum()))
                {
                    continue;
                }

                if (lcl->OperIs(GT_STORE_LCL_VAR, GT_STORE_LCL_FLD))
                {
                    if (lcl->TypeIs(TYP_I_IMPL, TYP_BYREF) && ((lcl->Data()->gtFlags & GTF_SIDE_EFFECT) == 0))
                    {
                        stores.Push({stmt, lcl});
                    }
                }
                else
                {
                    BitVecOps::RemoveElemD(&localsTraits, unreadLocals, lcl->GetLclNum());
                }
            }
        }
    }

    if (BitVecOps::IsEmpty(&localsTraits, unreadLocals))
    {
        JITDUMP("No destinations of propagated LCL_ADDR nodes are unread\n");
        fgExposeLocalsInBitVec(assertions->GetLocalsToExpose());
        return false;
    }

    bool changed = false;
    for (int i = 0; i < stores.Height(); i++)
    {
        const Store& store = stores.BottomRef(i);
        assert(store.Tree->TypeIs(TYP_I_IMPL, TYP_BYREF));

        if (BitVecOps::IsMember(&localsTraits, unreadLocals, store.Tree->GetLclNum()))
        {
            JITDUMP("V%02u is unread; removing store data of [%06u]\n", store.Tree->GetLclNum(), dspTreeID(store.Tree));
            DISPTREE(store.Tree);

            store.Tree->Data()->BashToConst(0, store.Tree->Data()->TypeGet());
            fgSequenceLocals(store.Statement);

            JITDUMP("\nResult:\n");
            DISPTREE(store.Tree);
            JITDUMP("\n");
            changed = true;
        }
    }

    if (changed)
    {
        // Finally compute new set of exposed locals.
        BitVec exposedLocals(BitVecOps::MakeEmpty(&localsTraits));

        for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
        {
            BasicBlock* block = m_dfsTree->GetPostOrder(i - 1);

            for (Statement* stmt : block->Statements())
            {
                for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                {
                    if (!lcl->OperIs(GT_LCL_ADDR))
                    {
                        continue;
                    }

                    LclVarDsc* lclDsc        = lvaGetDesc(lcl);
                    unsigned   exposedLclNum = lclDsc->lvIsStructField ? lclDsc->lvParentLcl : lcl->GetLclNum();
                    BitVecOps::AddElemD(&localsTraits, exposedLocals, exposedLclNum);
                }
            }
        }

        auto dumpVars = [=, &localsTraits](BitVec_ValArg_T vec, BitVec_ValArg_T other) {
            const char* sep = "";
            for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
            {
                if (BitVecOps::IsMember(&localsTraits, vec, lclNum))
                {
                    JITDUMP("%sV%02u", sep, lclNum);
                    sep = " ";
                }
                else if (BitVecOps::IsMember(&localsTraits, other, lclNum))
                {
                    JITDUMP("%s   ", sep);
                    sep = " ";
                }
            }
        };

        // TODO-CQ: Instead of intersecting here, we should teach the above
        // logic about retbuf LCL_ADDRs not leading to exposure. This should
        // allow us to assert that exposedLocals is a subset of
        // assertions->GetLocalsToExpose().
        BitVecOps::IntersectionD(&localsTraits, exposedLocals, assertions->GetLocalsToExpose());

        JITDUMP("Old exposed set: ");
        dumpVars(assertions->GetLocalsToExpose(), exposedLocals);
        JITDUMP("\nNew exposed set: ");
        dumpVars(exposedLocals, assertions->GetLocalsToExpose());
        JITDUMP("\n");

        fgExposeLocalsInBitVec(exposedLocals);
    }

    return changed;
}

//-----------------------------------------------------------------------------------
// fgExposeLocalsInBitVec:
//   Mark all locals in the bit vector as address exposed.
//
void Compiler::fgExposeLocalsInBitVec(BitVec_ValArg_T bitVec)
{
    BitVecTraits    localsTraits(lvaCount, this);
    BitVecOps::Iter iter(&localsTraits, bitVec);
    unsigned        lclNum;
    while (iter.NextElem(&lclNum))
    {
        lvaSetVarAddrExposed(lclNum DEBUGARG(AddressExposedReason::ESCAPE_ADDRESS));
    }
}
