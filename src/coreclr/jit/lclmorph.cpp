// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

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
    // The existence of GT_ADDR nodes and their use together with GT_FIELD to form
    // FIELD/ADDR/FIELD/ADDR/LCL_VAR sequences complicate things a bit. A typical
    // GT_FIELD node acts like an indirection and should produce an unknown value,
    // local address analysis doesn't know or care what value the field stores.
    // But a GT_FIELD can also be used as an operand for a GT_ADDR node and then
    // the GT_FIELD node does not perform an indirection, it's just represents a
    // location, similar to GT_LCL_VAR and GT_LCL_FLD.
    //
    // To avoid this issue, the semantics of GT_FIELD (and for simplicity's sake any other
    // indirection) nodes slightly deviates from the IR semantics - an indirection does not
    // actually produce an unknown value but a location value, if the indirection address
    // operand is an address value.
    //
    // The actual indirection is performed when the indirection's user node is processed:
    //   - A GT_ADDR user turns the location value produced by the indirection back
    //     into an address value.
    //   - Any other user node performs the indirection and produces an unknown value.
    //
    class Value
    {
        GenTree*      m_node;
        FieldSeqNode* m_fieldSeq;
        unsigned      m_lclNum;
        unsigned      m_offset;
        bool          m_address;
        INDEBUG(bool m_consumed;)

    public:
        // Produce an unknown value associated with the specified node.
        Value(GenTree* node)
            : m_node(node)
            , m_fieldSeq(nullptr)
            , m_lclNum(BAD_VAR_NUM)
            , m_offset(0)
            , m_address(false)
#ifdef DEBUG
            , m_consumed(false)
#endif // DEBUG
        {
        }

        // Get the node that produced this value.
        GenTree* Node() const
        {
            return m_node;
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

        // Get the location's field sequence.
        FieldSeqNode* FieldSeq() const
        {
            return m_fieldSeq;
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
            assert(m_fieldSeq == nullptr);
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
            assert(m_fieldSeq == nullptr);
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

            m_lclNum   = lclFld->GetLclNum();
            m_offset   = lclFld->GetLclOffs();
            m_fieldSeq = lclFld->GetFieldSeq();
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

            m_lclNum   = lclFld->GetLclNum();
            m_offset   = lclFld->GetLclOffs();
            m_fieldSeq = lclFld->GetFieldSeq();
            m_address  = true;
        }

        //------------------------------------------------------------------------
        // Address: Produce an address value from a location value.
        //
        // Arguments:
        //    val - the input value
        //
        // Notes:
        //   - LOCATION(lclNum, offset) => ADDRESS(lclNum, offset)
        //   - ADDRESS(lclNum, offset) => invalid, we should never encounter something like ADDR(ADDR(...))
        //   - UNKNOWN => UNKNOWN
        //
        void Address(Value& val)
        {
            assert(!IsLocation() && !IsAddress());
            assert(!val.IsAddress());

            if (val.IsLocation())
            {
                m_address  = true;
                m_lclNum   = val.m_lclNum;
                m_offset   = val.m_offset;
                m_fieldSeq = val.m_fieldSeq;
            }

            INDEBUG(val.Consume();)
        }

        //------------------------------------------------------------------------
        // Field: Produce a location value from an address value.
        //
        // Arguments:
        //    val - the input value
        //    field - the FIELD node that uses the input address value
        //    fieldSeqStore - the compiler's field sequence store
        //
        // Return Value:
        //    `true` if the value was consumed. `false` if the input value
        //    cannot be consumed because it is itsef a location or because
        //    the offset overflowed. In this case the caller is expected
        //    to escape the input value.
        //
        // Notes:
        //   - LOCATION(lclNum, offset) => not representable, must escape
        //   - ADDRESS(lclNum, offset) => LOCATION(lclNum, offset + field.Offset)
        //     if the offset overflows then location is not representable, must escape
        //   - UNKNOWN => UNKNOWN
        //
        bool Field(Value& val, GenTreeField* field, FieldSeqStore* fieldSeqStore)
        {
            assert(!IsLocation() && !IsAddress());

            if (val.IsLocation())
            {
                return false;
            }

            if (val.IsAddress())
            {
                ClrSafeInt<unsigned> newOffset =
                    ClrSafeInt<unsigned>(val.m_offset) + ClrSafeInt<unsigned>(field->gtFldOffset);

                if (newOffset.IsOverflow())
                {
                    return false;
                }

                m_lclNum = val.m_lclNum;
                m_offset = newOffset.Value();

                if (field->gtFldMayOverlap)
                {
                    m_fieldSeq = FieldSeqStore::NotAField();
                }
                else
                {
                    m_fieldSeq = fieldSeqStore->Append(val.m_fieldSeq, fieldSeqStore->CreateSingleton(field->gtFldHnd));
                }
            }

            INDEBUG(val.Consume();)
            return true;
        }

        //------------------------------------------------------------------------
        // Indir: Produce a location value from an address value.
        //
        // Arguments:
        //    val - the input value
        //
        // Return Value:
        //    `true` if the value was consumed. `false` if the input value
        //    cannot be consumed because it is itsef a location. In this
        //    case the caller is expected to escape the input value.
        //
        // Notes:
        //   - LOCATION(lclNum, offset) => not representable, must escape
        //   - ADDRESS(lclNum, offset) => LOCATION(lclNum, offset)
        //   - UNKNOWN => UNKNOWN
        //
        bool Indir(Value& val)
        {
            assert(!IsLocation() && !IsAddress());

            if (val.IsLocation())
            {
                return false;
            }

            if (val.IsAddress())
            {
                m_lclNum   = val.m_lclNum;
                m_offset   = val.m_offset;
                m_fieldSeq = val.m_fieldSeq;
            }

            INDEBUG(val.Consume();)
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

    ArrayStack<Value> m_valueStack;
    INDEBUG(bool m_stmtModified;)

public:
    enum
    {
        DoPreOrder        = true,
        DoPostOrder       = true,
        ComputeStack      = true,
        DoLclVarsOnly     = false,
        UseExecutionOrder = false,
    };

    LocalAddressVisitor(Compiler* comp)
        : GenTreeVisitor<LocalAddressVisitor>(comp), m_valueStack(comp->getAllocator(CMK_LocalAddressVisitor))
    {
    }

    void VisitStmt(Statement* stmt)
    {
#ifdef DEBUG
        if (m_compiler->verbose)
        {
            printf("LocalAddressVisitor visiting statement:\n");
            m_compiler->gtDispStmt(stmt);
            m_stmtModified = false;
        }
#endif // DEBUG

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

    // Morph promoted struct fields and count implicit byref argument occurrences.
    // Also create and push the value produced by the visited node. This is done here
    // rather than in PostOrderVisit because it makes it easy to handle nodes with an
    // arbitrary number of operands - just pop values until the value corresponding
    // to the visited node is encountered.
    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* node = *use;

        if (node->OperIs(GT_FIELD))
        {
            MorphStructField(node, user);
        }
        else if (node->OperIs(GT_LCL_FLD))
        {
            MorphLocalField(node, user);
        }

        if (node->OperIsLocal())
        {
            unsigned lclNum = node->AsLclVarCommon()->GetLclNum();

            LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
            if (varDsc->lvIsStructField)
            {
                // Promoted field, increase counter for the parent lclVar.
                assert(!m_compiler->lvaIsImplicitByRefLocal(lclNum));
                unsigned parentLclNum = varDsc->lvParentLcl;
                UpdateEarlyRefCountForImplicitByRef(parentLclNum);
            }
            else
            {
                UpdateEarlyRefCountForImplicitByRef(lclNum);
            }
        }

        PushValue(node);

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
                break;

            case GT_LCL_VAR_ADDR:
                assert(TopValue(0).Node() == node);

                TopValue(0).Address(node->AsLclVar());
                break;

            case GT_LCL_FLD:
                assert(TopValue(0).Node() == node);

                TopValue(0).Location(node->AsLclFld());
                break;

            case GT_LCL_FLD_ADDR:
                assert(TopValue(0).Node() == node);

                TopValue(0).Address(node->AsLclFld());
                break;

            case GT_ADDR:
                assert(TopValue(1).Node() == node);
                assert(TopValue(0).Node() == node->gtGetOp1());

                TopValue(1).Address(TopValue(0));
                PopValue();
                break;

            case GT_FIELD:
                if (node->AsField()->gtFldObj != nullptr)
                {
                    assert(TopValue(1).Node() == node);
                    assert(TopValue(0).Node() == node->AsField()->gtFldObj);

                    if (!TopValue(1).Field(TopValue(0), node->AsField(), m_compiler->GetFieldSeqStore()))
                    {
                        // Either the address comes from a location value (e.g. FIELD(IND(...)))
                        // or the field offset has overflowed.
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

                if ((node->gtFlags & GTF_IND_VOLATILE) != 0)
                {
                    // Volatile indirections must not be removed so the address,
                    // if any, must be escaped.
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

            case GT_DYN_BLK:
                assert(TopValue(2).Node() == node);
                assert(TopValue(1).Node() == node->AsDynBlk()->Addr());
                assert(TopValue(0).Node() == node->AsDynBlk()->gtDynamicSize);

                // The block size may be the result of an indirection so we need
                // to escape the location that may be associated with it.
                EscapeValue(TopValue(0), node);

                if (!TopValue(2).Indir(TopValue(1)))
                {
                    // If the address comes from another indirection (e.g. DYN_BLK(IND(...))
                    // then we need to escape the location.
                    EscapeLocation(TopValue(1), node);
                }

                PopValue();
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
                        // be merged separatly.
                        GenTreeLclVar* lclVar = retVal->AsLclVar();
                        unsigned       lclNum = lclVar->GetLclNum();
                        if (!m_compiler->compMethodReturnsMultiRegRegTypeAlternate() &&
                            !m_compiler->lvaIsImplicitByRefLocal(lclVar->GetLclNum()))
                        {
                            LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
                            if (varDsc->lvFieldCnt > 1)
                            {
                                m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_BlockOp));
                            }
                        }
                    }

                    EscapeValue(TopValue(0), node);
                    PopValue();
                }
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
    void PushValue(GenTree* node)
    {
        m_valueStack.Push(node);
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

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(val.LclNum());

        // In general we don't know how an exposed struct field address will be used - it may be used to
        // access only that specific field or it may be used to access other fields in the same struct
        // be using pointer/ref arithmetic. It seems reasonable to make an exception for the "this" arg
        // of calls - it would be highly unsual for a struct member method to attempt to access memory
        // beyond "this" instance. And calling struct member methods is common enough that attempting to
        // mark the entire struct as address exposed results in CQ regressions.
        bool isThisArg = user->IsCall() && (user->AsCall()->gtCallThisArg != nullptr) &&
                         (val.Node() == user->AsCall()->gtCallThisArg->GetNode());
        bool exposeParentLcl = varDsc->lvIsStructField && !isThisArg;

        m_compiler->lvaSetVarAddrExposed(exposeParentLcl ? varDsc->lvParentLcl : val.LclNum());

#ifdef TARGET_64BIT
        // If the address of a variable is passed in a call and the allocation size of the variable
        // is 32 bits we will quirk the size to 64 bits. Some PInvoke signatures incorrectly specify
        // a ByRef to an INT32 when they actually write a SIZE_T or INT64. There are cases where
        // overwriting these extra 4 bytes corrupts some data (such as a saved register) that leads
        // to A/V. Wheras previously the JIT64 codegen did not lead to an A/V.
        if (!varDsc->lvIsParam && !varDsc->lvIsStructField && (genActualType(varDsc->TypeGet()) == TYP_INT))
        {
            // TODO-Cleanup: This should simply check if the user is a call node, not if a call ancestor exists.
            if (Compiler::gtHasCallOnStack(&m_ancestors))
            {
                varDsc->lvQuirkToLong = true;
                JITDUMP("Adding a quirk for the storage size of V%02u of type %s\n", val.LclNum(),
                        varTypeName(varDsc->TypeGet()));
            }
        }
#endif // TARGET_64BIT

        // TODO-ADDR: For now use LCL_VAR_ADDR and LCL_FLD_ADDR only as call arguments and assignment sources.
        // Other usages require more changes. For example, a tree like OBJ(ADD(ADDR(LCL_VAR), 4))
        // could be changed to OBJ(LCL_FLD_ADDR) but then DefinesLocalAddr does not recognize
        // LCL_FLD_ADDR (even though it does recognize LCL_VAR_ADDR).
        if (user->OperIs(GT_CALL, GT_ASG))
        {
            MorphLocalAddress(val);
        }

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
            // something like IND(ADDR(LCL_VAR)), global morph will change it to GT_LCL_VAR or
            // GT_LCL_FLD so the lclvar does not need to be address exposed.
            //
            // However, it is possible for the indirection to be wider than the lclvar
            // (e.g. *(long*)&int32Var) or to have a field offset that pushes the indirection
            // past the end of the lclvar memory location. In such cases morph doesn't do
            // anything so the lclvar needs to be address exposed.
            //
            // More importantly, if the lclvar is a promoted struct field then the parent lclvar
            // also needs to be address exposed so we get dependent struct promotion. Code like
            // *(long*)&int32Var has undefined behavior and it's practically useless but reading,
            // say, 2 consecutive Int32 struct fields as Int64 has more practical value.

            LclVarDsc* varDsc    = m_compiler->lvaGetDesc(val.LclNum());
            unsigned   indirSize = GetIndirSize(node, user);
            bool       isWide;

            if (indirSize == 0)
            {
                // If we can't figure out the indirection size then treat it as a wide indirection.
                isWide = true;
            }
            else
            {
                ClrSafeInt<unsigned> endOffset = ClrSafeInt<unsigned>(val.Offset()) + ClrSafeInt<unsigned>(indirSize);

                if (endOffset.IsOverflow())
                {
                    isWide = true;
                }
                else if (varDsc->TypeGet() == TYP_STRUCT)
                {
                    isWide = (endOffset.Value() > varDsc->lvExactSize);
                }
                else
                {
                    // For small int types use the real type size, not the stack slot size.
                    // Morph does manage to transform `*(int*)&byteVar` into just byteVar where
                    // the LCL_VAR node has type TYP_INT. But such code is simply bogus and
                    // there's no reason to attempt to optimize it. It makes more sense to
                    // mark the variable address exposed in such circumstances.
                    //
                    // Same for "small" SIMD types - SIMD8/12 have 8/12 bytes, even if the
                    // stack location may have 16 bytes.
                    //
                    // For TYP_BLK variables the type size is 0 so they're always address
                    // exposed.
                    isWide = (endOffset.Value() > genTypeSize(varDsc->TypeGet()));
                }
            }

            if (isWide)
            {
                m_compiler->lvaSetVarAddrExposed(varDsc->lvIsStructField ? varDsc->lvParentLcl : val.LclNum());
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
    //    This returns 0 for indirection of unknown size, typically GT_DYN_BLK.
    //    GT_IND nodes that have type TYP_STRUCT are expected to only appears
    //    on the RHS of an assignment, in which case the LHS size will be used instead.
    //    Otherwise 0 is returned as well.
    //
    unsigned GetIndirSize(GenTree* indir, GenTree* user)
    {
        assert(indir->OperIs(GT_IND, GT_OBJ, GT_BLK, GT_DYN_BLK, GT_FIELD));

        if (indir->TypeGet() != TYP_STRUCT)
        {
            return genTypeSize(indir->TypeGet());
        }

        // A struct indir that is the RHS of an assignment needs special casing:
        // - It can be a GT_IND of type TYP_STRUCT, in which case the size is given by the LHS.
        // - It can be a GT_OBJ that has a correct size, but different than the size of the LHS.
        //   The LHS size takes precedence.
        // Just take the LHS size in all cases.
        if (user != nullptr && user->OperIs(GT_ASG) && (indir == user->gtGetOp2()))
        {
            indir = user->gtGetOp1();

            if (indir->TypeGet() != TYP_STRUCT)
            {
                return genTypeSize(indir->TypeGet());
            }

            // The LHS may be a LCL_VAR/LCL_FLD, these are not indirections so we need to handle them here.
            // It can also be a GT_INDEX, this is an indirection but it never applies to lclvar addresses
            // so it needs to be handled here as well.

            switch (indir->OperGet())
            {
                case GT_LCL_VAR:
                    return m_compiler->lvaGetDesc(indir->AsLclVar())->lvExactSize;
                case GT_LCL_FLD:
                    return genTypeSize(indir->TypeGet());
                case GT_INDEX:
                    return indir->AsIndex()->gtIndElemSize;
                default:
                    break;
            }
        }

        switch (indir->OperGet())
        {
            case GT_FIELD:
                return m_compiler->info.compCompHnd->getClassSize(
                    m_compiler->info.compCompHnd->getFieldClass(indir->AsField()->gtFldHnd));
            case GT_BLK:
            case GT_OBJ:
                return indir->AsBlk()->GetLayout()->GetSize();
            default:
                assert(indir->OperIs(GT_IND, GT_DYN_BLK));
                return 0;
        }
    }

    //------------------------------------------------------------------------
    // MorphLocalAddress: Change a tree that represents a local variable address
    //    to a single LCL_VAR_ADDR or LCL_FLD_ADDR node.
    //
    // Arguments:
    //    val - a value that represents the local address
    //
    void MorphLocalAddress(const Value& val)
    {
        assert(val.IsAddress());
        assert(val.Node()->TypeIs(TYP_BYREF, TYP_I_IMPL));
        assert(m_compiler->lvaVarAddrExposed(val.LclNum()));

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(val.LclNum());

        if (varDsc->lvPromoted || varDsc->lvIsStructField || m_compiler->lvaIsImplicitByRefLocal(val.LclNum()))
        {
            // TODO-ADDR: For now we ignore promoted and "implicit by ref" variables,
            // they require additional changes in subsequent phases.
            return;
        }

#ifdef TARGET_X86
        if (m_compiler->info.compIsVarArgs && varDsc->lvIsParam && !varDsc->lvIsRegArg)
        {
            // TODO-ADDR: For now we ignore all stack parameters of varargs methods,
            // fgMorphStackArgForVarArgs does not handle LCL_VAR|FLD_ADDR nodes.
            return;
        }
#endif

        GenTree* addr = val.Node();

        if (val.Offset() > UINT16_MAX)
        {
            // The offset is too large to store in a LCL_FLD_ADDR node,
            // use ADD(LCL_VAR_ADDR, offset) instead.
            addr->ChangeOper(GT_ADD);
            addr->AsOp()->gtOp1 = m_compiler->gtNewLclVarAddrNode(val.LclNum());
            addr->AsOp()->gtOp2 = m_compiler->gtNewIconNode(val.Offset(), val.FieldSeq());
        }
        else if ((val.Offset() != 0) || (val.FieldSeq() != nullptr))
        {
            addr->ChangeOper(GT_LCL_FLD_ADDR);
            addr->AsLclFld()->SetLclNum(val.LclNum());
            addr->AsLclFld()->SetLclOffs(val.Offset());
            addr->AsLclFld()->SetFieldSeq(val.FieldSeq());
        }
        else
        {
            addr->ChangeOper(GT_LCL_VAR_ADDR);
            addr->AsLclVar()->SetLclNum(val.LclNum());
        }

        // Local address nodes never have side effects (nor any other flags, at least at this point).
        addr->gtFlags = GTF_EMPTY;

        INDEBUG(m_stmtModified = true;)
    }

    //------------------------------------------------------------------------
    // MorphLocalIndir: Change a tree that represents an indirect access to a struct
    //    variable to a single LCL_VAR or LCL_FLD node.
    //
    // Arguments:
    //    val - a value that represents the local indirection
    //    user - the indirection's user node
    //
    void MorphLocalIndir(const Value& val, GenTree* user)
    {
        assert(val.IsLocation());

        GenTree* indir = val.Node();
        assert(indir->OperIs(GT_IND, GT_OBJ, GT_BLK, GT_FIELD));

        if (val.Offset() > UINT16_MAX)
        {
            // TODO-ADDR: We can't use LCL_FLD because the offset is too large but we should
            // transform the tree into IND(ADD(LCL_VAR_ADDR, offset)) instead of leaving this
            // this to fgMorphField.
            return;
        }

        if (indir->OperIs(GT_FIELD) ? indir->AsField()->IsVolatile() : indir->AsIndir()->IsVolatile())
        {
            // TODO-ADDR: We shouldn't remove the indir because it's volatile but we should
            // transform the tree into IND(LCL_VAR|FLD_ADDR) instead of leaving this to
            // fgMorphField.
            return;
        }

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(val.LclNum());

        if (varDsc->TypeGet() != TYP_STRUCT)
        {
            // TODO-ADDR: Skip integral/floating point variables for now, they're more
            // complicated to transform. We can always turn an indirect access of such
            // a variable into a LCL_FLD but that blocks enregistration so we need to
            // detect those case where we can use LCL_VAR instead, perhaps in conjuction
            // with CAST and/or BITCAST.
            // Also skip SIMD variables for now, fgMorphFieldAssignToSimdSetElement and
            // others need to be updated to recognize LCL_FLDs.
            return;
        }

        if (varDsc->lvPromoted || varDsc->lvIsStructField || m_compiler->lvaIsImplicitByRefLocal(val.LclNum()))
        {
            // TODO-ADDR: For now we ignore promoted and "implicit by ref" variables,
            // they require additional changes in subsequent phases
            // (e.g. fgMorphImplicitByRefArgs does not handle LCL_FLD nodes).
            return;
        }

#ifdef TARGET_X86
        if (m_compiler->info.compIsVarArgs && varDsc->lvIsParam && !varDsc->lvIsRegArg)
        {
            // TODO-ADDR: For now we ignore all stack parameters of varargs methods,
            // fgMorphStackArgForVarArgs does not handle LCL_FLD nodes.
            return;
        }
#endif

        ClassLayout*  structLayout = nullptr;
        FieldSeqNode* fieldSeq     = val.FieldSeq();

        if ((fieldSeq != nullptr) && (fieldSeq != FieldSeqStore::NotAField()))
        {
            // TODO-ADDR: ObjectAllocator produces FIELD nodes with FirstElemPseudoField as field
            // handle so we cannot use FieldSeqNode::GetFieldHandle() because it asserts on such
            // handles. ObjectAllocator should be changed to create LCL_FLD nodes directly.
            assert(!indir->OperIs(GT_FIELD) || (indir->AsField()->gtFldHnd == fieldSeq->GetTail()->m_fieldHnd));
        }
        else
        {
            // Normalize fieldSeq to null so we don't need to keep checking for both null and NotAField.
            fieldSeq = nullptr;
        }

        if (varTypeIsSIMD(indir->TypeGet()))
        {
            // TODO-ADDR: Skip SIMD indirs for now, SIMD typed LCL_FLDs works most of the time
            // but there are exceptions - fgMorphFieldAssignToSimdSetElement for example.
            // And more importantly, SIMD call args have to be wrapped in OBJ nodes currently.
            return;
        }

        if (indir->TypeGet() != TYP_STRUCT)
        {
            if ((fieldSeq != nullptr) && !indir->OperIs(GT_FIELD))
            {
                // If we have an indirection node and a field sequence then they should have the same type.
                // Otherwise it's best to forget the field sequence since the resulting LCL_FLD
                // doesn't match a real struct field. Value numbering protects itself from such
                // mismatches but there doesn't seem to be any good reason to generate a LCL_FLD
                // with a mismatched field sequence only to have to ignore it later.

                if (indir->TypeGet() !=
                    JITtype2varType(m_compiler->info.compCompHnd->getFieldType(fieldSeq->GetTail()->GetFieldHandle())))
                {
                    fieldSeq = nullptr;
                }
            }
        }
        else
        {
            if (indir->OperIs(GT_IND))
            {
                // Skip TYP_STRUCT IND nodes, it's not clear what we can do with them.
                // Normally these should appear only as sources of variable sized copy block
                // operations (DYN_BLK) so it probably doesn't make much sense to try to
                // convert these to local nodes.
                return;
            }

            if ((user == nullptr) || !user->OperIs(GT_ASG))
            {
                // TODO-ADDR: Skip TYP_STRUCT indirs for now, unless they're used by an ASG.
                // At least call args will require extra work because currently they must be
                // wrapped in OBJ nodes so we can't replace those with local nodes.
                return;
            }

            if (indir->OperIs(GT_FIELD))
            {
                CORINFO_CLASS_HANDLE fieldClassHandle;
                CorInfoType          corType =
                    m_compiler->info.compCompHnd->getFieldType(indir->AsField()->gtFldHnd, &fieldClassHandle);
                assert(corType == CORINFO_TYPE_VALUECLASS);

                structLayout = m_compiler->typGetObjLayout(fieldClassHandle);
            }
            else
            {
                structLayout = indir->AsBlk()->GetLayout();
            }

            // We're not going to produce a TYP_STRUCT LCL_FLD so we don't need the field sequence.
            fieldSeq = nullptr;
        }

        // We're only processing TYP_STRUCT variables now so the layout should never be null,
        // otherwise the below layout equality check would be insufficient.
        assert(varDsc->GetLayout() != nullptr);

        if ((val.Offset() == 0) && (structLayout != nullptr) &&
            ClassLayout::AreCompatible(structLayout, varDsc->GetLayout()))
        {
            indir->ChangeOper(GT_LCL_VAR);
            indir->AsLclVar()->SetLclNum(val.LclNum());
        }
        else if (!varTypeIsStruct(indir->TypeGet()))
        {
            indir->ChangeOper(GT_LCL_FLD);
            indir->AsLclFld()->SetLclNum(val.LclNum());
            indir->AsLclFld()->SetLclOffs(val.Offset());
            indir->AsLclFld()->SetFieldSeq(fieldSeq == nullptr ? FieldSeqStore::NotAField() : fieldSeq);

            // Promoted struct vars aren't currently handled here so the created LCL_FLD can't be
            // later transformed into a LCL_VAR and the variable cannot be enregistered.
            m_compiler->lvaSetVarDoNotEnregister(val.LclNum() DEBUGARG(Compiler::DNER_LocalField));
        }
        else
        {
            // TODO-ADDR: Add TYP_STRUCT support to LCL_FLD.
            return;
        }

        GenTreeFlags flags = GTF_EMPTY;

        if ((user != nullptr) && user->OperIs(GT_ASG) && (user->AsOp()->gtGetOp1() == indir))
        {
            flags |= GTF_VAR_DEF | GTF_DONT_CSE;

            if (indir->OperIs(GT_LCL_FLD))
            {
                // Currently we don't generate TYP_STRUCT LCL_FLDs so we do not need to
                // bother to find out the size of the LHS for "partial definition" purposes.
                assert(!varTypeIsStruct(indir->TypeGet()));

                if (genTypeSize(indir->TypeGet()) < m_compiler->lvaLclExactSize(val.LclNum()))
                {
                    flags |= GTF_VAR_USEASG;
                }
            }
        }

        indir->gtFlags = flags;

        INDEBUG(m_stmtModified = true;)
    }

    //------------------------------------------------------------------------
    // MorphStructField: Replaces a GT_FIELD based promoted/normed struct field access
    //    (e.g. FIELD(ADDR(LCL_VAR))) with a GT_LCL_VAR that references the struct field.
    //
    // Arguments:
    //    node - the GT_FIELD node
    //    user - the node that uses the field
    //
    // Notes:
    //    This does not do anything if the field access does not denote
    //    a promoted/normed struct field.
    //
    void MorphStructField(GenTree* node, GenTree* user)
    {
        assert(node->OperIs(GT_FIELD));
        // TODO-Cleanup: Move fgMorphStructField implementation here, it's not used anywhere else.
        m_compiler->fgMorphStructField(node, user);
        INDEBUG(m_stmtModified |= node->OperIs(GT_LCL_VAR);)
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
    //    If the GT_LCL_FLD offset does not have a coresponding promoted struct
    //    field then no transformation is done and struct local's enregistration
    //    is disabled.
    //
    void MorphLocalField(GenTree* node, GenTree* user)
    {
        assert(node->OperIs(GT_LCL_FLD));
        // TODO-Cleanup: Move fgMorphLocalField implementation here, it's not used anywhere else.
        m_compiler->fgMorphLocalField(node, user);
        INDEBUG(m_stmtModified |= node->OperIs(GT_LCL_VAR);)
    }

    //------------------------------------------------------------------------
    // UpdateEarlyRefCountForImplicitByRef: updates the ref count for implicit byref params.
    //
    // Arguments:
    //    lclNum - the local number to update the count for.
    //
    // Notes:
    //    fgMakeOutgoingStructArgCopy checks the ref counts for implicit byref params when it decides
    //    if it's legal to elide certain copies of them;
    //    fgRetypeImplicitByRefArgs checks the ref counts when it decides to undo promotions.
    //
    void UpdateEarlyRefCountForImplicitByRef(unsigned lclNum)
    {
        if (!m_compiler->lvaIsImplicitByRefLocal(lclNum))
        {
            return;
        }

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        JITDUMP("LocalAddressVisitor incrementing ref count from %d to %d for implicit by-ref V%02d\n",
                varDsc->lvRefCnt(RCS_EARLY), varDsc->lvRefCnt(RCS_EARLY) + 1, lclNum);
        varDsc->incLvRefCnt(1, RCS_EARLY);

        // See if this struct is an argument to a call. This information is recorded
        // via the weighted early ref count for the local, and feeds the undo promotion
        // heuristic.
        //
        // It can be approximate, so the pattern match below need not be exhaustive.
        // But the pattern should at least subset the implicit byref cases that are
        // handled in fgCanFastTailCall and fgMakeOutgoingStructArgCopy.
        //
        // CALL(OBJ(ADDR(LCL_VAR...)))
        bool isArgToCall   = false;
        bool keepSearching = true;
        for (int i = 0; i < m_ancestors.Height() && keepSearching; i++)
        {
            GenTree* node = m_ancestors.Top(i);
            switch (i)
            {
                case 0:
                {
                    keepSearching = node->OperIs(GT_LCL_VAR);
                }
                break;

                case 1:
                {
                    keepSearching = node->OperIs(GT_ADDR);
                }
                break;

                case 2:
                {
                    keepSearching = node->OperIs(GT_OBJ);
                }
                break;

                case 3:
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
};

//------------------------------------------------------------------------
// fgMarkAddressExposedLocals: Traverses the entire method and marks address
//    exposed locals.
//
// Notes:
//    Trees such as IND(ADDR(LCL_VAR)), that morph is expected to fold
//    to just LCL_VAR, do not result in the involved local being marked
//    address exposed.
//
void Compiler::fgMarkAddressExposedLocals()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgMarkAddressExposedLocals()\n");
    }
#endif // DEBUG

    LocalAddressVisitor visitor(this);

    for (BasicBlock* const block : Blocks())
    {
        // Make the current basic block address available globally
        compCurBB = block;

        for (Statement* const stmt : block->Statements())
        {
            visitor.VisitStmt(stmt);
        }
    }
}

//------------------------------------------------------------------------
// fgMarkAddressExposedLocals: Traverses the specified statement and marks address
//    exposed locals.
//
// Arguments:
//    stmt - the statement to traverse
//
// Notes:
//    Trees such as IND(ADDR(LCL_VAR)), that morph is expected to fold
//    to just LCL_VAR, do not result in the involved local being marked
//    address exposed.
//
void Compiler::fgMarkAddressExposedLocals(Statement* stmt)
{
    LocalAddressVisitor visitor(this);
    visitor.VisitStmt(stmt);
}
