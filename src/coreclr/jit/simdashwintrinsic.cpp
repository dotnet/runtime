// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "simdashwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const SimdAsHWIntrinsicInfo simdAsHWIntrinsicInfoArray[] = {
// clang-format off
#if defined(TARGET_XARCH)
#define SIMD_AS_HWINTRINSIC(classId, id, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                      \
    {NI_##classId##_##id, name, SimdAsHWIntrinsicClassId::classId, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, static_cast<SimdAsHWIntrinsicFlag>(flag)},
#include "simdashwintrinsiclistxarch.h"
#elif defined(TARGET_ARM64)
#define SIMD_AS_HWINTRINSIC(classId, id, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                      \
    {NI_##classId##_##id, name, SimdAsHWIntrinsicClassId::classId, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, static_cast<SimdAsHWIntrinsicFlag>(flag)},
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
//    comp               -- The compiler
//    sig                -- The signature of the intrinsic
//    className          -- The name of the class associated with the SimdIntrinsic to lookup
//    methodName         -- The name of the method associated with the SimdIntrinsic to lookup
//    enclosingClassName -- The name of the enclosing class
//
// Return Value:
//    The NamedIntrinsic associated with methodName and classId
NamedIntrinsic SimdAsHWIntrinsicInfo::lookupId(Compiler*         comp,
                                               CORINFO_SIG_INFO* sig,
                                               const char*       className,
                                               const char*       methodName,
                                               const char*       enclosingClassName)
{
    SimdAsHWIntrinsicClassId classId = lookupClassId(comp, className, enclosingClassName);

    if (classId == SimdAsHWIntrinsicClassId::Unknown)
    {
        return NI_Illegal;
    }

    unsigned numArgs = sig->numArgs;

    if (sig->hasThis())
    {
        return NI_Illegal;
    }

    if (classId == SimdAsHWIntrinsicClassId::Vector)
    {
        // We want to avoid doing anything that would unnecessarily trigger a recorded dependency against Vector<T>
        // so we duplicate a few checks here to ensure this works smoothly for the static Vector class.

        if (strcmp(methodName, "get_IsHardwareAccelerated") == 0)
        {
            return comp->IsBaselineSimdIsaSupported() ? NI_IsSupported_True : NI_IsSupported_False;
        }

        var_types            retType         = JITtype2varType(sig->retType);
        CorInfoType          simdBaseJitType = CORINFO_TYPE_UNDEF;
        CORINFO_CLASS_HANDLE argClass        = NO_CLASS_HANDLE;

        if (retType == TYP_STRUCT)
        {
            argClass = sig->retTypeSigClass;
        }
        else
        {
            assert(numArgs != 0);
            argClass = comp->info.compCompHnd->getArgClass(sig, sig->args);
        }

        const char* argNamespaceName;
        const char* argClassName = comp->getClassNameFromMetadata(argClass, &argNamespaceName);

        classId = lookupClassId(comp, argClassName, nullptr);

        if (classId == SimdAsHWIntrinsicClassId::Unknown)
        {
            return NI_Illegal;
        }
        assert(classId != SimdAsHWIntrinsicClassId::Vector);
    }

    assert(strcmp(methodName, "get_IsHardwareAccelerated") != 0);

    for (int i = 0; i < (NI_SIMD_AS_HWINTRINSIC_END - NI_SIMD_AS_HWINTRINSIC_START - 1); i++)
    {
        const SimdAsHWIntrinsicInfo& intrinsicInfo = simdAsHWIntrinsicInfoArray[i];

        if (classId != intrinsicInfo.classId)
        {
            continue;
        }

        if (numArgs != static_cast<unsigned>(intrinsicInfo.numArgs))
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
//    comp               -- The compiler
//    className          -- The name of the class associated with the SimdAsHWIntrinsicClassId to lookup
//    enclosingClassName -- The name of the enclosing class
//
// Return Value:
//    The SimdAsHWIntrinsicClassId associated with className and enclosingClassName
SimdAsHWIntrinsicClassId SimdAsHWIntrinsicInfo::lookupClassId(Compiler*   comp,
                                                              const char* className,
                                                              const char* enclosingClassName)
{
    if ((className == nullptr) || (enclosingClassName != nullptr))
    {
        return SimdAsHWIntrinsicClassId::Unknown;
    }

    switch (className[0])
    {
        case 'V':
        {
            if (strncmp(className, "Vector", 6) != 0)
            {
                break;
            }

            className += 6;

            if (className[0] == '\0')
            {
                return SimdAsHWIntrinsicClassId::Vector;
            }
            else if (strcmp(className, "`1") == 0)
            {
                uint32_t vectorTByteLength = comp->getVectorTByteLength();

#if defined(TARGET_XARCH)
                if ((vectorTByteLength == 16) || (vectorTByteLength == 32) || (vectorTByteLength == 64))
#else
                if (vectorTByteLength == 16)
#endif
                {
                    return SimdAsHWIntrinsicClassId::VectorT;
                }

                // We return unknown for any unsupported size
                return SimdAsHWIntrinsicClassId::Unknown;
            }
            break;
        }

        default:
        {
            break;
        }
    }

    return SimdAsHWIntrinsicClassId::Unknown;
}

//------------------------------------------------------------------------
// impSimdAsIntrinsic: Import a SIMD intrinsic as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic  -- id of the intrinsic function.
//    clsHnd     -- class handle containing the intrinsic function.
//    method     -- method handle of the intrinsic function.
//    sig        -- signature of the intrinsic call
//    mustExpand -- true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impSimdAsHWIntrinsic(NamedIntrinsic        intrinsic,
                                        CORINFO_CLASS_HANDLE  clsHnd,
                                        CORINFO_METHOD_HANDLE method,
                                        CORINFO_SIG_INFO*     sig,
                                        bool                  mustExpand)
{
    if (!IsBaselineSimdIsaSupported())
    {
        // The user disabled support for the baseline ISA so
        // don't emit any SIMD intrinsics as they all require
        // this at a minimum
        return nullptr;
    }

    // NextCallRetAddr requires a CALL, so return nullptr.
    if (info.compHasNextCallRetAddr)
    {
        return nullptr;
    }

    CORINFO_CLASS_HANDLE argClass        = NO_CLASS_HANDLE;
    var_types            retType         = genActualType(JITtype2varType(sig->retType));
    CorInfoType          simdBaseJitType = CORINFO_TYPE_UNDEF;
    var_types            simdType        = TYP_UNKNOWN;
    unsigned             simdSize        = 0;
    unsigned             numArgs         = sig->numArgs;

    // We want to resolve and populate the handle cache for this type even
    // if it isn't the basis for anything carried on the node.
    simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);

    if ((clsHnd != m_simdHandleCache->VectorHandle) &&
        ((simdBaseJitType == CORINFO_TYPE_UNDEF) || !varTypeIsArithmetic(JitType2PreciseVarType(simdBaseJitType))))
    {
        // We want to exit early if the clsHnd should have a base type and it isn't one
        // of the supported types. This handles cases like op_Explicit which take a Vector<T>
        return nullptr;
    }

    if (retType == TYP_STRUCT)
    {
        simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &simdSize);
        if ((simdBaseJitType == CORINFO_TYPE_UNDEF) || !varTypeIsArithmetic(JitType2PreciseVarType(simdBaseJitType)) ||
            (simdSize == 0))
        {
            // Unsupported type
            return nullptr;
        }
        retType = getSIMDTypeForSize(simdSize);
    }
    else if (numArgs != 0)
    {
        if (sig->hasThis() && (retType == TYP_VOID))
        {
            simdBaseJitType = strip(info.compCompHnd->getArgType(sig, sig->args, &argClass));
        }
        else
        {
            argClass        = info.compCompHnd->getArgClass(sig, sig->args);
            simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(argClass, &simdSize);
        }
    }

    assert(!sig->hasThis());

    if ((clsHnd == m_simdHandleCache->VectorHandle) && (numArgs != 0) &&
        !SimdAsHWIntrinsicInfo::KeepBaseTypeFromRet(intrinsic))
    {
        // We need to fixup the clsHnd in the case we are an intrinsic on Vector
        // The first argument will be the appropriate Vector<T> handle to use
        clsHnd = info.compCompHnd->getArgClass(sig, sig->args);

        // We also need to adjust the simdBaseJitType as some methods on Vector return
        // a type different than the operation we need to perform. An example
        // is LessThan or Equals which takes double but returns long. This is
        // unlike the counterparts on Vector<T> which take a return the same type.
        simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);
    }

    if ((simdBaseJitType == CORINFO_TYPE_UNDEF) || !varTypeIsArithmetic(JitType2PreciseVarType(simdBaseJitType)) ||
        (simdSize == 0))
    {
        // We get here for a devirtualization of IEquatable`1.Equals
        // or if the user tries to use Vector<T> with an unsupported type
        return nullptr;
    }

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    simdType               = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    NamedIntrinsic hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);

    if ((hwIntrinsic == NI_Illegal) || !varTypeIsSIMD(simdType))
    {
        // The simdBaseJitType isn't supported by the intrinsic
        return nullptr;
    }

    // Set `compFloatingPointUsed` to cover the scenario where an intrinsic
    // is operating on SIMD fields, but where no SIMD local vars are in use.
    compFloatingPointUsed = true;

    if (hwIntrinsic == intrinsic)
    {
        // The SIMD intrinsic requires special handling outside the normal code path
        return impSimdAsHWIntrinsicSpecial(intrinsic, clsHnd, sig, retType, simdBaseJitType, simdSize, mustExpand);
    }

    CORINFO_InstructionSet hwIntrinsicIsa = HWIntrinsicInfo::lookupIsa(hwIntrinsic);

    if (!compOpportunisticallyDependsOn(hwIntrinsicIsa))
    {
        // The JIT doesn't support the required ISA
        return nullptr;
    }

    CORINFO_ARG_LIST_HANDLE argList = sig->args;
    var_types               argType = TYP_UNKNOWN;

    GenTree* op1 = nullptr;
    GenTree* op2 = nullptr;

    switch (numArgs)
    {
        case 0:
        {
            return gtNewSimdAsHWIntrinsicNode(retType, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case 1:
        {
            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            return gtNewSimdAsHWIntrinsicNode(retType, op1, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case 2:
        {
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
        }
    }

    assert(!"Unexpected SimdAsHWIntrinsic");
    return nullptr;
}

//------------------------------------------------------------------------
// impSimdAsHWIntrinsicSpecial: Import a SIMD intrinsic as a GT_HWINTRINSIC node if possible
//                              This method handles cases which cannot be table driven
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    sig             -- signature of the intrinsic call
//    retType         -- the return type of the intrinsic call
//    simdBaseJitType -- the base JIT type of SIMD type of the intrinsic
//    simdSize        -- the size of the SIMD type of the intrinsic
//    mustExpand      -- true if the intrinsic must return a GenTree*; otherwise, false
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impSimdAsHWIntrinsicSpecial(NamedIntrinsic       intrinsic,
                                               CORINFO_CLASS_HANDLE clsHnd,
                                               CORINFO_SIG_INFO*    sig,
                                               var_types            retType,
                                               CorInfoType          simdBaseJitType,
                                               unsigned             simdSize,
                                               bool                 mustExpand)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

    assert(retType != TYP_UNKNOWN);
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);
    assert(SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType) == intrinsic);

    var_types simdType = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    CORINFO_ARG_LIST_HANDLE argList  = sig->args;
    var_types               argType  = TYP_UNKNOWN;
    CORINFO_CLASS_HANDLE    argClass = NO_CLASS_HANDLE;

    GenTree* op1 = nullptr;
    GenTree* op2 = nullptr;
    GenTree* op3 = nullptr;
    GenTree* op4 = nullptr;
    GenTree* op5 = nullptr;

    unsigned numArgs = sig->numArgs;
    assert(!sig->hasThis());

#if defined(TARGET_XARCH)
    // We should have already exited early if SSE2 isn't supported
    assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));

    if (SimdAsHWIntrinsicInfo::lookupClassId(intrinsic) == SimdAsHWIntrinsicClassId::VectorT)
    {
        if (simdSize == 32)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
        }
        else if (simdSize == 64)
        {
            assert(IsBaselineVector512IsaSupportedDebugOnly());
        }
    }
