// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          AssertionProp                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// Contains: Whether the range contains a given integral value, inclusive.
//
// Arguments:
//    value - the integral value in question
//
// Return Value:
//    "true" if the value is within the range's bounds, "false" otherwise.
//
bool IntegralRange::Contains(int64_t value) const
{
    int64_t lowerBound = SymbolicToRealValue(m_lowerBound);
    int64_t upperBound = SymbolicToRealValue(m_upperBound);

    return (lowerBound <= value) && (value <= upperBound);
}

//------------------------------------------------------------------------
// SymbolicToRealValue: Convert a symbolic value to a 64-bit signed integer.
//
// Arguments:
//    value - the symbolic value in question
//
// Return Value:
//    Integer corresponding to the symbolic value.
//
/* static */ int64_t IntegralRange::SymbolicToRealValue(SymbolicIntegerValue value)
{
    static const int64_t SymbolicToRealMap[]{
        INT64_MIN,               // SymbolicIntegerValue::LongMin
        INT32_MIN,               // SymbolicIntegerValue::IntMin
        INT16_MIN,               // SymbolicIntegerValue::ShortMin
        INT8_MIN,                // SymbolicIntegerValue::ByteMin
        0,                       // SymbolicIntegerValue::Zero
        1,                       // SymbolicIntegerValue::One
        INT8_MAX,                // SymbolicIntegerValue::ByteMax
        UINT8_MAX,               // SymbolicIntegerValue::UByteMax
        INT16_MAX,               // SymbolicIntegerValue::ShortMax
        UINT16_MAX,              // SymbolicIntegerValue::UShortMax
        CORINFO_Array_MaxLength, // SymbolicIntegerValue::ArrayLenMax
        INT32_MAX,               // SymbolicIntegerValue::IntMax
        UINT32_MAX,              // SymbolicIntegerValue::UIntMax
        INT64_MAX                // SymbolicIntegerValue::LongMax
    };

    assert(sizeof(SymbolicIntegerValue) == sizeof(int32_t));
    assert(SymbolicToRealMap[static_cast<int32_t>(SymbolicIntegerValue::LongMin)] == INT64_MIN);
    assert(SymbolicToRealMap[static_cast<int32_t>(SymbolicIntegerValue::Zero)] == 0);
    assert(SymbolicToRealMap[static_cast<int32_t>(SymbolicIntegerValue::LongMax)] == INT64_MAX);

    return SymbolicToRealMap[static_cast<int32_t>(value)];
}

//------------------------------------------------------------------------
// LowerBoundForType: Get the symbolic lower bound for a type.
//
// Arguments:
//    type - the integral type in question
//
// Return Value:
//    Symbolic value representing the smallest possible value "type" can represent.
//
/* static */ SymbolicIntegerValue IntegralRange::LowerBoundForType(var_types type)
{
    switch (type)
    {
        case TYP_BOOL:
        case TYP_UBYTE:
        case TYP_USHORT:
            return SymbolicIntegerValue::Zero;
        case TYP_BYTE:
            return SymbolicIntegerValue::ByteMin;
        case TYP_SHORT:
            return SymbolicIntegerValue::ShortMin;
        case TYP_INT:
            return SymbolicIntegerValue::IntMin;
        case TYP_LONG:
            return SymbolicIntegerValue::LongMin;
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// UpperBoundForType: Get the symbolic upper bound for a type.
//
// Arguments:
//    type - the integral type in question
//
// Return Value:
//    Symbolic value representing the largest possible value "type" can represent.
//
/* static */ SymbolicIntegerValue IntegralRange::UpperBoundForType(var_types type)
{
    switch (type)
    {
        case TYP_BYTE:
            return SymbolicIntegerValue::ByteMax;
        case TYP_BOOL:
        case TYP_UBYTE:
            return SymbolicIntegerValue::UByteMax;
        case TYP_SHORT:
            return SymbolicIntegerValue::ShortMax;
        case TYP_USHORT:
            return SymbolicIntegerValue::UShortMax;
        case TYP_INT:
            return SymbolicIntegerValue::IntMax;
        case TYP_UINT:
            return SymbolicIntegerValue::UIntMax;
        case TYP_LONG:
            return SymbolicIntegerValue::LongMax;
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// ForNode: Compute the integral range for a node.
//
// Arguments:
//    node     - the node, of an integral type, in question
//    compiler - the Compiler, used to retrieve additional info
//
// Return Value:
//    The integral range this node produces.
//
/* static */ IntegralRange IntegralRange::ForNode(GenTree* node, Compiler* compiler)
{
    assert(varTypeIsIntegral(node));

    var_types rangeType = node->TypeGet();

    switch (node->OperGet())
    {
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::One};

        case GT_ARR_LENGTH:
            return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::ArrayLenMax};

        case GT_CALL:
            if (node->AsCall()->NormalizesSmallTypesOnReturn())
            {
                rangeType = static_cast<var_types>(node->AsCall()->gtReturnType);
            }
            break;

        case GT_LCL_VAR:
            if (compiler->lvaGetDesc(node->AsLclVar())->lvNormalizeOnStore())
            {
                rangeType = compiler->lvaGetDesc(node->AsLclVar())->TypeGet();
            }
            break;

        case GT_CAST:
            return ForCastOutput(node->AsCast());

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            switch (node->AsHWIntrinsic()->GetHWIntrinsicId())
            {
#if defined(TARGET_XARCH)
                case NI_BMI1_TrailingZeroCount:
                case NI_BMI1_X64_TrailingZeroCount:
                case NI_LZCNT_LeadingZeroCount:
                case NI_LZCNT_X64_LeadingZeroCount:
                case NI_POPCNT_PopCount:
                case NI_POPCNT_X64_PopCount:
#elif defined(TARGET_ARM64)
                case NI_AdvSimd_PopCount:
                case NI_AdvSimd_LeadingZeroCount:
                case NI_AdvSimd_LeadingSignCount:
                case NI_ArmBase_LeadingZeroCount:
                case NI_ArmBase_Arm64_LeadingZeroCount:
                case NI_ArmBase_Arm64_LeadingSignCount:
#else
#error Unsupported platform
#endif
                    // TODO-Casts: specify more precise ranges once "IntegralRange" supports them.
                    return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::ByteMax};
                default:
                    break;
            }
            break;
#endif // defined(FEATURE_HW_INTRINSICS)

        default:
            break;
    }

    return ForType(rangeType);
}

//------------------------------------------------------------------------
// ForCastInput: Get the non-overflowing input range for a cast.
//
// This routine computes the input range for a cast from
// an integer to an integer for which it will not overflow.
// See also the specification comment for IntegralRange.
//
// Arguments:
//    cast - the cast node for which the range will be computed
//
// Return Value:
//    The range this cast consumes without overflowing - see description.
//
/* static */ IntegralRange IntegralRange::ForCastInput(GenTreeCast* cast)
{
    var_types fromType     = genActualType(cast->CastOp());
    var_types toType       = cast->CastToType();
    bool      fromUnsigned = cast->IsUnsigned();

    assert((fromType == TYP_INT) || (fromType == TYP_LONG) || varTypeIsGC(fromType));
    assert(varTypeIsIntegral(toType));

    // Cast from a GC type is the same as a cast from TYP_I_IMPL for our purposes.
    if (varTypeIsGC(fromType))
    {
        fromType = TYP_I_IMPL;
    }

    if (!cast->gtOverflow())
    {
        // CAST(small type <- uint/int/ulong/long) - [TO_TYPE_MIN..TO_TYPE_MAX]
        if (varTypeIsSmall(toType))
        {
            return {LowerBoundForType(toType), UpperBoundForType(toType)};
        }

        // We choose to say here that representation-changing casts never overflow.
        // It does not really matter what we do here because representation-changing
        // non-overflowing casts cannot be deleted from the IR in any case.
        // CAST(uint/int <- uint/int)     - [INT_MIN..INT_MAX]
        // CAST(uint/int <- ulong/long)   - [LONG_MIN..LONG_MAX]
        // CAST(ulong/long <- uint/int)   - [INT_MIN..INT_MAX]
        // CAST(ulong/long <- ulong/long) - [LONG_MIN..LONG_MAX]
        return ForType(fromType);
    }

    SymbolicIntegerValue lowerBound;
    SymbolicIntegerValue upperBound;

    // CAST_OVF(small type <- int/long)   - [TO_TYPE_MIN..TO_TYPE_MAX]
    // CAST_OVF(small type <- uint/ulong) - [0..TO_TYPE_MAX]
    if (varTypeIsSmall(toType))
    {
        lowerBound = fromUnsigned ? SymbolicIntegerValue::Zero : LowerBoundForType(toType);
        upperBound = UpperBoundForType(toType);
    }
    else
    {
        switch (toType)
        {
            // CAST_OVF(uint <- uint)       - [INT_MIN..INT_MAX]
            // CAST_OVF(uint <- int)        - [0..INT_MAX]
            // CAST_OVF(uint <- ulong/long) - [0..UINT_MAX]
            case TYP_UINT:
                if (fromType == TYP_LONG)
                {
                    lowerBound = SymbolicIntegerValue::Zero;
                    upperBound = SymbolicIntegerValue::UIntMax;
                }
                else
                {
                    lowerBound = fromUnsigned ? SymbolicIntegerValue::IntMin : SymbolicIntegerValue::Zero;
                    upperBound = SymbolicIntegerValue::IntMax;
                }
                break;

            // CAST_OVF(int <- uint/ulong) - [0..INT_MAX]
            // CAST_OVF(int <- int/long)   - [INT_MIN..INT_MAX]
            case TYP_INT:
                lowerBound = fromUnsigned ? SymbolicIntegerValue::Zero : SymbolicIntegerValue::IntMin;
                upperBound = SymbolicIntegerValue::IntMax;
                break;

            // CAST_OVF(ulong <- uint)  - [INT_MIN..INT_MAX]
            // CAST_OVF(ulong <- int)   - [0..INT_MAX]
            // CAST_OVF(ulong <- ulong) - [LONG_MIN..LONG_MAX]
            // CAST_OVF(ulong <- long)  - [0..LONG_MAX]
            case TYP_ULONG:
                lowerBound = fromUnsigned ? LowerBoundForType(fromType) : SymbolicIntegerValue::Zero;
                upperBound = UpperBoundForType(fromType);
                break;

            // CAST_OVF(long <- uint/int) - [INT_MIN..INT_MAX]
            // CAST_OVF(long <- ulong)    - [0..LONG_MAX]
            // CAST_OVF(long <- long)     - [LONG_MIN..LONG_MAX]
            case TYP_LONG:
                if (fromUnsigned && (fromType == TYP_LONG))
                {
                    lowerBound = SymbolicIntegerValue::Zero;
                }
                else
                {
                    lowerBound = LowerBoundForType(fromType);
                }
                upperBound = UpperBoundForType(fromType);
                break;

            default:
                unreached();
        }
    }

    return {lowerBound, upperBound};
}

//------------------------------------------------------------------------
// ForCastOutput: Get the output range for a cast.
//
// This method is the "output" counterpart to ForCastInput, it returns
// a range produced by a cast (by definition, non-overflowing one).
// The output range is the same for representation-preserving casts, but
// can be different for others. One example is CAST_OVF(uint <- long).
// The input range is [0..UINT_MAX], while the output is [INT_MIN..INT_MAX].
// Unlike ForCastInput, this method supports casts from floating point types.
//
// Arguments:
//   cast - the cast node for which the range will be computed
//
// Return Value:
//   The range this cast produces - see description.
//
/* static */ IntegralRange IntegralRange::ForCastOutput(GenTreeCast* cast)
{
    var_types fromType     = genActualType(cast->CastOp());
    var_types toType       = cast->CastToType();
    bool      fromUnsigned = cast->IsUnsigned();

    assert((fromType == TYP_INT) || (fromType == TYP_LONG) || varTypeIsFloating(fromType) || varTypeIsGC(fromType));
    assert(varTypeIsIntegral(toType));

    // CAST/CAST_OVF(small type <- float/double) - [TO_TYPE_MIN..TO_TYPE_MAX]
    // CAST/CAST_OVF(uint/int <- float/double)   - [INT_MIN..INT_MAX]
    // CAST/CAST_OVF(ulong/long <- float/double) - [LONG_MIN..LONG_MAX]
    if (varTypeIsFloating(fromType))
    {
        if (!varTypeIsSmall(toType))
        {
            toType = genActualType(toType);
        }

        return IntegralRange::ForType(toType);
    }

    // Cast from a GC type is the same as a cast from TYP_I_IMPL for our purposes.
    if (varTypeIsGC(fromType))
    {
        fromType = TYP_I_IMPL;
    }

    if (varTypeIsSmall(toType) || (genActualType(toType) == fromType))
    {
        return ForCastInput(cast);
    }

    // CAST(uint/int <- ulong/long) - [INT_MIN..INT_MAX]
    // CAST(ulong/long <- uint)     - [0..UINT_MAX]
    // CAST(ulong/long <- int)      - [INT_MIN..INT_MAX]
    if (!cast->gtOverflow())
    {
        if ((fromType == TYP_INT) && fromUnsigned)
        {
            return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::UIntMax};
        }

        return {SymbolicIntegerValue::IntMin, SymbolicIntegerValue::IntMax};
    }

    SymbolicIntegerValue lowerBound;
    SymbolicIntegerValue upperBound;
    switch (toType)
    {
        // CAST_OVF(uint <- ulong) - [INT_MIN..INT_MAX]
        // CAST_OVF(uint <- long)  - [INT_MIN..INT_MAX]
        case TYP_UINT:
            lowerBound = SymbolicIntegerValue::IntMin;
            upperBound = SymbolicIntegerValue::IntMax;
            break;

        // CAST_OVF(int <- ulong) - [0..INT_MAX]
        // CAST_OVF(int <- long)  - [INT_MIN..INT_MAX]
        case TYP_INT:
            lowerBound = fromUnsigned ? SymbolicIntegerValue::Zero : SymbolicIntegerValue::IntMin;
            upperBound = SymbolicIntegerValue::IntMax;
            break;

        // CAST_OVF(ulong <- uint) - [0..UINT_MAX]
        // CAST_OVF(ulong <- int)  - [0..INT_MAX]
        case TYP_ULONG:
            lowerBound = SymbolicIntegerValue::Zero;
            upperBound = fromUnsigned ? SymbolicIntegerValue::UIntMax : SymbolicIntegerValue::IntMax;
            break;

        // CAST_OVF(long <- uint) - [0..UINT_MAX]
        // CAST_OVF(long <- int)  - [INT_MIN..INT_MAX]
        case TYP_LONG:
            lowerBound = fromUnsigned ? SymbolicIntegerValue::Zero : SymbolicIntegerValue::IntMin;
            upperBound = fromUnsigned ? SymbolicIntegerValue::UIntMax : SymbolicIntegerValue::IntMax;
            break;

        default:
            unreached();
    }

    return {lowerBound, upperBound};
}

#ifdef DEBUG
/* static */ void IntegralRange::Print(IntegralRange range)
{
    printf("[%lld", SymbolicToRealValue(range.m_lowerBound));
    printf("..");
    printf("%lld]", SymbolicToRealValue(range.m_upperBound));
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Helper passed to Compiler::fgWalkTreePre() to find the Asgn node for optAddCopies()
 */

/* static */
Compiler::fgWalkResult Compiler::optAddCopiesCallback(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->OperIs(GT_ASG))
    {
        GenTree*  op1  = tree->AsOp()->gtOp1;
        Compiler* comp = data->compiler;

        if ((op1->gtOper == GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == comp->optAddCopyLclNum))
        {
            comp->optAddCopyAsgnNode = tree;
            return WALK_ABORT;
        }
    }
    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Add new copies before Assertion Prop.
 */

void Compiler::optAddCopies()
{
    unsigned   lclNum;
    LclVarDsc* varDsc;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In optAddCopies()\n\n");
    }
    if (verboseTrees)
    {
        printf("Blocks/Trees at start of phase\n");
        fgDispBasicBlocks(true);
    }
#endif

    // Don't add any copies if we have reached the tracking limit.
    if (lvaHaveManyLocals())
    {
        return;
    }

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        var_types typ = varDsc->TypeGet();

        // We only add copies for non temp local variables
        // that have a single def and that can possibly be enregistered

        if (varDsc->lvIsTemp || !varDsc->lvSingleDef || !varTypeIsEnregisterable(typ))
        {
            continue;
        }

        /* For lvNormalizeOnLoad(), we need to add a cast to the copy-assignment
           like "copyLclNum = int(varDsc)" and optAssertionGen() only
           tracks simple assignments. The same goes for lvNormalizedOnStore as
           the cast is generated in fgMorphSmpOpAsg. This boils down to not having
           a copy until optAssertionGen handles this*/
        if (varDsc->lvNormalizeOnLoad() || varDsc->lvNormalizeOnStore())
        {
            continue;
        }

        if (varTypeIsSmall(varDsc->TypeGet()) || typ == TYP_BOOL)
        {
            continue;
        }

        // If locals must be initialized to zero, that initialization counts as a second definition.
        // VB in particular allows usage of variables not explicitly initialized.
        // Note that this effectively disables this optimization for all local variables
        // as C# sets InitLocals all the time starting in Whidbey.

        if (!varDsc->lvIsParam && info.compInitMem)
        {
            continue;
        }

        // On x86 we may want to add a copy for an incoming double parameter
        // because we can ensure that the copy we make is double aligned
        // where as we can never ensure the alignment of an incoming double parameter
        //
        // On all other platforms we will never need to make a copy
        // for an incoming double parameter

        bool isFloatParam = false;

#ifdef TARGET_X86
        isFloatParam = varDsc->lvIsParam && varTypeIsFloating(typ);
#endif

        if (!isFloatParam && !varDsc->lvVolatileHint)
        {
            continue;
        }

        // We don't want to add a copy for a variable that is part of a struct
        if (varDsc->lvIsStructField)
        {
            continue;
        }

        // We require that the weighted ref count be significant.
        if (varDsc->lvRefCntWtd() <= (BB_LOOP_WEIGHT_SCALE * BB_UNITY_WEIGHT / 2))
        {
            continue;
        }

        // For parameters, we only want to add a copy for the heavier-than-average
        // uses instead of adding a copy to cover every single use.
        // 'paramImportantUseDom' is the set of blocks that dominate the
        // heavier-than-average uses of a parameter.
        // Initial value is all blocks.

        BlockSet paramImportantUseDom(BlockSetOps::MakeFull(this));

        // This will be threshold for determining heavier-than-average uses
        weight_t paramAvgWtdRefDiv2 = (varDsc->lvRefCntWtd() + varDsc->lvRefCnt() / 2) / (varDsc->lvRefCnt() * 2);

        bool paramFoundImportantUse = false;

#ifdef DEBUG
        if (verbose)
        {
            printf("Trying to add a copy for V%02u %s, avg_wtd = %s\n", lclNum,
                   varDsc->lvIsParam ? "an arg" : "a local", refCntWtd2str(paramAvgWtdRefDiv2));
        }
#endif

        //
        // We must have a ref in a block that is dominated only by the entry block
        //

        if (BlockSetOps::MayBeUninit(varDsc->lvRefBlks))
        {
            // No references
            continue;
        }

        bool isDominatedByFirstBB = false;

        BlockSetOps::Iter iter(this, varDsc->lvRefBlks);
        unsigned          bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            /* Find the block 'bbNum' */
            BasicBlock* block = fgFirstBB;
            while (block && (block->bbNum != bbNum))
            {
                block = block->bbNext;
            }
            noway_assert(block && (block->bbNum == bbNum));

            bool     importantUseInBlock = (varDsc->lvIsParam) && (block->getBBWeight(this) > paramAvgWtdRefDiv2);
            bool     isPreHeaderBlock    = ((block->bbFlags & BBF_LOOP_PREHEADER) != 0);
            BlockSet blockDom(BlockSetOps::UninitVal());
            BlockSet blockDomSub0(BlockSetOps::UninitVal());

            if (block->bbIDom == nullptr && isPreHeaderBlock)
            {
                // Loop Preheader blocks that we insert will have a bbDom set that is nullptr
                // but we can instead use the bNext successor block's dominator information
                noway_assert(block->bbNext != nullptr);
                BlockSetOps::AssignNoCopy(this, blockDom, fgGetDominatorSet(block->bbNext));
            }
            else
            {
                BlockSetOps::AssignNoCopy(this, blockDom, fgGetDominatorSet(block));
            }

            if (!BlockSetOps::IsEmpty(this, blockDom))
            {
                BlockSetOps::Assign(this, blockDomSub0, blockDom);
                if (isPreHeaderBlock)
                {
                    // We must clear bbNext block number from the dominator set
                    BlockSetOps::RemoveElemD(this, blockDomSub0, block->bbNext->bbNum);
                }
                /* Is this block dominated by fgFirstBB? */
                if (BlockSetOps::IsMember(this, blockDomSub0, fgFirstBB->bbNum))
                {
                    isDominatedByFirstBB = true;
                }
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("        Referenced in " FMT_BB ", bbWeight is %s", bbNum,
                       refCntWtd2str(block->getBBWeight(this)));

                if (isDominatedByFirstBB)
                {
                    printf(", which is dominated by BB01");
                }

                if (importantUseInBlock)
                {
                    printf(", ImportantUse");
                }

                printf("\n");
            }
#endif

            /* If this is a heavier-than-average block, then track which
               blocks dominate this use of the parameter. */
            if (importantUseInBlock)
            {
                paramFoundImportantUse = true;
                BlockSetOps::IntersectionD(this, paramImportantUseDom,
                                           blockDomSub0); // Clear blocks that do not dominate
            }
        }

        // We should have found at least one heavier-than-averageDiv2 block.
        if (varDsc->lvIsParam)
        {
            if (!paramFoundImportantUse)
            {
                continue;
            }
        }

        // For us to add a new copy:
        // we require that we have a floating point parameter
        // or a lvVolatile variable that is always reached from the first BB
        // and we have at least one block available in paramImportantUseDom
        //
        bool doCopy = (isFloatParam || (isDominatedByFirstBB && varDsc->lvVolatileHint)) &&
                      !BlockSetOps::IsEmpty(this, paramImportantUseDom);

        // Under stress mode we expand the number of candidates
        // to include parameters of any type
        // or any variable that is always reached from the first BB
        //
        if (compStressCompile(STRESS_GENERIC_VARN, 30))
        {
            // Ensure that we preserve the invariants required by the subsequent code.
            if (varDsc->lvIsParam || isDominatedByFirstBB)
            {
                doCopy = true;
            }
        }

        if (!doCopy)
        {
            continue;
        }

        Statement* stmt;
        unsigned   copyLclNum = lvaGrabTemp(false DEBUGARG("optAddCopies"));

        // Because lvaGrabTemp may have reallocated the lvaTable, ensure varDsc is still in sync.
        varDsc = lvaGetDesc(lclNum);

        // Set lvType on the new Temp Lcl Var
        lvaGetDesc(copyLclNum)->lvType = typ;

#ifdef DEBUG
        if (verbose)
        {
            printf("\n    Finding the best place to insert the assignment V%02i=V%02i\n", copyLclNum, lclNum);
        }
#endif

        if (varDsc->lvIsParam)
        {
            noway_assert(varDsc->lvDefStmt == nullptr || varDsc->lvIsStructField);

            // Create a new copy assignment tree
            GenTree* copyAsgn = gtNewTempAssign(copyLclNum, gtNewLclvNode(lclNum, typ));

            /* Find the best block to insert the new assignment     */
            /* We will choose the lowest weighted block, and within */
            /* those block, the highest numbered block which        */
            /* dominates all the uses of the local variable         */

            /* Our default is to use the first block */
            BasicBlock* bestBlock  = fgFirstBB;
            weight_t    bestWeight = bestBlock->getBBWeight(this);
            BasicBlock* block      = bestBlock;

#ifdef DEBUG
            if (verbose)
            {
                printf("        Starting at " FMT_BB ", bbWeight is %s", block->bbNum,
                       refCntWtd2str(block->getBBWeight(this)));

                printf(", bestWeight is %s\n", refCntWtd2str(bestWeight));
            }
#endif

            /* We have already calculated paramImportantUseDom above. */
            BlockSetOps::Iter iter(this, paramImportantUseDom);
            unsigned          bbNum = 0;
            while (iter.NextElem(&bbNum))
            {
                /* Advance block to point to 'bbNum' */
                /* This assumes that the iterator returns block number is increasing lexical order. */
                while (block && (block->bbNum != bbNum))
                {
                    block = block->bbNext;
                }
                noway_assert(block && (block->bbNum == bbNum));

#ifdef DEBUG
                if (verbose)
                {
                    printf("        Considering " FMT_BB ", bbWeight is %s", block->bbNum,
                           refCntWtd2str(block->getBBWeight(this)));

                    printf(", bestWeight is %s\n", refCntWtd2str(bestWeight));
                }
#endif

                // Does this block have a smaller bbWeight value?
                if (block->getBBWeight(this) > bestWeight)
                {
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("bbWeight too high\n");
                    }
#endif
                    continue;
                }

                // Don't use blocks that are exception handlers because
                // inserting a new first statement will interface with
                // the CATCHARG

                if (handlerGetsXcptnObj(block->bbCatchTyp))
                {
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Catch block\n");
                    }
#endif
                    continue;
                }

                // Don't use the BBJ_ALWAYS block marked with BBF_KEEP_BBJ_ALWAYS. These
                // are used by EH code. The JIT can not generate code for such a block.

                if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
                {
#if defined(FEATURE_EH_FUNCLETS)
                    // With funclets, this is only used for BBJ_CALLFINALLY/BBJ_ALWAYS pairs. For x86, it is also used
                    // as the "final step" block for leaving finallys.
                    assert(block->isBBCallAlwaysPairTail());
#endif // FEATURE_EH_FUNCLETS
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Internal EH BBJ_ALWAYS block\n");
                    }
#endif
                    continue;
                }

                // This block will be the new candidate for the insert point
                // for the new assignment
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                if (verbose)
                {
                    printf("new bestBlock\n");
                }
