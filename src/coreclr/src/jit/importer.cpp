// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef DEBUG
    impTreeList        = nullptr;
    impTreeLast        = nullptr;
    impInlinedCodeSize = 0;
#endif
}

/*****************************************************************************
 *
 *  Pushes the given tree on the stack.
 */

void Compiler::impPushOnStack(GenTreePtr tree, typeInfo ti)
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
        assert(ti.IsDead() || ti.IsByRef() && (tree->TypeGet() == TYP_I_IMPL || tree->TypeGet() == TYP_BYREF) ||
               ti.IsUnboxedGenericTypeVar() && tree->TypeGet() == TYP_REF ||
               ti.IsObjRef() && tree->TypeGet() == TYP_REF || ti.IsMethod() && tree->TypeGet() == TYP_I_IMPL ||
               ti.IsType(TI_STRUCT) && tree->TypeGet() != TYP_REF ||
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
    // Remember that the code is not verifiable
    // Note that the method may yet pass canSkipMethodVerification(),
    // and so the presence of unverifiable code may not be an issue.
    tiIsVerifiableCode = FALSE;

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

    if (verNeedsVerification() || compIsForImportOnly())
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
bool Compiler::impILConsumesAddr(const BYTE* codeAddr, CORINFO_METHOD_HANDLE fncHandle, CORINFO_MODULE_HANDLE scpHandle)
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

            CORINFO_CLASS_HANDLE clsHnd;
            var_types lclTyp = JITtype2varType(info.compCompHnd->getFieldType(resolvedToken.hField, &clsHnd));

            // Preserve 'small' int types
            if (lclTyp > TYP_INT)
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
static bool impValidSpilledStackEntry(GenTreePtr tree)
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
                GenTreePtr tree   = verCurrentState.esStack[level].val;

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

/*****************************************************************************
 *
 *  Get the tree list started for a new basic block.
 */
inline void Compiler::impBeginTreeList()
{
    assert(impTreeList == nullptr && impTreeLast == nullptr);

    impTreeList = impTreeLast = new (this, GT_BEG_STMTS) GenTree(GT_BEG_STMTS, TYP_VOID);
}

/*****************************************************************************
 *
 *  Store the given start and end stmt in the given basic block. This is
 *  mostly called by impEndTreeList(BasicBlock *block). It is called
 *  directly only for handling CEE_LEAVEs out of finally-protected try's.
 */

inline void Compiler::impEndTreeList(BasicBlock* block, GenTreePtr firstStmt, GenTreePtr lastStmt)
{
    assert(firstStmt->gtOper == GT_STMT);
    assert(lastStmt->gtOper == GT_STMT);

    /* Make the list circular, so that we can easily walk it backwards */

    firstStmt->gtPrev = lastStmt;

    /* Store the tree list in the basic block */

    block->bbTreeList = firstStmt;

    /* The block should not already be marked as imported */
    assert((block->bbFlags & BBF_IMPORTED) == 0);

    block->bbFlags |= BBF_IMPORTED;
}

/*****************************************************************************
 *
 *  Store the current tree list in the given basic block.
 */

inline void Compiler::impEndTreeList(BasicBlock* block)
{
    assert(impTreeList->gtOper == GT_BEG_STMTS);

    GenTreePtr firstTree = impTreeList->gtNext;

    if (!firstTree)
    {
        /* The block should not already be marked as imported */
        assert((block->bbFlags & BBF_IMPORTED) == 0);

        // Empty block. Just mark it as imported
        block->bbFlags |= BBF_IMPORTED;
    }
    else
    {
        // Ignore the GT_BEG_STMTS
        assert(firstTree->gtPrev == impTreeList);

        impEndTreeList(block, firstTree, impTreeLast);
    }

#ifdef DEBUG
    if (impLastILoffsStmt != nullptr)
    {
        impLastILoffsStmt->gtStmt.gtStmtLastILoffs = compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs;
        impLastILoffsStmt                          = nullptr;
    }

    impTreeList = impTreeLast = nullptr;
#endif
}

/*****************************************************************************
 *
 *  Check that storing the given tree doesnt mess up the semantic order. Note
 *  that this has only limited value as we can only check [0..chkLevel).
 */

inline void Compiler::impAppendStmtCheck(GenTreePtr stmt, unsigned chkLevel)
{
#ifndef DEBUG
    return;
#else
    assert(stmt->gtOper == GT_STMT);

    if (chkLevel == (unsigned)CHECK_SPILL_ALL)
    {
        chkLevel = verCurrentState.esStackDepth;
    }

    if (verCurrentState.esStackDepth == 0 || chkLevel == 0 || chkLevel == (unsigned)CHECK_SPILL_NONE)
    {
        return;
    }

    GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

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

        if (tree->gtOp.gtOp1->gtOper == GT_LCL_VAR)
        {
            unsigned lclNum = tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
            for (unsigned level = 0; level < chkLevel; level++)
            {
                assert(!gtHasRef(verCurrentState.esStack[level].val, lclNum, false));
                assert(!lvaTable[lclNum].lvAddrExposed ||
                       (verCurrentState.esStack[level].val->gtFlags & GTF_SIDE_EFFECT) == 0);
            }
        }

        // If the access may be to global memory, all side effects have to be spilled.

        else if (tree->gtOp.gtOp1->gtFlags & GTF_GLOB_REF)
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
 *  Append the given GT_STMT node to the current block's tree list.
 *  [0..chkLevel) is the portion of the stack which we will check for
 *    interference with stmt and spill if needed.
 */

inline void Compiler::impAppendStmt(GenTreePtr stmt, unsigned chkLevel)
{
    assert(stmt->gtOper == GT_STMT);
    noway_assert(impTreeLast != nullptr);

    /* If the statement being appended has any side-effects, check the stack
       to see if anything needs to be spilled to preserve correct ordering. */

    GenTreePtr expr  = stmt->gtStmt.gtStmtExpr;
    unsigned   flags = expr->gtFlags & GTF_GLOB_EFFECT;

    // Assignment to (unaliased) locals don't count as a side-effect as
    // we handle them specially using impSpillLclRefs(). Temp locals should
    // be fine too.

    if ((expr->gtOper == GT_ASG) && (expr->gtOp.gtOp1->gtOper == GT_LCL_VAR) &&
        !(expr->gtOp.gtOp1->gtFlags & GTF_GLOB_REF) && !gtHasLocalsWithAddrOp(expr->gtOp.gtOp2))
    {
        unsigned op2Flags = expr->gtOp.gtOp2->gtFlags & GTF_GLOB_EFFECT;
        assert(flags == (op2Flags | GTF_ASG));
        flags = op2Flags;
    }

    if (chkLevel == (unsigned)CHECK_SPILL_ALL)
    {
        chkLevel = verCurrentState.esStackDepth;
    }

    if (chkLevel && chkLevel != (unsigned)CHECK_SPILL_NONE)
    {
        assert(chkLevel <= verCurrentState.esStackDepth);

        if (flags)
        {
            // If there is a call, we have to spill global refs
            bool spillGlobEffects = (flags & GTF_CALL) ? true : false;

            if (expr->gtOper == GT_ASG)
            {
                GenTree* lhs = expr->gtGetOp1();
                // If we are assigning to a global ref, we have to spill global refs on stack.
                // TODO-1stClassStructs: Previously, spillGlobEffects was set to true for
                // GT_INITBLK and GT_COPYBLK, but this is overly conservative, and should be
                // revisited. (Note that it was NOT set to true for GT_COPYOBJ.)
                if (!expr->OperIsBlkOp())
                {
                    // If we are assigning to a global ref, we have to spill global refs on stack
                    if ((lhs->gtFlags & GTF_GLOB_REF) != 0)
                    {
                        spillGlobEffects = true;
                    }
                }
                else if ((lhs->OperIsBlk() && !lhs->AsBlk()->HasGCPtr()) ||
                         ((lhs->OperGet() == GT_LCL_VAR) &&
                          (lvaTable[lhs->AsLclVarCommon()->gtLclNum].lvStructGcCount == 0)))
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

    /* Point 'prev' at the previous node, so that we can walk backwards */

    stmt->gtPrev = impTreeLast;

    /* Append the expression statement to the list */

    impTreeLast->gtNext = stmt;
    impTreeLast         = stmt;

#ifdef FEATURE_SIMD
    impMarkContiguousSIMDFieldAssignments(stmt);
#endif

    /* Once we set impCurStmtOffs in an appended tree, we are ready to
       report the following offsets. So reset impCurStmtOffs */

    if (impTreeLast->gtStmt.gtStmtILoffsx == impCurStmtOffs)
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
        gtDispTree(stmt);
    }
#endif
}

/*****************************************************************************
 *
 *  Insert the given GT_STMT "stmt" before GT_STMT "stmtBefore"
 */

inline void Compiler::impInsertStmtBefore(GenTreePtr stmt, GenTreePtr stmtBefore)
{
    assert(stmt->gtOper == GT_STMT);
    assert(stmtBefore->gtOper == GT_STMT);

    GenTreePtr stmtPrev = stmtBefore->gtPrev;
    stmt->gtPrev        = stmtPrev;
    stmt->gtNext        = stmtBefore;
    stmtPrev->gtNext    = stmt;
    stmtBefore->gtPrev  = stmt;
}

/*****************************************************************************
 *
 *  Append the given expression tree to the current block's tree list.
 *  Return the newly created statement.
 */

GenTreePtr Compiler::impAppendTree(GenTreePtr tree, unsigned chkLevel, IL_OFFSETX offset)
{
    assert(tree);

    /* Allocate an 'expression statement' node */

    GenTreePtr expr = gtNewStmt(tree, offset);

    /* Append the statement to the current block's stmt list */

    impAppendStmt(expr, chkLevel);

    return expr;
}

/*****************************************************************************
 *
 *  Insert the given exression tree before GT_STMT "stmtBefore"
 */

void Compiler::impInsertTreeBefore(GenTreePtr tree, IL_OFFSETX offset, GenTreePtr stmtBefore)
{
    assert(stmtBefore->gtOper == GT_STMT);

    /* Allocate an 'expression statement' node */

    GenTreePtr expr = gtNewStmt(tree, offset);

    /* Append the statement to the current block's stmt list */

    impInsertStmtBefore(expr, stmtBefore);
}

/*****************************************************************************
 *
 *  Append an assignment of the given value to a temp to the current tree list.
 *  curLevel is the stack level for which the spill to the temp is being done.
 */

void Compiler::impAssignTempGen(unsigned    tmp,
                                GenTreePtr  val,
                                unsigned    curLevel,
                                GenTreePtr* pAfterStmt, /* = NULL */
                                IL_OFFSETX  ilOffset,   /* = BAD_IL_OFFSET */
                                BasicBlock* block       /* = NULL */
                                )
{
    GenTreePtr asg = gtNewTempAssign(tmp, val);

    if (!asg->IsNothingNode())
    {
        if (pAfterStmt)
        {
            GenTreePtr asgStmt = gtNewStmt(asg, ilOffset);
            *pAfterStmt        = fgInsertStmtAfter(block, *pAfterStmt, asgStmt);
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
                                GenTreePtr           val,
                                CORINFO_CLASS_HANDLE structType,
                                unsigned             curLevel,
                                GenTreePtr*          pAfterStmt, /* = NULL */
                                IL_OFFSETX           ilOffset,   /* = BAD_IL_OFFSET */
                                BasicBlock*          block       /* = NULL */
                                )
{
    GenTreePtr asg;

    if (varTypeIsStruct(val))
    {
        assert(tmpNum < lvaCount);
        assert(structType != NO_CLASS_HANDLE);

        // if the method is non-verifiable the assert is not true
        // so at least ignore it in the case when verification is turned on
        // since any block that tries to use the temp would have failed verification.
        var_types varType = lvaTable[tmpNum].lvType;
        assert(tiVerificationNeeded || varType == TYP_UNDEF || varTypeIsStruct(varType));
        lvaSetStruct(tmpNum, structType, false);

        // Now, set the type of the struct value. Note that lvaSetStruct may modify the type
        // of the lclVar to a specialized type (e.g. TYP_SIMD), based on the handle (structType)
        // that has been passed in for the value being assigned to the temp, in which case we
        // need to set 'val' to that same type.
        // Note also that if we always normalized the types of any node that might be a struct
        // type, this would not be necessary - but that requires additional JIT/EE interface
        // calls that may not actually be required - e.g. if we only access a field of a struct.

        val->gtType = lvaTable[tmpNum].lvType;

        GenTreePtr dst = gtNewLclvNode(tmpNum, val->gtType);
        asg            = impAssignStruct(dst, val, structType, curLevel, pAfterStmt, block);
    }
    else
    {
        asg = gtNewTempAssign(tmpNum, val);
    }

    if (!asg->IsNothingNode())
    {
        if (pAfterStmt)
        {
            GenTreePtr asgStmt = gtNewStmt(asg, ilOffset);
            *pAfterStmt        = fgInsertStmtAfter(block, *pAfterStmt, asgStmt);
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

GenTreeArgList* Compiler::impPopList(unsigned          count,
                                     unsigned*         flagsPtr,
                                     CORINFO_SIG_INFO* sig,
                                     GenTreeArgList*   prefixTree)
{
    assert(sig == nullptr || count == sig->numArgs);

    unsigned             flags = 0;
    CORINFO_CLASS_HANDLE structType;
    GenTreeArgList*      treeList;

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
    {
        treeList = nullptr;
    }
    else
    { // ARG_ORDER_L2R
        treeList = prefixTree;
    }

    while (count--)
    {
        StackEntry se   = impPopStack();
        typeInfo   ti   = se.seTypeInfo;
        GenTreePtr temp = se.val;

        if (varTypeIsStruct(temp))
        {
            // Morph trees that aren't already OBJs or MKREFANY to be OBJs
            assert(ti.IsType(TI_STRUCT));
            structType = ti.GetClassHandleForValueClass();
            temp       = impNormStructVal(temp, structType, (unsigned)CHECK_SPILL_ALL);
        }

        /* NOTE: we defer bashing the type for I_IMPL to fgMorphArgs */
        flags |= temp->gtFlags;
        treeList = gtNewListNode(temp, treeList);
    }

    *flagsPtr = flags;

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

        CORINFO_ARG_LIST_HANDLE argLst = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        CORINFO_CLASS_HANDLE    argRealClass;
        GenTreeArgList*         args;
        unsigned                sigSize;

        for (args = treeList, count = sig->numArgs; count > 0; args = args->Rest(), count--)
        {
            PREFIX_ASSUME(args != nullptr);

            CorInfoType corType = strip(info.compCompHnd->getArgType(sig, argLst, &argClass));

            // insert implied casts (from float to double or double to float)

            if (corType == CORINFO_TYPE_DOUBLE && args->Current()->TypeGet() == TYP_FLOAT)
            {
                args->Current() = gtNewCastNode(TYP_DOUBLE, args->Current(), TYP_DOUBLE);
            }
            else if (corType == CORINFO_TYPE_FLOAT && args->Current()->TypeGet() == TYP_DOUBLE)
            {
                args->Current() = gtNewCastNode(TYP_FLOAT, args->Current(), TYP_FLOAT);
            }

            // insert any widening or narrowing casts for backwards compatibility

            args->Current() = impImplicitIorI4Cast(args->Current(), JITtype2varType(corType));

            if (corType != CORINFO_TYPE_CLASS && corType != CORINFO_TYPE_BYREF && corType != CORINFO_TYPE_PTR &&
                corType != CORINFO_TYPE_VAR && (argRealClass = info.compCompHnd->getArgClass(sig, argLst)) != nullptr)
            {
                // Everett MC++ could generate IL with a mismatched valuetypes. It used to work with Everett JIT,
                // but it stopped working in Whidbey when we have started passing simple valuetypes as underlying
                // primitive types.
                // We will try to adjust for this case here to avoid breaking customers code (see VSW 485789 for
                // details).
                if (corType == CORINFO_TYPE_VALUECLASS && !varTypeIsStruct(args->Current()))
                {
                    args->Current() = impNormStructVal(args->Current(), argRealClass, (unsigned)CHECK_SPILL_ALL, true);
                }

                // Make sure that all valuetypes (including enums) that we push are loaded.
                // This is to guarantee that if a GC is triggered from the prestub of this methods,
                // all valuetypes in the method signature are already loaded.
                // We need to be able to find the size of the valuetypes, but we cannot
                // do a class-load from within GC.
                info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(argRealClass);
            }

            argLst = info.compCompHnd->getArgNext(argLst);
        }
    }

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
    {
        // Prepend the prefixTree

        // Simple in-place reversal to place treeList
        // at the end of a reversed prefixTree
        while (prefixTree != nullptr)
        {
            GenTreeArgList* next = prefixTree->Rest();
            prefixTree->Rest()   = treeList;
            treeList             = prefixTree;
            prefixTree           = next;
        }
    }
    return treeList;
}

/*****************************************************************************
 *
 *  Pop the given number of values from the stack in reverse order (STDCALL/CDECL etc.)
 *  The first "skipReverseCount" items are not reversed.
 */

GenTreeArgList* Compiler::impPopRevList(unsigned          count,
                                        unsigned*         flagsPtr,
                                        CORINFO_SIG_INFO* sig,
                                        unsigned          skipReverseCount)

{
    assert(skipReverseCount <= count);

    GenTreeArgList* list = impPopList(count, flagsPtr, sig);

    // reverse the list
    if (list == nullptr || skipReverseCount == count)
    {
        return list;
    }

    GenTreeArgList* ptr          = nullptr; // Initialized to the first node that needs to be reversed
    GenTreeArgList* lastSkipNode = nullptr; // Will be set to the last node that does not need to be reversed

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
            lastSkipNode = lastSkipNode->Rest();
        }

        PREFIX_ASSUME(lastSkipNode != nullptr);
        ptr = lastSkipNode->Rest();
    }

    GenTreeArgList* reversedList = nullptr;

    do
    {
        GenTreeArgList* tmp = ptr->Rest();
        ptr->Rest()         = reversedList;
        reversedList        = ptr;
        ptr                 = tmp;
    } while (ptr != nullptr);

    if (skipReverseCount)
    {
        lastSkipNode->Rest() = reversedList;
        return list;
    }
    else
    {
        return reversedList;
    }
}

/*****************************************************************************
   Assign (copy) the structure from 'src' to 'dest'.  The structure is a value
   class of type 'clsHnd'.  It returns the tree that should be appended to the
   statement list that represents the assignment.
   Temp assignments may be appended to impTreeList if spilling is necessary.
   curLevel is the stack level for which a spill may be being done.
 */

GenTreePtr Compiler::impAssignStruct(GenTreePtr           dest,
                                     GenTreePtr           src,
                                     CORINFO_CLASS_HANDLE structHnd,
                                     unsigned             curLevel,
                                     GenTreePtr*          pAfterStmt, /* = NULL */
                                     BasicBlock*          block       /* = NULL */
                                     )
{
    assert(varTypeIsStruct(dest));

    while (dest->gtOper == GT_COMMA)
    {
        assert(varTypeIsStruct(dest->gtOp.gtOp2)); // Second thing is the struct

        // Append all the op1 of GT_COMMA trees before we evaluate op2 of the GT_COMMA tree.
        if (pAfterStmt)
        {
            *pAfterStmt = fgInsertStmtAfter(block, *pAfterStmt, gtNewStmt(dest->gtOp.gtOp1, impCurStmtOffs));
        }
        else
        {
            impAppendTree(dest->gtOp.gtOp1, curLevel, impCurStmtOffs); // do the side effect
        }

        // set dest to the second thing
        dest = dest->gtOp.gtOp2;
    }

    assert(dest->gtOper == GT_LCL_VAR || dest->gtOper == GT_RETURN || dest->gtOper == GT_FIELD ||
           dest->gtOper == GT_IND || dest->gtOper == GT_OBJ || dest->gtOper == GT_INDEX);

    if (dest->OperGet() == GT_LCL_VAR && src->OperGet() == GT_LCL_VAR &&
        src->gtLclVarCommon.gtLclNum == dest->gtLclVarCommon.gtLclNum)
    {
        // Make this a NOP
        return gtNewNothingNode();
    }

    // TODO-1stClassStructs: Avoid creating an address if it is not needed,
    // or re-creating a Blk node if it is.
    GenTreePtr destAddr;

    if (dest->gtOper == GT_IND || dest->OperIsBlk())
    {
        destAddr = dest->gtOp.gtOp1;
    }
    else
    {
        destAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);
    }

    return (impAssignStructPtr(destAddr, src, structHnd, curLevel, pAfterStmt, block));
}

/*****************************************************************************/

GenTreePtr Compiler::impAssignStructPtr(GenTreePtr           destAddr,
                                        GenTreePtr           src,
                                        CORINFO_CLASS_HANDLE structHnd,
                                        unsigned             curLevel,
                                        GenTreePtr*          pAfterStmt, /* = NULL */
                                        BasicBlock*          block       /* = NULL */
                                        )
{
    var_types  destType;
    GenTreePtr dest      = nullptr;
    unsigned   destFlags = 0;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    assert(varTypeIsStruct(src) || (src->gtOper == GT_ADDR && src->TypeGet() == TYP_BYREF));
    // TODO-ARM-BUG: Does ARM need this?
    // TODO-ARM64-BUG: Does ARM64 need this?
    assert(src->gtOper == GT_LCL_VAR || src->gtOper == GT_FIELD || src->gtOper == GT_IND || src->gtOper == GT_OBJ ||
           src->gtOper == GT_CALL || src->gtOper == GT_MKREFANY || src->gtOper == GT_RET_EXPR ||
           src->gtOper == GT_COMMA || src->gtOper == GT_ADDR ||
           (src->TypeGet() != TYP_STRUCT && (GenTree::OperIsSIMD(src->gtOper) || src->gtOper == GT_LCL_FLD)));
#else  // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    assert(varTypeIsStruct(src));

    assert(src->gtOper == GT_LCL_VAR || src->gtOper == GT_FIELD || src->gtOper == GT_IND || src->gtOper == GT_OBJ ||
           src->gtOper == GT_CALL || src->gtOper == GT_MKREFANY || src->gtOper == GT_RET_EXPR ||
           src->gtOper == GT_COMMA ||
           (src->TypeGet() != TYP_STRUCT && (GenTree::OperIsSIMD(src->gtOper) || src->gtOper == GT_LCL_FLD)));
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    if (destAddr->OperGet() == GT_ADDR)
    {
        GenTree* destNode = destAddr->gtGetOp1();
        // If the actual destination is a local (for non-LEGACY_BACKEND), or already a block node, or is a node that
        // will be morphed, don't insert an OBJ(ADDR).
        if (destNode->gtOper == GT_INDEX || destNode->OperIsBlk()
#ifndef LEGACY_BACKEND
            || ((destNode->OperGet() == GT_LCL_VAR) && (destNode->TypeGet() == src->TypeGet()))
#endif // !LEGACY_BACKEND
                )
        {
            dest = destNode;
        }
        destType = destNode->TypeGet();
    }
    else
    {
        destType = src->TypeGet();
    }

    var_types asgType = src->TypeGet();

    if (src->gtOper == GT_CALL)
    {
        if (src->AsCall()->TreatAsHasRetBufArg(this))
        {
            // Case of call returning a struct via hidden retbuf arg

            // insert the return value buffer into the argument list as first byref parameter
            src->gtCall.gtCallArgs = gtNewListNode(destAddr, src->gtCall.gtCallArgs);

            // now returns void, not a struct
            src->gtType = TYP_VOID;

            // return the morphed call node
            return src;
        }
        else
        {
            // Case of call returning a struct in one or more registers.

            var_types returnType = (var_types)src->gtCall.gtReturnType;

            // We won't use a return buffer, so change the type of src->gtType to 'returnType'
            src->gtType = genActualType(returnType);

            // First we try to change this to "LclVar/LclFld = call"
            //
            if ((destAddr->gtOper == GT_ADDR) && (destAddr->gtOp.gtOp1->gtOper == GT_LCL_VAR))
            {
                // If it is a multi-reg struct return, don't change the oper to GT_LCL_FLD.
                // That is, the IR will be of the form lclVar = call for multi-reg return
                //
                GenTreePtr lcl = destAddr->gtOp.gtOp1;
                if (src->AsCall()->HasMultiRegRetVal())
                {
                    // Mark the struct LclVar as used in a MultiReg return context
                    //  which currently makes it non promotable.
                    // TODO-1stClassStructs: Eliminate this pessimization when we can more generally
                    // handle multireg returns.
                    lcl->gtFlags |= GTF_DONT_CSE;
                    lvaTable[lcl->gtLclVarCommon.gtLclNum].lvIsMultiRegRet = true;
                }
                else // The call result is not a multireg return
                {
                    // We change this to a GT_LCL_FLD (from a GT_ADDR of a GT_LCL_VAR)
                    lcl->ChangeOper(GT_LCL_FLD);
                    fgLclFldAssign(lcl->gtLclVarCommon.gtLclNum);
                }

                lcl->gtType = src->gtType;
                asgType     = src->gtType;
                dest        = lcl;

#if defined(_TARGET_ARM_)
                // TODO-Cleanup: This should have been taken care of in the above HasMultiRegRetVal() case,
                // but that method has not been updadted to include ARM.
                impMarkLclDstNotPromotable(lcl->gtLclVarCommon.gtLclNum, src, structHnd);
                lcl->gtFlags |= GTF_DONT_CSE;
#elif defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                // Not allowed for FEATURE_CORCLR which is the only SKU available for System V OSs.
                assert(!src->gtCall.IsVarargs() && "varargs not allowed for System V OSs.");

                // Make the struct non promotable. The eightbytes could contain multiple fields.
                // TODO-1stClassStructs: Eliminate this pessimization when we can more generally
                // handle multireg returns.
                // TODO-Cleanup: Why is this needed here? This seems that it will set this even for
                // non-multireg returns.
                lcl->gtFlags |= GTF_DONT_CSE;
                lvaTable[lcl->gtLclVarCommon.gtLclNum].lvIsMultiRegRet = true;
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
        GenTreeCall* call = src->gtRetExpr.gtInlineCandidate->AsCall();
        noway_assert(call->gtOper == GT_CALL);

        if (call->HasRetBufArg())
        {
            // insert the return value buffer into the argument list as first byref parameter
            call->gtCallArgs = gtNewListNode(destAddr, call->gtCallArgs);

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
            //
            var_types returnType = (var_types)call->gtReturnType;

            // We won't need a return buffer
            asgType      = returnType;
            src->gtType  = genActualType(returnType);
            call->gtType = src->gtType;

            // If we've changed the type, and it no longer matches a local destination,
            // we must use an indirection.
            if ((dest != nullptr) && (dest->OperGet() == GT_LCL_VAR) && (dest->TypeGet() != asgType))
            {
                dest = nullptr;
            }

            // !!! The destination could be on stack. !!!
            // This flag will let us choose the correct write barrier.
            destFlags = GTF_IND_TGTANYWHERE;
        }
    }
    else if (src->OperIsBlk())
    {
        asgType = impNormStructType(structHnd);
        if (src->gtOper == GT_OBJ)
        {
            assert(src->gtObj.gtClass == structHnd);
        }
    }
    else if (src->gtOper == GT_INDEX)
    {
        asgType = impNormStructType(structHnd);
        assert(src->gtIndex.gtStructElemClass == structHnd);
    }
    else if (src->gtOper == GT_MKREFANY)
    {
        // Since we are assigning the result of a GT_MKREFANY,
        // "destAddr" must point to a refany.

        GenTreePtr destAddrClone;
        destAddr =
            impCloneExpr(destAddr, &destAddrClone, structHnd, curLevel, pAfterStmt DEBUGARG("MKREFANY assignment"));

        assert(offsetof(CORINFO_RefAny, dataPtr) == 0);
        assert(destAddr->gtType == TYP_I_IMPL || destAddr->gtType == TYP_BYREF);
        GetZeroOffsetFieldMap()->Set(destAddr, GetFieldSeqStore()->CreateSingleton(GetRefanyDataField()));
        GenTreePtr     ptrSlot         = gtNewOperNode(GT_IND, TYP_I_IMPL, destAddr);
        GenTreeIntCon* typeFieldOffset = gtNewIconNode(offsetof(CORINFO_RefAny, type), TYP_I_IMPL);
        typeFieldOffset->gtFieldSeq    = GetFieldSeqStore()->CreateSingleton(GetRefanyTypeField());
        GenTreePtr typeSlot =
            gtNewOperNode(GT_IND, TYP_I_IMPL, gtNewOperNode(GT_ADD, destAddr->gtType, destAddrClone, typeFieldOffset));

        // append the assign of the pointer value
        GenTreePtr asg = gtNewAssignNode(ptrSlot, src->gtOp.gtOp1);
        if (pAfterStmt)
        {
            *pAfterStmt = fgInsertStmtAfter(block, *pAfterStmt, gtNewStmt(asg, impCurStmtOffs));
        }
        else
        {
            impAppendTree(asg, curLevel, impCurStmtOffs);
        }

        // return the assign of the type value, to be appended
        return gtNewAssignNode(typeSlot, src->gtOp.gtOp2);
    }
    else if (src->gtOper == GT_COMMA)
    {
        // The second thing is the struct or its address.
        assert(varTypeIsStruct(src->gtOp.gtOp2) || src->gtOp.gtOp2->gtType == TYP_BYREF);
        if (pAfterStmt)
        {
            *pAfterStmt = fgInsertStmtAfter(block, *pAfterStmt, gtNewStmt(src->gtOp.gtOp1, impCurStmtOffs));
        }
        else
        {
            impAppendTree(src->gtOp.gtOp1, curLevel, impCurStmtOffs); // do the side effect
        }

        // Evaluate the second thing using recursion.
        return impAssignStructPtr(destAddr, src->gtOp.gtOp2, structHnd, curLevel, pAfterStmt, block);
    }
    else if (src->IsLocal())
    {
        asgType = src->TypeGet();
    }
    else if (asgType == TYP_STRUCT)
    {
        asgType     = impNormStructType(structHnd);
        src->gtType = asgType;
#ifdef LEGACY_BACKEND
        if (asgType == TYP_STRUCT)
        {
            GenTree* srcAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, src);
            src              = gtNewOperNode(GT_IND, TYP_STRUCT, srcAddr);
        }
#endif
    }
    if (dest == nullptr)
    {
        // TODO-1stClassStructs: We shouldn't really need a block node as the destination
        // if this is a known struct type.
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
        else if (varTypeIsStruct(asgType))
        {
            dest = new (this, GT_BLK) GenTreeBlk(GT_BLK, asgType, destAddr, genTypeSize(asgType));
        }
        else
        {
            dest = gtNewOperNode(GT_IND, asgType, destAddr);
        }
    }
    else
    {
        dest->gtType = asgType;
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

GenTreePtr Compiler::impGetStructAddr(GenTreePtr           structVal,
                                      CORINFO_CLASS_HANDLE structHnd,
                                      unsigned             curLevel,
                                      bool                 willDeref)
{
    assert(varTypeIsStruct(structVal) || eeIsValueClass(structHnd));

    var_types type = structVal->TypeGet();

    genTreeOps oper = structVal->gtOper;

    if (oper == GT_OBJ && willDeref)
    {
        assert(structVal->gtObj.gtClass == structHnd);
        return (structVal->gtObj.Addr());
    }
    else if (oper == GT_CALL || oper == GT_RET_EXPR || oper == GT_OBJ || oper == GT_MKREFANY)
    {
        unsigned tmpNum = lvaGrabTemp(true DEBUGARG("struct address for call/obj"));

        impAssignTempGen(tmpNum, structVal, structHnd, curLevel);

        // The 'return value' is now the temp itself

        type            = genActualType(lvaTable[tmpNum].TypeGet());
        GenTreePtr temp = gtNewLclvNode(tmpNum, type);
        temp            = gtNewOperNode(GT_ADDR, TYP_BYREF, temp);
        return temp;
    }
    else if (oper == GT_COMMA)
    {
        assert(structVal->gtOp.gtOp2->gtType == type); // Second thing is the struct

        GenTreePtr oldTreeLast = impTreeLast;
        structVal->gtOp.gtOp2  = impGetStructAddr(structVal->gtOp.gtOp2, structHnd, curLevel, willDeref);
        structVal->gtType      = TYP_BYREF;

        if (oldTreeLast != impTreeLast)
        {
            // Some temp assignment statement was placed on the statement list
            // for Op2, but that would be out of order with op1, so we need to
            // spill op1 onto the statement list after whatever was last
            // before we recursed on Op2 (i.e. before whatever Op2 appended).
            impInsertTreeBefore(structVal->gtOp.gtOp1, impCurStmtOffs, oldTreeLast->gtNext);
            structVal->gtOp.gtOp1 = gtNewNothingNode();
        }

        return (structVal);
    }

    return (gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
}

//------------------------------------------------------------------------
// impNormStructType: Given a (known to be) struct class handle structHnd, normalize its type,
//                    and optionally determine the GC layout of the struct.
//
// Arguments:
//    structHnd       - The class handle for the struct type of interest.
//    gcLayout        - (optional, default nullptr) - a BYTE pointer, allocated by the caller,
//                      into which the gcLayout will be written.
//    pNumGCVars      - (optional, default nullptr) - if non-null, a pointer to an unsigned,
//                      which will be set to the number of GC fields in the struct.
//    pSimdBaseType   - (optional, default nullptr) - if non-null, and the struct is a SIMD
//                      type, set to the SIMD base type
//
// Return Value:
//    The JIT type for the struct (e.g. TYP_STRUCT, or TYP_SIMD*).
//    The gcLayout will be returned using the pointers provided by the caller, if non-null.
//    It may also modify the compFloatingPointUsed flag if the type is a SIMD type.
//
// Assumptions:
//    The caller must set gcLayout to nullptr OR ensure that it is large enough
//    (see ICorStaticInfo::getClassGClayout in corinfo.h).
//
// Notes:
//    Normalizing the type involves examining the struct type to determine if it should
//    be modified to one that is handled specially by the JIT, possibly being a candidate
//    for full enregistration, e.g. TYP_SIMD16.

var_types Compiler::impNormStructType(CORINFO_CLASS_HANDLE structHnd,
                                      BYTE*                gcLayout,
                                      unsigned*            pNumGCVars,
                                      var_types*           pSimdBaseType)
{
    assert(structHnd != NO_CLASS_HANDLE);

    const DWORD structFlags = info.compCompHnd->getClassAttribs(structHnd);
    var_types   structType  = TYP_STRUCT;

    // On coreclr the check for GC includes a "may" to account for the special
    // ByRef like span structs.  The added check for "CONTAINS_STACK_PTR" is the particular bit.
    // When this is set the struct will contain a ByRef that could be a GC pointer or a native
    // pointer.
    const bool mayContainGCPtrs =
        ((structFlags & CORINFO_FLG_CONTAINS_STACK_PTR) != 0 || ((structFlags & CORINFO_FLG_CONTAINS_GC_PTR) != 0));

#ifdef FEATURE_SIMD
    // Check to see if this is a SIMD type.
    if (featureSIMD && !mayContainGCPtrs)
    {
        unsigned originalSize = info.compCompHnd->getClassSize(structHnd);

        if ((originalSize >= minSIMDStructBytes()) && (originalSize <= maxSIMDStructBytes()))
        {
            unsigned int sizeBytes;
            var_types    simdBaseType = getBaseTypeAndSizeOfSIMDType(structHnd, &sizeBytes);
            if (simdBaseType != TYP_UNKNOWN)
            {
                assert(sizeBytes == originalSize);
                structType = getSIMDTypeForSize(sizeBytes);
                if (pSimdBaseType != nullptr)
                {
                    *pSimdBaseType = simdBaseType;
                }
                // Also indicate that we use floating point registers.
                compFloatingPointUsed = true;
            }
        }
    }
#endif // FEATURE_SIMD

    // Fetch GC layout info if requested
    if (gcLayout != nullptr)
    {
        unsigned numGCVars = info.compCompHnd->getClassGClayout(structHnd, gcLayout);

        // Verify that the quick test up above via the class attributes gave a
        // safe view of the type's GCness.
        //
        // Note there are cases where mayContainGCPtrs is true but getClassGClayout
        // does not report any gc fields.

        assert(mayContainGCPtrs || (numGCVars == 0));

        if (pNumGCVars != nullptr)
        {
            *pNumGCVars = numGCVars;
        }
    }
    else
    {
        // Can't safely ask for number of GC pointers without also
        // asking for layout.
        assert(pNumGCVars == nullptr);
    }

    return structType;
}

//****************************************************************************
//  Given TYP_STRUCT value 'structVal', make sure it is 'canonical', that is
//  it is either an OBJ or a MKREFANY node, or a node (e.g. GT_INDEX) that will be morphed.
//
GenTreePtr Compiler::impNormStructVal(GenTreePtr           structVal,
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
            structVal->gtCall.gtRetClsHnd = structHnd;
            makeTemp                      = true;
            break;

        case GT_RET_EXPR:
            structVal->gtRetExpr.gtRetClsHnd = structHnd;
            makeTemp                         = true;
            break;

        case GT_ARGPLACE:
            structVal->gtArgPlace.gtArgPlaceClsHnd = structHnd;
            break;

        case GT_INDEX:
            // This will be transformed to an OBJ later.
            alreadyNormalized                    = true;
            structVal->gtIndex.gtStructElemClass = structHnd;
            structVal->gtIndex.gtIndElemSize     = info.compCompHnd->getClassSize(structHnd);
            break;

        case GT_FIELD:
            // Wrap it in a GT_OBJ.
            structVal->gtType = structType;
            structVal         = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
            break;

        case GT_LCL_VAR:
        case GT_LCL_FLD:
            structLcl = structVal->AsLclVarCommon();
            // Wrap it in a GT_OBJ.
            structVal = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
            __fallthrough;

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

        case GT_COMMA:
        {
            // The second thing could either be a block node or a GT_SIMD or a GT_COMMA node.
            GenTree* blockNode = structVal->gtOp.gtOp2;
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
                    blockNode = blockNode->gtOp.gtOp2;
                } while (blockNode->OperGet() == GT_COMMA);
            }

#ifdef FEATURE_SIMD
            if (blockNode->OperGet() == GT_SIMD)
            {
                parent->gtOp.gtOp2 = impNormStructVal(blockNode, structHnd, curLevel, forceNormalization);
                alreadyNormalized  = true;
            }
            else
#endif
            {
                assert(blockNode->OperIsBlk());

                // Sink the GT_COMMA below the blockNode addr.
                // That is GT_COMMA(op1, op2=blockNode) is tranformed into
                // blockNode(GT_COMMA(TYP_BYREF, op1, op2's op1)).
                //
                // In case of a chained GT_COMMA case, we sink the last
                // GT_COMMA below the blockNode addr.
                GenTree* blockNodeAddr = blockNode->gtOp.gtOp1;
                assert(blockNodeAddr->gtType == TYP_BYREF);
                GenTree* commaNode    = parent;
                commaNode->gtType     = TYP_BYREF;
                commaNode->gtOp.gtOp2 = blockNodeAddr;
                blockNode->gtOp.gtOp1 = commaNode;
                if (parent == structVal)
                {
                    structVal = blockNode;
                }
                alreadyNormalized = true;
            }
        }
        break;

        default:
            assert(!"Unexpected node in impNormStructVal()");
            break;
    }
    structVal->gtType  = structType;
    GenTree* structObj = structVal;

    if (!alreadyNormalized || forceNormalization)
    {
        if (makeTemp)
        {
            unsigned tmpNum = lvaGrabTemp(true DEBUGARG("struct address for call/obj"));

            impAssignTempGen(tmpNum, structVal, structHnd, curLevel);

            // The structVal is now the temp itself

            structLcl = gtNewLclvNode(tmpNum, structType)->AsLclVarCommon();
            // TODO-1stClassStructs: Avoid always wrapping in GT_OBJ.
            structObj = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structLcl));
        }
        else if (varTypeIsStruct(structType) && !structVal->OperIsBlk())
        {
            // Wrap it in a GT_OBJ
            structObj = gtNewObjNode(structHnd, gtNewOperNode(GT_ADDR, TYP_BYREF, structVal));
        }
    }

    if (structLcl != nullptr)
    {
        // A OBJ on a ADDR(LCL_VAR) can never raise an exception
        // so we don't set GTF_EXCEPT here.
        if (!lvaIsImplicitByRefLocal(structLcl->gtLclNum))
        {
            structObj->gtFlags &= ~GTF_GLOB_REF;
        }
    }
    else
    {
        // In general a OBJ is an indirection and could raise an exception.
        structObj->gtFlags |= GTF_EXCEPT;
    }
    return (structObj);
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
GenTreePtr Compiler::impTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                      BOOL*                   pRuntimeLookup /* = NULL */,
                                      BOOL                    mustRestoreHandle /* = FALSE */,
                                      BOOL                    importParent /* = FALSE */)
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

    return impLookupToTree(pResolvedToken, &embedInfo.lookup, gtTokenToIconFlags(pResolvedToken->token),
                           embedInfo.compileTimeHandle);
}

GenTreePtr Compiler::impLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                     CORINFO_LOOKUP*         pLookup,
                                     unsigned                handleFlags,
                                     void*                   compileTimeHandle)
{
    if (!pLookup->lookupKind.needsRuntimeLookup)
    {
        // No runtime lookup is required.
        // Access is direct or memory-indirect (of a fixed address) reference

        CORINFO_GENERIC_HANDLE handle       = nullptr;
        void*                  pIndirection = nullptr;
        assert(pLookup->constLookup.accessType != IAT_PPVALUE);

        if (pLookup->constLookup.accessType == IAT_VALUE)
        {
            handle = pLookup->constLookup.handle;
        }
        else if (pLookup->constLookup.accessType == IAT_PVALUE)
        {
            pIndirection = pLookup->constLookup.addr;
        }
        return gtNewIconEmbHndNode(handle, pIndirection, handleFlags, 0, nullptr, compileTimeHandle);
    }
    else if (compIsForInlining())
    {
        // Don't import runtime lookups when inlining
        // Inlining has to be aborted in such a case
        compInlineResult->NoteFatal(InlineObservation::CALLSITE_GENERIC_DICTIONARY_LOOKUP);
        return nullptr;
    }
    else
    {
        // Need to use dictionary-based access which depends on the typeContext
        // which is only available at runtime, not at compile-time.

        return impRuntimeLookupToTree(pResolvedToken, pLookup, compileTimeHandle);
    }
}

#ifdef FEATURE_READYTORUN_COMPILER
GenTreePtr Compiler::impReadyToRunLookupToTree(CORINFO_CONST_LOOKUP* pLookup,
                                               unsigned              handleFlags,
                                               void*                 compileTimeHandle)
{
    CORINFO_GENERIC_HANDLE handle       = nullptr;
    void*                  pIndirection = nullptr;
    assert(pLookup->accessType != IAT_PPVALUE);

    if (pLookup->accessType == IAT_VALUE)
    {
        handle = pLookup->handle;
    }
    else if (pLookup->accessType == IAT_PVALUE)
    {
        pIndirection = pLookup->addr;
    }
    return gtNewIconEmbHndNode(handle, pIndirection, handleFlags, 0, nullptr, compileTimeHandle);
}

GenTreeCall* Compiler::impReadyToRunHelperToTree(
    CORINFO_RESOLVED_TOKEN* pResolvedToken,
    CorInfoHelpFunc         helper,
    var_types               type,
    GenTreeArgList*         args /* =NULL*/,
    CORINFO_LOOKUP_KIND*    pGenericLookupKind /* =NULL. Only used with generics */)
{
    CORINFO_CONST_LOOKUP lookup;
    if (!info.compCompHnd->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, helper, &lookup))
    {
        return nullptr;
    }

    GenTreeCall* op1 = gtNewHelperCallNode(helper, type, GTF_EXCEPT, args);

    op1->setEntryPoint(lookup);

    return op1;
}
#endif

GenTreePtr Compiler::impMethodPointer(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo)
{
    GenTreePtr op1 = nullptr;

    switch (pCallInfo->kind)
    {
        case CORINFO_CALL:
            op1 = new (this, GT_FTN_ADDR) GenTreeFptrVal(TYP_I_IMPL, pCallInfo->hMethod);

#ifdef FEATURE_READYTORUN_COMPILER
            if (opts.IsReadyToRun())
            {
                op1->gtFptrVal.gtEntryPoint = pCallInfo->codePointerLookup.constLookup;
            }
            else
            {
                op1->gtFptrVal.gtEntryPoint.addr = nullptr;
            }
#endif
            break;

        case CORINFO_CALL_CODE_POINTER:
            if (compIsForInlining())
            {
                // Don't import runtime lookups when inlining
                // Inlining has to be aborted in such a case
                compInlineResult->NoteFatal(InlineObservation::CALLSITE_GENERIC_DICTIONARY_LOOKUP);
                return nullptr;
            }

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

GenTreePtr Compiler::getRuntimeContextTree(CORINFO_RUNTIME_LOOKUP_KIND kind)
{
    GenTreePtr ctxTree = nullptr;

    // Collectible types requires that for shared generic code, if we use the generic context parameter
    // that we report it. (This is a conservative approach, we could detect some cases particularly when the
    // context parameter is this that we don't need the eager reporting logic.)
    lvaGenericsContextUseCount++;

    if (kind == CORINFO_LOOKUP_THISOBJ)
    {
        // this Object
        ctxTree = gtNewLclvNode(info.compThisArg, TYP_REF);

        // Vtable pointer of this object
        ctxTree = gtNewOperNode(GT_IND, TYP_I_IMPL, ctxTree);
        ctxTree->gtFlags |= GTF_EXCEPT; // Null-pointer exception
        ctxTree->gtFlags |= GTF_IND_INVARIANT;
    }
    else
    {
        assert(kind == CORINFO_LOOKUP_METHODPARAM || kind == CORINFO_LOOKUP_CLASSPARAM);

        ctxTree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL); // Exact method descriptor as passed in as last arg
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

GenTreePtr Compiler::impRuntimeLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                            CORINFO_LOOKUP*         pLookup,
                                            void*                   compileTimeHandle)
{

    // This method can only be called from the importer instance of the Compiler.
    // In other word, it cannot be called by the instance of the Compiler for the inlinee.
    assert(!compIsForInlining());

    GenTreePtr ctxTree = getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind);

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        return impReadyToRunHelperToTree(pResolvedToken, CORINFO_HELP_READYTORUN_GENERIC_HANDLE, TYP_I_IMPL,
                                         gtNewArgList(ctxTree), &pLookup->lookupKind);
    }
#endif

    CORINFO_RUNTIME_LOOKUP* pRuntimeLookup = &pLookup->runtimeLookup;
    // It's available only via the run-time helper function
    if (pRuntimeLookup->indirections == CORINFO_USEHELPER)
    {
        GenTreeArgList* helperArgs =
            gtNewArgList(ctxTree, gtNewIconEmbHndNode(pRuntimeLookup->signature, nullptr, GTF_ICON_TOKEN_HDL, 0,
                                                      nullptr, compileTimeHandle));

        return gtNewHelperCallNode(pRuntimeLookup->helper, TYP_I_IMPL, GTF_EXCEPT, helperArgs);
    }

    // Slot pointer
    GenTreePtr slotPtrTree = ctxTree;

    if (pRuntimeLookup->testForNull)
    {
        slotPtrTree = impCloneExpr(ctxTree, &ctxTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("impRuntimeLookup slot"));
    }

    GenTreePtr indOffTree = nullptr;

    // Applied repeated indirections
    for (WORD i = 0; i < pRuntimeLookup->indirections; i++)
    {
        if (i == 1 && pRuntimeLookup->indirectFirstOffset)
        {
            indOffTree = impCloneExpr(slotPtrTree, &slotPtrTree, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                      nullptr DEBUGARG("impRuntimeLookup indirectFirstOffset"));
        }

        if (i != 0)
        {
            slotPtrTree = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
            slotPtrTree->gtFlags |= GTF_IND_NONFAULTING;
            slotPtrTree->gtFlags |= GTF_IND_INVARIANT;
        }

        if (i == 1 && pRuntimeLookup->indirectFirstOffset)
        {
            slotPtrTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, indOffTree, slotPtrTree);
        }

        if (pRuntimeLookup->offsets[i] != 0)
        {
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
        relop->gtFlags |= GTF_RELOP_QMARK;

        // slot = GT_IND(slot - 1)
        slot           = gtNewLclvNode(slotLclNum, TYP_I_IMPL);
        GenTree* add   = gtNewOperNode(GT_ADD, TYP_I_IMPL, slot, gtNewIconNode(-1, TYP_I_IMPL));
        GenTree* indir = gtNewOperNode(GT_IND, TYP_I_IMPL, add);
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
    GenTreePtr handle = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
    handle->gtFlags |= GTF_IND_NONFAULTING;

    GenTreePtr handleCopy = impCloneExpr(handle, &handle, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                         nullptr DEBUGARG("impRuntimeLookup typehandle"));

    // Call to helper
    GenTreeArgList* helperArgs =
        gtNewArgList(ctxTree, gtNewIconEmbHndNode(pRuntimeLookup->signature, nullptr, GTF_ICON_TOKEN_HDL, 0, nullptr,
                                                  compileTimeHandle));
    GenTreePtr helperCall = gtNewHelperCallNode(pRuntimeLookup->helper, TYP_I_IMPL, GTF_EXCEPT, helperArgs);

    // Check for null and possibly call helper
    GenTreePtr relop = gtNewOperNode(GT_NE, TYP_INT, handle, gtNewIconNode(0, TYP_I_IMPL));
    relop->gtFlags |= GTF_RELOP_QMARK;

    GenTreePtr colon = new (this, GT_COLON) GenTreeColon(TYP_I_IMPL,
                                                         gtNewNothingNode(), // do nothing if nonnull
                                                         helperCall);

    GenTreePtr qmark = gtNewQmarkNode(TYP_I_IMPL, relop, colon);

    unsigned tmp;
    if (handleCopy->IsLocal())
    {
        tmp = handleCopy->gtLclVarCommon.gtLclNum;
    }
    else
    {
        tmp = lvaGrabTemp(true DEBUGARG("spilling QMark1"));
    }

    impAssignTempGen(tmp, qmark, (unsigned)CHECK_SPILL_NONE);
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

    GenTreePtr tree = verCurrentState.esStack[level].val;

    /* Allocate a temp if we haven't been asked to use a particular one */

    if (tiVerificationNeeded)
    {
        // Ignore bad temp requests (they will happen with bad code and will be
        // catched when importing the destblock)
        if ((tnum != BAD_VAR_NUM && tnum >= lvaCount) && verNeedsVerification())
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
#ifndef _TARGET_64BIT_
                (valTyp == TYP_I_IMPL && dstTyp == TYP_BYREF) || (valTyp == TYP_BYREF && dstTyp == TYP_I_IMPL) ||
#endif // !_TARGET_64BIT_
                (varTypeIsFloating(dstTyp) && varTypeIsFloating(valTyp))))
        {
            if (verNeedsVerification())
            {
                return false;
            }
        }
    }

    /* Assign the spilled entry to the temp */
    impAssignTempGen(tnum, tree, verCurrentState.esStack[level].seTypeInfo.GetClassHandle(), level);

    // If temp is newly introduced and a ref type, grab what type info we can.
    if (isNewTemp && (lvaTable[tnum].lvType == TYP_REF))
    {
        CORINFO_CLASS_HANDLE stkHnd = verCurrentState.esStack[level].seTypeInfo.GetClassHandle();
        lvaSetClass(tnum, tree, stkHnd);
    }

    // The tree type may be modified by impAssignTempGen, so use the type of the lclVar.
    var_types  type                    = genActualType(lvaTable[tnum].TypeGet());
    GenTreePtr temp                    = gtNewLclvNode(tnum, type);
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
        GenTreePtr tree = verCurrentState.esStack[level].val;

        if (!spillLeaves && tree->OperIsLeaf())
        {
            continue;
        }

        // Temps introduced by the importer itself don't need to be spilled

        bool isTempLcl = (tree->OperGet() == GT_LCL_VAR) && (tree->gtLclVarCommon.gtLclNum >= info.compLocalsCount);

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
        GenTreePtr tree = verCurrentState.esStack[i].val;

        GenTreePtr lclVarTree;

        if ((tree->gtFlags & spillFlags) != 0 ||
            (spillGlobEffects &&                        // Only consider the following when  spillGlobEffects == TRUE
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
        GenTreePtr tree = verCurrentState.esStack[level].val;
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
        GenTreePtr tree = verCurrentState.esStack[level].val;

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

Compiler::fgWalkResult Compiler::impFindValueClasses(GenTreePtr* pTree, fgWalkData* data)
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
        GenTreePtr tree = verCurrentState.esStack[level].val;

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
    if ((hndBlk->bbFlags & (BBF_IMPORTED | BBF_INTERNAL | BBF_DONT_REMOVE | BBF_HAS_LABEL | BBF_JMP_TARGET)) ==
        (BBF_IMPORTED | BBF_INTERNAL | BBF_DONT_REMOVE | BBF_HAS_LABEL | BBF_JMP_TARGET))
    {
        GenTreePtr tree = hndBlk->bbTreeList;

        if (tree != nullptr && tree->gtOper == GT_STMT)
        {
            tree = tree->gtStmt.gtStmtExpr;
            assert(tree != nullptr);

            if ((tree->gtOper == GT_ASG) && (tree->gtOp.gtOp1->gtOper == GT_LCL_VAR) &&
                (tree->gtOp.gtOp2->gtOper == GT_CATCH_ARG))
            {
                tree = gtNewLclvNode(tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum, TYP_REF);

                impPushOnStack(tree, typeInfo(TI_REF, clsHnd));

                return hndBlk->bbNext;
            }
        }

        // If we get here, it must have been some other kind of internal block. It's possible that
        // someone prepended something to our injected block, but that's unlikely.
    }

    /* Push the exception address value on the stack */
    GenTreePtr arg = new (this, GT_CATCH_ARG) GenTree(GT_CATCH_ARG, TYP_REF);

    /* Mark the node as having a side-effect - i.e. cannot be
     * moved around since it is tied to a fixed location (EAX) */
    arg->gtFlags |= GTF_ORDER_SIDEEFF;

#if defined(JIT32_GCENCODER)
    const bool forceInsertNewBlock = isSingleBlockFilter || compStressCompile(STRESS_CATCH_ARG, 5);
#else
    const bool forceInsertNewBlock                                     = compStressCompile(STRESS_CATCH_ARG, 5);
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
        newBlk->bbFlags |= BBF_IMPORTED | BBF_DONT_REMOVE | BBF_HAS_LABEL | BBF_JMP_TARGET;
        newBlk->setBBWeight(hndBlk->bbWeight);
        newBlk->bbCodeOffs = hndBlk->bbCodeOffs;

        /* Account for the new link we are about to create */
        hndBlk->bbRefs++;

        /* Spill into a temp */
        unsigned tempNum         = lvaGrabTemp(false DEBUGARG("SpillCatchArg"));
        lvaTable[tempNum].lvType = TYP_REF;
        arg                      = gtNewTempAssign(tempNum, arg);

        hndBlk->bbStkTempsIn = tempNum;

        /* Report the debug info. impImportBlockCode won't treat
         * the actual handler as exception block and thus won't do it for us. */
        if (info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES)
        {
            impCurStmtOffs = newBlk->bbCodeOffs | IL_OFFSETX_STKBIT;
            arg            = gtNewStmt(arg, impCurStmtOffs);
        }

        fgInsertStmtAtEnd(newBlk, arg);

        arg = gtNewLclvNode(tempNum, TYP_REF);
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

GenTreePtr Compiler::impCloneExpr(GenTreePtr           tree,
                                  GenTreePtr*          pClone,
                                  CORINFO_CLASS_HANDLE structHnd,
                                  unsigned             curLevel,
                                  GenTreePtr* pAfterStmt DEBUGARG(const char* reason))
{
    if (!(tree->gtFlags & GTF_GLOB_EFFECT))
    {
        GenTreePtr clone = gtClone(tree, true);

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
        GenTreePtr callStmt = impInlineInfo->iciStmt;
        assert(callStmt->gtOper == GT_STMT);
        impCurStmtOffs = callStmt->gtStmt.gtStmtILoffsx;
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
    // Don't spill after ldtoken, because it could be a part of the InitializeArray sequence.
    // Avoid breaking up to guarantee that impInitializeArrayIntrinsic can succeed.
    return prevOpcode != CEE_LDTOKEN;
}

/*****************************************************************************
 *
 *  Remember the instr offset for the statements
 *
 *  When we do impAppendTree(tree), we can't set tree->gtStmtLastILoffs to
 *  impCurOpcOffs, if the append was done because of a partial stack spill,
 *  as some of the trees corresponding to code up to impCurOpcOffs might
 *  still be sitting on the stack.
 *  So we delay marking of gtStmtLastILoffs until impNoteLastILoffs().
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

        assert(impTreeLast);
        assert(impTreeLast->gtOper == GT_STMT);

        impTreeLast->gtStmt.gtStmtLastILoffs = compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs;
    }
    else
    {
        impLastILoffsStmt->gtStmt.gtStmtLastILoffs = compIsForInlining() ? BAD_IL_OFFSET : impCurOpcOffs;
        impLastILoffsStmt                          = nullptr;
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

static inline bool impOpcodeIsCallOpcode(OPCODE opcode)
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
void Compiler::impBashVarAddrsToI(GenTreePtr tree1, GenTreePtr tree2)
{
    if (tree1->IsVarAddr())
    {
        tree1->gtType = TYP_I_IMPL;
    }

    if (tree2 && tree2->IsVarAddr())
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

GenTreePtr Compiler::impImplicitIorI4Cast(GenTreePtr tree, var_types dstTyp)
{
    var_types currType   = genActualType(tree->gtType);
    var_types wantedType = genActualType(dstTyp);

    if (wantedType != currType)
    {
        // Automatic upcast for a GT_CNS_INT into TYP_I_IMPL
        if ((tree->OperGet() == GT_CNS_INT) && varTypeIsI(dstTyp))
        {
            if (!varTypeIsI(tree->gtType) || ((tree->gtType == TYP_REF) && (tree->gtIntCon.gtIconVal == 0)))
            {
                tree->gtType = TYP_I_IMPL;
            }
        }
#ifdef _TARGET_64BIT_
        else if (varTypeIsI(wantedType) && (currType == TYP_INT))
        {
            // Note that this allows TYP_INT to be cast to a TYP_I_IMPL when wantedType is a TYP_BYREF or TYP_REF
            tree = gtNewCastNode(TYP_I_IMPL, tree, TYP_I_IMPL);
        }
        else if ((wantedType == TYP_INT) && varTypeIsI(currType))
        {
            // Note that this allows TYP_BYREF or TYP_REF to be cast to a TYP_INT
            tree = gtNewCastNode(TYP_INT, tree, TYP_INT);
        }
#endif // _TARGET_64BIT_
    }

    return tree;
}

/*****************************************************************************
 *  TYP_FLOAT and TYP_DOUBLE can be used almost interchangeably in some cases,
 *  but we want to make that an explicit cast in our trees, so any implicit casts
 *  that exist in the IL are turned into explicit casts here.
 */

GenTreePtr Compiler::impImplicitR4orR8Cast(GenTreePtr tree, var_types dstTyp)
{
#ifndef LEGACY_BACKEND
    if (varTypeIsFloating(tree) && varTypeIsFloating(dstTyp) && (dstTyp != tree->gtType))
    {
        tree = gtNewCastNode(dstTyp, tree, dstTyp);
    }
#endif // !LEGACY_BACKEND

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

GenTreePtr Compiler::impInitializeArrayIntrinsic(CORINFO_SIG_INFO* sig)
{
    assert(sig->numArgs == 2);

    GenTreePtr fieldTokenNode = impStackTop(0).val;
    GenTreePtr arrayLocalNode = impStackTop(1).val;

    //
    // Verify that the field token is known and valid.  Note that It's also
    // possible for the token to come from reflection, in which case we cannot do
    // the optimization and must therefore revert to calling the helper.  You can
    // see an example of this in bvt\DynIL\initarray2.exe (in Main).
    //

    // Check to see if the ldtoken helper call is what we see here.
    if (fieldTokenNode->gtOper != GT_CALL || (fieldTokenNode->gtCall.gtCallType != CT_HELPER) ||
        (fieldTokenNode->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD)))
    {
        return nullptr;
    }

    // Strip helper call away
    fieldTokenNode = fieldTokenNode->gtCall.gtCallArgs->Current();

    if (fieldTokenNode->gtOper == GT_IND)
    {
        fieldTokenNode = fieldTokenNode->gtOp.gtOp1;
    }

    // Check for constant
    if (fieldTokenNode->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    CORINFO_FIELD_HANDLE fieldToken = (CORINFO_FIELD_HANDLE)fieldTokenNode->gtIntCon.gtCompileTimeHandle;
    if (!fieldTokenNode->IsIconHandle(GTF_ICON_FIELD_HDL) || (fieldToken == nullptr))
    {
        return nullptr;
    }

    //
    // We need to get the number of elements in the array and the size of each element.
    // We verify that the newarr statement is exactly what we expect it to be.
    // If it's not then we just return NULL and we don't optimize this call
    //

    //
    // It is possible the we don't have any statements in the block yet
    //
    if (impTreeLast->gtOper != GT_STMT)
    {
        assert(impTreeLast->gtOper == GT_BEG_STMTS);
        return nullptr;
    }

    //
    // We start by looking at the last statement, making sure it's an assignment, and
    // that the target of the assignment is the array passed to InitializeArray.
    //
    GenTreePtr arrayAssignment = impTreeLast->gtStmt.gtStmtExpr;
    if ((arrayAssignment->gtOper != GT_ASG) || (arrayAssignment->gtOp.gtOp1->gtOper != GT_LCL_VAR) ||
        (arrayLocalNode->gtOper != GT_LCL_VAR) ||
        (arrayAssignment->gtOp.gtOp1->gtLclVarCommon.gtLclNum != arrayLocalNode->gtLclVarCommon.gtLclNum))
    {
        return nullptr;
    }

    //
    // Make sure that the object being assigned is a helper call.
    //

    GenTreePtr newArrayCall = arrayAssignment->gtOp.gtOp2;
    if ((newArrayCall->gtOper != GT_CALL) || (newArrayCall->gtCall.gtCallType != CT_HELPER))
    {
        return nullptr;
    }

    //
    // Verify that it is one of the new array helpers.
    //

    bool isMDArray = false;

    if (newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_DIRECT) &&
        newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_OBJ) &&
        newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_VC) &&
        newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_ALIGN8)
#ifdef FEATURE_READYTORUN_COMPILER
        && newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1)
#endif
            )
    {
        if (newArrayCall->gtCall.gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEW_MDARR_NONVARARG))
        {
            return nullptr;
        }

        isMDArray = true;
    }

    CORINFO_CLASS_HANDLE arrayClsHnd = (CORINFO_CLASS_HANDLE)newArrayCall->gtCall.compileTimeHelperArgumentHandle;

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

        GenTreeArgList* tokenArg = newArrayCall->gtCall.gtCallArgs;
        assert(tokenArg != nullptr);
        GenTreeArgList* numArgsArg = tokenArg->Rest();
        assert(numArgsArg != nullptr);
        GenTreeArgList* argsArg = numArgsArg->Rest();
        assert(argsArg != nullptr);

        //
        // The number of arguments should be a constant between 1 and 64. The rank can't be 0
        // so at least one length must be present and the rank can't exceed 32 so there can
        // be at most 64 arguments - 32 lengths and 32 lower bounds.
        //

        if ((!numArgsArg->Current()->IsCnsIntOrI()) || (numArgsArg->Current()->AsIntCon()->IconValue() < 1) ||
            (numArgsArg->Current()->AsIntCon()->IconValue() > 64))
        {
            return nullptr;
        }

        unsigned numArgs = static_cast<unsigned>(numArgsArg->Current()->AsIntCon()->IconValue());
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

        for (comma = argsArg->Current(); Match::IsComma(comma); comma = comma->gtGetOp2())
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

        GenTreePtr arrayLengthNode;

        GenTreeArgList* args = newArrayCall->gtCall.gtCallArgs;
