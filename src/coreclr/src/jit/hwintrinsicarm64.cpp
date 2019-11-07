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
static InstructionSet Arm64VersionOfIsa(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_AdvSimd:
            return InstructionSet_AdvSimd_Arm64;
        case InstructionSet_ArmBase:
            return InstructionSet_ArmBase_Arm64;
        default:
            unreached();
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
static InstructionSet lookupInstructionSet(const char* className)
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
InstructionSet HWIntrinsicInfo::lookupIsa(const char* className, const char* enclosingClassName)
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
    assert(HWIntrinsicInfo::HasFullRangeImm(id));
    return 255;
}

//------------------------------------------------------------------------
// isInImmRange: Check if ival is valid for the intrinsic
//
// Arguments:
//    id   -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//    ival -- the imm value to be checked
//
// Return Value:
//     true if ival is valid for the intrinsic
//
bool HWIntrinsicInfo::isInImmRange(NamedIntrinsic id, int ival)
{
    assert(HWIntrinsicInfo::lookupCategory(id) == HW_Category_IMM);
    return ival <= lookupImmUpperBound(id) && ival >= 0;
}

//------------------------------------------------------------------------
// isFullyImplementedIsa: Gets a value that indicates whether the InstructionSet is fully implemented
//
// Arguments:
//    isa - The InstructionSet to check
//
// Return Value:
//    true if isa is supported; otherwise, false
bool HWIntrinsicInfo::isFullyImplementedIsa(InstructionSet isa)
{
    switch (isa)
    {
        // These ISAs are fully implemented
        case InstructionSet_AdvSimd:
        case InstructionSet_AdvSimd_Arm64:
        case InstructionSet_Aes:
        case InstructionSet_ArmBase:
        case InstructionSet_ArmBase_Arm64:
        case InstructionSet_Sha1:
        case InstructionSet_Sha256:
        case InstructionSet_Vector64:
        case InstructionSet_Vector128:
        {
            return true;
        }

        default:
        {
            unreached();
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
bool HWIntrinsicInfo::isScalarIsa(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_ArmBase:
        case InstructionSet_ArmBase_Arm64:
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
    unreached();
}

//------------------------------------------------------------------------
// impSpecialIntrinsic: Import a hardware intrinsic that requires special handling as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic  -- id of the intrinsic function.
//    method     -- method handle of the intrinsic function.
//    sig        -- signature of the intrinsic call
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                       CORINFO_CLASS_HANDLE  clsHnd,
                                       CORINFO_METHOD_HANDLE method,
                                       CORINFO_SIG_INFO*     sig,
                                       bool                  mustExpand)
{
    HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);
    int                 numArgs  = sig->numArgs;
    var_types           retType  = JITtype2varType(sig->retType);
    var_types           baseType = TYP_UNKNOWN;

    if ((retType == TYP_STRUCT) && featureSIMD)
    {
        unsigned int sizeBytes;
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
        retType  = getSIMDTypeForSize(sizeBytes);
        assert(sizeBytes != 0);

        if (!varTypeIsArithmetic(baseType))
        {
            assert((intrinsic == NI_Vector64_AsByte) || (intrinsic == NI_Vector128_As));
            return nullptr;
        }
    }

    if (HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic))
    {
        CORINFO_ARG_LIST_HANDLE arg = sig->args;

        CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
        baseType                      = getBaseTypeAndSizeOfSIMDType(argClass);

        if (baseType == TYP_UNKNOWN) // the argument is not a vector
        {
            CORINFO_CLASS_HANDLE tmpClass;
            CorInfoType          corInfoType = strip(info.compCompHnd->getArgType(sig, arg, &tmpClass));

            if (corInfoType == CORINFO_TYPE_PTR)
            {
                corInfoType = info.compCompHnd->getChildType(argClass, &tmpClass);
            }

            baseType = JITtype2varType(corInfoType);
        }
    }
    else if (baseType == TYP_UNKNOWN)
    {
        if (category != HW_Category_Scalar)
        {
            unsigned int sizeBytes;
            baseType = getBaseTypeAndSizeOfSIMDType(clsHnd, &sizeBytes);
            assert(sizeBytes != 0);
        }
        else
        {
            baseType = retType;
        }
    }

    if (!varTypeIsArithmetic(baseType))
    {
        return nullptr;
    }

    unsigned                simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
    CORINFO_ARG_LIST_HANDLE argList  = sig->args;
    CORINFO_CLASS_HANDLE    argClass;
    var_types               argType = TYP_UNKNOWN;

    assert(numArgs >= 0);

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;

    switch (intrinsic)
    {
        case NI_Vector64_AsByte:
        case NI_Vector64_AsInt16:
        case NI_Vector64_AsInt32:
        case NI_Vector64_AsSByte:
        case NI_Vector64_AsSingle:
        case NI_Vector64_AsUInt16:
        case NI_Vector64_AsUInt32:
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

        case NI_ArmBase_LeadingZeroCount:
        case NI_ArmBase_Arm64_LeadingSignCount:
        case NI_ArmBase_Arm64_LeadingZeroCount:
        case NI_Sha1_FixedRotate:
        {
            assert(numArgs == 1);
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);
            return gtNewScalarHWIntrinsicNode(baseType, op1, intrinsic);
        }

        default:
        {
            unreached();
            break;
        }
    }

    return retNode;
}

#endif // FEATURE_HW_INTRINSICS