#endif

                bestBlock  = block;
                bestWeight = block->getBBWeight(this);
            }

            // If there is a use of the variable in this block
            // then we insert the assignment at the beginning
            // otherwise we insert the statement at the end
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (verbose)
            {
                printf("        Insert copy at the %s of " FMT_BB "\n",
                       (BlockSetOps::IsEmpty(this, paramImportantUseDom) ||
                        BlockSetOps::IsMember(this, varDsc->lvRefBlks, bestBlock->bbNum))
                           ? "start"
                           : "end",
                       bestBlock->bbNum);
            }
#endif

            if (BlockSetOps::IsEmpty(this, paramImportantUseDom) ||
                BlockSetOps::IsMember(this, varDsc->lvRefBlks, bestBlock->bbNum))
            {
                stmt = fgNewStmtAtBeg(bestBlock, copyAsgn);
            }
            else
            {
                stmt = fgNewStmtNearEnd(bestBlock, copyAsgn);
            }
        }
        else
        {
            noway_assert(varDsc->lvDefStmt != nullptr);

            /* Locate the assignment to varDsc in the lvDefStmt */
            stmt = varDsc->lvDefStmt;

            optAddCopyLclNum   = lclNum;  // in
            optAddCopyAsgnNode = nullptr; // out

            fgWalkTreePre(stmt->GetRootNodePointer(), Compiler::optAddCopiesCallback, (void*)this, false);

            noway_assert(optAddCopyAsgnNode);

            GenTree* tree = optAddCopyAsgnNode;
            GenTree* op1  = tree->AsOp()->gtOp1;

            noway_assert(tree && op1 && tree->OperIs(GT_ASG) && (op1->gtOper == GT_LCL_VAR) &&
                         (op1->AsLclVarCommon()->GetLclNum() == lclNum));

            /* Assign the old expression into the new temp */

            GenTree* newAsgn = gtNewTempAssign(copyLclNum, tree->AsOp()->gtOp2);

            /* Copy the new temp to op1 */

            GenTree* copyAsgn = gtNewAssignNode(op1, gtNewLclvNode(copyLclNum, typ));

            /* Change the tree to a GT_COMMA with the two assignments as child nodes */

            tree->gtBashToNOP();
            tree->ChangeOper(GT_COMMA);

            tree->AsOp()->gtOp1 = newAsgn;
            tree->AsOp()->gtOp2 = copyAsgn;

            tree->gtFlags |= (newAsgn->gtFlags & GTF_ALL_EFFECT);
            tree->gtFlags |= (copyAsgn->gtFlags & GTF_ALL_EFFECT);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\nIntroducing a new copy for V%02u\n", lclNum);
            gtDispTree(stmt->GetRootNode());
            printf("\n");
        }
#endif
    }
}

//------------------------------------------------------------------------------
// GetAssertionDep: Retrieve the assertions on this local variable
//
// Arguments:
//    lclNum - The local var id.
//
// Return Value:
//    The dependent assertions (assertions using the value of the local var)
//    of the local var.
//

ASSERT_TP& Compiler::GetAssertionDep(unsigned lclNum)
{
    JitExpandArray<ASSERT_TP>& dep = *optAssertionDep;
    if (dep[lclNum] == nullptr)
    {
        dep[lclNum] = BitVecOps::MakeEmpty(apTraits);
    }
    return dep[lclNum];
}

/*****************************************************************************
 *
 *  Initialize the assertion prop bitset traits and the default bitsets.
 */

void Compiler::optAssertionTraitsInit(AssertionIndex assertionCount)
{
    apTraits = new (this, CMK_AssertionProp) BitVecTraits(assertionCount, this);
    apFull   = BitVecOps::MakeFull(apTraits);
}

/*****************************************************************************
 *
 *  Initialize the assertion prop tracking logic.
 */

void Compiler::optAssertionInit(bool isLocalProp)
{
    // Use a function countFunc to determine a proper maximum assertion count for the
    // method being compiled. The function is linear to the IL size for small and
    // moderate methods. For large methods, considering throughput impact, we track no
    // more than 64 assertions.
    // Note this tracks at most only 256 assertions.
    static const AssertionIndex countFunc[] = {64, 128, 256, 64};
    static const unsigned       lowerBound  = 0;
    static const unsigned       upperBound  = ArrLen(countFunc) - 1;
    const unsigned              codeSize    = info.compILCodeSize / 512;
    optMaxAssertionCount                    = countFunc[isLocalProp ? lowerBound : min(upperBound, codeSize)];

    optLocalAssertionProp  = isLocalProp;
    optAssertionTabPrivate = new (this, CMK_AssertionProp) AssertionDsc[optMaxAssertionCount];
    optComplementaryAssertionMap =
        new (this, CMK_AssertionProp) AssertionIndex[optMaxAssertionCount + 1](); // zero-inited (NO_ASSERTION_INDEX)
    assert(NO_ASSERTION_INDEX == 0);

    if (!isLocalProp)
    {
        optValueNumToAsserts =
            new (getAllocator(CMK_AssertionProp)) ValueNumToAssertsMap(getAllocator(CMK_AssertionProp));
    }

    if (optAssertionDep == nullptr)
    {
        optAssertionDep =
            new (this, CMK_AssertionProp) JitExpandArray<ASSERT_TP>(getAllocator(CMK_AssertionProp), max(1, lvaCount));
    }

    optAssertionTraitsInit(optMaxAssertionCount);
    optAssertionCount      = 0;
    optAssertionPropagated = false;
    bbJtrueAssertionOut    = nullptr;
}

#ifdef DEBUG
void Compiler::optPrintAssertion(AssertionDsc* curAssertion, AssertionIndex assertionIndex /* = 0 */)
{
    if (curAssertion->op1.kind == O1K_EXACT_TYPE)
    {
        printf("Type     ");
    }
    else if (curAssertion->op1.kind == O1K_ARR_BND)
    {
        printf("ArrBnds  ");
    }
    else if (curAssertion->op1.kind == O1K_SUBTYPE)
    {
        printf("Subtype  ");
    }
    else if (curAssertion->op2.kind == O2K_LCLVAR_COPY)
    {
        printf("Copy     ");
    }
    else if ((curAssertion->op2.kind == O2K_CONST_INT) || (curAssertion->op2.kind == O2K_CONST_LONG) ||
             (curAssertion->op2.kind == O2K_CONST_DOUBLE) || (curAssertion->op2.kind == O2K_ZEROOBJ))
    {
        printf("Constant ");
    }
    else if (curAssertion->op2.kind == O2K_SUBRANGE)
    {
        printf("Subrange ");
    }
    else
    {
        printf("?assertion classification? ");
    }
    printf("Assertion: ");

    if (!optLocalAssertionProp)
    {
        printf("(" FMT_VN "," FMT_VN ") ", curAssertion->op1.vn, curAssertion->op2.vn);
    }

    if ((curAssertion->op1.kind == O1K_LCLVAR) || (curAssertion->op1.kind == O1K_EXACT_TYPE) ||
        (curAssertion->op1.kind == O1K_SUBTYPE))
    {
        printf("V%02u", curAssertion->op1.lcl.lclNum);
        if (curAssertion->op1.lcl.ssaNum != SsaConfig::RESERVED_SSA_NUM)
        {
            printf(".%02u", curAssertion->op1.lcl.ssaNum);
        }
    }
    else if (curAssertion->op1.kind == O1K_ARR_BND)
    {
        printf("[idx:");
        vnStore->vnDump(this, curAssertion->op1.bnd.vnIdx);
        printf(";len:");
        vnStore->vnDump(this, curAssertion->op1.bnd.vnLen);
        printf("]");
    }
    else if (curAssertion->op1.kind == O1K_BOUND_OPER_BND)
    {
        printf("Oper_Bnd");
        vnStore->vnDump(this, curAssertion->op1.vn);
    }
    else if (curAssertion->op1.kind == O1K_BOUND_LOOP_BND)
    {
        printf("Loop_Bnd");
        vnStore->vnDump(this, curAssertion->op1.vn);
    }
    else if (curAssertion->op1.kind == O1K_CONSTANT_LOOP_BND)
    {
        printf("Const_Loop_Bnd");
        vnStore->vnDump(this, curAssertion->op1.vn);
    }
    else if (curAssertion->op1.kind == O1K_CONSTANT_LOOP_BND_UN)
    {
        printf("Const_Loop_Bnd_Un");
        vnStore->vnDump(this, curAssertion->op1.vn);
    }
    else if (curAssertion->op1.kind == O1K_VALUE_NUMBER)
    {
        printf("Value_Number");
        vnStore->vnDump(this, curAssertion->op1.vn);
    }
    else
    {
        printf("?op1.kind?");
    }

    if (curAssertion->assertionKind == OAK_SUBRANGE)
    {
        printf(" in ");
    }
    else if (curAssertion->assertionKind == OAK_EQUAL)
    {
        if (curAssertion->op1.kind == O1K_LCLVAR)
        {
            printf(" == ");
        }
        else
        {
            printf(" is ");
        }
    }
    else if (curAssertion->assertionKind == OAK_NO_THROW)
    {
        printf(" in range ");
    }
    else if (curAssertion->assertionKind == OAK_NOT_EQUAL)
    {
        if (curAssertion->op1.kind == O1K_LCLVAR)
        {
            printf(" != ");
        }
        else
        {
            printf(" is not ");
        }
    }
    else
    {
        printf(" ?assertionKind? ");
    }

    if (curAssertion->op1.kind != O1K_ARR_BND)
    {
        switch (curAssertion->op2.kind)
        {
            case O2K_LCLVAR_COPY:
                printf("V%02u", curAssertion->op2.lcl.lclNum);
                if (curAssertion->op1.lcl.ssaNum != SsaConfig::RESERVED_SSA_NUM)
                {
                    printf(".%02u", curAssertion->op1.lcl.ssaNum);
                }
                if (curAssertion->op2.zeroOffsetFieldSeq != nullptr)
                {
                    printf(" Zero");
                    gtDispFieldSeq(curAssertion->op2.zeroOffsetFieldSeq);
                }
                break;

            case O2K_CONST_INT:
            case O2K_IND_CNS_INT:
                if (curAssertion->op1.kind == O1K_EXACT_TYPE)
                {
                    printf("Exact Type MT(%08X)", dspPtr(curAssertion->op2.u1.iconVal));
                    assert(curAssertion->op2.u1.iconFlags != GTF_EMPTY);
                }
                else if (curAssertion->op1.kind == O1K_SUBTYPE)
                {
                    printf("MT(%08X)", dspPtr(curAssertion->op2.u1.iconVal));
                    assert(curAssertion->op2.u1.iconFlags != GTF_EMPTY);
                }
                else if ((curAssertion->op1.kind == O1K_BOUND_OPER_BND) ||
                         (curAssertion->op1.kind == O1K_BOUND_LOOP_BND) ||
                         (curAssertion->op1.kind == O1K_CONSTANT_LOOP_BND) ||
                         (curAssertion->op1.kind == O1K_CONSTANT_LOOP_BND_UN))
                {
                    assert(!optLocalAssertionProp);
                    vnStore->vnDump(this, curAssertion->op2.vn);
                }
                else
                {
                    var_types op1Type;

                    if (curAssertion->op1.kind == O1K_VALUE_NUMBER)
                    {
                        op1Type = vnStore->TypeOfVN(curAssertion->op1.vn);
                    }
                    else
                    {
                        unsigned lclNum = curAssertion->op1.lcl.lclNum;
                        op1Type         = lvaGetDesc(lclNum)->lvType;
                    }

                    if (op1Type == TYP_REF)
                    {
                        assert(curAssertion->op2.u1.iconVal == 0);
                        printf("null");
                    }
                    else
                    {
                        if ((curAssertion->op2.u1.iconFlags & GTF_ICON_HDL_MASK) != 0)
                        {
                            printf("[%08p]", dspPtr(curAssertion->op2.u1.iconVal));
                        }
                        else
                        {
                            printf("%d", curAssertion->op2.u1.iconVal);
                        }
                    }
                }
                break;

            case O2K_CONST_LONG:
                printf("0x%016llx", curAssertion->op2.lconVal);
                break;

            case O2K_CONST_DOUBLE:
                if (*((__int64*)&curAssertion->op2.dconVal) == (__int64)I64(0x8000000000000000))
                {
                    printf("-0.00000");
                }
                else
                {
                    printf("%#lg", curAssertion->op2.dconVal);
                }
                break;

            case O2K_ZEROOBJ:
                printf("ZeroObj");
                break;

            case O2K_SUBRANGE:
                IntegralRange::Print(curAssertion->op2.u2);
                break;

            default:
                printf("?op2.kind?");
                break;
        }
    }

    if (assertionIndex > 0)
    {
        printf(", index = ");
        optPrintAssertionIndex(assertionIndex);
    }
    printf("\n");
}

void Compiler::optPrintAssertionIndex(AssertionIndex index)
{
    if (index == NO_ASSERTION_INDEX)
    {
        printf("#NA");
        return;
    }

    printf("#%02u", index);
}

void Compiler::optPrintAssertionIndices(ASSERT_TP assertions)
{
    if (BitVecOps::IsEmpty(apTraits, assertions))
    {
        optPrintAssertionIndex(NO_ASSERTION_INDEX);
        return;
    }

    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        bitIndex = 0;
    if (iter.NextElem(&bitIndex))
    {
        optPrintAssertionIndex(static_cast<AssertionIndex>(bitIndex + 1));
        while (iter.NextElem(&bitIndex))
        {
            printf(" ");
            optPrintAssertionIndex(static_cast<AssertionIndex>(bitIndex + 1));
        }
    }
}
#endif // DEBUG

/* static */
void Compiler::optDumpAssertionIndices(const char* header, ASSERT_TP assertions, const char* footer /* = nullptr */)
{
#ifdef DEBUG
    Compiler* compiler = JitTls::GetCompiler();
    if (compiler->verbose)
    {
        printf(header);
        compiler->optPrintAssertionIndices(assertions);
        if (footer != nullptr)
        {
            printf(footer);
        }
    }
#endif // DEBUG
}

/* static */
void Compiler::optDumpAssertionIndices(ASSERT_TP assertions, const char* footer /* = nullptr */)
{
    optDumpAssertionIndices("", assertions, footer);
}

/******************************************************************************
 *
 * Helper to retrieve the "assertIndex" assertion. Note that assertIndex 0
 * is NO_ASSERTION_INDEX and "optAssertionCount" is the last valid index.
 *
 */
Compiler::AssertionDsc* Compiler::optGetAssertion(AssertionIndex assertIndex)
{
    assert(NO_ASSERTION_INDEX == 0);
    assert(assertIndex != NO_ASSERTION_INDEX);
    assert(assertIndex <= optAssertionCount);
    AssertionDsc* assertion = &optAssertionTabPrivate[assertIndex - 1];
#ifdef DEBUG
    optDebugCheckAssertion(assertion);
#endif

    return assertion;
}

//------------------------------------------------------------------------
// optCreateAssertion: Create an (op1 assertionKind op2) assertion.
//
// Arguments:
//    op1 - the first assertion operand
//    op2 - the second assertion operand
//    assertionKind - the assertion kind
//    helperCallArgs - when true this indicates that the assertion operands
//                     are the arguments of a type cast helper call such as
//                     CORINFO_HELP_ISINSTANCEOFCLASS
// Return Value:
//    The new assertion index or NO_ASSERTION_INDEX if a new assertion
//    was not created.
//
// Notes:
//    Assertion creation may fail either because the provided assertion
//    operands aren't supported or because the assertion table is full.
//
AssertionIndex Compiler::optCreateAssertion(GenTree*         op1,
                                            GenTree*         op2,
                                            optAssertionKind assertionKind,
                                            bool             helperCallArgs)
{
    assert(op1 != nullptr);
    assert(!helperCallArgs || (op2 != nullptr));

    AssertionDsc assertion = {OAK_INVALID};
    assert(assertion.assertionKind == OAK_INVALID);

    if (op1->OperIs(GT_BOUNDS_CHECK))
    {
        if (assertionKind == OAK_NO_THROW)
        {
            GenTreeBoundsChk* arrBndsChk = op1->AsBoundsChk();
            assertion.assertionKind      = assertionKind;
            assertion.op1.kind           = O1K_ARR_BND;
            assertion.op1.bnd.vnIdx      = vnStore->VNConservativeNormalValue(arrBndsChk->GetIndex()->gtVNPair);
            assertion.op1.bnd.vnLen      = vnStore->VNConservativeNormalValue(arrBndsChk->GetArrayLength()->gtVNPair);
            goto DONE_ASSERTION;
        }
    }

    //
    // Are we trying to make a non-null assertion?
    //
    if (op2 == nullptr)
    {
        //
        // Must be an OAK_NOT_EQUAL assertion
        //
        noway_assert(assertionKind == OAK_NOT_EQUAL);

        //
        // Set op1 to the instance pointer of the indirection
        //
        op1 = op1->gtEffectiveVal(/* commaOnly */ true);

        ssize_t offset = 0;
        while ((op1->gtOper == GT_ADD) && (op1->gtType == TYP_BYREF))
        {
            if (op1->gtGetOp2()->IsCnsIntOrI())
            {
                offset += op1->gtGetOp2()->AsIntCon()->gtIconVal;
                op1 = op1->gtGetOp1();
            }
            else if (op1->gtGetOp1()->IsCnsIntOrI())
            {
                offset += op1->gtGetOp1()->AsIntCon()->gtIconVal;
                op1 = op1->gtGetOp2();
            }
            else
            {
                break;
            }
        }

        if (fgIsBigOffset(offset) || op1->gtOper != GT_LCL_VAR)
        {
            goto DONE_ASSERTION; // Don't make an assertion
        }

        unsigned   lclNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* lclVar = lvaGetDesc(lclNum);

        ValueNum vn;

        //
        // We only perform null-checks on GC refs
        // so only make non-null assertions about GC refs or byrefs if we can't determine
        // the corresponding ref.
        //
        if (lclVar->TypeGet() != TYP_REF)
        {
            if (optLocalAssertionProp || (lclVar->TypeGet() != TYP_BYREF))
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            vn = vnStore->VNConservativeNormalValue(op1->gtVNPair);
            VNFuncApp funcAttr;

            // Try to get value number corresponding to the GC ref of the indirection
            while (vnStore->GetVNFunc(vn, &funcAttr) && (funcAttr.m_func == (VNFunc)GT_ADD) &&
                   (vnStore->TypeOfVN(vn) == TYP_BYREF))
            {
                if (vnStore->IsVNConstant(funcAttr.m_args[1]) &&
                    varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[1])))
                {
                    offset += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[1]);
                    vn = funcAttr.m_args[0];
                }
                else if (vnStore->IsVNConstant(funcAttr.m_args[0]) &&
                         varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[0])))
                {
                    offset += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[0]);
                    vn = funcAttr.m_args[1];
                }
                else
                {
                    break;
                }
            }

            if (fgIsBigOffset(offset))
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            assertion.op1.kind = O1K_VALUE_NUMBER;
        }
        else
        {
            //  If the local variable has its address exposed then bail
            if (lclVar->IsAddressExposed())
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            assertion.op1.kind       = O1K_LCLVAR;
            assertion.op1.lcl.lclNum = lclNum;
            assertion.op1.lcl.ssaNum = op1->AsLclVarCommon()->GetSsaNum();
            vn                       = vnStore->VNConservativeNormalValue(op1->gtVNPair);
        }

        assertion.op1.vn           = vn;
        assertion.assertionKind    = assertionKind;
        assertion.op2.kind         = O2K_CONST_INT;
        assertion.op2.vn           = ValueNumStore::VNForNull();
        assertion.op2.u1.iconVal   = 0;
        assertion.op2.u1.iconFlags = GTF_EMPTY;
    }
    //
    // Are we making an assertion about a local variable?
    //
    else if (op1->gtOper == GT_LCL_VAR)
    {
        unsigned   lclNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* lclVar = lvaGetDesc(lclNum);

        //  If the local variable has its address exposed then bail
        if (lclVar->IsAddressExposed())
        {
            goto DONE_ASSERTION; // Don't make an assertion
        }

        if (helperCallArgs)
        {
            //
            // Must either be an OAK_EQUAL or an OAK_NOT_EQUAL assertion
            //
            if ((assertionKind != OAK_EQUAL) && (assertionKind != OAK_NOT_EQUAL))
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            if (op2->gtOper == GT_IND)
            {
                op2                = op2->AsOp()->gtOp1;
                assertion.op2.kind = O2K_IND_CNS_INT;
            }
            else
            {
                assertion.op2.kind = O2K_CONST_INT;
            }

            if (op2->gtOper != GT_CNS_INT)
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            //
            // TODO-CQ: Check for Sealed class and change kind to O1K_EXACT_TYPE
            //          And consider the special cases, like CORINFO_FLG_SHAREDINST or CORINFO_FLG_VARIANCE
            //          where a class can be sealed, but they don't behave as exact types because casts to
            //          non-base types sometimes still succeed.
            //
            assertion.op1.kind         = O1K_SUBTYPE;
            assertion.op1.lcl.lclNum   = lclNum;
            assertion.op1.vn           = vnStore->VNConservativeNormalValue(op1->gtVNPair);
            assertion.op1.lcl.ssaNum   = op1->AsLclVarCommon()->GetSsaNum();
            assertion.op2.u1.iconVal   = op2->AsIntCon()->gtIconVal;
            assertion.op2.vn           = vnStore->VNConservativeNormalValue(op2->gtVNPair);
            assertion.op2.u1.iconFlags = op2->GetIconHandleFlag();

            //
            // Ok everything has been set and the assertion looks good
            //
            assertion.assertionKind = assertionKind;
        }
        else // !helperCallArgs
        {
            /* Skip over a GT_COMMA node(s), if necessary */
            while (op2->gtOper == GT_COMMA)
            {
                op2 = op2->AsOp()->gtOp2;
            }

            assertion.op1.kind       = O1K_LCLVAR;
            assertion.op1.lcl.lclNum = lclNum;
            assertion.op1.vn         = vnStore->VNConservativeNormalValue(op1->gtVNPair);
            assertion.op1.lcl.ssaNum = op1->AsLclVarCommon()->GetSsaNum();

            switch (op2->gtOper)
            {
                optOp2Kind op2Kind;

                //
                //  Constant Assertions
                //
                case GT_CNS_INT:
                    if (varTypeIsStruct(op1))
                    {
                        assert(op2->IsIntegralConst(0));
                        op2Kind = O2K_ZEROOBJ;
                    }
                    else
                    {
                        op2Kind = O2K_CONST_INT;
                    }
                    goto CNS_COMMON;

                case GT_CNS_LNG:
                    op2Kind = O2K_CONST_LONG;
                    goto CNS_COMMON;

                case GT_CNS_DBL:
                    op2Kind = O2K_CONST_DOUBLE;
                    goto CNS_COMMON;

                CNS_COMMON:
                {
                    //
                    // Must either be an OAK_EQUAL or an OAK_NOT_EQUAL assertion
                    //
                    if ((assertionKind != OAK_EQUAL) && (assertionKind != OAK_NOT_EQUAL))
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    // If the LclVar is a TYP_LONG then we only make
                    // assertions where op2 is also TYP_LONG
                    //
                    if ((lclVar->TypeGet() == TYP_LONG) && (op2->TypeGet() != TYP_LONG))
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    assertion.op2.kind    = op2Kind;
                    assertion.op2.lconVal = 0;
                    assertion.op2.vn      = vnStore->VNConservativeNormalValue(op2->gtVNPair);

                    if (op2->gtOper == GT_CNS_INT)
                    {
#ifdef TARGET_ARM
                        // Do not Constant-Prop large constants for ARM
                        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had
                        // target_ssize_t type.
                        if (!codeGen->validImmForMov((target_ssize_t)op2->AsIntCon()->gtIconVal))
                        {
                            goto DONE_ASSERTION; // Don't make an assertion
                        }
#endif // TARGET_ARM
                        assertion.op2.u1.iconVal   = op2->AsIntCon()->gtIconVal;
                        assertion.op2.u1.iconFlags = op2->GetIconHandleFlag();
                    }
                    else if (op2->gtOper == GT_CNS_LNG)
                    {
                        assertion.op2.lconVal = op2->AsLngCon()->gtLconVal;
                    }
                    else
                    {
                        noway_assert(op2->gtOper == GT_CNS_DBL);
                        /* If we have an NaN value then don't record it */
                        if (_isnan(op2->AsDblCon()->gtDconVal))
                        {
                            goto DONE_ASSERTION; // Don't make an assertion
                        }
                        assertion.op2.dconVal = op2->AsDblCon()->gtDconVal;
                    }

                    //
                    // Ok everything has been set and the assertion looks good
                    //
                    assertion.assertionKind = assertionKind;

                    goto DONE_ASSERTION;
                }

                //
                //  Copy Assertions
                //
                case GT_LCL_VAR:
                {
                    //
                    // Must either be an OAK_EQUAL or an OAK_NOT_EQUAL assertion
                    //
                    if ((assertionKind != OAK_EQUAL) && (assertionKind != OAK_NOT_EQUAL))
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    unsigned   lclNum2 = op2->AsLclVarCommon()->GetLclNum();
                    LclVarDsc* lclVar2 = lvaGetDesc(lclNum2);

                    // If the two locals are the same then bail
                    if (lclNum == lclNum2)
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    // If the types are different then bail */
                    if (lclVar->lvType != lclVar2->lvType)
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    // If we're making a copy of a "normalize on load" lclvar then the destination
                    // has to be "normalize on load" as well, otherwise we risk skipping normalization.
                    if (lclVar2->lvNormalizeOnLoad() && !lclVar->lvNormalizeOnLoad())
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    //  If the local variable has its address exposed then bail
                    if (lclVar2->IsAddressExposed())
                    {
                        goto DONE_ASSERTION; // Don't make an assertion
                    }

                    FieldSeqNode* zeroOffsetFieldSeq = nullptr;
                    GetZeroOffsetFieldMap()->Lookup(op2, &zeroOffsetFieldSeq);

                    assertion.op2.kind               = O2K_LCLVAR_COPY;
                    assertion.op2.vn                 = vnStore->VNConservativeNormalValue(op2->gtVNPair);
                    assertion.op2.lcl.lclNum         = lclNum2;
                    assertion.op2.lcl.ssaNum         = op2->AsLclVarCommon()->GetSsaNum();
                    assertion.op2.zeroOffsetFieldSeq = zeroOffsetFieldSeq;

                    // Ok everything has been set and the assertion looks good
                    assertion.assertionKind = assertionKind;

                    goto DONE_ASSERTION;
                }

                default:
                    break;
            }

            // Try and see if we can make a subrange assertion.
            if (((assertionKind == OAK_SUBRANGE) || (assertionKind == OAK_EQUAL)) && varTypeIsIntegral(op2))
            {
                IntegralRange nodeRange = IntegralRange::ForNode(op2, this);
                IntegralRange typeRange = IntegralRange::ForType(genActualType(op2));
                assert(typeRange.Contains(nodeRange));

                if (!typeRange.Equals(nodeRange))
                {
                    assertion.op2.kind      = O2K_SUBRANGE;
                    assertion.assertionKind = OAK_SUBRANGE;
                    assertion.op2.u2        = nodeRange;
                }
            }
        }
    }

    //
    // Are we making an IsType assertion?
    //
    else if (op1->gtOper == GT_IND)
    {
        op1 = op1->AsOp()->gtOp1;
        //
        // Is this an indirection of a local variable?
        //
        if (op1->gtOper == GT_LCL_VAR)
        {
            unsigned lclNum = op1->AsLclVarCommon()->GetLclNum();

            //  If the local variable is not in SSA then bail
            if (!lvaInSsa(lclNum))
            {
                goto DONE_ASSERTION;
            }

            // If we have an typeHnd indirection then op1 must be a TYP_REF
            //  and the indirection must produce a TYP_I
            //
            if (op1->gtType != TYP_REF)
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }

            assertion.op1.kind       = O1K_EXACT_TYPE;
            assertion.op1.lcl.lclNum = lclNum;
            assertion.op1.vn         = vnStore->VNConservativeNormalValue(op1->gtVNPair);
            assertion.op1.lcl.ssaNum = op1->AsLclVarCommon()->GetSsaNum();

            assert((assertion.op1.lcl.ssaNum == SsaConfig::RESERVED_SSA_NUM) ||
                   (assertion.op1.vn ==
                    vnStore->VNConservativeNormalValue(
                        lvaGetDesc(lclNum)->GetPerSsaData(assertion.op1.lcl.ssaNum)->m_vnPair)));

            ssize_t      cnsValue  = 0;
            GenTreeFlags iconFlags = GTF_EMPTY;
            // Ngen case
            if (op2->gtOper == GT_IND)
            {
                if (!optIsTreeKnownIntValue(!optLocalAssertionProp, op2->AsOp()->gtOp1, &cnsValue, &iconFlags))
                {
                    goto DONE_ASSERTION; // Don't make an assertion
                }

                assertion.assertionKind  = assertionKind;
                assertion.op2.kind       = O2K_IND_CNS_INT;
                assertion.op2.u1.iconVal = cnsValue;
                assertion.op2.vn         = vnStore->VNConservativeNormalValue(op2->AsOp()->gtOp1->gtVNPair);

                /* iconFlags should only contain bits in GTF_ICON_HDL_MASK */
                assert((iconFlags & ~GTF_ICON_HDL_MASK) == 0);
                assertion.op2.u1.iconFlags = iconFlags;
            }
            // JIT case
            else if (optIsTreeKnownIntValue(!optLocalAssertionProp, op2, &cnsValue, &iconFlags))
            {
                assertion.assertionKind  = assertionKind;
                assertion.op2.kind       = O2K_CONST_INT;
                assertion.op2.u1.iconVal = cnsValue;
                assertion.op2.vn         = vnStore->VNConservativeNormalValue(op2->gtVNPair);

                /* iconFlags should only contain bits in GTF_ICON_HDL_MASK */
                assert((iconFlags & ~GTF_ICON_HDL_MASK) == 0);
                assertion.op2.u1.iconFlags = iconFlags;
            }
            else
            {
                goto DONE_ASSERTION; // Don't make an assertion
            }
        }
    }

