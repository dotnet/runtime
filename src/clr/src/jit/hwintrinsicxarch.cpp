// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
// clang-format off
#define HARDWARE_INTRINSIC(id, name, isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    {NI_##id, name, InstructionSet_##isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, static_cast<HWIntrinsicFlag>(flag)},
// clang-format on
#include "hwintrinsiclistxarch.h"
};

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
// lookupId: Gets the NamedIntrinsic for a given method name and InstructionSet
//
// Arguments:
//    className  -- The name of the class associated with the HWIntrinsic to lookup
//    methodName -- The name of the method associated with the HWIntrinsic to lookup
//    enclosingClassName -- The name of the enclosing class of X64 classes
//
// Return Value:
//    The NamedIntrinsic associated with methodName and isa
NamedIntrinsic HWIntrinsicInfo::lookupId(Compiler*   comp,
                                         const char* className,
                                         const char* methodName,
                                         const char* enclosingClassName)
{
    // TODO-Throughput: replace sequential search by binary search
    InstructionSet isa = lookupIsa(className, enclosingClassName);

    if (isa == InstructionSet_ILLEGAL)
    {
        // There are several platform-agnostic intrinsics (e.g., Vector64) that
        // are not supported in x86, so early return NI_Illegal
        return NI_Illegal;
    }

    bool isIsaSupported = comp->compSupports(isa) && comp->compSupportsHWIntrinsic(isa);

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
        if (isa != hwIntrinsicInfoArray[i].isa)
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
// X64VersionOfIsa: Gets the corresponding 64-bit only InstructionSet for a given InstructionSet
//
// Arguments:
//    isa -- The InstructionSet ID
//
// Return Value:
//    The 64-bit only InstructionSet associated with isa
static InstructionSet X64VersionOfIsa(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_SSE:
            return InstructionSet_SSE_X64;
        case InstructionSet_SSE2:
            return InstructionSet_SSE2_X64;
        case InstructionSet_SSE41:
            return InstructionSet_SSE41_X64;
        case InstructionSet_SSE42:
            return InstructionSet_SSE42_X64;
        case InstructionSet_BMI1:
            return InstructionSet_BMI1_X64;
        case InstructionSet_BMI2:
            return InstructionSet_BMI2_X64;
        case InstructionSet_LZCNT:
            return InstructionSet_LZCNT_X64;
        case InstructionSet_POPCNT:
            return InstructionSet_POPCNT_X64;
        default:
            unreached();
            return InstructionSet_ILLEGAL;
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

    return InstructionSet_ILLEGAL;
}

//------------------------------------------------------------------------
// lookupIsa: Gets the InstructionSet for a given class name and enclsoing class name
//
// Arguments:
//    className -- The name of the class associated with the InstructionSet to lookup
//    enclosingClassName -- The name of the enclosing class of X64 classes
//
// Return Value:
//    The InstructionSet associated with className and enclosingClassName
InstructionSet HWIntrinsicInfo::lookupIsa(const char* className, const char* enclosingClassName)
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
// lookupSimdSize: Gets the SimdSize for a given HWIntrinsic and signature
//
// Arguments:
//    id -- The ID associated with the HWIntrinsic to lookup
//   sig -- The signature of the HWIntrinsic to lookup
//
// Return Value:
//    The SIMD size for the HWIntrinsic associated with id and sig
//
// Remarks:
//    This function is only used by the importer. After importation, we can
//    get the SIMD size from the GenTreeHWIntrinsic node.
unsigned HWIntrinsicInfo::lookupSimdSize(Compiler* comp, NamedIntrinsic id, CORINFO_SIG_INFO* sig)
{
    if (HWIntrinsicInfo::HasFixedSimdSize(id))
    {
        return lookupSimdSize(id);
    }

    CORINFO_CLASS_HANDLE typeHnd = nullptr;

    if (JITtype2varType(sig->retType) == TYP_STRUCT)
    {
        typeHnd = sig->retTypeSigClass;
    }
    else if (HWIntrinsicInfo::BaseTypeFromFirstArg(id))
    {
        typeHnd = comp->info.compCompHnd->getArgClass(sig, sig->args);
    }
    else
    {
        assert(HWIntrinsicInfo::BaseTypeFromSecondArg(id));
        CORINFO_ARG_LIST_HANDLE secondArg = comp->info.compCompHnd->getArgNext(sig->args);
        typeHnd                           = comp->info.compCompHnd->getArgClass(sig, secondArg);
    }

    unsigned  simdSize = 0;
    var_types baseType = comp->getBaseTypeAndSizeOfSIMDType(typeHnd, &simdSize);
    assert((simdSize > 0) && (baseType != TYP_UNKNOWN));
    return simdSize;
}

//------------------------------------------------------------------------
// lookupNumArgs: Gets the number of args for a given HWIntrinsic node
//
// Arguments:
//    node -- The HWIntrinsic node to get the number of args for
//
// Return Value:
//    The number of args for the HWIntrinsic associated with node
int HWIntrinsicInfo::lookupNumArgs(const GenTreeHWIntrinsic* node)
{
    assert(node != nullptr);

    NamedIntrinsic id      = node->gtHWIntrinsicId;
    int            numArgs = lookupNumArgs(id);

    if (numArgs >= 0)
    {
        return numArgs;
    }

    assert(numArgs == -1);

    GenTree* op1 = node->gtGetOp1();

    if (op1 == nullptr)
    {
        return 0;
    }

    if (op1->OperIsList())
    {
        GenTreeArgList* list = op1->AsArgList();
        numArgs              = 0;

        do
        {
            numArgs++;
            list = list->Rest();
        } while (list != nullptr);

        return numArgs;
    }

    GenTree* op2 = node->gtGetOp2();

    return (op2 == nullptr) ? 1 : 2;
}

//------------------------------------------------------------------------
// lookupLastOp: Gets the last operand for a given HWIntrinsic node
//
// Arguments:
//    node   -- The HWIntrinsic node to get the last operand for
//
// Return Value:
//     The last operand for node
GenTree* HWIntrinsicInfo::lookupLastOp(const GenTreeHWIntrinsic* node)
{
    int numArgs = lookupNumArgs(node);

    switch (numArgs)
    {
        case 0:
        {
            assert(node->gtGetOp1() == nullptr);
            assert(node->gtGetOp2() == nullptr);
            return nullptr;
        }

        case 1:
        {
            assert(node->gtGetOp1() != nullptr);
            assert(!node->gtGetOp1()->OperIsList());
            assert(node->gtGetOp2() == nullptr);

            return node->gtGetOp1();
        }

        case 2:
        {
            assert(node->gtGetOp1() != nullptr);
            assert(!node->gtGetOp1()->OperIsList());
            assert(node->gtGetOp2() != nullptr);

            return node->gtGetOp2();
        }

        case 3:
        {
            assert(node->gtGetOp1() != nullptr);
            assert(node->gtGetOp1()->OperIsList());
            assert(node->gtGetOp2() == nullptr);
            assert(node->gtGetOp1()->AsArgList()->Rest()->Rest()->Current() != nullptr);
            assert(node->gtGetOp1()->AsArgList()->Rest()->Rest()->Rest() == nullptr);

            return node->gtGetOp1()->AsArgList()->Rest()->Rest()->Current();
        }

        case 5:
        {
            assert(node->gtGetOp1() != nullptr);
            assert(node->gtGetOp1()->OperIsList());
            assert(node->gtGetOp2() == nullptr);
            assert(node->gtGetOp1()->AsArgList()->Rest()->Rest()->Rest()->Rest()->Current() != nullptr);
            assert(node->gtGetOp1()->AsArgList()->Rest()->Rest()->Rest()->Rest()->Rest() == nullptr);

            return node->gtGetOp1()->AsArgList()->Rest()->Rest()->Rest()->Rest()->Current();
        }

        default:
        {
            unreached();
            return nullptr;
        }
    }
}

//------------------------------------------------------------------------
// isImmOp: Gets a value that indicates whether the HWIntrinsic node has an imm operand
//
// Arguments:
//    id -- The NamedIntrinsic associated with the HWIntrinsic to lookup
//    op -- The operand to check
//
// Return Value:
//     true if the node has an imm operand; otherwise, false
bool HWIntrinsicInfo::isImmOp(NamedIntrinsic id, const GenTree* op)
{
    if (HWIntrinsicInfo::lookupCategory(id) != HW_Category_IMM)
    {
        return false;
    }

    if (!HWIntrinsicInfo::MaybeImm(id))
    {
        return true;
    }

    if (genActualType(op->TypeGet()) != TYP_INT)
    {
        return false;
    }

    return true;
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

    if (isAVX2GatherIntrinsic(id))
    {
        return ival == 1 || ival == 2 || ival == 4 || ival == 8;
    }
    else
    {
        return ival <= lookupImmUpperBound(id) && ival >= 0;
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
        case InstructionSet_AES:
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2_X64:
        case InstructionSet_FMA:
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
        case InstructionSet_PCLMULQDQ:
        case InstructionSet_POPCNT:
        case InstructionSet_POPCNT_X64:
        case InstructionSet_SSE:
        case InstructionSet_SSE_X64:
        case InstructionSet_SSE2:
        case InstructionSet_SSE2_X64:
        case InstructionSet_SSE3:
        case InstructionSet_SSSE3:
        case InstructionSet_SSE41:
        case InstructionSet_SSE41_X64:
        case InstructionSet_SSE42:
        case InstructionSet_SSE42_X64:
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
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
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2_X64:
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
        case InstructionSet_POPCNT:
        case InstructionSet_POPCNT_X64:
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
// getArgForHWIntrinsic: get the argument from the stack and match  the signature
//
// Arguments:
//    argType   -- the required type of argument
//    argClass  -- the class handle of argType
//
// Return Value:
//     get the argument at the given index from the stack and match  the signature
//
GenTree* Compiler::getArgForHWIntrinsic(var_types argType, CORINFO_CLASS_HANDLE argClass)
{
    GenTree* arg = nullptr;
    if (argType == TYP_STRUCT)
    {
        unsigned int argSizeBytes;
        var_types    base = getBaseTypeAndSizeOfSIMDType(argClass, &argSizeBytes);
        argType           = getSIMDTypeForSize(argSizeBytes);
        assert((argType == TYP_SIMD32) || (argType == TYP_SIMD16));
        arg = impSIMDPopStack(argType);
        assert((arg->TypeGet() == TYP_SIMD16) || (arg->TypeGet() == TYP_SIMD32));
    }
    else
    {
        assert(varTypeIsArithmetic(argType));
        arg = impPopStack().val;
        assert(varTypeIsArithmetic(arg->TypeGet()));
        assert(genActualType(arg->gtType) == genActualType(argType));
    }
    return arg;
}

//------------------------------------------------------------------------
// impNonConstFallback: convert certain SSE2/AVX2 shift intrinsic to its semantic alternative when the imm-arg is
// not a compile-time constant
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    simdType   -- Vector type
//    baseType   -- base type of the Vector128/256<T>
//
// Return Value:
//     return the IR of semantic alternative on non-const imm-arg
//
GenTree* Compiler::impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, var_types baseType)
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
            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impSIMDPopStack(simdType);
            GenTree* tmpOp =
                gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_SSE2_ConvertScalarToVector128Int32, TYP_INT, 16);
            return gtNewSimdHWIntrinsicNode(simdType, op1, tmpOp, intrinsic, baseType, genTypeSize(simdType));
        }

        default:
            unreached();
            return nullptr;
    }
}

