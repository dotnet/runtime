// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Morph                                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h" // for alloca

// Convert the given node into a call to the specified helper passing
// the given argument list.
//
// Tries to fold constants and also adds an edge for overflow exception
// returns the morphed tree
GenTreePtr Compiler::fgMorphCastIntoHelper(GenTreePtr tree, int helper, GenTreePtr oper)
{
    GenTree* result;

    /* If the operand is a constant, we'll try to fold it */
    if (oper->OperIsConst())
    {
        GenTreePtr oldTree = tree;

        tree = gtFoldExprConst(tree); // This may not fold the constant (NaN ...)

        if (tree != oldTree)
        {
            return fgMorphTree(tree);
        }
        else if (tree->OperKind() & GTK_CONST)
        {
            return fgMorphConst(tree);
        }

        // assert that oper is unchanged and that it is still a GT_CAST node
        noway_assert(tree->gtCast.CastOp() == oper);
        noway_assert(tree->gtOper == GT_CAST);
    }
    result = fgMorphIntoHelperCall(tree, helper, gtNewArgList(oper));
    assert(result == tree);
    return result;
}

/*****************************************************************************
 *
 *  Convert the given node into a call to the specified helper passing
 *  the given argument list.
 */

GenTreePtr Compiler::fgMorphIntoHelperCall(GenTreePtr tree, int helper, GenTreeArgList* args)
{
    tree->ChangeOper(GT_CALL);

    tree->gtFlags |= GTF_CALL;
    if (args)
    {
        tree->gtFlags |= (args->gtFlags & GTF_ALL_EFFECT);
    }
    tree->gtCall.gtCallType            = CT_HELPER;
    tree->gtCall.gtCallMethHnd         = eeFindHelper(helper);
    tree->gtCall.gtCallArgs            = args;
    tree->gtCall.gtCallObjp            = nullptr;
    tree->gtCall.gtCallLateArgs        = nullptr;
    tree->gtCall.fgArgInfo             = nullptr;
    tree->gtCall.gtRetClsHnd           = nullptr;
    tree->gtCall.gtCallMoreFlags       = 0;
    tree->gtCall.gtInlineCandidateInfo = nullptr;
    tree->gtCall.gtControlExpr         = nullptr;

#ifdef LEGACY_BACKEND
    tree->gtCall.gtCallRegUsedMask = RBM_NONE;
#endif // LEGACY_BACKEND

#if DEBUG
    // Helper calls are never candidates.

    tree->gtCall.gtInlineObservation = InlineObservation::CALLSITE_IS_CALL_TO_HELPER;
#endif // DEBUG

#ifdef FEATURE_READYTORUN_COMPILER
    tree->gtCall.gtEntryPoint.addr = nullptr;
#endif

#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
    if (varTypeIsLong(tree))
    {
        GenTreeCall*    callNode    = tree->AsCall();
        ReturnTypeDesc* retTypeDesc = callNode->GetReturnTypeDesc();
        retTypeDesc->Reset();
        retTypeDesc->InitializeLongReturnType(this);
        callNode->ClearOtherRegs();
    }
#endif

    /* Perform the morphing */

    tree = fgMorphArgs(tree->AsCall());

    return tree;
}

/*****************************************************************************
 *
 *  Determine if a relop must be morphed to a qmark to manifest a boolean value.
 *  This is done when code generation can't create straight-line code to do it.
 */
bool Compiler::fgMorphRelopToQmark(GenTreePtr tree)
{
#ifndef LEGACY_BACKEND
    return false;
#else  // LEGACY_BACKEND
    return (genActualType(tree->TypeGet()) == TYP_LONG) || varTypeIsFloating(tree->TypeGet());
#endif // LEGACY_BACKEND
}

/*****************************************************************************
 *
 *  Morph a cast node (we perform some very simple transformations here).
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTreePtr Compiler::fgMorphCast(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_CAST);
    noway_assert(genTypeSize(TYP_I_IMPL) == sizeof(void*));

    /* The first sub-operand is the thing being cast */

    GenTreePtr oper    = tree->gtCast.CastOp();
    var_types  srcType = genActualType(oper->TypeGet());
    unsigned   srcSize;

    var_types dstType = tree->CastToType();
    unsigned  dstSize = genTypeSize(dstType);

    // See if the cast has to be done in two steps.  R -> I
    if (varTypeIsFloating(srcType) && varTypeIsIntegral(dstType))
    {
        // Only x86 must go through TYP_DOUBLE to get to all
        // integral types everybody else can get straight there
        // except for when using helpers
        if (srcType == TYP_FLOAT
#if !FEATURE_STACK_FP_X87

#if defined(_TARGET_ARM64_)
            // Amd64: src = float, dst is overflow conversion.
            // This goes through helper and hence src needs to be converted to double.
            && tree->gtOverflow()
#elif defined(_TARGET_AMD64_)
            // Amd64: src = float, dst = uint64 or overflow conversion.
            // This goes through helper and hence src needs to be converted to double.
            && (tree->gtOverflow() || (dstType == TYP_ULONG))
#elif defined(_TARGET_ARM_)
            // Arm: src = float, dst = int64/uint64 or overflow conversion.
            && (tree->gtOverflow() || varTypeIsLong(dstType))
#endif

#endif // FEATURE_STACK_FP_X87
                )
        {
            oper = gtNewCastNode(TYP_DOUBLE, oper, TYP_DOUBLE);
        }

        // do we need to do it in two steps R -> I, '-> smallType
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM64_) || defined(_TARGET_AMD64_)
        if (dstSize < genTypeSize(TYP_INT))
        {
            oper = gtNewCastNodeL(TYP_INT, oper, TYP_INT);
            oper->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED | GTF_OVERFLOW | GTF_EXCEPT));
            tree->gtFlags &= ~GTF_UNSIGNED;
        }
#else
        if (dstSize < sizeof(void*))
        {
            oper = gtNewCastNodeL(TYP_I_IMPL, oper, TYP_I_IMPL);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
        }
#endif
        else
        {
            /* Note that if we need to use a helper call then we can not morph oper */
            if (!tree->gtOverflow())
            {
#ifdef _TARGET_ARM64_ // On ARM64 All non-overflow checking conversions can be optimized
                goto OPTIMIZECAST;
#else
                switch (dstType)
                {
                    case TYP_INT:
#ifdef _TARGET_X86_ // there is no rounding convert to integer instruction on ARM or x64 so skip this
                        if ((oper->gtOper == GT_INTRINSIC) &&
                            (oper->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round))
                        {
                            /* optimization: conv.i4(round.d(d)) -> round.i(d) */
                            oper->gtType = dstType;
                            return fgMorphTree(oper);
                        }
                        // if SSE2 is not enabled, we need the helper
                        else if (!opts.compCanUseSSE2)
                        {
                            return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2INT, oper);
                        }
                        else
#endif // _TARGET_X86_
                        {
                            goto OPTIMIZECAST;
                        }
#if defined(_TARGET_ARM_) || defined(_TARGET_AMD64_)
                    case TYP_UINT:
                        goto OPTIMIZECAST;
#else  // _TARGET_ARM_
                    case TYP_UINT:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2UINT, oper);
#endif // _TARGET_ARM_

#ifdef _TARGET_AMD64_
                    // SSE2 has instructions to convert a float/double directly to a long
                    case TYP_LONG:
                        goto OPTIMIZECAST;
#else
                    case TYP_LONG:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2LNG, oper);
#endif //_TARGET_AMD64_
                    case TYP_ULONG:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2ULNG, oper);
                    default:
                        break;
                }
#endif // _TARGET_ARM64_
            }
            else
            {
                switch (dstType)
                {
                    case TYP_INT:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2INT_OVF, oper);
                    case TYP_UINT:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2UINT_OVF, oper);
                    case TYP_LONG:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2LNG_OVF, oper);
                    case TYP_ULONG:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2ULNG_OVF, oper);
                    default:
                        break;
                }
            }
            noway_assert(!"Unexpected dstType");
        }
    }
#ifndef _TARGET_64BIT_
    // The code generation phase (for x86 & ARM32) does not handle casts
    // directly from [u]long to anything other than [u]int. Insert an
    // intermediate cast to native int.
    else if (varTypeIsLong(srcType) && varTypeIsSmall(dstType))
    {
        oper = gtNewCastNode(TYP_I_IMPL, oper, TYP_I_IMPL);
        oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT | GTF_UNSIGNED));
        tree->gtFlags &= ~GTF_UNSIGNED;
    }
#endif //!_TARGET_64BIT_

#ifdef _TARGET_ARM_
    else if ((dstType == TYP_FLOAT) && (srcType == TYP_DOUBLE) && (oper->gtOper == GT_CAST) &&
             !varTypeIsLong(oper->gtCast.CastOp()))
    {
        // optimization: conv.r4(conv.r8(?)) -> conv.r4(d)
        // except when the ultimate source is a long because there is no long-to-float helper, so it must be 2 step.
        // This happens semi-frequently because there is no IL 'conv.r4.un'
        oper->gtType       = TYP_FLOAT;
        oper->CastToType() = TYP_FLOAT;
        return fgMorphTree(oper);
    }
    // converts long/ulong --> float/double casts into helper calls.
    else if (varTypeIsFloating(dstType) && varTypeIsLong(srcType))
    {
        if (dstType == TYP_FLOAT)
        {
            // there is only a double helper, so we
            // - change the dsttype to double
            // - insert a cast from double to float
            // - recurse into the resulting tree
            tree->CastToType() = TYP_DOUBLE;
            tree->gtType       = TYP_DOUBLE;

            tree = gtNewCastNode(TYP_FLOAT, tree, TYP_FLOAT);

            return fgMorphTree(tree);
        }
        if (tree->gtFlags & GTF_UNSIGNED)
            return fgMorphCastIntoHelper(tree, CORINFO_HELP_ULNG2DBL, oper);
        return fgMorphCastIntoHelper(tree, CORINFO_HELP_LNG2DBL, oper);
    }
#endif //_TARGET_ARM_

#ifdef _TARGET_AMD64_
    // Do we have to do two step U4/8 -> R4/8 ?
    // Codegen supports the following conversion as one-step operation
    // a) Long -> R4/R8
    // b) U8 -> R8
    //
    // The following conversions are performed as two-step operations using above.
    // U4 -> R4/8 = U4-> Long -> R4/8
    // U8 -> R4   = U8 -> R8 -> R4
    else if ((tree->gtFlags & GTF_UNSIGNED) && varTypeIsFloating(dstType))
    {
        srcType = genUnsignedType(srcType);

        if (srcType == TYP_ULONG)
        {
            if (dstType == TYP_FLOAT)
            {
                // Codegen can handle U8 -> R8 conversion.
                // U8 -> R4 =  U8 -> R8 -> R4
                // - change the dsttype to double
                // - insert a cast from double to float
                // - recurse into the resulting tree
                tree->CastToType() = TYP_DOUBLE;
                tree->gtType       = TYP_DOUBLE;
                tree               = gtNewCastNode(TYP_FLOAT, tree, TYP_FLOAT);
                return fgMorphTree(tree);
            }
        }
        else if (srcType == TYP_UINT)
        {
            oper = gtNewCastNode(TYP_LONG, oper, TYP_LONG);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT | GTF_UNSIGNED));
            tree->gtFlags &= ~GTF_UNSIGNED;
        }
    }
#endif // _TARGET_AMD64_

#ifdef _TARGET_X86_
    // Do we have to do two step U4/8 -> R4/8 ?
    else if ((tree->gtFlags & GTF_UNSIGNED) && varTypeIsFloating(dstType))
    {
        srcType = genUnsignedType(srcType);

        if (srcType == TYP_ULONG)
        {
            return fgMorphCastIntoHelper(tree, CORINFO_HELP_ULNG2DBL, oper);
        }
        else if (srcType == TYP_UINT)
        {
            oper = gtNewCastNode(TYP_LONG, oper, TYP_LONG);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT | GTF_UNSIGNED));
            tree->gtFlags &= ~GTF_UNSIGNED;
        }
    }
#endif //_TARGET_XARCH_
    else if (varTypeIsGC(srcType) != varTypeIsGC(dstType))
    {
        // We are casting away GC information.  we would like to just
        // change the type to int, however this gives the emitter fits because
        // it believes the variable is a GC variable at the begining of the
        // instruction group, but is not turned non-gc by the code generator
        // we fix this by copying the GC pointer to a non-gc pointer temp.
        noway_assert(!varTypeIsGC(dstType) && "How can we have a cast to a GCRef here?");

        // We generate an assignment to an int and then do the cast from an int. With this we avoid
        // the gc problem and we allow casts to bytes, longs,  etc...
        unsigned lclNum = lvaGrabTemp(true DEBUGARG("Cast away GC"));
        oper->gtType    = TYP_I_IMPL;
        GenTreePtr asg  = gtNewTempAssign(lclNum, oper);
        oper->gtType    = srcType;

        // do the real cast
        GenTreePtr cast = gtNewCastNode(tree->TypeGet(), gtNewLclvNode(lclNum, TYP_I_IMPL), dstType);

        // Generate the comma tree
        oper = gtNewOperNode(GT_COMMA, tree->TypeGet(), asg, cast);

        return fgMorphTree(oper);
    }

    // Look for narrowing casts ([u]long -> [u]int) and try to push them
    // down into the operand before morphing it.
    //
    // It doesn't matter if this is cast is from ulong or long (i.e. if
    // GTF_UNSIGNED is set) because the transformation is only applied to
    // overflow-insensitive narrowing casts, which always silently truncate.
    //
    // Note that casts from [u]long to small integer types are handled above.
    if ((srcType == TYP_LONG) && ((dstType == TYP_INT) || (dstType == TYP_UINT)))
    {
        // As a special case, look for overflow-sensitive casts of an AND
        // expression, and see if the second operand is a small constant. Since
        // the result of an AND is bound by its smaller operand, it may be
        // possible to prove that the cast won't overflow, which will in turn
        // allow the cast's operand to be transformed.
        if (tree->gtOverflow() && (oper->OperGet() == GT_AND))
        {
            GenTreePtr andOp2 = oper->gtOp.gtOp2;

            // Special case to the special case: AND with a casted int.
            if ((andOp2->OperGet() == GT_CAST) && (andOp2->gtCast.CastOp()->OperGet() == GT_CNS_INT))
            {
                // gtFoldExprConst will deal with whether the cast is signed or
                // unsigned, or overflow-sensitive.
                andOp2 = oper->gtOp.gtOp2 = gtFoldExprConst(andOp2);
            }

            // Look for a constant less than 2^{32} for a cast to uint, or less
            // than 2^{31} for a cast to int.
            int maxWidth = (dstType == TYP_UINT) ? 32 : 31;

            if ((andOp2->OperGet() == GT_CNS_NATIVELONG) && ((andOp2->gtIntConCommon.LngValue() >> maxWidth) == 0))
            {
                // This cast can't overflow.
                tree->gtFlags &= ~(GTF_OVERFLOW | GTF_EXCEPT);
            }
        }

        // Only apply this transformation during global morph,
        // when neither the cast node nor the oper node may throw an exception
        // based on the upper 32 bits.
        //
        if (fgGlobalMorph && !tree->gtOverflow() && !oper->gtOverflowEx())
        {
            // For these operations the lower 32 bits of the result only depends
            // upon the lower 32 bits of the operands
            //
            if ((oper->OperGet() == GT_ADD) || (oper->OperGet() == GT_MUL) || (oper->OperGet() == GT_AND) ||
                (oper->OperGet() == GT_OR) || (oper->OperGet() == GT_XOR))
            {
                DEBUG_DESTROY_NODE(tree);

                // Insert narrowing casts for op1 and op2
                oper->gtOp.gtOp1 = gtNewCastNode(TYP_INT, oper->gtOp.gtOp1, dstType);
                oper->gtOp.gtOp2 = gtNewCastNode(TYP_INT, oper->gtOp.gtOp2, dstType);

                // Clear the GT_MUL_64RSLT if it is set
                if (oper->gtOper == GT_MUL && (oper->gtFlags & GTF_MUL_64RSLT))
                {
                    oper->gtFlags &= ~GTF_MUL_64RSLT;
                }

                // The operation now produces a 32-bit result.
                oper->gtType = TYP_INT;

                // Remorph the new tree as the casts that we added may be folded away.
                return fgMorphTree(oper);
            }
        }
    }

OPTIMIZECAST:
    noway_assert(tree->gtOper == GT_CAST);

    /* Morph the operand */
    tree->gtCast.CastOp() = oper = fgMorphTree(oper);

    /* Reset the call flag */
    tree->gtFlags &= ~GTF_CALL;

    /* unless we have an overflow cast, reset the except flag */
    if (!tree->gtOverflow())
    {
        tree->gtFlags &= ~GTF_EXCEPT;
    }

    /* Just in case new side effects were introduced */
    tree->gtFlags |= (oper->gtFlags & GTF_ALL_EFFECT);

    srcType = oper->TypeGet();

    /* if GTF_UNSIGNED is set then force srcType to an unsigned type */
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    srcSize = genTypeSize(srcType);

    if (!gtIsActiveCSE_Candidate(tree)) // tree cannot be a CSE candidate
    {
        /* See if we can discard the cast */
        if (varTypeIsIntegral(srcType) && varTypeIsIntegral(dstType))
        {
            if (srcType == dstType)
            { // Certainly if they are identical it is pointless
                goto REMOVE_CAST;
            }

            if (oper->OperGet() == GT_LCL_VAR && varTypeIsSmall(dstType))
            {
                unsigned   varNum = oper->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc = &lvaTable[varNum];
                if (varDsc->TypeGet() == dstType && varDsc->lvNormalizeOnStore())
                {
                    goto REMOVE_CAST;
                }
            }

            bool unsignedSrc = varTypeIsUnsigned(srcType);
            bool unsignedDst = varTypeIsUnsigned(dstType);
            bool signsDiffer = (unsignedSrc != unsignedDst);

            // For same sized casts with
            //    the same signs or non-overflow cast we discard them as well
            if (srcSize == dstSize)
            {
                /* This should have been handled above */
                noway_assert(varTypeIsGC(srcType) == varTypeIsGC(dstType));

                if (!signsDiffer)
                {
                    goto REMOVE_CAST;
                }

                if (!tree->gtOverflow())
                {
                    /* For small type casts, when necessary we force
                       the src operand to the dstType and allow the
                       implied load from memory to perform the casting */
                    if (varTypeIsSmall(srcType))
                    {
                        switch (oper->gtOper)
                        {
                            case GT_IND:
                            case GT_CLS_VAR:
                            case GT_LCL_FLD:
                            case GT_ARR_ELEM:
                                oper->gtType = dstType;
                                goto REMOVE_CAST;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        goto REMOVE_CAST;
                    }
                }
            }

            if (srcSize < dstSize) // widening cast
            {
                // Keep any long casts
                if (dstSize == sizeof(int))
                {
                    // Only keep signed to unsigned widening cast with overflow check
                    if (!tree->gtOverflow() || !unsignedDst || unsignedSrc)
                    {
                        goto REMOVE_CAST;
                    }
                }

                // Casts from signed->unsigned can never overflow while widening

                if (unsignedSrc || !unsignedDst)
                {
                    tree->gtFlags &= ~GTF_OVERFLOW;
                }
            }
            else
            {
                // Try to narrow the operand of the cast and discard the cast
                // Note: Do not narrow a cast that is marked as a CSE
                // And do not narrow if the oper is marked as a CSE either
                //
                if (!tree->gtOverflow() && !gtIsActiveCSE_Candidate(oper) && (opts.compFlags & CLFLG_TREETRANS) &&
                    optNarrowTree(oper, srcType, dstType, tree->gtVNPair, false))
                {
                    optNarrowTree(oper, srcType, dstType, tree->gtVNPair, true);

                    /* If oper is changed into a cast to TYP_INT, or to a GT_NOP, we may need to discard it */
                    if (oper->gtOper == GT_CAST && oper->CastToType() == genActualType(oper->CastFromType()))
                    {
                        oper = oper->gtCast.CastOp();
                    }
                    goto REMOVE_CAST;
                }
            }
        }

        switch (oper->gtOper)
        {
            /* If the operand is a constant, we'll fold it */
            case GT_CNS_INT:
            case GT_CNS_LNG:
            case GT_CNS_DBL:
            case GT_CNS_STR:
            {
                GenTreePtr oldTree = tree;

                tree = gtFoldExprConst(tree); // This may not fold the constant (NaN ...)

                // Did we get a comma throw as a result of gtFoldExprConst?
                if ((oldTree != tree) && (oldTree->gtOper != GT_COMMA))
                {
                    noway_assert(fgIsCommaThrow(tree));
                    tree->gtOp.gtOp1 = fgMorphTree(tree->gtOp.gtOp1);
                    fgMorphTreeDone(tree);
                    return tree;
                }
                else if (tree->gtOper != GT_CAST)
                {
                    return tree;
                }

                noway_assert(tree->gtCast.CastOp() == oper); // unchanged
            }
            break;

            case GT_CAST:
                /* Check for two consecutive casts into the same dstType */
                if (!tree->gtOverflow())
                {
                    var_types dstType2 = oper->CastToType();
                    if (dstType == dstType2)
                    {
                        goto REMOVE_CAST;
                    }
                }
                break;

            /* If op1 is a mod node, mark it with the GTF_MOD_INT_RESULT flag
               so that the code generator will know not to convert the result
               of the idiv to a regpair */
            case GT_MOD:
                if (dstType == TYP_INT)
                {
                    tree->gtOp.gtOp1->gtFlags |= GTF_MOD_INT_RESULT;
                }

                break;
            case GT_UMOD:
                if (dstType == TYP_UINT)
                {
                    tree->gtOp.gtOp1->gtFlags |= GTF_MOD_INT_RESULT;
                }
                break;

            case GT_COMMA:
                // Check for cast of a GT_COMMA with a throw overflow
                // Bug 110829: Since this optimization will bash the types
                // neither oper or commaOp2 can be CSE candidates
                if (fgIsCommaThrow(oper) && !gtIsActiveCSE_Candidate(oper)) // oper can not be a CSE candidate
                {
                    GenTreePtr commaOp2 = oper->gtOp.gtOp2;

                    if (!gtIsActiveCSE_Candidate(commaOp2)) // commaOp2 can not be a CSE candidate
                    {
                        // need type of oper to be same as tree
                        if (tree->gtType == TYP_LONG)
                        {
                            commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                            commaOp2->gtIntConCommon.SetLngValue(0);
                            /* Change the types of oper and commaOp2 to TYP_LONG */
                            oper->gtType = commaOp2->gtType = TYP_LONG;
                        }
                        else if (varTypeIsFloating(tree->gtType))
                        {
                            commaOp2->ChangeOperConst(GT_CNS_DBL);
                            commaOp2->gtDblCon.gtDconVal = 0.0;
                            // Change the types of oper and commaOp2
                            // X87 promotes everything to TYP_DOUBLE
                            // But other's are a little more precise
                            const var_types newTyp
#if FEATURE_X87_DOUBLES
                                = TYP_DOUBLE;
#else  // FEATURE_X87_DOUBLES
                                = tree->gtType;
#endif // FEATURE_X87_DOUBLES
                            oper->gtType = commaOp2->gtType = newTyp;
                        }
                        else
                        {
                            commaOp2->ChangeOperConst(GT_CNS_INT);
                            commaOp2->gtIntCon.gtIconVal = 0;
                            /* Change the types of oper and commaOp2 to TYP_INT */
                            oper->gtType = commaOp2->gtType = TYP_INT;
                        }
                    }

                    if (vnStore != nullptr)
                    {
                        fgValueNumberTreeConst(commaOp2);
                    }

                    /* Return the GT_COMMA node as the new tree */
                    return oper;
                }
                break;

            default:
                break;
        } /* end switch (oper->gtOper) */
    }

    if (tree->gtOverflow())
    {
        fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW, fgPtrArgCntCur);
    }

    return tree;

REMOVE_CAST:

    /* Here we've eliminated the cast, so just return it's operand */
    assert(!gtIsActiveCSE_Candidate(tree)); // tree cannot be a CSE candidate

    DEBUG_DESTROY_NODE(tree);
    return oper;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Perform an unwrap operation on a Proxy object
 */

GenTreePtr Compiler::fgUnwrapProxy(GenTreePtr objRef)
{
    assert(info.compIsContextful && info.compUnwrapContextful && impIsThis(objRef));

    CORINFO_EE_INFO* pInfo = eeGetEEInfo();
    GenTreePtr       addTree;

    // Perform the unwrap:
    //
    //   This requires two extra indirections.
    //   We mark these indirections as 'invariant' and
    //   the CSE logic will hoist them when appropriate.
    //
    //  Note that each dereference is a GC pointer

    addTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, objRef, gtNewIconNode(pInfo->offsetOfTransparentProxyRP, TYP_I_IMPL));

    objRef = gtNewOperNode(GT_IND, TYP_REF, addTree);
    objRef->gtFlags |= GTF_IND_INVARIANT;

    addTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, objRef, gtNewIconNode(pInfo->offsetOfRealProxyServer, TYP_I_IMPL));

    objRef = gtNewOperNode(GT_IND, TYP_REF, addTree);
    objRef->gtFlags |= GTF_IND_INVARIANT;

    // objRef now hold the 'real this' reference (i.e. the unwrapped proxy)
    return objRef;
}

/*****************************************************************************
 *
 *  Morph an argument list; compute the pointer argument count in the process.
 *
 *  NOTE: This function can be called from any place in the JIT to perform re-morphing
 *  due to graph altering modifications such as copy / constant propagation
 */

unsigned UpdateGT_LISTFlags(GenTreePtr tree)
{
    assert(tree->gtOper == GT_LIST);

    unsigned flags = 0;
    if (tree->gtOp.gtOp2)
    {
        flags |= UpdateGT_LISTFlags(tree->gtOp.gtOp2);
    }

    flags |= (tree->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT);

    tree->gtFlags &= ~GTF_ALL_EFFECT;
    tree->gtFlags |= flags;

    return tree->gtFlags;
}

#ifdef DEBUG
void fgArgTabEntry::Dump()
{
    printf("fgArgTabEntry[arg %u", argNum);
    if (regNum != REG_STK)
    {
        printf(", %s, regs=%u", getRegName(regNum), numRegs);
    }
    if (numSlots > 0)
    {
        printf(", numSlots=%u, slotNum=%u", numSlots, slotNum);
    }
    printf(", align=%u", alignment);
    if (lateArgInx != (unsigned)-1)
    {
        printf(", lateArgInx=%u", lateArgInx);
    }
    if (isSplit)
    {
        printf(", isSplit");
    }
    if (needTmp)
    {
        printf(", tmpNum=V%02u", tmpNum);
    }
    if (needPlace)
    {
        printf(", needPlace");
    }
    if (isTmp)
    {
        printf(", isTmp");
    }
    if (processed)
    {
        printf(", processed");
    }
    if (isHfaRegArg)
    {
        printf(", isHfa");
    }
    if (isBackFilled)
    {
        printf(", isBackFilled");
    }
    if (isNonStandard)
    {
        printf(", isNonStandard");
    }
    printf("]\n");
}
#endif

fgArgInfo::fgArgInfo(Compiler* comp, GenTreePtr call, unsigned numArgs)
{
    compiler = comp;
    callTree = call;
    assert(call->IsCall());
    argCount     = 0; // filled in arg count, starts at zero
    nextSlotNum  = INIT_ARG_STACK_SLOT;
    stkLevel     = 0;
    argTableSize = numArgs; // the allocated table size

    hasRegArgs   = false;
    hasStackArgs = false;
    argsComplete = false;
    argsSorted   = false;

    if (argTableSize == 0)
    {
        argTable = nullptr;
    }
    else
    {
        argTable = new (compiler, CMK_fgArgInfoPtrArr) fgArgTabEntryPtr[argTableSize];
    }
}

/*****************************************************************************
 *
 *  fgArgInfo Copy Constructor
 *
 *  This method needs to act like a copy constructor for fgArgInfo.
 *  The newCall needs to have its fgArgInfo initialized such that
 *  we have newCall that is an exact copy of the oldCall.
 *  We have to take care since the argument information
 *  in the argTable contains pointers that must point to the
 *  new arguments and not the old arguments.
 */
fgArgInfo::fgArgInfo(GenTreePtr newCall, GenTreePtr oldCall)
{
    assert(oldCall->IsCall());
    assert(newCall->IsCall());

    fgArgInfoPtr oldArgInfo = oldCall->gtCall.fgArgInfo;

    compiler = oldArgInfo->compiler;
    ;
    callTree = newCall;
    assert(newCall->IsCall());
    argCount     = 0; // filled in arg count, starts at zero
    nextSlotNum  = INIT_ARG_STACK_SLOT;
    stkLevel     = oldArgInfo->stkLevel;
    argTableSize = oldArgInfo->argTableSize;
    argsComplete = false;
    argTable     = nullptr;
    if (argTableSize > 0)
    {
        argTable = new (compiler, CMK_fgArgInfoPtrArr) fgArgTabEntryPtr[argTableSize];
        for (unsigned inx = 0; inx < argTableSize; inx++)
        {
            argTable[inx] = nullptr;
        }
    }

    assert(oldArgInfo->argsComplete);

    // We create local, artificial GenTreeArgLists that includes the gtCallObjp, if that exists, as first argument,
    // so we can iterate over these argument lists more uniformly.
    // Need to provide a temporary non-null first arguments to these constructors: if we use them, we'll replace them
    GenTreeArgList* newArgs;
    GenTreeArgList  newArgObjp(newCall, newCall->gtCall.gtCallArgs);
    GenTreeArgList* oldArgs;
    GenTreeArgList  oldArgObjp(oldCall, oldCall->gtCall.gtCallArgs);

    if (newCall->gtCall.gtCallObjp == nullptr)
    {
        assert(oldCall->gtCall.gtCallObjp == nullptr);
        newArgs = newCall->gtCall.gtCallArgs;
        oldArgs = oldCall->gtCall.gtCallArgs;
    }
    else
    {
        assert(oldCall->gtCall.gtCallObjp != nullptr);
        newArgObjp.Current() = newCall->gtCall.gtCallArgs;
        newArgs              = &newArgObjp;
        oldArgObjp.Current() = oldCall->gtCall.gtCallObjp;
        oldArgs              = &oldArgObjp;
    }

    GenTreePtr        newCurr;
    GenTreePtr        oldCurr;
    GenTreeArgList*   newParent   = nullptr;
    GenTreeArgList*   oldParent   = nullptr;
    fgArgTabEntryPtr* oldArgTable = oldArgInfo->argTable;
    bool              scanRegArgs = false;

    while (newArgs)
    {
        /* Get hold of the next argument values for the oldCall and newCall */

        newCurr = newArgs->Current();
        oldCurr = oldArgs->Current();
        if (newArgs != &newArgObjp)
        {
            newParent = newArgs;
            oldParent = oldArgs;
        }
        else
        {
            assert(newParent == nullptr && oldParent == nullptr);
        }
        newArgs = newArgs->Rest();
        oldArgs = oldArgs->Rest();

        fgArgTabEntryPtr oldArgTabEntry = nullptr;
        fgArgTabEntryPtr newArgTabEntry = nullptr;

        for (unsigned inx = 0; inx < argTableSize; inx++)
        {
            oldArgTabEntry = oldArgTable[inx];

            if (oldArgTabEntry->parent == oldParent)
            {
                assert((oldParent == nullptr) == (newParent == nullptr));

                // We have found the matching "parent" field in oldArgTabEntry

                newArgTabEntry = new (compiler, CMK_fgArgInfo) fgArgTabEntry;

                // First block copy all fields
                //
                *newArgTabEntry = *oldArgTabEntry;

                // Then update all GenTreePtr fields in the newArgTabEntry
                //
                newArgTabEntry->parent = newParent;

                // The node field is likely to have been updated
                //  to point at a node in the gtCallLateArgs list
                //
                if (oldArgTabEntry->node == oldCurr)
                {
                    // node is not pointing into the gtCallLateArgs list
                    newArgTabEntry->node = newCurr;
                }
                else
                {
                    // node must be pointing into the gtCallLateArgs list
                    //
                    // We will fix this pointer up in the next loop
                    //
                    newArgTabEntry->node = nullptr; // For now we assign a NULL to this field

                    scanRegArgs = true;
                }

                // Now initialize the proper element in the argTable array
                //
                argTable[inx] = newArgTabEntry;
                break;
            }
        }
        // We should have found the matching oldArgTabEntry and created the newArgTabEntry
        //
        assert(newArgTabEntry != nullptr);
    }

    if (scanRegArgs)
    {
        newArgs = newCall->gtCall.gtCallLateArgs;
        oldArgs = oldCall->gtCall.gtCallLateArgs;

        while (newArgs)
        {
            /* Get hold of the next argument values for the oldCall and newCall */

            assert(newArgs->IsList());

            newCurr = newArgs->Current();
            newArgs = newArgs->Rest();

            assert(oldArgs->IsList());

            oldCurr = oldArgs->Current();
            oldArgs = oldArgs->Rest();

            fgArgTabEntryPtr oldArgTabEntry = nullptr;
            fgArgTabEntryPtr newArgTabEntry = nullptr;

            for (unsigned inx = 0; inx < argTableSize; inx++)
            {
                oldArgTabEntry = oldArgTable[inx];

                if (oldArgTabEntry->node == oldCurr)
                {
                    // We have found the matching "node" field in oldArgTabEntry

                    newArgTabEntry = argTable[inx];
                    assert(newArgTabEntry != nullptr);

                    // update the "node" GenTreePtr fields in the newArgTabEntry
                    //
                    assert(newArgTabEntry->node == nullptr); // We previously assigned NULL to this field

                    newArgTabEntry->node = newCurr;
                    break;
                }
            }
        }
    }

    argCount     = oldArgInfo->argCount;
    nextSlotNum  = oldArgInfo->nextSlotNum;
    argsComplete = true;
    argsSorted   = true;
}

void fgArgInfo::AddArg(fgArgTabEntryPtr curArgTabEntry)
{
    assert(argCount < argTableSize);
    argTable[argCount] = curArgTabEntry;
    argCount++;
}

fgArgTabEntryPtr fgArgInfo::AddRegArg(
    unsigned argNum, GenTreePtr node, GenTreePtr parent, regNumber regNum, unsigned numRegs, unsigned alignment)
{
    fgArgTabEntryPtr curArgTabEntry = new (compiler, CMK_fgArgInfo) fgArgTabEntry;

    curArgTabEntry->argNum        = argNum;
    curArgTabEntry->node          = node;
    curArgTabEntry->parent        = parent;
    curArgTabEntry->regNum        = regNum;
    curArgTabEntry->slotNum       = 0;
    curArgTabEntry->numRegs       = numRegs;
    curArgTabEntry->numSlots      = 0;
    curArgTabEntry->alignment     = alignment;
    curArgTabEntry->lateArgInx    = (unsigned)-1;
    curArgTabEntry->tmpNum        = (unsigned)-1;
    curArgTabEntry->isSplit       = false;
    curArgTabEntry->isTmp         = false;
    curArgTabEntry->needTmp       = false;
    curArgTabEntry->needPlace     = false;
    curArgTabEntry->processed     = false;
    curArgTabEntry->isHfaRegArg   = false;
    curArgTabEntry->isBackFilled  = false;
    curArgTabEntry->isNonStandard = false;

    hasRegArgs = true;
    AddArg(curArgTabEntry);
    return curArgTabEntry;
}

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
fgArgTabEntryPtr fgArgInfo::AddRegArg(unsigned                                                         argNum,
                                      GenTreePtr                                                       node,
                                      GenTreePtr                                                       parent,
                                      regNumber                                                        regNum,
                                      unsigned                                                         numRegs,
                                      unsigned                                                         alignment,
                                      const bool                                                       isStruct,
                                      const regNumber                                                  otherRegNum,
                                      const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* const structDescPtr)
{
    fgArgTabEntryPtr curArgTabEntry = AddRegArg(argNum, node, parent, regNum, numRegs, alignment);
    assert(curArgTabEntry != nullptr);

    // The node of the ArgTabEntry could change after remorphing - it could be rewritten to a cpyblk or a
    // PlaceHolder node (in case of needed late argument, for example.)
    // This requires using of an extra flag. At creation time the state is right, so
    // and this assert enforces that.
    assert((varTypeIsStruct(node) && isStruct) || (!varTypeIsStruct(node) && !isStruct));
    curArgTabEntry->otherRegNum = otherRegNum; // Second reg for the struct
    curArgTabEntry->isStruct    = isStruct;    // is this a struct arg

    if (isStruct && structDescPtr != nullptr)
    {
        curArgTabEntry->structDesc.CopyFrom(*structDescPtr);
    }

    return curArgTabEntry;
}
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

fgArgTabEntryPtr fgArgInfo::AddStkArg(unsigned   argNum,
                                      GenTreePtr node,
                                      GenTreePtr parent,
                                      unsigned   numSlots,
                                      unsigned   alignment
                                          FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(const bool isStruct))
{
    fgArgTabEntryPtr curArgTabEntry = new (compiler, CMK_fgArgInfo) fgArgTabEntry;

    nextSlotNum = (unsigned)roundUp(nextSlotNum, alignment);

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    // The node of the ArgTabEntry could change after remorphing - it could be rewritten to a cpyblk or a
    // PlaceHolder node (in case of needed late argument, for example.)
    // This reqires using of an extra flag. At creation time the state is right, so
    // and this assert enforces that.
    assert((varTypeIsStruct(node) && isStruct) || (!varTypeIsStruct(node) && !isStruct));
    curArgTabEntry->isStruct = isStruct; // is this a struct arg
#endif                                   // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    curArgTabEntry->argNum        = argNum;
    curArgTabEntry->node          = node;
    curArgTabEntry->parent        = parent;
    curArgTabEntry->regNum        = REG_STK;
    curArgTabEntry->slotNum       = nextSlotNum;
    curArgTabEntry->numRegs       = 0;
    curArgTabEntry->numSlots      = numSlots;
    curArgTabEntry->alignment     = alignment;
    curArgTabEntry->lateArgInx    = (unsigned)-1;
    curArgTabEntry->tmpNum        = (unsigned)-1;
    curArgTabEntry->isSplit       = false;
    curArgTabEntry->isTmp         = false;
    curArgTabEntry->needTmp       = false;
    curArgTabEntry->needPlace     = false;
    curArgTabEntry->processed     = false;
    curArgTabEntry->isHfaRegArg   = false;
    curArgTabEntry->isBackFilled  = false;
    curArgTabEntry->isNonStandard = false;

    hasStackArgs = true;
    AddArg(curArgTabEntry);

    nextSlotNum += numSlots;
    return curArgTabEntry;
}

void fgArgInfo::RemorphReset()
{
    nextSlotNum = INIT_ARG_STACK_SLOT;
}

fgArgTabEntry* fgArgInfo::RemorphRegArg(
    unsigned argNum, GenTreePtr node, GenTreePtr parent, regNumber regNum, unsigned numRegs, unsigned alignment)
{
    fgArgTabEntryPtr curArgTabEntry = nullptr;
    unsigned         regArgInx      = 0;
    unsigned         inx;

    for (inx = 0; inx < argCount; inx++)
    {
        curArgTabEntry = argTable[inx];
        if (curArgTabEntry->argNum == argNum)
        {
            break;
        }

        bool       isRegArg;
        GenTreePtr argx;
        if (curArgTabEntry->parent != nullptr)
        {
            assert(curArgTabEntry->parent->IsList());
            argx     = curArgTabEntry->parent->Current();
            isRegArg = (argx->gtFlags & GTF_LATE_ARG) != 0;
        }
        else
        {
            argx     = curArgTabEntry->node;
            isRegArg = true;
        }

        if (isRegArg)
        {
            regArgInx++;
        }
    }
    // if this was a nonstandard arg the table is definitive
    if (curArgTabEntry->isNonStandard)
    {
        regNum = curArgTabEntry->regNum;
    }

    assert(curArgTabEntry->argNum == argNum);
    assert(curArgTabEntry->regNum == regNum);
    assert(curArgTabEntry->alignment == alignment);
    assert(curArgTabEntry->parent == parent);

    if (curArgTabEntry->node != node)
    {
        GenTreePtr argx     = nullptr;
        unsigned   regIndex = 0;

        /* process the register argument list */
        for (GenTreeArgList* list = callTree->gtCall.gtCallLateArgs; list; (regIndex++, list = list->Rest()))
        {
            argx = list->Current();
            assert(!argx->IsArgPlaceHolderNode()); // No place holders nodes are in gtCallLateArgs;
            if (regIndex == regArgInx)
            {
                break;
            }
        }
        assert(regIndex == regArgInx);
        assert(regArgInx == curArgTabEntry->lateArgInx);

        if (curArgTabEntry->node != argx)
        {
            curArgTabEntry->node = argx;
        }
    }
    return curArgTabEntry;
}

void fgArgInfo::RemorphStkArg(
    unsigned argNum, GenTreePtr node, GenTreePtr parent, unsigned numSlots, unsigned alignment)
{
    fgArgTabEntryPtr curArgTabEntry = nullptr;
    bool             isRegArg       = false;
    unsigned         regArgInx      = 0;
    GenTreePtr       argx;
    unsigned         inx;

    for (inx = 0; inx < argCount; inx++)
    {
        curArgTabEntry = argTable[inx];

        if (curArgTabEntry->parent != nullptr)
        {
            assert(curArgTabEntry->parent->IsList());
            argx     = curArgTabEntry->parent->Current();
            isRegArg = (argx->gtFlags & GTF_LATE_ARG) != 0;
        }
        else
        {
            argx     = curArgTabEntry->node;
            isRegArg = true;
        }

        if (curArgTabEntry->argNum == argNum)
        {
            break;
        }

        if (isRegArg)
        {
            regArgInx++;
        }
    }

    nextSlotNum = (unsigned)roundUp(nextSlotNum, alignment);

    assert(curArgTabEntry->argNum == argNum);
    assert(curArgTabEntry->slotNum == nextSlotNum);
    assert(curArgTabEntry->numSlots == numSlots);
    assert(curArgTabEntry->alignment == alignment);
    assert(curArgTabEntry->parent == parent);
    assert(parent->IsList());

#if FEATURE_FIXED_OUT_ARGS
    if (curArgTabEntry->node != node)
    {
        if (isRegArg)
        {
            GenTreePtr argx     = nullptr;
            unsigned   regIndex = 0;

            /* process the register argument list */
            for (GenTreeArgList *list = callTree->gtCall.gtCallLateArgs; list; list = list->Rest(), regIndex++)
            {
                argx = list->Current();
                assert(!argx->IsArgPlaceHolderNode()); // No place holders nodes are in gtCallLateArgs;
                if (regIndex == regArgInx)
                {
                    break;
                }
            }
            assert(regIndex == regArgInx);
            assert(regArgInx == curArgTabEntry->lateArgInx);

            if (curArgTabEntry->node != argx)
            {
                curArgTabEntry->node = argx;
            }
        }
        else
        {
            assert(parent->Current() == node);
            curArgTabEntry->node = node;
        }
    }
#else
    curArgTabEntry->node = node;
#endif

    nextSlotNum += numSlots;
}

void fgArgInfo::SplitArg(unsigned argNum, unsigned numRegs, unsigned numSlots)
{
    fgArgTabEntryPtr curArgTabEntry = nullptr;
    assert(argNum < argCount);
    for (unsigned inx = 0; inx < argCount; inx++)
    {
        curArgTabEntry = argTable[inx];
        if (curArgTabEntry->argNum == argNum)
        {
            break;
        }
    }

    assert(numRegs > 0);
    assert(numSlots > 0);

    curArgTabEntry->isSplit  = true;
    curArgTabEntry->numRegs  = numRegs;
    curArgTabEntry->numSlots = numSlots;

    nextSlotNum += numSlots;
}

void fgArgInfo::EvalToTmp(unsigned argNum, unsigned tmpNum, GenTreePtr newNode)
{
    fgArgTabEntryPtr curArgTabEntry = nullptr;
    assert(argNum < argCount);
    for (unsigned inx = 0; inx < argCount; inx++)
    {
        curArgTabEntry = argTable[inx];
        if (curArgTabEntry->argNum == argNum)
        {
            break;
        }
    }
    assert(curArgTabEntry->parent->Current() == newNode);

    curArgTabEntry->node   = newNode;
    curArgTabEntry->tmpNum = tmpNum;
    curArgTabEntry->isTmp  = true;
}

void fgArgInfo::ArgsComplete()
{
    bool hasStackArgs    = false;
    bool hasStructRegArg = false;

    for (unsigned curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntryPtr curArgTabEntry = argTable[curInx];
        assert(curArgTabEntry != nullptr);
        GenTreePtr argx = curArgTabEntry->node;

        if (curArgTabEntry->regNum == REG_STK)
        {
            hasStackArgs = true;
#if !FEATURE_FIXED_OUT_ARGS
            // On x86 we use push instructions to pass arguments:
            //   The non-register arguments are evaluated and pushed in order
            //   and they are never evaluated into temps
            //
            continue;
#endif
        }
        else // we have a register argument, next we look for a struct type.
        {
            if (varTypeIsStruct(argx) FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY(|| curArgTabEntry->isStruct))
            {
                hasStructRegArg = true;
            }
        }

        /* If the argument tree contains an assignment (GTF_ASG) then the argument and
           and every earlier argument (except constants) must be evaluated into temps
           since there may be other arguments that follow and they may use the value being assigned.

           EXAMPLE: ArgTab is "a, a=5, a"
                    -> when we see the second arg "a=5"
                       we know the first two arguments "a, a=5" have to be evaluated into temps

           For the case of an assignment, we only know that there exist some assignment someplace
           in the tree.  We don't know what is being assigned so we are very conservative here
           and assume that any local variable could have been assigned.
         */

        if (argx->gtFlags & GTF_ASG)
        {
            // If this is not the only argument, or it's a copyblk, or it already evaluates the expression to
            // a tmp, then we need a temp in the late arg list.
            if ((argCount > 1) || argx->OperIsCopyBlkOp()
#ifdef FEATURE_FIXED_OUT_ARGS
                || curArgTabEntry->isTmp // I protect this by "FEATURE_FIXED_OUT_ARGS" to preserve the property
                                         // that we only have late non-register args when that feature is on.
#endif                                   // FEATURE_FIXED_OUT_ARGS
                )
            {
                curArgTabEntry->needTmp = true;
            }

            // For all previous arguments, unless they are a simple constant
            //  we require that they be evaluated into temps
            for (unsigned prevInx = 0; prevInx < curInx; prevInx++)
            {
                fgArgTabEntryPtr prevArgTabEntry = argTable[prevInx];
                assert(prevArgTabEntry->argNum < curArgTabEntry->argNum);

                assert(prevArgTabEntry->node);
                if (prevArgTabEntry->node->gtOper != GT_CNS_INT)
                {
                    prevArgTabEntry->needTmp = true;
                }
            }
        }

#if FEATURE_FIXED_OUT_ARGS
        // Like calls, if this argument has a tree that will do an inline throw,
        // a call to a jit helper, then we need to treat it like a call (but only
        // if there are/were any stack args).
        // This means unnesting, sorting, etc.  Technically this is overly
        // conservative, but I want to avoid as much special-case debug-only code
        // as possible, so leveraging the GTF_CALL flag is the easiest.
        if (!(argx->gtFlags & GTF_CALL) && (argx->gtFlags & GTF_EXCEPT) && (argCount > 1) &&
            compiler->opts.compDbgCode &&
            (compiler->fgWalkTreePre(&argx, Compiler::fgChkThrowCB) == Compiler::WALK_ABORT))
        {
            for (unsigned otherInx = 0; otherInx < argCount; otherInx++)
            {
                if (otherInx == curInx)
                {
                    continue;
                }

                if (argTable[otherInx]->regNum == REG_STK)
                {
                    argx->gtFlags |= GTF_CALL;
                    break;
                }
            }
        }
#endif // FEATURE_FIXED_OUT_ARGS

        /* If it contains a call (GTF_CALL) then itself and everything before the call
           with a GLOB_EFFECT must eval to temp (this is because everything with SIDE_EFFECT
           has to be kept in the right order since we will move the call to the first position)

           For calls we don't have to be quite as conservative as we are with an assignment
           since the call won't be modifying any non-address taken LclVars.
         */

        if (argx->gtFlags & GTF_CALL)
        {
            if (argCount > 1) // If this is not the only argument
            {
                curArgTabEntry->needTmp = true;
            }
            else if (varTypeIsFloating(argx->TypeGet()) && (argx->OperGet() == GT_CALL))
            {
                // Spill all arguments that are floating point calls
                curArgTabEntry->needTmp = true;
            }

            // All previous arguments may need to be evaluated into temps
            for (unsigned prevInx = 0; prevInx < curInx; prevInx++)
            {
                fgArgTabEntryPtr prevArgTabEntry = argTable[prevInx];
                assert(prevArgTabEntry->argNum < curArgTabEntry->argNum);
                assert(prevArgTabEntry->node);

                // For all previous arguments, if they have any GTF_ALL_EFFECT
                //  we require that they be evaluated into a temp
                if ((prevArgTabEntry->node->gtFlags & GTF_ALL_EFFECT) != 0)
                {
                    prevArgTabEntry->needTmp = true;
                }
#if FEATURE_FIXED_OUT_ARGS
                // Or, if they are stored into the FIXED_OUT_ARG area
                // we require that they be moved to the gtCallLateArgs
                // and replaced with a placeholder node
                else if (prevArgTabEntry->regNum == REG_STK)
                {
                    prevArgTabEntry->needPlace = true;
                }
#endif
            }
        }

#ifndef LEGACY_BACKEND
#if FEATURE_MULTIREG_ARGS
        // For RyuJIT backend we will expand a Multireg arg into a GT_LIST
        // with multiple indirections, so here we consider spilling it into a tmp LclVar.
        //
        // Note that Arm32 is a LEGACY_BACKEND and it defines FEATURE_MULTIREG_ARGS
        // so we skip this for ARM32 until it is ported to use RyuJIT backend
        //

        bool isMultiRegArg = (curArgTabEntry->numRegs > 1);

        if ((argx->TypeGet() == TYP_STRUCT) && (curArgTabEntry->needTmp == false))
        {
            if (isMultiRegArg && ((argx->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
            {
                // Spill multireg struct arguments that have Assignments or Calls embedded in them
                curArgTabEntry->needTmp = true;
            }
            else
            {
                // We call gtPrepareCost to measure the cost of evaluating this tree
                compiler->gtPrepareCost(argx);

                if (isMultiRegArg && (argx->gtCostEx > (6 * IND_COST_EX)))
                {
                    // Spill multireg struct arguments that are expensive to evaluate twice
                    curArgTabEntry->needTmp = true;
                }
                else if (argx->OperGet() == GT_OBJ)
                {
                    GenTreeObj*          argObj     = argx->AsObj();
                    CORINFO_CLASS_HANDLE objClass   = argObj->gtClass;
                    unsigned             structSize = compiler->info.compCompHnd->getClassSize(objClass);
                    switch (structSize)
                    {
                        case 3:
                        case 5:
                        case 6:
                        case 7:
                            // If we have a stack based LclVar we can perform a wider read of 4 or 8 bytes
                            //
                            if (argObj->gtObj.gtOp1->IsVarAddr() == false) // Is the source not a LclVar?
                            {
                                // If we don't have a LclVar we need to read exactly 3,5,6 or 7 bytes
                                // For now we use a a GT_CPBLK to copy the exact size into a GT_LCL_VAR temp.
                                //
                                curArgTabEntry->needTmp = true;
                            }
                            break;

                        case 11:
                        case 13:
                        case 14:
                        case 15:
                            // Spill any GT_OBJ multireg structs that are difficult to extract
                            //
                            // When we have a GT_OBJ of a struct with the above sizes we would need
                            // to use 3 or 4 load instructions to load the exact size of this struct.
                            // Instead we spill the GT_OBJ into a new GT_LCL_VAR temp and this sequence
                            // will use a GT_CPBLK to copy the exact size into the GT_LCL_VAR temp.
                            // Then we can just load all 16 bytes of the GT_LCL_VAR temp when passing
                            // the argument.
                            //
                            curArgTabEntry->needTmp = true;
                            break;

                        default:
                            break;
                    }
                }
            }
        }
#endif // FEATURE_MULTIREG_ARGS
#endif // LEGACY_BACKEND
    }

    // We only care because we can't spill structs and qmarks involve a lot of spilling, but
    // if we don't have qmarks, then it doesn't matter.
    // So check for Qmark's globally once here, instead of inside the loop.
    //
    const bool hasStructRegArgWeCareAbout = (hasStructRegArg && compiler->compQmarkUsed);

#if FEATURE_FIXED_OUT_ARGS

    // For Arm/x64 we only care because we can't reorder a register
    // argument that uses GT_LCLHEAP.  This is an optimization to
    // save a check inside the below loop.
    //
    const bool hasStackArgsWeCareAbout = (hasStackArgs && compiler->compLocallocUsed);

#else

    const bool hasStackArgsWeCareAbout = hasStackArgs;

#endif // FEATURE_FIXED_OUT_ARGS

    // If we have any stack args we have to force the evaluation
    // of any arguments passed in registers that might throw an exception
    //
    // Technically we only a required to handle the following two cases:
    //     a GT_IND with GTF_IND_RNGCHK (only on x86) or
    //     a GT_LCLHEAP node that allocates stuff on the stack
    //
    if (hasStackArgsWeCareAbout || hasStructRegArgWeCareAbout)
    {
        for (unsigned curInx = 0; curInx < argCount; curInx++)
        {
            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];
            assert(curArgTabEntry != nullptr);
            GenTreePtr argx = curArgTabEntry->node;

            // Examine the register args that are currently not marked needTmp
            //
            if (!curArgTabEntry->needTmp && (curArgTabEntry->regNum != REG_STK))
            {
                if (hasStackArgsWeCareAbout)
                {
#if !FEATURE_FIXED_OUT_ARGS
                    // On x86 we previously recorded a stack depth of zero when
                    // morphing the register arguments of any GT_IND with a GTF_IND_RNGCHK flag
                    // Thus we can not reorder the argument after any stack based argument
                    // (Note that GT_LCLHEAP sets the GTF_EXCEPT flag so we don't need to
                    // check for it explicitly
                    //
                    if (argx->gtFlags & GTF_EXCEPT)
                    {
                        curArgTabEntry->needTmp = true;
                        continue;
                    }
#else
                    // For Arm/X64 we can't reorder a register argument that uses a GT_LCLHEAP
                    //
                    if (argx->gtFlags & GTF_EXCEPT)
                    {
                        assert(compiler->compLocallocUsed);

                        // Returns WALK_ABORT if a GT_LCLHEAP node is encountered in the argx tree
                        //
                        if (compiler->fgWalkTreePre(&argx, Compiler::fgChkLocAllocCB) == Compiler::WALK_ABORT)
                        {
                            curArgTabEntry->needTmp = true;
                            continue;
                        }
                    }
#endif
                }
                if (hasStructRegArgWeCareAbout)
                {
                    // Returns true if a GT_QMARK node is encountered in the argx tree
                    //
                    if (compiler->fgWalkTreePre(&argx, Compiler::fgChkQmarkCB) == Compiler::WALK_ABORT)
                    {
                        curArgTabEntry->needTmp = true;
                        continue;
                    }
                }
            }
        }
    }

    argsComplete = true;
}

void fgArgInfo::SortArgs()
{
    assert(argsComplete == true);

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\nSorting the arguments:\n");
    }
#endif

    /* Shuffle the arguments around before we build the gtCallLateArgs list.
       The idea is to move all "simple" arguments like constants and local vars
       to the end of the table, and move the complex arguments towards the beginning
       of the table. This will help prevent registers from being spilled by
       allowing us to evaluate the more complex arguments before the simpler arguments.
       The argTable ends up looking like:
           +------------------------------------+  <--- argTable[argCount - 1]
           |          constants                 |
           +------------------------------------+
           |    local var / local field         |
           +------------------------------------+
           | remaining arguments sorted by cost |
           +------------------------------------+
           | temps (argTable[].needTmp = true)  |
           +------------------------------------+
           |  args with calls (GTF_CALL)        |
           +------------------------------------+  <--- argTable[0]
     */

    /* Set the beginning and end for the new argument table */
    unsigned curInx;
    int      regCount      = 0;
    unsigned begTab        = 0;
    unsigned endTab        = argCount - 1;
    unsigned argsRemaining = argCount;

    // First take care of arguments that are constants.
    // [We use a backward iterator pattern]
    //
    curInx = argCount;
    do
    {
        curInx--;

        fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

        if (curArgTabEntry->regNum != REG_STK)
        {
            regCount++;
        }

        // Skip any already processed args
        //
        if (!curArgTabEntry->processed)
        {
            GenTreePtr argx = curArgTabEntry->node;

            // put constants at the end of the table
            //
            if (argx->gtOper == GT_CNS_INT)
            {
                noway_assert(curInx <= endTab);

                curArgTabEntry->processed = true;

                // place curArgTabEntry at the endTab position by performing a swap
                //
                if (curInx != endTab)
                {
                    argTable[curInx] = argTable[endTab];
                    argTable[endTab] = curArgTabEntry;
                }

                endTab--;
                argsRemaining--;
            }
        }
    } while (curInx > 0);

    if (argsRemaining > 0)
    {
        // Next take care of arguments that are calls.
        // [We use a forward iterator pattern]
        //
        for (curInx = begTab; curInx <= endTab; curInx++)
        {
            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTreePtr argx = curArgTabEntry->node;

                // put calls at the beginning of the table
                //
                if (argx->gtFlags & GTF_CALL)
                {
                    curArgTabEntry->processed = true;

                    // place curArgTabEntry at the begTab position by performing a swap
                    //
                    if (curInx != begTab)
                    {
                        argTable[curInx] = argTable[begTab];
                        argTable[begTab] = curArgTabEntry;
                    }

                    begTab++;
                    argsRemaining--;
                }
            }
        }
    }

    if (argsRemaining > 0)
    {
        // Next take care arguments that are temps.
        // These temps come before the arguments that are
        // ordinary local vars or local fields
        // since this will give them a better chance to become
        // enregistered into their actual argument register.
        // [We use a forward iterator pattern]
        //
        for (curInx = begTab; curInx <= endTab; curInx++)
        {
            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                if (curArgTabEntry->needTmp)
                {
                    curArgTabEntry->processed = true;

                    // place curArgTabEntry at the begTab position by performing a swap
                    //
                    if (curInx != begTab)
                    {
                        argTable[curInx] = argTable[begTab];
                        argTable[begTab] = curArgTabEntry;
                    }

                    begTab++;
                    argsRemaining--;
                }
            }
        }
    }

    if (argsRemaining > 0)
    {
        // Next take care of local var and local field arguments.
        // These are moved towards the end of the argument evaluation.
        // [We use a backward iterator pattern]
        //
        curInx = endTab + 1;
        do
        {
            curInx--;

            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTreePtr argx = curArgTabEntry->node;

                if ((argx->gtOper == GT_LCL_VAR) || (argx->gtOper == GT_LCL_FLD))
                {
                    noway_assert(curInx <= endTab);

                    curArgTabEntry->processed = true;

                    // place curArgTabEntry at the endTab position by performing a swap
                    //
                    if (curInx != endTab)
                    {
                        argTable[curInx] = argTable[endTab];
                        argTable[endTab] = curArgTabEntry;
                    }

                    endTab--;
                    argsRemaining--;
                }
            }
        } while (curInx > begTab);
    }

    // Finally, take care of all the remaining arguments.
    // Note that we fill in one arg at a time using a while loop.
    bool costsPrepared = false; // Only prepare tree costs once, the first time through this loop
    while (argsRemaining > 0)
    {
        /* Find the most expensive arg remaining and evaluate it next */

        fgArgTabEntryPtr expensiveArgTabEntry = nullptr;
        unsigned         expensiveArg         = UINT_MAX;
        unsigned         expensiveArgCost     = 0;

        // [We use a forward iterator pattern]
        //
        for (curInx = begTab; curInx <= endTab; curInx++)
        {
            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTreePtr argx = curArgTabEntry->node;

                // We should have already handled these kinds of args
                assert(argx->gtOper != GT_LCL_VAR);
                assert(argx->gtOper != GT_LCL_FLD);
                assert(argx->gtOper != GT_CNS_INT);

                // This arg should either have no persistent side effects or be the last one in our table
                // assert(((argx->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0) || (curInx == (argCount-1)));

                if (argsRemaining == 1)
                {
                    // This is the last arg to place
                    expensiveArg         = curInx;
                    expensiveArgTabEntry = curArgTabEntry;
                    assert(begTab == endTab);
                    break;
                }
                else
                {
                    if (!costsPrepared)
                    {
                        /* We call gtPrepareCost to measure the cost of evaluating this tree */
                        compiler->gtPrepareCost(argx);
                    }

                    if (argx->gtCostEx > expensiveArgCost)
                    {
                        // Remember this arg as the most expensive one that we have yet seen
                        expensiveArgCost     = argx->gtCostEx;
                        expensiveArg         = curInx;
                        expensiveArgTabEntry = curArgTabEntry;
                    }
                }
            }
        }

        noway_assert(expensiveArg != UINT_MAX);

        // put the most expensive arg towards the beginning of the table

        expensiveArgTabEntry->processed = true;

        // place expensiveArgTabEntry at the begTab position by performing a swap
        //
        if (expensiveArg != begTab)
        {
            argTable[expensiveArg] = argTable[begTab];
            argTable[begTab]       = expensiveArgTabEntry;
        }

        begTab++;
        argsRemaining--;

        costsPrepared = true; // If we have more expensive arguments, don't re-evaluate the tree cost on the next loop
    }

    // The table should now be completely filled and thus begTab should now be adjacent to endTab
    // and regArgsRemaining should be zero
    assert(begTab == (endTab + 1));
    assert(argsRemaining == 0);

#if !FEATURE_FIXED_OUT_ARGS
    // Finally build the regArgList
    //
    callTree->gtCall.regArgList      = NULL;
    callTree->gtCall.regArgListCount = regCount;

    unsigned regInx = 0;
    for (curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

        if (curArgTabEntry->regNum != REG_STK)
        {
            // Encode the argument register in the register mask
            //
            callTree->gtCall.regArgList[regInx] = curArgTabEntry->regNum;
            regInx++;
        }
    }
#endif // !FEATURE_FIXED_OUT_ARGS

    argsSorted = true;
}

//------------------------------------------------------------------------------
// fgMakeTmpArgNode : This function creates a tmp var only if needed.
//                    We need this to be done in order to enforce ordering
//                    of the evaluation of arguments.
//
// Arguments:
//    tmpVarNum  - the var num which we clone into the newly created temp var.
//
// Return Value:
//    the newly created temp var tree.

GenTreePtr Compiler::fgMakeTmpArgNode(
    unsigned tmpVarNum FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(const bool passedInRegisters))
{
    LclVarDsc* varDsc = &lvaTable[tmpVarNum];
    assert(varDsc->lvIsTemp);
    var_types type = varDsc->TypeGet();

    // Create a copy of the temp to go into the late argument list
    GenTreePtr arg      = gtNewLclvNode(tmpVarNum, type);
    GenTreePtr addrNode = nullptr;

    if (varTypeIsStruct(type))
    {

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

        arg->gtFlags |= GTF_DONT_CSE;

#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING 
        // Can this type be passed in a single register?
        // If so, the following call will return the corresponding primitive type.
        // Otherwise, it will return TYP_UNKNOWN and we will pass by reference.

        bool passedInRegisters = false;
        structPassingKind kind;
        CORINFO_CLASS_HANDLE clsHnd = varDsc->lvVerTypeInfo.GetClassHandle();
        var_types structBaseType = getPrimitiveTypeForStruct(lvaLclExactSize(tmpVarNum), clsHnd);

        if (structBaseType != TYP_UNKNOWN)
        {
            passedInRegisters = true;
            type = structBaseType;
        }
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

        // If it is passed in registers, don't get the address of the var. Make it a
        // field instead. It will be loaded in registers with putarg_reg tree in lower.
        if (passedInRegisters)
        {
            arg->ChangeOper(GT_LCL_FLD);
            arg->gtType = type;
        }
        else
        {
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            // TODO-Cleanup: Fix this - we should never have an address that is TYP_STRUCT.
            var_types addrType = type;
#else
            var_types addrType = TYP_BYREF;
#endif
            arg      = gtNewOperNode(GT_ADDR, addrType, arg);
            addrNode = arg;

#if FEATURE_MULTIREG_ARGS
#ifdef _TARGET_ARM64_
            assert(varTypeIsStruct(type));
            if (lvaIsMultiregStruct(varDsc))
            {
                // ToDo-ARM64: Consider using:  arg->ChangeOper(GT_LCL_FLD);
                // as that is how FEATURE_UNIX_AMD64_STRUCT_PASSING works.
                // We will create a GT_OBJ for the argument below.
                // This will be passed by value in two registers.
                assert(addrNode != nullptr);

                // Create an Obj of the temp to use it as a call argument.
                arg = gtNewObjNode(lvaGetStruct(tmpVarNum), arg);
            }
#endif // _TARGET_ARM64_
#endif // FEATURE_MULTIREG_ARGS
        }

#else // not (_TARGET_AMD64_ or _TARGET_ARM64_)

        // other targets, we pass the struct by value
        assert(varTypeIsStruct(type));

        addrNode = gtNewOperNode(GT_ADDR, TYP_BYREF, arg);

        // Get a new Obj node temp to use it as a call argument.
        // gtNewObjNode will set the GTF_EXCEPT flag if this is not a local stack object.
        arg = gtNewObjNode(lvaGetStruct(tmpVarNum), addrNode);

#endif // not (_TARGET_AMD64_ or _TARGET_ARM64_)

    } // (varTypeIsStruct(type))

    if (addrNode != nullptr)
    {
        assert(addrNode->gtOper == GT_ADDR);

        // This will prevent this LclVar from being optimized away
        lvaSetVarAddrExposed(tmpVarNum);

        // the child of a GT_ADDR is required to have this flag set
        addrNode->gtOp.gtOp1->gtFlags |= GTF_DONT_CSE;
    }

    return arg;
}

void fgArgInfo::EvalArgsToTemps()
{
    assert(argsSorted == true);

    unsigned regArgInx = 0;
    // Now go through the argument table and perform the necessary evaluation into temps
    GenTreeArgList* tmpRegArgNext = nullptr;
    for (unsigned curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

        GenTreePtr argx     = curArgTabEntry->node;
        GenTreePtr setupArg = nullptr;
        GenTreePtr defArg;

#if !FEATURE_FIXED_OUT_ARGS
        // Only ever set for FEATURE_FIXED_OUT_ARGS
        assert(curArgTabEntry->needPlace == false);

        // On x86 and other archs that use push instructions to pass arguments:
        //   Only the register arguments need to be replaced with placeholder nodes.
        //   Stacked arguments are evaluated and pushed (or stored into the stack) in order.
        //
        if (curArgTabEntry->regNum == REG_STK)
            continue;
#endif

        if (curArgTabEntry->needTmp)
        {
            unsigned tmpVarNum;

            if (curArgTabEntry->isTmp == true)
            {
                // Create a copy of the temp to go into the late argument list
                tmpVarNum = curArgTabEntry->tmpNum;
                defArg    = compiler->fgMakeTmpArgNode(tmpVarNum FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(
                    argTable[curInx]->structDesc.passedInRegisters));

                // mark the original node as a late argument
                argx->gtFlags |= GTF_LATE_ARG;
            }
            else
            {
                // Create a temp assignment for the argument
                // Put the temp in the gtCallLateArgs list
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("Argument with 'side effect'...\n");
                    compiler->gtDispTree(argx);
                }
#endif

#if defined(_TARGET_AMD64_) && !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                noway_assert(argx->gtType != TYP_STRUCT);
#endif

                tmpVarNum = compiler->lvaGrabTemp(true DEBUGARG("argument with side effect"));
                if (argx->gtOper == GT_MKREFANY)
                {
                    // For GT_MKREFANY, typically the actual struct copying does
                    // not have any side-effects and can be delayed. So instead
                    // of using a temp for the whole struct, we can just use a temp
                    // for operand that that has a side-effect
                    GenTreePtr operand;
                    if ((argx->gtOp.gtOp2->gtFlags & GTF_ALL_EFFECT) == 0)
                    {
                        operand = argx->gtOp.gtOp1;

                        // In the early argument evaluation, place an assignment to the temp
                        // from the source operand of the mkrefany
                        setupArg = compiler->gtNewTempAssign(tmpVarNum, operand);

                        // Replace the operand for the mkrefany with the new temp.
                        argx->gtOp.gtOp1 = compiler->gtNewLclvNode(tmpVarNum, operand->TypeGet());
                    }
                    else if ((argx->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT) == 0)
                    {
                        operand = argx->gtOp.gtOp2;

                        // In the early argument evaluation, place an assignment to the temp
                        // from the source operand of the mkrefany
                        setupArg = compiler->gtNewTempAssign(tmpVarNum, operand);

                        // Replace the operand for the mkrefany with the new temp.
                        argx->gtOp.gtOp2 = compiler->gtNewLclvNode(tmpVarNum, operand->TypeGet());
                    }
                }

                if (setupArg != nullptr)
                {
                    // Now keep the mkrefany for the late argument list
                    defArg = argx;

                    // Clear the side-effect flags because now both op1 and op2 have no side-effects
                    defArg->gtFlags &= ~GTF_ALL_EFFECT;
                }
                else
                {
                    setupArg = compiler->gtNewTempAssign(tmpVarNum, argx);

                    LclVarDsc* varDsc = compiler->lvaTable + tmpVarNum;

#ifndef LEGACY_BACKEND
                    if (compiler->fgOrder == Compiler::FGOrderLinear)
                    {
                        // We'll reference this temporary variable just once
                        // when we perform the function call after
                        // setting up this argument.
                        varDsc->lvRefCnt = 1;
                    }
#endif // !LEGACY_BACKEND

                    var_types lclVarType = genActualType(argx->gtType);
                    var_types scalarType = TYP_UNKNOWN;

                    if (setupArg->OperIsCopyBlkOp())
                    {
                        setupArg = compiler->fgMorphCopyBlock(setupArg);
#ifdef _TARGET_ARM64_
                        // This scalar LclVar widening step is only performed for ARM64
                        //
                        CORINFO_CLASS_HANDLE clsHnd     = compiler->lvaGetStruct(tmpVarNum);
                        unsigned             structSize = varDsc->lvExactSize;

                        scalarType = compiler->getPrimitiveTypeForStruct(structSize, clsHnd);
#endif // _TARGET_ARM64_
                    }

                    // scalarType can be set to a wider type for ARM64: (3 => 4)  or (5,6,7 => 8)
                    if ((scalarType != TYP_UNKNOWN) && (scalarType != lclVarType))
                    {
                        // Create a GT_LCL_FLD using the wider type to go to the late argument list
                        defArg = compiler->gtNewLclFldNode(tmpVarNum, scalarType, 0);
                    }
                    else
                    {
                        // Create a copy of the temp to go to the late argument list
                        defArg = compiler->gtNewLclvNode(tmpVarNum, lclVarType);
                    }

                    curArgTabEntry->isTmp  = true;
                    curArgTabEntry->tmpNum = tmpVarNum;

#ifdef _TARGET_ARM_
                    // Previously we might have thought the local was promoted, and thus the 'COPYBLK'
                    // might have left holes in the used registers (see
                    // fgAddSkippedRegsInPromotedStructArg).
                    // Too bad we're not that smart for these intermediate temps...
                    if (isValidIntArgReg(curArgTabEntry->regNum) && (curArgTabEntry->numRegs > 1))
                    {
                        regNumber argReg      = curArgTabEntry->regNum;
                        regMaskTP allUsedRegs = genRegMask(curArgTabEntry->regNum);
                        for (unsigned i = 1; i < curArgTabEntry->numRegs; i++)
                        {
                            argReg = genRegArgNext(argReg);
                            allUsedRegs |= genRegMask(argReg);
                        }
#ifdef LEGACY_BACKEND
                        callTree->gtCall.gtCallRegUsedMask |= allUsedRegs;
#endif // LEGACY_BACKEND
                    }
#endif // _TARGET_ARM_
                }

                /* mark the assignment as a late argument */
                setupArg->gtFlags |= GTF_LATE_ARG;

#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\n  Evaluate to a temp:\n");
                    compiler->gtDispTree(setupArg);
                }
#endif
            }
        }
        else // curArgTabEntry->needTmp == false
        {
            //   On x86 -
            //      Only register args are replaced with placeholder nodes
            //      and the stack based arguments are evaluated and pushed in order.
            //
            //   On Arm/x64 - When needTmp is false and needPlace is false,
            //      the non-register arguments are evaluated and stored in order.
            //      When needPlace is true we have a nested call that comes after
            //      this argument so we have to replace it in the gtCallArgs list
            //      (the initial argument evaluation list) with a placeholder.
            //
            if ((curArgTabEntry->regNum == REG_STK) && (curArgTabEntry->needPlace == false))
            {
                continue;
            }

            /* No temp needed - move the whole node to the gtCallLateArgs list */

            /* The argument is deferred and put in the late argument list */

            defArg = argx;

            // Create a placeholder node to put in its place in gtCallLateArgs.

            // For a struct type we also need to record the class handle of the arg.
            CORINFO_CLASS_HANDLE clsHnd = NO_CLASS_HANDLE;

#if defined(_TARGET_AMD64_) && !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

            // All structs are either passed (and retyped) as integral types, OR they
            // are passed by reference.
            noway_assert(argx->gtType != TYP_STRUCT);

#else // !defined(_TARGET_AMD64_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

            if (varTypeIsStruct(defArg))
            {
                // Need a temp to walk any GT_COMMA nodes when searching for the clsHnd
                GenTreePtr defArgTmp = defArg;

                // The GT_OBJ may be be a child of a GT_COMMA.
                while (defArgTmp->gtOper == GT_COMMA)
                {
                    defArgTmp = defArgTmp->gtOp.gtOp2;
                }
                assert(varTypeIsStruct(defArgTmp));

                // We handle two opcodes: GT_MKREFANY and GT_OBJ.
                if (defArgTmp->gtOper == GT_MKREFANY)
                {
                    clsHnd = compiler->impGetRefAnyClass();
                }
                else if (defArgTmp->gtOper == GT_OBJ)
                {
                    clsHnd = defArgTmp->AsObj()->gtClass;
                }
                else
                {
                    BADCODE("Unhandled struct argument tree in fgMorphArgs");
                }
            }

#endif // !(defined(_TARGET_AMD64_) && !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING))

            setupArg = compiler->gtNewArgPlaceHolderNode(defArg->gtType, clsHnd);

            /* mark the placeholder node as a late argument */
            setupArg->gtFlags |= GTF_LATE_ARG;

#ifdef DEBUG
            if (compiler->verbose)
            {
                if (curArgTabEntry->regNum == REG_STK)
                {
                    printf("Deferred stack argument :\n");
                }
                else
                {
                    printf("Deferred argument ('%s'):\n", getRegName(curArgTabEntry->regNum));
                }

                compiler->gtDispTree(argx);
                printf("Replaced with placeholder node:\n");
                compiler->gtDispTree(setupArg);
            }
#endif
        }

        if (setupArg != nullptr)
        {
            if (curArgTabEntry->parent)
            {
                GenTreePtr parent = curArgTabEntry->parent;
                /* a normal argument from the list */
                noway_assert(parent->IsList());
                noway_assert(parent->gtOp.gtOp1 == argx);

                parent->gtOp.gtOp1 = setupArg;
            }
            else
            {
                /* must be the gtCallObjp */
                noway_assert(callTree->gtCall.gtCallObjp == argx);

                callTree->gtCall.gtCallObjp = setupArg;
            }
        }

        /* deferred arg goes into the late argument list */

        if (tmpRegArgNext == nullptr)
        {
            tmpRegArgNext                   = compiler->gtNewArgList(defArg);
            callTree->gtCall.gtCallLateArgs = tmpRegArgNext;
        }
        else
        {
            noway_assert(tmpRegArgNext->IsList());
            noway_assert(tmpRegArgNext->Current());
            tmpRegArgNext->gtOp.gtOp2 = compiler->gtNewArgList(defArg);
            tmpRegArgNext             = tmpRegArgNext->Rest();
        }

        curArgTabEntry->node       = defArg;
        curArgTabEntry->lateArgInx = regArgInx++;
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\nShuffled argument table:    ");
        for (unsigned curInx = 0; curInx < argCount; curInx++)
        {
            fgArgTabEntryPtr curArgTabEntry = argTable[curInx];

            if (curArgTabEntry->regNum != REG_STK)
            {
                printf("%s ", getRegName(curArgTabEntry->regNum));
            }
        }
        printf("\n");
    }
#endif
}

void fgArgInfo::RecordStkLevel(unsigned stkLvl)
{
    assert(!IsUninitialized(stkLvl));
    this->stkLevel = stkLvl;
}

unsigned fgArgInfo::RetrieveStkLevel()
{
    assert(!IsUninitialized(stkLevel));
    return stkLevel;
}

// Return a conservative estimate of the stack size in bytes.
// It will be used only on the intercepted-for-host code path to copy the arguments.
int Compiler::fgEstimateCallStackSize(GenTreeCall* call)
{

    int numArgs = 0;
    for (GenTreeArgList* args = call->gtCallArgs; args; args = args->Rest())
    {
        numArgs++;
    }

    int numStkArgs;
    if (numArgs > MAX_REG_ARG)
    {
        numStkArgs = numArgs - MAX_REG_ARG;
    }
    else
    {
        numStkArgs = 0;
    }

    return numStkArgs * REGSIZE_BYTES;
}

//------------------------------------------------------------------------------
// fgMakeMultiUse : If the node is a local, clone it and increase the ref count
//                  otherwise insert a comma form temp
//
// Arguments:
//    ppTree  - a pointer to the child node we will be replacing with the comma expression that
//              evaluates ppTree to a temp and returns the result
//
// Return Value:
//    A fresh GT_LCL_VAR node referencing the temp which has not been used
//
// Assumption:
//    The result tree MUST be added to the tree structure since the ref counts are
//    already incremented.

GenTree* Compiler::fgMakeMultiUse(GenTree** pOp)
{
    GenTree* tree = *pOp;
    if (tree->IsLocal())
    {
        auto result = gtClone(tree);
        if (lvaLocalVarRefCounted)
        {
            lvaTable[tree->gtLclVarCommon.gtLclNum].incRefCnts(compCurBB->getBBWeight(this), this);
        }
        return result;
    }
    else
    {
        GenTree* result = fgInsertCommaFormTemp(pOp);

        // At this point, *pOp is GT_COMMA(GT_ASG(V01, *pOp), V01) and result = V01
        // Therefore, the ref count has to be incremented 3 times for *pOp and result, if result will
        // be added by the caller.
        if (lvaLocalVarRefCounted)
        {
            lvaTable[result->gtLclVarCommon.gtLclNum].incRefCnts(compCurBB->getBBWeight(this), this);
            lvaTable[result->gtLclVarCommon.gtLclNum].incRefCnts(compCurBB->getBBWeight(this), this);
            lvaTable[result->gtLclVarCommon.gtLclNum].incRefCnts(compCurBB->getBBWeight(this), this);
        }

        return result;
    }
}

//------------------------------------------------------------------------------
// fgInsertCommaFormTemp: Create a new temporary variable to hold the result of *ppTree,
//                        and replace *ppTree with comma(asg(newLcl, *ppTree), newLcl)
//
// Arguments:
//    ppTree     - a pointer to the child node we will be replacing with the comma expression that
//                 evaluates ppTree to a temp and returns the result
//
//    structType - value type handle if the temp created is of TYP_STRUCT.
//
// Return Value:
//    A fresh GT_LCL_VAR node referencing the temp which has not been used
//

GenTree* Compiler::fgInsertCommaFormTemp(GenTree** ppTree, CORINFO_CLASS_HANDLE structType /*= nullptr*/)
{
    GenTree* subTree = *ppTree;

    unsigned lclNum = lvaGrabTemp(true DEBUGARG("fgInsertCommaFormTemp is creating a new local variable"));

    if (varTypeIsStruct(subTree))
    {
        assert(structType != nullptr);
        lvaSetStruct(lclNum, structType, false);
    }

    // If subTree->TypeGet() == TYP_STRUCT, gtNewTempAssign() will create a GT_COPYBLK tree.
    // The type of GT_COPYBLK is TYP_VOID.  Therefore, we should use subTree->TypeGet() for
    // setting type of lcl vars created.
    GenTree* asg = gtNewTempAssign(lclNum, subTree);

    GenTree* load = new (this, GT_LCL_VAR) GenTreeLclVar(subTree->TypeGet(), lclNum, BAD_IL_OFFSET);

    GenTree* comma = gtNewOperNode(GT_COMMA, subTree->TypeGet(), asg, load);

    *ppTree = comma;

    return new (this, GT_LCL_VAR) GenTreeLclVar(subTree->TypeGet(), lclNum, BAD_IL_OFFSET);
}

//------------------------------------------------------------------------
// fgMorphArgs: Walk and transform (morph) the arguments of a call
//
// Arguments:
//    callNode - the call for which we are doing the argument morphing
//
// Return Value:
//    Like most morph methods, this method returns the morphed node,
//    though in this case there are currently no scenarios where the
//    node itself is re-created.
//
// Notes:
//    This method is even less idempotent than most morph methods.
//    That is, it makes changes that should not be redone. It uses the existence
//    of gtCallLateArgs (the late arguments list) to determine if it has
//    already done that work.
//
//    The first time it is called (i.e. during global morphing), this method
//    computes the "late arguments". This is when it determines which arguments
//    need to be evaluated to temps prior to the main argument setup, and which
//    can be directly evaluated into the argument location. It also creates a
//    second argument list (gtCallLateArgs) that does the final placement of the
//    arguments, e.g. into registers or onto the stack.
//
//    The "non-late arguments", aka the gtCallArgs, are doing the in-order
//    evaluation of the arguments that might have side-effects, such as embedded
//    assignments, calls or possible throws. In these cases, it and earlier
//    arguments must be evaluated to temps.
//
//    On targets with a fixed outgoing argument area (FEATURE_FIXED_OUT_ARGS),
//    if we have any nested calls, we need to defer the copying of the argument
//    into the fixed argument area until after the call. If the argument did not
//    otherwise need to be computed into a temp, it is moved to gtCallLateArgs and
//    replaced in the "early" arg list (gtCallArgs) with a placeholder node.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTreeCall* Compiler::fgMorphArgs(GenTreeCall* callNode)
{
    GenTreeCall* call = callNode->AsCall();

    GenTreePtr args;
    GenTreePtr argx;

    unsigned flagsSummary    = 0;
    unsigned genPtrArgCntSav = fgPtrArgCntCur;

    unsigned argIndex = 0;

    unsigned intArgRegNum = 0;
    unsigned fltArgRegNum = 0;

#ifdef _TARGET_ARM_
    regMaskTP argSkippedRegMask    = RBM_NONE;
    regMaskTP fltArgSkippedRegMask = RBM_NONE;
#endif //  _TARGET_ARM_

#if defined(_TARGET_X86_)
    unsigned maxRegArgs = MAX_REG_ARG; // X86: non-const, must be calculated
#else
    const unsigned maxRegArgs = MAX_REG_ARG; // other arch: fixed constant number
#endif

    unsigned argSlots                = 0;
    unsigned nonRegPassedStructSlots = 0;
    bool     lateArgsComputed        = (call->gtCallLateArgs != nullptr);
    bool     callHasRetBuffArg       = call->HasRetBufArg();

#ifndef _TARGET_X86_ // i.e. _TARGET_AMD64_ or _TARGET_ARM_
    bool callIsVararg = call->IsVarargs();
#endif

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    // If fgMakeOutgoingStructArgCopy is called and copies are generated, hasStackArgCopy is set
    // to make sure to call EvalArgsToTemp. fgMakeOutgoingStructArgCopy just marks the argument
    // to need a temp variable, and EvalArgsToTemp actually creates the temp variable node.
    bool hasStackArgCopy = false;
#endif

#ifndef LEGACY_BACKEND
    // Data structure for keeping track of non-standard args. Non-standard args are those that are not passed
    // following the normal calling convention or in the normal argument registers. We either mark existing
    // arguments as non-standard (such as the x8 return buffer register on ARM64), or we manually insert the
    // non-standard arguments into the argument list, below.
    class NonStandardArgs
    {
        struct NonStandardArg
        {
            regNumber reg;  // The register to be assigned to this non-standard argument.
            GenTree*  node; // The tree node representing this non-standard argument.
                            //   Note that this must be updated if the tree node changes due to morphing!
        };

        ArrayStack<NonStandardArg> args;

    public:
        NonStandardArgs(Compiler* compiler) : args(compiler, 3) // We will have at most 3 non-standard arguments
        {
        }

        //-----------------------------------------------------------------------------
        // Add: add a non-standard argument to the table of non-standard arguments
        //
        // Arguments:
        //    node - a GenTree node that has a non-standard argument.
        //    reg - the register to assign to this node.
        //
        // Return Value:
        //    None.
        //
        void Add(GenTree* node, regNumber reg)
        {
            NonStandardArg nsa = {reg, node};
            args.Push(nsa);
        }

        //-----------------------------------------------------------------------------
        // Find: Look for a GenTree* in the set of non-standard args.
        //
        // Arguments:
        //    node - a GenTree node to look for
        //
        // Return Value:
        //    The index of the non-standard argument (a non-negative, unique, stable number).
        //    If the node is not a non-standard argument, return -1.
        //
        int Find(GenTree* node)
        {
            for (int i = 0; i < args.Height(); i++)
            {
                if (node == args.Index(i).node)
                {
                    return i;
                }
            }
            return -1;
        }

        //-----------------------------------------------------------------------------
        // FindReg: Look for a GenTree node in the non-standard arguments set. If found,
        // set the register to use for the node.
        //
        // Arguments:
        //    node - a GenTree node to look for
        //    pReg - an OUT argument. *pReg is set to the non-standard register to use if
        //           'node' is found in the non-standard argument set.
        //
        // Return Value:
        //    'true' if 'node' is a non-standard argument. In this case, *pReg is set to the
        //          register to use.
        //    'false' otherwise (in this case, *pReg is unmodified).
        //
        bool FindReg(GenTree* node, regNumber* pReg)
        {
            for (int i = 0; i < args.Height(); i++)
            {
                NonStandardArg& nsa = args.IndexRef(i);
                if (node == nsa.node)
                {
                    *pReg = nsa.reg;
                    return true;
                }
            }
            return false;
        }

        //-----------------------------------------------------------------------------
        // Replace: Replace the non-standard argument node at a given index. This is done when
        // the original node was replaced via morphing, but we need to continue to assign a
        // particular non-standard arg to it.
        //
        // Arguments:
        //    index - the index of the non-standard arg. It must exist.
        //    node - the new GenTree node.
        //
        // Return Value:
        //    None.
        //
        void Replace(int index, GenTree* node)
        {
            args.IndexRef(index).node = node;
        }

    } nonStandardArgs(this);
#endif // !LEGACY_BACKEND

    // Count of args. On first morph, this is counted before we've filled in the arg table.
    // On remorph, we grab it from the arg table.
    unsigned numArgs = 0;

    // Process the late arguments (which were determined by a previous caller).
    // Do this before resetting fgPtrArgCntCur as fgMorphTree(call->gtCallLateArgs)
    // may need to refer to it.
    if (lateArgsComputed)
    {
        // We need to reMorph the gtCallLateArgs early since that is what triggers
        // the expression folding and we need to have the final folded gtCallLateArgs
        // available when we call RemorphRegArg so that we correctly update the fgArgInfo
        // with the folded tree that represents the final optimized argument nodes.
        //
        // However if a range-check needs to be generated for any of these late
        // arguments we also need to "know" what the stack depth will be when we generate
        // code to branch to the throw range check failure block as that is part of the
        // GC information contract for that block.
        //
        // Since the late arguments are evaluated last we have pushed all of the
        // other arguments on the stack before we evaluate these late arguments,
        // so we record the stack depth on the first morph call when lateArgsComputed
        // was false (via RecordStkLevel) and then retrieve that value here (via RetrieveStkLevel)
        //
        unsigned callStkLevel = call->fgArgInfo->RetrieveStkLevel();
        fgPtrArgCntCur += callStkLevel;
        call->gtCallLateArgs = fgMorphTree(call->gtCallLateArgs)->AsArgList();
        flagsSummary |= call->gtCallLateArgs->gtFlags;
        fgPtrArgCntCur -= callStkLevel;
        assert(call->fgArgInfo != nullptr);
        call->fgArgInfo->RemorphReset();

        numArgs = call->fgArgInfo->ArgCount();
    }
    else
    {
        // First we need to count the args
        if (call->gtCallObjp)
        {
            numArgs++;
        }
        for (args = call->gtCallArgs; (args != nullptr); args = args->gtOp.gtOp2)
        {
            numArgs++;
        }

        // Insert or mark non-standard args. These are either outside the normal calling convention, or
        // arguments registers that don't follow the normal progression of argument registers in the calling
        // convention (such as for the ARM64 fixed return buffer argument x8).
        //
        // *********** NOTE *************
        // The logic here must remain in sync with GetNonStandardAddedArgCount(), which is used to map arguments
        // in the implementation of fast tail call.
        // *********** END NOTE *********
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(LEGACY_BACKEND) && defined(_TARGET_X86_)
        // The x86 CORINFO_HELP_INIT_PINVOKE_FRAME helper has a custom calling convention. Set the argument registers
        // correctly here.
        if (call->IsHelperCall(this, CORINFO_HELP_INIT_PINVOKE_FRAME))
        {
            GenTreeArgList* args = call->gtCallArgs;
            GenTree*        arg1 = args->Current();
            assert(arg1 != nullptr);
            nonStandardArgs.Add(arg1, REG_PINVOKE_FRAME);
        }
#endif // !defined(LEGACY_BACKEND) && defined(_TARGET_X86_)

#if !defined(LEGACY_BACKEND) && !defined(_TARGET_X86_)
        // TODO-X86-CQ: Currently RyuJIT/x86 passes args on the stack, so this is not needed.
        // If/when we change that, the following code needs to be changed to correctly support the (TBD) managed calling
        // convention for x86/SSE.

        // If we have a Fixed Return Buffer argument register then we setup a non-standard argument for it
        //
        if (hasFixedRetBuffReg() && call->HasRetBufArg())
        {
            args = call->gtCallArgs;
            assert(args != nullptr);
            assert(args->IsList());

            argx = call->gtCallArgs->Current();

            // We don't increment numArgs here, since we already counted this argument above.

            nonStandardArgs.Add(argx, theFixedRetBuffReg());
        }

        // We are allowed to have a Fixed Return Buffer argument combined
        // with any of the remaining non-standard arguments
        //
        if (call->IsUnmanaged() && !opts.ShouldUsePInvokeHelpers())
        {
            assert(!call->gtCallCookie);
            // Add a conservative estimate of the stack size in a special parameter (r11) at the call site.
            // It will be used only on the intercepted-for-host code path to copy the arguments.

            GenTree* cns     = new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, fgEstimateCallStackSize(call));
            call->gtCallArgs = gtNewListNode(cns, call->gtCallArgs);
            numArgs++;

            nonStandardArgs.Add(cns, REG_PINVOKE_COOKIE_PARAM);
        }
        else if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT) && !call->IsTailCallViaHelper())
        {
            // indirect VSD stubs need the base of the indirection cell to be
            // passed in addition.  At this point that is the value in gtCallAddr.
            // The actual call target will be derived from gtCallAddr in call
            // lowering.

            // If it is a VSD call getting dispatched via tail call helper,
            // fgMorphTailCall() would materialize stub addr as an additional
            // parameter added to the original arg list and hence no need to
            // add as a non-standard arg.

            GenTree* arg = call->gtCallAddr;
            if (arg->OperIsLocal())
            {
                arg = gtClone(arg, true);
            }
            else
            {
                call->gtCallAddr = fgInsertCommaFormTemp(&arg);
                call->gtFlags |= GTF_ASG;
            }
            noway_assert(arg != nullptr);

            // And push the stub address onto the list of arguments
            call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);
            numArgs++;

            nonStandardArgs.Add(arg, REG_VIRTUAL_STUB_PARAM);
        }
        else if (call->gtCallType == CT_INDIRECT && call->gtCallCookie)
        {
            assert(!call->IsUnmanaged());

            // put cookie into R11
            GenTree* arg = call->gtCallCookie;
            noway_assert(arg != nullptr);
            call->gtCallCookie = nullptr;

            call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);
            numArgs++;

            nonStandardArgs.Add(arg, REG_PINVOKE_COOKIE_PARAM);

            // put destination into R10
            arg              = gtClone(call->gtCallAddr, true);
            call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);
            numArgs++;

            nonStandardArgs.Add(arg, REG_PINVOKE_TARGET_PARAM);

            // finally change this call to a helper call
            call->gtCallType    = CT_HELPER;
            call->gtCallMethHnd = eeFindHelper(CORINFO_HELP_PINVOKE_CALLI);
        }
#endif // !defined(LEGACY_BACKEND) && !defined(_TARGET_X86_)

        // Allocate the fgArgInfo for the call node;
        //
        call->fgArgInfo = new (this, CMK_Unknown) fgArgInfo(this, call, numArgs);
    }

    if (varTypeIsStruct(call))
    {
        fgFixupStructReturn(call);
    }

    /* First we morph the argument subtrees ('this' pointer, arguments, etc.).
     * During the first call to fgMorphArgs we also record the
     * information about late arguments we have in 'fgArgInfo'.
     * This information is used later to contruct the gtCallLateArgs */

    /* Process the 'this' argument value, if present */

    argx = call->gtCallObjp;

    if (argx)
    {
        argx             = fgMorphTree(argx);
        call->gtCallObjp = argx;
        flagsSummary |= argx->gtFlags;

        assert(call->gtCallType == CT_USER_FUNC || call->gtCallType == CT_INDIRECT);

        assert(argIndex == 0);

        /* We must fill in or update the argInfo table */

        if (lateArgsComputed)
        {
            /* this is a register argument - possibly update it in the table */
            call->fgArgInfo->RemorphRegArg(argIndex, argx, nullptr, genMapIntRegArgNumToRegNum(intArgRegNum), 1, 1);
        }
        else
        {
            assert(varTypeIsGC(call->gtCallObjp->gtType) || (call->gtCallObjp->gtType == TYP_I_IMPL));

            /* this is a register argument - put it in the table */
            call->fgArgInfo->AddRegArg(argIndex, argx, nullptr, genMapIntRegArgNumToRegNum(intArgRegNum), 1, 1
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                                       ,
                                       false, REG_STK, nullptr
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
                                       );
        }
        // this can't be a struct.
        assert(argx->gtType != TYP_STRUCT);

        /* Increment the argument register count and argument index */
        if (!varTypeIsFloating(argx->gtType) || opts.compUseSoftFP)
        {
            intArgRegNum++;
#ifdef WINDOWS_AMD64_ABI
            // Whenever we pass an integer register argument
            // we skip the corresponding floating point register argument
            fltArgRegNum++;
#endif // WINDOWS_AMD64_ABI
        }
        else
        {
            noway_assert(!"the 'this' pointer can not be a floating point type");
        }
        argIndex++;
        argSlots++;
    }

#ifdef _TARGET_X86_
    // Compute the maximum number of arguments that can be passed in registers.
    // For X86 we handle the varargs and unmanaged calling conventions

    if (call->gtFlags & GTF_CALL_POP_ARGS)
    {
        noway_assert(intArgRegNum < MAX_REG_ARG);
        // No more register arguments for varargs (CALL_POP_ARGS)
        maxRegArgs = intArgRegNum;

        // Add in the ret buff arg
        if (callHasRetBuffArg)
            maxRegArgs++;
    }

    if (call->IsUnmanaged())
    {
        noway_assert(intArgRegNum == 0);

        if (call->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
        {
            noway_assert(call->gtCallArgs->gtOp.gtOp1->TypeGet() == TYP_I_IMPL ||
                         call->gtCallArgs->gtOp.gtOp1->TypeGet() == TYP_BYREF ||
                         call->gtCallArgs->gtOp.gtOp1->gtOper ==
                             GT_NOP); // the arg was already morphed to a register (fgMorph called twice)
            maxRegArgs = 1;
        }
        else
        {
            maxRegArgs = 0;
        }

        // Add in the ret buff arg
        if (callHasRetBuffArg)
            maxRegArgs++;
    }
#endif // _TARGET_X86_

    /* Morph the user arguments */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM_)

    // The ARM ABI has a concept of back-filling of floating-point argument registers, according
    // to the "Procedure Call Standard for the ARM Architecture" document, especially
    // section 6.1.2.3 "Parameter passing". Back-filling is where floating-point argument N+1 can
    // appear in a lower-numbered register than floating point argument N. That is, argument
    // register allocation is not strictly increasing. To support this, we need to keep track of unused
    // floating-point argument registers that we can back-fill. We only support 4-byte float and
    // 8-byte double types, and one to four element HFAs composed of these types. With this, we will
    // only back-fill single registers, since there is no way with these types to create
    // an alignment hole greater than one register. However, there can be up to 3 back-fill slots
    // available (with 16 FP argument registers). Consider this code:
    //
    // struct HFA { float x, y, z; }; // a three element HFA
    // void bar(float a1,   // passed in f0
    //          double a2,  // passed in f2/f3; skip f1 for alignment
    //          HFA a3,     // passed in f4/f5/f6
    //          double a4,  // passed in f8/f9; skip f7 for alignment. NOTE: it doesn't fit in the f1 back-fill slot
    //          HFA a5,     // passed in f10/f11/f12
    //          double a6,  // passed in f14/f15; skip f13 for alignment. NOTE: it doesn't fit in the f1 or f7 back-fill
    //                      // slots
    //          float a7,   // passed in f1 (back-filled)
    //          float a8,   // passed in f7 (back-filled)
    //          float a9,   // passed in f13 (back-filled)
    //          float a10)  // passed on the stack in [OutArg+0]
    //
    // Note that if we ever support FP types with larger alignment requirements, then there could
    // be more than single register back-fills.
    //
    // Once we assign a floating-pointer register to the stack, they all must be on the stack.
    // See "Procedure Call Standard for the ARM Architecture", section 6.1.2.3, "The back-filling
    // continues only so long as no VFP CPRC has been allocated to a slot on the stack."
    // We set anyFloatStackArgs to true when a floating-point argument has been assigned to the stack
    // and prevent any additional floating-point arguments from going in registers.

    bool anyFloatStackArgs = false;

#endif // _TARGET_ARM_

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    bool hasStructArgument     = false; // @TODO-ARM64-UNIX: Remove this bool during a future refactoring
    bool hasMultiregStructArgs = false;
    for (args = call->gtCallArgs; args; args = args->gtOp.gtOp2, argIndex++)
    {
        GenTreePtr* parentArgx = &args->gtOp.gtOp1;

#if FEATURE_MULTIREG_ARGS
        if (!hasStructArgument)
        {
            hasStructArgument = varTypeIsStruct(args->gtOp.gtOp1);
        }
#endif // FEATURE_MULTIREG_ARGS

#ifndef LEGACY_BACKEND
        // Record the index of any nonStandard arg that we may be processing here, as we are
        // about to call fgMorphTree on it and fgMorphTree may replace it with a new tree.
        GenTreePtr orig_argx         = *parentArgx;
        int        nonStandard_index = nonStandardArgs.Find(orig_argx);
#endif // !LEGACY_BACKEND

        argx        = fgMorphTree(*parentArgx);
        *parentArgx = argx;
        flagsSummary |= argx->gtFlags;

        assert(args->IsList());
        assert(argx == args->Current());

#ifndef LEGACY_BACKEND
        if ((nonStandard_index != -1) && (argx != orig_argx))
        {
            // We need to update the node field for this nonStandard arg here
            // as it was changed by the call to fgMorphTree
            nonStandardArgs.Replace(nonStandard_index, argx);
        }
#endif // !LEGACY_BACKEND

        /* Change the node to TYP_I_IMPL so we don't report GC info
         * NOTE: We deferred this from the importer because of the inliner */

        if (argx->IsVarAddr())
        {
            argx->gtType = TYP_I_IMPL;
        }

        bool     passUsingFloatRegs;
        unsigned argAlign = 1;
        // Setup any HFA information about 'argx'
        var_types hfaType  = GetHfaType(argx);
        bool      isHfaArg = varTypeIsFloating(hfaType);
        unsigned  hfaSlots = 0;

        if (isHfaArg)
        {
            hfaSlots = GetHfaCount(argx);

            // If we have a HFA struct it's possible we transition from a method that originally
            // only had integer types to now start having FP types.  We have to communicate this
            // through this flag since LSRA later on will use this flag to determine whether
            // or not to track the FP register set.
            //
            compFloatingPointUsed = true;
        }

        unsigned             size         = 0;
        CORINFO_CLASS_HANDLE copyBlkClass = nullptr;
        bool                 isRegArg     = false;

        fgArgTabEntryPtr argEntry = nullptr;

        if (lateArgsComputed)
        {
            argEntry = gtArgEntryByArgNum(call, argIndex);
        }

#ifdef _TARGET_ARM_

        bool passUsingIntRegs;
        if (lateArgsComputed)
        {
            passUsingFloatRegs = isValidFloatArgReg(argEntry->regNum);
            passUsingIntRegs   = isValidIntArgReg(argEntry->regNum);
        }
        else
        {
            passUsingFloatRegs = !callIsVararg && (isHfaArg || varTypeIsFloating(argx)) && !opts.compUseSoftFP;
            passUsingIntRegs   = passUsingFloatRegs ? false : (intArgRegNum < MAX_REG_ARG);
        }

        GenTreePtr curArg = argx;
        // If late args have already been computed, use the node in the argument table.
        if (argEntry != NULL && argEntry->isTmp)
        {
            curArg = argEntry->node;
        }

        // We don't use the "size" return value from InferOpSizeAlign().
        codeGen->InferOpSizeAlign(curArg, &argAlign);

        argAlign = roundUp(argAlign, TARGET_POINTER_SIZE);
        argAlign /= TARGET_POINTER_SIZE;

        if (argAlign == 2)
        {
            if (passUsingFloatRegs)
            {
                if (fltArgRegNum % 2 == 1)
                {
                    fltArgSkippedRegMask |= genMapArgNumToRegMask(fltArgRegNum, TYP_FLOAT);
                    fltArgRegNum++;
                }
            }
            else if (passUsingIntRegs)
            {
                if (intArgRegNum % 2 == 1)
                {
                    argSkippedRegMask |= genMapArgNumToRegMask(intArgRegNum, TYP_I_IMPL);
                    intArgRegNum++;
                }
            }

            if (argSlots % 2 == 1)
            {
                argSlots++;
            }
        }

#elif defined(_TARGET_ARM64_)

        if (lateArgsComputed)
        {
            passUsingFloatRegs = isValidFloatArgReg(argEntry->regNum);
        }
        else
        {
            passUsingFloatRegs = !callIsVararg && (isHfaArg || varTypeIsFloating(argx));
        }

#elif defined(_TARGET_AMD64_)
#if defined(UNIX_AMD64_ABI)
        if (lateArgsComputed)
        {
            passUsingFloatRegs = isValidFloatArgReg(argEntry->regNum);
        }
        else
        {
            passUsingFloatRegs = varTypeIsFloating(argx);
        }
#else  // WINDOWS_AMD64_ABI
        passUsingFloatRegs = varTypeIsFloating(argx);
#endif // !UNIX_AMD64_ABI
#elif defined(_TARGET_X86_)

        passUsingFloatRegs = false;

#else
#error Unsupported or unset target architecture
#endif // _TARGET_*

        bool      isBackFilled     = false;
        unsigned  nextFltArgRegNum = fltArgRegNum; // This is the next floating-point argument register number to use
        var_types structBaseType   = TYP_STRUCT;
        unsigned  structSize       = 0;

        bool isStructArg = varTypeIsStruct(argx);

        if (lateArgsComputed)
        {
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            // Get the struct description for the already completed struct argument.
            fgArgTabEntryPtr fgEntryPtr = gtArgEntryByNode(call, argx);
            assert(fgEntryPtr != nullptr);

            // As described in few other places, this can happen when the argx was morphed
            // into an arg setup node - COPYBLK. The COPYBLK has always a type of void.
            // In such case the fgArgTabEntry keeps track of whether the original node (before morphing)
            // was a struct and the struct classification.
            isStructArg = fgEntryPtr->isStruct;

            if (isStructArg)
            {
                structDesc.CopyFrom(fgEntryPtr->structDesc);
            }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

            assert(argEntry != nullptr);
            if (argEntry->IsBackFilled())
            {
                isRegArg         = true;
                size             = argEntry->numRegs;
                nextFltArgRegNum = genMapFloatRegNumToRegArgNum(argEntry->regNum);
                assert(size == 1);
                isBackFilled = true;
            }
            else if (argEntry->regNum == REG_STK)
            {
                isRegArg = false;
                assert(argEntry->numRegs == 0);
                size = argEntry->numSlots;
            }
            else
            {
                isRegArg = true;
                assert(argEntry->numRegs > 0);
                size = argEntry->numRegs + argEntry->numSlots;
            }

            // This size has now been computed
            assert(size != 0);
        }
        else // !lateArgsComputed
        {
            //
            // Figure out the size of the argument. This is either in number of registers, or number of
            // TARGET_POINTER_SIZE stack slots, or the sum of these if the argument is split between the registers and
            // the stack.
            //
            if (argx->IsArgPlaceHolderNode() || (!isStructArg))
            {
#if defined(_TARGET_AMD64_)
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                if (!isStructArg)
                {
                    size = 1; // On AMD64, all primitives fit in a single (64-bit) 'slot'
                }
                else
                {
                    size = (unsigned)(roundUp(info.compCompHnd->getClassSize(argx->gtArgPlace.gtArgPlaceClsHnd),
                                              TARGET_POINTER_SIZE)) /
                           TARGET_POINTER_SIZE;
                    eeGetSystemVAmd64PassStructInRegisterDescriptor(argx->gtArgPlace.gtArgPlaceClsHnd, &structDesc);
                    if (size > 1)
                    {
                        hasMultiregStructArgs = true;
                    }
                }
#else  // !FEATURE_UNIX_AMD64_STRUCT_PASSING
                size = 1; // On AMD64, all primitives fit in a single (64-bit) 'slot'
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
#elif defined(_TARGET_ARM64_)
                if (isStructArg)
                {
                    if (isHfaArg)
                    {
                        size = GetHfaCount(argx);
                        // HFA structs are passed by value in multiple registers
                        hasMultiregStructArgs = true;
                    }
                    else
                    {
                        // Structs are either passed in 1 or 2 (64-bit) slots
                        size = (unsigned)(roundUp(info.compCompHnd->getClassSize(argx->gtArgPlace.gtArgPlaceClsHnd),
                                                  TARGET_POINTER_SIZE)) /
                               TARGET_POINTER_SIZE;

                        if (size == 2)
                        {
                            // Structs that are the size of 2 pointers are passed by value in multiple registers
                            hasMultiregStructArgs = true;
                        }
                        else if (size > 2)
                        {
                            size = 1; // Structs that are larger that 2 pointers (except for HFAs) are passed by
                                      // reference (to a copy)
                        }
                    }
                    // Note that there are some additional rules for multireg structs.
                    // (i.e they cannot be split between registers and the stack)
                }
                else
                {
                    size = 1; // Otherwise, all primitive types fit in a single (64-bit) 'slot'
                }
#elif defined(_TARGET_ARM_)
                if (isStructArg)
                {
                    size = (unsigned)(roundUp(info.compCompHnd->getClassSize(argx->gtArgPlace.gtArgPlaceClsHnd),
                                              TARGET_POINTER_SIZE)) /
                           TARGET_POINTER_SIZE;
                }
                else
                {
                    // The typical case
                    size = genTypeStSz(argx->gtType);
                }
#elif defined(_TARGET_X86_)
                size       = genTypeStSz(argx->gtType);
#else
#error Unsupported or unset target architecture
#endif // _TARGET_XXX_
            }
#ifdef _TARGET_ARM_
            else if (isHfaArg)
            {
                size = GetHfaCount(argx);
            }
#endif           // _TARGET_ARM_
            else // struct type
            {
                // We handle two opcodes: GT_MKREFANY and GT_OBJ
                if (argx->gtOper == GT_MKREFANY)
                {
                    if (varTypeIsStruct(argx))
                    {
                        isStructArg = true;
                    }
#ifdef _TARGET_AMD64_
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                    if (varTypeIsStruct(argx))
                    {
                        size                 = info.compCompHnd->getClassSize(impGetRefAnyClass());
                        unsigned roundupSize = (unsigned)roundUp(size, TARGET_POINTER_SIZE);
                        size                 = roundupSize / TARGET_POINTER_SIZE;
                        eeGetSystemVAmd64PassStructInRegisterDescriptor(impGetRefAnyClass(), &structDesc);
                    }
                    else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                    {
                        size = 1;
                    }
#else
                    size                 = 2;
#endif
                }
                else // We must have a GT_OBJ with a struct type, but the GT_OBJ may be be a child of a GT_COMMA
                {
                    GenTreePtr  argObj         = argx;
                    GenTreePtr* parentOfArgObj = parentArgx;

                    assert(args->IsList());
                    assert(argx == args->Current());

                    /* The GT_OBJ may be be a child of a GT_COMMA */
                    while (argObj->gtOper == GT_COMMA)
                    {
                        parentOfArgObj = &argObj->gtOp.gtOp2;
                        argObj         = argObj->gtOp.gtOp2;
                    }

                    // TODO-1stClassStructs: An OBJ node should not be required for lclVars.
                    if (argObj->gtOper != GT_OBJ)
                    {
                        BADCODE("illegal argument tree in fgMorphArgs");
                    }

                    CORINFO_CLASS_HANDLE objClass = argObj->gtObj.gtClass;
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                    eeGetSystemVAmd64PassStructInRegisterDescriptor(objClass, &structDesc);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

                    unsigned originalSize = info.compCompHnd->getClassSize(objClass);
                    originalSize          = (originalSize == 0 ? TARGET_POINTER_SIZE : originalSize);
                    unsigned roundupSize  = (unsigned)roundUp(originalSize, TARGET_POINTER_SIZE);

                    structSize = originalSize;

                    structPassingKind howToPassStruct;
                    structBaseType = getArgTypeForStruct(objClass, &howToPassStruct, originalSize);

#ifdef _TARGET_ARM64_
                    if ((howToPassStruct == SPK_PrimitiveType) && // Passed in a single register
                        !isPow2(originalSize))                    // size is 3,5,6 or 7 bytes
                    {
                        if (argObj->gtObj.gtOp1->IsVarAddr()) // Is the source a LclVar?
                        {
                            // For ARM64 we pass structs that are 3,5,6,7 bytes in size
                            // we can read 4 or 8 bytes from the LclVar to pass this arg
                            originalSize = genTypeSize(structBaseType);
                        }
                    }
#endif //  _TARGET_ARM64_

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                    // On System V OS-es a struct is never passed by reference.
                    // It is either passed by value on the stack or in registers.
                    bool passStructInRegisters = false;
#else  // !FEATURE_UNIX_AMD64_STRUCT_PASSING
                    bool passStructByRef = false;
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

                    // The following if-then-else needs to be carefully refactored.
                    // Basically the else portion wants to turn a struct load (a GT_OBJ)
                    // into a GT_IND of the appropriate size.
                    // It can do this with structs sizes that are 1, 2, 4, or 8 bytes.
                    // It can't do this when FEATURE_UNIX_AMD64_STRUCT_PASSING is defined  (Why?)
                    // TODO-Cleanup: Remove the #ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING below.
                    // It also can't do this if we have a HFA arg,
                    // unless we have a 1-elem HFA in which case we want to do the optimization.
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_X86_
#ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING
                    // Check for struct argument with size 1, 2, 4 or 8 bytes
                    // As we can optimize these by turning them into a GT_IND of the correct type
                    //
                    // Check for cases that we cannot optimize:
                    //
                    if ((originalSize > TARGET_POINTER_SIZE) || // it is struct that is larger than a pointer
                        !isPow2(originalSize) ||                // it is not a power of two (1, 2, 4 or 8)
                        (isHfaArg && (hfaSlots != 1)))          // it is a one element HFA struct
#endif                                                          // FEATURE_UNIX_AMD64_STRUCT_PASSING
                    {
                        // Normalize 'size' to the number of pointer sized items
                        // 'size' is the number of register slots that we will use to pass the argument
                        size = roundupSize / TARGET_POINTER_SIZE;
#if defined(_TARGET_AMD64_)
#ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING
                        size            = 1; // This must be copied to a temp and passed by address
                        passStructByRef = true;
                        copyBlkClass    = objClass;
#else // FEATURE_UNIX_AMD64_STRUCT_PASSING
                        if (!structDesc.passedInRegisters)
                        {
                            GenTreePtr lclVar     = fgIsIndirOfAddrOfLocal(argObj);
                            bool       needCpyBlk = false;
                            if (lclVar != nullptr)
                            {
                                // If the struct is promoted to registers, it has to be materialized
                                // on stack. We may want to support promoted structures in
                                // codegening pugarg_stk instead of creating a copy here.
                                LclVarDsc* varDsc = &lvaTable[lclVar->gtLclVarCommon.gtLclNum];
                                needCpyBlk        = varDsc->lvPromoted;
                            }
                            else
                            {
                                // If simd16 comes from vector<t>, eeGetSystemVAmd64PassStructInRegisterDescriptor
                                // sets structDesc.passedInRegisters to be false.
                                //
                                // GT_ADDR(GT_SIMD) is not a rationalized IR form and is not handled
                                // by rationalizer. For now we will let SIMD struct arg to be copied to
                                // a local. As part of cpblk rewrite, rationalizer will handle GT_ADDR(GT_SIMD)
                                //
                                // +--*  obj       simd16
                                // |  \--*  addr      byref
                                // |     |  /--*  lclVar    simd16 V05 loc4
                                // |     \--*  simd      simd16 int -
                                // |        \--*  lclVar    simd16 V08 tmp1
                                //
                                // TODO-Amd64-Unix: The rationalizer can be updated to handle this pattern,
                                // so that we don't need to generate a copy here.
                                GenTree* addr = argObj->gtOp.gtOp1;
                                if (addr->OperGet() == GT_ADDR)
                                {
                                    GenTree* addrChild = addr->gtOp.gtOp1;
                                    if (addrChild->OperGet() == GT_SIMD)
                                    {
                                        needCpyBlk = true;
                                    }
                                }
                            }
                            passStructInRegisters = false;
                            if (needCpyBlk)
                            {
                                copyBlkClass = objClass;
                            }
                            else
                            {
                                copyBlkClass = NO_CLASS_HANDLE;
                            }
                        }
                        else
                        {
                            // The objClass is used to materialize the struct on stack.
                            // For SystemV, the code below generates copies for struct arguments classified
                            // as register argument.
                            // TODO-Amd64-Unix: We don't always need copies for this case. Struct arguments
                            // can be passed on registers or can be copied directly to outgoing area.
                            passStructInRegisters = true;
                            copyBlkClass          = objClass;
                        }

#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
#elif defined(_TARGET_ARM64_)
                        if ((size > 2) && !isHfaArg)
                        {
                            size            = 1; // This must be copied to a temp and passed by address
                            passStructByRef = true;
                            copyBlkClass    = objClass;
                        }
#endif

#ifdef _TARGET_ARM_
                        // If we're passing a promoted struct local var,
                        // we may need to skip some registers due to alignment; record those.
                        GenTreePtr lclVar = fgIsIndirOfAddrOfLocal(argObj);
                        if (lclVar != NULL)
                        {
                            LclVarDsc* varDsc = &lvaTable[lclVar->gtLclVarCommon.gtLclNum];
                            if (varDsc->lvPromoted)
                            {
                                assert(argObj->OperGet() == GT_OBJ);
                                if (lvaGetPromotionType(varDsc) == PROMOTION_TYPE_INDEPENDENT)
                                {
                                    fgAddSkippedRegsInPromotedStructArg(varDsc, intArgRegNum, &argSkippedRegMask);
                                }
                            }
                        }
#endif // _TARGET_ARM_
                    }
#ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING
                    // TODO-Amd64-Unix: Since the else part below is disabled for UNIX_AMD64, copies are always
                    // generated for struct 1, 2, 4, or 8.
                    else // We have a struct argument with size 1, 2, 4 or 8 bytes
                    {
                        // change our GT_OBJ into a GT_IND of the correct type.
                        // We've already ensured above that size is a power of 2, and less than or equal to pointer
                        // size.

                        assert(howToPassStruct == SPK_PrimitiveType);

                        // ToDo: remove this block as getArgTypeForStruct properly handles turning one element HFAs into
                        // primitives
                        if (isHfaArg)
                        {
                            // If we reach here with an HFA arg it has to be a one element HFA
                            assert(hfaSlots == 1);
                            structBaseType = hfaType; // change the indirection type to a floating point type
                        }

                        noway_assert(structBaseType != TYP_UNKNOWN);

                        argObj->ChangeOper(GT_IND);

                        // Now see if we can fold *(&X) into X
                        if (argObj->gtOp.gtOp1->gtOper == GT_ADDR)
                        {
                            GenTreePtr temp = argObj->gtOp.gtOp1->gtOp.gtOp1;

                            // Keep the DONT_CSE flag in sync
                            // (as the addr always marks it for its op1)
                            temp->gtFlags &= ~GTF_DONT_CSE;
                            temp->gtFlags |= (argObj->gtFlags & GTF_DONT_CSE);
                            DEBUG_DESTROY_NODE(argObj->gtOp.gtOp1); // GT_ADDR
                            DEBUG_DESTROY_NODE(argObj);             // GT_IND

                            argObj          = temp;
                            *parentOfArgObj = temp;

                            // If the OBJ had been the top level node, we've now changed argx.
                            if (parentOfArgObj == parentArgx)
                            {
                                argx = temp;
                            }
                        }
                        if (argObj->gtOper == GT_LCL_VAR)
                        {
                            unsigned   lclNum = argObj->gtLclVarCommon.gtLclNum;
                            LclVarDsc* varDsc = &lvaTable[lclNum];

                            if (varDsc->lvPromoted)
                            {
                                if (varDsc->lvFieldCnt == 1)
                                {
                                    // get the first and only promoted field
                                    LclVarDsc* fieldVarDsc = &lvaTable[varDsc->lvFieldLclStart];
                                    if (genTypeSize(fieldVarDsc->TypeGet()) >= originalSize)
                                    {
                                        // we will use the first and only promoted field
                                        argObj->gtLclVarCommon.SetLclNum(varDsc->lvFieldLclStart);

                                        if (varTypeCanReg(fieldVarDsc->TypeGet()) &&
                                            (genTypeSize(fieldVarDsc->TypeGet()) == originalSize))
                                        {
                                            // Just use the existing field's type
                                            argObj->gtType = fieldVarDsc->TypeGet();
                                        }
                                        else
                                        {
                                            // Can't use the existing field's type, so use GT_LCL_FLD to swizzle
                                            // to a new type
                                            argObj->ChangeOper(GT_LCL_FLD);
                                            argObj->gtType = structBaseType;
                                        }
                                        assert(varTypeCanReg(argObj->TypeGet()));
                                        assert(copyBlkClass == NO_CLASS_HANDLE);
                                    }
                                    else
                                    {
                                        // use GT_LCL_FLD to swizzle the single field struct to a new type
                                        lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LocalField));
                                        argObj->ChangeOper(GT_LCL_FLD);
                                        argObj->gtType = structBaseType;
                                    }
                                }
                                else
                                {
                                    // The struct fits into a single register, but it has been promoted into its
                                    // constituent fields, and so we have to re-assemble it
                                    copyBlkClass = objClass;
#ifdef _TARGET_ARM_
                                    // Alignment constraints may cause us not to use (to "skip") some argument
                                    // registers. Add those, if any, to the skipped (int) arg reg mask.
                                    fgAddSkippedRegsInPromotedStructArg(varDsc, intArgRegNum, &argSkippedRegMask);
#endif // _TARGET_ARM_
                                }
                            }
                            else if (!varTypeIsIntegralOrI(varDsc->TypeGet()))
                            {
                                // Not a promoted struct, so just swizzle the type by using GT_LCL_FLD
                                argObj->ChangeOper(GT_LCL_FLD);
                                argObj->gtType = structBaseType;
                            }
                        }
                        else
                        {
                            // Not a GT_LCL_VAR, so we can just change the type on the node
                            argObj->gtType = structBaseType;
                        }
                        assert(varTypeCanReg(argObj->TypeGet()) ||
                               ((copyBlkClass != NO_CLASS_HANDLE) && varTypeIsIntegral(structBaseType)));

                        size = 1;
                    }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

#endif // not _TARGET_X86_
                    // We still have a struct unless we converted the GT_OBJ into a GT_IND above...
                    if ((structBaseType == TYP_STRUCT) &&
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                        !passStructInRegisters
#else  // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                        !passStructByRef
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                        )
                    {
                        if (isHfaArg && passUsingFloatRegs)
                        {
                            size = GetHfaCount(argx); // GetHfaCount returns number of elements in the HFA
                        }
                        else
                        {
                            // If the valuetype size is not a multiple of sizeof(void*),
                            // we must copyblk to a temp before doing the obj to avoid
                            // the obj reading memory past the end of the valuetype
                            CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
                            // TODO-X86-CQ: [1091733] Revisit for small structs, we should use push instruction
                            copyBlkClass = objClass;
                            size = roundupSize / TARGET_POINTER_SIZE; // Normalize size to number of pointer sized items
#else                                                                 // !defined(_TARGET_X86_) || defined(LEGACY_BACKEND)
                            if (roundupSize > originalSize)
                            {
                                copyBlkClass = objClass;

                                // There are a few special cases where we can omit using a CopyBlk
                                // where we normally would need to use one.

                                if (argObj->gtObj.gtOp1->IsVarAddr()) // Is the source a LclVar?
                                {
                                    copyBlkClass = NO_CLASS_HANDLE;
                                }
                            }

                            size = roundupSize / TARGET_POINTER_SIZE; // Normalize size to number of pointer sized items
#endif // !defined(_TARGET_X86_) || defined(LEGACY_BACKEND)
                        }
                    }
                }

#ifndef _TARGET_X86_
                // TODO-Arm: Does this apply for _TARGET_ARM_, where structs passed by value can be split between
                // registers and stack?
                if (size > 1)
                {
                    hasMultiregStructArgs = true;
                }
#endif // !_TARGET_X86_
            }

            // The 'size' value has now must have been set. (the original value of zero is an invalid value)
            assert(size != 0);

            //
            // Figure out if the argument will be passed in a register.
            //

            if (isRegParamType(genActualType(argx->TypeGet()))
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                && (!isStructArg || structDesc.passedInRegisters)
#endif
                    )
            {
#ifdef _TARGET_ARM_
                if (passUsingFloatRegs)
                {
                    // First, see if it can be back-filled
                    if (!anyFloatStackArgs && // Is it legal to back-fill? (We haven't put any FP args on the stack yet)
                        (fltArgSkippedRegMask != RBM_NONE) && // Is there an available back-fill slot?
                        (size == 1))                          // The size to back-fill is one float register
                    {
                        // Back-fill the register.
                        isBackFilled              = true;
                        regMaskTP backFillBitMask = genFindLowestBit(fltArgSkippedRegMask);
                        fltArgSkippedRegMask &=
                            ~backFillBitMask; // Remove the back-filled register(s) from the skipped mask
                        nextFltArgRegNum = genMapFloatRegNumToRegArgNum(genRegNumFromMask(backFillBitMask));
                        assert(nextFltArgRegNum < MAX_FLOAT_REG_ARG);
                    }

                    // Does the entire float, double, or HFA fit in the FP arg registers?
                    // Check if the last register needed is still in the argument register range.
                    isRegArg = (nextFltArgRegNum + size - 1) < MAX_FLOAT_REG_ARG;

                    if (!isRegArg)
                    {
                        anyFloatStackArgs = true;
                    }
                }
                else
                {
                    isRegArg = intArgRegNum < MAX_REG_ARG;
                }
#elif defined(_TARGET_ARM64_)
                if (passUsingFloatRegs)
                {
                    // Check if the last register needed is still in the fp argument register range.
                    isRegArg = (nextFltArgRegNum + (size - 1)) < MAX_FLOAT_REG_ARG;

                    // Do we have a HFA arg that we wanted to pass in registers, but we ran out of FP registers?
                    if (isHfaArg && !isRegArg)
                    {
                        // recompute the 'size' so that it represent the number of stack slots rather than the number of
                        // registers
                        //
                        unsigned roundupSize = (unsigned)roundUp(structSize, TARGET_POINTER_SIZE);
                        size                 = roundupSize / TARGET_POINTER_SIZE;

                        // We also must update fltArgRegNum so that we no longer try to
                        // allocate any new floating point registers for args
                        // This prevents us from backfilling a subsequent arg into d7
                        //
                        fltArgRegNum = MAX_FLOAT_REG_ARG;
                    }
                }
                else
                {
                    // Check if the last register needed is still in the int argument register range.
                    isRegArg = (intArgRegNum + (size - 1)) < maxRegArgs;

                    // Did we run out of registers when we had a 16-byte struct (size===2) ?
                    // (i.e we only have one register remaining but we needed two registers to pass this arg)
                    // This prevents us from backfilling a subsequent arg into x7
                    //
                    if (!isRegArg && (size > 1))
                    {
                        // We also must update intArgRegNum so that we no longer try to
                        // allocate any new general purpose registers for args
                        //
                        intArgRegNum = maxRegArgs;
                    }
                }
#else // not _TARGET_ARM_ or _TARGET_ARM64_

#if defined(UNIX_AMD64_ABI)

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                // Here a struct can be passed in register following the classifications of its members and size.
                // Now make sure there are actually enough registers to do so.
                if (isStructArg)
                {
                    unsigned int structFloatRegs = 0;
                    unsigned int structIntRegs   = 0;
                    for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
                    {
                        if (structDesc.IsIntegralSlot(i))
                        {
                            structIntRegs++;
                        }
                        else if (structDesc.IsSseSlot(i))
                        {
                            structFloatRegs++;
                        }
                    }

                    isRegArg = ((nextFltArgRegNum + structFloatRegs) <= MAX_FLOAT_REG_ARG) &&
                               ((intArgRegNum + structIntRegs) <= MAX_REG_ARG);
                }
                else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                {
                    if (passUsingFloatRegs)
                    {
                        isRegArg = nextFltArgRegNum < MAX_FLOAT_REG_ARG;
                    }
                    else
                    {
                        isRegArg = intArgRegNum < MAX_REG_ARG;
                    }
                }
#else  // !defined(UNIX_AMD64_ABI)
                isRegArg   = (intArgRegNum + (size - 1)) < maxRegArgs;
#endif // !defined(UNIX_AMD64_ABI)
#endif // _TARGET_ARM_
            }
            else
            {
                isRegArg = false;
            }

#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
            if (call->IsTailCallViaHelper())
            {
                // We have already (before calling fgMorphArgs()) appended the 4 special args
                // required by the x86 tailcall helper. These args are required to go on the
                // stack. Force them to the stack here.
                assert(numArgs >= 4);
                if (argIndex >= numArgs - 4)
                {
                    isRegArg = false;
                }
            }
#endif // defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)

        } // end !lateArgsComputed

        //
        // Now we know if the argument goes in registers or not and how big it is,
        // whether we had to just compute it or this is a re-morph call and we looked it up.
        //
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_ARM_
        // If we ever allocate a floating point argument to the stack, then all
        // subsequent HFA/float/double arguments go on the stack.
        if (!isRegArg && passUsingFloatRegs)
        {
            for (; fltArgRegNum < MAX_FLOAT_REG_ARG; ++fltArgRegNum)
            {
                fltArgSkippedRegMask |= genMapArgNumToRegMask(fltArgRegNum, TYP_FLOAT);
            }
        }

        // If we think we're going to split a struct between integer registers and the stack, check to
        // see if we've already assigned a floating-point arg to the stack.
        if (isRegArg &&                            // We decided above to use a register for the argument
            !passUsingFloatRegs &&                 // We're using integer registers
            (intArgRegNum + size > MAX_REG_ARG) && // We're going to split a struct type onto registers and stack
            anyFloatStackArgs)                     // We've already used the stack for a floating-point argument
        {
            isRegArg = false; // Change our mind; don't pass this struct partially in registers

            // Skip the rest of the integer argument registers
            for (; intArgRegNum < MAX_REG_ARG; ++intArgRegNum)
            {
                argSkippedRegMask |= genMapArgNumToRegMask(intArgRegNum, TYP_I_IMPL);
            }
        }

#endif // _TARGET_ARM_

        if (isRegArg)
        {
            regNumber nextRegNum = REG_STK;
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            regNumber    nextOtherRegNum = REG_STK;
            unsigned int structFloatRegs = 0;
            unsigned int structIntRegs   = 0;

            if (isStructArg && structDesc.passedInRegisters)
            {
                // It is a struct passed in registers. Assign the next available register.
                assert((structDesc.eightByteCount <= 2) && "Too many eightbytes.");
                regNumber* nextRegNumPtrs[2] = {&nextRegNum, &nextOtherRegNum};
                for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
                {
                    if (structDesc.IsIntegralSlot(i))
                    {
                        *nextRegNumPtrs[i] = genMapIntRegArgNumToRegNum(intArgRegNum + structIntRegs);
                        structIntRegs++;
                    }
                    else if (structDesc.IsSseSlot(i))
                    {
                        *nextRegNumPtrs[i] = genMapFloatRegArgNumToRegNum(nextFltArgRegNum + structFloatRegs);
                        structFloatRegs++;
                    }
                }
            }
            else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            {
                // fill in or update the argInfo table
                nextRegNum = passUsingFloatRegs ? genMapFloatRegArgNumToRegNum(nextFltArgRegNum)
                                                : genMapIntRegArgNumToRegNum(intArgRegNum);
            }

#ifdef _TARGET_AMD64_
#ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING
            assert(size == 1);
#endif
#endif

            fgArgTabEntryPtr newArgEntry;
            if (lateArgsComputed)
            {
                // This is a register argument - possibly update it in the table
                newArgEntry = call->fgArgInfo->RemorphRegArg(argIndex, argx, args, nextRegNum, size, argAlign);
            }
            else
            {
                bool isNonStandard = false;

#ifndef LEGACY_BACKEND
                // If there are nonstandard args (outside the calling convention) they were inserted above
                // and noted them in a table so we can recognize them here and build their argInfo.
                //
                // They should not affect the placement of any other args or stack space required.
                // Example: on AMD64 R10 and R11 are used for indirect VSD (generic interface) and cookie calls.
                isNonStandard = nonStandardArgs.FindReg(argx, &nextRegNum);
#endif // !LEGACY_BACKEND

                // This is a register argument - put it in the table
                newArgEntry = call->fgArgInfo->AddRegArg(argIndex, argx, args, nextRegNum, size, argAlign
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                                                         ,
                                                         isStructArg, nextOtherRegNum, &structDesc
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                                                         );

                newArgEntry->SetIsHfaRegArg(passUsingFloatRegs &&
                                            isHfaArg); // Note on Arm32 a HFA is passed in int regs for varargs
                newArgEntry->SetIsBackFilled(isBackFilled);
                newArgEntry->isNonStandard = isNonStandard;
            }

            if (newArgEntry->isNonStandard)
            {
                continue;
            }

            // Set up the next intArgRegNum and fltArgRegNum values.
            if (!isBackFilled)
            {
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                if (isStructArg)
                {
                    intArgRegNum += structIntRegs;
                    fltArgRegNum += structFloatRegs;
                }
                else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                {
                    if (passUsingFloatRegs)
                    {
                        fltArgRegNum += size;

#ifdef WINDOWS_AMD64_ABI
                        // Whenever we pass an integer register argument
                        // we skip the corresponding floating point register argument
                        intArgRegNum = min(intArgRegNum + size, MAX_REG_ARG);
#endif // WINDOWS_AMD64_ABI
#ifdef _TARGET_ARM_
                        if (fltArgRegNum > MAX_FLOAT_REG_ARG)
                        {
                            // This indicates a partial enregistration of a struct type
                            assert(varTypeIsStruct(argx));
                            unsigned numRegsPartial = size - (fltArgRegNum - MAX_FLOAT_REG_ARG);
                            assert((unsigned char)numRegsPartial == numRegsPartial);
                            call->fgArgInfo->SplitArg(argIndex, numRegsPartial, size - numRegsPartial);
                            fltArgRegNum = MAX_FLOAT_REG_ARG;
                        }
#endif // _TARGET_ARM_
                    }
                    else
                    {
                        if (hasFixedRetBuffReg() && (nextRegNum == theFixedRetBuffReg()))
                        {
                            // we are setting up the fixed return buffer register argument
                            // so don't increment intArgRegNum
                            assert(size == 1);
                        }
                        else
                        {
                            // Increment intArgRegNum by 'size' registers
                            intArgRegNum += size;
                        }

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI)
                        fltArgRegNum = min(fltArgRegNum + size, MAX_FLOAT_REG_ARG);
#endif // _TARGET_AMD64_
#ifdef _TARGET_ARM_
                        if (intArgRegNum > MAX_REG_ARG)
                        {
                            // This indicates a partial enregistration of a struct type
                            assert((isStructArg) || argx->OperIsCopyBlkOp() ||
                                   (argx->gtOper == GT_COMMA && (args->gtFlags & GTF_ASG)));
                            unsigned numRegsPartial = size - (intArgRegNum - MAX_REG_ARG);
                            assert((unsigned char)numRegsPartial == numRegsPartial);
                            call->fgArgInfo->SplitArg(argIndex, numRegsPartial, size - numRegsPartial);
                            intArgRegNum = MAX_REG_ARG;
                            fgPtrArgCntCur += size - numRegsPartial;
                        }
#endif // _TARGET_ARM_
                    }
                }
            }
        }
        else // We have an argument that is not passed in a register
        {
            fgPtrArgCntCur += size;

            // If the register arguments have not been determined then we must fill in the argInfo

            if (lateArgsComputed)
            {
                // This is a stack argument - possibly update it in the table
                call->fgArgInfo->RemorphStkArg(argIndex, argx, args, size, argAlign);
            }
            else
            {
                // This is a stack argument - put it in the table
                call->fgArgInfo->AddStkArg(argIndex, argx, args, size,
                                           argAlign FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(isStructArg));
            }
        }

        if (copyBlkClass != NO_CLASS_HANDLE)
        {
            noway_assert(!lateArgsComputed);
            fgMakeOutgoingStructArgCopy(call, args, argIndex,
                                        copyBlkClass FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(&structDesc));

            // This can cause a GTF_EXCEPT flag to be set.
            // TODO-CQ: Fix the cases where this happens. We shouldn't be adding any new flags.
            // This currently occurs in the case where we are re-morphing the args on x86/RyuJIT, and
            // there are no register arguments. Then lateArgsComputed is never true, so we keep re-copying
            // any struct arguments.
            // i.e. assert(((call->gtFlags & GTF_EXCEPT) != 0) || ((args->Current()->gtFlags & GTF_EXCEPT) == 0)
            flagsSummary |= (args->Current()->gtFlags & GTF_EXCEPT);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            hasStackArgCopy = true;
#endif
        }

#ifndef LEGACY_BACKEND
        if (argx->gtOper == GT_MKREFANY)
        {
            NYI_X86("MKREFANY");

            // 'Lower' the MKREFANY tree and insert it.
            noway_assert(!lateArgsComputed);

            // Get a new temp
            // Here we don't need unsafe value cls check since the addr of temp is used only in mkrefany
            unsigned tmp = lvaGrabTemp(true DEBUGARG("by-value mkrefany struct argument"));
            lvaSetStruct(tmp, impGetRefAnyClass(), false);

            // Build the mkrefany as a comma node:
            // (tmp.ptr=argx),(tmp.type=handle)
            GenTreeLclFld* destPtrSlot  = gtNewLclFldNode(tmp, TYP_I_IMPL, offsetof(CORINFO_RefAny, dataPtr));
            GenTreeLclFld* destTypeSlot = gtNewLclFldNode(tmp, TYP_I_IMPL, offsetof(CORINFO_RefAny, type));
            destPtrSlot->gtFieldSeq     = GetFieldSeqStore()->CreateSingleton(GetRefanyDataField());
            destPtrSlot->gtFlags |= GTF_VAR_DEF;
            destTypeSlot->gtFieldSeq = GetFieldSeqStore()->CreateSingleton(GetRefanyTypeField());
            destTypeSlot->gtFlags |= GTF_VAR_DEF;

            GenTreePtr asgPtrSlot  = gtNewAssignNode(destPtrSlot, argx->gtOp.gtOp1);
            GenTreePtr asgTypeSlot = gtNewAssignNode(destTypeSlot, argx->gtOp.gtOp2);
            GenTreePtr asg         = gtNewOperNode(GT_COMMA, TYP_VOID, asgPtrSlot, asgTypeSlot);

            // Change the expression to "(tmp=val)"
            args->gtOp.gtOp1 = asg;

            // EvalArgsToTemps will cause tmp to actually get loaded as the argument
            call->fgArgInfo->EvalToTmp(argIndex, tmp, asg);
            lvaSetVarAddrExposed(tmp);
        }
#endif // !LEGACY_BACKEND

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (isStructArg && !isRegArg)
        {
            nonRegPassedStructSlots += size;
        }
        else
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
        {
            argSlots += size;
        }
    } // end foreach argument loop

    if (!lateArgsComputed)
    {
        call->fgArgInfo->ArgsComplete();
#ifdef LEGACY_BACKEND
        call->gtCallRegUsedMask = genIntAllRegArgMask(intArgRegNum);
#if defined(_TARGET_ARM_)
        call->gtCallRegUsedMask &= ~argSkippedRegMask;
#endif
        if (fltArgRegNum > 0)
        {
#if defined(_TARGET_ARM_)
            call->gtCallRegUsedMask |= genFltAllRegArgMask(fltArgRegNum) & ~fltArgSkippedRegMask;
#endif
        }
#endif // LEGACY_BACKEND
    }

    if (call->gtCallArgs)
    {
        UpdateGT_LISTFlags(call->gtCallArgs);
    }

    /* Process the function address, if indirect call */

    if (call->gtCallType == CT_INDIRECT)
    {
        call->gtCallAddr = fgMorphTree(call->gtCallAddr);
    }

    call->fgArgInfo->RecordStkLevel(fgPtrArgCntCur);

    if ((call->gtCallType == CT_INDIRECT) && (call->gtCallCookie != nullptr))
    {
        fgPtrArgCntCur++;
    }

    /* Remember the maximum value we ever see */

    if (fgPtrArgCntMax < fgPtrArgCntCur)
    {
        fgPtrArgCntMax = fgPtrArgCntCur;
    }

    /* The call will pop all the arguments we pushed */

    fgPtrArgCntCur = genPtrArgCntSav;

#if FEATURE_FIXED_OUT_ARGS

    // Update the outgoing argument size.
    // If the call is a fast tail call, it will setup its arguments in incoming arg
    // area instead of the out-going arg area.  Therefore, don't consider fast tail
    // calls to update lvaOutgoingArgSpaceSize.
    if (!call->IsFastTailCall())
    {
        unsigned preallocatedArgCount = call->fgArgInfo->GetNextSlotNum();

#if defined(UNIX_AMD64_ABI)
        opts.compNeedToAlignFrame = true; // this is currently required for the UNIX ABI to work correctly

        // ToDo: Remove this re-calculation preallocatedArgCount and use the value assigned above.

        // First slots go in registers only, no stack needed.
        // TODO-Amd64-Unix-CQ This calculation is only accurate for integer arguments,
        // and ignores floating point args (it is overly conservative in that case).
        preallocatedArgCount = nonRegPassedStructSlots;
        if (argSlots > MAX_REG_ARG)
        {
            preallocatedArgCount += argSlots - MAX_REG_ARG;
        }
#endif // UNIX_AMD64_ABI

        // Check if we need to increase the size of our Outgoing Arg Space
        if (preallocatedArgCount * REGSIZE_BYTES > lvaOutgoingArgSpaceSize)
        {
            lvaOutgoingArgSpaceSize = preallocatedArgCount * REGSIZE_BYTES;

            // If a function has localloc, we will need to move the outgoing arg space when the
            // localloc happens. When we do this, we need to maintain stack alignment. To avoid
            // leaving alignment-related holes when doing this move, make sure the outgoing
            // argument space size is a multiple of the stack alignment by aligning up to the next
            // stack alignment boundary.
            if (compLocallocUsed)
            {
                lvaOutgoingArgSpaceSize = (unsigned)roundUp(lvaOutgoingArgSpaceSize, STACK_ALIGN);
            }
        }
#ifdef DEBUG
        if (verbose)
        {
            printf("argSlots=%d, preallocatedArgCount=%d, nextSlotNum=%d, lvaOutgoingArgSpaceSize=%d\n", argSlots,
                   preallocatedArgCount, call->fgArgInfo->GetNextSlotNum(), lvaOutgoingArgSpaceSize);
        }
#endif
    }
#endif // FEATURE_FIXED_OUT_ARGS

    /* Update the 'side effect' flags value for the call */

    call->gtFlags |= (flagsSummary & GTF_ALL_EFFECT);

    // If the register arguments have already been determined
    // or we have no register arguments then we don't need to
    // call SortArgs() and EvalArgsToTemps()
    //
    // For UNIX_AMD64, the condition without hasStackArgCopy cannot catch
    // all cases of fgMakeOutgoingStructArgCopy() being called. hasStackArgCopy
    // is added to make sure to call EvalArgsToTemp.
    if (!lateArgsComputed && (call->fgArgInfo->HasRegArgs()
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                              || hasStackArgCopy
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
                              ))
    {
        // This is the first time that we morph this call AND it has register arguments.
        // Follow into the code below and do the 'defer or eval to temp' analysis.

        call->fgArgInfo->SortArgs();

        call->fgArgInfo->EvalArgsToTemps();

        // We may have updated the arguments
        if (call->gtCallArgs)
        {
            UpdateGT_LISTFlags(call->gtCallArgs);
        }
    }

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

    // Rewrite the struct args to be passed by value on stack or in registers.
    fgMorphSystemVStructArgs(call, hasStructArgument);

#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifndef LEGACY_BACKEND
    // In the future we can migrate UNIX_AMD64 to use this
    // method instead of fgMorphSystemVStructArgs

    // We only build GT_LISTs for MultiReg structs for the RyuJIT backend
    if (hasMultiregStructArgs)
    {
        fgMorphMultiregStructArgs(call);
    }
#endif // LEGACY_BACKEND

#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef DEBUG
    if (verbose)
    {
        fgArgInfoPtr argInfo = call->fgArgInfo;
        for (unsigned curInx = 0; curInx < argInfo->ArgCount(); curInx++)
        {
            fgArgTabEntryPtr curArgEntry = argInfo->ArgTable()[curInx];
            curArgEntry->Dump();
        }
    }
#endif

    return call;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
// fgMorphSystemVStructArgs:
//   Rewrite the struct args to be passed by value on stack or in registers.
//
// args:
//   call: The call whose arguments need to be morphed.
//   hasStructArgument: Whether this call has struct arguments.
//
void Compiler::fgMorphSystemVStructArgs(GenTreeCall* call, bool hasStructArgument)
{
    unsigned   flagsSummary = 0;
    GenTreePtr args;
    GenTreePtr argx;

    if (hasStructArgument)
    {
        fgArgInfoPtr allArgInfo = call->fgArgInfo;

        for (args = call->gtCallArgs; args != nullptr; args = args->gtOp.gtOp2)
        {
            // For late arguments the arg tree that is overridden is in the gtCallLateArgs list.
            // For such late args the gtCallArgList contains the setup arg node (evaluating the arg.)
            // The tree from the gtCallLateArgs list is passed to the callee. The fgArgEntry node contains the mapping
            // between the nodes in both lists. If the arg is not a late arg, the fgArgEntry->node points to itself,
            // otherwise points to the list in the late args list.
            bool             isLateArg  = (args->gtOp.gtOp1->gtFlags & GTF_LATE_ARG) != 0;
            fgArgTabEntryPtr fgEntryPtr = gtArgEntryByNode(call, args->gtOp.gtOp1);
            assert(fgEntryPtr != nullptr);
            GenTreePtr argx     = fgEntryPtr->node;
            GenTreePtr lateList = nullptr;
            GenTreePtr lateNode = nullptr;

            if (isLateArg)
            {
                for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
                {
                    assert(list->IsList());

                    GenTreePtr argNode = list->Current();
                    if (argx == argNode)
                    {
                        lateList = list;
                        lateNode = argNode;
                        break;
                    }
                }
                assert(lateList != nullptr && lateNode != nullptr);
            }
            GenTreePtr arg            = argx;
            bool       argListCreated = false;

            var_types type = arg->TypeGet();

            if (varTypeIsStruct(type))
            {
                var_types originalType = type;
                // If we have already processed the arg...
                if (arg->OperGet() == GT_LIST && varTypeIsStruct(arg))
                {
                    continue;
                }

                // If already OBJ it is set properly already.
                if (arg->OperGet() == GT_OBJ)
                {
                    assert(!fgEntryPtr->structDesc.passedInRegisters);
                    continue;
                }

                assert(arg->OperGet() == GT_LCL_VAR || arg->OperGet() == GT_LCL_FLD ||
                       (arg->OperGet() == GT_ADDR &&
                        (arg->gtOp.gtOp1->OperGet() == GT_LCL_FLD || arg->gtOp.gtOp1->OperGet() == GT_LCL_VAR)));

                GenTreeLclVarCommon* lclCommon =
                    arg->OperGet() == GT_ADDR ? arg->gtOp.gtOp1->AsLclVarCommon() : arg->AsLclVarCommon();
                if (fgEntryPtr->structDesc.passedInRegisters)
                {
                    if (fgEntryPtr->structDesc.eightByteCount == 1)
                    {
                        // Change the type and below the code will change the LclVar to a LCL_FLD
                        type = GetTypeFromClassificationAndSizes(fgEntryPtr->structDesc.eightByteClassifications[0],
                                                                 fgEntryPtr->structDesc.eightByteSizes[0]);
                    }
                    else if (fgEntryPtr->structDesc.eightByteCount == 2)
                    {
                        // Create LCL_FLD for each eightbyte.
                        argListCreated = true;

                        // Second eightbyte.
                        GenTreeLclFld* newLclField = new (this, GT_LCL_FLD)
                            GenTreeLclFld(GetTypeFromClassificationAndSizes(fgEntryPtr->structDesc
                                                                                .eightByteClassifications[1],
                                                                            fgEntryPtr->structDesc.eightByteSizes[1]),
                                          lclCommon->gtLclNum, fgEntryPtr->structDesc.eightByteOffsets[1]);
                        // Note this should actually be: secondNode = gtNewArgList(newLclField)
                        GenTreeArgList* secondNode = gtNewListNode(newLclField, nullptr);
                        secondNode->gtType         = originalType; // Preserve the type. It is a special case.
                        newLclField->gtFieldSeq    = FieldSeqStore::NotAField();

                        // First field
                        arg->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
                        arg->gtType =
                            GetTypeFromClassificationAndSizes(fgEntryPtr->structDesc.eightByteClassifications[0],
                                                              fgEntryPtr->structDesc.eightByteSizes[0]);
                        arg         = gtNewListNode(arg, secondNode);
                        arg->gtType = type; // Preserve the type. It is a special case.
                    }
                    else
                    {
                        assert(false && "More than two eightbytes detected for CLR."); // No more than two eightbytes
                                                                                       // for the CLR.
                    }
                }

                // If we didn't change the type of the struct, it means
                // its classification doesn't support to be passed directly through a
                // register, so we need to pass a pointer to the destination where
                // where we copied the struct to.
                if (!argListCreated)
                {
                    if (fgEntryPtr->structDesc.passedInRegisters)
                    {
                        arg->gtType = type;
                    }
                    else
                    {
                        // Make sure this is an addr node.
                        if (arg->OperGet() != GT_ADDR && arg->OperGet() != GT_LCL_VAR_ADDR)
                        {
                            arg = gtNewOperNode(GT_ADDR, TYP_I_IMPL, arg);
                        }

                        assert(arg->OperGet() == GT_ADDR || arg->OperGet() == GT_LCL_VAR_ADDR);

                        // Create an Obj of the temp to use it as a call argument.
                        arg = new (this, GT_OBJ) GenTreeObj(originalType, arg, lvaGetStruct(lclCommon->gtLclNum));
                    }
                }
            }

            if (argx != arg)
            {
                bool             isLateArg  = (args->gtOp.gtOp1->gtFlags & GTF_LATE_ARG) != 0;
                fgArgTabEntryPtr fgEntryPtr = gtArgEntryByNode(call, args->gtOp.gtOp1);
                assert(fgEntryPtr != nullptr);
                GenTreePtr argx     = fgEntryPtr->node;
                GenTreePtr lateList = nullptr;
                GenTreePtr lateNode = nullptr;
                if (isLateArg)
                {
                    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
                    {
                        assert(list->IsList());

                        GenTreePtr argNode = list->Current();
                        if (argx == argNode)
                        {
                            lateList = list;
                            lateNode = argNode;
                            break;
                        }
                    }
                    assert(lateList != nullptr && lateNode != nullptr);
                }

                fgEntryPtr->node = arg;
                if (isLateArg)
                {
                    lateList->gtOp.gtOp1 = arg;
                }
                else
                {
                    args->gtOp.gtOp1 = arg;
                }
            }
        }
    }

    // Update the flags
    call->gtFlags |= (flagsSummary & GTF_ALL_EFFECT);
}
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

//-----------------------------------------------------------------------------
// fgMorphMultiregStructArgs:  Locate the TYP_STRUCT arguments and
//                             call fgMorphMultiregStructArg on each of them.
//
// Arguments:
//    call:    a GenTreeCall node that has one or more TYP_STRUCT arguments
//
// Notes:
//    We only call fgMorphMultiregStructArg for the register passed TYP_STRUCT arguments.
//    The call to fgMorphMultiregStructArg will mutate the argument into the GT_LIST form
//    whicj is only used for register arguments.
//    If this method fails to find any TYP_STRUCT arguments it will assert.
//
void Compiler::fgMorphMultiregStructArgs(GenTreeCall* call)
{
    GenTreePtr   args;
    GenTreePtr   argx;
    bool         foundStructArg = false;
    unsigned     initialFlags   = call->gtFlags;
    unsigned     flagsSummary   = 0;
    fgArgInfoPtr allArgInfo     = call->fgArgInfo;

    // Currently only ARM64 is using this method to morph the MultiReg struct args
    //  in the future AMD64_UNIX and for HFAs ARM32, will also use this method
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_ARM_
    NYI_ARM("fgMorphMultiregStructArgs");
#endif
#ifdef _TARGET_X86_
    assert(!"Logic error: no MultiregStructArgs for X86");
#endif
#ifdef _TARGET_AMD64_
#if defined(UNIX_AMD64_ABI)
    NYI_AMD64("fgMorphMultiregStructArgs (UNIX ABI)");
#else  // WINDOWS_AMD64_ABI
    assert(!"Logic error: no MultiregStructArgs for Windows X64 ABI");
#endif // !UNIX_AMD64_ABI
#endif

    for (args = call->gtCallArgs; args != nullptr; args = args->gtOp.gtOp2)
    {
        // For late arguments the arg tree that is overridden is in the gtCallLateArgs list.
        // For such late args the gtCallArgList contains the setup arg node (evaluating the arg.)
        // The tree from the gtCallLateArgs list is passed to the callee. The fgArgEntry node contains the mapping
        // between the nodes in both lists. If the arg is not a late arg, the fgArgEntry->node points to itself,
        // otherwise points to the list in the late args list.
        bool             isLateArg  = (args->gtOp.gtOp1->gtFlags & GTF_LATE_ARG) != 0;
        fgArgTabEntryPtr fgEntryPtr = gtArgEntryByNode(call, args->gtOp.gtOp1);
        assert(fgEntryPtr != nullptr);
        GenTreePtr argx     = fgEntryPtr->node;
        GenTreePtr lateList = nullptr;
        GenTreePtr lateNode = nullptr;

        if (isLateArg)
        {
            for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
            {
                assert(list->IsList());

                GenTreePtr argNode = list->Current();
                if (argx == argNode)
                {
                    lateList = list;
                    lateNode = argNode;
                    break;
                }
            }
            assert(lateList != nullptr && lateNode != nullptr);
        }

        GenTreePtr arg = argx;

        if (arg->TypeGet() == TYP_STRUCT)
        {
            foundStructArg = true;

            arg = fgMorphMultiregStructArg(arg, fgEntryPtr);

            // Did we replace 'argx' with a new tree?
            if (arg != argx)
            {
                fgEntryPtr->node = arg; // Record the new value for the arg in the fgEntryPtr->node

                // link the new arg node into either the late arg list or the gtCallArgs list
                if (isLateArg)
                {
                    lateList->gtOp.gtOp1 = arg;
                }
                else
                {
                    args->gtOp.gtOp1 = arg;
                }
            }
        }
    }

    // We should only call this method when we actually have one or more multireg struct args
    assert(foundStructArg);

    // Update the flags
    call->gtFlags |= (flagsSummary & GTF_ALL_EFFECT);
}

//-----------------------------------------------------------------------------
// fgMorphMultiregStructArg:  Given a multireg TYP_STRUCT arg from a call argument list
//   Morph the argument into a set of GT_LIST nodes.
//
// Arguments:
//     arg        - A GenTree node containing a TYP_STRUCT arg that
//                  is to be passed in multiple registers
//     fgEntryPtr - the fgArgTabEntry information for the current 'arg'
//
// Notes:
//    arg must be a GT_OBJ or GT_LCL_VAR or GT_LCL_FLD of TYP_STRUCT that is suitable
//    for passing in multiple registers.
//    If arg is a LclVar we check if it is struct promoted and has the right number of fields
//    and if they are at the appropriate offsets we will use the struct promted fields
//    in the GT_LIST nodes that we create.
//    If we have a GT_LCL_VAR that isn't struct promoted or doesn't meet the requirements
//    we will use a set of GT_LCL_FLDs nodes to access the various portions of the struct
//    this also forces the struct to be stack allocated into the local frame.
//    For the GT_OBJ case will clone the address expression and generate two (or more)
//    indirections.
//    Currently the implementation only handles ARM64 and will NYI for other architectures.
//
GenTreePtr Compiler::fgMorphMultiregStructArg(GenTreePtr arg, fgArgTabEntryPtr fgEntryPtr)
{
    assert(arg->TypeGet() == TYP_STRUCT);

#ifndef _TARGET_ARM64_
    NYI("fgMorphMultiregStructArg requires implementation for this target");
#endif

#if FEATURE_MULTIREG_ARGS
    // Examine 'arg' and setup argValue objClass and structSize
    //
    CORINFO_CLASS_HANDLE objClass   = NO_CLASS_HANDLE;
    GenTreePtr           argValue   = arg; // normally argValue will be arg, but see right below
    unsigned             structSize = 0;

    if (arg->OperGet() == GT_OBJ)
    {
        GenTreeObj* argObj = arg->AsObj();
        objClass           = argObj->gtClass;
        structSize         = info.compCompHnd->getClassSize(objClass);

        // If we have a GT_OBJ of a GT_ADDR then we set argValue to the child node of the GT_ADDR
        //
        if (argObj->gtOp1->OperGet() == GT_ADDR)
        {
            argValue = argObj->gtOp1->gtOp.gtOp1;
        }
    }
    else if (arg->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* varNode = arg->AsLclVarCommon();
        unsigned             varNum  = varNode->gtLclNum;
        assert(varNum < lvaCount);
        LclVarDsc* varDsc = &lvaTable[varNum];

        objClass   = lvaGetStruct(varNum);
        structSize = varDsc->lvExactSize;
    }
    noway_assert(objClass != nullptr);

    var_types hfaType                 = TYP_UNDEF;
    var_types elemType                = TYP_UNDEF;
    unsigned  elemCount               = 0;
    unsigned  elemSize                = 0;
    var_types type[MAX_ARG_REG_COUNT] = {}; // TYP_UNDEF = 0

    hfaType = GetHfaType(objClass); // set to float or double if it is an HFA, otherwise TYP_UNDEF
    if (varTypeIsFloating(hfaType))
    {
        elemType  = hfaType;
        elemSize  = genTypeSize(elemType);
        elemCount = structSize / elemSize;
        assert(elemSize * elemCount == structSize);
        for (unsigned inx = 0; inx < elemCount; inx++)
        {
            type[inx] = elemType;
        }
    }
    else
    {
        assert(structSize <= 2 * TARGET_POINTER_SIZE);
        BYTE gcPtrs[2] = {TYPE_GC_NONE, TYPE_GC_NONE};
        info.compCompHnd->getClassGClayout(objClass, &gcPtrs[0]);
        elemCount = 2;
        type[0]   = getJitGCType(gcPtrs[0]);
        type[1]   = getJitGCType(gcPtrs[1]);

        if ((argValue->OperGet() == GT_LCL_FLD) || (argValue->OperGet() == GT_LCL_VAR))
        {
            // We can safely widen this to 16 bytes since we are loading from
            // a GT_LCL_VAR or a GT_LCL_FLD which is properly padded and
            // lives in the stack frame or will be a promoted field.
            //
            elemSize   = TARGET_POINTER_SIZE;
            structSize = 2 * TARGET_POINTER_SIZE;
        }
        else // we must have a GT_OBJ
        {
            assert(argValue->OperGet() == GT_OBJ);

            // We need to load the struct from an arbitrary address
            // and we can't read past the end of the structSize
            // We adjust the second load type here
            //
            if (structSize < 2 * TARGET_POINTER_SIZE)
            {
                switch (structSize - TARGET_POINTER_SIZE)
                {
                    case 1:
                        type[1] = TYP_BYTE;
                        break;
                    case 2:
                        type[1] = TYP_SHORT;
                        break;
                    case 4:
                        type[1] = TYP_INT;
                        break;
                    default:
                        noway_assert(!"NYI: odd sized struct in fgMorphMultiregStructArg");
                        break;
                }
            }
        }
    }
    // We should still have a TYP_STRUCT
    assert(argValue->TypeGet() == TYP_STRUCT);

    GenTreeArgList* newArg = nullptr;

    // Are we passing a struct LclVar?
    //
    if (argValue->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* varNode = argValue->AsLclVarCommon();
        unsigned             varNum  = varNode->gtLclNum;
        assert(varNum < lvaCount);
        LclVarDsc* varDsc = &lvaTable[varNum];

        // At this point any TYP_STRUCT LclVar must be a 16-byte struct
        // or an HFA struct, both which are passed by value.
        //
        assert((varDsc->lvSize() == 2 * TARGET_POINTER_SIZE) || varDsc->lvIsHfa());

        varDsc->lvIsMultiRegArg = true;

#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("Multireg struct argument V%02u : ");
            fgEntryPtr->Dump();
        }
#endif // DEBUG

        // This local variable must match the layout of the 'objClass' type exactly
        if (varDsc->lvIsHfa())
        {
            // We have a HFA struct
            noway_assert(elemType == (varDsc->lvHfaTypeIsFloat() ? TYP_FLOAT : TYP_DOUBLE));
            noway_assert(elemSize == genTypeSize(elemType));
            noway_assert(elemCount == (varDsc->lvExactSize / elemSize));
            noway_assert(elemSize * elemCount == varDsc->lvExactSize);

            for (unsigned inx = 0; (inx < elemCount); inx++)
            {
                noway_assert(type[inx] == elemType);
            }
        }
        else
        {
            // We must have a 16-byte struct (non-HFA)
            noway_assert(elemCount == 2);

            for (unsigned inx = 0; inx < elemCount; inx++)
            {
                CorInfoGCType currentGcLayoutType = (CorInfoGCType)varDsc->lvGcLayout[inx];

                // We setup the type[inx] value above using the GC info from 'objClass'
                // This GT_LCL_VAR must have the same GC layout info
                //
                if (currentGcLayoutType != TYPE_GC_NONE)
                {
                    noway_assert(type[inx] == getJitGCType((BYTE)currentGcLayoutType));
                }
                else
                {
                    // We may have use a small type when we setup the type[inx] values above
                    // We can safely widen this to TYP_I_IMPL
                    type[inx] = TYP_I_IMPL;
                }
            }
        }

        // Is this LclVar a promoted struct with exactly 2 fields?
        // TODO-ARM64-CQ: Support struct promoted HFA types here
        if (varDsc->lvPromoted && (varDsc->lvFieldCnt == 2) && !varDsc->lvIsHfa())
        {
            // See if we have two promoted fields that start at offset 0 and 8?
            unsigned loVarNum = lvaGetFieldLocal(varDsc, 0);
            unsigned hiVarNum = lvaGetFieldLocal(varDsc, TARGET_POINTER_SIZE);

            // Did we find the promoted fields at the necessary offsets?
            if ((loVarNum != BAD_VAR_NUM) && (hiVarNum != BAD_VAR_NUM))
            {
                LclVarDsc* loVarDsc = &lvaTable[loVarNum];
                LclVarDsc* hiVarDsc = &lvaTable[hiVarNum];

                var_types loType = loVarDsc->lvType;
                var_types hiType = hiVarDsc->lvType;

                if (varTypeIsFloating(loType) || varTypeIsFloating(hiType))
                {
                    // TODO-LSRA - It currently doesn't support the passing of floating point LCL_VARS in the integer
                    // registers. So for now we will use GT_LCLFLD's to pass this struct (it won't be enregistered)
                    //
                    JITDUMP("Multireg struct V%02u will be passed using GT_LCLFLD because it has float fields.\n",
                            varNum);
                    //
                    // we call lvaSetVarDoNotEnregister and do the proper transformation below.
                    //
                }
                else
                {
                    // We can use the struct promoted field as the two arguments

                    GenTreePtr loLclVar = gtNewLclvNode(loVarNum, loType, loVarNum);
                    GenTreePtr hiLclVar = gtNewLclvNode(hiVarNum, hiType, hiVarNum);

                    // Create a new tree for 'arg'
                    //    replace the existing LDOBJ(ADDR(LCLVAR))
                    //    with a LIST(LCLVAR-LO, LIST(LCLVAR-HI, nullptr))
                    //
                    newArg = gtNewListNode(loLclVar, gtNewArgList(hiLclVar));
                }
            }
        }
        else
        {
            //
            // We will create a list of GT_LCL_FLDs nodes to pass this struct
            //
            lvaSetVarDoNotEnregister(varNum DEBUG_ARG(DNER_LocalField));
        }
    }

    // If we didn't set newarg to a new List Node tree
    //
    if (newArg == nullptr)
    {
        if (fgEntryPtr->regNum == REG_STK)
        {
            // We leave this stack passed argument alone
            return arg;
        }

        // Are we passing a GT_LCL_FLD (or a GT_LCL_VAR that was not struct promoted )
        // A GT_LCL_FLD could also contain a 16-byte struct or HFA struct inside it?
        //
        if ((argValue->OperGet() == GT_LCL_FLD) || (argValue->OperGet() == GT_LCL_VAR))
        {
            GenTreeLclVarCommon* varNode = argValue->AsLclVarCommon();
            unsigned             varNum  = varNode->gtLclNum;
            assert(varNum < lvaCount);
            LclVarDsc* varDsc = &lvaTable[varNum];

            unsigned baseOffset = (argValue->OperGet() == GT_LCL_FLD) ? argValue->gtLclFld.gtLclOffs : 0;
            unsigned lastOffset = baseOffset + (elemCount * elemSize);

            // The allocated size of our LocalVar must be at least as big as lastOffset
            assert(varDsc->lvSize() >= lastOffset);

            if (varDsc->lvStructGcCount > 0)
            {
                // alignment of the baseOffset is required
                noway_assert((baseOffset % TARGET_POINTER_SIZE) == 0);
                noway_assert(elemSize == TARGET_POINTER_SIZE);
                unsigned    baseIndex = baseOffset / TARGET_POINTER_SIZE;
                const BYTE* gcPtrs    = varDsc->lvGcLayout; // Get the GC layout for the local variable
                for (unsigned inx = 0; (inx < elemCount); inx++)
                {
                    // The GC information must match what we setup using 'objClass'
                    noway_assert(type[inx] == getJitGCType(gcPtrs[baseIndex + inx]));
                }
            }
            else //  this varDsc contains no GC pointers
            {
                for (unsigned inx = 0; inx < elemCount; inx++)
                {
                    // The GC information must match what we setup using 'objClass'
                    noway_assert(!varTypeIsGC(type[inx]));
                }
            }

            //
            // We create a list of GT_LCL_FLDs nodes to pass this struct
            //
            lvaSetVarDoNotEnregister(varNum DEBUG_ARG(DNER_LocalField));

            // Start building our list from the last element
            unsigned offset = lastOffset;
            unsigned inx    = elemCount;

            // Create a new tree for 'arg'
            //    replace the existing LDOBJ(ADDR(LCLVAR))
            //    with a LIST(LCLFLD-LO, LIST(LCLFLD-HI, nullptr) ...)
            //
            while (inx > 0)
            {
                inx--;
                offset -= elemSize;
                GenTreePtr nextLclFld = gtNewLclFldNode(varNum, type[inx], offset);
                if (newArg == nullptr)
                {
                    newArg = gtNewArgList(nextLclFld);
                }
                else
                {
                    newArg = gtNewListNode(nextLclFld, newArg);
                }
            }
        }
        // Are we passing a GT_OBJ struct?
        //
        else if (argValue->OperGet() == GT_OBJ)
        {
            GenTreeObj* argObj   = argValue->AsObj();
            GenTreePtr  baseAddr = argObj->gtOp1;
            var_types   addrType = baseAddr->TypeGet();

            // Create a new tree for 'arg'
            //    replace the existing LDOBJ(EXPR)
            //    with a LIST(IND(EXPR), LIST(IND(EXPR+8), nullptr) ...)
            //

            // Start building our list from the last element
            unsigned offset = structSize;
            unsigned inx    = elemCount;
            while (inx > 0)
            {
                inx--;
                elemSize = genTypeSize(type[inx]);
                offset -= elemSize;
                GenTreePtr curAddr = baseAddr;
                if (offset != 0)
                {
                    GenTreePtr baseAddrDup = gtCloneExpr(baseAddr);
                    noway_assert(baseAddrDup != nullptr);
                    curAddr = gtNewOperNode(GT_ADD, addrType, baseAddrDup, gtNewIconNode(offset, TYP_I_IMPL));
                }
                else
                {
                    curAddr = baseAddr;
                }
                GenTreePtr curItem = gtNewOperNode(GT_IND, type[inx], curAddr);
                if (newArg == nullptr)
                {
                    newArg = gtNewArgList(curItem);
                }
                else
                {
                    newArg = gtNewListNode(curItem, newArg);
                }
            }
        }
    }

    // If we reach here we should have set newArg to something
    if (newArg == nullptr)
    {
#ifdef DEBUG
        gtDispTree(argValue);
#endif
        assert(!"Missing case in fgMorphMultiregStructArg");
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("fgMorphMultiregStructArg created tree:\n");
        gtDispTree(newArg);
    }
#endif

    arg = newArg; // consider calling fgMorphTree(newArg);

#endif // FEATURE_MULTIREG_ARGS

    return arg;
}

// Make a copy of a struct variable if necessary, to pass to a callee.
// returns: tree that computes address of the outgoing arg
void Compiler::fgMakeOutgoingStructArgCopy(
    GenTreeCall*         call,
    GenTree*             args,
    unsigned             argIndex,
    CORINFO_CLASS_HANDLE copyBlkClass FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(
        const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* const structDescPtr))
{
    GenTree* argx = args->Current();
    noway_assert(argx->gtOper != GT_MKREFANY);
    // See if we need to insert a copy at all
    // Case 1: don't need a copy if it is the last use of a local.  We can't determine that all of the time
    // but if there is only one use and no loops, the use must be last.
    GenTreeLclVarCommon* lcl = nullptr;
    if ((argx->OperGet() == GT_OBJ) && argx->AsObj()->Addr()->OperIsLocal())
    {
        lcl = argx->AsObj()->Addr()->AsLclVarCommon();
    }
    if (lcl != nullptr)
    {
        unsigned varNum = lcl->AsLclVarCommon()->GetLclNum();
        if (lvaIsImplicitByRefLocal(varNum))
        {
            LclVarDsc* varDsc = &lvaTable[varNum];
            // JIT_TailCall helper has an implicit assumption that all tail call arguments live
            // on the caller's frame. If an argument lives on the caller caller's frame, it may get
            // overwritten if that frame is reused for the tail call. Therefore, we should always copy
            // struct parameters if they are passed as arguments to a tail call.
            if (!call->IsTailCallViaHelper() && (varDsc->lvRefCnt == 1) && !fgMightHaveLoop())
            {
                varDsc->lvRefCnt    = 0;
                args->gtOp.gtOp1    = lcl;
                fgArgTabEntryPtr fp = Compiler::gtArgEntryByNode(call, argx);
                fp->node            = lcl;

                JITDUMP("did not have to make outgoing copy for V%2d", varNum);
                return;
            }
        }
    }

    if (fgOutgoingArgTemps == nullptr)
    {
        fgOutgoingArgTemps = hashBv::Create(this);
    }

    unsigned tmp   = 0;
    bool     found = false;

    // Attempt to find a local we have already used for an outgoing struct and reuse it.
    // We do not reuse within a statement.
    if (!opts.MinOpts())
    {
        indexType lclNum;
        FOREACH_HBV_BIT_SET(lclNum, fgOutgoingArgTemps)
        {
            LclVarDsc* varDsc = &lvaTable[lclNum];
            if (typeInfo::AreEquivalent(varDsc->lvVerTypeInfo, typeInfo(TI_STRUCT, copyBlkClass)) &&
                !fgCurrentlyInUseArgTemps->testBit(lclNum))
            {
                tmp   = (unsigned)lclNum;
                found = true;
                JITDUMP("reusing outgoing struct arg");
                break;
            }
        }
        NEXT_HBV_BIT_SET;
    }

    // Create the CopyBlk tree and insert it.
    if (!found)
    {
        // Get a new temp
        // Here We don't need unsafe value cls check, since the addr of this temp is used only in copyblk.
        tmp = lvaGrabTemp(true DEBUGARG("by-value struct argument"));
        lvaSetStruct(tmp, copyBlkClass, false);
        fgOutgoingArgTemps->setBit(tmp);
    }

    fgCurrentlyInUseArgTemps->setBit(tmp);

    // TYP_SIMD structs should not be enregistered, since ABI requires it to be
    // allocated on stack and address of it needs to be passed.
    if (lclVarIsSIMDType(tmp))
    {
        lvaSetVarDoNotEnregister(tmp DEBUGARG(DNER_IsStruct));
    }

    // Create a reference to the temp
    GenTreePtr dest = gtNewLclvNode(tmp, lvaTable[tmp].lvType);
    dest->gtFlags |= (GTF_DONT_CSE | GTF_VAR_DEF); // This is a def of the local, "entire" by construction.
    dest = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);

    // TODO-Cleanup: This probably shouldn't be done here because arg morphing is done prior
    // to ref counting of the lclVars.
    lvaTable[tmp].incRefCnts(compCurBB->getBBWeight(this), this);

    GenTreePtr src;
    if (argx->gtOper == GT_OBJ)
    {
        src = argx->gtOp.gtOp1;
    }
    else
    {
        argx->gtFlags |= GTF_DONT_CSE;
        src = gtNewOperNode(GT_ADDR, TYP_BYREF, argx);
    }

    // Copy the valuetype to the temp
    GenTreePtr copyBlk = gtNewCpObjNode(dest, src, copyBlkClass, false);
    copyBlk            = fgMorphCopyBlock(copyBlk);

#if FEATURE_FIXED_OUT_ARGS

    // Do the copy early, and evalute the temp later (see EvalArgsToTemps)
    // When on Unix create LCL_FLD for structs passed in more than one registers. See fgMakeTmpArgNode
    GenTreePtr arg = copyBlk;

#else // FEATURE_FIXED_OUT_ARGS

    // Structs are always on the stack, and thus never need temps
    // so we have to put the copy and temp all into one expression
    GenTreePtr arg = fgMakeTmpArgNode(tmp FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(structDescPtr->passedInRegisters));

    // Change the expression to "(tmp=val),tmp"
    arg = gtNewOperNode(GT_COMMA, arg->TypeGet(), copyBlk, arg);

#endif // FEATURE_FIXED_OUT_ARGS

    args->gtOp.gtOp1 = arg;
    call->fgArgInfo->EvalToTmp(argIndex, tmp, arg);

    return;
}

#ifdef _TARGET_ARM_
// See declaration for specification comment.
void Compiler::fgAddSkippedRegsInPromotedStructArg(LclVarDsc* varDsc,
                                                   unsigned   firstArgRegNum,
                                                   regMaskTP* pArgSkippedRegMask)
{
    assert(varDsc->lvPromoted);
    // There's no way to do these calculations without breaking abstraction and assuming that
    // integer register arguments are consecutive ints.  They are on ARM.

    // To start, figure out what register contains the last byte of the first argument.
    LclVarDsc* firstFldVarDsc = &lvaTable[varDsc->lvFieldLclStart];
    unsigned   lastFldRegOfLastByte =
        (firstFldVarDsc->lvFldOffset + firstFldVarDsc->lvExactSize - 1) / TARGET_POINTER_SIZE;
    ;

    // Now we're keeping track of the register that the last field ended in; see what registers
    // subsequent fields start in, and whether any are skipped.
    // (We assume here the invariant that the fields are sorted in offset order.)
    for (unsigned fldVarOffset = 1; fldVarOffset < varDsc->lvFieldCnt; fldVarOffset++)
    {
        unsigned   fldVarNum    = varDsc->lvFieldLclStart + fldVarOffset;
        LclVarDsc* fldVarDsc    = &lvaTable[fldVarNum];
        unsigned   fldRegOffset = fldVarDsc->lvFldOffset / TARGET_POINTER_SIZE;
        assert(fldRegOffset >= lastFldRegOfLastByte); // Assuming sorted fields.
        // This loop should enumerate the offsets of any registers skipped.
        // Find what reg contains the last byte:
        // And start at the first register after that.  If that isn't the first reg of the current
        for (unsigned skippedRegOffsets = lastFldRegOfLastByte + 1; skippedRegOffsets < fldRegOffset;
             skippedRegOffsets++)
        {
            // If the register number would not be an arg reg, we're done.
            if (firstArgRegNum + skippedRegOffsets >= MAX_REG_ARG)
                return;
            *pArgSkippedRegMask |= genRegMask(regNumber(firstArgRegNum + skippedRegOffsets));
        }
        lastFldRegOfLastByte = (fldVarDsc->lvFldOffset + fldVarDsc->lvExactSize - 1) / TARGET_POINTER_SIZE;
    }
}

#endif // _TARGET_ARM_

//****************************************************************************
//  fgFixupStructReturn:
//    The companion to impFixupCallStructReturn.  Now that the importer is done
//    change the gtType to the precomputed native return type
//    requires that callNode currently has a struct type
//
void Compiler::fgFixupStructReturn(GenTreePtr callNode)
{
    assert(varTypeIsStruct(callNode));

    GenTreeCall* call              = callNode->AsCall();
    bool         callHasRetBuffArg = call->HasRetBufArg();
    bool         isHelperCall      = call->IsHelperCall();

    // Decide on the proper return type for this call that currently returns a struct
    //
    CORINFO_CLASS_HANDLE        retClsHnd = call->gtRetClsHnd;
    Compiler::structPassingKind howToReturnStruct;
    var_types                   returnType;

    // There are a couple of Helper Calls that say they return a TYP_STRUCT but they
    // expect this method to re-type this to a TYP_REF (what is in call->gtReturnType)
    //
    //    CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD
    //    CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD
    //    CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL
    //
    if (isHelperCall)
    {
        assert(!callHasRetBuffArg);
        assert(retClsHnd == NO_CLASS_HANDLE);

        // Now that we are past the importer, re-type this node
        howToReturnStruct = SPK_PrimitiveType;
        returnType        = (var_types)call->gtReturnType;
    }
    else
    {
        returnType = getReturnTypeForStruct(retClsHnd, &howToReturnStruct);
    }

    if (howToReturnStruct == SPK_ByReference)
    {
        assert(returnType == TYP_UNKNOWN);
        assert(callHasRetBuffArg);
    }
    else
    {
        assert(returnType != TYP_UNKNOWN);

        if (returnType != TYP_STRUCT)
        {
            // Widen the primitive type if necessary
            returnType = genActualType(returnType);
        }
        call->gtType = returnType;
    }

#if FEATURE_MULTIREG_RET
    // Either we don't have a struct now or if struct, then it is a struct returned in regs or in return buffer.
    assert(!varTypeIsStruct(call) || call->HasMultiRegRetVal() || callHasRetBuffArg);
#else // !FEATURE_MULTIREG_RET
    // No more struct returns
    assert(call->TypeGet() != TYP_STRUCT);
#endif

#if !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    // If it was a struct return, it has been transformed into a call
    // with a return buffer (that returns TYP_VOID) or into a return
    // of a primitive/enregisterable type
    assert(!callHasRetBuffArg || (call->TypeGet() == TYP_VOID));
#endif
}

/*****************************************************************************
 *
 *  A little helper used to rearrange nested commutative operations. The
 *  effect is that nested associative, commutative operations are transformed
 *  into a 'left-deep' tree, i.e. into something like this:
 *
 *      (((a op b) op c) op d) op...
 */

#if REARRANGE_ADDS

void Compiler::fgMoveOpsLeft(GenTreePtr tree)
{
    GenTreePtr op1;
    GenTreePtr op2;
    genTreeOps oper;

    do
    {
        op1  = tree->gtOp.gtOp1;
        op2  = tree->gtOp.gtOp2;
        oper = tree->OperGet();

        noway_assert(GenTree::OperIsCommutative(oper));
        noway_assert(oper == GT_ADD || oper == GT_XOR || oper == GT_OR || oper == GT_AND || oper == GT_MUL);
        noway_assert(!varTypeIsFloating(tree->TypeGet()) || !opts.genFPorder);
        noway_assert(oper == op2->gtOper);

        // Commutativity doesn't hold if overflow checks are needed

        if (tree->gtOverflowEx() || op2->gtOverflowEx())
        {
            return;
        }

        if (gtIsActiveCSE_Candidate(op2))
        {
            // If we have marked op2 as a CSE candidate,
            // we can't perform a commutative reordering
            // because any value numbers that we computed for op2
            // will be incorrect after performing a commutative reordering
            //
            return;
        }

        if (oper == GT_MUL && (op2->gtFlags & GTF_MUL_64RSLT))
        {
            return;
        }

        // Check for GTF_ADDRMODE_NO_CSE flag on add/mul Binary Operators
        if (((oper == GT_ADD) || (oper == GT_MUL)) && ((tree->gtFlags & GTF_ADDRMODE_NO_CSE) != 0))
        {
            return;
        }

        if ((tree->gtFlags | op2->gtFlags) & GTF_BOOLEAN)
        {
            // We could deal with this, but we were always broken and just hit the assert
            // below regarding flags, which means it's not frequent, so will just bail out.
            // See #195514
            return;
        }

        noway_assert(!tree->gtOverflowEx() && !op2->gtOverflowEx());

        GenTreePtr ad1 = op2->gtOp.gtOp1;
        GenTreePtr ad2 = op2->gtOp.gtOp2;

        // Compiler::optOptimizeBools() can create GT_OR of two GC pointers yeilding a GT_INT
        // We can not reorder such GT_OR trees
        //
        if (varTypeIsGC(ad1->TypeGet()) != varTypeIsGC(op2->TypeGet()))
        {
            break;
        }

        /* Change "(x op (y op z))" to "(x op y) op z" */
        /* ie.    "(op1 op (ad1 op ad2))" to "(op1 op ad1) op ad2" */

        GenTreePtr new_op1 = op2;

        new_op1->gtOp.gtOp1 = op1;
        new_op1->gtOp.gtOp2 = ad1;

        /* Change the flags. */

        // Make sure we arent throwing away any flags
        noway_assert((new_op1->gtFlags &
                      ~(GTF_MAKE_CSE | GTF_DONT_CSE | // It is ok that new_op1->gtFlags contains GTF_DONT_CSE flag.
                        GTF_REVERSE_OPS |             // The reverse ops flag also can be set, it will be re-calculated
                        GTF_NODE_MASK | GTF_ALL_EFFECT | GTF_UNSIGNED)) == 0);

        new_op1->gtFlags =
            (new_op1->gtFlags & (GTF_NODE_MASK | GTF_DONT_CSE)) | // Make sure we propagate GTF_DONT_CSE flag.
            (op1->gtFlags & GTF_ALL_EFFECT) | (ad1->gtFlags & GTF_ALL_EFFECT);

        /* Retype new_op1 if it has not/become a GC ptr. */

        if (varTypeIsGC(op1->TypeGet()))
        {
            noway_assert((varTypeIsGC(tree->TypeGet()) && op2->TypeGet() == TYP_I_IMPL &&
                          oper == GT_ADD) || // byref(ref + (int+int))
                         (varTypeIsI(tree->TypeGet()) && op2->TypeGet() == TYP_I_IMPL &&
                          oper == GT_OR)); // int(gcref | int(gcref|intval))

            new_op1->gtType = tree->gtType;
        }
        else if (varTypeIsGC(ad2->TypeGet()))
        {
            // Neither ad1 nor op1 are GC. So new_op1 isnt either
            noway_assert(op1->gtType == TYP_I_IMPL && ad1->gtType == TYP_I_IMPL);
            new_op1->gtType = TYP_I_IMPL;
        }

        // If new_op1 is a new expression. Assign it a new unique value number.
        // vnStore is null before the ValueNumber phase has run
        if (vnStore != nullptr)
        {
            // We can only keep the old value number on new_op1 if both op1 and ad2
            // have the same non-NoVN value numbers. Since op is commutative, comparing
            // only ad2 and op1 is enough.
            if ((op1->gtVNPair.GetLiberal() == ValueNumStore::NoVN) ||
                (ad2->gtVNPair.GetLiberal() == ValueNumStore::NoVN) ||
                (ad2->gtVNPair.GetLiberal() != op1->gtVNPair.GetLiberal()))
            {
                new_op1->gtVNPair.SetBoth(vnStore->VNForExpr(nullptr, new_op1->TypeGet()));
            }
        }

        tree->gtOp.gtOp1 = new_op1;
        tree->gtOp.gtOp2 = ad2;

        /* If 'new_op1' is now the same nested op, process it recursively */

        if ((ad1->gtOper == oper) && !ad1->gtOverflowEx())
        {
            fgMoveOpsLeft(new_op1);
        }

        /* If   'ad2'   is now the same nested op, process it
         * Instead of recursion, we set up op1 and op2 for the next loop.
         */

        op1 = new_op1;
        op2 = ad2;
    } while ((op2->gtOper == oper) && !op2->gtOverflowEx());

    return;
}

#endif

/*****************************************************************************/

void Compiler::fgSetRngChkTarget(GenTreePtr tree, bool delay)
{
    GenTreeBoundsChk* bndsChk = nullptr;
    SpecialCodeKind   kind    = SCK_RNGCHK_FAIL;

#ifdef FEATURE_SIMD
    if ((tree->gtOper == GT_ARR_BOUNDS_CHECK) || (tree->gtOper == GT_SIMD_CHK))
#else  // FEATURE_SIMD
    if (tree->gtOper == GT_ARR_BOUNDS_CHECK)
#endif // FEATURE_SIMD
    {
        bndsChk = tree->AsBoundsChk();
        kind    = tree->gtBoundsChk.gtThrowKind;
    }
    else
    {
        noway_assert((tree->gtOper == GT_ARR_ELEM) || (tree->gtOper == GT_ARR_INDEX));
    }

#ifdef _TARGET_X86_
    unsigned callStkDepth = fgPtrArgCntCur;
#else
    // only x86 pushes args
    const unsigned callStkDepth = 0;
#endif

    if (opts.MinOpts())
    {
        delay = false;

        // we need to initialize this field
        if (fgGlobalMorph && bndsChk != nullptr)
        {
            bndsChk->gtStkDepth = callStkDepth;
        }
    }

    if (!opts.compDbgCode)
    {
        if (delay || compIsForInlining())
        {
            /*  We delay this until after loop-oriented range check
                analysis. For now we merely store the current stack
                level in the tree node.
             */
            if (bndsChk != nullptr)
            {
                noway_assert(!bndsChk->gtIndRngFailBB || previousCompletedPhase >= PHASE_OPTIMIZE_LOOPS);
                bndsChk->gtStkDepth = callStkDepth;
            }
        }
        else
        {
            /* Create/find the appropriate "range-fail" label */

            // fgPtrArgCntCur is only valid for global morph or if we walk full stmt.
            noway_assert((bndsChk != nullptr) || fgGlobalMorph);

            unsigned stkDepth = (bndsChk != nullptr) ? bndsChk->gtStkDepth : callStkDepth;

            BasicBlock* rngErrBlk = fgRngChkTarget(compCurBB, stkDepth, kind);

            /* Add the label to the indirection node */

            if (bndsChk != nullptr)
            {
                bndsChk->gtIndRngFailBB = gtNewCodeRef(rngErrBlk);
            }
        }
    }
}

/*****************************************************************************
 *
 *  Expand a GT_INDEX node and fully morph the child operands
 *
 *  The orginal GT_INDEX node is bashed into the GT_IND node that accesses
 *  the array element.  We expand the GT_INDEX node into a larger tree that
 *  evaluates the array base and index.  The simplest expansion is a GT_COMMA
 *  with a GT_ARR_BOUND_CHK and a GT_IND with a GTF_INX_RNGCHK flag.
 *  For complex array or index expressions one or more GT_COMMA assignments
 *  are inserted so that we only evaluate the array or index expressions once.
 *
 *  The fully expanded tree is then morphed.  This causes gtFoldExpr to
 *  perform local constant prop and reorder the constants in the tree and
 *  fold them.
 *
 *  We then parse the resulting array element expression in order to locate
 *  and label the constants and variables that occur in the tree.
 */

const int MAX_ARR_COMPLEXITY   = 4;
const int MAX_INDEX_COMPLEXITY = 4;

GenTreePtr Compiler::fgMorphArrayIndex(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_INDEX);
    GenTreeIndex* asIndex = tree->AsIndex();

    var_types            elemTyp        = tree->TypeGet();
    unsigned             elemSize       = tree->gtIndex.gtIndElemSize;
    CORINFO_CLASS_HANDLE elemStructType = tree->gtIndex.gtStructElemClass;

    noway_assert(elemTyp != TYP_STRUCT || elemStructType != nullptr);

#ifdef FEATURE_SIMD
    if (featureSIMD && varTypeIsStruct(elemTyp) && elemSize <= getSIMDVectorRegisterByteLength())
    {
        // If this is a SIMD type, this is the point at which we lose the type information,
        // so we need to set the correct type on the GT_IND.
        // (We don't care about the base type here, so we only check, but don't retain, the return value).
        unsigned simdElemSize = 0;
        if (getBaseTypeAndSizeOfSIMDType(elemStructType, &simdElemSize) != TYP_UNKNOWN)
        {
            assert(simdElemSize == elemSize);
            elemTyp = getSIMDTypeForSize(elemSize);
            // This is the new type of the node.
            tree->gtType = elemTyp;
            // Now set elemStructType to null so that we don't confuse value numbering.
            elemStructType = nullptr;
        }
    }
#endif // FEATURE_SIMD

    GenTreePtr arrRef = asIndex->Arr();
    GenTreePtr index  = asIndex->Index();

    // Set up the the array length's offset into lenOffs
    // And    the the first element's offset into elemOffs
    ssize_t lenOffs;
    ssize_t elemOffs;
    if (tree->gtFlags & GTF_INX_STRING_LAYOUT)
    {
        lenOffs  = offsetof(CORINFO_String, stringLen);
        elemOffs = offsetof(CORINFO_String, chars);
        tree->gtFlags &= ~GTF_INX_STRING_LAYOUT; // Clear this flag as it is used for GTF_IND_VOLATILE
    }
    else if (tree->gtFlags & GTF_INX_REFARR_LAYOUT)
    {
        lenOffs  = offsetof(CORINFO_RefArray, length);
        elemOffs = eeGetEEInfo()->offsetOfObjArrayData;
    }
    else // We have a standard array
    {
        lenOffs  = offsetof(CORINFO_Array, length);
        elemOffs = offsetof(CORINFO_Array, u1Elems);
    }

    bool chkd = ((tree->gtFlags & GTF_INX_RNGCHK) != 0); // if false, range checking will be disabled
    bool nCSE = ((tree->gtFlags & GTF_DONT_CSE) != 0);

    GenTreePtr arrRefDefn = nullptr; // non-NULL if we need to allocate a temp for the arrRef expression
    GenTreePtr indexDefn  = nullptr; // non-NULL if we need to allocate a temp for the index expression
    GenTreePtr bndsChk    = nullptr;

    // If we're doing range checking, introduce a GT_ARR_BOUNDS_CHECK node for the address.
    if (chkd)
    {
        GenTreePtr arrRef2 = nullptr; // The second copy will be used in array address expression
        GenTreePtr index2  = nullptr;

        // If the arrRef expression involves an assignment, a call or reads from global memory,
        // then we *must* allocate a temporary in which to "localize" those values,
        // to ensure that the same values are used in the bounds check and the actual
        // dereference.
        // Also we allocate the temporary when the arrRef is sufficiently complex/expensive.
        //
        if ((arrRef->gtFlags & (GTF_ASG | GTF_CALL | GTF_GLOB_REF)) || gtComplexityExceeds(&arrRef, MAX_ARR_COMPLEXITY))
        {
            unsigned arrRefTmpNum = lvaGrabTemp(true DEBUGARG("arr expr"));
            arrRefDefn            = gtNewTempAssign(arrRefTmpNum, arrRef);
            arrRef                = gtNewLclvNode(arrRefTmpNum, arrRef->TypeGet());
            arrRef2               = gtNewLclvNode(arrRefTmpNum, arrRef->TypeGet());
        }
        else
        {
            arrRef2 = gtCloneExpr(arrRef);
            noway_assert(arrRef2 != nullptr);
        }

        // If the index expression involves an assignment, a call or reads from global memory,
        // we *must* allocate a temporary in which to "localize" those values,
        // to ensure that the same values are used in the bounds check and the actual
        // dereference.
        // Also we allocate the temporary when the index is sufficiently complex/expensive.
        //
        if ((index->gtFlags & (GTF_ASG | GTF_CALL | GTF_GLOB_REF)) || gtComplexityExceeds(&index, MAX_ARR_COMPLEXITY))
        {
            unsigned indexTmpNum = lvaGrabTemp(true DEBUGARG("arr expr"));
            indexDefn            = gtNewTempAssign(indexTmpNum, index);
            index                = gtNewLclvNode(indexTmpNum, index->TypeGet());
            index2               = gtNewLclvNode(indexTmpNum, index->TypeGet());
        }
        else
        {
            index2 = gtCloneExpr(index);
            noway_assert(index2 != nullptr);
        }

        // Next introduce a GT_ARR_BOUNDS_CHECK node
        var_types bndsChkType = TYP_INT; // By default, try to use 32-bit comparison for array bounds check.

#ifdef _TARGET_64BIT_
        // The CLI Spec allows an array to be indexed by either an int32 or a native int.  In the case
        // of a 64 bit architecture this means the array index can potentially be a TYP_LONG, so for this case,
        // the comparison will have to be widen to 64 bits.
        if (index->TypeGet() == TYP_I_IMPL)
        {
            bndsChkType = TYP_I_IMPL;
        }
#endif // _TARGET_64BIT_

        GenTree* arrLen = new (this, GT_ARR_LENGTH) GenTreeArrLen(TYP_INT, arrRef, (int)lenOffs);

        if (bndsChkType != TYP_INT)
        {
            arrLen = gtNewCastNode(bndsChkType, arrLen, bndsChkType);
        }

        GenTreeBoundsChk* arrBndsChk = new (this, GT_ARR_BOUNDS_CHECK)
            GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, arrLen, index, SCK_RNGCHK_FAIL);

        bndsChk = arrBndsChk;

        // Make sure to increment ref-counts if already ref-counted.
        if (lvaLocalVarRefCounted)
        {
            lvaRecursiveIncRefCounts(index);
            lvaRecursiveIncRefCounts(arrRef);
        }

        // Now we'll switch to using the second copies for arrRef and index
        // to compute the address expression

        arrRef = arrRef2;
        index  = index2;
    }

    // Create the "addr" which is "*(arrRef + ((index * elemSize) + elemOffs))"

    GenTreePtr addr;

#ifdef _TARGET_64BIT_
    // Widen 'index' on 64-bit targets
    if (index->TypeGet() != TYP_I_IMPL)
    {
        if (index->OperGet() == GT_CNS_INT)
        {
            index->gtType = TYP_I_IMPL;
        }
        else
        {
            index = gtNewCastNode(TYP_I_IMPL, index, TYP_I_IMPL);
        }
    }
#endif // _TARGET_64BIT_

    /* Scale the index value if necessary */
    if (elemSize > 1)
    {
        GenTreePtr size = gtNewIconNode(elemSize, TYP_I_IMPL);

        // Fix 392756 WP7 Crossgen
        //
        // During codegen optGetArrayRefScaleAndIndex() makes the assumption that op2 of a GT_MUL node
        // is a constant and is not capable of handling CSE'ing the elemSize constant into a lclvar.
        // Hence to prevent the constant from becoming a CSE we mark it as NO_CSE.
        //
        size->gtFlags |= GTF_DONT_CSE;

        /* Multiply by the array element size */
        addr = gtNewOperNode(GT_MUL, TYP_I_IMPL, index, size);
    }
    else
    {
        addr = index;
    }

    /* Add the object ref to the element's offset */

    addr = gtNewOperNode(GT_ADD, TYP_BYREF, arrRef, addr);

    /* Add the first element's offset */

    GenTreePtr cns = gtNewIconNode(elemOffs, TYP_I_IMPL);

    addr = gtNewOperNode(GT_ADD, TYP_BYREF, addr, cns);

#if SMALL_TREE_NODES
    assert(tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE);
#endif

    // Change the orginal GT_INDEX node into a GT_IND node
    tree->SetOper(GT_IND);

    // If the index node is a floating-point type, notify the compiler
    // we'll potentially use floating point registers at the time of codegen.
    if (varTypeIsFloating(tree->gtType))
    {
        this->compFloatingPointUsed = true;
    }

    // We've now consumed the GTF_INX_RNGCHK, and the node
    // is no longer a GT_INDEX node.
    tree->gtFlags &= ~GTF_INX_RNGCHK;

    tree->gtOp.gtOp1 = addr;

    // This is an array index expression.
    tree->gtFlags |= GTF_IND_ARR_INDEX;

    /* An indirection will cause a GPF if the address is null */
    tree->gtFlags |= GTF_EXCEPT;

    if (nCSE)
    {
        tree->gtFlags |= GTF_DONT_CSE;
    }

    // Store information about it.
    GetArrayInfoMap()->Set(tree, ArrayInfo(elemTyp, elemSize, (int)elemOffs, elemStructType));

    // Remember this 'indTree' that we just created, as we still need to attach the fieldSeq information to it.

    GenTreePtr indTree = tree;

    // Did we create a bndsChk tree?
    if (bndsChk)
    {
        // Use a GT_COMMA node to prepend the array bound check
        //
        tree = gtNewOperNode(GT_COMMA, elemTyp, bndsChk, tree);

        /* Mark the indirection node as needing a range check */
        fgSetRngChkTarget(bndsChk);
    }

    if (indexDefn != nullptr)
    {
        // Use a GT_COMMA node to prepend the index assignment
        //
        tree = gtNewOperNode(GT_COMMA, tree->TypeGet(), indexDefn, tree);
    }
    if (arrRefDefn != nullptr)
    {
        // Use a GT_COMMA node to prepend the arRef assignment
        //
        tree = gtNewOperNode(GT_COMMA, tree->TypeGet(), arrRefDefn, tree);
    }

    // Currently we morph the tree to perform some folding operations prior
    // to attaching fieldSeq info and labeling constant array index contributions
    //
    fgMorphTree(tree);

    // Ideally we just want to proceed to attaching fieldSeq info and labeling the
    // constant array index contributions, but the morphing operation may have changed
    // the 'tree' into something that now unconditionally throws an exception.
    //
    // In such case the gtEffectiveVal could be a new tree or it's gtOper could be modified
    // or it could be left unchanged.  If it is unchanged then we should not return,
    // instead we should proceed to attaching fieldSeq info, etc...
    //
    GenTreePtr arrElem = tree->gtEffectiveVal();

    if (fgIsCommaThrow(tree))
    {
        if ((arrElem != indTree) ||         // A new tree node may have been created
            (indTree->OperGet() != GT_IND)) // The GT_IND may have been changed to a GT_CNS_INT
        {
            return tree; // Just return the Comma-Throw, don't try to attach the fieldSeq info, etc..
        }
    }

    assert(!fgGlobalMorph || (arrElem->gtDebugFlags & GTF_DEBUG_NODE_MORPHED));

    addr = arrElem->gtOp.gtOp1;

    assert(addr->TypeGet() == TYP_BYREF);

    GenTreePtr cnsOff = nullptr;
    if (addr->OperGet() == GT_ADD)
    {
        if (addr->gtOp.gtOp2->gtOper == GT_CNS_INT)
        {
            cnsOff = addr->gtOp.gtOp2;
            addr   = addr->gtOp.gtOp1;
        }

        while ((addr->OperGet() == GT_ADD) || (addr->OperGet() == GT_SUB))
        {
            assert(addr->TypeGet() == TYP_BYREF);
            GenTreePtr index = addr->gtOp.gtOp2;

            // Label any constant array index contributions with #ConstantIndex and any LclVars with GTF_VAR_ARR_INDEX
            index->LabelIndex(this);

            addr = addr->gtOp.gtOp1;
        }
        assert(addr->TypeGet() == TYP_REF);
    }
    else if (addr->OperGet() == GT_CNS_INT)
    {
        cnsOff = addr;
    }

    FieldSeqNode* firstElemFseq = GetFieldSeqStore()->CreateSingleton(FieldSeqStore::FirstElemPseudoField);

    if ((cnsOff != nullptr) && (cnsOff->gtIntCon.gtIconVal == elemOffs))
    {
        // Assign it the [#FirstElem] field sequence
        //
        cnsOff->gtIntCon.gtFieldSeq = firstElemFseq;
    }
    else //  We have folded the first element's offset with the index expression
    {
        // Build the [#ConstantIndex, #FirstElem] field sequence
        //
        FieldSeqNode* constantIndexFseq = GetFieldSeqStore()->CreateSingleton(FieldSeqStore::ConstantIndexPseudoField);
        FieldSeqNode* fieldSeq          = GetFieldSeqStore()->Append(constantIndexFseq, firstElemFseq);

        if (cnsOff == nullptr) // It must have folded into a zero offset
        {
            // Record in the general zero-offset map.
            GetZeroOffsetFieldMap()->Set(addr, fieldSeq);
        }
        else
        {
            cnsOff->gtIntCon.gtFieldSeq = fieldSeq;
        }
    }

    return tree;
}

#ifdef _TARGET_X86_
/*****************************************************************************
 *
 *  Wrap fixed stack arguments for varargs functions to go through varargs
 *  cookie to access them, except for the cookie itself.
 *
 * Non-x86 platforms are allowed to access all arguments directly
 * so we don't need this code.
 *
 */
GenTreePtr Compiler::fgMorphStackArgForVarArgs(unsigned lclNum, var_types varType, unsigned lclOffs)
{
    /* For the fixed stack arguments of a varargs function, we need to go
        through the varargs cookies to access them, except for the
        cookie itself */

    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varDsc->lvIsParam && !varDsc->lvIsRegArg && lclNum != lvaVarargsHandleArg)
    {
        // Create a node representing the local pointing to the base of the args
        GenTreePtr ptrArg =
            gtNewOperNode(GT_SUB, TYP_I_IMPL, gtNewLclvNode(lvaVarargsBaseOfStkArgs, TYP_I_IMPL),
                          gtNewIconNode(varDsc->lvStkOffs - codeGen->intRegState.rsCalleeRegArgCount * sizeof(void*) +
                                        lclOffs));

        // Access the argument through the local
        GenTreePtr tree = gtNewOperNode(GT_IND, varType, ptrArg);
        tree->gtFlags |= GTF_IND_TGTANYWHERE;

        if (varDsc->lvAddrExposed)
        {
            tree->gtFlags |= GTF_GLOB_REF;
        }

        return fgMorphTree(tree);
    }

    return NULL;
}
#endif

/*****************************************************************************
 *
 *  Transform the given GT_LCL_VAR tree for code generation.
 */

GenTreePtr Compiler::fgMorphLocalVar(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_LCL_VAR);

    unsigned   lclNum  = tree->gtLclVarCommon.gtLclNum;
    var_types  varType = lvaGetRealType(lclNum);
    LclVarDsc* varDsc  = &lvaTable[lclNum];

    if (varDsc->lvAddrExposed)
    {
        tree->gtFlags |= GTF_GLOB_REF;
    }

#ifdef _TARGET_X86_
    if (info.compIsVarArgs)
    {
        GenTreePtr newTree = fgMorphStackArgForVarArgs(lclNum, varType, 0);
        if (newTree != NULL)
            return newTree;
    }
#endif // _TARGET_X86_

    /* If not during the global morphing phase bail */

    if (!fgGlobalMorph)
    {
        return tree;
    }

    bool varAddr = (tree->gtFlags & GTF_DONT_CSE) != 0;

    noway_assert(!(tree->gtFlags & GTF_VAR_DEF) || varAddr); // GTF_VAR_DEF should always imply varAddr

    if (!varAddr && varTypeIsSmall(varDsc->TypeGet()) && varDsc->lvNormalizeOnLoad())
    {
#if LOCAL_ASSERTION_PROP
        /* Assertion prop can tell us to omit adding a cast here */
        if (optLocalAssertionProp && optAssertionIsSubrange(tree, varType, apFull) != NO_ASSERTION_INDEX)
        {
            return tree;
        }
#endif
        /* Small-typed arguments and aliased locals are normalized on load.
           Other small-typed locals are normalized on store.
           Also, under the debugger as the debugger could write to the variable.
           If this is one of the former, insert a narrowing cast on the load.
                   ie. Convert: var-short --> cast-short(var-int) */

        tree->gtType = TYP_INT;
        fgMorphTreeDone(tree);
        tree = gtNewCastNode(TYP_INT, tree, varType);
        fgMorphTreeDone(tree);
        return tree;
    }

    return tree;
}

/*****************************************************************************
  Grab a temp for big offset morphing.
  This method will grab a new temp if no temp of this "type" has been created.
  Or it will return the same cached one if it has been created.
*/
unsigned Compiler::fgGetBigOffsetMorphingTemp(var_types type)
{
    unsigned lclNum = fgBigOffsetMorphingTemps[type];

    if (lclNum == BAD_VAR_NUM)
    {
        // We haven't created a temp for this kind of type. Create one now.
        lclNum                         = lvaGrabTemp(false DEBUGARG("Big Offset Morphing"));
        fgBigOffsetMorphingTemps[type] = lclNum;
    }
    else
    {
        // We better get the right type.
        noway_assert(lvaTable[lclNum].TypeGet() == type);
    }

    noway_assert(lclNum != BAD_VAR_NUM);
    return lclNum;
}

/*****************************************************************************
 *
 *  Transform the given GT_FIELD tree for code generation.
 */

GenTreePtr Compiler::fgMorphField(GenTreePtr tree, MorphAddrContext* mac)
{
    assert(tree->gtOper == GT_FIELD);

    noway_assert(tree->gtFlags & GTF_GLOB_REF);

    CORINFO_FIELD_HANDLE symHnd          = tree->gtField.gtFldHnd;
    unsigned             fldOffset       = tree->gtField.gtFldOffset;
    GenTreePtr           objRef          = tree->gtField.gtFldObj;
    bool                 fieldMayOverlap = false;
    if (tree->gtField.gtFldMayOverlap)
    {
        fieldMayOverlap = true;
        // Reset the flag because we may reuse the node.
        tree->gtField.gtFldMayOverlap = false;
    }

#ifdef FEATURE_SIMD
    // if this field belongs to simd struct, tranlate it to simd instrinsic.
    if (mac == nullptr || mac->m_kind != MACK_Addr)
    {
        GenTreePtr newTree = fgMorphFieldToSIMDIntrinsicGet(tree);
        if (newTree != tree)
        {
            newTree = fgMorphSmpOp(newTree);
            return newTree;
        }
    }
    else if (objRef != nullptr && objRef->OperGet() == GT_ADDR && objRef->OperIsSIMD())
    {
        // We have a field of an SIMD intrinsic in an address-taken context.
        // We need to copy the SIMD result to a temp, and take the field of that.
        GenTree* copy      = fgCopySIMDNode(objRef->gtOp.gtOp1->AsSIMD());
        objRef->gtOp.gtOp1 = copy;
    }
#endif

    /* Is this an instance data member? */

    if (objRef)
    {
        GenTreePtr addr;

        if (tree->gtFlags & GTF_IND_TLS_REF)
        {
            NO_WAY("instance field can not be a TLS ref.");
        }

        /* We'll create the expression "*(objRef + mem_offs)" */

        noway_assert(varTypeIsGC(objRef->TypeGet()) || objRef->TypeGet() == TYP_I_IMPL);

        // An optimization for Contextful classes:
        // we unwrap the proxy when we have a 'this reference'
        if (info.compIsContextful && info.compUnwrapContextful && impIsThis(objRef))
        {
            objRef = fgUnwrapProxy(objRef);
        }

        /*
            Now we have a tree like this:

                                  +--------------------+
                                  |      GT_FIELD      |   tree
                                  +----------+---------+
                                             |
                              +--------------+-------------+
                              |   tree->gtField.gtFldObj   |
                              +--------------+-------------+


            We want to make it like this (when fldOffset is <= MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT):

                                  +--------------------+
                                  |   GT_IND/GT_OBJ    |   tree
                                  +---------+----------+
                                            |
                                            |
                                  +---------+----------+
                                  |       GT_ADD       |   addr
                                  +---------+----------+
                                            |
                                          /   \
                                        /       \
                                      /           \
                         +-------------------+  +----------------------+
                         |       objRef      |  |     fldOffset        |
                         |                   |  | (when fldOffset !=0) |
                         +-------------------+  +----------------------+


            or this (when fldOffset is > MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT):


                                  +--------------------+
                                  |   GT_IND/GT_OBJ    |   tree
                                  +----------+---------+
                                             |
                                  +----------+---------+
                                  |       GT_COMMA     |  comma2
                                  +----------+---------+
                                             |
                                            / \
                                          /     \
                                        /         \
                                      /             \
                 +---------+----------+               +---------+----------+
           comma |      GT_COMMA      |               |  "+" (i.e. GT_ADD) |   addr
                 +---------+----------+               +---------+----------+
                           |                                     |
                         /   \                                  /  \
                       /       \                              /      \
                     /           \                          /          \
         +-----+-----+             +-----+-----+      +---------+   +-----------+
     asg |  GT_ASG   |         ind |   GT_IND  |      |  tmpLcl |   | fldOffset |
         +-----+-----+             +-----+-----+      +---------+   +-----------+
               |                         |
              / \                        |
            /     \                      |
          /         \                    |
   +-----+-----+   +-----+-----+   +-----------+
   |   tmpLcl  |   |   objRef  |   |   tmpLcl  |
   +-----------+   +-----------+   +-----------+


        */

        var_types objRefType = objRef->TypeGet();

        GenTreePtr comma = nullptr;

        bool addedExplicitNullCheck = false;

        // NULL mac means we encounter the GT_FIELD first.  This denotes a dereference of the field,
        // and thus is equivalent to a MACK_Ind with zero offset.
        MorphAddrContext defMAC(MACK_Ind);
        if (mac == nullptr)
        {
            mac = &defMAC;
        }

        // This flag is set to enable the "conservative" style of explicit null-check insertion.
        // This means that we insert an explicit null check whenever we create byref by adding a
        // constant offset to a ref, in a MACK_Addr context (meaning that the byref is not immediately
        // dereferenced).  The alternative is "aggressive", which would not insert such checks (for
        // small offsets); in this plan, we would transfer some null-checking responsibility to
        // callee's of methods taking byref parameters.  They would have to add explicit null checks
        // when creating derived byrefs from argument byrefs by adding constants to argument byrefs, in
        // contexts where the resulting derived byref is not immediately dereferenced (or if the offset is too
        // large).  To make the "aggressive" scheme work, however, we'd also have to add explicit derived-from-null
        // checks for byref parameters to "external" methods implemented in C++, and in P/Invoke stubs.
        // This is left here to point out how to implement it.
        CLANG_FORMAT_COMMENT_ANCHOR;

#define CONSERVATIVE_NULL_CHECK_BYREF_CREATION 1

        // If the objRef is a GT_ADDR node, it, itself, never requires null checking.  The expression
        // whose address is being taken is either a local or static variable, whose address is necessarily
        // non-null, or else it is a field dereference, which will do its own bounds checking if necessary.
        if (objRef->gtOper != GT_ADDR && ((mac->m_kind == MACK_Addr || mac->m_kind == MACK_Ind) &&
                                          (!mac->m_allConstantOffsets || fgIsBigOffset(mac->m_totalOffset + fldOffset)
#if CONSERVATIVE_NULL_CHECK_BYREF_CREATION
                                           || (mac->m_kind == MACK_Addr && (mac->m_totalOffset + fldOffset > 0))
#else
                                           || (objRef->gtType == TYP_BYREF && mac->m_kind == MACK_Addr &&
                                               (mac->m_totalOffset + fldOffset > 0))
#endif
                                               )))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Before explicit null check morphing:\n");
                gtDispTree(tree);
            }
#endif

            //
            // Create the "comma" subtree
            //
            GenTreePtr asg = nullptr;
            GenTreePtr nullchk;

            unsigned lclNum;

            if (objRef->gtOper != GT_LCL_VAR)
            {
                lclNum = fgGetBigOffsetMorphingTemp(genActualType(objRef->TypeGet()));

                // Create the "asg" node
                asg = gtNewTempAssign(lclNum, objRef);
            }
            else
            {
                lclNum = objRef->gtLclVarCommon.gtLclNum;
            }

            // Create the "nullchk" node.
            // Make it TYP_BYTE so we only deference it for 1 byte.
            GenTreePtr lclVar = gtNewLclvNode(lclNum, objRefType);
            nullchk           = new (this, GT_NULLCHECK) GenTreeIndir(GT_NULLCHECK, TYP_BYTE, lclVar, nullptr);

            nullchk->gtFlags |= GTF_DONT_CSE; // Don't try to create a CSE for these TYP_BYTE indirections

            // An indirection will cause a GPF if the address is null.
            nullchk->gtFlags |= GTF_EXCEPT;

            compCurBB->bbFlags |= BBF_HAS_NULLCHECK;
            optMethodFlags |= OMF_HAS_NULLCHECK;

            if (asg)
            {
                // Create the "comma" node.
                comma = gtNewOperNode(GT_COMMA,
                                      TYP_VOID, // We don't want to return anything from this "comma" node.
                                                // Set the type to TYP_VOID, so we can select "cmp" instruction
                                                // instead of "mov" instruction later on.
                                      asg, nullchk);
            }
            else
            {
                comma = nullchk;
            }

            addr = gtNewLclvNode(lclNum, objRefType); // Use "tmpLcl" to create "addr" node.

            addedExplicitNullCheck = true;
        }
        else if (fldOffset == 0)
        {
            // Generate the "addr" node.
            addr = objRef;
            FieldSeqNode* fieldSeq =
                fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
            GetZeroOffsetFieldMap()->Set(addr, fieldSeq);
        }
        else
        {
            addr = objRef;
        }

#ifdef FEATURE_READYTORUN_COMPILER
        if (tree->gtField.gtFieldLookup.addr != nullptr)
        {
            GenTreePtr baseOffset = gtNewIconEmbHndNode(tree->gtField.gtFieldLookup.addr, nullptr, GTF_ICON_FIELD_HDL);

            if (tree->gtField.gtFieldLookup.accessType == IAT_PVALUE)
                baseOffset = gtNewOperNode(GT_IND, TYP_I_IMPL, baseOffset);

            addr =
                gtNewOperNode(GT_ADD, (var_types)(objRefType == TYP_I_IMPL ? TYP_I_IMPL : TYP_BYREF), addr, baseOffset);
        }
#endif
        if (fldOffset != 0)
        {
            // Generate the "addr" node.
            /* Add the member offset to the object's address */
            FieldSeqNode* fieldSeq =
                fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
            addr = gtNewOperNode(GT_ADD, (var_types)(objRefType == TYP_I_IMPL ? TYP_I_IMPL : TYP_BYREF), addr,
                                 gtNewIconHandleNode(fldOffset, GTF_ICON_FIELD_OFF, fieldSeq));
        }

        // Now let's set the "tree" as a GT_IND tree.

        tree->SetOper(GT_IND);
        tree->gtOp.gtOp1 = addr;

        if (fgAddrCouldBeNull(addr))
        {
            // This indirection can cause a GPF if the address could be null.
            tree->gtFlags |= GTF_EXCEPT;
        }

        if (addedExplicitNullCheck)
        {
            //
            // Create "comma2" node and link it to "tree".
            //
            GenTreePtr comma2;
            comma2 = gtNewOperNode(GT_COMMA,
                                   addr->TypeGet(), // The type of "comma2" node is the same as the type of "addr" node.
                                   comma, addr);
            tree->gtOp.gtOp1 = comma2;
        }

#ifdef DEBUG
        if (verbose)
        {
            if (addedExplicitNullCheck)
            {
                printf("After adding explicit null check:\n");
                gtDispTree(tree);
            }
        }
#endif
    }
    else /* This is a static data member */
    {
        if (tree->gtFlags & GTF_IND_TLS_REF)
        {
            // Thread Local Storage static field reference
            //
            // Field ref is a TLS 'Thread-Local-Storage' reference
            //
            // Build this tree:  IND(*) #
            //                    |
            //                   ADD(I_IMPL)
            //                   / \
            //                  /  CNS(fldOffset)
            //                 /
            //                /
            //               /
            //             IND(I_IMPL) == [Base of this DLL's TLS]
            //              |
            //             ADD(I_IMPL)
            //             / \
            //            /   CNS(IdValue*4) or MUL
            //           /                      / \
            //          IND(I_IMPL)            /  CNS(4)
            //           |                    /
            //          CNS(TLS_HDL,0x2C)    IND
            //                                |
            //                               CNS(pIdAddr)
            //
            // # Denotes the orginal node
            //
            void**   pIdAddr = nullptr;
            unsigned IdValue = info.compCompHnd->getFieldThreadLocalStoreID(symHnd, (void**)&pIdAddr);

            //
            // If we can we access the TLS DLL index ID value directly
            // then pIdAddr will be NULL and
            //      IdValue will be the actual TLS DLL index ID
            //
            GenTreePtr dllRef = nullptr;
            if (pIdAddr == nullptr)
            {
                if (IdValue != 0)
                {
                    dllRef = gtNewIconNode(IdValue * 4, TYP_I_IMPL);
                }
            }
            else
            {
                dllRef = gtNewIconHandleNode((size_t)pIdAddr, GTF_ICON_STATIC_HDL);
                dllRef = gtNewOperNode(GT_IND, TYP_I_IMPL, dllRef);
                dllRef->gtFlags |= GTF_IND_INVARIANT;

                /* Multiply by 4 */

                dllRef = gtNewOperNode(GT_MUL, TYP_I_IMPL, dllRef, gtNewIconNode(4, TYP_I_IMPL));
            }

#define WIN32_TLS_SLOTS (0x2C) // Offset from fs:[0] where the pointer to the slots resides

            // Mark this ICON as a TLS_HDL, codegen will use FS:[cns]

            GenTreePtr tlsRef = gtNewIconHandleNode(WIN32_TLS_SLOTS, GTF_ICON_TLS_HDL);

            tlsRef = gtNewOperNode(GT_IND, TYP_I_IMPL, tlsRef);

            if (dllRef != nullptr)
            {
                /* Add the dllRef */
                tlsRef = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsRef, dllRef);
            }

            /* indirect to have tlsRef point at the base of the DLLs Thread Local Storage */
            tlsRef = gtNewOperNode(GT_IND, TYP_I_IMPL, tlsRef);

            if (fldOffset != 0)
            {
                FieldSeqNode* fieldSeq =
                    fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
                GenTreePtr fldOffsetNode = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, fldOffset, fieldSeq);

                /* Add the TLS static field offset to the address */

                tlsRef = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsRef, fldOffsetNode);
            }

            // Final indirect to get to actual value of TLS static field

            tree->SetOper(GT_IND);
            tree->gtOp.gtOp1 = tlsRef;

            noway_assert(tree->gtFlags & GTF_IND_TLS_REF);
        }
        else
        {
            // Normal static field reference

            //
            // If we can we access the static's address directly
            // then pFldAddr will be NULL and
            //      fldAddr will be the actual address of the static field
            //
            void** pFldAddr = nullptr;
            void*  fldAddr  = info.compCompHnd->getFieldAddress(symHnd, (void**)&pFldAddr);

            if (pFldAddr == nullptr)
            {
#ifdef _TARGET_64BIT_
                if (IMAGE_REL_BASED_REL32 != eeGetRelocTypeHint(fldAddr))
                {
                    // The address is not directly addressible, so force it into a
                    // constant, so we handle it properly

                    GenTreePtr addr = gtNewIconHandleNode((size_t)fldAddr, GTF_ICON_STATIC_HDL);
                    addr->gtType    = TYP_I_IMPL;
                    FieldSeqNode* fieldSeq =
                        fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
                    addr->gtIntCon.gtFieldSeq = fieldSeq;

                    tree->SetOper(GT_IND);
                    tree->gtOp.gtOp1 = addr;

                    return fgMorphSmpOp(tree);
                }
                else
#endif // _TARGET_64BIT_
                {
                    // Only volatile could be set, and it maps over
                    noway_assert((tree->gtFlags & ~(GTF_FLD_VOLATILE | GTF_COMMON_MASK)) == 0);
                    noway_assert(GTF_FLD_VOLATILE == GTF_IND_VOLATILE);
                    tree->SetOper(GT_CLS_VAR);
                    tree->gtClsVar.gtClsVarHnd = symHnd;
                    FieldSeqNode* fieldSeq =
                        fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
                    tree->gtClsVar.gtFieldSeq = fieldSeq;
                }

                return tree;
            }
            else
            {
                GenTreePtr addr = gtNewIconHandleNode((size_t)pFldAddr, GTF_ICON_STATIC_HDL);

                // There are two cases here, either the static is RVA based,
                // in which case the type of the FIELD node is not a GC type
                // and the handle to the RVA is a TYP_I_IMPL.  Or the FIELD node is
                // a GC type and the handle to it is a TYP_BYREF in the GC heap
                // because handles to statics now go into the large object heap

                var_types  handleTyp = (var_types)(varTypeIsGC(tree->TypeGet()) ? TYP_BYREF : TYP_I_IMPL);
                GenTreePtr op1       = gtNewOperNode(GT_IND, handleTyp, addr);
                op1->gtFlags |= GTF_IND_INVARIANT;

                tree->SetOper(GT_IND);
                tree->gtOp.gtOp1 = op1;
            }
        }
    }
    noway_assert(tree->gtOper == GT_IND);

    GenTreePtr res = fgMorphSmpOp(tree);

    if (fldOffset == 0 && res->OperGet() == GT_IND)
    {
        GenTreePtr addr = res->gtOp.gtOp1;
        // Since we don't make a constant zero to attach the field sequence to, associate it with the "addr" node.
        FieldSeqNode* fieldSeq =
            fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
        fgAddFieldSeqForZeroOffset(addr, fieldSeq);
    }

    return res;
}

//------------------------------------------------------------------------------
// fgMorphCallInline: attempt to inline a call
//
// Arguments:
//    call         - call expression to inline, inline candidate
//    inlineResult - result tracking and reporting
//
// Notes:
//    Attempts to inline the call.
//
//    If successful, callee's IR is inserted in place of the call, and
//    is marked with an InlineContext.
//
//    If unsuccessful, the transformations done in anticpation of a
//    possible inline are undone, and the candidate flag on the call
//    is cleared.

void Compiler::fgMorphCallInline(GenTreeCall* call, InlineResult* inlineResult)
{
    // The call must be a candiate for inlining.
    assert((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0);

    // Attempt the inline
    fgMorphCallInlineHelper(call, inlineResult);

    // We should have made up our minds one way or another....
    assert(inlineResult->IsDecided());

    // If we failed to inline, we have a bit of work to do to cleanup
    if (inlineResult->IsFailure())
    {

#ifdef DEBUG

        // Before we do any cleanup, create a failing InlineContext to
        // capture details of the inlining attempt.
        m_inlineStrategy->NewFailure(fgMorphStmt, inlineResult);

#endif

        // It was an inline candidate, but we haven't expanded it.
        if (call->gtCall.gtReturnType != TYP_VOID)
        {
            // Detach the GT_CALL tree from the original statement by
            // hanging a "nothing" node to it. Later the "nothing" node will be removed
            // and the original GT_CALL tree will be picked up by the GT_RET_EXPR node.

            noway_assert(fgMorphStmt->gtStmt.gtStmtExpr == call);
            fgMorphStmt->gtStmt.gtStmtExpr = gtNewNothingNode();
        }

        // Clear the Inline Candidate flag so we can ensure later we tried
        // inlining all candidates.
        //
        call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
    }
}

/*****************************************************************************
 *  Helper to attempt to inline a call
 *  Sets success/failure in inline result
 *  If success, modifies current method's IR with inlinee's IR
 *  If failed, undoes any speculative modifications to current method
 */

void Compiler::fgMorphCallInlineHelper(GenTreeCall* call, InlineResult* result)
{
    // Don't expect any surprises here.
    assert(result->IsCandidate());

    if (lvaCount >= MAX_LV_NUM_COUNT_FOR_INLINING)
    {
        // For now, attributing this to call site, though it's really
        // more of a budget issue (lvaCount currently includes all
        // caller and prospective callee locals). We still might be
        // able to inline other callees into this caller, or inline
        // this callee in other callers.
        result->NoteFatal(InlineObservation::CALLSITE_TOO_MANY_LOCALS);
        return;
    }

    if (call->IsVirtual())
    {
        result->NoteFatal(InlineObservation::CALLSITE_IS_VIRTUAL);
        return;
    }

    // impMarkInlineCandidate() is expected not to mark tail prefixed calls
    // and recursive tail calls as inline candidates.
    noway_assert(!call->IsTailPrefixedCall());
    noway_assert(!call->IsImplicitTailCall() || !gtIsRecursiveCall(call));

    /* If the caller's stack frame is marked, then we can't do any inlining. Period.
       Although we have checked this in impCanInline, it is possible that later IL instructions
       might cause compNeedSecurityCheck to be set. Therefore we need to check it here again.
    */

    if (opts.compNeedSecurityCheck)
    {
        result->NoteFatal(InlineObservation::CALLER_NEEDS_SECURITY_CHECK);
        return;
    }

    //
    // Calling inlinee's compiler to inline the method.
    //

    unsigned startVars = lvaCount;

#ifdef DEBUG
    if (verbose)
    {
        printf("Expanding INLINE_CANDIDATE in statement ");
        printTreeID(fgMorphStmt);
        printf(" in BB%02u:\n", compCurBB->bbNum);
        gtDispTree(fgMorphStmt);

        // printf("startVars=%d.\n", startVars);
    }
#endif

    impInlineRoot()->m_inlineStrategy->NoteAttempt(result);

    //
    // Invoke the compiler to inline the call.
    //

    fgInvokeInlineeCompiler(call, result);

    if (result->IsFailure())
    {
        // Undo some changes made in anticipation of inlining...

        // Zero out the used locals
        memset(lvaTable + startVars, 0, (lvaCount - startVars) * sizeof(*lvaTable));
        for (unsigned i = startVars; i < lvaCount; i++)
        {
            new (&lvaTable[i], jitstd::placement_t()) LclVarDsc(this); // call the constructor.
        }

        lvaCount = startVars;

#ifdef DEBUG
        if (verbose)
        {
            // printf("Inlining failed. Restore lvaCount to %d.\n", lvaCount);
        }
#endif

        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        // printf("After inlining lvaCount=%d.\n", lvaCount);
    }
#endif
}

/*****************************************************************************
 *
 * Performs checks to see if this tail call can be optimized as epilog+jmp.
 */
bool Compiler::fgCanFastTailCall(GenTreeCall* callee)
{
#if FEATURE_FASTTAILCALL
    // Reached here means that return types of caller and callee are tail call compatible.
    // In case of structs that can be returned in a register, compRetNativeType is set to the actual return type.
    //
    // In an implicit tail call case callSig may not be available but it is guaranteed to be available
    // for explicit tail call cases.  The reason implicit tail case callSig may not be available is that
    // a call node might be marked as an in-line candidate and could fail to be in-lined. In which case
    // fgInline() will replace return value place holder with call node using gtCloneExpr() which is
    // currently not copying/setting callSig.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (callee->IsTailPrefixedCall())
    {
        assert(impTailCallRetTypeCompatible(info.compRetNativeType, info.compMethodInfo->args.retTypeClass,
                                            (var_types)callee->gtReturnType, callee->callSig->retTypeClass));
    }
#endif

    // Note on vararg methods:
    // If the caller is vararg method, we don't know the number of arguments passed by caller's caller.
    // But we can be sure that in-coming arg area of vararg caller would be sufficient to hold its
    // fixed args. Therefore, we can allow a vararg method to fast tail call other methods as long as
    // out-going area required for callee is bounded by caller's fixed argument space.
    //
    // Note that callee being a vararg method is not a problem since we can account the params being passed.

    // Count of caller args including implicit and hidden (i.e. thisPtr, RetBuf, GenericContext, VarargCookie)
    unsigned nCallerArgs = info.compArgsCount;

    // Count the callee args including implicit and hidden.
    // Note that GenericContext and VarargCookie are added by importer while
    // importing the call to gtCallArgs list along with explicit user args.
    unsigned nCalleeArgs = 0;
    if (callee->gtCallObjp) // thisPtr
    {
        nCalleeArgs++;
    }

    if (callee->HasRetBufArg()) // RetBuf
    {
        nCalleeArgs++;

        // If callee has RetBuf param, caller too must have it.
        // Otherwise go the slow route.
        if (info.compRetBuffArg == BAD_VAR_NUM)
        {
            return false;
        }
    }

    // Count user args while tracking whether any of them is a multi-byte params
    // that cannot be passed in a register. Note that we don't need to count
    // non-standard and secret params passed in registers (e.g. R10, R11) since
    // these won't contribute to out-going arg size.
    bool hasMultiByteArgs = false;
    for (GenTreePtr args = callee->gtCallArgs; (args != nullptr) && !hasMultiByteArgs; args = args->gtOp.gtOp2)
    {
        nCalleeArgs++;

        assert(args->IsList());
        GenTreePtr argx = args->gtOp.gtOp1;

        if (varTypeIsStruct(argx))
        {
            // Actual arg may be a child of a GT_COMMA. Skip over comma opers.
            while (argx->gtOper == GT_COMMA)
            {
                argx = argx->gtOp.gtOp2;
            }

            // Get the size of the struct and see if it is register passable.
            CORINFO_CLASS_HANDLE objClass = nullptr;

            if (argx->OperGet() == GT_OBJ)
            {
                objClass = argx->AsObj()->gtClass;
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

                unsigned typeSize = 0;
                hasMultiByteArgs  = !VarTypeIsMultiByteAndCanEnreg(argx->TypeGet(), objClass, &typeSize, false);

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) || defined(_TARGET_ARM64_)
                // On System V/arm64 the args could be a 2 eightbyte struct that is passed in two registers.
                // Account for the second eightbyte in the nCalleeArgs.
                // https://github.com/dotnet/coreclr/issues/2666
                // TODO-CQ-Amd64-Unix/arm64:  Structs of size between 9 to 16 bytes are conservatively estimated
                //                            as two args, since they need two registers whereas nCallerArgs is
                //                            counting such an arg as one. This would mean we will not be optimizing
                //                            certain calls though technically possible.

                if (typeSize > TARGET_POINTER_SIZE)
                {
                    unsigned extraArgRegsToAdd = (typeSize / TARGET_POINTER_SIZE);
                    nCalleeArgs += extraArgRegsToAdd;
                }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING || _TARGET_ARM64_

#else
                assert(!"Target platform ABI rules regarding passing struct type args in registers");
                unreached();
#endif //_TARGET_AMD64_ || _TARGET_ARM64_
            }
            else
            {
                hasMultiByteArgs = true;
            }
        }
    }

    // Go the slow route, if it has multi-byte params
    if (hasMultiByteArgs)
    {
        return false;
    }

    // If we reached here means that callee has only those argument types which can be passed in
    // a register and if passed on stack will occupy exactly one stack slot in out-going arg area.
    // If we are passing args on stack for callee and it has more args passed on stack than
    // caller, then fast tail call cannot be performed.
    //
    // Note that the GC'ness of on stack args need not match since the arg setup area is marked
    // as non-interruptible for fast tail calls.
    if ((nCalleeArgs > MAX_REG_ARG) && (nCallerArgs < nCalleeArgs))
    {
        return false;
    }

    return true;
#else
    return false;
#endif
}

/*****************************************************************************
 *
 *  Transform the given GT_CALL tree for tail call code generation.
 */
void Compiler::fgMorphTailCall(GenTreeCall* call)
{
    JITDUMP("fgMorphTailCall (before):\n");
    DISPTREE(call);

#if defined(_TARGET_ARM_)
    // For the helper-assisted tail calls, we need to push all the arguments
    // into a single list, and then add a few extra at the beginning

    // Check for PInvoke call types that we don't handle in codegen yet.
    assert(!call->IsUnmanaged());
    assert(call->IsVirtual() || (call->gtCallType != CT_INDIRECT) || (call->gtCallCookie == NULL));

    // First move the this pointer (if any) onto the regular arg list
    GenTreePtr thisPtr = NULL;
    if (call->gtCallObjp)
    {
        GenTreePtr objp  = call->gtCallObjp;
        call->gtCallObjp = NULL;

        if ((call->gtFlags & GTF_CALL_NULLCHECK) || call->IsVirtualVtable())
        {
            thisPtr      = gtClone(objp, true);
            var_types vt = objp->TypeGet();
            if (thisPtr == NULL)
            {
                // Too complex, so use a temp
                unsigned   lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
                GenTreePtr asg    = gtNewTempAssign(lclNum, objp);
                if (!call->IsVirtualVtable())
                {
                    // Add an indirection to get the nullcheck
                    GenTreePtr tmp = gtNewLclvNode(lclNum, vt);
                    GenTreePtr ind = gtNewOperNode(GT_IND, TYP_INT, tmp);
                    asg            = gtNewOperNode(GT_COMMA, TYP_VOID, asg, ind);
                }
                objp    = gtNewOperNode(GT_COMMA, vt, asg, gtNewLclvNode(lclNum, vt));
                thisPtr = gtNewLclvNode(lclNum, vt);
            }
            else if (!call->IsVirtualVtable())
            {
                GenTreePtr ind = gtNewOperNode(GT_IND, TYP_INT, thisPtr);
                objp           = gtNewOperNode(GT_COMMA, vt, ind, objp);
                thisPtr        = gtClone(thisPtr, true);
            }

            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }

        call->gtCallArgs = gtNewListNode(objp, call->gtCallArgs);
    }

    // Add the extra VSD parameter if needed
    CorInfoHelperTailCallSpecialHandling flags = CorInfoHelperTailCallSpecialHandling(0);
    if (call->IsVirtualStub())
    {
        flags = CORINFO_TAILCALL_STUB_DISPATCH_ARG;

        GenTreePtr arg;
        if (call->gtCallType == CT_INDIRECT)
        {
            arg = gtClone(call->gtCallAddr, true);
            noway_assert(arg != NULL);
        }
        else
        {
            noway_assert(call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT);
            ssize_t addr = ssize_t(call->gtStubCallStubAddr);
            arg          = gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);

            // Change the call type, so we can add the extra indirection here, rather than in codegen
            call->gtCallAddr         = gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
            call->gtStubCallStubAddr = NULL;
            call->gtCallType         = CT_INDIRECT;
        }
        // Add the extra indirection to generate the real target
        call->gtCallAddr = gtNewOperNode(GT_IND, TYP_I_IMPL, call->gtCallAddr);
        call->gtFlags |= GTF_EXCEPT;

        // And push the stub address onto the list of arguments
        call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);
    }
    else if (call->IsVirtualVtable())
    {
        // TODO-ARM-NYI: for x64 handle CORINFO_TAILCALL_THIS_IN_SECRET_REGISTER

        noway_assert(thisPtr != NULL);

        GenTreePtr add  = gtNewOperNode(GT_ADD, TYP_I_IMPL, thisPtr, gtNewIconNode(VPTR_OFFS, TYP_I_IMPL));
        GenTreePtr vtbl = gtNewOperNode(GT_IND, TYP_I_IMPL, add);
        vtbl->gtFlags |= GTF_EXCEPT;

        unsigned vtabOffsOfIndirection;
        unsigned vtabOffsAfterIndirection;
        info.compCompHnd->getMethodVTableOffset(call->gtCallMethHnd, &vtabOffsOfIndirection, &vtabOffsAfterIndirection);

        /* Get the appropriate vtable chunk */

        add  = gtNewOperNode(GT_ADD, TYP_I_IMPL, vtbl, gtNewIconNode(vtabOffsOfIndirection, TYP_I_IMPL));
        vtbl = gtNewOperNode(GT_IND, TYP_I_IMPL, add);

        /* Now the appropriate vtable slot */

        add  = gtNewOperNode(GT_ADD, TYP_I_IMPL, vtbl, gtNewIconNode(vtabOffsAfterIndirection, TYP_I_IMPL));
        vtbl = gtNewOperNode(GT_IND, TYP_I_IMPL, add);

        // Switch this to a plain indirect call
        call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;
        assert(!call->IsVirtual());
        call->gtCallType = CT_INDIRECT;

        call->gtCallAddr   = vtbl;
        call->gtCallCookie = NULL;
        call->gtFlags |= GTF_EXCEPT;
    }

    // Now inject a placeholder for the real call target that codegen
    // will generate
    GenTreePtr arg = new (this, GT_NOP) GenTreeOp(GT_NOP, TYP_I_IMPL);
    codeGen->genMarkTreeInReg(arg, REG_TAILCALL_ADDR);
    call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);

    // Lastly inject the pointer for the copy routine
    noway_assert(call->callSig != NULL);
    void* pfnCopyArgs = info.compCompHnd->getTailCallCopyArgsThunk(call->callSig, flags);
    arg               = gtNewIconHandleNode(ssize_t(pfnCopyArgs), GTF_ICON_FTN_ADDR);
    call->gtCallArgs  = gtNewListNode(arg, call->gtCallArgs);

    // It is now a varargs tail call
    call->gtCallMoreFlags = GTF_CALL_M_VARARGS | GTF_CALL_M_TAILCALL;
    call->gtFlags &= ~GTF_CALL_POP_ARGS;

#elif defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)

    // x86 classic codegen doesn't require any morphing

    // For the helper-assisted tail calls, we need to push all the arguments
    // into a single list, and then add a few extra at the beginning or end.
    //
    // For AMD64, the tailcall helper (JIT_TailCall) is defined as:
    //
    //      JIT_TailCall(void* copyRoutine, void* callTarget, <function args>)
    //
    // We need to add "copyRoutine" and "callTarget" extra params at the beginning.
    // But callTarget is determined by the Lower phase. Therefore, we add a placeholder arg
    // for callTarget here which will be replaced later with callTarget in tail call lowering.
    //
    // For x86, the tailcall helper is defined as:
    //
    //      JIT_TailCall(<function args>, int numberOfOldStackArgsWords, int numberOfNewStackArgsWords, int flags, void*
    //      callTarget)
    //
    // Note that the special arguments are on the stack, whereas the function arguments follow
    // the normal convention: there might be register arguments in ECX and EDX. The stack will
    // look like (highest address at the top):
    //      first normal stack argument
    //      ...
    //      last normal stack argument
    //      numberOfOldStackArgs
    //      numberOfNewStackArgs
    //      flags
    //      callTarget
    //
    // Each special arg is 4 bytes.
    //
    // 'flags' is a bitmask where:
    //      1 == restore callee-save registers (EDI,ESI,EBX). The JIT always saves all
    //          callee-saved registers for tailcall functions. Note that the helper assumes
    //          that the callee-saved registers live immediately below EBP, and must have been
    //          pushed in this order: EDI, ESI, EBX.
    //      2 == call target is a virtual stub dispatch.
    //
    // The x86 tail call helper lives in VM\i386\jithelp.asm. See that function for more details
    // on the custom calling convention.

    // Check for PInvoke call types that we don't handle in codegen yet.
    assert(!call->IsUnmanaged());
    assert(call->IsVirtual() || (call->gtCallType != CT_INDIRECT) || (call->gtCallCookie == nullptr));

    // Don't support tail calling helper methods
    assert(call->gtCallType != CT_HELPER);

    // We come this route only for tail prefixed calls that cannot be dispatched as
    // fast tail calls
    assert(!call->IsImplicitTailCall());
    assert(!fgCanFastTailCall(call));

    // First move the 'this' pointer (if any) onto the regular arg list. We do this because
    // we are going to prepend special arguments onto the argument list (for non-x86 platforms),
    // and thus shift where the 'this' pointer will be passed to a later argument slot. In
    // addition, for all platforms, we are going to change the call into a helper call. Our code
    // generation code for handling calls to helpers does not handle 'this' pointers. So, when we
    // do this transformation, we must explicitly create a null 'this' pointer check, if required,
    // since special 'this' pointer handling will no longer kick in.
    //
    // Some call types, such as virtual vtable calls, require creating a call address expression
    // that involves the "this" pointer. Lowering will sometimes create an embedded statement
    // to create a temporary that is assigned to the "this" pointer expression, and then use
    // that temp to create the call address expression. This temp creation embedded statement
    // will occur immediately before the "this" pointer argument, and then will be used for both
    // the "this" pointer argument as well as the call address expression. In the normal ordering,
    // the embedded statement establishing the "this" pointer temp will execute before both uses
    // of the temp. However, for tail calls via a helper, we move the "this" pointer onto the
    // normal call argument list, and insert a placeholder which will hold the call address
    // expression. For non-x86, things are ok, because the order of execution of these is not
    // altered. However, for x86, the call address expression is inserted as the *last* argument
    // in the argument list, *after* the "this" pointer. It will be put on the stack, and be
    // evaluated first. To ensure we don't end up with out-of-order temp definition and use,
    // for those cases where call lowering creates an embedded form temp of "this", we will
    // create a temp here, early, that will later get morphed correctly.

    if (call->gtCallObjp)
    {
        GenTreePtr thisPtr = nullptr;
        GenTreePtr objp    = call->gtCallObjp;
        call->gtCallObjp   = nullptr;

#ifdef _TARGET_X86_
        if ((call->IsDelegateInvoke() || call->IsVirtualVtable()) && !objp->IsLocal())
        {
            // tmp = "this"
            unsigned   lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
            GenTreePtr asg    = gtNewTempAssign(lclNum, objp);

            // COMMA(tmp = "this", tmp)
            var_types  vt  = objp->TypeGet();
            GenTreePtr tmp = gtNewLclvNode(lclNum, vt);
            thisPtr        = gtNewOperNode(GT_COMMA, vt, asg, tmp);

            objp = thisPtr;
        }
#endif // _TARGET_X86_

        if (call->NeedsNullCheck())
        {
            // clone "this" if "this" has no side effects.
            if ((thisPtr == nullptr) && !(objp->gtFlags & GTF_SIDE_EFFECT))
            {
                thisPtr = gtClone(objp, true);
            }

            var_types vt = objp->TypeGet();
            if (thisPtr == nullptr)
            {
                // create a temp if either "this" has side effects or "this" is too complex to clone.

                // tmp = "this"
                unsigned   lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
                GenTreePtr asg    = gtNewTempAssign(lclNum, objp);

                // COMMA(tmp = "this", deref(tmp))
                GenTreePtr tmp = gtNewLclvNode(lclNum, vt);
                GenTreePtr ind = gtNewOperNode(GT_IND, TYP_INT, tmp);
                asg            = gtNewOperNode(GT_COMMA, TYP_VOID, asg, ind);

                // COMMA(COMMA(tmp = "this", deref(tmp)), tmp)
                thisPtr = gtNewOperNode(GT_COMMA, vt, asg, gtNewLclvNode(lclNum, vt));
            }
            else
            {
                // thisPtr = COMMA(deref("this"), "this")
                GenTreePtr ind = gtNewOperNode(GT_IND, TYP_INT, thisPtr);
                thisPtr        = gtNewOperNode(GT_COMMA, vt, ind, gtClone(objp, true));
            }

            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }
        else
        {
            thisPtr = objp;
        }

        // During rationalization tmp="this" and null check will
        // materialize as embedded stmts in right execution order.
        assert(thisPtr != nullptr);
        call->gtCallArgs = gtNewListNode(thisPtr, call->gtCallArgs);
    }

#if defined(_TARGET_AMD64_)

    // Add the extra VSD parameter to arg list in case of VSD calls.
    // Tail call arg copying thunk will move this extra VSD parameter
    // to R11 before tail calling VSD stub. See CreateTailCallCopyArgsThunk()
    // in Stublinkerx86.cpp for more details.
    CorInfoHelperTailCallSpecialHandling flags = CorInfoHelperTailCallSpecialHandling(0);
    if (call->IsVirtualStub())
    {
        GenTreePtr stubAddrArg;

        flags = CORINFO_TAILCALL_STUB_DISPATCH_ARG;

        if (call->gtCallType == CT_INDIRECT)
        {
            stubAddrArg = gtClone(call->gtCallAddr, true);
            noway_assert(stubAddrArg != nullptr);
        }
        else
        {
            noway_assert((call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT) != 0);

            ssize_t addr = ssize_t(call->gtStubCallStubAddr);
            stubAddrArg  = gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
        }

        // Push the stub address onto the list of arguments
        call->gtCallArgs = gtNewListNode(stubAddrArg, call->gtCallArgs);
    }

    // Now inject a placeholder for the real call target that Lower phase will generate.
    GenTreePtr arg   = gtNewIconNode(0, TYP_I_IMPL);
    call->gtCallArgs = gtNewListNode(arg, call->gtCallArgs);

    // Inject the pointer for the copy routine to be used for struct copying
    noway_assert(call->callSig != nullptr);
    void* pfnCopyArgs = info.compCompHnd->getTailCallCopyArgsThunk(call->callSig, flags);
    arg               = gtNewIconHandleNode(ssize_t(pfnCopyArgs), GTF_ICON_FTN_ADDR);
    call->gtCallArgs  = gtNewListNode(arg, call->gtCallArgs);

#else // !_TARGET_AMD64_

    // Find the end of the argument list. ppArg will point at the last pointer; setting *ppArg will
    // append to the list.
    GenTreeArgList** ppArg = &call->gtCallArgs;
    for (GenTreeArgList* args = call->gtCallArgs; args != nullptr; args = args->Rest())
    {
        ppArg = (GenTreeArgList**)&args->gtOp2;
    }
    assert(ppArg != nullptr);
    assert(*ppArg == nullptr);

    unsigned nOldStkArgsWords =
        (compArgSize - (codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES)) / REGSIZE_BYTES;
    GenTree* arg3 = gtNewIconNode((ssize_t)nOldStkArgsWords, TYP_I_IMPL);
    *ppArg        = gtNewListNode(arg3, nullptr); // numberOfOldStackArgs
    ppArg         = (GenTreeArgList**)&((*ppArg)->gtOp2);

    // Inject a placeholder for the count of outgoing stack arguments that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg2 = gtNewIconNode(9, TYP_I_IMPL);
    *ppArg        = gtNewListNode(arg2, nullptr); // numberOfNewStackArgs
    ppArg         = (GenTreeArgList**)&((*ppArg)->gtOp2);

    // Inject a placeholder for the flags.
    // The constant will be replaced.
    GenTree* arg1 = gtNewIconNode(8, TYP_I_IMPL);
    *ppArg        = gtNewListNode(arg1, nullptr);
    ppArg         = (GenTreeArgList**)&((*ppArg)->gtOp2);

    // Inject a placeholder for the real call target that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg0 = gtNewIconNode(7, TYP_I_IMPL);
    *ppArg        = gtNewListNode(arg0, nullptr);

#endif // !_TARGET_AMD64_

    // It is now a varargs tail call dispatched via helper.
    call->gtCallMoreFlags |= GTF_CALL_M_VARARGS | GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_HELPER;
    call->gtFlags &= ~GTF_CALL_POP_ARGS;

#endif // _TARGET_*

    JITDUMP("fgMorphTailCall (after):\n");
    DISPTREE(call);
}

//------------------------------------------------------------------------------
// fgMorphRecursiveFastTailCallIntoLoop : Transform a recursive fast tail call into a loop.
//
//
// Arguments:
//    block  - basic block ending with a recursive fast tail call
//    recursiveTailCall - recursive tail call to transform
//
// Notes:
//    The legality of the transformation is ensured by the checks in endsWithTailCallConvertibleToLoop.

void Compiler::fgMorphRecursiveFastTailCallIntoLoop(BasicBlock* block, GenTreeCall* recursiveTailCall)
{
    assert(recursiveTailCall->IsTailCallConvertibleToLoop());
    GenTreePtr last = block->lastStmt();
    assert(recursiveTailCall == last->gtStmt.gtStmtExpr);

    // Transform recursive tail call into a loop.

    GenTreePtr earlyArgInsertionPoint = last;
    IL_OFFSETX callILOffset           = last->gtStmt.gtStmtILoffsx;

    // Hoist arg setup statement for the 'this' argument.
    GenTreePtr thisArg = recursiveTailCall->gtCallObjp;
    if (thisArg && !thisArg->IsNothingNode() && !thisArg->IsArgPlaceHolderNode())
    {
        GenTreePtr thisArgStmt = gtNewStmt(thisArg, callILOffset);
        fgInsertStmtBefore(block, earlyArgInsertionPoint, thisArgStmt);
    }

    // All arguments whose trees may involve caller parameter local variables need to be assigned to temps first;
    // then the temps need to be assigned to the method parameters. This is done so that the caller
    // parameters are not re-assigned before call arguments depending on them  are evaluated.
    // tmpAssignmentInsertionPoint and paramAssignmentInsertionPoint keep track of
    // where the next temp or parameter assignment should be inserted.

    // In the example below the first call argument (arg1 - 1) needs to be assigned to a temp first
    // while the second call argument (const 1) doesn't.
    // Basic block before tail recursion elimination:
    //  ***** BB04, stmt 1 (top level)
    //  [000037] ------------             *  stmtExpr  void  (top level) (IL 0x00A...0x013)
    //  [000033] --C - G------ - \--*  call      void   RecursiveMethod
    //  [000030] ------------ | / --*  const     int - 1
    //  [000031] ------------arg0 in rcx + --*  +int
    //  [000029] ------------ | \--*  lclVar    int    V00 arg1
    //  [000032] ------------arg1 in rdx    \--*  const     int    1
    //
    //
    //  Basic block after tail recursion elimination :
    //  ***** BB04, stmt 1 (top level)
    //  [000051] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000030] ------------ | / --*  const     int - 1
    //  [000031] ------------ | / --*  +int
    //  [000029] ------------ | | \--*  lclVar    int    V00 arg1
    //  [000050] - A----------             \--* = int
    //  [000049] D------N----                \--*  lclVar    int    V02 tmp0
    //
    //  ***** BB04, stmt 2 (top level)
    //  [000055] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000052] ------------ | / --*  lclVar    int    V02 tmp0
    //  [000054] - A----------             \--* = int
    //  [000053] D------N----                \--*  lclVar    int    V00 arg0

    //  ***** BB04, stmt 3 (top level)
    //  [000058] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000032] ------------ | / --*  const     int    1
    //  [000057] - A----------             \--* = int
    //  [000056] D------N----                \--*  lclVar    int    V01 arg1

    GenTreePtr tmpAssignmentInsertionPoint   = last;
    GenTreePtr paramAssignmentInsertionPoint = last;

    // Process early args. They may contain both setup statements for late args and actual args.
    // Early args don't include 'this' arg. We need to account for that so that the call to gtArgEntryByArgNum
    // below has the correct second argument.
    int earlyArgIndex = (thisArg == nullptr) ? 0 : 1;
    for (GenTreeArgList* earlyArgs = recursiveTailCall->gtCallArgs; earlyArgs != nullptr;
         (earlyArgIndex++, earlyArgs = earlyArgs->Rest()))
    {
        GenTreePtr earlyArg = earlyArgs->Current();
        if (!earlyArg->IsNothingNode() && !earlyArg->IsArgPlaceHolderNode())
        {
            if ((earlyArg->gtFlags & GTF_LATE_ARG) != 0)
            {
                // This is a setup node so we need to hoist it.
                GenTreePtr earlyArgStmt = gtNewStmt(earlyArg, callILOffset);
                fgInsertStmtBefore(block, earlyArgInsertionPoint, earlyArgStmt);
            }
            else
            {
                // This is an actual argument that needs to be assigned to the corresponding caller parameter.
                fgArgTabEntryPtr curArgTabEntry = gtArgEntryByArgNum(recursiveTailCall, earlyArgIndex);
                GenTreePtr       paramAssignStmt =
                    fgAssignRecursiveCallArgToCallerParam(earlyArg, curArgTabEntry, block, callILOffset,
                                                          tmpAssignmentInsertionPoint, paramAssignmentInsertionPoint);
                if ((tmpAssignmentInsertionPoint == last) && (paramAssignStmt != nullptr))
                {
                    // All temp assignments will happen before the first param assignment.
                    tmpAssignmentInsertionPoint = paramAssignStmt;
                }
            }
        }
    }

    // Process late args.
    int lateArgIndex = 0;
    for (GenTreeArgList* lateArgs = recursiveTailCall->gtCallLateArgs; lateArgs != nullptr;
         (lateArgIndex++, lateArgs = lateArgs->Rest()))
    {
        // A late argument is an actual argument that needs to be assigned to the corresponding caller's parameter.
        GenTreePtr       lateArg        = lateArgs->Current();
        fgArgTabEntryPtr curArgTabEntry = gtArgEntryByLateArgIndex(recursiveTailCall, lateArgIndex);
        GenTreePtr       paramAssignStmt =
            fgAssignRecursiveCallArgToCallerParam(lateArg, curArgTabEntry, block, callILOffset,
                                                  tmpAssignmentInsertionPoint, paramAssignmentInsertionPoint);

        if ((tmpAssignmentInsertionPoint == last) && (paramAssignStmt != nullptr))
        {
            // All temp assignments will happen before the first param assignment.
            tmpAssignmentInsertionPoint = paramAssignStmt;
        }
    }

    // If the method has starg.s 0 or ldarga.s 0 a special local (lvaArg0Var) is created so that
    // compThisArg stays immutable. Normally it's assigned in fgFirstBBScratch block. Since that
    // block won't be in the loop (it's assumed to have no predecessors), we need to update the special local here.
    if (!info.compIsStatic && (lvaArg0Var != info.compThisArg))
    {
        var_types  thisType           = lvaTable[info.compThisArg].TypeGet();
        GenTreePtr arg0               = gtNewLclvNode(lvaArg0Var, thisType);
        GenTreePtr arg0Assignment     = gtNewAssignNode(arg0, gtNewLclvNode(info.compThisArg, thisType));
        GenTreePtr arg0AssignmentStmt = gtNewStmt(arg0Assignment, callILOffset);
        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, arg0AssignmentStmt);
    }

    // Remove the call
    fgRemoveStmt(block, last);

    // Set the loop edge.
    block->bbJumpKind = BBJ_ALWAYS;
    block->bbJumpDest = fgFirstBBisScratch() ? fgFirstBB->bbNext : fgFirstBB;
    fgAddRefPred(block->bbJumpDest, block);
    block->bbFlags &= ~BBF_HAS_JMP;
}

//------------------------------------------------------------------------------
// fgAssignRecursiveCallArgToCallerParam : Assign argument to a recursive call to the corresponding caller parameter.
//
//
// Arguments:
//    arg  -  argument to assign
//    argTabEntry  -  argument table entry corresponding to arg
//    block  --- basic block the call is in
//    callILOffset  -  IL offset of the call
//    tmpAssignmentInsertionPoint  -  tree before which temp assignment should be inserted (if necessary)
//    paramAssignmentInsertionPoint  -  tree before which parameter assignment should be inserted
//
// Return Value:
//    parameter assignment statement if one was inserted; nullptr otherwise.

GenTreePtr Compiler::fgAssignRecursiveCallArgToCallerParam(GenTreePtr       arg,
                                                           fgArgTabEntryPtr argTabEntry,
                                                           BasicBlock*      block,
                                                           IL_OFFSETX       callILOffset,
                                                           GenTreePtr       tmpAssignmentInsertionPoint,
                                                           GenTreePtr       paramAssignmentInsertionPoint)
{
    // Call arguments should be assigned to temps first and then the temps should be assigned to parameters because
    // some argument trees may reference parameters directly.

    GenTreePtr argInTemp             = nullptr;
    unsigned   originalArgNum        = argTabEntry->argNum;
    bool       needToAssignParameter = true;

    // TODO-CQ: enable calls with struct arguments passed in registers.
    noway_assert(!varTypeIsStruct(arg->TypeGet()));

    if ((argTabEntry->isTmp) || arg->IsCnsIntOrI() || arg->IsCnsFltOrDbl())
    {
        // The argument is already assigned to a temp or is a const.
        argInTemp = arg;
    }
    else if (arg->OperGet() == GT_LCL_VAR)
    {
        unsigned   lclNum = arg->AsLclVar()->gtLclNum;
        LclVarDsc* varDsc = &lvaTable[lclNum];
        if (!varDsc->lvIsParam)
        {
            // The argument is a non-parameter local so it doesn't need to be assigned to a temp.
            argInTemp = arg;
        }
        else if (lclNum == originalArgNum)
        {
            // The argument is the same parameter local that we were about to assign so
            // we can skip the assignment.
            needToAssignParameter = false;
        }
    }

    // TODO: We don't need temp assignments if we can prove that the argument tree doesn't involve
    // any caller parameters. Some common cases are handled above but we may be able to eliminate
    // more temp assignments.

    GenTreePtr paramAssignStmt = nullptr;
    if (needToAssignParameter)
    {
        if (argInTemp == nullptr)
        {
            // The argument is not assigned to a temp. We need to create a new temp and insert an assignment.
            // TODO: we can avoid a temp assignment if we can prove that the argument tree
            // doesn't involve any caller parameters.
            unsigned   tmpNum        = lvaGrabTemp(true DEBUGARG("arg temp"));
            GenTreePtr tempSrc       = arg;
            GenTreePtr tempDest      = gtNewLclvNode(tmpNum, tempSrc->gtType);
            GenTreePtr tmpAssignNode = gtNewAssignNode(tempDest, tempSrc);
            GenTreePtr tmpAssignStmt = gtNewStmt(tmpAssignNode, callILOffset);
            fgInsertStmtBefore(block, tmpAssignmentInsertionPoint, tmpAssignStmt);
            argInTemp = gtNewLclvNode(tmpNum, tempSrc->gtType);
        }

        // Now assign the temp to the parameter.
        LclVarDsc* paramDsc = lvaTable + originalArgNum;
        assert(paramDsc->lvIsParam);
        GenTreePtr paramDest       = gtNewLclvNode(originalArgNum, paramDsc->lvType);
        GenTreePtr paramAssignNode = gtNewAssignNode(paramDest, argInTemp);
        paramAssignStmt            = gtNewStmt(paramAssignNode, callILOffset);

        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, paramAssignStmt);
    }
    return paramAssignStmt;
}

/*****************************************************************************
 *
 *  Transform the given GT_CALL tree for code generation.
 */

GenTreePtr Compiler::fgMorphCall(GenTreeCall* call)
{
    if (call->CanTailCall())
    {
        // It should either be an explicit (i.e. tail prefixed) or an implicit tail call
        assert(call->IsTailPrefixedCall() ^ call->IsImplicitTailCall());

        // It cannot be an inline candidate
        assert(!call->IsInlineCandidate());

        const char* szFailReason   = nullptr;
        bool        hasStructParam = false;
        if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
        {
            szFailReason = "Might turn into an intrinsic";
        }

        if (opts.compNeedSecurityCheck)
        {
            szFailReason = "Needs security check";
        }
        else if (compLocallocUsed)
        {
            szFailReason = "Localloc used";
        }
#ifdef _TARGET_AMD64_
        // Needed for Jit64 compat.
        // In future, enabling tail calls from methods that need GS cookie check
        // would require codegen side work to emit GS cookie check before a tail
        // call.
        else if (getNeedsGSSecurityCookie())
        {
            szFailReason = "GS Security cookie check";
        }
#endif
#ifdef DEBUG
        // DDB 99324: Just disable tailcall under compGcChecks stress mode.
        else if (opts.compGcChecks)
        {
            szFailReason = "GcChecks";
        }
#endif
#if FEATURE_TAILCALL_OPT
        else
        {
            // We are still not sure whether it can be a tail call. Because, when converting
            // a call to an implicit tail call, we must check that there are no locals with
            // their address taken.  If this is the case, we have to assume that the address
            // has been leaked and the current stack frame must live until after the final
            // call.

            // Verify that none of vars has lvHasLdAddrOp or lvAddrExposed bit set. Note
            // that lvHasLdAddrOp is much more conservative.  We cannot just base it on
            // lvAddrExposed alone since it is not guaranteed to be set on all VarDscs
            // during morph stage. The reason for also checking lvAddrExposed is that in case
            // of vararg methods user args are marked as addr exposed but not lvHasLdAddrOp.
            // The combination of lvHasLdAddrOp and lvAddrExposed though conservative allows us
            // never to be incorrect.
            //
            // TODO-Throughput: have a compiler level flag to indicate whether method has vars whose
            // address is taken. Such a flag could be set whenever lvHasLdAddrOp or LvAddrExposed
            // is set. This avoids the need for iterating through all lcl vars of the current
            // method.  Right now throughout the code base we are not consistently using 'set'
            // method to set lvHasLdAddrOp and lvAddrExposed flags.
            unsigned   varNum;
            LclVarDsc* varDsc;
            bool       hasAddrExposedVars     = false;
            bool       hasStructPromotedParam = false;
            bool       hasPinnedVars          = false;

            for (varNum = 0, varDsc = lvaTable; varNum < lvaCount; varNum++, varDsc++)
            {
                // If the method is marked as an explicit tail call we will skip the
                // following three hazard checks.
                // We still must check for any struct parameters and set 'hasStructParam'
                // so that we won't transform the recursive tail call into a loop.
                //
                if (call->IsImplicitTailCall())
                {
                    if (varDsc->lvHasLdAddrOp || varDsc->lvAddrExposed)
                    {
                        hasAddrExposedVars = true;
                        break;
                    }
                    if (varDsc->lvPromoted && varDsc->lvIsParam)
                    {
                        hasStructPromotedParam = true;
                        break;
                    }
                    if (varDsc->lvPinned)
                    {
                        // A tail call removes the method from the stack, which means the pinning
                        // goes away for the callee.  We can't allow that.
                        hasPinnedVars = true;
                        break;
                    }
                }
                if (varTypeIsStruct(varDsc->TypeGet()) && varDsc->lvIsParam)
                {
                    hasStructParam = true;
                    // This prevents transforming a recursive tail call into a loop
                    // but doesn't prevent tail call optimization so we need to
                    // look at the rest of parameters.
                    continue;
                }
            }

            if (hasAddrExposedVars)
            {
                szFailReason = "Local address taken";
            }
            if (hasStructPromotedParam)
            {
                szFailReason = "Has Struct Promoted Param";
            }
            if (hasPinnedVars)
            {
                szFailReason = "Has Pinned Vars";
            }
        }
#endif // FEATURE_TAILCALL_OPT

        if (varTypeIsStruct(call))
        {
            fgFixupStructReturn(call);
        }

        var_types callType = call->TypeGet();

        // We have to ensure to pass the incoming retValBuf as the
        // outgoing one. Using a temp will not do as this function will
        // not regain control to do the copy.

        if (info.compRetBuffArg != BAD_VAR_NUM)
        {
            noway_assert(callType == TYP_VOID);
            GenTreePtr retValBuf = call->gtCallArgs->gtOp.gtOp1;
            if (retValBuf->gtOper != GT_LCL_VAR || retValBuf->gtLclVarCommon.gtLclNum != info.compRetBuffArg)
            {
                szFailReason = "Need to copy return buffer";
            }
        }

        // If this is an opportunistic tail call and cannot be dispatched as
        // fast tail call, go the non-tail call route.  This is done for perf
        // reason.
        //
        // Avoid the cost of determining whether can be dispatched as fast tail
        // call if we already know that tail call cannot be honored for other
        // reasons.
        bool canFastTailCall = false;
        if (szFailReason == nullptr)
        {
            canFastTailCall = fgCanFastTailCall(call);
            if (!canFastTailCall)
            {
                // Implicit or opportunistic tail calls are always dispatched via fast tail call
                // mechanism and never via tail call helper for perf.
                if (call->IsImplicitTailCall())
                {
                    szFailReason = "Opportunistic tail call cannot be dispatched as epilog+jmp";
                }
#ifndef LEGACY_BACKEND
                else if (!call->IsVirtualStub() && call->HasNonStandardAddedArgs(this))
                {
                    // If we are here, it means that the call is an explicitly ".tail" prefixed and cannot be
                    // dispatched as a fast tail call.

                    // Methods with non-standard args will have indirection cell or cookie param passed
                    // in callee trash register (e.g. R11). Tail call helper doesn't preserve it before
                    // tail calling the target method and hence ".tail" prefix on such calls needs to be
                    // ignored.
                    //
                    // Exception to the above rule: although Virtual Stub Dispatch (VSD) calls require
                    // extra stub param (e.g. in R11 on Amd64), they can still be called via tail call helper.
                    // This is done by by adding stubAddr as an additional arg before the original list of
                    // args. For more details see fgMorphTailCall() and CreateTailCallCopyArgsThunk()
                    // in Stublinkerx86.cpp.
                    szFailReason = "Method with non-standard args passed in callee trash register cannot be tail "
                                   "called via helper";
                }
#ifdef _TARGET_ARM64_
                else
                {
                    // NYI - TAILCALL_RECURSIVE/TAILCALL_HELPER.
                    // So, bail out if we can't make fast tail call.
                    szFailReason = "Non-qualified fast tail call";
                }
#endif
#endif // LEGACY_BACKEND
            }
        }

        // Clear these flags before calling fgMorphCall() to avoid recursion.
        bool isTailPrefixed = call->IsTailPrefixedCall();
        call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;

#if FEATURE_TAILCALL_OPT
        call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
#endif

#ifdef FEATURE_PAL
        if (!canFastTailCall && szFailReason == nullptr)
        {
            szFailReason = "Non fast tail calls disabled for PAL based systems.";
        }
#endif // FEATURE_PAL

        if (szFailReason != nullptr)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("\nRejecting tail call late for call ");
                printTreeID(call);
                printf(": %s\n", szFailReason);
            }
#endif

            // for non user funcs, we have no handles to report
            info.compCompHnd->reportTailCallDecision(nullptr,
                                                     (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                                     isTailPrefixed, TAILCALL_FAIL, szFailReason);

            goto NO_TAIL_CALL;
        }

#if !FEATURE_TAILCALL_OPT_SHARED_RETURN
        // We enable shared-ret tail call optimization for recursive calls even if
        // FEATURE_TAILCALL_OPT_SHARED_RETURN is not defined.
        if (gtIsRecursiveCall(call))
#endif
        {
            // Many tailcalls will have call and ret in the same block, and thus be BBJ_RETURN,
            // but if the call falls through to a ret, and we are doing a tailcall, change it here.
            if (compCurBB->bbJumpKind != BBJ_RETURN)
            {
                compCurBB->bbJumpKind = BBJ_RETURN;
            }
        }

        // Set this flag before calling fgMorphCall() to prevent inlining this call.
        call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL;

        bool fastTailCallToLoop = false;
#if FEATURE_TAILCALL_OPT
        // TODO-CQ: enable the transformation when the method has a struct parameter that can be passed in a register
        // or return type is a struct that can be passed in a register.
        //
        // TODO-CQ: if the method being compiled requires generic context reported in gc-info (either through
        // hidden generic context param or through keep alive thisptr), then while transforming a recursive
        // call to such a method requires that the generic context stored on stack slot be updated.  Right now,
        // fgMorphRecursiveFastTailCallIntoLoop() is not handling update of generic context while transforming
        // a recursive call into a loop.  Another option is to modify gtIsRecursiveCall() to check that the
        // generic type parameters of both caller and callee generic method are the same.
        if (opts.compTailCallLoopOpt && canFastTailCall && gtIsRecursiveCall(call) && !lvaReportParamTypeArg() &&
            !lvaKeepAliveAndReportThis() && !call->IsVirtual() && !hasStructParam && !varTypeIsStruct(call->TypeGet()))
        {
            call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL_TO_LOOP;
            fastTailCallToLoop = true;
        }
#endif

        // Do some target-specific transformations (before we process the args, etc.)
        // This is needed only for tail prefixed calls that cannot be dispatched as
        // fast calls.
        if (!canFastTailCall)
        {
            fgMorphTailCall(call);
        }

        // Implementation note : If we optimize tailcall to do a direct jump
        // to the target function (after stomping on the return address, etc),
        // without using CORINFO_HELP_TAILCALL, we have to make certain that
        // we don't starve the hijacking logic (by stomping on the hijacked
        // return address etc).

        // At this point, we are committed to do the tailcall.
        compTailCallUsed = true;

        CorInfoTailCall tailCallResult;

        if (fastTailCallToLoop)
        {
            tailCallResult = TAILCALL_RECURSIVE;
        }
        else if (canFastTailCall)
        {
            tailCallResult = TAILCALL_OPTIMIZED;
        }
        else
        {
            tailCallResult = TAILCALL_HELPER;
        }

        // for non user funcs, we have no handles to report
        info.compCompHnd->reportTailCallDecision(nullptr,
                                                 (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                                 isTailPrefixed, tailCallResult, nullptr);

        // As we will actually call CORINFO_HELP_TAILCALL, set the callTyp to TYP_VOID.
        // to avoid doing any extra work for the return value.
        call->gtType = TYP_VOID;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nGTF_CALL_M_TAILCALL bit set for call ");
            printTreeID(call);
            printf("\n");
            if (fastTailCallToLoop)
            {
                printf("\nGTF_CALL_M_TAILCALL_TO_LOOP bit set for call ");
                printTreeID(call);
                printf("\n");
            }
        }
#endif

        GenTreePtr stmtExpr = fgMorphStmt->gtStmt.gtStmtExpr;

#ifdef DEBUG
        // Tail call needs to be in one of the following IR forms
        //    Either a call stmt or
        //    GT_RETURN(GT_CALL(..)) or
        //    var = call
        noway_assert((stmtExpr->gtOper == GT_CALL && stmtExpr == call) ||
                     (stmtExpr->gtOper == GT_RETURN &&
                      (stmtExpr->gtOp.gtOp1 == call || stmtExpr->gtOp.gtOp1->gtOp.gtOp1 == call)) ||
                     (stmtExpr->gtOper == GT_ASG && stmtExpr->gtOp.gtOp2 == call));
#endif

        // For void calls, we would have created a GT_CALL in the stmt list.
        // For non-void calls, we would have created a GT_RETURN(GT_CAST(GT_CALL)).
        // For calls returning structs, we would have a void call, followed by a void return.
        // For debuggable code, it would be an assignment of the call to a temp
        // We want to get rid of any of this extra trees, and just leave
        // the call.
        GenTreePtr nextMorphStmt = fgMorphStmt->gtNext;

#ifdef _TARGET_AMD64_
        // Legacy Jit64 Compat:
        // There could be any number of GT_NOPs between tail call and GT_RETURN.
        // That is tail call pattern could be one of the following:
        //  1) tail.call, nop*, ret
        //  2) tail.call, nop*, pop, nop*, ret
        //  3) var=tail.call, nop*, ret(var)
        //  4) var=tail.call, nop*, pop, ret
        //
        // See impIsTailCallILPattern() for details on tail call IL patterns
        // that are supported.
        if ((stmtExpr->gtOper == GT_CALL) || (stmtExpr->gtOper == GT_ASG))
        {
            // First delete all GT_NOPs after the call
            GenTreePtr morphStmtToRemove = nullptr;
            while (nextMorphStmt != nullptr)
            {
                GenTreePtr nextStmtExpr = nextMorphStmt->gtStmt.gtStmtExpr;
                if (!nextStmtExpr->IsNothingNode())
                {
                    break;
                }

                morphStmtToRemove = nextMorphStmt;
                nextMorphStmt     = morphStmtToRemove->gtNext;
                fgRemoveStmt(compCurBB, morphStmtToRemove);
            }

            // Check to see if there is a pop.
            // Since tail call is honored, we can get rid of the stmt corresponding to pop.
            if (nextMorphStmt != nullptr && nextMorphStmt->gtStmt.gtStmtExpr->gtOper != GT_RETURN)
            {
                // Note that pop opcode may or may not result in a new stmt (for details see
                // impImportBlockCode()). Hence, it is not possible to assert about the IR
                // form generated by pop but pop tree must be side-effect free so that we can
                // delete it safely.
                GenTreePtr popStmt = nextMorphStmt;
                nextMorphStmt      = nextMorphStmt->gtNext;

                noway_assert((popStmt->gtStmt.gtStmtExpr->gtFlags & GTF_ALL_EFFECT) == 0);
                fgRemoveStmt(compCurBB, popStmt);
            }

            // Next delete any GT_NOP nodes after pop
            while (nextMorphStmt != nullptr)
            {
                GenTreePtr nextStmtExpr = nextMorphStmt->gtStmt.gtStmtExpr;
                if (!nextStmtExpr->IsNothingNode())
                {
                    break;
                }

                morphStmtToRemove = nextMorphStmt;
                nextMorphStmt     = morphStmtToRemove->gtNext;
                fgRemoveStmt(compCurBB, morphStmtToRemove);
            }
        }
#endif // _TARGET_AMD64_

        // Delete GT_RETURN  if any
        if (nextMorphStmt != nullptr)
        {
            GenTreePtr retExpr = nextMorphStmt->gtStmt.gtStmtExpr;
            noway_assert(retExpr->gtOper == GT_RETURN);

            // If var=call, then the next stmt must be a GT_RETURN(TYP_VOID) or GT_RETURN(var).
            // This can occur if impSpillStackEnsure() has introduced an assignment to a temp.
            if (stmtExpr->gtOper == GT_ASG && info.compRetType != TYP_VOID)
            {
                noway_assert(stmtExpr->gtGetOp1()->OperIsLocal());
                noway_assert(stmtExpr->gtGetOp1()->AsLclVarCommon()->gtLclNum ==
                             retExpr->gtGetOp1()->AsLclVarCommon()->gtLclNum);
            }

            fgRemoveStmt(compCurBB, nextMorphStmt);
        }

        fgMorphStmt->gtStmt.gtStmtExpr = call;

        // Tail call via helper: The VM can't use return address hijacking if we're
        // not going to return and the helper doesn't have enough info to safely poll,
        // so we poll before the tail call, if the block isn't already safe.  Since
        // tail call via helper is a slow mechanism it doen't matter whether we emit
        // GC poll.  This is done to be in parity with Jit64. Also this avoids GC info
        // size increase if all most all methods are expected to be tail calls (e.g. F#).
        //
        // Note that we can avoid emitting GC-poll if we know that the current BB is
        // dominated by a Gc-SafePoint block.  But we don't have dominator info at this
        // point.  One option is to just add a place holder node for GC-poll (e.g. GT_GCPOLL)
        // here and remove it in lowering if the block is dominated by a GC-SafePoint.  For
        // now it not clear whether optimizing slow tail calls is worth the effort.  As a
        // low cost check, we check whether the first and current basic blocks are
        // GC-SafePoints.
        //
        // Fast Tail call as epilog+jmp - No need to insert GC-poll. Instead, fgSetBlockOrder()
        // is going to mark the method as fully interruptible if the block containing this tail
        // call is reachable without executing any call.
        if (canFastTailCall || (fgFirstBB->bbFlags & BBF_GC_SAFE_POINT) || (compCurBB->bbFlags & BBF_GC_SAFE_POINT) ||
            !fgCreateGCPoll(GCPOLL_INLINE, compCurBB))
        {
            // We didn't insert a poll block, so we need to morph the call now
            // (Normally it will get morphed when we get to the split poll block)
            GenTreePtr temp = fgMorphCall(call);
            noway_assert(temp == call);
        }

        // Tail call via helper: we just call CORINFO_HELP_TAILCALL, and it jumps to
        // the target. So we don't need an epilog - just like CORINFO_HELP_THROW.
        //
        // Fast tail call: in case of fast tail calls, we need a jmp epilog and
        // hence mark it as BBJ_RETURN with BBF_JMP flag set.
        noway_assert(compCurBB->bbJumpKind == BBJ_RETURN);

        if (canFastTailCall)
        {
            compCurBB->bbFlags |= BBF_HAS_JMP;
        }
        else
        {
            compCurBB->bbJumpKind = BBJ_THROW;
        }

        // For non-void calls, we return a place holder which will be
        // used by the parent GT_RETURN node of this call.

        GenTree* result = call;
        if (callType != TYP_VOID && info.compRetType != TYP_VOID)
        {
#ifdef FEATURE_HFA
            // Return a dummy node, as the return is already removed.
            if (callType == TYP_STRUCT)
            {
                // This is a HFA, use float 0.
                callType = TYP_FLOAT;
            }
#elif defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            // Return a dummy node, as the return is already removed.
            if (varTypeIsStruct(callType))
            {
                // This is a register-returned struct. Return a 0.
                // The actual return registers are hacked in lower and the register allocator.
                callType = TYP_INT;
            }
#endif
#ifdef FEATURE_SIMD
            // Return a dummy node, as the return is already removed.
            if (varTypeIsSIMD(callType))
            {
                callType = TYP_DOUBLE;
            }
#endif
            result = gtNewZeroConNode(genActualType(callType));
            result = fgMorphTree(result);
        }

        return result;
    }

NO_TAIL_CALL:

    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) == 0 &&
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_VIRTUAL_FUNC_PTR)
#ifdef FEATURE_READYTORUN_COMPILER
         || call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR)
#endif
             ) &&
        (call == fgMorphStmt->gtStmt.gtStmtExpr))
    {
        // This is call to CORINFO_HELP_VIRTUAL_FUNC_PTR with ignored result.
        // Transform it into a null check.

        GenTreePtr thisPtr = call->gtCallArgs->gtOp.gtOp1;

        GenTreePtr nullCheck = gtNewOperNode(GT_IND, TYP_I_IMPL, thisPtr);
        nullCheck->gtFlags |= GTF_EXCEPT;

        return fgMorphTree(nullCheck);
    }

    noway_assert(call->gtOper == GT_CALL);

    //
    // Only count calls once (only in the global morph phase)
    //
    if (fgGlobalMorph)
    {
        if (call->gtCallType == CT_INDIRECT)
        {
            optCallCount++;
            optIndirectCallCount++;
        }
        else if (call->gtCallType == CT_USER_FUNC)
        {
            optCallCount++;
            if (call->IsVirtual())
            {
                optIndirectCallCount++;
            }
        }
    }

    // Couldn't inline - remember that this BB contains method calls

    // If this is a 'regular' call, mark the basic block as
    // having a call (for computing full interruptibility).
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_AMD64_
    // Amd64 note: If this is a fast tail call then don't count it as a call
    // since we don't insert GC-polls but instead make the method fully GC
    // interruptible.
    if (!call->IsFastTailCall())
#endif
    {
        if (call->gtCallType == CT_INDIRECT)
        {
            compCurBB->bbFlags |= BBF_GC_SAFE_POINT;
        }
        else if (call->gtCallType == CT_USER_FUNC)
        {
            if ((call->gtCallMoreFlags & GTF_CALL_M_NOGCCHECK) == 0)
            {
                compCurBB->bbFlags |= BBF_GC_SAFE_POINT;
            }
        }
        // otherwise we have a CT_HELPER
    }

    // Morph Type.op_Equality and Type.op_Inequality
    // We need to do this before the arguments are morphed
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC))
    {
        CorInfoIntrinsics methodID = info.compCompHnd->getIntrinsicID(call->gtCallMethHnd);

        genTreeOps simpleOp = GT_CALL;
        if (methodID == CORINFO_INTRINSIC_TypeEQ)
        {
            simpleOp = GT_EQ;
        }
        else if (methodID == CORINFO_INTRINSIC_TypeNEQ)
        {
            simpleOp = GT_NE;
        }

        if (simpleOp == GT_EQ || simpleOp == GT_NE)
        {
            noway_assert(call->TypeGet() == TYP_INT);

            // Check for GetClassFromHandle(handle) and obj.GetType() both of which will only return RuntimeType
            // objects. Then if either operand is one of these two calls we can simplify op_Equality/op_Inequality to
            // GT_NE/GT_NE: One important invariance that should never change is that type equivalency is always
            // equivalent to object identity equality for runtime type objects in reflection. This is also reflected
            // in RuntimeTypeHandle::TypeEquals. If this invariance would ever be broken, we need to remove the
            // optimization below.

            GenTreePtr op1 = call->gtCallArgs->gtOp.gtOp1;
            GenTreePtr op2 = call->gtCallArgs->gtOp.gtOp2->gtOp.gtOp1;

            if (gtCanOptimizeTypeEquality(op1) || gtCanOptimizeTypeEquality(op2))
            {
                GenTreePtr compare = gtNewOperNode(simpleOp, TYP_INT, op1, op2);

                // fgMorphSmpOp will further optimize the following patterns:
                //  1. typeof(...) == typeof(...)
                //  2. typeof(...) == obj.GetType()
                return fgMorphTree(compare);
            }
        }
    }

    // Make sure that return buffers containing GC pointers that aren't too large are pointers into the stack.
    GenTreePtr origDest = nullptr; // Will only become non-null if we do the transformation (and thus require
                                   // copy-back).
    unsigned             retValTmpNum = BAD_VAR_NUM;
    CORINFO_CLASS_HANDLE structHnd    = nullptr;
    if (call->HasRetBufArg() &&
        call->gtCallLateArgs == nullptr) // Don't do this if we're re-morphing (which will make late args non-null).
    {
        // We're enforcing the invariant that return buffers pointers (at least for
        // struct return types containing GC pointers) are never pointers into the heap.
        // The large majority of cases are address of local variables, which are OK.
        // Otherwise, allocate a local of the given struct type, pass its address,
        // then assign from that into the proper destination.  (We don't need to do this
        // if we're passing the caller's ret buff arg to the callee, since the caller's caller
        // will maintain the same invariant.)

        GenTreePtr dest = call->gtCallArgs->gtOp.gtOp1;
        assert(dest->OperGet() != GT_ARGPLACE); // If it was, we'd be in a remorph, which we've already excluded above.
        if (dest->gtType == TYP_BYREF && !(dest->OperGet() == GT_ADDR && dest->gtOp.gtOp1->OperGet() == GT_LCL_VAR))
        {
            // We'll exempt helper calls from this, assuming that the helper implementation
            // follows the old convention, and does whatever barrier is required.
            if (call->gtCallType != CT_HELPER)
            {
                structHnd = call->gtRetClsHnd;
                if (info.compCompHnd->isStructRequiringStackAllocRetBuf(structHnd) &&
                    !((dest->OperGet() == GT_LCL_VAR || dest->OperGet() == GT_REG_VAR) &&
                      dest->gtLclVar.gtLclNum == info.compRetBuffArg))
                {
                    origDest = dest;

                    retValTmpNum = lvaGrabTemp(true DEBUGARG("substitute local for ret buff arg"));
                    lvaSetStruct(retValTmpNum, structHnd, true);
                    dest = gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(retValTmpNum, TYP_STRUCT));
                }
            }
        }

        call->gtCallArgs->gtOp.gtOp1 = dest;
    }

    /* Process the "normal" argument list */
    call = fgMorphArgs(call);
    noway_assert(call->gtOper == GT_CALL);

    // Morph stelem.ref helper call to store a null value, into a store into an array without the helper.
    // This needs to be done after the arguments are morphed to ensure constant propagation has already taken place.
    if ((call->gtCallType == CT_HELPER) && (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ARRADDR_ST)))
    {
        GenTreePtr value = gtArgEntryByArgNum(call, 2)->node;

        if (value->IsIntegralConst(0))
        {
            assert(value->OperGet() == GT_CNS_INT);
            GenTreePtr arr   = gtArgEntryByArgNum(call, 0)->node;
            GenTreePtr index = gtArgEntryByArgNum(call, 1)->node;

            arr = gtClone(arr, true);
            if (arr != nullptr)
            {
                index = gtClone(index, true);
                if (index != nullptr)
                {
                    value = gtClone(value);
                    noway_assert(value != nullptr);

                    GenTreePtr nullCheckedArr = impCheckForNullPointer(arr);
                    GenTreePtr arrIndexNode   = gtNewIndexRef(TYP_REF, nullCheckedArr, index);
                    GenTreePtr arrStore       = gtNewAssignNode(arrIndexNode, value);
                    arrStore->gtFlags |= GTF_ASG;

                    return fgMorphTree(arrStore);
                }
            }
        }
    }

    // Optimize get_ManagedThreadId(get_CurrentThread)
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) &&
        info.compCompHnd->getIntrinsicID(call->gtCallMethHnd) == CORINFO_INTRINSIC_GetManagedThreadId)
    {
        noway_assert(origDest == nullptr);
        noway_assert(call->gtCallLateArgs->gtOp.gtOp1 != nullptr);

        GenTreePtr innerCall = call->gtCallLateArgs->gtOp.gtOp1;

        if (innerCall->gtOper == GT_CALL && (innerCall->gtCall.gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) &&
            info.compCompHnd->getIntrinsicID(innerCall->gtCall.gtCallMethHnd) ==
                CORINFO_INTRINSIC_GetCurrentManagedThread)
        {
            // substitute expression with call to helper
            GenTreePtr newCall = gtNewHelperCallNode(CORINFO_HELP_GETCURRENTMANAGEDTHREADID, TYP_INT, 0);
            JITDUMP("get_ManagedThreadId(get_CurrentThread) folding performed\n");
            return fgMorphTree(newCall);
        }
    }

    if (origDest != nullptr)
    {
        GenTreePtr retValVarAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(retValTmpNum, TYP_STRUCT));
        // If the origDest expression was an assignment to a variable, it might be to an otherwise-unused
        // var, which would allow the whole assignment to be optimized away to a NOP.  So in that case, make the
        // origDest into a comma that uses the var.  Note that the var doesn't have to be a temp for this to
        // be correct.
        if (origDest->OperGet() == GT_ASG)
        {
            if (origDest->gtOp.gtOp1->OperGet() == GT_LCL_VAR)
            {
                GenTreePtr var = origDest->gtOp.gtOp1;
                origDest       = gtNewOperNode(GT_COMMA, var->TypeGet(), origDest,
                                         gtNewLclvNode(var->gtLclVar.gtLclNum, var->TypeGet()));
            }
        }
        GenTreePtr copyBlk = gtNewCpObjNode(origDest, retValVarAddr, structHnd, false);
        copyBlk            = fgMorphTree(copyBlk);
        GenTree* result    = gtNewOperNode(GT_COMMA, TYP_VOID, call, copyBlk);
#ifdef DEBUG
        result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
        return result;
    }

    if (call->IsNoReturn())
    {
        //
        // If we know that the call does not return then we can set fgRemoveRestOfBlock
        // to remove all subsequent statements and change the call's basic block to BBJ_THROW.
        // As a result the compiler won't need to preserve live registers across the call.
        //
        // This isn't need for tail calls as there shouldn't be any code after the call anyway.
        // Besides, the tail call code is part of the epilog and converting the block to
        // BBJ_THROW would result in the tail call being dropped as the epilog is generated
        // only for BBJ_RETURN blocks.
        //
        // Currently this doesn't work for non-void callees. Some of the code that handles
        // fgRemoveRestOfBlock expects the tree to have GTF_EXCEPT flag set but call nodes
        // do not have this flag by default. We could add the flag here but the proper solution
        // would be to replace the return expression with a local var node during inlining
        // so the rest of the call tree stays in a separate statement. That statement can then
        // be removed by fgRemoveRestOfBlock without needing to add GTF_EXCEPT anywhere.
        //

        if (!call->IsTailCall() && call->TypeGet() == TYP_VOID)
        {
            fgRemoveRestOfBlock = true;
        }
    }

    return call;
}

/*****************************************************************************
 *
 *  Transform the given GTK_CONST tree for code generation.
 */

GenTreePtr Compiler::fgMorphConst(GenTreePtr tree)
{
    noway_assert(tree->OperKind() & GTK_CONST);

    /* Clear any exception flags or other unnecessary flags
     * that may have been set before folding this node to a constant */

    tree->gtFlags &= ~(GTF_ALL_EFFECT | GTF_REVERSE_OPS);

    if (tree->OperGet() != GT_CNS_STR)
    {
        return tree;
    }

    // TODO-CQ: Do this for compCurBB->isRunRarely(). Doing that currently will
    // guarantee slow performance for that block. Instead cache the return value
    // of CORINFO_HELP_STRCNS and go to cache first giving reasonable perf.

    if (compCurBB->bbJumpKind == BBJ_THROW)
    {
        CorInfoHelpFunc helper = info.compCompHnd->getLazyStringLiteralHelper(tree->gtStrCon.gtScpHnd);
        if (helper != CORINFO_HELP_UNDEF)
        {
            // For un-important blocks, we want to construct the string lazily

            GenTreeArgList* args;
            if (helper == CORINFO_HELP_STRCNS_CURRENT_MODULE)
            {
                args = gtNewArgList(gtNewIconNode(RidFromToken(tree->gtStrCon.gtSconCPX), TYP_INT));
            }
            else
            {
                args = gtNewArgList(gtNewIconNode(RidFromToken(tree->gtStrCon.gtSconCPX), TYP_INT),
                                    gtNewIconEmbScpHndNode(tree->gtStrCon.gtScpHnd));
            }

            tree = gtNewHelperCallNode(helper, TYP_REF, 0, args);
            return fgMorphTree(tree);
        }
    }

    assert(tree->gtStrCon.gtScpHnd == info.compScopeHnd || !IsUninitialized(tree->gtStrCon.gtScpHnd));

    LPVOID         pValue;
    InfoAccessType iat =
        info.compCompHnd->constructStringLiteral(tree->gtStrCon.gtScpHnd, tree->gtStrCon.gtSconCPX, &pValue);

    tree = gtNewStringLiteralNode(iat, pValue);

    return fgMorphTree(tree);
}

/*****************************************************************************
 *
 *  Transform the given GTK_LEAF tree for code generation.
 */

GenTreePtr Compiler::fgMorphLeaf(GenTreePtr tree)
{
    noway_assert(tree->OperKind() & GTK_LEAF);

    if (tree->gtOper == GT_LCL_VAR)
    {
        return fgMorphLocalVar(tree);
    }
#ifdef _TARGET_X86_
    else if (tree->gtOper == GT_LCL_FLD)
    {
        if (info.compIsVarArgs)
        {
            GenTreePtr newTree =
                fgMorphStackArgForVarArgs(tree->gtLclFld.gtLclNum, tree->gtType, tree->gtLclFld.gtLclOffs);
            if (newTree != NULL)
                return newTree;
        }
    }
#endif // _TARGET_X86_
    else if (tree->gtOper == GT_FTN_ADDR)
    {
        CORINFO_CONST_LOOKUP addrInfo;

#ifdef FEATURE_READYTORUN_COMPILER
        if (tree->gtFptrVal.gtEntryPoint.addr != nullptr)
        {
            addrInfo = tree->gtFptrVal.gtEntryPoint;
        }
        else
#endif
        {
            info.compCompHnd->getFunctionFixedEntryPoint(tree->gtFptrVal.gtFptrMethod, &addrInfo);
        }

        // Refer to gtNewIconHandleNode() as the template for constructing a constant handle
        //
        tree->SetOper(GT_CNS_INT);
        tree->gtIntConCommon.SetIconValue(ssize_t(addrInfo.handle));
        tree->gtFlags |= GTF_ICON_FTN_ADDR;

        switch (addrInfo.accessType)
        {
            case IAT_PPVALUE:
                tree = gtNewOperNode(GT_IND, TYP_I_IMPL, tree);
                tree->gtFlags |= GTF_IND_INVARIANT;

                __fallthrough;

            case IAT_PVALUE:
                tree = gtNewOperNode(GT_IND, TYP_I_IMPL, tree);
                break;

            case IAT_VALUE:
                tree = gtNewOperNode(GT_NOP, tree->TypeGet(), tree); // prevents constant folding
                break;

            default:
                noway_assert(!"Unknown addrInfo.accessType");
        }

        return fgMorphTree(tree);
    }

    return tree;
}

void Compiler::fgAssignSetVarDef(GenTreePtr tree)
{
    GenTreeLclVarCommon* lclVarCmnTree;
    bool                 isEntire = false;
    if (tree->DefinesLocal(this, &lclVarCmnTree, &isEntire))
    {
        if (isEntire)
        {
            lclVarCmnTree->gtFlags |= GTF_VAR_DEF;
        }
        else
        {
            // We consider partial definitions to be modeled as uses followed by definitions.
            // This captures the idea that precedings defs are not necessarily made redundant
            // by this definition.
            lclVarCmnTree->gtFlags |= (GTF_VAR_DEF | GTF_VAR_USEASG);
        }
    }
}

GenTreePtr Compiler::fgMorphOneAsgBlockOp(GenTreePtr tree)
{
    genTreeOps oper = tree->gtOper;

    // This must be a block assignment.
    noway_assert(tree->OperIsBlkOp());
    var_types   asgType = TYP_STRUCT;

    GenTreePtr dest        = tree->gtOp.gtOp1->gtOp.gtOp1;   // Dest address
    GenTreePtr src         = tree->gtOp.gtOp1->gtOp.gtOp2;   // Src
    GenTreePtr blkShape    = tree->gtOp.gtOp2;               // [size/clsHnd]
    bool       volatil     = tree->AsBlkOp()->IsVolatile();
    unsigned   destVarNum  = BAD_VAR_NUM;
    LclVarDsc* destVarDsc  = nullptr;
    GenTreePtr result;
    GenTreePtr lclVarTree  = nullptr;
    bool       isCopyBlock = tree->OperIsCopyBlkOp();
    bool       isInitBlock = !isCopyBlock;

    // The dest must be an address
    noway_assert(genActualType(dest->gtType) == TYP_I_IMPL || dest->gtType == TYP_BYREF);

    // For COPYBLK the src must be an address
    noway_assert(!isCopyBlock || (genActualType( src->gtType) == TYP_I_IMPL || src->gtType  == TYP_BYREF));

    // For INITBLK the src must be a TYP_INT
    noway_assert(oper != GT_INITBLK || (genActualType(src->gtType) == TYP_INT));

    // The size must be an integer type
    noway_assert(varTypeIsIntegral(blkShape->gtType));

    CORINFO_CLASS_HANDLE clsHnd;
    size_t               size;

    if (blkShape->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

#ifdef FEATURE_SIMD
    // importer introduces cpblk nodes with src = GT_ADDR(GT_SIMD)
    // The SIMD type in question could be Vector2f which is 8-bytes in size.
    // The below check is to make sure that we don't turn that copyblk
    // into a assignment, since rationalizer logic will transform the
    // copyblk apropriately. Otherwise, the transormation made in this
    // routine will prevent rationalizer logic and we might end up with
    // GT_ADDR(GT_SIMD) node post rationalization, leading to a noway assert
    // in codegen.
    // TODO-1stClassStructs: This is here to preserve old behavior.
    // It should be eliminated.
    if (src->OperGet() == GT_ADDR && src->gtGetOp1()->OperGet() == GT_SIMD)
    {
        return nullptr;
    }
#endif

    if (!blkShape->IsIconHandle())
    {
        clsHnd = NO_CLASS_HANDLE;
        size   = blkShape->gtIntCon.gtIconVal;
    }
    else
    {
        clsHnd = (CORINFO_CLASS_HANDLE)blkShape->gtIntCon.gtIconVal;
        size   = roundUp(info.compCompHnd->getClassSize(clsHnd), sizeof(void*));

        // Since we round up, we are not handling the case where we have a
        // non-dword sized struct with GC pointers.
        // The EE currently does not allow this, but we may change.  Lets assert it
        // just to be safe.
        noway_assert(info.compCompHnd->getClassSize(clsHnd) == size);
    }

    //
    //  See if we can do a simple transformation:
    //
    //          GT_ASG <TYP_size>
    //          /   \
    //      GT_IND GT_IND or CNS_INT
    //         |      |
    //       [dest] [src]
    //

    if (size == REGSIZE_BYTES)
    {
        if (clsHnd == NO_CLASS_HANDLE)
        {
            // A register-sized cpblk can be treated as an integer asignment.
            asgType = TYP_I_IMPL;
        }
        else
        {
            BYTE gcPtr;
            info.compCompHnd->getClassGClayout(clsHnd, &gcPtr);
            asgType = getJitGCType(gcPtr);
        }
    }
    else
    {
        switch (size)
        {
            case 1:
                asgType = TYP_BYTE;
                break;
            case 2:
                asgType = TYP_SHORT;
                break;

#ifdef _TARGET_64BIT_
            case 4:
                asgType = TYP_INT;
                break;
#endif // _TARGET_64BIT_
        }
    }

    // TODO-1stClassStructs: Change this to asgType != TYP_STRUCT.
    if (!varTypeIsStruct(asgType))
    {
        // For initBlk, a non constant source is not going to allow us to fiddle
        // with the bits to create a single assigment.
        noway_assert(size <= REGSIZE_BYTES);

        if (isInitBlock && (src->gtOper != GT_CNS_INT))
        {
            return nullptr;
        }

        bool needsIndirection = true;
        if (impIsAddressInLocal(dest, &lclVarTree))
        {
            destVarNum = lclVarTree->AsLclVarCommon()->gtLclNum;
            destVarDsc = &(lvaTable[destVarNum]);
        }
        if (destVarDsc != nullptr)
        {
#if LOCAL_ASSERTION_PROP
            // Kill everything about dest
            if (optLocalAssertionProp)
            {
                if (optAssertionCount > 0)
                {
                    fgKillDependentAssertions(destVarNum DEBUGARG(tree));
                }
            }
#endif // LOCAL_ASSERTION_PROP

            // A previous incarnation of this code also required the local not to be
            // address-exposed(=taken).  That seems orthogonal to the decision of whether
            // to do field-wise assignments: being address-exposed will cause it to be
            // "dependently" promoted, so it will be in the right memory location.  One possible
            // further reason for avoiding field-wise stores is that the struct might have alignment-induced
            // holes, whose contents could be meaningful in unsafe code.  If we decide that's a valid
            // concern, then we could compromise, and say that address-exposed + fields do not completely cover the
            // memory of the struct prevent field-wise assignments.  Same situation exists for the "src" decision.
            if (varTypeIsStruct(lclVarTree) && (destVarDsc->lvPromoted || destVarDsc->lvIsSIMDType()))
            {
                // Let fgMorphInitBlock handle it.  (Since we'll need to do field-var-wise assignments.)
                return nullptr;
            }
            else if (!varTypeIsFloating(lclVarTree->TypeGet()) &&
                     size == genTypeSize(var_types(lvaTable[destVarNum].lvType)))
            {
                // Use the dest local var directly.
                dest = lclVarTree;
                asgType = destVarDsc->lvType; // Make the type used in the GT_IND node match
                needsIndirection = false;

                // If the block operation had been a write to a local var of a small int type,
                // of the exact size of the small int type, and the var is NormalizeOnStore,
                // we would have labeled it GTF_VAR_USEASG, because the block operation wouldn't
                // have done that normalization.  If we're now making it into an assignment,
                // the NormalizeOnStore will work, and it can be a full def.
                if (destVarDsc->lvNormalizeOnStore())
                {
                    dest->gtFlags &= (~GTF_VAR_USEASG);
                }
            }
            else
            {
                // Could be a non-promoted struct, or a floating point type local, or
                // an int subject to a partial write.  Don't enregister.
                lvaSetVarDoNotEnregister(destVarNum DEBUGARG(DNER_LocalField));

                // Mark the local var tree as a definition point of the local.
                lclVarTree->gtFlags |= GTF_VAR_DEF;
                if (size < destVarDsc->lvExactSize)
                { // If it's not a full-width assignment....
                    lclVarTree->gtFlags |= GTF_VAR_USEASG;
                }
            }
        }

        if (needsIndirection)
        {
            // Check to ensure we are not creating a reducible *(& ... )
            if (dest->gtOper == GT_ADDR)
            {
                GenTreePtr addrOp = dest->gtOp.gtOp1;
                // Ignore reinterpret casts between int/gc
                if ((addrOp->TypeGet() == asgType) ||
                    (varTypeIsIntegralOrI(addrOp) && (genTypeSize(addrOp->TypeGet()) == size)))
                {
                    dest = addrOp;
                    asgType = addrOp->TypeGet();
                    needsIndirection = false;
                }
            }
        }

        if (needsIndirection)
        {
            // Indirect the dest node.

            dest = gtNewOperNode(GT_IND, asgType, dest);

            // If we have no information about the destination, we have to assume it could
            // live anywhere (not just in the GC heap).
            // Mark the GT_IND node so that we use the correct write barrier helper in case
            // the field is a GC ref.

            if (!fgIsIndirOfAddrOfLocal(dest))
            {
                dest->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF | GTF_IND_TGTANYWHERE);
            }
        }

        if (volatil)
        {
            dest->gtFlags |= GTF_DONT_CSE;
        }

        LclVarDsc* srcVarDsc = nullptr;
        if (isCopyBlock)
        {
            if (impIsAddressInLocal(src, &lclVarTree))
            {
                srcVarDsc = &(lvaTable[lclVarTree->AsLclVarCommon()->gtLclNum]);
                if (varTypeIsStruct(lclVarTree) && (srcVarDsc->lvPromoted || srcVarDsc->lvIsSIMDType()))
                {
                    // Let fgMorphCopyBlock handle it.
                    return nullptr;
                }
                else if (!varTypeIsFloating(lclVarTree->TypeGet()) &&
                            size == genTypeSize(genActualType(lclVarTree->TypeGet())))
                {
                    // Use the src local var directly.
                    src = lclVarTree;
                }
                else
                {
#ifndef LEGACY_BACKEND

                    // The source argument of the copyblk can potentially
                    // be accessed only through indir(addr(lclVar))
                    // or indir(lclVarAddr) in rational form and liveness
                    // won't account for these uses. That said,
                    // we have to mark this local as address exposed so
                    // we don't delete it as a dead store later on.
                    unsigned lclVarNum                = lclVarTree->gtLclVarCommon.gtLclNum;
                    lvaTable[lclVarNum].lvAddrExposed = true;
                    lvaSetVarDoNotEnregister(lclVarNum DEBUGARG(DNER_AddrExposed));

#else  // LEGACY_BACKEND
                    lvaSetVarDoNotEnregister(lclVarTree->gtLclVarCommon.gtLclNum DEBUGARG(DNER_LocalField));
#endif // LEGACY_BACKEND
                }
            }

            if (src != lclVarTree)
            {
                // Indirect the src node.

                src = gtNewOperNode(GT_IND, asgType, src);

                // If we have no information about the src, we have to assume it could
                // live anywhere (not just in the GC heap).
                // Mark the GT_IND node so that we use the correct write barrier helper in case
                // the field is a GC ref.

                if (!fgIsIndirOfAddrOfLocal(src))
                {
                    src->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF | GTF_IND_TGTANYWHERE);
                }
            }

            if (volatil)
            {
                src->gtFlags |= GTF_DONT_CSE;
            }
        }
        else // (oper == GT_INITBLK)
        {
            // This will mutate the integer constant, in place, to be the correct
            // value for the type we are using in the assignment.
            src->AsIntCon()->FixupInitBlkValue(asgType);
        }

        // Create the assignment node.

        result         = gtNewAssignNode(dest, src);
        result->gtType = asgType;

        return result;
    }

    return nullptr;
}

//------------------------------------------------------------------------
// fgMorphInitBlock: Perform the Morphing of a GT_INITBLK node
//
// Arguments:
//    tree - a tree node with a gtOper of GT_INITBLK
//           the child nodes for tree have already been Morphed
//
// Return Value:
//    We can return the orginal GT_INITBLK unmodified (least desirable, but always correct)
//    We can return a single assignment, when fgMorphOneAsgBlockOp transforms it (most desirable)
//    If we have performed struct promotion of the Dest() then we will try to
//    perform a field by field assignment for each of the promoted struct fields
//
// Notes:
//    If we leave it as a GT_INITBLK we will call lvaSetVarDoNotEnregister() with a reason of DNER_BlockOp
//    if the Dest() is a a struct that has a "CustomLayout" and "ConstainsHoles" then we
//    can not use a field by field assignment and must the orginal GT_INITBLK unmodified.

GenTreePtr Compiler::fgMorphInitBlock(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_INITBLK);

    JITDUMP("\nfgMorphInitBlock:");

    GenTreePtr oneAsgTree = fgMorphOneAsgBlockOp(tree);
    if (oneAsgTree)
    {
        JITDUMP(" using oneAsgTree.\n");
        tree = oneAsgTree;
    }
    else
    {
        GenTreeInitBlk* initBlkOp = tree->AsInitBlk();

        GenTreePtr destAddr  = initBlkOp->Dest();
        GenTreePtr initVal   = initBlkOp->InitVal();
        GenTreePtr blockSize = initBlkOp->Size();

        // The dest must be an address
        noway_assert(genActualType(destAddr->gtType) == TYP_I_IMPL || destAddr->gtType == TYP_BYREF);

        // The size must be an integer type
        assert(varTypeIsIntegral(blockSize->gtType));

        unsigned blockWidth        = 0;
        bool     blockWidthIsConst = false;

        if (blockSize->IsCnsIntOrI())
        {
            blockWidthIsConst = true;
            blockWidth        = unsigned(blockSize->gtIntConCommon.IconValue());
        }

        GenTreeLclVarCommon* lclVarTree = nullptr;

        FieldSeqNode* destFldSeq   = nullptr;
        unsigned      destLclNum   = BAD_VAR_NUM;
        LclVarDsc*    destLclVar   = nullptr;
        bool          destDoFldAsg = false;

        if (destAddr->IsLocalAddrExpr(this, &lclVarTree, &destFldSeq))
        {
            destLclNum = lclVarTree->gtLclNum;
            destLclVar = &lvaTable[destLclNum];

#if LOCAL_ASSERTION_PROP
            // Kill everything about destLclNum (and its field locals)
            if (optLocalAssertionProp)
            {
                if (optAssertionCount > 0)
                {
                    fgKillDependentAssertions(destLclNum DEBUGARG(tree));
                }
            }
#endif // LOCAL_ASSERTION_PROP

            if (destLclVar->lvPromoted && blockWidthIsConst)
            {
                noway_assert(varTypeIsStruct(destLclVar));
                noway_assert(!opts.MinOpts());
                if (destLclVar->lvAddrExposed & destLclVar->lvContainsHoles)
                {
                    JITDUMP(" dest is address exposed");
                }
                else
                {
                    if (blockWidth == destLclVar->lvExactSize)
                    {
                        JITDUMP(" (destDoFldAsg=true)");
                        // We may decide later that a copyblk is required when this struct has holes
                        destDoFldAsg = true;
                    }
                    else
                    {
                        JITDUMP(" with mismatched size");
                    }
                }
            }
        }

        // Can we use field by field assignment for the dest?
        if (destDoFldAsg && destLclVar->lvCustomLayout && destLclVar->lvContainsHoles)
        {
            JITDUMP(" dest contains holes");
            destDoFldAsg = false;
        }

        JITDUMP(destDoFldAsg ? " using field by field initialization.\n" : " this requires an InitBlock.\n");

        if (!destDoFldAsg && (destLclVar != nullptr))
        {
            // If destLclVar is not a reg-sized non-field-addressed struct, set it as DoNotEnregister.
            if (!destLclVar->lvRegStruct)
            {
                // Mark it as DoNotEnregister.
                lvaSetVarDoNotEnregister(destLclNum DEBUGARG(DNER_BlockOp));
            }
        }

        // Mark the dest struct as DoNotEnreg
        // when they are LclVar structs and we are using a CopyBlock
        // or the struct is not promoted
        //
        if (!destDoFldAsg)
        {
#if CPU_USES_BLOCK_MOVE
            compBlkOpUsed = true;
#endif
            goto _Done;
        }

        // The initVal must be a constant of TYP_INT
        noway_assert(initVal->OperGet() == GT_CNS_INT);
        noway_assert(genActualType(initVal->gtType) == TYP_INT);

        // The dest must be of a struct type.
        noway_assert(varTypeIsStruct(destLclVar));

        //
        // Now, convert InitBlock to individual assignments
        //

        tree = nullptr;

        GenTreePtr asg;
        GenTreePtr dest;
        GenTreePtr srcCopy;
        unsigned   fieldLclNum;
        unsigned   fieldCnt = destLclVar->lvFieldCnt;

        for (unsigned i = 0; i < fieldCnt; ++i)
        {
            fieldLclNum = destLclVar->lvFieldLclStart + i;
            dest        = gtNewLclvNode(fieldLclNum, lvaTable[fieldLclNum].TypeGet());

            noway_assert(destAddr->gtOp.gtOp1->gtOper == GT_LCL_VAR);
            // If it had been labeled a "USEASG", assignments to the the individual promoted fields are not.
            dest->gtFlags |= destAddr->gtOp.gtOp1->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);

            srcCopy = gtCloneExpr(initVal);
            noway_assert(srcCopy != nullptr);

            // need type of oper to be same as tree
            if (dest->gtType == TYP_LONG)
            {
                srcCopy->ChangeOperConst(GT_CNS_NATIVELONG);
                // copy and extend the value
                srcCopy->gtIntConCommon.SetLngValue(initVal->gtIntConCommon.IconValue());
                /* Change the types of srcCopy to TYP_LONG */
                srcCopy->gtType = TYP_LONG;
            }
            else if (varTypeIsFloating(dest->gtType))
            {
                srcCopy->ChangeOperConst(GT_CNS_DBL);
                // setup the bit pattern
                memset(&srcCopy->gtDblCon.gtDconVal, (int)initVal->gtIntCon.gtIconVal,
                       sizeof(srcCopy->gtDblCon.gtDconVal));
                /* Change the types of srcCopy to TYP_DOUBLE */
                srcCopy->gtType = TYP_DOUBLE;
            }
            else
            {
                noway_assert(srcCopy->gtOper == GT_CNS_INT);
                noway_assert(srcCopy->TypeGet() == TYP_INT);
                // setup the bit pattern
                memset(&srcCopy->gtIntCon.gtIconVal, (int)initVal->gtIntCon.gtIconVal,
                       sizeof(srcCopy->gtIntCon.gtIconVal));
            }

            srcCopy->gtType = dest->TypeGet();

            asg = gtNewAssignNode(dest, srcCopy);

#if LOCAL_ASSERTION_PROP
            if (optLocalAssertionProp)
            {
                optAssertionGen(asg);
            }
#endif // LOCAL_ASSERTION_PROP

            if (tree)
            {
                tree = gtNewOperNode(GT_COMMA, TYP_VOID, tree, asg);
            }
            else
            {
                tree = asg;
            }
        }
    }

#ifdef DEBUG
    tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;

    if (verbose)
    {
        printf("fgMorphInitBlock (after):\n");
        gtDispTree(tree);
    }
#endif

_Done:
    return tree;
}

//------------------------------------------------------------------------
// fgMorphCopyBlock: Perform the Morphing of a GT_COPYBLK and GT_COPYOBJ nodes
//
// Arguments:
//    tree - a tree node with a gtOper of GT_COPYBLK or GT_COPYOBJ
//           the child nodes for tree have already been Morphed
//
// Return Value:
//    We can return the orginal GT_COPYBLK or GT_COPYOBJ unmodified (least desirable, but always correct)
//    We can return a single assignment, when fgMorphOneAsgBlockOp transforms it (most desirable)
//    If we have performed struct promotion of the Source() or the Dest() then we will try to
//    perform a field by field assignment for each of the promoted struct fields
//
// Notes:
//    If we leave it as a GT_COPYBLK or GT_COPYOBJ we will call lvaSetVarDoNotEnregister() on both Source() and Dest()
//    When performing a field by field assignment we can have one of Source() or Dest treated as a blob of bytes
//    and in such cases we will call lvaSetVarDoNotEnregister() on the one treated as a blob of bytes.
//    if the Source() or Dest() is a a struct that has a "CustomLayout" and "ConstainsHoles" then we
//    can not use a field by field assignment and must the orginal GT_COPYBLK unmodified.

GenTreePtr Compiler::fgMorphCopyBlock(GenTreePtr tree)
{
    noway_assert(tree->OperIsCopyBlkOp());

    JITDUMP("\nfgMorphCopyBlock:");

    bool isLateArg = (tree->gtFlags & GTF_LATE_ARG) != 0;

    GenTreePtr oneAsgTree = fgMorphOneAsgBlockOp(tree);

    if (oneAsgTree)
    {
        JITDUMP(" using oneAsgTree.\n");
        tree = oneAsgTree;
    }
    else
    {
        GenTreePtr destAddr;
        GenTreePtr srcAddr;
        GenTreePtr blockSize;
        bool       isCopyObj;

        if (tree->OperGet() == GT_COPYBLK)
        {
            GenTreeCpBlk* copyBlkOp = tree->AsCpBlk();

            isCopyObj = false;
            destAddr  = copyBlkOp->Dest();
            srcAddr   = copyBlkOp->Source();
            blockSize = copyBlkOp->Size();
        }
        else
        {
            GenTreeCpObj* copyObjOp = tree->AsCpObj();

            isCopyObj = true;
            destAddr  = copyObjOp->Dest();
            srcAddr   = copyObjOp->Source();
            blockSize = copyObjOp->ClsTok();
        }

        noway_assert(destAddr->TypeGet() == TYP_BYREF || destAddr->TypeGet() == TYP_I_IMPL);
        noway_assert(srcAddr->TypeGet() == TYP_BYREF || srcAddr->TypeGet() == TYP_I_IMPL);

        unsigned blockWidth        = 0;
        bool     blockWidthIsConst = false;

        if (blockSize->IsCnsIntOrI())
        {
            blockWidthIsConst = true;
            if (blockSize->IsIconHandle(GTF_ICON_CLASS_HDL))
            {
                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE)blockSize->gtIntConCommon.IconValue();
                blockWidth                  = info.compCompHnd->getClassSize(clsHnd);
            }
            else
            {
                blockWidth = unsigned(blockSize->gtIntConCommon.IconValue());
            }
        }

        GenTreeLclVarCommon* lclVarTree = nullptr;

        FieldSeqNode* destFldSeq   = nullptr;
        unsigned      destLclNum   = BAD_VAR_NUM;
        LclVarDsc*    destLclVar   = nullptr;
        bool          destDoFldAsg = false;
        bool          destOnStack  = false;

        if (destAddr->IsLocalAddrExpr(this, &lclVarTree, &destFldSeq))
        {
            destOnStack = true;
            destLclNum  = lclVarTree->gtLclNum;
            destLclVar  = &lvaTable[destLclNum];

#if LOCAL_ASSERTION_PROP
            // Kill everything about destLclNum (and its field locals)
            if (optLocalAssertionProp)
            {
                if (optAssertionCount > 0)
                {
                    fgKillDependentAssertions(destLclNum DEBUGARG(tree));
                }
            }
#endif // LOCAL_ASSERTION_PROP

            if (destLclVar->lvPromoted && blockWidthIsConst)
            {
                noway_assert(varTypeIsStruct(destLclVar));
                noway_assert(!opts.MinOpts());

                if (blockWidth == destLclVar->lvExactSize)
                {
                    JITDUMP(" (destDoFldAsg=true)");
                    // We may decide later that a copyblk is required when this struct has holes
                    destDoFldAsg = true;
                }
                else
                {
                    JITDUMP(" with mismatched dest size");
                }
            }
        }

        FieldSeqNode* srcFldSeq   = nullptr;
        unsigned      srcLclNum   = BAD_VAR_NUM;
        LclVarDsc*    srcLclVar   = nullptr;
        bool          srcDoFldAsg = false;

        if (srcAddr->IsLocalAddrExpr(this, &lclVarTree, &srcFldSeq))
        {
            srcLclNum = lclVarTree->gtLclNum;
            srcLclVar = &lvaTable[srcLclNum];

            if (srcLclVar->lvPromoted && blockWidthIsConst)
            {
                noway_assert(varTypeIsStruct(srcLclVar));
                noway_assert(!opts.MinOpts());

                if (blockWidth == srcLclVar->lvExactSize)
                {
                    JITDUMP(" (srcDoFldAsg=true)");
                    // We may decide later that a copyblk is required when this struct has holes
                    srcDoFldAsg = true;
                }
                else
                {
                    JITDUMP(" with mismatched src size");
                }
            }
        }

        // Check to see if we are required to do a copy block because the struct contains holes
        // and either the src or dest is externally visible
        //
        bool requiresCopyBlock  = false;
        bool srcSingleLclVarAsg = false;

        // If either src or dest is a reg-sized non-field-addressed struct, keep the copyBlock.
        if ((destLclVar != nullptr && destLclVar->lvRegStruct) || (srcLclVar != nullptr && srcLclVar->lvRegStruct))
        {
            requiresCopyBlock = true;
        }

        // Can we use field by field assignment for the dest?
        if (destDoFldAsg && destLclVar->lvCustomLayout && destLclVar->lvContainsHoles)
        {
            JITDUMP(" dest contains custom layout and contains holes");
            // C++ style CopyBlock with holes
            requiresCopyBlock = true;
        }

        // Can we use field by field assignment for the src?
        if (srcDoFldAsg && srcLclVar->lvCustomLayout && srcLclVar->lvContainsHoles)
        {
            JITDUMP(" src contains custom layout and contains holes");
            // C++ style CopyBlock with holes
            requiresCopyBlock = true;
        }

        if (tree->OperGet() == GT_COPYBLK && tree->AsCpBlk()->gtBlkOpGcUnsafe)
        {
            requiresCopyBlock = true;
        }

        // If we passed the above checks, then we will check these two
        if (!requiresCopyBlock)
        {
            // Are both dest and src promoted structs?
            if (destDoFldAsg && srcDoFldAsg)
            {
                // Both structs should be of the same type, if not we will use a copy block
                if (lvaTable[destLclNum].lvVerTypeInfo.GetClassHandle() !=
                    lvaTable[srcLclNum].lvVerTypeInfo.GetClassHandle())
                {
                    requiresCopyBlock = true; // Mismatched types, leave as a CopyBlock
                    JITDUMP(" with mismatched types");
                }
            }
            // Are neither dest or src promoted structs?
            else if (!destDoFldAsg && !srcDoFldAsg)
            {
                requiresCopyBlock = true; // Leave as a CopyBlock
                JITDUMP(" with no promoted structs");
            }
            else if (destDoFldAsg)
            {
                // Match the following kinds of trees:
                //  fgMorphTree BB01, stmt 9 (before)
                //   [000052] ------------        const     int    8
                //   [000053] -A--G-------     copyBlk   void
                //   [000051] ------------           addr      byref
                //   [000050] ------------              lclVar    long   V07 loc5
                //   [000054] --------R---        <list>    void
                //   [000049] ------------           addr      byref
                //   [000048] ------------              lclVar    struct(P) V06 loc4
                //                                              long   V06.h (offs=0x00) -> V17 tmp9
                // Yields this transformation
                //  fgMorphCopyBlock (after):
                //   [000050] ------------        lclVar    long   V07 loc5
                //   [000085] -A----------     =         long
                //   [000083] D------N----        lclVar    long   V17 tmp9
                //
                if (blockWidthIsConst && (destLclVar->lvFieldCnt == 1) && (srcLclVar != nullptr) &&
                    (blockWidth == genTypeSize(srcLclVar->TypeGet())))
                {
                    // Reject the following tree:
                    //  - seen on x86chk    jit\jit64\hfa\main\hfa_sf3E_r.exe
                    //
                    //  fgMorphTree BB01, stmt 6 (before)
                    //   [000038] -------------        const     int    4
                    //   [000039] -A--G--------     copyBlk   void
                    //   [000037] -------------           addr      byref
                    //   [000036] -------------              lclVar    int    V05 loc3
                    //   [000040] --------R----        <list>    void
                    //   [000035] -------------           addr      byref
                    //   [000034] -------------              lclVar    struct(P) V04 loc2
                    //                                          float  V04.f1 (offs=0x00) -> V13 tmp6
                    // As this would framsform into
                    //   float V13 = int V05
                    //
                    unsigned  fieldLclNum = lvaTable[destLclNum].lvFieldLclStart;
                    var_types destType    = lvaTable[fieldLclNum].TypeGet();
                    if (srcLclVar->TypeGet() == destType)
                    {
                        srcSingleLclVarAsg = true;
                    }
                }
            }
        }

        // If we require a copy block the set both of the field assign bools to false
        if (requiresCopyBlock)
        {
            // If a copy block is required then we won't do field by field assignments
            destDoFldAsg = false;
            srcDoFldAsg  = false;
        }

        JITDUMP(requiresCopyBlock ? " this requires a CopyBlock.\n" : " using field by field assignments.\n");

        // Mark the dest/src structs as DoNotEnreg
        // when they are not reg-sized non-field-addressed structs and we are using a CopyBlock
        // or the struct is not promoted
        //
        if (!destDoFldAsg && (destLclVar != nullptr))
        {
            if (!destLclVar->lvRegStruct)
            {
                // Mark it as DoNotEnregister.
                lvaSetVarDoNotEnregister(destLclNum DEBUGARG(DNER_BlockOp));
            }
        }

        if (!srcDoFldAsg && (srcLclVar != nullptr) && !srcSingleLclVarAsg)
        {
            if (!srcLclVar->lvRegStruct)
            {
                lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DNER_BlockOp));
            }
        }

        if (requiresCopyBlock)
        {
#if CPU_USES_BLOCK_MOVE
            compBlkOpUsed = true;
#endif
#ifdef CPBLK_UNROLL_LIMIT
            // Note that the unrolling of CopyBlk is only implemented on some platforms
            // Currently that includes x64 and Arm64 but not x64 or Arm32

            // If we have a CopyObj with a dest on the stack
            // we will convert it into an GC Unsafe CopyBlk that is non-interruptible
            // when its size is small enouch to be completely unrolled (i.e. between [16..64] bytes)
            //
            if (isCopyObj && destOnStack && blockWidthIsConst && (blockWidth >= (2 * TARGET_POINTER_SIZE)) &&
                (blockWidth <= CPBLK_UNROLL_LIMIT))
            {
                tree->SetOper(GT_COPYBLK);
                tree->AsCpBlk()->gtBlkOpGcUnsafe = true; // Mark as a GC unsage copy block
                blockSize->gtIntConCommon.SetIconValue(ssize_t(blockWidth));
                blockSize->gtFlags &= ~GTF_ICON_HDL_MASK; // Clear the GTF_ICON_CLASS_HDL flags
            }
#endif
            // Liveness doesn't consider copyblk arguments of simple types as being
            // a use or def, so explicitly mark these variables as address-exposed.
            if (srcLclNum != BAD_VAR_NUM && !varTypeIsStruct(srcLclVar))
            {
                JITDUMP("Non-struct copyBlk src V%02d is addr exposed\n", srcLclNum);
                lvaTable[srcLclNum].lvAddrExposed = true;
            }

            if (destLclNum != BAD_VAR_NUM && !varTypeIsStruct(destLclVar))
            {
                JITDUMP("Non-struct copyBlk dest V%02d is addr exposed\n", destLclNum);
                lvaTable[destLclNum].lvAddrExposed = true;
            }

            goto _Done;
        }

        //
        // Otherwise we convert this CopyBlock into individual field by field assignments
        //
        tree = nullptr;

        GenTreePtr asg;
        GenTreePtr dest;
        GenTreePtr src;
        GenTreePtr addrSpill            = nullptr;
        unsigned   addrSpillTemp        = BAD_VAR_NUM;
        bool       addrSpillIsStackDest = false; // true if 'addrSpill' represents the address in our local stack frame

        unsigned fieldCnt = DUMMY_INIT(0);

        if (destDoFldAsg && srcDoFldAsg)
        {
            // To do fieldwise assignments for both sides, they'd better be the same struct type!
            // All of these conditions were checked above...
            assert(destLclNum != BAD_VAR_NUM && srcLclNum != BAD_VAR_NUM);
            assert(lvaTable[destLclNum].lvVerTypeInfo.GetClassHandle() ==
                   lvaTable[srcLclNum].lvVerTypeInfo.GetClassHandle());
            assert(destLclVar != nullptr && srcLclVar != nullptr && destLclVar->lvFieldCnt == srcLclVar->lvFieldCnt);

            fieldCnt = destLclVar->lvFieldCnt;
            goto _AssignFields; // No need to spill the address to the temp. Go ahead to morph it into field
                                // assignments.
        }
        else if (destDoFldAsg)
        {
            fieldCnt = destLclVar->lvFieldCnt;
        }
        else
        {
            assert(srcDoFldAsg);
            fieldCnt = srcLclVar->lvFieldCnt;
        }

        if (destDoFldAsg)
        {
            noway_assert(!srcDoFldAsg);
            if (gtClone(srcAddr))
            {
                // srcAddr is simple expression. No need to spill.
                noway_assert((srcAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
            }
            else
            {
                // srcAddr is complex expression. Clone and spill it (unless the destination is
                // a struct local that only has one field, in which case we'd only use the
                // address value once...)
                if (destLclVar->lvFieldCnt > 1)
                {
                    addrSpill = gtCloneExpr(srcAddr); // addrSpill represents the 'srcAddr'
                    noway_assert(addrSpill != nullptr);
                }
            }
        }

        if (srcDoFldAsg)
        {
            noway_assert(!destDoFldAsg);

            // If we're doing field-wise stores, to an address within a local, and we copy
            // the address into "addrSpill", do *not* declare the original local var node in the
            // field address as GTF_VAR_DEF and GTF_VAR_USEASG; we will declare each of the
            // field-wise assignments as an "indirect" assignment to the local.
            // ("lclVarTree" is a subtree of "destAddr"; make sure we remove the flags before
            // we clone it.)
            if (lclVarTree != nullptr)
            {
                lclVarTree->gtFlags &= ~(GTF_VAR_DEF | GTF_VAR_USEASG);
            }

            if (gtClone(destAddr))
            {
                // destAddr is simple expression. No need to spill
                noway_assert((destAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
            }
            else
            {
                // destAddr is complex expression. Clone and spill it (unless
                // the source is a struct local that only has one field, in which case we'd only
                // use the address value once...)
                if (srcLclVar->lvFieldCnt > 1)
                {
                    addrSpill = gtCloneExpr(destAddr); // addrSpill represents the 'destAddr'
                    noway_assert(addrSpill != nullptr);
                }

                // TODO-CQ: this should be based on a more general
                // "BaseAddress" method, that handles fields of structs, before or after
                // morphing.
                if (addrSpill != nullptr && addrSpill->OperGet() == GT_ADDR)
                {
                    if (addrSpill->gtOp.gtOp1->IsLocal())
                    {
                        // We will *not* consider this to define the local, but rather have each individual field assign
                        // be a definition.
                        addrSpill->gtOp.gtOp1->gtFlags &= ~(GTF_LIVENESS_MASK);
                        assert(lvaTable[addrSpill->gtOp.gtOp1->gtLclVarCommon.gtLclNum].lvLclBlockOpAddr == 1);
                        addrSpillIsStackDest = true; // addrSpill represents the address of LclVar[varNum] in our
                                                     // local stack frame
                    }
                }
            }
        }

        if (addrSpill != nullptr)
        {
            // Spill the (complex) address to a BYREF temp.
            // Note, at most one address may need to be spilled.
            addrSpillTemp = lvaGrabTemp(true DEBUGARG("BlockOp address local"));

            lvaTable[addrSpillTemp].lvType = TYP_BYREF;

            if (addrSpillIsStackDest)
            {
                lvaTable[addrSpillTemp].lvStackByref = true;
            }

            tree = gtNewAssignNode(gtNewLclvNode(addrSpillTemp, TYP_BYREF), addrSpill);

#ifndef LEGACY_BACKEND
            // If we are assigning the address of a LclVar here
            // liveness does not account for this kind of address taken use.
            //
            // We have to mark this local as address exposed so
            // that we don't delete the definition for this LclVar
            // as a dead store later on.
            //
            if (addrSpill->OperGet() == GT_ADDR)
            {
                GenTreePtr addrOp = addrSpill->gtOp.gtOp1;
                if (addrOp->IsLocal())
                {
                    unsigned lclVarNum                = addrOp->gtLclVarCommon.gtLclNum;
                    lvaTable[lclVarNum].lvAddrExposed = true;
                    lvaSetVarDoNotEnregister(lclVarNum DEBUGARG(DNER_AddrExposed));
                }
            }
#endif // !LEGACY_BACKEND
        }

    _AssignFields:

        for (unsigned i = 0; i < fieldCnt; ++i)
        {
            FieldSeqNode* curFieldSeq = nullptr;
            if (destDoFldAsg)
            {
                noway_assert(destLclNum != BAD_VAR_NUM);
                unsigned fieldLclNum = lvaTable[destLclNum].lvFieldLclStart + i;
                dest                 = gtNewLclvNode(fieldLclNum, lvaTable[fieldLclNum].TypeGet());

                noway_assert(destAddr->gtOp.gtOp1->gtOper == GT_LCL_VAR);
                // If it had been labeled a "USEASG", assignments to the the individual promoted fields are not.
                dest->gtFlags |= destAddr->gtOp.gtOp1->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);
            }
            else
            {
                noway_assert(srcDoFldAsg);
                noway_assert(srcLclNum != BAD_VAR_NUM);
                unsigned fieldLclNum = lvaTable[srcLclNum].lvFieldLclStart + i;

                if (addrSpill)
                {
                    assert(addrSpillTemp != BAD_VAR_NUM);
                    dest = gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                }
                else
                {
                    dest = gtCloneExpr(destAddr);
                    noway_assert(dest != nullptr);

                    // Is the address of a local?
                    GenTreeLclVarCommon* lclVarTree = nullptr;
                    bool                 isEntire   = false;
                    bool*                pIsEntire  = (blockWidthIsConst ? &isEntire : nullptr);
                    if (dest->DefinesLocalAddr(this, blockWidth, &lclVarTree, pIsEntire))
                    {
                        lclVarTree->gtFlags |= GTF_VAR_DEF;
                        if (!isEntire)
                        {
                            lclVarTree->gtFlags |= GTF_VAR_USEASG;
                        }
                    }
                }

                GenTreePtr fieldOffsetNode = gtNewIconNode(lvaTable[fieldLclNum].lvFldOffset, TYP_I_IMPL);
                // Have to set the field sequence -- which means we need the field handle.
                CORINFO_CLASS_HANDLE classHnd = lvaTable[srcLclNum].lvVerTypeInfo.GetClassHandle();
                CORINFO_FIELD_HANDLE fieldHnd =
                    info.compCompHnd->getFieldInClass(classHnd, lvaTable[fieldLclNum].lvFldOrdinal);
                curFieldSeq                          = GetFieldSeqStore()->CreateSingleton(fieldHnd);
                fieldOffsetNode->gtIntCon.gtFieldSeq = curFieldSeq;

                dest = gtNewOperNode(GT_ADD, TYP_BYREF, dest, fieldOffsetNode);

                dest = gtNewOperNode(GT_IND, lvaTable[fieldLclNum].TypeGet(), dest);

                // !!! The destination could be on stack. !!!
                // This flag will let us choose the correct write barrier.
                dest->gtFlags |= GTF_IND_TGTANYWHERE;
            }

            if (srcDoFldAsg)
            {
                noway_assert(srcLclNum != BAD_VAR_NUM);
                unsigned fieldLclNum = lvaTable[srcLclNum].lvFieldLclStart + i;
                src                  = gtNewLclvNode(fieldLclNum, lvaTable[fieldLclNum].TypeGet());

                noway_assert(srcAddr->gtOp.gtOp1->gtOper == GT_LCL_VAR);
                src->gtFlags |= srcAddr->gtOp.gtOp1->gtFlags & ~GTF_NODE_MASK;
            }
            else
            {
                noway_assert(destDoFldAsg);
                noway_assert(destLclNum != BAD_VAR_NUM);
                unsigned fieldLclNum = lvaTable[destLclNum].lvFieldLclStart + i;

                if (srcSingleLclVarAsg)
                {
                    noway_assert(fieldCnt == 1);
                    noway_assert(srcLclVar != nullptr);
                    noway_assert(addrSpill == nullptr);

                    src = gtNewLclvNode(srcLclNum, srcLclVar->TypeGet());
                }
                else
                {
                    if (addrSpill)
                    {
                        assert(addrSpillTemp != BAD_VAR_NUM);
                        src = gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                    }
                    else
                    {
                        src = gtCloneExpr(srcAddr);
                        noway_assert(src != nullptr);
                    }

                    CORINFO_CLASS_HANDLE classHnd = lvaTable[destLclNum].lvVerTypeInfo.GetClassHandle();
                    CORINFO_FIELD_HANDLE fieldHnd =
                        info.compCompHnd->getFieldInClass(classHnd, lvaTable[fieldLclNum].lvFldOrdinal);
                    curFieldSeq = GetFieldSeqStore()->CreateSingleton(fieldHnd);

                    src = gtNewOperNode(GT_ADD, TYP_BYREF, src,
                                        new (this, GT_CNS_INT)
                                            GenTreeIntCon(TYP_I_IMPL, lvaTable[fieldLclNum].lvFldOffset, curFieldSeq));

                    src = gtNewOperNode(GT_IND, lvaTable[fieldLclNum].TypeGet(), src);
                }
            }

            noway_assert(dest->TypeGet() == src->TypeGet());

            asg = gtNewAssignNode(dest, src);

            // If we spilled the address, and we didn't do individual field assignments to promoted fields,
            // and it was of a local, record the assignment as an indirect update of a local.
            if (addrSpill && !destDoFldAsg && destLclNum != BAD_VAR_NUM)
            {
                curFieldSeq   = GetFieldSeqStore()->Append(destFldSeq, curFieldSeq);
                bool isEntire = (genTypeSize(var_types(lvaTable[destLclNum].lvType)) == genTypeSize(dest->TypeGet()));
                IndirectAssignmentAnnotation* pIndirAnnot =
                    new (this, CMK_Unknown) IndirectAssignmentAnnotation(destLclNum, curFieldSeq, isEntire);
                GetIndirAssignMap()->Set(asg, pIndirAnnot);
            }

#if LOCAL_ASSERTION_PROP
            if (optLocalAssertionProp)
            {
                optAssertionGen(asg);
            }
#endif // LOCAL_ASSERTION_PROP

            if (tree)
            {
                tree = gtNewOperNode(GT_COMMA, TYP_VOID, tree, asg);
            }
            else
            {
                tree = asg;
            }
        }
    }

    if (isLateArg)
    {
        tree->gtFlags |= GTF_LATE_ARG;
    }

#ifdef DEBUG
    tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;

    if (verbose)
    {
        printf("\nfgMorphCopyBlock (after):\n");
        gtDispTree(tree);
    }
#endif

_Done:
    return tree;
}

// insert conversions and normalize to make tree amenable to register
// FP architectures
GenTree* Compiler::fgMorphForRegisterFP(GenTree* tree)
{
    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtGetOp2();

    if (tree->OperIsArithmetic() && varTypeIsFloating(tree))
    {
        if (op1->TypeGet() != tree->TypeGet())
        {
            tree->gtOp.gtOp1 = gtNewCastNode(tree->TypeGet(), tree->gtOp.gtOp1, tree->TypeGet());
        }
        if (op2->TypeGet() != tree->TypeGet())
        {
            tree->gtOp.gtOp2 = gtNewCastNode(tree->TypeGet(), tree->gtOp.gtOp2, tree->TypeGet());
        }
    }
    else if (tree->OperIsCompare() && varTypeIsFloating(op1) && op1->TypeGet() != op2->TypeGet())
    {
        // both had better be floating, just one bigger than other
        assert(varTypeIsFloating(op2));
        if (op1->TypeGet() == TYP_FLOAT)
        {
            tree->gtOp.gtOp1 = gtNewCastNode(TYP_DOUBLE, tree->gtOp.gtOp1, TYP_DOUBLE);
        }
        else if (op2->TypeGet() == TYP_FLOAT)
        {
            tree->gtOp.gtOp2 = gtNewCastNode(TYP_DOUBLE, tree->gtOp.gtOp2, TYP_DOUBLE);
        }
    }

    return tree;
}

GenTree* Compiler::fgMorphRecognizeBoxNullable(GenTree* compare)
{
    GenTree*     op1 = compare->gtOp.gtOp1;
    GenTree*     op2 = compare->gtOp.gtOp2;
    GenTree*     opCns;
    GenTreeCall* opCall;

    // recognize this pattern:
    //
    // stmtExpr  void  (IL 0x000...  ???)
    //     return    int
    //             const     ref    null
    //         ==        int
    //             call help ref    HELPER.CORINFO_HELP_BOX_NULLABLE
    //                 const(h)  long   0x7fed96836c8 class
    //                 addr      byref
    //                     ld.lclVar struct V00 arg0
    //
    //
    // which comes from this code (reported by customer as being slow) :
    //
    // private static bool IsNull<T>(T arg)
    // {
    //    return arg==null;
    // }
    //

    if (op1->IsCnsIntOrI() && op2->IsHelperCall())
    {
        opCns  = op1;
        opCall = op2->AsCall();
    }
    else if (op1->IsHelperCall() && op2->IsCnsIntOrI())
    {
        opCns  = op2;
        opCall = op1->AsCall();
    }
    else
    {
        return compare;
    }

    if (!opCns->IsIntegralConst(0))
    {
        return compare;
    }

    if (eeGetHelperNum(opCall->gtCallMethHnd) != CORINFO_HELP_BOX_NULLABLE)
    {
        return compare;
    }

    // replace the box with an access of the nullable 'hasValue' field which is at the zero offset
    GenTree* newOp = gtNewOperNode(GT_IND, TYP_BOOL, opCall->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp1);

    if (opCall == op1)
    {
        compare->gtOp.gtOp1 = newOp;
    }
    else
    {
        compare->gtOp.gtOp2 = newOp;
    }

    return compare;
}

#ifdef FEATURE_SIMD

//--------------------------------------------------------------------------------------
// fgCopySIMDNode: make a copy of a SIMD intrinsic node, e.g. so that a field can be accessed.
//
// Arguments:
//    simdNode  - The GenTreeSIMD node to be copied
//
// Return Value:
//    A comma node where op1 is the assignment of the simd node to a temp, and op2 is the temp lclVar.
//
GenTree* Compiler::fgCopySIMDNode(GenTreeSIMD* simdNode)
{
    // Copy the result of the SIMD intrinsic into a temp.
    unsigned lclNum = lvaGrabTemp(true DEBUGARG("Copy of SIMD intrinsic with field access"));

    CORINFO_CLASS_HANDLE simdHandle = NO_CLASS_HANDLE;
    // We only have fields of the fixed float vectors.
    noway_assert(simdNode->gtSIMDBaseType == TYP_FLOAT);
    switch (simdNode->gtSIMDSize)
    {
        case 8:
            simdHandle = SIMDVector2Handle;
            break;
        case 12:
            simdHandle = SIMDVector3Handle;
            break;
        case 16:
            simdHandle = SIMDVector4Handle;
            break;
        default:
            noway_assert(!"field of unexpected SIMD type");
            break;
    }
    assert(simdHandle != NO_CLASS_HANDLE);

    lvaSetStruct(lclNum, simdHandle, false, true);
    lvaTable[lclNum].lvFieldAccessed = true;

    GenTree* asg           = gtNewTempAssign(lclNum, simdNode);
    GenTree* newLclVarNode = new (this, GT_LCL_VAR) GenTreeLclVar(simdNode->TypeGet(), lclNum, BAD_IL_OFFSET);

    GenTree* comma = gtNewOperNode(GT_COMMA, simdNode->TypeGet(), asg, newLclVarNode);
    return comma;
}

//--------------------------------------------------------------------------------------------------------------
// getSIMDStructFromField:
//   Checking whether the field belongs to a simd struct or not. If it is, return the GenTreePtr for
//   the struct node, also base type, field index and simd size. If it is not, just return  nullptr.
//   Usually if the tree node is from a simd lclvar which is not used in any SIMD intrinsic, then we
//   should return nullptr, since in this case we should treat SIMD struct as a regular struct.
//   However if no matter what, you just want get simd struct node, you can set the ignoreUsedInSIMDIntrinsic
//   as true. Then there will be no IsUsedInSIMDIntrinsic checking, and it will return SIMD struct node
//   if the struct is a SIMD struct.
//
// Arguments:
//       tree - GentreePtr. This node will be checked to see this is a field which belongs to a simd
//               struct used for simd intrinsic or not.
//       pBaseTypeOut - var_types pointer, if the tree node is the tree we want, we set *pBaseTypeOut
//                      to simd lclvar's base type.
//       indexOut - unsigned pointer, if the tree is used for simd intrinsic, we will set *indexOut
//                  equals to the index number of this field.
//       simdSizeOut - unsigned pointer, if the tree is used for simd intrinsic, set the *simdSizeOut
//                     equals to the simd struct size which this tree belongs to.
//      ignoreUsedInSIMDIntrinsic - bool. If this is set to true, then this function will ignore
//                                  the UsedInSIMDIntrinsic check.
//
// return value:
//       A GenTreePtr which points the simd lclvar tree belongs to. If the tree is not the simd
//       instrinic related field, return nullptr.
//

GenTreePtr Compiler::getSIMDStructFromField(GenTreePtr tree,
                                            var_types* pBaseTypeOut,
                                            unsigned*  indexOut,
                                            unsigned*  simdSizeOut,
                                            bool       ignoreUsedInSIMDIntrinsic /*false*/)
{
    GenTreePtr ret = nullptr;
    if (tree->OperGet() == GT_FIELD)
    {
        GenTreePtr objRef = tree->gtField.gtFldObj;
        if (objRef != nullptr)
        {
            GenTreePtr obj = nullptr;
            if (objRef->gtOper == GT_ADDR)
            {
                obj = objRef->gtOp.gtOp1;
            }
            else if (ignoreUsedInSIMDIntrinsic)
            {
                obj = objRef;
            }
            else
            {
                return nullptr;
            }

            if (isSIMDTypeLocal(obj))
            {
                unsigned   lclNum = obj->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc = &lvaTable[lclNum];
                if (varDsc->lvIsUsedInSIMDIntrinsic() || ignoreUsedInSIMDIntrinsic)
                {
                    *simdSizeOut  = varDsc->lvExactSize;
                    *pBaseTypeOut = getBaseTypeOfSIMDLocal(obj);
                    ret           = obj;
                }
            }
            else if (obj->OperGet() == GT_SIMD)
            {
                ret                   = obj;
                GenTreeSIMD* simdNode = obj->AsSIMD();
                *simdSizeOut          = simdNode->gtSIMDSize;
                *pBaseTypeOut         = simdNode->gtSIMDBaseType;
            }
        }
    }
    if (ret != nullptr)
    {
        unsigned BaseTypeSize = genTypeSize(*pBaseTypeOut);
        *indexOut             = tree->gtField.gtFldOffset / BaseTypeSize;
    }
    return ret;
}

/*****************************************************************************
*  If a read operation tries to access simd struct field, then transform the this
*  operation to to the SIMD intrinsic SIMDIntrinsicGetItem, and return the new tree.
*  Otherwise, return the old tree.
*  Argument:
*   tree - GenTreePtr. If this pointer points to simd struct which is used for simd
*          intrinsic. We will morph it as simd intrinsic SIMDIntrinsicGetItem.
*  Return:
*   A GenTreePtr which points to the new tree. If the tree is not for simd intrinsic,
*   return nullptr.
*/

GenTreePtr Compiler::fgMorphFieldToSIMDIntrinsicGet(GenTreePtr tree)
{
    unsigned   index          = 0;
    var_types  baseType       = TYP_UNKNOWN;
    unsigned   simdSize       = 0;
    GenTreePtr simdStructNode = getSIMDStructFromField(tree, &baseType, &index, &simdSize);
    if (simdStructNode != nullptr)
    {

        assert(simdSize >= ((index + 1) * genTypeSize(baseType)));
        GenTree* op2 = gtNewIconNode(index);
        tree         = gtNewSIMDNode(baseType, simdStructNode, op2, SIMDIntrinsicGetItem, baseType, simdSize);
#ifdef DEBUG
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
    }
    return tree;
}

/*****************************************************************************
*  Transform an assignment of a SIMD struct field to SIMD intrinsic
*  SIMDIntrinsicGetItem, and return a new tree. If If it is not such an assignment,
*  then return the old tree.
*  Argument:
*   tree - GenTreePtr. If this pointer points to simd struct which is used for simd
*          intrinsic. We will morph it as simd intrinsic set.
*  Return:
*   A GenTreePtr which points to the new tree. If the tree is not for simd intrinsic,
*   return nullptr.
*/

GenTreePtr Compiler::fgMorphFieldAssignToSIMDIntrinsicSet(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_ASG);
    GenTreePtr op1 = tree->gtGetOp1();
    GenTreePtr op2 = tree->gtGetOp2();

    unsigned   index         = 0;
    var_types  baseType      = TYP_UNKNOWN;
    unsigned   simdSize      = 0;
    GenTreePtr simdOp1Struct = getSIMDStructFromField(op1, &baseType, &index, &simdSize);
    if (simdOp1Struct != nullptr)
    {
        // Generate the simd set intrinsic
        assert(simdSize >= ((index + 1) * genTypeSize(baseType)));

        SIMDIntrinsicID simdIntrinsicID = SIMDIntrinsicInvalid;
        switch (index)
        {
            case 0:
                simdIntrinsicID = SIMDIntrinsicSetX;
                break;
            case 1:
                simdIntrinsicID = SIMDIntrinsicSetY;
                break;
            case 2:
                simdIntrinsicID = SIMDIntrinsicSetZ;
                break;
            case 3:
                simdIntrinsicID = SIMDIntrinsicSetW;
                break;
            default:
                noway_assert(!"There is no set intrinsic for index bigger than 3");
        }

        GenTreePtr newStruct = gtClone(simdOp1Struct);
        assert((newStruct != nullptr) && (varTypeIsSIMD(newStruct)));
        GenTreePtr simdTree = gtNewSIMDNode(newStruct->gtType, simdOp1Struct, op2, simdIntrinsicID, baseType, simdSize);
        GenTreePtr copyBlkDst = gtNewOperNode(GT_ADDR, TYP_BYREF, newStruct);
        tree                  = gtNewBlkOpNode(GT_COPYBLK, copyBlkDst, gtNewOperNode(GT_ADDR, TYP_BYREF, simdTree),
                              gtNewIconNode(simdSize), false);
#ifdef DEBUG
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
    }

    return tree;
}

#endif
/*****************************************************************************
 *
 *  Transform the given GTK_SMPOP tree for code generation.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTreePtr Compiler::fgMorphSmpOp(GenTreePtr tree, MorphAddrContext* mac)
{
    // this extra scope is a workaround for a gcc bug
    // the inline destructor for ALLOCA_CHECK confuses the control
    // flow and gcc thinks that the function never returns
    {
        ALLOCA_CHECK();
        noway_assert(tree->OperKind() & GTK_SMPOP);

        /* The steps in this function are :
           o Perform required preorder processing
           o Process the first, then second operand, if any
           o Perform required postorder morphing
           o Perform optional postorder morphing if optimizing
         */

        bool isQmarkColon = false;

#if LOCAL_ASSERTION_PROP
        AssertionIndex origAssertionCount = DUMMY_INIT(0);
        AssertionDsc*  origAssertionTab   = DUMMY_INIT(NULL);

        AssertionIndex thenAssertionCount = DUMMY_INIT(0);
        AssertionDsc*  thenAssertionTab   = DUMMY_INIT(NULL);
#endif

        if (fgGlobalMorph)
        {
#if !FEATURE_STACK_FP_X87
            tree = fgMorphForRegisterFP(tree);
#endif
        }

        genTreeOps oper = tree->OperGet();
        var_types  typ  = tree->TypeGet();
        GenTreePtr op1  = tree->gtOp.gtOp1;
        GenTreePtr op2  = tree->gtGetOp2();

        /*-------------------------------------------------------------------------
         * First do any PRE-ORDER processing
         */

        switch (oper)
        {
            // Some arithmetic operators need to use a helper call to the EE
            int helper;

            case GT_ASG:
                tree = fgDoNormalizeOnStore(tree);
                /* fgDoNormalizeOnStore can change op2 */
                noway_assert(op1 == tree->gtOp.gtOp1);
                op2 = tree->gtOp.gtOp2;

#ifdef FEATURE_SIMD
                {
                    // We should check whether op2 should be assigned to a SIMD field or not.
                    // if it is, we should tranlate the tree to simd intrinsic
                    GenTreePtr newTree = fgMorphFieldAssignToSIMDIntrinsicSet(tree);
                    if (newTree != tree)
                    {
                        tree = newTree;
                        oper = tree->OperGet();
                        typ  = tree->TypeGet();
                        op1  = tree->gtOp.gtOp1;
                        op2  = tree->gtGetOp2();
                    }
                }
#endif

                __fallthrough;

            case GT_ASG_ADD:
            case GT_ASG_SUB:
            case GT_ASG_MUL:
            case GT_ASG_DIV:
            case GT_ASG_MOD:
            case GT_ASG_UDIV:
            case GT_ASG_UMOD:
            case GT_ASG_OR:
            case GT_ASG_XOR:
            case GT_ASG_AND:
            case GT_ASG_LSH:
            case GT_ASG_RSH:
            case GT_ASG_RSZ:
            case GT_CHS:

                /* We can't CSE the LHS of an assignment. Only r-values can be CSEed */
                op1->gtFlags |= GTF_DONT_CSE;
                break;

            case GT_ADDR:

                /* op1 of a GT_ADDR is an l-value. Only r-values can be CSEed */
                op1->gtFlags |= GTF_DONT_CSE;
                break;

            case GT_QMARK:
            case GT_JTRUE:

                noway_assert(op1);

                if (op1->OperKind() & GTK_RELOP)
                {
                    noway_assert((oper == GT_JTRUE) || (op1->gtFlags & GTF_RELOP_QMARK));
                    /* Mark the comparison node with GTF_RELOP_JMP_USED so it knows that it does
                       not need to materialize the result as a 0 or 1. */

                    /* We also mark it as DONT_CSE, as we don't handle QMARKs with nonRELOP op1s */
                    op1->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

                    // Request that the codegen for op1 sets the condition flags
                    // when it generates the code for op1.
                    //
                    // Codegen for op1 must set the condition flags if
                    // this method returns true.
                    //
                    op1->gtRequestSetFlags();
                }
                else
                {
                    GenTreePtr effOp1 = op1->gtEffectiveVal();
                    noway_assert((effOp1->gtOper == GT_CNS_INT) &&
                                 (effOp1->IsIntegralConst(0) || effOp1->IsIntegralConst(1)));
                }
                break;

            case GT_COLON:
#if LOCAL_ASSERTION_PROP
                if (optLocalAssertionProp)
                {
#endif
                    isQmarkColon = true;
                }
                break;

            case GT_INDEX:
                return fgMorphArrayIndex(tree);

            case GT_CAST:
                return fgMorphCast(tree);

            case GT_MUL:

#ifndef _TARGET_64BIT_
                if (typ == TYP_LONG)
                {
                    /* For (long)int1 * (long)int2, we dont actually do the
                       casts, and just multiply the 32 bit values, which will
                       give us the 64 bit result in edx:eax */

                    noway_assert(op2);
                    if ((op1->gtOper == GT_CAST && op2->gtOper == GT_CAST &&
                         genActualType(op1->CastFromType()) == TYP_INT &&
                         genActualType(op2->CastFromType()) == TYP_INT) &&
                        !op1->gtOverflow() && !op2->gtOverflow())
                    {
                        // The casts have to be of the same signedness.
                        if ((op1->gtFlags & GTF_UNSIGNED) != (op2->gtFlags & GTF_UNSIGNED))
                        {
                            // We see if we can force an int constant to change its signedness
                            GenTreePtr constOp;
                            if (op1->gtCast.CastOp()->gtOper == GT_CNS_INT)
                                constOp = op1;
                            else if (op2->gtCast.CastOp()->gtOper == GT_CNS_INT)
                                constOp = op2;
                            else
                                goto NO_MUL_64RSLT;

                            if (((unsigned)(constOp->gtCast.CastOp()->gtIntCon.gtIconVal) < (unsigned)(0x80000000)))
                                constOp->gtFlags ^= GTF_UNSIGNED;
                            else
                                goto NO_MUL_64RSLT;
                        }

                        // The only combination that can overflow
                        if (tree->gtOverflow() && (tree->gtFlags & GTF_UNSIGNED) && !(op1->gtFlags & GTF_UNSIGNED))
                            goto NO_MUL_64RSLT;

                        /* Remaining combinations can never overflow during long mul. */

                        tree->gtFlags &= ~GTF_OVERFLOW;

                        /* Do unsigned mul only if the casts were unsigned */

                        tree->gtFlags &= ~GTF_UNSIGNED;
                        tree->gtFlags |= op1->gtFlags & GTF_UNSIGNED;

                        /* Since we are committing to GTF_MUL_64RSLT, we don't want
                           the casts to be folded away. So morph the castees directly */

                        op1->gtOp.gtOp1 = fgMorphTree(op1->gtOp.gtOp1);
                        op2->gtOp.gtOp1 = fgMorphTree(op2->gtOp.gtOp1);

                        // Propagate side effect flags up the tree
                        op1->gtFlags &= ~GTF_ALL_EFFECT;
                        op1->gtFlags |= (op1->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT);
                        op2->gtFlags &= ~GTF_ALL_EFFECT;
                        op2->gtFlags |= (op2->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT);

                        // If the GT_MUL can be altogether folded away, we should do that.

                        if ((op1->gtCast.CastOp()->OperKind() & op2->gtCast.CastOp()->OperKind() & GTK_CONST) &&
                            opts.OptEnabled(CLFLG_CONSTANTFOLD))
                        {
                            tree->gtOp.gtOp1 = op1 = gtFoldExprConst(op1);
                            tree->gtOp.gtOp2 = op2 = gtFoldExprConst(op2);
                            noway_assert(op1->OperKind() & op2->OperKind() & GTK_CONST);
                            tree = gtFoldExprConst(tree);
                            noway_assert(tree->OperIsConst());
                            return tree;
                        }

                        tree->gtFlags |= GTF_MUL_64RSLT;

                        // If op1 and op2 are unsigned casts, we need to do an unsigned mult
                        tree->gtFlags |= (op1->gtFlags & GTF_UNSIGNED);

                        // Insert GT_NOP nodes for the cast operands so that they do not get folded
                        // And propagate the new flags. We don't want to CSE the casts because
                        // codegen expects GTF_MUL_64RSLT muls to have a certain layout.

                        if (op1->gtCast.CastOp()->OperGet() != GT_NOP)
                        {
                            op1->gtOp.gtOp1 = gtNewOperNode(GT_NOP, TYP_INT, op1->gtCast.CastOp());
                            op1->gtFlags &= ~GTF_ALL_EFFECT;
                            op1->gtFlags |= (op1->gtCast.CastOp()->gtFlags & GTF_ALL_EFFECT);
                            op1->gtFlags |= GTF_DONT_CSE;
                        }

                        if (op2->gtCast.CastOp()->OperGet() != GT_NOP)
                        {
                            op2->gtOp.gtOp1 = gtNewOperNode(GT_NOP, TYP_INT, op2->gtCast.CastOp());
                            op2->gtFlags &= ~GTF_ALL_EFFECT;
                            op2->gtFlags |= (op2->gtCast.CastOp()->gtFlags & GTF_ALL_EFFECT);
                            op2->gtFlags |= GTF_DONT_CSE;
                        }

                        tree->gtFlags &= ~GTF_ALL_EFFECT;
                        tree->gtFlags |= ((op1->gtFlags | op2->gtFlags) & GTF_ALL_EFFECT);

                        goto DONE_MORPHING_CHILDREN;
                    }
                    else if ((tree->gtFlags & GTF_MUL_64RSLT) == 0)
                    {
                    NO_MUL_64RSLT:
                        if (tree->gtOverflow())
                            helper = (tree->gtFlags & GTF_UNSIGNED) ? CORINFO_HELP_ULMUL_OVF : CORINFO_HELP_LMUL_OVF;
                        else
                            helper = CORINFO_HELP_LMUL;

                        goto USE_HELPER_FOR_ARITH;
                    }
                    else
                    {
                        /* We are seeing this node again. We have decided to use
                           GTF_MUL_64RSLT, so leave it alone. */

                        assert(tree->gtIsValid64RsltMul());
                    }
                }
#endif // !_TARGET_64BIT_
                break;

            case GT_DIV:

#ifndef _TARGET_64BIT_
                if (typ == TYP_LONG)
                {
                    helper = CORINFO_HELP_LDIV;
                    goto USE_HELPER_FOR_ARITH;
                }

#if USE_HELPERS_FOR_INT_DIV
                if (typ == TYP_INT && !fgIsSignedDivOptimizable(op2))
                {
                    helper = CORINFO_HELP_DIV;
                    goto USE_HELPER_FOR_ARITH;
                }
#endif
#endif // !_TARGET_64BIT_

#ifndef LEGACY_BACKEND
                if (op2->gtOper == GT_CAST && op2->gtOp.gtOp1->IsCnsIntOrI())
                {
                    op2 = gtFoldExprConst(op2);
                }

                if (fgShouldUseMagicNumberDivide(tree->AsOp()))
                {
                    tree = fgMorphDivByConst(tree->AsOp());
                    op1  = tree->gtOp.gtOp1;
                    op2  = tree->gtOp.gtOp2;
                }
#endif // !LEGACY_BACKEND
                break;

            case GT_UDIV:

#ifndef _TARGET_64BIT_
                if (typ == TYP_LONG)
                {
                    helper = CORINFO_HELP_ULDIV;
                    goto USE_HELPER_FOR_ARITH;
                }
#if USE_HELPERS_FOR_INT_DIV
                if (typ == TYP_INT && !fgIsUnsignedDivOptimizable(op2))
                {
                    helper = CORINFO_HELP_UDIV;
                    goto USE_HELPER_FOR_ARITH;
                }
#endif
#endif // _TARGET_64BIT_
                break;

            case GT_MOD:

                if (varTypeIsFloating(typ))
                {
                    helper = CORINFO_HELP_DBLREM;
                    noway_assert(op2);
                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        if (op2->TypeGet() == TYP_FLOAT)
                        {
                            helper = CORINFO_HELP_FLTREM;
                        }
                        else
                        {
                            tree->gtOp.gtOp1 = op1 = gtNewCastNode(TYP_DOUBLE, op1, TYP_DOUBLE);
                        }
                    }
                    else if (op2->TypeGet() == TYP_FLOAT)
                    {
                        tree->gtOp.gtOp2 = op2 = gtNewCastNode(TYP_DOUBLE, op2, TYP_DOUBLE);
                    }
                    goto USE_HELPER_FOR_ARITH;
                }

                // Do not use optimizations (unlike UMOD's idiv optimizing during codegen) for signed mod.
                // A similar optimization for signed mod will not work for a negative perfectly divisible
                // HI-word. To make it correct, we would need to divide without the sign and then flip the
                // result sign after mod. This requires 18 opcodes + flow making it not worthy to inline.
                goto ASSIGN_HELPER_FOR_MOD;

            case GT_UMOD:

#ifdef _TARGET_ARMARCH_
//
// Note for _TARGET_ARMARCH_ we don't have  a remainder instruction, so we don't do this optimization
//
#else  // _TARGET_XARCH
            /* If this is an unsigned long mod with op2 which is a cast to long from a
               constant int, then don't morph to a call to the helper.  This can be done
               faster inline using idiv.
            */

            noway_assert(op2);
            if ((typ == TYP_LONG) && opts.OptEnabled(CLFLG_CONSTANTFOLD) &&
                ((tree->gtFlags & GTF_UNSIGNED) == (op1->gtFlags & GTF_UNSIGNED)) &&
                ((tree->gtFlags & GTF_UNSIGNED) == (op2->gtFlags & GTF_UNSIGNED)))
            {
                if (op2->gtOper == GT_CAST && op2->gtCast.CastOp()->gtOper == GT_CNS_INT &&
                    op2->gtCast.CastOp()->gtIntCon.gtIconVal >= 2 &&
                    op2->gtCast.CastOp()->gtIntCon.gtIconVal <= 0x3fffffff &&
                    (tree->gtFlags & GTF_UNSIGNED) == (op2->gtCast.CastOp()->gtFlags & GTF_UNSIGNED))
                {
                    tree->gtOp.gtOp2 = op2 = fgMorphCast(op2);
                    noway_assert(op2->gtOper == GT_CNS_NATIVELONG);
                }

                if (op2->gtOper == GT_CNS_NATIVELONG && op2->gtIntConCommon.LngValue() >= 2 &&
                    op2->gtIntConCommon.LngValue() <= 0x3fffffff)
                {
                    tree->gtOp.gtOp1 = op1 = fgMorphTree(op1);
                    noway_assert(op1->TypeGet() == TYP_LONG);

                    // Update flags for op1 morph
                    tree->gtFlags &= ~GTF_ALL_EFFECT;

                    tree->gtFlags |= (op1->gtFlags & GTF_ALL_EFFECT); // Only update with op1 as op2 is a constant

                    // If op1 is a constant, then do constant folding of the division operator
                    if (op1->gtOper == GT_CNS_NATIVELONG)
                    {
                        tree = gtFoldExpr(tree);
                    }
                    return tree;
                }
            }
#endif // _TARGET_XARCH

            ASSIGN_HELPER_FOR_MOD:

                // For "val % 1", return 0 if op1 doesn't have any side effects
                // and we are not in the CSE phase, we cannot discard 'tree'
                // because it may contain CSE expressions that we haven't yet examined.
                //
                if (((op1->gtFlags & GTF_SIDE_EFFECT) == 0) && !optValnumCSE_phase)
                {
                    if (op2->IsIntegralConst(1))
                    {
                        GenTreePtr zeroNode = gtNewZeroConNode(typ);
#ifdef DEBUG
                        zeroNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                        DEBUG_DESTROY_NODE(tree);
                        return zeroNode;
                    }
                }

#ifndef _TARGET_64BIT_
                if (typ == TYP_LONG)
                {
                    helper = (oper == GT_UMOD) ? CORINFO_HELP_ULMOD : CORINFO_HELP_LMOD;
                    goto USE_HELPER_FOR_ARITH;
                }

#if USE_HELPERS_FOR_INT_DIV
                if (typ == TYP_INT)
                {
                    if (oper == GT_UMOD && !fgIsUnsignedModOptimizable(op2))
                    {
                        helper = CORINFO_HELP_UMOD;
                        goto USE_HELPER_FOR_ARITH;
                    }
                    else if (oper == GT_MOD && !fgIsSignedModOptimizable(op2))
                    {
                        helper = CORINFO_HELP_MOD;
                        goto USE_HELPER_FOR_ARITH;
                    }
                }
#endif
#endif // !_TARGET_64BIT_

#ifndef LEGACY_BACKEND
                if (op2->gtOper == GT_CAST && op2->gtOp.gtOp1->IsCnsIntOrI())
                {
                    op2 = gtFoldExprConst(op2);
                }

#ifdef _TARGET_ARM64_

                // For ARM64 we don't have a remainder instruction,
                // The architecture manual suggests the following transformation to
                // generate code for such operator:
                //
                // a % b = a - (a / b) * b;
                //
                tree = fgMorphModToSubMulDiv(tree->AsOp());
                op1  = tree->gtOp.gtOp1;
                op2  = tree->gtOp.gtOp2;

#else // !_TARGET_ARM64_

                if (oper != GT_UMOD && fgShouldUseMagicNumberDivide(tree->AsOp()))
                {
                    tree = fgMorphModByConst(tree->AsOp());
                    op1  = tree->gtOp.gtOp1;
                    op2  = tree->gtOp.gtOp2;
                }

#endif //_TARGET_ARM64_
#endif // !LEGACY_BACKEND
                break;

            USE_HELPER_FOR_ARITH:
            {
                /* We have to morph these arithmetic operations into helper calls
                   before morphing the arguments (preorder), else the arguments
                   won't get correct values of fgPtrArgCntCur.
                   However, try to fold the tree first in case we end up with a
                   simple node which won't need a helper call at all */

                noway_assert(tree->OperIsBinary());

                GenTreePtr oldTree = tree;

                tree = gtFoldExpr(tree);

                // Were we able to fold it ?
                // Note that gtFoldExpr may return a non-leaf even if successful
                // e.g. for something like "expr / 1" - see also bug #290853
                if (tree->OperIsLeaf() || (oldTree != tree))

                {
                    return (oldTree != tree) ? fgMorphTree(tree) : fgMorphLeaf(tree);
                }

                // Did we fold it into a comma node with throw?
                if (tree->gtOper == GT_COMMA)
                {
                    noway_assert(fgIsCommaThrow(tree));
                    return fgMorphTree(tree);
                }
            }
                return fgMorphIntoHelperCall(tree, helper, gtNewArgList(op1, op2));

            case GT_RETURN:
                // normalize small integer return values
                if (fgGlobalMorph && varTypeIsSmall(info.compRetType) && (op1 != nullptr) &&
                    (op1->TypeGet() != TYP_VOID) && fgCastNeeded(op1, info.compRetType))
                {
                    // Small-typed return values are normalized by the callee
                    op1 = gtNewCastNode(TYP_INT, op1, info.compRetType);

                    // Propagate GTF_COLON_COND
                    op1->gtFlags |= (tree->gtFlags & GTF_COLON_COND);

                    tree->gtOp.gtOp1 = fgMorphCast(op1);

                    // Propagate side effect flags
                    tree->gtFlags &= ~GTF_ALL_EFFECT;
                    tree->gtFlags |= (tree->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT);

                    return tree;
                }
                break;

            case GT_EQ:
            case GT_NE:

                // Check for typeof(...) == obj.GetType()
                // Also check for typeof(...) == typeof(...)
                // IMPORTANT NOTE: this optimization relies on a one-to-one mapping between
                // type handles and instances of System.Type
                // If this invariant is ever broken, the optimization will need updating
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef LEGACY_BACKEND
                if (op1->gtOper == GT_CALL && op2->gtOper == GT_CALL &&
                    ((op1->gtCall.gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) ||
                     (op1->gtCall.gtCallType == CT_HELPER)) &&
                    ((op2->gtCall.gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) ||
                     (op2->gtCall.gtCallType == CT_HELPER)))
#else
            if ((((op1->gtOper == GT_INTRINSIC) &&
                  (op1->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Object_GetType)) ||
                 ((op1->gtOper == GT_CALL) && (op1->gtCall.gtCallType == CT_HELPER))) &&
                (((op2->gtOper == GT_INTRINSIC) &&
                  (op2->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Object_GetType)) ||
                 ((op2->gtOper == GT_CALL) && (op2->gtCall.gtCallType == CT_HELPER))))
#endif
                {
                    GenTreePtr pGetClassFromHandle;
                    GenTreePtr pGetType;

#ifdef LEGACY_BACKEND
                    bool bOp1ClassFromHandle = gtIsTypeHandleToRuntimeTypeHelper(op1);
                    bool bOp2ClassFromHandle = gtIsTypeHandleToRuntimeTypeHelper(op2);
#else
                bool bOp1ClassFromHandle = op1->gtOper == GT_CALL ? gtIsTypeHandleToRuntimeTypeHelper(op1) : false;
                bool bOp2ClassFromHandle = op2->gtOper == GT_CALL ? gtIsTypeHandleToRuntimeTypeHelper(op2) : false;
#endif

                    // Optimize typeof(...) == typeof(...)
                    // Typically this occurs in generic code that attempts a type switch
                    // e.g. typeof(T) == typeof(int)

                    if (bOp1ClassFromHandle && bOp2ClassFromHandle)
                    {
                        GenTreePtr classFromHandleArg1 = tree->gtOp.gtOp1->gtCall.gtCallArgs->gtOp.gtOp1;
                        GenTreePtr classFromHandleArg2 = tree->gtOp.gtOp2->gtCall.gtCallArgs->gtOp.gtOp1;

                        GenTreePtr compare = gtNewOperNode(oper, TYP_INT, classFromHandleArg1, classFromHandleArg2);

                        compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);

                        // Morph and return
                        return fgMorphTree(compare);
                    }
                    else if (bOp1ClassFromHandle || bOp2ClassFromHandle)
                    {
                        //
                        // Now check for GetClassFromHandle(handle) == obj.GetType()
                        //

                        if (bOp1ClassFromHandle)
                        {
                            pGetClassFromHandle = tree->gtOp.gtOp1;
                            pGetType            = op2;
                        }
                        else
                        {
                            pGetClassFromHandle = tree->gtOp.gtOp2;
                            pGetType            = op1;
                        }

                        GenTreePtr pGetClassFromHandleArgument = pGetClassFromHandle->gtCall.gtCallArgs->gtOp.gtOp1;
                        GenTreePtr pConstLiteral               = pGetClassFromHandleArgument;

                        // Unwrap GT_NOP node used to prevent constant folding
                        if (pConstLiteral->gtOper == GT_NOP && pConstLiteral->gtType == TYP_I_IMPL)
                        {
                            pConstLiteral = pConstLiteral->gtOp.gtOp1;
                        }

                        // In the ngen case, we have to go thru an indirection to get the right handle.
                        if (pConstLiteral->gtOper == GT_IND)
                        {
                            pConstLiteral = pConstLiteral->gtOp.gtOp1;
                        }
#ifdef LEGACY_BACKEND

                        if (pGetType->gtCall.gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC &&
                            info.compCompHnd->getIntrinsicID(pGetType->gtCall.gtCallMethHnd) ==
                                CORINFO_INTRINSIC_Object_GetType &&
#else
                    if ((pGetType->gtOper == GT_INTRINSIC) &&
                        (pGetType->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Object_GetType) &&
#endif
                            pConstLiteral->gtOper == GT_CNS_INT && pConstLiteral->gtType == TYP_I_IMPL)
                        {
                            CORINFO_CLASS_HANDLE clsHnd =
                                CORINFO_CLASS_HANDLE(pConstLiteral->gtIntCon.gtCompileTimeHandle);

                            if (info.compCompHnd->canInlineTypeCheckWithObjectVTable(clsHnd))
                            {
                                // Method Table tree
                                CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef LEGACY_BACKEND
                                GenTreePtr objMT = gtNewOperNode(GT_IND, TYP_I_IMPL, pGetType->gtCall.gtCallObjp);
#else
                            GenTreePtr objMT = gtNewOperNode(GT_IND, TYP_I_IMPL, pGetType->gtUnOp.gtOp1);
#endif
                                objMT->gtFlags |= GTF_EXCEPT; // Null ref exception if object is null
                                compCurBB->bbFlags |= BBF_HAS_VTABREF;
                                optMethodFlags |= OMF_HAS_VTABLEREF;

                                // Method table constant
                                GenTreePtr cnsMT = pGetClassFromHandleArgument;

                                GenTreePtr compare = gtNewOperNode(oper, TYP_INT, objMT, cnsMT);

                                compare->gtFlags |=
                                    tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);

                                // Morph and return
                                return fgMorphTree(compare);
                            }
                        }
                    }
                }
                fgMorphRecognizeBoxNullable(tree);
                op1 = tree->gtOp.gtOp1;
                op2 = tree->gtGetOp2();

                break;

#ifdef _TARGET_ARM_
            case GT_INTRINSIC:
                if (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round)
                {
                    switch (tree->TypeGet())
                    {
                        case TYP_DOUBLE:
                            return fgMorphIntoHelperCall(tree, CORINFO_HELP_DBLROUND, gtNewArgList(op1));
                        case TYP_FLOAT:
                            return fgMorphIntoHelperCall(tree, CORINFO_HELP_FLTROUND, gtNewArgList(op1));
                        default:
                            unreached();
                    }
                }
                break;
#endif

            default:
                break;
        }

#if !CPU_HAS_FP_SUPPORT
        tree = fgMorphToEmulatedFP(tree);
#endif

        /* Could this operator throw an exception? */
        if (fgGlobalMorph && tree->OperMayThrow())
        {
            if ((tree->OperGet() != GT_IND) || fgAddrCouldBeNull(tree->gtOp.gtOp1))
            {
                /* Mark the tree node as potentially throwing an exception */
                tree->gtFlags |= GTF_EXCEPT;
            }
        }

        /*-------------------------------------------------------------------------
         * Process the first operand, if any
         */

        if (op1)
        {

#if LOCAL_ASSERTION_PROP
            // If we are entering the "then" part of a Qmark-Colon we must
            // save the state of the current copy assignment table
            // so that we can restore this state when entering the "else" part
            if (isQmarkColon)
            {
                noway_assert(optLocalAssertionProp);
                if (optAssertionCount)
                {
                    noway_assert(optAssertionCount <= optMaxAssertionCount); // else ALLOCA() is a bad idea
                    unsigned tabSize   = optAssertionCount * sizeof(AssertionDsc);
                    origAssertionTab   = (AssertionDsc*)ALLOCA(tabSize);
                    origAssertionCount = optAssertionCount;
                    memcpy(origAssertionTab, optAssertionTabPrivate, tabSize);
                }
                else
                {
                    origAssertionCount = 0;
                    origAssertionTab   = nullptr;
                }
            }
#endif // LOCAL_ASSERTION_PROP

            // We might need a new MorphAddressContext context.  (These are used to convey
            // parent context about how addresses being calculated will be used; see the
            // specification comment for MorphAddrContext for full details.)
            // Assume it's an Ind context to start.
            MorphAddrContext  subIndMac1(MACK_Ind);
            MorphAddrContext* subMac1 = mac;
            if (subMac1 == nullptr || subMac1->m_kind == MACK_Ind || subMac1->m_kind == MACK_CopyBlock)
            {
                switch (tree->gtOper)
                {
                    case GT_ADDR:
                        if (subMac1 == nullptr)
                        {
                            subMac1         = &subIndMac1;
                            subMac1->m_kind = MACK_Addr;
                        }
                        break;
                    case GT_COMMA:
                        // In a comma, the incoming context only applies to the rightmost arg of the
                        // comma list.  The left arg (op1) gets a fresh context.
                        subMac1 = nullptr;
                        break;
                    case GT_COPYBLK:
                    case GT_COPYOBJ:
                        assert(subMac1 == nullptr); // Should only occur at top level, since value is void.
                        subMac1 = &s_CopyBlockMAC;
                        break;
                    case GT_LIST:
                        // If the list is the first arg of a copy block, its two args should be evaluated as
                        // IND-context addresses, separately.
                        if (subMac1 != nullptr && subMac1->m_kind == MACK_CopyBlock)
                        {
                            subMac1 = &subIndMac1;
                        }
                        break;
                    case GT_IND:
                    case GT_INITBLK:
                    case GT_OBJ:
                        subMac1 = &subIndMac1;
                        break;
                    default:
                        break;
                }
            }

            // For additions, if we're in an IND context keep track of whether
            // all offsets added to the address are constant, and their sum.
            if (tree->gtOper == GT_ADD && subMac1 != nullptr)
            {
                assert(subMac1->m_kind == MACK_Ind || subMac1->m_kind == MACK_Addr); // Can't be a CopyBlock.
                GenTreePtr otherOp = tree->gtOp.gtOp2;
                // Is the other operator a constant?
                if (otherOp->IsCnsIntOrI())
                {
                    ClrSafeInt<size_t> totalOffset(subMac1->m_totalOffset);
                    totalOffset += otherOp->gtIntConCommon.IconValue();
                    if (totalOffset.IsOverflow())
                    {
                        // We will consider an offset so large as to overflow as "not a constant" --
                        // we will do a null check.
                        subMac1->m_allConstantOffsets = false;
                    }
                    else
                    {
                        subMac1->m_totalOffset += otherOp->gtIntConCommon.IconValue();
                    }
                }
                else
                {
                    subMac1->m_allConstantOffsets = false;
                }
            }

            tree->gtOp.gtOp1 = op1 = fgMorphTree(op1, subMac1);

#if LOCAL_ASSERTION_PROP
            // If we are exiting the "then" part of a Qmark-Colon we must
            // save the state of the current copy assignment table
            // so that we can merge this state with the "else" part exit
            if (isQmarkColon)
            {
                noway_assert(optLocalAssertionProp);
                if (optAssertionCount)
                {
                    noway_assert(optAssertionCount <= optMaxAssertionCount); // else ALLOCA() is a bad idea
                    unsigned tabSize   = optAssertionCount * sizeof(AssertionDsc);
                    thenAssertionTab   = (AssertionDsc*)ALLOCA(tabSize);
                    thenAssertionCount = optAssertionCount;
                    memcpy(thenAssertionTab, optAssertionTabPrivate, tabSize);
                }
                else
                {
                    thenAssertionCount = 0;
                    thenAssertionTab   = nullptr;
                }
            }
#endif // LOCAL_ASSERTION_PROP

            /* Morphing along with folding and inlining may have changed the
             * side effect flags, so we have to reset them
             *
             * NOTE: Don't reset the exception flags on nodes that may throw */

            noway_assert(tree->gtOper != GT_CALL);

            if ((tree->gtOper != GT_INTRINSIC) || !IsIntrinsicImplementedByUserCall(tree->gtIntrinsic.gtIntrinsicId))
            {
                tree->gtFlags &= ~GTF_CALL;
            }

            if (!tree->OperMayThrow())
            {
                tree->gtFlags &= ~GTF_EXCEPT;
            }

            /* Propagate the new flags */
            tree->gtFlags |= (op1->gtFlags & GTF_ALL_EFFECT);

            // &aliasedVar doesn't need GTF_GLOB_REF, though alisasedVar does
            // Similarly for clsVar
            if (oper == GT_ADDR && (op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_CLS_VAR))
            {
                tree->gtFlags &= ~GTF_GLOB_REF;
            }
        } // if (op1)

        /*-------------------------------------------------------------------------
         * Process the second operand, if any
         */

        if (op2)
        {

#if LOCAL_ASSERTION_PROP
            // If we are entering the "else" part of a Qmark-Colon we must
            // reset the state of the current copy assignment table
            if (isQmarkColon)
            {
                noway_assert(optLocalAssertionProp);
                optAssertionReset(0);
                if (origAssertionCount)
                {
                    size_t tabSize = origAssertionCount * sizeof(AssertionDsc);
                    memcpy(optAssertionTabPrivate, origAssertionTab, tabSize);
                    optAssertionReset(origAssertionCount);
                }
            }
#endif // LOCAL_ASSERTION_PROP

            // We might need a new MorphAddressContext context to use in evaluating op2.
            // (These are used to convey parent context about how addresses being calculated
            // will be used; see the specification comment for MorphAddrContext for full details.)
            // Assume it's an Ind context to start.
            MorphAddrContext subIndMac2(MACK_Ind);
            switch (tree->gtOper)
            {
                case GT_ADD:
                    if (mac != nullptr && mac->m_kind == MACK_Ind)
                    {
                        GenTreePtr otherOp = tree->gtOp.gtOp1;
                        // Is the other operator a constant?
                        if (otherOp->IsCnsIntOrI())
                        {
                            mac->m_totalOffset += otherOp->gtIntConCommon.IconValue();
                        }
                        else
                        {
                            mac->m_allConstantOffsets = false;
                        }
                    }
                    break;
                case GT_LIST:
                    if (mac != nullptr && mac->m_kind == MACK_CopyBlock)
                    {
                        mac = &subIndMac2;
                    }
                    break;
                default:
                    break;
            }
            tree->gtOp.gtOp2 = op2 = fgMorphTree(op2, mac);

            /* Propagate the side effect flags from op2 */

            tree->gtFlags |= (op2->gtFlags & GTF_ALL_EFFECT);

#if LOCAL_ASSERTION_PROP
            // If we are exiting the "else" part of a Qmark-Colon we must
            // merge the state of the current copy assignment table with
            // that of the exit of the "then" part.
            if (isQmarkColon)
            {
                noway_assert(optLocalAssertionProp);
                // If either exit table has zero entries then
                // the merged table also has zero entries
                if (optAssertionCount == 0 || thenAssertionCount == 0)
                {
                    optAssertionReset(0);
                }
                else
                {
                    size_t tabSize = optAssertionCount * sizeof(AssertionDsc);
                    if ((optAssertionCount != thenAssertionCount) ||
                        (memcmp(thenAssertionTab, optAssertionTabPrivate, tabSize) != 0))
                    {
                        // Yes they are different so we have to find the merged set
                        // Iterate over the copy asgn table removing any entries
                        // that do not have an exact match in the thenAssertionTab
                        AssertionIndex index = 1;
                        while (index <= optAssertionCount)
                        {
                            AssertionDsc* curAssertion = optGetAssertion(index);

                            for (unsigned j = 0; j < thenAssertionCount; j++)
                            {
                                AssertionDsc* thenAssertion = &thenAssertionTab[j];

                                // Do the left sides match?
                                if ((curAssertion->op1.lcl.lclNum == thenAssertion->op1.lcl.lclNum) &&
                                    (curAssertion->assertionKind == thenAssertion->assertionKind))
                                {
                                    // Do the right sides match?
                                    if ((curAssertion->op2.kind == thenAssertion->op2.kind) &&
                                        (curAssertion->op2.lconVal == thenAssertion->op2.lconVal))
                                    {
                                        goto KEEP;
                                    }
                                    else
                                    {
                                        goto REMOVE;
                                    }
                                }
                            }
                        //
                        // If we fall out of the loop above then we didn't find
                        // any matching entry in the thenAssertionTab so it must
                        // have been killed on that path so we remove it here
                        //
                        REMOVE:
                            // The data at optAssertionTabPrivate[i] is to be removed
                            CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("The QMARK-COLON ");
                                printTreeID(tree);
                                printf(" removes assertion candidate #%d\n", index);
                            }
#endif
                            optAssertionRemove(index);
                            continue;
                        KEEP:
                            // The data at optAssertionTabPrivate[i] is to be kept
                            index++;
                        }
                    }
                }
            }
#endif    // LOCAL_ASSERTION_PROP
        } // if (op2)

    DONE_MORPHING_CHILDREN:

/*-------------------------------------------------------------------------
 * Now do POST-ORDER processing
 */

#if FEATURE_FIXED_OUT_ARGS && !defined(_TARGET_64BIT_)
        // Variable shifts of a long end up being helper calls, so mark the tree as such. This
        // is potentially too conservative, since they'll get treated as having side effects.
        // It is important to mark them as calls so if they are part of an argument list,
        // they will get sorted and processed properly (for example, it is important to handle
        // all nested calls before putting struct arguments in the argument registers). We
        // could mark the trees just before argument processing, but it would require a full
        // tree walk of the argument tree, so we just do it here, instead, even though we'll
        // mark non-argument trees (that will still get converted to calls, anyway).
        if (GenTree::OperIsShift(oper) && (tree->TypeGet() == TYP_LONG) && (op2->OperGet() != GT_CNS_INT))
        {
            tree->gtFlags |= GTF_CALL;
        }
#endif // FEATURE_FIXED_OUT_ARGS && !_TARGET_64BIT_

        if (varTypeIsGC(tree->TypeGet()) && (op1 && !varTypeIsGC(op1->TypeGet())) &&
            (op2 && !varTypeIsGC(op2->TypeGet())))
        {
            // The tree is really not GC but was marked as such. Now that the
            // children have been unmarked, unmark the tree too.

            // Remember that GT_COMMA inherits it's type only from op2
            if (tree->gtOper == GT_COMMA)
            {
                tree->gtType = genActualType(op2->TypeGet());
            }
            else
            {
                tree->gtType = genActualType(op1->TypeGet());
            }
        }

        GenTreePtr oldTree = tree;

        GenTreePtr qmarkOp1 = nullptr;
        GenTreePtr qmarkOp2 = nullptr;

        if ((tree->OperGet() == GT_QMARK) && (tree->gtOp.gtOp2->OperGet() == GT_COLON))
        {
            qmarkOp1 = oldTree->gtOp.gtOp2->gtOp.gtOp1;
            qmarkOp2 = oldTree->gtOp.gtOp2->gtOp.gtOp2;
        }

        // Try to fold it, maybe we get lucky,
        tree = gtFoldExpr(tree);

        if (oldTree != tree)
        {
            /* if gtFoldExpr returned op1 or op2 then we are done */
            if ((tree == op1) || (tree == op2) || (tree == qmarkOp1) || (tree == qmarkOp2))
            {
                return tree;
            }

            /* If we created a comma-throw tree then we need to morph op1 */
            if (fgIsCommaThrow(tree))
            {
                tree->gtOp.gtOp1 = fgMorphTree(tree->gtOp.gtOp1);
                fgMorphTreeDone(tree);
                return tree;
            }

            return tree;
        }
        else if (tree->OperKind() & GTK_CONST)
        {
            return tree;
        }

        /* gtFoldExpr could have used setOper to change the oper */
        oper = tree->OperGet();
        typ  = tree->TypeGet();

        /* gtFoldExpr could have changed op1 and op2 */
        op1 = tree->gtOp.gtOp1;
        op2 = tree->gtGetOp2();

        // Do we have an integer compare operation?
        //
        if (tree->OperIsCompare() && varTypeIsIntegralOrI(tree->TypeGet()))
        {
            // Are we comparing against zero?
            //
            if (op2->IsIntegralConst(0))
            {
                // Request that the codegen for op1 sets the condition flags
                // when it generates the code for op1.
                //
                // Codegen for op1 must set the condition flags if
                // this method returns true.
                //
                op1->gtRequestSetFlags();
            }
        }
        /*-------------------------------------------------------------------------
         * Perform the required oper-specific postorder morphing
         */

        GenTreePtr           temp;
        GenTreePtr           cns1, cns2;
        GenTreePtr           thenNode;
        GenTreePtr           elseNode;
        size_t               ival1, ival2;
        GenTreePtr           lclVarTree;
        GenTreeLclVarCommon* lclVarCmnTree;
        FieldSeqNode*        fieldSeq = nullptr;

        switch (oper)
        {
            case GT_ASG:

                lclVarTree = fgIsIndirOfAddrOfLocal(op1);
                if (lclVarTree != nullptr)
                {
                    lclVarTree->gtFlags |= GTF_VAR_DEF;
                }

                /* If we are storing a small type, we might be able to omit a cast */
                if ((op1->gtOper == GT_IND) && varTypeIsSmall(op1->TypeGet()))
                {
                    if (!gtIsActiveCSE_Candidate(op2) && (op2->gtOper == GT_CAST) && !op2->gtOverflow())
                    {
                        var_types castType = op2->CastToType();

                        // If we are performing a narrowing cast and
                        // castType is larger or the same as op1's type
                        // then we can discard the cast.

                        if (varTypeIsSmall(castType) && (castType >= op1->TypeGet()))
                        {
                            tree->gtOp.gtOp2 = op2 = op2->gtCast.CastOp();
                        }
                    }
                    else if (op2->OperIsCompare() && varTypeIsByte(op1->TypeGet()))
                    {
                        /* We don't need to zero extend the setcc instruction */
                        op2->gtType = TYP_BYTE;
                    }
                }
                // If we introduced a CSE we may need to undo the optimization above
                // (i.e. " op2->gtType = TYP_BYTE;" which depends upon op1 being a GT_IND of a byte type)
                // When we introduce the CSE we remove the GT_IND and subsitute a GT_LCL_VAR in it place.
                else if (op2->OperIsCompare() && (op2->gtType == TYP_BYTE) && (op1->gtOper == GT_LCL_VAR))
                {
                    unsigned   varNum = op1->gtLclVarCommon.gtLclNum;
                    LclVarDsc* varDsc = &lvaTable[varNum];

                    /* We again need to zero extend the setcc instruction */
                    op2->gtType = varDsc->TypeGet();
                }

                __fallthrough;

            case GT_COPYOBJ:
            case GT_COPYBLK:
            case GT_INITBLK:
                fgAssignSetVarDef(tree);

                __fallthrough;

            case GT_ASG_ADD:
            case GT_ASG_SUB:
            case GT_ASG_MUL:
            case GT_ASG_DIV:
            case GT_ASG_MOD:
            case GT_ASG_UDIV:
            case GT_ASG_UMOD:
            case GT_ASG_OR:
            case GT_ASG_XOR:
            case GT_ASG_AND:
            case GT_ASG_LSH:
            case GT_ASG_RSH:
            case GT_ASG_RSZ:

                /* We can't CSE the LHS of an assignment */
                /* We also must set in the pre-morphing phase, otherwise assertionProp doesn't see it */
                op1->gtFlags |= GTF_DONT_CSE;
                break;

            case GT_EQ:
            case GT_NE:

                /* Make sure we're allowed to do this */

                if (optValnumCSE_phase)
                {
                    // It is not safe to reorder/delete CSE's
                    break;
                }

                cns2 = op2;

                /* Check for "(expr +/- icon1) ==/!= (non-zero-icon2)" */

                if (cns2->gtOper == GT_CNS_INT && cns2->gtIntCon.gtIconVal != 0)
                {
                    op1 = tree->gtOp.gtOp1;

                    /* Since this can occur repeatedly we use a while loop */

                    while ((op1->gtOper == GT_ADD || op1->gtOper == GT_SUB) &&
                           (op1->gtOp.gtOp2->gtOper == GT_CNS_INT) && (op1->gtType == TYP_INT) &&
                           (op1->gtOverflow() == false))
                    {
                        /* Got it; change "x+icon1==icon2" to "x==icon2-icon1" */

                        ival1 = op1->gtOp.gtOp2->gtIntCon.gtIconVal;
                        ival2 = cns2->gtIntCon.gtIconVal;

                        if (op1->gtOper == GT_ADD)
                        {
                            ival2 -= ival1;
                        }
                        else
                        {
                            ival2 += ival1;
                        }
                        cns2->gtIntCon.gtIconVal = ival2;

#ifdef _TARGET_64BIT_
                        // we need to properly re-sign-extend or truncate as needed.
                        cns2->AsIntCon()->TruncateOrSignExtend32();
#endif // _TARGET_64BIT_

                        op1 = tree->gtOp.gtOp1 = op1->gtOp.gtOp1;
                    }
                }

                //
                // Here we look for the following tree
                //
                //                        EQ/NE
                //                        /  \
        //                      op1   CNS 0/1
                //
                ival2 = INT_MAX; // The value of INT_MAX for ival2 just means that the constant value is not 0 or 1

                // cast to unsigned allows test for both 0 and 1
                if ((cns2->gtOper == GT_CNS_INT) && (((size_t)cns2->gtIntConCommon.IconValue()) <= 1U))
                {
                    ival2 = (size_t)cns2->gtIntConCommon.IconValue();
                }
                else // cast to UINT64 allows test for both 0 and 1
                    if ((cns2->gtOper == GT_CNS_LNG) && (((UINT64)cns2->gtIntConCommon.LngValue()) <= 1ULL))
                {
                    ival2 = (size_t)cns2->gtIntConCommon.LngValue();
                }

                if (ival2 != INT_MAX)
                {
                    // If we don't have a comma and relop, we can't do this optimization
                    //
                    if ((op1->gtOper == GT_COMMA) && (op1->gtOp.gtOp2->OperIsCompare()))
                    {
                        // Here we look for the following transformation
                        //
                        //                  EQ/NE                    Possible REVERSE(RELOP)
                        //                  /  \                           /      \
                //               COMMA CNS 0/1             ->   COMMA   relop_op2
                        //              /   \                          /    \
                //             x  RELOP                       x     relop_op1
                        //               /    \
                //         relop_op1  relop_op2
                        //
                        //
                        //
                        GenTreePtr comma = op1;
                        GenTreePtr relop = comma->gtOp.gtOp2;

                        GenTreePtr relop_op1 = relop->gtOp.gtOp1;

                        bool reverse = ((ival2 == 0) == (oper == GT_EQ));

                        if (reverse)
                        {
                            gtReverseCond(relop);
                        }

                        relop->gtOp.gtOp1 = comma;
                        comma->gtOp.gtOp2 = relop_op1;

                        // Comma now has fewer nodes underneath it, so we need to regenerate its flags
                        comma->gtFlags &= ~GTF_ALL_EFFECT;
                        comma->gtFlags |= (comma->gtOp.gtOp1->gtFlags) & GTF_ALL_EFFECT;
                        comma->gtFlags |= (comma->gtOp.gtOp2->gtFlags) & GTF_ALL_EFFECT;

                        noway_assert((relop->gtFlags & GTF_RELOP_JMP_USED) == 0);
                        noway_assert((relop->gtFlags & GTF_REVERSE_OPS) == 0);
                        relop->gtFlags |=
                            tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE | GTF_ALL_EFFECT);

                        return relop;
                    }

                    if (op1->gtOper == GT_COMMA)
                    {
                        // Here we look for the following tree
                        // and when the LCL_VAR is a temp we can fold the tree:
                        //
                        //                        EQ/NE                  EQ/NE
                        //                        /  \                   /  \
                //                     COMMA  CNS 0/1  ->     RELOP CNS 0/1
                        //                     /   \                   / \
                //                   ASG  LCL_VAR
                        //                  /  \
                //           LCL_VAR   RELOP
                        //                      / \
                //

                        GenTreePtr asg = op1->gtOp.gtOp1;
                        GenTreePtr lcl = op1->gtOp.gtOp2;

                        /* Make sure that the left side of the comma is the assignment of the LCL_VAR */
                        if (asg->gtOper != GT_ASG)
                        {
                            goto SKIP;
                        }

                        /* The right side of the comma must be a LCL_VAR temp */
                        if (lcl->gtOper != GT_LCL_VAR)
                        {
                            goto SKIP;
                        }

                        unsigned lclNum = lcl->gtLclVarCommon.gtLclNum;
                        noway_assert(lclNum < lvaCount);

                        /* If the LCL_VAR is not a temp then bail, a temp has a single def */
                        if (!lvaTable[lclNum].lvIsTemp)
                        {
                            goto SKIP;
                        }

#if FEATURE_ANYCSE
                        /* If the LCL_VAR is a CSE temp then bail, it could have multiple defs/uses */
                        // Fix 383856 X86/ARM ILGEN
                        if (lclNumIsCSE(lclNum))
                        {
                            goto SKIP;
                        }
#endif

                        /* We also must be assigning the result of a RELOP */
                        if (asg->gtOp.gtOp1->gtOper != GT_LCL_VAR)
                        {
                            goto SKIP;
                        }

                        /* Both of the LCL_VAR must match */
                        if (asg->gtOp.gtOp1->gtLclVarCommon.gtLclNum != lclNum)
                        {
                            goto SKIP;
                        }

                        /* If right side of asg is not a RELOP then skip */
                        if (!asg->gtOp.gtOp2->OperIsCompare())
                        {
                            goto SKIP;
                        }

                        LclVarDsc* varDsc = lvaTable + lclNum;

                        /* Set op1 to the right side of asg, (i.e. the RELOP) */
                        op1 = asg->gtOp.gtOp2;

                        DEBUG_DESTROY_NODE(asg->gtOp.gtOp1);
                        DEBUG_DESTROY_NODE(lcl);

                        /* This local variable should never be used again */
                        // <BUGNUM>
                        // VSW 184221: Make RefCnt to zero to indicate that this local var
                        // is not used any more. (Keey the lvType as is.)
                        // Otherwise lvOnFrame will be set to true in Compiler::raMarkStkVars
                        // And then emitter::emitEndCodeGen will assert in the following line:
                        //        noway_assert( dsc->lvTracked);
                        // </BUGNUM>
                        noway_assert(varDsc->lvRefCnt == 0 || // lvRefCnt may not have been set yet.
                                     varDsc->lvRefCnt == 2    // Or, we assume this tmp should only be used here,
                                                              // and it only shows up twice.
                                     );
                        lvaTable[lclNum].lvRefCnt = 0;
                        lvaTable[lclNum].lvaResetSortAgainFlag(this);
                    }

                    if (op1->OperIsCompare())
                    {
                        // Here we look for the following tree
                        //
                        //                        EQ/NE           ->      RELOP/!RELOP
                        //                        /  \                       /    \
                //                     RELOP  CNS 0/1
                        //                     /   \
                //
                        // Note that we will remove/destroy the EQ/NE node and move
                        // the RELOP up into it's location.

                        /* Here we reverse the RELOP if necessary */

                        bool reverse = ((ival2 == 0) == (oper == GT_EQ));

                        if (reverse)
                        {
                            gtReverseCond(op1);
                        }

                        /* Propagate gtType of tree into op1 in case it is TYP_BYTE for setcc optimization */
                        op1->gtType = tree->gtType;

                        noway_assert((op1->gtFlags & GTF_RELOP_JMP_USED) == 0);
                        op1->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);

                        DEBUG_DESTROY_NODE(tree);
                        return op1;
                    }

                    //
                    // Now we check for a compare with the result of an '&' operator
                    //
                    // Here we look for the following transformation:
                    //
                    //                        EQ/NE                  EQ/NE
                    //                        /  \                   /  \
            //                      AND   CNS 0/1  ->      AND   CNS 0
                    //                     /   \                  /   \
            //                RSZ/RSH   CNS 1            x     CNS (1 << y)
                    //                  /  \
            //                 x   CNS_INT +y

                    if (op1->gtOper == GT_AND)
                    {
                        GenTreePtr andOp    = op1;
                        GenTreePtr rshiftOp = andOp->gtOp.gtOp1;

                        if ((rshiftOp->gtOper != GT_RSZ) && (rshiftOp->gtOper != GT_RSH))
                        {
                            goto SKIP;
                        }

                        if (!rshiftOp->gtOp.gtOp2->IsCnsIntOrI())
                        {
                            goto SKIP;
                        }

                        ssize_t shiftAmount = rshiftOp->gtOp.gtOp2->gtIntCon.gtIconVal;

                        if (shiftAmount < 0)
                        {
                            goto SKIP;
                        }

                        if (!andOp->gtOp.gtOp2->IsIntegralConst(1))
                        {
                            goto SKIP;
                        }

                        if (andOp->gtType == TYP_INT)
                        {
                            if (shiftAmount > 31)
                            {
                                goto SKIP;
                            }

                            UINT32 newAndOperand = ((UINT32)1) << shiftAmount;

                            andOp->gtOp.gtOp2->gtIntCon.gtIconVal = newAndOperand;

                            // Reverse the cond if necessary
                            if (ival2 == 1)
                            {
                                gtReverseCond(tree);
                                cns2->gtIntCon.gtIconVal = 0;
                                oper                     = tree->gtOper;
                            }
                        }
                        else if (andOp->gtType == TYP_LONG)
                        {
                            if (shiftAmount > 63)
                            {
                                goto SKIP;
                            }

                            UINT64 newAndOperand = ((UINT64)1) << shiftAmount;

                            andOp->gtOp.gtOp2->gtIntConCommon.SetLngValue(newAndOperand);

                            // Reverse the cond if necessary
                            if (ival2 == 1)
                            {
                                gtReverseCond(tree);
                                cns2->gtIntConCommon.SetLngValue(0);
                                oper = tree->gtOper;
                            }
                        }

                        andOp->gtOp.gtOp1 = rshiftOp->gtOp.gtOp1;

                        DEBUG_DESTROY_NODE(rshiftOp->gtOp.gtOp2);
                        DEBUG_DESTROY_NODE(rshiftOp);
                    }
                } // END if (ival2 != INT_MAX)

            SKIP:
                /* Now check for compares with small constant longs that can be cast to int */

                if (!cns2->OperIsConst())
                {
                    goto COMPARE;
                }

                if (cns2->TypeGet() != TYP_LONG)
                {
                    goto COMPARE;
                }

                /* Is the constant 31 bits or smaller? */

                if ((cns2->gtIntConCommon.LngValue() >> 31) != 0)
                {
                    goto COMPARE;
                }

                /* Is the first comparand mask operation of type long ? */

                if (op1->gtOper != GT_AND)
                {
                    /* Another interesting case: cast from int */

                    if (op1->gtOper == GT_CAST && op1->CastFromType() == TYP_INT &&
                        !gtIsActiveCSE_Candidate(op1) && // op1 cannot be a CSE candidate
                        !op1->gtOverflow())              // cannot be an overflow checking cast
                    {
                        /* Simply make this into an integer comparison */

                        tree->gtOp.gtOp1 = op1->gtCast.CastOp();
                        tree->gtOp.gtOp2 = gtNewIconNode((int)cns2->gtIntConCommon.LngValue(), TYP_INT);
                    }

                    goto COMPARE;
                }

                noway_assert(op1->TypeGet() == TYP_LONG && op1->OperGet() == GT_AND);

                /* Is the result of the mask effectively an INT ? */

                GenTreePtr andMask;
                andMask = op1->gtOp.gtOp2;
                if (andMask->gtOper != GT_CNS_NATIVELONG)
                {
                    goto COMPARE;
                }
                if ((andMask->gtIntConCommon.LngValue() >> 32) != 0)
                {
                    goto COMPARE;
                }

                /* Now we know that we can cast gtOp.gtOp1 of AND to int */

                op1->gtOp.gtOp1 = gtNewCastNode(TYP_INT, op1->gtOp.gtOp1, TYP_INT);

                /* now replace the mask node (gtOp.gtOp2 of AND node) */

                noway_assert(andMask == op1->gtOp.gtOp2);

                ival1 = (int)andMask->gtIntConCommon.LngValue();
                andMask->SetOper(GT_CNS_INT);
                andMask->gtType             = TYP_INT;
                andMask->gtIntCon.gtIconVal = ival1;

                /* now change the type of the AND node */

                op1->gtType = TYP_INT;

                /* finally we replace the comparand */

                ival2 = (int)cns2->gtIntConCommon.LngValue();
                cns2->SetOper(GT_CNS_INT);
                cns2->gtType = TYP_INT;

                noway_assert(cns2 == op2);
                cns2->gtIntCon.gtIconVal = ival2;

                goto COMPARE;

            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:

                if ((tree->gtFlags & GTF_UNSIGNED) == 0)
                {
                    if (op2->gtOper == GT_CNS_INT)
                    {
                        cns2 = op2;
                        /* Check for "expr relop 1" */
                        if (cns2->IsIntegralConst(1))
                        {
                            /* Check for "expr >= 1" */
                            if (oper == GT_GE)
                            {
                                /* Change to "expr > 0" */
                                oper = GT_GT;
                                goto SET_OPER;
                            }
                            /* Check for "expr < 1" */
                            else if (oper == GT_LT)
                            {
                                /* Change to "expr <= 0" */
                                oper = GT_LE;
                                goto SET_OPER;
                            }
                        }
                        /* Check for "expr relop -1" */
                        else if (cns2->IsIntegralConst(-1) && ((oper == GT_LE) || (oper == GT_GT)))
                        {
                            /* Check for "expr <= -1" */
                            if (oper == GT_LE)
                            {
                                /* Change to "expr < 0" */
                                oper = GT_LT;
                                goto SET_OPER;
                            }
                            /* Check for "expr > -1" */
                            else if (oper == GT_GT)
                            {
                                /* Change to "expr >= 0" */
                                oper = GT_GE;

                            SET_OPER:
                                // IF we get here we should be changing 'oper'
                                assert(tree->OperGet() != oper);

                                // Keep the old ValueNumber for 'tree' as the new expr
                                // will still compute the same value as before
                                tree->SetOper(oper, GenTree::PRESERVE_VN);
                                cns2->gtIntCon.gtIconVal = 0;

                                // vnStore is null before the ValueNumber phase has run
                                if (vnStore != nullptr)
                                {
                                    // Update the ValueNumber for 'cns2', as we just changed it to 0
                                    fgValueNumberTreeConst(cns2);
                                }

                                op2 = tree->gtOp.gtOp2 = gtFoldExpr(op2);
                            }
                        }
                    }
                }

            COMPARE:

                noway_assert(tree->OperKind() & GTK_RELOP);

                /* Check if the result of the comparison is used for a jump.
                 * If not then only the int (i.e. 32 bit) case is handled in
                 * the code generator through the (x86) "set" instructions.
                 * For the rest of the cases, the simplest way is to
                 * "simulate" the comparison with ?:
                 *
                 * On ARM, we previously used the IT instruction, but the IT instructions
                 * have mostly been declared obsolete and off-limits, so all cases on ARM
                 * get converted to ?: */

                if (!(tree->gtFlags & GTF_RELOP_JMP_USED) && fgMorphRelopToQmark(op1))
                {
                    /* We convert it to "(CMP_TRUE) ? (1):(0)" */

                    op1 = tree;
                    op1->gtFlags |= (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);
                    op1->gtRequestSetFlags();

                    op2 = new (this, GT_COLON) GenTreeColon(TYP_INT, gtNewIconNode(1), gtNewIconNode(0));
                    op2 = fgMorphTree(op2);

                    tree = gtNewQmarkNode(TYP_INT, op1, op2);

                    fgMorphTreeDone(tree);

                    return tree;
                }
                break;

            case GT_QMARK:

                /* If op1 is a comma throw node then we won't be keeping op2 */
                if (fgIsCommaThrow(op1))
                {
                    break;
                }

                /* Get hold of the two branches */

                noway_assert(op2->OperGet() == GT_COLON);
                elseNode = op2->AsColon()->ElseNode();
                thenNode = op2->AsColon()->ThenNode();

                /* Try to hoist assignments out of qmark colon constructs.
                   ie. replace (cond?(x=a):(x=b)) with (x=(cond?a:b)). */

                if (tree->TypeGet() == TYP_VOID && thenNode->OperGet() == GT_ASG && elseNode->OperGet() == GT_ASG &&
                    thenNode->TypeGet() != TYP_LONG && GenTree::Compare(thenNode->gtOp.gtOp1, elseNode->gtOp.gtOp1) &&
                    thenNode->gtOp.gtOp2->TypeGet() == elseNode->gtOp.gtOp2->TypeGet())
                {
                    noway_assert(thenNode->TypeGet() == elseNode->TypeGet());

                    GenTreePtr asg    = thenNode;
                    GenTreePtr colon  = op2;
                    colon->gtOp.gtOp1 = thenNode->gtOp.gtOp2;
                    colon->gtOp.gtOp2 = elseNode->gtOp.gtOp2;
                    tree->gtType = colon->gtType = asg->gtOp.gtOp2->gtType;
                    asg->gtOp.gtOp2              = tree;

                    // Asg will have all the flags that the QMARK had
                    asg->gtFlags |= (tree->gtFlags & GTF_ALL_EFFECT);

                    // Colon flag won't have the flags that x had.
                    colon->gtFlags &= ~GTF_ALL_EFFECT;
                    colon->gtFlags |= (colon->gtOp.gtOp1->gtFlags | colon->gtOp.gtOp2->gtFlags) & GTF_ALL_EFFECT;

                    DEBUG_DESTROY_NODE(elseNode->gtOp.gtOp1);
                    DEBUG_DESTROY_NODE(elseNode);

                    return asg;
                }

                /* If the 'else' branch is empty swap the two branches and reverse the condition */

                if (elseNode->IsNothingNode())
                {
                    /* This can only happen for VOID ?: */
                    noway_assert(op2->gtType == TYP_VOID);

                    /* If the thenNode and elseNode are both nop nodes then optimize away the QMARK */
                    if (thenNode->IsNothingNode())
                    {
                        // We may be able to throw away op1 (unless it has side-effects)

                        if ((op1->gtFlags & GTF_SIDE_EFFECT) == 0)
                        {
                            /* Just return a a Nop Node */
                            return thenNode;
                        }
                        else
                        {
                            /* Just return the relop, but clear the special flags.  Note
                               that we can't do that for longs and floats (see code under
                               COMPARE label above) */

                            if (!fgMorphRelopToQmark(op1->gtOp.gtOp1))
                            {
                                op1->gtFlags &= ~(GTF_RELOP_QMARK | GTF_RELOP_JMP_USED);
                                return op1;
                            }
                        }
                    }
                    else
                    {
                        GenTreePtr tmp = elseNode;

                        op2->AsColon()->ElseNode() = elseNode = thenNode;
                        op2->AsColon()->ThenNode() = thenNode = tmp;
                        gtReverseCond(op1);
                    }
                }

#if !defined(_TARGET_ARM_)
                // If we have (cond)?0:1, then we just return "cond" for TYP_INTs
                //
                // Don't do this optimization for ARM: we always require assignment
                // to boolean to remain ?:, since we don't have any way to generate
                // this with straight-line code, like x86 does using setcc (at least
                // after the IT instruction is deprecated).

                if (genActualType(op1->gtOp.gtOp1->gtType) == TYP_INT && genActualType(typ) == TYP_INT &&
                    thenNode->gtOper == GT_CNS_INT && elseNode->gtOper == GT_CNS_INT)
                {
                    ival1 = thenNode->gtIntCon.gtIconVal;
                    ival2 = elseNode->gtIntCon.gtIconVal;

                    // Is one constant 0 and the other 1?
                    if ((ival1 | ival2) == 1 && (ival1 & ival2) == 0)
                    {
                        // If the constants are {1, 0}, reverse the condition
                        if (ival1 == 1)
                        {
                            gtReverseCond(op1);
                        }

                        // Unmark GTF_RELOP_JMP_USED on the condition node so it knows that it
                        // needs to materialize the result as a 0 or 1.
                        noway_assert(op1->gtFlags & (GTF_RELOP_QMARK | GTF_RELOP_JMP_USED));
                        op1->gtFlags &= ~(GTF_RELOP_QMARK | GTF_RELOP_JMP_USED);

                        DEBUG_DESTROY_NODE(tree);
                        DEBUG_DESTROY_NODE(op2);

                        return op1;
                    }
                }
#endif // !_TARGET_ARM_

                break; // end case GT_QMARK

            case GT_MUL:

#ifndef _TARGET_64BIT_
                if (typ == TYP_LONG)
                {
                    // This must be GTF_MUL_64RSLT
                    assert(tree->gtIsValid64RsltMul());
                    return tree;
                }
#endif // _TARGET_64BIT_
                goto CM_OVF_OP;

            case GT_SUB:

                if (tree->gtOverflow())
                {
                    goto CM_OVF_OP;
                }

                /* Check for "op1 - cns2" , we change it to "op1 + (-cns2)" */

                noway_assert(op2);
                if (op2->IsCnsIntOrI())
                {
                    /* Negate the constant and change the node to be "+" */

                    op2->gtIntConCommon.SetIconValue(-op2->gtIntConCommon.IconValue());
                    oper = GT_ADD;
                    tree->ChangeOper(oper);
                    goto CM_ADD_OP;
                }

                /* Check for "cns1 - op2" , we change it to "(cns1 + (-op2))" */

                noway_assert(op1);
                if (op1->IsCnsIntOrI())
                {
                    noway_assert(varTypeIsIntOrI(tree));

                    tree->gtOp.gtOp2 = op2 =
                        gtNewOperNode(GT_NEG, tree->gtType, op2); // The type of the new GT_NEG node should be the same
                                                                  // as the type of the tree, i.e. tree->gtType.
                    fgMorphTreeDone(op2);

                    oper = GT_ADD;
                    tree->ChangeOper(oper);
                    goto CM_ADD_OP;
                }

                /* No match - exit */

                break;

#ifdef _TARGET_ARM64_
            case GT_DIV:
                if (!varTypeIsFloating(tree->gtType))
                {
                    // Codegen for this instruction needs to be able to throw two exceptions:
                    fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW, fgPtrArgCntCur);
                    fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_DIV_BY_ZERO, fgPtrArgCntCur);
                }
                break;
            case GT_UDIV:
                // Codegen for this instruction needs to be able to throw one exception:
                fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_DIV_BY_ZERO, fgPtrArgCntCur);
                break;
#endif

            case GT_ADD:

            CM_OVF_OP:
                if (tree->gtOverflow())
                {
                    tree->gtRequestSetFlags();

                    // Add the excptn-throwing basic block to jump to on overflow

                    fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW, fgPtrArgCntCur);

                    // We can't do any commutative morphing for overflow instructions

                    break;
                }

            CM_ADD_OP:

            case GT_OR:
            case GT_XOR:
            case GT_AND:

                /* Commute any non-REF constants to the right */

                noway_assert(op1);
                if (op1->OperIsConst() && (op1->gtType != TYP_REF))
                {
                    // TODO-Review: We used to assert here that
                    // noway_assert(!op2->OperIsConst() || !opts.OptEnabled(CLFLG_CONSTANTFOLD));
                    // With modifications to AddrTaken==>AddrExposed, we did more assertion propagation,
                    // and would sometimes hit this assertion.  This may indicate a missed "remorph".
                    // Task is to re-enable this assertion and investigate.

                    /* Swap the operands */
                    tree->gtOp.gtOp1 = op2;
                    tree->gtOp.gtOp2 = op1;

                    op1 = op2;
                    op2 = tree->gtOp.gtOp2;
                }

                /* See if we can fold GT_ADD nodes. */

                if (oper == GT_ADD)
                {
                    /* Fold "((x+icon1)+(y+icon2)) to ((x+y)+(icon1+icon2))" */

                    if (op1->gtOper == GT_ADD && op2->gtOper == GT_ADD && !gtIsActiveCSE_Candidate(op2) &&
                        op1->gtOp.gtOp2->gtOper == GT_CNS_INT && op2->gtOp.gtOp2->gtOper == GT_CNS_INT &&
                        !op1->gtOverflow() && !op2->gtOverflow())
                    {
                        cns1 = op1->gtOp.gtOp2;
                        cns2 = op2->gtOp.gtOp2;
                        cns1->gtIntCon.gtIconVal += cns2->gtIntCon.gtIconVal;
#ifdef _TARGET_64BIT_
                        if (cns1->TypeGet() == TYP_INT)
                        {
                            // we need to properly re-sign-extend or truncate after adding two int constants above
                            cns1->AsIntCon()->TruncateOrSignExtend32();
                        }
#endif //_TARGET_64BIT_

                        tree->gtOp.gtOp2 = cns1;
                        DEBUG_DESTROY_NODE(cns2);

                        op1->gtOp.gtOp2 = op2->gtOp.gtOp1;
                        op1->gtFlags |= (op1->gtOp.gtOp2->gtFlags & GTF_ALL_EFFECT);
                        DEBUG_DESTROY_NODE(op2);
                        op2 = tree->gtOp.gtOp2;
                    }

                    if (op2->IsCnsIntOrI() && varTypeIsIntegralOrI(typ))
                    {
                        /* Fold "((x+icon1)+icon2) to (x+(icon1+icon2))" */

                        if (op1->gtOper == GT_ADD && !gtIsActiveCSE_Candidate(op1) && op1->gtOp.gtOp2->IsCnsIntOrI() &&
                            !op1->gtOverflow() && op1->gtOp.gtOp2->OperGet() == op2->OperGet())
                        {
                            cns1 = op1->gtOp.gtOp2;
                            op2->gtIntConCommon.SetIconValue(cns1->gtIntConCommon.IconValue() +
                                                             op2->gtIntConCommon.IconValue());
#ifdef _TARGET_64BIT_
                            if (op2->TypeGet() == TYP_INT)
                            {
                                // we need to properly re-sign-extend or truncate after adding two int constants above
                                op2->AsIntCon()->TruncateOrSignExtend32();
                            }
#endif //_TARGET_64BIT_

                            if (cns1->OperGet() == GT_CNS_INT)
                            {
                                op2->gtIntCon.gtFieldSeq =
                                    GetFieldSeqStore()->Append(cns1->gtIntCon.gtFieldSeq, op2->gtIntCon.gtFieldSeq);
                            }
                            DEBUG_DESTROY_NODE(cns1);

                            tree->gtOp.gtOp1 = op1->gtOp.gtOp1;
                            DEBUG_DESTROY_NODE(op1);
                            op1 = tree->gtOp.gtOp1;
                        }

                        // Fold (x + 0).

                        if ((op2->gtIntConCommon.IconValue() == 0) && !gtIsActiveCSE_Candidate(tree))
                        {

                            // If this addition is adding an offset to a null pointer,
                            // avoid the work and yield the null pointer immediately.
                            // Dereferencing the pointer in either case will have the
                            // same effect.

                            if (!gtIsActiveCSE_Candidate(op1) && varTypeIsGC(op2->TypeGet()))
                            {
                                op2->gtType = tree->gtType;
                                DEBUG_DESTROY_NODE(op1);
                                DEBUG_DESTROY_NODE(tree);
                                return op2;
                            }

                            // Remove the addition iff it won't change the tree type
                            // to TYP_REF.

                            if (!gtIsActiveCSE_Candidate(op2) &&
                                ((op1->TypeGet() == tree->TypeGet()) || (op1->TypeGet() != TYP_REF)))
                            {
                                if (fgGlobalMorph && (op2->OperGet() == GT_CNS_INT) &&
                                    (op2->gtIntCon.gtFieldSeq != nullptr) &&
                                    (op2->gtIntCon.gtFieldSeq != FieldSeqStore::NotAField()))
                                {
                                    fgAddFieldSeqForZeroOffset(op1, op2->gtIntCon.gtFieldSeq);
                                }

                                DEBUG_DESTROY_NODE(op2);
                                DEBUG_DESTROY_NODE(tree);

                                return op1;
                            }
                        }
                    }
                }
                /* See if we can fold GT_MUL by const nodes */
                else if (oper == GT_MUL && op2->IsCnsIntOrI() && !optValnumCSE_phase)
                {
#ifndef _TARGET_64BIT_
                    noway_assert(typ <= TYP_UINT);
#endif // _TARGET_64BIT_
                    noway_assert(!tree->gtOverflow());

                    ssize_t mult            = op2->gtIntConCommon.IconValue();
                    bool    op2IsConstIndex = op2->OperGet() == GT_CNS_INT && op2->gtIntCon.gtFieldSeq != nullptr &&
                                           op2->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq();

                    assert(!op2IsConstIndex || op2->AsIntCon()->gtFieldSeq->m_next == nullptr);

                    if (mult == 0)
                    {
                        // We may be able to throw away op1 (unless it has side-effects)

                        if ((op1->gtFlags & GTF_SIDE_EFFECT) == 0)
                        {
                            DEBUG_DESTROY_NODE(op1);
                            DEBUG_DESTROY_NODE(tree);
                            return op2; // Just return the "0" node
                        }

                        // We need to keep op1 for the side-effects. Hang it off
                        // a GT_COMMA node

                        tree->ChangeOper(GT_COMMA);
                        return tree;
                    }

                    size_t abs_mult      = (mult >= 0) ? mult : -mult;
                    size_t lowestBit     = genFindLowestBit(abs_mult);
                    bool   changeToShift = false;

                    // is it a power of two? (positive or negative)
                    if (abs_mult == lowestBit)
                    {
                        // if negative negate (min-int does not need negation)
                        if (mult < 0 && mult != SSIZE_T_MIN)
                        {
                            tree->gtOp.gtOp1 = op1 = gtNewOperNode(GT_NEG, op1->gtType, op1);
                            fgMorphTreeDone(op1);
                        }

                        // If "op2" is a constant array index, the other multiplicand must be a constant.
                        // Transfer the annotation to the other one.
                        if (op2->OperGet() == GT_CNS_INT && op2->gtIntCon.gtFieldSeq != nullptr &&
                            op2->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq())
                        {
                            assert(op2->gtIntCon.gtFieldSeq->m_next == nullptr);
                            GenTreePtr otherOp = op1;
                            if (otherOp->OperGet() == GT_NEG)
                            {
                                otherOp = otherOp->gtOp.gtOp1;
                            }
                            assert(otherOp->OperGet() == GT_CNS_INT);
                            assert(otherOp->gtIntCon.gtFieldSeq == FieldSeqStore::NotAField());
                            otherOp->gtIntCon.gtFieldSeq = op2->gtIntCon.gtFieldSeq;
                        }

                        if (abs_mult == 1)
                        {
                            DEBUG_DESTROY_NODE(op2);
                            DEBUG_DESTROY_NODE(tree);
                            return op1;
                        }

                        /* Change the multiplication into a shift by log2(val) bits */
                        op2->gtIntConCommon.SetIconValue(genLog2(abs_mult));
                        changeToShift = true;
                    }
#if LEA_AVAILABLE
                    else if ((lowestBit > 1) && jitIsScaleIndexMul(lowestBit) && optAvoidIntMult())
                    {
                        int     shift  = genLog2(lowestBit);
                        ssize_t factor = abs_mult >> shift;

                        if (factor == 3 || factor == 5 || factor == 9)
                        {
                            // if negative negate (min-int does not need negation)
                            if (mult < 0 && mult != SSIZE_T_MIN)
                            {
                                tree->gtOp.gtOp1 = op1 = gtNewOperNode(GT_NEG, op1->gtType, op1);
                                fgMorphTreeDone(op1);
                            }

                            GenTreePtr factorIcon = gtNewIconNode(factor, TYP_I_IMPL);
                            if (op2IsConstIndex)
                            {
                                factorIcon->AsIntCon()->gtFieldSeq =
                                    GetFieldSeqStore()->CreateSingleton(FieldSeqStore::ConstantIndexPseudoField);
                            }

                            // change the multiplication into a smaller multiplication (by 3, 5 or 9) and a shift
                            tree->gtOp.gtOp1 = op1 = gtNewOperNode(GT_MUL, tree->gtType, op1, factorIcon);
                            fgMorphTreeDone(op1);

                            op2->gtIntConCommon.SetIconValue(shift);
                            changeToShift = true;
                        }
                    }
#endif // LEA_AVAILABLE
                    if (changeToShift)
                    {
                        // vnStore is null before the ValueNumber phase has run
                        if (vnStore != nullptr)
                        {
                            // Update the ValueNumber for 'op2', as we just changed the constant
                            fgValueNumberTreeConst(op2);
                        }
                        oper = GT_LSH;
                        // Keep the old ValueNumber for 'tree' as the new expr
                        // will still compute the same value as before
                        tree->ChangeOper(oper, GenTree::PRESERVE_VN);

                        goto DONE_MORPHING_CHILDREN;
                    }
                }
                else if (fgOperIsBitwiseRotationRoot(oper))
                {
                    tree = fgRecognizeAndMorphBitwiseRotation(tree);

                    // fgRecognizeAndMorphBitwiseRotation may return a new tree
                    oper = tree->OperGet();
                    typ  = tree->TypeGet();
                    op1  = tree->gtOp.gtOp1;
                    op2  = tree->gtOp.gtOp2;
                }

                break;

            case GT_CHS:
            case GT_NOT:
            case GT_NEG:

                /* Any constant cases should have been folded earlier */
                noway_assert(!op1->OperIsConst() || !opts.OptEnabled(CLFLG_CONSTANTFOLD) || optValnumCSE_phase);
                break;

            case GT_CKFINITE:

                noway_assert(varTypeIsFloating(op1->TypeGet()));

                fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_ARITH_EXCPN, fgPtrArgCntCur);
                break;

            case GT_OBJ:
                // If we have GT_OBJ(GT_ADDR(X)) and X has GTF_GLOB_REF, we must set GTF_GLOB_REF on
                // the GT_OBJ. Note that the GTF_GLOB_REF will have been cleared on ADDR(X) where X
                // is a local or clsVar, even if it has been address-exposed.
                if (op1->OperGet() == GT_ADDR)
                {
                    tree->gtFlags |= (op1->gtGetOp1()->gtFlags & GTF_GLOB_REF);
                }
                break;

            case GT_IND:

                // Can not remove a GT_IND if it is currently a CSE candidate.
                if (gtIsActiveCSE_Candidate(tree))
                {
                    break;
                }

                bool foldAndReturnTemp;
                foldAndReturnTemp = false;
                temp              = nullptr;
                ival1             = 0;

                /* Try to Fold *(&X) into X */
                if (op1->gtOper == GT_ADDR)
                {
                    // Can not remove a GT_ADDR if it is currently a CSE candidate.
                    if (gtIsActiveCSE_Candidate(op1))
                    {
                        break;
                    }

                    temp = op1->gtOp.gtOp1; // X

                    // In the test below, if they're both TYP_STRUCT, this of course does *not* mean that
                    // they are the *same* struct type.  In fact, they almost certainly aren't.  If the
                    // address has an associated field sequence, that identifies this case; go through
                    // the "lcl_fld" path rather than this one.
                    FieldSeqNode* addrFieldSeq = nullptr; // This is an unused out parameter below.
                    if (typ == temp->TypeGet() && !GetZeroOffsetFieldMap()->Lookup(op1, &addrFieldSeq))
                    {
                        foldAndReturnTemp = true;
                    }
                    else if (temp->OperIsLocal())
                    {
                        unsigned   lclNum = temp->gtLclVarCommon.gtLclNum;
                        LclVarDsc* varDsc = &lvaTable[lclNum];

                        // We will try to optimize when we have a promoted struct promoted with a zero lvFldOffset
                        if (varDsc->lvPromoted && (varDsc->lvFldOffset == 0))
                        {
                            noway_assert(varTypeIsStruct(varDsc));

                            // We will try to optimize when we have a single field struct that is being struct promoted
                            if (varDsc->lvFieldCnt == 1)
                            {
                                unsigned lclNumFld = varDsc->lvFieldLclStart;
                                // just grab the promoted field
                                LclVarDsc* fieldVarDsc = &lvaTable[lclNumFld];

                                // Also make sure that the tree type matches the fieldVarType and that it's lvFldOffset
                                // is zero
                                if (fieldVarDsc->TypeGet() == tree->TypeGet() && (fieldVarDsc->lvFldOffset == 0))
                                {
                                    // We can just use the existing promoted field LclNum
                                    temp->gtLclVarCommon.SetLclNum(lclNumFld);
                                    temp->gtType = fieldVarDsc->TypeGet();

                                    foldAndReturnTemp = true;
                                }
                            }
                        }
                        // If the type of the IND (typ) is a "small int", and the type of the local has the
                        // same width, then we can reduce to just the local variable -- it will be
                        // correctly normalized, and signed/unsigned differences won't matter.
                        //
                        // The below transformation cannot be applied if the local var needs to be normalized on load.
                        else if (varTypeIsSmall(typ) && (genTypeSize(lvaTable[lclNum].lvType) == genTypeSize(typ)) &&
                                 !lvaTable[lclNum].lvNormalizeOnLoad())
                        {
                            tree->gtType      = temp->gtType;
                            foldAndReturnTemp = true;
                        }
                        else
                        {
                            // Assumes that when Lookup returns "false" it will leave "fieldSeq" unmodified (i.e.
                            // nullptr)
                            assert(fieldSeq == nullptr);
                            bool b = GetZeroOffsetFieldMap()->Lookup(op1, &fieldSeq);
                            assert(b || fieldSeq == nullptr);

                            if ((fieldSeq != nullptr) && (temp->OperGet() == GT_LCL_FLD))
                            {
                                // Append the field sequence, change the type.
                                temp->AsLclFld()->gtFieldSeq =
                                    GetFieldSeqStore()->Append(temp->AsLclFld()->gtFieldSeq, fieldSeq);
                                temp->gtType = tree->TypeGet();

                                foldAndReturnTemp = true;
                            }
                        }
                        // Otherwise will will fold this into a GT_LCL_FLD below
                        //   where we check (temp != nullptr)
                    }
                    else // !temp->OperIsLocal()
                    {
                        // We don't try to fold away the GT_IND/GT_ADDR for this case
                        temp = nullptr;
                    }
                }
                else if (op1->OperGet() == GT_ADD)
                {
                    /* Try to change *(&lcl + cns) into lcl[cns] to prevent materialization of &lcl */

                    if (op1->gtOp.gtOp1->OperGet() == GT_ADDR && op1->gtOp.gtOp2->OperGet() == GT_CNS_INT &&
                        (!(opts.MinOpts() || opts.compDbgCode)))
                    {
                        // No overflow arithmetic with pointers
                        noway_assert(!op1->gtOverflow());

                        temp = op1->gtOp.gtOp1->gtOp.gtOp1;
                        if (!temp->OperIsLocal())
                        {
                            temp = nullptr;
                            break;
                        }

                        // Can not remove the GT_ADDR if it is currently a CSE candidate.
                        if (gtIsActiveCSE_Candidate(op1->gtOp.gtOp1))
                        {
                            break;
                        }

                        ival1    = op1->gtOp.gtOp2->gtIntCon.gtIconVal;
                        fieldSeq = op1->gtOp.gtOp2->gtIntCon.gtFieldSeq;

                        // Does the address have an associated zero-offset field sequence?
                        FieldSeqNode* addrFieldSeq = nullptr;
                        if (GetZeroOffsetFieldMap()->Lookup(op1->gtOp.gtOp1, &addrFieldSeq))
                        {
                            fieldSeq = GetFieldSeqStore()->Append(addrFieldSeq, fieldSeq);
                        }

                        if (ival1 == 0 && typ == temp->TypeGet() && temp->TypeGet() != TYP_STRUCT)
                        {
                            noway_assert(!varTypeIsGC(temp->TypeGet()));
                            foldAndReturnTemp = true;
                        }
                        else
                        {
                            // The emitter can't handle large offsets
                            if (ival1 != (unsigned short)ival1)
                            {
                                break;
                            }

                            // The emitter can get confused by invalid offsets
                            if (ival1 >= Compiler::lvaLclSize(temp->gtLclVarCommon.gtLclNum))
                            {
                                break;
                            }

#ifdef _TARGET_ARM_
                            // Check for a LclVar TYP_STRUCT with misalignment on a Floating Point field
                            //
                            if (varTypeIsFloating(tree->TypeGet()))
                            {
                                if ((ival1 % emitTypeSize(tree->TypeGet())) != 0)
                                {
                                    tree->gtFlags |= GTF_IND_UNALIGNED;
                                    break;
                                }
                            }
#endif
                        }
                        // Now we can fold this into a GT_LCL_FLD below
                        //   where we check (temp != nullptr)
                    }
                }

#ifdef DEBUG
                // If we have decided to fold, then temp cannot be nullptr
                if (foldAndReturnTemp)
                {
                    assert(temp != nullptr);
                }
#endif

                if (temp != nullptr)
                {
                    noway_assert(op1->gtOper == GT_ADD || op1->gtOper == GT_ADDR);

                    // If we haven't already decided to fold this expression
                    //
                    if (!foldAndReturnTemp)
                    {
                        noway_assert(temp->OperIsLocal());
                        LclVarDsc* varDsc = &(lvaTable[temp->AsLclVarCommon()->gtLclNum]);
                        // Make sure we don't separately promote the fields of this struct.
                        if (varDsc->lvRegStruct)
                        {
                            // We can enregister, but can't promote.
                            varDsc->lvPromoted = false;
                        }
                        else
                        {
                            lvaSetVarDoNotEnregister(temp->gtLclVarCommon.gtLclNum DEBUGARG(DNER_LocalField));
                        }

                        // We will turn a GT_LCL_VAR into a GT_LCL_FLD with an gtLclOffs of 'ival'
                        // or if we already have a GT_LCL_FLD we will adjust the gtLclOffs by adding 'ival'
                        // Then we change the type of the GT_LCL_FLD to match the orginal GT_IND type.
                        //
                        if (temp->OperGet() == GT_LCL_FLD)
                        {
                            temp->AsLclFld()->gtLclOffs += (unsigned short)ival1;
                            temp->AsLclFld()->gtFieldSeq =
                                GetFieldSeqStore()->Append(temp->AsLclFld()->gtFieldSeq, fieldSeq);
                        }
                        else
                        {
                            temp->ChangeOper(GT_LCL_FLD); // Note that this makes the gtFieldSeq "NotAField"...
                            temp->AsLclFld()->gtLclOffs = (unsigned short)ival1;
                            if (fieldSeq != nullptr)
                            { // If it does represent a field, note that.
                                temp->AsLclFld()->gtFieldSeq = fieldSeq;
                            }
                        }
                        temp->gtType      = tree->gtType;
                        foldAndReturnTemp = true;
                    }

                    assert(foldAndReturnTemp == true);

                    // Keep the DONT_CSE flag in sync
                    // (i.e keep the original value of this flag from tree)
                    // as it can be set for 'temp' because a GT_ADDR always marks it for it's op1
                    //
                    temp->gtFlags &= ~GTF_DONT_CSE;
                    temp->gtFlags |= (tree->gtFlags & GTF_DONT_CSE);

                    noway_assert(op1->gtOper == GT_ADD || op1->gtOper == GT_ADDR);
                    noway_assert(temp->gtType == tree->gtType);

                    if (op1->OperGet() == GT_ADD)
                    {
                        DEBUG_DESTROY_NODE(op1->gtOp.gtOp1); // GT_ADDR
                        DEBUG_DESTROY_NODE(op1->gtOp.gtOp2); // GT_CNS_INT
                    }
                    DEBUG_DESTROY_NODE(op1);  // GT_ADD or GT_ADDR
                    DEBUG_DESTROY_NODE(tree); // GT_IND

                    return temp;
                }

                // Only do this optimization when we are in the global optimizer. Doing this after value numbering
                // could result in an invalid value number for the newly generated GT_IND node.
                if ((op1->OperGet() == GT_COMMA) && fgGlobalMorph)
                {
                    // Perform the transform IND(COMMA(x, ..., z)) == COMMA(x, ..., IND(z)).
                    // TBD: this transformation is currently necessary for correctness -- it might
                    // be good to analyze the failures that result if we don't do this, and fix them
                    // in other ways.  Ideally, this should be optional.
                    GenTreePtr commaNode = op1;
                    unsigned   treeFlags = tree->gtFlags;
                    commaNode->gtType    = typ;
                    commaNode->gtFlags   = (treeFlags & ~GTF_REVERSE_OPS); // Bashing the GT_COMMA flags here is
                                                                           // dangerous, clear the GTF_REVERSE_OPS at
                                                                           // least.
#ifdef DEBUG
                    commaNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                    while (commaNode->gtOp.gtOp2->gtOper == GT_COMMA)
                    {
                        commaNode          = commaNode->gtOp.gtOp2;
                        commaNode->gtType  = typ;
                        commaNode->gtFlags = (treeFlags & ~GTF_REVERSE_OPS); // Bashing the GT_COMMA flags here is
                                                                             // dangerous, clear the GTF_REVERSE_OPS at
                                                                             // least.
#ifdef DEBUG
                        commaNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                    }
                    bool      wasArrIndex = (tree->gtFlags & GTF_IND_ARR_INDEX) != 0;
                    ArrayInfo arrInfo;
                    if (wasArrIndex)
                    {
                        bool b = GetArrayInfoMap()->Lookup(tree, &arrInfo);
                        assert(b);
                        GetArrayInfoMap()->Remove(tree);
                    }
                    tree         = op1;
                    op1          = gtNewOperNode(GT_IND, typ, commaNode->gtOp.gtOp2);
                    op1->gtFlags = treeFlags;
                    if (wasArrIndex)
                    {
                        GetArrayInfoMap()->Set(op1, arrInfo);
                    }
#ifdef DEBUG
                    op1->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                    commaNode->gtOp.gtOp2 = op1;
                    return tree;
                }

                break;

            case GT_ADDR:

                // Can not remove op1 if it is currently a CSE candidate.
                if (gtIsActiveCSE_Candidate(op1))
                {
                    break;
                }

                if (op1->OperGet() == GT_IND)
                {
                    if ((op1->gtFlags & GTF_IND_ARR_INDEX) == 0)
                    {
                        // Can not remove a GT_ADDR if it is currently a CSE candidate.
                        if (gtIsActiveCSE_Candidate(tree))
                        {
                            break;
                        }

                        // Perform the transform ADDR(IND(...)) == (...).
                        GenTreePtr addr = op1->gtOp.gtOp1;

                        noway_assert(varTypeIsGC(addr->gtType) || addr->gtType == TYP_I_IMPL);

                        DEBUG_DESTROY_NODE(op1);
                        DEBUG_DESTROY_NODE(tree);

                        return addr;
                    }
                }
                else if (op1->gtOper == GT_CAST)
                {
                    GenTreePtr casting = op1->gtCast.CastOp();
                    if (casting->gtOper == GT_LCL_VAR || casting->gtOper == GT_CLS_VAR)
                    {
                        DEBUG_DESTROY_NODE(op1);
                        tree->gtOp.gtOp1 = op1 = casting;
                    }
                }
                else if ((op1->gtOper == GT_COMMA) && !optValnumCSE_phase)
                {
                    // Perform the transform ADDR(COMMA(x, ..., z)) == COMMA(x, ..., ADDR(z)).
                    // (Be sure to mark "z" as an l-value...)
                    GenTreePtr commaNode = op1;
                    while (commaNode->gtOp.gtOp2->gtOper == GT_COMMA)
                    {
                        commaNode = commaNode->gtOp.gtOp2;
                    }
                    // The top-level addr might be annotated with a zeroOffset field.
                    FieldSeqNode* zeroFieldSeq = nullptr;
                    bool          isZeroOffset = GetZeroOffsetFieldMap()->Lookup(tree, &zeroFieldSeq);
                    tree                       = op1;
                    commaNode->gtOp.gtOp2->gtFlags |= GTF_DONT_CSE;

                    // If the node we're about to put under a GT_ADDR is a GT_IND, the indirection
                    // doesn't need to be materialized, since we only want the addressing mode. Because
                    // of this, this GT_IND is not a faulting indirection and we don't have to extract it
                    // as a side effect.
                    GenTree* commaOp2 = commaNode->gtOp.gtOp2;
                    if (commaOp2->gtOper == GT_IND)
                    {
                        commaOp2->gtFlags |= GTF_IND_NONFAULTING;
                    }

                    op1 = gtNewOperNode(GT_ADDR, TYP_BYREF, commaOp2);

                    if (isZeroOffset)
                    {
                        // Transfer the annotation to the new GT_ADDR node.
                        GetZeroOffsetFieldMap()->Set(op1, zeroFieldSeq);
                    }
                    commaNode->gtOp.gtOp2 = op1;
                    // Originally, I gave all the comma nodes type "byref".  But the ADDR(IND(x)) == x transform
                    // might give op1 a type different from byref (like, say, native int).  So now go back and give
                    // all the comma nodes the type of op1.
                    // TODO: the comma flag update below is conservative and can be improved.
                    // For example, if we made the ADDR(IND(x)) == x transformation, we may be able to
                    // get rid of some of the the IND flags on the COMMA nodes (e.g., GTF_GLOB_REF).
                    commaNode = tree;
                    while (commaNode->gtOper == GT_COMMA)
                    {
                        commaNode->gtType = op1->gtType;
                        commaNode->gtFlags |= op1->gtFlags;
#ifdef DEBUG
                        commaNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                        commaNode = commaNode->gtOp.gtOp2;
                    }

                    return tree;
                }

                /* op1 of a GT_ADDR is an l-value. Only r-values can be CSEed */
                op1->gtFlags |= GTF_DONT_CSE;
                break;

            case GT_COLON:
                if (fgGlobalMorph)
                {
                    /* Mark the nodes that are conditionally executed */
                    fgWalkTreePre(&tree, gtMarkColonCond);
                }
                /* Since we're doing this postorder we clear this if it got set by a child */
                fgRemoveRestOfBlock = false;
                break;

            case GT_COMMA:

                /* Special case: trees that don't produce a value */
                if ((op2->OperKind() & GTK_ASGOP) || (op2->OperGet() == GT_COMMA && op2->TypeGet() == TYP_VOID) ||
                    fgIsThrow(op2))
                {
                    typ = tree->gtType = TYP_VOID;
                }

                // If we are in the Valuenum CSE phase then don't morph away anything as these
                // nodes may have CSE defs/uses in them.
                //
                if (!optValnumCSE_phase)
                {
                    // Extract the side effects from the left side of the comma.  Since they don't "go" anywhere, this
                    // is all we need.

                    GenTreePtr op1SideEffects = nullptr;
                    // The addition of "GTF_MAKE_CSE" below prevents us from throwing away (for example)
                    // hoisted expressions in loops.
                    gtExtractSideEffList(op1, &op1SideEffects, (GTF_SIDE_EFFECT | GTF_MAKE_CSE));
                    if (op1SideEffects)
                    {
                        // Replace the left hand side with the side effect list.
                        tree->gtOp.gtOp1 = op1SideEffects;
                        tree->gtFlags |= (op1SideEffects->gtFlags & GTF_ALL_EFFECT);
                    }
                    else
                    {
                        /* The left operand is worthless, throw it away */
                        if (lvaLocalVarRefCounted)
                        {
                            lvaRecursiveDecRefCounts(op1);
                        }
                        op2->gtFlags |= (tree->gtFlags & (GTF_DONT_CSE | GTF_LATE_ARG));
                        DEBUG_DESTROY_NODE(tree);
                        DEBUG_DESTROY_NODE(op1);
                        return op2;
                    }

                    /* If the right operand is just a void nop node, throw it away */
                    if (op2->IsNothingNode() && op1->gtType == TYP_VOID)
                    {
                        op1->gtFlags |= (tree->gtFlags & (GTF_DONT_CSE | GTF_LATE_ARG));
                        DEBUG_DESTROY_NODE(tree);
                        DEBUG_DESTROY_NODE(op2);
                        return op1;
                    }
                }

                break;

            case GT_JTRUE:

                /* Special case if fgRemoveRestOfBlock is set to true */
                if (fgRemoveRestOfBlock)
                {
                    if (fgIsCommaThrow(op1, true))
                    {
                        GenTreePtr throwNode = op1->gtOp.gtOp1;
                        noway_assert(throwNode->gtType == TYP_VOID);

                        return throwNode;
                    }

                    noway_assert(op1->OperKind() & GTK_RELOP);
                    noway_assert(op1->gtFlags & GTF_EXCEPT);

                    // We need to keep op1 for the side-effects. Hang it off
                    // a GT_COMMA node

                    tree->ChangeOper(GT_COMMA);
                    tree->gtOp.gtOp2 = op2 = gtNewNothingNode();

                    // Additionally since we're eliminating the JTRUE
                    // codegen won't like it if op1 is a RELOP of longs, floats or doubles.
                    // So we change it into a GT_COMMA as well.
                    op1->ChangeOper(GT_COMMA);
                    op1->gtType = op1->gtOp.gtOp1->gtType;

                    return tree;
                }

            default:
                break;
        }

        noway_assert(oper == tree->gtOper);

        // If we are in the Valuenum CSE phase then don't morph away anything as these
        // nodes may have CSE defs/uses in them.
        //
        if (!optValnumCSE_phase && (oper != GT_ASG) && (oper != GT_COLON) && !tree->IsList())
        {
            /* Check for op1 as a GT_COMMA with a unconditional throw node */
            if (op1 && fgIsCommaThrow(op1, true))
            {
                if ((op1->gtFlags & GTF_COLON_COND) == 0)
                {
                    /* We can safely throw out the rest of the statements */
                    fgRemoveRestOfBlock = true;
                }

                GenTreePtr throwNode = op1->gtOp.gtOp1;
                noway_assert(throwNode->gtType == TYP_VOID);

                if (oper == GT_COMMA)
                {
                    /* Both tree and op1 are GT_COMMA nodes */
                    /* Change the tree's op1 to the throw node: op1->gtOp.gtOp1 */
                    tree->gtOp.gtOp1 = throwNode;
                    return tree;
                }
                else if (oper != GT_NOP)
                {
                    if (genActualType(typ) == genActualType(op1->gtType))
                    {
                        /* The types match so, return the comma throw node as the new tree */
                        return op1;
                    }
                    else
                    {
                        if (typ == TYP_VOID)
                        {
                            // Return the throw node
                            return throwNode;
                        }
                        else
                        {
                            GenTreePtr commaOp2 = op1->gtOp.gtOp2;

                            // need type of oper to be same as tree
                            if (typ == TYP_LONG)
                            {
                                commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                                commaOp2->gtIntConCommon.SetLngValue(0);
                                /* Change the types of oper and commaOp2 to TYP_LONG */
                                op1->gtType = commaOp2->gtType = TYP_LONG;
                            }
                            else if (varTypeIsFloating(typ))
                            {
                                commaOp2->ChangeOperConst(GT_CNS_DBL);
                                commaOp2->gtDblCon.gtDconVal = 0.0;
                                /* Change the types of oper and commaOp2 to TYP_DOUBLE */
                                op1->gtType = commaOp2->gtType = TYP_DOUBLE;
                            }
                            else
                            {
                                commaOp2->ChangeOperConst(GT_CNS_INT);
                                commaOp2->gtIntConCommon.SetIconValue(0);
                                /* Change the types of oper and commaOp2 to TYP_INT */
                                op1->gtType = commaOp2->gtType = TYP_INT;
                            }

                            /* Return the GT_COMMA node as the new tree */
                            return op1;
                        }
                    }
                }
            }

            /* Check for op2 as a GT_COMMA with a unconditional throw */

            if (op2 && fgIsCommaThrow(op2, true))
            {
                if ((op2->gtFlags & GTF_COLON_COND) == 0)
                {
                    /* We can safely throw out the rest of the statements */
                    fgRemoveRestOfBlock = true;
                }

                // If op1 has no side-effects
                if ((op1->gtFlags & GTF_ALL_EFFECT) == 0)
                {
                    // If tree is an asg node
                    if (tree->OperIsAssignment())
                    {
                        /* Return the throw node as the new tree */
                        return op2->gtOp.gtOp1;
                    }

                    if (tree->OperGet() == GT_ARR_BOUNDS_CHECK)
                    {
                        /* Return the throw node as the new tree */
                        return op2->gtOp.gtOp1;
                    }

                    // If tree is a comma node
                    if (tree->OperGet() == GT_COMMA)
                    {
                        /* Return the throw node as the new tree */
                        return op2->gtOp.gtOp1;
                    }

                    /* for the shift nodes the type of op2 can differ from the tree type */
                    if ((typ == TYP_LONG) && (genActualType(op2->gtType) == TYP_INT))
                    {
                        noway_assert(GenTree::OperIsShiftOrRotate(oper));

                        GenTreePtr commaOp2 = op2->gtOp.gtOp2;

                        commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                        commaOp2->gtIntConCommon.SetLngValue(0);

                        /* Change the types of oper and commaOp2 to TYP_LONG */
                        op2->gtType = commaOp2->gtType = TYP_LONG;
                    }

                    if ((genActualType(typ) == TYP_INT) &&
                        (genActualType(op2->gtType) == TYP_LONG || varTypeIsFloating(op2->TypeGet())))
                    {
                        // An example case is comparison (say GT_GT) of two longs or floating point values.

                        GenTreePtr commaOp2 = op2->gtOp.gtOp2;

                        commaOp2->ChangeOperConst(GT_CNS_INT);
                        commaOp2->gtIntCon.gtIconVal = 0;
                        /* Change the types of oper and commaOp2 to TYP_INT */
                        op2->gtType = commaOp2->gtType = TYP_INT;
                    }

                    if ((typ == TYP_BYREF) && (genActualType(op2->gtType) == TYP_I_IMPL))
                    {
                        noway_assert(tree->OperGet() == GT_ADD);

                        GenTreePtr commaOp2 = op2->gtOp.gtOp2;

                        commaOp2->ChangeOperConst(GT_CNS_INT);
                        commaOp2->gtIntCon.gtIconVal = 0;
                        /* Change the types of oper and commaOp2 to TYP_BYREF */
                        op2->gtType = commaOp2->gtType = TYP_BYREF;
                    }

                    /* types should now match */
                    noway_assert((genActualType(typ) == genActualType(op2->gtType)));

                    /* Return the GT_COMMA node as the new tree */
                    return op2;
                }
            }
        }

        /*-------------------------------------------------------------------------
         * Optional morphing is done if tree transformations is permitted
         */

        if ((opts.compFlags & CLFLG_TREETRANS) == 0)
        {
            return tree;
        }

        tree = fgMorphSmpOpOptional(tree->AsOp());

    } // extra scope for gcc workaround
    return tree;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

GenTree* Compiler::fgMorphSmpOpOptional(GenTreeOp* tree)
{
    genTreeOps oper = tree->gtOper;
    GenTree*   op1  = tree->gtOp1;
    GenTree*   op2  = tree->gtOp2;
    var_types  typ  = tree->TypeGet();

    if (GenTree::OperIsCommutative(oper))
    {
        /* Swap the operands so that the more expensive one is 'op1' */

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            tree->gtOp1 = op2;
            tree->gtOp2 = op1;

            op2 = op1;
            op1 = tree->gtOp1;

            tree->gtFlags &= ~GTF_REVERSE_OPS;
        }

        if (oper == op2->gtOper)
        {
            /*  Reorder nested operators at the same precedence level to be
                left-recursive. For example, change "(a+(b+c))" to the
                equivalent expression "((a+b)+c)".
             */

            /* Things are handled differently for floating-point operators */

            if (!varTypeIsFloating(tree->TypeGet()))
            {
                fgMoveOpsLeft(tree);
                op1 = tree->gtOp1;
                op2 = tree->gtOp2;
            }
        }
    }

#if REARRANGE_ADDS

    /* Change "((x+icon)+y)" to "((x+y)+icon)"
       Don't reorder floating-point operations */

    if ((oper == GT_ADD) && !tree->gtOverflow() && (op1->gtOper == GT_ADD) && !op1->gtOverflow() &&
        varTypeIsIntegralOrI(typ))
    {
        GenTreePtr ad2 = op1->gtOp.gtOp2;

        if (op2->OperIsConst() == 0 && ad2->OperIsConst() != 0)
        {
            // This takes
            //       + (tree)
            //      / \
            //     /   \
            //    /     \
            //   + (op1) op2
            //  / \
            //     \
            //     ad2
            //
            // And it swaps ad2 and op2.  If (op2) is varTypeIsGC, then this implies that (tree) is
            // varTypeIsGC.  If (op1) is not, then when we swap (ad2) and (op2), then we have a TYP_INT node
            // (op1) with a child that is varTypeIsGC.  If we encounter that situation, make (op1) the same
            // type as (tree).
            //
            // Also, if (ad2) is varTypeIsGC then (tree) must also be (since op1 is), so no fixing is
            // necessary

            if (varTypeIsGC(op2->TypeGet()))
            {
                noway_assert(varTypeIsGC(typ));
                op1->gtType = typ;
            }
            tree->gtOp2 = ad2;

            op1->gtOp.gtOp2 = op2;
            op1->gtFlags |= op2->gtFlags & GTF_ALL_EFFECT;

            op2 = tree->gtOp2;
        }
    }

#endif

    /*-------------------------------------------------------------------------
     * Perform optional oper-specific postorder morphing
     */

    switch (oper)
    {
        genTreeOps cmop;
        bool       dstIsSafeLclVar;

        case GT_ASG:
            /* We'll convert "a = a <op> x" into "a <op>= x"                     */
            /*     and also  "a = x <op> a" into "a <op>= x" for communative ops */
            CLANG_FORMAT_COMMENT_ANCHOR;

#if !LONG_ASG_OPS
            if (typ == TYP_LONG)
            {
                break;
            }
#endif

            /* Make sure we're allowed to do this */

            if (optValnumCSE_phase)
            {
                // It is not safe to reorder/delete CSE's
                break;
            }

            /* Are we assigning to a GT_LCL_VAR ? */

            dstIsSafeLclVar = (op1->gtOper == GT_LCL_VAR);

            /* If we have a GT_LCL_VAR, then is the address taken? */
            if (dstIsSafeLclVar)
            {
                unsigned   lclNum = op1->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc = lvaTable + lclNum;

                noway_assert(lclNum < lvaCount);

                /* Is the address taken? */
                if (varDsc->lvAddrExposed)
                {
                    dstIsSafeLclVar = false;
                }
                else if (op2->gtFlags & GTF_ASG)
                {
                    break;
                }
            }

            if (!dstIsSafeLclVar)
            {
                if (op2->gtFlags & GTF_ASG)
                {
                    break;
                }

                if ((op2->gtFlags & GTF_CALL) && (op1->gtFlags & GTF_ALL_EFFECT))
                {
                    break;
                }
            }

            /* Special case: a cast that can be thrown away */

            if (op1->gtOper == GT_IND && op2->gtOper == GT_CAST && !op2->gtOverflow())
            {
                var_types srct;
                var_types cast;
                var_types dstt;

                srct = op2->gtCast.CastOp()->TypeGet();
                cast = (var_types)op2->CastToType();
                dstt = op1->TypeGet();

                /* Make sure these are all ints and precision is not lost */

                if (cast >= dstt && dstt <= TYP_INT && srct <= TYP_INT)
                {
                    op2 = tree->gtOp2 = op2->gtCast.CastOp();
                }
            }

            /* Make sure we have the operator range right */

            noway_assert(GT_SUB == GT_ADD + 1);
            noway_assert(GT_MUL == GT_ADD + 2);
            noway_assert(GT_DIV == GT_ADD + 3);
            noway_assert(GT_MOD == GT_ADD + 4);
            noway_assert(GT_UDIV == GT_ADD + 5);
            noway_assert(GT_UMOD == GT_ADD + 6);

            noway_assert(GT_OR == GT_ADD + 7);
            noway_assert(GT_XOR == GT_ADD + 8);
            noway_assert(GT_AND == GT_ADD + 9);

            noway_assert(GT_LSH == GT_ADD + 10);
            noway_assert(GT_RSH == GT_ADD + 11);
            noway_assert(GT_RSZ == GT_ADD + 12);

            /* Check for a suitable operator on the RHS */

            cmop = op2->OperGet();

            switch (cmop)
            {
                case GT_NEG:
                    // GT_CHS only supported for integer types
                    if (varTypeIsFloating(tree->TypeGet()))
                    {
                        break;
                    }

                    goto ASG_OP;

                case GT_MUL:
                    // GT_ASG_MUL only supported for floating point types
                    if (!varTypeIsFloating(tree->TypeGet()))
                    {
                        break;
                    }

                    __fallthrough;

                case GT_ADD:
                case GT_SUB:
                    if (op2->gtOverflow())
                    {
                        /* Disable folding into "<op>=" if the result can be
                           visible to anyone as <op> may throw an exception and
                           the assignment should not proceed
                           We are safe with an assignment to a local variables
                         */
                        if (ehBlockHasExnFlowDsc(compCurBB))
                        {
                            break;
                        }
                        if (!dstIsSafeLclVar)
                        {
                            break;
                        }
                    }
#ifndef _TARGET_AMD64_
                    // This is hard for byte-operations as we need to make
                    // sure both operands are in RBM_BYTE_REGS.
                    if (varTypeIsByte(op2->TypeGet()))
                        break;
#endif // _TARGET_AMD64_
                    goto ASG_OP;

                case GT_DIV:
                case GT_UDIV:
                    // GT_ASG_DIV only supported for floating point types
                    if (!varTypeIsFloating(tree->TypeGet()))
                    {
                        break;
                    }

                case GT_LSH:
                case GT_RSH:
                case GT_RSZ:

#if LONG_ASG_OPS

                    if (typ == TYP_LONG)
                        break;
#endif

                case GT_OR:
                case GT_XOR:
                case GT_AND:

#if LONG_ASG_OPS

                    /* TODO: allow non-const long assignment operators */

                    if (typ == TYP_LONG && op2->gtOp.gtOp2->gtOper != GT_CNS_LNG)
                        break;
#endif

                ASG_OP:
                {
                    bool bReverse       = false;
                    bool bAsgOpFoldable = fgShouldCreateAssignOp(tree, &bReverse);
                    if (bAsgOpFoldable)
                    {
                        if (bReverse)
                        {
                            // We will transform this from "a = x <op> a" to "a <op>= x"
                            // so we can now destroy the duplicate "a"
                            DEBUG_DESTROY_NODE(op2->gtOp.gtOp2);
                            op2->gtOp.gtOp2 = op2->gtOp.gtOp1;
                        }

                        /* Special case: "x |= -1" and "x &= 0" */
                        if (((cmop == GT_AND) && op2->gtOp.gtOp2->IsIntegralConst(0)) ||
                            ((cmop == GT_OR) && op2->gtOp.gtOp2->IsIntegralConst(-1)))
                        {
                            /* Simply change to an assignment */
                            tree->gtOp2 = op2->gtOp.gtOp2;
                            break;
                        }

                        if (cmop == GT_NEG)
                        {
                            /* This is "x = -x;", use the flipsign operator */

                            tree->ChangeOper(GT_CHS);

                            if (op1->gtOper == GT_LCL_VAR)
                            {
                                op1->gtFlags |= GTF_VAR_USEASG;
                            }

                            tree->gtOp2 = gtNewIconNode(0, op1->TypeGet());

                            break;
                        }

                        if (cmop == GT_RSH && varTypeIsSmall(op1->TypeGet()) && varTypeIsUnsigned(op1->TypeGet()))
                        {
                            // Changing from x = x op y to x op= y when x is a small integer type
                            // makes the op size smaller (originally the op size was 32 bits, after
                            // sign or zero extension of x, and there is an implicit truncation in the
                            // assignment).
                            // This is ok in most cases because the upper bits were
                            // lost when assigning the op result to a small type var,
                            // but it may not be ok for the right shift operation where the higher bits
                            // could be shifted into the lower bits and preserved.
                            // Signed right shift of signed x still works (i.e. (sbyte)((int)(sbyte)x >>signed y) ==
                            // (sbyte)x >>signed y)) as do unsigned right shift ((ubyte)((int)(ubyte)x >>unsigned y) ==
                            // (ubyte)x >>unsigned y), but signed right shift of an unigned small type may give the
                            // wrong
                            // result:
                            // e.g. (ubyte)((int)(ubyte)0xf0 >>signed 4) == 0x0f,
                            // but  (ubyte)0xf0 >>signed 4 == 0xff which is incorrect.
                            // The result becomes correct if we use >>unsigned instead of >>signed.
                            noway_assert(op1->TypeGet() == op2->gtOp.gtOp1->TypeGet());
                            cmop = GT_RSZ;
                        }

                        /* Replace with an assignment operator */
                        noway_assert(GT_ADD - GT_ADD == GT_ASG_ADD - GT_ASG_ADD);
                        noway_assert(GT_SUB - GT_ADD == GT_ASG_SUB - GT_ASG_ADD);
                        noway_assert(GT_OR - GT_ADD == GT_ASG_OR - GT_ASG_ADD);
                        noway_assert(GT_XOR - GT_ADD == GT_ASG_XOR - GT_ASG_ADD);
                        noway_assert(GT_AND - GT_ADD == GT_ASG_AND - GT_ASG_ADD);
                        noway_assert(GT_LSH - GT_ADD == GT_ASG_LSH - GT_ASG_ADD);
                        noway_assert(GT_RSH - GT_ADD == GT_ASG_RSH - GT_ASG_ADD);
                        noway_assert(GT_RSZ - GT_ADD == GT_ASG_RSZ - GT_ASG_ADD);

                        tree->SetOper((genTreeOps)(cmop - GT_ADD + GT_ASG_ADD));
                        tree->gtOp2 = op2->gtOp.gtOp2;

                        /* Propagate GTF_OVERFLOW */

                        if (op2->gtOverflowEx())
                        {
                            tree->gtType = op2->gtType;
                            tree->gtFlags |= (op2->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT | GTF_UNSIGNED));
                        }

#if FEATURE_SET_FLAGS

                        /* Propagate GTF_SET_FLAGS */
                        if (op2->gtSetFlags())
                        {
                            tree->gtRequestSetFlags();
                        }

#endif // FEATURE_SET_FLAGS

                        DEBUG_DESTROY_NODE(op2);
                        op2 = tree->gtOp2;

                        /* The target is used as well as being defined */
                        if (op1->OperIsLocal())
                        {
                            op1->gtFlags |= GTF_VAR_USEASG;
                        }

#if CPU_HAS_FP_SUPPORT
                        /* Check for the special case "x += y * x;" */

                        // GT_ASG_MUL only supported for floating point types
                        if (cmop != GT_ADD && cmop != GT_SUB)
                        {
                            break;
                        }

                        if (op2->gtOper == GT_MUL && varTypeIsFloating(tree->TypeGet()))
                        {
                            if (GenTree::Compare(op1, op2->gtOp.gtOp1))
                            {
                                /* Change "x += x * y" into "x *= (y + 1)" */

                                op2 = op2->gtOp.gtOp2;
                            }
                            else if (GenTree::Compare(op1, op2->gtOp.gtOp2))
                            {
                                /* Change "x += y * x" into "x *= (y + 1)" */

                                op2 = op2->gtOp.gtOp1;
                            }
                            else
                            {
                                break;
                            }

                            op1 = gtNewDconNode(1.0);

                            /* Now make the "*=" node */

                            if (cmop == GT_ADD)
                            {
                                /* Change "x += x * y" into "x *= (y + 1)" */

                                tree->gtOp2 = op2 = gtNewOperNode(GT_ADD, tree->TypeGet(), op2, op1);
                            }
                            else
                            {
                                /* Change "x -= x * y" into "x *= (1 - y)" */

                                noway_assert(cmop == GT_SUB);
                                tree->gtOp2 = op2 = gtNewOperNode(GT_SUB, tree->TypeGet(), op1, op2);
                            }
                            tree->ChangeOper(GT_ASG_MUL);
                        }
#endif // CPU_HAS_FP_SUPPORT
                    }
                }

                break;

                case GT_NOT:

                    /* Is the destination identical to the first RHS sub-operand? */

                    if (GenTree::Compare(op1, op2->gtOp.gtOp1))
                    {
                        /* This is "x = ~x" which is the same as "x ^= -1"
                         * Transform the node into a GT_ASG_XOR */

                        noway_assert(genActualType(typ) == TYP_INT || genActualType(typ) == TYP_LONG);

                        op2->gtOp.gtOp2 = (genActualType(typ) == TYP_INT) ? gtNewIconNode(-1) : gtNewLconNode(-1);

                        cmop = GT_XOR;
                        goto ASG_OP;
                    }

                    break;
                default:
                    break;
            }

            break;

        case GT_MUL:

            /* Check for the case "(val + icon) * icon" */

            if (op2->gtOper == GT_CNS_INT && op1->gtOper == GT_ADD)
            {
                GenTreePtr add = op1->gtOp.gtOp2;

                if (add->IsCnsIntOrI() && (op2->GetScaleIndexMul() != 0))
                {
                    if (tree->gtOverflow() || op1->gtOverflow())
                    {
                        break;
                    }

                    ssize_t imul = op2->gtIntCon.gtIconVal;
                    ssize_t iadd = add->gtIntCon.gtIconVal;

                    /* Change '(val + iadd) * imul' -> '(val * imul) + (iadd * imul)' */

                    oper = GT_ADD;
                    tree->ChangeOper(oper);

                    op2->gtIntCon.gtIconVal = iadd * imul;

                    op1->ChangeOper(GT_MUL);

                    add->gtIntCon.gtIconVal = imul;
#ifdef _TARGET_64BIT_
                    if (add->gtType == TYP_INT)
                    {
                        // we need to properly re-sign-extend or truncate after multiplying two int constants above
                        add->AsIntCon()->TruncateOrSignExtend32();
                    }
#endif //_TARGET_64BIT_
                }
            }

            break;

        case GT_DIV:

            /* For "val / 1", just return "val" */

            if (op2->IsIntegralConst(1))
            {
                DEBUG_DESTROY_NODE(tree);
                return op1;
            }

            break;

        case GT_LSH:

            /* Check for the case "(val + icon) << icon" */

            if (op2->IsCnsIntOrI() && op1->gtOper == GT_ADD && !op1->gtOverflow())
            {
                GenTreePtr cns = op1->gtOp.gtOp2;

                if (cns->IsCnsIntOrI() && (op2->GetScaleIndexShf() != 0))
                {
                    ssize_t ishf = op2->gtIntConCommon.IconValue();
                    ssize_t iadd = cns->gtIntConCommon.IconValue();

                    // printf("Changing '(val+icon1)<<icon2' into '(val<<icon2+icon1<<icon2)'\n");

                    /* Change "(val + iadd) << ishf" into "(val<<ishf + iadd<<ishf)" */

                    tree->ChangeOper(GT_ADD);
                    ssize_t result = iadd << ishf;
                    op2->gtIntConCommon.SetIconValue(result);
#ifdef _TARGET_64BIT_
                    if (op1->gtType == TYP_INT)
                    {
                        op2->AsIntCon()->TruncateOrSignExtend32();
                    }
#endif // _TARGET_64BIT_

                    // we are reusing the shift amount node here, but the type we want is that of the shift result
                    op2->gtType = op1->gtType;

                    if (cns->gtOper == GT_CNS_INT && cns->gtIntCon.gtFieldSeq != nullptr &&
                        cns->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq())
                    {
                        assert(cns->gtIntCon.gtFieldSeq->m_next == nullptr);
                        op2->gtIntCon.gtFieldSeq = cns->gtIntCon.gtFieldSeq;
                    }

                    op1->ChangeOper(GT_LSH);

                    cns->gtIntConCommon.SetIconValue(ishf);
                }
            }

            break;

        case GT_XOR:

            if (!optValnumCSE_phase)
            {
                /* "x ^ -1" is "~x" */

                if (op2->IsIntegralConst(-1))
                {
                    tree->ChangeOper(GT_NOT);
                    tree->gtOp2 = nullptr;
                    DEBUG_DESTROY_NODE(op2);
                }
                else if (op2->IsIntegralConst(1) && op1->OperIsCompare())
                {
                    /* "binaryVal ^ 1" is "!binaryVal" */
                    gtReverseCond(op1);
                    DEBUG_DESTROY_NODE(op2);
                    DEBUG_DESTROY_NODE(tree);
                    return op1;
                }
            }

            break;

        case GT_INITBLK:
            return fgMorphInitBlock(tree);
            break;

        case GT_COPYOBJ:
        case GT_COPYBLK:
            return fgMorphCopyBlock(tree);
            break;

        default:
            break;
    }
    return tree;
}

// code to generate a magic number and shift amount for the magic number division
// optimization.  This code is previously from UTC where it notes it was taken from
// _The_PowerPC_Compiler_Writer's_Guide_, pages 57-58.
// The paper it is based on is "Division by invariant integers using multiplication"
// by Torbjorn Granlund and Peter L. Montgomery in PLDI 94

template <typename T>
T GetSignedMagicNumberForDivide(T denom, int* shift /*out*/)
{
    // static SMAG smag;
    const int bits         = sizeof(T) * 8;
    const int bits_minus_1 = bits - 1;

    typedef typename jitstd::make_unsigned<T>::type UT;

    const UT two_nminus1 = UT(1) << bits_minus_1;

    int p;
    UT  absDenom;
    UT  absNc;
    UT  delta;
    UT  q1;
    UT  r1;
    UT  r2;
    UT  q2;
    UT  t;
    T   result_magic;
    int result_shift;
    int iters = 0;

    absDenom = abs(denom);
    t        = two_nminus1 + ((unsigned int)denom >> 31);
    absNc    = t - 1 - (t % absDenom);        // absolute value of nc
    p        = bits_minus_1;                  // initialize p
    q1       = two_nminus1 / absNc;           // initialize q1 = 2^p / abs(nc)
    r1       = two_nminus1 - (q1 * absNc);    // initialize r1 = rem(2^p, abs(nc))
    q2       = two_nminus1 / absDenom;        // initialize q1 = 2^p / abs(denom)
    r2       = two_nminus1 - (q2 * absDenom); // initialize r1 = rem(2^p, abs(denom))

    do
    {
        iters++;
        p++;
        q1 *= 2; // update q1 = 2^p / abs(nc)
        r1 *= 2; // update r1 = rem(2^p / abs(nc))

        if (r1 >= absNc)
        { // must be unsigned comparison
            q1++;
            r1 -= absNc;
        }

        q2 *= 2; // update q2 = 2^p / abs(denom)
        r2 *= 2; // update r2 = rem(2^p / abs(denom))

        if (r2 >= absDenom)
        { // must be unsigned comparison
            q2++;
            r2 -= absDenom;
        }

        delta = absDenom - r2;
    } while (q1 < delta || (q1 == delta && r1 == 0));

    result_magic = q2 + 1; // resulting magic number
    if (denom < 0)
    {
        result_magic = -result_magic;
    }
    *shift = p - bits; // resulting shift

    return result_magic;
}

bool Compiler::fgShouldUseMagicNumberDivide(GenTreeOp* tree)
{
#ifdef _TARGET_ARM64_
    // TODO-ARM64-NYI: We don't have a 'mulHi' implementation yet for ARM64
    return false;
#else

    // During the optOptimizeValnumCSEs phase we can call fgMorph and when we do,
    // if this method returns true we will introduce a new LclVar and
    // a couple of new GenTree nodes, including an assignment to the new LclVar.
    // None of these new GenTree nodes will have valid ValueNumbers.
    // That is an invalid state for a GenTree node during the optOptimizeValnumCSEs phase.
    //
    // Also during optAssertionProp when extracting side effects we can assert
    // during gtBuildCommaList if we have one tree that has Value Numbers
    //  and another one that does not.
    //
    if (!fgGlobalMorph)
    {
        // We only perform the Magic Number Divide optimization during
        // the initial global morph phase
        return false;
    }

    if (tree->gtFlags & GTF_OVERFLOW)
    {
        return false;
    }

    if (tree->gtOp2->gtOper != GT_CNS_INT && tree->gtOp2->gtOper != GT_CNS_LNG)
    {
        return false;
    }

    ssize_t cons = tree->gtOp2->gtIntConCommon.IconValue();

    if (cons == 0 || cons == -1 || cons == 1)
    {
        return false;
    }

    // codegen will expand these
    if (cons == SSIZE_T_MIN || isPow2(abs(cons)))
    {
        return false;
    }

    // someone else will fold this away, so don't make it complicated for them
    if (tree->gtOp1->IsCnsIntOrI())
    {
        return false;
    }

    // There is no technical barrier to handling unsigned, however it is quite rare
    // and more work to support and test
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        return false;
    }

    return true;
#endif
}

// transform x%c -> x-((x/c)*c)

GenTree* Compiler::fgMorphModByConst(GenTreeOp* tree)
{
    assert(fgShouldUseMagicNumberDivide(tree));

    var_types type = tree->gtType;

    GenTree* cns = tree->gtOp2;

    GenTree* numerator = fgMakeMultiUse(&tree->gtOp1);

    tree->SetOper(GT_DIV);

    GenTree* mul = gtNewOperNode(GT_MUL, type, tree, gtCloneExpr(cns));

    GenTree* sub = gtNewOperNode(GT_SUB, type, numerator, mul);

#ifdef DEBUG
    sub->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif

    return sub;
}

// For ARM64 we don't have a remainder instruction,
// The architecture manual suggests the following transformation to
// generate code for such operator:
//
// a % b = a - (a / b) * b;
//
// This method will produce the above expression in 'a' and 'b' are
// leaf nodes, otherwise, if any of them is not a leaf it will spill
// its value into a temporary variable, an example:
// (x * 2 - 1) % (y + 1) ->  t1 - (t2 * ( comma(t1 = x * 2 - 1, t1) / comma(t2 = y + 1, t2) ) )
//
GenTree* Compiler::fgMorphModToSubMulDiv(GenTreeOp* tree)
{
#ifndef _TARGET_ARM64_
    assert(!"This should only be called for ARM64");
#endif

    if (tree->OperGet() == GT_MOD)
    {
        tree->SetOper(GT_DIV);
    }
    else if (tree->OperGet() == GT_UMOD)
    {
        tree->SetOper(GT_UDIV);
    }
    else
    {
        noway_assert(!"Illegal gtOper in fgMorphModToSubMulDiv");
    }

    var_types type        = tree->gtType;
    GenTree*  denominator = tree->gtOp2;
    GenTree*  numerator   = tree->gtOp1;

    if (!numerator->OperIsLeaf())
    {
        numerator = fgMakeMultiUse(&tree->gtOp1);
    }

    if (!denominator->OperIsLeaf())
    {
        denominator = fgMakeMultiUse(&tree->gtOp2);
    }

    GenTree* mul = gtNewOperNode(GT_MUL, type, tree, gtCloneExpr(denominator));
    GenTree* sub = gtNewOperNode(GT_SUB, type, gtCloneExpr(numerator), mul);

#ifdef DEBUG
    sub->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif

    return sub;
}

// Turn a division by a constant into a multiplication by constant + some adjustments
// see comments on GetSignedMagicNumberForDivide for source of this algorithm.
// returns: the transformed tree

GenTree* Compiler::fgMorphDivByConst(GenTreeOp* tree)
{
    assert(fgShouldUseMagicNumberDivide(tree));

    JITDUMP("doing magic number divide optimization\n");

    int64_t   denominator = tree->gtOp2->gtIntConCommon.IconValue();
    int64_t   magic;
    int       shift;
    var_types type = tree->gtType;

    if (tree->gtType == TYP_INT)
    {
        magic = GetSignedMagicNumberForDivide<int32_t>((int32_t)denominator, &shift);
    }
    else
    {
        magic = GetSignedMagicNumberForDivide<int64_t>((int64_t)denominator, &shift);
    }

    GenTree* numerator = nullptr;

    // If signs of the denominator and magic number don't match,
    // we will need to use the numerator again.
    if (signum(denominator) != signum(magic))
    {
        numerator = fgMakeMultiUse(&tree->gtOp1);
        tree->gtFlags |= GTF_ASG;
    }

    if (type == TYP_LONG)
    {
        tree->gtOp2->gtIntConCommon.SetLngValue(magic);
    }
    else
    {
        tree->gtOp2->gtIntConCommon.SetIconValue((ssize_t)magic);
    }

    tree->SetOper(GT_MULHI);

    GenTree* t         = tree;
    GenTree* mulresult = tree;

    JITDUMP("Multiply Result:\n");
    DISPTREE(mulresult);

    GenTree* adjusted = mulresult;

    if (denominator > 0 && magic < 0)
    {
        // add the numerator back in
        adjusted = gtNewOperNode(GT_ADD, type, mulresult, numerator);
    }
    else if (denominator < 0 && magic > 0)
    {
        // subtract the numerator off
        adjusted = gtNewOperNode(GT_SUB, type, mulresult, numerator);
    }
    else
    {
        adjusted = mulresult;
    }

    GenTree* result1 = adjusted;
    if (shift != 0)
    {
        result1 = gtNewOperNode(GT_RSH, type, adjusted, gtNewIconNode(shift, TYP_INT));
    }

    GenTree* secondClone = fgMakeMultiUse(&result1);

    GenTree* result2 = gtNewOperNode(GT_RSZ, type, secondClone, gtNewIconNode(genTypeSize(type) * 8 - 1, type));

    GenTree* result = gtNewOperNode(GT_ADD, type, result1, result2);
    JITDUMP("Final Magic Number divide:\n");
    DISPTREE(result);

#ifdef DEBUG
    result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif

    return result;
}

//------------------------------------------------------------------------------
// fgOperIsBitwiseRotationRoot : Check if the operation can be a root of a bitwise rotation tree.
//
//
// Arguments:
//    oper  - Operation to check
//
// Return Value:
//    True if the operation can be a root of a bitwise rotation tree; false otherwise.

bool Compiler::fgOperIsBitwiseRotationRoot(genTreeOps oper)
{
    return (oper == GT_OR) || (oper == GT_XOR);
}

//------------------------------------------------------------------------------
// fgRecognizeAndMorphBitwiseRotation : Check if the tree represents a left or right rotation. If so, return
//                                      an equivalent GT_ROL or GT_ROR tree; otherwise, return the original tree.
//
// Arguments:
//    tree  - tree to check for a rotation pattern
//
// Return Value:
//    An equivalent GT_ROL or GT_ROR tree if a pattern is found; original tree otherwise.
//
// Assumption:
//    The input is a GT_OR or a GT_XOR tree.

GenTreePtr Compiler::fgRecognizeAndMorphBitwiseRotation(GenTreePtr tree)
{
#ifndef LEGACY_BACKEND
    //
    // Check for a rotation pattern, e.g.,
    //
    //                         OR                      ROL
    //                      /      \                   / \
        //                    LSH      RSZ      ->        x   y
    //                    / \      / \
        //                   x  AND   x  AND
    //                      / \      / \
        //                     y  31   ADD  31
    //                             / \
        //                            NEG 32
    //                             |
    //                             y
    // The patterns recognized:
    // (x << (y & M)) op (x >>> ((-y + N) & M))
    // (x >>> ((-y + N) & M)) op (x << (y & M))
    //
    // (x << y) op (x >>> (-y + N))
    // (x >> > (-y + N)) op (x << y)
    //
    // (x >>> (y & M)) op (x << ((-y + N) & M))
    // (x << ((-y + N) & M)) op (x >>> (y & M))
    //
    // (x >>> y) op (x << (-y + N))
    // (x << (-y + N)) op (x >>> y)
    //
    // (x << c1) op (x >>> c2)
    // (x >>> c1) op (x << c2)
    //
    // where
    // c1 and c2 are const
    // c1 + c2 == bitsize(x)
    // N == bitsize(x)
    // M is const
    // M & (N - 1) == N - 1
    // op is either | or ^

    if (((tree->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0) || ((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0))
    {
        // We can't do anything if the tree has assignments, calls, or volatile
        // reads. Note that we allow GTF_EXCEPT side effect since any exceptions
        // thrown by the original tree will be thrown by the transformed tree as well.
        return tree;
    }

    genTreeOps oper = tree->OperGet();
    assert(fgOperIsBitwiseRotationRoot(oper));

    // Check if we have an LSH on one side of the OR and an RSZ on the other side.
    GenTreePtr op1            = tree->gtGetOp1();
    GenTreePtr op2            = tree->gtGetOp2();
    GenTreePtr leftShiftTree  = nullptr;
    GenTreePtr rightShiftTree = nullptr;
    if ((op1->OperGet() == GT_LSH) && (op2->OperGet() == GT_RSZ))
    {
        leftShiftTree  = op1;
        rightShiftTree = op2;
    }
    else if ((op1->OperGet() == GT_RSZ) && (op2->OperGet() == GT_LSH))
    {
        leftShiftTree  = op2;
        rightShiftTree = op1;
    }
    else
    {
        return tree;
    }

    // Check if the trees representing the value to shift are identical.
    // We already checked that there are no side effects above.
    if (GenTree::Compare(leftShiftTree->gtGetOp1(), rightShiftTree->gtGetOp1()))
    {
        GenTreePtr rotatedValue           = leftShiftTree->gtGetOp1();
        var_types  rotatedValueActualType = genActualType(rotatedValue->gtType);
        ssize_t    rotatedValueBitSize    = genTypeSize(rotatedValueActualType) * 8;
        noway_assert((rotatedValueBitSize == 32) || (rotatedValueBitSize == 64));
        GenTreePtr leftShiftIndex  = leftShiftTree->gtGetOp2();
        GenTreePtr rightShiftIndex = rightShiftTree->gtGetOp2();

        // The shift index may be masked. At least (rotatedValueBitSize - 1) lower bits
        // shouldn't be masked for the transformation to be valid. If additional
        // higher bits are not masked, the transformation is still valid since the result
        // of MSIL shift instructions is unspecified if the shift amount is greater or equal
        // than the width of the value being shifted.
        ssize_t minimalMask    = rotatedValueBitSize - 1;
        ssize_t leftShiftMask  = -1;
        ssize_t rightShiftMask = -1;

        if ((leftShiftIndex->OperGet() == GT_AND))
        {
            if (leftShiftIndex->gtGetOp2()->IsCnsIntOrI())
            {
                leftShiftMask  = leftShiftIndex->gtGetOp2()->gtIntCon.gtIconVal;
                leftShiftIndex = leftShiftIndex->gtGetOp1();
            }
            else
            {
                return tree;
            }
        }

        if ((rightShiftIndex->OperGet() == GT_AND))
        {
            if (rightShiftIndex->gtGetOp2()->IsCnsIntOrI())
            {
                rightShiftMask  = rightShiftIndex->gtGetOp2()->gtIntCon.gtIconVal;
                rightShiftIndex = rightShiftIndex->gtGetOp1();
            }
            else
            {
                return tree;
            }
        }

        if (((minimalMask & leftShiftMask) != minimalMask) || ((minimalMask & rightShiftMask) != minimalMask))
        {
            // The shift index is overmasked, e.g., we have
            // something like (x << y & 15) or
            // (x >> (32 - y) & 15 with 32 bit x.
            // The transformation is not valid.
            return tree;
        }

        GenTreePtr shiftIndexWithAdd    = nullptr;
        GenTreePtr shiftIndexWithoutAdd = nullptr;
        genTreeOps rotateOp             = GT_NONE;
        GenTreePtr rotateIndex          = nullptr;

        if (leftShiftIndex->OperGet() == GT_ADD)
        {
            shiftIndexWithAdd    = leftShiftIndex;
            shiftIndexWithoutAdd = rightShiftIndex;
            rotateOp             = GT_ROR;
        }
        else if (rightShiftIndex->OperGet() == GT_ADD)
        {
            shiftIndexWithAdd    = rightShiftIndex;
            shiftIndexWithoutAdd = leftShiftIndex;
            rotateOp             = GT_ROL;
        }

        if (shiftIndexWithAdd != nullptr)
        {
            if (shiftIndexWithAdd->gtGetOp2()->IsCnsIntOrI())
            {
                if (shiftIndexWithAdd->gtGetOp2()->gtIntCon.gtIconVal == rotatedValueBitSize)
                {
                    if (shiftIndexWithAdd->gtGetOp1()->OperGet() == GT_NEG)
                    {
                        if (GenTree::Compare(shiftIndexWithAdd->gtGetOp1()->gtGetOp1(), shiftIndexWithoutAdd))
                        {
                            // We found one of these patterns:
                            // (x << (y & M)) | (x >>> ((-y + N) & M))
                            // (x << y) | (x >>> (-y + N))
                            // (x >>> (y & M)) | (x << ((-y + N) & M))
                            // (x >>> y) | (x << (-y + N))
                            // where N == bitsize(x), M is const, and
                            // M & (N - 1) == N - 1
                            CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_64BIT_
                            if (!shiftIndexWithoutAdd->IsCnsIntOrI() && (rotatedValueBitSize == 64))
                            {
                                // TODO: we need to handle variable-sized long shifts specially on x86.
                                // GT_LSH, GT_RSH, and GT_RSZ have helpers for this case. We may need
                                // to add helpers for GT_ROL and GT_ROR.
                                NYI("Rotation of a long value by variable amount");
                            }
#endif

                            rotateIndex = shiftIndexWithoutAdd;
                        }
                    }
                }
            }
        }
        else if ((leftShiftIndex->IsCnsIntOrI() && rightShiftIndex->IsCnsIntOrI()))
        {
            if (leftShiftIndex->gtIntCon.gtIconVal + rightShiftIndex->gtIntCon.gtIconVal == rotatedValueBitSize)
            {
                // We found this pattern:
                // (x << c1) | (x >>> c2)
                // where c1 and c2 are const and c1 + c2 == bitsize(x)
                rotateOp    = GT_ROL;
                rotateIndex = leftShiftIndex;
            }
        }

        if (rotateIndex != nullptr)
        {
            noway_assert(GenTree::OperIsRotate(rotateOp));

            unsigned inputTreeEffects = tree->gtFlags & GTF_ALL_EFFECT;

            // We can use the same tree only during global morph; reusing the tree in a later morph
            // may invalidate value numbers.
            if (fgGlobalMorph)
            {
                tree->gtOp.gtOp1 = rotatedValue;
                tree->gtOp.gtOp2 = rotateIndex;
                tree->ChangeOper(rotateOp);
                noway_assert(inputTreeEffects == ((rotatedValue->gtFlags | rotateIndex->gtFlags) & GTF_ALL_EFFECT));
            }
            else
            {
                tree = gtNewOperNode(rotateOp, rotatedValueActualType, rotatedValue, rotateIndex);
                noway_assert(inputTreeEffects == (tree->gtFlags & GTF_ALL_EFFECT));
            }

            return tree;
        }
    }
#endif // LEGACY_BACKEND
    return tree;
}

#if !CPU_HAS_FP_SUPPORT
GenTreePtr Compiler::fgMorphToEmulatedFP(GenTreePtr tree)
{

    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();
    GenTreePtr op1  = tree->gtOp.gtOp1;
    GenTreePtr op2  = tree->gtGetOp2();

    /*
        We have to use helper calls for all FP operations:

            FP operators that operate on FP values
            casts to and from FP
            comparisons of FP values
     */

    if (varTypeIsFloating(typ) || (op1 && varTypeIsFloating(op1->TypeGet())))
    {
        int        helper;
        GenTreePtr args;
        size_t     argc = genTypeStSz(typ);

        /* Not all FP operations need helper calls */

        switch (oper)
        {
            case GT_ASG:
            case GT_IND:
            case GT_LIST:
            case GT_ADDR:
            case GT_COMMA:
                return tree;
        }

#ifdef DEBUG

        /* If the result isn't FP, it better be a compare or cast */

        if (!(varTypeIsFloating(typ) || tree->OperIsCompare() || oper == GT_CAST))
            gtDispTree(tree);

        noway_assert(varTypeIsFloating(typ) || tree->OperIsCompare() || oper == GT_CAST);
#endif

        /* Keep track of how many arguments we're passing */

        fgPtrArgCntCur += argc;

        /* Is this a binary operator? */

        if (op2)
        {
            /* Add the second operand to the argument count */

            fgPtrArgCntCur += argc;
            argc *= 2;

            /* What kind of an operator do we have? */

            switch (oper)
            {
                case GT_ADD:
                    helper = CPX_R4_ADD;
                    break;
                case GT_SUB:
                    helper = CPX_R4_SUB;
                    break;
                case GT_MUL:
                    helper = CPX_R4_MUL;
                    break;
                case GT_DIV:
                    helper = CPX_R4_DIV;
                    break;
                // case GT_MOD: helper = CPX_R4_REM; break;

                case GT_EQ:
                    helper = CPX_R4_EQ;
                    break;
                case GT_NE:
                    helper = CPX_R4_NE;
                    break;
                case GT_LT:
                    helper = CPX_R4_LT;
                    break;
                case GT_LE:
                    helper = CPX_R4_LE;
                    break;
                case GT_GE:
                    helper = CPX_R4_GE;
                    break;
                case GT_GT:
                    helper = CPX_R4_GT;
                    break;

                default:
#ifdef DEBUG
                    gtDispTree(tree);
#endif
                    noway_assert(!"unexpected FP binary op");
                    break;
            }

            args = gtNewArgList(tree->gtOp.gtOp2, tree->gtOp.gtOp1);
        }
        else
        {
            switch (oper)
            {
                case GT_RETURN:
                    return tree;

                case GT_CAST:
                    noway_assert(!"FP cast");

                case GT_NEG:
                    helper = CPX_R4_NEG;
                    break;

                default:
#ifdef DEBUG
                    gtDispTree(tree);
#endif
                    noway_assert(!"unexpected FP unary op");
                    break;
            }

            args = gtNewArgList(tree->gtOp.gtOp1);
        }

        /* If we have double result/operands, modify the helper */

        if (typ == TYP_DOUBLE)
        {
            noway_assert(CPX_R4_NEG + 1 == CPX_R8_NEG);
            noway_assert(CPX_R4_ADD + 1 == CPX_R8_ADD);
            noway_assert(CPX_R4_SUB + 1 == CPX_R8_SUB);
            noway_assert(CPX_R4_MUL + 1 == CPX_R8_MUL);
            noway_assert(CPX_R4_DIV + 1 == CPX_R8_DIV);

            helper++;
        }
        else
        {
            noway_assert(tree->OperIsCompare());

            noway_assert(CPX_R4_EQ + 1 == CPX_R8_EQ);
            noway_assert(CPX_R4_NE + 1 == CPX_R8_NE);
            noway_assert(CPX_R4_LT + 1 == CPX_R8_LT);
            noway_assert(CPX_R4_LE + 1 == CPX_R8_LE);
            noway_assert(CPX_R4_GE + 1 == CPX_R8_GE);
            noway_assert(CPX_R4_GT + 1 == CPX_R8_GT);
        }

        tree = fgMorphIntoHelperCall(tree, helper, args);

        if (fgPtrArgCntMax < fgPtrArgCntCur)
            fgPtrArgCntMax = fgPtrArgCntCur;

        fgPtrArgCntCur -= argc;
        return tree;

        case GT_RETURN:

            if (op1)
            {

                if (compCurBB == genReturnBB)
                {
                    /* This is the 'exitCrit' call at the exit label */

                    noway_assert(op1->gtType == TYP_VOID);
                    noway_assert(op2 == 0);

                    tree->gtOp.gtOp1 = op1 = fgMorphTree(op1);

                    return tree;
                }

                /* This is a (real) return value -- check its type */
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                if (genActualType(op1->TypeGet()) != genActualType(info.compRetType))
                {
                    bool allowMismatch = false;

                    // Allow TYP_BYREF to be returned as TYP_I_IMPL and vice versa
                    if ((info.compRetType == TYP_BYREF && genActualType(op1->TypeGet()) == TYP_I_IMPL) ||
                        (op1->TypeGet() == TYP_BYREF && genActualType(info.compRetType) == TYP_I_IMPL))
                        allowMismatch = true;

                    if (varTypeIsFloating(info.compRetType) && varTypeIsFloating(op1->TypeGet()))
                        allowMismatch = true;

                    if (!allowMismatch)
                        NO_WAY("Return type mismatch");
                }
#endif
            }
            break;
    }
    return tree;
}
#endif

/*****************************************************************************
 *
 *  Transform the given tree for code generation and return an equivalent tree.
 */

GenTreePtr Compiler::fgMorphTree(GenTreePtr tree, MorphAddrContext* mac)
{
    noway_assert(tree);
    noway_assert(tree->gtOper != GT_STMT);

#ifdef DEBUG
    if (verbose)
    {
        if ((unsigned)JitConfig.JitBreakMorphTree() == tree->gtTreeID)
        {
            noway_assert(!"JitBreakMorphTree hit");
        }
    }
#endif

#ifdef DEBUG
    int thisMorphNum = 0;
    if (verbose && treesBeforeAfterMorph)
    {
        thisMorphNum = morphNum++;
        printf("\nfgMorphTree (before %d):\n", thisMorphNum);
        gtDispTree(tree);
    }
#endif

/*-------------------------------------------------------------------------
 * fgMorphTree() can potentially replace a tree with another, and the
 * caller has to store the return value correctly.
 * Turn this on to always make copy of "tree" here to shake out
 * hidden/unupdated references.
 */

#ifdef DEBUG

    if (compStressCompile(STRESS_GENERIC_CHECK, 0))
    {
        GenTreePtr copy;

#ifdef SMALL_TREE_NODES
        if (GenTree::s_gtNodeSizes[tree->gtOper] == TREE_NODE_SZ_SMALL)
        {
            copy = gtNewLargeOperNode(GT_ADD, TYP_INT);
        }
        else
#endif
        {
            copy = new (this, GT_CALL) GenTreeCall(TYP_INT);
        }

        copy->CopyFrom(tree, this);

#if defined(LATE_DISASM)
        // GT_CNS_INT is considered small, so CopyFrom() won't copy all fields
        if ((tree->gtOper == GT_CNS_INT) && tree->IsIconHandle())
        {
            copy->gtIntCon.gtIconHdl.gtIconHdl1 = tree->gtIntCon.gtIconHdl.gtIconHdl1;
            copy->gtIntCon.gtIconHdl.gtIconHdl2 = tree->gtIntCon.gtIconHdl.gtIconHdl2;
        }
#endif

        DEBUG_DESTROY_NODE(tree);
        tree = copy;
    }
#endif // DEBUG

    if (fgGlobalMorph)
    {
        /* Ensure that we haven't morphed this node already */
        assert(((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) == 0) && "ERROR: Already morphed this node!");

#if LOCAL_ASSERTION_PROP
        /* Before morphing the tree, we try to propagate any active assertions */
        if (optLocalAssertionProp)
        {
            /* Do we have any active assertions? */

            if (optAssertionCount > 0)
            {
                GenTreePtr newTree = tree;
                while (newTree != nullptr)
                {
                    tree = newTree;
                    /* newTree is non-Null if we propagated an assertion */
                    newTree = optAssertionProp(apFull, tree, nullptr);
                }
                noway_assert(tree != nullptr);
            }
        }
        PREFAST_ASSUME(tree != nullptr);
#endif
    }

    /* Save the original un-morphed tree for fgMorphTreeDone */

    GenTreePtr oldTree = tree;

    /* Figure out what kind of a node we have */

    unsigned kind = tree->OperKind();

    /* Is this a constant node? */

    if (kind & GTK_CONST)
    {
        tree = fgMorphConst(tree);
        goto DONE;
    }

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        tree = fgMorphLeaf(tree);
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        tree = fgMorphSmpOp(tree, mac);
        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch (tree->OperGet())
    {
        case GT_FIELD:
            tree = fgMorphField(tree, mac);
            break;

        case GT_CALL:
            tree = fgMorphCall(tree->AsCall());
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            fgSetRngChkTarget(tree);

            GenTreeBoundsChk* bndsChk = tree->AsBoundsChk();
            bndsChk->gtArrLen         = fgMorphTree(bndsChk->gtArrLen);
            bndsChk->gtIndex          = fgMorphTree(bndsChk->gtIndex);
            // If the index is a comma(throw, x), just return that.
            if (!optValnumCSE_phase && fgIsCommaThrow(bndsChk->gtIndex))
            {
                tree = bndsChk->gtIndex;
            }

            // Propagate effects flags upwards
            bndsChk->gtFlags |= (bndsChk->gtArrLen->gtFlags & GTF_ALL_EFFECT);
            bndsChk->gtFlags |= (bndsChk->gtIndex->gtFlags & GTF_ALL_EFFECT);

            // Otherwise, we don't change the tree.
        }
        break;

        case GT_ARR_ELEM:
            tree->gtArrElem.gtArrObj = fgMorphTree(tree->gtArrElem.gtArrObj);
            tree->gtFlags |= tree->gtArrElem.gtArrObj->gtFlags & GTF_ALL_EFFECT;

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                tree->gtArrElem.gtArrInds[dim] = fgMorphTree(tree->gtArrElem.gtArrInds[dim]);
                tree->gtFlags |= tree->gtArrElem.gtArrInds[dim]->gtFlags & GTF_ALL_EFFECT;
            }
            if (fgGlobalMorph)
            {
                fgSetRngChkTarget(tree, false);
            }
            break;

        case GT_ARR_OFFSET:
            tree->gtArrOffs.gtOffset = fgMorphTree(tree->gtArrOffs.gtOffset);
            tree->gtFlags |= tree->gtArrOffs.gtOffset->gtFlags & GTF_ALL_EFFECT;
            tree->gtArrOffs.gtIndex = fgMorphTree(tree->gtArrOffs.gtIndex);
            tree->gtFlags |= tree->gtArrOffs.gtIndex->gtFlags & GTF_ALL_EFFECT;
            tree->gtArrOffs.gtArrObj = fgMorphTree(tree->gtArrOffs.gtArrObj);
            tree->gtFlags |= tree->gtArrOffs.gtArrObj->gtFlags & GTF_ALL_EFFECT;
            if (fgGlobalMorph)
            {
                fgSetRngChkTarget(tree, false);
            }
            break;

        case GT_CMPXCHG:
            tree->gtCmpXchg.gtOpLocation  = fgMorphTree(tree->gtCmpXchg.gtOpLocation);
            tree->gtCmpXchg.gtOpValue     = fgMorphTree(tree->gtCmpXchg.gtOpValue);
            tree->gtCmpXchg.gtOpComparand = fgMorphTree(tree->gtCmpXchg.gtOpComparand);
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
#endif
            noway_assert(!"unexpected operator");
    }
DONE:

    fgMorphTreeDone(tree, oldTree DEBUGARG(thisMorphNum));

    return tree;
}

#if LOCAL_ASSERTION_PROP
/*****************************************************************************
 *
 *  Kill all dependent assertions with regard to lclNum.
 *
 */

void Compiler::fgKillDependentAssertions(unsigned lclNum DEBUGARG(GenTreePtr tree))
{
    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        // Kill the field locals.
        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            fgKillDependentAssertions(i DEBUGARG(tree));
        }

        // Fall through to kill the struct local itself.
    }

    /* All dependent assertions are killed here */

    ASSERT_TP killed = BitVecOps::MakeCopy(apTraits, GetAssertionDep(lclNum));

    if (killed)
    {
        AssertionIndex index = optAssertionCount;
        while (killed && (index > 0))
        {
            if (BitVecOps::IsMember(apTraits, killed, index - 1))
            {
#ifdef DEBUG
                AssertionDsc* curAssertion = optGetAssertion(index);
                noway_assert((curAssertion->op1.lcl.lclNum == lclNum) ||
                             ((curAssertion->op2.kind == O2K_LCLVAR_COPY) && (curAssertion->op2.lcl.lclNum == lclNum)));
                if (verbose)
                {
                    printf("\nThe assignment ");
                    printTreeID(tree);
                    printf(" using V%02u removes: ", curAssertion->op1.lcl.lclNum);
                    optPrintAssertion(curAssertion);
                }
#endif
                // Remove this bit from the killed mask
                BitVecOps::RemoveElemD(apTraits, killed, index - 1);

                optAssertionRemove(index);
            }

            index--;
        }

        // killed mask should now be zero
        noway_assert(BitVecOps::IsEmpty(apTraits, killed));
    }
}
#endif // LOCAL_ASSERTION_PROP

/*****************************************************************************
 *
 *  This function is called to complete the morphing of a tree node
 *  It should only be called once for each node.
 *  If DEBUG is defined the flag GTF_DEBUG_NODE_MORPHED is checked and updated,
 *  to enforce the invariant that each node is only morphed once.
 *  If LOCAL_ASSERTION_PROP is enabled the result tree may be replaced
 *  by an equivalent tree.
 *
 */

void Compiler::fgMorphTreeDone(GenTreePtr tree,
                               GenTreePtr oldTree /* == NULL */
                               DEBUGARG(int morphNum))
{
#ifdef DEBUG
    if (verbose && treesBeforeAfterMorph)
    {
        printf("\nfgMorphTree (after %d):\n", morphNum);
        gtDispTree(tree);
        printf(""); // in our logic this causes a flush
    }
#endif

    if (!fgGlobalMorph)
    {
        return;
    }

    if ((oldTree != nullptr) && (oldTree != tree))
    {
        /* Ensure that we have morphed this node */
        assert((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) && "ERROR: Did not morph this node!");

#ifdef DEBUG
        TransferTestDataToNode(oldTree, tree);
#endif
    }
    else
    {
        // Ensure that we haven't morphed this node already
        assert(((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) == 0) && "ERROR: Already morphed this node!");
    }

    if (tree->OperKind() & GTK_CONST)
    {
        goto DONE;
    }

#if LOCAL_ASSERTION_PROP

    if (!optLocalAssertionProp)
    {
        goto DONE;
    }

    /* Do we have any active assertions? */

    if (optAssertionCount > 0)
    {
        /* Is this an assignment to a local variable */

        if ((tree->OperKind() & GTK_ASGOP) &&
            (tree->gtOp.gtOp1->gtOper == GT_LCL_VAR || tree->gtOp.gtOp1->gtOper == GT_LCL_FLD))
        {
            unsigned op1LclNum = tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
            noway_assert(op1LclNum < lvaCount);
            fgKillDependentAssertions(op1LclNum DEBUGARG(tree));
        }
    }

    /* If this tree makes a new assertion - make it available */
    optAssertionGen(tree);

#endif // LOCAL_ASSERTION_PROP

DONE:;

#ifdef DEBUG
    /* Mark this node as being morphed */
    tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
}

/*****************************************************************************
 *
 *  Check and fold blocks of type BBJ_COND and BBJ_SWITCH on constants
 *  Returns true if we modified the flow graph
 */

bool Compiler::fgFoldConditional(BasicBlock* block)
{
    bool result = false;

    // We don't want to make any code unreachable
    if (opts.compDbgCode || opts.MinOpts())
    {
        return false;
    }

    if (block->bbJumpKind == BBJ_COND)
    {
        noway_assert(block->bbTreeList && block->bbTreeList->gtPrev);

        GenTreePtr stmt = block->bbTreeList->gtPrev;

        noway_assert(stmt->gtNext == nullptr);

        if (stmt->gtStmt.gtStmtExpr->gtOper == GT_CALL)
        {
            noway_assert(fgRemoveRestOfBlock);

            /* Unconditional throw - transform the basic block into a BBJ_THROW */
            fgConvertBBToThrowBB(block);

            /* Remove 'block' from the predecessor list of 'block->bbNext' */
            fgRemoveRefPred(block->bbNext, block);

            /* Remove 'block' from the predecessor list of 'block->bbJumpDest' */
            fgRemoveRefPred(block->bbJumpDest, block);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at BB%02u\n", block->bbNum);
                printf("BB%02u becomes a BBJ_THROW\n", block->bbNum);
            }
#endif
            goto DONE_COND;
        }

        noway_assert(stmt->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);

        /* Did we fold the conditional */

        noway_assert(stmt->gtStmt.gtStmtExpr->gtOp.gtOp1);
        GenTreePtr cond;
        cond = stmt->gtStmt.gtStmtExpr->gtOp.gtOp1;

        if (cond->OperKind() & GTK_CONST)
        {
            /* Yupee - we folded the conditional!
             * Remove the conditional statement */

            noway_assert(cond->gtOper == GT_CNS_INT);
            noway_assert((block->bbNext->countOfInEdges() > 0) && (block->bbJumpDest->countOfInEdges() > 0));

            /* remove the statement from bbTreelist - No need to update
             * the reference counts since there are no lcl vars */
            fgRemoveStmt(block, stmt);

            // block is a BBJ_COND that we are folding the conditional for
            // bTaken is the path that will always be taken from block
            // bNotTaken is the path that will never be taken from block
            //
            BasicBlock* bTaken;
            BasicBlock* bNotTaken;

            if (cond->gtIntCon.gtIconVal != 0)
            {
                /* JTRUE 1 - transform the basic block into a BBJ_ALWAYS */
                block->bbJumpKind = BBJ_ALWAYS;
                bTaken            = block->bbJumpDest;
                bNotTaken         = block->bbNext;
            }
            else
            {
                /* Unmark the loop if we are removing a backwards branch */
                /* dest block must also be marked as a loop head and     */
                /* We must be able to reach the backedge block           */
                if ((block->bbJumpDest->isLoopHead()) && (block->bbJumpDest->bbNum <= block->bbNum) &&
                    fgReachable(block->bbJumpDest, block))
                {
                    optUnmarkLoopBlocks(block->bbJumpDest, block);
                }

                /* JTRUE 0 - transform the basic block into a BBJ_NONE   */
                block->bbJumpKind = BBJ_NONE;
                noway_assert(!(block->bbFlags & BBF_NEEDS_GCPOLL));
                bTaken    = block->bbNext;
                bNotTaken = block->bbJumpDest;
            }

            if (fgHaveValidEdgeWeights)
            {
                // We are removing an edge from block to bNotTaken
                // and we have already computed the edge weights, so
                // we will try to adjust some of the weights
                //
                flowList*   edgeTaken = fgGetPredForBlock(bTaken, block);
                BasicBlock* bUpdated  = nullptr; // non-NULL if we updated the weight of an internal block

                // We examine the taken edge (block -> bTaken)
                // if block has valid profile weight and bTaken does not we try to adjust bTaken's weight
                // else if bTaken has valid profile weight and block does not we try to adjust block's weight
                // We can only adjust the block weights when (the edge block -> bTaken) is the only edge into bTaken
                //
                if (block->bbFlags & BBF_PROF_WEIGHT)
                {
                    // The edge weights for (block -> bTaken) are 100% of block's weight
                    edgeTaken->flEdgeWeightMin = block->bbWeight;
                    edgeTaken->flEdgeWeightMax = block->bbWeight;

                    if ((bTaken->bbFlags & BBF_PROF_WEIGHT) == 0)
                    {
                        if ((bTaken->countOfInEdges() == 1) || (bTaken->bbWeight < block->bbWeight))
                        {
                            // Update the weight of bTaken
                            bTaken->inheritWeight(block);
                            bUpdated = bTaken;
                        }
                    }
                }
                else if (bTaken->bbFlags & BBF_PROF_WEIGHT)
                {
                    if (bTaken->countOfInEdges() == 1)
                    {
                        // There is only one in edge to bTaken
                        edgeTaken->flEdgeWeightMin = bTaken->bbWeight;
                        edgeTaken->flEdgeWeightMax = bTaken->bbWeight;

                        // Update the weight of block
                        block->inheritWeight(bTaken);
                        bUpdated = block;
                    }
                }

                if (bUpdated != nullptr)
                {
                    flowList* edge;
                    // Now fix the weights of the edges out of 'bUpdated'
                    switch (bUpdated->bbJumpKind)
                    {
                        case BBJ_NONE:
                            edge                  = fgGetPredForBlock(bUpdated->bbNext, bUpdated);
                            edge->flEdgeWeightMax = bUpdated->bbWeight;
                            break;
                        case BBJ_COND:
                            edge                  = fgGetPredForBlock(bUpdated->bbNext, bUpdated);
                            edge->flEdgeWeightMax = bUpdated->bbWeight;
                            __fallthrough;
                        case BBJ_ALWAYS:
                            edge                  = fgGetPredForBlock(bUpdated->bbJumpDest, bUpdated);
                            edge->flEdgeWeightMax = bUpdated->bbWeight;
                            break;
                        default:
                            // We don't handle BBJ_SWITCH
                            break;
                    }
                }
            }

            /* modify the flow graph */

            /* Remove 'block' from the predecessor list of 'bNotTaken' */
            fgRemoveRefPred(bNotTaken, block);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at BB%02u\n", block->bbNum);
                printf("BB%02u becomes a %s", block->bbNum,
                       block->bbJumpKind == BBJ_ALWAYS ? "BBJ_ALWAYS" : "BBJ_NONE");
                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    printf(" to BB%02u", block->bbJumpDest->bbNum);
                }
                printf("\n");
            }
#endif

            /* if the block was a loop condition we may have to modify
             * the loop table */

            for (unsigned loopNum = 0; loopNum < optLoopCount; loopNum++)
            {
                /* Some loops may have been already removed by
                 * loop unrolling or conditional folding */

                if (optLoopTable[loopNum].lpFlags & LPFLG_REMOVED)
                {
                    continue;
                }

                /* We are only interested in the loop bottom */

                if (optLoopTable[loopNum].lpBottom == block)
                {
                    if (cond->gtIntCon.gtIconVal == 0)
                    {
                        /* This was a bogus loop (condition always false)
                         * Remove the loop from the table */

                        optLoopTable[loopNum].lpFlags |= LPFLG_REMOVED;
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Removing loop L%02u (from BB%02u to BB%02u)\n\n", loopNum,
                                   optLoopTable[loopNum].lpFirst->bbNum, optLoopTable[loopNum].lpBottom->bbNum);
                        }
#endif
                    }
                }
            }
        DONE_COND:
            result = true;
        }
    }
    else if (block->bbJumpKind == BBJ_SWITCH)
    {
        noway_assert(block->bbTreeList && block->bbTreeList->gtPrev);

        GenTreePtr stmt = block->bbTreeList->gtPrev;

        noway_assert(stmt->gtNext == nullptr);

        if (stmt->gtStmt.gtStmtExpr->gtOper == GT_CALL)
        {
            noway_assert(fgRemoveRestOfBlock);

            /* Unconditional throw - transform the basic block into a BBJ_THROW */
            fgConvertBBToThrowBB(block);

            /* update the flow graph */

            unsigned     jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock** jumpTab = block->bbJumpSwt->bbsDstTab;

            for (unsigned val = 0; val < jumpCnt; val++, jumpTab++)
            {
                BasicBlock* curJump = *jumpTab;

                /* Remove 'block' from the predecessor list of 'curJump' */
                fgRemoveRefPred(curJump, block);
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at BB%02u\n", block->bbNum);
                printf("BB%02u becomes a BBJ_THROW\n", block->bbNum);
            }
#endif
            goto DONE_SWITCH;
        }

        noway_assert(stmt->gtStmt.gtStmtExpr->gtOper == GT_SWITCH);

        /* Did we fold the conditional */

        noway_assert(stmt->gtStmt.gtStmtExpr->gtOp.gtOp1);
        GenTreePtr cond;
        cond = stmt->gtStmt.gtStmtExpr->gtOp.gtOp1;

        if (cond->OperKind() & GTK_CONST)
        {
            /* Yupee - we folded the conditional!
             * Remove the conditional statement */

            noway_assert(cond->gtOper == GT_CNS_INT);

            /* remove the statement from bbTreelist - No need to update
             * the reference counts since there are no lcl vars */
            fgRemoveStmt(block, stmt);

            /* modify the flow graph */

            /* Find the actual jump target */
            unsigned switchVal;
            switchVal = (unsigned)cond->gtIntCon.gtIconVal;
            unsigned jumpCnt;
            jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock** jumpTab;
            jumpTab = block->bbJumpSwt->bbsDstTab;
            bool foundVal;
            foundVal = false;

            for (unsigned val = 0; val < jumpCnt; val++, jumpTab++)
            {
                BasicBlock* curJump = *jumpTab;

                assert(curJump->countOfInEdges() > 0);

                // If val matches switchVal or we are at the last entry and
                // we never found the switch value then set the new jump dest

                if ((val == switchVal) || (!foundVal && (val == jumpCnt - 1)))
                {
                    if (curJump != block->bbNext)
                    {
                        /* transform the basic block into a BBJ_ALWAYS */
                        block->bbJumpKind = BBJ_ALWAYS;
                        block->bbJumpDest = curJump;

                        // if we are jumping backwards, make sure we have a GC Poll.
                        if (curJump->bbNum > block->bbNum)
                        {
                            block->bbFlags &= ~BBF_NEEDS_GCPOLL;
                        }
                    }
                    else
                    {
                        /* transform the basic block into a BBJ_NONE */
                        block->bbJumpKind = BBJ_NONE;
                        block->bbFlags &= ~BBF_NEEDS_GCPOLL;
                    }
                    foundVal = true;
                }
                else
                {
                    /* Remove 'block' from the predecessor list of 'curJump' */
                    fgRemoveRefPred(curJump, block);
                }
            }
#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at BB%02u\n", block->bbNum);
                printf("BB%02u becomes a %s", block->bbNum,
                       block->bbJumpKind == BBJ_ALWAYS ? "BBJ_ALWAYS" : "BBJ_NONE");
                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    printf(" to BB%02u", block->bbJumpDest->bbNum);
                }
                printf("\n");
            }
#endif
        DONE_SWITCH:
            result = true;
        }
    }
    return result;
}

//*****************************************************************************
//
// Morphs a single statement in a block.
// Can be called anytime, unlike fgMorphStmts() which should only be called once.
//
// Returns true  if 'stmt' was removed from the block.
// Returns false if 'stmt' is still in the block (even if other statements were removed).
//

bool Compiler::fgMorphBlockStmt(BasicBlock* block, GenTreePtr stmt DEBUGARG(const char* msg))
{
    noway_assert(stmt->gtOper == GT_STMT);

    compCurBB   = block;
    compCurStmt = stmt;

    GenTreePtr morph = fgMorphTree(stmt->gtStmt.gtStmtExpr);

    // Bug 1106830 - During the CSE phase we can't just remove
    // morph->gtOp.gtOp2 as it could contain CSE expressions.
    // This leads to a noway_assert in OptCSE.cpp when
    // searching for the removed CSE ref. (using gtFindLink)
    //
    if (!optValnumCSE_phase)
    {
        /* Check for morph as a GT_COMMA with an unconditional throw */
        if (fgIsCommaThrow(morph, true))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Folding a top-level fgIsCommaThrow stmt\n");
                printf("Removing op2 as unreachable:\n");
                gtDispTree(morph->gtOp.gtOp2);
                printf("\n");
            }
#endif
            /* Use the call as the new stmt */
            morph = morph->gtOp.gtOp1;
            noway_assert(morph->gtOper == GT_CALL);
        }

        /* we can get a throw as a statement root*/
        if (fgIsThrow(morph))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("We have a top-level fgIsThrow stmt\n");
                printf("Removing the rest of block as unreachable:\n");
            }
#endif
            noway_assert((morph->gtFlags & GTF_COLON_COND) == 0);
            fgRemoveRestOfBlock = true;
        }
    }

    stmt->gtStmt.gtStmtExpr = morph;

    /* Can the entire tree be removed ? */

    bool removedStmt = fgCheckRemoveStmt(block, stmt);

    /* Or this is the last statement of a conditional branch that was just folded */

    if ((!removedStmt) && (stmt->gtNext == nullptr) && !fgRemoveRestOfBlock)
    {
        if (fgFoldConditional(block))
        {
            if (block->bbJumpKind != BBJ_THROW)
            {
                removedStmt = true;
            }
        }
    }

    if (!removedStmt)
    {
        /* Have to re-do the evaluation order since for example
         * some later code does not expect constants as op1 */
        gtSetStmtInfo(stmt);

        /* Have to re-link the nodes for this statement */
        fgSetStmtSeq(stmt);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("%s %s tree:\n", msg, (removedStmt ? "removed" : "morphed"));
        gtDispTree(morph);
        printf("\n");
    }
#endif

    if (fgRemoveRestOfBlock)
    {
        /* Remove the rest of the stmts in the block */

        while (stmt->gtNext)
        {
            stmt = stmt->gtNext;
            noway_assert(stmt->gtOper == GT_STMT);

            fgRemoveStmt(block, stmt);
        }

        // The rest of block has been removed
        // and we will always throw an exception

        // Update succesors of block
        fgRemoveBlockAsPred(block);

        // For compDbgCode, we prepend an empty BB as the firstBB, it is BBJ_NONE.
        // We should not convert it to a ThrowBB.
        if ((block != fgFirstBB) || ((fgFirstBB->bbFlags & BBF_INTERNAL) == 0))
        {
            // Convert block to a throw bb
            fgConvertBBToThrowBB(block);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\n%s Block BB%02u becomes a throw block.\n", msg, block->bbNum);
        }
#endif
        fgRemoveRestOfBlock = false;
    }

    return removedStmt;
}

/*****************************************************************************
 *
 *  Morph the statements of the given block.
 *  This function should be called just once for a block. Use fgMorphBlockStmt()
 *  for reentrant calls.
 */

void Compiler::fgMorphStmts(BasicBlock* block, bool* mult, bool* lnot, bool* loadw)
{
    fgRemoveRestOfBlock = false;

    noway_assert(fgExpandInline == false);

    /* Make the current basic block address available globally */

    compCurBB = block;

    *mult = *lnot = *loadw = false;

    fgCurrentlyInUseArgTemps = hashBv::Create(this);

    GenTreePtr stmt, prev;
    for (stmt = block->bbTreeList, prev = nullptr; stmt; prev = stmt->gtStmt.gtStmtExpr, stmt = stmt->gtNext)
    {
        noway_assert(stmt->gtOper == GT_STMT);

        if (fgRemoveRestOfBlock)
        {
            fgRemoveStmt(block, stmt);
            continue;
        }
#ifdef FEATURE_SIMD
        if (!opts.MinOpts() && stmt->gtStmt.gtStmtExpr->TypeGet() == TYP_FLOAT &&
            stmt->gtStmt.gtStmtExpr->OperGet() == GT_ASG)
        {
            fgMorphCombineSIMDFieldAssignments(block, stmt);
        }
#endif

        fgMorphStmt     = stmt;
        compCurStmt     = stmt;
        GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

#ifdef DEBUG
        compCurStmtNum++;
        if (stmt == block->bbTreeList)
        {
            block->bbStmtNum = compCurStmtNum; // Set the block->bbStmtNum
        }

        unsigned oldHash = verbose ? gtHashValue(tree) : DUMMY_INIT(~0);

        if (verbose)
        {
            printf("\nfgMorphTree BB%02u, stmt %d (before)\n", block->bbNum, compCurStmtNum);
            gtDispTree(tree);
        }
#endif

        /* Morph this statement tree */

        GenTreePtr morph = fgMorphTree(tree);

        // mark any outgoing arg temps as free so we can reuse them in the next statement.

        fgCurrentlyInUseArgTemps->ZeroAll();

        // Has fgMorphStmt been sneakily changed ?

        if (stmt->gtStmt.gtStmtExpr != tree)
        {
            /* This must be tailcall. Ignore 'morph' and carry on with
               the tail-call node */

            morph = stmt->gtStmt.gtStmtExpr;
            noway_assert(compTailCallUsed);
            noway_assert((morph->gtOper == GT_CALL) && morph->AsCall()->IsTailCall());
            noway_assert(stmt->gtNext == nullptr);

            GenTreeCall* call = morph->AsCall();
            // Could either be
            //   - a tail call dispatched via helper in which case block will be ending with BBJ_THROW or
            //   - a fast call made as jmp in which case block will be ending with BBJ_RETURN and marked as containing
            //     a jmp.
            noway_assert((call->IsTailCallViaHelper() && (compCurBB->bbJumpKind == BBJ_THROW)) ||
                         (call->IsFastTailCall() && (compCurBB->bbJumpKind == BBJ_RETURN) &&
                          (compCurBB->bbFlags & BBF_HAS_JMP)));
        }
        else if (block != compCurBB)
        {
            /* This must be a tail call that caused a GCPoll to get
               injected.  We haven't actually morphed the call yet
               but the flag still got set, clear it here...  */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            tree->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif

            noway_assert(compTailCallUsed);
            noway_assert((tree->gtOper == GT_CALL) && tree->AsCall()->IsTailCall());
            noway_assert(stmt->gtNext == nullptr);

            GenTreeCall* call = morph->AsCall();

            // Could either be
            //   - a tail call dispatched via helper in which case block will be ending with BBJ_THROW or
            //   - a fast call made as jmp in which case block will be ending with BBJ_RETURN and marked as containing
            //     a jmp.
            noway_assert((call->IsTailCallViaHelper() && (compCurBB->bbJumpKind == BBJ_THROW)) ||
                         (call->IsFastTailCall() && (compCurBB->bbJumpKind == BBJ_RETURN) &&
                          (compCurBB->bbFlags & BBF_HAS_JMP)));
        }

#ifdef DEBUG
        if (compStressCompile(STRESS_CLONE_EXPR, 30))
        {
            // Clone all the trees to stress gtCloneExpr()

            if (verbose)
            {
                printf("\nfgMorphTree (stressClone from):\n");
                gtDispTree(morph);
            }

            morph = gtCloneExpr(morph);
            noway_assert(morph);

            if (verbose)
            {
                printf("\nfgMorphTree (stressClone to):\n");
                gtDispTree(morph);
            }
        }

        /* If the hash value changes. we modified the tree during morphing */
        if (verbose)
        {
            unsigned newHash = gtHashValue(morph);
            if (newHash != oldHash)
            {
                printf("\nfgMorphTree BB%02u, stmt %d (after)\n", block->bbNum, compCurStmtNum);
                gtDispTree(morph);
            }
        }
#endif

        /* Check for morph as a GT_COMMA with an unconditional throw */
        if (!gtIsActiveCSE_Candidate(morph) && fgIsCommaThrow(morph, true))
        {
            /* Use the call as the new stmt */
            morph = morph->gtOp.gtOp1;
            noway_assert(morph->gtOper == GT_CALL);
            noway_assert((morph->gtFlags & GTF_COLON_COND) == 0);

            fgRemoveRestOfBlock = true;
        }

        stmt->gtStmt.gtStmtExpr = tree = morph;

        noway_assert(fgPtrArgCntCur == 0);

        if (fgRemoveRestOfBlock)
        {
            continue;
        }

        /* Has the statement been optimized away */

        if (fgCheckRemoveStmt(block, stmt))
        {
            continue;
        }

        /* Check if this block ends with a conditional branch that can be folded */

        if (fgFoldConditional(block))
        {
            continue;
        }

        if (ehBlockHasExnFlowDsc(block))
        {
            continue;
        }

#if OPT_MULT_ADDSUB

        /* Note whether we have two or more +=/-= operators in a row */

        if (tree->gtOper == GT_ASG_ADD || tree->gtOper == GT_ASG_SUB)
        {
            if (prev && prev->gtOper == tree->gtOper)
            {
                *mult = true;
            }
        }

#endif

        /* Note "x = a[i] & icon" followed by "x |= a[i] << 8" */

        if (tree->gtOper == GT_ASG_OR && prev && prev->gtOper == GT_ASG)
        {
            *loadw = true;
        }
    }

    if (fgRemoveRestOfBlock)
    {
        if ((block->bbJumpKind == BBJ_COND) || (block->bbJumpKind == BBJ_SWITCH))
        {
            GenTreePtr first = block->bbTreeList;
            noway_assert(first);
            GenTreePtr last = first->gtPrev;
            noway_assert(last && last->gtNext == nullptr);
            GenTreePtr lastStmt = last->gtStmt.gtStmtExpr;

            if (((block->bbJumpKind == BBJ_COND) && (lastStmt->gtOper == GT_JTRUE)) ||
                ((block->bbJumpKind == BBJ_SWITCH) && (lastStmt->gtOper == GT_SWITCH)))
            {
                GenTreePtr op1 = lastStmt->gtOp.gtOp1;

                if (op1->OperKind() & GTK_RELOP)
                {
                    /* Unmark the comparison node with GTF_RELOP_JMP_USED */
                    op1->gtFlags &= ~GTF_RELOP_JMP_USED;
                }

                last->gtStmt.gtStmtExpr = fgMorphTree(op1);
            }
        }

        /* Mark block as a BBJ_THROW block */
        fgConvertBBToThrowBB(block);
    }

    noway_assert(fgExpandInline == false);

#if FEATURE_FASTTAILCALL
    GenTreePtr recursiveTailCall = nullptr;
    if (block->endsWithTailCallConvertibleToLoop(this, &recursiveTailCall))
    {
        fgMorphRecursiveFastTailCallIntoLoop(block, recursiveTailCall->AsCall());
    }
#endif

#ifdef DEBUG
    compCurBB = (BasicBlock*)INVALID_POINTER_VALUE;
#endif

    // Reset this back so that it doesn't leak out impacting other blocks
    fgRemoveRestOfBlock = false;
}

/*****************************************************************************
 *
 *  Morph the blocks of the method.
 *  Returns true if the basic block list is modified.
 *  This function should be called just once.
 */

void Compiler::fgMorphBlocks()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgMorphBlocks()\n");
    }
#endif

    /* Since fgMorphTree can be called after various optimizations to re-arrange
     * the nodes we need a global flag to signal if we are during the one-pass
     * global morphing */

    fgGlobalMorph = true;

#if LOCAL_ASSERTION_PROP
    //
    // Local assertion prop is enabled if we are optimized
    //
    optLocalAssertionProp = (!opts.compDbgCode && !opts.MinOpts());

    if (optLocalAssertionProp)
    {
        //
        // Initialize for local assertion prop
        //
        optAssertionInit(true);
    }
#elif ASSERTION_PROP
    //
    // If LOCAL_ASSERTION_PROP is not set
    // and we have global assertion prop
    // then local assertion prop is always off
    //
    optLocalAssertionProp = false;

#endif

    /*-------------------------------------------------------------------------
     * Process all basic blocks in the function
     */

    BasicBlock* block = fgFirstBB;
    noway_assert(block);

#ifdef DEBUG
    compCurStmtNum = 0;
#endif

    do
    {
#if OPT_MULT_ADDSUB
        bool mult = false;
#endif

#if OPT_BOOL_OPS
        bool lnot = false;
#endif

        bool loadw = false;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nMorphing BB%02u of '%s'\n", block->bbNum, info.compFullName);
        }
#endif

#if LOCAL_ASSERTION_PROP
        if (optLocalAssertionProp)
        {
            //
            // Clear out any currently recorded assertion candidates
            // before processing each basic block,
            // also we must  handle QMARK-COLON specially
            //
            optAssertionReset(0);
        }
#endif

        /* Process all statement trees in the basic block */

        GenTreePtr tree;

        fgMorphStmts(block, &mult, &lnot, &loadw);

#if OPT_MULT_ADDSUB

        if (mult && (opts.compFlags & CLFLG_TREETRANS) && !opts.compDbgCode && !opts.MinOpts())
        {
            for (tree = block->bbTreeList; tree; tree = tree->gtNext)
            {
                noway_assert(tree->gtOper == GT_STMT);
                GenTreePtr last = tree->gtStmt.gtStmtExpr;

                if (last->gtOper == GT_ASG_ADD || last->gtOper == GT_ASG_SUB)
                {
                    GenTreePtr temp;
                    GenTreePtr next;

                    GenTreePtr dst1 = last->gtOp.gtOp1;
                    GenTreePtr src1 = last->gtOp.gtOp2;

                    if (!last->IsCnsIntOrI())
                    {
                        goto NOT_CAFFE;
                    }

                    if (dst1->gtOper != GT_LCL_VAR)
                    {
                        goto NOT_CAFFE;
                    }
                    if (!src1->IsCnsIntOrI())
                    {
                        goto NOT_CAFFE;
                    }

                    for (;;)
                    {
                        GenTreePtr dst2;
                        GenTreePtr src2;

                        /* Look at the next statement */

                        temp = tree->gtNext;
                        if (!temp)
                        {
                            goto NOT_CAFFE;
                        }

                        noway_assert(temp->gtOper == GT_STMT);
                        next = temp->gtStmt.gtStmtExpr;

                        if (next->gtOper != last->gtOper)
                        {
                            goto NOT_CAFFE;
                        }
                        if (next->gtType != last->gtType)
                        {
                            goto NOT_CAFFE;
                        }

                        dst2 = next->gtOp.gtOp1;
                        src2 = next->gtOp.gtOp2;

                        if (dst2->gtOper != GT_LCL_VAR)
                        {
                            goto NOT_CAFFE;
                        }
                        if (dst2->gtLclVarCommon.gtLclNum != dst1->gtLclVarCommon.gtLclNum)
                        {
                            goto NOT_CAFFE;
                        }

                        if (!src2->IsCnsIntOrI())
                        {
                            goto NOT_CAFFE;
                        }

                        if (last->gtOverflow() != next->gtOverflow())
                        {
                            goto NOT_CAFFE;
                        }

                        const ssize_t i1    = src1->gtIntCon.gtIconVal;
                        const ssize_t i2    = src2->gtIntCon.gtIconVal;
                        const ssize_t itemp = i1 + i2;

                        /* if the operators are checking for overflow, check for overflow of the operands */

                        if (next->gtOverflow())
                        {
                            if (next->TypeGet() == TYP_LONG)
                            {
                                if (next->gtFlags & GTF_UNSIGNED)
                                {
                                    ClrSafeInt<UINT64> si1(i1);
                                    if ((si1 + ClrSafeInt<UINT64>(i2)).IsOverflow())
                                    {
                                        goto NOT_CAFFE;
                                    }
                                }
                                else
                                {
                                    ClrSafeInt<INT64> si1(i1);
                                    if ((si1 + ClrSafeInt<INT64>(i2)).IsOverflow())
                                    {
                                        goto NOT_CAFFE;
                                    }
                                }
                            }
                            else if (next->gtFlags & GTF_UNSIGNED)
                            {
                                ClrSafeInt<UINT32> si1(i1);
                                if ((si1 + ClrSafeInt<UINT32>(i2)).IsOverflow())
                                {
                                    goto NOT_CAFFE;
                                }
                            }
                            else
                            {
                                ClrSafeInt<INT32> si1(i1);
                                if ((si1 + ClrSafeInt<INT32>(i2)).IsOverflow())
                                {
                                    goto NOT_CAFFE;
                                }
                            }
                        }

                        /* Fold the two increments/decrements into one */

                        src1->gtIntCon.gtIconVal = itemp;
#ifdef _TARGET_64BIT_
                        if (src1->gtType == TYP_INT)
                        {
                            src1->AsIntCon()->TruncateOrSignExtend32();
                        }
#endif //_TARGET_64BIT_

                        /* Remove the second statement completely */

                        noway_assert(tree->gtNext == temp);
                        noway_assert(temp->gtPrev == tree);

                        if (temp->gtNext)
                        {
                            noway_assert(temp->gtNext->gtPrev == temp);

                            temp->gtNext->gtPrev = tree;
                            tree->gtNext         = temp->gtNext;
                        }
                        else
                        {
                            tree->gtNext = nullptr;

                            noway_assert(block->bbTreeList->gtPrev == temp);

                            block->bbTreeList->gtPrev = tree;
                        }
                    }
                }

            NOT_CAFFE:;
            }
        }

#endif

        /* Are we using a single return block? */

        if (block->bbJumpKind == BBJ_RETURN)
        {
            if ((genReturnBB != nullptr) && (genReturnBB != block) && ((block->bbFlags & BBF_HAS_JMP) == 0))
            {
                /* We'll jump to the genReturnBB */
                CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(_TARGET_X86_)
                if (info.compFlags & CORINFO_FLG_SYNCH)
                {
                    fgConvertSyncReturnToLeave(block);
                }
                else
#endif // !_TARGET_X86_
                {
                    block->bbJumpKind = BBJ_ALWAYS;
                    block->bbJumpDest = genReturnBB;
                    fgReturnCount--;
                }

                // Note 1: A block is not guaranteed to have a last stmt if its jump kind is BBJ_RETURN.
                // For example a method returning void could have an empty block with jump kind BBJ_RETURN.
                // Such blocks do materialize as part of in-lining.
                //
                // Note 2: A block with jump kind BBJ_RETURN does not necessarily need to end with GT_RETURN.
                // It could end with a tail call or rejected tail call or monitor.exit or a GT_INTRINSIC.
                // For now it is safe to explicitly check whether last stmt is GT_RETURN if genReturnLocal
                // is BAD_VAR_NUM.
                //
                // TODO: Need to characterize the last top level stmt of a block ending with BBJ_RETURN.

                GenTreePtr last = (block->bbTreeList != nullptr) ? block->bbTreeList->gtPrev : nullptr;
                GenTreePtr ret  = (last != nullptr) ? last->gtStmt.gtStmtExpr : nullptr;

                // replace the GT_RETURN node to be a GT_ASG that stores the return value into genReturnLocal.
                if (genReturnLocal != BAD_VAR_NUM)
                {
                    // Method must be returning a value other than TYP_VOID.
                    noway_assert(compMethodHasRetVal());

                    // This block must be ending with a GT_RETURN
                    noway_assert(last != nullptr);
                    noway_assert(last->gtOper == GT_STMT);
                    noway_assert(last->gtNext == nullptr);
                    noway_assert(ret != nullptr);

                    // GT_RETURN must have non-null operand as the method is returning the value assigned to
                    // genReturnLocal
                    noway_assert(ret->OperGet() == GT_RETURN);
                    noway_assert(ret->gtGetOp1() != nullptr);
                    noway_assert(ret->gtGetOp2() == nullptr);

                    GenTreePtr tree = gtNewTempAssign(genReturnLocal, ret->gtGetOp1());

                    last->gtStmt.gtStmtExpr = (tree->OperIsCopyBlkOp()) ? fgMorphCopyBlock(tree) : tree;

                    // make sure that copy-prop ignores this assignment.
                    last->gtStmt.gtStmtExpr->gtFlags |= GTF_DONT_CSE;
                }
                else if (ret != nullptr && ret->OperGet() == GT_RETURN)
                {
                    // This block ends with a GT_RETURN
                    noway_assert(last != nullptr);
                    noway_assert(last->gtOper == GT_STMT);
                    noway_assert(last->gtNext == nullptr);

                    // Must be a void GT_RETURN with null operand; delete it as this block branches to oneReturn block
                    noway_assert(ret->TypeGet() == TYP_VOID);
                    noway_assert(ret->gtGetOp1() == nullptr);
                    noway_assert(ret->gtGetOp2() == nullptr);

                    fgRemoveStmt(block, last);
                }

#ifdef DEBUG
                if (verbose)
                {
                    printf("morph BB%02u to point at onereturn.  New block is\n", block->bbNum);
                    fgTableDispBasicBlock(block);
                }
#endif
            }
        }

        block = block->bbNext;
    } while (block);

    /* We are done with the global morphing phase */

    fgGlobalMorph = false;

#ifdef DEBUG
    if (verboseTrees)
    {
        fgDispBasicBlocks(true);
    }
#endif
}

/*****************************************************************************
 *
 *  Make some decisions about the kind of code to generate.
 */

void Compiler::fgSetOptions()
{
#ifdef DEBUG
    /* Should we force fully interruptible code ? */
    if (JitConfig.JitFullyInt() || compStressCompile(STRESS_GENERIC_VARN, 30))
    {
        noway_assert(!codeGen->isGCTypeFixed());
        genInterruptible = true;
    }
#endif

#ifdef DEBUGGING_SUPPORT
    if (opts.compDbgCode)
    {
        assert(!codeGen->isGCTypeFixed());
        genInterruptible = true; // debugging is easier this way ...
    }
#endif

    /* Assume we won't need an explicit stack frame if this is allowed */

    // CORINFO_HELP_TAILCALL won't work with localloc because of the restoring of
    // the callee-saved registers.
    noway_assert(!compTailCallUsed || !compLocallocUsed);

    if (compLocallocUsed)
    {
        codeGen->setFramePointerRequired(true);
    }

#ifdef _TARGET_X86_

    if (compTailCallUsed)
        codeGen->setFramePointerRequired(true);

#endif // _TARGET_X86_

    if (!opts.genFPopt)
    {
        codeGen->setFramePointerRequired(true);
    }

    // Assert that the EH table has been initialized by now. Note that
    // compHndBBtabAllocCount never decreases; it is a high-water mark
    // of table allocation. In contrast, compHndBBtabCount does shrink
    // if we delete a dead EH region, and if it shrinks to zero, the
    // table pointer compHndBBtab is unreliable.
    assert(compHndBBtabAllocCount >= info.compXcptnsCount);

#ifdef _TARGET_X86_

    // Note: this case, and the !X86 case below, should both use the
    // !X86 path. This would require a few more changes for X86 to use
    // compHndBBtabCount (the current number of EH clauses) instead of
    // info.compXcptnsCount (the number of EH clauses in IL), such as
    // in ehNeedsShadowSPslots(). This is because sometimes the IL has
    // an EH clause that we delete as statically dead code before we
    // get here, leaving no EH clauses left, and thus no requirement
    // to use a frame pointer because of EH. But until all the code uses
    // the same test, leave info.compXcptnsCount here.
    if (info.compXcptnsCount > 0)
        codeGen->setFramePointerRequiredEH(true);

#else // !_TARGET_X86_

    if (compHndBBtabCount > 0)
    {
        codeGen->setFramePointerRequiredEH(true);
    }

#endif // _TARGET_X86_

    //  fpPtrArgCntMax records the maximum number of pushed arguments
    //  Depending upon this value of the maximum number of pushed arguments
    //  we may need to use an EBP frame or be partially interuptible
    //

    if (!compCanEncodePtrArgCntMax())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Too many pushed arguments for fully interruptible encoding, marking method as partially "
                   "interruptible\n");
        }
#endif
        genInterruptible = false;
    }
    if (fgPtrArgCntMax >= sizeof(unsigned))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Too many pushed arguments for an ESP based encoding, forcing an EBP frame\n");
        }
#endif
        codeGen->setFramePointerRequiredGCInfo(true);
    }

    if (info.compCallUnmanaged)
    {
        codeGen->setFramePointerRequired(true); // Setup of Pinvoke frame currently requires an EBP style frame
    }

    if (info.compPublishStubParam)
    {
        codeGen->setFramePointerRequiredGCInfo(true);
    }

    if (opts.compNeedSecurityCheck)
    {
        codeGen->setFramePointerRequiredGCInfo(true);

#ifndef JIT32_GCENCODER

        // The decoder only reports objects in frames with exceptions if the frame
        // is fully interruptible.
        // Even if there is no catch or other way to resume execution in this frame
        // the VM requires the security object to remain alive until later, so
        // Frames with security objects must be fully interruptible.
        genInterruptible = true;

#endif // JIT32_GCENCODER
    }

    if (compIsProfilerHookNeeded())
    {
        codeGen->setFramePointerRequired(true);
    }

    if (info.compIsVarArgs)
    {
        // Code that initializes lvaVarargsBaseOfStkArgs requires this to be EBP relative.
        codeGen->setFramePointerRequiredGCInfo(true);
    }

    if (lvaReportParamTypeArg())
    {
        codeGen->setFramePointerRequiredGCInfo(true);
    }

    // printf("method will %s be fully interruptible\n", genInterruptible ? "   " : "not");
}

/*****************************************************************************/

GenTreePtr Compiler::fgInitThisClass()
{
    noway_assert(!compIsForInlining());

    CORINFO_LOOKUP_KIND kind = info.compCompHnd->getLocationOfThisType(info.compMethodHnd);

    if (!kind.needsRuntimeLookup)
    {
        return fgGetSharedCCtor(info.compClassHnd);
    }
    else
    {
        // Collectible types requires that for shared generic code, if we use the generic context paramter
        // that we report it. (This is a conservative approach, we could detect some cases particularly when the
        // context parameter is this that we don't need the eager reporting logic.)
        lvaGenericsContextUsed = true;

        switch (kind.runtimeLookupKind)
        {
            case CORINFO_LOOKUP_THISOBJ:
                // This code takes a this pointer; but we need to pass the static method desc to get the right point in
                // the hierarchy
                {
                    GenTreePtr vtTree = gtNewLclvNode(info.compThisArg, TYP_REF);
                    // Vtable pointer of this object
                    vtTree = gtNewOperNode(GT_IND, TYP_I_IMPL, vtTree);
                    vtTree->gtFlags |= GTF_EXCEPT; // Null-pointer exception
                    GenTreePtr methodHnd = gtNewIconEmbMethHndNode(info.compMethodHnd);

                    return gtNewHelperCallNode(CORINFO_HELP_INITINSTCLASS, TYP_VOID, 0,
                                               gtNewArgList(vtTree, methodHnd));
                }

            case CORINFO_LOOKUP_CLASSPARAM:
            {
                GenTreePtr vtTree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                return gtNewHelperCallNode(CORINFO_HELP_INITCLASS, TYP_VOID, 0, gtNewArgList(vtTree));
            }

            case CORINFO_LOOKUP_METHODPARAM:
            {
                GenTreePtr methHndTree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                return gtNewHelperCallNode(CORINFO_HELP_INITINSTCLASS, TYP_VOID, 0,
                                           gtNewArgList(gtNewIconNode(0), methHndTree));
            }
        }
    }

    noway_assert(!"Unknown LOOKUP_KIND");
    UNREACHABLE();
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  Tree walk callback to make sure no GT_QMARK nodes are present in the tree,
 *  except for the allowed ? 1 : 0; pattern.
 */
Compiler::fgWalkResult Compiler::fgAssertNoQmark(GenTreePtr* tree, fgWalkData* data)
{
    if ((*tree)->OperGet() == GT_QMARK)
    {
        fgCheckQmarkAllowedForm(*tree);
    }
    return WALK_CONTINUE;
}

void Compiler::fgCheckQmarkAllowedForm(GenTree* tree)
{
    assert(tree->OperGet() == GT_QMARK);
#ifndef LEGACY_BACKEND
    assert(!"Qmarks beyond morph disallowed.");
#else  // LEGACY_BACKEND
    GenTreePtr colon = tree->gtOp.gtOp2;

    assert(colon->gtOp.gtOp1->IsIntegralConst(0));
    assert(colon->gtOp.gtOp2->IsIntegralConst(1));
#endif // LEGACY_BACKEND
}

/*****************************************************************************
 *
 *  Verify that the importer has created GT_QMARK nodes in a way we can
 *  process them. The following is allowed:
 *
 *  1. A top level qmark. Top level qmark is of the form:
 *      a) (bool) ? (void) : (void) OR
 *      b) V0N = (bool) ? (type) : (type)
 *
 *  2. Recursion is allowed at the top level, i.e., a GT_QMARK can be a child
 *     of either op1 of colon or op2 of colon but not a child of any other
 *     operator.
 */
void Compiler::fgPreExpandQmarkChecks(GenTreePtr expr)
{
    GenTreePtr topQmark = fgGetTopLevelQmark(expr);

    // If the top level Qmark is null, then scan the tree to make sure
    // there are no qmarks within it.
    if (topQmark == nullptr)
    {
        fgWalkTreePre(&expr, Compiler::fgAssertNoQmark, nullptr);
    }
    else
    {
        // We could probably expand the cond node also, but don't think the extra effort is necessary,
        // so let's just assert the cond node of a top level qmark doesn't have further top level qmarks.
        fgWalkTreePre(&topQmark->gtOp.gtOp1, Compiler::fgAssertNoQmark, nullptr);

        fgPreExpandQmarkChecks(topQmark->gtOp.gtOp2->gtOp.gtOp1);
        fgPreExpandQmarkChecks(topQmark->gtOp.gtOp2->gtOp.gtOp2);
    }
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Get the top level GT_QMARK node in a given "expr", return NULL if such a
 *  node is not present. If the top level GT_QMARK node is assigned to a
 *  GT_LCL_VAR, then return the lcl node in ppDst.
 *
 */
GenTreePtr Compiler::fgGetTopLevelQmark(GenTreePtr expr, GenTreePtr* ppDst /* = NULL */)
{
    if (ppDst != nullptr)
    {
        *ppDst = nullptr;
    }

    GenTreePtr topQmark = nullptr;
    if (expr->gtOper == GT_QMARK)
    {
        topQmark = expr;
    }
    else if (expr->gtOper == GT_ASG && expr->gtOp.gtOp2->gtOper == GT_QMARK && expr->gtOp.gtOp1->gtOper == GT_LCL_VAR)
    {
        topQmark = expr->gtOp.gtOp2;
        if (ppDst != nullptr)
        {
            *ppDst = expr->gtOp.gtOp1;
        }
    }
    return topQmark;
}

/*********************************************************************************
 *
 *  For a castclass helper call,
 *  Importer creates the following tree:
 *      tmp = (op1 == null) ? op1 : ((*op1 == (cse = op2, cse)) ? op1 : helper());
 *
 *  This method splits the qmark expression created by the importer into the
 *  following blocks: (block, asg, cond1, cond2, helper, remainder)
 *  Notice that op1 is the result for both the conditions. So we coalesce these
 *  assignments into a single block instead of two blocks resulting a nested diamond.
 *
 *                       +---------->-----------+
 *                       |          |           |
 *                       ^          ^           v
 *                       |          |           |
 *  block-->asg-->cond1--+-->cond2--+-->helper--+-->remainder
 *
 *  We expect to achieve the following codegen:
 *     mov      rsi, rdx                           tmp = op1                  // asgBlock
 *     test     rsi, rsi                           goto skip if tmp == null ? // cond1Block
 *     je       SKIP
 *     mov      rcx, 0x76543210                    cns = op2                  // cond2Block
 *     cmp      qword ptr [rsi], rcx               goto skip if *tmp == op2
 *     je       SKIP
 *     call     CORINFO_HELP_CHKCASTCLASS_SPECIAL  tmp = helper(cns, tmp)     // helperBlock
 *     mov      rsi, rax
 *  SKIP:                                                                     // remainderBlock
 *     tmp has the result.
 *
 */
void Compiler::fgExpandQmarkForCastInstOf(BasicBlock* block, GenTreePtr stmt)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding CastInstOf qmark in BB%02u (before)\n", block->bbNum);
        fgDispBasicBlocks(block, block, true);
    }
#endif // DEBUG

    GenTreePtr expr = stmt->gtStmt.gtStmtExpr;

    GenTreePtr dst   = nullptr;
    GenTreePtr qmark = fgGetTopLevelQmark(expr, &dst);
    noway_assert(dst != nullptr);

    assert(qmark->gtFlags & GTF_QMARK_CAST_INSTOF);

    // Get cond, true, false exprs for the qmark.
    GenTreePtr condExpr  = qmark->gtGetOp1();
    GenTreePtr trueExpr  = qmark->gtGetOp2()->AsColon()->ThenNode();
    GenTreePtr falseExpr = qmark->gtGetOp2()->AsColon()->ElseNode();

    // Get cond, true, false exprs for the nested qmark.
    GenTreePtr nestedQmark = falseExpr;
    GenTreePtr cond2Expr;
    GenTreePtr true2Expr;
    GenTreePtr false2Expr;

    if (nestedQmark->gtOper == GT_QMARK)
    {
        cond2Expr  = nestedQmark->gtGetOp1();
        true2Expr  = nestedQmark->gtGetOp2()->AsColon()->ThenNode();
        false2Expr = nestedQmark->gtGetOp2()->AsColon()->ElseNode();

        assert(cond2Expr->gtFlags & GTF_RELOP_QMARK);
        cond2Expr->gtFlags &= ~GTF_RELOP_QMARK;
    }
    else
    {
        // This is a rare case that arises when we are doing minopts and encounter isinst of null
        // gtFoldExpr was still is able to optimize away part of the tree (but not all).
        // That means it does not match our pattern.

        // Rather than write code to handle this case, just fake up some nodes to make it match the common
        // case.  Synthesize a comparison that is always true, and for the result-on-true, use the
        // entire subtree we expected to be the nested question op.

        cond2Expr  = gtNewOperNode(GT_EQ, TYP_INT, gtNewIconNode(0, TYP_I_IMPL), gtNewIconNode(0, TYP_I_IMPL));
        true2Expr  = nestedQmark;
        false2Expr = gtNewIconNode(0, TYP_I_IMPL);
    }
    assert(false2Expr->OperGet() == trueExpr->OperGet());

    // Clear flags as they are now going to be part of JTRUE.
    assert(condExpr->gtFlags & GTF_RELOP_QMARK);
    condExpr->gtFlags &= ~GTF_RELOP_QMARK;

    // Create the chain of blocks. See method header comment.
    // The order of blocks after this is the following:
    //     block ... asgBlock ... cond1Block ... cond2Block ... helperBlock ... remainderBlock
    //
    // We need to remember flags that exist on 'block' that we want to propagate to 'remainderBlock',
    // if they are going to be cleared by fgSplitBlockAfterStatement(). We currently only do this only
    // for the GC safe point bit, the logic being that if 'block' was marked gcsafe, then surely
    // remainderBlock will still be GC safe.
    unsigned    propagateFlags = block->bbFlags & BBF_GC_SAFE_POINT;
    BasicBlock* remainderBlock = fgSplitBlockAfterStatement(block, stmt);
    fgRemoveRefPred(remainderBlock, block); // We're going to put more blocks between block and remainderBlock.

    BasicBlock* helperBlock = fgNewBBafter(BBJ_NONE, block, true);
    BasicBlock* cond2Block  = fgNewBBafter(BBJ_COND, block, true);
    BasicBlock* cond1Block  = fgNewBBafter(BBJ_COND, block, true);
    BasicBlock* asgBlock    = fgNewBBafter(BBJ_NONE, block, true);

    remainderBlock->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL | propagateFlags;

    // These blocks are only internal if 'block' is (but they've been set as internal by fgNewBBafter).
    // If they're not internal, mark them as imported to avoid asserts about un-imported blocks.
    if ((block->bbFlags & BBF_INTERNAL) == 0)
    {
        helperBlock->bbFlags &= ~BBF_INTERNAL;
        cond2Block->bbFlags &= ~BBF_INTERNAL;
        cond1Block->bbFlags &= ~BBF_INTERNAL;
        asgBlock->bbFlags &= ~BBF_INTERNAL;
        helperBlock->bbFlags |= BBF_IMPORTED;
        cond2Block->bbFlags |= BBF_IMPORTED;
        cond1Block->bbFlags |= BBF_IMPORTED;
        asgBlock->bbFlags |= BBF_IMPORTED;
    }

    // Chain the flow correctly.
    fgAddRefPred(asgBlock, block);
    fgAddRefPred(cond1Block, asgBlock);
    fgAddRefPred(cond2Block, cond1Block);
    fgAddRefPred(helperBlock, cond2Block);
    fgAddRefPred(remainderBlock, helperBlock);
    fgAddRefPred(remainderBlock, cond1Block);
    fgAddRefPred(remainderBlock, cond2Block);

    cond1Block->bbJumpDest = remainderBlock;
    cond2Block->bbJumpDest = remainderBlock;

    // Set the weights; some are guesses.
    asgBlock->inheritWeight(block);
    cond1Block->inheritWeight(block);
    cond2Block->inheritWeightPercentage(cond1Block, 50);
    helperBlock->inheritWeightPercentage(cond2Block, 50);

    // Append cond1 as JTRUE to cond1Block
    GenTreePtr jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, condExpr);
    GenTreePtr jmpStmt = fgNewStmtFromTree(jmpTree, stmt->gtStmt.gtStmtILoffsx);
    fgInsertStmtAtEnd(cond1Block, jmpStmt);

    // Append cond2 as JTRUE to cond2Block
    jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, cond2Expr);
    jmpStmt = fgNewStmtFromTree(jmpTree, stmt->gtStmt.gtStmtILoffsx);
    fgInsertStmtAtEnd(cond2Block, jmpStmt);

    // AsgBlock should get tmp = op1 assignment.
    trueExpr            = gtNewTempAssign(dst->AsLclVarCommon()->GetLclNum(), trueExpr);
    GenTreePtr trueStmt = fgNewStmtFromTree(trueExpr, stmt->gtStmt.gtStmtILoffsx);
    fgInsertStmtAtEnd(asgBlock, trueStmt);

    // Since we are adding helper in the JTRUE false path, reverse the cond2 and add the helper.
    gtReverseCond(cond2Expr);
    GenTreePtr helperExpr = gtNewTempAssign(dst->AsLclVarCommon()->GetLclNum(), true2Expr);
    GenTreePtr helperStmt = fgNewStmtFromTree(helperExpr, stmt->gtStmt.gtStmtILoffsx);
    fgInsertStmtAtEnd(helperBlock, helperStmt);

    // Finally remove the nested qmark stmt.
    fgRemoveStmt(block, stmt);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding CastInstOf qmark in BB%02u (after)\n", block->bbNum);
        fgDispBasicBlocks(block, remainderBlock, true);
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Expand a statement with a top level qmark node. There are three cases, based
 *  on whether the qmark has both "true" and "false" arms, or just one of them.
 *
 *     S0;
 *     C ? T : F;
 *     S1;
 *
 *     Generates ===>
 *
 *                       bbj_always
 *                       +---->------+
 *                 false |           |
 *     S0 -->-- ~C -->-- T   F -->-- S1
 *              |            |
 *              +--->--------+
 *              bbj_cond(true)
 *
 *     -----------------------------------------
 *
 *     S0;
 *     C ? T : NOP;
 *     S1;
 *
 *     Generates ===>
 *
 *                 false
 *     S0 -->-- ~C -->-- T -->-- S1
 *              |                |
 *              +-->-------------+
 *              bbj_cond(true)
 *
 *     -----------------------------------------
 *
 *     S0;
 *     C ? NOP : F;
 *     S1;
 *
 *     Generates ===>
 *
 *                false
 *     S0 -->-- C -->-- F -->-- S1
 *              |               |
 *              +-->------------+
 *              bbj_cond(true)
 *
 *  If the qmark assigns to a variable, then create tmps for "then"
 *  and "else" results and assign the temp to the variable as a writeback step.
 */
void Compiler::fgExpandQmarkStmt(BasicBlock* block, GenTreePtr stmt)
{
    GenTreePtr expr = stmt->gtStmt.gtStmtExpr;

    // Retrieve the Qmark node to be expanded.
    GenTreePtr dst   = nullptr;
    GenTreePtr qmark = fgGetTopLevelQmark(expr, &dst);
    if (qmark == nullptr)
    {
        return;
    }

    if (qmark->gtFlags & GTF_QMARK_CAST_INSTOF)
    {
        fgExpandQmarkForCastInstOf(block, stmt);
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding top-level qmark in BB%02u (before)\n", block->bbNum);
        fgDispBasicBlocks(block, block, true);
    }
#endif // DEBUG

    // Retrieve the operands.
    GenTreePtr condExpr  = qmark->gtGetOp1();
    GenTreePtr trueExpr  = qmark->gtGetOp2()->AsColon()->ThenNode();
    GenTreePtr falseExpr = qmark->gtGetOp2()->AsColon()->ElseNode();

    assert(condExpr->gtFlags & GTF_RELOP_QMARK);
    condExpr->gtFlags &= ~GTF_RELOP_QMARK;

    assert(!varTypeIsFloating(condExpr->TypeGet()));

    bool hasTrueExpr  = (trueExpr->OperGet() != GT_NOP);
    bool hasFalseExpr = (falseExpr->OperGet() != GT_NOP);
    assert(hasTrueExpr || hasFalseExpr); // We expect to have at least one arm of the qmark!

    // Create remainder, cond and "else" blocks. After this, the blocks are in this order:
    //     block ... condBlock ... elseBlock ... remainderBlock
    //
    // We need to remember flags that exist on 'block' that we want to propagate to 'remainderBlock',
    // if they are going to be cleared by fgSplitBlockAfterStatement(). We currently only do this only
    // for the GC safe point bit, the logic being that if 'block' was marked gcsafe, then surely
    // remainderBlock will still be GC safe.
    unsigned    propagateFlags = block->bbFlags & BBF_GC_SAFE_POINT;
    BasicBlock* remainderBlock = fgSplitBlockAfterStatement(block, stmt);
    fgRemoveRefPred(remainderBlock, block); // We're going to put more blocks between block and remainderBlock.

    BasicBlock* condBlock = fgNewBBafter(BBJ_COND, block, true);
    BasicBlock* elseBlock = fgNewBBafter(BBJ_NONE, condBlock, true);

    // These blocks are only internal if 'block' is (but they've been set as internal by fgNewBBafter).
    // If they're not internal, mark them as imported to avoid asserts about un-imported blocks.
    if ((block->bbFlags & BBF_INTERNAL) == 0)
    {
        condBlock->bbFlags &= ~BBF_INTERNAL;
        elseBlock->bbFlags &= ~BBF_INTERNAL;
        condBlock->bbFlags |= BBF_IMPORTED;
        elseBlock->bbFlags |= BBF_IMPORTED;
    }

    remainderBlock->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL | propagateFlags;

    condBlock->inheritWeight(block);

    fgAddRefPred(condBlock, block);
    fgAddRefPred(elseBlock, condBlock);
    fgAddRefPred(remainderBlock, elseBlock);

    BasicBlock* thenBlock = nullptr;
    if (hasTrueExpr && hasFalseExpr)
    {
        //                       bbj_always
        //                       +---->------+
        //                 false |           |
        //     S0 -->-- ~C -->-- T   F -->-- S1
        //              |            |
        //              +--->--------+
        //              bbj_cond(true)
        //
        gtReverseCond(condExpr);
        condBlock->bbJumpDest = elseBlock;

        thenBlock             = fgNewBBafter(BBJ_ALWAYS, condBlock, true);
        thenBlock->bbJumpDest = remainderBlock;
        if ((block->bbFlags & BBF_INTERNAL) == 0)
        {
            thenBlock->bbFlags &= ~BBF_INTERNAL;
            thenBlock->bbFlags |= BBF_IMPORTED;
        }

        elseBlock->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);

        fgAddRefPred(thenBlock, condBlock);
        fgAddRefPred(remainderBlock, thenBlock);

        thenBlock->inheritWeightPercentage(condBlock, 50);
        elseBlock->inheritWeightPercentage(condBlock, 50);
    }
    else if (hasTrueExpr)
    {
        //                 false
        //     S0 -->-- ~C -->-- T -->-- S1
        //              |                |
        //              +-->-------------+
        //              bbj_cond(true)
        //
        gtReverseCond(condExpr);
        condBlock->bbJumpDest = remainderBlock;
        fgAddRefPred(remainderBlock, condBlock);
        // Since we have no false expr, use the one we'd already created.
        thenBlock = elseBlock;
        elseBlock = nullptr;

        thenBlock->inheritWeightPercentage(condBlock, 50);
    }
    else if (hasFalseExpr)
    {
        //                false
        //     S0 -->-- C -->-- F -->-- S1
        //              |               |
        //              +-->------------+
        //              bbj_cond(true)
        //
        condBlock->bbJumpDest = remainderBlock;
        fgAddRefPred(remainderBlock, condBlock);

        elseBlock->inheritWeightPercentage(condBlock, 50);
    }

    GenTreePtr jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, qmark->gtGetOp1());
    GenTreePtr jmpStmt = fgNewStmtFromTree(jmpTree, stmt->gtStmt.gtStmtILoffsx);
    fgInsertStmtAtEnd(condBlock, jmpStmt);

    // Remove the original qmark statement.
    fgRemoveStmt(block, stmt);

    // Since we have top level qmarks, we either have a dst for it in which case
    // we need to create tmps for true and falseExprs, else just don't bother
    // assigning.
    unsigned lclNum = BAD_VAR_NUM;
    if (dst != nullptr)
    {
        assert(dst->gtOper == GT_LCL_VAR);
        lclNum = dst->gtLclVar.gtLclNum;
    }
    else
    {
        assert(qmark->TypeGet() == TYP_VOID);
    }

    if (hasTrueExpr)
    {
        if (dst != nullptr)
        {
            trueExpr = gtNewTempAssign(lclNum, trueExpr);
        }
        GenTreePtr trueStmt = fgNewStmtFromTree(trueExpr, stmt->gtStmt.gtStmtILoffsx);
        fgInsertStmtAtEnd(thenBlock, trueStmt);
    }

    // Assign the falseExpr into the dst or tmp, insert in elseBlock
    if (hasFalseExpr)
    {
        if (dst != nullptr)
        {
            falseExpr = gtNewTempAssign(lclNum, falseExpr);
        }
        GenTreePtr falseStmt = fgNewStmtFromTree(falseExpr, stmt->gtStmt.gtStmtILoffsx);
        fgInsertStmtAtEnd(elseBlock, falseStmt);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding top-level qmark in BB%02u (after)\n", block->bbNum);
        fgDispBasicBlocks(block, remainderBlock, true);
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Expand GT_QMARK nodes from the flow graph into basic blocks.
 *
 */

void Compiler::fgExpandQmarkNodes()
{
    if (compQmarkUsed)
    {
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            for (GenTreePtr stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
            {
                GenTreePtr expr = stmt->gtStmt.gtStmtExpr;
#ifdef DEBUG
                fgPreExpandQmarkChecks(expr);
#endif
                fgExpandQmarkStmt(block, stmt);
            }
        }
#ifdef DEBUG
        fgPostExpandQmarkChecks();
#endif
    }
    compQmarkRationalized = true;
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  Make sure we don't have any more GT_QMARK nodes.
 *
 */
void Compiler::fgPostExpandQmarkChecks()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        for (GenTreePtr stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            GenTreePtr expr = stmt->gtStmt.gtStmtExpr;
            fgWalkTreePre(&expr, Compiler::fgAssertNoQmark, nullptr);
        }
    }
}
#endif

/*****************************************************************************
 *
 *  Transform all basic blocks for codegen.
 */

void Compiler::fgMorph()
{
    noway_assert(!compIsForInlining()); // Inlinee's compiler should never reach here.

    fgOutgoingArgTemps = nullptr;

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgMorph()\n");
    }
    if (verboseTrees)
    {
        fgDispBasicBlocks(true);
    }
#endif // DEBUG

    // Insert call to class constructor as the first basic block if
    // we were asked to do so.
    if (info.compCompHnd->initClass(nullptr /* field */, info.compMethodHnd /* method */,
                                    impTokenLookupContextHandle /* context */) &
        CORINFO_INITCLASS_USE_HELPER)
    {
        fgEnsureFirstBBisScratch();
        fgInsertStmtAtBeg(fgFirstBB, fgInitThisClass());
    }

#ifdef DEBUG
    if (opts.compGcChecks)
    {
        for (unsigned i = 0; i < info.compArgsCount; i++)
        {
            if (lvaTable[i].TypeGet() == TYP_REF)
            {
                // confirm that the argument is a GC pointer (for debugging (GC stress))
                GenTreePtr      op   = gtNewLclvNode(i, TYP_REF);
                GenTreeArgList* args = gtNewArgList(op);
                op                   = gtNewHelperCallNode(CORINFO_HELP_CHECK_OBJ, TYP_VOID, 0, args);

                fgEnsureFirstBBisScratch();
                fgInsertStmtAtEnd(fgFirstBB, op);
            }
        }
    }

    if (opts.compStackCheckOnRet)
    {
        lvaReturnEspCheck                  = lvaGrabTempWithImplicitUse(false DEBUGARG("ReturnEspCheck"));
        lvaTable[lvaReturnEspCheck].lvType = TYP_INT;
    }

    if (opts.compStackCheckOnCall)
    {
        lvaCallEspCheck                  = lvaGrabTempWithImplicitUse(false DEBUGARG("CallEspCheck"));
        lvaTable[lvaCallEspCheck].lvType = TYP_INT;
    }
#endif // DEBUG

    /* Filter out unimported BBs */

    fgRemoveEmptyBlocks();

    /* Add any internal blocks/trees we may need */

    fgAddInternal();

#if OPT_BOOL_OPS
    fgMultipleNots = false;
#endif

#ifdef DEBUG
    /* Inliner could add basic blocks. Check that the flowgraph data is up-to-date */
    fgDebugCheckBBlist(false, false);
#endif // DEBUG

    /* Inline */
    fgInline();
#if 0
    JITDUMP("trees after inlining\n");
    DBEXEC(VERBOSE, fgDispBasicBlocks(true));
#endif

    RecordStateAtEndOfInlining(); // Record "start" values for post-inlining cycles and elapsed time.

#ifdef DEBUG
    /* Inliner could add basic blocks. Check that the flowgraph data is up-to-date */
    fgDebugCheckBBlist(false, false);
#endif // DEBUG

    /* For x64 and ARM64 we need to mark irregular parameters early so that they don't get promoted */
    fgMarkImplicitByRefArgs();

    /* Promote struct locals if necessary */
    fgPromoteStructs();

    /* Now it is the time to figure out what locals have address-taken. */
    fgMarkAddressExposedLocals();

#ifdef DEBUG
    /* Now that locals have address-taken marked, we can safely apply stress. */
    lvaStressLclFld();
    fgStress64RsltMul();
#endif // DEBUG

    /* Morph the trees in all the blocks of the method */

    fgMorphBlocks();

#if 0
    JITDUMP("trees after fgMorphBlocks\n");
    DBEXEC(VERBOSE, fgDispBasicBlocks(true));
#endif

    /* Decide the kind of code we want to generate */

    fgSetOptions();

    fgExpandQmarkNodes();

#ifdef DEBUG
    compCurBB = nullptr;
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Promoting struct locals
 */
void Compiler::fgPromoteStructs()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgPromoteStructs()\n");
    }
#endif // DEBUG

    if (!opts.OptEnabled(CLFLG_STRUCTPROMOTE))
    {
        return;
    }

    if (fgNoStructPromotion)
    {
        return;
    }

#if 0
    // The code in this #if has been useful in debugging struct promotion issues, by
    // enabling selective enablement of the struct promotion optimization according to
    // method hash.
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("structpromohashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
    }
    char* histr = getenv("structpromohashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
    }
    if (methHash < methHashLo || methHash > methHashHi)
    {
        return;
    }
    else
    {
        printf("Promoting structs for method %s, hash = 0x%x.\n",
               info.compFullName, info.compMethodHash());
        printf("");         // in our logic this causes a flush
    }
#endif // DEBUG
#endif // 0

    if (info.compIsVarArgs)
    {
        return;
    }

    if (getNeedsGSSecurityCookie())
    {
        return;
    }

    // The lvaTable might grow as we grab temps. Make a local copy here.

    unsigned startLvaCount = lvaCount;

    //
    // Loop through the original lvaTable. Looking for struct locals to be promoted.
    //

    lvaStructPromotionInfo structPromotionInfo;
    bool                   tooManyLocals = false;

    for (unsigned lclNum = 0; lclNum < startLvaCount; lclNum++)
    {
        // Whether this var got promoted
        bool       promotedVar = false;
        LclVarDsc* varDsc      = &lvaTable[lclNum];

#ifdef FEATURE_SIMD
        if (varDsc->lvSIMDType && varDsc->lvUsedInSIMDIntrinsic)
        {
            // If we have marked this as lvUsedInSIMDIntrinsic, then we do not want to promote
            // its fields.  Instead, we will attempt to enregister the entire struct.
            varDsc->lvRegStruct = true;
        }
        else
#endif // FEATURE_SIMD
            // Don't promote if we have reached the tracking limit.
            if (lvaHaveManyLocals())
        {
            // Print the message first time when we detected this condition
            if (!tooManyLocals)
            {
                JITDUMP("Stopped promoting struct fields, due to too many locals.\n");
            }
            tooManyLocals = true;
        }
#if !FEATURE_MULTIREG_STRUCT_PROMOTE
        else if (varDsc->lvIsMultiRegArg)
        {
            JITDUMP("Skipping V%02u: marked lvIsMultiRegArg.\n", lclNum);
        }
#endif // !FEATURE_MULTIREG_STRUCT_PROMOTE
        else if (varDsc->lvIsMultiRegRet)
        {
            JITDUMP("Skipping V%02u: marked lvIsMultiRegRet.\n", lclNum);
        }
        else if (varTypeIsStruct(varDsc))
        {
            lvaCanPromoteStructVar(lclNum, &structPromotionInfo);
            bool canPromote = structPromotionInfo.canPromote;

            // We start off with shouldPromote same as canPromote.
            // Based on further profitablity checks done below, shouldPromote
            // could be set to false.
            bool shouldPromote = canPromote;

            if (canPromote)
            {

                // We *can* promote; *should* we promote?
                // We should only do so if promotion has potential savings.  One source of savings
                // is if a field of the struct is accessed, since this access will be turned into
                // an access of the corresponding promoted field variable.  Even if there are no
                // field accesses, but only block-level operations on the whole struct, if the struct
                // has only one or two fields, then doing those block operations field-wise is probably faster
                // than doing a whole-variable block operation (e.g., a hardware "copy loop" on x86).
                // So if no fields are accessed independently, and there are three or more fields,
                // then do not promote.
                if (structPromotionInfo.fieldCnt > 2 && !varDsc->lvFieldAccessed)
                {
                    JITDUMP("Not promoting promotable struct local V%02u: #fields = %d, fieldAccessed = %d.\n", lclNum,
                            structPromotionInfo.fieldCnt, varDsc->lvFieldAccessed);
                    shouldPromote = false;
                }
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
                // TODO-PERF - Only do this when the LclVar is used in an argument context
                // TODO-ARM64 - HFA support should also eliminate the need for this.
                // TODO-LSRA - Currently doesn't support the passing of floating point LCL_VARS in the integer registers
                //
                // For now we currently don't promote structs with a single float field
                // Promoting it can cause us to shuffle it back and forth between the int and
                //  the float regs when it is used as a argument, which is very expensive for XARCH
                //
                else if ((structPromotionInfo.fieldCnt == 1) &&
                         varTypeIsFloating(structPromotionInfo.fields[0].fldType))
                {
                    JITDUMP("Not promoting promotable struct local V%02u: #fields = %d because it is a struct with "
                            "single float field.\n",
                            lclNum, structPromotionInfo.fieldCnt);
                    shouldPromote = false;
                }
#endif // _TARGET_AMD64_ || _TARGET_ARM64_

#if !FEATURE_MULTIREG_STRUCT_PROMOTE
#if defined(_TARGET_ARM64_)
                //
                // For now we currently don't promote structs that are  passed in registers
                //
                else if (lvaIsMultiregStruct(varDsc))
                {
                    JITDUMP("Not promoting promotable multireg struct local V%02u (size==%d): ", lclNum,
                            lvaLclExactSize(lclNum));
                    shouldPromote = false;
                }
#endif // _TARGET_ARM64_
#endif // !FEATURE_MULTIREG_STRUCT_PROMOTE
                else if (varDsc->lvIsParam)
                {
#if FEATURE_MULTIREG_STRUCT_PROMOTE
                    if (lvaIsMultiregStruct(
                            varDsc) && // Is this a variable holding a value that is passed in multiple registers?
                        (structPromotionInfo.fieldCnt != 2)) // Does it have exactly two fields
                    {
                        JITDUMP(
                            "Not promoting multireg struct local V%02u, because lvIsParam is true and #fields != 2\n",
                            lclNum);
                        shouldPromote = false;
                    }
                    else
#endif // !FEATURE_MULTIREG_STRUCT_PROMOTE

                        // TODO-PERF - Implement struct promotion for incoming multireg structs
                        //             Currently it hits assert(lvFieldCnt==1) in lclvar.cpp line 4417

                        if (structPromotionInfo.fieldCnt != 1)
                    {
                        JITDUMP("Not promoting promotable struct local V%02u, because lvIsParam is true and #fields = "
                                "%d.\n",
                                lclNum, structPromotionInfo.fieldCnt);
                        shouldPromote = false;
                    }
                }

                //
                // If the lvRefCnt is zero and we have a struct promoted parameter we can end up with an extra store of
                // the the incoming register into the stack frame slot.
                // In that case, we would like to avoid promortion.
                // However we haven't yet computed the lvRefCnt values so we can't do that.
                //
                CLANG_FORMAT_COMMENT_ANCHOR;

#if 0
                // Often-useful debugging code: if you've narrowed down a struct-promotion problem to a single
                // method, this allows you to select a subset of the vars to promote (by 1-based ordinal number).
                static int structPromoVarNum = 0;
                structPromoVarNum++;
                if (atoi(getenv("structpromovarnumlo")) <= structPromoVarNum && structPromoVarNum <= atoi(getenv("structpromovarnumhi")))
#endif // 0

                if (shouldPromote)
                {
                    assert(canPromote);

                    // Promote the this struct local var.
                    lvaPromoteStructVar(lclNum, &structPromotionInfo);
                    promotedVar = true;

#ifdef _TARGET_ARM_
                    if (structPromotionInfo.requiresScratchVar)
                    {
                        // Ensure that the scratch variable is allocated, in case we
                        // pass a promoted struct as an argument.
                        if (lvaPromotedStructAssemblyScratchVar == BAD_VAR_NUM)
                        {
                            lvaPromotedStructAssemblyScratchVar =
                                lvaGrabTempWithImplicitUse(false DEBUGARG("promoted struct assembly scratch var."));
                            lvaTable[lvaPromotedStructAssemblyScratchVar].lvType = TYP_I_IMPL;
                        }
                    }
#endif // _TARGET_ARM_
                }
            }
        }

#ifdef FEATURE_SIMD
        if (!promotedVar && varDsc->lvSIMDType && !varDsc->lvFieldAccessed)
        {
            // Even if we have not used this in a SIMD intrinsic, if it is not being promoted,
            // we will treat it as a reg struct.
            varDsc->lvRegStruct = true;
        }
#endif // FEATURE_SIMD
    }
}

Compiler::fgWalkResult Compiler::fgMorphStructField(GenTreePtr tree, fgWalkData* fgWalkPre)
{
    noway_assert(tree->OperGet() == GT_FIELD);
    noway_assert(tree->gtFlags & GTF_GLOB_REF);

    GenTreePtr objRef = tree->gtField.gtFldObj;

    /* Is this an instance data member? */

    if (objRef)
    {
        if (objRef->gtOper == GT_ADDR)
        {
            GenTreePtr obj = objRef->gtOp.gtOp1;

            if (obj->gtOper == GT_LCL_VAR)
            {
                unsigned   lclNum = obj->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc = &lvaTable[lclNum];

                if (varTypeIsStruct(obj))
                {
                    if (varDsc->lvPromoted)
                    {
                        // Promoted struct
                        unsigned fldOffset     = tree->gtField.gtFldOffset;
                        unsigned fieldLclIndex = lvaGetFieldLocal(varDsc, fldOffset);
                        noway_assert(fieldLclIndex != BAD_VAR_NUM);

                        tree->SetOper(GT_LCL_VAR);
                        tree->gtLclVarCommon.SetLclNum(fieldLclIndex);
                        tree->gtType = lvaTable[fieldLclIndex].TypeGet();
                        tree->gtFlags &= GTF_NODE_MASK;
                        tree->gtFlags &= ~GTF_GLOB_REF;

                        GenTreePtr parent = fgWalkPre->parentStack->Index(1);
                        if ((parent->gtOper == GT_ASG) && (parent->gtOp.gtOp1 == tree))
                        {
                            tree->gtFlags |= GTF_VAR_DEF;
                            tree->gtFlags |= GTF_DONT_CSE;
                        }
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Replacing the field in promoted struct with a local var:\n");
                            fgWalkPre->printModified = true;
                        }
#endif // DEBUG
                        return WALK_SKIP_SUBTREES;
                    }
                }
                else
                {
                    // Normed struct
                    // A "normed struct" is a struct that the VM tells us is a basic type. This can only happen if
                    // the struct contains a single element, and that element is 4 bytes (on x64 it can also be 8
                    // bytes). Normally, the type of the local var and the type of GT_FIELD are equivalent. However,
                    // there is one extremely rare case where that won't be true. An enum type is a special value type
                    // that contains exactly one element of a primitive integer type (that, for CLS programs is named
                    // "value__"). The VM tells us that a local var of that enum type is the primitive type of the
                    // enum's single field. It turns out that it is legal for IL to access this field using ldflda or
                    // ldfld. For example:
                    //
                    //  .class public auto ansi sealed mynamespace.e_t extends [mscorlib]System.Enum
                    //  {
                    //    .field public specialname rtspecialname int16 value__
                    //    .field public static literal valuetype mynamespace.e_t one = int16(0x0000)
                    //  }
                    //  .method public hidebysig static void  Main() cil managed
                    //  {
                    //     .locals init (valuetype mynamespace.e_t V_0)
                    //     ...
                    //     ldloca.s   V_0
                    //     ldflda     int16 mynamespace.e_t::value__
                    //     ...
                    //  }
                    //
                    // Normally, compilers will not generate the ldflda, since it is superfluous.
                    //
                    // In the example, the lclVar is short, but the JIT promotes all trees using this local to the
                    // "actual type", that is, INT. But the GT_FIELD is still SHORT. So, in the case of a type
                    // mismatch like this, don't do this morphing. The local var may end up getting marked as
                    // address taken, and the appropriate SHORT load will be done from memory in that case.

                    if (tree->TypeGet() == obj->TypeGet())
                    {
                        tree->ChangeOper(GT_LCL_VAR);
                        tree->gtLclVarCommon.SetLclNum(lclNum);
                        tree->gtFlags &= GTF_NODE_MASK;

                        GenTreePtr parent = fgWalkPre->parentStack->Index(1);
                        if ((parent->gtOper == GT_ASG) && (parent->gtOp.gtOp1 == tree))
                        {
                            tree->gtFlags |= GTF_VAR_DEF;
                            tree->gtFlags |= GTF_DONT_CSE;
                        }
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Replacing the field in normed struct with the local var:\n");
                            fgWalkPre->printModified = true;
                        }
#endif // DEBUG
                        return WALK_SKIP_SUBTREES;
                    }
                }
            }
        }
    }

    return WALK_CONTINUE;
}

Compiler::fgWalkResult Compiler::fgMorphLocalField(GenTreePtr tree, fgWalkData* fgWalkPre)
{
    noway_assert(tree->OperGet() == GT_LCL_FLD);

    unsigned   lclNum = tree->gtLclFld.gtLclNum;
    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varTypeIsStruct(varDsc) && (varDsc->lvPromoted))
    {
        // Promoted struct
        unsigned   fldOffset     = tree->gtLclFld.gtLclOffs;
        unsigned   fieldLclIndex = 0;
        LclVarDsc* fldVarDsc     = nullptr;

        if (fldOffset != BAD_VAR_NUM)
        {
            fieldLclIndex = lvaGetFieldLocal(varDsc, fldOffset);
            noway_assert(fieldLclIndex != BAD_VAR_NUM);
            fldVarDsc = &lvaTable[fieldLclIndex];
        }

        if (fldOffset != BAD_VAR_NUM && genTypeSize(fldVarDsc->TypeGet()) == genTypeSize(tree->gtType)
#ifdef _TARGET_X86_
            && varTypeIsFloating(fldVarDsc->TypeGet()) == varTypeIsFloating(tree->gtType)
#endif
                )
        {
            // There is an existing sub-field we can use
            tree->gtLclFld.SetLclNum(fieldLclIndex);

            // We need to keep the types 'compatible'.  If we can switch back to a GT_LCL_VAR
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_ARM_
            assert(varTypeIsIntegralOrI(tree->TypeGet()) || varTypeIsFloating(tree->TypeGet()));
#else
            assert(varTypeIsIntegralOrI(tree->TypeGet()));
#endif
            if (varTypeCanReg(fldVarDsc->TypeGet()))
            {
                // If the type is integer-ish, then we can use it as-is
                tree->ChangeOper(GT_LCL_VAR);
                assert(tree->gtLclVarCommon.gtLclNum == fieldLclIndex);
                tree->gtType = fldVarDsc->TypeGet();
#ifdef DEBUG
                if (verbose)
                {
                    printf("Replacing the GT_LCL_FLD in promoted struct with a local var:\n");
                    fgWalkPre->printModified = true;
                }
#endif // DEBUG
            }

            GenTreePtr parent = fgWalkPre->parentStack->Index(1);
            if ((parent->gtOper == GT_ASG) && (parent->gtOp.gtOp1 == tree))
            {
                tree->gtFlags |= GTF_VAR_DEF;
                tree->gtFlags |= GTF_DONT_CSE;
            }
        }
        else
        {
            // There is no existing field that has all the parts that we need
            // So we must ensure that the struct lives in memory.
            lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LocalField));

#ifdef DEBUG
            // We can't convert this guy to a float because he really does have his
            // address taken..
            varDsc->lvKeepType = 1;
#endif // DEBUG
        }

        return WALK_SKIP_SUBTREES;
    }

    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Mark irregular parameters.  For x64 this is 3, 5, 6, 7, >8 byte structs that are passed by reference.
 *  For ARM64, this is structs larger than 16 bytes that are also not HFAs that are passed by reference.
 */
void Compiler::fgMarkImplicitByRefArgs()
{
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgMarkImplicitByRefs()\n");
    }
#endif // DEBUG

    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = &lvaTable[lclNum];

        assert(!varDsc->lvPromoted); // Called in the wrong order?

        if (varDsc->lvIsParam && varTypeIsStruct(varDsc))
        {
            size_t size;

            if (varDsc->lvSize() > REGSIZE_BYTES)
            {
                size = varDsc->lvSize();
            }
            else
            {
                CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
                size                         = info.compCompHnd->getClassSize(typeHnd);
            }

#if !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#if defined(_TARGET_AMD64_)
            if (size > REGSIZE_BYTES || (size & (size - 1)) != 0)
#elif defined(_TARGET_ARM64_)
            if ((size > TARGET_POINTER_SIZE) && !lvaIsMultiregStruct(varDsc))
#endif
            {
                // Previously nobody was ever setting lvIsParam and lvIsTemp on the same local
                // So I am now using it to indicate that this is one of the weird implicit
                // by ref locals.
                // The address taken cleanup will look for references to locals marked like
                // this, and transform them appropriately.
                varDsc->lvIsTemp = 1;

                // Also marking them as BYREF will hide them from struct promotion.
                varDsc->lvType   = TYP_BYREF;
                varDsc->lvRefCnt = 0;

                // Since this previously was a TYP_STRUCT and we have changed it to a TYP_BYREF
                // make sure that the following flag is not set as these will force SSA to
                // exclude tracking/enregistering these LclVars. (see fgExcludeFromSsa)
                //
                varDsc->lvOverlappingFields = 0; // This flag could have been set, clear it.

#ifdef DEBUG
                // This should not be converted to a double in stress mode,
                // because it is really a pointer
                varDsc->lvKeepType = 1;

                if (verbose)
                {
                    printf("Changing the lvType for struct parameter V%02d to TYP_BYREF.\n", lclNum);
                }
#endif // DEBUG
            }
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING
        }
    }

#endif // _TARGET_AMD64_ || _TARGET_ARM64_
}

/*****************************************************************************
 *
 *  Morph irregular parameters
 *    for x64 and ARM64 this means turning them into byrefs, adding extra indirs.
 */
bool Compiler::fgMorphImplicitByRefArgs(GenTreePtr* pTree, fgWalkData* fgWalkPre)
{
#if !defined(_TARGET_AMD64_) && !defined(_TARGET_ARM64_)

    return false;

#else // _TARGET_AMD64_ || _TARGET_ARM64_

    GenTree* tree = *pTree;
    assert((tree->gtOper == GT_LCL_VAR) || ((tree->gtOper == GT_ADDR) && (tree->gtOp.gtOp1->gtOper == GT_LCL_VAR)));

    bool       isAddr     = (tree->gtOper == GT_ADDR);
    GenTreePtr lclVarTree = isAddr ? tree->gtOp.gtOp1 : tree;
    unsigned   lclNum     = lclVarTree->gtLclVarCommon.gtLclNum;
    LclVarDsc* lclVarDsc  = &lvaTable[lclNum];

    if (!lvaIsImplicitByRefLocal(lclNum))
    {
        // We only need to tranform the 'marked' implicit by ref parameters
        return false;
    }

    // The SIMD transformation to coalesce contiguous references to SIMD vector fields will
    // re-invoke the traversal to mark address-taken locals.
    // So, we may encounter a tree that has already been transformed to TYP_BYREF.
    // If we do, leave it as-is.
    if (!varTypeIsStruct(lclVarTree))
    {
        assert(lclVarTree->TypeGet() == TYP_BYREF);
        return false;
    }

    // We are overloading the lvRefCnt field here because real ref counts have not been set.
    lclVarDsc->lvRefCnt++;

    // This is no longer a def of the lclVar, even if it WAS a def of the struct.
    lclVarTree->gtFlags &= ~(GTF_LIVENESS_MASK);

    if (isAddr)
    {
        // change &X into just plain X
        tree->CopyFrom(lclVarTree, this);
        tree->gtType = TYP_BYREF;

#ifdef DEBUG
        if (verbose)
        {
            printf("Replacing address of implicit by ref struct parameter with byref:\n");
            fgWalkPre->printModified = true;
        }
#endif // DEBUG
    }
    else
    {
        // Change X into *X.
        var_types structType = tree->gtType;
        tree->gtType = TYP_BYREF;
        tree = gtNewOperNode(GT_IND, structType, tree);
        // TODO-CQ: If the VM ever stops violating the ABI and passing heap references
        // we could remove TGTANYWHERE
        tree->gtFlags = ((tree->gtFlags & GTF_COMMON_MASK) | GTF_IND_TGTANYWHERE);

#ifdef DEBUG
        if (verbose)
        {
            printf("Replacing value of implicit by ref struct parameter with indir of parameter:\n");
            gtDispTree(tree, nullptr, nullptr, true);
            fgWalkPre->printModified = true;
        }
#endif // DEBUG
    }

    *pTree = tree;
    return true;

#endif // _TARGET_AMD64_ || _TARGET_ARM64_
}

// An "AddrExposedContext" expresses the calling context in which an address expression occurs.
enum AddrExposedContext
{
    AXC_None,     // None of the below seen yet.
    AXC_Ind,      // The address being computed is to be dereferenced.
    AXC_Addr,     // We're computing a raw address (not dereferenced, at least not immediately).
    AXC_IndWide,  // A block operation dereferenced an address referencing more bytes than the address
                  // addresses -- if the address addresses a field of a struct local, we need to consider
                  // the entire local address taken (not just the field).
    AXC_AddrWide, // The address being computed will be dereferenced by a block operation that operates
                  // on more bytes than the width of the storage location addressed.  If this is a
                  // field of a promoted struct local, declare the entire struct local address-taken.
    AXC_InitBlk,  // An GT_INITBLK is the immediate parent.  The first argument is in an IND context.
    AXC_CopyBlk,  // An GT_COPYBLK is the immediate parent.  The first argument is in a GT_LIST, whose
                  // args should be evaluated in an IND context.
    AXC_IndAdd,   // A GT_ADD is the immediate parent, and it was evaluated in an IND contxt.
                  // If one arg is a constant int, evaluate the other in an IND context.  Otherwise, none.
};

typedef ArrayStack<AddrExposedContext> AXCStack;

// We use pre-post to simulate passing an argument in a recursion, via a stack.
Compiler::fgWalkResult Compiler::fgMarkAddrTakenLocalsPostCB(GenTreePtr* pTree, fgWalkData* fgWalkPre)
{
    AXCStack* axcStack = reinterpret_cast<AXCStack*>(fgWalkPre->pCallbackData);
    (void)axcStack->Pop();
    return WALK_CONTINUE;
}

Compiler::fgWalkResult Compiler::fgMarkAddrTakenLocalsPreCB(GenTreePtr* pTree, fgWalkData* fgWalkPre)
{
    GenTreePtr         tree     = *pTree;
    Compiler*          comp     = fgWalkPre->compiler;
    AXCStack*          axcStack = reinterpret_cast<AXCStack*>(fgWalkPre->pCallbackData);
    AddrExposedContext axc      = axcStack->Top();

    // In some situations, we have to figure out what the effective context is in which to
    // evaluate the current tree, depending on which argument position it is in its parent.

    // If the parent was an initblock, and this is its first argument, we're in
    // and "ind" context.
    switch (axc)
    {
        case AXC_InitBlk:
        case AXC_CopyBlk:
        {
            // In both cases, the second argument is an integer struct size.  That should have a "none" context.
            // The first argument is a GT_LIST.  For GT_COPYBLK, both args of the list are addresses
            // that are dereferenced; for GT_INITBLK, the first is.  We pass "axc" to the GT_LIST;
            // which will pass it to its arguments; these will decide whether they're in an Ind context
            // depending on "axc" and which argument they are.
            // A GT_INITBLK's first argument is a GT_LIST, whose first argument is an address
            // that should be considered to be dereferenced, and whose second argument the integer
            // (byte) value to fill the block with.  The second argument of the GT_INITBLK is also
            // an integer, the block size.
            GenTreePtr parent = fgWalkPre->parentStack->Index(1);
            if (parent->gtOp.gtOp2 == tree && parent->OperIsBlkOp())
            {
                axc = AXC_None;
            }
            else if (parent->OperGet() == GT_LIST)
            {
                genTreeOps axcOper = fgWalkPre->parentStack->Index(2)->OperGet();
                assert((axc == AXC_InitBlk && axcOper == GT_INITBLK) ||
                       (axc == AXC_CopyBlk && GenTree::OperIsCopyBlkOp(axcOper)));

                // The block operation will derefence its argument(s) -- usually.  If the size of the initblk
                // or copyblk exceeds the size of a storage location whose address is used as one of the
                // arguments, then we have to consider that storage location (indeed, it's underlying containing
                // location) to be address taken.  So get the width of the initblk or copyblk.
                GenTreePtr widthNode = fgWalkPre->parentStack->Index(2)->gtOp.gtOp2;
                unsigned   width     = UINT_MAX; // If it's not a constant, assume it's maximally big.
                if (widthNode->IsCnsIntOrI())
                {
                    if (widthNode->IsIconHandle())
                    {
                        // If it's a handle, it must be a class handle.  We only create such block operations
                        // for initialization of struct types, so the type of the argument(s) will match this
                        // type, by construction.  Set the width to zero to make sure nothing fits in it.
                        assert(widthNode->IsIconHandle(GTF_ICON_CLASS_HDL));
                        width = 0;
                    }
                    else
                    {
                        ssize_t swidth = widthNode->gtIntConCommon.IconValue();
                        assert(swidth > 0); // Well-formedness of the block operation node...
                        width = unsigned(swidth);
                    }
                }

                if (parent->gtOp.gtOp1 == tree)
                {
                    // First argument is (potentially) dereferenced by both kinds of block operations.
                    if (tree->OperGet() == GT_ADDR && !comp->fgFitsInOrNotLoc(tree->gtOp.gtOp1, width))
                    {
                        axc = AXC_IndWide;
                    }
                    else
                    {
                        axc = AXC_Ind;
                    }
                }
                else if (axc == AXC_CopyBlk)
                {
                    assert(parent->gtOp.gtOp2 == tree);
                    if (tree->OperGet() == GT_ADDR && !comp->fgFitsInOrNotLoc(tree->gtOp.gtOp1, width))
                    {
                        axc = AXC_IndWide;
                    }
                    else
                    {
                        axc = AXC_Ind;
                    }
                }
                else
                {
                    axc = AXC_None;
                }
            }
        }
        break;

        case AXC_IndAdd:
        {
            GenTreePtr parent = fgWalkPre->parentStack->Index(1);
            assert(parent->OperGet() == GT_ADD);
            // Is one of the args a constant representing a field offset,
            // and is this the other?  If so, Ind context.
            if (parent->gtOp.gtOp1->IsCnsIntOrI() && parent->gtOp.gtOp2 == tree)
            {
                axc = AXC_Ind;
            }
            else if (parent->gtOp.gtOp2->IsCnsIntOrI() && parent->gtOp.gtOp1 == tree)
            {
                axc = AXC_Ind;
            }
            else
            {
                axc = AXC_None;
            }
        }
        break;

        default:
            break;
    }

    // Now recurse properly for the tree.
    switch (tree->gtOper)
    {
        case GT_IND:
        case GT_OBJ:
            if (axc != AXC_Addr)
            {
                axcStack->Push(AXC_Ind);
            }
            else
            {
                axcStack->Push(AXC_None);
            }
            return WALK_CONTINUE;

        case GT_INITBLK:
            axcStack->Push(AXC_InitBlk);
            return WALK_CONTINUE;

        case GT_COPYOBJ:
        case GT_COPYBLK:
            axcStack->Push(AXC_CopyBlk);
            return WALK_CONTINUE;

        case GT_LIST:
            if (axc == AXC_InitBlk || axc == AXC_CopyBlk)
            {
                axcStack->Push(axc);
            }
            else
            {
                axcStack->Push(AXC_None);
            }
            return WALK_CONTINUE;

        case GT_INDEX:
            // Taking the address of an array element never takes the address of a local.
            axcStack->Push(AXC_None);
            return WALK_CONTINUE;

        case GT_ADDR:
            // If we have ADDR(lcl), and "lcl" is an implicit byref parameter, fgMorphImplicitByRefArgs will
            // convert to just "lcl".  This is never an address-context use, since the local is already a
            // byref after this transformation.
            if (tree->gtOp.gtOp1->OperGet() == GT_LCL_VAR && comp->fgMorphImplicitByRefArgs(pTree, fgWalkPre))
            {
                // Push something to keep the PostCB, which will pop it, happy.
                axcStack->Push(AXC_None);
                // In the first case, tree may no longer be a leaf, but we're done with it; is a leaf in the second
                // case.
                return WALK_SKIP_SUBTREES;
            }
#ifdef FEATURE_SIMD
            if (tree->gtOp.gtOp1->OperGet() == GT_SIMD)
            {
                axcStack->Push(AXC_None);
            }
            else
#endif // FEATURE_SIMD
                if (axc == AXC_Ind)
            {
                axcStack->Push(AXC_None);
            }
            else if (axc == AXC_IndWide)
            {
                axcStack->Push(AXC_AddrWide);
            }
            else
            {
                assert(axc == AXC_None);
                axcStack->Push(AXC_Addr);
            }
            return WALK_CONTINUE;

        case GT_FIELD:
            // First, handle a couple of special cases: field of promoted struct local, field
            // of "normed" struct.
            if (comp->fgMorphStructField(tree, fgWalkPre) == WALK_SKIP_SUBTREES)
            {
                // It (may have) replaced the field with a local var or local field.  If we're in an addr context,
                // label it addr-taken.
                if (tree->OperIsLocal() && (axc == AXC_Addr || axc == AXC_AddrWide))
                {
                    unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
                    comp->lvaSetVarAddrExposed(lclNum);
                    if (axc == AXC_AddrWide)
                    {
                        LclVarDsc* varDsc = &comp->lvaTable[lclNum];
                        if (varDsc->lvIsStructField)
                        {
                            comp->lvaSetVarAddrExposed(varDsc->lvParentLcl);
                        }
                    }
                }
                // Push something to keep the PostCB, which will pop it, happy.
                axcStack->Push(AXC_None);
                return WALK_SKIP_SUBTREES;
            }
            else
            {
                // GT_FIELD is an implicit deref.
                if (axc == AXC_Addr)
                {
                    axcStack->Push(AXC_None);
                }
                else if (axc == AXC_AddrWide)
                {
                    axcStack->Push(AXC_IndWide);
                }
                else
                {
                    axcStack->Push(AXC_Ind);
                }
                return WALK_CONTINUE;
            }

        case GT_LCL_FLD:
        {
            assert(axc != AXC_Addr);
            // This recognizes certain forms, and does all the work.  In that case, returns WALK_SKIP_SUBTREES,
            // else WALK_CONTINUE.  We do the same here.
            fgWalkResult res = comp->fgMorphLocalField(tree, fgWalkPre);
            if (res == WALK_SKIP_SUBTREES && tree->OperGet() == GT_LCL_VAR && (axc == AXC_Addr || axc == AXC_AddrWide))
            {
                unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
                comp->lvaSetVarAddrExposed(lclNum);
                if (axc == AXC_AddrWide)
                {
                    LclVarDsc* varDsc = &comp->lvaTable[lclNum];
                    if (varDsc->lvIsStructField)
                    {
                        comp->lvaSetVarAddrExposed(varDsc->lvParentLcl);
                    }
                }
            }
            // Must push something; if res is WALK_SKIP_SUBTREES, doesn't matter
            // what, but something to be popped by the post callback.  If we're going
            // to analyze children, the LCL_FLD creates an Ind context, so use that.
            axcStack->Push(AXC_Ind);
            return res;
        }

        case GT_LCL_VAR:
            // On some architectures, some arguments are passed implicitly by reference.
            // Modify the trees to reflect that, if this local is one of those.
            if (comp->fgMorphImplicitByRefArgs(pTree, fgWalkPre))
            {
                // We can't be in an address context; the ADDR(lcl), where lcl is an implicit byref param, was
                // handled earlier.  (And we can't have added anything to this address, since it was implicit.)
                assert(axc != AXC_Addr);
            }
            else
            {
                if (axc == AXC_Addr || axc == AXC_AddrWide)
                {
                    unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
                    comp->lvaSetVarAddrExposed(lclNum);
                    if (axc == AXC_AddrWide)
                    {
                        LclVarDsc* varDsc = &comp->lvaTable[lclNum];
                        if (varDsc->lvIsStructField)
                        {
                            comp->lvaSetVarAddrExposed(varDsc->lvParentLcl);
                        }
                    }

                    // We may need to Quirk the storage size for this LCL_VAR
                    // some PInvoke signatures incorrectly specify a ByRef to an INT32
                    // when they actually write a SIZE_T or INT64
                    if (axc == AXC_Addr)
                    {
                        comp->gtCheckQuirkAddrExposedLclVar(tree, fgWalkPre->parentStack);
                    }
                }
            }
            // Push something to keep the PostCB, which will pop it, happy.
            axcStack->Push(AXC_None);
            // In the first case, tree may no longer be a leaf, but we're done with it; is a leaf in the second case.
            return WALK_SKIP_SUBTREES;

        case GT_ADD:
            assert(axc != AXC_Addr);
            // See below about treating pointer operations as wider indirection.
            if (tree->gtOp.gtOp1->gtType == TYP_BYREF || tree->gtOp.gtOp2->gtType == TYP_BYREF)
            {
                axcStack->Push(AXC_IndWide);
            }
            else if (axc == AXC_Ind)
            {
                // Let the children know that the parent was a GT_ADD, to be evaluated in an IND context.
                // If it's an add of a constant and an address, and the constant represents a field,
                // then we'll evaluate the address argument in an Ind context; otherwise, the None context.
                axcStack->Push(AXC_IndAdd);
            }
            else
            {
                axcStack->Push(axc);
            }
            return WALK_CONTINUE;

        // !!! Treat Pointer Operations as Wider Indirection
        //
        // If we are performing pointer operations, make sure we treat that as equivalent to a wider
        // indirection. This is because the pointers could be pointing to the address of struct fields
        // and could be used to perform operations on the whole struct or passed to another method.
        //
        // When visiting a node in this pre-order walk, we do not know if we would in the future
        // encounter a GT_ADDR of a GT_FIELD below.
        //
        // Note: GT_ADDR of a GT_FIELD is always a TYP_BYREF.
        // So let us be conservative and treat TYP_BYREF operations as AXC_IndWide and propagate a
        // wider indirection context down the expr tree.
        //
        // Example, in unsafe code,
        //
        //   IL_000e  12 00             ldloca.s     0x0
        //   IL_0010  7c 02 00 00 04    ldflda       0x4000002
        //   IL_0015  12 00             ldloca.s     0x0
        //   IL_0017  7c 01 00 00 04    ldflda       0x4000001
        //   IL_001c  59                sub
        //
        // When visiting the GT_SUB node, if the types of either of the GT_SUB's operand are BYREF, then
        // consider GT_SUB to be equivalent of an AXC_IndWide.
        //
        // Similarly for pointer comparisons and pointer escaping as integers through conversions, treat
        // them as AXC_IndWide.
        //

        // BINOP
        case GT_SUB:
        case GT_MUL:
        case GT_DIV:
        case GT_UDIV:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GT:
        case GT_GE:
        // UNOP
        case GT_CAST:
            if ((tree->gtOp.gtOp1->gtType == TYP_BYREF) ||
                (tree->OperIsBinary() && (tree->gtOp.gtOp2->gtType == TYP_BYREF)))
            {
                axcStack->Push(AXC_IndWide);
                return WALK_CONTINUE;
            }
            __fallthrough;

        default:
            // To be safe/conservative: pass Addr through, but not Ind -- otherwise, revert to "None".  We must
            // handle the "Ind" propogation explicitly above.
            if (axc == AXC_Addr || axc == AXC_AddrWide)
            {
                axcStack->Push(axc);
            }
            else
            {
                axcStack->Push(AXC_None);
            }
            return WALK_CONTINUE;
    }
}

bool Compiler::fgFitsInOrNotLoc(GenTreePtr tree, unsigned width)
{
    if (tree->TypeGet() != TYP_STRUCT)
    {
        return width <= genTypeSize(tree->TypeGet());
    }
    else if (tree->OperGet() == GT_LCL_VAR)
    {
        assert(tree->TypeGet() == TYP_STRUCT);
        unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
        return width <= lvaTable[lclNum].lvExactSize;
    }
    else if (tree->OperGet() == GT_FIELD)
    {
        CORINFO_CLASS_HANDLE fldClass = info.compCompHnd->getFieldClass(tree->gtField.gtFldHnd);
        return width <= info.compCompHnd->getClassSize(fldClass);
    }
    else if (tree->OperGet() == GT_INDEX)
    {
        return width <= tree->gtIndex.gtIndElemSize;
    }
    else
    {
        return false;
    }
}

void Compiler::fgAddFieldSeqForZeroOffset(GenTreePtr op1, FieldSeqNode* fieldSeq)
{
    assert(op1->TypeGet() == TYP_BYREF || op1->TypeGet() == TYP_I_IMPL || op1->TypeGet() == TYP_REF);

    switch (op1->OperGet())
    {
        case GT_ADDR:
            if (op1->gtOp.gtOp1->OperGet() == GT_LCL_FLD)
            {
                GenTreeLclFld* lclFld = op1->gtOp.gtOp1->AsLclFld();
                lclFld->gtFieldSeq    = GetFieldSeqStore()->Append(lclFld->gtFieldSeq, fieldSeq);
            }
            break;

        case GT_ADD:
            if (op1->gtOp.gtOp1->OperGet() == GT_CNS_INT)
            {
                FieldSeqNode* op1Fs = op1->gtOp.gtOp1->gtIntCon.gtFieldSeq;
                if (op1Fs != nullptr)
                {
                    op1Fs                                = GetFieldSeqStore()->Append(op1Fs, fieldSeq);
                    op1->gtOp.gtOp1->gtIntCon.gtFieldSeq = op1Fs;
                }
            }
            else if (op1->gtOp.gtOp2->OperGet() == GT_CNS_INT)
            {
                FieldSeqNode* op2Fs = op1->gtOp.gtOp2->gtIntCon.gtFieldSeq;
                if (op2Fs != nullptr)
                {
                    op2Fs                                = GetFieldSeqStore()->Append(op2Fs, fieldSeq);
                    op1->gtOp.gtOp2->gtIntCon.gtFieldSeq = op2Fs;
                }
            }
            break;

        case GT_CNS_INT:
        {
            FieldSeqNode* op1Fs = op1->gtIntCon.gtFieldSeq;
            if (op1Fs != nullptr)
            {
                op1Fs                    = GetFieldSeqStore()->Append(op1Fs, fieldSeq);
                op1->gtIntCon.gtFieldSeq = op1Fs;
            }
        }
        break;

        default:
            // Record in the general zero-offset map.
            GetZeroOffsetFieldMap()->Set(op1, fieldSeq);
            break;
    }
}

/*****************************************************************************
 *
 *  Mark address-taken locals.
 */

void Compiler::fgMarkAddressExposedLocals()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgMarkAddressExposedLocals()\n");
    }
#endif // DEBUG

    BasicBlock* block = fgFirstBB;
    noway_assert(block);

    do
    {
        /* Make the current basic block address available globally */

        compCurBB = block;

        GenTreePtr stmt;

        for (stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            // Call Compiler::fgMarkAddrTakenLocalsCB on each node
            AXCStack stk(this);
            stk.Push(AXC_None); // We start in neither an addr or ind context.
            fgWalkTree(&stmt->gtStmt.gtStmtExpr, fgMarkAddrTakenLocalsPreCB, fgMarkAddrTakenLocalsPostCB, &stk);
        }

        block = block->bbNext;

    } while (block);
}

// fgNodesMayInterfere:
//   return true if moving nodes relative to each other can change the result of a computation
//
// args:
//   read: a node which reads
//

bool Compiler::fgNodesMayInterfere(GenTree* write, GenTree* read)
{
    LclVarDsc* srcVar = nullptr;

    bool readIsIndir  = read->OperIsIndir() || read->OperIsImplicitIndir();
    bool writeIsIndir = write->OperIsIndir() || write->OperIsImplicitIndir();

    if (read->OperIsLocal())
    {
        srcVar = &lvaTable[read->gtLclVarCommon.gtLclNum];
    }

    if (writeIsIndir)
    {
        if (srcVar && srcVar->lvAddrExposed)
        {
            return true;
        }
        else if (readIsIndir)
        {
            return true;
        }
        return false;
    }
    else if (write->OperIsLocal())
    {
        LclVarDsc* dstVar = &lvaTable[write->gtLclVarCommon.gtLclNum];
        if (readIsIndir)
        {
            return dstVar->lvAddrExposed;
        }
        else if (read->OperIsLocal())
        {
            if (read->gtLclVarCommon.gtLclNum == write->gtLclVarCommon.gtLclNum)
            {
                return true;
            }
            return false;
        }
        else
        {
            return false;
        }
    }
    else
    {
        return false;
    }
}

/** This predicate decides whether we will fold a tree with the structure:
 *  x = x <op> y where x could be any arbitrary expression into
 *  x <op>= y.
 *
 *  This modification is only performed when the target architecture supports
 *  complex addressing modes.  In the case of ARM for example, this transformation
 *  yields no benefit.
 *
 *  In case this functions decides we can proceed to fold into an assignment operator
 *  we need to inspect whether the operator is commutative to tell fgMorph whether we need to
 *  reverse the tree due to the fact we saw x = y <op> x and we want to fold that into
 *  x <op>= y because the operator property.
 */
bool Compiler::fgShouldCreateAssignOp(GenTreePtr tree, bool* bReverse)
{
#if CPU_LOAD_STORE_ARCH
    /* In the case of a load/store architecture, there's no gain by doing any of this, we bail. */
    return false;
#elif !defined(LEGACY_BACKEND)
    return false;
#else  // defined(LEGACY_BACKEND)

    GenTreePtr op1  = tree->gtOp.gtOp1;
    GenTreePtr op2  = tree->gtGetOp2();
    genTreeOps cmop = op2->OperGet();

    /* Is the destination identical to the first RHS sub-operand? */
    if (GenTree::Compare(op1, op2->gtOp.gtOp1))
    {
        /*
        Do not transform the following tree

        [0024CFA4] -----------               const     int    1
        [0024CFDC] ----G------               |         int
        [0024CF5C] -----------               lclVar    ubyte  V01 tmp0
        [0024D05C] -A--G------               =         ubyte
        [0024D014] D------N---               lclVar    ubyte  V01 tmp0

        to

        [0024CFA4] -----------               const     int    1
        [0024D05C] -A--G------               |=        ubyte
        [0024D014] U------N---               lclVar    ubyte  V01 tmp0

        , when V01 is a struct field local.
        */

        if (op1->gtOper == GT_LCL_VAR && varTypeIsSmall(op1->TypeGet()) && op1->TypeGet() != op2->gtOp.gtOp2->TypeGet())
        {
            unsigned   lclNum = op1->gtLclVarCommon.gtLclNum;
            LclVarDsc* varDsc = lvaTable + lclNum;

            if (varDsc->lvIsStructField)
            {
                return false;
            }
        }

        *bReverse = false;
        return true;
    }
    else if (GenTree::OperIsCommutative(cmop))
    {
        /* For commutative ops only, check for "a = x <op> a" */

        /* Should we be doing this at all? */
        if ((opts.compFlags & CLFLG_TREETRANS) == 0)
        {
            return false;
        }

        /* Can we swap the operands to cmop ... */
        if ((op2->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT) && (op2->gtOp.gtOp2->gtFlags & GTF_ALL_EFFECT))
        {
            // Both sides must have side effects to prevent swap */
            return false;
        }

        /* Is the destination identical to the second RHS sub-operand? */
        if (GenTree::Compare(op1, op2->gtOp.gtOp2))
        {
            *bReverse = true;
            return true;
        }
    }
    return false;
#endif // defined(LEGACY_BACKEND)
}

// Static variables.
Compiler::MorphAddrContext Compiler::s_CopyBlockMAC(Compiler::MACK_CopyBlock);

#ifdef FEATURE_SIMD

//-----------------------------------------------------------------------------------
// fgMorphCombineSIMDFieldAssignments:
//  If the RHS of the input stmt is a read for simd vector X Field, then this function
//  will keep reading next few stmts based on the vector size(2, 3, 4).
//  If the next stmts LHS are located contiguous and RHS are also located
//  contiguous, then we replace those statements with a copyblk.
//
// Argument:
//  block - BasicBlock*. block which stmt belongs to
//  stmt  - GenTreeStmt*. the stmt node we want to check
//
// return value:
//  if this funciton successfully optimized the stmts, then return true. Otherwise
//  return false;

bool Compiler::fgMorphCombineSIMDFieldAssignments(BasicBlock* block, GenTreePtr stmt)
{

    noway_assert(stmt->gtOper == GT_STMT);
    GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
    assert(tree->OperGet() == GT_ASG);

    GenTreePtr originalLHS    = tree->gtOp.gtOp1;
    GenTreePtr prevLHS        = tree->gtOp.gtOp1;
    GenTreePtr prevRHS        = tree->gtOp.gtOp2;
    unsigned   index          = 0;
    var_types  baseType       = TYP_UNKNOWN;
    unsigned   simdSize       = 0;
    GenTreePtr simdStructNode = getSIMDStructFromField(prevRHS, &baseType, &index, &simdSize, true);

    if (simdStructNode == nullptr || index != 0 || baseType != TYP_FLOAT)
    {
        // if the RHS is not from a SIMD vector field X, then there is no need to check further.
        return false;
    }

    int        assignmentsCount     = simdSize / genTypeSize(baseType) - 1;
    int        remainingAssignments = assignmentsCount;
    GenTreePtr curStmt              = stmt->gtNext;
    GenTreePtr lastStmt             = stmt;

    while (curStmt != nullptr && remainingAssignments > 0)
    {
        GenTreePtr exp = curStmt->gtStmt.gtStmtExpr;
        if (exp->OperGet() != GT_ASG)
        {
            break;
        }
        GenTreePtr curLHS = exp->gtGetOp1();
        GenTreePtr curRHS = exp->gtGetOp2();

        if (!areArgumentsContiguous(prevLHS, curLHS) || !areArgumentsContiguous(prevRHS, curRHS))
        {
            break;
        }

        remainingAssignments--;
        prevLHS = curLHS;
        prevRHS = curRHS;

        lastStmt = curStmt;
        curStmt  = curStmt->gtNext;
    }

    if (remainingAssignments > 0)
    {
        // if the left assignments number is bigger than zero, then this means
        // that the assignments are not assgining to the contiguously memory
        // locations from same vector.
        return false;
    }
#ifdef DEBUG
    if (verbose)
    {
        printf("\nFound contiguous assignments from a SIMD vector to memory.\n");
        printf("From BB%02u, stmt", block->bbNum);
        printTreeID(stmt);
        printf(" to stmt");
        printTreeID(lastStmt);
        printf("\n");
    }
#endif

    for (int i = 0; i < assignmentsCount; i++)
    {
        fgRemoveStmt(block, stmt->gtNext);
    }

    GenTree* copyBlkDst = createAddressNodeForSIMDInit(originalLHS, simdSize);
    if (simdStructNode->OperIsLocal())
    {
        setLclRelatedToSIMDIntrinsic(simdStructNode);
    }
    GenTree* copyBlkAddr = copyBlkDst;
    if (copyBlkAddr->gtOper == GT_LEA)
    {
        copyBlkAddr = copyBlkAddr->AsAddrMode()->Base();
    }
    GenTreeLclVarCommon* localDst = nullptr;
    if (copyBlkAddr->IsLocalAddrExpr(this, &localDst, nullptr))
    {
        setLclRelatedToSIMDIntrinsic(localDst);
    }

    GenTree* simdStructAddr;
    if (simdStructNode->TypeGet() == TYP_BYREF)
    {
        assert(simdStructNode->OperIsLocal());
        assert(lvaIsImplicitByRefLocal(simdStructNode->AsLclVarCommon()->gtLclNum));
        simdStructAddr = simdStructNode;
    }
    else
    {
        assert(varTypeIsSIMD(simdStructNode));
        simdStructAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, simdStructNode);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBB%02u stmt", block->bbNum);
        printTreeID(stmt);
        printf("(before)\n");
        gtDispTree(stmt);
    }
#endif

    tree = gtNewBlkOpNode(GT_COPYBLK, copyBlkDst, simdStructAddr, gtNewIconNode(simdSize), false);

    stmt->gtStmt.gtStmtExpr = tree;

    // Since we generated a new address node which didn't exist before,
    // we should expose this address manually here.
    AXCStack stk(this);
    stk.Push(AXC_None);
    fgWalkTree(&stmt->gtStmt.gtStmtExpr, fgMarkAddrTakenLocalsPreCB, fgMarkAddrTakenLocalsPostCB, &stk);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nReplaced BB%02u stmt", block->bbNum);
        printTreeID(stmt);
        printf("(after)\n");
        gtDispTree(stmt);
    }
#endif
    return true;
}

#endif // FEATURE_SIMD
