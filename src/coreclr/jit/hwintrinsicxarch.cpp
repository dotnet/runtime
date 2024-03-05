// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// X64VersionOfIsa: Gets the corresponding 64-bit only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The 64-bit only InstructionSet associated with isa
static CORINFO_InstructionSet X64VersionOfIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_X86Base:
            return InstructionSet_X86Base_X64;
        case InstructionSet_SSE:
            return InstructionSet_SSE_X64;
        case InstructionSet_SSE2:
            return InstructionSet_SSE2_X64;
        case InstructionSet_SSE3:
            return InstructionSet_SSE3_X64;
        case InstructionSet_SSSE3:
            return InstructionSet_SSSE3_X64;
        case InstructionSet_SSE41:
            return InstructionSet_SSE41_X64;
        case InstructionSet_SSE42:
            return InstructionSet_SSE42_X64;
        case InstructionSet_AVX:
            return InstructionSet_AVX_X64;
        case InstructionSet_AVX2:
            return InstructionSet_AVX2_X64;
        case InstructionSet_AVX512BW:
            return InstructionSet_AVX512BW_X64;
        case InstructionSet_AVX512BW_VL:
            return InstructionSet_AVX512BW_VL_X64;
        case InstructionSet_AVX512CD:
            return InstructionSet_AVX512CD_X64;
        case InstructionSet_AVX512CD_VL:
            return InstructionSet_AVX512CD_VL_X64;
        case InstructionSet_AVX512DQ:
            return InstructionSet_AVX512DQ_X64;
        case InstructionSet_AVX512DQ_VL:
            return InstructionSet_AVX512DQ_VL_X64;
        case InstructionSet_AVX512F:
            return InstructionSet_AVX512F_X64;
        case InstructionSet_AVX512F_VL:
            return InstructionSet_AVX512F_VL_X64;
        case InstructionSet_AVX512VBMI:
            return InstructionSet_AVX512VBMI_X64;
        case InstructionSet_AVX512VBMI_VL:
            return InstructionSet_AVX512VBMI_VL_X64;
        case InstructionSet_AVXVNNI:
            return InstructionSet_AVXVNNI_X64;
        case InstructionSet_AES:
            return InstructionSet_AES_X64;
        case InstructionSet_BMI1:
            return InstructionSet_BMI1_X64;
        case InstructionSet_BMI2:
            return InstructionSet_BMI2_X64;
        case InstructionSet_FMA:
            return InstructionSet_FMA_X64;
        case InstructionSet_LZCNT:
            return InstructionSet_LZCNT_X64;
        case InstructionSet_PCLMULQDQ:
            return InstructionSet_PCLMULQDQ_X64;
        case InstructionSet_POPCNT:
            return InstructionSet_POPCNT_X64;
        case InstructionSet_X86Serialize:
            return InstructionSet_X86Serialize_X64;
        default:
            return InstructionSet_NONE;
    }
}

//------------------------------------------------------------------------
// VLVersionOfIsa: Gets the corresponding AVX512VL only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The AVX512VL only InstructionSet associated with isa
static CORINFO_InstructionSet VLVersionOfIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_AVX512BW:
            return InstructionSet_AVX512BW_VL;
        case InstructionSet_AVX512CD:
            return InstructionSet_AVX512CD_VL;
        case InstructionSet_AVX512DQ:
            return InstructionSet_AVX512DQ_VL;
        case InstructionSet_AVX512F:
            return InstructionSet_AVX512F_VL;
        case InstructionSet_AVX512VBMI:
            return InstructionSet_AVX512VBMI_VL;
        default:
            return InstructionSet_NONE;
    }
}

//------------------------------------------------------------------------
// lookupInstructionSet: Gets the InstructionSet for a given class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//
// Return Value:
//    The InstructionSet associated with className
static CORINFO_InstructionSet lookupInstructionSet(const char* className)
{
    assert(className != nullptr);
    if (className[0] == 'A')
    {
        if (strcmp(className, "Aes") == 0)
        {
            return InstructionSet_AES;
        }
        if (strcmp(className, "Avx") == 0)
        {
            return InstructionSet_AVX;
        }
        if (strcmp(className, "Avx2") == 0)
        {
            return InstructionSet_AVX2;
        }
        if (strcmp(className, "Avx512BW") == 0)
        {
            return InstructionSet_AVX512BW;
        }
        if (strcmp(className, "Avx512CD") == 0)
        {
            return InstructionSet_AVX512CD;
        }
        if (strcmp(className, "Avx512DQ") == 0)
        {
            return InstructionSet_AVX512DQ;
        }
        if (strcmp(className, "Avx512F") == 0)
        {
            return InstructionSet_AVX512F;
        }
        if (strcmp(className, "Avx512Vbmi") == 0)
        {
            return InstructionSet_AVX512VBMI;
        }
        if (strcmp(className, "AvxVnni") == 0)
        {
            return InstructionSet_AVXVNNI;
        }
    }
    else if (className[0] == 'S')
    {
        if (strcmp(className, "Sse") == 0)
        {
            return InstructionSet_SSE;
        }
        if (strcmp(className, "Sse2") == 0)
        {
            return InstructionSet_SSE2;
        }
        if (strcmp(className, "Sse3") == 0)
        {
            return InstructionSet_SSE3;
        }
        if (strcmp(className, "Ssse3") == 0)
        {
            return InstructionSet_SSSE3;
        }
        if (strcmp(className, "Sse41") == 0)
        {
            return InstructionSet_SSE41;
        }
        if (strcmp(className, "Sse42") == 0)
        {
            return InstructionSet_SSE42;
        }
    }
    else if (className[0] == 'B')
    {
        if (strcmp(className, "Bmi1") == 0)
        {
            return InstructionSet_BMI1;
        }
        if (strcmp(className, "Bmi2") == 0)
        {
            return InstructionSet_BMI2;
        }
    }
    else if (className[0] == 'P')
    {
        if (strcmp(className, "Pclmulqdq") == 0)
        {
            return InstructionSet_PCLMULQDQ;
        }
        if (strcmp(className, "Popcnt") == 0)
        {
            return InstructionSet_POPCNT;
        }
    }
    else if (className[0] == 'V')
    {
        if (strncmp(className, "Vector128", 9) == 0)
        {
            return InstructionSet_Vector128;
        }
        else if (strncmp(className, "Vector256", 9) == 0)
        {
            return InstructionSet_Vector256;
        }
        else if (strncmp(className, "Vector512", 9) == 0)
        {
            return InstructionSet_Vector512;
        }
        else if (strcmp(className, "VL") == 0)
        {
            assert(!"VL.X64 support doesn't exist in the managed libraries and so is not yet implemented");
            return InstructionSet_ILLEGAL;
        }
    }
    else if (strcmp(className, "Fma") == 0)
    {
        return InstructionSet_FMA;
    }
    else if (strcmp(className, "Lzcnt") == 0)
    {
        return InstructionSet_LZCNT;
    }
    else if (strcmp(className, "X86Base") == 0)
    {
        return InstructionSet_X86Base;
    }
    else if (strcmp(className, "X86Serialize") == 0)
    {
        return InstructionSet_X86Serialize;
    }

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclosing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    enclosingClassName -- The name of the enclosing class of X64 classes
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
CORINFO_InstructionSet HWIntrinsicInfo::lookupIsa(const char* className, const char* enclosingClassName)
{
    assert(className != nullptr);

    if (strcmp(className, "X64") == 0)
    {
        assert(enclosingClassName != nullptr);
        return X64VersionOfIsa(lookupInstructionSet(enclosingClassName));
    }
    else if (strcmp(className, "VL") == 0)
    {
        assert(enclosingClassName != nullptr);
        return VLVersionOfIsa(lookupInstructionSet(enclosingClassName));
    }
    else
    {
        return lookupInstructionSet(className);
    }
}

//------------------------------------------------------------------------
// lookupImmUpperBound: Gets the upper bound for the imm-value of a given NamedIntrinsic
//
// Arguments:
//    id -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//
// Return Value:
//     The upper bound for the imm-value of the intrinsic associated with id
//
int HWIntrinsicInfo::lookupImmUpperBound(NamedIntrinsic id)
{
    if (HWIntrinsicInfo::IsEmbRoundingCompatible(id))
    {
        // The only case this branch should be hit is that JIT is generating a jump table fallback when the
        // FloatRoundingMode is not a compile-time constant.
        // Although the expected FloatRoundingMode values are 8, 9, 10, 11, but in the generated jump table, results for
        // entries within [0, 11] are all calculated,
        // Any unexpected value, say [0, 7] should be blocked by the managed code.
        return 11;
    }

    assert(HWIntrinsicInfo::lookupCategory(id) == HW_Category_IMM);

    switch (id)
    {
        case NI_AVX_Compare:
        case NI_AVX_CompareScalar:
        {
            assert(!HWIntrinsicInfo::HasFullRangeImm(id));
            return 31; // enum FloatComparisonMode has 32 values
        }

        case NI_AVX2_GatherVector128:
        case NI_AVX2_GatherVector256:
        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
        {
            assert(!HWIntrinsicInfo::HasFullRangeImm(id));
            return 8;
        }

        case NI_AVX512F_GetMantissa:
        case NI_AVX512F_GetMantissaScalar:
        case NI_AVX512F_VL_GetMantissa:
        case NI_AVX512DQ_Range:
        case NI_AVX512DQ_RangeScalar:
        case NI_AVX512DQ_VL_Range:
        {
            assert(!HWIntrinsicInfo::HasFullRangeImm(id));
            return 15;
        }

        default:
        {
            assert(HWIntrinsicInfo::HasFullRangeImm(id));
            return 255;
        }
    }
}

//------------------------------------------------------------------------
// isAVX2GatherIntrinsic: Check if the intrinsic is AVX Gather*
//
// Arguments:
//    id   -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//
// Return Value:
//     true if id is AVX Gather* intrinsic
//
bool HWIntrinsicInfo::isAVX2GatherIntrinsic(NamedIntrinsic id)
{
    switch (id)
    {
        case NI_AVX2_GatherVector128:
        case NI_AVX2_GatherVector256:
        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
            return true;
        default:
            return false;
    }
}

//------------------------------------------------------------------------
// lookupFloatComparisonModeForSwappedArgs: Get the floating-point comparison
//      mode to use when the operands are swapped.
//
// Arguments:
//    comparison -- The comparison mode used for (op1, op2)
//
// Return Value:
//     The comparison mode to use for (op2, op1)
//
FloatComparisonMode HWIntrinsicInfo::lookupFloatComparisonModeForSwappedArgs(FloatComparisonMode comparison)
{
    switch (comparison)
    {
        // These comparison modes are the same even if the operands are swapped

        case FloatComparisonMode::OrderedEqualNonSignaling:
            return FloatComparisonMode::OrderedEqualNonSignaling;
        case FloatComparisonMode::UnorderedNonSignaling:
            return FloatComparisonMode::UnorderedNonSignaling;
        case FloatComparisonMode::UnorderedNotEqualNonSignaling:
            return FloatComparisonMode::UnorderedNotEqualNonSignaling;
        case FloatComparisonMode::OrderedNonSignaling:
            return FloatComparisonMode::OrderedNonSignaling;
        case FloatComparisonMode::UnorderedEqualNonSignaling:
            return FloatComparisonMode::UnorderedEqualNonSignaling;
        case FloatComparisonMode::OrderedFalseNonSignaling:
            return FloatComparisonMode::OrderedFalseNonSignaling;
        case FloatComparisonMode::OrderedNotEqualNonSignaling:
            return FloatComparisonMode::OrderedNotEqualNonSignaling;
        case FloatComparisonMode::UnorderedTrueNonSignaling:
            return FloatComparisonMode::UnorderedTrueNonSignaling;
        case FloatComparisonMode::OrderedEqualSignaling:
            return FloatComparisonMode::OrderedEqualSignaling;
        case FloatComparisonMode::UnorderedSignaling:
            return FloatComparisonMode::UnorderedSignaling;
        case FloatComparisonMode::UnorderedNotEqualSignaling:
            return FloatComparisonMode::UnorderedNotEqualSignaling;
        case FloatComparisonMode::OrderedSignaling:
            return FloatComparisonMode::OrderedSignaling;
        case FloatComparisonMode::UnorderedEqualSignaling:
            return FloatComparisonMode::UnorderedEqualSignaling;
        case FloatComparisonMode::OrderedFalseSignaling:
            return FloatComparisonMode::OrderedFalseSignaling;
        case FloatComparisonMode::OrderedNotEqualSignaling:
            return FloatComparisonMode::OrderedNotEqualSignaling;
        case FloatComparisonMode::UnorderedTrueSignaling:
            return FloatComparisonMode::UnorderedTrueSignaling;

        // These comparison modes need a different mode if the operands are swapped

        case FloatComparisonMode::OrderedLessThanSignaling:
            return FloatComparisonMode::OrderedGreaterThanSignaling;
        case FloatComparisonMode::OrderedLessThanOrEqualSignaling:
            return FloatComparisonMode::OrderedGreaterThanOrEqualSignaling;
        case FloatComparisonMode::UnorderedNotLessThanSignaling:
            return FloatComparisonMode::UnorderedNotGreaterThanSignaling;
        case FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling:
            return FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling;
        case FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling:
            return FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling;
        case FloatComparisonMode::UnorderedNotGreaterThanSignaling:
            return FloatComparisonMode::UnorderedNotLessThanSignaling;
        case FloatComparisonMode::OrderedGreaterThanOrEqualSignaling:
            return FloatComparisonMode::OrderedLessThanOrEqualSignaling;
        case FloatComparisonMode::OrderedGreaterThanSignaling:
            return FloatComparisonMode::OrderedLessThanSignaling;
        case FloatComparisonMode::OrderedLessThanNonSignaling:
            return FloatComparisonMode::OrderedGreaterThanNonSignaling;
        case FloatComparisonMode::OrderedLessThanOrEqualNonSignaling:
            return FloatComparisonMode::OrderedGreaterThanOrEqualNonSignaling;
        case FloatComparisonMode::UnorderedNotLessThanNonSignaling:
            return FloatComparisonMode::UnorderedNotGreaterThanNonSignaling;
        case FloatComparisonMode::UnorderedNotLessThanOrEqualNonSignaling:
            return FloatComparisonMode::UnorderedNotGreaterThanOrEqualNonSignaling;
        case FloatComparisonMode::UnorderedNotGreaterThanOrEqualNonSignaling:
            return FloatComparisonMode::UnorderedNotLessThanOrEqualNonSignaling;
        case FloatComparisonMode::UnorderedNotGreaterThanNonSignaling:
            return FloatComparisonMode::UnorderedNotLessThanNonSignaling;
        case FloatComparisonMode::OrderedGreaterThanOrEqualNonSignaling:
            return FloatComparisonMode::OrderedLessThanOrEqualNonSignaling;
        case FloatComparisonMode::OrderedGreaterThanNonSignaling:
            return FloatComparisonMode::OrderedLessThanNonSignaling;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// isFullyImplementedIsa: Gets a value that indicates whether the InstructionSet is fully implemented
//
// Arguments:
//    isa - The InstructionSet to check
//
// Return Value:
//    true if isa is supported; otherwise, false
bool HWIntrinsicInfo::isFullyImplementedIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        // These ISAs are fully implemented
        case InstructionSet_AES:
        case InstructionSet_AES_X64:
        case InstructionSet_AVX:
        case InstructionSet_AVX_X64:
        case InstructionSet_AVX2:
        case InstructionSet_AVX2_X64:
        case InstructionSet_AVX512F:
        case InstructionSet_AVX512F_VL:
        case InstructionSet_AVX512F_VL_X64:
        case InstructionSet_AVX512F_X64:
        case InstructionSet_AVX512BW:
        case InstructionSet_AVX512BW_VL:
        case InstructionSet_AVX512BW_VL_X64:
        case InstructionSet_AVX512BW_X64:
        case InstructionSet_AVX512CD:
        case InstructionSet_AVX512CD_VL:
        case InstructionSet_AVX512CD_VL_X64:
        case InstructionSet_AVX512CD_X64:
        case InstructionSet_AVX512DQ:
        case InstructionSet_AVX512DQ_VL:
        case InstructionSet_AVX512DQ_VL_X64:
        case InstructionSet_AVX512DQ_X64:
        case InstructionSet_AVX512VBMI:
        case InstructionSet_AVX512VBMI_VL:
        case InstructionSet_AVX512VBMI_VL_X64:
        case InstructionSet_AVX512VBMI_X64:
        case InstructionSet_AVXVNNI:
        case InstructionSet_AVXVNNI_X64:
        case InstructionSet_BMI1:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
        case InstructionSet_FMA:
        case InstructionSet_FMA_X64:
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
        case InstructionSet_PCLMULQDQ:
        case InstructionSet_PCLMULQDQ_X64:
        case InstructionSet_POPCNT:
        case InstructionSet_POPCNT_X64:
        case InstructionSet_SSE:
        case InstructionSet_SSE_X64:
        case InstructionSet_SSE2:
        case InstructionSet_SSE2_X64:
        case InstructionSet_SSE3:
        case InstructionSet_SSE3_X64:
        case InstructionSet_SSSE3:
        case InstructionSet_SSSE3_X64:
        case InstructionSet_SSE41:
        case InstructionSet_SSE41_X64:
        case InstructionSet_SSE42:
        case InstructionSet_SSE42_X64:
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
        case InstructionSet_Vector512:
        case InstructionSet_X86Base:
        case InstructionSet_X86Base_X64:
        case InstructionSet_X86Serialize:
        case InstructionSet_X86Serialize_X64:
        {
            return true;
        }

        default:
        {
            return false;
        }
    }
}

//------------------------------------------------------------------------
// isScalarIsa: Gets a value that indicates whether the InstructionSet is scalar
//
// Arguments:
//    isa - The InstructionSet to check
//
// Return Value:
//    true if isa is scalar; otherwise, false
bool HWIntrinsicInfo::isScalarIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_BMI1:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
        case InstructionSet_X86Base:
        case InstructionSet_X86Base_X64:
        {
            // InstructionSet_POPCNT and InstructionSet_POPCNT_X64 are excluded
            // even though they are "scalar" ISA because they depend on SSE4.2
            // and Popcnt.IsSupported implies Sse42.IsSupported
            return true;
        }

        default:
        {
            return false;
        }
    }
}

