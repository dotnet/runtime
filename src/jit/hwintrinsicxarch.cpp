// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#if FEATURE_HW_INTRINSICS

struct HWIntrinsicInfo
{
    NamedIntrinsic      intrinsicID;
    const char*         intrinsicName;
    InstructionSet      isa;
    int                 ival;
    unsigned            simdSize;
    int                 numArgs;
    instruction         ins[10];
    HWIntrinsicCategory category;
    HWIntrinsicFlag     flags;
};

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
#define HARDWARE_INTRINSIC(id, name, isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    {NI_##id, name, InstructionSet_##isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag},
#include "hwintrinsiclistxarch.h"
};

extern const char* getHWIntrinsicName(NamedIntrinsic intrinsic)
{
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].intrinsicName;
}

//------------------------------------------------------------------------
// lookupHWIntrinsicISA: map class name to InstructionSet value
//
// Arguments:
//    className -- class name in System.Runtime.Intrinsics.X86
//
// Return Value:
//    Id for the ISA class.
//
InstructionSet Compiler::lookupHWIntrinsicISA(const char* className)
{
    if (className != nullptr)
    {
        if (className[0] == 'A')
        {
            if (strcmp(className, "Aes") == 0)
            {
                return InstructionSet_AES;
            }
            else if (strcmp(className, "Avx") == 0)
            {
                return InstructionSet_AVX;
            }
            else if (strcmp(className, "Avx2") == 0)
            {
                return InstructionSet_AVX2;
            }
        }
        if (className[0] == 'S')
        {
            if (strcmp(className, "Sse") == 0)
            {
                return InstructionSet_SSE;
            }
            else if (strcmp(className, "Sse2") == 0)
            {
                return InstructionSet_SSE2;
            }
            else if (strcmp(className, "Sse3") == 0)
            {
                return InstructionSet_SSE3;
            }
            else if (strcmp(className, "Ssse3") == 0)
            {
                return InstructionSet_SSSE3;
            }
            else if (strcmp(className, "Sse41") == 0)
            {
                return InstructionSet_SSE41;
            }
            else if (strcmp(className, "Sse42") == 0)
            {
                return InstructionSet_SSE42;
            }
        }

        if (strcmp(className, "Bmi1") == 0)
        {
            return InstructionSet_BMI1;
        }
        else if (strcmp(className, "Bmi2") == 0)
        {
            return InstructionSet_BMI2;
        }
        else if (strcmp(className, "Fma") == 0)
        {
            return InstructionSet_FMA;
        }
        else if (strcmp(className, "Lzcnt") == 0)
        {
            return InstructionSet_LZCNT;
        }
        else if (strcmp(className, "Pclmulqdq") == 0)
        {
            return InstructionSet_PCLMULQDQ;
        }
        else if (strcmp(className, "Popcnt") == 0)
        {
            return InstructionSet_POPCNT;
        }
    }

    JITDUMP("Unsupported ISA.\n");
    return InstructionSet_ILLEGAL;
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
// TODO-Throughput: replace sequential search by binary search
NamedIntrinsic Compiler::lookupHWIntrinsic(const char* methodName, InstructionSet isa)
{
    NamedIntrinsic result = NI_Illegal;
    if (isa != InstructionSet_ILLEGAL)
    {
        for (int i = 0; i < NI_HW_INTRINSIC_END - NI_HW_INTRINSIC_START; i++)
        {
            if (isa == hwIntrinsicInfoArray[i].isa && strcmp(methodName, hwIntrinsicInfoArray[i].intrinsicName) == 0)
            {
                result = hwIntrinsicInfoArray[i].intrinsicID;
            }
        }
    }
    return result;
}

//------------------------------------------------------------------------
// isaOfHWIntrinsic: map named intrinsic value to its instruction set
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//    instruction set of the intrinsic.
//
InstructionSet Compiler::isaOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].isa;
}

//------------------------------------------------------------------------
// ivalOfHWIntrinsic: get the imm8 value of this intrinsic from the hwIntrinsicInfoArray table
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     The imm8 value that is implicit for this intrinsic, or -1 for intrinsics that do not take an immediate, or for
//     which the immediate is an explicit argument.
//
int Compiler::ivalOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].ival;
}

