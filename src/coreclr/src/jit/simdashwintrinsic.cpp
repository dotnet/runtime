// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "simdashwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const SimdAsHWIntrinsicInfo simdAsHWIntrinsicInfoArray[] = {
// clang-format off
#if defined(TARGET_XARCH)
#define SIMD_AS_HWINTRINSIC(classId, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                      \
    {NI_##classId##_##name, #name, SimdAsHWIntrinsicClassId::classId, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, static_cast<SimdAsHWIntrinsicFlag>(flag)},
#include "simdashwintrinsiclistxarch.h"
#elif defined(TARGET_ARM64)
#define SIMD_AS_HWINTRINSIC(classId, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                      \
    {NI_##classId##_##name, #name, SimdAsHWIntrinsicClassId::classId, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, static_cast<SimdAsHWIntrinsicFlag>(flag)},
#include "simdashwintrinsiclistarm64.h"
#else
#error Unsupported platform
#endif
    // clang-format on
};

//------------------------------------------------------------------------
// lookup: Gets the SimdAsHWIntrinsicInfo associated with a given NamedIntrinsic
//
// Arguments:
//    id -- The NamedIntrinsic associated with the SimdAsHWIntrinsic to lookup
//
// Return Value:
//    The SimdAsHWIntrinsicInfo associated with id
const SimdAsHWIntrinsicInfo& SimdAsHWIntrinsicInfo::lookup(NamedIntrinsic id)
{
    assert(id != NI_Illegal);

    assert(id > NI_SIMD_AS_HWINTRINSIC_START);
    assert(id < NI_SIMD_AS_HWINTRINSIC_END);

    return simdAsHWIntrinsicInfoArray[id - NI_SIMD_AS_HWINTRINSIC_START - 1];
}

//------------------------------------------------------------------------
// lookupId: Gets the NamedIntrinsic for a given method name and InstructionSet
//
// Arguments:
//    className          -- The name of the class associated with the SimdIntrinsic to lookup
//    methodName         -- The name of the method associated with the SimdIntrinsic to lookup
//    enclosingClassName -- The name of the enclosing class
//    sizeOfVectorT      -- The size of Vector<T> in bytes
//
// Return Value:
//    The NamedIntrinsic associated with methodName and classId
NamedIntrinsic SimdAsHWIntrinsicInfo::lookupId(CORINFO_SIG_INFO* sig,
                                               const char*       className,
                                               const char*       methodName,
                                               const char*       enclosingClassName,
                                               int               sizeOfVectorT)
{
    SimdAsHWIntrinsicClassId classId = lookupClassId(className, enclosingClassName, sizeOfVectorT);

    if (classId == SimdAsHWIntrinsicClassId::Unknown)
    {
        return NI_Illegal;
    }

    for (int i = 0; i < (NI_SIMD_AS_HWINTRINSIC_END - NI_SIMD_AS_HWINTRINSIC_START - 1); i++)
    {
        const SimdAsHWIntrinsicInfo& intrinsicInfo = simdAsHWIntrinsicInfoArray[i];

        if (classId != intrinsicInfo.classId)
        {
            continue;
        }

        if (sig->numArgs != static_cast<unsigned>(intrinsicInfo.numArgs))
        {
            continue;
        }

        if (sig->hasThis() != SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsicInfo.id))
        {
            continue;
        }

        if (strcmp(methodName, intrinsicInfo.name) != 0)
        {
            continue;
        }

        return intrinsicInfo.id;
    }

    return NI_Illegal;
}

