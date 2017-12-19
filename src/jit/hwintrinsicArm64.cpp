// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hwintrinsicArm64.h"

#ifdef FEATURE_HW_INTRINSICS

namespace IsaFlag
{
enum Flag
{
#define HARDWARE_INTRINSIC_CLASS(flag, isa) isa = 1ULL << InstructionSet_##isa,
#include "hwintrinsiclistArm64.h"
    None     = 0,
    Base     = 1ULL << InstructionSet_Base,
    EveryISA = ~0ULL
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
    // Add lookupHWIntrinsic special cases see lookupHWIntrinsic() below
    //     NI_ARM64_IsSupported_True is used to expand get_IsSupported to const true
    //     NI_ARM64_IsSupported_False is used to expand get_IsSupported to const false
    //     NI_ARM64_PlatformNotSupported to throw PlatformNotSupported exception for every intrinsic not supported on the running platform
    {NI_ARM64_IsSupported_True,     "get_IsSupported",                 IsaFlag::EveryISA, HWIntrinsicInfo::IsSupported, HWIntrinsicInfo::None, {}},
    {NI_ARM64_IsSupported_False,    "::NI_ARM64_IsSupported_False",    IsaFlag::EveryISA, HWIntrinsicInfo::IsSupported, HWIntrinsicInfo::None, {}},
    {NI_ARM64_PlatformNotSupported, "::NI_ARM64_PlatformNotSupported", IsaFlag::EveryISA, HWIntrinsicInfo::Unsupported, HWIntrinsicInfo::None, {}},
#define HARDWARE_INTRINSIC(id, isa, name, form, i0, i1, i2, flags) \
    {id,                            #name,                             IsaFlag::isa,      HWIntrinsicInfo::form,        HWIntrinsicInfo::flags, { i0, i1, i2 }},
#include "hwintrinsiclistArm64.h"
};
// clang-format on

extern const char* getHWIntrinsicName(NamedIntrinsic intrinsic)
{
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].intrinsicName;
}

const HWIntrinsicInfo& Compiler::getHWIntrinsicInfo(NamedIntrinsic intrinsic)
{
    assert(intrinsic > NI_HW_INTRINSIC_START);
    assert(intrinsic < NI_HW_INTRINSIC_END);

    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1];
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
#define HARDWARE_INTRINSIC_CLASS(flag, isa)                                                                            \
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
//
// TODO-Throughput: replace sequential search by hash lookup
NamedIntrinsic Compiler::lookupHWIntrinsic(const char* className, const char* methodName)
{
    InstructionSet isa    = lookupHWIntrinsicISA(className);
    NamedIntrinsic result = NI_Illegal;
    if (isa != InstructionSet_NONE)
    {
        IsaFlag::Flag isaFlag = IsaFlag::flag(isa);
        for (int i = 0; i < NI_HW_INTRINSIC_END - NI_HW_INTRINSIC_START; i++)
        {
            if ((isaFlag & hwIntrinsicInfoArray[i].isaflags) &&
                strcmp(methodName, hwIntrinsicInfoArray[i].intrinsicName) == 0)
            {
                if (compSupports(isa))
                {
                    // Intrinsic is supported on platform
                    result = hwIntrinsicInfoArray[i].intrinsicID;
                }
                else
                {
                    // When the intrinsic class is not supported
                    // Return NI_ARM64_PlatformNotSupported for all intrinsics
                    // Return NI_ARM64_IsSupported_False for the IsSupported property
                    result = (hwIntrinsicInfoArray[i].intrinsicID != NI_ARM64_IsSupported_True)
                                 ? NI_ARM64_PlatformNotSupported
                                 : NI_ARM64_IsSupported_False;
                }
                break;
            }
        }
    }
    return result;
}

//------------------------------------------------------------------------
// impUnsupportedHWIntrinsic: returns a node for an unsupported HWIntrinsic
//
// Arguments:
//    helper     - JIT helper ID for the exception to be thrown
//    method     - method handle of the intrinsic function.
//    sig        - signature of the intrinsic call
//    mustExpand - true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    a gtNewMustThrowException if mustExpand is true; otherwise, nullptr
//
GenTree* Compiler::impUnsupportedHWIntrinsic(unsigned              helper,
                                             CORINFO_METHOD_HANDLE method,
                                             CORINFO_SIG_INFO*     sig,
                                             bool                  mustExpand)
{
    // We've hit some error case and may need to return a node for the given error.
    //
    // When `mustExpand=false`, we are attempting to inline the intrinsic directly into another method. In this
    // scenario, we need to return `nullptr` so that a GT_CALL to the intrinsic is emitted instead. This is to
    // ensure that everything continues to behave correctly when optimizations are enabled (e.g. things like the
    // inliner may expect the node we return to have a certain signature, and the `MustThrowException` node won't
    // match that).
    //
    // When `mustExpand=true`, we are in a GT_CALL to the intrinsic and are attempting to JIT it. This will generally
    // be in response to an indirect call (e.g. done via reflection) or in response to an earlier attempt returning
    // `nullptr` (under `mustExpand=false`). In that scenario, we are safe to return the `MustThrowException` node.

    if (mustExpand)
    {
        for (unsigned i = 0; i < sig->numArgs; i++)
        {
            impPopStack();
        }

        return gtNewMustThrowException(helper, JITtype2varType(sig->retType), sig->retTypeClass);
    }
    else
    {
        return nullptr;
    }
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
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO*     sig,
                                  bool                  mustExpand)
{
    GenTree*  retNode       = nullptr;
    GenTree*  op1           = nullptr;
    GenTree*  op2           = nullptr;
    var_types simdType      = TYP_UNKNOWN;
    var_types simdBaseType  = TYP_UNKNOWN;
    unsigned  simdSizeBytes = 0;

    // Instantiation type check
    switch (getHWIntrinsicInfo(intrinsic).form)
    {
        case HWIntrinsicInfo::SimdBinaryOp:
        case HWIntrinsicInfo::SimdUnaryOp:
            simdBaseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeClass, &simdSizeBytes);

            if (simdBaseType == TYP_UNKNOWN)
            {
                // TODO-FIXME Add CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED
                unsigned CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED = CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED;

                return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
            }
            simdType = getSIMDTypeForSize(simdSizeBytes);
            break;
        default:
            break;
    }

