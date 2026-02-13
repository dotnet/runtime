// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        IntegralRange                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "rangecheck.h"
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

        case GT_AND:
        {
            IntegralRange leftRange  = IntegralRange::ForNode(node->gtGetOp1(), compiler);
            IntegralRange rightRange = IntegralRange::ForNode(node->gtGetOp2(), compiler);
            if (leftRange.IsNonNegative() && rightRange.IsNonNegative())
            {
                // If both sides are known to be non-negative, the result is non-negative.
                // Further, the top end of the range cannot exceed the min of the two upper bounds.
                return {SymbolicIntegerValue::Zero, min(leftRange.GetUpperBound(), rightRange.GetUpperBound())};
            }

            if (leftRange.IsNonNegative() || rightRange.IsNonNegative())
            {
                // If only one side is known to be non-negative, however it is harder to
                // reason about the upper bound.
                return {SymbolicIntegerValue::Zero, UpperBoundForType(rangeType)};
            }

            break;
        }

        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
            return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::ArrayLenMax};

        case GT_CALL:
            if (node->AsCall()->NormalizesSmallTypesOnReturn())
            {
                rangeType = static_cast<var_types>(node->AsCall()->gtReturnType);
            }
            break;

        case GT_IND:
        {
            GenTree* const addr = node->AsIndir()->Addr();

            if (node->TypeIs(TYP_INT) && addr->OperIs(GT_ADD) && addr->gtGetOp1()->OperIs(GT_LCL_VAR) &&
                addr->gtGetOp2()->IsIntegralConst(OFFSETOF__CORINFO_Span__length))
            {
                GenTreeLclVar* const lclVar = addr->gtGetOp1()->AsLclVar();

                if (compiler->lvaGetDesc(lclVar->GetLclNum())->IsSpan())
                {
                    assert(compiler->lvaIsImplicitByRefLocal(lclVar->GetLclNum()));
                    return {SymbolicIntegerValue::Zero, UpperBoundForType(rangeType)};
                }
            }
            break;
        }

        case GT_LCL_FLD:
        {
            GenTreeLclFld* const lclFld = node->AsLclFld();
            LclVarDsc* const     varDsc = compiler->lvaGetDesc(lclFld);

            if (node->TypeIs(TYP_INT) && varDsc->IsSpan() && lclFld->GetLclOffs() == OFFSETOF__CORINFO_Span__length)
            {
                return {SymbolicIntegerValue::Zero, UpperBoundForType(rangeType)};
            }

            break;
        }

        case GT_LCL_VAR:
        {
            LclVarDsc* const varDsc = compiler->lvaGetDesc(node->AsLclVar());

            if (varDsc->lvNormalizeOnStore())
            {
                rangeType = compiler->lvaGetDesc(node->AsLclVar())->TypeGet();
            }

            if (varDsc->IsNeverNegative())
            {
                return {SymbolicIntegerValue::Zero, UpperBoundForType(rangeType)};
            }
            break;
        }

        case GT_CNS_INT:
        {
            if (node->IsIntegralConst(0) || node->IsIntegralConst(1))
            {
                return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::One};
            }

            int64_t constValue = node->AsIntCon()->IntegralValue();
            if (constValue >= 0)
            {
                return {SymbolicIntegerValue::Zero, UpperBoundForType(rangeType)};
            }

            break;
        }

        case GT_QMARK:
            return Union(ForNode(node->AsQmark()->ThenNode(), compiler),
                         ForNode(node->AsQmark()->ElseNode(), compiler));

        case GT_CAST:
            return ForCastOutput(node->AsCast(), compiler);

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            switch (node->AsHWIntrinsic()->GetHWIntrinsicId())
            {
#if defined(TARGET_XARCH)
                case NI_Vector128_op_Equality:
                case NI_Vector128_op_Inequality:
                case NI_Vector256_op_Equality:
                case NI_Vector256_op_Inequality:
                case NI_Vector512_op_Equality:
                case NI_Vector512_op_Inequality:
                case NI_X86Base_CompareScalarOrderedEqual:
                case NI_X86Base_CompareScalarOrderedNotEqual:
                case NI_X86Base_CompareScalarOrderedLessThan:
                case NI_X86Base_CompareScalarOrderedLessThanOrEqual:
                case NI_X86Base_CompareScalarOrderedGreaterThan:
                case NI_X86Base_CompareScalarOrderedGreaterThanOrEqual:
                case NI_X86Base_CompareScalarUnorderedEqual:
                case NI_X86Base_CompareScalarUnorderedNotEqual:
                case NI_X86Base_CompareScalarUnorderedLessThanOrEqual:
                case NI_X86Base_CompareScalarUnorderedLessThan:
                case NI_X86Base_CompareScalarUnorderedGreaterThanOrEqual:
                case NI_X86Base_CompareScalarUnorderedGreaterThan:
                case NI_X86Base_TestC:
                case NI_X86Base_TestZ:
                case NI_X86Base_TestNotZAndNotC:
                case NI_AVX_TestC:
                case NI_AVX_TestZ:
                case NI_AVX_TestNotZAndNotC:
                    return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::One};

                case NI_X86Base_Extract:
                case NI_X86Base_X64_Extract:
                case NI_Vector128_ToScalar:
                case NI_Vector256_ToScalar:
                case NI_Vector512_ToScalar:
                case NI_Vector128_GetElement:
                case NI_Vector256_GetElement:
                case NI_Vector512_GetElement:
                    if (varTypeIsSmall(node->AsHWIntrinsic()->GetSimdBaseType()))
                    {
                        return ForType(node->AsHWIntrinsic()->GetSimdBaseType());
                    }
                    break;

                case NI_AVX2_LeadingZeroCount:
                case NI_AVX2_TrailingZeroCount:
                case NI_AVX2_X64_LeadingZeroCount:
                case NI_AVX2_X64_TrailingZeroCount:
                case NI_X86Base_PopCount:
                case NI_X86Base_X64_PopCount:
                    // Note: No advantage in using a precise range for IntegralRange.
                    // Example: IntCns = 42 gives [0..127] with a non -precise range, [42,42] with a precise range.
                    return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::ByteMax};