//------------------------------------------------------------------------
// lookupClassId: Gets the SimdAsHWIntrinsicClassId for a given class name and enclsoing class name
//
// Arguments:
//    className          -- The name of the class associated with the SimdAsHWIntrinsicClassId to lookup
//    enclosingClassName -- The name of the enclosing class
//    sizeOfVectorT      -- The size of Vector<T> in bytes
//
// Return Value:
//    The SimdAsHWIntrinsicClassId associated with className and enclosingClassName
SimdAsHWIntrinsicClassId SimdAsHWIntrinsicInfo::lookupClassId(const char* className,
                                                              const char* enclosingClassName,
                                                              int         sizeOfVectorT)
{
    assert(className != nullptr);

    if ((enclosingClassName != nullptr) || (className[0] != 'V'))
    {
        return SimdAsHWIntrinsicClassId::Unknown;
    }
    if (strcmp(className, "Vector2") == 0)
    {
        return SimdAsHWIntrinsicClassId::Vector2;
    }
    if (strcmp(className, "Vector3") == 0)
    {
        return SimdAsHWIntrinsicClassId::Vector3;
    }
    if (strcmp(className, "Vector4") == 0)
    {
        return SimdAsHWIntrinsicClassId::Vector4;
    }
    if (strcmp(className, "Vector`1") == 0)
    {
#if defined(TARGET_XARCH)
        if (sizeOfVectorT == 32)
        {
            return SimdAsHWIntrinsicClassId::VectorT256;
        }
#endif // TARGET_XARCH

        assert(sizeOfVectorT == 16);
        return SimdAsHWIntrinsicClassId::VectorT128;
    }

    return SimdAsHWIntrinsicClassId::Unknown;
}

GenTree* Compiler::impSimdAsHWIntrinsic(NamedIntrinsic        intrinsic,
                                        CORINFO_CLASS_HANDLE  clsHnd,
                                        CORINFO_METHOD_HANDLE method,
                                        CORINFO_SIG_INFO*     sig,
                                        bool                  mustExpand)
{
    if (!featureSIMD)
    {
        return nullptr;
    }

    var_types retType  = JITtype2varType(sig->retType);
    var_types baseType = TYP_UNKNOWN;
    var_types simdType = TYP_UNKNOWN;
    unsigned  simdSize = 0;

    if (retType == TYP_STRUCT)
    {
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &simdSize);
        simdType = getSIMDTypeForSize(simdSize);
        retType  = simdType;
    }
    else
    {
        assert(!"Unexpected SimdAsHWIntrinsic");
        return nullptr;
    }

    if (!varTypeIsArithmetic(baseType))
    {
        return nullptr;
    }

    NamedIntrinsic hwIntrinsic      = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, baseType);
    bool           isInstanceMethod = SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsic);

    if ((hwIntrinsic == NI_Illegal) || !varTypeIsSIMD(simdType))
    {
        return nullptr;
    }

    CORINFO_InstructionSet hwIntrinsicIsa = HWIntrinsicInfo::lookupIsa(hwIntrinsic);

    if (!compOpportunisticallyDependsOn(hwIntrinsicIsa))
    {
        return nullptr;
    }

    if (SimdAsHWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic
        // is operating on SIMD fields, but where no SIMD local vars are in use.
        compFloatingPointUsed = true;
    }

    if (!SimdAsHWIntrinsicInfo::IsTableDriven(intrinsic))
    {
        return impSimdAsHWIntrinsicSpecial(intrinsic, clsHnd, method, sig, mustExpand);
    }

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    var_types               argType = TYP_UNKNOWN;
    CORINFO_CLASS_HANDLE    argClass;

    GenTree* op1 = nullptr;
    GenTree* op2 = nullptr;

    switch (sig->numArgs)
    {
        case 2:
        {
            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            if (SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic))
            {
                GenTree* tmp = op2;
                op2          = op1;
                op1          = tmp;
            }

            return gtNewSimdHWIntrinsicNode(retType, op1, op2, hwIntrinsic, baseType, simdSize);
        }
    }

    assert(!"Unexpected SimdAsHWIntrinsic");
    return nullptr;
}

GenTree* Compiler::impSimdAsHWIntrinsicSpecial(NamedIntrinsic        intrinsic,
                                               CORINFO_CLASS_HANDLE  clsHnd,
                                               CORINFO_METHOD_HANDLE method,
                                               CORINFO_SIG_INFO*     sig,
                                               bool                  mustExpand)
{
    assert(featureSIMD);
    assert(!SimdAsHWIntrinsicInfo::IsTableDriven(intrinsic));

    var_types retType  = JITtype2varType(sig->retType);
    var_types baseType = TYP_UNKNOWN;
    var_types simdType = TYP_UNKNOWN;
    unsigned  simdSize = 0;

    if (retType == TYP_STRUCT)
    {
        baseType = getBaseTypeAndSizeOfSIMDType(sig->retTypeSigClass, &simdSize);
        simdType = getSIMDTypeForSize(simdSize);
        retType  = simdType;
    }
    else
    {
        assert(!"Unexpected SimdAsHWIntrinsic");
        return nullptr;
    }

    assert(varTypeIsArithmetic(baseType));

    NamedIntrinsic         hwIntrinsic      = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, baseType);
    CORINFO_InstructionSet hwIntrinsicIsa   = HWIntrinsicInfo::lookupIsa(hwIntrinsic);
    bool                   isInstanceMethod = SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsic);

    assert((hwIntrinsic != NI_Illegal) && varTypeIsSIMD(simdType) && compIsaSupportedDebugOnly(hwIntrinsicIsa));

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    var_types               argType = TYP_UNKNOWN;

    GenTree* op1 = nullptr;
    GenTree* op2 = nullptr;