DONE_ASSERTION:
    return optFinalizeCreatingAssertion(&assertion);
}

//------------------------------------------------------------------------
// optFinalizeCreatingAssertion: Add the assertion, if well-formed, to the table.
//
// Checks that in global assertion propagation assertions do not have missing
// value and SSA numbers.
//
// Arguments:
//    assertion - assertion to check and add to the table
//
// Return Value:
//    Index of the assertion if it was successfully created, NO_ASSERTION_INDEX otherwise.
//
AssertionIndex Compiler::optFinalizeCreatingAssertion(AssertionDsc* assertion)
{
    if (assertion->assertionKind == OAK_INVALID)
    {
        return NO_ASSERTION_INDEX;
    }

    if (!optLocalAssertionProp)
    {
        if ((assertion->op1.vn == ValueNumStore::NoVN) || (assertion->op2.vn == ValueNumStore::NoVN) ||
            (assertion->op1.vn == ValueNumStore::VNForVoid()) || (assertion->op2.vn == ValueNumStore::VNForVoid()))
        {
            return NO_ASSERTION_INDEX;
        }

        // TODO: only copy assertions rely on valid SSA number so we could generate more assertions here
        if ((assertion->op1.kind != O1K_VALUE_NUMBER) && (assertion->op1.lcl.ssaNum == SsaConfig::RESERVED_SSA_NUM))
        {
            return NO_ASSERTION_INDEX;
        }
    }

    // Now add the assertion to our assertion table
    noway_assert(assertion->op1.kind != O1K_INVALID);
    noway_assert((assertion->op1.kind == O1K_ARR_BND) || (assertion->op2.kind != O2K_INVALID));

    return optAddAssertion(assertion);
}

/*****************************************************************************
 *
 * If tree is a constant node holding an integral value, retrieve the value in
 * pConstant. If the method returns true, pConstant holds the appropriate
 * constant. Set "vnBased" to true to indicate local or global assertion prop.
 * "pFlags" indicates if the constant is a handle marked by GTF_ICON_HDL_MASK.
 */
bool Compiler::optIsTreeKnownIntValue(bool vnBased, GenTree* tree, ssize_t* pConstant, GenTreeFlags* pFlags)
{
    // Is Local assertion prop?
    if (!vnBased)
    {
        if (tree->OperGet() == GT_CNS_INT)
        {
            *pConstant = tree->AsIntCon()->IconValue();
            *pFlags    = tree->GetIconHandleFlag();
            return true;
        }
#ifdef TARGET_64BIT
        // Just to be clear, get it from gtLconVal rather than
        // overlapping gtIconVal.
        else if (tree->OperGet() == GT_CNS_LNG)
        {
            *pConstant = tree->AsLngCon()->gtLconVal;
            *pFlags    = tree->GetIconHandleFlag();
            return true;
        }
#endif
        return false;
    }

    // Global assertion prop
    ValueNum vn = vnStore->VNConservativeNormalValue(tree->gtVNPair);
    if (!vnStore->IsVNConstant(vn))
    {
        return false;
    }

    // ValueNumber 'vn' indicates that this node evaluates to a constant

    var_types vnType = vnStore->TypeOfVN(vn);
    if (vnType == TYP_INT)
    {
        *pConstant = vnStore->ConstantValue<int>(vn);
        *pFlags    = vnStore->IsVNHandle(vn) ? vnStore->GetHandleFlags(vn) : GTF_EMPTY;
        return true;
    }
#ifdef TARGET_64BIT
    else if (vnType == TYP_LONG)
    {
        *pConstant = vnStore->ConstantValue<INT64>(vn);
        *pFlags    = vnStore->IsVNHandle(vn) ? vnStore->GetHandleFlags(vn) : GTF_EMPTY;
        return true;
    }
#endif
    return false;
}

#ifdef DEBUG
/*****************************************************************************
 *
 * Print the assertions related to a VN for all VNs.
 *
 */
void Compiler::optPrintVnAssertionMapping()
{
    printf("\nVN Assertion Mapping\n");
    printf("---------------------\n");
    for (ValueNumToAssertsMap::KeyIterator ki = optValueNumToAsserts->Begin(); !ki.Equal(optValueNumToAsserts->End());
         ++ki)
    {
        printf("(%d => ", ki.Get());
        printf("%s)\n", BitVecOps::ToString(apTraits, ki.GetValue()));
    }
}
#endif

/*****************************************************************************
 *
 * Maintain a map "optValueNumToAsserts" i.e., vn -> to set of assertions
 * about that VN. Given "assertions" about a "vn" add it to the previously
 * mapped assertions about that "vn."
 */
void Compiler::optAddVnAssertionMapping(ValueNum vn, AssertionIndex index)
{
    ASSERT_TP* cur = optValueNumToAsserts->LookupPointer(vn);
    if (cur == nullptr)
    {
        optValueNumToAsserts->Set(vn, BitVecOps::MakeSingleton(apTraits, index - 1));
    }
    else
    {
        BitVecOps::AddElemD(apTraits, *cur, index - 1);
    }
}

/*****************************************************************************
 * Statically if we know that this assertion's VN involves a NaN don't bother
 * wasting an assertion table slot.
 */
bool Compiler::optAssertionVnInvolvesNan(AssertionDsc* assertion)
{
    if (optLocalAssertionProp)
    {
        return false;
    }

    static const int SZ      = 2;
    ValueNum         vns[SZ] = {assertion->op1.vn, assertion->op2.vn};
    for (int i = 0; i < SZ; ++i)
    {
        if (vnStore->IsVNConstant(vns[i]))
        {
            var_types type = vnStore->TypeOfVN(vns[i]);
            if ((type == TYP_FLOAT && _isnan(vnStore->ConstantValue<float>(vns[i])) != 0) ||
                (type == TYP_DOUBLE && _isnan(vnStore->ConstantValue<double>(vns[i])) != 0))
            {
                return true;
            }
        }
    }
    return false;
}

/*****************************************************************************
 *
 *  Given an assertion add it to the assertion table
 *
 *  If it is already in the assertion table return the assertionIndex that
 *  we use to refer to this element.
 *  Otherwise add it to the assertion table ad return the assertionIndex that
 *  we use to refer to this element.
 *  If we need to add to the table and the table is full return the value zero
 */
AssertionIndex Compiler::optAddAssertion(AssertionDsc* newAssertion)
{
    noway_assert(newAssertion->assertionKind != OAK_INVALID);

    // Even though the propagation step takes care of NaN, just a check
    // to make sure there is no slot involving a NaN.
    if (optAssertionVnInvolvesNan(newAssertion))
    {
        JITDUMP("Assertion involved Nan not adding\n");
        return NO_ASSERTION_INDEX;
    }

    // Check if exists already, so we can skip adding new one. Search backwards.
    for (AssertionIndex index = optAssertionCount; index >= 1; index--)
    {
        AssertionDsc* curAssertion = optGetAssertion(index);
        if (curAssertion->Equals(newAssertion, !optLocalAssertionProp))
        {
            return index;
        }
    }

    // Check if we are within max count.
    if (optAssertionCount >= optMaxAssertionCount)
    {
        return NO_ASSERTION_INDEX;
    }

    optAssertionTabPrivate[optAssertionCount] = *newAssertion;
    optAssertionCount++;

#ifdef DEBUG
    if (verbose)
    {
        printf("GenTreeNode creates assertion:\n");
        gtDispTree(optAssertionPropCurrentTree, nullptr, nullptr, true);
        printf(optLocalAssertionProp ? "In " FMT_BB " New Local " : "In " FMT_BB " New Global ", compCurBB->bbNum);
        optPrintAssertion(newAssertion, optAssertionCount);
    }
#endif // DEBUG

    // Assertion mask bits are [index + 1].
    if (optLocalAssertionProp)
    {
        assert(newAssertion->op1.kind == O1K_LCLVAR);

        // Mark the variables this index depends on
        unsigned lclNum = newAssertion->op1.lcl.lclNum;
        BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), optAssertionCount - 1);
        if (newAssertion->op2.kind == O2K_LCLVAR_COPY)
        {
            lclNum = newAssertion->op2.lcl.lclNum;
            BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), optAssertionCount - 1);
        }
    }
    else
    // If global assertion prop, then add it to the dependents map.
    {
        optAddVnAssertionMapping(newAssertion->op1.vn, optAssertionCount);
        if (newAssertion->op2.kind == O2K_LCLVAR_COPY)
        {
            optAddVnAssertionMapping(newAssertion->op2.vn, optAssertionCount);
        }
    }

#ifdef DEBUG
    optDebugCheckAssertions(optAssertionCount);
#endif
    return optAssertionCount;
}

#ifdef DEBUG
void Compiler::optDebugCheckAssertion(AssertionDsc* assertion)
{
    assert(assertion->assertionKind < OAK_COUNT);
    assert(assertion->op1.kind < O1K_COUNT);
    assert(assertion->op2.kind < O2K_COUNT);
    // It would be good to check that op1.vn and op2.vn are valid value numbers.

    switch (assertion->op1.kind)
    {
        case O1K_LCLVAR:
        case O1K_EXACT_TYPE:
        case O1K_SUBTYPE:
            assert(optLocalAssertionProp ||
                   lvaGetDesc(assertion->op1.lcl.lclNum)->lvPerSsaData.IsValidSsaNum(assertion->op1.lcl.ssaNum));
            break;
        case O1K_ARR_BND:
            // It would be good to check that bnd.vnIdx and bnd.vnLen are valid value numbers.
            break;
        case O1K_BOUND_OPER_BND:
        case O1K_BOUND_LOOP_BND:
        case O1K_CONSTANT_LOOP_BND:
        case O1K_CONSTANT_LOOP_BND_UN:
        case O1K_VALUE_NUMBER:
            assert(!optLocalAssertionProp);
            break;
        default:
            break;
    }
    switch (assertion->op2.kind)
    {
        case O2K_IND_CNS_INT:
        case O2K_CONST_INT:
        {
            // The only flags that can be set are those in the GTF_ICON_HDL_MASK.
            assert((assertion->op2.u1.iconFlags & ~GTF_ICON_HDL_MASK) == 0);

            switch (assertion->op1.kind)
            {
                case O1K_EXACT_TYPE:
                case O1K_SUBTYPE:
                    assert(assertion->op2.u1.iconFlags != GTF_EMPTY);
                    break;
                case O1K_LCLVAR:
                    assert((lvaGetDesc(assertion->op1.lcl.lclNum)->lvType != TYP_REF) ||
                           (assertion->op2.u1.iconVal == 0) || doesMethodHaveFrozenString());
                    break;
                case O1K_VALUE_NUMBER:
                    assert((vnStore->TypeOfVN(assertion->op1.vn) != TYP_REF) || (assertion->op2.u1.iconVal == 0));
                    break;
                default:
                    break;
            }
        }
        break;

        case O2K_CONST_LONG:
        {
            // All handles should be represented by O2K_CONST_INT,
            // so no handle bits should be set here.
            assert((assertion->op2.u1.iconFlags & GTF_ICON_HDL_MASK) == 0);
        }
        break;

        case O2K_ZEROOBJ:
        {
            // We only make these assertion for assignments (not control flow).
            assert(assertion->assertionKind == OAK_EQUAL);
            // We use "optLocalAssertionIsEqualOrNotEqual" to find these.
            assert(assertion->op2.u1.iconVal == 0);
        }
        break;

        default:
            // for all other 'assertion->op2.kind' values we don't check anything
            break;
    }
}

/*****************************************************************************
 *
 *  Verify that assertion prop related assumptions are valid. If "index"
 *  is 0 (i.e., NO_ASSERTION_INDEX) then verify all assertions in the table.
 *  If "index" is between 1 and optAssertionCount, then verify the assertion
 *  desc corresponding to "index."
 */
void Compiler::optDebugCheckAssertions(AssertionIndex index)
{
    AssertionIndex start = (index == NO_ASSERTION_INDEX) ? 1 : index;
    AssertionIndex end   = (index == NO_ASSERTION_INDEX) ? optAssertionCount : index;
    for (AssertionIndex ind = start; ind <= end; ++ind)
    {
        AssertionDsc* assertion = optGetAssertion(ind);
        optDebugCheckAssertion(assertion);
    }
}
#endif

//------------------------------------------------------------------------
// optCreateComplementaryAssertion: Create an assertion that is the complementary
//     of the specified assertion.
//
// Arguments:
//    assertionIndex - the index of the assertion
//    op1 - the first assertion operand
//    op2 - the second assertion operand
//    helperCallArgs - when true this indicates that the assertion operands
//                     are the arguments of a type cast helper call such as
//                     CORINFO_HELP_ISINSTANCEOFCLASS
//
// Notes:
//    The created complementary assertion is associated with the original
//    assertion such that it can be found by optFindComplementary.
//
void Compiler::optCreateComplementaryAssertion(AssertionIndex assertionIndex,
                                               GenTree*       op1,
                                               GenTree*       op2,
                                               bool           helperCallArgs)
{
    if (assertionIndex == NO_ASSERTION_INDEX)
    {
        return;
    }

    AssertionDsc& candidateAssertion = *optGetAssertion(assertionIndex);
    if ((candidateAssertion.op1.kind == O1K_BOUND_OPER_BND) || (candidateAssertion.op1.kind == O1K_BOUND_LOOP_BND) ||
        (candidateAssertion.op1.kind == O1K_CONSTANT_LOOP_BND) ||
        (candidateAssertion.op1.kind == O1K_CONSTANT_LOOP_BND_UN))
    {
        AssertionDsc dsc  = candidateAssertion;
        dsc.assertionKind = dsc.assertionKind == OAK_EQUAL ? OAK_NOT_EQUAL : OAK_EQUAL;
        optAddAssertion(&dsc);
        return;
    }

    if (candidateAssertion.assertionKind == OAK_EQUAL)
    {
        AssertionIndex index = optCreateAssertion(op1, op2, OAK_NOT_EQUAL, helperCallArgs);
        optMapComplementary(index, assertionIndex);
    }
    else if (candidateAssertion.assertionKind == OAK_NOT_EQUAL)
    {
        AssertionIndex index = optCreateAssertion(op1, op2, OAK_EQUAL, helperCallArgs);
        optMapComplementary(index, assertionIndex);
    }

    // Are we making a subtype or exact type assertion?
    if ((candidateAssertion.op1.kind == O1K_SUBTYPE) || (candidateAssertion.op1.kind == O1K_EXACT_TYPE))
    {
        optCreateAssertion(op1, nullptr, OAK_NOT_EQUAL);
    }
}

// optAssertionGenCast: Create a tentative subrange assertion for a cast.
//
// This function will try to create an assertion that the cast's operand
// is within the "input" range for the cast, so that this assertion can
// later be proven via implication and the cast removed. Such assertions
// are only generated during global propagation, and only for LCL_VARs.
//
// Arguments:
//    cast - the cast node for which to create the assertion
//
// Return Value:
//    Index of the generated assertion, or NO_ASSERTION_INDEX if it was not
//    legal, profitable, or possible to create one.
//
AssertionIndex Compiler::optAssertionGenCast(GenTreeCast* cast)
{
    if (optLocalAssertionProp || !varTypeIsIntegral(cast) || !varTypeIsIntegral(cast->CastOp()))
    {
        return NO_ASSERTION_INDEX;
    }

    // This condition exists to preverve previous behavior.
    if (!cast->CastOp()->OperIs(GT_LCL_VAR))
    {
        return NO_ASSERTION_INDEX;
    }

    GenTreeLclVar* lclVar = cast->CastOp()->AsLclVar();
    LclVarDsc*     varDsc = lvaGetDesc(lclVar);

    // It is not useful to make assertions about address-exposed variables, they will never be proven.
    if (varDsc->IsAddressExposed())
    {
        return NO_ASSERTION_INDEX;
    }

    // A representation-changing cast cannot be simplified if it is not checked.
    if (!cast->gtOverflow() && (genActualType(cast) != genActualType(lclVar)))
    {
        return NO_ASSERTION_INDEX;
    }

    AssertionDsc assertion   = {OAK_SUBRANGE};
    assertion.op1.kind       = O1K_LCLVAR;
    assertion.op1.vn         = vnStore->VNConservativeNormalValue(lclVar->gtVNPair);
    assertion.op1.lcl.lclNum = lclVar->GetLclNum();
    assertion.op1.lcl.ssaNum = lclVar->GetSsaNum();
    assertion.op2.kind       = O2K_SUBRANGE;
    assertion.op2.u2         = IntegralRange::ForCastInput(cast);

    return optFinalizeCreatingAssertion(&assertion);
}

//------------------------------------------------------------------------
// optCreateJtrueAssertions: Create assertions about a JTRUE's relop operands.
//
// Arguments:
//    op1 - the first assertion operand
//    op2 - the second assertion operand
//    assertionKind - the assertion kind
//    helperCallArgs - when true this indicates that the assertion operands
//                     are the arguments of a type cast helper call such as
//                     CORINFO_HELP_ISINSTANCEOFCLASS
// Return Value:
//    The new assertion index or NO_ASSERTION_INDEX if a new assertion
//    was not created.
//
// Notes:
//    Assertion creation may fail either because the provided assertion
//    operands aren't supported or because the assertion table is full.
//    If an assertion is created succesfully then an attempt is made to also
//    create a second, complementary assertion. This may too fail, for the
//    same reasons as the first one.
//
AssertionIndex Compiler::optCreateJtrueAssertions(GenTree*                   op1,
                                                  GenTree*                   op2,
                                                  Compiler::optAssertionKind assertionKind,
                                                  bool                       helperCallArgs)
{
    AssertionIndex assertionIndex = optCreateAssertion(op1, op2, assertionKind, helperCallArgs);
    // Don't bother if we don't have an assertion on the JTrue False path. Current implementation
    // allows for a complementary only if there is an assertion on the False path (tree->HasAssertion()).
    if (assertionIndex != NO_ASSERTION_INDEX)
    {
        optCreateComplementaryAssertion(assertionIndex, op1, op2, helperCallArgs);
    }
    return assertionIndex;
}