#ifdef FEATURE_READYTORUN_COMPILER
        if (newArrayCall->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1))
        {
            // Array length is 1st argument for readytorun helper
            arrayLengthNode = args->Current();
        }
        else
#endif
        {
            // Array length is 2nd argument for regular helper
            arrayLengthNode = args->Rest()->Current();
        }

        //
        // Make sure that the number of elements look valid.
        //
        if (arrayLengthNode->gtOper != GT_CNS_INT)
        {
            return nullptr;
        }

        numElements = S_SIZE_T(arrayLengthNode->gtIntCon.gtIconVal);

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
        assert(verNeedsVerification());
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
    GenTreePtr     dst;

    if (isMDArray)
    {
        unsigned dataOffset = eeGetMDArrayDataOffset(elementType, rank);

        dst = gtNewOperNode(GT_ADD, TYP_BYREF, arrayLocalNode, gtNewIconNode(dataOffset, TYP_I_IMPL));
    }
    else
    {
        dst = gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewIndexRef(elementType, arrayLocalNode, gtNewIconNode(0)));
    }
    GenTreePtr blk     = gtNewBlockVal(dst, blkSize);
    GenTreePtr srcAddr = gtNewIconHandleNode((size_t)initData, GTF_ICON_STATIC_HDL);
    GenTreePtr src     = gtNewOperNode(GT_IND, TYP_STRUCT, srcAddr);

    return gtNewBlkOpNode(blk,     // dst
                          src,     // src
                          blkSize, // size
                          false,   // volatil
                          true);   // copyBlock
}

/*****************************************************************************/
// Returns the GenTree that should be used to do the intrinsic instead of the call.
// Returns NULL if an intrinsic cannot be used

GenTreePtr Compiler::impIntrinsic(GenTreePtr            newobjThis,
                                  CORINFO_CLASS_HANDLE  clsHnd,
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO*     sig,
                                  int                   memberRef,
                                  bool                  readonlyCall,
                                  bool                  tailCall,
                                  CorInfoIntrinsics*    pIntrinsicID)
{
    bool              mustExpand  = false;
    CorInfoIntrinsics intrinsicID = info.compCompHnd->getIntrinsicID(method, &mustExpand);
    *pIntrinsicID                 = intrinsicID;

#ifndef _TARGET_ARM_
    genTreeOps interlockedOperator;
#endif

    if (intrinsicID == CORINFO_INTRINSIC_StubHelpers_GetStubContext)
    {
        // must be done regardless of DbgCode and MinOpts
        return gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL);
    }
#ifdef _TARGET_64BIT_
    if (intrinsicID == CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr)
    {
        // must be done regardless of DbgCode and MinOpts
        return gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL));
    }
#else
    assert(intrinsicID != CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr);