//------------------------------------------------------------------------
// simdSizeOfHWIntrinsic: get the SIMD size of this intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the SIMD size of this intrinsic
//         - from the hwIntrinsicInfoArray table if intrinsic has NO HW_Flag_UnfixedSIMDSize
//         - TODO-XArch-NYI - from the signature if intrinsic has HW_Flag_UnfixedSIMDSize
//
// Note - this function is only used by the importer
//        after importation (i.e., codegen), we can get the SIMD size from GenTreeHWIntrinsic IR
static unsigned simdSizeOfHWIntrinsic(NamedIntrinsic intrinsic, CORINFO_SIG_INFO* sig)
{
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    assert((hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].flags & HW_Flag_UnfixedSIMDSize) == 0);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].simdSize;
}

//------------------------------------------------------------------------
// numArgsOfHWIntrinsic: get the number of arguments
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     number of arguments
//
int Compiler::numArgsOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].numArgs;
}

//------------------------------------------------------------------------
// insOfHWIntrinsic: get the instruction of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    type      -- vector base type of this intrinsic
//
// Return Value:
//     the instruction of the given intrinsic on the base type
//     return INS_invalid for unsupported base types
//
instruction Compiler::insOfHWIntrinsic(NamedIntrinsic intrinsic, var_types type)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    assert(type >= TYP_BYTE && type <= TYP_DOUBLE);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].ins[type - TYP_BYTE];
}

//------------------------------------------------------------------------
// categoryOfHWIntrinsic: get the category of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the category of the given intrinsic
//
HWIntrinsicCategory Compiler::categoryOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].category;
}