//------------------------------------------------------------------------
// lookupIval: Gets a the implicit immediate value for the given intrinsic
//
// Arguments:
//    comp         - The compiler
//    id           - The intrinsic for which to get the ival
//    simdBaseType - The base type for the intrinsic
//
// Return Value:
//    The immediate value for the given intrinsic or -1 if none exists
int HWIntrinsicInfo::lookupIval(Compiler* comp, NamedIntrinsic id, var_types simdBaseType)
{
    switch (id)
    {
        case NI_SSE_CompareEqual:
        case NI_SSE_CompareScalarEqual:
        case NI_SSE2_CompareEqual:
        case NI_SSE2_CompareScalarEqual:
        case NI_AVX_CompareEqual:
        case NI_AVX512F_CompareEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::OrderedEqualNonSignaling);
            }
            else
            {
                // We can emit `vpcmpeqb`, `vpcmpeqw`, `vpcmpeqd`, or `vpcmpeqq`
            }
            break;
        }

        case NI_SSE_CompareGreaterThan:
        case NI_SSE_CompareScalarGreaterThan:
        case NI_SSE2_CompareGreaterThan:
        case NI_SSE2_CompareScalarGreaterThan:
        case NI_AVX_CompareGreaterThan:
        case NI_AVX512F_CompareGreaterThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    return static_cast<int>(FloatComparisonMode::OrderedGreaterThanSignaling);
                }

                // CompareGreaterThan is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareGreaterThan);
                return static_cast<int>(FloatComparisonMode::OrderedLessThanSignaling);
            }
            else if ((id == NI_AVX512F_CompareGreaterThanMask) && varTypeIsUnsigned(simdBaseType))
            {
                // TODO-XARCH-CQ: Allow the other integer paths to use the EVEX encoding
                return static_cast<int>(IntComparisonMode::GreaterThan);
            }
            break;
        }

        case NI_SSE_CompareLessThan:
        case NI_SSE_CompareScalarLessThan:
        case NI_SSE2_CompareLessThan:
        case NI_SSE2_CompareScalarLessThan:
        case NI_AVX_CompareLessThan:
        case NI_AVX512F_CompareLessThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanSignaling);
            }
            else if (id == NI_AVX512F_CompareLessThanMask)
            {
                // TODO-XARCH-CQ: Allow the other integer paths to use the EVEX encoding
                return static_cast<int>(IntComparisonMode::LessThan);
            }
            break;
        }

        case NI_SSE_CompareGreaterThanOrEqual:
        case NI_SSE_CompareScalarGreaterThanOrEqual:
        case NI_SSE2_CompareGreaterThanOrEqual:
        case NI_SSE2_CompareScalarGreaterThanOrEqual:
        case NI_AVX_CompareGreaterThanOrEqual:
        case NI_AVX512F_CompareGreaterThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    return static_cast<int>(FloatComparisonMode::OrderedGreaterThanOrEqualSignaling);
                }

                // CompareGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareGreaterThanOrEqual);
                return static_cast<int>(FloatComparisonMode::OrderedLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareGreaterThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::GreaterThanOrEqual);
            }
            break;
        }

        case NI_SSE_CompareLessThanOrEqual:
        case NI_SSE_CompareScalarLessThanOrEqual:
        case NI_SSE2_CompareLessThanOrEqual:
        case NI_SSE2_CompareScalarLessThanOrEqual:
        case NI_AVX_CompareLessThanOrEqual:
        case NI_AVX512F_CompareLessThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareLessThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::LessThanOrEqual);
            }
            break;
        }

        case NI_SSE_CompareNotEqual:
        case NI_SSE_CompareScalarNotEqual:
        case NI_SSE2_CompareNotEqual:
        case NI_SSE2_CompareScalarNotEqual:
        case NI_AVX_CompareNotEqual:
        case NI_AVX512F_CompareNotEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotEqualNonSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareNotEqualMask);
                return static_cast<int>(IntComparisonMode::NotEqual);
            }
            break;
        }

        case NI_SSE_CompareNotGreaterThan:
        case NI_SSE_CompareScalarNotGreaterThan:
        case NI_SSE2_CompareNotGreaterThan:
        case NI_SSE2_CompareScalarNotGreaterThan:
        case NI_AVX_CompareNotGreaterThan:
        case NI_AVX512F_CompareNotGreaterThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanSignaling);
                }

                // CompareNotGreaterThan is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareGreaterThan);
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareNotGreaterThanMask);
                return static_cast<int>(IntComparisonMode::LessThanOrEqual);
            }
            break;
        }

        case NI_SSE_CompareNotLessThan:
        case NI_SSE_CompareScalarNotLessThan:
        case NI_SSE2_CompareNotLessThan:
        case NI_SSE2_CompareScalarNotLessThan:
        case NI_AVX_CompareNotLessThan:
        case NI_AVX512F_CompareNotLessThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareNotLessThanMask);
                return static_cast<int>(IntComparisonMode::GreaterThanOrEqual);
            }
            break;
        }

        case NI_SSE_CompareNotGreaterThanOrEqual:
        case NI_SSE_CompareScalarNotGreaterThanOrEqual:
        case NI_SSE2_CompareNotGreaterThanOrEqual:
        case NI_SSE2_CompareScalarNotGreaterThanOrEqual:
        case NI_AVX_CompareNotGreaterThanOrEqual:
        case NI_AVX512F_CompareNotGreaterThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling);
                }

                // CompareNotGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // We will return the inverted case here and lowering will itself swap the ops
                // to ensure the emitted code remains correct. This simplifies the overall logic
                // here and for other use cases.

                assert(id != NI_AVX_CompareNotGreaterThanOrEqual);
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareNotGreaterThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::LessThan);
            }
            break;
        }

        case NI_SSE_CompareNotLessThanOrEqual:
        case NI_SSE_CompareScalarNotLessThanOrEqual:
        case NI_SSE2_CompareNotLessThanOrEqual:
        case NI_SSE2_CompareScalarNotLessThanOrEqual:
        case NI_AVX_CompareNotLessThanOrEqual:
        case NI_AVX512F_CompareNotLessThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512F_CompareNotLessThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::GreaterThan);
            }
            break;
        }

        case NI_SSE_CompareOrdered:
        case NI_SSE_CompareScalarOrdered:
        case NI_SSE2_CompareOrdered:
        case NI_SSE2_CompareScalarOrdered:
        case NI_AVX_CompareOrdered:
        case NI_AVX512F_CompareOrderedMask:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatComparisonMode::OrderedNonSignaling);
        }

        case NI_SSE_CompareUnordered:
        case NI_SSE_CompareScalarUnordered:
        case NI_SSE2_CompareUnordered:
        case NI_SSE2_CompareScalarUnordered:
        case NI_AVX_CompareUnordered:
        case NI_AVX512F_CompareUnorderedMask:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatComparisonMode::UnorderedNonSignaling);
        }

        case NI_SSE41_Ceiling:
        case NI_SSE41_CeilingScalar:
        case NI_AVX_Ceiling:
        {
            FALLTHROUGH;
        }

        case NI_SSE41_RoundToPositiveInfinity:
        case NI_SSE41_RoundToPositiveInfinityScalar:
        case NI_AVX_RoundToPositiveInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToPositiveInfinity);
        }

        case NI_SSE41_Floor:
        case NI_SSE41_FloorScalar:
        case NI_AVX_Floor:
        {
            FALLTHROUGH;
        }

        case NI_SSE41_RoundToNegativeInfinity:
        case NI_SSE41_RoundToNegativeInfinityScalar:
        case NI_AVX_RoundToNegativeInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNegativeInfinity);
        }

        case NI_SSE41_RoundCurrentDirection:
        case NI_SSE41_RoundCurrentDirectionScalar:
        case NI_AVX_RoundCurrentDirection:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::CurrentDirection);
        }

        case NI_SSE41_RoundToNearestInteger:
        case NI_SSE41_RoundToNearestIntegerScalar:
        case NI_AVX_RoundToNearestInteger:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNearestInteger);
        }

        case NI_SSE41_RoundToZero:
        case NI_SSE41_RoundToZeroScalar:
        case NI_AVX_RoundToZero:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToZero);
        }

        default:
        {
            break;
        }
    }

    return -1;
}

