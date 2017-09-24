// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#ifdef _TARGET_XARCH_

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
GenTree* Compiler::impX86HWIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    InstructionSet isa = isaOfHWIntrinsic(intrinsic);
    switch (isa)
    {
        case InstructionSet_SSE:
            return impSSEIntrinsic(intrinsic, method, sig);
        case InstructionSet_SSE2:
            return impSSE2Intrinsic(intrinsic, method, sig);
        case InstructionSet_SSE3:
            return impSSE3Intrinsic(intrinsic, method, sig);
        case InstructionSet_SSSE3:
            return impSSSE3Intrinsic(intrinsic, method, sig);
        case InstructionSet_SSE41:
            return impSSE41Intrinsic(intrinsic, method, sig);
        case InstructionSet_SSE42:
            return impSSE42Intrinsic(intrinsic, method, sig);
        case InstructionSet_AVX:
            return impAVXIntrinsic(intrinsic, method, sig);
        case InstructionSet_AVX2:
            return impAVX2Intrinsic(intrinsic, method, sig);

        case InstructionSet_AES:
            return impAESIntrinsic(intrinsic, method, sig);
        case InstructionSet_BMI1:
            return impBMI1Intrinsic(intrinsic, method, sig);
        case InstructionSet_BMI2:
            return impBMI2Intrinsic(intrinsic, method, sig);
        case InstructionSet_FMA:
            return impFMAIntrinsic(intrinsic, method, sig);
        case InstructionSet_LZCNT:
            return impLZCNTIntrinsic(intrinsic, method, sig);
        case InstructionSet_PCLMULQDQ:
            return impPCLMULQDQIntrinsic(intrinsic, method, sig);
        case InstructionSet_POPCNT:
            return impPOPCNTIntrinsic(intrinsic, method, sig);
        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSEIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSE_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSE));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSE2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSE2_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSE2));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSE3Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSE3_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSE3));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSSE3Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSSE3_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSSE3));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSE41Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSE41_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSE41));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impSSE42Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_SSE42_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_SSE42));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impAVXIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_AVX_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_AVX));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impAVX2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_AVX2_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_AVX2));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impAESIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_AES_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_AES));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impBMI1Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_BMI1_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_BMI1));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impBMI2Intrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_BMI2_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_BMI2));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impFMAIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_FMA_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_FMA));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impLZCNTIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_LZCNT_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_LZCNT));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impPCLMULQDQIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_PCLMULQDQ_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_PCLMULQDQ));

        default:
            return nullptr;
    }
}

GenTree* Compiler::impPOPCNTIntrinsic(NamedIntrinsic intrinsic, CORINFO_METHOD_HANDLE method, CORINFO_SIG_INFO* sig)
{
    switch (intrinsic)
    {
        case NI_POPCNT_IsSupported:
            return gtNewIconNode(compSupports(InstructionSet_POPCNT));

        default:
            return nullptr;
    }
}

#endif // _TARGET_XARCH_