#elif defined(TARGET_ARM64)
    // We should have already exited early if AdvSimd isn't supported
    assert(compIsaSupportedDebugOnly(InstructionSet_AdvSimd));
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    bool isOpExplicit = false;

    switch (intrinsic)
    {
        case NI_VectorT_ConvertToInt32Native:
        {
            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                return nullptr;
            }
            break;
        }

        case NI_VectorT_ConvertToInt64Native:
        case NI_VectorT_ConvertToUInt32Native:
        case NI_VectorT_ConvertToUInt64Native:
        {
            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                return nullptr;
            }

#if defined(TARGET_XARCH)
            if (!IsBaselineVector512IsaSupportedOpportunistically())
            {
                return nullptr;
            }
#endif // TARGET_XARCH

            break;
        }

        case NI_VectorT_MultiplyAddEstimate:
        {
            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                return nullptr;
            }
            break;
        }

#if defined(TARGET_XARCH)
        case NI_VectorT_ConvertToDouble:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically() ||
                ((simdSize != 64) && compOpportunisticallyDependsOn(InstructionSet_AVX10v1)))
            {
                break;
            }
            return nullptr;
        }

        case NI_VectorT_ConvertToInt32:
        {
            if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                break;
            }
            return nullptr;
        }

        case NI_VectorT_ConvertToInt64:
        case NI_VectorT_ConvertToUInt32:
        case NI_VectorT_ConvertToUInt64:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically() ||
                (simdSize != 64 && compOpportunisticallyDependsOn(InstructionSet_AVX10v1)))
            {
                break;
            }
            return nullptr;
        }

        case NI_VectorT_ConvertToSingle:
        {
            if ((simdBaseType == TYP_INT) ||
                (simdBaseType == TYP_UINT &&
                 (IsBaselineVector512IsaSupportedOpportunistically() ||
                  (simdSize != 64 && compOpportunisticallyDependsOn(InstructionSet_AVX10v1)))))
            {
                break;
            }
            return nullptr;
        }
