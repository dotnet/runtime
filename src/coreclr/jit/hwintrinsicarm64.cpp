// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        case InstructionSet_Aes:
            return InstructionSet_Aes_Arm64;
        case InstructionSet_ArmBase:
            return InstructionSet_ArmBase_Arm64;
        case InstructionSet_Crc32:
            return InstructionSet_Crc32_Arm64;
        case InstructionSet_Dp:
            return InstructionSet_Dp_Arm64;
        case InstructionSet_Sha1:
            return InstructionSet_Sha1_Arm64;
        case InstructionSet_Sha256:
            return InstructionSet_Sha256_Arm64;
        case InstructionSet_Rdm:
            return InstructionSet_Rdm_Arm64;
        case InstructionSet_Sve:
            return InstructionSet_Sve_Arm64;
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
    else if (className[0] == 'D')
    {
        if (strcmp(className, "Dp") == 0)
        {
            return InstructionSet_Dp;
        }
    }
    else if (className[0] == 'R')
    {
        if (strcmp(className, "Rdm") == 0)
        {
            return InstructionSet_Rdm;
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
        if (strcmp(className, "Sve") == 0)
        {
            return InstructionSet_Sve;
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
        case InstructionSet_Aes_Arm64:
        case InstructionSet_ArmBase:
        case InstructionSet_ArmBase_Arm64:
        case InstructionSet_Crc32:
        case InstructionSet_Crc32_Arm64:
        case InstructionSet_Dp:
        case InstructionSet_Dp_Arm64:
        case InstructionSet_Rdm:
        case InstructionSet_Rdm_Arm64:
        case InstructionSet_Sha1:
        case InstructionSet_Sha1_Arm64:
        case InstructionSet_Sha256:
        case InstructionSet_Sha256_Arm64:
        case InstructionSet_Sve:
        case InstructionSet_Sve_Arm64:
        case InstructionSet_Vector64:
        case InstructionSet_Vector128:
            return true;

        default:
            return false;
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
    HWIntrinsicCategory category            = HWIntrinsicInfo::lookupCategory(intrinsic);
    bool                hasImmediateOperand = HasImmediateOperand(intrinsic);

    assert(hasImmediateOperand);

    assert(pImmLowerBound != nullptr);
    assert(pImmUpperBound != nullptr);

    int immLowerBound = 0;
    int immUpperBound = 0;

    if (category == HW_Category_ShiftLeftByImmediate)
    {
        // The left shift amount is in the range 0 to the element width in bits minus 1.
        immUpperBound = BITS_PER_BYTE * genTypeSize(baseType) - 1;
    }
    else if (category == HW_Category_ShiftRightByImmediate)
    {
        // The right shift amount, in the range 1 to the element width in bits.
        immLowerBound = 1;
        immUpperBound = BITS_PER_BYTE * genTypeSize(baseType);
    }
    else if (category == HW_Category_SIMDByIndexedElement)
    {
        immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
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
            case NI_AdvSimd_InsertScalar:
            case NI_AdvSimd_LoadAndInsertScalar:
            case NI_AdvSimd_LoadAndInsertScalarVector64x2:
            case NI_AdvSimd_LoadAndInsertScalarVector64x3:
            case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
            case NI_AdvSimd_StoreSelectedScalar:
            case NI_AdvSimd_StoreSelectedScalarVector64x2:
            case NI_AdvSimd_StoreSelectedScalarVector64x3:
            case NI_AdvSimd_StoreSelectedScalarVector64x4:
            case NI_AdvSimd_Arm64_StoreSelectedScalar:
            case NI_AdvSimd_Arm64_StoreSelectedScalarVector128x2:
            case NI_AdvSimd_Arm64_StoreSelectedScalarVector128x3:
            case NI_AdvSimd_Arm64_StoreSelectedScalarVector128x4:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Arm64_InsertSelectedScalar:
                immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
                break;

            case NI_Sve_CreateTrueMaskByte:
            case NI_Sve_CreateTrueMaskDouble:
            case NI_Sve_CreateTrueMaskInt16:
            case NI_Sve_CreateTrueMaskInt32:
            case NI_Sve_CreateTrueMaskInt64:
            case NI_Sve_CreateTrueMaskSByte:
            case NI_Sve_CreateTrueMaskSingle:
            case NI_Sve_CreateTrueMaskUInt16:
            case NI_Sve_CreateTrueMaskUInt32:
            case NI_Sve_CreateTrueMaskUInt64:
                immLowerBound = (int)SVE_PATTERN_POW2;
                immUpperBound = (int)SVE_PATTERN_ALL;
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
// impNonConstFallback: generate alternate code when the imm-arg is not a compile-time constant
//
// Arguments:
//    intrinsic       -- intrinsic ID
//    simdType        -- Vector type
//    simdBaseJitType -- base JIT type of the Vector64/128<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, CorInfoType simdBaseJitType)
{
    return nullptr;
}

//------------------------------------------------------------------------
// impSpecialIntrinsic: Import a hardware intrinsic that requires special handling as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    simdBaseJitType -- generic argument of the intrinsic.
//    retType         -- return type of the intrinsic.
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO*     sig,
                                       CorInfoType           simdBaseJitType,
                                       var_types             retType,
                                       unsigned              simdSize)
{
    const HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);
    const int                 numArgs  = sig->numArgs;

    // The vast majority of "special" intrinsics are Vector64/Vector128 methods.
    // The only exception is ArmBase.Yield which should be treated differently.
    if (intrinsic == NI_ArmBase_Yield)
    {
        assert(sig->numArgs == 0);
        assert(JITtype2varType(sig->retType) == TYP_VOID);
        assert(simdSize == 0);

        return gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
    }

    assert(category != HW_Category_Scalar);
    assert(!HWIntrinsicInfo::isScalarIsa(HWIntrinsicInfo::lookupIsa(intrinsic)));

    assert(numArgs >= 0);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

    switch (intrinsic)
    {
        case NI_Vector64_Abs:
        case NI_Vector128_Abs:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Add:
        case NI_Vector128_Add:
        case NI_Vector64_op_Addition:
        case NI_Vector128_op_Addition:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_AndNot:
        case NI_Vector128_AndNot:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_As:
        case NI_Vector64_AsByte:
        case NI_Vector64_AsDouble:
        case NI_Vector64_AsInt16:
        case NI_Vector64_AsInt32:
        case NI_Vector64_AsInt64:
        case NI_Vector64_AsNInt:
        case NI_Vector64_AsNUInt:
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
        case NI_Vector128_AsNInt:
        case NI_Vector128_AsNUInt:
        case NI_Vector128_AsSByte:
        case NI_Vector128_AsSingle:
        case NI_Vector128_AsUInt16:
        case NI_Vector128_AsUInt32:
        case NI_Vector128_AsUInt64:
        case NI_Vector128_AsVector:
        case NI_Vector128_AsVector4:
        {
            assert(!sig->hasThis());
            assert(numArgs == 1);

            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            retNode = impSIMDPopStack();
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

        case NI_Vector128_AsVector2:
        {
            assert(sig->numArgs == 1);
            assert((simdSize == 16) && (simdBaseType == TYP_FLOAT));
            assert(retType == TYP_SIMD8);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetLowerNode(TYP_SIMD8, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_AsVector3:
        {
            assert(sig->numArgs == 1);
            assert((simdSize == 16) && (simdBaseType == TYP_FLOAT));
            assert(retType == TYP_SIMD12);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_AsVector128:
        {
            assert(!sig->hasThis());
            assert(numArgs == 1);
            assert(retType == TYP_SIMD16);

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

                default:
                {
                    unreached();
                }
            }

            break;
        }

        case NI_Vector64_BitwiseAnd:
        case NI_Vector128_BitwiseAnd:
        case NI_Vector64_op_BitwiseAnd:
        case NI_Vector128_op_BitwiseAnd:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_BitwiseOr:
        case NI_Vector128_BitwiseOr:
        case NI_Vector64_op_BitwiseOr:
        case NI_Vector128_op_BitwiseOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Ceiling:
        case NI_Vector128_Ceiling:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConditionalSelect:
        case NI_Vector128_ConditionalSelect:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack();
            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConvertToDouble:
        case NI_Vector128_ConvertToDouble:
        {
            assert(sig->numArgs == 1);
            assert((simdBaseType == TYP_LONG) || (simdBaseType == TYP_ULONG));

            intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_ConvertToDoubleScalar : NI_AdvSimd_Arm64_ConvertToDouble;

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConvertToInt32:
        case NI_Vector128_ConvertToInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            op1 = impSIMDPopStack();
            retNode =
                gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToInt32RoundToZero, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConvertToInt64:
        case NI_Vector128_ConvertToInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_ConvertToInt64RoundToZeroScalar
                                        : NI_AdvSimd_Arm64_ConvertToInt64RoundToZero;

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConvertToSingle:
        case NI_Vector128_ConvertToSingle:
        {
            assert(sig->numArgs == 1);
            assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToSingle, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ConvertToUInt32:
        case NI_Vector128_ConvertToUInt32:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_FLOAT);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToUInt32RoundToZero, simdBaseJitType,
                                               simdSize);
            break;
        }

        case NI_Vector64_ConvertToUInt64:
        case NI_Vector128_ConvertToUInt64:
        {
            assert(sig->numArgs == 1);
            assert(simdBaseType == TYP_DOUBLE);

            intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_ConvertToUInt64RoundToZeroScalar
                                        : NI_AdvSimd_Arm64_ConvertToUInt64RoundToZero;

            op1     = impSIMDPopStack();
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Create:
        case NI_Vector128_Create:
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
                // Some of the below code assumes 8 or 16 byte SIMD types
                assert((simdSize == 8) || (simdSize == 16));

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
                            cnsVal = static_cast<double>(impPopStack().val->AsDblCon()->DconValue());
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

        case NI_Vector64_CreateScalar:
        case NI_Vector128_CreateScalar:
        {
            assert(sig->numArgs == 1);

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_CreateSequence:
        case NI_Vector128_CreateSequence:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->OperIsConst())
            {
                // TODO-ARM64-CQ: We should support long/ulong multiplication.
                break;
            }

            impSpillSideEffect(true, verCurrentState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdCreateSequenceNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_CreateScalarUnsafe:
        case NI_Vector128_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

            op1     = impPopStack().val;
            retNode = gtNewSimdCreateScalarUnsafeNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Divide:
        case NI_Vector128_Divide:
        case NI_Vector64_op_Division:
        case NI_Vector128_op_Division:
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

        case NI_Vector64_Dot:
        case NI_Vector128_Dot:
        {
            assert(sig->numArgs == 2);

            if (!varTypeIsLong(simdBaseType))
            {
                var_types simdType = getSIMDTypeForSize(simdSize);

                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
                retNode = gtNewSimdGetElementNode(retType, retNode, gtNewIconNode(0), simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector64_Equals:
        case NI_Vector128_Equals:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_EqualsAll:
        case NI_Vector128_EqualsAll:
        case NI_Vector64_op_Equality:
        case NI_Vector128_op_Equality:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_EqualsAny:
        case NI_Vector128_EqualsAny:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ExtractMostSignificantBits:
        case NI_Vector128_ExtractMostSignificantBits:
        {
            assert(sig->numArgs == 1);

            // ARM64 doesn't have a single instruction that performs the behavior so we'll emulate it instead.
            // To do this, we effectively perform the following steps:
            // 1. tmp = input & 0x80         ; and the input to clear all but the most significant bit
            // 2. tmp = tmp >> index         ; right shift each element by its index
            // 3. tmp = sum(tmp)             ; sum the elements together

            // For byte/sbyte, we also need to handle the fact that we can only shift by up to 8
            // but for Vector128, we have 16 elements to handle. In that scenario, we will simply
            // extract both scalars, and combine them via: (upper << 8) | lower

            var_types simdType = getSIMDTypeForSize(simdSize);

            op1 = impSIMDPopStack();

            GenTreeVecCon* vecCon2 = gtNewVconNode(simdType);
            GenTreeVecCon* vecCon3 = gtNewVconNode(simdType);

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                {
                    simdBaseType    = TYP_UBYTE;
                    simdBaseJitType = CORINFO_TYPE_UBYTE;

                    vecCon2->gtSimdVal.u64[0] = 0x8080808080808080;
                    vecCon3->gtSimdVal.u64[0] = 0x00FFFEFDFCFBFAF9;

                    if (simdSize == 16)
                    {
                        vecCon2->gtSimdVal.u64[1] = 0x8080808080808080;
                        vecCon3->gtSimdVal.u64[1] = 0x00FFFEFDFCFBFAF9;
                    }
                    break;
                }

                case TYP_SHORT:
                case TYP_USHORT:
                {
                    simdBaseType    = TYP_USHORT;
                    simdBaseJitType = CORINFO_TYPE_USHORT;

                    vecCon2->gtSimdVal.u64[0] = 0x8000800080008000;
                    vecCon3->gtSimdVal.u64[0] = 0xFFF4FFF3FFF2FFF1;

                    if (simdSize == 16)
                    {
                        vecCon2->gtSimdVal.u64[1] = 0x8000800080008000;
                        vecCon3->gtSimdVal.u64[1] = 0xFFF8FFF7FFF6FFF5;
                    }
                    break;
                }

                case TYP_INT:
                case TYP_UINT:
                case TYP_FLOAT:
                {
                    simdBaseType    = TYP_INT;
                    simdBaseJitType = CORINFO_TYPE_INT;

                    vecCon2->gtSimdVal.u64[0] = 0x8000000080000000;
                    vecCon3->gtSimdVal.u64[0] = 0xFFFFFFE2FFFFFFE1;

                    if (simdSize == 16)
                    {
                        vecCon2->gtSimdVal.u64[1] = 0x8000000080000000;
                        vecCon3->gtSimdVal.u64[1] = 0xFFFFFFE4FFFFFFE3;
                    }
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                case TYP_DOUBLE:
                {
                    simdBaseType    = TYP_LONG;
                    simdBaseJitType = CORINFO_TYPE_LONG;

                    vecCon2->gtSimdVal.u64[0] = 0x8000000000000000;
                    vecCon3->gtSimdVal.u64[0] = 0xFFFFFFFFFFFFFFC1;

                    if (simdSize == 16)
                    {
                        vecCon2->gtSimdVal.u64[1] = 0x8000000000000000;
                        vecCon3->gtSimdVal.u64[1] = 0xFFFFFFFFFFFFFFC2;
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            op3 = vecCon3;
            op2 = vecCon2;
            op1 = gtNewSimdHWIntrinsicNode(simdType, op1, op2, NI_AdvSimd_And, simdBaseJitType, simdSize);

            NamedIntrinsic shiftIntrinsic = NI_AdvSimd_ShiftLogical;

            if ((simdSize == 8) && varTypeIsLong(simdBaseType))
            {
                shiftIntrinsic = NI_AdvSimd_ShiftLogicalScalar;
            }

            op1 = gtNewSimdHWIntrinsicNode(simdType, op1, op3, shiftIntrinsic, simdBaseJitType, simdSize);

            if (varTypeIsByte(simdBaseType) && (simdSize == 16))
            {
                op1 = impCloneExpr(op1, &op2, CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone op1 for vector extractmostsignificantbits"));

                op1 = gtNewSimdGetLowerNode(TYP_SIMD8, op1, simdBaseJitType, simdSize);
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, 8);
                op1 = gtNewSimdToScalarNode(genActualType(simdBaseType), op1, simdBaseJitType, 8);
                op1 = gtNewCastNode(TYP_INT, op1, /* isUnsigned */ true, TYP_INT);

                GenTree* zero  = gtNewZeroConNode(TYP_SIMD16);
                ssize_t  index = 8 / genTypeSize(simdBaseType);

                op2 = gtNewSimdGetUpperNode(TYP_SIMD8, op2, simdBaseJitType, simdSize);
                op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op2, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, 8);
                op2 = gtNewSimdToScalarNode(genActualType(simdBaseType), op2, simdBaseJitType, 8);
                op2 = gtNewCastNode(TYP_INT, op2, /* isUnsigned */ true, TYP_INT);

                op2     = gtNewOperNode(GT_LSH, TYP_INT, op2, gtNewIconNode(8));
                retNode = gtNewOperNode(GT_OR, TYP_INT, op1, op2);
            }
            else
            {
                if (!varTypeIsLong(simdBaseType))
                {
                    if ((simdSize == 8) && ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT)))
                    {
                        op1 = impCloneExpr(op1, &op2, CHECK_SPILL_ALL,
                                           nullptr DEBUGARG("Clone op1 for vector extractmostsignificantbits"));
                        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, op2, NI_AdvSimd_AddPairwise, simdBaseJitType,
                                                       simdSize);
                    }
                    else
                    {
                        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType,
                                                       simdSize);
                    }
                }
                else if (simdSize == 16)
                {
                    op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddPairwiseScalar, simdBaseJitType,
                                                   simdSize);
                }

                retNode = gtNewSimdToScalarNode(genActualType(simdBaseType), op1, simdBaseJitType, 8);

                if ((simdBaseType != TYP_INT) && (simdBaseType != TYP_UINT))
                {
                    retNode = gtNewCastNode(TYP_INT, retNode, /* isUnsigned */ true, TYP_INT);
                }
            }
            break;
        }

        case NI_Vector64_Floor:
        case NI_Vector128_Floor:
        {
            assert(sig->numArgs == 1);

            if (!varTypeIsFloating(simdBaseType))
            {
                retNode = impSIMDPopStack();
                break;
            }

            op1     = impSIMDPopStack();
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_get_AllBitsSet:
        case NI_Vector128_get_AllBitsSet:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewAllBitsSetConNode(retType);
            break;
        }

        case NI_Vector64_get_One:
        case NI_Vector128_get_One:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewOneConNode(retType, simdBaseType);
            break;
        }

        case NI_Vector64_get_Zero:
        case NI_Vector128_get_Zero:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewZeroConNode(retType);
            break;
        }

        case NI_Vector64_GetElement:
        case NI_Vector128_GetElement:
        {
            assert(!sig->hasThis());
            assert(numArgs == 2);

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_GetLower:
        {
            assert(sig->numArgs == 1);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetLowerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_GetUpper:
        {
            assert(sig->numArgs == 1);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdGetUpperNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThan:
        case NI_Vector128_GreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThanAll:
        case NI_Vector128_GreaterThanAll:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThanAny:
        case NI_Vector128_GreaterThanAny:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThanOrEqual:
        case NI_Vector128_GreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThanOrEqualAll:
        case NI_Vector128_GreaterThanOrEqualAll:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_GreaterThanOrEqualAny:
        case NI_Vector128_GreaterThanOrEqualAny:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThan:
        case NI_Vector128_LessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThanAll:
        case NI_Vector128_LessThanAll:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThanAny:
        case NI_Vector128_LessThanAny:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThanOrEqual:
        case NI_Vector128_LessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThanOrEqualAll:
        case NI_Vector128_LessThanOrEqualAll:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LessThanOrEqualAny:
        case NI_Vector128_LessThanOrEqualAny:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_AdvSimd_LoadVector64:
        case NI_AdvSimd_LoadVector128:
        case NI_Vector64_Load:
        case NI_Vector128_Load:
        case NI_Vector64_LoadUnsafe:
        case NI_Vector128_LoadUnsafe:
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

        case NI_Vector64_LoadAligned:
        case NI_Vector128_LoadAligned:
        {
            assert(sig->numArgs == 1);

            if (opts.OptimizationDisabled())
            {
                // ARM64 doesn't have aligned loads, but aligned loads are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadAlignedNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_LoadAlignedNonTemporal:
        case NI_Vector128_LoadAlignedNonTemporal:
        {
            assert(sig->numArgs == 1);

            if (opts.OptimizationDisabled())
            {
                // ARM64 doesn't have aligned loads, but aligned loads are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadNonTemporalNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Max:
        case NI_Vector128_Max:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Min:
        case NI_Vector128_Min:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Multiply:
        case NI_Vector128_Multiply:
        case NI_Vector64_op_Multiply:
        case NI_Vector128_op_Multiply:
        {
            assert(sig->numArgs == 2);

            if (varTypeIsLong(simdBaseType))
            {
                // TODO-ARM64-CQ: We should support long/ulong multiplication.
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

        case NI_Vector64_Narrow:
        case NI_Vector128_Narrow:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Negate:
        case NI_Vector128_Negate:
        case NI_Vector64_op_UnaryNegation:
        case NI_Vector128_op_UnaryNegation:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_OnesComplement:
        case NI_Vector128_OnesComplement:
        case NI_Vector64_op_OnesComplement:
        case NI_Vector128_op_OnesComplement:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack();
            retNode = gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_op_Inequality:
        case NI_Vector128_op_Inequality:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_op_UnaryPlus:
        case NI_Vector128_op_UnaryPlus:
        {
            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack();
            break;
        }

        case NI_Vector64_Subtract:
        case NI_Vector128_Subtract:
        case NI_Vector64_op_Subtraction:
        case NI_Vector128_op_Subtraction:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ShiftLeft:
        case NI_Vector128_ShiftLeft:
        case NI_Vector64_op_LeftShift:
        case NI_Vector128_op_LeftShift:
        {
            assert(sig->numArgs == 2);

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ShiftRightArithmetic:
        case NI_Vector128_ShiftRightArithmetic:
        case NI_Vector64_op_RightShift:
        case NI_Vector128_op_RightShift:
        {
            assert(sig->numArgs == 2);
            genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_ShiftRightLogical:
        case NI_Vector128_ShiftRightLogical:
        case NI_Vector64_op_UnsignedRightShift:
        case NI_Vector128_op_UnsignedRightShift:
        {
            assert(sig->numArgs == 2);

            op2 = impPopStack().val;
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Shuffle:
        case NI_Vector128_Shuffle:
        {
            assert((sig->numArgs == 2) || (sig->numArgs == 3));
            assert((simdSize == 8) || (simdSize == 16));

            GenTree* indices = impStackTop(0).val;

            if (!indices->IsVectorConst())
            {
                // TODO-ARM64-CQ: Handling non-constant indices is a bit more complex
                break;
            }

            if (sig->numArgs == 2)
            {
                op2 = impSIMDPopStack();
                op1 = impSIMDPopStack();

                retNode = gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector64_Sqrt:
        case NI_Vector128_Sqrt:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                op1     = impSIMDPopStack();
                retNode = gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_AdvSimd_Store:
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

        case NI_Vector64_Store:
        case NI_Vector64_StoreUnsafe:
        case NI_Vector128_Store:
        case NI_Vector128_StoreUnsafe:
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

        case NI_Vector64_StoreAligned:
        case NI_Vector128_StoreAligned:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

            if (opts.OptimizationDisabled())
            {
                // ARM64 doesn't have aligned stores, but aligned stores are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }

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

        case NI_Vector64_StoreAlignedNonTemporal:
        case NI_Vector128_StoreAlignedNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

            if (opts.OptimizationDisabled())
            {
                // ARM64 doesn't have aligned stores, but aligned stores are only validated to be
                // aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                break;
            }

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

        case NI_AdvSimd_StoreVector64x2AndZip:
        case NI_AdvSimd_StoreVector64x3AndZip:
        case NI_AdvSimd_StoreVector64x4AndZip:
        case NI_AdvSimd_Arm64_StoreVector128x2AndZip:
        case NI_AdvSimd_Arm64_StoreVector128x3AndZip:
        case NI_AdvSimd_Arm64_StoreVector128x4AndZip:
        case NI_AdvSimd_StoreVector64x2:
        case NI_AdvSimd_StoreVector64x3:
        case NI_AdvSimd_StoreVector64x4:
        case NI_AdvSimd_Arm64_StoreVector128x2:
        case NI_AdvSimd_Arm64_StoreVector128x3:
        case NI_AdvSimd_Arm64_StoreVector128x4:
        {
            assert(sig->numArgs == 2);
            assert(retType == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType             = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                 = impPopStack().val;
            unsigned fieldCount = info.compCompHnd->getClassNumInstanceFields(argClass);
            argType             = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1                 = getArgForHWIntrinsic(argType, argClass);

            assert(op2->TypeGet() == TYP_STRUCT);
            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            if (!op2->OperIs(GT_LCL_VAR))
            {
                unsigned tmp = lvaGrabTemp(true DEBUGARG("StoreVectorNx2 temp tree"));

                impStoreTemp(tmp, op2, CHECK_SPILL_NONE);
                op2 = gtNewLclvNode(tmp, argType);
            }
            op2 = gtConvertTableOpToFieldList(op2, fieldCount);

            info.compNeedsConsecutiveRegisters = true;
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_AdvSimd_StoreSelectedScalar:
        case NI_AdvSimd_Arm64_StoreSelectedScalar:
        {
            assert(sig->numArgs == 3);
            assert(retType == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3     = info.compCompHnd->getArgNext(arg2);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;
            argType                = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3                    = impPopStack().val;
            argType                = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                    = impPopStack().val;
            unsigned fieldCount    = info.compCompHnd->getClassNumInstanceFields(argClass);
            int      immLowerBound = 0;
            int      immUpperBound = 0;

            if (op2->TypeGet() == TYP_STRUCT)
            {
                info.compNeedsConsecutiveRegisters = true;
                switch (fieldCount)
                {
                    case 2:
                        intrinsic = simdSize == 8 ? NI_AdvSimd_StoreSelectedScalarVector64x2
                                                  : NI_AdvSimd_Arm64_StoreSelectedScalarVector128x2;
                        break;
                    case 3:
                        intrinsic = simdSize == 8 ? NI_AdvSimd_StoreSelectedScalarVector64x3
                                                  : NI_AdvSimd_Arm64_StoreSelectedScalarVector128x3;
                        break;
                    case 4:
                        intrinsic = simdSize == 8 ? NI_AdvSimd_StoreSelectedScalarVector64x4
                                                  : NI_AdvSimd_Arm64_StoreSelectedScalarVector128x4;
                        break;
                    default:
                        assert("unsupported");
                }

                if (!op2->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("StoreSelectedScalarN"));

                    impStoreTemp(tmp, op2, CHECK_SPILL_NONE);
                    op2 = gtNewLclvNode(tmp, argType);
                }
                op2 = gtConvertTableOpToFieldList(op2, fieldCount);
            }
            else
            {
                // While storing from a single vector, both Vector128 and Vector64 API calls are in AdvSimd class.
                // Thus, we get simdSize as 8 for both of the calls. We re-calculate that simd size for such API calls.
                getBaseJitTypeAndSizeOfSIMDType(argClass, &simdSize);
            }

            assert(HWIntrinsicInfo::isImmOp(intrinsic, op3));
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, &immLowerBound, &immUpperBound);
            op3     = addRangeCheckIfNeeded(intrinsic, op3, (!op3->IsCnsIntOrI()), immLowerBound, immUpperBound);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op1 = op1->gtGetOp1();
                }
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Sum:
        case NI_Vector128_Sum:
        {
            assert(sig->numArgs == 1);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op1     = impSIMDPopStack();
            retNode = gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_WidenLower:
        case NI_Vector128_WidenLower:
        {
            assert(sig->numArgs == 1);

            op1 = impSIMDPopStack();

            retNode = gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_WidenUpper:
        case NI_Vector128_WidenUpper:
        {
            assert(sig->numArgs == 1);

            op1 = impSIMDPopStack();

            retNode = gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_WithElement:
        case NI_Vector128_WithElement:
        {
            assert(numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->OperIsConst())
            {
                // TODO-ARM64-CQ: We should always import these like we do with GetElement
                // If index is not constant use software fallback.
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if ((imm8 >= count) || (imm8 < 0))
            {
                // Using software fallback if index is out of range (throw exception)
                return nullptr;
            }

            GenTree* valueOp = impPopStack().val;
            impPopStack(); // pop the indexOp that we already have.
            GenTree* vectorOp = impSIMDPopStack();

            retNode = gtNewSimdWithElementNode(retType, vectorOp, indexOp, valueOp, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_WithLower:
        {
            assert(sig->numArgs == 2);

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithLowerNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector128_WithUpper:
        {
            assert(sig->numArgs == 2);

            op2     = impSIMDPopStack();
            op1     = impSIMDPopStack();
            retNode = gtNewSimdWithUpperNode(retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_Xor:
        case NI_Vector128_Xor:
        case NI_Vector64_op_ExclusiveOr:
        case NI_Vector128_op_ExclusiveOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize);
            break;
        }

        case NI_AdvSimd_LoadVector64x2AndUnzip:
        case NI_AdvSimd_LoadVector64x3AndUnzip:
        case NI_AdvSimd_LoadVector64x4AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x2AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x3AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x4AndUnzip:
        case NI_AdvSimd_LoadVector64x2:
        case NI_AdvSimd_LoadVector64x3:
        case NI_AdvSimd_LoadVector64x4:
        case NI_AdvSimd_Arm64_LoadVector128x2:
        case NI_AdvSimd_Arm64_LoadVector128x3:
        case NI_AdvSimd_Arm64_LoadVector128x4:
        case NI_AdvSimd_LoadAndReplicateToVector64x2:
        case NI_AdvSimd_LoadAndReplicateToVector64x3:
        case NI_AdvSimd_LoadAndReplicateToVector64x4:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x2:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x3:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x4:
            info.compNeedsConsecutiveRegisters = true;
            FALLTHROUGH;
        case NI_AdvSimd_Arm64_LoadPairScalarVector64:
        case NI_AdvSimd_Arm64_LoadPairScalarVector64NonTemporal:
        case NI_AdvSimd_Arm64_LoadPairVector128:
        case NI_AdvSimd_Arm64_LoadPairVector128NonTemporal:
        case NI_AdvSimd_Arm64_LoadPairVector64:
        case NI_AdvSimd_Arm64_LoadPairVector64NonTemporal:
        {
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

            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));

            op1     = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
            retNode = impStoreMultiRegValueToVar(op1, sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
            break;
        }
        case NI_AdvSimd_LoadAndInsertScalarVector64x2:
        case NI_AdvSimd_LoadAndInsertScalarVector64x3:
        case NI_AdvSimd_LoadAndInsertScalarVector64x4:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
        {
            assert(sig->numArgs == 3);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3     = info.compCompHnd->getArgNext(arg2);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = impPopStack().val;

            if (op3->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op3->gtGetOp1()->TypeGet() == TYP_BYREF)
                {
                    op3 = op3->gtGetOp1();
                }
            }

            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));
            assert(op1->TypeGet() == TYP_STRUCT);

            info.compNeedsConsecutiveRegisters = true;
            unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

            if (!op1->OperIs(GT_LCL_VAR))
            {
                unsigned tmp = lvaGrabTemp(true DEBUGARG("LoadAndInsertScalar temp tree"));

                impStoreTemp(tmp, op1, CHECK_SPILL_NONE);
                op1 = gtNewLclvNode(tmp, argType);
            }

            op1     = gtConvertParamOpToFieldList(op1, fieldCount, argClass);
            op1     = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            retNode = impStoreMultiRegValueToVar(op1, sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
            break;
        }
        case NI_AdvSimd_VectorTableLookup:
        case NI_AdvSimd_Arm64_VectorTableLookup:
        {
            assert(sig->numArgs == 2);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = impPopStack().val;

            if (op1->TypeGet() == TYP_STRUCT)
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

                if (!op1->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("VectorTableLookup temp tree"));

                    impStoreTemp(tmp, op1, CHECK_SPILL_NONE);
                    op1 = gtNewLclvNode(tmp, argType);
                }

                op1 = gtConvertTableOpToFieldList(op1, fieldCount);
            }
            else
            {
                assert(varTypeIsSIMD(op1->TypeGet()));
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseJitType, simdSize);
            break;
        }
        case NI_AdvSimd_VectorTableLookupExtension:
        case NI_AdvSimd_Arm64_VectorTableLookupExtension:
        {
            assert(sig->numArgs == 3);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3     = info.compCompHnd->getArgNext(arg2);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = impPopStack().val;
            op1     = impPopStack().val;

            if (op2->TypeGet() == TYP_STRUCT)
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

                if (!op2->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("VectorTableLookupExtension temp tree"));

                    impStoreTemp(tmp, op2, CHECK_SPILL_NONE);
                    op2 = gtNewLclvNode(tmp, argType);
                }

                op2 = gtConvertTableOpToFieldList(op2, fieldCount);
            }
            else
            {
                assert(varTypeIsSIMD(op1->TypeGet()));
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        default:
        {
            return nullptr;
        }
    }

    return retNode;
}

//------------------------------------------------------------------------
// convertHWIntrinsicFromMask: Convert a HW instrinsic vector node to a mask
//
// Arguments:
//    node            -- The node to convert
//    simdBaseJitType -- the base jit type of the converted node
//    simdSize        -- the simd size of the converted node
//
// Return Value:
//    The node converted to the a mask type
//
GenTree* Compiler::convertHWIntrinsicToMask(var_types   type,
                                            GenTree*    node,
                                            CorInfoType simdBaseJitType,
                                            unsigned    simdSize)
{
    // ConvertVectorToMask uses cmpne which requires an embedded mask.
    // TODO-SVE: Refactor this out once full embedded masking is adding.
    NamedIntrinsic maskName;
    switch (simdBaseJitType)
    {
        case CORINFO_TYPE_UBYTE:
            maskName = NI_Sve_CreateTrueMaskAllByte;
            break;

        case CORINFO_TYPE_DOUBLE:
            maskName = NI_Sve_CreateTrueMaskAllDouble;
            break;

        case CORINFO_TYPE_SHORT:
            maskName = NI_Sve_CreateTrueMaskAllInt16;
            break;

        case CORINFO_TYPE_INT:
            maskName = NI_Sve_CreateTrueMaskAllInt32;
            break;

        case CORINFO_TYPE_LONG:
            maskName = NI_Sve_CreateTrueMaskAllInt64;
            break;

        case CORINFO_TYPE_BYTE:
            maskName = NI_Sve_CreateTrueMaskAllSByte;
            break;

        case CORINFO_TYPE_FLOAT:
            maskName = NI_Sve_CreateTrueMaskAllSingle;
            break;

        case CORINFO_TYPE_USHORT:
            maskName = NI_Sve_CreateTrueMaskAllUInt16;
            break;

        case CORINFO_TYPE_UINT:
            maskName = NI_Sve_CreateTrueMaskAllUInt32;
            break;

        case CORINFO_TYPE_ULONG:
            maskName = NI_Sve_CreateTrueMaskAllUInt64;
            break;

        default:
            unreached();
    }
    GenTree* embeddedMask = gtNewSimdHWIntrinsicNode(TYP_MASK, maskName, simdBaseJitType, simdSize);
    return gtNewSimdHWIntrinsicNode(TYP_MASK, embeddedMask, node, NI_Sve_ConvertVectorToMask, simdBaseJitType,
                                    simdSize);
}

//------------------------------------------------------------------------
// convertHWIntrinsicFromMask: Convert a HW instrinsic mask node to a vector
//
// Arguments:
//    node          -- The node to convert
//    type          -- The type of the node to convert to
//
// Return Value:
//    The node converted to the given type
//
GenTree* Compiler::convertHWIntrinsicFromMask(GenTreeHWIntrinsic* node, var_types type)
{
    assert(node->TypeGet() == TYP_MASK);
    return gtNewSimdHWIntrinsicNode(type, node, NI_Sve_ConvertMaskToVector, node->GetSimdBaseJitType(),
                                    node->GetSimdSize());
}

#endif // FEATURE_HW_INTRINSICS
