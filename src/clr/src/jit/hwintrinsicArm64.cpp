// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

namespace IsaFlag
{
enum Flag
{
#define HARDWARE_INTRINSIC_CLASS(flag, jit_config, isa) isa = 1ULL << InstructionSet_##isa,
#include "hwintrinsiclistArm64.h"
    None      = 0,
    Base      = 1ULL << InstructionSet_Base,
    Vector64  = 1ULL << InstructionSet_Vector64,
    Vector128 = 1ULL << InstructionSet_Vector128,
    EveryISA  = ~0ULL
};

Flag operator|(Flag a, Flag b)
{
    return Flag(uint64_t(a) | uint64_t(b));
}

Flag flag(InstructionSet isa)
{
    return Flag(1ULL << isa);
}
}

// clang-format off
static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
#define HARDWARE_INTRINSIC(id, isa, name, form, i0, i1, i2, flags) \
    {id,                            #name,                             IsaFlag::isa,      HWIntrinsicInfo::form,        HWIntrinsicInfo::flags, { i0, i1, i2 }},
#include "hwintrinsiclistArm64.h"
};
// clang-format on

//------------------------------------------------------------------------
// lookup: Gets the HWIntrinsicInfo associated with a given NamedIntrinsic
//
// Arguments:
//    id -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//
// Return Value:
//    The HWIntrinsicInfo associated with id
const HWIntrinsicInfo& HWIntrinsicInfo::lookup(NamedIntrinsic id)
{
    assert(id != NI_Illegal);

    assert(id > NI_HW_INTRINSIC_START);
    assert(id < NI_HW_INTRINSIC_END);

    return hwIntrinsicInfoArray[id - NI_HW_INTRINSIC_START - 1];
}

