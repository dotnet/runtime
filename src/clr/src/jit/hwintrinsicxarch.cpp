// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#if FEATURE_HW_INTRINSICS

struct HWIntrinsicInfo
{
    NamedIntrinsic intrinsicID;
    const char*    intrinsicName;
    InstructionSet isa;
}

static const hwIntrinsicInfoArray[] = {
#define HARDWARE_INTRINSIC(id, name, isa) {NI_##id, name, InstructionSet_##isa},
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
// isIntrinsicAnIsSupportedPropertyGetter: return true if the intrinsic is "get_IsSupported"
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//
// Return Value:
//    true if the intrinsic is "get_IsSupported"
//    Sometimes we need to specially treat "get_IsSupported"
bool Compiler::isIntrinsicAnIsSupportedPropertyGetter(NamedIntrinsic intrinsic)
{
    switch (intrinsic)
    {
        case NI_SSE_IsSupported:
        case NI_SSE2_IsSupported:
        case NI_SSE3_IsSupported:
        case NI_SSSE3_IsSupported:
        case NI_SSE41_IsSupported:
        case NI_SSE42_IsSupported:
        case NI_AVX_IsSupported:
        case NI_AVX2_IsSupported:
        case NI_AES_IsSupported:
        case NI_BMI1_IsSupported:
        case NI_BMI2_IsSupported:
        case NI_FMA_IsSupported:
        case NI_LZCNT_IsSupported:
        case NI_PCLMULQDQ_IsSupported:
        case NI_POPCNT_IsSupported:
            return true;
        default:
            return false;
    }
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
// impX86HWIntrinsic: dispatch hardware intrinsics to their own implementation
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
GenTree* Compiler::impX86HWIntrinsic(NamedIntrinsic        intrinsic,
                                     CORINFO_METHOD_HANDLE method,
                                     CORINFO_SIG_INFO*     sig,
                                     bool                  mustExpand)
{
    InstructionSet isa = isaOfHWIntrinsic(intrinsic);

    // This intrinsic is supported if
    // - the ISA is available on the underlying hardware (compSupports returns true)
    // - the compiler supports this hardware intrinsics (compSupportsHWIntrinsic returns true)
    bool issupported = compSupports(isa) && compSupportsHWIntrinsic(isa);

    if (isIntrinsicAnIsSupportedPropertyGetter(intrinsic))
    {
        return gtNewIconNode(issupported);
    }
    else if (!issupported)
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }

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
    GenTree* retNode = nullptr;
    GenTree* op1     = nullptr;
    GenTree* op2     = nullptr;
    switch (intrinsic)
    {
        case NI_SSE_Add:
        case NI_SSE_And:
        case NI_SSE_AndNot:
        case NI_SSE_CompareEqual:
        case NI_SSE_CompareGreaterThan:
        case NI_SSE_CompareGreaterThanOrEqual:
        case NI_SSE_CompareLessThan:
        case NI_SSE_CompareLessThanOrEqual:
        case NI_SSE_CompareNotEqual:
        case NI_SSE_CompareNotGreaterThan:
        case NI_SSE_CompareNotGreaterThanOrEqual:
        case NI_SSE_CompareNotLessThan:
        case NI_SSE_CompareNotLessThanOrEqual:
        case NI_SSE_CompareOrdered:
        case NI_SSE_CompareUnordered:
        case NI_SSE_Divide:
        case NI_SSE_Max:
        case NI_SSE_Min:
        case NI_SSE_MoveHighToLow:
        case NI_SSE_MoveLowToHigh:
        case NI_SSE_Multiply:
        case NI_SSE_Or:
        case NI_SSE_Subtract:
        case NI_SSE_UnpackHigh:
        case NI_SSE_UnpackLow:
        case NI_SSE_Xor:
            assert(sig->numArgs == 2);
            assert(getBaseTypeOfSIMDType(sig->retTypeSigClass) == TYP_FLOAT);
            op2     = impSIMDPopStack(TYP_SIMD16);
            op1     = impSIMDPopStack(TYP_SIMD16);
            retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, intrinsic, TYP_FLOAT, 16);
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
        case NI_SSE2_Add:
            assert(sig->numArgs == 2);
            op2      = impSIMDPopStack(TYP_SIMD16);
            op1      = impSIMDPopStack(TYP_SIMD16);
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, NI_SSE2_Add, baseType, 16);
            break;

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

    CORINFO_ARG_LIST_HANDLE argLst = sig->args;
    CORINFO_CLASS_HANDLE    argClass;
    CorInfoType             corType;
    switch (intrinsic)
    {
        case NI_SSE42_Crc32:
            assert(sig->numArgs == 2);

#ifdef _TARGET_X86_
            if (varTypeIsLong(callType))
            {
                return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
            }
#endif

            op2 = impPopStack().val;
            op1 = impPopStack().val;

            argLst  = info.compCompHnd->getArgNext(argLst);                        // the second argument
            corType = strip(info.compCompHnd->getArgType(sig, argLst, &argClass)); // type of the second argument

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
        case NI_AVX_Add:
            assert(sig->numArgs == 2);
            op2      = impSIMDPopStack(TYP_SIMD32);
            op1      = impSIMDPopStack(TYP_SIMD32);
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, op2, NI_AVX_Add, baseType, 32);
            break;

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
        case NI_AVX2_Add:
            assert(sig->numArgs == 2);
            op2      = impSIMDPopStack(TYP_SIMD32);
            op1      = impSIMDPopStack(TYP_SIMD32);
            baseType = getBaseTypeOfSIMDType(sig->retTypeSigClass);
            retNode  = gtNewSimdHWIntrinsicNode(TYP_SIMD32, op1, op2, NI_AVX2_Add, baseType, 32);
            break;

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

#ifdef _TARGET_X86_
    if (varTypeIsLong(callType))
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }
#endif

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

#ifdef _TARGET_X86_
    if (varTypeIsLong(callType))
    {
        return impUnsupportedHWIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }
#endif

    return gtNewScalarHWIntrinsicNode(callType, impPopStack().val, NI_POPCNT_PopCount);
}

#endif // FEATURE_HW_INTRINSICS
