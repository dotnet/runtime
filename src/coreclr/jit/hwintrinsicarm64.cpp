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
            case NI_AdvSimd_StoreSelectedScalar:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Arm64_InsertSelectedScalar:
                immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
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
    HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);
    int                 numArgs  = sig->numArgs;

    if (!featureSIMD || !IsBaselineSimdIsaSupported())
    {
        return nullptr;
    }

    assert(numArgs >= 0);

    var_types simdBaseType = TYP_UNKNOWN;

    if (intrinsic != NI_ArmBase_Yield)
    {
        simdBaseType = JitType2PreciseVarType(simdBaseJitType);
        assert(varTypeIsArithmetic(simdBaseType));
    }

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;

    switch (intrinsic)
    {
        case NI_ArmBase_Yield:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewScalarHWIntrinsicNode(TYP_VOID, intrinsic);
            break;
        }

        case NI_Vector64_Abs:
        case NI_Vector128_Abs:
        {
            assert(sig->numArgs == 1);
            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Add:
        case NI_Vector128_Add:
        case NI_Vector64_op_Addition:
        case NI_Vector128_op_Addition:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_AndNot:
        case NI_Vector128_AndNot:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

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

            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            retNode = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));
            break;
        }

        case NI_Vector64_BitwiseAnd:
        case NI_Vector128_BitwiseAnd:
        case NI_Vector64_op_BitwiseAnd:
        case NI_Vector128_op_BitwiseAnd:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_BitwiseOr:
        case NI_Vector128_BitwiseOr:
        case NI_Vector64_op_BitwiseOr:
        case NI_Vector128_op_BitwiseOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Ceiling:
        case NI_Vector128_Ceiling:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsFloating(simdBaseType));

            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_ConditionalSelect:
        case NI_Vector128_ConditionalSelect:
        {
            assert(sig->numArgs == 3);

            op3 = impSIMDPopStack(retType);
            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode =
                gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_ConvertToDouble:
        case NI_Vector128_ConvertToDouble:
        case NI_Vector64_ConvertToInt32:
        case NI_Vector128_ConvertToInt32:
        case NI_Vector64_ConvertToInt64:
        case NI_Vector128_ConvertToInt64:
        case NI_Vector64_ConvertToSingle:
        case NI_Vector128_ConvertToSingle:
        case NI_Vector64_ConvertToUInt32:
        case NI_Vector128_ConvertToUInt32:
        case NI_Vector64_ConvertToUInt64:
        case NI_Vector128_ConvertToUInt64:
        {
            assert(sig->numArgs == 1);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_Create:
        case NI_Vector128_Create:
        {
            // We shouldn't handle this as an intrinsic if the
            // respective ISAs have been disabled by the user.

            IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), sig->numArgs);

            for (int i = sig->numArgs - 1; i >= 0; i--)
            {
                nodeBuilder.AddOperand(i, impPopStack().val);
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, std::move(nodeBuilder), intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_get_Count:
        case NI_Vector128_get_Count:
        {
            assert(!sig->hasThis());
            assert(numArgs == 0);

            GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
            countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
            retNode = countNode;
            break;
        }

        case NI_Vector64_Divide:
        case NI_Vector128_Divide:
        case NI_Vector64_op_Division:
        case NI_Vector128_op_Division:
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

        case NI_Vector64_Dot:
        case NI_Vector128_Dot:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode =
                gtNewSimdDotProdNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Equals:
        case NI_Vector128_Equals:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_EqualsAll:
        case NI_Vector128_EqualsAll:
        case NI_Vector64_op_Equality:
        case NI_Vector128_op_Equality:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            retNode = gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_EqualsAny:
        case NI_Vector128_EqualsAny:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_Floor:
        case NI_Vector128_Floor:
        {
            assert(sig->numArgs == 1);
            assert(varTypeIsFloating(simdBaseType));

            op1     = impSIMDPopStack(retType);
            retNode = gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_get_AllBitsSet:
        case NI_Vector128_get_AllBitsSet:
        {
            assert(!sig->hasThis());
            assert(numArgs == 0);

            retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, simdBaseJitType, simdSize);
            break;
        }

        case NI_Vector64_get_Zero:
        case NI_Vector128_get_Zero:
        {
            assert(sig->numArgs == 0);
            retNode = gtNewSimdZeroNode(retType, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_GetElement:
        case NI_Vector128_GetElement:
        {
            assert(!sig->hasThis());
            assert(numArgs == 2);

            op2 = impPopStack().val;
            op1 = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            const bool isSimdAsHWIntrinsic = true;
            retNode = gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize, isSimdAsHWIntrinsic);
            break;
        }

        case NI_Vector128_GetUpper:
        {
            // Converts to equivalent managed code:
            //   AdvSimd.ExtractVector128(vector, Vector128<T>.Zero, 8 / sizeof(T)).GetLower();
            assert(numArgs == 1);
            op1            = impPopStack().val;
            GenTree* zero  = gtNewSimdHWIntrinsicNode(retType, NI_Vector128_get_Zero, simdBaseJitType, simdSize);
            ssize_t  index = 8 / genTypeSize(simdBaseType);

            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, zero, gtNewIconNode(index), NI_AdvSimd_ExtractVector128,
                                               simdBaseJitType, simdSize);
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD8, retNode, NI_Vector128_GetLower, simdBaseJitType, 8);
            break;
        }

        case NI_Vector64_GreaterThan:
        case NI_Vector128_GreaterThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_GreaterThanAll:
        case NI_Vector128_GreaterThanAll:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_GreaterThanAny:
        case NI_Vector128_GreaterThanAny:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_GreaterThanOrEqual:
        case NI_Vector128_GreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_GreaterThanOrEqualAll:
        case NI_Vector128_GreaterThanOrEqualAll:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_GreaterThanOrEqualAny:
        case NI_Vector128_GreaterThanOrEqualAny:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_LessThan:
        case NI_Vector128_LessThan:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_LessThanAll:
        case NI_Vector128_LessThanAll:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_LessThanAny:
        case NI_Vector128_LessThanAny:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_LessThanOrEqual:
        case NI_Vector128_LessThanOrEqual:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_LessThanOrEqualAll:
        case NI_Vector128_LessThanOrEqualAll:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_LessThanOrEqualAny:
        case NI_Vector128_LessThanOrEqualAny:
        {
            assert(sig->numArgs == 2);
            // TODO-ARM64-CQ: These intrinsics should be accelerated.
            break;
        }

        case NI_Vector64_Max:
        case NI_Vector128_Max:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Min:
        case NI_Vector128_Min:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
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

            retNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Narrow:
        case NI_Vector128_Narrow:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode =
                gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Negate:
        case NI_Vector128_Negate:
        case NI_Vector64_op_UnaryNegation:
        case NI_Vector128_op_UnaryNegation:
        {
            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack(retType);
            retNode =
                gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_OnesComplement:
        case NI_Vector128_OnesComplement:
        case NI_Vector64_op_OnesComplement:
        case NI_Vector128_op_OnesComplement:
        {
            assert(sig->numArgs == 1);
            op1 = impSIMDPopStack(retType);
            retNode =
                gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_op_Inequality:
        case NI_Vector128_op_Inequality:
        {
            assert(sig->numArgs == 2);
            var_types simdType = getSIMDTypeForSize(simdSize);

            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            retNode = gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_op_UnaryPlus:
        case NI_Vector128_op_UnaryPlus:
        {
            assert(sig->numArgs == 1);
            retNode = impSIMDPopStack(retType);
            break;
        }

        case NI_Vector64_Subtract:
        case NI_Vector128_Subtract:
        case NI_Vector64_op_Subtraction:
        case NI_Vector128_op_Subtraction:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_Sqrt:
        case NI_Vector128_Sqrt:
        {
            assert(sig->numArgs == 1);

            if (varTypeIsFloating(simdBaseType))
            {
                op1     = impSIMDPopStack(retType);
                retNode = gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            }
            break;
        }

        case NI_Vector64_WidenLower:
        case NI_Vector128_WidenLower:
        {
            assert(sig->numArgs == 1);

            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_WidenUpper:
        case NI_Vector128_WidenUpper:
        {
            assert(sig->numArgs == 1);

            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ false);
            break;
        }

        case NI_Vector64_WithElement:
        case NI_Vector128_WithElement:
        {
            assert(numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;
            if (!indexOp->OperIsConst())
            {
                // TODO-XARCH-CQ: We should always import these like we do with GetElement
                // If index is not constant use software fallback.
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if (imm8 >= count || imm8 < 0)
            {
                // Using software fallback if index is out of range (throw exeception)
                return nullptr;
            }

            GenTree* valueOp = impPopStack().val;
            impPopStack(); // pop the indexOp that we already have.
            GenTree* vectorOp = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            retNode = gtNewSimdWithElementNode(retType, vectorOp, indexOp, valueOp, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
            break;
        }

        case NI_Vector64_Xor:
        case NI_Vector128_Xor:
        case NI_Vector64_op_ExclusiveOr:
        case NI_Vector128_op_ExclusiveOr:
        {
            assert(sig->numArgs == 2);

            op2 = impSIMDPopStack(retType);
            op1 = impSIMDPopStack(retType);

            retNode = gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize,
                                         /* isSimdAsHWIntrinsic */ false);
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