AssertionInfo Compiler::optCreateJTrueBoundsAssertion(GenTree* tree)
{
    GenTree* relop = tree->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        return NO_ASSERTION_INDEX;
    }
    GenTree* op1 = relop->gtGetOp1();
    GenTree* op2 = relop->gtGetOp2();

    ValueNum op1VN   = vnStore->VNConservativeNormalValue(op1->gtVNPair);
    ValueNum op2VN   = vnStore->VNConservativeNormalValue(op2->gtVNPair);
    ValueNum relopVN = vnStore->VNConservativeNormalValue(relop->gtVNPair);

    bool hasTestAgainstZero =
        (relop->gtOper == GT_EQ || relop->gtOper == GT_NE) && (op2VN == vnStore->VNZeroForType(op2->TypeGet()));

    ValueNumStore::UnsignedCompareCheckedBoundInfo unsignedCompareBnd;
    // Cases where op1 holds the upper bound arithmetic and op2 is 0.
    // Loop condition like: "i < bnd +/-k == 0"
    // Assertion: "i < bnd +/- k == 0"
    if (hasTestAgainstZero && vnStore->IsVNCompareCheckedBoundArith(op1VN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = relop->gtOper == GT_EQ ? OAK_EQUAL : OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_BOUND_OPER_BND;
        dsc.op1.vn           = op1VN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(op2->TypeGet());
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    // Cases where op1 holds the lhs of the condition and op2 holds the bound arithmetic.
    // Loop condition like: "i < bnd +/-k"
    // Assertion: "i < bnd +/- k != 0"
    else if (vnStore->IsVNCompareCheckedBoundArith(relopVN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_BOUND_OPER_BND;
        dsc.op1.vn           = relopVN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(op2->TypeGet());
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    // Cases where op1 holds the upper bound and op2 is 0.
    // Loop condition like: "i < bnd == 0"
    // Assertion: "i < bnd == false"
    else if (hasTestAgainstZero && vnStore->IsVNCompareCheckedBound(op1VN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = relop->gtOper == GT_EQ ? OAK_EQUAL : OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_BOUND_LOOP_BND;
        dsc.op1.vn           = op1VN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(op2->TypeGet());
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    // Cases where op1 holds the lhs of the condition op2 holds the bound.
    // Loop condition like "i < bnd"
    // Assertion: "i < bnd != 0"
    else if (vnStore->IsVNCompareCheckedBound(relopVN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_BOUND_LOOP_BND;
        dsc.op1.vn           = relopVN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(TYP_INT);
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    // Loop condition like "(uint)i < (uint)bnd" or equivalent
    // Assertion: "no throw" since this condition guarantees that i is both >= 0 and < bnd (on the appropiate edge)
    else if (vnStore->IsVNUnsignedCompareCheckedBound(relopVN, &unsignedCompareBnd))
    {
        assert(unsignedCompareBnd.vnIdx != ValueNumStore::NoVN);
        assert((unsignedCompareBnd.cmpOper == VNF_LT_UN) || (unsignedCompareBnd.cmpOper == VNF_GE_UN));
        assert(vnStore->IsVNCheckedBound(unsignedCompareBnd.vnBound));

        AssertionDsc dsc;
        dsc.assertionKind = OAK_NO_THROW;
        dsc.op1.kind      = O1K_ARR_BND;
        dsc.op1.vn        = relopVN;
        dsc.op1.bnd.vnIdx = unsignedCompareBnd.vnIdx;
        dsc.op1.bnd.vnLen = vnStore->VNNormalValue(unsignedCompareBnd.vnBound);
        dsc.op2.kind      = O2K_INVALID;
        dsc.op2.vn        = ValueNumStore::NoVN;

        AssertionIndex index = optAddAssertion(&dsc);
        if (unsignedCompareBnd.cmpOper == VNF_GE_UN)
        {
            // By default JTRUE generated assertions hold on the "jump" edge. We have i >= bnd but we're really
            // after i < bnd so we need to change the assertion edge to "next".
            return AssertionInfo::ForNextEdge(index);
        }
        return index;
    }
    // Cases where op1 holds the condition bound check and op2 is 0.
    // Loop condition like: "i < 100 == 0"
    // Assertion: "i < 100 == false"
    else if (hasTestAgainstZero && vnStore->IsVNConstantBound(op1VN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = relop->gtOper == GT_EQ ? OAK_EQUAL : OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_CONSTANT_LOOP_BND;
        dsc.op1.vn           = op1VN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(op2->TypeGet());
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    // Cases where op1 holds the lhs of the condition op2 holds rhs.
    // Loop condition like "i < 100"
    // Assertion: "i < 100 != 0"
    else if (vnStore->IsVNConstantBound(relopVN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_CONSTANT_LOOP_BND;
        dsc.op1.vn           = relopVN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(TYP_INT);
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    else if (vnStore->IsVNConstantBoundUnsigned(relopVN))
    {
        AssertionDsc dsc;
        dsc.assertionKind    = OAK_NOT_EQUAL;
        dsc.op1.kind         = O1K_CONSTANT_LOOP_BND_UN;
        dsc.op1.vn           = relopVN;
        dsc.op2.kind         = O2K_CONST_INT;
        dsc.op2.vn           = vnStore->VNZeroForType(TYP_INT);
        dsc.op2.u1.iconVal   = 0;
        dsc.op2.u1.iconFlags = GTF_EMPTY;
        AssertionIndex index = optAddAssertion(&dsc);
        optCreateComplementaryAssertion(index, nullptr, nullptr);
        return index;
    }
    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  Compute assertions for the JTrue node.
 */
AssertionInfo Compiler::optAssertionGenJtrue(GenTree* tree)
{
    // Only create assertions for JTRUE when we are in the global phase
    if (optLocalAssertionProp)
    {
        return NO_ASSERTION_INDEX;
    }

    GenTree* relop = tree->AsOp()->gtOp1;
    if (!relop->OperIsCompare())
    {
        return NO_ASSERTION_INDEX;
    }

    Compiler::optAssertionKind assertionKind = OAK_INVALID;

    AssertionInfo info = optCreateJTrueBoundsAssertion(tree);
    if (info.HasAssertion())
    {
        return info;
    }

    // Find assertion kind.
    switch (relop->gtOper)
    {
        case GT_EQ:
            assertionKind = OAK_EQUAL;
            break;
        case GT_NE:
            assertionKind = OAK_NOT_EQUAL;
            break;
        default:
            // TODO-CQ: add other relop operands. Disabled for now to measure perf
            // and not occupy assertion table slots. We'll add them when used.
            return NO_ASSERTION_INDEX;
    }

    // Look through any CSEs so we see the actual trees providing values, if possible.
    // This is important for exact type assertions, which need to see the GT_IND.
    //
    GenTree* op1 = relop->AsOp()->gtOp1->gtCommaAssignVal();
    GenTree* op2 = relop->AsOp()->gtOp2->gtCommaAssignVal();

    // Check for op1 or op2 to be lcl var and if so, keep it in op1.
    if ((op1->gtOper != GT_LCL_VAR) && (op2->gtOper == GT_LCL_VAR))
    {
        std::swap(op1, op2);
    }

    ValueNum op1VN = vnStore->VNConservativeNormalValue(op1->gtVNPair);
    ValueNum op2VN = vnStore->VNConservativeNormalValue(op2->gtVNPair);
    // If op1 is lcl and op2 is const or lcl, create assertion.
    if ((op1->gtOper == GT_LCL_VAR) && (op2->OperIsConst() || (op2->gtOper == GT_LCL_VAR))) // Fix for Dev10 851483
    {
        return optCreateJtrueAssertions(op1, op2, assertionKind);
    }
    else if (vnStore->IsVNCheckedBound(op1VN) && vnStore->IsVNInt32Constant(op2VN))
    {
        assert(relop->OperIs(GT_EQ, GT_NE));

        int con = vnStore->ConstantValue<int>(op2VN);
        if (con >= 0)
        {
            AssertionDsc dsc;

            // For arr.Length != 0, we know that 0 is a valid index
            // For arr.Length == con, we know that con - 1 is the greatest valid index
            if (con == 0)
            {
                dsc.assertionKind = OAK_NOT_EQUAL;
                dsc.op1.bnd.vnIdx = vnStore->VNForIntCon(0);
            }
            else
            {
                dsc.assertionKind = OAK_EQUAL;
                dsc.op1.bnd.vnIdx = vnStore->VNForIntCon(con - 1);
            }

            dsc.op1.vn           = op1VN;
            dsc.op1.kind         = O1K_ARR_BND;
            dsc.op1.bnd.vnLen    = op1VN;
            dsc.op2.vn           = vnStore->VNConservativeNormalValue(op2->gtVNPair);
            dsc.op2.kind         = O2K_CONST_INT;
            dsc.op2.u1.iconFlags = GTF_EMPTY;
            dsc.op2.u1.iconVal   = 0;

            // when con is not zero, create an assertion on the arr.Length == con edge
            // when con is zero, create an assertion on the arr.Length != 0 edge
            AssertionIndex index = optAddAssertion(&dsc);
            if (relop->OperIs(GT_NE) != (con == 0))
            {
                return AssertionInfo::ForNextEdge(index);
            }
            else
            {
                return index;
            }
        }
    }

    // Check op1 and op2 for an indirection of a GT_LCL_VAR and keep it in op1.
    if (((op1->gtOper != GT_IND) || (op1->AsOp()->gtOp1->gtOper != GT_LCL_VAR)) &&
        ((op2->gtOper == GT_IND) && (op2->AsOp()->gtOp1->gtOper == GT_LCL_VAR)))
    {
        std::swap(op1, op2);
    }
    // If op1 is ind, then extract op1's oper.
    if ((op1->gtOper == GT_IND) && (op1->AsOp()->gtOp1->gtOper == GT_LCL_VAR))
    {
        return optCreateJtrueAssertions(op1, op2, assertionKind);
    }

    // Look for a call to an IsInstanceOf helper compared to a nullptr
    if ((op2->gtOper != GT_CNS_INT) && (op1->gtOper == GT_CNS_INT))
    {
        std::swap(op1, op2);
    }
    // Validate op1 and op2
    if ((op1->gtOper != GT_CALL) || (op1->AsCall()->gtCallType != CT_HELPER) || (op1->TypeGet() != TYP_REF) || // op1
        (op2->gtOper != GT_CNS_INT) || (op2->AsIntCon()->gtIconVal != 0))                                      // op2
    {
        return NO_ASSERTION_INDEX;
    }

    GenTreeCall* call = op1->AsCall();

    // Note CORINFO_HELP_READYTORUN_ISINSTANCEOF does not have the same argument pattern.
    // In particular, it is not possible to deduce what class is being tested from its args.
    //
    // Also note The CASTCLASS helpers won't appear in predicates as they throw on failure.
    // So the helper list here is smaller than the one in optAssertionProp_Call.
    if ((call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFINTERFACE)) ||
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFARRAY)) ||
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFCLASS)) ||
        (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFANY)))
    {
        GenTree* objectNode      = call->gtArgs.GetArgByIndex(1)->GetNode();
        GenTree* methodTableNode = call->gtArgs.GetArgByIndex(0)->GetNode();

        assert(objectNode->TypeGet() == TYP_REF);
        assert(methodTableNode->TypeGet() == TYP_I_IMPL);

        // Reverse the assertion
        assert((assertionKind == OAK_EQUAL) || (assertionKind == OAK_NOT_EQUAL));
        assertionKind = (assertionKind == OAK_EQUAL) ? OAK_NOT_EQUAL : OAK_EQUAL;

        if (objectNode->OperIs(GT_LCL_VAR))
        {
            return optCreateJtrueAssertions(objectNode, methodTableNode, assertionKind, /* helperCallArgs */ true);
        }
    }

    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  Create an assertion on the phi node if some information can be gleaned
 *  from all of the constituent phi operands.
 *
 */
AssertionIndex Compiler::optAssertionGenPhiDefn(GenTree* tree)
{
    if (!tree->IsPhiDefn())
    {
        return NO_ASSERTION_INDEX;
    }

    // Try to find if all phi arguments are known to be non-null.
    bool isNonNull = true;
    for (GenTreePhi::Use& use : tree->AsOp()->gtGetOp2()->AsPhi()->Uses())
    {
        if (!vnStore->IsKnownNonNull(use.GetNode()->gtVNPair.GetConservative()))
        {
            isNonNull = false;
            break;
        }
    }

    // All phi arguments are non-null implies phi rhs is non-null.
    if (isNonNull)
    {
        return optCreateAssertion(tree->AsOp()->gtOp1, nullptr, OAK_NOT_EQUAL);
    }
    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  If this statement creates a value assignment or assertion
 *  then assign an index to the given value assignment by adding
 *  it to the lookup table, if necessary.
 */
void Compiler::optAssertionGen(GenTree* tree)
{
    tree->ClearAssertion();

    // If there are QMARKs in the IR, we won't generate assertions
    // for conditionally executed code.
    //
    if (optLocalAssertionProp && ((tree->gtFlags & GTF_COLON_COND) != 0))
    {
        return;
    }

#ifdef DEBUG
    optAssertionPropCurrentTree = tree;
#endif

    // For most of the assertions that we create below
    // the assertion is true after the tree is processed
    bool          assertionProven = true;
    AssertionInfo assertionInfo;
    switch (tree->gtOper)
    {
        case GT_ASG:
            // An indirect store - we can create a non-null assertion. Note that we do not lose out
            // on the dataflow assertions here as local propagation only deals with LCL_VAR LHSs.
            if (tree->AsOp()->gtGetOp1()->OperIsIndir())
            {
                assertionInfo = optCreateAssertion(tree->AsOp()->gtGetOp1()->AsIndir()->Addr(), nullptr, OAK_NOT_EQUAL);
            }
            // VN takes care of non local assertions for assignments and data flow.
            else if (optLocalAssertionProp)
            {
                assertionInfo = optCreateAssertion(tree->AsOp()->gtOp1, tree->AsOp()->gtOp2, OAK_EQUAL);
            }
            else
            {
                assertionInfo = optAssertionGenPhiDefn(tree);
            }
            break;

        case GT_OBJ:
        case GT_BLK:
        case GT_IND:
            // R-value indirections create non-null assertions, but not all indirections are R-values.
            // Those under ADDR nodes or on the LHS of ASGs are "locations", and will not end up
            // dereferencing their operands. We cannot reliably detect them here, however, and so
            // will have to rely on the conservative approximation of the GTF_NO_CSE flag.
            if (tree->CanCSE())
            {
                assertionInfo = optCreateAssertion(tree->AsIndir()->Addr(), nullptr, OAK_NOT_EQUAL);
            }
            break;

        case GT_ARR_LENGTH:
            // An array length is an (always R-value) indirection (but doesn't derive from GenTreeIndir).
            assertionInfo = optCreateAssertion(tree->AsArrLen()->ArrRef(), nullptr, OAK_NOT_EQUAL);
            break;

        case GT_NULLCHECK:
            // Explicit null checks always create non-null assertions.
            assertionInfo = optCreateAssertion(tree->AsIndir()->Addr(), nullptr, OAK_NOT_EQUAL);
            break;

        case GT_INTRINSIC:
            if (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType)
            {
                assertionInfo = optCreateAssertion(tree->AsIntrinsic()->gtGetOp1(), nullptr, OAK_NOT_EQUAL);
            }
            break;

        case GT_BOUNDS_CHECK:
            if (!optLocalAssertionProp)
            {
                assertionInfo = optCreateAssertion(tree, nullptr, OAK_NO_THROW);
            }
            break;

        case GT_ARR_ELEM:
            // An array element reference can create a non-null assertion
            assertionInfo = optCreateAssertion(tree->AsArrElem()->gtArrObj, nullptr, OAK_NOT_EQUAL);
            break;

        case GT_CALL:
        {
            // A virtual call can create a non-null assertion. We transform some virtual calls into non-virtual calls
            // with a GTF_CALL_NULLCHECK flag set.
            // Ignore tail calls because they have 'this` pointer in the regular arg list and an implicit null check.
            GenTreeCall* const call = tree->AsCall();
            if (call->NeedsNullCheck() || (call->IsVirtual() && !call->IsTailCall()))
            {
                //  Retrieve the 'this' arg.
                GenTree* thisArg = call->gtArgs.GetThisArg()->GetNode();
                assert(thisArg != nullptr);
                assertionInfo = optCreateAssertion(thisArg, nullptr, OAK_NOT_EQUAL);
            }
        }
        break;

        case GT_CAST:
            // This represets an assertion that we would like to prove to be true.
            // If we can prove this assertion true then we can eliminate this cast.
            // We only create this assertion for global assertion propagation.
            assertionInfo   = optAssertionGenCast(tree->AsCast());
            assertionProven = false;
            break;

        case GT_JTRUE:
            assertionInfo = optAssertionGenJtrue(tree);
            break;

        default:
            // All other gtOper node kinds, leave 'assertionIndex' = NO_ASSERTION_INDEX
            break;
    }

    // For global assertion prop we must store the assertion number in the tree node
    if (assertionInfo.HasAssertion() && assertionProven && !optLocalAssertionProp)
    {
        tree->SetAssertionInfo(assertionInfo);
    }
}

/*****************************************************************************
 *
 * Maps a complementary assertion to its original assertion so it can be
 * retrieved faster.
 */
void Compiler::optMapComplementary(AssertionIndex assertionIndex, AssertionIndex index)
{
    if (assertionIndex == NO_ASSERTION_INDEX || index == NO_ASSERTION_INDEX)
    {
        return;
    }

    assert(assertionIndex <= optMaxAssertionCount);
    assert(index <= optMaxAssertionCount);

    optComplementaryAssertionMap[assertionIndex] = index;
    optComplementaryAssertionMap[index]          = assertionIndex;
}

/*****************************************************************************
 *
 *  Given an assertion index, return the assertion index of the complementary
 *  assertion or 0 if one does not exist.
 */
AssertionIndex Compiler::optFindComplementary(AssertionIndex assertIndex)
{
    if (assertIndex == NO_ASSERTION_INDEX)
    {
        return NO_ASSERTION_INDEX;
    }
    AssertionDsc* inputAssertion = optGetAssertion(assertIndex);

    // Must be an equal or not equal assertion.
    if (inputAssertion->assertionKind != OAK_EQUAL && inputAssertion->assertionKind != OAK_NOT_EQUAL)
    {
        return NO_ASSERTION_INDEX;
    }

    AssertionIndex index = optComplementaryAssertionMap[assertIndex];
    if (index != NO_ASSERTION_INDEX && index <= optAssertionCount)
    {
        return index;
    }

    for (AssertionIndex index = 1; index <= optAssertionCount; ++index)
    {
        // Make sure assertion kinds are complementary and op1, op2 kinds match.
        AssertionDsc* curAssertion = optGetAssertion(index);
        if (curAssertion->Complementary(inputAssertion, !optLocalAssertionProp))
        {
            optMapComplementary(assertIndex, index);
            return index;
        }
    }
    return NO_ASSERTION_INDEX;
}

//------------------------------------------------------------------------
// optAssertionIsSubrange: Find a subrange assertion for the given range and tree.
//
// This function will return the index of the first assertion in "assertions"
// which claims that the value of "tree" is withing the bounds of the provided
// "range" (i. e. "range.Contains(assertedRange)").
//
// Arguments:
//    tree       - the tree for which to find the assertion
//    range      - range the subrange of which to look for
//    assertions - the set of assertions
//
// Return Value:
//    Index of the found assertion, NO_ASSERTION_INDEX otherwise.
//
AssertionIndex Compiler::optAssertionIsSubrange(GenTree* tree, IntegralRange range, ASSERT_VALARG_TP assertions)
{
    if (!optLocalAssertionProp && BitVecOps::IsEmpty(apTraits, assertions))
    {
        return NO_ASSERTION_INDEX;
    }

    for (AssertionIndex index = 1; index <= optAssertionCount; index++)
    {
        AssertionDsc* curAssertion = optGetAssertion(index);
        if ((optLocalAssertionProp ||
             BitVecOps::IsMember(apTraits, assertions, index - 1)) && // either local prop or use propagated assertions
            (curAssertion->assertionKind == OAK_SUBRANGE) &&
            (curAssertion->op1.kind == O1K_LCLVAR))
        {
            // For local assertion prop use comparison on locals, and use comparison on vns for global prop.
            bool isEqual = optLocalAssertionProp
                               ? (curAssertion->op1.lcl.lclNum == tree->AsLclVarCommon()->GetLclNum())
                               : (curAssertion->op1.vn == vnStore->VNConservativeNormalValue(tree->gtVNPair));
            if (!isEqual)
            {
                continue;
            }

            if (range.Contains(curAssertion->op2.u2))
            {
                return index;
            }
        }
    }

    return NO_ASSERTION_INDEX;
}

/**********************************************************************************
 *
 * Given a "tree" that is usually arg1 of a isinst/cast kind of GT_CALL (a class
 * handle), and "methodTableArg" which is a const int (a class handle), then search
 * if there is an assertion in "assertions", that asserts the equality of the two
 * class handles and then returns the index of the assertion. If one such assertion
 * could not be found, then it returns NO_ASSERTION_INDEX.
 *
 */
AssertionIndex Compiler::optAssertionIsSubtype(GenTree* tree, GenTree* methodTableArg, ASSERT_VALARG_TP assertions)
{
    if (!optLocalAssertionProp && BitVecOps::IsEmpty(apTraits, assertions))
    {
        return NO_ASSERTION_INDEX;
    }
    for (AssertionIndex index = 1; index <= optAssertionCount; index++)
    {
        if (!optLocalAssertionProp && !BitVecOps::IsMember(apTraits, assertions, index - 1))
        {
            continue;
        }

        AssertionDsc* curAssertion = optGetAssertion(index);
        if (curAssertion->assertionKind != OAK_EQUAL ||
            (curAssertion->op1.kind != O1K_SUBTYPE && curAssertion->op1.kind != O1K_EXACT_TYPE))
        {
            continue;
        }

        // If local assertion prop use "lcl" based comparison, if global assertion prop use vn based comparison.
        if ((optLocalAssertionProp) ? (curAssertion->op1.lcl.lclNum != tree->AsLclVarCommon()->GetLclNum())
                                    : (curAssertion->op1.vn != vnStore->VNConservativeNormalValue(tree->gtVNPair)))
        {
            continue;
        }

        if (curAssertion->op2.kind == O2K_IND_CNS_INT)
        {
            if (methodTableArg->gtOper != GT_IND)
            {
                continue;
            }
            methodTableArg = methodTableArg->AsOp()->gtOp1;
        }
        else if (curAssertion->op2.kind != O2K_CONST_INT)
        {
            continue;
        }

        ssize_t      methodTableVal = 0;
        GenTreeFlags iconFlags      = GTF_EMPTY;
        if (!optIsTreeKnownIntValue(!optLocalAssertionProp, methodTableArg, &methodTableVal, &iconFlags))
        {
            continue;
        }

        if (curAssertion->op2.u1.iconVal == methodTableVal)
        {
            return index;
        }
    }
    return NO_ASSERTION_INDEX;
}

//------------------------------------------------------------------------------
// optVNConstantPropOnTree: Substitutes tree with an evaluated constant while
//                          managing side-effects.
//
// Arguments:
//    block -  The block containing the tree.
//    stmt  -  The statement in the block containing the tree.
//    tree  -  The tree node whose value is known at compile time.
//             The tree should have a constant value number.
//
// Return Value:
//    Returns a potentially new or a transformed tree node.
//    Returns nullptr when no transformation is possible.
//
// Description:
//    Transforms a tree node if its result evaluates to a constant. The
//    transformation can be a "ChangeOper" to a constant or a new constant node
//    with extracted side-effects.
//
//    Before replacing or substituting the "tree" with a constant, extracts any
//    side effects from the "tree" and creates a comma separated side effect list
//    and then appends the transformed node at the end of the list.
//    This comma separated list is then returned.
//
//    For JTrue nodes, side effects are not put into a comma separated list. If
//    the relop will evaluate to "true" or "false" statically, then the side-effects
//    will be put into new statements, presuming the JTrue will be folded away.
//
GenTree* Compiler::optVNConstantPropOnTree(BasicBlock* block, GenTree* tree)
{
    if (tree->OperGet() == GT_JTRUE)
    {
        // Treat JTRUE separately to extract side effects into respective statements rather
        // than using a COMMA separated op1.
        return optVNConstantPropOnJTrue(block, tree);
    }
    // If relop is part of JTRUE, this should be optimized as part of the parent JTRUE.
    // Or if relop is part of QMARK or anything else, we simply bail here.
    else if (tree->OperIsCompare() && (tree->gtFlags & GTF_RELOP_JMP_USED))
    {
        return nullptr;
    }

    // We want to use the Normal ValueNumber when checking for constants.
    ValueNumPair vnPair = tree->gtVNPair;
    ValueNum     vnCns  = vnStore->VNConservativeNormalValue(vnPair);

    // Check if node evaluates to a constant
    if (!vnStore->IsVNConstant(vnCns))
    {
        return nullptr;
    }

    GenTree* conValTree = nullptr;
    switch (vnStore->TypeOfVN(vnCns))
    {
        case TYP_FLOAT:
        {
            float value = vnStore->ConstantValue<float>(vnCns);

            if (tree->TypeGet() == TYP_INT)
            {
                // Same sized reinterpretation of bits to integer
                conValTree = gtNewIconNode(*(reinterpret_cast<int*>(&value)));
            }
            else
            {
                // Implicit assignment conversion to float or double
                assert(varTypeIsFloating(tree->TypeGet()));
                conValTree = gtNewDconNode(value, tree->TypeGet());
            }
            break;
        }

        case TYP_DOUBLE:
        {
            double value = vnStore->ConstantValue<double>(vnCns);

            if (tree->TypeGet() == TYP_LONG)
            {
                conValTree = gtNewLconNode(*(reinterpret_cast<INT64*>(&value)));
            }
            else
            {
                // Implicit assignment conversion to float or double
                assert(varTypeIsFloating(tree->TypeGet()));
                conValTree = gtNewDconNode(value, tree->TypeGet());
            }
            break;
        }

        case TYP_LONG:
        {
            INT64 value = vnStore->ConstantValue<INT64>(vnCns);

#ifdef TARGET_64BIT
            if (vnStore->IsVNHandle(vnCns))
            {
                // Don't perform constant folding that involves a handle that needs
                // to be recorded as a relocation with the VM.
                if (!opts.compReloc)
                {
                    conValTree = gtNewIconHandleNode(value, vnStore->GetHandleFlags(vnCns));
                }
            }
            else
#endif
            {
                switch (tree->TypeGet())
                {
                    case TYP_INT:
                        // Implicit assignment conversion to smaller integer
                        conValTree = gtNewIconNode(static_cast<int>(value));
                        break;

                    case TYP_LONG:
                        // Same type no conversion required
                        conValTree = gtNewLconNode(value);
                        break;

                    case TYP_FLOAT:
                        // No implicit conversions from long to float and value numbering will
                        // not propagate through memory reinterpretations of different size.
                        unreached();
                        break;

                    case TYP_DOUBLE:
                        // Same sized reinterpretation of bits to double
                        conValTree = gtNewDconNode(*(reinterpret_cast<double*>(&value)));
                        break;

                    default:
                        // Do not support such optimization.
                        break;
                }
            }
        }
        break;

        case TYP_REF:
        {
            assert(vnStore->ConstantValue<size_t>(vnCns) == 0);
            // Support onle ref(ref(0)), do not support other forms (e.g byref(ref(0)).
            if (tree->TypeGet() == TYP_REF)
            {
                conValTree = gtNewIconNode(0, TYP_REF);
            }
        }
        break;

        case TYP_INT:
        {
            int value = vnStore->ConstantValue<int>(vnCns);
#ifndef TARGET_64BIT
            if (vnStore->IsVNHandle(vnCns))
            {
                // Don't perform constant folding that involves a handle that needs
                // to be recorded as a relocation with the VM.
                if (!opts.compReloc)
                {
                    conValTree = gtNewIconHandleNode(value, vnStore->GetHandleFlags(vnCns));
                }
            }
            else
#endif
            {
                switch (tree->TypeGet())
                {
                    case TYP_REF:
                    case TYP_INT:
                        // Same type no conversion required
                        conValTree = gtNewIconNode(value);
                        break;

                    case TYP_LONG:
                        // Implicit assignment conversion to larger integer
                        conValTree = gtNewLconNode(value);
                        break;

                    case TYP_FLOAT:
                        // Same sized reinterpretation of bits to float
                        conValTree = gtNewDconNode(*reinterpret_cast<float*>(&value), TYP_FLOAT);
                        break;

                    case TYP_DOUBLE:
                        // No implicit conversions from int to double and value numbering will
                        // not propagate through memory reinterpretations of different size.
                        unreached();
                        break;

                    case TYP_BOOL:
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    case TYP_SHORT:
                    case TYP_USHORT:
                        assert(FitsIn(tree->TypeGet(), value));
                        conValTree = gtNewIconNode(value);
                        break;

                    default:
                        // Do not support (e.g. byref(const int)).
                        break;
                }
            }
        }
        break;

#if FEATURE_SIMD
        case TYP_SIMD8:
        {
            simd8_t value = vnStore->ConstantValue<simd8_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet(), CORINFO_TYPE_FLOAT);
            vecCon->gtSimd8Val    = value;

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD12:
        {
            simd12_t value = vnStore->ConstantValue<simd12_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet(), CORINFO_TYPE_FLOAT);
            vecCon->gtSimd12Val   = value;

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD16:
        {
            simd16_t value = vnStore->ConstantValue<simd16_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet(), CORINFO_TYPE_FLOAT);
            vecCon->gtSimd16Val   = value;

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD32:
        {
            simd32_t value = vnStore->ConstantValue<simd32_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet(), CORINFO_TYPE_FLOAT);
            vecCon->gtSimd32Val   = value;

            conValTree = vecCon;
            break;
        }
        break;
#endif // FEATURE_SIMD

        case TYP_BYREF:
            // Do not support const byref optimization.
            break;

        default:
            // We do not record constants of other types.
            unreached();
            break;
    }

    if (conValTree != nullptr)
    {
        if (tree->OperIs(GT_LCL_VAR))
        {
            if (!optIsProfitableToSubstitute(tree->AsLclVar(), block, conValTree))
            {
                // Not profitable to substitute
                return nullptr;
            }
        }

        // Were able to optimize.
        conValTree->gtVNPair = vnPair;
        GenTree* sideEffList = optExtractSideEffListFromConst(tree);
        if (sideEffList != nullptr)
        {
            // Replace as COMMA(side_effects, const value tree);
            assert((sideEffList->gtFlags & GTF_SIDE_EFFECT) != 0);
            return gtNewOperNode(GT_COMMA, conValTree->TypeGet(), sideEffList, conValTree);
        }
        else
        {
            // No side effects, replace as const value tree.
            return conValTree;
        }
    }
    else
    {
        // Was not able to optimize.
        return nullptr;
    }
}

//------------------------------------------------------------------------------
// optIsProfitableToSubstitute: Checks if value worth substituting to lcl location
//
// Arguments:
//    lcl       - lcl to replace with value if profitable
//    lclBlock  - Basic block lcl located in
//    value     - value we plan to substitute to lcl
//
// Returns:
//    False if it's likely not profitable to do substitution, True otherwise
//
bool Compiler::optIsProfitableToSubstitute(GenTreeLclVarCommon* lcl, BasicBlock* lclBlock, GenTree* value)
{
    // A simple heuristic: If the constant is defined outside of a loop (not far from its head)
    // and is used inside it - don't propagate.

    // TODO: Extend on more kinds of trees
    if (!value->OperIs(GT_CNS_VEC, GT_CNS_DBL))
    {
        return true;
    }

    gtPrepareCost(value);

    if ((value->GetCostEx() > 1) && (value->GetCostSz() > 1))
    {
        // Try to find the block this constant was originally defined in
        if (lcl->HasSsaName())
        {
            BasicBlock* defBlock = lvaGetDesc(lcl)->GetPerSsaData(lcl->GetSsaNum())->GetBlock();
            if (defBlock != nullptr)
            {
                // Avoid propagating if the weighted use cost is significantly greater than the def cost.
                // NOTE: this currently does not take "a float living across a call" case into account
                // where we might end up with spill/restore on ABIs without callee-saved registers
                const weight_t defBlockWeight = defBlock->getBBWeight(this);
                const weight_t lclblockWeight = lclBlock->getBBWeight(this);

                if ((defBlockWeight > 0) && ((lclblockWeight / defBlockWeight) >= BB_LOOP_WEIGHT_SCALE))
                {
                    JITDUMP("Constant propagation inside loop " FMT_BB " is not profitable\n", lclBlock->bbNum);
                    return false;
                }
            }
        }
    }
    return true;
}

//------------------------------------------------------------------------------
// optConstantAssertionProp: Possibly substitute a constant for a local use
//
// Arguments:
//    curAssertion - assertion to propagate
//    tree         - tree to possibly modify
//    stmt         - statement containing the tree
//    index        - index of this assertion in the assertion table
//
// Returns:
//    Updated tree (may be the input tree, modified in place), or nullptr
//
// Notes:
//    stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optConstantAssertionProp(AssertionDsc*        curAssertion,
                                            GenTreeLclVarCommon* tree,
                                            Statement* stmt DEBUGARG(AssertionIndex index))
{
    const unsigned lclNum = tree->GetLclNum();

    if (lclNumIsCSE(lclNum))
    {
        return nullptr;
    }

    GenTree* newTree = tree;

    // Update 'newTree' with the new value from our table
    // Typically newTree == tree and we are updating the node in place
    switch (curAssertion->op2.kind)
    {
        case O2K_CONST_DOUBLE:
            // There could be a positive zero and a negative zero, so don't propagate zeroes.
            if (curAssertion->op2.dconVal == 0.0)
            {
                return nullptr;
            }
            newTree->BashToConst(curAssertion->op2.dconVal, tree->TypeGet());
            break;

        case O2K_CONST_LONG:
            if (newTree->TypeIs(TYP_LONG))
            {
                newTree->BashToConst(curAssertion->op2.lconVal);
            }
            else
            {
                newTree->BashToConst(static_cast<int32_t>(curAssertion->op2.lconVal));
            }
            break;

        case O2K_CONST_INT:

            // Don't propagate handles if we need to report relocs.
            if (opts.compReloc && ((curAssertion->op2.u1.iconFlags & GTF_ICON_HDL_MASK) != 0))
            {
                return nullptr;
            }

            if (curAssertion->op2.u1.iconFlags & GTF_ICON_HDL_MASK)
            {
                // Here we have to allocate a new 'large' node to replace the old one
                newTree = gtNewIconHandleNode(curAssertion->op2.u1.iconVal,
                                              curAssertion->op2.u1.iconFlags & GTF_ICON_HDL_MASK);
            }
            else
            {
                assert(varTypeIsIntegralOrI(tree));
                newTree->BashToConst(curAssertion->op2.u1.iconVal, genActualType(tree));
            }
            break;

        default:
            return nullptr;
    }

    if (!optLocalAssertionProp)
    {
        assert(newTree->OperIsConst());                      // We should have a simple Constant node for newTree
        assert(vnStore->IsVNConstant(curAssertion->op2.vn)); // The value number stored for op2 should be a valid
                                                             // VN representing the constant
        newTree->gtVNPair.SetBoth(curAssertion->op2.vn);     // Set the ValueNumPair to the constant VN from op2
                                                             // of the assertion
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAssertion prop in " FMT_BB ":\n", compCurBB->bbNum);
        optPrintAssertion(curAssertion, index);
        gtDispTree(newTree, nullptr, nullptr, true);
    }
#endif

    return optAssertionProp_Update(newTree, tree, stmt);
}

//------------------------------------------------------------------------------
// optZeroObjAssertionProp: Find and propagate a ZEROOBJ assertion for the given tree.
//
// Arguments:
//    assertions - set of live assertions
//    tree       - the tree to possibly replace, in-place, with a zero
//
// Returns:
//    Whether propagation took place.
//
// Notes:
//    Because not all users of struct nodes support "zero" operands, instead of
//    propagating ZEROOBJ on locals, we propagate it on their parents.
//
bool Compiler::optZeroObjAssertionProp(GenTree* tree, ASSERT_VALARG_TP assertions)
{
    assert(varTypeIsStruct(tree));

    // We only make ZEROOBJ assertions in local propagation.
    if (!optLocalAssertionProp)
    {
        return false;
    }

    if (!tree->OperIs(GT_LCL_VAR) || lvaGetDesc(tree->AsLclVar())->IsAddressExposed())
    {
        return false;
    }

    unsigned       lclNum         = tree->AsLclVar()->GetLclNum();
    AssertionIndex assertionIndex = optLocalAssertionIsEqualOrNotEqual(O1K_LCLVAR, lclNum, O2K_ZEROOBJ, 0, assertions);
    if (assertionIndex == NO_ASSERTION_INDEX)
    {
        return false;
    }

    AssertionDsc* assertion = optGetAssertion(assertionIndex);
    JITDUMP("\nAssertion prop in " FMT_BB ":\n", compCurBB->bbNum);
    JITDUMPEXEC(optPrintAssertion(assertion, assertionIndex));
    DISPNODE(tree);

    tree->BashToZeroConst(TYP_INT);

    JITDUMP(" =>\n");
    DISPNODE(tree);

    return true;
}

//------------------------------------------------------------------------------
// optAssertionProp_LclVarTypeCheck: verify compatible types for copy prop
//
// Arguments:
//    tree         - tree to possibly modify
//    lclVarDsc    - local accessed by tree
//    copyVarDsc   - local to possibly copy prop into tree
//
// Returns:
//    True if copy prop is safe.
//
// Notes:
//    Before substituting copyVar for lclVar, make sure using copyVar doesn't widen access.
//
bool Compiler::optAssertionProp_LclVarTypeCheck(GenTree* tree, LclVarDsc* lclVarDsc, LclVarDsc* copyVarDsc)
{
    /*
        Small struct field locals are stored using the exact width and loaded widened
        (i.e. lvNormalizeOnStore==false   lvNormalizeOnLoad==true),
        because the field locals might end up embedded in the parent struct local with the exact width.

            In other words, a store to a short field local should always done using an exact width store

                [00254538] 0x0009 ------------               const     int    0x1234
            [002545B8] 0x000B -A--G--NR---               =         short
                [00254570] 0x000A D------N----               lclVar    short  V43 tmp40

            mov   word  ptr [L_043], 0x1234

        Now, if we copy prop, say a short field local V43, to another short local V34
        for the following tree:

                [04E18650] 0x0001 ------------               lclVar    int   V34 tmp31
            [04E19714] 0x0002 -A----------               =         int
                [04E196DC] 0x0001 D------N----               lclVar    int   V36 tmp33

        We will end with this tree:

                [04E18650] 0x0001 ------------               lclVar    int   V43 tmp40
            [04E19714] 0x0002 -A-----NR---               =         int
                [04E196DC] 0x0001 D------N----               lclVar    int   V36 tmp33    EAX

        And eventually causing a fetch of 4-byte out from [L_043] :(
            mov     EAX, dword ptr [L_043]

        The following check is to make sure we only perform the copy prop
        when we don't retrieve the wider value.
    */

    if (copyVarDsc->lvIsStructField)
    {
        var_types varType = (var_types)copyVarDsc->lvType;
        // Make sure we don't retrieve the wider value.
        return !varTypeIsSmall(varType) || (varType == tree->TypeGet());
    }
    // Called in the context of a single copy assertion, so the types should have been
    // taken care by the assertion gen logic for other cases. Just return true.
    return true;
}

//------------------------------------------------------------------------
// optCopyAssertionProp: copy prop use of one local with another
//
// Arguments:
//    curAssertion - assertion triggering the possible copy
//    tree         - tree use to consider replacing
//    stmt         - statment containing the tree
//    index        - index of the assertion
//
// Returns:
//    Updated tree, or nullptr
//
// Notes:
//    stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optCopyAssertionProp(AssertionDsc*        curAssertion,
                                        GenTreeLclVarCommon* tree,
                                        Statement* stmt DEBUGARG(AssertionIndex index))
{
    const AssertionDsc::AssertionDscOp1& op1 = curAssertion->op1;
    const AssertionDsc::AssertionDscOp2& op2 = curAssertion->op2;

    noway_assert(op1.lcl.lclNum != op2.lcl.lclNum);

    const unsigned lclNum = tree->GetLclNum();

    // Make sure one of the lclNum of the assertion matches with that of the tree.
    if (op1.lcl.lclNum != lclNum && op2.lcl.lclNum != lclNum)
    {
        return nullptr;
    }

    // Extract the matching lclNum and ssaNum, as well as the field sequence.
    unsigned      copyLclNum;
    unsigned      copySsaNum;
    FieldSeqNode* zeroOffsetFieldSeq;
    if (op1.lcl.lclNum == lclNum)
    {
        copyLclNum         = op2.lcl.lclNum;
        copySsaNum         = op2.lcl.ssaNum;
        zeroOffsetFieldSeq = op2.zeroOffsetFieldSeq;
    }
    else
    {
        copyLclNum         = op1.lcl.lclNum;
        copySsaNum         = op1.lcl.ssaNum;
        zeroOffsetFieldSeq = nullptr;  // Only the RHS of an assignment can have a FldSeq.
        assert(optLocalAssertionProp); // Were we to perform replacements in global propagation, that makes copy
                                       // assertions for control flow ("if (a == b) { ... }"), where both operands
                                       // could have a FldSeq, we'd need to save it for "op1" too.
    }

    if (!optLocalAssertionProp)
    {
        // Extract the ssaNum of the matching lclNum.
        unsigned ssaNum = (op1.lcl.lclNum == lclNum) ? op1.lcl.ssaNum : op2.lcl.ssaNum;

        if (ssaNum != tree->GetSsaNum())
        {
            return nullptr;
        }
    }

    LclVarDsc* const copyVarDsc = lvaGetDesc(copyLclNum);
    LclVarDsc* const lclVarDsc  = lvaGetDesc(lclNum);

    // Make sure the types are compatible.
    if (!optAssertionProp_LclVarTypeCheck(tree, lclVarDsc, copyVarDsc))
    {
        return nullptr;
    }

    // Make sure we can perform this copy prop.
    if (optCopyProp_LclVarScore(lclVarDsc, copyVarDsc, curAssertion->op1.lcl.lclNum == lclNum) <= 0)
    {
        return nullptr;
    }

    tree->SetLclNum(copyLclNum);
    tree->SetSsaNum(copySsaNum);

    // The sequence we are propagating (if any) represents the inner fields.
    if (zeroOffsetFieldSeq != nullptr)
    {
        FieldSeqNode* outerZeroOffsetFieldSeq = nullptr;
        if (GetZeroOffsetFieldMap()->Lookup(tree, &outerZeroOffsetFieldSeq))
        {
            zeroOffsetFieldSeq = GetFieldSeqStore()->Append(zeroOffsetFieldSeq, outerZeroOffsetFieldSeq);
            GetZeroOffsetFieldMap()->Remove(tree);
        }

        fgAddFieldSeqForZeroOffset(tree, zeroOffsetFieldSeq);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAssertion prop in " FMT_BB ":\n", compCurBB->bbNum);
        optPrintAssertion(curAssertion, index);
        DISPNODE(tree);
    }
#endif

    // Update and morph the tree.
    return optAssertionProp_Update(tree, tree, stmt);
}

//------------------------------------------------------------------------
// optAssertionProp_LclVar: try and optimize a local var use via assertions
//
// Arguments:
//    assertions - set of live assertions
//    tree       - local use to optimize
//    stmt       - statement containing the tree
//
// Returns:
//    Updated tree, or nullptr
//
// Notes:
//   stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optAssertionProp_LclVar(ASSERT_VALARG_TP assertions, GenTreeLclVarCommon* tree, Statement* stmt)
{
    // If we have a var definition then bail or
    // If this is the address of the var then it will have the GTF_DONT_CSE
    // flag set and we don't want to to assertion prop on it.
    if (tree->gtFlags & (GTF_VAR_DEF | GTF_DONT_CSE))
    {
        return nullptr;
    }

    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        AssertionIndex assertionIndex = GetAssertionIndex(index);
        if (assertionIndex > optAssertionCount)
        {
            break;
        }
        // See if the variable is equal to a constant or another variable.
        AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
        if (curAssertion->assertionKind != OAK_EQUAL || curAssertion->op1.kind != O1K_LCLVAR)
        {
            continue;
        }

        // Copy prop.
        if (curAssertion->op2.kind == O2K_LCLVAR_COPY)
        {
            // Cannot do copy prop during global assertion prop because of no knowledge
            // of kill sets. We will still make a == b copy assertions during the global phase to allow
            // for any implied assertions that can be retrieved. Because implied assertions look for
            // matching SSA numbers (i.e., if a0 == b1 and b1 == c0 then a0 == c0) they don't need kill sets.
            if (optLocalAssertionProp)
            {
                // Perform copy assertion prop.
                GenTree* newTree = optCopyAssertionProp(curAssertion, tree, stmt DEBUGARG(assertionIndex));
                if (newTree != nullptr)
                {
                    return newTree;
                }
            }

            continue;
        }

        // There are no constant assertions for structs.
        //
        if (varTypeIsStruct(tree))
        {
            continue;
        }

        // Constant prop.
        //
        // The case where the tree type could be different than the LclVar type is caused by
        // gtFoldExpr, specifically the case of a cast, where the fold operation changes the type of the LclVar
        // node.  In such a case is not safe to perform the substitution since later on the JIT will assert mismatching
        // types between trees.
        const unsigned lclNum = tree->GetLclNum();
        if (curAssertion->op1.lcl.lclNum == lclNum)
        {
            LclVarDsc* const lclDsc = lvaGetDesc(lclNum);
            // Verify types match
            if (tree->TypeGet() == lclDsc->lvType)
            {
                // If local assertion prop, just perform constant prop.
                if (optLocalAssertionProp)
                {
                    return optConstantAssertionProp(curAssertion, tree, stmt DEBUGARG(assertionIndex));
                }

                // If global assertion, perform constant propagation only if the VN's match.
                if (curAssertion->op1.vn == vnStore->VNConservativeNormalValue(tree->gtVNPair))
                {
                    return optConstantAssertionProp(curAssertion, tree, stmt DEBUGARG(assertionIndex));
                }
            }
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// optAssertionProp_Asg: Try and optimize an assignment via assertions.
//
// Propagates ZEROOBJ for the RHS.
//
// Arguments:
//    assertions - set of live assertions
//    asg        - the store to optimize
//    stmt       - statement containing "asg"
//
// Returns:
//    Updated "asg", or "nullptr"
//
// Notes:
//   stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optAssertionProp_Asg(ASSERT_VALARG_TP assertions, GenTreeOp* asg, Statement* stmt)
{
    GenTree* rhs = asg->gtGetOp2();
    if (asg->OperIsCopyBlkOp() && varTypeIsStruct(rhs))
    {
        if (optZeroObjAssertionProp(rhs, assertions))
        {
            return optAssertionProp_Update(asg, asg, stmt);
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// optAssertionProp_Return: Try and optimize a GT_RETURN via assertions.
//
// Propagates ZEROOBJ for the return value.
//
// Arguments:
//    assertions - set of live assertions
//    ret        - the return node to optimize
//    stmt       - statement containing "ret"
//
// Returns:
//    Updated "ret", or "nullptr"
//
// Notes:
//   stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optAssertionProp_Return(ASSERT_VALARG_TP assertions, GenTreeUnOp* ret, Statement* stmt)
{
    GenTree* retValue = ret->gtGetOp1();

    // Only propagate zeroes that lowering can deal with.
    if (!ret->TypeIs(TYP_VOID) && varTypeIsStruct(retValue) && !varTypeIsStruct(info.compRetNativeType))
    {
        if (optZeroObjAssertionProp(retValue, assertions))
        {
            return optAssertionProp_Update(ret, ret, stmt);
        }
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  Given a set of "assertions" to search, find an assertion that matches
 *  op1Kind and lclNum, op2Kind and the constant value and is either equal or
 *  not equal assertion.
 */
AssertionIndex Compiler::optLocalAssertionIsEqualOrNotEqual(
    optOp1Kind op1Kind, unsigned lclNum, optOp2Kind op2Kind, ssize_t cnsVal, ASSERT_VALARG_TP assertions)
{
    noway_assert((op1Kind == O1K_LCLVAR) || (op1Kind == O1K_EXACT_TYPE) || (op1Kind == O1K_SUBTYPE));
    noway_assert((op2Kind == O2K_CONST_INT) || (op2Kind == O2K_IND_CNS_INT) || (op2Kind == O2K_ZEROOBJ));
    if (!optLocalAssertionProp && BitVecOps::IsEmpty(apTraits, assertions))
    {
        return NO_ASSERTION_INDEX;
    }

    for (AssertionIndex index = 1; index <= optAssertionCount; ++index)
    {
        AssertionDsc* curAssertion = optGetAssertion(index);
        if (optLocalAssertionProp || BitVecOps::IsMember(apTraits, assertions, index - 1))
        {
            if ((curAssertion->assertionKind != OAK_EQUAL) && (curAssertion->assertionKind != OAK_NOT_EQUAL))
            {
                continue;
            }

            if ((curAssertion->op1.kind == op1Kind) && (curAssertion->op1.lcl.lclNum == lclNum) &&
                (curAssertion->op2.kind == op2Kind))
            {
                bool constantIsEqual  = (curAssertion->op2.u1.iconVal == cnsVal);
                bool assertionIsEqual = (curAssertion->assertionKind == OAK_EQUAL);

                if (constantIsEqual || assertionIsEqual)
                {
                    return index;
                }
            }
        }
    }
    return NO_ASSERTION_INDEX;
}

//------------------------------------------------------------------------
// optGlobalAssertionIsEqualOrNotEqual: Look for an assertion in the specified
//        set that is one of op1 == op1, op1 != op2, or *op1 == op2,
//        where equality is based on value numbers.
//
// Arguments:
//      assertions: bit vector describing set of assertions
//      op1, op2:    the treen nodes in question
//
// Returns:
//      Index of first matching assertion, or NO_ASSERTION_INDEX if no
//      assertions in the set are matches.
//
// Notes:
//      Assertions based on *op1 are the result of exact type tests and are
//      only returned when op1 is a local var with ref type and the assertion
//      is an exact type equality.
//
AssertionIndex Compiler::optGlobalAssertionIsEqualOrNotEqual(ASSERT_VALARG_TP assertions, GenTree* op1, GenTree* op2)
{
    if (BitVecOps::IsEmpty(apTraits, assertions))
    {
        return NO_ASSERTION_INDEX;
    }
    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        AssertionIndex assertionIndex = GetAssertionIndex(index);
        if (assertionIndex > optAssertionCount)
        {
            break;
        }
        AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
        if ((curAssertion->assertionKind != OAK_EQUAL && curAssertion->assertionKind != OAK_NOT_EQUAL))
        {
            continue;
        }

        if ((curAssertion->op1.vn == vnStore->VNConservativeNormalValue(op1->gtVNPair)) &&
            (curAssertion->op2.vn == vnStore->VNConservativeNormalValue(op2->gtVNPair)))
        {
            return assertionIndex;
        }

        // Look for matching exact type assertions based on vtable accesses
        if ((curAssertion->assertionKind == OAK_EQUAL) && (curAssertion->op1.kind == O1K_EXACT_TYPE) &&
            op1->OperIs(GT_IND))
        {
            GenTree* indirAddr = op1->AsIndir()->Addr();

            if (indirAddr->OperIs(GT_LCL_VAR) && (indirAddr->TypeGet() == TYP_REF))
            {
                // op1 is accessing vtable of a ref type local var
                if ((curAssertion->op1.vn == vnStore->VNConservativeNormalValue(indirAddr->gtVNPair)) &&
                    (curAssertion->op2.vn == vnStore->VNConservativeNormalValue(op2->gtVNPair)))
                {
                    return assertionIndex;
                }
            }
        }
    }
    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  Given a set of "assertions" to search for, find an assertion that is either
 *  op == 0 or op != 0
 *
 */
AssertionIndex Compiler::optGlobalAssertionIsEqualOrNotEqualZero(ASSERT_VALARG_TP assertions, GenTree* op1)
{
    if (BitVecOps::IsEmpty(apTraits, assertions))
    {
        return NO_ASSERTION_INDEX;
    }
    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        AssertionIndex assertionIndex = GetAssertionIndex(index);
        if (assertionIndex > optAssertionCount)
        {
            break;
        }
        AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
        if ((curAssertion->assertionKind != OAK_EQUAL && curAssertion->assertionKind != OAK_NOT_EQUAL))
        {
            continue;
        }

        if ((curAssertion->op1.vn == vnStore->VNConservativeNormalValue(op1->gtVNPair)) &&
            (curAssertion->op2.vn == vnStore->VNZeroForType(op1->TypeGet())))
        {
            return assertionIndex;
        }
    }
    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  Given a tree consisting of a RelOp and a set of available assertions
 *  we try to propagate an assertion and modify the RelOp tree if we can.
 *  We pass in the root of the tree via 'stmt', for local copy prop 'stmt' will be nullptr
 *  Returns the modified tree, or nullptr if no assertion prop took place
 */

GenTree* Compiler::optAssertionProp_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    assert(tree->OperIsCompare());

    if (!optLocalAssertionProp)
    {
        // If global assertion prop then use value numbering.
        return optAssertionPropGlobal_RelOp(assertions, tree, stmt);
    }

    //
    // Currently only GT_EQ or GT_NE are supported Relops for local AssertionProp
    //

    if ((tree->gtOper != GT_EQ) && (tree->gtOper != GT_NE))
    {
        return nullptr;
    }

    // If local assertion prop then use variable based prop.
    return optAssertionPropLocal_RelOp(assertions, tree, stmt);
}

//------------------------------------------------------------------------
// optAssertionProp: try and optimize a relop via assertion propagation
//
// Arguments:
//   assertions  - set of live assertions
//   tree        - tree to possibly optimize
//   stmt        - statement containing the tree
//
// Returns:
//   The modified tree, or nullptr if no assertion prop took place.
//
GenTree* Compiler::optAssertionPropGlobal_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    GenTree* newTree = tree;
    GenTree* op1     = tree->AsOp()->gtOp1;
    GenTree* op2     = tree->AsOp()->gtOp2;

    // Look for assertions of the form (tree EQ/NE 0)
    AssertionIndex index = optGlobalAssertionIsEqualOrNotEqualZero(assertions, tree);

    if (index != NO_ASSERTION_INDEX)
    {
        // We know that this relop is either 0 or != 0 (1)
        AssertionDsc* curAssertion = optGetAssertion(index);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nVN relop based constant assertion prop in " FMT_BB ":\n", compCurBB->bbNum);
            printf("Assertion index=#%02u: ", index);
            printTreeID(tree);
            printf(" %s 0\n", (curAssertion->assertionKind == OAK_EQUAL) ? "==" : "!=");
        }
#endif

        // Bail out if tree is not side effect free.
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            JITDUMP("sorry, blocked by side effects\n");
            return nullptr;
        }

        if (curAssertion->assertionKind == OAK_EQUAL)
        {
            tree->BashToConst(0);
        }
        else
        {
            tree->BashToConst(1);
        }

        newTree = fgMorphTree(tree);
        DISPTREE(newTree);
        return optAssertionProp_Update(newTree, tree, stmt);
    }

    // Else check if we have an equality check involving a local or an indir
    if (!tree->OperIs(GT_EQ, GT_NE))
    {
        return nullptr;
    }

    // Bail out if tree is not side effect free.
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        return nullptr;
    }

    if (!op1->OperIs(GT_LCL_VAR, GT_IND))
    {
        return nullptr;
    }

    // Find an equal or not equal assertion involving "op1" and "op2".
    index = optGlobalAssertionIsEqualOrNotEqual(assertions, op1, op2);

    if (index == NO_ASSERTION_INDEX)
    {
        return nullptr;
    }

    AssertionDsc* curAssertion         = optGetAssertion(index);
    bool          assertionKindIsEqual = (curAssertion->assertionKind == OAK_EQUAL);

    // Allow or not to reverse condition for OAK_NOT_EQUAL assertions.
    bool allowReverse = true;

    // If the assertion involves "op2" and it is a constant, then check if "op1" also has a constant value.
    ValueNum vnCns = vnStore->VNConservativeNormalValue(op2->gtVNPair);
    if (vnStore->IsVNConstant(vnCns))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nVN relop based constant assertion prop in " FMT_BB ":\n", compCurBB->bbNum);
            printf("Assertion index=#%02u: ", index);
            printTreeID(op1);
            printf(" %s ", assertionKindIsEqual ? "==" : "!=");
            if (genActualType(op1->TypeGet()) == TYP_INT)
            {
                printf("%d\n", vnStore->ConstantValue<int>(vnCns));
            }
            else if (op1->TypeGet() == TYP_LONG)
            {
                printf("%I64d\n", vnStore->ConstantValue<INT64>(vnCns));
            }
            else if (op1->TypeGet() == TYP_DOUBLE)
            {
                printf("%f\n", vnStore->ConstantValue<double>(vnCns));
            }
            else if (op1->TypeGet() == TYP_FLOAT)
            {
                printf("%f\n", vnStore->ConstantValue<float>(vnCns));
            }
            else if (op1->TypeGet() == TYP_REF)
            {
                // The only constant of TYP_REF that ValueNumbering supports is 'null'
                assert(vnStore->ConstantValue<size_t>(vnCns) == 0);
                printf("null\n");
            }
            else if (op1->TypeGet() == TYP_BYREF)
            {
                printf("%d (byref)\n", static_cast<target_ssize_t>(vnStore->ConstantValue<size_t>(vnCns)));
            }
            else
            {
                printf("??unknown\n");
            }
            gtDispTree(tree, nullptr, nullptr, true);
        }
#endif
        // Change the oper to const.
        if (genActualType(op1->TypeGet()) == TYP_INT)
        {
            op1->BashToConst(vnStore->ConstantValue<int>(vnCns));

            if (vnStore->IsVNHandle(vnCns))
            {
                op1->gtFlags |= (vnStore->GetHandleFlags(vnCns) & GTF_ICON_HDL_MASK);
            }
        }
        else if (op1->TypeGet() == TYP_LONG)
        {
            op1->BashToConst(vnStore->ConstantValue<INT64>(vnCns));

            if (vnStore->IsVNHandle(vnCns))
            {
                op1->gtFlags |= (vnStore->GetHandleFlags(vnCns) & GTF_ICON_HDL_MASK);
            }
        }
        else if (op1->TypeGet() == TYP_DOUBLE)
        {
            double constant = vnStore->ConstantValue<double>(vnCns);
            op1->BashToConst(constant);

            // Nothing can be equal to NaN. So if IL had "op1 == NaN", then we already made op1 NaN,
            // which will yield a false correctly. Instead if IL had "op1 != NaN", then we already
            // made op1 NaN which will yield a true correctly. Note that this is irrespective of the
            // assertion we have made.
            allowReverse = (_isnan(constant) == 0);
        }
        else if (op1->TypeGet() == TYP_FLOAT)
        {
            float constant = vnStore->ConstantValue<float>(vnCns);
            op1->BashToConst(constant);

            // See comments for TYP_DOUBLE.
            allowReverse = (_isnan(constant) == 0);
        }
        else if (op1->TypeGet() == TYP_REF)
        {
            op1->BashToConst(0, TYP_REF);
            // The only constant of TYP_REF that ValueNumbering supports is 'null'
            noway_assert(vnStore->ConstantValue<size_t>(vnCns) == 0);
        }
        else if (op1->TypeGet() == TYP_BYREF)
        {
            op1->BashToConst(static_cast<target_ssize_t>(vnStore->ConstantValue<size_t>(vnCns)), TYP_BYREF);
        }
        else
        {
            noway_assert(!"unknown type in Global_RelOp");
        }

        op1->gtVNPair.SetBoth(vnCns); // Preserve the ValueNumPair, as BashToConst will clear it.

        // set foldResult to either 0 or 1
        bool foldResult = assertionKindIsEqual;
        if (tree->gtOper == GT_NE)
        {
            foldResult = !foldResult;
        }

        // Set the value number on the relop to 1 (true) or 0 (false)
        if (foldResult)
        {
            tree->gtVNPair.SetBoth(vnStore->VNOneForType(TYP_INT));
        }
        else
        {
            tree->gtVNPair.SetBoth(vnStore->VNZeroForType(TYP_INT));
        }
    }
    // If the assertion involves "op2" and "op1" is also a local var, then just morph the tree.
    else if (op2->gtOper == GT_LCL_VAR)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nVN relop based copy assertion prop in " FMT_BB ":\n", compCurBB->bbNum);
            printf("Assertion index=#%02u: V%02d.%02d %s V%02d.%02d\n", index, op1->AsLclVar()->GetLclNum(),
                   op1->AsLclVar()->GetSsaNum(), (curAssertion->assertionKind == OAK_EQUAL) ? "==" : "!=",
                   op2->AsLclVar()->GetLclNum(), op2->AsLclVar()->GetSsaNum());
            gtDispTree(tree, nullptr, nullptr, true);
        }
#endif
        // If floating point, don't just substitute op1 with op2, this won't work if
        // op2 is NaN. Just turn it into a "true" or "false" yielding expression.
        if (op1->TypeIs(TYP_FLOAT, TYP_DOUBLE))
        {
            // Note we can't trust the OAK_EQUAL as the value could end up being a NaN
            // violating the assertion. However, we create OAK_EQUAL assertions for floating
            // point only on JTrue nodes, so if the condition held earlier, it will hold
            // now. We don't create OAK_EQUAL assertion on floating point from GT_ASG
            // because we depend on value num which would constant prop the NaN.
            op1->BashToConst(0.0, op1->TypeGet());
            op2->BashToConst(0.0, op2->TypeGet());
        }
        // Change the op1 LclVar to the op2 LclVar
        else
        {
            noway_assert(varTypeIsIntegralOrI(op1->TypeGet()));
            op1->AsLclVarCommon()->SetLclNum(op2->AsLclVarCommon()->GetLclNum());
            op1->AsLclVarCommon()->SetSsaNum(op2->AsLclVarCommon()->GetSsaNum());
        }
    }
    else
    {
        return nullptr;
    }

    // Finally reverse the condition, if we have a not equal assertion.
    if (allowReverse && curAssertion->assertionKind == OAK_NOT_EQUAL)
    {
        gtReverseCond(tree);
    }

    newTree = fgMorphTree(tree);

#ifdef DEBUG
    if (verbose)
    {
        gtDispTree(newTree, nullptr, nullptr, true);
    }
#endif

    return optAssertionProp_Update(newTree, tree, stmt);
}

/*************************************************************************************
 *
 *  Given the set of "assertions" to look up a relop assertion about the relop "tree",
 *  perform local variable name based relop assertion propagation on the tree.
 *
 */
GenTree* Compiler::optAssertionPropLocal_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    assert(tree->OperGet() == GT_EQ || tree->OperGet() == GT_NE);

    GenTree* op1 = tree->AsOp()->gtOp1;
    GenTree* op2 = tree->AsOp()->gtOp2;

    // For Local AssertionProp we only can fold when op1 is a GT_LCL_VAR
    if (op1->gtOper != GT_LCL_VAR)
    {
        return nullptr;
    }

    // For Local AssertionProp we only can fold when op2 is a GT_CNS_INT
    if (op2->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    optOp1Kind op1Kind = O1K_LCLVAR;
    optOp2Kind op2Kind = O2K_CONST_INT;
    ssize_t    cnsVal  = op2->AsIntCon()->gtIconVal;
    var_types  cmpType = op1->TypeGet();

    // Don't try to fold/optimize Floating Compares; there are multiple zero values.
    if (varTypeIsFloating(cmpType))
    {
        return nullptr;
    }

    // Find an equal or not equal assertion about op1 var.
    unsigned lclNum = op1->AsLclVarCommon()->GetLclNum();
    noway_assert(lclNum < lvaCount);
    AssertionIndex index = optLocalAssertionIsEqualOrNotEqual(op1Kind, lclNum, op2Kind, cnsVal, assertions);

    if (index == NO_ASSERTION_INDEX)
    {
        return nullptr;
    }

    AssertionDsc* curAssertion = optGetAssertion(index);

    bool assertionKindIsEqual = (curAssertion->assertionKind == OAK_EQUAL);
    bool constantIsEqual      = false;

    if (genTypeSize(cmpType) == TARGET_POINTER_SIZE)
    {
        constantIsEqual = (curAssertion->op2.u1.iconVal == cnsVal);
    }
#ifdef TARGET_64BIT
    else if (genTypeSize(cmpType) == sizeof(INT32))
    {
        // Compare the low 32-bits only
        constantIsEqual = (((INT32)curAssertion->op2.u1.iconVal) == ((INT32)cnsVal));
    }
#endif
    else
    {
        // We currently don't fold/optimize when the GT_LCL_VAR has been cast to a small type
        return nullptr;
    }

    noway_assert(constantIsEqual || assertionKindIsEqual);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAssertion prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
        gtDispTree(tree, nullptr, nullptr, true);
    }
#endif

    // Return either CNS_INT 0 or CNS_INT 1.
    bool foldResult = (constantIsEqual == assertionKindIsEqual);
    if (tree->gtOper == GT_NE)
    {
        foldResult = !foldResult;
    }

    op2->AsIntCon()->gtIconVal = foldResult;
    op2->gtType                = TYP_INT;

    return optAssertionProp_Update(op2, tree, stmt);
}

//------------------------------------------------------------------------
// optAssertionProp_Cast: Propagate assertion for a cast, possibly removing it.
//
// The function use "optAssertionIsSubrange" to find an assertion which claims the
// cast's operand (only locals are supported) is a subrange of the "input" range
// for the cast, as computed by "IntegralRange::ForCastInput", and, if such
// assertion is found, act on it - either remove the cast if it is not changing
// representation, or try to remove the GTF_OVERFLOW flag from it.
//
// Arguments:
//    assertions - the set of live assertions
//    cast       - the cast for which to propagate the assertions
//    stmt       - statement "cast" is a part of, "nullptr" for local prop
//
// Return Value:
//    The, possibly modified, cast tree or "nullptr" if no propagation took place.
//
GenTree* Compiler::optAssertionProp_Cast(ASSERT_VALARG_TP assertions, GenTreeCast* cast, Statement* stmt)
{
    GenTree* op1 = cast->CastOp();

    // Bail if we have a cast involving floating point or GC types.
    if (!varTypeIsIntegral(cast) || !varTypeIsIntegral(op1))
    {
        return nullptr;
    }

    // Skip over a GT_COMMA node(s), if necessary to get to the lcl.
    GenTree* lcl = op1->gtEffectiveVal();

    // If we don't have a cast of a LCL_VAR then bail.
    if (!lcl->OperIs(GT_LCL_VAR))
    {
        return nullptr;
    }

    IntegralRange  range = IntegralRange::ForCastInput(cast);
    AssertionIndex index = optAssertionIsSubrange(lcl, range, assertions);
    if (index != NO_ASSERTION_INDEX)
    {
        LclVarDsc* varDsc = lvaGetDesc(lcl->AsLclVarCommon());

        // Representation-changing casts cannot be removed.
        if ((genActualType(cast) != genActualType(lcl)))
        {
            // Can we just remove the GTF_OVERFLOW flag?
            if (!cast->gtOverflow())
            {
                return nullptr;
            }
#ifdef DEBUG
            if (verbose)
            {
                printf("\nSubrange prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
                DISPNODE(cast);
            }
#endif
            cast->ClearOverflow();
            return optAssertionProp_Update(cast, cast, stmt);
        }

        // We might need to retype a "normalize on load" local back to its original small type
        // so that codegen recognizes it needs to use narrow loads if the local ends up in memory.
        if (varDsc->lvNormalizeOnLoad())
        {
            // The Jit is known to play somewhat loose with small types, so let's restrict this code
            // to the pattern we know is "safe and sound", i. e. CAST(type <- LCL_VAR(int, V00 type)).
            if ((varDsc->TypeGet() != cast->CastToType()) || !lcl->TypeIs(TYP_INT))
            {
                return nullptr;
            }

            op1->ChangeType(varDsc->TypeGet());
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSubrange prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
            DISPNODE(cast);
        }
#endif
        return optAssertionProp_Update(op1, cast, stmt);
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  Given a tree with an array bounds check node, eliminate it because it was
 *  checked already in the program.
 */
GenTree* Compiler::optAssertionProp_Comma(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    // Remove the bounds check as part of the GT_COMMA node since we need parent pointer to remove nodes.
    // When processing visits the bounds check, it sets the throw kind to None if the check is redundant.
    if (tree->gtGetOp1()->OperIs(GT_BOUNDS_CHECK) && ((tree->gtGetOp1()->gtFlags & GTF_CHK_INDEX_INBND) != 0))
    {
        optRemoveCommaBasedRangeCheck(tree, stmt);
        return optAssertionProp_Update(tree, tree, stmt);
    }
    return nullptr;
}

//------------------------------------------------------------------------
// optAssertionProp_Ind: see if we can prove the indirection can't cause
//    and exception.
//
// Arguments:
//   assertions  - set of live assertions
//   tree        - tree to possibly optimize
//   stmt        - statement containing the tree
//
// Returns:
//   The modified tree, or nullptr if no assertion prop took place.
//
// Notes:
//   stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optAssertionProp_Ind(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    assert(tree->OperIsIndir());

    if (!(tree->gtFlags & GTF_EXCEPT))
    {
        return nullptr;
    }

#ifdef DEBUG
    bool           vnBased = false;
    AssertionIndex index   = NO_ASSERTION_INDEX;
#endif
    if (optAssertionIsNonNull(tree->AsIndir()->Addr(), assertions DEBUGARG(&vnBased) DEBUGARG(&index)))
    {
#ifdef DEBUG
        if (verbose)
        {
            (vnBased) ? printf("\nVN based non-null prop in " FMT_BB ":\n", compCurBB->bbNum)
                      : printf("\nNon-null prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
            gtDispTree(tree, nullptr, nullptr, true);
        }
#endif
        tree->gtFlags &= ~GTF_EXCEPT;
        tree->gtFlags |= GTF_IND_NONFAULTING;

        // Set this flag to prevent reordering
        tree->gtFlags |= GTF_ORDER_SIDEEFF;

        return optAssertionProp_Update(tree, tree, stmt);
    }

    return nullptr;
}

//------------------------------------------------------------------------
// optAssertionIsNonNull: see if we can prove a tree's value will be non-null
//   based on assertions
//
// Arguments:
//   op - tree to check
//   assertions  - set of live assertions
//   pVnBased - [out] set to true if value numbers were used
//   pIndex - [out] the assertion used in the proof
//
// Returns:
//   true if the tree's value will be non-null
//
// Notes:
//   Sets "pVnBased" if the assertion is value number based. If no matching
//    assertions are found from the table, then returns "NO_ASSERTION_INDEX."
//
//   If both VN and assertion table yield a matching assertion, "pVnBased"
//   is only set and the return value is "NO_ASSERTION_INDEX."
//
bool Compiler::optAssertionIsNonNull(GenTree*         op,
                                     ASSERT_VALARG_TP assertions DEBUGARG(bool* pVnBased)
                                         DEBUGARG(AssertionIndex* pIndex))
{
    if (op->OperIs(GT_ADD) && op->AsOp()->gtGetOp2()->IsCnsIntOrI() &&
        !fgIsBigOffset(op->AsOp()->gtGetOp2()->AsIntCon()->IconValue()))
    {
        op = op->AsOp()->gtGetOp1();
    }

    bool vnBased = (!optLocalAssertionProp && vnStore->IsKnownNonNull(op->gtVNPair.GetConservative()));
#ifdef DEBUG
    *pIndex   = NO_ASSERTION_INDEX;
    *pVnBased = vnBased;
#endif

    if (vnBased)
    {
        return true;
    }

    if (!op->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    AssertionIndex index = optAssertionIsNonNullInternal(op, assertions DEBUGARG(pVnBased));
#ifdef DEBUG
    *pIndex = index;
#endif
    return index != NO_ASSERTION_INDEX;
}

//------------------------------------------------------------------------
// optAssertionIsNonNullInternal: see if we can prove a tree's value will
//   be non-null based on assertions
//
// Arguments:
//   op - tree to check
//   assertions  - set of live assertions
//   pVnBased - [out] set to true if value numbers were used
//
// Returns:
//   index of assertion, or NO_ASSERTION_INDEX
//
AssertionIndex Compiler::optAssertionIsNonNullInternal(GenTree*         op,
                                                       ASSERT_VALARG_TP assertions DEBUGARG(bool* pVnBased))
{

#ifdef DEBUG
    // Initialize the out param
    //
    *pVnBased = false;
#endif

    // If local assertion prop use lcl comparison, else use VN comparison.
    if (!optLocalAssertionProp)
    {
        if (BitVecOps::MayBeUninit(assertions) || BitVecOps::IsEmpty(apTraits, assertions))
        {
            return NO_ASSERTION_INDEX;
        }

        // Look at both the top-level vn, and
        // the vn we get by stripping off any constant adds.
        //
        ValueNum  vn     = vnStore->VNConservativeNormalValue(op->gtVNPair);
        ValueNum  vnBase = vn;
        VNFuncApp funcAttr;

        while (vnStore->GetVNFunc(vnBase, &funcAttr) && (funcAttr.m_func == (VNFunc)GT_ADD))
        {
            if (vnStore->IsVNConstant(funcAttr.m_args[1]) && varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[1])))
            {
                vnBase = funcAttr.m_args[0];
            }
            else if (vnStore->IsVNConstant(funcAttr.m_args[0]) &&
                     varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[0])))
            {
                vnBase = funcAttr.m_args[1];
            }
            else
            {
                break;
            }
        }

        // Check each assertion to find if we have a vn != null assertion.
        //
        BitVecOps::Iter iter(apTraits, assertions);
        unsigned        index = 0;
        while (iter.NextElem(&index))
        {
            AssertionIndex assertionIndex = GetAssertionIndex(index);
            if (assertionIndex > optAssertionCount)
            {
                break;
            }
            AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
            if (curAssertion->assertionKind != OAK_NOT_EQUAL)
            {
                continue;
            }

            if (curAssertion->op2.vn != ValueNumStore::VNForNull())
            {
                continue;
            }

            if ((curAssertion->op1.vn != vn) && (curAssertion->op1.vn != vnBase))
            {
                continue;
            }

#ifdef DEBUG
            *pVnBased = true;
#endif

            return assertionIndex;
        }
    }
    else
    {
        unsigned lclNum = op->AsLclVarCommon()->GetLclNum();
        // Check each assertion to find if we have a variable == or != null assertion.
        for (AssertionIndex index = 1; index <= optAssertionCount; index++)
        {
            AssertionDsc* curAssertion = optGetAssertion(index);
            if ((curAssertion->assertionKind == OAK_NOT_EQUAL) && // kind
                (curAssertion->op1.kind == O1K_LCLVAR) &&         // op1
                (curAssertion->op2.kind == O2K_CONST_INT) &&      // op2
                (curAssertion->op1.lcl.lclNum == lclNum) && (curAssertion->op2.u1.iconVal == 0))
            {
                return index;
            }
        }
    }
    return NO_ASSERTION_INDEX;
}
/*****************************************************************************
 *
 *  Given a tree consisting of a call and a set of available assertions, we
 *  try to propagate a non-null assertion and modify the Call tree if we can.
 *  Returns the modified tree, or nullptr if no assertion prop took place.
 *
 */
GenTree* Compiler::optNonNullAssertionProp_Call(ASSERT_VALARG_TP assertions, GenTreeCall* call)
{
    if (!call->NeedsNullCheck())
    {
        return nullptr;
    }

    GenTree* op1 = call->gtArgs.GetThisArg()->GetNode();
    noway_assert(op1 != nullptr);

#ifdef DEBUG
    bool           vnBased = false;
    AssertionIndex index   = NO_ASSERTION_INDEX;
#endif
    if (optAssertionIsNonNull(op1, assertions DEBUGARG(&vnBased) DEBUGARG(&index)))
    {
#ifdef DEBUG
        if (verbose)
        {
            (vnBased) ? printf("\nVN based non-null prop in " FMT_BB ":\n", compCurBB->bbNum)
                      : printf("\nNon-null prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
            gtDispTree(call, nullptr, nullptr, true);
        }
#endif
        call->gtFlags &= ~GTF_CALL_NULLCHECK;
        call->gtFlags &= ~GTF_EXCEPT;
        noway_assert(call->gtFlags & GTF_SIDE_EFFECT);
        return call;
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  Given a tree consisting of a call and a set of available assertions, we
 *  try to propagate an assertion and modify the Call tree if we can. Our
 *  current modifications are limited to removing the nullptrCHECK flag from
 *  the call.
 *  We pass in the root of the tree via 'stmt', for local copy prop 'stmt'
 *  will be nullptr. Returns the modified tree, or nullptr if no assertion prop
 *  took place.
 *
 */

GenTree* Compiler::optAssertionProp_Call(ASSERT_VALARG_TP assertions, GenTreeCall* call, Statement* stmt)
{
    if (optNonNullAssertionProp_Call(assertions, call))
    {
        return optAssertionProp_Update(call, call, stmt);
    }
    else if (!optLocalAssertionProp && (call->gtCallType == CT_HELPER))
    {
        if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFINTERFACE) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFARRAY) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFCLASS) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_ISINSTANCEOFANY) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_CHKCASTINTERFACE) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_CHKCASTARRAY) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_CHKCASTCLASS) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_CHKCASTANY) ||
            call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_CHKCASTCLASS_SPECIAL))
        {
            GenTree* arg1 = call->gtArgs.GetArgByIndex(1)->GetNode();
            if (arg1->gtOper != GT_LCL_VAR)
            {
                return nullptr;
            }

            GenTree* arg2 = call->gtArgs.GetArgByIndex(0)->GetNode();

            unsigned index = optAssertionIsSubtype(arg1, arg2, assertions);
            if (index != NO_ASSERTION_INDEX)
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nDid VN based subtype prop for index #%02u in " FMT_BB ":\n", index, compCurBB->bbNum);
                    gtDispTree(call, nullptr, nullptr, true);
                }
