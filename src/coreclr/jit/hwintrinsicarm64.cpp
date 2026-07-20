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
        case InstructionSet_Sve2:
            return InstructionSet_Sve2_Arm64;
        case InstructionSet_Sha3:
            return InstructionSet_Sha3_Arm64;
        case InstructionSet_Sm4:
            return InstructionSet_Sm4_Arm64;
        case InstructionSet_SveAes:
            return InstructionSet_SveAes_Arm64;
        case InstructionSet_SveSha3:
            return InstructionSet_SveSha3_Arm64;
        case InstructionSet_SveSm4:
            return InstructionSet_SveSm4_Arm64;
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
CORINFO_InstructionSet Compiler::lookupInstructionSet(const char* className)
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
        if (strcmp(className, "Sve2") == 0)
        {
            return InstructionSet_Sve2;
        }
        if (strcmp(className, "Sve") == 0)
        {
            return InstructionSet_Sve;
        }
        if (strcmp(className, "Sha3") == 0)
        {
            return InstructionSet_Sha3;
        }
        if (strcmp(className, "Sm4") == 0)
        {
            return InstructionSet_Sm4;
        }
        if (strcmp(className, "SveAes") == 0)
        {
            return InstructionSet_SveAes;
        }
        if (strcmp(className, "SveSha3") == 0)
        {
            return InstructionSet_SveSha3;
        }
        if (strcmp(className, "SveSm4") == 0)
        {
            return InstructionSet_SveSm4;
        }
    }
    else if (className[0] == 'V')
    {
        if (strncmp(className, "Vector", 6) == 0)
        {
            const char* suffix = className + 6;

            if ((*suffix == '\0') || (strcmp(suffix, "`1") == 0))
            {
                return InstructionSet_VectorT;
            }
            else if (strncmp(suffix, "64", 2) == 0)
            {
                return InstructionSet_Vector64;
            }
            else if (strncmp(suffix, "128", 3) == 0)
            {
                return InstructionSet_Vector128;
            }
        }
    }

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclsoing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    innerEnclosingClassName -- The name of the inner enclosing class or nullptr if one doesn't exist
//    outerEnclosingClassName -- The name of the outer enclosing class or nullptr if one doesn't exist
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
//
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

    if (strcmp(className, "Arm64") == 0)
    {
        return Arm64VersionOfIsa(enclosingIsa);
    }

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIval: Gets a the implicit immediate value for the given intrinsic
//
// Arguments:
//    id           - The intrinsic for which to get the ival
//
// Return Value:
//    The immediate value for the given intrinsic or -1 if none exists
int HWIntrinsicInfo::lookupIval(NamedIntrinsic id)
{
    switch (id)
    {
        case NI_Sve_Compute16BitAddresses:
            return 1;
        case NI_Sve_Compute32BitAddresses:
            return 2;
        case NI_Sve_Compute64BitAddresses:
            return 3;
        case NI_Sve_Compute8BitAddresses:
            return 0;
        default:
            unreached();
    }
    return -1;
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
    if (!HWIntrinsicInfo::HasImmediateOperand(intrinsic))
    {
        return;
    }

    // Position of the immediates from top of stack
    int imm1Pos = -1;
    int imm2Pos = -1;

    HWIntrinsicInfo::GetImmOpsPositions(intrinsic, sig, &imm1Pos, &imm2Pos);

    if (imm1Pos >= 0)
    {
        *immOp1Ptr = impStackTop(imm1Pos).val;
        assert(HWIntrinsicInfo::isImmOp(intrinsic, *immOp1Ptr));
    }

    if (imm2Pos >= 0)
    {
        *immOp2Ptr = impStackTop(imm2Pos).val;
        assert(HWIntrinsicInfo::isImmOp(intrinsic, *immOp2Ptr));
    }
}

//------------------------------------------------------------------------
// getHWIntrinsicImmTypes: Gets the type/size for an immediate for an intrinsic
//                         if it differs from the default type/size of the instrinsic
//
// Arguments:
//    intrinsic                -- NamedIntrinsic associated with the HWIntrinsic to lookup
//    sig                      -- signature of the intrinsic call.
//    immNumber                -- Which immediate to use (1 for most intrinsics)
//    immSimdSize [IN/OUT]     -- Size of the immediate to override
//    immSimdBaseType [IN/OUT] -- Base type of the immediate to override
//
void Compiler::getHWIntrinsicImmTypes(NamedIntrinsic    intrinsic,
                                      CORINFO_SIG_INFO* sig,
                                      unsigned          immNumber,
                                      unsigned*         immSimdSize,
                                      var_types*        immSimdBaseType)
{
    HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);

    if (category == HW_Category_SIMDByIndexedElement)
    {
        assert(immNumber == 1);
        *immSimdSize                   = 0;
        CORINFO_ARG_LIST_HANDLE immArg = sig->args;

        switch (sig->numArgs)
        {
            case 4:
                immArg = info.compCompHnd->getArgNext(immArg);
                FALLTHROUGH;
            case 3:
                immArg = info.compCompHnd->getArgNext(immArg);
                FALLTHROUGH;
            case 2:
            {
                CORINFO_CLASS_HANDLE typeHnd = info.compCompHnd->getArgClass(sig, immArg);
                getBaseTypeAndSizeOfSIMDType(typeHnd, immSimdSize);
                break;
            }
            default:
                unreached();
        }
    }
    else if (intrinsic == NI_AdvSimd_Arm64_InsertSelectedScalar)
    {
        if (immNumber == 2)
        {
            CORINFO_ARG_LIST_HANDLE immArg     = sig->args;
            immArg                             = info.compCompHnd->getArgNext(immArg);
            immArg                             = info.compCompHnd->getArgNext(immArg);
            CORINFO_CLASS_HANDLE typeHnd       = info.compCompHnd->getArgClass(sig, immArg);
            var_types            otherBaseType = getBaseTypeAndSizeOfSIMDType(typeHnd, immSimdSize);
            *immSimdBaseType                   = otherBaseType;
        }
        // For imm1 use default simd sizes.
    }

    // For all other imms, use default simd sizes
}

