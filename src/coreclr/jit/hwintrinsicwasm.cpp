// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

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
    if (strcmp(className, "WasmBase") == 0)
    {
        return InstructionSet_WasmBase;
    }
    else if (strcmp(className, "PackedSimd") == 0)
    {
        return InstructionSet_PackedSimd;
    }
    else if (strcmp(className, "Vector128") == 0)
    {
        return InstructionSet_Vector128;
    }

    return InstructionSet_ILLEGAL;
}

int HWIntrinsicInfo::lookupImmUpperBound(NamedIntrinsic id, unsigned int simdSize, var_types baseType)
{
    switch (id)
    {
        case NI_PackedSimd_ExtractScalar:
        case NI_PackedSimd_ReplaceScalar:
        case NI_PackedSimd_LoadScalarAndInsert:
        case NI_PackedSimd_StoreSelectedScalar:
            return Compiler::getSIMDVectorLength(simdSize, baseType) - 1;
        default:
            unreached();
    }

    return 0;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclosing class name
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
        return lookupInstructionSet(className);
    }

    return InstructionSet_ILLEGAL;
}

GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types simdBaseType)
{
    NYI_WASM_SIMD("impNonConstFallback");
    return nullptr;
}

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

    assert(varTypeIsArithmetic(simdBaseType));

    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;

    switch (intrinsic)
    {
        case NI_PackedSimd_CompareGreaterThan:
        {
            assert(sig->numArgs == 2);
            assert(simdSize == 16);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_CompareGreaterThanOrEqual:
        {
            assert(sig->numArgs == 2);
            assert(simdSize == 16);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_CompareLessThan:
        {
            assert(sig->numArgs == 2);
            assert(simdSize == 16);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_CompareLessThanOrEqual:
        {
            assert(sig->numArgs == 2);
            assert(simdSize == 16);

            op2 = impSIMDPopStack();
            op1 = impSIMDPopStack();

            retNode = gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_LoadVector128:
        {
            assert(sig->numArgs == 1);
            assert(simdSize == 16);

            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdLoadNode(retType, op1, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_LoadScalarVector128:
        case NI_PackedSimd_LoadScalarAndSplatVector128:
        case NI_PackedSimd_LoadScalarAndInsert:
        case NI_PackedSimd_LoadWideningVector128:
        {
            break;
        }

        case NI_PackedSimd_Store:
        {
            assert(sig->numArgs == 2);
            assert(simdSize == 16);

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
            {
                // If what we have is a BYREF, that's what we really want, so throw away the cast.
                op1 = op1->gtGetOp1();
            }

            retNode = gtNewSimdStoreNode(op1, op2, simdBaseType, simdSize);
            break;
        }

        case NI_PackedSimd_StoreSelectedScalar:
        {
            break;
        }

        default:
        {
            unreached();
        }
    }

    return retNode;
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

    // Position of the immediates (in eval order)
    int imm1Pos = -1;
    int imm2Pos = -1;

    int numArgs = HWIntrinsicInfo::lookupNumArgs(intrinsic);
    HWIntrinsicInfo::GetImmOpsPositions(intrinsic, &imm1Pos, &imm2Pos);
    if (imm1Pos >= 0)
    {
        int imm1StackPos = numArgs - imm1Pos;
        *immOp1Ptr       = impStackTop(imm1StackPos).val;
        assert(HWIntrinsicInfo::isImmOp(intrinsic, *immOp1Ptr));
    }

    if (imm2Pos >= 0)
    {
        int imm2StackPos = numArgs - imm2Pos;
        *immOp2Ptr       = impStackTop(imm2StackPos).val;
        assert(HWIntrinsicInfo::isImmOp(intrinsic, *immOp2Ptr));
    }
}
#endif
