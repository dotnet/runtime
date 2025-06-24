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
        case InstructionSet_SSE42:
            return InstructionSet_SSE42_X64;
        case InstructionSet_AVX:
            return InstructionSet_AVX_X64;
        case InstructionSet_AVX2:
            return InstructionSet_AVX2_X64;
        case InstructionSet_AVX512:
            return InstructionSet_AVX512_X64;
        case InstructionSet_AVX512v2:
            return InstructionSet_AVX512v2_X64;
        case InstructionSet_AVX512v3:
            return InstructionSet_AVX512v3_X64;
        case InstructionSet_AVX10v1:
            return InstructionSet_AVX10v1_X64;
        case InstructionSet_AVX10v2:
            return InstructionSet_AVX10v2_X64;
        case InstructionSet_AES:
            return InstructionSet_AES_X64;
        case InstructionSet_AVX512VP2INTERSECT:
            return InstructionSet_AVX512VP2INTERSECT_X64;
        case InstructionSet_AVXIFMA:
            return InstructionSet_AVXIFMA_X64;
        case InstructionSet_AVXVNNI:
            return InstructionSet_AVXVNNI_X64;
        case InstructionSet_GFNI:
            return InstructionSet_GFNI_X64;
        case InstructionSet_SHA:
            return InstructionSet_SHA_X64;
        case InstructionSet_WAITPKG:
            return InstructionSet_WAITPKG_X64;
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
        case InstructionSet_AVX512:
        case InstructionSet_AVX512v2:
        case InstructionSet_AVX512v3:
        case InstructionSet_AVX10v1:
        {
            // These nested ISAs aren't tracked by the JIT support
            return isa;
        }

        default:
        {
            return InstructionSet_NONE;
        }
    }
}

//------------------------------------------------------------------------
// V256VersionOfIsa: Gets the corresponding V256 only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The V256 only InstructionSet associated with isa
static CORINFO_InstructionSet V256VersionOfIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_AES:
        {
            return InstructionSet_AES_V256;
        }

        case InstructionSet_GFNI:
        {
            return InstructionSet_GFNI_V256;
        }

        default:
        {
            return InstructionSet_NONE;
        }
    }
}