//------------------------------------------------------------------------
// addRangeCheckIfNeeded: add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    lastOp     -- the last operand of the intrinsic that points to the imm-arg
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//     add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckIfNeeded(NamedIntrinsic intrinsic, GenTree* lastOp, bool mustExpand)
{
    assert(lastOp != nullptr);
    // Full-range imm-intrinsics do not need the range-check
    // because the imm-parameter of the intrinsic method is a byte.
    // AVX2 Gather intrinsics no not need the range-check
    // because their imm-parameter have discrete valid values that are handle by managed code
    if (mustExpand && !HWIntrinsicInfo::HasFullRangeImm(intrinsic) && HWIntrinsicInfo::isImmOp(intrinsic, lastOp) &&
        !HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic))
    {
        assert(!lastOp->IsCnsIntOrI());
        GenTree* upperBoundNode =
            new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, HWIntrinsicInfo::lookupImmUpperBound(intrinsic));
        GenTree* index = nullptr;
        if ((lastOp->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            index = fgInsertCommaFormTemp(&lastOp);
        }
        else
        {
            index = gtCloneExpr(lastOp);
        }
        GenTreeBoundsChk* hwIntrinsicChk = new (this, GT_HW_INTRINSIC_CHK)
            GenTreeBoundsChk(GT_HW_INTRINSIC_CHK, TYP_VOID, index, upperBoundNode, SCK_RNGCHK_FAIL);
        hwIntrinsicChk->gtThrowKind = SCK_ARG_RNG_EXCPN;
        return gtNewOperNode(GT_COMMA, lastOp->TypeGet(), hwIntrinsicChk, lastOp);
    }
    else
    {
        return lastOp;
    }
}