#endif // TARGET_XARCH

#if defined(TARGET_X86)
        case NI_VectorT_Create:
        {
            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->IsIntegralConst())
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                return nullptr;
            }
            break;
        }
#endif // TARGET_X86

        case NI_VectorT_CreateSequence:
        {
            if (varTypeIsLong(simdBaseType) && !impStackTop(0).val->OperIsConst())
            {
#if defined(TARGET_XARCH)
                if (!canUseEvexEncoding())
                {
                    // TODO-XARCH-CQ: We should support long/ulong multiplication
                    return nullptr;
                }
#endif // TARGET_XARCH

#if defined(TARGET_X86) || defined(TARGET_ARM64)
                // TODO-XARCH-CQ: We need to support 64-bit CreateBroadcast
                // TODO-ARM64-CQ: We should support long/ulong multiplication.
                return nullptr;
#endif // TARGET_X86 || TARGET_ARM64
            }
            break;
        }

        case NI_VectorT_As:
        case NI_VectorT_AsVectorByte:
        case NI_VectorT_AsVectorDouble:
        case NI_VectorT_AsVectorInt16:
        case NI_VectorT_AsVectorInt32:
        case NI_VectorT_AsVectorInt64:
        case NI_VectorT_AsVectorNInt:
        case NI_VectorT_AsVectorNUInt:
        case NI_VectorT_AsVectorSByte:
        case NI_VectorT_AsVectorSingle:
        case NI_VectorT_AsVectorUInt16:
        case NI_VectorT_AsVectorUInt32:
        case NI_VectorT_AsVectorUInt64:
        {
            unsigned    retSimdSize;
            CorInfoType retBaseJitType = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &retSimdSize);

            if ((retBaseJitType == CORINFO_TYPE_UNDEF) ||
                !varTypeIsArithmetic(JitType2PreciseVarType(retBaseJitType)) || (retSimdSize == 0))
            {
                // We get here if the return type is an unsupported type
                return nullptr;
            }

            isOpExplicit = true;
            break;
        }