//------------------------------------------------------------------------
// lookupHWIntrinsicISA: map class name to InstructionSet value
//
// Arguments:
//    className -- class name in System.Runtime.Intrinsics.Arm.Arm64
//
// Return Value:
//    Id for the ISA class if enabled.
//
InstructionSet Compiler::lookupHWIntrinsicISA(const char* className)
{
    if (className != nullptr)
    {
        if (strcmp(className, "Base") == 0)
            return InstructionSet_Base;
        if (strncmp(className, "Vector64", 8) == 0)
            return InstructionSet_Vector64;
        if (strncmp(className, "Vector128", 9) == 0)
            return InstructionSet_Vector128;
#define HARDWARE_INTRINSIC_CLASS(flag, jit_config, isa)                                                                \
    if (strcmp(className, #isa) == 0)                                                                                  \
        return InstructionSet_##isa;
#include "hwintrinsiclistArm64.h"
    }

    return InstructionSet_NONE;
}

//------------------------------------------------------------------------
// lookupHWIntrinsic: map intrinsic name to named intrinsic value
//
// Arguments:
//    methodName -- name of the intrinsic function.
//    isa        -- instruction set of the intrinsic.
//
// Return Value:
//    Id for the hardware intrinsic.
NamedIntrinsic Compiler::lookupHWIntrinsic(const char* className, const char* methodName)
{
    // TODO-Throughput: replace sequential search by binary search
    InstructionSet isa = lookupHWIntrinsicISA(className);

    if (isa == InstructionSet_NONE)
    {
        // There are several platform-agnostic intrinsics (e.g., Vector256) that
        // are not supported in Arm64, so early return NI_Illegal
        return NI_Illegal;
    }

    bool isIsaSupported = compSupports(isa) && compSupportsHWIntrinsic(isa);

    if (strcmp(methodName, "get_IsSupported") == 0)
    {
        return isIsaSupported ? NI_IsSupported_True : NI_IsSupported_False;
    }
    else if (!isIsaSupported)
    {
        return NI_Throw_PlatformNotSupportedException;
    }

    for (int i = 0; i < (NI_HW_INTRINSIC_END - NI_HW_INTRINSIC_START - 1); i++)
    {
        if ((IsaFlag::flag(isa) & hwIntrinsicInfoArray[i].isaflags) == 0)
        {
            continue;
        }

        if (strcmp(methodName, hwIntrinsicInfoArray[i].name) == 0)
        {
            return hwIntrinsicInfoArray[i].id;
        }
    }

    // There are several helper intrinsics that are implemented in managed code
    // Those intrinsics will hit this code path and need to return NI_Illegal
    return NI_Illegal;
}

//------------------------------------------------------------------------
// isFullyImplementedIsa: Gets a value that indicates whether the InstructionSet is fully implemented
//
// Arguments:
//    isa - The InstructionSet to check
//
// Return Value:
//    true if isa is supported; otherwise, false
//
// Notes:
//    This currently returns true for all partially-implemented ISAs.
//    TODO-Bug: Set this to return the correct values as https://github.com/dotnet/coreclr/issues/20427 is resolved.
//
bool HWIntrinsicInfo::isFullyImplementedIsa(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_Base:
        case InstructionSet_Crc32:
        case InstructionSet_Aes:
        case InstructionSet_Simd:
        case InstructionSet_Sha1:
        case InstructionSet_Sha256:
        case InstructionSet_Vector64:
        case InstructionSet_Vector128:
            return true;

        default:
            assert(!"Unexpected Arm64 HW intrinsics ISA");
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
bool HWIntrinsicInfo::isScalarIsa(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_Base:
        case InstructionSet_Crc32:
            return true;

        case InstructionSet_Aes:
        case InstructionSet_Simd:
        case InstructionSet_Sha1:
        case InstructionSet_Sha256:
        case InstructionSet_Vector64:
        case InstructionSet_Vector128:
            return false;

        default:
            assert(!"Unexpected Arm64 HW intrinsics ISA");
            return true;
    }
}

//------------------------------------------------------------------------
// addRangeCheckIfNeeded: add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic
//
// Arguments:
//    immOp      -- the operand of the intrinsic that points to the imm-arg
//    max        -- maximum allowable value for the immOp
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//     if necessary, add a GT_HW_INTRINSIC_CHK node which throws an ArgumentOutOfRangeException
//     when the immOp is not in the valid range; otherwise, just return the provided immOp.
//
GenTree* Compiler::addRangeCheckIfNeeded(GenTree* immOp, unsigned int max, bool mustExpand)
{
    assert(immOp != nullptr);

    // Need to range check only if we're must expand.
    if (mustExpand)
    {
        GenTree* upperBoundNode = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, max);
        GenTree* index          = nullptr;
        if ((immOp->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            index = fgInsertCommaFormTemp(&immOp);
        }
        else
        {
            index = gtCloneExpr(immOp);
        }
        GenTreeBoundsChk* hwIntrinsicChk = new (this, GT_HW_INTRINSIC_CHK)
            GenTreeBoundsChk(GT_HW_INTRINSIC_CHK, TYP_VOID, index, upperBoundNode, SCK_RNGCHK_FAIL);
        hwIntrinsicChk->gtThrowKind = SCK_ARG_RNG_EXCPN;
        return gtNewOperNode(GT_COMMA, immOp->TypeGet(), hwIntrinsicChk, immOp);
    }
    else
    {
        return immOp;
    }
}

//------------------------------------------------------------------------
// compSupportsHWIntrinsic: compiler support of hardware intrinsics
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true if
//    - isa is a scalar ISA
//    - isa is a SIMD ISA and featureSIMD=true
//    - isa is fully implemented or EnableIncompleteISAClass=true
bool Compiler::compSupportsHWIntrinsic(InstructionSet isa)
{
    return (featureSIMD || HWIntrinsicInfo::isScalarIsa(isa)) && (
#ifdef DEBUG
                                                                     JitConfig.EnableIncompleteISAClass() ||
#endif
                                                                     HWIntrinsicInfo::isFullyImplementedIsa(isa));
}

//------------------------------------------------------------------------
// lookupNumArgs: gets the number of arguments for the hardware intrinsic.
// This attempts to do a table based lookup but will fallback to the number
// of operands in 'node' if the table entry is -1.
//
// Arguments:
//    node      -- GenTreeHWIntrinsic* node with nullptr default value
//
// Return Value:
//     number of arguments
//
int HWIntrinsicInfo::lookupNumArgs(const GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsic = node->gtHWIntrinsicId;

    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);

    GenTree* op1     = node->gtGetOp1();
    GenTree* op2     = node->gtGetOp2();
    int      numArgs = 0;

    if (op1 == nullptr)
    {
        return 0;
    }

    if (op1->OperIsList())
    {
        numArgs              = 0;
        GenTreeArgList* list = op1->AsArgList();

        while (list != nullptr)
        {
            numArgs++;
            list = list->Rest();
        }

        // We should only use a list if we have 3 operands.
        assert(numArgs >= 3);
        return numArgs;
    }

    if (op2 == nullptr)
    {
        return 1;
    }

    return 2;
}

//------------------------------------------------------------------------
// impHWIntrinsic: dispatch hardware intrinsics to their own implementation
// function
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    method    -- method handle of the intrinsic function.
//    sig       -- signature of the intrinsic call
//
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impHWIntrinsic(NamedIntrinsic        intrinsic,
                                  CORINFO_CLASS_HANDLE  clsHnd,
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO*     sig,
                                  bool                  mustExpand)
{
    GenTree*             retNode       = nullptr;
    GenTree*             op1           = nullptr;
    GenTree*             op2           = nullptr;
    GenTree*             op3           = nullptr;
    CORINFO_CLASS_HANDLE simdClass     = nullptr;
    var_types            simdType      = TYP_UNKNOWN;
    var_types            simdBaseType  = TYP_UNKNOWN;
    unsigned             simdSizeBytes = 0;

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
            if (!featureSIMD)
            {
                return nullptr;
            }

            // We fold away the cast here, as it only exists to satisfy
            // the type system. It is safe to do this here since the retNode type
            // and the signature return type are both the same TYP_SIMD.

            var_types op1SimdBaseType = TYP_UNKNOWN;

            assert(!sig->hasThis());
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_STRUCT);

            simdBaseType    = getBaseTypeAndSizeOfSIMDType(sig->retTypeClass, &simdSizeBytes);
            op1SimdBaseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));

            if (!varTypeIsArithmetic(simdBaseType) || !varTypeIsArithmetic(op1SimdBaseType))
            {
                return nullptr;
            }

            retNode = impSIMDPopStack(getSIMDTypeForSize(simdSizeBytes), /* expectAddr: */ false, sig->retTypeClass);
            SetOpLclRelatedToSIMDIntrinsic(retNode);
            assert(retNode->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

            return retNode;
        }

        default:
            break;
    }

    switch (HWIntrinsicInfo::lookup(intrinsic).form)
    {
        case HWIntrinsicInfo::SimdBinaryOp:
        case HWIntrinsicInfo::SimdInsertOp:
        case HWIntrinsicInfo::SimdSelectOp:
        case HWIntrinsicInfo::SimdSetAllOp:
        case HWIntrinsicInfo::SimdUnaryOp:
        case HWIntrinsicInfo::SimdBinaryRMWOp:
        case HWIntrinsicInfo::SimdTernaryRMWOp:
        case HWIntrinsicInfo::Sha1HashOp:
            simdClass = sig->retTypeClass;
            break;
        case HWIntrinsicInfo::SimdExtractOp:
            info.compCompHnd->getArgType(sig, sig->args, &simdClass);
            break;
        default:
            break;
    }

    // Simd instantiation type check
    if (simdClass != nullptr)
    {
        if (featureSIMD)
        {
            compFloatingPointUsed = true;

            simdBaseType = getBaseTypeAndSizeOfSIMDType(simdClass, &simdSizeBytes);
        }

        if (simdBaseType == TYP_UNKNOWN)
        {
            return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
        }
        simdType = getSIMDTypeForSize(simdSizeBytes);
    }

    switch (HWIntrinsicInfo::lookup(intrinsic).form)
    {
        case HWIntrinsicInfo::UnaryOp:
            op1 = impPopStack().val;

            return gtNewScalarHWIntrinsicNode(JITtype2varType(sig->retType), op1, intrinsic);

        case HWIntrinsicInfo::SimdBinaryOp:
        case HWIntrinsicInfo::SimdBinaryRMWOp:
            // op1 is the first operand
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, op2, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::SimdTernaryRMWOp:
        case HWIntrinsicInfo::SimdSelectOp:
            // op1 is the first operand
            // op2 is the second operand
            // op3 is the third operand
            op3 = impSIMDPopStack(simdType);
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, op2, op3, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::SimdSetAllOp:
            op1 = impPopStack().val;

            return gtNewSimdHWIntrinsicNode(simdType, op1, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::SimdUnaryOp:
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::SimdExtractOp:
        {
            int vectorLength = getSIMDVectorLength(simdSizeBytes, simdBaseType);
            op2              = impStackTop().val;
            if (!mustExpand && (!op2->IsCnsIntOrI() || op2->AsIntConCommon()->IconValue() >= vectorLength))
            {
                // This is either an out-of-range constant or a non-constant.
                // We won't expand it; it will be handled recursively, at which point 'mustExpand'
                // will be true.
                return nullptr;
            }
            op2 = impPopStack().val;
            op2 = addRangeCheckIfNeeded(op2, vectorLength, mustExpand);
            op1 = impSIMDPopStack(simdType);

            return gtNewScalarHWIntrinsicNode(JITtype2varType(sig->retType), op1, op2, intrinsic);
        }
        case HWIntrinsicInfo::SimdInsertOp:
        {
            int vectorLength = getSIMDVectorLength(simdSizeBytes, simdBaseType);
            op2              = impStackTop(1).val;
            if (!mustExpand && (!op2->IsCnsIntOrI() || op2->AsIntConCommon()->IconValue() >= vectorLength))
            {
                // This is either an out-of-range constant or a non-constant.
                // We won't expand it; it will be handled recursively, at which point 'mustExpand'
                // will be true.
                return nullptr;
            }
            op3 = impPopStack().val;
            op2 = impPopStack().val;
            op2 = addRangeCheckIfNeeded(op2, vectorLength, mustExpand);
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, op2, op3, intrinsic, simdBaseType, simdSizeBytes);
        }
        case HWIntrinsicInfo::Sha1HashOp:
            op3 = impSIMDPopStack(simdType);
            op2 = impPopStack().val;
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, op2, op3, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::Sha1RotateOp:
            assert(sig->numArgs == 1);
            compFloatingPointUsed = true;
            return gtNewScalarHWIntrinsicNode(TYP_UINT, impPopStack().val, NI_ARM64_Sha1FixedRotate);

        default:
            JITDUMP("Not implemented hardware intrinsic form");
            assert(!"Unimplemented SIMD Intrinsic form");

            break;
    }
    return retNode;
}

#endif // FEATURE_HW_INTRINSICS