    switch (getHWIntrinsicInfo(intrinsic).form)
    {
        case HWIntrinsicInfo::IsSupported:
            return gtNewIconNode((intrinsic == NI_ARM64_IsSupported_True) ? 1 : 0);

        case HWIntrinsicInfo::Unsupported:
            return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);

        case HWIntrinsicInfo::SimdBinaryOp:
            // op1 is the first operand
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, op2, intrinsic, simdBaseType, simdSizeBytes);

        case HWIntrinsicInfo::SimdUnaryOp:
            op1 = impSIMDPopStack(simdType);

            return gtNewSimdHWIntrinsicNode(simdType, op1, nullptr, intrinsic, simdBaseType, simdSizeBytes);

        default:
            JITDUMP("Not implemented hardware intrinsic form");
            assert(!"Unimplemented SIMD Intrinsic form");

            break;
    }
    return retNode;
}

CORINFO_CLASS_HANDLE Compiler::gtGetStructHandleForHWSIMD(var_types simdType, var_types simdBaseType)
{
    if (simdType == TYP_SIMD16)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return Vector128FloatHandle;
            case TYP_DOUBLE:
                return Vector128DoubleHandle;
            case TYP_INT:
                return Vector128IntHandle;
            case TYP_USHORT:
                return Vector128UShortHandle;
            case TYP_UBYTE:
                return Vector128UByteHandle;
            case TYP_SHORT:
                return Vector128ShortHandle;
            case TYP_BYTE:
                return Vector128ByteHandle;
            case TYP_LONG:
                return Vector128LongHandle;
            case TYP_UINT:
                return Vector128UIntHandle;
            case TYP_ULONG:
                return Vector128ULongHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }
    else if (simdType == TYP_SIMD8)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return Vector64FloatHandle;
            case TYP_UINT:
                return Vector64UIntHandle;
            case TYP_USHORT:
                return Vector64UShortHandle;
            case TYP_UBYTE:
                return Vector64UByteHandle;
            case TYP_SHORT:
                return Vector64ShortHandle;
            case TYP_BYTE:
                return Vector64ByteHandle;
            case TYP_INT:
                return Vector64IntHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }

    return NO_CLASS_HANDLE;
}

#endif // FEATURE_HW_INTRINSICS