#elif defined(TARGET_ARM64)
                case NI_Vector64_op_Equality:
                case NI_Vector64_op_Inequality:
                case NI_Vector128_op_Equality:
                case NI_Vector128_op_Inequality:
                    return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::One};

                case NI_AdvSimd_Extract:
                case NI_Vector64_ToScalar:
                case NI_Vector128_ToScalar:
                case NI_Vector64_GetElement:
                case NI_Vector128_GetElement:
                    if (varTypeIsSmall(node->AsHWIntrinsic()->GetSimdBaseType()))
                    {
                        return ForType(node->AsHWIntrinsic()->GetSimdBaseType());
                    }
                    break;

                case NI_AdvSimd_PopCount:
                case NI_AdvSimd_LeadingZeroCount:
                case NI_AdvSimd_LeadingSignCount:
                case NI_ArmBase_LeadingZeroCount:
                case NI_ArmBase_Arm64_LeadingZeroCount:
                case NI_ArmBase_Arm64_LeadingSignCount:
                    // Note: No advantage in using a precise range for IntegralRange.
                    // Example: IntCns = 42 gives [0..127] with a non -precise range, [42,42] with a precise range.
                    return {SymbolicIntegerValue::Zero, SymbolicIntegerValue::ByteMax};
#else
#error Unsupported platform
#endif
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
//   cast     - the cast node for which the range will be computed
//   compiler - Compiler object
//
// Return Value:
//   The range this cast produces - see description.
//
/* static */ IntegralRange IntegralRange::ForCastOutput(GenTreeCast* cast, Compiler* compiler)
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

    // if we're upcasting and the cast op is a known non-negative - consider
    // this cast unsigned
    if (!fromUnsigned && (genTypeSize(toType) >= genTypeSize(fromType)))
    {
        fromUnsigned = cast->CastOp()->IsNeverNegative(compiler);
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

/* static */ IntegralRange IntegralRange::Union(IntegralRange range1, IntegralRange range2)
{
    return IntegralRange(min(range1.GetLowerBound(), range2.GetLowerBound()),
                         max(range1.GetUpperBound(), range2.GetUpperBound()));
}

#ifdef DEBUG
/* static */ void IntegralRange::Print(IntegralRange range)
{
    printf("[%lld", SymbolicToRealValue(range.m_lowerBound));
    printf("..");
    printf("%lld]", SymbolicToRealValue(range.m_upperBound));
}
#endif // DEBUG

