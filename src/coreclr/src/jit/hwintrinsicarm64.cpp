// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// Arm64VersionOfIsa: Gets the corresponding 64-bit only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The 64-bit only InstructionSet associated with isa
static CORINFO_InstructionSet Arm64VersionOfIsa(CORINFO_InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_AdvSimd:
            return InstructionSet_AdvSimd_Arm64;
        case InstructionSet_ArmBase:
            return InstructionSet_ArmBase_Arm64;
        case InstructionSet_Crc32:
            return InstructionSet_Crc32_Arm64;
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
        if (strcmp(className, "AdvSimd") == 0)
        {
            return InstructionSet_AdvSimd;
        }
        if (strcmp(className, "Aes") == 0)
        {
            return InstructionSet_Aes;
        }
        if (strcmp(className, "ArmBase") == 0)
        {
            return InstructionSet_ArmBase;
        }
    }
    else if (className[0] == 'C')
    {
        if (strcmp(className, "Crc32") == 0)
        {
            return InstructionSet_Crc32;
        }
    }
    else if (className[0] == 'S')
    {
        if (strcmp(className, "Sha1") == 0)
        {
            return InstructionSet_Sha1;
        }
        if (strcmp(className, "Sha256") == 0)
        {
            return InstructionSet_Sha256;
        }
    }
    else if (className[0] == 'V')
    {
        if (strncmp(className, "Vector64", 8) == 0)
        {
            return InstructionSet_Vector64;
        }
        else if (strncmp(className, "Vector128", 9) == 0)
        {
            return InstructionSet_Vector128;
        }
    }

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclsoing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    enclosingClassName -- The name of the enclosing class or nullptr if one doesn't exist
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
CORINFO_InstructionSet HWIntrinsicInfo::lookupIsa(const char* className, const char* enclosingClassName)
{
    assert(className != nullptr);

    if (strcmp(className, "Arm64") == 0)
    {
        assert(enclosingClassName != nullptr);
        return Arm64VersionOfIsa(lookupInstructionSet(enclosingClassName));
    }
    else
    {
        return lookupInstructionSet(className);
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
        case InstructionSet_AdvSimd:
        case InstructionSet_AdvSimd_Arm64:
        case InstructionSet_Aes:
        case InstructionSet_ArmBase:
        case InstructionSet_ArmBase_Arm64:
        case InstructionSet_Crc32:
        case InstructionSet_Crc32_Arm64:
        case InstructionSet_Sha1:
        case InstructionSet_Sha256:
        case InstructionSet_Vector64:
        case InstructionSet_Vector128:
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
        case InstructionSet_ArmBase:
        case InstructionSet_ArmBase_Arm64:
        case InstructionSet_Crc32:
        case InstructionSet_Crc32_Arm64:
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
// lookupImmBounds: Gets the lower and upper bounds for the imm-value of a given NamedIntrinsic
//
// Arguments:
//    intrinsic -- NamedIntrinsic associated with the HWIntrinsic to lookup
//    simdType  -- vector size
//    baseType  -- base type of the Vector64/128<T>
//    pImmLowerBound [OUT] - The lower incl. bound for a value of the intrinsic immediate operand
//    pImmUpperBound [OUT] - The upper incl. bound for a value of the intrinsic immediate operand
//
void HWIntrinsicInfo::lookupImmBounds(
    NamedIntrinsic intrinsic, int simdSize, var_types baseType, int* pImmLowerBound, int* pImmUpperBound)
{
    assert(HWIntrinsicInfo::lookupCategory(intrinsic) == HW_Category_IMM);

    assert(pImmLowerBound != nullptr);
    assert(pImmUpperBound != nullptr);

    int immLowerBound = 0;
    int immUpperBound = 0;

    if (HWIntrinsicInfo::HasFullRangeImm(intrinsic))
    {
        immUpperBound = 255;
    }
    else
    {
        switch (intrinsic)
        {
            case NI_AdvSimd_DuplicateSelectedScalarToVector64:
            case NI_AdvSimd_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Extract:
            case NI_AdvSimd_ExtractVector128:
            case NI_AdvSimd_ExtractVector64:
            case NI_AdvSimd_Insert:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            case NI_Vector64_GetElement:
            case NI_Vector128_GetElement:
                immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
                break;

            case NI_AdvSimd_ShiftLeftLogical:
            case NI_AdvSimd_ShiftLeftLogicalAndInsert:
            case NI_AdvSimd_ShiftLeftLogicalAndInsertScalar:
            case NI_AdvSimd_ShiftLeftLogicalSaturate:
            case NI_AdvSimd_ShiftLeftLogicalSaturateScalar:
            case NI_AdvSimd_ShiftLeftLogicalSaturateUnsigned:
            case NI_AdvSimd_ShiftLeftLogicalSaturateUnsignedScalar:
            case NI_AdvSimd_ShiftLeftLogicalScalar:
            case NI_AdvSimd_ShiftLeftLogicalWideningLower:
            case NI_AdvSimd_ShiftLeftLogicalWideningUpper:
            case NI_AdvSimd_Arm64_ShiftLeftLogicalSaturateScalar:
            case NI_AdvSimd_Arm64_ShiftLeftLogicalSaturateUnsignedScalar:
                // The left shift amount is in the range 0 to the element width in bits minus 1.
                immUpperBound = BITS_PER_BYTE * genTypeSize(baseType) - 1;
                break;

            case NI_AdvSimd_ShiftRightAndInsert:
            case NI_AdvSimd_ShiftRightArithmetic:
            case NI_AdvSimd_ShiftRightArithmeticAdd:
            case NI_AdvSimd_ShiftRightArithmeticAddScalar:
            case NI_AdvSimd_ShiftRightArithmeticNarrowingSaturateLower:
            case NI_AdvSimd_ShiftRightArithmeticNarrowingSaturateUnsignedLower:
            case NI_AdvSimd_ShiftRightArithmeticNarrowingSaturateUnsignedUpper:
            case NI_AdvSimd_ShiftRightArithmeticNarrowingSaturateUpper:
            case NI_AdvSimd_ShiftRightArithmeticRounded:
            case NI_AdvSimd_ShiftRightArithmeticRoundedAdd:
            case NI_AdvSimd_ShiftRightArithmeticRoundedAddScalar:
            case NI_AdvSimd_ShiftRightArithmeticRoundedNarrowingSaturateLower:
            case NI_AdvSimd_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower:
            case NI_AdvSimd_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper:
            case NI_AdvSimd_ShiftRightArithmeticRoundedNarrowingSaturateUpper:
            case NI_AdvSimd_ShiftRightArithmeticRoundedScalar:
            case NI_AdvSimd_ShiftRightArithmeticScalar:
            case NI_AdvSimd_ShiftRightLogical:
            case NI_AdvSimd_ShiftRightLogicalAdd:
            case NI_AdvSimd_ShiftRightLogicalAddScalar:
            case NI_AdvSimd_ShiftRightLogicalAndInsertScalar:
            case NI_AdvSimd_ShiftRightLogicalNarrowingLower:
            case NI_AdvSimd_ShiftRightLogicalNarrowingSaturateLower:
            case NI_AdvSimd_ShiftRightLogicalNarrowingSaturateUpper:
            case NI_AdvSimd_ShiftRightLogicalNarrowingUpper:
            case NI_AdvSimd_ShiftRightLogicalRounded:
            case NI_AdvSimd_ShiftRightLogicalRoundedAdd:
            case NI_AdvSimd_ShiftRightLogicalRoundedAddScalar:
            case NI_AdvSimd_ShiftRightLogicalRoundedNarrowingLower:
            case NI_AdvSimd_ShiftRightLogicalRoundedNarrowingSaturateLower:
            case NI_AdvSimd_ShiftRightLogicalRoundedNarrowingSaturateUpper:
            case NI_AdvSimd_ShiftRightLogicalRoundedNarrowingUpper:
            case NI_AdvSimd_ShiftRightLogicalRoundedScalar:
            case NI_AdvSimd_ShiftRightLogicalScalar:
            case NI_AdvSimd_Arm64_ShiftRightArithmeticNarrowingSaturateScalar:
            case NI_AdvSimd_Arm64_ShiftRightArithmeticNarrowingSaturateUnsignedScalar:
            case NI_AdvSimd_Arm64_ShiftRightArithmeticRoundedNarrowingSaturateScalar:
            case NI_AdvSimd_Arm64_ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar:
            case NI_AdvSimd_Arm64_ShiftRightLogicalNarrowingSaturateScalar:
            case NI_AdvSimd_Arm64_ShiftRightLogicalRoundedNarrowingSaturateScalar:
                // The right shift amount, in the range 1 to the element width in bits.
                immLowerBound = 1;
                immUpperBound = BITS_PER_BYTE * genTypeSize(baseType);
                break;
            default:
                unreached();
        }
    }

    assert(immLowerBound <= immUpperBound);

    *pImmLowerBound = immLowerBound;
    *pImmUpperBound = immUpperBound;
}

//------------------------------------------------------------------------
// isInImmRange: Check if ival is valid for the intrinsic
//
// Arguments:
//    id        -- the NamedIntrinsic associated with the HWIntrinsic to lookup
//    ival      -- the imm value to be checked
//    simdType  -- vector size
//    baseType  -- base type of the Vector64/128<T>
//
// Return Value:
//     true if ival is valid for the intrinsic
//
bool HWIntrinsicInfo::isInImmRange(NamedIntrinsic id, int ival, int simdSize, var_types baseType)
{
    assert(HWIntrinsicInfo::lookupCategory(id) == HW_Category_IMM);

    int immLowerBound = 0;
    int immUpperBound = 0;

    lookupImmBounds(id, simdSize, baseType, &immLowerBound, &immUpperBound);

    return (immLowerBound <= ival) && (ival <= immUpperBound);
}

//------------------------------------------------------------------------
// impNonConstFallback: generate alternate code when the imm-arg is not a compile-time constant
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    simdType   -- Vector type
//    baseType   -- base type of the Vector64/128<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types baseType)
{
    return nullptr;
}

//------------------------------------------------------------------------
// impSpecialIntrinsic: Import a hardware intrinsic that requires special handling as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic  -- id of the intrinsic function.
//    clsHnd     -- class handle containing the intrinsic function.
//    method     -- method handle of the intrinsic function.
//    sig        -- signature of the intrinsic call.
//    baseType   -- generic argument of the intrinsic.
//    retType    -- return type of the intrinsic.
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO*     sig,
                                       var_types             baseType,
                                       var_types             retType,
                                       unsigned              simdSize)
{
    HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);
    int                 numArgs  = sig->numArgs;

    assert(numArgs >= 0);
    assert(varTypeIsArithmetic(baseType));

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;

    switch (intrinsic)
    {
        case NI_Vector64_As:
        case NI_Vector64_AsByte:
        case NI_Vector64_AsDouble:
        case NI_Vector64_AsInt16:
        case NI_Vector64_AsInt32:
        case NI_Vector64_AsInt64:
        case NI_Vector64_AsSByte:
        case NI_Vector64_AsSingle:
        case NI_Vector64_AsUInt16:
        case NI_Vector64_AsUInt32:
        case NI_Vector64_AsUInt64:
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
        case NI_Vector128_AsVector:
        case NI_Vector128_AsVector4:
        case NI_Vector128_AsVector128:
        {
            assert(!sig->hasThis());
            assert(numArgs == 1);

            if (!featureSIMD)
            {
                return nullptr;
            }

            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

        case NI_Vector64_Create:
        case NI_Vector128_Create:
        {
            // We shouldn't handle this as an intrinsic if the
            // respective ISAs have been disabled by the user.

            if (!compExactlyDependsOn(InstructionSet_AdvSimd))
            {
                break;
            }

            if (sig->numArgs == 1)
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            }
            else if (sig->numArgs == 2)
            {
                op2     = impPopStack().val;
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, baseType, simdSize);
            }
            else
            {
                assert(sig->numArgs >= 3);

                GenTreeArgList* tmp = nullptr;

                for (unsigned i = 0; i < sig->numArgs; i++)
                {
                    tmp        = gtNewArgList(impPopStack().val);
                    tmp->gtOp2 = op1;
                    op1        = tmp;
                }

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector64_get_Count:
        case NI_Vector128_get_Count:
        {
            assert(!sig->hasThis());
            assert(numArgs == 0);

            GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, baseType), TYP_INT);
            countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
            retNode = countNode;
            break;
        }

        case NI_Vector64_get_Zero:
        case NI_Vector64_get_AllBitsSet:
        case NI_Vector128_get_Zero:
        case NI_Vector128_get_AllBitsSet:
        {
            assert(!sig->hasThis());
            assert(numArgs == 0);

            retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
            break;
        }

        case NI_Vector64_WithElement:
        case NI_Vector128_WithElement:
        {
            assert(numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;
            if (!indexOp->OperIsConst())
            {
                // If index is not constant use software fallback.
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(baseType);

            if (imm8 >= count || imm8 < 0)
            {
                // Using software fallback if index is out of range (throw exeception)
                return nullptr;
            }

            GenTree* valueOp = impPopStack().val;
            impPopStack(); // pop the indexOp that we already have.
            GenTree* vectorOp = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            switch (baseType)
            {
                case TYP_LONG:
                case TYP_ULONG:
                case TYP_DOUBLE:
                    if (simdSize == 16)
                    {
                        retNode = gtNewSimdHWIntrinsicNode(retType, vectorOp, gtNewIconNode(imm8), valueOp,
                                                           NI_AdvSimd_Insert, baseType, simdSize);
                    }
                    else
                    {
                        retNode = gtNewSimdHWIntrinsicNode(retType, valueOp, NI_Vector64_Create, baseType, simdSize);
                    }
                    break;

                case TYP_FLOAT:
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_SHORT:
                case TYP_USHORT:
                case TYP_INT:
                case TYP_UINT:
                    retNode = gtNewSimdHWIntrinsicNode(retType, vectorOp, gtNewIconNode(imm8), valueOp,
                                                       NI_AdvSimd_Insert, baseType, simdSize);
                    break;

                default:
                    return nullptr;
            }

            break;
        }

        case NI_Vector128_GetUpper:
        {
            // Converts to equivalent managed code:
            //   AdvSimd.ExtractVector128(vector, Vector128<T>.Zero, 8 / sizeof(T)).GetLower();
            assert(numArgs == 1);
            op1            = impPopStack().val;
            GenTree* zero  = gtNewSimdHWIntrinsicNode(retType, NI_Vector128_get_Zero, baseType, simdSize);
            ssize_t  index = 8 / genTypeSize(baseType);

            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, zero, gtNewIconNode(index), NI_AdvSimd_ExtractVector128,
                                               baseType, simdSize);
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD8, retNode, NI_Vector128_GetLower, baseType, 8);
            break;
        }

        default:
        {
            return nullptr;
        }
    }

    return retNode;
}

#endif // FEATURE_HW_INTRINSICS