#endif
                GenTree* list = nullptr;
                gtExtractSideEffList(call, &list, GTF_SIDE_EFFECT, true);
                if (list != nullptr)
                {
                    arg1 = gtNewOperNode(GT_COMMA, call->TypeGet(), list, arg1);
                    fgSetTreeSeq(arg1);
                }

                return optAssertionProp_Update(arg1, call, stmt);
            }
        }
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  Given a tree with a bounds check, remove it if it has already been checked in the program flow.
 */
GenTree* Compiler::optAssertionProp_BndsChk(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt)
{
    if (optLocalAssertionProp)
    {
        return nullptr;
    }

    assert(tree->OperIs(GT_BOUNDS_CHECK));

#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
    if (JitConfig.JitNoRangeChks())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nFlagging check redundant due to JitNoRangeChks in " FMT_BB ":\n", compCurBB->bbNum);
            gtDispTree(tree, nullptr, nullptr, true);
        }
#endif // DEBUG
        tree->gtFlags |= GTF_CHK_INDEX_INBND;
        return nullptr;
    }
#endif // FEATURE_ENABLE_NO_RANGE_CHECKS

    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        AssertionIndex assertionIndex = GetAssertionIndex(index);
        if (assertionIndex > optAssertionCount)
        {
            break;
        }
        // If it is not a nothrow assertion, skip.
        AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
        if (!curAssertion->IsBoundsCheckNoThrow())
        {
            continue;
        }

        GenTreeBoundsChk* arrBndsChk = tree->AsBoundsChk();

        // Set 'isRedundant' to true if we can determine that 'arrBndsChk' can be
        // classified as a redundant bounds check using 'curAssertion'
        bool isRedundant = false;