#endif

    GenTreePtr retNode = nullptr;

    //
    // We disable the inlining of instrinsics for MinOpts.
    //
    if (!mustExpand && (opts.compDbgCode || opts.MinOpts()))
    {
        *pIntrinsicID = CORINFO_INTRINSIC_Illegal;
        return retNode;
    }

    // Currently we don't have CORINFO_INTRINSIC_Exp because it does not
    // seem to work properly for Infinity values, we don't do
    // CORINFO_INTRINSIC_Pow because it needs a Helper which we currently don't have

    var_types callType = JITtype2varType(sig->retType);

    /* First do the intrinsics which are always smaller than a call */

    switch (intrinsicID)
    {
        GenTreePtr op1, op2;

        case CORINFO_INTRINSIC_Sin:
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Round:
        case CORINFO_INTRINSIC_Cosh:
        case CORINFO_INTRINSIC_Sinh:
        case CORINFO_INTRINSIC_Tan:
        case CORINFO_INTRINSIC_Tanh:
        case CORINFO_INTRINSIC_Asin:
        case CORINFO_INTRINSIC_Acos:
        case CORINFO_INTRINSIC_Atan:
        case CORINFO_INTRINSIC_Atan2:
        case CORINFO_INTRINSIC_Log10:
        case CORINFO_INTRINSIC_Pow:
        case CORINFO_INTRINSIC_Exp:
        case CORINFO_INTRINSIC_Ceiling:
        case CORINFO_INTRINSIC_Floor:

            // These are math intrinsics

            assert(callType != TYP_STRUCT);

            op1 = nullptr;

#if defined(LEGACY_BACKEND)
            if (IsTargetIntrinsic(intrinsicID))
#elif !defined(_TARGET_X86_)
            // Intrinsics that are not implemented directly by target instructions will
            // be re-materialized as users calls in rationalizer. For prefixed tail calls,
            // don't do this optimization, because
            //  a) For back compatibility reasons on desktop.Net 4.6 / 4.6.1
            //  b) It will be non-trivial task or too late to re-materialize a surviving
            //     tail prefixed GT_INTRINSIC as tail call in rationalizer.
            if (!IsIntrinsicImplementedByUserCall(intrinsicID) || !tailCall)
#else
            // On x86 RyuJIT, importing intrinsics that are implemented as user calls can cause incorrect calculation
            // of the depth of the stack if these intrinsics are used as arguments to another call. This causes bad
            // code generation for certain EH constructs.
            if (!IsIntrinsicImplementedByUserCall(intrinsicID))
#endif
            {
                switch (sig->numArgs)
                {
                    case 1:
                        op1 = impPopStack().val;

#if FEATURE_X87_DOUBLES

                        // X87 stack doesn't differentiate between float/double
                        // so it doesn't need a cast, but everybody else does
                        // Just double check it is at least a FP type
                        noway_assert(varTypeIsFloating(op1));

#else // FEATURE_X87_DOUBLES

                        if (op1->TypeGet() != callType)
                        {
                            op1 = gtNewCastNode(callType, op1, callType);
                        }

#endif // FEATURE_X87_DOUBLES

                        op1 = new (this, GT_INTRINSIC)
                            GenTreeIntrinsic(genActualType(callType), op1, intrinsicID, method);
                        break;

                    case 2:
                        op2 = impPopStack().val;
                        op1 = impPopStack().val;

#if FEATURE_X87_DOUBLES

                        // X87 stack doesn't differentiate between float/double
                        // so it doesn't need a cast, but everybody else does
                        // Just double check it is at least a FP type
                        noway_assert(varTypeIsFloating(op2));
                        noway_assert(varTypeIsFloating(op1));

#else // FEATURE_X87_DOUBLES

                        if (op2->TypeGet() != callType)
                        {
                            op2 = gtNewCastNode(callType, op2, callType);
                        }
                        if (op1->TypeGet() != callType)
                        {
                            op1 = gtNewCastNode(callType, op1, callType);
                        }

#endif // FEATURE_X87_DOUBLES

                        op1 = new (this, GT_INTRINSIC)
                            GenTreeIntrinsic(genActualType(callType), op1, op2, intrinsicID, method);
                        break;

                    default:
                        NO_WAY("Unsupported number of args for Math Instrinsic");
                }

#ifndef LEGACY_BACKEND
                if (IsIntrinsicImplementedByUserCall(intrinsicID))
                {
                    op1->gtFlags |= GTF_CALL;
                }
#endif
            }

            retNode = op1;
            break;

#ifdef _TARGET_XARCH_
        // TODO-ARM-CQ: reenable treating Interlocked operation as intrinsic
        case CORINFO_INTRINSIC_InterlockedAdd32:
            interlockedOperator = GT_LOCKADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXAdd32:
            interlockedOperator = GT_XADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXchg32:
            interlockedOperator = GT_XCHG;
            goto InterlockedBinOpCommon;

#ifdef _TARGET_AMD64_
        case CORINFO_INTRINSIC_InterlockedAdd64:
            interlockedOperator = GT_LOCKADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXAdd64:
            interlockedOperator = GT_XADD;
            goto InterlockedBinOpCommon;
        case CORINFO_INTRINSIC_InterlockedXchg64:
            interlockedOperator = GT_XCHG;
            goto InterlockedBinOpCommon;
#endif // _TARGET_AMD64_

        InterlockedBinOpCommon:
            assert(callType != TYP_STRUCT);
            assert(sig->numArgs == 2);

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
            op1->gtFlags |= GTF_GLOB_EFFECT;
            retNode = op1;
            break;
#endif // _TARGET_XARCH_

        case CORINFO_INTRINSIC_MemoryBarrier:

            assert(sig->numArgs == 0);

            op1 = new (this, GT_MEMORYBARRIER) GenTree(GT_MEMORYBARRIER, TYP_VOID);
            op1->gtFlags |= GTF_GLOB_EFFECT;
            retNode = op1;
            break;

#ifdef _TARGET_XARCH_
        // TODO-ARM-CQ: reenable treating InterlockedCmpXchg32 operation as intrinsic
        case CORINFO_INTRINSIC_InterlockedCmpXchg32:
#ifdef _TARGET_AMD64_
        case CORINFO_INTRINSIC_InterlockedCmpXchg64:
#endif
        {
            assert(callType != TYP_STRUCT);
            assert(sig->numArgs == 3);
            GenTreePtr op3;

            op3 = impPopStack().val; // comparand
            op2 = impPopStack().val; // value
            op1 = impPopStack().val; // location

            GenTreePtr node = new (this, GT_CMPXCHG) GenTreeCmpXchg(genActualType(callType), op1, op2, op3);

            node->gtCmpXchg.gtOpLocation->gtFlags |= GTF_DONT_CSE;
            retNode = node;
            break;
        }
#endif

        case CORINFO_INTRINSIC_StringLength:
            op1 = impPopStack().val;
            if (!opts.MinOpts() && !opts.compDbgCode)
            {
                GenTreeArrLen* arrLen =
                    new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, op1, offsetof(CORINFO_String, stringLen));
                op1 = arrLen;
            }
            else
            {
                /* Create the expression "*(str_addr + stringLengthOffset)" */
                op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                    gtNewIconNode(offsetof(CORINFO_String, stringLen), TYP_I_IMPL));
                op1 = gtNewOperNode(GT_IND, TYP_INT, op1);
            }

            // Getting the length of a null string should throw
            op1->gtFlags |= GTF_EXCEPT;

            retNode = op1;
            break;

        case CORINFO_INTRINSIC_StringGetChar:
            op2 = impPopStack().val;
            op1 = impPopStack().val;
            op1 = gtNewIndexRef(TYP_CHAR, op1, op2);
            op1->gtFlags |= GTF_INX_STRING_LAYOUT;
            retNode = op1;
            break;

        case CORINFO_INTRINSIC_InitializeArray:
            retNode = impInitializeArrayIntrinsic(sig);
            break;

        case CORINFO_INTRINSIC_Array_Address:
        case CORINFO_INTRINSIC_Array_Get:
        case CORINFO_INTRINSIC_Array_Set:
            retNode = impArrayAccessIntrinsic(clsHnd, sig, memberRef, readonlyCall, intrinsicID);
            break;

        case CORINFO_INTRINSIC_GetTypeFromHandle:
            op1 = impStackTop(0).val;
            if (op1->gtOper == GT_CALL && (op1->gtCall.gtCallType == CT_HELPER) &&
                gtIsTypeHandleToRuntimeTypeHelper(op1->AsCall()))
            {
                op1 = impPopStack().val;
                // Change call to return RuntimeType directly.
                op1->gtType = TYP_REF;
                retNode     = op1;
            }
            // Call the regular function.
            break;

        case CORINFO_INTRINSIC_RTH_GetValueInternal:
            op1 = impStackTop(0).val;
            if (op1->gtOper == GT_CALL && (op1->gtCall.gtCallType == CT_HELPER) &&
                gtIsTypeHandleToRuntimeTypeHelper(op1->AsCall()))
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
                op1 = op1->gtCall.gtCallArgs;
                assert(op1->OperIsList());
                assert(op1->gtOp.gtOp2 == nullptr);
                op1     = op1->gtOp.gtOp1;
                retNode = op1;
            }
            // Call the regular function.
            break;

#ifndef LEGACY_BACKEND
        case CORINFO_INTRINSIC_Object_GetType:

            op1 = impPopStack().val;
            op1 = new (this, GT_INTRINSIC) GenTreeIntrinsic(genActualType(callType), op1, intrinsicID, method);

            // Set the CALL flag to indicate that the operator is implemented by a call.
            // Set also the EXCEPTION flag because the native implementation of
            // CORINFO_INTRINSIC_Object_GetType intrinsic can throw NullReferenceException.
            op1->gtFlags |= (GTF_CALL | GTF_EXCEPT);
            retNode = op1;
            break;
#endif
        // Implement ByReference Ctor.  This wraps the assignment of the ref into a byref-like field
        // in a value type.  The canonical example of this is Span<T>. In effect this is just a
        // substitution.  The parameter byref will be assigned into the newly allocated object.
        case CORINFO_INTRINSIC_ByReference_Ctor:
        {
            // Remove call to constructor and directly assign the byref passed
            // to the call to the first slot of the ByReference struct.
            op1                                    = impPopStack().val;
            GenTreePtr           thisptr           = newobjThis;
            CORINFO_FIELD_HANDLE fldHnd            = info.compCompHnd->getFieldInClass(clsHnd, 0);
            GenTreePtr           field             = gtNewFieldRef(TYP_BYREF, fldHnd, thisptr, 0, false);
            GenTreePtr           assign            = gtNewAssignNode(field, op1);
            GenTreePtr           byReferenceStruct = gtCloneExpr(thisptr->gtGetOp1());
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
            GenTreePtr           field  = gtNewFieldRef(TYP_BYREF, fldHnd, op1, 0, false);
            retNode                     = field;
            break;
        }
        case CORINFO_INTRINSIC_Span_GetItem:
        case CORINFO_INTRINSIC_ReadOnlySpan_GetItem:
        {
            // Have index, stack pointer-to Span<T> s on the stack. Expand to:
            //
            // For Span<T>
            //   Comma
            //     BoundsCheck(index, s->_length)
            //     s->_pointer + index * sizeof(T)
            //
            // For ReadOnlySpan<T>
            //   Comma
            //     BoundsCheck(index, s->_length)
            //     *(s->_pointer + index * sizeof(T))
            //
            // Signature should show one class type parameter, which
            // we need to examine.
            assert(sig->sigInst.classInstCount == 1);
            CORINFO_CLASS_HANDLE spanElemHnd = sig->sigInst.classInst[0];
            const unsigned       elemSize    = info.compCompHnd->getClassSize(spanElemHnd);
            assert(elemSize > 0);

            const bool isReadOnly = (intrinsicID == CORINFO_INTRINSIC_ReadOnlySpan_GetItem);

            JITDUMP("\nimpIntrinsic: Expanding %sSpan<T>.get_Item, T=%s, sizeof(T)=%u\n", isReadOnly ? "ReadOnly" : "",
                    info.compCompHnd->getClassName(spanElemHnd), elemSize);

            GenTreePtr index          = impPopStack().val;
            GenTreePtr ptrToSpan      = impPopStack().val;
            GenTreePtr indexClone     = nullptr;
            GenTreePtr ptrToSpanClone = nullptr;

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
            GenTreePtr           length       = gtNewFieldRef(TYP_INT, lengthHnd, ptrToSpan, lengthOffset, false);
            GenTreePtr           boundsCheck  = new (this, GT_ARR_BOUNDS_CHECK)
                GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, index, length, SCK_RNGCHK_FAIL);

            // Element access
            GenTreePtr           indexIntPtr = impImplicitIorI4Cast(indexClone, TYP_I_IMPL);
            GenTreePtr           sizeofNode  = gtNewIconNode(elemSize);
            GenTreePtr           mulNode     = gtNewOperNode(GT_MUL, TYP_I_IMPL, indexIntPtr, sizeofNode);
            CORINFO_FIELD_HANDLE ptrHnd      = info.compCompHnd->getFieldInClass(clsHnd, 0);
            const unsigned       ptrOffset   = info.compCompHnd->getFieldOffset(ptrHnd);
            GenTreePtr           data        = gtNewFieldRef(TYP_BYREF, ptrHnd, ptrToSpanClone, ptrOffset, false);
            GenTreePtr           result      = gtNewOperNode(GT_ADD, TYP_BYREF, data, mulNode);

            // Prepare result
            var_types resultType = JITtype2varType(sig->retType);

            if (isReadOnly)
            {
                result = gtNewOperNode(GT_IND, resultType, result);
            }
            else
            {
                assert(resultType == result->TypeGet());
            }

            retNode = gtNewOperNode(GT_COMMA, resultType, boundsCheck, result);

            break;
        }

        default:
            /* Unknown intrinsic */
            break;
    }

    if (mustExpand)
    {
        if (retNode == nullptr)
        {
            NO_WAY("JIT must expand the intrinsic!");
        }
    }

    return retNode;
}

/*****************************************************************************/

GenTreePtr Compiler::impArrayAccessIntrinsic(
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

    GenTreePtr val = nullptr;

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

    GenTreePtr inds[GT_ARR_MAX_RANK];
    for (unsigned k = rank; k > 0; k--)
    {
        inds[k - 1] = impPopStack().val;
    }

    GenTreePtr arr = impPopStack().val;
    assert(arr->gtType == TYP_REF);

    GenTreePtr arrElem =
        new (this, GT_ARR_ELEM) GenTreeArrElem(TYP_BYREF, arr, static_cast<unsigned char>(rank),
                                               static_cast<unsigned char>(arrayElemSize), elemType, &inds[0]);

    if (intrinsicID != CORINFO_INTRINSIC_Array_Address)
    {
        arrElem = gtNewOperNode(GT_IND, elemType, arrElem);
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

BOOL Compiler::verMergeEntryStates(BasicBlock* block, bool* changed)
{
    unsigned i;

    // do some basic checks first
    if (block->bbStackDepthOnEntry() != verCurrentState.esStackDepth)
    {
        return FALSE;
    }

    if (verCurrentState.esStackDepth > 0)
    {
        // merge stack types
        StackEntry* parentStack = block->bbStackOnEntry();
        StackEntry* childStack  = verCurrentState.esStack;

        for (i = 0; i < verCurrentState.esStackDepth; i++, parentStack++, childStack++)
        {
            if (tiMergeToCommonParent(&parentStack->seTypeInfo, &childStack->seTypeInfo, changed) == FALSE)
            {
                return FALSE;
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

    return TRUE;
}

/*****************************************************************************
 * 'logMsg' is true if a log message needs to be logged. false if the caller has
 *   already logged it (presumably in a more detailed fashion than done here)
 * 'bVerificationException' is true for a verification exception, false for a
 *   "call unauthorized by host" exception.
 */

void Compiler::verConvertBBToThrowVerificationException(BasicBlock* block DEBUGARG(bool logMsg))
{
    block->bbJumpKind = BBJ_THROW;
    block->bbFlags |= BBF_FAILED_VERIFICATION;

    impCurStmtOffsSet(block->bbCodeOffs);

#ifdef DEBUG
    // we need this since BeginTreeList asserts otherwise
    impTreeList = impTreeLast = nullptr;
    block->bbFlags &= ~BBF_IMPORTED;

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

    GenTreePtr op1 = gtNewHelperCallNode(CORINFO_HELP_VERIFICATION, TYP_VOID, GTF_EXCEPT,
                                         gtNewArgList(gtNewIconNode(block->bbCodeOffs)));
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
    // In AMD64, for historical reasons involving design limitations of JIT64, the VM has a
    // slightly different mechanism in which it calls the JIT to perform IL verification:
    // in the case of transparent methods the VM calls for a predicate IsVerifiable()
    // that consists of calling the JIT with the IMPORT_ONLY flag and with the IL verify flag on.
    // If the JIT determines the method is not verifiable, it should raise the exception to the VM and let
    // it bubble up until reported by the runtime.  Currently in RyuJIT, this method doesn't bubble
    // up the exception, instead it embeds a throw inside the offending basic block and lets this
    // to fail upon runtime of the jitted method.
    //
    // For AMD64 we don't want this behavior when the JIT has been called only for verification (i.e.
    // with the IMPORT_ONLY and IL Verification flag set) because this won't actually generate code,
    // just try to find out whether to fail this method before even actually jitting it.  So, in case
    // we detect these two conditions, instead of generating a throw statement inside the offending
    // basic block, we immediately fail to JIT and notify the VM to make the IsVerifiable() predicate
    // to return false and make RyuJIT behave the same way JIT64 does.
    //
    // The rationale behind this workaround is to avoid modifying the VM and maintain compatibility between JIT64 and
    // RyuJIT for the time being until we completely replace JIT64.
    // TODO-ARM64-Cleanup:  We probably want to actually modify the VM in the future to avoid the unnecesary two passes.

    // In AMD64 we must make sure we're behaving the same way as JIT64, meaning we should only raise the verification
    // exception if we are only importing and verifying.  The method verNeedsVerification() can also modify the
    // tiVerificationNeeded flag in the case it determines it can 'skip verification' during importation and defer it
    // to a runtime check. That's why we must assert one or the other (since the flag tiVerificationNeeded can
    // be turned off during importation).
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_

#ifdef DEBUG
    bool canSkipVerificationResult =
        info.compCompHnd->canSkipMethodVerification(info.compMethodHnd) != CORINFO_VERIFICATION_CANNOT_SKIP;
    assert(tiVerificationNeeded || canSkipVerificationResult);
#endif // DEBUG

    // Add the non verifiable flag to the compiler
    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IMPORT_ONLY))
    {
        tiIsVerifiableCode = FALSE;
    }
#endif //_TARGET_64BIT_
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

#ifdef _TARGET_64BIT_
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
#endif // _TARGET_64BIT_

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

#ifdef _TARGET_64BIT_
        if (t == CORINFO_TYPE_NATIVEINT || t == CORINFO_TYPE_NATIVEUINT)
        {
            return typeInfo::nativeInt();
        }
#endif // _TARGET_64BIT_

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
BOOL Compiler::verIsSDArray(typeInfo ti)
{
    if (ti.IsNullObjRef())
    { // nulls are SD arrays
        return TRUE;
    }

    if (!ti.IsType(TI_REF))
    {
        return FALSE;
    }

    if (!info.compCompHnd->isSDArray(ti.GetClassHandleForObjRef()))
    {
        return FALSE;
    }
    return TRUE;
}

/******************************************************************************/
/* Given 'arrayObjectType' which is an array type, fetch the element type. */
/* Returns an error type if anything goes wrong */

typeInfo Compiler::verGetArrayElemType(typeInfo arrayObjectType)
{
    assert(!arrayObjectType.IsNullObjRef()); // you need to check for null explictly since that is a success case

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

/*****************************************************************************/

// This does the expensive check to figure out whether the method
// needs to be verified. It is called only when we fail verification,
// just before throwing the verification exception.

BOOL Compiler::verNeedsVerification()
{
    // If we have previously determined that verification is NOT needed
    // (for example in Compiler::compCompile), that means verification is really not needed.
    // Return the same decision we made before.
    // (Note: This literally means that tiVerificationNeeded can never go from 0 to 1.)

    if (!tiVerificationNeeded)
    {
        return tiVerificationNeeded;
    }

    assert(tiVerificationNeeded);

    // Ok, we haven't concluded that verification is NOT needed. Consult the EE now to
    // obtain the answer.
    CorInfoCanSkipVerificationResult canSkipVerificationResult =
        info.compCompHnd->canSkipMethodVerification(info.compMethodHnd);

    // canSkipVerification will return one of the following three values:
    //    CORINFO_VERIFICATION_CANNOT_SKIP = 0,       // Cannot skip verification during jit time.
    //    CORINFO_VERIFICATION_CAN_SKIP = 1,          // Can skip verification during jit time.
    //    CORINFO_VERIFICATION_RUNTIME_CHECK = 2,     // Skip verification during jit time,
    //     but need to insert a callout to the VM to ask during runtime
    //     whether to skip verification or not.

    // Set tiRuntimeCalloutNeeded if canSkipVerification() instructs us to insert a callout for runtime check
    if (canSkipVerificationResult == CORINFO_VERIFICATION_RUNTIME_CHECK)
    {
        tiRuntimeCalloutNeeded = true;
    }

    if (canSkipVerificationResult == CORINFO_VERIFICATION_DONT_JIT)
    {
        // Dev10 706080 - Testers don't like the assert, so just silence it
        // by not using the macros that invoke debugAssert.
        badCode();
    }

    // When tiVerificationNeeded is true, JIT will do the verification during JIT time.
    // The following line means we will NOT do jit time verification if canSkipVerification
    // returns CORINFO_VERIFICATION_CAN_SKIP or CORINFO_VERIFICATION_RUNTIME_CHECK.
    tiVerificationNeeded = (canSkipVerificationResult == CORINFO_VERIFICATION_CANNOT_SKIP);
    return tiVerificationNeeded;
}

BOOL Compiler::verIsByRefLike(const typeInfo& ti)
{
    if (ti.IsByRef())
    {
        return TRUE;
    }
    if (!ti.IsType(TI_STRUCT))
    {
        return FALSE;
    }
    return info.compCompHnd->getClassAttribs(ti.GetClassHandleForValueClass()) & CORINFO_FLG_CONTAINS_STACK_PTR;
}

BOOL Compiler::verIsSafeToReturnByRef(const typeInfo& ti)
{
    if (ti.IsPermanentHomeByRef())
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

BOOL Compiler::verIsBoxable(const typeInfo& ti)
{
    return (ti.IsPrimitiveType() || ti.IsObjRef() // includes boxed generic type variables
            || ti.IsUnboxedGenericTypeVar() ||
            (ti.IsType(TI_STRUCT) &&
             // exclude byreflike structs
             !(info.compCompHnd->getClassAttribs(ti.GetClassHandleForValueClass()) & CORINFO_FLG_CONTAINS_STACK_PTR)));
}

// Is it a boxed value type?
bool Compiler::verIsBoxedValueType(typeInfo ti)
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
        eeGetSig(pResolvedToken->token, info.compScopeHnd, impTokenLookupContextHandle, &sig);

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
        eeGetCallSiteSig(pResolvedToken->token, info.compScopeHnd, impTokenLookupContextHandle, &sig);
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
            eeGetCallSiteSig(pResolvedToken->token, info.compScopeHnd, impTokenLookupContextHandle, &sig);
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

                BOOL isOpenDelegate = FALSE;
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

                    if ((actualMethodAttribs & CORINFO_FLG_VIRTUAL) && ((actualMethodAttribs & CORINFO_FLG_FINAL) == 0)
#ifdef DEBUG
                        && StrictCheckForNonVirtualCallToVirtualMethod()
#endif
                            )
                    {
                        if (info.compCompHnd->shouldEnforceCallvirtRestriction(info.compScopeHnd))
                        {
                            VerifyOrReturn(tiActualObj.IsThisPtr() && lvaIsOriginalThisReadOnly() ||
                                               verIsBoxedValueType(tiActualObj),
                                           "The 'this' parameter to the call must be either the calling method's "
                                           "'this' parameter or "
                                           "a boxed value type.");
                        }
                    }
                }

                if (actualMethodAttribs & CORINFO_FLG_PROTECTED)
                {
                    BOOL targetIsStatic = actualMethodAttribs & CORINFO_FLG_STATIC;

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

        if (opcode == CEE_CALL && (mflags & CORINFO_FLG_VIRTUAL) && ((mflags & CORINFO_FLG_FINAL) == 0)
#ifdef DEBUG
            && StrictCheckForNonVirtualCallToVirtualMethod()
#endif
                )
        {
            if (info.compCompHnd->shouldEnforceCallvirtRestriction(info.compScopeHnd))
            {
                VerifyOrReturn(
                    tiThis.IsThisPtr() && lvaIsOriginalThisReadOnly() || verIsBoxedValueType(tiThis),
                    "The 'this' parameter to the call must be either the calling method's 'this' parameter or "
                    "a boxed value type.");
            }
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

BOOL Compiler::verCheckDelegateCreation(const BYTE*  delegateCreateStart,
                                        const BYTE*  codeAddr,
                                        mdMemberRef& targetMemberRef)
{
    if (impIsLDFTN_TOKEN(delegateCreateStart, codeAddr))
    {
        targetMemberRef = getU4LittleEndian(&delegateCreateStart[2]);
        return TRUE;
    }
    else if (impIsDUP_LDVIRTFTN_TOKEN(delegateCreateStart, codeAddr))
    {
        targetMemberRef = getU4LittleEndian(&delegateCreateStart[3]);
        return TRUE;
    }

    return FALSE;
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
// 'fieldFlags' is the fields attributes, and mutator is TRUE if it is a
// ld*flda or a st*fld.
// 'enclosingClass' is given if we are accessing a field in some specific type.

void Compiler::verVerifyField(CORINFO_RESOLVED_TOKEN*   pResolvedToken,
                              const CORINFO_FIELD_INFO& fieldInfo,
                              const typeInfo*           tiThis,
                              BOOL                      mutator,
                              BOOL                      allowPlainStructAsThis)
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
#ifdef _TARGET_64BIT_
        Verify(tiCompatibleWith(tiOp1, tiOp2, true), "Cond type mismatch");
#else  // _TARGET_64BIT
        // [10/17/2013] Consider changing this: to put on my verification lawyer hat,
        // this is non-conforming to the ECMA Spec: types don't have to be equivalent,
        // but compatible, since we can coalesce native int with int32 (see section III.1.5).
        Verify(typeInfo::AreEquivalent(tiOp1, tiOp2), "Cond type mismatch");
#endif // !_TARGET_64BIT_
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
                Verify(FALSE, "Cond not allowed on object types");
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

BOOL Compiler::verIsCallToInitThisPtr(CORINFO_CLASS_HANDLE context, CORINFO_CLASS_HANDLE target)
{
    // Either target == context, in this case calling an alternate .ctor
    // Or target is the immediate parent of context

    return ((target == context) || (target == info.compCompHnd->getParentType(context)));
}

GenTreePtr Compiler::impImportLdvirtftn(GenTreePtr              thisPtr,
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
        GenTreePtr runtimeMethodHandle = nullptr;
        if (pCallInfo->exactContextNeedsRuntimeLookup)
        {
            runtimeMethodHandle =
                impRuntimeLookupToTree(pResolvedToken, &pCallInfo->codePointerLookup, pCallInfo->hMethod);
        }
        else
        {
            runtimeMethodHandle = gtNewIconEmbMethHndNode(pResolvedToken->hMethod);
        }
        return gtNewHelperCallNode(CORINFO_HELP_GVMLOOKUP_FOR_SLOT, TYP_I_IMPL, GTF_EXCEPT,
                                   gtNewArgList(thisPtr, runtimeMethodHandle));
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        if (!pCallInfo->exactContextNeedsRuntimeLookup)
        {
            GenTreeCall* call = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR, TYP_I_IMPL, GTF_EXCEPT,
                                                    gtNewArgList(thisPtr));

            call->setEntryPoint(pCallInfo->codePointerLookup.constLookup);

            return call;
        }

        // We need a runtime lookup. CoreRT has a ReadyToRun helper for that too.
        if (IsTargetAbi(CORINFO_CORERT_ABI))
        {
            GenTreePtr ctxTree = getRuntimeContextTree(pCallInfo->codePointerLookup.lookupKind.runtimeLookupKind);

            return impReadyToRunHelperToTree(pResolvedToken, CORINFO_HELP_READYTORUN_GENERIC_HANDLE, TYP_I_IMPL,
                                             gtNewArgList(ctxTree), &pCallInfo->codePointerLookup.lookupKind);
        }
    }
#endif

    // Get the exact descriptor for the static callsite
    GenTreePtr exactTypeDesc = impParentClassTokenToHandle(pResolvedToken);
    if (exactTypeDesc == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    GenTreePtr exactMethodDesc = impTokenToHandle(pResolvedToken);
    if (exactMethodDesc == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    GenTreeArgList* helpArgs = gtNewArgList(exactMethodDesc);

    helpArgs = gtNewListNode(exactTypeDesc, helpArgs);

    helpArgs = gtNewListNode(thisPtr, helpArgs);

    // Call helper function.  This gets the target address of the final destination callsite.

    return gtNewHelperCallNode(CORINFO_HELP_VIRTUAL_FUNC_PTR, TYP_I_IMPL, GTF_EXCEPT, helpArgs);
}

/*****************************************************************************
 *
 *  Build and import a box node
 */

void Compiler::impImportAndPushBox(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    // Get the tree for the type handle for the boxed object.  In the case
    // of shared generic code or ngen'd code this might be an embedded
    // computation.
    // Note we can only box do it if the class construtor has been called
    // We can always do it on primitive types

    GenTreePtr op1 = nullptr;
    GenTreePtr op2 = nullptr;
    var_types  lclTyp;

    impSpillSpecialSideEff();

    // Now get the expression to box from the stack.
    StackEntry           se        = impPopStack();
    CORINFO_CLASS_HANDLE operCls   = se.seTypeInfo.GetClassHandle();
    GenTreePtr           exprToBox = se.val;

    CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pResolvedToken->hClass);
    if (boxHelper == CORINFO_HELP_BOX)
    {
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

        if (impBoxTempInUse || impBoxTemp == BAD_VAR_NUM)
        {
            impBoxTemp = lvaGrabTemp(true DEBUGARG("Box Helper"));
        }

        // needs to stay in use until this box expression is appended
        // some other node.  We approximate this by keeping it alive until
        // the opcode stack becomes empty
        impBoxTempInUse = true;

#ifdef FEATURE_READYTORUN_COMPILER
        bool usingReadyToRunHelper = false;

        if (opts.IsReadyToRun())
        {
            op1                   = impReadyToRunHelperToTree(pResolvedToken, CORINFO_HELP_READYTORUN_NEW, TYP_REF);
            usingReadyToRunHelper = (op1 != nullptr);
        }

        if (!usingReadyToRunHelper)
#endif
        {
            // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
            // and the newfast call with a single call to a dynamic R2R cell that will:
            //      1) Load the context
            //      2) Perform the generic dictionary lookup and caching, and generate the appropriate stub
            //      3) Allocate and return the new object for boxing
            // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

            // Ensure that the value class is restored
            op2 = impTokenToHandle(pResolvedToken, nullptr, TRUE /* mustRestoreHandle */);
            if (op2 == nullptr)
            { // compDonotInline()
                return;
            }

            op1 = gtNewHelperCallNode(info.compCompHnd->getNewHelper(pResolvedToken, info.compMethodHnd), TYP_REF, 0,
                                      gtNewArgList(op2));
        }

        /* Remember that this basic block contains 'new' of an array */
        compCurBB->bbFlags |= BBF_HAS_NEWOBJ;

        GenTreePtr asg = gtNewTempAssign(impBoxTemp, op1);

        GenTreePtr asgStmt = impAppendTree(asg, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        op1 = gtNewLclvNode(impBoxTemp, TYP_REF);
        op2 = gtNewIconNode(sizeof(void*), TYP_I_IMPL);
        op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1, op2);

        if (varTypeIsStruct(exprToBox))
        {
            assert(info.compCompHnd->getClassSize(pResolvedToken->hClass) == info.compCompHnd->getClassSize(operCls));
            op1 = impAssignStructPtr(op1, exprToBox, operCls, (unsigned)CHECK_SPILL_ALL);
        }
        else
        {
            lclTyp = exprToBox->TypeGet();
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
                exprToBox = gtNewCastNode(dstTyp, exprToBox, dstTyp);
            }
            op1 = gtNewAssignNode(gtNewOperNode(GT_IND, lclTyp, op1), exprToBox);
        }

        op2 = gtNewLclvNode(impBoxTemp, TYP_REF);
        op1 = gtNewOperNode(GT_COMMA, TYP_REF, op1, op2);

        // Record that this is a "box" node.
        op1 = new (this, GT_BOX) GenTreeBox(TYP_REF, op1, asgStmt);

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
        // Don't optimize, just call the helper and be done with it

        // Ensure that the value class is restored
        op2 = impTokenToHandle(pResolvedToken, nullptr, TRUE /* mustRestoreHandle */);
        if (op2 == nullptr)
        { // compDonotInline()
            return;
        }

        GenTreeArgList* args = gtNewArgList(op2, impGetStructAddr(exprToBox, operCls, (unsigned)CHECK_SPILL_ALL, true));
        op1                  = gtNewHelperCallNode(boxHelper, TYP_REF, GTF_EXCEPT, args);
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
    GenTreePtr classHandle = impParentClassTokenToHandle(pResolvedToken);
    if (classHandle == nullptr)
    { // compDonotInline()
        return;
    }

    assert(pCallInfo->sig.numArgs);

    GenTreePtr      node;
    GenTreeArgList* args;

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
        LclVarDsc* newObjArrayArgsVar;

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
            GenTreePtr arg = impImplicitIorI4Cast(impPopStack().val, TYP_INT);

            GenTreePtr dest = gtNewLclvNode(lvaNewObjArrayArgs, TYP_BLK);
            dest            = gtNewOperNode(GT_ADDR, TYP_I_IMPL, dest);
            dest            = gtNewOperNode(GT_ADD, TYP_I_IMPL, dest,
                                 new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, sizeof(INT32) * i));
            dest = gtNewOperNode(GT_IND, TYP_INT, dest);

            node = gtNewOperNode(GT_COMMA, node->TypeGet(), gtNewAssignNode(dest, arg), node);
        }

        args = gtNewArgList(node);

        // pass number of arguments to the helper
        args = gtNewListNode(gtNewIconNode(pCallInfo->sig.numArgs), args);

        args = gtNewListNode(classHandle, args);

        node = gtNewHelperCallNode(CORINFO_HELP_NEW_MDARR_NONVARARG, TYP_REF, 0, args);
    }
    else
    {
        //
        // The varargs helper needs the type and method handles as last
        // and  last-1 param (this is a cdecl call, so args will be
        // pushed in reverse order on the CPU stack)
        //

        args = gtNewArgList(classHandle);

        // pass number of arguments to the helper
        args = gtNewListNode(gtNewIconNode(pCallInfo->sig.numArgs), args);

        unsigned argFlags = 0;
        args              = impPopList(pCallInfo->sig.numArgs, &argFlags, &pCallInfo->sig, args);

        node = gtNewHelperCallNode(CORINFO_HELP_NEW_MDARR, TYP_REF, 0, args);

        // varargs, so we pop the arguments
        node->gtFlags |= GTF_CALL_POP_ARGS;

#ifdef DEBUG
        // At the present time we don't track Caller pop arguments
        // that have GC references in them
        for (GenTreeArgList* temp = args; temp; temp = temp->Rest())
        {
            assert(temp->Current()->gtType != TYP_REF);
        }
#endif
    }

    node->gtFlags |= args->gtFlags & GTF_GLOB_EFFECT;
    node->gtCall.compileTimeHelperArgumentHandle = (CORINFO_GENERIC_HANDLE)pResolvedToken->hClass;

    // Remember that this basic block contains 'new' of a md array
    compCurBB->bbFlags |= BBF_HAS_NEWARRAY;

    impPushOnStack(node, typeInfo(TI_REF, pResolvedToken->hClass));
}

GenTreePtr Compiler::impTransformThis(GenTreePtr              thisPtr,
                                      CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                      CORINFO_THIS_TRANSFORM  transform)
{
    switch (transform)
    {
        case CORINFO_DEREF_THIS:
        {
            GenTreePtr obj = thisPtr;

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

            GenTreePtr obj = thisPtr;

            assert(obj->TypeGet() == TYP_BYREF || obj->TypeGet() == TYP_I_IMPL);
            obj = gtNewObjNode(pConstrainedResolvedToken->hClass, obj);
            obj->gtFlags |= GTF_EXCEPT;

            CorInfoType jitTyp  = info.compCompHnd->asCorInfoType(pConstrainedResolvedToken->hClass);
            var_types   objType = JITtype2varType(jitTyp);
            if (impIsPrimitive(jitTyp))
            {
                if (obj->OperIsBlk())
                {
                    obj->ChangeOperUnchecked(GT_IND);

                    // Obj could point anywhere, example a boxed class static int
                    obj->gtFlags |= GTF_IND_TGTANYWHERE;
                    obj->gtOp.gtOp2 = nullptr; // must be zero for tree walkers
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

#ifdef _TARGET_AMD64_
    // On x64, we disable pinvoke inlining inside of try regions.
    // Here is the comment from JIT64 explaining why:
    //
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
        return false;
    }
#endif // _TARGET_AMD64_

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
//   If GTF_CALL_UNMANAGED is set, increments info.compCallUnmanaged

void Compiler::impCheckForPInvokeCall(
    GenTreeCall* call, CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* sig, unsigned mflags, BasicBlock* block)
{
    CorInfoUnmanagedCallConv unmanagedCallConv;

    // If VM flagged it as Pinvoke, flag the call node accordingly
    if ((mflags & CORINFO_FLG_PINVOKE) != 0)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_PINVOKE;
    }

    if (methHnd)
    {
        if ((mflags & CORINFO_FLG_PINVOKE) == 0 || (mflags & CORINFO_FLG_NOSECURITYWRAP) == 0)
        {
            return;
        }

        unmanagedCallConv = info.compCompHnd->getUnmanagedCallConv(methHnd);
    }
    else
    {
        CorInfoCallConv callConv = CorInfoCallConv(sig->callConv & CORINFO_CALLCONV_MASK);
        if (callConv == CORINFO_CALLCONV_NATIVEVARARG)
        {
            // Used by the IL Stubs.
            callConv = CORINFO_CALLCONV_C;
        }
        static_assert_no_msg((unsigned)CORINFO_CALLCONV_C == (unsigned)CORINFO_UNMANAGED_CALLCONV_C);
        static_assert_no_msg((unsigned)CORINFO_CALLCONV_STDCALL == (unsigned)CORINFO_UNMANAGED_CALLCONV_STDCALL);
        static_assert_no_msg((unsigned)CORINFO_CALLCONV_THISCALL == (unsigned)CORINFO_UNMANAGED_CALLCONV_THISCALL);
        unmanagedCallConv = CorInfoUnmanagedCallConv(callConv);

        assert(!call->gtCallCookie);
    }

    if (unmanagedCallConv != CORINFO_UNMANAGED_CALLCONV_C && unmanagedCallConv != CORINFO_UNMANAGED_CALLCONV_STDCALL &&
        unmanagedCallConv != CORINFO_UNMANAGED_CALLCONV_THISCALL)
    {
        return;
    }
    optNativeCallCount++;

    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) && methHnd == nullptr)
    {
        // PInvoke CALLI in IL stubs must be inlined
    }
    else
    {
        // Check legality
        if (!impCanPInvokeInlineCallSite(block))
        {
            return;
        }

        // PInvoke CALL in IL stubs must be inlined on CoreRT. Skip the ambient conditions checks and
        // profitability checks
        if (!(opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) && IsTargetAbi(CORINFO_CORERT_ABI)))
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

        // The expensive check should be last
        if (info.compCompHnd->pInvokeMarshalingRequired(methHnd, sig))
        {
            return;
        }
    }

    JITLOG((LL_INFO1000000, "\nInline a CALLI PINVOKE call from method %s", info.compFullName));

    call->gtFlags |= GTF_CALL_UNMANAGED;
    info.compCallUnmanaged++;

    // AMD64 convention is same for native and managed
    if (unmanagedCallConv == CORINFO_UNMANAGED_CALLCONV_C)
    {
        call->gtFlags |= GTF_CALL_POP_ARGS;
    }

    if (unmanagedCallConv == CORINFO_UNMANAGED_CALLCONV_THISCALL)
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

    GenTreePtr fptr = impPopStack().val;

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
        lvaTable[fptr->gtLclVarCommon.gtLclNum].lvKeepType = 1;
    }
#endif

    /* Create the call node */

    GenTreeCall* call = gtNewIndCallNode(fptr, callRetTyp, nullptr, ilOffset);

    call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);

    return call;
}

/*****************************************************************************/

void Compiler::impPopArgsForUnmanagedCall(GenTreePtr call, CORINFO_SIG_INFO* sig)
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

    if (call->gtCall.gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
    {
        assert(argsToReverse);
        argsToReverse--;
    }

#ifndef _TARGET_X86_
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

    unsigned   argFlags = 0;
    GenTreePtr args     = call->gtCall.gtCallArgs =
        impPopRevList(sig->numArgs, &argFlags, sig, sig->numArgs - argsToReverse);

    if (call->gtCall.gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
    {
        GenTreePtr thisPtr = args->Current();
        impBashVarAddrsToI(thisPtr);
        assert(thisPtr->TypeGet() == TYP_I_IMPL || thisPtr->TypeGet() == TYP_BYREF);
    }

    if (args)
    {
        call->gtFlags |= args->gtFlags & GTF_GLOB_EFFECT;
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

GenTreePtr Compiler::impInitClass(CORINFO_RESOLVED_TOKEN* pResolvedToken)
{
    CorInfoInitClassResult initClassResult =
        info.compCompHnd->initClass(pResolvedToken->hField, info.compMethodHnd, impTokenLookupContextHandle);

    if ((initClassResult & CORINFO_INITCLASS_USE_HELPER) == 0)
    {
        return nullptr;
    }
    BOOL runtimeLookup;

    GenTreePtr node = impParentClassTokenToHandle(pResolvedToken, &runtimeLookup);

    if (node == nullptr)
    {
        assert(compDonotInline());
        return nullptr;
    }

    if (runtimeLookup)
    {
        node = gtNewHelperCallNode(CORINFO_HELP_INITCLASS, TYP_VOID, 0, gtNewArgList(node));
    }
    else
    {
        // Call the shared non gc static helper, as its the fastest
        node = fgGetSharedCCtor(pResolvedToken->hClass);
    }

    return node;
}

GenTreePtr Compiler::impImportStaticReadOnlyField(void* fldAddr, var_types lclTyp)
{
    GenTreePtr op1 = nullptr;

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

        case TYP_CHAR:
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
            dval = *((float*)fldAddr);
            op1  = gtNewDconNode(dval);
#if !FEATURE_X87_DOUBLES
            // X87 stack doesn't differentiate between float/double
            // so R4 is treated as R8, but everybody else does
            op1->gtType = TYP_FLOAT;
#endif // FEATURE_X87_DOUBLES
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

GenTreePtr Compiler::impImportStaticFieldAccess(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                                CORINFO_ACCESS_FLAGS    access,
                                                CORINFO_FIELD_INFO*     pFieldInfo,
                                                var_types               lclTyp)
{
    GenTreePtr op1;

    switch (pFieldInfo->fieldAccessor)
    {
        case CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER:
        {
            assert(!compIsForInlining());

            // We first call a special helper to get the statics base pointer
            op1 = impParentClassTokenToHandle(pResolvedToken);

            // compIsForInlining() is false so we should not neve get NULL here
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

            op1 = gtNewHelperCallNode(pFieldInfo->helper, type, 0, gtNewArgList(op1));

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
                unsigned callFlags = 0;

                if (info.compCompHnd->getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_BEFOREFIELDINIT)
                {
                    callFlags |= GTF_CALL_HOISTABLE;
                }

                op1 = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_STATIC_BASE, TYP_BYREF, callFlags);

                op1->gtCall.setEntryPoint(pFieldInfo->fieldLookup);
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
            noway_assert(opts.IsReadyToRun());
            CORINFO_LOOKUP_KIND kind = info.compCompHnd->getLocationOfThisType(info.compMethodHnd);
            assert(kind.needsRuntimeLookup);

            GenTreePtr      ctxTree = getRuntimeContextTree(kind.runtimeLookupKind);
            GenTreeArgList* args    = gtNewArgList(ctxTree);

            unsigned callFlags = 0;

            if (info.compCompHnd->getClassAttribs(pResolvedToken->hClass) & CORINFO_FLG_BEFOREFIELDINIT)
            {
                callFlags |= GTF_CALL_HOISTABLE;
            }
            var_types type = TYP_BYREF;
            op1            = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE, type, callFlags, args);

            op1->gtCall.setEntryPoint(pFieldInfo->fieldLookup);
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
            if (!(access & CORINFO_ACCESS_ADDRESS))
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
                    op1 =
                        gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                      new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, sizeof(void*), firstElemFldSeq));

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
            else
            {
                void** pFldAddr = nullptr;
                void*  fldAddr  = info.compCompHnd->getFieldAddress(pResolvedToken->hField, (void**)&pFldAddr);

                FieldSeqNode* fldSeq = GetFieldSeqStore()->CreateSingleton(pResolvedToken->hField);

                /* Create the data member node */
                op1 = gtNewIconHandleNode(pFldAddr == nullptr ? (size_t)fldAddr : (size_t)pFldAddr, GTF_ICON_STATIC_HDL,
                                          fldSeq);

                if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    op1->gtFlags |= GTF_ICON_INITCLASS;
                }

                if (pFldAddr != nullptr)
                {
                    // There are two cases here, either the static is RVA based,
                    // in which case the type of the FIELD node is not a GC type
                    // and the handle to the RVA is a TYP_I_IMPL.  Or the FIELD node is
                    // a GC type and the handle to it is a TYP_BYREF in the GC heap
                    // because handles to statics now go into the large object heap

                    var_types handleTyp = (var_types)(varTypeIsGC(lclTyp) ? TYP_BYREF : TYP_I_IMPL);
                    op1                 = gtNewOperNode(GT_IND, handleTyp, op1);
                    op1->gtFlags |= GTF_IND_INVARIANT | GTF_IND_NONFAULTING;
                }
            }
            break;
        }
    }

    if (pFieldInfo->fieldFlags & CORINFO_FLG_FIELD_STATIC_IN_HEAP)
    {
        op1 = gtNewOperNode(GT_IND, TYP_REF, op1);

        FieldSeqNode* fldSeq = GetFieldSeqStore()->CreateSingleton(FieldSeqStore::FirstElemPseudoField);

        op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                            new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, sizeof(void*), fldSeq));
    }

    if (!(access & CORINFO_ACCESS_ADDRESS))
    {
        op1 = gtNewOperNode(GT_IND, lclTyp, op1);
        op1->gtFlags |= GTF_GLOB_REF;
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
        case CORINFO_ACCESS_RUNTIME_CHECK:
            impInsertHelperCall(helperCall);
            break;
    }
}

