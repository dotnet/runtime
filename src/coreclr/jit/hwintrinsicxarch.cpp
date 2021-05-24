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
            return 8;

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
        case InstructionSet_X86Base:
        case InstructionSet_X86Base_X64:
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
    assert(HWIntrinsicInfo::NoJmpTableImm(intrinsic));
    switch (intrinsic)
    {
        case NI_SSE2_ShiftLeftLogical:
        case NI_SSE2_ShiftRightArithmetic:
        case NI_SSE2_ShiftRightLogical:
        case NI_AVX2_ShiftLeftLogical:
        case NI_AVX2_ShiftRightArithmetic:
        case NI_AVX2_ShiftRightLogical:
        {
            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack(simdType);
            GenTree* tmpOp =
                gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_SSE2_ConvertScalarToVector128Int32, CORINFO_TYPE_INT, 16);
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
    // other intrinsics need special importation
    switch (HWIntrinsicInfo::lookupIsa(intrinsic))
    {
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
            return impBaseIntrinsic(intrinsic, clsHnd, method, sig, simdBaseJitType, retType, simdSize);
        case InstructionSet_SSE:
            return impSSEIntrinsic(intrinsic, method, sig);
        case InstructionSet_SSE2:
            return impSSE2Intrinsic(intrinsic, method, sig);
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
            return impAvxOrAvx2Intrinsic(intrinsic, method, sig);

        case InstructionSet_BMI1:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
            return impBMI1OrBMI2Intrinsic(intrinsic, method, sig);
        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// impBaseIntrinsic: dispatch intrinsics to their own implementation
//
// Arguments:
//    intrinsic  -- id of the intrinsic function.
//    method     -- method handle of the intrinsic function.
//    sig        -- signature of the intrinsic call.
//    baseType   -- generic argument of the intrinsic.
//    retType    -- return type of the intrinsic.
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impBaseIntrinsic(NamedIntrinsic        intrinsic,
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

    if (!featureSIMD)
    {
        return nullptr;
    }

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

    switch (intrinsic)
    {
        case NI_Vector256_As:
        case NI_Vector256_AsByte:
        case NI_Vector256_AsDouble:
        case NI_Vector256_AsInt16:
        case NI_Vector256_AsInt32:
        case NI_Vector256_AsInt64:
        case NI_Vector256_AsSByte:
        case NI_Vector256_AsSingle:
        case NI_Vector256_AsUInt16:
        case NI_Vector256_AsUInt32:
        case NI_Vector256_AsUInt64:
        {
            if (!compExactlyDependsOn(InstructionSet_AVX))
            {
                // We don't want to deal with TYP_SIMD32 if the compiler doesn't otherwise support the type.
                break;
            }

            FALLTHROUGH;
        }

        case NI_Vector128_As:
        case NI_Vector128_AsByte:
        case NI_Vector128_AsDouble:
        case NI_Vector128_AsInt16:
        case NI_Vector128_AsInt32:
        case NI_Vector128_AsInt64:
        case NI_Vector128_AsSByte:
        case NI_Vector128_AsSingle:
        case NI_Vector128_AsUInt16:
        case NI_Vector128_AsUInt32:
        case NI_Vector128_AsUInt64:
        {
            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            assert(sig->numArgs == 1);

            retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

        case NI_Vector128_AsVector:
        {
            assert(sig->numArgs == 1);

            if (getSIMDVectorRegisterByteLength() == YMM_REGSIZE_BYTES)
            {
                // Vector<T> is TYP_SIMD32, so we should treat this as a call to Vector128.ToVector256
                return impBaseIntrinsic(NI_Vector128_ToVector256, clsHnd, method, sig, simdBaseJitType, retType,
                                        simdSize);
            }

            assert(getSIMDVectorRegisterByteLength() == XMM_REGSIZE_BYTES);

            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

            break;
        }

        case NI_Vector128_AsVector2:
        case NI_Vector128_AsVector3:
        {
            // TYP_SIMD8 and TYP_SIMD12 currently only expose "safe" versions
            // which zero the upper elements and so are implemented in managed.
            unreached();
        }

        case NI_Vector128_AsVector4:
        {
            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD or the
            // return type is a smaller TYP_SIMD that shares the same register.

            retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

            break;
        }

        case NI_Vector128_AsVector128:
        {
            assert(sig->numArgs == 1);
            assert(HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic));
            assert(simdBaseJitType ==
                   getBaseJitTypeAndSizeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args), &simdSize));

            switch (getSIMDTypeForSize(simdSize))
            {
                case TYP_SIMD8:
                case TYP_SIMD12:
                {
                    // TYP_SIMD8 and TYP_SIMD12 currently only expose "safe" versions
                    // which zero the upper elements and so are implemented in managed.
                    unreached();
                }

                case TYP_SIMD16:
                {
                    // We fold away the cast here, as it only exists to satisfy
                    // the type system. It is safe to do this here since the retNode type
                    // and the signature return type are both the same TYP_SIMD.

                    retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
                    SetOpLclRelatedToSIMDIntrinsic(retNode);
                    assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                    break;
                }

                case TYP_SIMD32:
                {
                    // Vector<T> is TYP_SIMD32, so we should treat this as a call to Vector256.GetLower
                    return impBaseIntrinsic(NI_Vector256_GetLower, clsHnd, method, sig, simdBaseJitType, retType,
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

            if (getSIMDVectorRegisterByteLength() == YMM_REGSIZE_BYTES)
            {
                // We fold away the cast here, as it only exists to satisfy
                // the type system. It is safe to do this here since the retNode type
                // and the signature return type are both the same TYP_SIMD.

                retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
                SetOpLclRelatedToSIMDIntrinsic(retNode);
                assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                break;
            }

            assert(getSIMDVectorRegisterByteLength() == XMM_REGSIZE_BYTES);

            if (compExactlyDependsOn(InstructionSet_AVX))
            {
                // We support Vector256 but Vector<T> is only 16-bytes, so we should
                // treat this method as a call to Vector256.GetLower or Vector128.ToVector256

                if (intrinsic == NI_Vector256_AsVector)
                {
                    return impBaseIntrinsic(NI_Vector256_GetLower, clsHnd, method, sig, simdBaseJitType, retType,
                                            simdSize);
                }
                else
                {
                    assert(intrinsic == NI_Vector256_AsVector256);
                    return impBaseIntrinsic(NI_Vector128_ToVector256, clsHnd, method, sig, simdBaseJitType, retType,
                                            16);
                }
            }

            break;
        }

        case NI_Vector128_get_Count:
        case NI_Vector256_get_Count:
        {
            assert(sig->numArgs == 0);

            GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
            countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
            retNode = countNode;
            break;
        }

        case NI_Vector128_Create:
        case NI_Vector256_Create:
        {
#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            // We shouldn't handle this as an intrinsic if the
            // respective ISAs have been disabled by the user.

            if (intrinsic == NI_Vector256_Create)
            {
                if (!compExactlyDependsOn(InstructionSet_AVX))
                {
                    break;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                if (!compExactlyDependsOn(InstructionSet_SSE))
                {
                    break;
                }
            }
            else if (!compExactlyDependsOn(InstructionSet_SSE2))
            {
                break;
            }

            if (sig->numArgs == 1)
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            else if (sig->numArgs == 2)
            {
                op2     = impPopStack().val;
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
            }
            else
            {
                assert(sig->numArgs >= 3);

                GenTreeArgList* tmp = nullptr;

                for (unsigned i = 0; i < sig->numArgs; i++)
                {
                    tmp = gtNewListNode(impPopStack().val, tmp);
                }

                op1     = tmp;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#ifdef TARGET_X86
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            if (compExactlyDependsOn(InstructionSet_SSE2) ||
                (compExactlyDependsOn(InstructionSet_SSE) && (simdBaseType == TYP_FLOAT)))
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ToScalar:
        {
            assert(sig->numArgs == 1);

            bool isSupported = false;

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_SHORT:
                case TYP_USHORT:
                case TYP_INT:
                case TYP_UINT:
                {
                    isSupported = compExactlyDependsOn(InstructionSet_SSE2);
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                {
                    isSupported = compExactlyDependsOn(InstructionSet_SSE2_X64);
                    break;
                }

                case TYP_FLOAT:
                case TYP_DOUBLE:
                {
                    isSupported = compExactlyDependsOn(InstructionSet_SSE);
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            if (isSupported)
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector256_ToScalar:
        {
            assert(sig->numArgs == 1);

            bool isSupported = false;

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_SHORT:
                case TYP_USHORT:
                case TYP_INT:
                case TYP_UINT:
                {
                    isSupported = compExactlyDependsOn(InstructionSet_AVX);
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                {
                    isSupported =
                        compExactlyDependsOn(InstructionSet_AVX) && compExactlyDependsOn(InstructionSet_SSE2_X64);
                    break;
                }

                case TYP_FLOAT:
                case TYP_DOUBLE:
                {
                    isSupported = compExactlyDependsOn(InstructionSet_AVX);
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            if (isSupported)
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_ToVector256:
        case NI_Vector128_ToVector256Unsafe:
        case NI_Vector256_GetLower:
        {
            assert(sig->numArgs == 1);

            if (compExactlyDependsOn(InstructionSet_AVX))
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_get_Zero:
        case NI_Vector128_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);

            if (compExactlyDependsOn(InstructionSet_SSE))
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector256_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#ifdef TARGET_X86
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // TARGET_X86

            if (compExactlyDependsOn(InstructionSet_AVX))
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector256_get_Zero:
        case NI_Vector256_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);

            if (compExactlyDependsOn(InstructionSet_AVX))
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector256_WithElement:
        {
            if (!compExactlyDependsOn(InstructionSet_AVX))
            {
                // Using software fallback if JIT/hardware don't support AVX instructions and YMM registers
                return nullptr;
            }
            FALLTHROUGH;
        }

        case NI_Vector128_WithElement:
        {
            assert(sig->numArgs == 3);

            if (!compExactlyDependsOn(InstructionSet_SSE2) || !varTypeIsArithmetic(simdBaseType))
            {
                // Using software fallback if
                // 1. JIT/hardware don't support SSE2 instructions
                // 2. simdBaseType is not a numeric type (throw exceptions)
                return nullptr;
            }

            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->OperIsConst())
            {
                // Index is not a constant, use the software fallback
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if (imm8 >= count || imm8 < 0)
            {
                // Using software fallback if index is out of range (throw exeception)
                return nullptr;
            }

            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    if (!compExactlyDependsOn(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_LONG:
                case TYP_ULONG:
                    if (!compExactlyDependsOn(InstructionSet_SSE41_X64))
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
            GenTree* vectorOp = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            retNode = gtNewSimdWithElementNode(retType, vectorOp, indexOp, valueOp, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
            break;
        }

        case NI_Vector256_GetElement:
        {
            if (!compExactlyDependsOn(InstructionSet_AVX))
            {
                // Using software fallback if JIT/hardware don't support AVX instructions and YMM registers
                return nullptr;
            }
            FALLTHROUGH;
        }

        case NI_Vector128_GetElement:
        {
            assert(sig->numArgs == 2);

            if (!compExactlyDependsOn(InstructionSet_SSE2) || !varTypeIsArithmetic(simdBaseType))
            {
                // Using software fallback if
                // 1. JIT/hardware don't support SSE2 instructions
                // 2. simdBaseType is not a numeric type (throw execptions)
                return nullptr;
            }

            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                case TYP_LONG:
                case TYP_ULONG:
                    if (!compExactlyDependsOn(InstructionSet_SSE41))
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

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            retNode = gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
            break;
        }

        default:
        {
            return nullptr;
        }
    }

    return retNode;
}

GenTree* Compiler::impSSEIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    GenTree*    retNode         = nullptr;
    GenTree*    op1             = nullptr;
    GenTree*    op2             = nullptr;
    int         simdSize        = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;

    // The Prefetch and StoreFence intrinsics don't take any SIMD operands
    // and have a simdSize of 0
    assert((simdSize == 16) || (simdSize == 0));

    switch (intrinsic)
    {
        case NI_SSE_CompareScalarGreaterThan:
        case NI_SSE_CompareScalarGreaterThanOrEqual:
        case NI_SSE_CompareScalarNotGreaterThan:
        case NI_SSE_CompareScalarNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);
            op2             = impSIMDPopStack(TYP_SIMD16);
            op1             = impSIMDPopStack(TYP_SIMD16);
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_FLOAT);

            if (compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                // These intrinsics are "special import" because the non-AVX path isn't directly
                // hardware supported. Instead, they start with "swapped operands" and we fix that here.

                FloatComparisonMode comparison =
                    static_cast<FloatComparisonMode>(HWIntrinsicInfo::lookupIval(intrinsic, true));
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, gtNewIconNode(static_cast<int>(comparison)),
                                                   NI_AVX_CompareScalar, simdBaseJitType, simdSize);
            }
            else
            {
                GenTree* clonedOp1 = nullptr;
                op1                = impCloneExpr(op1, &clonedOp1, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
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
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, CORINFO_TYPE_VOID, 0);
            break;

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    GenTree*    retNode         = nullptr;
    GenTree*    op1             = nullptr;
    GenTree*    op2             = nullptr;
    int         simdSize        = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
    CorInfoType simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);

    // The  fencing intrinsics don't take any operands and simdSize is 0
    assert((simdSize == 16) || (simdSize == 0));

    switch (intrinsic)
    {
        case NI_SSE2_CompareScalarGreaterThan:
        case NI_SSE2_CompareScalarGreaterThanOrEqual:
        case NI_SSE2_CompareScalarNotGreaterThan:
        case NI_SSE2_CompareScalarNotGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);
            op2 = impSIMDPopStack(TYP_SIMD16);
            op1 = impSIMDPopStack(TYP_SIMD16);
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_DOUBLE);

            if (compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                // These intrinsics are "special import" because the non-AVX path isn't directly
                // hardware supported. Instead, they start with "swapped operands" and we fix that here.

                FloatComparisonMode comparison =
                    static_cast<FloatComparisonMode>(HWIntrinsicInfo::lookupIval(intrinsic, true));
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, gtNewIconNode(static_cast<int>(comparison)),
                                                   NI_AVX_CompareScalar, simdBaseJitType, simdSize);
            }
            else
            {
                GenTree* clonedOp1 = nullptr;
                op1                = impCloneExpr(op1, &clonedOp1, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
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

            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, CORINFO_TYPE_VOID, simdSize);
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

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAvxOrAvx2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    GenTree*    retNode         = nullptr;
    GenTree*    op1             = nullptr;
    GenTree*    op2             = nullptr;
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    int         simdSize        = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);

    switch (intrinsic)
    {
        case NI_AVX2_PermuteVar8x32:
        {
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);
            // swap the two operands
            GenTree* indexVector  = impSIMDPopStack(TYP_SIMD32);
            GenTree* sourceVector = impSIMDPopStack(TYP_SIMD32);
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD32, indexVector, sourceVector, NI_AVX2_PermuteVar8x32,
                                               simdBaseJitType, 32);
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

            GenTree* opList = new (this, GT_LIST) GenTreeArgList(op1, gtNewArgList(op2, op3, op4, op5));
            retNode =
                new (this, GT_HWINTRINSIC) GenTreeHWIntrinsic(retType, opList, intrinsic, simdBaseJitType, simdSize);
            retNode->AsHWIntrinsic()->SetAuxiliaryJitType(indexBaseJitType);
            break;
        }

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impBMI1OrBMI2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    var_types callType = JITtype2varType(sig->retType);

    switch (intrinsic)
    {
        case NI_BMI2_ZeroHighBits:
        case NI_BMI2_X64_ZeroHighBits:
        {
            assert(sig->numArgs == 2);

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            // Instruction BZHI requires to encode op2 (3rd register) in VEX.vvvv and op1 maybe memory operand,
            // so swap op1 and op2 to unify the backend code.
            return gtNewScalarHWIntrinsicNode(callType, op2, op1, intrinsic);
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
            return gtNewScalarHWIntrinsicNode(callType, op2, op1, intrinsic);
        }

        default:
            return nullptr;
    }
}

#endif // FEATURE_HW_INTRINSICS