#ifdef DEBUG
        const char* dbgMsg = "Not Set";
#endif

        // Do we have a previous range check involving the same 'vnLen' upper bound?
        if (curAssertion->op1.bnd.vnLen == vnStore->VNConservativeNormalValue(arrBndsChk->GetArrayLength()->gtVNPair))
        {
            ValueNum vnCurIdx = vnStore->VNConservativeNormalValue(arrBndsChk->GetIndex()->gtVNPair);

            // Do we have the exact same lower bound 'vnIdx'?
            //       a[i] followed by a[i]
            if (curAssertion->op1.bnd.vnIdx == vnCurIdx)
            {
                isRedundant = true;
#ifdef DEBUG
                dbgMsg = "a[i] followed by a[i]";
#endif
            }
            // Are we using zero as the index?
            // It can always be considered as redundant with any previous value
            //       a[*] followed by a[0]
            else if (vnCurIdx == vnStore->VNZeroForType(arrBndsChk->GetIndex()->TypeGet()))
            {
                isRedundant = true;
#ifdef DEBUG
                dbgMsg = "a[*] followed by a[0]";
#endif
            }
            // Do we have two constant indexes?
            else if (vnStore->IsVNConstant(curAssertion->op1.bnd.vnIdx) && vnStore->IsVNConstant(vnCurIdx))
            {
                // Make sure the types match.
                var_types type1 = vnStore->TypeOfVN(curAssertion->op1.bnd.vnIdx);
                var_types type2 = vnStore->TypeOfVN(vnCurIdx);

                if (type1 == type2 && type1 == TYP_INT)
                {
                    int index1 = vnStore->ConstantValue<int>(curAssertion->op1.bnd.vnIdx);
                    int index2 = vnStore->ConstantValue<int>(vnCurIdx);

                    // the case where index1 == index2 should have been handled above
                    assert(index1 != index2);

                    // It can always be considered as redundant with any previous higher constant value
                    //       a[K1] followed by a[K2], with K2 >= 0 and K1 >= K2
                    if (index2 >= 0 && index1 >= index2)
                    {
                        isRedundant = true;
#ifdef DEBUG
                        dbgMsg = "a[K1] followed by a[K2], with K2 >= 0 and K1 >= K2";
#endif
                    }
                }
            }
            // Extend this to remove additional redundant bounds checks:
            // i.e.  a[i+1] followed by a[i]  by using the VN(i+1) >= VN(i)
            //       a[i]   followed by a[j]  when j is known to be >= i
            //       a[i]   followed by a[5]  when i is known to be >= 5
        }

        if (!isRedundant)
        {
            continue;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\nVN based redundant (%s) bounds check assertion prop for index #%02u in " FMT_BB ":\n", dbgMsg,
                   assertionIndex, compCurBB->bbNum);
            gtDispTree(tree, nullptr, nullptr, true);
        }