void Compiler::impInsertHelperCall(CORINFO_HELPER_DESC* helperInfo)
{
    // Construct the argument list
    GenTreeArgList* args = nullptr;
    assert(helperInfo->helperNum != CORINFO_HELP_UNDEF);
    for (unsigned i = helperInfo->numArgs; i > 0; --i)
    {
        const CORINFO_HELPER_ARG& helperArg  = helperInfo->args[i - 1];
        GenTreePtr                currentArg = nullptr;
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
        args = (currentArg == nullptr) ? gtNewArgList(currentArg) : gtNewListNode(currentArg, args);
    }

    /* TODO-Review:
     * Mark as CSE'able, and hoistable.  Consider marking hoistable unless you're in the inlinee.
     * Also, consider sticking this in the first basic block.
     */
    GenTreePtr callout = gtNewHelperCallNode(helperInfo->helperNum, TYP_VOID, GTF_EXCEPT, args);
    impAppendTree(callout, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);
}

// Checks whether the return types of caller and callee are compatible
// so that callee can be tail called. Note that here we don't check
// compatibility in IL Verifier sense, but on the lines of return type
// sizes are equal and get returned in the same return register.
bool Compiler::impTailCallRetTypeCompatible(var_types            callerRetType,
                                            CORINFO_CLASS_HANDLE callerRetTypeClass,
                                            var_types            calleeRetType,
                                            CORINFO_CLASS_HANDLE calleeRetTypeClass)
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

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
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
    bool     isCallerRetTypMBEnreg =
        VarTypeIsMultiByteAndCanEnreg(callerRetType, callerRetTypeClass, &callerRetTypeSize, true);
    bool isCalleeRetTypMBEnreg =
        VarTypeIsMultiByteAndCanEnreg(calleeRetType, calleeRetTypeClass, &calleeRetTypeSize, true);

    if (varTypeIsIntegral(callerRetType) || isCallerRetTypMBEnreg)
    {
        return (varTypeIsIntegral(calleeRetType) || isCalleeRetTypMBEnreg) && (callerRetTypeSize == calleeRetTypeSize);
    }
#endif // _TARGET_AMD64_ || _TARGET_ARM64_

    return false;
}

// For prefixFlags
enum
{
    PREFIX_TAILCALL_EXPLICIT = 0x00000001, // call has "tail" IL prefix
    PREFIX_TAILCALL_IMPLICIT =
        0x00000010, // call is treated as having "tail" prefix even though there is no "tail" IL prefix
    PREFIX_TAILCALL    = (PREFIX_TAILCALL_EXPLICIT | PREFIX_TAILCALL_IMPLICIT),
    PREFIX_VOLATILE    = 0x00000100,
    PREFIX_UNALIGNED   = 0x00001000,
    PREFIX_CONSTRAINED = 0x00010000,
    PREFIX_READONLY    = 0x00100000
};

/********************************************************************************
 *
 * Returns true if the current opcode and and the opcodes following it correspond
 * to a supported tail call IL pattern.
 *
 */
bool Compiler::impIsTailCallILPattern(bool        tailPrefixed,
                                      OPCODE      curOpcode,
                                      const BYTE* codeAddrOfNextOpcode,
                                      const BYTE* codeEnd,
                                      bool        isRecursive,
                                      bool*       isCallPopAndRet /* = nullptr */)
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

    // Scan the opcodes to look for the following IL patterns if either
    //   i) the call is not tail prefixed (i.e. implicit tail call) or
    //  ii) if tail prefixed, IL verification is not needed for the method.
    //
    // Only in the above two cases we can allow the below tail call patterns
    // violating ECMA spec.
    //
    // Pattern1:
    //       call
    //       nop*
    //       ret
    //
    // Pattern2:
    //       call
    //       nop*
    //       pop
    //       nop*
    //       ret
    int    cntPop = 0;
    OPCODE nextOpcode;

#if !defined(FEATURE_CORECLR) && defined(_TARGET_AMD64_)
    do
    {
        nextOpcode = (OPCODE)getU1LittleEndian(codeAddrOfNextOpcode);
        codeAddrOfNextOpcode += sizeof(__int8);
    } while ((codeAddrOfNextOpcode < codeEnd) &&         // Haven't reached end of method
             (!tailPrefixed || !tiVerificationNeeded) && // Not ".tail" prefixed or method requires no IL verification
             ((nextOpcode == CEE_NOP) || ((nextOpcode == CEE_POP) && (++cntPop == 1)))); // Next opcode = nop or exactly
                                                                                         // one pop seen so far.
#else
    nextOpcode = (OPCODE)getU1LittleEndian(codeAddrOfNextOpcode);
#endif // !FEATURE_CORECLR && _TARGET_AMD64_

    if (isCallPopAndRet)
    {
        // Allow call+pop+ret to be tail call optimized if caller ret type is void
        *isCallPopAndRet = (nextOpcode == CEE_RET) && (cntPop == 1);
    }

#if !defined(FEATURE_CORECLR) && defined(_TARGET_AMD64_)
    // Jit64 Compat:
    // Tail call IL pattern could be either of the following
    // 1) call/callvirt/calli + ret
    // 2) call/callvirt/calli + pop + ret in a method returning void.
    return (nextOpcode == CEE_RET) && ((cntPop == 0) || ((cntPop == 1) && (info.compRetType == TYP_VOID)));
#else
    return (nextOpcode == CEE_RET) && (cntPop == 0);
#endif // !FEATURE_CORECLR && _TARGET_AMD64_
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

    if (opts.compDbgCode || opts.MinOpts())
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
                                  GenTreePtr              newobjThis,
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
    unsigned               argFlags                       = 0;
    GenTreePtr             call                           = nullptr;
    GenTreeArgList*        args                           = nullptr;
    CORINFO_THIS_TRANSFORM constraintCallThisTransform    = CORINFO_NO_THIS_TRANSFORM;
    CORINFO_CONTEXT_HANDLE exactContextHnd                = nullptr;
    bool                   exactContextNeedsRuntimeLookup = false;
    bool                   canTailCall                    = true;
    const char*            szCanTailCallFailReason        = nullptr;
    int                    tailCall                       = prefixFlags & PREFIX_TAILCALL;
    bool                   readonlyCall                   = (prefixFlags & PREFIX_READONLY) != 0;

    CORINFO_RESOLVED_TOKEN* ldftnToken = nullptr;

    // Synchronized methods need to call CORINFO_HELP_MON_EXIT at the end. We could
    // do that before tailcalls, but that is probably not the intended
    // semantic. So just disallow tailcalls from synchronized methods.
    // Also, popping arguments in a varargs function is more work and NYI
    // If we have a security object, we have to keep our frame around for callers
    // to see any imperative security.
    if (info.compFlags & CORINFO_FLG_SYNCH)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is synchronized";
    }
#if !FEATURE_FIXED_OUT_ARGS
    else if (info.compIsVarArgs)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is varargs";
    }
#endif // FEATURE_FIXED_OUT_ARGS
    else if (opts.compNeedSecurityCheck)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller requires a security check.";
    }

    // We only need to cast the return value of pinvoke inlined calls that return small types

    // TODO-AMD64-Cleanup: Remove this when we stop interoperating with JIT64, or if we decide to stop
    // widening everything! CoreCLR does not support JIT64 interoperation so no need to widen there.
    // The existing x64 JIT doesn't bother widening all types to int, so we have to assume for
    // the time being that the callee might be compiled by the other JIT and thus the return
    // value will need to be widened by us (or not widened at all...)

    // ReadyToRun code sticks with default calling convention that does not widen small return types.

    bool checkForSmallType  = opts.IsJit64Compat() || opts.IsReadyToRun();
    bool bIntrinsicImported = false;

    CORINFO_SIG_INFO calliSig;
    GenTreeArgList*  extraArg = nullptr;

    /*-------------------------------------------------------------------------
     * First create the call node
     */

    if (opcode == CEE_CALLI)
    {
        /* Get the call site sig */
        eeGetSig(pResolvedToken->token, info.compScopeHnd, impTokenLookupContextHandle, &calliSig);

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

#ifdef DEBUG
        // We cannot lazily obtain the signature of a CALLI call because it has no method
        // handle that we can use, so we need to save its full call signature here.
        assert(call->gtCall.callSig == nullptr);
        call->gtCall.callSig  = new (this, CMK_CorSig) CORINFO_SIG_INFO;
        *call->gtCall.callSig = calliSig;
#endif // DEBUG

        if (IsTargetAbi(CORINFO_CORERT_ABI))
        {
            bool managedCall = (((calliSig.callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_STDCALL) &&
                                ((calliSig.callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_C) &&
                                ((calliSig.callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_THISCALL) &&
                                ((calliSig.callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_FASTCALL));
            if (managedCall)
            {
                addFatPointerCandidate(call->AsCall());
            }
        }
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
                return callRetTyp;
            }

            /* Does the inlinee need a security check token on the frame */

            if (mflags & CORINFO_FLG_SECURITYCHECK)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_NEEDS_SECURITY_CHECK);
                return callRetTyp;
            }

            /* Does the inlinee use StackCrawlMark */

            if (mflags & CORINFO_FLG_DONT_INLINE_CALLER)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_STACK_CRAWL_MARK);
                return callRetTyp;
            }

            /* For now ignore delegate invoke */

            if (mflags & CORINFO_FLG_DELEGATE_INVOKE)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_DELEGATE_INVOKE);
                return callRetTyp;
            }

            /* For now ignore varargs */
            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_NATIVE_VARARGS);
                return callRetTyp;
            }

            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_MANAGED_VARARGS);
                return callRetTyp;
            }

            if ((mflags & CORINFO_FLG_VIRTUAL) && (sig->sigInst.methInstCount != 0) && (opcode == CEE_CALLVIRT))
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_GENERIC_VIRTUAL);
                return callRetTyp;
            }
        }

        clsHnd = pResolvedToken->hClass;

        clsFlags = callInfo->classFlags;

#ifdef DEBUG
        // If this is a call to JitTestLabel.Mark, do "early inlining", and record the test attribute.

        // This recognition should really be done by knowing the methHnd of the relevant Mark method(s).
        // These should be in mscorlib.h, and available through a JIT/EE interface call.
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
        if ((mflags & CORINFO_FLG_INTRINSIC) && !pConstrainedResolvedToken)
        {
            call = impIntrinsic(newobjThis, clsHnd, methHnd, sig, pResolvedToken->token, readonlyCall,
                                (canTailCall && (tailCall != 0)), &intrinsicID);

            if (call != nullptr)
            {
                assert(!(mflags & CORINFO_FLG_VIRTUAL) || (mflags & CORINFO_FLG_FINAL) ||
                       (clsFlags & CORINFO_FLG_FINAL));

#ifdef FEATURE_READYTORUN_COMPILER
                if (call->OperGet() == GT_INTRINSIC)
                {
                    if (opts.IsReadyToRun())
                    {
                        noway_assert(callInfo->kind == CORINFO_CALL);
                        call->gtIntrinsic.gtEntryPoint = callInfo->codePointerLookup.constLookup;
                    }
                    else
                    {
                        call->gtIntrinsic.gtEntryPoint.addr = nullptr;
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
            call = impSIMDIntrinsic(opcode, newobjThis, clsHnd, methHnd, sig, pResolvedToken->token);
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
        exactContextNeedsRuntimeLookup = callInfo->exactContextNeedsRuntimeLookup == TRUE;

        // Recursive call is treaded as a loop to the begining of the method.
        if (methHnd == info.compMethodHnd)
        {
#ifdef DEBUG
            if (verbose)
            {
                JITDUMP("\nFound recursive call in the method. Mark BB%02u to BB%02u as having a backward branch.\n",
                        fgFirstBB->bbNum, compCurBB->bbNum);
            }
#endif
            fgMarkBackwardJump(fgFirstBB, compCurBB);
        }

        switch (callInfo->kind)
        {

            case CORINFO_VIRTUALCALL_STUB:
            {
                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                if (callInfo->stubLookup.lookupKind.needsRuntimeLookup)
                {

                    if (compIsForInlining())
                    {
                        // Don't import runtime lookups when inlining
                        // Inlining has to be aborted in such a case
                        /* XXX Fri 3/20/2009
                         * By the way, this would never succeed.  If the handle lookup is into the generic
                         * dictionary for a candidate, you'll generate different dictionary offsets and the
                         * inlined code will crash.
                         *
                         * To anyone code reviewing this, when could this ever succeed in the future?  It'll
                         * always have a handle lookup.  These lookups are safe intra-module, but we're just
                         * failing here.
                         */
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_COMPLEX_HANDLE);
                        return callRetTyp;
                    }

                    GenTreePtr stubAddr = impRuntimeLookupToTree(pResolvedToken, &callInfo->stubLookup, methHnd);
                    assert(!compDonotInline());

                    // This is the rough code to set up an indirect stub call
                    assert(stubAddr != nullptr);

                    // The stubAddr may be a
                    // complex expression. As it is evaluated after the args,
                    // it may cause registered args to be spilled. Simply spill it.

                    unsigned lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall with runtime lookup"));
                    impAssignTempGen(lclNum, stubAddr, (unsigned)CHECK_SPILL_ALL);
                    stubAddr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                    // Create the actual call node

                    assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
                           (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);

                    call = gtNewIndCallNode(stubAddr, callRetTyp, nullptr);

                    call->gtFlags |= GTF_EXCEPT | (stubAddr->gtFlags & GTF_GLOB_EFFECT);
                    call->gtFlags |= GTF_CALL_VIRT_STUB;

#ifdef _TARGET_X86_
                    // No tailcalls allowed for these yet...
                    canTailCall             = false;
                    szCanTailCallFailReason = "VirtualCall with runtime lookup";
#endif
                }
                else
                {
                    // ok, the stub is available at compile type.

                    call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, nullptr, ilOffset);
                    call->gtCall.gtStubCallStubAddr = callInfo->stubLookup.constLookup.addr;
                    call->gtFlags |= GTF_CALL_VIRT_STUB;
                    assert(callInfo->stubLookup.constLookup.accessType != IAT_PPVALUE);
                    if (callInfo->stubLookup.constLookup.accessType == IAT_PVALUE)
                    {
                        call->gtCall.gtCallMoreFlags |= GTF_CALL_M_VIRTSTUB_REL_INDIRECT;
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
                break;
            }

            case CORINFO_VIRTUALCALL_LDVIRTFTN:
            {
                if (compIsForInlining())
                {
                    compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_CALL_VIA_LDVIRTFTN);
                    return callRetTyp;
                }

                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                // OK, We've been told to call via LDVIRTFTN, so just
                // take the call now....

                args = impPopList(sig->numArgs, &argFlags, sig);

                GenTreePtr thisPtr = impPopStack().val;
                thisPtr            = impTransformThis(thisPtr, pConstrainedResolvedToken, callInfo->thisTransform);
                if (compDonotInline())
                {
                    return callRetTyp;
                }

                // Clone the (possibly transformed) "this" pointer
                GenTreePtr thisPtrCopy;
                thisPtr = impCloneExpr(thisPtr, &thisPtrCopy, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("LDVIRTFTN this pointer"));

                GenTreePtr fptr = impImportLdvirtftn(thisPtr, pResolvedToken, callInfo);

                if (compDonotInline())
                {
                    return callRetTyp;
                }

                thisPtr = nullptr; // can't reuse it

                // Now make an indirect call through the function pointer

                unsigned lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall through function pointer"));
                impAssignTempGen(lclNum, fptr, (unsigned)CHECK_SPILL_ALL);
                fptr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                // Create the actual call node

                call                    = gtNewIndCallNode(fptr, callRetTyp, args, ilOffset);
                call->gtCall.gtCallObjp = thisPtrCopy;
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

                // We remove the nullcheck for the GetType call instrinsic.
                // TODO-CQ: JIT64 does not introduce the null check for many more helper calls
                // and instrinsics.
                if (callInfo->nullInstanceCheck &&
                    !((mflags & CORINFO_FLG_INTRINSIC) != 0 && (intrinsicID == CORINFO_INTRINSIC_Object_GetType)))
                {
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    call->gtCall.setEntryPoint(callInfo->codePointerLookup.constLookup);
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

                GenTreePtr fptr =
                    impLookupToTree(pResolvedToken, &callInfo->codePointerLookup, GTF_ICON_FTN_ADDR, callInfo->hMethod);

                if (compDonotInline())
                {
                    return callRetTyp;
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
            call->gtCall.gtCallMoreFlags |= GTF_CALL_M_NOGCCHECK;
        }

        // Mark call if it's one of the ones we will maybe treat as an intrinsic
        if (intrinsicID == CORINFO_INTRINSIC_Object_GetType || intrinsicID == CORINFO_INTRINSIC_TypeEQ ||
            intrinsicID == CORINFO_INTRINSIC_TypeNEQ || intrinsicID == CORINFO_INTRINSIC_GetCurrentManagedThread ||
            intrinsicID == CORINFO_INTRINSIC_GetManagedThreadId)
        {
            call->gtCall.gtCallMoreFlags |= GTF_CALL_M_SPECIAL_INTRINSIC;
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
        assert(!compIsForInlining());
        assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
        assert(mflags & CORINFO_FLG_FINAL);

        /* Set the delegate flag */
        call->gtCall.gtCallMoreFlags |= GTF_CALL_M_DELEGATE_INV;

        if (callInfo->secureDelegateInvoke)
        {
            call->gtCall.gtCallMoreFlags |= GTF_CALL_M_SECURE_DELEGATE_INV;
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
    if (varTypeIsStruct(callRetTyp))
    {
        callRetTyp   = impNormStructType(actualMethodRetTypeSigClass);
        call->gtType = callRetTyp;
    }

#if !FEATURE_VARARG
    /* Check for varargs */
    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        BADCODE("Varargs not supported.");
    }
#endif // !FEATURE_VARARG

#ifdef UNIX_X86_ABI
    if (call->gtCall.callSig == nullptr)
    {
        call->gtCall.callSig  = new (this, CMK_CorSig) CORINFO_SIG_INFO;
        *call->gtCall.callSig = *sig;
    }
#endif // UNIX_X86_ABI

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        assert(!compIsForInlining());

        /* Set the right flags */

        call->gtFlags |= GTF_CALL_POP_ARGS;
        call->gtCall.gtCallMoreFlags |= GTF_CALL_M_VARARGS;

        /* Can't allow tailcall for varargs as it is caller-pop. The caller
           will be expecting to pop a certain number of arguments, but if we
           tailcall to a function with a different number of arguments, we
           are hosed. There are ways around this (caller remembers esp value,
           varargs is not caller-pop, etc), but not worth it. */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
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
            eeGetCallSiteSig(pResolvedToken->token, info.compScopeHnd, impTokenLookupContextHandle, sig);

#ifdef DEBUG
            // We cannot lazily obtain the signature of a vararg call because using its method
            // handle will give us only the declared argument list, not the full argument list.
            assert(call->gtCall.callSig == nullptr);
            call->gtCall.callSig  = new (this, CMK_CorSig) CORINFO_SIG_INFO;
            *call->gtCall.callSig = *sig;
#endif

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

    if (mflags & CORINFO_FLG_SECURITYCHECK)
    {
        assert(!compIsForInlining());

        // Need security prolog/epilog callouts when there is
        // imperative security in the method. This is to give security a
        // chance to do any setup in the prolog and cleanup in the epilog if needed.

        if (compIsForInlining())
        {
            // Cannot handle this if the method being imported is an inlinee by itself.
            // Because inlinee method does not have its own frame.

            compInlineResult->NoteFatal(InlineObservation::CALLEE_NEEDS_SECURITY_CHECK);
            return callRetTyp;
        }
        else
        {
            tiSecurityCalloutNeeded = true;

            // If the current method calls a method which needs a security check,
            // (i.e. the method being compiled has imperative security)
            // we need to reserve a slot for the security object in
            // the current method's stack frame
            opts.compNeedSecurityCheck = true;
        }
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
    else if ((opcode == CEE_CALLI) && (((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_STDCALL) ||
                                       ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_C) ||
                                       ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_THISCALL) ||
                                       ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_FASTCALL)))
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

            return callRetTyp;
        }

        GenTreePtr cookie = eeGetPInvokeCookie(sig);

        // This cookie is required to be either a simple GT_CNS_INT or
        // an indirection of a GT_CNS_INT
        //
        GenTreePtr cookieConst = cookie;
        if (cookie->gtOper == GT_IND)
        {
            cookieConst = cookie->gtOp.gtOp1;
        }
        assert(cookieConst->gtOper == GT_CNS_INT);

        // Setting GTF_DONT_CSE on the GT_CNS_INT as well as on the GT_IND (if it exists) will ensure that
        // we won't allow this tree to participate in any CSE logic
        //
        cookie->gtFlags |= GTF_DONT_CSE;
        cookieConst->gtFlags |= GTF_DONT_CSE;

        call->gtCall.gtCallCookie = cookie;

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
            return callRetTyp;
        }

        varCookie = info.compCompHnd->getVarArgsHandle(sig, &pVarCookie);
        assert((!varCookie) != (!pVarCookie));
        GenTreePtr cookie = gtNewIconEmbHndNode(varCookie, pVarCookie, GTF_ICON_VARG_HDL);

        assert(extraArg == nullptr);
        extraArg = gtNewArgList(cookie);
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
        assert(call->gtCall.gtCallType == CT_USER_FUNC);
        if (clsHnd == nullptr)
        {
            NO_WAY("CALLI on parameterized type");
        }

        assert(opcode != CEE_CALLI);

        GenTreePtr instParam;
        BOOL       runtimeLookup;

        // Instantiated generic method
        if (((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
        {
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
                        return callRetTyp;
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
                instParam = impTokenToHandle(pResolvedToken, &runtimeLookup, TRUE /*mustRestoreHandle*/);
                if (instParam == nullptr)
                {
                    return callRetTyp;
                }
            }
        }

        // otherwise must be an instance method in a generic struct,
        // a static method in a generic type, or a runtime-generated array method
        else
        {
            assert(((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
            CORINFO_CLASS_HANDLE exactClassHandle =
                (CORINFO_CLASS_HANDLE)((SIZE_T)exactContextHnd & ~CORINFO_CONTEXTFLAGS_MASK);

            if (compIsForInlining() && (clsFlags & CORINFO_FLG_ARRAY) != 0)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_ARRAY_METHOD);
                return callRetTyp;
            }

            if ((clsFlags & CORINFO_FLG_ARRAY) && readonlyCall)
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
                        return callRetTyp;
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
                instParam = impParentClassTokenToHandle(pResolvedToken, &runtimeLookup, TRUE /*mustRestoreHandle*/);
                if (instParam == nullptr)
                {
                    return callRetTyp;
                }
            }
        }

        assert(extraArg == nullptr);
        extraArg = gtNewArgList(instParam);
    }

    // Inlining may need the exact type context (exactContextHnd) if we're inlining shared generic code, in particular
    // to inline 'polytypic' operations such as static field accesses, type tests and method calls which
    // rely on the exact context. The exactContextHnd is passed back to the JitInterface at appropriate points.
    // exactContextHnd is not currently required when inlining shared generic code into shared
    // generic code, since the inliner aborts whenever shared code polytypic operations are encountered
    // (e.g. anything marked needsRuntimeLookup)
    if (exactContextNeedsRuntimeLookup)
    {
        exactContextHnd = nullptr;
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

    args = call->gtCall.gtCallArgs = impPopList(sig->numArgs, &argFlags, sig, extraArg);

    if (args)
    {
        call->gtFlags |= args->gtFlags & GTF_GLOB_EFFECT;
    }

    //-------------------------------------------------------------------------
    // The "this" pointer

    if (!(mflags & CORINFO_FLG_STATIC) && !((opcode == CEE_NEWOBJ) && (newobjThis == nullptr)))
    {
        GenTreePtr obj;

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
                return callRetTyp;
            }
        }

        /* Is this a virtual or interface call? */

        if ((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) != GTF_CALL_NONVIRT)
        {
            /* only true object pointers can be virtual */
            assert(obj->gtType == TYP_REF);

            // See if we can devirtualize.
            impDevirtualizeCall(call->AsCall(), obj, callInfo, &exactContextHnd);
        }
        else
        {
            if (impIsThis(obj))
            {
                call->gtCall.gtCallMoreFlags |= GTF_CALL_M_NONVIRT_SAME_THIS;
            }
        }

        /* Store the "this" value in the call */

        call->gtFlags |= obj->gtFlags & GTF_GLOB_EFFECT;
        call->gtCall.gtCallObjp = obj;
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
                call->gtCall.gtRawILOffset = rawILOffset;

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
                assert(newobjThis->gtOper == GT_ADDR && newobjThis->gtOp.gtOp1->gtOper == GT_LCL_VAR);

                unsigned tmp = newobjThis->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
                impPushOnStack(gtNewLclvNode(tmp, lvaGetRealType(tmp)), verMakeTypeInfo(clsHnd).NormaliseForStack());
            }
            else
            {
                if (newobjThis->gtOper == GT_COMMA)
                {
                    // In coreclr the callout can be inserted even if verification is disabled
                    // so we cannot rely on tiVerificationNeeded alone

                    // We must have inserted the callout. Get the real newobj.
                    newobjThis = newobjThis->gtOp.gtOp2;
                }

                assert(newobjThis->gtOper == GT_LCL_VAR);
                impPushOnStack(gtNewLclvNode(newobjThis->gtLclVarCommon.gtLclNum, TYP_REF), typeInfo(TI_REF, clsHnd));
            }
        }
        return callRetTyp;
    }

DONE:

    if (tailCall)
    {
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
        if ((tailCall & PREFIX_TAILCALL_EXPLICIT) && (verCurrentState.esStackDepth != 0))
        {
            BADCODE("Stack should be empty after tailcall");
        }

        // Note that we can not relax this condition with genActualType() as
        // the calling convention dictates that the caller of a function with
        // a small-typed return value is responsible for normalizing the return val

        if (canTailCall &&
            !impTailCallRetTypeCompatible(info.compRetType, info.compMethodInfo->args.retTypeClass, callRetTyp,
                                          callInfo->sig.retTypeClass))
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Return types are not tail call compatible";
        }

        // Stack empty check for implicit tail calls.
        if (canTailCall && (tailCall & PREFIX_TAILCALL_IMPLICIT) && (verCurrentState.esStackDepth != 0))
        {
#ifdef _TARGET_AMD64_
            // JIT64 Compatibility:  Opportunistic tail call stack mismatch throws a VerificationException
            // in JIT64, not an InvalidProgramException.
            Verify(false, "Stack should be empty after tailcall");
#else  // _TARGET_64BIT_
            BADCODE("Stack should be empty after tailcall");
#endif //!_TARGET_64BIT_
        }

        // assert(compCurBB is not a catch, finally or filter block);
        // assert(compCurBB is not a try block protected by a finally block);

        // Check for permission to tailcall
        bool explicitTailCall = (tailCall & PREFIX_TAILCALL_EXPLICIT) != 0;

        assert(!explicitTailCall || compCurBB->bbJumpKind == BBJ_RETURN);

        if (canTailCall)
        {
            // True virtual or indirect calls, shouldn't pass in a callee handle.
            CORINFO_METHOD_HANDLE exactCalleeHnd = ((call->gtCall.gtCallType != CT_USER_FUNC) ||
                                                    ((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) != GTF_CALL_NONVIRT))
                                                       ? nullptr
                                                       : methHnd;
            GenTreePtr thisArg = call->gtCall.gtCallObjp;

            if (info.compCompHnd->canTailCall(info.compMethodHnd, methHnd, exactCalleeHnd, explicitTailCall))
            {
                canTailCall = true;
                if (explicitTailCall)
                {
                    // In case of explicit tail calls, mark it so that it is not considered
                    // for in-lining.
                    call->gtCall.gtCallMoreFlags |= GTF_CALL_M_EXPLICIT_TAILCALL;
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nGTF_CALL_M_EXPLICIT_TAILCALL bit set for call ");
                        printTreeID(call);
                        printf("\n");
                    }
#endif
                }
                else
                {
#if FEATURE_TAILCALL_OPT
                    // Must be an implicit tail call.
                    assert((tailCall & PREFIX_TAILCALL_IMPLICIT) != 0);

                    // It is possible that a call node is both an inline candidate and marked
                    // for opportunistic tail calling.  In-lining happens before morhphing of
                    // trees.  If in-lining of an in-line candidate gets aborted for whatever
                    // reason, it will survive to the morphing stage at which point it will be
                    // transformed into a tail call after performing additional checks.

                    call->gtCall.gtCallMoreFlags |= GTF_CALL_M_IMPLICIT_TAILCALL;
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nGTF_CALL_M_IMPLICIT_TAILCALL bit set for call ");
                        printTreeID(call);
                        printf("\n");
                    }
#endif

#else //! FEATURE_TAILCALL_OPT
                    NYI("Implicit tail call prefix on a target which doesn't support opportunistic tail calls");

#endif // FEATURE_TAILCALL_OPT
                }

                // we can't report success just yet...
            }
            else
            {
                canTailCall = false;
// canTailCall reported its reasons already
#ifdef DEBUG
                if (verbose)
                {
                    printf("\ninfo.compCompHnd->canTailCall returned false for call ");
                    printTreeID(call);
                    printf("\n");
                }
#endif
            }
        }
        else
        {
            // If this assert fires it means that canTailCall was set to false without setting a reason!
            assert(szCanTailCallFailReason != nullptr);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nRejecting %splicit tail call for call ", explicitTailCall ? "ex" : "im");
                printTreeID(call);
                printf(": %s\n", szCanTailCallFailReason);
            }
