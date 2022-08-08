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
            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

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
        case InstructionSet_Vector256:
        case InstructionSet_Vector128:
        case InstructionSet_X86Base:
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

        case InstructionSet_X86Serialize:
        case InstructionSet_X86Serialize_X64:
            return impSerializeIntrinsic(intrinsic, method, sig);

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
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

    CORINFO_InstructionSet isa = HWIntrinsicInfo::lookupIsa(intrinsic);

    if ((isa == InstructionSet_Vector256) && !compExactlyDependsOn(InstructionSet_AVX))
    {
        // We don't want to deal with TYP_SIMD32 if the compiler doesn't otherwise support the type.
        return nullptr;
    }

    var_types simdBaseType = TYP_UNKNOWN;

    if (intrinsic != NI_X86Base_Pause)
    {
        simdBaseType = JitType2PreciseVarType(simdBaseJitType);
        assert(varTypeIsArithmetic(simdBaseType));
    }

    switch (intrinsic)
    {
        case NI_Vector128_Abs:
        case NI_Vector256_Abs:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || varTypeIsUnsigned(simdBaseType) ||
                compExactlyDependsOn(InstructionSet_AVX2))
            {
                op1     = impSIMDPopStack(retType);
                retNode = gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Add:
        case NI_Vector256_Add:
        case NI_Vector128_op_Addition:
        case NI_Vector256_op_Addition:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_AndNot:
        case NI_Vector256_AndNot:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
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

        case NI_Vector128_BitwiseAnd:
        case NI_Vector256_BitwiseAnd:
        case NI_Vector128_op_BitwiseAnd:
        case NI_Vector256_op_BitwiseAnd:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_BitwiseOr:
        case NI_Vector256_BitwiseOr:
        case NI_Vector128_op_BitwiseOr:
        case NI_Vector256_op_BitwiseOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_Ceiling:
        case NI_Vector256_Ceiling:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsFloating(simdBaseType));

            if ((simdSize != 32) && !compExactlyDependsOn(InstructionSet_SSE41))
            {
                // Ceiling is only supported for floating-point types on SSE4.1 or later
                break;
            }

            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_ConditionalSelect:
        case NI_Vector256_ConditionalSelect:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack(retType);
            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode =
                gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
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
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            intrinsic = (simdSize == 32) ? NI_AVX_ConvertToVector256Int32WithTruncation
                                         : NI_SSE2_ConvertToVector128Int32WithTruncation;

            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_ConvertToSingle:
        case NI_Vector256_ConvertToSingle:
        {
            assert(sig->numArgs == 1);

            if (simdBaseType == TYP_INT)
            {
                intrinsic = (simdSize == 32) ? NI_AVX_ConvertToVector256Single : NI_SSE2_ConvertToVector128Single;

                op1     = impSIMDPopStack(retType);
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
        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        {
            uint32_t simdLength = getSIMDVectorLength(simdSize, simdBaseType);
            assert((sig->numArgs == 1) || (sig->numArgs == simdLength));

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
                // Some of the below code assumes 16 or 32 byte SIMD types
                assert((simdSize == 16) || (simdSize == 32));

                // For create intrinsics that take 1 operand, we broadcast the value.
                //
                // This happens even for CreateScalarUnsafe since the upper bits are
                // considered non-deterministic and we can therefore set them to anything.
                //
                // We do this as it simplifies the logic and allows certain code paths to
                // have better codegen, such as for 0, AllBitsSet, or certain small constants

                GenTreeVecCon* vecCon = gtNewVconNode(retType, simdBaseJitType);

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        uint8_t cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<uint8_t>(impPopStack().val->AsIntConCommon()->IntegralValue());
                            vecCon->gtSimd32Val.u8[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < simdLength - 1; index++)
                            {
                                vecCon->gtSimd32Val.u8[index] = cnsVal;
                            }
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
                            vecCon->gtSimd32Val.u16[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < (simdLength - 1); index++)
                            {
                                vecCon->gtSimd32Val.u16[index] = cnsVal;
                            }
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
                            vecCon->gtSimd32Val.u32[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < (simdLength - 1); index++)
                            {
                                vecCon->gtSimd32Val.u32[index] = cnsVal;
                            }
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
                            vecCon->gtSimd32Val.u64[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < (simdLength - 1); index++)
                            {
                                vecCon->gtSimd32Val.u64[index] = cnsVal;
                            }
                        }
                        break;
                    }

                    case TYP_FLOAT:
                    {
                        float cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<float>(impPopStack().val->AsDblCon()->gtDconVal);
                            vecCon->gtSimd32Val.f32[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < (simdLength - 1); index++)
                            {
                                vecCon->gtSimd32Val.f32[index] = cnsVal;
                            }
                        }
                        break;
                    }

                    case TYP_DOUBLE:
                    {
                        double cnsVal = 0;

                        for (uint32_t index = 0; index < sig->numArgs; index++)
                        {
                            cnsVal = static_cast<double>(impPopStack().val->AsDblCon()->gtDconVal);
                            vecCon->gtSimd32Val.f64[simdLength - 1 - index] = cnsVal;
                        }

                        if (sig->numArgs == 1)
                        {
                            for (uint32_t index = 0; index < (simdLength - 1); index++)
                            {
                                vecCon->gtSimd32Val.f64[index] = cnsVal;
                            }
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

            for (int i = sig->numArgs - 1; i >= 0; i--)
            {
                nodeBuilder.AddOperand(i, impPopStack().val);
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, std::move(nodeBuilder), intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Divide:
        case NI_Vector256_Divide:
        case NI_Vector128_op_Division:
        case NI_Vector256_op_Division:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsFloating(simdBaseType))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
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
                if (!varTypeIsFloating(simdBaseType) && !compExactlyDependsOn(InstructionSet_AVX2))
                {
                    // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                    break;
                }
            }
            else if ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT))
            {
                if (!compExactlyDependsOn(InstructionSet_SSE41))
                {
                    // TODO-XARCH-CQ: We can support 32-bit integers if we updating multiplication
                    // to be lowered rather than imported as the relevant operations.
                    break;
                }
            }

            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            retNode =
                gtNewSimdDotProdNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_Equals:
        case NI_Vector256_Equals:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_EqualsAll:
        case NI_Vector256_EqualsAll:
        case NI_Vector128_op_Equality:
        case NI_Vector256_op_Equality:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_EqualsAny:
        case NI_Vector256_EqualsAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_ExtractMostSignificantBits:
        case NI_Vector256_ExtractMostSignificantBits:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                NamedIntrinsic moveMaskIntrinsic = NI_Illegal;
                NamedIntrinsic shuffleIntrinsic  = NI_Illegal;
                NamedIntrinsic createIntrinsic   = NI_Illegal;

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        op1               = impSIMDPopStack(simdType);
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX2_MoveMask : NI_SSE2_MoveMask;
                        break;
                    }

                    case TYP_SHORT:
                    case TYP_USHORT:
                    {
                        simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

                        assert((simdSize == 16) || (simdSize == 32));
                        IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), simdSize);

                        // We want to tightly pack the most significant byte of each short/ushort
                        // and then zero the tightly packed least significant bytes

                        nodeBuilder.AddOperand(0x00, gtNewIconNode(0x01));
                        nodeBuilder.AddOperand(0x01, gtNewIconNode(0x03));
                        nodeBuilder.AddOperand(0x02, gtNewIconNode(0x05));
                        nodeBuilder.AddOperand(0x03, gtNewIconNode(0x07));
                        nodeBuilder.AddOperand(0x04, gtNewIconNode(0x09));
                        nodeBuilder.AddOperand(0x05, gtNewIconNode(0x0B));
                        nodeBuilder.AddOperand(0x06, gtNewIconNode(0x0D));
                        nodeBuilder.AddOperand(0x07, gtNewIconNode(0x0F));

                        for (unsigned i = 0x08; i < 0x10; i++)
                        {
                            // The most significant bit being set means zero the value
                            nodeBuilder.AddOperand(i, gtNewIconNode(0x80));
                        }

                        if (simdSize == 32)
                        {
                            // Vector256 works on 2x128-bit lanes, so repeat the same indices for the upper lane

                            nodeBuilder.AddOperand(0x10, gtNewIconNode(0x01));
                            nodeBuilder.AddOperand(0x11, gtNewIconNode(0x03));
                            nodeBuilder.AddOperand(0x12, gtNewIconNode(0x05));
                            nodeBuilder.AddOperand(0x13, gtNewIconNode(0x07));
                            nodeBuilder.AddOperand(0x14, gtNewIconNode(0x09));
                            nodeBuilder.AddOperand(0x15, gtNewIconNode(0x0B));
                            nodeBuilder.AddOperand(0x16, gtNewIconNode(0x0D));
                            nodeBuilder.AddOperand(0x17, gtNewIconNode(0x0F));

                            for (unsigned i = 0x18; i < 0x20; i++)
                            {
                                // The most significant bit being set means zero the value
                                nodeBuilder.AddOperand(i, gtNewIconNode(0x80));
                            }

                            createIntrinsic   = NI_Vector256_Create;
                            shuffleIntrinsic  = NI_AVX2_Shuffle;
                            moveMaskIntrinsic = NI_AVX2_MoveMask;
                        }
                        else if (compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                        {
                            createIntrinsic   = NI_Vector128_Create;
                            shuffleIntrinsic  = NI_SSSE3_Shuffle;
                            moveMaskIntrinsic = NI_SSE2_MoveMask;
                        }
                        else
                        {
                            return nullptr;
                        }

                        op2 = gtNewSimdHWIntrinsicNode(simdType, std::move(nodeBuilder), createIntrinsic,
                                                       simdBaseJitType, simdSize);

                        op1 = impSIMDPopStack(simdType);
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

                            simdSize = 16;
                            simdType = TYP_SIMD16;

                            op1 = gtNewSimdHWIntrinsicNode(simdType, op1, NI_Vector256_GetLower, simdBaseJitType,
                                                           simdSize);
                        }
                        break;
                    }

                    case TYP_INT:
                    case TYP_UINT:
                    case TYP_FLOAT:
                    {
                        simdBaseJitType   = CORINFO_TYPE_FLOAT;
                        op1               = impSIMDPopStack(simdType);
                        moveMaskIntrinsic = (simdSize == 32) ? NI_AVX_MoveMask : NI_SSE_MoveMask;
                        break;
                    }

                    case TYP_LONG:
                    case TYP_ULONG:
                    case TYP_DOUBLE:
                    {
                        simdBaseJitType   = CORINFO_TYPE_DOUBLE;
                        op1               = impSIMDPopStack(simdType);
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

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, moveMaskIntrinsic, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Floor:
        case NI_Vector256_Floor:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsFloating(simdBaseType));

            if ((simdSize != 32) && !compExactlyDependsOn(InstructionSet_SSE41))
            {
                // Ceiling is only supported for floating-point types on SSE4.1 or later
                break;
            }

            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_get_AllBitsSet:
        case NI_Vector256_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewAllBitsSetConNode(retType, simdBaseJitType);
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

        case NI_Vector128_get_Zero:
        case NI_Vector256_get_Zero:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewZeroConNode(retType, simdBaseJitType);
            break;
        }

        case NI_Vector128_GetElement:
        case NI_Vector256_GetElement:
        {
            assert(sig->numArgs == 2);

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

        case NI_Vector128_GreaterThan:
        case NI_Vector256_GreaterThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_GreaterThanAll:
        case NI_Vector256_GreaterThanAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_GreaterThanAny:
        case NI_Vector256_GreaterThanAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqual:
        case NI_Vector256_GreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqualAll:
        case NI_Vector256_GreaterThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_GreaterThanOrEqualAny:
        case NI_Vector256_GreaterThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThan:
        case NI_Vector256_LessThan:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThanAll:
        case NI_Vector256_LessThanAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThanAny:
        case NI_Vector256_LessThanAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqual:
        case NI_Vector256_LessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqualAll:
        case NI_Vector256_LessThanOrEqualAll:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_LessThanOrEqualAny:
        case NI_Vector256_LessThanOrEqualAny:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Load:
        case NI_Vector256_Load:
        {
            assert(sig->numArgs == 1);

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            NamedIntrinsic loadIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                loadIntrinsic = NI_AVX_LoadVector256;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                loadIntrinsic = NI_SSE2_LoadVector128;
            }
            else
            {
                loadIntrinsic = NI_SSE_LoadVector128;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_LoadAligned:
        case NI_Vector256_LoadAligned:
        {
            assert(sig->numArgs == 1);

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            NamedIntrinsic loadIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                loadIntrinsic = NI_AVX_LoadAlignedVector256;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                loadIntrinsic = NI_SSE2_LoadAlignedVector128;
            }
            else
            {
                loadIntrinsic = NI_SSE_LoadAlignedVector128;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_LoadAlignedNonTemporal:
        case NI_Vector256_LoadAlignedNonTemporal:
        {
            assert(sig->numArgs == 1);

            if ((simdSize == 16) && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // Vector128 requires at least SSE4.1
                break;
            }
            else if ((simdSize == 32) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // Vector256 requires at least AVX2
                break;
            }

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            NamedIntrinsic loadIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                loadIntrinsic = NI_AVX2_LoadAlignedVector256NonTemporal;
            }
            else
            {
                loadIntrinsic = NI_SSE41_LoadAlignedVector128NonTemporal;
            }

            // float and double don't have actual instructions for non-temporal loads
            // so we'll just use the equivalent integer instruction instead.

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseJitType = CORINFO_TYPE_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseJitType = CORINFO_TYPE_LONG;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_LoadUnsafe:
        case NI_Vector256_LoadUnsafe:
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

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            if (sig->numArgs == 2)
            {
                op3 = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, op3);
                op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);
            }

            NamedIntrinsic loadIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                loadIntrinsic = NI_AVX_LoadVector256;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                loadIntrinsic = NI_SSE2_LoadVector128;
            }
            else
            {
                loadIntrinsic = NI_SSE_LoadVector128;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Max:
        case NI_Vector256_Max:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode =
                    gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Min:
        case NI_Vector256_Min:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode =
                    gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Multiply:
        case NI_Vector256_Multiply:
        case NI_Vector128_op_Multiply:
        case NI_Vector256_op_Multiply:
        {
            assert(sig->numArgs == 2);

            if ((simdSize == 32) && !varTypeIsFloating(simdBaseType) && !compExactlyDependsOn(InstructionSet_AVX2))
            {
                // We can't deal with TYP_SIMD32 for integral types if the compiler doesn't support AVX2
                break;
            }

            if ((simdBaseType == TYP_BYTE) || (simdBaseType == TYP_UBYTE))
            {
                // TODO-XARCH-CQ: We should support byte/sbyte multiplication
                break;
            }

            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: We should support long/ulong multiplication
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

            retNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_Narrow:
        case NI_Vector256_Narrow:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode =
                    gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Negate:
        case NI_Vector256_Negate:
        case NI_Vector128_op_UnaryNegation:
        case NI_Vector256_op_UnaryNegation:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op1 = impSIMDPopStack(retType);
                retNode =
                    gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_OnesComplement:
        case NI_Vector256_OnesComplement:
        case NI_Vector128_op_OnesComplement:
        case NI_Vector256_op_OnesComplement:
        {
            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack(retType);
            retNode =
                gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_op_Inequality:
        case NI_Vector256_op_Inequality:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack(simdType);
                op1 = impSIMDPopStack(simdType);

                retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_op_UnaryPlus:
        case NI_Vector256_op_UnaryPlus:
        {
            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack(retType);
            break;
        }

        case NI_Vector128_Subtract:
        case NI_Vector256_Subtract:
        case NI_Vector128_op_Subtraction:
        case NI_Vector256_op_Subtraction:
        {
            assert(sig->numArgs == 2);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_ShiftLeft:
        case NI_Vector256_ShiftLeft:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsByte(simdBaseType))
            {
                // byte and sbyte would require more work to support
                break;
            }

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_ShiftRightArithmetic:
        case NI_Vector256_ShiftRightArithmetic:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsByte(simdBaseType) || varTypeIsLong(simdBaseType))
            {
                // byte, sbyte, long, and ulong would require more work to support
                break;
            }

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_RSH, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_ShiftRightLogical:
        case NI_Vector256_ShiftRightLogical:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsByte(simdBaseType))
            {
                // byte and sbyte would require more work to support
                break;
            }

            if ((simdSize != 32) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op2 = impPopStack().val;
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Shuffle:
        case NI_Vector256_Shuffle:
        {
            assert((sig->numArgs == 2) || (sig->numArgs == 3));
            assert((simdSize == 16) || (simdSize == 32));

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
                if (!compExactlyDependsOn(InstructionSet_AVX2))
                {
                    // While we could accelerate some functions on hardware with only AVX support
                    // it's likely not worth it overall given that IsHardwareAccelerated reports false
                    break;
                }
                else if (varTypeIsSmallInt(simdBaseType))
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
            else
            {
                assert(simdSize == 16);

                if (varTypeIsSmallInt(simdBaseType) && !compExactlyDependsOn(InstructionSet_SSSE3))
                {
                    // TYP_BYTE, TYP_UBYTE, TYP_SHORT, and TYP_USHORT need SSSE3 to be able to shuffle any operation
                    break;
                }
            }

            if (sig->numArgs == 2)
            {
                op2 = impSIMDPopStack(retType);
                op1 = impSIMDPopStack(retType);

                retNode = gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Sqrt:
        case NI_Vector256_Sqrt:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                op1     = impSIMDPopStack(retType);
                retNode = gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_Store:
        case NI_Vector256_Store:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;
            op1 = impSIMDPopStack(simdType);

            NamedIntrinsic storeIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                storeIntrinsic = NI_AVX_Store;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                storeIntrinsic = NI_SSE2_Store;
            }
            else
            {
                storeIntrinsic = NI_SSE_Store;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_StoreAligned:
        case NI_Vector256_StoreAligned:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;
            op1 = impSIMDPopStack(simdType);

            NamedIntrinsic storeIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                storeIntrinsic = NI_AVX_StoreAligned;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                storeIntrinsic = NI_SSE2_StoreAligned;
            }
            else
            {
                storeIntrinsic = NI_SSE_StoreAligned;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_StoreAlignedNonTemporal:
        case NI_Vector256_StoreAlignedNonTemporal:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

            op2 = impPopStack().val;
            op1 = impSIMDPopStack(simdType);

            NamedIntrinsic storeIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                storeIntrinsic = NI_AVX_StoreAlignedNonTemporal;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                storeIntrinsic = NI_SSE2_StoreAlignedNonTemporal;
            }
            else
            {
                storeIntrinsic = NI_SSE_StoreAlignedNonTemporal;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_StoreUnsafe:
        case NI_Vector256_StoreUnsafe:
        {
            var_types simdType = getSIMDTypeForSize(simdSize);

            if (sig->numArgs == 3)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             3 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op2 side effects for HWIntrinsic"));

                op3 = impPopStack().val;
            }
            else
            {
                assert(sig->numArgs == 2);

                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impPopStack().val;
            op1 = impSIMDPopStack(simdType);

            if (sig->numArgs == 3)
            {
                op4 = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, op4);
                op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);
            }

            NamedIntrinsic storeIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                storeIntrinsic = NI_AVX_Store;
            }
            else if (simdBaseType != TYP_FLOAT)
            {
                storeIntrinsic = NI_SSE2_Store;
            }
            else
            {
                storeIntrinsic = NI_SSE_Store;
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_Sum:
        case NI_Vector256_Sum:
        {
            assert(sig->numArgs == 1);
            var_types simdType = getSIMDTypeForSize(simdSize);

            if ((simdSize == 32) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // Vector256 for integer types requires AVX2
                break;
            }
            else if (varTypeIsFloating(simdBaseType))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_SSE3))
                {
                    // Floating-point types require SSE3.HorizontalAdd
                    break;
                }
            }
            else if (!compOpportunisticallyDependsOn(InstructionSet_SSSE3))
            {
                // Integral types require SSSE3.HorizontalAdd
                break;
            }
            else if (varTypeIsByte(simdBaseType) || varTypeIsLong(simdBaseType))
            {
                // byte, sbyte, long, and ulong all would require more work to support
                break;
            }

            op1     = impSIMDPopStack(simdType);
            retNode = gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        {
            assert(sig->numArgs == 1);

#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to decompose this operation
                break;
            }
#endif // TARGET_X86

            // TODO-XARCH-CQ: It may be beneficial to import this as GetElement(0)
            op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
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

        case NI_Vector128_WidenLower:
        case NI_Vector256_WidenLower:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op1 = impSIMDPopStack(retType);

                retNode =
                    gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_WidenUpper:
        case NI_Vector256_WidenUpper:
        {
            assert(sig->numArgs == 1);

            if ((simdSize != 32) || varTypeIsFloating(simdBaseType) || compExactlyDependsOn(InstructionSet_AVX2))
            {
                op1 = impSIMDPopStack(retType);

                retNode =
                    gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector128_WithElement:
        case NI_Vector256_WithElement:
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

            if (imm8 >= count || imm8 < 0)
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

        case NI_Vector128_Xor:
        case NI_Vector256_Xor:
        case NI_Vector128_op_ExclusiveOr:
        case NI_Vector256_op_ExclusiveOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
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

            bool supportsAvx = compOpportunisticallyDependsOn(InstructionSet_AVX);

            if (!supportsAvx)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2             = impSIMDPopStack(TYP_SIMD16);
            op1             = impSIMDPopStack(TYP_SIMD16);
            simdBaseJitType = getBaseJitTypeOfSIMDType(sig->retTypeSigClass);
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_FLOAT);

            if (supportsAvx)
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
            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
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

            bool supportsAvx = compOpportunisticallyDependsOn(InstructionSet_AVX);

            if (!supportsAvx)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));
            }

            op2 = impSIMDPopStack(TYP_SIMD16);
            op1 = impSIMDPopStack(TYP_SIMD16);
            assert(JitType2PreciseVarType(simdBaseJitType) == TYP_DOUBLE);

            if (supportsAvx)
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

            impSpillSideEffect(true,
                               verCurrentState.esStackDepth - 2 DEBUGARG("Spilling op1 side effects for HWIntrinsic"));

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

            const bool isSimdAsHWIntrinsic = false;

            retNode = new (this, GT_HWINTRINSIC)
                GenTreeHWIntrinsic(retType, getAllocator(CMK_ASTNode), intrinsic, simdBaseJitType, simdSize,
                                   isSimdAsHWIntrinsic, op1, op2, op3, op4, op5);
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

GenTree* Compiler::impSerializeIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    GenTree* retNode = nullptr;

    switch (intrinsic)
    {
        case NI_X86Serialize_Serialize:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);

            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;
        }

        default:
            return nullptr;
    }

    return retNode;
}

#endif // FEATURE_HW_INTRINSICS