//------------------------------------------------------------------------
// V512VersionOfIsa: Gets the corresponding V512 only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The V512 only InstructionSet associated with isa
static CORINFO_InstructionSet V512VersionOfIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_AVX10v1:
        case InstructionSet_AVX10v1_X64:
        case InstructionSet_AVX10v2:
        case InstructionSet_AVX10v2_X64:
        {
            // These nested ISAs aren't tracked by the JIT support
            return isa;
        }

        case InstructionSet_AES:
        {
            return InstructionSet_AES_V512;
        }

        case InstructionSet_GFNI:
        {
            return InstructionSet_GFNI_V512;
        }

        default:
        {
            return InstructionSet_NONE;
        }
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
        if (strcmp(className + 1, "es") == 0)
        {
            return InstructionSet_AES;
        }
        else if (strncmp(className + 1, "vx", 2) == 0)
        {
            if (className[3] == '\0')
            {
                return InstructionSet_AVX;
            }
            else if (strncmp(className + 3, "10v", 3) == 0)
            {
                if (strcmp(className + 6, "1") == 0)
                {
                    return InstructionSet_AVX10v1;
                }
                else if (strcmp(className + 6, "2") == 0)
                {
                    return InstructionSet_AVX10v2;
                }
            }
            else if (strcmp(className + 3, "2") == 0)
            {
                return InstructionSet_AVX2;
            }
            else if (strncmp(className + 3, "512", 3) == 0)
            {
                if (className[6] == 'B')
                {
                    if (strcmp(className + 7, "italg") == 0)
                    {
                        return InstructionSet_AVX512v3;
                    }
                    else if (strcmp(className + 7, "f16") == 0)
                    {
                        return InstructionSet_AVX10v1;
                    }
                    else if (strcmp(className + 7, "W") == 0)
                    {
                        return InstructionSet_AVX512;
                    }
                }
                else if ((strcmp(className + 6, "CD") == 0) || (strcmp(className + 6, "DQ") == 0))
                {
                    return InstructionSet_AVX512;
                }
                else if (className[6] == 'F')
                {
                    if (className[7] == '\0')
                    {
                        return InstructionSet_AVX512;
                    }
                    else if (strcmp(className + 7, "p16") == 0)
                    {
                        return InstructionSet_AVX10v1;
                    }
                }
                else if (className[6] == 'V')
                {
                    if (strncmp(className + 7, "bmi", 3) == 0)
                    {
                        if (className[10] == '\0')
                        {
                            return InstructionSet_AVX512v2;
                        }
                        else if (strcmp(className + 10, "2") == 0)
                        {
                            return InstructionSet_AVX512v3;
                        }
                    }
                    else if (className[7] == 'p')
                    {
                        if (strcmp(className + 8, "p2intersect") == 0)
                        {
                            return InstructionSet_AVX512VP2INTERSECT;
                        }
                        else if (strcmp(className + 8, "opcntdq") == 0)
                        {
                            return InstructionSet_AVX512v3;
                        }
                    }
                }
            }
            else if (strcmp(className + 3, "Ifma") == 0)
            {
                return InstructionSet_AVXIFMA;
            }
            else if (strcmp(className + 3, "Vnni") == 0)
            {
                return InstructionSet_AVXVNNI;
            }
        }
    }
    else if (className[0] == 'B')
    {
        if (strncmp(className + 1, "mi", 2) == 0)
        {
            if (strcmp(className + 3, "1") == 0)
            {
                return InstructionSet_AVX2;
            }
            else if (strcmp(className + 3, "2") == 0)
            {
                return InstructionSet_AVX2;
            }
        }
    }
    else if (className[0] == 'F')
    {
        if (strcmp(className + 1, "ma") == 0)
        {
            return InstructionSet_AVX2;
        }
        else if (strcmp(className + 1, "16c") == 0)
        {
            return InstructionSet_AVX2;
        }
    }
    else if (className[0] == 'G')
    {
        if (strcmp(className + 1, "fni") == 0)
        {
            return InstructionSet_GFNI;
        }
    }
    else if (className[0] == 'L')
    {
        if (strcmp(className + 1, "zcnt") == 0)
        {
            return InstructionSet_AVX2;
        }
    }
    else if (className[0] == 'P')
    {
        if (strcmp(className + 1, "clmulqdq") == 0)
        {
            return InstructionSet_AES;
        }
        else if (strcmp(className + 1, "opcnt") == 0)
        {
            return InstructionSet_SSE42;
        }
    }
    else if (className[0] == 'S')
    {
        if (strcmp(className + 1, "ha") == 0)
        {
            return InstructionSet_SHA;
        }
        else if (strncmp(className + 1, "se", 2) == 0)
        {
            if ((className[3] == '\0') || (strcmp(className + 3, "2") == 0))
            {
                return InstructionSet_X86Base;
            }
            else if (strcmp(className + 3, "3") == 0)
            {
                return InstructionSet_SSE42;
            }
            else if (strcmp(className + 3, "41") == 0)
            {
                return InstructionSet_SSE42;
            }
            else if (strcmp(className + 3, "42") == 0)
            {
                return InstructionSet_SSE42;
            }
        }
        else if (strcmp(className + 1, "sse3") == 0)
        {
            return InstructionSet_SSE42;
        }
    }
    else if (className[0] == 'V')
    {
        if (strncmp(className + 1, "ector", 5) == 0)
        {
            if (strncmp(className + 6, "128", 3) == 0)
            {
                if ((className[9] == '\0') || (strcmp(className + 9, "`1") == 0))
                {
                    return InstructionSet_Vector128;
                }
            }
            else if (strncmp(className + 6, "256", 3) == 0)
            {
                if ((className[9] == '\0') || (strcmp(className + 9, "`1") == 0))
                {
                    return InstructionSet_Vector256;
                }
            }
            else if (strncmp(className + 6, "512", 3) == 0)
            {
                if ((className[9] == '\0') || (strcmp(className + 9, "`1") == 0))
                {
                    return InstructionSet_Vector512;
                }
            }
        }
        else if (strcmp(className + 1, "L") == 0)
        {
            assert(!"VL.X64 support doesn't exist in the managed libraries and so is not yet implemented");
            return InstructionSet_ILLEGAL;
        }
    }
    else if (strcmp(className, "WaitPkg") == 0)
    {
        return InstructionSet_WAITPKG;
    }
    else if (strncmp(className, "X86", 3) == 0)
    {
        if (strcmp(className + 3, "Base") == 0)
        {
            return InstructionSet_X86Base;
        }
        else if (strcmp(className + 3, "Serialize") == 0)
        {
            return InstructionSet_X86Serialize;
        }
    }

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclosing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    innerEnclosingClassName -- The name of the inner enclosing class of X64 classes
//    outerEnclosingClassName -- The name of the outer enclosing class of X64 classes
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
CORINFO_InstructionSet HWIntrinsicInfo::lookupIsa(const char* className,
                                                  const char* innerEnclosingClassName,
                                                  const char* outerEnclosingClassName)
{
    assert(className != nullptr);

    if (innerEnclosingClassName == nullptr)
    {
        // No nested class is the most common, so fast path it
        return lookupInstructionSet(className);
    }

    // Since lookupId is only called for the xplat intrinsics
    // or intrinsics in the platform specific namespace, we assume
    // that it will be one we can handle and don't try to early out.

    CORINFO_InstructionSet enclosingIsa = lookupIsa(innerEnclosingClassName, outerEnclosingClassName, nullptr);

    if (className[0] == 'V')
    {
        if (strcmp(className, "V256") == 0)
        {
            return V256VersionOfIsa(enclosingIsa);
        }
        else if (strcmp(className, "V512") == 0)
        {
            return V512VersionOfIsa(enclosingIsa);
        }
        else if (strcmp(className, "VL") == 0)
        {
            return VLVersionOfIsa(enclosingIsa);
        }
    }
    else if (strcmp(className, "X64") == 0)
    {
        return X64VersionOfIsa(enclosingIsa);
    }

    return InstructionSet_ILLEGAL;
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
        case NI_AVX512_Compare:
        case NI_AVX512_CompareMask:
        case NI_AVX10v2_MinMaxScalar:
        case NI_AVX10v2_MinMax:
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

        case NI_AVX512_GetMantissa:
        case NI_AVX512_GetMantissaScalar:
        case NI_AVX512_Range:
        case NI_AVX512_RangeScalar:
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
// lookupIdForFloatComparisonMode: Get the intrinsic ID to use for a given float comparison mode
//
// Arguments:
//    intrinsic    -- The base intrinsic that is being simplified
//    comparison   -- The comparison mode used
//    simdBaseType -- The base type for which the comparison is being done
//    simdSize     -- The simd size for which the comparison is being done
//
// Return Value:
//     The intrinsic ID to use instead of intrinsic
//
NamedIntrinsic HWIntrinsicInfo::lookupIdForFloatComparisonMode(NamedIntrinsic      intrinsic,
                                                               FloatComparisonMode comparison,
                                                               var_types           simdBaseType,
                                                               unsigned            simdSize)
{
    assert(varTypeIsFloating(simdBaseType));
    assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));

    switch (comparison)
    {
        case FloatComparisonMode::OrderedEqualNonSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareEqual;
            }
            return NI_X86Base_CompareEqual;
        }

        case FloatComparisonMode::OrderedGreaterThanSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareGreaterThanMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarGreaterThan;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareGreaterThan;
            }
            return NI_X86Base_CompareGreaterThan;
        }

        case FloatComparisonMode::OrderedGreaterThanOrEqualSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareGreaterThanOrEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarGreaterThanOrEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareGreaterThanOrEqual;
            }
            return NI_X86Base_CompareGreaterThanOrEqual;
        }

        case FloatComparisonMode::OrderedLessThanSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareLessThanMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarLessThan;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareLessThan;
            }
            return NI_X86Base_CompareLessThan;
        }

        case FloatComparisonMode::OrderedLessThanOrEqualSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareLessThanOrEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarLessThanOrEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareLessThanOrEqual;
            }
            return NI_X86Base_CompareLessThanOrEqual;
        }

        case FloatComparisonMode::UnorderedNotEqualNonSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareNotEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarNotEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareNotEqual;
            }
            return NI_X86Base_CompareNotEqual;
        }

        case FloatComparisonMode::UnorderedNotGreaterThanSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareNotGreaterThanMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarNotGreaterThan;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareNotGreaterThan;
            }
            return NI_X86Base_CompareNotGreaterThan;
        }

        case FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareNotGreaterThanOrEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarNotGreaterThanOrEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareNotGreaterThanOrEqual;
            }
            return NI_X86Base_CompareNotGreaterThanOrEqual;
        }

        case FloatComparisonMode::UnorderedNotLessThanSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareNotLessThanMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarNotLessThan;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareNotLessThan;
            }
            return NI_X86Base_CompareNotLessThan;
        }

        case FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareNotLessThanOrEqualMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarNotLessThanOrEqual;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareNotLessThanOrEqual;
            }
            return NI_X86Base_CompareNotLessThanOrEqual;
        }

        case FloatComparisonMode::OrderedNonSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareOrderedMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarOrdered;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareOrdered;
            }
            return NI_X86Base_CompareOrdered;
        }

        case FloatComparisonMode::UnorderedNonSignaling:
        {
            if (intrinsic == NI_AVX512_CompareMask)
            {
                return NI_AVX512_CompareUnorderedMask;
            }
            else if (intrinsic == NI_AVX_CompareScalar)
            {
                return NI_X86Base_CompareScalarUnordered;
            }

            assert(intrinsic == NI_AVX_Compare);

            if (simdSize == 32)
            {
                return NI_AVX_CompareUnordered;
            }
            return NI_X86Base_CompareUnordered;
        }

        default:
        {
            return intrinsic;
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
        case NI_X86Base_CompareEqual:
        case NI_X86Base_CompareScalarEqual:
        case NI_AVX_CompareEqual:
        case NI_AVX512_CompareEqualMask:
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

        case NI_X86Base_CompareGreaterThan:
        case NI_X86Base_CompareScalarGreaterThan:
        case NI_AVX_CompareGreaterThan:
        case NI_AVX512_CompareGreaterThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                // CompareGreaterThan is not directly supported in hardware without AVX support.
                // Lowering ensures we swap the operands and change to the correct ID.

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                return static_cast<int>(FloatComparisonMode::OrderedGreaterThanSignaling);
            }
            else if ((id == NI_AVX512_CompareGreaterThanMask) && varTypeIsUnsigned(simdBaseType))
            {
                // TODO-XARCH-CQ: Allow the other integer paths to use the EVEX encoding
                return static_cast<int>(IntComparisonMode::GreaterThan);
            }
            break;
        }

        case NI_X86Base_CompareLessThan:
        case NI_X86Base_CompareScalarLessThan:
        case NI_AVX_CompareLessThan:
        case NI_AVX512_CompareLessThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanSignaling);
            }
            else if (id == NI_AVX512_CompareLessThanMask)
            {
                // TODO-XARCH-CQ: Allow the other integer paths to use the EVEX encoding
                return static_cast<int>(IntComparisonMode::LessThan);
            }
            break;
        }

        case NI_X86Base_CompareGreaterThanOrEqual:
        case NI_X86Base_CompareScalarGreaterThanOrEqual:
        case NI_AVX_CompareGreaterThanOrEqual:
        case NI_AVX512_CompareGreaterThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                // CompareGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // Lowering ensures we swap the operands and change to the correct ID.

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                return static_cast<int>(FloatComparisonMode::OrderedGreaterThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareGreaterThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::GreaterThanOrEqual);
            }
            break;
        }

        case NI_X86Base_CompareLessThanOrEqual:
        case NI_X86Base_CompareScalarLessThanOrEqual:
        case NI_AVX_CompareLessThanOrEqual:
        case NI_AVX512_CompareLessThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::OrderedLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareLessThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::LessThanOrEqual);
            }
            break;
        }

        case NI_X86Base_CompareNotEqual:
        case NI_X86Base_CompareScalarNotEqual:
        case NI_AVX_CompareNotEqual:
        case NI_AVX512_CompareNotEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotEqualNonSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareNotEqualMask);
                return static_cast<int>(IntComparisonMode::NotEqual);
            }
            break;
        }

        case NI_X86Base_CompareNotGreaterThan:
        case NI_X86Base_CompareScalarNotGreaterThan:
        case NI_AVX_CompareNotGreaterThan:
        case NI_AVX512_CompareNotGreaterThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                // CompareNotGreaterThan is not directly supported in hardware without AVX support.
                // Lowering ensures we swap the operands and change to the correct ID.

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareNotGreaterThanMask);
                return static_cast<int>(IntComparisonMode::LessThanOrEqual);
            }
            break;
        }

        case NI_X86Base_CompareNotLessThan:
        case NI_X86Base_CompareScalarNotLessThan:
        case NI_AVX_CompareNotLessThan:
        case NI_AVX512_CompareNotLessThanMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareNotLessThanMask);
                return static_cast<int>(IntComparisonMode::GreaterThanOrEqual);
            }
            break;
        }

        case NI_X86Base_CompareNotGreaterThanOrEqual:
        case NI_X86Base_CompareScalarNotGreaterThanOrEqual:
        case NI_AVX_CompareNotGreaterThanOrEqual:
        case NI_AVX512_CompareNotGreaterThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                // CompareNotGreaterThanOrEqual is not directly supported in hardware without AVX support.
                // Lowering ensures we swap the operands and change to the correct ID.

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                return static_cast<int>(FloatComparisonMode::UnorderedNotGreaterThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareNotGreaterThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::LessThan);
            }
            break;
        }

        case NI_X86Base_CompareNotLessThanOrEqual:
        case NI_X86Base_CompareScalarNotLessThanOrEqual:
        case NI_AVX_CompareNotLessThanOrEqual:
        case NI_AVX512_CompareNotLessThanOrEqualMask:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                return static_cast<int>(FloatComparisonMode::UnorderedNotLessThanOrEqualSignaling);
            }
            else
            {
                assert(id == NI_AVX512_CompareNotLessThanOrEqualMask);
                return static_cast<int>(IntComparisonMode::GreaterThan);
            }
            break;
        }

        case NI_X86Base_CompareOrdered:
        case NI_X86Base_CompareScalarOrdered:
        case NI_AVX_CompareOrdered:
        case NI_AVX512_CompareOrderedMask:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatComparisonMode::OrderedNonSignaling);
        }

        case NI_X86Base_CompareUnordered:
        case NI_X86Base_CompareScalarUnordered:
        case NI_AVX_CompareUnordered:
        case NI_AVX512_CompareUnorderedMask:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatComparisonMode::UnorderedNonSignaling);
        }

        case NI_SSE42_Ceiling:
        case NI_SSE42_CeilingScalar:
        case NI_AVX_Ceiling:
        {
            FALLTHROUGH;
        }

        case NI_SSE42_RoundToPositiveInfinity:
        case NI_SSE42_RoundToPositiveInfinityScalar:
        case NI_AVX_RoundToPositiveInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToPositiveInfinity);
        }

        case NI_SSE42_Floor:
        case NI_SSE42_FloorScalar:
        case NI_AVX_Floor:
        {
            FALLTHROUGH;
        }

        case NI_SSE42_RoundToNegativeInfinity:
        case NI_SSE42_RoundToNegativeInfinityScalar:
        case NI_AVX_RoundToNegativeInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNegativeInfinity);
        }

        case NI_SSE42_RoundCurrentDirection:
        case NI_SSE42_RoundCurrentDirectionScalar:
        case NI_AVX_RoundCurrentDirection:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::CurrentDirection);
        }

        case NI_SSE42_RoundToNearestInteger:
        case NI_SSE42_RoundToNearestIntegerScalar:
        case NI_AVX_RoundToNearestInteger:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNearestInteger);
        }

        case NI_SSE42_RoundToZero:
        case NI_SSE42_RoundToZeroScalar:
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
        case NI_X86Base_ShiftLeftLogical:
        case NI_X86Base_ShiftRightArithmetic:
        case NI_X86Base_ShiftRightLogical:
        case NI_AVX2_ShiftLeftLogical:
        case NI_AVX2_ShiftRightArithmetic:
        case NI_AVX2_ShiftRightLogical:
        case NI_AVX512_ShiftLeftLogical:
        case NI_AVX512_ShiftRightArithmetic:
        case NI_AVX512_ShiftRightLogical:
        {
            // These intrinsics have overloads that take op2 in a simd register and just read the lowest 8-bits

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack();

            GenTree* tmpOp = gtNewSimdCreateScalarNode(TYP_SIMD16, op2, CORINFO_TYPE_INT, 16);
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, simdBaseJitType, genTypeSize(simdType));
        }

        case NI_AVX512_RotateLeft:
        case NI_AVX512_RotateRight:
        {
            var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

            // These intrinsics have variants that take op2 in a simd register and read a unique shift per element
            intrinsic = static_cast<NamedIntrinsic>(intrinsic + 1);

            static_assert_no_msg(NI_AVX512_RotateLeftVariable == (NI_AVX512_RotateLeft + 1));
            static_assert_no_msg(NI_AVX512_RotateRightVariable == (NI_AVX512_RotateRight + 1));

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

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
//    entryPoint      -- The entry point information required for R2R scenarios
//    simdBaseJitType -- generic argument of the intrinsic.
//    retType         -- return type of the intrinsic.
//    mustExpand      -- true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    the expanded intrinsic.
//
// Assumptions:
//    For Vector### methods, attempted intrinsic expansion implies
//    baseline ISA requirements have been met, as follows:
//      Vector128: SSE2
//      Vector256: AVX (note that AVX2 cannot be assumed)
//      Vector512: AVX-512F+CD+DQ+BW+VL
//    For hardware ISA classes, attempted expansion means the ISA
//    is explicitly supported.
//
GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO* sig R2RARG(CORINFO_CONST_LOOKUP* entryPoint),
                                       CorInfoType           simdBaseJitType,
                                       var_types             retType,
                                       unsigned              simdSize,
                                       bool                  mustExpand)
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

