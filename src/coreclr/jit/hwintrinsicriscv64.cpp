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

    if (className[0] == 'R')
    {
        if (strcmp(className, "RiscVBase") == 0)
        {
            // return InstructionSet_RiscVBase;
        }
    }

    return InstructionSet_NONE;
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
CORINFO_InstructionSet HWIntrinsicInfo::lookupIsa(const char* className,
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
        // case InstructionSet_RiscVBase:
        case InstructionSet_NONE:
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
        // case InstructionSet_RiscVBase:
        case InstructionSet_NONE:
            return true;

        default:
            return false;
    }
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
    assert(!HWIntrinsicInfo::HasImmediateOperand(intrinsic));
    return;
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
//    entryPoint      -- The entry point information required for R2R scenarios
//    simdBaseJitType -- generic argument of the intrinsic.
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
                                       CorInfoType           simdBaseJitType,
                                       var_types             retType,
                                       unsigned              simdSize,
                                       bool                  mustExpand)
{
    const HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrinsic);
    const int                 numArgs  = sig->numArgs;

    bool isScalar = (category == HW_Category_Scalar);
    assert(!HWIntrinsicInfo::isScalarIsa(HWIntrinsicInfo::lookupIsa(intrinsic)));

    assert(numArgs >= 0);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
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
        default:
            return nullptr;
    }
}

#endif // FEATURE_HW_INTRINSICS