//------------------------------------------------------------------------
// lookupImmBounds: Gets the lower and upper bounds for the imm-value of a given NamedIntrinsic
//
// Arguments:
//    intrinsic -- NamedIntrinsic associated with the HWIntrinsic to lookup
//    simdType  -- vector size
//    baseType  -- base type of the Vector64/128<T>
//    immNumber -- which immediate operand to check for (most intrinsics only have one)
//    pImmLowerBound [OUT] - The lower incl. bound for a value of the intrinsic immediate operand
//    pImmUpperBound [OUT] - The upper incl. bound for a value of the intrinsic immediate operand
//
void HWIntrinsicInfo::lookupImmBounds(
    NamedIntrinsic intrinsic, int simdSize, var_types baseType, int immNumber, int* pImmLowerBound, int* pImmUpperBound)
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
        int size = genTypeSize(baseType);

        if (intrinsic == NI_Sve2_ShiftLeftLogicalWideningEven || intrinsic == NI_Sve2_ShiftLeftLogicalWideningOdd)
        {
            // Edge case for widening shifts. The base type is the wide type, but the maximum shift is the number
            // of bits in the narrow type.
            size /= 2;
        }

        // The left shift amount is in the range 0 to the element width in bits minus 1.
        immUpperBound = BITS_PER_BYTE * size - 1;
    }
    else if (category == HW_Category_ShiftRightByImmediate)
    {
        // The right shift amount, in the range 1 to the element width in bits.
        immLowerBound = 1;
        immUpperBound = BITS_PER_BYTE * genTypeSize(baseType);
    }
    else if (category == HW_Category_SIMDByIndexedElement)
    {
        switch (intrinsic)
        {
            case NI_Sve_DuplicateSelectedScalarToVector:
                // For SVE_DUP, the upper bound on index does not depend on the vector length.
                immUpperBound = (512 / (BITS_PER_BYTE * genTypeSize(baseType))) - 1;
                break;
            case NI_Sve2_MultiplyBySelectedScalarWideningEven:
            case NI_Sve2_MultiplyBySelectedScalarWideningEvenAndAdd:
            case NI_Sve2_MultiplyBySelectedScalarWideningEvenAndSubtract:
            case NI_Sve2_MultiplyBySelectedScalarWideningOdd:
            case NI_Sve2_MultiplyBySelectedScalarWideningOddAndAdd:
            case NI_Sve2_MultiplyBySelectedScalarWideningOddAndSubtract:
            case NI_Sve2_MultiplyDoublingWideningBySelectedScalarAndAddSaturateEven:
            case NI_Sve2_MultiplyDoublingWideningBySelectedScalarAndAddSaturateOdd:
            case NI_Sve2_MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateEven:
            case NI_Sve2_MultiplyDoublingWideningBySelectedScalarAndSubtractSaturateOdd:
            case NI_Sve2_MultiplyDoublingWideningSaturateEvenBySelectedScalar:
            case NI_Sve2_MultiplyDoublingWideningSaturateOddBySelectedScalar:
                // Index is on the half-width vector, hence double the maximum index.
                immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) * 2 - 1;
                break;
            default:
                immUpperBound = Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
                break;
        }
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
            case NI_AdvSimd_Arm64_StoreSelectedScalar:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Arm64_InsertSelectedScalar:
            case NI_Sve_FusedMultiplyAddBySelectedScalar:
            case NI_Sve_FusedMultiplySubtractBySelectedScalar:
            case NI_Sve_ExtractVector:
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
            case NI_Sve_Count16BitElements:
            case NI_Sve_Count32BitElements:
            case NI_Sve_Count64BitElements:
            case NI_Sve_Count8BitElements:
                immLowerBound = (int)SVE_PATTERN_POW2;
                immUpperBound = (int)SVE_PATTERN_ALL;
                break;

            case NI_Sve_SaturatingDecrementBy16BitElementCount:
            case NI_Sve_SaturatingDecrementBy32BitElementCount:
            case NI_Sve_SaturatingDecrementBy64BitElementCount:
            case NI_Sve_SaturatingDecrementBy8BitElementCount:
            case NI_Sve_SaturatingIncrementBy16BitElementCount:
            case NI_Sve_SaturatingIncrementBy32BitElementCount:
            case NI_Sve_SaturatingIncrementBy64BitElementCount:
            case NI_Sve_SaturatingIncrementBy8BitElementCount:
            case NI_Sve_SaturatingDecrementBy16BitElementCountScalar:
            case NI_Sve_SaturatingDecrementBy32BitElementCountScalar:
            case NI_Sve_SaturatingDecrementBy64BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy16BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy32BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy64BitElementCountScalar:
                if (immNumber == 1)
                {
                    immLowerBound = 1;
                    immUpperBound = 16;
                }
                else
                {
                    assert(immNumber == 2);
                    immLowerBound = (int)SVE_PATTERN_POW2;
                    immUpperBound = (int)SVE_PATTERN_ALL;
                }
                break;

            case NI_Sve_GatherPrefetch8Bit:
            case NI_Sve_GatherPrefetch16Bit:
            case NI_Sve_GatherPrefetch32Bit:
            case NI_Sve_GatherPrefetch64Bit:
            case NI_Sve_Prefetch16Bit:
            case NI_Sve_Prefetch32Bit:
            case NI_Sve_Prefetch64Bit:
            case NI_Sve_Prefetch8Bit:
                immLowerBound = (int)SVE_PRFOP_PLDL1KEEP;
                immUpperBound = (int)SVE_PRFOP_CONST15;
                break;

            case NI_Sve_AddRotateComplex:
            case NI_Sve2_AddRotateComplex:
            case NI_Sve2_AddSaturateRotateComplex:
                immLowerBound = 0;
                immUpperBound = 1;
                break;

            case NI_Sve_MultiplyAddRotateComplex:
            case NI_Sve2_MultiplyAddRotateComplex:
            case NI_Sve2_MultiplyAddRoundedDoublingSaturateHighRotateComplex:
            case NI_Sve2_DotProductRotateComplex:
                immLowerBound = 0;
                immUpperBound = 3;
                break;

            case NI_Sve_MultiplyAddRotateComplexBySelectedScalar:
                // rotation comes after index in the intrinsic's signature,
                // but flip the order here so we check the larger range first.
                // This conforms to the existing logic in LinearScan::BuildHWIntrinsic
                // when determining if we need an internal register for the jump table.
                // This flipped ordering is reflected in HWIntrinsicInfo::GetImmOpsPositions.
                if (immNumber == 1)
                {
                    // Bounds for rotation
                    immLowerBound = 0;
                    immUpperBound = 3;
                }
                else
                {
                    // Bounds for index
                    assert(immNumber == 2);
                    immLowerBound = 0;
                    immUpperBound = 1;
                }
                break;

            case NI_Sve2_DotProductRotateComplexBySelectedIndex:
                if (immNumber == 1)
                {
                    // Bounds for rotation
                    immLowerBound = 0;
                    immUpperBound = 3;
                }
                else
                {
                    // Bounds for index
                    assert(immNumber == 2);
                    assert(baseType == TYP_BYTE || baseType == TYP_SHORT);
                    immLowerBound = 0;
                    immUpperBound = (baseType == TYP_BYTE) ? 3 : 1;
                }
                break;

            case NI_Sve2_MultiplyAddRotateComplexBySelectedScalar:
                if (immNumber == 1)
                {
                    // Bounds for rotation
                    immLowerBound = 0;
                    immUpperBound = 3;
                }
                else
                {
                    // Bounds for index
                    assert(immNumber == 2);
                    assert(baseType == TYP_USHORT || baseType == TYP_SHORT || baseType == TYP_INT ||
                           baseType == TYP_UINT);
                    immLowerBound = 0;
                    immUpperBound = (baseType == TYP_USHORT || baseType == TYP_SHORT) ? 3 : 1;
                }
                break;

            case NI_Sve2_MultiplyAddRoundedDoublingSaturateHighRotateComplexBySelectedScalar:
                if (immNumber == 1)
                {
                    // Bounds for rotation
                    immLowerBound = 0;
                    immUpperBound = 3;
                }
                else
                {
                    // Bounds for index
                    assert(immNumber == 2);
                    assert(baseType == TYP_INT || baseType == TYP_SHORT);
                    immLowerBound = 0;
                    immUpperBound = (baseType == TYP_SHORT) ? 3 : 1;
                }
                break;

            case NI_Sve_TrigonometricMultiplyAddCoefficient:
                immLowerBound = 0;
                immUpperBound = 7;
                break;

            case NI_Sha3_XorRotateRight:
                immLowerBound = 0;
                immUpperBound = 63;
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
//    simdBaseType    -- base type of the Vector64/128<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types simdBaseType)
{
    bool isRightShift = true;

    switch (intrinsic)
    {
        case NI_AdvSimd_ShiftLeftLogical:
        case NI_AdvSimd_ShiftLeftLogicalScalar:
            isRightShift = false;
            FALLTHROUGH;

        case NI_AdvSimd_ShiftRightLogical:
        case NI_AdvSimd_ShiftRightLogicalScalar:
        case NI_AdvSimd_ShiftRightArithmetic:
        case NI_AdvSimd_ShiftRightArithmeticScalar:
        {
            // AdvSimd.ShiftLeft* and AdvSimd.ShiftRight* can be replaced with AdvSimd.Shift*, which takes op2 in a simd
            // register

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack();

            // AdvSimd.ShiftLogical does right-shifts with negative immediates, hence the negation
            if (isRightShift)
            {
                op2 = gtNewOperNode(GT_NEG, genActualType(op2->TypeGet()), op2);
            }

            NamedIntrinsic fallbackIntrinsic;
            switch (intrinsic)
            {
                case NI_AdvSimd_ShiftLeftLogical:
                case NI_AdvSimd_ShiftRightLogical:
                    fallbackIntrinsic = NI_AdvSimd_ShiftLogical;
                    break;

                case NI_AdvSimd_ShiftLeftLogicalScalar:
                case NI_AdvSimd_ShiftRightLogicalScalar:
                    fallbackIntrinsic = NI_AdvSimd_ShiftLogicalScalar;
                    break;

                case NI_AdvSimd_ShiftRightArithmetic:
                    fallbackIntrinsic = NI_AdvSimd_ShiftArithmetic;
                    break;

                case NI_AdvSimd_ShiftRightArithmeticScalar:
                    fallbackIntrinsic = NI_AdvSimd_ShiftArithmeticScalar;
                    break;

                default:
                    unreached();
            }

            GenTree* tmpOp = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseType, genTypeSize(simdType));
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, fallbackIntrinsic, simdBaseType,
                                            genTypeSize(simdType));
        }

        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// impSpecialIntrinsic: Import a hardware intrinsic that requires special handling as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    method          -- method handle of the intrinsic function.