//------------------------------------------------------------------------
// compSupportsHWIntrinsic: compiler support of hardware intrinsics
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true if EnableHWIntrinsic=true and
//    - isa is a scalar ISA
//    - isa is a SIMD ISA and featureSIMD=true
//    - isa is fully implemented or EnableIncompleteISAClass=true
bool Compiler::compSupportsHWIntrinsic(InstructionSet isa)
{
    return JitConfig.EnableHWIntrinsic() && (featureSIMD || HWIntrinsicInfo::isScalarIsa(isa)) &&
           (
#ifdef DEBUG
               JitConfig.EnableIncompleteISAClass() ||
#endif
               HWIntrinsicInfo::isFullyImplementedIsa(isa));
}

//------------------------------------------------------------------------
// impIsTableDrivenHWIntrinsic:
//
// Arguments:
//    category - category of a HW intrinsic
//
// Return Value:
//    returns true if this category can be table-driven in the importer
//
static bool impIsTableDrivenHWIntrinsic(NamedIntrinsic intrinsicId, HWIntrinsicCategory category)
{
    // HW_Flag_NoCodeGen implies this intrinsic should be manually morphed in the importer.
    return (category != HW_Category_Special) && (category != HW_Category_Scalar) &&
           HWIntrinsicInfo::RequiresCodegen(intrinsicId) && !HWIntrinsicInfo::HasSpecialImport(intrinsicId);
}