#endif
            info.compCompHnd->reportTailCallDecision(info.compMethodHnd, methHnd, explicitTailCall, TAILCALL_FAIL,
                                                     szCanTailCallFailReason);
        }
    }

    // Note: we assume that small return types are already normalized by the managed callee
    // or by the pinvoke stub for calls to unmanaged code.

    if (!bIntrinsicImported)
    {
        //
        // Things needed to be checked when bIntrinsicImported is false.
        //

        assert(call->gtOper == GT_CALL);
        assert(sig != nullptr);

        // Tail calls require us to save the call site's sig info so we can obtain an argument
        // copying thunk from the EE later on.
        if (call->gtCall.callSig == nullptr)
        {
            call->gtCall.callSig  = new (this, CMK_CorSig) CORINFO_SIG_INFO;
            *call->gtCall.callSig = *sig;
        }

        if (compIsForInlining() && opcode == CEE_CALLVIRT)
        {
            GenTreePtr callObj = call->gtCall.gtCallObjp;
            assert(callObj != nullptr);

            unsigned callKind = call->gtFlags & GTF_CALL_VIRT_KIND_MASK;

            if (((callKind != GTF_CALL_NONVIRT) || (call->gtFlags & GTF_CALL_NULLCHECK)) &&
                impInlineIsGuaranteedThisDerefBeforeAnySideEffects(call->gtCall.gtCallArgs, callObj,
                                                                   impInlineInfo->inlArgInfo))
            {
                impInlineInfo->thisDereferencedFirst = true;
            }
        }

#if defined(DEBUG) || defined(INLINE_DATA)

        // Keep track of the raw IL offset of the call
        call->gtCall.gtRawILOffset = rawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

        // Is it an inline candidate?
        impMarkInlineCandidate(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo);
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
        if ((clsFlags & CORINFO_FLG_ARRAY) && readonlyCall && tiRetVal.IsByRef())
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

            bool fatPointerCandidate = call->AsCall()->IsFatPointerCandidate();
            if (varTypeIsStruct(callRetTyp))
            {
                call = impFixupCallStructReturn(call->AsCall(), sig->retTypeClass);
            }

            if ((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0)
            {
                assert(opts.OptEnabled(CLFLG_INLINING));
                assert(!fatPointerCandidate); // We should not try to inline calli.

                // Make the call its own tree (spill the stack if needed).
                impAppendTree(call, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);

                // TODO: Still using the widened type.
                call = gtNewInlineCandidateReturnExpr(call, genActualType(callRetTyp));
            }
            else
            {
                if (fatPointerCandidate)
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
                    if ((callNode->gtCallType == CT_HELPER) && gtIsTypeHandleToRuntimeTypeHelper(callNode))
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
                call = gtNewCastNode(genActualType(callRetTyp), call, callRetTyp);
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

bool Compiler::impMethodInfo_hasRetBuffArg(CORINFO_METHOD_INFO* methInfo)
{
    CorInfoType corType = methInfo->args.retType;

    if ((corType == CORINFO_TYPE_VALUECLASS) || (corType == CORINFO_TYPE_REFANY))
    {
        // We have some kind of STRUCT being returned

        structPassingKind howToReturnStruct = SPK_Unknown;

        var_types returnType = getReturnTypeForStruct(methInfo->args.retTypeClass, &howToReturnStruct);

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
        GenTreePtr val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_tl = (TestLabel)val->AsIntConCommon()->IconValue();
    }
    else if (numArgs == 3)
    {
        StackEntry se = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        GenTreePtr val = se.val;
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
    GenTreePtr node  = expSe.val;

    // There are a small number of special cases, where we actually put the annotation on a subnode.
    if (tlAndN.m_tl == TL_LoopHoist && tlAndN.m_num >= 100)
    {
        // A loop hoist annotation with value >= 100 means that the expression should be a static field access,
        // a GT_IND of a static field address, which should be the sum of a (hoistable) helper call and possibly some
        // offset within the the static field block whose address is returned by the helper call.
        // The annotation is saying that this address calculation, but not the entire access, should be hoisted.
        GenTreePtr helperCall = nullptr;
        assert(node->OperGet() == GT_IND);
        tlAndN.m_num -= 100;
        GetNodeTestData()->Set(node->gtOp.gtOp1, tlAndN);
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
GenTreePtr Compiler::impFixupCallStructReturn(GenTreeCall* call, CORINFO_CLASS_HANDLE retClsHnd)
{
    if (!varTypeIsStruct(call))
    {
        return call;
    }

    call->gtRetClsHnd = retClsHnd;

#if FEATURE_MULTIREG_RET
    // Initialize Return type descriptor of call node
    ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    retTypeDesc->InitializeStructReturnType(this, retClsHnd);
#endif // FEATURE_MULTIREG_RET

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

    // Not allowed for FEATURE_CORCLR which is the only SKU available for System V OSs.
    assert(!call->IsVarargs() && "varargs not allowed for System V OSs.");

    // The return type will remain as the incoming struct type unless normalized to a
    // single eightbyte return type below.
    call->gtReturnType = call->gtType;

    unsigned retRegCount = retTypeDesc->GetReturnRegCount();
    if (retRegCount != 0)
    {
        if (retRegCount == 1)
        {
            // struct returned in a single register
            call->gtReturnType = retTypeDesc->GetReturnRegType(0);
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
                return impAssignMultiRegTypeToVar(call, retClsHnd);
            }
        }
    }
    else
    {
        // struct not returned in registers i.e returned via hiddden retbuf arg.
        call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;
    }

#else // not FEATURE_UNIX_AMD64_STRUCT_PASSING

#if FEATURE_MULTIREG_RET && defined(_TARGET_ARM_)
    // There is no fixup necessary if the return type is a HFA struct.
    // HFA structs are returned in registers for ARM32 and ARM64
    //
    if (!call->IsVarargs() && IsHfa(retClsHnd))
    {
        if (call->CanTailCall())
        {
            if (info.compIsVarArgs)
            {
                // We cannot tail call because control needs to return to fixup the calling
                // convention for result return.
                call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
            }
            else
            {
                // If we can tail call returning HFA, then don't assign it to
                // a variable back and forth.
                return call;
            }
        }

        if (call->gtFlags & GTF_CALL_INLINE_CANDIDATE)
        {
            return call;
        }

        unsigned retRegCount = retTypeDesc->GetReturnRegCount();
        if (retRegCount >= 2)
        {
            return impAssignMultiRegTypeToVar(call, retClsHnd);
        }
    }
#endif // _TARGET_ARM_

    // Check for TYP_STRUCT type that wraps a primitive type
    // Such structs are returned using a single register
    // and we change the return type on those calls here.
    //
    structPassingKind howToReturnStruct;
    var_types         returnType = getReturnTypeForStruct(retClsHnd, &howToReturnStruct);

    if (howToReturnStruct == SPK_ByReference)
    {
        assert(returnType == TYP_UNKNOWN);
        call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;
    }
    else
    {
        assert(returnType != TYP_UNKNOWN);
        call->gtReturnType = returnType;

        // ToDo: Refactor this common code sequence into its own method as it is used 4+ times
        if ((returnType == TYP_LONG) && (compLongUsed == false))
        {
            compLongUsed = true;
        }
        else if (((returnType == TYP_FLOAT) || (returnType == TYP_DOUBLE)) && (compFloatingPointUsed == false))
        {
            compFloatingPointUsed = true;
        }

#if FEATURE_MULTIREG_RET
        unsigned retRegCount = retTypeDesc->GetReturnRegCount();
        assert(retRegCount != 0);

        if (retRegCount >= 2)
        {
            if ((!call->CanTailCall()) && (!call->IsInlineCandidate()))
            {
                // Force a call returning multi-reg struct to be always of the IR form
                //   tmp = call
                //
                // No need to assign a multi-reg struct to a local var if:
                //  - It is a tail call or
                //  - The call is marked for in-lining later
                return impAssignMultiRegTypeToVar(call, retClsHnd);
            }
        }
#endif // FEATURE_MULTIREG_RET
    }

#endif // not FEATURE_UNIX_AMD64_STRUCT_PASSING

    return call;
}

/*****************************************************************************
   For struct return values, re-type the operand in the case where the ABI
   does not use a struct return buffer
   Note that this method is only call for !_TARGET_X86_
 */

GenTreePtr Compiler::impFixupStructReturnType(GenTreePtr op, CORINFO_CLASS_HANDLE retClsHnd)
{
    assert(varTypeIsStruct(info.compRetType));
    assert(info.compRetBuffArg == BAD_VAR_NUM);

#if defined(_TARGET_XARCH_)

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    // No VarArgs for CoreCLR on x64 Unix
    assert(!info.compIsVarArgs);

    // Is method returning a multi-reg struct?
    if (varTypeIsStruct(info.compRetNativeType) && IsMultiRegReturnedType(retClsHnd))
    {
        // In case of multi-reg struct return, we force IR to be one of the following:
        // GT_RETURN(lclvar) or GT_RETURN(call).  If op is anything other than a
        // lclvar or call, it is assigned to a temp to create: temp = op and GT_RETURN(tmp).

        if (op->gtOper == GT_LCL_VAR)
        {
            // Make sure that this struct stays in memory and doesn't get promoted.
            unsigned lclNum                  = op->gtLclVarCommon.gtLclNum;
            lvaTable[lclNum].lvIsMultiRegRet = true;

            // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
            op->gtFlags |= GTF_DONT_CSE;

            return op;
        }

        if (op->gtOper == GT_CALL)
        {
            return op;
        }

        return impAssignMultiRegTypeToVar(op, retClsHnd);
    }
#else  // !FEATURE_UNIX_AMD64_STRUCT_PASSING
    assert(info.compRetNativeType != TYP_STRUCT);
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

#elif FEATURE_MULTIREG_RET && defined(_TARGET_ARM_)

    if (varTypeIsStruct(info.compRetNativeType) && !info.compIsVarArgs && IsHfa(retClsHnd))
    {
        if (op->gtOper == GT_LCL_VAR)
        {
            // This LCL_VAR is an HFA return value, it stays as a TYP_STRUCT
            unsigned lclNum = op->gtLclVarCommon.gtLclNum;
            // Make sure this struct type stays as struct so that we can return it as an HFA
            lvaTable[lclNum].lvIsMultiRegRet = true;

            // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
            op->gtFlags |= GTF_DONT_CSE;

            return op;
        }

        if (op->gtOper == GT_CALL)
        {
            if (op->gtCall.IsVarargs())
            {
                // We cannot tail call because control needs to return to fixup the calling
                // convention for result return.
                op->gtCall.gtCallMoreFlags &= ~GTF_CALL_M_TAILCALL;
                op->gtCall.gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
            }
            else
            {
                return op;
            }
        }
        return impAssignMultiRegTypeToVar(op, retClsHnd);
    }

#elif FEATURE_MULTIREG_RET && defined(_TARGET_ARM64_)

    // Is method returning a multi-reg struct?
    if (IsMultiRegReturnedType(retClsHnd))
    {
        if (op->gtOper == GT_LCL_VAR)
        {
            // This LCL_VAR stays as a TYP_STRUCT
            unsigned lclNum = op->gtLclVarCommon.gtLclNum;

            // Make sure this struct type is not struct promoted
            lvaTable[lclNum].lvIsMultiRegRet = true;

            // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
            op->gtFlags |= GTF_DONT_CSE;

            return op;
        }

        if (op->gtOper == GT_CALL)
        {
            if (op->gtCall.IsVarargs())
            {
                // We cannot tail call because control needs to return to fixup the calling
                // convention for result return.
                op->gtCall.gtCallMoreFlags &= ~GTF_CALL_M_TAILCALL;
                op->gtCall.gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
            }
            else
            {
                return op;
            }
        }
        return impAssignMultiRegTypeToVar(op, retClsHnd);
    }

#endif //  FEATURE_MULTIREG_RET && FEATURE_HFA

REDO_RETURN_NODE:
    // adjust the type away from struct to integral
    // and no normalizing
    if (op->gtOper == GT_LCL_VAR)
    {
        op->ChangeOper(GT_LCL_FLD);
    }
    else if (op->gtOper == GT_OBJ)
    {
        GenTreePtr op1 = op->AsObj()->Addr();

        // We will fold away OBJ/ADDR
        // except for OBJ/ADDR/INDEX
        //     as the array type influences the array element's offset
        //     Later in this method we change op->gtType to info.compRetNativeType
        //     This is not correct when op is a GT_INDEX as the starting offset
        //     for the array elements 'elemOffs' is different for an array of
        //     TYP_REF than an array of TYP_STRUCT (which simply wraps a TYP_REF)
        //     Also refer to the GTF_INX_REFARR_LAYOUT flag
        //
        if ((op1->gtOper == GT_ADDR) && (op1->gtOp.gtOp1->gtOper != GT_INDEX))
        {
            // Change '*(&X)' to 'X' and see if we can do better
            op = op1->gtOp.gtOp1;
            goto REDO_RETURN_NODE;
        }
        op->gtObj.gtClass = NO_CLASS_HANDLE;
        op->ChangeOperUnchecked(GT_IND);
        op->gtFlags |= GTF_IND_TGTANYWHERE;
    }
    else if (op->gtOper == GT_CALL)
    {
        if (op->AsCall()->TreatAsHasRetBufArg(this))
        {
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

            // Don't create both a GT_ADDR & GT_OBJ just to undo all of that; instead,
            // jump directly to a GT_LCL_FLD.
            op = gtNewLclvNode(tmpNum, info.compRetNativeType);
            op->ChangeOper(GT_LCL_FLD);
        }
        else
        {
            assert(info.compRetNativeType == op->gtCall.gtReturnType);

            // Don't change the gtType of the node just yet, it will get changed later.
            return op;
        }
    }
    else if (op->gtOper == GT_COMMA)
    {
        op->gtOp.gtOp2 = impFixupStructReturnType(op->gtOp.gtOp2, retClsHnd);
    }

    op->gtType = info.compRetNativeType;

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

#if !FEATURE_EH_FUNCLETS

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
    GenTreePtr  endCatches   = NULL;
    GenTreePtr  endLFin      = NULL; // The statement tree to indicate the end of locally-invoked finally.

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
            GenTreePtr endCatch = gtNewHelperCallNode(CORINFO_HELP_ENDCATCH, TYP_VOID);

            // Make a list of all the currently pending endCatches
            if (endCatches)
                endCatches = gtNewOperNode(GT_COMMA, TYP_VOID, endCatches, endCatch);
            else
                endCatches = endCatch;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - BB%02u jumping out of catch handler EH#%u, adding call to "
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

            assert(!encFinallies == !endLFin); // if we have finallies, we better have an endLFin tree, and vice-versa

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
                           "block BB%02u [%08p]\n",
                           callBlock->bbNum, dspPtr(callBlock));
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
                callBlock->setBBWeight(block->bbWeight);
                callBlock->bbFlags |= block->bbFlags & BBF_RUN_RARELY;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try, new BBJ_CALLFINALLY block BB%02u "
                           "[%08p]\n",
                           callBlock->bbNum, dspPtr(callBlock));
                }
#endif

                GenTreePtr lastStmt;

                if (endCatches)
                {
                    lastStmt         = gtNewStmt(endCatches);
                    endLFin->gtNext  = lastStmt;
                    lastStmt->gtPrev = endLFin;
                }
                else
                {
                    lastStmt = endLFin;
                }

                // note that this sets BBF_IMPORTED on the block
                impEndTreeList(callBlock, endLFin, lastStmt);
            }

            step = fgNewBBafter(BBJ_ALWAYS, callBlock, true);
            /* The new block will inherit this block's weight */
            step->setBBWeight(block->bbWeight);
            step->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED | BBF_KEEP_BBJ_ALWAYS;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - jumping out of a finally-protected try, created step (BBJ_ALWAYS) block "
                       "BB%02u [%08p]\n",
                       step->bbNum, dspPtr(step));
            }
#endif

            unsigned finallyNesting = compHndBBtab[XTnum].ebdHandlerNestingLevel;
            assert(finallyNesting <= compHndBBtabCount);

            callBlock->bbJumpDest = HBtab->ebdHndBeg; // This callBlock will call the "finally" handler.
            endLFin               = new (this, GT_END_LFIN) GenTreeVal(GT_END_LFIN, TYP_VOID, finallyNesting);
            endLFin               = gtNewStmt(endLFin);
            endCatches            = NULL;

            encFinallies++;

            invalidatePreds = true;
        }
    }

    /* Append any remaining endCatches, if any */

    assert(!encFinallies == !endLFin);

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
                   "block BB%02u [%08p]\n",
                   block->bbNum, dspPtr(block));
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
        finalStep->setBBWeight(block->bbWeight);
        finalStep->bbFlags |= block->bbFlags & BBF_RUN_RARELY;

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - finalStep block required (encFinallies(%d) > 0), new block BB%02u [%08p]\n",
                   encFinallies, finalStep->bbNum, dspPtr(finalStep));
        }
#endif

        GenTreePtr lastStmt;

        if (endCatches)
        {
            lastStmt         = gtNewStmt(endCatches);
            endLFin->gtNext  = lastStmt;
            lastStmt->gtPrev = endLFin;
        }
        else
        {
            lastStmt = endLFin;
        }

        impEndTreeList(finalStep, endLFin, lastStmt);

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
        printf("\nBefore import CEE_LEAVE in BB%02u (targetting BB%02u):\n", block->bbNum, block->bbJumpDest->bbNum);
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
                    printf("impImportLeave - jumping out of a catch (EH#%u), convert block BB%02u to BBJ_EHCATCHRET "
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

#if defined(_TARGET_ARM_)
                if (stepType == ST_FinallyReturn)
                {
                    assert(step->bbJumpKind == BBJ_ALWAYS);
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(_TARGET_ARM_)

                /* The new block will inherit this block's weight */
                exitBlock->setBBWeight(block->bbWeight);
                exitBlock->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

                /* This exit block is the new step */
                step     = exitBlock;
                stepType = ST_Catch;

                invalidatePreds = true;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a catch (EH#%u), new BBJ_EHCATCHRET block BB%02u\n", XTnum,
                           exitBlock->bbNum);
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
                callBlock->setBBWeight(block->bbWeight);
                callBlock->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), convert block BB%02u to "
                           "BBJ_ALWAYS, add BBJ_CALLFINALLY block BB%02u\n",
                           XTnum, block->bbNum, callBlock->bbNum);
                }
#endif

#else // !FEATURE_EH_CALLFINALLY_THUNKS

                callBlock             = block;
                callBlock->bbJumpKind = BBJ_CALLFINALLY; // convert the BBJ_LEAVE to BBJ_CALLFINALLY

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), convert block BB%02u to "
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
                    step2->setBBWeight(block->bbWeight);
                    step2->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("impImportLeave - jumping out of a finally-protected try (EH#%u), step block is "
                               "BBJ_EHCATCHRET (BB%02u), new BBJ_ALWAYS step-step block BB%02u\n",
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

#if defined(_TARGET_ARM_)
                if (stepType == ST_FinallyReturn)
                {
                    assert(step->bbJumpKind == BBJ_ALWAYS);
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(_TARGET_ARM_)

                /* The new block will inherit this block's weight */
                callBlock->setBBWeight(block->bbWeight);
                callBlock->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    printf("impImportLeave - jumping out of a finally-protected try (EH#%u), new BBJ_CALLFINALLY block "
                           "BB%02u\n",
                           XTnum, callBlock->bbNum);
                }
#endif
            }

            step     = fgNewBBafter(BBJ_ALWAYS, callBlock, true);
            stepType = ST_FinallyReturn;

            /* The new block will inherit this block's weight */
            step->setBBWeight(block->bbWeight);
            step->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED | BBF_KEEP_BBJ_ALWAYS;