//------------------------------------------------------------------------
// HWIntrinsicFlag: get the flags of the given intrinsic
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//     the flags of the given intrinsic
//
HWIntrinsicFlag Compiler::flagsOfHWIntrinsic(NamedIntrinsic intrinsic)
{
    assert(intrinsic != NI_Illegal);
    assert(intrinsic > NI_HW_INTRINSIC_START && intrinsic < NI_HW_INTRINSIC_END);
    return hwIntrinsicInfoArray[intrinsic - NI_HW_INTRINSIC_START - 1].flags;
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
        assert(argType == TYP_SIMD32 || argType == TYP_SIMD16);
        arg = impSIMDPopStack(argType);
        assert(arg->TypeGet() == TYP_SIMD16 || arg->TypeGet() == TYP_SIMD32);
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
// isFullyImplmentedISAClass: return true if all the hardware intrinsics
//    of this ISA are implemented in RyuJIT.
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true - all the hardware intrinsics of "isa" are implemented in RyuJIT.
//
bool Compiler::isFullyImplmentedISAClass(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_SSE:
        case InstructionSet_SSE2:
        case InstructionSet_SSE3:
        case InstructionSet_SSSE3:
        case InstructionSet_SSE41:
        case InstructionSet_SSE42:
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
        case InstructionSet_AES:
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_FMA:
        case InstructionSet_PCLMULQDQ:
            return false;

        case InstructionSet_LZCNT:
        case InstructionSet_POPCNT:
            return true;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// isScalarISA:
//
// Arguments:
//    isa - Instruction set
// Return Value:
//    true - if "isa" only contains scalar instructions
//
bool Compiler::isScalarISA(InstructionSet isa)
{
    switch (isa)
    {
        case InstructionSet_BMI1:
        case InstructionSet_BMI2:
        case InstructionSet_LZCNT:
        case InstructionSet_POPCNT:
            return true;

        default:
            return false;
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
    return (featureSIMD || isScalarISA(isa)) && (
#ifdef DEBUG
                                                    JitConfig.EnableIncompleteISAClass() ||
#endif
                                                    isFullyImplmentedISAClass(isa));
}

static bool isTypeSupportedForIntrinsic(var_types type)
{
#ifdef _TARGET_X86_
    return !varTypeIsLong(type);
#else
    return true;
#endif
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
// impIsTableDrivenHWIntrinsic:
//
// Arguments:
//    category - category of a HW intrinsic
//
// Return Value:
//    returns true if this category can be table-driven in the importer
//
static bool impIsTableDrivenHWIntrinsic(HWIntrinsicCategory category, HWIntrinsicFlag flags)
{
    // HW_Flag_NoCodeGen implies this intrinsic should be manually morphed in the importer.
    return category != HW_Category_Special && category != HW_Category_Scalar && (flags & HW_Flag_NoCodeGen) == 0;
}

//------------------------------------------------------------------------
// impX86HWIntrinsic: dispatch hardware intrinsics to their own implementation
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    method    -- method handle of the intrinsic function.
//    sig       -- signature of the intrinsic call
//
// Return Value:
//    the expanded intrinsic.
//
GenTree* Compiler::impX86HWIntrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    InstructionSet      isa      = isaOfHWIntrinsic(intrinsic);
    HWIntrinsicCategory category = categoryOfHWIntrinsic(intrinsic);
    HWIntrinsicFlag     flags    = flagsOfHWIntrinsic(intrinsic);
    int                 numArgs  = sig->numArgs;
    var_types           retType  = JITtype2varType(sig->retType);
    var_types           baseType = TYP_UNKNOWN;
    if (retType == TYP_STRUCT && featureSIMD)
    {
        unsigned int sizeBytes;
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &sizeBytes);
        retType  = getSIMDTypeForSize(sizeBytes);
        assert(sizeBytes != 0 && baseType != TYP_UNKNOWN);
    }

    // This intrinsic is supported if
    // - the ISA is available on the underlying hardware (compSupports returns true)
    // - the compiler supports this hardware intrinsics (compSupportsHWIntrinsic returns true)
    // - intrinsics do not require 64-bit registers (r64) on 32-bit platforms (isTypeSupportedForIntrinsic returns
    // true)
    bool issupported = compSupports(isa) && compSupportsHWIntrinsic(isa) && isTypeSupportedForIntrinsic(retType);

    if (category == HW_Category_IsSupportedProperty)
    {
        return gtNewIconNode(issupported);
    }
    // - calling to unsupported intrinsics must throw PlatforNotSupportedException
    else if (!issupported)
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }
    else if (category == HW_Category_IMM)
    {
        GenTree* lastOp = impStackTop().val;
        if (!lastOp->IsCnsIntOrI() && !mustExpand)
        {
            // When the imm-argument is not a constant and we are not being forced to expand, we need to
            // return nullptr so a GT_CALL to the intrinsic method is emitted instead. The
            // intrinsic method is recursive and will be forced to expand, at which point
            // we emit some less efficient fallback code.
            return nullptr;
        }
    }

    if ((flags & HW_Flag_Generic) != 0)
    {
        assert(baseType != TYP_UNKNOWN);
        // When the type argument is not a numeric type (and we are not being forced to expand), we need to
        // return nullptr so a GT_CALL to the intrinsic method is emitted that will throw NotSupportedException
        if (!varTypeIsArithmetic(baseType))
        {
            assert(!mustExpand);
            return nullptr;
        }

        if ((flags & HW_Flag_TwoTypeGeneric) != 0)
        {
            // StaticCast<T, U> has two type parameters.
            assert(!mustExpand);
            assert(numArgs == 1);
            var_types srcType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
            assert(srcType != TYP_UNKNOWN);
            if (!varTypeIsArithmetic(srcType))
            {
                return nullptr;
            }
        }
    }

    // table-driven importer of simple intrinsics
    if (impIsTableDrivenHWIntrinsic(category, flags))
    {
        if (!varTypeIsSIMD(retType))
        {
            baseType = getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args));
            assert(baseType != TYP_UNKNOWN);
        }

        unsigned                simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);
        CORINFO_ARG_LIST_HANDLE argList  = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        var_types               argType = TYP_UNKNOWN;

        assert(numArgs >= 0);
        assert(insOfHWIntrinsic(intrinsic, baseType) != INS_invalid);
        assert(simdSize == 32 || simdSize == 16);

        GenTree* retNode = nullptr;
        GenTree* op1     = nullptr;
        GenTree* op2     = nullptr;

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

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2     = getArgForHWIntrinsic(argType, argClass);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, baseType, simdSize);
                break;
            }
            default:
                unreached();
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
        case InstructionSet_SSE3:
            return impSSE3Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSSE3:
            return impSSSE3Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE41:
            return impSSE41Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_SSE42:
            return impSSE42Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_AVX:
            return impAVXIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_AVX2:
            return impAVX2Intrinsic(intrinsic, method, sig, mustExpand);

        case InstructionSet_AES:
            return impAESIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_BMI1:
            return impBMI1Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_BMI2:
            return impBMI2Intrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_FMA:
            return impFMAIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_LZCNT:
            return impLZCNTIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_PCLMULQDQ:
            return impPCLMULQDQIntrinsic(intrinsic, method, sig, mustExpand);
        case InstructionSet_POPCNT:
            return impPOPCNTIntrinsic(intrinsic, method, sig, mustExpand);
        default:
            return nullptr;
    }
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
    else if (simdType == TYP_SIMD32)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return Vector256FloatHandle;
            case TYP_DOUBLE:
                return Vector256DoubleHandle;
            case TYP_INT:
                return Vector256IntHandle;
            case TYP_USHORT:
                return Vector256UShortHandle;
            case TYP_UBYTE:
                return Vector256UByteHandle;
            case TYP_SHORT:
                return Vector256ShortHandle;
            case TYP_BYTE:
                return Vector256ByteHandle;
            case TYP_LONG:
                return Vector256LongHandle;
            case TYP_UINT:
                return Vector256UIntHandle;
            case TYP_ULONG:
                return Vector256ULongHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }

    return NO_CLASS_HANDLE;
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
    int      simdSize = simdSizeOfHWIntrinsic(intrinsic, sig);
    assert(simdSize == 16);

    switch (intrinsic)
    {
        case NI_SSE_SetVector128:
        {
            assert(sig->numArgs == 4);
            assert(getBaseTypeOfSIMDType(sig->retTypeSigClass) == TYP_FLOAT);

            op4 = impPopStack().val;
            op3 = impPopStack().val;
            op2 = impPopStack().val;
            op1 = impPopStack().val;

            GenTree* left    = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op4, op3, NI_SSE_UnpackLow, TYP_FLOAT, simdSize);
            GenTree* right   = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, op1, NI_SSE_UnpackLow, TYP_FLOAT, simdSize);
            GenTree* control = gtNewIconNode(68, TYP_UBYTE);

            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, left, right, control, NI_SSE_Shuffle, TYP_FLOAT, simdSize);
            break;
        }

        case NI_SSE_ConvertToVector128SingleScalar:
        {
            assert(sig->numArgs == 2);
            assert(getBaseTypeOfSIMDType(sig->retTypeSigClass) == TYP_FLOAT);

#ifdef _TARGET_X86_
            CORINFO_CLASS_HANDLE argClass;

            CORINFO_ARG_LIST_HANDLE argLst = info.compCompHnd->getArgNext(sig->args);
            CorInfoType             corType =
                strip(info.compCompHnd->getArgType(sig, argLst, &argClass)); // type of the second argument

            if (varTypeIsLong(JITtype2varType(corType)))
            {
                return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
            }
#endif // _TARGET_X86_

            op2     = impPopStack().val;
            op1     = impSIMDPopStack(TYP_SIMD16);
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, intrinsic, TYP_FLOAT, simdSize);
            break;
        }

        case NI_SSE_MoveMask:
            assert(sig->numArgs == 1);
            assert(JITtype2varType(sig->retType) == TYP_INT);
            assert(getBaseTypeOfSIMDType(info.compCompHnd->getArgClass(sig, sig->args)) == TYP_FLOAT);
            op1     = impSIMDPopStack(TYP_SIMD16);
            retNode = gtNewSimdHWIntrinsicNode(TYP_INT, op1, intrinsic, TYP_FLOAT, simdSize);
            break;

        case NI_SSE_SetAllVector128:
            assert(sig->numArgs == 1);
            assert(getBaseTypeOfSIMDType(sig->retTypeSigClass) == TYP_FLOAT);
            op1     = impPopStack().val;
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, gtCloneExpr(op1), gtNewIconNode(0), NI_SSE_Shuffle,
                                               TYP_FLOAT, simdSize);
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
    var_types baseType = TYP_UNKNOWN;
    switch (intrinsic)
    {
        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impSSE3Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impSSSE3Intrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impSSE41Intrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    return nullptr;
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
            assert(sig->numArgs == 2);
            op2     = impPopStack().val;
            op1     = impPopStack().val;
            argList = info.compCompHnd->getArgNext(argList);                        // the second argument
            corType = strip(info.compCompHnd->getArgType(sig, argList, &argClass)); // type of the second argument

            retNode = gtNewScalarHWIntrinsicNode(callType, op1, op2, NI_SSE42_Crc32);

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

GenTree* Compiler::impAVXIntrinsic(NamedIntrinsic        intrinsic,
                                   CORINFO_METHOD_HANDLE method,
                                   CORINFO_SIG_INFO*     sig,
                                   bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types baseType = TYP_UNKNOWN;
    switch (intrinsic)
    {
        default:
            JITDUMP("Not implemented hardware intrinsic");
            break;
    }
    return retNode;
}

GenTree* Compiler::impAVX2Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    GenTree*  retNode  = nullptr;
    GenTree*  op1      = nullptr;
    GenTree*  op2      = nullptr;
    var_types baseType = TYP_UNKNOWN;
    switch (intrinsic)
    {
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

GenTree* Compiler::impBMI1Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    return nullptr;
}

GenTree* Compiler::impBMI2Intrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
                                    bool                  mustExpand)
{
    return nullptr;
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
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, NI_LZCNT_LeadingZeroCount);
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
    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, NI_POPCNT_PopCount);
}

#endif // FEATURE_HW_INTRINSICS