#endif
        if (arrBndsChk == stmt->GetRootNode())
        {
            // We have a top-level bounds check node.
            // This can happen when trees are broken up due to inlining.
            // optRemoveStandaloneRangeCheck will return the modified tree (side effects or a no-op).
            GenTree* newTree = optRemoveStandaloneRangeCheck(arrBndsChk, stmt);

            return optAssertionProp_Update(newTree, arrBndsChk, stmt);
        }

        // Defer actually removing the tree until processing reaches its parent comma, since
        // optRemoveCommaBasedRangeCheck needs to rewrite the whole comma tree.
        arrBndsChk->gtFlags |= GTF_CHK_INDEX_INBND;

        return nullptr;
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  Called when we have a successfully performed an assertion prop. We have
 *  the newTree in hand. This method will replace the existing tree in the
 *  stmt with the newTree.
 *
 */

GenTree* Compiler::optAssertionProp_Update(GenTree* newTree, GenTree* tree, Statement* stmt)
{
    assert(newTree != nullptr);
    assert(tree != nullptr);

    if (stmt == nullptr)
    {
        noway_assert(optLocalAssertionProp);
    }
    else
    {
        noway_assert(!optLocalAssertionProp);

        // If newTree == tree then we modified the tree in-place otherwise we have to
        // locate our parent node and update it so that it points to newTree.
        if (newTree != tree)
        {
            FindLinkData linkData = gtFindLink(stmt, tree);
            GenTree**    useEdge  = linkData.result;
            GenTree*     parent   = linkData.parent;
            noway_assert(useEdge != nullptr);

            if (parent != nullptr)
            {
                parent->ReplaceOperand(useEdge, newTree);
            }
            else
            {
                // If there's no parent, the tree being replaced is the root of the
                // statement.
                assert((stmt->GetRootNode() == tree) && (stmt->GetRootNodePointer() == useEdge));
                stmt->SetRootNode(newTree);
            }

            // We only need to ensure that the gtNext field is set as it is used to traverse
            // to the next node in the tree. We will re-morph this entire statement in
            // optAssertionPropMain(). It will reset the gtPrev and gtNext links for all nodes.
            newTree->gtNext = tree->gtNext;

            // Old tree should not be referenced anymore.
            DEBUG_DESTROY_NODE(tree);
        }
    }

    // Record that we propagated the assertion.
    optAssertionPropagated            = true;
    optAssertionPropagatedCurrentStmt = true;

    return newTree;
}

//------------------------------------------------------------------------
// optAssertionProp: try and optimize a tree via assertion propagation
//
// Arguments:
//   assertions  - set of live assertions
//   tree        - tree to possibly optimize
//   stmt        - statement containing the tree
//   block       - block containing the statement
//
// Returns:
//   The modified tree, or nullptr if no assertion prop took place.
//
// Notes:
//   stmt may be nullptr during local assertion prop
//
GenTree* Compiler::optAssertionProp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt, BasicBlock* block)
{
    switch (tree->gtOper)
    {
        case GT_LCL_VAR:
            return optAssertionProp_LclVar(assertions, tree->AsLclVarCommon(), stmt);

        case GT_ASG:
            return optAssertionProp_Asg(assertions, tree->AsOp(), stmt);

        case GT_RETURN:
            return optAssertionProp_Return(assertions, tree->AsUnOp(), stmt);

        case GT_OBJ:
        case GT_BLK:
        case GT_IND:
        case GT_NULLCHECK:
        case GT_STORE_DYN_BLK:
            return optAssertionProp_Ind(assertions, tree, stmt);

        case GT_BOUNDS_CHECK:
            return optAssertionProp_BndsChk(assertions, tree, stmt);

        case GT_COMMA:
            return optAssertionProp_Comma(assertions, tree, stmt);

        case GT_CAST:
            return optAssertionProp_Cast(assertions, tree->AsCast(), stmt);

        case GT_CALL:
            return optAssertionProp_Call(assertions, tree->AsCall(), stmt);

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GT:
        case GT_GE:

            return optAssertionProp_RelOp(assertions, tree, stmt);

        case GT_JTRUE:

            if (block != nullptr)
            {
                return optVNConstantPropOnJTrue(block, tree);
            }
            return nullptr;

        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// optImpliedAssertions: Given an assertion this method computes the set
//                       of implied assertions that are also true.
//
// Arguments:
//      assertionIndex   : The id of the assertion.
//      activeAssertions : The assertions that are already true at this point.
//                         This method will add the discovered implied assertions
//                         to this set.
//
void Compiler::optImpliedAssertions(AssertionIndex assertionIndex, ASSERT_TP& activeAssertions)
{
    noway_assert(!optLocalAssertionProp);
    noway_assert(assertionIndex != 0);
    noway_assert(assertionIndex <= optAssertionCount);

    AssertionDsc* curAssertion = optGetAssertion(assertionIndex);
    if (!BitVecOps::IsEmpty(apTraits, activeAssertions))
    {
        const ASSERT_TP mappedAssertions = optGetVnMappedAssertions(curAssertion->op1.vn);
        if (mappedAssertions == nullptr)
        {
            return;
        }

        ASSERT_TP chkAssertions = BitVecOps::MakeCopy(apTraits, mappedAssertions);

        if (curAssertion->op2.kind == O2K_LCLVAR_COPY)
        {
            const ASSERT_TP op2Assertions = optGetVnMappedAssertions(curAssertion->op2.vn);
            if (op2Assertions != nullptr)
            {
                BitVecOps::UnionD(apTraits, chkAssertions, op2Assertions);
            }
        }
        BitVecOps::IntersectionD(apTraits, chkAssertions, activeAssertions);

        if (BitVecOps::IsEmpty(apTraits, chkAssertions))
        {
            return;
        }

        // Check each assertion in chkAssertions to see if it can be applied to curAssertion
        BitVecOps::Iter chkIter(apTraits, chkAssertions);
        unsigned        chkIndex = 0;
        while (chkIter.NextElem(&chkIndex))
        {
            AssertionIndex chkAssertionIndex = GetAssertionIndex(chkIndex);
            if (chkAssertionIndex > optAssertionCount)
            {
                break;
            }
            if (chkAssertionIndex == assertionIndex)
            {
                continue;
            }

            // Determine which one is a copy assertion and use the other to check for implied assertions.
            AssertionDsc* iterAssertion = optGetAssertion(chkAssertionIndex);
            if (curAssertion->IsCopyAssertion())
            {
                optImpliedByCopyAssertion(curAssertion, iterAssertion, activeAssertions);
            }
            else if (iterAssertion->IsCopyAssertion())
            {
                optImpliedByCopyAssertion(iterAssertion, curAssertion, activeAssertions);
            }
        }
    }
    // Is curAssertion a constant assignment of a 32-bit integer?
    // (i.e  GT_LVL_VAR X  == GT_CNS_INT)
    else if ((curAssertion->assertionKind == OAK_EQUAL) && (curAssertion->op1.kind == O1K_LCLVAR) &&
             (curAssertion->op2.kind == O2K_CONST_INT))
    {
        optImpliedByConstAssertion(curAssertion, activeAssertions);
    }
}

/*****************************************************************************
 *
 *   Given a set of active assertions this method computes the set
 *   of non-Null implied assertions that are also true
 */

void Compiler::optImpliedByTypeOfAssertions(ASSERT_TP& activeAssertions)
{
    if (BitVecOps::IsEmpty(apTraits, activeAssertions))
    {
        return;
    }

    // Check each assertion in activeAssertions to see if it can be applied to constAssertion
    BitVecOps::Iter chkIter(apTraits, activeAssertions);
    unsigned        chkIndex = 0;
    while (chkIter.NextElem(&chkIndex))
    {
        AssertionIndex chkAssertionIndex = GetAssertionIndex(chkIndex);
        if (chkAssertionIndex > optAssertionCount)
        {
            break;
        }
        // chkAssertion must be Type/Subtype is equal assertion
        AssertionDsc* chkAssertion = optGetAssertion(chkAssertionIndex);
        if ((chkAssertion->op1.kind != O1K_SUBTYPE && chkAssertion->op1.kind != O1K_EXACT_TYPE) ||
            (chkAssertion->assertionKind != OAK_EQUAL))
        {
            continue;
        }

        // Search the assertion table for a non-null assertion on op1 that matches chkAssertion
        for (AssertionIndex impIndex = 1; impIndex <= optAssertionCount; impIndex++)
        {
            AssertionDsc* impAssertion = optGetAssertion(impIndex);

            //  The impAssertion must be different from the chkAssertion
            if (impIndex == chkAssertionIndex)
            {
                continue;
            }

            // impAssertion must be a Non Null assertion on lclNum
            if ((impAssertion->assertionKind != OAK_NOT_EQUAL) ||
                ((impAssertion->op1.kind != O1K_LCLVAR) && (impAssertion->op1.kind != O1K_VALUE_NUMBER)) ||
                (impAssertion->op2.kind != O2K_CONST_INT) || (impAssertion->op1.vn != chkAssertion->op1.vn))
            {
                continue;
            }

            // The bit may already be in the result set
            if (!BitVecOps::IsMember(apTraits, activeAssertions, impIndex - 1))
            {
                BitVecOps::AddElemD(apTraits, activeAssertions, impIndex - 1);
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nCompiler::optImpliedByTypeOfAssertions: %s Assertion #%02d, implies assertion #%02d",
                           (chkAssertion->op1.kind == O1K_SUBTYPE) ? "Subtype" : "Exact-type", chkAssertionIndex,
                           impIndex);
                }
#endif
            }

            // There is at most one non-null assertion that is implied by the current chkIndex assertion
            break;
        }
    }
}

//------------------------------------------------------------------------
// optGetVnMappedAssertions: Given a value number, get the assertions
//                           we have about the value number.
//
// Arguments:
//      vn - The given value number.
//
// Return Value:
//      The assertions we have about the value number.
//
ASSERT_VALRET_TP Compiler::optGetVnMappedAssertions(ValueNum vn)
{
    ASSERT_TP set = BitVecOps::UninitVal();
    if (optValueNumToAsserts->Lookup(vn, &set))
    {
        return set;
    }
    return BitVecOps::UninitVal();
}

/*****************************************************************************
 *
 *   Given a const assertion this method computes the set of implied assertions
 *   that are also true
 */

void Compiler::optImpliedByConstAssertion(AssertionDsc* constAssertion, ASSERT_TP& result)
{
    noway_assert(constAssertion->assertionKind == OAK_EQUAL);
    noway_assert(constAssertion->op1.kind == O1K_LCLVAR);
    noway_assert(constAssertion->op2.kind == O2K_CONST_INT);

    ssize_t iconVal = constAssertion->op2.u1.iconVal;

    const ASSERT_TP chkAssertions = optGetVnMappedAssertions(constAssertion->op1.vn);
    if (chkAssertions == nullptr || BitVecOps::IsEmpty(apTraits, chkAssertions))
    {
        return;
    }

    // Check each assertion in chkAssertions to see if it can be applied to constAssertion
    BitVecOps::Iter chkIter(apTraits, chkAssertions);
    unsigned        chkIndex = 0;
    while (chkIter.NextElem(&chkIndex))
    {
        AssertionIndex chkAssertionIndex = GetAssertionIndex(chkIndex);
        if (chkAssertionIndex > optAssertionCount)
        {
            break;
        }
        // The impAssertion must be different from the const assertion.
        AssertionDsc* impAssertion = optGetAssertion(chkAssertionIndex);
        if (impAssertion == constAssertion)
        {
            continue;
        }

        // The impAssertion must be an assertion about the same local var.
        if (impAssertion->op1.vn != constAssertion->op1.vn)
        {
            continue;
        }

        bool usable = false;
        switch (impAssertion->op2.kind)
        {
            case O2K_SUBRANGE:
                // Is the const assertion's constant, within implied assertion's bounds?
                usable = impAssertion->op2.u2.Contains(iconVal);
                break;

            case O2K_CONST_INT:
                // Is the const assertion's constant equal/not equal to the implied assertion?
                usable = ((impAssertion->assertionKind == OAK_EQUAL) && (impAssertion->op2.u1.iconVal == iconVal)) ||
                         ((impAssertion->assertionKind == OAK_NOT_EQUAL) && (impAssertion->op2.u1.iconVal != iconVal));
                break;

            default:
                // leave 'usable' = false;
                break;
        }

        if (usable)
        {
            BitVecOps::AddElemD(apTraits, result, chkIndex);
#ifdef DEBUG
            if (verbose)
            {
                AssertionDsc* firstAssertion = optGetAssertion(1);
                printf("Compiler::optImpliedByConstAssertion: const assertion #%02d implies assertion #%02d\n",
                       (constAssertion - firstAssertion) + 1, (impAssertion - firstAssertion) + 1);
            }
#endif
        }
    }
}

/*****************************************************************************
 *
 *  Given a copy assertion and a dependent assertion this method computes the
 *  set of implied assertions that are also true.
 *  For copy assertions, exact SSA num and LCL nums should match, because
 *  we don't have kill sets and we depend on their value num for dataflow.
 */

void Compiler::optImpliedByCopyAssertion(AssertionDsc* copyAssertion, AssertionDsc* depAssertion, ASSERT_TP& result)
{
    noway_assert(copyAssertion->IsCopyAssertion());

    // Get the copyAssert's lcl/ssa nums.
    unsigned copyAssertLclNum = BAD_VAR_NUM;
    unsigned copyAssertSsaNum = SsaConfig::RESERVED_SSA_NUM;

    // Check if copyAssertion's op1 or op2 matches the depAssertion's op1.
    if (depAssertion->op1.lcl.lclNum == copyAssertion->op1.lcl.lclNum)
    {
        copyAssertLclNum = copyAssertion->op2.lcl.lclNum;
        copyAssertSsaNum = copyAssertion->op2.lcl.ssaNum;
    }
    else if (depAssertion->op1.lcl.lclNum == copyAssertion->op2.lcl.lclNum)
    {
        copyAssertLclNum = copyAssertion->op1.lcl.lclNum;
        copyAssertSsaNum = copyAssertion->op1.lcl.ssaNum;
    }
    // Check if copyAssertion's op1 or op2 matches the depAssertion's op2.
    else if (depAssertion->op2.kind == O2K_LCLVAR_COPY)
    {
        if (depAssertion->op2.lcl.lclNum == copyAssertion->op1.lcl.lclNum)
        {
            copyAssertLclNum = copyAssertion->op2.lcl.lclNum;
            copyAssertSsaNum = copyAssertion->op2.lcl.ssaNum;
        }
        else if (depAssertion->op2.lcl.lclNum == copyAssertion->op2.lcl.lclNum)
        {
            copyAssertLclNum = copyAssertion->op1.lcl.lclNum;
            copyAssertSsaNum = copyAssertion->op1.lcl.ssaNum;
        }
    }

    if (copyAssertLclNum == BAD_VAR_NUM || copyAssertSsaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return;
    }

    // Get the depAssert's lcl/ssa nums.
    unsigned depAssertLclNum = BAD_VAR_NUM;
    unsigned depAssertSsaNum = SsaConfig::RESERVED_SSA_NUM;
    if ((depAssertion->op1.kind == O1K_LCLVAR) && (depAssertion->op2.kind == O2K_LCLVAR_COPY))
    {
        if ((depAssertion->op1.lcl.lclNum == copyAssertion->op1.lcl.lclNum) ||
            (depAssertion->op1.lcl.lclNum == copyAssertion->op2.lcl.lclNum))
        {
            depAssertLclNum = depAssertion->op2.lcl.lclNum;
            depAssertSsaNum = depAssertion->op2.lcl.ssaNum;
        }
        else if ((depAssertion->op2.lcl.lclNum == copyAssertion->op1.lcl.lclNum) ||
                 (depAssertion->op2.lcl.lclNum == copyAssertion->op2.lcl.lclNum))
        {
            depAssertLclNum = depAssertion->op1.lcl.lclNum;
            depAssertSsaNum = depAssertion->op1.lcl.ssaNum;
        }
    }

    if (depAssertLclNum == BAD_VAR_NUM || depAssertSsaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return;
    }

    // Is depAssertion a constant assignment of a 32-bit integer?
    // (i.e  GT_LVL_VAR X == GT_CNS_INT)
    bool depIsConstAssertion = ((depAssertion->assertionKind == OAK_EQUAL) && (depAssertion->op1.kind == O1K_LCLVAR) &&
                                (depAssertion->op2.kind == O2K_CONST_INT));

    // Search the assertion table for an assertion on op1 that matches depAssertion
    // The matching assertion is the implied assertion.
    for (AssertionIndex impIndex = 1; impIndex <= optAssertionCount; impIndex++)
    {
        AssertionDsc* impAssertion = optGetAssertion(impIndex);

        //  The impAssertion must be different from the copy and dependent assertions
        if (impAssertion == copyAssertion || impAssertion == depAssertion)
        {
            continue;
        }

        if (!AssertionDsc::SameKind(depAssertion, impAssertion))
        {
            continue;
        }

        bool op1MatchesCopy =
            (copyAssertLclNum == impAssertion->op1.lcl.lclNum) && (copyAssertSsaNum == impAssertion->op1.lcl.ssaNum);

        bool usable = false;
        switch (impAssertion->op2.kind)
        {
            case O2K_SUBRANGE:
                usable = op1MatchesCopy && impAssertion->op2.u2.Contains(depAssertion->op2.u2);
                break;

            case O2K_CONST_LONG:
                usable = op1MatchesCopy && (impAssertion->op2.lconVal == depAssertion->op2.lconVal);
                break;

            case O2K_CONST_DOUBLE:
                // Exact memory match because of positive and negative zero
                usable = op1MatchesCopy &&
                         (memcmp(&impAssertion->op2.dconVal, &depAssertion->op2.dconVal, sizeof(double)) == 0);
                break;

            case O2K_IND_CNS_INT:
                // This is the ngen case where we have an indirection of an address.
                noway_assert((impAssertion->op1.kind == O1K_EXACT_TYPE) || (impAssertion->op1.kind == O1K_SUBTYPE));

                FALLTHROUGH;

            case O2K_CONST_INT:
                usable = op1MatchesCopy && (impAssertion->op2.u1.iconVal == depAssertion->op2.u1.iconVal);
                break;

            case O2K_LCLVAR_COPY:
                // Check if op1 of impAssertion matches copyAssertion and also op2 of impAssertion matches depAssertion.
                if (op1MatchesCopy && (depAssertLclNum == impAssertion->op2.lcl.lclNum &&
                                       depAssertSsaNum == impAssertion->op2.lcl.ssaNum))
                {
                    usable = true;
                }
                else
                {
                    // Otherwise, op2 of impAssertion should match copyAssertion and also op1 of impAssertion matches
                    // depAssertion.
                    usable = ((copyAssertLclNum == impAssertion->op2.lcl.lclNum &&
                               copyAssertSsaNum == impAssertion->op2.lcl.ssaNum) &&
                              (depAssertLclNum == impAssertion->op1.lcl.lclNum &&
                               depAssertSsaNum == impAssertion->op1.lcl.ssaNum));
                }
                break;

            default:
                // leave 'usable' = false;
                break;
        }

        if (usable)
        {
            BitVecOps::AddElemD(apTraits, result, impIndex - 1);

#ifdef DEBUG
            if (verbose)
            {
                AssertionDsc* firstAssertion = optGetAssertion(1);
                printf("\nCompiler::optImpliedByCopyAssertion: copyAssertion #%02d and depAssertion #%02d, implies "
                       "assertion #%02d",
                       (copyAssertion - firstAssertion) + 1, (depAssertion - firstAssertion) + 1,
                       (impAssertion - firstAssertion) + 1);
            }
#endif
            // If the depAssertion is a const assertion then any other assertions that it implies could also imply a
            // subrange assertion.
            if (depIsConstAssertion)
            {
                optImpliedByConstAssertion(impAssertion, result);
            }
        }
    }
}

#include "dataflow.h"

/*****************************************************************************
 *
 * Dataflow visitor like callback so that all dataflow is in a single place
 *
 */
class AssertionPropFlowCallback
{
private:
    ASSERT_TP preMergeOut;
    ASSERT_TP preMergeJumpDestOut;

    ASSERT_TP* mJumpDestOut;
    ASSERT_TP* mJumpDestGen;

    BitVecTraits* apTraits;

public:
    AssertionPropFlowCallback(Compiler* pCompiler, ASSERT_TP* jumpDestOut, ASSERT_TP* jumpDestGen)
        : preMergeOut(BitVecOps::UninitVal())
        , preMergeJumpDestOut(BitVecOps::UninitVal())
        , mJumpDestOut(jumpDestOut)
        , mJumpDestGen(jumpDestGen)
        , apTraits(pCompiler->apTraits)
    {
    }

    // At the start of the merge function of the dataflow equations, initialize premerge state (to detect change.)
    void StartMerge(BasicBlock* block)
    {
        if (VerboseDataflow())
        {
            JITDUMP("StartMerge: " FMT_BB " ", block->bbNum);
            Compiler::optDumpAssertionIndices("in -> ", block->bbAssertionIn, "\n");
        }

        BitVecOps::Assign(apTraits, preMergeOut, block->bbAssertionOut);
        BitVecOps::Assign(apTraits, preMergeJumpDestOut, mJumpDestOut[block->bbNum]);
    }

    // During merge, perform the actual merging of the predecessor's (since this is a forward analysis) dataflow flags.
    void Merge(BasicBlock* block, BasicBlock* predBlock, unsigned dupCount)
    {
        ASSERT_TP pAssertionOut;

        if (predBlock->bbJumpKind == BBJ_COND && (predBlock->bbJumpDest == block))
        {
            pAssertionOut = mJumpDestOut[predBlock->bbNum];

            if (dupCount > 1)
            {
                // Scenario where next block and conditional block, both point to the same block.
                // In such case, intersect the assertions present on both the out edges of predBlock.
                assert(predBlock->bbNext == block);
                BitVecOps::IntersectionD(apTraits, pAssertionOut, predBlock->bbAssertionOut);

                if (VerboseDataflow())
                {
                    JITDUMP("Merge     : Duplicate flow, " FMT_BB " ", block->bbNum);
                    Compiler::optDumpAssertionIndices("in -> ", block->bbAssertionIn, "; ");
                    JITDUMP("pred " FMT_BB " ", predBlock->bbNum);
                    Compiler::optDumpAssertionIndices("out1 -> ", mJumpDestOut[predBlock->bbNum], "; ");
                    Compiler::optDumpAssertionIndices("out2 -> ", predBlock->bbAssertionOut, "\n");
                }
            }
        }
        else
        {
            pAssertionOut = predBlock->bbAssertionOut;
        }

        if (VerboseDataflow())
        {
            JITDUMP("Merge     : " FMT_BB " ", block->bbNum);
            Compiler::optDumpAssertionIndices("in -> ", block->bbAssertionIn, "; ");
            JITDUMP("pred " FMT_BB " ", predBlock->bbNum);
            Compiler::optDumpAssertionIndices("out -> ", pAssertionOut, "\n");
        }

        BitVecOps::IntersectionD(apTraits, block->bbAssertionIn, pAssertionOut);
    }

    //------------------------------------------------------------------------
    // MergeHandler: Merge assertions into the first exception handler/filter block.
    //
    // Arguments:
    //   block         - the block that is the start of a handler or filter;
    //   firstTryBlock - the first block of the try for "block" handler;
    //   lastTryBlock  - the last block of the try for "block" handler;.
    //
    // Notes:
    //   We can jump to the handler from any instruction in the try region.
    //   It means we can propagate only assertions that are valid for the whole try region.
    void MergeHandler(BasicBlock* block, BasicBlock* firstTryBlock, BasicBlock* lastTryBlock)
    {
        if (VerboseDataflow())
        {
            JITDUMP("Merge     : " FMT_BB " ", block->bbNum);
            Compiler::optDumpAssertionIndices("in -> ", block->bbAssertionIn, "; ");
            JITDUMP("firstTryBlock " FMT_BB " ", firstTryBlock->bbNum);
            Compiler::optDumpAssertionIndices("in -> ", firstTryBlock->bbAssertionIn, "; ");
            JITDUMP("lastTryBlock " FMT_BB " ", lastTryBlock->bbNum);
            Compiler::optDumpAssertionIndices("out -> ", lastTryBlock->bbAssertionOut, "\n");
        }
        BitVecOps::IntersectionD(apTraits, block->bbAssertionIn, firstTryBlock->bbAssertionIn);
        BitVecOps::IntersectionD(apTraits, block->bbAssertionIn, lastTryBlock->bbAssertionOut);
    }

