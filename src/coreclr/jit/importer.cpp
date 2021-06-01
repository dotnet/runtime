
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Importer                                        XX
XX                                                                           XX
XX   Imports the given method and converts it to semantic trees              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "corexcep.h"

#define Verify(cond, msg)                                                                                              \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            verRaiseVerifyExceptionIfNeeded(INDEBUG(msg) DEBUGARG(__FILE__) DEBUGARG(__LINE__));                       \
        }                                                                                                              \
    } while (0)

#define VerifyOrReturn(cond, msg)                                                                                      \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            verRaiseVerifyExceptionIfNeeded(INDEBUG(msg) DEBUGARG(__FILE__) DEBUGARG(__LINE__));                       \
            return;                                                                                                    \
        }                                                                                                              \
    } while (0)

#define VerifyOrReturnSpeculative(cond, msg, speculative)                                                              \
    do                                                                                                                 \
    {                                                                                                                  \
        if (speculative)                                                                                               \
        {                                                                                                              \
            if (!(cond))                                                                                               \
            {                                                                                                          \
                return false;                                                                                          \
            }                                                                                                          \
        }                                                                                                              \
        else                                                                                                           \
        {                                                                                                              \
            if (!(cond))                                                                                               \
            {                                                                                                          \
                verRaiseVerifyExceptionIfNeeded(INDEBUG(msg) DEBUGARG(__FILE__) DEBUGARG(__LINE__));                   \
                return false;                                                                                          \
            }                                                                                                          \
        }                                                                                                              \
    } while (0)

/*****************************************************************************/

void Compiler::impInit()
{
    impStmtList = impLastStmt = nullptr;
#ifdef DEBUG
    impInlinedCodeSize = 0;
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Pushes the given tree on the stack.
 */

void Compiler::impPushOnStack(GenTree* tree, typeInfo ti)
{
    /* Check for overflow. If inlining, we may be using a bigger stack */

    if ((verCurrentState.esStackDepth >= info.compMaxStack) &&
        (verCurrentState.esStackDepth >= impStkSize || ((compCurBB->bbFlags & BBF_IMPORTED) == 0)))
    {
        BADCODE("stack overflow");
    }

#ifdef DEBUG
    // If we are pushing a struct, make certain we know the precise type!
    if (tree->TypeGet() == TYP_STRUCT)
    {
        assert(ti.IsType(TI_STRUCT));
        CORINFO_CLASS_HANDLE clsHnd = ti.GetClassHandle();
        assert(clsHnd != NO_CLASS_HANDLE);
    }

    if (tiVerificationNeeded && !ti.IsDead())
    {
        assert(typeInfo::AreEquivalent(NormaliseForStack(ti), ti)); // types are normalized

        // The ti type is consistent with the tree type.
        //

        // On 64-bit systems, nodes whose "proper" type is "native int" get labeled TYP_LONG.
        // In the verification type system, we always transform "native int" to "TI_INT".
        // Ideally, we would keep track of which nodes labeled "TYP_LONG" are really "native int", but
        // attempts to do that have proved too difficult.  Instead, we'll assume that in checks like this,
        // when there's a mismatch, it's because of this reason -- the typeInfo::AreEquivalentModuloNativeInt
        // method used in the last disjunct allows exactly this mismatch.
        assert(ti.IsDead() || (ti.IsByRef() && ((tree->TypeGet() == TYP_I_IMPL) || (tree->TypeGet() == TYP_BYREF))) ||
               (ti.IsUnboxedGenericTypeVar() && tree->TypeGet() == TYP_REF) ||
               (ti.IsObjRef() && tree->TypeGet() == TYP_REF) || (ti.IsMethod() && tree->TypeGet() == TYP_I_IMPL) ||
               (ti.IsType(TI_STRUCT) && tree->TypeGet() != TYP_REF) ||
               typeInfo::AreEquivalentModuloNativeInt(NormaliseForStack(ti),
                                                      NormaliseForStack(typeInfo(tree->TypeGet()))));

        // If it is a struct type, make certain we normalized the primitive types
        assert(!ti.IsType(TI_STRUCT) ||
               info.compCompHnd->getTypeForPrimitiveValueClass(ti.GetClassHandle()) == CORINFO_TYPE_UNDEF);
    }

#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        printf("\n");
        printf(TI_DUMP_PADDING);
        printf("About to push to stack: ");
        ti.Dump();
    }
#endif // VERBOSE_VERIFY

#endif // DEBUG

    verCurrentState.esStack[verCurrentState.esStackDepth].seTypeInfo = ti;
    verCurrentState.esStack[verCurrentState.esStackDepth++].val      = tree;

    if ((tree->gtType == TYP_LONG) && (compLongUsed == false))
    {
        compLongUsed = true;
    }
    else if (((tree->gtType == TYP_FLOAT) || (tree->gtType == TYP_DOUBLE)) && (compFloatingPointUsed == false))
    {
        compFloatingPointUsed = true;
    }
}

inline void Compiler::impPushNullObjRefOnStack()
{
    impPushOnStack(gtNewIconNode(0, TYP_REF), typeInfo(TI_NULL));
}

// This method gets called when we run into unverifiable code
// (and we are verifying the method)

inline void Compiler::verRaiseVerifyExceptionIfNeeded(INDEBUG(const char* msg) DEBUGARG(const char* file)
                                                          DEBUGARG(unsigned line))
{
#ifdef DEBUG
    const char* tail = strrchr(file, '\\');
    if (tail)
    {
        file = tail + 1;
    }

    if (JitConfig.JitBreakOnUnsafeCode())
    {
        assert(!"Unsafe code detected");
    }
#endif

    JITLOG((LL_INFO10000, "Detected unsafe code: %s:%d : %s, while compiling %s opcode %s, IL offset %x\n", file, line,
            msg, info.compFullName, impCurOpcName, impCurOpcOffs));

    if (compIsForImportOnly())
    {
        JITLOG((LL_ERROR, "Verification failure:  %s:%d : %s, while compiling %s opcode %s, IL offset %x\n", file, line,
                msg, info.compFullName, impCurOpcName, impCurOpcOffs));
        verRaiseVerifyException(INDEBUG(msg) DEBUGARG(file) DEBUGARG(line));
    }
}

inline void DECLSPEC_NORETURN Compiler::verRaiseVerifyException(INDEBUG(const char* msg) DEBUGARG(const char* file)
                                                                    DEBUGARG(unsigned line))
{
    JITLOG((LL_ERROR, "Verification failure:  %s:%d : %s, while compiling %s opcode %s, IL offset %x\n", file, line,
            msg, info.compFullName, impCurOpcName, impCurOpcOffs));

#ifdef DEBUG
    //    BreakIfDebuggerPresent();
    if (getBreakOnBadCode())
    {
        assert(!"Typechecking error");
    }
#endif

    RaiseException(SEH_VERIFICATION_EXCEPTION, EXCEPTION_NONCONTINUABLE, 0, nullptr);
    UNREACHABLE();
}

// helper function that will tell us if the IL instruction at the addr passed
// by param consumes an address at the top of the stack. We use it to save
// us lvAddrTaken
bool Compiler::impILConsumesAddr(const BYTE* codeAddr)
{
    assert(!compIsForInlining());

    OPCODE opcode;

    opcode = (OPCODE)getU1LittleEndian(codeAddr);

    switch (opcode)
    {
        // case CEE_LDFLDA: We're taking this one out as if you have a sequence
        // like
        //
        //          ldloca.0
        //          ldflda whatever
        //
        // of a primitivelike struct, you end up after morphing with addr of a local
        // that's not marked as addrtaken, which is wrong. Also ldflda is usually used
        // for structs that contain other structs, which isnt a case we handle very
        // well now for other reasons.

        case CEE_LDFLD:
        {
            // We won't collapse small fields. This is probably not the right place to have this
            // check, but we're only using the function for this purpose, and is easy to factor
            // out if we need to do so.

            CORINFO_RESOLVED_TOKEN resolvedToken;
            impResolveToken(codeAddr + sizeof(__int8), &resolvedToken, CORINFO_TOKENKIND_Field);

            var_types lclTyp = JITtype2varType(info.compCompHnd->getFieldType(resolvedToken.hField));

            // Preserve 'small' int types
            if (!varTypeIsSmall(lclTyp))
            {
                lclTyp = genActualType(lclTyp);
            }

            if (varTypeIsSmall(lclTyp))
            {
                return false;
            }

            return true;
        }
        default:
            break;
    }

    return false;
}

void Compiler::impResolveToken(const BYTE* addr, CORINFO_RESOLVED_TOKEN* pResolvedToken, CorInfoTokenKind kind)
{
    pResolvedToken->tokenContext = impTokenLookupContextHandle;
    pResolvedToken->tokenScope   = info.compScopeHnd;
    pResolvedToken->token        = getU4LittleEndian(addr);
    pResolvedToken->tokenType    = kind;

    if (!tiVerificationNeeded)
    {
        info.compCompHnd->resolveToken(pResolvedToken);
    }
    else
    {
        Verify(eeTryResolveToken(pResolvedToken), "Token resolution failed");
    }
}

/*****************************************************************************
 *
 *  Pop one tree from the stack.
 */

StackEntry Compiler::impPopStack()
{
    if (verCurrentState.esStackDepth == 0)
    {
        BADCODE("stack underflow");
    }

#ifdef DEBUG
#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        JITDUMP("\n");
        printf(TI_DUMP_PADDING);
        printf("About to pop from the stack: ");
        const typeInfo& ti = verCurrentState.esStack[verCurrentState.esStackDepth - 1].seTypeInfo;
        ti.Dump();
    }
#endif // VERBOSE_VERIFY
#endif // DEBUG

    return verCurrentState.esStack[--verCurrentState.esStackDepth];
}

/*****************************************************************************
 *
 *  Peep at n'th (0-based) tree on the top of the stack.
 */

StackEntry& Compiler::impStackTop(unsigned n)
{
    if (verCurrentState.esStackDepth <= n)
    {
        BADCODE("stack underflow");
    }

    return verCurrentState.esStack[verCurrentState.esStackDepth - n - 1];
}

unsigned Compiler::impStackHeight()
{
    return verCurrentState.esStackDepth;
}

/*****************************************************************************
 *  Some of the trees are spilled specially. While unspilling them, or
 *  making a copy, these need to be handled specially. The function
 *  enumerates the operators possible after spilling.
 */

#ifdef DEBUG // only used in asserts
static bool impValidSpilledStackEntry(GenTree* tree)
{
    if (tree->gtOper == GT_LCL_VAR)
    {
        return true;
    }

    if (tree->OperIsConst())
    {
        return true;
    }

    return false;
}
#endif

/*****************************************************************************
 *
 *  The following logic is used to save/restore stack contents.
 *  If 'copy' is true, then we make a copy of the trees on the stack. These
 *  have to all be cloneable/spilled values.
 */

void Compiler::impSaveStackState(SavedStack* savePtr, bool copy)
{
    savePtr->ssDepth = verCurrentState.esStackDepth;

    if (verCurrentState.esStackDepth)
    {
        savePtr->ssTrees = new (this, CMK_ImpStack) StackEntry[verCurrentState.esStackDepth];
        size_t saveSize  = verCurrentState.esStackDepth * sizeof(*savePtr->ssTrees);

        if (copy)
        {
            StackEntry* table = savePtr->ssTrees;

            /* Make a fresh copy of all the stack entries */

            for (unsigned level = 0; level < verCurrentState.esStackDepth; level++, table++)
            {
                table->seTypeInfo = verCurrentState.esStack[level].seTypeInfo;
                GenTree* tree     = verCurrentState.esStack[level].val;

                assert(impValidSpilledStackEntry(tree));

                switch (tree->gtOper)
                {
                    case GT_CNS_INT:
                    case GT_CNS_LNG:
                    case GT_CNS_DBL:
                    case GT_CNS_STR:
                    case GT_LCL_VAR:
                        table->val = gtCloneExpr(tree);
                        break;

                    default:
                        assert(!"Bad oper - Not covered by impValidSpilledStackEntry()");
                        break;
                }
            }
        }
        else
        {
            memcpy(savePtr->ssTrees, verCurrentState.esStack, saveSize);
        }
    }
}

void Compiler::impRestoreStackState(SavedStack* savePtr)
{
    verCurrentState.esStackDepth = savePtr->ssDepth;

    if (verCurrentState.esStackDepth)
    {
        memcpy(verCurrentState.esStack, savePtr->ssTrees,
               verCurrentState.esStackDepth * sizeof(*verCurrentState.esStack));
    }
}

//------------------------------------------------------------------------
// impBeginTreeList: Get the tree list started for a new basic block.
//
inline void Compiler::impBeginTreeList()
{
    assert(impStmtList == nullptr && impLastStmt == nullptr);
}

/*****************************************************************************
 *
 *  Store the given start and end stmt in the given basic block. This is
 *  mostly called by impEndTreeList(BasicBlock *block). It is called
 *  directly only for handling CEE_LEAVEs out of finally-protected try's.
 */

inline void Compiler::impEndTreeList(BasicBlock* block, Statement* firstStmt, Statement* lastStmt)
{
    /* Make the list circular, so that we can easily walk it backwards */

    firstStmt->SetPrevStmt(lastStmt);

    /* Store the tree list in the basic block */

    block->bbStmtList = firstStmt;

    /* The block should not already be marked as imported */
    assert((block->bbFlags & BBF_IMPORTED) == 0);

    block->bbFlags |= BBF_IMPORTED;
}

//------------------------------------------------------------------------
// impEndTreeList: Store the current tree list in the given basic block.
//
// Arguments:
//    block - the basic block to store into.
//
inline void Compiler::impEndTreeList(BasicBlock* block)
{
    if (impStmtList == nullptr)
    {
        // The block should not already be marked as imported.
        assert((block->bbFlags & BBF_IMPORTED) == 0);

        // Empty block. Just mark it as imported.
        block->bbFlags |= BBF_IMPORTED;
    }
    else
    {
        impEndTreeList(block, impStmtList, impLastStmt);
    }

#ifdef DEBUG
    if (impLastILoffsStmt != nullptr)
    {
        impLastILoffsStmt->SetLastILOffset(compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs);
        impLastILoffsStmt = nullptr;
    }
#endif
    impStmtList = impLastStmt = nullptr;
}

/*****************************************************************************
 *
 *  Check that storing the given tree doesnt mess up the semantic order. Note
 *  that this has only limited value as we can only check [0..chkLevel).
 */

inline void Compiler::impAppendStmtCheck(Statement* stmt, unsigned chkLevel)
{
#ifndef DEBUG
    return;
#else

    if (chkLevel == (unsigned)CHECK_SPILL_ALL)
    {
        chkLevel = verCurrentState.esStackDepth;
    }

    if (verCurrentState.esStackDepth == 0 || chkLevel == 0 || chkLevel == (unsigned)CHECK_SPILL_NONE)
    {
        return;
    }

    GenTree* tree = stmt->GetRootNode();

    // Calls can only be appended if there are no GTF_GLOB_EFFECT on the stack

    if (tree->gtFlags & GTF_CALL)
    {
        for (unsigned level = 0; level < chkLevel; level++)
        {
            assert((verCurrentState.esStack[level].val->gtFlags & GTF_GLOB_EFFECT) == 0);
        }
    }

    if (tree->gtOper == GT_ASG)
    {
        // For an assignment to a local variable, all references of that
        // variable have to be spilled. If it is aliased, all calls and
        // indirect accesses have to be spilled

        if (tree->AsOp()->gtOp1->gtOper == GT_LCL_VAR)
        {
            unsigned lclNum = tree->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
            for (unsigned level = 0; level < chkLevel; level++)
            {
                assert(!gtHasRef(verCurrentState.esStack[level].val, lclNum, false));
                assert(!lvaTable[lclNum].lvAddrExposed ||
                       (verCurrentState.esStack[level].val->gtFlags & GTF_SIDE_EFFECT) == 0);
            }
        }

        // If the access may be to global memory, all side effects have to be spilled.

        else if (tree->AsOp()->gtOp1->gtFlags & GTF_GLOB_REF)
        {
            for (unsigned level = 0; level < chkLevel; level++)
            {
                assert((verCurrentState.esStack[level].val->gtFlags & GTF_GLOB_REF) == 0);
            }
        }
    }
#endif
}

/*****************************************************************************
 *
 *  Append the given statement to the current block's tree list.
 *  [0..chkLevel) is the portion of the stack which we will check for
 *    interference with stmt and spill if needed.
 */

inline void Compiler::impAppendStmt(Statement* stmt, unsigned chkLevel)
{
    if (chkLevel == (unsigned)CHECK_SPILL_ALL)
    {
        chkLevel = verCurrentState.esStackDepth;
    }

    if ((chkLevel != 0) && (chkLevel != (unsigned)CHECK_SPILL_NONE))
    {
        assert(chkLevel <= verCurrentState.esStackDepth);

        /* If the statement being appended has any side-effects, check the stack
           to see if anything needs to be spilled to preserve correct ordering. */

        GenTree* expr  = stmt->GetRootNode();
        unsigned flags = expr->gtFlags & GTF_GLOB_EFFECT;

        // Assignment to (unaliased) locals don't count as a side-effect as
        // we handle them specially using impSpillLclRefs(). Temp locals should
        // be fine too.

        if ((expr->gtOper == GT_ASG) && (expr->AsOp()->gtOp1->gtOper == GT_LCL_VAR) &&
            ((expr->AsOp()->gtOp1->gtFlags & GTF_GLOB_REF) == 0) && !gtHasLocalsWithAddrOp(expr->AsOp()->gtOp2))
        {
            unsigned op2Flags = expr->AsOp()->gtOp2->gtFlags & GTF_GLOB_EFFECT;
            assert(flags == (op2Flags | GTF_ASG));
            flags = op2Flags;
        }

        if (flags != 0)
        {
            bool spillGlobEffects = false;

            if ((flags & GTF_CALL) != 0)
            {
                // If there is a call, we have to spill global refs
                spillGlobEffects = true;
            }
            else if (!expr->OperIs(GT_ASG))
            {
                if ((flags & GTF_ASG) != 0)
                {
                    // The expression is not an assignment node but it has an assignment side effect, it
                    // must be an atomic op, HW intrinsic or some other kind of node that stores to memory.
                    // Since we don't know what it assigns to, we need to spill global refs.
                    spillGlobEffects = true;
                }
            }
            else
            {
                GenTree* lhs = expr->gtGetOp1();
                GenTree* rhs = expr->gtGetOp2();

                if (((rhs->gtFlags | lhs->gtFlags) & GTF_ASG) != 0)
                {
                    // Either side of the assignment node has an assignment side effect.
                    // Since we don't know what it assigns to, we need to spill global refs.
                    spillGlobEffects = true;
                }
                else if ((lhs->gtFlags & GTF_GLOB_REF) != 0)
                {
                    spillGlobEffects = true;
                }
            }

            impSpillSideEffects(spillGlobEffects, chkLevel DEBUGARG("impAppendStmt"));
        }
        else
        {
            impSpillSpecialSideEff();
        }
    }

    impAppendStmtCheck(stmt, chkLevel);

    impAppendStmt(stmt);

#ifdef FEATURE_SIMD
    impMarkContiguousSIMDFieldAssignments(stmt);
#endif

    /* Once we set impCurStmtOffs in an appended tree, we are ready to
       report the following offsets. So reset impCurStmtOffs */

    if (impLastStmt->GetILOffsetX() == impCurStmtOffs)
    {
        impCurStmtOffsSet(BAD_IL_OFFSET);
    }

#ifdef DEBUG
    if (impLastILoffsStmt == nullptr)
    {
        impLastILoffsStmt = stmt;
    }

    if (verbose)
    {
        printf("\n\n");
        gtDispStmt(stmt);
    }
#endif
}

//------------------------------------------------------------------------
// impAppendStmt: Add the statement to the current stmts list.
//
// Arguments:
//    stmt - the statement to add.
//
inline void Compiler::impAppendStmt(Statement* stmt)
{
    if (impStmtList == nullptr)
    {
        // The stmt is the first in the list.
        impStmtList = stmt;
    }
    else
    {
        // Append the expression statement to the existing list.
        impLastStmt->SetNextStmt(stmt);
        stmt->SetPrevStmt(impLastStmt);
    }
    impLastStmt = stmt;
}

//------------------------------------------------------------------------
// impExtractLastStmt: Extract the last statement from the current stmts list.
//
// Return Value:
//    The extracted statement.
//
// Notes:
//    It assumes that the stmt will be reinserted later.
//
Statement* Compiler::impExtractLastStmt()
{
    assert(impLastStmt != nullptr);

    Statement* stmt = impLastStmt;
    impLastStmt     = impLastStmt->GetPrevStmt();
    if (impLastStmt == nullptr)
    {
        impStmtList = nullptr;
    }
    return stmt;
}

//-------------------------------------------------------------------------
// impInsertStmtBefore: Insert the given "stmt" before "stmtBefore".
//
// Arguments:
//    stmt       - a statement to insert;
//    stmtBefore - an insertion point to insert "stmt" before.
//
inline void Compiler::impInsertStmtBefore(Statement* stmt, Statement* stmtBefore)
{
    assert(stmt != nullptr);
    assert(stmtBefore != nullptr);

    if (stmtBefore == impStmtList)
    {
        impStmtList = stmt;
    }
    else
    {
        Statement* stmtPrev = stmtBefore->GetPrevStmt();
        stmt->SetPrevStmt(stmtPrev);
        stmtPrev->SetNextStmt(stmt);
    }
    stmt->SetNextStmt(stmtBefore);
    stmtBefore->SetPrevStmt(stmt);
}

/*****************************************************************************
 *
 *  Append the given expression tree to the current block's tree list.
 *  Return the newly created statement.
 */

Statement* Compiler::impAppendTree(GenTree* tree, unsigned chkLevel, IL_OFFSETX offset)
{
    assert(tree);

    /* Allocate an 'expression statement' node */

    Statement* stmt = gtNewStmt(tree, offset);

    /* Append the statement to the current block's stmt list */

    impAppendStmt(stmt, chkLevel);

    return stmt;
}

/*****************************************************************************
 *
 *  Insert the given expression tree before "stmtBefore"
 */

void Compiler::impInsertTreeBefore(GenTree* tree, IL_OFFSETX offset, Statement* stmtBefore)
{
    /* Allocate an 'expression statement' node */

    Statement* stmt = gtNewStmt(tree, offset);

    /* Append the statement to the current block's stmt list */

    impInsertStmtBefore(stmt, stmtBefore);
}

/*****************************************************************************
 *
 *  Append an assignment of the given value to a temp to the current tree list.
 *  curLevel is the stack level for which the spill to the temp is being done.
 */

void Compiler::impAssignTempGen(unsigned    tmp,
                                GenTree*    val,
                                unsigned    curLevel,
                                Statement** pAfterStmt, /* = NULL */
                                IL_OFFSETX  ilOffset,   /* = BAD_IL_OFFSET */
                                BasicBlock* block       /* = NULL */
                                )
{
    GenTree* asg = gtNewTempAssign(tmp, val);

    if (!asg->IsNothingNode())
    {
        if (pAfterStmt)
        {
            Statement* asgStmt = gtNewStmt(asg, ilOffset);
            fgInsertStmtAfter(block, *pAfterStmt, asgStmt);
            *pAfterStmt = asgStmt;
        }
        else
        {
            impAppendTree(asg, curLevel, impCurStmtOffs);
        }
    }
}

/*****************************************************************************
 * same as above, but handle the valueclass case too
 */

void Compiler::impAssignTempGen(unsigned             tmpNum,
                                GenTree*             val,
                                CORINFO_CLASS_HANDLE structType,
                                unsigned             curLevel,
                                Statement**          pAfterStmt, /* = NULL */
                                IL_OFFSETX           ilOffset,   /* = BAD_IL_OFFSET */
                                BasicBlock*          block       /* = NULL */
                                )
{
    GenTree* asg;

    assert(val->TypeGet() != TYP_STRUCT || structType != NO_CLASS_HANDLE);
    if (varTypeIsStruct(val) && (structType != NO_CLASS_HANDLE))
    {
        assert(tmpNum < lvaCount);
        assert(structType != NO_CLASS_HANDLE);

        // if the method is non-verifiable the assert is not true
        // so at least ignore it in the case when verification is turned on
        // since any block that tries to use the temp would have failed verification.
        var_types varType = lvaTable[tmpNum].lvType;
        assert(tiVerificationNeeded || varType == TYP_UNDEF || varTypeIsStruct(varType));
        lvaSetStruct(tmpNum, structType, false);

        varType = lvaTable[tmpNum].lvType;
        // Now, set the type of the struct value. Note that lvaSetStruct may modify the type
        // of the lclVar to a specialized type (e.g. TYP_SIMD), based on the handle (structType)
        // that has been passed in for the value being assigned to the temp, in which case we
        // need to set 'val' to that same type.
        // Note also that if we always normalized the types of any node that might be a struct
        // type, this would not be necessary - but that requires additional JIT/EE interface
        // calls that may not actually be required - e.g. if we only access a field of a struct.

        GenTree* dst = gtNewLclvNode(tmpNum, varType);
        asg          = impAssignStruct(dst, val, structType, curLevel, pAfterStmt, ilOffset, block);
    }
    else
    {
        asg = gtNewTempAssign(tmpNum, val);
    }

    if (!asg->IsNothingNode())
    {
        if (pAfterStmt)
        {
            Statement* asgStmt = gtNewStmt(asg, ilOffset);
            fgInsertStmtAfter(block, *pAfterStmt, asgStmt);
            *pAfterStmt = asgStmt;
        }
        else
        {
            impAppendTree(asg, curLevel, impCurStmtOffs);
        }
    }
}

/*****************************************************************************
 *
 *  Pop the given number of values from the stack and return a list node with
 *  their values.
 *  The 'prefixTree' argument may optionally contain an argument
 *  list that is prepended to the list returned from this function.
 *
 *  The notion of prepended is a bit misleading in that the list is backwards
 *  from the way I would expect: The first element popped is at the end of
 *  the returned list, and prefixTree is 'before' that, meaning closer to
 *  the end of the list.  To get to prefixTree, you have to walk to the
 *  end of the list.
 *
 *  For ARG_ORDER_R2L prefixTree is only used to insert extra arguments, as
 *  such we reverse its meaning such that returnValue has a reversed
 *  prefixTree at the head of the list.
 */

GenTreeCall::Use* Compiler::impPopCallArgs(unsigned count, CORINFO_SIG_INFO* sig, GenTreeCall::Use* prefixArgs)
{
    assert(sig == nullptr || count == sig->numArgs);

    CORINFO_CLASS_HANDLE structType;
    GenTreeCall::Use*    argList;

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
    {
        argList = nullptr;
    }
    else
    { // ARG_ORDER_L2R
        argList = prefixArgs;
    }

    while (count--)
    {
        StackEntry se   = impPopStack();
        typeInfo   ti   = se.seTypeInfo;
        GenTree*   temp = se.val;

        if (varTypeIsStruct(temp))
        {
            // Morph trees that aren't already OBJs or MKREFANY to be OBJs
            assert(ti.IsType(TI_STRUCT));
            structType = ti.GetClassHandleForValueClass();

            bool forceNormalization = false;
            if (varTypeIsSIMD(temp))
            {
                // We need to ensure that fgMorphArgs will use the correct struct handle to ensure proper
                // ABI handling of this argument.
                // Note that this can happen, for example, if we have a SIMD intrinsic that returns a SIMD type
                // with a different baseType than we've seen.
                // We also need to ensure an OBJ node if we have a FIELD node that might be transformed to LCL_FLD
                // or a plain GT_IND.
                // TODO-Cleanup: Consider whether we can eliminate all of these cases.
                if ((gtGetStructHandleIfPresent(temp) != structType) || temp->OperIs(GT_FIELD))
                {
                    forceNormalization = true;
                }
            }
#ifdef DEBUG
            if (verbose)
            {
                printf("Calling impNormStructVal on:\n");
                gtDispTree(temp);
            }
#endif
            temp = impNormStructVal(temp, structType, (unsigned)CHECK_SPILL_ALL, forceNormalization);
#ifdef DEBUG
            if (verbose)
            {
                printf("resulting tree:\n");
                gtDispTree(temp);
            }
#endif
        }

        /* NOTE: we defer bashing the type for I_IMPL to fgMorphArgs */
        argList = gtPrependNewCallArg(temp, argList);
    }

    if (sig != nullptr)
    {
        if (sig->retTypeSigClass != nullptr && sig->retType != CORINFO_TYPE_CLASS &&
            sig->retType != CORINFO_TYPE_BYREF && sig->retType != CORINFO_TYPE_PTR && sig->retType != CORINFO_TYPE_VAR)
        {
            // Make sure that all valuetypes (including enums) that we push are loaded.
            // This is to guarantee that if a GC is triggerred from the prestub of this methods,
            // all valuetypes in the method signature are already loaded.
            // We need to be able to find the size of the valuetypes, but we cannot
            // do a class-load from within GC.
            info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(sig->retTypeSigClass);
        }

        CORINFO_ARG_LIST_HANDLE sigArgs = sig->args;
        GenTreeCall::Use*       arg;

        for (arg = argList, count = sig->numArgs; count > 0; arg = arg->GetNext(), count--)
        {
            PREFIX_ASSUME(arg != nullptr);

            CORINFO_CLASS_HANDLE classHnd;
            CorInfoType          corType = strip(info.compCompHnd->getArgType(sig, sigArgs, &classHnd));

            var_types jitSigType = JITtype2varType(corType);

            if (!impCheckImplicitArgumentCoercion(jitSigType, arg->GetNode()->TypeGet()))
            {
                BADCODE("the call argument has a type that can't be implicitly converted to the signature type");
            }

            // insert implied casts (from float to double or double to float)
            if ((jitSigType == TYP_DOUBLE) && (arg->GetNode()->TypeGet() == TYP_FLOAT))
            {
                arg->SetNode(gtNewCastNode(TYP_DOUBLE, arg->GetNode(), false, TYP_DOUBLE));
            }
            else if ((jitSigType == TYP_FLOAT) && (arg->GetNode()->TypeGet() == TYP_DOUBLE))
            {
                arg->SetNode(gtNewCastNode(TYP_FLOAT, arg->GetNode(), false, TYP_FLOAT));
            }

            // insert any widening or narrowing casts for backwards compatibility
            arg->SetNode(impImplicitIorI4Cast(arg->GetNode(), jitSigType));

            if (corType != CORINFO_TYPE_CLASS && corType != CORINFO_TYPE_BYREF && corType != CORINFO_TYPE_PTR &&
                corType != CORINFO_TYPE_VAR)
            {
                CORINFO_CLASS_HANDLE argRealClass = info.compCompHnd->getArgClass(sig, sigArgs);
                if (argRealClass != nullptr)
                {
                    // Make sure that all valuetypes (including enums) that we push are loaded.
                    // This is to guarantee that if a GC is triggered from the prestub of this methods,
                    // all valuetypes in the method signature are already loaded.
                    // We need to be able to find the size of the valuetypes, but we cannot
                    // do a class-load from within GC.
                    info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(argRealClass);
                }
            }

            const var_types nodeArgType = arg->GetNode()->TypeGet();
            if (!varTypeIsStruct(jitSigType) && genTypeSize(nodeArgType) != genTypeSize(jitSigType))
            {
                assert(!varTypeIsStruct(nodeArgType));
                // Some ABI require precise size information for call arguments less than target pointer size,
                // for example arm64 OSX. Create a special node to keep this information until morph
                // consumes it into `fgArgInfo`.
                GenTree* putArgType = gtNewOperNode(GT_PUTARG_TYPE, jitSigType, arg->GetNode());
                arg->SetNode(putArgType);
            }

            sigArgs = info.compCompHnd->getArgNext(sigArgs);
        }
    }

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
    {
        // Prepend the prefixTree

        // Simple in-place reversal to place treeList
        // at the end of a reversed prefixTree
        while (prefixArgs != nullptr)
        {
            GenTreeCall::Use* next = prefixArgs->GetNext();
            prefixArgs->SetNext(argList);
            argList    = prefixArgs;
            prefixArgs = next;
        }
    }
    return argList;
}

static bool TypeIs(var_types type1, var_types type2)
{
    return type1 == type2;
}

// Check if type1 matches any type from the list.
template <typename... T>
static bool TypeIs(var_types type1, var_types type2, T... rest)
{
    return TypeIs(type1, type2) || TypeIs(type1, rest...);
}

//------------------------------------------------------------------------
// impCheckImplicitArgumentCoercion: check that the node's type is compatible with
//   the signature's type using ECMA implicit argument coercion table.
//
// Arguments:
//    sigType  - the type in the call signature;
//    nodeType - the node type.
//
// Return Value:
//    true if they are compatible, false otherwise.
//
// Notes:
//   - it is currently allowing byref->long passing, should be fixed in VM;
//   - it can't check long -> native int case on 64-bit platforms,
//      so the behavior is different depending on the target bitness.
//
bool Compiler::impCheckImplicitArgumentCoercion(var_types sigType, var_types nodeType) const
{
    if (sigType == nodeType)
    {
        return true;
    }

    if (TypeIs(sigType, TYP_BOOL, TYP_UBYTE, TYP_BYTE, TYP_USHORT, TYP_SHORT, TYP_UINT, TYP_INT))
    {
        if (TypeIs(nodeType, TYP_BOOL, TYP_UBYTE, TYP_BYTE, TYP_USHORT, TYP_SHORT, TYP_UINT, TYP_INT, TYP_I_IMPL))
        {
            return true;
        }
    }
    else if (TypeIs(sigType, TYP_ULONG, TYP_LONG))
    {
        if (TypeIs(nodeType, TYP_LONG))
        {
            return true;
        }
    }
    else if (TypeIs(sigType, TYP_FLOAT, TYP_DOUBLE))
    {
        if (TypeIs(nodeType, TYP_FLOAT, TYP_DOUBLE))
        {
            return true;
        }
    }
    else if (TypeIs(sigType, TYP_BYREF))
    {
        if (TypeIs(nodeType, TYP_I_IMPL))
        {
            return true;
        }

        // This condition tolerates such IL:
        // ;  V00 this              ref  this class-hnd
        // ldarg.0
        // call(byref)
        if (TypeIs(nodeType, TYP_REF))
        {
            return true;
        }
    }
    else if (varTypeIsStruct(sigType))
    {
        if (varTypeIsStruct(nodeType))
        {
            return true;
        }
    }

    // This condition should not be under `else` because `TYP_I_IMPL`
    // intersects with `TYP_LONG` or `TYP_INT`.
    if (TypeIs(sigType, TYP_I_IMPL, TYP_U_IMPL))
    {
        // Note that it allows `ldc.i8 1; call(nint)` on 64-bit platforms,
        // but we can't distinguish `nint` from `long` there.
        if (TypeIs(nodeType, TYP_I_IMPL, TYP_U_IMPL, TYP_INT, TYP_UINT))
        {
            return true;
        }

        // It tolerates IL that ECMA does not allow but that is commonly used.
        // Example:
        //   V02 loc1           struct <RTL_OSVERSIONINFOEX, 32>
        //   ldloca.s     0x2
        //   call(native int)
        if (TypeIs(nodeType, TYP_BYREF))
        {
            return true;
        }
    }

    return false;
}

/*****************************************************************************
 *
 *  Pop the given number of values from the stack in reverse order (STDCALL/CDECL etc.)
 *  The first "skipReverseCount" items are not reversed.
 */

GenTreeCall::Use* Compiler::impPopReverseCallArgs(unsigned count, CORINFO_SIG_INFO* sig, unsigned skipReverseCount)
{
    assert(skipReverseCount <= count);

    GenTreeCall::Use* list = impPopCallArgs(count, sig);

    // reverse the list
    if (list == nullptr || skipReverseCount == count)
    {
        return list;
    }

    GenTreeCall::Use* ptr          = nullptr; // Initialized to the first node that needs to be reversed
    GenTreeCall::Use* lastSkipNode = nullptr; // Will be set to the last node that does not need to be reversed

    if (skipReverseCount == 0)
    {
        ptr = list;
    }
    else
    {
        lastSkipNode = list;
        // Get to the first node that needs to be reversed
        for (unsigned i = 0; i < skipReverseCount - 1; i++)
        {
            lastSkipNode = lastSkipNode->GetNext();
        }

        PREFIX_ASSUME(lastSkipNode != nullptr);
        ptr = lastSkipNode->GetNext();
    }

    GenTreeCall::Use* reversedList = nullptr;

    do
    {
        GenTreeCall::Use* tmp = ptr->GetNext();
        ptr->SetNext(reversedList);
        reversedList = ptr;
        ptr          = tmp;
    } while (ptr != nullptr);

    if (skipReverseCount)
    {
        lastSkipNode->SetNext(reversedList);
        return list;
    }
    else
    {
        return reversedList;
    }
}

//------------------------------------------------------------------------
// impAssignStruct: Create a struct assignment
//
// Arguments:
//    dest         - the destination of the assignment
//    src          - the value to be assigned
//    structHnd    - handle representing the struct type
//    curLevel     - stack level for which a spill may be being done
//    pAfterStmt   - statement to insert any additional statements after
//    ilOffset     - il offset for new statements
//    block        - block to insert any additional statements in
//
// Return Value:
//    The tree that should be appended to the statement list that represents the assignment.
//
// Notes:
//    Temp assignments may be appended to impStmtList if spilling is necessary.

GenTree* Compiler::impAssignStruct(GenTree*             dest,
                                   GenTree*             src,
                                   CORINFO_CLASS_HANDLE structHnd,
                                   unsigned             curLevel,
                                   Statement**          pAfterStmt, /* = nullptr */
                                   IL_OFFSETX           ilOffset,   /* = BAD_IL_OFFSET */
                                   BasicBlock*          block       /* = nullptr */
                                   )
{
    assert(varTypeIsStruct(dest));

    if (ilOffset == BAD_IL_OFFSET)
    {
        ilOffset = impCurStmtOffs;
    }

    while (dest->gtOper == GT_COMMA)
    {
        // Second thing is the struct.
        assert(varTypeIsStruct(dest->AsOp()->gtOp2));

        // Append all the op1 of GT_COMMA trees before we evaluate op2 of the GT_COMMA tree.
        if (pAfterStmt)
        {
            Statement* newStmt = gtNewStmt(dest->AsOp()->gtOp1, ilOffset);
            fgInsertStmtAfter(block, *pAfterStmt, newStmt);
            *pAfterStmt = newStmt;
        }
        else
        {
            impAppendTree(dest->AsOp()->gtOp1, curLevel, ilOffset); // do the side effect
        }

        // set dest to the second thing
        dest = dest->AsOp()->gtOp2;
    }

    assert(dest->gtOper == GT_LCL_VAR || dest->gtOper == GT_RETURN || dest->gtOper == GT_FIELD ||
           dest->gtOper == GT_IND || dest->gtOper == GT_OBJ || dest->gtOper == GT_INDEX);

    // Return a NOP if this is a self-assignment.
    if (dest->OperGet() == GT_LCL_VAR && src->OperGet() == GT_LCL_VAR &&
        src->AsLclVarCommon()->GetLclNum() == dest->AsLclVarCommon()->GetLclNum())
    {
        return gtNewNothingNode();
    }

    // TODO-1stClassStructs: Avoid creating an address if it is not needed,
    // or re-creating a Blk node if it is.
    GenTree* destAddr;

    if (dest->gtOper == GT_IND || dest->OperIsBlk())
    {
        destAddr = dest->AsOp()->gtOp1;
    }
    else
    {
        destAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);
    }

    return (impAssignStructPtr(destAddr, src, structHnd, curLevel, pAfterStmt, ilOffset, block));
}

//------------------------------------------------------------------------
// impAssignStructPtr: Assign (copy) the structure from 'src' to 'destAddr'.
//
// Arguments:
//    destAddr     - address of the destination of the assignment
//    src          - source of the assignment
//    structHnd    - handle representing the struct type
//    curLevel     - stack level for which a spill may be being done
//    pAfterStmt   - statement to insert any additional statements after
//    ilOffset     - il offset for new statements
//    block        - block to insert any additional statements in
//
// Return Value:
//    The tree that should be appended to the statement list that represents the assignment.
//
// Notes:
//    Temp assignments may be appended to impStmtList if spilling is necessary.

GenTree* Compiler::impAssignStructPtr(GenTree*             destAddr,
                                      GenTree*             src,
                                      CORINFO_CLASS_HANDLE structHnd,
                                      unsigned             curLevel,
                                      Statement**          pAfterStmt, /* = NULL */
                                      IL_OFFSETX           ilOffset,   /* = BAD_IL_OFFSET */
                                      BasicBlock*          block       /* = NULL */
                                      )
{
    GenTree*     dest      = nullptr;
    GenTreeFlags destFlags = GTF_EMPTY;

    if (ilOffset == BAD_IL_OFFSET)
    {
        ilOffset = impCurStmtOffs;
    }

    assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_FIELD, GT_IND, GT_OBJ, GT_CALL, GT_MKREFANY, GT_RET_EXPR, GT_COMMA) ||
           (src->TypeGet() != TYP_STRUCT && src->OperIsSimdOrHWintrinsic()));

    var_types asgType = src->TypeGet();

    if (src->gtOper == GT_CALL)
    {
        GenTreeCall* srcCall = src->AsCall();
        if (srcCall->TreatAsHasRetBufArg(this))
        {
            // Case of call returning a struct via hidden retbuf arg
            CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_WINDOWS) && !defined(TARGET_ARM)
            // Unmanaged instance methods on Windows need the retbuf arg after the first (this) parameter
            if (srcCall->IsUnmanaged())
            {
                if (callConvIsInstanceMethodCallConv(srcCall->GetUnmanagedCallConv()))
                {
#ifdef TARGET_X86
                    // The argument list has already been reversed.
                    // Insert the return buffer as the second-to-last node
                    // so it will be pushed on to the stack after the user args but before the native this arg
                    // as required by the native ABI.
                    GenTreeCall::Use* lastArg = srcCall->gtCallArgs;
                    if (lastArg == nullptr)
                    {
                        srcCall->gtCallArgs = gtPrependNewCallArg(destAddr, srcCall->gtCallArgs);
                    }
                    else if (srcCall->GetUnmanagedCallConv() == CorInfoCallConvExtension::Thiscall)
                    {
                        // For thiscall, the "this" parameter is not included in the argument list reversal,
                        // so we need to put the return buffer as the last parameter.
                        for (; lastArg->GetNext() != nullptr; lastArg = lastArg->GetNext())
                            ;
                        gtInsertNewCallArgAfter(destAddr, lastArg);
                    }
                    else if (lastArg->GetNext() == nullptr)
                    {
                        srcCall->gtCallArgs = gtPrependNewCallArg(destAddr, lastArg);
                    }
                    else
                    {
                        assert(lastArg != nullptr && lastArg->GetNext() != nullptr);
                        GenTreeCall::Use* secondLastArg = lastArg;
                        lastArg                         = lastArg->GetNext();
                        for (; lastArg->GetNext() != nullptr; secondLastArg = lastArg, lastArg = lastArg->GetNext())
                            ;
                        assert(secondLastArg->GetNext() != nullptr);
                        gtInsertNewCallArgAfter(destAddr, secondLastArg);
                    }
#else
                    GenTreeCall::Use* thisArg = gtInsertNewCallArgAfter(destAddr, srcCall->gtCallArgs);
#endif
                }
                else
                {
#ifdef TARGET_X86
                    // The argument list has already been reversed.
                    // Insert the return buffer as the last node so it will be pushed on to the stack last
                    // as required by the native ABI.
                    GenTreeCall::Use* lastArg = srcCall->gtCallArgs;
                    if (lastArg == nullptr)
                    {
                        srcCall->gtCallArgs = gtPrependNewCallArg(destAddr, srcCall->gtCallArgs);
                    }
                    else
                    {
                        for (; lastArg->GetNext() != nullptr; lastArg = lastArg->GetNext())
                            ;
                        gtInsertNewCallArgAfter(destAddr, lastArg);
                    }
#else
                    // insert the return value buffer into the argument list as first byref parameter
                    srcCall->gtCallArgs = gtPrependNewCallArg(destAddr, srcCall->gtCallArgs);
#endif
                }
            }
            else
#endif
            {
                // insert the return value buffer into the argument list as first byref parameter
                srcCall->gtCallArgs = gtPrependNewCallArg(destAddr, srcCall->gtCallArgs);
            }

            // now returns void, not a struct
            src->gtType = TYP_VOID;

            // return the morphed call node
            return src;
        }
        else
        {
            // Case of call returning a struct in one or more registers.

            var_types returnType = (var_types)srcCall->gtReturnType;

            // First we try to change this to "LclVar/LclFld = call"
            //
            if ((destAddr->gtOper == GT_ADDR) && (destAddr->AsOp()->gtOp1->gtOper == GT_LCL_VAR))
            {
                // If it is a multi-reg struct return, don't change the oper to GT_LCL_FLD.
                // That is, the IR will be of the form lclVar = call for multi-reg return
                //
                GenTreeLclVar* lcl    = destAddr->AsOp()->gtOp1->AsLclVar();
                unsigned       lclNum = lcl->GetLclNum();
                LclVarDsc*     varDsc = lvaGetDesc(lclNum);
                if (src->AsCall()->HasMultiRegRetVal())
                {
                    // Mark the struct LclVar as used in a MultiReg return context
                    //  which currently makes it non promotable.
                    // TODO-1stClassStructs: Eliminate this pessimization when we can more generally
                    // handle multireg returns.
                    lcl->gtFlags |= GTF_DONT_CSE;
                    varDsc->lvIsMultiRegRet = true;
                }

                dest = lcl;

#if defined(TARGET_ARM)
                // TODO-Cleanup: This should have been taken care of in the above HasMultiRegRetVal() case,
                // but that method has not been updadted to include ARM.
                impMarkLclDstNotPromotable(lclNum, src, structHnd);
                lcl->gtFlags |= GTF_DONT_CSE;
#elif defined(UNIX_AMD64_ABI)
                // Not allowed for FEATURE_CORCLR which is the only SKU available for System V OSs.
                assert(!src->AsCall()->IsVarargs() && "varargs not allowed for System V OSs.");

                // Make the struct non promotable. The eightbytes could contain multiple fields.
                // TODO-1stClassStructs: Eliminate this pessimization when we can more generally
                // handle multireg returns.
                // TODO-Cleanup: Why is this needed here? This seems that it will set this even for
                // non-multireg returns.
                lcl->gtFlags |= GTF_DONT_CSE;
                varDsc->lvIsMultiRegRet = true;
#endif
            }
            else // we don't have a GT_ADDR of a GT_LCL_VAR
            {
                // !!! The destination could be on stack. !!!
                // This flag will let us choose the correct write barrier.
                asgType   = returnType;
                destFlags = GTF_IND_TGTANYWHERE;
            }
        }
    }
    else if (src->gtOper == GT_RET_EXPR)
    {
        GenTreeCall* call = src->AsRetExpr()->gtInlineCandidate->AsCall();
        noway_assert(call->gtOper == GT_CALL);

        if (call->HasRetBufArg())
        {
            // insert the return value buffer into the argument list as first byref parameter
            call->gtCallArgs = gtPrependNewCallArg(destAddr, call->gtCallArgs);

            // now returns void, not a struct
            src->gtType  = TYP_VOID;
            call->gtType = TYP_VOID;

            // We already have appended the write to 'dest' GT_CALL's args
            // So now we just return an empty node (pruning the GT_RET_EXPR)
            return src;
        }
        else
        {
            // Case of inline method returning a struct in one or more registers.
            // We won't need a return buffer
            asgType = src->gtType;

            if ((destAddr->gtOper != GT_ADDR) || (destAddr->AsOp()->gtOp1->gtOper != GT_LCL_VAR))
            {
                // !!! The destination could be on stack. !!!
                // This flag will let us choose the correct write barrier.
                destFlags = GTF_IND_TGTANYWHERE;
            }
        }
    }
    else if (src->OperIsBlk())
    {
        asgType = impNormStructType(structHnd);
        if (src->gtOper == GT_OBJ)
        {
            assert(src->AsObj()->GetLayout()->GetClassHandle() == structHnd);
        }
    }
    else if (src->gtOper == GT_INDEX)
    {
        asgType = impNormStructType(structHnd);
        assert(src->AsIndex()->gtStructElemClass == structHnd);
    }
    else if (src->gtOper == GT_MKREFANY)
    {
        // Since we are assigning the result of a GT_MKREFANY,
        // "destAddr" must point to a refany.

        GenTree* destAddrClone;
        destAddr =
            impCloneExpr(destAddr, &destAddrClone, structHnd, curLevel, pAfterStmt DEBUGARG("MKREFANY assignment"));

        assert(OFFSETOF__CORINFO_TypedReference__dataPtr == 0);
        assert(destAddr->gtType == TYP_I_IMPL || destAddr->gtType == TYP_BYREF);
        fgAddFieldSeqForZeroOffset(destAddr, GetFieldSeqStore()->CreateSingleton(GetRefanyDataField()));

        GenTree*       ptrSlot         = gtNewOperNode(GT_IND, TYP_I_IMPL, destAddr);
        GenTreeIntCon* typeFieldOffset = gtNewIconNode(OFFSETOF__CORINFO_TypedReference__type, TYP_I_IMPL);
        typeFieldOffset->gtFieldSeq    = GetFieldSeqStore()->CreateSingleton(GetRefanyTypeField());
        GenTree* typeSlot =
            gtNewOperNode(GT_IND, TYP_I_IMPL, gtNewOperNode(GT_ADD, destAddr->gtType, destAddrClone, typeFieldOffset));

        // append the assign of the pointer value
        GenTree* asg = gtNewAssignNode(ptrSlot, src->AsOp()->gtOp1);
        if (pAfterStmt)
        {
            Statement* newStmt = gtNewStmt(asg, ilOffset);
            fgInsertStmtAfter(block, *pAfterStmt, newStmt);
            *pAfterStmt = newStmt;
        }
        else
        {
            impAppendTree(asg, curLevel, ilOffset);
        }

        // return the assign of the type value, to be appended
        return gtNewAssignNode(typeSlot, src->AsOp()->gtOp2);
    }
    else if (src->gtOper == GT_COMMA)
    {
        // The second thing is the struct or its address.
        assert(varTypeIsStruct(src->AsOp()->gtOp2) || src->AsOp()->gtOp2->gtType == TYP_BYREF);
        if (pAfterStmt)
        {
            // Insert op1 after '*pAfterStmt'
            Statement* newStmt = gtNewStmt(src->AsOp()->gtOp1, ilOffset);
            fgInsertStmtAfter(block, *pAfterStmt, newStmt);
            *pAfterStmt = newStmt;
        }
        else if (impLastStmt != nullptr)
        {
            // Do the side-effect as a separate statement.
            impAppendTree(src->AsOp()->gtOp1, curLevel, ilOffset);
        }
        else
        {
            // In this case we have neither been given a statement to insert after, nor are we
            // in the importer where we can append the side effect.
            // Instead, we're going to sink the assignment below the COMMA.
            src->AsOp()->gtOp2 =
                impAssignStructPtr(destAddr, src->AsOp()->gtOp2, structHnd, curLevel, pAfterStmt, ilOffset, block);
            return src;
        }

        // Evaluate the second thing using recursion.
        return impAssignStructPtr(destAddr, src->AsOp()->gtOp2, structHnd, curLevel, pAfterStmt, ilOffset, block);
    }
    else if (src->IsLocal())
    {
        asgType = src->TypeGet();
    }
    else if (asgType == TYP_STRUCT)
    {
        // It should already have the appropriate type.
        assert(asgType == impNormStructType(structHnd));
    }
    if ((dest == nullptr) && (destAddr->OperGet() == GT_ADDR))
    {
        GenTree* destNode = destAddr->gtGetOp1();
        // If the actual destination is a local, a GT_INDEX or a block node, or is a node that
        // will be morphed, don't insert an OBJ(ADDR) if it already has the right type.
        if (destNode->OperIs(GT_LCL_VAR, GT_INDEX) || destNode->OperIsBlk())
        {
            var_types destType = destNode->TypeGet();
            // If one or both types are TYP_STRUCT (one may not yet be normalized), they are compatible
            // iff their handles are the same.
            // Otherwise, they are compatible if their types are the same.
            bool typesAreCompatible =
                ((destType == TYP_STRUCT) || (asgType == TYP_STRUCT))
                    ? ((gtGetStructHandleIfPresent(destNode) == structHnd) && varTypeIsStruct(asgType))
                    : (destType == asgType);
            if (typesAreCompatible)
            {
                dest = destNode;
                if (destType != TYP_STRUCT)
                {
                    // Use a normalized type if available. We know from above that they're equivalent.
                    asgType = destType;
                }
            }
        }
    }

    if (dest == nullptr)
    {
        if (asgType == TYP_STRUCT)
        {
            dest = gtNewObjNode(structHnd, destAddr);
            gtSetObjGcInfo(dest->AsObj());
            // Although an obj as a call argument was always assumed to be a globRef
            // (which is itself overly conservative), that is not true of the operands
            // of a block assignment.
            dest->gtFlags &= ~GTF_GLOB_REF;
            dest->gtFlags |= (destAddr->gtFlags & GTF_GLOB_REF);
        }
        else
        {
            dest = gtNewOperNode(GT_IND, asgType, destAddr);
        }
    }

    if (dest->OperIs(GT_LCL_VAR) &&
        (src->IsMultiRegNode() ||
         (src->OperIs(GT_RET_EXPR) && src->AsRetExpr()->gtInlineCandidate->AsCall()->HasMultiRegRetVal())))
    {
        if (lvaEnregMultiRegVars && varTypeIsStruct(dest))
        {
            dest->AsLclVar()->SetMultiReg();
        }
        if (src->OperIs(GT_CALL))
        {
            lvaGetDesc(dest->AsLclVar())->lvIsMultiRegRet = true;
        }
    }

    dest->gtFlags |= destFlags;
    destFlags = dest->gtFlags;

    // return an assignment node, to be appended
    GenTree* asgNode = gtNewAssignNode(dest, src);
    gtBlockOpInit(asgNode, dest, src, false);

    // TODO-1stClassStructs: Clean up the settings of GTF_DONT_CSE on the lhs
    // of assignments.
    if ((destFlags & GTF_DONT_CSE) == 0)
    {
        dest->gtFlags &= ~(GTF_DONT_CSE);
    }
    return asgNode;
}

/*****************************************************************************
   Given a struct value, and the class handle for that structure, return
   the expression for the address for that structure value.

   willDeref - does the caller guarantee to dereference the pointer.
*/

GenTree* Compiler::impGetStructAddr(GenTree*             structVal,
                                    CORINFO_CLASS_HANDLE structHnd,
                                    unsigned             curLevel,
                                    bool                 willDeref)
{
    assert(varTypeIsStruct(structVal) || eeIsValueClass(structHnd));

    var_types type = structVal->TypeGet();

    genTreeOps oper = structVal->gtOper;

    if (oper == GT_OBJ && willDeref)
    {
        assert(structVal->AsObj()->GetLayout()->GetClassHandle() == structHnd);
        return (structVal->AsObj()->Addr());
    }
    else if (oper == GT_CALL || oper == GT_RET_EXPR || oper == GT_OBJ || oper == GT_MKREFANY ||
             structVal->OperIsSimdOrHWintrinsic())
    {
        unsigned tmpNum = lvaGrabTemp(true DEBUGARG("struct address for call/obj"));

        impAssignTempGen(tmpNum, structVal, structHnd, curLevel);

        // The 'return value' is now the temp itself

        type          = genActualType(lvaTable[tmpNum].TypeGet());
        GenTree* temp = gtNewLclvNode(tmpNum, type);
        temp          = gtNewOperNode(GT_ADDR, TYP_BYREF, temp);
        return temp;
    }
    else if (oper == GT_COMMA)
    {
        assert(structVal->AsOp()->gtOp2->gtType == type); // Second thing is the struct

        Statement* oldLastStmt   = impLastStmt;
        structVal->AsOp()->gtOp2 = impGetStructAddr(structVal->AsOp()->gtOp2, structHnd, curLevel, willDeref);
        structVal->gtType        = TYP_BYREF;

        if (oldLastStmt != impLastStmt)
        {
            // Some temp assignment statement was placed on the statement list
            // for Op2, but that would be out of order with op1, so we need to
            // spill op1 onto the statement list after whatever was last
            // before we recursed on Op2 (i.e. before whatever Op2 appended).
            Statement* beforeStmt;
            if (oldLastStmt == nullptr)
            {
                // The op1 stmt should be the first in the list.
                beforeStmt = impStmtList;
            }
            else
            {
                // Insert after the oldLastStmt before the first inserted for op2.
                beforeStmt = oldLastStmt->GetNextStmt();
            }

            impInsertTreeBefore(structVal->AsOp()->gtOp1, impCurStmtOffs, beforeStmt);
            structVal->AsOp()->gtOp1 = gtNewNothingNode();
        }

        return (structVal);
    }

    return (gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
}

//------------------------------------------------------------------------
// impNormStructType: Normalize the type of a (known to be) struct class handle.
//
// Arguments:
//    structHnd        - The class handle for the struct type of interest.
//    pSimdBaseJitType - (optional, default nullptr) - if non-null, and the struct is a SIMD
//                       type, set to the SIMD base JIT type
//
// Return Value:
//    The JIT type for the struct (e.g. TYP_STRUCT, or TYP_SIMD*).
//    It may also modify the compFloatingPointUsed flag if the type is a SIMD type.
//
// Notes:
//    Normalizing the type involves examining the struct type to determine if it should
//    be modified to one that is handled specially by the JIT, possibly being a candidate
//    for full enregistration, e.g. TYP_SIMD16. If the size of the struct is already known
//    call structSizeMightRepresentSIMDType to determine if this api needs to be called.

var_types Compiler::impNormStructType(CORINFO_CLASS_HANDLE structHnd, CorInfoType* pSimdBaseJitType)
{
    assert(structHnd != NO_CLASS_HANDLE);

    var_types structType = TYP_STRUCT;

#ifdef FEATURE_SIMD
    if (supportSIMDTypes())
    {
        const DWORD structFlags = info.compCompHnd->getClassAttribs(structHnd);

        // Don't bother if the struct contains GC references of byrefs, it can't be a SIMD type.
        if ((structFlags & (CORINFO_FLG_CONTAINS_GC_PTR | CORINFO_FLG_CONTAINS_STACK_PTR)) == 0)
        {
            unsigned originalSize = info.compCompHnd->getClassSize(structHnd);

            if (structSizeMightRepresentSIMDType(originalSize))
            {
                unsigned int sizeBytes;
                CorInfoType  simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(structHnd, &sizeBytes);
                if (simdBaseJitType != CORINFO_TYPE_UNDEF)
                {
                    assert(sizeBytes == originalSize);
                    structType = getSIMDTypeForSize(sizeBytes);
                    if (pSimdBaseJitType != nullptr)
                    {
                        *pSimdBaseJitType = simdBaseJitType;
                    }
                    // Also indicate that we use floating point registers.
                    compFloatingPointUsed = true;
                }
            }
        }
    }
#endif // FEATURE_SIMD

    return structType;
}

//------------------------------------------------------------------------
//  Compiler::impNormStructVal: Normalize a struct value
//
//  Arguments:
//     structVal          - the node we are going to normalize
//     structHnd          - the class handle for the node
//     curLevel           - the current stack level
//     forceNormalization - Force the creation of an OBJ node (default is false).
//
// Notes:
//     Given struct value 'structVal', make sure it is 'canonical', that is
//     it is either:
//     - a known struct type (non-TYP_STRUCT, e.g. TYP_SIMD8)
//     - an OBJ or a MKREFANY node, or
//     - a node (e.g. GT_INDEX) that will be morphed.
//    If the node is a CALL or RET_EXPR, a copy will be made to a new temp.
//
GenTree* Compiler::impNormStructVal(GenTree*             structVal,
                                    CORINFO_CLASS_HANDLE structHnd,
                                    unsigned             curLevel,
                                    bool                 forceNormalization /*=false*/)
{
    assert(forceNormalization || varTypeIsStruct(structVal));
    assert(structHnd != NO_CLASS_HANDLE);
    var_types structType = structVal->TypeGet();
    bool      makeTemp   = false;
    if (structType == TYP_STRUCT)
    {
        structType = impNormStructType(structHnd);
    }
    bool                 alreadyNormalized = false;
    GenTreeLclVarCommon* structLcl         = nullptr;

    genTreeOps oper = structVal->OperGet();
    switch (oper)
    {
        // GT_RETURN and GT_MKREFANY don't capture the handle.
        case GT_RETURN:
            break;
        case GT_MKREFANY:
            alreadyNormalized = true;
            break;

        case GT_CALL:
            structVal->AsCall()->gtRetClsHnd = structHnd;
            makeTemp                         = true;
            break;

        case GT_RET_EXPR:
            structVal->AsRetExpr()->gtRetClsHnd = structHnd;
            makeTemp                            = true;
            break;

        case GT_ARGPLACE:
            structVal->AsArgPlace()->gtArgPlaceClsHnd = structHnd;
            break;

        case GT_INDEX:
            // This will be transformed to an OBJ later.
            alreadyNormalized                       = true;
            structVal->AsIndex()->gtStructElemClass = structHnd;
            structVal->AsIndex()->gtIndElemSize     = info.compCompHnd->getClassSize(structHnd);
            break;

        case GT_FIELD:
            // Wrap it in a GT_OBJ, if needed.
            structVal->gtType = structType;
            if ((structType == TYP_STRUCT) || forceNormalization)
            {
                structVal = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
            }
            break;

        case GT_LCL_VAR:
        case GT_LCL_FLD:
            structLcl = structVal->AsLclVarCommon();
            // Wrap it in a GT_OBJ.
            structVal = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
            FALLTHROUGH;

        case GT_OBJ:
        case GT_BLK:
        case GT_DYN_BLK:
        case GT_ASG:
            // These should already have the appropriate type.
            assert(structVal->gtType == structType);
            alreadyNormalized = true;
            break;

        case GT_IND:
            assert(structVal->gtType == structType);
            structVal         = gtNewObjNode(structHnd, structVal->gtGetOp1());
            alreadyNormalized = true;
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            assert(varTypeIsSIMD(structVal) && (structVal->gtType == structType));
            break;
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            assert(varTypeIsSIMD(structVal) && (structVal->gtType == structType));
            break;
#endif

        case GT_COMMA:
        {
            // The second thing could either be a block node or a GT_FIELD or a GT_SIMD or a GT_COMMA node.
            GenTree* blockNode = structVal->AsOp()->gtOp2;
            assert(blockNode->gtType == structType);

            // Is this GT_COMMA(op1, GT_COMMA())?
            GenTree* parent = structVal;
            if (blockNode->OperGet() == GT_COMMA)
            {
                // Find the last node in the comma chain.
                do
                {
                    assert(blockNode->gtType == structType);
                    parent    = blockNode;
                    blockNode = blockNode->AsOp()->gtOp2;
                } while (blockNode->OperGet() == GT_COMMA);
            }

            if (blockNode->OperGet() == GT_FIELD)
            {
                // If we have a GT_FIELD then wrap it in a GT_OBJ.
                blockNode = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, blockNode));
            }

#ifdef FEATURE_SIMD
            if (blockNode->OperIsSimdOrHWintrinsic())
            {
                parent->AsOp()->gtOp2 = impNormStructVal(blockNode, structHnd, curLevel, forceNormalization);
                alreadyNormalized     = true;
            }
            else
#endif
            {
                noway_assert(blockNode->OperIsBlk());

                // Sink the GT_COMMA below the blockNode addr.
                // That is GT_COMMA(op1, op2=blockNode) is tranformed into
                // blockNode(GT_COMMA(TYP_BYREF, op1, op2's op1)).
                //
                // In case of a chained GT_COMMA case, we sink the last
                // GT_COMMA below the blockNode addr.
                GenTree* blockNodeAddr = blockNode->AsOp()->gtOp1;
                assert(blockNodeAddr->gtType == TYP_BYREF);
                GenTree* commaNode       = parent;
                commaNode->gtType        = TYP_BYREF;
                commaNode->AsOp()->gtOp2 = blockNodeAddr;
                blockNode->AsOp()->gtOp1 = commaNode;
                if (parent == structVal)
                {
                    structVal = blockNode;
                }
                alreadyNormalized = true;
            }
        }
        break;

        default:
            noway_assert(!"Unexpected node in impNormStructVal()");
            break;
    }
    structVal->gtType = structType;

    if (!alreadyNormalized || forceNormalization)
    {
        if (makeTemp)
        {
            unsigned tmpNum = lvaGrabTemp(true DEBUGARG("struct address for call/obj"));

            impAssignTempGen(tmpNum, structVal, structHnd, curLevel);

            // The structVal is now the temp itself

            structLcl = gtNewLclvNode(tmpNum, structType)->AsLclVarCommon();
            structVal = structLcl;
        }
        if ((forceNormalization || (structType == TYP_STRUCT)) && !structVal->OperIsBlk())
        {
            // Wrap it in a GT_OBJ
            structVal = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
        }
    }

    if (structLcl != nullptr)
    {
        // A OBJ on a ADDR(LCL_VAR) can never raise an exception
        // so we don't set GTF_EXCEPT here.
        if (!lvaIsImplicitByRefLocal(structLcl->GetLclNum()))
        {
            structVal->gtFlags &= ~GTF_GLOB_REF;
        }
    }
    else if (structVal->OperIsBlk())
    {
        // In general a OBJ is an indirection and could raise an exception.
        structVal->gtFlags |= GTF_EXCEPT;
    }
    return structVal;
}

/******************************************************************************/
// Given a type token, generate code that will evaluate to the correct
// handle representation of that token (type handle, field handle, or method handle)
//
// For most cases, the handle is determined at compile-time, and the code
// generated is simply an embedded handle.
//
// Run-time lookup is required if the enclosing method is shared between instantiations
// and the token refers to formal type parameters whose instantiation is not known
// at compile-time.
//
GenTree* Compiler::impTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    bool*                   pRuntimeLookup /* = NULL */,
                                    bool                    mustRestoreHandle /* = false */,
                                    bool                    importParent /* = false */)
{
    assert(!fgGlobalMorph);

    CORINFO_GENERICHANDLE_RESULT embedInfo;
    info.compCompHnd->embedGenericHandle(pResolvedToken, importParent, &embedInfo);

    if (pRuntimeLookup)
    {
        *pRuntimeLookup = embedInfo.lookup.lookupKind.needsRuntimeLookup;
    }

    if (mustRestoreHandle && !embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        switch (embedInfo.handleType)
        {
            case CORINFO_HANDLETYPE_CLASS:
                info.compCompHnd->classMustBeLoadedBeforeCodeIsRun((CORINFO_CLASS_HANDLE)embedInfo.compileTimeHandle);
                break;

            case CORINFO_HANDLETYPE_METHOD:
                info.compCompHnd->methodMustBeLoadedBeforeCodeIsRun((CORINFO_METHOD_HANDLE)embedInfo.compileTimeHandle);
                break;

            case CORINFO_HANDLETYPE_FIELD:
                info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(
                    info.compCompHnd->getFieldClass((CORINFO_FIELD_HANDLE)embedInfo.compileTimeHandle));
                break;

            default:
                break;
        }
    }

    // Generate the full lookup tree. May be null if we're abandoning an inline attempt.
    GenTree* result = impLookupToTree(pResolvedToken, &embedInfo.lookup, gtTokenToIconFlags(pResolvedToken->token),
                                      embedInfo.compileTimeHandle);

    // If we have a result and it requires runtime lookup, wrap it in a runtime lookup node.
    if ((result != nullptr) && embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        result = gtNewRuntimeLookup(embedInfo.compileTimeHandle, embedInfo.handleType, result);
    }

    return result;
}

GenTree* Compiler::impLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                   CORINFO_LOOKUP*         pLookup,
                                   GenTreeFlags            handleFlags,
                                   void*                   compileTimeHandle)
{
    if (!pLookup->lookupKind.needsRuntimeLookup)
    {
        // No runtime lookup is required.
        // Access is direct or memory-indirect (of a fixed address) reference

        CORINFO_GENERIC_HANDLE handle       = nullptr;
        void*                  pIndirection = nullptr;
        assert(pLookup->constLookup.accessType != IAT_PPVALUE && pLookup->constLookup.accessType != IAT_RELPVALUE);

        if (pLookup->constLookup.accessType == IAT_VALUE)
        {
            handle = pLookup->constLookup.handle;
        }
        else if (pLookup->constLookup.accessType == IAT_PVALUE)
        {
            pIndirection = pLookup->constLookup.addr;
        }
        GenTree* addr = gtNewIconEmbHndNode(handle, pIndirection, handleFlags, compileTimeHandle);

#ifdef DEBUG
        size_t handleToTrack;
        if (handleFlags == GTF_ICON_TOKEN_HDL)
        {
            handleToTrack = 0;
        }
        else
        {
            handleToTrack = (size_t)compileTimeHandle;
        }

        if (handle != nullptr)
        {
            addr->AsIntCon()->gtTargetHandle = handleToTrack;
        }
        else
        {
            addr->gtGetOp1()->AsIntCon()->gtTargetHandle = handleToTrack;
        }
#endif
        return addr;
    }

    if (pLookup->lookupKind.runtimeLookupKind == CORINFO_LOOKUP_NOT_SUPPORTED)
    {
        // Runtime does not support inlining of all shapes of runtime lookups
        // Inlining has to be aborted in such a case
        assert(compIsForInlining());
        compInlineResult->NoteFatal(InlineObservation::CALLSITE_GENERIC_DICTIONARY_LOOKUP);
        return nullptr;
    }

    // Need to use dictionary-based access which depends on the typeContext
    // which is only available at runtime, not at compile-time.
    return impRuntimeLookupToTree(pResolvedToken, pLookup, compileTimeHandle);
}

#ifdef FEATURE_READYTORUN_COMPILER
GenTree* Compiler::impReadyToRunLookupToTree(CORINFO_CONST_LOOKUP* pLookup,
                                             GenTreeFlags          handleFlags,
                                             void*                 compileTimeHandle)
{
    CORINFO_GENERIC_HANDLE handle       = nullptr;
    void*                  pIndirection = nullptr;
    assert(pLookup->accessType != IAT_PPVALUE && pLookup->accessType != IAT_RELPVALUE);

    if (pLookup->accessType == IAT_VALUE)
    {
        handle = pLookup->handle;
    }
    else if (pLookup->accessType == IAT_PVALUE)
    {
        pIndirection = pLookup->addr;
    }
    GenTree* addr = gtNewIconEmbHndNode(handle, pIndirection, handleFlags, compileTimeHandle);
#ifdef DEBUG
    assert((handleFlags == GTF_ICON_CLASS_HDL) || (handleFlags == GTF_ICON_METHOD_HDL));
    if (handle != nullptr)
    {
        addr->AsIntCon()->gtTargetHandle = (size_t)compileTimeHandle;
    }
    else
    {
        addr->gtGetOp1()->AsIntCon()->gtTargetHandle = (size_t)compileTimeHandle;
    }
#endif //  DEBUG
    return addr;
}

GenTreeCall* Compiler::impReadyToRunHelperToTree(
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    CorInfoHelpFunc         helper,
    var_types               type,
    GenTreeCall::Use*       args /* = nullptr */,
    CORINFO_LOOKUP_KIND*    pGenericLookupKind /* =NULL. Only used with generics */)
{
    CORINFO_CONST_LOOKUP lookup;
    if (!info.compCompHnd->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, helper, &lookup))
    {
        return nullptr;
    }

    GenTreeCall* op1 = gtNewHelperCallNode(helper, type, args);

    op1->setEntryPoint(lookup);

    return op1;
}
#endif

GenTree* Compiler::impMethodPointer(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo)
{
    GenTree* op1 = nullptr;

    switch (pCallInfo->kind)
    {
        case CORINFO_CALL:
            op1 = new (this, GT_FTN_ADDR) GenTreeFptrVal(TYP_I_IMPL, pCallInfo->hMethod);

#ifdef FEATURE_READYTORUN_COMPILER
            if (opts.IsReadyToRun())
            {
                op1->AsFptrVal()->gtEntryPoint = pCallInfo->codePointerLookup.constLookup;
            }
#endif
            break;

        case CORINFO_CALL_CODE_POINTER:
            op1 = impLookupToTree(pResolvedToken, &pCallInfo->codePointerLookup, GTF_ICON_FTN_ADDR, pCallInfo->hMethod);
            break;

        default:
            noway_assert(!"unknown call kind");
            break;
    }

    return op1;
}

//------------------------------------------------------------------------
// getRuntimeContextTree: find pointer to context for runtime lookup.
//
// Arguments:
//    kind - lookup kind.
//
// Return Value:
//    Return GenTree pointer to generic shared context.
//
// Notes:
//    Reports about generic context using.

GenTree* Compiler::getRuntimeContextTree(CORINFO_RUNTIME_LOOKUP_KIND kind)
{
    GenTree* ctxTree = nullptr;

    // Collectible types requires that for shared generic code, if we use the generic context parameter
    // that we report it. (This is a conservative approach, we could detect some cases particularly when the
    // context parameter is this that we don't need the eager reporting logic.)
    lvaGenericsContextInUse = true;

    Compiler* pRoot = impInlineRoot();

    if (kind == CORINFO_LOOKUP_THISOBJ)
    {
        // this Object
        ctxTree = gtNewLclvNode(pRoot->info.compThisArg, TYP_REF);
        ctxTree->gtFlags |= GTF_VAR_CONTEXT;

        // context is the method table pointer of the this object
        ctxTree = gtNewMethodTableLookup(ctxTree);
    }
    else
    {
        assert(kind == CORINFO_LOOKUP_METHODPARAM || kind == CORINFO_LOOKUP_CLASSPARAM);

        // Exact method descriptor as passed in
        ctxTree = gtNewLclvNode(pRoot->info.compTypeCtxtArg, TYP_I_IMPL);
        ctxTree->gtFlags |= GTF_VAR_CONTEXT;
    }
    return ctxTree;
}

/*****************************************************************************/
/* Import a dictionary lookup to access a handle in code shared between
   generic instantiations.
   The lookup depends on the typeContext which is only available at
   runtime, and not at compile-time.
   pLookup->token1 and pLookup->token2 specify the handle that is needed.
   The cases are:

   1. pLookup->indirections == CORINFO_USEHELPER : Call a helper passing it the
      instantiation-specific handle, and the tokens to lookup the handle.
   2. pLookup->indirections != CORINFO_USEHELPER :
      2a. pLookup->testForNull == false : Dereference the instantiation-specific handle
          to get the handle.
      2b. pLookup->testForNull == true : Dereference the instantiation-specific handle.
          If it is non-NULL, it is the handle required. Else, call a helper
          to lookup the handle.
 */

GenTree* Compiler::impRuntimeLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                          CORINFO_LOOKUP*         pLookup,
                                          void*                   compileTimeHandle)
{
    GenTree* ctxTree = getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind);
#if 0
    ctxTree->gtFlags |= GTF_DONT_CSE;   // ToDo Remove this
#endif
    CORINFO_RUNTIME_LOOKUP* pRuntimeLookup = &pLookup->runtimeLookup;
    // It's available only via the run-time helper function
    if (pRuntimeLookup->indirections == CORINFO_USEHELPER)
    {
#ifdef FEATURE_READYTORUN_COMPILER
        if (opts.IsReadyToRun())
        {
            return impReadyToRunHelperToTree(pResolvedToken, CORINFO_HELP_READYTORUN_GENERIC_HANDLE, TYP_I_IMPL,
                                             gtNewCallArgs(ctxTree), &pLookup->lookupKind);
        }
#endif
        return gtNewRuntimeLookupHelperCallNode(pRuntimeLookup, ctxTree, compileTimeHandle);
    }

    // Slot pointer
    GenTree* slotPtrTree = ctxTree;

    if (pRuntimeLookup->testForNull)
    {
        slotPtrTree = impCloneExpr(ctxTree, &ctxTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("impRuntimeLookup slot"));
    }

    GenTree* indOffTree    = nullptr;
    GenTree* lastIndOfTree = nullptr;

    // Applied repeated indirections
    for (WORD i = 0; i < pRuntimeLookup->indirections; i++)
    {
        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            indOffTree = impCloneExpr(slotPtrTree, &slotPtrTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                      nullptr DEBUGARG("impRuntimeLookup indirectOffset"));
        }

        // The last indirection could be subject to a size check (dynamic dictionary expansion)
        bool isLastIndirectionWithSizeCheck =
            ((i == pRuntimeLookup->indirections - 1) && (pRuntimeLookup->sizeOffset != CORINFO_NO_SIZE_CHECK));

        if (i != 0)
        {
            slotPtrTree = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
            slotPtrTree->gtFlags |= GTF_IND_NONFAULTING;
            if (!isLastIndirectionWithSizeCheck)
            {
                slotPtrTree->gtFlags |= GTF_IND_INVARIANT;
            }
        }

        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            slotPtrTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, indOffTree, slotPtrTree);
        }

        if (pRuntimeLookup->offsets[i] != 0)
        {
            if (isLastIndirectionWithSizeCheck)
            {
                lastIndOfTree = impCloneExpr(slotPtrTree, &slotPtrTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                             nullptr DEBUGARG("impRuntimeLookup indirectOffset"));
            }

            slotPtrTree =
                gtNewOperNode(GT_ADD, TYP_I_IMPL, slotPtrTree, gtNewIconNode(pRuntimeLookup->offsets[i], TYP_I_IMPL));
        }
    }

    // No null test required
    if (!pRuntimeLookup->testForNull)
    {
        if (pRuntimeLookup->indirections == 0)
        {
            return slotPtrTree;
        }

        slotPtrTree = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
        slotPtrTree->gtFlags |= GTF_IND_NONFAULTING;

        if (!pRuntimeLookup->testForFixup)
        {
            return slotPtrTree;
        }

        impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("bubbling QMark0"));

        unsigned slotLclNum = lvaGrabTemp(true DEBUGARG("impRuntimeLookup test"));
        impAssignTempGen(slotLclNum, slotPtrTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL, nullptr, impCurStmtOffs);

        GenTree* slot = gtNewLclvNode(slotLclNum, TYP_I_IMPL);
        // downcast the pointer to a TYP_INT on 64-bit targets
        slot = impImplicitIorI4Cast(slot, TYP_INT);
        // Use a GT_AND to check for the lowest bit and indirect if it is set
        GenTree* test  = gtNewOperNode(GT_AND, TYP_INT, slot, gtNewIconNode(1));
        GenTree* relop = gtNewOperNode(GT_EQ, TYP_INT, test, gtNewIconNode(0));

        // slot = GT_IND(slot - 1)
        slot           = gtNewLclvNode(slotLclNum, TYP_I_IMPL);
        GenTree* add   = gtNewOperNode(GT_ADD, TYP_I_IMPL, slot, gtNewIconNode(-1, TYP_I_IMPL));
        GenTree* indir = gtNewOperNode(GT_IND, TYP_I_IMPL, add);
        indir->gtFlags |= GTF_IND_NONFAULTING;
        indir->gtFlags |= GTF_IND_INVARIANT;

        slot           = gtNewLclvNode(slotLclNum, TYP_I_IMPL);
        GenTree* asg   = gtNewAssignNode(slot, indir);
        GenTree* colon = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), asg);
        GenTree* qmark = gtNewQmarkNode(TYP_VOID, relop, colon);
        impAppendTree(qmark, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        return gtNewLclvNode(slotLclNum, TYP_I_IMPL);
    }

    assert(pRuntimeLookup->indirections != 0);

    impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("bubbling QMark1"));

    // Extract the handle
    GenTree* handleForNullCheck = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
    handleForNullCheck->gtFlags |= GTF_IND_NONFAULTING;

    // Call the helper
    // - Setup argNode with the pointer to the signature returned by the lookup
    GenTree* argNode = gtNewIconEmbHndNode(pRuntimeLookup->signature, nullptr, GTF_ICON_GLOBAL_PTR, compileTimeHandle);

    GenTreeCall::Use* helperArgs = gtNewCallArgs(ctxTree, argNode);
    GenTreeCall*      helperCall = gtNewHelperCallNode(pRuntimeLookup->helper, TYP_I_IMPL, helperArgs);

    // Check for null and possibly call helper
    GenTree* nullCheck       = gtNewOperNode(GT_NE, TYP_INT, handleForNullCheck, gtNewIconNode(0, TYP_I_IMPL));
    GenTree* handleForResult = gtCloneExpr(handleForNullCheck);

    GenTree* result = nullptr;

    if (pRuntimeLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
    {
        // Dynamic dictionary expansion support

        assert((lastIndOfTree != nullptr) && (pRuntimeLookup->indirections > 0));

        // sizeValue = dictionary[pRuntimeLookup->sizeOffset]
        GenTreeIntCon* sizeOffset      = gtNewIconNode(pRuntimeLookup->sizeOffset, TYP_I_IMPL);
        GenTree*       sizeValueOffset = gtNewOperNode(GT_ADD, TYP_I_IMPL, lastIndOfTree, sizeOffset);
        GenTree*       sizeValue       = gtNewOperNode(GT_IND, TYP_I_IMPL, sizeValueOffset);
        sizeValue->gtFlags |= GTF_IND_NONFAULTING;

        // sizeCheck fails if sizeValue < pRuntimeLookup->offsets[i]
        GenTree* offsetValue = gtNewIconNode(pRuntimeLookup->offsets[pRuntimeLookup->indirections - 1], TYP_I_IMPL);
        GenTree* sizeCheck   = gtNewOperNode(GT_LE, TYP_INT, sizeValue, offsetValue);

        // revert null check condition.
        nullCheck->ChangeOperUnchecked(GT_EQ);

        // ((sizeCheck fails || nullCheck fails))) ? (helperCall : handle).
        // Add checks and the handle as call arguments, indirect call transformer will handle this.
        helperCall->gtCallArgs = gtPrependNewCallArg(handleForResult, helperCall->gtCallArgs);
        helperCall->gtCallArgs = gtPrependNewCallArg(sizeCheck, helperCall->gtCallArgs);
        helperCall->gtCallArgs = gtPrependNewCallArg(nullCheck, helperCall->gtCallArgs);
        result                 = helperCall;
        addExpRuntimeLookupCandidate(helperCall);
    }
    else
    {
        GenTreeColon* colonNullCheck = new (this, GT_COLON) GenTreeColon(TYP_I_IMPL, handleForResult, helperCall);
        result                       = gtNewQmarkNode(TYP_I_IMPL, nullCheck, colonNullCheck);
    }

    unsigned tmp = lvaGrabTemp(true DEBUGARG("spilling Runtime Lookup tree"));

    impAssignTempGen(tmp, result, (unsigned)CHECK_SPILL_NONE);
    return gtNewLclvNode(tmp, TYP_I_IMPL);
}

/******************************************************************************
 *  Spills the stack at verCurrentState.esStack[level] and replaces it with a temp.
 *  If tnum!=BAD_VAR_NUM, the temp var used to replace the tree is tnum,
 *     else, grab a new temp.
 *  For structs (which can be pushed on the stack using obj, etc),
 *  special handling is needed
 */

struct RecursiveGuard
{
public:
    RecursiveGuard()
    {
        m_pAddress = nullptr;
    }

    ~RecursiveGuard()
    {
        if (m_pAddress)
        {
            *m_pAddress = false;
        }
    }

    void Init(bool* pAddress, bool bInitialize)
    {
        assert(pAddress && *pAddress == false && "Recursive guard violation");
        m_pAddress = pAddress;

        if (bInitialize)
        {
            *m_pAddress = true;
        }
    }

protected:
    bool* m_pAddress;
};

bool Compiler::impSpillStackEntry(unsigned level,
                                  unsigned tnum
#ifdef DEBUG
                                  ,
                                  bool        bAssertOnRecursion,
                                  const char* reason
#endif
                                  )
{

#ifdef DEBUG
    RecursiveGuard guard;
    guard.Init(&impNestedStackSpill, bAssertOnRecursion);
#endif

    GenTree* tree = verCurrentState.esStack[level].val;

    /* Allocate a temp if we haven't been asked to use a particular one */

    if (tiVerificationNeeded)
    {
        // Ignore bad temp requests (they will happen with bad code and will be
        // catched when importing the destblock)
        if (tnum != BAD_VAR_NUM && tnum >= lvaCount)
        {
            return false;
        }
    }
    else
    {
        if (tnum != BAD_VAR_NUM && (tnum >= lvaCount))
        {
            return false;
        }
    }

    bool isNewTemp = false;

    if (tnum == BAD_VAR_NUM)
    {
        tnum      = lvaGrabTemp(true DEBUGARG(reason));
        isNewTemp = true;
    }
    else if (tiVerificationNeeded && lvaTable[tnum].TypeGet() != TYP_UNDEF)
    {
        // if verification is needed and tnum's type is incompatible with
        // type on that stack, we grab a new temp. This is safe since
        // we will throw a verification exception in the dest block.

        var_types valTyp = tree->TypeGet();
        var_types dstTyp = lvaTable[tnum].TypeGet();

        // if the two types are different, we return. This will only happen with bad code and will
        // be catched when importing the destblock. We still allow int/byrefs and float/double differences.
        if ((genActualType(valTyp) != genActualType(dstTyp)) &&
            !(
#ifndef TARGET_64BIT
                (valTyp == TYP_I_IMPL && dstTyp == TYP_BYREF) || (valTyp == TYP_BYREF && dstTyp == TYP_I_IMPL) ||
#endif // !TARGET_64BIT
                (varTypeIsFloating(dstTyp) && varTypeIsFloating(valTyp))))
        {
            return false;
        }
    }

    /* Assign the spilled entry to the temp */
    impAssignTempGen(tnum, tree, verCurrentState.esStack[level].seTypeInfo.GetClassHandle(), level);

    // If temp is newly introduced and a ref type, grab what type info we can.
    if (isNewTemp && (lvaTable[tnum].lvType == TYP_REF))
    {
        assert(lvaTable[tnum].lvSingleDef == 0);
        lvaTable[tnum].lvSingleDef = 1;
        JITDUMP("Marked V%02u as a single def temp\n", tnum);
        CORINFO_CLASS_HANDLE stkHnd = verCurrentState.esStack[level].seTypeInfo.GetClassHandle();
        lvaSetClass(tnum, tree, stkHnd);

        // If we're assigning a GT_RET_EXPR, note the temp over on the call,
        // so the inliner can use it in case it needs a return spill temp.
        if (tree->OperGet() == GT_RET_EXPR)
        {
            JITDUMP("\n*** see V%02u = GT_RET_EXPR, noting temp\n", tnum);
            GenTree*             call = tree->AsRetExpr()->gtInlineCandidate;
            InlineCandidateInfo* ici  = call->AsCall()->gtInlineCandidateInfo;
            ici->preexistingSpillTemp = tnum;
        }
    }

    // The tree type may be modified by impAssignTempGen, so use the type of the lclVar.
    var_types type                     = genActualType(lvaTable[tnum].TypeGet());
    GenTree*  temp                     = gtNewLclvNode(tnum, type);
    verCurrentState.esStack[level].val = temp;

    return true;
}

/*****************************************************************************
 *
 *  Ensure that the stack has only spilled values
 */

void Compiler::impSpillStackEnsure(bool spillLeaves)
{
    assert(!spillLeaves || opts.compDbgCode);

    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        GenTree* tree = verCurrentState.esStack[level].val;

        if (!spillLeaves && tree->OperIsLeaf())
        {
            continue;
        }

        // Temps introduced by the importer itself don't need to be spilled

        bool isTempLcl =
            (tree->OperGet() == GT_LCL_VAR) && (tree->AsLclVarCommon()->GetLclNum() >= info.compLocalsCount);

        if (isTempLcl)
        {
            continue;
        }

        impSpillStackEntry(level, BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impSpillStackEnsure"));
    }
}

void Compiler::impSpillEvalStack()
{
    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        impSpillStackEntry(level, BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impSpillEvalStack"));
    }
}

/*****************************************************************************
 *
 *  If the stack contains any trees with side effects in them, assign those
 *  trees to temps and append the assignments to the statement list.
 *  On return the stack is guaranteed to be empty.
 */

inline void Compiler::impEvalSideEffects()
{
    impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG("impEvalSideEffects"));
    verCurrentState.esStackDepth = 0;
}

/*****************************************************************************
 *
 *  If the stack contains any trees with side effects in them, assign those
 *  trees to temps and replace them on the stack with refs to their temps.
 *  [0..chkLevel) is the portion of the stack which will be checked and spilled.
 */

inline void Compiler::impSpillSideEffects(bool spillGlobEffects, unsigned chkLevel DEBUGARG(const char* reason))
{
    assert(chkLevel != (unsigned)CHECK_SPILL_NONE);

    /* Before we make any appends to the tree list we must spill the
     * "special" side effects (GTF_ORDER_SIDEEFF on a GT_CATCH_ARG) */

    impSpillSpecialSideEff();

    if (chkLevel == (unsigned)CHECK_SPILL_ALL)
    {
        chkLevel = verCurrentState.esStackDepth;
    }

    assert(chkLevel <= verCurrentState.esStackDepth);

    unsigned spillFlags = spillGlobEffects ? GTF_GLOB_EFFECT : GTF_SIDE_EFFECT;

    for (unsigned i = 0; i < chkLevel; i++)
    {
        GenTree* tree = verCurrentState.esStack[i].val;

        GenTree* lclVarTree;

        if ((tree->gtFlags & spillFlags) != 0 ||
            (spillGlobEffects &&                        // Only consider the following when  spillGlobEffects == true
             !impIsAddressInLocal(tree, &lclVarTree) && // No need to spill the GT_ADDR node on a local.
             gtHasLocalsWithAddrOp(tree))) // Spill if we still see GT_LCL_VAR that contains lvHasLdAddrOp or
                                           // lvAddrTaken flag.
        {
            impSpillStackEntry(i, BAD_VAR_NUM DEBUGARG(false) DEBUGARG(reason));
        }
    }
}

/*****************************************************************************
 *
 *  If the stack contains any trees with special side effects in them, assign
 *  those trees to temps and replace them on the stack with refs to their temps.
 */

inline void Compiler::impSpillSpecialSideEff()
{
    // Only exception objects need to be carefully handled

    if (!compCurBB->bbCatchTyp)
    {
        return;
    }

    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        GenTree* tree = verCurrentState.esStack[level].val;
        // Make sure if we have an exception object in the sub tree we spill ourselves.
        if (gtHasCatchArg(tree))
        {
            impSpillStackEntry(level, BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impSpillSpecialSideEff"));
        }
    }
}

/*****************************************************************************
 *
 *  Spill all stack references to value classes (TYP_STRUCT nodes)
 */

void Compiler::impSpillValueClasses()
{
    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        GenTree* tree = verCurrentState.esStack[level].val;

        if (fgWalkTreePre(&tree, impFindValueClasses) == WALK_ABORT)
        {
            // Tree walk was aborted, which means that we found a
            // value class on the stack.  Need to spill that
            // stack entry.

            impSpillStackEntry(level, BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impSpillValueClasses"));
        }
    }
}

/*****************************************************************************
 *
 *  Callback that checks if a tree node is TYP_STRUCT
 */

Compiler::fgWalkResult Compiler::impFindValueClasses(GenTree** pTree, fgWalkData* data)
{
    fgWalkResult walkResult = WALK_CONTINUE;

    if ((*pTree)->gtType == TYP_STRUCT)
    {
        // Abort the walk and indicate that we found a value class

        walkResult = WALK_ABORT;
    }

    return walkResult;
}

/*****************************************************************************
 *
 *  If the stack contains any trees with references to local #lclNum, assign
 *  those trees to temps and replace their place on the stack with refs to
 *  their temps.
 */

void Compiler::impSpillLclRefs(ssize_t lclNum)
{
    /* Before we make any appends to the tree list we must spill the
     * "special" side effects (GTF_ORDER_SIDEEFF) - GT_CATCH_ARG */

    impSpillSpecialSideEff();

    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        GenTree* tree = verCurrentState.esStack[level].val;

        /* If the tree may throw an exception, and the block has a handler,
           then we need to spill assignments to the local if the local is
           live on entry to the handler.
           Just spill 'em all without considering the liveness */

        bool xcptnCaught = ehBlockHasExnFlowDsc(compCurBB) && (tree->gtFlags & (GTF_CALL | GTF_EXCEPT));

        /* Skip the tree if it doesn't have an affected reference,
           unless xcptnCaught */

        if (xcptnCaught || gtHasRef(tree, lclNum, false))
        {
            impSpillStackEntry(level, BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impSpillLclRefs"));
        }
    }
}

/*****************************************************************************
 *
 *  Push catch arg onto the stack.
 *  If there are jumps to the beginning of the handler, insert basic block
 *  and spill catch arg to a temp. Update the handler block if necessary.
 *
 *  Returns the basic block of the actual handler.
 */

BasicBlock* Compiler::impPushCatchArgOnStack(BasicBlock* hndBlk, CORINFO_CLASS_HANDLE clsHnd, bool isSingleBlockFilter)
{
    // Do not inject the basic block twice on reimport. This should be
    // hit only under JIT stress. See if the block is the one we injected.
    // Note that EH canonicalization can inject internal blocks here. We might
    // be able to re-use such a block (but we don't, right now).
    if ((hndBlk->bbFlags & (BBF_IMPORTED | BBF_INTERNAL | BBF_DONT_REMOVE)) ==
        (BBF_IMPORTED | BBF_INTERNAL | BBF_DONT_REMOVE))
    {
        Statement* stmt = hndBlk->firstStmt();

        if (stmt != nullptr)
        {
            GenTree* tree = stmt->GetRootNode();
            assert(tree != nullptr);

            if ((tree->gtOper == GT_ASG) && (tree->AsOp()->gtOp1->gtOper == GT_LCL_VAR) &&
                (tree->AsOp()->gtOp2->gtOper == GT_CATCH_ARG))
            {
                tree = gtNewLclvNode(tree->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum(), TYP_REF);

                impPushOnStack(tree, typeInfo(TI_REF, clsHnd));

                return hndBlk->bbNext;
            }
        }

        // If we get here, it must have been some other kind of internal block. It's possible that
        // someone prepended something to our injected block, but that's unlikely.
    }

    /* Push the exception address value on the stack */
    GenTree* arg = new (this, GT_CATCH_ARG) GenTree(GT_CATCH_ARG, TYP_REF);

    /* Mark the node as having a side-effect - i.e. cannot be
     * moved around since it is tied to a fixed location (EAX) */
    arg->gtFlags |= GTF_ORDER_SIDEEFF;

#if defined(JIT32_GCENCODER)
    const bool forceInsertNewBlock = isSingleBlockFilter || compStressCompile(STRESS_CATCH_ARG, 5);
#else
    const bool forceInsertNewBlock      = compStressCompile(STRESS_CATCH_ARG, 5);
#endif // defined(JIT32_GCENCODER)

    /* Spill GT_CATCH_ARG to a temp if there are jumps to the beginning of the handler */
    if (hndBlk->bbRefs > 1 || forceInsertNewBlock)
    {
        if (hndBlk->bbRefs == 1)
        {
            hndBlk->bbRefs++;
        }

        /* Create extra basic block for the spill */
        BasicBlock* newBlk = fgNewBBbefore(BBJ_NONE, hndBlk, /* extendRegion */ true);
        newBlk->bbFlags |= BBF_IMPORTED | BBF_DONT_REMOVE;
        newBlk->inheritWeight(hndBlk);
        newBlk->bbCodeOffs = hndBlk->bbCodeOffs;

        /* Account for the new link we are about to create */
        hndBlk->bbRefs++;

        // Spill into a temp.
        unsigned tempNum         = lvaGrabTemp(false DEBUGARG("SpillCatchArg"));
        lvaTable[tempNum].lvType = TYP_REF;
        GenTree* argAsg          = gtNewTempAssign(tempNum, arg);
        arg                      = gtNewLclvNode(tempNum, TYP_REF);

        hndBlk->bbStkTempsIn = tempNum;

        Statement* argStmt;

        if (info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES)
        {
            // Report the debug info. impImportBlockCode won't treat the actual handler as exception block and thus
            // won't do it for us.
            impCurStmtOffs = newBlk->bbCodeOffs | IL_OFFSETX_STKBIT;
            argStmt        = gtNewStmt(argAsg, impCurStmtOffs);
        }
        else
        {
            argStmt = gtNewStmt(argAsg);
        }

        fgInsertStmtAtEnd(newBlk, argStmt);
    }

    impPushOnStack(arg, typeInfo(TI_REF, clsHnd));

    return hndBlk;
}

/*****************************************************************************
 *
 *  Given a tree, clone it. *pClone is set to the cloned tree.
 *  Returns the original tree if the cloning was easy,
 *   else returns the temp to which the tree had to be spilled to.
 *  If the tree has side-effects, it will be spilled to a temp.
 */

GenTree* Compiler::impCloneExpr(GenTree*             tree,
                                GenTree**            pClone,
                                CORINFO_CLASS_HANDLE structHnd,
                                unsigned             curLevel,
                                Statement** pAfterStmt DEBUGARG(const char* reason))
{
    if (!(tree->gtFlags & GTF_GLOB_EFFECT))
    {
        GenTree* clone = gtClone(tree, true);

        if (clone)
        {
            *pClone = clone;
            return tree;
        }
    }

    /* Store the operand in a temp and return the temp */

    unsigned temp = lvaGrabTemp(true DEBUGARG(reason));

    // impAssignTempGen() may change tree->gtType to TYP_VOID for calls which
    // return a struct type. It also may modify the struct type to a more
    // specialized type (e.g. a SIMD type).  So we will get the type from
    // the lclVar AFTER calling impAssignTempGen().

    impAssignTempGen(temp, tree, structHnd, curLevel, pAfterStmt, impCurStmtOffs);
    var_types type = genActualType(lvaTable[temp].TypeGet());

    *pClone = gtNewLclvNode(temp, type);
    return gtNewLclvNode(temp, type);
}

/*****************************************************************************
 * Remember the IL offset (including stack-empty info) for the trees we will
 * generate now.
 */

inline void Compiler::impCurStmtOffsSet(IL_OFFSET offs)
{
    if (compIsForInlining())
    {
        Statement* callStmt = impInlineInfo->iciStmt;
        impCurStmtOffs      = callStmt->GetILOffsetX();
    }
    else
    {
        assert(offs == BAD_IL_OFFSET || (offs & IL_OFFSETX_BITS) == 0);
        IL_OFFSETX stkBit = (verCurrentState.esStackDepth > 0) ? IL_OFFSETX_STKBIT : 0;
        impCurStmtOffs    = offs | stkBit;
    }
}

/*****************************************************************************
 * Returns current IL offset with stack-empty and call-instruction info incorporated
 */
inline IL_OFFSETX Compiler::impCurILOffset(IL_OFFSET offs, bool callInstruction)
{
    if (compIsForInlining())
    {
        return BAD_IL_OFFSET;
    }
    else
    {
        assert(offs == BAD_IL_OFFSET || (offs & IL_OFFSETX_BITS) == 0);
        IL_OFFSETX stkBit             = (verCurrentState.esStackDepth > 0) ? IL_OFFSETX_STKBIT : 0;
        IL_OFFSETX callInstructionBit = callInstruction ? IL_OFFSETX_CALLINSTRUCTIONBIT : 0;
        return offs | stkBit | callInstructionBit;
    }
}

//------------------------------------------------------------------------
// impCanSpillNow: check is it possible to spill all values from eeStack to local variables.
//
// Arguments:
//    prevOpcode - last importer opcode
//
// Return Value:
//    true if it is legal, false if it could be a sequence that we do not want to divide.
bool Compiler::impCanSpillNow(OPCODE prevOpcode)
{
    // Don't spill after ldtoken, newarr and newobj, because it could be a part of the InitializeArray sequence.
    // Avoid breaking up to guarantee that impInitializeArrayIntrinsic can succeed.
    return (prevOpcode != CEE_LDTOKEN) && (prevOpcode != CEE_NEWARR) && (prevOpcode != CEE_NEWOBJ);
}

/*****************************************************************************
 *
 *  Remember the instr offset for the statements
 *
 *  When we do impAppendTree(tree), we can't set stmt->SetLastILOffset(impCurOpcOffs),
 *  if the append was done because of a partial stack spill,
 *  as some of the trees corresponding to code up to impCurOpcOffs might
 *  still be sitting on the stack.
 *  So we delay calling of SetLastILOffset() until impNoteLastILoffs().
 *  This should be called when an opcode finally/explicitly causes
 *  impAppendTree(tree) to be called (as opposed to being called because of
 *  a spill caused by the opcode)
 */

#ifdef DEBUG

void Compiler::impNoteLastILoffs()
{
    if (impLastILoffsStmt == nullptr)
    {
        // We should have added a statement for the current basic block
        // Is this assert correct ?

        assert(impLastStmt);

        impLastStmt->SetLastILOffset(compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs);
    }
    else
    {
        impLastILoffsStmt->SetLastILOffset(compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs);
        impLastILoffsStmt = nullptr;
    }
}

#endif // DEBUG

/*****************************************************************************
 * We don't create any GenTree (excluding spills) for a branch.
 * For debugging info, we need a placeholder so that we can note
 * the IL offset in gtStmt.gtStmtOffs. So append an empty statement.
 */

void Compiler::impNoteBranchOffs()
{
    if (opts.compDbgCode)
    {
        impAppendTree(gtNewNothingNode(), (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
    }
}

/*****************************************************************************
 * Locate the next stmt boundary for which we need to record info.
 * We will have to spill the stack at such boundaries if it is not
 * already empty.
 * Returns the next stmt boundary (after the start of the block)
 */

unsigned Compiler::impInitBlockLineInfo()
{
    /* Assume the block does not correspond with any IL offset. This prevents
       us from reporting extra offsets. Extra mappings can cause confusing
       stepping, especially if the extra mapping is a jump-target, and the
       debugger does not ignore extra mappings, but instead rewinds to the
       nearest known offset */

    impCurStmtOffsSet(BAD_IL_OFFSET);

    if (compIsForInlining())
    {
        return ~0;
    }

    IL_OFFSET blockOffs = compCurBB->bbCodeOffs;

    if ((verCurrentState.esStackDepth == 0) && (info.compStmtOffsetsImplicit & ICorDebugInfo::STACK_EMPTY_BOUNDARIES))
    {
        impCurStmtOffsSet(blockOffs);
    }

    if (false && (info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES))
    {
        impCurStmtOffsSet(blockOffs);
    }

    /* Always report IL offset 0 or some tests get confused.
       Probably a good idea anyways */

    if (blockOffs == 0)
    {
        impCurStmtOffsSet(blockOffs);
    }

    if (!info.compStmtOffsetsCount)
    {
        return ~0;
    }

    /* Find the lowest explicit stmt boundary within the block */

    /* Start looking at an entry that is based on our instr offset */

    unsigned index = (info.compStmtOffsetsCount * blockOffs) / info.compILCodeSize;

    if (index >= info.compStmtOffsetsCount)
    {
        index = info.compStmtOffsetsCount - 1;
    }

    /* If we've guessed too far, back up */

    while (index > 0 && info.compStmtOffsets[index - 1] >= blockOffs)
    {
        index--;
    }

    /* If we guessed short, advance ahead */

    while (info.compStmtOffsets[index] < blockOffs)
    {
        index++;

        if (index == info.compStmtOffsetsCount)
        {
            return info.compStmtOffsetsCount;
        }
    }

    assert(index < info.compStmtOffsetsCount);

    if (info.compStmtOffsets[index] == blockOffs)
    {
        /* There is an explicit boundary for the start of this basic block.
           So we will start with bbCodeOffs. Else we will wait until we
           get to the next explicit boundary */

        impCurStmtOffsSet(blockOffs);

        index++;
    }

    return index;
}

/*****************************************************************************/

bool Compiler::impOpcodeIsCallOpcode(OPCODE opcode)
{
    switch (opcode)
    {
        case CEE_CALL:
        case CEE_CALLI:
        case CEE_CALLVIRT:
            return true;

        default:
            return false;
    }
}

/*****************************************************************************/

static inline bool impOpcodeIsCallSiteBoundary(OPCODE opcode)
{
    switch (opcode)
    {
        case CEE_CALL:
        case CEE_CALLI:
        case CEE_CALLVIRT:
        case CEE_JMP:
        case CEE_NEWOBJ:
        case CEE_NEWARR:
            return true;

        default:
            return false;
    }
}

/*****************************************************************************/

// One might think it is worth caching these values, but results indicate
// that it isn't.
// In addition, caching them causes SuperPMI to be unable to completely
// encapsulate an individual method context.
CORINFO_CLASS_HANDLE Compiler::impGetRefAnyClass()
{
    CORINFO_CLASS_HANDLE refAnyClass = info.compCompHnd->getBuiltinClass(CLASSID_TYPED_BYREF);
    assert(refAnyClass != (CORINFO_CLASS_HANDLE) nullptr);
    return refAnyClass;
}

CORINFO_CLASS_HANDLE Compiler::impGetTypeHandleClass()
{
    CORINFO_CLASS_HANDLE typeHandleClass = info.compCompHnd->getBuiltinClass(CLASSID_TYPE_HANDLE);
    assert(typeHandleClass != (CORINFO_CLASS_HANDLE) nullptr);
    return typeHandleClass;
}

CORINFO_CLASS_HANDLE Compiler::impGetRuntimeArgumentHandle()
{
    CORINFO_CLASS_HANDLE argIteratorClass = info.compCompHnd->getBuiltinClass(CLASSID_ARGUMENT_HANDLE);
    assert(argIteratorClass != (CORINFO_CLASS_HANDLE) nullptr);
    return argIteratorClass;
}

CORINFO_CLASS_HANDLE Compiler::impGetStringClass()
{
    CORINFO_CLASS_HANDLE stringClass = info.compCompHnd->getBuiltinClass(CLASSID_STRING);
    assert(stringClass != (CORINFO_CLASS_HANDLE) nullptr);
    return stringClass;
}

CORINFO_CLASS_HANDLE Compiler::impGetObjectClass()
{
    CORINFO_CLASS_HANDLE objectClass = info.compCompHnd->getBuiltinClass(CLASSID_SYSTEM_OBJECT);
    assert(objectClass != (CORINFO_CLASS_HANDLE) nullptr);
    return objectClass;
}

/*****************************************************************************
 *  "&var" can be used either as TYP_BYREF or TYP_I_IMPL, but we
 *  set its type to TYP_BYREF when we create it. We know if it can be
 *  changed to TYP_I_IMPL only at the point where we use it
 */

/* static */
void Compiler::impBashVarAddrsToI(GenTree* tree1, GenTree* tree2)
{
    if (tree1->IsLocalAddrExpr() != nullptr)
    {
        tree1->gtType = TYP_I_IMPL;
    }

    if (tree2 && (tree2->IsLocalAddrExpr() != nullptr))
    {
        tree2->gtType = TYP_I_IMPL;
    }
}

/*****************************************************************************
 *  TYP_INT and TYP_I_IMPL can be used almost interchangeably, but we want
 *  to make that an explicit cast in our trees, so any implicit casts that
 *  exist in the IL (at least on 64-bit where TYP_I_IMPL != TYP_INT) are
 *  turned into explicit casts here.
 *  We also allow an implicit conversion of a ldnull into a TYP_I_IMPL(0)
 */

GenTree* Compiler::impImplicitIorI4Cast(GenTree* tree, var_types dstTyp)
{
    var_types currType   = genActualType(tree->gtType);
    var_types wantedType = genActualType(dstTyp);

    if (wantedType != currType)
    {
        // Automatic upcast for a GT_CNS_INT into TYP_I_IMPL
        if ((tree->OperGet() == GT_CNS_INT) && varTypeIsI(dstTyp))
        {
            if (!varTypeIsI(tree->gtType) || ((tree->gtType == TYP_REF) && (tree->AsIntCon()->gtIconVal == 0)))
            {
                tree->gtType = TYP_I_IMPL;
            }
        }
#ifdef TARGET_64BIT
        else if (varTypeIsI(wantedType) && (currType == TYP_INT))
        {
            // Note that this allows TYP_INT to be cast to a TYP_I_IMPL when wantedType is a TYP_BYREF or TYP_REF
            tree = gtNewCastNode(TYP_I_IMPL, tree, false, TYP_I_IMPL);
        }
        else if ((wantedType == TYP_INT) && varTypeIsI(currType))
        {
            // Note that this allows TYP_BYREF or TYP_REF to be cast to a TYP_INT
            tree = gtNewCastNode(TYP_INT, tree, false, TYP_INT);
        }
#endif // TARGET_64BIT
    }

    return tree;
}

/*****************************************************************************
 *  TYP_FLOAT and TYP_DOUBLE can be used almost interchangeably in some cases,
 *  but we want to make that an explicit cast in our trees, so any implicit casts
 *  that exist in the IL are turned into explicit casts here.
 */

GenTree* Compiler::impImplicitR4orR8Cast(GenTree* tree, var_types dstTyp)
{
    if (varTypeIsFloating(tree) && varTypeIsFloating(dstTyp) && (dstTyp != tree->gtType))
    {
        tree = gtNewCastNode(dstTyp, tree, false, dstTyp);
    }

    return tree;
}

//------------------------------------------------------------------------
// impInitializeArrayIntrinsic: Attempts to replace a call to InitializeArray
//    with a GT_COPYBLK node.
//
// Arguments:
//    sig - The InitializeArray signature.
//
// Return Value:
//    A pointer to the newly created GT_COPYBLK node if the replacement succeeds or
//    nullptr otherwise.
//
// Notes:
//    The function recognizes the following IL pattern:
//      ldc <length> or a list of ldc <lower bound>/<length>
//      newarr or newobj
//      dup
//      ldtoken <field handle>
//      call InitializeArray
//    The lower bounds need not be constant except when the array rank is 1.
//    The function recognizes all kinds of arrays thus enabling a small runtime
//    such as CoreRT to skip providing an implementation for InitializeArray.

GenTree* Compiler::impInitializeArrayIntrinsic(CORINFO_SIG_INFO* sig)
{
    assert(sig->numArgs == 2);

    GenTree* fieldTokenNode = impStackTop(0).val;
    GenTree* arrayLocalNode = impStackTop(1).val;

    //
    // Verify that the field token is known and valid.  Note that It's also
    // possible for the token to come from reflection, in which case we cannot do
    // the optimization and must therefore revert to calling the helper.  You can
    // see an example of this in bvt\DynIL\initarray2.exe (in Main).
    //

    // Check to see if the ldtoken helper call is what we see here.
    if (fieldTokenNode->gtOper != GT_CALL || (fieldTokenNode->AsCall()->gtCallType != CT_HELPER) ||
        (fieldTokenNode->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD)))
    {
        return nullptr;
    }

    // Strip helper call away
    fieldTokenNode = fieldTokenNode->AsCall()->gtCallArgs->GetNode();

    if (fieldTokenNode->gtOper == GT_IND)
    {
        fieldTokenNode = fieldTokenNode->AsOp()->gtOp1;
    }

    // Check for constant
    if (fieldTokenNode->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    CORINFO_FIELD_HANDLE fieldToken = (CORINFO_FIELD_HANDLE)fieldTokenNode->AsIntCon()->gtCompileTimeHandle;
    if (!fieldTokenNode->IsIconHandle(GTF_ICON_FIELD_HDL) || (fieldToken == nullptr))
    {
        return nullptr;
    }

    //
    // We need to get the number of elements in the array and the size of each element.
    // We verify that the newarr statement is exactly what we expect it to be.
    // If it's not then we just return NULL and we don't optimize this call
    //

    // It is possible the we don't have any statements in the block yet.
    if (impLastStmt == nullptr)
    {
        return nullptr;
    }

    //
    // We start by looking at the last statement, making sure it's an assignment, and
    // that the target of the assignment is the array passed to InitializeArray.
    //
    GenTree* arrayAssignment = impLastStmt->GetRootNode();
    if ((arrayAssignment->gtOper != GT_ASG) || (arrayAssignment->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
        (arrayLocalNode->gtOper != GT_LCL_VAR) || (arrayAssignment->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() !=
                                                   arrayLocalNode->AsLclVarCommon()->GetLclNum()))
    {
        return nullptr;
    }

    //
    // Make sure that the object being assigned is a helper call.
    //

    GenTree* newArrayCall = arrayAssignment->AsOp()->gtOp2;
    if ((newArrayCall->gtOper != GT_CALL) || (newArrayCall->AsCall()->gtCallType != CT_HELPER))
    {
        return nullptr;
    }

    //
    // Verify that it is one of the new array helpers.
    //

    bool isMDArray = false;

    if (newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_DIRECT) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_OBJ) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_VC) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_ALIGN8)
#ifdef FEATURE_READYTORUN_COMPILER
        && newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1)
#endif
            )
    {
        if (newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEW_MDARR_NONVARARG))
        {
            return nullptr;
        }

        isMDArray = true;
    }

    CORINFO_CLASS_HANDLE arrayClsHnd = (CORINFO_CLASS_HANDLE)newArrayCall->AsCall()->compileTimeHelperArgumentHandle;

    //
    // Make sure we found a compile time handle to the array
    //

    if (!arrayClsHnd)
    {
        return nullptr;
    }

    unsigned rank = 0;
    S_UINT32 numElements;

    if (isMDArray)
    {
        rank = info.compCompHnd->getArrayRank(arrayClsHnd);

        if (rank == 0)
        {
            return nullptr;
        }

        GenTreeCall::Use* tokenArg = newArrayCall->AsCall()->gtCallArgs;
        assert(tokenArg != nullptr);
        GenTreeCall::Use* numArgsArg = tokenArg->GetNext();
        assert(numArgsArg != nullptr);
        GenTreeCall::Use* argsArg = numArgsArg->GetNext();
        assert(argsArg != nullptr);

        //
        // The number of arguments should be a constant between 1 and 64. The rank can't be 0
        // so at least one length must be present and the rank can't exceed 32 so there can
        // be at most 64 arguments - 32 lengths and 32 lower bounds.
        //

        if ((!numArgsArg->GetNode()->IsCnsIntOrI()) || (numArgsArg->GetNode()->AsIntCon()->IconValue() < 1) ||
            (numArgsArg->GetNode()->AsIntCon()->IconValue() > 64))
        {
            return nullptr;
        }

        unsigned numArgs = static_cast<unsigned>(numArgsArg->GetNode()->AsIntCon()->IconValue());
        bool     lowerBoundsSpecified;

        if (numArgs == rank * 2)
        {
            lowerBoundsSpecified = true;
        }
        else if (numArgs == rank)
        {
            lowerBoundsSpecified = false;

            //
            // If the rank is 1 and a lower bound isn't specified then the runtime creates
            // a SDArray. Note that even if a lower bound is specified it can be 0 and then
            // we get a SDArray as well, see the for loop below.
            //

            if (rank == 1)
            {
                isMDArray = false;
            }
        }
        else
        {
            return nullptr;
        }

        //
        // The rank is known to be at least 1 so we can start with numElements being 1
        // to avoid the need to special case the first dimension.
        //

        numElements = S_UINT32(1);

        struct Match
        {
            static bool IsArgsFieldInit(GenTree* tree, unsigned index, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_ASG) && IsArgsFieldIndir(tree->gtGetOp1(), index, lvaNewObjArrayArgs) &&
                       IsArgsAddr(tree->gtGetOp1()->gtGetOp1()->gtGetOp1(), lvaNewObjArrayArgs);
            }

            static bool IsArgsFieldIndir(GenTree* tree, unsigned index, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_IND) && (tree->gtGetOp1()->OperGet() == GT_ADD) &&
                       (tree->gtGetOp1()->gtGetOp2()->IsIntegralConst(sizeof(INT32) * index)) &&
                       IsArgsAddr(tree->gtGetOp1()->gtGetOp1(), lvaNewObjArrayArgs);
            }

            static bool IsArgsAddr(GenTree* tree, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_ADDR) && (tree->gtGetOp1()->OperGet() == GT_LCL_VAR) &&
                       (tree->gtGetOp1()->AsLclVar()->GetLclNum() == lvaNewObjArrayArgs);
            }

            static bool IsComma(GenTree* tree)
            {
                return (tree != nullptr) && (tree->OperGet() == GT_COMMA);
            }
        };

        unsigned argIndex = 0;
        GenTree* comma;

        for (comma = argsArg->GetNode(); Match::IsComma(comma); comma = comma->gtGetOp2())
        {
            if (lowerBoundsSpecified)
            {
                //
                // In general lower bounds can be ignored because they're not needed to
                // calculate the total number of elements. But for single dimensional arrays
                // we need to know if the lower bound is 0 because in this case the runtime
                // creates a SDArray and this affects the way the array data offset is calculated.
                //

                if (rank == 1)
                {
                    GenTree* lowerBoundAssign = comma->gtGetOp1();
                    assert(Match::IsArgsFieldInit(lowerBoundAssign, argIndex, lvaNewObjArrayArgs));
                    GenTree* lowerBoundNode = lowerBoundAssign->gtGetOp2();

                    if (lowerBoundNode->IsIntegralConst(0))
                    {
                        isMDArray = false;
                    }
                }

                comma = comma->gtGetOp2();
                argIndex++;
            }

            GenTree* lengthNodeAssign = comma->gtGetOp1();
            assert(Match::IsArgsFieldInit(lengthNodeAssign, argIndex, lvaNewObjArrayArgs));
            GenTree* lengthNode = lengthNodeAssign->gtGetOp2();

            if (!lengthNode->IsCnsIntOrI())
            {
                return nullptr;
            }

            numElements *= S_SIZE_T(lengthNode->AsIntCon()->IconValue());
            argIndex++;
        }

        assert((comma != nullptr) && Match::IsArgsAddr(comma, lvaNewObjArrayArgs));

        if (argIndex != numArgs)
        {
            return nullptr;
        }
    }
    else
    {
        //
        // Make sure there are exactly two arguments:  the array class and
        // the number of elements.
        //

        GenTree* arrayLengthNode;

        GenTreeCall::Use* args = newArrayCall->AsCall()->gtCallArgs;
#ifdef FEATURE_READYTORUN_COMPILER
        if (newArrayCall->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1))
        {
            // Array length is 1st argument for readytorun helper
            arrayLengthNode = args->GetNode();
        }
        else
#endif
        {
            // Array length is 2nd argument for regular helper
            arrayLengthNode = args->GetNext()->GetNode();
        }

        //
        // This optimization is only valid for a constant array size.
        //
        if (arrayLengthNode->gtOper != GT_CNS_INT)
        {
            return nullptr;
        }

        numElements = S_SIZE_T(arrayLengthNode->AsIntCon()->gtIconVal);

        if (!info.compCompHnd->isSDArray(arrayClsHnd))
        {
            return nullptr;
        }
    }

    CORINFO_CLASS_HANDLE elemClsHnd;
    var_types            elementType = JITtype2varType(info.compCompHnd->getChildType(arrayClsHnd, &elemClsHnd));

    //
    // Note that genTypeSize will return zero for non primitive types, which is exactly
    // what we want (size will then be 0, and we will catch this in the conditional below).
    // Note that we don't expect this to fail for valid binaries, so we assert in the
    // non-verification case (the verification case should not assert but rather correctly
    // handle bad binaries).  This assert is not guarding any specific invariant, but rather
    // saying that we don't expect this to happen, and if it is hit, we need to investigate
    // why.
    //

    S_UINT32 elemSize(genTypeSize(elementType));
    S_UINT32 size = elemSize * S_UINT32(numElements);

    if (size.IsOverflow())
    {
        return nullptr;
    }

    if ((size.Value() == 0) || (varTypeIsGC(elementType)))
    {
        return nullptr;
    }

    void* initData = info.compCompHnd->getArrayInitializationData(fieldToken, size.Value());
    if (!initData)
    {
        return nullptr;
    }

    //
    // At this point we are ready to commit to implementing the InitializeArray
    // intrinsic using a struct assignment.  Pop the arguments from the stack and
    // return the struct assignment node.
    //

    impPopStack();
    impPopStack();

    const unsigned blkSize = size.Value();
    unsigned       dataOffset;

    if (isMDArray)
    {
        dataOffset = eeGetMDArrayDataOffset(elementType, rank);
    }
    else
    {
        dataOffset = eeGetArrayDataOffset(elementType);
    }

    GenTree* dstAddr = gtNewOperNode(GT_ADD, TYP_BYREF, arrayLocalNode, gtNewIconNode(dataOffset, TYP_I_IMPL));
    GenTree* dst     = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, dstAddr, typGetBlkLayout(blkSize));
    GenTree* src     = gtNewIndOfIconHandleNode(TYP_STRUCT, (size_t)initData, GTF_ICON_CONST_PTR, true);

#ifdef DEBUG
    src->gtGetOp1()->AsIntCon()->gtTargetHandle = THT_IntializeArrayIntrinsics;
#endif

    return gtNewBlkOpNode(dst,   // dst
                          src,   // src
                          false, // volatile
                          true); // copyBlock
}

//------------------------------------------------------------------------
// impIntrinsic: possibly expand intrinsic call into alternate IR sequence
//
// Arguments:
//    newobjThis - for constructor calls, the tree for the newly allocated object
//    clsHnd - handle for the intrinsic method's class
//    method - handle for the intrinsic method
//    sig    - signature of the intrinsic method
//    methodFlags - CORINFO_FLG_XXX flags of the intrinsic method
//    memberRef - the token for the intrinsic method
//    readonlyCall - true if call has a readonly prefix
//    tailCall - true if call is in tail position
//    pConstrainedResolvedToken -- resolved token for constrained call, or nullptr
//       if call is not constrained
//    constraintCallThisTransform -- this transform to apply for a constrained call
//    pIntrinsicID [OUT] -- intrinsic ID (see enumeration in corinfo.h)
//       for "traditional" jit intrinsics
//    isSpecialIntrinsic [OUT] -- set true if intrinsic expansion is a call
//       that is amenable to special downstream optimization opportunities
//
// Returns:
//    IR tree to use in place of the call, or nullptr if the jit should treat
//    the intrinsic call like a normal call.
//
//    pIntrinsicID set to non-illegal value if the call is recognized as a
//    traditional jit intrinsic, even if the intrinsic is not expaned.
//
//    isSpecial set true if the expansion is subject to special
//    optimizations later in the jit processing
//
// Notes:
//    On success the IR tree may be a call to a different method or an inline
//    sequence. If it is a call, then the intrinsic processing here is responsible
//    for handling all the special cases, as upon return to impImportCall
//    expanded intrinsics bypass most of the normal call processing.
//
//    Intrinsics are generally not recognized in minopts and debug codegen.
//
//    However, certain traditional intrinsics are identifed as "must expand"
//    if there is no fallback implmentation to invoke; these must be handled
//    in all codegen modes.
//
//    New style intrinsics (where the fallback implementation is in IL) are
//    identified as "must expand" if they are invoked from within their
//    own method bodies.
//

GenTree* Compiler::impIntrinsic(GenTree*                newobjThis,
                                CORINFO_CLASS_HANDLE    clsHnd,
                                CORINFO_METHOD_HANDLE   method,
                                CORINFO_SIG_INFO*       sig,
                                unsigned                methodFlags,
                                int                     memberRef,
                                bool                    readonlyCall,
                                bool                    tailCall,
                                CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                CORINFO_THIS_TRANSFORM  constraintCallThisTransform,
                                CorInfoIntrinsics*      pIntrinsicID,
                                bool*                   isSpecialIntrinsic)
{
    assert((methodFlags & (CORINFO_FLG_INTRINSIC | CORINFO_FLG_JIT_INTRINSIC)) != 0);

    bool              mustExpand  = false;
    bool              isSpecial   = false;
    CorInfoIntrinsics intrinsicID = CORINFO_INTRINSIC_Illegal;
    NamedIntrinsic    ni          = NI_Illegal;

    if ((methodFlags & CORINFO_FLG_INTRINSIC) != 0)
    {
        intrinsicID = info.compCompHnd->getIntrinsicID(method, &mustExpand);
    }

    if ((methodFlags & CORINFO_FLG_JIT_INTRINSIC) != 0)
    {
        // The recursive non-virtual calls to Jit intrinsics are must-expand by convention.
        mustExpand = mustExpand || (gtIsRecursiveCall(method) && !(methodFlags & CORINFO_FLG_VIRTUAL));

        if (intrinsicID == CORINFO_INTRINSIC_Illegal)
        {
            ni = lookupNamedIntrinsic(method);

            // We specially support the following on all platforms to allow for dead
            // code optimization and to more generally support recursive intrinsics.

            if (ni == NI_IsSupported_True)
            {
                assert(sig->numArgs == 0);
                return gtNewIconNode(true);
            }

            if (ni == NI_IsSupported_False)
            {
                assert(sig->numArgs == 0);
                return gtNewIconNode(false);
            }

            if (ni == NI_Throw_PlatformNotSupportedException)
            {
                return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
            }

#ifdef FEATURE_HW_INTRINSICS
            if ((ni > NI_HW_INTRINSIC_START) && (ni < NI_HW_INTRINSIC_END))
            {
                GenTree* hwintrinsic = impHWIntrinsic(ni, clsHnd, method, sig, mustExpand);

                if (mustExpand && (hwintrinsic == nullptr))
                {
                    return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_NOT_IMPLEMENTED, method, sig, mustExpand);
                }

                return hwintrinsic;
            }

            if ((ni > NI_SIMD_AS_HWINTRINSIC_START) && (ni < NI_SIMD_AS_HWINTRINSIC_END))
            {
                // These intrinsics aren't defined recursively and so they will never be mustExpand
                // Instead, they provide software fallbacks that will be executed instead.

                assert(!mustExpand);
                return impSimdAsHWIntrinsic(ni, clsHnd, method, sig, newobjThis);
            }
#endif // FEATURE_HW_INTRINSICS
        }
    }

    *pIntrinsicID = intrinsicID;

#ifndef TARGET_ARM
    genTreeOps interlockedOperator;
#endif

    if (intrinsicID == CORINFO_INTRINSIC_StubHelpers_GetStubContext)
    {
        // must be done regardless of DbgCode and MinOpts
        return gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL);
    }
#ifdef TARGET_64BIT
    if (intrinsicID == CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr)
    {
        // must be done regardless of DbgCode and MinOpts
        return gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL));
    }
#else
    assert(intrinsicID != CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr);
#endif

    if (intrinsicID == CORINFO_INTRINSIC_StubHelpers_NextCallReturnAddress)
    {
        // For now we just avoid inlining anything into these methods since
        // this intrinsic is only rarely used. We could do this better if we
        // wanted to by trying to match which call is the one we need to get
        // the return address of.
        info.compHasNextCallRetAddr = true;
        return new (this, GT_LABEL) GenTree(GT_LABEL, TYP_I_IMPL);
    }

    GenTree* retNode = nullptr;

    // Under debug and minopts, only expand what is required.
    // NextCallReturnAddress intrinsic returns the return address of the next call.
    // If that call is an intrinsic and is expanded, codegen for NextCallReturnAddress will fail.
    // To avoid that we conservatively expand only required intrinsics in methods that call
    // the NextCallReturnAddress intrinsic.
    if (!mustExpand && (opts.OptimizationDisabled() || info.compHasNextCallRetAddr))
    {
        *pIntrinsicID = CORINFO_INTRINSIC_Illegal;
        return retNode;
    }

    CorInfoType callJitType = sig->retType;
    var_types   callType    = JITtype2varType(callJitType);

    /* First do the intrinsics which are always smaller than a call */

    switch (intrinsicID)
    {
        GenTree* op1;

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        // TODO-ARM-CQ: reenable treating Interlocked operation as intrinsic

        // Note that CORINFO_INTRINSIC_InterlockedAdd32/64 are not actually used.
        // Anyway, we can import them as XADD and leave it to lowering/codegen to perform
        // whatever optimizations may arise from the fact that result value is not used.
        case CORINFO_INTRINSIC_InterlockedAdd32:
        case CORINFO_INTRINSIC_InterlockedXAdd32:
            interlockedOperator = GT_XADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXchg32:
            interlockedOperator = GT_XCHG;
            goto InterlockedBinOpCommon;

#ifdef TARGET_64BIT
        case CORINFO_INTRINSIC_InterlockedAdd64:
        case CORINFO_INTRINSIC_InterlockedXAdd64:
            interlockedOperator = GT_XADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXchg64:
            interlockedOperator = GT_XCHG;
            goto InterlockedBinOpCommon;
#endif // TARGET_AMD64

        InterlockedBinOpCommon:
            assert(callType != TYP_STRUCT);
            assert(sig->numArgs == 2);

            GenTree* op2;
            op2 = impPopStack().val;
            op1 = impPopStack().val;

            // This creates:
            //   val
            // XAdd
            //   addr
            //     field (for example)
            //
            // In the case where the first argument is the address of a local, we might
            // want to make this *not* make the var address-taken -- but atomic instructions
            // on a local are probably pretty useless anyway, so we probably don't care.

            op1 = gtNewOperNode(interlockedOperator, genActualType(callType), op1, op2);
            op1->gtFlags |= GTF_GLOB_REF | GTF_ASG;
            retNode = op1;
            break;
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)

        case CORINFO_INTRINSIC_MemoryBarrier:
        case CORINFO_INTRINSIC_MemoryBarrierLoad:

            assert(sig->numArgs == 0);

            op1 = new (this, GT_MEMORYBARRIER) GenTree(GT_MEMORYBARRIER, TYP_VOID);
            op1->gtFlags |= GTF_GLOB_REF | GTF_ASG;

            // On XARCH `CORINFO_INTRINSIC_MemoryBarrierLoad` fences need not be emitted.
            // However, we still need to capture the effect on reordering.
            if (intrinsicID == CORINFO_INTRINSIC_MemoryBarrierLoad)
            {
                op1->gtFlags |= GTF_MEMORYBARRIER_LOAD;
            }

            retNode = op1;
            break;

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        // TODO-ARM-CQ: reenable treating InterlockedCmpXchg32 operation as intrinsic
        case CORINFO_INTRINSIC_InterlockedCmpXchg32:
#ifdef TARGET_64BIT
        case CORINFO_INTRINSIC_InterlockedCmpXchg64:
#endif
        {
            assert(callType != TYP_STRUCT);
            assert(sig->numArgs == 3);
            GenTree* op2;
            GenTree* op3;

            op3 = impPopStack().val; // comparand
            op2 = impPopStack().val; // value
            op1 = impPopStack().val; // location

            GenTree* node = new (this, GT_CMPXCHG) GenTreeCmpXchg(genActualType(callType), op1, op2, op3);

            node->AsCmpXchg()->gtOpLocation->gtFlags |= GTF_DONT_CSE;
            retNode = node;
            break;
        }
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)

        case CORINFO_INTRINSIC_InitializeArray:
            retNode = impInitializeArrayIntrinsic(sig);
            break;

        case CORINFO_INTRINSIC_Array_Address:
        case CORINFO_INTRINSIC_Array_Get:
        case CORINFO_INTRINSIC_Array_Set:
            retNode = impArrayAccessIntrinsic(clsHnd, sig, memberRef, readonlyCall, intrinsicID);
            break;

        case CORINFO_INTRINSIC_RTH_GetValueInternal:
            op1 = impStackTop(0).val;
            if (op1->gtOper == GT_CALL && (op1->AsCall()->gtCallType == CT_HELPER) &&
                gtIsTypeHandleToRuntimeTypeHandleHelper(op1->AsCall()))
            {
                // Old tree
                // Helper-RuntimeTypeHandle -> TreeToGetNativeTypeHandle
                //
                // New tree
                // TreeToGetNativeTypeHandle

                // Remove call to helper and return the native TypeHandle pointer that was the parameter
                // to that helper.

                op1 = impPopStack().val;

                // Get native TypeHandle argument to old helper
                GenTreeCall::Use* arg = op1->AsCall()->gtCallArgs;
                assert(arg->GetNext() == nullptr);
                op1     = arg->GetNode();
                retNode = op1;
            }
            // Call the regular function.
            break;

        case CORINFO_INTRINSIC_Object_GetType:
        {
            JITDUMP("\n impIntrinsic: call to Object.GetType\n");
            op1 = impStackTop(0).val;

            // If we're calling GetType on a boxed value, just get the type directly.
            if (op1->IsBoxedValue())
            {
                JITDUMP("Attempting to optimize box(...).getType() to direct type construction\n");

                // Try and clean up the box. Obtain the handle we
                // were going to pass to the newobj.
                GenTree* boxTypeHandle = gtTryRemoveBoxUpstreamEffects(op1, BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE);

                if (boxTypeHandle != nullptr)
                {
                    // Note we don't need to play the TYP_STRUCT games here like
                    // do for LDTOKEN since the return value of this operator is Type,
                    // not RuntimeTypeHandle.
                    impPopStack();
                    GenTreeCall::Use* helperArgs = gtNewCallArgs(boxTypeHandle);
                    GenTree*          runtimeType =
                        gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, TYP_REF, helperArgs);
                    retNode = runtimeType;
                }
            }

            // If we have a constrained callvirt with a "box this" transform
            // we know we have a value class and hence an exact type.
            //
            // If so, instead of boxing and then extracting the type, just
            // construct the type directly.
            if ((retNode == nullptr) && (pConstrainedResolvedToken != nullptr) &&
                (constraintCallThisTransform == CORINFO_BOX_THIS))
            {
                // Ensure this is one of the is simple box cases (in particular, rule out nullables).
                const CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pConstrainedResolvedToken->hClass);
                const bool            isSafeToOptimize = (boxHelper == CORINFO_HELP_BOX);

                if (isSafeToOptimize)
                {
                    JITDUMP("Optimizing constrained box-this obj.getType() to direct type construction\n");
                    impPopStack();
                    GenTree* typeHandleOp =
                        impTokenToHandle(pConstrainedResolvedToken, nullptr, true /* mustRestoreHandle */);
                    if (typeHandleOp == nullptr)
                    {
                        assert(compDonotInline());
                        return nullptr;
                    }
                    GenTreeCall::Use* helperArgs = gtNewCallArgs(typeHandleOp);
                    GenTree*          runtimeType =
                        gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, TYP_REF, helperArgs);
                    retNode = runtimeType;
                }
            }

#ifdef DEBUG
            if (retNode != nullptr)
            {
                JITDUMP("Optimized result for call to GetType is\n");
                if (verbose)
                {
                    gtDispTree(retNode);
                }
            }
#endif

            // Else expand as an intrinsic, unless the call is constrained,
            // in which case we defer expansion to allow impImportCall do the
            // special constraint processing.
            if ((retNode == nullptr) && (pConstrainedResolvedToken == nullptr))
            {
                JITDUMP("Expanding as special intrinsic\n");
                impPopStack();
                op1 = new (this, GT_INTRINSIC) GenTreeIntrinsic(genActualType(callType), op1, intrinsicID, ni, method);

                // Set the CALL flag to indicate that the operator is implemented by a call.
                // Set also the EXCEPTION flag because the native implementation of
                // CORINFO_INTRINSIC_Object_GetType intrinsic can throw NullReferenceException.
                op1->gtFlags |= (GTF_CALL | GTF_EXCEPT);
                retNode = op1;
                // Might be further optimizable, so arrange to leave a mark behind
                isSpecial = true;
            }

            if (retNode == nullptr)
            {
                JITDUMP("Leaving as normal call\n");
                // Might be further optimizable, so arrange to leave a mark behind
                isSpecial = true;
            }

            break;
        }

        // Implement ByReference Ctor.  This wraps the assignment of the ref into a byref-like field
        // in a value type.  The canonical example of this is Span<T>. In effect this is just a
        // substitution.  The parameter byref will be assigned into the newly allocated object.
        case CORINFO_INTRINSIC_ByReference_Ctor:
        {
            // Remove call to constructor and directly assign the byref passed
            // to the call to the first slot of the ByReference struct.
            op1                                    = impPopStack().val;
            GenTree*             thisptr           = newobjThis;
            CORINFO_FIELD_HANDLE fldHnd            = info.compCompHnd->getFieldInClass(clsHnd, 0);
            GenTree*             field             = gtNewFieldRef(TYP_BYREF, fldHnd, thisptr, 0);
            GenTree*             assign            = gtNewAssignNode(field, op1);
            GenTree*             byReferenceStruct = gtCloneExpr(thisptr->gtGetOp1());
            assert(byReferenceStruct != nullptr);
            impPushOnStack(byReferenceStruct, typeInfo(TI_STRUCT, clsHnd));
            retNode = assign;
            break;
        }
        // Implement ptr value getter for ByReference struct.
        case CORINFO_INTRINSIC_ByReference_Value:
        {
            op1                         = impPopStack().val;
            CORINFO_FIELD_HANDLE fldHnd = info.compCompHnd->getFieldInClass(clsHnd, 0);
            GenTree*             field  = gtNewFieldRef(TYP_BYREF, fldHnd, op1, 0);
            retNode                     = field;
            break;
        }

        case CORINFO_INTRINSIC_GetRawHandle:
        {
            noway_assert(IsTargetAbi(CORINFO_CORERT_ABI)); // Only CoreRT supports it.
            CORINFO_RESOLVED_TOKEN resolvedToken;
            resolvedToken.tokenContext = impTokenLookupContextHandle;
            resolvedToken.tokenScope   = info.compScopeHnd;
            resolvedToken.token        = memberRef;
            resolvedToken.tokenType    = CORINFO_TOKENKIND_Method;

            CORINFO_GENERICHANDLE_RESULT embedInfo;
            info.compCompHnd->expandRawHandleIntrinsic(&resolvedToken, &embedInfo);

            GenTree* rawHandle = impLookupToTree(&resolvedToken, &embedInfo.lookup, gtTokenToIconFlags(memberRef),
                                                 embedInfo.compileTimeHandle);
            if (rawHandle == nullptr)
            {
                return nullptr;
            }

            noway_assert(genTypeSize(rawHandle->TypeGet()) == genTypeSize(TYP_I_IMPL));

            unsigned rawHandleSlot = lvaGrabTemp(true DEBUGARG("rawHandle"));
            impAssignTempGen(rawHandleSlot, rawHandle, clsHnd, (unsigned)CHECK_SPILL_NONE);

            GenTree*  lclVar     = gtNewLclvNode(rawHandleSlot, TYP_I_IMPL);
            GenTree*  lclVarAddr = gtNewOperNode(GT_ADDR, TYP_I_IMPL, lclVar);
            var_types resultType = JITtype2varType(sig->retType);
            retNode              = gtNewOperNode(GT_IND, resultType, lclVarAddr);

            break;
        }

        default:
            /* Unknown intrinsic */
            intrinsicID = CORINFO_INTRINSIC_Illegal;
            break;
    }

    // Look for new-style jit intrinsics by name
    if (ni != NI_Illegal)
    {
        assert(retNode == nullptr);
        switch (ni)
        {
            case NI_System_String_get_Chars:
            {
                GenTree* op2 = impPopStack().val;
                GenTree* op1 = impPopStack().val;
                retNode      = gtNewIndexRef(TYP_USHORT, op1, op2);
                retNode->gtFlags |= GTF_INX_STRING_LAYOUT;
                break;
            }

            case NI_System_String_get_Length:
            {
                GenTree* op1 = impPopStack().val;
                if (opts.OptimizationEnabled())
                {
                    if (op1->OperIs(GT_CNS_STR))
                    {
                        // Optimize `ldstr + String::get_Length()` to CNS_INT
                        // e.g. "Hello".Length => 5
                        GenTreeIntCon* iconNode = gtNewStringLiteralLength(op1->AsStrCon());
                        if (iconNode != nullptr)
                        {
                            retNode = iconNode;
                            break;
                        }
                    }
                    GenTreeArrLen* arrLen = gtNewArrLen(TYP_INT, op1, OFFSETOF__CORINFO_String__stringLen, compCurBB);
                    op1                   = arrLen;
                }
                else
                {
                    /* Create the expression "*(str_addr + stringLengthOffset)" */
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        gtNewIconNode(OFFSETOF__CORINFO_String__stringLen, TYP_I_IMPL));
                    op1 = gtNewOperNode(GT_IND, TYP_INT, op1);
                }

                // Getting the length of a null string should throw
                op1->gtFlags |= GTF_EXCEPT;

                retNode = op1;
                break;
            }

            case NI_System_Span_get_Item:
            case NI_System_ReadOnlySpan_get_Item:
            {
                // Have index, stack pointer-to Span<T> s on the stack. Expand to:
                //
                // For Span<T>
                //   Comma
                //     BoundsCheck(index, s->_length)
                //     s->_pointer + index * sizeof(T)
                //
                // For ReadOnlySpan<T> -- same expansion, as it now returns a readonly ref
                //
                // Signature should show one class type parameter, which
                // we need to examine.
                assert(sig->sigInst.classInstCount == 1);
                assert(sig->numArgs == 1);
                CORINFO_CLASS_HANDLE spanElemHnd = sig->sigInst.classInst[0];
                const unsigned       elemSize    = info.compCompHnd->getClassSize(spanElemHnd);
                assert(elemSize > 0);

                const bool isReadOnly = (ni == NI_System_ReadOnlySpan_get_Item);

                JITDUMP("\nimpIntrinsic: Expanding %sSpan<T>.get_Item, T=%s, sizeof(T)=%u\n",
                        isReadOnly ? "ReadOnly" : "", info.compCompHnd->getClassName(spanElemHnd), elemSize);

                GenTree* index          = impPopStack().val;
                GenTree* ptrToSpan      = impPopStack().val;
                GenTree* indexClone     = nullptr;
                GenTree* ptrToSpanClone = nullptr;
                assert(varTypeIsIntegral(index));
                assert(ptrToSpan->TypeGet() == TYP_BYREF);

#if defined(DEBUG)
                if (verbose)
                {
                    printf("with ptr-to-span\n");
                    gtDispTree(ptrToSpan);
                    printf("and index\n");
                    gtDispTree(index);
                }
#endif // defined(DEBUG)

                // We need to use both index and ptr-to-span twice, so clone or spill.
                index = impCloneExpr(index, &indexClone, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                     nullptr DEBUGARG("Span.get_Item index"));
                ptrToSpan = impCloneExpr(ptrToSpan, &ptrToSpanClone, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                         nullptr DEBUGARG("Span.get_Item ptrToSpan"));

                // Bounds check
                CORINFO_FIELD_HANDLE lengthHnd    = info.compCompHnd->getFieldInClass(clsHnd, 1);
                const unsigned       lengthOffset = info.compCompHnd->getFieldOffset(lengthHnd);
                GenTree*             length       = gtNewFieldRef(TYP_INT, lengthHnd, ptrToSpan, lengthOffset);
                GenTree*             boundsCheck  = new (this, GT_ARR_BOUNDS_CHECK)
                    GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, index, length, SCK_RNGCHK_FAIL);

                // Element access
                GenTree*             indexIntPtr = impImplicitIorI4Cast(indexClone, TYP_I_IMPL);
                GenTree*             sizeofNode  = gtNewIconNode(elemSize);
                GenTree*             mulNode     = gtNewOperNode(GT_MUL, TYP_I_IMPL, indexIntPtr, sizeofNode);
                CORINFO_FIELD_HANDLE ptrHnd      = info.compCompHnd->getFieldInClass(clsHnd, 0);
                const unsigned       ptrOffset   = info.compCompHnd->getFieldOffset(ptrHnd);
                GenTree*             data        = gtNewFieldRef(TYP_BYREF, ptrHnd, ptrToSpanClone, ptrOffset);
                GenTree*             result      = gtNewOperNode(GT_ADD, TYP_BYREF, data, mulNode);

                // Prepare result
                var_types resultType = JITtype2varType(sig->retType);
                assert(resultType == result->TypeGet());
                retNode = gtNewOperNode(GT_COMMA, resultType, boundsCheck, result);

                break;
            }

            case NI_System_Type_GetTypeFromHandle:
            {
                GenTree*        op1 = impStackTop(0).val;
                CorInfoHelpFunc typeHandleHelper;
                if (op1->gtOper == GT_CALL && (op1->AsCall()->gtCallType == CT_HELPER) &&
                    gtIsTypeHandleToRuntimeTypeHandleHelper(op1->AsCall(), &typeHandleHelper))
                {
                    op1 = impPopStack().val;
                    // Replace helper with a more specialized helper that returns RuntimeType
                    if (typeHandleHelper == CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE)
                    {
                        typeHandleHelper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE;
                    }
                    else
                    {
                        assert(typeHandleHelper == CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL);
                        typeHandleHelper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL;
                    }
                    assert(op1->AsCall()->gtCallArgs->GetNext() == nullptr);
                    op1         = gtNewHelperCallNode(typeHandleHelper, TYP_REF, op1->AsCall()->gtCallArgs);
                    op1->gtType = TYP_REF;
                    retNode     = op1;
                }
                break;
            }

            case NI_System_Type_op_Equality:
            case NI_System_Type_op_Inequality:
            {
                JITDUMP("Importing Type.op_*Equality intrinsic\n");
                GenTree* op1     = impStackTop(1).val;
                GenTree* op2     = impStackTop(0).val;
                GenTree* optTree = gtFoldTypeEqualityCall(ni == NI_System_Type_op_Equality, op1, op2);
                if (optTree != nullptr)
                {
                    // Success, clean up the evaluation stack.
                    impPopStack();
                    impPopStack();

                    // See if we can optimize even further, to a handle compare.
                    optTree = gtFoldTypeCompare(optTree);

                    // See if we can now fold a handle compare to a constant.
                    optTree = gtFoldExpr(optTree);

                    retNode = optTree;
                }
                else
                {
                    // Retry optimizing these later
                    isSpecial = true;
                }
                break;
            }

            case NI_System_Enum_HasFlag:
            {
                GenTree* thisOp  = impStackTop(1).val;
                GenTree* flagOp  = impStackTop(0).val;
                GenTree* optTree = gtOptimizeEnumHasFlag(thisOp, flagOp);

                if (optTree != nullptr)
                {
                    // Optimization successful. Pop the stack for real.
                    impPopStack();
                    impPopStack();
                    retNode = optTree;
                }
                else
                {
                    // Retry optimizing this during morph.
                    isSpecial = true;
                }

                break;
            }

            case NI_System_Type_IsAssignableFrom:
            {
                GenTree* typeTo   = impStackTop(1).val;
                GenTree* typeFrom = impStackTop(0).val;

                retNode = impTypeIsAssignable(typeTo, typeFrom);
                break;
            }

            case NI_System_Type_IsAssignableTo:
            {
                GenTree* typeTo   = impStackTop(0).val;
                GenTree* typeFrom = impStackTop(1).val;

                retNode = impTypeIsAssignable(typeTo, typeFrom);
                break;
            }

            case NI_System_Type_get_IsValueType:
            {
                // Optimize
                //
                //   call Type.GetTypeFromHandle (which is replaced with CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE)
                //   call Type.IsValueType
                //
                // to `true` or `false`
                // e.g. `typeof(int).IsValueType` => `true`
                if (impStackTop().val->IsCall())
                {
                    GenTreeCall* call = impStackTop().val->AsCall();
                    if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE))
                    {
                        CORINFO_CLASS_HANDLE hClass = gtGetHelperArgClassHandle(call->gtCallArgs->GetNode());
                        if (hClass != NO_CLASS_HANDLE)
                        {
                            retNode =
                                gtNewIconNode((eeIsValueClass(hClass) &&
                                               // pointers are not value types (e.g. typeof(int*).IsValueType is false)
                                               info.compCompHnd->asCorInfoType(hClass) != CORINFO_TYPE_PTR)
                                                  ? 1
                                                  : 0);
                            impPopStack(); // drop CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE call
                        }
                    }
                }
                break;
            }

            case NI_System_Threading_Thread_get_ManagedThreadId:
            {
                if (opts.OptimizationEnabled() && impStackTop().val->OperIs(GT_RET_EXPR))
                {
                    GenTreeCall* call = impStackTop().val->AsRetExpr()->gtInlineCandidate->AsCall();
                    if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
                    {
                        if (lookupNamedIntrinsic(call->gtCallMethHnd) == NI_System_Threading_Thread_get_CurrentThread)
                        {
                            // drop get_CurrentThread() call
                            impPopStack();
                            call->ReplaceWith(gtNewNothingNode(), this);
                            retNode = gtNewHelperCallNode(CORINFO_HELP_GETCURRENTMANAGEDTHREADID, TYP_INT);
                        }
                    }
                }
                break;
            }

#ifdef TARGET_ARM64
            // Intrinsify Interlocked.Or and Interlocked.And only for arm64-v8.1 (and newer)
            // TODO-CQ: Implement for XArch (https://github.com/dotnet/runtime/issues/32239).
            case NI_System_Threading_Interlocked_Or:
            case NI_System_Threading_Interlocked_And:
            {
                if (opts.OptimizationEnabled() && compOpportunisticallyDependsOn(InstructionSet_Atomics))
                {
                    assert(sig->numArgs == 2);
                    GenTree*   op2 = impPopStack().val;
                    GenTree*   op1 = impPopStack().val;
                    genTreeOps op  = (ni == NI_System_Threading_Interlocked_Or) ? GT_XORR : GT_XAND;
                    retNode        = gtNewOperNode(op, genActualType(callType), op1, op2);
                    retNode->gtFlags |= GTF_GLOB_REF | GTF_ASG;
                }
                break;
            }
#endif // TARGET_ARM64

#ifdef FEATURE_HW_INTRINSICS
            case NI_System_Math_FusedMultiplyAdd:
            {
#ifdef TARGET_XARCH
                if (compExactlyDependsOn(InstructionSet_FMA) && supportSIMDTypes())
                {
                    assert(varTypeIsFloating(callType));

                    // We are constructing a chain of intrinsics similar to:
                    //    return FMA.MultiplyAddScalar(
                    //        Vector128.CreateScalarUnsafe(x),
                    //        Vector128.CreateScalarUnsafe(y),
                    //        Vector128.CreateScalarUnsafe(z)
                    //    ).ToScalar();

                    GenTree* op3 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* res =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, op3, NI_FMA_MultiplyAddScalar, callJitType, 16);

                    retNode = gtNewSimdHWIntrinsicNode(callType, res, NI_Vector128_ToScalar, callJitType, 16);
                    break;
                }
#elif defined(TARGET_ARM64)
                if (compExactlyDependsOn(InstructionSet_AdvSimd))
                {
                    assert(varTypeIsFloating(callType));

                    // We are constructing a chain of intrinsics similar to:
                    //    return AdvSimd.FusedMultiplyAddScalar(
                    //        Vector64.Create{ScalarUnsafe}(z),
                    //        Vector64.Create{ScalarUnsafe}(y),
                    //        Vector64.Create{ScalarUnsafe}(x)
                    //    ).ToScalar();

                    NamedIntrinsic createVector64 =
                        (callType == TYP_DOUBLE) ? NI_Vector64_Create : NI_Vector64_CreateScalarUnsafe;

                    constexpr unsigned int simdSize = 8;

                    GenTree* op3 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);
                    GenTree* op2 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);
                    GenTree* op1 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);

                    // Note that AdvSimd.FusedMultiplyAddScalar(op1,op2,op3) corresponds to op1 + op2 * op3
                    // while Math{F}.FusedMultiplyAddScalar(op1,op2,op3) corresponds to op1 * op2 + op3
                    retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op3, op2, op1, NI_AdvSimd_FusedMultiplyAddScalar,
                                                       callJitType, simdSize);

                    retNode = gtNewSimdHWIntrinsicNode(callType, retNode, NI_Vector64_ToScalar, callJitType, simdSize);
                    break;
                }
#endif

                // TODO-CQ-XArch: Ideally we would create a GT_INTRINSIC node for fma, however, that currently
                // requires more extensive changes to valuenum to support methods with 3 operands

                // We want to generate a GT_INTRINSIC node in the case the call can't be treated as
                // a target intrinsic so that we can still benefit from CSE and constant folding.

                break;
            }
#endif // FEATURE_HW_INTRINSICS

            case NI_System_Math_Abs:
            case NI_System_Math_Acos:
            case NI_System_Math_Acosh:
            case NI_System_Math_Asin:
            case NI_System_Math_Asinh:
            case NI_System_Math_Atan:
            case NI_System_Math_Atanh:
            case NI_System_Math_Atan2:
            case NI_System_Math_Cbrt:
            case NI_System_Math_Ceiling:
            case NI_System_Math_Cos:
            case NI_System_Math_Cosh:
            case NI_System_Math_Exp:
            case NI_System_Math_Floor:
            case NI_System_Math_FMod:
            case NI_System_Math_ILogB:
            case NI_System_Math_Log:
            case NI_System_Math_Log2:
            case NI_System_Math_Log10:
            case NI_System_Math_Pow:
            case NI_System_Math_Round:
            case NI_System_Math_Sin:
            case NI_System_Math_Sinh:
            case NI_System_Math_Sqrt:
            case NI_System_Math_Tan:
            case NI_System_Math_Tanh:
            {
                retNode = impMathIntrinsic(method, sig, callType, ni, tailCall);
                break;
            }

            case NI_System_Array_Clone:
            case NI_System_Collections_Generic_Comparer_get_Default:
            case NI_System_Collections_Generic_EqualityComparer_get_Default:
            case NI_System_Object_MemberwiseClone:
            case NI_System_Threading_Thread_get_CurrentThread:
            {
                // Flag for later handling.
                isSpecial = true;
                break;
            }

            case NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness:
            {
                assert(sig->numArgs == 1);

                // We expect the return type of the ReverseEndianness routine to match the type of the
                // one and only argument to the method. We use a special instruction for 16-bit
                // BSWAPs since on x86 processors this is implemented as ROR <16-bit reg>, 8. Additionally,
                // we only emit 64-bit BSWAP instructions on 64-bit archs; if we're asked to perform a
                // 64-bit byte swap on a 32-bit arch, we'll fall to the default case in the switch block below.

                switch (sig->retType)
                {
                    case CorInfoType::CORINFO_TYPE_SHORT:
                    case CorInfoType::CORINFO_TYPE_USHORT:
                        retNode = gtNewCastNode(TYP_INT, gtNewOperNode(GT_BSWAP16, TYP_INT, impPopStack().val), false,
                                                callType);
                        break;

                    case CorInfoType::CORINFO_TYPE_INT:
                    case CorInfoType::CORINFO_TYPE_UINT:
#ifdef TARGET_64BIT
                    case CorInfoType::CORINFO_TYPE_LONG:
                    case CorInfoType::CORINFO_TYPE_ULONG:
#endif // TARGET_64BIT
                        retNode = gtNewOperNode(GT_BSWAP, callType, impPopStack().val);
                        break;

                    default:
                        // This default case gets hit on 32-bit archs when a call to a 64-bit overload
                        // of ReverseEndianness is encountered. In that case we'll let JIT treat this as a standard
                        // method call, where the implementation decomposes the operation into two 32-bit
                        // bswap routines. If the input to the 64-bit function is a constant, then we rely
                        // on inlining + constant folding of 32-bit bswaps to effectively constant fold
                        // the 64-bit call site.
                        break;
                }

                break;
            }

            // Fold PopCount for constant input
            case NI_System_Numerics_BitOperations_PopCount:
            {
                assert(sig->numArgs == 1);
                if (impStackTop().val->IsIntegralConst())
                {
                    typeInfo argType = verParseArgSigToTypeInfo(sig, sig->args).NormaliseForStack();
                    INT64    cns     = impPopStack().val->AsIntConCommon()->IntegralValue();
                    if (argType.IsType(TI_LONG))
                    {
                        retNode = gtNewIconNode(genCountBits(cns), callType);
                    }
                    else
                    {
                        assert(argType.IsType(TI_INT));
                        retNode = gtNewIconNode(genCountBits(static_cast<unsigned>(cns)), callType);
                    }
                }
                break;
            }

            case NI_System_GC_KeepAlive:
            {
                retNode = gtNewOperNode(GT_KEEPALIVE, TYP_VOID, impPopStack().val);

                // Prevent both reordering and removal. Invalid optimizations of GC.KeepAlive are
                // very subtle and hard to observe. Thus we are conservatively marking it with both
                // GTF_CALL and GTF_GLOB_REF side-effects even though it may be more than strictly
                // necessary. The conservative side-effects are unlikely to have negative impact
                // on code quality in this case.
                retNode->gtFlags |= (GTF_CALL | GTF_GLOB_REF);

                break;
            }

            default:
                break;
        }
    }

    if (mustExpand && (retNode == nullptr))
    {
        assert(!"Unhandled must expand intrinsic, throwing PlatformNotSupportedException");
        return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }

    // Optionally report if this intrinsic is special
    // (that is, potentially re-optimizable during morph).
    if (isSpecialIntrinsic != nullptr)
    {
        *isSpecialIntrinsic = isSpecial;
    }

    return retNode;
}

GenTree* Compiler::impTypeIsAssignable(GenTree* typeTo, GenTree* typeFrom)
{
    // Optimize patterns like:
    //
    //   typeof(TTo).IsAssignableFrom(typeof(TTFrom))
    //   valueTypeVar.GetType().IsAssignableFrom(typeof(TTFrom))
    //   typeof(TTFrom).IsAssignableTo(typeof(TTo))
    //   typeof(TTFrom).IsAssignableTo(valueTypeVar.GetType())
    //
    // to true/false

    if (typeTo->IsCall() && typeFrom->IsCall())
    {
        // make sure both arguments are `typeof()`
        CORINFO_METHOD_HANDLE hTypeof = eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE);
        if ((typeTo->AsCall()->gtCallMethHnd == hTypeof) && (typeFrom->AsCall()->gtCallMethHnd == hTypeof))
        {
            CORINFO_CLASS_HANDLE hClassTo   = gtGetHelperArgClassHandle(typeTo->AsCall()->gtCallArgs->GetNode());
            CORINFO_CLASS_HANDLE hClassFrom = gtGetHelperArgClassHandle(typeFrom->AsCall()->gtCallArgs->GetNode());

            if (hClassTo == NO_CLASS_HANDLE || hClassFrom == NO_CLASS_HANDLE)
            {
                return nullptr;
            }

            TypeCompareState castResult = info.compCompHnd->compareTypesForCast(hClassFrom, hClassTo);
            if (castResult == TypeCompareState::May)
            {
                // requires runtime check
                // e.g. __Canon, COMObjects, Nullable
                return nullptr;
            }

            GenTreeIntCon* retNode = gtNewIconNode((castResult == TypeCompareState::Must) ? 1 : 0);
            impPopStack(); // drop both CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE calls
            impPopStack();

            return retNode;
        }
    }

    return nullptr;
}

GenTree* Compiler::impMathIntrinsic(CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    var_types             callType,
                                    NamedIntrinsic        intrinsicName,
                                    bool                  tailCall)
{
    GenTree* op1;
    GenTree* op2;

    assert(callType != TYP_STRUCT);
    assert(IsMathIntrinsic(intrinsicName));

    op1 = nullptr;

#if !defined(TARGET_X86)
    // Intrinsics that are not implemented directly by target instructions will
    // be re-materialized as users calls in rationalizer. For prefixed tail calls,
    // don't do this optimization, because
    //  a) For back compatibility reasons on desktop .NET Framework 4.6 / 4.6.1
    //  b) It will be non-trivial task or too late to re-materialize a surviving
    //     tail prefixed GT_INTRINSIC as tail call in rationalizer.
    if (!IsIntrinsicImplementedByUserCall(intrinsicName) || !tailCall)
#else
    // On x86 RyuJIT, importing intrinsics that are implemented as user calls can cause incorrect calculation
    // of the depth of the stack if these intrinsics are used as arguments to another call. This causes bad
    // code generation for certain EH constructs.
    if (!IsIntrinsicImplementedByUserCall(intrinsicName))
#endif
    {
        CORINFO_CLASS_HANDLE    tmpClass;
        CORINFO_ARG_LIST_HANDLE arg;
        var_types               op1Type;
        var_types               op2Type;

        switch (sig->numArgs)
        {
            case 1:
                op1 = impPopStack().val;

                arg     = sig->args;
                op1Type = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg, &tmpClass)));

                if (op1->TypeGet() != genActualType(op1Type))
                {
                    assert(varTypeIsFloating(op1));
                    op1 = gtNewCastNode(callType, op1, false, callType);
                }

                op1 = new (this, GT_INTRINSIC)
                    GenTreeIntrinsic(genActualType(callType), op1, CORINFO_INTRINSIC_Illegal, intrinsicName, method);
                break;

            case 2:
                op2 = impPopStack().val;
                op1 = impPopStack().val;

                arg     = sig->args;
                op1Type = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg, &tmpClass)));

                if (op1->TypeGet() != genActualType(op1Type))
                {
                    assert(varTypeIsFloating(op1));
                    op1 = gtNewCastNode(callType, op1, false, callType);
                }

                arg     = info.compCompHnd->getArgNext(arg);
                op2Type = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg, &tmpClass)));

                if (op2->TypeGet() != genActualType(op2Type))
                {
                    assert(varTypeIsFloating(op2));
                    op2 = gtNewCastNode(callType, op2, false, callType);
                }

                op1 = new (this, GT_INTRINSIC) GenTreeIntrinsic(genActualType(callType), op1, op2,
                                                                CORINFO_INTRINSIC_Illegal, intrinsicName, method);
                break;

            default:
                NO_WAY("Unsupported number of args for Math Intrinsic");
        }

        if (IsIntrinsicImplementedByUserCall(intrinsicName))
        {
            op1->gtFlags |= GTF_CALL;
        }
    }

    return op1;
}

//------------------------------------------------------------------------
// lookupNamedIntrinsic: map method to jit named intrinsic value
//
// Arguments:
//    method -- method handle for method
//
// Return Value:
//    Id for the named intrinsic, or Illegal if none.
//
// Notes:
//    method should have CORINFO_FLG_JIT_INTRINSIC set in its attributes,
//    otherwise it is not a named jit intrinsic.
//

NamedIntrinsic Compiler::lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method)
{
    const char* className          = nullptr;
    const char* namespaceName      = nullptr;
    const char* enclosingClassName = nullptr;
    const char* methodName =
        info.compCompHnd->getMethodNameFromMetadata(method, &className, &namespaceName, &enclosingClassName);

    JITDUMP("Named Intrinsic ");

    if (namespaceName != nullptr)
    {
        JITDUMP("%s.", namespaceName);
    }
    if (enclosingClassName != nullptr)
    {
        JITDUMP("%s.", enclosingClassName);
    }
    if (className != nullptr)
    {
        JITDUMP("%s.", className);
    }
    if (methodName != nullptr)
    {
        JITDUMP("%s", methodName);
    }
    JITDUMP(": ");

    if ((namespaceName == nullptr) || (className == nullptr) || (methodName == nullptr))
    {
        JITDUMP("Not recognized, not enough metadata\n");
        return NI_Illegal;
    }

    NamedIntrinsic result = NI_Illegal;

    if (strcmp(namespaceName, "System") == 0)
    {
        if ((strcmp(className, "Enum") == 0) && (strcmp(methodName, "HasFlag") == 0))
        {
            result = NI_System_Enum_HasFlag;
        }
        else if (strcmp(className, "Math") == 0 || strcmp(className, "MathF") == 0)
        {
            if (strcmp(methodName, "Abs") == 0)
            {
                result = NI_System_Math_Abs;
            }
            else if (strcmp(methodName, "Acos") == 0)
            {
                result = NI_System_Math_Acos;
            }
            else if (strcmp(methodName, "Acosh") == 0)
            {
                result = NI_System_Math_Acosh;
            }
            else if (strcmp(methodName, "Asin") == 0)
            {
                result = NI_System_Math_Asin;
            }
            else if (strcmp(methodName, "Asinh") == 0)
            {
                result = NI_System_Math_Asinh;
            }
            else if (strcmp(methodName, "Atan") == 0)
            {
                result = NI_System_Math_Atan;
            }
            else if (strcmp(methodName, "Atanh") == 0)
            {
                result = NI_System_Math_Atanh;
            }
            else if (strcmp(methodName, "Atan2") == 0)
            {
                result = NI_System_Math_Atan2;
            }
            else if (strcmp(methodName, "Cbrt") == 0)
            {
                result = NI_System_Math_Cbrt;
            }
            else if (strcmp(methodName, "Ceiling") == 0)
            {
                result = NI_System_Math_Ceiling;
            }
            else if (strcmp(methodName, "Cos") == 0)
            {
                result = NI_System_Math_Cos;
            }
            else if (strcmp(methodName, "Cosh") == 0)
            {
                result = NI_System_Math_Cosh;
            }
            else if (strcmp(methodName, "Exp") == 0)
            {
                result = NI_System_Math_Exp;
            }
            else if (strcmp(methodName, "Floor") == 0)
            {
                result = NI_System_Math_Floor;
            }
            else if (strcmp(methodName, "FMod") == 0)
            {
                result = NI_System_Math_FMod;
            }
            else if (strcmp(methodName, "FusedMultiplyAdd") == 0)
            {
                result = NI_System_Math_FusedMultiplyAdd;
            }
            else if (strcmp(methodName, "ILogB") == 0)
            {
                result = NI_System_Math_ILogB;
            }
            else if (strcmp(methodName, "Log") == 0)
            {
                result = NI_System_Math_Log;
            }
            else if (strcmp(methodName, "Log2") == 0)
            {
                result = NI_System_Math_Log2;
            }
            else if (strcmp(methodName, "Log10") == 0)
            {
                result = NI_System_Math_Log10;
            }
            else if (strcmp(methodName, "Pow") == 0)
            {
                result = NI_System_Math_Pow;
            }
            else if (strcmp(methodName, "Round") == 0)
            {
                result = NI_System_Math_Round;
            }
            else if (strcmp(methodName, "Sin") == 0)
            {
                result = NI_System_Math_Sin;
            }
            else if (strcmp(methodName, "Sinh") == 0)
            {
                result = NI_System_Math_Sinh;
            }
            else if (strcmp(methodName, "Sqrt") == 0)
            {
                result = NI_System_Math_Sqrt;
            }
            else if (strcmp(methodName, "Tan") == 0)
            {
                result = NI_System_Math_Tan;
            }
            else if (strcmp(methodName, "Tanh") == 0)
            {
                result = NI_System_Math_Tanh;
            }
        }
        else if (strcmp(className, "GC") == 0)
        {
            if (strcmp(methodName, "KeepAlive") == 0)
            {
                result = NI_System_GC_KeepAlive;
            }
        }
        else if (strcmp(className, "Array") == 0)
        {
            if (strcmp(methodName, "Clone") == 0)
            {
                result = NI_System_Array_Clone;
            }
        }
        else if (strcmp(className, "Object") == 0)
        {
            if (strcmp(methodName, "MemberwiseClone") == 0)
            {
                result = NI_System_Object_MemberwiseClone;
            }
        }
        else if (strcmp(className, "Type") == 0)
        {
            if (strcmp(methodName, "get_IsValueType") == 0)
            {
                result = NI_System_Type_get_IsValueType;
            }
            else if (strcmp(methodName, "IsAssignableFrom") == 0)
            {
                result = NI_System_Type_IsAssignableFrom;
            }
            else if (strcmp(methodName, "IsAssignableTo") == 0)
            {
                result = NI_System_Type_IsAssignableTo;
            }
            else if (strcmp(methodName, "op_Equality") == 0)
            {
                result = NI_System_Type_op_Equality;
            }
            else if (strcmp(methodName, "op_Inequality") == 0)
            {
                result = NI_System_Type_op_Inequality;
            }
            else if (strcmp(methodName, "GetTypeFromHandle") == 0)
            {
                result = NI_System_Type_GetTypeFromHandle;
            }
        }
        else if (strcmp(className, "String") == 0)
        {
            if (strcmp(methodName, "get_Chars") == 0)
            {
                result = NI_System_String_get_Chars;
            }
            else if (strcmp(methodName, "get_Length") == 0)
            {
                result = NI_System_String_get_Length;
            }
        }
        else if (strcmp(className, "Span`1") == 0)
        {
            if (strcmp(methodName, "get_Item") == 0)
            {
                result = NI_System_Span_get_Item;
            }
        }
        else if (strcmp(className, "ReadOnlySpan`1") == 0)
        {
            if (strcmp(methodName, "get_Item") == 0)
            {
                result = NI_System_ReadOnlySpan_get_Item;
            }
        }
    }
    else if (strcmp(namespaceName, "System.Threading") == 0)
    {
        if (strcmp(className, "Thread") == 0)
        {
            if (strcmp(methodName, "get_CurrentThread") == 0)
            {
                result = NI_System_Threading_Thread_get_CurrentThread;
            }
            else if (strcmp(methodName, "get_ManagedThreadId") == 0)
            {
                result = NI_System_Threading_Thread_get_ManagedThreadId;
            }
        }
#ifndef TARGET_ARM64
        // TODO-CQ: Implement for XArch (https://github.com/dotnet/runtime/issues/32239).
        else if (strcmp(className, "Interlocked") == 0)
        {
            if (strcmp(methodName, "And") == 0)
            {
                result = NI_System_Threading_Interlocked_And;
            }
            else if (strcmp(methodName, "Or") == 0)
            {
                result = NI_System_Threading_Interlocked_Or;
            }
        }
#endif
    }
#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
    else if (strcmp(namespaceName, "System.Buffers.Binary") == 0)
    {
        if ((strcmp(className, "BinaryPrimitives") == 0) && (strcmp(methodName, "ReverseEndianness") == 0))
        {
            result = NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness;
        }
    }
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)
    else if (strcmp(namespaceName, "System.Collections.Generic") == 0)
    {
        if ((strcmp(className, "EqualityComparer`1") == 0) && (strcmp(methodName, "get_Default") == 0))
        {
            result = NI_System_Collections_Generic_EqualityComparer_get_Default;
        }
        else if ((strcmp(className, "Comparer`1") == 0) && (strcmp(methodName, "get_Default") == 0))
        {
            result = NI_System_Collections_Generic_Comparer_get_Default;
        }
    }
    else if ((strcmp(namespaceName, "System.Numerics") == 0) && (strcmp(className, "BitOperations") == 0))
    {
        if (strcmp(methodName, "PopCount") == 0)
        {
            result = NI_System_Numerics_BitOperations_PopCount;
        }
    }
#ifdef FEATURE_HW_INTRINSICS
    else if (strcmp(namespaceName, "System.Numerics") == 0)
    {
        CORINFO_SIG_INFO sig;
        info.compCompHnd->getMethodSig(method, &sig);

        int sizeOfVectorT = getSIMDVectorRegisterByteLength();

        result = SimdAsHWIntrinsicInfo::lookupId(&sig, className, methodName, enclosingClassName, sizeOfVectorT);
    }
#endif // FEATURE_HW_INTRINSICS
    else if (strncmp(namespaceName, "System.Runtime.Intrinsics", 25) == 0)
    {
        // We go down this path even when FEATURE_HW_INTRINSICS isn't enabled
        // so we can specially handle IsSupported and recursive calls.

        // This is required to appropriately handle the intrinsics on platforms
        // which don't support them. On such a platform methods like Vector64.Create
        // will be seen as `Intrinsic` and `mustExpand` due to having a code path
        // which is recursive. When such a path is hit we expect it to be handled by
        // the importer and we fire an assert if it wasn't and in previous versions
        // of the JIT would fail fast. This was changed to throw a PNSE instead but
        // we still assert as most intrinsics should have been recognized/handled.

        // In order to avoid the assert, we specially handle the IsSupported checks
        // (to better allow dead-code optimizations) and we explicitly throw a PNSE
        // as we know that is the desired behavior for the HWIntrinsics when not
        // supported. For cases like Vector64.Create, this is fine because it will
        // be behind a relevant IsSupported check and will never be hit and the
        // software fallback will be executed instead.

        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef FEATURE_HW_INTRINSICS
        namespaceName += 25;
        const char* platformNamespaceName;

#if defined(TARGET_XARCH)
        platformNamespaceName = ".X86";
#elif defined(TARGET_ARM64)
        platformNamespaceName = ".Arm";
#else
#error Unsupported platform
#endif

        if ((namespaceName[0] == '\0') || (strcmp(namespaceName, platformNamespaceName) == 0))
        {
            CORINFO_SIG_INFO sig;
            info.compCompHnd->getMethodSig(method, &sig);

            result = HWIntrinsicInfo::lookupId(this, &sig, className, methodName, enclosingClassName);
        }
#endif // FEATURE_HW_INTRINSICS

        if (result == NI_Illegal)
        {
            if (strcmp(methodName, "get_IsSupported") == 0)
            {
                // This allows the relevant code paths to be dropped as dead code even
                // on platforms where FEATURE_HW_INTRINSICS is not supported.

                result = NI_IsSupported_False;
            }
            else if (gtIsRecursiveCall(method))
            {
                // For the framework itself, any recursive intrinsics will either be
                // only supported on a single platform or will be guarded by a relevant
                // IsSupported check so the throw PNSE will be valid or dropped.

                result = NI_Throw_PlatformNotSupportedException;
            }
        }
    }

    if (result == NI_Illegal)
    {
        JITDUMP("Not recognized\n");
    }
    else if (result == NI_IsSupported_False)
    {
        JITDUMP("Unsupported - return false");
    }
    else if (result == NI_Throw_PlatformNotSupportedException)
    {
        JITDUMP("Unsupported - throw PlatformNotSupportedException");
    }
    else
    {
        JITDUMP("Recognized\n");
    }
    return result;
}

//------------------------------------------------------------------------
// impUnsupportedNamedIntrinsic: Throws an exception for an unsupported named intrinsic
//
// Arguments:
//    helper     - JIT helper ID for the exception to be thrown
//    method     - method handle of the intrinsic function.
//    sig        - signature of the intrinsic call
//    mustExpand - true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    a gtNewMustThrowException if mustExpand is true; otherwise, nullptr
//
GenTree* Compiler::impUnsupportedNamedIntrinsic(unsigned              helper,
                                                CORINFO_METHOD_HANDLE method,
                                                CORINFO_SIG_INFO*     sig,
                                                bool                  mustExpand)
{
    // We've hit some error case and may need to return a node for the given error.
    //
    // When `mustExpand=false`, we are attempting to inline the intrinsic directly into another method. In this
    // scenario, we need to return `nullptr` so that a GT_CALL to the intrinsic is emitted instead. This is to
    // ensure that everything continues to behave correctly when optimizations are enabled (e.g. things like the
    // inliner may expect the node we return to have a certain signature, and the `MustThrowException` node won't
    // match that).
    //
    // When `mustExpand=true`, we are in a GT_CALL to the intrinsic and are attempting to JIT it. This will generally
    // be in response to an indirect call (e.g. done via reflection) or in response to an earlier attempt returning
    // `nullptr` (under `mustExpand=false`). In that scenario, we are safe to return the `MustThrowException` node.

    if (mustExpand)
    {
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            impPopStack();
        }

        return gtNewMustThrowException(helper, JITtype2varType(sig->retType), sig->retTypeClass);
    }
    else
    {
        return nullptr;
    }
}

/*****************************************************************************/

GenTree* Compiler::impArrayAccessIntrinsic(
    CORINFO_CLASS_HANDLE clsHnd, CORINFO_SIG_INFO* sig, int memberRef, bool readonlyCall, CorInfoIntrinsics intrinsicID)
{
    /* If we are generating SMALL_CODE, we don't want to use intrinsics for
       the following, as it generates fatter code.
    */

    if (compCodeOpt() == SMALL_CODE)
    {
        return nullptr;
    }

    /* These intrinsics generate fatter (but faster) code and are only
       done if we don't need SMALL_CODE */

    unsigned rank = (intrinsicID == CORINFO_INTRINSIC_Array_Set) ? (sig->numArgs - 1) : sig->numArgs;

    // The rank 1 case is special because it has to handle two array formats
    // we will simply not do that case
    if (rank > GT_ARR_MAX_RANK || rank <= 1)
    {
        return nullptr;
    }

    CORINFO_CLASS_HANDLE arrElemClsHnd = nullptr;
    var_types            elemType      = JITtype2varType(info.compCompHnd->getChildType(clsHnd, &arrElemClsHnd));

    // For the ref case, we will only be able to inline if the types match
    // (verifier checks for this, we don't care for the nonverified case and the
    // type is final (so we don't need to do the cast)
    if ((intrinsicID != CORINFO_INTRINSIC_Array_Get) && !readonlyCall && varTypeIsGC(elemType))
    {
        // Get the call site signature
        CORINFO_SIG_INFO LocalSig;
        eeGetCallSiteSig(memberRef, info.compScopeHnd, impTokenLookupContextHandle, &LocalSig);
        assert(LocalSig.hasThis());

        CORINFO_CLASS_HANDLE actualElemClsHnd;

        if (intrinsicID == CORINFO_INTRINSIC_Array_Set)
        {
            // Fetch the last argument, the one that indicates the type we are setting.
            CORINFO_ARG_LIST_HANDLE argType = LocalSig.args;
            for (unsigned r = 0; r < rank; r++)
            {
                argType = info.compCompHnd->getArgNext(argType);
            }

            typeInfo argInfo = verParseArgSigToTypeInfo(&LocalSig, argType);
            actualElemClsHnd = argInfo.GetClassHandle();
        }
        else
        {
            assert(intrinsicID == CORINFO_INTRINSIC_Array_Address);

            // Fetch the return type
            typeInfo retInfo = verMakeTypeInfo(LocalSig.retType, LocalSig.retTypeClass);
            assert(retInfo.IsByRef());
            actualElemClsHnd = retInfo.GetClassHandle();
        }

        // if it's not final, we can't do the optimization
        if (!(info.compCompHnd->getClassAttribs(actualElemClsHnd) & CORINFO_FLG_FINAL))
        {
            return nullptr;
        }
    }

    unsigned arrayElemSize;
    if (elemType == TYP_STRUCT)
    {
        assert(arrElemClsHnd);

        arrayElemSize = info.compCompHnd->getClassSize(arrElemClsHnd);
    }
    else
    {
        arrayElemSize = genTypeSize(elemType);
    }

    if ((unsigned char)arrayElemSize != arrayElemSize)
    {
        // arrayElemSize would be truncated as an unsigned char.
        // This means the array element is too large. Don't do the optimization.
        return nullptr;
    }

    GenTree* val = nullptr;

    if (intrinsicID == CORINFO_INTRINSIC_Array_Set)
    {
        // Assignment of a struct is more work, and there are more gets than sets.
        if (elemType == TYP_STRUCT)
        {
            return nullptr;
        }

        val = impPopStack().val;
        assert(genActualType(elemType) == genActualType(val->gtType) ||
               (elemType == TYP_FLOAT && val->gtType == TYP_DOUBLE) ||
               (elemType == TYP_INT && val->gtType == TYP_BYREF) ||
               (elemType == TYP_DOUBLE && val->gtType == TYP_FLOAT));
    }

    noway_assert((unsigned char)GT_ARR_MAX_RANK == GT_ARR_MAX_RANK);

    GenTree* inds[GT_ARR_MAX_RANK];
    for (unsigned k = rank; k > 0; k--)
    {
        inds[k - 1] = impPopStack().val;
    }

    GenTree* arr = impPopStack().val;
    assert(arr->gtType == TYP_REF);

    GenTree* arrElem =
        new (this, GT_ARR_ELEM) GenTreeArrElem(TYP_BYREF, arr, static_cast<unsigned char>(rank),
                                               static_cast<unsigned char>(arrayElemSize), elemType, &inds[0]);

    if (intrinsicID != CORINFO_INTRINSIC_Array_Address)
    {
        if (varTypeIsStruct(elemType))
        {
            arrElem = gtNewObjNode(sig->retTypeClass, arrElem);
        }
        else
        {
            arrElem = gtNewOperNode(GT_IND, elemType, arrElem);
        }
    }

    if (intrinsicID == CORINFO_INTRINSIC_Array_Set)
    {
        assert(val != nullptr);
        return gtNewAssignNode(arrElem, val);
    }
    else
    {
        return arrElem;
    }
}

bool Compiler::verMergeEntryStates(BasicBlock* block, bool* changed)
{
    unsigned i;

    // do some basic checks first
    if (block->bbStackDepthOnEntry() != verCurrentState.esStackDepth)
    {
        return false;
    }

    if (verCurrentState.esStackDepth > 0)
    {
        // merge stack types
        StackEntry* parentStack = block->bbStackOnEntry();
        StackEntry* childStack  = verCurrentState.esStack;

        for (i = 0; i < verCurrentState.esStackDepth; i++, parentStack++, childStack++)
        {
            if (tiMergeToCommonParent(&parentStack->seTypeInfo, &childStack->seTypeInfo, changed) == false)
            {
                return false;
            }
        }
    }

    // merge initialization status of this ptr

    if (verTrackObjCtorInitState)
    {
        // If we're tracking the CtorInitState, then it must not be unknown in the current state.
        assert(verCurrentState.thisInitialized != TIS_Bottom);

        // If the successor block's thisInit state is unknown, copy it from the current state.
        if (block->bbThisOnEntry() == TIS_Bottom)
        {
            *changed = true;
            verSetThisInit(block, verCurrentState.thisInitialized);
        }
        else if (verCurrentState.thisInitialized != block->bbThisOnEntry())
        {
            if (block->bbThisOnEntry() != TIS_Top)
            {
                *changed = true;
                verSetThisInit(block, TIS_Top);

                if (block->bbFlags & BBF_FAILED_VERIFICATION)
                {
                    // The block is bad. Control can flow through the block to any handler that catches the
                    // verification exception, but the importer ignores bad blocks and therefore won't model
                    // this flow in the normal way. To complete the merge into the bad block, the new state
                    // needs to be manually pushed to the handlers that may be reached after the verification
                    // exception occurs.
                    //
                    // Usually, the new state was already propagated to the relevant handlers while processing
                    // the predecessors of the bad block. The exception is when the bad block is at the start
                    // of a try region, meaning it is protected by additional handlers that do not protect its
                    // predecessors.
                    //
                    if (block->hasTryIndex() && ((block->bbFlags & BBF_TRY_BEG) != 0))
                    {
                        // Push TIS_Top to the handlers that protect the bad block. Note that this can cause
                        // recursive calls back into this code path (if successors of the current bad block are
                        // also bad blocks).
                        //
                        ThisInitState origTIS           = verCurrentState.thisInitialized;
                        verCurrentState.thisInitialized = TIS_Top;
                        impVerifyEHBlock(block, true);
                        verCurrentState.thisInitialized = origTIS;
                    }
                }
            }
        }
    }
    else
    {
        assert(verCurrentState.thisInitialized == TIS_Bottom && block->bbThisOnEntry() == TIS_Bottom);
    }

    return true;
}

/*****************************************************************************
 * 'logMsg' is true if a log message needs to be logged. false if the caller has
 *   already logged it (presumably in a more detailed fashion than done here)
 */

void Compiler::verConvertBBToThrowVerificationException(BasicBlock* block DEBUGARG(bool logMsg))
{
    block->bbJumpKind = BBJ_THROW;
    block->bbFlags |= BBF_FAILED_VERIFICATION;
    block->bbFlags &= ~BBF_IMPORTED;

    impCurStmtOffsSet(block->bbCodeOffs);

    // Clear the statement list as it exists so far; we're only going to have a verification exception.
    impStmtList = impLastStmt = nullptr;

#ifdef DEBUG
    if (logMsg)
    {
        JITLOG((LL_ERROR, "Verification failure: while compiling %s near IL offset %x..%xh \n", info.compFullName,
                block->bbCodeOffs, block->bbCodeOffsEnd));
        if (verbose)
        {
            printf("\n\nVerification failure: %s near IL %xh \n", info.compFullName, block->bbCodeOffs);
        }
    }

    if (JitConfig.DebugBreakOnVerificationFailure())
    {
        DebugBreak();
    }
#endif

    impBeginTreeList();

    // if the stack is non-empty evaluate all the side-effects
    if (verCurrentState.esStackDepth > 0)
    {
        impEvalSideEffects();
    }
    assert(verCurrentState.esStackDepth == 0);

    GenTree* op1 =
        gtNewHelperCallNode(CORINFO_HELP_VERIFICATION, TYP_VOID, gtNewCallArgs(gtNewIconNode(block->bbCodeOffs)));
    // verCurrentState.esStackDepth = 0;
    impAppendTree(op1, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

    // The inliner is not able to handle methods that require throw block, so
    // make sure this methods never gets inlined.
    info.compCompHnd->setMethodAttribs(info.compMethodHnd, CORINFO_FLG_BAD_INLINEE);
}

/*****************************************************************************
 *
 */
void Compiler::verHandleVerificationFailure(BasicBlock* block DEBUGARG(bool logMsg))
{
    verResetCurrentState(block, &verCurrentState);
    verConvertBBToThrowVerificationException(block DEBUGARG(logMsg));

#ifdef DEBUG
    impNoteLastILoffs(); // Remember at which BC offset the tree was finished
#endif                   // DEBUG
}

/******************************************************************************/
typeInfo Compiler::verMakeTypeInfo(CorInfoType ciType, CORINFO_CLASS_HANDLE clsHnd)
{
    assert(ciType < CORINFO_TYPE_COUNT);

    typeInfo tiResult;
    switch (ciType)
    {
        case CORINFO_TYPE_STRING:
        case CORINFO_TYPE_CLASS:
            tiResult = verMakeTypeInfo(clsHnd);
            if (!tiResult.IsType(TI_REF))
            { // type must be consistent with element type
                return typeInfo();
            }
            break;

#ifdef TARGET_64BIT
        case CORINFO_TYPE_NATIVEINT:
        case CORINFO_TYPE_NATIVEUINT:
            if (clsHnd)
            {
                // If we have more precise information, use it
                return verMakeTypeInfo(clsHnd);
            }
            else
            {
                return typeInfo::nativeInt();
            }
            break;
#endif // TARGET_64BIT

        case CORINFO_TYPE_VALUECLASS:
        case CORINFO_TYPE_REFANY:
            tiResult = verMakeTypeInfo(clsHnd);
            // type must be constant with element type;
            if (!tiResult.IsValueClass())
            {
                return typeInfo();
            }
            break;
        case CORINFO_TYPE_VAR:
            return verMakeTypeInfo(clsHnd);

        case CORINFO_TYPE_PTR: // for now, pointers are treated as an error
        case CORINFO_TYPE_VOID:
            return typeInfo();
            break;

        case CORINFO_TYPE_BYREF:
        {
            CORINFO_CLASS_HANDLE childClassHandle;
            CorInfoType          childType = info.compCompHnd->getChildType(clsHnd, &childClassHandle);
            return ByRef(verMakeTypeInfo(childType, childClassHandle));
        }
        break;

        default:
            if (clsHnd)
            { // If we have more precise information, use it
                return typeInfo(TI_STRUCT, clsHnd);
            }
            else
            {
                return typeInfo(JITtype2tiType(ciType));
            }
    }
    return tiResult;
}

/******************************************************************************/

typeInfo Compiler::verMakeTypeInfo(CORINFO_CLASS_HANDLE clsHnd, bool bashStructToRef /* = false */)
{
    if (clsHnd == nullptr)
    {
        return typeInfo();
    }

    // Byrefs should only occur in method and local signatures, which are accessed
    // using ICorClassInfo and ICorClassInfo.getChildType.
    // So findClass() and getClassAttribs() should not be called for byrefs

    if (JITtype2varType(info.compCompHnd->asCorInfoType(clsHnd)) == TYP_BYREF)
    {
        assert(!"Did findClass() return a Byref?");
        return typeInfo();
    }

    unsigned attribs = info.compCompHnd->getClassAttribs(clsHnd);

    if (attribs & CORINFO_FLG_VALUECLASS)
    {
        CorInfoType t = info.compCompHnd->getTypeForPrimitiveValueClass(clsHnd);

        // Meta-data validation should ensure that CORINF_TYPE_BYREF should
        // not occur here, so we may want to change this to an assert instead.
        if (t == CORINFO_TYPE_VOID || t == CORINFO_TYPE_BYREF || t == CORINFO_TYPE_PTR)
        {
            return typeInfo();
        }

#ifdef TARGET_64BIT
        if (t == CORINFO_TYPE_NATIVEINT || t == CORINFO_TYPE_NATIVEUINT)
        {
            return typeInfo::nativeInt();
        }
#endif // TARGET_64BIT

        if (t != CORINFO_TYPE_UNDEF)
        {
            return (typeInfo(JITtype2tiType(t)));
        }
        else if (bashStructToRef)
        {
            return (typeInfo(TI_REF, clsHnd));
        }
        else
        {
            return (typeInfo(TI_STRUCT, clsHnd));
        }
    }
    else if (attribs & CORINFO_FLG_GENERIC_TYPE_VARIABLE)
    {
        // See comment in _typeInfo.h for why we do it this way.
        return (typeInfo(TI_REF, clsHnd, true));
    }
    else
    {
        return (typeInfo(TI_REF, clsHnd));
    }
}

/******************************************************************************/
bool Compiler::verIsSDArray(const typeInfo& ti)
{
    if (ti.IsNullObjRef())
    { // nulls are SD arrays
        return true;
    }

    if (!ti.IsType(TI_REF))
    {
        return false;
    }

    if (!info.compCompHnd->isSDArray(ti.GetClassHandleForObjRef()))
    {
        return false;
    }
    return true;
}

/******************************************************************************/
/* Given 'arrayObjectType' which is an array type, fetch the element type. */
/* Returns an error type if anything goes wrong */

typeInfo Compiler::verGetArrayElemType(const typeInfo& arrayObjectType)
{
    assert(!arrayObjectType.IsNullObjRef()); // you need to check for null explicitly since that is a success case

    if (!verIsSDArray(arrayObjectType))
    {
        return typeInfo();
    }

    CORINFO_CLASS_HANDLE childClassHandle = nullptr;
    CorInfoType ciType = info.compCompHnd->getChildType(arrayObjectType.GetClassHandleForObjRef(), &childClassHandle);

    return verMakeTypeInfo(ciType, childClassHandle);
}

/*****************************************************************************
 */
typeInfo Compiler::verParseArgSigToTypeInfo(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE args)
{
    CORINFO_CLASS_HANDLE classHandle;
    CorInfoType          ciType = strip(info.compCompHnd->getArgType(sig, args, &classHandle));

    var_types type = JITtype2varType(ciType);
    if (varTypeIsGC(type))
    {
        // For efficiency, getArgType only returns something in classHandle for
        // value types.  For other types that have addition type info, you
        // have to call back explicitly
        classHandle = info.compCompHnd->getArgClass(sig, args);
        if (!classHandle)
        {
            NO_WAY("Could not figure out Class specified in argument or local signature");
        }
    }

    return verMakeTypeInfo(ciType, classHandle);
}

bool Compiler::verIsByRefLike(const typeInfo& ti)
{
    if (ti.IsByRef())
    {
        return true;
    }
    if (!ti.IsType(TI_STRUCT))
    {
        return false;
    }
    return info.compCompHnd->getClassAttribs(ti.GetClassHandleForValueClass()) & CORINFO_FLG_CONTAINS_STACK_PTR;
}

bool Compiler::verIsSafeToReturnByRef(const typeInfo& ti)
{
    if (ti.IsPermanentHomeByRef())
    {
        return true;
    }
    else
    {
        return false;
    }
}

bool Compiler::verIsBoxable(const typeInfo& ti)
{
    return (ti.IsPrimitiveType() || ti.IsObjRef() // includes boxed generic type variables
            || ti.IsUnboxedGenericTypeVar() ||
            (ti.IsType(TI_STRUCT) &&
             // exclude byreflike structs
             !(info.compCompHnd->getClassAttribs(ti.GetClassHandleForValueClass()) & CORINFO_FLG_CONTAINS_STACK_PTR)));
}

// Is it a boxed value type?
bool Compiler::verIsBoxedValueType(const typeInfo& ti)
{
    if (ti.GetType() == TI_REF)
    {
        CORINFO_CLASS_HANDLE clsHnd = ti.GetClassHandleForObjRef();
        return !!eeIsValueClass(clsHnd);
    }
    else
    {
        return false;
    }
}

/*****************************************************************************
 *
 *  Check if a TailCall is legal.
 */

bool Compiler::verCheckTailCallConstraint(
    OPCODE                  opcode,
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, // Is this a "constrained." call on a type parameter?
    bool                    speculative                // If true, won't throw if verificatoin fails. Instead it will
                                                       // return false to the caller.
                                                       // If false, it will throw.
    )
{
    DWORD            mflags;
    CORINFO_SIG_INFO sig;
    unsigned int     popCount = 0; // we can't pop the stack since impImportCall needs it, so
                                   // this counter is used to keep track of how many items have been
                                   // virtually popped

    CORINFO_METHOD_HANDLE methodHnd       = nullptr;
    CORINFO_CLASS_HANDLE  methodClassHnd  = nullptr;
    unsigned              methodClassFlgs = 0;

    assert(impOpcodeIsCallOpcode(opcode));

    if (compIsForInlining())
    {
        return false;
    }

    // for calli, VerifyOrReturn that this is not a virtual method
    if (opcode == CEE_CALLI)
    {
        /* Get the call sig */
        eeGetSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, &sig);

        // We don't know the target method, so we have to infer the flags, or
        // assume the worst-case.
        mflags = (sig.callConv & CORINFO_CALLCONV_HASTHIS) ? 0 : CORINFO_FLG_STATIC;
    }
    else
    {
        methodHnd = pResolvedToken->hMethod;

        mflags = info.compCompHnd->getMethodAttribs(methodHnd);

        // When verifying generic code we pair the method handle with its
        // owning class to get the exact method signature.
        methodClassHnd = pResolvedToken->hClass;
        assert(methodClassHnd);

        eeGetMethodSig(methodHnd, &sig, methodClassHnd);

        // opcode specific check
        methodClassFlgs = info.compCompHnd->getClassAttribs(methodClassHnd);
    }

    // We must have got the methodClassHnd if opcode is not CEE_CALLI
    assert((methodHnd != nullptr && methodClassHnd != nullptr) || opcode == CEE_CALLI);

    if ((sig.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, &sig);
    }

    // check compatibility of the arguments
    unsigned int argCount;
    argCount = sig.numArgs;
    CORINFO_ARG_LIST_HANDLE args;
    args = sig.args;
    while (argCount--)
    {
        typeInfo tiDeclared = verParseArgSigToTypeInfo(&sig, args).NormaliseForStack();

        // check that the argument is not a byref for tailcalls
        VerifyOrReturnSpeculative(!verIsByRefLike(tiDeclared), "tailcall on byrefs", speculative);

        // For unsafe code, we might have parameters containing pointer to the stack location.
        // Disallow the tailcall for this kind.
        CORINFO_CLASS_HANDLE classHandle;
        CorInfoType          ciType = strip(info.compCompHnd->getArgType(&sig, args, &classHandle));
        VerifyOrReturnSpeculative(ciType != CORINFO_TYPE_PTR, "tailcall on CORINFO_TYPE_PTR", speculative);

        args = info.compCompHnd->getArgNext(args);
    }

    // update popCount
    popCount += sig.numArgs;

    // check for 'this' which is on non-static methods, not called via NEWOBJ
    if (!(mflags & CORINFO_FLG_STATIC))
    {
        // Always update the popCount.
        // This is crucial for the stack calculation to be correct.
        typeInfo tiThis = impStackTop(popCount).seTypeInfo;
        popCount++;

        if (opcode == CEE_CALLI)
        {
            // For CALLI, we don't know the methodClassHnd. Therefore, let's check the "this" object
            // on the stack.
            if (tiThis.IsValueClass())
            {
                tiThis.MakeByRef();
            }
            VerifyOrReturnSpeculative(!verIsByRefLike(tiThis), "byref in tailcall", speculative);
        }
        else
        {
            // Check type compatibility of the this argument
            typeInfo tiDeclaredThis = verMakeTypeInfo(methodClassHnd);
            if (tiDeclaredThis.IsValueClass())
            {
                tiDeclaredThis.MakeByRef();
            }

            VerifyOrReturnSpeculative(!verIsByRefLike(tiDeclaredThis), "byref in tailcall", speculative);
        }
    }

    // Tail calls on constrained calls should be illegal too:
    // when instantiated at a value type, a constrained call may pass the address of a stack allocated value
    VerifyOrReturnSpeculative(!pConstrainedResolvedToken, "byref in constrained tailcall", speculative);

    // Get the exact view of the signature for an array method
    if (sig.retType != CORINFO_TYPE_VOID)
    {
        if (methodClassFlgs & CORINFO_FLG_ARRAY)
        {
            assert(opcode != CEE_CALLI);
            eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, &sig);
        }
    }

    typeInfo tiCalleeRetType = verMakeTypeInfo(sig.retType, sig.retTypeClass);
    typeInfo tiCallerRetType =
        verMakeTypeInfo(info.compMethodInfo->args.retType, info.compMethodInfo->args.retTypeClass);

    // void return type gets morphed into the error type, so we have to treat them specially here
    if (sig.retType == CORINFO_TYPE_VOID)
    {
        VerifyOrReturnSpeculative(info.compMethodInfo->args.retType == CORINFO_TYPE_VOID, "tailcall return mismatch",
                                  speculative);
    }
    else
    {
        VerifyOrReturnSpeculative(tiCompatibleWith(NormaliseForStack(tiCalleeRetType),
                                                   NormaliseForStack(tiCallerRetType), true),
                                  "tailcall return mismatch", speculative);
    }

    // for tailcall, stack must be empty
    VerifyOrReturnSpeculative(verCurrentState.esStackDepth == popCount, "stack non-empty on tailcall", speculative);

    return true; // Yes, tailcall is legal
}

/*****************************************************************************
 *
 *  Checks the IL verification rules for the call
 */

void Compiler::verVerifyCall(OPCODE                  opcode,
                             CORINFO_RESOLVED_TOKEN* pResolvedToken,
                             CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                             bool                    tailCall,
                             bool                    readonlyCall,
                             const BYTE*             delegateCreateStart,
                             const BYTE*             codeAddr,
                             CORINFO_CALL_INFO* callInfo DEBUGARG(const char* methodName))
{
    DWORD             mflags;
    CORINFO_SIG_INFO* sig      = nullptr;
    unsigned int      popCount = 0; // we can't pop the stack since impImportCall needs it, so
                                    // this counter is used to keep track of how many items have been
                                    // virtually popped

    // for calli, VerifyOrReturn that this is not a virtual method
    if (opcode == CEE_CALLI)
    {
        Verify(false, "Calli not verifiable");
        return;
    }

    //<NICE> It would be nice to cache the rest of it, but eeFindMethod is the big ticket item.
    mflags = callInfo->verMethodFlags;

    sig = &callInfo->verSig;

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, sig);
    }

    // opcode specific check
    unsigned methodClassFlgs = callInfo->classFlags;
    switch (opcode)
    {
        case CEE_CALLVIRT:
            // cannot do callvirt on valuetypes
            VerifyOrReturn(!(methodClassFlgs & CORINFO_FLG_VALUECLASS), "callVirt on value class");
            VerifyOrReturn(sig->hasThis(), "CallVirt on static method");
            break;

        case CEE_NEWOBJ:
        {
            assert(!tailCall); // Importer should not allow this
            VerifyOrReturn((mflags & CORINFO_FLG_CONSTRUCTOR) && !(mflags & CORINFO_FLG_STATIC),
                           "newobj must be on instance");

            if (methodClassFlgs & CORINFO_FLG_DELEGATE)
            {
                VerifyOrReturn(sig->numArgs == 2, "wrong number args to delegate ctor");
                typeInfo tiDeclaredObj = verParseArgSigToTypeInfo(sig, sig->args).NormaliseForStack();
                typeInfo tiDeclaredFtn =
                    verParseArgSigToTypeInfo(sig, info.compCompHnd->getArgNext(sig->args)).NormaliseForStack();
                VerifyOrReturn(tiDeclaredFtn.IsNativeIntType(), "ftn arg needs to be a native int type");

                assert(popCount == 0);
                typeInfo tiActualObj = impStackTop(1).seTypeInfo;
                typeInfo tiActualFtn = impStackTop(0).seTypeInfo;

                VerifyOrReturn(tiActualFtn.IsMethod(), "delegate needs method as first arg");
                VerifyOrReturn(tiCompatibleWith(tiActualObj, tiDeclaredObj, true), "delegate object type mismatch");
                VerifyOrReturn(tiActualObj.IsNullObjRef() || tiActualObj.IsType(TI_REF),
                               "delegate object type mismatch");

                CORINFO_CLASS_HANDLE objTypeHandle =
                    tiActualObj.IsNullObjRef() ? nullptr : tiActualObj.GetClassHandleForObjRef();

                // the method signature must be compatible with the delegate's invoke method

                // check that for virtual functions, the type of the object used to get the
                // ftn ptr is the same as the type of the object passed to the delegate ctor.
                // since this is a bit of work to determine in general, we pattern match stylized
                // code sequences

                // the delegate creation code check, which used to be done later, is now done here
                // so we can read delegateMethodRef directly from
                // from the preceding LDFTN or CEE_LDVIRTFN instruction sequence;
                // we then use it in our call to isCompatibleDelegate().

                mdMemberRef delegateMethodRef = mdMemberRefNil;
                VerifyOrReturn(verCheckDelegateCreation(delegateCreateStart, codeAddr, delegateMethodRef),
                               "must create delegates with certain IL");

                CORINFO_RESOLVED_TOKEN delegateResolvedToken;
                delegateResolvedToken.tokenContext = impTokenLookupContextHandle;
                delegateResolvedToken.tokenScope   = info.compScopeHnd;
                delegateResolvedToken.token        = delegateMethodRef;
                delegateResolvedToken.tokenType    = CORINFO_TOKENKIND_Method;
                info.compCompHnd->resolveToken(&delegateResolvedToken);

                CORINFO_CALL_INFO delegateCallInfo;
                eeGetCallInfo(&delegateResolvedToken, nullptr /* constraint typeRef */,
                              addVerifyFlag(CORINFO_CALLINFO_SECURITYCHECKS), &delegateCallInfo);

                bool isOpenDelegate = false;
                VerifyOrReturn(info.compCompHnd->isCompatibleDelegate(objTypeHandle, delegateResolvedToken.hClass,
                                                                      tiActualFtn.GetMethod(), pResolvedToken->hClass,
                                                                      &isOpenDelegate),
                               "function incompatible with delegate");

                // check the constraints on the target method
                VerifyOrReturn(info.compCompHnd->satisfiesClassConstraints(delegateResolvedToken.hClass),
                               "delegate target has unsatisfied class constraints");
                VerifyOrReturn(info.compCompHnd->satisfiesMethodConstraints(delegateResolvedToken.hClass,
                                                                            tiActualFtn.GetMethod()),
                               "delegate target has unsatisfied method constraints");

                // See ECMA spec section 1.8.1.5.2 (Delegating via instance dispatch)
                // for additional verification rules for delegates
                CORINFO_METHOD_HANDLE actualMethodHandle  = tiActualFtn.GetMethod();
                DWORD                 actualMethodAttribs = info.compCompHnd->getMethodAttribs(actualMethodHandle);
                if (impIsLDFTN_TOKEN(delegateCreateStart, codeAddr))
                {

                    if ((actualMethodAttribs & CORINFO_FLG_VIRTUAL) && ((actualMethodAttribs & CORINFO_FLG_FINAL) == 0))
                    {
                        VerifyOrReturn((tiActualObj.IsThisPtr() && lvaIsOriginalThisReadOnly()) ||
                                           verIsBoxedValueType(tiActualObj),
                                       "The 'this' parameter to the call must be either the calling method's "
                                       "'this' parameter or "
                                       "a boxed value type.");
                    }
                }

                if (actualMethodAttribs & CORINFO_FLG_PROTECTED)
                {
                    bool targetIsStatic = actualMethodAttribs & CORINFO_FLG_STATIC;

                    Verify(targetIsStatic || !isOpenDelegate,
                           "Unverifiable creation of an open instance delegate for a protected member.");

                    CORINFO_CLASS_HANDLE instanceClassHnd = (tiActualObj.IsNullObjRef() || targetIsStatic)
                                                                ? info.compClassHnd
                                                                : tiActualObj.GetClassHandleForObjRef();

                    // In the case of protected methods, it is a requirement that the 'this'
                    // pointer be a subclass of the current context.  Perform this check.
                    Verify(info.compCompHnd->canAccessFamily(info.compMethodHnd, instanceClassHnd),
                           "Accessing protected method through wrong type.");
                }
                goto DONE_ARGS;
            }
        }
            // fall thru to default checks
            FALLTHROUGH;
        default:
            VerifyOrReturn(!(mflags & CORINFO_FLG_ABSTRACT), "method abstract");
    }
    VerifyOrReturn(!((mflags & CORINFO_FLG_CONSTRUCTOR) && (methodClassFlgs & CORINFO_FLG_DELEGATE)),
                   "can only newobj a delegate constructor");

    // check compatibility of the arguments
    unsigned int argCount;
    argCount = sig->numArgs;
    CORINFO_ARG_LIST_HANDLE args;
    args = sig->args;
    while (argCount--)
    {
        typeInfo tiActual = impStackTop(popCount + argCount).seTypeInfo;

        typeInfo tiDeclared = verParseArgSigToTypeInfo(sig, args).NormaliseForStack();
        VerifyOrReturn(tiCompatibleWith(tiActual, tiDeclared, true), "type mismatch");

        args = info.compCompHnd->getArgNext(args);
    }

DONE_ARGS:

    // update popCount
    popCount += sig->numArgs;

    // check for 'this' which are is non-static methods, not called via NEWOBJ
    CORINFO_CLASS_HANDLE instanceClassHnd = info.compClassHnd;
    if (!(mflags & CORINFO_FLG_STATIC) && (opcode != CEE_NEWOBJ))
    {
        typeInfo tiThis = impStackTop(popCount).seTypeInfo;
        popCount++;

        // If it is null, we assume we can access it (since it will AV shortly)
        // If it is anything but a reference class, there is no hierarchy, so
        // again, we don't need the precise instance class to compute 'protected' access
        if (tiThis.IsType(TI_REF))
        {
            instanceClassHnd = tiThis.GetClassHandleForObjRef();
        }

        // Check type compatibility of the this argument
        typeInfo tiDeclaredThis = verMakeTypeInfo(pResolvedToken->hClass);
        if (tiDeclaredThis.IsValueClass())
        {
            tiDeclaredThis.MakeByRef();
        }

        // If this is a call to the base class .ctor, set thisPtr Init for
        // this block.
        if (mflags & CORINFO_FLG_CONSTRUCTOR)
        {
            if (verTrackObjCtorInitState && tiThis.IsThisPtr() &&
                verIsCallToInitThisPtr(info.compClassHnd, pResolvedToken->hClass))
            {
                assert(verCurrentState.thisInitialized !=
                       TIS_Bottom); // This should never be the case just from the logic of the verifier.
                VerifyOrReturn(verCurrentState.thisInitialized == TIS_Uninit,
                               "Call to base class constructor when 'this' is possibly initialized");
                // Otherwise, 'this' is now initialized.
                verCurrentState.thisInitialized = TIS_Init;
                tiThis.SetInitialisedObjRef();
            }
            else
            {
                // We allow direct calls to value type constructors
                // NB: we have to check that the contents of tiThis is a value type, otherwise we could use a
                // constrained callvirt to illegally re-enter a .ctor on a value of reference type.
                VerifyOrReturn(tiThis.IsByRef() && DereferenceByRef(tiThis).IsValueClass(),
                               "Bad call to a constructor");
            }
        }

        if (pConstrainedResolvedToken != nullptr)
        {
            VerifyOrReturn(tiThis.IsByRef(), "non-byref this type in constrained call");

            typeInfo tiConstraint = verMakeTypeInfo(pConstrainedResolvedToken->hClass);

            // We just dereference this and test for equality
            tiThis.DereferenceByRef();
            VerifyOrReturn(typeInfo::AreEquivalent(tiThis, tiConstraint),
                           "this type mismatch with constrained type operand");

            // Now pretend the this type is the boxed constrained type, for the sake of subsequent checks
            tiThis = typeInfo(TI_REF, pConstrainedResolvedToken->hClass);
        }

        // To support direct calls on readonly byrefs, just pretend tiDeclaredThis is readonly too
        if (tiDeclaredThis.IsByRef() && tiThis.IsReadonlyByRef())
        {
            tiDeclaredThis.SetIsReadonlyByRef();
        }

        VerifyOrReturn(tiCompatibleWith(tiThis, tiDeclaredThis, true), "this type mismatch");

        if (tiThis.IsByRef())
        {
            // Find the actual type where the method exists (as opposed to what is declared
            // in the metadata). This is to prevent passing a byref as the "this" argument
            // while calling methods like System.ValueType.GetHashCode() which expect boxed objects.

            CORINFO_CLASS_HANDLE actualClassHnd = info.compCompHnd->getMethodClass(pResolvedToken->hMethod);
            VerifyOrReturn(eeIsValueClass(actualClassHnd),
                           "Call to base type of valuetype (which is never a valuetype)");
        }

        // Rules for non-virtual call to a non-final virtual method:

        // Define:
        // The "this" pointer is considered to be "possibly written" if
        //   1. Its address have been taken (LDARGA 0) anywhere in the method.
        //   (or)
        //   2. It has been stored to (STARG.0) anywhere in the method.

        // A non-virtual call to a non-final virtual method is only allowed if
        //   1. The this pointer passed to the callee is an instance of a boxed value type.
        //   (or)
        //   2. The this pointer passed to the callee is the current method's this pointer.
        //      (and) The current method's this pointer is not "possibly written".

        // Thus the rule is that if you assign to this ANYWHERE you can't make "base" calls to
        // virtual methods.  (Luckily this does affect .ctors, since they are not virtual).
        // This is stronger that is strictly needed, but implementing a laxer rule is significantly
        // hard and more error prone.

        if (opcode == CEE_CALL && (mflags & CORINFO_FLG_VIRTUAL) && ((mflags & CORINFO_FLG_FINAL) == 0))
        {
            VerifyOrReturn((tiThis.IsThisPtr() && lvaIsOriginalThisReadOnly()) || verIsBoxedValueType(tiThis),
                           "The 'this' parameter to the call must be either the calling method's 'this' parameter or "
                           "a boxed value type.");
        }
    }

    // check any constraints on the callee's class and type parameters
    VerifyOrReturn(info.compCompHnd->satisfiesClassConstraints(pResolvedToken->hClass),
                   "method has unsatisfied class constraints");
    VerifyOrReturn(info.compCompHnd->satisfiesMethodConstraints(pResolvedToken->hClass, pResolvedToken->hMethod),
                   "method has unsatisfied method constraints");

    if (mflags & CORINFO_FLG_PROTECTED)
    {
        VerifyOrReturn(info.compCompHnd->canAccessFamily(info.compMethodHnd, instanceClassHnd),
                       "Can't access protected method");
    }

    // Get the exact view of the signature for an array method
    if (sig->retType != CORINFO_TYPE_VOID)
    {
        eeGetMethodSig(pResolvedToken->hMethod, sig, pResolvedToken->hClass);
    }

    // "readonly." prefixed calls only allowed for the Address operation on arrays.
    // The methods supported by array types are under the control of the EE
    // so we can trust that only the Address operation returns a byref.
    if (readonlyCall)
    {
        typeInfo tiCalleeRetType = verMakeTypeInfo(sig->retType, sig->retTypeClass);
        VerifyOrReturn((methodClassFlgs & CORINFO_FLG_ARRAY) && tiCalleeRetType.IsByRef(),
                       "unexpected use of readonly prefix");
    }

    // Verify the tailcall
    if (tailCall)
    {
        verCheckTailCallConstraint(opcode, pResolvedToken, pConstrainedResolvedToken, false);
    }
}

/*****************************************************************************
 *  Checks that a delegate creation is done using the following pattern:
 *     dup
 *     ldvirtftn targetMemberRef
 *  OR
 *     ldftn targetMemberRef
 *
 * 'delegateCreateStart' points at the last dup or ldftn in this basic block (null if
 *  not in this basic block)
 *
 *  targetMemberRef is read from the code sequence.
 *  targetMemberRef is validated iff verificationNeeded.
 */

bool Compiler::verCheckDelegateCreation(const BYTE*  delegateCreateStart,
                                        const BYTE*  codeAddr,
                                        mdMemberRef& targetMemberRef)
{
    if (impIsLDFTN_TOKEN(delegateCreateStart, codeAddr))
    {
        targetMemberRef = getU4LittleEndian(&delegateCreateStart[2]);
        return true;
    }
    else if (impIsDUP_LDVIRTFTN_TOKEN(delegateCreateStart, codeAddr))
    {
        targetMemberRef = getU4LittleEndian(&delegateCreateStart[3]);
        return true;
    }

    return false;
}

typeInfo Compiler::verVerifySTIND(const typeInfo& tiTo, const typeInfo& value, const typeInfo& instrType)
{
    Verify(!tiTo.IsReadonlyByRef(), "write to readonly byref");
    typeInfo ptrVal     = verVerifyLDIND(tiTo, instrType);
    typeInfo normPtrVal = typeInfo(ptrVal).NormaliseForStack();
    if (!tiCompatibleWith(value, normPtrVal, true))
    {
        Verify(tiCompatibleWith(value, normPtrVal, true), "type mismatch");
        compUnsafeCastUsed = true;
    }
    return ptrVal;
}

typeInfo Compiler::verVerifyLDIND(const typeInfo& ptr, const typeInfo& instrType)
{
    assert(!instrType.IsStruct());

    typeInfo ptrVal;
    if (ptr.IsByRef())
    {
        ptrVal = DereferenceByRef(ptr);
        if (instrType.IsObjRef() && !ptrVal.IsObjRef())
        {
            Verify(false, "bad pointer");
            compUnsafeCastUsed = true;
        }
        else if (!instrType.IsObjRef() && !typeInfo::AreEquivalent(instrType, ptrVal))
        {
            Verify(false, "pointer not consistent with instr");
            compUnsafeCastUsed = true;
        }
    }
    else
    {
        Verify(false, "pointer not byref");
        compUnsafeCastUsed = true;
    }

    return ptrVal;
}

// Verify that the field is used properly.  'tiThis' is NULL for statics,
// 'fieldFlags' is the fields attributes, and mutator is true if it is a
// ld*flda or a st*fld.
// 'enclosingClass' is given if we are accessing a field in some specific type.

void Compiler::verVerifyField(CORINFO_RESOLVED_TOKEN*   pResolvedToken,
                              const CORINFO_FIELD_INFO& fieldInfo,
                              const typeInfo*           tiThis,
                              bool                      mutator,
                              bool                      allowPlainStructAsThis)
{
    CORINFO_CLASS_HANDLE enclosingClass = pResolvedToken->hClass;
    unsigned             fieldFlags     = fieldInfo.fieldFlags;
    CORINFO_CLASS_HANDLE instanceClass =
        info.compClassHnd; // for statics, we imagine the instance is the current class.

    bool isStaticField = ((fieldFlags & CORINFO_FLG_FIELD_STATIC) != 0);
    if (mutator)
    {
        Verify(!(fieldFlags & CORINFO_FLG_FIELD_UNMANAGED), "mutating an RVA bases static");
        if ((fieldFlags & CORINFO_FLG_FIELD_FINAL))
        {
            Verify((info.compFlags & CORINFO_FLG_CONSTRUCTOR) && enclosingClass == info.compClassHnd &&
                       info.compIsStatic == isStaticField,
                   "bad use of initonly field (set or address taken)");
        }
    }

    if (tiThis == nullptr)
    {
        Verify(isStaticField, "used static opcode with non-static field");
    }
    else
    {
        typeInfo tThis = *tiThis;

        if (allowPlainStructAsThis && tThis.IsValueClass())
        {
            tThis.MakeByRef();
        }

        // If it is null, we assume we can access it (since it will AV shortly)
        // If it is anything but a refernce class, there is no hierarchy, so
        // again, we don't need the precise instance class to compute 'protected' access
        if (tiThis->IsType(TI_REF))
        {
            instanceClass = tiThis->GetClassHandleForObjRef();
        }

        // Note that even if the field is static, we require that the this pointer
        // satisfy the same constraints as a non-static field  This happens to
        // be simpler and seems reasonable
        typeInfo tiDeclaredThis = verMakeTypeInfo(enclosingClass);
        if (tiDeclaredThis.IsValueClass())
        {
            tiDeclaredThis.MakeByRef();

            // we allow read-only tThis, on any field access (even stores!), because if the
            // class implementor wants to prohibit stores he should make the field private.
            // we do this by setting the read-only bit on the type we compare tThis to.
            tiDeclaredThis.SetIsReadonlyByRef();
        }
        else if (verTrackObjCtorInitState && tThis.IsThisPtr())
        {
            // Any field access is legal on "uninitialized" this pointers.
            // The easiest way to implement this is to simply set the
            // initialized bit for the duration of the type check on the
            // field access only.  It does not change the state of the "this"
            // for the function as a whole. Note that the "tThis" is a copy
            // of the original "this" type (*tiThis) passed in.
            tThis.SetInitialisedObjRef();
        }

        Verify(tiCompatibleWith(tThis, tiDeclaredThis, true), "this type mismatch");
    }

    // Presently the JIT does not check that we don't store or take the address of init-only fields
    // since we cannot guarantee their immutability and it is not a security issue.

    // check any constraints on the fields's class --- accessing the field might cause a class constructor to run.
    VerifyOrReturn(info.compCompHnd->satisfiesClassConstraints(enclosingClass),
                   "field has unsatisfied class constraints");
    if (fieldFlags & CORINFO_FLG_FIELD_PROTECTED)
    {
        Verify(info.compCompHnd->canAccessFamily(info.compMethodHnd, instanceClass),
               "Accessing protected method through wrong type.");
    }
}

void Compiler::verVerifyCond(const typeInfo& tiOp1, const typeInfo& tiOp2, unsigned opcode)
{
    if (tiOp1.IsNumberType())
    {
#ifdef TARGET_64BIT
        Verify(tiCompatibleWith(tiOp1, tiOp2, true), "Cond type mismatch");
#else  // TARGET_64BIT
        // [10/17/2013] Consider changing this: to put on my verification lawyer hat,
        // this is non-conforming to the ECMA Spec: types don't have to be equivalent,
        // but compatible, since we can coalesce native int with int32 (see section III.1.5).
        Verify(typeInfo::AreEquivalent(tiOp1, tiOp2), "Cond type mismatch");
#endif // !TARGET_64BIT
    }
    else if (tiOp1.IsObjRef())
    {
        switch (opcode)
        {
            case CEE_BEQ_S:
            case CEE_BEQ:
            case CEE_BNE_UN_S:
            case CEE_BNE_UN:
            case CEE_CEQ:
            case CEE_CGT_UN:
                break;
            default:
                Verify(false, "Cond not allowed on object types");
        }
        Verify(tiOp2.IsObjRef(), "Cond type mismatch");
    }
    else if (tiOp1.IsByRef())
    {
        Verify(tiOp2.IsByRef(), "Cond type mismatch");
    }
    else
    {
        Verify(tiOp1.IsMethod() && tiOp2.IsMethod(), "Cond type mismatch");
    }
}

void Compiler::verVerifyThisPtrInitialised()
{
    if (verTrackObjCtorInitState)
    {
        Verify(verCurrentState.thisInitialized == TIS_Init, "this ptr is not initialized");
    }
}

bool Compiler::verIsCallToInitThisPtr(CORINFO_CLASS_HANDLE context, CORINFO_CLASS_HANDLE target)
{
    // Either target == context, in this case calling an alternate .ctor
    // Or target is the immediate parent of context

    return ((target == context) || (target == info.compCompHnd->getParentType(context)));
}

GenTree* Compiler::impImportLdvirtftn(GenTree*                thisPtr,
                                      CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                      CORINFO_CALL_INFO*      pCallInfo)
{
    if ((pCallInfo->methodFlags & CORINFO_FLG_EnC) && !(pCallInfo->classFlags & CORINFO_FLG_INTERFACE))
    {
        NO_WAY("Virtual call to a function added via EnC is not supported");
    }

    // CoreRT generic virtual method
    if ((pCallInfo->sig.sigInst.methInstCount != 0) && IsTargetAbi(CORINFO_CORERT_ABI))
    {
        GenTree* runtimeMethodHandle =
            impLookupToTree(pResolvedToken, &pCallInfo->codePointerLookup, GTF_ICON_METHOD_HDL, pCallInfo->hMethod);
        return gtNewHelperCallNode(CORINFO_HELP_GVMLOOKUP_FOR_SLOT, TYP_I_IMPL,
                                   gtNewCallArgs(thisPtr, runtimeMethodHandle));
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        if (!pCallInfo->exactContextNeedsRuntimeLookup)
        {
            GenTreeCall* call =
                gtNewHelperCallNode(CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR, TYP_I_IMPL, gtNewCallArgs(thisPtr));

            call->setEntryPoint(pCallInfo->codePointerLookup.constLookup);

            return call;
        }

        // We need a runtime lookup. CoreRT has a ReadyToRun helper for that too.
        if (IsTargetAbi(CORINFO_CORERT_ABI))
        {
            GenTree* ctxTree = getRuntimeContextTree(pCallInfo->codePointerLookup.lookupKind.runtimeLookupKind);

            return impReadyToRunHelperToTree(pResolvedToken, CORINFO_HELP_READYTORUN_GENERIC_HANDLE, TYP_I_IMPL,
                                             gtNewCallArgs(ctxTree), &pCallInfo->codePointerLookup.lookupKind);
        }
    }
#endif

    // Get the exact descriptor for the static callsite
    GenTree* exactTypeDesc = impParentClassTokenToHandle(pResolvedToken);
    if (exactTypeDesc == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    GenTree* exactMethodDesc = impTokenToHandle(pResolvedToken);
    if (exactMethodDesc == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    GenTreeCall::Use* helpArgs = gtNewCallArgs(exactMethodDesc);

    helpArgs = gtPrependNewCallArg(exactTypeDesc, helpArgs);

    helpArgs = gtPrependNewCallArg(thisPtr, helpArgs);

    // Call helper function.  This gets the target address of the final destination callsite.

    return gtNewHelperCallNode(CORINFO_HELP_VIRTUAL_FUNC_PTR, TYP_I_IMPL, helpArgs);
}

//------------------------------------------------------------------------
// impBoxPatternMatch: match and import common box idioms
//
// Arguments:
//   pResolvedToken - resolved token from the box operation
//   codeAddr - position in IL stream after the box instruction
//   codeEndp - end of IL stream
//
// Return Value:
//   Number of IL bytes matched and imported, -1 otherwise
//
// Notes:
//   pResolvedToken is known to be a value type; ref type boxing
//   is handled in the CEE_BOX clause.

int Compiler::impBoxPatternMatch(CORINFO_RESOLVED_TOKEN* pResolvedToken, const BYTE* codeAddr, const BYTE* codeEndp)
{
    if (codeAddr >= codeEndp)
    {
        return -1;
    }

    switch (codeAddr[0])
    {
        case CEE_UNBOX_ANY:
            // box + unbox.any
            if (codeAddr + 1 + sizeof(mdToken) <= codeEndp)
            {
                CORINFO_RESOLVED_TOKEN unboxResolvedToken;

                impResolveToken(codeAddr + 1, &unboxResolvedToken, CORINFO_TOKENKIND_Class);

                // See if the resolved tokens describe types that are equal.
                const TypeCompareState compare =
                    info.compCompHnd->compareTypesForEquality(unboxResolvedToken.hClass, pResolvedToken->hClass);

                // If so, box/unbox.any is a nop.
                if (compare == TypeCompareState::Must)
                {
                    JITDUMP("\n Importing BOX; UNBOX.ANY as NOP\n");
                    // Skip the next unbox.any instruction
                    return 1 + sizeof(mdToken);
                }
            }
            break;

        case CEE_BRTRUE:
        case CEE_BRTRUE_S:
        case CEE_BRFALSE:
        case CEE_BRFALSE_S:
            // box + br_true/false
            if ((codeAddr + ((codeAddr[0] >= CEE_BRFALSE) ? 5 : 2)) <= codeEndp)
            {
                GenTree* const treeToBox       = impStackTop().val;
                bool           canOptimize     = true;
                GenTree*       treeToNullcheck = nullptr;

                // Can the thing being boxed cause a side effect?
                if ((treeToBox->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    // Is this a side effect we can replicate cheaply?
                    if (((treeToBox->gtFlags & GTF_SIDE_EFFECT) == GTF_EXCEPT) &&
                        treeToBox->OperIs(GT_OBJ, GT_BLK, GT_IND))
                    {
                        // Yes, we just need to perform a null check if needed.
                        GenTree* const addr = treeToBox->AsOp()->gtGetOp1();
                        if (fgAddrCouldBeNull(addr))
                        {
                            treeToNullcheck = addr;
                        }
                    }
                    else
                    {
                        canOptimize = false;
                    }
                }

                if (canOptimize)
                {
                    CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pResolvedToken->hClass);
                    if (boxHelper == CORINFO_HELP_BOX)
                    {
                        JITDUMP("\n Importing BOX; BR_TRUE/FALSE as %sconstant\n",
                                treeToNullcheck == nullptr ? "" : "nullcheck+");
                        impPopStack();

                        GenTree* result = gtNewIconNode(1);

                        if (treeToNullcheck != nullptr)
                        {
                            GenTree* nullcheck = gtNewNullCheck(treeToNullcheck, compCurBB);
                            result             = gtNewOperNode(GT_COMMA, TYP_INT, nullcheck, result);
                        }

                        impPushOnStack(result, typeInfo(TI_INT));
                        return 0;
                    }
                }
            }
            break;

        case CEE_ISINST:
            if (codeAddr + 1 + sizeof(mdToken) + 1 <= codeEndp)
            {
                const BYTE* nextCodeAddr = codeAddr + 1 + sizeof(mdToken);

                switch (nextCodeAddr[0])
                {
                    // box + isinst + br_true/false
                    case CEE_BRTRUE:
                    case CEE_BRTRUE_S:
                    case CEE_BRFALSE:
                    case CEE_BRFALSE_S:
                        if ((nextCodeAddr + ((nextCodeAddr[0] >= CEE_BRFALSE) ? 5 : 2)) <= codeEndp)
                        {
                            if (!(impStackTop().val->gtFlags & GTF_SIDE_EFFECT))
                            {
                                CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pResolvedToken->hClass);
                                if (boxHelper == CORINFO_HELP_BOX)
                                {
                                    CORINFO_RESOLVED_TOKEN isInstResolvedToken;

                                    impResolveToken(codeAddr + 1, &isInstResolvedToken, CORINFO_TOKENKIND_Casting);

                                    TypeCompareState castResult =
                                        info.compCompHnd->compareTypesForCast(pResolvedToken->hClass,
                                                                              isInstResolvedToken.hClass);
                                    if (castResult != TypeCompareState::May)
                                    {
                                        JITDUMP("\n Importing BOX; ISINST; BR_TRUE/FALSE as constant\n");
                                        impPopStack();

                                        impPushOnStack(gtNewIconNode((castResult == TypeCompareState::Must) ? 1 : 0),
                                                       typeInfo(TI_INT));

                                        // Skip the next isinst instruction
                                        return 1 + sizeof(mdToken);
                                    }
                                }
                                else if (boxHelper == CORINFO_HELP_BOX_NULLABLE)
                                {
                                    // For nullable we're going to fold it to "ldfld hasValue + brtrue/brfalse" or
                                    // "ldc.i4.0 + brtrue/brfalse" in case if the underlying type is not castable to
                                    // the target type.
                                    CORINFO_RESOLVED_TOKEN isInstResolvedToken;
                                    impResolveToken(codeAddr + 1, &isInstResolvedToken, CORINFO_TOKENKIND_Casting);

                                    CORINFO_CLASS_HANDLE nullableCls   = pResolvedToken->hClass;
                                    CORINFO_CLASS_HANDLE underlyingCls = info.compCompHnd->getTypeForBox(nullableCls);

                                    TypeCompareState castResult =
                                        info.compCompHnd->compareTypesForCast(underlyingCls,
                                                                              isInstResolvedToken.hClass);

                                    if (castResult == TypeCompareState::Must)
                                    {
                                        const CORINFO_FIELD_HANDLE hasValueFldHnd =
                                            info.compCompHnd->getFieldInClass(nullableCls, 0);

                                        assert(info.compCompHnd->getFieldOffset(hasValueFldHnd) == 0);
                                        assert(!strcmp(info.compCompHnd->getFieldName(hasValueFldHnd, nullptr),
                                                       "hasValue"));

                                        GenTree* objToBox = impPopStack().val;

                                        // Spill struct to get its address (to access hasValue field)
                                        objToBox =
                                            impGetStructAddr(objToBox, nullableCls, (unsigned)CHECK_SPILL_ALL, true);

                                        impPushOnStack(gtNewFieldRef(TYP_BOOL, hasValueFldHnd, objToBox, 0),
                                                       typeInfo(TI_INT));

                                        JITDUMP("\n Importing BOX; ISINST; BR_TRUE/FALSE as nullableVT.hasValue\n");
                                        return 1 + sizeof(mdToken);
                                    }
                                    else if (castResult == TypeCompareState::MustNot)
                                    {
                                        impPopStack();
                                        impPushOnStack(gtNewIconNode(0), typeInfo(TI_INT));
                                        JITDUMP("\n Importing BOX; ISINST; BR_TRUE/FALSE as constant (false)\n");
                                        return 1 + sizeof(mdToken);
                                    }
                                }
                            }
                        }
                        break;

                    // box + isinst + unbox.any
                    case CEE_UNBOX_ANY:
                        if ((nextCodeAddr + 1 + sizeof(mdToken)) <= codeEndp)
                        {
                            // See if the resolved tokens in box, isinst and unbox.any describe types that are equal.
                            CORINFO_RESOLVED_TOKEN isinstResolvedToken = {};
                            impResolveToken(codeAddr + 1, &isinstResolvedToken, CORINFO_TOKENKIND_Class);

                            if (info.compCompHnd->compareTypesForEquality(isinstResolvedToken.hClass,
                                                                          pResolvedToken->hClass) ==
                                TypeCompareState::Must)
                            {
                                CORINFO_RESOLVED_TOKEN unboxResolvedToken = {};
                                impResolveToken(nextCodeAddr + 1, &unboxResolvedToken, CORINFO_TOKENKIND_Class);

                                // If so, box + isinst + unbox.any is a nop.
                                if (info.compCompHnd->compareTypesForEquality(unboxResolvedToken.hClass,
                                                                              pResolvedToken->hClass) ==
                                    TypeCompareState::Must)
                                {
                                    JITDUMP("\n Importing BOX; ISINST, UNBOX.ANY as NOP\n");
                                    return 2 + sizeof(mdToken) * 2;
                                }
                            }
                        }
                        break;
                }
            }
            break;

        default:
            break;
    }

    return -1;
}

//------------------------------------------------------------------------
// impImportAndPushBox: build and import a value-type box
//
// Arguments:
//   pResolvedToken - resolved token from the box operation
//
// Return Value:
//   None.
//
// Side Effects:
//   The value to be boxed is popped from the stack, and a tree for
//   the boxed value is pushed. This method may create upstream
//   statements, spill side effecting trees, and create new temps.
//
//   If importing an inlinee, we may also discover the inline must
//   fail. If so there is no new value pushed on the stack. Callers
//   should use CompDoNotInline after calling this method to see if
//   ongoing importation should be aborted.
//
// Notes:
//   Boxing of ref classes results in the same value as the value on
//   the top of the stack, so is handled inline in impImportBlockCode
//   for the CEE_BOX case. Only value or primitive type boxes make it
//   here.
//
//   Boxing for nullable types is done via a helper call; boxing
//   of other value types is expanded inline or handled via helper
//   call, depending on the jit's codegen mode.
//
//   When the jit is operating in size and time constrained modes,
//   using a helper call here can save jit time and code size. But it
//   also may inhibit cleanup optimizations that could have also had a
//   even greater benefit effect on code size and jit time. An optimal
//   strategy may need to peek ahead and see if it is easy to tell how
//   the box is being used. For now, we defer.

void Compiler::impImportAndPushBox(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    // Spill any special side effects
    impSpillSpecialSideEff();

    // Get get the expression to box from the stack.
    GenTree*             op1       = nullptr;
    GenTree*             op2       = nullptr;
    StackEntry           se        = impPopStack();
    CORINFO_CLASS_HANDLE operCls   = se.seTypeInfo.GetClassHandle();
    GenTree*             exprToBox = se.val;

    // Look at what helper we should use.
    CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pResolvedToken->hClass);

    // Determine what expansion to prefer.
    //
    // In size/time/debuggable constrained modes, the helper call
    // expansion for box is generally smaller and is preferred, unless
    // the value to box is a struct that comes from a call. In that
    // case the call can construct its return value directly into the
    // box payload, saving possibly some up-front zeroing.
    //
    // Currently primitive type boxes always get inline expanded. We may
    // want to do the same for small structs if they don't come from
    // calls and don't have GC pointers, since explicitly copying such
    // structs is cheap.
    JITDUMP("\nCompiler::impImportAndPushBox -- handling BOX(value class) via");
    bool canExpandInline = (boxHelper == CORINFO_HELP_BOX);
    bool optForSize      = !exprToBox->IsCall() && (operCls != nullptr) && opts.OptimizationDisabled();
    bool expandInline    = canExpandInline && !optForSize;

    if (expandInline)
    {
        JITDUMP(" inline allocate/copy sequence\n");

        // we are doing 'normal' boxing.  This means that we can inline the box operation
        // Box(expr) gets morphed into
        // temp = new(clsHnd)
        // cpobj(temp+4, expr, clsHnd)
        // push temp
        // The code paths differ slightly below for structs and primitives because
        // "cpobj" differs in these cases.  In one case you get
        //    impAssignStructPtr(temp+4, expr, clsHnd)
        // and the other you get
        //    *(temp+4) = expr

        if (opts.OptimizationDisabled())
        {
            // For minopts/debug code, try and minimize the total number
            // of box temps by reusing an existing temp when possible.
            if (impBoxTempInUse || impBoxTemp == BAD_VAR_NUM)
            {
                impBoxTemp = lvaGrabTemp(true DEBUGARG("Reusable Box Helper"));
            }
        }
        else
        {
            // When optimizing, use a new temp for each box operation
            // since we then know the exact class of the box temp.
            impBoxTemp                       = lvaGrabTemp(true DEBUGARG("Single-def Box Helper"));
            lvaTable[impBoxTemp].lvType      = TYP_REF;
            lvaTable[impBoxTemp].lvSingleDef = 1;
            JITDUMP("Marking V%02u as a single def local\n", impBoxTemp);
            const bool isExact = true;
            lvaSetClass(impBoxTemp, pResolvedToken->hClass, isExact);
        }

        // needs to stay in use until this box expression is appended
        // some other node.  We approximate this by keeping it alive until
        // the opcode stack becomes empty
        impBoxTempInUse = true;

        const bool useParent = false;
        op1                  = gtNewAllocObjNode(pResolvedToken, useParent);
        if (op1 == nullptr)
        {
            return;
        }

        /* Remember that this basic block contains 'new' of an object, and so does this method */
        compCurBB->bbFlags |= BBF_HAS_NEWOBJ;
        optMethodFlags |= OMF_HAS_NEWOBJ;

        GenTree* asg = gtNewTempAssign(impBoxTemp, op1);

        Statement* asgStmt = impAppendTree(asg, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        op1 = gtNewLclvNode(impBoxTemp, TYP_REF);
        op2 = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
        op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1, op2);

        if (varTypeIsStruct(exprToBox))
        {
            assert(info.compCompHnd->getClassSize(pResolvedToken->hClass) == info.compCompHnd->getClassSize(operCls));
            op1 = impAssignStructPtr(op1, exprToBox, operCls, (unsigned)CHECK_SPILL_ALL);
        }
        else
        {
            var_types lclTyp = exprToBox->TypeGet();
            if (lclTyp == TYP_BYREF)
            {
                lclTyp = TYP_I_IMPL;
            }
            CorInfoType jitType = info.compCompHnd->asCorInfoType(pResolvedToken->hClass);
            if (impIsPrimitive(jitType))
            {
                lclTyp = JITtype2varType(jitType);
            }
            assert(genActualType(exprToBox->TypeGet()) == genActualType(lclTyp) ||
                   varTypeIsFloating(lclTyp) == varTypeIsFloating(exprToBox->TypeGet()));
            var_types srcTyp = exprToBox->TypeGet();
            var_types dstTyp = lclTyp;

            if (srcTyp != dstTyp)
            {
                assert((varTypeIsFloating(srcTyp) && varTypeIsFloating(dstTyp)) ||
                       (varTypeIsIntegral(srcTyp) && varTypeIsIntegral(dstTyp)));
                exprToBox = gtNewCastNode(dstTyp, exprToBox, false, dstTyp);
            }
            op1 = gtNewAssignNode(gtNewOperNode(GT_IND, lclTyp, op1), exprToBox);
        }

        // Spill eval stack to flush out any pending side effects.
        impSpillSideEffects(true, (unsigned)CHECK_SPILL_ALL DEBUGARG("impImportAndPushBox"));

        // Set up this copy as a second assignment.
        Statement* copyStmt = impAppendTree(op1, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        op1 = gtNewLclvNode(impBoxTemp, TYP_REF);

        // Record that this is a "box" node and keep track of the matching parts.
        op1 = new (this, GT_BOX) GenTreeBox(TYP_REF, op1, asgStmt, copyStmt);

        // If it is a value class, mark the "box" node.  We can use this information
        // to optimise several cases:
        //    "box(x) == null" --> false
        //    "(box(x)).CallAnInterfaceMethod(...)" --> "(&x).CallAValueTypeMethod"
        //    "(box(x)).CallAnObjectMethod(...)" --> "(&x).CallAValueTypeMethod"

        op1->gtFlags |= GTF_BOX_VALUE;
        assert(op1->IsBoxedValue());
        assert(asg->gtOper == GT_ASG);
    }
    else
    {
        // Don't optimize, just call the helper and be done with it.
        JITDUMP(" helper call because: %s\n", canExpandInline ? "optimizing for size" : "nullable");
        assert(operCls != nullptr);

        // Ensure that the value class is restored
        op2 = impTokenToHandle(pResolvedToken, nullptr, true /* mustRestoreHandle */);
        if (op2 == nullptr)
        {
            // We must be backing out of an inline.
            assert(compDonotInline());
            return;
        }

        GenTreeCall::Use* args =
            gtNewCallArgs(op2, impGetStructAddr(exprToBox, operCls, (unsigned)CHECK_SPILL_ALL, true));
        op1 = gtNewHelperCallNode(boxHelper, TYP_REF, args);
    }

    /* Push the result back on the stack, */
    /* even if clsHnd is a value class we want the TI_REF */
    typeInfo tiRetVal = typeInfo(TI_REF, info.compCompHnd->getTypeForBox(pResolvedToken->hClass));
    impPushOnStack(op1, tiRetVal);
}

//------------------------------------------------------------------------
// impImportNewObjArray: Build and import `new` of multi-dimmensional array
//
// Arguments:
//    pResolvedToken - The CORINFO_RESOLVED_TOKEN that has been initialized
//                     by a call to CEEInfo::resolveToken().
//    pCallInfo - The CORINFO_CALL_INFO that has been initialized
//                by a call to CEEInfo::getCallInfo().
//
// Assumptions:
//    The multi-dimensional array constructor arguments (array dimensions) are
//    pushed on the IL stack on entry to this method.
//
// Notes:
//    Multi-dimensional array constructors are imported as calls to a JIT
//    helper, not as regular calls.

void Compiler::impImportNewObjArray(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo)
{
    GenTree* classHandle = impParentClassTokenToHandle(pResolvedToken);
    if (classHandle == nullptr)
    { // compDonotInline()
        return;
    }

    assert(pCallInfo->sig.numArgs);

    GenTree* node;

    //
    // There are two different JIT helpers that can be used to allocate
    // multi-dimensional arrays:
    //
    // - CORINFO_HELP_NEW_MDARR - takes the array dimensions as varargs.
    //      This variant is deprecated. It should be eventually removed.
    //
    // - CORINFO_HELP_NEW_MDARR_NONVARARG - takes the array dimensions as
    //      pointer to block of int32s. This variant is more portable.
    //
    // The non-varargs helper is enabled for CoreRT only for now. Enabling this
    // unconditionally would require ReadyToRun version bump.
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

    if (!opts.IsReadyToRun() || IsTargetAbi(CORINFO_CORERT_ABI))
    {

        // Reuse the temp used to pass the array dimensions to avoid bloating
        // the stack frame in case there are multiple calls to multi-dim array
        // constructors within a single method.
        if (lvaNewObjArrayArgs == BAD_VAR_NUM)
        {
            lvaNewObjArrayArgs                       = lvaGrabTemp(false DEBUGARG("NewObjArrayArgs"));
            lvaTable[lvaNewObjArrayArgs].lvType      = TYP_BLK;
            lvaTable[lvaNewObjArrayArgs].lvExactSize = 0;
        }

        // Increase size of lvaNewObjArrayArgs to be the largest size needed to hold 'numArgs' integers
        // for our call to CORINFO_HELP_NEW_MDARR_NONVARARG.
        lvaTable[lvaNewObjArrayArgs].lvExactSize =
            max(lvaTable[lvaNewObjArrayArgs].lvExactSize, pCallInfo->sig.numArgs * sizeof(INT32));

        // The side-effects may include allocation of more multi-dimensional arrays. Spill all side-effects
        // to ensure that the shared lvaNewObjArrayArgs local variable is only ever used to pass arguments
        // to one allocation at a time.
        impSpillSideEffects(true, (unsigned)CHECK_SPILL_ALL DEBUGARG("impImportNewObjArray"));

        //
        // The arguments of the CORINFO_HELP_NEW_MDARR_NONVARARG helper are:
        //  - Array class handle
        //  - Number of dimension arguments
        //  - Pointer to block of int32 dimensions - address  of lvaNewObjArrayArgs temp.
        //

        node = gtNewLclvNode(lvaNewObjArrayArgs, TYP_BLK);
        node = gtNewOperNode(GT_ADDR, TYP_I_IMPL, node);

        // Pop dimension arguments from the stack one at a time and store it
        // into lvaNewObjArrayArgs temp.
        for (int i = pCallInfo->sig.numArgs - 1; i >= 0; i--)
        {
            GenTree* arg = impImplicitIorI4Cast(impPopStack().val, TYP_INT);

            GenTree* dest = gtNewLclvNode(lvaNewObjArrayArgs, TYP_BLK);
            dest          = gtNewOperNode(GT_ADDR, TYP_I_IMPL, dest);
            dest          = gtNewOperNode(GT_ADD, TYP_I_IMPL, dest,
                                 new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, sizeof(INT32) * i));
            dest = gtNewOperNode(GT_IND, TYP_INT, dest);

            node = gtNewOperNode(GT_COMMA, node->TypeGet(), gtNewAssignNode(dest, arg), node);
        }

        GenTreeCall::Use* args = gtNewCallArgs(node);

        // pass number of arguments to the helper
        args = gtPrependNewCallArg(gtNewIconNode(pCallInfo->sig.numArgs), args);

        args = gtPrependNewCallArg(classHandle, args);

        node = gtNewHelperCallNode(CORINFO_HELP_NEW_MDARR_NONVARARG, TYP_REF, args);
    }
    else
    {
        //
        // The varargs helper needs the type and method handles as last
        // and  last-1 param (this is a cdecl call, so args will be
        // pushed in reverse order on the CPU stack)
        //

        GenTreeCall::Use* args = gtNewCallArgs(classHandle);

        // pass number of arguments to the helper
        args = gtPrependNewCallArg(gtNewIconNode(pCallInfo->sig.numArgs), args);

        args = impPopCallArgs(pCallInfo->sig.numArgs, &pCallInfo->sig, args);

        node = gtNewHelperCallNode(CORINFO_HELP_NEW_MDARR, TYP_REF, args);

        // varargs, so we pop the arguments
        node->gtFlags |= GTF_CALL_POP_ARGS;

#ifdef DEBUG
        // At the present time we don't track Caller pop arguments
        // that have GC references in them
        for (GenTreeCall::Use& use : GenTreeCall::UseList(args))
        {
            assert(use.GetNode()->TypeGet() != TYP_REF);
        }
#endif
    }

    for (GenTreeCall::Use& use : node->AsCall()->Args())
    {
        node->gtFlags |= use.GetNode()->gtFlags & GTF_GLOB_EFFECT;
    }

    node->AsCall()->compileTimeHelperArgumentHandle = (CORINFO_GENERIC_HANDLE)pResolvedToken->hClass;

    // Remember that this basic block contains 'new' of a md array
    compCurBB->bbFlags |= BBF_HAS_NEWARRAY;

    impPushOnStack(node, typeInfo(TI_REF, pResolvedToken->hClass));
}

GenTree* Compiler::impTransformThis(GenTree*                thisPtr,
                                    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                    CORINFO_THIS_TRANSFORM  transform)
{
    switch (transform)
    {
        case CORINFO_DEREF_THIS:
        {
            GenTree* obj = thisPtr;

            // This does a LDIND on the obj, which should be a byref. pointing to a ref
            impBashVarAddrsToI(obj);
            assert(genActualType(obj->gtType) == TYP_I_IMPL || obj->gtType == TYP_BYREF);
            CorInfoType constraintTyp = info.compCompHnd->asCorInfoType(pConstrainedResolvedToken->hClass);

            obj = gtNewOperNode(GT_IND, JITtype2varType(constraintTyp), obj);
            // ldind could point anywhere, example a boxed class static int
            obj->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF | GTF_IND_TGTANYWHERE);

            return obj;
        }

        case CORINFO_BOX_THIS:
        {
            // Constraint calls where there might be no
            // unboxed entry point require us to implement the call via helper.
            // These only occur when a possible target of the call
            // may have inherited an implementation of an interface
            // method from System.Object or System.ValueType.  The EE does not provide us with
            // "unboxed" versions of these methods.

            GenTree* obj = thisPtr;

            assert(obj->TypeGet() == TYP_BYREF || obj->TypeGet() == TYP_I_IMPL);
            obj = gtNewObjNode(pConstrainedResolvedToken->hClass, obj);
            obj->gtFlags |= GTF_EXCEPT;

            CorInfoType jitTyp = info.compCompHnd->asCorInfoType(pConstrainedResolvedToken->hClass);
            if (impIsPrimitive(jitTyp))
            {
                if (obj->OperIsBlk())
                {
                    obj->ChangeOperUnchecked(GT_IND);

                    // Obj could point anywhere, example a boxed class static int
                    obj->gtFlags |= GTF_IND_TGTANYWHERE;
                    obj->AsOp()->gtOp2 = nullptr; // must be zero for tree walkers
                }

                obj->gtType = JITtype2varType(jitTyp);
                assert(varTypeIsArithmetic(obj->gtType));
            }

            // This pushes on the dereferenced byref
            // This is then used immediately to box.
            impPushOnStack(obj, verMakeTypeInfo(pConstrainedResolvedToken->hClass).NormaliseForStack());

            // This pops off the byref-to-a-value-type remaining on the stack and
            // replaces it with a boxed object.
            // This is then used as the object to the virtual call immediately below.
            impImportAndPushBox(pConstrainedResolvedToken);
            if (compDonotInline())
            {
                return nullptr;
            }

            obj = impPopStack().val;
            return obj;
        }
        case CORINFO_NO_THIS_TRANSFORM:
        default:
            return thisPtr;
    }
}

//------------------------------------------------------------------------
// impCanPInvokeInline: check whether PInvoke inlining should enabled in current method.
//
// Return Value:
//    true if PInvoke inlining should be enabled in current method, false otherwise
//
// Notes:
//    Checks a number of ambient conditions where we could pinvoke but choose not to

bool Compiler::impCanPInvokeInline()
{
    return getInlinePInvokeEnabled() && (!opts.compDbgCode) && (compCodeOpt() != SMALL_CODE) &&
           (!opts.compNoPInvokeInlineCB) // profiler is preventing inline pinvoke
        ;
}

//------------------------------------------------------------------------
// impCanPInvokeInlineCallSite: basic legality checks using information
// from a call to see if the call qualifies as an inline pinvoke.
//
// Arguments:
//    block      - block contaning the call, or for inlinees, block
//                 containing the call being inlined
//
// Return Value:
//    true if this call can legally qualify as an inline pinvoke, false otherwise
//
// Notes:
//    For runtimes that support exception handling interop there are
//    restrictions on using inline pinvoke in handler regions.
//
//    * We have to disable pinvoke inlining inside of filters because
//    in case the main execution (i.e. in the try block) is inside
//    unmanaged code, we cannot reuse the inlined stub (we still need
//    the original state until we are in the catch handler)
//
//    * We disable pinvoke inlining inside handlers since the GSCookie
//    is in the inlined Frame (see
//    CORINFO_EE_INFO::InlinedCallFrameInfo::offsetOfGSCookie), but
//    this would not protect framelets/return-address of handlers.
//
//    These restrictions are currently also in place for CoreCLR but
//    can be relaxed when coreclr/#8459 is addressed.

bool Compiler::impCanPInvokeInlineCallSite(BasicBlock* block)
{
    if (block->hasHndIndex())
    {
        return false;
    }

    // The remaining limitations do not apply to CoreRT
    if (IsTargetAbi(CORINFO_CORERT_ABI))
    {
        return true;
    }

#ifdef TARGET_64BIT
    // On 64-bit platforms, we disable pinvoke inlining inside of try regions.
    // Note that this could be needed on other architectures too, but we
    // haven't done enough investigation to know for sure at this point.
    //
    // Here is the comment from JIT64 explaining why:
    //   [VSWhidbey: 611015] - because the jitted code links in the
    //   Frame (instead of the stub) we rely on the Frame not being
    //   'active' until inside the stub.  This normally happens by the
    //   stub setting the return address pointer in the Frame object
    //   inside the stub.  On a normal return, the return address
    //   pointer is zeroed out so the Frame can be safely re-used, but
    //   if an exception occurs, nobody zeros out the return address
    //   pointer.  Thus if we re-used the Frame object, it would go
    //   'active' as soon as we link it into the Frame chain.
    //
    //   Technically we only need to disable PInvoke inlining if we're
    //   in a handler or if we're in a try body with a catch or
    //   filter/except where other non-handler code in this method
    //   might run and try to re-use the dirty Frame object.
    //
    //   A desktop test case where this seems to matter is
    //   jit\jit64\ebvts\mcpp\sources2\ijw\__clrcall\vector_ctor_dtor.02\deldtor_clr.exe
    if (block->hasTryIndex())
    {
        // This does not apply to the raw pinvoke call that is inside the pinvoke
        // ILStub. In this case, we have to inline the raw pinvoke call into the stub,
        // otherwise we would end up with a stub that recursively calls itself, and end
        // up with a stack overflow.
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) && opts.ShouldUsePInvokeHelpers())
        {
            return true;
        }

        return false;
    }
#endif // TARGET_64BIT

    return true;
}

//------------------------------------------------------------------------
// impCheckForPInvokeCall examine call to see if it is a pinvoke and if so
// if it can be expressed as an inline pinvoke.
//
// Arguments:
//    call       - tree for the call
//    methHnd    - handle for the method being called (may be null)
//    sig        - signature of the method being called
//    mflags     - method flags for the method being called
//    block      - block contaning the call, or for inlinees, block
//                 containing the call being inlined
//
// Notes:
//   Sets GTF_CALL_M_PINVOKE on the call for pinvokes.
//
//   Also sets GTF_CALL_UNMANAGED on call for inline pinvokes if the
//   call passes a combination of legality and profitabilty checks.
//
//   If GTF_CALL_UNMANAGED is set, increments info.compUnmanagedCallCountWithGCTransition

void Compiler::impCheckForPInvokeCall(
    GenTreeCall* call, CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* sig, unsigned mflags, BasicBlock* block)
{
    CorInfoCallConvExtension unmanagedCallConv;

    // If VM flagged it as Pinvoke, flag the call node accordingly
    if ((mflags & CORINFO_FLG_PINVOKE) != 0)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_PINVOKE;
    }

    bool suppressGCTransition = false;
    if (methHnd)
    {
        if ((mflags & CORINFO_FLG_PINVOKE) == 0)
        {
            return;
        }

        unmanagedCallConv = info.compCompHnd->getUnmanagedCallConv(methHnd, nullptr, &suppressGCTransition);
    }
    else
    {
        if (sig->getCallConv() == CORINFO_CALLCONV_DEFAULT || sig->getCallConv() == CORINFO_CALLCONV_VARARG)
        {
            return;
        }

        unmanagedCallConv = info.compCompHnd->getUnmanagedCallConv(nullptr, sig, &suppressGCTransition);

        assert(!call->gtCallCookie);
    }

    if (suppressGCTransition)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_SUPPRESS_GC_TRANSITION;
    }

    // If we can't get the unmanaged calling convention or the calling convention is unsupported in the JIT,
    // return here without inlining the native call.
    if (unmanagedCallConv == CorInfoCallConvExtension::Managed ||
        unmanagedCallConv == CorInfoCallConvExtension::Fastcall ||
        unmanagedCallConv == CorInfoCallConvExtension::FastcallMemberFunction)
    {
        return;
    }
    optNativeCallCount++;

    if (methHnd == nullptr && (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) || IsTargetAbi(CORINFO_CORERT_ABI)))
    {
        // PInvoke in CoreRT ABI must be always inlined. Non-inlineable CALLI cases have been
        // converted to regular method calls earlier using convertPInvokeCalliToCall.

        // PInvoke CALLI in IL stubs must be inlined
    }
    else
    {
        // Check legality
        if (!impCanPInvokeInlineCallSite(block))
        {
            return;
        }

        // Legal PInvoke CALL in PInvoke IL stubs must be inlined to avoid infinite recursive
        // inlining in CoreRT. Skip the ambient conditions checks and profitability checks.
        if (!IsTargetAbi(CORINFO_CORERT_ABI) || (info.compFlags & CORINFO_FLG_PINVOKE) == 0)
        {
            if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) && opts.ShouldUsePInvokeHelpers())
            {
                // Raw PInvoke call in PInvoke IL stub generated must be inlined to avoid infinite
                // recursive calls to the stub.
            }
            else
            {
                if (!impCanPInvokeInline())
                {
                    return;
                }

                // Size-speed tradeoff: don't use inline pinvoke at rarely
                // executed call sites.  The non-inline version is more
                // compact.
                if (block->isRunRarely())
                {
                    return;
                }
            }
        }

        // The expensive check should be last
        if (info.compCompHnd->pInvokeMarshalingRequired(methHnd, sig))
        {
            return;
        }
    }

    JITLOG((LL_INFO1000000, "\nInline a CALLI PINVOKE call from method %s", info.compFullName));

    call->gtFlags |= GTF_CALL_UNMANAGED;
    call->unmgdCallConv = unmanagedCallConv;
    if (!call->IsSuppressGCTransition())
    {
        info.compUnmanagedCallCountWithGCTransition++;
    }

    // AMD64 convention is same for native and managed
    if (unmanagedCallConv == CorInfoCallConvExtension::C ||
        unmanagedCallConv == CorInfoCallConvExtension::CMemberFunction)
    {
        call->gtFlags |= GTF_CALL_POP_ARGS;
    }

    if (unmanagedCallConv == CorInfoCallConvExtension::Thiscall)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_UNMGD_THISCALL;
    }
}

GenTreeCall* Compiler::impImportIndirectCall(CORINFO_SIG_INFO* sig, IL_OFFSETX ilOffset)
{
    var_types callRetTyp = JITtype2varType(sig->retType);

    /* The function pointer is on top of the stack - It may be a
     * complex expression. As it is evaluated after the args,
     * it may cause registered args to be spilled. Simply spill it.
     */

    // Ignore this trivial case.
    if (impStackTop().val->gtOper != GT_LCL_VAR)
    {
        impSpillStackEntry(verCurrentState.esStackDepth - 1,
                           BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impImportIndirectCall"));
    }

    /* Get the function pointer */

    GenTree* fptr = impPopStack().val;

    // The function pointer is typically a sized to match the target pointer size
    // However, stubgen IL optimization can change LDC.I8 to LDC.I4
    // See ILCodeStream::LowerOpcode
    assert(genActualType(fptr->gtType) == TYP_I_IMPL || genActualType(fptr->gtType) == TYP_INT);

#ifdef DEBUG
    // This temporary must never be converted to a double in stress mode,
    // because that can introduce a call to the cast helper after the
    // arguments have already been evaluated.

    if (fptr->OperGet() == GT_LCL_VAR)
    {
        lvaTable[fptr->AsLclVarCommon()->GetLclNum()].lvKeepType = 1;
    }
#endif

    /* Create the call node */

    GenTreeCall* call = gtNewIndCallNode(fptr, callRetTyp, nullptr, ilOffset);

    call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);

    return call;
}

/*****************************************************************************/

void Compiler::impPopArgsForUnmanagedCall(GenTree* call, CORINFO_SIG_INFO* sig)
{
    assert(call->gtFlags & GTF_CALL_UNMANAGED);

    /* Since we push the arguments in reverse order (i.e. right -> left)
     * spill any side effects from the stack
     *
     * OBS: If there is only one side effect we do not need to spill it
     *      thus we have to spill all side-effects except last one
     */

    unsigned lastLevelWithSideEffects = UINT_MAX;

    unsigned argsToReverse = sig->numArgs;

    // For "thiscall", the first argument goes in a register. Since its
    // order does not need to be changed, we do not need to spill it

    if (call->AsCall()->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
    {
        assert(argsToReverse);
        argsToReverse--;
    }

#ifndef TARGET_X86
    // Don't reverse args on ARM or x64 - first four args always placed in regs in order
    argsToReverse = 0;
#endif

    for (unsigned level = verCurrentState.esStackDepth - argsToReverse; level < verCurrentState.esStackDepth; level++)
    {
        if (verCurrentState.esStack[level].val->gtFlags & GTF_ORDER_SIDEEFF)
        {
            assert(lastLevelWithSideEffects == UINT_MAX);

            impSpillStackEntry(level,
                               BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impPopArgsForUnmanagedCall - other side effect"));
        }
        else if (verCurrentState.esStack[level].val->gtFlags & GTF_SIDE_EFFECT)
        {
            if (lastLevelWithSideEffects != UINT_MAX)
            {
                /* We had a previous side effect - must spill it */
                impSpillStackEntry(lastLevelWithSideEffects,
                                   BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impPopArgsForUnmanagedCall - side effect"));

                /* Record the level for the current side effect in case we will spill it */
                lastLevelWithSideEffects = level;
            }
            else
            {
                /* This is the first side effect encountered - record its level */

                lastLevelWithSideEffects = level;
            }
        }
    }

    /* The argument list is now "clean" - no out-of-order side effects
     * Pop the argument list in reverse order */

    GenTreeCall::Use* args     = impPopReverseCallArgs(sig->numArgs, sig, sig->numArgs - argsToReverse);
    call->AsCall()->gtCallArgs = args;

    if (call->AsCall()->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
    {
        GenTree* thisPtr = args->GetNode();
        impBashVarAddrsToI(thisPtr);
        assert(thisPtr->TypeGet() == TYP_I_IMPL || thisPtr->TypeGet() == TYP_BYREF);
    }

    for (GenTreeCall::Use& argUse : GenTreeCall::UseList(args))
    {
        GenTree* arg = argUse.GetNode();
        call->gtFlags |= arg->gtFlags & GTF_GLOB_EFFECT;

        // We should not be passing gc typed args to an unmanaged call.
        if (varTypeIsGC(arg->TypeGet()))
        {
            // Tolerate byrefs by retyping to native int.
            //
            // This is needed or we'll generate inconsistent GC info
            // for this arg at the call site (gc info says byref,
            // pinvoke sig says native int).
            //
            if (arg->TypeGet() == TYP_BYREF)
            {
                arg->ChangeType(TYP_I_IMPL);
            }
            else
            {
                assert(!"*** invalid IL: gc ref passed to unmanaged call");
            }
        }
    }
}

//------------------------------------------------------------------------
// impInitClass: Build a node to initialize the class before accessing the
//               field if necessary
//
// Arguments:
//    pResolvedToken - The CORINFO_RESOLVED_TOKEN that has been initialized
//                     by a call to CEEInfo::resolveToken().
//
// Return Value: If needed, a pointer to the node that will perform the class
//               initializtion.  Otherwise, nullptr.
//

GenTree* Compiler::impInitClass(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    CorInfoInitClassResult initClassResult =
        info.compCompHnd->initClass(pResolvedToken->hField, info.compMethodHnd, impTokenLookupContextHandle);

    if ((initClassResult & CORINFO_INITCLASS_USE_HELPER) == 0)
    {
        return nullptr;
    }
    bool runtimeLookup;

    GenTree* node = impParentClassTokenToHandle(pResolvedToken, &runtimeLookup);

    if (node == nullptr)
    {
        assert(compDonotInline());
        return nullptr;
    }

    if (runtimeLookup)
    {
        node = gtNewHelperCallNode(CORINFO_HELP_INITCLASS, TYP_VOID, gtNewCallArgs(node));
    }
    else
    {
        // Call the shared non gc static helper, as its the fastest
        node = fgGetSharedCCtor(pResolvedToken->hClass);
    }

    return node;
}

GenTree* Compiler::impImportStaticReadOnlyField(void* fldAddr, var_types lclTyp)
{
    GenTree* op1 = nullptr;

    switch (lclTyp)
    {
        int     ival;
        __int64 lval;
        double  dval;

        case TYP_BOOL:
            ival = *((bool*)fldAddr);
            goto IVAL_COMMON;

        case TYP_BYTE:
            ival = *((signed char*)fldAddr);
            goto IVAL_COMMON;

        case TYP_UBYTE:
            ival = *((unsigned char*)fldAddr);
            goto IVAL_COMMON;

        case TYP_SHORT:
            ival = *((short*)fldAddr);
            goto IVAL_COMMON;

        case TYP_USHORT:
            ival = *((unsigned short*)fldAddr);
            goto IVAL_COMMON;

        case TYP_UINT:
        case TYP_INT:
            ival = *((int*)fldAddr);
        IVAL_COMMON:
            op1 = gtNewIconNode(ival);
            break;

        case TYP_LONG:
        case TYP_ULONG:
            lval = *((__int64*)fldAddr);
            op1  = gtNewLconNode(lval);
            break;

        case TYP_FLOAT:
            dval        = *((float*)fldAddr);
            op1         = gtNewDconNode(dval);
            op1->gtType = TYP_FLOAT;
            break;

        case TYP_DOUBLE:
            dval = *((double*)fldAddr);
            op1  = gtNewDconNode(dval);
            break;

        default:
            assert(!"Unexpected lclTyp");
            break;
    }

    return op1;
}

GenTree* Compiler::impImportStaticFieldAccess(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                              CORINFO_ACCESS_FLAGS    access,
                                              CORINFO_FIELD_INFO*     pFieldInfo,
                                              var_types               lclTyp)
{
    GenTree* op1;

    switch (pFieldInfo->fieldAccessor)
    {
        case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
        {
            assert(!compIsForInlining());

            // We first call a special helper to get the statics base pointer
            op1 = impParentClassTokenToHandle(pResolvedToken);

            // compIsForInlining() is false so we should not get NULL here
            assert(op1 != nullptr);

            var_types type = TYP_BYREF;

            switch (pFieldInfo->helper)
            {
                case CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE:
                    type = TYP_I_IMPL;
                    break;
                case CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
                case CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
                case CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE:
                    break;
                default:
                    assert(!"unknown generic statics helper");
                    break;
            }

            op1 = gtNewHelperCallNode(pFieldInfo->helper, type, gtNewCallArgs(op1));

            FieldSeqNode* fs = GetFieldSeqStore()->CreateSingleton(pResolvedToken->hField);
            op1              = gtNewOperNode(GT_ADD, type, op1,
                                new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, pFieldInfo->offset, fs));
        }
        break;

        case CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER:
        {
#ifdef FEATURE_READYTORUN_COMPILER
            if (opts.IsReadyToRun())
            {
                GenTreeFlags callFlags = GTF_EMPTY;

                if (info.compCompHnd->getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_BEFOREFIELDINIT)
                {
                    callFlags |= GTF_CALL_HOISTABLE;
                }

                op1 = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_STATIC_BASE, TYP_BYREF);
                op1->gtFlags |= callFlags;

                op1->AsCall()->setEntryPoint(pFieldInfo->fieldLookup);
            }
            else
#endif
            {
                op1 = fgGetStaticsCCtorHelper(pResolvedToken->hClass, pFieldInfo->helper);
            }

            {
                FieldSeqNode* fs = GetFieldSeqStore()->CreateSingleton(pResolvedToken->hField);
                op1              = gtNewOperNode(GT_ADD, op1->TypeGet(), op1,
                                    new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, pFieldInfo->offset, fs));
            }
            break;
        }

        case CORINFO_FIELD_STATIC_READYTORUN_HELPER:
        {
#ifdef FEATURE_READYTORUN_COMPILER
            assert(opts.IsReadyToRun());
            assert(!compIsForInlining());
            CORINFO_LOOKUP_KIND kind;
            info.compCompHnd->getLocationOfThisType(info.compMethodHnd, &kind);
            assert(kind.needsRuntimeLookup);

            GenTree*          ctxTree = getRuntimeContextTree(kind.runtimeLookupKind);
            GenTreeCall::Use* args    = gtNewCallArgs(ctxTree);

            GenTreeFlags callFlags = GTF_EMPTY;

            if (info.compCompHnd->getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_BEFOREFIELDINIT)
            {
                callFlags |= GTF_CALL_HOISTABLE;
            }
            var_types type = TYP_BYREF;
            op1            = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE, type, args);
            op1->gtFlags |= callFlags;

            op1->AsCall()->setEntryPoint(pFieldInfo->fieldLookup);
            FieldSeqNode* fs = GetFieldSeqStore()->CreateSingleton(pResolvedToken->hField);
            op1              = gtNewOperNode(GT_ADD, type, op1,
                                new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, pFieldInfo->offset, fs));
#else
            unreached();
#endif // FEATURE_READYTORUN_COMPILER
        }
        break;

        default:
        {
            // Do we need the address of a static field?
            //
            if (access & CORINFO_ACCESS_ADDRESS)
            {
                void** pFldAddr = nullptr;
                void*  fldAddr  = info.compCompHnd->getFieldAddress(pResolvedToken->hField, (void**)&pFldAddr);

                // We should always be able to access this static's address directly
                //
                assert(pFldAddr == nullptr);

                FieldSeqNode* fldSeq = GetFieldSeqStore()->CreateSingleton(pResolvedToken->hField);

                /* Create the data member node */
                op1 = gtNewIconHandleNode(pFldAddr == nullptr ? (size_t)fldAddr : (size_t)pFldAddr, GTF_ICON_STATIC_HDL,
                                          fldSeq);
#ifdef DEBUG
                op1->AsIntCon()->gtTargetHandle = op1->AsIntCon()->gtIconVal;
#endif

                if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    op1->gtFlags |= GTF_ICON_INITCLASS;
                }
            }
            else // We need the value of a static field
            {
                // In future, it may be better to just create the right tree here instead of folding it later.
                op1 = gtNewFieldRef(lclTyp, pResolvedToken->hField);

                if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    op1->gtFlags |= GTF_FLD_INITCLASS;
                }

                if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP)
                {
                    op1->gtType = TYP_REF; // points at boxed object
                    FieldSeqNode* firstElemFldSeq =
                        GetFieldSeqStore()->CreateSingleton(FieldSeqStore::FirstElemPseudoField);
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        new (this, GT_CNS_INT)
                                            GenTreeIntCon(TYP_I_IMPL, TARGET_POINTER_SIZE, firstElemFldSeq));

                    if (varTypeIsStruct(lclTyp))
                    {
                        // Constructor adds GTF_GLOB_REF.  Note that this is *not* GTF_EXCEPT.
                        op1 = gtNewObjNode(pFieldInfo->structType, op1);
                    }
                    else
                    {
                        op1 = gtNewOperNode(GT_IND, lclTyp, op1);
                        op1->gtFlags |= GTF_GLOB_REF | GTF_IND_NONFAULTING;
                    }
                }

                return op1;
            }

            break;
        }
    }

    if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP)
    {
        op1 = gtNewOperNode(GT_IND, TYP_REF, op1);

        FieldSeqNode* fldSeq = GetFieldSeqStore()->CreateSingleton(FieldSeqStore::FirstElemPseudoField);

        op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                            new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, TARGET_POINTER_SIZE, fldSeq));
    }

    if (!(access & CORINFO_ACCESS_ADDRESS))
    {
        if (varTypeIsStruct(lclTyp))
        {
            // Constructor adds GTF_GLOB_REF.  Note that this is *not* GTF_EXCEPT.
            op1 = gtNewObjNode(pFieldInfo->structType, op1);
        }
        else
        {
            op1 = gtNewOperNode(GT_IND, lclTyp, op1);
            op1->gtFlags |= GTF_GLOB_REF;
        }
    }

    return op1;
}

// In general try to call this before most of the verification work.  Most people expect the access
// exceptions before the verification exceptions.  If you do this after, that usually doesn't happen.  Turns
// out if you can't access something we also think that you're unverifiable for other reasons.
void Compiler::impHandleAccessAllowed(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall)
{
    if (result != CORINFO_ACCESS_ALLOWED)
    {
        impHandleAccessAllowedInternal(result, helperCall);
    }
}

void Compiler::impHandleAccessAllowedInternal(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall)
{
    switch (result)
    {
        case CORINFO_ACCESS_ALLOWED:
            break;
        case CORINFO_ACCESS_ILLEGAL:
            // if we're verifying, then we need to reject the illegal access to ensure that we don't think the
            // method is verifiable.  Otherwise, delay the exception to runtime.
            if (compIsForImportOnly())
            {
                info.compCompHnd->ThrowExceptionForHelper(helperCall);
            }
            else
            {
                impInsertHelperCall(helperCall);
            }
            break;
    }
}

void Compiler::impInsertHelperCall(CORINFO_HELPER_DESC* helperInfo)
{
    // Construct the argument list
    GenTreeCall::Use* args = nullptr;
    assert(helperInfo->helperNum != CORINFO_HELP_UNDEF);
    for (unsigned i = helperInfo->numArgs; i > 0; --i)
    {
        const CORINFO_HELPER_ARG& helperArg  = helperInfo->args[i - 1];
        GenTree*                  currentArg = nullptr;
        switch (helperArg.argType)
        {
            case CORINFO_HELPER_ARG_TYPE_Field:
                info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(
                    info.compCompHnd->getFieldClass(helperArg.fieldHandle));
                currentArg = gtNewIconEmbFldHndNode(helperArg.fieldHandle);
                break;
            case CORINFO_HELPER_ARG_TYPE_Method:
                info.compCompHnd->methodMustBeLoadedBeforeCodeIsRun(helperArg.methodHandle);
                currentArg = gtNewIconEmbMethHndNode(helperArg.methodHandle);
                break;
            case CORINFO_HELPER_ARG_TYPE_Class:
                info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(helperArg.classHandle);
                currentArg = gtNewIconEmbClsHndNode(helperArg.classHandle);
                break;
            case CORINFO_HELPER_ARG_TYPE_Module:
                currentArg = gtNewIconEmbScpHndNode(helperArg.moduleHandle);
                break;
            case CORINFO_HELPER_ARG_TYPE_Const:
                currentArg = gtNewIconNode(helperArg.constant);
                break;
            default:
                NO_WAY("Illegal helper arg type");
        }
        args = gtPrependNewCallArg(currentArg, args);
    }

    /* TODO-Review:
     * Mark as CSE'able, and hoistable.  Consider marking hoistable unless you're in the inlinee.
     * Also, consider sticking this in the first basic block.
     */
    GenTree* callout = gtNewHelperCallNode(helperInfo->helperNum, TYP_VOID, args);
    impAppendTree(callout, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
}

// Checks whether the return types of caller and callee are compatible
// so that callee can be tail called. Note that here we don't check
// compatibility in IL Verifier sense, but on the lines of return type
// sizes are equal and get returned in the same return register.
bool Compiler::impTailCallRetTypeCompatible(var_types                callerRetType,
                                            CORINFO_CLASS_HANDLE     callerRetTypeClass,
                                            CorInfoCallConvExtension callerCallConv,
                                            var_types                calleeRetType,
                                            CORINFO_CLASS_HANDLE     calleeRetTypeClass,
                                            CorInfoCallConvExtension calleeCallConv)
{
    // Note that we can not relax this condition with genActualType() as the
    // calling convention dictates that the caller of a function with a small
    // typed return value is responsible for normalizing the return val.
    if (callerRetType == calleeRetType)
    {
        return true;
    }

    // If the class handles are the same and not null, the return types are compatible.
    if ((callerRetTypeClass != nullptr) && (callerRetTypeClass == calleeRetTypeClass))
    {
        return true;
    }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    // Jit64 compat:
    if (callerRetType == TYP_VOID)
    {
        // This needs to be allowed to support the following IL pattern that Jit64 allows:
        //     tail.call
        //     pop
        //     ret
        //
        // Note that the above IL pattern is not valid as per IL verification rules.
        // Therefore, only full trust code can take advantage of this pattern.
        return true;
    }

    // These checks return true if the return value type sizes are the same and
    // get returned in the same return register i.e. caller doesn't need to normalize
    // return value. Some of the tail calls permitted by below checks would have
    // been rejected by IL Verifier before we reached here.  Therefore, only full
    // trust code can make those tail calls.
    unsigned callerRetTypeSize = 0;
    unsigned calleeRetTypeSize = 0;
    bool isCallerRetTypMBEnreg = VarTypeIsMultiByteAndCanEnreg(callerRetType, callerRetTypeClass, &callerRetTypeSize,
                                                               true, info.compIsVarArgs, callerCallConv);
    bool isCalleeRetTypMBEnreg = VarTypeIsMultiByteAndCanEnreg(calleeRetType, calleeRetTypeClass, &calleeRetTypeSize,
                                                               true, info.compIsVarArgs, calleeCallConv);

    if (varTypeIsIntegral(callerRetType) || isCallerRetTypMBEnreg)
    {
        return (varTypeIsIntegral(calleeRetType) || isCalleeRetTypMBEnreg) && (callerRetTypeSize == calleeRetTypeSize);
    }
#endif // TARGET_AMD64 || TARGET_ARM64

    return false;
}

/********************************************************************************
 *
 * Returns true if the current opcode and and the opcodes following it correspond
 * to a supported tail call IL pattern.
 *
 */
bool Compiler::impIsTailCallILPattern(
    bool tailPrefixed, OPCODE curOpcode, const BYTE* codeAddrOfNextOpcode, const BYTE* codeEnd, bool isRecursive)
{
    // Bail out if the current opcode is not a call.
    if (!impOpcodeIsCallOpcode(curOpcode))
    {
        return false;
    }

#if !FEATURE_TAILCALL_OPT_SHARED_RETURN
    // If shared ret tail opt is not enabled, we will enable
    // it for recursive methods.
    if (isRecursive)
#endif
    {
        // we can actually handle if the ret is in a fallthrough block, as long as that is the only part of the
        // sequence. Make sure we don't go past the end of the IL however.
        codeEnd = min(codeEnd + 1, info.compCode + info.compILCodeSize);
    }

    // Bail out if there is no next opcode after call
    if (codeAddrOfNextOpcode >= codeEnd)
    {
        return false;
    }

    OPCODE nextOpcode = (OPCODE)getU1LittleEndian(codeAddrOfNextOpcode);

    return (nextOpcode == CEE_RET);
}

/*****************************************************************************
 *
 * Determine whether the call could be converted to an implicit tail call
 *
 */
bool Compiler::impIsImplicitTailCallCandidate(
    OPCODE opcode, const BYTE* codeAddrOfNextOpcode, const BYTE* codeEnd, int prefixFlags, bool isRecursive)
{

#if FEATURE_TAILCALL_OPT
    if (!opts.compTailCallOpt)
    {
        return false;
    }

    if (opts.OptimizationDisabled())
    {
        return false;
    }

    // must not be tail prefixed
    if (prefixFlags & PREFIX_TAILCALL_EXPLICIT)
    {
        return false;
    }

#if !FEATURE_TAILCALL_OPT_SHARED_RETURN
    // the block containing call is marked as BBJ_RETURN
    // We allow shared ret tail call optimization on recursive calls even under
    // !FEATURE_TAILCALL_OPT_SHARED_RETURN.
    if (!isRecursive && (compCurBB->bbJumpKind != BBJ_RETURN))
        return false;
#endif // !FEATURE_TAILCALL_OPT_SHARED_RETURN

    // must be call+ret or call+pop+ret
    if (!impIsTailCallILPattern(false, opcode, codeAddrOfNextOpcode, codeEnd, isRecursive))
    {
        return false;
    }

    return true;
#else
    return false;
#endif // FEATURE_TAILCALL_OPT
}

//------------------------------------------------------------------------
// impImportCall: import a call-inspiring opcode
//
// Arguments:
//    opcode                    - opcode that inspires the call
//    pResolvedToken            - resolved token for the call target
//    pConstrainedResolvedToken - resolved constraint token (or nullptr)
//    newObjThis                - tree for this pointer or uninitalized newobj temp (or nullptr)
//    prefixFlags               - IL prefix flags for the call
//    callInfo                  - EE supplied info for the call
//    rawILOffset               - IL offset of the opcode
//
// Returns:
//    Type of the call's return value.
//    If we're importing an inlinee and have realized the inline must fail, the call return type should be TYP_UNDEF.
//    However we can't assert for this here yet because there are cases we miss. See issue #13272.
//
//
// Notes:
//    opcode can be CEE_CALL, CEE_CALLI, CEE_CALLVIRT, or CEE_NEWOBJ.
//
//    For CEE_NEWOBJ, newobjThis should be the temp grabbed for the allocated
//    uninitalized object.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

var_types Compiler::impImportCall(OPCODE                  opcode,
                                  CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                  CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                  GenTree*                newobjThis,
                                  int                     prefixFlags,
                                  CORINFO_CALL_INFO*      callInfo,
                                  IL_OFFSET               rawILOffset)
{
    assert(opcode == CEE_CALL || opcode == CEE_CALLVIRT || opcode == CEE_NEWOBJ || opcode == CEE_CALLI);

    IL_OFFSETX             ilOffset                       = impCurILOffset(rawILOffset, true);
    var_types              callRetTyp                     = TYP_COUNT;
    CORINFO_SIG_INFO*      sig                            = nullptr;
    CORINFO_METHOD_HANDLE  methHnd                        = nullptr;
    CORINFO_CLASS_HANDLE   clsHnd                         = nullptr;
    unsigned               clsFlags                       = 0;
    unsigned               mflags                         = 0;
    GenTree*               call                           = nullptr;
    GenTreeCall::Use*      args                           = nullptr;
    CORINFO_THIS_TRANSFORM constraintCallThisTransform    = CORINFO_NO_THIS_TRANSFORM;
    CORINFO_CONTEXT_HANDLE exactContextHnd                = nullptr;
    bool                   exactContextNeedsRuntimeLookup = false;
    bool                   canTailCall                    = true;
    const char*            szCanTailCallFailReason        = nullptr;
    const int              tailCallFlags                  = (prefixFlags & PREFIX_TAILCALL);
    const bool             isReadonlyCall                 = (prefixFlags & PREFIX_READONLY) != 0;

    CORINFO_RESOLVED_TOKEN* ldftnToken = nullptr;

    // Synchronized methods need to call CORINFO_HELP_MON_EXIT at the end. We could
    // do that before tailcalls, but that is probably not the intended
    // semantic. So just disallow tailcalls from synchronized methods.
    // Also, popping arguments in a varargs function is more work and NYI
    // If we have a security object, we have to keep our frame around for callers
    // to see any imperative security.
    // Reverse P/Invokes need a call to CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT
    // at the end, so tailcalls should be disabled.
    if (info.compFlags & CORINFO_FLG_SYNCH)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is synchronized";
    }
    else if (opts.IsReversePInvoke())
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is Reverse P/Invoke";
    }
#if !FEATURE_FIXED_OUT_ARGS
    else if (info.compIsVarArgs)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is varargs";
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // We only need to cast the return value of pinvoke inlined calls that return small types

    // TODO-AMD64-Cleanup: Remove this when we stop interoperating with JIT64, or if we decide to stop
    // widening everything! CoreCLR does not support JIT64 interoperation so no need to widen there.
    // The existing x64 JIT doesn't bother widening all types to int, so we have to assume for
    // the time being that the callee might be compiled by the other JIT and thus the return
    // value will need to be widened by us (or not widened at all...)

    // ReadyToRun code sticks with default calling convention that does not widen small return types.

    bool checkForSmallType  = opts.IsReadyToRun();
    bool bIntrinsicImported = false;

    CORINFO_SIG_INFO  calliSig;
    GenTreeCall::Use* extraArg = nullptr;

    /*-------------------------------------------------------------------------
     * First create the call node
     */

    if (opcode == CEE_CALLI)
    {
        if (IsTargetAbi(CORINFO_CORERT_ABI))
        {
            // See comment in impCheckForPInvokeCall
            BasicBlock* block = compIsForInlining() ? impInlineInfo->iciBlock : compCurBB;
            if (info.compCompHnd->convertPInvokeCalliToCall(pResolvedToken, !impCanPInvokeInlineCallSite(block)))
            {
                eeGetCallInfo(pResolvedToken, nullptr, CORINFO_CALLINFO_ALLOWINSTPARAM, callInfo);
                return impImportCall(CEE_CALL, pResolvedToken, nullptr, nullptr, prefixFlags, callInfo, rawILOffset);
            }
        }

        /* Get the call site sig */
        eeGetSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, &calliSig);

        callRetTyp = JITtype2varType(calliSig.retType);

        call = impImportIndirectCall(&calliSig, ilOffset);

        // We don't know the target method, so we have to infer the flags, or
        // assume the worst-case.
        mflags = (calliSig.callConv & CORINFO_CALLCONV_HASTHIS) ? 0 : CORINFO_FLG_STATIC;

#ifdef DEBUG
        if (verbose)
        {
            unsigned structSize =
                (callRetTyp == TYP_STRUCT) ? info.compCompHnd->getClassSize(calliSig.retTypeSigClass) : 0;
            printf("\nIn Compiler::impImportCall: opcode is %s, kind=%d, callRetType is %s, structSize is %d\n",
                   opcodeNames[opcode], callInfo->kind, varTypeName(callRetTyp), structSize);
        }
#endif
        // This should be checked in impImportBlockCode.
        assert(!compIsForInlining() || !(impInlineInfo->inlineCandidateInfo->dwRestrictions & INLINE_RESPECT_BOUNDARY));

        sig = &calliSig;
    }
    else // (opcode != CEE_CALLI)
    {
        CorInfoIntrinsics intrinsicID = CORINFO_INTRINSIC_Count;

        // Passing CORINFO_CALLINFO_ALLOWINSTPARAM indicates that this JIT is prepared to
        // supply the instantiation parameters necessary to make direct calls to underlying
        // shared generic code, rather than calling through instantiating stubs.  If the
        // returned signature has CORINFO_CALLCONV_PARAMTYPE then this indicates that the JIT
        // must indeed pass an instantiation parameter.

        methHnd = callInfo->hMethod;

        sig        = &(callInfo->sig);
        callRetTyp = JITtype2varType(sig->retType);

        mflags = callInfo->methodFlags;

#ifdef DEBUG
        if (verbose)
        {
            unsigned structSize = (callRetTyp == TYP_STRUCT) ? info.compCompHnd->getClassSize(sig->retTypeSigClass) : 0;
            printf("\nIn Compiler::impImportCall: opcode is %s, kind=%d, callRetType is %s, structSize is %d\n",
                   opcodeNames[opcode], callInfo->kind, varTypeName(callRetTyp), structSize);
        }
#endif
        if (compIsForInlining())
        {
            /* Does this call site have security boundary restrictions? */

            if (impInlineInfo->inlineCandidateInfo->dwRestrictions & INLINE_RESPECT_BOUNDARY)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLSITE_CROSS_BOUNDARY_SECURITY);
                return TYP_UNDEF;
            }

            /* Does the inlinee use StackCrawlMark */

            if (mflags & CORINFO_FLG_DONT_INLINE_CALLER)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_STACK_CRAWL_MARK);
                return TYP_UNDEF;
            }

            /* For now ignore varargs */
            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_NATIVE_VARARGS);
                return TYP_UNDEF;
            }

            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_MANAGED_VARARGS);
                return TYP_UNDEF;
            }

            if ((mflags & CORINFO_FLG_VIRTUAL) && (sig->sigInst.methInstCount != 0) && (opcode == CEE_CALLVIRT))
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_GENERIC_VIRTUAL);
                return TYP_UNDEF;
            }
        }

        clsHnd = pResolvedToken->hClass;

        clsFlags = callInfo->classFlags;

#ifdef DEBUG
        // If this is a call to JitTestLabel.Mark, do "early inlining", and record the test attribute.

        // This recognition should really be done by knowing the methHnd of the relevant Mark method(s).
        // These should be in corelib.h, and available through a JIT/EE interface call.
        const char* modName;
        const char* className;
        const char* methodName;
        if ((className = eeGetClassName(clsHnd)) != nullptr &&
            strcmp(className, "System.Runtime.CompilerServices.JitTestLabel") == 0 &&
            (methodName = eeGetMethodName(methHnd, &modName)) != nullptr && strcmp(methodName, "Mark") == 0)
        {
            return impImportJitTestLabelMark(sig->numArgs);
        }
#endif // DEBUG

        // <NICE> Factor this into getCallInfo </NICE>
        bool isSpecialIntrinsic = false;
        if ((mflags & (CORINFO_FLG_INTRINSIC | CORINFO_FLG_JIT_INTRINSIC)) != 0)
        {
            const bool isTailCall = canTailCall && (tailCallFlags != 0);

            call = impIntrinsic(newobjThis, clsHnd, methHnd, sig, mflags, pResolvedToken->token, isReadonlyCall,
                                isTailCall, pConstrainedResolvedToken, callInfo->thisTransform, &intrinsicID,
                                &isSpecialIntrinsic);

            if (compDonotInline())
            {
                return TYP_UNDEF;
            }

            if (call != nullptr)
            {
#ifdef FEATURE_READYTORUN_COMPILER
                if (call->OperGet() == GT_INTRINSIC)
                {
                    if (opts.IsReadyToRun())
                    {
                        noway_assert(callInfo->kind == CORINFO_CALL);
                        call->AsIntrinsic()->gtEntryPoint = callInfo->codePointerLookup.constLookup;
                    }
                    else
                    {
                        call->AsIntrinsic()->gtEntryPoint.addr       = nullptr;
                        call->AsIntrinsic()->gtEntryPoint.accessType = IAT_VALUE;
                    }
                }
#endif

                bIntrinsicImported = true;
                goto DONE_CALL;
            }
        }

#ifdef FEATURE_SIMD
        if (featureSIMD)
        {
            call = impSIMDIntrinsic(opcode, newobjThis, clsHnd, methHnd, sig, mflags, pResolvedToken->token);
            if (call != nullptr)
            {
                bIntrinsicImported = true;
                goto DONE_CALL;
            }
        }
#endif // FEATURE_SIMD

        if ((mflags & CORINFO_FLG_VIRTUAL) && (mflags & CORINFO_FLG_EnC) && (opcode == CEE_CALLVIRT))
        {
            NO_WAY("Virtual call to a function added via EnC is not supported");
        }

        if ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_DEFAULT &&
            (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
            (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG)
        {
            BADCODE("Bad calling convention");
        }

        //-------------------------------------------------------------------------
        //  Construct the call node
        //
        // Work out what sort of call we're making.
        // Dispense with virtual calls implemented via LDVIRTFTN immediately.

        constraintCallThisTransform    = callInfo->thisTransform;
        exactContextHnd                = callInfo->contextHandle;
        exactContextNeedsRuntimeLookup = callInfo->exactContextNeedsRuntimeLookup;

        switch (callInfo->kind)
        {

            case CORINFO_VIRTUALCALL_STUB:
            {
                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                if (callInfo->stubLookup.lookupKind.needsRuntimeLookup)
                {
                    if (callInfo->stubLookup.lookupKind.runtimeLookupKind == CORINFO_LOOKUP_NOT_SUPPORTED)
                    {
                        // Runtime does not support inlining of all shapes of runtime lookups
                        // Inlining has to be aborted in such a case
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_COMPLEX_HANDLE);
                        return TYP_UNDEF;
                    }

                    GenTree* stubAddr = impRuntimeLookupToTree(pResolvedToken, &callInfo->stubLookup, methHnd);
                    assert(!compDonotInline());

                    // This is the rough code to set up an indirect stub call
                    assert(stubAddr != nullptr);

                    // The stubAddr may be a
                    // complex expression. As it is evaluated after the args,
                    // it may cause registered args to be spilled. Simply spill it.

                    unsigned lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall with runtime lookup"));
                    impAssignTempGen(lclNum, stubAddr, (unsigned)CHECK_SPILL_NONE);
                    stubAddr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                    // Create the actual call node

                    assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
                           (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);

                    call = gtNewIndCallNode(stubAddr, callRetTyp, nullptr);

                    call->gtFlags |= GTF_EXCEPT | (stubAddr->gtFlags & GTF_GLOB_EFFECT);
                    call->gtFlags |= GTF_CALL_VIRT_STUB;

#ifdef TARGET_X86
                    // No tailcalls allowed for these yet...
                    canTailCall             = false;
                    szCanTailCallFailReason = "VirtualCall with runtime lookup";
#endif
                }
                else
                {
                    // The stub address is known at compile time
                    call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, nullptr, ilOffset);
                    call->AsCall()->gtStubCallStubAddr = callInfo->stubLookup.constLookup.addr;
                    call->gtFlags |= GTF_CALL_VIRT_STUB;
                    assert(callInfo->stubLookup.constLookup.accessType != IAT_PPVALUE &&
                           callInfo->stubLookup.constLookup.accessType != IAT_RELPVALUE);
                    if (callInfo->stubLookup.constLookup.accessType == IAT_PVALUE)
                    {
                        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_VIRTSTUB_REL_INDIRECT;
                    }
                }

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    // Null check is sometimes needed for ready to run to handle
                    // non-virtual <-> virtual changes between versions
                    if (callInfo->nullInstanceCheck)
                    {
                        call->gtFlags |= GTF_CALL_NULLCHECK;
                    }
                }
#endif

                break;
            }

            case CORINFO_VIRTUALCALL_VTABLE:
            {
                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, nullptr, ilOffset);
                call->gtFlags |= GTF_CALL_VIRT_VTABLE;

                // Should we expand virtual call targets early for this method?
                //
                if (opts.compExpandCallsEarly)
                {
                    // Mark this method to expand the virtual call target early in fgMorpgCall
                    call->AsCall()->SetExpandedEarly();
                }
                break;
            }

            case CORINFO_VIRTUALCALL_LDVIRTFTN:
            {
                if (compIsForInlining())
                {
                    compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_CALL_VIA_LDVIRTFTN);
                    return TYP_UNDEF;
                }

                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                // OK, We've been told to call via LDVIRTFTN, so just
                // take the call now....

                GenTreeCall::Use* args = impPopCallArgs(sig->numArgs, sig);

                GenTree* thisPtr = impPopStack().val;
                thisPtr          = impTransformThis(thisPtr, pConstrainedResolvedToken, callInfo->thisTransform);
                assert(thisPtr != nullptr);

                // Clone the (possibly transformed) "this" pointer
                GenTree* thisPtrCopy;
                thisPtr = impCloneExpr(thisPtr, &thisPtrCopy, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("LDVIRTFTN this pointer"));

                GenTree* fptr = impImportLdvirtftn(thisPtr, pResolvedToken, callInfo);
                assert(fptr != nullptr);

                thisPtr = nullptr; // can't reuse it

                // Now make an indirect call through the function pointer

                unsigned lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall through function pointer"));
                impAssignTempGen(lclNum, fptr, (unsigned)CHECK_SPILL_ALL);
                fptr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                // Create the actual call node

                call                          = gtNewIndCallNode(fptr, callRetTyp, args, ilOffset);
                call->AsCall()->gtCallThisArg = gtNewCallArgs(thisPtrCopy);
                call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);

                if ((sig->sigInst.methInstCount != 0) && IsTargetAbi(CORINFO_CORERT_ABI))
                {
                    // CoreRT generic virtual method: need to handle potential fat function pointers
                    addFatPointerCandidate(call->AsCall());
                }
#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    // Null check is needed for ready to run to handle
                    // non-virtual <-> virtual changes between versions
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }
#endif

                // Sine we are jumping over some code, check that its OK to skip that code
                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
                       (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);
                goto DONE;
            }

            case CORINFO_CALL:
            {
                // This is for a non-virtual, non-interface etc. call
                call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, nullptr, ilOffset);

                // We remove the nullcheck for the GetType call intrinsic.
                // TODO-CQ: JIT64 does not introduce the null check for many more helper calls
                // and intrinsics.
                if (callInfo->nullInstanceCheck &&
                    !((mflags & CORINFO_FLG_INTRINSIC) != 0 && (intrinsicID == CORINFO_INTRINSIC_Object_GetType)))
                {
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    call->AsCall()->setEntryPoint(callInfo->codePointerLookup.constLookup);
                }
#endif
                break;
            }

            case CORINFO_CALL_CODE_POINTER:
            {
                // The EE has asked us to call by computing a code pointer and then doing an
                // indirect call.  This is because a runtime lookup is required to get the code entry point.

                // These calls always follow a uniform calling convention, i.e. no extra hidden params
                assert((sig->callConv & CORINFO_CALLCONV_PARAMTYPE) == 0);

                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG);
                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);

                GenTree* fptr =
                    impLookupToTree(pResolvedToken, &callInfo->codePointerLookup, GTF_ICON_FTN_ADDR, callInfo->hMethod);

                if (compDonotInline())
                {
                    return TYP_UNDEF;
                }

                // Now make an indirect call through the function pointer

                unsigned lclNum = lvaGrabTemp(true DEBUGARG("Indirect call through function pointer"));
                impAssignTempGen(lclNum, fptr, (unsigned)CHECK_SPILL_ALL);
                fptr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                call = gtNewIndCallNode(fptr, callRetTyp, nullptr, ilOffset);
                call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);
                if (callInfo->nullInstanceCheck)
                {
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }

                break;
            }

            default:
                assert(!"unknown call kind");
                break;
        }

        //-------------------------------------------------------------------------
        // Set more flags

        PREFIX_ASSUME(call != nullptr);

        if (mflags & CORINFO_FLG_NOGCCHECK)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_NOGCCHECK;
        }

        // Mark call if it's one of the ones we will maybe treat as an intrinsic
        if (isSpecialIntrinsic)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_SPECIAL_INTRINSIC;
        }
    }
    assert(sig);
    assert(clsHnd || (opcode == CEE_CALLI)); // We're never verifying for CALLI, so this is not set.

    /* Some sanity checks */

    // CALL_VIRT and NEWOBJ must have a THIS pointer
    assert((opcode != CEE_CALLVIRT && opcode != CEE_NEWOBJ) || (sig->callConv & CORINFO_CALLCONV_HASTHIS));
    // static bit and hasThis are negations of one another
    assert(((mflags & CORINFO_FLG_STATIC) != 0) == ((sig->callConv & CORINFO_CALLCONV_HASTHIS) == 0));
    assert(call != nullptr);

    /*-------------------------------------------------------------------------
     * Check special-cases etc
     */

    /* Special case - Check if it is a call to Delegate.Invoke(). */

    if (mflags & CORINFO_FLG_DELEGATE_INVOKE)
    {
        assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
        assert(mflags & CORINFO_FLG_FINAL);

        /* Set the delegate flag */
        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_DELEGATE_INV;

        if (callInfo->wrapperDelegateInvoke)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_WRAPPER_DELEGATE_INV;
        }

        if (opcode == CEE_CALLVIRT)
        {
            assert(mflags & CORINFO_FLG_FINAL);

            /* It should have the GTF_CALL_NULLCHECK flag set. Reset it */
            assert(call->gtFlags & GTF_CALL_NULLCHECK);
            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }
    }

    CORINFO_CLASS_HANDLE actualMethodRetTypeSigClass;
    actualMethodRetTypeSigClass = sig->retTypeSigClass;

#if !FEATURE_VARARG
    /* Check for varargs */
    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        BADCODE("Varargs not supported.");
    }
#endif // !FEATURE_VARARG

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        assert(!compIsForInlining());

        /* Set the right flags */

        call->gtFlags |= GTF_CALL_POP_ARGS;
        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_VARARGS;

        /* Can't allow tailcall for varargs as it is caller-pop. The caller
           will be expecting to pop a certain number of arguments, but if we
           tailcall to a function with a different number of arguments, we
           are hosed. There are ways around this (caller remembers esp value,
           varargs is not caller-pop, etc), but not worth it. */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Callee is varargs";
        }
#endif

        /* Get the total number of arguments - this is already correct
         * for CALLI - for methods we have to get it from the call site */

        if (opcode != CEE_CALLI)
        {
#ifdef DEBUG
            unsigned numArgsDef = sig->numArgs;
#endif
            eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, sig);

            // For vararg calls we must be sure to load the return type of the
            // method actually being called, as well as the return types of the
            // specified in the vararg signature. With type equivalency, these types
            // may not be the same.
            if (sig->retTypeSigClass != actualMethodRetTypeSigClass)
            {
                if (actualMethodRetTypeSigClass != nullptr && sig->retType != CORINFO_TYPE_CLASS &&
                    sig->retType != CORINFO_TYPE_BYREF && sig->retType != CORINFO_TYPE_PTR &&
                    sig->retType != CORINFO_TYPE_VAR)
                {
                    // Make sure that all valuetypes (including enums) that we push are loaded.
                    // This is to guarantee that if a GC is triggerred from the prestub of this methods,
                    // all valuetypes in the method signature are already loaded.
                    // We need to be able to find the size of the valuetypes, but we cannot
                    // do a class-load from within GC.
                    info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(actualMethodRetTypeSigClass);
                }
            }

            assert(numArgsDef <= sig->numArgs);
        }

        /* We will have "cookie" as the last argument but we cannot push
         * it on the operand stack because we may overflow, so we append it
         * to the arg list next after we pop them */
    }

    //--------------------------- Inline NDirect ------------------------------

    // For inline cases we technically should look at both the current
    // block and the call site block (or just the latter if we've
    // fused the EH trees). However the block-related checks pertain to
    // EH and we currently won't inline a method with EH. So for
    // inlinees, just checking the call site block is sufficient.
    {
        // New lexical block here to avoid compilation errors because of GOTOs.
        BasicBlock* block = compIsForInlining() ? impInlineInfo->iciBlock : compCurBB;
        impCheckForPInvokeCall(call->AsCall(), methHnd, sig, mflags, block);
    }

#ifdef UNIX_X86_ABI
    // On Unix x86 we use caller-cleaned convention.
    if ((call->gtFlags & GTF_CALL_UNMANAGED) == 0)
        call->gtFlags |= GTF_CALL_POP_ARGS;
#endif // UNIX_X86_ABI

    if (call->gtFlags & GTF_CALL_UNMANAGED)
    {
        // We set up the unmanaged call by linking the frame, disabling GC, etc
        // This needs to be cleaned up on return
        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Callee is native";
        }

        checkForSmallType = true;

        impPopArgsForUnmanagedCall(call, sig);

        goto DONE;
    }
    else if ((opcode == CEE_CALLI) && ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_DEFAULT) &&
             ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG))
    {
        if (!info.compCompHnd->canGetCookieForPInvokeCalliSig(sig))
        {
            // Normally this only happens with inlining.
            // However, a generic method (or type) being NGENd into another module
            // can run into this issue as well.  There's not an easy fall-back for NGEN
            // so instead we fallback to JIT.
            if (compIsForInlining())
            {
                compInlineResult->NoteFatal(InlineObservation::CALLSITE_CANT_EMBED_PINVOKE_COOKIE);
            }
            else
            {
                IMPL_LIMITATION("Can't get PInvoke cookie (cross module generics)");
            }

            return TYP_UNDEF;
        }

        GenTree* cookie = eeGetPInvokeCookie(sig);

        // This cookie is required to be either a simple GT_CNS_INT or
        // an indirection of a GT_CNS_INT
        //
        GenTree* cookieConst = cookie;
        if (cookie->gtOper == GT_IND)
        {
            cookieConst = cookie->AsOp()->gtOp1;
        }
        assert(cookieConst->gtOper == GT_CNS_INT);

        // Setting GTF_DONT_CSE on the GT_CNS_INT as well as on the GT_IND (if it exists) will ensure that
        // we won't allow this tree to participate in any CSE logic
        //
        cookie->gtFlags |= GTF_DONT_CSE;
        cookieConst->gtFlags |= GTF_DONT_CSE;

        call->AsCall()->gtCallCookie = cookie;

        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "PInvoke calli";
        }
    }

    /*-------------------------------------------------------------------------
     * Create the argument list
     */

    //-------------------------------------------------------------------------
    // Special case - for varargs we have an implicit last argument

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        assert(!compIsForInlining());

        void *varCookie, *pVarCookie;
        if (!info.compCompHnd->canGetVarArgsHandle(sig))
        {
            compInlineResult->NoteFatal(InlineObservation::CALLSITE_CANT_EMBED_VARARGS_COOKIE);
            return TYP_UNDEF;
        }

        varCookie = info.compCompHnd->getVarArgsHandle(sig, &pVarCookie);
        assert((!varCookie) != (!pVarCookie));
        GenTree* cookie = gtNewIconEmbHndNode(varCookie, pVarCookie, GTF_ICON_VARG_HDL, sig);

        assert(extraArg == nullptr);
        extraArg = gtNewCallArgs(cookie);
    }

    //-------------------------------------------------------------------------
    // Extra arg for shared generic code and array methods
    //
    // Extra argument containing instantiation information is passed in the
    // following circumstances:
    // (a) To the "Address" method on array classes; the extra parameter is
    //     the array's type handle (a TypeDesc)
    // (b) To shared-code instance methods in generic structs; the extra parameter
    //     is the struct's type handle (a vtable ptr)
    // (c) To shared-code per-instantiation non-generic static methods in generic
    //     classes and structs; the extra parameter is the type handle
    // (d) To shared-code generic methods; the extra parameter is an
    //     exact-instantiation MethodDesc
    //
    // We also set the exact type context associated with the call so we can
    // inline the call correctly later on.

    if (sig->callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        assert(call->AsCall()->gtCallType == CT_USER_FUNC);
        if (clsHnd == nullptr)
        {
            NO_WAY("CALLI on parameterized type");
        }

        assert(opcode != CEE_CALLI);

        GenTree* instParam;
        bool     runtimeLookup;

        // Instantiated generic method
        if (((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
        {
            assert(exactContextHnd != METHOD_BEING_COMPILED_CONTEXT());

            CORINFO_METHOD_HANDLE exactMethodHandle =
                (CORINFO_METHOD_HANDLE)((SIZE_T)exactContextHnd & ~CORINFO_CONTEXTFLAGS_MASK);

            if (!exactContextNeedsRuntimeLookup)
            {
#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    instParam =
                        impReadyToRunLookupToTree(&callInfo->instParamLookup, GTF_ICON_METHOD_HDL, exactMethodHandle);
                    if (instParam == nullptr)
                    {
                        assert(compDonotInline());
                        return TYP_UNDEF;
                    }
                }
                else
#endif
                {
                    instParam = gtNewIconEmbMethHndNode(exactMethodHandle);
                    info.compCompHnd->methodMustBeLoadedBeforeCodeIsRun(exactMethodHandle);
                }
            }
            else
            {
                instParam = impTokenToHandle(pResolvedToken, &runtimeLookup, true /*mustRestoreHandle*/);
                if (instParam == nullptr)
                {
                    assert(compDonotInline());
                    return TYP_UNDEF;
                }
            }
        }

        // otherwise must be an instance method in a generic struct,
        // a static method in a generic type, or a runtime-generated array method
        else
        {
            assert(((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
            CORINFO_CLASS_HANDLE exactClassHandle = eeGetClassFromContext(exactContextHnd);

            if (compIsForInlining() && (clsFlags & CORINFO_FLG_ARRAY) != 0)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_ARRAY_METHOD);
                return TYP_UNDEF;
            }

            if ((clsFlags & CORINFO_FLG_ARRAY) && isReadonlyCall)
            {
                // We indicate "readonly" to the Address operation by using a null
                // instParam.
                instParam = gtNewIconNode(0, TYP_REF);
            }
            else if (!exactContextNeedsRuntimeLookup)
            {
#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    instParam =
                        impReadyToRunLookupToTree(&callInfo->instParamLookup, GTF_ICON_CLASS_HDL, exactClassHandle);
                    if (instParam == nullptr)
                    {
                        assert(compDonotInline());
                        return TYP_UNDEF;
                    }
                }
                else
#endif
                {
                    instParam = gtNewIconEmbClsHndNode(exactClassHandle);
                    info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(exactClassHandle);
                }
            }
            else
            {
                // If the EE was able to resolve a constrained call, the instantiating parameter to use is the type
                // by which the call was constrained with. We embed pConstrainedResolvedToken as the extra argument
                // because pResolvedToken is an interface method and interface types make a poor generic context.
                if (pConstrainedResolvedToken)
                {
                    instParam = impTokenToHandle(pConstrainedResolvedToken, &runtimeLookup, true /*mustRestoreHandle*/,
                                                 false /* importParent */);
                }
                else
                {
                    instParam = impParentClassTokenToHandle(pResolvedToken, &runtimeLookup, true /*mustRestoreHandle*/);
                }

                if (instParam == nullptr)
                {
                    assert(compDonotInline());
                    return TYP_UNDEF;
                }
            }
        }

        assert(extraArg == nullptr);
        extraArg = gtNewCallArgs(instParam);
    }

    if ((opcode == CEE_NEWOBJ) && ((clsFlags & CORINFO_FLG_DELEGATE) != 0))
    {
        // Only verifiable cases are supported.
        // dup; ldvirtftn; newobj; or ldftn; newobj.
        // IL test could contain unverifiable sequence, in this case optimization should not be done.
        if (impStackHeight() > 0)
        {
            typeInfo delegateTypeInfo = impStackTop().seTypeInfo;
            if (delegateTypeInfo.IsToken())
            {
                ldftnToken = delegateTypeInfo.GetToken();
            }
        }
    }

    //-------------------------------------------------------------------------
    // The main group of arguments

    args                       = impPopCallArgs(sig->numArgs, sig, extraArg);
    call->AsCall()->gtCallArgs = args;

    for (GenTreeCall::Use& use : call->AsCall()->Args())
    {
        call->gtFlags |= use.GetNode()->gtFlags & GTF_GLOB_EFFECT;
    }

    //-------------------------------------------------------------------------
    // The "this" pointer

    if (((mflags & CORINFO_FLG_STATIC) == 0) && ((sig->callConv & CORINFO_CALLCONV_EXPLICITTHIS) == 0) &&
        !((opcode == CEE_NEWOBJ) && (newobjThis == nullptr)))
    {
        GenTree* obj;

        if (opcode == CEE_NEWOBJ)
        {
            obj = newobjThis;
        }
        else
        {
            obj = impPopStack().val;
            obj = impTransformThis(obj, pConstrainedResolvedToken, constraintCallThisTransform);
            if (compDonotInline())
            {
                return TYP_UNDEF;
            }
        }

        // Store the "this" value in the call
        call->gtFlags |= obj->gtFlags & GTF_GLOB_EFFECT;
        call->AsCall()->gtCallThisArg = gtNewCallArgs(obj);

        // Is this a virtual or interface call?
        if (call->AsCall()->IsVirtual())
        {
            // only true object pointers can be virtual
            assert(obj->gtType == TYP_REF);

            // See if we can devirtualize.

            const bool isExplicitTailCall     = (tailCallFlags & PREFIX_TAILCALL_EXPLICIT) != 0;
            const bool isLateDevirtualization = false;
            impDevirtualizeCall(call->AsCall(), &callInfo->hMethod, &callInfo->methodFlags, &callInfo->contextHandle,
                                &exactContextHnd, isLateDevirtualization, isExplicitTailCall, rawILOffset);
        }

        if (impIsThis(obj))
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_NONVIRT_SAME_THIS;
        }
    }

    //-------------------------------------------------------------------------
    // The "this" pointer for "newobj"

    if (opcode == CEE_NEWOBJ)
    {
        if (clsFlags & CORINFO_FLG_VAROBJSIZE)
        {
            assert(!(clsFlags & CORINFO_FLG_ARRAY)); // arrays handled separately
            // This is a 'new' of a variable sized object, wher
            // the constructor is to return the object.  In this case
            // the constructor claims to return VOID but we know it
            // actually returns the new object
            assert(callRetTyp == TYP_VOID);
            callRetTyp   = TYP_REF;
            call->gtType = TYP_REF;
            impSpillSpecialSideEff();

            impPushOnStack(call, typeInfo(TI_REF, clsHnd));
        }
        else
        {
            if (clsFlags & CORINFO_FLG_DELEGATE)
            {
                // New inliner morph it in impImportCall.
                // This will allow us to inline the call to the delegate constructor.
                call = fgOptimizeDelegateConstructor(call->AsCall(), &exactContextHnd, ldftnToken);
            }

            if (!bIntrinsicImported)
            {

#if defined(DEBUG) || defined(INLINE_DATA)

                // Keep track of the raw IL offset of the call
                call->AsCall()->gtRawILOffset = rawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

                // Is it an inline candidate?
                impMarkInlineCandidate(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo);
            }

            // append the call node.
            impAppendTree(call, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);

            // Now push the value of the 'new onto the stack

            // This is a 'new' of a non-variable sized object.
            // Append the new node (op1) to the statement list,
            // and then push the local holding the value of this
            // new instruction on the stack.

            if (clsFlags & CORINFO_FLG_VALUECLASS)
            {
                assert(newobjThis->gtOper == GT_ADDR && newobjThis->AsOp()->gtOp1->gtOper == GT_LCL_VAR);

                unsigned tmp = newobjThis->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
                impPushOnStack(gtNewLclvNode(tmp, lvaGetRealType(tmp)), verMakeTypeInfo(clsHnd).NormaliseForStack());
            }
            else
            {
                if (newobjThis->gtOper == GT_COMMA)
                {
                    // In coreclr the callout can be inserted even if verification is disabled
                    // so we cannot rely on tiVerificationNeeded alone

                    // We must have inserted the callout. Get the real newobj.
                    newobjThis = newobjThis->AsOp()->gtOp2;
                }

                assert(newobjThis->gtOper == GT_LCL_VAR);
                impPushOnStack(gtNewLclvNode(newobjThis->AsLclVarCommon()->GetLclNum(), TYP_REF),
                               typeInfo(TI_REF, clsHnd));
            }
        }
        return callRetTyp;
    }

DONE:

#ifdef DEBUG
    // In debug we want to be able to register callsites with the EE.
    assert(call->AsCall()->callSig == nullptr);
    call->AsCall()->callSig  = new (this, CMK_Generic) CORINFO_SIG_INFO;
    *call->AsCall()->callSig = *sig;
#endif

    // Final importer checks for calls flagged as tail calls.
    //
    if (tailCallFlags != 0)
    {
        const bool isExplicitTailCall = (tailCallFlags & PREFIX_TAILCALL_EXPLICIT) != 0;
        const bool isImplicitTailCall = (tailCallFlags & PREFIX_TAILCALL_IMPLICIT) != 0;
        const bool isStressTailCall   = (tailCallFlags & PREFIX_TAILCALL_STRESS) != 0;

        // Exactly one of these should be true.
        assert(isExplicitTailCall != isImplicitTailCall);

        // This check cannot be performed for implicit tail calls for the reason
        // that impIsImplicitTailCallCandidate() is not checking whether return
        // types are compatible before marking a call node with PREFIX_TAILCALL_IMPLICIT.
        // As a result it is possible that in the following case, we find that
        // the type stack is non-empty if Callee() is considered for implicit
        // tail calling.
        //      int Caller(..) { .... void Callee(); ret val; ... }
        //
        // Note that we cannot check return type compatibility before ImpImportCall()
        // as we don't have required info or need to duplicate some of the logic of
        // ImpImportCall().
        //
        // For implicit tail calls, we perform this check after return types are
        // known to be compatible.
        if (isExplicitTailCall && (verCurrentState.esStackDepth != 0))
        {
            BADCODE("Stack should be empty after tailcall");
        }

        // Note that we can not relax this condition with genActualType() as
        // the calling convention dictates that the caller of a function with
        // a small-typed return value is responsible for normalizing the return val

        if (canTailCall &&
            !impTailCallRetTypeCompatible(info.compRetType, info.compMethodInfo->args.retTypeClass, info.compCallConv,
                                          callRetTyp, sig->retTypeClass, call->AsCall()->GetUnmanagedCallConv()))
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Return types are not tail call compatible";
        }

        // Stack empty check for implicit tail calls.
        if (canTailCall && isImplicitTailCall && (verCurrentState.esStackDepth != 0))
        {
#ifdef TARGET_AMD64
            // JIT64 Compatibility:  Opportunistic tail call stack mismatch throws a VerificationException
            // in JIT64, not an InvalidProgramException.
            Verify(false, "Stack should be empty after tailcall");
#else  // TARGET_64BIT
            BADCODE("Stack should be empty after tailcall");
#endif //! TARGET_64BIT
        }

        // assert(compCurBB is not a catch, finally or filter block);
        // assert(compCurBB is not a try block protected by a finally block);
        assert(!isExplicitTailCall || compCurBB->bbJumpKind == BBJ_RETURN);

        // Ask VM for permission to tailcall
        if (canTailCall)
        {
            // True virtual or indirect calls, shouldn't pass in a callee handle.
            CORINFO_METHOD_HANDLE exactCalleeHnd =
                ((call->AsCall()->gtCallType != CT_USER_FUNC) || call->AsCall()->IsVirtual()) ? nullptr : methHnd;

            if (info.compCompHnd->canTailCall(info.compMethodHnd, methHnd, exactCalleeHnd, isExplicitTailCall))
            {
                if (isExplicitTailCall)
                {
                    // In case of explicit tail calls, mark it so that it is not considered
                    // for in-lining.
                    call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_EXPLICIT_TAILCALL;
                    JITDUMP("\nGTF_CALL_M_EXPLICIT_TAILCALL set for call [%06u]\n", dspTreeID(call));

                    if (isStressTailCall)
                    {
                        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_STRESS_TAILCALL;
                        JITDUMP("\nGTF_CALL_M_STRESS_TAILCALL set for call [%06u]\n", dspTreeID(call));
                    }
                }
                else
                {
#if FEATURE_TAILCALL_OPT
                    // Must be an implicit tail call.
                    assert(isImplicitTailCall);

                    // It is possible that a call node is both an inline candidate and marked
                    // for opportunistic tail calling.  In-lining happens before morhphing of
                    // trees.  If in-lining of an in-line candidate gets aborted for whatever
                    // reason, it will survive to the morphing stage at which point it will be
                    // transformed into a tail call after performing additional checks.

                    call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_IMPLICIT_TAILCALL;
                    JITDUMP("\nGTF_CALL_M_IMPLICIT_TAILCALL set for call [%06u]\n", dspTreeID(call));

#else //! FEATURE_TAILCALL_OPT
                    NYI("Implicit tail call prefix on a target which doesn't support opportunistic tail calls");

#endif // FEATURE_TAILCALL_OPT
                }

                // This might or might not turn into a tailcall. We do more
                // checks in morph. For explicit tailcalls we need more
                // information in morph in case it turns out to be a
                // helper-based tailcall.
                if (isExplicitTailCall)
                {
                    assert(call->AsCall()->tailCallInfo == nullptr);
                    call->AsCall()->tailCallInfo = new (this, CMK_CorTailCallInfo) TailCallSiteInfo;
                    switch (opcode)
                    {
                        case CEE_CALLI:
                            call->AsCall()->tailCallInfo->SetCalli(sig);
                            break;
                        case CEE_CALLVIRT:
                            call->AsCall()->tailCallInfo->SetCallvirt(sig, pResolvedToken);
                            break;
                        default:
                            call->AsCall()->tailCallInfo->SetCall(sig, pResolvedToken);
                            break;
                    }
                }
            }
            else
            {
                // canTailCall reported its reasons already
                canTailCall = false;
                JITDUMP("\ninfo.compCompHnd->canTailCall returned false for call [%06u]\n", dspTreeID(call));
            }
        }
        else
        {
            // If this assert fires it means that canTailCall was set to false without setting a reason!
            assert(szCanTailCallFailReason != nullptr);
            JITDUMP("\nRejecting %splicit tail call for [%06u], reason: '%s'\n", isExplicitTailCall ? "ex" : "im",
                    dspTreeID(call), szCanTailCallFailReason);
            info.compCompHnd->reportTailCallDecision(info.compMethodHnd, methHnd, isExplicitTailCall, TAILCALL_FAIL,
                                                     szCanTailCallFailReason);
        }
    }

    // A tail recursive call is a potential loop from the current block to the start of the method.
    if ((tailCallFlags != 0) && canTailCall && gtIsRecursiveCall(methHnd))
    {
        assert(verCurrentState.esStackDepth == 0);
        BasicBlock* loopHead = nullptr;
        if (opts.IsOSR())
        {
            // We might not have been planning on importing the method
            // entry block, but now we must.

            // We should have remembered the real method entry block.
            assert(fgEntryBB != nullptr);

            JITDUMP("\nOSR: found tail recursive call in the method, scheduling " FMT_BB " for importation\n",
                    fgEntryBB->bbNum);
            impImportBlockPending(fgEntryBB);
            loopHead = fgEntryBB;
        }
        else
        {
            // For normal jitting we'll branch back to the firstBB; this
            // should already be imported.
            loopHead = fgFirstBB;
        }

        JITDUMP("\nFound tail recursive call in the method. Mark " FMT_BB " to " FMT_BB
                " as having a backward branch.\n",
                loopHead->bbNum, compCurBB->bbNum);
        fgMarkBackwardJump(loopHead, compCurBB);
    }

    // Note: we assume that small return types are already normalized by the managed callee
    // or by the pinvoke stub for calls to unmanaged code.

    if (!bIntrinsicImported)
    {
        //
        // Things needed to be checked when bIntrinsicImported is false.
        //

        assert(call->gtOper == GT_CALL);
        assert(callInfo != nullptr);

        if (compIsForInlining() && opcode == CEE_CALLVIRT)
        {
            GenTree* callObj = call->AsCall()->gtCallThisArg->GetNode();

            if ((call->AsCall()->IsVirtual() || (call->gtFlags & GTF_CALL_NULLCHECK)) &&
                impInlineIsGuaranteedThisDerefBeforeAnySideEffects(nullptr, call->AsCall()->gtCallArgs, callObj,
                                                                   impInlineInfo->inlArgInfo))
            {
                impInlineInfo->thisDereferencedFirst = true;
            }
        }

#if defined(DEBUG) || defined(INLINE_DATA)

        // Keep track of the raw IL offset of the call
        call->AsCall()->gtRawILOffset = rawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

        // Is it an inline candidate?
        impMarkInlineCandidate(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo);
    }

    if ((sig->flags & CORINFO_SIGFLAG_FAT_CALL) != 0)
    {
        assert(opcode == CEE_CALLI);
        addFatPointerCandidate(call->AsCall());
    }

DONE_CALL:
    // Push or append the result of the call
    if (callRetTyp == TYP_VOID)
    {
        if (opcode == CEE_NEWOBJ)
        {
            // we actually did push something, so don't spill the thing we just pushed.
            assert(verCurrentState.esStackDepth > 0);
            impAppendTree(call, verCurrentState.esStackDepth - 1, impCurStmtOffs);
        }
        else
        {
            impAppendTree(call, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
        }
    }
    else
    {
        impSpillSpecialSideEff();

        if (clsFlags & CORINFO_FLG_ARRAY)
        {
            eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, sig);
        }

        // Find the return type used for verification by interpreting the method signature.
        // NB: we are clobbering the already established sig.
        if (tiVerificationNeeded)
        {
            // Actually, we never get the sig for the original method.
            sig = &(callInfo->verSig);
        }

        typeInfo tiRetVal = verMakeTypeInfo(sig->retType, sig->retTypeClass);
        tiRetVal.NormaliseForStack();

        // The CEE_READONLY prefix modifies the verification semantics of an Address
        // operation on an array type.
        if ((clsFlags & CORINFO_FLG_ARRAY) && isReadonlyCall && tiRetVal.IsByRef())
        {
            tiRetVal.SetIsReadonlyByRef();
        }

        if (tiVerificationNeeded)
        {
            // We assume all calls return permanent home byrefs. If they
            // didn't they wouldn't be verifiable. This is also covering
            // the Address() helper for multidimensional arrays.
            if (tiRetVal.IsByRef())
            {
                tiRetVal.SetIsPermanentHomeByRef();
            }
        }

        if (call->IsCall())
        {
            // Sometimes "call" is not a GT_CALL (if we imported an intrinsic that didn't turn into a call)

            GenTreeCall* origCall = call->AsCall();

            const bool isFatPointerCandidate              = origCall->IsFatPointerCandidate();
            const bool isInlineCandidate                  = origCall->IsInlineCandidate();
            const bool isGuardedDevirtualizationCandidate = origCall->IsGuardedDevirtualizationCandidate();

            if (varTypeIsStruct(callRetTyp))
            {
                // Need to treat all "split tree" cases here, not just inline candidates
                call = impFixupCallStructReturn(call->AsCall(), sig->retTypeClass);
            }

            // TODO: consider handling fatcalli cases this way too...?
            if (isInlineCandidate || isGuardedDevirtualizationCandidate)
            {
                // We should not have made any adjustments in impFixupCallStructReturn
                // as we defer those until we know the fate of the call.
                assert(call == origCall);

                assert(opts.OptEnabled(CLFLG_INLINING));
                assert(!isFatPointerCandidate); // We should not try to inline calli.

                // Make the call its own tree (spill the stack if needed).
                impAppendTree(call, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);

                // TODO: Still using the widened type.
                GenTree* retExpr = gtNewInlineCandidateReturnExpr(call, genActualType(callRetTyp), compCurBB->bbFlags);

                // Link the retExpr to the call so if necessary we can manipulate it later.
                origCall->gtInlineCandidateInfo->retExpr = retExpr;

                // Propagate retExpr as the placeholder for the call.
                call = retExpr;
            }
            else
            {
                if (isFatPointerCandidate)
                {
                    // fatPointer candidates should be in statements of the form call() or var = call().
                    // Such form allows to find statements with fat calls without walking through whole trees
                    // and removes problems with cutting trees.
                    assert(!bIntrinsicImported);
                    assert(IsTargetAbi(CORINFO_CORERT_ABI));
                    if (call->OperGet() != GT_LCL_VAR) // can be already converted by impFixupCallStructReturn.
                    {
                        unsigned   calliSlot  = lvaGrabTemp(true DEBUGARG("calli"));
                        LclVarDsc* varDsc     = &lvaTable[calliSlot];
                        varDsc->lvVerTypeInfo = tiRetVal;
                        impAssignTempGen(calliSlot, call, tiRetVal.GetClassHandle(), (unsigned)CHECK_SPILL_NONE);
                        // impAssignTempGen can change src arg list and return type for call that returns struct.
                        var_types type = genActualType(lvaTable[calliSlot].TypeGet());
                        call           = gtNewLclvNode(calliSlot, type);
                    }
                }

                // For non-candidates we must also spill, since we
                // might have locals live on the eval stack that this
                // call can modify.
                //
                // Suppress this for certain well-known call targets
                // that we know won't modify locals, eg calls that are
                // recognized in gtCanOptimizeTypeEquality. Otherwise
                // we may break key fragile pattern matches later on.
                bool spillStack = true;
                if (call->IsCall())
                {
                    GenTreeCall* callNode = call->AsCall();
                    if ((callNode->gtCallType == CT_HELPER) && (gtIsTypeHandleToRuntimeTypeHelper(callNode) ||
                                                                gtIsTypeHandleToRuntimeTypeHandleHelper(callNode)))
                    {
                        spillStack = false;
                    }
                    else if ((callNode->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) != 0)
                    {
                        spillStack = false;
                    }
                }

                if (spillStack)
                {
                    impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("non-inline candidate call"));
                }
            }
        }

        if (!bIntrinsicImported)
        {
            //-------------------------------------------------------------------------
            //
            /* If the call is of a small type and the callee is managed, the callee will normalize the result
                before returning.
                However, we need to normalize small type values returned by unmanaged
                functions (pinvoke). The pinvoke stub does the normalization, but we need to do it here
                if we use the shorter inlined pinvoke stub. */

            if (checkForSmallType && varTypeIsIntegral(callRetTyp) && genTypeSize(callRetTyp) < genTypeSize(TYP_INT))
            {
                call = gtNewCastNode(genActualType(callRetTyp), call, false, callRetTyp);
            }
        }

        impPushOnStack(call, tiRetVal);
    }

    // VSD functions get a new call target each time we getCallInfo, so clear the cache.
    // Also, the call info cache for CALLI instructions is largely incomplete, so clear it out.
    // if ( (opcode == CEE_CALLI) || (callInfoCache.fetchCallInfo().kind == CORINFO_VIRTUALCALL_STUB))
    //  callInfoCache.uncacheCallInfo();

    return callRetTyp;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

bool Compiler::impMethodInfo_hasRetBuffArg(CORINFO_METHOD_INFO* methInfo, CorInfoCallConvExtension callConv)
{
    CorInfoType corType = methInfo->args.retType;

    if ((corType == CORINFO_TYPE_VALUECLASS) || (corType == CORINFO_TYPE_REFANY))
    {
        // We have some kind of STRUCT being returned
        structPassingKind howToReturnStruct = SPK_Unknown;

        var_types returnType = getReturnTypeForStruct(methInfo->args.retTypeClass, callConv, &howToReturnStruct);

        if (howToReturnStruct == SPK_ByReference)
        {
            return true;
        }
    }

    return false;
}

#ifdef DEBUG
//
var_types Compiler::impImportJitTestLabelMark(int numArgs)
{
    TestLabelAndNum tlAndN;
    if (numArgs == 2)
    {
        tlAndN.m_num  = 0;
        StackEntry se = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        GenTree* val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_tl = (TestLabel)val->AsIntConCommon()->IconValue();
    }
    else if (numArgs == 3)
    {
        StackEntry se = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        GenTree* val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_num = val->AsIntConCommon()->IconValue();
        se           = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_tl = (TestLabel)val->AsIntConCommon()->IconValue();
    }
    else
    {
        assert(false);
    }

    StackEntry expSe = impPopStack();
    GenTree*   node  = expSe.val;

    // There are a small number of special cases, where we actually put the annotation on a subnode.
    if (tlAndN.m_tl == TL_LoopHoist && tlAndN.m_num >= 100)
    {
        // A loop hoist annotation with value >= 100 means that the expression should be a static field access,
        // a GT_IND of a static field address, which should be the sum of a (hoistable) helper call and possibly some
        // offset within the the static field block whose address is returned by the helper call.
        // The annotation is saying that this address calculation, but not the entire access, should be hoisted.
        assert(node->OperGet() == GT_IND);
        tlAndN.m_num -= 100;
        GetNodeTestData()->Set(node->AsOp()->gtOp1, tlAndN);
        GetNodeTestData()->Remove(node);
    }
    else
    {
        GetNodeTestData()->Set(node, tlAndN);
    }

    impPushOnStack(node, expSe.seTypeInfo);
    return node->TypeGet();
}
#endif // DEBUG

//-----------------------------------------------------------------------------------
//  impFixupCallStructReturn: For a call node that returns a struct type either
//  adjust the return type to an enregisterable type, or set the flag to indicate
//  struct return via retbuf arg.
//
//  Arguments:
//    call       -  GT_CALL GenTree node
//    retClsHnd  -  Class handle of return type of the call
//
//  Return Value:
//    Returns new GenTree node after fixing struct return of call node
//
GenTree* Compiler::impFixupCallStructReturn(GenTreeCall* call, CORINFO_CLASS_HANDLE retClsHnd)
{
    if (!varTypeIsStruct(call))
    {
        return call;
    }

    call->gtRetClsHnd = retClsHnd;

#if FEATURE_MULTIREG_RET
    call->InitializeStructReturnType(this, retClsHnd, call->GetUnmanagedCallConv());
#endif // FEATURE_MULTIREG_RET

#ifdef UNIX_AMD64_ABI

    // Not allowed for FEATURE_CORCLR which is the only SKU available for System V OSs.
    assert(!call->IsVarargs() && "varargs not allowed for System V OSs.");

    const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    const unsigned        retRegCount = retTypeDesc->GetReturnRegCount();
    if (retRegCount == 0)
    {
        // struct not returned in registers i.e returned via hiddden retbuf arg.
        call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;
    }
    else if (retRegCount == 1)
    {
        return call;
    }
    else
    {
        // must be a struct returned in two registers
        assert(retRegCount == 2);

        if ((!call->CanTailCall()) && (!call->IsInlineCandidate()))
        {
            // Force a call returning multi-reg struct to be always of the IR form
            //   tmp = call
            //
            // No need to assign a multi-reg struct to a local var if:
            //  - It is a tail call or
            //  - The call is marked for in-lining later
            return impAssignMultiRegTypeToVar(call, retClsHnd DEBUGARG(call->GetUnmanagedCallConv()));
        }
    }

#else // not UNIX_AMD64_ABI

    // Check for TYP_STRUCT type that wraps a primitive type
    // Such structs are returned using a single register
    // and we change the return type on those calls here.
    //
    structPassingKind howToReturnStruct;
    var_types         returnType;

    returnType = getReturnTypeForStruct(retClsHnd, call->GetUnmanagedCallConv(), &howToReturnStruct);

    if (howToReturnStruct == SPK_ByReference)
    {
        assert(returnType == TYP_UNKNOWN);
        call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;
    }
    else
    {

#if !FEATURE_MULTIREG_RET
        return call;
#else  // FEATURE_MULTIREG_RET
        const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
        const unsigned        retRegCount = retTypeDesc->GetReturnRegCount();
        assert(retRegCount != 0);
        if (retRegCount == 1)
        {
            return call;
        }

        assert(returnType == TYP_STRUCT);
        assert((howToReturnStruct == SPK_ByValueAsHfa) || (howToReturnStruct == SPK_ByValue));

        assert(call->gtReturnType == returnType);

        assert(retRegCount >= 2);
        if ((!call->CanTailCall()) && (!call->IsInlineCandidate()))
        {
            // Force a call returning multi-reg struct to be always of the IR form
            //   tmp = call
            //
            // No need to assign a multi-reg struct to a local var if:
            //  - It is a tail call or
            //  - The call is marked for in-lining later
            return impAssignMultiRegTypeToVar(call, retClsHnd DEBUGARG(call->GetUnmanagedCallConv()));
        }
#endif // FEATURE_MULTIREG_RET
    }
#endif // not UNIX_AMD64_ABI

    return call;
}

/*****************************************************************************
   For struct return values, re-type the operand in the case where the ABI
   does not use a struct return buffer
 */

//------------------------------------------------------------------------
// impFixupStructReturnType: For struct return values it sets appropriate flags in MULTIREG returns case;
//   in non-multiref case it handles two special helpers: `CORINFO_HELP_GETFIELDSTRUCT`, `CORINFO_HELP_UNBOX_NULLABLE`.
//
// Arguments:
//    op - the return value;
//    retClsHnd - the struct handle;
//    unmgdCallConv - the calling convention of the function that returns this struct.
//
// Return Value:
//    the result tree that does the return.
//
GenTree* Compiler::impFixupStructReturnType(GenTree*                 op,
                                            CORINFO_CLASS_HANDLE     retClsHnd,
                                            CorInfoCallConvExtension unmgdCallConv)
{
    assert(varTypeIsStruct(info.compRetType));
    assert(info.compRetBuffArg == BAD_VAR_NUM);

    JITDUMP("\nimpFixupStructReturnType: retyping\n");
    DISPTREE(op);

#if defined(TARGET_XARCH)

#if FEATURE_MULTIREG_RET
    // No VarArgs for CoreCLR on x64 Unix
    UNIX_AMD64_ABI_ONLY(assert(!info.compIsVarArgs));

    // Is method returning a multi-reg struct?
    if (varTypeIsStruct(info.compRetNativeType) && IsMultiRegReturnedType(retClsHnd, unmgdCallConv))
    {
        // In case of multi-reg struct return, we force IR to be one of the following:
        // GT_RETURN(lclvar) or GT_RETURN(call).  If op is anything other than a
        // lclvar or call, it is assigned to a temp to create: temp = op and GT_RETURN(tmp).

        if (op->gtOper == GT_LCL_VAR)
        {
            // Note that this is a multi-reg return.
            unsigned lclNum                  = op->AsLclVarCommon()->GetLclNum();
            lvaTable[lclNum].lvIsMultiRegRet = true;

            // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
            op->gtFlags |= GTF_DONT_CSE;

            return op;
        }

        if (op->gtOper == GT_CALL)
        {
            return op;
        }

        return impAssignMultiRegTypeToVar(op, retClsHnd DEBUGARG(unmgdCallConv));
    }
#else
    assert(info.compRetNativeType != TYP_STRUCT);
#endif // defined(UNIX_AMD64_ABI) || defined(TARGET_X86)

#elif FEATURE_MULTIREG_RET && defined(TARGET_ARM)

    if (varTypeIsStruct(info.compRetNativeType) && !info.compIsVarArgs && IsHfa(retClsHnd))
    {
        if (op->gtOper == GT_LCL_VAR)
        {
            // This LCL_VAR is an HFA return value, it stays as a TYP_STRUCT
            unsigned lclNum = op->AsLclVarCommon()->GetLclNum();
            // Make sure this struct type stays as struct so that we can return it as an HFA
            lvaTable[lclNum].lvIsMultiRegRet = true;

            // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
            op->gtFlags |= GTF_DONT_CSE;

            return op;
        }

        if (op->gtOper == GT_CALL)
        {
            if (op->AsCall()->IsVarargs())
            {
                // We cannot tail call because control needs to return to fixup the calling
                // convention for result return.
                op->AsCall()->gtCallMoreFlags &= ~GTF_CALL_M_TAILCALL;
                op->AsCall()->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
            }
            else
            {
                return op;
            }
        }
        return impAssignMultiRegTypeToVar(op, retClsHnd DEBUGARG(unmgdCallConv));
    }

#elif FEATURE_MULTIREG_RET && defined(TARGET_ARM64)

    // Is method returning a multi-reg struct?
    if (IsMultiRegReturnedType(retClsHnd, unmgdCallConv))
    {
        if (op->gtOper == GT_LCL_VAR)
        {
            // This LCL_VAR stays as a TYP_STRUCT
            unsigned lclNum = op->AsLclVarCommon()->GetLclNum();

            if (!lvaIsImplicitByRefLocal(lclNum))
            {
                // Make sure this struct type is not struct promoted
                lvaTable[lclNum].lvIsMultiRegRet = true;

                // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
                op->gtFlags |= GTF_DONT_CSE;

                return op;
            }
        }

        if (op->gtOper == GT_CALL)
        {
            if (op->AsCall()->IsVarargs())
            {
                // We cannot tail call because control needs to return to fixup the calling
                // convention for result return.
                op->AsCall()->gtCallMoreFlags &= ~GTF_CALL_M_TAILCALL;
                op->AsCall()->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
            }
            else
            {
                return op;
            }
        }
        return impAssignMultiRegTypeToVar(op, retClsHnd DEBUGARG(unmgdCallConv));
    }

#endif //  FEATURE_MULTIREG_RET && TARGET_ARM64

    if (!op->IsCall() || !op->AsCall()->TreatAsHasRetBufArg(this))
    {
        // Don't retype `struct` as a primitive type in `ret` instruction.
        return op;
    }

    // This must be one of those 'special' helpers that don't
    // really have a return buffer, but instead use it as a way
    // to keep the trees cleaner with fewer address-taken temps.
    //
    // Well now we have to materialize the the return buffer as
    // an address-taken temp. Then we can return the temp.
    //
    // NOTE: this code assumes that since the call directly
    // feeds the return, then the call must be returning the
    // same structure/class/type.
    //
    unsigned tmpNum = lvaGrabTemp(true DEBUGARG("pseudo return buffer"));

    // No need to spill anything as we're about to return.
    impAssignTempGen(tmpNum, op, info.compMethodInfo->args.retTypeClass, (unsigned)CHECK_SPILL_NONE);

    op = gtNewLclvNode(tmpNum, info.compRetType);
    JITDUMP("\nimpFixupStructReturnType: created a pseudo-return buffer for a special helper\n");
    DISPTREE(op);
    return op;
}

/*****************************************************************************
   CEE_LEAVE may be jumping out of a protected block, viz, a catch or a
   finally-protected try. We find the finally blocks protecting the current
   offset (in order) by walking over the complete exception table and
   finding enclosing clauses. This assumes that the table is sorted.
   This will create a series of BBJ_CALLFINALLY -> BBJ_CALLFINALLY ... -> BBJ_ALWAYS.

   If we are leaving a catch handler, we need to attach the
   CPX_ENDCATCHes to the correct BBJ_CALLFINALLY blocks.

   After this function, the BBJ_LEAVE block has been converted to a different type.
 */

#if !defined(FEATURE_EH_FUNCLETS)

void Compiler::impImportLeave(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nBefore import CEE_LEAVE:\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG

    bool        invalidatePreds = false; // If we create new blocks, invalidate the predecessor lists (if created)
    unsigned    blkAddr         = block->bbCodeOffs;
    BasicBlock* leaveTarget     = block->bbJumpDest;
    unsigned    jmpAddr         = leaveTarget->bbCodeOffs;

    // LEAVE clears the stack, spill side effects, and set stack to 0

    impSpillSideEffects(true, (unsigned)CHECK_SPILL_ALL DEBUGARG("impImportLeave"));
    verCurrentState.esStackDepth = 0;

    assert(block->bbJumpKind == BBJ_LEAVE);
    assert(fgBBs == (BasicBlock**)0xCDCD || fgLookupBB(jmpAddr) != NULL); // should be a BB boundary

    BasicBlock* step         = DUMMY_INIT(NULL);
    unsigned    encFinallies = 0; // Number of enclosing finallies.
    GenTree*    endCatches   = NULL;
    Statement*  endLFinStmt  = NULL; // The statement tree to indicate the end of locally-invoked finally.

    unsigned  XTnum;
    EHblkDsc* HBtab;

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        // Grab the handler offsets

        IL_OFFSET tryBeg = HBtab->ebdTryBegOffs();
        IL_OFFSET tryEnd = HBtab->ebdTryEndOffs();
        IL_OFFSET hndBeg = HBtab->ebdHndBegOffs();
        IL_OFFSET hndEnd = HBtab->ebdHndEndOffs();

        /* Is this a catch-handler we are CEE_LEAVEing out of?
         * If so, we need to call CORINFO_HELP_ENDCATCH.
         */

        if (jitIsBetween(blkAddr, hndBeg, hndEnd) && !jitIsBetween(jmpAddr, hndBeg, hndEnd))
        {
            // Can't CEE_LEAVE out of a finally/fault handler
            if (HBtab->HasFinallyOrFaultHandler())
                BADCODE("leave out of fault/finally block");

            // Create the call to CORINFO_HELP_ENDCATCH
            GenTree* endCatch = gtNewHelperCallNode(CORINFO_HELP_ENDCATCH, TYP_VOID);

            // Make a list of all the currently pending endCatches
            if (endCatches)
                endCatches = gtNewOperNode(GT_COMMA, TYP_VOID, endCatches, endCatch);
            else
                endCatches = endCatch;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - " FMT_BB " jumping out of catch handler EH#%u, adding call to "
                       "CORINFO_HELP_ENDCATCH\n",
                       block->bbNum, XTnum);
            }
#endif
        }
        else if (HBtab->HasFinallyHandler() && jitIsBetween(blkAddr, tryBeg, tryEnd) &&
                 !jitIsBetween(jmpAddr, tryBeg, tryEnd))
        {
            /* This is a finally-protected try we are jumping out of */

            /* If there are any pending endCatches, and we have already
               jumped out of a finally-protected try, then the endCatches
               have to be put in a block in an outer try for async
               exceptions to work correctly.
               Else, just use append to the original block */

            BasicBlock* callBlock;

            assert(!encFinallies ==
                   !endLFinStmt); // if we have finallies, we better have an endLFin tree, and vice-versa

            if (encFinallies == 0)
            {
                assert(step == DUMMY_INIT(NULL));
                callBlock             = block;
                callBlock->bbJumpKind = BBJ_CALLFINALLY; // convert the BBJ_LEAVE to BBJ_CALLFINALLY

                if (endCatches)
                    impAppendTree(endCatches, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try, convert block to BBJ_CALLFINALLY "
                           "block %s\n",
                           callBlock->dspToString());
                }
#endif
            }
            else
            {
                assert(step != DUMMY_INIT(NULL));

                /* Calling the finally block */
                callBlock = fgNewBBinRegion(BBJ_CALLFINALLY, XTnum + 1, 0, step);
                assert(step->bbJumpKind == BBJ_ALWAYS);
                step->bbJumpDest = callBlock; // the previous call to a finally returns to this call (to the next
                                              // finally in the chain)
                step->bbJumpDest->bbRefs++;

                /* The new block will inherit this block's weight */
                callBlock->inheritWeight(block);

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try, new BBJ_CALLFINALLY block %s\n",
                           callBlock->dspToString());
                }
#endif

                Statement* lastStmt;

                if (endCatches)
                {
                    lastStmt = gtNewStmt(endCatches);
                    endLFinStmt->SetNextStmt(lastStmt);
                    lastStmt->SetPrevStmt(endLFinStmt);
                }
                else
                {
                    lastStmt = endLFinStmt;
                }

                // note that this sets BBF_IMPORTED on the block
                impEndTreeList(callBlock, endLFinStmt, lastStmt);
            }

            step = fgNewBBafter(BBJ_ALWAYS, callBlock, true);
            /* The new block will inherit this block's weight */
            step->inheritWeight(block);
            step->bbFlags |= BBF_IMPORTED | BBF_KEEP_BBJ_ALWAYS;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - jumping out of a finally-protected try, created step (BBJ_ALWAYS) block %s\n",
                       step->dspToString());
            }
#endif

            unsigned finallyNesting = compHndBBtab[XTnum].ebdHandlerNestingLevel;
            assert(finallyNesting <= compHndBBtabCount);

            callBlock->bbJumpDest = HBtab->ebdHndBeg; // This callBlock will call the "finally" handler.
            GenTree* endLFin      = new (this, GT_END_LFIN) GenTreeVal(GT_END_LFIN, TYP_VOID, finallyNesting);
            endLFinStmt           = gtNewStmt(endLFin);
            endCatches            = NULL;

            encFinallies++;

            invalidatePreds = true;
        }
    }

    /* Append any remaining endCatches, if any */

    assert(!encFinallies == !endLFinStmt);

    if (encFinallies == 0)
    {
        assert(step == DUMMY_INIT(NULL));
        block->bbJumpKind = BBJ_ALWAYS; // convert the BBJ_LEAVE to a BBJ_ALWAYS

        if (endCatches)
            impAppendTree(endCatches, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - no enclosing finally-protected try blocks; convert CEE_LEAVE block to BBJ_ALWAYS "
                   "block %s\n",
                   block->dspToString());
        }
#endif
    }
    else
    {
        // If leaveTarget is the start of another try block, we want to make sure that
        // we do not insert finalStep into that try block. Hence, we find the enclosing
        // try block.
        unsigned tryIndex = bbFindInnermostCommonTryRegion(step, leaveTarget);

        // Insert a new BB either in the try region indicated by tryIndex or
        // the handler region indicated by leaveTarget->bbHndIndex,
        // depending on which is the inner region.
        BasicBlock* finalStep = fgNewBBinRegion(BBJ_ALWAYS, tryIndex, leaveTarget->bbHndIndex, step);
        finalStep->bbFlags |= BBF_KEEP_BBJ_ALWAYS;
        step->bbJumpDest = finalStep;

        /* The new block will inherit this block's weight */
        finalStep->inheritWeight(block);

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - finalStep block required (encFinallies(%d) > 0), new block %s\n", encFinallies,
                   finalStep->dspToString());
        }
#endif

        Statement* lastStmt;

        if (endCatches)
        {
            lastStmt = gtNewStmt(endCatches);
            endLFinStmt->SetNextStmt(lastStmt);
            lastStmt->SetPrevStmt(endLFinStmt);
        }
        else
        {
            lastStmt = endLFinStmt;
        }

        impEndTreeList(finalStep, endLFinStmt, lastStmt);

        finalStep->bbJumpDest = leaveTarget; // this is the ultimate destination of the LEAVE

        // Queue up the jump target for importing

        impImportBlockPending(leaveTarget);

        invalidatePreds = true;
    }

    if (invalidatePreds && fgComputePredsDone)
    {
        JITDUMP("\n**** impImportLeave - Removing preds after creating new blocks\n");
        fgRemovePreds();
    }

#ifdef DEBUG
    fgVerifyHandlerTab();

    if (verbose)
    {
        printf("\nAfter import CEE_LEAVE:\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG
}

#else // FEATURE_EH_FUNCLETS

void Compiler::impImportLeave(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nBefore import CEE_LEAVE in " FMT_BB " (targetting " FMT_BB "):\n", block->bbNum,
               block->bbJumpDest->bbNum);
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG

    bool        invalidatePreds = false; // If we create new blocks, invalidate the predecessor lists (if created)
    unsigned    blkAddr         = block->bbCodeOffs;
    BasicBlock* leaveTarget     = block->bbJumpDest;
    unsigned    jmpAddr         = leaveTarget->bbCodeOffs;

    // LEAVE clears the stack, spill side effects, and set stack to 0

    impSpillSideEffects(true, (unsigned)CHECK_SPILL_ALL DEBUGARG("impImportLeave"));
    verCurrentState.esStackDepth = 0;

    assert(block->bbJumpKind == BBJ_LEAVE);
    assert(fgBBs == (BasicBlock**)0xCDCD || fgLookupBB(jmpAddr) != nullptr); // should be a BB boundary

    BasicBlock* step = nullptr;

    enum StepType
    {
        // No step type; step == NULL.
        ST_None,

        // Is the step block the BBJ_ALWAYS block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair?
        // That is, is step->bbJumpDest where a finally will return to?
        ST_FinallyReturn,

        // The step block is a catch return.
        ST_Catch,

        // The step block is in a "try", created as the target for a finally return or the target for a catch return.
        ST_Try
    };
    StepType stepType = ST_None;

    unsigned  XTnum;
    EHblkDsc* HBtab;

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        // Grab the handler offsets

        IL_OFFSET tryBeg = HBtab->ebdTryBegOffs();
        IL_OFFSET tryEnd = HBtab->ebdTryEndOffs();
        IL_OFFSET hndBeg = HBtab->ebdHndBegOffs();
        IL_OFFSET hndEnd = HBtab->ebdHndEndOffs();

        /* Is this a catch-handler we are CEE_LEAVEing out of?
         */

        if (jitIsBetween(blkAddr, hndBeg, hndEnd) && !jitIsBetween(jmpAddr, hndBeg, hndEnd))
        {
            // Can't CEE_LEAVE out of a finally/fault handler
            if (HBtab->HasFinallyOrFaultHandler())
            {
                BADCODE("leave out of fault/finally block");
            }

            /* We are jumping out of a catch */

            if (step == nullptr)
            {
                step             = block;
                step->bbJumpKind = BBJ_EHCATCHRET; // convert the BBJ_LEAVE to BBJ_EHCATCHRET
                stepType         = ST_Catch;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a catch (EH#%u), convert block " FMT_BB
                           " to BBJ_EHCATCHRET "
                           "block\n",
                           XTnum, step->bbNum);
                }
#endif
            }
            else
            {
                BasicBlock* exitBlock;

                /* Create a new catch exit block in the catch region for the existing step block to jump to in this
                 * scope */
                exitBlock = fgNewBBinRegion(BBJ_EHCATCHRET, 0, XTnum + 1, step);

                assert(step->bbJumpKind == BBJ_ALWAYS || step->bbJumpKind == BBJ_EHCATCHRET);
                step->bbJumpDest = exitBlock; // the previous step (maybe a call to a nested finally, or a nested catch
                                              // exit) returns to this block
                step->bbJumpDest->bbRefs++;

#if defined(TARGET_ARM)
                if (stepType == ST_FinallyReturn)
                {
                    assert(step->bbJumpKind == BBJ_ALWAYS);
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(TARGET_ARM)

                /* The new block will inherit this block's weight */
                exitBlock->inheritWeight(block);
                exitBlock->bbFlags |= BBF_IMPORTED;

                /* This exit block is the new step */
                step     = exitBlock;
                stepType = ST_Catch;

                invalidatePreds = true;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a catch (EH#%u), new BBJ_EHCATCHRET block " FMT_BB "\n",
                           XTnum, exitBlock->bbNum);
                }
#endif
            }
        }
        else if (HBtab->HasFinallyHandler() && jitIsBetween(blkAddr, tryBeg, tryEnd) &&
                 !jitIsBetween(jmpAddr, tryBeg, tryEnd))
        {
            /* We are jumping out of a finally-protected try */

            BasicBlock* callBlock;

            if (step == nullptr)
            {
#if FEATURE_EH_CALLFINALLY_THUNKS

                // Put the call to the finally in the enclosing region.
                unsigned callFinallyTryIndex =
                    (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) ? 0 : HBtab->ebdEnclosingTryIndex + 1;
                unsigned callFinallyHndIndex =
                    (HBtab->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX) ? 0 : HBtab->ebdEnclosingHndIndex + 1;
                callBlock = fgNewBBinRegion(BBJ_CALLFINALLY, callFinallyTryIndex, callFinallyHndIndex, block);

                // Convert the BBJ_LEAVE to BBJ_ALWAYS, jumping to the new BBJ_CALLFINALLY. This is because
                // the new BBJ_CALLFINALLY is in a different EH region, thus it can't just replace the BBJ_LEAVE,
                // which might be in the middle of the "try". In most cases, the BBJ_ALWAYS will jump to the
                // next block, and flow optimizations will remove it.
                block->bbJumpKind = BBJ_ALWAYS;
                block->bbJumpDest = callBlock;
                block->bbJumpDest->bbRefs++;

                /* The new block will inherit this block's weight */
                callBlock->inheritWeight(block);
                callBlock->bbFlags |= BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), convert block " FMT_BB
                           " to "
                           "BBJ_ALWAYS, add BBJ_CALLFINALLY block " FMT_BB "\n",
                           XTnum, block->bbNum, callBlock->bbNum);
                }
#endif

#else // !FEATURE_EH_CALLFINALLY_THUNKS

                callBlock             = block;
                callBlock->bbJumpKind = BBJ_CALLFINALLY; // convert the BBJ_LEAVE to BBJ_CALLFINALLY

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), convert block " FMT_BB
                           " to "
                           "BBJ_CALLFINALLY block\n",
                           XTnum, callBlock->bbNum);
                }
#endif

#endif // !FEATURE_EH_CALLFINALLY_THUNKS
            }
            else
            {
                // Calling the finally block. We already have a step block that is either the call-to-finally from a
                // more nested try/finally (thus we are jumping out of multiple nested 'try' blocks, each protected by
                // a 'finally'), or the step block is the return from a catch.
                //
                // Due to ThreadAbortException, we can't have the catch return target the call-to-finally block
                // directly. Note that if a 'catch' ends without resetting the ThreadAbortException, the VM will
                // automatically re-raise the exception, using the return address of the catch (that is, the target
                // block of the BBJ_EHCATCHRET) as the re-raise address. If this address is in a finally, the VM will
                // refuse to do the re-raise, and the ThreadAbortException will get eaten (and lost). On AMD64/ARM64,
                // we put the call-to-finally thunk in a special "cloned finally" EH region that does look like a
                // finally clause to the VM. Thus, on these platforms, we can't have BBJ_EHCATCHRET target a
                // BBJ_CALLFINALLY directly. (Note that on ARM32, we don't mark the thunk specially -- it lives directly
                // within the 'try' region protected by the finally, since we generate code in such a way that execution
                // never returns to the call-to-finally call, and the finally-protected 'try' region doesn't appear on
                // stack walks.)

                assert(step->bbJumpKind == BBJ_ALWAYS || step->bbJumpKind == BBJ_EHCATCHRET);

#if FEATURE_EH_CALLFINALLY_THUNKS
                if (step->bbJumpKind == BBJ_EHCATCHRET)
                {
                    // Need to create another step block in the 'try' region that will actually branch to the
                    // call-to-finally thunk.
                    BasicBlock* step2 = fgNewBBinRegion(BBJ_ALWAYS, XTnum + 1, 0, step);
                    step->bbJumpDest  = step2;
                    step->bbJumpDest->bbRefs++;
                    step2->inheritWeight(block);
                    step2->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("impImportLeave - jumping out of a finally-protected try (EH#%u), step block is "
                               "BBJ_EHCATCHRET (" FMT_BB "), new BBJ_ALWAYS step-step block " FMT_BB "\n",
                               XTnum, step->bbNum, step2->bbNum);
                    }
#endif

                    step = step2;
                    assert(stepType == ST_Catch); // Leave it as catch type for now.
                }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

#if FEATURE_EH_CALLFINALLY_THUNKS
                unsigned callFinallyTryIndex =
                    (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) ? 0 : HBtab->ebdEnclosingTryIndex + 1;
                unsigned callFinallyHndIndex =
                    (HBtab->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX) ? 0 : HBtab->ebdEnclosingHndIndex + 1;
#else  // !FEATURE_EH_CALLFINALLY_THUNKS
                unsigned callFinallyTryIndex = XTnum + 1;
                unsigned callFinallyHndIndex = 0; // don't care
#endif // !FEATURE_EH_CALLFINALLY_THUNKS

                callBlock        = fgNewBBinRegion(BBJ_CALLFINALLY, callFinallyTryIndex, callFinallyHndIndex, step);
                step->bbJumpDest = callBlock; // the previous call to a finally returns to this call (to the next
                                              // finally in the chain)
                step->bbJumpDest->bbRefs++;

#if defined(TARGET_ARM)
                if (stepType == ST_FinallyReturn)
                {
                    assert(step->bbJumpKind == BBJ_ALWAYS);
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(TARGET_ARM)

                /* The new block will inherit this block's weight */
                callBlock->inheritWeight(block);
                callBlock->bbFlags |= BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), new BBJ_CALLFINALLY "
                           "block " FMT_BB "\n",
                           XTnum, callBlock->bbNum);
                }
#endif
            }

            step     = fgNewBBafter(BBJ_ALWAYS, callBlock, true);
            stepType = ST_FinallyReturn;

            /* The new block will inherit this block's weight */
            step->inheritWeight(block);
            step->bbFlags |= BBF_IMPORTED | BBF_KEEP_BBJ_ALWAYS;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - jumping out of a finally-protected try (EH#%u), created step (BBJ_ALWAYS) "
                       "block " FMT_BB "\n",
                       XTnum, step->bbNum);
            }
#endif

            callBlock->bbJumpDest = HBtab->ebdHndBeg; // This callBlock will call the "finally" handler.

            invalidatePreds = true;
        }
        else if (HBtab->HasCatchHandler() && jitIsBetween(blkAddr, tryBeg, tryEnd) &&
                 !jitIsBetween(jmpAddr, tryBeg, tryEnd))
        {
            // We are jumping out of a catch-protected try.
            //
            // If we are returning from a call to a finally, then we must have a step block within a try
            // that is protected by a catch. This is so when unwinding from that finally (e.g., if code within the
            // finally raises an exception), the VM will find this step block, notice that it is in a protected region,
            // and invoke the appropriate catch.
            //
            // We also need to handle a special case with the handling of ThreadAbortException. If a try/catch
            // catches a ThreadAbortException (which might be because it catches a parent, e.g. System.Exception),
            // and the catch doesn't call System.Threading.Thread::ResetAbort(), then when the catch returns to the VM,
            // the VM will automatically re-raise the ThreadAbortException. When it does this, it uses the target
            // address of the catch return as the new exception address. That is, the re-raised exception appears to
            // occur at the catch return address. If this exception return address skips an enclosing try/catch that
            // catches ThreadAbortException, then the enclosing try/catch will not catch the exception, as it should.
            // For example:
            //
            // try {
            //    try {
            //       // something here raises ThreadAbortException
            //       LEAVE LABEL_1; // no need to stop at LABEL_2
            //    } catch (Exception) {
            //       // This catches ThreadAbortException, but doesn't call System.Threading.Thread::ResetAbort(), so
            //       // ThreadAbortException is re-raised by the VM at the address specified by the LEAVE opcode.
            //       // This is bad, since it means the outer try/catch won't get a chance to catch the re-raised
            //       // ThreadAbortException. So, instead, create step block LABEL_2 and LEAVE to that. We only
            //       // need to do this transformation if the current EH block is a try/catch that catches
            //       // ThreadAbortException (or one of its parents), however we might not be able to find that
            //       // information, so currently we do it for all catch types.
            //       LEAVE LABEL_1; // Convert this to LEAVE LABEL2;
            //    }
            //    LABEL_2: LEAVE LABEL_1; // inserted by this step creation code
            // } catch (ThreadAbortException) {
            // }
            // LABEL_1:
            //
            // Note that this pattern isn't theoretical: it occurs in ASP.NET, in IL code generated by the Roslyn C#
            // compiler.

            if ((stepType == ST_FinallyReturn) || (stepType == ST_Catch))
            {
                BasicBlock* catchStep;

                assert(step);

                if (stepType == ST_FinallyReturn)
                {
                    assert(step->bbJumpKind == BBJ_ALWAYS);
                }
                else
                {
                    assert(stepType == ST_Catch);
                    assert(step->bbJumpKind == BBJ_EHCATCHRET);
                }

                /* Create a new exit block in the try region for the existing step block to jump to in this scope */
                catchStep        = fgNewBBinRegion(BBJ_ALWAYS, XTnum + 1, 0, step);
                step->bbJumpDest = catchStep;
                step->bbJumpDest->bbRefs++;

#if defined(TARGET_ARM)
                if (stepType == ST_FinallyReturn)
                {
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(TARGET_ARM)

                /* The new block will inherit this block's weight */
                catchStep->inheritWeight(block);
                catchStep->bbFlags |= BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    if (stepType == ST_FinallyReturn)
                    {
                        printf("impImportLeave - return from finally jumping out of a catch-protected try (EH#%u), new "
                               "BBJ_ALWAYS block " FMT_BB "\n",
                               XTnum, catchStep->bbNum);
                    }
                    else
                    {
                        assert(stepType == ST_Catch);
                        printf("impImportLeave - return from catch jumping out of a catch-protected try (EH#%u), new "
                               "BBJ_ALWAYS block " FMT_BB "\n",
                               XTnum, catchStep->bbNum);
                    }
                }
#endif // DEBUG

                /* This block is the new step */
                step     = catchStep;
                stepType = ST_Try;

                invalidatePreds = true;
            }
        }
    }

    if (step == nullptr)
    {
        block->bbJumpKind = BBJ_ALWAYS; // convert the BBJ_LEAVE to a BBJ_ALWAYS

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - no enclosing finally-protected try blocks or catch handlers; convert CEE_LEAVE "
                   "block " FMT_BB " to BBJ_ALWAYS\n",
                   block->bbNum);
        }
#endif
    }
    else
    {
        step->bbJumpDest = leaveTarget; // this is the ultimate destination of the LEAVE

#if defined(TARGET_ARM)
        if (stepType == ST_FinallyReturn)
        {
            assert(step->bbJumpKind == BBJ_ALWAYS);
            // Mark the target of a finally return
            step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
        }
#endif // defined(TARGET_ARM)

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - final destination of step blocks set to " FMT_BB "\n", leaveTarget->bbNum);
        }
#endif

        // Queue up the jump target for importing

        impImportBlockPending(leaveTarget);
    }

    if (invalidatePreds && fgComputePredsDone)
    {
        JITDUMP("\n**** impImportLeave - Removing preds after creating new blocks\n");
        fgRemovePreds();
    }

#ifdef DEBUG
    fgVerifyHandlerTab();

    if (verbose)
    {
        printf("\nAfter import CEE_LEAVE:\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG
}

#endif // FEATURE_EH_FUNCLETS

/*****************************************************************************/
// This is called when reimporting a leave block. It resets the JumpKind,
// JumpDest, and bbNext to the original values

void Compiler::impResetLeaveBlock(BasicBlock* block, unsigned jmpAddr)
{
#if defined(FEATURE_EH_FUNCLETS)
    // With EH Funclets, while importing leave opcode we create another block ending with BBJ_ALWAYS (call it B1)
    // and the block containing leave (say B0) is marked as BBJ_CALLFINALLY.   Say for some reason we reimport B0,
    // it is reset (in this routine) by marking as ending with BBJ_LEAVE and further down when B0 is reimported, we
    // create another BBJ_ALWAYS (call it B2). In this process B1 gets orphaned and any blocks to which B1 is the
    // only predecessor are also considered orphans and attempted to be deleted.
    //
    //  try  {
    //     ....
    //     try
    //     {
    //         ....
    //         leave OUTSIDE;  // B0 is the block containing this leave, following this would be B1
    //     } finally { }
    //  } finally { }
    //  OUTSIDE:
    //
    // In the above nested try-finally example, we create a step block (call it Bstep) which in branches to a block
    // where a finally would branch to (and such block is marked as finally target).  Block B1 branches to step block.
    // Because of re-import of B0, Bstep is also orphaned. Since Bstep is a finally target it cannot be removed.  To
    // work around this we will duplicate B0 (call it B0Dup) before reseting. B0Dup is marked as BBJ_CALLFINALLY and
    // only serves to pair up with B1 (BBJ_ALWAYS) that got orphaned. Now during orphan block deletion B0Dup and B1
    // will be treated as pair and handled correctly.
    if (block->bbJumpKind == BBJ_CALLFINALLY)
    {
        BasicBlock* dupBlock = bbNewBasicBlock(block->bbJumpKind);
        dupBlock->bbFlags    = block->bbFlags;
        dupBlock->bbJumpDest = block->bbJumpDest;
        dupBlock->copyEHRegion(block);
        dupBlock->bbCatchTyp = block->bbCatchTyp;

        // Mark this block as
        //  a) not referenced by any other block to make sure that it gets deleted
        //  b) weight zero
        //  c) prevent from being imported
        //  d) as internal
        //  e) as rarely run
        dupBlock->bbRefs   = 0;
        dupBlock->bbWeight = BB_ZERO_WEIGHT;
        dupBlock->bbFlags |= BBF_IMPORTED | BBF_INTERNAL | BBF_RUN_RARELY;

        // Insert the block right after the block which is getting reset so that BBJ_CALLFINALLY and BBJ_ALWAYS
        // will be next to each other.
        fgInsertBBafter(block, dupBlock);

#ifdef DEBUG
        if (verbose)
        {
            printf("New Basic Block " FMT_BB " duplicate of " FMT_BB " created.\n", dupBlock->bbNum, block->bbNum);
        }
#endif
    }
#endif // FEATURE_EH_FUNCLETS

    block->bbJumpKind = BBJ_LEAVE;
    fgInitBBLookup();
    block->bbJumpDest = fgLookupBB(jmpAddr);

    // We will leave the BBJ_ALWAYS block we introduced. When it's reimported
    // the BBJ_ALWAYS block will be unreachable, and will be removed after. The
    // reason we don't want to remove the block at this point is that if we call
    // fgInitBBLookup() again we will do it wrong as the BBJ_ALWAYS block won't be
    // added and the linked list length will be different than fgBBcount.
}

/*****************************************************************************/
// Get the first non-prefix opcode. Used for verification of valid combinations
// of prefixes and actual opcodes.

OPCODE Compiler::impGetNonPrefixOpcode(const BYTE* codeAddr, const BYTE* codeEndp)
{
    while (codeAddr < codeEndp)
    {
        OPCODE opcode = (OPCODE)getU1LittleEndian(codeAddr);
        codeAddr += sizeof(__int8);

        if (opcode == CEE_PREFIX1)
        {
            if (codeAddr >= codeEndp)
            {
                break;
            }
            opcode = (OPCODE)(getU1LittleEndian(codeAddr) + 256);
            codeAddr += sizeof(__int8);
        }

        switch (opcode)
        {
            case CEE_UNALIGNED:
            case CEE_VOLATILE:
            case CEE_TAILCALL:
            case CEE_CONSTRAINED:
            case CEE_READONLY:
                break;
            default:
                return opcode;
        }

        codeAddr += opcodeSizes[opcode];
    }

    return CEE_ILLEGAL;
}

/*****************************************************************************/
// Checks whether the opcode is a valid opcode for volatile. and unaligned. prefixes

void Compiler::impValidateMemoryAccessOpcode(const BYTE* codeAddr, const BYTE* codeEndp, bool volatilePrefix)
{
    OPCODE opcode = impGetNonPrefixOpcode(codeAddr, codeEndp);

    if (!(
            // Opcode of all ldind and stdind happen to be in continuous, except stind.i.
            ((CEE_LDIND_I1 <= opcode) && (opcode <= CEE_STIND_R8)) || (opcode == CEE_STIND_I) ||
            (opcode == CEE_LDFLD) || (opcode == CEE_STFLD) || (opcode == CEE_LDOBJ) || (opcode == CEE_STOBJ) ||
            (opcode == CEE_INITBLK) || (opcode == CEE_CPBLK) ||
            // volatile. prefix is allowed with the ldsfld and stsfld
            (volatilePrefix && ((opcode == CEE_LDSFLD) || (opcode == CEE_STSFLD)))))
    {
        BADCODE("Invalid opcode for unaligned. or volatile. prefix");
    }
}

/*****************************************************************************/

#ifdef DEBUG

#undef RETURN // undef contracts RETURN macro

enum controlFlow_t
{
    NEXT,
    CALL,
    RETURN,
    THROW,
    BRANCH,
    COND_BRANCH,
    BREAK,
    PHI,
    META,
};

const static controlFlow_t controlFlow[] = {
#define OPDEF(c, s, pop, push, args, type, l, s1, s2, flow) flow,
#include "opcode.def"
#undef OPDEF
};

#endif // DEBUG

/*****************************************************************************
 *  Determine the result type of an arithemetic operation
 *  On 64-bit inserts upcasts when native int is mixed with int32
 */
var_types Compiler::impGetByRefResultType(genTreeOps oper, bool fUnsigned, GenTree** pOp1, GenTree** pOp2)
{
    var_types type = TYP_UNDEF;
    GenTree*  op1  = *pOp1;
    GenTree*  op2  = *pOp2;

    // Arithemetic operations are generally only allowed with
    // primitive types, but certain operations are allowed
    // with byrefs

    if ((oper == GT_SUB) && (genActualType(op1->TypeGet()) == TYP_BYREF || genActualType(op2->TypeGet()) == TYP_BYREF))
    {
        if ((genActualType(op1->TypeGet()) == TYP_BYREF) && (genActualType(op2->TypeGet()) == TYP_BYREF))
        {
            // byref1-byref2 => gives a native int
            type = TYP_I_IMPL;
        }
        else if (genActualTypeIsIntOrI(op1->TypeGet()) && (genActualType(op2->TypeGet()) == TYP_BYREF))
        {
            // [native] int - byref => gives a native int

            //
            // The reason is that it is possible, in managed C++,
            // to have a tree like this:
            //
            //              -
            //             / \.
            //            /   \.
            //           /     \.
            //          /       \.
            // const(h) int     addr byref
            //
            // <BUGNUM> VSW 318822 </BUGNUM>
            //
            // So here we decide to make the resulting type to be a native int.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_64BIT
            if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
            {
                // insert an explicit upcast
                op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
            }
#endif // TARGET_64BIT

            type = TYP_I_IMPL;
        }
        else
        {
            // byref - [native] int => gives a byref
            assert(genActualType(op1->TypeGet()) == TYP_BYREF && genActualTypeIsIntOrI(op2->TypeGet()));

#ifdef TARGET_64BIT
            if ((genActualType(op2->TypeGet()) != TYP_I_IMPL))
            {
                // insert an explicit upcast
                op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
            }
#endif // TARGET_64BIT

            type = TYP_BYREF;
        }
    }
    else if ((oper == GT_ADD) &&
             (genActualType(op1->TypeGet()) == TYP_BYREF || genActualType(op2->TypeGet()) == TYP_BYREF))
    {
        // byref + [native] int => gives a byref
        // (or)
        // [native] int + byref => gives a byref

        // only one can be a byref : byref op byref not allowed
        assert(genActualType(op1->TypeGet()) != TYP_BYREF || genActualType(op2->TypeGet()) != TYP_BYREF);
        assert(genActualTypeIsIntOrI(op1->TypeGet()) || genActualTypeIsIntOrI(op2->TypeGet()));

#ifdef TARGET_64BIT
        if (genActualType(op2->TypeGet()) == TYP_BYREF)
        {
            if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
            {
                // insert an explicit upcast
                op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
            }
        }
        else if (genActualType(op2->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
        }
#endif // TARGET_64BIT

        type = TYP_BYREF;
    }
#ifdef TARGET_64BIT
    else if (genActualType(op1->TypeGet()) == TYP_I_IMPL || genActualType(op2->TypeGet()) == TYP_I_IMPL)
    {
        assert(!varTypeIsFloating(op1->gtType) && !varTypeIsFloating(op2->gtType));

        // int + long => gives long
        // long + int => gives long
        // we get this because in the IL the long isn't Int64, it's just IntPtr

        if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
        }
        else if (genActualType(op2->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, fUnsigned, fUnsigned ? TYP_U_IMPL : TYP_I_IMPL);
        }

        type = TYP_I_IMPL;
    }
#else  // 32-bit TARGET
    else if (genActualType(op1->TypeGet()) == TYP_LONG || genActualType(op2->TypeGet()) == TYP_LONG)
    {
        assert(!varTypeIsFloating(op1->gtType) && !varTypeIsFloating(op2->gtType));

        // int + long => gives long
        // long + int => gives long

        type = TYP_LONG;
    }
#endif // TARGET_64BIT
    else
    {
        // int + int => gives an int
        assert(genActualType(op1->TypeGet()) != TYP_BYREF && genActualType(op2->TypeGet()) != TYP_BYREF);

        assert(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
               (varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType)));

        type = genActualType(op1->gtType);

        // If both operands are TYP_FLOAT, then leave it as TYP_FLOAT.
        // Otherwise, turn floats into doubles
        if ((type == TYP_FLOAT) && (genActualType(op2->gtType) != TYP_FLOAT))
        {
            assert(genActualType(op2->gtType) == TYP_DOUBLE);
            type = TYP_DOUBLE;
        }
    }

    assert(type == TYP_BYREF || type == TYP_DOUBLE || type == TYP_FLOAT || type == TYP_LONG || type == TYP_INT);
    return type;
}

//------------------------------------------------------------------------
// impOptimizeCastClassOrIsInst: attempt to resolve a cast when jitting
//
// Arguments:
//   op1 - value to cast
//   pResolvedToken - resolved token for type to cast to
//   isCastClass - true if this is a castclass, false if isinst
//
// Return Value:
//   tree representing optimized cast, or null if no optimization possible

GenTree* Compiler::impOptimizeCastClassOrIsInst(GenTree* op1, CORINFO_RESOLVED_TOKEN* pResolvedToken, bool isCastClass)
{
    assert(op1->TypeGet() == TYP_REF);

    // Don't optimize for minopts or debug codegen.
    if (opts.OptimizationDisabled())
    {
        return nullptr;
    }

    // See what we know about the type of the object being cast.
    bool                 isExact   = false;
    bool                 isNonNull = false;
    CORINFO_CLASS_HANDLE fromClass = gtGetClassHandle(op1, &isExact, &isNonNull);

    if (fromClass != nullptr)
    {
        CORINFO_CLASS_HANDLE toClass = pResolvedToken->hClass;
        JITDUMP("\nConsidering optimization of %s from %s%p (%s) to %p (%s)\n", isCastClass ? "castclass" : "isinst",
                isExact ? "exact " : "", dspPtr(fromClass), info.compCompHnd->getClassName(fromClass), dspPtr(toClass),
                info.compCompHnd->getClassName(toClass));

        // Perhaps we know if the cast will succeed or fail.
        TypeCompareState castResult = info.compCompHnd->compareTypesForCast(fromClass, toClass);

        if (castResult == TypeCompareState::Must)
        {
            // Cast will succeed, result is simply op1.
            JITDUMP("Cast will succeed, optimizing to simply return input\n");
            return op1;
        }
        else if (castResult == TypeCompareState::MustNot)
        {
            // See if we can sharpen exactness by looking for final classes
            if (!isExact)
            {
                isExact = impIsClassExact(fromClass);
            }

            // Cast to exact type will fail. Handle case where we have
            // an exact type (that is, fromClass is not a subtype)
            // and we're not going to throw on failure.
            if (isExact && !isCastClass)
            {
                JITDUMP("Cast will fail, optimizing to return null\n");
                GenTree* result = gtNewIconNode(0, TYP_REF);

                // If the cast was fed by a box, we can remove that too.
                if (op1->IsBoxedValue())
                {
                    JITDUMP("Also removing upstream box\n");
                    gtTryRemoveBoxUpstreamEffects(op1);
                }

                return result;
            }
            else if (isExact)
            {
                JITDUMP("Not optimizing failing castclass (yet)\n");
            }
            else
            {
                JITDUMP("Can't optimize since fromClass is inexact\n");
            }
        }
        else
        {
            JITDUMP("Result of cast unknown, must generate runtime test\n");
        }
    }
    else
    {
        JITDUMP("\nCan't optimize since fromClass is unknown\n");
    }

    return nullptr;
}

//------------------------------------------------------------------------
// impCastClassOrIsInstToTree: build and import castclass/isinst
//
// Arguments:
//   op1 - value to cast
//   op2 - type handle for type to cast to
//   pResolvedToken - resolved token from the cast operation
//   isCastClass - true if this is castclass, false means isinst
//
// Return Value:
//   Tree representing the cast
//
// Notes:
//   May expand into a series of runtime checks or a helper call.

GenTree* Compiler::impCastClassOrIsInstToTree(GenTree*                op1,
                                              GenTree*                op2,
                                              CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                              bool                    isCastClass)
{
    assert(op1->TypeGet() == TYP_REF);

    // Optimistically assume the jit should expand this as an inline test
    bool shouldExpandInline = true;

    // Profitability check.
    //
    // Don't bother with inline expansion when jit is trying to
    // generate code quickly, or the cast is in code that won't run very
    // often, or the method already is pretty big.
    if (compCurBB->isRunRarely() || opts.OptimizationDisabled())
    {
        // not worth the code expansion if jitting fast or in a rarely run block
        shouldExpandInline = false;
    }
    else if ((op1->gtFlags & GTF_GLOB_EFFECT) && lvaHaveManyLocals())
    {
        // not worth creating an untracked local variable
        shouldExpandInline = false;
    }

    // Pessimistically assume the jit cannot expand this as an inline test
    bool                  canExpandInline = false;
    const CorInfoHelpFunc helper          = info.compCompHnd->getCastingHelper(pResolvedToken, isCastClass);

    // Legality check.
    //
    // Not all classclass/isinst operations can be inline expanded.
    // Check legality only if an inline expansion is desirable.
    if (shouldExpandInline)
    {
        if (isCastClass)
        {
            // Jit can only inline expand the normal CHKCASTCLASS helper.
            canExpandInline = (helper == CORINFO_HELP_CHKCASTCLASS);
        }
        else
        {
            if (helper == CORINFO_HELP_ISINSTANCEOFCLASS)
            {
                // If the class is exact, the jit can expand the IsInst check inline.
                canExpandInline = impIsClassExact(pResolvedToken->hClass);
            }
        }
    }

    const bool expandInline = canExpandInline && shouldExpandInline;

    if (!expandInline)
    {
        JITDUMP("\nExpanding %s as call because %s\n", isCastClass ? "castclass" : "isinst",
                canExpandInline ? "want smaller code or faster jitting" : "inline expansion not legal");

        // If we CSE this class handle we prevent assertionProp from making SubType assertions
        // so instead we force the CSE logic to not consider CSE-ing this class handle.
        //
        op2->gtFlags |= GTF_DONT_CSE;

        return gtNewHelperCallNode(helper, TYP_REF, gtNewCallArgs(op2, op1));
    }

    JITDUMP("\nExpanding %s inline\n", isCastClass ? "castclass" : "isinst");

    impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("bubbling QMark2"));

    GenTree* temp;
    GenTree* condMT;
    //
    // expand the methodtable match:
    //
    //  condMT ==>   GT_NE
    //               /    \.
    //           GT_IND   op2 (typically CNS_INT)
    //              |
    //           op1Copy
    //

    // This can replace op1 with a GT_COMMA that evaluates op1 into a local
    //
    op1 = impCloneExpr(op1, &temp, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL, nullptr DEBUGARG("CASTCLASS eval op1"));
    //
    // op1 is now known to be a non-complex tree
    // thus we can use gtClone(op1) from now on
    //

    GenTree* op2Var = op2;
    if (isCastClass)
    {
        op2Var                                                  = fgInsertCommaFormTemp(&op2);
        lvaTable[op2Var->AsLclVarCommon()->GetLclNum()].lvIsCSE = true;
    }
    temp   = gtNewMethodTableLookup(temp);
    condMT = gtNewOperNode(GT_NE, TYP_INT, temp, op2);

    GenTree* condNull;
    //
    // expand the null check:
    //
    //  condNull ==>   GT_EQ
    //                 /    \.
    //             op1Copy CNS_INT
    //                      null
    //
    condNull = gtNewOperNode(GT_EQ, TYP_INT, gtClone(op1), gtNewIconNode(0, TYP_REF));

    //
    // expand the true and false trees for the condMT
    //
    GenTree* condFalse = gtClone(op1);
    GenTree* condTrue;
    if (isCastClass)
    {
        //
        // use the special helper that skips the cases checked by our inlined cast
        //
        const CorInfoHelpFunc specialHelper = CORINFO_HELP_CHKCASTCLASS_SPECIAL;

        condTrue = gtNewHelperCallNode(specialHelper, TYP_REF, gtNewCallArgs(op2Var, gtClone(op1)));
    }
    else
    {
        condTrue = gtNewIconNode(0, TYP_REF);
    }

    GenTree* qmarkMT;
    //
    // Generate first QMARK - COLON tree
    //
    //  qmarkMT ==>   GT_QMARK
    //                 /     \.
    //            condMT   GT_COLON
    //                      /     \.
    //                condFalse  condTrue
    //
    temp    = new (this, GT_COLON) GenTreeColon(TYP_REF, condTrue, condFalse);
    qmarkMT = gtNewQmarkNode(TYP_REF, condMT, temp);

    if (isCastClass && impIsClassExact(pResolvedToken->hClass) && condTrue->OperIs(GT_CALL))
    {
        // condTrue is used only for throwing InvalidCastException in case of casting to an exact class.
        condTrue->AsCall()->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;
    }

    GenTree* qmarkNull;
    //
    // Generate second QMARK - COLON tree
    //
    //  qmarkNull ==>  GT_QMARK
    //                 /     \.
    //           condNull  GT_COLON
    //                      /     \.
    //                qmarkMT   op1Copy
    //
    temp      = new (this, GT_COLON) GenTreeColon(TYP_REF, gtClone(op1), qmarkMT);
    qmarkNull = gtNewQmarkNode(TYP_REF, condNull, temp);
    qmarkNull->gtFlags |= GTF_QMARK_CAST_INSTOF;

    // Make QMark node a top level node by spilling it.
    unsigned tmp = lvaGrabTemp(true DEBUGARG("spilling QMark2"));
    impAssignTempGen(tmp, qmarkNull, (unsigned)CHECK_SPILL_NONE);

    // TODO-CQ: Is it possible op1 has a better type?
    //
    // See also gtGetHelperCallClassHandle where we make the same
    // determination for the helper call variants.
    LclVarDsc* lclDsc = lvaGetDesc(tmp);
    assert(lclDsc->lvSingleDef == 0);
    lclDsc->lvSingleDef = 1;
    JITDUMP("Marked V%02u as a single def temp\n", tmp);
    lvaSetClass(tmp, pResolvedToken->hClass);
    return gtNewLclvNode(tmp, TYP_REF);
}

#ifndef DEBUG
#define assertImp(cond) ((void)0)
#else
#define assertImp(cond)                                                                                                \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            const int cchAssertImpBuf = 600;                                                                           \
            char*     assertImpBuf    = (char*)alloca(cchAssertImpBuf);                                                \
            _snprintf_s(assertImpBuf, cchAssertImpBuf, cchAssertImpBuf - 1,                                            \
                        "%s : Possibly bad IL with CEE_%s at offset %04Xh (op1=%s op2=%s stkDepth=%d)", #cond,         \
                        impCurOpcName, impCurOpcOffs, op1 ? varTypeName(op1->TypeGet()) : "NULL",                      \
                        op2 ? varTypeName(op2->TypeGet()) : "NULL", verCurrentState.esStackDepth);                     \
            assertAbort(assertImpBuf, __FILE__, __LINE__);                                                             \
        }                                                                                                              \
    } while (0)
#endif // DEBUG

//------------------------------------------------------------------------
// impBlockIsInALoop: check if a block might be in a loop
//
// Arguments:
//    block - block to check
//
// Returns:
//    true if the block might be in a loop.
//
// Notes:
//    Conservatively correct; may return true for some blocks that are
//    not actually in loops.
//
bool Compiler::impBlockIsInALoop(BasicBlock* block)
{
    return (compIsForInlining() && ((impInlineInfo->iciBlock->bbFlags & BBF_BACKWARD_JUMP) != 0)) ||
           ((block->bbFlags & BBF_BACKWARD_JUMP) != 0);
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
/*****************************************************************************
 *  Import the instr for the given basic block
 */
void Compiler::impImportBlockCode(BasicBlock* block)
{
#define _impResolveToken(kind) impResolveToken(codeAddr, &resolvedToken, kind)

#ifdef DEBUG

    if (verbose)
    {
        printf("\nImporting " FMT_BB " (PC=%03u) of '%s'", block->bbNum, block->bbCodeOffs, info.compFullName);
    }
#endif

    unsigned                     nxtStmtIndex = impInitBlockLineInfo();
    IL_OFFSET                    nxtStmtOffs;
    CorInfoHelpFunc              helper;
    CorInfoIsAccessAllowedResult accessAllowedResult;
    CORINFO_HELPER_DESC          calloutHelper;
    const BYTE*                  lastLoadToken = nullptr;

    // reject cyclic constraints
    if (tiVerificationNeeded)
    {
        Verify(!info.hasCircularClassConstraints, "Method parent has circular class type parameter constraints.");
        Verify(!info.hasCircularMethodConstraints, "Method has circular method type parameter constraints.");
    }

    /* Get the tree list started */

    impBeginTreeList();

#ifdef FEATURE_ON_STACK_REPLACEMENT

    // Are there any places in the method where we might add a patchpoint?
    if (compHasBackwardJump)
    {
        // Are patchpoints enabled?
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0) && (JitConfig.TC_OnStackReplacement() > 0))
        {
            // We don't inline at Tier0, if we do, we may need rethink our approach.
            // Could probably support inlines that don't introduce flow.
            assert(!compIsForInlining());

            // Is the start of this block a suitable patchpoint?
            // Current strategy is blocks that are stack-empty and backwards branch targets
            if (block->bbFlags & BBF_BACKWARD_JUMP_TARGET && (verCurrentState.esStackDepth == 0))
            {
                block->bbFlags |= BBF_PATCHPOINT;
                setMethodHasPatchpoint();
            }
        }
    }
    else
    {
        // Should not see backward branch targets w/o backwards branches
        assert((block->bbFlags & BBF_BACKWARD_JUMP_TARGET) == 0);
    }

#endif // FEATURE_ON_STACK_REPLACEMENT

    /* Walk the opcodes that comprise the basic block */

    const BYTE* codeAddr = info.compCode + block->bbCodeOffs;
    const BYTE* codeEndp = info.compCode + block->bbCodeOffsEnd;

    IL_OFFSET opcodeOffs    = block->bbCodeOffs;
    IL_OFFSET lastSpillOffs = opcodeOffs;

    signed jmpDist;

    /* remember the start of the delegate creation sequence (used for verification) */
    const BYTE* delegateCreateStart = nullptr;

    int  prefixFlags = 0;
    bool explicitTailCall, constraintCall, readonlyCall;

    typeInfo tiRetVal;

    unsigned numArgs = info.compArgsCount;

    /* Now process all the opcodes in the block */

    var_types callTyp    = TYP_COUNT;
    OPCODE    prevOpcode = CEE_ILLEGAL;

    if (block->bbCatchTyp)
    {
        if (info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES)
        {
            impCurStmtOffsSet(block->bbCodeOffs);
        }

        // We will spill the GT_CATCH_ARG and the input of the BB_QMARK block
        // to a temp. This is a trade off for code simplicity
        impSpillSpecialSideEff();
    }

    while (codeAddr < codeEndp)
    {
#ifdef FEATURE_READYTORUN_COMPILER
        bool usingReadyToRunHelper = false;
#endif
        CORINFO_RESOLVED_TOKEN resolvedToken;
        CORINFO_RESOLVED_TOKEN constrainedResolvedToken;
        CORINFO_CALL_INFO      callInfo;
        CORINFO_FIELD_INFO     fieldInfo;

        tiRetVal = typeInfo(); // Default type info

        //---------------------------------------------------------------------

        /* We need to restrict the max tree depth as many of the Compiler
           functions are recursive. We do this by spilling the stack */

        if (verCurrentState.esStackDepth)
        {
            /* Has it been a while since we last saw a non-empty stack (which
               guarantees that the tree depth isnt accumulating. */

            if ((opcodeOffs - lastSpillOffs) > MAX_TREE_SIZE && impCanSpillNow(prevOpcode))
            {
                impSpillStackEnsure();
                lastSpillOffs = opcodeOffs;
            }
        }
        else
        {
            lastSpillOffs   = opcodeOffs;
            impBoxTempInUse = false; // nothing on the stack, box temp OK to use again
        }

        /* Compute the current instr offset */

        opcodeOffs = (IL_OFFSET)(codeAddr - info.compCode);

#ifndef DEBUG
        if (opts.compDbgInfo)
#endif
        {
            if (!compIsForInlining())
            {
                nxtStmtOffs =
                    (nxtStmtIndex < info.compStmtOffsetsCount) ? info.compStmtOffsets[nxtStmtIndex] : BAD_IL_OFFSET;

                /* Have we reached the next stmt boundary ? */

                if (nxtStmtOffs != BAD_IL_OFFSET && opcodeOffs >= nxtStmtOffs)
                {
                    assert(nxtStmtOffs == info.compStmtOffsets[nxtStmtIndex]);

                    if (verCurrentState.esStackDepth != 0 && opts.compDbgCode)
                    {
                        /* We need to provide accurate IP-mapping at this point.
                           So spill anything on the stack so that it will form
                           gtStmts with the correct stmt offset noted */

                        impSpillStackEnsure(true);
                    }

                    // Has impCurStmtOffs been reported in any tree?

                    if (impCurStmtOffs != BAD_IL_OFFSET && opts.compDbgCode)
                    {
                        GenTree* placeHolder = new (this, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
                        impAppendTree(placeHolder, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

                        assert(impCurStmtOffs == BAD_IL_OFFSET);
                    }

                    if (impCurStmtOffs == BAD_IL_OFFSET)
                    {
                        /* Make sure that nxtStmtIndex is in sync with opcodeOffs.
                           If opcodeOffs has gone past nxtStmtIndex, catch up */

                        while ((nxtStmtIndex + 1) < info.compStmtOffsetsCount &&
                               info.compStmtOffsets[nxtStmtIndex + 1] <= opcodeOffs)
                        {
                            nxtStmtIndex++;
                        }

                        /* Go to the new stmt */

                        impCurStmtOffsSet(info.compStmtOffsets[nxtStmtIndex]);

                        /* Update the stmt boundary index */

                        nxtStmtIndex++;
                        assert(nxtStmtIndex <= info.compStmtOffsetsCount);

                        /* Are there any more line# entries after this one? */

                        if (nxtStmtIndex < info.compStmtOffsetsCount)
                        {
                            /* Remember where the next line# starts */

                            nxtStmtOffs = info.compStmtOffsets[nxtStmtIndex];
                        }
                        else
                        {
                            /* No more line# entries */

                            nxtStmtOffs = BAD_IL_OFFSET;
                        }
                    }
                }
                else if ((info.compStmtOffsetsImplicit & ICorDebugInfo::STACK_EMPTY_BOUNDARIES) &&
                         (verCurrentState.esStackDepth == 0))
                {
                    /* At stack-empty locations, we have already added the tree to
                       the stmt list with the last offset. We just need to update
                       impCurStmtOffs
                     */

                    impCurStmtOffsSet(opcodeOffs);
                }
                else if ((info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES) &&
                         impOpcodeIsCallSiteBoundary(prevOpcode))
                {
                    /* Make sure we have a type cached */
                    assert(callTyp != TYP_COUNT);

                    if (callTyp == TYP_VOID)
                    {
                        impCurStmtOffsSet(opcodeOffs);
                    }
                    else if (opts.compDbgCode)
                    {
                        impSpillStackEnsure(true);
                        impCurStmtOffsSet(opcodeOffs);
                    }
                }
                else if ((info.compStmtOffsetsImplicit & ICorDebugInfo::NOP_BOUNDARIES) && (prevOpcode == CEE_NOP))
                {
                    if (opts.compDbgCode)
                    {
                        impSpillStackEnsure(true);
                    }

                    impCurStmtOffsSet(opcodeOffs);
                }

                assert(impCurStmtOffs == BAD_IL_OFFSET || nxtStmtOffs == BAD_IL_OFFSET ||
                       jitGetILoffs(impCurStmtOffs) <= nxtStmtOffs);
            }
        }

        CORINFO_CLASS_HANDLE clsHnd       = DUMMY_INIT(NULL);
        CORINFO_CLASS_HANDLE ldelemClsHnd = DUMMY_INIT(NULL);
        CORINFO_CLASS_HANDLE stelemClsHnd = DUMMY_INIT(NULL);

        var_types lclTyp, ovflType = TYP_UNKNOWN;
        GenTree*  op1           = DUMMY_INIT(NULL);
        GenTree*  op2           = DUMMY_INIT(NULL);
        GenTree*  newObjThisPtr = DUMMY_INIT(NULL);
        bool      uns           = DUMMY_INIT(false);
        bool      isLocal       = false;

        /* Get the next opcode and the size of its parameters */

        OPCODE opcode = (OPCODE)getU1LittleEndian(codeAddr);
        codeAddr += sizeof(__int8);

#ifdef DEBUG
        impCurOpcOffs = (IL_OFFSET)(codeAddr - info.compCode - 1);
        JITDUMP("\n    [%2u] %3u (0x%03x) ", verCurrentState.esStackDepth, impCurOpcOffs, impCurOpcOffs);
#endif

    DECODE_OPCODE:

        // Return if any previous code has caused inline to fail.
        if (compDonotInline())
        {
            return;
        }

        /* Get the size of additional parameters */

        signed int sz = opcodeSizes[opcode];

#ifdef DEBUG
        clsHnd  = NO_CLASS_HANDLE;
        lclTyp  = TYP_COUNT;
        callTyp = TYP_COUNT;

        impCurOpcOffs = (IL_OFFSET)(codeAddr - info.compCode - 1);
        impCurOpcName = opcodeNames[opcode];

        if (verbose && (opcode != CEE_PREFIX1))
        {
            printf("%s", impCurOpcName);
        }

        /* Use assertImp() to display the opcode */

        op1 = op2 = nullptr;
#endif

        /* See what kind of an opcode we have, then */

        unsigned mflags   = 0;
        unsigned clsFlags = 0;

        switch (opcode)
        {
            unsigned  lclNum;
            var_types type;

            GenTree*   op3;
            genTreeOps oper;
            unsigned   size;

            int val;

            CORINFO_SIG_INFO     sig;
            IL_OFFSET            jmpAddr;
            bool                 ovfl, unordered, callNode;
            bool                 ldstruct;
            CORINFO_CLASS_HANDLE tokenType;

            union {
                int     intVal;
                float   fltVal;
                __int64 lngVal;
                double  dblVal;
            } cval;

            case CEE_PREFIX1:
                opcode     = (OPCODE)(getU1LittleEndian(codeAddr) + 256);
                opcodeOffs = (IL_OFFSET)(codeAddr - info.compCode);
                codeAddr += sizeof(__int8);
                goto DECODE_OPCODE;

            SPILL_APPEND:

                // We need to call impSpillLclRefs() for a struct type lclVar.
                // This is because there may be loads of that lclVar on the evaluation stack, and
                // we need to ensure that those loads are completed before we modify it.
                if ((op1->OperGet() == GT_ASG) && varTypeIsStruct(op1->gtGetOp1()))
                {
                    GenTree*             lhs    = op1->gtGetOp1();
                    GenTreeLclVarCommon* lclVar = nullptr;
                    if (lhs->gtOper == GT_LCL_VAR)
                    {
                        lclVar = lhs->AsLclVarCommon();
                    }
                    else if (lhs->OperIsBlk())
                    {
                        // Check if LHS address is within some struct local, to catch
                        // cases where we're updating the struct by something other than a stfld
                        GenTree* addr = lhs->AsBlk()->Addr();

                        // Catches ADDR(LCL_VAR), or ADD(ADDR(LCL_VAR),CNS_INT))
                        lclVar = addr->IsLocalAddrExpr();

                        // Catches ADDR(FIELD(... ADDR(LCL_VAR)))
                        if (lclVar == nullptr)
                        {
                            GenTree* lclTree = nullptr;
                            if (impIsAddressInLocal(addr, &lclTree))
                            {
                                lclVar = lclTree->AsLclVarCommon();
                            }
                        }
                    }
                    if (lclVar != nullptr)
                    {
                        impSpillLclRefs(lclVar->GetLclNum());
                    }
                }

                /* Append 'op1' to the list of statements */
                impAppendTree(op1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
                goto DONE_APPEND;

            APPEND:

                /* Append 'op1' to the list of statements */

                impAppendTree(op1, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
                goto DONE_APPEND;

            DONE_APPEND:

#ifdef DEBUG
                // Remember at which BC offset the tree was finished
                impNoteLastILoffs();
#endif
                break;

            case CEE_LDNULL:
                impPushNullObjRefOnStack();
                break;

            case CEE_LDC_I4_M1:
            case CEE_LDC_I4_0:
            case CEE_LDC_I4_1:
            case CEE_LDC_I4_2:
            case CEE_LDC_I4_3:
            case CEE_LDC_I4_4:
            case CEE_LDC_I4_5:
            case CEE_LDC_I4_6:
            case CEE_LDC_I4_7:
            case CEE_LDC_I4_8:
                cval.intVal = (opcode - CEE_LDC_I4_0);
                assert(-1 <= cval.intVal && cval.intVal <= 8);
                goto PUSH_I4CON;

            case CEE_LDC_I4_S:
                cval.intVal = getI1LittleEndian(codeAddr);
                goto PUSH_I4CON;
            case CEE_LDC_I4:
                cval.intVal = getI4LittleEndian(codeAddr);
                goto PUSH_I4CON;
            PUSH_I4CON:
                JITDUMP(" %d", cval.intVal);
                impPushOnStack(gtNewIconNode(cval.intVal), typeInfo(TI_INT));
                break;

            case CEE_LDC_I8:
                cval.lngVal = getI8LittleEndian(codeAddr);
                JITDUMP(" 0x%016llx", cval.lngVal);
                impPushOnStack(gtNewLconNode(cval.lngVal), typeInfo(TI_LONG));
                break;

            case CEE_LDC_R8:
                cval.dblVal = getR8LittleEndian(codeAddr);
                JITDUMP(" %#.17g", cval.dblVal);
                impPushOnStack(gtNewDconNode(cval.dblVal), typeInfo(TI_DOUBLE));
                break;

            case CEE_LDC_R4:
                cval.dblVal = getR4LittleEndian(codeAddr);
                JITDUMP(" %#.17g", cval.dblVal);
                impPushOnStack(gtNewDconNode(cval.dblVal, TYP_FLOAT), typeInfo(TI_DOUBLE));
                break;

            case CEE_LDSTR:

                if (compIsForInlining())
                {
                    if (impInlineInfo->inlineCandidateInfo->dwRestrictions & INLINE_NO_CALLEE_LDSTR)
                    {
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_LDSTR_RESTRICTION);
                        return;
                    }
                }

                val = getU4LittleEndian(codeAddr);
                JITDUMP(" %08X", val);
                if (tiVerificationNeeded)
                {
                    Verify(info.compCompHnd->isValidStringRef(info.compScopeHnd, val), "bad string");
                    tiRetVal = typeInfo(TI_REF, impGetStringClass());
                }
                impPushOnStack(gtNewSconNode(val, info.compScopeHnd), tiRetVal);

                break;

            case CEE_LDARG:
                lclNum = getU2LittleEndian(codeAddr);
                JITDUMP(" %u", lclNum);
                impLoadArg(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_LDARG_S:
                lclNum = getU1LittleEndian(codeAddr);
                JITDUMP(" %u", lclNum);
                impLoadArg(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_LDARG_0:
            case CEE_LDARG_1:
            case CEE_LDARG_2:
            case CEE_LDARG_3:
                lclNum = (opcode - CEE_LDARG_0);
                assert(lclNum >= 0 && lclNum < 4);
                impLoadArg(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_LDLOC:
                lclNum = getU2LittleEndian(codeAddr);
                JITDUMP(" %u", lclNum);
                impLoadLoc(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_LDLOC_S:
                lclNum = getU1LittleEndian(codeAddr);
                JITDUMP(" %u", lclNum);
                impLoadLoc(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_LDLOC_0:
            case CEE_LDLOC_1:
            case CEE_LDLOC_2:
            case CEE_LDLOC_3:
                lclNum = (opcode - CEE_LDLOC_0);
                assert(lclNum >= 0 && lclNum < 4);
                impLoadLoc(lclNum, opcodeOffs + sz + 1);
                break;

            case CEE_STARG:
                lclNum = getU2LittleEndian(codeAddr);
                goto STARG;

            case CEE_STARG_S:
                lclNum = getU1LittleEndian(codeAddr);
            STARG:
                JITDUMP(" %u", lclNum);

                if (tiVerificationNeeded)
                {
                    Verify(lclNum < info.compILargsCount, "bad arg num");
                }

                if (compIsForInlining())
                {
                    op1 = impInlineFetchArg(lclNum, impInlineInfo->inlArgInfo, impInlineInfo->lclVarInfo);
                    noway_assert(op1->gtOper == GT_LCL_VAR);
                    lclNum = op1->AsLclVar()->GetLclNum();

                    goto VAR_ST_VALID;
                }

                lclNum = compMapILargNum(lclNum); // account for possible hidden param
                assertImp(lclNum < numArgs);

                if (lclNum == info.compThisArg)
                {
                    lclNum = lvaArg0Var;
                }

                // We should have seen this arg write in the prescan
                assert(lvaTable[lclNum].lvHasILStoreOp);

                if (tiVerificationNeeded)
                {
                    typeInfo& tiLclVar = lvaTable[lclNum].lvVerTypeInfo;
                    Verify(tiCompatibleWith(impStackTop().seTypeInfo, NormaliseForStack(tiLclVar), true),
                           "type mismatch");

                    if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init))
                    {
                        Verify(!tiLclVar.IsThisPtr(), "storing to uninit this ptr");
                    }
                }

                goto VAR_ST;

            case CEE_STLOC:
                lclNum  = getU2LittleEndian(codeAddr);
                isLocal = true;
                JITDUMP(" %u", lclNum);
                goto LOC_ST;

            case CEE_STLOC_S:
                lclNum  = getU1LittleEndian(codeAddr);
                isLocal = true;
                JITDUMP(" %u", lclNum);
                goto LOC_ST;

            case CEE_STLOC_0:
            case CEE_STLOC_1:
            case CEE_STLOC_2:
            case CEE_STLOC_3:
                isLocal = true;
                lclNum  = (opcode - CEE_STLOC_0);
                assert(lclNum >= 0 && lclNum < 4);

            LOC_ST:
                if (tiVerificationNeeded)
                {
                    Verify(lclNum < info.compMethodInfo->locals.numArgs, "bad local num");
                    Verify(tiCompatibleWith(impStackTop().seTypeInfo,
                                            NormaliseForStack(lvaTable[lclNum + numArgs].lvVerTypeInfo), true),
                           "type mismatch");
                }

                if (compIsForInlining())
                {
                    lclTyp = impInlineInfo->lclVarInfo[lclNum + impInlineInfo->argCnt].lclTypeInfo;

                    /* Have we allocated a temp for this local? */

                    lclNum = impInlineFetchLocal(lclNum DEBUGARG("Inline stloc first use temp"));

                    goto _PopValue;
                }

                lclNum += numArgs;

            VAR_ST:

                if (lclNum >= info.compLocalsCount && lclNum != lvaArg0Var)
                {
                    assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                    BADCODE("Bad IL");
                }

            VAR_ST_VALID:

                /* if it is a struct assignment, make certain we don't overflow the buffer */
                assert(lclTyp != TYP_STRUCT || lvaLclSize(lclNum) >= info.compCompHnd->getClassSize(clsHnd));

                if (lvaTable[lclNum].lvNormalizeOnLoad())
                {
                    lclTyp = lvaGetRealType(lclNum);
                }
                else
                {
                    lclTyp = lvaGetActualType(lclNum);
                }

            _PopValue:
                /* Pop the value being assigned */

                {
                    StackEntry se = impPopStack();
                    clsHnd        = se.seTypeInfo.GetClassHandle();
                    op1           = se.val;
                    tiRetVal      = se.seTypeInfo;
                }

#ifdef FEATURE_SIMD
                if (varTypeIsSIMD(lclTyp) && (lclTyp != op1->TypeGet()))
                {
                    assert(op1->TypeGet() == TYP_STRUCT);
                    op1->gtType = lclTyp;
                }
#endif // FEATURE_SIMD

                op1 = impImplicitIorI4Cast(op1, lclTyp);

#ifdef TARGET_64BIT
                // Downcast the TYP_I_IMPL into a 32-bit Int for x86 JIT compatiblity
                if (varTypeIsI(op1->TypeGet()) && (genActualType(lclTyp) == TYP_INT))
                {
                    assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                    op1 = gtNewCastNode(TYP_INT, op1, false, TYP_INT);
                }
#endif // TARGET_64BIT

                // We had better assign it a value of the correct type
                assertImp(
                    genActualType(lclTyp) == genActualType(op1->gtType) ||
                    (genActualType(lclTyp) == TYP_I_IMPL && op1->IsLocalAddrExpr() != nullptr) ||
                    (genActualType(lclTyp) == TYP_I_IMPL && (op1->gtType == TYP_BYREF || op1->gtType == TYP_REF)) ||
                    (genActualType(op1->gtType) == TYP_I_IMPL && lclTyp == TYP_BYREF) ||
                    (varTypeIsFloating(lclTyp) && varTypeIsFloating(op1->TypeGet())) ||
                    ((genActualType(lclTyp) == TYP_BYREF) && genActualType(op1->TypeGet()) == TYP_REF));

                /* If op1 is "&var" then its type is the transient "*" and it can
                   be used either as TYP_BYREF or TYP_I_IMPL */

                if (op1->IsLocalAddrExpr() != nullptr)
                {
                    assertImp(genActualType(lclTyp) == TYP_I_IMPL || lclTyp == TYP_BYREF);

                    /* When "&var" is created, we assume it is a byref. If it is
                       being assigned to a TYP_I_IMPL var, change the type to
                       prevent unnecessary GC info */

                    if (genActualType(lclTyp) == TYP_I_IMPL)
                    {
                        op1->gtType = TYP_I_IMPL;
                    }
                }

                // If this is a local and the local is a ref type, see
                // if we can improve type information based on the
                // value being assigned.
                if (isLocal && (lclTyp == TYP_REF))
                {
                    // We should have seen a stloc in our IL prescan.
                    assert(lvaTable[lclNum].lvHasILStoreOp);

                    // Is there just one place this local is defined?
                    const bool isSingleDefLocal = lvaTable[lclNum].lvSingleDef;

                    // Conservative check that there is just one
                    // definition that reaches this store.
                    const bool hasSingleReachingDef = (block->bbStackDepthOnEntry() == 0);

                    if (isSingleDefLocal && hasSingleReachingDef)
                    {
                        lvaUpdateClass(lclNum, op1, clsHnd);
                    }
                }

                /* Filter out simple assignments to itself */

                if (op1->gtOper == GT_LCL_VAR && lclNum == op1->AsLclVarCommon()->GetLclNum())
                {
                    if (opts.compDbgCode)
                    {
                        op1 = gtNewNothingNode();
                        goto SPILL_APPEND;
                    }
                    else
                    {
                        break;
                    }
                }

                /* Create the assignment node */

                op2 = gtNewLclvNode(lclNum, lclTyp DEBUGARG(opcodeOffs + sz + 1));

                /* If the local is aliased or pinned, we need to spill calls and
                   indirections from the stack. */

                if ((lvaTable[lclNum].lvAddrExposed || lvaTable[lclNum].lvHasLdAddrOp || lvaTable[lclNum].lvPinned) &&
                    (verCurrentState.esStackDepth > 0))
                {
                    impSpillSideEffects(false,
                                        (unsigned)CHECK_SPILL_ALL DEBUGARG("Local could be aliased or is pinned"));
                }

                /* Spill any refs to the local from the stack */

                impSpillLclRefs(lclNum);

                // We can generate an assignment to a TYP_FLOAT from a TYP_DOUBLE
                // We insert a cast to the dest 'op2' type
                //
                if ((op1->TypeGet() != op2->TypeGet()) && varTypeIsFloating(op1->gtType) &&
                    varTypeIsFloating(op2->gtType))
                {
                    op1 = gtNewCastNode(op2->TypeGet(), op1, false, op2->TypeGet());
                }

                if (varTypeIsStruct(lclTyp))
                {
                    op1 = impAssignStruct(op2, op1, clsHnd, (unsigned)CHECK_SPILL_ALL);
                }
                else
                {
                    // The code generator generates GC tracking information
                    // based on the RHS of the assignment.  Later the LHS (which is
                    // is a BYREF) gets used and the emitter checks that that variable
                    // is being tracked.  It is not (since the RHS was an int and did
                    // not need tracking).  To keep this assert happy, we change the RHS
                    if (lclTyp == TYP_BYREF && !varTypeIsGC(op1->gtType))
                    {
                        op1->gtType = TYP_BYREF;
                    }
                    op1 = gtNewAssignNode(op2, op1);
                }

                goto SPILL_APPEND;

            case CEE_LDLOCA:
                lclNum = getU2LittleEndian(codeAddr);
                goto LDLOCA;

            case CEE_LDLOCA_S:
                lclNum = getU1LittleEndian(codeAddr);
            LDLOCA:
                JITDUMP(" %u", lclNum);
                if (tiVerificationNeeded)
                {
                    Verify(lclNum < info.compMethodInfo->locals.numArgs, "bad local num");
                    Verify(info.compInitMem, "initLocals not set");
                }

                if (compIsForInlining())
                {
                    // Get the local type
                    lclTyp = impInlineInfo->lclVarInfo[lclNum + impInlineInfo->argCnt].lclTypeInfo;

                    /* Have we allocated a temp for this local? */

                    lclNum = impInlineFetchLocal(lclNum DEBUGARG("Inline ldloca(s) first use temp"));

                    op1 = gtNewLclvNode(lclNum, lvaGetActualType(lclNum));

                    goto _PUSH_ADRVAR;
                }

                lclNum += numArgs;
                assertImp(lclNum < info.compLocalsCount);
                goto ADRVAR;

            case CEE_LDARGA:
                lclNum = getU2LittleEndian(codeAddr);
                goto LDARGA;

            case CEE_LDARGA_S:
                lclNum = getU1LittleEndian(codeAddr);
            LDARGA:
                JITDUMP(" %u", lclNum);
                Verify(lclNum < info.compILargsCount, "bad arg num");

                if (compIsForInlining())
                {
                    // In IL, LDARGA(_S) is used to load the byref managed pointer of struct argument,
                    // followed by a ldfld to load the field.

                    op1 = impInlineFetchArg(lclNum, impInlineInfo->inlArgInfo, impInlineInfo->lclVarInfo);
                    if (op1->gtOper != GT_LCL_VAR)
                    {
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_LDARGA_NOT_LOCAL_VAR);
                        return;
                    }

                    assert(op1->gtOper == GT_LCL_VAR);

                    goto _PUSH_ADRVAR;
                }

                lclNum = compMapILargNum(lclNum); // account for possible hidden param
                assertImp(lclNum < numArgs);

                if (lclNum == info.compThisArg)
                {
                    lclNum = lvaArg0Var;
                }

                goto ADRVAR;

            ADRVAR:

                op1 = gtNewLclvNode(lclNum, lvaGetActualType(lclNum) DEBUGARG(opcodeOffs + sz + 1));

            _PUSH_ADRVAR:
                assert(op1->gtOper == GT_LCL_VAR);

                /* Note that this is supposed to create the transient type "*"
                   which may be used as a TYP_I_IMPL. However we catch places
                   where it is used as a TYP_I_IMPL and change the node if needed.
                   Thus we are pessimistic and may report byrefs in the GC info
                   where it was not absolutely needed, but it is safer this way.
                 */
                op1 = gtNewOperNode(GT_ADDR, TYP_BYREF, op1);

                // &aliasedVar doesnt need GTF_GLOB_REF, though alisasedVar does
                assert((op1->gtFlags & GTF_GLOB_REF) == 0);

                tiRetVal = lvaTable[lclNum].lvVerTypeInfo;
                if (tiVerificationNeeded)
                {
                    // Don't allow taking address of uninit this ptr.
                    if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init))
                    {
                        Verify(!tiRetVal.IsThisPtr(), "address of uninit this ptr");
                    }

                    if (!tiRetVal.IsByRef())
                    {
                        tiRetVal.MakeByRef();
                    }
                    else
                    {
                        Verify(false, "byref to byref");
                    }
                }

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_ARGLIST:

                if (!info.compIsVarArgs)
                {
                    BADCODE("arglist in non-vararg method");
                }

                if (tiVerificationNeeded)
                {
                    tiRetVal = typeInfo(TI_STRUCT, impGetRuntimeArgumentHandle());
                }
                assertImp((info.compMethodInfo->args.callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG);

                /* The ARGLIST cookie is a hidden 'last' parameter, we have already
                   adjusted the arg count cos this is like fetching the last param */
                assertImp(0 < numArgs);
                assert(lvaTable[lvaVarargsHandleArg].lvAddrExposed);
                lclNum = lvaVarargsHandleArg;
                op1    = gtNewLclvNode(lclNum, TYP_I_IMPL DEBUGARG(opcodeOffs + sz + 1));
                op1    = gtNewOperNode(GT_ADDR, TYP_BYREF, op1);
                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_ENDFINALLY:

                if (compIsForInlining())
                {
                    assert(!"Shouldn't have exception handlers in the inliner!");
                    compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_ENDFINALLY);
                    return;
                }

                if (verCurrentState.esStackDepth > 0)
                {
                    impEvalSideEffects();
                }

                if (info.compXcptnsCount == 0)
                {
                    BADCODE("endfinally outside finally");
                }

                assert(verCurrentState.esStackDepth == 0);

                op1 = gtNewOperNode(GT_RETFILT, TYP_VOID, nullptr);
                goto APPEND;

            case CEE_ENDFILTER:

                if (compIsForInlining())
                {
                    assert(!"Shouldn't have exception handlers in the inliner!");
                    compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_ENDFILTER);
                    return;
                }

                block->bbSetRunRarely(); // filters are rare

                if (info.compXcptnsCount == 0)
                {
                    BADCODE("endfilter outside filter");
                }

                if (tiVerificationNeeded)
                {
                    Verify(impStackTop().seTypeInfo.IsType(TI_INT), "bad endfilt arg");
                }

                op1 = impPopStack().val;
                assertImp(op1->gtType == TYP_INT);
                if (!bbInFilterILRange(block))
                {
                    BADCODE("EndFilter outside a filter handler");
                }

                /* Mark current bb as end of filter */

                assert(compCurBB->bbFlags & BBF_DONT_REMOVE);
                assert(compCurBB->bbJumpKind == BBJ_EHFILTERRET);

                /* Mark catch handler as successor */

                op1 = gtNewOperNode(GT_RETFILT, op1->TypeGet(), op1);
                if (verCurrentState.esStackDepth != 0)
                {
                    verRaiseVerifyException(INDEBUG("stack must be 1 on end of filter") DEBUGARG(__FILE__)
                                                DEBUGARG(__LINE__));
                }
                goto APPEND;

            case CEE_RET:
                prefixFlags &= ~PREFIX_TAILCALL; // ret without call before it
            RET:
                if (!impReturnInstruction(prefixFlags, opcode))
                {
                    return; // abort
                }
                else
                {
                    break;
                }

            case CEE_JMP:

                assert(!compIsForInlining());

                if (tiVerificationNeeded)
                {
                    Verify(false, "Invalid opcode: CEE_JMP");
                }

                if ((info.compFlags & CORINFO_FLG_SYNCH) || block->hasTryIndex() || block->hasHndIndex())
                {
                    /* CEE_JMP does not make sense in some "protected" regions. */

                    BADCODE("Jmp not allowed in protected region");
                }

                if (opts.IsReversePInvoke())
                {
                    BADCODE("Jmp not allowed in reverse P/Invoke");
                }

                if (verCurrentState.esStackDepth != 0)
                {
                    BADCODE("Stack must be empty after CEE_JMPs");
                }

                _impResolveToken(CORINFO_TOKENKIND_Method);

                JITDUMP(" %08X", resolvedToken.token);

                /* The signature of the target has to be identical to ours.
                   At least check that argCnt and returnType match */

                eeGetMethodSig(resolvedToken.hMethod, &sig);
                if (sig.numArgs != info.compMethodInfo->args.numArgs ||
                    sig.retType != info.compMethodInfo->args.retType ||
                    sig.callConv != info.compMethodInfo->args.callConv)
                {
                    BADCODE("Incompatible target for CEE_JMPs");
                }

                op1 = new (this, GT_JMP) GenTreeVal(GT_JMP, TYP_VOID, (size_t)resolvedToken.hMethod);

                /* Mark the basic block as being a JUMP instead of RETURN */

                block->bbFlags |= BBF_HAS_JMP;

                /* Set this flag to make sure register arguments have a location assigned
                 * even if we don't use them inside the method */

                compJmpOpUsed = true;

                fgNoStructPromotion = true;

                goto APPEND;

            case CEE_LDELEMA:
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                ldelemClsHnd = resolvedToken.hClass;

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(1).seTypeInfo;
                    typeInfo tiIndex = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");

                    typeInfo arrayElemType = verMakeTypeInfo(ldelemClsHnd);
                    Verify(tiArray.IsNullObjRef() ||
                               typeInfo::AreEquivalent(verGetArrayElemType(tiArray), arrayElemType),
                           "bad array");

                    tiRetVal = arrayElemType;
                    tiRetVal.MakeByRef();
                    if (prefixFlags & PREFIX_READONLY)
                    {
                        tiRetVal.SetIsReadonlyByRef();
                    }

                    // an array interior pointer is always in the heap
                    tiRetVal.SetIsPermanentHomeByRef();
                }

                // If it's a value class array we just do a simple address-of
                if (eeIsValueClass(ldelemClsHnd))
                {
                    CorInfoType cit = info.compCompHnd->getTypeForPrimitiveValueClass(ldelemClsHnd);
                    if (cit == CORINFO_TYPE_UNDEF)
                    {
                        lclTyp = TYP_STRUCT;
                    }
                    else
                    {
                        lclTyp = JITtype2varType(cit);
                    }
                    goto ARR_LD_POST_VERIFY;
                }

                // Similarly, if its a readonly access, we can do a simple address-of
                // without doing a runtime type-check
                if (prefixFlags & PREFIX_READONLY)
                {
                    lclTyp = TYP_REF;
                    goto ARR_LD_POST_VERIFY;
                }

                // Otherwise we need the full helper function with run-time type check
                op1 = impTokenToHandle(&resolvedToken);
                if (op1 == nullptr)
                { // compDonotInline()
                    return;
                }

                {
                    GenTreeCall::Use* args = gtNewCallArgs(op1);                           // Type
                    args                   = gtPrependNewCallArg(impPopStack().val, args); // index
                    args                   = gtPrependNewCallArg(impPopStack().val, args); // array
                    op1                    = gtNewHelperCallNode(CORINFO_HELP_LDELEMA_REF, TYP_BYREF, args);
                }

                impPushOnStack(op1, tiRetVal);
                break;

            // ldelem for reference and value types
            case CEE_LDELEM:
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                ldelemClsHnd = resolvedToken.hClass;

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(1).seTypeInfo;
                    typeInfo tiIndex = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");
                    tiRetVal = verMakeTypeInfo(ldelemClsHnd);

                    Verify(tiArray.IsNullObjRef() || tiCompatibleWith(verGetArrayElemType(tiArray), tiRetVal, false),
                           "type of array incompatible with type operand");
                    tiRetVal.NormaliseForStack();
                }

                // If it's a reference type or generic variable type
                // then just generate code as though it's a ldelem.ref instruction
                if (!eeIsValueClass(ldelemClsHnd))
                {
                    lclTyp = TYP_REF;
                    opcode = CEE_LDELEM_REF;
                }
                else
                {
                    CorInfoType jitTyp = info.compCompHnd->asCorInfoType(ldelemClsHnd);
                    lclTyp             = JITtype2varType(jitTyp);
                    tiRetVal           = verMakeTypeInfo(ldelemClsHnd); // precise type always needed for struct
                    tiRetVal.NormaliseForStack();
                }
                goto ARR_LD_POST_VERIFY;

            case CEE_LDELEM_I1:
                lclTyp = TYP_BYTE;
                goto ARR_LD;
            case CEE_LDELEM_I2:
                lclTyp = TYP_SHORT;
                goto ARR_LD;
            case CEE_LDELEM_I:
                lclTyp = TYP_I_IMPL;
                goto ARR_LD;

            // Should be UINT, but since no platform widens 4->8 bytes it doesn't matter
            // and treating it as TYP_INT avoids other asserts.
            case CEE_LDELEM_U4:
                lclTyp = TYP_INT;
                goto ARR_LD;

            case CEE_LDELEM_I4:
                lclTyp = TYP_INT;
                goto ARR_LD;
            case CEE_LDELEM_I8:
                lclTyp = TYP_LONG;
                goto ARR_LD;
            case CEE_LDELEM_REF:
                lclTyp = TYP_REF;
                goto ARR_LD;
            case CEE_LDELEM_R4:
                lclTyp = TYP_FLOAT;
                goto ARR_LD;
            case CEE_LDELEM_R8:
                lclTyp = TYP_DOUBLE;
                goto ARR_LD;
            case CEE_LDELEM_U1:
                lclTyp = TYP_UBYTE;
                goto ARR_LD;
            case CEE_LDELEM_U2:
                lclTyp = TYP_USHORT;
                goto ARR_LD;

            ARR_LD:

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(1).seTypeInfo;
                    typeInfo tiIndex = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");
                    if (tiArray.IsNullObjRef())
                    {
                        if (lclTyp == TYP_REF)
                        { // we will say a deref of a null array yields a null ref
                            tiRetVal = typeInfo(TI_NULL);
                        }
                        else
                        {
                            tiRetVal = typeInfo(lclTyp);
                        }
                    }
                    else
                    {
                        tiRetVal             = verGetArrayElemType(tiArray);
                        typeInfo arrayElemTi = typeInfo(lclTyp);
#ifdef TARGET_64BIT
                        if (opcode == CEE_LDELEM_I)
                        {
                            arrayElemTi = typeInfo::nativeInt();
                        }

                        if (lclTyp != TYP_REF && lclTyp != TYP_STRUCT)
                        {
                            Verify(typeInfo::AreEquivalent(tiRetVal, arrayElemTi), "bad array");
                        }
                        else
#endif // TARGET_64BIT
                        {
                            Verify(tiRetVal.IsType(arrayElemTi.GetType()), "bad array");
                        }
                    }
                    tiRetVal.NormaliseForStack();
                }
            ARR_LD_POST_VERIFY:

                /* Pull the index value and array address */
                op2 = impPopStack().val;
                op1 = impPopStack().val;
                assertImp(op1->gtType == TYP_REF);

                /* Check for null pointer - in the inliner case we simply abort */

                if (compIsForInlining())
                {
                    if (op1->gtOper == GT_CNS_INT)
                    {
                        compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_NULL_FOR_LDELEM);
                        return;
                    }
                }

                op1 = impCheckForNullPointer(op1);

                /* Mark the block as containing an index expression */

                if (op1->gtOper == GT_LCL_VAR)
                {
                    if (op2->gtOper == GT_LCL_VAR || op2->gtOper == GT_CNS_INT || op2->gtOper == GT_ADD)
                    {
                        block->bbFlags |= BBF_HAS_IDX_LEN;
                        optMethodFlags |= OMF_HAS_ARRAYREF;
                    }
                }

                /* Create the index node and push it on the stack */

                op1 = gtNewIndexRef(lclTyp, op1, op2);

                ldstruct = (opcode == CEE_LDELEM && lclTyp == TYP_STRUCT);

                if ((opcode == CEE_LDELEMA) || ldstruct ||
                    (ldelemClsHnd != DUMMY_INIT(NULL) && eeIsValueClass(ldelemClsHnd)))
                {
                    assert(ldelemClsHnd != DUMMY_INIT(NULL));

                    // remember the element size
                    if (lclTyp == TYP_REF)
                    {
                        op1->AsIndex()->gtIndElemSize = TARGET_POINTER_SIZE;
                    }
                    else
                    {
                        // If ldElemClass is precisely a primitive type, use that, otherwise, preserve the struct type.
                        if (info.compCompHnd->getTypeForPrimitiveValueClass(ldelemClsHnd) == CORINFO_TYPE_UNDEF)
                        {
                            op1->AsIndex()->gtStructElemClass = ldelemClsHnd;
                        }
                        assert(lclTyp != TYP_STRUCT || op1->AsIndex()->gtStructElemClass != nullptr);
                        if (lclTyp == TYP_STRUCT)
                        {
                            size                          = info.compCompHnd->getClassSize(ldelemClsHnd);
                            op1->AsIndex()->gtIndElemSize = size;
                            op1->gtType                   = lclTyp;
                        }
                    }

                    if ((opcode == CEE_LDELEMA) || ldstruct)
                    {
                        // wrap it in a &
                        lclTyp = TYP_BYREF;

                        op1 = gtNewOperNode(GT_ADDR, lclTyp, op1);
                    }
                    else
                    {
                        assert(lclTyp != TYP_STRUCT);
                    }
                }

                if (ldstruct)
                {
                    // Create an OBJ for the result
                    op1 = gtNewObjNode(ldelemClsHnd, op1);
                    op1->gtFlags |= GTF_EXCEPT;
                }
                impPushOnStack(op1, tiRetVal);
                break;

            // stelem for reference and value types
            case CEE_STELEM:

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                stelemClsHnd = resolvedToken.hClass;

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(2).seTypeInfo;
                    typeInfo tiIndex = impStackTop(1).seTypeInfo;
                    typeInfo tiValue = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");
                    typeInfo arrayElem = verMakeTypeInfo(stelemClsHnd);

                    Verify(tiArray.IsNullObjRef() || tiCompatibleWith(arrayElem, verGetArrayElemType(tiArray), false),
                           "type operand incompatible with array element type");
                    arrayElem.NormaliseForStack();
                    Verify(tiCompatibleWith(tiValue, arrayElem, true), "value incompatible with type operand");
                }

                // If it's a reference type just behave as though it's a stelem.ref instruction
                if (!eeIsValueClass(stelemClsHnd))
                {
                    goto STELEM_REF_POST_VERIFY;
                }

                // Otherwise extract the type
                {
                    CorInfoType jitTyp = info.compCompHnd->asCorInfoType(stelemClsHnd);
                    lclTyp             = JITtype2varType(jitTyp);
                    goto ARR_ST_POST_VERIFY;
                }

            case CEE_STELEM_REF:

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(2).seTypeInfo;
                    typeInfo tiIndex = impStackTop(1).seTypeInfo;
                    typeInfo tiValue = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");
                    Verify(tiValue.IsObjRef(), "bad value");

                    // we only check that it is an object referece, The helper does additional checks
                    Verify(tiArray.IsNullObjRef() || verGetArrayElemType(tiArray).IsType(TI_REF), "bad array");
                }

            STELEM_REF_POST_VERIFY:

                if (opts.OptimizationEnabled())
                {
                    GenTree* array = impStackTop(2).val;
                    GenTree* value = impStackTop().val;

                    // Is this a case where we can skip the covariant store check?
                    if (impCanSkipCovariantStoreCheck(value, array))
                    {
                        lclTyp = TYP_REF;
                        goto ARR_ST_POST_VERIFY;
                    }
                }

                // Else call a helper function to do the assignment
                op1 = gtNewHelperCallNode(CORINFO_HELP_ARRADDR_ST, TYP_VOID, impPopCallArgs(3, nullptr));
                goto SPILL_APPEND;

            case CEE_STELEM_I1:
                lclTyp = TYP_BYTE;
                goto ARR_ST;
            case CEE_STELEM_I2:
                lclTyp = TYP_SHORT;
                goto ARR_ST;
            case CEE_STELEM_I:
                lclTyp = TYP_I_IMPL;
                goto ARR_ST;
            case CEE_STELEM_I4:
                lclTyp = TYP_INT;
                goto ARR_ST;
            case CEE_STELEM_I8:
                lclTyp = TYP_LONG;
                goto ARR_ST;
            case CEE_STELEM_R4:
                lclTyp = TYP_FLOAT;
                goto ARR_ST;
            case CEE_STELEM_R8:
                lclTyp = TYP_DOUBLE;
                goto ARR_ST;

            ARR_ST:

                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop(2).seTypeInfo;
                    typeInfo tiIndex = impStackTop(1).seTypeInfo;
                    typeInfo tiValue = impStackTop().seTypeInfo;

                    // As per ECMA 'index' specified can be either int32 or native int.
                    Verify(tiIndex.IsIntOrNativeIntType(), "bad index");
                    typeInfo arrayElem = typeInfo(lclTyp);
#ifdef TARGET_64BIT
                    if (opcode == CEE_STELEM_I)
                    {
                        arrayElem = typeInfo::nativeInt();
                    }
#endif // TARGET_64BIT
                    Verify(tiArray.IsNullObjRef() || typeInfo::AreEquivalent(verGetArrayElemType(tiArray), arrayElem),
                           "bad array");

                    Verify(tiCompatibleWith(NormaliseForStack(tiValue), arrayElem.NormaliseForStack(), true),
                           "bad value");
                }

            ARR_ST_POST_VERIFY:
                /* The strict order of evaluation is LHS-operands, RHS-operands,
                   range-check, and then assignment. However, codegen currently
                   does the range-check before evaluation the RHS-operands. So to
                   maintain strict ordering, we spill the stack. */

                if (impStackTop().val->gtFlags & GTF_SIDE_EFFECT)
                {
                    impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG(
                                                   "Strict ordering of exceptions for Array store"));
                }

                /* Pull the new value from the stack */
                op2 = impPopStack().val;

                /* Pull the index value */
                op1 = impPopStack().val;

                /* Pull the array address */
                op3 = impPopStack().val;

                assertImp(op3->gtType == TYP_REF);
                if (op2->IsLocalAddrExpr() != nullptr)
                {
                    op2->gtType = TYP_I_IMPL;
                }

                op3 = impCheckForNullPointer(op3);

                // Mark the block as containing an index expression

                if (op3->gtOper == GT_LCL_VAR)
                {
                    if (op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_CNS_INT || op1->gtOper == GT_ADD)
                    {
                        block->bbFlags |= BBF_HAS_IDX_LEN;
                        optMethodFlags |= OMF_HAS_ARRAYREF;
                    }
                }

                /* Create the index node */

                op1 = gtNewIndexRef(lclTyp, op3, op1);

                /* Create the assignment node and append it */

                if (lclTyp == TYP_STRUCT)
                {
                    assert(stelemClsHnd != DUMMY_INIT(NULL));

                    op1->AsIndex()->gtStructElemClass = stelemClsHnd;
                    op1->AsIndex()->gtIndElemSize     = info.compCompHnd->getClassSize(stelemClsHnd);
                }
                if (varTypeIsStruct(op1))
                {
                    op1 = impAssignStruct(op1, op2, stelemClsHnd, (unsigned)CHECK_SPILL_ALL);
                }
                else
                {
                    op2 = impImplicitR4orR8Cast(op2, op1->TypeGet());
                    op1 = gtNewAssignNode(op1, op2);
                }

                /* Mark the expression as containing an assignment */

                op1->gtFlags |= GTF_ASG;

                goto SPILL_APPEND;

            case CEE_ADD:
                oper = GT_ADD;
                goto MATH_OP2;

            case CEE_ADD_OVF:
                uns = false;
                goto ADD_OVF;
            case CEE_ADD_OVF_UN:
                uns = true;
                goto ADD_OVF;

            ADD_OVF:
                ovfl     = true;
                callNode = false;
                oper     = GT_ADD;
                goto MATH_OP2_FLAGS;

            case CEE_SUB:
                oper = GT_SUB;
                goto MATH_OP2;

            case CEE_SUB_OVF:
                uns = false;
                goto SUB_OVF;
            case CEE_SUB_OVF_UN:
                uns = true;
                goto SUB_OVF;

            SUB_OVF:
                ovfl     = true;
                callNode = false;
                oper     = GT_SUB;
                goto MATH_OP2_FLAGS;

            case CEE_MUL:
                oper = GT_MUL;
                goto MATH_MAYBE_CALL_NO_OVF;

            case CEE_MUL_OVF:
                uns = false;
                goto MUL_OVF;
            case CEE_MUL_OVF_UN:
                uns = true;
                goto MUL_OVF;

            MUL_OVF:
                ovfl = true;
                oper = GT_MUL;
                goto MATH_MAYBE_CALL_OVF;

            // Other binary math operations

            case CEE_DIV:
                oper = GT_DIV;
                goto MATH_MAYBE_CALL_NO_OVF;

            case CEE_DIV_UN:
                oper = GT_UDIV;
                goto MATH_MAYBE_CALL_NO_OVF;

            case CEE_REM:
                oper = GT_MOD;
                goto MATH_MAYBE_CALL_NO_OVF;

            case CEE_REM_UN:
                oper = GT_UMOD;
                goto MATH_MAYBE_CALL_NO_OVF;

            MATH_MAYBE_CALL_NO_OVF:
                ovfl = false;
            MATH_MAYBE_CALL_OVF:
                // Morpher has some complex logic about when to turn different
                // typed nodes on different platforms into helper calls. We
                // need to either duplicate that logic here, or just
                // pessimistically make all the nodes large enough to become
                // call nodes.  Since call nodes aren't that much larger and
                // these opcodes are infrequent enough I chose the latter.
                callNode = true;
                goto MATH_OP2_FLAGS;

            case CEE_AND:
                oper = GT_AND;
                goto MATH_OP2;
            case CEE_OR:
                oper = GT_OR;
                goto MATH_OP2;
            case CEE_XOR:
                oper = GT_XOR;
                goto MATH_OP2;

            MATH_OP2: // For default values of 'ovfl' and 'callNode'

                ovfl     = false;
                callNode = false;

            MATH_OP2_FLAGS: // If 'ovfl' and 'callNode' have already been set

                /* Pull two values and push back the result */

                if (tiVerificationNeeded)
                {
                    const typeInfo& tiOp1 = impStackTop(1).seTypeInfo;
                    const typeInfo& tiOp2 = impStackTop().seTypeInfo;

                    Verify(tiCompatibleWith(tiOp1, tiOp2, true), "different arg type");
                    if (oper == GT_ADD || oper == GT_DIV || oper == GT_SUB || oper == GT_MUL || oper == GT_MOD)
                    {
                        Verify(tiOp1.IsNumberType(), "not number");
                    }
                    else
                    {
                        Verify(tiOp1.IsIntegerType(), "not integer");
                    }

                    Verify(!ovfl || tiOp1.IsIntegerType(), "not integer");

                    tiRetVal = tiOp1;

#ifdef TARGET_64BIT
                    if (tiOp2.IsNativeIntType())
                    {
                        tiRetVal = tiOp2;
                    }
#endif // TARGET_64BIT
                }

                op2 = impPopStack().val;
                op1 = impPopStack().val;

                /* Can't do arithmetic with references */
                assertImp(genActualType(op1->TypeGet()) != TYP_REF && genActualType(op2->TypeGet()) != TYP_REF);

                // Change both to TYP_I_IMPL (impBashVarAddrsToI won't change if its a true byref, only
                // if it is in the stack)
                impBashVarAddrsToI(op1, op2);

                type = impGetByRefResultType(oper, uns, &op1, &op2);

                assert(!ovfl || !varTypeIsFloating(op1->gtType));

                /* Special case: "int+0", "int-0", "int*1", "int/1" */

                if (op2->gtOper == GT_CNS_INT)
                {
                    if ((op2->IsIntegralConst(0) && (oper == GT_ADD || oper == GT_SUB)) ||
                        (op2->IsIntegralConst(1) && (oper == GT_MUL || oper == GT_DIV)))

                    {
                        impPushOnStack(op1, tiRetVal);
                        break;
                    }
                }

                // We can generate a TYP_FLOAT operation that has a TYP_DOUBLE operand
                //
                if (varTypeIsFloating(type) && varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType))
                {
                    if (op1->TypeGet() != type)
                    {
                        // We insert a cast of op1 to 'type'
                        op1 = gtNewCastNode(type, op1, false, type);
                    }
                    if (op2->TypeGet() != type)
                    {
                        // We insert a cast of op2 to 'type'
                        op2 = gtNewCastNode(type, op2, false, type);
                    }
                }

                if (callNode)
                {
                    /* These operators can later be transformed into 'GT_CALL' */

                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_MUL]);
#ifndef TARGET_ARM
                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_DIV]);
                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_UDIV]);
                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_MOD]);
                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_UMOD]);
#endif
                    // It's tempting to use LargeOpOpcode() here, but this logic is *not* saying
                    // that we'll need to transform into a general large node, but rather specifically
                    // to a call: by doing it this way, things keep working if there are multiple sizes,
                    // and a CALL is no longer the largest.
                    // That said, as of now it *is* a large node, so we'll do this with an assert rather
                    // than an "if".
                    assert(GenTree::s_gtNodeSizes[GT_CALL] == TREE_NODE_SZ_LARGE);
                    op1 = new (this, GT_CALL) GenTreeOp(oper, type, op1, op2 DEBUGARG(/*largeNode*/ true));
                }
                else
                {
                    op1 = gtNewOperNode(oper, type, op1, op2);
                }

                /* Special case: integer/long division may throw an exception */

                if (varTypeIsIntegral(op1->TypeGet()) && op1->OperMayThrow(this))
                {
                    op1->gtFlags |= GTF_EXCEPT;
                }

                if (ovfl)
                {
                    assert(oper == GT_ADD || oper == GT_SUB || oper == GT_MUL);
                    if (ovflType != TYP_UNKNOWN)
                    {
                        op1->gtType = ovflType;
                    }
                    op1->gtFlags |= (GTF_EXCEPT | GTF_OVERFLOW);
                    if (uns)
                    {
                        op1->gtFlags |= GTF_UNSIGNED;
                    }
                }

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_SHL:
                oper = GT_LSH;
                goto CEE_SH_OP2;

            case CEE_SHR:
                oper = GT_RSH;
                goto CEE_SH_OP2;
            case CEE_SHR_UN:
                oper = GT_RSZ;
                goto CEE_SH_OP2;

            CEE_SH_OP2:
                if (tiVerificationNeeded)
                {
                    const typeInfo& tiVal   = impStackTop(1).seTypeInfo;
                    const typeInfo& tiShift = impStackTop(0).seTypeInfo;
                    Verify(tiVal.IsIntegerType() && tiShift.IsType(TI_INT), "Bad shift args");
                    tiRetVal = tiVal;
                }
                op2 = impPopStack().val;
                op1 = impPopStack().val; // operand to be shifted
                impBashVarAddrsToI(op1, op2);

                type = genActualType(op1->TypeGet());
                op1  = gtNewOperNode(oper, type, op1, op2);

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_NOT:
                if (tiVerificationNeeded)
                {
                    tiRetVal = impStackTop().seTypeInfo;
                    Verify(tiRetVal.IsIntegerType(), "bad int value");
                }

                op1 = impPopStack().val;
                impBashVarAddrsToI(op1, nullptr);
                type = genActualType(op1->TypeGet());
                impPushOnStack(gtNewOperNode(GT_NOT, type, op1), tiRetVal);
                break;

            case CEE_CKFINITE:
                if (tiVerificationNeeded)
                {
                    tiRetVal = impStackTop().seTypeInfo;
                    Verify(tiRetVal.IsType(TI_DOUBLE), "bad R value");
                }
                op1  = impPopStack().val;
                type = op1->TypeGet();
                op1  = gtNewOperNode(GT_CKFINITE, type, op1);
                op1->gtFlags |= GTF_EXCEPT;

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_LEAVE:

                val     = getI4LittleEndian(codeAddr); // jump distance
                jmpAddr = (IL_OFFSET)((codeAddr - info.compCode + sizeof(__int32)) + val);
                goto LEAVE;

            case CEE_LEAVE_S:
                val     = getI1LittleEndian(codeAddr); // jump distance
                jmpAddr = (IL_OFFSET)((codeAddr - info.compCode + sizeof(__int8)) + val);

            LEAVE:

                if (compIsForInlining())
                {
                    compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_LEAVE);
                    return;
                }

                JITDUMP(" %04X", jmpAddr);
                if (block->bbJumpKind != BBJ_LEAVE)
                {
                    impResetLeaveBlock(block, jmpAddr);
                }

                assert(jmpAddr == block->bbJumpDest->bbCodeOffs);
                impImportLeave(block);
                impNoteBranchOffs();

                break;

            case CEE_BR:
            case CEE_BR_S:
                jmpDist = (sz == 1) ? getI1LittleEndian(codeAddr) : getI4LittleEndian(codeAddr);

                if (compIsForInlining() && jmpDist == 0)
                {
                    break; /* NOP */
                }

                impNoteBranchOffs();
                break;

            case CEE_BRTRUE:
            case CEE_BRTRUE_S:
            case CEE_BRFALSE:
            case CEE_BRFALSE_S:

                /* Pop the comparand (now there's a neat term) from the stack */
                if (tiVerificationNeeded)
                {
                    typeInfo& tiVal = impStackTop().seTypeInfo;
                    Verify(tiVal.IsObjRef() || tiVal.IsByRef() || tiVal.IsIntegerType() || tiVal.IsMethod(),
                           "bad value");
                }

                op1  = impPopStack().val;
                type = op1->TypeGet();

                // Per Ecma-355, brfalse and brtrue are only specified for nint, ref, and byref.
                //
                // We've historically been a bit more permissive, so here we allow
                // any type that gtNewZeroConNode can handle.
                if (!varTypeIsArithmetic(type) && !varTypeIsGC(type))
                {
                    BADCODE("invalid type for brtrue/brfalse");
                }

                if (opts.OptimizationEnabled() && (block->bbJumpDest == block->bbNext))
                {
                    block->bbJumpKind = BBJ_NONE;

                    if (op1->gtFlags & GTF_GLOB_EFFECT)
                    {
                        op1 = gtUnusedValNode(op1);
                        goto SPILL_APPEND;
                    }
                    else
                    {
                        break;
                    }
                }

                if (op1->OperIsCompare())
                {
                    if (opcode == CEE_BRFALSE || opcode == CEE_BRFALSE_S)
                    {
                        // Flip the sense of the compare

                        op1 = gtReverseCond(op1);
                    }
                }
                else
                {
                    // We'll compare against an equally-sized integer 0
                    // For small types, we always compare against int
                    op2 = gtNewZeroConNode(genActualType(op1->gtType));

                    // Create the comparison operator and try to fold it
                    oper = (opcode == CEE_BRTRUE || opcode == CEE_BRTRUE_S) ? GT_NE : GT_EQ;
                    op1  = gtNewOperNode(oper, TYP_INT, op1, op2);
                }

            // fall through

            COND_JUMP:

                /* Fold comparison if we can */

                op1 = gtFoldExpr(op1);

                /* Try to fold the really simple cases like 'iconst *, ifne/ifeq'*/
                /* Don't make any blocks unreachable in import only mode */

                if ((op1->gtOper == GT_CNS_INT) && !compIsForImportOnly())
                {
                    /* gtFoldExpr() should prevent this as we don't want to make any blocks
                       unreachable under compDbgCode */
                    assert(!opts.compDbgCode);

                    BBjumpKinds foldedJumpKind = (BBjumpKinds)(op1->AsIntCon()->gtIconVal ? BBJ_ALWAYS : BBJ_NONE);
                    assertImp((block->bbJumpKind == BBJ_COND)            // normal case
                              || (block->bbJumpKind == foldedJumpKind)); // this can happen if we are reimporting the
                                                                         // block for the second time

                    block->bbJumpKind = foldedJumpKind;
#ifdef DEBUG
                    if (verbose)
                    {
                        if (op1->AsIntCon()->gtIconVal)
                        {
                            printf("\nThe conditional jump becomes an unconditional jump to " FMT_BB "\n",
                                   block->bbJumpDest->bbNum);
                        }
                        else
                        {
                            printf("\nThe block falls through into the next " FMT_BB "\n", block->bbNext->bbNum);
                        }
                    }
#endif
                    break;
                }

                op1 = gtNewOperNode(GT_JTRUE, TYP_VOID, op1);

                /* GT_JTRUE is handled specially for non-empty stacks. See 'addStmt'
                   in impImportBlock(block). For correct line numbers, spill stack. */

                if (opts.compDbgCode && impCurStmtOffs != BAD_IL_OFFSET)
                {
                    impSpillStackEnsure(true);
                }

                goto SPILL_APPEND;

            case CEE_CEQ:
                oper = GT_EQ;
                uns  = false;
                goto CMP_2_OPs;
            case CEE_CGT_UN:
                oper = GT_GT;
                uns  = true;
                goto CMP_2_OPs;
            case CEE_CGT:
                oper = GT_GT;
                uns  = false;
                goto CMP_2_OPs;
            case CEE_CLT_UN:
                oper = GT_LT;
                uns  = true;
                goto CMP_2_OPs;
            case CEE_CLT:
                oper = GT_LT;
                uns  = false;
                goto CMP_2_OPs;

            CMP_2_OPs:
                if (tiVerificationNeeded)
                {
                    verVerifyCond(impStackTop(1).seTypeInfo, impStackTop().seTypeInfo, opcode);
                    tiRetVal = typeInfo(TI_INT);
                }

                op2 = impPopStack().val;
                op1 = impPopStack().val;

#ifdef TARGET_64BIT
                if (varTypeIsI(op1->TypeGet()) && (genActualType(op2->TypeGet()) == TYP_INT))
                {
                    op2 = gtNewCastNode(TYP_I_IMPL, op2, uns, uns ? TYP_U_IMPL : TYP_I_IMPL);
                }
                else if (varTypeIsI(op2->TypeGet()) && (genActualType(op1->TypeGet()) == TYP_INT))
                {
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, uns, uns ? TYP_U_IMPL : TYP_I_IMPL);
                }
#endif // TARGET_64BIT

                assertImp(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
                          (varTypeIsI(op1->TypeGet()) && varTypeIsI(op2->TypeGet())) ||
                          (varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType)));

                /* Create the comparison node */

                op1 = gtNewOperNode(oper, TYP_INT, op1, op2);

                /* TODO: setting both flags when only one is appropriate */
                if (opcode == CEE_CGT_UN || opcode == CEE_CLT_UN)
                {
                    op1->gtFlags |= GTF_RELOP_NAN_UN | GTF_UNSIGNED;
                }

                // Fold result, if possible.
                op1 = gtFoldExpr(op1);

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_BEQ_S:
            case CEE_BEQ:
                oper = GT_EQ;
                goto CMP_2_OPs_AND_BR;

            case CEE_BGE_S:
            case CEE_BGE:
                oper = GT_GE;
                goto CMP_2_OPs_AND_BR;

            case CEE_BGE_UN_S:
            case CEE_BGE_UN:
                oper = GT_GE;
                goto CMP_2_OPs_AND_BR_UN;

            case CEE_BGT_S:
            case CEE_BGT:
                oper = GT_GT;
                goto CMP_2_OPs_AND_BR;

            case CEE_BGT_UN_S:
            case CEE_BGT_UN:
                oper = GT_GT;
                goto CMP_2_OPs_AND_BR_UN;

            case CEE_BLE_S:
            case CEE_BLE:
                oper = GT_LE;
                goto CMP_2_OPs_AND_BR;

            case CEE_BLE_UN_S:
            case CEE_BLE_UN:
                oper = GT_LE;
                goto CMP_2_OPs_AND_BR_UN;

            case CEE_BLT_S:
            case CEE_BLT:
                oper = GT_LT;
                goto CMP_2_OPs_AND_BR;

            case CEE_BLT_UN_S:
            case CEE_BLT_UN:
                oper = GT_LT;
                goto CMP_2_OPs_AND_BR_UN;

            case CEE_BNE_UN_S:
            case CEE_BNE_UN:
                oper = GT_NE;
                goto CMP_2_OPs_AND_BR_UN;

            CMP_2_OPs_AND_BR_UN:
                uns       = true;
                unordered = true;
                goto CMP_2_OPs_AND_BR_ALL;
            CMP_2_OPs_AND_BR:
                uns       = false;
                unordered = false;
                goto CMP_2_OPs_AND_BR_ALL;
            CMP_2_OPs_AND_BR_ALL:

                if (tiVerificationNeeded)
                {
                    verVerifyCond(impStackTop(1).seTypeInfo, impStackTop().seTypeInfo, opcode);
                }

                /* Pull two values */
                op2 = impPopStack().val;
                op1 = impPopStack().val;

#ifdef TARGET_64BIT
                if ((op1->TypeGet() == TYP_I_IMPL) && (genActualType(op2->TypeGet()) == TYP_INT))
                {
                    op2 = gtNewCastNode(TYP_I_IMPL, op2, uns, uns ? TYP_U_IMPL : TYP_I_IMPL);
                }
                else if ((op2->TypeGet() == TYP_I_IMPL) && (genActualType(op1->TypeGet()) == TYP_INT))
                {
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, uns, uns ? TYP_U_IMPL : TYP_I_IMPL);
                }
#endif // TARGET_64BIT

                assertImp(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
                          (varTypeIsI(op1->TypeGet()) && varTypeIsI(op2->TypeGet())) ||
                          (varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType)));

                if (opts.OptimizationEnabled() && (block->bbJumpDest == block->bbNext))
                {
                    block->bbJumpKind = BBJ_NONE;

                    if (op1->gtFlags & GTF_GLOB_EFFECT)
                    {
                        impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG(
                                                       "Branch to next Optimization, op1 side effect"));
                        impAppendTree(gtUnusedValNode(op1), (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
                    }
                    if (op2->gtFlags & GTF_GLOB_EFFECT)
                    {
                        impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG(
                                                       "Branch to next Optimization, op2 side effect"));
                        impAppendTree(gtUnusedValNode(op2), (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
                    }

#ifdef DEBUG
                    if ((op1->gtFlags | op2->gtFlags) & GTF_GLOB_EFFECT)
                    {
                        impNoteLastILoffs();
                    }
#endif
                    break;
                }

                // We can generate an compare of different sized floating point op1 and op2
                // We insert a cast
                //
                if (varTypeIsFloating(op1->TypeGet()))
                {
                    if (op1->TypeGet() != op2->TypeGet())
                    {
                        assert(varTypeIsFloating(op2->TypeGet()));

                        // say op1=double, op2=float. To avoid loss of precision
                        // while comparing, op2 is converted to double and double
                        // comparison is done.
                        if (op1->TypeGet() == TYP_DOUBLE)
                        {
                            // We insert a cast of op2 to TYP_DOUBLE
                            op2 = gtNewCastNode(TYP_DOUBLE, op2, false, TYP_DOUBLE);
                        }
                        else if (op2->TypeGet() == TYP_DOUBLE)
                        {
                            // We insert a cast of op1 to TYP_DOUBLE
                            op1 = gtNewCastNode(TYP_DOUBLE, op1, false, TYP_DOUBLE);
                        }
                    }
                }

                /* Create and append the operator */

                op1 = gtNewOperNode(oper, TYP_INT, op1, op2);

                if (uns)
                {
                    op1->gtFlags |= GTF_UNSIGNED;
                }

                if (unordered)
                {
                    op1->gtFlags |= GTF_RELOP_NAN_UN;
                }

                goto COND_JUMP;

            case CEE_SWITCH:
                assert(!compIsForInlining());

                if (tiVerificationNeeded)
                {
                    Verify(impStackTop().seTypeInfo.IsType(TI_INT), "Bad switch val");
                }
                /* Pop the switch value off the stack */
                op1 = impPopStack().val;
                assertImp(genActualTypeIsIntOrI(op1->TypeGet()));

                /* We can create a switch node */

                op1 = gtNewOperNode(GT_SWITCH, TYP_VOID, op1);

                val = (int)getU4LittleEndian(codeAddr);
                codeAddr += 4 + val * 4; // skip over the switch-table

                goto SPILL_APPEND;

            /************************** Casting OPCODES ***************************/

            case CEE_CONV_OVF_I1:
                lclTyp = TYP_BYTE;
                goto CONV_OVF;
            case CEE_CONV_OVF_I2:
                lclTyp = TYP_SHORT;
                goto CONV_OVF;
            case CEE_CONV_OVF_I:
                lclTyp = TYP_I_IMPL;
                goto CONV_OVF;
            case CEE_CONV_OVF_I4:
                lclTyp = TYP_INT;
                goto CONV_OVF;
            case CEE_CONV_OVF_I8:
                lclTyp = TYP_LONG;
                goto CONV_OVF;

            case CEE_CONV_OVF_U1:
                lclTyp = TYP_UBYTE;
                goto CONV_OVF;
            case CEE_CONV_OVF_U2:
                lclTyp = TYP_USHORT;
                goto CONV_OVF;
            case CEE_CONV_OVF_U:
                lclTyp = TYP_U_IMPL;
                goto CONV_OVF;
            case CEE_CONV_OVF_U4:
                lclTyp = TYP_UINT;
                goto CONV_OVF;
            case CEE_CONV_OVF_U8:
                lclTyp = TYP_ULONG;
                goto CONV_OVF;

            case CEE_CONV_OVF_I1_UN:
                lclTyp = TYP_BYTE;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_I2_UN:
                lclTyp = TYP_SHORT;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_I_UN:
                lclTyp = TYP_I_IMPL;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_I4_UN:
                lclTyp = TYP_INT;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_I8_UN:
                lclTyp = TYP_LONG;
                goto CONV_OVF_UN;

            case CEE_CONV_OVF_U1_UN:
                lclTyp = TYP_UBYTE;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_U2_UN:
                lclTyp = TYP_USHORT;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_U_UN:
                lclTyp = TYP_U_IMPL;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_U4_UN:
                lclTyp = TYP_UINT;
                goto CONV_OVF_UN;
            case CEE_CONV_OVF_U8_UN:
                lclTyp = TYP_ULONG;
                goto CONV_OVF_UN;

            CONV_OVF_UN:
                uns = true;
                goto CONV_OVF_COMMON;
            CONV_OVF:
                uns = false;
                goto CONV_OVF_COMMON;

            CONV_OVF_COMMON:
                ovfl = true;
                goto _CONV;

            case CEE_CONV_I1:
                lclTyp = TYP_BYTE;
                goto CONV;
            case CEE_CONV_I2:
                lclTyp = TYP_SHORT;
                goto CONV;
            case CEE_CONV_I:
                lclTyp = TYP_I_IMPL;
                goto CONV;
            case CEE_CONV_I4:
                lclTyp = TYP_INT;
                goto CONV;
            case CEE_CONV_I8:
                lclTyp = TYP_LONG;
                goto CONV;

            case CEE_CONV_U1:
                lclTyp = TYP_UBYTE;
                goto CONV;
            case CEE_CONV_U2:
                lclTyp = TYP_USHORT;
                goto CONV;
#if (REGSIZE_BYTES == 8)
            case CEE_CONV_U:
                lclTyp = TYP_U_IMPL;
                goto CONV_UN;
#else
            case CEE_CONV_U:
                lclTyp = TYP_U_IMPL;
                goto CONV;
#endif
            case CEE_CONV_U4:
                lclTyp = TYP_UINT;
                goto CONV;
            case CEE_CONV_U8:
                lclTyp = TYP_ULONG;
                goto CONV_UN;

            case CEE_CONV_R4:
                lclTyp = TYP_FLOAT;
                goto CONV;
            case CEE_CONV_R8:
                lclTyp = TYP_DOUBLE;
                goto CONV;

            case CEE_CONV_R_UN:
                lclTyp = TYP_DOUBLE;
                goto CONV_UN;

            CONV_UN:
                uns  = true;
                ovfl = false;
                goto _CONV;

            CONV:
                uns  = false;
                ovfl = false;
                goto _CONV;

            _CONV:
                // just check that we have a number on the stack
                if (tiVerificationNeeded)
                {
                    const typeInfo& tiVal = impStackTop().seTypeInfo;
                    Verify(tiVal.IsNumberType(), "bad arg");

#ifdef TARGET_64BIT
                    bool isNative = false;

                    switch (opcode)
                    {
                        case CEE_CONV_OVF_I:
                        case CEE_CONV_OVF_I_UN:
                        case CEE_CONV_I:
                        case CEE_CONV_OVF_U:
                        case CEE_CONV_OVF_U_UN:
                        case CEE_CONV_U:
                            isNative = true;
                            break;
                        default:
                            // leave 'isNative' = false;
                            break;
                    }
                    if (isNative)
                    {
                        tiRetVal = typeInfo::nativeInt();
                    }
                    else
#endif // TARGET_64BIT
                    {
                        tiRetVal = typeInfo(lclTyp).NormaliseForStack();
                    }
                }

                // only converts from FLOAT or DOUBLE to an integer type
                // and converts from  ULONG (or LONG on ARM) to DOUBLE are morphed to calls

                if (varTypeIsFloating(lclTyp))
                {
                    callNode = varTypeIsLong(impStackTop().val) || uns // uint->dbl gets turned into uint->long->dbl
#ifdef TARGET_64BIT
                               // TODO-ARM64-Bug?: This was AMD64; I enabled it for ARM64 also. OK?
                               // TYP_BYREF could be used as TYP_I_IMPL which is long.
                               // TODO-CQ: remove this when we lower casts long/ulong --> float/double
                               // and generate SSE2 code instead of going through helper calls.
                               || (impStackTop().val->TypeGet() == TYP_BYREF)
#endif
                        ;
                }
                else
                {
                    callNode = varTypeIsFloating(impStackTop().val->TypeGet());
                }

                op1 = impPopStack().val;
                impBashVarAddrsToI(op1);

                // Casts from floating point types must not have GTF_UNSIGNED set.
                if (varTypeIsFloating(op1))
                {
                    uns = false;
                }

                // At this point uns, ovf, callNode are all set.

                if (varTypeIsSmall(lclTyp) && !ovfl && op1->gtType == TYP_INT && op1->gtOper == GT_AND)
                {
                    op2 = op1->AsOp()->gtOp2;

                    if (op2->gtOper == GT_CNS_INT)
                    {
                        ssize_t ival = op2->AsIntCon()->gtIconVal;
                        ssize_t mask, umask;

                        switch (lclTyp)
                        {
                            case TYP_BYTE:
                            case TYP_UBYTE:
                                mask  = 0x00FF;
                                umask = 0x007F;
                                break;
                            case TYP_USHORT:
                            case TYP_SHORT:
                                mask  = 0xFFFF;
                                umask = 0x7FFF;
                                break;

                            default:
                                assert(!"unexpected type");
                                return;
                        }

                        if (((ival & umask) == ival) || ((ival & mask) == ival && uns))
                        {
                            /* Toss the cast, it's a waste of time */

                            impPushOnStack(op1, tiRetVal);
                            break;
                        }
                        else if (ival == mask)
                        {
                            /* Toss the masking, it's a waste of time, since
                               we sign-extend from the small value anyways */

                            op1 = op1->AsOp()->gtOp1;
                        }
                    }
                }

                /*  The 'op2' sub-operand of a cast is the 'real' type number,
                    since the result of a cast to one of the 'small' integer
                    types is an integer.
                 */

                type = genActualType(lclTyp);

                // If this is a no-op cast, just use op1.
                if (!ovfl && (type == op1->TypeGet()) && (genTypeSize(type) == genTypeSize(lclTyp)))
                {
                    // Nothing needs to change
                }
                // Work is evidently required, add cast node
                else
                {
                    if (callNode)
                    {
                        op1 = gtNewCastNodeL(type, op1, uns, lclTyp);
                    }
                    else
                    {
                        op1 = gtNewCastNode(type, op1, uns, lclTyp);
                    }

                    if (ovfl)
                    {
                        op1->gtFlags |= (GTF_OVERFLOW | GTF_EXCEPT);
                    }

                    if (op1->gtGetOp1()->OperIsConst() && opts.OptimizationEnabled())
                    {
                        // Try and fold the introduced cast
                        op1 = gtFoldExprConst(op1);
                    }
                }

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_NEG:
                if (tiVerificationNeeded)
                {
                    tiRetVal = impStackTop().seTypeInfo;
                    Verify(tiRetVal.IsNumberType(), "Bad arg");
                }

                op1 = impPopStack().val;
                impBashVarAddrsToI(op1, nullptr);
                impPushOnStack(gtNewOperNode(GT_NEG, genActualType(op1->gtType), op1), tiRetVal);
                break;

            case CEE_POP:
            {
                /* Pull the top value from the stack */

                StackEntry se = impPopStack();
                clsHnd        = se.seTypeInfo.GetClassHandle();
                op1           = se.val;

                /* Get hold of the type of the value being duplicated */

                lclTyp = genActualType(op1->gtType);

                /* Does the value have any side effects? */

                if ((op1->gtFlags & GTF_SIDE_EFFECT) || opts.compDbgCode)
                {
                    // Since we are throwing away the value, just normalize
                    // it to its address.  This is more efficient.

                    if (varTypeIsStruct(op1))
                    {
                        JITDUMP("\n ... CEE_POP struct ...\n");
                        DISPTREE(op1);
#ifdef UNIX_AMD64_ABI
                        // Non-calls, such as obj or ret_expr, have to go through this.
                        // Calls with large struct return value have to go through this.
                        // Helper calls with small struct return value also have to go
                        // through this since they do not follow Unix calling convention.
                        if (op1->gtOper != GT_CALL ||
                            !IsMultiRegReturnedType(clsHnd, op1->AsCall()->GetUnmanagedCallConv()) ||
                            op1->AsCall()->gtCallType == CT_HELPER)
#endif // UNIX_AMD64_ABI
                        {
                            // If the value being produced comes from loading
                            // via an underlying address, just null check the address.
                            if (op1->OperIs(GT_FIELD, GT_IND, GT_OBJ))
                            {
                                gtChangeOperToNullCheck(op1, block);
                            }
                            else
                            {
                                op1 = impGetStructAddr(op1, clsHnd, (unsigned)CHECK_SPILL_ALL, false);
                            }

                            JITDUMP("\n ... optimized to ...\n");
                            DISPTREE(op1);
                        }
                    }

                    // If op1 is non-overflow cast, throw it away since it is useless.
                    // Another reason for throwing away the useless cast is in the context of
                    // implicit tail calls when the operand of pop is GT_CAST(GT_CALL(..)).
                    // The cast gets added as part of importing GT_CALL, which gets in the way
                    // of fgMorphCall() on the forms of tail call nodes that we assert.
                    if ((op1->gtOper == GT_CAST) && !op1->gtOverflow())
                    {
                        op1 = op1->AsOp()->gtOp1;
                    }

                    if (op1->gtOper != GT_CALL)
                    {
                        if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
                        {
                            op1 = gtUnusedValNode(op1);
                        }
                        else
                        {
                            // Can't bash to NOP here because op1 can be referenced from `currentBlock->bbEntryState`,
                            // if we ever need to reimport we need a valid LCL_VAR on it.
                            op1 = gtNewNothingNode();
                        }
                    }

                    /* Append the value to the tree list */
                    goto SPILL_APPEND;
                }

                /* No side effects - just throw the <BEEP> thing away */
            }
            break;

            case CEE_DUP:
            {
                if (tiVerificationNeeded)
                {
                    // Dup could start the beginning of delegate creation sequence, remember that
                    delegateCreateStart = codeAddr - 1;
                    impStackTop(0);
                }

                // If the expression to dup is simple, just clone it.
                // Otherwise spill it to a temp, and reload the temp
                // twice.
                StackEntry se   = impPopStack();
                GenTree*   tree = se.val;
                tiRetVal        = se.seTypeInfo;
                op1             = tree;

                if (!opts.compDbgCode && !op1->IsIntegralConst(0) && !op1->IsFPZero() && !op1->IsLocal())
                {
                    const unsigned tmpNum = lvaGrabTemp(true DEBUGARG("dup spill"));
                    impAssignTempGen(tmpNum, op1, tiRetVal.GetClassHandle(), (unsigned)CHECK_SPILL_ALL);
                    var_types type = genActualType(lvaTable[tmpNum].TypeGet());
                    op1            = gtNewLclvNode(tmpNum, type);

                    // Propagate type info to the temp from the stack and the original tree
                    if (type == TYP_REF)
                    {
                        assert(lvaTable[tmpNum].lvSingleDef == 0);
                        lvaTable[tmpNum].lvSingleDef = 1;
                        JITDUMP("Marked V%02u as a single def local\n", tmpNum);
                        lvaSetClass(tmpNum, tree, tiRetVal.GetClassHandle());
                    }
                }

                op1 = impCloneExpr(op1, &op2, tiRetVal.GetClassHandle(), (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("DUP instruction"));

                assert(!(op1->gtFlags & GTF_GLOB_EFFECT) && !(op2->gtFlags & GTF_GLOB_EFFECT));
                impPushOnStack(op1, tiRetVal);
                impPushOnStack(op2, tiRetVal);
            }
            break;

            case CEE_STIND_I1:
                lclTyp = TYP_BYTE;
                goto STIND;
            case CEE_STIND_I2:
                lclTyp = TYP_SHORT;
                goto STIND;
            case CEE_STIND_I4:
                lclTyp = TYP_INT;
                goto STIND;
            case CEE_STIND_I8:
                lclTyp = TYP_LONG;
                goto STIND;
            case CEE_STIND_I:
                lclTyp = TYP_I_IMPL;
                goto STIND;
            case CEE_STIND_REF:
                lclTyp = TYP_REF;
                goto STIND;
            case CEE_STIND_R4:
                lclTyp = TYP_FLOAT;
                goto STIND;
            case CEE_STIND_R8:
                lclTyp = TYP_DOUBLE;
                goto STIND;
            STIND:

                if (tiVerificationNeeded)
                {
                    typeInfo instrType(lclTyp);
#ifdef TARGET_64BIT
                    if (opcode == CEE_STIND_I)
                    {
                        instrType = typeInfo::nativeInt();
                    }
#endif // TARGET_64BIT
                    verVerifySTIND(impStackTop(1).seTypeInfo, impStackTop(0).seTypeInfo, instrType);
                }
                else
                {
                    compUnsafeCastUsed = true; // Have to go conservative
                }

            STIND_POST_VERIFY:

                op2 = impPopStack().val; // value to store
                op1 = impPopStack().val; // address to store to

                // you can indirect off of a TYP_I_IMPL (if we are in C) or a BYREF
                assertImp(genActualType(op1->gtType) == TYP_I_IMPL || op1->gtType == TYP_BYREF);

                impBashVarAddrsToI(op1, op2);

                op2 = impImplicitR4orR8Cast(op2, lclTyp);

#ifdef TARGET_64BIT
                // Automatic upcast for a GT_CNS_INT into TYP_I_IMPL
                if ((op2->OperGet() == GT_CNS_INT) && varTypeIsI(lclTyp) && !varTypeIsI(op2->gtType))
                {
                    op2->gtType = TYP_I_IMPL;
                }
                else
                {
                    // Allow a downcast of op2 from TYP_I_IMPL into a 32-bit Int for x86 JIT compatiblity
                    //
                    if (varTypeIsI(op2->gtType) && (genActualType(lclTyp) == TYP_INT))
                    {
                        assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                        op2 = gtNewCastNode(TYP_INT, op2, false, TYP_INT);
                    }
                    // Allow an upcast of op2 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                    //
                    if (varTypeIsI(lclTyp) && (genActualType(op2->gtType) == TYP_INT))
                    {
                        assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                        op2 = gtNewCastNode(TYP_I_IMPL, op2, false, TYP_I_IMPL);
                    }
                }
#endif // TARGET_64BIT

                if (opcode == CEE_STIND_REF)
                {
                    // STIND_REF can be used to store TYP_INT, TYP_I_IMPL, TYP_REF, or TYP_BYREF
                    assertImp(varTypeIsIntOrI(op2->gtType) || varTypeIsGC(op2->gtType));
                    lclTyp = genActualType(op2->TypeGet());
                }

// Check target type.
#ifdef DEBUG
                if (op2->gtType == TYP_BYREF || lclTyp == TYP_BYREF)
                {
                    if (op2->gtType == TYP_BYREF)
                    {
                        assertImp(lclTyp == TYP_BYREF || lclTyp == TYP_I_IMPL);
                    }
                    else if (lclTyp == TYP_BYREF)
                    {
                        assertImp(op2->gtType == TYP_BYREF || varTypeIsIntOrI(op2->gtType));
                    }
                }
                else
                {
                    assertImp(genActualType(op2->gtType) == genActualType(lclTyp) ||
                              ((lclTyp == TYP_I_IMPL) && (genActualType(op2->gtType) == TYP_INT)) ||
                              (varTypeIsFloating(op2->gtType) && varTypeIsFloating(lclTyp)));
                }
#endif

                op1 = gtNewOperNode(GT_IND, lclTyp, op1);

                // stind could point anywhere, example a boxed class static int
                op1->gtFlags |= GTF_IND_TGTANYWHERE;

                if (prefixFlags & PREFIX_VOLATILE)
                {
                    assert(op1->OperGet() == GT_IND);
                    op1->gtFlags |= GTF_DONT_CSE;      // Can't CSE a volatile
                    op1->gtFlags |= GTF_ORDER_SIDEEFF; // Prevent this from being reordered
                    op1->gtFlags |= GTF_IND_VOLATILE;
                }

                if ((prefixFlags & PREFIX_UNALIGNED) && !varTypeIsByte(lclTyp))
                {
                    assert(op1->OperGet() == GT_IND);
                    op1->gtFlags |= GTF_IND_UNALIGNED;
                }

                op1 = gtNewAssignNode(op1, op2);
                op1->gtFlags |= GTF_EXCEPT | GTF_GLOB_REF;

                // Spill side-effects AND global-data-accesses
                if (verCurrentState.esStackDepth > 0)
                {
                    impSpillSideEffects(true, (unsigned)CHECK_SPILL_ALL DEBUGARG("spill side effects before STIND"));
                }

                goto APPEND;

            case CEE_LDIND_I1:
                lclTyp = TYP_BYTE;
                goto LDIND;
            case CEE_LDIND_I2:
                lclTyp = TYP_SHORT;
                goto LDIND;
            case CEE_LDIND_U4:
            case CEE_LDIND_I4:
                lclTyp = TYP_INT;
                goto LDIND;
            case CEE_LDIND_I8:
                lclTyp = TYP_LONG;
                goto LDIND;
            case CEE_LDIND_REF:
                lclTyp = TYP_REF;
                goto LDIND;
            case CEE_LDIND_I:
                lclTyp = TYP_I_IMPL;
                goto LDIND;
            case CEE_LDIND_R4:
                lclTyp = TYP_FLOAT;
                goto LDIND;
            case CEE_LDIND_R8:
                lclTyp = TYP_DOUBLE;
                goto LDIND;
            case CEE_LDIND_U1:
                lclTyp = TYP_UBYTE;
                goto LDIND;
            case CEE_LDIND_U2:
                lclTyp = TYP_USHORT;
                goto LDIND;
            LDIND:

                if (tiVerificationNeeded)
                {
                    typeInfo lclTiType(lclTyp);
#ifdef TARGET_64BIT
                    if (opcode == CEE_LDIND_I)
                    {
                        lclTiType = typeInfo::nativeInt();
                    }
#endif // TARGET_64BIT
                    tiRetVal = verVerifyLDIND(impStackTop().seTypeInfo, lclTiType);
                    tiRetVal.NormaliseForStack();
                }
                else
                {
                    compUnsafeCastUsed = true; // Have to go conservative
                }

            LDIND_POST_VERIFY:

                op1 = impPopStack().val; // address to load from
                impBashVarAddrsToI(op1);

#ifdef TARGET_64BIT
                // Allow an upcast of op1 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                //
                if (genActualType(op1->gtType) == TYP_INT)
                {
                    assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, false, TYP_I_IMPL);
                }
#endif

                assertImp(genActualType(op1->gtType) == TYP_I_IMPL || op1->gtType == TYP_BYREF);

                op1 = gtNewOperNode(GT_IND, lclTyp, op1);

                // ldind could point anywhere, example a boxed class static int
                op1->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF | GTF_IND_TGTANYWHERE);

                if (prefixFlags & PREFIX_VOLATILE)
                {
                    assert(op1->OperGet() == GT_IND);
                    op1->gtFlags |= GTF_DONT_CSE;      // Can't CSE a volatile
                    op1->gtFlags |= GTF_ORDER_SIDEEFF; // Prevent this from being reordered
                    op1->gtFlags |= GTF_IND_VOLATILE;
                }

                if ((prefixFlags & PREFIX_UNALIGNED) && !varTypeIsByte(lclTyp))
                {
                    assert(op1->OperGet() == GT_IND);
                    op1->gtFlags |= GTF_IND_UNALIGNED;
                }

                impPushOnStack(op1, tiRetVal);

                break;

            case CEE_UNALIGNED:

                assert(sz == 1);
                val = getU1LittleEndian(codeAddr);
                ++codeAddr;
                JITDUMP(" %u", val);
                if ((val != 1) && (val != 2) && (val != 4))
                {
                    BADCODE("Alignment unaligned. must be 1, 2, or 4");
                }

                Verify(!(prefixFlags & PREFIX_UNALIGNED), "Multiple unaligned. prefixes");
                prefixFlags |= PREFIX_UNALIGNED;

                impValidateMemoryAccessOpcode(codeAddr, codeEndp, false);

            PREFIX:
                opcode     = (OPCODE)getU1LittleEndian(codeAddr);
                opcodeOffs = (IL_OFFSET)(codeAddr - info.compCode);
                codeAddr += sizeof(__int8);
                goto DECODE_OPCODE;

            case CEE_VOLATILE:

                Verify(!(prefixFlags & PREFIX_VOLATILE), "Multiple volatile. prefixes");
                prefixFlags |= PREFIX_VOLATILE;

                impValidateMemoryAccessOpcode(codeAddr, codeEndp, true);

                assert(sz == 0);
                goto PREFIX;

            case CEE_LDFTN:
            {
                // Need to do a lookup here so that we perform an access check
                // and do a NOWAY if protections are violated
                _impResolveToken(CORINFO_TOKENKIND_Method);

                JITDUMP(" %08X", resolvedToken.token);

                eeGetCallInfo(&resolvedToken, (prefixFlags & PREFIX_CONSTRAINED) ? &constrainedResolvedToken : nullptr,
                              addVerifyFlag(combine(CORINFO_CALLINFO_SECURITYCHECKS, CORINFO_CALLINFO_LDFTN)),
                              &callInfo);

                // This check really only applies to intrinsic Array.Address methods
                if (callInfo.sig.callConv & CORINFO_CALLCONV_PARAMTYPE)
                {
                    NO_WAY("Currently do not support LDFTN of Parameterized functions");
                }

                // Do this before DO_LDFTN since CEE_LDVIRTFN does it on its own.
                impHandleAccessAllowed(callInfo.accessAllowed, &callInfo.callsiteCalloutHelper);

                if (tiVerificationNeeded)
                {
                    // LDFTN could start the beginning of delegate creation sequence, remember that
                    delegateCreateStart = codeAddr - 2;

                    // check any constraints on the callee's class and type parameters
                    VerifyOrReturn(info.compCompHnd->satisfiesClassConstraints(resolvedToken.hClass),
                                   "method has unsatisfied class constraints");
                    VerifyOrReturn(info.compCompHnd->satisfiesMethodConstraints(resolvedToken.hClass,
                                                                                resolvedToken.hMethod),
                                   "method has unsatisfied method constraints");

                    mflags = callInfo.verMethodFlags;
                    Verify(!(mflags & CORINFO_FLG_CONSTRUCTOR), "LDFTN on a constructor");
                }

            DO_LDFTN:
                op1 = impMethodPointer(&resolvedToken, &callInfo);

                if (compDonotInline())
                {
                    return;
                }

                // Call info may have more precise information about the function than
                // the resolved token.
                CORINFO_RESOLVED_TOKEN* heapToken = impAllocateToken(resolvedToken);
                assert(callInfo.hMethod != nullptr);
                heapToken->hMethod = callInfo.hMethod;
                impPushOnStack(op1, typeInfo(heapToken));

                break;
            }

            case CEE_LDVIRTFTN:
            {
                /* Get the method token */

                _impResolveToken(CORINFO_TOKENKIND_Method);

                JITDUMP(" %08X", resolvedToken.token);

                eeGetCallInfo(&resolvedToken, nullptr /* constraint typeRef */,
                              addVerifyFlag(combine(combine(CORINFO_CALLINFO_SECURITYCHECKS, CORINFO_CALLINFO_LDFTN),
                                                    CORINFO_CALLINFO_CALLVIRT)),
                              &callInfo);

                // This check really only applies to intrinsic Array.Address methods
                if (callInfo.sig.callConv & CORINFO_CALLCONV_PARAMTYPE)
                {
                    NO_WAY("Currently do not support LDFTN of Parameterized functions");
                }

                mflags = callInfo.methodFlags;

                impHandleAccessAllowed(callInfo.accessAllowed, &callInfo.callsiteCalloutHelper);

                if (compIsForInlining())
                {
                    if (mflags & (CORINFO_FLG_FINAL | CORINFO_FLG_STATIC) || !(mflags & CORINFO_FLG_VIRTUAL))
                    {
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_LDVIRTFN_ON_NON_VIRTUAL);
                        return;
                    }
                }

                CORINFO_SIG_INFO& ftnSig = callInfo.sig;

                if (tiVerificationNeeded)
                {

                    Verify(ftnSig.hasThis(), "ldvirtftn on a static method");
                    Verify(!(mflags & CORINFO_FLG_CONSTRUCTOR), "LDVIRTFTN on a constructor");

                    // JIT32 verifier rejects verifiable ldvirtftn pattern
                    typeInfo declType =
                        verMakeTypeInfo(resolvedToken.hClass, true); // Change TI_STRUCT to TI_REF when necessary

                    typeInfo arg = impStackTop().seTypeInfo;
                    Verify((arg.IsType(TI_REF) || arg.IsType(TI_NULL)) && tiCompatibleWith(arg, declType, true),
                           "bad ldvirtftn");

                    CORINFO_CLASS_HANDLE instanceClassHnd = info.compClassHnd;
                    if (!(arg.IsType(TI_NULL) || (mflags & CORINFO_FLG_STATIC)))
                    {
                        instanceClassHnd = arg.GetClassHandleForObjRef();
                    }

                    // check any constraints on the method's class and type parameters
                    VerifyOrReturn(info.compCompHnd->satisfiesClassConstraints(resolvedToken.hClass),
                                   "method has unsatisfied class constraints");
                    VerifyOrReturn(info.compCompHnd->satisfiesMethodConstraints(resolvedToken.hClass,
                                                                                resolvedToken.hMethod),
                                   "method has unsatisfied method constraints");

                    if (mflags & CORINFO_FLG_PROTECTED)
                    {
                        Verify(info.compCompHnd->canAccessFamily(info.compMethodHnd, instanceClassHnd),
                               "Accessing protected method through wrong type.");
                    }
                }

                /* Get the object-ref */
                op1 = impPopStack().val;
                assertImp(op1->gtType == TYP_REF);

                if (opts.IsReadyToRun())
                {
                    if (callInfo.kind != CORINFO_VIRTUALCALL_LDVIRTFTN)
                    {
                        if (op1->gtFlags & GTF_SIDE_EFFECT)
                        {
                            op1 = gtUnusedValNode(op1);
                            impAppendTree(op1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
                        }
                        goto DO_LDFTN;
                    }
                }
                else if (mflags & (CORINFO_FLG_FINAL | CORINFO_FLG_STATIC) || !(mflags & CORINFO_FLG_VIRTUAL))
                {
                    if (op1->gtFlags & GTF_SIDE_EFFECT)
                    {
                        op1 = gtUnusedValNode(op1);
                        impAppendTree(op1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
                    }
                    goto DO_LDFTN;
                }

                GenTree* fptr = impImportLdvirtftn(op1, &resolvedToken, &callInfo);
                if (compDonotInline())
                {
                    return;
                }

                CORINFO_RESOLVED_TOKEN* heapToken = impAllocateToken(resolvedToken);

                assert(heapToken->tokenType == CORINFO_TOKENKIND_Method);
                assert(callInfo.hMethod != nullptr);

                heapToken->tokenType = CORINFO_TOKENKIND_Ldvirtftn;
                heapToken->hMethod   = callInfo.hMethod;
                impPushOnStack(fptr, typeInfo(heapToken));

                break;
            }

            case CEE_CONSTRAINED:

                assertImp(sz == sizeof(unsigned));
                impResolveToken(codeAddr, &constrainedResolvedToken, CORINFO_TOKENKIND_Constrained);
                codeAddr += sizeof(unsigned); // prefix instructions must increment codeAddr manually
                JITDUMP(" (%08X) ", constrainedResolvedToken.token);

                Verify(!(prefixFlags & PREFIX_CONSTRAINED), "Multiple constrained. prefixes");
                prefixFlags |= PREFIX_CONSTRAINED;

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);
                    if (actualOpcode != CEE_CALLVIRT && actualOpcode != CEE_CALL && actualOpcode != CEE_LDFTN)
                    {
                        BADCODE("constrained. has to be followed by callvirt, call or ldftn");
                    }
                }

                goto PREFIX;

            case CEE_READONLY:
                JITDUMP(" readonly.");

                Verify(!(prefixFlags & PREFIX_READONLY), "Multiple readonly. prefixes");
                prefixFlags |= PREFIX_READONLY;

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);
                    if (actualOpcode != CEE_LDELEMA && !impOpcodeIsCallOpcode(actualOpcode))
                    {
                        BADCODE("readonly. has to be followed by ldelema or call");
                    }
                }

                assert(sz == 0);
                goto PREFIX;

            case CEE_TAILCALL:
                JITDUMP(" tail.");

                Verify(!(prefixFlags & PREFIX_TAILCALL_EXPLICIT), "Multiple tailcall. prefixes");
                prefixFlags |= PREFIX_TAILCALL_EXPLICIT;

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);
                    if (!impOpcodeIsCallOpcode(actualOpcode))
                    {
                        BADCODE("tailcall. has to be followed by call, callvirt or calli");
                    }
                }
                assert(sz == 0);
                goto PREFIX;

            case CEE_NEWOBJ:

                /* Since we will implicitly insert newObjThisPtr at the start of the
                   argument list, spill any GTF_ORDER_SIDEEFF */
                impSpillSpecialSideEff();

                /* NEWOBJ does not respond to TAIL */
                prefixFlags &= ~PREFIX_TAILCALL_EXPLICIT;

                /* NEWOBJ does not respond to CONSTRAINED */
                prefixFlags &= ~PREFIX_CONSTRAINED;

                _impResolveToken(CORINFO_TOKENKIND_NewObj);

                eeGetCallInfo(&resolvedToken, nullptr /* constraint typeRef*/,
                              addVerifyFlag(combine(CORINFO_CALLINFO_SECURITYCHECKS, CORINFO_CALLINFO_ALLOWINSTPARAM)),
                              &callInfo);

                if (compIsForInlining())
                {
                    if (impInlineInfo->inlineCandidateInfo->dwRestrictions & INLINE_RESPECT_BOUNDARY)
                    {
                        // Check to see if this call violates the boundary.
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_CROSS_BOUNDARY_SECURITY);
                        return;
                    }
                }

                mflags = callInfo.methodFlags;

                if ((mflags & (CORINFO_FLG_STATIC | CORINFO_FLG_ABSTRACT)) != 0)
                {
                    BADCODE("newobj on static or abstract method");
                }

                // Insert the security callout before any actual code is generated
                impHandleAccessAllowed(callInfo.accessAllowed, &callInfo.callsiteCalloutHelper);

                // There are three different cases for new
                // Object size is variable (depends on arguments)
                //      1) Object is an array (arrays treated specially by the EE)
                //      2) Object is some other variable sized object (e.g. String)
                //      3) Class Size can be determined beforehand (normal case)
                // In the first case, we need to call a NEWOBJ helper (multinewarray)
                // in the second case we call the constructor with a '0' this pointer
                // In the third case we alloc the memory, then call the constuctor

                clsFlags = callInfo.classFlags;
                if (clsFlags & CORINFO_FLG_ARRAY)
                {
                    if (tiVerificationNeeded)
                    {
                        CORINFO_CLASS_HANDLE elemTypeHnd;
                        INDEBUG(CorInfoType corType =)
                        info.compCompHnd->getChildType(resolvedToken.hClass, &elemTypeHnd);
                        assert(!(elemTypeHnd == nullptr && corType == CORINFO_TYPE_VALUECLASS));
                        Verify(elemTypeHnd == nullptr ||
                                   !(info.compCompHnd->getClassAttribs(elemTypeHnd) & CORINFO_FLG_CONTAINS_STACK_PTR),
                               "newarr of byref-like objects");
                        verVerifyCall(opcode, &resolvedToken, nullptr, ((prefixFlags & PREFIX_TAILCALL_EXPLICIT) != 0),
                                      ((prefixFlags & PREFIX_READONLY) != 0), delegateCreateStart, codeAddr - 1,
                                      &callInfo DEBUGARG(info.compFullName));
                    }
                    // Arrays need to call the NEWOBJ helper.
                    assertImp(clsFlags & CORINFO_FLG_VAROBJSIZE);

                    impImportNewObjArray(&resolvedToken, &callInfo);
                    if (compDonotInline())
                    {
                        return;
                    }

                    callTyp = TYP_REF;
                    break;
                }
                // At present this can only be String
                else if (clsFlags & CORINFO_FLG_VAROBJSIZE)
                {
                    if (IsTargetAbi(CORINFO_CORERT_ABI))
                    {
                        // The dummy argument does not exist in CoreRT
                        newObjThisPtr = nullptr;
                    }
                    else
                    {
                        // This is the case for variable-sized objects that are not
                        // arrays.  In this case, call the constructor with a null 'this'
                        // pointer
                        newObjThisPtr = gtNewIconNode(0, TYP_REF);
                    }

                    /* Remember that this basic block contains 'new' of an object */
                    block->bbFlags |= BBF_HAS_NEWOBJ;
                    optMethodFlags |= OMF_HAS_NEWOBJ;
                }
                else
                {
                    // This is the normal case where the size of the object is
                    // fixed.  Allocate the memory and call the constructor.

                    // Note: We cannot add a peep to avoid use of temp here
                    // becase we don't have enough interference info to detect when
                    // sources and destination interfere, example: s = new S(ref);

                    // TODO: We find the correct place to introduce a general
                    // reverse copy prop for struct return values from newobj or
                    // any function returning structs.

                    /* get a temporary for the new object */
                    lclNum = lvaGrabTemp(true DEBUGARG("NewObj constructor temp"));
                    if (compDonotInline())
                    {
                        // Fail fast if lvaGrabTemp fails with CALLSITE_TOO_MANY_LOCALS.
                        assert(compInlineResult->GetObservation() == InlineObservation::CALLSITE_TOO_MANY_LOCALS);
                        return;
                    }

                    // In the value class case we only need clsHnd for size calcs.
                    //
                    // The lookup of the code pointer will be handled by CALL in this case
                    if (clsFlags & CORINFO_FLG_VALUECLASS)
                    {
                        if (compIsForInlining())
                        {
                            // If value class has GC fields, inform the inliner. It may choose to
                            // bail out on the inline.
                            DWORD typeFlags = info.compCompHnd->getClassAttribs(resolvedToken.hClass);
                            if ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) != 0)
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_HAS_GC_STRUCT);
                                if (compInlineResult->IsFailure())
                                {
                                    return;
                                }

                                // Do further notification in the case where the call site is rare;
                                // some policies do not track the relative hotness of call sites for
                                // "always" inline cases.
                                if (impInlineInfo->iciBlock->isRunRarely())
                                {
                                    compInlineResult->Note(InlineObservation::CALLSITE_RARE_GC_STRUCT);
                                    if (compInlineResult->IsFailure())
                                    {
                                        return;
                                    }
                                }
                            }
                        }

                        CorInfoType jitTyp = info.compCompHnd->asCorInfoType(resolvedToken.hClass);

                        if (impIsPrimitive(jitTyp))
                        {
                            lvaTable[lclNum].lvType = JITtype2varType(jitTyp);
                        }
                        else
                        {
                            // The local variable itself is the allocated space.
                            // Here we need unsafe value cls check, since the address of struct is taken for further use
                            // and potentially exploitable.
                            lvaSetStruct(lclNum, resolvedToken.hClass, true /* unsafe value cls check */);
                        }

                        bool bbInALoop  = impBlockIsInALoop(block);
                        bool bbIsReturn = (block->bbJumpKind == BBJ_RETURN) &&
                                          (!compIsForInlining() || (impInlineInfo->iciBlock->bbJumpKind == BBJ_RETURN));
                        LclVarDsc* const lclDsc = lvaGetDesc(lclNum);
                        if (fgVarNeedsExplicitZeroInit(lclNum, bbInALoop, bbIsReturn))
                        {
                            // Append a tree to zero-out the temp
                            newObjThisPtr = gtNewLclvNode(lclNum, lclDsc->TypeGet());

                            newObjThisPtr = gtNewBlkOpNode(newObjThisPtr,    // Dest
                                                           gtNewIconNode(0), // Value
                                                           false,            // isVolatile
                                                           false);           // not copyBlock
                            impAppendTree(newObjThisPtr, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
                        }
                        else
                        {
                            JITDUMP("\nSuppressing zero-init for V%02u -- expect to zero in prolog\n", lclNum);
                            lclDsc->lvSuppressedZeroInit = 1;
                            compSuppressedZeroInit       = true;
                        }

                        // Obtain the address of the temp
                        newObjThisPtr =
                            gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(lclNum, lvaTable[lclNum].TypeGet()));
                    }
                    else
                    {
                        const bool useParent = true;
                        op1                  = gtNewAllocObjNode(&resolvedToken, useParent);
                        if (op1 == nullptr)
                        {
                            return;
                        }

                        // Remember that this basic block contains 'new' of an object
                        block->bbFlags |= BBF_HAS_NEWOBJ;
                        optMethodFlags |= OMF_HAS_NEWOBJ;

                        // Append the assignment to the temp/local. Dont need to spill
                        // at all as we are just calling an EE-Jit helper which can only
                        // cause an (async) OutOfMemoryException.

                        // We assign the newly allocated object (by a GT_ALLOCOBJ node)
                        // to a temp. Note that the pattern "temp = allocObj" is required
                        // by ObjectAllocator phase to be able to determine GT_ALLOCOBJ nodes
                        // without exhaustive walk over all expressions.

                        impAssignTempGen(lclNum, op1, (unsigned)CHECK_SPILL_NONE);

                        assert(lvaTable[lclNum].lvSingleDef == 0);
                        lvaTable[lclNum].lvSingleDef = 1;
                        JITDUMP("Marked V%02u as a single def local\n", lclNum);
                        lvaSetClass(lclNum, resolvedToken.hClass, true /* is Exact */);

                        newObjThisPtr = gtNewLclvNode(lclNum, TYP_REF);
                    }
                }
                goto CALL;

            case CEE_CALLI:

                /* CALLI does not respond to CONSTRAINED */
                prefixFlags &= ~PREFIX_CONSTRAINED;

                if (compIsForInlining())
                {
                    // CALLI doesn't have a method handle, so assume the worst.
                    if (impInlineInfo->inlineCandidateInfo->dwRestrictions & INLINE_RESPECT_BOUNDARY)
                    {
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_CROSS_BOUNDARY_CALLI);
                        return;
                    }
                }

                FALLTHROUGH;

            case CEE_CALLVIRT:
            case CEE_CALL:

                // We can't call getCallInfo on the token from a CALLI, but we need it in
                // many other places.  We unfortunately embed that knowledge here.
                if (opcode != CEE_CALLI)
                {
                    _impResolveToken(CORINFO_TOKENKIND_Method);

                    eeGetCallInfo(&resolvedToken,
                                  (prefixFlags & PREFIX_CONSTRAINED) ? &constrainedResolvedToken : nullptr,
                                  // this is how impImportCall invokes getCallInfo
                                  addVerifyFlag(
                                      combine(combine(CORINFO_CALLINFO_ALLOWINSTPARAM, CORINFO_CALLINFO_SECURITYCHECKS),
                                              (opcode == CEE_CALLVIRT) ? CORINFO_CALLINFO_CALLVIRT
                                                                       : CORINFO_CALLINFO_NONE)),
                                  &callInfo);
                }
                else
                {
                    // Suppress uninitialized use warning.
                    memset(&resolvedToken, 0, sizeof(resolvedToken));
                    memset(&callInfo, 0, sizeof(callInfo));

                    resolvedToken.token        = getU4LittleEndian(codeAddr);
                    resolvedToken.tokenContext = impTokenLookupContextHandle;
                    resolvedToken.tokenScope   = info.compScopeHnd;
                }

            CALL: // memberRef should be set.
                // newObjThisPtr should be set for CEE_NEWOBJ

                JITDUMP(" %08X", resolvedToken.token);
                constraintCall = (prefixFlags & PREFIX_CONSTRAINED) != 0;

                bool newBBcreatedForTailcallStress;
                bool passedStressModeValidation;

                newBBcreatedForTailcallStress = false;
                passedStressModeValidation    = true;

                if (compIsForInlining())
                {
                    if (compDonotInline())
                    {
                        return;
                    }
                    // We rule out inlinees with explicit tail calls in fgMakeBasicBlocks.
                    assert((prefixFlags & PREFIX_TAILCALL_EXPLICIT) == 0);
                }
                else
                {
                    if (compTailCallStress())
                    {
                        // Have we created a new BB after the "call" instruction in fgMakeBasicBlocks()?
                        // Tail call stress only recognizes call+ret patterns and forces them to be
                        // explicit tail prefixed calls.  Also fgMakeBasicBlocks() under tail call stress
                        // doesn't import 'ret' opcode following the call into the basic block containing
                        // the call instead imports it to a new basic block.  Note that fgMakeBasicBlocks()
                        // is already checking that there is an opcode following call and hence it is
                        // safe here to read next opcode without bounds check.
                        newBBcreatedForTailcallStress =
                            impOpcodeIsCallOpcode(opcode) && // Current opcode is a CALL, (not a CEE_NEWOBJ). So, don't
                                                             // make it jump to RET.
                            (OPCODE)getU1LittleEndian(codeAddr + sz) == CEE_RET; // Next opcode is a CEE_RET

                        bool hasTailPrefix = (prefixFlags & PREFIX_TAILCALL_EXPLICIT);
                        if (newBBcreatedForTailcallStress && !hasTailPrefix)
                        {
                            // Do a more detailed evaluation of legality
                            const bool returnFalseIfInvalid = true;
                            const bool passedConstraintCheck =
                                verCheckTailCallConstraint(opcode, &resolvedToken,
                                                           constraintCall ? &constrainedResolvedToken : nullptr,
                                                           returnFalseIfInvalid);

                            if (passedConstraintCheck)
                            {
                                // Now check with the runtime
                                CORINFO_METHOD_HANDLE declaredCalleeHnd = callInfo.hMethod;
                                bool                  isVirtual         = (callInfo.kind == CORINFO_VIRTUALCALL_STUB) ||
                                                 (callInfo.kind == CORINFO_VIRTUALCALL_VTABLE);
                                CORINFO_METHOD_HANDLE exactCalleeHnd = isVirtual ? nullptr : declaredCalleeHnd;
                                if (info.compCompHnd->canTailCall(info.compMethodHnd, declaredCalleeHnd, exactCalleeHnd,
                                                                  hasTailPrefix)) // Is it legal to do tailcall?
                                {
                                    // Stress the tailcall.
                                    JITDUMP(" (Tailcall stress: prefixFlags |= PREFIX_TAILCALL_EXPLICIT)");
                                    prefixFlags |= PREFIX_TAILCALL_EXPLICIT;
                                    prefixFlags |= PREFIX_TAILCALL_STRESS;
                                }
                                else
                                {
                                    // Runtime disallows this tail call
                                    JITDUMP(" (Tailcall stress: runtime preventing tailcall)");
                                    passedStressModeValidation = false;
                                }
                            }
                            else
                            {
                                // Constraints disallow this tail call
                                JITDUMP(" (Tailcall stress: constraint check failed)");
                                passedStressModeValidation = false;
                            }
                        }
                    }
                }

                // This is split up to avoid goto flow warnings.
                bool isRecursive;
                isRecursive = !compIsForInlining() && (callInfo.hMethod == info.compMethodHnd);

                // If we've already disqualified this call as a tail call under tail call stress,
                // don't consider it for implicit tail calling either.
                //
                // When not running under tail call stress, we may mark this call as an implicit
                // tail call candidate. We'll do an "equivalent" validation during impImportCall.
                //
                // Note that when running under tail call stress, a call marked as explicit
                // tail prefixed will not be considered for implicit tail calling.
                if (passedStressModeValidation &&
                    impIsImplicitTailCallCandidate(opcode, codeAddr + sz, codeEndp, prefixFlags, isRecursive))
                {
                    if (compIsForInlining())
                    {
#if FEATURE_TAILCALL_OPT_SHARED_RETURN
                        // Are we inlining at an implicit tail call site? If so the we can flag
                        // implicit tail call sites in the inline body. These call sites
                        // often end up in non BBJ_RETURN blocks, so only flag them when
                        // we're able to handle shared returns.
                        if (impInlineInfo->iciCall->IsImplicitTailCall())
                        {
                            JITDUMP("\n (Inline Implicit Tail call: prefixFlags |= PREFIX_TAILCALL_IMPLICIT)");
                            prefixFlags |= PREFIX_TAILCALL_IMPLICIT;
                        }
#endif // FEATURE_TAILCALL_OPT_SHARED_RETURN
                    }
                    else
                    {
                        JITDUMP("\n (Implicit Tail call: prefixFlags |= PREFIX_TAILCALL_IMPLICIT)");
                        prefixFlags |= PREFIX_TAILCALL_IMPLICIT;
                    }
                }

                // Treat this call as tail call for verification only if "tail" prefixed (i.e. explicit tail call).
                explicitTailCall = (prefixFlags & PREFIX_TAILCALL_EXPLICIT) != 0;
                readonlyCall     = (prefixFlags & PREFIX_READONLY) != 0;

                if (opcode != CEE_CALLI && opcode != CEE_NEWOBJ)
                {
                    // All calls and delegates need a security callout.
                    // For delegates, this is the call to the delegate constructor, not the access check on the
                    // LD(virt)FTN.
                    impHandleAccessAllowed(callInfo.accessAllowed, &callInfo.callsiteCalloutHelper);
                }

                if (tiVerificationNeeded)
                {
                    verVerifyCall(opcode, &resolvedToken, constraintCall ? &constrainedResolvedToken : nullptr,
                                  explicitTailCall, readonlyCall, delegateCreateStart, codeAddr - 1,
                                  &callInfo DEBUGARG(info.compFullName));
                }

                callTyp = impImportCall(opcode, &resolvedToken, constraintCall ? &constrainedResolvedToken : nullptr,
                                        newObjThisPtr, prefixFlags, &callInfo, opcodeOffs);
                if (compDonotInline())
                {
                    // We do not check fails after lvaGrabTemp. It is covered with CoreCLR_13272 issue.
                    assert((callTyp == TYP_UNDEF) ||
                           (compInlineResult->GetObservation() == InlineObservation::CALLSITE_TOO_MANY_LOCALS));
                    return;
                }

                if (explicitTailCall || newBBcreatedForTailcallStress) // If newBBcreatedForTailcallStress is true, we
                                                                       // have created a new BB after the "call"
                // instruction in fgMakeBasicBlocks(). So we need to jump to RET regardless.
                {
                    assert(!compIsForInlining());
                    goto RET;
                }

                break;

            case CEE_LDFLD:
            case CEE_LDSFLD:
            case CEE_LDFLDA:
            case CEE_LDSFLDA:
            {

                bool isLoadAddress = (opcode == CEE_LDFLDA || opcode == CEE_LDSFLDA);
                bool isLoadStatic  = (opcode == CEE_LDSFLD || opcode == CEE_LDSFLDA);

                /* Get the CP_Fieldref index */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Field);

                JITDUMP(" %08X", resolvedToken.token);

                int aflags = isLoadAddress ? CORINFO_ACCESS_ADDRESS : CORINFO_ACCESS_GET;

                GenTree*             obj     = nullptr;
                typeInfo*            tiObj   = nullptr;
                CORINFO_CLASS_HANDLE objType = nullptr; // used for fields

                if (opcode == CEE_LDFLD || opcode == CEE_LDFLDA)
                {
                    tiObj         = &impStackTop().seTypeInfo;
                    StackEntry se = impPopStack();
                    objType       = se.seTypeInfo.GetClassHandle();
                    obj           = se.val;

                    if (impIsThis(obj))
                    {
                        aflags |= CORINFO_ACCESS_THIS;
                    }
                }

                eeGetFieldInfo(&resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo);

                // Figure out the type of the member.  We always call canAccessField, so you always need this
                // handle
                CorInfoType ciType = fieldInfo.fieldType;
                clsHnd             = fieldInfo.structType;

                lclTyp = JITtype2varType(ciType);

                if (compIsForInlining())
                {
                    switch (fieldInfo.fieldAccessor)
                    {
                        case CORINFO_FIELD_INSTANCE_HELPER:
                        case CORINFO_FIELD_INSTANCE_ADDR_HELPER:
                        case CORINFO_FIELD_STATIC_ADDR_HELPER:
                        case CORINFO_FIELD_STATIC_TLS:

                            compInlineResult->NoteFatal(InlineObservation::CALLEE_LDFLD_NEEDS_HELPER);
                            return;

                        case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
                        case CORINFO_FIELD_STATIC_READYTORUN_HELPER:
                            /* We may be able to inline the field accessors in specific instantiations of generic
                             * methods */
                            compInlineResult->NoteFatal(InlineObservation::CALLSITE_LDFLD_NEEDS_HELPER);
                            return;

                        default:
                            break;
                    }

                    if (!isLoadAddress && (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC) && lclTyp == TYP_STRUCT &&
                        clsHnd)
                    {
                        if ((info.compCompHnd->getTypeForPrimitiveValueClass(clsHnd) == CORINFO_TYPE_UNDEF) &&
                            !(info.compFlags & CORINFO_FLG_FORCEINLINE))
                        {
                            // Loading a static valuetype field usually will cause a JitHelper to be called
                            // for the static base. This will bloat the code.
                            compInlineResult->Note(InlineObservation::CALLEE_LDFLD_STATIC_VALUECLASS);

                            if (compInlineResult->IsFailure())
                            {
                                return;
                            }
                        }
                    }
                }

                tiRetVal = verMakeTypeInfo(ciType, clsHnd);
                if (isLoadAddress)
                {
                    tiRetVal.MakeByRef();
                }
                else
                {
                    tiRetVal.NormaliseForStack();
                }

                // Perform this check always to ensure that we get field access exceptions even with
                // SkipVerification.
                impHandleAccessAllowed(fieldInfo.accessAllowed, &fieldInfo.accessCalloutHelper);

                if (tiVerificationNeeded)
                {
                    // You can also pass the unboxed struct to  LDFLD
                    bool bAllowPlainValueTypeAsThis = false;
                    if (opcode == CEE_LDFLD && impIsValueType(tiObj))
                    {
                        bAllowPlainValueTypeAsThis = true;
                    }

                    verVerifyField(&resolvedToken, fieldInfo, tiObj, isLoadAddress, bAllowPlainValueTypeAsThis);

                    // If we're doing this on a heap object or from a 'safe' byref
                    // then the result is a safe byref too
                    if (isLoadAddress) // load address
                    {
                        if (fieldInfo.fieldFlags &
                            CORINFO_FLG_FIELD_STATIC) // statics marked as safe will have permanent home
                        {
                            if (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_SAFESTATIC_BYREF_RETURN)
                            {
                                tiRetVal.SetIsPermanentHomeByRef();
                            }
                        }
                        else if (tiObj->IsObjRef() || tiObj->IsPermanentHomeByRef())
                        {
                            // ldflda of byref is safe if done on a gc object or on  a
                            // safe byref
                            tiRetVal.SetIsPermanentHomeByRef();
                        }
                    }
                }
                else
                {
                    // tiVerificationNeeded is false.
                    // Raise InvalidProgramException if static load accesses non-static field
                    if (isLoadStatic && ((fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC) == 0))
                    {
                        BADCODE("static access on an instance field");
                    }
                }

                // We are using ldfld/a on a static field. We allow it, but need to get side-effect from obj.
                if ((fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC) && obj != nullptr)
                {
                    if (obj->gtFlags & GTF_SIDE_EFFECT)
                    {
                        obj = gtUnusedValNode(obj);
                        impAppendTree(obj, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
                    }
                    obj = nullptr;
                }

                /* Preserve 'small' int types */
                if (!varTypeIsSmall(lclTyp))
                {
                    lclTyp = genActualType(lclTyp);
                }

                bool usesHelper = false;

                switch (fieldInfo.fieldAccessor)
                {
                    case CORINFO_FIELD_INSTANCE:
#ifdef FEATURE_READYTORUN_COMPILER
                    case CORINFO_FIELD_INSTANCE_WITH_BASE:
#endif
                    {
                        obj = impCheckForNullPointer(obj);

                        // If the object is a struct, what we really want is
                        // for the field to operate on the address of the struct.
                        if (!varTypeGCtype(obj->TypeGet()) && impIsValueType(tiObj))
                        {
                            assert(opcode == CEE_LDFLD && objType != nullptr);

                            obj = impGetStructAddr(obj, objType, (unsigned)CHECK_SPILL_ALL, true);
                        }

                        /* Create the data member node */
                        op1 = gtNewFieldRef(lclTyp, resolvedToken.hField, obj, fieldInfo.offset);

#ifdef FEATURE_READYTORUN_COMPILER
                        if (fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE_WITH_BASE)
                        {
                            op1->AsField()->gtFieldLookup = fieldInfo.fieldLookup;
                        }
#endif

                        op1->gtFlags |= (obj->gtFlags & GTF_GLOB_EFFECT);

                        if (fgAddrCouldBeNull(obj))
                        {
                            op1->gtFlags |= GTF_EXCEPT;
                        }

                        // If gtFldObj is a BYREF then our target is a value class and
                        // it could point anywhere, example a boxed class static int
                        if (obj->gtType == TYP_BYREF)
                        {
                            op1->gtFlags |= GTF_IND_TGTANYWHERE;
                        }

                        DWORD typeFlags = info.compCompHnd->getClassAttribs(resolvedToken.hClass);
                        if (StructHasOverlappingFields(typeFlags))
                        {
                            op1->AsField()->gtFldMayOverlap = true;
                        }

                        // wrap it in a address of operator if necessary
                        if (isLoadAddress)
                        {
                            op1 = gtNewOperNode(GT_ADDR,
                                                (var_types)(varTypeIsGC(obj->TypeGet()) ? TYP_BYREF : TYP_I_IMPL), op1);
                        }
                        else
                        {
                            if (compIsForInlining() &&
                                impInlineIsGuaranteedThisDerefBeforeAnySideEffects(nullptr, nullptr, obj,
                                                                                   impInlineInfo->inlArgInfo))
                            {
                                impInlineInfo->thisDereferencedFirst = true;
                            }
                        }
                    }
                    break;

                    case CORINFO_FIELD_STATIC_TLS:
#ifdef TARGET_X86
                        // Legacy TLS access is implemented as intrinsic on x86 only

                        /* Create the data member node */
                        op1 = gtNewFieldRef(lclTyp, resolvedToken.hField, NULL, fieldInfo.offset);
                        op1->gtFlags |= GTF_IND_TLS_REF; // fgMorphField will handle the transformation

                        if (isLoadAddress)
                        {
                            op1 = gtNewOperNode(GT_ADDR, (var_types)TYP_I_IMPL, op1);
                        }
                        break;
#else
                        fieldInfo.fieldAccessor = CORINFO_FIELD_STATIC_ADDR_HELPER;

                        FALLTHROUGH;
#endif

                    case CORINFO_FIELD_STATIC_ADDR_HELPER:
                    case CORINFO_FIELD_INSTANCE_HELPER:
                    case CORINFO_FIELD_INSTANCE_ADDR_HELPER:
                        op1 = gtNewRefCOMfield(obj, &resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo, lclTyp,
                                               clsHnd, nullptr);
                        usesHelper = true;
                        break;

                    case CORINFO_FIELD_STATIC_ADDRESS:
                        // Replace static read-only fields with constant if possible
                        if ((aflags & CORINFO_ACCESS_GET) && (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_FINAL) &&
                            !(fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP) &&
                            (varTypeIsIntegral(lclTyp) || varTypeIsFloating(lclTyp)))
                        {
                            CorInfoInitClassResult initClassResult =
                                info.compCompHnd->initClass(resolvedToken.hField, info.compMethodHnd,
                                                            impTokenLookupContextHandle);

                            if (initClassResult & CORINFO_INITCLASS_INITIALIZED)
                            {
                                void** pFldAddr = nullptr;
                                void*  fldAddr =
                                    info.compCompHnd->getFieldAddress(resolvedToken.hField, (void**)&pFldAddr);

                                // We should always be able to access this static's address directly
                                //
                                assert(pFldAddr == nullptr);

                                op1 = impImportStaticReadOnlyField(fldAddr, lclTyp);

                                // Widen small types since we're propagating the value
                                // instead of producing an indir.
                                //
                                op1->gtType = genActualType(lclTyp);

                                goto FIELD_DONE;
                            }
                        }

                        FALLTHROUGH;

                    case CORINFO_FIELD_STATIC_RVA_ADDRESS:
                    case CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER:
                    case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
                    case CORINFO_FIELD_STATIC_READYTORUN_HELPER:
                        op1 = impImportStaticFieldAccess(&resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo,
                                                         lclTyp);
                        break;

                    case CORINFO_FIELD_INTRINSIC_ZERO:
                    {
                        assert(aflags & CORINFO_ACCESS_GET);
                        // Widen to stack type
                        lclTyp = genActualType(lclTyp);
                        op1    = gtNewIconNode(0, lclTyp);
                        goto FIELD_DONE;
                    }
                    break;

                    case CORINFO_FIELD_INTRINSIC_EMPTY_STRING:
                    {
                        assert(aflags & CORINFO_ACCESS_GET);

                        LPVOID         pValue;
                        InfoAccessType iat = info.compCompHnd->emptyStringLiteral(&pValue);
                        op1                = gtNewStringLiteralNode(iat, pValue);
                        goto FIELD_DONE;
                    }
                    break;

                    case CORINFO_FIELD_INTRINSIC_ISLITTLEENDIAN:
                    {
                        assert(aflags & CORINFO_ACCESS_GET);
                        // Widen to stack type
                        lclTyp = genActualType(lclTyp);
#if BIGENDIAN
                        op1 = gtNewIconNode(0, lclTyp);
#else
                        op1                     = gtNewIconNode(1, lclTyp);
#endif
                        goto FIELD_DONE;
                    }
                    break;

                    default:
                        assert(!"Unexpected fieldAccessor");
                }

                if (!isLoadAddress)
                {

                    if (prefixFlags & PREFIX_VOLATILE)
                    {
                        op1->gtFlags |= GTF_DONT_CSE;      // Can't CSE a volatile
                        op1->gtFlags |= GTF_ORDER_SIDEEFF; // Prevent this from being reordered

                        if (!usesHelper)
                        {
                            assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND) ||
                                   (op1->OperGet() == GT_OBJ));
                            op1->gtFlags |= GTF_IND_VOLATILE;
                        }
                    }

                    if ((prefixFlags & PREFIX_UNALIGNED) && !varTypeIsByte(lclTyp))
                    {
                        if (!usesHelper)
                        {
                            assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND) ||
                                   (op1->OperGet() == GT_OBJ));
                            op1->gtFlags |= GTF_IND_UNALIGNED;
                        }
                    }
                }

                /* Check if the class needs explicit initialization */

                if (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    GenTree* helperNode = impInitClass(&resolvedToken);
                    if (compDonotInline())
                    {
                        return;
                    }
                    if (helperNode != nullptr)
                    {
                        op1 = gtNewOperNode(GT_COMMA, op1->TypeGet(), helperNode, op1);
                    }
                }

            FIELD_DONE:
                impPushOnStack(op1, tiRetVal);
            }
            break;

            case CEE_STFLD:
            case CEE_STSFLD:
            {

                bool isStoreStatic = (opcode == CEE_STSFLD);

                CORINFO_CLASS_HANDLE fieldClsHnd; // class of the field (if it's a ref type)

                /* Get the CP_Fieldref index */

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Field);

                JITDUMP(" %08X", resolvedToken.token);

                int       aflags = CORINFO_ACCESS_SET;
                GenTree*  obj    = nullptr;
                typeInfo* tiObj  = nullptr;
                typeInfo  tiVal;

                /* Pull the value from the stack */
                StackEntry se = impPopStack();
                op2           = se.val;
                tiVal         = se.seTypeInfo;
                clsHnd        = tiVal.GetClassHandle();

                if (opcode == CEE_STFLD)
                {
                    tiObj = &impStackTop().seTypeInfo;
                    obj   = impPopStack().val;

                    if (impIsThis(obj))
                    {
                        aflags |= CORINFO_ACCESS_THIS;
                    }
                }

                eeGetFieldInfo(&resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo);

                // Figure out the type of the member.  We always call canAccessField, so you always need this
                // handle
                CorInfoType ciType = fieldInfo.fieldType;
                fieldClsHnd        = fieldInfo.structType;

                lclTyp = JITtype2varType(ciType);

                if (compIsForInlining())
                {
                    /* Is this a 'special' (COM) field? or a TLS ref static field?, field stored int GC heap? or
                     * per-inst static? */

                    switch (fieldInfo.fieldAccessor)
                    {
                        case CORINFO_FIELD_INSTANCE_HELPER:
                        case CORINFO_FIELD_INSTANCE_ADDR_HELPER:
                        case CORINFO_FIELD_STATIC_ADDR_HELPER:
                        case CORINFO_FIELD_STATIC_TLS:

                            compInlineResult->NoteFatal(InlineObservation::CALLEE_STFLD_NEEDS_HELPER);
                            return;

                        case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
                        case CORINFO_FIELD_STATIC_READYTORUN_HELPER:
                            /* We may be able to inline the field accessors in specific instantiations of generic
                             * methods */
                            compInlineResult->NoteFatal(InlineObservation::CALLSITE_STFLD_NEEDS_HELPER);
                            return;

                        default:
                            break;
                    }
                }

                impHandleAccessAllowed(fieldInfo.accessAllowed, &fieldInfo.accessCalloutHelper);

                if (tiVerificationNeeded)
                {
                    verVerifyField(&resolvedToken, fieldInfo, tiObj, true);
                    typeInfo fieldType = verMakeTypeInfo(ciType, fieldClsHnd);
                    Verify(tiCompatibleWith(tiVal, fieldType.NormaliseForStack(), true), "type mismatch");
                }
                else
                {
                    // tiVerificationNeed is false.
                    // Raise InvalidProgramException if static store accesses non-static field
                    if (isStoreStatic && ((fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC) == 0))
                    {
                        BADCODE("static access on an instance field");
                    }
                }

                // We are using stfld on a static field.
                // We allow it, but need to eval any side-effects for obj
                if ((fieldInfo.fieldFlags & CORINFO_FLG_FIELD_STATIC) && obj != nullptr)
                {
                    if (obj->gtFlags & GTF_SIDE_EFFECT)
                    {
                        obj = gtUnusedValNode(obj);
                        impAppendTree(obj, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
                    }
                    obj = nullptr;
                }

                /* Preserve 'small' int types */
                if (!varTypeIsSmall(lclTyp))
                {
                    lclTyp = genActualType(lclTyp);
                }

                switch (fieldInfo.fieldAccessor)
                {
                    case CORINFO_FIELD_INSTANCE:
#ifdef FEATURE_READYTORUN_COMPILER
                    case CORINFO_FIELD_INSTANCE_WITH_BASE:
#endif
                    {
                        obj = impCheckForNullPointer(obj);

                        /* Create the data member node */
                        op1             = gtNewFieldRef(lclTyp, resolvedToken.hField, obj, fieldInfo.offset);
                        DWORD typeFlags = info.compCompHnd->getClassAttribs(resolvedToken.hClass);
                        if (StructHasOverlappingFields(typeFlags))
                        {
                            op1->AsField()->gtFldMayOverlap = true;
                        }

#ifdef FEATURE_READYTORUN_COMPILER
                        if (fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE_WITH_BASE)
                        {
                            op1->AsField()->gtFieldLookup = fieldInfo.fieldLookup;
                        }
#endif

                        op1->gtFlags |= (obj->gtFlags & GTF_GLOB_EFFECT);

                        if (fgAddrCouldBeNull(obj))
                        {
                            op1->gtFlags |= GTF_EXCEPT;
                        }

                        // If gtFldObj is a BYREF then our target is a value class and
                        // it could point anywhere, example a boxed class static int
                        if (obj->gtType == TYP_BYREF)
                        {
                            op1->gtFlags |= GTF_IND_TGTANYWHERE;
                        }

                        if (compIsForInlining() &&
                            impInlineIsGuaranteedThisDerefBeforeAnySideEffects(op2, nullptr, obj,
                                                                               impInlineInfo->inlArgInfo))
                        {
                            impInlineInfo->thisDereferencedFirst = true;
                        }
                    }
                    break;

                    case CORINFO_FIELD_STATIC_TLS:
#ifdef TARGET_X86
                        // Legacy TLS access is implemented as intrinsic on x86 only

                        /* Create the data member node */
                        op1 = gtNewFieldRef(lclTyp, resolvedToken.hField, NULL, fieldInfo.offset);
                        op1->gtFlags |= GTF_IND_TLS_REF; // fgMorphField will handle the transformation

                        break;
#else
                        fieldInfo.fieldAccessor = CORINFO_FIELD_STATIC_ADDR_HELPER;

                        FALLTHROUGH;
#endif

                    case CORINFO_FIELD_STATIC_ADDR_HELPER:
                    case CORINFO_FIELD_INSTANCE_HELPER:
                    case CORINFO_FIELD_INSTANCE_ADDR_HELPER:
                        op1 = gtNewRefCOMfield(obj, &resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo, lclTyp,
                                               clsHnd, op2);
                        goto SPILL_APPEND;

                    case CORINFO_FIELD_STATIC_ADDRESS:
                    case CORINFO_FIELD_STATIC_RVA_ADDRESS:
                    case CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER:
                    case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
                    case CORINFO_FIELD_STATIC_READYTORUN_HELPER:
                        op1 = impImportStaticFieldAccess(&resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo,
                                                         lclTyp);
                        break;

                    default:
                        assert(!"Unexpected fieldAccessor");
                }

                // Create the member assignment, unless we have a TYP_STRUCT.
                bool deferStructAssign = (lclTyp == TYP_STRUCT);

                if (!deferStructAssign)
                {
                    if (prefixFlags & PREFIX_VOLATILE)
                    {
                        assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND));
                        op1->gtFlags |= GTF_DONT_CSE;      // Can't CSE a volatile
                        op1->gtFlags |= GTF_ORDER_SIDEEFF; // Prevent this from being reordered
                        op1->gtFlags |= GTF_IND_VOLATILE;
                    }
                    if ((prefixFlags & PREFIX_UNALIGNED) && !varTypeIsByte(lclTyp))
                    {
                        assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND));
                        op1->gtFlags |= GTF_IND_UNALIGNED;
                    }

                    /* V4.0 allows assignment of i4 constant values to i8 type vars when IL verifier is bypassed (full
                       trust apps). The reason this works is that JIT stores an i4 constant in Gentree union during
                       importation and reads from the union as if it were a long during code generation. Though this
                       can potentially read garbage, one can get lucky to have this working correctly.

                       This code pattern is generated by Dev10 MC++ compiler while storing to fields when compiled with
                       /O2 switch (default when compiling retail configs in Dev10) and a customer app has taken a
                       dependency on it. To be backward compatible, we will explicitly add an upward cast here so that
                       it works correctly always.

                       Note that this is limited to x86 alone as there is no back compat to be addressed for Arm JIT
                       for V4.0.
                    */
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef TARGET_64BIT
                    // In UWP6.0 and beyond (post-.NET Core 2.0), we decided to let this cast from int to long be
                    // generated for ARM as well as x86, so the following IR will be accepted:
                    // STMTx (IL 0x... ???)
                    //   *  ASG long
                    //   +--*  CLS_VAR   long
                    //   \--*  CNS_INT   int    2

                    if ((op1->TypeGet() != op2->TypeGet()) && op2->OperIsConst() && varTypeIsIntOrI(op2->TypeGet()) &&
                        varTypeIsLong(op1->TypeGet()))
                    {
                        op2 = gtNewCastNode(op1->TypeGet(), op2, false, op1->TypeGet());
                    }
#endif

#ifdef TARGET_64BIT
                    // Automatic upcast for a GT_CNS_INT into TYP_I_IMPL
                    if ((op2->OperGet() == GT_CNS_INT) && varTypeIsI(lclTyp) && !varTypeIsI(op2->gtType))
                    {
                        op2->gtType = TYP_I_IMPL;
                    }
                    else
                    {
                        // Allow a downcast of op2 from TYP_I_IMPL into a 32-bit Int for x86 JIT compatiblity
                        //
                        if (varTypeIsI(op2->gtType) && (genActualType(lclTyp) == TYP_INT))
                        {
                            op2 = gtNewCastNode(TYP_INT, op2, false, TYP_INT);
                        }
                        // Allow an upcast of op2 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                        //
                        if (varTypeIsI(lclTyp) && (genActualType(op2->gtType) == TYP_INT))
                        {
                            op2 = gtNewCastNode(TYP_I_IMPL, op2, false, TYP_I_IMPL);
                        }
                    }
#endif

                    // We can generate an assignment to a TYP_FLOAT from a TYP_DOUBLE
                    // We insert a cast to the dest 'op1' type
                    //
                    if ((op1->TypeGet() != op2->TypeGet()) && varTypeIsFloating(op1->gtType) &&
                        varTypeIsFloating(op2->gtType))
                    {
                        op2 = gtNewCastNode(op1->TypeGet(), op2, false, op1->TypeGet());
                    }

                    op1 = gtNewAssignNode(op1, op2);

                    /* Mark the expression as containing an assignment */

                    op1->gtFlags |= GTF_ASG;
                }

                /* Check if the class needs explicit initialization */

                if (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    GenTree* helperNode = impInitClass(&resolvedToken);
                    if (compDonotInline())
                    {
                        return;
                    }
                    if (helperNode != nullptr)
                    {
                        op1 = gtNewOperNode(GT_COMMA, op1->TypeGet(), helperNode, op1);
                    }
                }

                /* stfld can interfere with value classes (consider the sequence
                   ldloc, ldloca, ..., stfld, stloc).  We will be conservative and
                   spill all value class references from the stack. */

                if (obj && ((obj->gtType == TYP_BYREF) || (obj->gtType == TYP_I_IMPL)))
                {
                    assert(tiObj);

                    // If we can resolve the field to be within some local,
                    // then just spill that local.
                    //
                    GenTreeLclVarCommon* const lcl = obj->IsLocalAddrExpr();

                    if (lcl != nullptr)
                    {
                        impSpillLclRefs(lcl->GetLclNum());
                    }
                    else if (impIsValueType(tiObj))
                    {
                        impSpillEvalStack();
                    }
                    else
                    {
                        impSpillValueClasses();
                    }
                }

                /* Spill any refs to the same member from the stack */

                impSpillLclRefs((ssize_t)resolvedToken.hField);

                /* stsfld also interferes with indirect accesses (for aliased
                   statics) and calls. But don't need to spill other statics
                   as we have explicitly spilled this particular static field. */

                impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG("spill side effects before STFLD"));

                if (deferStructAssign)
                {
                    op1 = impAssignStruct(op1, op2, clsHnd, (unsigned)CHECK_SPILL_ALL);
                }
            }
                goto APPEND;

            case CEE_NEWARR:
            {

                /* Get the class type index operand */

                _impResolveToken(CORINFO_TOKENKIND_Newarr);

                JITDUMP(" %08X", resolvedToken.token);

                if (!opts.IsReadyToRun())
                {
                    // Need to restore array classes before creating array objects on the heap
                    op1 = impTokenToHandle(&resolvedToken, nullptr, true /*mustRestoreHandle*/);
                    if (op1 == nullptr)
                    { // compDonotInline()
                        return;
                    }
                }

                if (tiVerificationNeeded)
                {
                    // As per ECMA 'numElems' specified can be either int32 or native int.
                    Verify(impStackTop().seTypeInfo.IsIntOrNativeIntType(), "bad bound");

                    CORINFO_CLASS_HANDLE elemTypeHnd;
                    info.compCompHnd->getChildType(resolvedToken.hClass, &elemTypeHnd);
                    Verify(elemTypeHnd == nullptr ||
                               !(info.compCompHnd->getClassAttribs(elemTypeHnd) & CORINFO_FLG_CONTAINS_STACK_PTR),
                           "array of byref-like type");
                }

                tiRetVal = verMakeTypeInfo(resolvedToken.hClass);

                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                /* Form the arglist: array class handle, size */
                op2 = impPopStack().val;
                assertImp(genActualTypeIsIntOrI(op2->gtType));

#ifdef TARGET_64BIT
                // The array helper takes a native int for array length.
                // So if we have an int, explicitly extend it to be a native int.
                if (genActualType(op2->TypeGet()) != TYP_I_IMPL)
                {
                    if (op2->IsIntegralConst())
                    {
                        op2->gtType = TYP_I_IMPL;
                    }
                    else
                    {
                        bool isUnsigned = false;
                        op2             = gtNewCastNode(TYP_I_IMPL, op2, isUnsigned, TYP_I_IMPL);
                    }
                }
#endif // TARGET_64BIT

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    op1 = impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_NEWARR_1, TYP_REF,
                                                    gtNewCallArgs(op2));
                    usingReadyToRunHelper = (op1 != nullptr);

                    if (!usingReadyToRunHelper)
                    {
                        // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                        // and the newarr call with a single call to a dynamic R2R cell that will:
                        //      1) Load the context
                        //      2) Perform the generic dictionary lookup and caching, and generate the appropriate stub
                        //      3) Allocate the new array
                        // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                        // Need to restore array classes before creating array objects on the heap
                        op1 = impTokenToHandle(&resolvedToken, nullptr, true /*mustRestoreHandle*/);
                        if (op1 == nullptr)
                        { // compDonotInline()
                            return;
                        }
                    }
                }

                if (!usingReadyToRunHelper)
#endif
                {
                    GenTreeCall::Use* args = gtNewCallArgs(op1, op2);

                    /* Create a call to 'new' */

                    // Note that this only works for shared generic code because the same helper is used for all
                    // reference array types
                    op1 = gtNewHelperCallNode(info.compCompHnd->getNewArrHelper(resolvedToken.hClass), TYP_REF, args);
                }

                op1->AsCall()->compileTimeHelperArgumentHandle = (CORINFO_GENERIC_HANDLE)resolvedToken.hClass;

                /* Remember that this basic block contains 'new' of an sd array */

                block->bbFlags |= BBF_HAS_NEWARRAY;
                optMethodFlags |= OMF_HAS_NEWARRAY;

                /* Push the result of the call on the stack */

                impPushOnStack(op1, tiRetVal);

                callTyp = TYP_REF;
            }
            break;

            case CEE_LOCALLOC:
                if (tiVerificationNeeded)
                {
                    Verify(false, "bad opcode");
                }

                // We don't allow locallocs inside handlers
                if (block->hasHndIndex())
                {
                    BADCODE("Localloc can't be inside handler");
                }

                // Get the size to allocate

                op2 = impPopStack().val;
                assertImp(genActualTypeIsIntOrI(op2->gtType));

                if (verCurrentState.esStackDepth != 0)
                {
                    BADCODE("Localloc can only be used when the stack is empty");
                }

                // If the localloc is not in a loop and its size is a small constant,
                // create a new local var of TYP_BLK and return its address.
                {
                    bool convertedToLocal = false;

                    // Need to aggressively fold here, as even fixed-size locallocs
                    // will have casts in the way.
                    op2 = gtFoldExpr(op2);

                    if (op2->IsIntegralConst())
                    {
                        const ssize_t allocSize = op2->AsIntCon()->IconValue();

                        bool bbInALoop = impBlockIsInALoop(block);

                        if (allocSize == 0)
                        {
                            // Result is nullptr
                            JITDUMP("Converting stackalloc of 0 bytes to push null unmanaged pointer\n");
                            op1              = gtNewIconNode(0, TYP_I_IMPL);
                            convertedToLocal = true;
                        }
                        else if ((allocSize > 0) && !bbInALoop)
                        {
                            // Get the size threshold for local conversion
                            ssize_t maxSize = DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE;

#ifdef DEBUG
                            // Optionally allow this to be modified
                            maxSize = JitConfig.JitStackAllocToLocalSize();
#endif // DEBUG

                            if (allocSize <= maxSize)
                            {
                                const unsigned stackallocAsLocal = lvaGrabTemp(false DEBUGARG("stackallocLocal"));
                                JITDUMP("Converting stackalloc of %zd bytes to new local V%02u\n", allocSize,
                                        stackallocAsLocal);
                                lvaTable[stackallocAsLocal].lvType           = TYP_BLK;
                                lvaTable[stackallocAsLocal].lvExactSize      = (unsigned)allocSize;
                                lvaTable[stackallocAsLocal].lvIsUnsafeBuffer = true;
                                op1              = gtNewLclvNode(stackallocAsLocal, TYP_BLK);
                                op1              = gtNewOperNode(GT_ADDR, TYP_I_IMPL, op1);
                                convertedToLocal = true;

                                if (!this->opts.compDbgEnC)
                                {
                                    // Ensure we have stack security for this method.
                                    // Reorder layout since the converted localloc is treated as an unsafe buffer.
                                    setNeedsGSSecurityCookie();
                                    compGSReorderStackLayout = true;
                                }
                            }
                        }
                    }

                    if (!convertedToLocal)
                    {
                        // Bail out if inlining and the localloc was not converted.
                        //
                        // Note we might consider allowing the inline, if the call
                        // site is not in a loop.
                        if (compIsForInlining())
                        {
                            InlineObservation obs = op2->IsIntegralConst()
                                                        ? InlineObservation::CALLEE_LOCALLOC_TOO_LARGE
                                                        : InlineObservation::CALLSITE_LOCALLOC_SIZE_UNKNOWN;
                            compInlineResult->NoteFatal(obs);
                            return;
                        }

                        op1 = gtNewOperNode(GT_LCLHEAP, TYP_I_IMPL, op2);
                        // May throw a stack overflow exception. Obviously, we don't want locallocs to be CSE'd.
                        op1->gtFlags |= (GTF_EXCEPT | GTF_DONT_CSE);

                        // Ensure we have stack security for this method.
                        setNeedsGSSecurityCookie();

                        /* The FP register may not be back to the original value at the end
                           of the method, even if the frame size is 0, as localloc may
                           have modified it. So we will HAVE to reset it */
                        compLocallocUsed = true;
                    }
                    else
                    {
                        compLocallocOptimized = true;
                    }
                }

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_ISINST:
            {
                /* Get the type token */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Casting);

                JITDUMP(" %08X", resolvedToken.token);

                if (!opts.IsReadyToRun())
                {
                    op2 = impTokenToHandle(&resolvedToken, nullptr, false);
                    if (op2 == nullptr)
                    { // compDonotInline()
                        return;
                    }
                }

                if (tiVerificationNeeded)
                {
                    Verify(impStackTop().seTypeInfo.IsObjRef(), "obj reference needed");
                    // Even if this is a value class, we know it is boxed.
                    tiRetVal = typeInfo(TI_REF, resolvedToken.hClass);
                }
                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                op1 = impPopStack().val;

                GenTree* optTree = impOptimizeCastClassOrIsInst(op1, &resolvedToken, false);

                if (optTree != nullptr)
                {
                    impPushOnStack(optTree, tiRetVal);
                }
                else
                {

#ifdef FEATURE_READYTORUN_COMPILER
                    if (opts.IsReadyToRun())
                    {
                        GenTreeCall* opLookup =
                            impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_ISINSTANCEOF, TYP_REF,
                                                      gtNewCallArgs(op1));
                        usingReadyToRunHelper = (opLookup != nullptr);
                        op1                   = (usingReadyToRunHelper ? opLookup : op1);

                        if (!usingReadyToRunHelper)
                        {
                            // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                            // and the isinstanceof_any call with a single call to a dynamic R2R cell that will:
                            //      1) Load the context
                            //      2) Perform the generic dictionary lookup and caching, and generate the appropriate
                            //      stub
                            //      3) Perform the 'is instance' check on the input object
                            // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                            op2 = impTokenToHandle(&resolvedToken, nullptr, false);
                            if (op2 == nullptr)
                            { // compDonotInline()
                                return;
                            }
                        }
                    }

                    if (!usingReadyToRunHelper)
#endif
                    {
                        op1 = impCastClassOrIsInstToTree(op1, op2, &resolvedToken, false);
                    }
                    if (compDonotInline())
                    {
                        return;
                    }

                    impPushOnStack(op1, tiRetVal);
                }
                break;
            }

            case CEE_REFANYVAL:

                // get the class handle and make a ICON node out of it

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                op2 = impTokenToHandle(&resolvedToken);
                if (op2 == nullptr)
                { // compDonotInline()
                    return;
                }

                if (tiVerificationNeeded)
                {
                    Verify(typeInfo::AreEquivalent(impStackTop().seTypeInfo, verMakeTypeInfo(impGetRefAnyClass())),
                           "need refany");
                    tiRetVal = verMakeTypeInfo(resolvedToken.hClass).MakeByRef();
                }

                op1 = impPopStack().val;
                // make certain it is normalized;
                op1 = impNormStructVal(op1, impGetRefAnyClass(), (unsigned)CHECK_SPILL_ALL);

                // Call helper GETREFANY(classHandle, op1);
                op1 = gtNewHelperCallNode(CORINFO_HELP_GETREFANY, TYP_BYREF, gtNewCallArgs(op2, op1));

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_REFANYTYPE:

                if (tiVerificationNeeded)
                {
                    Verify(typeInfo::AreEquivalent(impStackTop().seTypeInfo, verMakeTypeInfo(impGetRefAnyClass())),
                           "need refany");
                }

                op1 = impPopStack().val;

                // make certain it is normalized;
                op1 = impNormStructVal(op1, impGetRefAnyClass(), (unsigned)CHECK_SPILL_ALL);

                if (op1->gtOper == GT_OBJ)
                {
                    // Get the address of the refany
                    op1 = op1->AsOp()->gtOp1;

                    // Fetch the type from the correct slot
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        gtNewIconNode(OFFSETOF__CORINFO_TypedReference__type, TYP_I_IMPL));
                    op1 = gtNewOperNode(GT_IND, TYP_BYREF, op1);
                }
                else
                {
                    assertImp(op1->gtOper == GT_MKREFANY);

                    // The pointer may have side-effects
                    if (op1->AsOp()->gtOp1->gtFlags & GTF_SIDE_EFFECT)
                    {
                        impAppendTree(op1->AsOp()->gtOp1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
#ifdef DEBUG
                        impNoteLastILoffs();
#endif
                    }

                    // We already have the class handle
                    op1 = op1->AsOp()->gtOp2;
                }

                // convert native TypeHandle to RuntimeTypeHandle
                {
                    GenTreeCall::Use* helperArgs = gtNewCallArgs(op1);

                    op1 = gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL, TYP_STRUCT,
                                              helperArgs);

                    CORINFO_CLASS_HANDLE classHandle = impGetTypeHandleClass();

                    // The handle struct is returned in register
                    op1->AsCall()->gtReturnType = GetRuntimeHandleUnderlyingType();
                    op1->AsCall()->gtRetClsHnd  = classHandle;
#if FEATURE_MULTIREG_RET
                    op1->AsCall()->InitializeStructReturnType(this, classHandle, op1->AsCall()->GetUnmanagedCallConv());
#endif

                    tiRetVal = typeInfo(TI_STRUCT, classHandle);
                }

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_LDTOKEN:
            {
                /* Get the Class index */
                assertImp(sz == sizeof(unsigned));
                lastLoadToken = codeAddr;
                _impResolveToken(CORINFO_TOKENKIND_Ldtoken);

                tokenType = info.compCompHnd->getTokenTypeAsHandle(&resolvedToken);

                op1 = impTokenToHandle(&resolvedToken, nullptr, true);
                if (op1 == nullptr)
                { // compDonotInline()
                    return;
                }

                helper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE;
                assert(resolvedToken.hClass != nullptr);

                if (resolvedToken.hMethod != nullptr)
                {
                    helper = CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD;
                }
                else if (resolvedToken.hField != nullptr)
                {
                    helper = CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD;
                }

                GenTreeCall::Use* helperArgs = gtNewCallArgs(op1);

                op1 = gtNewHelperCallNode(helper, TYP_STRUCT, helperArgs);

                // The handle struct is returned in register and
                // it could be consumed both as `TYP_STRUCT` and `TYP_REF`.
                op1->AsCall()->gtReturnType = GetRuntimeHandleUnderlyingType();
#if FEATURE_MULTIREG_RET
                op1->AsCall()->InitializeStructReturnType(this, tokenType, op1->AsCall()->GetUnmanagedCallConv());
#endif
                op1->AsCall()->gtRetClsHnd = tokenType;

                tiRetVal = verMakeTypeInfo(tokenType);
                impPushOnStack(op1, tiRetVal);
            }
            break;

            case CEE_UNBOX:
            case CEE_UNBOX_ANY:
            {
                /* Get the Class index */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                bool runtimeLookup;
                op2 = impTokenToHandle(&resolvedToken, &runtimeLookup);
                if (op2 == nullptr)
                {
                    assert(compDonotInline());
                    return;
                }

                // Run this always so we can get access exceptions even with SkipVerification.
                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                if (opcode == CEE_UNBOX_ANY && !eeIsValueClass(resolvedToken.hClass))
                {
                    if (tiVerificationNeeded)
                    {
                        typeInfo tiUnbox = impStackTop().seTypeInfo;
                        Verify(tiUnbox.IsObjRef(), "bad unbox.any arg");
                        tiRetVal = verMakeTypeInfo(resolvedToken.hClass);
                        tiRetVal.NormaliseForStack();
                    }
                    JITDUMP("\n Importing UNBOX.ANY(refClass) as CASTCLASS\n");
                    op1 = impPopStack().val;
                    goto CASTCLASS;
                }

                /* Pop the object and create the unbox helper call */
                /* You might think that for UNBOX_ANY we need to push a different */
                /* (non-byref) type, but here we're making the tiRetVal that is used */
                /* for the intermediate pointer which we then transfer onto the OBJ */
                /* instruction.  OBJ then creates the appropriate tiRetVal. */
                if (tiVerificationNeeded)
                {
                    typeInfo tiUnbox = impStackTop().seTypeInfo;
                    Verify(tiUnbox.IsObjRef(), "Bad unbox arg");

                    tiRetVal = verMakeTypeInfo(resolvedToken.hClass);
                    Verify(tiRetVal.IsValueClass(), "not value class");
                    tiRetVal.MakeByRef();

                    // We always come from an objref, so this is safe byref
                    tiRetVal.SetIsPermanentHomeByRef();
                    tiRetVal.SetIsReadonlyByRef();
                }

                op1 = impPopStack().val;
                assertImp(op1->gtType == TYP_REF);

                helper = info.compCompHnd->getUnBoxHelper(resolvedToken.hClass);
                assert(helper == CORINFO_HELP_UNBOX || helper == CORINFO_HELP_UNBOX_NULLABLE);

                // Check legality and profitability of inline expansion for unboxing.
                const bool canExpandInline    = (helper == CORINFO_HELP_UNBOX);
                const bool shouldExpandInline = !compCurBB->isRunRarely() && opts.OptimizationEnabled();

                if (canExpandInline && shouldExpandInline)
                {
                    // See if we know anything about the type of op1, the object being unboxed.
                    bool                 isExact   = false;
                    bool                 isNonNull = false;
                    CORINFO_CLASS_HANDLE clsHnd    = gtGetClassHandle(op1, &isExact, &isNonNull);

                    // We can skip the "exact" bit here as we are comparing to a value class.
                    // compareTypesForEquality should bail on comparisions for shared value classes.
                    if (clsHnd != NO_CLASS_HANDLE)
                    {
                        const TypeCompareState compare =
                            info.compCompHnd->compareTypesForEquality(resolvedToken.hClass, clsHnd);

                        if (compare == TypeCompareState::Must)
                        {
                            JITDUMP("\nOptimizing %s (%s) -- type test will succeed\n",
                                    opcode == CEE_UNBOX ? "UNBOX" : "UNBOX.ANY", eeGetClassName(clsHnd));

                            // For UNBOX, null check (if necessary), and then leave the box payload byref on the stack.
                            if (opcode == CEE_UNBOX)
                            {
                                GenTree* cloneOperand;
                                op1 = impCloneExpr(op1, &cloneOperand, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                                   nullptr DEBUGARG("optimized unbox clone"));

                                GenTree* boxPayloadOffset = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                                GenTree* boxPayloadAddress =
                                    gtNewOperNode(GT_ADD, TYP_BYREF, cloneOperand, boxPayloadOffset);
                                GenTree* nullcheck = gtNewNullCheck(op1, block);
                                GenTree* result    = gtNewOperNode(GT_COMMA, TYP_BYREF, nullcheck, boxPayloadAddress);
                                impPushOnStack(result, tiRetVal);
                                break;
                            }

                            // For UNBOX.ANY load the struct from the box payload byref (the load will nullcheck)
                            assert(opcode == CEE_UNBOX_ANY);
                            GenTree* boxPayloadOffset  = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                            GenTree* boxPayloadAddress = gtNewOperNode(GT_ADD, TYP_BYREF, op1, boxPayloadOffset);
                            impPushOnStack(boxPayloadAddress, tiRetVal);
                            oper = GT_OBJ;
                            goto OBJ;
                        }
                        else
                        {
                            JITDUMP("\nUnable to optimize %s -- can't resolve type comparison\n",
                                    opcode == CEE_UNBOX ? "UNBOX" : "UNBOX.ANY");
                        }
                    }
                    else
                    {
                        JITDUMP("\nUnable to optimize %s -- class for [%06u] not known\n",
                                opcode == CEE_UNBOX ? "UNBOX" : "UNBOX.ANY", dspTreeID(op1));
                    }

                    JITDUMP("\n Importing %s as inline sequence\n", opcode == CEE_UNBOX ? "UNBOX" : "UNBOX.ANY");
                    // we are doing normal unboxing
                    // inline the common case of the unbox helper
                    // UNBOX(exp) morphs into
                    // clone = pop(exp);
                    // ((*clone == typeToken) ? nop : helper(clone, typeToken));
                    // push(clone + TARGET_POINTER_SIZE)
                    //
                    GenTree* cloneOperand;
                    op1 = impCloneExpr(op1, &cloneOperand, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("inline UNBOX clone1"));
                    op1 = gtNewMethodTableLookup(op1);

                    GenTree* condBox = gtNewOperNode(GT_EQ, TYP_INT, op1, op2);

                    op1 = impCloneExpr(cloneOperand, &cloneOperand, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("inline UNBOX clone2"));
                    op2 = impTokenToHandle(&resolvedToken);
                    if (op2 == nullptr)
                    { // compDonotInline()
                        return;
                    }
                    op1 = gtNewHelperCallNode(helper, TYP_VOID, gtNewCallArgs(op2, op1));

                    op1 = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), op1);
                    op1 = gtNewQmarkNode(TYP_VOID, condBox, op1);

                    // QMARK nodes cannot reside on the evaluation stack. Because there
                    // may be other trees on the evaluation stack that side-effect the
                    // sources of the UNBOX operation we must spill the stack.

                    impAppendTree(op1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);

                    // Create the address-expression to reference past the object header
                    // to the beginning of the value-type. Today this means adjusting
                    // past the base of the objects vtable field which is pointer sized.

                    op2 = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, cloneOperand, op2);
                }
                else
                {
                    JITDUMP("\n Importing %s as helper call because %s\n", opcode == CEE_UNBOX ? "UNBOX" : "UNBOX.ANY",
                            canExpandInline ? "want smaller code or faster jitting" : "inline expansion not legal");

                    // Don't optimize, just call the helper and be done with it
                    op1 = gtNewHelperCallNode(helper,
                                              (var_types)((helper == CORINFO_HELP_UNBOX) ? TYP_BYREF : TYP_STRUCT),
                                              gtNewCallArgs(op2, op1));
                    if (op1->gtType == TYP_STRUCT)
                    {
                        op1->AsCall()->gtRetClsHnd = resolvedToken.hClass;
                    }
                }

                assert((helper == CORINFO_HELP_UNBOX && op1->gtType == TYP_BYREF) || // Unbox helper returns a byref.
                       (helper == CORINFO_HELP_UNBOX_NULLABLE &&
                        varTypeIsStruct(op1)) // UnboxNullable helper returns a struct.
                       );

                /*
                  ----------------------------------------------------------------------
                  | \ helper  |                         |                              |
                  |   \       |                         |                              |
                  |     \     | CORINFO_HELP_UNBOX      | CORINFO_HELP_UNBOX_NULLABLE  |
                  |       \   | (which returns a BYREF) | (which returns a STRUCT)     |                              |
                  | opcode  \ |                         |                              |
                  |---------------------------------------------------------------------
                  | UNBOX     | push the BYREF          | spill the STRUCT to a local, |
                  |           |                         | push the BYREF to this local |
                  |---------------------------------------------------------------------
                  | UNBOX_ANY | push a GT_OBJ of        | push the STRUCT              |
                  |           | the BYREF               | For Linux when the           |
                  |           |                         |  struct is returned in two   |
                  |           |                         |  registers create a temp     |
                  |           |                         |  which address is passed to  |
                  |           |                         |  the unbox_nullable helper.  |
                  |---------------------------------------------------------------------
                */

                if (opcode == CEE_UNBOX)
                {
                    if (helper == CORINFO_HELP_UNBOX_NULLABLE)
                    {
                        // Unbox nullable helper returns a struct type.
                        // We need to spill it to a temp so than can take the address of it.
                        // Here we need unsafe value cls check, since the address of struct is taken to be used
                        // further along and potetially be exploitable.

                        unsigned tmp = lvaGrabTemp(true DEBUGARG("UNBOXing a nullable"));
                        lvaSetStruct(tmp, resolvedToken.hClass, true /* unsafe value cls check */);

                        op2 = gtNewLclvNode(tmp, TYP_STRUCT);
                        op1 = impAssignStruct(op2, op1, resolvedToken.hClass, (unsigned)CHECK_SPILL_ALL);
                        assert(op1->gtType == TYP_VOID); // We must be assigning the return struct to the temp.

                        op2 = gtNewLclvNode(tmp, TYP_STRUCT);
                        op2 = gtNewOperNode(GT_ADDR, TYP_BYREF, op2);
                        op1 = gtNewOperNode(GT_COMMA, TYP_BYREF, op1, op2);
                    }

                    assert(op1->gtType == TYP_BYREF);
                    assert(!tiVerificationNeeded || tiRetVal.IsByRef());
                }
                else
                {
                    assert(opcode == CEE_UNBOX_ANY);

                    if (helper == CORINFO_HELP_UNBOX)
                    {
                        // Normal unbox helper returns a TYP_BYREF.
                        impPushOnStack(op1, tiRetVal);
                        oper = GT_OBJ;
                        goto OBJ;
                    }

                    assert(helper == CORINFO_HELP_UNBOX_NULLABLE && "Make sure the helper is nullable!");

#if FEATURE_MULTIREG_RET

                    if (varTypeIsStruct(op1) &&
                        IsMultiRegReturnedType(resolvedToken.hClass, CorInfoCallConvExtension::Managed))
                    {
                        // Unbox nullable helper returns a TYP_STRUCT.
                        // For the multi-reg case we need to spill it to a temp so that
                        // we can pass the address to the unbox_nullable jit helper.

                        unsigned tmp = lvaGrabTemp(true DEBUGARG("UNBOXing a register returnable nullable"));
                        lvaTable[tmp].lvIsMultiRegArg = true;
                        lvaSetStruct(tmp, resolvedToken.hClass, true /* unsafe value cls check */);

                        op2 = gtNewLclvNode(tmp, TYP_STRUCT);
                        op1 = impAssignStruct(op2, op1, resolvedToken.hClass, (unsigned)CHECK_SPILL_ALL);
                        assert(op1->gtType == TYP_VOID); // We must be assigning the return struct to the temp.

                        op2 = gtNewLclvNode(tmp, TYP_STRUCT);
                        op2 = gtNewOperNode(GT_ADDR, TYP_BYREF, op2);
                        op1 = gtNewOperNode(GT_COMMA, TYP_BYREF, op1, op2);

                        // In this case the return value of the unbox helper is TYP_BYREF.
                        // Make sure the right type is placed on the operand type stack.
                        impPushOnStack(op1, tiRetVal);

                        // Load the struct.
                        oper = GT_OBJ;

                        assert(op1->gtType == TYP_BYREF);
                        assert(!tiVerificationNeeded || tiRetVal.IsByRef());

                        goto OBJ;
                    }
                    else

#endif // !FEATURE_MULTIREG_RET

                    {
                        // If non register passable struct we have it materialized in the RetBuf.
                        assert(op1->gtType == TYP_STRUCT);
                        tiRetVal = verMakeTypeInfo(resolvedToken.hClass);
                        assert(tiRetVal.IsValueClass());
                    }
                }

                impPushOnStack(op1, tiRetVal);
            }
            break;

            case CEE_BOX:
            {
                /* Get the Class index */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Box);

                JITDUMP(" %08X", resolvedToken.token);

                if (tiVerificationNeeded)
                {
                    typeInfo tiActual = impStackTop().seTypeInfo;
                    typeInfo tiBox    = verMakeTypeInfo(resolvedToken.hClass);

                    Verify(verIsBoxable(tiBox), "boxable type expected");

                    // check the class constraints of the boxed type in case we are boxing an uninitialized value
                    Verify(info.compCompHnd->satisfiesClassConstraints(resolvedToken.hClass),
                           "boxed type has unsatisfied class constraints");

                    Verify(tiCompatibleWith(tiActual, tiBox.NormaliseForStack(), true), "type mismatch");

                    // Observation: the following code introduces a boxed value class on the stack, but,
                    // according to the ECMA spec, one would simply expect: tiRetVal =
                    // typeInfo(TI_REF,impGetObjectClass());

                    // Push the result back on the stack,
                    // even if clsHnd is a value class we want the TI_REF
                    // we call back to the EE to get find out what hte type we should push (for nullable<T> we push T)
                    tiRetVal = typeInfo(TI_REF, info.compCompHnd->getTypeForBox(resolvedToken.hClass));
                }

                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                // Note BOX can be used on things that are not value classes, in which
                // case we get a NOP.  However the verifier's view of the type on the
                // stack changes (in generic code a 'T' becomes a 'boxed T')
                if (!eeIsValueClass(resolvedToken.hClass))
                {
                    JITDUMP("\n Importing BOX(refClass) as NOP\n");
                    verCurrentState.esStack[verCurrentState.esStackDepth - 1].seTypeInfo = tiRetVal;
                    break;
                }

                // Look ahead for box idioms
                int matched = impBoxPatternMatch(&resolvedToken, codeAddr + sz, codeEndp);
                if (matched >= 0)
                {
                    // Skip the matched IL instructions
                    sz += matched;
                    break;
                }

                impImportAndPushBox(&resolvedToken);
                if (compDonotInline())
                {
                    return;
                }
            }
            break;

            case CEE_SIZEOF:

                /* Get the Class index */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                if (tiVerificationNeeded)
                {
                    tiRetVal = typeInfo(TI_INT);
                }

                op1 = gtNewIconNode(info.compCompHnd->getClassSize(resolvedToken.hClass));
                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_CASTCLASS:

                /* Get the Class index */

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Casting);

                JITDUMP(" %08X", resolvedToken.token);

                if (!opts.IsReadyToRun())
                {
                    op2 = impTokenToHandle(&resolvedToken, nullptr, false);
                    if (op2 == nullptr)
                    { // compDonotInline()
                        return;
                    }
                }

                if (tiVerificationNeeded)
                {
                    Verify(impStackTop().seTypeInfo.IsObjRef(), "object ref expected");
                    // box it
                    tiRetVal = typeInfo(TI_REF, resolvedToken.hClass);
                }

                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                op1 = impPopStack().val;

            /* Pop the address and create the 'checked cast' helper call */

            // At this point we expect typeRef to contain the token, op1 to contain the value being cast,
            // and op2 to contain code that creates the type handle corresponding to typeRef
            CASTCLASS:
            {
                GenTree* optTree = impOptimizeCastClassOrIsInst(op1, &resolvedToken, true);

                if (optTree != nullptr)
                {
                    impPushOnStack(optTree, tiRetVal);
                }
                else
                {

#ifdef FEATURE_READYTORUN_COMPILER
                    if (opts.IsReadyToRun())
                    {
                        GenTreeCall* opLookup =
                            impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_CHKCAST, TYP_REF,
                                                      gtNewCallArgs(op1));
                        usingReadyToRunHelper = (opLookup != nullptr);
                        op1                   = (usingReadyToRunHelper ? opLookup : op1);

                        if (!usingReadyToRunHelper)
                        {
                            // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                            // and the chkcastany call with a single call to a dynamic R2R cell that will:
                            //      1) Load the context
                            //      2) Perform the generic dictionary lookup and caching, and generate the appropriate
                            //      stub
                            //      3) Check the object on the stack for the type-cast
                            // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                            op2 = impTokenToHandle(&resolvedToken, nullptr, false);
                            if (op2 == nullptr)
                            { // compDonotInline()
                                return;
                            }
                        }
                    }

                    if (!usingReadyToRunHelper)
#endif
                    {
                        op1 = impCastClassOrIsInstToTree(op1, op2, &resolvedToken, true);
                    }
                    if (compDonotInline())
                    {
                        return;
                    }

                    /* Push the result back on the stack */
                    impPushOnStack(op1, tiRetVal);
                }
            }
            break;

            case CEE_THROW:

                if (tiVerificationNeeded)
                {
                    tiRetVal = impStackTop().seTypeInfo;
                    Verify(tiRetVal.IsObjRef(), "object ref expected");
                    if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init))
                    {
                        Verify(!tiRetVal.IsThisPtr(), "throw uninitialized this");
                    }
                }

                // Any block with a throw is rarely executed.
                block->bbSetRunRarely();

                // Pop the exception object and create the 'throw' helper call
                op1 = gtNewHelperCallNode(CORINFO_HELP_THROW, TYP_VOID, gtNewCallArgs(impPopStack().val));

            // Fall through to clear out the eval stack.

            EVAL_APPEND:
                if (verCurrentState.esStackDepth > 0)
                {
                    impEvalSideEffects();
                }

                assert(verCurrentState.esStackDepth == 0);

                goto APPEND;

            case CEE_RETHROW:

                assert(!compIsForInlining());

                if (info.compXcptnsCount == 0)
                {
                    BADCODE("rethrow outside catch");
                }

                if (tiVerificationNeeded)
                {
                    Verify(block->hasHndIndex(), "rethrow outside catch");
                    if (block->hasHndIndex())
                    {
                        EHblkDsc* HBtab = ehGetDsc(block->getHndIndex());
                        Verify(!HBtab->HasFinallyOrFaultHandler(), "rethrow in finally or fault");
                        if (HBtab->HasFilter())
                        {
                            // we better be in the handler clause part, not the filter part
                            Verify(jitIsBetween(compCurBB->bbCodeOffs, HBtab->ebdHndBegOffs(), HBtab->ebdHndEndOffs()),
                                   "rethrow in filter");
                        }
                    }
                }

                /* Create the 'rethrow' helper call */

                op1 = gtNewHelperCallNode(CORINFO_HELP_RETHROW, TYP_VOID);

                goto EVAL_APPEND;

            case CEE_INITOBJ:

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                if (tiVerificationNeeded)
                {
                    typeInfo tiTo    = impStackTop().seTypeInfo;
                    typeInfo tiInstr = verMakeTypeInfo(resolvedToken.hClass);

                    Verify(tiTo.IsByRef(), "byref expected");
                    Verify(!tiTo.IsReadonlyByRef(), "write to readonly byref");

                    Verify(tiCompatibleWith(tiInstr, tiTo.DereferenceByRef(), false),
                           "type operand incompatible with type of address");
                }

                size = info.compCompHnd->getClassSize(resolvedToken.hClass); // Size
                op2  = gtNewIconNode(0);                                     // Value
                op1  = impPopStack().val;                                    // Dest
                op1  = gtNewBlockVal(op1, size);
                op1  = gtNewBlkOpNode(op1, op2, (prefixFlags & PREFIX_VOLATILE) != 0, false);
                goto SPILL_APPEND;

            case CEE_INITBLK:

                if (tiVerificationNeeded)
                {
                    Verify(false, "bad opcode");
                }

                op3 = impPopStack().val; // Size
                op2 = impPopStack().val; // Value
                op1 = impPopStack().val; // Dest

                if (op3->IsCnsIntOrI())
                {
                    size = (unsigned)op3->AsIntConCommon()->IconValue();
                    op1  = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, op1, typGetBlkLayout(size));
                }
                else
                {
                    op1  = new (this, GT_DYN_BLK) GenTreeDynBlk(op1, op3);
                    size = 0;
                }
                op1 = gtNewBlkOpNode(op1, op2, (prefixFlags & PREFIX_VOLATILE) != 0, false);

                goto SPILL_APPEND;

            case CEE_CPBLK:

                if (tiVerificationNeeded)
                {
                    Verify(false, "bad opcode");
                }
                op3 = impPopStack().val; // Size
                op2 = impPopStack().val; // Src
                op1 = impPopStack().val; // Dest

                if (op3->IsCnsIntOrI())
                {
                    size = (unsigned)op3->AsIntConCommon()->IconValue();
                    op1  = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, op1, typGetBlkLayout(size));
                }
                else
                {
                    op1  = new (this, GT_DYN_BLK) GenTreeDynBlk(op1, op3);
                    size = 0;
                }
                if (op2->OperGet() == GT_ADDR)
                {
                    op2 = op2->AsOp()->gtOp1;
                }
                else
                {
                    op2 = gtNewOperNode(GT_IND, TYP_STRUCT, op2);
                }

                op1 = gtNewBlkOpNode(op1, op2, (prefixFlags & PREFIX_VOLATILE) != 0, true);
                goto SPILL_APPEND;

            case CEE_CPOBJ:

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                if (tiVerificationNeeded)
                {
                    typeInfo tiFrom  = impStackTop().seTypeInfo;
                    typeInfo tiTo    = impStackTop(1).seTypeInfo;
                    typeInfo tiInstr = verMakeTypeInfo(resolvedToken.hClass);

                    Verify(tiFrom.IsByRef(), "expected byref source");
                    Verify(tiTo.IsByRef(), "expected byref destination");

                    Verify(tiCompatibleWith(tiFrom.DereferenceByRef(), tiInstr, false),
                           "type of source address incompatible with type operand");
                    Verify(!tiTo.IsReadonlyByRef(), "write to readonly byref");
                    Verify(tiCompatibleWith(tiInstr, tiTo.DereferenceByRef(), false),
                           "type operand incompatible with type of destination address");
                }

                if (!eeIsValueClass(resolvedToken.hClass))
                {
                    op1 = impPopStack().val; // address to load from

                    impBashVarAddrsToI(op1);

                    assertImp(genActualType(op1->gtType) == TYP_I_IMPL || op1->gtType == TYP_BYREF);

                    op1 = gtNewOperNode(GT_IND, TYP_REF, op1);
                    op1->gtFlags |= GTF_EXCEPT | GTF_GLOB_REF;

                    impPushOnStack(op1, typeInfo());
                    opcode = CEE_STIND_REF;
                    lclTyp = TYP_REF;
                    goto STIND_POST_VERIFY;
                }

                op2 = impPopStack().val; // Src
                op1 = impPopStack().val; // Dest
                op1 = gtNewCpObjNode(op1, op2, resolvedToken.hClass, ((prefixFlags & PREFIX_VOLATILE) != 0));
                goto SPILL_APPEND;

            case CEE_STOBJ:
            {
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                if (eeIsValueClass(resolvedToken.hClass))
                {
                    lclTyp = TYP_STRUCT;
                }
                else
                {
                    lclTyp = TYP_REF;
                }

                if (tiVerificationNeeded)
                {

                    typeInfo tiPtr = impStackTop(1).seTypeInfo;

                    // Make sure we have a good looking byref
                    Verify(tiPtr.IsByRef(), "pointer not byref");
                    Verify(!tiPtr.IsReadonlyByRef(), "write to readonly byref");
                    if (!tiPtr.IsByRef() || tiPtr.IsReadonlyByRef())
                    {
                        compUnsafeCastUsed = true;
                    }

                    typeInfo ptrVal = DereferenceByRef(tiPtr);
                    typeInfo argVal = verMakeTypeInfo(resolvedToken.hClass);

                    if (!tiCompatibleWith(impStackTop(0).seTypeInfo, NormaliseForStack(argVal), true))
                    {
                        Verify(false, "type of value incompatible with type operand");
                        compUnsafeCastUsed = true;
                    }

                    if (!tiCompatibleWith(argVal, ptrVal, false))
                    {
                        Verify(false, "type operand incompatible with type of address");
                        compUnsafeCastUsed = true;
                    }
                }
                else
                {
                    compUnsafeCastUsed = true;
                }

                if (lclTyp == TYP_REF)
                {
                    opcode = CEE_STIND_REF;
                    goto STIND_POST_VERIFY;
                }

                CorInfoType jitTyp = info.compCompHnd->asCorInfoType(resolvedToken.hClass);
                if (impIsPrimitive(jitTyp))
                {
                    lclTyp = JITtype2varType(jitTyp);
                    goto STIND_POST_VERIFY;
                }

                op2 = impPopStack().val; // Value
                op1 = impPopStack().val; // Ptr

                assertImp(varTypeIsStruct(op2));

                op1 = impAssignStructPtr(op1, op2, resolvedToken.hClass, (unsigned)CHECK_SPILL_ALL);

                if (op1->OperIsBlkOp() && (prefixFlags & PREFIX_UNALIGNED))
                {
                    op1->gtFlags |= GTF_BLK_UNALIGNED;
                }
                goto SPILL_APPEND;
            }

            case CEE_MKREFANY:

                assert(!compIsForInlining());

                // Being lazy here. Refanys are tricky in terms of gc tracking.
                // Since it is uncommon, just don't perform struct promotion in any method that contains mkrefany.

                JITDUMP("disabling struct promotion because of mkrefany\n");
                fgNoStructPromotion = true;

                oper = GT_MKREFANY;
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

                op2 = impTokenToHandle(&resolvedToken, nullptr, true);
                if (op2 == nullptr)
                { // compDonotInline()
                    return;
                }

                if (tiVerificationNeeded)
                {
                    typeInfo tiPtr   = impStackTop().seTypeInfo;
                    typeInfo tiInstr = verMakeTypeInfo(resolvedToken.hClass);

                    Verify(!verIsByRefLike(tiInstr), "mkrefany of byref-like class");
                    Verify(!tiPtr.IsReadonlyByRef(), "readonly byref used with mkrefany");
                    Verify(typeInfo::AreEquivalent(tiPtr.DereferenceByRef(), tiInstr), "type mismatch");
                }

                accessAllowedResult =
                    info.compCompHnd->canAccessClass(&resolvedToken, info.compMethodHnd, &calloutHelper);
                impHandleAccessAllowed(accessAllowedResult, &calloutHelper);

                op1 = impPopStack().val;

                // @SPECVIOLATION: TYP_INT should not be allowed here by a strict reading of the spec.
                // But JIT32 allowed it, so we continue to allow it.
                assertImp(op1->TypeGet() == TYP_BYREF || op1->TypeGet() == TYP_I_IMPL || op1->TypeGet() == TYP_INT);

                // MKREFANY returns a struct.  op2 is the class token.
                op1 = gtNewOperNode(oper, TYP_STRUCT, op1, op2);

                impPushOnStack(op1, verMakeTypeInfo(impGetRefAnyClass()));
                break;

            case CEE_LDOBJ:
            {
                oper = GT_OBJ;
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Class);

                JITDUMP(" %08X", resolvedToken.token);

            OBJ:

                tiRetVal = verMakeTypeInfo(resolvedToken.hClass);

                if (tiVerificationNeeded)
                {
                    typeInfo tiPtr = impStackTop().seTypeInfo;

                    // Make sure we have a byref
                    if (!tiPtr.IsByRef())
                    {
                        Verify(false, "pointer not byref");
                        compUnsafeCastUsed = true;
                    }
                    typeInfo tiPtrVal = DereferenceByRef(tiPtr);

                    if (!tiCompatibleWith(tiPtrVal, tiRetVal, false))
                    {
                        Verify(false, "type of address incompatible with type operand");
                        compUnsafeCastUsed = true;
                    }
                    tiRetVal.NormaliseForStack();
                }
                else
                {
                    compUnsafeCastUsed = true;
                }

                if (eeIsValueClass(resolvedToken.hClass))
                {
                    lclTyp = TYP_STRUCT;
                }
                else
                {
                    lclTyp = TYP_REF;
                    opcode = CEE_LDIND_REF;
                    goto LDIND_POST_VERIFY;
                }

                op1 = impPopStack().val;

                assertImp(op1->TypeGet() == TYP_BYREF || op1->TypeGet() == TYP_I_IMPL);

                CorInfoType jitTyp = info.compCompHnd->asCorInfoType(resolvedToken.hClass);
                if (impIsPrimitive(jitTyp))
                {
                    op1 = gtNewOperNode(GT_IND, JITtype2varType(jitTyp), op1);

                    // Could point anywhere, example a boxed class static int
                    op1->gtFlags |= GTF_IND_TGTANYWHERE | GTF_GLOB_REF;
                    assertImp(varTypeIsArithmetic(op1->gtType));
                }
                else
                {
                    // OBJ returns a struct
                    // and an inline argument which is the class token of the loaded obj
                    op1 = gtNewObjNode(resolvedToken.hClass, op1);
                }
                op1->gtFlags |= GTF_EXCEPT;

                if (prefixFlags & PREFIX_UNALIGNED)
                {
                    op1->gtFlags |= GTF_IND_UNALIGNED;
                }

                impPushOnStack(op1, tiRetVal);
                break;
            }

            case CEE_LDLEN:
                if (tiVerificationNeeded)
                {
                    typeInfo tiArray = impStackTop().seTypeInfo;
                    Verify(verIsSDArray(tiArray), "bad array");
                    tiRetVal = typeInfo(TI_INT);
                }

                op1 = impPopStack().val;
                if (opts.OptimizationEnabled())
                {
                    /* Use GT_ARR_LENGTH operator so rng check opts see this */
                    GenTreeArrLen* arrLen = gtNewArrLen(TYP_INT, op1, OFFSETOF__CORINFO_Array__length, block);

                    op1 = arrLen;
                }
                else
                {
                    /* Create the expression "*(array_addr + ArrLenOffs)" */
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        gtNewIconNode(OFFSETOF__CORINFO_Array__length, TYP_I_IMPL));
                    op1 = gtNewIndir(TYP_INT, op1);
                }

                /* Push the result back on the stack */
                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_BREAK:
                op1 = gtNewHelperCallNode(CORINFO_HELP_USER_BREAKPOINT, TYP_VOID);
                goto SPILL_APPEND;

            case CEE_NOP:
                if (opts.compDbgCode)
                {
                    op1 = new (this, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
                    goto SPILL_APPEND;
                }
                break;

            /******************************** NYI *******************************/

            case 0xCC:
                OutputDebugStringA("CLR: Invalid x86 breakpoint in IL stream\n");
                FALLTHROUGH;

            case CEE_ILLEGAL:
            case CEE_MACRO_END:

            default:
                if (compIsForInlining())
                {
                    compInlineResult->NoteFatal(InlineObservation::CALLEE_COMPILATION_ERROR);
                    return;
                }

                BADCODE3("unknown opcode", ": %02X", (int)opcode);
        }

        codeAddr += sz;
        prevOpcode = opcode;

        prefixFlags = 0;
    }

    return;
#undef _impResolveToken
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

// Push a local/argument treeon the operand stack
void Compiler::impPushVar(GenTree* op, typeInfo tiRetVal)
{
    tiRetVal.NormaliseForStack();

    if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init) && tiRetVal.IsThisPtr())
    {
        tiRetVal.SetUninitialisedObjRef();
    }

    impPushOnStack(op, tiRetVal);
}

// Load a local/argument on the operand stack
// lclNum is an index into lvaTable *NOT* the arg/lcl index in the IL
void Compiler::impLoadVar(unsigned lclNum, IL_OFFSET offset, const typeInfo& tiRetVal)
{
    var_types lclTyp;

    if (lvaTable[lclNum].lvNormalizeOnLoad())
    {
        lclTyp = lvaGetRealType(lclNum);
    }
    else
    {
        lclTyp = lvaGetActualType(lclNum);
    }

    impPushVar(gtNewLclvNode(lclNum, lclTyp DEBUGARG(offset)), tiRetVal);
}

// Load an argument on the operand stack
// Shared by the various CEE_LDARG opcodes
// ilArgNum is the argument index as specified in IL.
// It will be mapped to the correct lvaTable index
void Compiler::impLoadArg(unsigned ilArgNum, IL_OFFSET offset)
{
    Verify(ilArgNum < info.compILargsCount, "bad arg num");

    if (compIsForInlining())
    {
        if (ilArgNum >= info.compArgsCount)
        {
            compInlineResult->NoteFatal(InlineObservation::CALLEE_BAD_ARGUMENT_NUMBER);
            return;
        }

        impPushVar(impInlineFetchArg(ilArgNum, impInlineInfo->inlArgInfo, impInlineInfo->lclVarInfo),
                   impInlineInfo->lclVarInfo[ilArgNum].lclVerTypeInfo);
    }
    else
    {
        if (ilArgNum >= info.compArgsCount)
        {
            BADCODE("Bad IL");
        }

        unsigned lclNum = compMapILargNum(ilArgNum); // account for possible hidden param

        if (lclNum == info.compThisArg)
        {
            lclNum = lvaArg0Var;
        }

        impLoadVar(lclNum, offset);
    }
}

// Load a local on the operand stack
// Shared by the various CEE_LDLOC opcodes
// ilLclNum is the local index as specified in IL.
// It will be mapped to the correct lvaTable index
void Compiler::impLoadLoc(unsigned ilLclNum, IL_OFFSET offset)
{
    if (tiVerificationNeeded)
    {
        Verify(ilLclNum < info.compMethodInfo->locals.numArgs, "bad loc num");
        Verify(info.compInitMem, "initLocals not set");
    }

    if (compIsForInlining())
    {
        if (ilLclNum >= info.compMethodInfo->locals.numArgs)
        {
            compInlineResult->NoteFatal(InlineObservation::CALLEE_BAD_LOCAL_NUMBER);
            return;
        }

        // Get the local type
        var_types lclTyp = impInlineInfo->lclVarInfo[ilLclNum + impInlineInfo->argCnt].lclTypeInfo;

        typeInfo tiRetVal = impInlineInfo->lclVarInfo[ilLclNum + impInlineInfo->argCnt].lclVerTypeInfo;

        /* Have we allocated a temp for this local? */

        unsigned lclNum = impInlineFetchLocal(ilLclNum DEBUGARG("Inline ldloc first use temp"));

        // All vars of inlined methods should be !lvNormalizeOnLoad()

        assert(!lvaTable[lclNum].lvNormalizeOnLoad());
        lclTyp = genActualType(lclTyp);

        impPushVar(gtNewLclvNode(lclNum, lclTyp), tiRetVal);
    }
    else
    {
        if (ilLclNum >= info.compMethodInfo->locals.numArgs)
        {
            BADCODE("Bad IL");
        }

        unsigned lclNum = info.compArgsCount + ilLclNum;

        impLoadVar(lclNum, offset);
    }
}

#ifdef TARGET_ARM
/**************************************************************************************
 *
 *  When assigning a vararg call src to a HFA lcl dest, mark that we cannot promote the
 *  dst struct, because struct promotion will turn it into a float/double variable while
 *  the rhs will be an int/long variable. We don't code generate assignment of int into
 *  a float, but there is nothing that might prevent us from doing so. The tree however
 *  would like: (=, (typ_float, typ_int)) or (GT_TRANSFER, (typ_float, typ_int))
 *
 *  tmpNum - the lcl dst variable num that is a struct.
 *  src    - the src tree assigned to the dest that is a struct/int (when varargs call.)
 *  hClass - the type handle for the struct variable.
 *
 *  TODO-ARM-CQ: [301608] This is a rare scenario with varargs and struct promotion coming into play,
 *        however, we could do a codegen of transferring from int to float registers
 *        (transfer, not a cast.)
 *
 */
void Compiler::impMarkLclDstNotPromotable(unsigned tmpNum, GenTree* src, CORINFO_CLASS_HANDLE hClass)
{
    if (src->gtOper == GT_CALL && src->AsCall()->IsVarargs() && IsHfa(hClass))
    {
        int       hfaSlots = GetHfaCount(hClass);
        var_types hfaType  = GetHfaType(hClass);

        // If we have varargs we morph the method's return type to be "int" irrespective of its original
        // type: struct/float at importer because the ABI calls out return in integer registers.
        // We don't want struct promotion to replace an expression like this:
        //   lclFld_int = callvar_int() into lclFld_float = callvar_int();
        // This means an int is getting assigned to a float without a cast. Prevent the promotion.
        if ((hfaType == TYP_DOUBLE && hfaSlots == sizeof(double) / REGSIZE_BYTES) ||
            (hfaType == TYP_FLOAT && hfaSlots == sizeof(float) / REGSIZE_BYTES))
        {
            // Make sure this struct type stays as struct so we can receive the call in a struct.
            lvaTable[tmpNum].lvIsMultiRegRet = true;
        }
    }
}
#endif // TARGET_ARM

//------------------------------------------------------------------------
// impAssignSmallStructTypeToVar: ensure calls that return small structs whose
//    sizes are not supported integral type sizes return values to temps.
//
// Arguments:
//     op -- call returning a small struct in a register
//     hClass -- class handle for struct
//
// Returns:
//     Tree with reference to struct local to use as call return value.
//
// Remarks:
//     The call will be spilled into a preceding statement.
//     Currently handles struct returns for 3, 5, 6, and 7 byte structs.

GenTree* Compiler::impAssignSmallStructTypeToVar(GenTree* op, CORINFO_CLASS_HANDLE hClass)
{
    unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Return value temp for small struct return"));
    impAssignTempGen(tmpNum, op, hClass, (unsigned)CHECK_SPILL_ALL);
    GenTree* ret = gtNewLclvNode(tmpNum, lvaTable[tmpNum].lvType);
    return ret;
}

#if FEATURE_MULTIREG_RET
//------------------------------------------------------------------------
// impAssignMultiRegTypeToVar: ensure calls that return structs in multiple
//    registers return values to suitable temps.
//
// Arguments:
//     op -- call returning a struct in registers
//     hClass -- class handle for struct
//
// Returns:
//     Tree with reference to struct local to use as call return value.

GenTree* Compiler::impAssignMultiRegTypeToVar(GenTree*             op,
                                              CORINFO_CLASS_HANDLE hClass DEBUGARG(CorInfoCallConvExtension callConv))
{
    unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Return value temp for multireg return"));
    impAssignTempGen(tmpNum, op, hClass, (unsigned)CHECK_SPILL_ALL);
    GenTree* ret = gtNewLclvNode(tmpNum, lvaTable[tmpNum].lvType);

    // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
    ret->gtFlags |= GTF_DONT_CSE;

    assert(IsMultiRegReturnedType(hClass, callConv));

    // Mark the var so that fields are not promoted and stay together.
    lvaTable[tmpNum].lvIsMultiRegRet = true;

    return ret;
}
#endif // FEATURE_MULTIREG_RET

//------------------------------------------------------------------------
// impReturnInstruction: import a return or an explicit tail call
//
// Arguments:
//     prefixFlags -- active IL prefixes
//     opcode -- [in, out] IL opcode
//
// Returns:
//     True if import was successful (may fail for some inlinees)
//
bool Compiler::impReturnInstruction(int prefixFlags, OPCODE& opcode)
{
    const bool isTailCall = (prefixFlags & PREFIX_TAILCALL) != 0;

    if (tiVerificationNeeded)
    {
        verVerifyThisPtrInitialised();

        unsigned expectedStack = 0;
        if (info.compRetType != TYP_VOID)
        {
            typeInfo tiVal = impStackTop().seTypeInfo;
            typeInfo tiDeclared =
                verMakeTypeInfo(info.compMethodInfo->args.retType, info.compMethodInfo->args.retTypeClass);

            Verify(!verIsByRefLike(tiDeclared) || verIsSafeToReturnByRef(tiVal), "byref return");

            Verify(tiCompatibleWith(tiVal, tiDeclared.NormaliseForStack(), true), "type mismatch");
            expectedStack = 1;
        }
        Verify(verCurrentState.esStackDepth == expectedStack, "stack non-empty on return");
    }

#ifdef DEBUG
    // If we are importing an inlinee and have GC ref locals we always
    // need to have a spill temp for the return value.  This temp
    // should have been set up in advance, over in fgFindBasicBlocks.
    if (compIsForInlining() && impInlineInfo->HasGcRefLocals() && (info.compRetType != TYP_VOID))
    {
        assert(lvaInlineeReturnSpillTemp != BAD_VAR_NUM);
    }
#endif // DEBUG

    GenTree*             op2       = nullptr;
    GenTree*             op1       = nullptr;
    CORINFO_CLASS_HANDLE retClsHnd = nullptr;

    if (info.compRetType != TYP_VOID)
    {
        StackEntry se = impPopStack();
        retClsHnd     = se.seTypeInfo.GetClassHandle();
        op2           = se.val;

        if (!compIsForInlining())
        {
            impBashVarAddrsToI(op2);
            op2 = impImplicitIorI4Cast(op2, info.compRetType);
            op2 = impImplicitR4orR8Cast(op2, info.compRetType);
            // Note that we allow TYP_I_IMPL<->TYP_BYREF transformation, but only TYP_I_IMPL<-TYP_REF.
            assertImp((genActualType(op2->TypeGet()) == genActualType(info.compRetType)) ||
                      ((op2->TypeGet() == TYP_I_IMPL) && TypeIs(info.compRetType, TYP_BYREF)) ||
                      (op2->TypeIs(TYP_BYREF, TYP_REF) && (info.compRetType == TYP_I_IMPL)) ||
                      (varTypeIsFloating(op2->gtType) && varTypeIsFloating(info.compRetType)) ||
                      (varTypeIsStruct(op2) && varTypeIsStruct(info.compRetType)));

#ifdef DEBUG
            if (!isTailCall && opts.compGcChecks && (info.compRetType == TYP_REF))
            {
                // DDB 3483  : JIT Stress: early termination of GC ref's life time in exception code path
                // VSW 440513: Incorrect gcinfo on the return value under COMPlus_JitGCChecks=1 for methods with
                // one-return BB.

                assert(op2->gtType == TYP_REF);

                // confirm that the argument is a GC pointer (for debugging (GC stress))
                GenTreeCall::Use* args = gtNewCallArgs(op2);
                op2                    = gtNewHelperCallNode(CORINFO_HELP_CHECK_OBJ, TYP_REF, args);

                if (verbose)
                {
                    printf("\ncompGcChecks tree:\n");
                    gtDispTree(op2);
                }
            }
#endif
        }
        else
        {
            // inlinee's stack should be empty now.
            assert(verCurrentState.esStackDepth == 0);

#ifdef DEBUG
            if (verbose)
            {
                printf("\n\n    Inlinee Return expression (before normalization)  =>\n");
                gtDispTree(op2);
            }
#endif

            // Make sure the type matches the original call.

            var_types returnType       = genActualType(op2->gtType);
            var_types originalCallType = impInlineInfo->inlineCandidateInfo->fncRetType;
            if ((returnType != originalCallType) && (originalCallType == TYP_STRUCT))
            {
                originalCallType = impNormStructType(impInlineInfo->inlineCandidateInfo->methInfo.args.retTypeClass);
            }

            if (returnType != originalCallType)
            {
                // Allow TYP_BYREF to be returned as TYP_I_IMPL and vice versa.
                // Allow TYP_REF to be returned as TYP_I_IMPL and NOT vice verse.
                if ((TypeIs(returnType, TYP_BYREF, TYP_REF) && (originalCallType == TYP_I_IMPL)) ||
                    ((returnType == TYP_I_IMPL) && TypeIs(originalCallType, TYP_BYREF)))
                {
                    JITDUMP("Allowing return type mismatch: have %s, needed %s\n", varTypeName(returnType),
                            varTypeName(originalCallType));
                }
                else
                {
                    JITDUMP("Return type mismatch: have %s, needed %s\n", varTypeName(returnType),
                            varTypeName(originalCallType));
                    compInlineResult->NoteFatal(InlineObservation::CALLSITE_RETURN_TYPE_MISMATCH);
                    return false;
                }
            }

            // Below, we are going to set impInlineInfo->retExpr to the tree with the return
            // expression. At this point, retExpr could already be set if there are multiple
            // return blocks (meaning fgNeedReturnSpillTemp() == true) and one of
            // the other blocks already set it. If there is only a single return block,
            // retExpr shouldn't be set. However, this is not true if we reimport a block
            // with a return. In that case, retExpr will be set, then the block will be
            // reimported, but retExpr won't get cleared as part of setting the block to
            // be reimported. The reimported retExpr value should be the same, so even if
            // we don't unconditionally overwrite it, it shouldn't matter.
            if (info.compRetNativeType != TYP_STRUCT)
            {
                // compRetNativeType is not TYP_STRUCT.
                // This implies it could be either a scalar type or SIMD vector type or
                // a struct type that can be normalized to a scalar type.

                if (varTypeIsStruct(info.compRetType))
                {
                    noway_assert(info.compRetBuffArg == BAD_VAR_NUM);
                    // adjust the type away from struct to integral
                    // and no normalizing
                    op2 = impFixupStructReturnType(op2, retClsHnd, info.compCallConv);
                }
                else
                {
                    // Do we have to normalize?
                    var_types fncRealRetType = JITtype2varType(info.compMethodInfo->args.retType);
                    if ((varTypeIsSmall(op2->TypeGet()) || varTypeIsSmall(fncRealRetType)) &&
                        fgCastNeeded(op2, fncRealRetType))
                    {
                        // Small-typed return values are normalized by the callee
                        op2 = gtNewCastNode(TYP_INT, op2, false, fncRealRetType);
                    }
                }

                if (fgNeedReturnSpillTemp())
                {
                    assert(info.compRetNativeType != TYP_VOID &&
                           (fgMoreThanOneReturnBlock() || impInlineInfo->HasGcRefLocals()));

                    // If this method returns a ref type, track the actual types seen
                    // in the returns.
                    if (info.compRetType == TYP_REF)
                    {
                        bool                 isExact      = false;
                        bool                 isNonNull    = false;
                        CORINFO_CLASS_HANDLE returnClsHnd = gtGetClassHandle(op2, &isExact, &isNonNull);

                        if (impInlineInfo->retExpr == nullptr)
                        {
                            // This is the first return, so best known type is the type
                            // of this return value.
                            impInlineInfo->retExprClassHnd        = returnClsHnd;
                            impInlineInfo->retExprClassHndIsExact = isExact;
                        }
                        else if (impInlineInfo->retExprClassHnd != returnClsHnd)
                        {
                            // This return site type differs from earlier seen sites,
                            // so reset the info and we'll fall back to using the method's
                            // declared return type for the return spill temp.
                            impInlineInfo->retExprClassHnd        = nullptr;
                            impInlineInfo->retExprClassHndIsExact = false;
                        }
                    }

                    impAssignTempGen(lvaInlineeReturnSpillTemp, op2, se.seTypeInfo.GetClassHandle(),
                                     (unsigned)CHECK_SPILL_ALL);

                    var_types lclRetType = lvaGetDesc(lvaInlineeReturnSpillTemp)->lvType;
                    GenTree*  tmpOp2     = gtNewLclvNode(lvaInlineeReturnSpillTemp, lclRetType);

                    op2 = tmpOp2;
#ifdef DEBUG
                    if (impInlineInfo->retExpr)
                    {
                        // Some other block(s) have seen the CEE_RET first.
                        // Better they spilled to the same temp.
                        assert(impInlineInfo->retExpr->gtOper == GT_LCL_VAR);
                        assert(impInlineInfo->retExpr->AsLclVarCommon()->GetLclNum() ==
                               op2->AsLclVarCommon()->GetLclNum());
                    }
#endif
                }

                // If we are inlining a method that returns a struct byref, check whether we are "reinterpreting" the
                // struct.
                GenTree* effectiveRetVal = op2->gtEffectiveVal();
                if ((returnType == TYP_BYREF) && (info.compRetType == TYP_BYREF) &&
                    (effectiveRetVal->OperGet() == GT_ADDR))
                {
                    GenTree* addrChild = effectiveRetVal->gtGetOp1();
                    if (addrChild->OperGet() == GT_LCL_VAR)
                    {
                        LclVarDsc* varDsc = lvaGetDesc(addrChild->AsLclVarCommon());

                        if (varTypeIsStruct(addrChild->TypeGet()) && !isOpaqueSIMDLclVar(varDsc))
                        {
                            CORINFO_CLASS_HANDLE referentClassHandle;
                            CorInfoType          referentType =
                                info.compCompHnd->getChildType(info.compMethodInfo->args.retTypeClass,
                                                               &referentClassHandle);
                            if (varTypeIsStruct(JITtype2varType(referentType)) &&
                                (varDsc->GetStructHnd() != referentClassHandle))
                            {
                                // We are returning a byref to struct1; the method signature specifies return type as
                                // byref
                                // to struct2. struct1 and struct2 are different so we are "reinterpreting" the struct.
                                // This may happen in, for example, System.Runtime.CompilerServices.Unsafe.As<TFrom,
                                // TTo>.
                                // We need to mark the source struct variable as having overlapping fields because its
                                // fields may be accessed using field handles of a different type, which may confuse
                                // optimizations, in particular, value numbering.

                                JITDUMP("\nSetting lvOverlappingFields to true on V%02u because of struct "
                                        "reinterpretation\n",
                                        addrChild->AsLclVarCommon()->GetLclNum());

                                varDsc->lvOverlappingFields = true;
                            }
                        }
                    }
                }

#ifdef DEBUG
                if (verbose)
                {
                    printf("\n\n    Inlinee Return expression (after normalization) =>\n");
                    gtDispTree(op2);
                }
#endif

                // Report the return expression
                impInlineInfo->retExpr = op2;
            }
            else
            {
                // compRetNativeType is TYP_STRUCT.
                // This implies that struct return via RetBuf arg or multi-reg struct return

                GenTreeCall* iciCall = impInlineInfo->iciCall->AsCall();

                // Assign the inlinee return into a spill temp.
                // spill temp only exists if there are multiple return points
                if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
                {
                    // in this case we have to insert multiple struct copies to the temp
                    // and the retexpr is just the temp.
                    assert(info.compRetNativeType != TYP_VOID);
                    assert(fgMoreThanOneReturnBlock() || impInlineInfo->HasGcRefLocals());

                    impAssignTempGen(lvaInlineeReturnSpillTemp, op2, se.seTypeInfo.GetClassHandle(),
                                     (unsigned)CHECK_SPILL_ALL);
                }

#if defined(TARGET_ARM) || defined(UNIX_AMD64_ABI)
#if defined(TARGET_ARM)
                // TODO-ARM64-NYI: HFA
                // TODO-AMD64-Unix and TODO-ARM once the ARM64 functionality is implemented the
                // next ifdefs could be refactored in a single method with the ifdef inside.
                if (IsHfa(retClsHnd))
                {
// Same as !IsHfa but just don't bother with impAssignStructPtr.
#else  // defined(UNIX_AMD64_ABI)
                ReturnTypeDesc retTypeDesc;
                retTypeDesc.InitializeStructReturnType(this, retClsHnd, info.compCallConv);
                unsigned retRegCount = retTypeDesc.GetReturnRegCount();

                if (retRegCount != 0)
                {
                    // If single eightbyte, the return type would have been normalized and there won't be a temp var.
                    // This code will be called only if the struct return has not been normalized (i.e. 2 eightbytes -
                    // max allowed.)
                    assert(retRegCount == MAX_RET_REG_COUNT);
                    // Same as !structDesc.passedInRegisters but just don't bother with impAssignStructPtr.
                    CLANG_FORMAT_COMMENT_ANCHOR;
#endif // defined(UNIX_AMD64_ABI)

                    if (fgNeedReturnSpillTemp())
                    {
                        if (!impInlineInfo->retExpr)
                        {
#if defined(TARGET_ARM)
                            impInlineInfo->retExpr = gtNewLclvNode(lvaInlineeReturnSpillTemp, info.compRetType);
#else  // defined(UNIX_AMD64_ABI)
                            // The inlinee compiler has figured out the type of the temp already. Use it here.
                            impInlineInfo->retExpr =
                                gtNewLclvNode(lvaInlineeReturnSpillTemp, lvaTable[lvaInlineeReturnSpillTemp].lvType);
#endif // defined(UNIX_AMD64_ABI)
                        }
                    }
                    else
                    {
                        impInlineInfo->retExpr = op2;
                    }
                }
                else
#elif defined(TARGET_ARM64)
                ReturnTypeDesc retTypeDesc;
                retTypeDesc.InitializeStructReturnType(this, retClsHnd, info.compCallConv);
                unsigned retRegCount = retTypeDesc.GetReturnRegCount();

                if (retRegCount != 0)
                {
                    assert(!iciCall->HasRetBufArg());
                    assert(retRegCount >= 2);
                    if (fgNeedReturnSpillTemp())
                    {
                        if (!impInlineInfo->retExpr)
                        {
                            // The inlinee compiler has figured out the type of the temp already. Use it here.
                            impInlineInfo->retExpr =
                                gtNewLclvNode(lvaInlineeReturnSpillTemp, lvaTable[lvaInlineeReturnSpillTemp].lvType);
                        }
                    }
                    else
                    {
                        impInlineInfo->retExpr = op2;
                    }
                }
                else
#elif defined(TARGET_X86)
                ReturnTypeDesc retTypeDesc;
                retTypeDesc.InitializeStructReturnType(this, retClsHnd, info.compCallConv);
                unsigned retRegCount = retTypeDesc.GetReturnRegCount();

                if (retRegCount != 0)
                {
                    assert(!iciCall->HasRetBufArg());
                    assert(retRegCount == MAX_RET_REG_COUNT);
                    if (fgNeedReturnSpillTemp())
                    {
                        if (!impInlineInfo->retExpr)
                        {
                            // The inlinee compiler has figured out the type of the temp already. Use it here.
                            impInlineInfo->retExpr =
                                gtNewLclvNode(lvaInlineeReturnSpillTemp, lvaTable[lvaInlineeReturnSpillTemp].lvType);
                        }
                    }
                    else
                    {
                        impInlineInfo->retExpr = op2;
                    }
                }
                else
#endif // defined(TARGET_ARM64)
                {
                    assert(iciCall->HasRetBufArg());
                    GenTree* dest = gtCloneExpr(iciCall->gtCallArgs->GetNode());
                    // spill temp only exists if there are multiple return points
                    if (fgNeedReturnSpillTemp())
                    {
                        // if this is the first return we have seen set the retExpr
                        if (!impInlineInfo->retExpr)
                        {
                            impInlineInfo->retExpr =
                                impAssignStructPtr(dest, gtNewLclvNode(lvaInlineeReturnSpillTemp, info.compRetType),
                                                   retClsHnd, (unsigned)CHECK_SPILL_ALL);
                        }
                    }
                    else
                    {
                        impInlineInfo->retExpr = impAssignStructPtr(dest, op2, retClsHnd, (unsigned)CHECK_SPILL_ALL);
                    }
                }
            }

            if (impInlineInfo->retExpr != nullptr)
            {
                impInlineInfo->retBB = compCurBB;
            }
        }
    }

    if (compIsForInlining())
    {
        return true;
    }

    if (info.compRetType == TYP_VOID)
    {
        // return void
        op1 = new (this, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID);
    }
    else if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        // Assign value to return buff (first param)
        GenTree* retBuffAddr = gtNewLclvNode(info.compRetBuffArg, TYP_BYREF DEBUGARG(impCurStmtOffs));

        op2 = impAssignStructPtr(retBuffAddr, op2, retClsHnd, (unsigned)CHECK_SPILL_ALL);
        impAppendTree(op2, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        // There are cases where the address of the implicit RetBuf should be returned explicitly (in RAX).
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_AMD64)

        // x64 (System V and Win64) calling convention requires to
        // return the implicit return buffer explicitly (in RAX).
        // Change the return type to be BYREF.
        op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
#else // !defined(TARGET_AMD64)
        // In case of non-AMD64 targets the profiler hook requires to return the implicit RetBuf explicitly (in RAX).
        // In such case the return value of the function is changed to BYREF.
        // If profiler hook is not needed the return type of the function is TYP_VOID.
        if (compIsProfilerHookNeeded())
        {
            op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
        }
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        // On ARM64, the native instance calling convention variant
        // requires the implicit ByRef to be explicitly returned.
        else if (callConvIsInstanceMethodCallConv(info.compCallConv))
        {
            op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
        }
#endif
#if defined(TARGET_X86)
        else if (info.compCallConv != CorInfoCallConvExtension::Managed)
        {
            op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
        }
#endif
        else
        {
            // return void
            op1 = new (this, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID);
        }
#endif // !defined(TARGET_AMD64)
    }
    else if (varTypeIsStruct(info.compRetType))
    {
#if !FEATURE_MULTIREG_RET
        // For both ARM architectures the HFA native types are maintained as structs.
        // Also on System V AMD64 the multireg structs returns are also left as structs.
        noway_assert(info.compRetNativeType != TYP_STRUCT);
#endif
        op2 = impFixupStructReturnType(op2, retClsHnd, info.compCallConv);
        // return op2
        var_types returnType = info.compRetType;
        op1                  = gtNewOperNode(GT_RETURN, genActualType(returnType), op2);
    }
    else
    {
        // return op2
        op1 = gtNewOperNode(GT_RETURN, genActualType(info.compRetType), op2);
    }

    // We must have imported a tailcall and jumped to RET
    if (isTailCall)
    {
        assert(verCurrentState.esStackDepth == 0 && impOpcodeIsCallOpcode(opcode));

        opcode = CEE_RET; // To prevent trying to spill if CALL_SITE_BOUNDARIES

        // impImportCall() would have already appended TYP_VOID calls
        if (info.compRetType == TYP_VOID)
        {
            return true;
        }
    }

    impAppendTree(op1, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
#ifdef DEBUG
    // Remember at which BC offset the tree was finished
    impNoteLastILoffs();
#endif
    return true;
}

/*****************************************************************************
 *  Mark the block as unimported.
 *  Note that the caller is responsible for calling impImportBlockPending(),
 *  with the appropriate stack-state
 */

inline void Compiler::impReimportMarkBlock(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose && (block->bbFlags & BBF_IMPORTED))
    {
        printf("\n" FMT_BB " will be reimported\n", block->bbNum);
    }
#endif

    block->bbFlags &= ~BBF_IMPORTED;
}

/*****************************************************************************
 *  Mark the successors of the given block as unimported.
 *  Note that the caller is responsible for calling impImportBlockPending()
 *  for all the successors, with the appropriate stack-state.
 */

void Compiler::impReimportMarkSuccessors(BasicBlock* block)
{
    const unsigned numSuccs = block->NumSucc();
    for (unsigned i = 0; i < numSuccs; i++)
    {
        impReimportMarkBlock(block->GetSucc(i));
    }
}

/*****************************************************************************
 *
 *  Filter wrapper to handle only passed in exception code
 *  from it).
 */

LONG FilterVerificationExceptions(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam)
{
    if (pExceptionPointers->ExceptionRecord->ExceptionCode == SEH_VERIFICATION_EXCEPTION)
    {
        return EXCEPTION_EXECUTE_HANDLER;
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

void Compiler::impVerifyEHBlock(BasicBlock* block, bool isTryStart)
{
    assert(block->hasTryIndex());
    assert(!compIsForInlining());

    unsigned  tryIndex = block->getTryIndex();
    EHblkDsc* HBtab    = ehGetDsc(tryIndex);

    if (isTryStart)
    {
        assert(block->bbFlags & BBF_TRY_BEG);

        // The Stack must be empty
        //
        if (block->bbStkDepth != 0)
        {
            BADCODE("Evaluation stack must be empty on entry into a try block");
        }
    }

    // Save the stack contents, we'll need to restore it later
    //
    SavedStack blockState;
    impSaveStackState(&blockState, false);

    while (HBtab != nullptr)
    {
        if (isTryStart)
        {
            // Are we verifying that an instance constructor properly initializes it's 'this' pointer once?
            //  We do not allow the 'this' pointer to be uninitialized when entering most kinds try regions
            //
            if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init))
            {
                // We  trigger an invalid program exception here unless we have a try/fault region.
                //
                if (HBtab->HasCatchHandler() || HBtab->HasFinallyHandler() || HBtab->HasFilter())
                {
                    BADCODE(
                        "The 'this' pointer of an instance constructor is not intialized upon entry to a try region");
                }
                else
                {
                    // Allow a try/fault region to proceed.
                    assert(HBtab->HasFaultHandler());
                }
            }
        }

        // Recursively process the handler block, if we haven't already done so.
        BasicBlock* hndBegBB = HBtab->ebdHndBeg;

        if (((hndBegBB->bbFlags & BBF_IMPORTED) == 0) && (impGetPendingBlockMember(hndBegBB) == 0))
        {
            //  Construct the proper verification stack state
            //   either empty or one that contains just
            //   the Exception Object that we are dealing with
            //
            verCurrentState.esStackDepth = 0;

            if (handlerGetsXcptnObj(hndBegBB->bbCatchTyp))
            {
                CORINFO_CLASS_HANDLE clsHnd;

                if (HBtab->HasFilter())
                {
                    clsHnd = impGetObjectClass();
                }
                else
                {
                    CORINFO_RESOLVED_TOKEN resolvedToken;

                    resolvedToken.tokenContext = impTokenLookupContextHandle;
                    resolvedToken.tokenScope   = info.compScopeHnd;
                    resolvedToken.token        = HBtab->ebdTyp;
                    resolvedToken.tokenType    = CORINFO_TOKENKIND_Class;
                    info.compCompHnd->resolveToken(&resolvedToken);

                    clsHnd = resolvedToken.hClass;
                }

                // push catch arg the stack, spill to a temp if necessary
                // Note: can update HBtab->ebdHndBeg!
                hndBegBB = impPushCatchArgOnStack(hndBegBB, clsHnd, false);
            }

            // Queue up the handler for importing
            //
            impImportBlockPending(hndBegBB);
        }

        // Process the filter block, if we haven't already done so.
        if (HBtab->HasFilter())
        {
            /* @VERIFICATION : Ideally the end of filter state should get
               propagated to the catch handler, this is an incompleteness,
               but is not a security/compliance issue, since the only
               interesting state is the 'thisInit' state.
            */

            BasicBlock* filterBB = HBtab->ebdFilter;

            if (((filterBB->bbFlags & BBF_IMPORTED) == 0) && (impGetPendingBlockMember(filterBB) == 0))
            {
                verCurrentState.esStackDepth = 0;

                // push catch arg the stack, spill to a temp if necessary
                // Note: can update HBtab->ebdFilter!
                const bool isSingleBlockFilter = (filterBB->bbNext == hndBegBB);
                filterBB = impPushCatchArgOnStack(filterBB, impGetObjectClass(), isSingleBlockFilter);

                impImportBlockPending(filterBB);
            }
        }

        // This seems redundant ....??
        if (verTrackObjCtorInitState && HBtab->HasFaultHandler())
        {
            /* Recursively process the handler block */

            verCurrentState.esStackDepth = 0;

            // Queue up the fault handler for importing
            //
            impImportBlockPending(HBtab->ebdHndBeg);
        }

        // Now process our enclosing try index (if any)
        //
        tryIndex = HBtab->ebdEnclosingTryIndex;
        if (tryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
        {
            HBtab = nullptr;
        }
        else
        {
            HBtab = ehGetDsc(tryIndex);
        }
    }

    // Restore the stack contents
    impRestoreStackState(&blockState);
}

//***************************************************************
// Import the instructions for the given basic block.  Perform
// verification, throwing an exception on failure.  Push any successor blocks that are enabled for the first
// time, or whose verification pre-state is changed.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void Compiler::impImportBlock(BasicBlock* block)
{
    // BBF_INTERNAL blocks only exist during importation due to EH canonicalization. We need to
    // handle them specially. In particular, there is no IL to import for them, but we do need
    // to mark them as imported and put their successors on the pending import list.
    if (block->bbFlags & BBF_INTERNAL)
    {
        JITDUMP("Marking BBF_INTERNAL block " FMT_BB " as BBF_IMPORTED\n", block->bbNum);
        block->bbFlags |= BBF_IMPORTED;

        const unsigned numSuccs = block->NumSucc();
        for (unsigned i = 0; i < numSuccs; i++)
        {
            impImportBlockPending(block->GetSucc(i));
        }

        return;
    }

    bool markImport;

    assert(block);

    /* Make the block globaly available */

    compCurBB = block;

#ifdef DEBUG
    /* Initialize the debug variables */
    impCurOpcName = "unknown";
    impCurOpcOffs = block->bbCodeOffs;
#endif

    /* Set the current stack state to the merged result */
    verResetCurrentState(block, &verCurrentState);

    /* Now walk the code and import the IL into GenTrees */

    struct FilterVerificationExceptionsParam
    {
        Compiler*   pThis;
        BasicBlock* block;
    };
    FilterVerificationExceptionsParam param;

    param.pThis = this;
    param.block = block;

    PAL_TRY(FilterVerificationExceptionsParam*, pParam, &param)
    {
        /* @VERIFICATION : For now, the only state propagation from try
           to it's handler is "thisInit" state (stack is empty at start of try).
           In general, for state that we track in verification, we need to
           model the possibility that an exception might happen at any IL
           instruction, so we really need to merge all states that obtain
           between IL instructions in a try block into the start states of
           all handlers.

           However we do not allow the 'this' pointer to be uninitialized when
           entering most kinds try regions (only try/fault are allowed to have
           an uninitialized this pointer on entry to the try)

           Fortunately, the stack is thrown away when an exception
           leads to a handler, so we don't have to worry about that.
           We DO, however, have to worry about the "thisInit" state.
           But only for the try/fault case.

           The only allowed transition is from TIS_Uninit to TIS_Init.

           So for a try/fault region for the fault handler block
           we will merge the start state of the try begin
           and the post-state of each block that is part of this try region
        */

        // merge the start state of the try begin
        //
        if (pParam->block->bbFlags & BBF_TRY_BEG)
        {
            pParam->pThis->impVerifyEHBlock(pParam->block, true);
        }

        pParam->pThis->impImportBlockCode(pParam->block);

        // As discussed above:
        // merge the post-state of each block that is part of this try region
        //
        if (pParam->block->hasTryIndex())
        {
            pParam->pThis->impVerifyEHBlock(pParam->block, false);
        }
    }
    PAL_EXCEPT_FILTER(FilterVerificationExceptions)
    {
        verHandleVerificationFailure(block DEBUGARG(false));
    }
    PAL_ENDTRY

    if (compDonotInline())
    {
        return;
    }

    assert(!compDonotInline());

    markImport = false;

SPILLSTACK:

    unsigned    baseTmp             = NO_BASE_TMP; // input temps assigned to successor blocks
    bool        reimportSpillClique = false;
    BasicBlock* tgtBlock            = nullptr;

    /* If the stack is non-empty, we might have to spill its contents */

    if (verCurrentState.esStackDepth != 0)
    {
        impBoxTemp = BAD_VAR_NUM; // if a box temp is used in a block that leaves something
                                  // on the stack, its lifetime is hard to determine, simply
                                  // don't reuse such temps.

        Statement* addStmt = nullptr;

        /* Do the successors of 'block' have any other predecessors ?
           We do not want to do some of the optimizations related to multiRef
           if we can reimport blocks */

        unsigned multRef = impCanReimport ? unsigned(~0) : 0;

        switch (block->bbJumpKind)
        {
            case BBJ_COND:

                addStmt = impExtractLastStmt();

                assert(addStmt->GetRootNode()->gtOper == GT_JTRUE);

                /* Note if the next block has more than one ancestor */

                multRef |= block->bbNext->bbRefs;

                /* Does the next block have temps assigned? */

                baseTmp  = block->bbNext->bbStkTempsIn;
                tgtBlock = block->bbNext;

                if (baseTmp != NO_BASE_TMP)
                {
                    break;
                }

                /* Try the target of the jump then */

                multRef |= block->bbJumpDest->bbRefs;
                baseTmp  = block->bbJumpDest->bbStkTempsIn;
                tgtBlock = block->bbJumpDest;
                break;

            case BBJ_ALWAYS:
                multRef |= block->bbJumpDest->bbRefs;
                baseTmp  = block->bbJumpDest->bbStkTempsIn;
                tgtBlock = block->bbJumpDest;
                break;

            case BBJ_NONE:
                multRef |= block->bbNext->bbRefs;
                baseTmp  = block->bbNext->bbStkTempsIn;
                tgtBlock = block->bbNext;
                break;

            case BBJ_SWITCH:

                BasicBlock** jmpTab;
                unsigned     jmpCnt;

                addStmt = impExtractLastStmt();
                assert(addStmt->GetRootNode()->gtOper == GT_SWITCH);

                jmpCnt = block->bbJumpSwt->bbsCount;
                jmpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    tgtBlock = (*jmpTab);

                    multRef |= tgtBlock->bbRefs;

                    // Thanks to spill cliques, we should have assigned all or none
                    assert((baseTmp == NO_BASE_TMP) || (baseTmp == tgtBlock->bbStkTempsIn));
                    baseTmp = tgtBlock->bbStkTempsIn;
                    if (multRef > 1)
                    {
                        break;
                    }
                } while (++jmpTab, --jmpCnt);

                break;

            case BBJ_CALLFINALLY:
            case BBJ_EHCATCHRET:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
            case BBJ_THROW:
                NO_WAY("can't have 'unreached' end of BB with non-empty stack");
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

        assert(multRef >= 1);

        /* Do we have a base temp number? */

        bool newTemps = (baseTmp == NO_BASE_TMP);

        if (newTemps)
        {
            /* Grab enough temps for the whole stack */
            baseTmp = impGetSpillTmpBase(block);
        }

        /* Spill all stack entries into temps */
        unsigned level, tempNum;

        JITDUMP("\nSpilling stack entries into temps\n");
        for (level = 0, tempNum = baseTmp; level < verCurrentState.esStackDepth; level++, tempNum++)
        {
            GenTree* tree = verCurrentState.esStack[level].val;

            /* VC generates code where it pushes a byref from one branch, and an int (ldc.i4 0) from
               the other. This should merge to a byref in unverifiable code.
               However, if the branch which leaves the TYP_I_IMPL on the stack is imported first, the
               successor would be imported assuming there was a TYP_I_IMPL on
               the stack. Thus the value would not get GC-tracked. Hence,
               change the temp to TYP_BYREF and reimport the successors.
               Note: We should only allow this in unverifiable code.
            */
            if (tree->gtType == TYP_BYREF && lvaTable[tempNum].lvType == TYP_I_IMPL)
            {
                lvaTable[tempNum].lvType = TYP_BYREF;
                impReimportMarkSuccessors(block);
                markImport = true;
            }

#ifdef TARGET_64BIT
            if (genActualType(tree->gtType) == TYP_I_IMPL && lvaTable[tempNum].lvType == TYP_INT)
            {
                if (tiVerificationNeeded && tgtBlock->bbEntryState != nullptr &&
                    (tgtBlock->bbFlags & BBF_FAILED_VERIFICATION) == 0)
                {
                    // Merge the current state into the entry state of block;
                    // the call to verMergeEntryStates must have changed
                    // the entry state of the block by merging the int local var
                    // and the native-int stack entry.
                    bool changed = false;
                    if (verMergeEntryStates(tgtBlock, &changed))
                    {
                        impRetypeEntryStateTemps(tgtBlock);
                        impReimportBlockPending(tgtBlock);
                        assert(changed);
                    }
                    else
                    {
                        tgtBlock->bbFlags |= BBF_FAILED_VERIFICATION;
                        break;
                    }
                }

                // Some other block in the spill clique set this to "int", but now we have "native int".
                // Change the type and go back to re-import any blocks that used the wrong type.
                lvaTable[tempNum].lvType = TYP_I_IMPL;
                reimportSpillClique      = true;
            }
            else if (genActualType(tree->gtType) == TYP_INT && lvaTable[tempNum].lvType == TYP_I_IMPL)
            {
                // Spill clique has decided this should be "native int", but this block only pushes an "int".
                // Insert a sign-extension to "native int" so we match the clique.
                verCurrentState.esStack[level].val = gtNewCastNode(TYP_I_IMPL, tree, false, TYP_I_IMPL);
            }

            // Consider the case where one branch left a 'byref' on the stack and the other leaves
            // an 'int'. On 32-bit, this is allowed (in non-verifiable code) since they are the same
            // size. JIT64 managed to make this work on 64-bit. For compatibility, we support JIT64
            // behavior instead of asserting and then generating bad code (where we save/restore the
            // low 32 bits of a byref pointer to an 'int' sized local). If the 'int' side has been
            // imported already, we need to change the type of the local and reimport the spill clique.
            // If the 'byref' side has imported, we insert a cast from int to 'native int' to match
            // the 'byref' size.
            if (!tiVerificationNeeded)
            {
                if (genActualType(tree->gtType) == TYP_BYREF && lvaTable[tempNum].lvType == TYP_INT)
                {
                    // Some other block in the spill clique set this to "int", but now we have "byref".
                    // Change the type and go back to re-import any blocks that used the wrong type.
                    lvaTable[tempNum].lvType = TYP_BYREF;
                    reimportSpillClique      = true;
                }
                else if (genActualType(tree->gtType) == TYP_INT && lvaTable[tempNum].lvType == TYP_BYREF)
                {
                    // Spill clique has decided this should be "byref", but this block only pushes an "int".
                    // Insert a sign-extension to "native int" so we match the clique size.
                    verCurrentState.esStack[level].val = gtNewCastNode(TYP_I_IMPL, tree, false, TYP_I_IMPL);
                }
            }
#endif // TARGET_64BIT

            if (tree->gtType == TYP_DOUBLE && lvaTable[tempNum].lvType == TYP_FLOAT)
            {
                // Some other block in the spill clique set this to "float", but now we have "double".
                // Change the type and go back to re-import any blocks that used the wrong type.
                lvaTable[tempNum].lvType = TYP_DOUBLE;
                reimportSpillClique      = true;
            }
            else if (tree->gtType == TYP_FLOAT && lvaTable[tempNum].lvType == TYP_DOUBLE)
            {
                // Spill clique has decided this should be "double", but this block only pushes a "float".
                // Insert a cast to "double" so we match the clique.
                verCurrentState.esStack[level].val = gtNewCastNode(TYP_DOUBLE, tree, false, TYP_DOUBLE);
            }

            /* If addStmt has a reference to tempNum (can only happen if we
               are spilling to the temps already used by a previous block),
               we need to spill addStmt */

            if (addStmt != nullptr && !newTemps && gtHasRef(addStmt->GetRootNode(), tempNum, false))
            {
                GenTree* addTree = addStmt->GetRootNode();

                if (addTree->gtOper == GT_JTRUE)
                {
                    GenTree* relOp = addTree->AsOp()->gtOp1;
                    assert(relOp->OperIsCompare());

                    var_types type = genActualType(relOp->AsOp()->gtOp1->TypeGet());

                    if (gtHasRef(relOp->AsOp()->gtOp1, tempNum, false))
                    {
                        unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt JTRUE ref Op1"));
                        impAssignTempGen(temp, relOp->AsOp()->gtOp1, level);
                        type                 = genActualType(lvaTable[temp].TypeGet());
                        relOp->AsOp()->gtOp1 = gtNewLclvNode(temp, type);
                    }

                    if (gtHasRef(relOp->AsOp()->gtOp2, tempNum, false))
                    {
                        unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt JTRUE ref Op2"));
                        impAssignTempGen(temp, relOp->AsOp()->gtOp2, level);
                        type                 = genActualType(lvaTable[temp].TypeGet());
                        relOp->AsOp()->gtOp2 = gtNewLclvNode(temp, type);
                    }
                }
                else
                {
                    assert(addTree->gtOper == GT_SWITCH && genActualTypeIsIntOrI(addTree->AsOp()->gtOp1->TypeGet()));

                    unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt SWITCH"));
                    impAssignTempGen(temp, addTree->AsOp()->gtOp1, level);
                    addTree->AsOp()->gtOp1 = gtNewLclvNode(temp, genActualType(addTree->AsOp()->gtOp1->TypeGet()));
                }
            }

            /* Spill the stack entry, and replace with the temp */

            if (!impSpillStackEntry(level, tempNum
#ifdef DEBUG
                                    ,
                                    true, "Spill Stack Entry"
#endif
                                    ))
            {
                if (markImport)
                {
                    BADCODE("bad stack state");
                }

                // Oops. Something went wrong when spilling. Bad code.
                verHandleVerificationFailure(block DEBUGARG(true));

                goto SPILLSTACK;
            }
        }

        /* Put back the 'jtrue'/'switch' if we removed it earlier */

        if (addStmt != nullptr)
        {
            impAppendStmt(addStmt, (unsigned)CHECK_SPILL_NONE);
        }
    }

    // Some of the append/spill logic works on compCurBB

    assert(compCurBB == block);

    /* Save the tree list in the block */
    impEndTreeList(block);

    // impEndTreeList sets BBF_IMPORTED on the block
    // We do *NOT* want to set it later than this because
    // impReimportSpillClique might clear it if this block is both a
    // predecessor and successor in the current spill clique
    assert(block->bbFlags & BBF_IMPORTED);

    // If we had a int/native int, or float/double collision, we need to re-import
    if (reimportSpillClique)
    {
        // This will re-import all the successors of block (as well as each of their predecessors)
        impReimportSpillClique(block);

        // For blocks that haven't been imported yet, we still need to mark them as pending import.
        const unsigned numSuccs = block->NumSucc();
        for (unsigned i = 0; i < numSuccs; i++)
        {
            BasicBlock* succ = block->GetSucc(i);
            if ((succ->bbFlags & BBF_IMPORTED) == 0)
            {
                impImportBlockPending(succ);
            }
        }
    }
    else // the normal case
    {
        // otherwise just import the successors of block

        /* Does this block jump to any other blocks? */
        const unsigned numSuccs = block->NumSucc();
        for (unsigned i = 0; i < numSuccs; i++)
        {
            impImportBlockPending(block->GetSucc(i));
        }
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************/
//
// Ensures that "block" is a member of the list of BBs waiting to be imported, pushing it on the list if
// necessary (and ensures that it is a member of the set of BB's on the list, by setting its byte in
// impPendingBlockMembers).  Merges the current verification state into the verification state of "block"
// (its "pre-state").

void Compiler::impImportBlockPending(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nimpImportBlockPending for " FMT_BB "\n", block->bbNum);
    }
#endif

    // We will add a block to the pending set if it has not already been imported (or needs to be re-imported),
    // or if it has, but merging in a predecessor's post-state changes the block's pre-state.
    // (When we're doing verification, we always attempt the merge to detect verification errors.)

    // If the block has not been imported, add to pending set.
    bool addToPending = ((block->bbFlags & BBF_IMPORTED) == 0);

    // Initialize bbEntryState just the first time we try to add this block to the pending list
    // Just because bbEntryState is NULL, doesn't mean the pre-state wasn't previously set
    // We use NULL to indicate the 'common' state to avoid memory allocation
    if ((block->bbEntryState == nullptr) && ((block->bbFlags & (BBF_IMPORTED | BBF_FAILED_VERIFICATION)) == 0) &&
        (impGetPendingBlockMember(block) == 0))
    {
        verInitBBEntryState(block, &verCurrentState);
        assert(block->bbStkDepth == 0);
        block->bbStkDepth = static_cast<unsigned short>(verCurrentState.esStackDepth);
        assert(addToPending);
        assert(impGetPendingBlockMember(block) == 0);
    }
    else
    {
        // The stack should have the same height on entry to the block from all its predecessors.
        if (block->bbStkDepth != verCurrentState.esStackDepth)
        {
#ifdef DEBUG
            char buffer[400];
            sprintf_s(buffer, sizeof(buffer),
                      "Block at offset %4.4x to %4.4x in %0.200s entered with different stack depths.\n"
                      "Previous depth was %d, current depth is %d",
                      block->bbCodeOffs, block->bbCodeOffsEnd, info.compFullName, block->bbStkDepth,
                      verCurrentState.esStackDepth);
            buffer[400 - 1] = 0;
            NO_WAY(buffer);
#else
            NO_WAY("Block entered with different stack depths");
#endif
        }

        // Additionally, if we need to verify, merge the verification state.
        if (tiVerificationNeeded)
        {
            // Merge the current state into the entry state of block; if this does not change the entry state
            // by merging, do not add the block to the pending-list.
            bool changed = false;
            if (!verMergeEntryStates(block, &changed))
            {
                block->bbFlags |= BBF_FAILED_VERIFICATION;
                addToPending = true; // We will pop it off, and check the flag set above.
            }
            else if (changed)
            {
                addToPending = true;

                JITDUMP("Adding " FMT_BB " to pending set due to new merge result\n", block->bbNum);
            }
        }

        if (!addToPending)
        {
            return;
        }

        if (block->bbStkDepth > 0)
        {
            // We need to fix the types of any spill temps that might have changed:
            //   int->native int, float->double, int->byref, etc.
            impRetypeEntryStateTemps(block);
        }

        // OK, we must add to the pending list, if it's not already in it.
        if (impGetPendingBlockMember(block) != 0)
        {
            return;
        }
    }

    // Get an entry to add to the pending list

    PendingDsc* dsc;

    if (impPendingFree)
    {
        // We can reuse one of the freed up dscs.
        dsc            = impPendingFree;
        impPendingFree = dsc->pdNext;
    }
    else
    {
        // We have to create a new dsc
        dsc = new (this, CMK_Unknown) PendingDsc;
    }

    dsc->pdBB                 = block;
    dsc->pdSavedStack.ssDepth = verCurrentState.esStackDepth;
    dsc->pdThisPtrInit        = verCurrentState.thisInitialized;

    // Save the stack trees for later

    if (verCurrentState.esStackDepth)
    {
        impSaveStackState(&dsc->pdSavedStack, false);
    }

    // Add the entry to the pending list

    dsc->pdNext    = impPendingList;
    impPendingList = dsc;
    impSetPendingBlockMember(block, 1); // And indicate that it's now a member of the set.

    // Various assertions require us to now to consider the block as not imported (at least for
    // the final time...)
    block->bbFlags &= ~BBF_IMPORTED;

#ifdef DEBUG
    if (verbose && 0)
    {
        printf("Added PendingDsc - %08p for " FMT_BB "\n", dspPtr(dsc), block->bbNum);
    }
#endif
}

/*****************************************************************************/
//
// Ensures that "block" is a member of the list of BBs waiting to be imported, pushing it on the list if
// necessary (and ensures that it is a member of the set of BB's on the list, by setting its byte in
// impPendingBlockMembers).  Does *NOT* change the existing "pre-state" of the block.

void Compiler::impReimportBlockPending(BasicBlock* block)
{
    JITDUMP("\nimpReimportBlockPending for " FMT_BB, block->bbNum);

    assert(block->bbFlags & BBF_IMPORTED);

    // OK, we must add to the pending list, if it's not already in it.
    if (impGetPendingBlockMember(block) != 0)
    {
        return;
    }

    // Get an entry to add to the pending list

    PendingDsc* dsc;

    if (impPendingFree)
    {
        // We can reuse one of the freed up dscs.
        dsc            = impPendingFree;
        impPendingFree = dsc->pdNext;
    }
    else
    {
        // We have to create a new dsc
        dsc = new (this, CMK_ImpStack) PendingDsc;
    }

    dsc->pdBB = block;

    if (block->bbEntryState)
    {
        dsc->pdThisPtrInit        = block->bbEntryState->thisInitialized;
        dsc->pdSavedStack.ssDepth = block->bbEntryState->esStackDepth;
        dsc->pdSavedStack.ssTrees = block->bbEntryState->esStack;
    }
    else
    {
        dsc->pdThisPtrInit        = TIS_Bottom;
        dsc->pdSavedStack.ssDepth = 0;
        dsc->pdSavedStack.ssTrees = nullptr;
    }

    // Add the entry to the pending list

    dsc->pdNext    = impPendingList;
    impPendingList = dsc;
    impSetPendingBlockMember(block, 1); // And indicate that it's now a member of the set.

    // Various assertions require us to now to consider the block as not imported (at least for
    // the final time...)
    block->bbFlags &= ~BBF_IMPORTED;

#ifdef DEBUG
    if (verbose && 0)
    {
        printf("Added PendingDsc - %08p for " FMT_BB "\n", dspPtr(dsc), block->bbNum);
    }
#endif
}

void* Compiler::BlockListNode::operator new(size_t sz, Compiler* comp)
{
    if (comp->impBlockListNodeFreeList == nullptr)
    {
        return comp->getAllocator(CMK_BasicBlock).allocate<BlockListNode>(1);
    }
    else
    {
        BlockListNode* res             = comp->impBlockListNodeFreeList;
        comp->impBlockListNodeFreeList = res->m_next;
        return res;
    }
}

void Compiler::FreeBlockListNode(Compiler::BlockListNode* node)
{
    node->m_next             = impBlockListNodeFreeList;
    impBlockListNodeFreeList = node;
}

void Compiler::impWalkSpillCliqueFromPred(BasicBlock* block, SpillCliqueWalker* callback)
{
    bool toDo = true;

    noway_assert(!fgComputePredsDone);
    if (!fgCheapPredsValid)
    {
        fgComputeCheapPreds();
    }

    BlockListNode* succCliqueToDo = nullptr;
    BlockListNode* predCliqueToDo = new (this) BlockListNode(block);
    while (toDo)
    {
        toDo = false;
        // Look at the successors of every member of the predecessor to-do list.
        while (predCliqueToDo != nullptr)
        {
            BlockListNode* node = predCliqueToDo;
            predCliqueToDo      = node->m_next;
            BasicBlock* blk     = node->m_blk;
            FreeBlockListNode(node);

            const unsigned numSuccs = blk->NumSucc();
            for (unsigned succNum = 0; succNum < numSuccs; succNum++)
            {
                BasicBlock* succ = blk->GetSucc(succNum);
                // If it's not already in the clique, add it, and also add it
                // as a member of the successor "toDo" set.
                if (impSpillCliqueGetMember(SpillCliqueSucc, succ) == 0)
                {
                    callback->Visit(SpillCliqueSucc, succ);
                    impSpillCliqueSetMember(SpillCliqueSucc, succ, 1);
                    succCliqueToDo = new (this) BlockListNode(succ, succCliqueToDo);
                    toDo           = true;
                }
            }
        }
        // Look at the predecessors of every member of the successor to-do list.
        while (succCliqueToDo != nullptr)
        {
            BlockListNode* node = succCliqueToDo;
            succCliqueToDo      = node->m_next;
            BasicBlock* blk     = node->m_blk;
            FreeBlockListNode(node);

            for (BasicBlockList* pred = blk->bbCheapPreds; pred != nullptr; pred = pred->next)
            {
                BasicBlock* predBlock = pred->block;
                // If it's not already in the clique, add it, and also add it
                // as a member of the predecessor "toDo" set.
                if (impSpillCliqueGetMember(SpillCliquePred, predBlock) == 0)
                {
                    callback->Visit(SpillCliquePred, predBlock);
                    impSpillCliqueSetMember(SpillCliquePred, predBlock, 1);
                    predCliqueToDo = new (this) BlockListNode(predBlock, predCliqueToDo);
                    toDo           = true;
                }
            }
        }
    }

    // If this fails, it means we didn't walk the spill clique properly and somehow managed
    // miss walking back to include the predecessor we started from.
    // This most likely cause: missing or out of date bbPreds
    assert(impSpillCliqueGetMember(SpillCliquePred, block) != 0);
}

void Compiler::SetSpillTempsBase::Visit(SpillCliqueDir predOrSucc, BasicBlock* blk)
{
    if (predOrSucc == SpillCliqueSucc)
    {
        assert(blk->bbStkTempsIn == NO_BASE_TMP); // Should not already be a member of a clique as a successor.
        blk->bbStkTempsIn = m_baseTmp;
    }
    else
    {
        assert(predOrSucc == SpillCliquePred);
        assert(blk->bbStkTempsOut == NO_BASE_TMP); // Should not already be a member of a clique as a predecessor.
        blk->bbStkTempsOut = m_baseTmp;
    }
}

void Compiler::ReimportSpillClique::Visit(SpillCliqueDir predOrSucc, BasicBlock* blk)
{
    // For Preds we could be a little smarter and just find the existing store
    // and re-type it/add a cast, but that is complicated and hopefully very rare, so
    // just re-import the whole block (just like we do for successors)

    if (((blk->bbFlags & BBF_IMPORTED) == 0) && (m_pComp->impGetPendingBlockMember(blk) == 0))
    {
        // If we haven't imported this block and we're not going to (because it isn't on
        // the pending list) then just ignore it for now.

        // This block has either never been imported (EntryState == NULL) or it failed
        // verification. Neither state requires us to force it to be imported now.
        assert((blk->bbEntryState == nullptr) || (blk->bbFlags & BBF_FAILED_VERIFICATION));
        return;
    }

    // For successors we have a valid verCurrentState, so just mark them for reimport
    // the 'normal' way
    // Unlike predecessors, we *DO* need to reimport the current block because the
    // initial import had the wrong entry state types.
    // Similarly, blocks that are currently on the pending list, still need to call
    // impImportBlockPending to fixup their entry state.
    if (predOrSucc == SpillCliqueSucc)
    {
        m_pComp->impReimportMarkBlock(blk);

        // Set the current stack state to that of the blk->bbEntryState
        m_pComp->verResetCurrentState(blk, &m_pComp->verCurrentState);
        assert(m_pComp->verCurrentState.thisInitialized == blk->bbThisOnEntry());

        m_pComp->impImportBlockPending(blk);
    }
    else if ((blk != m_pComp->compCurBB) && ((blk->bbFlags & BBF_IMPORTED) != 0))
    {
        // As described above, we are only visiting predecessors so they can
        // add the appropriate casts, since we have already done that for the current
        // block, it does not need to be reimported.
        // Nor do we need to reimport blocks that are still pending, but not yet
        // imported.
        //
        // For predecessors, we have no state to seed the EntryState, so we just have
        // to assume the existing one is correct.
        // If the block is also a successor, it will get the EntryState properly
        // updated when it is visited as a successor in the above "if" block.
        assert(predOrSucc == SpillCliquePred);
        m_pComp->impReimportBlockPending(blk);
    }
}

// Re-type the incoming lclVar nodes to match the varDsc.
void Compiler::impRetypeEntryStateTemps(BasicBlock* blk)
{
    if (blk->bbEntryState != nullptr)
    {
        EntryState* es = blk->bbEntryState;
        for (unsigned level = 0; level < es->esStackDepth; level++)
        {
            GenTree* tree = es->esStack[level].val;
            if ((tree->gtOper == GT_LCL_VAR) || (tree->gtOper == GT_LCL_FLD))
            {
                unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
                noway_assert(lclNum < lvaCount);
                LclVarDsc* varDsc              = lvaTable + lclNum;
                es->esStack[level].val->gtType = varDsc->TypeGet();
            }
        }
    }
}

unsigned Compiler::impGetSpillTmpBase(BasicBlock* block)
{
    if (block->bbStkTempsOut != NO_BASE_TMP)
    {
        return block->bbStkTempsOut;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In impGetSpillTmpBase(" FMT_BB ")\n", block->bbNum);
    }
#endif // DEBUG

    // Otherwise, choose one, and propagate to all members of the spill clique.
    // Grab enough temps for the whole stack.
    unsigned baseTmp = lvaGrabTemps(verCurrentState.esStackDepth DEBUGARG("IL Stack Entries"));
    SetSpillTempsBase callback(baseTmp);

    // We do *NOT* need to reset the SpillClique*Members because a block can only be the predecessor
    // to one spill clique, and similarly can only be the sucessor to one spill clique
    impWalkSpillCliqueFromPred(block, &callback);

    return baseTmp;
}

void Compiler::impReimportSpillClique(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In impReimportSpillClique(" FMT_BB ")\n", block->bbNum);
    }
#endif // DEBUG

    // If we get here, it is because this block is already part of a spill clique
    // and one predecessor had an outgoing live stack slot of type int, and this
    // block has an outgoing live stack slot of type native int.
    // We need to reset these before traversal because they have already been set
    // by the previous walk to determine all the members of the spill clique.
    impInlineRoot()->impSpillCliquePredMembers.Reset();
    impInlineRoot()->impSpillCliqueSuccMembers.Reset();

    ReimportSpillClique callback(this);

    impWalkSpillCliqueFromPred(block, &callback);
}

// Set the pre-state of "block" (which should not have a pre-state allocated) to
// a copy of "srcState", cloning tree pointers as required.
void Compiler::verInitBBEntryState(BasicBlock* block, EntryState* srcState)
{
    if (srcState->esStackDepth == 0 && srcState->thisInitialized == TIS_Bottom)
    {
        block->bbEntryState = nullptr;
        return;
    }

    block->bbEntryState = getAllocator(CMK_Unknown).allocate<EntryState>(1);

    // block->bbEntryState.esRefcount = 1;

    block->bbEntryState->esStackDepth    = srcState->esStackDepth;
    block->bbEntryState->thisInitialized = TIS_Bottom;

    if (srcState->esStackDepth > 0)
    {
        block->bbSetStack(new (this, CMK_Unknown) StackEntry[srcState->esStackDepth]);
        unsigned stackSize = srcState->esStackDepth * sizeof(StackEntry);

        memcpy(block->bbEntryState->esStack, srcState->esStack, stackSize);
        for (unsigned level = 0; level < srcState->esStackDepth; level++)
        {
            GenTree* tree                           = srcState->esStack[level].val;
            block->bbEntryState->esStack[level].val = gtCloneExpr(tree);
        }
    }

    if (verTrackObjCtorInitState)
    {
        verSetThisInit(block, srcState->thisInitialized);
    }

    return;
}

void Compiler::verSetThisInit(BasicBlock* block, ThisInitState tis)
{
    assert(tis != TIS_Bottom); // Precondition.
    if (block->bbEntryState == nullptr)
    {
        block->bbEntryState = new (this, CMK_Unknown) EntryState();
    }

    block->bbEntryState->thisInitialized = tis;
}

/*
 * Resets the current state to the state at the start of the basic block
 */
void Compiler::verResetCurrentState(BasicBlock* block, EntryState* destState)
{

    if (block->bbEntryState == nullptr)
    {
        destState->esStackDepth    = 0;
        destState->thisInitialized = TIS_Bottom;
        return;
    }

    destState->esStackDepth = block->bbEntryState->esStackDepth;

    if (destState->esStackDepth > 0)
    {
        unsigned stackSize = destState->esStackDepth * sizeof(StackEntry);

        memcpy(destState->esStack, block->bbStackOnEntry(), stackSize);
    }

    destState->thisInitialized = block->bbThisOnEntry();

    return;
}

ThisInitState BasicBlock::bbThisOnEntry()
{
    return bbEntryState ? bbEntryState->thisInitialized : TIS_Bottom;
}

unsigned BasicBlock::bbStackDepthOnEntry()
{
    return (bbEntryState ? bbEntryState->esStackDepth : 0);
}

void BasicBlock::bbSetStack(void* stackBuffer)
{
    assert(bbEntryState);
    assert(stackBuffer);
    bbEntryState->esStack = (StackEntry*)stackBuffer;
}

StackEntry* BasicBlock::bbStackOnEntry()
{
    assert(bbEntryState);
    return bbEntryState->esStack;
}

void Compiler::verInitCurrentState()
{
    verTrackObjCtorInitState        = false;
    verCurrentState.thisInitialized = TIS_Bottom;

    if (tiVerificationNeeded)
    {
        // Track this ptr initialization
        if (!info.compIsStatic && (info.compFlags & CORINFO_FLG_CONSTRUCTOR) && lvaTable[0].lvVerTypeInfo.IsObjRef())
        {
            verTrackObjCtorInitState        = true;
            verCurrentState.thisInitialized = TIS_Uninit;
        }
    }

    // initialize stack info

    verCurrentState.esStackDepth = 0;
    assert(verCurrentState.esStack != nullptr);

    // copy current state to entry state of first BB
    verInitBBEntryState(fgFirstBB, &verCurrentState);
}

Compiler* Compiler::impInlineRoot()
{
    if (impInlineInfo == nullptr)
    {
        return this;
    }
    else
    {
        return impInlineInfo->InlineRoot;
    }
}

BYTE Compiler::impSpillCliqueGetMember(SpillCliqueDir predOrSucc, BasicBlock* blk)
{
    if (predOrSucc == SpillCliquePred)
    {
        return impInlineRoot()->impSpillCliquePredMembers.Get(blk->bbInd());
    }
    else
    {
        assert(predOrSucc == SpillCliqueSucc);
        return impInlineRoot()->impSpillCliqueSuccMembers.Get(blk->bbInd());
    }
}

void Compiler::impSpillCliqueSetMember(SpillCliqueDir predOrSucc, BasicBlock* blk, BYTE val)
{
    if (predOrSucc == SpillCliquePred)
    {
        impInlineRoot()->impSpillCliquePredMembers.Set(blk->bbInd(), val);
    }
    else
    {
        assert(predOrSucc == SpillCliqueSucc);
        impInlineRoot()->impSpillCliqueSuccMembers.Set(blk->bbInd(), val);
    }
}

/*****************************************************************************
 *
 *  Convert the instrs ("import") into our internal format (trees). The
 *  basic flowgraph has already been constructed and is passed in.
 */

void Compiler::impImport()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In impImport() for %s\n", info.compFullName);
    }
#endif

    Compiler* inlineRoot = impInlineRoot();

    if (info.compMaxStack <= SMALL_STACK_SIZE)
    {
        impStkSize = SMALL_STACK_SIZE;
    }
    else
    {
        impStkSize = info.compMaxStack;
    }

    if (this == inlineRoot)
    {
        // Allocate the stack contents
        verCurrentState.esStack = new (this, CMK_ImpStack) StackEntry[impStkSize];
    }
    else
    {
        // This is the inlinee compiler, steal the stack from the inliner compiler
        // (after ensuring that it is large enough).
        if (inlineRoot->impStkSize < impStkSize)
        {
            inlineRoot->impStkSize              = impStkSize;
            inlineRoot->verCurrentState.esStack = new (this, CMK_ImpStack) StackEntry[impStkSize];
        }

        verCurrentState.esStack = inlineRoot->verCurrentState.esStack;
    }

    // initialize the entry state at start of method
    verInitCurrentState();

    // Initialize stuff related to figuring "spill cliques" (see spec comment for impGetSpillTmpBase).
    if (this == inlineRoot) // These are only used on the root of the inlining tree.
    {
        // We have initialized these previously, but to size 0.  Make them larger.
        impPendingBlockMembers.Init(getAllocator(), fgBBNumMax * 2);
        impSpillCliquePredMembers.Init(getAllocator(), fgBBNumMax * 2);
        impSpillCliqueSuccMembers.Init(getAllocator(), fgBBNumMax * 2);
    }
    inlineRoot->impPendingBlockMembers.Reset(fgBBNumMax * 2);
    inlineRoot->impSpillCliquePredMembers.Reset(fgBBNumMax * 2);
    inlineRoot->impSpillCliqueSuccMembers.Reset(fgBBNumMax * 2);
    impBlockListNodeFreeList = nullptr;

#ifdef DEBUG
    impLastILoffsStmt   = nullptr;
    impNestedStackSpill = false;
#endif
    impBoxTemp = BAD_VAR_NUM;

    impPendingList = impPendingFree = nullptr;

    // Skip leading internal blocks.
    // These can arise from needing a leading scratch BB, from EH normalization, and from OSR entry redirects.
    //
    // We expect a linear flow to the first non-internal block. But not necessarily straght-line flow.
    BasicBlock* entryBlock = fgFirstBB;

    while (entryBlock->bbFlags & BBF_INTERNAL)
    {
        JITDUMP("Marking leading BBF_INTERNAL block " FMT_BB " as BBF_IMPORTED\n", entryBlock->bbNum);
        entryBlock->bbFlags |= BBF_IMPORTED;

        if (entryBlock->bbJumpKind == BBJ_NONE)
        {
            entryBlock = entryBlock->bbNext;
        }
        else if (entryBlock->bbJumpKind == BBJ_ALWAYS)
        {
            // Only expected for OSR
            assert(opts.IsOSR());
            entryBlock = entryBlock->bbJumpDest;
        }
        else
        {
            assert(!"unexpected bbJumpKind in entry sequence");
        }
    }

    // Note for OSR we'd like to be able to verify this block must be
    // stack empty, but won't know that until we've imported...so instead
    // we'll BADCODE out if we mess up.
    //
    // (the concern here is that the runtime asks us to OSR a
    // different IL version than the one that matched the method that
    // triggered OSR).  This should not happen but I might have the
    // IL versioning stuff wrong.
    //
    // TODO: we also currently expect this block to be a join point,
    // which we should verify over when we find jump targets.
    impImportBlockPending(entryBlock);

    /* Import blocks in the worker-list until there are no more */

    while (impPendingList)
    {
        /* Remove the entry at the front of the list */

        PendingDsc* dsc = impPendingList;
        impPendingList  = impPendingList->pdNext;
        impSetPendingBlockMember(dsc->pdBB, 0);

        /* Restore the stack state */

        verCurrentState.thisInitialized = dsc->pdThisPtrInit;
        verCurrentState.esStackDepth    = dsc->pdSavedStack.ssDepth;
        if (verCurrentState.esStackDepth)
        {
            impRestoreStackState(&dsc->pdSavedStack);
        }

        /* Add the entry to the free list for reuse */

        dsc->pdNext    = impPendingFree;
        impPendingFree = dsc;

        /* Now import the block */

        if (dsc->pdBB->bbFlags & BBF_FAILED_VERIFICATION)
        {

#ifdef TARGET_64BIT
            // On AMD64, during verification we have to match JIT64 behavior since the VM is very tighly
            // coupled with the JIT64 IL Verification logic.  Look inside verHandleVerificationFailure
            // method for further explanation on why we raise this exception instead of making the jitted
            // code throw the verification exception during execution.
            if (tiVerificationNeeded && opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IMPORT_ONLY))
            {
                BADCODE("Basic block marked as not verifiable");
            }
            else
#endif // TARGET_64BIT
            {
                verConvertBBToThrowVerificationException(dsc->pdBB DEBUGARG(true));
                impEndTreeList(dsc->pdBB);
            }
        }
        else
        {
            impImportBlock(dsc->pdBB);

            if (compDonotInline())
            {
                return;
            }
            if (compIsForImportOnly() && !tiVerificationNeeded)
            {
                return;
            }
        }
    }

#ifdef DEBUG
    if (verbose && info.compXcptnsCount)
    {
        printf("\nAfter impImport() added block for try,catch,finally");
        fgDispBasicBlocks();
        printf("\n");
    }

    // Used in impImportBlockPending() for STRESS_CHK_REIMPORT
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        block->bbFlags &= ~BBF_VISITED;
    }
#endif

    assert(!compIsForInlining() || !tiVerificationNeeded);
}

// Checks if a typeinfo (usually stored in the type stack) is a struct.
// The invariant here is that if it's not a ref or a method and has a class handle
// it's a valuetype
bool Compiler::impIsValueType(typeInfo* pTypeInfo)
{
    if (pTypeInfo && pTypeInfo->IsValueClassWithClsHnd())
    {
        return true;
    }
    else
    {
        return false;
    }
}

/*****************************************************************************
 *  Check to see if the tree is the address of a local or
    the address of a field in a local.

    *lclVarTreeOut will contain the GT_LCL_VAR tree when it returns true.

 */

bool Compiler::impIsAddressInLocal(const GenTree* tree, GenTree** lclVarTreeOut)
{
    if (tree->gtOper != GT_ADDR)
    {
        return false;
    }

    GenTree* op = tree->AsOp()->gtOp1;
    while (op->gtOper == GT_FIELD)
    {
        op = op->AsField()->gtFldObj;
        if (op && op->gtOper == GT_ADDR) // Skip static fields where op will be NULL.
        {
            op = op->AsOp()->gtOp1;
        }
        else
        {
            return false;
        }
    }

    if (op->gtOper == GT_LCL_VAR)
    {
        *lclVarTreeOut = op;
        return true;
    }
    else
    {
        return false;
    }
}

//------------------------------------------------------------------------
// impMakeDiscretionaryInlineObservations: make observations that help
// determine the profitability of a discretionary inline
//
// Arguments:
//    pInlineInfo -- InlineInfo for the inline, or null for the prejit root
//    inlineResult -- InlineResult accumulating information about this inline
//
// Notes:
//    If inlining or prejitting the root, this method also makes
//    various observations about the method that factor into inline
//    decisions. It sets `compNativeSizeEstimate` as a side effect.

void Compiler::impMakeDiscretionaryInlineObservations(InlineInfo* pInlineInfo, InlineResult* inlineResult)
{
    assert((pInlineInfo != nullptr && compIsForInlining()) || // Perform the actual inlining.
           (pInlineInfo == nullptr && !compIsForInlining())   // Calculate the static inlining hint for ngen.
           );

    // If we're really inlining, we should just have one result in play.
    assert((pInlineInfo == nullptr) || (inlineResult == pInlineInfo->inlineResult));

    // If this is a "forceinline" method, the JIT probably shouldn't have gone
    // to the trouble of estimating the native code size. Even if it did, it
    // shouldn't be relying on the result of this method.
    assert(inlineResult->GetObservation() == InlineObservation::CALLEE_IS_DISCRETIONARY_INLINE);

    // Note if the caller contains NEWOBJ or NEWARR.
    Compiler* rootCompiler = impInlineRoot();

    if ((rootCompiler->optMethodFlags & OMF_HAS_NEWARRAY) != 0)
    {
        inlineResult->Note(InlineObservation::CALLER_HAS_NEWARRAY);
    }

    if ((rootCompiler->optMethodFlags & OMF_HAS_NEWOBJ) != 0)
    {
        inlineResult->Note(InlineObservation::CALLER_HAS_NEWOBJ);
    }

    bool calleeIsStatic  = (info.compFlags & CORINFO_FLG_STATIC) != 0;
    bool isSpecialMethod = (info.compFlags & CORINFO_FLG_CONSTRUCTOR) != 0;

    if (isSpecialMethod)
    {
        if (calleeIsStatic)
        {
            inlineResult->Note(InlineObservation::CALLEE_IS_CLASS_CTOR);
        }
        else
        {
            inlineResult->Note(InlineObservation::CALLEE_IS_INSTANCE_CTOR);
        }
    }
    else if (!calleeIsStatic)
    {
        // Callee is an instance method.
        //
        // Check if the callee has the same 'this' as the root.
        if (pInlineInfo != nullptr)
        {
            GenTree* thisArg = pInlineInfo->iciCall->AsCall()->gtCallThisArg->GetNode();
            assert(thisArg);
            bool isSameThis = impIsThis(thisArg);
            inlineResult->NoteBool(InlineObservation::CALLSITE_IS_SAME_THIS, isSameThis);
        }
    }

    // Note if the callee's class is a promotable struct
    if ((info.compClassAttr & CORINFO_FLG_VALUECLASS) != 0)
    {
        assert(structPromotionHelper != nullptr);
        if (structPromotionHelper->CanPromoteStructType(info.compClassHnd))
        {
            inlineResult->Note(InlineObservation::CALLEE_CLASS_PROMOTABLE);
        }
    }

#ifdef FEATURE_SIMD

    // Note if this method is has SIMD args or return value
    if (pInlineInfo != nullptr && pInlineInfo->hasSIMDTypeArgLocalOrReturn)
    {
        inlineResult->Note(InlineObservation::CALLEE_HAS_SIMD);
    }

#endif // FEATURE_SIMD

    // Roughly classify callsite frequency.
    InlineCallsiteFrequency frequency = InlineCallsiteFrequency::UNUSED;

    // If this is a prejit root, or a maximally hot block...
    if ((pInlineInfo == nullptr) || (pInlineInfo->iciBlock->isMaxBBWeight()))
    {
        frequency = InlineCallsiteFrequency::HOT;
    }
    // No training data.  Look for loop-like things.
    // We consider a recursive call loop-like.  Do not give the inlining boost to the method itself.
    // However, give it to things nearby.
    else if ((pInlineInfo->iciBlock->bbFlags & BBF_BACKWARD_JUMP) &&
             (pInlineInfo->fncHandle != pInlineInfo->inlineCandidateInfo->ilCallerHandle))
    {
        frequency = InlineCallsiteFrequency::LOOP;
    }
    else if (pInlineInfo->iciBlock->hasProfileWeight() && (pInlineInfo->iciBlock->bbWeight > BB_ZERO_WEIGHT))
    {
        frequency = InlineCallsiteFrequency::WARM;
    }
    // Now modify the multiplier based on where we're called from.
    else if (pInlineInfo->iciBlock->isRunRarely() || ((info.compFlags & FLG_CCTOR) == FLG_CCTOR))
    {
        frequency = InlineCallsiteFrequency::RARE;
    }
    else
    {
        frequency = InlineCallsiteFrequency::BORING;
    }

    // Also capture the block weight of the call site.
    //
    // In the prejit root case, assume at runtime there might be a hot call site
    // for this method, so we won't prematurely conclude this method should never
    // be inlined.
    //
    BasicBlock::weight_t weight = 0;

    if (pInlineInfo != nullptr)
    {
        weight = pInlineInfo->iciBlock->bbWeight;
    }
    else
    {
        const float prejitHotCallerWeight = 1000000.0f;
        weight                            = prejitHotCallerWeight;
    }

    inlineResult->NoteInt(InlineObservation::CALLSITE_FREQUENCY, static_cast<int>(frequency));
    inlineResult->NoteInt(InlineObservation::CALLSITE_WEIGHT, (int)(weight));

    // If the call site has profile data, report the relative frequency of the site.
    //
    if ((pInlineInfo != nullptr) && pInlineInfo->iciBlock->hasProfileWeight())
    {
        double callSiteWeight = (double)pInlineInfo->iciBlock->bbWeight;
        double entryWeight    = (double)impInlineRoot()->fgFirstBB->bbWeight;

        assert(callSiteWeight >= 0);
        assert(entryWeight >= 0);

        if (entryWeight != 0)
        {
            inlineResult->NoteBool(InlineObservation::CALLSITE_HAS_PROFILE, true);

            double frequency = callSiteWeight / entryWeight;
            inlineResult->NoteDouble(InlineObservation::CALLSITE_PROFILE_FREQUENCY, frequency);
        }
    }
}

/*****************************************************************************
 This method makes STATIC inlining decision based on the IL code.
 It should not make any inlining decision based on the context.
 If forceInline is true, then the inlining decision should not depend on
 performance heuristics (code size, etc.).
 */

void Compiler::impCanInlineIL(CORINFO_METHOD_HANDLE fncHandle,
                              CORINFO_METHOD_INFO*  methInfo,
                              bool                  forceInline,
                              InlineResult*         inlineResult)
{
    unsigned codeSize = methInfo->ILCodeSize;

    // We shouldn't have made up our minds yet...
    assert(!inlineResult->IsDecided());

    if (methInfo->EHcount)
    {
        inlineResult->NoteFatal(InlineObservation::CALLEE_HAS_EH);
        return;
    }

    if ((methInfo->ILCode == nullptr) || (codeSize == 0))
    {
        inlineResult->NoteFatal(InlineObservation::CALLEE_HAS_NO_BODY);
        return;
    }

    // For now we don't inline varargs (import code can't handle it)

    if (methInfo->args.isVarArg())
    {
        inlineResult->NoteFatal(InlineObservation::CALLEE_HAS_MANAGED_VARARGS);
        return;
    }

    // Reject if it has too many locals.
    // This is currently an implementation limit due to fixed-size arrays in the
    // inline info, rather than a performance heuristic.

    inlineResult->NoteInt(InlineObservation::CALLEE_NUMBER_OF_LOCALS, methInfo->locals.numArgs);

    if (methInfo->locals.numArgs > MAX_INL_LCLS)
    {
        inlineResult->NoteFatal(InlineObservation::CALLEE_TOO_MANY_LOCALS);
        return;
    }

    // Make sure there aren't too many arguments.
    // This is currently an implementation limit due to fixed-size arrays in the
    // inline info, rather than a performance heuristic.

    inlineResult->NoteInt(InlineObservation::CALLEE_NUMBER_OF_ARGUMENTS, methInfo->args.numArgs);

    if (methInfo->args.numArgs > MAX_INL_ARGS)
    {
        inlineResult->NoteFatal(InlineObservation::CALLEE_TOO_MANY_ARGUMENTS);
        return;
    }

    // Note force inline state

    inlineResult->NoteBool(InlineObservation::CALLEE_IS_FORCE_INLINE, forceInline);

    // Note IL code size

    inlineResult->NoteInt(InlineObservation::CALLEE_IL_CODE_SIZE, codeSize);

    if (inlineResult->IsFailure())
    {
        return;
    }

    // Make sure maxstack is not too big

    inlineResult->NoteInt(InlineObservation::CALLEE_MAXSTACK, methInfo->maxStack);

    if (inlineResult->IsFailure())
    {
        return;
    }
}

/*****************************************************************************
 */

void Compiler::impCheckCanInline(GenTreeCall*           call,
                                 CORINFO_METHOD_HANDLE  fncHandle,
                                 unsigned               methAttr,
                                 CORINFO_CONTEXT_HANDLE exactContextHnd,
                                 InlineCandidateInfo**  ppInlineCandidateInfo,
                                 InlineResult*          inlineResult)
{
    // Either EE or JIT might throw exceptions below.
    // If that happens, just don't inline the method.

    struct Param
    {
        Compiler*              pThis;
        GenTreeCall*           call;
        CORINFO_METHOD_HANDLE  fncHandle;
        unsigned               methAttr;
        CORINFO_CONTEXT_HANDLE exactContextHnd;
        InlineResult*          result;
        InlineCandidateInfo**  ppInlineCandidateInfo;
    } param;
    memset(&param, 0, sizeof(param));

    param.pThis                 = this;
    param.call                  = call;
    param.fncHandle             = fncHandle;
    param.methAttr              = methAttr;
    param.exactContextHnd       = (exactContextHnd != nullptr) ? exactContextHnd : MAKE_METHODCONTEXT(fncHandle);
    param.result                = inlineResult;
    param.ppInlineCandidateInfo = ppInlineCandidateInfo;

    bool success = eeRunWithErrorTrap<Param>(
        [](Param* pParam) {
            uint32_t               dwRestrictions = 0;
            CorInfoInitClassResult initClassResult;

#ifdef DEBUG
            const char* methodName;
            const char* className;
            methodName = pParam->pThis->eeGetMethodName(pParam->fncHandle, &className);

            if (JitConfig.JitNoInline())
            {
                pParam->result->NoteFatal(InlineObservation::CALLEE_IS_JIT_NOINLINE);
                goto _exit;
            }
#endif

            /* Try to get the code address/size for the method */

            CORINFO_METHOD_INFO methInfo;
            if (!pParam->pThis->info.compCompHnd->getMethodInfo(pParam->fncHandle, &methInfo))
            {
                pParam->result->NoteFatal(InlineObservation::CALLEE_NO_METHOD_INFO);
                goto _exit;
            }

            bool forceInline;
            forceInline = !!(pParam->methAttr & CORINFO_FLG_FORCEINLINE);

            pParam->pThis->impCanInlineIL(pParam->fncHandle, &methInfo, forceInline, pParam->result);

            if (pParam->result->IsFailure())
            {
                assert(pParam->result->IsNever());
                goto _exit;
            }

            // Speculatively check if initClass() can be done.
            // If it can be done, we will try to inline the method.
            initClassResult =
                pParam->pThis->info.compCompHnd->initClass(nullptr /* field */, pParam->fncHandle /* method */,
                                                           pParam->exactContextHnd /* context */);

            if (initClassResult & CORINFO_INITCLASS_DONT_INLINE)
            {
                pParam->result->NoteFatal(InlineObservation::CALLSITE_CANT_CLASS_INIT);
                goto _exit;
            }

            // Given the EE the final say in whether to inline or not.
            // This should be last since for verifiable code, this can be expensive

            /* VM Inline check also ensures that the method is verifiable if needed */
            CorInfoInline vmResult;
            vmResult = pParam->pThis->info.compCompHnd->canInline(pParam->pThis->info.compMethodHnd, pParam->fncHandle,
                                                                  &dwRestrictions);

            if (vmResult == INLINE_FAIL)
            {
                pParam->result->NoteFatal(InlineObservation::CALLSITE_IS_VM_NOINLINE);
            }
            else if (vmResult == INLINE_NEVER)
            {
                pParam->result->NoteFatal(InlineObservation::CALLEE_IS_VM_NOINLINE);
            }

            if (pParam->result->IsFailure())
            {
                // Make sure not to report this one.  It was already reported by the VM.
                pParam->result->SetReported();
                goto _exit;
            }

            // check for unsupported inlining restrictions
            assert((dwRestrictions & ~(INLINE_RESPECT_BOUNDARY | INLINE_NO_CALLEE_LDSTR | INLINE_SAME_THIS)) == 0);

            if (dwRestrictions & INLINE_SAME_THIS)
            {
                GenTree* thisArg = pParam->call->AsCall()->gtCallThisArg->GetNode();
                assert(thisArg);

                if (!pParam->pThis->impIsThis(thisArg))
                {
                    pParam->result->NoteFatal(InlineObservation::CALLSITE_REQUIRES_SAME_THIS);
                    goto _exit;
                }
            }

            /* Get the method properties */

            CORINFO_CLASS_HANDLE clsHandle;
            clsHandle = pParam->pThis->info.compCompHnd->getMethodClass(pParam->fncHandle);
            unsigned clsAttr;
            clsAttr = pParam->pThis->info.compCompHnd->getClassAttribs(clsHandle);

            /* Get the return type */

            var_types fncRetType;
            fncRetType = pParam->call->TypeGet();

#ifdef DEBUG
            var_types fncRealRetType;
            fncRealRetType = JITtype2varType(methInfo.args.retType);

            assert((genActualType(fncRealRetType) == genActualType(fncRetType)) ||
                   // <BUGNUM> VSW 288602 </BUGNUM>
                   // In case of IJW, we allow to assign a native pointer to a BYREF.
                   (fncRetType == TYP_BYREF && methInfo.args.retType == CORINFO_TYPE_PTR) ||
                   (varTypeIsStruct(fncRetType) && (fncRealRetType == TYP_STRUCT)));
#endif

            // Allocate an InlineCandidateInfo structure,
            //
            // Or, reuse the existing GuardedDevirtualizationCandidateInfo,
            // which was pre-allocated to have extra room.
            //
            InlineCandidateInfo* pInfo;

            if (pParam->call->IsGuardedDevirtualizationCandidate())
            {
                pInfo = pParam->call->gtInlineCandidateInfo;
            }
            else
            {
                pInfo = new (pParam->pThis, CMK_Inlining) InlineCandidateInfo;

                // Null out bits we don't use when we're just inlining
                pInfo->guardedClassHandle              = nullptr;
                pInfo->guardedMethodHandle             = nullptr;
                pInfo->guardedMethodUnboxedEntryHandle = nullptr;
                pInfo->stubAddr                        = nullptr;
                pInfo->likelihood                      = 0;
                pInfo->requiresInstMethodTableArg      = false;
            }

            pInfo->methInfo                       = methInfo;
            pInfo->ilCallerHandle                 = pParam->pThis->info.compMethodHnd;
            pInfo->clsHandle                      = clsHandle;
            pInfo->exactContextHnd                = pParam->exactContextHnd;
            pInfo->retExpr                        = nullptr;
            pInfo->dwRestrictions                 = dwRestrictions;
            pInfo->preexistingSpillTemp           = BAD_VAR_NUM;
            pInfo->clsAttr                        = clsAttr;
            pInfo->methAttr                       = pParam->methAttr;
            pInfo->initClassResult                = initClassResult;
            pInfo->fncRetType                     = fncRetType;
            pInfo->exactContextNeedsRuntimeLookup = false;

            // Note exactContextNeedsRuntimeLookup is reset later on,
            // over in impMarkInlineCandidate.

            *(pParam->ppInlineCandidateInfo) = pInfo;

        _exit:;
        },
        &param);
    if (!success)
    {
        param.result->NoteFatal(InlineObservation::CALLSITE_COMPILATION_ERROR);
    }
}

//------------------------------------------------------------------------
// impInlineRecordArgInfo: record information about an inline candidate argument
//
// Arguments:
//   pInlineInfo - inline info for the inline candidate
//   curArgVal - tree for the caller actual argument value
//   argNum - logical index of this argument
//   inlineResult - result of ongoing inline evaluation
//
// Notes:
//
//   Checks for various inline blocking conditions and makes notes in
//   the inline info arg table about the properties of the actual. These
//   properties are used later by impInlineFetchArg to determine how best to
//   pass the argument into the inlinee.

void Compiler::impInlineRecordArgInfo(InlineInfo*   pInlineInfo,
                                      GenTree*      curArgVal,
                                      unsigned      argNum,
                                      InlineResult* inlineResult)
{
    InlArgInfo* inlCurArgInfo = &pInlineInfo->inlArgInfo[argNum];

    inlCurArgInfo->argNode = curArgVal; // Save the original tree, with PUT_ARG and RET_EXPR.

    curArgVal = curArgVal->gtSkipPutArgType();
    curArgVal = curArgVal->gtRetExprVal();

    if (curArgVal->gtOper == GT_MKREFANY)
    {
        inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_IS_MKREFANY);
        return;
    }

    GenTree* lclVarTree;

    const bool isAddressInLocal = impIsAddressInLocal(curArgVal, &lclVarTree);
    if (isAddressInLocal && varTypeIsStruct(lclVarTree))
    {
        inlCurArgInfo->argIsByRefToStructLocal = true;
#ifdef FEATURE_SIMD
        if (lvaTable[lclVarTree->AsLclVarCommon()->GetLclNum()].lvSIMDType)
        {
            pInlineInfo->hasSIMDTypeArgLocalOrReturn = true;
        }
#endif // FEATURE_SIMD
    }

    if (curArgVal->gtFlags & GTF_ALL_EFFECT)
    {
        inlCurArgInfo->argHasGlobRef = (curArgVal->gtFlags & GTF_GLOB_REF) != 0;
        inlCurArgInfo->argHasSideEff = (curArgVal->gtFlags & (GTF_ALL_EFFECT & ~GTF_GLOB_REF)) != 0;
    }

    if (curArgVal->gtOper == GT_LCL_VAR)
    {
        inlCurArgInfo->argIsLclVar = true;

        /* Remember the "original" argument number */
        INDEBUG(curArgVal->AsLclVar()->gtLclILoffs = argNum;)
    }

    if (curArgVal->IsInvariant())
    {
        inlCurArgInfo->argIsInvariant = true;
        if (inlCurArgInfo->argIsThis && (curArgVal->gtOper == GT_CNS_INT) && (curArgVal->AsIntCon()->gtIconVal == 0))
        {
            // Abort inlining at this call site
            inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_HAS_NULL_THIS);
            return;
        }
    }

    // If the arg is a local that is address-taken, we can't safely
    // directly substitute it into the inlinee.
    //
    // Previously we'd accomplish this by setting "argHasLdargaOp" but
    // that has a stronger meaning: that the arg value can change in
    // the method body. Using that flag prevents type propagation,
    // which is safe in this case.
    //
    // Instead mark the arg as having a caller local ref.
    if (!inlCurArgInfo->argIsInvariant && gtHasLocalsWithAddrOp(curArgVal))
    {
        inlCurArgInfo->argHasCallerLocalRef = true;
    }

#ifdef DEBUG
    if (verbose)
    {
        if (inlCurArgInfo->argIsThis)
        {
            printf("thisArg:");
        }
        else
        {
            printf("\nArgument #%u:", argNum);
        }
        if (inlCurArgInfo->argIsLclVar)
        {
            printf(" is a local var");
        }
        if (inlCurArgInfo->argIsInvariant)
        {
            printf(" is a constant");
        }
        if (inlCurArgInfo->argHasGlobRef)
        {
            printf(" has global refs");
        }
        if (inlCurArgInfo->argHasCallerLocalRef)
        {
            printf(" has caller local ref");
        }
        if (inlCurArgInfo->argHasSideEff)
        {
            printf(" has side effects");
        }
        if (inlCurArgInfo->argHasLdargaOp)
        {
            printf(" has ldarga effect");
        }
        if (inlCurArgInfo->argHasStargOp)
        {
            printf(" has starg effect");
        }
        if (inlCurArgInfo->argIsByRefToStructLocal)
        {
            printf(" is byref to a struct local");
        }

        printf("\n");
        gtDispTree(curArgVal);
        printf("\n");
    }
#endif
}

//------------------------------------------------------------------------
// impInlineInitVars: setup inline information for inlinee args and locals
//
// Arguments:
//    pInlineInfo - inline info for the inline candidate
//
// Notes:
//    This method primarily adds caller-supplied info to the inlArgInfo
//    and sets up the lclVarInfo table.
//
//    For args, the inlArgInfo records properties of the actual argument
//    including the tree node that produces the arg value. This node is
//    usually the tree node present at the call, but may also differ in
//    various ways:
//    - when the call arg is a GT_RET_EXPR, we search back through the ret
//      expr chain for the actual node. Note this will either be the original
//      call (which will be a failed inline by this point), or the return
//      expression from some set of inlines.
//    - when argument type casting is needed the necessary casts are added
//      around the argument node.
//    - if an argument can be simplified by folding then the node here is the
//      folded value.
//
//   The method may make observations that lead to marking this candidate as
//   a failed inline. If this happens the initialization is abandoned immediately
//   to try and reduce the jit time cost for a failed inline.

void Compiler::impInlineInitVars(InlineInfo* pInlineInfo)
{
    assert(!compIsForInlining());

    GenTreeCall*         call         = pInlineInfo->iciCall;
    CORINFO_METHOD_INFO* methInfo     = &pInlineInfo->inlineCandidateInfo->methInfo;
    unsigned             clsAttr      = pInlineInfo->inlineCandidateInfo->clsAttr;
    InlArgInfo*          inlArgInfo   = pInlineInfo->inlArgInfo;
    InlLclVarInfo*       lclVarInfo   = pInlineInfo->lclVarInfo;
    InlineResult*        inlineResult = pInlineInfo->inlineResult;

    // Inlined methods always use the managed calling convention
    const bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(methInfo, CorInfoCallConvExtension::Managed);

    /* init the argument stuct */

    memset(inlArgInfo, 0, (MAX_INL_ARGS + 1) * sizeof(inlArgInfo[0]));

    GenTreeCall::Use* thisArg = call->gtCallThisArg;
    unsigned          argCnt  = 0; // Count of the arguments

    assert((methInfo->args.hasThis()) == (thisArg != nullptr));

    if (thisArg != nullptr)
    {
        inlArgInfo[0].argIsThis = true;
        impInlineRecordArgInfo(pInlineInfo, thisArg->GetNode(), argCnt, inlineResult);

        if (inlineResult->IsFailure())
        {
            return;
        }

        /* Increment the argument count */
        argCnt++;
    }

    /* Record some information about each of the arguments */
    bool hasTypeCtxtArg = (methInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE) != 0;

#if USER_ARGS_COME_LAST
    unsigned typeCtxtArg = (thisArg != nullptr) ? 1 : 0;
#else  // USER_ARGS_COME_LAST
    unsigned typeCtxtArg = methInfo->args.totalILArgs();
#endif // USER_ARGS_COME_LAST

    for (GenTreeCall::Use& use : call->Args())
    {
        if (hasRetBuffArg && (&use == call->gtCallArgs))
        {
            continue;
        }

        // Ignore the type context argument
        if (hasTypeCtxtArg && (argCnt == typeCtxtArg))
        {
            pInlineInfo->typeContextArg = typeCtxtArg;
            typeCtxtArg                 = 0xFFFFFFFF;
            continue;
        }

        GenTree* actualArg = use.GetNode();
        impInlineRecordArgInfo(pInlineInfo, actualArg, argCnt, inlineResult);

        if (inlineResult->IsFailure())
        {
            return;
        }

        /* Increment the argument count */
        argCnt++;
    }

    /* Make sure we got the arg number right */
    assert(argCnt == methInfo->args.totalILArgs());

#ifdef FEATURE_SIMD
    bool foundSIMDType = pInlineInfo->hasSIMDTypeArgLocalOrReturn;
#endif // FEATURE_SIMD

    /* We have typeless opcodes, get type information from the signature */

    if (thisArg != nullptr)
    {
        lclVarInfo[0].lclVerTypeInfo = verMakeTypeInfo(pInlineInfo->inlineCandidateInfo->clsHandle);
        lclVarInfo[0].lclHasLdlocaOp = false;

#ifdef FEATURE_SIMD
        // We always want to check isSIMDClass, since we want to set foundSIMDType (to increase
        // the inlining multiplier) for anything in that assembly.
        // But we only need to normalize it if it is a TYP_STRUCT
        // (which we need to do even if we have already set foundSIMDType).
        if (!foundSIMDType && isSIMDorHWSIMDClass(&(lclVarInfo[0].lclVerTypeInfo)))
        {
            foundSIMDType = true;
        }
#endif // FEATURE_SIMD

        var_types sigType         = ((clsAttr & CORINFO_FLG_VALUECLASS) != 0) ? TYP_BYREF : TYP_REF;
        lclVarInfo[0].lclTypeInfo = sigType;

        GenTree* thisArgNode = thisArg->GetNode();

        assert(varTypeIsGC(thisArgNode->TypeGet()) ||     // "this" is managed
               ((thisArgNode->TypeGet() == TYP_I_IMPL) && // "this" is unmgd but the method's class doesnt care
                (clsAttr & CORINFO_FLG_VALUECLASS)));

        if (genActualType(thisArgNode->TypeGet()) != genActualType(sigType))
        {
            if (sigType == TYP_REF)
            {
                /* The argument cannot be bashed into a ref (see bug 750871) */
                inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_NO_BASH_TO_REF);
                return;
            }

            /* This can only happen with byrefs <-> ints/shorts */

            assert(sigType == TYP_BYREF);
            assert((genActualType(thisArgNode->TypeGet()) == TYP_I_IMPL) || (thisArgNode->TypeGet() == TYP_BYREF));

            lclVarInfo[0].lclVerTypeInfo = typeInfo(varType2tiType(TYP_I_IMPL));
        }
    }

    /* Init the types of the arguments and make sure the types
     * from the trees match the types in the signature */

    CORINFO_ARG_LIST_HANDLE argLst;
    argLst = methInfo->args.args;

    unsigned i;
    for (i = (thisArg ? 1 : 0); i < argCnt; i++, argLst = info.compCompHnd->getArgNext(argLst))
    {
        var_types sigType = (var_types)eeGetArgType(argLst, &methInfo->args);

        lclVarInfo[i].lclVerTypeInfo = verParseArgSigToTypeInfo(&methInfo->args, argLst);

#ifdef FEATURE_SIMD
        if ((!foundSIMDType || (sigType == TYP_STRUCT)) && isSIMDorHWSIMDClass(&(lclVarInfo[i].lclVerTypeInfo)))
        {
            // If this is a SIMD class (i.e. in the SIMD assembly), then we will consider that we've
            // found a SIMD type, even if this may not be a type we recognize (the assumption is that
            // it is likely to use a SIMD type, and therefore we want to increase the inlining multiplier).
            foundSIMDType = true;
            if (sigType == TYP_STRUCT)
            {
                var_types structType = impNormStructType(lclVarInfo[i].lclVerTypeInfo.GetClassHandle());
                sigType              = structType;
            }
        }
#endif // FEATURE_SIMD

        lclVarInfo[i].lclTypeInfo    = sigType;
        lclVarInfo[i].lclHasLdlocaOp = false;

        /* Does the tree type match the signature type? */

        GenTree* inlArgNode = inlArgInfo[i].argNode;

        if ((sigType != inlArgNode->gtType) || inlArgNode->OperIs(GT_PUTARG_TYPE))
        {
            assert(impCheckImplicitArgumentCoercion(sigType, inlArgNode->gtType));
            assert(!varTypeIsStruct(inlArgNode->gtType) && !varTypeIsStruct(sigType) &&
                   genTypeSize(inlArgNode->gtType) == genTypeSize(sigType));

            /* In valid IL, this can only happen for short integer types or byrefs <-> [native] ints,
               but in bad IL cases with caller-callee signature mismatches we can see other types.
               Intentionally reject cases with mismatches so the jit is more flexible when
               encountering bad IL. */

            bool isPlausibleTypeMatch = (genActualType(sigType) == genActualType(inlArgNode->gtType)) ||
                                        (genActualTypeIsIntOrI(sigType) && inlArgNode->gtType == TYP_BYREF) ||
                                        (sigType == TYP_BYREF && genActualTypeIsIntOrI(inlArgNode->gtType));

            if (!isPlausibleTypeMatch)
            {
                inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_TYPES_INCOMPATIBLE);
                return;
            }

            GenTree** pInlArgNode;
            if (inlArgNode->OperIs(GT_PUTARG_TYPE))
            {
                // There was a widening or narrowing cast.
                GenTreeUnOp* putArgType = inlArgNode->AsUnOp();
                pInlArgNode             = &putArgType->gtOp1;
                inlArgNode              = putArgType->gtOp1;
            }
            else
            {
                // The same size but different type of the arguments.
                pInlArgNode = &inlArgInfo[i].argNode;
            }

            /* Is it a narrowing or widening cast?
             * Widening casts are ok since the value computed is already
             * normalized to an int (on the IL stack) */
            if (genTypeSize(inlArgNode->gtType) >= genTypeSize(sigType))
            {
                if (sigType == TYP_BYREF)
                {
                    lclVarInfo[i].lclVerTypeInfo = typeInfo(varType2tiType(TYP_I_IMPL));
                }
                else if (inlArgNode->gtType == TYP_BYREF)
                {
                    assert(varTypeIsIntOrI(sigType));

                    /* If possible bash the BYREF to an int */
                    if (inlArgNode->IsLocalAddrExpr() != nullptr)
                    {
                        inlArgNode->gtType           = TYP_I_IMPL;
                        lclVarInfo[i].lclVerTypeInfo = typeInfo(varType2tiType(TYP_I_IMPL));
                    }
                    else
                    {
                        /* Arguments 'int <- byref' cannot be changed */
                        inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_NO_BASH_TO_INT);
                        return;
                    }
                }
                else if (genTypeSize(sigType) < TARGET_POINTER_SIZE)
                {
                    // Narrowing cast.
                    if (inlArgNode->OperIs(GT_LCL_VAR))
                    {
                        const unsigned lclNum = inlArgNode->AsLclVarCommon()->GetLclNum();
                        if (!lvaTable[lclNum].lvNormalizeOnLoad() && sigType == lvaGetRealType(lclNum))
                        {
                            // We don't need to insert a cast here as the variable
                            // was assigned a normalized value of the right type.
                            continue;
                        }
                    }

                    inlArgNode = gtNewCastNode(TYP_INT, inlArgNode, false, sigType);

                    inlArgInfo[i].argIsLclVar = false;
                    // Try to fold the node in case we have constant arguments.
                    if (inlArgInfo[i].argIsInvariant)
                    {
                        inlArgNode = gtFoldExprConst(inlArgNode);
                        assert(inlArgNode->OperIsConst());
                    }
                    *pInlArgNode = inlArgNode;
                }
#ifdef TARGET_64BIT
                else if (genTypeSize(genActualType(inlArgNode->gtType)) < genTypeSize(sigType))
                {
                    // This should only happen for int -> native int widening
                    inlArgNode = gtNewCastNode(genActualType(sigType), inlArgNode, false, sigType);

                    inlArgInfo[i].argIsLclVar = false;

                    /* Try to fold the node in case we have constant arguments */

                    if (inlArgInfo[i].argIsInvariant)
                    {
                        inlArgNode = gtFoldExprConst(inlArgNode);
                        assert(inlArgNode->OperIsConst());
                    }
                    *pInlArgNode = inlArgNode;
                }
#endif // TARGET_64BIT
            }
        }
    }

    /* Init the types of the local variables */

    CORINFO_ARG_LIST_HANDLE localsSig;
    localsSig = methInfo->locals.args;

    for (i = 0; i < methInfo->locals.numArgs; i++)
    {
        bool      isPinned;
        var_types type = (var_types)eeGetArgType(localsSig, &methInfo->locals, &isPinned);

        lclVarInfo[i + argCnt].lclHasLdlocaOp = false;
        lclVarInfo[i + argCnt].lclTypeInfo    = type;

        if (varTypeIsGC(type))
        {
            if (isPinned)
            {
                JITDUMP("Inlinee local #%02u is pinned\n", i);
                lclVarInfo[i + argCnt].lclIsPinned = true;

                // Pinned locals may cause inlines to fail.
                inlineResult->Note(InlineObservation::CALLEE_HAS_PINNED_LOCALS);
                if (inlineResult->IsFailure())
                {
                    return;
                }
            }

            pInlineInfo->numberOfGcRefLocals++;
        }
        else if (isPinned)
        {
            JITDUMP("Ignoring pin on inlinee local #%02u -- not a GC type\n", i);
        }

        lclVarInfo[i + argCnt].lclVerTypeInfo = verParseArgSigToTypeInfo(&methInfo->locals, localsSig);

        // If this local is a struct type with GC fields, inform the inliner. It may choose to bail
        // out on the inline.
        if (type == TYP_STRUCT)
        {
            CORINFO_CLASS_HANDLE lclHandle = lclVarInfo[i + argCnt].lclVerTypeInfo.GetClassHandle();
            DWORD                typeFlags = info.compCompHnd->getClassAttribs(lclHandle);
            if ((typeFlags & CORINFO_FLG_CONTAINS_GC_PTR) != 0)
            {
                inlineResult->Note(InlineObservation::CALLEE_HAS_GC_STRUCT);
                if (inlineResult->IsFailure())
                {
                    return;
                }

                // Do further notification in the case where the call site is rare; some policies do
                // not track the relative hotness of call sites for "always" inline cases.
                if (pInlineInfo->iciBlock->isRunRarely())
                {
                    inlineResult->Note(InlineObservation::CALLSITE_RARE_GC_STRUCT);
                    if (inlineResult->IsFailure())
                    {

                        return;
                    }
                }
            }
        }

        localsSig = info.compCompHnd->getArgNext(localsSig);

#ifdef FEATURE_SIMD
        if ((!foundSIMDType || (type == TYP_STRUCT)) && isSIMDorHWSIMDClass(&(lclVarInfo[i + argCnt].lclVerTypeInfo)))
        {
            foundSIMDType = true;
            if (supportSIMDTypes() && type == TYP_STRUCT)
            {
                var_types structType = impNormStructType(lclVarInfo[i + argCnt].lclVerTypeInfo.GetClassHandle());
                lclVarInfo[i + argCnt].lclTypeInfo = structType;
            }
        }
#endif // FEATURE_SIMD
    }

#ifdef FEATURE_SIMD
    if (!foundSIMDType && (call->AsCall()->gtRetClsHnd != nullptr) && isSIMDorHWSIMDClass(call->AsCall()->gtRetClsHnd))
    {
        foundSIMDType = true;
    }
    pInlineInfo->hasSIMDTypeArgLocalOrReturn = foundSIMDType;
#endif // FEATURE_SIMD
}

//------------------------------------------------------------------------
// impInlineFetchLocal: get a local var that represents an inlinee local
//
// Arguments:
//    lclNum -- number of the inlinee local
//    reason -- debug string describing purpose of the local var
//
// Returns:
//    Number of the local to use
//
// Notes:
//    This method is invoked only for locals actually used in the
//    inlinee body.
//
//    Allocates a new temp if necessary, and copies key properties
//    over from the inlinee local var info.

unsigned Compiler::impInlineFetchLocal(unsigned lclNum DEBUGARG(const char* reason))
{
    assert(compIsForInlining());

    unsigned tmpNum = impInlineInfo->lclTmpNum[lclNum];

    if (tmpNum == BAD_VAR_NUM)
    {
        const InlLclVarInfo& inlineeLocal = impInlineInfo->lclVarInfo[lclNum + impInlineInfo->argCnt];
        const var_types      lclTyp       = inlineeLocal.lclTypeInfo;

        // The lifetime of this local might span multiple BBs.
        // So it is a long lifetime local.
        impInlineInfo->lclTmpNum[lclNum] = tmpNum = lvaGrabTemp(false DEBUGARG(reason));

        // Copy over key info
        lvaTable[tmpNum].lvType                 = lclTyp;
        lvaTable[tmpNum].lvHasLdAddrOp          = inlineeLocal.lclHasLdlocaOp;
        lvaTable[tmpNum].lvPinned               = inlineeLocal.lclIsPinned;
        lvaTable[tmpNum].lvHasILStoreOp         = inlineeLocal.lclHasStlocOp;
        lvaTable[tmpNum].lvHasMultipleILStoreOp = inlineeLocal.lclHasMultipleStlocOp;

        // Copy over class handle for ref types. Note this may be a
        // shared type -- someday perhaps we can get the exact
        // signature and pass in a more precise type.
        if (lclTyp == TYP_REF)
        {
            assert(lvaTable[tmpNum].lvSingleDef == 0);

            lvaTable[tmpNum].lvSingleDef = !inlineeLocal.lclHasMultipleStlocOp && !inlineeLocal.lclHasLdlocaOp;
            if (lvaTable[tmpNum].lvSingleDef)
            {
                JITDUMP("Marked V%02u as a single def temp\n", tmpNum);
            }

            lvaSetClass(tmpNum, inlineeLocal.lclVerTypeInfo.GetClassHandleForObjRef());
        }

        if (inlineeLocal.lclVerTypeInfo.IsStruct())
        {
            if (varTypeIsStruct(lclTyp))
            {
                lvaSetStruct(tmpNum, inlineeLocal.lclVerTypeInfo.GetClassHandle(), true /* unsafe value cls check */);
            }
            else
            {
                // This is a wrapped primitive.  Make sure the verstate knows that
                lvaTable[tmpNum].lvVerTypeInfo = inlineeLocal.lclVerTypeInfo;
            }
        }

#ifdef DEBUG
        // Sanity check that we're properly prepared for gc ref locals.
        if (varTypeIsGC(lclTyp))
        {
            // Since there are gc locals we should have seen them earlier
            // and if there was a return value, set up the spill temp.
            assert(impInlineInfo->HasGcRefLocals());
            assert((info.compRetNativeType == TYP_VOID) || fgNeedReturnSpillTemp());
        }
        else
        {
            // Make sure all pinned locals count as gc refs.
            assert(!inlineeLocal.lclIsPinned);
        }
#endif // DEBUG
    }

    return tmpNum;
}

//------------------------------------------------------------------------
// impInlineFetchArg: return tree node for argument value in an inlinee
//
// Arguments:
//    lclNum -- argument number in inlinee IL
//    inlArgInfo -- argument info for inlinee
//    lclVarInfo -- var info for inlinee
//
// Returns:
//    Tree for the argument's value. Often an inlinee-scoped temp
//    GT_LCL_VAR but can be other tree kinds, if the argument
//    expression from the caller can be directly substituted into the
//    inlinee body.
//
// Notes:
//    Must be used only for arguments -- use impInlineFetchLocal for
//    inlinee locals.
//
//    Direct substitution is performed when the formal argument cannot
//    change value in the inlinee body (no starg or ldarga), and the
//    actual argument expression's value cannot be changed if it is
//    substituted it into the inlinee body.
//
//    Even if an inlinee-scoped temp is returned here, it may later be
//    "bashed" to a caller-supplied tree when arguments are actually
//    passed (see fgInlinePrependStatements). Bashing can happen if
//    the argument ends up being single use and other conditions are
//    met. So the contents of the tree returned here may not end up
//    being the ones ultimately used for the argument.
//
//    This method will side effect inlArgInfo. It should only be called
//    for actual uses of the argument in the inlinee.

GenTree* Compiler::impInlineFetchArg(unsigned lclNum, InlArgInfo* inlArgInfo, InlLclVarInfo* lclVarInfo)
{
    // Cache the relevant arg and lcl info for this argument.
    // We will modify argInfo but not lclVarInfo.
    InlArgInfo&          argInfo          = inlArgInfo[lclNum];
    const InlLclVarInfo& lclInfo          = lclVarInfo[lclNum];
    const bool           argCanBeModified = argInfo.argHasLdargaOp || argInfo.argHasStargOp;
    const var_types      lclTyp           = lclInfo.lclTypeInfo;
    GenTree*             op1              = nullptr;

    GenTree* argNode = argInfo.argNode->gtSkipPutArgType()->gtRetExprVal();

    if (argInfo.argIsInvariant && !argCanBeModified)
    {
        // Directly substitute constants or addresses of locals
        //
        // Clone the constant. Note that we cannot directly use
        // argNode in the trees even if !argInfo.argIsUsed as this
        // would introduce aliasing between inlArgInfo[].argNode and
        // impInlineExpr. Then gtFoldExpr() could change it, causing
        // further references to the argument working off of the
        // bashed copy.
        op1 = gtCloneExpr(argNode);
        PREFIX_ASSUME(op1 != nullptr);
        argInfo.argTmpNum = BAD_VAR_NUM;

        // We may need to retype to ensure we match the callee's view of the type.
        // Otherwise callee-pass throughs of arguments can create return type
        // mismatches that block inlining.
        //
        // Note argument type mismatches that prevent inlining should
        // have been caught in impInlineInitVars.
        if (op1->TypeGet() != lclTyp)
        {
            op1->gtType = genActualType(lclTyp);
        }
    }
    else if (argInfo.argIsLclVar && !argCanBeModified && !argInfo.argHasCallerLocalRef)
    {
        // Directly substitute unaliased caller locals for args that cannot be modified
        //
        // Use the caller-supplied node if this is the first use.
        op1               = argNode;
        argInfo.argTmpNum = op1->AsLclVarCommon()->GetLclNum();

        // Use an equivalent copy if this is the second or subsequent
        // use, or if we need to retype.
        //
        // Note argument type mismatches that prevent inlining should
        // have been caught in impInlineInitVars.
        if (argInfo.argIsUsed || (op1->TypeGet() != lclTyp))
        {
            assert(op1->gtOper == GT_LCL_VAR);
            assert(lclNum == op1->AsLclVar()->gtLclILoffs);

            var_types newTyp = lclTyp;

            if (!lvaTable[op1->AsLclVarCommon()->GetLclNum()].lvNormalizeOnLoad())
            {
                newTyp = genActualType(lclTyp);
            }

            // Create a new lcl var node - remember the argument lclNum
            op1 = gtNewLclvNode(op1->AsLclVarCommon()->GetLclNum(), newTyp DEBUGARG(op1->AsLclVar()->gtLclILoffs));
        }
    }
    else if (argInfo.argIsByRefToStructLocal && !argInfo.argHasStargOp)
    {
        /* Argument is a by-ref address to a struct, a normed struct, or its field.
           In these cases, don't spill the byref to a local, simply clone the tree and use it.
           This way we will increase the chance for this byref to be optimized away by
           a subsequent "dereference" operation.

           From Dev11 bug #139955: Argument node can also be TYP_I_IMPL if we've bashed the tree
           (in impInlineInitVars()), if the arg has argHasLdargaOp as well as argIsByRefToStructLocal.
           For example, if the caller is:
                ldloca.s   V_1  // V_1 is a local struct
                call       void Test.ILPart::RunLdargaOnPointerArg(int32*)
           and the callee being inlined has:
                .method public static void  RunLdargaOnPointerArg(int32* ptrToInts) cil managed
                    ldarga.s   ptrToInts
                    call       void Test.FourInts::NotInlined_SetExpectedValuesThroughPointerToPointer(int32**)
           then we change the argument tree (of "ldloca.s V_1") to TYP_I_IMPL to match the callee signature. We'll
           soon afterwards reject the inlining anyway, since the tree we return isn't a GT_LCL_VAR.
        */
        assert(argNode->TypeGet() == TYP_BYREF || argNode->TypeGet() == TYP_I_IMPL);
        op1 = gtCloneExpr(argNode);
    }
    else
    {
        /* Argument is a complex expression - it must be evaluated into a temp */

        if (argInfo.argHasTmp)
        {
            assert(argInfo.argIsUsed);
            assert(argInfo.argTmpNum < lvaCount);

            /* Create a new lcl var node - remember the argument lclNum */
            op1 = gtNewLclvNode(argInfo.argTmpNum, genActualType(lclTyp));

            /* This is the second or later use of the this argument,
            so we have to use the temp (instead of the actual arg) */
            argInfo.argBashTmpNode = nullptr;
        }
        else
        {
            /* First time use */
            assert(!argInfo.argIsUsed);

            /* Reserve a temp for the expression.
            * Use a large size node as we may change it later */

            const unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Inlining Arg"));

            lvaTable[tmpNum].lvType = lclTyp;

            // For ref types, determine the type of the temp.
            if (lclTyp == TYP_REF)
            {
                if (!argCanBeModified)
                {
                    // If the arg can't be modified in the method
                    // body, use the type of the value, if
                    // known. Otherwise, use the declared type.
                    assert(lvaTable[tmpNum].lvSingleDef == 0);
                    lvaTable[tmpNum].lvSingleDef = 1;
                    JITDUMP("Marked V%02u as a single def temp\n", tmpNum);
                    lvaSetClass(tmpNum, argNode, lclInfo.lclVerTypeInfo.GetClassHandleForObjRef());
                }
                else
                {
                    // Arg might be modified, use the declared type of
                    // the argument.
                    lvaSetClass(tmpNum, lclInfo.lclVerTypeInfo.GetClassHandleForObjRef());
                }
            }

            assert(lvaTable[tmpNum].lvAddrExposed == 0);
            if (argInfo.argHasLdargaOp)
            {
                lvaTable[tmpNum].lvHasLdAddrOp = 1;
            }

            if (lclInfo.lclVerTypeInfo.IsStruct())
            {
                if (varTypeIsStruct(lclTyp))
                {
                    lvaSetStruct(tmpNum, lclInfo.lclVerTypeInfo.GetClassHandle(), true /* unsafe value cls check */);
                    if (info.compIsVarArgs)
                    {
                        lvaSetStructUsedAsVarArg(tmpNum);
                    }
                }
                else
                {
                    // This is a wrapped primitive.  Make sure the verstate knows that
                    lvaTable[tmpNum].lvVerTypeInfo = lclInfo.lclVerTypeInfo;
                }
            }

            argInfo.argHasTmp = true;
            argInfo.argTmpNum = tmpNum;

            // If we require strict exception order, then arguments must
            // be evaluated in sequence before the body of the inlined method.
            // So we need to evaluate them to a temp.
            // Also, if arguments have global or local references, we need to
            // evaluate them to a temp before the inlined body as the
            // inlined body may be modifying the global ref.
            // TODO-1stClassStructs: We currently do not reuse an existing lclVar
            // if it is a struct, because it requires some additional handling.

            if (!varTypeIsStruct(lclTyp) && !argInfo.argHasSideEff && !argInfo.argHasGlobRef &&
                !argInfo.argHasCallerLocalRef)
            {
                /* Get a *LARGE* LCL_VAR node */
                op1 = gtNewLclLNode(tmpNum, genActualType(lclTyp) DEBUGARG(lclNum));

                /* Record op1 as the very first use of this argument.
                If there are no further uses of the arg, we may be
                able to use the actual arg node instead of the temp.
                If we do see any further uses, we will clear this. */
                argInfo.argBashTmpNode = op1;
            }
            else
            {
                /* Get a small LCL_VAR node */
                op1 = gtNewLclvNode(tmpNum, genActualType(lclTyp));
                /* No bashing of this argument */
                argInfo.argBashTmpNode = nullptr;
            }
        }
    }

    // Mark this argument as used.
    argInfo.argIsUsed = true;

    return op1;
}

/******************************************************************************
 Is this the original "this" argument to the call being inlined?

 Note that we do not inline methods with "starg 0", and so we do not need to
 worry about it.
*/

bool Compiler::impInlineIsThis(GenTree* tree, InlArgInfo* inlArgInfo)
{
    assert(compIsForInlining());
    return (tree->gtOper == GT_LCL_VAR && tree->AsLclVarCommon()->GetLclNum() == inlArgInfo[0].argTmpNum);
}

//-----------------------------------------------------------------------------
// impInlineIsGuaranteedThisDerefBeforeAnySideEffects: Check if a dereference in
// the inlinee can guarantee that the "this" pointer is non-NULL.
//
// Arguments:
//    additionalTree - a tree to check for side effects
//    additionalCallArgs - a list of call args to check for side effects
//    dereferencedAddress - address expression being dereferenced
//    inlArgInfo - inlinee argument information
//
// Notes:
//    If we haven't hit a branch or a side effect, and we are dereferencing
//    from 'this' to access a field or make GTF_CALL_NULLCHECK call,
//    then we can avoid a separate null pointer check.
//
//    The importer stack and current statement list are searched for side effects.
//    Trees that have been popped of the stack but haven't been appended to the
//    statement list and have to be checked for side effects may be provided via
//    additionalTree and additionalCallArgs.
//
bool Compiler::impInlineIsGuaranteedThisDerefBeforeAnySideEffects(GenTree*          additionalTree,
                                                                  GenTreeCall::Use* additionalCallArgs,
                                                                  GenTree*          dereferencedAddress,
                                                                  InlArgInfo*       inlArgInfo)
{
    assert(compIsForInlining());
    assert(opts.OptEnabled(CLFLG_INLINING));

    BasicBlock* block = compCurBB;

    if (block != fgFirstBB)
    {
        return false;
    }

    if (!impInlineIsThis(dereferencedAddress, inlArgInfo))
    {
        return false;
    }

    if ((additionalTree != nullptr) && GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(additionalTree->gtFlags))
    {
        return false;
    }

    for (GenTreeCall::Use& use : GenTreeCall::UseList(additionalCallArgs))
    {
        if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(use.GetNode()->gtFlags))
        {
            return false;
        }
    }

    for (Statement* stmt : StatementList(impStmtList))
    {
        GenTree* expr = stmt->GetRootNode();
        if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(expr->gtFlags))
        {
            return false;
        }
    }

    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        unsigned stackTreeFlags = verCurrentState.esStack[level].val->gtFlags;
        if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(stackTreeFlags))
        {
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// impMarkInlineCandidate: determine if this call can be subsequently inlined
//
// Arguments:
//    callNode -- call under scrutiny
//    exactContextHnd -- context handle for inlining
//    exactContextNeedsRuntimeLookup -- true if context required runtime lookup
//    callInfo -- call info from VM
//
// Notes:
//    Mostly a wrapper for impMarkInlineCandidateHelper that also undoes
//    guarded devirtualization for virtual calls where the method we'd
//    devirtualize to cannot be inlined.

void Compiler::impMarkInlineCandidate(GenTree*               callNode,
                                      CORINFO_CONTEXT_HANDLE exactContextHnd,
                                      bool                   exactContextNeedsRuntimeLookup,
                                      CORINFO_CALL_INFO*     callInfo)
{
    GenTreeCall* call = callNode->AsCall();

    // Do the actual evaluation
    impMarkInlineCandidateHelper(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo);

    // If this call is an inline candidate or is not a guarded devirtualization
    // candidate, we're done.
    if (call->IsInlineCandidate() || !call->IsGuardedDevirtualizationCandidate())
    {
        return;
    }

    // If we can't inline the call we'd guardedly devirtualize to,
    // we undo the guarded devirtualization, as the benefit from
    // just guarded devirtualization alone is likely not worth the
    // extra jit time and code size.
    //
    // TODO: it is possibly interesting to allow this, but requires
    // fixes elsewhere too...
    JITDUMP("Revoking guarded devirtualization candidacy for call [%06u]: target method can't be inlined\n",
            dspTreeID(call));

    call->ClearGuardedDevirtualizationCandidate();

    // If we have a stub address, restore it back into the union that it shares
    // with the candidate info.
    if (call->IsVirtualStub())
    {
        JITDUMP("Restoring stub addr %p from guarded devirt candidate info\n",
                dspPtr(call->gtGuardedDevirtualizationCandidateInfo->stubAddr));
        call->gtStubCallStubAddr = call->gtGuardedDevirtualizationCandidateInfo->stubAddr;
    }
}

//------------------------------------------------------------------------
// impMarkInlineCandidateHelper: determine if this call can be subsequently
//     inlined
//
// Arguments:
//    callNode -- call under scrutiny
//    exactContextHnd -- context handle for inlining
//    exactContextNeedsRuntimeLookup -- true if context required runtime lookup
//    callInfo -- call info from VM
//
// Notes:
//    If callNode is an inline candidate, this method sets the flag
//    GTF_CALL_INLINE_CANDIDATE, and ensures that helper methods have
//    filled in the associated InlineCandidateInfo.
//
//    If callNode is not an inline candidate, and the reason is
//    something that is inherent to the method being called, the
//    method may be marked as "noinline" to short-circuit any
//    future assessments of calls to this method.

void Compiler::impMarkInlineCandidateHelper(GenTreeCall*           call,
                                            CORINFO_CONTEXT_HANDLE exactContextHnd,
                                            bool                   exactContextNeedsRuntimeLookup,
                                            CORINFO_CALL_INFO*     callInfo)
{
    // Let the strategy know there's another call
    impInlineRoot()->m_inlineStrategy->NoteCall();

    if (!opts.OptEnabled(CLFLG_INLINING))
    {
        /* XXX Mon 8/18/2008
         * This assert is misleading.  The caller does not ensure that we have CLFLG_INLINING set before
         * calling impMarkInlineCandidate.  However, if this assert trips it means that we're an inlinee and
         * CLFLG_MINOPT is set.  That doesn't make a lot of sense.  If you hit this assert, work back and
         * figure out why we did not set MAXOPT for this compile.
         */
        assert(!compIsForInlining());
        return;
    }

    if (compIsForImportOnly())
    {
        // Don't bother creating the inline candidate during verification.
        // Otherwise the call to info.compCompHnd->canInline will trigger a recursive verification
        // that leads to the creation of multiple instances of Compiler.
        return;
    }

    InlineResult inlineResult(this, call, nullptr, "impMarkInlineCandidate");

    // Don't inline if not optimizing root method
    if (opts.compDbgCode)
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_DEBUG_CODEGEN);
        return;
    }

    // Don't inline if inlining into this method is disabled.
    if (impInlineRoot()->m_inlineStrategy->IsInliningDisabled())
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_IS_JIT_NOINLINE);
        return;
    }

    // Don't inline into callers that use the NextCallReturnAddress intrinsic.
    if (info.compHasNextCallRetAddr)
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_USES_NEXT_CALL_RET_ADDR);
        return;
    }

    // Inlining candidate determination needs to honor only IL tail prefix.
    // Inlining takes precedence over implicit tail call optimization (if the call is not directly recursive).
    if (call->IsTailPrefixedCall())
    {
        inlineResult.NoteFatal(InlineObservation::CALLSITE_EXPLICIT_TAIL_PREFIX);
        return;
    }

    // Tail recursion elimination takes precedence over inlining.
    // TODO: We may want to do some of the additional checks from fgMorphCall
    // here to reduce the chance we don't inline a call that won't be optimized
    // as a fast tail call or turned into a loop.
    if (gtIsRecursiveCall(call) && call->IsImplicitTailCall())
    {
        inlineResult.NoteFatal(InlineObservation::CALLSITE_IMPLICIT_REC_TAIL_CALL);
        return;
    }

    if (call->IsVirtual())
    {
        // Allow guarded devirt calls to be treated as inline candidates,
        // but reject all other virtual calls.
        if (!call->IsGuardedDevirtualizationCandidate())
        {
            inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_NOT_DIRECT);
            return;
        }
    }

    /* Ignore helper calls */

    if (call->gtCallType == CT_HELPER)
    {
        assert(!call->IsGuardedDevirtualizationCandidate());
        inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_CALL_TO_HELPER);
        return;
    }

    /* Ignore indirect calls */
    if (call->gtCallType == CT_INDIRECT)
    {
        inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_NOT_DIRECT_MANAGED);
        return;
    }

    /* I removed the check for BBJ_THROW.  BBJ_THROW is usually marked as rarely run.  This more or less
     * restricts the inliner to non-expanding inlines.  I removed the check to allow for non-expanding
     * inlining in throw blocks.  I should consider the same thing for catch and filter regions. */

    CORINFO_METHOD_HANDLE fncHandle;
    unsigned              methAttr;

    if (call->IsGuardedDevirtualizationCandidate())
    {
        if (call->gtGuardedDevirtualizationCandidateInfo->guardedMethodUnboxedEntryHandle != nullptr)
        {
            fncHandle = call->gtGuardedDevirtualizationCandidateInfo->guardedMethodUnboxedEntryHandle;
        }
        else
        {
            fncHandle = call->gtGuardedDevirtualizationCandidateInfo->guardedMethodHandle;
        }
        methAttr = info.compCompHnd->getMethodAttribs(fncHandle);
    }
    else
    {
        fncHandle = call->gtCallMethHnd;

        // Reuse method flags from the original callInfo if possible
        if (fncHandle == callInfo->hMethod)
        {
            methAttr = callInfo->methodFlags;
        }
        else
        {
            methAttr = info.compCompHnd->getMethodAttribs(fncHandle);
        }
    }

#ifdef DEBUG
    if (compStressCompile(STRESS_FORCE_INLINE, 0))
    {
        methAttr |= CORINFO_FLG_FORCEINLINE;
    }
#endif

    // Check for COMPlus_AggressiveInlining
    if (compDoAggressiveInlining)
    {
        methAttr |= CORINFO_FLG_FORCEINLINE;
    }

    if (!(methAttr & CORINFO_FLG_FORCEINLINE))
    {
        /* Don't bother inline blocks that are in the filter region */
        if (bbInCatchHandlerILRange(compCurBB))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("\nWill not inline blocks that are in the catch handler region\n");
            }

#endif

            inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_WITHIN_CATCH);
            return;
        }

        if (bbInFilterILRange(compCurBB))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("\nWill not inline blocks that are in the filter region\n");
            }
#endif

            inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_WITHIN_FILTER);
            return;
        }
    }

    /* Check if we tried to inline this method before */

    if (methAttr & CORINFO_FLG_DONT_INLINE)
    {
        inlineResult.NoteFatal(InlineObservation::CALLEE_IS_NOINLINE);
        return;
    }

    /* Cannot inline synchronized methods */

    if (methAttr & CORINFO_FLG_SYNCH)
    {
        inlineResult.NoteFatal(InlineObservation::CALLEE_IS_SYNCHRONIZED);
        return;
    }

    /* Check legality of PInvoke callsite (for inlining of marshalling code) */

    if (methAttr & CORINFO_FLG_PINVOKE)
    {
        // See comment in impCheckForPInvokeCall
        BasicBlock* block = compIsForInlining() ? impInlineInfo->iciBlock : compCurBB;
        if (!impCanPInvokeInlineCallSite(block))
        {
            inlineResult.NoteFatal(InlineObservation::CALLSITE_PINVOKE_EH);
            return;
        }
    }

    InlineCandidateInfo* inlineCandidateInfo = nullptr;
    impCheckCanInline(call, fncHandle, methAttr, exactContextHnd, &inlineCandidateInfo, &inlineResult);

    if (inlineResult.IsFailure())
    {
        return;
    }

    // The old value should be null OR this call should be a guarded devirtualization candidate.
    assert((call->gtInlineCandidateInfo == nullptr) || call->IsGuardedDevirtualizationCandidate());

    // The new value should not be null.
    assert(inlineCandidateInfo != nullptr);
    inlineCandidateInfo->exactContextNeedsRuntimeLookup = exactContextNeedsRuntimeLookup;
    call->gtInlineCandidateInfo                         = inlineCandidateInfo;

    // If we're in an inlinee compiler, and have a return spill temp, and this inline candidate
    // is also a tail call candidate, it can use the same return spill temp.
    //
    if (compIsForInlining() && call->CanTailCall() &&
        (impInlineInfo->inlineCandidateInfo->preexistingSpillTemp != BAD_VAR_NUM))
    {
        inlineCandidateInfo->preexistingSpillTemp = impInlineInfo->inlineCandidateInfo->preexistingSpillTemp;
        JITDUMP("Inline candidate [%06u] can share spill temp V%02u\n", dspTreeID(call),
                inlineCandidateInfo->preexistingSpillTemp);
    }

    // Mark the call node as inline candidate.
    call->gtFlags |= GTF_CALL_INLINE_CANDIDATE;

    // Let the strategy know there's another candidate.
    impInlineRoot()->m_inlineStrategy->NoteCandidate();

    // Since we're not actually inlining yet, and this call site is
    // still just an inline candidate, there's nothing to report.
    inlineResult.SetReported();
}

/******************************************************************************/
// Returns true if the given intrinsic will be implemented by target-specific
// instructions

bool Compiler::IsTargetIntrinsic(NamedIntrinsic intrinsicName)
{
#if defined(TARGET_XARCH)
    switch (intrinsicName)
    {
        // AMD64/x86 has SSE2 instructions to directly compute sqrt/abs and SSE4.1
        // instructions to directly compute round/ceiling/floor.

        case NI_System_Math_Abs:
        case NI_System_Math_Sqrt:
            return true;

        case NI_System_Math_Ceiling:
        case NI_System_Math_Floor:
        case NI_System_Math_Round:
            return compOpportunisticallyDependsOn(InstructionSet_SSE41);

        case NI_System_Math_FusedMultiplyAdd:
        {
            // AMD64/x86 has FMA3 instructions to directly compute fma. However, in
            // the scenario where it is supported we should have generated GT_HWINTRINSIC
            // nodes in place of the GT_INTRINSIC node.

            assert(!compIsaSupportedDebugOnly(InstructionSet_FMA));
            return false;
        }

        default:
            return false;
    }
#elif defined(TARGET_ARM64)
    switch (intrinsicName)
    {
        case NI_System_Math_Abs:
        case NI_System_Math_Ceiling:
        case NI_System_Math_Floor:
        case NI_System_Math_Round:
        case NI_System_Math_Sqrt:
            return true;

        case NI_System_Math_FusedMultiplyAdd:
        {
            // ARM64 has AdvSimd instructions to directly compute fma. However, in
            // the scenario where it is supported we should have generated GT_HWINTRINSIC
            // nodes in place of the GT_INTRINSIC node.

            assert(!compIsaSupportedDebugOnly(InstructionSet_AdvSimd));
            return false;
        }

        default:
            return false;
    }
#elif defined(TARGET_ARM)
    switch (intrinsicName)
    {
        case NI_System_Math_Abs:
        case NI_System_Math_Round:
        case NI_System_Math_Sqrt:
            return true;

        default:
            return false;
    }
#else
    // TODO: This portion of logic is not implemented for other arch.
    // The reason for returning true is that on all other arch the only intrinsic
    // enabled are target intrinsics.
    return true;
#endif
}

/******************************************************************************/
// Returns true if the given intrinsic will be implemented by calling System.Math
// methods.

bool Compiler::IsIntrinsicImplementedByUserCall(NamedIntrinsic intrinsicName)
{
    // Currently, if a math intrinsic is not implemented by target-specific
    // instructions, it will be implemented by a System.Math call. In the
    // future, if we turn to implementing some of them with helper calls,
    // this predicate needs to be revisited.
    return !IsTargetIntrinsic(intrinsicName);
}

bool Compiler::IsMathIntrinsic(NamedIntrinsic intrinsicName)
{
    switch (intrinsicName)
    {
        case NI_System_Math_Abs:
        case NI_System_Math_Acos:
        case NI_System_Math_Acosh:
        case NI_System_Math_Asin:
        case NI_System_Math_Asinh:
        case NI_System_Math_Atan:
        case NI_System_Math_Atanh:
        case NI_System_Math_Atan2:
        case NI_System_Math_Cbrt:
        case NI_System_Math_Ceiling:
        case NI_System_Math_Cos:
        case NI_System_Math_Cosh:
        case NI_System_Math_Exp:
        case NI_System_Math_Floor:
        case NI_System_Math_FMod:
        case NI_System_Math_FusedMultiplyAdd:
        case NI_System_Math_ILogB:
        case NI_System_Math_Log:
        case NI_System_Math_Log2:
        case NI_System_Math_Log10:
        case NI_System_Math_Pow:
        case NI_System_Math_Round:
        case NI_System_Math_Sin:
        case NI_System_Math_Sinh:
        case NI_System_Math_Sqrt:
        case NI_System_Math_Tan:
        case NI_System_Math_Tanh:
        {
            assert((intrinsicName > NI_SYSTEM_MATH_START) && (intrinsicName < NI_SYSTEM_MATH_END));
            return true;
        }

        default:
        {
            assert((intrinsicName < NI_SYSTEM_MATH_START) || (intrinsicName > NI_SYSTEM_MATH_END));
            return false;
        }
    }
}

bool Compiler::IsMathIntrinsic(GenTree* tree)
{
    return (tree->OperGet() == GT_INTRINSIC) && IsMathIntrinsic(tree->AsIntrinsic()->gtIntrinsicName);
}

//------------------------------------------------------------------------
// impDevirtualizeCall: Attempt to change a virtual vtable call into a
//   normal call
//
// Arguments:
//     call -- the call node to examine/modify
//     method   -- [IN/OUT] the method handle for call. Updated iff call devirtualized.
//     methodFlags -- [IN/OUT] flags for the method to call. Updated iff call devirtualized.
//     pContextHandle -- [IN/OUT] context handle for the call. Updated iff call devirtualized.
//     pExactContextHandle -- [OUT] updated context handle iff call devirtualized
//     isLateDevirtualization -- if devirtualization is happening after importation
//     isExplicitTailCalll -- [IN] true if we plan on using an explicit tail call
//     ilOffset -- IL offset of the call
//
// Notes:
//     Virtual calls in IL will always "invoke" the base class method.
//
//     This transformation looks for evidence that the type of 'this'
//     in the call is exactly known, is a final class or would invoke
//     a final method, and if that and other safety checks pan out,
//     modifies the call and the call info to create a direct call.
//
//     This transformation is initially done in the importer and not
//     in some subsequent optimization pass because we want it to be
//     upstream of inline candidate identification.
//
//     However, later phases may supply improved type information that
//     can enable further devirtualization. We currently reinvoke this
//     code after inlining, if the return value of the inlined call is
//     the 'this obj' of a subsequent virtual call.
//
//     If devirtualization succeeds and the call's this object is a
//     (boxed) value type, the jit will ask the EE for the unboxed entry
//     point. If this exists, the jit will invoke the unboxed entry
//     on the box payload. In addition if the boxing operation is
//     visible to the jit and the call is the only consmer of the box,
//     the jit will try analyze the box to see if the call can be instead
//     instead made on a local copy. If that is doable, the call is
//     updated to invoke the unboxed entry on the local copy and the
//     boxing operation is removed.
//
//     When guarded devirtualization is enabled, this method will mark
//     calls as guarded devirtualization candidates, if the type of `this`
//     is not exactly known, and there is a plausible guess for the type.
//
void Compiler::impDevirtualizeCall(GenTreeCall*            call,
                                   CORINFO_METHOD_HANDLE*  method,
                                   unsigned*               methodFlags,
                                   CORINFO_CONTEXT_HANDLE* pContextHandle,
                                   CORINFO_CONTEXT_HANDLE* pExactContextHandle,
                                   bool                    isLateDevirtualization,
                                   bool                    isExplicitTailCall,
                                   IL_OFFSETX              ilOffset)
{
    assert(call != nullptr);
    assert(method != nullptr);
    assert(methodFlags != nullptr);
    assert(pContextHandle != nullptr);

    // This should be a virtual vtable or virtual stub call.
    //
    assert(call->IsVirtual());

    // Possibly instrument, if not optimizing.
    //
    if (opts.OptimizationDisabled())
    {
        // During importation, optionally flag this block as one that
        // contains calls requiring class profiling. Ideally perhaps
        // we'd just keep track of the calls themselves, so we don't
        // have to search for them later.
        //
        if ((call->gtCallType != CT_INDIRECT) && opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR) &&
            !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) && (JitConfig.JitClassProfiling() > 0) &&
            !isLateDevirtualization)
        {
            JITDUMP("\n ... marking [%06u] in " FMT_BB " for class profile instrumentation\n", dspTreeID(call),
                    compCurBB->bbNum);
            ClassProfileCandidateInfo* pInfo = new (this, CMK_Inlining) ClassProfileCandidateInfo;

            // Record some info needed for the class profiling probe.
            //
            pInfo->ilOffset   = ilOffset;
            pInfo->probeIndex = info.compClassProbeCount++;
            pInfo->stubAddr   = call->gtStubCallStubAddr;

            // note this overwrites gtCallStubAddr, so it needs to be undone
            // during the instrumentation phase, or we won't generate proper
            // code for vsd calls.
            //
            call->gtClassProfileCandidateInfo = pInfo;

            // Flag block as needing scrutiny
            //
            compCurBB->bbFlags |= BBF_HAS_CLASS_PROFILE;
        }

        return;
    }

#if defined(DEBUG)
    // Bail if devirt is disabled.
    if (JitConfig.JitEnableDevirtualization() == 0)
    {
        return;
    }

    // Optionally, print info on devirtualization
    Compiler* const rootCompiler = impInlineRoot();
    const bool      doPrint      = JitConfig.JitPrintDevirtualizedMethods().contains(rootCompiler->info.compMethodName,
                                                                           rootCompiler->info.compClassName,
                                                                           &rootCompiler->info.compMethodInfo->args);
#endif // DEBUG

    // Fetch information about the virtual method we're calling.
    CORINFO_METHOD_HANDLE baseMethod        = *method;
    unsigned              baseMethodAttribs = *methodFlags;

    if (baseMethodAttribs == 0)
    {
        // For late devirt we may not have method attributes, so fetch them.
        baseMethodAttribs = info.compCompHnd->getMethodAttribs(baseMethod);
    }
    else
    {
#if defined(DEBUG)
        // Validate that callInfo has up to date method flags
        const DWORD freshBaseMethodAttribs = info.compCompHnd->getMethodAttribs(baseMethod);

        // All the base method attributes should agree, save that
        // CORINFO_FLG_DONT_INLINE may have changed from 0 to 1
        // because of concurrent jitting activity.
        //
        // Note we don't look at this particular flag bit below, and
        // later on (if we do try and inline) we will rediscover why
        // the method can't be inlined, so there's no danger here in
        // seeing this particular flag bit in different states between
        // the cached and fresh values.
        if ((freshBaseMethodAttribs & ~CORINFO_FLG_DONT_INLINE) != (baseMethodAttribs & ~CORINFO_FLG_DONT_INLINE))
        {
            assert(!"mismatched method attributes");
        }
#endif // DEBUG
    }

    // In R2R mode, we might see virtual stub calls to
    // non-virtuals. For instance cases where the non-virtual method
    // is in a different assembly but is called via CALLVIRT. For
    // verison resilience we must allow for the fact that the method
    // might become virtual in some update.
    //
    // In non-R2R modes CALLVIRT <nonvirtual> will be turned into a
    // regular call+nullcheck upstream, so we won't reach this
    // point.
    if ((baseMethodAttribs & CORINFO_FLG_VIRTUAL) == 0)
    {
        assert(call->IsVirtualStub());
        assert(opts.IsReadyToRun());
        JITDUMP("\nimpDevirtualizeCall: [R2R] base method not virtual, sorry\n");
        return;
    }

    // See what we know about the type of 'this' in the call.
    GenTree*             thisObj      = call->gtCallThisArg->GetNode()->gtEffectiveVal(false);
    bool                 isExact      = false;
    bool                 objIsNonNull = false;
    CORINFO_CLASS_HANDLE objClass     = gtGetClassHandle(thisObj, &isExact, &objIsNonNull);

    // Bail if we know nothing.
    if (objClass == nullptr)
    {
        JITDUMP("\nimpDevirtualizeCall: no type available (op=%s)\n", GenTree::OpName(thisObj->OperGet()));
        return;
    }

    // Fetch information about the class that introduced the virtual method.
    CORINFO_CLASS_HANDLE baseClass        = info.compCompHnd->getMethodClass(baseMethod);
    const DWORD          baseClassAttribs = info.compCompHnd->getClassAttribs(baseClass);

    // Is the call an interface call?
    const bool isInterface = (baseClassAttribs & CORINFO_FLG_INTERFACE) != 0;

    // If the objClass is sealed (final), then we may be able to devirtualize.
    const DWORD objClassAttribs = info.compCompHnd->getClassAttribs(objClass);
    const bool  objClassIsFinal = (objClassAttribs & CORINFO_FLG_FINAL) != 0;

#if defined(DEBUG)
    const char* callKind       = isInterface ? "interface" : "virtual";
    const char* objClassNote   = "[?]";
    const char* objClassName   = "?objClass";
    const char* baseClassName  = "?baseClass";
    const char* baseMethodName = "?baseMethod";

    if (verbose || doPrint)
    {
        objClassNote   = isExact ? " [exact]" : objClassIsFinal ? " [final]" : "";
        objClassName   = info.compCompHnd->getClassName(objClass);
        baseClassName  = info.compCompHnd->getClassName(baseClass);
        baseMethodName = eeGetMethodName(baseMethod, nullptr);

        if (verbose)
        {
            printf("\nimpDevirtualizeCall: Trying to devirtualize %s call:\n"
                   "    class for 'this' is %s%s (attrib %08x)\n"
                   "    base method is %s::%s\n",
                   callKind, objClassName, objClassNote, objClassAttribs, baseClassName, baseMethodName);
        }
    }
#endif // defined(DEBUG)

    // See if the jit's best type for `obj` is an interface.
    // See for instance System.ValueTuple`8::GetHashCode, where lcl 0 is System.IValueTupleInternal
    //   IL_021d:  ldloc.0
    //   IL_021e:  callvirt   instance int32 System.Object::GetHashCode()
    //
    // If so, we can't devirtualize, but we may be able to do guarded devirtualization.
    //
    if ((objClassAttribs & CORINFO_FLG_INTERFACE) != 0)
    {
        // Don't try guarded devirtualiztion when we're doing late devirtualization.
        //
        if (isLateDevirtualization)
        {
            JITDUMP("No guarded devirt during late devirtualization\n");
            return;
        }

        considerGuardedDevirtualization(call, ilOffset, isInterface, baseMethod, baseClass,
                                        pContextHandle DEBUGARG(objClass) DEBUGARG(objClassName));
        return;
    }

    // If we get this far, the jit has a lower bound class type for the `this` object being used for dispatch.
    // It may or may not know enough to devirtualize...
    if (isInterface)
    {
        assert(call->IsVirtualStub());
        JITDUMP("--- base class is interface\n");
    }

    // Fetch the method that would be called based on the declared type of 'this',
    // and prepare to fetch the method attributes.
    //
    CORINFO_DEVIRTUALIZATION_INFO dvInfo;
    dvInfo.virtualMethod = baseMethod;
    dvInfo.objClass      = objClass;
    dvInfo.context       = *pContextHandle;
    dvInfo.detail        = CORINFO_DEVIRTUALIZATION_UNKNOWN;

    info.compCompHnd->resolveVirtualMethod(&dvInfo);

    CORINFO_METHOD_HANDLE  derivedMethod = dvInfo.devirtualizedMethod;
    CORINFO_CONTEXT_HANDLE exactContext  = dvInfo.exactContext;
    CORINFO_CLASS_HANDLE   derivedClass  = NO_CLASS_HANDLE;

    if (derivedMethod != nullptr)
    {
        assert(exactContext != nullptr);
        assert(((size_t)exactContext & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
        derivedClass = (CORINFO_CLASS_HANDLE)((size_t)exactContext & ~CORINFO_CONTEXTFLAGS_MASK);
    }

    DWORD derivedMethodAttribs = 0;
    bool  derivedMethodIsFinal = false;
    bool  canDevirtualize      = false;

#if defined(DEBUG)
    const char* derivedClassName  = "?derivedClass";
    const char* derivedMethodName = "?derivedMethod";
    const char* note              = "inexact or not final";
#endif

    // If we failed to get a method handle, we can't directly devirtualize.
    //
    // This can happen when prejitting, if the devirtualization crosses
    // servicing bubble boundaries, or if objClass is a shared class.
    //
    if (derivedMethod == nullptr)
    {
        JITDUMP("--- no derived method: %s\n", devirtualizationDetailToString(dvInfo.detail));
    }
    else
    {
        // Fetch method attributes to see if method is marked final.
        derivedMethodAttribs = info.compCompHnd->getMethodAttribs(derivedMethod);
        derivedMethodIsFinal = ((derivedMethodAttribs & CORINFO_FLG_FINAL) != 0);

#if defined(DEBUG)
        if (isExact)
        {
            note = "exact";
        }
        else if (objClassIsFinal)
        {
            note = "final class";
        }
        else if (derivedMethodIsFinal)
        {
            note = "final method";
        }

        if (verbose || doPrint)
        {
            derivedMethodName = eeGetMethodName(derivedMethod, nullptr);
            derivedClassName  = eeGetClassName(derivedClass);
            if (verbose)
            {
                printf("    devirt to %s::%s -- %s\n", derivedClassName, derivedMethodName, note);
                gtDispTree(call);
            }
        }
#endif // defined(DEBUG)

        canDevirtualize = isExact || objClassIsFinal || (!isInterface && derivedMethodIsFinal);
    }

    // We still might be able to do a guarded devirtualization.
    // Note the call might be an interface call or a virtual call.
    //
    if (!canDevirtualize)
    {
        JITDUMP("    Class not final or exact%s\n", isInterface ? "" : ", and method not final");

#if defined(DEBUG)
        // If we know the object type exactly, we generally expect we can devirtualize.
        // (don't when doing late devirt as we won't have an owner type (yet))
        //
        if (!isLateDevirtualization && (isExact || objClassIsFinal) && JitConfig.JitNoteFailedExactDevirtualization())
        {
            printf("@@@ Exact/Final devirt failure in %s at [%06u] $ %s\n", info.compFullName, dspTreeID(call),
                   devirtualizationDetailToString(dvInfo.detail));
        }
#endif

        // Don't try guarded devirtualiztion if we're doing late devirtualization.
        //
        if (isLateDevirtualization)
        {
            JITDUMP("No guarded devirt during late devirtualization\n");
            return;
        }

        considerGuardedDevirtualization(call, ilOffset, isInterface, baseMethod, baseClass,
                                        pContextHandle DEBUGARG(objClass) DEBUGARG(objClassName));
        return;
    }

    // All checks done. Time to transform the call.
    //
    // We should always have an exact class context.
    //
    // Note that wouldnt' be true if the runtime side supported array interface devirt,
    // the resulting method would be a generic method of the non-generic SZArrayHelper class.
    //
    assert(canDevirtualize);

    JITDUMP("    %s; can devirtualize\n", note);

    // Make the updates.
    call->gtFlags &= ~GTF_CALL_VIRT_VTABLE;
    call->gtFlags &= ~GTF_CALL_VIRT_STUB;
    call->gtCallMethHnd = derivedMethod;
    call->gtCallType    = CT_USER_FUNC;
    call->gtCallMoreFlags |= GTF_CALL_M_DEVIRTUALIZED;

    // Virtual calls include an implicit null check, which we may
    // now need to make explicit.
    if (!objIsNonNull)
    {
        call->gtFlags |= GTF_CALL_NULLCHECK;
    }

    // Clear the inline candidate info (may be non-null since
    // it's a union field used for other things by virtual
    // stubs)
    call->gtInlineCandidateInfo = nullptr;

#if defined(DEBUG)
    if (verbose)
    {
        printf("... after devirt...\n");
        gtDispTree(call);
    }

    if (doPrint)
    {
        printf("Devirtualized %s call to %s:%s; now direct call to %s:%s [%s]\n", callKind, baseClassName,
               baseMethodName, derivedClassName, derivedMethodName, note);
    }

    // If we successfully devirtualized based on an exact or final class,
    // and we have dynamic PGO data describing the likely class, make sure they agree.
    //
    // If pgo source is not dynamic we may see likely classes from other versions of this code
    // where types had different properties.
    //
    // If method is an inlinee we may be specializing to a class that wasn't seen at runtime.
    //
    const bool canSensiblyCheck =
        (isExact || objClassIsFinal) && (fgPgoSource == ICorJitInfo::PgoSource::Dynamic) && !compIsForInlining();
    if (JitConfig.JitCrossCheckDevirtualizationAndPGO() && canSensiblyCheck)
    {
        unsigned likelihood      = 0;
        unsigned numberOfClasses = 0;

        CORINFO_CLASS_HANDLE likelyClass =
            getLikelyClass(fgPgoSchema, fgPgoSchemaCount, fgPgoData, ilOffset, &likelihood, &numberOfClasses);

        if (likelyClass != NO_CLASS_HANDLE)
        {
            // PGO had better agree the class we devirtualized to is plausible.
            //
            if (likelyClass != derivedClass)
            {
                // Managed type system may report different addresses for a class handle
                // at different times....?
                //
                // Also, AOT may have a more nuanced notion of class equality.
                //
                if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
                {
                    bool mismatch = true;

                    // derivedClass will be the introducer of derived method, so it's possible
                    // likelyClass is a non-overriding subclass. Check up the hierarchy.
                    //
                    CORINFO_CLASS_HANDLE parentClass = likelyClass;
                    while (parentClass != NO_CLASS_HANDLE)
                    {
                        if (parentClass == derivedClass)
                        {
                            mismatch = false;
                            break;
                        }

                        parentClass = info.compCompHnd->getParentType(parentClass);
                    }

                    if (mismatch || (numberOfClasses != 1) || (likelihood != 100))
                    {
                        printf("@@@ Likely %p (%s) != Derived %p (%s) [n=%u, l=%u, il=%u] in %s \n", likelyClass,
                               eeGetClassName(likelyClass), derivedClass, eeGetClassName(derivedClass), numberOfClasses,
                               likelihood, ilOffset, info.compFullName);
                    }

                    assert(!(mismatch || (numberOfClasses != 1) || (likelihood != 100)));
                }
            }
        }
    }
#endif // defined(DEBUG)

    // If the 'this' object is a value class, see if we can rework the call to invoke the
    // unboxed entry. This effectively inlines the normally un-inlineable wrapper stub
    // and exposes the potentially inlinable unboxed entry method.
    //
    // We won't optimize explicit tail calls, as ensuring we get the right tail call info
    // is tricky (we'd need to pass an updated sig and resolved token back to some callers).
    //
    // Note we may not have a derived class in some cases (eg interface call on an array)
    //
    if (info.compCompHnd->isValueClass(derivedClass))
    {
        if (isExplicitTailCall)
        {
            JITDUMP("Have a direct explicit tail call to boxed entry point; can't optimize further\n");
        }
        else if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
        {
            // Per https://github.com/dotnet/runtime/issues/52483, crossgen2 seemingly gets
            // confused about whether the unboxed entry requires an extra arg.
            // So defer further optimization for now.
            //
            JITDUMP("Have a direct boxed entry point, prejitting. Can't optimize further yet.\n");
        }
        else
        {
            JITDUMP("Have a direct call to boxed entry point. Trying to optimize to call an unboxed entry point\n");

            // Note for some shared methods the unboxed entry point requires an extra parameter.
            bool                  requiresInstMethodTableArg = false;
            CORINFO_METHOD_HANDLE unboxedEntryMethod =
                info.compCompHnd->getUnboxedEntry(derivedMethod, &requiresInstMethodTableArg);

            if (unboxedEntryMethod != nullptr)
            {
                bool optimizedTheBox = false;

                // If the 'this' object is a local box, see if we can revise things
                // to not require boxing.
                //
                if (thisObj->IsBoxedValue() && !isExplicitTailCall)
                {
                    // Since the call is the only consumer of the box, we know the box can't escape
                    // since it is being passed an interior pointer.
                    //
                    // So, revise the box to simply create a local copy, use the address of that copy
                    // as the this pointer, and update the entry point to the unboxed entry.
                    //
                    // Ideally, we then inline the boxed method and and if it turns out not to modify
                    // the copy, we can undo the copy too.
                    if (requiresInstMethodTableArg)
                    {
                        // Perform a trial box removal and ask for the type handle tree that fed the box.
                        //
                        JITDUMP("Unboxed entry needs method table arg...\n");
                        GenTree* methodTableArg =
                            gtTryRemoveBoxUpstreamEffects(thisObj, BR_DONT_REMOVE_WANT_TYPE_HANDLE);

                        if (methodTableArg != nullptr)
                        {
                            // If that worked, turn the box into a copy to a local var
                            //
                            JITDUMP("Found suitable method table arg tree [%06u]\n", dspTreeID(methodTableArg));
                            GenTree* localCopyThis = gtTryRemoveBoxUpstreamEffects(thisObj, BR_MAKE_LOCAL_COPY);

                            if (localCopyThis != nullptr)
                            {
                                // Pass the local var as this and the type handle as a new arg
                                //
                                JITDUMP("Success! invoking unboxed entry point on local copy, and passing method table "
                                        "arg\n");
                                call->gtCallThisArg = gtNewCallArgs(localCopyThis);
                                call->gtCallMoreFlags |= GTF_CALL_M_UNBOXED;

                                // Prepend for R2L arg passing or empty L2R passing
                                // Append for non-empty L2R
                                //
                                if ((Target::g_tgtArgOrder == Target::ARG_ORDER_R2L) || (call->gtCallArgs == nullptr))
                                {
                                    // If there's a ret buf, the method table is the second arg.
                                    //
                                    if (call->HasRetBufArg())
                                    {
                                        gtInsertNewCallArgAfter(methodTableArg, call->gtCallArgs);
                                    }
                                    else
                                    {
                                        call->gtCallArgs = gtPrependNewCallArg(methodTableArg, call->gtCallArgs);
                                    }
                                }
                                else
                                {
                                    GenTreeCall::Use* beforeArg = call->gtCallArgs;
                                    while (beforeArg->GetNext() != nullptr)
                                    {
                                        beforeArg = beforeArg->GetNext();
                                    }

                                    beforeArg->SetNext(gtNewCallArgs(methodTableArg));
                                }

                                call->gtCallMethHnd = unboxedEntryMethod;
                                derivedMethod       = unboxedEntryMethod;

                                // Method attributes will differ because unboxed entry point is shared
                                //
                                const DWORD unboxedMethodAttribs =
                                    info.compCompHnd->getMethodAttribs(unboxedEntryMethod);
                                JITDUMP("Updating method attribs from 0x%08x to 0x%08x\n", derivedMethodAttribs,
                                        unboxedMethodAttribs);
                                derivedMethodAttribs = unboxedMethodAttribs;
                                optimizedTheBox      = true;
                            }
                            else
                            {
                                JITDUMP("Sorry, failed to undo the box -- can't convert to local copy\n");
                            }
                        }
                        else
                        {
                            JITDUMP("Sorry, failed to undo the box -- can't find method table arg\n");
                        }
                    }
                    else
                    {
                        JITDUMP("Found unboxed entry point, trying to simplify box to a local copy\n");
                        GenTree* localCopyThis = gtTryRemoveBoxUpstreamEffects(thisObj, BR_MAKE_LOCAL_COPY);

                        if (localCopyThis != nullptr)
                        {
                            JITDUMP("Success! invoking unboxed entry point on local copy\n");
                            call->gtCallThisArg = gtNewCallArgs(localCopyThis);
                            call->gtCallMethHnd = unboxedEntryMethod;
                            call->gtCallMoreFlags |= GTF_CALL_M_UNBOXED;
                            derivedMethod = unboxedEntryMethod;

                            optimizedTheBox = true;
                        }
                        else
                        {
                            JITDUMP("Sorry, failed to undo the box\n");
                        }
                    }

                    if (optimizedTheBox)
                    {

#if FEATURE_TAILCALL_OPT
                        if (call->IsImplicitTailCall())
                        {
                            JITDUMP("Clearing the implicit tail call flag\n");

                            // If set, we clear the implicit tail call flag
                            // as we just introduced a new address taken local variable
                            //
                            call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
                        }
#endif // FEATURE_TAILCALL_OPT
                    }
                }

                if (!optimizedTheBox)
                {
                    // If we get here, we have a boxed value class that either wasn't boxed
                    // locally, or was boxed locally but we were unable to remove the box for
                    // various reasons.
                    //
                    // We can still update the call to invoke the unboxed entry, if the
                    // boxed value is simple.
                    //
                    if (requiresInstMethodTableArg)
                    {
                        // Get the method table from the boxed object.
                        //
                        GenTree* const thisArg       = call->gtCallThisArg->GetNode();
                        GenTree* const clonedThisArg = gtClone(thisArg);

                        if (clonedThisArg == nullptr)
                        {
                            JITDUMP(
                                "unboxed entry needs MT arg, but `this` was too complex to clone. Deferring update.\n");
                        }
                        else
                        {
                            JITDUMP("revising call to invoke unboxed entry with additional method table arg\n");

                            GenTree* const methodTableArg = gtNewMethodTableLookup(clonedThisArg);

                            // Update the 'this' pointer to refer to the box payload
                            //
                            GenTree* const payloadOffset = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                            GenTree* const boxPayload    = gtNewOperNode(GT_ADD, TYP_BYREF, thisArg, payloadOffset);

                            call->gtCallThisArg = gtNewCallArgs(boxPayload);
                            call->gtCallMethHnd = unboxedEntryMethod;
                            call->gtCallMoreFlags |= GTF_CALL_M_UNBOXED;

                            // Method attributes will differ because unboxed entry point is shared
                            //
                            const DWORD unboxedMethodAttribs = info.compCompHnd->getMethodAttribs(unboxedEntryMethod);
                            JITDUMP("Updating method attribs from 0x%08x to 0x%08x\n", derivedMethodAttribs,
                                    unboxedMethodAttribs);
                            derivedMethod        = unboxedEntryMethod;
                            derivedMethodAttribs = unboxedMethodAttribs;

                            // Add the method table argument.
                            //
                            // Prepend for R2L arg passing or empty L2R passing
                            // Append for non-empty L2R
                            //
                            if ((Target::g_tgtArgOrder == Target::ARG_ORDER_R2L) || (call->gtCallArgs == nullptr))
                            {
                                // If there's a ret buf, the method table is the second arg.
                                //
                                if (call->HasRetBufArg())
                                {
                                    gtInsertNewCallArgAfter(methodTableArg, call->gtCallArgs);
                                }
                                else
                                {
                                    call->gtCallArgs = gtPrependNewCallArg(methodTableArg, call->gtCallArgs);
                                }
                            }
                            else
                            {
                                GenTreeCall::Use* beforeArg = call->gtCallArgs;
                                while (beforeArg->GetNext() != nullptr)
                                {
                                    beforeArg = beforeArg->GetNext();
                                }

                                beforeArg->SetNext(gtNewCallArgs(methodTableArg));
                            }
                        }
                    }
                    else
                    {
                        JITDUMP("revising call to invoke unboxed entry\n");

                        GenTree* const thisArg       = call->gtCallThisArg->GetNode();
                        GenTree* const payloadOffset = gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL);
                        GenTree* const boxPayload    = gtNewOperNode(GT_ADD, TYP_BYREF, thisArg, payloadOffset);

                        call->gtCallThisArg = gtNewCallArgs(boxPayload);
                        call->gtCallMethHnd = unboxedEntryMethod;
                        call->gtCallMoreFlags |= GTF_CALL_M_UNBOXED;
                        derivedMethod = unboxedEntryMethod;
                    }
                }
            }
            else
            {
                // Many of the low-level methods on value classes won't have unboxed entries,
                // as they need access to the type of the object.
                //
                // Note this may be a cue for us to stack allocate the boxed object, since
                // we probably know that these objects don't escape.
                JITDUMP("Sorry, failed to find unboxed entry point\n");
            }
        }
    }

    // Need to update call info too.
    //
    *method      = derivedMethod;
    *methodFlags = derivedMethodAttribs;

    // Update context handle
    //
    *pContextHandle = MAKE_METHODCONTEXT(derivedMethod);

    // Update exact context handle.
    //
    if (pExactContextHandle != nullptr)
    {
        *pExactContextHandle = MAKE_CLASSCONTEXT(derivedClass);
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        // For R2R, getCallInfo triggers bookkeeping on the zap
        // side so we need to call it here.
        //
        // First, cons up a suitable resolved token.
        CORINFO_RESOLVED_TOKEN derivedResolvedToken = {};

        derivedResolvedToken.tokenScope   = info.compCompHnd->getMethodModule(derivedMethod);
        derivedResolvedToken.tokenContext = *pContextHandle;
        derivedResolvedToken.token        = info.compCompHnd->getMethodDefFromMethod(derivedMethod);
        derivedResolvedToken.tokenType    = CORINFO_TOKENKIND_DevirtualizedMethod;
        derivedResolvedToken.hClass       = derivedClass;
        derivedResolvedToken.hMethod      = derivedMethod;

        // Look up the new call info.
        CORINFO_CALL_INFO derivedCallInfo;
        eeGetCallInfo(&derivedResolvedToken, nullptr, addVerifyFlag(CORINFO_CALLINFO_ALLOWINSTPARAM), &derivedCallInfo);

        // Update the call.
        call->gtCallMoreFlags &= ~GTF_CALL_M_VIRTSTUB_REL_INDIRECT;
        call->gtCallMoreFlags &= ~GTF_CALL_M_R2R_REL_INDIRECT;
        call->setEntryPoint(derivedCallInfo.codePointerLookup.constLookup);
    }
#endif // FEATURE_READYTORUN_COMPILER
}

//------------------------------------------------------------------------
// impGetSpecialIntrinsicExactReturnType: Look for special cases where a call
//   to an intrinsic returns an exact type
//
// Arguments:
//     methodHnd -- handle for the special intrinsic method
//
// Returns:
//     Exact class handle returned by the intrinsic call, if known.
//     Nullptr if not known, or not likely to lead to beneficial optimization.

CORINFO_CLASS_HANDLE Compiler::impGetSpecialIntrinsicExactReturnType(CORINFO_METHOD_HANDLE methodHnd)
{
    JITDUMP("Special intrinsic: looking for exact type returned by %s\n", eeGetMethodFullName(methodHnd));

    CORINFO_CLASS_HANDLE result = nullptr;

    // See what intrinisc we have...
    const NamedIntrinsic ni = lookupNamedIntrinsic(methodHnd);
    switch (ni)
    {
        case NI_System_Collections_Generic_Comparer_get_Default:
        case NI_System_Collections_Generic_EqualityComparer_get_Default:
        {
            // Expect one class generic parameter; figure out which it is.
            CORINFO_SIG_INFO sig;
            info.compCompHnd->getMethodSig(methodHnd, &sig);
            assert(sig.sigInst.classInstCount == 1);
            CORINFO_CLASS_HANDLE typeHnd = sig.sigInst.classInst[0];
            assert(typeHnd != nullptr);

            // Lookup can incorrect when we have __Canon as it won't appear
            // to implement any interface types.
            //
            // And if we do not have a final type, devirt & inlining is
            // unlikely to result in much simplification.
            //
            // We can use CORINFO_FLG_FINAL to screen out both of these cases.
            const DWORD typeAttribs = info.compCompHnd->getClassAttribs(typeHnd);
            const bool  isFinalType = ((typeAttribs & CORINFO_FLG_FINAL) != 0);

            if (isFinalType)
            {
                if (ni == NI_System_Collections_Generic_EqualityComparer_get_Default)
                {
                    result = info.compCompHnd->getDefaultEqualityComparerClass(typeHnd);
                }
                else
                {
                    assert(ni == NI_System_Collections_Generic_Comparer_get_Default);
                    result = info.compCompHnd->getDefaultComparerClass(typeHnd);
                }
                JITDUMP("Special intrinsic for type %s: return type is %s\n", eeGetClassName(typeHnd),
                        result != nullptr ? eeGetClassName(result) : "unknown");
            }
            else
            {
                JITDUMP("Special intrinsic for type %s: type not final, so deferring opt\n", eeGetClassName(typeHnd));
            }

            break;
        }

        default:
        {
            JITDUMP("This special intrinsic not handled, sorry...\n");
            break;
        }
    }

    return result;
}

//------------------------------------------------------------------------
// impAllocateToken: create CORINFO_RESOLVED_TOKEN into jit-allocated memory and init it.
//
// Arguments:
//    token - init value for the allocated token.
//
// Return Value:
//    pointer to token into jit-allocated memory.
CORINFO_RESOLVED_TOKEN* Compiler::impAllocateToken(const CORINFO_RESOLVED_TOKEN& token)
{
    CORINFO_RESOLVED_TOKEN* memory = getAllocator(CMK_Unknown).allocate<CORINFO_RESOLVED_TOKEN>(1);
    *memory                        = token;
    return memory;
}

//------------------------------------------------------------------------
// SpillRetExprHelper: iterate through arguments tree and spill ret_expr to local variables.
//
class SpillRetExprHelper
{
public:
    SpillRetExprHelper(Compiler* comp) : comp(comp)
    {
    }

    void StoreRetExprResultsInArgs(GenTreeCall* call)
    {
        for (GenTreeCall::Use& use : call->Args())
        {
            comp->fgWalkTreePre(&use.NodeRef(), SpillRetExprVisitor, this);
        }

        if (call->gtCallThisArg != nullptr)
        {
            comp->fgWalkTreePre(&call->gtCallThisArg->NodeRef(), SpillRetExprVisitor, this);
        }
    }

private:
    static Compiler::fgWalkResult SpillRetExprVisitor(GenTree** pTree, Compiler::fgWalkData* fgWalkPre)
    {
        assert((pTree != nullptr) && (*pTree != nullptr));
        GenTree* tree = *pTree;
        if ((tree->gtFlags & GTF_CALL) == 0)
        {
            // Trees with ret_expr are marked as GTF_CALL.
            return Compiler::WALK_SKIP_SUBTREES;
        }
        if (tree->OperGet() == GT_RET_EXPR)
        {
            SpillRetExprHelper* walker = static_cast<SpillRetExprHelper*>(fgWalkPre->pCallbackData);
            walker->StoreRetExprAsLocalVar(pTree);
        }
        return Compiler::WALK_CONTINUE;
    }

    void StoreRetExprAsLocalVar(GenTree** pRetExpr)
    {
        GenTree* retExpr = *pRetExpr;
        assert(retExpr->OperGet() == GT_RET_EXPR);
        const unsigned tmp = comp->lvaGrabTemp(true DEBUGARG("spilling ret_expr"));
        JITDUMP("Storing return expression [%06u] to a local var V%02u.\n", comp->dspTreeID(retExpr), tmp);
        comp->impAssignTempGen(tmp, retExpr, (unsigned)Compiler::CHECK_SPILL_NONE);
        *pRetExpr = comp->gtNewLclvNode(tmp, retExpr->TypeGet());

        if (retExpr->TypeGet() == TYP_REF)
        {
            assert(comp->lvaTable[tmp].lvSingleDef == 0);
            comp->lvaTable[tmp].lvSingleDef = 1;
            JITDUMP("Marked V%02u as a single def temp\n", tmp);

            bool                 isExact   = false;
            bool                 isNonNull = false;
            CORINFO_CLASS_HANDLE retClsHnd = comp->gtGetClassHandle(retExpr, &isExact, &isNonNull);
            if (retClsHnd != nullptr)
            {
                comp->lvaSetClass(tmp, retClsHnd, isExact);
            }
        }
    }

private:
    Compiler* comp;
};

//------------------------------------------------------------------------
// addFatPointerCandidate: mark the call and the method, that they have a fat pointer candidate.
//                         Spill ret_expr in the call node, because they can't be cloned.
//
// Arguments:
//    call - fat calli candidate
//
void Compiler::addFatPointerCandidate(GenTreeCall* call)
{
    JITDUMP("Marking call [%06u] as fat pointer candidate\n", dspTreeID(call));
    setMethodHasFatPointer();
    call->SetFatPointerCandidate();
    SpillRetExprHelper helper(this);
    helper.StoreRetExprResultsInArgs(call);
}

//------------------------------------------------------------------------
// considerGuardedDevirtualization: see if we can profitably guess at the
//    class involved in an interface or virtual call.
//
// Arguments:
//
//    call - potential guarded devirtualization candidate
//    ilOffset - IL offset of the call instruction
//    isInterface - true if this is an interface call
//    baseMethod - target method of the call
//    baseClass - class that introduced the target method
//    pContextHandle - context handle for the call
//    objClass - class of 'this' in the call
//    objClassName - name of the obj Class
//
// Notes:
//    Consults with VM to see if there's a likely class at runtime,
//    if so, adds a candidate for guarded devirtualization.
//
void Compiler::considerGuardedDevirtualization(
    GenTreeCall*            call,
    IL_OFFSETX              ilOffset,
    bool                    isInterface,
    CORINFO_METHOD_HANDLE   baseMethod,
    CORINFO_CLASS_HANDLE    baseClass,
    CORINFO_CONTEXT_HANDLE* pContextHandle DEBUGARG(CORINFO_CLASS_HANDLE objClass) DEBUGARG(const char* objClassName))
{
#if defined(DEBUG)
    const char* callKind = isInterface ? "interface" : "virtual";
#endif

    JITDUMP("Considering guarded devirtualization at IL offset %u (0x%x)\n", ilOffset, ilOffset);

    // We currently only get likely class guesses when there is PGO data
    // with class profiles.
    //
    if (fgPgoClassProfiles == 0)
    {
        JITDUMP("Not guessing for class: no class profile pgo data, or pgo disabled\n");
        return;
    }

    // See if there's a likely guess for the class.
    //
    const unsigned likelihoodThreshold = isInterface ? 25 : 30;
    unsigned       likelihood          = 0;
    unsigned       numberOfClasses     = 0;

    CORINFO_CLASS_HANDLE likelyClass =
        getLikelyClass(fgPgoSchema, fgPgoSchemaCount, fgPgoData, ilOffset, &likelihood, &numberOfClasses);

    if (likelyClass == NO_CLASS_HANDLE)
    {
        JITDUMP("No likely class, sorry\n");
        return;
    }

    JITDUMP("Likely class for %p (%s) is %p (%s) [likelihood:%u classes seen:%u]\n", dspPtr(objClass), objClassName,
            likelyClass, eeGetClassName(likelyClass), likelihood, numberOfClasses);

    // Todo: a more advanced heuristic using likelihood, number of
    // classes, and the profile count for this block.
    //
    // For now we will guess if the likelihood is at least 25%/30% (intfc/virt), as studies
    // have shown this transformation should pay off even if we guess wrong sometimes.
    //
    if (likelihood < likelihoodThreshold)
    {
        JITDUMP("Not guessing for class; likelihood is below %s call threshold %u\n", callKind, likelihoodThreshold);
        return;
    }

    // Figure out which method will be called.
    //
    CORINFO_DEVIRTUALIZATION_INFO dvInfo;
    dvInfo.virtualMethod = baseMethod;
    dvInfo.objClass      = likelyClass;
    dvInfo.context       = *pContextHandle;

    const bool canResolve = info.compCompHnd->resolveVirtualMethod(&dvInfo);

    if (!canResolve)
    {
        JITDUMP("Can't figure out which method would be invoked, sorry\n");
        return;
    }

    CORINFO_METHOD_HANDLE likelyMethod = dvInfo.devirtualizedMethod;
    JITDUMP("%s call would invoke method %s\n", callKind, eeGetMethodName(likelyMethod, nullptr));

    // Add this as a potential candidate.
    //
    uint32_t const likelyMethodAttribs = info.compCompHnd->getMethodAttribs(likelyMethod);
    uint32_t const likelyClassAttribs  = info.compCompHnd->getClassAttribs(likelyClass);
    addGuardedDevirtualizationCandidate(call, likelyMethod, likelyClass, likelyMethodAttribs, likelyClassAttribs,
                                        likelihood);
}

//------------------------------------------------------------------------
// addGuardedDevirtualizationCandidate: potentially mark the call as a guarded
//    devirtualization candidate
//
// Notes:
//
// Call sites in rare or unoptimized code, and calls that require cookies are
// not marked as candidates.
//
// As part of marking the candidate, the code spills GT_RET_EXPRs anywhere in any
// child tree, because and we need to clone all these trees when we clone the call
// as part of guarded devirtualization, and these IR nodes can't be cloned.
//
// Arguments:
//    call - potential guarded devirtualization candidate
//    methodHandle - method that will be invoked if the class test succeeds
//    classHandle - class that will be tested for at runtime
//    methodAttr - attributes of the method
//    classAttr - attributes of the class
//    likelihood - odds that this class is the class seen at runtime
//
void Compiler::addGuardedDevirtualizationCandidate(GenTreeCall*          call,
                                                   CORINFO_METHOD_HANDLE methodHandle,
                                                   CORINFO_CLASS_HANDLE  classHandle,
                                                   unsigned              methodAttr,
                                                   unsigned              classAttr,
                                                   unsigned              likelihood)
{
    // This transformation only makes sense for virtual calls
    assert(call->IsVirtual());

    // Only mark calls if the feature is enabled.
    const bool isEnabled = JitConfig.JitEnableGuardedDevirtualization() > 0;

    if (!isEnabled)
    {
        JITDUMP("NOT Marking call [%06u] as guarded devirtualization candidate -- disabled by jit config\n",
                dspTreeID(call));
        return;
    }

    // Bail if not optimizing or the call site is very likely cold
    if (compCurBB->isRunRarely() || opts.OptimizationDisabled())
    {
        JITDUMP("NOT Marking call [%06u] as guarded devirtualization candidate -- rare / dbg / minopts\n",
                dspTreeID(call));
        return;
    }

    // CT_INDIRECT calls may use the cookie, bail if so...
    //
    // If transforming these provides a benefit, we could save this off in the same way
    // we save the stub address below.
    if ((call->gtCallType == CT_INDIRECT) && (call->AsCall()->gtCallCookie != nullptr))
    {
        JITDUMP("NOT Marking call [%06u] as guarded devirtualization candidate -- CT_INDIRECT with cookie\n",
                dspTreeID(call));
        return;
    }

#ifdef DEBUG

    // See if disabled by range
    //
    static ConfigMethodRange JitGuardedDevirtualizationRange;
    JitGuardedDevirtualizationRange.EnsureInit(JitConfig.JitGuardedDevirtualizationRange());
    assert(!JitGuardedDevirtualizationRange.Error());
    if (!JitGuardedDevirtualizationRange.Contains(impInlineRoot()->info.compMethodHash()))
    {
        JITDUMP("NOT Marking call [%06u] as guarded devirtualization candidate -- excluded by "
                "JitGuardedDevirtualizationRange",
                dspTreeID(call));
        return;
    }

#endif

    // We're all set, proceed with candidate creation.
    //
    JITDUMP("Marking call [%06u] as guarded devirtualization candidate; will guess for class %s\n", dspTreeID(call),
            eeGetClassName(classHandle));
    setMethodHasGuardedDevirtualization();
    call->SetGuardedDevirtualizationCandidate();

    // Spill off any GT_RET_EXPR subtrees so we can clone the call.
    //
    SpillRetExprHelper helper(this);
    helper.StoreRetExprResultsInArgs(call);

    // Gather some information for later. Note we actually allocate InlineCandidateInfo
    // here, as the devirtualized half of this call will likely become an inline candidate.
    //
    GuardedDevirtualizationCandidateInfo* pInfo = new (this, CMK_Inlining) InlineCandidateInfo;

    pInfo->guardedMethodHandle             = methodHandle;
    pInfo->guardedMethodUnboxedEntryHandle = nullptr;
    pInfo->guardedClassHandle              = classHandle;
    pInfo->likelihood                      = likelihood;
    pInfo->requiresInstMethodTableArg      = false;

    // If the guarded class is a value class, look for an unboxed entry point.
    //
    if ((classAttr & CORINFO_FLG_VALUECLASS) != 0)
    {
        JITDUMP("    ... class is a value class, looking for unboxed entry\n");
        bool                  requiresInstMethodTableArg = false;
        CORINFO_METHOD_HANDLE unboxedEntryMethodHandle =
            info.compCompHnd->getUnboxedEntry(methodHandle, &requiresInstMethodTableArg);

        if (unboxedEntryMethodHandle != nullptr)
        {
            JITDUMP("    ... updating GDV candidate with unboxed entry info\n");
            pInfo->guardedMethodUnboxedEntryHandle = unboxedEntryMethodHandle;
            pInfo->requiresInstMethodTableArg      = requiresInstMethodTableArg;
        }
    }

    // Save off the stub address since it shares a union with the candidate info.
    //
    if (call->IsVirtualStub())
    {
        JITDUMP("Saving stub addr %p in candidate info\n", dspPtr(call->gtStubCallStubAddr));
        pInfo->stubAddr = call->gtStubCallStubAddr;
    }
    else
    {
        pInfo->stubAddr = nullptr;
    }

    call->gtGuardedDevirtualizationCandidateInfo = pInfo;
}

void Compiler::addExpRuntimeLookupCandidate(GenTreeCall* call)
{
    setMethodHasExpRuntimeLookup();
    call->SetExpRuntimeLookup();
}

//------------------------------------------------------------------------
// impIsClassExact: check if a class handle can only describe values
//    of exactly one class.
//
// Arguments:
//    classHnd - handle for class in question
//
// Returns:
//    true if class is final and not subject to special casting from
//    variance or similar.
//
// Note:
//    We are conservative on arrays of primitive types here.

bool Compiler::impIsClassExact(CORINFO_CLASS_HANDLE classHnd)
{
    DWORD flags     = info.compCompHnd->getClassAttribs(classHnd);
    DWORD flagsMask = CORINFO_FLG_FINAL | CORINFO_FLG_VARIANCE | CORINFO_FLG_ARRAY;

    if ((flags & flagsMask) == CORINFO_FLG_FINAL)
    {
        return true;
    }
    if ((flags & flagsMask) == (CORINFO_FLG_FINAL | CORINFO_FLG_ARRAY))
    {
        CORINFO_CLASS_HANDLE arrayElementHandle = nullptr;
        CorInfoType          type               = info.compCompHnd->getChildType(classHnd, &arrayElementHandle);

        if ((type == CORINFO_TYPE_CLASS) || (type == CORINFO_TYPE_VALUECLASS))
        {
            return impIsClassExact(arrayElementHandle);
        }
    }
    return false;
}

//------------------------------------------------------------------------
// impCanSkipCovariantStoreCheck: see if storing a ref type value to an array
//    can skip the array store covariance check.
//
// Arguments:
//    value -- tree producing the value to store
//    array -- tree representing the array to store to
//
// Returns:
//    true if the store does not require a covariance check.
//
bool Compiler::impCanSkipCovariantStoreCheck(GenTree* value, GenTree* array)
{
    // We should only call this when optimizing.
    assert(opts.OptimizationEnabled());

    // Check for assignment to same array, ie. arrLcl[i] = arrLcl[j]
    if (value->OperIs(GT_INDEX) && array->OperIs(GT_LCL_VAR))
    {
        GenTree* valueIndex = value->AsIndex()->Arr();
        if (valueIndex->OperIs(GT_LCL_VAR))
        {
            unsigned valueLcl = valueIndex->AsLclVar()->GetLclNum();
            unsigned arrayLcl = array->AsLclVar()->GetLclNum();
            if ((valueLcl == arrayLcl) && !lvaGetDesc(arrayLcl)->lvAddrExposed)
            {
                JITDUMP("\nstelem of ref from same array: skipping covariant store check\n");
                return true;
            }
        }
    }

    // Check for assignment of NULL.
    if (value->OperIs(GT_CNS_INT))
    {
        assert(value->gtType == TYP_REF);
        if (value->AsIntCon()->gtIconVal == 0)
        {
            JITDUMP("\nstelem of null: skipping covariant store check\n");
            return true;
        }
        // Non-0 const refs can only occur with frozen objects
        assert(value->IsIconHandle(GTF_ICON_STR_HDL));
        assert(doesMethodHaveFrozenString() ||
               (compIsForInlining() && impInlineInfo->InlinerCompiler->doesMethodHaveFrozenString()));
    }

    // Try and get a class handle for the array
    if (value->gtType != TYP_REF)
    {
        return false;
    }

    bool                 arrayIsExact   = false;
    bool                 arrayIsNonNull = false;
    CORINFO_CLASS_HANDLE arrayHandle    = gtGetClassHandle(array, &arrayIsExact, &arrayIsNonNull);

    if (arrayHandle == NO_CLASS_HANDLE)
    {
        return false;
    }

    // There are some methods in corelib where we're storing to an array but the IL
    // doesn't reflect this (see SZArrayHelper). Avoid.
    DWORD attribs = info.compCompHnd->getClassAttribs(arrayHandle);
    if ((attribs & CORINFO_FLG_ARRAY) == 0)
    {
        return false;
    }

    CORINFO_CLASS_HANDLE arrayElementHandle = nullptr;
    CorInfoType          arrayElemType      = info.compCompHnd->getChildType(arrayHandle, &arrayElementHandle);

    // Verify array type handle is really an array of ref type
    assert(arrayElemType == CORINFO_TYPE_CLASS);

    // Check for exactly object[]
    if (arrayIsExact && (arrayElementHandle == impGetObjectClass()))
    {
        JITDUMP("\nstelem to (exact) object[]: skipping covariant store check\n");
        return true;
    }

    // Check for T[] with T exact.
    if (!impIsClassExact(arrayElementHandle))
    {
        return false;
    }

    bool                 valueIsExact   = false;
    bool                 valueIsNonNull = false;
    CORINFO_CLASS_HANDLE valueHandle    = gtGetClassHandle(value, &valueIsExact, &valueIsNonNull);

    if (valueHandle == arrayElementHandle)
    {
        JITDUMP("\nstelem to T[] with T exact: skipping covariant store check\n");
        return true;
    }

    return false;
}