//------------------------------------------------------------------------
// impNonConstFallback: convert certain SSE2/AVX2 shift intrinsic to its semantic alternative when the imm-arg is
// not a compile-time constant
//
// Arguments:
//    intrinsic       -- intrinsic ID
//    simdType        -- Vector type
//    simdBaseJitType -- SIMD base JIT type of the Vector128/256<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, CorInfoType simdBaseJitType)
{
    assert(HWIntrinsicInfo::NoJmpTableImm(intrinsic) || HWIntrinsicInfo::MaybeNoJmpTableImm(intrinsic));
    switch (intrinsic)
    {
        case NI_SSE2_ShiftLeftLogical:
        case NI_SSE2_ShiftRightArithmetic:
        case NI_SSE2_ShiftRightLogical:
        case NI_AVX2_ShiftLeftLogical:
        case NI_AVX2_ShiftRightArithmetic:
        case NI_AVX2_ShiftRightLogical:
        case NI_AVX512F_ShiftLeftLogical:
        case NI_AVX512F_ShiftRightArithmetic:
        case NI_AVX512F_ShiftRightLogical:
        case NI_AVX512F_VL_ShiftRightArithmetic:
        case NI_AVX512BW_ShiftLeftLogical:
        case NI_AVX512BW_ShiftRightArithmetic:
        case NI_AVX512BW_ShiftRightLogical:
        {
            // These intrinsics have overloads that take op2 in a simd register and just read the lowest 8-bits

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack();

            GenTree* tmpOp = gtNewSimdCreateScalarNode(TYP_SIMD16, op2, CORINFO_TYPE_INT, 16);
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, simdBaseJitType, genTypeSize(simdType));
        }

        case NI_AVX512F_RotateLeft:
        case NI_AVX512F_RotateRight:
        case NI_AVX512F_VL_RotateLeft:
        case NI_AVX512F_VL_RotateRight:
        {
            var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

            // These intrinsics have variants that take op2 in a simd register and read a unique shift per element
            intrinsic = static_cast<NamedIntrinsic>(intrinsic + 1);

            static_assert_no_msg(NI_AVX512F_RotateLeftVariable == (NI_AVX512F_RotateLeft + 1));
            static_assert_no_msg(NI_AVX512F_RotateRightVariable == (NI_AVX512F_RotateRight + 1));
            static_assert_no_msg(NI_AVX512F_VL_RotateLeftVariable == (NI_AVX512F_VL_RotateLeft + 1));
            static_assert_no_msg(NI_AVX512F_VL_RotateRightVariable == (NI_AVX512F_VL_RotateRight + 1));

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack();

            if (varTypeIsLong(simdBaseType))
            {
                op2 = gtNewCastNode(TYP_LONG, op2, /* fromUnsigned */ true, TYP_LONG);
            }

            GenTree* tmpOp = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, genTypeSize(simdType));
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, simdBaseJitType, genTypeSize(simdType));
        }

        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// impSpecialIntrinsic: dispatch intrinsics to their own implementation
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    simdBaseJitType -- generic argument of the intrinsic.
//    retType         -- return type of the intrinsic.
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO*     sig,
                                       CorInfoType           simdBaseJitType,
                                       var_types             retType,
                                       unsigned              simdSize)
{
    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

    CORINFO_InstructionSet isa = HWIntrinsicInfo::lookupIsa(intrinsic);

    var_types simdBaseType = TYP_UNKNOWN;
    if (simdSize != 0)
    {
        simdBaseType = JitType2PreciseVarType(simdBaseJitType);
        assert(varTypeIsArithmetic(simdBaseType));
    }

    switch (intrinsic)
    {
        case NI_Vector128_Abs:
        case NI_Vector256_Abs:
        case NI_Vector512_Abs:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || varTypeIsUnsigned(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Add:
        case NI_Vector256_Add:
        case NI_Vector512_Add:
        case NI_Vector128_op_Addition:
        case NI_Vector256_op_Addition:
        case NI_Vector512_op_Addition:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_AndNot:
        case NI_Vector256_AndNot:
        case NI_Vector512_AndNot:
        {
            assert(sig->numArgs == 2);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_As:
        case NI_Vector128_AsByte:
        case NI_Vector128_AsDouble:
        case NI_Vector128_AsInt16:
        case NI_Vector128_AsInt32:
        case NI_Vector128_AsInt64:
        case NI_Vector128_AsNInt:
        case NI_Vector128_AsNUInt:
        case NI_Vector128_AsSByte:
        case NI_Vector128_AsSingle:
        case NI_Vector128_AsUInt16:
        case NI_Vector128_AsUInt32:
        case NI_Vector128_AsUInt64:
        case NI_Vector128_AsVector4:
        case NI_Vector256_As:
        case NI_Vector256_AsByte:
        case NI_Vector256_AsDouble:
        case NI_Vector256_AsInt16:
        case NI_Vector256_AsInt32:
        case NI_Vector256_AsInt64:
        case NI_Vector256_AsNInt:
        case NI_Vector256_AsNUInt:
        case NI_Vector256_AsSByte:
        case NI_Vector256_AsSingle:
        case NI_Vector256_AsUInt16:
        case NI_Vector256_AsUInt32:
        case NI_Vector256_AsUInt64:
        case NI_Vector512_As:
        case NI_Vector512_AsByte:
        case NI_Vector512_AsDouble:
        case NI_Vector512_AsInt16:
        case NI_Vector512_AsInt32:
        case NI_Vector512_AsInt64:
        case NI_Vector512_AsNInt:
        case NI_Vector512_AsNUInt:
        case NI_Vector512_AsSByte:
        case NI_Vector512_AsSingle:
        case NI_Vector512_AsUInt16:
        case NI_Vector512_AsUInt32:
        case NI_Vector512_AsUInt64:
        {
            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            assert(sig->numArgs == 1);

            retNode = impSIMDPopStack();
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

        case NI_Vector128_AsVector:
        {
            assert(sig->numArgs == 1);
            uint32_t vectorTByteLength = getVectorTByteLength();

            if (vectorTByteLength == YMM_REGSIZE_BYTES)
            {
                // Vector<T> is TYP_SIMD32, so we should treat this as a call to Vector128.ToVector256
                return impSpecialIntrinsic(NI_Vector128_ToVector256, clsHnd, method, sig, simdBaseJitType, retType,
                                           simdSize);
            }
            else if (vectorTByteLength == XMM_REGSIZE_BYTES)
            {
                // We fold away the cast here, as it only exists to satisfy
                // the type system. It is safe to do this here since the retNode type
                // and the signature return type are both the same TYP_SIMD.

                retNode = impSIMDPopStack();
                SetOpLclRelatedToSIMDIntrinsic(retNode);
                assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            }
            else
            {
                assert(vectorTByteLength == 0);
            }
            break;
        }

        case NI_Vector128_AsVector2:
        case NI_Vector128_AsVector3:
        {
            assert(sig->numArgs == 1);
            assert((simdSize == 16) && (simdBaseType == TYP_FLOAT));
            assert((retType == TYP_SIMD8) || (retType == TYP_SIMD12));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_AsVector128:
        {
            assert(sig->numArgs == 1);
            assert(retType == TYP_SIMD16);
            assert(HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic));

            CorInfoType op1SimdBaseJitType =
                getBaseJitTypeAndSizeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args), &simdSize);

            assert(simdBaseJitType == op1SimdBaseJitType);

            switch (getSIMDTypeForSize(simdSize))
            {
                case TYP_SIMD8:
                {
                    assert((simdSize == 8) && (simdBaseType == TYP_FLOAT));

                    op1 = impSIMDPopStack();

                    if (op1->IsCnsVec())
                    {
                        GenTreeVecCon* vecCon = op1->AsVecCon();
                        vecCon->gtType        = TYP_SIMD16;

                        vecCon->gtSimdVal.f32[2] = 0.0f;
                        vecCon->gtSimdVal.f32[3] = 0.0f;

                        return vecCon;
                    }

                    GenTree* idx  = gtNewIconNode(2, TYP_INT);
                    GenTree* zero = gtNewZeroConNode(TYP_FLOAT);
                    op1           = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseJitType, 16);

                    idx     = gtNewIconNode(3, TYP_INT);
                    zero    = gtNewZeroConNode(TYP_FLOAT);
                    retNode = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseJitType, 16);

                    break;
                }

                case TYP_SIMD12:
                {
                    assert((simdSize == 12) && (simdBaseType == TYP_FLOAT));

                    op1 = impSIMDPopStack();

                    if (op1->IsCnsVec())
                    {
                        GenTreeVecCon* vecCon = op1->AsVecCon();
                        vecCon->gtType        = TYP_SIMD16;

                        vecCon->gtSimdVal.f32[3] = 0.0f;
                        return vecCon;
                    }

                    GenTree* idx  = gtNewIconNode(3, TYP_INT);
                    GenTree* zero = gtNewZeroConNode(TYP_FLOAT);
                    retNode       = gtNewSimdWithElementNode(retType, op1, idx, zero, simdBaseJitType, 16);
                    break;
                }

                case TYP_SIMD16:
                {
                    // We fold away the cast here, as it only exists to satisfy
                    // the type system. It is safe to do this here since the retNode type
                    // and the signature return type are both the same TYP_SIMD.

                    retNode = impSIMDPopStack();
                    SetOpLclRelatedToSIMDIntrinsic(retNode);
                    assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                    break;
                }

                case TYP_SIMD32:
                {
                    // Vector<T> is TYP_SIMD32, so we should treat this as a call to Vector256.GetLower
                    return impSpecialIntrinsic(NI_Vector256_GetLower, clsHnd, method, sig, simdBaseJitType, retType,
                                               simdSize);
                }

                default:
                {
                    unreached();
                }
            }

            break;
        }

        case NI_Vector256_AsVector:
        case NI_Vector256_AsVector256:
        {
            assert(sig->numArgs == 1);
            uint32_t vectorTByteLength = getVectorTByteLength();

            if (vectorTByteLength == YMM_REGSIZE_BYTES)
            {
                // We fold away the cast here, as it only exists to satisfy
                // the type system. It is safe to do this here since the retNode type
                // and the signature return type are both the same TYP_SIMD.

                retNode = impSIMDPopStack();
                SetOpLclRelatedToSIMDIntrinsic(retNode);
                assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                break;
            }
            else if (vectorTByteLength == XMM_REGSIZE_BYTES)
            {
                if (compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    // We support Vector256 but Vector<T> is only 16-bytes, so we should
                    // treat this method as a call to Vector256.GetLower or Vector128.ToVector256

                    if (intrinsic == NI_Vector256_AsVector)
                    {
                        return impSpecialIntrinsic(NI_Vector256_GetLower, clsHnd, method, sig, simdBaseJitType, retType,
                                                   simdSize);
                    }
                    else
                    {
                        assert(intrinsic == NI_Vector256_AsVector256);
                        return impSpecialIntrinsic(NI_Vector128_ToVector256, clsHnd, method, sig, simdBaseJitType,
                                                   retType, 16);
                    }
                }
            }
            else
            {
                assert(vectorTByteLength == 0);
            }
            break;
        }

        case NI_Vector512_AsVector:
        case NI_Vector512_AsVector512:
        {
            assert(sig->numArgs == 1);
            uint32_t vectorTByteLength = getVectorTByteLength();

            if (vectorTByteLength == YMM_REGSIZE_BYTES)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());

                // We support Vector512 but Vector<T> is only 32-bytes, so we should
                // treat this method as a call to Vector512.GetLower or Vector256.ToVector512

                if (intrinsic == NI_Vector512_AsVector)
                {
                    return impSpecialIntrinsic(NI_Vector512_GetLower, clsHnd, method, sig, simdBaseJitType, retType,
                                               simdSize);
                }
                else
                {
                    assert(intrinsic == NI_Vector512_AsVector512);
                    return impSpecialIntrinsic(NI_Vector256_ToVector512, clsHnd, method, sig, simdBaseJitType, retType,
                                               32);
                }
                break;
            }
            else if (vectorTByteLength == XMM_REGSIZE_BYTES)
            {
                if (compOpportunisticallyDependsOn(InstructionSet_AVX512F))
                {
                    // We support Vector512 but Vector<T> is only 16-bytes, so we should
                    // treat this method as a call to Vector512.GetLower128 or Vector128.ToVector512

                    if (intrinsic == NI_Vector512_AsVector)
                    {
                        return impSpecialIntrinsic(NI_Vector512_GetLower128, clsHnd, method, sig, simdBaseJitType,
                                                   retType, simdSize);
                    }
                    else
                    {
                        assert(intrinsic == NI_Vector512_AsVector512);
                        return impSpecialIntrinsic(NI_Vector128_ToVector512, clsHnd, method, sig, simdBaseJitType,
                                                   retType, 16);
                    }
                }
            }
            else
            {
                assert(vectorTByteLength == 0);
            }
            break;
        }

        case NI_Vector128_BitwiseAnd:
        case NI_Vector256_BitwiseAnd:
        case NI_Vector512_BitwiseAnd:
        case NI_Vector128_op_BitwiseAnd:
        case NI_Vector256_op_BitwiseAnd:
        case NI_Vector512_op_BitwiseAnd:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_BitwiseOr:
        case NI_Vector256_BitwiseOr:
        case NI_Vector512_BitwiseOr:
        case NI_Vector128_op_BitwiseOr:
        case NI_Vector256_op_BitwiseOr:
        case NI_Vector512_op_BitwiseOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Ceiling:
        case NI_Vector256_Ceiling:
        case NI_Vector512_Ceiling:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // Ceiling is only supported for floating-point types on SSE4.1 or later
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ConditionalSelect:
        case NI_Vector256_ConditionalSelect:
        case NI_Vector512_ConditionalSelect:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ConvertToDouble:
        case NI_Vector256_ConvertToDouble:
        case NI_Vector128_ConvertToInt64:
        case NI_Vector256_ConvertToInt64:
        case NI_Vector128_ConvertToUInt32:
        case NI_Vector256_ConvertToUInt32:
        case NI_Vector128_ConvertToUInt64:
        case NI_Vector256_ConvertToUInt64:
        {
            assert(sig->numArgs == 1);
            // TODO-XARCH-CQ: These intrinsics should be accelerated
            break;
        }

        case NI_Vector128_ConvertToInt32:
        case NI_Vector256_ConvertToInt32:
        case NI_Vector512_ConvertToInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            switch (simdSize)
            {
                case 16:
                    intrinsic = NI_SSE2_ConvertToVector128Int32WithTruncation;
                    break;
                case 32:
                    intrinsic = NI_AVX_ConvertToVector256Int32WithTruncation;
                    break;
                case 64:
                    intrinsic = NI_AVX512F_ConvertToVector512Int32WithTruncation;
                    break;
                default:
                    unreached();
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ConvertToSingle:
        case NI_Vector256_ConvertToSingle:
        case NI_Vector512_ConvertToSingle:
        {
            assert(sig->numArgs == 1);

            if (simdBaseType == TYP_INT)
            {
                switch (simdSize)
                {
                    case 16:
                        intrinsic = NI_SSE2_ConvertToVector128Single;
                        break;
                    case 32:
                        intrinsic = NI_AVX_ConvertToVector256Single;
                        break;
                    case 64:
                        intrinsic = NI_AVX512F_ConvertToVector512Single;
                        break;
                    default:
                        unreached();
                }

                op1     = impSIMDPopStack();
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            else
            {
                // TODO-XARCH-CQ: These intrinsics should be accelerated
                assert(simdBaseType == TYP_UINT);
            }
            break;
        }

        case NI_Vector128_Create:
        case NI_Vector256_Create:
        case NI_Vector512_Create:
        {
            if (sig->numArgs == 1)
            {
#if defined(TARGET_X86)
                if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->IsIntegralConst())
                {
                    // TODO-XARCH-CQ: It may be beneficial to emit the movq
                    // instruction, which takes a 64-bit memory address and
                    // works on 32-bit x86 systems.
                    break;
                }
#endif // TARGET_X86

                op1     = impPopStack().val;
                retNode = gtNewSimdCreateBroadcastNode(retType, op1, simdBaseJitType, simdSize);
                break;
            }

            uint32_t simdLength = getSIMDVectorLength(simdSize, simdBaseType);
            assert(sig->numArgs == simdLength);

            bool isConstant = true;

            if (varTypeIsFloating(simdBaseType))
            {
                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    GenTree* arg = impStackTop(index).val;

                    if (!arg->IsCnsFltOrDbl())
                    {
                        isConstant = false;
                        break;
                    }
                }
            }
            else
            {
                assert(varTypeIsIntegral(simdBaseType));

                for (uint32_t index = 0; index < sig->numArgs; index++)
                {
                    GenTree* arg = impStackTop(index).val;

                    if (!arg->IsIntegralConst())
                    {
                        isConstant = false;
                        break;
                    }
                }
            }

            if (isConstant)
            {
                // Some of the below code assumes 16/32/64 byte SIMD types
                assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));

                GenTreeVecCon* vecCon = gtNewVconNode(retType);

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        uint8_t cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<uint8_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u8[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_SHORT:
                    case TYP_USHORT:
                    {
                        uint16_t cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<uint16_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u16[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_INT:
                    case TYP_UINT:
                    {
                        uint32_t cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<uint32_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u32[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_LONG:
                    case TYP_ULONG:
                    {
                        uint64_t cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<uint64_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimdVal.u64[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_FLOAT:
                    {
                        float cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<float>(impPopStack().val->AsDblCon()->DconValue());
                            vecCon->gtSimdVal.f32[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    case TYP_DOUBLE:
                    {
                        double cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            double cnsVal = static_cast<double>(impPopStack().val->AsDblCon()->DconValue());
                            vecCon->gtSimdVal.f64[simdLength - 1 - index] = cnsVal;
                        }
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                retNode = vecCon;
                break;
            }

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), sig->numArgs);

            // TODO-CQ: We don't handle contiguous args for anything except TYP_FLOAT today

            GenTree* prevArg           = nullptr;
            bool     areArgsContiguous = (simdBaseType == TYP_FLOAT);

            for (int i = sig->numArgs - 1; i >= 0; i--)
            {
                GenTree* arg = impPopStack().val;

                if (areArgsContiguous)
                {
                    if (prevArg != nullptr)
                    {
                        // Recall that we are popping the args off the stack in reverse order.
                        areArgsContiguous = areArgumentsContiguous(arg, prevArg);
                    }

                    prevArg = arg;
                }

                nodeBuilder.AddOperand(i, arg);
            }

            if (areArgsContiguous)
            {
                op1                 = nodeBuilder.GetOperand(0);
                GenTree* op1Address = CreateAddressNodeForSimdHWIntrinsicCreate(op1, simdBaseType, simdSize);
                retNode             = gtNewIndir(retType, op1Address);
            }
            else
            {
                retNode =
                    gtNewSimdHWIntrinsicNode(retType, std::move(nodeBuilder), intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_CreateScalar:
        case NI_Vector256_CreateScalar:
        case NI_Vector512_CreateScalar:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->IsIntegralConst())
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        case NI_Vector512_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->IsIntegralConst())
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarUnsafeNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_CreateSequence:
        case NI_Vector256_CreateSequence:
        case NI_Vector512_CreateSequence:
        {
            assert(sig->numArgs == 2);

            if (!impStackTop(1).val->OperIsConst() || !impStackTop(0).val->OperIsConst())
            {
                // One of the operands isn't constant, so we need to do a computation in the form of:
                //     (Indices * op2) + op1

                if (simdSize == 32)
                {
                    if (varTypeIsIntegral(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
                    {
                        // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                        break;
                    }
                }

                if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->OperIsConst())
                {
                    // When op2 is a constant, we can skip the multiplication allowing us to always
                    // generate better code. However, if it isn't then we need to fallback in the
                    // cases where multiplication isn't supported.

                    if ((simdSize != 64) && !compOpportunisticallyDependsOn(InstructionSet_AVX512DQ_VL))
                    {
                        // TODO-XARCH-CQ: We should support long/ulong multiplication
                        break;
                    }

#if defined(TARGET_X86)
                    // TODO-XARCH-CQ: We need to support 64-bit CreateBroadcast
                    break;
#endif // TARGET_X86
                }
            }

            impSpillSideEffect(true, verCurrentState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateSequenceNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Divide:
        case NI_Vector256_Divide:
        case NI_Vector512_Divide:
        case NI_Vector128_op_Division:
        case NI_Vector256_op_Division:
        case NI_Vector512_op_Division:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsFloating(simdBaseType))
            {
                // We can't trivially handle division for integral types using SIMD
                break;
            }

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Dot:
        case NI_Vector256_Dot:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            if (varTypeIsByte(simdBaseType) || varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: We could support dot product for 8-bit and
                // 64-bit integers if we support multiplication for the same
                break;
            }

            if (simdSize == 32)
            {
                if (!varTypeIsFloating(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                    break;
                }
            }
            else if ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    // TODO-XARCH-CQ: We can support 32-bit integers if we updating multiplication
                    // to be lowered rather than imported as the relevant operations.
                    break;
                }
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
            retNode = gtNewSimdGetElementNode(retType, retNode, gtNewIconNode(0), simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Equals:
        case NI_Vector256_Equals:
        case NI_Vector512_Equals:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_EqualsAll:
        case NI_Vector256_EqualsAll:
        case NI_Vector512_EqualsAll:
        case NI_Vector128_op_Equality:
        case NI_Vector256_op_Equality:
        case NI_Vector512_op_Equality:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_EqualsAny:
        case NI_Vector256_EqualsAny:
        case NI_Vector512_EqualsAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector512_ExtractMostSignificantBits:
        {
#if defined(TARGET_X86)
            // TODO-XARCH-CQ: It may be beneficial to decompose this operation
            break;
#endif // TARGET_X86

            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                op1 = impSIMDPopStack();
                op1 =
                    gtNewSimdHWIntrinsicNode(TYP_MASK, op1, NI_AVX512F_ConvertVectorToMask, simdBaseJitType, simdSize);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_AVX512F_MoveMask, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ExtractMostSignificantBits:
        case NI_Vector256_ExtractMostSignificantBits:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                NamedIntrinsic moveMaskIntrinsic = NI_Illegal;
                NamedIntrinsic shuffleIntrinsic  = NI_Illegal;

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        op1               = impSIMDPopStack();
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX2_MoveMask : NI_SSE2_MoveMask;
                        break;
                    }

                    case TYP_SHORT:
                    case TYP_USHORT:
                    {
                        simd_t simdVal = {};

                        assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));
                        simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

                        // We want to tightly pack the most significant byte of each short/ushort
                        // and then zero the tightly packed least significant bytes
                        //
                        // The most significant bit being set means zero the value

                        simdVal.u64[0] = 0x0F0D0B0907050301;
                        simdVal.u64[1] = 0x8080808080808080;

                        if (simdSize == 32)
                        {
                            // Vector256 works on 2x128-bit lanes, so repeat the same indices for the upper lane

                            simdVal.u64[2] = 0x0F0D0B0907050301;
                            simdVal.u64[3] = 0x8080808080808080;

                            shuffleIntrinsic  = NI_AVX2_Shuffle;
                            moveMaskIntrinsic = NI_SSE2_MoveMask;
                        }
                        else if (compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                        {
                            shuffleIntrinsic  = NI_SSSE3_Shuffle;
                            moveMaskIntrinsic = NI_SSE2_MoveMask;
                        }
                        else
                        {
                            return nullptr;
                        }

                        op2 = gtNewVconNode(simdType);
                        memcpy(&op2->AsVecCon()->gtSimdVal, &simdVal, simdSize);

                        op1 = impSIMDPopStack();
                        op1 = gtNewSimdHWIntrinsicNode(simdType, op1, op2, shuffleIntrinsic, simdBaseJitType, simdSize);

                        if (simdSize == 32)
                        {
                            CorInfoType simdOtherJitType;

                            // Since Vector256 is 2x128-bit lanes we need a full width permutation so we get the lower
                            // 64-bits of each lane next to eachother. The upper bits should be zero, but also don't
                            // matter so we can also then simplify down to a 128-bit move mask.

                            simdOtherJitType = (simdBaseType == TYP_UBYTE) ? CORINFO_TYPE_ULONG : CORINFO_TYPE_LONG;

                            op1 = gtNewSimdHWIntrinsicNode(simdType, op1, gtNewIconNode(0xD8), NI_AVX2_Permute4x64,
                                                           simdOtherJitType, simdSize);

                            simdType = TYP_SIMD16;

                            op1 = gtNewSimdGetLowerNode(simdType, op1, simdBaseJitType, simdSize);

                            simdSize = 16;
                        }
                        break;
                    }

                    case TYP_INT:
                    case TYP_UINT:
                    case TYP_FLOAT:
                    {
                        simdBaseJitType   = CORINFO_TYPE_FLOAT;
                        op1               = impSIMDPopStack();
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX_MoveMask : NI_SSE_MoveMask;
                        break;
                    }

                    case TYP_LONG:
                    case TYP_ULONG:
                    case TYP_DOUBLE:
                    {
                        simdBaseJitType   = CORINFO_TYPE_DOUBLE;
                        op1               = impSIMDPopStack();
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX_MoveMask : NI_SSE2_MoveMask;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                assert(moveMaskIntrinsic != NI_Illegal);
                assert(op1 != nullptr);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, moveMaskIntrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Floor:
        case NI_Vector256_Floor:
        case NI_Vector512_Floor:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // Floor is only supported for floating-point types on SSE4.1 or later
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_get_AllBitsSet:
        case NI_Vector256_get_AllBitsSet:
        case NI_Vector512_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewAllBitsSetConNode(retType);
            break;
        }

        case NI_Vector128_get_Indices:
        case NI_Vector256_get_Indices:
        case NI_Vector512_get_Indices:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewSimdGetIndicesNode(retType, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_get_One:
        case NI_Vector256_get_One:
        case NI_Vector512_get_One:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewOneConNode(retType, simdBaseType);
            break;
        }

        case NI_Vector128_get_Zero:
        case NI_Vector256_get_Zero:
        case NI_Vector512_get_Zero:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewZeroConNode(retType);
            break;
        }

        case NI_Vector128_GetElement:
        case NI_Vector256_GetElement:
        case NI_Vector512_GetElement:
        {
            assert(sig->numArgs == 2);

            op2 = impStackTop(0).val;

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                case TYP_LONG:
                case TYP_ULONG:
                {
                    bool useToScalar = op2->IsIntegralConst(0);

#if defined(TARGET_X86)
                    useToScalar &= !varTypeIsLong(simdBaseType);
#endif // TARGET_X86

                    if (!useToScalar && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        // Using software fallback if simdBaseType is not supported by hardware
                        return nullptr;
                    }
                    break;
                }

                case TYP_DOUBLE:
                case TYP_FLOAT:
                case TYP_SHORT:
                case TYP_USHORT:
                {
                    // short/ushort/float/double is supported by SSE2
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            impPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_GreaterThan:
        case NI_Vector256_GreaterThan:
        case NI_Vector512_GreaterThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_GreaterThanAll:
        case NI_Vector256_GreaterThanAll:
        case NI_Vector512_GreaterThanAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_GreaterThanAny:
        case NI_Vector256_GreaterThanAny:
        case NI_Vector512_GreaterThanAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqual:
        case NI_Vector256_GreaterThanOrEqual:
        case NI_Vector512_GreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqualAll:
        case NI_Vector256_GreaterThanOrEqualAll:
        case NI_Vector512_GreaterThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqualAny:
        case NI_Vector256_GreaterThanOrEqualAny:
        case NI_Vector512_GreaterThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThan:
        case NI_Vector256_LessThan:
        case NI_Vector512_LessThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThanAll:
        case NI_Vector256_LessThanAll:
        case NI_Vector512_LessThanAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThanAny:
        case NI_Vector256_LessThanAny:
        case NI_Vector512_LessThanAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqual:
        case NI_Vector256_LessThanOrEqual:
        case NI_Vector512_LessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqualAll:
        case NI_Vector256_LessThanOrEqualAll:
        case NI_Vector512_LessThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqualAny:
        case NI_Vector256_LessThanOrEqualAny:
        case NI_Vector512_LessThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_SSE_LoadVector128:
        case NI_SSE2_LoadVector128:
        case NI_AVX_LoadVector256:
        case NI_AVX512F_LoadVector512:
        case NI_AVX512BW_LoadVector512:
        case NI_Vector128_Load:
        case NI_Vector256_Load:
        case NI_Vector512_Load:
        case NI_Vector128_LoadUnsafe:
        case NI_Vector256_LoadUnsafe:
        case NI_Vector512_LoadUnsafe:
        {
            if (sig->numArgs == 2)
            {
                op2 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 1);
            }

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            if (sig->numArgs == 2)
            {
                op3 = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, op3);
                op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);
            }

            retNode = gtNewSimdLoadNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_LoadAligned:
        case NI_Vector256_LoadAligned:
        case NI_Vector512_LoadAligned:
        {
            assert(sig->numArgs == 1);

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadAlignedNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_LoadAlignedNonTemporal:
        case NI_Vector256_LoadAlignedNonTemporal:
        case NI_Vector512_LoadAlignedNonTemporal:
        {
            assert(sig->numArgs == 1);

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadNonTemporalNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Max:
        case NI_Vector256_Max:
        case NI_Vector512_Max:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Min:
        case NI_Vector256_Min:
        case NI_Vector512_Min:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Multiply:
        case NI_Vector256_Multiply:
        case NI_Vector512_Multiply:
        case NI_Vector128_op_Multiply:
        case NI_Vector256_op_Multiply:
        case NI_Vector512_op_Multiply:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 32) && !varTypeIsFloating(simdBaseType) &&
                !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                break;
            }

            assert(simdSize != 64 || IsBaselineVector512IsaSupportedDebugOnly());

            if (varTypeIsLong(simdBaseType))
            {
                if (simdSize != 64 && !compOpportunisticallyDependsOn(InstructionSet_AVX512DQ_VL))
                {
                    // TODO-XARCH-CQ: We should support long/ulong multiplication
                    break;
                }
// else if simdSize == 64 then above assert would check if baseline isa supported

#if defined(TARGET_X86)
                // TODO-XARCH-CQ: We need to support 64-bit CreateBroadcast
                break;
#endif // TARGET_X86
            }

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Narrow:
        case NI_Vector256_Narrow:
        case NI_Vector512_Narrow:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                assert((simdSize != 64) || IsBaselineVector512IsaSupportedDebugOnly());

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Negate:
        case NI_Vector256_Negate:
        case NI_Vector512_Negate:
        case NI_Vector128_op_UnaryNegation:
        case NI_Vector256_op_UnaryNegation:
        case NI_Vector512_op_UnaryNegation:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_OnesComplement:
        case NI_Vector256_OnesComplement:
        case NI_Vector512_OnesComplement:
        case NI_Vector128_op_OnesComplement:
        case NI_Vector256_op_OnesComplement:
        case NI_Vector512_op_OnesComplement:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_op_Inequality:
        case NI_Vector256_op_Inequality:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector512_op_Inequality:
        {
            assert(sig->numArgs == 2);

            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize);
            }

            break;
        }

        case NI_Vector128_op_UnaryPlus:
        case NI_Vector256_op_UnaryPlus:
        case NI_Vector512_op_UnaryPlus:
        {
            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack();
            break;
        }

        case NI_Vector128_Subtract:
        case NI_Vector256_Subtract:
        case NI_Vector512_Subtract:
        case NI_Vector128_op_Subtraction:
        case NI_Vector256_op_Subtraction:
        case NI_Vector512_op_Subtraction:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ShiftLeft:
        case NI_Vector256_ShiftLeft:
        case NI_Vector512_ShiftLeft:
        case NI_Vector128_op_LeftShift:
        case NI_Vector256_op_LeftShift:
        case NI_Vector512_op_LeftShift:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsByte(simdBaseType))
            {
                // byte and sbyte would require more work to support
                break;
            }

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ShiftRightArithmetic:
        case NI_Vector256_ShiftRightArithmetic:
        case NI_Vector512_ShiftRightArithmetic:
        case NI_Vector128_op_RightShift:
        case NI_Vector256_op_RightShift:
        case NI_Vector512_op_RightShift:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsByte(simdBaseType))
            {
                // byte and sbyte would require more work to support
                break;
            }

            if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
                {
                    // long, ulong, and double would require more work to support
                    break;
                }
            }

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;

                op2 = impPopStack().val;
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ShiftRightLogical:
        case NI_Vector256_ShiftRightLogical:
        case NI_Vector512_ShiftRightLogical:
        case NI_Vector128_op_UnsignedRightShift:
        case NI_Vector256_op_UnsignedRightShift:
        case NI_Vector512_op_UnsignedRightShift:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Shuffle:
        case NI_Vector256_Shuffle:
        case NI_Vector512_Shuffle:
        {
            assert((sig->numArgs == 2) || (sig->numArgs == 3));
            assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));

            GenTree* indices = impStackTop(0).val;

            if (!indices->IsVectorConst())
            {
                // TODO-XARCH-CQ: Handling non-constant indices is a bit more complex
                break;
            }

            size_t elementSize  = genTypeSize(simdBaseType);
            size_t elementCount = simdSize / elementSize;

            if (simdSize == 32)
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    // While we could accelerate some functions on hardware with only AVX support
                    // it's likely not worth it overall given that IsHardwareAccelerated reports false
                    break;
                }
                else if ((varTypeIsByte(simdBaseType) &&
                          !compOpportunisticallyDependsOn(InstructionSet_AVX512VBMI_VL)) ||
                         (varTypeIsShort(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX512BW_VL)))
                {
                    bool crossLane = false;

                    for (size_t index = 0; index < elementCount; index++)
                    {
                        uint64_t value = indices->GetIntegralVectorConstElement(index, simdBaseType);

                        if (value >= elementCount)
                        {
                            continue;
                        }

                        if (index < (elementCount / 2))
                        {
                            if (value >= (elementCount / 2))
                            {
                                crossLane = true;
                                break;
                            }
                        }
                        else if (value < (elementCount / 2))
                        {
                            crossLane = true;
                            break;
                        }
                    }

                    if (crossLane)
                    {
                        // TODO-XARCH-CQ: We should emulate cross-lane shuffling for byte/sbyte and short/ushort
                        break;
                    }
                }
            }
            else if (simdSize == 64)
            {
                if (varTypeIsByte(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX512VBMI))
                {
                    // TYP_BYTE, TYP_UBYTE need AVX512VBMI.
                    break;
                }
            }
            else
            {
                assert(simdSize == 16);

                if (varTypeIsSmall(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                {
                    // TYP_BYTE, TYP_UBYTE, TYP_SHORT, and TYP_USHORT need SSSE3 to be able to shuffle any operation
                    break;
                }
            }

            if (sig->numArgs == 2)
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Sqrt:
        case NI_Vector256_Sqrt:
        case NI_Vector512_Sqrt:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_SSE_Store:
        case NI_SSE2_Store:
        case NI_AVX_Store:
        case NI_AVX512F_Store:
        case NI_AVX512BW_Store:
        {
            assert(retType == TYP_VOID);
            assert(sig->numArgs == 2);

            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdStoreNode(op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Store:
        case NI_Vector256_Store:
        case NI_Vector512_Store:
        case NI_Vector128_StoreUnsafe:
        case NI_Vector256_StoreUnsafe:
        case NI_Vector512_StoreUnsafe:
        {
            assert(retType == TYP_VOID);
            var_types simdType = getSIMDTypeForSize(simdSize);

            if (sig->numArgs == 3)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

                op3 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 2);

                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            if (sig->numArgs == 3)
            {
                op4 = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, op4);
                op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreNode(op2, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_StoreAligned:
        case NI_Vector256_StoreAligned:
        case NI_Vector512_StoreAligned:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

            var_types simdType = getSIMDTypeForSize(simdSize);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreAlignedNode(op2, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_StoreAlignedNonTemporal:
        case NI_Vector256_StoreAlignedNonTemporal:
        case NI_Vector512_StoreAlignedNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

            var_types simdType = getSIMDTypeForSize(simdSize);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;

            if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op2 = op2->gtGetOp1();
            }

            op1 = impSIMDPopStack();

            retNode = gtNewSimdStoreNonTemporalNode(op2, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Sum:
        case NI_Vector256_Sum:
        case NI_Vector512_Sum:
        {
            assert(sig->numArgs == 1);
            var_types simdType = getSIMDTypeForSize(simdSize);

            if ((simdSize == 32) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // Vector256 requires AVX2
                break;
            }
            else if ((simdSize == 16) && !compOpportunisticallyDependsOn(InstructionSet_SSE2))
            {
                break;
            }
#if defined(TARGET_X86)
            else if (varTypeIsLong(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // We need SSE41 to handle long, use software fallback
                break;
            }
#endif // TARGET_X86

            op1     = impSIMDPopStack();
            retNode = gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // We need SSE41 to handle long, use software fallback
                break;
            }
#endif // TARGET_X86

            op1     = impSIMDPopStack();
            retNode = gtNewSimdToScalarNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ToVector256:
        case NI_Vector128_ToVector256Unsafe:
        {
            assert(sig->numArgs == 1);
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_GetLower:
        {
            assert(sig->numArgs == 1);
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetLowerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_GetUpper:
        {
            assert(sig->numArgs == 1);
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetUpperNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector512_GetLower:
        {
            assert(sig->numArgs == 1);
            assert(IsBaselineVector512IsaSupportedDebugOnly());

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetLowerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector512_GetUpper:
        {
            assert(sig->numArgs == 1);
            assert(IsBaselineVector512IsaSupportedDebugOnly());

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetUpperNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ToVector512:
        case NI_Vector256_ToVector512:
        case NI_Vector256_ToVector512Unsafe:
        case NI_Vector512_GetLower128:
        {
            assert(sig->numArgs == 1);
            assert(IsBaselineVector512IsaSupportedDebugOnly());

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_WidenLower:
        case NI_Vector256_WidenLower:
        case NI_Vector512_WidenLower:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                assert((simdSize != 64) || IsBaselineVector512IsaSupportedDebugOnly());

                op1 = impSIMDPopStack();

                retNode = gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_WidenUpper:
        case NI_Vector256_WidenUpper:
        case NI_Vector512_WidenUpper:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                assert((simdSize != 64) || IsBaselineVector512IsaSupportedDebugOnly());

                op1 = impSIMDPopStack();

                retNode = gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_WithElement:
        case NI_Vector256_WithElement:
        case NI_Vector512_WithElement:
        {
            assert(sig->numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->OperIsConst())
            {
                // TODO-XARCH-CQ: We should always import these like we do with GetElement
                // Index is not a constant, use the software fallback
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if ((imm8 >= count) || (imm8 < 0))
            {
                // Using software fallback if index is out of range (throw exception)
                return nullptr;
            }

            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_LONG:
                case TYP_ULONG:
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE41_X64))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_DOUBLE:
                case TYP_FLOAT:
                case TYP_SHORT:
                case TYP_USHORT:
                    // short/ushort/float/double is supported by SSE2
                    break;

                default:
                    unreached();
            }

            GenTree* valueOp = impPopStack().val;
            impPopStack(); // Pop the indexOp now that we know its valid
            GenTree* vectorOp = impSIMDPopStack();

            retNode = gtNewSimdWithElementNode(retType, vectorOp, indexOp, valueOp, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_WithLower:
        {
            assert(sig->numArgs == 2);
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithLowerNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_WithUpper:
        {
            assert(sig->numArgs == 2);
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector512_WithLower:
        {
            assert(sig->numArgs == 2);
            assert(IsBaselineVector512IsaSupportedDebugOnly());

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithLowerNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector512_WithUpper:
        {
            assert(sig->numArgs == 2);
            assert(IsBaselineVector512IsaSupportedDebugOnly());

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Xor:
        case NI_Vector256_Xor:
        case NI_Vector512_Xor:
        case NI_Vector128_op_ExclusiveOr:
        case NI_Vector256_op_ExclusiveOr:
        case NI_Vector512_op_ExclusiveOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_X86Base_Pause:
        case NI_X86Serialize_Serialize:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;
        }

        case NI_X86Base_DivRem:
        case NI_X86Base_X64_DivRem:
        {
            assert(sig->numArgs == 3);
            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));
            assert(retType == TYP_STRUCT);
            assert(simdBaseJitType != CORINFO_TYPE_UNDEF);

            op3 = impPopStack().val;
            op2 = impPopStack().val;
            op1 = impPopStack().val;

            GenTreeHWIntrinsic* divRemIntrinsic = gtNewScalarHWIntrinsicNode(retType, op1, op2, op3, intrinsic);

            // Store the type from signature into SIMD base type for convenience
            divRemIntrinsic->SetSimdBaseJitType(simdBaseJitType);

            retNode = impStoreMultiRegValueToVar(divRemIntrinsic,
                                                 sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
            break;
        }

        case NI_SSE_CompareScalarGreaterThan:
        case NI_SSE_CompareScalarGreaterThanOrEqual:
        case NI_SSE_CompareScalarNotGreaterThan:
        case NI_SSE_CompareScalarNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            bool supportsAvx = compOpportunisticallyDependsOn(InstructionSet_AVX);

            if (!supportsAvx)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2             = impSIMDPopStack();
            op1             = impSIMDPopStack();
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_FLOAT);

            if (supportsAvx)
            {
                // These intrinsics are "special import" because the non-AVX path isn't directly
                // hardware supported. Instead, they start with "swapped operands" and we fix that here.

                int ival = HWIntrinsicInfo::lookupIval(this, intrinsic, simdBaseType);
                retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, gtNewIconNode(ival), NI_AVX_CompareScalar,
                                                   simdBaseJitType, simdSize);
            }
            else
            {
                GenTree* clonedOp1 = nullptr;
                op1                = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone op1 for Sse.CompareScalarGreaterThan"));

                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, intrinsic, simdBaseJitType, simdSize);
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, clonedOp1, retNode, NI_SSE_MoveScalar, simdBaseJitType,
                                                   simdSize);
            }
            break;
        }

        case NI_SSE_Prefetch0:
        case NI_SSE_Prefetch1:
        case NI_SSE_Prefetch2:
        case NI_SSE_PrefetchNonTemporal:
        {
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, intrinsic, CORINFO_TYPE_UBYTE, 0);
            break;
        }

        case NI_SSE_StoreFence:
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;

        case NI_SSE2_CompareScalarGreaterThan:
        case NI_SSE2_CompareScalarGreaterThanOrEqual:
        case NI_SSE2_CompareScalarNotGreaterThan:
        case NI_SSE2_CompareScalarNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            bool supportsAvx = compOpportunisticallyDependsOn(InstructionSet_AVX);

            if (!supportsAvx)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_DOUBLE);

            if (supportsAvx)
            {
                // These intrinsics are "special import" because the non-AVX path isn't directly
                // hardware supported. Instead, they start with "swapped operands" and we fix that here.

                int ival = HWIntrinsicInfo::lookupIval(this, intrinsic, simdBaseType);
                retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, gtNewIconNode(ival), NI_AVX_CompareScalar,
                                                   simdBaseJitType, simdSize);
            }
            else
            {
                GenTree* clonedOp1 = nullptr;
                op1                = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone op1 for Sse2.CompareScalarGreaterThan"));

                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, intrinsic, simdBaseJitType, simdSize);
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, clonedOp1, retNode, NI_SSE2_MoveScalar, simdBaseJitType,
                                                   simdSize);
            }
            break;
        }

        case NI_SSE2_LoadFence:
        case NI_SSE2_MemoryFence:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(JITtype2varType(sig->retType) == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE argList = info.compCompHnd->getArgNext(sig->args);
            CORINFO_CLASS_HANDLE    argClass;
            CorInfoType             argJitType = strip(info.compCompHnd->getArgType(sig, argList, &argClass));

            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, NI_SSE2_StoreNonTemporal, argJitType, 0);
            break;
        }

        case NI_AVX2_PermuteVar8x32:
        case NI_AVX512BW_PermuteVar32x16:
        case NI_AVX512BW_VL_PermuteVar8x16:
        case NI_AVX512BW_VL_PermuteVar16x16:
        case NI_AVX512F_PermuteVar8x64:
        case NI_AVX512F_PermuteVar16x32:
        case NI_AVX512F_VL_PermuteVar4x64:
        case NI_AVX512VBMI_PermuteVar64x8:
        case NI_AVX512VBMI_VL_PermuteVar16x8:
        case NI_AVX512VBMI_VL_PermuteVar32x8:
        {
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            // swap the two operands
            GenTree* idxVector = impSIMDPopStack();
            GenTree* srcVector = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, idxVector, srcVector, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512F_Fixup:
        case NI_AVX512F_FixupScalar:
        case NI_AVX512F_VL_Fixup:
        {
            assert(sig->numArgs == 4);

            op4 = impPopStack().val;
            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, op4, intrinsic, simdBaseJitType, simdSize);

            if (!retNode->isRMWHWIntrinsic(this))
            {
                if (!op1->IsVectorZero())
                {
                    GenTree* zero = gtNewZeroConNode(retType);

                    if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
                    {
                        op1 = gtNewOperNode(GT_COMMA, retType, op1, zero);
                    }
                    else
                    {
                        op1 = zero;
                    }

                    retNode->AsHWIntrinsic()->Op(1) = op1;
                }
            }
            break;
        }

        case NI_AVX512F_TernaryLogic:
        case NI_AVX512F_VL_TernaryLogic:
        {
            assert(sig->numArgs == 4);

            op4 = impPopStack().val;

            if (op4->IsIntegralConst())
            {
                uint8_t                 control  = static_cast<uint8_t>(op4->AsIntCon()->gtIconVal);
                const TernaryLogicInfo& info     = TernaryLogicInfo::lookup(control);
                TernaryLogicUseFlags    useFlags = info.GetAllUseFlags();

                if (useFlags != TernaryLogicUseFlags::ABC)
                {
                    // We are not using all 3 inputs, so we can potentially optimize
                    //
                    // In particular, for unary and binary operations we want to prefer
                    // the standard operator over vpternlog with unused operands where
                    // possible and we want to normalize to a consistent ternlog otherwise.
                    //
                    // Doing this massively simplifies downstream checks because later
                    // phases, such as morph which can combine bitwise operations to
                    // produce new vpternlog nodes, no longer have to consider all the
                    // special edges themselves.
                    //
                    // For example, they don't have to consider that `bitwise and` could
                    // present itself as all of the following:
                    // * HWINTRINSIC_TernaryLogic(op1, op2, unused, cns)
                    // * HWINTRINSIC_TernaryLogic(op1, unused, op2, cns)
                    // * HWINTRINSIC_TernaryLogic(unused, op1, op2, cns)
                    //
                    // Instead, it will only see HWINTRINSIC_And(op1, op2).
                    //
                    // For cases which must be kept as vpternlog, such as `not` or `xnor`
                    // (because there is no regular unary/binary operator for them), it
                    // ensures we only have one form to consider and that any side effects
                    // will have already been spilled where relevant.
                    //
                    // For example, they don't have to consider that `not` could present
                    // itself as all of the following:
                    // * HWINTRINSIC_TernaryLogic(op1, unused, unused, cns)
                    // * HWINTRINSIC_TernaryLogic(unused, op1, unused, cns)
                    // * HWINTRINSIC_TernaryLogic(unused, unused, op1, cns)
                    //
                    // Instead, it will only see  HWINTRINSIC_TernaryLogic(unused, unused, op1, cns)

                    assert(info.oper2 != TernaryLogicOperKind::Select);
                    assert(info.oper2 != TernaryLogicOperKind::True);
                    assert(info.oper2 != TernaryLogicOperKind::False);
                    assert(info.oper2 != TernaryLogicOperKind::Cond);
                    assert(info.oper2 != TernaryLogicOperKind::Major);
                    assert(info.oper2 != TernaryLogicOperKind::Minor);
                    assert(info.oper3 == TernaryLogicOperKind::None);
                    assert(info.oper3Use == TernaryLogicUseFlags::None);

                    bool spillOp1 = false;
                    bool spillOp2 = false;

                    GenTree** val1 = &op1;
                    GenTree** val2 = &op2;
                    GenTree** val3 = &op3;

                    bool unusedVal1 = false;
                    bool unusedVal2 = false;
                    bool unusedVal3 = false;

                    switch (useFlags)
                    {
                        case TernaryLogicUseFlags::A:
                        {
                            // We're only using op1, so we'll swap
                            // from '1, 2, 3' to '2, 3, 1', this
                            // means we need to spill op1 and
                            // append op2/op3 as gtUnusedVal
                            //
                            // This gives us:
                            // * tmp1 = op1
                            // * unused(op2)
                            // * unused(op3)
                            // * res  = tmp1

                            spillOp1 = true;

                            std::swap(val1, val2); // 2, 1, 3
                            std::swap(val2, val3); // 2, 3, 1

                            unusedVal1 = true;
                            unusedVal2 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::B:
                        {
                            // We're only using op2, so we'll swap
                            // from '1, 2, 3' to '1, 3, 2', this
                            // means we need to spill op1/op2 and
                            // append op3 as gtUnusedVal
                            //
                            // This gives us:
                            // * tmp1 = op1
                            // * tmp2 = op2
                            // * unused(op3)
                            // * res  = tmp2

                            spillOp1 = true;
                            spillOp2 = true;

                            std::swap(val2, val3); // 1, 3, 2

                            unusedVal1 = true;
                            unusedVal2 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::C:
                        {
                            // We're only using op3, so we don't
                            // need to swap, but we do need to
                            // append op1/op2 as gtUnusedVal
                            //
                            // This gives us:
                            // * unused(op1)
                            // * unused(op2)
                            // * res = op3

                            unusedVal1 = true;
                            unusedVal2 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::AB:
                        {
                            // We're using op1 and op2, so we need
                            // to swap from '1, 2, 3' to '3, 1, 2',
                            // this means we need to spill op1/op2
                            // and append op3 as gtUnusedVal
                            //
                            // This gives us:
                            // tmp1 = op1
                            // tmp2 = op2
                            // unused(op3)
                            // res  = BinOp(tmp1, tmp2)

                            spillOp1 = true;
                            spillOp2 = true;

                            std::swap(val1, val3); // 3, 2, 1
                            std::swap(val2, val3); // 3, 1, 2

                            unusedVal1 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::AC:
                        {
                            // We're using op1 and op3, so we need
                            // to swap from  '1, 2, 3' to '2, 1, 3',
                            // this means we need to spill op1 and
                            // append op2 as gtUnusedVal
                            //
                            // This gives us:
                            // tmp1 = op1
                            // unused(op2)
                            // res  = BinOp(tmp1, op3)

                            spillOp1 = true;

                            std::swap(val1, val2); // 2, 1, 3

                            unusedVal1 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::BC:
                        {
                            // We're using op2 and op3, so we don't
                            // need to swap, but we do need to
                            // append op1 as gtUnusedVal
                            //
                            // This gives us:
                            // * unused(op1)
                            // * res = BinOp(op2, op3)

                            unusedVal1 = true;
                            break;
                        }

                        case TernaryLogicUseFlags::None:
                        {
                            // We're not using any operands, so we don't
                            // need to swap, but we do need push all three
                            // operands up as gtUnusedVal

                            unusedVal1 = true;
                            unusedVal2 = true;
                            unusedVal3 = true;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    if (spillOp1)
                    {
                        impSpillSideEffect(true, verCurrentState.esStackDepth -
                                                     3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
                    }

                    if (spillOp2)
                    {
                        impSpillSideEffect(true, verCurrentState.esStackDepth -
                                                     2 DEBUGARG("Spilling op2 side effects for HWIntrinsic"));
                    }

                    op3 = impSIMDPopStack();
                    op2 = impSIMDPopStack();
                    op1 = impSIMDPopStack();

                    // Consume operands we won't use, in case they have side effects.
                    //
                    if (unusedVal1 && !(*val1)->IsVectorZero())
                    {
                        impAppendTree(gtUnusedValNode(*val1), CHECK_SPILL_ALL, impCurStmtDI);
                    }

                    if (unusedVal2 && !(*val2)->IsVectorZero())
                    {
                        impAppendTree(gtUnusedValNode(*val2), CHECK_SPILL_ALL, impCurStmtDI);
                    }

                    if (unusedVal3 && !(*val3)->IsVectorZero())
                    {
                        impAppendTree(gtUnusedValNode(*val3), CHECK_SPILL_ALL, impCurStmtDI);
                    }

                    // cast in switch clause is needed for old gcc
                    switch ((TernaryLogicOperKind)info.oper1)
                    {
                        case TernaryLogicOperKind::Select:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(0xF0)) || // A
                                   (control == static_cast<uint8_t>(0xCC)) || // B
                                   (control == static_cast<uint8_t>(0xAA)));  // C

                            assert(unusedVal1);
                            assert(unusedVal2);
                            assert(!unusedVal3);

                            return *val3;
                        }

                        case TernaryLogicOperKind::True:
                        {
                            assert(info.oper1Use == TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert(control == static_cast<uint8_t>(0xFF));

                            assert(unusedVal1);
                            assert(unusedVal2);
                            assert(unusedVal3);

                            return gtNewAllBitsSetConNode(retType);
                        }

                        case TernaryLogicOperKind::False:
                        {
                            assert(info.oper1Use == TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert(control == static_cast<uint8_t>(0x00));

                            assert(unusedVal1);
                            assert(unusedVal2);
                            assert(unusedVal3);

                            return gtNewZeroConNode(retType);
                        }

                        case TernaryLogicOperKind::Not:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            if (info.oper2 == TernaryLogicOperKind::None)
                            {
                                assert(info.oper2Use == TernaryLogicUseFlags::None);

                                assert((control == static_cast<uint8_t>(~0xF0)) || // ~A
                                       (control == static_cast<uint8_t>(~0xCC)) || // ~B
                                       (control == static_cast<uint8_t>(~0xAA)));  // ~C

                                assert(unusedVal1);
                                assert(unusedVal2);
                                assert(!unusedVal3);

                                if (!(*val1)->IsVectorZero())
                                {
                                    *val1 = gtNewZeroConNode(retType);
                                }

                                if (!(*val2)->IsVectorZero())
                                {
                                    *val2 = gtNewZeroConNode(retType);
                                }

                                op4->AsIntCon()->gtIconVal = static_cast<uint8_t>(~0xAA);
                                break;
                            }

                            assert(info.oper2Use != TernaryLogicUseFlags::None);

                            if (info.oper2 == TernaryLogicOperKind::And)
                            {
                                if ((control == static_cast<uint8_t>(~0xCC & 0xF0)) || // ~B & A
                                    (control == static_cast<uint8_t>(~0xAA & 0xF0)) || // ~C & A
                                    (control == static_cast<uint8_t>(~0xAA & 0xCC)))   // ~C & B
                                {
                                    // We're normalizing to ~B & C, so we need another swap
                                    std::swap(*val2, *val3);
                                }
                                else
                                {
                                    assert((control == static_cast<uint8_t>(~0xF0 & 0xCC)) || // ~A & B
                                           (control == static_cast<uint8_t>(~0xF0 & 0xAA)) || // ~A & C
                                           (control == static_cast<uint8_t>(~0xCC & 0xAA)));  // ~B & C
                                }

                                assert(unusedVal1);
                                assert(!unusedVal2);
                                assert(!unusedVal3);

                                // GT_AND_NOT takes them as `op1 & ~op2` and x86 reorders them back to `~op1 & op2`
                                // since the underlying andnps/andnpd/pandn instructions take them as such

                                return gtNewSimdBinOpNode(GT_AND_NOT, retType, *val3, *val2, simdBaseJitType, simdSize);
                            }
                            else
                            {
                                assert(info.oper2 == TernaryLogicOperKind::Or);

                                if ((control == static_cast<uint8_t>(~0xCC | 0xF0)) || // ~B | A
                                    (control == static_cast<uint8_t>(~0xAA | 0xF0)) || // ~C | A
                                    (control == static_cast<uint8_t>(~0xAA | 0xCC)))   // ~C | B
                                {
                                    // We're normalizing to ~B & C, so we need another swap
                                    std::swap(*val2, *val3);
                                }
                                else
                                {
                                    assert((control == static_cast<uint8_t>(~0xF0 | 0xCC)) || // ~A | B
                                           (control == static_cast<uint8_t>(~0xF0 | 0xAA)) || // ~A | C
                                           (control == static_cast<uint8_t>(~0xCC | 0xAA)));  // ~B | C
                                }

                                assert(unusedVal1);
                                assert(!unusedVal2);
                                assert(!unusedVal3);

                                if (!(*val1)->IsVectorZero())
                                {
                                    *val1 = gtNewZeroConNode(retType);
                                }

                                op4->AsIntCon()->gtIconVal = static_cast<uint8_t>(~0xCC | 0xAA);
                            }
                            break;
                        }

                        case TernaryLogicOperKind::And:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(0xF0 & 0xCC)) || // A & B
                                   (control == static_cast<uint8_t>(0xF0 & 0xAA)) || // A & C
                                   (control == static_cast<uint8_t>(0xCC & 0xAA)));  // B & C

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            return gtNewSimdBinOpNode(GT_AND, retType, *val2, *val3, simdBaseJitType, simdSize);
                        }

                        case TernaryLogicOperKind::Nand:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(~(0xF0 & 0xCC))) || // ~(A & B)
                                   (control == static_cast<uint8_t>(~(0xF0 & 0xAA))) || // ~(A & C)
                                   (control == static_cast<uint8_t>(~(0xCC & 0xAA))));  // ~(B & C)

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            if (!(*val1)->IsVectorZero())
                            {
                                *val1 = gtNewZeroConNode(retType);
                            }

                            op4->AsIntCon()->gtIconVal = static_cast<uint8_t>(~(0xCC & 0xAA));
                            break;
                        }

                        case TernaryLogicOperKind::Or:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(0xF0 | 0xCC)) || // A | B
                                   (control == static_cast<uint8_t>(0xF0 | 0xAA)) || // A | C
                                   (control == static_cast<uint8_t>(0xCC | 0xAA)));  // B | C

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            return gtNewSimdBinOpNode(GT_OR, retType, *val2, *val3, simdBaseJitType, simdSize);
                        }

                        case TernaryLogicOperKind::Nor:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(~(0xF0 | 0xCC))) || // ~(A | B)
                                   (control == static_cast<uint8_t>(~(0xF0 | 0xAA))) || // ~(A | C)
                                   (control == static_cast<uint8_t>(~(0xCC | 0xAA))));  // ~(B | C)

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            if (!(*val1)->IsVectorZero())
                            {
                                *val1 = gtNewZeroConNode(retType);
                            }

                            op4->AsIntCon()->gtIconVal = static_cast<uint8_t>(~(0xCC | 0xAA));
                            break;
                        }

                        case TernaryLogicOperKind::Xor:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(0xF0 ^ 0xCC)) || // A ^ B
                                   (control == static_cast<uint8_t>(0xF0 ^ 0xAA)) || // A ^ C
                                   (control == static_cast<uint8_t>(0xCC ^ 0xAA)));  // B ^ C

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            return gtNewSimdBinOpNode(GT_XOR, retType, *val2, *val3, simdBaseJitType, simdSize);
                        }

                        case TernaryLogicOperKind::Xnor:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            assert(info.oper2 == TernaryLogicOperKind::None);
                            assert(info.oper2Use == TernaryLogicUseFlags::None);

                            assert((control == static_cast<uint8_t>(~(0xF0 ^ 0xCC))) || // ~(A ^ B)
                                   (control == static_cast<uint8_t>(~(0xF0 ^ 0xAA))) || // ~(A ^ C)
                                   (control == static_cast<uint8_t>(~(0xCC ^ 0xAA))));  // ~(B ^ C)

                            assert(unusedVal1);
                            assert(!unusedVal2);
                            assert(!unusedVal3);

                            if (!(*val1)->IsVectorZero())
                            {
                                *val1 = gtNewZeroConNode(retType);
                            }

                            op4->AsIntCon()->gtIconVal = static_cast<uint8_t>(~(0xCC ^ 0xAA));
                            break;
                        }

                        case TernaryLogicOperKind::None:
                        case TernaryLogicOperKind::Cond:
                        case TernaryLogicOperKind::Major:
                        case TernaryLogicOperKind::Minor:
                        {
                            // invalid table metadata
                            unreached();
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    retNode = gtNewSimdTernaryLogicNode(retType, *val1, *val2, *val3, op4, simdBaseJitType, simdSize);
                    break;
                }
            }

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdTernaryLogicNode(retType, op1, op2, op3, op4, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512F_BlendVariable:
        case NI_AVX512BW_BlendVariable:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op3 = gtNewSimdHWIntrinsicNode(TYP_MASK, op3, NI_AVX512F_ConvertVectorToMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, NI_AVX512F_BlendVariableMask, simdBaseJitType,
                                               simdSize);
            break;
        }

        case NI_AVX512F_CompareEqual:
        case NI_AVX512BW_CompareEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareEqualMask, simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareGreaterThan:
        case NI_AVX512F_VL_CompareGreaterThan:
        case NI_AVX512BW_CompareGreaterThan:
        case NI_AVX512BW_VL_CompareGreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareGreaterThanMask, simdBaseJitType,
                                               simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareGreaterThanOrEqual:
        case NI_AVX512F_VL_CompareGreaterThanOrEqual:
        case NI_AVX512BW_CompareGreaterThanOrEqual:
        case NI_AVX512BW_VL_CompareGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareGreaterThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareLessThan:
        case NI_AVX512F_VL_CompareLessThan:
        case NI_AVX512BW_CompareLessThan:
        case NI_AVX512BW_VL_CompareLessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareLessThanMask, simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareLessThanOrEqual:
        case NI_AVX512F_VL_CompareLessThanOrEqual:
        case NI_AVX512BW_CompareLessThanOrEqual:
        case NI_AVX512BW_VL_CompareLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareLessThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareNotEqual:
        case NI_AVX512F_VL_CompareNotEqual:
        case NI_AVX512BW_CompareNotEqual:
        case NI_AVX512BW_VL_CompareNotEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareNotEqualMask, simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareNotGreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareNotGreaterThanMask,
                                               simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareNotGreaterThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareNotLessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareNotLessThanMask, simdBaseJitType,
                                               simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareNotLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareNotLessThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareOrdered:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareOrderedMask, simdBaseJitType, simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX512F_CompareUnordered:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512F_CompareUnorderedMask, simdBaseJitType,
                                               simdSize);
            retNode =
                gtNewSimdHWIntrinsicNode(retType, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);

            break;
        }

        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
        {
            CORINFO_ARG_LIST_HANDLE argList = sig->args;
            CORINFO_CLASS_HANDLE    argClass;
            var_types               argType = TYP_UNKNOWN;
            unsigned int            sizeBytes;
            simdBaseJitType   = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
            var_types retType = getSIMDTypeForSize(sizeBytes);

            assert(sig->numArgs == 5);
            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);
            CORINFO_ARG_LIST_HANDLE arg4 = info.compCompHnd->getArgNext(arg3);
            CORINFO_ARG_LIST_HANDLE arg5 = info.compCompHnd->getArgNext(arg4);

            argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg5, &argClass)));
            GenTree* op5 = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op5);

            argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
            GenTree* op4 = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op4);

            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            CorInfoType indexBaseJitType = getBaseJitTypeOfSIMDType(argClass);
            GenTree*    op3              = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op3);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op2);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op1);

            retNode = new (this, GT_HWINTRINSIC) GenTreeHWIntrinsic(retType, getAllocator(CMK_ASTNode), intrinsic,
                                                                    simdBaseJitType, simdSize, op1, op2, op3, op4, op5);
            retNode->AsHWIntrinsic()->SetAuxiliaryJitType(indexBaseJitType);
            break;
        }

        case NI_BMI2_ZeroHighBits:
        case NI_BMI2_X64_ZeroHighBits:
        {
            assert(sig->numArgs == 2);

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            // Instruction BZHI requires to encode op2 (3rd register) in VEX.vvvv and op1 maybe memory operand,
            // so swap op1 and op2 to unify the backend code.
            return gtNewScalarHWIntrinsicNode(retType, op2, op1, intrinsic);
        }

        case NI_BMI1_BitFieldExtract:
        case NI_BMI1_X64_BitFieldExtract:
        {
            // The 3-arg version is implemented in managed code
            if (sig->numArgs == 3)
            {
                return nullptr;
            }
            assert(sig->numArgs == 2);

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            // Instruction BEXTR requires to encode op2 (3rd register) in VEX.vvvv and op1 maybe memory operand,
            // so swap op1 and op2 to unify the backend code.
            return gtNewScalarHWIntrinsicNode(retType, op2, op1, intrinsic);
        }

        default:
        {
            return nullptr;
        }
    }

    return retNode;
}
#endif // FEATURE_HW_INTRINSICS
