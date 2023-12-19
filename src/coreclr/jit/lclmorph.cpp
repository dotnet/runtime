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

    LocalSequencer(Compiler* comp) : GenTreeVisitor(comp), m_prevNode(nullptr)
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

    ArrayStack<Value> m_valueStack;
    bool              m_stmtModified;
    bool              m_madeChanges;
    LocalSequencer*   m_sequencer;

public:
    enum
    {
        DoPreOrder        = true,
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    LocalAddressVisitor(Compiler* comp, LocalSequencer* sequencer)
        : GenTreeVisitor<LocalAddressVisitor>(comp)
        , m_valueStack(comp->getAllocator(CMK_LocalAddressVisitor))
        , m_stmtModified(false)
        , m_madeChanges(false)
        , m_sequencer(sequencer)
    {
    }

    bool MadeChanges() const
    {
        return m_madeChanges;
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
                assert(TopValue(0).Node() == node->AsLclVarCommon()->Data());
                EscapeValue(TopValue(0), node);
                PopValue();
                FALLTHROUGH;

            case GT_LCL_VAR:
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
                assert(TopValue(0).Node() == node->AsIndir()->Data());
                EscapeValue(TopValue(0), node);
                PopValue();
                FALLTHROUGH;

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
            IsValidLclAddr(lclNum, val.Offset()))
        {
            // We will only attempt this optimization for locals that are:
            // a) Not susceptible to liveness bugs (see "lvaSetHiddenBufferStructArg").
            // b) Do not later turn into indirections.
            //
            bool isSuitableLocal =
                varTypeIsStruct(varDsc) && varDsc->lvIsTemp && !m_compiler->lvaIsImplicitByRefLocal(lclNum);
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
            m_compiler->lvaSetVarAddrExposed(
                varDsc->lvIsStructField ? varDsc->lvParentLcl : lclNum DEBUGARG(AddressExposedReason::ESCAPE_ADDRESS));
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

                if ((varDsc->TypeGet() == TYP_STRUCT) && varDsc->GetLayout()->IsBlockLayout())
                {
                    // TODO-CQ: TYP_BLK used to always be exposed here. This is in principle not necessary, but
                    // not doing so would require VN changes. For now, exposing gets better CQ as otherwise the
                    // variable ends up untracked and VN treats untracked-not-exposed locals more conservatively
                    // than exposed ones.
                    m_compiler->lvaSetVarAddrExposed(lclNum DEBUGARG(AddressExposedReason::TOO_CONSERVATIVE));
                }
            }
        }

        if (isWide)
        {
            m_compiler->lvaSetVarAddrExposed(
                varDsc->lvIsStructField ? varDsc->lvParentLcl : lclNum DEBUGARG(AddressExposedReason::WIDE_INDIR));

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
        assert(m_compiler->lvaVarAddrExposed(lclNum) || m_compiler->lvaGetDesc(lclNum)->IsHiddenBufferStructArg());

        if (IsValidLclAddr(lclNum, offset))
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
            // We have two cases we want to handle:
            // 1. Vector2/3/4 and Quaternion where we have 4x float fields
            // 2. Plane where we have 1x Vector3 and 1x float field

            case IndirTransform::GetElement:
            {
                GenTree*  hwiNode     = nullptr;
                var_types elementType = indir->TypeGet();
                lclNode               = indir->gtGetOp1()->BashToLclVar(m_compiler, lclNum);

                switch (elementType)
                {
                    case TYP_FLOAT:
                    {
                        GenTree* indexNode = m_compiler->gtNewIconNode(offset / genTypeSize(elementType));
                        hwiNode            = m_compiler->gtNewSimdGetElementNode(elementType, lclNode, indexNode,
                                                                      CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        break;
                    }
                    case TYP_SIMD12:
                    {
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
                        GenTree* indexNode = m_compiler->gtNewIconNode(offset / genTypeSize(elementType));
                        hwiNode =
                            m_compiler->gtNewSimdWithElementNode(varDsc->TypeGet(), simdLclNode, indexNode, elementNode,
                                                                 CORINFO_TYPE_FLOAT, genTypeSize(varDsc));
                        break;
                    }
                    case TYP_SIMD12:
                    {
                        assert(varDsc->TypeGet() == TYP_SIMD16);

                        // We inverse the operands here and take elementNode as the main value and simdLclNode[3] as the
                        // new value. This gives us a new TYP_SIMD16 with all elements in the right spots
                        GenTree* indexNode = m_compiler->gtNewIconNode(3, TYP_INT);
                        hwiNode = m_compiler->gtNewSimdWithElementNode(TYP_SIMD16, elementNode, indexNode, simdLclNode,
                                                                       CORINFO_TYPE_FLOAT, 16);
                        break;
                    }
                    case TYP_SIMD8:
#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
                    case TYP_SIMD16:
                    case TYP_SIMD32:
#endif
                    {
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
                    GenTree* data = indir->Data();
                    indir->ChangeOper(GT_STORE_LCL_VAR);
                    indir->AsLclVar()->Data() = data;
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
                    GenTree* data = indir->Data();
                    indir->ChangeOper(GT_STORE_LCL_FLD);
                    indir->AsLclFld()->Data() = data;
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
                // 1. Vector2/3/4 and Quaternion where we have 4x float fields
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
                GenTree* data = node->Data();
                node->ChangeOper(GT_STORE_LCL_VAR);
                node->AsLclVar()->Data() = data;
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
    // IsValidLclAddr: Can the given local address be represented as "LCL_FLD_ADDR"?
    //
    // Local address nodes cannot point beyond the local and can only store
    // 16 bits worth of offset.
    //
    // Arguments:
    //    lclNum - The local's number
    //    offset - The address' offset
    //
    // Return Value:
    //    Whether "LCL_FLD_ADDR<lclNum> [+offset]" would be valid IR.
    //
    bool IsValidLclAddr(unsigned lclNum, unsigned offset) const
    {
        return (offset < UINT16_MAX) && (offset < m_compiler->lvaLclExactSize(lclNum));
    }

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
    bool                madeChanges = false;
    LocalSequencer      sequencer(this);
    LocalAddressVisitor visitor(this, opts.OptimizationEnabled() ? &sequencer : nullptr);

    for (BasicBlock* const block : Blocks())
    {
        // Make the current basic block address available globally
        compCurBB = block;

        for (Statement* const stmt : block->Statements())
        {
#ifdef FEATURE_SIMD
            if (opts.OptimizationEnabled() && stmt->GetRootNode()->TypeIs(TYP_FLOAT) &&
                stmt->GetRootNode()->OperIsStore())
            {
                madeChanges |= fgMorphCombineSIMDFieldStores(block, stmt);
            }
#endif

            visitor.VisitStmt(stmt);
        }

        // We could check for GT_JMP inside the visitor, but this node is very
        // rare so keeping it here avoids pessimizing the hot code.
        if (block->endsWithJmpMethod(this))
        {
            // GT_JMP has implicit uses of all arguments.
            for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
            {
                visitor.UpdateEarlyRefCount(lclNum, nullptr, nullptr);
            }
        }
    }

    madeChanges |= visitor.MadeChanges();

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