#ifdef DEBUG
            if (verbose)
            {
                printf("impImportLeave - jumping out of a finally-protected try (EH#%u), created step (BBJ_ALWAYS) "
                       "block BB%02u\n",
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

#if defined(_TARGET_ARM_)
                if (stepType == ST_FinallyReturn)
                {
                    // Mark the target of a finally return
                    step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
                }
#endif // defined(_TARGET_ARM_)

                /* The new block will inherit this block's weight */
                catchStep->setBBWeight(block->bbWeight);
                catchStep->bbFlags |= (block->bbFlags & BBF_RUN_RARELY) | BBF_IMPORTED;

#ifdef DEBUG
                if (verbose)
                {
                    if (stepType == ST_FinallyReturn)
                    {
                        printf("impImportLeave - return from finally jumping out of a catch-protected try (EH#%u), new "
                               "BBJ_ALWAYS block BB%02u\n",
                               XTnum, catchStep->bbNum);
                    }
                    else
                    {
                        assert(stepType == ST_Catch);
                        printf("impImportLeave - return from catch jumping out of a catch-protected try (EH#%u), new "
                               "BBJ_ALWAYS block BB%02u\n",
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
                   "block BB%02u to BBJ_ALWAYS\n",
                   block->bbNum);
        }
#endif
    }
    else
    {
        step->bbJumpDest = leaveTarget; // this is the ultimate destination of the LEAVE

#if defined(_TARGET_ARM_)
        if (stepType == ST_FinallyReturn)
        {
            assert(step->bbJumpKind == BBJ_ALWAYS);
            // Mark the target of a finally return
            step->bbJumpDest->bbFlags |= BBF_FINALLY_TARGET;
        }
#endif // defined(_TARGET_ARM_)

#ifdef DEBUG
        if (verbose)
        {
            printf("impImportLeave - final destination of step blocks set to BB%02u\n", leaveTarget->bbNum);
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
#if FEATURE_EH_FUNCLETS
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
        dupBlock->bbWeight = 0;
        dupBlock->bbFlags |= BBF_IMPORTED | BBF_INTERNAL | BBF_RUN_RARELY;

        // Insert the block right after the block which is getting reset so that BBJ_CALLFINALLY and BBJ_ALWAYS
        // will be next to each other.
        fgInsertBBafter(block, dupBlock);

#ifdef DEBUG
        if (verbose)
        {
            printf("New Basic Block BB%02u duplicate of BB%02u created.\n", dupBlock->bbNum, block->bbNum);
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

static OPCODE impGetNonPrefixOpcode(const BYTE* codeAddr, const BYTE* codeEndp)
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

static void impValidateMemoryAccessOpcode(const BYTE* codeAddr, const BYTE* codeEndp, bool volatilePrefix)
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
var_types Compiler::impGetByRefResultType(genTreeOps oper, bool fUnsigned, GenTreePtr* pOp1, GenTreePtr* pOp2)
{
    var_types  type = TYP_UNDEF;
    GenTreePtr op1 = *pOp1, op2 = *pOp2;

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
            //             / \
            //            /   \
            //           /     \
            //          /       \
            // const(h) int     addr byref
            //
            // <BUGNUM> VSW 318822 </BUGNUM>
            //
            // So here we decide to make the resulting type to be a native int.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
            if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
            {
                // insert an explicit upcast
                op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
            }
#endif // _TARGET_64BIT_

            type = TYP_I_IMPL;
        }
        else
        {
            // byref - [native] int => gives a byref
            assert(genActualType(op1->TypeGet()) == TYP_BYREF && genActualTypeIsIntOrI(op2->TypeGet()));

#ifdef _TARGET_64BIT_
            if ((genActualType(op2->TypeGet()) != TYP_I_IMPL))
            {
                // insert an explicit upcast
                op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
            }
#endif // _TARGET_64BIT_

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

#ifdef _TARGET_64BIT_
        if (genActualType(op2->TypeGet()) == TYP_BYREF)
        {
            if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
            {
                // insert an explicit upcast
                op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
            }
        }
        else if (genActualType(op2->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
        }
#endif // _TARGET_64BIT_

        type = TYP_BYREF;
    }
#ifdef _TARGET_64BIT_
    else if (genActualType(op1->TypeGet()) == TYP_I_IMPL || genActualType(op2->TypeGet()) == TYP_I_IMPL)
    {
        assert(!varTypeIsFloating(op1->gtType) && !varTypeIsFloating(op2->gtType));

        // int + long => gives long
        // long + int => gives long
        // we get this because in the IL the long isn't Int64, it's just IntPtr

        if (genActualType(op1->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op1 = *pOp1 = gtNewCastNode(TYP_I_IMPL, op1, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
        }
        else if (genActualType(op2->TypeGet()) != TYP_I_IMPL)
        {
            // insert an explicit upcast
            op2 = *pOp2 = gtNewCastNode(TYP_I_IMPL, op2, (var_types)(fUnsigned ? TYP_U_IMPL : TYP_I_IMPL));
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
#endif // _TARGET_64BIT_
    else
    {
        // int + int => gives an int
        assert(genActualType(op1->TypeGet()) != TYP_BYREF && genActualType(op2->TypeGet()) != TYP_BYREF);

        assert(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
               varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType));

        type = genActualType(op1->gtType);

#if FEATURE_X87_DOUBLES

        // For x87, since we only have 1 size of registers, prefer double
        // For everybody else, be more precise
        if (type == TYP_FLOAT)
            type = TYP_DOUBLE;

#else // !FEATURE_X87_DOUBLES

        // If both operands are TYP_FLOAT, then leave it as TYP_FLOAT.
        // Otherwise, turn floats into doubles
        if ((type == TYP_FLOAT) && (genActualType(op2->gtType) != TYP_FLOAT))
        {
            assert(genActualType(op2->gtType) == TYP_DOUBLE);
            type = TYP_DOUBLE;
        }

#endif // FEATURE_X87_DOUBLES
    }

#if FEATURE_X87_DOUBLES
    assert(type == TYP_BYREF || type == TYP_DOUBLE || type == TYP_LONG || type == TYP_INT);
#else  // FEATURE_X87_DOUBLES
    assert(type == TYP_BYREF || type == TYP_DOUBLE || type == TYP_FLOAT || type == TYP_LONG || type == TYP_INT);
#endif // FEATURE_X87_DOUBLES

    return type;
}

/*****************************************************************************
 * Casting Helper Function to service both CEE_CASTCLASS and CEE_ISINST
 *
 * typeRef contains the token, op1 to contain the value being cast,
 * and op2 to contain code that creates the type handle corresponding to typeRef
 * isCastClass = true means CEE_CASTCLASS, false means CEE_ISINST
 */
GenTreePtr Compiler::impCastClassOrIsInstToTree(GenTreePtr              op1,
                                                GenTreePtr              op2,
                                                CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                                bool                    isCastClass)
{
    bool expandInline;

    assert(op1->TypeGet() == TYP_REF);

    CorInfoHelpFunc helper = info.compCompHnd->getCastingHelper(pResolvedToken, isCastClass);

    if (isCastClass)
    {
        // We only want to expand inline the normal CHKCASTCLASS helper;
        expandInline = (helper == CORINFO_HELP_CHKCASTCLASS);
    }
    else
    {
        if (helper == CORINFO_HELP_ISINSTANCEOFCLASS)
        {
            // Get the Class Handle abd class attributes for the type we are casting to
            //
            DWORD flags = info.compCompHnd->getClassAttribs(pResolvedToken->hClass);

            //
            // If the class handle is marked as final we can also expand the IsInst check inline
            //
            expandInline = ((flags & CORINFO_FLG_FINAL) != 0);

            //
            // But don't expand inline these two cases
            //
            if (flags & CORINFO_FLG_MARSHAL_BYREF)
            {
                expandInline = false;
            }
            else if (flags & CORINFO_FLG_CONTEXTFUL)
            {
                expandInline = false;
            }
        }
        else
        {
            //
            // We can't expand inline any other helpers
            //
            expandInline = false;
        }
    }

    if (expandInline)
    {
        if (compCurBB->isRunRarely())
        {
            expandInline = false; // not worth the code expansion in a rarely run block
        }

        if ((op1->gtFlags & GTF_GLOB_EFFECT) && lvaHaveManyLocals())
        {
            expandInline = false; // not worth creating an untracked local variable
        }
    }

    if (!expandInline)
    {
        // If we CSE this class handle we prevent assertionProp from making SubType assertions
        // so instead we force the CSE logic to not consider CSE-ing this class handle.
        //
        op2->gtFlags |= GTF_DONT_CSE;

        return gtNewHelperCallNode(helper, TYP_REF, 0, gtNewArgList(op2, op1));
    }

    impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("bubbling QMark2"));

    GenTreePtr temp;
    GenTreePtr condMT;
    //
    // expand the methodtable match:
    //
    //  condMT ==>   GT_NE
    //               /    \
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

    GenTreePtr op2Var = op2;
    if (isCastClass)
    {
        op2Var                                                  = fgInsertCommaFormTemp(&op2);
        lvaTable[op2Var->AsLclVarCommon()->GetLclNum()].lvIsCSE = true;
    }
    temp = gtNewOperNode(GT_IND, TYP_I_IMPL, temp);
    temp->gtFlags |= GTF_EXCEPT;
    condMT = gtNewOperNode(GT_NE, TYP_INT, temp, op2);

    GenTreePtr condNull;
    //
    // expand the null check:
    //
    //  condNull ==>   GT_EQ
    //                 /    \
    //             op1Copy CNS_INT
    //                      null
    //
    condNull = gtNewOperNode(GT_EQ, TYP_INT, gtClone(op1), gtNewIconNode(0, TYP_REF));

    //
    // expand the true and false trees for the condMT
    //
    GenTreePtr condFalse = gtClone(op1);
    GenTreePtr condTrue;
    if (isCastClass)
    {
        //
        // use the special helper that skips the cases checked by our inlined cast
        //
        helper = CORINFO_HELP_CHKCASTCLASS_SPECIAL;

        condTrue = gtNewHelperCallNode(helper, TYP_REF, 0, gtNewArgList(op2Var, gtClone(op1)));
    }
    else
    {
        condTrue = gtNewIconNode(0, TYP_REF);
    }

#define USE_QMARK_TREES

#ifdef USE_QMARK_TREES
    GenTreePtr qmarkMT;
    //
    // Generate first QMARK - COLON tree
    //
    //  qmarkMT ==>   GT_QMARK
    //                 /     \
    //            condMT   GT_COLON
    //                      /     \
    //                condFalse  condTrue
    //
    temp    = new (this, GT_COLON) GenTreeColon(TYP_REF, condTrue, condFalse);
    qmarkMT = gtNewQmarkNode(TYP_REF, condMT, temp);
    condMT->gtFlags |= GTF_RELOP_QMARK;

    GenTreePtr qmarkNull;
    //
    // Generate second QMARK - COLON tree
    //
    //  qmarkNull ==>  GT_QMARK
    //                 /     \
    //           condNull  GT_COLON
    //                      /     \
    //                qmarkMT   op1Copy
    //
    temp      = new (this, GT_COLON) GenTreeColon(TYP_REF, gtClone(op1), qmarkMT);
    qmarkNull = gtNewQmarkNode(TYP_REF, condNull, temp);
    qmarkNull->gtFlags |= GTF_QMARK_CAST_INSTOF;
    condNull->gtFlags |= GTF_RELOP_QMARK;

    // Make QMark node a top level node by spilling it.
    unsigned tmp = lvaGrabTemp(true DEBUGARG("spilling QMark2"));
    impAssignTempGen(tmp, qmarkNull, (unsigned)CHECK_SPILL_NONE);

    // TODO: Is it possible op1 has a better type?
    lvaSetClass(tmp, pResolvedToken->hClass);
    return gtNewLclvNode(tmp, TYP_REF);
#endif
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
        printf("\nImporting BB%02u (PC=%03u) of '%s'", block->bbNum, block->bbCodeOffs, info.compFullName);
    }
#endif

    unsigned  nxtStmtIndex = impInitBlockLineInfo();
    IL_OFFSET nxtStmtOffs;

    GenTreePtr                   arrayNodeFrom, arrayNodeTo, arrayNodeToIndex;
    bool                         expandInline;
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
        bool                   usingReadyToRunHelper = false;
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
                        GenTreePtr placeHolder = new (this, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
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

        var_types       lclTyp, ovflType = TYP_UNKNOWN;
        GenTreePtr      op1           = DUMMY_INIT(NULL);
        GenTreePtr      op2           = DUMMY_INIT(NULL);
        GenTreeArgList* args          = nullptr; // What good do these "DUMMY_INIT"s do?
        GenTreePtr      newObjThisPtr = DUMMY_INIT(NULL);
        bool            uns           = DUMMY_INIT(false);
        bool            isLocal       = false;

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

            GenTreePtr op3;
            genTreeOps oper;
            unsigned   size;

            int val;

            CORINFO_SIG_INFO     sig;
            unsigned             flags;
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
                opcode = (OPCODE)(getU1LittleEndian(codeAddr) + 256);
                codeAddr += sizeof(__int8);
                opcodeOffs = (IL_OFFSET)(codeAddr - info.compCode);
                goto DECODE_OPCODE;

            SPILL_APPEND:

                // We need to call impSpillLclRefs() for a struct type lclVar.
                // This is done for non-block assignments in the handling of stloc.
                if ((op1->OperGet() == GT_ASG) && varTypeIsStruct(op1->gtOp.gtOp1) &&
                    (op1->gtOp.gtOp1->gtOper == GT_LCL_VAR))
                {
                    impSpillLclRefs(op1->gtOp.gtOp1->AsLclVarCommon()->gtLclNum);
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
                {
                    GenTreePtr cnsOp = gtNewDconNode(cval.dblVal);
#if !FEATURE_X87_DOUBLES
                    // X87 stack doesn't differentiate between float/double
                    // so R4 is treated as R8, but everybody else does
                    cnsOp->gtType = TYP_FLOAT;
#endif // FEATURE_X87_DOUBLES
                    impPushOnStack(cnsOp, typeInfo(TI_DOUBLE));
                }
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
                    lclNum = op1->AsLclVar()->gtLclNum;

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

#ifdef _TARGET_64BIT_
                // Downcast the TYP_I_IMPL into a 32-bit Int for x86 JIT compatiblity
                if (varTypeIsI(op1->TypeGet()) && (genActualType(lclTyp) == TYP_INT))
                {
                    assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                    op1 = gtNewCastNode(TYP_INT, op1, TYP_INT);
                }
#endif // _TARGET_64BIT_

                // We had better assign it a value of the correct type
                assertImp(
                    genActualType(lclTyp) == genActualType(op1->gtType) ||
                    genActualType(lclTyp) == TYP_I_IMPL && op1->IsVarAddr() ||
                    (genActualType(lclTyp) == TYP_I_IMPL && (op1->gtType == TYP_BYREF || op1->gtType == TYP_REF)) ||
                    (genActualType(op1->gtType) == TYP_I_IMPL && lclTyp == TYP_BYREF) ||
                    (varTypeIsFloating(lclTyp) && varTypeIsFloating(op1->TypeGet())) ||
                    ((genActualType(lclTyp) == TYP_BYREF) && genActualType(op1->TypeGet()) == TYP_REF));

                /* If op1 is "&var" then its type is the transient "*" and it can
                   be used either as TYP_BYREF or TYP_I_IMPL */

                if (op1->IsVarAddr())
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

                    const bool isSingleILStoreLocal =
                        !lvaTable[lclNum].lvHasMultipleILStoreOp && !lvaTable[lclNum].lvHasLdAddrOp;

                    // Conservative check that there is just one
                    // definition that reaches this store.
                    const bool hasSingleReachingDef = (block->bbStackDepthOnEntry() == 0);

                    if (isSingleILStoreLocal && hasSingleReachingDef)
                    {
                        lvaUpdateClass(lclNum, op1, clsHnd);
                    }
                }

                /* Filter out simple assignments to itself */

                if (op1->gtOper == GT_LCL_VAR && lclNum == op1->gtLclVarCommon.gtLclNum)
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

                op2 = gtNewLclvNode(lclNum, lclTyp, opcodeOffs + sz + 1);

                /* If the local is aliased, we need to spill calls and
                   indirections from the stack. */

                if ((lvaTable[lclNum].lvAddrExposed || lvaTable[lclNum].lvHasLdAddrOp) &&
                    verCurrentState.esStackDepth > 0)
                {
                    impSpillSideEffects(false, (unsigned)CHECK_SPILL_ALL DEBUGARG("Local could be aliased"));
                }

                /* Spill any refs to the local from the stack */

                impSpillLclRefs(lclNum);

#if !FEATURE_X87_DOUBLES
                // We can generate an assignment to a TYP_FLOAT from a TYP_DOUBLE
                // We insert a cast to the dest 'op2' type
                //
                if ((op1->TypeGet() != op2->TypeGet()) && varTypeIsFloating(op1->gtType) &&
                    varTypeIsFloating(op2->gtType))
                {
                    op1 = gtNewCastNode(op2->TypeGet(), op1, op2->TypeGet());
                }
#endif // !FEATURE_X87_DOUBLES

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

                op1 = gtNewLclvNode(lclNum, lvaGetActualType(lclNum), opcodeOffs + sz + 1);

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
                op1    = gtNewLclvNode(lclNum, TYP_I_IMPL, opcodeOffs + sz + 1);
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
                if (!impReturnInstruction(block, prefixFlags, opcode))
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

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARMARCH_)

                op1 = new (this, GT_JMP) GenTreeVal(GT_JMP, TYP_VOID, (size_t)resolvedToken.hMethod);

                /* Mark the basic block as being a JUMP instead of RETURN */

                block->bbFlags |= BBF_HAS_JMP;

                /* Set this flag to make sure register arguments have a location assigned
                 * even if we don't use them inside the method */

                compJmpOpUsed = true;

                fgNoStructPromotion = true;

                goto APPEND;

#else // !_TARGET_XARCH_ && !_TARGET_ARMARCH_

                // Import this just like a series of LDARGs + tail. + call + ret

                if (info.compIsVarArgs)
                {
                    // For now we don't implement true tail calls, so this breaks varargs.
                    // So warn the user instead of generating bad code.
                    // This is a semi-temporary workaround for DevDiv 173860, until we can properly
                    // implement true tail calls.
                    IMPL_LIMITATION("varags + CEE_JMP doesn't work yet");
                }

                // First load up the arguments (0 - N)
                for (unsigned argNum = 0; argNum < info.compILargsCount; argNum++)
                {
                    impLoadArg(argNum, opcodeOffs + sz + 1);
                }

                // Now generate the tail call
                noway_assert(prefixFlags == 0);
                prefixFlags = PREFIX_TAILCALL_EXPLICIT;
                opcode      = CEE_CALL;

                eeGetCallInfo(&resolvedToken, NULL,
                              combine(CORINFO_CALLINFO_ALLOWINSTPARAM, CORINFO_CALLINFO_SECURITYCHECKS), &callInfo);

                // All calls and delegates need a security callout.
                impHandleAccessAllowed(callInfo.accessAllowed, &callInfo.callsiteCalloutHelper);

                callTyp = impImportCall(CEE_CALL, &resolvedToken, NULL, NULL, PREFIX_TAILCALL_EXPLICIT, &callInfo,
                                        opcodeOffs);

                // And finish with the ret
                goto RET;

#endif // _TARGET_XARCH_ || _TARGET_ARMARCH_

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

                args = gtNewArgList(op1);                      // Type
                args = gtNewListNode(impPopStack().val, args); // index
                args = gtNewListNode(impPopStack().val, args); // array
                op1  = gtNewHelperCallNode(CORINFO_HELP_LDELEMA_REF, TYP_BYREF, GTF_EXCEPT, args);

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
                lclTyp = TYP_CHAR;
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
#ifdef _TARGET_64BIT_
                        if (opcode == CEE_LDELEM_I)
                        {
                            arrayElemTi = typeInfo::nativeInt();
                        }

                        if (lclTyp != TYP_REF && lclTyp != TYP_STRUCT)
                        {
                            Verify(typeInfo::AreEquivalent(tiRetVal, arrayElemTi), "bad array");
                        }
                        else
#endif // _TARGET_64BIT_
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
                        op1->gtIndex.gtIndElemSize = sizeof(void*);
                    }
                    else
                    {
                        // If ldElemClass is precisely a primitive type, use that, otherwise, preserve the struct type.
                        if (info.compCompHnd->getTypeForPrimitiveValueClass(ldelemClsHnd) == CORINFO_TYPE_UNDEF)
                        {
                            op1->gtIndex.gtStructElemClass = ldelemClsHnd;
                        }
                        assert(lclTyp != TYP_STRUCT || op1->gtIndex.gtStructElemClass != nullptr);
                        if (lclTyp == TYP_STRUCT)
                        {
                            size                       = info.compCompHnd->getClassSize(ldelemClsHnd);
                            op1->gtIndex.gtIndElemSize = size;
                            op1->gtType                = lclTyp;
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

                arrayNodeTo      = impStackTop(2).val;
                arrayNodeToIndex = impStackTop(1).val;
                arrayNodeFrom    = impStackTop().val;

                //
                // Note that it is not legal to optimize away CORINFO_HELP_ARRADDR_ST in a
                // lot of cases because of covariance. ie. foo[] can be cast to object[].
                //

                // Check for assignment to same array, ie. arrLcl[i] = arrLcl[j]
                // This does not need CORINFO_HELP_ARRADDR_ST

                if (arrayNodeFrom->OperGet() == GT_INDEX && arrayNodeFrom->gtOp.gtOp1->gtOper == GT_LCL_VAR &&
                    arrayNodeTo->gtOper == GT_LCL_VAR &&
                    arrayNodeTo->gtLclVarCommon.gtLclNum == arrayNodeFrom->gtOp.gtOp1->gtLclVarCommon.gtLclNum &&
                    !lvaTable[arrayNodeTo->gtLclVarCommon.gtLclNum].lvAddrExposed)
                {
                    lclTyp = TYP_REF;
                    goto ARR_ST_POST_VERIFY;
                }

                // Check for assignment of NULL. This does not need CORINFO_HELP_ARRADDR_ST

                if (arrayNodeFrom->OperGet() == GT_CNS_INT)
                {
                    assert(arrayNodeFrom->gtType == TYP_REF && arrayNodeFrom->gtIntCon.gtIconVal == 0);

                    lclTyp = TYP_REF;
                    goto ARR_ST_POST_VERIFY;
                }

            STELEM_REF_POST_VERIFY:

                /* Call a helper function to do the assignment */
                op1 = gtNewHelperCallNode(CORINFO_HELP_ARRADDR_ST, TYP_VOID, 0, impPopList(3, &flags, nullptr));

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
#ifdef _TARGET_64BIT_
                    if (opcode == CEE_STELEM_I)
                    {
                        arrayElem = typeInfo::nativeInt();
                    }
#endif // _TARGET_64BIT_
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
                if (op2->IsVarAddr())
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

                    op1->gtIndex.gtStructElemClass = stelemClsHnd;
                    op1->gtIndex.gtIndElemSize     = info.compCompHnd->getClassSize(stelemClsHnd);
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

#ifdef _TARGET_64BIT_
                    if (tiOp2.IsNativeIntType())
                    {
                        tiRetVal = tiOp2;
                    }
#endif // _TARGET_64BIT_
                }

                op2 = impPopStack().val;
                op1 = impPopStack().val;

#if !CPU_HAS_FP_SUPPORT
                if (varTypeIsFloating(op1->gtType))
                {
                    callNode = true;
                }
#endif
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

#if !FEATURE_X87_DOUBLES
                // We can generate a TYP_FLOAT operation that has a TYP_DOUBLE operand
                //
                if (varTypeIsFloating(type) && varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType))
                {
                    if (op1->TypeGet() != type)
                    {
                        // We insert a cast of op1 to 'type'
                        op1 = gtNewCastNode(type, op1, type);
                    }
                    if (op2->TypeGet() != type)
                    {
                        // We insert a cast of op2 to 'type'
                        op2 = gtNewCastNode(type, op2, type);
                    }
                }
#endif // !FEATURE_X87_DOUBLES

#if SMALL_TREE_NODES
                if (callNode)
                {
                    /* These operators can later be transformed into 'GT_CALL' */

                    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_MUL]);
#ifndef _TARGET_ARM_
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
#endif // SMALL_TREE_NODES
                {
                    op1 = gtNewOperNode(oper, type, op1, op2);
                }

                /* Special case: integer/long division may throw an exception */

                if (varTypeIsIntegral(op1->TypeGet()) && op1->OperMayThrow())
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

                // brfalse and brtrue is only allowed on I4, refs, and byrefs.
                if (!opts.MinOpts() && !opts.compDbgCode && block->bbJumpDest == block->bbNext)
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
                    /* We'll compare against an equally-sized integer 0 */
                    /* For small types, we always compare against int   */
                    op2 = gtNewZeroConNode(genActualType(op1->gtType));

                    /* Create the comparison operator and try to fold it */

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

                    BBjumpKinds foldedJumpKind = (BBjumpKinds)(op1->gtIntCon.gtIconVal ? BBJ_ALWAYS : BBJ_NONE);
                    assertImp((block->bbJumpKind == BBJ_COND)            // normal case
                              || (block->bbJumpKind == foldedJumpKind)); // this can happen if we are reimporting the
                                                                         // block for the second time

                    block->bbJumpKind = foldedJumpKind;
#ifdef DEBUG
                    if (verbose)
                    {
                        if (op1->gtIntCon.gtIconVal)
                        {
                            printf("\nThe conditional jump becomes an unconditional jump to BB%02u\n",
                                   block->bbJumpDest->bbNum);
                        }
                        else
                        {
                            printf("\nThe block falls through into the next BB%02u\n", block->bbNext->bbNum);
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

#ifdef _TARGET_64BIT_
                if (varTypeIsI(op1->TypeGet()) && (genActualType(op2->TypeGet()) == TYP_INT))
                {
                    op2 = gtNewCastNode(TYP_I_IMPL, op2, (var_types)(uns ? TYP_U_IMPL : TYP_I_IMPL));
                }
                else if (varTypeIsI(op2->TypeGet()) && (genActualType(op1->TypeGet()) == TYP_INT))
                {
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, (var_types)(uns ? TYP_U_IMPL : TYP_I_IMPL));
                }
#endif // _TARGET_64BIT_

                assertImp(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
                          varTypeIsI(op1->TypeGet()) && varTypeIsI(op2->TypeGet()) ||
                          varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType));

                /* Create the comparison node */

                op1 = gtNewOperNode(oper, TYP_INT, op1, op2);

                /* TODO: setting both flags when only one is appropriate */
                if (opcode == CEE_CGT_UN || opcode == CEE_CLT_UN)
                {
                    op1->gtFlags |= GTF_RELOP_NAN_UN | GTF_UNSIGNED;
                }

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

#ifdef _TARGET_64BIT_
                if ((op1->TypeGet() == TYP_I_IMPL) && (genActualType(op2->TypeGet()) == TYP_INT))
                {
                    op2 = gtNewCastNode(TYP_I_IMPL, op2, (var_types)(uns ? TYP_U_IMPL : TYP_I_IMPL));
                }
                else if ((op2->TypeGet() == TYP_I_IMPL) && (genActualType(op1->TypeGet()) == TYP_INT))
                {
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, (var_types)(uns ? TYP_U_IMPL : TYP_I_IMPL));
                }
#endif // _TARGET_64BIT_

                assertImp(genActualType(op1->TypeGet()) == genActualType(op2->TypeGet()) ||
                          varTypeIsI(op1->TypeGet()) && varTypeIsI(op2->TypeGet()) ||
                          varTypeIsFloating(op1->gtType) && varTypeIsFloating(op2->gtType));

                if (!opts.MinOpts() && !opts.compDbgCode && block->bbJumpDest == block->bbNext)
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
#if !FEATURE_X87_DOUBLES
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
                            op2 = gtNewCastNode(TYP_DOUBLE, op2, TYP_DOUBLE);
                        }
                        else if (op2->TypeGet() == TYP_DOUBLE)
                        {
                            // We insert a cast of op1 to TYP_DOUBLE
                            op1 = gtNewCastNode(TYP_DOUBLE, op1, TYP_DOUBLE);
                        }
                    }
                }
#endif // !FEATURE_X87_DOUBLES

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
                lclTyp = TYP_CHAR;
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
                lclTyp = TYP_CHAR;
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
                lclTyp = TYP_CHAR;
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

#ifdef _TARGET_64BIT_
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
                        default:
                            // leave 'isNative' = false;
                            break;
                    }
                    if (isNative)
                    {
                        tiRetVal = typeInfo::nativeInt();
                    }
                    else
#endif // _TARGET_64BIT_
                    {
                        tiRetVal = typeInfo(lclTyp).NormaliseForStack();
                    }
                }

                // only converts from FLOAT or DOUBLE to an integer type
                // and converts from  ULONG (or LONG on ARM) to DOUBLE are morphed to calls

                if (varTypeIsFloating(lclTyp))
                {
                    callNode = varTypeIsLong(impStackTop().val) || uns // uint->dbl gets turned into uint->long->dbl
#ifdef _TARGET_64BIT_
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

                // At this point uns, ovf, callNode all set

                op1 = impPopStack().val;
                impBashVarAddrsToI(op1);

                if (varTypeIsSmall(lclTyp) && !ovfl && op1->gtType == TYP_INT && op1->gtOper == GT_AND)
                {
                    op2 = op1->gtOp.gtOp2;

                    if (op2->gtOper == GT_CNS_INT)
                    {
                        ssize_t ival = op2->gtIntCon.gtIconVal;
                        ssize_t mask, umask;

                        switch (lclTyp)
                        {
                            case TYP_BYTE:
                            case TYP_UBYTE:
                                mask  = 0x00FF;
                                umask = 0x007F;
                                break;
                            case TYP_CHAR:
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

                            op1 = op1->gtOp.gtOp1;
                        }
                    }
                }

                /*  The 'op2' sub-operand of a cast is the 'real' type number,
                    since the result of a cast to one of the 'small' integer
                    types is an integer.
                 */

                type = genActualType(lclTyp);

#if SMALL_TREE_NODES
                if (callNode)
                {
                    op1 = gtNewCastNodeL(type, op1, lclTyp);
                }
                else
#endif // SMALL_TREE_NODES
                {
                    op1 = gtNewCastNode(type, op1, lclTyp);
                }

                if (ovfl)
                {
                    op1->gtFlags |= (GTF_OVERFLOW | GTF_EXCEPT);
                }
                if (uns)
                {
                    op1->gtFlags |= GTF_UNSIGNED;
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
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                        // Non-calls, such as obj or ret_expr, have to go through this.
                        // Calls with large struct return value have to go through this.
                        // Helper calls with small struct return value also have to go
                        // through this since they do not follow Unix calling convention.
                        if (op1->gtOper != GT_CALL || !IsMultiRegReturnedType(clsHnd) ||
                            op1->AsCall()->gtCallType == CT_HELPER)
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
                        {
                            op1 = impGetStructAddr(op1, clsHnd, (unsigned)CHECK_SPILL_ALL, false);
                        }
                    }

                    // If op1 is non-overflow cast, throw it away since it is useless.
                    // Another reason for throwing away the useless cast is in the context of
                    // implicit tail calls when the operand of pop is GT_CAST(GT_CALL(..)).
                    // The cast gets added as part of importing GT_CALL, which gets in the way
                    // of fgMorphCall() on the forms of tail call nodes that we assert.
                    if ((op1->gtOper == GT_CAST) && !op1->gtOverflow())
                    {
                        op1 = op1->gtOp.gtOp1;
                    }

                    // If 'op1' is an expression, create an assignment node.
                    // Helps analyses (like CSE) to work fine.

                    if (op1->gtOper != GT_CALL)
                    {
                        op1 = gtUnusedValNode(op1);
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
                    // Dup could start the begining of delegate creation sequence, remember that
                    delegateCreateStart = codeAddr - 1;
                    impStackTop(0);
                }

                // If the expression to dup is simple, just clone it.
                // Otherwise spill it to a temp, and reload the temp
                // twice.
                StackEntry se = impPopStack();
                tiRetVal      = se.seTypeInfo;
                op1           = se.val;

                if (!opts.compDbgCode && !op1->IsIntegralConst(0) && !op1->IsFPZero() && !op1->IsLocal())
                {
                    const unsigned tmpNum = lvaGrabTemp(true DEBUGARG("dup spill"));
                    impAssignTempGen(tmpNum, op1, tiRetVal.GetClassHandle(), (unsigned)CHECK_SPILL_ALL);
                    var_types type = genActualType(lvaTable[tmpNum].TypeGet());
                    op1            = gtNewLclvNode(tmpNum, type);

                    // Propagate type info to the temp
                    if (type == TYP_REF)
                    {
                        lvaSetClass(tmpNum, op1, tiRetVal.GetClassHandle());
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
#ifdef _TARGET_64BIT_
                    if (opcode == CEE_STIND_I)
                    {
                        instrType = typeInfo::nativeInt();
                    }
#endif // _TARGET_64BIT_
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

#ifdef _TARGET_64BIT_
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
                        op2 = gtNewCastNode(TYP_INT, op2, TYP_INT);
                    }
                    // Allow an upcast of op2 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                    //
                    if (varTypeIsI(lclTyp) && (genActualType(op2->gtType) == TYP_INT))
                    {
                        assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                        op2 = gtNewCastNode(TYP_I_IMPL, op2, TYP_I_IMPL);
                    }
                }
#endif // _TARGET_64BIT_

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

                if (prefixFlags & PREFIX_UNALIGNED)
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
                lclTyp = TYP_CHAR;
                goto LDIND;
            LDIND:

                if (tiVerificationNeeded)
                {
                    typeInfo lclTiType(lclTyp);
#ifdef _TARGET_64BIT_
                    if (opcode == CEE_LDIND_I)
                    {
                        lclTiType = typeInfo::nativeInt();
                    }
#endif // _TARGET_64BIT_
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

#ifdef _TARGET_64BIT_
                // Allow an upcast of op1 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                //
                if (genActualType(op1->gtType) == TYP_INT)
                {
                    assert(!tiVerificationNeeded); // We should have thrown the VerificationException before.
                    op1 = gtNewCastNode(TYP_I_IMPL, op1, TYP_I_IMPL);
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

                if (prefixFlags & PREFIX_UNALIGNED)
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
                opcode = (OPCODE)getU1LittleEndian(codeAddr);
                codeAddr += sizeof(__int8);
                opcodeOffs = (IL_OFFSET)(codeAddr - info.compCode);
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

                eeGetCallInfo(&resolvedToken, nullptr /* constraint typeRef*/,
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
                    // LDFTN could start the begining of delegate creation sequence, remember that
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

                CORINFO_RESOLVED_TOKEN* heapToken = impAllocateToken(resolvedToken);
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

                GenTreePtr fptr = impImportLdvirtftn(op1, &resolvedToken, &callInfo);
                if (compDonotInline())
                {
                    return;
                }

                CORINFO_RESOLVED_TOKEN* heapToken = impAllocateToken(resolvedToken);
                assert(heapToken->tokenType == CORINFO_TOKENKIND_Method);
                heapToken->tokenType = CORINFO_TOKENKIND_Ldvirtftn;
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
                    if (actualOpcode != CEE_CALLVIRT)
                    {
                        BADCODE("constrained. has to be followed by callvirt");
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
                        unsigned    size   = info.compCompHnd->getClassSize(resolvedToken.hClass);

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

                        // Append a tree to zero-out the temp
                        newObjThisPtr = gtNewLclvNode(lclNum, lvaTable[lclNum].TypeGet());

                        newObjThisPtr = gtNewBlkOpNode(newObjThisPtr,    // Dest
                                                       gtNewIconNode(0), // Value
                                                       size,             // Size
                                                       false,            // isVolatile
                                                       false);           // not copyBlock
                        impAppendTree(newObjThisPtr, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

                        // Obtain the address of the temp
                        newObjThisPtr =
                            gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(lclNum, lvaTable[lclNum].TypeGet()));
                    }
                    else
                    {
#ifdef FEATURE_READYTORUN_COMPILER
                        if (opts.IsReadyToRun())
                        {
                            op1 = impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_NEW, TYP_REF);
                            usingReadyToRunHelper = (op1 != nullptr);
                        }

                        if (!usingReadyToRunHelper)
#endif
                        {
                            op1 = impParentClassTokenToHandle(&resolvedToken, nullptr, TRUE);
                            if (op1 == nullptr)
                            { // compDonotInline()
                                return;
                            }

                            // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                            // and the newfast call with a single call to a dynamic R2R cell that will:
                            //      1) Load the context
                            //      2) Perform the generic dictionary lookup and caching, and generate the appropriate
                            //      stub
                            //      3) Allocate and return the new object
                            // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                            op1 = gtNewAllocObjNode(info.compCompHnd->getNewHelper(&resolvedToken, info.compMethodHnd),
                                                    resolvedToken.hClass, TYP_REF, op1);
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

            // fall through

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

                    resolvedToken.token = getU4LittleEndian(codeAddr);
                }

            CALL: // memberRef should be set.
                // newObjThisPtr should be set for CEE_NEWOBJ

                JITDUMP(" %08X", resolvedToken.token);
                constraintCall = (prefixFlags & PREFIX_CONSTRAINED) != 0;

                bool newBBcreatedForTailcallStress;

                newBBcreatedForTailcallStress = false;

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

                        if (newBBcreatedForTailcallStress &&
                            !(prefixFlags & PREFIX_TAILCALL_EXPLICIT) && // User hasn't set "tail." prefix yet.
                            verCheckTailCallConstraint(opcode, &resolvedToken,
                                                       constraintCall ? &constrainedResolvedToken : nullptr,
                                                       true) // Is it legal to do talcall?
                            )
                        {
                            // Stress the tailcall.
                            JITDUMP(" (Tailcall stress: prefixFlags |= PREFIX_TAILCALL_EXPLICIT)");
                            prefixFlags |= PREFIX_TAILCALL_EXPLICIT;
                        }
                    }
                }

                // This is split up to avoid goto flow warnings.
                bool isRecursive;
                isRecursive = !compIsForInlining() && (callInfo.hMethod == info.compMethodHnd);

                // Note that when running under tail call stress, a call will be marked as explicit tail prefixed
                // hence will not be considered for implicit tail calling.
                if (impIsImplicitTailCallCandidate(opcode, codeAddr + sz, codeEndp, prefixFlags, isRecursive))
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
                            JITDUMP(" (Inline Implicit Tail call: prefixFlags |= PREFIX_TAILCALL_IMPLICIT)");
                            prefixFlags |= PREFIX_TAILCALL_IMPLICIT;
                        }
#endif // FEATURE_TAILCALL_OPT_SHARED_RETURN
                    }
                    else
                    {
                        JITDUMP(" (Implicit Tail call: prefixFlags |= PREFIX_TAILCALL_IMPLICIT)");
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

#if 0 // DevDiv 410397 - This breaks too many obfuscated apps to do this in an in-place release

                // DevDiv 291703 - we need to check for accessibility between the caller of InitializeArray
                // and the field it is reading, thus it is now unverifiable to not immediately precede with
                // ldtoken <filed token>, and we now check accessibility
                if ((callInfo.methodFlags & CORINFO_FLG_INTRINSIC) &&
                    (info.compCompHnd->getIntrinsicID(callInfo.hMethod) == CORINFO_INTRINSIC_InitializeArray))
                {
                    if (prevOpcode != CEE_LDTOKEN)
                    {
                        Verify(prevOpcode == CEE_LDTOKEN, "Need ldtoken for InitializeArray");
                    }
                    else
                    {
                        assert(lastLoadToken != NULL);
                        // Now that we know we have a token, verify that it is accessible for loading
                        CORINFO_RESOLVED_TOKEN resolvedLoadField;
                        impResolveToken(lastLoadToken, &resolvedLoadField, CORINFO_TOKENKIND_Field);
                        eeGetFieldInfo(&resolvedLoadField, CORINFO_ACCESS_INIT_ARRAY, &fieldInfo);
                        impHandleAccessAllowed(fieldInfo.accessAllowed, &fieldInfo.accessCalloutHelper);
                    }
                }

#endif // DevDiv 410397
                }

                if (tiVerificationNeeded)
                {
                    verVerifyCall(opcode, &resolvedToken, constraintCall ? &constrainedResolvedToken : nullptr,
                                  explicitTailCall, readonlyCall, delegateCreateStart, codeAddr - 1,
                                  &callInfo DEBUGARG(info.compFullName));
                }

                // Insert delegate callout here.
                if (opcode == CEE_NEWOBJ && (mflags & CORINFO_FLG_CONSTRUCTOR) && (clsFlags & CORINFO_FLG_DELEGATE))
                {
#ifdef DEBUG
                    // We should do this only if verification is enabled
                    // If verification is disabled, delegateCreateStart will not be initialized correctly
                    if (tiVerificationNeeded)
                    {
                        mdMemberRef delegateMethodRef = mdMemberRefNil;
                        // We should get here only for well formed delegate creation.
                        assert(verCheckDelegateCreation(delegateCreateStart, codeAddr - 1, delegateMethodRef));
                    }
#endif
                }

                callTyp = impImportCall(opcode, &resolvedToken, constraintCall ? &constrainedResolvedToken : nullptr,
                                        newObjThisPtr, prefixFlags, &callInfo, opcodeOffs);
                if (compDonotInline())
                {
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

                BOOL isLoadAddress = (opcode == CEE_LDFLDA || opcode == CEE_LDSFLDA);
                BOOL isLoadStatic  = (opcode == CEE_LDSFLD || opcode == CEE_LDSFLDA);

                /* Get the CP_Fieldref index */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Field);

                JITDUMP(" %08X", resolvedToken.token);

                int aflags = isLoadAddress ? CORINFO_ACCESS_ADDRESS : CORINFO_ACCESS_GET;

                GenTreePtr           obj     = nullptr;
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

                        // An optimization for Contextful classes:
                        // we unwrap the proxy when we have a 'this reference'

                        if (info.compUnwrapContextful)
                        {
                            aflags |= CORINFO_ACCESS_UNWRAP;
                        }
                    }
                }

                eeGetFieldInfo(&resolvedToken, (CORINFO_ACCESS_FLAGS)aflags, &fieldInfo);

                // Figure out the type of the member.  We always call canAccessField, so you always need this
                // handle
                CorInfoType ciType = fieldInfo.fieldType;
                clsHnd             = fieldInfo.structType;

                lclTyp = JITtype2varType(ciType);

#ifdef _TARGET_AMD64
                noway_assert(varTypeIsIntegralOrI(lclTyp) || varTypeIsFloating(lclTyp) || lclTyp == TYP_STRUCT);
#endif // _TARGET_AMD64

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
                    BOOL bAllowPlainValueTypeAsThis = FALSE;
                    if (opcode == CEE_LDFLD && impIsValueType(tiObj))
                    {
                        bAllowPlainValueTypeAsThis = TRUE;
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
                if (lclTyp > TYP_INT)
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
                        bool nullcheckNeeded = false;

                        obj = impCheckForNullPointer(obj);

                        if (isLoadAddress && (obj->gtType == TYP_BYREF) && fgAddrCouldBeNull(obj))
                        {
                            nullcheckNeeded = true;
                        }

                        // If the object is a struct, what we really want is
                        // for the field to operate on the address of the struct.
                        if (!varTypeGCtype(obj->TypeGet()) && impIsValueType(tiObj))
                        {
                            assert(opcode == CEE_LDFLD && objType != nullptr);

                            obj = impGetStructAddr(obj, objType, (unsigned)CHECK_SPILL_ALL, true);
                        }

                        /* Create the data member node */
                        op1 = gtNewFieldRef(lclTyp, resolvedToken.hField, obj, fieldInfo.offset, nullcheckNeeded);

#ifdef FEATURE_READYTORUN_COMPILER
                        if (fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE_WITH_BASE)
                        {
                            op1->gtField.gtFieldLookup = fieldInfo.fieldLookup;
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
                            op1->gtField.gtFldMayOverlap = true;
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
                                impInlineIsGuaranteedThisDerefBeforeAnySideEffects(nullptr, obj,
                                                                                   impInlineInfo->inlArgInfo))
                            {
                                impInlineInfo->thisDereferencedFirst = true;
                            }
                        }
                    }
                    break;

                    case CORINFO_FIELD_STATIC_TLS:
#ifdef _TARGET_X86_
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

                        __fallthrough;
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
                                assert(pFldAddr == nullptr);

                                op1 = impImportStaticReadOnlyField(fldAddr, lclTyp);
                                goto FIELD_DONE;
                            }
                        }

                        __fallthrough;

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
                        op1 = gtNewIconNode(0, lclTyp);
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

                    if (prefixFlags & PREFIX_UNALIGNED)
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
                    GenTreePtr helperNode = impInitClass(&resolvedToken);
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

                BOOL isStoreStatic = (opcode == CEE_STSFLD);

                CORINFO_CLASS_HANDLE fieldClsHnd; // class of the field (if it's a ref type)

                /* Get the CP_Fieldref index */

                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Field);

                JITDUMP(" %08X", resolvedToken.token);

                int        aflags = CORINFO_ACCESS_SET;
                GenTreePtr obj    = nullptr;
                typeInfo*  tiObj  = nullptr;
                typeInfo   tiVal;

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

                        // An optimization for Contextful classes:
                        // we unwrap the proxy when we have a 'this reference'

                        if (info.compUnwrapContextful)
                        {
                            aflags |= CORINFO_ACCESS_UNWRAP;
                        }
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
                    verVerifyField(&resolvedToken, fieldInfo, tiObj, TRUE);
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
                if (lclTyp > TYP_INT)
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
                            op1->gtField.gtFldMayOverlap = true;
                        }

#ifdef FEATURE_READYTORUN_COMPILER
                        if (fieldInfo.fieldAccessor == CORINFO_FIELD_INSTANCE_WITH_BASE)
                        {
                            op1->gtField.gtFieldLookup = fieldInfo.fieldLookup;
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
                            impInlineIsGuaranteedThisDerefBeforeAnySideEffects(op2, obj, impInlineInfo->inlArgInfo))
                        {
                            impInlineInfo->thisDereferencedFirst = true;
                        }
                    }
                    break;

                    case CORINFO_FIELD_STATIC_TLS:
#ifdef _TARGET_X86_
                        // Legacy TLS access is implemented as intrinsic on x86 only

                        /* Create the data member node */
                        op1 = gtNewFieldRef(lclTyp, resolvedToken.hField, NULL, fieldInfo.offset);
                        op1->gtFlags |= GTF_IND_TLS_REF; // fgMorphField will handle the transformation

                        break;
#else
                        fieldInfo.fieldAccessor = CORINFO_FIELD_STATIC_ADDR_HELPER;

                        __fallthrough;
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

                // Create the member assignment, unless we have a struct.
                // TODO-1stClassStructs: This could be limited to TYP_STRUCT, to avoid extra copies.
                bool deferStructAssign = varTypeIsStruct(lclTyp);

                if (!deferStructAssign)
                {
                    if (prefixFlags & PREFIX_VOLATILE)
                    {
                        assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND));
                        op1->gtFlags |= GTF_DONT_CSE;      // Can't CSE a volatile
                        op1->gtFlags |= GTF_ORDER_SIDEEFF; // Prevent this from being reordered
                        op1->gtFlags |= GTF_IND_VOLATILE;
                    }
                    if (prefixFlags & PREFIX_UNALIGNED)
                    {
                        assert((op1->OperGet() == GT_FIELD) || (op1->OperGet() == GT_IND));
                        op1->gtFlags |= GTF_IND_UNALIGNED;
                    }

                    /* V4.0 allows assignment of i4 constant values to i8 type vars when IL verifier is bypassed (full
                       trust
                       apps).  The reason this works is that JIT stores an i4 constant in Gentree union during
                       importation
                       and reads from the union as if it were a long during code generation. Though this can potentially
                       read garbage, one can get lucky to have this working correctly.

                       This code pattern is generated by Dev10 MC++ compiler while storing to fields when compiled with
                       /O2
                       switch (default when compiling retail configs in Dev10) and a customer app has taken a dependency
                       on
                       it.  To be backward compatible, we will explicitly add an upward cast here so that it works
                       correctly
                       always.

                       Note that this is limited to x86 alone as thereis no back compat to be addressed for Arm JIT for
                       V4.0.
                    */
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
                    if (op1->TypeGet() != op2->TypeGet() && op2->OperIsConst() && varTypeIsIntOrI(op2->TypeGet()) &&
                        varTypeIsLong(op1->TypeGet()))
                    {
                        op2 = gtNewCastNode(op1->TypeGet(), op2, op1->TypeGet());
                    }
#endif

#ifdef _TARGET_64BIT_
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
                            op2 = gtNewCastNode(TYP_INT, op2, TYP_INT);
                        }
                        // Allow an upcast of op2 from a 32-bit Int into TYP_I_IMPL for x86 JIT compatiblity
                        //
                        if (varTypeIsI(lclTyp) && (genActualType(op2->gtType) == TYP_INT))
                        {
                            op2 = gtNewCastNode(TYP_I_IMPL, op2, TYP_I_IMPL);
                        }
                    }
#endif

#if !FEATURE_X87_DOUBLES
                    // We can generate an assignment to a TYP_FLOAT from a TYP_DOUBLE
                    // We insert a cast to the dest 'op1' type
                    //
                    if ((op1->TypeGet() != op2->TypeGet()) && varTypeIsFloating(op1->gtType) &&
                        varTypeIsFloating(op2->gtType))
                    {
                        op2 = gtNewCastNode(op1->TypeGet(), op2, op1->TypeGet());
                    }
#endif // !FEATURE_X87_DOUBLES

                    op1 = gtNewAssignNode(op1, op2);

                    /* Mark the expression as containing an assignment */

                    op1->gtFlags |= GTF_ASG;
                }

                /* Check if the class needs explicit initialization */

                if (fieldInfo.fieldFlags & CORINFO_FLG_FIELD_INITCLASS)
                {
                    GenTreePtr helperNode = impInitClass(&resolvedToken);
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

                    if (impIsValueType(tiObj))
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
                    op1 = impTokenToHandle(&resolvedToken, nullptr, TRUE /*mustRestoreHandle*/);
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

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    op1 = impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_NEWARR_1, TYP_REF,
                                                    gtNewArgList(op2));
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
                        op1 = impTokenToHandle(&resolvedToken, nullptr, TRUE /*mustRestoreHandle*/);
                        if (op1 == nullptr)
                        { // compDonotInline()
                            return;
                        }
                    }
                }

                if (!usingReadyToRunHelper)
#endif
                {
                    args = gtNewArgList(op1, op2);

                    /* Create a call to 'new' */

                    // Note that this only works for shared generic code because the same helper is used for all
                    // reference array types
                    op1 =
                        gtNewHelperCallNode(info.compCompHnd->getNewArrHelper(resolvedToken.hClass), TYP_REF, 0, args);
                }

                op1->gtCall.compileTimeHelperArgumentHandle = (CORINFO_GENERIC_HANDLE)resolvedToken.hClass;

                /* Remember that this basic block contains 'new' of an sd array */

                block->bbFlags |= BBF_HAS_NEWARRAY;
                optMethodFlags |= OMF_HAS_NEWARRAY;

                /* Push the result of the call on the stack */

                impPushOnStack(op1, tiRetVal);

                callTyp = TYP_REF;
            }
            break;

            case CEE_LOCALLOC:
                assert(!compIsForInlining());

                if (tiVerificationNeeded)
                {
                    Verify(false, "bad opcode");
                }

                // We don't allow locallocs inside handlers
                if (block->hasHndIndex())
                {
                    BADCODE("Localloc can't be inside handler");
                }

                /* The FP register may not be back to the original value at the end
                   of the method, even if the frame size is 0, as localloc may
                   have modified it. So we will HAVE to reset it */

                compLocallocUsed = true;
                setNeedsGSSecurityCookie();

                // Get the size to allocate

                op2 = impPopStack().val;
                assertImp(genActualTypeIsIntOrI(op2->gtType));

                if (verCurrentState.esStackDepth != 0)
                {
                    BADCODE("Localloc can only be used when the stack is empty");
                }

                op1 = gtNewOperNode(GT_LCLHEAP, TYP_I_IMPL, op2);

                // May throw a stack overflow exception. Obviously, we don't want locallocs to be CSE'd.

                op1->gtFlags |= (GTF_EXCEPT | GTF_DONT_CSE);

                impPushOnStack(op1, tiRetVal);
                break;

            case CEE_ISINST:

                /* Get the type token */
                assertImp(sz == sizeof(unsigned));

                _impResolveToken(CORINFO_TOKENKIND_Casting);

                JITDUMP(" %08X", resolvedToken.token);

                if (!opts.IsReadyToRun())
                {
                    op2 = impTokenToHandle(&resolvedToken, nullptr, FALSE);
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

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    GenTreeCall* opLookup =
                        impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_ISINSTANCEOF, TYP_REF,
                                                  gtNewArgList(op1));
                    usingReadyToRunHelper = (opLookup != nullptr);
                    op1                   = (usingReadyToRunHelper ? opLookup : op1);

                    if (!usingReadyToRunHelper)
                    {
                        // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                        // and the isinstanceof_any call with a single call to a dynamic R2R cell that will:
                        //      1) Load the context
                        //      2) Perform the generic dictionary lookup and caching, and generate the appropriate stub
                        //      3) Perform the 'is instance' check on the input object
                        // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                        op2 = impTokenToHandle(&resolvedToken, nullptr, FALSE);
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

                break;

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
                args = gtNewArgList(op2, op1);
                op1  = gtNewHelperCallNode(CORINFO_HELP_GETREFANY, TYP_BYREF, 0, args);

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
                    op1 = op1->gtOp.gtOp1;

                    // Fetch the type from the correct slot
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        gtNewIconNode(offsetof(CORINFO_RefAny, type), TYP_I_IMPL));
                    op1 = gtNewOperNode(GT_IND, TYP_BYREF, op1);
                }
                else
                {
                    assertImp(op1->gtOper == GT_MKREFANY);

                    // The pointer may have side-effects
                    if (op1->gtOp.gtOp1->gtFlags & GTF_SIDE_EFFECT)
                    {
                        impAppendTree(op1->gtOp.gtOp1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);
#ifdef DEBUG
                        impNoteLastILoffs();
#endif
                    }

                    // We already have the class handle
                    op1 = op1->gtOp.gtOp2;
                }

                // convert native TypeHandle to RuntimeTypeHandle
                {
                    GenTreeArgList* helperArgs = gtNewArgList(op1);

                    op1 = gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL, TYP_STRUCT, GTF_EXCEPT,
                                              helperArgs);

                    // The handle struct is returned in register
                    op1->gtCall.gtReturnType = TYP_REF;

                    tiRetVal = typeInfo(TI_STRUCT, impGetTypeHandleClass());
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

                op1 = impTokenToHandle(&resolvedToken, nullptr, TRUE);
                if (op1 == nullptr)
                { // compDonotInline()
                    return;
                }

                helper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE;
                assert(resolvedToken.hClass != nullptr);

                if (resolvedToken.hMethod != nullptr)
                {
                    helper = CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD;
                }
                else if (resolvedToken.hField != nullptr)
                {
                    helper = CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD;
                }

                GenTreeArgList* helperArgs = gtNewArgList(op1);

                op1 = gtNewHelperCallNode(helper, TYP_STRUCT, GTF_EXCEPT, helperArgs);

                // The handle struct is returned in register
                op1->gtCall.gtReturnType = TYP_REF;

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

                BOOL runtimeLookup;
                op2 = impTokenToHandle(&resolvedToken, &runtimeLookup);
                if (op2 == nullptr)
                { // compDonotInline()
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

                // We only want to expand inline the normal UNBOX helper;
                expandInline = (helper == CORINFO_HELP_UNBOX);

                if (expandInline)
                {
                    if (compCurBB->isRunRarely())
                    {
                        expandInline = false; // not worth the code expansion
                    }
                }

                if (expandInline)
                {
                    // we are doing normal unboxing
                    // inline the common case of the unbox helper
                    // UNBOX(exp) morphs into
                    // clone = pop(exp);
                    // ((*clone == typeToken) ? nop : helper(clone, typeToken));
                    // push(clone + sizeof(void*))
                    //
                    GenTreePtr cloneOperand;
                    op1 = impCloneExpr(op1, &cloneOperand, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("inline UNBOX clone1"));
                    op1 = gtNewOperNode(GT_IND, TYP_I_IMPL, op1);

                    GenTreePtr condBox = gtNewOperNode(GT_EQ, TYP_INT, op1, op2);

                    op1 = impCloneExpr(cloneOperand, &cloneOperand, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("inline UNBOX clone2"));
                    op2 = impTokenToHandle(&resolvedToken);
                    if (op2 == nullptr)
                    { // compDonotInline()
                        return;
                    }
                    args = gtNewArgList(op2, op1);
                    op1  = gtNewHelperCallNode(helper, TYP_VOID, 0, args);

                    op1 = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), op1);
                    op1 = gtNewQmarkNode(TYP_VOID, condBox, op1);
                    condBox->gtFlags |= GTF_RELOP_QMARK;

                    // QMARK nodes cannot reside on the evaluation stack. Because there
                    // may be other trees on the evaluation stack that side-effect the
                    // sources of the UNBOX operation we must spill the stack.

                    impAppendTree(op1, (unsigned)CHECK_SPILL_ALL, impCurStmtOffs);

                    // Create the address-expression to reference past the object header
                    // to the beginning of the value-type. Today this means adjusting
                    // past the base of the objects vtable field which is pointer sized.

                    op2 = gtNewIconNode(sizeof(void*), TYP_I_IMPL);
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, cloneOperand, op2);
                }
                else
                {
                    unsigned callFlags = (helper == CORINFO_HELP_UNBOX) ? 0 : GTF_EXCEPT;

                    // Don't optimize, just call the helper and be done with it
                    args = gtNewArgList(op2, op1);
                    op1  = gtNewHelperCallNode(helper,
                                              (var_types)((helper == CORINFO_HELP_UNBOX) ? TYP_BYREF : TYP_STRUCT),
                                              callFlags, args);
                }

                assert(helper == CORINFO_HELP_UNBOX && op1->gtType == TYP_BYREF || // Unbox helper returns a byref.
                       helper == CORINFO_HELP_UNBOX_NULLABLE &&
                           varTypeIsStruct(op1) // UnboxNullable helper returns a struct.
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

                    if (varTypeIsStruct(op1) && IsMultiRegReturnedType(resolvedToken.hClass))
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
                    verCurrentState.esStack[verCurrentState.esStackDepth - 1].seTypeInfo = tiRetVal;
                    break;
                }

                // Look ahead for unbox.any
                if (codeAddr + (sz + 1 + sizeof(mdToken)) <= codeEndp && codeAddr[sz] == CEE_UNBOX_ANY)
                {
                    DWORD classAttribs = info.compCompHnd->getClassAttribs(resolvedToken.hClass);
                    if (!(classAttribs & CORINFO_FLG_SHAREDINST))
                    {
                        CORINFO_RESOLVED_TOKEN unboxResolvedToken;

                        impResolveToken(codeAddr + (sz + 1), &unboxResolvedToken, CORINFO_TOKENKIND_Class);

                        if (unboxResolvedToken.hClass == resolvedToken.hClass)
                        {
                            // Skip the next unbox.any instruction
                            sz += sizeof(mdToken) + 1;
                            break;
                        }
                    }
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
                    op2 = impTokenToHandle(&resolvedToken, nullptr, FALSE);
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

#ifdef FEATURE_READYTORUN_COMPILER
                if (opts.IsReadyToRun())
                {
                    GenTreeCall* opLookup = impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_CHKCAST,
                                                                      TYP_REF, gtNewArgList(op1));
                    usingReadyToRunHelper = (opLookup != nullptr);
                    op1                   = (usingReadyToRunHelper ? opLookup : op1);

                    if (!usingReadyToRunHelper)
                    {
                        // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
                        // and the chkcastany call with a single call to a dynamic R2R cell that will:
                        //      1) Load the context
                        //      2) Perform the generic dictionary lookup and caching, and generate the appropriate stub
                        //      3) Check the object on the stack for the type-cast
                        // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

                        op2 = impTokenToHandle(&resolvedToken, nullptr, FALSE);
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
                break;

            case CEE_THROW:

                if (compIsForInlining())
                {
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // TODO: Will this be too strict, given that we will inline many basic blocks?
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    /* Do we have just the exception on the stack ?*/

                    if (verCurrentState.esStackDepth != 1)
                    {
                        /* if not, just don't inline the method */

                        compInlineResult->NoteFatal(InlineObservation::CALLEE_THROW_WITH_INVALID_STACK);
                        return;
                    }
                }

                if (tiVerificationNeeded)
                {
                    tiRetVal = impStackTop().seTypeInfo;
                    Verify(tiRetVal.IsObjRef(), "object ref expected");
                    if (verTrackObjCtorInitState && (verCurrentState.thisInitialized != TIS_Init))
                    {
                        Verify(!tiRetVal.IsThisPtr(), "throw uninitialized this");
                    }
                }

                block->bbSetRunRarely(); // any block with a throw is rare
                /* Pop the exception object and create the 'throw' helper call */

                op1 = gtNewHelperCallNode(CORINFO_HELP_THROW, TYP_VOID, GTF_EXCEPT, gtNewArgList(impPopStack().val));

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

                op1 = gtNewHelperCallNode(CORINFO_HELP_RETHROW, TYP_VOID, GTF_EXCEPT);

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
                op1  = gtNewBlkOpNode(op1, op2, size, (prefixFlags & PREFIX_VOLATILE) != 0, false);
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
                    op1  = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, op1, size);
                }
                else
                {
                    op1  = new (this, GT_DYN_BLK) GenTreeDynBlk(op1, op3);
                    size = 0;
                }
                op1 = gtNewBlkOpNode(op1, op2, size, (prefixFlags & PREFIX_VOLATILE) != 0, false);

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
                    op1  = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, op1, size);
                }
                else
                {
                    op1  = new (this, GT_DYN_BLK) GenTreeDynBlk(op1, op3);
                    size = 0;
                }
                if (op2->OperGet() == GT_ADDR)
                {
                    op2 = op2->gtOp.gtOp1;
                }
                else
                {
                    op2 = gtNewOperNode(GT_IND, TYP_STRUCT, op2);
                }

                op1 = gtNewBlkOpNode(op1, op2, size, (prefixFlags & PREFIX_VOLATILE) != 0, true);
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

                op2 = impTokenToHandle(&resolvedToken, nullptr, TRUE);
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
                if (!opts.MinOpts() && !opts.compDbgCode)
                {
                    /* Use GT_ARR_LENGTH operator so rng check opts see this */
                    GenTreeArrLen* arrLen =
                        new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, op1, offsetof(CORINFO_Array, length));

                    /* Mark the block as containing a length expression */

                    if (op1->gtOper == GT_LCL_VAR)
                    {
                        block->bbFlags |= BBF_HAS_IDX_LEN;
                    }

                    op1 = arrLen;
                }
                else
                {
                    /* Create the expression "*(array_addr + ArrLenOffs)" */
                    op1 = gtNewOperNode(GT_ADD, TYP_BYREF, op1,
                                        gtNewIconNode(offsetof(CORINFO_Array, length), TYP_I_IMPL));
                    op1 = gtNewOperNode(GT_IND, TYP_INT, op1);
                    op1->gtFlags |= GTF_IND_ARR_LEN;
                }

                /* An indirection will cause a GPF if the address is null */
                op1->gtFlags |= GTF_EXCEPT;

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

            case CEE_ILLEGAL:
            case CEE_MACRO_END:

            default:
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
void Compiler::impLoadVar(unsigned lclNum, IL_OFFSET offset, typeInfo tiRetVal)
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

    impPushVar(gtNewLclvNode(lclNum, lclTyp, offset), tiRetVal);
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

