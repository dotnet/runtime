// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
GenTree* Compiler::fgMorphCastIntoHelper(GenTree* tree, int helper, GenTree* oper)
{
    GenTree* result;

    /* If the operand is a constant, we'll try to fold it */
    if (oper->OperIsConst())
    {
        GenTree* oldTree = tree;

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
        noway_assert(tree->AsCast()->CastOp() == oper);
        noway_assert(tree->gtOper == GT_CAST);
    }
    result = fgMorphIntoHelperCall(tree, helper, gtNewCallArgs(oper));
    assert(result == tree);
    return result;
}

/*****************************************************************************
 *
 *  Convert the given node into a call to the specified helper passing
 *  the given argument list.
 */

GenTree* Compiler::fgMorphIntoHelperCall(GenTree* tree, int helper, GenTreeCall::Use* args, bool morphArgs)
{
    // The helper call ought to be semantically equivalent to the original node, so preserve its VN.
    tree->ChangeOper(GT_CALL, GenTree::PRESERVE_VN);

    GenTreeCall* call = tree->AsCall();

    call->gtCallType            = CT_HELPER;
    call->gtCallMethHnd         = eeFindHelper(helper);
    call->gtCallThisArg         = nullptr;
    call->gtCallArgs            = args;
    call->gtCallLateArgs        = nullptr;
    call->fgArgInfo             = nullptr;
    call->gtRetClsHnd           = nullptr;
    call->gtCallMoreFlags       = 0;
    call->gtInlineCandidateInfo = nullptr;
    call->gtControlExpr         = nullptr;

#if DEBUG
    // Helper calls are never candidates.

    call->gtInlineObservation = InlineObservation::CALLSITE_IS_CALL_TO_HELPER;
#endif // DEBUG

#ifdef FEATURE_READYTORUN_COMPILER
    call->gtEntryPoint.addr       = nullptr;
    call->gtEntryPoint.accessType = IAT_VALUE;
#endif

#if FEATURE_MULTIREG_RET
    call->ResetReturnType();
    call->ClearOtherRegs();
    call->ClearOtherRegFlags();
#ifndef TARGET_64BIT
    if (varTypeIsLong(tree))
    {
        call->InitializeLongReturnType();
    }
#endif // !TARGET_64BIT
#endif // FEATURE_MULTIREG_RET

    if (tree->OperMayThrow(this))
    {
        tree->gtFlags |= GTF_EXCEPT;
    }
    else
    {
        tree->gtFlags &= ~GTF_EXCEPT;
    }
    tree->gtFlags |= GTF_CALL;

    for (GenTreeCall::Use& use : GenTreeCall::UseList(args))
    {
        tree->gtFlags |= (use.GetNode()->gtFlags & GTF_ALL_EFFECT);
    }

    /* Perform the morphing */

    if (morphArgs)
    {
        tree = fgMorphArgs(call);
    }

    return tree;
}

/*****************************************************************************
 *
 *  Morph a cast node (we perform some very simple transformations here).
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTree* Compiler::fgMorphCast(GenTree* tree)
{
    noway_assert(tree->gtOper == GT_CAST);
    noway_assert(genTypeSize(TYP_I_IMPL) == TARGET_POINTER_SIZE);

    /* The first sub-operand is the thing being cast */

    GenTree* oper = tree->AsCast()->CastOp();

    if (fgGlobalMorph && (oper->gtOper == GT_ADDR))
    {
        // Make sure we've checked if 'oper' is an address of an implicit-byref parameter.
        // If it is, fgMorphImplicitByRefArgs will change its type, and we want the cast
        // morphing code to see that type.
        fgMorphImplicitByRefArgs(oper);
    }

    var_types srcType = genActualType(oper->TypeGet());

    var_types dstType = tree->CastToType();
    unsigned  dstSize = genTypeSize(dstType);

    // See if the cast has to be done in two steps.  R -> I
    if (varTypeIsFloating(srcType) && varTypeIsIntegral(dstType))
    {
        if (srcType == TYP_FLOAT
#if defined(TARGET_ARM64)
            // Arm64: src = float, dst is overflow conversion.
            // This goes through helper and hence src needs to be converted to double.
            && tree->gtOverflow()
#elif defined(TARGET_AMD64)
            // Amd64: src = float, dst = uint64 or overflow conversion.
            // This goes through helper and hence src needs to be converted to double.
            && (tree->gtOverflow() || (dstType == TYP_ULONG))
#elif defined(TARGET_ARM)
            // Arm: src = float, dst = int64/uint64 or overflow conversion.
            && (tree->gtOverflow() || varTypeIsLong(dstType))
#else
            // x86: src = float, dst = uint32/int64/uint64 or overflow conversion.
            && (tree->gtOverflow() || varTypeIsLong(dstType) || (dstType == TYP_UINT))
#endif
                )
        {
            oper = gtNewCastNode(TYP_DOUBLE, oper, false, TYP_DOUBLE);
        }

        // do we need to do it in two steps R -> I, '-> smallType
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
        if (dstSize < genTypeSize(TYP_INT))
        {
            oper = gtNewCastNodeL(TYP_INT, oper, tree->IsUnsigned(), TYP_INT);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
            tree->gtFlags &= ~GTF_UNSIGNED;
        }
#else
        if (dstSize < TARGET_POINTER_SIZE)
        {
            oper = gtNewCastNodeL(TYP_I_IMPL, oper, false, TYP_I_IMPL);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
        }
#endif
        else
        {
            /* Note that if we need to use a helper call then we can not morph oper */
            if (!tree->gtOverflow())
            {
#ifdef TARGET_ARM64 // On ARM64 All non-overflow checking conversions can be optimized
                goto OPTIMIZECAST;
#else
                switch (dstType)
                {
                    case TYP_INT:
                        goto OPTIMIZECAST;

                    case TYP_UINT:
#if defined(TARGET_ARM) || defined(TARGET_AMD64)
                        goto OPTIMIZECAST;
#else  // TARGET_X86
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2UINT, oper);
#endif // TARGET_X86

                    case TYP_LONG:
#ifdef TARGET_AMD64
                        // SSE2 has instructions to convert a float/double directly to a long
                        goto OPTIMIZECAST;
#else  // !TARGET_AMD64
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2LNG, oper);
#endif // !TARGET_AMD64

                    case TYP_ULONG:
                        return fgMorphCastIntoHelper(tree, CORINFO_HELP_DBL2ULNG, oper);
                    default:
                        break;
                }
#endif // TARGET_ARM64
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
#ifndef TARGET_64BIT
    // The code generation phase (for x86 & ARM32) does not handle casts
    // directly from [u]long to anything other than [u]int. Insert an
    // intermediate cast to native int.
    else if (varTypeIsLong(srcType) && varTypeIsSmall(dstType))
    {
        oper = gtNewCastNode(TYP_I_IMPL, oper, tree->IsUnsigned(), TYP_I_IMPL);
        oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
        tree->gtFlags &= ~GTF_UNSIGNED;
    }
#endif //! TARGET_64BIT

#ifdef TARGET_ARM
    else if ((dstType == TYP_FLOAT) && (srcType == TYP_DOUBLE) && (oper->gtOper == GT_CAST) &&
             !varTypeIsLong(oper->AsCast()->CastOp()))
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

            tree = gtNewCastNode(TYP_FLOAT, tree, false, TYP_FLOAT);

            return fgMorphTree(tree);
        }
        if (tree->gtFlags & GTF_UNSIGNED)
            return fgMorphCastIntoHelper(tree, CORINFO_HELP_ULNG2DBL, oper);
        return fgMorphCastIntoHelper(tree, CORINFO_HELP_LNG2DBL, oper);
    }
#endif // TARGET_ARM

#ifdef TARGET_AMD64
    // Do we have to do two step U4/8 -> R4/8 ?
    // Codegen supports the following conversion as one-step operation
    // a) Long -> R4/R8
    // b) U8 -> R8
    //
    // The following conversions are performed as two-step operations using above.
    // U4 -> R4/8 = U4-> Long -> R4/8
    // U8 -> R4   = U8 -> R8 -> R4
    else if (tree->IsUnsigned() && varTypeIsFloating(dstType))
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
                tree               = gtNewCastNode(TYP_FLOAT, tree, false, TYP_FLOAT);
                return fgMorphTree(tree);
            }
        }
        else if (srcType == TYP_UINT)
        {
            oper = gtNewCastNode(TYP_LONG, oper, true, TYP_LONG);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
            tree->gtFlags &= ~GTF_UNSIGNED;
        }
    }
#endif // TARGET_AMD64

#ifdef TARGET_X86
    // Do we have to do two step U4/8 -> R4/8 ?
    else if (tree->IsUnsigned() && varTypeIsFloating(dstType))
    {
        srcType = genUnsignedType(srcType);

        if (srcType == TYP_ULONG)
        {
            return fgMorphCastIntoHelper(tree, CORINFO_HELP_ULNG2DBL, oper);
        }
        else if (srcType == TYP_UINT)
        {
            oper = gtNewCastNode(TYP_LONG, oper, true, TYP_LONG);
            oper->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));
            tree->gtFlags &= ~GTF_UNSIGNED;
            return fgMorphCastIntoHelper(tree, CORINFO_HELP_LNG2DBL, oper);
        }
    }
    else if (((tree->gtFlags & GTF_UNSIGNED) == 0) && (srcType == TYP_LONG) && varTypeIsFloating(dstType))
    {
        return fgMorphCastIntoHelper(tree, CORINFO_HELP_LNG2DBL, oper);
    }
#endif // TARGET_X86
    else if (varTypeIsGC(srcType) != varTypeIsGC(dstType))
    {
        // We are casting away GC information.  we would like to just
        // change the type to int, however this gives the emitter fits because
        // it believes the variable is a GC variable at the beginning of the
        // instruction group, but is not turned non-gc by the code generator
        // we fix this by copying the GC pointer to a non-gc pointer temp.
        noway_assert(!varTypeIsGC(dstType) && "How can we have a cast to a GCRef here?");

        // We generate an assignment to an int and then do the cast from an int. With this we avoid
        // the gc problem and we allow casts to bytes, longs,  etc...
        unsigned lclNum = lvaGrabTemp(true DEBUGARG("Cast away GC"));
        oper->gtType    = TYP_I_IMPL;
        GenTree* asg    = gtNewTempAssign(lclNum, oper);
        oper->gtType    = srcType;

        // do the real cast
        GenTree* cast = gtNewCastNode(tree->TypeGet(), gtNewLclvNode(lclNum, TYP_I_IMPL), false, dstType);

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
            GenTree* andOp2 = oper->AsOp()->gtOp2;

            // Special case to the special case: AND with a casted int.
            if ((andOp2->OperGet() == GT_CAST) && (andOp2->AsCast()->CastOp()->OperGet() == GT_CNS_INT))
            {
                // gtFoldExprConst will deal with whether the cast is signed or
                // unsigned, or overflow-sensitive.
                andOp2              = gtFoldExprConst(andOp2);
                oper->AsOp()->gtOp2 = andOp2;
            }

            // Look for a constant less than 2^{32} for a cast to uint, or less
            // than 2^{31} for a cast to int.
            int maxWidth = (dstType == TYP_UINT) ? 32 : 31;

            if ((andOp2->OperGet() == GT_CNS_NATIVELONG) && ((andOp2->AsIntConCommon()->LngValue() >> maxWidth) == 0))
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
            // upon the lower 32 bits of the operands.
            //
            bool canPushCast = oper->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_AND, GT_OR, GT_XOR, GT_NOT, GT_NEG);

            // For long LSH cast to int, there is a discontinuity in behavior
            // when the shift amount is 32 or larger.
            //
            // CAST(INT, LSH(1LL, 31)) == LSH(1, 31)
            // LSH(CAST(INT, 1LL), CAST(INT, 31)) == LSH(1, 31)
            //
            // CAST(INT, LSH(1LL, 32)) == 0
            // LSH(CAST(INT, 1LL), CAST(INT, 32)) == LSH(1, 32) == LSH(1, 0) == 1
            //
            // So some extra validation is needed.
            //
            if (oper->OperIs(GT_LSH))
            {
                GenTree* shiftAmount = oper->AsOp()->gtOp2;

                // Expose constant value for shift, if possible, to maximize the number
                // of cases we can handle.
                shiftAmount         = gtFoldExpr(shiftAmount);
                oper->AsOp()->gtOp2 = shiftAmount;

#if DEBUG
                // We may remorph the shift amount tree again later, so clear any morphed flag.
                shiftAmount->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG

                if (shiftAmount->IsIntegralConst())
                {
                    const ssize_t shiftAmountValue = shiftAmount->AsIntCon()->IconValue();

                    if ((shiftAmountValue >= 64) || (shiftAmountValue < 0))
                    {
                        // Shift amount is large enough or negative so result is undefined.
                        // Don't try to optimize.
                        assert(!canPushCast);
                    }
                    else if ((shiftAmountValue >= 32) && ((tree->gtFlags & GTF_ALL_EFFECT) == 0))
                    {
                        // Result of the shift is zero.
                        DEBUG_DESTROY_NODE(tree);
                        GenTree* zero = gtNewZeroConNode(TYP_INT);
                        return fgMorphTree(zero);
                    }
                    else
                    {
                        // Shift amount is positive and small enough that we can push the cast through.
                        canPushCast = true;
                    }
                }
                else
                {
                    // Shift amount is unknown. We can't optimize this case.
                    assert(!canPushCast);
                }
            }

            if (canPushCast)
            {
                DEBUG_DESTROY_NODE(tree);

                // Insert narrowing casts for op1 and op2.
                oper->AsOp()->gtOp1 = gtNewCastNode(TYP_INT, oper->AsOp()->gtOp1, false, dstType);
                if (oper->AsOp()->gtOp2 != nullptr)
                {
                    oper->AsOp()->gtOp2 = gtNewCastNode(TYP_INT, oper->AsOp()->gtOp2, false, dstType);
                }

                // Clear the GT_MUL_64RSLT if it is set.
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
    tree->AsCast()->CastOp() = oper = fgMorphTree(oper);

    /* Reset the call flag */
    tree->gtFlags &= ~GTF_CALL;

    /* Reset the assignment flag */
    tree->gtFlags &= ~GTF_ASG;

    /* unless we have an overflow cast, reset the except flag */
    if (!tree->gtOverflow())
    {
        tree->gtFlags &= ~GTF_EXCEPT;
    }

    /* Just in case new side effects were introduced */
    tree->gtFlags |= (oper->gtFlags & GTF_ALL_EFFECT);

    if (!gtIsActiveCSE_Candidate(tree) && !gtIsActiveCSE_Candidate(oper))
    {
        srcType = oper->TypeGet();

        /* See if we can discard the cast */
        if (varTypeIsIntegral(srcType) && varTypeIsIntegral(dstType))
        {
            if (tree->IsUnsigned() && !varTypeIsUnsigned(srcType))
            {
                if (varTypeIsSmall(srcType))
                {
                    // Small signed values are automatically sign extended to TYP_INT. If the cast is interpreting the
                    // resulting TYP_INT value as unsigned then the "sign" bits end up being "value" bits and srcType
                    // must be TYP_UINT, not the original small signed type. Otherwise "conv.ovf.i2.un(i1(-1))" is
                    // wrongly treated as a widening conversion from i1 to i2 when in fact it is a narrowing conversion
                    // from u4 to i2.
                    srcType = genActualType(srcType);
                }

                srcType = genUnsignedType(srcType);
            }

            if (srcType == dstType)
            { // Certainly if they are identical it is pointless
                goto REMOVE_CAST;
            }

            if (oper->OperGet() == GT_LCL_VAR && varTypeIsSmall(dstType))
            {
                unsigned   varNum = oper->AsLclVarCommon()->GetLclNum();
                LclVarDsc* varDsc = &lvaTable[varNum];
                if (varDsc->TypeGet() == dstType && varDsc->lvNormalizeOnStore())
                {
                    goto REMOVE_CAST;
                }
            }

            bool     unsignedSrc = varTypeIsUnsigned(srcType);
            bool     unsignedDst = varTypeIsUnsigned(dstType);
            bool     signsDiffer = (unsignedSrc != unsignedDst);
            unsigned srcSize     = genTypeSize(srcType);

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
                                // We're changing the type here so we need to update the VN;
                                // in other cases we discard the cast without modifying oper
                                // so the VN doesn't change.
                                oper->SetVNsFromNode(tree);
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
            else if (srcSize < dstSize) // widening cast
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

                // Widening casts from unsigned or to signed can never overflow

                if (unsignedSrc || !unsignedDst)
                {
                    tree->gtFlags &= ~GTF_OVERFLOW;
                    if (!(oper->gtFlags & GTF_EXCEPT))
                    {
                        tree->gtFlags &= ~GTF_EXCEPT;
                    }
                }
            }
            else // if (srcSize > dstSize)
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
                        oper = oper->AsCast()->CastOp();
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
                GenTree* oldTree = tree;

                tree = gtFoldExprConst(tree); // This may not fold the constant (NaN ...)

                // Did we get a comma throw as a result of gtFoldExprConst?
                if ((oldTree != tree) && (oldTree->gtOper != GT_COMMA))
                {
                    noway_assert(fgIsCommaThrow(tree));
                    tree->AsOp()->gtOp1 = fgMorphTree(tree->AsOp()->gtOp1);
                    fgMorphTreeDone(tree);
                    return tree;
                }
                else if (tree->gtOper != GT_CAST)
                {
                    return tree;
                }

                noway_assert(tree->AsCast()->CastOp() == oper); // unchanged
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

            case GT_COMMA:
                // Check for cast of a GT_COMMA with a throw overflow
                // Bug 110829: Since this optimization will bash the types
                // neither oper or commaOp2 can be CSE candidates
                if (fgIsCommaThrow(oper) && !gtIsActiveCSE_Candidate(oper)) // oper can not be a CSE candidate
                {
                    GenTree* commaOp2 = oper->AsOp()->gtOp2;

                    if (!gtIsActiveCSE_Candidate(commaOp2)) // commaOp2 can not be a CSE candidate
                    {
                        // need type of oper to be same as tree
                        if (tree->gtType == TYP_LONG)
                        {
                            commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                            commaOp2->AsIntConCommon()->SetLngValue(0);
                            /* Change the types of oper and commaOp2 to TYP_LONG */
                            oper->gtType = commaOp2->gtType = TYP_LONG;
                        }
                        else if (varTypeIsFloating(tree->gtType))
                        {
                            commaOp2->ChangeOperConst(GT_CNS_DBL);
                            commaOp2->AsDblCon()->gtDconVal = 0.0;
                            // Change the types of oper and commaOp2
                            oper->gtType = commaOp2->gtType = tree->gtType;
                        }
                        else
                        {
                            commaOp2->ChangeOperConst(GT_CNS_INT);
                            commaOp2->AsIntCon()->gtIconVal = 0;
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
        fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW);
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

#ifdef DEBUG
void fgArgTabEntry::Dump()
{
    printf("fgArgTabEntry[arg %u", argNum);
    printf(" %d.%s", GetNode()->gtTreeID, GenTree::OpName(GetNode()->OperGet()));
    printf(" %s", varTypeName(argType));
    printf(" (%s)", passedByRef ? "By ref" : "By value");
    if (GetRegNum() != REG_STK)
    {
        printf(", %u reg%s:", numRegs, numRegs == 1 ? "" : "s");
        for (unsigned i = 0; i < numRegs; i++)
        {
            printf(" %s", getRegName(regNums[i]));
        }
    }
    if (numSlots > 0)
    {
        printf(", numSlots=%u, slotNum=%u", numSlots, slotNum);
    }
    printf(", align=%u", alignment);
    if (isLateArg())
    {
        printf(", lateArgInx=%u", GetLateArgInx());
    }
    if (IsSplit())
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
    if (IsHfaRegArg())
    {
        printf(", isHfa(%s)", varTypeName(GetHfaType()));
    }
    if (isBackFilled)
    {
        printf(", isBackFilled");
    }
    if (isNonStandard)
    {
        printf(", isNonStandard");
    }
    if (isStruct)
    {
        printf(", isStruct");
    }
    printf("]\n");
}
#endif

fgArgInfo::fgArgInfo(Compiler* comp, GenTreeCall* call, unsigned numArgs)
{
    compiler    = comp;
    callTree    = call;
    argCount    = 0; // filled in arg count, starts at zero
    nextSlotNum = INIT_ARG_STACK_SLOT;
    stkLevel    = 0;
#if defined(UNIX_X86_ABI)
    alignmentDone = false;
    stkSizeBytes  = 0;
    padStkAlign   = 0;
#endif
#if FEATURE_FIXED_OUT_ARGS
    outArgSize = 0;
#endif

    argTableSize = numArgs; // the allocated table size

    hasRegArgs   = false;
    hasStackArgs = false;
    argsComplete = false;
    argsSorted   = false;
    needsTemps   = false;

    if (argTableSize == 0)
    {
        argTable = nullptr;
    }
    else
    {
        argTable = new (compiler, CMK_fgArgInfoPtrArr) fgArgTabEntry*[argTableSize];
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
fgArgInfo::fgArgInfo(GenTreeCall* newCall, GenTreeCall* oldCall)
{
    fgArgInfo* oldArgInfo = oldCall->AsCall()->fgArgInfo;

    compiler    = oldArgInfo->compiler;
    callTree    = newCall;
    argCount    = 0; // filled in arg count, starts at zero
    nextSlotNum = INIT_ARG_STACK_SLOT;
    stkLevel    = oldArgInfo->stkLevel;
#if defined(UNIX_X86_ABI)
    alignmentDone = oldArgInfo->alignmentDone;
    stkSizeBytes  = oldArgInfo->stkSizeBytes;
    padStkAlign   = oldArgInfo->padStkAlign;
#endif
#if FEATURE_FIXED_OUT_ARGS
    outArgSize = oldArgInfo->outArgSize;
#endif
    argTableSize = oldArgInfo->argTableSize;
    argsComplete = false;
    argTable     = nullptr;

    assert(oldArgInfo->argsComplete);

    if (argTableSize > 0)
    {
        argTable = new (compiler, CMK_fgArgInfoPtrArr) fgArgTabEntry*[argTableSize];

        // Copy the old arg entries
        for (unsigned i = 0; i < argTableSize; i++)
        {
            argTable[i] = new (compiler, CMK_fgArgInfo) fgArgTabEntry(*oldArgInfo->argTable[i]);
        }

        // The copied arg entries contain pointers to old uses, they need
        // to be updated to point to new uses.
        if (newCall->gtCallThisArg != nullptr)
        {
            for (unsigned i = 0; i < argTableSize; i++)
            {
                if (argTable[i]->use == oldCall->gtCallThisArg)
                {
                    argTable[i]->use = newCall->gtCallThisArg;
                    break;
                }
            }
        }

        GenTreeCall::UseIterator newUse    = newCall->Args().begin();
        GenTreeCall::UseIterator newUseEnd = newCall->Args().end();
        GenTreeCall::UseIterator oldUse    = oldCall->Args().begin();
        GenTreeCall::UseIterator oldUseEnd = newCall->Args().end();

        for (; newUse != newUseEnd; ++newUse, ++oldUse)
        {
            for (unsigned i = 0; i < argTableSize; i++)
            {
                if (argTable[i]->use == oldUse.GetUse())
                {
                    argTable[i]->use = newUse.GetUse();
                    break;
                }
            }
        }

        newUse    = newCall->LateArgs().begin();
        newUseEnd = newCall->LateArgs().end();
        oldUse    = oldCall->LateArgs().begin();
        oldUseEnd = newCall->LateArgs().end();

        for (; newUse != newUseEnd; ++newUse, ++oldUse)
        {
            for (unsigned i = 0; i < argTableSize; i++)
            {
                if (argTable[i]->lateUse == oldUse.GetUse())
                {
                    argTable[i]->lateUse = newUse.GetUse();
                    break;
                }
            }
        }
    }

    argCount     = oldArgInfo->argCount;
    nextSlotNum  = oldArgInfo->nextSlotNum;
    hasRegArgs   = oldArgInfo->hasRegArgs;
    hasStackArgs = oldArgInfo->hasStackArgs;
    argsComplete = true;
    argsSorted   = true;
}

void fgArgInfo::AddArg(fgArgTabEntry* curArgTabEntry)
{
    assert(argCount < argTableSize);
    argTable[argCount] = curArgTabEntry;
    argCount++;
}

fgArgTabEntry* fgArgInfo::AddRegArg(unsigned          argNum,
                                    GenTree*          node,
                                    GenTreeCall::Use* use,
                                    regNumber         regNum,
                                    unsigned          numRegs,
                                    unsigned          alignment,
                                    bool              isStruct,
                                    bool              isVararg /*=false*/)
{
    fgArgTabEntry* curArgTabEntry = new (compiler, CMK_fgArgInfo) fgArgTabEntry;

    // Any additional register numbers are set by the caller.
    // This is primarily because on ARM we don't yet know if it
    // will be split or if it is a double HFA, so the number of registers
    // may actually be less.
    curArgTabEntry->setRegNum(0, regNum);

    curArgTabEntry->argNum    = argNum;
    curArgTabEntry->argType   = node->TypeGet();
    curArgTabEntry->use       = use;
    curArgTabEntry->lateUse   = nullptr;
    curArgTabEntry->slotNum   = 0;
    curArgTabEntry->numRegs   = numRegs;
    curArgTabEntry->numSlots  = 0;
    curArgTabEntry->alignment = alignment;
    curArgTabEntry->SetLateArgInx(UINT_MAX);
    curArgTabEntry->tmpNum = BAD_VAR_NUM;
    curArgTabEntry->SetSplit(false);
    curArgTabEntry->isTmp     = false;
    curArgTabEntry->needTmp   = false;
    curArgTabEntry->needPlace = false;
    curArgTabEntry->processed = false;
#ifdef FEATURE_HFA
    curArgTabEntry->_hfaElemKind = CORINFO_HFA_ELEM_NONE;
#endif
    curArgTabEntry->isBackFilled  = false;
    curArgTabEntry->isNonStandard = false;
    curArgTabEntry->isStruct      = isStruct;
    curArgTabEntry->SetIsVararg(isVararg);

    hasRegArgs = true;
    AddArg(curArgTabEntry);
    return curArgTabEntry;
}

#if defined(UNIX_AMD64_ABI)
fgArgTabEntry* fgArgInfo::AddRegArg(unsigned                                                         argNum,
                                    GenTree*                                                         node,
                                    GenTreeCall::Use*                                                use,
                                    regNumber                                                        regNum,
                                    unsigned                                                         numRegs,
                                    unsigned                                                         alignment,
                                    const bool                                                       isStruct,
                                    const bool                                                       isVararg,
                                    const regNumber                                                  otherRegNum,
                                    const unsigned                                                   structIntRegs,
                                    const unsigned                                                   structFloatRegs,
                                    const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* const structDescPtr)
{
    fgArgTabEntry* curArgTabEntry = AddRegArg(argNum, node, use, regNum, numRegs, alignment, isStruct, isVararg);
    assert(curArgTabEntry != nullptr);

    curArgTabEntry->isStruct        = isStruct; // is this a struct arg
    curArgTabEntry->structIntRegs   = structIntRegs;
    curArgTabEntry->structFloatRegs = structFloatRegs;

    curArgTabEntry->checkIsStruct();
    assert(numRegs <= 2);
    if (numRegs == 2)
    {
        curArgTabEntry->setRegNum(1, otherRegNum);
    }

    if (isStruct && structDescPtr != nullptr)
    {
        curArgTabEntry->structDesc.CopyFrom(*structDescPtr);
    }

    return curArgTabEntry;
}
#endif // defined(UNIX_AMD64_ABI)

fgArgTabEntry* fgArgInfo::AddStkArg(unsigned          argNum,
                                    GenTree*          node,
                                    GenTreeCall::Use* use,
                                    unsigned          numSlots,
                                    unsigned          alignment,
                                    bool              isStruct,
                                    bool              isVararg /*=false*/)
{
    fgArgTabEntry* curArgTabEntry = new (compiler, CMK_fgArgInfo) fgArgTabEntry;

    nextSlotNum = roundUp(nextSlotNum, alignment);

    curArgTabEntry->setRegNum(0, REG_STK);
    curArgTabEntry->argNum  = argNum;
    curArgTabEntry->argType = node->TypeGet();
    curArgTabEntry->use     = use;
    curArgTabEntry->lateUse = nullptr;
    curArgTabEntry->slotNum = nextSlotNum;
    curArgTabEntry->numRegs = 0;
#if defined(UNIX_AMD64_ABI)
    curArgTabEntry->structIntRegs   = 0;
    curArgTabEntry->structFloatRegs = 0;
#endif // defined(UNIX_AMD64_ABI)
    curArgTabEntry->numSlots  = numSlots;
    curArgTabEntry->alignment = alignment;
    curArgTabEntry->SetLateArgInx(UINT_MAX);
    curArgTabEntry->tmpNum = BAD_VAR_NUM;
    curArgTabEntry->SetSplit(false);
    curArgTabEntry->isTmp     = false;
    curArgTabEntry->needTmp   = false;
    curArgTabEntry->needPlace = false;
    curArgTabEntry->processed = false;
#ifdef FEATURE_HFA
    curArgTabEntry->_hfaElemKind = CORINFO_HFA_ELEM_NONE;
#endif
    curArgTabEntry->isBackFilled  = false;
    curArgTabEntry->isNonStandard = false;
    curArgTabEntry->isStruct      = isStruct;
    curArgTabEntry->SetIsVararg(isVararg);

    hasStackArgs = true;
    AddArg(curArgTabEntry);

    nextSlotNum += numSlots;
    return curArgTabEntry;
}

void fgArgInfo::RemorphReset()
{
    nextSlotNum = INIT_ARG_STACK_SLOT;
}

//------------------------------------------------------------------------
// UpdateRegArg: Update the given fgArgTabEntry while morphing.
//
// Arguments:
//    curArgTabEntry - the fgArgTabEntry to update.
//    node           - the tree node that defines the argument
//    reMorphing     - a boolean value indicate whether we are remorphing the call
//
// Assumptions:
//    This must have already been determined to be at least partially passed in registers.
//
void fgArgInfo::UpdateRegArg(fgArgTabEntry* curArgTabEntry, GenTree* node, bool reMorphing)
{
    bool isLateArg = curArgTabEntry->isLateArg();
    // If this is a late arg, we'd better be updating it with a correctly marked node, and vice-versa.
    assert((isLateArg && ((node->gtFlags & GTF_LATE_ARG) != 0)) ||
           (!isLateArg && ((node->gtFlags & GTF_LATE_ARG) == 0)));

    assert(curArgTabEntry->numRegs != 0);
    assert(curArgTabEntry->use->GetNode() == node);
}

//------------------------------------------------------------------------
// UpdateStkArg: Update the given fgArgTabEntry while morphing.
//
// Arguments:
//    curArgTabEntry - the fgArgTabEntry to update.
//    node           - the tree node that defines the argument
//    reMorphing     - a boolean value indicate whether we are remorphing the call
//
// Assumptions:
//    This must have already been determined to be passed on the stack.
//
void fgArgInfo::UpdateStkArg(fgArgTabEntry* curArgTabEntry, GenTree* node, bool reMorphing)
{
    bool isLateArg = curArgTabEntry->isLateArg();
    // If this is a late arg, we'd better be updating it with a correctly marked node, and vice-versa.
    assert((isLateArg && ((node->gtFlags & GTF_LATE_ARG) != 0)) ||
           (!isLateArg && ((node->gtFlags & GTF_LATE_ARG) == 0)));

    noway_assert(curArgTabEntry->use != callTree->gtCallThisArg);
    assert((curArgTabEntry->GetRegNum() == REG_STK) || curArgTabEntry->IsSplit());
    assert(curArgTabEntry->use->GetNode() == node);
    nextSlotNum = (unsigned)roundUp(nextSlotNum, curArgTabEntry->alignment);
    assert(curArgTabEntry->slotNum == nextSlotNum);

    nextSlotNum += curArgTabEntry->numSlots;
}

void fgArgInfo::SplitArg(unsigned argNum, unsigned numRegs, unsigned numSlots)
{
    fgArgTabEntry* curArgTabEntry = nullptr;
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

    if (argsComplete)
    {
        assert(curArgTabEntry->IsSplit() == true);
        assert(curArgTabEntry->numRegs == numRegs);
        assert(curArgTabEntry->numSlots == numSlots);
        assert(hasStackArgs == true);
    }
    else
    {
        curArgTabEntry->SetSplit(true);
        curArgTabEntry->numRegs  = numRegs;
        curArgTabEntry->numSlots = numSlots;
        hasStackArgs             = true;
    }
    nextSlotNum += numSlots;
}

//------------------------------------------------------------------------
// EvalToTmp: Replace the node in the given fgArgTabEntry with a temp
//
// Arguments:
//    curArgTabEntry - the fgArgTabEntry for the argument
//    tmpNum         - the varNum for the temp
//    newNode        - the assignment of the argument value to the temp
//
// Notes:
//    Although the name of this method is EvalToTmp, it doesn't actually create
//    the temp or the copy.
//
void fgArgInfo::EvalToTmp(fgArgTabEntry* curArgTabEntry, unsigned tmpNum, GenTree* newNode)
{
    assert(curArgTabEntry->use != callTree->gtCallThisArg);
    assert(curArgTabEntry->use->GetNode() == newNode);

    assert(curArgTabEntry->GetNode() == newNode);
    curArgTabEntry->tmpNum = tmpNum;
    curArgTabEntry->isTmp  = true;
}

void fgArgInfo::ArgsComplete()
{
    bool hasStructRegArg = false;

    for (unsigned curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntry* curArgTabEntry = argTable[curInx];
        assert(curArgTabEntry != nullptr);
        GenTree* argx = curArgTabEntry->GetNode();

        if (curArgTabEntry->GetRegNum() == REG_STK)
        {
            assert(hasStackArgs == true);
#if !FEATURE_FIXED_OUT_ARGS
            // On x86 we use push instructions to pass arguments:
            //   The non-register arguments are evaluated and pushed in order
            //   and they are never evaluated into temps
            //
            continue;
#endif
        }
#if FEATURE_ARG_SPLIT
        else if (curArgTabEntry->IsSplit())
        {
            hasStructRegArg = true;
            assert(hasStackArgs == true);
        }
#endif       // FEATURE_ARG_SPLIT
        else // we have a register argument, next we look for a struct type.
        {
            if (varTypeIsStruct(argx) UNIX_AMD64_ABI_ONLY(|| curArgTabEntry->isStruct))
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
                needsTemps              = true;
            }

            // For all previous arguments, unless they are a simple constant
            //  we require that they be evaluated into temps
            for (unsigned prevInx = 0; prevInx < curInx; prevInx++)
            {
                fgArgTabEntry* prevArgTabEntry = argTable[prevInx];
                assert(prevArgTabEntry->argNum < curArgTabEntry->argNum);

                // TODO-CQ: We should also allow LCL_VAR_ADDR and LCL_FLD_ADDR here, they're
                // side effect free leaf nodes that like constant can be evaluated at any point.
                if (prevArgTabEntry->GetNode()->gtOper != GT_CNS_INT)
                {
                    prevArgTabEntry->needTmp = true;
                    needsTemps               = true;
                }
            }
        }

        bool treatLikeCall = ((argx->gtFlags & GTF_CALL) != 0);
#if FEATURE_FIXED_OUT_ARGS
        // Like calls, if this argument has a tree that will do an inline throw,
        // a call to a jit helper, then we need to treat it like a call (but only
        // if there are/were any stack args).
        // This means unnesting, sorting, etc.  Technically this is overly
        // conservative, but I want to avoid as much special-case debug-only code
        // as possible, so leveraging the GTF_CALL flag is the easiest.
        //
        if (!treatLikeCall && (argx->gtFlags & GTF_EXCEPT) && (argCount > 1) && compiler->opts.compDbgCode &&
            (compiler->fgWalkTreePre(&argx, Compiler::fgChkThrowCB) == Compiler::WALK_ABORT))
        {
            for (unsigned otherInx = 0; otherInx < argCount; otherInx++)
            {
                if (otherInx == curInx)
                {
                    continue;
                }

                if (argTable[otherInx]->GetRegNum() == REG_STK)
                {
                    treatLikeCall = true;
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

        if (treatLikeCall)
        {
            if (argCount > 1) // If this is not the only argument
            {
                curArgTabEntry->needTmp = true;
                needsTemps              = true;
            }
            else if (varTypeIsFloating(argx->TypeGet()) && (argx->OperGet() == GT_CALL))
            {
                // Spill all arguments that are floating point calls
                curArgTabEntry->needTmp = true;
                needsTemps              = true;
            }

            // All previous arguments may need to be evaluated into temps
            for (unsigned prevInx = 0; prevInx < curInx; prevInx++)
            {
                fgArgTabEntry* prevArgTabEntry = argTable[prevInx];
                assert(prevArgTabEntry->argNum < curArgTabEntry->argNum);

                // For all previous arguments, if they have any GTF_ALL_EFFECT
                //  we require that they be evaluated into a temp
                if ((prevArgTabEntry->GetNode()->gtFlags & GTF_ALL_EFFECT) != 0)
                {
                    prevArgTabEntry->needTmp = true;
                    needsTemps               = true;
                }
#if FEATURE_FIXED_OUT_ARGS
                // Or, if they are stored into the FIXED_OUT_ARG area
                // we require that they be moved to the gtCallLateArgs
                // and replaced with a placeholder node
                else if (prevArgTabEntry->GetRegNum() == REG_STK)
                {
                    prevArgTabEntry->needPlace = true;
                }
#if FEATURE_ARG_SPLIT
                else if (prevArgTabEntry->IsSplit())
                {
                    prevArgTabEntry->needPlace = true;
                }
#endif // TARGET_ARM
#endif
            }
        }

#if FEATURE_MULTIREG_ARGS
        // For RyuJIT backend we will expand a Multireg arg into a GT_FIELD_LIST
        // with multiple indirections, so here we consider spilling it into a tmp LclVar.
        //
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef TARGET_ARM
        bool isMultiRegArg = (curArgTabEntry->numRegs > 0) && (curArgTabEntry->numRegs + curArgTabEntry->numSlots > 1);
#else
        bool isMultiRegArg = (curArgTabEntry->numRegs > 1);
#endif

        if ((varTypeIsStruct(argx->TypeGet())) && (curArgTabEntry->needTmp == false))
        {
            if (isMultiRegArg && ((argx->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
            {
                // Spill multireg struct arguments that have Assignments or Calls embedded in them
                curArgTabEntry->needTmp = true;
                needsTemps              = true;
            }
            else
            {
                // We call gtPrepareCost to measure the cost of evaluating this tree
                compiler->gtPrepareCost(argx);

                if (isMultiRegArg && (argx->GetCostEx() > (6 * IND_COST_EX)))
                {
                    // Spill multireg struct arguments that are expensive to evaluate twice
                    curArgTabEntry->needTmp = true;
                    needsTemps              = true;
                }
#if defined(FEATURE_SIMD) && defined(TARGET_ARM64)
                else if (isMultiRegArg && varTypeIsSIMD(argx->TypeGet()))
                {
                    // SIMD types do not need the optimization below due to their sizes
                    if (argx->OperIsSimdOrHWintrinsic() ||
                        (argx->OperIs(GT_OBJ) && argx->AsObj()->gtOp1->OperIs(GT_ADDR) &&
                         argx->AsObj()->gtOp1->AsOp()->gtOp1->OperIsSimdOrHWintrinsic()))
                    {
                        curArgTabEntry->needTmp = true;
                        needsTemps              = true;
                    }
                }
#endif
#ifndef TARGET_ARM
                // TODO-Arm: This optimization is not implemented for ARM32
                // so we skip this for ARM32 until it is ported to use RyuJIT backend
                //
                else if (argx->OperGet() == GT_OBJ)
                {
                    GenTreeObj* argObj     = argx->AsObj();
                    unsigned    structSize = argObj->GetLayout()->GetSize();
                    switch (structSize)
                    {
                        case 3:
                        case 5:
                        case 6:
                        case 7:
                            // If we have a stack based LclVar we can perform a wider read of 4 or 8 bytes
                            //
                            if (argObj->AsObj()->gtOp1->IsLocalAddrExpr() == nullptr) // Is the source not a LclVar?
                            {
                                // If we don't have a LclVar we need to read exactly 3,5,6 or 7 bytes
                                // For now we use a a GT_CPBLK to copy the exact size into a GT_LCL_VAR temp.
                                //
                                curArgTabEntry->needTmp = true;
                                needsTemps              = true;
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
                            needsTemps              = true;
                            break;

                        default:
                            break;
                    }
                }
#endif // !TARGET_ARM
            }
        }
#endif // FEATURE_MULTIREG_ARGS
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
            fgArgTabEntry* curArgTabEntry = argTable[curInx];
            assert(curArgTabEntry != nullptr);
            GenTree* argx = curArgTabEntry->GetNode();

            // Examine the register args that are currently not marked needTmp
            //
            if (!curArgTabEntry->needTmp && (curArgTabEntry->GetRegNum() != REG_STK))
            {
                if (hasStackArgsWeCareAbout)
                {
#if !FEATURE_FIXED_OUT_ARGS
                    // On x86 we previously recorded a stack depth of zero when
                    // morphing the register arguments of any GT_IND with a GTF_IND_RNGCHK flag
                    // Thus we can not reorder the argument after any stack based argument
                    // (Note that GT_LCLHEAP sets the GTF_EXCEPT flag so we don't need to
                    // check for it explicitly.)
                    //
                    if (argx->gtFlags & GTF_EXCEPT)
                    {
                        curArgTabEntry->needTmp = true;
                        needsTemps              = true;
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
                            needsTemps              = true;
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
                        needsTemps              = true;
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

        fgArgTabEntry* curArgTabEntry = argTable[curInx];

        if (curArgTabEntry->GetRegNum() != REG_STK)
        {
            regCount++;
        }

        assert(curArgTabEntry->lateUse == nullptr);

        // Skip any already processed args
        //
        if (!curArgTabEntry->processed)
        {
            GenTree* argx = curArgTabEntry->GetNode();

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
            fgArgTabEntry* curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTree* argx = curArgTabEntry->GetNode();

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
            fgArgTabEntry* curArgTabEntry = argTable[curInx];

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

            fgArgTabEntry* curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTree* argx = curArgTabEntry->GetNode();

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

        fgArgTabEntry* expensiveArgTabEntry = nullptr;
        unsigned       expensiveArg         = UINT_MAX;
        unsigned       expensiveArgCost     = 0;

        // [We use a forward iterator pattern]
        //
        for (curInx = begTab; curInx <= endTab; curInx++)
        {
            fgArgTabEntry* curArgTabEntry = argTable[curInx];

            // Skip any already processed args
            //
            if (!curArgTabEntry->processed)
            {
                GenTree* argx = curArgTabEntry->GetNode();

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

                    if (argx->GetCostEx() > expensiveArgCost)
                    {
                        // Remember this arg as the most expensive one that we have yet seen
                        expensiveArgCost     = argx->GetCostEx();
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
    callTree->AsCall()->regArgList      = NULL;
    callTree->AsCall()->regArgListCount = regCount;

    unsigned regInx = 0;
    for (curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntry* curArgTabEntry = argTable[curInx];

        if (curArgTabEntry->GetRegNum() != REG_STK)
        {
            // Encode the argument register in the register mask
            //
            callTree->AsCall()->regArgList[regInx] = curArgTabEntry->GetRegNum();
            regInx++;
        }
    }
#endif // !FEATURE_FIXED_OUT_ARGS

    argsSorted = true;
}

#ifdef DEBUG
void fgArgInfo::Dump(Compiler* compiler)
{
    for (unsigned curInx = 0; curInx < ArgCount(); curInx++)
    {
        fgArgTabEntry* curArgEntry = ArgTable()[curInx];
        curArgEntry->Dump();
    }
}
#endif

//------------------------------------------------------------------------------
// fgMakeTmpArgNode : This function creates a tmp var only if needed.
//                    We need this to be done in order to enforce ordering
//                    of the evaluation of arguments.
//
// Arguments:
//    curArgTabEntry
//
// Return Value:
//    the newly created temp var tree.

GenTree* Compiler::fgMakeTmpArgNode(fgArgTabEntry* curArgTabEntry)
{
    unsigned   tmpVarNum = curArgTabEntry->tmpNum;
    LclVarDsc* varDsc    = &lvaTable[tmpVarNum];
    assert(varDsc->lvIsTemp);
    var_types type = varDsc->TypeGet();

    // Create a copy of the temp to go into the late argument list
    GenTree* arg      = gtNewLclvNode(tmpVarNum, type);
    GenTree* addrNode = nullptr;

    if (varTypeIsStruct(type))
    {

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM)

        // Can this type be passed as a primitive type?
        // If so, the following call will return the corresponding primitive type.
        // Otherwise, it will return TYP_UNKNOWN and we will pass it as a struct type.

        bool passedAsPrimitive = false;
        if (curArgTabEntry->isSingleRegOrSlot())
        {
            CORINFO_CLASS_HANDLE clsHnd = varDsc->lvVerTypeInfo.GetClassHandle();
            var_types            structBaseType =
                getPrimitiveTypeForStruct(lvaLclExactSize(tmpVarNum), clsHnd, curArgTabEntry->IsVararg());

            if (structBaseType != TYP_UNKNOWN)
            {
                passedAsPrimitive = true;
#if defined(UNIX_AMD64_ABI)
                // TODO-Cleanup: This is inelegant, but eventually we'll track this in the fgArgTabEntry,
                // and otherwise we'd have to either modify getPrimitiveTypeForStruct() to take
                // a structDesc or call eeGetSystemVAmd64PassStructInRegisterDescriptor yet again.
                //
                if (genIsValidFloatReg(curArgTabEntry->GetRegNum()))
                {
                    if (structBaseType == TYP_INT)
                    {
                        structBaseType = TYP_FLOAT;
                    }
                    else
                    {
                        assert(structBaseType == TYP_LONG);
                        structBaseType = TYP_DOUBLE;
                    }
                }
#endif
                type = structBaseType;
            }
        }

        // If it is passed in registers, don't get the address of the var. Make it a
        // field instead. It will be loaded in registers with putarg_reg tree in lower.
        if (passedAsPrimitive)
        {
            arg->ChangeOper(GT_LCL_FLD);
            arg->gtType = type;
        }
        else
        {
            var_types addrType = TYP_BYREF;
            arg                = gtNewOperNode(GT_ADDR, addrType, arg);
            addrNode           = arg;

#if FEATURE_MULTIREG_ARGS
#ifdef TARGET_ARM64
            assert(varTypeIsStruct(type));
            if (lvaIsMultiregStruct(varDsc, curArgTabEntry->IsVararg()))
            {
                // We will create a GT_OBJ for the argument below.
                // This will be passed by value in two registers.
                assert(addrNode != nullptr);

                // Create an Obj of the temp to use it as a call argument.
                arg = gtNewObjNode(lvaGetStruct(tmpVarNum), arg);
            }
#else
            // Always create an Obj of the temp to use it as a call argument.
            arg = gtNewObjNode(lvaGetStruct(tmpVarNum), arg);
#endif // !TARGET_ARM64
#endif // FEATURE_MULTIREG_ARGS
        }

#else // not (TARGET_AMD64 or TARGET_ARM64 or TARGET_ARM)

        // other targets, we pass the struct by value
        assert(varTypeIsStruct(type));

        addrNode = gtNewOperNode(GT_ADDR, TYP_BYREF, arg);

        // Get a new Obj node temp to use it as a call argument.
        // gtNewObjNode will set the GTF_EXCEPT flag if this is not a local stack object.
        arg = gtNewObjNode(lvaGetStruct(tmpVarNum), addrNode);

#endif // not (TARGET_AMD64 or TARGET_ARM64 or TARGET_ARM)

    } // (varTypeIsStruct(type))

    if (addrNode != nullptr)
    {
        assert(addrNode->gtOper == GT_ADDR);

        // This will prevent this LclVar from being optimized away
        lvaSetVarAddrExposed(tmpVarNum);

        // the child of a GT_ADDR is required to have this flag set
        addrNode->AsOp()->gtOp1->gtFlags |= GTF_DONT_CSE;
    }

    return arg;
}

//------------------------------------------------------------------------------
// EvalArgsToTemps : Create temp assignments and populate the LateArgs list.

void fgArgInfo::EvalArgsToTemps()
{
    assert(argsSorted);

    unsigned regArgInx = 0;
    // Now go through the argument table and perform the necessary evaluation into temps
    GenTreeCall::Use* tmpRegArgNext = nullptr;
    for (unsigned curInx = 0; curInx < argCount; curInx++)
    {
        fgArgTabEntry* curArgTabEntry = argTable[curInx];

        assert(curArgTabEntry->lateUse == nullptr);

        GenTree* argx     = curArgTabEntry->GetNode();
        GenTree* setupArg = nullptr;
        GenTree* defArg;

#if !FEATURE_FIXED_OUT_ARGS
        // Only ever set for FEATURE_FIXED_OUT_ARGS
        assert(curArgTabEntry->needPlace == false);

        // On x86 and other archs that use push instructions to pass arguments:
        //   Only the register arguments need to be replaced with placeholder nodes.
        //   Stacked arguments are evaluated and pushed (or stored into the stack) in order.
        //
        if (curArgTabEntry->GetRegNum() == REG_STK)
            continue;
#endif

        if (curArgTabEntry->needTmp)
        {
            if (curArgTabEntry->isTmp == true)
            {
                // Create a copy of the temp to go into the late argument list
                defArg = compiler->fgMakeTmpArgNode(curArgTabEntry);

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

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
                noway_assert(argx->gtType != TYP_STRUCT);
#endif

                unsigned tmpVarNum = compiler->lvaGrabTemp(true DEBUGARG("argument with side effect"));
                if (argx->gtOper == GT_MKREFANY)
                {
                    // For GT_MKREFANY, typically the actual struct copying does
                    // not have any side-effects and can be delayed. So instead
                    // of using a temp for the whole struct, we can just use a temp
                    // for operand that that has a side-effect
                    GenTree* operand;
                    if ((argx->AsOp()->gtOp2->gtFlags & GTF_ALL_EFFECT) == 0)
                    {
                        operand = argx->AsOp()->gtOp1;

                        // In the early argument evaluation, place an assignment to the temp
                        // from the source operand of the mkrefany
                        setupArg = compiler->gtNewTempAssign(tmpVarNum, operand);

                        // Replace the operand for the mkrefany with the new temp.
                        argx->AsOp()->gtOp1 = compiler->gtNewLclvNode(tmpVarNum, operand->TypeGet());
                    }
                    else if ((argx->AsOp()->gtOp1->gtFlags & GTF_ALL_EFFECT) == 0)
                    {
                        operand = argx->AsOp()->gtOp2;

                        // In the early argument evaluation, place an assignment to the temp
                        // from the source operand of the mkrefany
                        setupArg = compiler->gtNewTempAssign(tmpVarNum, operand);

                        // Replace the operand for the mkrefany with the new temp.
                        argx->AsOp()->gtOp2 = compiler->gtNewLclvNode(tmpVarNum, operand->TypeGet());
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

                    LclVarDsc* varDsc     = compiler->lvaTable + tmpVarNum;
                    var_types  lclVarType = genActualType(argx->gtType);
                    var_types  scalarType = TYP_UNKNOWN;

                    if (setupArg->OperIsCopyBlkOp())
                    {
                        setupArg = compiler->fgMorphCopyBlock(setupArg);
#if defined(TARGET_ARMARCH) || defined(UNIX_AMD64_ABI)
                        if (lclVarType == TYP_STRUCT)
                        {
                            // This scalar LclVar widening step is only performed for ARM architectures.
                            //
                            CORINFO_CLASS_HANDLE clsHnd     = compiler->lvaGetStruct(tmpVarNum);
                            unsigned             structSize = varDsc->lvExactSize;

                            scalarType =
                                compiler->getPrimitiveTypeForStruct(structSize, clsHnd, curArgTabEntry->IsVararg());
                        }
#endif // TARGET_ARMARCH || defined (UNIX_AMD64_ABI)
                    }

                    // scalarType can be set to a wider type for ARM or unix amd64 architectures: (3 => 4)  or (5,6,7 =>
                    // 8)
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

#ifdef TARGET_ARM
                    // Previously we might have thought the local was promoted, and thus the 'COPYBLK'
                    // might have left holes in the used registers (see
                    // fgAddSkippedRegsInPromotedStructArg).
                    // Too bad we're not that smart for these intermediate temps...
                    if (isValidIntArgReg(curArgTabEntry->GetRegNum()) && (curArgTabEntry->numRegs > 1))
                    {
                        regNumber argReg      = curArgTabEntry->GetRegNum();
                        regMaskTP allUsedRegs = genRegMask(curArgTabEntry->GetRegNum());
                        for (unsigned i = 1; i < curArgTabEntry->numRegs; i++)
                        {
                            argReg = genRegArgNext(argReg);
                            allUsedRegs |= genRegMask(argReg);
                        }
                    }
#endif // TARGET_ARM
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
            if ((curArgTabEntry->GetRegNum() == REG_STK) && (curArgTabEntry->needPlace == false))
            {
                continue;
            }

            /* No temp needed - move the whole node to the gtCallLateArgs list */

            /* The argument is deferred and put in the late argument list */

            defArg = argx;

            // Create a placeholder node to put in its place in gtCallLateArgs.

            // For a struct type we also need to record the class handle of the arg.
            CORINFO_CLASS_HANDLE clsHnd = NO_CLASS_HANDLE;

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)

            // All structs are either passed (and retyped) as integral types, OR they
            // are passed by reference.
            noway_assert(argx->gtType != TYP_STRUCT);

#else // !defined(TARGET_AMD64) || defined(UNIX_AMD64_ABI)

            if (defArg->TypeGet() == TYP_STRUCT)
            {
                clsHnd = compiler->gtGetStructHandleIfPresent(defArg);
                noway_assert(clsHnd != NO_CLASS_HANDLE);
            }

#endif // !(defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI))

            setupArg = compiler->gtNewArgPlaceHolderNode(defArg->gtType, clsHnd);

            /* mark the placeholder node as a late argument */
            setupArg->gtFlags |= GTF_LATE_ARG;

#ifdef DEBUG
            if (compiler->verbose)
            {
                if (curArgTabEntry->GetRegNum() == REG_STK)
                {
                    printf("Deferred stack argument :\n");
                }
                else
                {
                    printf("Deferred argument ('%s'):\n", getRegName(curArgTabEntry->GetRegNum()));
                }

                compiler->gtDispTree(argx);
                printf("Replaced with placeholder node:\n");
                compiler->gtDispTree(setupArg);
            }
#endif
        }

        if (setupArg != nullptr)
        {
            noway_assert(curArgTabEntry->use->GetNode() == argx);
            curArgTabEntry->use->SetNode(setupArg);
        }

        /* deferred arg goes into the late argument list */

        if (tmpRegArgNext == nullptr)
        {
            tmpRegArgNext                      = compiler->gtNewCallArgs(defArg);
            callTree->AsCall()->gtCallLateArgs = tmpRegArgNext;
        }
        else
        {
            noway_assert(tmpRegArgNext->GetNode() != nullptr);
            tmpRegArgNext->SetNext(compiler->gtNewCallArgs(defArg));

            tmpRegArgNext = tmpRegArgNext->GetNext();
        }

        curArgTabEntry->lateUse = tmpRegArgNext;
        curArgTabEntry->SetLateArgInx(regArgInx++);
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\nShuffled argument table:    ");
        for (unsigned curInx = 0; curInx < argCount; curInx++)
        {
            fgArgTabEntry* curArgTabEntry = argTable[curInx];

            if (curArgTabEntry->GetRegNum() != REG_STK)
            {
                printf("%s ", getRegName(curArgTabEntry->GetRegNum()));
            }
        }
        printf("\n");
    }
#endif
}

// Return a conservative estimate of the stack size in bytes.
// It will be used only on the intercepted-for-host code path to copy the arguments.
int Compiler::fgEstimateCallStackSize(GenTreeCall* call)
{
    int numArgs = 0;
    for (GenTreeCall::Use& use : call->Args())
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
// fgMakeMultiUse : If the node is a local, clone it, otherwise insert a comma form temp
//
// Arguments:
//    ppTree  - a pointer to the child node we will be replacing with the comma expression that
//              evaluates ppTree to a temp and returns the result
//
// Return Value:
//    A fresh GT_LCL_VAR node referencing the temp which has not been used

GenTree* Compiler::fgMakeMultiUse(GenTree** pOp)
{
    GenTree* tree = *pOp;
    if (tree->IsLocal())
    {
        return gtClone(tree);
    }
    else
    {
        return fgInsertCommaFormTemp(pOp);
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

    GenTree* load = new (this, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, subTree->TypeGet(), lclNum);

    GenTree* comma = gtNewOperNode(GT_COMMA, subTree->TypeGet(), asg, load);

    *ppTree = comma;

    return new (this, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, subTree->TypeGet(), lclNum);
}

//------------------------------------------------------------------------
// fgInitArgInfo: Construct the fgArgInfo for the call with the fgArgEntry for each arg
//
// Arguments:
//    callNode - the call for which we are generating the fgArgInfo
//
// Return Value:
//    None
//
// Notes:
//    This method is idempotent in that it checks whether the fgArgInfo has already been
//    constructed, and just returns.
//    This method only computes the arg table and arg entries for the call (the fgArgInfo),
//    and makes no modification of the args themselves.
//
void Compiler::fgInitArgInfo(GenTreeCall* call)
{
    GenTreeCall::Use* args;
    GenTree*          argx;

    unsigned argIndex     = 0;
    unsigned intArgRegNum = 0;
    unsigned fltArgRegNum = 0;
    unsigned argSlots     = 0;

    bool callHasRetBuffArg = call->HasRetBufArg();
    bool callIsVararg      = call->IsVarargs();

#ifdef TARGET_ARM
    regMaskTP argSkippedRegMask    = RBM_NONE;
    regMaskTP fltArgSkippedRegMask = RBM_NONE;
#endif //  TARGET_ARM

#if defined(TARGET_X86)
    unsigned maxRegArgs = MAX_REG_ARG; // X86: non-const, must be calculated
#else
    const unsigned maxRegArgs = MAX_REG_ARG; // other arch: fixed constant number
#endif

    if (call->fgArgInfo != nullptr)
    {
        // We've already initialized and set the fgArgInfo.
        return;
    }
    JITDUMP("Initializing arg info for %d.%s:\n", call->gtTreeID, GenTree::OpName(call->gtOper));

    // At this point, we should never have gtCallLateArgs, as this needs to be done before those are determined.
    assert(call->gtCallLateArgs == nullptr);

#ifdef TARGET_UNIX
    if (callIsVararg)
    {
        // Currently native varargs is not implemented on non windows targets.
        //
        // Note that some targets like Arm64 Unix should not need much work as
        // the ABI is the same. While other targets may only need small changes
        // such as amd64 Unix, which just expects RAX to pass numFPArguments.
        NYI("Morphing Vararg call not yet implemented on non Windows targets.");
    }
#endif // TARGET_UNIX

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
        NonStandardArgs(CompAllocator alloc) : args(alloc, 3) // We will have at most 3 non-standard arguments
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
                if (node == args.Top(i).node)
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
                NonStandardArg& nsa = args.TopRef(i);
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
            args.TopRef(index).node = node;
        }

    } nonStandardArgs(getAllocator(CMK_ArrayStack));

    // Count of args. On first morph, this is counted before we've filled in the arg table.
    // On remorph, we grab it from the arg table.
    unsigned numArgs = 0;

    // First we need to count the args
    if (call->gtCallThisArg != nullptr)
    {
        numArgs++;
    }
    for (GenTreeCall::Use& use : call->Args())
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

#if defined(TARGET_X86) || defined(TARGET_ARM)
    // The x86 and arm32 CORINFO_HELP_INIT_PINVOKE_FRAME helpers has a custom calling convention.
    // Set the argument registers correctly here.
    if (call->IsHelperCall(this, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        GenTreeCall::Use* args = call->gtCallArgs;
        GenTree*          arg1 = args->GetNode();
        assert(arg1 != nullptr);
        nonStandardArgs.Add(arg1, REG_PINVOKE_FRAME);
    }
#endif // defined(TARGET_X86) || defined(TARGET_ARM)
#if defined(TARGET_ARM)
    // A non-standard calling convention using wrapper delegate invoke is used on ARM, only, for wrapper
    // delegates. It is used for VSD delegate calls where the VSD custom calling convention ABI requires passing
    // R4, a callee-saved register, with a special value. Since R4 is a callee-saved register, its value needs
    // to be preserved. Thus, the VM uses a wrapper delegate IL stub, which preserves R4 and also sets up R4
    // correctly for the VSD call. The VM is simply reusing an existing mechanism (wrapper delegate IL stub)
    // to achieve its goal for delegate VSD call. See COMDelegate::NeedsWrapperDelegate() in the VM for details.
    else if (call->gtCallMoreFlags & GTF_CALL_M_WRAPPER_DELEGATE_INV)
    {
        GenTree* arg = call->gtCallThisArg->GetNode();
        if (arg->OperIsLocal())
        {
            arg = gtClone(arg, true);
        }
        else
        {
            GenTree* tmp = fgInsertCommaFormTemp(&arg);
            call->gtCallThisArg->SetNode(arg);
            call->gtFlags |= GTF_ASG;
            arg = tmp;
        }
        noway_assert(arg != nullptr);

        GenTree* newArg = new (this, GT_ADDR)
            GenTreeAddrMode(TYP_BYREF, arg, nullptr, 0, eeGetEEInfo()->offsetOfWrapperDelegateIndirectCell);

        // Append newArg as the last arg
        GenTreeCall::Use** insertionPoint = &call->gtCallArgs;
        for (; *insertionPoint != nullptr; insertionPoint = &((*insertionPoint)->NextRef()))
        {
        }
        *insertionPoint = gtNewCallArgs(newArg);

        numArgs++;
        nonStandardArgs.Add(newArg, virtualStubParamInfo->GetReg());
    }
#endif // defined(TARGET_ARM)
#if defined(TARGET_X86)
    // The x86 shift helpers have custom calling conventions and expect the lo part of the long to be in EAX and the
    // hi part to be in EDX. This sets the argument registers up correctly.
    else if (call->IsHelperCall(this, CORINFO_HELP_LLSH) || call->IsHelperCall(this, CORINFO_HELP_LRSH) ||
             call->IsHelperCall(this, CORINFO_HELP_LRSZ))
    {
        GenTreeCall::Use* args = call->gtCallArgs;
        GenTree*          arg1 = args->GetNode();
        assert(arg1 != nullptr);
        nonStandardArgs.Add(arg1, REG_LNGARG_LO);

        args          = args->GetNext();
        GenTree* arg2 = args->GetNode();
        assert(arg2 != nullptr);
        nonStandardArgs.Add(arg2, REG_LNGARG_HI);
    }
#else  // !TARGET_X86
    // TODO-X86-CQ: Currently RyuJIT/x86 passes args on the stack, so this is not needed.
    // If/when we change that, the following code needs to be changed to correctly support the (TBD) managed calling
    // convention for x86/SSE.

    // If we have a Fixed Return Buffer argument register then we setup a non-standard argument for it
    //
    if (hasFixedRetBuffReg() && call->HasRetBufArg())
    {
        args = call->gtCallArgs;
        assert(args != nullptr);

        argx = call->gtCallArgs->GetNode();

        // We don't increment numArgs here, since we already counted this argument above.

        nonStandardArgs.Add(argx, theFixedRetBuffReg());
    }

    // We are allowed to have a Fixed Return Buffer argument combined
    // with any of the remaining non-standard arguments
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

    if (call->IsVirtualStub())
    {
        if (!call->IsTailCallViaJitHelper())
        {
            GenTree* stubAddrArg = fgGetStubAddrArg(call);
            // And push the stub address onto the list of arguments
            call->gtCallArgs = gtPrependNewCallArg(stubAddrArg, call->gtCallArgs);

            numArgs++;
            nonStandardArgs.Add(stubAddrArg, stubAddrArg->GetRegNum());
        }
        else
        {
            // If it is a VSD call getting dispatched via tail call helper,
            // fgMorphTailCallViaJitHelper() would materialize stub addr as an additional
            // parameter added to the original arg list and hence no need to
            // add as a non-standard arg.
        }
    }
    else
#endif // !TARGET_X86
    if (call->gtCallType == CT_INDIRECT && (call->gtCallCookie != nullptr))
    {
        assert(!call->IsUnmanaged());

        GenTree* arg = call->gtCallCookie;
        noway_assert(arg != nullptr);
        call->gtCallCookie = nullptr;

#if defined(TARGET_X86)
        // x86 passes the cookie on the stack as the final argument to the call.
        GenTreeCall::Use** insertionPoint = &call->gtCallArgs;
        for (; *insertionPoint != nullptr; insertionPoint = &((*insertionPoint)->NextRef()))
        {
        }
        *insertionPoint = gtNewCallArgs(arg);
#else  // !defined(TARGET_X86)
        // All other architectures pass the cookie in a register.
        call->gtCallArgs = gtPrependNewCallArg(arg, call->gtCallArgs);
#endif // defined(TARGET_X86)

        nonStandardArgs.Add(arg, REG_PINVOKE_COOKIE_PARAM);
        numArgs++;

        // put destination into R10/EAX
        arg              = gtClone(call->gtCallAddr, true);
        call->gtCallArgs = gtPrependNewCallArg(arg, call->gtCallArgs);
        numArgs++;

        nonStandardArgs.Add(arg, REG_PINVOKE_TARGET_PARAM);

        // finally change this call to a helper call
        call->gtCallType    = CT_HELPER;
        call->gtCallMethHnd = eeFindHelper(CORINFO_HELP_PINVOKE_CALLI);
    }
#if defined(FEATURE_READYTORUN_COMPILER) && defined(TARGET_ARMARCH)
    // For arm, we dispatch code same as VSD using virtualStubParamInfo->GetReg()
    // for indirection cell address, which ZapIndirectHelperThunk expects.
    if (call->IsR2RRelativeIndir())
    {
        assert(call->gtEntryPoint.addr != nullptr);

        size_t   addrValue           = (size_t)call->gtEntryPoint.addr;
        GenTree* indirectCellAddress = gtNewIconHandleNode(addrValue, GTF_ICON_FTN_ADDR);
#ifdef DEBUG
        indirectCellAddress->AsIntCon()->gtTargetHandle = (size_t)call->gtCallMethHnd;
#endif
        indirectCellAddress->SetRegNum(REG_R2R_INDIRECT_PARAM);
#ifdef TARGET_ARM
        // Issue #xxxx : Don't attempt to CSE this constant on ARM32
        //
        // This constant has specific register requirements, and LSRA doesn't currently correctly
        // handle them when the value is in a CSE'd local.
        indirectCellAddress->SetDoNotCSE();
#endif // TARGET_ARM

        // Push the stub address onto the list of arguments.
        call->gtCallArgs = gtPrependNewCallArg(indirectCellAddress, call->gtCallArgs);

        numArgs++;
        nonStandardArgs.Add(indirectCellAddress, indirectCellAddress->GetRegNum());
    }

#endif // FEATURE_READYTORUN_COMPILER && TARGET_ARMARCH

    // Allocate the fgArgInfo for the call node;
    //
    call->fgArgInfo = new (this, CMK_Unknown) fgArgInfo(this, call, numArgs);

    // Add the 'this' argument value, if present.
    if (call->gtCallThisArg != nullptr)
    {
        argx = call->gtCallThisArg->GetNode();
        assert(argIndex == 0);
        assert(call->gtCallType == CT_USER_FUNC || call->gtCallType == CT_INDIRECT);
        assert(varTypeIsGC(argx) || (argx->gtType == TYP_I_IMPL));

        // This is a register argument - put it in the table.
        call->fgArgInfo->AddRegArg(argIndex, argx, call->gtCallThisArg, genMapIntRegArgNumToRegNum(intArgRegNum), 1, 1,
                                   false, callIsVararg UNIX_AMD64_ABI_ONLY_ARG(REG_STK) UNIX_AMD64_ABI_ONLY_ARG(0)
                                              UNIX_AMD64_ABI_ONLY_ARG(0) UNIX_AMD64_ABI_ONLY_ARG(nullptr));

        intArgRegNum++;
#ifdef WINDOWS_AMD64_ABI
        // Whenever we pass an integer register argument
        // we skip the corresponding floating point register argument
        fltArgRegNum++;
#endif // WINDOWS_AMD64_ABI
        argIndex++;
        argSlots++;
    }

#ifdef TARGET_X86
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
            noway_assert(call->gtCallArgs->GetNode()->TypeGet() == TYP_I_IMPL ||
                         call->gtCallArgs->GetNode()->TypeGet() == TYP_BYREF ||
                         call->gtCallArgs->GetNode()->gtOper ==
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
#endif // TARGET_X86

    /* Morph the user arguments */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_ARM)

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

#endif // TARGET_ARM

#ifdef UNIX_AMD64_ABI
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
#endif // UNIX_AMD64_ABI

    for (args = call->gtCallArgs; args != nullptr; args = args->GetNext(), argIndex++)
    {
        argx                    = args->GetNode();
        fgArgTabEntry* argEntry = nullptr;

        // Change the node to TYP_I_IMPL so we don't report GC info
        // NOTE: We deferred this from the importer because of the inliner.

        if (argx->IsLocalAddrExpr() != nullptr)
        {
            argx->gtType = TYP_I_IMPL;
        }

        // We should never have any ArgPlaceHolder nodes at this point.
        assert(!argx->IsArgPlaceHolderNode());

        // Setup any HFA information about 'argx'
        bool      isHfaArg = false;
        var_types hfaType  = TYP_UNDEF;
        unsigned  hfaSlots = 0;

        bool                 passUsingFloatRegs;
        unsigned             argAlign      = 1;
        unsigned             size          = 0;
        CORINFO_CLASS_HANDLE copyBlkClass  = nullptr;
        bool                 isRegArg      = false;
        bool                 isNonStandard = false;
        regNumber            nonStdRegNum  = REG_NA;

#ifdef FEATURE_HFA
        hfaType  = GetHfaType(argx);
        isHfaArg = varTypeIsValidHfaType(hfaType);

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
        // Make sure for vararg methods isHfaArg is not true.
        isHfaArg = callIsVararg ? false : isHfaArg;
#endif // defined(TARGET_WINDOWS) && defined(TARGET_ARM64)

        if (isHfaArg)
        {
            isHfaArg = true;
            hfaSlots = GetHfaCount(argx);

            // If we have a HFA struct it's possible we transition from a method that originally
            // only had integer types to now start having FP types.  We have to communicate this
            // through this flag since LSRA later on will use this flag to determine whether
            // or not to track the FP register set.
            //
            compFloatingPointUsed = true;
        }
#endif // FEATURE_HFA

#ifdef TARGET_ARM
        passUsingFloatRegs    = !callIsVararg && (isHfaArg || varTypeUsesFloatReg(argx)) && !opts.compUseSoftFP;
        bool passUsingIntRegs = passUsingFloatRegs ? false : (intArgRegNum < MAX_REG_ARG);

        // We don't use the "size" return value from InferOpSizeAlign().
        codeGen->InferOpSizeAlign(argx, &argAlign);

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

#elif defined(TARGET_ARM64)

        assert(!callIsVararg || !isHfaArg);
        passUsingFloatRegs = !callIsVararg && (isHfaArg || varTypeUsesFloatReg(argx));

#elif defined(TARGET_AMD64)

        passUsingFloatRegs = varTypeIsFloating(argx);

#elif defined(TARGET_X86)

        passUsingFloatRegs = false;

#else
#error Unsupported or unset target architecture
#endif // TARGET*

        bool      isBackFilled     = false;
        unsigned  nextFltArgRegNum = fltArgRegNum; // This is the next floating-point argument register number to use
        var_types structBaseType   = TYP_STRUCT;
        unsigned  structSize       = 0;
        bool      passStructByRef  = false;

        bool     isStructArg;
        GenTree* actualArg = argx->gtEffectiveVal(true /* Commas only */);

        //
        // Figure out the size of the argument. This is either in number of registers, or number of
        // TARGET_POINTER_SIZE stack slots, or the sum of these if the argument is split between the registers and
        // the stack.
        //
        isStructArg                   = varTypeIsStruct(argx);
        CORINFO_CLASS_HANDLE objClass = NO_CLASS_HANDLE;
        if (isStructArg)
        {
            objClass = gtGetStructHandle(argx);
            if (argx->TypeGet() == TYP_STRUCT)
            {
                // For TYP_STRUCT arguments we must have an OBJ, LCL_VAR or MKREFANY
                switch (actualArg->OperGet())
                {
                    case GT_OBJ:
                        structSize = actualArg->AsObj()->GetLayout()->GetSize();
                        assert(structSize == info.compCompHnd->getClassSize(objClass));
                        break;
                    case GT_LCL_VAR:
                        structSize = lvaGetDesc(actualArg->AsLclVarCommon())->lvExactSize;
                        break;
                    case GT_MKREFANY:
                        structSize = info.compCompHnd->getClassSize(objClass);
                        break;
                    default:
                        BADCODE("illegal argument tree in fgInitArgInfo");
                        break;
                }
            }
            else
            {
                structSize = genTypeSize(argx);
                assert(structSize == info.compCompHnd->getClassSize(objClass));
            }
        }
#if defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI
        if (!isStructArg)
        {
            size = 1; // On AMD64, all primitives fit in a single (64-bit) 'slot'
        }
        else
        {
            size = (unsigned)(roundUp(structSize, TARGET_POINTER_SIZE)) / TARGET_POINTER_SIZE;
            eeGetSystemVAmd64PassStructInRegisterDescriptor(objClass, &structDesc);
        }
#else  // !UNIX_AMD64_ABI
        size               = 1; // On AMD64 Windows, all args fit in a single (64-bit) 'slot'
#endif // UNIX_AMD64_ABI
#elif defined(TARGET_ARM64)
        if (isStructArg)
        {
            if (isHfaArg)
            {
                // HFA structs are passed by value in multiple registers.
                // The "size" in registers may differ the size in pointer-sized units.
                size = GetHfaCount(argx);
            }
            else
            {
                // Structs are either passed in 1 or 2 (64-bit) slots.
                // Structs that are the size of 2 pointers are passed by value in multiple registers,
                // if sufficient registers are available.
                // Structs that are larger than 2 pointers (except for HFAs) are passed by
                // reference (to a copy)
                size = (unsigned)(roundUp(structSize, TARGET_POINTER_SIZE)) / TARGET_POINTER_SIZE;

                if (size > 2)
                {
                    size = 1;
                }
            }
            // Note that there are some additional rules for multireg structs.
            // (i.e they cannot be split between registers and the stack)
        }
        else
        {
            size = 1; // Otherwise, all primitive types fit in a single (64-bit) 'slot'
        }
#elif defined(TARGET_ARM) || defined(TARGET_X86)
        if (isStructArg)
        {
            size = (unsigned)(roundUp(structSize, TARGET_POINTER_SIZE)) / TARGET_POINTER_SIZE;
        }
        else
        {
            // The typical case.
            // Long/double type argument(s) will be modified as needed in Lowering.
            size = genTypeStSz(argx->gtType);
        }
#else
#error Unsupported or unset target architecture
#endif // TARGET_XXX
        if (isStructArg)
        {
            // We have an argument with a struct type, but it may be be a child of a GT_COMMA
            GenTree* argObj = argx->gtEffectiveVal(true /*commaOnly*/);

            assert(argx == args->GetNode());

            unsigned originalSize = structSize;
            originalSize          = (originalSize == 0 ? TARGET_POINTER_SIZE : originalSize);
            unsigned roundupSize  = (unsigned)roundUp(originalSize, TARGET_POINTER_SIZE);

            structSize = originalSize;

            structPassingKind howToPassStruct;

            structBaseType = getArgTypeForStruct(objClass, &howToPassStruct, callIsVararg, originalSize);

            bool passedInRegisters = false;
            passStructByRef        = (howToPassStruct == SPK_ByReference);

            if (howToPassStruct == SPK_PrimitiveType)
            {
// For ARM64 or AMD64/UX we can pass non-power-of-2 structs in a register.
// For ARM or AMD64/Windows only power-of-2 structs are passed in registers.
#if !defined(TARGET_ARM64) && !defined(UNIX_AMD64_ABI)
                if (!isPow2(originalSize))
#endif //  !TARGET_ARM64 && !UNIX_AMD64_ABI
                {
                    passedInRegisters = true;
                }
#ifdef TARGET_ARM
                // TODO-CQ: getArgTypeForStruct should *not* return TYP_DOUBLE for a double struct,
                // or for a struct of two floats. This causes the struct to be address-taken.
                if (structBaseType == TYP_DOUBLE)
                {
                    size = 2;
                }
                else
#endif // TARGET_ARM
                {
                    size = 1;
                }
            }
            else if (passStructByRef)
            {
                size = 1;
            }
        }

        // The 'size' value has now must have been set. (the original value of zero is an invalid value)
        assert(size != 0);

        //
        // Figure out if the argument will be passed in a register.
        //

        if (isRegParamType(genActualType(argx->TypeGet()))
#ifdef UNIX_AMD64_ABI
            && (!isStructArg || structDesc.passedInRegisters)
#endif
                )
        {
#ifdef TARGET_ARM
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
#elif defined(TARGET_ARM64)
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
#if defined(TARGET_WINDOWS)
                    // Arm64 windows native varargs allows splitting a 16 byte struct between stack
                    // and the last general purpose register.
                    if (callIsVararg)
                    {
                        // Override the decision and force a split.
                        isRegArg = isRegArg = (intArgRegNum + (size - 1)) <= maxRegArgs;
                    }
                    else
#endif // defined(TARGET_WINDOWS)
                    {
                        // We also must update intArgRegNum so that we no longer try to
                        // allocate any new general purpose registers for args
                        //
                        intArgRegNum = maxRegArgs;
                    }
                }
            }
#else  // not TARGET_ARM or TARGET_ARM64

#if defined(UNIX_AMD64_ABI)

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
            isRegArg = (intArgRegNum + (size - 1)) < maxRegArgs;
#endif // !defined(UNIX_AMD64_ABI)
#endif // TARGET_ARM
        }
        else
        {
            isRegArg = false;
        }

        // If there are nonstandard args (outside the calling convention) they were inserted above
        // and noted them in a table so we can recognize them here and build their argInfo.
        //
        // They should not affect the placement of any other args or stack space required.
        // Example: on AMD64 R10 and R11 are used for indirect VSD (generic interface) and cookie calls.
        isNonStandard = nonStandardArgs.FindReg(argx, &nonStdRegNum);
        if (isNonStandard)
        {
            isRegArg = (nonStdRegNum != REG_STK);
        }
        else if (call->IsTailCallViaJitHelper())
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

        // Now we know if the argument goes in registers or not and how big it is.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_ARM
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
#endif // TARGET_ARM

        // Now create the fgArgTabEntry.
        fgArgTabEntry* newArgEntry;
        if (isRegArg)
        {
            regNumber nextRegNum = REG_STK;

#if defined(UNIX_AMD64_ABI)
            regNumber    nextOtherRegNum = REG_STK;
            unsigned int structFloatRegs = 0;
            unsigned int structIntRegs   = 0;
#endif // defined(UNIX_AMD64_ABI)

            if (isNonStandard)
            {
                nextRegNum = nonStdRegNum;
            }
#if defined(UNIX_AMD64_ABI)
            else if (isStructArg && structDesc.passedInRegisters)
            {
                // It is a struct passed in registers. Assign the next available register.
                assert((structDesc.eightByteCount <= 2) && "Too many eightbytes.");
                regNumber* nextRegNumPtrs[2] = {&nextRegNum, &nextOtherRegNum};
                for (unsigned int i = 0; i < structDesc.eightByteCount; i++)
                {
                    if (structDesc.IsIntegralSlot(i))
                    {
                        *nextRegNumPtrs[i] = genMapIntRegArgNumToRegNum(intArgRegNum + structIntRegs);
                        ++structIntRegs;
                    }
                    else if (structDesc.IsSseSlot(i))
                    {
                        *nextRegNumPtrs[i] = genMapFloatRegArgNumToRegNum(nextFltArgRegNum + structFloatRegs);
                        ++structFloatRegs;
                    }
                }
            }
#endif // defined(UNIX_AMD64_ABI)
            else
            {
                // fill in or update the argInfo table
                nextRegNum = passUsingFloatRegs ? genMapFloatRegArgNumToRegNum(nextFltArgRegNum)
                                                : genMapIntRegArgNumToRegNum(intArgRegNum);
            }

#ifdef TARGET_AMD64
#ifndef UNIX_AMD64_ABI
            assert(size == 1);
#endif
#endif

            // This is a register argument - put it in the table
            newArgEntry = call->fgArgInfo->AddRegArg(argIndex, argx, args, nextRegNum, size, argAlign, isStructArg,
                                                     callIsVararg UNIX_AMD64_ABI_ONLY_ARG(nextOtherRegNum)
                                                         UNIX_AMD64_ABI_ONLY_ARG(structIntRegs)
                                                             UNIX_AMD64_ABI_ONLY_ARG(structFloatRegs)
                                                                 UNIX_AMD64_ABI_ONLY_ARG(&structDesc));

            newArgEntry->SetIsBackFilled(isBackFilled);
            newArgEntry->isNonStandard = isNonStandard;

            // Set up the next intArgRegNum and fltArgRegNum values.
            if (!isBackFilled)
            {
#if defined(UNIX_AMD64_ABI)
                if (isStructArg)
                {
                    // For this case, we've already set the regNums in the argTabEntry
                    intArgRegNum += structIntRegs;
                    fltArgRegNum += structFloatRegs;
                }
                else
#endif // defined(UNIX_AMD64_ABI)
                {
                    if (!isNonStandard)
                    {
#if FEATURE_ARG_SPLIT
                        // Check for a split (partially enregistered) struct
                        if (!passUsingFloatRegs && ((intArgRegNum + size) > MAX_REG_ARG))
                        {
                            // This indicates a partial enregistration of a struct type
                            assert((isStructArg) || argx->OperIs(GT_FIELD_LIST) || argx->OperIsCopyBlkOp() ||
                                   (argx->gtOper == GT_COMMA && (argx->gtFlags & GTF_ASG)));
                            unsigned numRegsPartial = MAX_REG_ARG - intArgRegNum;
                            assert((unsigned char)numRegsPartial == numRegsPartial);
                            call->fgArgInfo->SplitArg(argIndex, numRegsPartial, size - numRegsPartial);
                        }
#endif // FEATURE_ARG_SPLIT

                        if (passUsingFloatRegs)
                        {
                            fltArgRegNum += size;

#ifdef WINDOWS_AMD64_ABI
                            // Whenever we pass an integer register argument
                            // we skip the corresponding floating point register argument
                            intArgRegNum = min(intArgRegNum + size, MAX_REG_ARG);
#endif // WINDOWS_AMD64_ABI
                            // No supported architecture supports partial structs using float registers.
                            assert(fltArgRegNum <= MAX_FLOAT_REG_ARG);
                        }
                        else
                        {
                            // Increment intArgRegNum by 'size' registers
                            intArgRegNum += size;

#ifdef WINDOWS_AMD64_ABI
                            fltArgRegNum = min(fltArgRegNum + size, MAX_FLOAT_REG_ARG);
#endif // WINDOWS_AMD64_ABI
                        }
                    }
                }
            }
        }
        else // We have an argument that is not passed in a register
        {
            // This is a stack argument - put it in the table
            newArgEntry = call->fgArgInfo->AddStkArg(argIndex, argx, args, size, argAlign, isStructArg, callIsVararg);
#ifdef UNIX_AMD64_ABI
            // TODO-Amd64-Unix-CQ: This is temporary (see also in fgMorphArgs).
            if (structDesc.passedInRegisters)
            {
                newArgEntry->structDesc.CopyFrom(structDesc);
            }
#endif
        }

#ifdef FEATURE_HFA
        if (isHfaArg)
        {
            newArgEntry->SetHfaType(hfaType, hfaSlots);
        }
#endif // FEATURE_HFA
        newArgEntry->SetMultiRegNums();

        noway_assert(newArgEntry != nullptr);
        if (newArgEntry->isStruct)
        {
            newArgEntry->passedByRef = passStructByRef;
            newArgEntry->argType     = (structBaseType == TYP_UNKNOWN) ? argx->TypeGet() : structBaseType;
        }
        else
        {
            newArgEntry->argType = argx->TypeGet();
        }

        argSlots += size;
    } // end foreach argument loop

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("ArgTable for %d.%s after fgInitArgInfo:\n", call->gtTreeID, GenTree::OpName(call->gtOper));
        call->fgArgInfo->Dump(this);
        JITDUMP("\n");
    }
#endif
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
//    This calls fgInitArgInfo to create the 'fgArgInfo' for the call.
//    If it has already been created, that method will simply return.
//
//    This method changes the state of the call node. It uses the existence
//    of gtCallLateArgs (the late arguments list) to determine if it has
//    already done the first round of morphing.
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
GenTreeCall* Compiler::fgMorphArgs(GenTreeCall* call)
{
    GenTreeCall::Use* args;
    GenTree*          argx;

    unsigned flagsSummary = 0;

    unsigned argIndex = 0;
    unsigned argSlots = 0;

    bool reMorphing = call->AreArgsComplete();

    // Set up the fgArgInfo.
    fgInitArgInfo(call);
    unsigned numArgs = call->fgArgInfo->ArgCount();
    JITDUMP("%sMorphing args for %d.%s:\n", (reMorphing) ? "Re" : "", call->gtTreeID, GenTree::OpName(call->gtOper));

    // If we are remorphing, process the late arguments (which were determined by a previous caller).
    if (reMorphing)
    {
        for (GenTreeCall::Use& use : call->LateArgs())
        {
            use.SetNode(fgMorphTree(use.GetNode()));
            flagsSummary |= use.GetNode()->gtFlags;
        }

        assert(call->fgArgInfo != nullptr);
    }
    call->fgArgInfo->RemorphReset();

    // First we morph the argument subtrees ('this' pointer, arguments, etc.).
    // During the first call to fgMorphArgs we also record the
    // information about late arguments we have in 'fgArgInfo'.
    // This information is used later to contruct the gtCallLateArgs */

    // Process the 'this' argument value, if present.
    if (call->gtCallThisArg != nullptr)
    {
        argx                        = call->gtCallThisArg->GetNode();
        fgArgTabEntry* thisArgEntry = call->fgArgInfo->GetArgEntry(0, reMorphing);
        argx                        = fgMorphTree(argx);
        call->gtCallThisArg->SetNode(argx);
        // This is a register argument - possibly update it in the table.
        call->fgArgInfo->UpdateRegArg(thisArgEntry, argx, reMorphing);
        flagsSummary |= argx->gtFlags;
        assert(argIndex == 0);
        argIndex++;
        argSlots++;
    }

    // Note that this name is a bit of a misnomer - it indicates that there are struct args
    // that occupy more than a single slot that are passed by value (not necessarily in regs).
    bool hasMultiregStructArgs = false;
    for (args = call->gtCallArgs; args != nullptr; args = args->GetNext(), argIndex++)
    {
        GenTree**      parentArgx = &args->NodeRef();
        fgArgTabEntry* argEntry   = call->fgArgInfo->GetArgEntry(argIndex, reMorphing);

        // Morph the arg node, and update the parent and argEntry pointers.
        argx        = *parentArgx;
        argx        = fgMorphTree(argx);
        *parentArgx = argx;
        assert(argx == args->GetNode());

        unsigned             argAlign     = argEntry->alignment;
        unsigned             size         = argEntry->getSize();
        CORINFO_CLASS_HANDLE copyBlkClass = NO_CLASS_HANDLE;

        if (argAlign == 2)
        {
            if (argSlots % 2 == 1)
            {
                argSlots++;
            }
        }
        if (argEntry->isNonStandard)
        {
            // We need to update the node field for this nonStandard arg here
            // as it may have been changed by the call to fgMorphTree.
            call->fgArgInfo->UpdateRegArg(argEntry, argx, reMorphing);
            flagsSummary |= argx->gtFlags;
            continue;
        }

        assert(size != 0);
        argSlots += argEntry->getSlotCount();

        if (argx->IsLocalAddrExpr() != nullptr)
        {
            argx->gtType = TYP_I_IMPL;
        }

        // Get information about this argument.
        var_types hfaType            = argEntry->GetHfaType();
        bool      isHfaArg           = (hfaType != TYP_UNDEF);
        unsigned  hfaSlots           = argEntry->numRegs;
        bool      passUsingFloatRegs = argEntry->isPassedInFloatRegisters();
        bool      isBackFilled       = argEntry->IsBackFilled();
        unsigned  structSize         = 0;

        // Struct arguments may be morphed into a node that is not a struct type.
        // In such case the fgArgTabEntry keeps track of whether the original node (before morphing)
        // was a struct and the struct classification.
        bool isStructArg = argEntry->isStruct;

        GenTree* argObj = argx->gtEffectiveVal(true /*commaOnly*/);
        if (isStructArg && varTypeIsStruct(argObj) && !argObj->OperIs(GT_ASG, GT_MKREFANY, GT_FIELD_LIST, GT_ARGPLACE))
        {
            CORINFO_CLASS_HANDLE objClass = gtGetStructHandle(argObj);
            unsigned             originalSize;
            if (argObj->TypeGet() == TYP_STRUCT)
            {
                if (argObj->OperIs(GT_OBJ))
                {
                    // Get the size off the OBJ node.
                    originalSize = argObj->AsObj()->GetLayout()->GetSize();
                    assert(originalSize == info.compCompHnd->getClassSize(objClass));
                }
                else
                {
                    // We have a BADCODE assert for this in fgInitArgInfo.
                    assert(argObj->OperIs(GT_LCL_VAR));
                    originalSize = lvaGetDesc(argObj->AsLclVarCommon())->lvExactSize;
                }
            }
            else
            {
                originalSize = genTypeSize(argx);
                assert(originalSize == info.compCompHnd->getClassSize(objClass));
            }
            unsigned  roundupSize    = (unsigned)roundUp(originalSize, TARGET_POINTER_SIZE);
            var_types structBaseType = argEntry->argType;

            // First, handle the case where the argument is passed by reference.
            if (argEntry->passedByRef)
            {
                assert(size == 1);
                copyBlkClass = objClass;
#ifdef UNIX_AMD64_ABI
                assert(!"Structs are not passed by reference on x64/ux");
#endif // UNIX_AMD64_ABI
            }
            else // This is passed by value.
            {

#ifndef TARGET_X86
                // Check to see if we can transform this into load of a primitive type.
                // 'size' must be the number of pointer sized items
                assert(size == roundupSize / TARGET_POINTER_SIZE);

                structSize           = originalSize;
                unsigned passingSize = originalSize;

                // Check to see if we can transform this struct load (GT_OBJ) into a GT_IND of the appropriate size.
                // When it can do this is platform-dependent:
                // - In general, it can be done for power of 2 structs that fit in a single register.
                // - For ARM and ARM64 it must also be a non-HFA struct, or have a single field.
                // - This is irrelevant for X86, since structs are always passed by value on the stack.

                GenTree* lclVar       = fgIsIndirOfAddrOfLocal(argObj);
                bool     canTransform = false;

                if (structBaseType != TYP_STRUCT)
                {
                    if (isPow2(passingSize))
                    {
                        canTransform = (!argEntry->IsHfaArg() || (passingSize == genTypeSize(argEntry->GetHfaType())));
                    }

#if defined(TARGET_ARM64) || defined(UNIX_AMD64_ABI)
                    // For ARM64 or AMD64/UX we can pass non-power-of-2 structs in a register, but we can
                    // only transform in that case if the arg is a local.
                    // TODO-CQ: This transformation should be applicable in general, not just for the ARM64
                    // or UNIX_AMD64_ABI cases where they will be passed in registers.
                    else
                    {
                        canTransform = (lclVar != nullptr);
                        passingSize  = genTypeSize(structBaseType);
                    }
#endif //  TARGET_ARM64 || UNIX_AMD64_ABI
                }

                if (!canTransform)
                {
#if defined(TARGET_AMD64)
#ifndef UNIX_AMD64_ABI
                    // On Windows structs are always copied and passed by reference (handled above) unless they are
                    // passed by value in a single register.
                    assert(size == 1);
                    copyBlkClass = objClass;
#else  // UNIX_AMD64_ABI
                    // On Unix, structs are always passed by value.
                    // We only need a copy if we have one of the following:
                    // - The sizes don't match for a non-lclVar argument.
                    // - We have a known struct type (e.g. SIMD) that requires multiple registers.
                    // TODO-Amd64-Unix-Throughput: We don't need to keep the structDesc in the argEntry if it's not
                    // actually passed in registers.
                    if (argEntry->isPassedInRegisters())
                    {
                        if (argObj->OperIs(GT_OBJ))
                        {
                            if (passingSize != structSize)
                            {
                                copyBlkClass = objClass;
                            }
                        }
                        else if (lclVar == nullptr)
                        {
                            // This should only be the case of a value directly producing a known struct type.
                            assert(argObj->TypeGet() != TYP_STRUCT);
                            if (argEntry->numRegs > 1)
                            {
                                copyBlkClass = objClass;
                            }
                        }
                    }
#endif // UNIX_AMD64_ABI
#elif defined(TARGET_ARM64)
                    if ((passingSize != structSize) && (lclVar == nullptr))
                    {
                        copyBlkClass = objClass;
                    }
#endif

#ifdef TARGET_ARM
                    // TODO-1stClassStructs: Unify these conditions across targets.
                    if (((lclVar != nullptr) &&
                         (lvaGetPromotionType(lclVar->AsLclVarCommon()->GetLclNum()) == PROMOTION_TYPE_INDEPENDENT)) ||
                        ((argObj->OperIs(GT_OBJ)) && (passingSize != structSize)))
                    {
                        copyBlkClass = objClass;
                    }

                    if (structSize < TARGET_POINTER_SIZE)
                    {
                        copyBlkClass = objClass;
                    }
#endif // TARGET_ARM
                }
                else
                {
                    // We have a struct argument that fits into a register, and it is either a power of 2,
                    // or a local.
                    // Change our argument, as needed, into a value of the appropriate type.
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_ARM
                    assert((size == 1) || ((structBaseType == TYP_DOUBLE) && (size == 2)));
#else
                    assert((size == 1) ||
                           (varTypeIsSIMD(structBaseType) && size == (genTypeSize(structBaseType) / REGSIZE_BYTES)));
#endif

                    assert((structBaseType != TYP_STRUCT) && (genTypeSize(structBaseType) >= originalSize));

                    if (argObj->OperIs(GT_OBJ))
                    {
                        argObj->ChangeOper(GT_IND);

                        // Now see if we can fold *(&X) into X
                        if (argObj->AsOp()->gtOp1->gtOper == GT_ADDR)
                        {
                            GenTree* temp = argObj->AsOp()->gtOp1->AsOp()->gtOp1;

                            // Keep the DONT_CSE flag in sync
                            // (as the addr always marks it for its op1)
                            temp->gtFlags &= ~GTF_DONT_CSE;
                            temp->gtFlags |= (argObj->gtFlags & GTF_DONT_CSE);
                            DEBUG_DESTROY_NODE(argObj->AsOp()->gtOp1); // GT_ADDR
                            DEBUG_DESTROY_NODE(argObj);                // GT_IND

                            argObj      = temp;
                            *parentArgx = temp;
                            argx        = temp;
                        }
                    }
                    if (argObj->gtOper == GT_LCL_VAR)
                    {
                        unsigned   lclNum = argObj->AsLclVarCommon()->GetLclNum();
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
                                    argObj->AsLclVarCommon()->SetLclNum(varDsc->lvFieldLclStart);

                                    if (varTypeIsEnregisterable(fieldVarDsc->TypeGet()) &&
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
                                    assert(varTypeIsEnregisterable(argObj->TypeGet()));
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
                            }
                        }
                        else if (genTypeSize(varDsc->TypeGet()) != genTypeSize(structBaseType))
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
                    assert(varTypeIsEnregisterable(argObj->TypeGet()) ||
                           ((copyBlkClass != NO_CLASS_HANDLE) && varTypeIsEnregisterable(structBaseType)));
                }
#endif // !TARGET_X86

#ifndef UNIX_AMD64_ABI
                // We still have a struct unless we converted the GT_OBJ into a GT_IND above...
                if (isHfaArg && passUsingFloatRegs)
                {
                    size = argEntry->numRegs;
                }
                else if (structBaseType == TYP_STRUCT)
                {
                    // If the valuetype size is not a multiple of TARGET_POINTER_SIZE,
                    // we must copyblk to a temp before doing the obj to avoid
                    // the obj reading memory past the end of the valuetype
                    CLANG_FORMAT_COMMENT_ANCHOR;

                    if (roundupSize > originalSize)
                    {
                        copyBlkClass = objClass;

                        // There are a few special cases where we can omit using a CopyBlk
                        // where we normally would need to use one.

                        if (argObj->OperIs(GT_OBJ) &&
                            argObj->AsObj()->gtGetOp1()->IsLocalAddrExpr() != nullptr) // Is the source a LclVar?
                        {
                            copyBlkClass = NO_CLASS_HANDLE;
                        }
                    }

                    size = roundupSize / TARGET_POINTER_SIZE; // Normalize size to number of pointer sized items
                }

#endif // !UNIX_AMD64_ABI
            }
        }

        if (argEntry->isPassedInRegisters())
        {
            call->fgArgInfo->UpdateRegArg(argEntry, argx, reMorphing);
        }
        else
        {
            call->fgArgInfo->UpdateStkArg(argEntry, argx, reMorphing);
        }

        if (copyBlkClass != NO_CLASS_HANDLE)
        {
            fgMakeOutgoingStructArgCopy(call, args, argIndex, copyBlkClass);
        }

        if (argx->gtOper == GT_MKREFANY)
        {
            // 'Lower' the MKREFANY tree and insert it.
            noway_assert(!reMorphing);

#ifdef TARGET_X86
            // Build the mkrefany as a GT_FIELD_LIST
            GenTreeFieldList* fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();
            fieldList->AddField(this, argx->AsOp()->gtGetOp1(), OFFSETOF__CORINFO_TypedReference__dataPtr, TYP_BYREF);
            fieldList->AddField(this, argx->AsOp()->gtGetOp2(), OFFSETOF__CORINFO_TypedReference__type, TYP_I_IMPL);
            fgArgTabEntry* fp = gtArgEntryByNode(call, argx);
            args->SetNode(fieldList);
            assert(fp->GetNode() == fieldList);
#else  // !TARGET_X86

            // Get a new temp
            // Here we don't need unsafe value cls check since the addr of temp is used only in mkrefany
            unsigned tmp = lvaGrabTemp(true DEBUGARG("by-value mkrefany struct argument"));
            lvaSetStruct(tmp, impGetRefAnyClass(), false);

            // Build the mkrefany as a comma node:
            // (tmp.ptr=argx),(tmp.type=handle)
            GenTreeLclFld* destPtrSlot  = gtNewLclFldNode(tmp, TYP_I_IMPL, OFFSETOF__CORINFO_TypedReference__dataPtr);
            GenTreeLclFld* destTypeSlot = gtNewLclFldNode(tmp, TYP_I_IMPL, OFFSETOF__CORINFO_TypedReference__type);
            destPtrSlot->SetFieldSeq(GetFieldSeqStore()->CreateSingleton(GetRefanyDataField()));
            destPtrSlot->gtFlags |= GTF_VAR_DEF;
            destTypeSlot->SetFieldSeq(GetFieldSeqStore()->CreateSingleton(GetRefanyTypeField()));
            destTypeSlot->gtFlags |= GTF_VAR_DEF;

            GenTree* asgPtrSlot  = gtNewAssignNode(destPtrSlot, argx->AsOp()->gtOp1);
            GenTree* asgTypeSlot = gtNewAssignNode(destTypeSlot, argx->AsOp()->gtOp2);
            GenTree* asg         = gtNewOperNode(GT_COMMA, TYP_VOID, asgPtrSlot, asgTypeSlot);

            // Change the expression to "(tmp=val)"
            args->SetNode(asg);

            // EvalArgsToTemps will cause tmp to actually get loaded as the argument
            call->fgArgInfo->EvalToTmp(argEntry, tmp, asg);
            lvaSetVarAddrExposed(tmp);
#endif // !TARGET_X86
        }

#if FEATURE_MULTIREG_ARGS
        if (isStructArg)
        {
            if (((argEntry->numRegs + argEntry->numSlots) > 1) || (isHfaArg && argx->TypeGet() == TYP_STRUCT))
            {
                hasMultiregStructArgs = true;
            }
        }
#ifdef TARGET_ARM
        else if ((argEntry->argType == TYP_LONG) || (argEntry->argType == TYP_DOUBLE))
        {
            assert((argEntry->numRegs == 2) || (argEntry->numSlots == 2));
        }
#endif
        else
        {
            // We must have exactly one register or slot.
            assert(((argEntry->numRegs == 1) && (argEntry->numSlots == 0)) ||
                   ((argEntry->numRegs == 0) && (argEntry->numSlots == 1)));
        }
#endif

#if defined(TARGET_X86)
        if (isStructArg)
        {
            GenTree* lclNode = argx->OperIs(GT_LCL_VAR) ? argx : fgIsIndirOfAddrOfLocal(argx);
            if ((lclNode != nullptr) &&
                (lvaGetPromotionType(lclNode->AsLclVarCommon()->GetLclNum()) == Compiler::PROMOTION_TYPE_INDEPENDENT))
            {
                // Make a GT_FIELD_LIST of the field lclVars.
                GenTreeLclVarCommon* lcl       = lclNode->AsLclVarCommon();
                LclVarDsc*           varDsc    = lvaGetDesc(lcl);
                GenTreeFieldList*    fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();

                fgArgTabEntry* fp = gtArgEntryByNode(call, argx);
                args->SetNode(fieldList);
                assert(fp->GetNode() == fieldList);

                for (unsigned fieldLclNum = varDsc->lvFieldLclStart;
                     fieldLclNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++fieldLclNum)
                {
                    LclVarDsc* fieldVarDsc = lvaGetDesc(fieldLclNum);
                    GenTree*   fieldLcl;

                    if (fieldLclNum == varDsc->lvFieldLclStart)
                    {
                        lcl->SetLclNum(fieldLclNum);
                        lcl->ChangeOper(GT_LCL_VAR);
                        lcl->gtType = fieldVarDsc->TypeGet();
                        fieldLcl    = lcl;
                    }
                    else
                    {
                        fieldLcl = gtNewLclvNode(fieldLclNum, fieldVarDsc->TypeGet());
                    }

                    fieldList->AddField(this, fieldLcl, fieldVarDsc->lvFldOffset, fieldVarDsc->TypeGet());
                }
            }
        }
#endif // TARGET_X86

        flagsSummary |= args->GetNode()->gtFlags;

    } // end foreach argument loop

    if (!reMorphing)
    {
        call->fgArgInfo->ArgsComplete();
    }

    /* Process the function address, if indirect call */

    if (call->gtCallType == CT_INDIRECT)
    {
        call->gtCallAddr = fgMorphTree(call->gtCallAddr);
        // Const CSE may create an assignment node here
        flagsSummary |= call->gtCallAddr->gtFlags;
    }

#if FEATURE_FIXED_OUT_ARGS

    // Record the outgoing argument size.  If the call is a fast tail
    // call, it will setup its arguments in incoming arg area instead
    // of the out-going arg area, so we don't need to track the
    // outgoing arg size.
    if (!call->IsFastTailCall())
    {
        unsigned preallocatedArgCount = call->fgArgInfo->GetNextSlotNum();

#if defined(UNIX_AMD64_ABI)
        // This is currently required for the UNIX ABI to work correctly.
        opts.compNeedToAlignFrame = true;
#endif // UNIX_AMD64_ABI

        const unsigned outgoingArgSpaceSize = preallocatedArgCount * REGSIZE_BYTES;
        call->fgArgInfo->SetOutArgSize(max(outgoingArgSpaceSize, MIN_ARG_AREA_FOR_CALL));

#ifdef DEBUG
        if (verbose)
        {
            printf("argSlots=%d, preallocatedArgCount=%d, nextSlotNum=%d, outgoingArgSpaceSize=%d\n", argSlots,
                   preallocatedArgCount, call->fgArgInfo->GetNextSlotNum(), outgoingArgSpaceSize);
        }
#endif
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // Clear the ASG and EXCEPT (if possible) flags on the call node
    call->gtFlags &= ~GTF_ASG;
    if (!call->OperMayThrow(this))
    {
        call->gtFlags &= ~GTF_EXCEPT;
    }

    // Union in the side effect flags from the call's operands
    call->gtFlags |= flagsSummary & GTF_ALL_EFFECT;

    // If we are remorphing or don't have any register arguments or other arguments that need
    // temps, then we don't need to call SortArgs() and EvalArgsToTemps().
    //
    if (!reMorphing && (call->fgArgInfo->HasRegArgs() || call->fgArgInfo->NeedsTemps()))
    {
        // Do the 'defer or eval to temp' analysis.

        call->fgArgInfo->SortArgs();

        call->fgArgInfo->EvalArgsToTemps();
    }

    if (hasMultiregStructArgs)
    {
        fgMorphMultiregStructArgs(call);
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("ArgTable for %d.%s after fgMorphArgs:\n", call->gtTreeID, GenTree::OpName(call->gtOper));
        call->fgArgInfo->Dump(this);
        JITDUMP("\n");
    }
#endif
    return call;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//-----------------------------------------------------------------------------
// fgMorphMultiregStructArgs:  Locate the TYP_STRUCT arguments and
//                             call fgMorphMultiregStructArg on each of them.
//
// Arguments:
//    call    :    a GenTreeCall node that has one or more TYP_STRUCT arguments\.
//
// Notes:
//    We only call fgMorphMultiregStructArg for struct arguments that are not passed as simple types.
//    It will ensure that the struct arguments are in the correct form.
//    If this method fails to find any TYP_STRUCT arguments it will assert.
//
void Compiler::fgMorphMultiregStructArgs(GenTreeCall* call)
{
    bool       foundStructArg = false;
    unsigned   initialFlags   = call->gtFlags;
    unsigned   flagsSummary   = 0;
    fgArgInfo* allArgInfo     = call->fgArgInfo;

#ifdef TARGET_X86
    assert(!"Logic error: no MultiregStructArgs for X86");
#endif
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    assert(!"Logic error: no MultiregStructArgs for Windows X64 ABI");
#endif

    for (GenTreeCall::Use& use : call->Args())
    {
        // For late arguments the arg tree that is overridden is in the gtCallLateArgs list.
        // For such late args the gtCallArgList contains the setup arg node (evaluating the arg.)
        // The tree from the gtCallLateArgs list is passed to the callee. The fgArgEntry node contains the mapping
        // between the nodes in both lists. If the arg is not a late arg, the fgArgEntry->node points to itself,
        // otherwise points to the list in the late args list.
        bool           isLateArg  = (use.GetNode()->gtFlags & GTF_LATE_ARG) != 0;
        fgArgTabEntry* fgEntryPtr = gtArgEntryByNode(call, use.GetNode());
        assert(fgEntryPtr != nullptr);
        GenTree*          argx     = fgEntryPtr->GetNode();
        GenTreeCall::Use* lateUse  = nullptr;
        GenTree*          lateNode = nullptr;

        if (isLateArg)
        {
            for (GenTreeCall::Use& lateArgUse : call->LateArgs())
            {
                GenTree* argNode = lateArgUse.GetNode();
                if (argx == argNode)
                {
                    lateUse  = &lateArgUse;
                    lateNode = argNode;
                    break;
                }
            }
            assert((lateUse != nullptr) && (lateNode != nullptr));
        }

        if (!fgEntryPtr->isStruct)
        {
            continue;
        }

        unsigned size = (fgEntryPtr->numRegs + fgEntryPtr->numSlots);
        if ((size > 1) || (fgEntryPtr->IsHfaArg() && argx->TypeGet() == TYP_STRUCT))
        {
            foundStructArg = true;
            if (varTypeIsStruct(argx) && !argx->OperIs(GT_FIELD_LIST))
            {
                if (fgEntryPtr->IsHfaRegArg())
                {
                    var_types hfaType = fgEntryPtr->GetHfaType();
                    unsigned  structSize;
                    if (argx->OperIs(GT_OBJ))
                    {
                        structSize = argx->AsObj()->GetLayout()->GetSize();
                    }
                    else
                    {
                        assert(argx->OperIs(GT_LCL_VAR));
                        structSize = lvaGetDesc(argx->AsLclVar()->GetLclNum())->lvExactSize;
                    }
                    assert(structSize > 0);
                    if (structSize == genTypeSize(hfaType))
                    {
                        if (argx->OperIs(GT_OBJ))
                        {
                            argx->SetOper(GT_IND);
                        }

                        argx->gtType = hfaType;
                    }
                }

                GenTree* newArgx = fgMorphMultiregStructArg(argx, fgEntryPtr);

                // Did we replace 'argx' with a new tree?
                if (newArgx != argx)
                {
                    // link the new arg node into either the late arg list or the gtCallArgs list
                    if (isLateArg)
                    {
                        lateUse->SetNode(newArgx);
                    }
                    else
                    {
                        use.SetNode(newArgx);
                    }

                    assert(fgEntryPtr->GetNode() == newArgx);
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
// fgMorphMultiregStructArg:  Given a TYP_STRUCT arg from a call argument list,
//     morph the argument as needed to be passed correctly.
//
// Arguments:
//     arg        - A GenTree node containing a TYP_STRUCT arg
//     fgEntryPtr - the fgArgTabEntry information for the current 'arg'
//
// Notes:
//    The arg must be a GT_OBJ or GT_LCL_VAR or GT_LCL_FLD of TYP_STRUCT.
//    If 'arg' is a lclVar passed on the stack, we will ensure that any lclVars that must be on the
//    stack are marked as doNotEnregister, and then we return.
//
//    If it is passed by register, we mutate the argument into the GT_FIELD_LIST form
//    which is only used for struct arguments.
//
//    If arg is a LclVar we check if it is struct promoted and has the right number of fields
//    and if they are at the appropriate offsets we will use the struct promted fields
//    in the GT_FIELD_LIST nodes that we create.
//    If we have a GT_LCL_VAR that isn't struct promoted or doesn't meet the requirements
//    we will use a set of GT_LCL_FLDs nodes to access the various portions of the struct
//    this also forces the struct to be stack allocated into the local frame.
//    For the GT_OBJ case will clone the address expression and generate two (or more)
//    indirections.
//    Currently the implementation handles ARM64/ARM and will NYI for other architectures.
//
GenTree* Compiler::fgMorphMultiregStructArg(GenTree* arg, fgArgTabEntry* fgEntryPtr)
{
    assert(varTypeIsStruct(arg->TypeGet()));

#if !defined(TARGET_ARMARCH) && !defined(UNIX_AMD64_ABI)
    NYI("fgMorphMultiregStructArg requires implementation for this target");
#endif

#ifdef TARGET_ARM
    if ((fgEntryPtr->IsSplit() && fgEntryPtr->numSlots + fgEntryPtr->numRegs > 4) ||
        (!fgEntryPtr->IsSplit() && fgEntryPtr->GetRegNum() == REG_STK))
#else
    if (fgEntryPtr->GetRegNum() == REG_STK)
#endif
    {
        GenTreeLclVarCommon* lcl       = nullptr;
        GenTree*             actualArg = arg->gtEffectiveVal();

        if (actualArg->OperGet() == GT_OBJ)
        {
            if (actualArg->gtGetOp1()->OperIs(GT_ADDR) && actualArg->gtGetOp1()->gtGetOp1()->OperIs(GT_LCL_VAR))
            {
                lcl = actualArg->gtGetOp1()->gtGetOp1()->AsLclVarCommon();
            }
        }
        else if (actualArg->OperGet() == GT_LCL_VAR)
        {
            lcl = actualArg->AsLclVarCommon();
        }
        if (lcl != nullptr)
        {
            if (lvaGetPromotionType(lcl->GetLclNum()) == PROMOTION_TYPE_INDEPENDENT)
            {
                arg = fgMorphLclArgToFieldlist(lcl);
            }
            else if (arg->TypeGet() == TYP_STRUCT)
            {
                // If this is a non-register struct, it must be referenced from memory.
                if (!actualArg->OperIs(GT_OBJ))
                {
                    // Create an Obj of the temp to use it as a call argument.
                    arg = gtNewOperNode(GT_ADDR, TYP_I_IMPL, arg);
                    arg = gtNewObjNode(lvaGetStruct(lcl->GetLclNum()), arg);
                }
                // Its fields will need to be accessed by address.
                lvaSetVarDoNotEnregister(lcl->GetLclNum() DEBUG_ARG(DNER_IsStructArg));
            }
        }

        return arg;
    }

#if FEATURE_MULTIREG_ARGS
    // Examine 'arg' and setup argValue objClass and structSize
    //
    CORINFO_CLASS_HANDLE objClass = gtGetStructHandleIfPresent(arg);
    noway_assert(objClass != NO_CLASS_HANDLE);
    GenTree* argValue   = arg; // normally argValue will be arg, but see right below
    unsigned structSize = 0;

    if (arg->TypeGet() != TYP_STRUCT)
    {
        structSize = genTypeSize(arg->TypeGet());
        assert(structSize == info.compCompHnd->getClassSize(objClass));
    }
    else if (arg->OperGet() == GT_OBJ)
    {
        GenTreeObj* argObj = arg->AsObj();
        structSize         = argObj->GetLayout()->GetSize();
        assert(structSize == info.compCompHnd->getClassSize(objClass));

        // If we have a GT_OBJ of a GT_ADDR then we set argValue to the child node of the GT_ADDR.
        GenTree* op1 = argObj->gtOp1;
        if (op1->OperGet() == GT_ADDR)
        {
            GenTree* underlyingTree = op1->AsOp()->gtOp1;

            // Only update to the same type.
            if (underlyingTree->OperIs(GT_LCL_VAR) && (underlyingTree->TypeGet() == argValue->TypeGet()) &&
                (objClass == gtGetStructHandleIfPresent(underlyingTree)))
            {
                argValue = underlyingTree;
            }
        }
    }
    else if (arg->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* varNode = arg->AsLclVarCommon();
        unsigned             varNum  = varNode->GetLclNum();
        assert(varNum < lvaCount);
        LclVarDsc* varDsc = &lvaTable[varNum];

        structSize = varDsc->lvExactSize;
        assert(structSize == info.compCompHnd->getClassSize(objClass));
    }
    else
    {
        structSize = info.compCompHnd->getClassSize(objClass);
    }
    noway_assert(objClass != NO_CLASS_HANDLE);

    var_types hfaType                 = TYP_UNDEF;
    var_types elemType                = TYP_UNDEF;
    unsigned  elemCount               = 0;
    unsigned  elemSize                = 0;
    var_types type[MAX_ARG_REG_COUNT] = {}; // TYP_UNDEF = 0

    hfaType = fgEntryPtr->GetHfaType();
    if (varTypeIsValidHfaType(hfaType)
#if !defined(HOST_UNIX) && defined(TARGET_ARM64)
        && !fgEntryPtr->IsVararg()
#endif // !defined(HOST_UNIX) && defined(TARGET_ARM64)
            )
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
        assert(structSize <= MAX_ARG_REG_COUNT * TARGET_POINTER_SIZE);
        BYTE gcPtrs[MAX_ARG_REG_COUNT];
        elemCount = roundUp(structSize, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
        info.compCompHnd->getClassGClayout(objClass, &gcPtrs[0]);

        for (unsigned inx = 0; inx < elemCount; inx++)
        {
#ifdef UNIX_AMD64_ABI
            if (gcPtrs[inx] == TYPE_GC_NONE)
            {
                type[inx] = GetTypeFromClassificationAndSizes(fgEntryPtr->structDesc.eightByteClassifications[inx],
                                                              fgEntryPtr->structDesc.eightByteSizes[inx]);
            }
            else
#endif // UNIX_AMD64_ABI
            {
                type[inx] = getJitGCType(gcPtrs[inx]);
            }
        }

#ifndef UNIX_AMD64_ABI
        if ((argValue->OperGet() == GT_LCL_FLD) || (argValue->OperGet() == GT_LCL_VAR))
        {
            elemSize = TARGET_POINTER_SIZE;
            // We can safely widen this to aligned bytes since we are loading from
            // a GT_LCL_VAR or a GT_LCL_FLD which is properly padded and
            // lives in the stack frame or will be a promoted field.
            //
            structSize = elemCount * TARGET_POINTER_SIZE;
        }
        else // we must have a GT_OBJ
        {
            assert(argValue->OperGet() == GT_OBJ);

            // We need to load the struct from an arbitrary address
            // and we can't read past the end of the structSize
            // We adjust the last load type here
            //
            unsigned remainingBytes = structSize % TARGET_POINTER_SIZE;
            unsigned lastElem       = elemCount - 1;
            if (remainingBytes != 0)
            {
                switch (remainingBytes)
                {
                    case 1:
                        type[lastElem] = TYP_BYTE;
                        break;
                    case 2:
                        type[lastElem] = TYP_SHORT;
                        break;
#if defined(TARGET_ARM64) || defined(UNIX_AMD64_ABI)
                    case 4:
                        type[lastElem] = TYP_INT;
                        break;
#endif // (TARGET_ARM64) || (UNIX_AMD64_ABI)
                    default:
                        noway_assert(!"NYI: odd sized struct in fgMorphMultiregStructArg");
                        break;
                }
            }
        }
#endif // !UNIX_AMD64_ABI
    }

    // We should still have a TYP_STRUCT
    assert(varTypeIsStruct(argValue->TypeGet()));

    GenTreeFieldList* newArg = nullptr;

    // Are we passing a struct LclVar?
    //
    if (argValue->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* varNode = argValue->AsLclVarCommon();
        unsigned             varNum  = varNode->GetLclNum();
        assert(varNum < lvaCount);
        LclVarDsc* varDsc = &lvaTable[varNum];

        // At this point any TYP_STRUCT LclVar must be an aligned struct
        // or an HFA struct, both which are passed by value.
        //
        assert((varDsc->lvSize() == elemCount * TARGET_POINTER_SIZE) || varDsc->lvIsHfa());

        varDsc->lvIsMultiRegArg = true;

#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("Multireg struct argument V%02u : ", varNum);
            fgEntryPtr->Dump();
        }
#endif // DEBUG

#ifndef UNIX_AMD64_ABI
        // This local variable must match the layout of the 'objClass' type exactly
        if (varDsc->lvIsHfa()
#if !defined(HOST_UNIX) && defined(TARGET_ARM64)
            && !fgEntryPtr->IsVararg()
#endif // !defined(HOST_UNIX) && defined(TARGET_ARM64)
                )
        {
            // We have a HFA struct.
            noway_assert(elemType == varDsc->GetHfaType());
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
#if defined(TARGET_ARM64)
            // We must have a 16-byte struct (non-HFA)
            noway_assert(elemCount == 2);
#elif defined(TARGET_ARM)
            noway_assert(elemCount <= 4);
#endif

            for (unsigned inx = 0; inx < elemCount; inx++)
            {
                var_types currentGcLayoutType = varDsc->GetLayout()->GetGCPtrType(inx);

                // We setup the type[inx] value above using the GC info from 'objClass'
                // This GT_LCL_VAR must have the same GC layout info
                //
                if (varTypeIsGC(currentGcLayoutType))
                {
                    noway_assert(type[inx] == currentGcLayoutType);
                }
                else
                {
                    // We may have use a small type when we setup the type[inx] values above
                    // We can safely widen this to TYP_I_IMPL
                    type[inx] = TYP_I_IMPL;
                }
            }
        }
#endif // !UNIX_AMD64_ABI

#if defined(TARGET_ARM64) || defined(UNIX_AMD64_ABI)
        // Is this LclVar a promoted struct with exactly 2 fields?
        // TODO-ARM64-CQ: Support struct promoted HFA types here
        if (varDsc->lvPromoted && (varDsc->lvFieldCnt == 2) && (!varDsc->lvIsHfa()
#if !defined(HOST_UNIX) && defined(TARGET_ARM64)
                                                                && !fgEntryPtr->IsVararg()
#endif // !defined(HOST_UNIX) && defined(TARGET_ARM64)
                                                                    ))
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

                    // Create a new tree for 'arg'
                    //    replace the existing LDOBJ(ADDR(LCLVAR))
                    //    with a FIELD_LIST(LCLVAR-LO, FIELD_LIST(LCLVAR-HI, nullptr))
                    //

                    newArg = new (this, GT_FIELD_LIST) GenTreeFieldList();
                    newArg->AddField(this, gtNewLclvNode(loVarNum, loType), 0, loType);
                    newArg->AddField(this, gtNewLclvNode(hiVarNum, hiType), TARGET_POINTER_SIZE, hiType);
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
#elif defined(TARGET_ARM)
        // Is this LclVar a promoted struct with exactly same size?
        if (varDsc->lvPromoted && (varDsc->lvFieldCnt == elemCount) && !varDsc->lvIsHfa())
        {
            // See if we have promoted fields?
            unsigned varNums[4];
            bool     hasBadVarNum = false;
            for (unsigned inx = 0; inx < elemCount; inx++)
            {
                varNums[inx] = lvaGetFieldLocal(varDsc, TARGET_POINTER_SIZE * inx);
                if (varNums[inx] == BAD_VAR_NUM)
                {
                    hasBadVarNum = true;
                    break;
                }
            }

            // Did we find the promoted fields at the necessary offsets?
            if (!hasBadVarNum)
            {
                LclVarDsc* varDscs[4];
                var_types  varType[4];
                bool       varIsFloat = false;

                for (unsigned inx = 0; inx < elemCount; inx++)
                {
                    varDscs[inx] = &lvaTable[varNums[inx]];
                    varType[inx] = varDscs[inx]->lvType;
                    if (varTypeIsFloating(varType[inx]))
                    {
                        // TODO-LSRA - It currently doesn't support the passing of floating point LCL_VARS in the
                        // integer
                        // registers. So for now we will use GT_LCLFLD's to pass this struct (it won't be enregistered)
                        //
                        JITDUMP("Multireg struct V%02u will be passed using GT_LCLFLD because it has float fields.\n",
                                varNum);
                        //
                        // we call lvaSetVarDoNotEnregister and do the proper transformation below.
                        //
                        varIsFloat = true;
                        break;
                    }
                }

                if (!varIsFloat)
                {
                    newArg = fgMorphLclArgToFieldlist(varNode);
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
#endif // TARGET_ARM
    }

    // If we didn't set newarg to a new List Node tree
    //
    if (newArg == nullptr)
    {
        if (fgEntryPtr->GetRegNum() == REG_STK)
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
            unsigned             varNum  = varNode->GetLclNum();
            assert(varNum < lvaCount);
            LclVarDsc* varDsc = &lvaTable[varNum];

            unsigned baseOffset = varNode->GetLclOffs();
            unsigned lastOffset = baseOffset + structSize;

            // The allocated size of our LocalVar must be at least as big as lastOffset
            assert(varDsc->lvSize() >= lastOffset);

            if (varDsc->HasGCPtr())
            {
                // alignment of the baseOffset is required
                noway_assert((baseOffset % TARGET_POINTER_SIZE) == 0);
#ifndef UNIX_AMD64_ABI
                noway_assert(elemSize == TARGET_POINTER_SIZE);
#endif
                unsigned     baseIndex = baseOffset / TARGET_POINTER_SIZE;
                ClassLayout* layout    = varDsc->GetLayout();
                for (unsigned inx = 0; (inx < elemCount); inx++)
                {
                    // The GC information must match what we setup using 'objClass'
                    if (layout->IsGCPtr(baseIndex + inx) || varTypeGCtype(type[inx]))
                    {
                        noway_assert(type[inx] == layout->GetGCPtrType(baseIndex + inx));
                    }
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

            // Create a new tree for 'arg'
            //    replace the existing LDOBJ(ADDR(LCLVAR))
            //    with a FIELD_LIST(LCLFLD-LO, LCLFLD-HI)
            //
            unsigned offset = baseOffset;
            newArg          = new (this, GT_FIELD_LIST) GenTreeFieldList();
            for (unsigned inx = 0; inx < elemCount; inx++)
            {
                GenTree* nextLclFld = gtNewLclFldNode(varNum, type[inx], offset);
                newArg->AddField(this, nextLclFld, offset, type[inx]);
                offset += genTypeSize(type[inx]);
            }
        }
        // Are we passing a GT_OBJ struct?
        //
        else if (argValue->OperGet() == GT_OBJ)
        {
            GenTreeObj* argObj   = argValue->AsObj();
            GenTree*    baseAddr = argObj->gtOp1;
            var_types   addrType = baseAddr->TypeGet();

            if (baseAddr->OperGet() == GT_ADDR)
            {
                GenTree* addrTaken = baseAddr->AsOp()->gtOp1;
                if (addrTaken->IsLocal())
                {
                    GenTreeLclVarCommon* varNode = addrTaken->AsLclVarCommon();
                    unsigned             varNum  = varNode->GetLclNum();
                    // We access non-struct type (for example, long) as a struct type.
                    // Make sure lclVar lives on stack to make sure its fields are accessible by address.
                    lvaSetVarDoNotEnregister(varNum DEBUGARG(DNER_LocalField));
                }
            }

            // Create a new tree for 'arg'
            //    replace the existing LDOBJ(EXPR)
            //    with a FIELD_LIST(IND(EXPR), FIELD_LIST(IND(EXPR+8), nullptr) ...)
            //

            newArg          = new (this, GT_FIELD_LIST) GenTreeFieldList();
            unsigned offset = 0;
            for (unsigned inx = 0; inx < elemCount; inx++)
            {
                GenTree* curAddr = baseAddr;
                if (offset != 0)
                {
                    GenTree* baseAddrDup = gtCloneExpr(baseAddr);
                    noway_assert(baseAddrDup != nullptr);
                    curAddr = gtNewOperNode(GT_ADD, addrType, baseAddrDup, gtNewIconNode(offset, TYP_I_IMPL));
                }
                else
                {
                    curAddr = baseAddr;
                }
                GenTree* curItem = gtNewIndir(type[inx], curAddr);

                // For safety all GT_IND should have at least GT_GLOB_REF set.
                curItem->gtFlags |= GTF_GLOB_REF;

                newArg->AddField(this, curItem, offset, type[inx]);
                offset += genTypeSize(type[inx]);
            }
        }
    }

#ifdef DEBUG
    // If we reach here we should have set newArg to something
    if (newArg == nullptr)
    {
        gtDispTree(argValue);
        assert(!"Missing case in fgMorphMultiregStructArg");
    }
#endif

    noway_assert(newArg != nullptr);

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

//------------------------------------------------------------------------
// fgMorphLclArgToFieldlist: Morph a GT_LCL_VAR node to a GT_FIELD_LIST of its promoted fields
//
// Arguments:
//    lcl  - The GT_LCL_VAR node we will transform
//
// Return value:
//    The new GT_FIELD_LIST that we have created.
//
GenTreeFieldList* Compiler::fgMorphLclArgToFieldlist(GenTreeLclVarCommon* lcl)
{
    LclVarDsc* varDsc = lvaGetDesc(lcl);
    assert(varDsc->lvPromoted);
    unsigned fieldCount  = varDsc->lvFieldCnt;
    unsigned fieldLclNum = varDsc->lvFieldLclStart;

    GenTreeFieldList* fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();
    for (unsigned i = 0; i < fieldCount; i++)
    {
        LclVarDsc* fieldVarDsc = lvaGetDesc(fieldLclNum);
        GenTree*   lclVar      = gtNewLclvNode(fieldLclNum, fieldVarDsc->TypeGet());
        fieldList->AddField(this, lclVar, fieldVarDsc->lvFldOffset, fieldVarDsc->TypeGet());
        fieldLclNum++;
    }
    return fieldList;
}

//------------------------------------------------------------------------
// fgMakeOutgoingStructArgCopy: make a copy of a struct variable if necessary,
//   to pass to a callee.
//
// Arguments:
//    call - call being processed
//    args - args for the call
///   argIndex - arg being processed
//    copyBlkClass - class handle for the struct
//
// Return value:
//    tree that computes address of the outgoing arg
//
void Compiler::fgMakeOutgoingStructArgCopy(GenTreeCall*         call,
                                           GenTreeCall::Use*    args,
                                           unsigned             argIndex,
                                           CORINFO_CLASS_HANDLE copyBlkClass)
{
    GenTree* argx = args->GetNode();
    noway_assert(argx->gtOper != GT_MKREFANY);
    fgArgTabEntry* argEntry = Compiler::gtArgEntryByNode(call, argx);

    // If we're optimizing, see if we can avoid making a copy.
    //
    // We don't need a copy if this is the last use of an implicit by-ref local.
    //
    if (opts.OptimizationEnabled())
    {
        GenTreeLclVar* const lcl = argx->IsImplicitByrefParameterValue(this);

        if (lcl != nullptr)
        {
            const unsigned       varNum           = lcl->GetLclNum();
            LclVarDsc* const     varDsc           = lvaGetDesc(varNum);
            const unsigned short totalAppearances = varDsc->lvRefCnt(RCS_EARLY);

            // We don't have liveness so we rely on other indications of last use.
            //
            // We handle these cases:
            //
            // * (must not copy) If the call is a tail call, the use is a last use.
            //   We must skip the copy if we have a fast tail call.
            //
            // * (may not copy) if the call is noreturn, the use is a last use.
            //   We also check for just one reference here as we are not doing
            //   alias analysis of the call's parameters, or checking if the call
            //   site is not within some try region.
            //
            // * (may not copy) if there is exactly one use of the local in the method,
            //   and the call is not in loop, this is a last use.
            //
            const bool isTailCallLastUse = call->IsTailCall();
            const bool isCallLastUse     = (totalAppearances == 1) && !fgMightHaveLoop();
            const bool isNoReturnLastUse = (totalAppearances == 1) && call->IsNoReturn();
            if (isTailCallLastUse || isCallLastUse || isNoReturnLastUse)
            {
                varDsc->setLvRefCnt(0, RCS_EARLY);
                args->SetNode(lcl);
                assert(argEntry->GetNode() == lcl);

                JITDUMP("did not need to make outgoing copy for last use of implicit byref V%2d\n", varNum);
                return;
            }
        }
    }

    JITDUMP("making an outgoing copy for struct arg\n");

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
        if (call->IsVarargs())
        {
            lvaSetStructUsedAsVarArg(tmp);
        }

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
    GenTree* dest = gtNewLclvNode(tmp, lvaTable[tmp].lvType);
    dest->gtFlags |= (GTF_DONT_CSE | GTF_VAR_DEF); // This is a def of the local, "entire" by construction.

    if (argx->gtOper == GT_OBJ)
    {
        argx->gtFlags &= ~(GTF_ALL_EFFECT) | (argx->AsBlk()->Addr()->gtFlags & GTF_ALL_EFFECT);
        argx->SetIndirExceptionFlags(this);
    }
    else
    {
        argx->gtFlags |= GTF_DONT_CSE;
    }

    // Copy the valuetype to the temp
    GenTree* copyBlk = gtNewBlkOpNode(dest, argx, false /* not volatile */, true /* copyBlock */);
    copyBlk          = fgMorphCopyBlock(copyBlk);

#if FEATURE_FIXED_OUT_ARGS

    // Do the copy early, and evalute the temp later (see EvalArgsToTemps)
    // When on Unix create LCL_FLD for structs passed in more than one registers. See fgMakeTmpArgNode
    GenTree* arg = copyBlk;

#else // FEATURE_FIXED_OUT_ARGS

    // Structs are always on the stack, and thus never need temps
    // so we have to put the copy and temp all into one expression.
    argEntry->tmpNum = tmp;
    GenTree* arg     = fgMakeTmpArgNode(argEntry);

    // Change the expression to "(tmp=val),tmp"
    arg = gtNewOperNode(GT_COMMA, arg->TypeGet(), copyBlk, arg);

#endif // FEATURE_FIXED_OUT_ARGS

    args->SetNode(arg);
    call->fgArgInfo->EvalToTmp(argEntry, tmp, arg);

    return;
}

#ifdef TARGET_ARM
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

#endif // TARGET_ARM

//****************************************************************************
//  fgFixupStructReturn:
//    The companion to impFixupCallStructReturn.  Now that the importer is done
//    change the gtType to the precomputed native return type
//    requires that callNode currently has a struct type
//
void Compiler::fgFixupStructReturn(GenTree* callNode)
{
    if (!compDoOldStructRetyping())
    {
        return;
    }
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

        if (!varTypeIsStruct(returnType))
        {
            // Widen the primitive type if necessary
            returnType = genActualType(returnType);
        }
        call->gtType = returnType;
    }

#if FEATURE_MULTIREG_RET
    // Either we don't have a struct now or if struct, then it is a struct returned in regs or in return buffer.
    assert((call->gtType != TYP_STRUCT) || call->HasMultiRegRetVal() || callHasRetBuffArg);
#else // !FEATURE_MULTIREG_RET
    // No more struct returns
    assert(call->TypeGet() != TYP_STRUCT);
#endif

#if !defined(UNIX_AMD64_ABI)
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

void Compiler::fgMoveOpsLeft(GenTree* tree)
{
    GenTree*   op1;
    GenTree*   op2;
    genTreeOps oper;

    do
    {
        op1  = tree->AsOp()->gtOp1;
        op2  = tree->AsOp()->gtOp2;
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

        GenTree* ad1 = op2->AsOp()->gtOp1;
        GenTree* ad2 = op2->AsOp()->gtOp2;

        // Compiler::optOptimizeBools() can create GT_OR of two GC pointers yeilding a GT_INT
        // We can not reorder such GT_OR trees
        //
        if (varTypeIsGC(ad1->TypeGet()) != varTypeIsGC(op2->TypeGet()))
        {
            break;
        }

        // Don't split up a byref calculation and create a new byref. E.g.,
        // [byref]+ (ref, [int]+ (int, int)) => [byref]+ ([byref]+ (ref, int), int).
        // Doing this transformation could create a situation where the first
        // addition (that is, [byref]+ (ref, int) ) creates a byref pointer that
        // no longer points within the ref object. If a GC happens, the byref won't
        // get updated. This can happen, for instance, if one of the int components
        // is negative. It also requires the address generation be in a fully-interruptible
        // code region.
        //
        if (varTypeIsGC(op1->TypeGet()) && op2->TypeGet() == TYP_I_IMPL)
        {
            assert(varTypeIsGC(tree->TypeGet()) && (oper == GT_ADD));
            break;
        }

        /* Change "(x op (y op z))" to "(x op y) op z" */
        /* ie.    "(op1 op (ad1 op ad2))" to "(op1 op ad1) op ad2" */

        GenTree* new_op1 = op2;

        new_op1->AsOp()->gtOp1 = op1;
        new_op1->AsOp()->gtOp2 = ad1;

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

        tree->AsOp()->gtOp1 = new_op1;
        tree->AsOp()->gtOp2 = ad2;

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

void Compiler::fgSetRngChkTarget(GenTree* tree, bool delay)
{
    if (tree->OperIsBoundsCheck())
    {
        GenTreeBoundsChk* const boundsChk = tree->AsBoundsChk();
        BasicBlock* const       failBlock = fgSetRngChkTargetInner(boundsChk->gtThrowKind, delay);
        if (failBlock != nullptr)
        {
            boundsChk->gtIndRngFailBB = failBlock;
        }
    }
    else if (tree->OperIs(GT_INDEX_ADDR))
    {
        GenTreeIndexAddr* const indexAddr = tree->AsIndexAddr();
        BasicBlock* const       failBlock = fgSetRngChkTargetInner(SCK_RNGCHK_FAIL, delay);
        if (failBlock != nullptr)
        {
            indexAddr->gtIndRngFailBB = failBlock;
        }
    }
    else
    {
        noway_assert(tree->OperIs(GT_ARR_ELEM, GT_ARR_INDEX));
        fgSetRngChkTargetInner(SCK_RNGCHK_FAIL, delay);
    }
}

BasicBlock* Compiler::fgSetRngChkTargetInner(SpecialCodeKind kind, bool delay)
{
    if (opts.MinOpts())
    {
        delay = false;
    }

    if (!opts.compDbgCode)
    {
        if (!delay && !compIsForInlining())
        {
            // Create/find the appropriate "range-fail" label
            return fgRngChkTarget(compCurBB, kind);
        }
    }

    return nullptr;
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

GenTree* Compiler::fgMorphArrayIndex(GenTree* tree)
{
    noway_assert(tree->gtOper == GT_INDEX);
    GenTreeIndex*        asIndex        = tree->AsIndex();
    var_types            elemTyp        = asIndex->TypeGet();
    unsigned             elemSize       = asIndex->gtIndElemSize;
    CORINFO_CLASS_HANDLE elemStructType = asIndex->gtStructElemClass;

    noway_assert(elemTyp != TYP_STRUCT || elemStructType != nullptr);

    // Fold "cns_str"[cns_index] to ushort constant
    if (opts.OptimizationEnabled() && asIndex->Arr()->OperIs(GT_CNS_STR) && asIndex->Index()->IsIntCnsFitsInI32())
    {
        const int cnsIndex = static_cast<int>(asIndex->Index()->AsIntConCommon()->IconValue());
        if (cnsIndex >= 0)
        {
            int     length;
            LPCWSTR str = info.compCompHnd->getStringLiteral(asIndex->Arr()->AsStrCon()->gtScpHnd,
                                                             asIndex->Arr()->AsStrCon()->gtSconCPX, &length);
            if ((cnsIndex < length) && (str != nullptr))
            {
                GenTree* cnsCharNode = gtNewIconNode(str[cnsIndex], elemTyp);
                INDEBUG(cnsCharNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);
                return cnsCharNode;
            }
        }
    }

#ifdef FEATURE_SIMD
    if (featureSIMD && varTypeIsStruct(elemTyp) && structSizeMightRepresentSIMDType(elemSize))
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

    // Set up the array length's offset into lenOffs
    // And    the first element's offset into elemOffs
    ssize_t lenOffs;
    ssize_t elemOffs;
    if (tree->gtFlags & GTF_INX_STRING_LAYOUT)
    {
        lenOffs  = OFFSETOF__CORINFO_String__stringLen;
        elemOffs = OFFSETOF__CORINFO_String__chars;
        tree->gtFlags &= ~GTF_INX_STRING_LAYOUT; // Clear this flag as it is used for GTF_IND_VOLATILE
    }
    else
    {
        // We have a standard array
        lenOffs  = OFFSETOF__CORINFO_Array__length;
        elemOffs = OFFSETOF__CORINFO_Array__data;
    }

    // In minopts, we expand GT_INDEX to GT_IND(GT_INDEX_ADDR) in order to minimize the size of the IR. As minopts
    // compilation time is roughly proportional to the size of the IR, this helps keep compilation times down.
    // Furthermore, this representation typically saves on code size in minopts w.r.t. the complete expansion
    // performed when optimizing, as it does not require LclVar nodes (which are always stack loads/stores in
    // minopts).
    //
    // When we *are* optimizing, we fully expand GT_INDEX to:
    // 1. Evaluate the array address expression and store the result in a temp if the expression is complex or
    //    side-effecting.
    // 2. Evaluate the array index expression and store the result in a temp if the expression is complex or
    //    side-effecting.
    // 3. Perform an explicit bounds check: GT_ARR_BOUNDS_CHK(index, GT_ARR_LENGTH(array))
    // 4. Compute the address of the element that will be accessed:
    //    GT_ADD(GT_ADD(array, firstElementOffset), GT_MUL(index, elementSize))
    // 5. Dereference the address with a GT_IND.
    //
    // This expansion explicitly exposes the bounds check and the address calculation to the optimizer, which allows
    // for more straightforward bounds-check removal, CSE, etc.
    if (opts.MinOpts())
    {
        GenTree* const array = fgMorphTree(asIndex->Arr());
        GenTree* const index = fgMorphTree(asIndex->Index());

        GenTreeIndexAddr* const indexAddr =
            new (this, GT_INDEX_ADDR) GenTreeIndexAddr(array, index, elemTyp, elemStructType, elemSize,
                                                       static_cast<unsigned>(lenOffs), static_cast<unsigned>(elemOffs));
        indexAddr->gtFlags |= (array->gtFlags | index->gtFlags) & GTF_ALL_EFFECT;

        // Mark the indirection node as needing a range check if necessary.
        // Note this will always be true unless JitSkipArrayBoundCheck() is used
        if ((indexAddr->gtFlags & GTF_INX_RNGCHK) != 0)
        {
            fgSetRngChkTarget(indexAddr);
        }

        // Change `tree` into an indirection and return.
        tree->ChangeOper(GT_IND);
        GenTreeIndir* const indir = tree->AsIndir();
        indir->Addr()             = indexAddr;
        bool canCSE               = indir->CanCSE();
        indir->gtFlags            = GTF_IND_ARR_INDEX | (indexAddr->gtFlags & GTF_ALL_EFFECT);
        if (!canCSE)
        {
            indir->SetDoNotCSE();
        }

#ifdef DEBUG
        indexAddr->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG

        return indir;
    }

    GenTree* arrRef = asIndex->Arr();
    GenTree* index  = asIndex->Index();

    bool chkd = ((tree->gtFlags & GTF_INX_RNGCHK) != 0); // if false, range checking will be disabled
    bool nCSE = ((tree->gtFlags & GTF_DONT_CSE) != 0);

    GenTree* arrRefDefn = nullptr; // non-NULL if we need to allocate a temp for the arrRef expression
    GenTree* indexDefn  = nullptr; // non-NULL if we need to allocate a temp for the index expression
    GenTree* bndsChk    = nullptr;

    // If we're doing range checking, introduce a GT_ARR_BOUNDS_CHECK node for the address.
    if (chkd)
    {
        GenTree* arrRef2 = nullptr; // The second copy will be used in array address expression
        GenTree* index2  = nullptr;

        // If the arrRef or index expressions involves an assignment, a call or reads from global memory,
        // then we *must* allocate a temporary in which to "localize" those values, to ensure that the
        // same values are used in the bounds check and the actual dereference.
        // Also we allocate the temporary when the expresion is sufficiently complex/expensive.
        //
        // Note that if the expression is a GT_FIELD, it has not yet been morphed so its true complexity is
        // not exposed. Without that condition there are cases of local struct fields that were previously,
        // needlessly, marked as GTF_GLOB_REF, and when that was fixed, there were some regressions that
        // were mostly ameliorated by adding this condition.
        //
        // Likewise, allocate a temporary if the expression is a GT_LCL_FLD node. These used to be created
        // after fgMorphArrayIndex from GT_FIELD trees so this preserves the existing behavior. This is
        // perhaps a decision that should be left to CSE but FX diffs show that it is slightly better to
        // do this here.

        if ((arrRef->gtFlags & (GTF_ASG | GTF_CALL | GTF_GLOB_REF)) ||
            gtComplexityExceeds(&arrRef, MAX_ARR_COMPLEXITY) || arrRef->OperIs(GT_FIELD, GT_LCL_FLD))
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

        if ((index->gtFlags & (GTF_ASG | GTF_CALL | GTF_GLOB_REF)) || gtComplexityExceeds(&index, MAX_ARR_COMPLEXITY) ||
            index->OperIs(GT_FIELD, GT_LCL_FLD))
        {
            unsigned indexTmpNum = lvaGrabTemp(true DEBUGARG("index expr"));
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

#ifdef TARGET_64BIT
        // The CLI Spec allows an array to be indexed by either an int32 or a native int.  In the case
        // of a 64 bit architecture this means the array index can potentially be a TYP_LONG, so for this case,
        // the comparison will have to be widen to 64 bits.
        if (index->TypeGet() == TYP_I_IMPL)
        {
            bndsChkType = TYP_I_IMPL;
        }
#endif // TARGET_64BIT

        GenTree* arrLen = gtNewArrLen(TYP_INT, arrRef, (int)lenOffs, compCurBB);

        if (bndsChkType != TYP_INT)
        {
            arrLen = gtNewCastNode(bndsChkType, arrLen, false, bndsChkType);
        }

        GenTreeBoundsChk* arrBndsChk = new (this, GT_ARR_BOUNDS_CHECK)
            GenTreeBoundsChk(GT_ARR_BOUNDS_CHECK, TYP_VOID, index, arrLen, SCK_RNGCHK_FAIL);

        bndsChk = arrBndsChk;

        // Now we'll switch to using the second copies for arrRef and index
        // to compute the address expression

        arrRef = arrRef2;
        index  = index2;
    }

    // Create the "addr" which is "*(arrRef + ((index * elemSize) + elemOffs))"

    GenTree* addr;

#ifdef TARGET_64BIT
    // Widen 'index' on 64-bit targets
    if (index->TypeGet() != TYP_I_IMPL)
    {
        if (index->OperGet() == GT_CNS_INT)
        {
            index->gtType = TYP_I_IMPL;
        }
        else
        {
            index = gtNewCastNode(TYP_I_IMPL, index, false, TYP_I_IMPL);
        }
    }
#endif // TARGET_64BIT

    /* Scale the index value if necessary */
    if (elemSize > 1)
    {
        GenTree* size = gtNewIconNode(elemSize, TYP_I_IMPL);

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

    // Be careful to only create the byref pointer when the full index expression is added to the array reference.
    // We don't want to create a partial byref address expression that doesn't include the full index offset:
    // a byref must point within the containing object. It is dangerous (especially when optimizations come into
    // play) to create a "partial" byref that doesn't point exactly to the correct object; there is risk that
    // the partial byref will not point within the object, and thus not get updated correctly during a GC.
    // This is mostly a risk in fully-interruptible code regions.
    //
    // NOTE: the tree form created here is pattern matched by optExtractArrIndex(), so changes here must
    // be reflected there.

    /* Add the first element's offset */

    GenTree* cns = gtNewIconNode(elemOffs, TYP_I_IMPL);

    addr = gtNewOperNode(GT_ADD, TYP_I_IMPL, addr, cns);

    /* Add the object ref to the element's offset */

    addr = gtNewOperNode(GT_ADD, TYP_BYREF, arrRef, addr);

    assert(((tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE) != 0) ||
           (GenTree::s_gtNodeSizes[GT_IND] == TREE_NODE_SZ_SMALL));

    // Change the orginal GT_INDEX node into a GT_IND node
    tree->SetOper(GT_IND);

    // If the index node is a floating-point type, notify the compiler
    // we'll potentially use floating point registers at the time of codegen.
    if (varTypeUsesFloatReg(tree->gtType))
    {
        this->compFloatingPointUsed = true;
    }

    // We've now consumed the GTF_INX_RNGCHK, and the node
    // is no longer a GT_INDEX node.
    tree->gtFlags &= ~GTF_INX_RNGCHK;

    tree->AsOp()->gtOp1 = addr;

    // This is an array index expression.
    tree->gtFlags |= GTF_IND_ARR_INDEX;

    // If there's a bounds check, the indir won't fault.
    if (bndsChk)
    {
        tree->gtFlags |= GTF_IND_NONFAULTING;
    }
    else
    {
        tree->gtFlags |= GTF_EXCEPT;
    }

    if (nCSE)
    {
        tree->gtFlags |= GTF_DONT_CSE;
    }

    // Store information about it.
    GetArrayInfoMap()->Set(tree, ArrayInfo(elemTyp, elemSize, (int)elemOffs, elemStructType));

    // Remember this 'indTree' that we just created, as we still need to attach the fieldSeq information to it.

    GenTree* indTree = tree;

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
    GenTree* arrElem = tree->gtEffectiveVal();

    if (fgIsCommaThrow(tree))
    {
        if ((arrElem != indTree) ||         // A new tree node may have been created
            (indTree->OperGet() != GT_IND)) // The GT_IND may have been changed to a GT_CNS_INT
        {
            return tree; // Just return the Comma-Throw, don't try to attach the fieldSeq info, etc..
        }
    }

    assert(!fgGlobalMorph || (arrElem->gtDebugFlags & GTF_DEBUG_NODE_MORPHED));

    addr = arrElem->AsOp()->gtOp1;

    assert(addr->TypeGet() == TYP_BYREF);

    GenTree* cnsOff = nullptr;
    if (addr->OperGet() == GT_ADD)
    {
        assert(addr->TypeGet() == TYP_BYREF);
        assert(addr->AsOp()->gtOp1->TypeGet() == TYP_REF);

        addr = addr->AsOp()->gtOp2;

        // Look for the constant [#FirstElem] node here, or as the RHS of an ADD.

        if (addr->gtOper == GT_CNS_INT)
        {
            cnsOff = addr;
            addr   = nullptr;
        }
        else
        {
            if ((addr->OperGet() == GT_ADD) && (addr->AsOp()->gtOp2->gtOper == GT_CNS_INT))
            {
                cnsOff = addr->AsOp()->gtOp2;
                addr   = addr->AsOp()->gtOp1;
            }

            // Label any constant array index contributions with #ConstantIndex and any LclVars with GTF_VAR_ARR_INDEX
            addr->LabelIndex(this);
        }
    }
    else if (addr->OperGet() == GT_CNS_INT)
    {
        cnsOff = addr;
    }

    FieldSeqNode* firstElemFseq = GetFieldSeqStore()->CreateSingleton(FieldSeqStore::FirstElemPseudoField);

    if ((cnsOff != nullptr) && (cnsOff->AsIntCon()->gtIconVal == elemOffs))
    {
        // Assign it the [#FirstElem] field sequence
        //
        cnsOff->AsIntCon()->gtFieldSeq = firstElemFseq;
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
            fgAddFieldSeqForZeroOffset(addr, fieldSeq);
        }
        else
        {
            cnsOff->AsIntCon()->gtFieldSeq = fieldSeq;
        }
    }

    return tree;
}

#ifdef TARGET_X86
/*****************************************************************************
 *
 *  Wrap fixed stack arguments for varargs functions to go through varargs
 *  cookie to access them, except for the cookie itself.
 *
 * Non-x86 platforms are allowed to access all arguments directly
 * so we don't need this code.
 *
 */
GenTree* Compiler::fgMorphStackArgForVarArgs(unsigned lclNum, var_types varType, unsigned lclOffs)
{
    /* For the fixed stack arguments of a varargs function, we need to go
        through the varargs cookies to access them, except for the
        cookie itself */

    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varDsc->lvIsParam && !varDsc->lvIsRegArg && lclNum != lvaVarargsHandleArg)
    {
        // Create a node representing the local pointing to the base of the args
        GenTree* ptrArg =
            gtNewOperNode(GT_SUB, TYP_I_IMPL, gtNewLclvNode(lvaVarargsBaseOfStkArgs, TYP_I_IMPL),
                          gtNewIconNode(varDsc->lvStkOffs - codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES -
                                        lclOffs));

        // Access the argument through the local
        GenTree* tree;
        if (varTypeIsStruct(varType))
        {
            tree = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, ptrArg, typGetBlkLayout(varDsc->lvExactSize));
        }
        else
        {
            tree = gtNewOperNode(GT_IND, varType, ptrArg);
        }
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

GenTree* Compiler::fgMorphLocalVar(GenTree* tree, bool forceRemorph)
{
    assert(tree->gtOper == GT_LCL_VAR);

    unsigned   lclNum  = tree->AsLclVarCommon()->GetLclNum();
    var_types  varType = lvaGetRealType(lclNum);
    LclVarDsc* varDsc  = &lvaTable[lclNum];

    if (varDsc->lvAddrExposed)
    {
        tree->gtFlags |= GTF_GLOB_REF;
    }

#ifdef TARGET_X86
    if (info.compIsVarArgs)
    {
        GenTree* newTree = fgMorphStackArgForVarArgs(lclNum, varType, 0);
        if (newTree != nullptr)
        {
            if (newTree->OperIsBlk() && ((tree->gtFlags & GTF_VAR_DEF) == 0))
            {
                newTree->SetOper(GT_IND);
            }
            return newTree;
        }
    }
#endif // TARGET_X86

    /* If not during the global morphing phase bail */

    if (!fgGlobalMorph && !forceRemorph)
    {
        return tree;
    }

    bool varAddr = (tree->gtFlags & GTF_DONT_CSE) != 0;

    noway_assert(!(tree->gtFlags & GTF_VAR_DEF) || varAddr); // GTF_VAR_DEF should always imply varAddr

    if (!varAddr && varTypeIsSmall(varDsc->TypeGet()) && varDsc->lvNormalizeOnLoad())
    {
#if LOCAL_ASSERTION_PROP
        /* Assertion prop can tell us to omit adding a cast here */
        if (optLocalAssertionProp && optAssertionIsSubrange(tree, TYP_INT, varType, apFull) != NO_ASSERTION_INDEX)
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
        tree = gtNewCastNode(TYP_INT, tree, false, varType);
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

GenTree* Compiler::fgMorphField(GenTree* tree, MorphAddrContext* mac)
{
    assert(tree->gtOper == GT_FIELD);

    CORINFO_FIELD_HANDLE symHnd          = tree->AsField()->gtFldHnd;
    unsigned             fldOffset       = tree->AsField()->gtFldOffset;
    GenTree*             objRef          = tree->AsField()->gtFldObj;
    bool                 fieldMayOverlap = false;
    bool                 objIsLocal      = false;

    if (fgGlobalMorph && (objRef != nullptr) && (objRef->gtOper == GT_ADDR))
    {
        // Make sure we've checked if 'objRef' is an address of an implicit-byref parameter.
        // If it is, fgMorphImplicitByRefArgs may change it do a different opcode, which the
        // simd field rewrites are sensitive to.
        fgMorphImplicitByRefArgs(objRef);
    }

    noway_assert(((objRef != nullptr) && (objRef->IsLocalAddrExpr() != nullptr)) ||
                 ((tree->gtFlags & GTF_GLOB_REF) != 0));

    if (tree->AsField()->gtFldMayOverlap)
    {
        fieldMayOverlap = true;
        // Reset the flag because we may reuse the node.
        tree->AsField()->gtFldMayOverlap = false;
    }

#ifdef FEATURE_SIMD
    // if this field belongs to simd struct, translate it to simd intrinsic.
    if (mac == nullptr)
    {
        GenTree* newTree = fgMorphFieldToSIMDIntrinsicGet(tree);
        if (newTree != tree)
        {
            newTree = fgMorphSmpOp(newTree);
            return newTree;
        }
    }
    else if ((objRef != nullptr) && (objRef->OperGet() == GT_ADDR) && varTypeIsSIMD(objRef->gtGetOp1()))
    {
        GenTreeLclVarCommon* lcl = objRef->IsLocalAddrExpr();
        if (lcl != nullptr)
        {
            lvaSetVarDoNotEnregister(lcl->GetLclNum() DEBUGARG(DNER_LocalField));
        }
    }
#endif

    /* Is this an instance data member? */

    if (objRef)
    {
        GenTree* addr;
        objIsLocal = objRef->IsLocal();

        if (tree->gtFlags & GTF_IND_TLS_REF)
        {
            NO_WAY("instance field can not be a TLS ref.");
        }

        /* We'll create the expression "*(objRef + mem_offs)" */

        noway_assert(varTypeIsGC(objRef->TypeGet()) || objRef->TypeGet() == TYP_I_IMPL);

        /*
            Now we have a tree like this:

                                  +--------------------+
                                  |      GT_FIELD      |   tree
                                  +----------+---------+
                                             |
                              +--------------+-------------+
                              |   tree->AsField()->gtFldObj   |
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

        GenTree* comma = nullptr;

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

        bool addExplicitNullCheck = false;

        // Implicit byref locals are never null.
        if (!((objRef->gtOper == GT_LCL_VAR) && lvaIsImplicitByRefLocal(objRef->AsLclVarCommon()->GetLclNum())))
        {
            // If the objRef is a GT_ADDR node, it, itself, never requires null checking.  The expression
            // whose address is being taken is either a local or static variable, whose address is necessarily
            // non-null, or else it is a field dereference, which will do its own bounds checking if necessary.
            if (objRef->gtOper != GT_ADDR && (mac->m_kind == MACK_Addr || mac->m_kind == MACK_Ind))
            {
                if (!mac->m_allConstantOffsets || fgIsBigOffset(mac->m_totalOffset + fldOffset))
                {
                    addExplicitNullCheck = true;
                }
                else
                {
                    // In R2R mode the field offset for some fields may change when the code
                    // is loaded. So we can't rely on a zero offset here to suppress the null check.
                    //
                    // See GitHub issue #16454.
                    bool fieldHasChangeableOffset = false;

#ifdef FEATURE_READYTORUN_COMPILER
                    fieldHasChangeableOffset = (tree->AsField()->gtFieldLookup.addr != nullptr);
#endif

#if CONSERVATIVE_NULL_CHECK_BYREF_CREATION
                    addExplicitNullCheck = (mac->m_kind == MACK_Addr) &&
                                           ((mac->m_totalOffset + fldOffset > 0) || fieldHasChangeableOffset);
#else
                    addExplicitNullCheck = (objRef->gtType == TYP_BYREF && mac->m_kind == MACK_Addr &&
                                            ((mac->m_totalOffset + fldOffset > 0) || fieldHasChangeableOffset));
#endif
                }
            }
        }

        if (addExplicitNullCheck)
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
            GenTree* asg = nullptr;
            GenTree* nullchk;

            unsigned lclNum;

            if (objRef->gtOper != GT_LCL_VAR)
            {
                lclNum = fgGetBigOffsetMorphingTemp(genActualType(objRef->TypeGet()));

                // Create the "asg" node
                asg = gtNewTempAssign(lclNum, objRef);
            }
            else
            {
                lclNum = objRef->AsLclVarCommon()->GetLclNum();
            }

            GenTree* lclVar = gtNewLclvNode(lclNum, objRefType);
            nullchk         = gtNewNullCheck(lclVar, compCurBB);

            nullchk->gtFlags |= GTF_DONT_CSE; // Don't try to create a CSE for these TYP_BYTE indirections

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
        }
        else
        {
            addr = objRef;
        }

#ifdef FEATURE_READYTORUN_COMPILER
        if (tree->AsField()->gtFieldLookup.addr != nullptr)
        {
            GenTree* offsetNode = nullptr;
            if (tree->AsField()->gtFieldLookup.accessType == IAT_PVALUE)
            {
                offsetNode = gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)tree->AsField()->gtFieldLookup.addr,
                                                      GTF_ICON_FIELD_HDL, false);
#ifdef DEBUG
                offsetNode->gtGetOp1()->AsIntCon()->gtTargetHandle = (size_t)symHnd;
#endif
            }
            else
            {
                noway_assert(!"unexpected accessType for R2R field access");
            }

            var_types addType = (objRefType == TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
            addr              = gtNewOperNode(GT_ADD, addType, addr, offsetNode);
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
        tree->AsOp()->gtOp1 = addr;

        tree->SetIndirExceptionFlags(this);

        if (addExplicitNullCheck)
        {
            //
            // Create "comma2" node and link it to "tree".
            //
            GenTree* comma2;
            comma2 = gtNewOperNode(GT_COMMA,
                                   addr->TypeGet(), // The type of "comma2" node is the same as the type of "addr" node.
                                   comma, addr);
            tree->AsOp()->gtOp1 = comma2;
        }

#ifdef DEBUG
        if (verbose)
        {
            if (addExplicitNullCheck)
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
            //                   / \.
            //                  /  CNS(fldOffset)
            //                 /
            //                /
            //               /
            //             IND(I_IMPL) == [Base of this DLL's TLS]
            //              |
            //             ADD(I_IMPL)
            //             / \.
            //            /   CNS(IdValue*4) or MUL
            //           /                      / \.
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
            GenTree* dllRef = nullptr;
            if (pIdAddr == nullptr)
            {
                if (IdValue != 0)
                {
                    dllRef = gtNewIconNode(IdValue * 4, TYP_I_IMPL);
                }
            }
            else
            {
                dllRef = gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)pIdAddr, GTF_ICON_STATIC_HDL, true);

                // Next we multiply by 4
                dllRef = gtNewOperNode(GT_MUL, TYP_I_IMPL, dllRef, gtNewIconNode(4, TYP_I_IMPL));
            }

#define WIN32_TLS_SLOTS (0x2C) // Offset from fs:[0] where the pointer to the slots resides

            // Mark this ICON as a TLS_HDL, codegen will use FS:[cns]

            GenTree* tlsRef = gtNewIconHandleNode(WIN32_TLS_SLOTS, GTF_ICON_TLS_HDL);

            // Translate GTF_FLD_INITCLASS to GTF_ICON_INITCLASS
            if ((tree->gtFlags & GTF_FLD_INITCLASS) != 0)
            {
                tree->gtFlags &= ~GTF_FLD_INITCLASS;
                tlsRef->gtFlags |= GTF_ICON_INITCLASS;
            }

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
                GenTree* fldOffsetNode = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, fldOffset, fieldSeq);

                /* Add the TLS static field offset to the address */

                tlsRef = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsRef, fldOffsetNode);
            }

            // Final indirect to get to actual value of TLS static field

            tree->SetOper(GT_IND);
            tree->AsOp()->gtOp1 = tlsRef;

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
#ifdef TARGET_64BIT
                if (IMAGE_REL_BASED_REL32 != eeGetRelocTypeHint(fldAddr))
                {
                    // The address is not directly addressible, so force it into a
                    // constant, so we handle it properly

                    GenTree* addr = gtNewIconHandleNode((size_t)fldAddr, GTF_ICON_STATIC_HDL);
                    addr->gtType  = TYP_I_IMPL;
                    FieldSeqNode* fieldSeq =
                        fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
                    addr->AsIntCon()->gtFieldSeq = fieldSeq;
                    // Translate GTF_FLD_INITCLASS to GTF_ICON_INITCLASS
                    if ((tree->gtFlags & GTF_FLD_INITCLASS) != 0)
                    {
                        tree->gtFlags &= ~GTF_FLD_INITCLASS;
                        addr->gtFlags |= GTF_ICON_INITCLASS;
                    }

                    tree->SetOper(GT_IND);
                    tree->AsOp()->gtOp1 = addr;

                    return fgMorphSmpOp(tree);
                }
                else
#endif // TARGET_64BIT
                {
                    // Only volatile or classinit could be set, and they map over
                    noway_assert((tree->gtFlags & ~(GTF_FLD_VOLATILE | GTF_FLD_INITCLASS | GTF_COMMON_MASK)) == 0);
                    static_assert_no_msg(GTF_FLD_VOLATILE == GTF_CLS_VAR_VOLATILE);
                    static_assert_no_msg(GTF_FLD_INITCLASS == GTF_CLS_VAR_INITCLASS);
                    tree->SetOper(GT_CLS_VAR);
                    tree->AsClsVar()->gtClsVarHnd = symHnd;
                    FieldSeqNode* fieldSeq =
                        fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
                    tree->AsClsVar()->gtFieldSeq = fieldSeq;
                }

                return tree;
            }
            else
            {
                GenTree* addr = gtNewIconHandleNode((size_t)pFldAddr, GTF_ICON_STATIC_HDL);

                // Translate GTF_FLD_INITCLASS to GTF_ICON_INITCLASS
                if ((tree->gtFlags & GTF_FLD_INITCLASS) != 0)
                {
                    tree->gtFlags &= ~GTF_FLD_INITCLASS;
                    addr->gtFlags |= GTF_ICON_INITCLASS;
                }

                // There are two cases here, either the static is RVA based,
                // in which case the type of the FIELD node is not a GC type
                // and the handle to the RVA is a TYP_I_IMPL.  Or the FIELD node is
                // a GC type and the handle to it is a TYP_BYREF in the GC heap
                // because handles to statics now go into the large object heap

                var_types handleTyp = (var_types)(varTypeIsGC(tree->TypeGet()) ? TYP_BYREF : TYP_I_IMPL);
                GenTree*  op1       = gtNewOperNode(GT_IND, handleTyp, addr);
                op1->gtFlags |= GTF_IND_INVARIANT;

                tree->SetOper(GT_IND);
                tree->AsOp()->gtOp1 = op1;
            }
        }
    }
    noway_assert(tree->gtOper == GT_IND);

    if (fldOffset == 0)
    {
        GenTree* addr = tree->AsOp()->gtOp1;

        // 'addr' may be a GT_COMMA. Skip over any comma nodes
        addr = addr->gtEffectiveVal();

#ifdef DEBUG
        if (verbose)
        {
            printf("\nBefore calling fgAddFieldSeqForZeroOffset:\n");
            gtDispTree(tree);
        }
#endif

        // We expect 'addr' to be an address at this point.
        assert(addr->TypeGet() == TYP_BYREF || addr->TypeGet() == TYP_I_IMPL || addr->TypeGet() == TYP_REF);

        // Since we don't make a constant zero to attach the field sequence to, associate it with the "addr" node.
        FieldSeqNode* fieldSeq =
            fieldMayOverlap ? FieldSeqStore::NotAField() : GetFieldSeqStore()->CreateSingleton(symHnd);
        fgAddFieldSeqForZeroOffset(addr, fieldSeq);
    }

    // Pass down the current mac; if non null we are computing an address
    GenTree* result = fgMorphSmpOp(tree, mac);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nFinal value of Compiler::fgMorphField after calling fgMorphSmpOp:\n");
        gtDispTree(result);
    }
#endif

    return result;
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
//    If unsuccessful, the transformations done in anticipation of a
//    possible inline are undone, and the candidate flag on the call
//    is cleared.

void Compiler::fgMorphCallInline(GenTreeCall* call, InlineResult* inlineResult)
{
    bool inliningFailed = false;

    // Is this call an inline candidate?
    if (call->IsInlineCandidate())
    {
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

            inliningFailed = true;

            // Clear the Inline Candidate flag so we can ensure later we tried
            // inlining all candidates.
            //
            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
        }
    }
    else
    {
        // This wasn't an inline candidate. So it must be a GDV candidate.
        assert(call->IsGuardedDevirtualizationCandidate());

        // We already know we can't inline this call, so don't even bother to try.
        inliningFailed = true;
    }

    // If we failed to inline (or didn't even try), do some cleanup.
    if (inliningFailed)
    {
        if (call->gtReturnType != TYP_VOID)
        {
            JITDUMP("Inlining [%06u] failed, so bashing " FMT_STMT " to NOP\n", dspTreeID(call), fgMorphStmt->GetID());

            // Detach the GT_CALL tree from the original statement by
            // hanging a "nothing" node to it. Later the "nothing" node will be removed
            // and the original GT_CALL tree will be picked up by the GT_RET_EXPR node.

            noway_assert(fgMorphStmt->GetRootNode() == call);
            fgMorphStmt->SetRootNode(gtNewNothingNode());
        }
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

    // Re-check this because guarded devirtualization may allow these through.
    if (gtIsRecursiveCall(call) && call->IsImplicitTailCall())
    {
        result->NoteFatal(InlineObservation::CALLSITE_IMPLICIT_REC_TAIL_CALL);
        return;
    }

    // impMarkInlineCandidate() is expected not to mark tail prefixed calls
    // and recursive tail calls as inline candidates.
    noway_assert(!call->IsTailPrefixedCall());
    noway_assert(!call->IsImplicitTailCall() || !gtIsRecursiveCall(call));

    //
    // Calling inlinee's compiler to inline the method.
    //

    unsigned startVars = lvaCount;

#ifdef DEBUG
    if (verbose)
    {
        printf("Expanding INLINE_CANDIDATE in statement ");
        printStmtID(fgMorphStmt);
        printf(" in " FMT_BB ":\n", compCurBB->bbNum);
        gtDispStmt(fgMorphStmt);
        if (call->IsImplicitTailCall())
        {
            printf("Note: candidate is implicit tail call\n");
        }
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
            new (&lvaTable[i], jitstd::placement_t()) LclVarDsc(); // call the constructor.
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

//------------------------------------------------------------------------
// fgCanFastTailCall: Check to see if this tail call can be optimized as epilog+jmp.
//
// Arguments:
//    callee - The callee to check
//    failReason - If this method returns false, the reason why. Can be nullptr.
//
// Return Value:
//    Returns true or false based on whether the callee can be fastTailCalled
//
// Notes:
//    This function is target specific and each target will make the fastTailCall
//    decision differently. See the notes below.
//
//
// Windows Amd64:
//    A fast tail call can be made whenever the number of callee arguments
//    is less than or equal to the number of caller arguments, or we have four
//    or fewer callee arguments. This is because, on Windows AMD64, each
//    argument uses exactly one register or one 8-byte stack slot. Thus, we only
//    need to count arguments, and not be concerned with the size of each
//    incoming or outgoing argument.
//
// Can fast tail call examples (amd64 Windows):
//
//    -- Callee will have all register arguments --
//    caller(int, int, int, int)
//    callee(int, int, float, int)
//
//    -- Callee requires stack space that is equal or less than the caller --
//    caller(struct, struct, struct, struct, struct, struct)
//    callee(int, int, int, int, int, int)
//
//    -- Callee requires stack space that is less than the caller --
//    caller(struct, double, struct, float, struct, struct)
//    callee(int, int, int, int, int)
//
//    -- Callee will have all register arguments --
//    caller(int)
//    callee(int, int, int, int)
//
// Cannot fast tail call examples (amd64 Windows):
//
//    -- Callee requires stack space that is larger than the caller --
//    caller(struct, double, struct, float, struct, struct)
//    callee(int, int, int, int, int, double, double, double)
//
//    -- Callee has a byref struct argument --
//    caller(int, int, int)
//    callee(struct(size 3 bytes))
//
// Unix Amd64 && Arm64:
//    A fastTailCall decision can be made whenever the callee's stack space is
//    less than or equal to the caller's stack space. There are many permutations
//    of when the caller and callee have different stack sizes if there are
//    structs being passed to either the caller or callee.
//
// Exceptions:
//    1) If the callee has structs which cannot be enregistered it will be
//    reported as cannot fast tail call. This is an implementation limitation
//    where the callee only is checked for non enregisterable structs. This is
//    tracked with https://github.com/dotnet/coreclr/issues/12644.
//
//    2) If the caller or callee has stack arguments and the callee has more
//    arguments then the caller it will be reported as cannot fast tail call.
//    This is due to a bug in LowerFastTailCall which assumes that
//    nCalleeArgs <= nCallerArgs, which is always true on Windows Amd64. This
//    is tracked with https://github.com/dotnet/coreclr/issues/12468.
//
//    3) If the callee has a 9 to 16 byte struct argument and the callee has
//    stack arguments, the decision will be to not fast tail call. This is
//    because before fgMorphArgs is done, the struct is unknown whether it
//    will be placed on the stack or enregistered. Therefore, the conservative
//    decision of do not fast tail call is taken. This limitations should be
//    removed if/when fgMorphArgs no longer depends on fgCanFastTailCall.
//
// Can fast tail call examples (amd64 Unix):
//
//    -- Callee will have all register arguments --
//    caller(int, int, int, int)
//    callee(int, int, float, int)
//
//    -- Callee requires stack space that is equal to the caller --
//    caller({ long, long }, { int, int }, { int }, { int }, { int }, { int }) -- 6 int register arguments, 16 byte
//    stack
//    space
//    callee(int, int, int, int, int, int, int, int) -- 6 int register arguments, 16 byte stack space
//
//    -- Callee requires stack space that is less than the caller --
//    caller({ long, long }, int, { long, long }, int, { long, long }, { long, long }) 6 int register arguments, 32 byte
//    stack
//    space
//    callee(int, int, int, int, int, int, { long, long } ) // 6 int register arguments, 16 byte stack space
//
//    -- Callee will have all register arguments --
//    caller(int)
//    callee(int, int, int, int)
//
// Cannot fast tail call examples (amd64 Unix):
//
//    -- Callee requires stack space that is larger than the caller --
//    caller(float, float, float, float, float, float, float, float) -- 8 float register arguments
//    callee(int, int, int, int, int, int, int, int) -- 6 int register arguments, 16 byte stack space
//
//    -- Callee has structs which cannot be enregistered (Implementation Limitation) --
//    caller(float, float, float, float, float, float, float, float, { double, double, double }) -- 8 float register
//    arguments, 24 byte stack space
//    callee({ double, double, double }) -- 24 bytes stack space
//
//    -- Callee requires stack space and has a struct argument >8 bytes and <16 bytes (Implementation Limitation) --
//    caller(int, int, int, int, int, int, { double, double, double }) -- 6 int register arguments, 24 byte stack space
//    callee(int, int, int, int, int, int, { int, int }) -- 6 int registers, 16 byte stack space
//
//    -- Caller requires stack space and nCalleeArgs > nCallerArgs (Bug) --
//    caller({ double, double, double, double, double, double }) // 48 byte stack
//    callee(int, int) -- 2 int registers

bool Compiler::fgCanFastTailCall(GenTreeCall* callee, const char** failReason)
{
#if FEATURE_FASTTAILCALL

    // To reach here means that the return types of the caller and callee are tail call compatible.
    // In the case of structs that can be returned in a register, compRetNativeType is set to the actual return type.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (callee->IsTailPrefixedCall())
    {
        var_types retType = (compDoOldStructRetyping() ? info.compRetNativeType : info.compRetType);
        assert(impTailCallRetTypeCompatible(retType, info.compMethodInfo->args.retTypeClass,
                                            (var_types)callee->gtReturnType, callee->gtRetClsHnd));
    }
#endif

    assert(!callee->AreArgsComplete());

    fgInitArgInfo(callee);

    fgArgInfo* argInfo = callee->fgArgInfo;

    size_t calleeArgStackSize = 0;
    size_t callerArgStackSize = info.compArgStackSize;

    for (unsigned index = 0; index < argInfo->ArgCount(); ++index)
    {
        fgArgTabEntry* arg = argInfo->GetArgEntry(index, false);

        calleeArgStackSize += arg->stackSize();
    }

    auto reportFastTailCallDecision = [&](const char* thisFailReason) {
        if (failReason != nullptr)
        {
            *failReason = thisFailReason;
        }

#ifdef DEBUG
        if ((JitConfig.JitReportFastTailCallDecisions()) == 1)
        {
            if (callee->gtCallType != CT_INDIRECT)
            {
                const char* methodName;

                methodName = eeGetMethodFullName(callee->gtCallMethHnd);

                printf("[Fast tailcall decision]: Caller: %s\n[Fast tailcall decision]: Callee: %s -- Decision: ",
                       info.compFullName, methodName);
            }
            else
            {
                printf("[Fast tailcall decision]: Caller: %s\n[Fast tailcall decision]: Callee: IndirectCall -- "
                       "Decision: ",
                       info.compFullName);
            }

            if (thisFailReason == nullptr)
            {
                printf("Will fast tailcall");
            }
            else
            {
                printf("Will not fast tailcall (%s)", thisFailReason);
            }

            printf(" (CallerArgStackSize: %d, CalleeArgStackSize: %d)\n\n", callerArgStackSize, calleeArgStackSize);
        }
        else
        {
            if (thisFailReason == nullptr)
            {
                JITDUMP("[Fast tailcall decision]: Will fast tailcall\n");
            }
            else
            {
                JITDUMP("[Fast tailcall decision]: Will not fast tailcall (%s)\n", thisFailReason);
            }
        }
#endif // DEBUG
    };

    if (!opts.compFastTailCalls)
    {
        reportFastTailCallDecision("Configuration doesn't allow fast tail calls");
        return false;
    }

    // Note on vararg methods:
    // If the caller is vararg method, we don't know the number of arguments passed by caller's caller.
    // But we can be sure that in-coming arg area of vararg caller would be sufficient to hold its
    // fixed args. Therefore, we can allow a vararg method to fast tail call other methods as long as
    // out-going area required for callee is bounded by caller's fixed argument space.
    //
    // Note that callee being a vararg method is not a problem since we can account the params being passed.
    //
    // We will currently decide to not fast tail call on Windows armarch if the caller or callee is a vararg
    // method. This is due to the ABI differences for native vararg methods for these platforms. There is
    // work required to shuffle arguments to the correct locations.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if (defined(TARGET_WINDOWS) && defined(TARGET_ARM)) || (defined(TARGET_WINDOWS) && defined(TARGET_ARM64))
    if (info.compIsVarArgs || callee->IsVarargs())
    {
        reportFastTailCallDecision("Fast tail calls with varargs not supported on Windows ARM/ARM64");
        return false;
    }
#endif // (defined(TARGET_WINDOWS) && defined(TARGET_ARM)) || defined(TARGET_WINDOWS) && defined(TARGET_ARM64))

    if (compLocallocUsed)
    {
        reportFastTailCallDecision("Localloc used");
        return false;
    }

#ifdef TARGET_AMD64
    // Needed for Jit64 compat.
    // In future, enabling fast tail calls from methods that need GS cookie
    // check would require codegen side work to emit GS cookie check before a
    // tail call.
    if (getNeedsGSSecurityCookie())
    {
        reportFastTailCallDecision("GS Security cookie check required");
        return false;
    }
#endif

    // If the NextCallReturnAddress intrinsic is used we should do normal calls.
    if (info.compHasNextCallRetAddr)
    {
        reportFastTailCallDecision("Uses NextCallReturnAddress intrinsic");
        return false;
    }

    if (callee->HasRetBufArg()) // RetBuf
    {
        // If callee has RetBuf param, caller too must have it.
        // Otherwise go the slow route.
        if (info.compRetBuffArg == BAD_VAR_NUM)
        {
            reportFastTailCallDecision("Callee has RetBuf but caller does not.");
            return false;
        }
    }

    // For a fast tail call the caller will use its incoming arg stack space to place
    // arguments, so if the callee requires more arg stack space than is available here
    // the fast tail call cannot be performed. This is common to all platforms.
    // Note that the GC'ness of on stack args need not match since the arg setup area is marked
    // as non-interruptible for fast tail calls.
    if (calleeArgStackSize > callerArgStackSize)
    {
        reportFastTailCallDecision("Not enough incoming arg space");
        return false;
    }

    // For Windows some struct parameters are copied on the local frame
    // and then passed by reference. We cannot fast tail call in these situation
    // as we need to keep our frame around.
    if (fgCallHasMustCopyByrefParameter(callee))
    {
        reportFastTailCallDecision("Callee has a byref parameter");
        return false;
    }

    reportFastTailCallDecision(nullptr);
    return true;
#else // FEATURE_FASTTAILCALL
    if (failReason)
        *failReason = "Fast tailcalls are not supported on this platform";
    return false;
#endif
}

//------------------------------------------------------------------------
// fgCallHasMustCopyByrefParameter: Check to see if this call has a byref parameter that
//                                  requires a struct copy in the caller.
//
// Arguments:
//    callee - The callee to check
//
// Return Value:
//    Returns true or false based on whether this call has a byref parameter that
//    requires a struct copy in the caller.

#if FEATURE_FASTTAILCALL
bool Compiler::fgCallHasMustCopyByrefParameter(GenTreeCall* callee)
{
    fgArgInfo* argInfo = callee->fgArgInfo;

    bool hasMustCopyByrefParameter = false;

    for (unsigned index = 0; index < argInfo->ArgCount(); ++index)
    {
        fgArgTabEntry* arg = argInfo->GetArgEntry(index, false);

        if (arg->isStruct)
        {
            if (arg->passedByRef)
            {
                // Generally a byref arg will block tail calling, as we have to
                // make a local copy of the struct for the callee.
                hasMustCopyByrefParameter = true;

                // If we're optimizing, we may be able to pass our caller's byref to our callee,
                // and so still be able to avoid a struct copy.
                if (opts.OptimizationEnabled())
                {
                    // First, see if this arg is an implicit byref param.
                    GenTreeLclVar* const lcl = arg->GetNode()->IsImplicitByrefParameterValue(this);

                    if (lcl != nullptr)
                    {
                        // Yes, the arg is an implicit byref param.
                        const unsigned   lclNum = lcl->GetLclNum();
                        LclVarDsc* const varDsc = lvaGetDesc(lcl);

                        // The param must not be promoted; if we've promoted, then the arg will be
                        // a local struct assembled from the promoted fields.
                        if (varDsc->lvPromoted)
                        {
                            JITDUMP("Arg [%06u] is promoted implicit byref V%02u, so no tail call\n",
                                    dspTreeID(arg->GetNode()), lclNum);
                        }
                        else
                        {
                            JITDUMP("Arg [%06u] is unpromoted implicit byref V%02u, seeing if we can still tail call\n",
                                    dspTreeID(arg->GetNode()), lclNum);

                            // We have to worry about introducing aliases if we bypass copying
                            // the struct at the call. We'll do some limited analysis to see if we
                            // can rule this out.
                            const unsigned argLimit = 6;

                            // If this is the only appearance of the byref in the method, then
                            // aliasing is not possible.
                            //
                            // If no other call arg refers to this byref, and no other arg is
                            // a pointer which could refer to this byref, we can optimize.
                            //
                            // We only check this for calls with small numbers of arguments,
                            // as the analysis cost will be quadratic.
                            //
                            if (varDsc->lvRefCnt(RCS_EARLY) == 1)
                            {
                                JITDUMP("... yes, arg is the only appearance of V%02u\n", lclNum);
                                hasMustCopyByrefParameter = false;
                            }
                            else if (argInfo->ArgCount() <= argLimit)
                            {
                                GenTree* interferingArg = nullptr;
                                for (unsigned index2 = 0; index2 < argInfo->ArgCount(); ++index2)
                                {
                                    if (index2 == index)
                                    {
                                        continue;
                                    }

                                    fgArgTabEntry* const arg2 = argInfo->GetArgEntry(index2, false);
                                    JITDUMP("... checking other arg [%06u]...\n", dspTreeID(arg2->GetNode()));
                                    DISPTREE(arg2->GetNode());

                                    // Do we pass 'lcl' more than once to the callee?
                                    if (arg2->isStruct && arg2->passedByRef)
                                    {
                                        GenTreeLclVarCommon* const lcl2 =
                                            arg2->GetNode()->IsImplicitByrefParameterValue(this);

                                        if ((lcl2 != nullptr) && (lclNum == lcl2->GetLclNum()))
                                        {
                                            // not copying would introduce aliased implicit byref structs
                                            // in the callee ... we can't optimize.
                                            interferingArg = arg2->GetNode();
                                            break;
                                        }
                                        else
                                        {
                                            JITDUMP("... arg refers to different implicit byref V%02u\n",
                                                    lcl2->GetLclNum());
                                            continue;
                                        }
                                    }

                                    // Do we pass a byref pointer which might point within 'lcl'?
                                    //
                                    // We can assume the 'lcl' is unaliased on entry to the
                                    // method, so the only way we can have an aliasing byref pointer at
                                    // the call is if 'lcl' is address taken/exposed in the method.
                                    //
                                    // Note even though 'lcl' is not promoted, we are in the middle
                                    // of the promote->rewrite->undo->(morph)->demote cycle, and so
                                    // might see references to promoted fields of 'lcl' that haven't yet
                                    // been demoted (see fgMarkDemotedImplicitByRefArgs).
                                    //
                                    // So, we also need to scan all 'lcl's fields, if any, to see if they
                                    // are exposed.
                                    //
                                    // When looking for aliases from other args, we check for both TYP_BYREF
                                    // and TYP_I_IMPL typed args here. Conceptually anything that points into
                                    // an implicit byref parameter should be TYP_BYREF, as these parameters could
                                    // refer to boxed heap locations (say if the method is invoked by reflection)
                                    // but there are some stack only structs (like typed references) where
                                    // the importer/runtime code uses TYP_I_IMPL, and fgInitArgInfo will
                                    // transiently retype all simple address-of implicit parameter args as
                                    // TYP_I_IMPL.
                                    //
                                    if ((arg2->argType == TYP_BYREF) || (arg2->argType == TYP_I_IMPL))
                                    {
                                        JITDUMP("...arg is a byref, must run an alias check\n");
                                        bool checkExposure = true;
                                        bool hasExposure   = false;

                                        // See if there is any way arg could refer to a parameter struct.
                                        GenTree* arg2Node = arg2->GetNode();
                                        if (arg2Node->OperIs(GT_LCL_VAR))
                                        {
                                            GenTreeLclVarCommon* arg2LclNode = arg2Node->AsLclVarCommon();
                                            assert(arg2LclNode->GetLclNum() != lclNum);
                                            LclVarDsc* arg2Dsc = lvaGetDesc(arg2LclNode);

                                            // Other params can't alias implicit byref params
                                            if (arg2Dsc->lvIsParam)
                                            {
                                                checkExposure = false;
                                            }
                                        }
                                        // Because we're checking TYP_I_IMPL above, at least
                                        // screen out obvious things that can't cause aliases.
                                        else if (arg2Node->IsIntegralConst())
                                        {
                                            checkExposure = false;
                                        }

                                        if (checkExposure)
                                        {
                                            JITDUMP(
                                                "... not sure where byref arg points, checking if V%02u is exposed\n",
                                                lclNum);
                                            // arg2 might alias arg, see if we've exposed
                                            // arg somewhere in the method.
                                            if (varDsc->lvHasLdAddrOp || varDsc->lvAddrExposed)
                                            {
                                                // Struct as a whole is exposed, can't optimize
                                                JITDUMP("... V%02u is exposed\n", lclNum);
                                                hasExposure = true;
                                            }
                                            else if (varDsc->lvFieldLclStart != 0)
                                            {
                                                // This is the promoted/undone struct case.
                                                //
                                                // The field start is actually the local number of the promoted local,
                                                // use it to enumerate the fields.
                                                const unsigned   promotedLcl    = varDsc->lvFieldLclStart;
                                                LclVarDsc* const promotedVarDsc = lvaGetDesc(promotedLcl);
                                                JITDUMP("...promoted-unpromoted case -- also checking exposure of "
                                                        "fields of V%02u\n",
                                                        promotedLcl);

                                                for (unsigned fieldIndex = 0; fieldIndex < promotedVarDsc->lvFieldCnt;
                                                     fieldIndex++)
                                                {
                                                    LclVarDsc* fieldDsc =
                                                        lvaGetDesc(promotedVarDsc->lvFieldLclStart + fieldIndex);

                                                    if (fieldDsc->lvHasLdAddrOp || fieldDsc->lvAddrExposed)
                                                    {
                                                        // Promoted and not yet demoted field is exposed, can't optimize
                                                        JITDUMP("... field V%02u is exposed\n",
                                                                promotedVarDsc->lvFieldLclStart + fieldIndex);
                                                        hasExposure = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (hasExposure)
                                        {
                                            interferingArg = arg2->GetNode();
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        JITDUMP("...arg is not a byref or implicit byref (%s)\n",
                                                varTypeName(arg2->GetNode()->TypeGet()));
                                    }
                                }

                                if (interferingArg != nullptr)
                                {
                                    JITDUMP("... no, arg [%06u] may alias with V%02u\n", dspTreeID(interferingArg),
                                            lclNum);
                                }
                                else
                                {
                                    JITDUMP("... yes, no other arg in call can alias V%02u\n", lclNum);
                                    hasMustCopyByrefParameter = false;
                                }
                            }
                            else
                            {
                                JITDUMP(" ... no, call has %u > %u args, alias analysis deemed too costly\n",
                                        argInfo->ArgCount(), argLimit);
                            }
                        }
                    }
                }

                if (hasMustCopyByrefParameter)
                {
                    // This arg requires a struct copy. No reason to keep scanning the remaining args.
                    break;
                }
            }
        }
    }

    return hasMustCopyByrefParameter;
}
#endif

//------------------------------------------------------------------------
// fgMorphPotentialTailCall: Attempt to morph a call that the importer has
// identified as a potential tailcall to an actual tailcall and return the
// placeholder node to use in this case.
//
// Arguments:
//    call - The call to morph.
//
// Return Value:
//    Returns a node to use if the call was morphed into a tailcall. If this
//    function returns a node the call is done being morphed and the new node
//    should be used. Otherwise the call will have been demoted to a regular call
//    and should go through normal morph.
//
// Notes:
//    This is called only for calls that the importer has already identified as
//    potential tailcalls. It will do profitability and legality checks and
//    classify which kind of tailcall we are able to (or should) do, along with
//    modifying the trees to perform that kind of tailcall.
//
GenTree* Compiler::fgMorphPotentialTailCall(GenTreeCall* call)
{
    // It should either be an explicit (i.e. tail prefixed) or an implicit tail call
    assert(call->IsTailPrefixedCall() ^ call->IsImplicitTailCall());

    // It cannot be an inline candidate
    assert(!call->IsInlineCandidate());

    auto failTailCall = [&](const char* reason, unsigned lclNum = BAD_VAR_NUM) {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nRejecting tail call in morph for call ");
            printTreeID(call);
            printf(": %s", reason);
            if (lclNum != BAD_VAR_NUM)
            {
                printf(" V%02u", lclNum);
            }
            printf("\n");
        }
#endif

        // for non user funcs, we have no handles to report
        info.compCompHnd->reportTailCallDecision(nullptr,
                                                 (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                                 call->IsTailPrefixedCall(), TAILCALL_FAIL, reason);

        // We have checked the candidate so demote.
        call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
#if FEATURE_TAILCALL_OPT
        call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
#endif
    };

    if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
    {
        failTailCall("Might turn into an intrinsic");
        return nullptr;
    }

    // Heuristic: regular calls to noreturn methods can sometimes be
    // merged, so if we have multiple such calls, we defer tail calling.
    //
    // TODO: re-examine this; now that we're merging before morph we
    // don't need to worry about interfering with merges.
    //
    if (call->IsNoReturn() && (optNoReturnCallCount > 1))
    {
        failTailCall("Defer tail calling throw helper; anticipating merge");
        return nullptr;
    }

#ifdef DEBUG
    if (opts.compGcChecks && (info.compRetType == TYP_REF))
    {
        failTailCall("COMPlus_JitGCChecks or stress might have interposed a call to CORINFO_HELP_CHECK_OBJ, "
                     "invalidating tailcall opportunity");
        return nullptr;
    }
#endif

    // We have to ensure to pass the incoming retValBuf as the
    // outgoing one. Using a temp will not do as this function will
    // not regain control to do the copy. This can happen when inlining
    // a tailcall which also has a potential tailcall in it: the IL looks
    // like we can do a tailcall, but the trees generated use a temp for the inlinee's
    // result. TODO-CQ: Fix this.
    if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        noway_assert(call->TypeGet() == TYP_VOID);
        GenTree* retValBuf = call->gtCallArgs->GetNode();
        if (retValBuf->gtOper != GT_LCL_VAR || retValBuf->AsLclVarCommon()->GetLclNum() != info.compRetBuffArg)
        {
            failTailCall("Need to copy return buffer");
            return nullptr;
        }
    }

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
    bool hasStructParam = false;
    for (unsigned varNum = 0; varNum < lvaCount; varNum++)
    {
        LclVarDsc* varDsc = lvaTable + varNum;
        // If the method is marked as an explicit tail call we will skip the
        // following three hazard checks.
        // We still must check for any struct parameters and set 'hasStructParam'
        // so that we won't transform the recursive tail call into a loop.
        //
        if (call->IsImplicitTailCall())
        {
            if (varDsc->lvHasLdAddrOp && !lvaIsImplicitByRefLocal(varNum))
            {
                failTailCall("Local address taken", varNum);
                return nullptr;
            }
            if (varDsc->lvAddrExposed)
            {
                if (lvaIsImplicitByRefLocal(varNum))
                {
                    // The address of the implicit-byref is a non-address use of the pointer parameter.
                }
                else if (varDsc->lvIsStructField && lvaIsImplicitByRefLocal(varDsc->lvParentLcl))
                {
                    // The address of the implicit-byref's field is likewise a non-address use of the pointer
                    // parameter.
                }
                else if (varDsc->lvPromoted && (lvaTable[varDsc->lvFieldLclStart].lvParentLcl != varNum))
                {
                    // This temp was used for struct promotion bookkeeping.  It will not be used, and will have
                    // its ref count and address-taken flag reset in fgMarkDemotedImplicitByRefArgs.
                    assert(lvaIsImplicitByRefLocal(lvaTable[varDsc->lvFieldLclStart].lvParentLcl));
                    assert(fgGlobalMorph);
                }
                else
                {
                    failTailCall("Local address taken", varNum);
                    return nullptr;
                }
            }
            if (varDsc->lvPromoted && varDsc->lvIsParam && !lvaIsImplicitByRefLocal(varNum))
            {
                failTailCall("Has Struct Promoted Param", varNum);
                return nullptr;
            }
            if (varDsc->lvPinned)
            {
                // A tail call removes the method from the stack, which means the pinning
                // goes away for the callee.  We can't allow that.
                failTailCall("Has Pinned Vars", varNum);
                return nullptr;
            }
        }

        if (varTypeIsStruct(varDsc->TypeGet()) && varDsc->lvIsParam)
        {
            hasStructParam = true;
            // This prevents transforming a recursive tail call into a loop
            // but doesn't prevent tail call optimization so we need to
            // look at the rest of parameters.
        }
    }

    if (!fgCheckStmtAfterTailCall())
    {
        failTailCall("Unexpected statements after the tail call");
        return nullptr;
    }

    const char* failReason      = nullptr;
    bool        canFastTailCall = fgCanFastTailCall(call, &failReason);

    CORINFO_TAILCALL_HELPERS tailCallHelpers;
    bool                     tailCallViaJitHelper = false;
    if (!canFastTailCall)
    {
        if (call->IsImplicitTailCall())
        {
            // Implicit or opportunistic tail calls are always dispatched via fast tail call
            // mechanism and never via tail call helper for perf.
            failTailCall(failReason);
            return nullptr;
        }

        assert(call->IsTailPrefixedCall());
        assert(call->tailCallInfo != nullptr);

        // We do not currently handle non-standard args except for VSD stubs.
        if (!call->IsVirtualStub() && call->HasNonStandardAddedArgs(this))
        {
            failTailCall(
                "Method with non-standard args passed in callee trash register cannot be tail called via helper");
            return nullptr;
        }

        // On x86 we have a faster mechanism than the general one which we use
        // in almost all cases. See fgCanTailCallViaJitHelper for more information.
        if (fgCanTailCallViaJitHelper())
        {
            tailCallViaJitHelper = true;
        }
        else
        {
            // Make sure we can get the helpers. We do this last as the runtime
            // will likely be required to generate these.
            CORINFO_RESOLVED_TOKEN* token = nullptr;
            CORINFO_SIG_INFO*       sig   = call->tailCallInfo->GetSig();
            unsigned                flags = 0;
            if (!call->tailCallInfo->IsCalli())
            {
                token = call->tailCallInfo->GetToken();
                if (call->tailCallInfo->IsCallvirt())
                {
                    flags |= CORINFO_TAILCALL_IS_CALLVIRT;
                }
            }

            if (call->gtCallThisArg != nullptr)
            {
                var_types thisArgType = call->gtCallThisArg->GetNode()->TypeGet();
                if (thisArgType != TYP_REF)
                {
                    flags |= CORINFO_TAILCALL_THIS_ARG_IS_BYREF;
                }
            }

            if (!info.compCompHnd->getTailCallHelpers(token, sig, (CORINFO_GET_TAILCALL_HELPERS_FLAGS)flags,
                                                      &tailCallHelpers))
            {
                failTailCall("Tail call help not available");
                return nullptr;
            }
        }
    }

    // Check if we can make the tailcall a loop.
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
        fastTailCallToLoop = true;
    }
#endif

    // Ok -- now we are committed to performing a tailcall. Report the decision.
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

    info.compCompHnd->reportTailCallDecision(nullptr,
                                             (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                             call->IsTailPrefixedCall(), tailCallResult, nullptr);
    // Now actually morph the call.
    compTailCallUsed = true;
    // This will prevent inlining this call.
    call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL;
    if (tailCallViaJitHelper)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL_VIA_JIT_HELPER;
    }

#if FEATURE_TAILCALL_OPT
    if (fastTailCallToLoop)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL_TO_LOOP;
    }
#endif

    // Mark that this is no longer a pending tailcall. We need to do this before
    // we call fgMorphCall again (which happens in the fast tailcall case) to
    // avoid recursing back into this method.
    call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
#if FEATURE_TAILCALL_OPT
    call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
#endif

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

    // This block is not longer any block's predecessor. If we end up
    // converting this tail call to a branch, we'll add appropriate
    // successor information then.
    fgRemoveBlockAsPred(compCurBB);

#if !FEATURE_TAILCALL_OPT_SHARED_RETURN
    // We enable shared-ret tail call optimization for recursive calls even if
    // FEATURE_TAILCALL_OPT_SHARED_RETURN is not defined.
    if (gtIsRecursiveCall(call))
#endif
    {
        // Many tailcalls will have call and ret in the same block, and thus be
        // BBJ_RETURN, but if the call falls through to a ret, and we are doing a
        // tailcall, change it here.
        compCurBB->bbJumpKind = BBJ_RETURN;
    }

    GenTree* stmtExpr = fgMorphStmt->GetRootNode();

#ifdef DEBUG
    // Tail call needs to be in one of the following IR forms
    //    Either a call stmt or
    //    GT_RETURN(GT_CALL(..)) or GT_RETURN(GT_CAST(GT_CALL(..)))
    //    var = GT_CALL(..) or var = (GT_CAST(GT_CALL(..)))
    //    GT_COMMA(GT_CALL(..), GT_NOP) or GT_COMMA(GT_CAST(GT_CALL(..)), GT_NOP)
    // In the above,
    //    GT_CASTS may be nested.
    genTreeOps stmtOper = stmtExpr->gtOper;
    if (stmtOper == GT_CALL)
    {
        assert(stmtExpr == call);
    }
    else
    {
        assert(stmtOper == GT_RETURN || stmtOper == GT_ASG || stmtOper == GT_COMMA);
        GenTree* treeWithCall;
        if (stmtOper == GT_RETURN)
        {
            treeWithCall = stmtExpr->gtGetOp1();
        }
        else if (stmtOper == GT_COMMA)
        {
            // Second operation must be nop.
            assert(stmtExpr->gtGetOp2()->IsNothingNode());
            treeWithCall = stmtExpr->gtGetOp1();
        }
        else
        {
            treeWithCall = stmtExpr->gtGetOp2();
        }

        // Peel off casts
        while (treeWithCall->gtOper == GT_CAST)
        {
            assert(!treeWithCall->gtOverflow());
            treeWithCall = treeWithCall->gtGetOp1();
        }

        assert(treeWithCall == call);
    }
#endif
    // Store the call type for later to introduce the correct placeholder.
    var_types origCallType = call->TypeGet();

    GenTree* result;
    if (!canFastTailCall && !tailCallViaJitHelper)
    {
        // For tailcall via CORINFO_TAILCALL_HELPERS we transform into regular
        // calls with (to the JIT) regular control flow so we do not need to do
        // much special handling.
        result = fgMorphTailCallViaHelpers(call, tailCallHelpers);
    }
    else
    {
        // Otherwise we will transform into something that does not return. For
        // fast tailcalls a "jump" and for tailcall via JIT helper a call to a
        // JIT helper that does not return. So peel off everything after the
        // call.
        Statement* nextMorphStmt = fgMorphStmt->GetNextStmt();
        JITDUMP("Remove all stmts after the call.\n");
        while (nextMorphStmt != nullptr)
        {
            Statement* stmtToRemove = nextMorphStmt;
            nextMorphStmt           = stmtToRemove->GetNextStmt();
            fgRemoveStmt(compCurBB, stmtToRemove);
        }

        bool     isRootReplaced = false;
        GenTree* root           = fgMorphStmt->GetRootNode();

        if (root != call)
        {
            JITDUMP("Replace root node [%06d] with [%06d] tail call node.\n", dspTreeID(root), dspTreeID(call));
            isRootReplaced = true;
            fgMorphStmt->SetRootNode(call);
        }

        // Avoid potential extra work for the return (for example, vzeroupper)
        call->gtType = TYP_VOID;

        // Do some target-specific transformations (before we process the args,
        // etc.) for the JIT helper case.
        if (tailCallViaJitHelper)
        {
            fgMorphTailCallViaJitHelper(call);

            // Force re-evaluating the argInfo. fgMorphTailCallViaJitHelper will modify the
            // argument list, invalidating the argInfo.
            call->fgArgInfo = nullptr;
        }

        // Tail call via JIT helper: The VM can't use return address hijacking
        // if we're not going to return and the helper doesn't have enough info
        // to safely poll, so we poll before the tail call, if the block isn't
        // already safe. Since tail call via helper is a slow mechanism it
        // doen't matter whether we emit GC poll. his is done to be in parity
        // with Jit64. Also this avoids GC info size increase if all most all
        // methods are expected to be tail calls (e.g. F#).
        //
        // Note that we can avoid emitting GC-poll if we know that the current
        // BB is dominated by a Gc-SafePoint block. But we don't have dominator
        // info at this point. One option is to just add a place holder node for
        // GC-poll (e.g. GT_GCPOLL) here and remove it in lowering if the block
        // is dominated by a GC-SafePoint. For now it not clear whether
        // optimizing slow tail calls is worth the effort. As a low cost check,
        // we check whether the first and current basic blocks are
        // GC-SafePoints.
        //
        // Fast Tail call as epilog+jmp - No need to insert GC-poll. Instead,
        // fgSetBlockOrder() is going to mark the method as fully interruptible
        // if the block containing this tail call is reachable without executing
        // any call.
        BasicBlock* curBlock = compCurBB;
        if (canFastTailCall || (fgFirstBB->bbFlags & BBF_GC_SAFE_POINT) || (compCurBB->bbFlags & BBF_GC_SAFE_POINT) ||
            (fgCreateGCPoll(GCPOLL_INLINE, compCurBB) == curBlock))
        {
            // We didn't insert a poll block, so we need to morph the call now
            // (Normally it will get morphed when we get to the split poll block)
            GenTree* temp = fgMorphCall(call);
            noway_assert(temp == call);
        }

        // Fast tail call: in case of fast tail calls, we need a jmp epilog and
        // hence mark it as BBJ_RETURN with BBF_JMP flag set.
        noway_assert(compCurBB->bbJumpKind == BBJ_RETURN);
        if (canFastTailCall)
        {
            compCurBB->bbFlags |= BBF_HAS_JMP;
        }
        else
        {
            // We call CORINFO_HELP_TAILCALL which does not return, so we will
            // not need epilogue.
            compCurBB->bbJumpKind = BBJ_THROW;
        }

        if (isRootReplaced)
        {
            // We have replaced the root node of this stmt and deleted the rest,
            // but we still have the deleted, dead nodes on the `fgMorph*` stack
            // if the root node was an `ASG`, `RET` or `CAST`.
            // Return a zero con node to exit morphing of the old trees without asserts
            // and forbid POST_ORDER morphing doing something wrong with our call.
            var_types callType;
            if (varTypeIsStruct(origCallType))
            {
                CORINFO_CLASS_HANDLE        retClsHnd = call->gtRetClsHnd;
                Compiler::structPassingKind howToReturnStruct;
                callType = getReturnTypeForStruct(retClsHnd, &howToReturnStruct);
                assert((howToReturnStruct != SPK_Unknown) && (howToReturnStruct != SPK_ByReference));
                if (howToReturnStruct == SPK_ByValue)
                {
                    callType = TYP_I_IMPL;
                }
                else if (howToReturnStruct == SPK_ByValueAsHfa || varTypeIsSIMD(callType))
                {
                    callType = TYP_FLOAT;
                }
                assert((callType != TYP_UNKNOWN) && !varTypeIsStruct(callType));
            }
            else
            {
                callType = origCallType;
            }
            assert((callType != TYP_UNKNOWN) && !varTypeIsStruct(callType));
            callType = genActualType(callType);

            GenTree* zero = gtNewZeroConNode(callType);
            result        = fgMorphTree(zero);
        }
        else
        {
            result = call;
        }
    }

    return result;
}

//------------------------------------------------------------------------
// fgMorphTailCallViaHelpers: Transform the given GT_CALL tree for tailcall code
// generation.
//
// Arguments:
//     call - The call to transform
//     helpers - The tailcall helpers provided by the runtime.
//
// Return Value:
//    Returns the transformed node.
//
// Notes:
//   This transforms
//     GT_CALL
//         {callTarget}
//         {this}
//         {args}
//   into
//     GT_COMMA
//       GT_CALL StoreArgsStub
//         {callTarget}         (depending on flags provided by the runtime)
//         {this}               (as a regular arg)
//         {args}
//       GT_COMMA
//         GT_CALL Dispatcher
//           GT_ADDR ReturnAddress
//           {CallTargetStub}
//           GT_ADDR ReturnValue
//         GT_LCL ReturnValue
// whenever the call node returns a value. If the call node does not return a
// value the last comma will not be there.
//
GenTree* Compiler::fgMorphTailCallViaHelpers(GenTreeCall* call, CORINFO_TAILCALL_HELPERS& help)
{
    // R2R requires different handling but we don't support tailcall via
    // helpers in R2R yet, so just leave it for now.
    // TODO: R2R: TailCallViaHelper
    assert(!opts.IsReadyToRun());

    JITDUMP("fgMorphTailCallViaHelpers (before):\n");
    DISPTREE(call);

    // Don't support tail calling helper methods
    assert(call->gtCallType != CT_HELPER);

    // We come this route only for tail prefixed calls that cannot be dispatched as
    // fast tail calls
    assert(!call->IsImplicitTailCall());
    assert(!fgCanFastTailCall(call, nullptr));

    // If VSD then get rid of arg to VSD since we turn this into a direct call.
    // The extra arg will be the first arg so this needs to be done before we
    // handle the retbuf below.
    if (call->IsVirtualStub())
    {
        JITDUMP("This is a VSD\n");
#if FEATURE_FASTTAILCALL
        // fgInitArgInfo has been called from fgCanFastTailCall and it added the stub address
        // to the arg list. Remove it now.
        call->gtCallArgs = call->gtCallArgs->GetNext();
        // We changed args so recompute info.
        call->fgArgInfo = nullptr;
#endif

        call->gtFlags &= ~GTF_CALL_VIRT_STUB;
    }

    GenTree* callDispatcherAndGetResult = fgCreateCallDispatcherAndGetResult(call, help.hCallTarget, help.hDispatcher);

    // Change the call to a call to the StoreArgs stub.
    if (call->HasRetBufArg())
    {
        JITDUMP("Removing retbuf");
        call->gtCallArgs = call->gtCallArgs->GetNext();
        call->gtCallMoreFlags &= ~GTF_CALL_M_RETBUFFARG;

        // We changed args so recompute info.
        call->fgArgInfo = nullptr;
    }

    // We may need to pass the target, for instance for calli or generic methods
    // where we pass instantiating stub.
    if ((help.flags & CORINFO_TAILCALL_STORE_TARGET) != 0)
    {
        // If asked to store target and we have a type arg we will store
        // instantiating stub, so in that case we should not pass the type arg.
        if (call->tailCallInfo->GetSig()->hasTypeArg())
        {
            JITDUMP("Removing type arg");

            assert(call->gtCallArgs != nullptr);
            if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
            {
                // Generic context is first arg
                call->gtCallArgs = call->gtCallArgs->GetNext();
            }
            else
            {
                // Generic context is last arg
                GenTreeCall::Use** lastArgSlot = &call->gtCallArgs;
                while ((*lastArgSlot)->GetNext() != nullptr)
                {
                    lastArgSlot = &(*lastArgSlot)->NextRef();
                }

                *lastArgSlot = nullptr;
            }
            call->fgArgInfo = nullptr;
        }

        JITDUMP("Adding target since VM requested it\n");
        GenTree* target;
        if (call->tailCallInfo->IsCalli())
        {
            noway_assert(call->gtCallType == CT_INDIRECT && call->gtCallAddr != nullptr);
            target = call->gtCallAddr;
        }
        else
        {
            CORINFO_CALL_INFO callInfo;
            unsigned          flags = CORINFO_CALLINFO_LDFTN;
            if (call->tailCallInfo->IsCallvirt())
            {
                flags |= CORINFO_CALLINFO_CALLVIRT;
            }

            eeGetCallInfo(call->tailCallInfo->GetToken(), nullptr, (CORINFO_CALLINFO_FLAGS)flags, &callInfo);

            if (!call->tailCallInfo->IsCallvirt() ||
                ((callInfo.methodFlags & (CORINFO_FLG_FINAL | CORINFO_FLG_STATIC)) != 0) ||
                ((callInfo.methodFlags & CORINFO_FLG_VIRTUAL) == 0))
            {
                target = getMethodPointerTree(call->tailCallInfo->GetToken(), &callInfo);
            }
            else
            {
                assert(call->gtCallThisArg != nullptr);
                // TODO: Proper cloning of the this pointer.
                target = getVirtMethodPointerTree(gtCloneExpr(call->gtCallThisArg->GetNode()),
                                                  call->tailCallInfo->GetToken(), &callInfo);
            }
        }

        // Insert target as last arg
        GenTreeCall::Use** newArgSlot = &call->gtCallArgs;
        while (*newArgSlot != nullptr)
        {
            newArgSlot = &(*newArgSlot)->NextRef();
        }

        *newArgSlot = gtNewCallArgs(target);

        call->fgArgInfo = nullptr;
    }

    // Put 'this' in normal param list
    if (call->gtCallThisArg != nullptr)
    {
        JITDUMP("Moving this pointer into arg list\n");
        GenTree* thisPtr    = nullptr;
        GenTree* objp       = call->gtCallThisArg->GetNode();
        call->gtCallThisArg = nullptr;

        if (call->NeedsNullCheck())
        {
            // clone "this" if "this" has no side effects.
            if ((objp->gtFlags & GTF_SIDE_EFFECT) == 0)
            {
                thisPtr = gtClone(objp, true);
            }

            var_types vt = objp->TypeGet();
            if (thisPtr == nullptr)
            {
                // create a temp if either "this" has side effects or "this" is too complex to clone.

                // tmp = "this"
                unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
                GenTree* asg    = gtNewTempAssign(lclNum, objp);

                // COMMA(tmp = "this", deref(tmp))
                GenTree* tmp       = gtNewLclvNode(lclNum, vt);
                GenTree* nullcheck = gtNewNullCheck(tmp, compCurBB);
                asg                = gtNewOperNode(GT_COMMA, TYP_VOID, asg, nullcheck);

                // COMMA(COMMA(tmp = "this", deref(tmp)), tmp)
                thisPtr = gtNewOperNode(GT_COMMA, vt, asg, gtNewLclvNode(lclNum, vt));
            }
            else
            {
                // thisPtr = COMMA(deref("this"), "this")
                GenTree* nullcheck = gtNewNullCheck(thisPtr, compCurBB);
                thisPtr            = gtNewOperNode(GT_COMMA, vt, nullcheck, gtClone(objp, true));
            }

            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }
        else
        {
            thisPtr = objp;
        }

        // During rationalization tmp="this" and null check will be materialized
        // in the right execution order.
        assert(thisPtr != nullptr);
        call->gtCallArgs = gtPrependNewCallArg(thisPtr, call->gtCallArgs);
        call->fgArgInfo  = nullptr;
    }

    // This is now a direct call to the store args stub and not a tailcall.
    call->gtCallType    = CT_USER_FUNC;
    call->gtCallMethHnd = help.hStoreArgs;
    call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;
    call->gtCallMoreFlags &= ~(GTF_CALL_M_TAILCALL | GTF_CALL_M_DELEGATE_INV | GTF_CALL_M_WRAPPER_DELEGATE_INV);

    // The store-args stub returns no value.
    call->gtRetClsHnd  = nullptr;
    call->gtType       = TYP_VOID;
    call->gtReturnType = TYP_VOID;

    GenTree* finalTree =
        gtNewOperNode(GT_COMMA, callDispatcherAndGetResult->TypeGet(), call, callDispatcherAndGetResult);

    finalTree = fgMorphTree(finalTree);

    JITDUMP("fgMorphTailCallViaHelpers (after):\n");
    DISPTREE(finalTree);
    return finalTree;
}

//------------------------------------------------------------------------
// fgCreateCallDispatcherAndGetResult: Given a call
// CALL
//   {callTarget}
//   {retbuf}
//   {this}
//   {args}
// create a similarly typed node that calls the tailcall dispatcher and returns
// the result, as in the following:
// COMMA
//   CALL TailCallDispatcher
//     ADDR ReturnAddress
//     &CallTargetFunc
//     ADDR RetValue
//   RetValue
// If the call has type TYP_VOID, only create the CALL node.
//
// Arguments:
//    origCall - the call
//    callTargetStubHnd - the handle of the CallTarget function (this is a special
//    IL stub created by the runtime)
//    dispatcherHnd - the handle of the tailcall dispatcher function
//
// Return Value:
//    A node that can be used in place of the original call.
//
GenTree* Compiler::fgCreateCallDispatcherAndGetResult(GenTreeCall*          origCall,
                                                      CORINFO_METHOD_HANDLE callTargetStubHnd,
                                                      CORINFO_METHOD_HANDLE dispatcherHnd)
{
    GenTreeCall* callDispatcherNode =
        gtNewCallNode(CT_USER_FUNC, dispatcherHnd, TYP_VOID, nullptr, fgMorphStmt->GetILOffsetX());
    // The dispatcher has signature
    // void DispatchTailCalls(void* callersRetAddrSlot, void* callTarget, void* retValue)

    // Add return value arg.
    GenTree*     retValArg;
    GenTree*     retVal    = nullptr;
    unsigned int newRetLcl = BAD_VAR_NUM;

    // Use existing retbuf if there is one.
    if (origCall->HasRetBufArg())
    {
        JITDUMP("Transferring retbuf\n");
        GenTree* retBufArg = origCall->gtCallArgs->GetNode();
        assert((info.compRetBuffArg != BAD_VAR_NUM) && retBufArg->OperIsLocal() &&
               (retBufArg->AsLclVarCommon()->GetLclNum() == info.compRetBuffArg));

        retValArg = retBufArg;
        if (origCall->gtType != TYP_VOID)
        {
            retVal = gtClone(retValArg);
        }
    }
    else if (origCall->gtType != TYP_VOID)
    {
        JITDUMP("Creating a new temp for the return value\n");
        newRetLcl = lvaGrabTemp(false DEBUGARG("Return value for tail call dispatcher"));
        if (varTypeIsStruct(origCall->gtType))
        {
            lvaSetStruct(newRetLcl, origCall->gtRetClsHnd, false);
        }
        else
        {
            // Since we pass a reference to the return value to the dispatcher
            // we need to use the real return type so we can normalize it on
            // load when we return it.
            lvaTable[newRetLcl].lvType = (var_types)origCall->gtReturnType;
        }

        lvaSetVarAddrExposed(newRetLcl);

        retValArg =
            gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(newRetLcl, genActualType(lvaTable[newRetLcl].lvType)));
        retVal = gtNewLclvNode(newRetLcl, genActualType(lvaTable[newRetLcl].lvType));

        if (varTypeIsStruct(origCall->gtType))
        {
            retVal = impFixupStructReturnType(retVal, origCall->gtRetClsHnd);
        }
    }
    else
    {
        JITDUMP("No return value so using null pointer as arg\n");
        retValArg = gtNewZeroConNode(TYP_I_IMPL);
    }

    callDispatcherNode->gtCallArgs = gtPrependNewCallArg(retValArg, callDispatcherNode->gtCallArgs);

    // Add callTarget
    callDispatcherNode->gtCallArgs =
        gtPrependNewCallArg(new (this, GT_FTN_ADDR) GenTreeFptrVal(TYP_I_IMPL, callTargetStubHnd),
                            callDispatcherNode->gtCallArgs);

    // Add the caller's return address slot.
    if (lvaRetAddrVar == BAD_VAR_NUM)
    {
        lvaRetAddrVar                  = lvaGrabTemp(false DEBUGARG("Return address"));
        lvaTable[lvaRetAddrVar].lvType = TYP_I_IMPL;
        lvaSetVarAddrExposed(lvaRetAddrVar);
    }

    GenTree* retAddrSlot           = gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(lvaRetAddrVar, TYP_I_IMPL));
    callDispatcherNode->gtCallArgs = gtPrependNewCallArg(retAddrSlot, callDispatcherNode->gtCallArgs);

    if (origCall->gtType == TYP_VOID)
    {
        return callDispatcherNode;
    }

    assert(retVal != nullptr);
    GenTree* comma = gtNewOperNode(GT_COMMA, origCall->TypeGet(), callDispatcherNode, retVal);
    // The JIT seems to want to CSE this comma and messes up multi-reg ret
    // values in the process. Just avoid CSE'ing this tree entirely in that
    // case.
    if (origCall->HasMultiRegRetVal())
    {
        comma->gtFlags |= GTF_DONT_CSE;
    }

    return comma;
}

//------------------------------------------------------------------------
// getMethodPointerTree: get a method pointer tree
//
// Arguments:
//    pResolvedToken - resolved token of the call
//    pCallInfo - the call info of the call
//
// Return Value:
//    A node representing the method pointer
//
GenTree* Compiler::getMethodPointerTree(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo)
{
    switch (pCallInfo->kind)
    {
        case CORINFO_CALL:
            return new (this, GT_FTN_ADDR) GenTreeFptrVal(TYP_I_IMPL, pCallInfo->hMethod);
        case CORINFO_CALL_CODE_POINTER:
            return getLookupTree(pResolvedToken, &pCallInfo->codePointerLookup, GTF_ICON_FTN_ADDR, pCallInfo->hMethod);
        default:
            noway_assert(!"unknown call kind");
            return nullptr;
    }
}

//------------------------------------------------------------------------
// getLookupTree: get a lookup tree
//
// Arguments:
//    pResolvedToken - resolved token of the call
//    pLookup - the lookup to get the tree for
//    handleFlags - flags to set on the result node
//    compileTimeHandle - compile-time handle corresponding to the lookup
//
// Return Value:
//    A node representing the lookup tree
//
GenTree* Compiler::getLookupTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
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
        assert(pLookup->constLookup.accessType != IAT_PPVALUE && pLookup->constLookup.accessType != IAT_RELPVALUE);

        if (pLookup->constLookup.accessType == IAT_VALUE)
        {
            handle = pLookup->constLookup.handle;
        }
        else if (pLookup->constLookup.accessType == IAT_PVALUE)
        {
            pIndirection = pLookup->constLookup.addr;
        }

        return gtNewIconEmbHndNode(handle, pIndirection, handleFlags, compileTimeHandle);
    }

    return getRuntimeLookupTree(pResolvedToken, pLookup, compileTimeHandle);
}

//------------------------------------------------------------------------
// getRuntimeLookupTree: get a tree for a runtime lookup
//
// Arguments:
//    pResolvedToken - resolved token of the call
//    pLookup - the lookup to get the tree for
//    compileTimeHandle - compile-time handle corresponding to the lookup
//
// Return Value:
//    A node representing the runtime lookup tree
//
GenTree* Compiler::getRuntimeLookupTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                        CORINFO_LOOKUP*         pLookup,
                                        void*                   compileTimeHandle)
{
    assert(!compIsForInlining());

    CORINFO_RUNTIME_LOOKUP* pRuntimeLookup = &pLookup->runtimeLookup;

    // If pRuntimeLookup->indirections is equal to CORINFO_USEHELPER, it specifies that a run-time helper should be
    // used; otherwise, it specifies the number of indirections via pRuntimeLookup->offsets array.
    if ((pRuntimeLookup->indirections == CORINFO_USEHELPER) || pRuntimeLookup->testForNull ||
        pRuntimeLookup->testForFixup)
    {
        // If the first condition is true, runtime lookup tree is available only via the run-time helper function.
        // TODO-CQ If the second or third condition is true, we are always using the slow path since we can't
        // introduce control flow at this point. See impRuntimeLookupToTree for the logic to avoid calling the helper.
        // The long-term solution is to introduce a new node representing a runtime lookup, create instances
        // of that node both in the importer and here, and expand the node in lower (introducing control flow if
        // necessary).
        return gtNewRuntimeLookupHelperCallNode(pRuntimeLookup,
                                                getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind),
                                                compileTimeHandle);
    }

    GenTree* result = getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind);

    ArrayStack<GenTree*> stmts(getAllocator(CMK_ArrayStack));

    auto cloneTree = [&](GenTree** tree DEBUGARG(const char* reason)) {
        if (!((*tree)->gtFlags & GTF_GLOB_EFFECT))
        {
            GenTree* clone = gtClone(*tree, true);

            if (clone)
            {
                return clone;
            }
        }

        unsigned temp = lvaGrabTemp(true DEBUGARG(reason));
        stmts.Push(gtNewTempAssign(temp, *tree));
        *tree = gtNewLclvNode(temp, lvaGetActualType(temp));
        return gtNewLclvNode(temp, lvaGetActualType(temp));
    };

    // Apply repeated indirections
    for (WORD i = 0; i < pRuntimeLookup->indirections; i++)
    {
        GenTree* preInd = nullptr;
        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            preInd = cloneTree(&result DEBUGARG("getRuntimeLookupTree indirectOffset"));
        }

        if (i != 0)
        {
            result = gtNewOperNode(GT_IND, TYP_I_IMPL, result);
            result->gtFlags |= GTF_IND_NONFAULTING;
            result->gtFlags |= GTF_IND_INVARIANT;
        }

        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            result = gtNewOperNode(GT_ADD, TYP_I_IMPL, preInd, result);
        }

        if (pRuntimeLookup->offsets[i] != 0)
        {
            result = gtNewOperNode(GT_ADD, TYP_I_IMPL, result, gtNewIconNode(pRuntimeLookup->offsets[i], TYP_I_IMPL));
        }
    }

    assert(!pRuntimeLookup->testForNull);
    if (pRuntimeLookup->indirections > 0)
    {
        assert(!pRuntimeLookup->testForFixup);
        result = gtNewOperNode(GT_IND, TYP_I_IMPL, result);
        result->gtFlags |= GTF_IND_NONFAULTING;
    }

    // Produces GT_COMMA(stmt1, GT_COMMA(stmt2, ... GT_COMMA(stmtN, result)))

    while (!stmts.Empty())
    {
        result = gtNewOperNode(GT_COMMA, TYP_I_IMPL, stmts.Pop(), result);
    }

    DISPTREE(result);
    return result;
}

//------------------------------------------------------------------------
// getVirtMethodPointerTree: get a tree for a virtual method pointer
//
// Arguments:
//    thisPtr - tree representing `this` pointer
//    pResolvedToken - pointer to the resolved token of the method
//    pCallInfo - pointer to call info
//
// Return Value:
//    A node representing the virtual method pointer

GenTree* Compiler::getVirtMethodPointerTree(GenTree*                thisPtr,
                                            CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                            CORINFO_CALL_INFO*      pCallInfo)
{
    GenTree* exactTypeDesc   = getTokenHandleTree(pResolvedToken, true);
    GenTree* exactMethodDesc = getTokenHandleTree(pResolvedToken, false);

    GenTreeCall::Use* helpArgs = gtNewCallArgs(thisPtr, exactTypeDesc, exactMethodDesc);
    return gtNewHelperCallNode(CORINFO_HELP_VIRTUAL_FUNC_PTR, TYP_I_IMPL, helpArgs);
}

//------------------------------------------------------------------------
// getTokenHandleTree: get a handle tree for a token
//
// Arguments:
//    pResolvedToken - token to get a handle for
//    parent - whether parent should be imported
//
// Return Value:
//    A node representing the virtual method pointer

GenTree* Compiler::getTokenHandleTree(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool parent)
{
    CORINFO_GENERICHANDLE_RESULT embedInfo;
    info.compCompHnd->embedGenericHandle(pResolvedToken, parent ? TRUE : FALSE, &embedInfo);

    GenTree* result = getLookupTree(pResolvedToken, &embedInfo.lookup, gtTokenToIconFlags(pResolvedToken->token),
                                    embedInfo.compileTimeHandle);

    // If we have a result and it requires runtime lookup, wrap it in a runtime lookup node.
    if ((result != nullptr) && embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        result = gtNewRuntimeLookup(embedInfo.compileTimeHandle, embedInfo.handleType, result);
    }

    return result;
}

/*****************************************************************************
 *
 *  Transform the given GT_CALL tree for tail call via JIT helper.
 */
void Compiler::fgMorphTailCallViaJitHelper(GenTreeCall* call)
{
    JITDUMP("fgMorphTailCallViaJitHelper (before):\n");
    DISPTREE(call);

    // The runtime requires that we perform a null check on the `this` argument before
    // tail calling  to a virtual dispatch stub. This requirement is a consequence of limitations
    // in the runtime's ability to map an AV to a NullReferenceException if
    // the AV occurs in a dispatch stub that has unmanaged caller.
    if (call->IsVirtualStub())
    {
        call->gtFlags |= GTF_CALL_NULLCHECK;
    }

    // For the helper-assisted tail calls, we need to push all the arguments
    // into a single list, and then add a few extra at the beginning or end.
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
    assert(!fgCanFastTailCall(call, nullptr));

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

    if (call->gtCallThisArg != nullptr)
    {
        GenTree* thisPtr    = nullptr;
        GenTree* objp       = call->gtCallThisArg->GetNode();
        call->gtCallThisArg = nullptr;

        if ((call->IsDelegateInvoke() || call->IsVirtualVtable()) && !objp->IsLocal())
        {
            // tmp = "this"
            unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
            GenTree* asg    = gtNewTempAssign(lclNum, objp);

            // COMMA(tmp = "this", tmp)
            var_types vt  = objp->TypeGet();
            GenTree*  tmp = gtNewLclvNode(lclNum, vt);
            thisPtr       = gtNewOperNode(GT_COMMA, vt, asg, tmp);

            objp = thisPtr;
        }

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
                unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
                GenTree* asg    = gtNewTempAssign(lclNum, objp);

                // COMMA(tmp = "this", deref(tmp))
                GenTree* tmp       = gtNewLclvNode(lclNum, vt);
                GenTree* nullcheck = gtNewNullCheck(tmp, compCurBB);
                asg                = gtNewOperNode(GT_COMMA, TYP_VOID, asg, nullcheck);

                // COMMA(COMMA(tmp = "this", deref(tmp)), tmp)
                thisPtr = gtNewOperNode(GT_COMMA, vt, asg, gtNewLclvNode(lclNum, vt));
            }
            else
            {
                // thisPtr = COMMA(deref("this"), "this")
                GenTree* nullcheck = gtNewNullCheck(thisPtr, compCurBB);
                thisPtr            = gtNewOperNode(GT_COMMA, vt, nullcheck, gtClone(objp, true));
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
        call->gtCallArgs = gtPrependNewCallArg(thisPtr, call->gtCallArgs);
    }

    // Find the end of the argument list. ppArg will point at the last pointer; setting *ppArg will
    // append to the list.
    GenTreeCall::Use** ppArg = &call->gtCallArgs;
    for (GenTreeCall::Use& use : call->Args())
    {
        ppArg = &use.NextRef();
    }
    assert(ppArg != nullptr);
    assert(*ppArg == nullptr);

    unsigned nOldStkArgsWords =
        (compArgSize - (codeGen->intRegState.rsCalleeRegArgCount * REGSIZE_BYTES)) / REGSIZE_BYTES;
    GenTree* arg3 = gtNewIconNode((ssize_t)nOldStkArgsWords, TYP_I_IMPL);
    *ppArg        = gtNewCallArgs(arg3); // numberOfOldStackArgs
    ppArg         = &((*ppArg)->NextRef());

    // Inject a placeholder for the count of outgoing stack arguments that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg2 = gtNewIconNode(9, TYP_I_IMPL);
    *ppArg        = gtNewCallArgs(arg2); // numberOfNewStackArgs
    ppArg         = &((*ppArg)->NextRef());

    // Inject a placeholder for the flags.
    // The constant will be replaced.
    GenTree* arg1 = gtNewIconNode(8, TYP_I_IMPL);
    *ppArg        = gtNewCallArgs(arg1);
    ppArg         = &((*ppArg)->NextRef());

    // Inject a placeholder for the real call target that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg0 = gtNewIconNode(7, TYP_I_IMPL);
    *ppArg        = gtNewCallArgs(arg0);

    // It is now a varargs tail call.
    call->gtCallMoreFlags |= GTF_CALL_M_VARARGS;
    call->gtFlags &= ~GTF_CALL_POP_ARGS;

    // The function is responsible for doing explicit null check when it is necessary.
    assert(!call->NeedsNullCheck());

    JITDUMP("fgMorphTailCallViaJitHelper (after):\n");
    DISPTREE(call);
}

//------------------------------------------------------------------------
// fgGetStubAddrArg: Return the virtual stub address for the given call.
//
// Notes:
//    the JIT must place the address of the stub used to load the call target,
//    the "stub indirection cell", in special call argument with special register.
//
// Arguments:
//    call - a call that needs virtual stub dispatching.
//
// Return Value:
//    addr tree with set resister requirements.
//
GenTree* Compiler::fgGetStubAddrArg(GenTreeCall* call)
{
    assert(call->IsVirtualStub());
    GenTree* stubAddrArg;
    if (call->gtCallType == CT_INDIRECT)
    {
        stubAddrArg = gtClone(call->gtCallAddr, true);
    }
    else
    {
        assert(call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT);
        ssize_t addr = ssize_t(call->gtStubCallStubAddr);
        stubAddrArg  = gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
#ifdef DEBUG
        stubAddrArg->AsIntCon()->gtTargetHandle = (size_t)call->gtCallMethHnd;
#endif
    }
    assert(stubAddrArg != nullptr);
    stubAddrArg->SetRegNum(virtualStubParamInfo->GetReg());
    return stubAddrArg;
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
    Statement* lastStmt = block->lastStmt();
    assert(recursiveTailCall == lastStmt->GetRootNode());

    // Transform recursive tail call into a loop.

    Statement* earlyArgInsertionPoint = lastStmt;
    IL_OFFSETX callILOffset           = lastStmt->GetILOffsetX();

    // Hoist arg setup statement for the 'this' argument.
    GenTreeCall::Use* thisArg = recursiveTailCall->gtCallThisArg;
    if ((thisArg != nullptr) && !thisArg->GetNode()->IsNothingNode() && !thisArg->GetNode()->IsArgPlaceHolderNode())
    {
        Statement* thisArgStmt = gtNewStmt(thisArg->GetNode(), callILOffset);
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

    Statement* tmpAssignmentInsertionPoint   = lastStmt;
    Statement* paramAssignmentInsertionPoint = lastStmt;

    // Process early args. They may contain both setup statements for late args and actual args.
    // Early args don't include 'this' arg. We need to account for that so that the call to gtArgEntryByArgNum
    // below has the correct second argument.
    int earlyArgIndex = (thisArg == nullptr) ? 0 : 1;
    for (GenTreeCall::Use& use : recursiveTailCall->Args())
    {
        GenTree* earlyArg = use.GetNode();
        if (!earlyArg->IsNothingNode() && !earlyArg->IsArgPlaceHolderNode())
        {
            if ((earlyArg->gtFlags & GTF_LATE_ARG) != 0)
            {
                // This is a setup node so we need to hoist it.
                Statement* earlyArgStmt = gtNewStmt(earlyArg, callILOffset);
                fgInsertStmtBefore(block, earlyArgInsertionPoint, earlyArgStmt);
            }
            else
            {
                // This is an actual argument that needs to be assigned to the corresponding caller parameter.
                fgArgTabEntry* curArgTabEntry = gtArgEntryByArgNum(recursiveTailCall, earlyArgIndex);
                Statement*     paramAssignStmt =
                    fgAssignRecursiveCallArgToCallerParam(earlyArg, curArgTabEntry, block, callILOffset,
                                                          tmpAssignmentInsertionPoint, paramAssignmentInsertionPoint);
                if ((tmpAssignmentInsertionPoint == lastStmt) && (paramAssignStmt != nullptr))
                {
                    // All temp assignments will happen before the first param assignment.
                    tmpAssignmentInsertionPoint = paramAssignStmt;
                }
            }
        }
        earlyArgIndex++;
    }

    // Process late args.
    int lateArgIndex = 0;
    for (GenTreeCall::Use& use : recursiveTailCall->LateArgs())
    {
        // A late argument is an actual argument that needs to be assigned to the corresponding caller's parameter.
        GenTree*       lateArg        = use.GetNode();
        fgArgTabEntry* curArgTabEntry = gtArgEntryByLateArgIndex(recursiveTailCall, lateArgIndex);
        Statement*     paramAssignStmt =
            fgAssignRecursiveCallArgToCallerParam(lateArg, curArgTabEntry, block, callILOffset,
                                                  tmpAssignmentInsertionPoint, paramAssignmentInsertionPoint);

        if ((tmpAssignmentInsertionPoint == lastStmt) && (paramAssignStmt != nullptr))
        {
            // All temp assignments will happen before the first param assignment.
            tmpAssignmentInsertionPoint = paramAssignStmt;
        }
        lateArgIndex++;
    }

    // If the method has starg.s 0 or ldarga.s 0 a special local (lvaArg0Var) is created so that
    // compThisArg stays immutable. Normally it's assigned in fgFirstBBScratch block. Since that
    // block won't be in the loop (it's assumed to have no predecessors), we need to update the special local here.
    if (!info.compIsStatic && (lvaArg0Var != info.compThisArg))
    {
        var_types  thisType           = lvaTable[info.compThisArg].TypeGet();
        GenTree*   arg0               = gtNewLclvNode(lvaArg0Var, thisType);
        GenTree*   arg0Assignment     = gtNewAssignNode(arg0, gtNewLclvNode(info.compThisArg, thisType));
        Statement* arg0AssignmentStmt = gtNewStmt(arg0Assignment, callILOffset);
        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, arg0AssignmentStmt);
    }

    // If compInitMem is set, we may need to zero-initialize some locals. Normally it's done in the prolog
    // but this loop can't include the prolog. Since we don't have liveness information, we insert zero-initialization
    // for all non-parameter IL locals as well as temp structs with GC fields.
    // Liveness phase will remove unnecessary initializations.
    if (info.compInitMem || compSuppressedZeroInit)
    {
        unsigned   varNum;
        LclVarDsc* varDsc;
        for (varNum = 0, varDsc = lvaTable; varNum < lvaCount; varNum++, varDsc++)
        {
#if FEATURE_FIXED_OUT_ARGS
            if (varNum == lvaOutgoingArgSpaceVar)
            {
                continue;
            }
#endif // FEATURE_FIXED_OUT_ARGS
            if (!varDsc->lvIsParam)
            {
                var_types lclType            = varDsc->TypeGet();
                bool      isUserLocal        = (varNum < info.compLocalsCount);
                bool      structWithGCFields = ((lclType == TYP_STRUCT) && varDsc->GetLayout()->HasGCPtr());
                bool      hadSuppressedInit  = varDsc->lvSuppressedZeroInit;
                if ((info.compInitMem && (isUserLocal || structWithGCFields)) || hadSuppressedInit)
                {
                    GenTree* lcl  = gtNewLclvNode(varNum, lclType);
                    GenTree* init = nullptr;
                    if (varTypeIsStruct(lclType))
                    {
                        const bool isVolatile  = false;
                        const bool isCopyBlock = false;
                        init                   = gtNewBlkOpNode(lcl, gtNewIconNode(0), isVolatile, isCopyBlock);
                        init                   = fgMorphInitBlock(init);
                    }
                    else
                    {
                        GenTree* zero = gtNewZeroConNode(genActualType(lclType));
                        init          = gtNewAssignNode(lcl, zero);
                    }
                    Statement* initStmt = gtNewStmt(init, callILOffset);
                    fgInsertStmtBefore(block, lastStmt, initStmt);
                }
            }
        }
    }

    // Remove the call
    fgRemoveStmt(block, lastStmt);

    // Set the loop edge.
    if (opts.IsOSR())
    {
        // Todo: this may not look like a viable loop header.
        // Might need the moral equivalent of a scratch BB.
        block->bbJumpDest = fgEntryBB;
    }
    else
    {
        // Ensure we have a scratch block and then target the next
        // block.  Loop detection needs to see a pred out of the loop,
        // so mark the scratch block BBF_DONT_REMOVE to prevent empty
        // block removal on it.
        fgEnsureFirstBBisScratch();
        fgFirstBB->bbFlags |= BBF_DONT_REMOVE;
        block->bbJumpDest = fgFirstBB->bbNext;
    }

    // Finish hooking things up.
    block->bbJumpKind = BBJ_ALWAYS;
    block->bbJumpDest->bbFlags |= BBF_JMP_TARGET;
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

Statement* Compiler::fgAssignRecursiveCallArgToCallerParam(GenTree*       arg,
                                                           fgArgTabEntry* argTabEntry,
                                                           BasicBlock*    block,
                                                           IL_OFFSETX     callILOffset,
                                                           Statement*     tmpAssignmentInsertionPoint,
                                                           Statement*     paramAssignmentInsertionPoint)
{
    // Call arguments should be assigned to temps first and then the temps should be assigned to parameters because
    // some argument trees may reference parameters directly.

    GenTree* argInTemp             = nullptr;
    unsigned originalArgNum        = argTabEntry->argNum;
    bool     needToAssignParameter = true;

    // TODO-CQ: enable calls with struct arguments passed in registers.
    noway_assert(!varTypeIsStruct(arg->TypeGet()));

    if ((argTabEntry->isTmp) || arg->IsCnsIntOrI() || arg->IsCnsFltOrDbl())
    {
        // The argument is already assigned to a temp or is a const.
        argInTemp = arg;
    }
    else if (arg->OperGet() == GT_LCL_VAR)
    {
        unsigned   lclNum = arg->AsLclVar()->GetLclNum();
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

    Statement* paramAssignStmt = nullptr;
    if (needToAssignParameter)
    {
        if (argInTemp == nullptr)
        {
            // The argument is not assigned to a temp. We need to create a new temp and insert an assignment.
            // TODO: we can avoid a temp assignment if we can prove that the argument tree
            // doesn't involve any caller parameters.
            unsigned tmpNum          = lvaGrabTemp(true DEBUGARG("arg temp"));
            lvaTable[tmpNum].lvType  = arg->gtType;
            GenTree*   tempSrc       = arg;
            GenTree*   tempDest      = gtNewLclvNode(tmpNum, tempSrc->gtType);
            GenTree*   tmpAssignNode = gtNewAssignNode(tempDest, tempSrc);
            Statement* tmpAssignStmt = gtNewStmt(tmpAssignNode, callILOffset);
            fgInsertStmtBefore(block, tmpAssignmentInsertionPoint, tmpAssignStmt);
            argInTemp = gtNewLclvNode(tmpNum, tempSrc->gtType);
        }

        // Now assign the temp to the parameter.
        LclVarDsc* paramDsc = lvaTable + originalArgNum;
        assert(paramDsc->lvIsParam);
        GenTree* paramDest       = gtNewLclvNode(originalArgNum, paramDsc->lvType);
        GenTree* paramAssignNode = gtNewAssignNode(paramDest, argInTemp);
        paramAssignStmt          = gtNewStmt(paramAssignNode, callILOffset);

        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, paramAssignStmt);
    }
    return paramAssignStmt;
}

/*****************************************************************************
 *
 *  Transform the given GT_CALL tree for code generation.
 */

GenTree* Compiler::fgMorphCall(GenTreeCall* call)
{
    if (varTypeIsStruct(call))
    {
        fgFixupStructReturn(call);
    }
    if (call->CanTailCall())
    {
        GenTree* newNode = fgMorphPotentialTailCall(call);
        if (newNode != nullptr)
        {
            return newNode;
        }

        assert(!call->CanTailCall());

#if FEATURE_MULTIREG_RET
        if (fgGlobalMorph && call->HasMultiRegRetVal())
        {
            // The tail call has been rejected so we must finish the work deferred
            // by impFixupCallStructReturn for multi-reg-returning calls and transform
            //     ret call
            // into
            //     temp = call
            //     ret temp

            // Force re-evaluating the argInfo as the return argument has changed.
            call->fgArgInfo = nullptr;

            // Create a new temp.
            unsigned tmpNum =
                lvaGrabTemp(false DEBUGARG("Return value temp for multi-reg return (rejected tail call)."));
            lvaTable[tmpNum].lvIsMultiRegRet = true;

            GenTree* assg = nullptr;
            if (varTypeIsStruct(call->TypeGet()))
            {
                CORINFO_CLASS_HANDLE structHandle = call->gtRetClsHnd;
                assert(structHandle != NO_CLASS_HANDLE);
                const bool unsafeValueClsCheck = false;
                lvaSetStruct(tmpNum, structHandle, unsafeValueClsCheck);
                var_types structType = lvaTable[tmpNum].lvType;
                GenTree*  dst        = gtNewLclvNode(tmpNum, structType);
                assg                 = gtNewAssignNode(dst, call);
            }
            else
            {
                assg = gtNewTempAssign(tmpNum, call);
            }

            assg = fgMorphTree(assg);

            // Create the assignment statement and insert it before the current statement.
            Statement* assgStmt = gtNewStmt(assg, compCurStmt->GetILOffsetX());
            fgInsertStmtBefore(compCurBB, compCurStmt, assgStmt);

            // Return the temp.
            GenTree* result = gtNewLclvNode(tmpNum, lvaTable[tmpNum].lvType);
            result->gtFlags |= GTF_DONT_CSE;

            compCurBB->bbFlags |= BBF_HAS_CALL; // This block has a call

#ifdef DEBUG
            if (verbose)
            {
                printf("\nInserting assignment of a multi-reg call result to a temp:\n");
                gtDispStmt(assgStmt);
            }
            result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
            return result;
        }
#endif
    }

    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) == 0 &&
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_VIRTUAL_FUNC_PTR)
#ifdef FEATURE_READYTORUN_COMPILER
         || call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR)
#endif
             ) &&
        (call == fgMorphStmt->GetRootNode()))
    {
        // This is call to CORINFO_HELP_VIRTUAL_FUNC_PTR with ignored result.
        // Transform it into a null check.

        GenTree* thisPtr = call->gtCallArgs->GetNode();

        GenTree* nullCheck = gtNewNullCheck(thisPtr, compCurBB);

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

    // Mark the block as a GC safe point for the call if possible.
    // In the event the call indicates the block isn't a GC safe point
    // and the call is unmanaged with a GC transition suppression request
    // then insert a GC poll.
    CLANG_FORMAT_COMMENT_ANCHOR;

    if (IsGcSafePoint(call))
    {
        compCurBB->bbFlags |= BBF_GC_SAFE_POINT;
    }

    // Regardless of the state of the basic block with respect to GC safe point,
    // we will always insert a GC Poll for scenarios involving a suppressed GC
    // transition. Only mark the block for GC Poll insertion on the first morph.
    if (fgGlobalMorph && call->IsUnmanaged() && call->IsSuppressGCTransition())
    {
        compCurBB->bbFlags |= (BBF_HAS_SUPPRESSGC_CALL | BBF_GC_SAFE_POINT);
        optMethodFlags |= OMF_NEEDS_GCPOLLS;
    }

    // Morph Type.op_Equality, Type.op_Inequality, and Enum.HasFlag
    //
    // We need to do these before the arguments are morphed
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC))
    {
        // See if this is foldable
        GenTree* optTree = gtFoldExprCall(call);

        // If we optimized, morph the result
        if (optTree != call)
        {
            return fgMorphTree(optTree);
        }
    }

    compCurBB->bbFlags |= BBF_HAS_CALL; // This block has a call

    // Make sure that return buffers containing GC pointers that aren't too large are pointers into the stack.
    GenTree* origDest = nullptr; // Will only become non-null if we do the transformation (and thus require
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

        GenTree* dest = call->gtCallArgs->GetNode();
        assert(dest->OperGet() != GT_ARGPLACE); // If it was, we'd be in a remorph, which we've already excluded above.
        if (dest->gtType == TYP_BYREF && !(dest->OperGet() == GT_ADDR && dest->AsOp()->gtOp1->OperGet() == GT_LCL_VAR))
        {
            // We'll exempt helper calls from this, assuming that the helper implementation
            // follows the old convention, and does whatever barrier is required.
            if (call->gtCallType != CT_HELPER)
            {
                structHnd = call->gtRetClsHnd;
                if (info.compCompHnd->isStructRequiringStackAllocRetBuf(structHnd) &&
                    !(dest->OperGet() == GT_LCL_VAR && dest->AsLclVar()->GetLclNum() == info.compRetBuffArg))
                {
                    // Force re-evaluating the argInfo as the return argument has changed.
                    call->fgArgInfo = nullptr;
                    origDest        = dest;

                    retValTmpNum = lvaGrabTemp(true DEBUGARG("substitute local for ret buff arg"));
                    lvaSetStruct(retValTmpNum, structHnd, true);
                    dest = gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(retValTmpNum, TYP_STRUCT));
                }
            }
        }

        call->gtCallArgs->SetNode(dest);
    }

    /* Process the "normal" argument list */
    call = fgMorphArgs(call);
    noway_assert(call->gtOper == GT_CALL);

    // Morph stelem.ref helper call to store a null value, into a store into an array without the helper.
    // This needs to be done after the arguments are morphed to ensure constant propagation has already taken place.
    if (opts.OptimizationEnabled() && (call->gtCallType == CT_HELPER) &&
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ARRADDR_ST)))
    {
        GenTree* value = gtArgEntryByArgNum(call, 2)->GetNode();
        if (value->IsIntegralConst(0))
        {
            assert(value->OperGet() == GT_CNS_INT);

            GenTree* arr   = gtArgEntryByArgNum(call, 0)->GetNode();
            GenTree* index = gtArgEntryByArgNum(call, 1)->GetNode();

            // Either or both of the array and index arguments may have been spilled to temps by `fgMorphArgs`. Copy
            // the spill trees as well if necessary.
            GenTreeOp* argSetup = nullptr;
            for (GenTreeCall::Use& use : call->Args())
            {
                GenTree* const arg = use.GetNode();
                if (arg->OperGet() != GT_ASG)
                {
                    continue;
                }

                assert(arg != arr);
                assert(arg != index);

                arg->gtFlags &= ~GTF_LATE_ARG;

                GenTree* op1 = argSetup;
                if (op1 == nullptr)
                {
                    op1 = gtNewNothingNode();
#if DEBUG
                    op1->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
                }

                argSetup = new (this, GT_COMMA) GenTreeOp(GT_COMMA, TYP_VOID, op1, arg);

#if DEBUG
                argSetup->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
            }

#ifdef DEBUG
            auto resetMorphedFlag = [](GenTree** slot, fgWalkData* data) -> fgWalkResult {
                (*slot)->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
                return WALK_CONTINUE;
            };

            fgWalkTreePost(&arr, resetMorphedFlag);
            fgWalkTreePost(&index, resetMorphedFlag);
            fgWalkTreePost(&value, resetMorphedFlag);
#endif // DEBUG

            GenTree* const nullCheckedArr = impCheckForNullPointer(arr);
            GenTree* const arrIndexNode   = gtNewIndexRef(TYP_REF, nullCheckedArr, index);
            GenTree* const arrStore       = gtNewAssignNode(arrIndexNode, value);
            arrStore->gtFlags |= GTF_ASG;

            GenTree* result = fgMorphTree(arrStore);
            if (argSetup != nullptr)
            {
                result = new (this, GT_COMMA) GenTreeOp(GT_COMMA, TYP_VOID, argSetup, result);
#if DEBUG
                result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
            }

            return result;
        }
    }

    // Optimize get_ManagedThreadId(get_CurrentThread)
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) &&
        info.compCompHnd->getIntrinsicID(call->gtCallMethHnd) == CORINFO_INTRINSIC_GetManagedThreadId)
    {
        noway_assert(origDest == nullptr);
        noway_assert(call->gtCallLateArgs->GetNode() != nullptr);

        GenTree* innerCall = call->gtCallLateArgs->GetNode();

        if (innerCall->gtOper == GT_CALL && (innerCall->AsCall()->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) &&
            info.compCompHnd->getIntrinsicID(innerCall->AsCall()->gtCallMethHnd) ==
                CORINFO_INTRINSIC_GetCurrentManagedThread)
        {
            // substitute expression with call to helper
            GenTree* newCall = gtNewHelperCallNode(CORINFO_HELP_GETCURRENTMANAGEDTHREADID, TYP_INT);
            JITDUMP("get_ManagedThreadId(get_CurrentThread) folding performed\n");
            return fgMorphTree(newCall);
        }
    }

    if (origDest != nullptr)
    {
        GenTree* retValVarAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, gtNewLclvNode(retValTmpNum, TYP_STRUCT));
        // If the origDest expression was an assignment to a variable, it might be to an otherwise-unused
        // var, which would allow the whole assignment to be optimized away to a NOP.  So in that case, make the
        // origDest into a comma that uses the var.  Note that the var doesn't have to be a temp for this to
        // be correct.
        if (origDest->OperGet() == GT_ASG)
        {
            if (origDest->AsOp()->gtOp1->OperGet() == GT_LCL_VAR)
            {
                GenTree* var = origDest->AsOp()->gtOp1;
                origDest     = gtNewOperNode(GT_COMMA, var->TypeGet(), origDest,
                                         gtNewLclvNode(var->AsLclVar()->GetLclNum(), var->TypeGet()));
            }
        }
        GenTree* copyBlk = gtNewCpObjNode(origDest, retValVarAddr, structHnd, false);
        copyBlk          = fgMorphTree(copyBlk);
        GenTree* result  = gtNewOperNode(GT_COMMA, TYP_VOID, call, copyBlk);
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

GenTree* Compiler::fgMorphConst(GenTree* tree)
{
    assert(tree->OperKind() & GTK_CONST);

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
        CorInfoHelpFunc helper = info.compCompHnd->getLazyStringLiteralHelper(tree->AsStrCon()->gtScpHnd);
        if (helper != CORINFO_HELP_UNDEF)
        {
            // For un-important blocks, we want to construct the string lazily

            GenTreeCall::Use* args;
            if (helper == CORINFO_HELP_STRCNS_CURRENT_MODULE)
            {
                args = gtNewCallArgs(gtNewIconNode(RidFromToken(tree->AsStrCon()->gtSconCPX), TYP_INT));
            }
            else
            {
                args = gtNewCallArgs(gtNewIconNode(RidFromToken(tree->AsStrCon()->gtSconCPX), TYP_INT),
                                     gtNewIconEmbScpHndNode(tree->AsStrCon()->gtScpHnd));
            }

            tree = gtNewHelperCallNode(helper, TYP_REF, args);
            return fgMorphTree(tree);
        }
    }

    assert(tree->AsStrCon()->gtScpHnd == info.compScopeHnd || !IsUninitialized(tree->AsStrCon()->gtScpHnd));

    LPVOID         pValue;
    InfoAccessType iat =
        info.compCompHnd->constructStringLiteral(tree->AsStrCon()->gtScpHnd, tree->AsStrCon()->gtSconCPX, &pValue);

    tree = gtNewStringLiteralNode(iat, pValue);

    return fgMorphTree(tree);
}

//------------------------------------------------------------------------
// fgMorphTryFoldObjAsLclVar: try to fold an Obj node as a LclVar.
//
// Arguments:
//    obj - the obj node.
//
// Return value:
//    GenTreeLclVar if the obj can be replaced by it, null otherwise.
//
// Notes:
//    TODO-CQ: currently this transformation is done only under copy block,
//    but it is benefitial to do for each OBJ node. However, `PUT_ARG_STACK`
//    for some platforms does not expect struct `LCL_VAR` as a source, so
//    it needs more work.
//
GenTreeLclVar* Compiler::fgMorphTryFoldObjAsLclVar(GenTreeObj* obj)
{
    if (opts.OptimizationEnabled())
    {
        GenTree* op1 = obj->Addr();
        if (op1->OperIs(GT_ADDR))
        {
            GenTreeUnOp* addr   = op1->AsUnOp();
            GenTree*     addrOp = addr->gtGetOp1();
            if (addrOp->TypeIs(obj->TypeGet()) && addrOp->OperIs(GT_LCL_VAR))
            {
                GenTreeLclVar* lclVar = addrOp->AsLclVar();

                ClassLayout* lclVarLayout = lvaGetDesc(lclVar)->GetLayout();
                ClassLayout* objLayout    = obj->GetLayout();
                if (ClassLayout::AreCompatible(lclVarLayout, objLayout))
                {
#ifdef DEBUG
                    CORINFO_CLASS_HANDLE objClsHandle = obj->GetLayout()->GetClassHandle();
                    assert(objClsHandle != NO_CLASS_HANDLE);
                    if (verbose)
                    {
                        CORINFO_CLASS_HANDLE lclClsHnd = gtGetStructHandle(lclVar);
                        printf("fold OBJ(ADDR(X)) [%06u] into X [%06u], ", dspTreeID(obj), dspTreeID(lclVar));
                        printf("with %s handles\n", ((lclClsHnd == objClsHandle) ? "matching" : "different"));
                    }
#endif
                    // Keep the DONT_CSE flag in sync
                    // (as the addr always marks it for its op1)
                    lclVar->gtFlags &= ~GTF_DONT_CSE;
                    lclVar->gtFlags |= (obj->gtFlags & GTF_DONT_CSE);

                    DEBUG_DESTROY_NODE(obj);
                    DEBUG_DESTROY_NODE(addr);
                    return lclVar;
                }
            }
        }
    }
    return nullptr;
}

/*****************************************************************************
 *
 *  Transform the given GTK_LEAF tree for code generation.
 */

GenTree* Compiler::fgMorphLeaf(GenTree* tree)
{
    assert(tree->OperKind() & GTK_LEAF);

    if (tree->gtOper == GT_LCL_VAR)
    {
        const bool forceRemorph = false;
        return fgMorphLocalVar(tree, forceRemorph);
    }
    else if (tree->gtOper == GT_LCL_FLD)
    {
        if (lvaGetDesc(tree->AsLclFld())->lvAddrExposed)
        {
            tree->gtFlags |= GTF_GLOB_REF;
        }

#ifdef TARGET_X86
        if (info.compIsVarArgs)
        {
            GenTree* newTree = fgMorphStackArgForVarArgs(tree->AsLclFld()->GetLclNum(), tree->TypeGet(),
                                                         tree->AsLclFld()->GetLclOffs());
            if (newTree != nullptr)
            {
                if (newTree->OperIsBlk() && ((tree->gtFlags & GTF_VAR_DEF) == 0))
                {
                    newTree->SetOper(GT_IND);
                }
                return newTree;
            }
        }
#endif // TARGET_X86
    }
    else if (tree->gtOper == GT_FTN_ADDR)
    {
        CORINFO_CONST_LOOKUP addrInfo;

#ifdef FEATURE_READYTORUN_COMPILER
        if (tree->AsFptrVal()->gtEntryPoint.addr != nullptr)
        {
            addrInfo = tree->AsFptrVal()->gtEntryPoint;
        }
        else
#endif
        {
            info.compCompHnd->getFunctionFixedEntryPoint(tree->AsFptrVal()->gtFptrMethod, &addrInfo);
        }

        // Refer to gtNewIconHandleNode() as the template for constructing a constant handle
        //
        tree->SetOper(GT_CNS_INT);
        tree->AsIntConCommon()->SetIconValue(ssize_t(addrInfo.handle));
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

void Compiler::fgAssignSetVarDef(GenTree* tree)
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

//------------------------------------------------------------------------
// fgMorphOneAsgBlockOp: Attempt to replace a block assignment with a scalar assignment
//
// Arguments:
//    tree - The block assignment to be possibly morphed
//
// Return Value:
//    The modified tree if successful, nullptr otherwise.
//
// Assumptions:
//    'tree' must be a block assignment.
//
// Notes:
//    If successful, this method always returns the incoming tree, modifying only
//    its arguments.

GenTree* Compiler::fgMorphOneAsgBlockOp(GenTree* tree)
{
    // This must be a block assignment.
    noway_assert(tree->OperIsBlkOp());
    var_types asgType = tree->TypeGet();

    GenTree*   asg            = tree;
    GenTree*   dest           = asg->gtGetOp1();
    GenTree*   src            = asg->gtGetOp2();
    unsigned   destVarNum     = BAD_VAR_NUM;
    LclVarDsc* destVarDsc     = nullptr;
    GenTree*   destLclVarTree = nullptr;
    bool       isCopyBlock    = asg->OperIsCopyBlkOp();
    bool       isInitBlock    = !isCopyBlock;

    unsigned             size   = 0;
    CORINFO_CLASS_HANDLE clsHnd = NO_CLASS_HANDLE;

    if (dest->gtEffectiveVal()->OperIsBlk())
    {
        GenTreeBlk* lhsBlk = dest->gtEffectiveVal()->AsBlk();
        size               = lhsBlk->Size();
        if (impIsAddressInLocal(lhsBlk->Addr(), &destLclVarTree))
        {
            destVarNum = destLclVarTree->AsLclVarCommon()->GetLclNum();
            destVarDsc = &(lvaTable[destVarNum]);
        }
        if (lhsBlk->OperGet() == GT_OBJ)
        {
            clsHnd = lhsBlk->AsObj()->GetLayout()->GetClassHandle();
        }
    }
    else
    {
        // Is this an enregisterable struct that is already a simple assignment?
        // This can happen if we are re-morphing.
        // Note that we won't do this straightaway if this is a SIMD type, since it
        // may be a promoted lclVar (sometimes we promote the individual float fields of
        // fixed-size SIMD).
        if (dest->OperGet() == GT_IND)
        {
            noway_assert(asgType != TYP_STRUCT);
            if (varTypeIsStruct(asgType))
            {
                destLclVarTree = fgIsIndirOfAddrOfLocal(dest);
            }
            if (isCopyBlock && destLclVarTree == nullptr && !src->OperIs(GT_LCL_VAR))
            {
                fgMorphBlockOperand(src, asgType, genTypeSize(asgType), false /*isBlkReqd*/);
                return tree;
            }
        }
        else
        {
            noway_assert(dest->OperIsLocal());
            destLclVarTree = dest;
        }
        if (destLclVarTree != nullptr)
        {
            destVarNum = destLclVarTree->AsLclVarCommon()->GetLclNum();
            destVarDsc = &(lvaTable[destVarNum]);
            if (asgType == TYP_STRUCT)
            {
                clsHnd = destVarDsc->lvVerTypeInfo.GetClassHandle();
                size   = destVarDsc->lvExactSize;
            }
        }
        if (asgType != TYP_STRUCT)
        {
            size = genTypeSize(asgType);
        }
    }
    if (size == 0)
    {
        return nullptr;
    }

    if ((destVarDsc != nullptr) && varTypeIsStruct(destLclVarTree) && destVarDsc->lvPromoted)
    {
        // Let fgMorphCopyBlock handle it.
        return nullptr;
    }

    if (src->IsCall() || src->OperIsSIMD())
    {
        // Can't take ADDR from these nodes, let fgMorphCopyBlock handle it, #11413.
        return nullptr;
    }

    if ((destVarDsc != nullptr) && !varTypeIsStruct(destVarDsc->TypeGet()))
    {

        //
        //  See if we can do a simple transformation:
        //
        //          GT_ASG <TYP_size>
        //          /   \.
        //      GT_IND GT_IND or CNS_INT
        //         |      |
        //       [dest] [src]
        //

        if (asgType == TYP_STRUCT)
        {
            // It is possible to use `initobj` to init a primitive type on the stack,
            // like `ldloca.s 1; initobj 1B000003` where `V01` has type `ref`;
            // in this case we generate `ASG struct(BLK<8> struct(ADDR byref(LCL_VAR ref)), 0)`
            // and this code path transforms it into `ASG ref(LCL_VARref, 0)` because it is not a real
            // struct assignment.

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

#ifdef TARGET_64BIT
                    case 4:
                        asgType = TYP_INT;
                        break;
#endif // TARGET_64BIT
                }
            }
        }
    }

    GenTree*   srcLclVarTree = nullptr;
    LclVarDsc* srcVarDsc     = nullptr;
    if (isCopyBlock)
    {
        if (src->OperGet() == GT_LCL_VAR)
        {
            srcLclVarTree = src;
            srcVarDsc     = &(lvaTable[src->AsLclVarCommon()->GetLclNum()]);
        }
        else if (src->OperIsIndir() && impIsAddressInLocal(src->AsOp()->gtOp1, &srcLclVarTree))
        {
            srcVarDsc = &(lvaTable[srcLclVarTree->AsLclVarCommon()->GetLclNum()]);
        }
        if ((srcVarDsc != nullptr) && varTypeIsStruct(srcLclVarTree) && srcVarDsc->lvPromoted)
        {
            // Let fgMorphCopyBlock handle it.
            return nullptr;
        }
    }

    if (asgType != TYP_STRUCT)
    {
        noway_assert((size <= REGSIZE_BYTES) || varTypeIsSIMD(asgType));

        // For initBlk, a non constant source is not going to allow us to fiddle
        // with the bits to create a single assigment.
        // Nor do we (for now) support transforming an InitBlock of SIMD type, unless
        // it is a direct assignment to a lclVar and the value is zero.
        if (isInitBlock)
        {
            if (!src->IsConstInitVal())
            {
                return nullptr;
            }
            if (varTypeIsSIMD(asgType) && (!src->IsIntegralConst(0) || (destVarDsc == nullptr)))
            {
                return nullptr;
            }
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
            if (varTypeIsStruct(destLclVarTree) && destVarDsc->lvPromoted)
            {
                // Let fgMorphInitBlock handle it.  (Since we'll need to do field-var-wise assignments.)
                return nullptr;
            }
            else if (!varTypeIsFloating(destLclVarTree->TypeGet()) && (size == genTypeSize(destVarDsc)))
            {
                // Use the dest local var directly, as well as its type.
                dest    = destLclVarTree;
                asgType = destVarDsc->lvType;

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
                destLclVarTree->gtFlags |= GTF_VAR_DEF;
                if (size < destVarDsc->lvExactSize)
                { // If it's not a full-width assignment....
                    destLclVarTree->gtFlags |= GTF_VAR_USEASG;
                }

                if (dest == destLclVarTree)
                {
                    GenTree* addr = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);
                    dest          = gtNewIndir(asgType, addr);
                }
            }
        }

        // Check to ensure we don't have a reducible *(& ... )
        if (dest->OperIsIndir() && dest->AsIndir()->Addr()->OperGet() == GT_ADDR)
        {
            // If dest is an Indir or Block, and it has a child that is a Addr node
            //
            GenTree* addrNode = dest->AsIndir()->Addr(); // known to be a GT_ADDR

            // Can we just remove the Ind(Addr(destOp)) and operate directly on 'destOp'?
            //
            GenTree*  destOp     = addrNode->gtGetOp1();
            var_types destOpType = destOp->TypeGet();

            // We can if we have a primitive integer type and the sizes are exactly the same.
            //
            if ((varTypeIsIntegralOrI(destOp) && (size == genTypeSize(destOpType))))
            {
                dest    = destOp;
                asgType = destOpType;
            }
        }

        if (dest->gtEffectiveVal()->OperIsIndir())
        {
            // If we have no information about the destination, we have to assume it could
            // live anywhere (not just in the GC heap).
            // Mark the GT_IND node so that we use the correct write barrier helper in case
            // the field is a GC ref.

            if (!fgIsIndirOfAddrOfLocal(dest))
            {
                dest->gtFlags |= (GTF_GLOB_REF | GTF_IND_TGTANYWHERE);
                tree->gtFlags |= GTF_GLOB_REF;
            }

            dest->SetIndirExceptionFlags(this);
            tree->gtFlags |= (dest->gtFlags & GTF_EXCEPT);
        }

        if (isCopyBlock)
        {
            if (srcVarDsc != nullptr)
            {
                // Handled above.
                assert(!varTypeIsStruct(srcLclVarTree) || !srcVarDsc->lvPromoted);
                if (!varTypeIsFloating(srcLclVarTree->TypeGet()) &&
                    size == genTypeSize(genActualType(srcLclVarTree->TypeGet())))
                {
                    // Use the src local var directly.
                    src = srcLclVarTree;
                }
                else
                {
                    // The source argument of the copyblk can potentially be accessed only through indir(addr(lclVar))
                    // or indir(lclVarAddr) so it must be on the stack.
                    unsigned lclVarNum = srcLclVarTree->AsLclVarCommon()->GetLclNum();
                    lvaSetVarDoNotEnregister(lclVarNum DEBUGARG(DNER_BlockOp));
                    GenTree* srcAddr;
                    if (src == srcLclVarTree)
                    {
                        srcAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, src);
                        src     = gtNewOperNode(GT_IND, asgType, srcAddr);
                    }
                    else
                    {
                        assert(src->OperIsIndir());
                    }
                }
            }

            if (src->OperIsIndir())
            {
                if (!fgIsIndirOfAddrOfLocal(src))
                {
                    // If we have no information about the src, we have to assume it could
                    // live anywhere (not just in the GC heap).
                    // Mark the GT_IND node so that we use the correct write barrier helper in case
                    // the field is a GC ref.
                    src->gtFlags |= (GTF_GLOB_REF | GTF_IND_TGTANYWHERE);
                }

                src->SetIndirExceptionFlags(this);
            }
        }
        else // InitBlk
        {
#if FEATURE_SIMD
            if (varTypeIsSIMD(asgType))
            {
                assert(!isCopyBlock); // Else we would have returned the tree above.
                noway_assert(src->IsIntegralConst(0));
                noway_assert(destVarDsc != nullptr);

                src = new (this, GT_SIMD) GenTreeSIMD(asgType, src, SIMDIntrinsicInit, destVarDsc->lvBaseType, size);
            }
            else
#endif
            {
                if (src->OperIsInitVal())
                {
                    src = src->gtGetOp1();
                }
                assert(src->IsCnsIntOrI());
                // This will mutate the integer constant, in place, to be the correct
                // value for the type we are using in the assignment.
                src->AsIntCon()->FixupInitBlkValue(asgType);
            }
        }

        // Ensure that the dest is setup appropriately.
        if (dest->gtEffectiveVal()->OperIsIndir())
        {
            dest = fgMorphBlockOperand(dest, asgType, size, false /*isBlkReqd*/);
        }

        // Ensure that the rhs is setup appropriately.
        if (isCopyBlock)
        {
            src = fgMorphBlockOperand(src, asgType, size, false /*isBlkReqd*/);
        }

        // Set the lhs and rhs on the assignment.
        if (dest != tree->AsOp()->gtOp1)
        {
            asg->AsOp()->gtOp1 = dest;
        }
        if (src != asg->AsOp()->gtOp2)
        {
            asg->AsOp()->gtOp2 = src;
        }

        asg->ChangeType(asgType);
        dest->gtFlags |= GTF_DONT_CSE;
        asg->gtFlags &= ~GTF_EXCEPT;
        asg->gtFlags |= ((dest->gtFlags | src->gtFlags) & GTF_ALL_EFFECT);
        // Un-set GTF_REVERSE_OPS, and it will be set later if appropriate.
        asg->gtFlags &= ~GTF_REVERSE_OPS;

#ifdef DEBUG
        if (verbose)
        {
            printf("fgMorphOneAsgBlock (after):\n");
            gtDispTree(tree);
        }
#endif
        return tree;
    }

    return nullptr;
}

//------------------------------------------------------------------------
// fgMorphInitBlock: Morph a block initialization assignment tree.
//
// Arguments:
//    tree - A GT_ASG tree that performs block initialization
//
// Return Value:
//    A single assignment, when fgMorphOneAsgBlockOp transforms it.
//
//    If the destination is a promoted struct local variable then we will try to
//    perform a field by field assignment for each of the promoted struct fields.
//    This is not always possible (e.g. if the struct has holes and custom layout).
//
//    Otherwise the orginal GT_ASG tree is returned unmodified (always correct but
//    least desirable because it prevents enregistration and/or blocks independent
//    struct promotion).
//
// Assumptions:
//    GT_ASG's children have already been morphed.
//
GenTree* Compiler::fgMorphInitBlock(GenTree* tree)
{
    // We must have the GT_ASG form of InitBlkOp.
    noway_assert((tree->OperGet() == GT_ASG) && tree->OperIsInitBlkOp());
#ifdef DEBUG
    bool morphed = false;
#endif // DEBUG

    GenTree* src      = tree->gtGetOp2();
    GenTree* origDest = tree->gtGetOp1();

    GenTree* dest = fgMorphBlkNode(origDest, true);
    if (dest != origDest)
    {
        tree->AsOp()->gtOp1 = dest;
    }
    tree->gtType = dest->TypeGet();
    JITDUMP("\nfgMorphInitBlock:");

    GenTree* oneAsgTree = fgMorphOneAsgBlockOp(tree);
    if (oneAsgTree)
    {
        JITDUMP(" using oneAsgTree.\n");
        tree = oneAsgTree;
    }
    else
    {
        GenTreeLclVarCommon* destLclNode = nullptr;
        unsigned             destLclNum  = BAD_VAR_NUM;
        LclVarDsc*           destLclVar  = nullptr;
        GenTree*             initVal     = src->OperIsInitVal() ? src->gtGetOp1() : src;
        unsigned             blockSize   = 0;

        if (dest->IsLocal())
        {
            destLclNode = dest->AsLclVarCommon();
            destLclNum  = destLclNode->GetLclNum();
            destLclVar  = lvaGetDesc(destLclNum);
            blockSize   = varTypeIsStruct(destLclVar) ? destLclVar->lvExactSize : genTypeSize(destLclVar->TypeGet());
        }
        else
        {
            blockSize = dest->AsBlk()->Size();

            FieldSeqNode* destFldSeq = nullptr;
            if (dest->AsIndir()->Addr()->IsLocalAddrExpr(this, &destLclNode, &destFldSeq))
            {
                destLclNum = destLclNode->GetLclNum();
                destLclVar = lvaGetDesc(destLclNum);
            }
        }

        bool destDoFldAsg = false;

        if (destLclNum != BAD_VAR_NUM)
        {
#if LOCAL_ASSERTION_PROP
            // Kill everything about destLclNum (and its field locals)
            if (optLocalAssertionProp && (optAssertionCount > 0))
            {
                fgKillDependentAssertions(destLclNum DEBUGARG(tree));
            }
#endif // LOCAL_ASSERTION_PROP

            // If we have already determined that a promoted TYP_STRUCT lclVar will not be enregistered,
            // we are better off doing a block init.
            if (destLclVar->lvPromoted && (!destLclVar->lvDoNotEnregister || !destLclNode->TypeIs(TYP_STRUCT)))
            {
                GenTree* newTree = fgMorphPromoteLocalInitBlock(destLclNode->AsLclVar(), initVal, blockSize);

                if (newTree != nullptr)
                {
                    tree         = newTree;
                    destDoFldAsg = true;
                    INDEBUG(morphed = true);
                }
            }

            // If destLclVar is not a reg-sized non-field-addressed struct, set it as DoNotEnregister.
            if (!destDoFldAsg && !destLclVar->lvRegStruct)
            {
                lvaSetVarDoNotEnregister(destLclNum DEBUGARG(DNER_BlockOp));
            }
        }

        if (!destDoFldAsg)
        {
            // For an InitBlock we always require a block operand.
            dest                = fgMorphBlockOperand(dest, dest->TypeGet(), blockSize, true /*isBlkReqd*/);
            tree->AsOp()->gtOp1 = dest;
            tree->gtFlags |= (dest->gtFlags & GTF_ALL_EFFECT);
        }
    }

#ifdef DEBUG
    if (morphed)
    {
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;

        if (verbose)
        {
            printf("fgMorphInitBlock (after):\n");
            gtDispTree(tree);
        }
    }
#endif

    return tree;
}

//------------------------------------------------------------------------
// fgMorphPromoteLocalInitBlock: Attempts to promote a local block init tree
// to a tree of promoted field initialization assignments.
//
// Arguments:
//    destLclNode - The destination LclVar node
//    initVal - The initialization value
//    blockSize - The amount of bytes to initialize
//
// Return Value:
//    A tree that performs field by field initialization of the destination
//    struct variable if various conditions are met, nullptr otherwise.
//
// Notes:
//    This transforms a single block initialization assignment like:
//
//    *  ASG       struct (init)
//    +--*  BLK(12)   struct
//    |  \--*  ADDR      long
//    |     \--*  LCL_VAR   struct(P) V02 loc0
//    |     \--*    int    V02.a (offs=0x00) -> V06 tmp3
//    |     \--*    ubyte  V02.c (offs=0x04) -> V07 tmp4
//    |     \--*    float  V02.d (offs=0x08) -> V08 tmp5
//    \--*  INIT_VAL  int
//       \--*  CNS_INT   int    42
//
//    into a COMMA tree of assignments that initialize each promoted struct
//    field:
//
//    *  COMMA     void
//    +--*  COMMA     void
//    |  +--*  ASG       int
//    |  |  +--*  LCL_VAR   int    V06 tmp3
//    |  |  \--*  CNS_INT   int    0x2A2A2A2A
//    |  \--*  ASG       ubyte
//    |     +--*  LCL_VAR   ubyte  V07 tmp4
//    |     \--*  CNS_INT   int    42
//    \--*  ASG       float
//       +--*  LCL_VAR   float  V08 tmp5
//       \--*  CNS_DBL   float  1.5113661732714390e-13
//
GenTree* Compiler::fgMorphPromoteLocalInitBlock(GenTreeLclVar* destLclNode, GenTree* initVal, unsigned blockSize)
{
    assert(destLclNode->OperIs(GT_LCL_VAR));

    LclVarDsc* destLclVar = lvaGetDesc(destLclNode);
    assert(varTypeIsStruct(destLclVar->TypeGet()));
    assert(destLclVar->lvPromoted);

    if (blockSize == 0)
    {
        JITDUMP(" size is zero or unknown.\n");
        return nullptr;
    }

    if (destLclVar->lvAddrExposed && destLclVar->lvContainsHoles)
    {
        JITDUMP(" dest is address exposed and contains holes.\n");
        return nullptr;
    }

    if (destLclVar->lvCustomLayout && destLclVar->lvContainsHoles)
    {
        JITDUMP(" dest has custom layout and contains holes.\n");
        return nullptr;
    }

    if (destLclVar->lvExactSize != blockSize)
    {
        JITDUMP(" dest size mismatch.\n");
        return nullptr;
    }

    if (!initVal->OperIs(GT_CNS_INT))
    {
        JITDUMP(" source is not constant.\n");
        return nullptr;
    }

    const int64_t initPattern = (initVal->AsIntCon()->IconValue() & 0xFF) * 0x0101010101010101LL;

    if (initPattern != 0)
    {
        for (unsigned i = 0; i < destLclVar->lvFieldCnt; ++i)
        {
            LclVarDsc* fieldDesc = lvaGetDesc(destLclVar->lvFieldLclStart + i);

            if (varTypeIsSIMD(fieldDesc->TypeGet()) || varTypeIsGC(fieldDesc->TypeGet()))
            {
                // Cannot initialize GC or SIMD types with a non-zero constant.
                // The former is completly bogus. The later restriction could be
                // lifted by supporting non-zero SIMD constants or by generating
                // field initialization code that converts an integer constant to
                // the appropiate SIMD value. Unlikely to be very useful, though.
                JITDUMP(" dest contains GC and/or SIMD fields and source constant is not 0.\n");
                return nullptr;
            }
        }
    }

    JITDUMP(" using field by field initialization.\n");

    GenTree* tree = nullptr;

    for (unsigned i = 0; i < destLclVar->lvFieldCnt; ++i)
    {
        unsigned   fieldLclNum = destLclVar->lvFieldLclStart + i;
        LclVarDsc* fieldDesc   = lvaGetDesc(fieldLclNum);
        GenTree*   dest        = gtNewLclvNode(fieldLclNum, fieldDesc->TypeGet());
        // If it had been labeled a "USEASG", assignments to the individual promoted fields are not.
        dest->gtFlags |= (destLclNode->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG));

        GenTree* src;

        switch (dest->TypeGet())
        {
            case TYP_BOOL:
            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_USHORT:
                // Promoted fields are expected to be "normalize on load". If that changes then
                // we may need to adjust this code to widen the constant correctly.
                assert(fieldDesc->lvNormalizeOnLoad());
                __fallthrough;
            case TYP_INT:
            {
                int64_t mask = (int64_t(1) << (genTypeSize(dest->TypeGet()) * 8)) - 1;
                src          = gtNewIconNode(static_cast<int32_t>(initPattern & mask));
                break;
            }
            case TYP_LONG:
                src = gtNewLconNode(initPattern);
                break;
            case TYP_FLOAT:
                float floatPattern;
                memcpy(&floatPattern, &initPattern, sizeof(floatPattern));
                src = gtNewDconNode(floatPattern, dest->TypeGet());
                break;
            case TYP_DOUBLE:
                double doublePattern;
                memcpy(&doublePattern, &initPattern, sizeof(doublePattern));
                src = gtNewDconNode(doublePattern, dest->TypeGet());
                break;
            case TYP_REF:
            case TYP_BYREF:
#ifdef FEATURE_SIMD
            case TYP_SIMD8:
            case TYP_SIMD12:
            case TYP_SIMD16:
            case TYP_SIMD32:
#endif // FEATURE_SIMD
                assert(initPattern == 0);
                src = gtNewIconNode(0, dest->TypeGet());
                break;
            default:
                unreached();
        }

        GenTree* asg = gtNewAssignNode(dest, src);

#if LOCAL_ASSERTION_PROP
        if (optLocalAssertionProp)
        {
            optAssertionGen(asg);
        }
#endif // LOCAL_ASSERTION_PROP

        if (tree != nullptr)
        {
            tree = gtNewOperNode(GT_COMMA, TYP_VOID, tree, asg);
        }
        else
        {
            tree = asg;
        }
    }

    return tree;
}

//------------------------------------------------------------------------
// fgMorphGetStructAddr: Gets the address of a struct object
//
// Arguments:
//    pTree    - the parent's pointer to the struct object node
//    clsHnd   - the class handle for the struct type
//    isRValue - true if this is a source (not dest)
//
// Return Value:
//    Returns the address of the struct value, possibly modifying the existing tree to
//    sink the address below any comma nodes (this is to canonicalize for value numbering).
//    If this is a source, it will morph it to an GT_IND before taking its address,
//    since it may not be remorphed (and we don't want blk nodes as rvalues).

GenTree* Compiler::fgMorphGetStructAddr(GenTree** pTree, CORINFO_CLASS_HANDLE clsHnd, bool isRValue)
{
    GenTree* addr;
    GenTree* tree = *pTree;
    // If this is an indirection, we can return its op1, unless it's a GTF_IND_ARR_INDEX, in which case we
    // need to hang onto that for the purposes of value numbering.
    if (tree->OperIsIndir())
    {
        if ((tree->gtFlags & GTF_IND_ARR_INDEX) == 0)
        {
            addr = tree->AsOp()->gtOp1;
        }
        else
        {
            if (isRValue && tree->OperIsBlk())
            {
                tree->ChangeOper(GT_IND);
            }
            addr = gtNewOperNode(GT_ADDR, TYP_BYREF, tree);
        }
    }
    else if (tree->gtOper == GT_COMMA)
    {
        // If this is a comma, we're going to "sink" the GT_ADDR below it.
        (void)fgMorphGetStructAddr(&(tree->AsOp()->gtOp2), clsHnd, isRValue);
        tree->gtType = TYP_BYREF;
        addr         = tree;
    }
    else
    {
        switch (tree->gtOper)
        {
            case GT_LCL_FLD:
            case GT_LCL_VAR:
            case GT_INDEX:
            case GT_FIELD:
            case GT_ARR_ELEM:
                addr = gtNewOperNode(GT_ADDR, TYP_BYREF, tree);
                break;
            case GT_INDEX_ADDR:
                addr = tree;
                break;
            default:
            {
                // TODO: Consider using lvaGrabTemp and gtNewTempAssign instead, since we're
                // not going to use "temp"
                GenTree* temp = fgInsertCommaFormTemp(pTree, clsHnd);
                assert(!compDoOldStructRetyping());
                unsigned lclNum = temp->gtEffectiveVal()->AsLclVar()->GetLclNum();
                lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DNER_VMNeedsStackAddr));
                addr = fgMorphGetStructAddr(pTree, clsHnd, isRValue);
                break;
            }
        }
    }
    *pTree = addr;
    return addr;
}

//------------------------------------------------------------------------
// fgMorphBlkNode: Morph a block node preparatory to morphing a block assignment
//
// Arguments:
//    tree   - The struct type node
//    isDest - True if this is the destination of the assignment
//
// Return Value:
//    Returns the possibly-morphed node. The caller is responsible for updating
//    the parent of this node..

GenTree* Compiler::fgMorphBlkNode(GenTree* tree, bool isDest)
{
    JITDUMP("fgMorphBlkNode for %s tree, before:\n", (isDest ? "dst" : "src"));
    DISPTREE(tree);
    GenTree* handleTree = nullptr;
    GenTree* addr       = nullptr;
    if (tree->OperIs(GT_COMMA))
    {
        // In order to CSE and value number array index expressions and bounds checks,
        // the commas in which they are contained need to match.
        // The pattern is that the COMMA should be the address expression.
        // Therefore, we insert a GT_ADDR just above the node, and wrap it in an obj or ind.
        // TODO-1stClassStructs: Consider whether this can be improved.
        // Example:
        //   before: [3] comma struct <- [2] comma struct <- [1] LCL_VAR struct
        //   after: [3] comma byref <- [2] comma byref <- [4] addr byref <- [1] LCL_VAR struct

        addr                  = tree;
        GenTree* effectiveVal = tree->gtEffectiveVal();

        GenTreePtrStack commas(getAllocator(CMK_ArrayStack));
        for (GenTree* comma = tree; comma != nullptr && comma->gtOper == GT_COMMA; comma = comma->gtGetOp2())
        {
            commas.Push(comma);
        }

        GenTree* lastComma = commas.Top();
        noway_assert(lastComma->gtGetOp2() == effectiveVal);
        GenTree* effectiveValAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, effectiveVal);
#ifdef DEBUG
        effectiveValAddr->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
        lastComma->AsOp()->gtOp2 = effectiveValAddr;

        while (!commas.Empty())
        {
            GenTree* comma = commas.Pop();
            comma->gtType  = TYP_BYREF;
            gtUpdateNodeSideEffects(comma);
        }

        handleTree = effectiveVal;
    }
    else if (tree->OperIs(GT_IND) && tree->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR))
    {
        handleTree = tree;
        addr       = tree->AsIndir()->Addr();
    }

    if (addr != nullptr)
    {
        var_types structType = handleTree->TypeGet();
        if (structType == TYP_STRUCT)
        {
            CORINFO_CLASS_HANDLE structHnd = gtGetStructHandleIfPresent(handleTree);
            if (structHnd == NO_CLASS_HANDLE)
            {
                tree = gtNewOperNode(GT_IND, structType, addr);
            }
            else
            {
                tree = gtNewObjNode(structHnd, addr);
                gtSetObjGcInfo(tree->AsObj());
            }
        }
        else
        {
            tree = new (this, GT_BLK) GenTreeBlk(GT_BLK, structType, addr, typGetBlkLayout(genTypeSize(structType)));
        }

        gtUpdateNodeSideEffects(tree);
#ifdef DEBUG
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
    }

    if (!tree->OperIsBlk())
    {
        JITDUMP("fgMorphBlkNode after:\n");
        DISPTREE(tree);
        return tree;
    }
    GenTreeBlk* blkNode = tree->AsBlk();
    if (blkNode->OperGet() == GT_DYN_BLK)
    {
        if (blkNode->AsDynBlk()->gtDynamicSize->IsCnsIntOrI())
        {
            unsigned size = (unsigned)blkNode->AsDynBlk()->gtDynamicSize->AsIntConCommon()->IconValue();
            // A GT_BLK with size of zero is not supported,
            // so if we encounter such a thing we just leave it as a GT_DYN_BLK
            if (size != 0)
            {
                blkNode->AsDynBlk()->gtDynamicSize = nullptr;
                blkNode->ChangeOper(GT_BLK);
                blkNode->SetLayout(typGetBlkLayout(size));
            }
            else
            {
                JITDUMP("fgMorphBlkNode after, DYN_BLK with zero size can't be morphed:\n");
                DISPTREE(blkNode);
                return blkNode;
            }
        }
        else
        {
            JITDUMP("fgMorphBlkNode after, DYN_BLK with non-const size can't be morphed:\n");
            DISPTREE(blkNode);
            return blkNode;
        }
    }
    GenTree* blkSrc = blkNode->Addr();
    assert(blkSrc != nullptr);
    if (!blkNode->TypeIs(TYP_STRUCT) && blkSrc->OperIs(GT_ADDR) && blkSrc->gtGetOp1()->OperIs(GT_LCL_VAR))
    {
        GenTreeLclVarCommon* lclVarNode = blkSrc->gtGetOp1()->AsLclVarCommon();
        if ((genTypeSize(blkNode) != genTypeSize(lclVarNode)) || (!isDest && !varTypeIsStruct(lclVarNode)))
        {
            lvaSetVarDoNotEnregister(lclVarNode->GetLclNum() DEBUG_ARG(DNER_VMNeedsStackAddr));
        }
    }

    JITDUMP("fgMorphBlkNode after:\n");
    DISPTREE(tree);
    return tree;
}

//------------------------------------------------------------------------
// fgMorphBlockOperand: Canonicalize an operand of a block assignment
//
// Arguments:
//    tree       - The block operand
//    asgType    - The type of the assignment
//    blockWidth - The size of the block
//    isBlkReqd  - true iff this operand must remain a block node
//
// Return Value:
//    Returns the morphed block operand
//
// Notes:
//    This does the following:
//    - Ensures that a struct operand is a block node or lclVar.
//    - Ensures that any COMMAs are above ADDR nodes.
//    Although 'tree' WAS an operand of a block assignment, the assignment
//    may have been retyped to be a scalar assignment.

GenTree* Compiler::fgMorphBlockOperand(GenTree* tree, var_types asgType, unsigned blockWidth, bool isBlkReqd)
{
    GenTree* effectiveVal = tree->gtEffectiveVal();

    if (asgType != TYP_STRUCT)
    {
        if (effectiveVal->OperIsIndir())
        {
            if (!isBlkReqd)
            {
                GenTree* addr = effectiveVal->AsIndir()->Addr();
                if ((addr->OperGet() == GT_ADDR) && (addr->gtGetOp1()->TypeGet() == asgType))
                {
                    effectiveVal = addr->gtGetOp1();
                }
                else if (effectiveVal->OperIsBlk())
                {
                    effectiveVal->SetOper(GT_IND);
                }
            }
            effectiveVal->gtType = asgType;
        }
        else if (effectiveVal->TypeGet() != asgType)
        {
            if (effectiveVal->IsCall())
            {
#ifdef DEBUG
                GenTreeCall* call = effectiveVal->AsCall();
                assert(call->TypeGet() == TYP_STRUCT);
                assert(blockWidth == info.compCompHnd->getClassSize(call->gtRetClsHnd));
#endif
            }
            else
            {
                GenTree* addr = gtNewOperNode(GT_ADDR, TYP_BYREF, effectiveVal);
                effectiveVal  = gtNewIndir(asgType, addr);
            }
        }
    }
    else
    {
        GenTreeIndir*        indirTree        = nullptr;
        GenTreeLclVarCommon* lclNode          = nullptr;
        bool                 needsIndirection = true;

        if (effectiveVal->OperIsIndir())
        {
            indirTree     = effectiveVal->AsIndir();
            GenTree* addr = effectiveVal->AsIndir()->Addr();
            if ((addr->OperGet() == GT_ADDR) && (addr->gtGetOp1()->OperGet() == GT_LCL_VAR))
            {
                lclNode = addr->gtGetOp1()->AsLclVarCommon();
            }
        }
        else if (effectiveVal->OperGet() == GT_LCL_VAR)
        {
            lclNode = effectiveVal->AsLclVarCommon();
        }
        else if (effectiveVal->IsCall())
        {
            needsIndirection = false;
#ifdef DEBUG
            GenTreeCall* call = effectiveVal->AsCall();
            assert(call->TypeGet() == TYP_STRUCT);
            assert(blockWidth == info.compCompHnd->getClassSize(call->gtRetClsHnd));
#endif
        }

        if (lclNode != nullptr)
        {
            LclVarDsc* varDsc = &(lvaTable[lclNode->GetLclNum()]);
            if (varTypeIsStruct(varDsc) && (varDsc->lvExactSize == blockWidth) && (varDsc->lvType == asgType))
            {
                if (effectiveVal != lclNode)
                {
                    JITDUMP("Replacing block node [%06d] with lclVar V%02u\n", dspTreeID(tree), lclNode->GetLclNum());
                    effectiveVal = lclNode;
                }
                needsIndirection = false;
            }
            else
            {
                // This may be a lclVar that was determined to be address-exposed.
                effectiveVal->gtFlags |= (lclNode->gtFlags & GTF_ALL_EFFECT);
            }
        }
        if (needsIndirection)
        {
            if (indirTree != nullptr)
            {
                if (indirTree->OperIsBlk() && !isBlkReqd)
                {
                    effectiveVal->SetOper(GT_IND);
                }
                else
                {
                    // If we have an indirection and a block is required, it should already be a block.
                    assert(indirTree->OperIsBlk() || !isBlkReqd);
                }
                effectiveVal->gtType = asgType;
            }
            else
            {
                GenTree* newTree;
                GenTree* addr = gtNewOperNode(GT_ADDR, TYP_BYREF, effectiveVal);
                if (isBlkReqd)
                {
                    CORINFO_CLASS_HANDLE clsHnd = gtGetStructHandleIfPresent(effectiveVal);
                    if (clsHnd == NO_CLASS_HANDLE)
                    {
                        newTree = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, addr, typGetBlkLayout(blockWidth));
                    }
                    else
                    {
                        newTree = gtNewObjNode(clsHnd, addr);
                        gtSetObjGcInfo(newTree->AsObj());
                    }
                }
                else
                {
                    newTree = gtNewIndir(asgType, addr);
                }
                effectiveVal = newTree;
            }
        }
    }
    assert(effectiveVal->TypeIs(asgType) || (varTypeIsSIMD(asgType) && varTypeIsStruct(effectiveVal)));
    tree = effectiveVal;
    return tree;
}

//------------------------------------------------------------------------
// fgMorphCopyBlock: Perform the Morphing of block copy
//
// Arguments:
//    tree - a block copy (i.e. an assignment with a block op on the lhs).
//
// Return Value:
//    We can return the orginal block copy unmodified (least desirable, but always correct)
//    We can return a single assignment, when fgMorphOneAsgBlockOp transforms it (most desirable).
//    If we have performed struct promotion of the Source() or the Dest() then we will try to
//    perform a field by field assignment for each of the promoted struct fields.
//
// Assumptions:
//    The child nodes for tree have already been Morphed.
//
// Notes:
//    If we leave it as a block copy we will call lvaSetVarDoNotEnregister() on both Source() and Dest().
//    When performing a field by field assignment we can have one of Source() or Dest treated as a blob of bytes
//    and in such cases we will call lvaSetVarDoNotEnregister() on the one treated as a blob of bytes.
//    if the Source() or Dest() is a a struct that has a "CustomLayout" and "ConstainsHoles" then we
//    can not use a field by field assignment and must leave the orginal block copy unmodified.

GenTree* Compiler::fgMorphCopyBlock(GenTree* tree)
{
    noway_assert(tree->OperIsCopyBlkOp());

    JITDUMP("fgMorphCopyBlock:\n");

    bool isLateArg = (tree->gtFlags & GTF_LATE_ARG) != 0;

    GenTreeOp* asg  = tree->AsOp();
    GenTree*   src  = asg->gtGetOp2();
    GenTree*   dest = asg->gtGetOp1();

#if FEATURE_MULTIREG_RET
    // If this is a multi-reg return, we will not do any morphing of this node.
    if (src->IsMultiRegCall())
    {
        assert(dest->OperGet() == GT_LCL_VAR);
        JITDUMP(" not morphing a multireg call return\n");
        return tree;
    }
#endif // FEATURE_MULTIREG_RET

    if (src->IsCall() && !compDoOldStructRetyping())
    {
        if (dest->OperIs(GT_OBJ))
        {
            GenTreeLclVar* lclVar = fgMorphTryFoldObjAsLclVar(dest->AsObj());
            if (lclVar != nullptr)
            {
                dest       = lclVar;
                asg->gtOp1 = lclVar;
            }
        }

        if (dest->OperIs(GT_LCL_VAR))
        {
            LclVarDsc* varDsc = lvaGetDesc(dest->AsLclVar());
            if (varTypeIsStruct(varDsc) && varDsc->CanBeReplacedWithItsField(this))
            {
                JITDUMP(" not morphing a single reg call return\n");
                return tree;
            }
        }
    }

    // If we have an array index on the lhs, we need to create an obj node.

    dest = fgMorphBlkNode(dest, true);
    if (dest != asg->gtGetOp1())
    {
        asg->gtOp1 = dest;
        if (dest->IsLocal())
        {
            dest->gtFlags |= GTF_VAR_DEF;
        }
    }
#ifdef DEBUG
    if (asg->TypeGet() != dest->TypeGet())
    {
        JITDUMP("changing type of dest from %-6s to %-6s\n", varTypeName(asg->TypeGet()), varTypeName(dest->TypeGet()));
    }
#endif
    asg->ChangeType(dest->TypeGet());
    src = fgMorphBlkNode(src, false);

    asg->gtOp2 = src;

    GenTree* oldTree    = tree;
    GenTree* oneAsgTree = fgMorphOneAsgBlockOp(tree);

    if (oneAsgTree)
    {
        JITDUMP(" using oneAsgTree.\n");
        tree = oneAsgTree;
    }
    else
    {
        unsigned             blockWidth;
        bool                 blockWidthIsConst = false;
        GenTreeLclVarCommon* lclVarTree        = nullptr;
        GenTreeLclVarCommon* srcLclVarTree     = nullptr;
        unsigned             destLclNum        = BAD_VAR_NUM;
        unsigned             modifiedLclNum    = BAD_VAR_NUM;
        LclVarDsc*           destLclVar        = nullptr;
        FieldSeqNode*        destFldSeq        = nullptr;
        bool                 destDoFldAsg      = false;
        GenTree*             destAddr          = nullptr;
        GenTree*             srcAddr           = nullptr;
        bool                 destOnStack       = false;
        bool                 hasGCPtrs         = false;

        JITDUMP("block assignment to morph:\n");
        DISPTREE(asg);

        if (dest->IsLocal())
        {
            blockWidthIsConst = true;
            destOnStack       = true;
            modifiedLclNum    = dest->AsLclVarCommon()->GetLclNum();
            if (dest->gtOper == GT_LCL_VAR)
            {
                lclVarTree = dest->AsLclVarCommon();
                destLclNum = modifiedLclNum;
                destLclVar = &lvaTable[destLclNum];
                if (destLclVar->lvType == TYP_STRUCT)
                {
                    // It would be nice if lvExactSize always corresponded to the size of the struct,
                    // but it doesn't always for the temps that the importer creates when it spills side
                    // effects.
                    // TODO-Cleanup: Determine when this happens, and whether it can be changed.
                    blockWidth = info.compCompHnd->getClassSize(destLclVar->lvVerTypeInfo.GetClassHandle());
                }
                else
                {
                    blockWidth = genTypeSize(destLclVar->lvType);
                }
                hasGCPtrs = destLclVar->HasGCPtr();
            }
            else
            {
                assert(dest->TypeGet() != TYP_STRUCT);
                assert(dest->gtOper == GT_LCL_FLD);
                blockWidth = genTypeSize(dest->TypeGet());
                destAddr   = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);
                destFldSeq = dest->AsLclFld()->GetFieldSeq();
            }
        }
        else
        {
            GenTree* effectiveDest = dest->gtEffectiveVal();
            if (effectiveDest->OperGet() == GT_IND)
            {
                assert(dest->TypeGet() != TYP_STRUCT);
                blockWidth        = genTypeSize(effectiveDest->TypeGet());
                blockWidthIsConst = true;
                if ((dest == effectiveDest) && ((dest->gtFlags & GTF_IND_ARR_INDEX) == 0))
                {
                    destAddr = dest->gtGetOp1();
                }
            }
            else
            {
                assert(effectiveDest->OperIsBlk());
                GenTreeBlk* blk = effectiveDest->AsBlk();

                blockWidth        = blk->Size();
                blockWidthIsConst = (blk->gtOper != GT_DYN_BLK);
                if ((dest == effectiveDest) && ((dest->gtFlags & GTF_IND_ARR_INDEX) == 0))
                {
                    destAddr = blk->Addr();
                }
            }
            if (destAddr != nullptr)
            {
                noway_assert(destAddr->TypeGet() == TYP_BYREF || destAddr->TypeGet() == TYP_I_IMPL);
                if (destAddr->IsLocalAddrExpr(this, &lclVarTree, &destFldSeq))
                {
                    destOnStack    = true;
                    destLclNum     = lclVarTree->GetLclNum();
                    modifiedLclNum = destLclNum;
                    destLclVar     = &lvaTable[destLclNum];
                }
            }
        }

#if LOCAL_ASSERTION_PROP
        // Kill everything about modifiedLclNum (and its field locals)
        if ((modifiedLclNum != BAD_VAR_NUM) && optLocalAssertionProp)
        {
            if (optAssertionCount > 0)
            {
                fgKillDependentAssertions(modifiedLclNum DEBUGARG(tree));
            }
        }
#endif // LOCAL_ASSERTION_PROP

        if (destLclVar != nullptr)
        {
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

        if (src->IsLocal())
        {
            srcLclVarTree = src->AsLclVarCommon();
            srcLclNum     = srcLclVarTree->GetLclNum();
            if (src->OperGet() == GT_LCL_FLD)
            {
                srcFldSeq = src->AsLclFld()->GetFieldSeq();
            }
        }
        else if (src->OperIsIndir())
        {
            if (src->AsOp()->gtOp1->IsLocalAddrExpr(this, &srcLclVarTree, &srcFldSeq))
            {
                srcLclNum = srcLclVarTree->GetLclNum();
            }
            else
            {
                srcAddr = src->AsOp()->gtOp1;
            }
        }

        if (srcLclNum != BAD_VAR_NUM)
        {
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

        // Check to see if we are doing a copy to/from the same local block.
        // If so, morph it to a nop.
        if ((destLclVar != nullptr) && (srcLclVar == destLclVar) && (destFldSeq == srcFldSeq) &&
            destFldSeq != FieldSeqStore::NotAField())
        {
            JITDUMP("Self-copy; replaced with a NOP.\n");
            GenTree* nop = gtNewNothingNode();
            INDEBUG(nop->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);
            return nop;
        }

        // Check to see if we are required to do a copy block because the struct contains holes
        // and either the src or dest is externally visible
        //
        bool requiresCopyBlock   = false;
        bool srcSingleLclVarAsg  = false;
        bool destSingleLclVarAsg = false;

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

#if defined(TARGET_ARM)
        if ((src->OperIsIndir()) && (src->gtFlags & GTF_IND_UNALIGNED))
        {
            JITDUMP(" src is unaligned");
            requiresCopyBlock = true;
        }

        if (asg->gtFlags & GTF_BLK_UNALIGNED)
        {
            JITDUMP(" asg is unaligned");
            requiresCopyBlock = true;
        }
#endif // TARGET_ARM

        // Can't use field by field assignment if the src is a call.
        if (src->OperGet() == GT_CALL)
        {
            JITDUMP(" src is a call");
            // C++ style CopyBlock with holes
            requiresCopyBlock = true;
        }

        // If we passed the above checks, then we will check these two
        if (!requiresCopyBlock)
        {
            // It is not always profitable to do field by field init for structs that are allocated to memory.
            // A struct with 8 bool fields will require 8 moves instead of one if we do this transformation.
            // A simple heuristic when field by field copy is prefered:
            // - if fields can be enregistered;
            // - if the struct has GCPtrs (block copy would be done via helper that is expensive);
            // - if the struct has only one field.
            bool dstFldIsProfitable =
                ((destLclVar != nullptr) &&
                 (!destLclVar->lvDoNotEnregister || destLclVar->HasGCPtr() || (destLclVar->lvFieldCnt == 1)));
            bool srcFldIsProfitable =
                ((srcLclVar != nullptr) &&
                 (!srcLclVar->lvDoNotEnregister || srcLclVar->HasGCPtr() || (srcLclVar->lvFieldCnt == 1)));
            // Are both dest and src promoted structs?
            if (destDoFldAsg && srcDoFldAsg && (dstFldIsProfitable || srcFldIsProfitable))
            {
                // Both structs should be of the same type, or have the same number of fields of the same type.
                // If not we will use a copy block.
                bool misMatchedTypes = false;
                if (destLclVar->lvVerTypeInfo.GetClassHandle() != srcLclVar->lvVerTypeInfo.GetClassHandle())
                {
                    if (destLclVar->lvFieldCnt != srcLclVar->lvFieldCnt)
                    {
                        misMatchedTypes = true;
                    }
                    else
                    {
                        for (int i = 0; i < destLclVar->lvFieldCnt; i++)
                        {
                            LclVarDsc* destFieldVarDsc = lvaGetDesc(destLclVar->lvFieldLclStart + i);
                            LclVarDsc* srcFieldVarDsc  = lvaGetDesc(srcLclVar->lvFieldLclStart + i);
                            if ((destFieldVarDsc->lvType != srcFieldVarDsc->lvType) ||
                                (destFieldVarDsc->lvFldOffset != srcFieldVarDsc->lvFldOffset))
                            {
                                misMatchedTypes = true;
                                break;
                            }
                        }
                    }
                    if (misMatchedTypes)
                    {
                        requiresCopyBlock = true; // Mismatched types, leave as a CopyBlock
                        JITDUMP(" with mismatched types");
                    }
                }
            }
            else if (destDoFldAsg && dstFldIsProfitable)
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
            else if (srcDoFldAsg && srcFldIsProfitable)
            {
                // Check for the symmetric case (which happens for the _pointer field of promoted spans):
                //
                //               [000240] -----+------             /--*  lclVar    struct(P) V18 tmp9
                //                                                  /--*    byref  V18._value (offs=0x00) -> V30 tmp21
                //               [000245] -A------R---             *  =         struct (copy)
                //               [000244] -----+------             \--*  obj(8)    struct
                //               [000243] -----+------                \--*  addr      byref
                //               [000242] D----+-N----                   \--*  lclVar    byref  V28 tmp19
                //
                if (blockWidthIsConst && (srcLclVar->lvFieldCnt == 1) && (destLclVar != nullptr) &&
                    (blockWidth == genTypeSize(destLclVar->TypeGet())))
                {
                    // Check for type agreement
                    unsigned  fieldLclNum = lvaTable[srcLclNum].lvFieldLclStart;
                    var_types srcType     = lvaTable[fieldLclNum].TypeGet();
                    if (destLclVar->TypeGet() == srcType)
                    {
                        destSingleLclVarAsg = true;
                    }
                }
            }
            // Are neither dest or src promoted structs?
            else
            {
                assert(!(destDoFldAsg && dstFldIsProfitable) && !(srcDoFldAsg && srcFldIsProfitable));
                requiresCopyBlock = true; // Leave as a CopyBlock
                JITDUMP(" with no promoted structs");
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

        // Mark the dest/src structs as DoNotEnreg when they are not being fully referenced as the same type.
        //
        if (!destDoFldAsg && (destLclVar != nullptr) && !destSingleLclVarAsg)
        {
            if (!destLclVar->lvRegStruct || (destLclVar->lvType != dest->TypeGet()))
            {
                if (!dest->IsMultiRegLclVar() || (blockWidth != destLclVar->lvExactSize) ||
                    (destLclVar->lvCustomLayout && destLclVar->lvContainsHoles))
                {
                    // Mark it as DoNotEnregister.
                    lvaSetVarDoNotEnregister(destLclNum DEBUGARG(DNER_BlockOp));
                }
                else if (dest->IsMultiRegLclVar())
                {
                    // Handle this as lvIsMultiRegRet; this signals to SSA that it can't consider these fields
                    // SSA candidates (we don't have a way to represent multiple SSANums on MultiRegLclVar nodes).
                    destLclVar->lvIsMultiRegRet = true;
                }
            }
        }

        if (!srcDoFldAsg && (srcLclVar != nullptr) && !srcSingleLclVarAsg)
        {
            if (!srcLclVar->lvRegStruct || (srcLclVar->lvType != dest->TypeGet()))
            {
                lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DNER_BlockOp));
            }
        }

        var_types asgType = dest->TypeGet();
        if (requiresCopyBlock)
        {
            bool isBlkReqd     = (asgType == TYP_STRUCT);
            dest               = fgMorphBlockOperand(dest, asgType, blockWidth, isBlkReqd);
            asg->AsOp()->gtOp1 = dest;
            asg->gtFlags |= (dest->gtFlags & GTF_ALL_EFFECT);

            // Eliminate the "OBJ or BLK" node on the src.
            src                = fgMorphBlockOperand(src, asgType, blockWidth, false /*!isBlkReqd*/);
            asg->AsOp()->gtOp2 = src;

            goto _Done;
        }

        //
        // Otherwise we convert this CopyBlock into individual field by field assignments
        //
        tree = nullptr;

        GenTree* addrSpill            = nullptr;
        unsigned addrSpillTemp        = BAD_VAR_NUM;
        bool     addrSpillIsStackDest = false; // true if 'addrSpill' represents the address in our local stack frame

        unsigned fieldCnt = DUMMY_INIT(0);

        if (destDoFldAsg && srcDoFldAsg)
        {
            // To do fieldwise assignments for both sides, they'd better be the same struct type!
            // All of these conditions were checked above...
            assert(destLclNum != BAD_VAR_NUM && srcLclNum != BAD_VAR_NUM);
            assert(destLclVar != nullptr && srcLclVar != nullptr && destLclVar->lvFieldCnt == srcLclVar->lvFieldCnt);

            fieldCnt = destLclVar->lvFieldCnt;
            goto _AssignFields; // No need to spill the address to the temp. Go ahead to morph it into field
                                // assignments.
        }
        else if (destDoFldAsg)
        {
            fieldCnt = destLclVar->lvFieldCnt;
            src      = fgMorphBlockOperand(src, asgType, blockWidth, false /*isBlkReqd*/);
            if (srcAddr == nullptr)
            {
                srcAddr = fgMorphGetStructAddr(&src, destLclVar->lvVerTypeInfo.GetClassHandle(), true /* rValue */);
            }
        }
        else
        {
            assert(srcDoFldAsg);
            fieldCnt = srcLclVar->lvFieldCnt;
            dest     = fgMorphBlockOperand(dest, asgType, blockWidth, false /*isBlkReqd*/);
            if (dest->OperIsBlk())
            {
                dest->SetOper(GT_IND);
                dest->gtType = TYP_STRUCT;
            }
            destAddr = gtNewOperNode(GT_ADDR, TYP_BYREF, dest);
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
            }
        }

        // TODO-CQ: this should be based on a more general
        // "BaseAddress" method, that handles fields of structs, before or after
        // morphing.
        if ((addrSpill != nullptr) && addrSpill->OperIs(GT_ADDR))
        {
            GenTree* addrSpillOp = addrSpill->AsOp()->gtGetOp1();
            if (addrSpillOp->IsLocal())
            {
                // We will *not* consider this to define the local, but rather have each individual field assign
                // be a definition.
                addrSpillOp->gtFlags &= ~(GTF_LIVENESS_MASK);
                assert(lvaGetPromotionType(addrSpillOp->AsLclVarCommon()->GetLclNum()) != PROMOTION_TYPE_INDEPENDENT);
                addrSpillIsStackDest = true; // addrSpill represents the address of LclVar[varNum] in our
                                             // local stack frame
            }
        }

        if (addrSpill != nullptr)
        {
            // Simplify the address if possible, and mark as DONT_CSE as needed..
            addrSpill = fgMorphTree(addrSpill);

            // Spill the (complex) address to a BYREF temp.
            // Note, at most one address may need to be spilled.
            addrSpillTemp = lvaGrabTemp(true DEBUGARG("BlockOp address local"));

            lvaTable[addrSpillTemp].lvType = TYP_BYREF;

            if (addrSpillIsStackDest)
            {
                lvaTable[addrSpillTemp].lvStackByref = true;
            }

            tree = gtNewAssignNode(gtNewLclvNode(addrSpillTemp, TYP_BYREF), addrSpill);

            // If we are assigning the address of a LclVar here
            // liveness does not account for this kind of address taken use.
            //
            // We have to mark this local as address exposed so
            // that we don't delete the definition for this LclVar
            // as a dead store later on.
            //
            if (addrSpill->OperGet() == GT_ADDR)
            {
                GenTree* addrOp = addrSpill->AsOp()->gtOp1;
                if (addrOp->IsLocal())
                {
                    unsigned lclVarNum                = addrOp->AsLclVarCommon()->GetLclNum();
                    lvaTable[lclVarNum].lvAddrExposed = true;
                    lvaSetVarDoNotEnregister(lclVarNum DEBUGARG(DNER_AddrExposed));
                }
            }
        }

    _AssignFields:

        // We may have allocated a temp above, and that may have caused the lvaTable to be expanded.
        // So, beyond this point we cannot rely on the old values of 'srcLclVar' and 'destLclVar'.
        for (unsigned i = 0; i < fieldCnt; ++i)
        {
            GenTree* dstFld;
            if (destDoFldAsg)
            {
                noway_assert(destLclNum != BAD_VAR_NUM);
                unsigned dstFieldLclNum = lvaTable[destLclNum].lvFieldLclStart + i;
                dstFld                  = gtNewLclvNode(dstFieldLclNum, lvaTable[dstFieldLclNum].TypeGet());
                // If it had been labeled a "USEASG", assignments to the individual promoted fields are not.
                if (destAddr != nullptr)
                {
                    noway_assert(destAddr->AsOp()->gtOp1->gtOper == GT_LCL_VAR);
                    dstFld->gtFlags |= destAddr->AsOp()->gtOp1->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);
                }
                else
                {
                    noway_assert(lclVarTree != nullptr);
                    dstFld->gtFlags |= lclVarTree->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);
                }
                // Don't CSE the lhs of an assignment.
                dstFld->gtFlags |= GTF_DONT_CSE;
            }
            else
            {
                noway_assert(srcDoFldAsg);

                if (destSingleLclVarAsg)
                {
                    noway_assert(fieldCnt == 1);
                    noway_assert(destLclVar != nullptr);
                    noway_assert(addrSpill == nullptr);

                    dstFld = gtNewLclvNode(destLclNum, destLclVar->TypeGet());
                }
                else
                {
                    if (addrSpill)
                    {
                        assert(addrSpillTemp != BAD_VAR_NUM);
                        dstFld = gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                    }
                    else
                    {
                        dstFld = gtCloneExpr(destAddr);
                        noway_assert(dstFld != nullptr);

                        // Is the address of a local?
                        GenTreeLclVarCommon* lclVarTree = nullptr;
                        bool                 isEntire   = false;
                        bool*                pIsEntire  = (blockWidthIsConst ? &isEntire : nullptr);
                        if (dstFld->DefinesLocalAddr(this, blockWidth, &lclVarTree, pIsEntire))
                        {
                            lclVarTree->gtFlags |= GTF_VAR_DEF;
                            if (!isEntire)
                            {
                                lclVarTree->gtFlags |= GTF_VAR_USEASG;
                            }
                        }
                    }

                    LclVarDsc* srcVarDsc      = lvaGetDesc(srcLclNum);
                    unsigned   srcFieldLclNum = srcVarDsc->lvFieldLclStart + i;
                    LclVarDsc* srcFieldVarDsc = lvaGetDesc(srcFieldLclNum);

                    // Have to set the field sequence -- which means we need the field handle.
                    CORINFO_CLASS_HANDLE classHnd = srcVarDsc->lvVerTypeInfo.GetClassHandle();
                    CORINFO_FIELD_HANDLE fieldHnd =
                        info.compCompHnd->getFieldInClass(classHnd, srcFieldVarDsc->lvFldOrdinal);
                    FieldSeqNode* curFieldSeq = GetFieldSeqStore()->CreateSingleton(fieldHnd);

                    unsigned srcFieldOffset = lvaGetDesc(srcFieldLclNum)->lvFldOffset;

                    if (srcFieldOffset == 0)
                    {
                        fgAddFieldSeqForZeroOffset(dstFld, curFieldSeq);
                    }
                    else
                    {
                        GenTree* fieldOffsetNode = gtNewIconNode(srcFieldVarDsc->lvFldOffset, curFieldSeq);
                        dstFld                   = gtNewOperNode(GT_ADD, TYP_BYREF, dstFld, fieldOffsetNode);
                    }

                    dstFld = gtNewIndir(srcFieldVarDsc->TypeGet(), dstFld);

                    // !!! The destination could be on stack. !!!
                    // This flag will let us choose the correct write barrier.
                    dstFld->gtFlags |= GTF_IND_TGTANYWHERE;
                }
            }

            GenTree* srcFld;
            if (srcDoFldAsg)
            {
                noway_assert(srcLclNum != BAD_VAR_NUM);
                unsigned srcFieldLclNum = lvaTable[srcLclNum].lvFieldLclStart + i;
                srcFld                  = gtNewLclvNode(srcFieldLclNum, lvaTable[srcFieldLclNum].TypeGet());

                noway_assert(srcLclVarTree != nullptr);
                srcFld->gtFlags |= srcLclVarTree->gtFlags & ~GTF_NODE_MASK;
            }
            else
            {
                noway_assert(destDoFldAsg);
                noway_assert(destLclNum != BAD_VAR_NUM);
                unsigned dstFieldLclNum = lvaTable[destLclNum].lvFieldLclStart + i;

                if (srcSingleLclVarAsg)
                {
                    noway_assert(fieldCnt == 1);
                    noway_assert(srcLclNum != BAD_VAR_NUM);
                    noway_assert(addrSpill == nullptr);

                    srcFld = gtNewLclvNode(srcLclNum, lvaGetDesc(srcLclNum)->TypeGet());
                }
                else
                {
                    if (addrSpill)
                    {
                        assert(addrSpillTemp != BAD_VAR_NUM);
                        srcFld = gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                    }
                    else
                    {
                        srcFld = gtCloneExpr(srcAddr);
                        noway_assert(srcFld != nullptr);
                    }

                    CORINFO_CLASS_HANDLE classHnd = lvaTable[destLclNum].lvVerTypeInfo.GetClassHandle();
                    CORINFO_FIELD_HANDLE fieldHnd =
                        info.compCompHnd->getFieldInClass(classHnd, lvaTable[dstFieldLclNum].lvFldOrdinal);
                    FieldSeqNode* curFieldSeq = GetFieldSeqStore()->CreateSingleton(fieldHnd);
                    var_types     destType    = lvaGetDesc(dstFieldLclNum)->lvType;

                    bool done = false;
                    if (lvaGetDesc(dstFieldLclNum)->lvFldOffset == 0)
                    {
                        // If this is a full-width use of the src via a different type, we need to create a GT_LCL_FLD.
                        // (Note that if it was the same type, 'srcSingleLclVarAsg' would be true.)
                        if (srcLclNum != BAD_VAR_NUM)
                        {
                            noway_assert(srcLclVarTree != nullptr);
                            assert(destType != TYP_STRUCT);
                            unsigned destSize = genTypeSize(destType);
                            srcLclVar         = lvaGetDesc(srcLclNum);
                            unsigned srcSize =
                                (srcLclVar->lvType == TYP_STRUCT) ? srcLclVar->lvExactSize : genTypeSize(srcLclVar);
                            if (destSize == srcSize)
                            {
                                srcLclVarTree->gtFlags |= GTF_VAR_CAST;
                                srcLclVarTree->ChangeOper(GT_LCL_FLD);
                                srcLclVarTree->gtType = destType;
                                srcLclVarTree->AsLclFld()->SetFieldSeq(curFieldSeq);
                                srcFld = srcLclVarTree;
                                done   = true;
                            }
                        }
                    }
                    if (!done)
                    {
                        unsigned fldOffset = lvaGetDesc(dstFieldLclNum)->lvFldOffset;
                        if (fldOffset == 0)
                        {
                            fgAddFieldSeqForZeroOffset(srcFld, curFieldSeq);
                        }
                        else
                        {
                            GenTreeIntCon* fldOffsetNode = gtNewIconNode(fldOffset, curFieldSeq);
                            srcFld                       = gtNewOperNode(GT_ADD, TYP_BYREF, srcFld, fldOffsetNode);
                        }
                        srcFld = gtNewIndir(destType, srcFld);
                    }
                }
            }

            noway_assert(dstFld->TypeGet() == srcFld->TypeGet());

            asg = gtNewAssignNode(dstFld, srcFld);

            // If we spilled the address, and we didn't do individual field assignments to promoted fields,
            // and it was of a local, ensure that the destination local variable has been marked as address
            // exposed. Neither liveness nor SSA are able to track this kind of indirect assignments.
            if (addrSpill && !destDoFldAsg && destLclNum != BAD_VAR_NUM)
            {
                noway_assert(lvaGetDesc(destLclNum)->lvAddrExposed);
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
    if (tree != oldTree)
    {
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
    }
#endif

_Done:

    JITDUMP("\nfgMorphCopyBlock (after):\n");
    DISPTREE(tree);

    return tree;
}

// insert conversions and normalize to make tree amenable to register
// FP architectures
GenTree* Compiler::fgMorphForRegisterFP(GenTree* tree)
{
    if (tree->OperIsArithmetic())
    {
        if (varTypeIsFloating(tree))
        {
            GenTree* op1 = tree->AsOp()->gtOp1;
            GenTree* op2 = tree->gtGetOp2();

            assert(varTypeIsFloating(op1->TypeGet()) && varTypeIsFloating(op2->TypeGet()));

            if (op1->TypeGet() != tree->TypeGet())
            {
                tree->AsOp()->gtOp1 = gtNewCastNode(tree->TypeGet(), op1, false, tree->TypeGet());
            }
            if (op2->TypeGet() != tree->TypeGet())
            {
                tree->AsOp()->gtOp2 = gtNewCastNode(tree->TypeGet(), op2, false, tree->TypeGet());
            }
        }
    }
    else if (tree->OperIsCompare())
    {
        GenTree* op1 = tree->AsOp()->gtOp1;

        if (varTypeIsFloating(op1))
        {
            GenTree* op2 = tree->gtGetOp2();
            assert(varTypeIsFloating(op2));

            if (op1->TypeGet() != op2->TypeGet())
            {
                // both had better be floating, just one bigger than other
                if (op1->TypeGet() == TYP_FLOAT)
                {
                    assert(op2->TypeGet() == TYP_DOUBLE);
                    tree->AsOp()->gtOp1 = gtNewCastNode(TYP_DOUBLE, op1, false, TYP_DOUBLE);
                }
                else if (op2->TypeGet() == TYP_FLOAT)
                {
                    assert(op1->TypeGet() == TYP_DOUBLE);
                    tree->AsOp()->gtOp2 = gtNewCastNode(TYP_DOUBLE, op2, false, TYP_DOUBLE);
                }
            }
        }
    }

    return tree;
}

#ifdef FEATURE_SIMD

//--------------------------------------------------------------------------------------------------------------
// getSIMDStructFromField:
//   Checking whether the field belongs to a simd struct or not. If it is, return the GenTree* for
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
//       A GenTree* which points the simd lclvar tree belongs to. If the tree is not the simd
//       instrinic related field, return nullptr.
//

GenTree* Compiler::getSIMDStructFromField(GenTree*   tree,
                                          var_types* pBaseTypeOut,
                                          unsigned*  indexOut,
                                          unsigned*  simdSizeOut,
                                          bool       ignoreUsedInSIMDIntrinsic /*false*/)
{
    GenTree* ret = nullptr;
    if (tree->OperGet() == GT_FIELD)
    {
        GenTree* objRef = tree->AsField()->gtFldObj;
        if (objRef != nullptr)
        {
            GenTree* obj = nullptr;
            if (objRef->gtOper == GT_ADDR)
            {
                obj = objRef->AsOp()->gtOp1;
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
                unsigned   lclNum = obj->AsLclVarCommon()->GetLclNum();
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
#ifdef FEATURE_HW_INTRINSICS
            else if (obj->OperIsHWIntrinsic())
            {
                ret                          = obj;
                GenTreeHWIntrinsic* simdNode = obj->AsHWIntrinsic();
                *simdSizeOut                 = simdNode->gtSIMDSize;
                *pBaseTypeOut                = simdNode->gtSIMDBaseType;
            }
#endif // FEATURE_HW_INTRINSICS
        }
    }
    if (ret != nullptr)
    {
        unsigned BaseTypeSize = genTypeSize(*pBaseTypeOut);
        *indexOut             = tree->AsField()->gtFldOffset / BaseTypeSize;
    }
    return ret;
}

/*****************************************************************************
*  If a read operation tries to access simd struct field, then transform the
*  operation to the SIMD intrinsic SIMDIntrinsicGetItem, and return the new tree.
*  Otherwise, return the old tree.
*  Argument:
*   tree - GenTree*. If this pointer points to simd struct which is used for simd
*          intrinsic, we will morph it as simd intrinsic SIMDIntrinsicGetItem.
*  Return:
*   A GenTree* which points to the new tree. If the tree is not for simd intrinsic,
*   return nullptr.
*/

GenTree* Compiler::fgMorphFieldToSIMDIntrinsicGet(GenTree* tree)
{
    unsigned  index          = 0;
    var_types baseType       = TYP_UNKNOWN;
    unsigned  simdSize       = 0;
    GenTree*  simdStructNode = getSIMDStructFromField(tree, &baseType, &index, &simdSize);
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
*  SIMDIntrinsicSet*, and return a new tree. If it is not such an assignment,
*  then return the old tree.
*  Argument:
*   tree - GenTree*. If this pointer points to simd struct which is used for simd
*          intrinsic, we will morph it as simd intrinsic set.
*  Return:
*   A GenTree* which points to the new tree. If the tree is not for simd intrinsic,
*   return nullptr.
*/

GenTree* Compiler::fgMorphFieldAssignToSIMDIntrinsicSet(GenTree* tree)
{
    assert(tree->OperGet() == GT_ASG);
    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    unsigned  index         = 0;
    var_types baseType      = TYP_UNKNOWN;
    unsigned  simdSize      = 0;
    GenTree*  simdOp1Struct = getSIMDStructFromField(op1, &baseType, &index, &simdSize);
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

        GenTree* target = gtClone(simdOp1Struct);
        assert(target != nullptr);
        var_types simdType = target->gtType;
        GenTree*  simdTree = gtNewSIMDNode(simdType, simdOp1Struct, op2, simdIntrinsicID, baseType, simdSize);

        tree->AsOp()->gtOp1 = target;
        tree->AsOp()->gtOp2 = simdTree;

        // fgMorphTree has already called fgMorphImplicitByRefArgs() on this assignment, but the source
        // and target have not yet been morphed.
        // Therefore, in case the source and/or target are now implicit byrefs, we need to call it again.
        if (fgMorphImplicitByRefArgs(tree))
        {
            if (tree->gtGetOp1()->OperIsBlk())
            {
                assert(tree->gtGetOp1()->TypeGet() == simdType);
                tree->gtGetOp1()->SetOper(GT_IND);
                tree->gtGetOp1()->gtType = simdType;
            }
        }
#ifdef DEBUG
        tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
    }

    return tree;
}

#endif // FEATURE_SIMD

/*****************************************************************************
 *
 *  Transform the given GTK_SMPOP tree for code generation.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTree* Compiler::fgMorphSmpOp(GenTree* tree, MorphAddrContext* mac)
{
    ALLOCA_CHECK();
    assert(tree->OperKind() & GTK_SMPOP);

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
        tree = fgMorphForRegisterFP(tree);
    }

    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();
    GenTree*   op1  = tree->AsOp()->gtOp1;
    GenTree*   op2  = tree->gtGetOp2IfPresent();

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
            noway_assert(op1 == tree->AsOp()->gtOp1);
            op2 = tree->AsOp()->gtOp2;

#ifdef FEATURE_SIMD
            {
                // We should check whether op2 should be assigned to a SIMD field or not.
                // If it is, we should tranlate the tree to simd intrinsic.
                assert(!fgGlobalMorph || ((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) == 0));
                GenTree* newTree = fgMorphFieldAssignToSIMDIntrinsicSet(tree);
                typ              = tree->TypeGet();
                op1              = tree->gtGetOp1();
                op2              = tree->gtGetOp2();
#ifdef DEBUG
                assert((tree == newTree) && (tree->OperGet() == oper));
                if ((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) != 0)
                {
                    tree->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
                }
#endif // DEBUG
            }
#endif

            // We can't CSE the LHS of an assignment. Only r-values can be CSEed.
            // Previously, the "lhs" (addr) of a block op was CSE'd.  So, to duplicate the former
            // behavior, allow CSE'ing if is a struct type (or a TYP_REF transformed from a struct type)
            // TODO-1stClassStructs: improve this.
            if (op1->IsLocal() || (op1->TypeGet() != TYP_STRUCT))
            {
                op1->gtFlags |= GTF_DONT_CSE;
            }
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
                GenTree* effOp1 = op1->gtEffectiveVal();
                noway_assert((effOp1->gtOper == GT_CNS_INT) &&
                             (effOp1->IsIntegralConst(0) || effOp1->IsIntegralConst(1)));
            }
            break;

        case GT_COLON:
#if LOCAL_ASSERTION_PROP
            if (optLocalAssertionProp)
#endif
            {
                isQmarkColon = true;
            }
            break;

        case GT_INDEX:
            return fgMorphArrayIndex(tree);

        case GT_CAST:
            return fgMorphCast(tree);

        case GT_MUL:

#ifndef TARGET_64BIT
            if (typ == TYP_LONG)
            {
                /* For (long)int1 * (long)int2, we dont actually do the
                   casts, and just multiply the 32 bit values, which will
                   give us the 64 bit result in edx:eax */

                noway_assert(op2);
                if ((op1->gtOper == GT_CAST && op2->gtOper == GT_CAST &&
                     genActualType(op1->CastFromType()) == TYP_INT && genActualType(op2->CastFromType()) == TYP_INT) &&
                    !op1->gtOverflow() && !op2->gtOverflow())
                {
                    // The casts have to be of the same signedness.
                    if ((op1->gtFlags & GTF_UNSIGNED) != (op2->gtFlags & GTF_UNSIGNED))
                    {
                        // We see if we can force an int constant to change its signedness
                        GenTree* constOp;
                        if (op1->AsCast()->CastOp()->gtOper == GT_CNS_INT)
                            constOp = op1;
                        else if (op2->AsCast()->CastOp()->gtOper == GT_CNS_INT)
                            constOp = op2;
                        else
                            goto NO_MUL_64RSLT;

                        if (((unsigned)(constOp->AsCast()->CastOp()->AsIntCon()->gtIconVal) < (unsigned)(0x80000000)))
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

                    op1->AsOp()->gtOp1 = fgMorphTree(op1->AsOp()->gtOp1);
                    op2->AsOp()->gtOp1 = fgMorphTree(op2->AsOp()->gtOp1);

                    // Propagate side effect flags up the tree
                    op1->gtFlags &= ~GTF_ALL_EFFECT;
                    op1->gtFlags |= (op1->AsOp()->gtOp1->gtFlags & GTF_ALL_EFFECT);
                    op2->gtFlags &= ~GTF_ALL_EFFECT;
                    op2->gtFlags |= (op2->AsOp()->gtOp1->gtFlags & GTF_ALL_EFFECT);

                    // If the GT_MUL can be altogether folded away, we should do that.

                    if ((op1->AsCast()->CastOp()->OperKind() & op2->AsCast()->CastOp()->OperKind() & GTK_CONST) &&
                        opts.OptEnabled(CLFLG_CONSTANTFOLD))
                    {
                        tree->AsOp()->gtOp1 = op1 = gtFoldExprConst(op1);
                        tree->AsOp()->gtOp2 = op2 = gtFoldExprConst(op2);
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

                    if (op1->AsCast()->CastOp()->OperGet() != GT_NOP)
                    {
                        op1->AsOp()->gtOp1 = gtNewOperNode(GT_NOP, TYP_INT, op1->AsCast()->CastOp());
                        op1->gtFlags &= ~GTF_ALL_EFFECT;
                        op1->gtFlags |= (op1->AsCast()->CastOp()->gtFlags & GTF_ALL_EFFECT);
                    }

                    if (op2->AsCast()->CastOp()->OperGet() != GT_NOP)
                    {
                        op2->AsOp()->gtOp1 = gtNewOperNode(GT_NOP, TYP_INT, op2->AsCast()->CastOp());
                        op2->gtFlags &= ~GTF_ALL_EFFECT;
                        op2->gtFlags |= (op2->AsCast()->CastOp()->gtFlags & GTF_ALL_EFFECT);
                    }

                    op1->gtFlags |= GTF_DONT_CSE;
                    op2->gtFlags |= GTF_DONT_CSE;

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
#endif // !TARGET_64BIT
            break;

        case GT_DIV:
            // Replace "val / dcon" with "val * (1.0 / dcon)" if dcon is a power of two.
            // Powers of two within range are always exactly represented,
            // so multiplication by the reciprocal is safe in this scenario
            if (fgGlobalMorph && op2->IsCnsFltOrDbl())
            {
                double divisor = op2->AsDblCon()->gtDconVal;
                if (((typ == TYP_DOUBLE) && FloatingPointUtils::hasPreciseReciprocal(divisor)) ||
                    ((typ == TYP_FLOAT) && FloatingPointUtils::hasPreciseReciprocal(forceCastToFloat(divisor))))
                {
                    oper = GT_MUL;
                    tree->ChangeOper(oper);
                    op2->AsDblCon()->gtDconVal = 1.0 / divisor;
                }
            }

            // array.Length is always positive so GT_DIV can be changed to GT_UDIV
            // if op2 is a positive cns
            if (!optValnumCSE_phase && op1->OperIs(GT_ARR_LENGTH) && op2->IsIntegralConst() &&
                op2->AsIntCon()->IconValue() >= 2) // for 0 and 1 it doesn't matter if it's UDIV or DIV
            {
                assert(tree->OperIs(GT_DIV));
                tree->ChangeOper(GT_UDIV);
                return fgMorphSmpOp(tree, mac);
            }

#ifndef TARGET_64BIT
            if (typ == TYP_LONG)
            {
                helper = CORINFO_HELP_LDIV;
                goto USE_HELPER_FOR_ARITH;
            }

#if USE_HELPERS_FOR_INT_DIV
            if (typ == TYP_INT)
            {
                helper = CORINFO_HELP_DIV;
                goto USE_HELPER_FOR_ARITH;
            }
#endif
#endif // !TARGET_64BIT

            if (op2->gtOper == GT_CAST && op2->AsOp()->gtOp1->IsCnsIntOrI())
            {
                op2 = gtFoldExprConst(op2);
            }
            break;

        case GT_UDIV:

#ifndef TARGET_64BIT
            if (typ == TYP_LONG)
            {
                helper = CORINFO_HELP_ULDIV;
                goto USE_HELPER_FOR_ARITH;
            }
#if USE_HELPERS_FOR_INT_DIV
            if (typ == TYP_INT)
            {
                helper = CORINFO_HELP_UDIV;
                goto USE_HELPER_FOR_ARITH;
            }
#endif
#endif // TARGET_64BIT
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
                        tree->AsOp()->gtOp1 = op1 = gtNewCastNode(TYP_DOUBLE, op1, false, TYP_DOUBLE);
                    }
                }
                else if (op2->TypeGet() == TYP_FLOAT)
                {
                    tree->AsOp()->gtOp2 = op2 = gtNewCastNode(TYP_DOUBLE, op2, false, TYP_DOUBLE);
                }
                goto USE_HELPER_FOR_ARITH;
            }

            // array.Length is always positive so GT_DIV can be changed to GT_UDIV
            // if op2 is a positive cns
            if (!optValnumCSE_phase && op1->OperIs(GT_ARR_LENGTH) && op2->IsIntegralConst() &&
                op2->AsIntCon()->IconValue() >= 2) // for 0 and 1 it doesn't matter if it's UMOD or MOD
            {
                assert(tree->OperIs(GT_MOD));
                tree->ChangeOper(GT_UMOD);
                return fgMorphSmpOp(tree, mac);
            }

            // Do not use optimizations (unlike UMOD's idiv optimizing during codegen) for signed mod.
            // A similar optimization for signed mod will not work for a negative perfectly divisible
            // HI-word. To make it correct, we would need to divide without the sign and then flip the
            // result sign after mod. This requires 18 opcodes + flow making it not worthy to inline.
            goto ASSIGN_HELPER_FOR_MOD;

        case GT_UMOD:

#ifdef TARGET_ARMARCH
//
// Note for TARGET_ARMARCH we don't have  a remainder instruction, so we don't do this optimization
//
#else  // TARGET_XARCH
            /* If this is an unsigned long mod with op2 which is a cast to long from a
               constant int, then don't morph to a call to the helper.  This can be done
               faster inline using idiv.
            */

            noway_assert(op2);
            if ((typ == TYP_LONG) && opts.OptEnabled(CLFLG_CONSTANTFOLD) &&
                ((tree->gtFlags & GTF_UNSIGNED) == (op1->gtFlags & GTF_UNSIGNED)) &&
                ((tree->gtFlags & GTF_UNSIGNED) == (op2->gtFlags & GTF_UNSIGNED)))
            {
                if (op2->gtOper == GT_CAST && op2->AsCast()->CastOp()->gtOper == GT_CNS_INT &&
                    op2->AsCast()->CastOp()->AsIntCon()->gtIconVal >= 2 &&
                    op2->AsCast()->CastOp()->AsIntCon()->gtIconVal <= 0x3fffffff &&
                    (tree->gtFlags & GTF_UNSIGNED) == (op2->AsCast()->CastOp()->gtFlags & GTF_UNSIGNED))
                {
                    tree->AsOp()->gtOp2 = op2 = fgMorphCast(op2);
                    noway_assert(op2->gtOper == GT_CNS_NATIVELONG);
                }

                if (op2->gtOper == GT_CNS_NATIVELONG && op2->AsIntConCommon()->LngValue() >= 2 &&
                    op2->AsIntConCommon()->LngValue() <= 0x3fffffff)
                {
                    tree->AsOp()->gtOp1 = op1 = fgMorphTree(op1);
                    noway_assert(op1->TypeGet() == TYP_LONG);

                    // Update flags for op1 morph
                    tree->gtFlags &= ~GTF_ALL_EFFECT;

                    tree->gtFlags |= (op1->gtFlags & GTF_ALL_EFFECT); // Only update with op1 as op2 is a constant

                    // If op1 is a constant, then do constant folding of the division operator
                    if (op1->gtOper == GT_CNS_NATIVELONG)
                    {
                        tree = gtFoldExpr(tree);
                    }

                    tree->AsOp()->CheckDivideByConstOptimized(this);
                    return tree;
                }
            }
#endif // TARGET_XARCH

        ASSIGN_HELPER_FOR_MOD:

            // For "val % 1", return 0 if op1 doesn't have any side effects
            // and we are not in the CSE phase, we cannot discard 'tree'
            // because it may contain CSE expressions that we haven't yet examined.
            //
            if (((op1->gtFlags & GTF_SIDE_EFFECT) == 0) && !optValnumCSE_phase)
            {
                if (op2->IsIntegralConst(1))
                {
                    GenTree* zeroNode = gtNewZeroConNode(typ);
#ifdef DEBUG
                    zeroNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                    DEBUG_DESTROY_NODE(tree);
                    return zeroNode;
                }
            }

#ifndef TARGET_64BIT
            if (typ == TYP_LONG)
            {
                helper = (oper == GT_UMOD) ? CORINFO_HELP_ULMOD : CORINFO_HELP_LMOD;
                goto USE_HELPER_FOR_ARITH;
            }

#if USE_HELPERS_FOR_INT_DIV
            if (typ == TYP_INT)
            {
                if (oper == GT_UMOD)
                {
                    helper = CORINFO_HELP_UMOD;
                    goto USE_HELPER_FOR_ARITH;
                }
                else if (oper == GT_MOD)
                {
                    helper = CORINFO_HELP_MOD;
                    goto USE_HELPER_FOR_ARITH;
                }
            }
#endif
#endif // !TARGET_64BIT

            if (op2->gtOper == GT_CAST && op2->AsOp()->gtOp1->IsCnsIntOrI())
            {
                op2 = gtFoldExprConst(op2);
            }

#ifdef TARGET_ARM64
            // For ARM64 we don't have a remainder instruction,
            // The architecture manual suggests the following transformation to
            // generate code for such operator:
            //
            // a % b = a - (a / b) * b;
            //
            // TODO: there are special cases where it can be done better, for example
            // when the modulo operation is unsigned and the divisor is a
            // integer constant power of two.  In this case, we can make the transform:
            //
            // a % b = a & (b - 1);
            //
            // Lower supports it for all cases except when `a` is constant, but
            // in Morph we can't guarantee that `a` won't be transformed into a constant,
            // so can't guarantee that lower will be able to do this optimization.
            {
                // Do "a % b = a - (a / b) * b" morph always, see TODO before this block.
                bool doMorphModToSubMulDiv = true;

                if (doMorphModToSubMulDiv)
                {
                    assert(!optValnumCSE_phase);

                    tree = fgMorphModToSubMulDiv(tree->AsOp());
                    op1  = tree->AsOp()->gtOp1;
                    op2  = tree->AsOp()->gtOp2;
                }
            }
#else  // !TARGET_ARM64
            // If b is not a power of 2 constant then lowering replaces a % b
            // with a - (a / b) * b and applies magic division optimization to
            // a / b. The code may already contain an a / b expression (e.g.
            // x = a / 10; y = a % 10;) and then we end up with redundant code.
            // If we convert % to / here we give CSE the opportunity to eliminate
            // the redundant division. If there's no redundant division then
            // nothing is lost, lowering would have done this transform anyway.

            if (!optValnumCSE_phase && ((tree->OperGet() == GT_MOD) && op2->IsIntegralConst()))
            {
                ssize_t divisorValue    = op2->AsIntCon()->IconValue();
                size_t  absDivisorValue = (divisorValue == SSIZE_T_MIN) ? static_cast<size_t>(divisorValue)
                                                                       : static_cast<size_t>(abs(divisorValue));

                if (!isPow2(absDivisorValue))
                {
                    tree = fgMorphModToSubMulDiv(tree->AsOp());
                    op1  = tree->AsOp()->gtOp1;
                    op2  = tree->AsOp()->gtOp2;
                }
            }
#endif // !TARGET_ARM64
            break;

        USE_HELPER_FOR_ARITH:
        {
            // TODO: this comment is wrong now, do an appropriate fix.
            /* We have to morph these arithmetic operations into helper calls
               before morphing the arguments (preorder), else the arguments
               won't get correct values of fgPtrArgCntCur.
               However, try to fold the tree first in case we end up with a
               simple node which won't need a helper call at all */

            noway_assert(tree->OperIsBinary());

            GenTree* oldTree = tree;

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
            return fgMorphIntoHelperCall(tree, helper, gtNewCallArgs(op1, op2));

        case GT_RETURN:
            // normalize small integer return values
            if (fgGlobalMorph && varTypeIsSmall(info.compRetType) && (op1 != nullptr) && (op1->TypeGet() != TYP_VOID) &&
                fgCastNeeded(op1, info.compRetType))
            {
                // Small-typed return values are normalized by the callee
                op1 = gtNewCastNode(TYP_INT, op1, false, info.compRetType);

                // Propagate GTF_COLON_COND
                op1->gtFlags |= (tree->gtFlags & GTF_COLON_COND);

                tree->AsOp()->gtOp1 = fgMorphCast(op1);

                // Propagate side effect flags
                tree->gtFlags &= ~GTF_ALL_EFFECT;
                tree->gtFlags |= (tree->AsOp()->gtOp1->gtFlags & GTF_ALL_EFFECT);

                return tree;
            }
            if (!compDoOldStructRetyping() && !tree->TypeIs(TYP_VOID))
            {
                if (op1->OperIs(GT_OBJ, GT_BLK, GT_IND))
                {
                    op1 = fgMorphRetInd(tree->AsUnOp());
                }
                if (op1->OperIs(GT_LCL_VAR))
                {
                    // With a `genReturnBB` this `RETURN(src)` tree will be replaced by a `ASG(genReturnLocal, src)`
                    // and `ASG` will be tranformed into field by field copy without parent local referencing if
                    // possible.
                    GenTreeLclVar* lclVar = op1->AsLclVar();
                    unsigned       lclNum = lclVar->GetLclNum();
                    if ((genReturnLocal == BAD_VAR_NUM) || (genReturnLocal == lclNum))
                    {
                        LclVarDsc* varDsc = lvaGetDesc(lclVar);
                        if (varDsc->CanBeReplacedWithItsField(this))
                        {
                            // We can replace the struct with its only field and allow copy propogation to replace
                            // return value that was written as a field.
                            unsigned   fieldLclNum = varDsc->lvFieldLclStart;
                            LclVarDsc* fieldDsc    = lvaGetDesc(fieldLclNum);

                            if (!varTypeIsSmallInt(fieldDsc->lvType))
                            {
                                // TODO-CQ: support that substitution for small types without creating `CAST` node.
                                // When a small struct is returned in a register higher bits could be left in undefined
                                // state.
                                JITDUMP("Replacing an independently promoted local var V%02u with its only field  "
                                        "V%02u for "
                                        "the return [%06u]\n",
                                        lclVar->GetLclNum(), fieldLclNum, dspTreeID(tree));
                                lclVar->SetLclNum(fieldLclNum);
                                var_types fieldType = fieldDsc->lvType;
                                lclVar->ChangeType(fieldDsc->lvType);
                            }
                        }
                    }
                }
            }
            break;

        case GT_EQ:
        case GT_NE:
        {
            GenTree* optimizedTree = gtFoldTypeCompare(tree);

            if (optimizedTree != tree)
            {
                return fgMorphTree(optimizedTree);
            }
        }

            __fallthrough;

        case GT_GT:
        {
            // Try and optimize nullable boxes feeding compares
            GenTree* optimizedTree = gtFoldBoxNullable(tree);

            if (optimizedTree->OperGet() != tree->OperGet())
            {
                return optimizedTree;
            }
            else
            {
                tree = optimizedTree;
            }

            op1 = tree->AsOp()->gtOp1;
            op2 = tree->gtGetOp2IfPresent();

            break;
        }

        case GT_RUNTIMELOOKUP:
            return fgMorphTree(op1);

#ifdef TARGET_ARM
        case GT_INTRINSIC:
            if (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Math_Round)
            {
                switch (tree->TypeGet())
                {
                    case TYP_DOUBLE:
                        return fgMorphIntoHelperCall(tree, CORINFO_HELP_DBLROUND, gtNewCallArgs(op1));
                    case TYP_FLOAT:
                        return fgMorphIntoHelperCall(tree, CORINFO_HELP_FLTROUND, gtNewCallArgs(op1));
                    default:
                        unreached();
                }
            }
            break;
#endif
        case GT_LIST:
            // Special handling for the arg list.
            return fgMorphArgList(tree->AsArgList(), mac);

        default:
            break;
    }

#if !CPU_HAS_FP_SUPPORT
    tree = fgMorphToEmulatedFP(tree);
#endif

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
        if (subMac1 == nullptr || subMac1->m_kind == MACK_Ind)
        {
            switch (tree->gtOper)
            {
                case GT_ADDR:
                    // A non-null mac here implies this node is part of an address computation.
                    // If so, we need to pass the existing mac down to the child node.
                    //
                    // Otherwise, use a new mac.
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
                case GT_OBJ:
                case GT_BLK:
                case GT_DYN_BLK:
                case GT_IND:
                    // A non-null mac here implies this node is part of an address computation (the tree parent is
                    // GT_ADDR).
                    // If so, we need to pass the existing mac down to the child node.
                    //
                    // Otherwise, use a new mac.
                    if (subMac1 == nullptr)
                    {
                        subMac1 = &subIndMac1;
                    }
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
            GenTree* otherOp = tree->AsOp()->gtOp2;
            // Is the other operator a constant?
            if (otherOp->IsCnsIntOrI())
            {
                ClrSafeInt<size_t> totalOffset(subMac1->m_totalOffset);
                totalOffset += otherOp->AsIntConCommon()->IconValue();
                if (totalOffset.IsOverflow())
                {
                    // We will consider an offset so large as to overflow as "not a constant" --
                    // we will do a null check.
                    subMac1->m_allConstantOffsets = false;
                }
                else
                {
                    subMac1->m_totalOffset += otherOp->AsIntConCommon()->IconValue();
                }
            }
            else
            {
                subMac1->m_allConstantOffsets = false;
            }
        }

        // If op1 is a GT_FIELD or indir, we need to pass down the mac if
        // its parent is GT_ADDR, since the address of op1
        // is part of an ongoing address computation. Otherwise
        // op1 represents the value of the field and so any address
        // calculations it does are in a new context.
        if (((op1->gtOper == GT_FIELD) || op1->OperIsIndir()) && (tree->gtOper != GT_ADDR))
        {
            subMac1 = nullptr;

            // The impact of op1's value to any ongoing
            // address computation is handled below when looking
            // at op2.
        }

        tree->AsOp()->gtOp1 = op1 = fgMorphTree(op1, subMac1);

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

        assert(tree->gtOper != GT_CALL);

        if (!tree->OperRequiresCallFlag(this))
        {
            tree->gtFlags &= ~GTF_CALL;
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
        switch (tree->gtOper)
        {
            case GT_ADD:
                if (mac != nullptr && mac->m_kind == MACK_Ind)
                {
                    GenTree* otherOp = tree->AsOp()->gtOp1;
                    // Is the other operator a constant?
                    if (otherOp->IsCnsIntOrI())
                    {
                        mac->m_totalOffset += otherOp->AsIntConCommon()->IconValue();
                    }
                    else
                    {
                        mac->m_allConstantOffsets = false;
                    }
                }
                break;
            default:
                break;
        }

        // If op2 is a GT_FIELD or indir, we must be taking its value,
        // so it should evaluate its address in a new context.
        if ((op2->gtOper == GT_FIELD) || op2->OperIsIndir())
        {
            // The impact of op2's value to any ongoing
            // address computation is handled above when looking
            // at op1.
            mac = nullptr;
        }

        tree->AsOp()->gtOp2 = op2 = fgMorphTree(op2, mac);

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
#endif // LOCAL_ASSERTION_PROP
    }  // if (op2)

DONE_MORPHING_CHILDREN:

    if (tree->OperIsIndirOrArrLength())
    {
        tree->SetIndirExceptionFlags(this);
    }
    else
    {
        if (tree->OperMayThrow(this))
        {
            // Mark the tree node as potentially throwing an exception
            tree->gtFlags |= GTF_EXCEPT;
        }
        else
        {
            if (((op1 == nullptr) || ((op1->gtFlags & GTF_EXCEPT) == 0)) &&
                ((op2 == nullptr) || ((op2->gtFlags & GTF_EXCEPT) == 0)))
            {
                tree->gtFlags &= ~GTF_EXCEPT;
            }
        }
    }

    if (tree->OperRequiresAsgFlag())
    {
        tree->gtFlags |= GTF_ASG;
    }
    else
    {
        if (((op1 == nullptr) || ((op1->gtFlags & GTF_ASG) == 0)) &&
            ((op2 == nullptr) || ((op2->gtFlags & GTF_ASG) == 0)))
        {
            tree->gtFlags &= ~GTF_ASG;
        }
    }

    if (tree->OperRequiresCallFlag(this))
    {
        tree->gtFlags |= GTF_CALL;
    }
    else
    {
        if (((op1 == nullptr) || ((op1->gtFlags & GTF_CALL) == 0)) &&
            ((op2 == nullptr) || ((op2->gtFlags & GTF_CALL) == 0)))
        {
            tree->gtFlags &= ~GTF_CALL;
        }
    }
    /*-------------------------------------------------------------------------
     * Now do POST-ORDER processing
     */

    if (varTypeIsGC(tree->TypeGet()) && (op1 && !varTypeIsGC(op1->TypeGet())) && (op2 && !varTypeIsGC(op2->TypeGet())))
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

    GenTree* oldTree = tree;

    GenTree* qmarkOp1 = nullptr;
    GenTree* qmarkOp2 = nullptr;

    if ((tree->OperGet() == GT_QMARK) && (tree->AsOp()->gtOp2->OperGet() == GT_COLON))
    {
        qmarkOp1 = oldTree->AsOp()->gtOp2->AsOp()->gtOp1;
        qmarkOp2 = oldTree->AsOp()->gtOp2->AsOp()->gtOp2;
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
            tree->AsOp()->gtOp1 = fgMorphTree(tree->AsOp()->gtOp1);
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
    op1 = tree->AsOp()->gtOp1;
    op2 = tree->gtGetOp2IfPresent();

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

    GenTree*      temp;
    GenTree*      cns1;
    GenTree*      cns2;
    size_t        ival1, ival2;
    GenTree*      lclVarTree;
    GenTree*      effectiveOp1;
    FieldSeqNode* fieldSeq = nullptr;

    switch (oper)
    {
        case GT_ASG:

            lclVarTree = fgIsIndirOfAddrOfLocal(op1);
            if (lclVarTree != nullptr)
            {
                lclVarTree->gtFlags |= GTF_VAR_DEF;
            }

            effectiveOp1 = op1->gtEffectiveVal();

            if (effectiveOp1->OperIsConst())
            {
                op1                 = gtNewOperNode(GT_IND, tree->TypeGet(), op1);
                tree->AsOp()->gtOp1 = op1;
            }

            /* If we are storing a small type, we might be able to omit a cast */
            if ((effectiveOp1->gtOper == GT_IND) && varTypeIsSmall(effectiveOp1->TypeGet()))
            {
                if (!gtIsActiveCSE_Candidate(op2) && (op2->gtOper == GT_CAST) && !op2->gtOverflow())
                {
                    var_types castType = op2->CastToType();

                    // If we are performing a narrowing cast and
                    // castType is larger or the same as op1's type
                    // then we can discard the cast.

                    if (varTypeIsSmall(castType) && (genTypeSize(castType) >= genTypeSize(effectiveOp1->TypeGet())))
                    {
                        tree->AsOp()->gtOp2 = op2 = op2->AsCast()->CastOp();
                    }
                }
                else if (op2->OperIsCompare() && varTypeIsByte(effectiveOp1->TypeGet()))
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
                unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
                LclVarDsc* varDsc = &lvaTable[varNum];

                /* We again need to zero extend the setcc instruction */
                op2->gtType = varDsc->TypeGet();
            }
            fgAssignSetVarDef(tree);

            /* We can't CSE the LHS of an assignment */
            /* We also must set in the pre-morphing phase, otherwise assertionProp doesn't see it */
            if (op1->IsLocal() || (op1->TypeGet() != TYP_STRUCT))
            {
                op1->gtFlags |= GTF_DONT_CSE;
            }
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

            if (cns2->gtOper == GT_CNS_INT && cns2->AsIntCon()->gtIconVal != 0)
            {
                op1 = tree->AsOp()->gtOp1;

                /* Since this can occur repeatedly we use a while loop */

                while ((op1->gtOper == GT_ADD || op1->gtOper == GT_SUB) && (op1->AsOp()->gtOp2->gtOper == GT_CNS_INT) &&
                       (op1->gtType == TYP_INT) && (op1->gtOverflow() == false))
                {
                    /* Got it; change "x+icon1==icon2" to "x==icon2-icon1" */

                    ival1 = op1->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                    ival2 = cns2->AsIntCon()->gtIconVal;

                    if (op1->gtOper == GT_ADD)
                    {
                        ival2 -= ival1;
                    }
                    else
                    {
                        ival2 += ival1;
                    }
                    cns2->AsIntCon()->gtIconVal = ival2;

#ifdef TARGET_64BIT
                    // we need to properly re-sign-extend or truncate as needed.
                    cns2->AsIntCon()->TruncateOrSignExtend32();
#endif // TARGET_64BIT

                    op1 = tree->AsOp()->gtOp1 = op1->AsOp()->gtOp1;
                }
            }

            //
            // Here we look for the following tree
            //
            //                        EQ/NE
            //                        /  \.
            //                      op1   CNS 0/1
            //
            ival2 = INT_MAX; // The value of INT_MAX for ival2 just means that the constant value is not 0 or 1

            // cast to unsigned allows test for both 0 and 1
            if ((cns2->gtOper == GT_CNS_INT) && (((size_t)cns2->AsIntConCommon()->IconValue()) <= 1U))
            {
                ival2 = (size_t)cns2->AsIntConCommon()->IconValue();
            }
            else // cast to UINT64 allows test for both 0 and 1
                if ((cns2->gtOper == GT_CNS_LNG) && (((UINT64)cns2->AsIntConCommon()->LngValue()) <= 1ULL))
            {
                ival2 = (size_t)cns2->AsIntConCommon()->LngValue();
            }

            if (ival2 != INT_MAX)
            {
                // If we don't have a comma and relop, we can't do this optimization
                //
                if ((op1->gtOper == GT_COMMA) && (op1->AsOp()->gtOp2->OperIsCompare()))
                {
                    // Here we look for the following transformation
                    //
                    //                  EQ/NE                    Possible REVERSE(RELOP)
                    //                  /  \                           /      \.
                    //               COMMA CNS 0/1             ->   COMMA   relop_op2
                    //              /   \                          /    \.
                    //             x  RELOP                       x     relop_op1
                    //               /    \.
                    //         relop_op1  relop_op2
                    //
                    //
                    //
                    GenTree* comma = op1;
                    GenTree* relop = comma->AsOp()->gtOp2;

                    GenTree* relop_op1 = relop->AsOp()->gtOp1;

                    bool reverse = ((ival2 == 0) == (oper == GT_EQ));

                    if (reverse)
                    {
                        gtReverseCond(relop);
                    }

                    relop->AsOp()->gtOp1 = comma;
                    comma->AsOp()->gtOp2 = relop_op1;

                    // Comma now has fewer nodes underneath it, so we need to regenerate its flags
                    comma->gtFlags &= ~GTF_ALL_EFFECT;
                    comma->gtFlags |= (comma->AsOp()->gtOp1->gtFlags) & GTF_ALL_EFFECT;
                    comma->gtFlags |= (comma->AsOp()->gtOp2->gtFlags) & GTF_ALL_EFFECT;

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
                    //                        /  \                   /  \.
                    //                     COMMA  CNS 0/1  ->     RELOP CNS 0/1
                    //                     /   \                   / \.
                    //                   ASG  LCL_VAR
                    //                  /  \.
                    //           LCL_VAR   RELOP
                    //                      / \.
                    //

                    GenTree* asg = op1->AsOp()->gtOp1;
                    GenTree* lcl = op1->AsOp()->gtOp2;

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

                    unsigned lclNum = lcl->AsLclVarCommon()->GetLclNum();
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
                    if (asg->AsOp()->gtOp1->gtOper != GT_LCL_VAR)
                    {
                        goto SKIP;
                    }

                    /* Both of the LCL_VAR must match */
                    if (asg->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() != lclNum)
                    {
                        goto SKIP;
                    }

                    /* If right side of asg is not a RELOP then skip */
                    if (!asg->AsOp()->gtOp2->OperIsCompare())
                    {
                        goto SKIP;
                    }

                    LclVarDsc* varDsc = lvaTable + lclNum;

                    /* Set op1 to the right side of asg, (i.e. the RELOP) */
                    op1 = asg->AsOp()->gtOp2;

                    DEBUG_DESTROY_NODE(asg->AsOp()->gtOp1);
                    DEBUG_DESTROY_NODE(lcl);
                }

                if (op1->OperIsCompare())
                {
                    // Here we look for the following tree
                    //
                    //                        EQ/NE           ->      RELOP/!RELOP
                    //                        /  \                       /    \.
                    //                     RELOP  CNS 0/1
                    //                     /   \.
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
                //                        /  \                   /  \.
                //                      AND   CNS 0/1  ->      AND   CNS 0
                //                     /   \                  /   \.
                //                RSZ/RSH   CNS 1            x     CNS (1 << y)
                //                  /  \.
                //                 x   CNS_INT +y

                if (op1->gtOper == GT_AND)
                {
                    GenTree* andOp    = op1;
                    GenTree* rshiftOp = andOp->AsOp()->gtOp1;

                    if ((rshiftOp->gtOper != GT_RSZ) && (rshiftOp->gtOper != GT_RSH))
                    {
                        goto SKIP;
                    }

                    if (!rshiftOp->AsOp()->gtOp2->IsCnsIntOrI())
                    {
                        goto SKIP;
                    }

                    ssize_t shiftAmount = rshiftOp->AsOp()->gtOp2->AsIntCon()->gtIconVal;

                    if (shiftAmount < 0)
                    {
                        goto SKIP;
                    }

                    if (!andOp->AsOp()->gtOp2->IsIntegralConst(1))
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

                        andOp->AsOp()->gtOp2->AsIntCon()->gtIconVal = newAndOperand;

                        // Reverse the cond if necessary
                        if (ival2 == 1)
                        {
                            gtReverseCond(tree);
                            cns2->AsIntCon()->gtIconVal = 0;
                            oper                        = tree->gtOper;
                        }
                    }
                    else if (andOp->gtType == TYP_LONG)
                    {
                        if (shiftAmount > 63)
                        {
                            goto SKIP;
                        }

                        UINT64 newAndOperand = ((UINT64)1) << shiftAmount;

                        andOp->AsOp()->gtOp2->AsIntConCommon()->SetLngValue(newAndOperand);

                        // Reverse the cond if necessary
                        if (ival2 == 1)
                        {
                            gtReverseCond(tree);
                            cns2->AsIntConCommon()->SetLngValue(0);
                            oper = tree->gtOper;
                        }
                    }

                    andOp->AsOp()->gtOp1 = rshiftOp->AsOp()->gtOp1;

                    DEBUG_DESTROY_NODE(rshiftOp->AsOp()->gtOp2);
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

            if ((cns2->AsIntConCommon()->LngValue() >> 31) != 0)
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

                    tree->AsOp()->gtOp1 = op1->AsCast()->CastOp();
                    tree->AsOp()->gtOp2 = gtNewIconNode((int)cns2->AsIntConCommon()->LngValue(), TYP_INT);
                }

                goto COMPARE;
            }

            noway_assert(op1->TypeGet() == TYP_LONG && op1->OperGet() == GT_AND);

            /* Is the result of the mask effectively an INT ? */

            GenTree* andMask;
            andMask = op1->AsOp()->gtOp2;
            if (andMask->gtOper != GT_CNS_NATIVELONG)
            {
                goto COMPARE;
            }
            if ((andMask->AsIntConCommon()->LngValue() >> 32) != 0)
            {
                goto COMPARE;
            }

            /* Now we know that we can cast AsOp()->gtOp1 of AND to int */

            op1->AsOp()->gtOp1 = gtNewCastNode(TYP_INT, op1->AsOp()->gtOp1, false, TYP_INT);

            /* now replace the mask node (AsOp()->gtOp2 of AND node) */

            noway_assert(andMask == op1->AsOp()->gtOp2);

            ival1 = (int)andMask->AsIntConCommon()->LngValue();
            andMask->SetOper(GT_CNS_INT);
            andMask->gtType                = TYP_INT;
            andMask->AsIntCon()->gtIconVal = ival1;

            /* now change the type of the AND node */

            op1->gtType = TYP_INT;

            /* finally we replace the comparand */

            ival2 = (int)cns2->AsIntConCommon()->LngValue();
            cns2->SetOper(GT_CNS_INT);
            cns2->gtType = TYP_INT;

            noway_assert(cns2 == op2);
            cns2->AsIntCon()->gtIconVal = ival2;

            goto COMPARE;

        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:

            if (op2->gtOper == GT_CNS_INT)
            {
                cns2 = op2;
                /* Check for "expr relop 1" */
                if (cns2->IsIntegralConst(1))
                {
                    /* Check for "expr >= 1" */
                    if (oper == GT_GE)
                    {
                        /* Change to "expr != 0" for unsigned and "expr > 0" for signed */
                        oper = (tree->IsUnsigned()) ? GT_NE : GT_GT;
                        goto SET_OPER;
                    }
                    /* Check for "expr < 1" */
                    else if (oper == GT_LT)
                    {
                        /* Change to "expr == 0" for unsigned and "expr <= 0" for signed */
                        oper = (tree->IsUnsigned()) ? GT_EQ : GT_LE;
                        goto SET_OPER;
                    }
                }
                /* Check for "expr relop -1" */
                else if (!tree->IsUnsigned() && cns2->IsIntegralConst(-1))
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
                        cns2->AsIntCon()->gtIconVal = 0;

                        // vnStore is null before the ValueNumber phase has run
                        if (vnStore != nullptr)
                        {
                            // Update the ValueNumber for 'cns2', as we just changed it to 0
                            fgValueNumberTreeConst(cns2);
                        }
                        op2 = tree->AsOp()->gtOp2 = gtFoldExpr(op2);
                    }
                }
                else if (tree->IsUnsigned() && op2->IsIntegralConst(0))
                {
                    if ((oper == GT_GT) || (oper == GT_LE))
                    {
                        // IL doesn't have a cne instruction so compilers use cgt.un instead. The JIT
                        // recognizes certain patterns that involve GT_NE (e.g (x & 4) != 0) and fails
                        // if GT_GT is used instead. Transform (x GT_GT.unsigned 0) into (x GT_NE 0)
                        // and (x GT_LE.unsigned 0) into (x GT_EQ 0). The later case is rare, it sometimes
                        // occurs as a result of branch inversion.
                        oper = (oper == GT_LE) ? GT_EQ : GT_NE;
                        tree->SetOper(oper, GenTree::PRESERVE_VN);
                        tree->gtFlags &= ~GTF_UNSIGNED;
                    }
                }
            }

        COMPARE:

            noway_assert(tree->OperKind() & GTK_RELOP);
            break;

        case GT_MUL:

#ifndef TARGET_64BIT
            if (typ == TYP_LONG)
            {
                // This must be GTF_MUL_64RSLT
                assert(tree->gtIsValid64RsltMul());
                return tree;
            }
#endif // TARGET_64BIT
            goto CM_OVF_OP;

        case GT_SUB:

            if (tree->gtOverflow())
            {
                goto CM_OVF_OP;
            }

            // TODO #4104: there are a lot of other places where
            // this condition is not checked before transformations.
            if (fgGlobalMorph)
            {
                /* Check for "op1 - cns2" , we change it to "op1 + (-cns2)" */

                noway_assert(op2);
                if (op2->IsCnsIntOrI())
                {
                    /* Negate the constant and change the node to be "+" */

                    op2->AsIntConCommon()->SetIconValue(-op2->AsIntConCommon()->IconValue());
                    op2->AsIntConRef().gtFieldSeq = FieldSeqStore::NotAField();
                    oper                          = GT_ADD;
                    tree->ChangeOper(oper);
                    goto CM_ADD_OP;
                }

                /* Check for "cns1 - op2" , we change it to "(cns1 + (-op2))" */

                noway_assert(op1);
                if (op1->IsCnsIntOrI())
                {
                    noway_assert(varTypeIsIntOrI(tree));

                    // The type of the new GT_NEG node cannot just be op2->TypeGet().
                    // Otherwise we may sign-extend incorrectly in cases where the GT_NEG
                    // node ends up feeding directly into a cast, for example in
                    // GT_CAST<ubyte>(GT_SUB(0, s_1.ubyte))
                    tree->AsOp()->gtOp2 = op2 = gtNewOperNode(GT_NEG, genActualType(op2->TypeGet()), op2);
                    fgMorphTreeDone(op2);

                    oper = GT_ADD;
                    tree->ChangeOper(oper);
                    goto CM_ADD_OP;
                }

                /* No match - exit */
            }
            break;

#ifdef TARGET_ARM64
        case GT_DIV:
            if (!varTypeIsFloating(tree->gtType))
            {
                // Codegen for this instruction needs to be able to throw two exceptions:
                fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW);
                fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_DIV_BY_ZERO);
            }
            break;
        case GT_UDIV:
            // Codegen for this instruction needs to be able to throw one exception:
            fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_DIV_BY_ZERO);
            break;
#endif

        case GT_ADD:

        CM_OVF_OP:
            if (tree->gtOverflow())
            {
                tree->gtRequestSetFlags();

                // Add the excptn-throwing basic block to jump to on overflow

                fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_OVERFLOW);

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
                tree->AsOp()->gtOp1 = op2;
                tree->AsOp()->gtOp2 = op1;

                op1 = op2;
                op2 = tree->AsOp()->gtOp2;
            }

            // See if we can fold floating point operations (can regress minopts mode)
            if (opts.OptimizationEnabled() && varTypeIsFloating(tree->TypeGet()) && !optValnumCSE_phase)
            {
                if ((oper == GT_MUL) && !op1->IsCnsFltOrDbl() && op2->IsCnsFltOrDbl())
                {
                    if (op2->AsDblCon()->gtDconVal == 2.0)
                    {
                        bool needsComma = !op1->OperIsLeaf() && !op1->IsLocal();
                        // if op1 is not a leaf/local we have to introduce a temp via GT_COMMA.
                        // Unfortunately, it's not optHoistLoopCode-friendly yet so let's do it later.
                        if (!needsComma || (fgOrder == FGOrderLinear))
                        {
                            // Fold "x*2.0" to "x+x"
                            op2  = fgMakeMultiUse(&tree->AsOp()->gtOp1);
                            op1  = tree->AsOp()->gtOp1;
                            oper = GT_ADD;
                            tree = gtNewOperNode(oper, tree->TypeGet(), op1, op2);
                            INDEBUG(tree->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);
                        }
                    }
                    else if (op2->AsDblCon()->gtDconVal == 1.0)
                    {
                        // Fold "x*1.0" to "x"
                        DEBUG_DESTROY_NODE(op2);
                        DEBUG_DESTROY_NODE(tree);
                        return op1;
                    }
                }
            }

            /* See if we can fold GT_ADD nodes. */

            if (oper == GT_ADD)
            {
                /* Fold "((x+icon1)+(y+icon2)) to ((x+y)+(icon1+icon2))" */

                if (op1->gtOper == GT_ADD && op2->gtOper == GT_ADD && !gtIsActiveCSE_Candidate(op2) &&
                    op1->AsOp()->gtOp2->gtOper == GT_CNS_INT && op2->AsOp()->gtOp2->gtOper == GT_CNS_INT &&
                    !op1->gtOverflow() && !op2->gtOverflow())
                {
                    // Don't create a byref pointer that may point outside of the ref object.
                    // If a GC happens, the byref won't get updated. This can happen if one
                    // of the int components is negative. It also requires the address generation
                    // be in a fully-interruptible code region.
                    if (!varTypeIsGC(op1->AsOp()->gtOp1->TypeGet()) && !varTypeIsGC(op2->AsOp()->gtOp1->TypeGet()))
                    {
                        cns1 = op1->AsOp()->gtOp2;
                        cns2 = op2->AsOp()->gtOp2;
                        cns1->AsIntCon()->gtIconVal += cns2->AsIntCon()->gtIconVal;
#ifdef TARGET_64BIT
                        if (cns1->TypeGet() == TYP_INT)
                        {
                            // we need to properly re-sign-extend or truncate after adding two int constants above
                            cns1->AsIntCon()->TruncateOrSignExtend32();
                        }
#endif // TARGET_64BIT

                        tree->AsOp()->gtOp2 = cns1;
                        DEBUG_DESTROY_NODE(cns2);

                        op1->AsOp()->gtOp2 = op2->AsOp()->gtOp1;
                        op1->gtFlags |= (op1->AsOp()->gtOp2->gtFlags & GTF_ALL_EFFECT);
                        DEBUG_DESTROY_NODE(op2);
                        op2 = tree->AsOp()->gtOp2;
                    }
                }

                if (op2->IsCnsIntOrI() && varTypeIsIntegralOrI(typ))
                {
                    // Fold "((x+icon1)+icon2) to (x+(icon1+icon2))"
                    // Fold "((comma(y, x+icon1)+icon2) to comma(y, x+(icon1+icon2))"
                    CLANG_FORMAT_COMMENT_ANCHOR;

                    const bool commasOnly        = true;
                    GenTree*   op1EffectiveValue = op1->gtEffectiveVal(commasOnly);

                    if (op1EffectiveValue->gtOper == GT_ADD && !gtIsActiveCSE_Candidate(op1EffectiveValue) &&
                        !op1EffectiveValue->gtOverflow() && op1EffectiveValue->AsOp()->gtOp2->IsCnsIntOrI() &&
                        (op1EffectiveValue->AsOp()->gtOp2->OperGet() == op2->OperGet()) &&
                        (op1EffectiveValue->AsOp()->gtOp2->TypeGet() != TYP_REF) && (op2->TypeGet() != TYP_REF))
                    {
                        cns1 = op1EffectiveValue->AsOp()->gtOp2;

                        cns1->AsIntConCommon()->SetIconValue(cns1->AsIntConCommon()->IconValue() +
                                                             op2->AsIntConCommon()->IconValue());
#ifdef TARGET_64BIT
                        if (cns1->TypeGet() == TYP_INT)
                        {
                            // we need to properly re-sign-extend or truncate after adding two int constants above
                            cns1->AsIntCon()->TruncateOrSignExtend32();
                        }
#endif // TARGET_64BIT

                        if (cns1->OperGet() == GT_CNS_INT)
                        {
                            cns1->AsIntCon()->gtFieldSeq =
                                GetFieldSeqStore()->Append(cns1->AsIntCon()->gtFieldSeq, op2->AsIntCon()->gtFieldSeq);
                        }
                        DEBUG_DESTROY_NODE(op2);

                        GenTree* oldTree = tree;
                        tree             = tree->AsOp()->gtOp1;
                        op1              = tree->AsOp()->gtOp1;
                        op2              = tree->AsOp()->gtOp2;
                        DEBUG_DESTROY_NODE(oldTree);

                        if (tree->OperGet() != GT_ADD)
                        {
                            return tree;
                        }
                    }

                    // Fold (x + 0).

                    if ((op2->AsIntConCommon()->IconValue() == 0) && !gtIsActiveCSE_Candidate(tree))
                    {

                        // If this addition is adding an offset to a null pointer,
                        // avoid the work and yield the null pointer immediately.
                        // Dereferencing the pointer in either case will have the
                        // same effect.

                        if (!optValnumCSE_phase && varTypeIsGC(op2->TypeGet()) &&
                            ((op1->gtFlags & GTF_ALL_EFFECT) == 0))
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
                                (op2->AsIntCon()->gtFieldSeq != nullptr) &&
                                (op2->AsIntCon()->gtFieldSeq != FieldSeqStore::NotAField()))
                            {
                                fgAddFieldSeqForZeroOffset(op1, op2->AsIntCon()->gtFieldSeq);
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
#ifndef TARGET_64BIT
                noway_assert(typ <= TYP_UINT);
#endif // TARGET_64BIT
                noway_assert(!tree->gtOverflow());

                ssize_t mult            = op2->AsIntConCommon()->IconValue();
                bool    op2IsConstIndex = op2->OperGet() == GT_CNS_INT && op2->AsIntCon()->gtFieldSeq != nullptr &&
                                       op2->AsIntCon()->gtFieldSeq->IsConstantIndexFieldSeq();

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
                        // The type of the new GT_NEG node cannot just be op1->TypeGet().
                        // Otherwise we may sign-extend incorrectly in cases where the GT_NEG
                        // node ends up feeding directly a cast, for example in
                        // GT_CAST<ubyte>(GT_MUL(-1, s_1.ubyte))
                        tree->AsOp()->gtOp1 = op1 = gtNewOperNode(GT_NEG, genActualType(op1->TypeGet()), op1);
                        fgMorphTreeDone(op1);
                    }

                    // If "op2" is a constant array index, the other multiplicand must be a constant.
                    // Transfer the annotation to the other one.
                    if (op2->OperGet() == GT_CNS_INT && op2->AsIntCon()->gtFieldSeq != nullptr &&
                        op2->AsIntCon()->gtFieldSeq->IsConstantIndexFieldSeq())
                    {
                        assert(op2->AsIntCon()->gtFieldSeq->m_next == nullptr);
                        GenTree* otherOp = op1;
                        if (otherOp->OperGet() == GT_NEG)
                        {
                            otherOp = otherOp->AsOp()->gtOp1;
                        }
                        assert(otherOp->OperGet() == GT_CNS_INT);
                        assert(otherOp->AsIntCon()->gtFieldSeq == FieldSeqStore::NotAField());
                        otherOp->AsIntCon()->gtFieldSeq = op2->AsIntCon()->gtFieldSeq;
                    }

                    if (abs_mult == 1)
                    {
                        DEBUG_DESTROY_NODE(op2);
                        DEBUG_DESTROY_NODE(tree);
                        return op1;
                    }

                    /* Change the multiplication into a shift by log2(val) bits */
                    op2->AsIntConCommon()->SetIconValue(genLog2(abs_mult));
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
                            tree->AsOp()->gtOp1 = op1 = gtNewOperNode(GT_NEG, genActualType(op1->TypeGet()), op1);
                            fgMorphTreeDone(op1);
                        }

                        GenTree* factorIcon = gtNewIconNode(factor, TYP_I_IMPL);
                        if (op2IsConstIndex)
                        {
                            factorIcon->AsIntCon()->gtFieldSeq =
                                GetFieldSeqStore()->CreateSingleton(FieldSeqStore::ConstantIndexPseudoField);
                        }

                        // change the multiplication into a smaller multiplication (by 3, 5 or 9) and a shift
                        tree->AsOp()->gtOp1 = op1 = gtNewOperNode(GT_MUL, tree->gtType, op1, factorIcon);
                        fgMorphTreeDone(op1);

                        op2->AsIntConCommon()->SetIconValue(shift);
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
                op1  = tree->AsOp()->gtOp1;
                op2  = tree->AsOp()->gtOp2;
            }

            break;

        case GT_NOT:
        case GT_NEG:

            /* Any constant cases should have been folded earlier */
            noway_assert(!op1->OperIsConst() || !opts.OptEnabled(CLFLG_CONSTANTFOLD) || optValnumCSE_phase);
            break;

        case GT_CKFINITE:

            noway_assert(varTypeIsFloating(op1->TypeGet()));

            fgAddCodeRef(compCurBB, bbThrowIndex(compCurBB), SCK_ARITH_EXCPN);
            break;

        case GT_OBJ:
            // If we have GT_OBJ(GT_ADDR(X)) and X has GTF_GLOB_REF, we must set GTF_GLOB_REF on
            // the GT_OBJ. Note that the GTF_GLOB_REF will have been cleared on ADDR(X) where X
            // is a local or clsVar, even if it has been address-exposed.
            if (op1->OperGet() == GT_ADDR)
            {
                GenTreeUnOp* addr   = op1->AsUnOp();
                GenTree*     addrOp = addr->gtGetOp1();
                tree->gtFlags |= (addrOp->gtFlags & GTF_GLOB_REF);
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

            // Don't remove a volatile GT_IND, even if the address points to a local variable.
            if ((tree->gtFlags & GTF_IND_VOLATILE) == 0)
            {
                /* Try to Fold *(&X) into X */
                if (op1->gtOper == GT_ADDR)
                {
                    // Can not remove a GT_ADDR if it is currently a CSE candidate.
                    if (gtIsActiveCSE_Candidate(op1))
                    {
                        break;
                    }

                    temp = op1->AsOp()->gtOp1; // X

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
                        unsigned   lclNum = temp->AsLclVarCommon()->GetLclNum();
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
                                if (fieldVarDsc->TypeGet() == typ && (fieldVarDsc->lvFldOffset == 0))
                                {
                                    // We can just use the existing promoted field LclNum
                                    temp->AsLclVarCommon()->SetLclNum(lclNumFld);
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
                            tree->gtType = typ = temp->TypeGet();
                            foldAndReturnTemp  = true;
                        }
                        else if (!varTypeIsStruct(typ) && (lvaTable[lclNum].lvType == typ) &&
                                 !lvaTable[lclNum].lvNormalizeOnLoad())
                        {
                            tree->gtType = typ = temp->TypeGet();
                            foldAndReturnTemp  = true;
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
                                temp->AsLclFld()->SetFieldSeq(
                                    GetFieldSeqStore()->Append(temp->AsLclFld()->GetFieldSeq(), fieldSeq));
                                temp->gtType = typ;

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
#ifdef TARGET_ARM
                    // Check for a misalignment floating point indirection.
                    if (varTypeIsFloating(typ))
                    {
                        GenTree* addOp2 = op1->AsOp()->gtGetOp2();
                        if (addOp2->IsCnsIntOrI())
                        {
                            ssize_t offset = addOp2->AsIntCon()->gtIconVal;
                            if ((offset % emitTypeSize(TYP_FLOAT)) != 0)
                            {
                                tree->gtFlags |= GTF_IND_UNALIGNED;
                            }
                        }
                    }
#endif // TARGET_ARM

                    /* Try to change *(&lcl + cns) into lcl[cns] to prevent materialization of &lcl */

                    if (op1->AsOp()->gtOp1->OperGet() == GT_ADDR && op1->AsOp()->gtOp2->OperGet() == GT_CNS_INT &&
                        opts.OptimizationEnabled())
                    {
                        // No overflow arithmetic with pointers
                        noway_assert(!op1->gtOverflow());

                        temp = op1->AsOp()->gtOp1->AsOp()->gtOp1;
                        if (!temp->OperIsLocal())
                        {
                            temp = nullptr;
                            break;
                        }

                        // Can not remove the GT_ADDR if it is currently a CSE candidate.
                        if (gtIsActiveCSE_Candidate(op1->AsOp()->gtOp1))
                        {
                            break;
                        }

                        ival1    = op1->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                        fieldSeq = op1->AsOp()->gtOp2->AsIntCon()->gtFieldSeq;

                        // Does the address have an associated zero-offset field sequence?
                        FieldSeqNode* addrFieldSeq = nullptr;
                        if (GetZeroOffsetFieldMap()->Lookup(op1->AsOp()->gtOp1, &addrFieldSeq))
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
                            if (ival1 >= Compiler::lvaLclSize(temp->AsLclVarCommon()->GetLclNum()))
                            {
                                break;
                            }
                        }
                        // Now we can fold this into a GT_LCL_FLD below
                        //   where we check (temp != nullptr)
                    }
                }
            }

            // At this point we may have a lclVar or lclFld that might be foldable with a bit of extra massaging:
            // - We may have a load of a local where the load has a different type than the local
            // - We may have a load of a local plus an offset
            //
            // In these cases, we will change the lclVar or lclFld into a lclFld of the appropriate type and
            // offset if doing so is legal. The only cases in which this transformation is illegal are if the load
            // begins before the local or if the load extends beyond the end of the local (i.e. if the load is
            // out-of-bounds w.r.t. the local).
            if ((temp != nullptr) && !foldAndReturnTemp)
            {
                assert(temp->OperIsLocal());

                const unsigned   lclNum = temp->AsLclVarCommon()->GetLclNum();
                LclVarDsc* const varDsc = &lvaTable[lclNum];

                const var_types tempTyp = temp->TypeGet();
                const bool useExactSize = varTypeIsStruct(tempTyp) || (tempTyp == TYP_BLK) || (tempTyp == TYP_LCLBLK);
                const unsigned varSize  = useExactSize ? varDsc->lvExactSize : genTypeSize(temp);

                // Make sure we do not enregister this lclVar.
                lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LocalField));

                // If the size of the load is greater than the size of the lclVar, we cannot fold this access into
                // a lclFld: the access represented by an lclFld node must begin at or after the start of the
                // lclVar and must not extend beyond the end of the lclVar.
                if ((ival1 >= 0) && ((ival1 + genTypeSize(typ)) <= varSize))
                {
                    GenTreeLclFld* lclFld;

                    // We will turn a GT_LCL_VAR into a GT_LCL_FLD with an gtLclOffs of 'ival'
                    // or if we already have a GT_LCL_FLD we will adjust the gtLclOffs by adding 'ival'
                    // Then we change the type of the GT_LCL_FLD to match the orginal GT_IND type.
                    //
                    if (temp->OperGet() == GT_LCL_FLD)
                    {
                        lclFld = temp->AsLclFld();
                        lclFld->SetLclOffs(lclFld->GetLclOffs() + static_cast<unsigned>(ival1));
                        lclFld->SetFieldSeq(GetFieldSeqStore()->Append(lclFld->GetFieldSeq(), fieldSeq));
                    }
                    else // we have a GT_LCL_VAR
                    {
                        assert(temp->OperGet() == GT_LCL_VAR);
                        temp->ChangeOper(GT_LCL_FLD); // Note that this typically makes the gtFieldSeq "NotAField",
                        // unless there is a zero filed offset associated with 'temp'.
                        lclFld = temp->AsLclFld();
                        lclFld->SetLclOffs(static_cast<unsigned>(ival1));

                        if (lclFld->GetFieldSeq() == FieldSeqStore::NotAField())
                        {
                            if (fieldSeq != nullptr)
                            {
                                // If it does represent a field, note that.
                                lclFld->SetFieldSeq(fieldSeq);
                            }
                        }
                        else
                        {
                            // Append 'fieldSeq' to the existing one
                            lclFld->SetFieldSeq(GetFieldSeqStore()->Append(lclFld->GetFieldSeq(), fieldSeq));
                        }
                    }
                    temp->gtType      = tree->gtType;
                    foldAndReturnTemp = true;
                }
            }

            if (foldAndReturnTemp)
            {
                assert(temp != nullptr);
                assert(temp->TypeGet() == typ);
                assert((op1->OperGet() == GT_ADD) || (op1->OperGet() == GT_ADDR));

                // Copy the value of GTF_DONT_CSE from the original tree to `temp`: it can be set for
                // 'temp' because a GT_ADDR always marks it for its operand.
                temp->gtFlags &= ~GTF_DONT_CSE;
                temp->gtFlags |= (tree->gtFlags & GTF_DONT_CSE);

                if (op1->OperGet() == GT_ADD)
                {
                    DEBUG_DESTROY_NODE(op1->AsOp()->gtOp1); // GT_ADDR
                    DEBUG_DESTROY_NODE(op1->AsOp()->gtOp2); // GT_CNS_INT
                }
                DEBUG_DESTROY_NODE(op1);  // GT_ADD or GT_ADDR
                DEBUG_DESTROY_NODE(tree); // GT_IND

                // If the result of the fold is a local var, we may need to perform further adjustments e.g. for
                // normalization.
                if (temp->OperIs(GT_LCL_VAR))
                {
#ifdef DEBUG
                    // We clear this flag on `temp` because `fgMorphLocalVar` may assert that this bit is clear
                    // and the node in question must have this bit set (as it has already been morphed).
                    temp->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
                    const bool forceRemorph = true;
                    temp                    = fgMorphLocalVar(temp, forceRemorph);
#ifdef DEBUG
                    // We then set this flag on `temp` because `fgMorhpLocalVar` may not set it itself, and the
                    // caller of `fgMorphSmpOp` may assert that this flag is set on `temp` once this function
                    // returns.
                    temp->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif // DEBUG
                }

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
                GenTree* commaNode = op1;
                unsigned treeFlags = tree->gtFlags;
                commaNode->gtType  = typ;
                commaNode->gtFlags = (treeFlags & ~GTF_REVERSE_OPS); // Bashing the GT_COMMA flags here is
                                                                     // dangerous, clear the GTF_REVERSE_OPS at
                                                                     // least.
#ifdef DEBUG
                commaNode->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                while (commaNode->AsOp()->gtOp2->gtOper == GT_COMMA)
                {
                    commaNode         = commaNode->AsOp()->gtOp2;
                    commaNode->gtType = typ;
                    commaNode->gtFlags =
                        (treeFlags & ~GTF_REVERSE_OPS & ~GTF_ASG & ~GTF_CALL); // Bashing the GT_COMMA flags here is
                    // dangerous, clear the GTF_REVERSE_OPS, GT_ASG, and GT_CALL at
                    // least.
                    commaNode->gtFlags |= ((commaNode->AsOp()->gtOp1->gtFlags | commaNode->AsOp()->gtOp2->gtFlags) &
                                           (GTF_ASG | GTF_CALL));
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
                tree          = op1;
                GenTree* addr = commaNode->AsOp()->gtOp2;
                op1           = gtNewIndir(typ, addr);
                // This is very conservative
                op1->gtFlags |= treeFlags & ~GTF_ALL_EFFECT & ~GTF_IND_NONFAULTING;
                op1->gtFlags |= (addr->gtFlags & GTF_ALL_EFFECT);

                if (wasArrIndex)
                {
                    GetArrayInfoMap()->Set(op1, arrInfo);
                }
#ifdef DEBUG
                op1->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                commaNode->AsOp()->gtOp2 = op1;
                commaNode->gtFlags |= (op1->gtFlags & GTF_ALL_EFFECT);
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
                    GenTree* addr = op1->AsOp()->gtOp1;

                    // If tree has a zero field sequence annotation, update the annotation
                    // on addr node.
                    FieldSeqNode* zeroFieldSeq = nullptr;
                    if (GetZeroOffsetFieldMap()->Lookup(tree, &zeroFieldSeq))
                    {
                        fgAddFieldSeqForZeroOffset(addr, zeroFieldSeq);
                    }

                    noway_assert(varTypeIsGC(addr->gtType) || addr->gtType == TYP_I_IMPL);

                    DEBUG_DESTROY_NODE(op1);
                    DEBUG_DESTROY_NODE(tree);

                    return addr;
                }
            }
            else if (op1->OperGet() == GT_OBJ)
            {
                // Can not remove a GT_ADDR if it is currently a CSE candidate.
                if (gtIsActiveCSE_Candidate(tree))
                {
                    break;
                }

                // Perform the transform ADDR(OBJ(...)) == (...).
                GenTree* addr = op1->AsObj()->Addr();

                noway_assert(varTypeIsGC(addr->gtType) || addr->gtType == TYP_I_IMPL);

                DEBUG_DESTROY_NODE(op1);
                DEBUG_DESTROY_NODE(tree);

                return addr;
            }
            else if (op1->gtOper == GT_CAST)
            {
                GenTree* casting = op1->AsCast()->CastOp();
                if (casting->gtOper == GT_LCL_VAR || casting->gtOper == GT_CLS_VAR)
                {
                    DEBUG_DESTROY_NODE(op1);
                    tree->AsOp()->gtOp1 = op1 = casting;
                }
            }
            else if ((op1->gtOper == GT_COMMA) && !optValnumCSE_phase)
            {
                // Perform the transform ADDR(COMMA(x, ..., z)) == COMMA(x, ..., ADDR(z)).
                // (Be sure to mark "z" as an l-value...)

                GenTreePtrStack commas(getAllocator(CMK_ArrayStack));
                for (GenTree* comma = op1; comma != nullptr && comma->gtOper == GT_COMMA; comma = comma->gtGetOp2())
                {
                    commas.Push(comma);
                }
                GenTree* commaNode = commas.Top();

                // The top-level addr might be annotated with a zeroOffset field.
                FieldSeqNode* zeroFieldSeq = nullptr;
                bool          isZeroOffset = GetZeroOffsetFieldMap()->Lookup(tree, &zeroFieldSeq);
                tree                       = op1;
                commaNode->AsOp()->gtOp2->gtFlags |= GTF_DONT_CSE;

                // If the node we're about to put under a GT_ADDR is an indirection, it
                // doesn't need to be materialized, since we only want the addressing mode. Because
                // of this, this GT_IND is not a faulting indirection and we don't have to extract it
                // as a side effect.
                GenTree* commaOp2 = commaNode->AsOp()->gtOp2;
                if (commaOp2->OperIsBlk())
                {
                    commaOp2->SetOper(GT_IND);
                }
                if (commaOp2->gtOper == GT_IND)
                {
                    commaOp2->gtFlags |= GTF_IND_NONFAULTING;
                    commaOp2->gtFlags &= ~GTF_EXCEPT;
                    commaOp2->gtFlags |= (commaOp2->AsOp()->gtOp1->gtFlags & GTF_EXCEPT);
                }

                op1 = gtNewOperNode(GT_ADDR, TYP_BYREF, commaOp2);

                if (isZeroOffset)
                {
                    // Transfer the annotation to the new GT_ADDR node.
                    fgAddFieldSeqForZeroOffset(op1, zeroFieldSeq);
                }
                commaNode->AsOp()->gtOp2 = op1;
                // Originally, I gave all the comma nodes type "byref".  But the ADDR(IND(x)) == x transform
                // might give op1 a type different from byref (like, say, native int).  So now go back and give
                // all the comma nodes the type of op1.
                // TODO: the comma flag update below is conservative and can be improved.
                // For example, if we made the ADDR(IND(x)) == x transformation, we may be able to
                // get rid of some of the IND flags on the COMMA nodes (e.g., GTF_GLOB_REF).

                while (!commas.Empty())
                {
                    GenTree* comma = commas.Pop();
                    comma->gtType  = op1->gtType;
                    comma->gtFlags |= op1->gtFlags;
#ifdef DEBUG
                    comma->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                    gtUpdateNodeSideEffects(comma);
                }

                return tree;
            }
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
            if (op2->OperIs(GT_ASG) || (op2->OperGet() == GT_COMMA && op2->TypeGet() == TYP_VOID) || fgIsThrow(op2))
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

                GenTree* op1SideEffects = nullptr;
                // The addition of "GTF_MAKE_CSE" below prevents us from throwing away (for example)
                // hoisted expressions in loops.
                gtExtractSideEffList(op1, &op1SideEffects, (GTF_SIDE_EFFECT | GTF_MAKE_CSE));
                if (op1SideEffects)
                {
                    // Replace the left hand side with the side effect list.
                    tree->AsOp()->gtOp1 = op1SideEffects;
                    gtUpdateNodeSideEffects(tree);
                }
                else
                {
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
                    GenTree* throwNode = op1->AsOp()->gtOp1;
                    noway_assert(throwNode->gtType == TYP_VOID);

                    JITDUMP("Removing [%06d] GT_JTRUE as the block now unconditionally throws an exception.\n",
                            dspTreeID(tree));
                    DEBUG_DESTROY_NODE(tree);

                    return throwNode;
                }

                noway_assert(op1->OperKind() & GTK_RELOP);
                noway_assert(op1->gtFlags & GTF_EXCEPT);

                // We need to keep op1 for the side-effects. Hang it off
                // a GT_COMMA node

                JITDUMP("Keeping side-effects by bashing [%06d] GT_JTRUE into a GT_COMMA.\n", dspTreeID(tree));

                tree->ChangeOper(GT_COMMA);
                tree->AsOp()->gtOp2 = op2 = gtNewNothingNode();

                // Additionally since we're eliminating the JTRUE
                // codegen won't like it if op1 is a RELOP of longs, floats or doubles.
                // So we change it into a GT_COMMA as well.
                JITDUMP("Also bashing [%06d] (a relop) into a GT_COMMA.\n", dspTreeID(op1));
                op1->ChangeOper(GT_COMMA);
                op1->gtFlags &= ~GTF_UNSIGNED; // Clear the unsigned flag if it was set on the relop
                op1->gtType = op1->AsOp()->gtOp1->gtType;

                return tree;
            }

        default:
            break;
    }

    assert(oper == tree->gtOper);

    // If we are in the Valuenum CSE phase then don't morph away anything as these
    // nodes may have CSE defs/uses in them.
    //
    if (!optValnumCSE_phase && (oper != GT_ASG) && (oper != GT_COLON) && !tree->OperIsAnyList())
    {
        /* Check for op1 as a GT_COMMA with a unconditional throw node */
        if (op1 && fgIsCommaThrow(op1, true))
        {
            if ((op1->gtFlags & GTF_COLON_COND) == 0)
            {
                /* We can safely throw out the rest of the statements */
                fgRemoveRestOfBlock = true;
            }

            GenTree* throwNode = op1->AsOp()->gtOp1;
            noway_assert(throwNode->gtType == TYP_VOID);

            if (oper == GT_COMMA)
            {
                /* Both tree and op1 are GT_COMMA nodes */
                /* Change the tree's op1 to the throw node: op1->AsOp()->gtOp1 */
                tree->AsOp()->gtOp1 = throwNode;

                // Possibly reset the assignment flag
                if (((throwNode->gtFlags & GTF_ASG) == 0) && ((op2 == nullptr) || ((op2->gtFlags & GTF_ASG) == 0)))
                {
                    tree->gtFlags &= ~GTF_ASG;
                }

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
                        GenTree* commaOp2 = op1->AsOp()->gtOp2;

                        // need type of oper to be same as tree
                        if (typ == TYP_LONG)
                        {
                            commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                            commaOp2->AsIntConCommon()->SetLngValue(0);
                            /* Change the types of oper and commaOp2 to TYP_LONG */
                            op1->gtType = commaOp2->gtType = TYP_LONG;
                        }
                        else if (varTypeIsFloating(typ))
                        {
                            commaOp2->ChangeOperConst(GT_CNS_DBL);
                            commaOp2->AsDblCon()->gtDconVal = 0.0;
                            /* Change the types of oper and commaOp2 to TYP_DOUBLE */
                            op1->gtType = commaOp2->gtType = TYP_DOUBLE;
                        }
                        else
                        {
                            commaOp2->ChangeOperConst(GT_CNS_INT);
                            commaOp2->AsIntConCommon()->SetIconValue(0);
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
                if (tree->OperIs(GT_ASG))
                {
                    /* Return the throw node as the new tree */
                    return op2->AsOp()->gtOp1;
                }

                if (tree->OperGet() == GT_ARR_BOUNDS_CHECK)
                {
                    /* Return the throw node as the new tree */
                    return op2->AsOp()->gtOp1;
                }

                // If tree is a comma node
                if (tree->OperGet() == GT_COMMA)
                {
                    /* Return the throw node as the new tree */
                    return op2->AsOp()->gtOp1;
                }

                /* for the shift nodes the type of op2 can differ from the tree type */
                if ((typ == TYP_LONG) && (genActualType(op2->gtType) == TYP_INT))
                {
                    noway_assert(GenTree::OperIsShiftOrRotate(oper));

                    GenTree* commaOp2 = op2->AsOp()->gtOp2;

                    commaOp2->ChangeOperConst(GT_CNS_NATIVELONG);
                    commaOp2->AsIntConCommon()->SetLngValue(0);

                    /* Change the types of oper and commaOp2 to TYP_LONG */
                    op2->gtType = commaOp2->gtType = TYP_LONG;
                }

                if ((genActualType(typ) == TYP_INT) &&
                    (genActualType(op2->gtType) == TYP_LONG || varTypeIsFloating(op2->TypeGet())))
                {
                    // An example case is comparison (say GT_GT) of two longs or floating point values.

                    GenTree* commaOp2 = op2->AsOp()->gtOp2;

                    commaOp2->ChangeOperConst(GT_CNS_INT);
                    commaOp2->AsIntCon()->gtIconVal = 0;
                    /* Change the types of oper and commaOp2 to TYP_INT */
                    op2->gtType = commaOp2->gtType = TYP_INT;
                }

                if ((typ == TYP_BYREF) && (genActualType(op2->gtType) == TYP_I_IMPL))
                {
                    noway_assert(tree->OperGet() == GT_ADD);

                    GenTree* commaOp2 = op2->AsOp()->gtOp2;

                    commaOp2->ChangeOperConst(GT_CNS_INT);
                    commaOp2->AsIntCon()->gtIconVal = 0;
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

    return tree;
}

//----------------------------------------------------------------------------------------------
// fgMorphRetInd: Try to get rid of extra IND(ADDR()) pairs in a return tree.
//
// Arguments:
//    node - The return node that uses an indirection.
//
// Return Value:
//    the original op1 of the ret if there was no optimization or an optimized new op1.
//
GenTree* Compiler::fgMorphRetInd(GenTreeUnOp* ret)
{
    assert(!compDoOldStructRetyping());
    assert(ret->OperIs(GT_RETURN));
    assert(ret->gtGetOp1()->OperIs(GT_IND, GT_BLK, GT_OBJ));
    GenTreeIndir* ind  = ret->gtGetOp1()->AsIndir();
    GenTree*      addr = ind->Addr();

    if (addr->OperIs(GT_ADDR) && addr->gtGetOp1()->OperIs(GT_LCL_VAR))
    {
        // If `return` retypes LCL_VAR as a smaller struct it should not set `doNotEnregister` on that
        // LclVar.
        // Example: in `Vector128:AsVector2` we have RETURN SIMD8(OBJ SIMD8(ADDR byref(LCL_VAR SIMD16))).
        GenTreeLclVar* lclVar = addr->gtGetOp1()->AsLclVar();
        if (!lvaIsImplicitByRefLocal(lclVar->GetLclNum()))
        {
            assert(!gtIsActiveCSE_Candidate(addr) && !gtIsActiveCSE_Candidate(ind));
            unsigned indSize;
            if (ind->OperIs(GT_IND))
            {
                indSize = genTypeSize(ind);
            }
            else
            {
                indSize = ind->AsBlk()->GetLayout()->GetSize();
            }

            LclVarDsc* varDsc = lvaGetDesc(lclVar);

            unsigned lclVarSize;
            if (!lclVar->TypeIs(TYP_STRUCT))

            {
                lclVarSize = genTypeSize(varDsc->TypeGet());
            }
            else
            {
                lclVarSize = varDsc->lvExactSize;
            }
            // TODO: change conditions in `canFold` to `indSize <= lclVarSize`, but currently do not support `BITCAST
            // int<-SIMD16` etc.
            assert((indSize <= lclVarSize) || varDsc->lvDoNotEnregister);

#if defined(TARGET_64BIT)
            bool canFold = (indSize == lclVarSize);
#else // !TARGET_64BIT
            // TODO: improve 32 bit targets handling for LONG returns if necessary, nowadays we do not support `BITCAST
            // long<->double` there.
            bool canFold = (indSize == lclVarSize) && (lclVarSize <= REGSIZE_BYTES);
#endif
            // TODO: support `genReturnBB != nullptr`, it requires #11413 to avoid `Incompatible types for
            // gtNewTempAssign`.
            if (canFold && (genReturnBB == nullptr))
            {
                // Fold (TYPE1)*(&(TYPE2)x) even if types do not match, lowering will handle it.
                // Getting rid of this IND(ADDR()) pair allows to keep lclVar as not address taken
                // and enregister it.
                DEBUG_DESTROY_NODE(ind);
                DEBUG_DESTROY_NODE(addr);
                ret->gtOp1 = lclVar;
                return ret->gtGetOp1();
            }
            else if (!varDsc->lvDoNotEnregister)
            {
                lvaSetVarDoNotEnregister(lclVar->GetLclNum() DEBUGARG(Compiler::DNER_BlockOp));
            }
        }
    }
    return ind;
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

    if (fgGlobalMorph && GenTree::OperIsCommutative(oper))
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

    if (fgGlobalMorph && (oper == GT_ADD) && !tree->gtOverflow() && (op1->gtOper == GT_ADD) && !op1->gtOverflow() &&
        varTypeIsIntegralOrI(typ))
    {
        GenTree* ad1 = op1->AsOp()->gtOp1;
        GenTree* ad2 = op1->AsOp()->gtOp2;

        if (!op2->OperIsConst() && ad2->OperIsConst())
        {
            //  This takes
            //        + (tree)
            //       / \.
            //      /   \.
            //     /     \.
            //    + (op1) op2
            //   / \.
            //  /   \.
            // ad1  ad2
            //
            // and it swaps ad2 and op2.

            // Don't create a byref pointer that may point outside of the ref object.
            // If a GC happens, the byref won't get updated. This can happen if one
            // of the int components is negative. It also requires the address generation
            // be in a fully-interruptible code region.
            if (!varTypeIsGC(ad1->TypeGet()) && !varTypeIsGC(op2->TypeGet()))
            {
                tree->gtOp2 = ad2;

                op1->AsOp()->gtOp2 = op2;
                op1->gtFlags |= op2->gtFlags & GTF_ALL_EFFECT;

                op2 = tree->gtOp2;
            }
        }
    }

#endif

    /*-------------------------------------------------------------------------
     * Perform optional oper-specific postorder morphing
     */

    switch (oper)
    {
        case GT_ASG:
            // Make sure we're allowed to do this.
            if (optValnumCSE_phase)
            {
                // It is not safe to reorder/delete CSE's
                break;
            }

            if (varTypeIsStruct(typ) && !tree->IsPhiDefn())
            {
                if (tree->OperIsCopyBlkOp())
                {
                    return fgMorphCopyBlock(tree);
                }
                else
                {
                    return fgMorphInitBlock(tree);
                }
            }

            if (typ == TYP_LONG)
            {
                break;
            }

            if (op2->gtFlags & GTF_ASG)
            {
                break;
            }

            if ((op2->gtFlags & GTF_CALL) && (op1->gtFlags & GTF_ALL_EFFECT))
            {
                break;
            }

            /* Special case: a cast that can be thrown away */

            // TODO-Cleanup: fgMorphSmp does a similar optimization. However, it removes only
            // one cast and sometimes there is another one after it that gets removed by this
            // code. fgMorphSmp should be improved to remove all redundant casts so this code
            // can be removed.

            if (op1->gtOper == GT_IND && op2->gtOper == GT_CAST && !op2->gtOverflow())
            {
                var_types srct;
                var_types cast;
                var_types dstt;

                srct = op2->AsCast()->CastOp()->TypeGet();
                cast = (var_types)op2->CastToType();
                dstt = op1->TypeGet();

                /* Make sure these are all ints and precision is not lost */

                if (genTypeSize(cast) >= genTypeSize(dstt) && dstt <= TYP_INT && srct <= TYP_INT)
                {
                    op2 = tree->gtOp2 = op2->AsCast()->CastOp();
                }
            }

            break;

        case GT_MUL:

            /* Check for the case "(val + icon) * icon" */

            if (op2->gtOper == GT_CNS_INT && op1->gtOper == GT_ADD)
            {
                GenTree* add = op1->AsOp()->gtOp2;

                if (add->IsCnsIntOrI() && (op2->GetScaleIndexMul() != 0))
                {
                    if (tree->gtOverflow() || op1->gtOverflow())
                    {
                        break;
                    }

                    ssize_t imul = op2->AsIntCon()->gtIconVal;
                    ssize_t iadd = add->AsIntCon()->gtIconVal;

                    /* Change '(val + iadd) * imul' -> '(val * imul) + (iadd * imul)' */

                    oper = GT_ADD;
                    tree->ChangeOper(oper);

                    op2->AsIntCon()->gtIconVal = iadd * imul;

                    op1->ChangeOper(GT_MUL);

                    add->AsIntCon()->gtIconVal = imul;
#ifdef TARGET_64BIT
                    if (add->gtType == TYP_INT)
                    {
                        // we need to properly re-sign-extend or truncate after multiplying two int constants above
                        add->AsIntCon()->TruncateOrSignExtend32();
                    }
#endif // TARGET_64BIT
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

        case GT_UDIV:
        case GT_UMOD:
            tree->CheckDivideByConstOptimized(this);
            break;

        case GT_LSH:

            /* Check for the case "(val + icon) << icon" */

            if (!optValnumCSE_phase && op2->IsCnsIntOrI() && op1->gtOper == GT_ADD && !op1->gtOverflow())
            {
                GenTree* cns = op1->AsOp()->gtOp2;

                if (cns->IsCnsIntOrI() && (op2->GetScaleIndexShf() != 0))
                {
                    ssize_t ishf = op2->AsIntConCommon()->IconValue();
                    ssize_t iadd = cns->AsIntConCommon()->IconValue();

                    // printf("Changing '(val+icon1)<<icon2' into '(val<<icon2+icon1<<icon2)'\n");

                    /* Change "(val + iadd) << ishf" into "(val<<ishf + iadd<<ishf)" */

                    tree->ChangeOper(GT_ADD);
                    ssize_t result = iadd << ishf;
                    op2->AsIntConCommon()->SetIconValue(result);
#ifdef TARGET_64BIT
                    if (op1->gtType == TYP_INT)
                    {
                        op2->AsIntCon()->TruncateOrSignExtend32();
                    }
#endif // TARGET_64BIT

                    // we are reusing the shift amount node here, but the type we want is that of the shift result
                    op2->gtType = op1->gtType;

                    if (cns->gtOper == GT_CNS_INT && cns->AsIntCon()->gtFieldSeq != nullptr &&
                        cns->AsIntCon()->gtFieldSeq->IsConstantIndexFieldSeq())
                    {
                        assert(cns->AsIntCon()->gtFieldSeq->m_next == nullptr);
                        op2->AsIntCon()->gtFieldSeq = cns->AsIntCon()->gtFieldSeq;
                    }

                    op1->ChangeOper(GT_LSH);

                    cns->AsIntConCommon()->SetIconValue(ishf);
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

        case GT_INIT_VAL:
            // Initialization values for initBlk have special semantics - their lower
            // byte is used to fill the struct. However, we allow 0 as a "bare" value,
            // which enables them to get a VNForZero, and be propagated.
            if (op1->IsIntegralConst(0))
            {
                return op1;
            }
            break;

        default:
            break;
    }
    return tree;
}

//------------------------------------------------------------------------
// fgMorphModToSubMulDiv: Transform a % b into the equivalent a - (a / b) * b
// (see ECMA III 3.55 and III.3.56).
//
// Arguments:
//    tree - The GT_MOD/GT_UMOD tree to morph
//
// Returns:
//    The morphed tree
//
// Notes:
//    For ARM64 we don't have a remainder instruction so this transform is
//    always done. For XARCH this transform is done if we know that magic
//    division will be used, in that case this transform allows CSE to
//    eliminate the redundant div from code like "x = a / 3; y = a % 3;".
//
//    This method will produce the above expression in 'a' and 'b' are
//    leaf nodes, otherwise, if any of them is not a leaf it will spill
//    its value into a temporary variable, an example:
//    (x * 2 - 1) % (y + 1) ->  t1 - (t2 * ( comma(t1 = x * 2 - 1, t1) / comma(t2 = y + 1, t2) ) )
//
GenTree* Compiler::fgMorphModToSubMulDiv(GenTreeOp* tree)
{
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

    // The numerator and denominator may have been assigned to temps, in which case
    // their defining assignments are in the current tree. Therefore, we need to
    // set the execuction order accordingly on the nodes we create.
    // That is, the "mul" will be evaluated in "normal" order, and the "sub" must
    // be set to be evaluated in reverse order.
    //
    GenTree* mul = gtNewOperNode(GT_MUL, type, tree, gtCloneExpr(denominator));
    assert(!mul->IsReverseOp());
    GenTree* sub = gtNewOperNode(GT_SUB, type, gtCloneExpr(numerator), mul);
    sub->gtFlags |= GTF_REVERSE_OPS;

#ifdef DEBUG
    sub->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif

    tree->CheckDivideByConstOptimized(this);

    return sub;
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

GenTree* Compiler::fgRecognizeAndMorphBitwiseRotation(GenTree* tree)
{
    //
    // Check for a rotation pattern, e.g.,
    //
    //                         OR                      ROL
    //                      /      \                   / \.
    //                    LSH      RSZ      ->        x   y
    //                    / \      / \.
    //                   x  AND   x  AND
    //                      / \      / \.
    //                     y  31   ADD  31
    //                             / \.
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
    GenTree* op1            = tree->gtGetOp1();
    GenTree* op2            = tree->gtGetOp2();
    GenTree* leftShiftTree  = nullptr;
    GenTree* rightShiftTree = nullptr;
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
        GenTree*  rotatedValue           = leftShiftTree->gtGetOp1();
        var_types rotatedValueActualType = genActualType(rotatedValue->gtType);
        ssize_t   rotatedValueBitSize    = genTypeSize(rotatedValueActualType) * 8;
        noway_assert((rotatedValueBitSize == 32) || (rotatedValueBitSize == 64));
        GenTree* leftShiftIndex  = leftShiftTree->gtGetOp2();
        GenTree* rightShiftIndex = rightShiftTree->gtGetOp2();

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
                leftShiftMask  = leftShiftIndex->gtGetOp2()->AsIntCon()->gtIconVal;
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
                rightShiftMask  = rightShiftIndex->gtGetOp2()->AsIntCon()->gtIconVal;
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

        GenTree*   shiftIndexWithAdd    = nullptr;
        GenTree*   shiftIndexWithoutAdd = nullptr;
        genTreeOps rotateOp             = GT_NONE;
        GenTree*   rotateIndex          = nullptr;

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
                if (shiftIndexWithAdd->gtGetOp2()->AsIntCon()->gtIconVal == rotatedValueBitSize)
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

#ifndef TARGET_64BIT
                            if (!shiftIndexWithoutAdd->IsCnsIntOrI() && (rotatedValueBitSize == 64))
                            {
                                // TODO-X86-CQ: we need to handle variable-sized long shifts specially on x86.
                                // GT_LSH, GT_RSH, and GT_RSZ have helpers for this case. We may need
                                // to add helpers for GT_ROL and GT_ROR.
                                return tree;
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
            if (leftShiftIndex->AsIntCon()->gtIconVal + rightShiftIndex->AsIntCon()->gtIconVal == rotatedValueBitSize)
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
                tree->AsOp()->gtOp1 = rotatedValue;
                tree->AsOp()->gtOp2 = rotateIndex;
                tree->ChangeOper(rotateOp);

                unsigned childFlags = 0;
                for (GenTree* op : tree->Operands())
                {
                    childFlags |= (op->gtFlags & GTF_ALL_EFFECT);
                }

                // The parent's flags should be a superset of its operands' flags
                noway_assert((inputTreeEffects & childFlags) == childFlags);
            }
            else
            {
                tree = gtNewOperNode(rotateOp, rotatedValueActualType, rotatedValue, rotateIndex);
                noway_assert(inputTreeEffects == (tree->gtFlags & GTF_ALL_EFFECT));
            }

            return tree;
        }
    }
    return tree;
}

#if !CPU_HAS_FP_SUPPORT
GenTree* Compiler::fgMorphToEmulatedFP(GenTree* tree)
{

    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();
    GenTree*   op1  = tree->AsOp()->gtOp1;
    GenTree*   op2  = tree->gtGetOp2IfPresent();

    /*
        We have to use helper calls for all FP operations:

            FP operators that operate on FP values
            casts to and from FP
            comparisons of FP values
     */

    if (varTypeIsFloating(typ) || (op1 && varTypeIsFloating(op1->TypeGet())))
    {
        int      helper;
        GenTree* args;

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

        /* Is this a binary operator? */

        if (op2)
        {
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

            args = gtNewArgList(tree->AsOp()->gtOp2, tree->AsOp()->gtOp1);
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

            args = gtNewArgList(tree->AsOp()->gtOp1);
        }

        /* If we have double result/operands, modify the helper */

        if (typ == TYP_DOUBLE)
        {
            static_assert_no_msg(CPX_R4_NEG + 1 == CPX_R8_NEG);
            static_assert_no_msg(CPX_R4_ADD + 1 == CPX_R8_ADD);
            static_assert_no_msg(CPX_R4_SUB + 1 == CPX_R8_SUB);
            static_assert_no_msg(CPX_R4_MUL + 1 == CPX_R8_MUL);
            static_assert_no_msg(CPX_R4_DIV + 1 == CPX_R8_DIV);

            helper++;
        }
        else
        {
            noway_assert(tree->OperIsCompare());

            static_assert_no_msg(CPX_R4_EQ + 1 == CPX_R8_EQ);
            static_assert_no_msg(CPX_R4_NE + 1 == CPX_R8_NE);
            static_assert_no_msg(CPX_R4_LT + 1 == CPX_R8_LT);
            static_assert_no_msg(CPX_R4_LE + 1 == CPX_R8_LE);
            static_assert_no_msg(CPX_R4_GE + 1 == CPX_R8_GE);
            static_assert_no_msg(CPX_R4_GT + 1 == CPX_R8_GT);
        }

        tree = fgMorphIntoHelperCall(tree, helper, args);

        return tree;

        case GT_RETURN:

            if (op1)
            {

                if (compCurBB == genReturnBB)
                {
                    /* This is the 'exitCrit' call at the exit label */

                    noway_assert(op1->gtType == TYP_VOID);
                    noway_assert(op2 == 0);

                    tree->AsOp()->gtOp1 = op1 = fgMorphTree(op1);

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

GenTree* Compiler::fgMorphTree(GenTree* tree, MorphAddrContext* mac)
{
    assert(tree);

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

    if (fgGlobalMorph)
    {
        // Apply any rewrites for implicit byref arguments before morphing the
        // tree.

        if (fgMorphImplicitByRefArgs(tree))
        {
#ifdef DEBUG
            if (verbose && treesBeforeAfterMorph)
            {
                printf("\nfgMorphTree (%d), after implicit-byref rewrite:\n", thisMorphNum);
                gtDispTree(tree);
            }
#endif
        }
    }

/*-------------------------------------------------------------------------
 * fgMorphTree() can potentially replace a tree with another, and the
 * caller has to store the return value correctly.
 * Turn this on to always make copy of "tree" here to shake out
 * hidden/unupdated references.
 */

#ifdef DEBUG

    if (compStressCompile(STRESS_GENERIC_CHECK, 0))
    {
        GenTree* copy;

        if (GenTree::s_gtNodeSizes[tree->gtOper] == TREE_NODE_SZ_SMALL)
        {
            copy = gtNewLargeOperNode(GT_ADD, TYP_INT);
        }
        else
        {
            copy = new (this, GT_CALL) GenTreeCall(TYP_INT);
        }

        copy->ReplaceWith(tree, this);

#if defined(LATE_DISASM)
        // GT_CNS_INT is considered small, so ReplaceWith() won't copy all fields
        if ((tree->gtOper == GT_CNS_INT) && tree->IsIconHandle())
        {
            copy->AsIntCon()->gtCompileTimeHandle = tree->AsIntCon()->gtCompileTimeHandle;
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
                GenTree* newTree = tree;
                while (newTree != nullptr)
                {
                    tree = newTree;
                    /* newTree is non-Null if we propagated an assertion */
                    newTree = optAssertionProp(apFull, tree, nullptr, nullptr);
                }
                assert(tree != nullptr);
            }
        }
        PREFAST_ASSUME(tree != nullptr);
#endif
    }

    /* Save the original un-morphed tree for fgMorphTreeDone */

    GenTree* oldTree = tree;

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
            if (tree->OperMayThrow(this))
            {
                tree->gtFlags |= GTF_EXCEPT;
            }
            else
            {
                tree->gtFlags &= ~GTF_EXCEPT;
            }
            tree = fgMorphCall(tree->AsCall());
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
        {
            fgSetRngChkTarget(tree);

            GenTreeBoundsChk* bndsChk = tree->AsBoundsChk();
            bndsChk->gtIndex          = fgMorphTree(bndsChk->gtIndex);
            bndsChk->gtArrLen         = fgMorphTree(bndsChk->gtArrLen);
            // If the index is a comma(throw, x), just return that.
            if (!optValnumCSE_phase && fgIsCommaThrow(bndsChk->gtIndex))
            {
                tree = bndsChk->gtIndex;
            }

            bndsChk->gtFlags &= ~GTF_CALL;

            // Propagate effects flags upwards
            bndsChk->gtFlags |= (bndsChk->gtIndex->gtFlags & GTF_ALL_EFFECT);
            bndsChk->gtFlags |= (bndsChk->gtArrLen->gtFlags & GTF_ALL_EFFECT);

            // Otherwise, we don't change the tree.
        }
        break;

        case GT_ARR_ELEM:
            tree->AsArrElem()->gtArrObj = fgMorphTree(tree->AsArrElem()->gtArrObj);

            unsigned dim;
            for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
            {
                tree->AsArrElem()->gtArrInds[dim] = fgMorphTree(tree->AsArrElem()->gtArrInds[dim]);
            }

            tree->gtFlags &= ~GTF_CALL;

            tree->gtFlags |= tree->AsArrElem()->gtArrObj->gtFlags & GTF_ALL_EFFECT;

            for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
            {
                tree->gtFlags |= tree->AsArrElem()->gtArrInds[dim]->gtFlags & GTF_ALL_EFFECT;
            }

            if (fgGlobalMorph)
            {
                fgSetRngChkTarget(tree, false);
            }
            break;

        case GT_ARR_OFFSET:
            tree->AsArrOffs()->gtOffset = fgMorphTree(tree->AsArrOffs()->gtOffset);
            tree->AsArrOffs()->gtIndex  = fgMorphTree(tree->AsArrOffs()->gtIndex);
            tree->AsArrOffs()->gtArrObj = fgMorphTree(tree->AsArrOffs()->gtArrObj);

            tree->gtFlags &= ~GTF_CALL;
            tree->gtFlags |= tree->AsArrOffs()->gtOffset->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= tree->AsArrOffs()->gtIndex->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= tree->AsArrOffs()->gtArrObj->gtFlags & GTF_ALL_EFFECT;
            if (fgGlobalMorph)
            {
                fgSetRngChkTarget(tree, false);
            }
            break;

        case GT_PHI:
            tree->gtFlags &= ~GTF_ALL_EFFECT;
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                use.SetNode(fgMorphTree(use.GetNode()));
                tree->gtFlags |= use.GetNode()->gtFlags & GTF_ALL_EFFECT;
            }
            break;

        case GT_FIELD_LIST:
            tree->gtFlags &= ~GTF_ALL_EFFECT;
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                use.SetNode(fgMorphTree(use.GetNode()));
                tree->gtFlags |= (use.GetNode()->gtFlags & GTF_ALL_EFFECT);
            }
            break;

        case GT_CMPXCHG:
            tree->AsCmpXchg()->gtOpLocation  = fgMorphTree(tree->AsCmpXchg()->gtOpLocation);
            tree->AsCmpXchg()->gtOpValue     = fgMorphTree(tree->AsCmpXchg()->gtOpValue);
            tree->AsCmpXchg()->gtOpComparand = fgMorphTree(tree->AsCmpXchg()->gtOpComparand);

            tree->gtFlags &= (~GTF_EXCEPT & ~GTF_CALL);

            tree->gtFlags |= tree->AsCmpXchg()->gtOpLocation->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= tree->AsCmpXchg()->gtOpValue->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= tree->AsCmpXchg()->gtOpComparand->gtFlags & GTF_ALL_EFFECT;
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            if (tree->OperGet() == GT_STORE_DYN_BLK)
            {
                tree->AsDynBlk()->Data() = fgMorphTree(tree->AsDynBlk()->Data());
            }
            tree->AsDynBlk()->Addr()        = fgMorphTree(tree->AsDynBlk()->Addr());
            tree->AsDynBlk()->gtDynamicSize = fgMorphTree(tree->AsDynBlk()->gtDynamicSize);

            tree->gtFlags &= ~GTF_CALL;
            tree->SetIndirExceptionFlags(this);

            if (tree->OperGet() == GT_STORE_DYN_BLK)
            {
                tree->gtFlags |= tree->AsDynBlk()->Data()->gtFlags & GTF_ALL_EFFECT;
            }
            tree->gtFlags |= tree->AsDynBlk()->Addr()->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= tree->AsDynBlk()->gtDynamicSize->gtFlags & GTF_ALL_EFFECT;
            break;

        case GT_INDEX_ADDR:
            GenTreeIndexAddr* indexAddr;
            indexAddr          = tree->AsIndexAddr();
            indexAddr->Index() = fgMorphTree(indexAddr->Index());
            indexAddr->Arr()   = fgMorphTree(indexAddr->Arr());

            tree->gtFlags &= ~GTF_CALL;

            tree->gtFlags |= indexAddr->Index()->gtFlags & GTF_ALL_EFFECT;
            tree->gtFlags |= indexAddr->Arr()->gtFlags & GTF_ALL_EFFECT;
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
//------------------------------------------------------------------------
// fgKillDependentAssertionsSingle: Kill all assertions specific to lclNum
//
// Arguments:
//    lclNum - The varNum of the lclVar for which we're killing assertions.
//    tree   - (DEBUG only) the tree responsible for killing its assertions.
//
void Compiler::fgKillDependentAssertionsSingle(unsigned lclNum DEBUGARG(GenTree* tree))
{
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
//------------------------------------------------------------------------
// fgKillDependentAssertions: Kill all dependent assertions with regard to lclNum.
//
// Arguments:
//    lclNum - The varNum of the lclVar for which we're killing assertions.
//    tree   - (DEBUG only) the tree responsible for killing its assertions.
//
// Notes:
//    For structs and struct fields, it will invalidate the children and parent
//    respectively.
//    Calls fgKillDependentAssertionsSingle to kill the assertions for a single lclVar.
//
void Compiler::fgKillDependentAssertions(unsigned lclNum DEBUGARG(GenTree* tree))
{
    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varDsc->lvPromoted)
    {
        noway_assert(varTypeIsStruct(varDsc));

        // Kill the field locals.
        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            fgKillDependentAssertionsSingle(i DEBUGARG(tree));
        }

        // Kill the struct local itself.
        fgKillDependentAssertionsSingle(lclNum DEBUGARG(tree));
    }
    else if (varDsc->lvIsStructField)
    {
        // Kill the field local.
        fgKillDependentAssertionsSingle(lclNum DEBUGARG(tree));

        // Kill the parent struct.
        fgKillDependentAssertionsSingle(varDsc->lvParentLcl DEBUGARG(tree));
    }
    else
    {
        fgKillDependentAssertionsSingle(lclNum DEBUGARG(tree));
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

void Compiler::fgMorphTreeDone(GenTree* tree,
                               GenTree* oldTree /* == NULL */
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
        GenTreeLclVarCommon* lclVarTree = nullptr;

        // The check below will miss LIR-style assignments.
        //
        // But we shouldn't be running local assertion prop on these,
        // as local prop gets disabled when we run global prop.
        assert(!tree->OperIs(GT_STORE_LCL_VAR, GT_STORE_LCL_FLD));

        // DefinesLocal can return true for some BLK op uses, so
        // check what gets assigned only when we're at an assignment.
        if (tree->OperIs(GT_ASG) && tree->DefinesLocal(this, &lclVarTree))
        {
            unsigned lclNum = lclVarTree->GetLclNum();
            noway_assert(lclNum < lvaCount);
            fgKillDependentAssertions(lclNum DEBUGARG(tree));
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
    if (opts.OptimizationDisabled())
    {
        return false;
    }

    if (block->bbJumpKind == BBJ_COND)
    {
        noway_assert(block->bbStmtList != nullptr && block->bbStmtList->GetPrevStmt() != nullptr);

        Statement* lastStmt = block->lastStmt();

        noway_assert(lastStmt->GetNextStmt() == nullptr);

        if (lastStmt->GetRootNode()->gtOper == GT_CALL)
        {
            noway_assert(fgRemoveRestOfBlock);

            /* Unconditional throw - transform the basic block into a BBJ_THROW */
            fgConvertBBToThrowBB(block);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at " FMT_BB "\n", block->bbNum);
                printf(FMT_BB " becomes a BBJ_THROW\n", block->bbNum);
            }
#endif
            goto DONE_COND;
        }

        noway_assert(lastStmt->GetRootNode()->gtOper == GT_JTRUE);

        /* Did we fold the conditional */

        noway_assert(lastStmt->GetRootNode()->AsOp()->gtOp1);
        GenTree* condTree;
        condTree = lastStmt->GetRootNode()->AsOp()->gtOp1;
        GenTree* cond;
        cond = condTree->gtEffectiveVal(true);

        if (cond->OperKind() & GTK_CONST)
        {
            /* Yupee - we folded the conditional!
             * Remove the conditional statement */

            noway_assert(cond->gtOper == GT_CNS_INT);
            noway_assert((block->bbNext->countOfInEdges() > 0) && (block->bbJumpDest->countOfInEdges() > 0));

            if (condTree != cond)
            {
                // Preserve any side effects
                assert(condTree->OperIs(GT_COMMA));
                lastStmt->SetRootNode(condTree);
            }
            else
            {
                // no side effects, remove the jump entirely
                fgRemoveStmt(block, lastStmt);
            }
            // block is a BBJ_COND that we are folding the conditional for
            // bTaken is the path that will always be taken from block
            // bNotTaken is the path that will never be taken from block
            //
            BasicBlock* bTaken;
            BasicBlock* bNotTaken;

            if (cond->AsIntCon()->gtIconVal != 0)
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
                if (block->hasProfileWeight())
                {
                    // The edge weights for (block -> bTaken) are 100% of block's weight

                    edgeTaken->setEdgeWeights(block->bbWeight, block->bbWeight);

                    if (!bTaken->hasProfileWeight())
                    {
                        if ((bTaken->countOfInEdges() == 1) || (bTaken->bbWeight < block->bbWeight))
                        {
                            // Update the weight of bTaken
                            bTaken->inheritWeight(block);
                            bUpdated = bTaken;
                        }
                    }
                }
                else if (bTaken->hasProfileWeight())
                {
                    if (bTaken->countOfInEdges() == 1)
                    {
                        // There is only one in edge to bTaken
                        edgeTaken->setEdgeWeights(bTaken->bbWeight, bTaken->bbWeight);

                        // Update the weight of block
                        block->inheritWeight(bTaken);
                        bUpdated = block;
                    }
                }

                if (bUpdated != nullptr)
                {
                    BasicBlock::weight_t newMinWeight;
                    BasicBlock::weight_t newMaxWeight;

                    flowList* edge;
                    // Now fix the weights of the edges out of 'bUpdated'
                    switch (bUpdated->bbJumpKind)
                    {
                        case BBJ_NONE:
                            edge         = fgGetPredForBlock(bUpdated->bbNext, bUpdated);
                            newMaxWeight = bUpdated->bbWeight;
                            newMinWeight = min(edge->edgeWeightMin(), newMaxWeight);
                            edge->setEdgeWeights(newMinWeight, newMaxWeight);
                            break;

                        case BBJ_COND:
                            edge         = fgGetPredForBlock(bUpdated->bbNext, bUpdated);
                            newMaxWeight = bUpdated->bbWeight;
                            newMinWeight = min(edge->edgeWeightMin(), newMaxWeight);
                            edge->setEdgeWeights(newMinWeight, newMaxWeight);
                            __fallthrough;

                        case BBJ_ALWAYS:
                            edge         = fgGetPredForBlock(bUpdated->bbJumpDest, bUpdated);
                            newMaxWeight = bUpdated->bbWeight;
                            newMinWeight = min(edge->edgeWeightMin(), newMaxWeight);
                            edge->setEdgeWeights(newMinWeight, newMaxWeight);
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
                printf("\nConditional folded at " FMT_BB "\n", block->bbNum);
                printf(FMT_BB " becomes a %s", block->bbNum,
                       block->bbJumpKind == BBJ_ALWAYS ? "BBJ_ALWAYS" : "BBJ_NONE");
                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    printf(" to " FMT_BB, block->bbJumpDest->bbNum);
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
                    if (cond->AsIntCon()->gtIconVal == 0)
                    {
                        /* This was a bogus loop (condition always false)
                         * Remove the loop from the table */

                        optLoopTable[loopNum].lpFlags |= LPFLG_REMOVED;
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Removing loop L%02u (from " FMT_BB " to " FMT_BB ")\n\n", loopNum,
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
        noway_assert(block->bbStmtList != nullptr && block->bbStmtList->GetPrevStmt() != nullptr);

        Statement* lastStmt = block->lastStmt();

        noway_assert(lastStmt->GetNextStmt() == nullptr);

        if (lastStmt->GetRootNode()->gtOper == GT_CALL)
        {
            noway_assert(fgRemoveRestOfBlock);

            /* Unconditional throw - transform the basic block into a BBJ_THROW */
            fgConvertBBToThrowBB(block);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nConditional folded at " FMT_BB "\n", block->bbNum);
                printf(FMT_BB " becomes a BBJ_THROW\n", block->bbNum);
            }
#endif
            goto DONE_SWITCH;
        }

        noway_assert(lastStmt->GetRootNode()->gtOper == GT_SWITCH);

        /* Did we fold the conditional */

        noway_assert(lastStmt->GetRootNode()->AsOp()->gtOp1);
        GenTree* condTree;
        condTree = lastStmt->GetRootNode()->AsOp()->gtOp1;
        GenTree* cond;
        cond = condTree->gtEffectiveVal(true);

        if (cond->OperKind() & GTK_CONST)
        {
            /* Yupee - we folded the conditional!
             * Remove the conditional statement */

            noway_assert(cond->gtOper == GT_CNS_INT);

            if (condTree != cond)
            {
                // Preserve any side effects
                assert(condTree->OperIs(GT_COMMA));
                lastStmt->SetRootNode(condTree);
            }
            else
            {
                // no side effects, remove the switch entirely
                fgRemoveStmt(block, lastStmt);
            }

            /* modify the flow graph */

            /* Find the actual jump target */
            unsigned switchVal;
            switchVal = (unsigned)cond->AsIntCon()->gtIconVal;
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
                printf("\nConditional folded at " FMT_BB "\n", block->bbNum);
                printf(FMT_BB " becomes a %s", block->bbNum,
                       block->bbJumpKind == BBJ_ALWAYS ? "BBJ_ALWAYS" : "BBJ_NONE");
                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    printf(" to " FMT_BB, block->bbJumpDest->bbNum);
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

//------------------------------------------------------------------------
// fgMorphBlockStmt: morph a single statement in a block.
//
// Arguments:
//    block - block containing the statement
//    stmt - statement to morph
//    msg - string to identify caller in a dump
//
// Returns:
//    true if 'stmt' was removed from the block.
//  s false if 'stmt' is still in the block (even if other statements were removed).
//
// Notes:
//   Can be called anytime, unlike fgMorphStmts() which should only be called once.
//
bool Compiler::fgMorphBlockStmt(BasicBlock* block, Statement* stmt DEBUGARG(const char* msg))
{
    assert(block != nullptr);
    assert(stmt != nullptr);

    // Reset some ambient state
    fgRemoveRestOfBlock = false;
    compCurBB           = block;
    compCurStmt         = stmt;

    GenTree* morph = fgMorphTree(stmt->GetRootNode());

    // Bug 1106830 - During the CSE phase we can't just remove
    // morph->AsOp()->gtOp2 as it could contain CSE expressions.
    // This leads to a noway_assert in OptCSE.cpp when
    // searching for the removed CSE ref. (using gtFindLink)
    //
    if (!optValnumCSE_phase)
    {
        // Check for morph as a GT_COMMA with an unconditional throw
        if (fgIsCommaThrow(morph, true))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Folding a top-level fgIsCommaThrow stmt\n");
                printf("Removing op2 as unreachable:\n");
                gtDispTree(morph->AsOp()->gtOp2);
                printf("\n");
            }
#endif
            // Use the call as the new stmt
            morph = morph->AsOp()->gtOp1;
            noway_assert(morph->gtOper == GT_CALL);
        }

        // we can get a throw as a statement root
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

    stmt->SetRootNode(morph);

    // Can the entire tree be removed?
    bool removedStmt = false;

    // Defer removing statements during CSE so we don't inadvertently remove any CSE defs.
    if (!optValnumCSE_phase)
    {
        removedStmt = fgCheckRemoveStmt(block, stmt);
    }

    // Or this is the last statement of a conditional branch that was just folded?
    if (!removedStmt && (stmt->GetNextStmt() == nullptr) && !fgRemoveRestOfBlock)
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
        // Have to re-do the evaluation order since for example some later code does not expect constants as op1
        gtSetStmtInfo(stmt);

        // Have to re-link the nodes for this statement
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
        // Remove the rest of the stmts in the block
        for (Statement* removeStmt : StatementList(stmt->GetNextStmt()))
        {
            fgRemoveStmt(block, removeStmt);
        }

        // The rest of block has been removed and we will always throw an exception.
        //
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
            printf("\n%s Block " FMT_BB " becomes a throw block.\n", msg, block->bbNum);
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

void Compiler::fgMorphStmts(BasicBlock* block, bool* lnot, bool* loadw)
{
    fgRemoveRestOfBlock = false;

    *lnot = *loadw = false;

    fgCurrentlyInUseArgTemps = hashBv::Create(this);

    for (Statement* stmt : block->Statements())
    {
        if (fgRemoveRestOfBlock)
        {
            fgRemoveStmt(block, stmt);
            continue;
        }
#ifdef FEATURE_SIMD
        if (opts.OptimizationEnabled() && stmt->GetRootNode()->TypeGet() == TYP_FLOAT &&
            stmt->GetRootNode()->OperGet() == GT_ASG)
        {
            fgMorphCombineSIMDFieldAssignments(block, stmt);
        }
#endif

        fgMorphStmt      = stmt;
        compCurStmt      = stmt;
        GenTree* oldTree = stmt->GetRootNode();

#ifdef DEBUG

        unsigned oldHash = verbose ? gtHashValue(oldTree) : DUMMY_INIT(~0);

        if (verbose)
        {
            printf("\nfgMorphTree " FMT_BB ", " FMT_STMT " (before)\n", block->bbNum, stmt->GetID());
            gtDispTree(oldTree);
        }
#endif

        /* Morph this statement tree */

        GenTree* morphedTree = fgMorphTree(oldTree);

        // mark any outgoing arg temps as free so we can reuse them in the next statement.

        fgCurrentlyInUseArgTemps->ZeroAll();

        // Has fgMorphStmt been sneakily changed ?

        if ((stmt->GetRootNode() != oldTree) || (block != compCurBB))
        {
            if (stmt->GetRootNode() != oldTree)
            {
                /* This must be tailcall. Ignore 'morphedTree' and carry on with
                the tail-call node */

                morphedTree = stmt->GetRootNode();
            }
            else
            {
                /* This must be a tailcall that caused a GCPoll to get
                injected. We haven't actually morphed the call yet
                but the flag still got set, clear it here...  */
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                morphedTree->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif
            }

            noway_assert(compTailCallUsed);
            noway_assert(morphedTree->gtOper == GT_CALL);
            GenTreeCall* call = morphedTree->AsCall();
            // Could be
            //   - a fast call made as jmp in which case block will be ending with
            //   BBJ_RETURN (as we need epilog) and marked as containing a jmp.
            //   - a tailcall dispatched via JIT helper, on x86, in which case
            //   block will be ending with BBJ_THROW.
            //   - a tail call dispatched via runtime help (IL stubs), in which
            //   case there will not be any tailcall and the block will be ending
            //   with BBJ_RETURN (as normal control flow)
            noway_assert((call->IsFastTailCall() && (compCurBB->bbJumpKind == BBJ_RETURN) &&
                          ((compCurBB->bbFlags & BBF_HAS_JMP)) != 0) ||
                         (call->IsTailCallViaJitHelper() && (compCurBB->bbJumpKind == BBJ_THROW)) ||
                         (!call->IsTailCall() && (compCurBB->bbJumpKind == BBJ_RETURN)));
        }

#ifdef DEBUG
        if (compStressCompile(STRESS_CLONE_EXPR, 30))
        {
            // Clone all the trees to stress gtCloneExpr()

            if (verbose)
            {
                printf("\nfgMorphTree (stressClone from):\n");
                gtDispTree(morphedTree);
            }

            morphedTree = gtCloneExpr(morphedTree);
            noway_assert(morphedTree != nullptr);

            if (verbose)
            {
                printf("\nfgMorphTree (stressClone to):\n");
                gtDispTree(morphedTree);
            }
        }

        /* If the hash value changes. we modified the tree during morphing */
        if (verbose)
        {
            unsigned newHash = gtHashValue(morphedTree);
            if (newHash != oldHash)
            {
                printf("\nfgMorphTree " FMT_BB ", " FMT_STMT " (after)\n", block->bbNum, stmt->GetID());
                gtDispTree(morphedTree);
            }
        }
#endif

        /* Check for morphedTree as a GT_COMMA with an unconditional throw */
        if (!gtIsActiveCSE_Candidate(morphedTree) && fgIsCommaThrow(morphedTree, true))
        {
            /* Use the call as the new stmt */
            morphedTree = morphedTree->AsOp()->gtOp1;
            noway_assert(morphedTree->gtOper == GT_CALL);
            noway_assert((morphedTree->gtFlags & GTF_COLON_COND) == 0);

            fgRemoveRestOfBlock = true;
        }

        stmt->SetRootNode(morphedTree);

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
    }

    if (fgRemoveRestOfBlock)
    {
        if ((block->bbJumpKind == BBJ_COND) || (block->bbJumpKind == BBJ_SWITCH))
        {
            Statement* first = block->firstStmt();
            noway_assert(first);
            Statement* lastStmt = block->lastStmt();
            noway_assert(lastStmt && lastStmt->GetNextStmt() == nullptr);
            GenTree* last = lastStmt->GetRootNode();

            if (((block->bbJumpKind == BBJ_COND) && (last->gtOper == GT_JTRUE)) ||
                ((block->bbJumpKind == BBJ_SWITCH) && (last->gtOper == GT_SWITCH)))
            {
                GenTree* op1 = last->AsOp()->gtOp1;

                if (op1->OperKind() & GTK_RELOP)
                {
                    /* Unmark the comparison node with GTF_RELOP_JMP_USED */
                    op1->gtFlags &= ~GTF_RELOP_JMP_USED;
                }

                lastStmt->SetRootNode(fgMorphTree(op1));
            }
        }

        /* Mark block as a BBJ_THROW block */
        fgConvertBBToThrowBB(block);
    }

#if FEATURE_FASTTAILCALL
    GenTree* recursiveTailCall = nullptr;
    if (block->endsWithTailCallConvertibleToLoop(this, &recursiveTailCall))
    {
        fgMorphRecursiveFastTailCallIntoLoop(block, recursiveTailCall->AsCall());
    }
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
    optLocalAssertionProp = opts.OptimizationEnabled();

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

    do
    {
#if OPT_BOOL_OPS
        bool lnot = false;
#endif

        bool loadw = false;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nMorphing " FMT_BB " of '%s'\n", block->bbNum, info.compFullName);
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
        // Make the current basic block address available globally.
        compCurBB = block;

        // Process all statement trees in the basic block.
        fgMorphStmts(block, &lnot, &loadw);

        // Do we need to merge the result of this block into a single return block?
        if ((block->bbJumpKind == BBJ_RETURN) && ((block->bbFlags & BBF_HAS_JMP) == 0))
        {
            if ((genReturnBB != nullptr) && (genReturnBB != block))
            {
                fgMergeBlockReturn(block);
            }
        }

        block = block->bbNext;
    } while (block != nullptr);

    // We are done with the global morphing phase
    fgGlobalMorph = false;
    compCurBB     = nullptr;

    // Under OSR, we no longer need to specially protect the original method entry
    //
    if (opts.IsOSR() && (fgEntryBB != nullptr) && (fgEntryBB->bbFlags & BBF_IMPORTED))
    {
        JITDUMP("OSR: un-protecting original method entry " FMT_BB "\n", fgEntryBB->bbNum);
        assert(fgEntryBB->bbRefs > 0);
        fgEntryBB->bbRefs--;
        // We don't need to remember this block anymore.
        fgEntryBB = nullptr;
    }

#ifdef DEBUG
    if (verboseTrees)
    {
        fgDispBasicBlocks(true);
    }
#endif
}

//------------------------------------------------------------------------
// fgMergeBlockReturn: assign the block return value (if any) into the single return temp
//   and branch to the single return block.
//
// Arguments:
//   block - the block to process.
//
// Notes:
//   A block is not guaranteed to have a last stmt if its jump kind is BBJ_RETURN.
//   For example a method returning void could have an empty block with jump kind BBJ_RETURN.
//   Such blocks do materialize as part of in-lining.
//
//   A block with jump kind BBJ_RETURN does not necessarily need to end with GT_RETURN.
//   It could end with a tail call or rejected tail call or monitor.exit or a GT_INTRINSIC.
//   For now it is safe to explicitly check whether last stmt is GT_RETURN if genReturnLocal
//   is BAD_VAR_NUM.
//
void Compiler::fgMergeBlockReturn(BasicBlock* block)
{
    assert((block->bbJumpKind == BBJ_RETURN) && ((block->bbFlags & BBF_HAS_JMP) == 0));
    assert((genReturnBB != nullptr) && (genReturnBB != block));

    // TODO: Need to characterize the last top level stmt of a block ending with BBJ_RETURN.

    Statement* lastStmt = block->lastStmt();
    GenTree*   ret      = (lastStmt != nullptr) ? lastStmt->GetRootNode() : nullptr;

    if ((ret != nullptr) && (ret->OperGet() == GT_RETURN) && ((ret->gtFlags & GTF_RET_MERGED) != 0))
    {
        // This return was generated during epilog merging, so leave it alone
    }
    else
    {
        // We'll jump to the genReturnBB.
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(TARGET_X86)
        if (info.compFlags & CORINFO_FLG_SYNCH)
        {
            fgConvertSyncReturnToLeave(block);
        }
        else
#endif // !TARGET_X86
        {
            block->bbJumpKind = BBJ_ALWAYS;
            block->bbJumpDest = genReturnBB;
            fgAddRefPred(genReturnBB, block);
            fgReturnCount--;
        }
        if (genReturnLocal != BAD_VAR_NUM)
        {
            // replace the GT_RETURN node to be a GT_ASG that stores the return value into genReturnLocal.

            // Method must be returning a value other than TYP_VOID.
            noway_assert(compMethodHasRetVal());

            // This block must be ending with a GT_RETURN
            noway_assert(lastStmt != nullptr);
            noway_assert(lastStmt->GetNextStmt() == nullptr);
            noway_assert(ret != nullptr);

            // GT_RETURN must have non-null operand as the method is returning the value assigned to
            // genReturnLocal
            noway_assert(ret->OperGet() == GT_RETURN);
            noway_assert(ret->gtGetOp1() != nullptr);

            Statement* pAfterStatement = lastStmt;
            IL_OFFSETX offset          = lastStmt->GetILOffsetX();
            GenTree*   tree = gtNewTempAssign(genReturnLocal, ret->gtGetOp1(), &pAfterStatement, offset, block);
            if (tree->OperIsCopyBlkOp())
            {
                tree = fgMorphCopyBlock(tree);
            }

            if (pAfterStatement == lastStmt)
            {
                lastStmt->SetRootNode(tree);
            }
            else
            {
                // gtNewTempAssign inserted additional statements after last
                fgRemoveStmt(block, lastStmt);
                Statement* newStmt = gtNewStmt(tree, offset);
                fgInsertStmtAfter(block, pAfterStatement, newStmt);
                lastStmt = newStmt;
            }
        }
        else if (ret != nullptr && ret->OperGet() == GT_RETURN)
        {
            // This block ends with a GT_RETURN
            noway_assert(lastStmt != nullptr);
            noway_assert(lastStmt->GetNextStmt() == nullptr);

            // Must be a void GT_RETURN with null operand; delete it as this block branches to oneReturn
            // block
            noway_assert(ret->TypeGet() == TYP_VOID);
            noway_assert(ret->gtGetOp1() == nullptr);

            fgRemoveStmt(block, lastStmt);
        }
#ifdef DEBUG
        if (verbose)
        {
            printf("morph " FMT_BB " to point at onereturn.  New block is\n", block->bbNum);
            fgTableDispBasicBlock(block);
        }
#endif
    }
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
        SetInterruptible(true);
    }
#endif

    if (opts.compDbgCode)
    {
        assert(!codeGen->isGCTypeFixed());
        SetInterruptible(true); // debugging is easier this way ...
    }

    /* Assume we won't need an explicit stack frame if this is allowed */

    if (compLocallocUsed)
    {
        codeGen->setFramePointerRequired(true);
    }

#ifdef TARGET_X86

    if (compTailCallUsed)
        codeGen->setFramePointerRequired(true);

#endif // TARGET_X86

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

#ifdef TARGET_X86

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
    {
        codeGen->setFramePointerRequiredEH(true);
    }

#else // !TARGET_X86

    if (compHndBBtabCount > 0)
    {
        codeGen->setFramePointerRequiredEH(true);
    }

#endif // TARGET_X86

#ifdef UNIX_X86_ABI
    if (info.compXcptnsCount > 0)
    {
        assert(!codeGen->isGCTypeFixed());
        // Enforce fully interruptible codegen for funclet unwinding
        SetInterruptible(true);
    }
#endif // UNIX_X86_ABI

    if (compMethodRequiresPInvokeFrame())
    {
        codeGen->setFramePointerRequired(true); // Setup of Pinvoke frame currently requires an EBP style frame
    }

    if (info.compPublishStubParam)
    {
        codeGen->setFramePointerRequiredGCInfo(true);
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

    // printf("method will %s be fully interruptible\n", GetInterruptible() ? "   " : "not");
}

/*****************************************************************************/

GenTree* Compiler::fgInitThisClass()
{
    noway_assert(!compIsForInlining());

    CORINFO_LOOKUP_KIND kind;
    info.compCompHnd->getLocationOfThisType(info.compMethodHnd, &kind);

    if (!kind.needsRuntimeLookup)
    {
        return fgGetSharedCCtor(info.compClassHnd);
    }
    else
    {
#ifdef FEATURE_READYTORUN_COMPILER
        // Only CoreRT understands CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE. Don't do this on CoreCLR.
        if (opts.IsReadyToRun() && IsTargetAbi(CORINFO_CORERT_ABI))
        {
            CORINFO_RESOLVED_TOKEN resolvedToken;
            memset(&resolvedToken, 0, sizeof(resolvedToken));

            // We are in a shared method body, but maybe we don't need a runtime lookup after all.
            // This covers the case of a generic method on a non-generic type.
            if (!(info.compClassAttr & CORINFO_FLG_SHAREDINST))
            {
                resolvedToken.hClass = info.compClassHnd;
                return impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_STATIC_BASE, TYP_BYREF);
            }

            // We need a runtime lookup.
            GenTree* ctxTree = getRuntimeContextTree(kind.runtimeLookupKind);

            // CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE with a zeroed out resolvedToken means "get the static
            // base of the class that owns the method being compiled". If we're in this method, it means we're not
            // inlining and there's no ambiguity.
            return impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE, TYP_BYREF,
                                             gtNewCallArgs(ctxTree), &kind);
        }
#endif

        // Collectible types requires that for shared generic code, if we use the generic context paramter
        // that we report it. (This is a conservative approach, we could detect some cases particularly when the
        // context parameter is this that we don't need the eager reporting logic.)
        lvaGenericsContextInUse = true;

        switch (kind.runtimeLookupKind)
        {
            case CORINFO_LOOKUP_THISOBJ:
            {
                // This code takes a this pointer; but we need to pass the static method desc to get the right point in
                // the hierarchy
                GenTree* vtTree = gtNewLclvNode(info.compThisArg, TYP_REF);
                vtTree->gtFlags |= GTF_VAR_CONTEXT;
                // Vtable pointer of this object
                vtTree = gtNewOperNode(GT_IND, TYP_I_IMPL, vtTree);
                vtTree->gtFlags |= GTF_EXCEPT; // Null-pointer exception
                GenTree* methodHnd = gtNewIconEmbMethHndNode(info.compMethodHnd);

                return gtNewHelperCallNode(CORINFO_HELP_INITINSTCLASS, TYP_VOID, gtNewCallArgs(vtTree, methodHnd));
            }

            case CORINFO_LOOKUP_CLASSPARAM:
            {
                GenTree* vtTree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                vtTree->gtFlags |= GTF_VAR_CONTEXT;
                return gtNewHelperCallNode(CORINFO_HELP_INITCLASS, TYP_VOID, gtNewCallArgs(vtTree));
            }

            case CORINFO_LOOKUP_METHODPARAM:
            {
                GenTree* methHndTree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                methHndTree->gtFlags |= GTF_VAR_CONTEXT;
                return gtNewHelperCallNode(CORINFO_HELP_INITINSTCLASS, TYP_VOID,
                                           gtNewCallArgs(gtNewIconNode(0), methHndTree));
            }

            default:
                noway_assert(!"Unknown LOOKUP_KIND");
                UNREACHABLE();
        }
    }
}

#ifdef DEBUG
/*****************************************************************************
 *
 *  Tree walk callback to make sure no GT_QMARK nodes are present in the tree,
 *  except for the allowed ? 1 : 0; pattern.
 */
Compiler::fgWalkResult Compiler::fgAssertNoQmark(GenTree** tree, fgWalkData* data)
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
    assert(!"Qmarks beyond morph disallowed.");
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
void Compiler::fgPreExpandQmarkChecks(GenTree* expr)
{
    GenTree* topQmark = fgGetTopLevelQmark(expr);

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
        fgWalkTreePre(&topQmark->AsOp()->gtOp1, Compiler::fgAssertNoQmark, nullptr);

        fgPreExpandQmarkChecks(topQmark->AsOp()->gtOp2->AsOp()->gtOp1);
        fgPreExpandQmarkChecks(topQmark->AsOp()->gtOp2->AsOp()->gtOp2);
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
GenTree* Compiler::fgGetTopLevelQmark(GenTree* expr, GenTree** ppDst /* = NULL */)
{
    if (ppDst != nullptr)
    {
        *ppDst = nullptr;
    }

    GenTree* topQmark = nullptr;
    if (expr->gtOper == GT_QMARK)
    {
        topQmark = expr;
    }
    else if (expr->gtOper == GT_ASG && expr->AsOp()->gtOp2->gtOper == GT_QMARK &&
             expr->AsOp()->gtOp1->gtOper == GT_LCL_VAR)
    {
        topQmark = expr->AsOp()->gtOp2;
        if (ppDst != nullptr)
        {
            *ppDst = expr->AsOp()->gtOp1;
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
void Compiler::fgExpandQmarkForCastInstOf(BasicBlock* block, Statement* stmt)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding CastInstOf qmark in " FMT_BB " (before)\n", block->bbNum);
        fgDispBasicBlocks(block, block, true);
    }
#endif // DEBUG

    GenTree* expr = stmt->GetRootNode();

    GenTree* dst   = nullptr;
    GenTree* qmark = fgGetTopLevelQmark(expr, &dst);
    noway_assert(dst != nullptr);

    assert(qmark->gtFlags & GTF_QMARK_CAST_INSTOF);

    // Get cond, true, false exprs for the qmark.
    GenTree* condExpr  = qmark->gtGetOp1();
    GenTree* trueExpr  = qmark->gtGetOp2()->AsColon()->ThenNode();
    GenTree* falseExpr = qmark->gtGetOp2()->AsColon()->ElseNode();

    // Get cond, true, false exprs for the nested qmark.
    GenTree* nestedQmark = falseExpr;
    GenTree* cond2Expr;
    GenTree* true2Expr;
    GenTree* false2Expr;

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
    GenTree*   jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, condExpr);
    Statement* jmpStmt = fgNewStmtFromTree(jmpTree, stmt->GetILOffsetX());
    fgInsertStmtAtEnd(cond1Block, jmpStmt);

    // Append cond2 as JTRUE to cond2Block
    jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, cond2Expr);
    jmpStmt = fgNewStmtFromTree(jmpTree, stmt->GetILOffsetX());
    fgInsertStmtAtEnd(cond2Block, jmpStmt);

    // AsgBlock should get tmp = op1 assignment.
    trueExpr            = gtNewTempAssign(dst->AsLclVarCommon()->GetLclNum(), trueExpr);
    Statement* trueStmt = fgNewStmtFromTree(trueExpr, stmt->GetILOffsetX());
    fgInsertStmtAtEnd(asgBlock, trueStmt);

    // Since we are adding helper in the JTRUE false path, reverse the cond2 and add the helper.
    gtReverseCond(cond2Expr);
    GenTree*   helperExpr = gtNewTempAssign(dst->AsLclVarCommon()->GetLclNum(), true2Expr);
    Statement* helperStmt = fgNewStmtFromTree(helperExpr, stmt->GetILOffsetX());
    fgInsertStmtAtEnd(helperBlock, helperStmt);

    // Finally remove the nested qmark stmt.
    fgRemoveStmt(block, stmt);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding CastInstOf qmark in " FMT_BB " (after)\n", block->bbNum);
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
void Compiler::fgExpandQmarkStmt(BasicBlock* block, Statement* stmt)
{
    GenTree* expr = stmt->GetRootNode();

    // Retrieve the Qmark node to be expanded.
    GenTree* dst   = nullptr;
    GenTree* qmark = fgGetTopLevelQmark(expr, &dst);
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
        printf("\nExpanding top-level qmark in " FMT_BB " (before)\n", block->bbNum);
        fgDispBasicBlocks(block, block, true);
    }
#endif // DEBUG

    // Retrieve the operands.
    GenTree* condExpr  = qmark->gtGetOp1();
    GenTree* trueExpr  = qmark->gtGetOp2()->AsColon()->ThenNode();
    GenTree* falseExpr = qmark->gtGetOp2()->AsColon()->ElseNode();

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

    GenTree*   jmpTree = gtNewOperNode(GT_JTRUE, TYP_VOID, qmark->gtGetOp1());
    Statement* jmpStmt = fgNewStmtFromTree(jmpTree, stmt->GetILOffsetX());
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
        lclNum = dst->AsLclVar()->GetLclNum();
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
        Statement* trueStmt = fgNewStmtFromTree(trueExpr, stmt->GetILOffsetX());
        fgInsertStmtAtEnd(thenBlock, trueStmt);
    }

    // Assign the falseExpr into the dst or tmp, insert in elseBlock
    if (hasFalseExpr)
    {
        if (dst != nullptr)
        {
            falseExpr = gtNewTempAssign(lclNum, falseExpr);
        }
        Statement* falseStmt = fgNewStmtFromTree(falseExpr, stmt->GetILOffsetX());
        fgInsertStmtAtEnd(elseBlock, falseStmt);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nExpanding top-level qmark in " FMT_BB " (after)\n", block->bbNum);
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
        for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            for (Statement* stmt : block->Statements())
            {
                GenTree* expr = stmt->GetRootNode();
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
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (Statement* stmt : block->Statements())
        {
            GenTree* expr = stmt->GetRootNode();
            fgWalkTreePre(&expr, Compiler::fgAssertNoQmark, nullptr);
        }
    }
}
#endif

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
        JITDUMP("  promotion opt flag not enabled\n");
        return;
    }

    if (fgNoStructPromotion)
    {
        JITDUMP("  promotion disabled by JitNoStructPromotion\n");
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
        JITDUMP("  promotion disabled because of varargs\n");
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nlvaTable before fgPromoteStructs\n");
        lvaTableDump();
    }
#endif // DEBUG

    // The lvaTable might grow as we grab temps. Make a local copy here.
    unsigned startLvaCount = lvaCount;

    //
    // Loop through the original lvaTable. Looking for struct locals to be promoted.
    //
    lvaStructPromotionInfo structPromotionInfo;
    bool                   tooManyLocalsReported = false;

    for (unsigned lclNum = 0; lclNum < startLvaCount; lclNum++)
    {
        // Whether this var got promoted
        bool       promotedVar = false;
        LclVarDsc* varDsc      = &lvaTable[lclNum];

        // If we have marked this as lvUsedInSIMDIntrinsic, then we do not want to promote
        // its fields.  Instead, we will attempt to enregister the entire struct.
        if (varDsc->lvIsSIMDType() && (varDsc->lvIsUsedInSIMDIntrinsic() || isOpaqueSIMDLclVar(varDsc)))
        {
            varDsc->lvRegStruct = true;
        }
        // Don't promote if we have reached the tracking limit.
        else if (lvaHaveManyLocals())
        {
            // Print the message first time when we detected this condition
            if (!tooManyLocalsReported)
            {
                JITDUMP("Stopped promoting struct fields, due to too many locals.\n");
            }
            tooManyLocalsReported = true;
        }
        else if (varTypeIsStruct(varDsc))
        {
            assert(structPromotionHelper != nullptr);
            promotedVar = structPromotionHelper->TryPromoteStructVar(lclNum);
        }

        if (!promotedVar && varDsc->lvIsSIMDType() && !varDsc->lvFieldAccessed)
        {
            // Even if we have not used this in a SIMD intrinsic, if it is not being promoted,
            // we will treat it as a reg struct.
            varDsc->lvRegStruct = true;
        }
    }

#ifdef TARGET_ARM
    if (structPromotionHelper->GetRequiresScratchVar())
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
#endif // TARGET_ARM

#ifdef DEBUG
    if (verbose)
    {
        printf("\nlvaTable after fgPromoteStructs\n");
        lvaTableDump();
    }
#endif // DEBUG
}

void Compiler::fgMorphStructField(GenTree* tree, GenTree* parent)
{
    noway_assert(tree->OperGet() == GT_FIELD);

    GenTreeField* field  = tree->AsField();
    GenTree*      objRef = field->gtFldObj;
    GenTree*      obj    = ((objRef != nullptr) && (objRef->gtOper == GT_ADDR)) ? objRef->AsOp()->gtOp1 : nullptr;
    noway_assert((tree->gtFlags & GTF_GLOB_REF) || ((obj != nullptr) && (obj->gtOper == GT_LCL_VAR)));

    /* Is this an instance data member? */

    if ((obj != nullptr) && (obj->gtOper == GT_LCL_VAR))
    {
        unsigned         lclNum = obj->AsLclVarCommon()->GetLclNum();
        const LclVarDsc* varDsc = &lvaTable[lclNum];

        if (varTypeIsStruct(obj))
        {
            if (varDsc->lvPromoted)
            {
                // Promoted struct
                unsigned fldOffset     = field->gtFldOffset;
                unsigned fieldLclIndex = lvaGetFieldLocal(varDsc, fldOffset);

                if (fieldLclIndex == BAD_VAR_NUM)
                {
                    // Access a promoted struct's field with an offset that doesn't correspond to any field.
                    // It can happen if the struct was cast to another struct with different offsets.
                    return;
                }

                const LclVarDsc* fieldDsc  = &lvaTable[fieldLclIndex];
                var_types        fieldType = fieldDsc->TypeGet();

                assert(fieldType != TYP_STRUCT); // promoted LCL_VAR can't have a struct type.
                if (tree->TypeGet() != fieldType)
                {
                    if (tree->TypeGet() != TYP_STRUCT)
                    {
                        // This is going to be an incorrect instruction promotion.
                        // For example when we try to read int as long.
                        return;
                    }

                    if (field->gtFldHnd != fieldDsc->lvFieldHnd)
                    {
                        CORINFO_CLASS_HANDLE fieldTreeClass = nullptr, fieldDscClass = nullptr;

                        CorInfoType fieldTreeType = info.compCompHnd->getFieldType(field->gtFldHnd, &fieldTreeClass);
                        CorInfoType fieldDscType = info.compCompHnd->getFieldType(fieldDsc->lvFieldHnd, &fieldDscClass);
                        if (fieldTreeType != fieldDscType || fieldTreeClass != fieldDscClass)
                        {
                            // Access the promoted field with a different class handle, can't check that types match.
                            return;
                        }
                        // Access the promoted field as a field of a non-promoted struct with the same class handle.
                    }
#ifdef DEBUG
                    else if (tree->TypeGet() == TYP_STRUCT)
                    {
                        // The field tree accesses it as a struct, but the promoted lcl var for the field
                        // says that it has another type. It can happen only if struct promotion faked
                        // field type for a struct of single field of scalar type aligned at their natural boundary.
                        assert(structPromotionHelper != nullptr);
                        structPromotionHelper->CheckRetypedAsScalar(field->gtFldHnd, fieldType);
                    }
#endif // DEBUG
                }

                tree->SetOper(GT_LCL_VAR);
                tree->AsLclVarCommon()->SetLclNum(fieldLclIndex);
                tree->gtType = fieldType;
                tree->gtFlags &= GTF_NODE_MASK; // Note: that clears all flags except `GTF_COLON_COND`.

                if (parent->gtOper == GT_ASG)
                {
                    if (parent->AsOp()->gtOp1 == tree)
                    {
                        tree->gtFlags |= GTF_VAR_DEF;
                        tree->gtFlags |= GTF_DONT_CSE;
                    }

                    // Promotion of struct containing struct fields where the field
                    // is a struct with a single pointer sized scalar type field: in
                    // this case struct promotion uses the type  of the underlying
                    // scalar field as the type of struct field instead of recursively
                    // promoting. This can lead to a case where we have a block-asgn
                    // with its RHS replaced with a scalar type.  Mark RHS value as
                    // DONT_CSE so that assertion prop will not do const propagation.
                    // The reason this is required is that if RHS of a block-asg is a
                    // constant, then it is interpreted as init-block incorrectly.
                    //
                    // TODO - This can also be avoided if we implement recursive struct
                    // promotion, tracked by #10019.
                    if (varTypeIsStruct(parent) && parent->AsOp()->gtOp2 == tree && !varTypeIsStruct(tree))
                    {
                        tree->gtFlags |= GTF_DONT_CSE;
                    }
                }
#ifdef DEBUG
                if (verbose)
                {
                    printf("Replacing the field in promoted struct with local var V%02u\n", fieldLclIndex);
                }
#endif // DEBUG
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
                tree->AsLclVarCommon()->SetLclNum(lclNum);
                tree->gtFlags &= GTF_NODE_MASK;

                if ((parent->gtOper == GT_ASG) && (parent->AsOp()->gtOp1 == tree))
                {
                    tree->gtFlags |= GTF_VAR_DEF;
                    tree->gtFlags |= GTF_DONT_CSE;
                }
#ifdef DEBUG
                if (verbose)
                {
                    printf("Replacing the field in normed struct with local var V%02u\n", lclNum);
                }
#endif // DEBUG
            }
        }
    }
}

void Compiler::fgMorphLocalField(GenTree* tree, GenTree* parent)
{
    noway_assert(tree->OperGet() == GT_LCL_FLD);

    unsigned   lclNum = tree->AsLclFld()->GetLclNum();
    LclVarDsc* varDsc = &lvaTable[lclNum];

    if (varTypeIsStruct(varDsc))
    {
        if (varDsc->lvPromoted)
        {
            // Promoted struct
            unsigned   fldOffset     = tree->AsLclFld()->GetLclOffs();
            unsigned   fieldLclIndex = 0;
            LclVarDsc* fldVarDsc     = nullptr;

            if (fldOffset != BAD_VAR_NUM)
            {
                fieldLclIndex = lvaGetFieldLocal(varDsc, fldOffset);
                noway_assert(fieldLclIndex != BAD_VAR_NUM);
                fldVarDsc = &lvaTable[fieldLclIndex];
            }

            var_types treeType  = tree->TypeGet();
            var_types fieldType = fldVarDsc->TypeGet();
            if (fldOffset != BAD_VAR_NUM &&
                ((genTypeSize(fieldType) == genTypeSize(treeType)) || (varDsc->lvFieldCnt == 1)))
            {
                // There is an existing sub-field we can use.
                tree->AsLclFld()->SetLclNum(fieldLclIndex);

                // The field must be an enregisterable type; otherwise it would not be a promoted field.
                // The tree type may not match, e.g. for return types that have been morphed, but both
                // must be enregisterable types.
                assert(varTypeIsEnregisterable(treeType) && varTypeIsEnregisterable(fieldType));

                tree->ChangeOper(GT_LCL_VAR);
                assert(tree->AsLclVarCommon()->GetLclNum() == fieldLclIndex);
                tree->gtType = fldVarDsc->TypeGet();

                if ((parent->gtOper == GT_ASG) && (parent->AsOp()->gtOp1 == tree))
                {
                    tree->gtFlags |= GTF_VAR_DEF;
                    tree->gtFlags |= GTF_DONT_CSE;
                }
                JITDUMP("Replacing the GT_LCL_FLD in promoted struct with local var V%02u\n", fieldLclIndex);
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
        }
        else if (varTypeIsSIMD(varDsc) && (genTypeSize(tree->TypeGet()) == genTypeSize(varDsc)))
        {
            assert(tree->AsLclFld()->GetLclOffs() == 0);
            tree->gtType = varDsc->TypeGet();
            tree->ChangeOper(GT_LCL_VAR);
            JITDUMP("Replacing GT_LCL_FLD of struct with local var V%02u\n", lclNum);
        }
    }
}

//------------------------------------------------------------------------
// fgResetImplicitByRefRefCount: Clear the ref count field of all implicit byrefs

void Compiler::fgResetImplicitByRefRefCount()
{
#if (defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)) || defined(TARGET_ARM64)
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgResetImplicitByRefRefCount()\n");
    }
#endif // DEBUG

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; ++lclNum)
    {
        LclVarDsc* varDsc = lvaGetDesc(lclNum);

        if (varDsc->lvIsImplicitByRef)
        {
            // Clear the ref count field; fgMarkAddressTakenLocals will increment it per
            // appearance of implicit-by-ref param so that call arg morphing can do an
            // optimization for single-use implicit-by-ref params whose single use is as
            // an outgoing call argument.
            varDsc->setLvRefCnt(0, RCS_EARLY);
        }
    }

#endif // (TARGET_AMD64 && !UNIX_AMD64_ABI) || TARGET_ARM64
}

//------------------------------------------------------------------------
// fgRetypeImplicitByRefArgs: Update the types on implicit byref parameters' `LclVarDsc`s (from
//                            struct to pointer).  Also choose (based on address-exposed analysis)
//                            which struct promotions of implicit byrefs to keep or discard.
//                            For those which are kept, insert the appropriate initialization code.
//                            For those which are to be discarded, annotate the promoted field locals
//                            so that fgMorphImplicitByRefArgs will know to rewrite their appearances
//                            using indirections off the pointer parameters.

void Compiler::fgRetypeImplicitByRefArgs()
{
#if (defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)) || defined(TARGET_ARM64)
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgRetypeImplicitByRefArgs()\n");
    }
#endif // DEBUG

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        LclVarDsc* varDsc = &lvaTable[lclNum];

        if (lvaIsImplicitByRefLocal(lclNum))
        {
            unsigned size;

            if (varDsc->lvSize() > REGSIZE_BYTES)
            {
                size = varDsc->lvSize();
            }
            else
            {
                CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
                size                         = info.compCompHnd->getClassSize(typeHnd);
            }

            if (varDsc->lvPromoted)
            {
                // This implicit-by-ref was promoted; create a new temp to represent the
                // promoted struct before rewriting this parameter as a pointer.
                unsigned newLclNum = lvaGrabTemp(false DEBUGARG("Promoted implicit byref"));
                lvaSetStruct(newLclNum, lvaGetStruct(lclNum), true);
                if (info.compIsVarArgs)
                {
                    lvaSetStructUsedAsVarArg(newLclNum);
                }

                // Update varDsc since lvaGrabTemp might have re-allocated the var dsc array.
                varDsc = &lvaTable[lclNum];

                // Copy the struct promotion annotations to the new temp.
                LclVarDsc* newVarDsc       = &lvaTable[newLclNum];
                newVarDsc->lvPromoted      = true;
                newVarDsc->lvFieldLclStart = varDsc->lvFieldLclStart;
                newVarDsc->lvFieldCnt      = varDsc->lvFieldCnt;
                newVarDsc->lvContainsHoles = varDsc->lvContainsHoles;
                newVarDsc->lvCustomLayout  = varDsc->lvCustomLayout;
#ifdef DEBUG
                newVarDsc->lvKeepType = true;
#endif // DEBUG

                // Propagate address-taken-ness and do-not-enregister-ness.
                newVarDsc->lvAddrExposed     = varDsc->lvAddrExposed;
                newVarDsc->lvDoNotEnregister = varDsc->lvDoNotEnregister;
#ifdef DEBUG
                newVarDsc->lvLclBlockOpAddr   = varDsc->lvLclBlockOpAddr;
                newVarDsc->lvLclFieldExpr     = varDsc->lvLclFieldExpr;
                newVarDsc->lvVMNeedsStackAddr = varDsc->lvVMNeedsStackAddr;
                newVarDsc->lvLiveInOutOfHndlr = varDsc->lvLiveInOutOfHndlr;
                newVarDsc->lvLiveAcrossUCall  = varDsc->lvLiveAcrossUCall;
#endif // DEBUG

                // If the promotion is dependent, the promoted temp would just be committed
                // to memory anyway, so we'll rewrite its appearances to be indirections
                // through the pointer parameter, the same as we'd do for this
                // parameter if it weren't promoted at all (otherwise the initialization
                // of the new temp would just be a needless memcpy at method entry).
                //
                // Otherwise, see how many appearances there are. We keep two early ref counts: total
                // number of references to the struct or some field, and how many of these are
                // arguments to calls. We undo promotion unless we see enough non-call uses.
                //
                const unsigned totalAppearances = varDsc->lvRefCnt(RCS_EARLY);
                const unsigned callAppearances  = varDsc->lvRefCntWtd(RCS_EARLY);
                assert(totalAppearances >= callAppearances);
                const unsigned nonCallAppearances = totalAppearances - callAppearances;

                bool undoPromotion = ((lvaGetPromotionType(newVarDsc) == PROMOTION_TYPE_DEPENDENT) ||
                                      (nonCallAppearances <= varDsc->lvFieldCnt));

                JITDUMP("%s promotion of implicit by-ref V%02u: %s total: %u non-call: %u fields: %u\n",
                        undoPromotion ? "Undoing" : "Keeping", lclNum,
                        (lvaGetPromotionType(newVarDsc) == PROMOTION_TYPE_DEPENDENT) ? "dependent;" : "",
                        totalAppearances, nonCallAppearances, varDsc->lvFieldCnt);

                if (!undoPromotion)
                {
                    // Insert IR that initializes the temp from the parameter.
                    // LHS is a simple reference to the temp.
                    fgEnsureFirstBBisScratch();
                    GenTree* lhs = gtNewLclvNode(newLclNum, varDsc->lvType);
                    // RHS is an indirection (using GT_OBJ) off the parameter.
                    GenTree* addr   = gtNewLclvNode(lclNum, TYP_BYREF);
                    GenTree* rhs    = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, addr, typGetBlkLayout(size));
                    GenTree* assign = gtNewAssignNode(lhs, rhs);
                    fgNewStmtAtBeg(fgFirstBB, assign);
                }

                // Update the locals corresponding to the promoted fields.
                unsigned fieldLclStart = varDsc->lvFieldLclStart;
                unsigned fieldCount    = varDsc->lvFieldCnt;
                unsigned fieldLclStop  = fieldLclStart + fieldCount;

                for (unsigned fieldLclNum = fieldLclStart; fieldLclNum < fieldLclStop; ++fieldLclNum)
                {
                    LclVarDsc* fieldVarDsc = &lvaTable[fieldLclNum];

                    if (undoPromotion)
                    {
                        // Leave lvParentLcl pointing to the parameter so that fgMorphImplicitByRefArgs
                        // will know to rewrite appearances of this local.
                        assert(fieldVarDsc->lvParentLcl == lclNum);
                    }
                    else
                    {
                        // Set the new parent.
                        fieldVarDsc->lvParentLcl = newLclNum;
                        // Clear the ref count field; it is used to communicate the number of references
                        // to the implicit byref parameter when morphing calls that pass the implicit byref
                        // out as an outgoing argument value, but that doesn't pertain to this field local
                        // which is now a field of a non-arg local.
                        fieldVarDsc->setLvRefCnt(0, RCS_EARLY);
                    }

                    fieldVarDsc->lvIsParam = false;
                    // The fields shouldn't inherit any register preferences from
                    // the parameter which is really a pointer to the struct.
                    fieldVarDsc->lvIsRegArg      = false;
                    fieldVarDsc->lvIsMultiRegArg = false;
                    fieldVarDsc->SetArgReg(REG_NA);
#if FEATURE_MULTIREG_ARGS
                    fieldVarDsc->SetOtherArgReg(REG_NA);
#endif
                }

                // Hijack lvFieldLclStart to record the new temp number.
                // It will get fixed up in fgMarkDemotedImplicitByRefArgs.
                varDsc->lvFieldLclStart = newLclNum;
                // Go ahead and clear lvFieldCnt -- either we're promoting
                // a replacement temp or we're not promoting this arg, and
                // in either case the parameter is now a pointer that doesn't
                // have these fields.
                varDsc->lvFieldCnt = 0;

                // Hijack lvPromoted to communicate to fgMorphImplicitByRefArgs
                // whether references to the struct should be rewritten as
                // indirections off the pointer (not promoted) or references
                // to the new struct local (promoted).
                varDsc->lvPromoted = !undoPromotion;
            }
            else
            {
                // The "undo promotion" path above clears lvPromoted for args that struct
                // promotion wanted to promote but that aren't considered profitable to
                // rewrite.  It hijacks lvFieldLclStart to communicate to
                // fgMarkDemotedImplicitByRefArgs that it needs to clean up annotations left
                // on such args for fgMorphImplicitByRefArgs to consult in the interim.
                // Here we have an arg that was simply never promoted, so make sure it doesn't
                // have nonzero lvFieldLclStart, since that would confuse fgMorphImplicitByRefArgs
                // and fgMarkDemotedImplicitByRefArgs.
                assert(varDsc->lvFieldLclStart == 0);
            }

            // Since the parameter in this position is really a pointer, its type is TYP_BYREF.
            varDsc->lvType = TYP_BYREF;

            // Since this previously was a TYP_STRUCT and we have changed it to a TYP_BYREF
            // make sure that the following flag is not set as these will force SSA to
            // exclude tracking/enregistering these LclVars. (see SsaBuilder::IncludeInSsa)
            //
            varDsc->lvOverlappingFields = 0; // This flag could have been set, clear it.

            // The struct parameter may have had its address taken, but the pointer parameter
            // cannot -- any uses of the struct parameter's address are uses of the pointer
            // parameter's value, and there's no way for the MSIL to reference the pointer
            // parameter's address.  So clear the address-taken bit for the parameter.
            varDsc->lvAddrExposed     = 0;
            varDsc->lvDoNotEnregister = 0;

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
    }

#endif // (TARGET_AMD64 && !UNIX_AMD64_ABI) || TARGET_ARM64
}

//------------------------------------------------------------------------
// fgMarkDemotedImplicitByRefArgs: Clear annotations for any implicit byrefs that struct promotion
//                                 asked to promote.  Appearances of these have now been rewritten
//                                 (by fgMorphImplicitByRefArgs) using indirections from the pointer
//                                 parameter or references to the promotion temp, as appropriate.

void Compiler::fgMarkDemotedImplicitByRefArgs()
{
#if (defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)) || defined(TARGET_ARM64)

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        LclVarDsc* varDsc = &lvaTable[lclNum];

        if (lvaIsImplicitByRefLocal(lclNum))
        {
            if (varDsc->lvPromoted)
            {
                // The parameter is simply a pointer now, so clear lvPromoted.  It was left set
                // by fgRetypeImplicitByRefArgs to communicate to fgMorphImplicitByRefArgs that
                // appearances of this arg needed to be rewritten to a new promoted struct local.
                varDsc->lvPromoted = false;

                // Clear the lvFieldLclStart value that was set by fgRetypeImplicitByRefArgs
                // to tell fgMorphImplicitByRefArgs which local is the new promoted struct one.
                varDsc->lvFieldLclStart = 0;
            }
            else if (varDsc->lvFieldLclStart != 0)
            {
                // We created new temps to represent a promoted struct corresponding to this
                // parameter, but decided not to go through with the promotion and have
                // rewritten all uses as indirections off the pointer parameter.
                // We stashed the pointer to the new struct temp in lvFieldLclStart; make
                // note of that and clear the annotation.
                unsigned structLclNum   = varDsc->lvFieldLclStart;
                varDsc->lvFieldLclStart = 0;

                // Clear the arg's ref count; this was set during address-taken analysis so that
                // call morphing could identify single-use implicit byrefs; we're done with
                // that, and want it to be in its default state of zero when we go to set
                // real ref counts for all variables.
                varDsc->setLvRefCnt(0, RCS_EARLY);

                // The temp struct is now unused; set flags appropriately so that we
                // won't allocate space for it on the stack.
                LclVarDsc* structVarDsc = &lvaTable[structLclNum];
                structVarDsc->setLvRefCnt(0, RCS_EARLY);
                structVarDsc->lvAddrExposed = false;
#ifdef DEBUG
                structVarDsc->lvUnusedStruct = true;
#endif // DEBUG

                unsigned fieldLclStart = structVarDsc->lvFieldLclStart;
                unsigned fieldCount    = structVarDsc->lvFieldCnt;
                unsigned fieldLclStop  = fieldLclStart + fieldCount;

                for (unsigned fieldLclNum = fieldLclStart; fieldLclNum < fieldLclStop; ++fieldLclNum)
                {
                    // Fix the pointer to the parent local.
                    LclVarDsc* fieldVarDsc = &lvaTable[fieldLclNum];
                    assert(fieldVarDsc->lvParentLcl == lclNum);
                    fieldVarDsc->lvParentLcl = structLclNum;

                    // The field local is now unused; set flags appropriately so that
                    // we won't allocate stack space for it.
                    fieldVarDsc->setLvRefCnt(0, RCS_EARLY);
                    fieldVarDsc->lvAddrExposed = false;
                }
            }
        }
    }

#endif // (TARGET_AMD64 && !UNIX_AMD64_ABI) || TARGET_ARM64
}

/*****************************************************************************
 *
 *  Morph irregular parameters
 *    for x64 and ARM64 this means turning them into byrefs, adding extra indirs.
 */
bool Compiler::fgMorphImplicitByRefArgs(GenTree* tree)
{
#if (!defined(TARGET_AMD64) || defined(UNIX_AMD64_ABI)) && !defined(TARGET_ARM64)

    return false;

#else  // (TARGET_AMD64 && !UNIX_AMD64_ABI) || TARGET_ARM64

    bool changed = false;

    // Implicit byref morphing needs to know if the reference to the parameter is a
    // child of GT_ADDR or not, so this method looks one level down and does the
    // rewrite whenever a child is a reference to an implicit byref parameter.
    if (tree->gtOper == GT_ADDR)
    {
        if (tree->AsOp()->gtOp1->gtOper == GT_LCL_VAR)
        {
            GenTree* morphedTree = fgMorphImplicitByRefArgs(tree, true);
            changed              = (morphedTree != nullptr);
            assert(!changed || (morphedTree == tree));
        }
    }
    else
    {
        for (GenTree** pTree : tree->UseEdges())
        {
            GenTree* childTree = *pTree;
            if (childTree->gtOper == GT_LCL_VAR)
            {
                GenTree* newChildTree = fgMorphImplicitByRefArgs(childTree, false);
                if (newChildTree != nullptr)
                {
                    changed = true;
                    *pTree  = newChildTree;
                }
            }
        }
    }

    return changed;
#endif // (TARGET_AMD64 && !UNIX_AMD64_ABI) || TARGET_ARM64
}

GenTree* Compiler::fgMorphImplicitByRefArgs(GenTree* tree, bool isAddr)
{
    assert((tree->gtOper == GT_LCL_VAR) || ((tree->gtOper == GT_ADDR) && (tree->AsOp()->gtOp1->gtOper == GT_LCL_VAR)));
    assert(isAddr == (tree->gtOper == GT_ADDR));

    GenTree*   lclVarTree = isAddr ? tree->AsOp()->gtOp1 : tree;
    unsigned   lclNum     = lclVarTree->AsLclVarCommon()->GetLclNum();
    LclVarDsc* lclVarDsc  = &lvaTable[lclNum];

    CORINFO_FIELD_HANDLE fieldHnd;
    unsigned             fieldOffset  = 0;
    var_types            fieldRefType = TYP_UNKNOWN;

    if (lvaIsImplicitByRefLocal(lclNum))
    {
        // The SIMD transformation to coalesce contiguous references to SIMD vector fields will
        // re-invoke the traversal to mark address-taken locals.
        // So, we may encounter a tree that has already been transformed to TYP_BYREF.
        // If we do, leave it as-is.
        if (!varTypeIsStruct(lclVarTree))
        {
            assert(lclVarTree->TypeGet() == TYP_BYREF);

            return nullptr;
        }
        else if (lclVarDsc->lvPromoted)
        {
            // fgRetypeImplicitByRefArgs created a new promoted struct local to represent this
            // arg.  Rewrite this to refer to the new local.
            assert(lclVarDsc->lvFieldLclStart != 0);
            lclVarTree->AsLclVarCommon()->SetLclNum(lclVarDsc->lvFieldLclStart);
            return tree;
        }

        fieldHnd = nullptr;
    }
    else if (lclVarDsc->lvIsStructField && lvaIsImplicitByRefLocal(lclVarDsc->lvParentLcl))
    {
        // This was a field reference to an implicit-by-reference struct parameter that was
        // dependently promoted; update it to a field reference off the pointer.
        // Grab the field handle from the struct field lclVar.
        fieldHnd    = lclVarDsc->lvFieldHnd;
        fieldOffset = lclVarDsc->lvFldOffset;
        assert(fieldHnd != nullptr);
        // Update lclNum/lclVarDsc to refer to the parameter
        lclNum       = lclVarDsc->lvParentLcl;
        lclVarDsc    = &lvaTable[lclNum];
        fieldRefType = lclVarTree->TypeGet();
    }
    else
    {
        // We only need to tranform the 'marked' implicit by ref parameters
        return nullptr;
    }

    // This is no longer a def of the lclVar, even if it WAS a def of the struct.
    lclVarTree->gtFlags &= ~(GTF_LIVENESS_MASK);

    if (isAddr)
    {
        if (fieldHnd == nullptr)
        {
            // change &X into just plain X
            tree->ReplaceWith(lclVarTree, this);
            tree->gtType = TYP_BYREF;
        }
        else
        {
            // change &(X.f) [i.e. GT_ADDR of local for promoted arg field]
            // into &(X, f) [i.e. GT_ADDR of GT_FIELD off ptr param]
            lclVarTree->AsLclVarCommon()->SetLclNum(lclNum);
            lclVarTree->gtType  = TYP_BYREF;
            tree->AsOp()->gtOp1 = gtNewFieldRef(fieldRefType, fieldHnd, lclVarTree, fieldOffset);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Replacing address of implicit by ref struct parameter with byref:\n");
        }
#endif // DEBUG
    }
    else
    {
        // Change X into OBJ(X) or FIELD(X, f)
        var_types structType = tree->gtType;
        tree->gtType         = TYP_BYREF;

        if (fieldHnd)
        {
            tree->AsLclVarCommon()->SetLclNum(lclNum);
            tree = gtNewFieldRef(fieldRefType, fieldHnd, tree, fieldOffset);
        }
        else
        {
            tree = gtNewObjNode(lclVarDsc->lvVerTypeInfo.GetClassHandle(), tree);

            if (structType == TYP_STRUCT)
            {
                gtSetObjGcInfo(tree->AsObj());
            }
        }

        // TODO-CQ: If the VM ever stops violating the ABI and passing heap references
        // we could remove TGTANYWHERE
        tree->gtFlags = ((tree->gtFlags & GTF_COMMON_MASK) | GTF_IND_TGTANYWHERE);

#ifdef DEBUG
        if (verbose)
        {
            printf("Replacing value of implicit by ref struct parameter with indir of parameter:\n");
        }
#endif // DEBUG
    }

#ifdef DEBUG
    if (verbose)
    {
        gtDispTree(tree);
    }
#endif // DEBUG

    return tree;
}

//------------------------------------------------------------------------
// fgAddFieldSeqForZeroOffset:
//    Associate a fieldSeq (with a zero offset) with the GenTree node 'addr'
//
// Arguments:
//    addr - A GenTree node
//    fieldSeqZero - a fieldSeq (with a zero offset)
//
// Notes:
//    Some GenTree nodes have internal fields that record the field sequence.
//    If we have one of these nodes: GT_CNS_INT, GT_LCL_FLD
//    we can append the field sequence using the gtFieldSeq
//    If we have a GT_ADD of a GT_CNS_INT we can use the
//    fieldSeq from child node.
//    Otherwise we record 'fieldSeqZero' in the GenTree node using
//    a Map:  GetFieldSeqStore()
//    When doing so we take care to preserve any existing zero field sequence
//
void Compiler::fgAddFieldSeqForZeroOffset(GenTree* addr, FieldSeqNode* fieldSeqZero)
{
    // We expect 'addr' to be an address at this point.
    assert(addr->TypeGet() == TYP_BYREF || addr->TypeGet() == TYP_I_IMPL || addr->TypeGet() == TYP_REF);

    // Tunnel through any commas.
    const bool commaOnly = true;
    addr                 = addr->gtEffectiveVal(commaOnly);

    // We still expect 'addr' to be an address at this point.
    assert(addr->TypeGet() == TYP_BYREF || addr->TypeGet() == TYP_I_IMPL || addr->TypeGet() == TYP_REF);

    FieldSeqNode* fieldSeqUpdate   = fieldSeqZero;
    GenTree*      fieldSeqNode     = addr;
    bool          fieldSeqRecorded = false;
    bool          isMapAnnotation  = false;

#ifdef DEBUG
    if (verbose)
    {
        printf("\nfgAddFieldSeqForZeroOffset for");
        gtDispFieldSeq(fieldSeqZero);

        printf("\naddr (Before)\n");
        gtDispNode(addr, nullptr, nullptr, false);
        gtDispCommonEndLine(addr);
    }
#endif // DEBUG

    switch (addr->OperGet())
    {
        case GT_CNS_INT:
            fieldSeqUpdate               = GetFieldSeqStore()->Append(addr->AsIntCon()->gtFieldSeq, fieldSeqZero);
            addr->AsIntCon()->gtFieldSeq = fieldSeqUpdate;
            fieldSeqRecorded             = true;
            break;

        case GT_LCL_FLD:
        {
            GenTreeLclFld* lclFld = addr->AsLclFld();
            fieldSeqUpdate        = GetFieldSeqStore()->Append(lclFld->GetFieldSeq(), fieldSeqZero);
            lclFld->SetFieldSeq(fieldSeqUpdate);
            fieldSeqRecorded = true;
            break;
        }

        case GT_ADDR:
            if (addr->AsOp()->gtOp1->OperGet() == GT_LCL_FLD)
            {
                fieldSeqNode = addr->AsOp()->gtOp1;

                GenTreeLclFld* lclFld = addr->AsOp()->gtOp1->AsLclFld();
                fieldSeqUpdate        = GetFieldSeqStore()->Append(lclFld->GetFieldSeq(), fieldSeqZero);
                lclFld->SetFieldSeq(fieldSeqUpdate);
                fieldSeqRecorded = true;
            }
            break;

        case GT_ADD:
            if (addr->AsOp()->gtOp1->OperGet() == GT_CNS_INT)
            {
                fieldSeqNode = addr->AsOp()->gtOp1;

                fieldSeqUpdate = GetFieldSeqStore()->Append(addr->AsOp()->gtOp1->AsIntCon()->gtFieldSeq, fieldSeqZero);
                addr->AsOp()->gtOp1->AsIntCon()->gtFieldSeq = fieldSeqUpdate;
                fieldSeqRecorded                            = true;
            }
            else if (addr->AsOp()->gtOp2->OperGet() == GT_CNS_INT)
            {
                fieldSeqNode = addr->AsOp()->gtOp2;

                fieldSeqUpdate = GetFieldSeqStore()->Append(addr->AsOp()->gtOp2->AsIntCon()->gtFieldSeq, fieldSeqZero);
                addr->AsOp()->gtOp2->AsIntCon()->gtFieldSeq = fieldSeqUpdate;
                fieldSeqRecorded                            = true;
            }
            break;

        default:
            break;
    }

    if (fieldSeqRecorded == false)
    {
        // Record in the general zero-offset map.

        // The "addr" node might already be annotated with a zero-offset field sequence.
        FieldSeqNode* existingFieldSeq = nullptr;
        if (GetZeroOffsetFieldMap()->Lookup(addr, &existingFieldSeq))
        {
            // Append the zero field sequences
            fieldSeqUpdate = GetFieldSeqStore()->Append(existingFieldSeq, fieldSeqZero);
        }
        // Overwrite the field sequence annotation for op1
        GetZeroOffsetFieldMap()->Set(addr, fieldSeqUpdate, NodeToFieldSeqMap::Overwrite);
        fieldSeqRecorded = true;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("     (After)\n");
        gtDispNode(fieldSeqNode, nullptr, nullptr, false);
        gtDispCommonEndLine(fieldSeqNode);
    }
#endif // DEBUG
}

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
//  stmt  - Statement*. the stmt node we want to check
//
// return value:
//  if this funciton successfully optimized the stmts, then return true. Otherwise
//  return false;

bool Compiler::fgMorphCombineSIMDFieldAssignments(BasicBlock* block, Statement* stmt)
{
    GenTree* tree = stmt->GetRootNode();
    assert(tree->OperGet() == GT_ASG);

    GenTree*  originalLHS    = tree->AsOp()->gtOp1;
    GenTree*  prevLHS        = tree->AsOp()->gtOp1;
    GenTree*  prevRHS        = tree->AsOp()->gtOp2;
    unsigned  index          = 0;
    var_types baseType       = TYP_UNKNOWN;
    unsigned  simdSize       = 0;
    GenTree*  simdStructNode = getSIMDStructFromField(prevRHS, &baseType, &index, &simdSize, true);

    if (simdStructNode == nullptr || index != 0 || baseType != TYP_FLOAT)
    {
        // if the RHS is not from a SIMD vector field X, then there is no need to check further.
        return false;
    }

    var_types  simdType             = getSIMDTypeForSize(simdSize);
    int        assignmentsCount     = simdSize / genTypeSize(baseType) - 1;
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
        // that the assignments are not assgining to the contiguously memory
        // locations from same vector.
        return false;
    }
#ifdef DEBUG
    if (verbose)
    {
        printf("\nFound contiguous assignments from a SIMD vector to memory.\n");
        printf("From " FMT_BB ", stmt ", block->bbNum);
        printStmtID(stmt);
        printf(" to stmt");
        printStmtID(lastStmt);
        printf("\n");
    }
#endif

    for (int i = 0; i < assignmentsCount; i++)
    {
        fgRemoveStmt(block, stmt->GetNextStmt());
    }

    GenTree* dstNode;

    if (originalLHS->OperIs(GT_LCL_FLD))
    {
        dstNode         = originalLHS;
        dstNode->gtType = simdType;
        dstNode->AsLclFld()->SetFieldSeq(FieldSeqStore::NotAField());

        // This may have changed a partial local field into full local field
        if (dstNode->IsPartialLclFld(this))
        {
            dstNode->gtFlags |= GTF_VAR_USEASG;
        }
        else
        {
            dstNode->gtFlags &= ~GTF_VAR_USEASG;
        }
    }
    else
    {
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

        if (simdStructNode->TypeGet() == TYP_BYREF)
        {
            assert(simdStructNode->OperIsLocal());
            assert(lvaIsImplicitByRefLocal(simdStructNode->AsLclVarCommon()->GetLclNum()));
            simdStructNode = gtNewIndir(simdType, simdStructNode);
        }
        else
        {
            assert(varTypeIsSIMD(simdStructNode));
        }

        dstNode = gtNewOperNode(GT_IND, simdType, copyBlkDst);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n" FMT_BB " stmt ", block->bbNum);
        printStmtID(stmt);
        printf("(before)\n");
        gtDispStmt(stmt);
    }
#endif

    assert(!simdStructNode->CanCSE());
    simdStructNode->ClearDoNotCSE();

    tree = gtNewAssignNode(dstNode, simdStructNode);

    stmt->SetRootNode(tree);

    // Since we generated a new address node which didn't exist before,
    // we should expose this address manually here.
    // TODO-ADDR: Remove this when LocalAddressVisitor transforms all
    // local field access into LCL_FLDs, at that point we would be
    // combining 2 existing LCL_FLDs or 2 FIELDs that do not reference
    // a local and thus cannot result in a new address exposed local.
    fgMarkAddressExposedLocals(stmt);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nReplaced " FMT_BB " stmt", block->bbNum);
        printStmtID(stmt);
        printf("(after)\n");
        gtDispStmt(stmt);
    }
#endif
    return true;
}

#endif // FEATURE_SIMD

//------------------------------------------------------------------------
// fgCheckStmtAfterTailCall: check that statements after the tail call stmt
// candidate are in one of expected forms, that are desctibed below.
//
// Return Value:
//    'true' if stmts are in the expected form, else 'false'.
//
bool Compiler::fgCheckStmtAfterTailCall()
{

    // For void calls, we would have created a GT_CALL in the stmt list.
    // For non-void calls, we would have created a GT_RETURN(GT_CAST(GT_CALL)).
    // For calls returning structs, we would have a void call, followed by a void return.
    // For debuggable code, it would be an assignment of the call to a temp
    // We want to get rid of any of this extra trees, and just leave
    // the call.
    Statement* callStmt = fgMorphStmt;

    Statement* nextMorphStmt = callStmt->GetNextStmt();

    // Check that the rest stmts in the block are in one of the following pattern:
    //  1) ret(void)
    //  2) ret(cast*(callResultLclVar))
    //  3) lclVar = callResultLclVar, the actual ret(lclVar) in another block
    if (nextMorphStmt != nullptr)
    {
        GenTree* callExpr = callStmt->GetRootNode();
        if (callExpr->gtOper != GT_ASG)
        {
            // The next stmt can be GT_RETURN(TYP_VOID) or GT_RETURN(lclVar),
            // where lclVar was return buffer in the call for structs or simd.
            Statement* retStmt = nextMorphStmt;
            GenTree*   retExpr = retStmt->GetRootNode();
            noway_assert(retExpr->gtOper == GT_RETURN);

            nextMorphStmt = retStmt->GetNextStmt();
        }
        else
        {
            noway_assert(callExpr->gtGetOp1()->OperIsLocal());
            unsigned callResultLclNumber = callExpr->gtGetOp1()->AsLclVarCommon()->GetLclNum();

#if FEATURE_TAILCALL_OPT_SHARED_RETURN

            // We can have a chain of assignments from the call result to
            // various inline return spill temps. These are ok as long
            // as the last one ultimately provides the return value or is ignored.
            //
            // And if we're returning a small type we may see a cast
            // on the source side.
            while ((nextMorphStmt != nullptr) && (nextMorphStmt->GetRootNode()->OperIs(GT_ASG)))
            {
                Statement* moveStmt = nextMorphStmt;
                GenTree*   moveExpr = nextMorphStmt->GetRootNode();
                GenTree*   moveDest = moveExpr->gtGetOp1();
                noway_assert(moveDest->OperIsLocal());

                // Tunnel through any casts on the source side.
                GenTree* moveSource = moveExpr->gtGetOp2();
                while (moveSource->OperIs(GT_CAST))
                {
                    noway_assert(!moveSource->gtOverflow());
                    moveSource = moveSource->gtGetOp1();
                }
                noway_assert(moveSource->OperIsLocal());

                // Verify we're just passing the value from one local to another
                // along the chain.
                const unsigned srcLclNum = moveSource->AsLclVarCommon()->GetLclNum();
                noway_assert(srcLclNum == callResultLclNumber);
                const unsigned dstLclNum = moveDest->AsLclVarCommon()->GetLclNum();
                callResultLclNumber      = dstLclNum;

                nextMorphStmt = moveStmt->GetNextStmt();
            }
            if (nextMorphStmt != nullptr)
#endif
            {
                Statement* retStmt = nextMorphStmt;
                GenTree*   retExpr = nextMorphStmt->GetRootNode();
                noway_assert(retExpr->gtOper == GT_RETURN);

                GenTree* treeWithLcl = retExpr->gtGetOp1();
                while (treeWithLcl->gtOper == GT_CAST)
                {
                    noway_assert(!treeWithLcl->gtOverflow());
                    treeWithLcl = treeWithLcl->gtGetOp1();
                }

                noway_assert(callResultLclNumber == treeWithLcl->AsLclVarCommon()->GetLclNum());

                nextMorphStmt = retStmt->GetNextStmt();
            }
        }
    }
    return nextMorphStmt == nullptr;
}

//------------------------------------------------------------------------
// fgCanTailCallViaJitHelper: check whether we can use the faster tailcall
// JIT helper on x86.
//
// Return Value:
//    'true' if we can; or 'false' if we should use the generic tailcall mechanism.
//
bool Compiler::fgCanTailCallViaJitHelper()
{
#ifndef TARGET_X86
    // On anything except X86 we have no faster mechanism available.
    return false;
#else
    // The JIT helper does not properly handle the case where localloc was used.
    if (compLocallocUsed)
        return false;

    return true;
#endif
}

static const int      numberOfTrackedFlags               = 5;
static const unsigned trackedFlags[numberOfTrackedFlags] = {GTF_ASG, GTF_CALL, GTF_EXCEPT, GTF_GLOB_REF,
                                                            GTF_ORDER_SIDEEFF};

//------------------------------------------------------------------------
// fgMorphArgList: morph argument list tree without recursion.
//
// Arguments:
//    args - argument list tree to morph;
//    mac  - morph address context, used to morph children.
//
// Return Value:
//    morphed argument list.
//
GenTreeArgList* Compiler::fgMorphArgList(GenTreeArgList* args, MorphAddrContext* mac)
{
    // Use a non-recursive algorithm that morphs all actual list values,
    // memorizes the last node for each effect flag and resets
    // them during the second iteration.
    assert((trackedFlags[0] | trackedFlags[1] | trackedFlags[2] | trackedFlags[3] | trackedFlags[4]) == GTF_ALL_EFFECT);

    GenTree* memorizedLastNodes[numberOfTrackedFlags] = {nullptr};

    for (GenTreeArgList* listNode = args; listNode != nullptr; listNode = listNode->Rest())
    {
        // Morph actual list values.
        GenTree*& arg = listNode->Current();
        arg           = fgMorphTree(arg, mac);

        // Remember the last list node with each flag.
        for (int i = 0; i < numberOfTrackedFlags; ++i)
        {
            if ((arg->gtFlags & trackedFlags[i]) != 0)
            {
                memorizedLastNodes[i] = listNode;
            }
        }
    }

    for (GenTreeArgList* listNode = args; listNode != nullptr; listNode = listNode->Rest())
    {
        // Clear all old effects from the list node.
        listNode->gtFlags &= ~GTF_ALL_EFFECT;

        // Spread each flag to all list nodes (to the prefix) before the memorized last node.
        for (int i = 0; i < numberOfTrackedFlags; ++i)
        {
            if (memorizedLastNodes[i] != nullptr)
            {
                listNode->gtFlags |= trackedFlags[i];
            }
            if (listNode == memorizedLastNodes[i])
            {
                memorizedLastNodes[i] = nullptr;
            }
        }
    }

    return args;
}
