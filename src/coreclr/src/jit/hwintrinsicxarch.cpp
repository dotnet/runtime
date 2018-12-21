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
NamedIntrinsic HWIntrinsicInfo::lookupId(const char* className, const char* methodName, const char* enclosingClassName)
{
    // TODO-Throughput: replace sequential search by binary search

    InstructionSet isa = lookupIsa(className, enclosingClassName);
    assert(isa != InstructionSet_ILLEGAL);

    assert(methodName != nullptr);

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
    else if (strcmp(className, "Fma") == 0)
    {
        return InstructionSet_FMA;
    }
    else if (strcmp(className, "Lzcnt") == 0)
    {
        return InstructionSet_LZCNT;
    }

    unreached();
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

    // This intrinsic is supported if
    // - the ISA is available on the underlying hardware (compSupports returns true)
    // - the compiler supports this hardware intrinsics (compSupportsHWIntrinsic returns true)
    bool issupported = compSupports(isa) && compSupportsHWIntrinsic(isa);

    if (category == HW_Category_IsSupportedProperty)
    {
        return gtNewIconNode(issupported);
    }
    // - calling to unsupported intrinsics must throw PlatforNotSupportedException
    else if (!issupported)
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
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

    bool isTableDriven = impIsTableDrivenHWIntrinsic(intrinsic, category);

    if (isTableDriven && ((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic) ||
                          HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic)))
    {
        if (HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic))
        {
            baseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
        }
        else
        {
            assert((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic));
            CORINFO_ARG_LIST_HANDLE secondArg      = info.compCompHnd->getArgNext(sig->args);
            CORINFO_CLASS_HANDLE    secondArgClass = info.compCompHnd->getArgClass(sig, secondArg);
            baseType                               = getBaseTypeOfSIMDType(secondArgClass);

            if (baseType == TYP_UNKNOWN) // the second argument is not a vector
            {
                baseType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, secondArg, &secondArgClass)));
            }
        }
    }

    if (HWIntrinsicInfo::IsOneTypeGeneric(intrinsic) && !HWIntrinsicInfo::HasSpecialImport(intrinsic))
    {
        if (!varTypeIsArithmetic(baseType))
        {
            return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
        }
    }

    if (HWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic is being on SIMD fields, but
        // where no SIMD local vars are in use. This is the same logic as is used for FEATURE_SIMD.
        compFloatingPointUsed = true;
    }

    // table-driven importer of simple intrinsics
    if (isTableDriven)
    {
        unsigned                simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
        CORINFO_ARG_LIST_HANDLE argList  = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        var_types               argType = TYP_UNKNOWN;

        assert(numArgs >= 0);
        assert(HWIntrinsicInfo::lookupIns(intrinsic, baseType) != INS_invalid);
        assert(simdSize == 32 || simdSize == 16);

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
        case NI_SSE_MoveMask:
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_INT);
            assert(getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args)) == TYP_FLOAT);
            op1     = impSIMDPopStack(TYP_SIMD16);
            retNode = gtNewSimdHWIntrinsicNode(TYP_INT, op1, intrinsic, TYP_FLOAT, simdSize);
            break;

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

        case NI_SSE2_MoveMask:
        {
            assert(sig->numArgs == 1);
            retType = JITtype2varType(sig->retType);
            assert(retType == TYP_INT);
            op1      = impSIMDPopStack(TYP_SIMD16);
            baseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
            retNode  = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
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
        case NI_AVX_ExtractVector128:
        case NI_AVX2_ExtractVector128:
        {
            GenTree* lastOp = impPopStack().val;
            assert(lastOp->IsCnsIntOrI() || mustExpand);
            GenTree* vectorOp = impSIMDPopStack(TYP_SIMD32);
            if (sig->numArgs == 2)
            {
                baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
                if (!varTypeIsArithmetic(baseType))
                {
                    retNode = impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED, method, sig, mustExpand);
                }
                else
                {
                    retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, vectorOp, lastOp, intrinsic, baseType, 32);
                }
            }
            else
            {
                assert(sig->numArgs == 3);
                op1                                    = impPopStack().val;
                CORINFO_ARG_LIST_HANDLE secondArg      = info.compCompHnd->getArgNext(sig->args);
                CORINFO_CLASS_HANDLE    secondArgClass = info.compCompHnd->getArgClass(sig, secondArg);
                baseType                               = getBaseTypeOfSIMDType(secondArgClass);
                retNode = gtNewSimdHWIntrinsicNode(TYP_VOID, op1, vectorOp, lastOp, intrinsic, baseType, 32);
            }
            break;
        }

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