#ifdef _TARGET_ARM_
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
void Compiler::impMarkLclDstNotPromotable(unsigned tmpNum, GenTreePtr src, CORINFO_CLASS_HANDLE hClass)
{
    if (src->gtOper == GT_CALL && src->gtCall.IsVarargs() && IsHfa(hClass))
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
#endif // _TARGET_ARM_

#if FEATURE_MULTIREG_RET
GenTreePtr Compiler::impAssignMultiRegTypeToVar(GenTreePtr op, CORINFO_CLASS_HANDLE hClass)
{
    unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Return value temp for multireg return."));
    impAssignTempGen(tmpNum, op, hClass, (unsigned)CHECK_SPILL_ALL);
    GenTreePtr ret = gtNewLclvNode(tmpNum, op->gtType);

    // TODO-1stClassStructs: Handle constant propagation and CSE-ing of multireg returns.
    ret->gtFlags |= GTF_DONT_CSE;

    assert(IsMultiRegReturnedType(hClass));

    // Mark the var so that fields are not promoted and stay together.
    lvaTable[tmpNum].lvIsMultiRegRet = true;

    return ret;
}
#endif // FEATURE_MULTIREG_RET

// do import for a return
// returns false if inlining was aborted
// opcode can be ret or call in the case of a tail.call
bool Compiler::impReturnInstruction(BasicBlock* block, int prefixFlags, OPCODE& opcode)
{
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
            assertImp((genActualType(op2->TypeGet()) == genActualType(info.compRetType)) ||
                      ((op2->TypeGet() == TYP_I_IMPL) && (info.compRetType == TYP_BYREF)) ||
                      ((op2->TypeGet() == TYP_BYREF) && (info.compRetType == TYP_I_IMPL)) ||
                      (varTypeIsFloating(op2->gtType) && varTypeIsFloating(info.compRetType)) ||
                      (varTypeIsStruct(op2) && varTypeIsStruct(info.compRetType)));

#ifdef DEBUG
            if (opts.compGcChecks && info.compRetType == TYP_REF)
            {
                // DDB 3483  : JIT Stress: early termination of GC ref's life time in exception code path
                // VSW 440513: Incorrect gcinfo on the return value under COMPlus_JitGCChecks=1 for methods with
                // one-return BB.

                assert(op2->gtType == TYP_REF);

                // confirm that the argument is a GC pointer (for debugging (GC stress))
                GenTreeArgList* args = gtNewArgList(op2);
                op2                  = gtNewHelperCallNode(CORINFO_HELP_CHECK_OBJ, TYP_REF, 0, args);

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
                compInlineResult->NoteFatal(InlineObservation::CALLSITE_RETURN_TYPE_MISMATCH);
                return false;
            }

            // Below, we are going to set impInlineInfo->retExpr to the tree with the return
            // expression. At this point, retExpr could already be set if there are multiple
            // return blocks (meaning lvaInlineeReturnSpillTemp != BAD_VAR_NUM) and one of
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
                    op2 = impFixupStructReturnType(op2, retClsHnd);
                }
                else
                {
                    // Do we have to normalize?
                    var_types fncRealRetType = JITtype2varType(info.compMethodInfo->args.retType);
                    if ((varTypeIsSmall(op2->TypeGet()) || varTypeIsSmall(fncRealRetType)) &&
                        fgCastNeeded(op2, fncRealRetType))
                    {
                        // Small-typed return values are normalized by the callee
                        op2 = gtNewCastNode(TYP_INT, op2, fncRealRetType);
                    }
                }

                if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
                {
                    assert(info.compRetNativeType != TYP_VOID &&
                           (fgMoreThanOneReturnBlock() || impInlineInfo->HasGcRefLocals()));

                    // This is a bit of a workaround...
                    // If we are inlining a call that returns a struct, where the actual "native" return type is
                    // not a struct (for example, the struct is composed of exactly one int, and the native
                    // return type is thus an int), and the inlinee has multiple return blocks (thus,
                    // lvaInlineeReturnSpillTemp is != BAD_VAR_NUM, and is the index of a local var that is set
                    // to the *native* return type), and at least one of the return blocks is the result of
                    // a call, then we have a problem. The situation is like this (from a failed test case):
                    //
                    // inliner:
                    //      // Note: valuetype plinq_devtests.LazyTests/LIX is a struct with only a single int
                    //      call !!0 [mscorlib]System.Threading.LazyInitializer::EnsureInitialized<valuetype
                    //      plinq_devtests.LazyTests/LIX>(!!0&, bool&, object&, class [mscorlib]System.Func`1<!!0>)
                    //
                    // inlinee:
                    //      ...
                    //      ldobj      !!T                 // this gets bashed to a GT_LCL_FLD, type TYP_INT
                    //      ret
                    //      ...
                    //      call       !!0 System.Threading.LazyInitializer::EnsureInitializedCore<!!0>(!!0&, bool&,
                    //      object&, class System.Func`1<!!0>)
                    //      ret
                    //
                    // In the code above, when we call impFixupStructReturnType(), we will change the op2 return type
                    // of the inlinee return node, but we don't do that for GT_CALL nodes, which we delay until
                    // morphing when we call fgFixupStructReturn(). We do this, apparently, to handle nested
                    // inlining properly by leaving the correct type on the GT_CALL node through importing.
                    //
                    // To fix this, for this case, we temporarily change the GT_CALL node type to the
                    // native return type, which is what it will be set to eventually. We generate the
                    // assignment to the return temp, using the correct type, and then restore the GT_CALL
                    // node type. During morphing, the GT_CALL will get the correct, final, native return type.

                    bool restoreType = false;
                    if ((op2->OperGet() == GT_CALL) && (info.compRetType == TYP_STRUCT))
                    {
                        noway_assert(op2->TypeGet() == TYP_STRUCT);
                        op2->gtType = info.compRetNativeType;
                        restoreType = true;
                    }

                    impAssignTempGen(lvaInlineeReturnSpillTemp, op2, se.seTypeInfo.GetClassHandle(),
                                     (unsigned)CHECK_SPILL_ALL);

                    GenTreePtr tmpOp2 = gtNewLclvNode(lvaInlineeReturnSpillTemp, op2->TypeGet());

                    if (restoreType)
                    {
                        op2->gtType = TYP_STRUCT; // restore it to what it was
                    }

                    op2 = tmpOp2;

#ifdef DEBUG
                    if (impInlineInfo->retExpr)
                    {
                        // Some other block(s) have seen the CEE_RET first.
                        // Better they spilled to the same temp.
                        assert(impInlineInfo->retExpr->gtOper == GT_LCL_VAR);
                        assert(impInlineInfo->retExpr->gtLclVarCommon.gtLclNum == op2->gtLclVarCommon.gtLclNum);
                    }
#endif
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

#if defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#if defined(_TARGET_ARM_)
                // TODO-ARM64-NYI: HFA
                // TODO-AMD64-Unix and TODO-ARM once the ARM64 functionality is implemented the
                // next ifdefs could be refactored in a single method with the ifdef inside.
                if (IsHfa(retClsHnd))
                {
// Same as !IsHfa but just don't bother with impAssignStructPtr.
#else  // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                ReturnTypeDesc retTypeDesc;
                retTypeDesc.InitializeStructReturnType(this, retClsHnd);
                unsigned retRegCount = retTypeDesc.GetReturnRegCount();

                if (retRegCount != 0)
                {
                    // If single eightbyte, the return type would have been normalized and there won't be a temp var.
                    // This code will be called only if the struct return has not been normalized (i.e. 2 eightbytes -
                    // max allowed.)
                    assert(retRegCount == MAX_RET_REG_COUNT);
                    // Same as !structDesc.passedInRegisters but just don't bother with impAssignStructPtr.
                    CLANG_FORMAT_COMMENT_ANCHOR;
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

                    if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
                    {
                        if (!impInlineInfo->retExpr)
                        {
#if defined(_TARGET_ARM_)
                            impInlineInfo->retExpr = gtNewLclvNode(lvaInlineeReturnSpillTemp, info.compRetType);
#else  // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                            // The inlinee compiler has figured out the type of the temp already. Use it here.
                            impInlineInfo->retExpr =
                                gtNewLclvNode(lvaInlineeReturnSpillTemp, lvaTable[lvaInlineeReturnSpillTemp].lvType);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                        }
                    }
                    else
                    {
                        impInlineInfo->retExpr = op2;
                    }
                }
                else
#elif defined(_TARGET_ARM64_)
                ReturnTypeDesc retTypeDesc;
                retTypeDesc.InitializeStructReturnType(this, retClsHnd);
                unsigned retRegCount = retTypeDesc.GetReturnRegCount();

                if (retRegCount != 0)
                {
                    assert(!iciCall->HasRetBufArg());
                    assert(retRegCount >= 2);
                    if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
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
#endif // defined(_TARGET_ARM64_)
                {
                    assert(iciCall->HasRetBufArg());
                    GenTreePtr dest = gtCloneExpr(iciCall->gtCallArgs->gtOp.gtOp1);
                    // spill temp only exists if there are multiple return points
                    if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
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
        GenTreePtr retBuffAddr = gtNewLclvNode(info.compRetBuffArg, TYP_BYREF, impCurStmtOffs);

        op2 = impAssignStructPtr(retBuffAddr, op2, retClsHnd, (unsigned)CHECK_SPILL_ALL);
        impAppendTree(op2, (unsigned)CHECK_SPILL_NONE, impCurStmtOffs);

        // There are cases where the address of the implicit RetBuf should be returned explicitly (in RAX).
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_AMD64_)

        // x64 (System V and Win64) calling convention requires to
        // return the implicit return buffer explicitly (in RAX).
        // Change the return type to be BYREF.
        op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
#else  // !defined(_TARGET_AMD64_)
        // In case of non-AMD64 targets the profiler hook requires to return the implicit RetBuf explicitly (in RAX).
        // In such case the return value of the function is changed to BYREF.
        // If profiler hook is not needed the return type of the function is TYP_VOID.
        if (compIsProfilerHookNeeded())
        {
            op1 = gtNewOperNode(GT_RETURN, TYP_BYREF, gtNewLclvNode(info.compRetBuffArg, TYP_BYREF));
        }
        else
        {
            // return void
            op1 = new (this, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID);
        }
#endif // !defined(_TARGET_AMD64_)
    }
    else if (varTypeIsStruct(info.compRetType))
    {
#if !FEATURE_MULTIREG_RET
        // For both ARM architectures the HFA native types are maintained as structs.
        // Also on System V AMD64 the multireg structs returns are also left as structs.
        noway_assert(info.compRetNativeType != TYP_STRUCT);
#endif
        op2 = impFixupStructReturnType(op2, retClsHnd);
        // return op2
        op1 = gtNewOperNode(GT_RETURN, genActualType(info.compRetNativeType), op2);
    }
    else
    {
        // return op2
        op1 = gtNewOperNode(GT_RETURN, genActualType(info.compRetType), op2);
    }

    // We must have imported a tailcall and jumped to RET
    if (prefixFlags & PREFIX_TAILCALL)
    {
#if defined(FEATURE_CORECLR) || !defined(_TARGET_AMD64_)
        // Jit64 compat:
        // This cannot be asserted on Amd64 since we permit the following IL pattern:
        //      tail.call
        //      pop
        //      ret
        assert(verCurrentState.esStackDepth == 0 && impOpcodeIsCallOpcode(opcode));
#endif // FEATURE_CORECLR || !_TARGET_AMD64_

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
        printf("\nBB%02u will be reimported\n", block->bbNum);
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

            /* Recursively process the handler block */
            BasicBlock* hndBegBB = HBtab->ebdHndBeg;

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

            if (HBtab->HasFilter())
            {
                /* @VERIFICATION : Ideally the end of filter state should get
                   propagated to the catch handler, this is an incompleteness,
                   but is not a security/compliance issue, since the only
                   interesting state is the 'thisInit' state.
                   */

                verCurrentState.esStackDepth = 0;

                BasicBlock* filterBB = HBtab->ebdFilter;

                // push catch arg the stack, spill to a temp if necessary
                // Note: can update HBtab->ebdFilter!
                const bool isSingleBlockFilter = (filterBB->bbNext == hndBegBB);
                filterBB = impPushCatchArgOnStack(filterBB, impGetObjectClass(), isSingleBlockFilter);

                impImportBlockPending(filterBB);
            }
        }
        else if (verTrackObjCtorInitState && HBtab->HasFaultHandler())
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
        JITDUMP("Marking BBF_INTERNAL block BB%02u as BBF_IMPORTED\n", block->bbNum);
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

        GenTreePtr addStmt = nullptr;

        /* Do the successors of 'block' have any other predecessors ?
           We do not want to do some of the optimizations related to multiRef
           if we can reimport blocks */

        unsigned multRef = impCanReimport ? unsigned(~0) : 0;

        switch (block->bbJumpKind)
        {
            case BBJ_COND:

                /* Temporarily remove the 'jtrue' from the end of the tree list */

                assert(impTreeLast);
                assert(impTreeLast->gtOper == GT_STMT);
                assert(impTreeLast->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);

                addStmt     = impTreeLast;
                impTreeLast = impTreeLast->gtPrev;

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

                /* Temporarily remove the GT_SWITCH from the end of the tree list */

                assert(impTreeLast);
                assert(impTreeLast->gtOper == GT_STMT);
                assert(impTreeLast->gtStmt.gtStmtExpr->gtOper == GT_SWITCH);

                addStmt     = impTreeLast;
                impTreeLast = impTreeLast->gtPrev;

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
            GenTreePtr tree = verCurrentState.esStack[level].val;

            /* VC generates code where it pushes a byref from one branch, and an int (ldc.i4 0) from
               the other. This should merge to a byref in unverifiable code.
               However, if the branch which leaves the TYP_I_IMPL on the stack is imported first, the
               successor would be imported assuming there was a TYP_I_IMPL on
               the stack. Thus the value would not get GC-tracked. Hence,
               change the temp to TYP_BYREF and reimport the successors.
               Note: We should only allow this in unverifiable code.
            */
            if (tree->gtType == TYP_BYREF && lvaTable[tempNum].lvType == TYP_I_IMPL && !verNeedsVerification())
            {
                lvaTable[tempNum].lvType = TYP_BYREF;
                impReimportMarkSuccessors(block);
                markImport = true;
            }

#ifdef _TARGET_64BIT_
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
                verCurrentState.esStack[level].val = gtNewCastNode(TYP_I_IMPL, tree, TYP_I_IMPL);
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
                    verCurrentState.esStack[level].val = gtNewCastNode(TYP_I_IMPL, tree, TYP_I_IMPL);
                }
            }
#endif // _TARGET_64BIT_

#if FEATURE_X87_DOUBLES
            // X87 stack doesn't differentiate between float/double
            // so promoting is no big deal.
            // For everybody else keep it as float until we have a collision and then promote
            // Just like for x64's TYP_INT<->TYP_I_IMPL

            if (multRef > 1 && tree->gtType == TYP_FLOAT)
            {
                verCurrentState.esStack[level].val = gtNewCastNode(TYP_DOUBLE, tree, TYP_DOUBLE);
            }

#else // !FEATURE_X87_DOUBLES

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
                verCurrentState.esStack[level].val = gtNewCastNode(TYP_DOUBLE, tree, TYP_DOUBLE);
            }

#endif // FEATURE_X87_DOUBLES

            /* If addStmt has a reference to tempNum (can only happen if we
               are spilling to the temps already used by a previous block),
               we need to spill addStmt */

            if (addStmt && !newTemps && gtHasRef(addStmt->gtStmt.gtStmtExpr, tempNum, false))
            {
                GenTreePtr addTree = addStmt->gtStmt.gtStmtExpr;

                if (addTree->gtOper == GT_JTRUE)
                {
                    GenTreePtr relOp = addTree->gtOp.gtOp1;
                    assert(relOp->OperIsCompare());

                    var_types type = genActualType(relOp->gtOp.gtOp1->TypeGet());

                    if (gtHasRef(relOp->gtOp.gtOp1, tempNum, false))
                    {
                        unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt JTRUE ref Op1"));
                        impAssignTempGen(temp, relOp->gtOp.gtOp1, level);
                        type              = genActualType(lvaTable[temp].TypeGet());
                        relOp->gtOp.gtOp1 = gtNewLclvNode(temp, type);
                    }

                    if (gtHasRef(relOp->gtOp.gtOp2, tempNum, false))
                    {
                        unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt JTRUE ref Op2"));
                        impAssignTempGen(temp, relOp->gtOp.gtOp2, level);
                        type              = genActualType(lvaTable[temp].TypeGet());
                        relOp->gtOp.gtOp2 = gtNewLclvNode(temp, type);
                    }
                }
                else
                {
                    assert(addTree->gtOper == GT_SWITCH && genActualTypeIsIntOrI(addTree->gtOp.gtOp1->TypeGet()));

                    unsigned temp = lvaGrabTemp(true DEBUGARG("spill addStmt SWITCH"));
                    impAssignTempGen(temp, addTree->gtOp.gtOp1, level);
                    addTree->gtOp.gtOp1 = gtNewLclvNode(temp, genActualType(addTree->gtOp.gtOp1->TypeGet()));
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

        if (addStmt)
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
        printf("\nimpImportBlockPending for BB%02u\n", block->bbNum);
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
                      "Block at offset %4.4x to %4.4x in %s entered with different stack depths.\n"
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

                JITDUMP("Adding BB%02u to pending set due to new merge result\n", block->bbNum);
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
        printf("Added PendingDsc - %08p for BB%02u\n", dspPtr(dsc), block->bbNum);
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
    JITDUMP("\nimpReimportBlockPending for BB%02u", block->bbNum);

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
        printf("Added PendingDsc - %08p for BB%02u\n", dspPtr(dsc), block->bbNum);
    }
#endif
}

void* Compiler::BlockListNode::operator new(size_t sz, Compiler* comp)
{
    if (comp->impBlockListNodeFreeList == nullptr)
    {
        return (BlockListNode*)comp->compGetMem(sizeof(BlockListNode), CMK_BasicBlock);
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
            GenTreePtr tree = es->esStack[level].val;
            if ((tree->gtOper == GT_LCL_VAR) || (tree->gtOper == GT_LCL_FLD))
            {
                unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
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
        printf("\n*************** In impGetSpillTmpBase(BB%02u)\n", block->bbNum);
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
        printf("\n*************** In impReimportSpillClique(BB%02u)\n", block->bbNum);
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

    block->bbEntryState = (EntryState*)compGetMemA(sizeof(EntryState));

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
            GenTreePtr tree                         = srcState->esStack[level].val;
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
    verTrackObjCtorInitState        = FALSE;
    verCurrentState.thisInitialized = TIS_Bottom;

    if (tiVerificationNeeded)
    {
        // Track this ptr initialization
        if (!info.compIsStatic && (info.compFlags & CORINFO_FLG_CONSTRUCTOR) && lvaTable[0].lvVerTypeInfo.IsObjRef())
        {
            verTrackObjCtorInitState        = TRUE;
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

void Compiler::impImport(BasicBlock* method)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In impImport() for %s\n", info.compFullName);
    }
#endif

    /* Allocate the stack contents */

    if (info.compMaxStack <= sizeof(impSmallStack) / sizeof(impSmallStack[0]))
    {
        /* Use local variable, don't waste time allocating on the heap */

        impStkSize              = sizeof(impSmallStack) / sizeof(impSmallStack[0]);
        verCurrentState.esStack = impSmallStack;
    }
    else
    {
        impStkSize              = info.compMaxStack;
        verCurrentState.esStack = new (this, CMK_ImpStack) StackEntry[impStkSize];
    }

    // initialize the entry state at start of method
    verInitCurrentState();

    // Initialize stuff related to figuring "spill cliques" (see spec comment for impGetSpillTmpBase).
    Compiler* inlineRoot = impInlineRoot();
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

    /* Add the entry-point to the worker-list */

    // Skip leading internal blocks. There can be one as a leading scratch BB, and more
    // from EH normalization.
    // NOTE: It might be possible to always just put fgFirstBB on the pending list, and let everything else just fall
    // out.
    for (; method->bbFlags & BBF_INTERNAL; method = method->bbNext)
    {
        // Treat these as imported.
        assert(method->bbJumpKind == BBJ_NONE); // We assume all the leading ones are fallthrough.
        JITDUMP("Marking leading BBF_INTERNAL block BB%02u as BBF_IMPORTED\n", method->bbNum);
        method->bbFlags |= BBF_IMPORTED;
    }

    impImportBlockPending(method);

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

#ifdef _TARGET_64BIT_
            // On AMD64, during verification we have to match JIT64 behavior since the VM is very tighly
            // coupled with the JIT64 IL Verification logic.  Look inside verHandleVerificationFailure
            // method for further explanation on why we raise this exception instead of making the jitted
            // code throw the verification exception during execution.
            if (tiVerificationNeeded && opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IMPORT_ONLY))
            {
                BADCODE("Basic block marked as not verifiable");
            }
            else
#endif // _TARGET_64BIT_
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

    *lclVarTreeOut will contain the GT_LCL_VAR tree when it returns TRUE.

 */

BOOL Compiler::impIsAddressInLocal(GenTreePtr tree, GenTreePtr* lclVarTreeOut)
{
    if (tree->gtOper != GT_ADDR)
    {
        return FALSE;
    }

    GenTreePtr op = tree->gtOp.gtOp1;
    while (op->gtOper == GT_FIELD)
    {
        op = op->gtField.gtFldObj;
        if (op && op->gtOper == GT_ADDR) // Skip static fields where op will be NULL.
        {
            op = op->gtOp.gtOp1;
        }
        else
        {
            return false;
        }
    }

    if (op->gtOper == GT_LCL_VAR)
    {
        *lclVarTreeOut = op;
        return TRUE;
    }
    else
    {
        return FALSE;
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
    assert(pInlineInfo != nullptr && compIsForInlining() || // Perform the actual inlining.
           pInlineInfo == nullptr && !compIsForInlining()   // Calculate the static inlining hint for ngen.
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
            GenTreePtr thisArg = pInlineInfo->iciCall->gtCall.gtCallObjp;
            assert(thisArg);
            bool isSameThis = impIsThis(thisArg);
            inlineResult->NoteBool(InlineObservation::CALLSITE_IS_SAME_THIS, isSameThis);
        }
    }

    // Note if the callee's class is a promotable struct
    if ((info.compClassAttr & CORINFO_FLG_VALUECLASS) != 0)
    {
        lvaStructPromotionInfo structPromotionInfo;
        lvaCanPromoteStructType(info.compClassHnd, &structPromotionInfo, false);
        if (structPromotionInfo.canPromote)
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
    if ((pInlineInfo == nullptr) || (pInlineInfo->iciBlock->bbWeight >= BB_MAX_WEIGHT))
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

    // Also capture the block weight of the call site.  In the prejit
    // root case, assume there's some hot call site for this method.
    unsigned weight = 0;

    if (pInlineInfo != nullptr)
    {
        weight = pInlineInfo->iciBlock->bbWeight;
    }
    else
    {
        weight = BB_MAX_WEIGHT;
    }

    inlineResult->NoteInt(InlineObservation::CALLSITE_FREQUENCY, static_cast<int>(frequency));
    inlineResult->NoteInt(InlineObservation::CALLSITE_WEIGHT, static_cast<int>(weight));
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

void Compiler::impCheckCanInline(GenTreePtr             call,
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
        GenTreePtr             call;
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
            DWORD                  dwRestrictions = 0;
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
            // If it can be done, we will try to inline the method. If inlining
            // succeeds, then we will do the non-speculative initClass() and commit it.
            // If this speculative call to initClass() fails, there is no point
            // trying to inline this method.
            initClassResult =
                pParam->pThis->info.compCompHnd->initClass(nullptr /* field */, pParam->fncHandle /* method */,
                                                           pParam->exactContextHnd /* context */,
                                                           TRUE /* speculative */);

            if (initClassResult & CORINFO_INITCLASS_DONT_INLINE)
            {
                pParam->result->NoteFatal(InlineObservation::CALLSITE_CLASS_INIT_FAILURE_SPEC);
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
                GenTreePtr thisArg = pParam->call->gtCall.gtCallObjp;
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

            //
            // Allocate an InlineCandidateInfo structure
            //
            InlineCandidateInfo* pInfo;
            pInfo = new (pParam->pThis, CMK_Inlining) InlineCandidateInfo;

            pInfo->dwRestrictions  = dwRestrictions;
            pInfo->methInfo        = methInfo;
            pInfo->methAttr        = pParam->methAttr;
            pInfo->clsHandle       = clsHandle;
            pInfo->clsAttr         = clsAttr;
            pInfo->fncRetType      = fncRetType;
            pInfo->exactContextHnd = pParam->exactContextHnd;
            pInfo->ilCallerHandle  = pParam->pThis->info.compMethodHnd;
            pInfo->initClassResult = initClassResult;

            *(pParam->ppInlineCandidateInfo) = pInfo;

        _exit:;
        },
        &param);
    if (!success)
    {
        param.result->NoteFatal(InlineObservation::CALLSITE_COMPILATION_ERROR);
    }
}

void Compiler::impInlineRecordArgInfo(InlineInfo*   pInlineInfo,
                                      GenTreePtr    curArgVal,
                                      unsigned      argNum,
                                      InlineResult* inlineResult)
{
    InlArgInfo* inlCurArgInfo = &pInlineInfo->inlArgInfo[argNum];

    if (curArgVal->gtOper == GT_MKREFANY)
    {
        inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_IS_MKREFANY);
        return;
    }

    inlCurArgInfo->argNode = curArgVal;

    GenTreePtr lclVarTree;
    if (impIsAddressInLocal(curArgVal, &lclVarTree) && varTypeIsStruct(lclVarTree))
    {
        inlCurArgInfo->argIsByRefToStructLocal = true;
#ifdef FEATURE_SIMD
        if (lvaTable[lclVarTree->AsLclVarCommon()->gtLclNum].lvSIMDType)
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
        curArgVal->gtLclVar.gtLclILoffs = argNum;
    }

    if ((curArgVal->OperKind() & GTK_CONST) ||
        ((curArgVal->gtOper == GT_ADDR) && (curArgVal->gtOp.gtOp1->gtOper == GT_LCL_VAR)))
    {
        inlCurArgInfo->argIsInvariant = true;
        if (inlCurArgInfo->argIsThis && (curArgVal->gtOper == GT_CNS_INT) && (curArgVal->gtIntCon.gtIconVal == 0))
        {
            /* Abort, but do not mark as not inlinable */
            inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_HAS_NULL_THIS);
            return;
        }
    }

    if (!inlCurArgInfo->argIsInvariant && gtHasLocalsWithAddrOp(curArgVal))
    {
        inlCurArgInfo->argHasLdargaOp = true;
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

/*****************************************************************************
 *
 */

void Compiler::impInlineInitVars(InlineInfo* pInlineInfo)
{
    assert(!compIsForInlining());

    GenTreePtr           call         = pInlineInfo->iciCall;
    CORINFO_METHOD_INFO* methInfo     = &pInlineInfo->inlineCandidateInfo->methInfo;
    unsigned             clsAttr      = pInlineInfo->inlineCandidateInfo->clsAttr;
    InlArgInfo*          inlArgInfo   = pInlineInfo->inlArgInfo;
    InlLclVarInfo*       lclVarInfo   = pInlineInfo->lclVarInfo;
    InlineResult*        inlineResult = pInlineInfo->inlineResult;

    const bool hasRetBuffArg = impMethodInfo_hasRetBuffArg(methInfo);

    /* init the argument stuct */

    memset(inlArgInfo, 0, (MAX_INL_ARGS + 1) * sizeof(inlArgInfo[0]));

    /* Get hold of the 'this' pointer and the argument list proper */

    GenTreePtr thisArg = call->gtCall.gtCallObjp;
    GenTreePtr argList = call->gtCall.gtCallArgs;
    unsigned   argCnt  = 0; // Count of the arguments

    assert((methInfo->args.hasThis()) == (thisArg != nullptr));

    if (thisArg)
    {
        inlArgInfo[0].argIsThis = true;

        impInlineRecordArgInfo(pInlineInfo, thisArg, argCnt, inlineResult);

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
    unsigned typeCtxtArg = thisArg ? 1 : 0;
#else  // USER_ARGS_COME_LAST
    unsigned typeCtxtArg = methInfo->args.totalILArgs();
#endif // USER_ARGS_COME_LAST

    for (GenTreePtr argTmp = argList; argTmp; argTmp = argTmp->gtOp.gtOp2)
    {
        if (argTmp == argList && hasRetBuffArg)
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

        assert(argTmp->gtOper == GT_LIST);
        GenTreePtr argVal = argTmp->gtOp.gtOp1;

        impInlineRecordArgInfo(pInlineInfo, argVal, argCnt, inlineResult);

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

    if (thisArg)
    {
        var_types sigType;

        if (clsAttr & CORINFO_FLG_VALUECLASS)
        {
            sigType = TYP_BYREF;
        }
        else
        {
            sigType = TYP_REF;
        }

        lclVarInfo[0].lclVerTypeInfo = verMakeTypeInfo(pInlineInfo->inlineCandidateInfo->clsHandle);
        lclVarInfo[0].lclHasLdlocaOp = false;

#ifdef FEATURE_SIMD
        // We always want to check isSIMDClass, since we want to set foundSIMDType (to increase
        // the inlining multiplier) for anything in that assembly.
        // But we only need to normalize it if it is a TYP_STRUCT
        // (which we need to do even if we have already set foundSIMDType).
        if ((!foundSIMDType || (sigType == TYP_STRUCT)) && isSIMDClass(&(lclVarInfo[0].lclVerTypeInfo)))
        {
            if (sigType == TYP_STRUCT)
            {
                sigType = impNormStructType(lclVarInfo[0].lclVerTypeInfo.GetClassHandle());
            }
            foundSIMDType = true;
        }
#endif // FEATURE_SIMD
        lclVarInfo[0].lclTypeInfo = sigType;

        assert(varTypeIsGC(thisArg->gtType) ||   // "this" is managed
               (thisArg->gtType == TYP_I_IMPL && // "this" is unmgd but the method's class doesnt care
                (clsAttr & CORINFO_FLG_VALUECLASS)));

        if (genActualType(thisArg->gtType) != genActualType(sigType))
        {
            if (sigType == TYP_REF)
            {
                /* The argument cannot be bashed into a ref (see bug 750871) */
                inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_NO_BASH_TO_REF);
                return;
            }

            /* This can only happen with byrefs <-> ints/shorts */

            assert(genActualType(sigType) == TYP_I_IMPL || sigType == TYP_BYREF);
            assert(genActualType(thisArg->gtType) == TYP_I_IMPL || thisArg->gtType == TYP_BYREF);

            if (sigType == TYP_BYREF)
            {
                lclVarInfo[0].lclVerTypeInfo = typeInfo(varType2tiType(TYP_I_IMPL));
            }
            else if (thisArg->gtType == TYP_BYREF)
            {
                assert(sigType == TYP_I_IMPL);

                /* If possible change the BYREF to an int */
                if (thisArg->IsVarAddr())
                {
                    thisArg->gtType              = TYP_I_IMPL;
                    lclVarInfo[0].lclVerTypeInfo = typeInfo(varType2tiType(TYP_I_IMPL));
                }
                else
                {
                    /* Arguments 'int <- byref' cannot be bashed */
                    inlineResult->NoteFatal(InlineObservation::CALLSITE_ARG_NO_BASH_TO_INT);
                    return;
                }
            }
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
        if ((!foundSIMDType || (sigType == TYP_STRUCT)) && isSIMDClass(&(lclVarInfo[i].lclVerTypeInfo)))
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

        GenTreePtr inlArgNode = inlArgInfo[i].argNode;

        if (sigType != inlArgNode->gtType)
        {
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
                    if (inlArgNode->IsVarAddr())
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
                else if (genTypeSize(sigType) < EA_PTRSIZE)
                {
                    /* Narrowing cast */

                    if (inlArgNode->gtOper == GT_LCL_VAR &&
                        !lvaTable[inlArgNode->gtLclVarCommon.gtLclNum].lvNormalizeOnLoad() &&
                        sigType == lvaGetRealType(inlArgNode->gtLclVarCommon.gtLclNum))
                    {
                        /* We don't need to insert a cast here as the variable
                           was assigned a normalized value of the right type */

                        continue;
                    }

                    inlArgNode = inlArgInfo[i].argNode = gtNewCastNode(TYP_INT, inlArgNode, sigType);

                    inlArgInfo[i].argIsLclVar = false;

                    /* Try to fold the node in case we have constant arguments */

                    if (inlArgInfo[i].argIsInvariant)
                    {
                        inlArgNode            = gtFoldExprConst(inlArgNode);
                        inlArgInfo[i].argNode = inlArgNode;
                        assert(inlArgNode->OperIsConst());
                    }
                }
#ifdef _TARGET_64BIT_
                else if (genTypeSize(genActualType(inlArgNode->gtType)) < genTypeSize(sigType))
                {
                    // This should only happen for int -> native int widening
                    inlArgNode = inlArgInfo[i].argNode = gtNewCastNode(genActualType(sigType), inlArgNode, sigType);

                    inlArgInfo[i].argIsLclVar = false;

                    /* Try to fold the node in case we have constant arguments */

                    if (inlArgInfo[i].argIsInvariant)
                    {
                        inlArgNode            = gtFoldExprConst(inlArgNode);
                        inlArgInfo[i].argNode = inlArgNode;
                        assert(inlArgNode->OperIsConst());
                    }
                }
#endif // _TARGET_64BIT_
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
        lclVarInfo[i + argCnt].lclIsPinned    = isPinned;
        lclVarInfo[i + argCnt].lclTypeInfo    = type;

        if (varTypeIsGC(type))
        {
            pInlineInfo->numberOfGcRefLocals++;
        }

        if (isPinned)
        {
            // Pinned locals may cause inlines to fail.
            inlineResult->Note(InlineObservation::CALLEE_HAS_PINNED_LOCALS);
            if (inlineResult->IsFailure())
            {
                return;
            }
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
        if ((!foundSIMDType || (type == TYP_STRUCT)) && isSIMDClass(&(lclVarInfo[i + argCnt].lclVerTypeInfo)))
        {
            foundSIMDType = true;
            if (featureSIMD && type == TYP_STRUCT)
            {
                var_types structType = impNormStructType(lclVarInfo[i + argCnt].lclVerTypeInfo.GetClassHandle());
                lclVarInfo[i + argCnt].lclTypeInfo = structType;
            }
        }
#endif // FEATURE_SIMD
    }

#ifdef FEATURE_SIMD
    if (!foundSIMDType && (call->AsCall()->gtRetClsHnd != nullptr) && isSIMDClass(call->AsCall()->gtRetClsHnd))
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
            assert((info.compRetNativeType == TYP_VOID) || (lvaInlineeReturnSpillTemp != BAD_VAR_NUM));
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

GenTreePtr Compiler::impInlineFetchArg(unsigned lclNum, InlArgInfo* inlArgInfo, InlLclVarInfo* lclVarInfo)
{
    // Cache the relevant arg and lcl info for this argument.
    // We will modify argInfo but not lclVarInfo.
    InlArgInfo&          argInfo          = inlArgInfo[lclNum];
    const InlLclVarInfo& lclInfo          = lclVarInfo[lclNum];
    const bool           argCanBeModified = argInfo.argHasLdargaOp || argInfo.argHasStargOp;
    const var_types      lclTyp           = lclInfo.lclTypeInfo;
    GenTreePtr           op1              = nullptr;

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
        op1 = gtCloneExpr(argInfo.argNode);
        PREFIX_ASSUME(op1 != nullptr);
        argInfo.argTmpNum = BAD_VAR_NUM;
    }
    else if (argInfo.argIsLclVar && !argCanBeModified)
    {
        // Directly substitute caller locals
        //
        // Use the caller-supplied node if this is the first use.
        op1               = argInfo.argNode;
        argInfo.argTmpNum = op1->gtLclVarCommon.gtLclNum;

        // Use an equivalent copy if this is the second or subsequent
        // use, or if we need to retype.
        //
        // Note argument type mismatches that prevent inlining should
        // have been caught in impInlineInitVars.
        if (argInfo.argIsUsed || (op1->TypeGet() != lclTyp))
        {
            assert(op1->gtOper == GT_LCL_VAR);
            assert(lclNum == op1->gtLclVar.gtLclILoffs);

            var_types newTyp = lclTyp;

            if (!lvaTable[op1->gtLclVarCommon.gtLclNum].lvNormalizeOnLoad())
            {
                newTyp = genActualType(lclTyp);
            }

            // Create a new lcl var node - remember the argument lclNum
            op1 = gtNewLclvNode(op1->gtLclVarCommon.gtLclNum, newTyp, op1->gtLclVar.gtLclILoffs);
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
        assert(argInfo.argNode->TypeGet() == TYP_BYREF || argInfo.argNode->TypeGet() == TYP_I_IMPL);
        op1 = gtCloneExpr(argInfo.argNode);
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

            // Copy over class handle for ref types. Note this may be
            // further improved if it is a shared type and we know the exact context.
            if (lclTyp == TYP_REF)
            {
                lvaSetClass(tmpNum, lclInfo.lclVerTypeInfo.GetClassHandleForObjRef());
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
            // Also, if arguments have global references, we need to
            // evaluate them to a temp before the inlined body as the
            // inlined body may be modifying the global ref.
            // TODO-1stClassStructs: We currently do not reuse an existing lclVar
            // if it is a struct, because it requires some additional handling.

            if (!varTypeIsStruct(lclTyp) && !argInfo.argHasSideEff && !argInfo.argHasGlobRef)
            {
                /* Get a *LARGE* LCL_VAR node */
                op1 = gtNewLclLNode(tmpNum, genActualType(lclTyp), lclNum);

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

BOOL Compiler::impInlineIsThis(GenTreePtr tree, InlArgInfo* inlArgInfo)
{
    assert(compIsForInlining());
    return (tree->gtOper == GT_LCL_VAR && tree->gtLclVarCommon.gtLclNum == inlArgInfo[0].argTmpNum);
}

//-----------------------------------------------------------------------------
// This function checks if a dereference in the inlinee can guarantee that
// the "this" is non-NULL.
// If we haven't hit a branch or a side effect, and we are dereferencing
// from 'this' to access a field or make GTF_CALL_NULLCHECK call,
// then we can avoid a separate null pointer check.
//
// "additionalTreesToBeEvaluatedBefore"
// is the set of pending trees that have not yet been added to the statement list,
// and which have been removed from verCurrentState.esStack[]

BOOL Compiler::impInlineIsGuaranteedThisDerefBeforeAnySideEffects(GenTreePtr  additionalTreesToBeEvaluatedBefore,
                                                                  GenTreePtr  variableBeingDereferenced,
                                                                  InlArgInfo* inlArgInfo)
{
    assert(compIsForInlining());
    assert(opts.OptEnabled(CLFLG_INLINING));

    BasicBlock* block = compCurBB;

    GenTreePtr stmt;
    GenTreePtr expr;

    if (block != fgFirstBB)
    {
        return FALSE;
    }

    if (!impInlineIsThis(variableBeingDereferenced, inlArgInfo))
    {
        return FALSE;
    }

    if (additionalTreesToBeEvaluatedBefore &&
        GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(additionalTreesToBeEvaluatedBefore->gtFlags))
    {
        return FALSE;
    }

    for (stmt = impTreeList->gtNext; stmt; stmt = stmt->gtNext)
    {
        expr = stmt->gtStmt.gtStmtExpr;

        if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(expr->gtFlags))
        {
            return FALSE;
        }
    }

    for (unsigned level = 0; level < verCurrentState.esStackDepth; level++)
    {
        unsigned stackTreeFlags = verCurrentState.esStack[level].val->gtFlags;
        if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(stackTreeFlags))
        {
            return FALSE;
        }
    }

    return TRUE;
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
//    If callNode is an inline candidate, this method sets the flag
//    GTF_CALL_INLINE_CANDIDATE, and ensures that helper methods have
//    filled in the associated InlineCandidateInfo.
//
//    If callNode is not an inline candidate, and the reason is
//    something that is inherent to the method being called, the
//    method may be marked as "noinline" to short-circuit any
//    future assessments of calls to this method.

void Compiler::impMarkInlineCandidate(GenTreePtr             callNode,
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

    GenTreeCall* call = callNode->AsCall();
    InlineResult inlineResult(this, call, nullptr, "impMarkInlineCandidate");

    // Don't inline if not optimizing root method
    if (opts.compDbgCode)
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_DEBUG_CODEGEN);
        return;
    }

    // Don't inline if inlining into root method is disabled.
    if (InlineStrategy::IsNoInline(info.compCompHnd, info.compMethodHnd))
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_IS_JIT_NOINLINE);
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

    if ((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) != GTF_CALL_NONVIRT)
    {
        inlineResult.NoteFatal(InlineObservation::CALLSITE_IS_NOT_DIRECT);
        return;
    }

    /* Ignore helper calls */

    if (call->gtCallType == CT_HELPER)
    {
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

    CORINFO_METHOD_HANDLE fncHandle = call->gtCallMethHnd;
    unsigned              methAttr;

    // Reuse method flags from the original callInfo if possible
    if (fncHandle == callInfo->hMethod)
    {
        methAttr = callInfo->methodFlags;
    }
    else
    {
        methAttr = info.compCompHnd->getMethodAttribs(fncHandle);
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

    /* If the caller's stack frame is marked, then we can't do any inlining. Period. */

    if (opts.compNeedSecurityCheck)
    {
        inlineResult.NoteFatal(InlineObservation::CALLER_NEEDS_SECURITY_CHECK);
        return;
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

    /* Do not inline if callee needs security checks (since they would then mark the wrong frame) */

    if (methAttr & CORINFO_FLG_SECURITYCHECK)
    {
        inlineResult.NoteFatal(InlineObservation::CALLEE_NEEDS_SECURITY_CHECK);
        return;
    }

    InlineCandidateInfo* inlineCandidateInfo = nullptr;
    impCheckCanInline(call, fncHandle, methAttr, exactContextHnd, &inlineCandidateInfo, &inlineResult);

    if (inlineResult.IsFailure())
    {
        return;
    }

    // The old value should be NULL
    assert(call->gtInlineCandidateInfo == nullptr);

    // The new value should not be NULL.
    assert(inlineCandidateInfo != nullptr);
    inlineCandidateInfo->exactContextNeedsRuntimeLookup = exactContextNeedsRuntimeLookup;

    call->gtInlineCandidateInfo = inlineCandidateInfo;

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

bool Compiler::IsTargetIntrinsic(CorInfoIntrinsics intrinsicId)
{
#if defined(_TARGET_AMD64_) || (defined(_TARGET_X86_) && !defined(LEGACY_BACKEND))
    switch (intrinsicId)
    {
        // Amd64 only has SSE2 instruction to directly compute sqrt/abs.
        //
        // TODO: Because the x86 backend only targets SSE for floating-point code,
        //       it does not treat Sine, Cosine, or Round as intrinsics (JIT32
        //       implemented those intrinsics as x87 instructions). If this poses
        //       a CQ problem, it may be necessary to change the implementation of
        //       the helper calls to decrease call overhead or switch back to the
        //       x87 instructions. This is tracked by #7097.
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
            return true;

        default:
            return false;
    }
#elif defined(_TARGET_ARM64_)
    switch (intrinsicId)
    {
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Round:
            return true;

        default:
            return false;
    }
#elif defined(_TARGET_ARM_)
    switch (intrinsicId)
    {
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Round:
            return true;

        default:
            return false;
    }
#elif defined(_TARGET_X86_)
    switch (intrinsicId)
    {
        case CORINFO_INTRINSIC_Sin:
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Round:
            return true;

        default:
            return false;
    }
#else
    // TODO: This portion of logic is not implemented for other arch.
    // The reason for returning true is that on all other arch the only intrinsic
    // enabled are target intrinsics.
    return true;
#endif //_TARGET_AMD64_
}

/******************************************************************************/
// Returns true if the given intrinsic will be implemented by calling System.Math
// methods.

bool Compiler::IsIntrinsicImplementedByUserCall(CorInfoIntrinsics intrinsicId)
{
    // Currently, if an math intrisic is not implemented by target-specific
    // intructions, it will be implemented by a System.Math call. In the
    // future, if we turn to implementing some of them with helper callers,
    // this predicate needs to be revisited.
    return !IsTargetIntrinsic(intrinsicId);
}

bool Compiler::IsMathIntrinsic(CorInfoIntrinsics intrinsicId)
{
    switch (intrinsicId)
    {
        case CORINFO_INTRINSIC_Sin:
        case CORINFO_INTRINSIC_Sqrt:
        case CORINFO_INTRINSIC_Abs:
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Round:
        case CORINFO_INTRINSIC_Cosh:
        case CORINFO_INTRINSIC_Sinh:
        case CORINFO_INTRINSIC_Tan:
        case CORINFO_INTRINSIC_Tanh:
        case CORINFO_INTRINSIC_Asin:
        case CORINFO_INTRINSIC_Acos:
        case CORINFO_INTRINSIC_Atan:
        case CORINFO_INTRINSIC_Atan2:
        case CORINFO_INTRINSIC_Log10:
        case CORINFO_INTRINSIC_Pow:
        case CORINFO_INTRINSIC_Exp:
        case CORINFO_INTRINSIC_Ceiling:
        case CORINFO_INTRINSIC_Floor:
            return true;
        default:
            return false;
    }
}

bool Compiler::IsMathIntrinsic(GenTreePtr tree)
{
    return (tree->OperGet() == GT_INTRINSIC) && IsMathIntrinsic(tree->gtIntrinsic.gtIntrinsicId);
}

//------------------------------------------------------------------------
// impDevirtualizeCall: Attempt to change a virtual vtable call into a
//   normal call
//
// Arguments:
//     call -- the call node to examine/modify
//     thisObj  -- the value of 'this' for the call
//     callInfo -- [IN/OUT] info about the call from the VM
//     exactContextHnd -- [OUT] updated context handle iff call devirtualized
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
void Compiler::impDevirtualizeCall(GenTreeCall*            call,
                                   GenTreePtr              thisObj,
                                   CORINFO_CALL_INFO*      callInfo,
                                   CORINFO_CONTEXT_HANDLE* exactContextHandle)
{
    // This should be a virtual vtable or virtual stub call.
    assert(call->IsVirtual());

    // Bail if not optimizing
    if (opts.MinOpts())
    {
        return;
    }

    // Bail if debuggable codegen
    if (opts.compDbgCode)
    {
        return;
    }

#if defined(DEBUG)
    // Bail if devirt is disabled.
    if (JitConfig.JitEnableDevirtualization() == 0)
    {
        return;
    }

    const bool doPrint = JitConfig.JitPrintDevirtualizedMethods() == 1;
#endif // DEBUG

    // Fetch information about the virtual method we're calling.
    CORINFO_METHOD_HANDLE baseMethod        = callInfo->hMethod;
    unsigned              baseMethodAttribs = callInfo->methodFlags;

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

#if !defined(FEATURE_CORECLR)
    // If base class is not beforefieldinit then devirtualizing may
    // cause us to miss a base class init trigger. Spec says we don't
    // need a trigger for ref class callvirts but desktop seems to
    // have one anyways. So defer.
    if ((baseClassAttribs & CORINFO_FLG_BEFOREFIELDINIT) == 0)
    {
        JITDUMP("\nimpDevirtualizeCall: base class has precise initialization, sorry\n");
        return;
    }
#endif // FEATURE_CORECLR

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

    // Bail if obj class is an interface.
    // See for instance System.ValueTuple`8::GetHashCode, where lcl 0 is System.IValueTupleInternal
    //   IL_021d:  ldloc.0
    //   IL_021e:  callvirt   instance int32 System.Object::GetHashCode()
    if ((objClassAttribs & CORINFO_FLG_INTERFACE) != 0)
    {
        JITDUMP("--- obj class is interface, sorry\n");
        return;
    }

    if (isInterface)
    {
        assert(call->IsVirtualStub());
        JITDUMP("--- base class is interface\n");
    }

    // Fetch the method that would be called based on the declared type of 'this'
    CORINFO_CONTEXT_HANDLE ownerType     = callInfo->contextHandle;
    CORINFO_METHOD_HANDLE  derivedMethod = info.compCompHnd->resolveVirtualMethod(baseMethod, objClass, ownerType);

    // If we failed to get a handle, we can't devirtualize.  This can
    // happen when prejitting, if the devirtualization crosses
    // servicing bubble boundaries.
    if (derivedMethod == nullptr)
    {
        JITDUMP("--- no derived method, sorry\n");
        return;
    }

    // Fetch method attributes to see if method is marked final.
    const DWORD derivedMethodAttribs = info.compCompHnd->getMethodAttribs(derivedMethod);
    const bool  derivedMethodIsFinal = ((derivedMethodAttribs & CORINFO_FLG_FINAL) != 0);

#if defined(DEBUG)
    const char* derivedClassName  = "?derivedClass";
    const char* derivedMethodName = "?derivedMethod";

    const char* note = "speculative";
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
        derivedMethodName = eeGetMethodName(derivedMethod, &derivedClassName);
        if (verbose)
        {
            printf("    devirt to %s::%s -- %s\n", derivedClassName, derivedMethodName, note);
            gtDispTree(call);
        }
    }
#endif // defined(DEBUG)

    if (!isExact && !objClassIsFinal && !derivedMethodIsFinal)
    {
        // Type is not exact, and neither class or method is final.
        //
        // We could speculatively devirtualize, but there's no
        // reason to believe the derived method is the one that
        // is likely to be invoked.
        //
        // If there's currently no further overriding (that is, at
        // the time of jitting, objClass has no subclasses that
        // override this method), then perhaps we'd be willing to
        // make a bet...?
        JITDUMP("    Class not final or exact, method not final, no devirtualization\n");
        return;
    }

    // For interface calls we must have an exact type or final class.
    if (isInterface && !isExact && !objClassIsFinal)
    {
        JITDUMP("    Class not final or exact for interface, no devirtualization\n");
        return;
    }

    JITDUMP("    %s; can devirtualize\n", note);

    // Make the updates.
    call->gtFlags &= ~GTF_CALL_VIRT_VTABLE;
    call->gtFlags &= ~GTF_CALL_VIRT_STUB;
    call->gtCallMethHnd = derivedMethod;
    call->gtCallType    = CT_USER_FUNC;

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

    // Fetch the class that introduced the derived method.
    //
    // Note this may not equal objClass, if there is a
    // final method that objClass inherits.
    CORINFO_CLASS_HANDLE derivedClass = info.compCompHnd->getMethodClass(derivedMethod);

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        // For R2R, getCallInfo triggers bookkeeping on the zap
        // side so we need to call it here.
        //
        // First, cons up a suitable resolved token.
        CORINFO_RESOLVED_TOKEN derivedResolvedToken = {};

        derivedResolvedToken.tokenScope   = info.compScopeHnd;
        derivedResolvedToken.tokenContext = callInfo->contextHandle;
        derivedResolvedToken.token        = info.compCompHnd->getMethodDefFromMethod(derivedMethod);
        derivedResolvedToken.tokenType    = CORINFO_TOKENKIND_Method;
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

    // Need to update call info too. This is fragile
    // but hopefully the derived method conforms to
    // the base in most other ways.
    callInfo->hMethod       = derivedMethod;
    callInfo->methodFlags   = derivedMethodAttribs;
    callInfo->contextHandle = MAKE_METHODCONTEXT(derivedMethod);

    // Update context handle.
    if ((exactContextHandle != nullptr) && (*exactContextHandle != nullptr))
    {
        *exactContextHandle = MAKE_METHODCONTEXT(derivedMethod);
    }

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
#endif // defined(DEBUG)
}

//------------------------------------------------------------------------
// impAllocateToken: create CORINFO_RESOLVED_TOKEN into jit-allocated memory and init it.
//
// Arguments:
//    token - init value for the allocated token.
//
// Return Value:
//    pointer to token into jit-allocated memory.
CORINFO_RESOLVED_TOKEN* Compiler::impAllocateToken(CORINFO_RESOLVED_TOKEN token)
{
    CORINFO_RESOLVED_TOKEN* memory = (CORINFO_RESOLVED_TOKEN*)compGetMem(sizeof(token));
    *memory                        = token;
    return memory;
}