#if defined(TARGET_XARCH)
    CORINFO_CLASS_HANDLE argClass;

    switch (sig->numArgs)
    {
        case 2:
        {
            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            if (SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic))
            {
                GenTree* tmp = op2;
                op2          = op1;
                op1          = tmp;
            }

            switch (intrinsic)
            {
                case NI_VectorT128_LessThan:
                case NI_VectorT128_LessThanOrEqual:
                case NI_VectorT256_LessThan:
                case NI_VectorT256_LessThanOrEqual:
                {
                    if (varTypeIsIntegral(baseType))
                    {
                        GenTree* tmp = op2;
                        op2          = op1;
                        op1          = tmp;
                    }

                    __fallthrough;
                }

                case NI_VectorT128_GreaterThan:
                case NI_VectorT128_GreaterThanOrEqual:
                case NI_VectorT256_GreaterThan:
                case NI_VectorT256_GreaterThanOrEqual:
                {
                    if (varTypeIsUnsigned(baseType))
                    {
                        // Vector<byte>, Vector<ushort>, Vector<uint> and Vector<ulong>:
                        // Hardware supports > for signed comparison. Therefore, to use it for
                        // comparing unsigned numbers, we subtract a constant from both the
                        // operands such that the result fits within the corresponding signed
                        // type. The resulting signed numbers are compared using signed comparison.
                        //
                        // Vector<byte>: constant to be subtracted is 2^7
                        // Vector<ushort> constant to be subtracted is 2^15
                        // Vector<uint> constant to be subtracted is 2^31
                        // Vector<ulong> constant to be subtracted is 2^63
                        //
                        // We need to treat op1 and op2 as signed for comparison purpose after
                        // the transformation.

                        GenTree* constVal = nullptr;

                        switch (baseType)
                        {
                            case TYP_UBYTE:
                            {
                                constVal = gtNewIconNode(0x80808080, TYP_INT);
                                baseType = TYP_BYTE;
                                break;
                            }

                            case TYP_USHORT:
                            {
                                constVal = gtNewIconNode(0x80008000, TYP_INT);
                                baseType = TYP_SHORT;
                                break;
                            }

                            case TYP_UINT:
                            {
                                constVal = gtNewIconNode(0x80000000, TYP_INT);
                                baseType = TYP_INT;
                                break;
                            }

                            case TYP_ULONG:
                            {
                                constVal = gtNewLconNode(0x8000000000000000);
                                baseType = TYP_LONG;
                                break;
                            }

                            default:
                            {
                                unreached();
                            }
                        }

                        GenTree* constVector;
                        GenTree* constVectorDup;

                        constVector =
                            gtNewSIMDNode(retType, constVal, nullptr, SIMDIntrinsicInit, constVal->TypeGet(), simdSize);
                        constVector = impCloneExpr(constVector, &constVectorDup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                                   nullptr DEBUGARG("Clone for Vector<T> unsigned comparison"));

                        NamedIntrinsic subtractIntrinsic = (simdSize == 32) ? NI_AVX2_Subtract : NI_SSE2_Subtract;

                        // op1 = op1 - constVector
                        op1 =
                            gtNewSimdHWIntrinsicNode(retType, op1, constVector, subtractIntrinsic, baseType, simdSize);

                        // op2 = op2 - constVector
                        op2 = gtNewSimdHWIntrinsicNode(retType, op2, constVectorDup, subtractIntrinsic, baseType,
                                                       simdSize);
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, op2, hwIntrinsic, baseType, simdSize);
                }

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and return below.
                    break;
                }
            }
        }
    }
#endif

    assert(!"Unexpected SimdAsHWIntrinsic");
    return nullptr;
}
#endif // FEATURE_HW_INTRINSICS