#if defined(TARGET_XARCH)
        case NI_VectorT_GetElement:
        {
            op2 = impStackTop(0).val;

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                case TYP_LONG:
                case TYP_ULONG:
                {
                    bool useToScalar = op2->IsIntegralConst(0);

#if defined(TARGET_X86)
                    useToScalar &= !varTypeIsLong(simdBaseType);
#endif // TARGET_X86

                    if (!useToScalar && !compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        // Using software fallback if simdBaseType is not supported by hardware
                        return nullptr;
                    }
                    break;
                }

                case TYP_DOUBLE:
                case TYP_FLOAT:
                case TYP_SHORT:
                case TYP_USHORT:
                {
                    // short/ushort/float/double is supported by SSE2
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_XARCH)
        case NI_VectorT_Dot:
        {
            if ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    // TODO-XARCH-CQ: We can support 32-bit integers if we updating multiplication
                    // to be lowered rather than imported as the relevant operations.
                    return nullptr;
                }
            }
            else
            {
                assert(varTypeIsShort(simdBaseType) || varTypeIsFloating(simdBaseType));
            }
            break;
        }

        case NI_VectorT_WithElement:
        {
            assert(sig->numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->IsIntegralConst())
            {
                // TODO-XARCH-CQ: We should always import these like we do with GetElement
                // Index is not a constant, use the software fallback
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if ((imm8 >= count) || (imm8 < 0))
            {
                // Using software fallback if index is out of range (throw exception)
                return nullptr;
            }

            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                {
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                {
                    if (!compOpportunisticallyDependsOn(InstructionSet_SSE41_X64))
                    {
                        return nullptr;
                    }
                    break;
                }

                case TYP_DOUBLE:
                case TYP_FLOAT:
                case TYP_SHORT:
                case TYP_USHORT:
                {
                    // short/ushort/float/double is supported by SSE2
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_ARM64)
        case NI_VectorT_LoadAligned:
        case NI_VectorT_LoadAlignedNonTemporal:
        case NI_VectorT_StoreAligned:
        case NI_VectorT_StoreAlignedNonTemporal:
        {
            if (opts.OptimizationDisabled())
            {
                // ARM64 doesn't have aligned loads/stores, but aligned simd ops are only validated
                // to be aligned when optimizations are disable, so only skip the intrinsic handling
                // if optimizations are enabled
                return nullptr;
            }
            break;
        }

        case NI_VectorT_WithElement:
        {
            assert(numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->IsIntegralConst())
            {
                // TODO-ARM64-CQ: We should always import these like we do with GetElement
                // If index is not constant use software fallback.
                return nullptr;
            }

            ssize_t imm8  = indexOp->AsIntCon()->IconValue();
            ssize_t count = simdSize / genTypeSize(simdBaseType);

            if ((imm8 >= count) || (imm8 < 0))
            {
                // Using software fallback if index is out of range (throw exception)
                return nullptr;
            }

            break;
        }
#endif

#if defined(TARGET_XARCH)
        case NI_VectorT_Floor:
        case NI_VectorT_Ceiling:
        {
            if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                return nullptr;
            }
            break;
        }
#endif // TARGET_XARCH

        case NI_VectorT_FusedMultiplyAdd:
        {
            bool isFmaAccelerated = false;

#if defined(TARGET_XARCH)
            isFmaAccelerated = compOpportunisticallyDependsOn(InstructionSet_FMA);
#elif defined(TARGET_ARM64)
            isFmaAccelerated = compOpportunisticallyDependsOn(InstructionSet_AdvSimd);
#endif

            if (!isFmaAccelerated)
            {
                return nullptr;
            }
            break;
        }

#if defined(TARGET_XARCH)
        case NI_VectorT_op_Multiply:
        {
            if (varTypeIsLong(simdBaseType))
            {
                if (!canUseEvexEncoding())
                {
                    // TODO-XARCH-CQ: We should support long/ulong multiplication
                    return nullptr;
                }

#if defined(TARGET_X86)
                // TODO-XARCH-CQ: We need to support 64-bit CreateBroadcast
                return nullptr;
#endif // TARGET_X86
            }
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_XARCH)
        case NI_VectorT_op_RightShift:
        {
            if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
            {
                if (!canUseEvexEncoding())
                {
                    // TODO-XARCH-CQ: We should support long/ulong arithmetic shift
                    return nullptr;
                }
            }
            break;
        }
#endif // TARGET_XARCH

        default:
        {
            // Most intrinsics have some path that works even if only SSE2/AdvSimd is available
            break;
        }
    }

    GenTree* copyBlkDst = nullptr;
    GenTree* copyBlkSrc = nullptr;

    switch (numArgs)
    {
        case 0:
        {
            switch (intrinsic)
            {
                case NI_VectorT_get_AllBitsSet:
                {
                    return gtNewAllBitsSetConNode(retType);
                }

                case NI_VectorT_get_Indices:
                {
                    assert(sig->numArgs == 0);
                    return gtNewSimdGetIndicesNode(retType, simdBaseJitType, simdSize);
                }

                case NI_VectorT_get_One:
                {
                    return gtNewOneConNode(retType, simdBaseType);
                }

                case NI_VectorT_get_Zero:
                {
                    return gtNewZeroConNode(retType);
                }

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and nullptr return below.
                    break;
                }
            }
            break;
        }

        case 1:
        {
            isOpExplicit |= (intrinsic == NI_VectorT_op_Explicit);

            if (isOpExplicit)
            {
                // We fold away the cast here, as it only exists to satisfy the
                // type system. It is safe to do this here since the op1 type
                // and the signature return type are both the same TYP_SIMD.
                op1 = impSIMDPopStack();
                SetOpLclRelatedToSIMDIntrinsic(op1);
                assert(op1->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                return op1;
            }

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            switch (intrinsic)
            {
                case NI_VectorT_Abs:
                {
                    return gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Ceiling:
                {
                    return gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Floor:
                {
                    return gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LoadUnsafe:
                {
                    if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op1 = op1->gtGetOp1();
                    }

                    return gtNewSimdLoadNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LoadAligned:
                {
                    if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op1 = op1->gtGetOp1();
                    }

                    return gtNewSimdLoadAlignedNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LoadAlignedNonTemporal:
                {
                    if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op1 = op1->gtGetOp1();
                    }

                    return gtNewSimdLoadNonTemporalNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_UnaryNegation:
                {
                    return gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_OnesComplement:
                {
                    return gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Sqrt:
                {
                    return gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Sum:
                {
                    return gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ToScalar:
                {
#if defined(TARGET_X86)
                    if (varTypeIsLong(simdBaseType))
                    {
                        op2 = gtNewIconNode(0);
                        return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize);
                    }
#endif // TARGET_X86

                    return gtNewSimdToScalarNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_UnaryPlus:
                {
                    return op1;
                }

                case NI_VectorT_WidenLower:
                {
                    return gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_WidenUpper:
                {
                    return gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize);
                }

#if defined(TARGET_XARCH)
                case NI_VectorT_ConvertToDouble:
                {
                    assert(sig->numArgs == 1);
                    assert(varTypeIsLong(simdBaseType));
                    NamedIntrinsic intrinsic = NI_Illegal;
                    if ((simdSize != 64) && compOpportunisticallyDependsOn(InstructionSet_AVX10v1))
                    {
                        if (simdSize == 32)
                        {
                            intrinsic = NI_AVX10v1_ConvertToVector256Double;
                        }
                        else
                        {
                            assert(simdSize == 16);
                            intrinsic = NI_AVX10v1_ConvertToVector128Double;
                        }
                    }
                    else
                    {
                        if (simdSize == 64)
                        {
                            intrinsic = NI_AVX512DQ_ConvertToVector512Double;
                        }
                        else if (simdSize == 32)
                        {
                            intrinsic = NI_AVX512DQ_VL_ConvertToVector256Double;
                        }
                        else
                        {
                            assert(simdSize == 16);
                            intrinsic = NI_AVX512DQ_VL_ConvertToVector128Double;
                        }
                    }
                    return gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToSingle:
                {
                    assert(varTypeIsInt(simdBaseType));
                    NamedIntrinsic intrinsic = NI_Illegal;
                    if (simdBaseType == TYP_INT)
                    {
                        switch (simdSize)
                        {
                            case 16:
                                intrinsic = NI_SSE2_ConvertToVector128Single;
                                break;
                            case 32:
                                intrinsic = NI_AVX_ConvertToVector256Single;
                                break;
                            case 64:
                                intrinsic = NI_AVX512F_ConvertToVector512Single;
                                break;
                            default:
                                unreached();
                        }
                    }
                    else if (simdBaseType == TYP_UINT && simdSize != 64 &&
                             compOpportunisticallyDependsOn(InstructionSet_AVX10v1))
                    {
                        switch (simdSize)
                        {
                            case 16:
                                intrinsic = NI_AVX10v1_ConvertToVector128Single;
                                break;
                            case 32:
                                intrinsic = NI_AVX10v1_ConvertToVector256Single;
                                break;
                            default:
                                unreached();
                        }
                    }
                    else if (simdBaseType == TYP_UINT)
                    {
                        switch (simdSize)
                        {
                            case 16:
                                intrinsic = NI_AVX512F_VL_ConvertToVector128Single;
                                break;
                            case 32:
                                intrinsic = NI_AVX512F_VL_ConvertToVector256Single;
                                break;
                            case 64:
                                intrinsic = NI_AVX512F_ConvertToVector512Single;
                                break;
                            default:
                                unreached();
                        }
                    }
                    assert(intrinsic != NI_Illegal);
                    return gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, simdBaseJitType, simdSize);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT_ConvertToDouble:
                {
                    assert((simdBaseType == TYP_LONG) || (simdBaseType == TYP_ULONG));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToDouble, simdBaseJitType,
                                                    simdSize);
                }

                case NI_VectorT_ConvertToSingle:
                {
                    assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToSingle, simdBaseJitType,
                                                    simdSize);
                }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

                case NI_VectorT_ConvertToInt32:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_INT, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToInt32Native:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_INT, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToInt64:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_LONG, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToInt64Native:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_LONG, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToUInt32:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_UINT, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToUInt32Native:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_UINT, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToUInt64:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdCvtNode(retType, op1, CORINFO_TYPE_ULONG, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToUInt64Native:
                {
                    assert(sig->numArgs == 1);
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdCvtNativeNode(retType, op1, CORINFO_TYPE_ULONG, simdBaseJitType, simdSize);
                }

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and nullptr return below.
                    break;
                }
            }
            break;
        }

        case 2:
        {
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));

            op1 = getArgForHWIntrinsic(argType, argClass);

            switch (intrinsic)
            {
                case NI_VectorT_op_Addition:
                {
                    return gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_AndNot:
                {
                    return gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_BitwiseAnd:
                {
                    return gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_BitwiseOr:
                {
                    return gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Create:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, simdSize);
                    break;
                }

                case NI_VectorT_CreateSequence:
                {
                    return gtNewSimdCreateSequenceNode(simdType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_Division:
                {
                    return gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Dot:
                {
                    op1 = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_VectorT_Equals:
                {
                    return gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_Equality:
                {
                    return gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_EqualsAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_ExclusiveOr:
                {
                    return gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GetElement:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThan:
                {
                    return gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThanOrEqual:
                {
                    return gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_GreaterThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_Inequality:
                {
                    return gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThan:
                {
                    return gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThanOrEqual:
                {
                    return gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LessThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_LoadUnsafeIndex:
                {
                    GenTree* tmp;

                    if (op1->OperIs(GT_CAST) && op1->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op1 = op1->gtGetOp1();
                    }

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                    op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, tmp);
                    op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);

                    return gtNewSimdLoadNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Max:
                {
                    return gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Min:
                {
                    return gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_Multiply:
                {
                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Narrow:
                {
                    return gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_LeftShift:
                {
                    return gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_RightShift:
                {
                    genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;
                    return gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_UnsignedRightShift:
                {
                    return gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_StoreUnsafe:
                {
                    assert(retType == TYP_VOID);

                    if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op2 = op2->gtGetOp1();
                    }

                    return gtNewSimdStoreNode(op2, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_StoreAligned:
                {
                    assert(retType == TYP_VOID);

                    if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op2 = op2->gtGetOp1();
                    }

                    return gtNewSimdStoreAlignedNode(op2, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_StoreAlignedNonTemporal:
                {
                    assert(retType == TYP_VOID);

                    if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op2 = op2->gtGetOp1();
                    }

                    return gtNewSimdStoreNonTemporalNode(op2, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_op_Subtraction:
                {
                    return gtNewSimdBinOpNode(GT_SUB, retType, op1, op2, simdBaseJitType, simdSize);
                }

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and nullptr return below.
                    break;
                }
            }
            break;
        }

        case 3:
        {
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             3 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op2 side effects for SimdAsHWIntrinsic"));
            }

            CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass);

            switch (intrinsic)
            {
                case NI_VectorT_ConditionalSelect:
                {
                    return gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                }

                case NI_VectorT_FusedMultiplyAdd:
                {
                    return gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                }

                case NI_VectorT_MultiplyAddEstimate:
                {
                    bool isFmaAccelerated = false;

#if defined(TARGET_XARCH)
                    isFmaAccelerated = compExactlyDependsOn(InstructionSet_FMA);
#elif defined(TARGET_ARM64)
                    isFmaAccelerated = compExactlyDependsOn(InstructionSet_AdvSimd);
#endif

                    if (isFmaAccelerated)
                    {
                        return gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                    }

                    GenTree* mulNode = gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
                    return gtNewSimdBinOpNode(GT_ADD, retType, mulNode, op3, simdBaseJitType, simdSize);
                }

                case NI_VectorT_StoreUnsafeIndex:
                {
                    assert(retType == TYP_VOID);
                    GenTree* tmp;

                    if (op2->OperIs(GT_CAST) && op2->gtGetOp1()->TypeIs(TYP_BYREF))
                    {
                        // If what we have is a BYREF, that's what we really want, so throw away the cast.
                        op2 = op2->gtGetOp1();
                    }

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                    op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, tmp);
                    op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);

                    return gtNewSimdStoreNode(op2, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_WithElement:
                {
                    return gtNewSimdWithElementNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                }

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and nullptr return below.
                    break;
                }
            }
            break;
        }
    }

    if (copyBlkDst != nullptr)
    {
        assert(copyBlkSrc != nullptr);
        GenTree* retNode = gtNewStoreValueNode(simdType, copyBlkDst, copyBlkSrc);

        return retNode;
    }
    assert(copyBlkSrc == nullptr);

    assert(!"Unexpected SimdAsHWIntrinsic");
    return nullptr;
}

#endif // FEATURE_HW_INTRINSICS