//    sig             -- signature of the intrinsic call.
//    entryPoint      -- The entry point information required for R2R scenarios
//    simdBaseType    -- generic argument of the intrinsic.
//    retType         -- return type of the intrinsic.
//    mustExpand      -- true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
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

    bool isScalar = (category == HW_Category_Scalar);
    assert(numArgs >= 0);

    assert(varTypeIsArithmetic(simdBaseType));

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    GenTree* op3     = nullptr;
    GenTree* op4     = nullptr;

#ifdef DEBUG
    bool isValidScalarIntrinsic = false;
#endif

    switch (intrinsic)
    {
        case NI_AdvSimd_BitwiseClear:
        case NI_Sve_BitwiseClear:
        {
            assert(sig->numArgs == 2);

            // We don't want to support creating AND_NOT nodes prior to LIR
            // as it can break important optimizations. We'll produces this
            // in lowering instead so decompose into the individual operations
            // on import

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op2     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op2, simdBaseType, simdSize));
            retNode = gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_AdvSimd_OrNot:
        {
            assert(sig->numArgs == 2);

            // We don't want to support creating OR_NOT nodes prior to LIR
            // as it can break important optimizations. We'll produces this
            // in lowering instead so decompose into the individual operations
            // on import

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            op2     = gtFoldExpr(gtNewSimdUnOpNode(GT_NOT, retType, op2, simdBaseType, simdSize));
            retNode = gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_AdvSimd_LoadVector64:
        case NI_AdvSimd_LoadVector128:
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

        case NI_AdvSimd_Store:
        case NI_AdvSimd_Arm64_Store:
        {
            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = impPopStack().val;

            if (op2->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

                if (!op2->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("StoreVectorN"));

                    impStoreToTemp(tmp, op2, CHECK_SPILL_NONE);
                    op2 = gtNewLclvNode(tmp, argType);
                }
                op2     = gtConvertTableOpToFieldList(op2, fieldCount);
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                if (op1->OperIs(GT_CAST))
                {
                    // Although the API specifies a pointer, if what we have is a BYREF, that's what
                    // we really want, so throw away the cast.
                    if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        op1 = op1->gtGetOp1();
                    }
                }

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            }
            else
            {
                if (op2->TypeIs(TYP_SIMD16))
                {
                    // Update the simdSize explicitly as Vector128 variant of Store() is present in AdvSimd instead of
                    // AdvSimd.Arm64.
                    simdSize = 16;
                }

                op1 = impPopStack().val;

                if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    // If what we have is a BYREF, that's what we really want, so throw away the cast.
                    op1 = op1->gtGetOp1();
                }

                retNode = gtNewSimdStoreNode(op1, op2, simdBaseType, simdSize);
            }
            break;
        }

        case NI_AdvSimd_StoreVectorAndZip:
        case NI_AdvSimd_Arm64_StoreVectorAndZip:
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

            assert(op2->TypeIs(TYP_STRUCT));
            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    op1 = op1->gtGetOp1();
                }
            }

            if (!op2->OperIs(GT_LCL_VAR))
            {
                unsigned tmp = lvaGrabTemp(true DEBUGARG("StoreVectorNx2 temp tree"));

                impStoreToTemp(tmp, op2, CHECK_SPILL_NONE);
                op2 = gtNewLclvNode(tmp, argType);
            }
            op2 = gtConvertTableOpToFieldList(op2, fieldCount);

            intrinsic = simdSize == 8 ? NI_AdvSimd_StoreVectorAndZip : NI_AdvSimd_Arm64_StoreVectorAndZip;

            info.compNeedsConsecutiveRegisters = true;
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_AdvSimd_StoreSelectedScalar:
        case NI_AdvSimd_Arm64_StoreSelectedScalar:
        {
            assert(sig->numArgs == 3);
            assert(retType == TYP_VOID);

            if (!mustExpand && !impStackTop(0).val->IsCnsIntOrI() && impStackTop(1).val->TypeIs(TYP_STRUCT))
            {
                // TODO-ARM64-CQ: Support rewriting nodes that involves
                // GenTreeFieldList as user calls during rationalization
                return nullptr;
            }

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

            if (op2->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                intrinsic = simdSize == 8 ? NI_AdvSimd_StoreSelectedScalar : NI_AdvSimd_Arm64_StoreSelectedScalar;

                if (!op2->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("StoreSelectedScalarN"));

                    impStoreToTemp(tmp, op2, CHECK_SPILL_NONE);
                    op2 = gtNewLclvNode(tmp, argType);
                }
                op2 = gtConvertTableOpToFieldList(op2, fieldCount);
            }
            else
            {
                // While storing from a single vector, both Vector128 and Vector64 API calls are in AdvSimd class.
                // Thus, we get simdSize as 8 for both of the calls. We re-calculate that simd size for such API calls.
                getBaseTypeAndSizeOfSIMDType(argClass, &simdSize);
            }

            assert(HWIntrinsicInfo::isImmOp(intrinsic, op3));
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 1, &immLowerBound, &immUpperBound);
            op3     = addRangeCheckIfNeeded(intrinsic, op3, immLowerBound, immUpperBound);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            if (op1->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    op1 = op1->gtGetOp1();
                }
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_AdvSimd_Load2xVector64AndUnzip:
        case NI_AdvSimd_Load3xVector64AndUnzip:
        case NI_AdvSimd_Load4xVector64AndUnzip:
        case NI_AdvSimd_Arm64_Load2xVector128AndUnzip:
        case NI_AdvSimd_Arm64_Load3xVector128AndUnzip:
        case NI_AdvSimd_Arm64_Load4xVector128AndUnzip:
        case NI_AdvSimd_Load2xVector64:
        case NI_AdvSimd_Load3xVector64:
        case NI_AdvSimd_Load4xVector64:
        case NI_AdvSimd_Arm64_Load2xVector128:
        case NI_AdvSimd_Arm64_Load3xVector128:
        case NI_AdvSimd_Arm64_Load4xVector128:
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
                if (op1->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    op1 = op1->gtGetOp1();
                }
            }

            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));

            op1     = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseType, simdSize);
            retNode = impStoreMultiRegValueToVar(op1, sig->retTypeSigClass DEBUGARG(CorInfoCallConvExtension::Managed));
            break;
        }

        case NI_Sve_CreateFalseMaskByte:
        case NI_Sve_CreateFalseMaskDouble:
        case NI_Sve_CreateFalseMaskInt16:
        case NI_Sve_CreateFalseMaskInt32:
        case NI_Sve_CreateFalseMaskInt64:
        case NI_Sve_CreateFalseMaskSByte:
        case NI_Sve_CreateFalseMaskSingle:
        case NI_Sve_CreateFalseMaskUInt16:
        case NI_Sve_CreateFalseMaskUInt32:
        case NI_Sve_CreateFalseMaskUInt64:
        {
            // Import as a constant vector 0
            GenTreeVecCon* vecCon = gtNewVconNode(retType);
            vecCon->gtSimdVal     = simd_t::Zero();
            retNode               = vecCon;
            break;
        }

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
        {
            assert(sig->numArgs == 1);
            op1 = impPopStack().val;

            // Where possible, import a constant mask to allow for optimisations.
            if (op1->IsIntegralConst())
            {
                int64_t pattern = op1->AsIntConCommon()->IntegralValue();
                simd_t  simdVal;

                if (EvaluateSimdPatternToVector(simdBaseType, &simdVal, (SveMaskPattern)pattern))
                {
                    retNode = gtNewVconNode(retType, &simdVal);
                    break;
                }
            }

            // Was not able to generate a pattern, instead import a truemaskall
            retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Sve_Load2xVectorAndUnzip:
        case NI_Sve_Load3xVectorAndUnzip:
        case NI_Sve_Load4xVectorAndUnzip:
        {
            info.compNeedsConsecutiveRegisters = true;

            assert(sig->numArgs == 2);

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            if (op2->OperIs(GT_CAST))
            {
                // Although the API specifies a pointer, if what we have is a BYREF, that's what
                // we really want, so throw away the cast.
                if (op2->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    op2 = op2->gtGetOp1();
                }
            }

            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));
            assert(HWIntrinsicInfo::IsExplicitMaskedOperation(intrinsic));

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
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

            if (!mustExpand && !impStackTop(1).val->IsCnsIntOrI())
            {
                // TODO-ARM64-CQ: Support rewriting nodes that involves
                // GenTreeFieldList as user calls during rationalization
                return nullptr;
            }

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
                if (op3->gtGetOp1()->TypeIs(TYP_BYREF))
                {
                    op3 = op3->gtGetOp1();
                }
            }

            assert(HWIntrinsicInfo::IsMultiReg(intrinsic));
            assert(op1->TypeIs(TYP_STRUCT));

            info.compNeedsConsecutiveRegisters = true;
            unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

            if (!op1->OperIs(GT_LCL_VAR))
            {
                unsigned tmp = lvaGrabTemp(true DEBUGARG("LoadAndInsertScalar temp tree"));

                impStoreToTemp(tmp, op1, CHECK_SPILL_NONE);
                op1 = gtNewLclvNode(tmp, argType);
            }

            op1     = gtConvertParamOpToFieldList(op1, fieldCount, argClass);
            op1     = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
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

            if (op1->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

                if (!op1->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("VectorTableLookup temp tree"));

                    impStoreToTemp(tmp, op1, CHECK_SPILL_NONE);
                    op1 = gtNewLclvNode(tmp, argType);
                }

                op1 = gtConvertTableOpToFieldList(op1, fieldCount);
            }
            else
            {
                assert(varTypeIsSIMD(op1->TypeGet()));
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
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

            if (op2->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);

                if (!op2->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("VectorTableLookupExtension temp tree"));

                    impStoreToTemp(tmp, op2, CHECK_SPILL_NONE);
                    op2 = gtNewLclvNode(tmp, argType);
                }

                op2 = gtConvertTableOpToFieldList(op2, fieldCount);
            }
            else
            {
                assert(varTypeIsSIMD(op1->TypeGet()));
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Sve_StoreAndZip:
        {
            assert(sig->numArgs == 3);
            assert(retType == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3     = info.compCompHnd->getArgNext(arg2);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType             = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3                 = impPopStack().val;
            unsigned fieldCount = info.compCompHnd->getClassNumInstanceFields(argClass);

            if (op3->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                switch (fieldCount)
                {
                    case 2:
                        intrinsic = NI_Sve_StoreAndZipx2;
                        break;

                    case 3:
                        intrinsic = NI_Sve_StoreAndZipx3;
                        break;

                    case 4:
                        intrinsic = NI_Sve_StoreAndZipx4;
                        break;

                    default:
                        assert(!"unsupported");
                }

                if (!op3->OperIs(GT_LCL_VAR))
                {
                    unsigned tmp = lvaGrabTemp(true DEBUGARG("SveStoreN"));

                    impStoreToTemp(tmp, op3, CHECK_SPILL_NONE);
                    op3 = gtNewLclvNode(tmp, argType);
                }
                op3 = gtConvertTableOpToFieldList(op3, fieldCount);
            }

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            break;
        }

        case NI_Sve_StoreNarrowing:
        {
            assert(sig->numArgs == 3);
            assert(retType == TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg   = sig->args;
            arg                           = info.compCompHnd->getArgNext(arg);
            CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
            CorInfoType          ptrType  = CORINFO_TYPE_UNDEF;
            CORINFO_CLASS_HANDLE tmpClass = NO_CLASS_HANDLE;

            // The size of narrowed target elements is determined from the second argument of StoreNarrowing().
            // Thus, we first extract the datatype of a pointer passed in the second argument and then store it as the
            // auxiliary type of intrinsic. This auxiliary type is then used in the codegen to choose the correct
            // instruction to emit.
            ptrType = strip(info.compCompHnd->getArgType(sig, arg, &tmpClass));
            assert(ptrType == CORINFO_TYPE_PTR);
            ptrType = info.compCompHnd->getChildType(argClass, &tmpClass);
            assert(JitType2PreciseVarType(ptrType) < simdBaseType);

            op3     = impPopStack().val;
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(JitType2PreciseVarType(ptrType));
            break;
        }

        case NI_Sve_SaturatingDecrementBy8BitElementCount:
        case NI_Sve_SaturatingIncrementBy8BitElementCount:
        case NI_Sve_SaturatingDecrementBy16BitElementCountScalar:
        case NI_Sve_SaturatingDecrementBy32BitElementCountScalar:
        case NI_Sve_SaturatingDecrementBy64BitElementCountScalar:
        case NI_Sve_SaturatingIncrementBy16BitElementCountScalar:
        case NI_Sve_SaturatingIncrementBy32BitElementCountScalar:
        case NI_Sve_SaturatingIncrementBy64BitElementCountScalar:
#ifdef DEBUG
            isValidScalarIntrinsic = true;
            FALLTHROUGH;
#endif
        case NI_Sve_SaturatingDecrementBy16BitElementCount:
        case NI_Sve_SaturatingDecrementBy32BitElementCount:
        case NI_Sve_SaturatingDecrementBy64BitElementCount:
        case NI_Sve_SaturatingIncrementBy16BitElementCount:
        case NI_Sve_SaturatingIncrementBy32BitElementCount:
        case NI_Sve_SaturatingIncrementBy64BitElementCount:

        {
            assert(sig->numArgs == 3);

            CORINFO_ARG_LIST_HANDLE arg1          = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2          = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3          = info.compCompHnd->getArgNext(arg2);
            var_types               argType       = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass      = NO_CLASS_HANDLE;
            int                     immLowerBound = 0;
            int                     immUpperBound = 0;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = impPopStack().val;

            assert(HWIntrinsicInfo::isImmOp(intrinsic, op2));
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 1, &immLowerBound, &immUpperBound);
            op2 = addRangeCheckIfNeeded(intrinsic, op2, immLowerBound, immUpperBound);

            assert(HWIntrinsicInfo::isImmOp(intrinsic, op3));
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 2, &immLowerBound, &immUpperBound);
            op3 = addRangeCheckIfNeeded(intrinsic, op3, immLowerBound, immUpperBound);

            retNode = isScalar ? gtNewScalarHWIntrinsicNode(retType, op1, op2, op3, intrinsic)
                               : gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);

            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            break;
        }

        case NI_Sve_SaturatingDecrementByActiveElementCount:
        case NI_Sve_SaturatingIncrementByActiveElementCount:
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

            var_types op1BaseType = getBaseTypeOfSIMDType(argClass);

            // HWInstrinsic requires a mask for op2
            if (!varTypeIsMask(op2))
            {
                op2 = gtNewSimdCvtVectorToMaskNode(TYP_MASK, op2, simdBaseType, simdSize);
            }

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);

            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(op1BaseType);
            break;
        }
        case NI_Sve_GatherPrefetch8Bit:
        case NI_Sve_GatherPrefetch16Bit:
        case NI_Sve_GatherPrefetch32Bit:
        case NI_Sve_GatherPrefetch64Bit:
        case NI_Sve_Prefetch16Bit:
        case NI_Sve_Prefetch32Bit:
        case NI_Sve_Prefetch64Bit:
        case NI_Sve_Prefetch8Bit:
        {
            assert((sig->numArgs == 3) || (sig->numArgs == 4));
            assert(!isScalar);

            var_types            argType       = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE argClass      = NO_CLASS_HANDLE;
            int                  immLowerBound = 0;
            int                  immUpperBound = 0;

            CORINFO_ARG_LIST_HANDLE arg1 = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 1, &immLowerBound, &immUpperBound);

            if (sig->numArgs == 3)
            {
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
                op3     = getArgForHWIntrinsic(argType, argClass);

                assert(HWIntrinsicInfo::isImmOp(intrinsic, op3));
                op3 = addRangeCheckIfNeeded(intrinsic, op3, immLowerBound, immUpperBound);

                argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2                   = getArgForHWIntrinsic(argType, argClass);
                var_types op2BaseType = getBaseTypeOfSIMDType(argClass);
                argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
                op1                   = impPopStack().val;

#ifdef DEBUG

                if ((intrinsic == NI_Sve_GatherPrefetch8Bit) || (intrinsic == NI_Sve_GatherPrefetch16Bit) ||
                    (intrinsic == NI_Sve_GatherPrefetch32Bit) || (intrinsic == NI_Sve_GatherPrefetch64Bit))
                {
                    assert(varTypeIsSIMD(op2->TypeGet()));
                }
                else
                {
                    assert(varTypeIsIntegral(op2->TypeGet()));
                }
#endif
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, simdBaseType, simdSize);
                retNode->AsHWIntrinsic()->SetAuxiliaryType(op2BaseType);
            }
            else
            {
                CORINFO_ARG_LIST_HANDLE arg4 = info.compCompHnd->getArgNext(arg3);
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
                op4     = getArgForHWIntrinsic(argType, argClass);

                assert(HWIntrinsicInfo::isImmOp(intrinsic, op4));
                op4 = addRangeCheckIfNeeded(intrinsic, op4, immLowerBound, immUpperBound);

                argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
                op3                   = getArgForHWIntrinsic(argType, argClass);
                var_types op3BaseType = getBaseTypeOfSIMDType(argClass);
                argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2                   = getArgForHWIntrinsic(argType, argClass);
                argType               = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
                op1                   = impPopStack().val;

                assert(varTypeIsSIMD(op3->TypeGet()));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, op4, intrinsic, simdBaseType, simdSize);
                retNode->AsHWIntrinsic()->SetAuxiliaryType(op3BaseType);
            }

            break;
        }
        case NI_Sve_ConditionalExtractAfterLastActiveElementScalar:
        case NI_Sve_ConditionalExtractLastActiveElementScalar:
        {
            assert(sig->numArgs == 3);

#ifdef DEBUG
            isValidScalarIntrinsic = true;
#endif

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
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewScalarHWIntrinsicNode(retType, op1, op2, op3, intrinsic);

            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            break;
        }

        case NI_Sve_ExtractAfterLastActiveElementScalar:
        case NI_Sve_ExtractLastActiveElementScalar:
        {
            assert(sig->numArgs == 2);

#ifdef DEBUG
            isValidScalarIntrinsic = true;
#endif

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = gtNewScalarHWIntrinsicNode(retType, op1, op2, intrinsic);

            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            break;
        }

        case NI_Sve_MultiplyAddRotateComplexBySelectedScalar:
        case NI_Sve2_MultiplyAddRotateComplexBySelectedScalar:
        case NI_Sve2_MultiplyAddRoundedDoublingSaturateHighRotateComplexBySelectedScalar:
        case NI_Sve2_DotProductRotateComplexBySelectedIndex:
        {
            assert(sig->numArgs == 5);
            assert(!isScalar);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_ARG_LIST_HANDLE arg3     = info.compCompHnd->getArgNext(arg2);
            CORINFO_ARG_LIST_HANDLE arg4     = info.compCompHnd->getArgNext(arg3);
            CORINFO_ARG_LIST_HANDLE arg5     = info.compCompHnd->getArgNext(arg4);
            var_types               argType  = TYP_UNKNOWN;
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            int imm1LowerBound, imm1UpperBound; // Range for rotation
            int imm2LowerBound, imm2UpperBound; // Range for index
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 1, &imm1LowerBound, &imm1UpperBound);
            HWIntrinsicInfo::lookupImmBounds(intrinsic, simdSize, simdBaseType, 2, &imm2LowerBound, &imm2UpperBound);

            argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg5, &argClass)));
            GenTree* op5 = getArgForHWIntrinsic(argType, argClass);
            assert(HWIntrinsicInfo::isImmOp(intrinsic, op5));
            op5 = addRangeCheckIfNeeded(intrinsic, op5, imm1LowerBound, imm1UpperBound);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
            op4     = getArgForHWIntrinsic(argType, argClass);
            assert(HWIntrinsicInfo::isImmOp(intrinsic, op4));
            op4 = addRangeCheckIfNeeded(intrinsic, op4, imm2LowerBound, imm2UpperBound);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            retNode = new (this, GT_HWINTRINSIC) GenTreeHWIntrinsic(retType, getAllocator(CMK_ASTNode), intrinsic,
                                                                    simdBaseType, simdSize, op1, op2, op3, op4, op5);
            break;
        }

        case NI_Sve2_VectorTableLookup:
        {
            assert(sig->numArgs == 2);
            assert(retType != TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;
            var_types argType1 = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            var_types argType2 = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));

            var_types op1BaseType = getBaseTypeOfSIMDType(argClass);

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            if (op1->TypeIs(TYP_STRUCT))
            {
                info.compNeedsConsecutiveRegisters = true;
                unsigned fieldCount                = info.compCompHnd->getClassNumInstanceFields(argClass);
                op1                                = gtConvertTableOpToFieldList(op1, fieldCount);
            }
            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(op1BaseType);
            break;
        }

        case NI_Sve2_AddWideningEven:
        case NI_Sve2_AddWideningOdd:
        case NI_Sve2_SubtractWideningEven:
        case NI_Sve2_SubtractWideningOdd:
        {
            assert(sig->numArgs == 2);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            op2 = impPopStack().val;
            op1 = impPopStack().val;

            var_types op1BaseType = getBaseTypeOfSIMDType(argClass);
            retNode               = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(op1BaseType);
            break;
        }

        case NI_Sve2_AddSaturate:
        {
            assert(sig->numArgs == 2);
            assert(retType != TYP_VOID);

            CORINFO_ARG_LIST_HANDLE arg1     = sig->args;
            CORINFO_ARG_LIST_HANDLE arg2     = info.compCompHnd->getArgNext(arg1);
            CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

            var_types argType1    = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg1, &argClass)));
            var_types op1BaseType = getBaseTypeOfSIMDType(argClass);
            var_types argType2    = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            var_types op2BaseType = getBaseTypeOfSIMDType(argClass);
            assert(op1BaseType == simdBaseType);

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, simdBaseType, simdSize);
            retNode->AsHWIntrinsic()->SetSimdBaseType(simdBaseType);
            retNode->AsHWIntrinsic()->SetAuxiliaryType(op2BaseType);
            break;
        }

        default:
        {
            return nullptr;
        }
    }

    assert(!isScalar || isValidScalarIntrinsic);
    return retNode;
}

//------------------------------------------------------------------------
// gtNewSimdAllTrueMaskNode: Create a mask with all bits set to true
//
// Arguments:
//    simdBaseType -- the base type of the nodes being masked
//
// Return Value:
//    The mask
//
GenTree* Compiler::gtNewSimdAllTrueMaskNode(var_types simdBaseType)
{
    // Import as a constant mask

    GenTreeMskCon* mskCon = gtNewMskConNode(TYP_MASK);

    // TODO-SVE: For agnostic VL, vector type may not be simd16_t

    bool found = EvaluateSimdPatternToMask<simd16_t>(simdBaseType, &mskCon->gtSimdMaskVal, SveMaskPatternAll);
    assert(found);

    return mskCon;
}

//------------------------------------------------------------------------
// gtNewSimdFalseMaskByteNode: Create a mask with all bits set to false
//
// Return Value:
//    The mask
//
GenTree* Compiler::gtNewSimdFalseMaskByteNode()
{
    // Import as a constant mask 0
    GenTreeMskCon* mskCon = gtNewMskConNode(TYP_MASK);
    mskCon->gtSimdMaskVal = simdmask_t::Zero();
    return mskCon;
}

#endif // FEATURE_HW_INTRINSICS