//------------------------------------------------------------------------
// impHWIntrinsic: dispatch hardware intrinsics to their own implementation
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
    InstructionSet      isa      = HWIntrinsicInfo::lookupIsa(intrinsic);
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
    }

    // Avoid checking stacktop for 0-op intrinsics
    if (sig->numArgs > 0 && HWIntrinsicInfo::isImmOp(intrinsic, impStackTop().val))
    {
        GenTree* lastOp = impStackTop().val;
        // The imm-HWintrinsics that do not accept all imm8 values may throw
        // ArgumentOutOfRangeException when the imm argument is not in the valid range
        if (!HWIntrinsicInfo::HasFullRangeImm(intrinsic))
        {
            if (!mustExpand && lastOp->IsCnsIntOrI() &&
                !HWIntrinsicInfo::isInImmRange(intrinsic, (int)lastOp->AsIntCon()->IconValue()))
            {
                return nullptr;
            }
        }

        if (!lastOp->IsCnsIntOrI())
        {
            if (HWIntrinsicInfo::NoJmpTableImm(intrinsic))
            {
                return impNonConstFallback(intrinsic, retType, baseType);
            }

            if (!mustExpand)
            {
                // When the imm-argument is not a constant and we are not being forced to expand, we need to
                // return nullptr so a GT_CALL to the intrinsic method is emitted instead. The
                // intrinsic method is recursive and will be forced to expand, at which point
                // we emit some less efficient fallback code.
                return nullptr;
            }
        }
    }

    if (HWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic is being on SIMD fields, but
        // where no SIMD local vars are in use. This is the same logic as is used for FEATURE_SIMD.
        compFloatingPointUsed = true;
    }

    // table-driven importer of simple intrinsics
    if (impIsTableDrivenHWIntrinsic(intrinsic, category))
    {
        if ((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic) ||
            HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
        {
            CORINFO_ARG_LIST_HANDLE arg = sig->args;

            if ((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
            {
                arg = info.compCompHnd->getArgNext(arg);
            }

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

            assert(baseType != TYP_UNKNOWN);
        }

        unsigned                simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
        CORINFO_ARG_LIST_HANDLE argList  = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        var_types               argType = TYP_UNKNOWN;

        assert(numArgs >= 0);
        if ((HWIntrinsicInfo::lookupIns(intrinsic, baseType) == INS_invalid) || ((simdSize != 32) && (simdSize != 16)))
        {
            assert(!"Unexpected HW Intrinsic");
            return nullptr;
        }

        GenTreeHWIntrinsic* retNode = nullptr;
        GenTree*            op1     = nullptr;
        GenTree*            op2     = nullptr;

        switch (numArgs)
        {
            case 0:
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
                break;
            case 1:
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);
                if ((category == HW_Category_MemoryLoad) && op1->OperIs(GT_CAST))
                {
                    // Although the API specifies a pointer, if what we have is a BYREF, that's what
                    // we really want, so throw away the cast.
                    if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                    {
                        op1 = op1->gtGetOp1();
                    }
                }
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
                break;
            case 2:
                argType = JITtype2varType(
                    strip(info.compCompHnd->getArgType(sig, info.compCompHnd->getArgNext(argList), &argClass)));
                op2 = getArgForHWIntrinsic(argType, argClass);

                op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, baseType, simdSize);
                break;

            case 3:
            {
                CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
                CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

                argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
                GenTree* op3 = getArgForHWIntrinsic(argType, argClass);

                op3 = addRangeCheckIfNeeded(intrinsic, op3, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2     = getArgForHWIntrinsic(argType, argClass);
                var_types op2Type;
                if (intrinsic == NI_AVX2_GatherVector128 || intrinsic == NI_AVX2_GatherVector256)
                {
                    assert(varTypeIsSIMD(op2->TypeGet()));
                    op2Type = getBaseTypeOfSIMDType(argClass);
                }

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, baseType, simdSize);

                if (intrinsic == NI_AVX2_GatherVector128 || intrinsic == NI_AVX2_GatherVector256)
                {
                    assert(varTypeIsSIMD(op2->TypeGet()));
                    retNode->AsHWIntrinsic()->gtIndexBaseType = op2Type;
                }
                break;
            }

            default:
                unreached();
        }

        bool isMemoryStore = retNode->OperIsMemoryStore();
        if (isMemoryStore || retNode->OperIsMemoryLoad())
        {
            if (isMemoryStore)
            {
                // A MemoryStore operation is an assignment
                retNode->gtFlags |= GTF_ASG;
            }

            // This operation contains an implicit indirection
            //   it could point into the gloabal heap or
            //   it could throw a null reference exception.
            //
            retNode->gtFlags |= (GTF_GLOB_REF | GTF_EXCEPT);
        }
        return retNode;
    }

    // other intrinsics need special importation
    switch (isa)
    {
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
            return impBaseIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE:
            return impSSEIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE2:
            return impSSE2Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE42:
        case InstructionSet_SSE42_X64:
            return impSSE42Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
            return impAvxOrAvx2Intrinsic(intrinsic, method, sig, mustExpand);

        case InstructionSet_AES:
            return impAESIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_BMI1:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
            return impBMI1OrBMI2Intrinsic(intrinsic, method, sig, mustExpand);

        case InstructionSet_FMA:
            return impFMAIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
            return impLZCNTIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_PCLMULQDQ:
            return impPCLMULQDQIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_POPCNT:
        case InstructionSet_POPCNT_X64:
            return impPOPCNTIntrinsic(intrinsic, method, sig, mustExpand);
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
//    sig        -- signature of the intrinsic call
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impBaseIntrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;

    if (!featureSIMD)
    {
        return nullptr;
    }

    unsigned  simdSize = 0;
    var_types baseType = TYP_UNKNOWN;
    var_types retType  = JITtype2varType(sig->retType);

    assert(!sig->hasThis());

    if (HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic))
    {
        baseType = getBaseTypeAndSizeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args), &simdSize);

        if (retType == TYP_STRUCT)
        {
            unsigned  retSimdSize = 0;
            var_types retBasetype = getBaseTypeAndSizeOfSIMDType(sig->retTypeClass, &retSimdSize);
            if (!varTypeIsArithmetic(retBasetype))
            {
                return nullptr;
            }
            retType = getSIMDTypeForSize(retSimdSize);
        }
    }
    else
    {
        assert(retType == TYP_STRUCT);
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeClass, &simdSize);
        retType  = getSIMDTypeForSize(simdSize);
    }

    if (!varTypeIsArithmetic(baseType))
    {
        return nullptr;
    }

    switch (intrinsic)
    {
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
            if (!compSupports(InstructionSet_AVX))
            {
                // We don't want to deal with TYP_SIMD32 if the compiler doesn't otherwise support the type.
                break;
            }

            __fallthrough;
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

        case NI_Vector128_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#ifdef _TARGET_X86_
            if (varTypeIsLong(baseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // _TARGET_X86_

            if (compSupports(InstructionSet_SSE2) || (compSupports(InstructionSet_SSE) && (baseType == TYP_FLOAT)))
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector128_ToScalar:
        {
            assert(sig->numArgs == 1);

            if (compSupports(InstructionSet_SSE) && varTypeIsFloating(baseType))
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, 16);
            }
            break;
        }

        case NI_Vector128_ToVector256:
        case NI_Vector128_ToVector256Unsafe:
        case NI_Vector256_GetLower:
        {
            assert(sig->numArgs == 1);

            if (compSupports(InstructionSet_AVX))
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector128_Zero:
        {
            assert(sig->numArgs == 0);

            if (compSupports(InstructionSet_SSE))
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector256_CreateScalarUnsafe:
        {
            assert(sig->numArgs == 1);

#ifdef _TARGET_X86_
            if (varTypeIsLong(baseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                break;
            }
#endif // _TARGET_X86_

            if (compSupports(InstructionSet_AVX))
            {
                op1     = impPopStack().val;
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector256_ToScalar:
        {
            assert(sig->numArgs == 1);

            if (compSupports(InstructionSet_AVX) && varTypeIsFloating(baseType))
            {
                op1     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, 32);
            }
            break;
        }

        case NI_Vector256_Zero:
        {
            assert(sig->numArgs == 0);

            if (compSupports(InstructionSet_AVX))
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
            }
            break;
        }

        case NI_Vector256_WithElement:
        {
            if (!compSupports(InstructionSet_AVX))
            {
                // Using software fallback if JIT/hardware don't support AVX instructions and YMM registers
                return nullptr;
            }
            __fallthrough;
        }

        case NI_Vector128_WithElement:
        {
            assert(sig->numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;
            if (!compSupports(InstructionSet_SSE2) || !varTypeIsArithmetic(baseType) || !indexOp->OperIsConst())
            {
                // Using software fallback if
                // 1. JIT/hardware don't support SSE2 instructions
                // 2. baseType is not a numeric type (throw execptions)
                // 3. index is not a constant
                return nullptr;
            }

            switch (baseType)
            {
                // Using software fallback if baseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    if (!compSupports(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_LONG:
                case TYP_ULONG:
                    if (!compSupports(InstructionSet_SSE41_X64))
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
                    break;
            }

            ssize_t imm8       = indexOp->AsIntCon()->IconValue();
            ssize_t cachedImm8 = imm8;
            ssize_t count      = simdSize / genTypeSize(baseType);

            if (imm8 >= count || imm8 < 0)
            {
                // Using software fallback if index is out of range (throw exeception)
                return nullptr;
            }

            GenTree* valueOp = impPopStack().val;
            impPopStack();
            GenTree* vectorOp = impSIMDPopStack(getSIMDTypeForSize(simdSize));

            GenTree* clonedVectorOp = nullptr;

            if (simdSize == 32)
            {
                // Extract the half vector that will be modified
                assert(compSupports(InstructionSet_AVX));

                // copy `vectorOp` to accept the modified half vector
                vectorOp = impCloneExpr(vectorOp, &clonedVectorOp, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                        nullptr DEBUGARG("Clone Vector for Vector256<T>.WithElement"));

                if (imm8 >= count / 2)
                {
                    imm8 -= count / 2;
                    vectorOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, gtNewIconNode(1), NI_AVX_ExtractVector128,
                                                        baseType, simdSize);
                }
                else
                {
                    vectorOp =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, NI_Vector256_GetLower, baseType, simdSize);
                }
            }

            GenTree* immNode = gtNewIconNode(imm8);

            switch (baseType)
            {
                case TYP_LONG:
                case TYP_ULONG:
                    retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, valueOp, immNode, NI_SSE41_X64_Insert,
                                                       baseType, 16);
                    break;

                case TYP_FLOAT:
                {
                    if (!compSupports(InstructionSet_SSE41))
                    {
                        // Emulate Vector128<float>.WithElement by SSE instructions
                        if (imm8 == 0)
                        {
                            // vector.WithElement(0, value)
                            // =>
                            // movss   xmm0, xmm1 (xmm0 = vector, xmm1 = value)
                            valueOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp, NI_Vector128_CreateScalarUnsafe,
                                                               TYP_FLOAT, 16);
                            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, valueOp, NI_SSE_MoveScalar,
                                                               TYP_FLOAT, 16);
                        }
                        else if (imm8 == 1)
                        {
                            // vector.WithElement(1, value)
                            // =>
                            // shufps  xmm1, xmm0, 0   (xmm0 = vector, xmm1 = value)
                            // shufps  xmm1, xmm0, 226
                            GenTree* tmpOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp,
                                                                      NI_Vector128_CreateScalarUnsafe, TYP_FLOAT, 16);
                            GenTree* dupVectorOp = nullptr;
                            vectorOp = impCloneExpr(vectorOp, &dupVectorOp, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                                    nullptr DEBUGARG("Clone Vector for Vector128<float>.WithElement"));
                            tmpOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmpOp, vectorOp, gtNewIconNode(0),
                                                             NI_SSE_Shuffle, TYP_FLOAT, 16);
                            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmpOp, dupVectorOp, gtNewIconNode(226),
                                                               NI_SSE_Shuffle, TYP_FLOAT, 16);
                        }
                        else
                        {
                            ssize_t controlBits1 = 0;
                            ssize_t controlBits2 = 0;
                            if (imm8 == 2)
                            {
                                controlBits1 = 48;
                                controlBits2 = 132;
                            }
                            else
                            {
                                controlBits1 = 32;
                                controlBits2 = 36;
                            }
                            // vector.WithElement(2, value)
                            // =>
                            // shufps  xmm1, xmm0, 48   (xmm0 = vector, xmm1 = value)
                            // shufps  xmm0, xmm1, 132
                            //
                            // vector.WithElement(3, value)
                            // =>
                            // shufps  xmm1, xmm0, 32   (xmm0 = vector, xmm1 = value)
                            // shufps  xmm0, xmm1, 36
                            GenTree* tmpOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp,
                                                                      NI_Vector128_CreateScalarUnsafe, TYP_FLOAT, 16);
                            GenTree* dupVectorOp = nullptr;
                            vectorOp = impCloneExpr(vectorOp, &dupVectorOp, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                                    nullptr DEBUGARG("Clone Vector for Vector128<float>.WithElement"));
                            valueOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, tmpOp, gtNewIconNode(controlBits1),
                                                               NI_SSE_Shuffle, TYP_FLOAT, 16);
                            retNode =
                                gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp, dupVectorOp, gtNewIconNode(controlBits2),
                                                         NI_SSE_Shuffle, TYP_FLOAT, 16);
                        }
                        break;
                    }
                    else
                    {
                        valueOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp, NI_Vector128_CreateScalarUnsafe,
                                                           TYP_FLOAT, 16);
                        immNode->AsIntCon()->SetIconValue(imm8 * 16);
                        __fallthrough;
                    }
                }

                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    retNode =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, valueOp, immNode, NI_SSE41_Insert, baseType, 16);
                    break;

                case TYP_SHORT:
                case TYP_USHORT:
                    retNode =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, valueOp, immNode, NI_SSE2_Insert, baseType, 16);
                    break;

                case TYP_DOUBLE:
                {
                    // vector.WithElement(0, value)
                    // =>
                    // movsd   xmm0, xmm1  (xmm0 = vector, xmm1 = value)
                    //
                    // vector.WithElement(1, value)
                    // =>
                    // unpcklpd  xmm0, xmm1  (xmm0 = vector, xmm1 = value)
                    valueOp =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, valueOp, NI_Vector128_CreateScalarUnsafe, TYP_DOUBLE, 16);
                    NamedIntrinsic in = (imm8 == 0) ? NI_SSE2_MoveScalar : NI_SSE2_UnpackLow;
                    retNode           = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, valueOp, in, TYP_DOUBLE, 16);
                    break;
                }

                default:
                    unreached();
                    break;
            }

            if (simdSize == 32)
            {
                assert(clonedVectorOp);
                int upperOrLower = (cachedImm8 >= count / 2) ? 1 : 0;
                retNode = gtNewSimdHWIntrinsicNode(retType, clonedVectorOp, retNode, gtNewIconNode(upperOrLower),
                                                   NI_AVX_InsertVector128, baseType, simdSize);
            }

            break;
        }

        case NI_Vector256_GetElement:
        {
            if (!compSupports(InstructionSet_AVX))
            {
                // Using software fallback if JIT/hardware don't support AVX instructions and YMM registers
                return nullptr;
            }
            __fallthrough;
        }

        case NI_Vector128_GetElement:
        {
            assert(sig->numArgs == 2);
            GenTree* indexOp = impStackTop().val;
            if (!compSupports(InstructionSet_SSE2) || !varTypeIsArithmetic(baseType) || !indexOp->OperIsConst())
            {
                // Using software fallback if
                // 1. JIT/hardware don't support SSE2 instructions
                // 2. baseType is not a numeric type (throw execptions)
                // 3. index is not a constant
                return nullptr;
            }

            switch (baseType)
            {
                // Using software fallback if baseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    if (!compSupports(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;

                case TYP_LONG:
                case TYP_ULONG:
                    if (!compSupports(InstructionSet_SSE41_X64))
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
                    break;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(baseType);

            if (imm8 >= count || imm8 < 0)
            {
                // Using software fallback if index is out of range (throw exeception)
                return nullptr;
            }

            impPopStack();
            GenTree*       vectorOp     = impSIMDPopStack(getSIMDTypeForSize(simdSize));
            NamedIntrinsic resIntrinsic = NI_Illegal;

            if (simdSize == 32)
            {
                assert(compSupports(InstructionSet_AVX));

                if (imm8 >= count / 2)
                {
                    imm8 -= count / 2;
                    vectorOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, gtNewIconNode(1), NI_AVX_ExtractVector128,
                                                        baseType, simdSize);
                }
                else
                {
                    vectorOp =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, NI_Vector256_GetLower, baseType, simdSize);
                }
            }

            if (imm8 == 0 && (genTypeSize(baseType) >= 4))
            {
                switch (baseType)
                {
                    case TYP_LONG:
                        resIntrinsic = NI_SSE2_X64_ConvertToInt64;
                        break;

                    case TYP_ULONG:
                        resIntrinsic = NI_SSE2_X64_ConvertToUInt64;
                        break;

                    case TYP_INT:
                        resIntrinsic = NI_SSE2_ConvertToInt32;
                        break;

                    case TYP_UINT:
                        resIntrinsic = NI_SSE2_ConvertToUInt32;
                        break;

                    case TYP_FLOAT:
                    case TYP_DOUBLE:
                        resIntrinsic = NI_Vector128_ToScalar;
                        break;

                    default:
                        unreached();
                }

                return gtNewSimdHWIntrinsicNode(retType, vectorOp, resIntrinsic, baseType, 16);
            }

            GenTree* immNode = gtNewIconNode(imm8);

            switch (baseType)
            {
                case TYP_LONG:
                case TYP_ULONG:
                    retNode = gtNewSimdHWIntrinsicNode(retType, vectorOp, immNode, NI_SSE41_X64_Extract, baseType, 16);
                    break;

                case TYP_FLOAT:
                {
                    if (!compSupports(InstructionSet_SSE41))
                    {
                        assert(imm8 >= 1);
                        assert(imm8 <= 3);
                        // Emulate Vector128<float>.GetElement(i) by SSE instructions
                        // vector.GetElement(i)
                        // =>
                        // shufps  xmm0, xmm0, control
                        // (xmm0 = vector, control = i + 228)
                        immNode->AsIntCon()->SetIconValue(228 + imm8);
                        GenTree* clonedVectorOp = nullptr;
                        vectorOp = impCloneExpr(vectorOp, &clonedVectorOp, NO_CLASS_HANDLE, (unsigned)CHECK_SPILL_ALL,
                                                nullptr DEBUGARG("Clone Vector for Vector128<float>.GetElement"));
                        vectorOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, clonedVectorOp, immNode,
                                                            NI_SSE_Shuffle, TYP_FLOAT, 16);
                        return gtNewSimdHWIntrinsicNode(retType, vectorOp, NI_Vector128_ToScalar, TYP_FLOAT, 16);
                    }
                    __fallthrough;
                }

                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                    retNode = gtNewSimdHWIntrinsicNode(retType, vectorOp, immNode, NI_SSE41_Extract, baseType, 16);
                    break;

                case TYP_BYTE:
                    // We do not have SSE41/SSE2 Extract APIs on signed small int, so need a CAST on the result
                    retNode = gtNewSimdHWIntrinsicNode(TYP_UBYTE, vectorOp, immNode, NI_SSE41_Extract, TYP_UBYTE, 16);
                    retNode = gtNewCastNode(TYP_INT, retNode, true, TYP_BYTE);
                    break;

                case TYP_SHORT:
                case TYP_USHORT:
                    // We do not have SSE41/SSE2 Extract APIs on signed small int, so need a CAST on the result
                    retNode = gtNewSimdHWIntrinsicNode(TYP_USHORT, vectorOp, immNode, NI_SSE2_Extract, TYP_USHORT, 16);
                    if (baseType == TYP_SHORT)
                    {
                        retNode = gtNewCastNode(TYP_INT, retNode, true, TYP_SHORT);
                    }
                    break;

                case TYP_DOUBLE:
                    assert(imm8 == 1);
                    // vector.GetElement(1)
                    // =>
                    // pshufd xmm1, xmm0, 0xEE (xmm0 = vector)
                    vectorOp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, gtNewIconNode(0xEE), NI_SSE2_Shuffle,
                                                        TYP_INT, 16);
                    retNode = gtNewSimdHWIntrinsicNode(TYP_DOUBLE, vectorOp, NI_Vector128_ToScalar, TYP_DOUBLE, 16);
                    break;

                default:
                    unreached();
            }

            break;
        }

        default:
        {
            unreached();
            break;
        }
    }

    return retNode;
}

