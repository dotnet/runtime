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

    unsigned numArgs          = sig->numArgs;
    bool     isInstanceMethod = false;

    if (sig->hasThis())
    {
        numArgs++;
        isInstanceMethod = true;
    }

    if (classId == SimdAsHWIntrinsicClassId::Vector)
    {
        // We want to avoid doing anything that would unnecessarily trigger a recorded dependency against Vector<T>
        // so we duplicate a few checks here to ensure this works smoothly for the static Vector class.

        assert(!isInstanceMethod);

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

        if (isInstanceMethod != SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsicInfo.id))
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
        case 'P':
        {
            if (strcmp(className, "Plane") == 0)
            {
                return SimdAsHWIntrinsicClassId::Plane;
            }
            break;
        }

        case 'Q':
        {
            if (strcmp(className, "Quaternion") == 0)
            {
                return SimdAsHWIntrinsicClassId::Quaternion;
            }
            break;
        }

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
            else if (strcmp(className, "2") == 0)
            {
                return SimdAsHWIntrinsicClassId::Vector2;
            }
            else if (strcmp(className, "3") == 0)
            {
                return SimdAsHWIntrinsicClassId::Vector3;
            }
            else if (strcmp(className, "4") == 0)
            {
                return SimdAsHWIntrinsicClassId::Vector4;
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
                                        GenTree*              newobjThis)
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

    CORINFO_CLASS_HANDLE argClass         = NO_CLASS_HANDLE;
    var_types            retType          = genActualType(JITtype2varType(sig->retType));
    CorInfoType          simdBaseJitType  = CORINFO_TYPE_UNDEF;
    var_types            simdType         = TYP_UNKNOWN;
    unsigned             simdSize         = 0;
    unsigned             numArgs          = sig->numArgs;
    bool                 isInstanceMethod = false;

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

    if (sig->hasThis())
    {
        assert(SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsic));
        numArgs++;

        isInstanceMethod = true;
        argClass         = clsHnd;

        if (SimdAsHWIntrinsicInfo::BaseTypeFromThisArg(intrinsic))
        {
            assert((simdBaseJitType == CORINFO_TYPE_UNDEF) || (simdBaseJitType == CORINFO_TYPE_VALUECLASS));
            simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);
        }
    }
    else if ((clsHnd == m_simdHandleCache->VectorHandle) && (numArgs != 0) &&
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

    if (SimdAsHWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic
        // is operating on SIMD fields, but where no SIMD local vars are in use.
        compFloatingPointUsed = true;
    }

    if (hwIntrinsic == intrinsic)
    {
        // The SIMD intrinsic requires special handling outside the normal code path
        return impSimdAsHWIntrinsicSpecial(intrinsic, clsHnd, sig, retType, simdBaseJitType, simdSize, newobjThis);
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
            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            return gtNewSimdAsHWIntrinsicNode(retType, op1, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case 2:
        {
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                assert(newobjThis == nullptr);
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

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
                                               GenTree*             newobjThis)
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

    unsigned numArgs          = sig->numArgs;
    bool     isInstanceMethod = false;

    if (sig->hasThis())
    {
        assert(SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsic));
        numArgs++;

        isInstanceMethod = true;
        argClass         = clsHnd;
    }

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
#if defined(TARGET_XARCH)
        case NI_VectorT_ConvertToDouble:
        case NI_VectorT_ConvertToInt64:
        case NI_VectorT_ConvertToUInt32:
        case NI_VectorT_ConvertToUInt64:
        {
            // TODO-XARCH-CQ: These intrinsics should be accelerated
            return nullptr;
        }

        case NI_VectorT_ConvertToSingle:
        {
            if (simdBaseType == TYP_UINT)
            {
                // TODO-XARCH-CQ: These intrinsics should be accelerated
                return nullptr;
            }
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_X86)
        case NI_VectorT_CreateBroadcast:
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
        case NI_VectorT_get_Item:
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

        case NI_Quaternion_WithElement:
        case NI_Vector2_WithElement:
        case NI_Vector3_WithElement:
        case NI_Vector4_WithElement:
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

        case NI_Quaternion_WithElement:
        case NI_Vector2_WithElement:
        case NI_Vector3_WithElement:
        case NI_Vector4_WithElement:
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

#if defined(TARGET_XARCH)
        case NI_VectorT_Multiply:
        case NI_VectorT_op_Multiply:
        {
            if (varTypeIsLong(simdBaseType))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512DQ_VL))
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
        case NI_VectorT_ShiftRightArithmetic:
        case NI_VectorT_op_RightShift:
        {
            if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
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
            assert(newobjThis == nullptr);

            switch (intrinsic)
            {
                case NI_VectorT_get_AllBitsSet:
                {
                    return gtNewAllBitsSetConNode(retType);
                }

                case NI_Vector2_get_One:
                case NI_Vector3_get_One:
                case NI_Vector4_get_One:
                case NI_VectorT_get_One:
                {
                    return gtNewOneConNode(retType, simdBaseType);
                }

                case NI_Vector2_get_UnitX:
                case NI_Vector3_get_UnitX:
                case NI_Vector4_get_UnitX:
                {
                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = 1.0f;
                    vecCon->gtSimdVal.f32[1] = 0.0f;
                    vecCon->gtSimdVal.f32[2] = 0.0f;
                    vecCon->gtSimdVal.f32[3] = 0.0f;

                    return vecCon;
                }

                case NI_Vector2_get_UnitY:
                case NI_Vector3_get_UnitY:
                case NI_Vector4_get_UnitY:
                {
                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = 0.0f;
                    vecCon->gtSimdVal.f32[1] = 1.0f;
                    vecCon->gtSimdVal.f32[2] = 0.0f;
                    vecCon->gtSimdVal.f32[3] = 0.0f;

                    return vecCon;
                }

                case NI_Vector3_get_UnitZ:
                case NI_Vector4_get_UnitZ:
                {
                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = 0.0f;
                    vecCon->gtSimdVal.f32[1] = 0.0f;
                    vecCon->gtSimdVal.f32[2] = 1.0f;
                    vecCon->gtSimdVal.f32[3] = 0.0f;

                    return vecCon;
                }

                case NI_Quaternion_get_Identity:
                case NI_Vector4_get_UnitW:
                {
                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = 0.0f;
                    vecCon->gtSimdVal.f32[1] = 0.0f;
                    vecCon->gtSimdVal.f32[2] = 0.0f;
                    vecCon->gtSimdVal.f32[3] = 1.0f;

                    return vecCon;
                }

                case NI_Quaternion_get_Zero:
                case NI_Vector2_get_Zero:
                case NI_Vector3_get_Zero:
                case NI_Vector4_get_Zero:
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
            assert(newobjThis == nullptr);

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

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            switch (intrinsic)
            {
                case NI_Vector2_Abs:
                case NI_Vector3_Abs:
                case NI_Vector4_Abs:
                case NI_VectorT_Abs:
                {
                    return gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Ceiling:
                {
                    return gtNewSimdCeilNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Conjugate:
                {
                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = -1.0f;
                    vecCon->gtSimdVal.f32[1] = -1.0f;
                    vecCon->gtSimdVal.f32[2] = -1.0f;
                    vecCon->gtSimdVal.f32[3] = +1.0f;

                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, vecCon, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Floor:
                {
                    return gtNewSimdFloorNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Inverse:
                {
                    GenTree* clonedOp1;
                    op1 = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for quaternion inverse (1)"));

                    GenTree* clonedOp2;
                    clonedOp1 = impCloneExpr(clonedOp1, &clonedOp2, CHECK_SPILL_ALL,
                                             nullptr DEBUGARG("Clone op1 for quaternion inverse (2)"));

                    GenTreeVecCon* vecCon = gtNewVconNode(retType);

                    vecCon->gtSimdVal.f32[0] = -1.0f;
                    vecCon->gtSimdVal.f32[1] = -1.0f;
                    vecCon->gtSimdVal.f32[2] = -1.0f;
                    vecCon->gtSimdVal.f32[3] = +1.0f;

                    GenTree* conjugate = gtNewSimdBinOpNode(GT_MUL, retType, op1, vecCon, simdBaseJitType, simdSize);
                    op1                = gtNewSimdDotProdNode(retType, clonedOp1, clonedOp2, simdBaseJitType, simdSize);

                    return gtNewSimdBinOpNode(GT_DIV, retType, conjugate, op1, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Length:
                case NI_Vector2_Length:
                case NI_Vector3_Length:
                case NI_Vector4_Length:
                {
                    GenTree* clonedOp1;
                    op1 =
                        impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL, nullptr DEBUGARG("Clone op1 for vector length"));

                    op1 = gtNewSimdDotProdNode(simdType, op1, clonedOp1, simdBaseJitType, simdSize);
                    op1 = gtNewSimdSqrtNode(simdType, op1, simdBaseJitType, simdSize);

                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_Quaternion_LengthSquared:
                case NI_Vector2_LengthSquared:
                case NI_Vector3_LengthSquared:
                case NI_Vector4_LengthSquared:
                {
                    GenTree* clonedOp1;
                    op1 = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for vector length squared"));

                    op1 = gtNewSimdDotProdNode(simdType, op1, clonedOp1, simdBaseJitType, simdSize);
                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_VectorT_Load:
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

                case NI_Quaternion_Negate:
                case NI_Quaternion_op_UnaryNegation:
                case NI_Vector2_Negate:
                case NI_Vector2_op_UnaryNegation:
                case NI_Vector3_Negate:
                case NI_Vector3_op_UnaryNegation:
                case NI_Vector4_Negate:
                case NI_Vector4_op_UnaryNegation:
                case NI_VectorT_Negate:
                case NI_VectorT_op_UnaryNegation:
                {
                    return gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Normalize:
                case NI_Vector2_Normalize:
                case NI_Vector3_Normalize:
                case NI_Vector4_Normalize:
                {
                    GenTree* clonedOp1;
                    op1 = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for vector normalize (1)"));

                    GenTree* clonedOp2;
                    clonedOp1 = impCloneExpr(clonedOp1, &clonedOp2, CHECK_SPILL_ALL,
                                             nullptr DEBUGARG("Clone op1 for vector normalize (2)"));

                    op1 = gtNewSimdDotProdNode(retType, op1, clonedOp1, simdBaseJitType, simdSize);
                    op1 = gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize);

                    return gtNewSimdBinOpNode(GT_DIV, retType, clonedOp2, op1, simdBaseJitType, simdSize);
                }

                case NI_VectorT_OnesComplement:
                case NI_VectorT_op_OnesComplement:
                {
                    return gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Sqrt:
                case NI_Vector3_Sqrt:
                case NI_Vector4_Sqrt:
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
                case NI_VectorT_ConvertToInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    NamedIntrinsic convert;

                    switch (simdSize)
                    {
                        case 16:
                            convert = NI_SSE2_ConvertToVector128Int32WithTruncation;
                            break;
                        case 32:
                            convert = NI_AVX_ConvertToVector256Int32WithTruncation;
                            break;
                        case 64:
                            convert = NI_AVX512F_ConvertToVector512Int32WithTruncation;
                            break;
                        default:
                            unreached();
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, convert, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToSingle:
                {
                    assert(simdBaseType == TYP_INT);
                    NamedIntrinsic convert;

                    switch (simdSize)
                    {
                        case 16:
                            convert = NI_SSE2_ConvertToVector128Single;
                            break;
                        case 32:
                            convert = NI_AVX_ConvertToVector256Single;
                            break;
                        case 64:
                            convert = NI_AVX512F_ConvertToVector512Single;
                            break;
                        default:
                            unreached();
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, convert, simdBaseJitType, simdSize);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT_ConvertToDouble:
                {
                    assert((simdBaseType == TYP_LONG) || (simdBaseType == TYP_ULONG));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToDouble, simdBaseJitType,
                                                    simdSize);
                }

                case NI_VectorT_ConvertToInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToInt32RoundToZero, simdBaseJitType,
                                                    simdSize);
                }

                case NI_VectorT_ConvertToInt64:
                {
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToInt64RoundToZero,
                                                    simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToSingle:
                {
                    assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToSingle, simdBaseJitType,
                                                    simdSize);
                }

                case NI_VectorT_ConvertToUInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToUInt32RoundToZero,
                                                    simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConvertToUInt64:
                {
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToUInt64RoundToZero,
                                                    simdBaseJitType, simdSize);
                }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

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
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic) && (newobjThis == nullptr))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            bool implicitConstructor = isInstanceMethod && (newobjThis == nullptr) && (retType == TYP_VOID);

            if (implicitConstructor)
            {
                op1 = getArgForHWIntrinsic(TYP_BYREF, argClass, isInstanceMethod, newobjThis);
            }
            else
            {
                argType = isInstanceMethod
                              ? simdType
                              : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));

                op1 = getArgForHWIntrinsic(argType, (newobjThis != nullptr) ? clsHnd : argClass, isInstanceMethod,
                                           newobjThis);
            }

            switch (intrinsic)
            {
                case NI_Quaternion_Add:
                case NI_Quaternion_op_Addition:
                case NI_Vector2_Add:
                case NI_Vector2_op_Addition:
                case NI_Vector3_Add:
                case NI_Vector3_op_Addition:
                case NI_Vector4_Add:
                case NI_Vector4_op_Addition:
                case NI_VectorT_Add:
                case NI_VectorT_op_Addition:
                {
                    return gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_AndNot:
                {
                    return gtNewSimdBinOpNode(GT_AND_NOT, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_BitwiseAnd:
                case NI_VectorT_op_BitwiseAnd:
                {
                    return gtNewSimdBinOpNode(GT_AND, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_BitwiseOr:
                case NI_VectorT_op_BitwiseOr:
                {
                    return gtNewSimdBinOpNode(GT_OR, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_CreateBroadcast:
                case NI_Vector3_CreateBroadcast:
                case NI_Vector4_CreateBroadcast:
                case NI_VectorT_CreateBroadcast:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, simdSize);
                    break;
                }

                case NI_Plane_CreateFromVector4:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = op2;

                    break;
                }

                case NI_Vector2_Distance:
                case NI_Vector3_Distance:
                case NI_Vector4_Distance:
                {
                    op1 = gtNewSimdBinOpNode(GT_SUB, simdType, op1, op2, simdBaseJitType, simdSize);

                    GenTree* clonedOp1;
                    op1 = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone diff for vector distance"));

                    op1 = gtNewSimdDotProdNode(simdType, op1, clonedOp1, simdBaseJitType, simdSize);
                    op1 = gtNewSimdSqrtNode(simdType, op1, simdBaseJitType, simdSize);

                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_Vector2_DistanceSquared:
                case NI_Vector3_DistanceSquared:
                case NI_Vector4_DistanceSquared:
                {
                    op1 = gtNewSimdBinOpNode(GT_SUB, simdType, op1, op2, simdBaseJitType, simdSize);

                    GenTree* clonedOp1;
                    op1 = impCloneExpr(op1, &clonedOp1, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone diff for vector distance squared"));

                    op1 = gtNewSimdDotProdNode(simdType, op1, clonedOp1, simdBaseJitType, simdSize);
                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Divide:
                case NI_Vector2_Divide:
                case NI_Vector2_op_Division:
                case NI_Vector3_Divide:
                case NI_Vector3_op_Division:
                case NI_Vector4_Divide:
                case NI_Vector4_op_Division:
                case NI_VectorT_Divide:
                case NI_VectorT_op_Division:
                {
                    return gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Plane_Dot:
                case NI_Quaternion_Dot:
                case NI_Vector2_Dot:
                case NI_Vector3_Dot:
                case NI_Vector4_Dot:
                case NI_VectorT_Dot:
                {
                    op1 = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_VectorT_Equals:
                {
                    return gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Plane_op_Equality:
                case NI_Quaternion_op_Equality:
                case NI_Vector2_op_Equality:
                case NI_Vector3_op_Equality:
                case NI_Vector4_op_Equality:
                case NI_VectorT_EqualsAll:
                case NI_VectorT_op_Equality:
                {
                    return gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_EqualsAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Xor:
                case NI_VectorT_op_ExclusiveOr:
                {
                    return gtNewSimdBinOpNode(GT_XOR, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_get_Item:
                case NI_Quaternion_GetElement:
                case NI_Vector2_get_Item:
                case NI_Vector2_GetElement:
                case NI_Vector3_get_Item:
                case NI_Vector3_GetElement:
                case NI_Vector4_get_Item:
                case NI_Vector4_GetElement:
                case NI_VectorT_get_Item:
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

                case NI_Plane_op_Inequality:
                case NI_Quaternion_op_Inequality:
                case NI_Vector2_op_Inequality:
                case NI_Vector3_op_Inequality:
                case NI_Vector4_op_Inequality:
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

                case NI_Vector2_Max:
                case NI_Vector3_Max:
                case NI_Vector4_Max:
                case NI_VectorT_Max:
                {
                    return gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Min:
                case NI_Vector3_Min:
                case NI_Vector4_Min:
                case NI_VectorT_Min:
                {
                    return gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Quaternion_Multiply:
                case NI_Quaternion_op_Multiply:
                case NI_Vector2_Multiply:
                case NI_Vector2_op_Multiply:
                case NI_Vector3_Multiply:
                case NI_Vector3_op_Multiply:
                case NI_Vector4_Multiply:
                case NI_Vector4_op_Multiply:
                case NI_VectorT_Multiply:
                case NI_VectorT_op_Multiply:
                {
                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Narrow:
                {
                    return gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ShiftLeft:
                case NI_VectorT_op_LeftShift:
                {
                    return gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ShiftRightArithmetic:
                case NI_VectorT_op_RightShift:
                {
                    genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;
                    return gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ShiftRightLogical:
                case NI_VectorT_op_UnsignedRightShift:
                {
                    return gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_VectorT_Store:
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

                case NI_Quaternion_Subtract:
                case NI_Quaternion_op_Subtraction:
                case NI_Vector2_Subtract:
                case NI_Vector2_op_Subtraction:
                case NI_Vector3_Subtract:
                case NI_Vector3_op_Subtraction:
                case NI_Vector4_Subtract:
                case NI_Vector4_op_Subtraction:
                case NI_VectorT_Subtract:
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
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic) && (newobjThis == nullptr))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             3 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic) && (newobjThis == nullptr))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op2 side effects for SimdAsHWIntrinsic"));
            }

            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            bool implicitConstructor = isInstanceMethod && (newobjThis == nullptr) && (retType == TYP_VOID);

            if (implicitConstructor)
            {
                op1 = getArgForHWIntrinsic(TYP_BYREF, argClass, isInstanceMethod, newobjThis);
            }
            else
            {
                argType = isInstanceMethod
                              ? simdType
                              : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));

                op1 = getArgForHWIntrinsic(argType, (newobjThis != nullptr) ? clsHnd : argClass, isInstanceMethod,
                                           newobjThis);
            }

            switch (intrinsic)
            {
                case NI_Vector2_Clamp:
                case NI_Vector3_Clamp:
                case NI_Vector4_Clamp:
                {
                    GenTree* maxNode = gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
                    return gtNewSimdMinNode(retType, maxNode, op3, simdBaseJitType, simdSize);
                }

                case NI_VectorT_ConditionalSelect:
                {
                    return gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Lerp:
                case NI_Vector3_Lerp:
                case NI_Vector4_Lerp:
                {
                    // We generate nodes equivalent to `(op1 * (1.0f - op3)) + (op2 * op3)`
                    // optimizing for xarch by doing a single broadcast and for arm64 by
                    // using multiply by scalar

                    assert(simdBaseType == TYP_FLOAT);

#if defined(TARGET_XARCH)
                    // op3 = broadcast(op3)
                    op3 = gtNewSimdCreateBroadcastNode(retType, op3, simdBaseJitType, simdSize);
#endif // TARGET_XARCH

                    // clonedOp3 = op3
                    GenTree* clonedOp3;
                    op3 = impCloneExpr(op3, &clonedOp3, CHECK_SPILL_ALL, nullptr DEBUGARG("Clone op3 for vector lerp"));

#if defined(TARGET_XARCH)
                    // op3 = 1.0f - op3
                    GenTree* oneCon = gtNewOneConNode(retType, simdBaseType);
                    op3             = gtNewSimdBinOpNode(GT_SUB, retType, oneCon, op3, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
                    // op3 = 1.0f - op3
                    GenTree* oneCon = gtNewOneConNode(simdBaseType);
                    op3             = gtNewOperNode(GT_SUB, TYP_FLOAT, oneCon, op3);
#else
#error Unsupported platform
#endif

                    // op1 *= op3
                    op1 = gtNewSimdBinOpNode(GT_MUL, retType, op1, op3, simdBaseJitType, simdSize);

                    // op2 *= clonedOp3
                    op2 = gtNewSimdBinOpNode(GT_MUL, retType, op2, clonedOp3, simdBaseJitType, simdSize);

                    // return op1 + op2
                    return gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
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

                case NI_Vector2_Create:
                {
                    assert(retType == TYP_VOID);
                    assert(simdBaseType == TYP_FLOAT);
                    assert(simdSize == 8);

                    if (op2->IsCnsFltOrDbl() && op3->IsCnsFltOrDbl())
                    {
                        GenTreeVecCon* vecCon = gtNewVconNode(TYP_SIMD8);

                        float cnsVal = 0;

                        vecCon->gtSimdVal.f32[0] = static_cast<float>(op2->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[1] = static_cast<float>(op3->AsDblCon()->DconValue());

                        copyBlkSrc = vecCon;
                    }
                    else if (areArgumentsContiguous(op2, op3))
                    {
                        GenTree* op2Address = CreateAddressNodeForSimdHWIntrinsicCreate(op2, simdBaseType, 8);
                        copyBlkSrc          = gtNewIndir(TYP_SIMD8, op2Address);
                    }
                    else
                    {
#if defined(TARGET_XARCH)
                        IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), 4);

                        nodeBuilder.AddOperand(0, op2);
                        nodeBuilder.AddOperand(1, op3);
                        nodeBuilder.AddOperand(2, gtNewZeroConNode(TYP_FLOAT));
                        nodeBuilder.AddOperand(3, gtNewZeroConNode(TYP_FLOAT));

                        copyBlkSrc = gtNewSimdHWIntrinsicNode(TYP_SIMD8, std::move(nodeBuilder), NI_Vector128_Create,
                                                              simdBaseJitType, 16);
#elif defined(TARGET_ARM64)
                        copyBlkSrc =
                            gtNewSimdHWIntrinsicNode(TYP_SIMD8, op2, op3, NI_Vector64_Create, simdBaseJitType, 8);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
                    }

                    copyBlkDst = op1;
                    break;
                }

                case NI_Plane_CreateFromVector3:
                case NI_Quaternion_CreateFromVector3:
                case NI_Vector3_CreateFromVector2:
                case NI_Vector4_CreateFromVector3:
                {
                    assert(retType == TYP_VOID);
                    assert(simdBaseType == TYP_FLOAT);
                    assert((simdSize == 12) || (simdSize == 16));

                    // TODO-CQ: We should be able to check for contiguous args here after
                    // the relevant methods are updated to support more than just float

                    if (op2->IsCnsVec() && op3->IsCnsFltOrDbl())
                    {
                        GenTreeVecCon* vecCon = op2->AsVecCon();
                        vecCon->gtType        = simdType;

                        if (simdSize == 12)
                        {
                            vecCon->gtSimdVal.f32[2] = static_cast<float>(op3->AsDblCon()->DconValue());
                        }
                        else
                        {
                            vecCon->gtSimdVal.f32[3] = static_cast<float>(op3->AsDblCon()->DconValue());
                        }

                        copyBlkSrc = vecCon;
                    }
                    else
                    {
                        GenTree* idx = gtNewIconNode((simdSize == 12) ? 2 : 3, TYP_INT);
                        copyBlkSrc   = gtNewSimdWithElementNode(simdType, op2, idx, op3, simdBaseJitType, simdSize);
                    }

                    copyBlkDst = op1;
                    break;
                }

                case NI_Quaternion_WithElement:
                case NI_Vector2_WithElement:
                case NI_Vector3_WithElement:
                case NI_Vector4_WithElement:
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

        case 4:
        {
            assert(isInstanceMethod);
            assert(SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic));
            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            if (newobjThis == nullptr)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             4 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            CORINFO_ARG_LIST_HANDLE arg2 = argList;
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);
            CORINFO_ARG_LIST_HANDLE arg4 = info.compCompHnd->getArgNext(arg3);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
            op4     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            if ((newobjThis == nullptr) && (retType == TYP_VOID))
            {
                op1 = getArgForHWIntrinsic(TYP_BYREF, argClass, isInstanceMethod, newobjThis);
            }
            else
            {
                op1 = getArgForHWIntrinsic(simdType, (newobjThis != nullptr) ? clsHnd : argClass, isInstanceMethod,
                                           newobjThis);
            }

            switch (intrinsic)
            {
                case NI_Vector3_Create:
                {
                    assert(retType == TYP_VOID);
                    assert(simdBaseType == TYP_FLOAT);
                    assert(simdSize == 12);

                    if (op2->IsCnsFltOrDbl() && op3->IsCnsFltOrDbl() && op4->IsCnsFltOrDbl())
                    {
                        GenTreeVecCon* vecCon = gtNewVconNode(TYP_SIMD12);

                        float cnsVal = 0;

                        vecCon->gtSimdVal.f32[0] = static_cast<float>(op2->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[1] = static_cast<float>(op3->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[2] = static_cast<float>(op4->AsDblCon()->DconValue());

                        copyBlkSrc = vecCon;
                    }
                    else if (areArgumentsContiguous(op2, op3) && areArgumentsContiguous(op3, op4))
                    {
                        GenTree* op2Address = CreateAddressNodeForSimdHWIntrinsicCreate(op2, simdBaseType, 12);
                        copyBlkSrc          = gtNewIndir(TYP_SIMD12, op2Address);
                    }
                    else
                    {
                        IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), 4);

                        nodeBuilder.AddOperand(0, op2);
                        nodeBuilder.AddOperand(1, op3);
                        nodeBuilder.AddOperand(2, op4);
                        nodeBuilder.AddOperand(3, gtNewZeroConNode(TYP_FLOAT));

                        copyBlkSrc = gtNewSimdHWIntrinsicNode(TYP_SIMD12, std::move(nodeBuilder), NI_Vector128_Create,
                                                              simdBaseJitType, 16);
                    }

                    copyBlkDst = op1;
                    break;
                }

                case NI_Vector4_CreateFromVector2:
                {
                    assert(retType == TYP_VOID);
                    assert(simdBaseType == TYP_FLOAT);
                    assert(simdSize == 16);

                    // TODO-CQ: We should be able to check for contiguous args here after
                    // the relevant methods are updated to support more than just float

                    if (op2->IsCnsVec() && op3->IsCnsFltOrDbl() && op4->IsCnsFltOrDbl())
                    {
                        GenTreeVecCon* vecCon = op2->AsVecCon();
                        vecCon->gtType        = simdType;

                        vecCon->gtSimdVal.f32[2] = static_cast<float>(op3->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[3] = static_cast<float>(op4->AsDblCon()->DconValue());

                        copyBlkSrc = vecCon;
                    }
                    else
                    {
                        GenTree* idx = gtNewIconNode(2, TYP_INT);
                        op2          = gtNewSimdWithElementNode(simdType, op2, idx, op3, simdBaseJitType, simdSize);

                        idx        = gtNewIconNode(3, TYP_INT);
                        copyBlkSrc = gtNewSimdWithElementNode(simdType, op2, idx, op4, simdBaseJitType, simdSize);
                    }

                    copyBlkDst = op1;
                    break;
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

        case 5:
        {
            assert(isInstanceMethod);
            assert(SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic));
            assert(!SimdAsHWIntrinsicInfo::SpillSideEffectsOp2(intrinsic));

            if (newobjThis == nullptr)
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             5 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            CORINFO_ARG_LIST_HANDLE arg2 = argList;
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);
            CORINFO_ARG_LIST_HANDLE arg4 = info.compCompHnd->getArgNext(arg3);
            CORINFO_ARG_LIST_HANDLE arg5 = info.compCompHnd->getArgNext(arg4);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg5, &argClass)));
            op5     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg4, &argClass)));
            op4     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            if ((newobjThis == nullptr) && (retType == TYP_VOID))
            {
                op1 = getArgForHWIntrinsic(TYP_BYREF, argClass, isInstanceMethod, newobjThis);
            }
            else
            {
                op1 = getArgForHWIntrinsic(simdType, (newobjThis != nullptr) ? clsHnd : argClass, isInstanceMethod,
                                           newobjThis);
            }

            switch (intrinsic)
            {
                case NI_Plane_Create:
                case NI_Quaternion_Create:
                case NI_Vector4_Create:
                {
                    assert(retType == TYP_VOID);
                    assert(simdBaseType == TYP_FLOAT);
                    assert(simdSize == 16);

                    if (op2->IsCnsFltOrDbl() && op3->IsCnsFltOrDbl() && op4->IsCnsFltOrDbl() && op5->IsCnsFltOrDbl())
                    {
                        GenTreeVecCon* vecCon = gtNewVconNode(TYP_SIMD16);

                        float cnsVal = 0;

                        vecCon->gtSimdVal.f32[0] = static_cast<float>(op2->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[1] = static_cast<float>(op3->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[2] = static_cast<float>(op4->AsDblCon()->DconValue());
                        vecCon->gtSimdVal.f32[3] = static_cast<float>(op5->AsDblCon()->DconValue());

                        copyBlkSrc = vecCon;
                    }
                    else if (areArgumentsContiguous(op2, op3) && areArgumentsContiguous(op3, op4) &&
                             areArgumentsContiguous(op4, op5))
                    {
                        GenTree* op2Address = CreateAddressNodeForSimdHWIntrinsicCreate(op2, simdBaseType, 16);
                        copyBlkSrc          = gtNewIndir(TYP_SIMD16, op2Address);
                    }
                    else
                    {
                        IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), 4);

                        nodeBuilder.AddOperand(0, op2);
                        nodeBuilder.AddOperand(1, op3);
                        nodeBuilder.AddOperand(2, op4);
                        nodeBuilder.AddOperand(3, op5);

                        copyBlkSrc = gtNewSimdHWIntrinsicNode(TYP_SIMD16, std::move(nodeBuilder), NI_Vector128_Create,
                                                              simdBaseJitType, 16);
                    }

                    copyBlkDst = op1;
                    break;
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