    // At the end of the merge store results of the dataflow equations, in a postmerge state.
    bool EndMerge(BasicBlock* block)
    {
        if (VerboseDataflow())
        {
            JITDUMP("EndMerge  : " FMT_BB " ", block->bbNum);
            Compiler::optDumpAssertionIndices("in -> ", block->bbAssertionIn, "\n\n");
        }

        BitVecOps::DataFlowD(apTraits, block->bbAssertionOut, block->bbAssertionGen, block->bbAssertionIn);
        BitVecOps::DataFlowD(apTraits, mJumpDestOut[block->bbNum], mJumpDestGen[block->bbNum], block->bbAssertionIn);

        bool changed = (!BitVecOps::Equal(apTraits, preMergeOut, block->bbAssertionOut) ||
                        !BitVecOps::Equal(apTraits, preMergeJumpDestOut, mJumpDestOut[block->bbNum]));

        if (VerboseDataflow())
        {
            if (changed)
            {
                JITDUMP("Changed   : " FMT_BB " ", block->bbNum);
                Compiler::optDumpAssertionIndices("before out -> ", preMergeOut, "; ");
                Compiler::optDumpAssertionIndices("after out -> ", block->bbAssertionOut, ";\n        ");
                Compiler::optDumpAssertionIndices("jumpDest before out -> ", preMergeJumpDestOut, "; ");
                Compiler::optDumpAssertionIndices("jumpDest after out -> ", mJumpDestOut[block->bbNum], ";\n\n");
            }
            else
            {
                JITDUMP("Unchanged : " FMT_BB " ", block->bbNum);
                Compiler::optDumpAssertionIndices("out -> ", block->bbAssertionOut, "; ");
                Compiler::optDumpAssertionIndices("jumpDest out -> ", mJumpDestOut[block->bbNum], "\n\n");
            }
        }

        return changed;
    }

    // Can be enabled to get detailed debug output about dataflow for assertions.
    bool VerboseDataflow()
    {
#if 0
        return VERBOSE;
#endif
        return false;
    }
};

/*****************************************************************************
 *
 *   Compute the assertions generated by each block.
 */
ASSERT_TP* Compiler::optComputeAssertionGen()
{
    ASSERT_TP* jumpDestGen = fgAllocateTypeForEachBlk<ASSERT_TP>();

    for (BasicBlock* const block : Blocks())
    {
        ASSERT_TP valueGen = BitVecOps::MakeEmpty(apTraits);
        GenTree*  jtrue    = nullptr;

        // Walk the statement trees in this basic block.
        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                if (tree->gtOper == GT_JTRUE)
                {
                    // A GT_TRUE is always the last node in a tree, so we can break here
                    assert((tree->gtNext == nullptr) && (stmt->GetNextStmt() == nullptr));
                    jtrue = tree;
                    break;
                }

                if (tree->GeneratesAssertion())
                {
                    AssertionInfo info = tree->GetAssertionInfo();
                    optImpliedAssertions(info.GetAssertionIndex(), valueGen);
                    BitVecOps::AddElemD(apTraits, valueGen, info.GetAssertionIndex() - 1);
                }
            }
        }

        if (jtrue != nullptr)
        {
            // Copy whatever we have accumulated into jumpDest edge's valueGen.
            ASSERT_TP jumpDestValueGen = BitVecOps::MakeCopy(apTraits, valueGen);

            if (jtrue->GeneratesAssertion())
            {
                AssertionInfo  info = jtrue->GetAssertionInfo();
                AssertionIndex valueAssertionIndex;
                AssertionIndex jumpDestAssertionIndex;

                if (info.IsNextEdgeAssertion())
                {
                    valueAssertionIndex    = info.GetAssertionIndex();
                    jumpDestAssertionIndex = optFindComplementary(info.GetAssertionIndex());
                }
                else // is jump edge assertion
                {
                    jumpDestAssertionIndex = info.GetAssertionIndex();
                    valueAssertionIndex    = optFindComplementary(jumpDestAssertionIndex);
                }

                if (valueAssertionIndex != NO_ASSERTION_INDEX)
                {
                    // Update valueGen if we have an assertion for the bbNext edge
                    optImpliedAssertions(valueAssertionIndex, valueGen);
                    BitVecOps::AddElemD(apTraits, valueGen, valueAssertionIndex - 1);
                }

                if (jumpDestAssertionIndex != NO_ASSERTION_INDEX)
                {
                    // Update jumpDestValueGen if we have an assertion for the bbJumpDest edge
                    optImpliedAssertions(jumpDestAssertionIndex, jumpDestValueGen);
                    BitVecOps::AddElemD(apTraits, jumpDestValueGen, jumpDestAssertionIndex - 1);
                }
            }

            jumpDestGen[block->bbNum] = jumpDestValueGen;
        }
        else
        {
            jumpDestGen[block->bbNum] = BitVecOps::MakeEmpty(apTraits);
        }

        block->bbAssertionGen = valueGen;

#ifdef DEBUG
        if (verbose)
        {
            if (block == fgFirstBB)
            {
                printf("\n");
            }

            printf(FMT_BB " valueGen = ", block->bbNum);
            optPrintAssertionIndices(block->bbAssertionGen);
            if (block->bbJumpKind == BBJ_COND)
            {
                printf(" => " FMT_BB " valueGen = ", block->bbJumpDest->bbNum);
                optPrintAssertionIndices(jumpDestGen[block->bbNum]);
            }
            printf("\n");

            if (block == fgLastBB)
            {
                printf("\n");
            }
        }
#endif
    }

    return jumpDestGen;
}

/*****************************************************************************
 *
 *   Initialize the assertion data flow flags that will be propagated.
 */

ASSERT_TP* Compiler::optInitAssertionDataflowFlags()
{
    ASSERT_TP* jumpDestOut = fgAllocateTypeForEachBlk<ASSERT_TP>();

    // The local assertion gen phase may have created unreachable blocks.
    // They will never be visited in the dataflow propagation phase, so they need to
    // be initialized correctly. This means that instead of setting their sets to
    // apFull (i.e. all possible bits set), we need to set the bits only for valid
    // assertions (note that at this point we are not creating any new assertions).
    // Also note that assertion indices start from 1.
    ASSERT_TP apValidFull = BitVecOps::MakeEmpty(apTraits);
    for (int i = 1; i <= optAssertionCount; i++)
    {
        BitVecOps::AddElemD(apTraits, apValidFull, i - 1);
    }

    // Initially estimate the OUT sets to everything except killed expressions
    // Also set the IN sets to 1, so that we can perform the intersection.
    for (BasicBlock* const block : Blocks())
    {
        block->bbAssertionIn      = BitVecOps::MakeCopy(apTraits, apValidFull);
        block->bbAssertionGen     = BitVecOps::MakeEmpty(apTraits);
        block->bbAssertionOut     = BitVecOps::MakeCopy(apTraits, apValidFull);
        jumpDestOut[block->bbNum] = BitVecOps::MakeCopy(apTraits, apValidFull);
    }
    // Compute the data flow values for all tracked expressions
    // IN and OUT never change for the initial basic block B1
    BitVecOps::ClearD(apTraits, fgFirstBB->bbAssertionIn);
    return jumpDestOut;
}

// Callback data for the VN based constant prop visitor.
struct VNAssertionPropVisitorInfo
{
    Compiler*   pThis;
    Statement*  stmt;
    BasicBlock* block;
    VNAssertionPropVisitorInfo(Compiler* pThis, BasicBlock* block, Statement* stmt)
        : pThis(pThis), stmt(stmt), block(block)
    {
    }
};

//------------------------------------------------------------------------------
// optExtractSideEffListFromConst
//    Extracts side effects from a tree so it can be replaced with a comma
//    separated list of side effects + a const tree.
//
// Note:
//   The caller expects that the root of the tree has no side effects and it
//   won't be extracted. Otherwise the resulting comma tree would be bigger
//   than the tree before optimization.
//
// Arguments:
//    tree  - The tree node with constant value to extrace side-effects from.
//
// Return Value:
//      1. Returns the extracted side-effects from "tree"
//      2. When no side-effects are present, returns null.
//
//
GenTree* Compiler::optExtractSideEffListFromConst(GenTree* tree)
{
    assert(vnStore->IsVNConstant(vnStore->VNConservativeNormalValue(tree->gtVNPair)));

    GenTree* sideEffList = nullptr;

    // If we have side effects, extract them.
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        // Do a sanity check to ensure persistent side effects aren't discarded and
        // tell gtExtractSideEffList to ignore the root of the tree.
        // We are relying here on an invariant that VN will only fold non-throwing expressions.
        const bool ignoreExceptions = true;
        const bool ignoreCctors     = false;
        // We have to check "AsCall()->HasSideEffects()" here separately because "gtNodeHasSideEffects"
        // also checks for side effects that arguments introduce (incosistently so, it otherwise only
        // checks for the side effects the node itself has). TODO-Cleanup: change it to not do that?
        assert(!gtNodeHasSideEffects(tree, GTF_PERSISTENT_SIDE_EFFECTS) ||
               (tree->IsCall() && !tree->AsCall()->HasSideEffects(this, ignoreExceptions, ignoreCctors)));

        // Exception side effects may be ignored because the root is known to be a constant
        // (e.g. VN may evaluate a DIV/MOD node to a constant and the node may still
        // have GTF_EXCEPT set, even if it does not actually throw any exceptions).
        bool ignoreRoot = true;

        gtExtractSideEffList(tree, &sideEffList, GTF_SIDE_EFFECT, ignoreRoot);

        JITDUMP("Extracted side effects from a constant tree [%06u]:\n", tree->gtTreeID);
        DISPTREE(sideEffList);
    }

    return sideEffList;
}

//------------------------------------------------------------------------------
// optVNConstantPropOnJTrue
//    Constant propagate on the JTrue node by extracting side effects and moving
//    them into their own statements. The relop node is then modified to yield
//    true or false, so the branch can be folded.
//
// Arguments:
//    block - The block that contains the JTrue.
//    test  - The JTrue node whose relop evaluates to 0 or non-zero value.
//
// Return Value:
//    The jmpTrue tree node that has relop of the form "0 =/!= 0".
//    If "tree" evaluates to "true" relop is "0 == 0". Else relop is "0 != 0".
//
// Description:
//    Special treatment for JTRUE nodes' constant propagation. This is because
//    for JTRUE(1) or JTRUE(0), if there are side effects they need to be put
//    in separate statements. This is to prevent relop's constant
//    propagation from doing a simple minded conversion from
//    (1) STMT(JTRUE(RELOP(COMMA(sideEffect, OP1), OP2)), S.T. op1 =/!= op2 to
//    (2) STMT(JTRUE(COMMA(sideEffect, 1/0)).
//
//    fgFoldConditional doesn't fold (2), a side-effecting JTRUE's op1. So, let us,
//    here, convert (1) as two statements: STMT(sideEffect), STMT(JTRUE(1/0)),
//    so that the JTRUE will get folded by fgFoldConditional.
//
//  Note: fgFoldConditional is called from other places as well, which may be
//  sensitive to adding new statements. Hence the change is not made directly
//  into fgFoldConditional.
//
GenTree* Compiler::optVNConstantPropOnJTrue(BasicBlock* block, GenTree* test)
{
    GenTree* relop = test->gtGetOp1();

    // VN based assertion non-null on this relop has been performed.
    if (!relop->OperIsCompare())
    {
        return nullptr;
    }

    //
    // Make sure GTF_RELOP_JMP_USED flag is set so that we can later skip constant
    // prop'ing a JTRUE's relop child node for a second time in the pre-order
    // tree walk.
    //
    assert((relop->gtFlags & GTF_RELOP_JMP_USED) != 0);

    // We want to use the Normal ValueNumber when checking for constants.
    ValueNum vnCns = vnStore->VNConservativeNormalValue(relop->gtVNPair);
    ValueNum vnLib = vnStore->VNLiberalNormalValue(relop->gtVNPair);
    if (!vnStore->IsVNConstant(vnCns))
    {
        return nullptr;
    }

    // Prepare the tree for replacement so any side effects can be extracted.
    GenTree* sideEffList = optExtractSideEffListFromConst(relop);

    // Transform the relop's operands to be both zeroes.
    ValueNum vnZero                = vnStore->VNZeroForType(TYP_INT);
    relop->AsOp()->gtOp1           = gtNewIconNode(0);
    relop->AsOp()->gtOp1->gtVNPair = ValueNumPair(vnZero, vnZero);
    relop->AsOp()->gtOp2           = gtNewIconNode(0);
    relop->AsOp()->gtOp2->gtVNPair = ValueNumPair(vnZero, vnZero);

    // Update the oper and restore the value numbers.
    bool evalsToTrue = (vnStore->CoercedConstantValue<INT64>(vnCns) != 0);
    relop->SetOper(evalsToTrue ? GT_EQ : GT_NE);
    relop->gtVNPair = ValueNumPair(vnLib, vnCns);

    // Insert side effects back after they were removed from the JTrue stmt.
    // It is important not to allow duplicates exist in the IR, that why we delete
    // these side effects from the JTrue stmt before insert them back here.
    while (sideEffList != nullptr)
    {
        Statement* newStmt;
        if (sideEffList->OperGet() == GT_COMMA)
        {
            newStmt     = fgNewStmtNearEnd(block, sideEffList->gtGetOp1());
            sideEffList = sideEffList->gtGetOp2();
        }
        else
        {
            newStmt     = fgNewStmtNearEnd(block, sideEffList);
            sideEffList = nullptr;
        }
        // fgMorphBlockStmt could potentially affect stmts after the current one,
        // for example when it decides to fgRemoveRestOfBlock.
        fgMorphBlockStmt(block, newStmt DEBUGARG(__FUNCTION__));
    }

    return test;
}

//------------------------------------------------------------------------------
// optVNConstantPropCurStmt
//    Performs constant prop on the current statement's tree nodes.
//
// Assumption:
//    This function is called as part of a pre-order tree walk.
//
// Arguments:
//    tree  - The currently visited tree node.
//    stmt  - The statement node in which the "tree" is present.
//    block - The block that contains the statement that contains the tree.
//
// Return Value:
//    Returns the standard visitor walk result.
//
// Description:
//    Checks if a node is an R-value and evaluates to a constant. If the node
//    evaluates to constant, then the tree is replaced by its side effects and
//    the constant node.
//
Compiler::fgWalkResult Compiler::optVNConstantPropCurStmt(BasicBlock* block, Statement* stmt, GenTree* tree)
{
    // Don't perform const prop on expressions marked with GTF_DONT_CSE
    if (!tree->CanCSE())
    {
        return WALK_CONTINUE;
    }

    // Don't propagate floating-point constants into a TYP_STRUCT LclVar
    // This can occur for HFA return values (see hfa_sf3E_r.exe)
    if (tree->TypeGet() == TYP_STRUCT)
    {
        return WALK_CONTINUE;
    }

    switch (tree->OperGet())
    {
        // Make sure we have an R-value.
        case GT_ADD:
        case GT_SUB:
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_NEG:
        case GT_CAST:
        case GT_INTRINSIC:
            break;

        case GT_JTRUE:
            break;

        case GT_MUL:
            // Don't transform long multiplies.
            if (tree->gtFlags & GTF_MUL_64RSLT)
            {
                return WALK_SKIP_SUBTREES;
            }
            break;

        case GT_LCL_VAR:
        case GT_LCL_FLD:
            // Make sure the local variable is an R-value.
            if ((tree->gtFlags & (GTF_VAR_USEASG | GTF_VAR_DEF | GTF_DONT_CSE)) != GTF_EMPTY)
            {
                return WALK_CONTINUE;
            }
            // Let's not conflict with CSE (to save the movw/movt).
            if (lclNumIsCSE(tree->AsLclVarCommon()->GetLclNum()))
            {
                return WALK_CONTINUE;
            }
            break;

        case GT_CALL:
            if (!tree->AsCall()->IsPure(this))
            {
                return WALK_CONTINUE;
            }
            break;

        default:
            // Unknown node, continue to walk.
            return WALK_CONTINUE;
    }

    // Perform the constant propagation
    GenTree* newTree = optVNConstantPropOnTree(block, tree);
    if (newTree == nullptr)
    {
        // Not propagated, keep going.
        return WALK_CONTINUE;
    }

    // Successful propagation, mark as assertion propagated and skip
    // sub-tree (with side-effects) visits.
    // TODO #18291: at that moment stmt could be already removed from the stmt list.

    optAssertionProp_Update(newTree, tree, stmt);

    JITDUMP("After constant propagation on [%06u]:\n", tree->gtTreeID);
    DBEXEC(VERBOSE, gtDispStmt(stmt));

    return WALK_SKIP_SUBTREES;
}

//------------------------------------------------------------------------------
// optVnNonNullPropCurStmt
//    Performs VN based non-null propagation on the tree node.
//
// Assumption:
//    This function is called as part of a pre-order tree walk.
//
// Arguments:
//    block - The block that contains the statement that contains the tree.
//    stmt  - The statement node in which the "tree" is present.
//    tree  - The currently visited tree node.
//
// Return Value:
//    None.
//
// Description:
//    Performs value number based non-null propagation on GT_CALL and
//    indirections. This is different from flow based assertions and helps
//    unify VN based constant prop and non-null prop in a single pre-order walk.
//
void Compiler::optVnNonNullPropCurStmt(BasicBlock* block, Statement* stmt, GenTree* tree)
{
    ASSERT_TP empty   = BitVecOps::UninitVal();
    GenTree*  newTree = nullptr;
    if (tree->OperGet() == GT_CALL)
    {
        newTree = optNonNullAssertionProp_Call(empty, tree->AsCall());
    }
    else if (tree->OperIsIndir())
    {
        newTree = optAssertionProp_Ind(empty, tree, stmt);
    }
    if (newTree)
    {
        assert(newTree == tree);
        optAssertionProp_Update(newTree, tree, stmt);
    }
}

//------------------------------------------------------------------------------
// optVNAssertionPropCurStmtVisitor
//    Unified Value Numbering based assertion propagation visitor.
//
// Assumption:
//    This function is called as part of a pre-order tree walk.
//
// Return Value:
//    WALK_RESULTs.
//
// Description:
//    An unified value numbering based assertion prop visitor that
//    performs non-null and constant assertion propagation based on
//    value numbers.
//
/* static */
Compiler::fgWalkResult Compiler::optVNAssertionPropCurStmtVisitor(GenTree** ppTree, fgWalkData* data)
{
    VNAssertionPropVisitorInfo* pData = (VNAssertionPropVisitorInfo*)data->pCallbackData;
    Compiler*                   pThis = pData->pThis;

    pThis->optVnNonNullPropCurStmt(pData->block, pData->stmt, *ppTree);

    return pThis->optVNConstantPropCurStmt(pData->block, pData->stmt, *ppTree);
}

/*****************************************************************************
 *
 *   Perform VN based i.e., data flow based assertion prop first because
 *   even if we don't gen new control flow assertions, we still propagate
 *   these first.
 *
 *   Returns the skipped next stmt if the current statement or next few
 *   statements got removed, else just returns the incoming stmt.
 */
Statement* Compiler::optVNAssertionPropCurStmt(BasicBlock* block, Statement* stmt)
{
    // TODO-Review: EH successor/predecessor iteration seems broken.
    // See: SELF_HOST_TESTS_ARM\jit\Directed\ExcepFilters\fault\fault.exe
    if (block->bbCatchTyp == BBCT_FAULT)
    {
        return stmt;
    }

    // Preserve the prev link before the propagation and morph.
    Statement* prev = (stmt == block->firstStmt()) ? nullptr : stmt->GetPrevStmt();

    // Perform VN based assertion prop first, in case we don't find
    // anything in assertion gen.
    optAssertionPropagatedCurrentStmt = false;

    VNAssertionPropVisitorInfo data(this, block, stmt);
    fgWalkTreePre(stmt->GetRootNodePointer(), Compiler::optVNAssertionPropCurStmtVisitor, &data);

    if (optAssertionPropagatedCurrentStmt)
    {
        fgMorphBlockStmt(block, stmt DEBUGARG("optVNAssertionPropCurStmt"));
    }

    // Check if propagation removed statements starting from current stmt.
    // If so, advance to the next good statement.
    Statement* nextStmt = (prev == nullptr) ? block->firstStmt() : prev->GetNextStmt();
    return nextStmt;
}

/*****************************************************************************
 *
 *   The entry point for assertion propagation
 */

void Compiler::optAssertionPropMain()
{
    if (fgSsaPassesCompleted == 0)
    {
        return;
    }
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optAssertionPropMain()\n");
        printf("Blocks/Trees at start of phase\n");
        fgDispBasicBlocks(true);
    }
#endif

    optAssertionInit(false);

    noway_assert(optAssertionCount == 0);

    // First discover all value assignments and record them in the table.
    for (BasicBlock* const block : Blocks())
    {
        compCurBB = block;

        fgRemoveRestOfBlock = false;

        Statement* stmt = block->firstStmt();
        while (stmt != nullptr)
        {
            // We need to remove the rest of the block.
            if (fgRemoveRestOfBlock)
            {
                fgRemoveStmt(block, stmt);
                stmt = stmt->GetNextStmt();
                continue;
            }
            else
            {
                // Perform VN based assertion prop before assertion gen.
                Statement* nextStmt = optVNAssertionPropCurStmt(block, stmt);

                // Propagation resulted in removal of the remaining stmts, perform it.
                if (fgRemoveRestOfBlock)
                {
                    stmt = stmt->GetNextStmt();
                    continue;
                }

                // Propagation removed the current stmt or next few stmts, so skip them.
                if (stmt != nextStmt)
                {
                    stmt = nextStmt;
                    continue;
                }
            }

            // Perform assertion gen for control flow based assertions.
            for (GenTree* const tree : stmt->TreeList())
            {
                optAssertionGen(tree);
            }

            // Advance the iterator
            stmt = stmt->GetNextStmt();
        }
    }

    if (optAssertionCount == 0)
    {
        // Zero out the bbAssertionIn values, as these can be referenced in RangeCheck::MergeAssertion
        // and this is sharedstate with the CSE phase: bbCseIn
        //
        for (BasicBlock* const block : Blocks())
        {
            block->bbAssertionIn = BitVecOps::MakeEmpty(apTraits);
        }
        return;
    }

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    // Allocate the bits for the predicate sensitive dataflow analysis
    bbJtrueAssertionOut    = optInitAssertionDataflowFlags();
    ASSERT_TP* jumpDestGen = optComputeAssertionGen();

    // Modified dataflow algorithm for available expressions.
    DataFlow                  flow(this);
    AssertionPropFlowCallback ap(this, bbJtrueAssertionOut, jumpDestGen);
    if (ap.VerboseDataflow())
    {
        JITDUMP("AssertionPropFlowCallback:\n\n")
    }
    flow.ForwardAnalysis(ap);

    for (BasicBlock* const block : Blocks())
    {
        // Compute any implied non-Null assertions for block->bbAssertionIn
        optImpliedByTypeOfAssertions(block->bbAssertionIn);
    }

#ifdef DEBUG
    if (verbose)
    {
        for (BasicBlock* const block : Blocks())
        {
            printf(FMT_BB ":\n", block->bbNum);
            optDumpAssertionIndices(" in   = ", block->bbAssertionIn, "\n");
            optDumpAssertionIndices(" out  = ", block->bbAssertionOut, "\n");
            if (block->bbJumpKind == BBJ_COND)
            {
                printf(" " FMT_BB " = ", block->bbJumpDest->bbNum);
                optDumpAssertionIndices(bbJtrueAssertionOut[block->bbNum], "\n");
            }
        }
        printf("\n");
    }
#endif // DEBUG

    ASSERT_TP assertions = BitVecOps::MakeEmpty(apTraits);

    // Perform assertion propagation (and constant folding)
    for (BasicBlock* const block : Blocks())
    {
        BitVecOps::Assign(apTraits, assertions, block->bbAssertionIn);

        // TODO-Review: EH successor/predecessor iteration seems broken.
        // SELF_HOST_TESTS_ARM\jit\Directed\ExcepFilters\fault\fault.exe
        if (block->bbCatchTyp == BBCT_FAULT)
        {
            continue;
        }

        // Make the current basic block address available globally.
        compCurBB           = block;
        fgRemoveRestOfBlock = false;

        // Walk the statement trees in this basic block
        Statement* stmt = block->FirstNonPhiDef();
        while (stmt != nullptr)
        {
            // Propagation tells us to remove the rest of the block. Remove it.
            if (fgRemoveRestOfBlock)
            {
                fgRemoveStmt(block, stmt);
                stmt = stmt->GetNextStmt();
                continue;
            }

            // Preserve the prev link before the propagation and morph, to check if propagation
            // removes the current stmt.
            Statement* prevStmt = (stmt == block->firstStmt()) ? nullptr : stmt->GetPrevStmt();

            optAssertionPropagatedCurrentStmt = false; // set to true if a assertion propagation took place
                                                       // and thus we must morph, set order, re-link
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                optDumpAssertionIndices("Propagating ", assertions, " ");
                JITDUMP("for " FMT_BB ", stmt " FMT_STMT ", tree [%06d]", block->bbNum, stmt->GetID(), dspTreeID(tree));
                JITDUMP(", tree -> ");
                JITDUMPEXEC(optPrintAssertionIndex(tree->GetAssertionInfo().GetAssertionIndex()));
                JITDUMP("\n");

                GenTree* newTree = optAssertionProp(assertions, tree, stmt, block);
                if (newTree)
                {
                    assert(optAssertionPropagatedCurrentStmt == true);
                    tree = newTree;
                }

                // If this tree makes an assertion - make it available.
                if (tree->GeneratesAssertion())
                {
                    AssertionInfo info = tree->GetAssertionInfo();
                    optImpliedAssertions(info.GetAssertionIndex(), assertions);
                    BitVecOps::AddElemD(apTraits, assertions, info.GetAssertionIndex() - 1);
                }
            }

            if (optAssertionPropagatedCurrentStmt)
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("Re-morphing this stmt:\n");
                    gtDispStmt(stmt);
                    printf("\n");
                }
#endif
                // Re-morph the statement.
                fgMorphBlockStmt(block, stmt DEBUGARG("optAssertionPropMain"));
            }

            // Check if propagation removed statements starting from current stmt.
            // If so, advance to the next good statement.
            Statement* nextStmt = (prevStmt == nullptr) ? block->firstStmt() : prevStmt->GetNextStmt();
            stmt                = (stmt == nextStmt) ? stmt->GetNextStmt() : nextStmt;
        }
        optAssertionPropagatedCurrentStmt = false; // clear it back as we are done with stmts.
    }

#ifdef DEBUG
    fgDebugCheckBBlist();
    fgDebugCheckLinks();
#endif
}