#if defined(FEATURE_READYTORUN)
    CORINFO_CONST_LOOKUP emptyEntryPoint;

    emptyEntryPoint.addr       = nullptr;
    emptyEntryPoint.accessType = IAT_VALUE;
#endif // FEATURE_READYTORUN

    bool isMinMaxIntrinsic = false;
    bool isMax             = false;
    bool isMagnitude       = false;
    bool isNative          = false;
    bool isNumber          = false;

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

        case NI_AVX2_AndNot:
        {
            if (varTypeIsSIMD(retType))
            {
                intrinsic             = NI_AVX2_AndNotVector;
                simdSize              = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
                simdBaseType          = JitType2PreciseVarType(simdBaseJitType);
                compFloatingPointUsed = true;
            }
            else
            {
                intrinsic = NI_AVX2_AndNotScalar;
            }
            FALLTHROUGH;
        }

        case NI_X86Base_AndNot:
        case NI_AVX_AndNot:
        case NI_AVX2_X64_AndNot:
        case NI_AVX512_AndNot:
        {
            assert(sig->numArgs == 2);

            if (simdBaseType != TYP_UNKNOWN)
            {
                // We don't want to support creating AND_NOT nodes prior to LIR
                // as it can break important optimizations. We'll produces this
                // in lowering instead so decompose into the individual operations
                // on import, taking into account that despite the name, these APIs
                // do (~op1 & op2), so we need to account for that

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                op1     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize));
                retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
            }
            else
            {
                // The same general reasoning for the decomposition exists here as
                // given above for the SIMD AndNot APIs.

                op2 = impPopStack().val;
                op1 = impPopStack().val;

                op1     = gtFoldExpr(gtNewOperNode(GT_NOT, retType, op1));
                retNode = gtNewOperNode(GT_AND, retType, op1, op2);
            }
            break;
        }

        case NI_Vector128_AddSaturate:
        case NI_Vector256_AddSaturate:
        case NI_Vector512_AddSaturate:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                if (varTypeIsFloating(simdBaseType))
                {
                    retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
                }
                else if (varTypeIsSmall(simdBaseType))
                {
                    if (simdSize == 64)
                    {
                        intrinsic = NI_AVX512_AddSaturate;
                    }
                    else if (simdSize == 32)
                    {
                        intrinsic = NI_AVX2_AddSaturate;
                    }
                    else
                    {
                        assert(simdSize == 16);
                        intrinsic = NI_X86Base_AddSaturate;
                    }

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
                }
                else if (varTypeIsUnsigned(simdBaseType))
                {
                    // For unsigned we simply have to detect `(x + y) < x`
                    // and in that scenario return MaxValue (AllBitsSet)

                    GenTree* cns     = gtNewAllBitsSetConNode(retType);
                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);

                    GenTree* tmp     = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* msk     = gtNewSimdCmpOpNode(GT_LT, retType, tmp, op1Dup1, simdBaseJitType, simdSize);

                    retNode = gtNewSimdCndSelNode(retType, msk, cns, tmpDup1, simdBaseJitType, simdSize);
                }
                else
                {
                    // For signed the logic is a bit more complex, but is
                    // explained on the managed side as part of Scalar<T>.AddSaturate

                    GenTreeVecCon* minCns = gtNewVconNode(retType);
                    GenTreeVecCon* maxCns = gtNewVconNode(retType);

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        {
                            minCns->EvaluateBroadcastInPlace<int16_t>(INT16_MIN);
                            maxCns->EvaluateBroadcastInPlace<int16_t>(INT16_MAX);
                            break;
                        }

                        case TYP_INT:
                        {
                            minCns->EvaluateBroadcastInPlace<int32_t>(INT32_MIN);
                            maxCns->EvaluateBroadcastInPlace<int32_t>(INT32_MAX);
                            break;
                        }

                        case TYP_LONG:
                        {
                            minCns->EvaluateBroadcastInPlace<int64_t>(INT64_MIN);
                            maxCns->EvaluateBroadcastInPlace<int64_t>(INT64_MAX);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);

                    GenTree* tmp = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);

                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* tmpDup2 = gtCloneExpr(tmpDup1);

                    GenTree* msk = gtNewSimdIsNegativeNode(retType, tmpDup1, simdBaseJitType, simdSize);
                    GenTree* ovf = gtNewSimdCndSelNode(retType, msk, maxCns, minCns, simdBaseJitType, simdSize);

                    // The mask we need is ((a ^ b) & ~(b ^ c)) < 0

                    if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
                    {
                        // tmpDup1 = a: 0xF0
                        // op1Dup1 = b: 0xCC
                        // op2Dup2 = c: 0xAA
                        //
                        // 0x18    = A ? norBC : andBC
                        //           a ? ~(b | c) : (b & c)
                        msk = gtNewSimdTernaryLogicNode(retType, tmp, op1Dup1, op2Dup1, gtNewIconNode(0x18),
                                                        simdBaseJitType, simdSize);
                    }
                    else
                    {
                        GenTree* op1Dup2 = gtCloneExpr(op1Dup1);

                        GenTree* msk2 = gtNewSimdBinOpNode(GT_XOR, retType, tmp, op1Dup1, simdBaseJitType, simdSize);
                        GenTree* msk3 =
                            gtNewSimdBinOpNode(GT_XOR, retType, op1Dup2, op2Dup1, simdBaseJitType, simdSize);

                        msk = gtNewSimdBinOpNode(GT_AND_NOT, retType, msk2, msk3, simdBaseJitType, simdSize);
                    }

                    msk     = gtNewSimdIsNegativeNode(retType, msk, simdBaseJitType, simdSize);
                    retNode = gtNewSimdCndSelNode(retType, msk, ovf, tmpDup2, simdBaseJitType, simdSize);
                }
            }
            break;
        }

        case NI_Vector128_AndNot:
        case NI_Vector256_AndNot:
        case NI_Vector512_AndNot:
        {
            assert(sig->numArgs == 2);

            // We don't want to support creating AND_NOT nodes prior to LIR
            // as it can break important optimizations. We'll produces this
            // in lowering instead so decompose into the individual operations
            // on import

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op2     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op2, simdBaseJitType, simdSize));
            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
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

                    op1 = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector128_AsVector128Unsafe, simdBaseJitType, 8);

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

                    op1 = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector128_AsVector128Unsafe, simdBaseJitType, 12);

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
                case TYP_SIMD64:
                {
                    // Vector<T> is larger, so we should treat this as a call to the appropriate narrowing intrinsic
                    intrinsic = simdSize == YMM_REGSIZE_BYTES ? NI_Vector256_GetLower : NI_Vector512_GetLower128;

                    op1     = impSIMDPopStack();
                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            break;
        }

        case NI_Vector128_AsVector128Unsafe:
        {
            assert(sig->numArgs == 1);
            assert(retType == TYP_SIMD16);
            assert(simdBaseJitType == CORINFO_TYPE_FLOAT);
            assert((simdSize == 8) || (simdSize == 12));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector128_AsVector128Unsafe, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_AsVector:
        case NI_Vector256_AsVector:
        case NI_Vector512_AsVector:
        case NI_Vector256_AsVector256:
        case NI_Vector512_AsVector512:
        {
            assert(sig->numArgs == 1);
            uint32_t vectorTByteLength = getVectorTByteLength();

            if (vectorTByteLength == 0)
            {
                // VectorT ISA was not present. Fall back to managed.
                break;
            }

            if (vectorTByteLength == simdSize)
            {
                // We fold away the cast here, as it only exists to satisfy
                // the type system. It is safe to do this here since the retNode type
                // and the signature return type are both the same TYP_SIMD.

                retNode = impSIMDPopStack();
                SetOpLclRelatedToSIMDIntrinsic(retNode);
                assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                break;
            }

            // Vector<T> is a different size than the source/target SIMD type, so we should
            // treat this as a call to the appropriate narrowing or widening intrinsic.

            NamedIntrinsic convertIntrinsic = NI_Illegal;

            switch (vectorTByteLength)
            {
                case XMM_REGSIZE_BYTES:
                {
                    switch (intrinsic)
                    {
                        case NI_Vector256_AsVector:
                            convertIntrinsic = NI_Vector256_GetLower;
                            break;
                        case NI_Vector512_AsVector:
                            convertIntrinsic = NI_Vector512_GetLower128;
                            break;
                        case NI_Vector256_AsVector256:
                            convertIntrinsic = NI_Vector128_ToVector256;
                            break;
                        case NI_Vector512_AsVector512:
                            convertIntrinsic = NI_Vector128_ToVector512;
                            break;
                        default:
                            unreached();
                    }
                    break;
                }

                case YMM_REGSIZE_BYTES:
                {
                    switch (intrinsic)
                    {
                        case NI_Vector128_AsVector:
                            convertIntrinsic = NI_Vector128_ToVector256;
                            break;
                        case NI_Vector512_AsVector:
                            convertIntrinsic = NI_Vector512_GetLower;
                            break;
                        case NI_Vector512_AsVector512:
                            convertIntrinsic = NI_Vector256_ToVector512;
                            break;
                        default:
                            unreached();
                    }
                    break;
                }

                case ZMM_REGSIZE_BYTES:
                {
                    switch (intrinsic)
                    {
                        case NI_Vector128_AsVector:
                            convertIntrinsic = NI_Vector128_ToVector512;
                            break;
                        case NI_Vector256_AsVector:
                            convertIntrinsic = NI_Vector256_ToVector512;
                            break;
                        case NI_Vector256_AsVector256:
                            convertIntrinsic = NI_Vector512_GetLower;
                            break;
                        default:
                            unreached();
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            unsigned convertSize = simdSize;
            bool     sizeFound   = HWIntrinsicInfo::tryLookupSimdSize(convertIntrinsic, &convertSize);
            assert(sizeFound);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, convertIntrinsic, simdBaseJitType, convertSize);

            break;
        }

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

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
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
        case NI_Vector512_ConvertToDouble:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsLong(simdBaseType));

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                if (simdSize == 64)
                {
                    intrinsic = NI_AVX512_ConvertToVector512Double;
                }
                else if (simdSize == 32)
                {
                    intrinsic = NI_AVX512_ConvertToVector256Double;
                }
                else
                {
                    assert(simdSize == 16);
                    intrinsic = NI_AVX512_ConvertToVector128Double;
                }
                op1     = impSIMDPopStack();
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToInt32:
        case NI_Vector256_ConvertToInt32:
        case NI_Vector512_ConvertToInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_INT, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToInt32Native:
        case NI_Vector256_ConvertToInt32Native:
        case NI_Vector512_ConvertToInt32Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_INT, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ConvertToInt64:
        case NI_Vector256_ConvertToInt64:
        case NI_Vector512_ConvertToInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_LONG, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToInt64Native:
        case NI_Vector256_ConvertToInt64Native:
        case NI_Vector512_ConvertToInt64Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_LONG, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToSingle:
        case NI_Vector256_ConvertToSingle:
        case NI_Vector512_ConvertToSingle:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsInt(simdBaseType));
            intrinsic = NI_Illegal;
            if (simdBaseType == TYP_INT)
            {
                switch (simdSize)
                {
                    case 16:
                        intrinsic = NI_X86Base_ConvertToVector128Single;
                        break;
                    case 32:
                        intrinsic = NI_AVX_ConvertToVector256Single;
                        break;
                    case 64:
                        intrinsic = NI_AVX512_ConvertToVector512Single;
                        break;
                    default:
                        unreached();
                }
            }
            else if (simdBaseType == TYP_UINT && compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                switch (simdSize)
                {
                    case 16:
                        intrinsic = NI_AVX512_ConvertToVector128Single;
                        break;
                    case 32:
                        intrinsic = NI_AVX512_ConvertToVector256Single;
                        break;
                    case 64:
                        intrinsic = NI_AVX512_ConvertToVector512Single;
                        break;
                    default:
                        unreached();
                }
            }
            if (intrinsic != NI_Illegal)
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToUInt32:
        case NI_Vector256_ConvertToUInt32:
        case NI_Vector512_ConvertToUInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_UINT, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToUInt32Native:
        case NI_Vector256_ConvertToUInt32Native:
        case NI_Vector512_ConvertToUInt32Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_UINT, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToUInt64:
        case NI_Vector256_ConvertToUInt64:
        case NI_Vector512_ConvertToUInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_ULONG, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ConvertToUInt64Native:
        case NI_Vector256_ConvertToUInt64Native:
        case NI_Vector512_ConvertToUInt64Native:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_ULONG, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Create:
        case NI_Vector256_Create:
        case NI_Vector512_Create:
        {
            if (sig->numArgs == 1)
            {
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

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        case NI_Vector512_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

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
            }

            impSpillSideEffect(true, stackState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for vector CreateSequence"));

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateSequenceNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_op_Division:
        case NI_Vector256_op_Division:
        case NI_Vector512_op_Division:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsFloating(simdBaseType))
            {
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
                // Check to see if it is possible to emulate the integer division
                if (!(simdBaseType == TYP_INT &&
                      ((simdSize == 16 && compOpportunisticallyDependsOn(InstructionSet_AVX)) ||
                       (simdSize == 32 && compOpportunisticallyDependsOn(InstructionSet_AVX512)))))
                {
                    break;
                }
                impSpillSideEffect(true, stackState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for vector integer division"));
#else
                break;
#endif // defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
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
        case NI_Vector512_Dot:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            if ((simdSize == 32) && !varTypeIsFloating(simdBaseType) &&
                !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                break;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if ((simdSize == 64) || varTypeIsByte(simdBaseType) || varTypeIsLong(simdBaseType) ||
                (varTypeIsInt(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_SSE42)))
            {
                // The lowering for Dot doesn't handle these cases, so import as Sum(left * right)
                retNode = gtNewSimdBinOpNode(GT_MUL, simdType, op1, op2, simdBaseJitType, simdSize);
                retNode = gtNewSimdSumNode(retType, retNode, simdBaseJitType, simdSize);
                break;
            }

            retNode = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
            retNode = gtNewSimdToScalarNode(retType, retNode, simdBaseJitType, simdSize);
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

        case NI_Vector128_ExtractMostSignificantBits:
        case NI_Vector256_ExtractMostSignificantBits:
        case NI_Vector512_ExtractMostSignificantBits:
        case NI_AVX512_MoveMask:
        {
            assert(sig->numArgs == 1);

            if ((simdSize == 64) || (varTypeIsShort(simdBaseType) && canUseEvexEncoding()))
            {
                intrinsic = NI_AVX512_MoveMask;
            }

            if (intrinsic == NI_AVX512_MoveMask)
            {
                op1 = impSIMDPopStack();

                op1     = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op1, simdBaseJitType, simdSize);
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
                break;
            }

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
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX2_MoveMask : NI_X86Base_MoveMask;
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
                            moveMaskIntrinsic = NI_X86Base_MoveMask;
                        }
                        else if (compOpportunisticallyDependsOn(InstructionSet_SSE42))
                        {
                            shuffleIntrinsic  = NI_SSE42_Shuffle;
                            moveMaskIntrinsic = NI_X86Base_MoveMask;
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
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
                        break;
                    }

                    case TYP_LONG:
                    case TYP_ULONG:
                    case TYP_DOUBLE:
                    {
                        simdBaseJitType   = CORINFO_TYPE_DOUBLE;
                        op1               = impSIMDPopStack();
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
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

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_FusedMultiplyAdd:
        case NI_Vector256_FusedMultiplyAdd:
        case NI_Vector512_FusedMultiplyAdd:
        {
            assert(sig->numArgs == 3);
            assert(varTypeIsFloating(simdBaseType));

            if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op3 = impSIMDPopStack();
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
            }
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
                    if (!op2->IsIntegralConst(0) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
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
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsEvenInteger:
        case NI_Vector256_IsEvenInteger:
        case NI_Vector512_IsEvenInteger:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                // The code for handling floating-point is decently complex but also expected
                // to be rare, so we fallback to the managed implementation, which is accelerated
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsEvenIntegerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_IsFinite:
        case NI_Vector256_IsFinite:
        case NI_Vector512_IsFinite:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsFiniteNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsInfinity:
        case NI_Vector256_IsInfinity:
        case NI_Vector512_IsInfinity:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsInfinityNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsInteger:
        case NI_Vector256_IsInteger:
        case NI_Vector512_IsInteger:
        {
            assert(sig->numArgs == 1);

            if ((simdSize == 16) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                break;
            }
            if ((simdSize == 32) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsIntegerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_IsNaN:
        case NI_Vector256_IsNaN:
        case NI_Vector512_IsNaN:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsNaNNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_IsNegative:
        case NI_Vector256_IsNegative:
        case NI_Vector512_IsNegative:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsNegativeNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsNegativeInfinity:
        case NI_Vector256_IsNegativeInfinity:
        case NI_Vector512_IsNegativeInfinity:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsNegativeInfinityNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsNormal:
        case NI_Vector256_IsNormal:
        case NI_Vector512_IsNormal:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsNormalNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsOddInteger:
        case NI_Vector256_IsOddInteger:
        case NI_Vector512_IsOddInteger:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                // The code for handling floating-point is decently complex but also expected
                // to be rare, so we fallback to the managed implementation, which is accelerated
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsOddIntegerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_IsPositive:
        case NI_Vector256_IsPositive:
        case NI_Vector512_IsPositive:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsPositiveNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsPositiveInfinity:
        case NI_Vector256_IsPositiveInfinity:
        case NI_Vector512_IsPositiveInfinity:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsPositiveInfinityNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsSubnormal:
        case NI_Vector256_IsSubnormal:
        case NI_Vector512_IsSubnormal:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdIsSubnormalNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_IsZero:
        case NI_Vector256_IsZero:
        case NI_Vector512_IsZero:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdIsZeroNode(retType, op1, simdBaseJitType, simdSize);
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
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_X86Base_LoadVector128:
        case NI_AVX_LoadVector256:
        case NI_AVX512_LoadVector512:
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
            isMinMaxIntrinsic = true;
            isMax             = true;
            break;
        }

        case NI_Vector128_MaxMagnitude:
        case NI_Vector256_MaxMagnitude:
        case NI_Vector512_MaxMagnitude:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isMagnitude       = true;
            break;
        }

        case NI_Vector128_MaxMagnitudeNumber:
        case NI_Vector256_MaxMagnitudeNumber:
        case NI_Vector512_MaxMagnitudeNumber:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isMagnitude       = true;
            isNumber          = true;
            break;
        }

        case NI_Vector128_MaxNative:
        case NI_Vector256_MaxNative:
        case NI_Vector512_MaxNative:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isNative          = true;
            break;
        }

        case NI_Vector128_MaxNumber:
        case NI_Vector256_MaxNumber:
        case NI_Vector512_MaxNumber:
        {
            isMinMaxIntrinsic = true;
            isMax             = true;
            isNumber          = true;
            break;
        }

        case NI_Vector128_Min:
        case NI_Vector256_Min:
        case NI_Vector512_Min:
        {
            isMinMaxIntrinsic = true;
            break;
        }

        case NI_Vector128_MinMagnitude:
        case NI_Vector256_MinMagnitude:
        case NI_Vector512_MinMagnitude:
        {
            isMinMaxIntrinsic = true;
            isMagnitude       = true;
            break;
        }

        case NI_Vector128_MinMagnitudeNumber:
        case NI_Vector256_MinMagnitudeNumber:
        case NI_Vector512_MinMagnitudeNumber:
        {
            isMinMaxIntrinsic = true;
            isMagnitude       = true;
            isNumber          = true;
            break;
        }

        case NI_Vector128_MinNative:
        case NI_Vector256_MinNative:
        case NI_Vector512_MinNative:
        {
            isMinMaxIntrinsic = true;
            isNative          = true;
            break;
        }

        case NI_Vector128_MinNumber:
        case NI_Vector256_MinNumber:
        case NI_Vector512_MinNumber:
        {
            isMinMaxIntrinsic = true;
            isNumber          = true;
            break;
        }

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

        case NI_Vector128_MultiplyAddEstimate:
        case NI_Vector256_MultiplyAddEstimate:
        case NI_Vector512_MultiplyAddEstimate:
        {
            assert(sig->numArgs == 3);

            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            if ((simdSize == 32) && !varTypeIsFloating(simdBaseType) &&
                !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                break;
            }

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (varTypeIsFloating(simdBaseType) && compExactlyDependsOn(InstructionSet_AVX2))
            {
                retNode = gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
            }
            else
            {
                GenTree* mulNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
                retNode          = gtNewSimdBinOpNode(GT_ADD, retType, mulNode, op3, simdBaseJitType, simdSize);
            }
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
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_NarrowWithSaturation:
        case NI_Vector256_NarrowWithSaturation:
        case NI_Vector512_NarrowWithSaturation:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                if (simdBaseType == TYP_DOUBLE)
                {
                    // gtNewSimdNarrowNode uses the base type of the return for the simdBaseType
                    retNode = gtNewSimdNarrowNode(retType, op1, op2, CORINFO_TYPE_FLOAT, simdSize);
                }
                else if ((simdSize == 16) && ((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_INT)))
                {
                    // PackSignedSaturate uses the base type of the return for the simdBaseType
                    simdBaseJitType = (simdBaseType == TYP_SHORT) ? CORINFO_TYPE_BYTE : CORINFO_TYPE_SHORT;

                    intrinsic = NI_X86Base_PackSignedSaturate;
                    retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
                {
                    if ((simdSize == 32) || (simdSize == 64))
                    {
                        if (simdSize == 32)
                        {
                            intrinsic = NI_Vector256_ToVector512Unsafe;

                            op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD64, op1, intrinsic, simdBaseJitType, simdSize);
                            op1 = gtNewSimdWithUpperNode(TYP_SIMD64, op1, op2, simdBaseJitType, simdSize * 2);
                        }

                        switch (simdBaseType)
                        {
                            case TYP_SHORT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256SByteWithSaturation;
                                break;
                            }

                            case TYP_USHORT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256ByteWithSaturation;
                                break;
                            }

                            case TYP_INT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256Int16WithSaturation;
                                break;
                            }

                            case TYP_UINT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256UInt16WithSaturation;
                                break;
                            }

                            case TYP_LONG:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256Int32WithSaturation;
                                break;
                            }

                            case TYP_ULONG:
                            {
                                intrinsic = NI_AVX512_ConvertToVector256UInt32WithSaturation;
                                break;
                            }

                            default:
                            {
                                unreached();
                            }
                        }
                    }
                    else
                    {
                        assert(simdSize == 16);
                        intrinsic = NI_Vector128_ToVector256Unsafe;

                        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, intrinsic, simdBaseJitType, simdSize);
                        op1 = gtNewSimdWithUpperNode(TYP_SIMD32, op1, op2, simdBaseJitType, simdSize * 2);

                        switch (simdBaseType)
                        {
                            case TYP_USHORT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector128ByteWithSaturation;
                                break;
                            }

                            case TYP_UINT:
                            {
                                intrinsic = NI_AVX512_ConvertToVector128UInt16WithSaturation;
                                break;
                            }

                            case TYP_LONG:
                            {
                                intrinsic = NI_AVX512_ConvertToVector128Int32WithSaturation;
                                break;
                            }

                            case TYP_ULONG:
                            {
                                intrinsic = NI_AVX512_ConvertToVector128UInt32WithSaturation;
                                break;
                            }

                            default:
                            {
                                unreached();
                            }
                        }
                    }

                    if (simdSize == 64)
                    {
                        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, intrinsic, simdBaseJitType, simdSize);
                        op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op2, intrinsic, simdBaseJitType, simdSize);

                        retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseJitType, simdSize);
                    }
                    else
                    {
                        retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize * 2);
                    }
                }
                else
                {
                    // gtNewSimdNarrowNode uses the base type of the return for the simdBaseType
                    CorInfoType narrowSimdBaseJitType;

                    GenTreeVecCon* minCns = varTypeIsSigned(simdBaseType) ? gtNewVconNode(retType) : nullptr;
                    GenTreeVecCon* maxCns = gtNewVconNode(retType);

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        {
                            minCns->EvaluateBroadcastInPlace<int16_t>(INT8_MIN);
                            maxCns->EvaluateBroadcastInPlace<int16_t>(INT8_MAX);

                            narrowSimdBaseJitType = CORINFO_TYPE_BYTE;
                            break;
                        }

                        case TYP_USHORT:
                        {
                            maxCns->EvaluateBroadcastInPlace<uint16_t>(UINT8_MAX);
                            narrowSimdBaseJitType = CORINFO_TYPE_UBYTE;
                            break;
                        }

                        case TYP_INT:
                        {
                            minCns->EvaluateBroadcastInPlace<int32_t>(INT16_MIN);
                            maxCns->EvaluateBroadcastInPlace<int32_t>(INT16_MAX);

                            narrowSimdBaseJitType = CORINFO_TYPE_SHORT;
                            break;
                        }

                        case TYP_UINT:
                        {
                            maxCns->EvaluateBroadcastInPlace<uint32_t>(UINT16_MAX);
                            narrowSimdBaseJitType = CORINFO_TYPE_USHORT;
                            break;
                        }

                        case TYP_LONG:
                        {
                            minCns->EvaluateBroadcastInPlace<int64_t>(INT32_MIN);
                            maxCns->EvaluateBroadcastInPlace<int64_t>(INT32_MAX);

                            narrowSimdBaseJitType = CORINFO_TYPE_INT;
                            break;
                        }

                        case TYP_ULONG:
                        {
                            maxCns->EvaluateBroadcastInPlace<uint64_t>(UINT32_MAX);
                            narrowSimdBaseJitType = CORINFO_TYPE_UINT;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    // This does a clamp which is defined as: Min(Max(value, min), max)
                    // which means that we do a max computation if a minimum constant is specified
                    // There will be none specified for unsigned to unsigned narrowing since
                    // they share a lower bound (0) and will already be correct.

                    if (minCns != nullptr)
                    {
                        op1 = gtNewSimdMinMaxNode(retType, op1, minCns, simdBaseJitType, simdSize, /* isMax */ true,
                                                  /* isMagnitude */ false, /* isNumber */ false);
                        op2 = gtNewSimdMinMaxNode(retType, op2, gtCloneExpr(minCns), simdBaseJitType, simdSize,
                                                  /* isMax */ true, /* isMagnitude */ false, /* isNumber */ false);
                    }

                    op1 = gtNewSimdMinMaxNode(retType, op1, maxCns, simdBaseJitType, simdSize, /* isMax */ false,
                                              /* isMagnitude */ false, /* isNumber */ false);
                    op2 = gtNewSimdMinMaxNode(retType, op2, gtCloneExpr(maxCns), simdBaseJitType, simdSize,
                                              /* isMax */ false, /* isMagnitude */ false, /* isNumber */ false);

                    retNode = gtNewSimdNarrowNode(retType, op1, op2, narrowSimdBaseJitType, simdSize);
                }
            }
            break;
        }

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
        case NI_Vector512_op_Inequality:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
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

        case NI_Vector128_op_LeftShift:
        case NI_Vector256_op_LeftShift:
        case NI_Vector512_op_LeftShift:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_op_RightShift:
        case NI_Vector256_op_RightShift:
        case NI_Vector512_op_RightShift:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;

                op2 = impPopStack().val;
                op1 = impSIMDPopStack();

                retNode = gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

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

        case NI_Vector128_Round:
        case NI_Vector256_Round:
        case NI_Vector512_Round:
        {
            if (sig->numArgs != 1)
            {
                break;
            }

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdRoundNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ShiftLeft:
        case NI_Vector256_ShiftLeft:
        case NI_Vector512_ShiftLeft:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsSIMD(impStackTop(0).val))
            {
                // We just want the inlining profitability boost for the helper intrinsics/
                // that have operator alternatives like `simd << int`
                break;
            }

            if ((simdSize != 16) || compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                if (simdSize == 64)
                {
                    intrinsic = NI_AVX512_ShiftLeftLogicalVariable;
                }
                else
                {
                    assert((simdSize == 16) || (simdSize == 32));
                    intrinsic = NI_AVX2_ShiftLeftLogicalVariable;
                }

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_Shuffle:
        case NI_Vector256_Shuffle:
        case NI_Vector512_Shuffle:
        case NI_Vector128_ShuffleNative:
        case NI_Vector256_ShuffleNative:
        case NI_Vector512_ShuffleNative:
        case NI_Vector128_ShuffleNativeFallback:
        case NI_Vector256_ShuffleNativeFallback:
        case NI_Vector512_ShuffleNativeFallback:
        {
            assert((sig->numArgs == 2) || (sig->numArgs == 3));

            // The Native variants are non-deterministic on xarch
            bool isShuffleNative = (intrinsic != NI_Vector128_Shuffle) && (intrinsic != NI_Vector256_Shuffle) &&
                                   (intrinsic != NI_Vector512_Shuffle);
            if (isShuffleNative && BlockNonDeterministicIntrinsics(mustExpand))
            {
                break;
            }

            GenTree* indices = impStackTop(0).val;

            // Check if the required intrinsics are available to emit now (validForShuffle). If we have variable
            // indices that might become possible to emit later (due to them becoming constant), this will be
            // indicated in canBecomeValidForShuffle; otherwise, it's just the same as validForShuffle.
            bool canBecomeValidForShuffle = false;
            bool validForShuffle =
                IsValidForShuffle(indices, simdSize, simdBaseType, &canBecomeValidForShuffle, isShuffleNative);

            // If it isn't valid for shuffle (and can't become valid later), then give up now.
            if (!canBecomeValidForShuffle)
            {
                return nullptr;
            }

            // If the indices might become constant later, then we don't emit for now, delay until later.
            if ((!validForShuffle) || (!indices->IsCnsVec()))
            {
                assert(sig->numArgs == 2);

                if (opts.OptimizationEnabled())
                {
                    // Only enable late stage rewriting if optimizations are enabled
                    // as we won't otherwise encounter a constant at the later point
                    op2 = impSIMDPopStack();
                    op1 = impSIMDPopStack();

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);

                    retNode->AsHWIntrinsic()->SetMethodHandle(this, method R2RARG(*entryPoint));
                    break;
                }

                // If we're not doing late stage rewriting, just return null now as it won't become valid.
                if (!validForShuffle)
                {
                    return nullptr;
                }
            }

            if (sig->numArgs == 2)
            {
                op2     = impSIMDPopStack();
                op1     = impSIMDPopStack();
                retNode = gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize, isShuffleNative);
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

        case NI_X86Base_Store:
        case NI_AVX_Store:
        case NI_AVX512_Store:
        {
            assert(retType == TYP_VOID);
            assert(sig->numArgs == 2);

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

        case NI_Vector128_StoreUnsafe:
        case NI_Vector256_StoreUnsafe:
        case NI_Vector512_StoreUnsafe:
        {
            assert(retType == TYP_VOID);

            if (sig->numArgs == 3)
            {
                impSpillSideEffect(true,
                                   stackState.esStackDepth - 3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

                op3 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 2);

                impSpillSideEffect(true,
                                   stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
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

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

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

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

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

        case NI_Vector128_SubtractSaturate:
        case NI_Vector256_SubtractSaturate:
        case NI_Vector512_SubtractSaturate:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                if (varTypeIsFloating(simdBaseType))
                {
                    retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);
                }
                else if (varTypeIsSmall(simdBaseType))
                {
                    if (simdSize == 64)
                    {
                        intrinsic = NI_AVX512_SubtractSaturate;
                    }
                    else if (simdSize == 32)
                    {
                        intrinsic = NI_AVX2_SubtractSaturate;
                    }
                    else
                    {
                        assert(simdSize == 16);
                        intrinsic = NI_X86Base_SubtractSaturate;
                    }

                    retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
                }
                else if (varTypeIsUnsigned(simdBaseType))
                {
                    // For unsigned we simply have to detect `(x - y) > x`
                    // and in that scenario return MinValue (Zero)

                    GenTree* cns     = gtNewZeroConNode(retType);
                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);

                    GenTree* tmp     = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);
                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* msk     = gtNewSimdCmpOpNode(GT_GT, retType, tmp, op1Dup1, simdBaseJitType, simdSize);

                    retNode = gtNewSimdCndSelNode(retType, msk, cns, tmpDup1, simdBaseJitType, simdSize);
                }
                else
                {
                    // For signed the logic is a bit more complex, but is
                    // explained on the managed side as part of Scalar<T>.SubtractSaturate

                    GenTreeVecCon* minCns = gtNewVconNode(retType);
                    GenTreeVecCon* maxCns = gtNewVconNode(retType);

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        {
                            minCns->EvaluateBroadcastInPlace<int16_t>(INT16_MIN);
                            maxCns->EvaluateBroadcastInPlace<int16_t>(INT16_MAX);
                            break;
                        }

                        case TYP_INT:
                        {
                            minCns->EvaluateBroadcastInPlace<int32_t>(INT32_MIN);
                            maxCns->EvaluateBroadcastInPlace<int32_t>(INT32_MAX);
                            break;
                        }

                        case TYP_LONG:
                        {
                            minCns->EvaluateBroadcastInPlace<int64_t>(INT64_MIN);
                            maxCns->EvaluateBroadcastInPlace<int64_t>(INT64_MAX);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);

                    GenTree* tmp = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);

                    GenTree* tmpDup1 = fgMakeMultiUse(&tmp);
                    GenTree* tmpDup2 = gtCloneExpr(tmpDup1);

                    GenTree* msk = gtNewSimdIsNegativeNode(retType, tmpDup1, simdBaseJitType, simdSize);
                    GenTree* ovf = gtNewSimdCndSelNode(retType, msk, maxCns, minCns, simdBaseJitType, simdSize);

                    // The mask we need is ((a ^ b) & (b ^ c)) < 0

                    if (compOpportunisticallyDependsOn(InstructionSet_AVX512))
                    {
                        // tmpDup1 = a: 0xF0
                        // op1Dup1 = b: 0xCC
                        // op2Dup2 = c: 0xAA
                        //
                        // 0x18    = B ? norAC : andAC
                        //           b ? ~(a | c) : (a & c)
                        msk = gtNewSimdTernaryLogicNode(retType, tmp, op1Dup1, op2Dup1, gtNewIconNode(0x24),
                                                        simdBaseJitType, simdSize);
                    }
                    else
                    {
                        GenTree* op1Dup2 = gtCloneExpr(op1Dup1);

                        GenTree* msk2 = gtNewSimdBinOpNode(GT_XOR, retType, tmp, op1Dup1, simdBaseJitType, simdSize);
                        GenTree* msk3 =
                            gtNewSimdBinOpNode(GT_XOR, retType, op1Dup2, op2Dup1, simdBaseJitType, simdSize);

                        msk = gtNewSimdBinOpNode(GT_AND, retType, msk2, msk3, simdBaseJitType, simdSize);
                    }

                    msk     = gtNewSimdIsNegativeNode(retType, msk, simdBaseJitType, simdSize);
                    retNode = gtNewSimdCndSelNode(retType, msk, ovf, tmpDup2, simdBaseJitType, simdSize);
                }
            }
            break;
        }

        case NI_Vector128_Sum:
        case NI_Vector256_Sum:
        case NI_Vector512_Sum:
        {
            assert(sig->numArgs == 1);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        {
            assert(sig->numArgs == 1);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdToScalarNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Truncate:
        case NI_Vector256_Truncate:
        case NI_Vector512_Truncate:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            if ((simdSize < 32) && !compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdTruncNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_GetUpper:
        case NI_Vector512_GetUpper:
        {
            assert(sig->numArgs == 1);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetUpperNode(retType, op1, simdBaseJitType, simdSize);
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
                assert((simdSize != 64) || compIsaSupportedDebugOnly(InstructionSet_AVX512));

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

            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE42))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_LONG:
                case TYP_ULONG:
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE42_X64))
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
        case NI_Vector512_WithLower:
        {
            assert(sig->numArgs == 2);

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithLowerNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector256_WithUpper:
        case NI_Vector512_WithUpper:
        {
            assert(sig->numArgs == 2);

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

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

        case NI_X86Base_CompareScalarGreaterThan:
        case NI_X86Base_CompareScalarGreaterThanOrEqual:
        case NI_X86Base_CompareScalarNotGreaterThan:
        case NI_X86Base_CompareScalarNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            bool supportsAvx = compOpportunisticallyDependsOn(InstructionSet_AVX);

            if (!supportsAvx)
            {
                impSpillSideEffect(true,
                                   stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();
            assert(varTypeIsFloating(JitType2PreciseVarType(simdBaseJitType)));

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
                                                  nullptr DEBUGARG("Clone op1 for CompareScalarGreaterThan"));

                switch (intrinsic)
                {
                    case NI_X86Base_CompareScalarGreaterThan:
                    {
                        intrinsic = NI_X86Base_CompareScalarLessThan;
                        break;
                    }

                    case NI_X86Base_CompareScalarGreaterThanOrEqual:
                    {
                        intrinsic = NI_X86Base_CompareScalarLessThanOrEqual;
                        break;
                    }

                    case NI_X86Base_CompareScalarNotGreaterThan:
                    {
                        intrinsic = NI_X86Base_CompareScalarNotLessThan;
                        break;
                    }

                    case NI_X86Base_CompareScalarNotGreaterThanOrEqual:
                    {
                        intrinsic = NI_X86Base_CompareScalarNotLessThanOrEqual;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, intrinsic, simdBaseJitType, simdSize);
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, clonedOp1, retNode, NI_X86Base_MoveScalar,
                                                   simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_X86Base_Prefetch0:
        case NI_X86Base_Prefetch1:
        case NI_X86Base_Prefetch2:
        case NI_X86Base_PrefetchNonTemporal:
        {
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, intrinsic, CORINFO_TYPE_UBYTE, 0);
            break;
        }

        case NI_X86Base_StoreFence:
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;

        case NI_X86Base_LoadFence:
        case NI_X86Base_MemoryFence:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;
        }

        case NI_X86Base_StoreNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(JITtype2varType(sig->retType) == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE argList = info.compCompHnd->getArgNext(sig->args);
            CORINFO_CLASS_HANDLE    argClass;
            CorInfoType             argJitType = strip(info.compCompHnd->getArgType(sig, argList, &argClass));

            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, NI_X86Base_StoreNonTemporal, argJitType, 0);
            break;
        }

        case NI_AVX2_PermuteVar8x32:
        case NI_AVX512_PermuteVar4x64:
        case NI_AVX512_PermuteVar8x16:
        case NI_AVX512_PermuteVar8x64:
        case NI_AVX512_PermuteVar16x16:
        case NI_AVX512_PermuteVar16x32:
        case NI_AVX512_PermuteVar32x16:
        case NI_AVX512v2_PermuteVar16x8:
        case NI_AVX512v2_PermuteVar32x8:
        case NI_AVX512v2_PermuteVar64x8:
        {
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            // swap the two operands
            GenTree* idxVector = impSIMDPopStack();
            GenTree* srcVector = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, idxVector, srcVector, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_Fixup:
        case NI_AVX512_FixupScalar:
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

        case NI_AVX512_TernaryLogic:
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

                    // Some normalization cases require us to swap the operands, which might require
                    // spilling side effects. Check that here.
                    //
                    // cast in switch clause is needed for old gcc
                    switch ((TernaryLogicOperKind)info.oper1)
                    {
                        case TernaryLogicOperKind::Not:
                        {
                            assert(info.oper1Use != TernaryLogicUseFlags::None);

                            bool needSideEffectSpill = false;

                            if (info.oper2 == TernaryLogicOperKind::And)
                            {
                                assert(info.oper2Use != TernaryLogicUseFlags::None);

                                if ((control == static_cast<uint8_t>(~0xCC & 0xF0)) || // ~B & A
                                    (control == static_cast<uint8_t>(~0xAA & 0xF0)) || // ~C & A
                                    (control == static_cast<uint8_t>(~0xAA & 0xCC)))   // ~C & B
                                {
                                    // We're normalizing to ~B & C, so we need another swap
                                    std::swap(val2, val3);
                                    needSideEffectSpill = (control == static_cast<uint8_t>(~0xAA & 0xCC)); // ~C & B
                                }
                            }
                            else if (info.oper2 == TernaryLogicOperKind::Or)
                            {
                                assert(info.oper2Use != TernaryLogicUseFlags::None);

                                if ((control == static_cast<uint8_t>(~0xCC | 0xF0)) || // ~B | A
                                    (control == static_cast<uint8_t>(~0xAA | 0xF0)) || // ~C | A
                                    (control == static_cast<uint8_t>(~0xAA | 0xCC)))   // ~C | B
                                {
                                    // We're normalizing to ~B | C, so we need another swap
                                    std::swap(val2, val3);
                                    needSideEffectSpill = (control == static_cast<uint8_t>(~0xAA | 0xCC)); // ~C | B
                                }
                            }

                            if (needSideEffectSpill)
                            {
                                // Side-effect cases:
                                // ~B op A ; order before swap C A B
                                //    op1 & op2 already set to be spilled; no further spilling necessary
                                // ~C op A ; order before swap B A C
                                //    op1 already set to be spilled; no further spilling necessary
                                // ~C op B ; order before swap A B C
                                //    nothing already set to be spilled; op1 & op2 need to be spilled

                                spillOp1 = true;
                                spillOp2 = true;
                            }
                            break;
                        }

                        default:
                            break;
                    }

                    if (spillOp1)
                    {
                        impSpillSideEffect(true, stackState.esStackDepth -
                                                     3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
                    }

                    if (spillOp2)
                    {
                        impSpillSideEffect(true, stackState.esStackDepth -
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
                                    // We already normalized to ~B & C above.
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
                                    // We already normalized to ~B | C above.
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

        case NI_AVX512_BlendVariable:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op3     = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op3, simdBaseJitType, simdSize);
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, NI_AVX512_BlendVariableMask, simdBaseJitType,
                                               simdSize);
            break;
        }

        case NI_AVX512_Classify:
        case NI_AVX512_ClassifyScalar:
        {
            assert(sig->numArgs == 2);

            if (intrinsic == NI_AVX512_Classify)
            {
                intrinsic = NI_AVX512_ClassifyMask;
            }
            else
            {
                intrinsic = NI_AVX512_ClassifyScalarMask;
            }

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, intrinsic, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX_Compare:
        case NI_AVX_CompareScalar:
        case NI_AVX512_Compare:
        {
            assert(sig->numArgs == 3);

            if (intrinsic == NI_AVX512_Compare)
            {
                intrinsic = NI_AVX512_CompareMask;
                retType   = TYP_MASK;
            }

            int immLowerBound = 0;
            int immUpperBound = HWIntrinsicInfo::lookupImmUpperBound(intrinsic);

            op3 = impPopStack().val;
            op3 = addRangeCheckIfNeeded(intrinsic, op3, immLowerBound, immUpperBound);
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if (op3->IsCnsIntOrI())
            {
                FloatComparisonMode mode = static_cast<FloatComparisonMode>(op3->AsIntConCommon()->IntegralValue());
                NamedIntrinsic      id =
                    HWIntrinsicInfo::lookupIdForFloatComparisonMode(intrinsic, mode, simdBaseType, simdSize);

                if (id != intrinsic)
                {
                    intrinsic = id;
                    op3       = nullptr;
                }
            }

            if (op3 == nullptr)
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
            }
            else
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            }

            if (retType == TYP_MASK)
            {
                retType = getSIMDTypeForSize(simdSize);
                retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_AVX512_CompareEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareEqualMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareGreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareGreaterThanMask, simdBaseJitType,
                                               simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareGreaterThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareLessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareLessThanMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareLessThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareNotEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareNotEqualMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareNotGreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareNotGreaterThanMask, simdBaseJitType,
                                               simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareNotGreaterThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareNotLessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareNotLessThanMask, simdBaseJitType,
                                               simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareNotLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareNotLessThanOrEqualMask,
                                               simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareOrdered:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareOrderedMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompareUnordered:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode =
                gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, NI_AVX512_CompareUnorderedMask, simdBaseJitType, simdSize);
            retNode = gtNewSimdCvtMaskToVectorNode(retType, retNode, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_Compress:
        case NI_AVX512v3_Compress:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            intrinsic = NI_AVX512_CompressMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_CompressStore:
        case NI_AVX512v3_CompressStore:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            intrinsic = NI_AVX512_CompressStoreMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_Expand:
        case NI_AVX512v3_Expand:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            intrinsic = NI_AVX512_ExpandMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_ExpandLoad:
        case NI_AVX512v3_ExpandLoad:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            intrinsic = NI_AVX512_ExpandLoadMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_MaskLoad:
        case NI_AVX512_MaskLoadAligned:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            intrinsic = (intrinsic == NI_AVX512_MaskLoad) ? NI_AVX512_MaskLoadMask : NI_AVX512_MaskLoadAlignedMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AVX512_MaskStore:
        case NI_AVX512_MaskStoreAligned:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            intrinsic = (intrinsic == NI_AVX512_MaskStore) ? NI_AVX512_MaskStoreMask : NI_AVX512_MaskStoreAlignedMask;
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseJitType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
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

        case NI_AVX2_ZeroHighBits:
        case NI_AVX2_X64_ZeroHighBits:
        {
            assert(sig->numArgs == 2);

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 for ZeroHighBits"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            // Instruction BZHI requires to encode op2 (3rd register) in VEX.vvvv and op1 maybe memory operand,
            // so swap op1 and op2 to unify the backend code.
            return gtNewScalarHWIntrinsicNode(retType, op2, op1, intrinsic);
        }

        case NI_AVX2_BitFieldExtract:
        case NI_AVX2_X64_BitFieldExtract:
        {
            // The 3-arg version is implemented in managed code
            if (sig->numArgs == 3)
            {
                return nullptr;
            }
            assert(sig->numArgs == 2);

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 for BitFieldExtract"));

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

    if (isMinMaxIntrinsic)
    {
        assert(sig->numArgs == 2);
        assert(retNode == nullptr);

        if (isNative && BlockNonDeterministicIntrinsics(mustExpand))
        {
            return nullptr;
        }

        op2 = impSIMDPopStack();
        op1 = impSIMDPopStack();

        if (isNative)
        {
            assert(!isMagnitude && !isNumber);
            retNode = gtNewSimdMinMaxNativeNode(retType, op1, op2, simdBaseJitType, simdSize, isMax);
        }
        else if ((simdSize != 32) || varTypeIsFloating(simdBaseType) ||
                 compOpportunisticallyDependsOn(InstructionSet_AVX2))
        {
            retNode = gtNewSimdMinMaxNode(retType, op1, op2, simdBaseJitType, simdSize, isMax, isMagnitude, isNumber);
        }
    }

    return retNode;
}

//------------------------------------------------------------------------
// getHWIntrinsicImmOps: Gets the immediate Ops for an intrinsic
//
// Arguments:
//    intrinsic       -- NamedIntrinsic associated with the HWIntrinsic to lookup
//    sig             -- signature of the intrinsic call.
//    immOp1Ptr [OUT] -- The first immediate Op
//    immOp2Ptr [OUT] -- The second immediate Op, if any. Otherwise unchanged.
//
void Compiler::getHWIntrinsicImmOps(NamedIntrinsic    intrinsic,
                                    CORINFO_SIG_INFO* sig,
                                    GenTree**         immOp1Ptr,
                                    GenTree**         immOp2Ptr)
{
    if ((sig->numArgs > 0) && HWIntrinsicInfo::isImmOp(intrinsic, impStackTop().val))
    {
        // NOTE: The following code assumes that for all intrinsics
        // taking an immediate operand, that operand will be last.
        *immOp1Ptr = impStackTop().val;
    }
}

#endif // FEATURE_HW_INTRINSICS
