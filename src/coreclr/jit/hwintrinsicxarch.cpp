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
        case InstructionSet_AVXVNNIINT:
            return InstructionSet_AVXVNNIINT;
        case InstructionSet_AVXVNNIINT_V512:
            return InstructionSet_AVXVNNIINT_V512;
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

        case InstructionSet_AVXVNNIINT:
        case InstructionSet_AVXVNNIINT_V512:
        {
            return InstructionSet_AVXVNNIINT_V512;
        }

        case InstructionSet_AVXVNNI:
        case InstructionSet_AVX512v3:
        {
            // AvxVnni.V512 lifts under AVX512v3, which carries the EVEX-encoded
            // VPDPBUSD / VPDPWSSD on ZMM. The class-name dispatch in
            // lookupInstructionSet has already chosen between AVXVNNI and AVX512v3
            // based on the available CPUID bits, and the caller's downstream
            // compSupportsHWIntrinsic(InstructionSet_AVX512v3) check gates the
            // result correctly: on machines without AVX-512 (e.g. Tiger Lake)
            // AVX512v3 isn't supported and IsSupported returns false.
            return InstructionSet_AVX512v3;
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
CORINFO_InstructionSet Compiler::lookupInstructionSet(const char* className)
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
                    else if (strcmp(className + 7, "mm") == 0)
                    {
                        return InstructionSet_AVX512BMM;
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
            else if (strncmp(className + 3, "Vnni", 4) == 0)
            {
                if (className[7] == '\0')
                {
                    if (compSupportsHWIntrinsic(InstructionSet_AVXVNNI))
                    {
                        return InstructionSet_AVXVNNI;
                    }
                    else
                    {
                        return InstructionSet_AVX512v3;
                    }
                }
                else if (strncmp(className + 7, "Int", 3) == 0)
                {
                    if ((strcmp(className + 10, "8") == 0) || (strcmp(className + 10, "16") == 0))
                    {
                        if (compSupportsHWIntrinsic(InstructionSet_AVXVNNIINT))
                        {
                            return InstructionSet_AVXVNNIINT;
                        }
                        else
                        {
                            return InstructionSet_AVXVNNIINT_V512;
                        }
                    }
                }
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
            return InstructionSet_X86Base;
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
                return InstructionSet_X86Base;
            }
            else if (strcmp(className + 3, "41") == 0)
            {
                return InstructionSet_X86Base;
            }
            else if (strcmp(className + 3, "42") == 0)
            {
                return InstructionSet_X86Base;
            }
        }
        else if (strcmp(className + 1, "sse3") == 0)
        {
            return InstructionSet_X86Base;
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
CORINFO_InstructionSet Compiler::lookupIsa(const char* className,
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
        case NI_AVX512_CompareScalarMask:
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

    // .NET doesn't differentiate between signalling and non-signalling
    // we disable IEEE 754 exceptions on startup and make it undefined
    // behavior to enable it, so we'll just normalize to the same form

    switch (comparison)
    {
        case FloatComparisonMode::OrderedEqualNonSignaling:
        case FloatComparisonMode::OrderedEqualSignaling:
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
        case FloatComparisonMode::OrderedGreaterThanNonSignaling:
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
        case FloatComparisonMode::OrderedGreaterThanOrEqualNonSignaling:
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
        case FloatComparisonMode::OrderedLessThanNonSignaling:
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
        case FloatComparisonMode::OrderedLessThanOrEqualNonSignaling:
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
        case FloatComparisonMode::UnorderedNotEqualSignaling:
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
        case FloatComparisonMode::UnorderedNotGreaterThanNonSignaling:
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
        case FloatComparisonMode::UnorderedNotGreaterThanOrEqualNonSignaling:
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
        case FloatComparisonMode::UnorderedNotLessThanNonSignaling:
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
        case FloatComparisonMode::UnorderedNotLessThanOrEqualNonSignaling:
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
        case FloatComparisonMode::OrderedSignaling:
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
        case FloatComparisonMode::UnorderedSignaling:
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

        case NI_X86Base_Ceiling:
        case NI_X86Base_CeilingScalar:
        case NI_AVX_Ceiling:
        {
            FALLTHROUGH;
        }

        case NI_X86Base_RoundToPositiveInfinity:
        case NI_X86Base_RoundToPositiveInfinityScalar:
        case NI_AVX_RoundToPositiveInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToPositiveInfinity);
        }

        case NI_X86Base_Floor:
        case NI_X86Base_FloorScalar:
        case NI_AVX_Floor:
        {
            FALLTHROUGH;
        }

        case NI_X86Base_RoundToNegativeInfinity:
        case NI_X86Base_RoundToNegativeInfinityScalar:
        case NI_AVX_RoundToNegativeInfinity:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNegativeInfinity);
        }

        case NI_X86Base_RoundCurrentDirection:
        case NI_X86Base_RoundCurrentDirectionScalar:
        case NI_AVX_RoundCurrentDirection:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::CurrentDirection);
        }

        case NI_X86Base_RoundToNearestInteger:
        case NI_X86Base_RoundToNearestIntegerScalar:
        case NI_AVX_RoundToNearestInteger:
        {
            assert(varTypeIsFloating(simdBaseType));
            return static_cast<int>(FloatRoundingMode::ToNearestInteger);
        }

        case NI_X86Base_RoundToZero:
        case NI_X86Base_RoundToZeroScalar:
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
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types simdBaseType)
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

            GenTree* tmpOp = gtNewSimdCreateScalarNode(TYP_SIMD16, op2, TYP_INT, 16);
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, simdBaseType, genTypeSize(simdType));
        }

        case NI_AVX512_RotateLeft:
        case NI_AVX512_RotateRight:
        {
            // These intrinsics have variants that take op2 in a simd register and read a unique shift per element
            intrinsic = static_cast<NamedIntrinsic>(intrinsic + 1);

            static_assert(NI_AVX512_RotateLeftVariable == (NI_AVX512_RotateLeft + 1));
            static_assert(NI_AVX512_RotateRightVariable == (NI_AVX512_RotateRight + 1));

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack();

            if (varTypeIsLong(simdBaseType))
            {
                op2 = gtNewCastNode(TYP_LONG, op2, /* fromUnsigned */ true, TYP_LONG);
            }

            GenTree* tmpOp = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseType, genTypeSize(simdType));
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, simdBaseType, genTypeSize(simdType));
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
                                       var_types             simdBaseType,
                                       var_types             retType,
                                       unsigned              simdSize,
                                       bool                  mustExpand)
{
    CORINFO_InstructionSet isa = HWIntrinsicInfo::lookupIsa(intrinsic);

    if (isa == InstructionSet_Vector)
    {
        return impXplatIntrinsic(intrinsic, clsHnd, method, sig R2RARG(entryPoint), simdBaseType, retType, simdSize,
                                 mustExpand);
    }

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

    if (simdSize != 0)
    {
        assert(varTypeIsArithmetic(simdBaseType));
    }

    switch (intrinsic)
    {
        case NI_AVX2_AndNot:
        {
            if (varTypeIsSIMD(retType))
            {
                intrinsic             = NI_AVX2_AndNotVector;
                simdSize              = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
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

            if (simdSize != 0)
            {
                // We don't want to support creating AND_NOT nodes prior to LIR
                // as it can break important optimizations. We'll produces this
                // in lowering instead so decompose into the individual operations
                // on import, taking into account that despite the name, these APIs
                // do (~op1 & op2), so we need to account for that

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                op1     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseType, simdSize));
                retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseType, simdSize);
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

        case NI_AVX512_MoveMask:
        {
            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack();

            op1     = gtFoldExpr(gtNewSimdCvtVectorToMaskNode(TYP_MASK, op1, simdBaseType, simdSize));
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_LoadVector128:
        case NI_AVX_LoadVector256:
        case NI_AVX512_LoadVector512:
        {
            assert(sig->numArgs == 1);
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadNode(retType, op1, simdBaseType, simdSize);
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

            retNode = gtNewSimdStoreNode(op1, op2, simdBaseType, simdSize);
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
            assert(simdBaseType != TYP_UNDEF);

            op3 = impPopStack().val;
            op2 = impPopStack().val;
            op1 = impPopStack().val;

            GenTreeHWIntrinsic* divRemIntrinsic = gtNewScalarHWIntrinsicNode(retType, op1, op2, op3, intrinsic);

            // Store the type from signature into SIMD base type for convenience
            divRemIntrinsic->SetSimdBaseType(simdBaseType);

            retNode = impStoreMultiRegValueToVar(divRemIntrinsic,
                                                 sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
            break;
        }

        case NI_X86Base_X64_BigMul:
        {
            assert(sig->numArgs == 2);
            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));
            assert(retType == TYP_STRUCT);
            assert(simdBaseType != TYP_UNDEF);

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            GenTreeHWIntrinsic* multiplyIntrinsic = gtNewScalarHWIntrinsicNode(retType, op1, op2, intrinsic);

            // Store the type from signature into SIMD base type for convenience
            multiplyIntrinsic->SetSimdBaseType(simdBaseType);

            retNode = impStoreMultiRegValueToVar(multiplyIntrinsic,
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
            assert(varTypeIsFloating(simdBaseType));

            if (supportsAvx)
            {
                // These intrinsics are "special import" because the non-AVX path isn't directly
                // hardware supported. Instead, they start with "swapped operands" and we fix that here.

                int ival = HWIntrinsicInfo::lookupIval(this, intrinsic, simdBaseType);
                retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, gtNewIconNode(ival), NI_AVX_CompareScalar,
                                                    simdBaseType, simdSize);
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

                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, intrinsic, simdBaseType, simdSize);
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, clonedOp1, retNode, NI_X86Base_MoveScalar, simdBaseType,
                                                   simdSize);
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
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, intrinsic, TYP_UBYTE, 0);
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
            var_types argType = JitType2PreciseVarType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));

            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, NI_X86Base_StoreNonTemporal, argType, 0);
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
            simdBaseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);

            impSpillSideEffect(true, stackState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            // swap the two operands
            GenTree* idxVector = impSIMDPopStack();
            GenTree* srcVector = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, idxVector, srcVector, intrinsic, simdBaseType, simdSize);
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

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, op4, intrinsic, simdBaseType, simdSize);

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
                uint8_t                 control  = static_cast<uint8_t>(op4->AsIntCon()->IconValue());
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

                                op4->AsIntCon()->SetIconValue(static_cast<uint8_t>(~0xAA));
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

                                return gtNewSimdBinOpNode(GT_AND_NOT, retType, *val3, *val2, simdBaseType, simdSize);
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

                                op4->AsIntCon()->SetIconValue(static_cast<uint8_t>(~0xCC | 0xAA));
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

                            return gtNewSimdBinOpNode(GT_AND, retType, *val2, *val3, simdBaseType, simdSize);
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

                            op4->AsIntCon()->SetIconValue(static_cast<uint8_t>(~(0xCC & 0xAA)));
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

                            return gtNewSimdBinOpNode(GT_OR, retType, *val2, *val3, simdBaseType, simdSize);
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

                            op4->AsIntCon()->SetIconValue(static_cast<uint8_t>(~(0xCC | 0xAA)));
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

                            return gtNewSimdBinOpNode(GT_XOR, retType, *val2, *val3, simdBaseType, simdSize);
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

                            op4->AsIntCon()->SetIconValue(static_cast<uint8_t>(~(0xCC ^ 0xAA)));
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

                    retNode = gtNewSimdTernaryLogicNode(retType, *val1, *val2, *val3, op4, simdBaseType, simdSize);
                    break;
                }
            }

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdTernaryLogicNode(retType, op1, op2, op3, op4, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_BlendVariable:
        case NI_AVX_BlendVariable:
        case NI_AVX2_BlendVariable:
        case NI_AVX512_BlendVariable:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                if ((intrinsic != NI_AVX512_BlendVariable) && varTypeIsIntegral(simdBaseType))
                {
                    // The pre-EVEX intrinsics for integers all emitted pblendvb and so we need
                    // to preserve that behavior when using the EVEX variant as otherwise we'll
                    // get slightly different results for certain masks.

                    if (varTypeIsSigned(simdBaseType))
                    {
                        simdBaseType = TYP_BYTE;
                    }
                    else
                    {
                        simdBaseType = TYP_UBYTE;
                    }
                }
                intrinsic = NI_AVX512_BlendVariableMask;
                op3       = gtFoldExpr(gtNewSimdCvtVectorToMaskNode(TYP_MASK, op3, simdBaseType, simdSize));
            }
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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
            retType = TYP_MASK;

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_AVX_Compare:
        case NI_AVX512_Compare:
        {
            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareMask;
                retType   = TYP_MASK;
            }
            FALLTHROUGH;
        }

        case NI_AVX_CompareScalar:
        {
            assert(sig->numArgs == 3);

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
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            }
            else
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            }
            break;
        }

        case NI_X86Base_CompareEqual:
        case NI_AVX_CompareEqual:
        case NI_AVX2_CompareEqual:
        case NI_AVX512_CompareEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareGreaterThan:
        case NI_AVX_CompareGreaterThan:
        case NI_AVX2_CompareGreaterThan:
        case NI_AVX512_CompareGreaterThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareGreaterThanMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareGreaterThanOrEqual:
        case NI_AVX_CompareGreaterThanOrEqual:
        case NI_AVX512_CompareGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareGreaterThanOrEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareLessThan:
        case NI_AVX_CompareLessThan:
        case NI_AVX2_CompareLessThan:
        case NI_AVX512_CompareLessThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareLessThanMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareLessThanOrEqual:
        case NI_AVX_CompareLessThanOrEqual:
        case NI_AVX512_CompareLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareLessThanOrEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareNotEqual:
        case NI_AVX_CompareNotEqual:
        case NI_AVX512_CompareNotEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareNotEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareNotGreaterThan:
        case NI_AVX_CompareNotGreaterThan:
        case NI_AVX512_CompareNotGreaterThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareNotGreaterThanMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareNotGreaterThanOrEqual:
        case NI_AVX_CompareNotGreaterThanOrEqual:
        case NI_AVX512_CompareNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareNotGreaterThanOrEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareNotLessThan:
        case NI_AVX_CompareNotLessThan:
        case NI_AVX512_CompareNotLessThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareNotLessThanMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareNotLessThanOrEqual:
        case NI_AVX_CompareNotLessThanOrEqual:
        case NI_AVX512_CompareNotLessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareNotLessThanOrEqualMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareOrdered:
        case NI_AVX_CompareOrdered:
        case NI_AVX512_CompareOrdered:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareOrderedMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_X86Base_CompareUnordered:
        case NI_AVX_CompareUnordered:
        case NI_AVX512_CompareUnordered:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 64) || canUseEvexEncoding())
            {
                intrinsic = NI_AVX512_CompareUnorderedMask;
                retType   = TYP_MASK;
            }

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_AVXVNNIINT_MultiplyWideningAndAdd:
        case NI_AVXVNNIINT_MultiplyWideningAndAddSaturate:
        case NI_AVXVNNIINT_V512_MultiplyWideningAndAdd:
        case NI_AVXVNNIINT_V512_MultiplyWideningAndAddSaturate:
        {
            assert(sig->numArgs == 3);

            CORINFO_ARG_LIST_HANDLE argList = sig->args;
            CORINFO_CLASS_HANDLE    argClass;
            var_types               argType = TYP_UNKNOWN;

            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

            argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            var_types op3BaseType = getBaseTypeOfSIMDType(argClass);
            GenTree*  op3         = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(op3BaseType);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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
            op2       = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            retNode   = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
        {
            CORINFO_ARG_LIST_HANDLE argList = sig->args;
            CORINFO_CLASS_HANDLE    argClass;
            var_types               argType = TYP_UNKNOWN;
            unsigned int            sizeBytes;
            simdBaseType      = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
            var_types retType = getSIMDTypeForSize(sizeBytes);

            assert(sig->numArgs == 5);
            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);
            CORINFO_ARG_LIST_HANDLE arg4 = info.compCompHnd->getArgNext(arg3);
            CORINFO_ARG_LIST_HANDLE arg5 = info.compCompHnd->getArgNext(arg4);

            argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg5, &argClass)));
            GenTree* op5 = getArgForHWIntrinsic(argType, argClass);

            argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
            GenTree* op4 = getArgForHWIntrinsic(argType, argClass);

            argType                 = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            var_types indexBaseType = getBaseTypeOfSIMDType(argClass);
            GenTree*  op3           = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = new (this, GT_HWINTRINSIC) GenTreeHWIntrinsic(retType, getAllocator(CMK_ASTNode), intrinsic,
                                                                    simdBaseType, simdSize, op1, op2, op3, op4, op5);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(indexBaseType);
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

    if (retType == TYP_MASK)
    {
        retType = getSIMDTypeForSize(simdSize);
        assert(retType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
        retNode = gtNewSimdCvtMaskToVectorNode(retType, gtFoldExpr(retNode), simdBaseType, simdSize);
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
