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
            // In the rare case that the root node becomes part of the linked
            // list (i.e. top level local) we get a circular linked list here.
            if (firstNode == rootNode)
            {
                assert(firstNode == lastNode);
                lastNode->gtNext = nullptr;
            }
            else
            {
                assert(lastNode->gtNext == nullptr);
                assert(lastNode->OperIsLocal() || lastNode->OperIsLocalAddr());
            }

            firstNode->gtPrev = nullptr;
        }

        stmt->SetTreeList(firstNode);
        stmt->SetTreeListEnd(lastNode);
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* node = *use;
        if (node->OperIsLocal() || node->OperIsLocalAddr())
        {
            SequenceLocal(node->AsLclVarCommon());
        }

        if (node->OperIs(GT_ASG))
        {
            SequenceAssignment(node->AsOp());
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
        lcl->gtNext        = nullptr;
        m_prevNode->gtNext = lcl;
        m_prevNode         = lcl;
    }

    //-------------------------------------------------------------------
    // SequenceAssignment: Post-process an assignment that may have a local on the LHS.
    //
    // Arguments:
    //     asg - the assignment
    //
    // Remarks:
    //     In execution order the LHS of an assignment is normally visited
    //     before the RHS. However, for our purposes, we would like to see the
    //     LHS local which is considered the def after the nodes on the RHS, so
    //     this function corrects where that local appears in the list.
    //
    //     This is handled in later liveness by guaranteeing GTF_REVERSE_OPS is
    //     set for assignments with tracked locals on the LHS.
    //
    void SequenceAssignment(GenTreeOp* asg)
    {
        if (asg->gtGetOp1()->OperIsLocal())
        {
            // Correct the point at which the definition of the local on the LHS appears.
            MoveNodeToEnd(asg->gtGetOp1());
        }
    }

    //-------------------------------------------------------------------
    // SequenceCall: Post-process a call that may define a local.
    //
    // Arguments:
    //     call - the call
    //
    // Remarks:
    //     Like above, but calls may also define a local that we would like to
    //     see after all other operands of the call have been evaluated.
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
    void MoveNodeToEnd(GenTree* node)
    {
        if (node->gtNext == nullptr)
        {
            return;
        }

        assert(m_prevNode != node);

        GenTree* prev = node->gtPrev;
        GenTree* next = node->gtNext;

        assert(prev != nullptr); // Should have sentinel always, even as the first local.
        prev->gtNext = next;
        next->gtPrev = prev;

        m_prevNode->gtNext = node;
        node->gtPrev       = m_prevNode;
        node->gtNext       = nullptr;
        m_prevNode         = node;
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
    //   - the memory location associated with a local variable, including an offset
    //     accumulated from GT_LCL_FLD and GT_FIELD nodes.
    //   - the address of local variable memory location, including an offset as well.
    //   - an unknown value - the result of a node we don't know how to process. This
    //     also includes the result of TYP_VOID nodes (or any other nodes that don't
    //     actually produce values in IR) in order to support the invariant that every
    //     node produces a value.
    //
    // Each value is processed ("escaped") when visiting (in post-order) its parent,
    // to achieve uniformity between how address and location values are handled.
    //
    class Value
    {
        GenTree** m_use;
        unsigned  m_lclNum;
        unsigned  m_offset;
        bool      m_address;
        INDEBUG(bool m_consumed);

    public:
        // Produce an unknown value associated with the specified node.
        Value(GenTree** use)
            : m_use(use)
            , m_lclNum(BAD_VAR_NUM)
            , m_offset(0)
            , m_address(false)
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

        // Does this value represent a location?
        bool IsLocation() const
        {
            return (m_lclNum != BAD_VAR_NUM) && !m_address;
        }

        // Does this value represent the address of a location?
        bool IsAddress() const
        {
            assert((m_lclNum != BAD_VAR_NUM) || !m_address);

            return m_address;
        }

        // Get the location's variable number.
        unsigned LclNum() const
        {
            assert(IsLocation() || IsAddress());

            return m_lclNum;
        }

        // Get the location's byte offset.
        unsigned Offset() const
        {
            assert(IsLocation() || IsAddress());

            return m_offset;
        }

        //------------------------------------------------------------------------
        // Location: Produce a location value.
        //
        // Arguments:
        //    lclVar - a GT_LCL_VAR node that defines the location
        //
        // Notes:
        //   - (lclnum) => LOCATION(lclNum, 0)
        //
        void Location(GenTreeLclVar* lclVar)
        {
            assert(lclVar->OperIs(GT_LCL_VAR));
            assert(!IsLocation() && !IsAddress());

            m_lclNum = lclVar->GetLclNum();

            assert(m_offset == 0);
        }

        //------------------------------------------------------------------------
        // Address: Produce an address value from a GT_LCL_VAR_ADDR node.
        //
        // Arguments:
        //    lclVar - a GT_LCL_VAR_ADDR node that defines the address
        //
        // Notes:
        //   - (lclnum) => ADDRESS(lclNum, 0)
        //
        void Address(GenTreeLclVar* lclVar)
        {
            assert(lclVar->OperIs(GT_LCL_VAR_ADDR));
            assert(!IsLocation() && !IsAddress());

            m_lclNum  = lclVar->GetLclNum();
            m_address = true;

            assert(m_offset == 0);
        }

        //------------------------------------------------------------------------
        // Location: Produce a location value.
        //
        // Arguments:
        //    lclFld - a GT_LCL_FLD node that defines the location
        //
        // Notes:
        //   - (lclnum, lclOffs) => LOCATION(lclNum, offset)
        //
        void Location(GenTreeLclFld* lclFld)
        {
            assert(lclFld->OperIs(GT_LCL_FLD));
            assert(!IsLocation() && !IsAddress());

            m_lclNum = lclFld->GetLclNum();
            m_offset = lclFld->GetLclOffs();
        }

        //------------------------------------------------------------------------
        // Address: Produce an address value from a LCL_FLD_ADDR node.
        //
        // Arguments:
        //    lclFld - a GT_LCL_FLD_ADDR node that defines the address
        //
        // Notes:
        //   - (lclnum, lclOffs) => ADDRESS(lclNum, offset)
        //
        void Address(GenTreeLclFld* lclFld)
        {
            assert(lclFld->OperIs(GT_LCL_FLD_ADDR));
            assert(!IsLocation() && !IsAddress());

            m_lclNum  = lclFld->GetLclNum();
            m_offset  = lclFld->GetLclOffs();
            m_address = true;
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
        //   - LOCATION(lclNum, offset) => not representable, must escape
        //   - ADDRESS(lclNum, offset) => ADDRESS(lclNum, offset + offs)
        //   - UNKNOWN => UNKNOWN
        //
        bool AddOffset(Value& val, unsigned addOffset)
        {
            assert(!IsAddress() && !IsLocation());

            if (val.IsLocation())
            {
                return false;
            }

            if (val.IsAddress())
            {
                ClrSafeInt<unsigned> newOffset = ClrSafeInt<unsigned>(val.m_offset) + ClrSafeInt<unsigned>(addOffset);

                if (newOffset.IsOverflow())
                {
                    return false;
                }

                m_lclNum  = val.m_lclNum;
                m_offset  = newOffset.Value();
                m_address = true;
            }

            INDEBUG(val.Consume();)
            return true;
        }

        //------------------------------------------------------------------------
        // Indir: Produce a location value from an address value.
        //
        // Arguments:
        //    val       - the input value
        //    addOffset - the offset to add
        //
        // Return Value:
        //    `true` if the value was consumed. `false` if the input value
        //    cannot be consumed because it is itsef a location or because
        //    the offset overflowed. In this case the caller is expected
        //    to escape the input value.
        //
        // Notes:
        //   - LOCATION(lclNum, offset) => not representable, must escape
        //   - ADDRESS(lclNum, offset) => LOCATION(lclNum, offset + addOffset)
        //     if the offset overflows then location is not representable, must escape
        //   - UNKNOWN => UNKNOWN
        //
        bool Indir(Value& val, unsigned addOffset = 0)
        {
            assert(!IsLocation() && !IsAddress());

            if (AddOffset(val, addOffset))
            {
                m_address = false;
                return true;
            }

            return false;
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
        None,
        Nop,
        BitCast,
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
        ComputeStack      = true,
        DoLclVarsOnly     = false,
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

        // We could have something a statement like IND(ADDR(LCL_VAR)) so we need to escape
        // the location here. This doesn't seem to happen often, if ever. The importer
        // tends to wrap such a tree in a COMMA.
        if (TopValue(0).IsLocation())
        {
            EscapeLocation(TopValue(0), nullptr);
        }
        else
        {
            // If we have an address on the stack then we don't need to do anything.
            // The address tree isn't actually used and it will be discarded during
            // morphing. So just mark any value as consumed to keep PopValue happy.
            INDEBUG(TopValue(0).Consume();)
        }

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

        if (node->OperIs(GT_IND, GT_FIELD, GT_FIELD_ADDR))
        {
            MorphStructField(node, user);
        }
        else if (node->OperIs(GT_LCL_FLD))
        {
            MorphLocalField(node, user);
        }

        if (node->OperIsLocal() || node->OperIsLocalAddr())
        {
            unsigned const   lclNum = node->AsLclVarCommon()->GetLclNum();
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            UpdateEarlyRefCount(lclNum);

            if (varDsc->lvIsStructField)
            {
                // Promoted field, increase count for the parent lclVar.
                //
                assert(!m_compiler->lvaIsImplicitByRefLocal(lclNum));
                unsigned parentLclNum = varDsc->lvParentLcl;
                UpdateEarlyRefCount(parentLclNum);
            }

            if (varDsc->lvPromoted)
            {
                // Promoted struct, increase count for each promoted field.
                //
                for (unsigned childLclNum = varDsc->lvFieldLclStart;
                     childLclNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++childLclNum)
                {
                    UpdateEarlyRefCount(childLclNum);
                }
            }
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
            case GT_LCL_VAR:
                assert(TopValue(0).Node() == node);

                TopValue(0).Location(node->AsLclVar());
                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_LCL_VAR_ADDR:
                assert(TopValue(0).Node() == node);

                TopValue(0).Address(node->AsLclVar());
                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_LCL_FLD:
                assert(TopValue(0).Node() == node);

                TopValue(0).Location(node->AsLclFld());
                SequenceLocal(node->AsLclVarCommon());
                break;

            case GT_LCL_FLD_ADDR:
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

            case GT_FIELD_ADDR:
                if (node->AsField()->IsInstance())
                {
                    assert(TopValue(1).Node() == node);
                    assert(TopValue(0).Node() == node->AsField()->GetFldObj());

                    if (!TopValue(1).AddOffset(TopValue(0), node->AsField()->gtFldOffset))
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

            case GT_FIELD:
                if (node->AsField()->IsInstance())
                {
                    assert(TopValue(1).Node() == node);
                    assert(TopValue(0).Node() == node->AsField()->GetFldObj());

                    if (node->AsField()->IsVolatile())
                    {
                        // Volatile indirections must not be removed so the address, if any, must be escaped.
                        EscapeValue(TopValue(0), node);
                    }
                    else if (!TopValue(1).Indir(TopValue(0), node->AsField()->gtFldOffset))
                    {
                        // Either the address comes from a location value (e.g. FIELD(IND(...))) or the field
                        // offset has overflowed.
                        EscapeValue(TopValue(0), node);
                    }

                    PopValue();
                }
                else
                {
                    assert(TopValue(0).Node() == node);
                }
                break;

            case GT_OBJ:
            case GT_BLK:
            case GT_IND:
                assert(TopValue(1).Node() == node);
                assert(TopValue(0).Node() == node->gtGetOp1());

                if (node->AsIndir()->IsVolatile())
                {
                    // Volatile indirections must not be removed so the address, if any, must be escaped.
                    EscapeValue(TopValue(0), node);
                }
                else if (!TopValue(1).Indir(TopValue(0)))
                {
                    // If the address comes from another indirection (e.g. IND(IND(...))
                    // then we need to escape the location.
                    EscapeLocation(TopValue(0), node);
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

            case GT_ASG:
                EscapeValue(TopValue(0), node);
                PopValue();
                EscapeValue(TopValue(0), node);
                PopValue();
                assert(TopValue(0).Node() == node);

                SequenceAssignment(node->AsOp());
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

        assert(TopValue(0).Node() == node);
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
        if (val.IsLocation())
        {
            EscapeLocation(val, user);
        }
        else if (val.IsAddress())
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
                    (varDsc->lvExactSize != m_compiler->typGetObjLayout(callUser->gtRetClsHnd)->GetSize()))
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
    // EscapeLocation: Process an escaped location value
    //
    // Arguments:
    //    val - the escaped location value
    //    user - the node that uses the location value
    //
    // Notes:
    //    Unlike EscapeAddress, this does not necessarily mark the lclvar associated
    //    with the value as address exposed. This is needed only if the indirection
    //    is wider than the lclvar.
    //
    void EscapeLocation(Value& val, GenTree* user)
    {
        assert(val.IsLocation());

        GenTree* node = val.Node();

        if (node->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            // If the location is accessed directly then we don't need to do anything.
            assert(node->AsLclVarCommon()->GetLclNum() == val.LclNum());
        }
        else
        {
            // Otherwise it must be accessed through some kind of indirection. Usually this is
            // something like IND(LCL_ADDR_VAR), which we will change to LCL_VAR or LCL_FLD so
            // the lclvar does not need to be address exposed.
            //
            // However, it is possible for the indirection to be wider than the lclvar
            // (e.g. *(long*)&int32Var) or to have a field offset that pushes the indirection
            // past the end of the lclvar memory location. In such cases we cannot do anything
            // so the lclvar needs to be address exposed.
            //
            // More importantly, if the lclvar is a promoted struct field then the parent lclvar
            // also needs to be address exposed so we get dependent struct promotion. Code like
            // *(long*)&int32Var has undefined behavior and it's practically useless but reading,
            // say, 2 consecutive Int32 struct fields as Int64 has more practical value.

            unsigned   lclNum    = val.LclNum();
            LclVarDsc* varDsc    = m_compiler->lvaGetDesc(lclNum);
            unsigned   indirSize = GetIndirSize(node);
            bool       isWide;

            if ((indirSize == 0) || ((val.Offset() + indirSize) > UINT16_MAX))
            {
                // If we can't figure out the indirection size then treat it as a wide indirection.
                // Additionally, treat indirections with large offsets as wide: local field nodes
                // and the emitter do not support them.
                isWide = true;
            }
            else
            {
                ClrSafeInt<unsigned> endOffset = ClrSafeInt<unsigned>(val.Offset()) + ClrSafeInt<unsigned>(indirSize);

                if (endOffset.IsOverflow())
                {
                    isWide = true;
                }
                else
                {
                    isWide = endOffset.Value() > m_compiler->lvaLclExactSize(lclNum);

                    if (varDsc->TypeGet() == TYP_BLK)
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
                m_compiler->lvaSetVarAddrExposed(varDsc->lvIsStructField
                                                     ? varDsc->lvParentLcl
                                                     : val.LclNum() DEBUGARG(AddressExposedReason::WIDE_INDIR));
                MorphWideLocalIndir(val);
            }
            else
            {
                MorphLocalIndir(val, user);
            }
        }

        INDEBUG(val.Consume();)
    }

    //------------------------------------------------------------------------
    // GetIndirSize: Return the size (in bytes) of an indirection node.
    //
    // Arguments:
    //    indir - the indirection node
    //    user - the node that uses the indirection
    //
    // Notes:
    //    This returns 0 for indirection of unknown size, i. e. IND<struct>
    //    nodes that are used as sources of STORE_DYN_BLKs.
    //
    unsigned GetIndirSize(GenTree* indir)
    {
        assert(indir->OperIs(GT_IND, GT_OBJ, GT_BLK, GT_FIELD));

        if (indir->TypeGet() != TYP_STRUCT)
        {
            return genTypeSize(indir);
        }

        if (indir->OperIs(GT_IND))
        {
            // TODO-1stClassStructs: remove once "IND<struct>" nodes are no more.
            return 0;
        }

        return indir->GetLayout(m_compiler)->GetSize();
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

        if (offset == 0)
        {
            addr->ChangeOper(GT_LCL_VAR_ADDR);
            addr->AsLclVar()->SetLclNum(lclNum);
        }
        else if (IsValidLclAddr(lclNum, offset))
        {
            addr->ChangeOper(GT_LCL_FLD_ADDR);
            addr->AsLclFld()->SetLclNum(lclNum);
            addr->AsLclFld()->SetLclOffs(offset);
            addr->AsLclFld()->SetLayout(nullptr);
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
    //    local variable to OBJ/BLK/IND(LCL_ADDR).
    //
    // Arguments:
    //    val  - a value that represents the local indirection
    //
    // Notes:
    //    This morphing is performed when the access cannot be turned into a
    //    a local node, e. g. it is volatile or "wide".
    //
    void MorphWideLocalIndir(const Value& val)
    {
        assert(val.Node()->OperIsIndir() || val.Node()->OperIs(GT_FIELD));

        GenTree* node = val.Node();
        GenTree* addr = node->gtGetOp1();

        MorphLocalAddress(addr, val.LclNum(), val.Offset());

        if (node->OperIs(GT_FIELD))
        {
            if (node->TypeIs(TYP_STRUCT))
            {
                ClassLayout* layout = node->GetLayout(m_compiler);
                node->SetOper(GT_OBJ);
                node->AsBlk()->Initialize(layout);
            }
            else
            {
                node->SetOper(GT_IND);
            }
        }

        // GLOB_REF may not be set already in the "large offset" case. Add it.
        node->gtFlags |= GTF_GLOB_REF;
    }

    //------------------------------------------------------------------------
    // MorphLocalIndir: Change a tree that represents indirect access to a
    //    local variable to a canonical shape (one of "IndirTransform"s).
    //
    // Arguments:
    //    val - a value that represents the local indirection
    //    user - the indirection's user node
    //
    void MorphLocalIndir(const Value& val, GenTree* user)
    {
        assert(val.IsLocation());

        ClassLayout*         indirLayout = nullptr;
        IndirTransform       transform   = SelectLocalIndirTransform(val, user, &indirLayout);
        GenTree*             indir       = val.Node();
        unsigned             lclNum      = val.LclNum();
        LclVarDsc*           varDsc      = m_compiler->lvaGetDesc(lclNum);
        GenTreeLclVarCommon* lclNode     = nullptr;

        switch (transform)
        {
            case IndirTransform::None:
                // TODO-ADDR: eliminate all such cases.
                return;

            case IndirTransform::Nop:
                indir->gtBashToNOP();
                m_stmtModified = true;
                return;

            case IndirTransform::BitCast:
                indir->ChangeOper(GT_BITCAST);
                lclNode = BashToLclVar(indir->gtGetOp1(), lclNum);
                break;

#ifdef FEATURE_HW_INTRINSICS
            case IndirTransform::GetElement:
            {
                var_types elementType = indir->TypeGet();
                assert(elementType == TYP_FLOAT);

                lclNode            = BashToLclVar(indir->gtGetOp1(), lclNum);
                GenTree* indexNode = m_compiler->gtNewIconNode(val.Offset() / genTypeSize(elementType));
                GenTree* hwiNode   = m_compiler->gtNewSimdGetElementNode(elementType, lclNode, indexNode,
                                                                       CORINFO_TYPE_FLOAT, genTypeSize(varDsc),
                                                                       /* isSimdAsHWIntrinsic */ false);
                indir      = hwiNode;
                *val.Use() = hwiNode;
            }
            break;

            case IndirTransform::WithElement:
            {
                assert(user->OperIs(GT_ASG) && (user->gtGetOp1() == indir));
                var_types elementType = indir->TypeGet();
                assert(elementType == TYP_FLOAT);

                lclNode              = BashToLclVar(indir, lclNum);
                GenTree* simdLclNode = m_compiler->gtNewLclvNode(lclNum, varDsc->TypeGet());
                GenTree* indexNode   = m_compiler->gtNewIconNode(val.Offset() / genTypeSize(elementType));
                GenTree* elementNode = user->gtGetOp2();
                user->AsOp()->gtOp2 =
                    m_compiler->gtNewSimdWithElementNode(varDsc->TypeGet(), simdLclNode, indexNode, elementNode,
                                                         CORINFO_TYPE_FLOAT, genTypeSize(varDsc),
                                                         /* isSimdAsHWIntrinsic */ false);
                user->ChangeType(varDsc->TypeGet());
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
                indir->ChangeOper(GT_LCL_VAR);
                indir->AsLclVar()->SetLclNum(lclNum);
                lclNode = indir->AsLclVarCommon();
                break;

            case IndirTransform::LclFld:
                indir->ChangeOper(GT_LCL_FLD);
                indir->AsLclFld()->SetLclNum(lclNum);
                indir->AsLclFld()->SetLclOffs(val.Offset());
                indir->AsLclFld()->SetLayout(indirLayout);
                lclNode = indir->AsLclVarCommon();

                if (!indir->TypeIs(TYP_STRUCT))
                {
                    // The general invariant in the compiler is that whoever creates a LCL_FLD node after local morph
                    // must mark the associated local DNER. We break this invariant here, for STRUCT fields, to allow
                    // global morph to transform these into enregisterable LCL_VARs, applying DNER otherwise.
                    m_compiler->lvaSetVarDoNotEnregister(val.LclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
                }
                break;

            default:
                unreached();
        }

        GenTreeFlags lclNodeFlags = GTF_EMPTY;

        if (user->OperIs(GT_ASG) && (user->AsOp()->gtGetOp1() == indir))
        {
            lclNodeFlags |= (GTF_VAR_DEF | GTF_DONT_CSE);

            if (!indir->OperIs(GT_LCL_VAR))
            {
                unsigned lhsSize = indir->TypeIs(TYP_STRUCT) ? indirLayout->GetSize() : genTypeSize(indir);
                unsigned lclSize = m_compiler->lvaLclExactSize(lclNum);
                if (lhsSize != lclSize)
                {
                    assert(lhsSize < lclSize);
                    lclNodeFlags |= GTF_VAR_USEASG;
                }
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
    //    val           - a value that represents the local indirection
    //    user          - the indirection's user node
    //    pStructLayout - [out] parameter for layout of struct indirections
    //
    // Return Value:
    //    The transformation the caller should perform on this indirection.
    //
    IndirTransform SelectLocalIndirTransform(const Value& val, GenTree* user, ClassLayout** pStructLayout)
    {
        GenTree* indir = val.Node();

        // We don't expect indirections that cannot be turned into local nodes here.
        assert(val.Offset() <= UINT16_MAX);
        assert(indir->OperIs(GT_IND, GT_OBJ, GT_BLK, GT_FIELD) && ((indir->gtFlags & GTF_IND_VOLATILE) == 0));

        if (IsUnused(indir, user))
        {
            return IndirTransform::Nop;
        }

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(val.LclNum());

        if (indir->TypeGet() != TYP_STRUCT)
        {
            if (indir->TypeGet() == varDsc->TypeGet())
            {
                return IndirTransform::LclVar;
            }

            // Bool and ubyte are the same type.
            if ((indir->TypeIs(TYP_BOOL) && (varDsc->TypeGet() == TYP_UBYTE)) ||
                (indir->TypeIs(TYP_UBYTE) && (varDsc->TypeGet() == TYP_BOOL)))
            {
                return IndirTransform::LclVar;
            }

            bool isDef = user->OperIs(GT_ASG) && (user->gtGetOp1() == indir);

            // For small locals on the LHS we can ignore the signed/unsigned diff.
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
            if (varTypeIsSIMD(varDsc) && indir->TypeIs(TYP_FLOAT) && ((val.Offset() % genTypeSize(TYP_FLOAT)) == 0) &&
                m_compiler->IsBaselineSimdIsaSupported())
            {
                return isDef ? IndirTransform::WithElement : IndirTransform::GetElement;
            }
#endif // FEATURE_HW_INTRINSICS

            // Turn this into a bitcast if we can.
            if ((genTypeSize(indir) == genTypeSize(varDsc)) && (varTypeIsFloating(indir) || varTypeIsFloating(varDsc)))
            {
                // TODO-ADDR: enable this optimization for all users and all targets.
                if (user->OperIs(GT_RETURN) && (genTypeSize(indir) <= TARGET_POINTER_SIZE))
                {
                    return IndirTransform::BitCast;
                }
            }

            return IndirTransform::LclFld;
        }

        ClassLayout* indirLayout = nullptr;

        if (indir->OperIs(GT_FIELD))
        {
            CORINFO_CLASS_HANDLE fieldClassHandle;
            var_types            fieldType = m_compiler->eeGetFieldType(indir->AsField()->gtFldHnd, &fieldClassHandle);
            assert(fieldType == TYP_STRUCT);

            indirLayout = m_compiler->typGetObjLayout(fieldClassHandle);
        }
        else
        {
            indirLayout = indir->AsBlk()->GetLayout();
        }

        *pStructLayout = indirLayout;

        if (varDsc->TypeGet() != TYP_STRUCT)
        {
            return IndirTransform::LclFld;
        }

        if ((val.Offset() == 0) && ClassLayout::AreCompatible(indirLayout, varDsc->GetLayout()))
        {
            return IndirTransform::LclVar;
        }

        return IndirTransform::LclFld;
    }

    //------------------------------------------------------------------------
    // MorphStructField: Reduces indirect access to a promoted local (e.g.
    //    FIELD(ADDR(LCL_VAR))) to a GT_LCL_VAR that references the struct field.
    //
    // Arguments:
    //    node - the GT_IND/GT_FIELD/GT_FIELD_ADDR node
    //    user - the node that uses the field
    //
    // Notes:
    //    This does not do anything if the access does not denote a promoted
    //    struct field.
    //
    void MorphStructField(GenTree* node, GenTree* user)
    {
        assert(node->OperIs(GT_IND, GT_FIELD, GT_FIELD_ADDR));

        GenTree* objRef = node->AsUnOp()->gtOp1;

        // TODO-Bug: this code does not pay attention to "GTF_IND_VOLATILE".
        if ((objRef != nullptr) && objRef->OperIs(GT_LCL_VAR_ADDR))
        {
            const LclVarDsc* varDsc = m_compiler->lvaGetDesc(objRef->AsLclVarCommon());

            if (varDsc->lvPromoted)
            {
                unsigned fieldOffset = node->OperIs(GT_IND) ? 0 : node->AsField()->gtFldOffset;
                unsigned fieldLclNum = m_compiler->lvaGetFieldLocal(varDsc, fieldOffset);

                if (fieldLclNum == BAD_VAR_NUM)
                {
                    // Access a promoted struct's field with an offset that doesn't correspond to any field.
                    // It can happen if the struct was cast to another struct with different offsets.
                    return;
                }

                const LclVarDsc* fieldDsc  = m_compiler->lvaGetDesc(fieldLclNum);
                var_types        fieldType = fieldDsc->TypeGet();
                assert(fieldType != TYP_STRUCT); // Promoted LCL_VAR can't have a struct type.

                if (node->OperIs(GT_FIELD_ADDR))
                {
                    node->ChangeOper(GT_LCL_VAR_ADDR);
                    node->AsLclVar()->SetLclNum(fieldLclNum);
                }
                else if (node->TypeGet() == fieldType)
                {
                    GenTreeFlags lclVarFlags = node->gtFlags & (GTF_NODE_MASK | GTF_DONT_CSE);

                    if ((user != nullptr) && user->OperIs(GT_ASG) && (user->AsOp()->gtOp1 == node))
                    {
                        lclVarFlags |= GTF_VAR_DEF;
                    }

                    node->ChangeOper(GT_LCL_VAR);
                    node->AsLclVar()->SetLclNum(fieldLclNum);
                    node->gtType  = fieldType;
                    node->gtFlags = lclVarFlags;
                }
                else // Here we will turn "FIELD/IND(LCL_ADDR_VAR<parent>)" into "OBJ/IND(LCL_ADDR_VAR<field>)".
                {
                    // This type mismatch is somewhat common due to how we retype fields of struct type that
                    // recursively simplify down to a primitive. E. g. for "struct { struct { int a } A, B }",
                    // the promoted local would look like "{ int a, B }", while the IR would contain "FIELD"
                    // nodes for the outer struct "A".
                    //
                    // TODO-1stClassStructs: delete this once "IND<struct>" nodes are no more.
                    if (node->OperIs(GT_IND) && node->TypeIs(TYP_STRUCT))
                    {
                        return;
                    }

                    ClassLayout* layout  = node->TypeIs(TYP_STRUCT) ? node->GetLayout(m_compiler) : nullptr;
                    unsigned     indSize = node->TypeIs(TYP_STRUCT) ? layout->GetSize() : genTypeSize(node);
                    if (indSize > genTypeSize(fieldType))
                    {
                        // Retargeting this indirection to reference the promoted field would make it
                        // "wide", address-exposing the whole parent struct (with all of its fields).
                        return;
                    }

                    if (node->TypeIs(TYP_STRUCT))
                    {
                        node->SetOper(GT_OBJ);
                        node->AsBlk()->Initialize(layout);
                    }
                    else
                    {
                        node->SetOper(GT_IND);
                    }

                    objRef->AsLclVar()->SetLclNum(fieldLclNum);
                }

                JITDUMP("Replacing the field in promoted struct with local var V%02u\n", fieldLclNum);
                m_stmtModified = true;
            }
        }
    }

    //------------------------------------------------------------------------
    // MorphLocalField: Replaces a GT_LCL_FLD based promoted struct field access
    //    with a GT_LCL_VAR that references the struct field.
    //
    // Arguments:
    //    node - the GT_LCL_FLD node
    //    user - the node that uses the field
    //
    // Notes:
    //    This does not do anything if the field access does not denote
    //    involved a promoted struct local.
    //    If the GT_LCL_FLD offset does not have a corresponding promoted struct
    //    field then no transformation is done and struct local's enregistration
    //    is disabled.
    //
    void MorphLocalField(GenTree* node, GenTree* user)
    {
        assert(node->OperIs(GT_LCL_FLD));
        // TODO-Cleanup: Move fgMorphLocalField implementation here, it's not used anywhere else.
        m_compiler->fgMorphLocalField(node, user);
        m_stmtModified |= node->OperIs(GT_LCL_VAR);
    }

public:
    //------------------------------------------------------------------------
    // UpdateEarlyRefCount: updates the ref count for locals
    //
    // Arguments:
    //    lclNum - the local number to update the count for.
    //
    // Notes:
    //    fgMakeOutgoingStructArgCopy checks the ref counts for implicit byref params when it decides
    //    if it's legal to elide certain copies of them;
    //    fgRetypeImplicitByRefArgs checks the ref counts when it decides to undo promotions.
    //    fgForwardSub uses ref counts to decide when to forward sub.
    //
    void UpdateEarlyRefCount(unsigned lclNum)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        // Note we don't need accurate counts when the values are large.
        //
        if (varDsc->lvRefCnt(RCS_EARLY) < USHRT_MAX)
        {
            varDsc->incLvRefCnt(1, RCS_EARLY);
        }

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
        // CALL(OBJ(LCL_VAR_ADDR...))
        bool isArgToCall   = false;
        bool keepSearching = true;
        for (int i = 0; i < m_ancestors.Height() && keepSearching; i++)
        {
            GenTree* node = m_ancestors.Top(i);
            switch (i)
            {
                case 0:
                {
                    keepSearching = node->OperIs(GT_LCL_VAR_ADDR);
                }
                break;

                case 1:
                {
                    keepSearching = node->OperIs(GT_OBJ);
                }
                break;

                case 2:
                {
                    keepSearching = false;
                    isArgToCall   = node->IsCall();
                }
                break;
                default:
                {
                    keepSearching = false;
                }
                break;
            }
        }

        if (isArgToCall)
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

    //------------------------------------------------------------------------
    // BashToLclVar: Bash node to a LCL_VAR.
    //
    // Arguments:
    //    node   - the node to bash
    //    lclNum - the local's number
    //
    // Return Value:
    //    The bashed node.
    //
    GenTreeLclVar* BashToLclVar(GenTree* node, unsigned lclNum)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        node->ChangeOper(GT_LCL_VAR);
        node->ChangeType(varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc));
        node->AsLclVar()->SetLclNum(lclNum);

        return node->AsLclVar();
    }

    void SequenceLocal(GenTreeLclVarCommon* lcl)
    {
        if (m_sequencer != nullptr)
        {
            m_sequencer->SequenceLocal(lcl);
        }
    }

    void SequenceAssignment(GenTreeOp* asg)
    {
        if (m_sequencer != nullptr)
        {
            m_sequencer->SequenceAssignment(asg);
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
                stmt->GetRootNode()->OperIs(GT_ASG))
            {
                madeChanges |= fgMorphCombineSIMDFieldAssignments(block, stmt);
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
                visitor.UpdateEarlyRefCount(lclNum);
            }
        }
    }

    madeChanges |= visitor.MadeChanges();

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#ifdef FEATURE_SIMD
//-----------------------------------------------------------------------------------
// fgMorphCombineSIMDFieldAssignments:
//    If the RHS of the input stmt is a read for simd vector X Field, then this
//    function will keep reading next few stmts based on the vector size(2, 3, 4).
//    If the next stmts LHS are located contiguous and RHS are also located
//    contiguous, then we replace those statements with one store.
//
// Argument:
//    block - BasicBlock*. block which stmt belongs to
//    stmt  - Statement*. the stmt node we want to check
//
// Return Value:
//    Whether the assignments were successfully coalesced.
//
bool Compiler::fgMorphCombineSIMDFieldAssignments(BasicBlock* block, Statement* stmt)
{
    GenTree* tree = stmt->GetRootNode();
    assert(tree->OperGet() == GT_ASG);

    GenTree*    originalLHS     = tree->AsOp()->gtOp1;
    GenTree*    prevLHS         = tree->AsOp()->gtOp1;
    GenTree*    prevRHS         = tree->AsOp()->gtOp2;
    unsigned    index           = 0;
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned    simdSize        = 0;
    GenTree*    simdLclAddr     = getSIMDStructFromField(prevRHS, &simdBaseJitType, &index, &simdSize, true);

    if ((simdLclAddr == nullptr) || (index != 0) || (simdBaseJitType != CORINFO_TYPE_FLOAT))
    {
        // if the RHS is not from a SIMD vector field X, then there is no need to check further.
        return false;
    }

    var_types  simdBaseType         = JitType2PreciseVarType(simdBaseJitType);
    var_types  simdType             = getSIMDTypeForSize(simdSize);
    int        assignmentsCount     = simdSize / genTypeSize(simdBaseType) - 1;
    int        remainingAssignments = assignmentsCount;
    Statement* curStmt              = stmt->GetNextStmt();
    Statement* lastStmt             = stmt;

    while (curStmt != nullptr && remainingAssignments > 0)
    {
        GenTree* exp = curStmt->GetRootNode();
        if (exp->OperGet() != GT_ASG)
        {
            break;
        }
        GenTree* curLHS = exp->gtGetOp1();
        GenTree* curRHS = exp->gtGetOp2();

        if (!areArgumentsContiguous(prevLHS, curLHS) || !areArgumentsContiguous(prevRHS, curRHS))
        {
            break;
        }

        remainingAssignments--;
        prevLHS = curLHS;
        prevRHS = curRHS;

        lastStmt = curStmt;
        curStmt  = curStmt->GetNextStmt();
    }

    if (remainingAssignments > 0)
    {
        // if the left assignments number is bigger than zero, then this means
        // that the assignments are not assigning to the contiguously memory
        // locations from same vector.
        return false;
    }

    JITDUMP("\nFound contiguous assignments from a SIMD vector to memory.\n");
    JITDUMP("From " FMT_BB ", " FMT_STMT " to " FMT_STMT "\n", block->bbNum, stmt->GetID(), lastStmt->GetID());

    for (int i = 0; i < assignmentsCount; i++)
    {
        fgRemoveStmt(block, stmt->GetNextStmt());
    }

    GenTree* dstNode;

    if (originalLHS->OperIs(GT_LCL_FLD))
    {
        dstNode         = originalLHS;
        dstNode->gtType = simdType;
    }
    else
    {
        GenTree* copyBlkDst = CreateAddressNodeForSimdHWIntrinsicCreate(originalLHS, TYP_FLOAT, simdSize);
        dstNode             = gtNewOperNode(GT_IND, simdType, copyBlkDst);
    }

    JITDUMP("\n" FMT_BB " " FMT_STMT " (before):\n", block->bbNum, stmt->GetID());
    DISPSTMT(stmt);

    tree = gtNewAssignNode(dstNode, gtNewLclvNode(simdLclAddr->AsLclVarCommon()->GetLclNum(), simdType));

    stmt->SetRootNode(tree);

    JITDUMP("\nReplaced " FMT_BB " " FMT_STMT " (after):\n", block->bbNum, stmt->GetID());
    DISPSTMT(stmt);

    return true;
}
#endif // FEATURE_SIMD