GenTree* Compiler::impSSEIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    GenTree* retNode  = nullptr;
    GenTree* op1      = nullptr;
    GenTree* op2      = nullptr;
    GenTree* op3      = nullptr;
    GenTree* op4      = nullptr;
    int      simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);

    // The Prefetch and StoreFence intrinsics don't take any SIMD operands
    // and have a simdSize of 0
    assert((simdSize == 16) || (simdSize == 0));

    switch (intrinsic)
    {
        case NI_SSE_Prefetch0:
        case NI_SSE_Prefetch1:
        case NI_SSE_Prefetch2:
        case NI_SSE_PrefetchNonTemporal:
        {
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, intrinsic, TYP_UBYTE, 0);
            break;
        }

        case NI_SSE_StoreFence:
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, TYP_VOID, 0);
            break;

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE2Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    int       ival     = -1;
    int       simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
    var_types baseType = TYP_UNKNOWN;
    var_types retType  = TYP_UNKNOWN;

    // The  fencing intrinsics don't take any operands and simdSize is 0
    assert((simdSize == 16) || (simdSize == 0));

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    var_types               argType = TYP_UNKNOWN;

    switch (intrinsic)
    {
        case NI_SSE2_CompareLessThan:
        {
            assert(sig->numArgs == 2);
            op2      = impSIMDPopStack(TYP_SIMD16);
            op1      = impSIMDPopStack(TYP_SIMD16);
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            if (baseType == TYP_DOUBLE)
            {
                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, intrinsic, baseType, simdSize);
            }
            else
            {
                retNode =
                    gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, NI_SSE2_CompareGreaterThan, baseType, simdSize);
            }
            break;
        }

        case NI_SSE2_LoadFence:
        case NI_SSE2_MemoryFence:
        {
            assert(sig->numArgs == 0);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            assert(simdSize == 0);

            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, intrinsic, TYP_VOID, simdSize);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        {
            assert(sig->numArgs == 2);
            assert(JITtype2varType(sig->retType) == TYP_VOID);
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, NI_SSE2_StoreNonTemporal, op2->TypeGet(), 0);
            break;
        }

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE42Intrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types callType = JITtype2varType(sig->retType);

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    CORINFO_CLASS_HANDLE    argClass;
    CorInfoType             corType;

    switch (intrinsic)
    {
        case NI_SSE42_Crc32:
        case NI_SSE42_X64_Crc32:
            assert(sig->numArgs == 2);
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            argList = info.compCompHnd->getArgNext(argList);                        // the second argument
            corType = strip(info.compCompHnd->getArgType(sig, argList, &argClass)); // type of the second argument

            retNode = gtNewScalarHWIntrinsicNode(callType, op1, op2, intrinsic);

            // TODO - currently we use the BaseType to bring the type of the second argument
            // to the code generator. May encode the overload info in other way.
            retNode->gtHWIntrinsic.gtSIMDBaseType = JITtype2varType(corType);
            break;

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAvxOrAvx2Intrinsic(NamedIntrinsic        intrinsic,
                                         CORINFO_METHOD_HANDLE method,
                                         CORINFO_SIG_INFO*     sig,
                                         bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types baseType = TYP_UNKNOWN;
    int       simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);

    switch (intrinsic)
    {
        case NI_AVX2_PermuteVar8x32:
        {
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            // swap the two operands
            GenTree* indexVector  = impSIMDPopStack(TYP_SIMD32);
            GenTree* sourceVector = impSIMDPopStack(TYP_SIMD32);
            retNode =
                gtNewSimdHWIntrinsicNode(TYP_SIMD32, indexVector, sourceVector, NI_AVX2_PermuteVar8x32, baseType, 32);
            break;
        }

        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
        {
            CORINFO_ARG_LIST_HANDLE argList = sig->args;
            CORINFO_CLASS_HANDLE    argClass;
            var_types               argType = TYP_UNKNOWN;
            unsigned int            sizeBytes;
            baseType          = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
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

            argType                 = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            var_types indexbaseType = getBaseTypeOfSIMDType(argClass);
            GenTree*  op3           = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op3);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op2);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);
            SetOpLclRelatedToSIMDIntrinsic(op1);

            GenTree* opList = new (this, GT_LIST) GenTreeArgList(op1, gtNewArgList(op2, op3, op4, op5));
            retNode = new (this, GT_HWIntrinsic) GenTreeHWIntrinsic(retType, opList, intrinsic, baseType, simdSize);
            retNode->AsHWIntrinsic()->gtIndexBaseType = indexbaseType;
            break;
        }

        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAESIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impBMI1OrBMI2Intrinsic(NamedIntrinsic        intrinsic,
                                          CORINFO_METHOD_HANDLE method,
                                          CORINFO_SIG_INFO*     sig,
                                          bool                  mustExpand)
{
    var_types callType = JITtype2varType(sig->retType);

    switch (intrinsic)
    {
        case NI_BMI1_AndNot:
        case NI_BMI1_X64_AndNot:
        case NI_BMI2_ParallelBitDeposit:
        case NI_BMI2_ParallelBitExtract:
        case NI_BMI2_X64_ParallelBitDeposit:
        case NI_BMI2_X64_ParallelBitExtract:
        {
            assert(sig->numArgs == 2);

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;

            return gtNewScalarHWIntrinsicNode(callType, op1, op2, intrinsic);
        }

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

        case NI_BMI1_ExtractLowestSetBit:
        case NI_BMI1_GetMaskUpToLowestSetBit:
        case NI_BMI1_ResetLowestSetBit:
        case NI_BMI1_TrailingZeroCount:
        case NI_BMI1_X64_ExtractLowestSetBit:
        case NI_BMI1_X64_GetMaskUpToLowestSetBit:
        case NI_BMI1_X64_ResetLowestSetBit:
        case NI_BMI1_X64_TrailingZeroCount:
        {
            assert(sig->numArgs == 1);
            GenTree* op1 = impPopStack().val;
            return gtNewScalarHWIntrinsicNode(callType, op1, intrinsic);
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

        case NI_BMI2_MultiplyNoFlags:
        case NI_BMI2_X64_MultiplyNoFlags:
        {
            assert(sig->numArgs == 2 || sig->numArgs == 3);
            GenTree* op3 = nullptr;
            if (sig->numArgs == 3)
            {
                op3 = impPopStack().val;
            }

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;

            if (sig->numArgs == 3)
            {
                return gtNewScalarHWIntrinsicNode(callType, op1, op2, op3, intrinsic);
            }
            else
            {
                return gtNewScalarHWIntrinsicNode(callType, op1, op2, intrinsic);
            }
        }

        default:
        {
            unreached();
            return nullptr;
        }
    }
}

GenTree* Compiler::impFMAIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impLZCNTIntrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    assert(sig->numArgs == 1);
    var_types callType = JITtype2varType(sig->retType);
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, intrinsic);
}

GenTree* Compiler::impPCLMULQDQIntrinsic(NamedIntrinsic        intrinsic,
                                         CORINFO_METHOD_HANDLE method,
                                         CORINFO_SIG_INFO*     sig,
                                         bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impPOPCNTIntrinsic(NamedIntrinsic        intrinsic,
                                      CORINFO_METHOD_HANDLE method,
                                      CORINFO_SIG_INFO*     sig,
                                      bool                  mustExpand)
{
    assert(sig->numArgs == 1);
    var_types callType = JITtype2varType(sig->retType);
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, intrinsic);
}

#endif // FEATURE_HW_INTRINSICS
