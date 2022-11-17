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
//    className          -- The name of the class associated with the SimdIntrinsic to lookup
//    methodName         -- The name of the method associated with the SimdIntrinsic to lookup
//    enclosingClassName -- The name of the enclosing class
//    sizeOfVectorT      -- The size of Vector<T> in bytes
//
// Return Value:
//    The NamedIntrinsic associated with methodName and classId
NamedIntrinsic SimdAsHWIntrinsicInfo::lookupId(Compiler*         comp,
                                               CORINFO_SIG_INFO* sig,
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

    unsigned numArgs          = sig->numArgs;
    bool     isInstanceMethod = false;

    if (sig->hasThis())
    {
        numArgs++;
        isInstanceMethod = true;
    }

    if (strcmp(methodName, "get_IsHardwareAccelerated") == 0)
    {
        return comp->IsBaselineSimdIsaSupported() ? NI_IsSupported_True : NI_IsSupported_False;
    }

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
    if ((strcmp(className, "Vector") == 0) || (strcmp(className, "Vector`1") == 0))
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
    var_types            retType          = JITtype2varType(sig->retType);
    CorInfoType          simdBaseJitType  = CORINFO_TYPE_UNDEF;
    var_types            simdType         = TYP_UNKNOWN;
    unsigned             simdSize         = 0;
    unsigned             numArgs          = sig->numArgs;
    bool                 isInstanceMethod = false;

    // We want to resolve and populate the handle cache for this type even
    // if it isn't the basis for anything carried on the node.
    simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);

    if ((clsHnd != m_simdHandleCache->SIMDVectorHandle) &&
        ((simdBaseJitType == CORINFO_TYPE_UNDEF) || !varTypeIsArithmetic(JitType2PreciseVarType(simdBaseJitType))))
    {
        // We want to exit early if the clsHnd should have a base type and it isn't one
        // of the supported types. This handles cases like op_Explicit which take a Vector<T>
        return nullptr;
    }

    if (retType == TYP_STRUCT)
    {
        simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &simdSize);
        retType         = getSIMDTypeForSize(simdSize);
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
            assert(simdBaseJitType == CORINFO_TYPE_UNDEF);
            simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);
        }
    }
    else if ((clsHnd == m_simdHandleCache->SIMDVectorHandle) && (numArgs != 0) &&
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
            assert(!SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic));
            return gtNewSimdAsHWIntrinsicNode(retType, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case 1:
        {
            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            assert(!SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic));
            return gtNewSimdAsHWIntrinsicNode(retType, op1, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case 2:
        {
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             2 DEBUGARG("Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            if (SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic))
            {
                std::swap(op1, op2);
            }

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
    bool isVectorT256 = (SimdAsHWIntrinsicInfo::lookupClassId(intrinsic) == SimdAsHWIntrinsicClassId::VectorT256);

    // We should have already exited early if SSE2 isn't supported
    assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));

    // Vector<T>, when 32-bytes, requires at least AVX2
    assert(!isVectorT256 || compIsaSupportedDebugOnly(InstructionSet_AVX2));
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
        case NI_VectorT128_ConvertToDouble:
        case NI_VectorT256_ConvertToDouble:
        case NI_VectorT128_ConvertToInt64:
        case NI_VectorT256_ConvertToInt64:
        case NI_VectorT128_ConvertToUInt32:
        case NI_VectorT256_ConvertToUInt32:
        case NI_VectorT128_ConvertToUInt64:
        case NI_VectorT256_ConvertToUInt64:
        {
            // TODO-XARCH-CQ: These intrinsics should be accelerated
            return nullptr;
        }

        case NI_VectorT128_ConvertToSingle:
        case NI_VectorT256_ConvertToSingle:
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
        case NI_VectorT128_CreateBroadcast:
        case NI_VectorT256_CreateBroadcast:
        {
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to emit the movq
                // instruction, which takes a 64-bit memory address and
                // works on 32-bit x86 systems.
                return nullptr;
            }
            break;
        }
#endif // TARGET_X86

#if defined(TARGET_XARCH)
        case NI_VectorT256_As:
        case NI_VectorT256_AsVectorByte:
        case NI_VectorT256_AsVectorDouble:
        case NI_VectorT256_AsVectorInt16:
        case NI_VectorT256_AsVectorInt32:
        case NI_VectorT256_AsVectorInt64:
        case NI_VectorT256_AsVectorNInt:
        case NI_VectorT256_AsVectorNUInt:
        case NI_VectorT256_AsVectorSByte:
        case NI_VectorT256_AsVectorSingle:
        case NI_VectorT256_AsVectorUInt16:
        case NI_VectorT256_AsVectorUInt32:
        case NI_VectorT256_AsVectorUInt64:
#endif // TARGET_XARCH
        case NI_VectorT128_As:
        case NI_VectorT128_AsVectorByte:
        case NI_VectorT128_AsVectorDouble:
        case NI_VectorT128_AsVectorInt16:
        case NI_VectorT128_AsVectorInt32:
        case NI_VectorT128_AsVectorInt64:
        case NI_VectorT128_AsVectorNInt:
        case NI_VectorT128_AsVectorNUInt:
        case NI_VectorT128_AsVectorSByte:
        case NI_VectorT128_AsVectorSingle:
        case NI_VectorT128_AsVectorUInt16:
        case NI_VectorT128_AsVectorUInt32:
        case NI_VectorT128_AsVectorUInt64:
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
        case NI_VectorT128_get_Item:
        case NI_VectorT256_get_Item:
        case NI_VectorT128_GetElement:
        case NI_VectorT256_GetElement:
        {
            switch (simdBaseType)
            {
                // Using software fallback if simdBaseType is not supported by hardware
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_INT:
                case TYP_UINT:
                case TYP_LONG:
                case TYP_ULONG:
                    if (!compExactlyDependsOn(InstructionSet_SSE41))
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
            }
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_XARCH)
        case NI_VectorT128_Dot:
        {
            if (!compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                // We need to exit early if this is Vector<T>.Dot for int or uint and SSE41 is not supported
                // The other types should be handled via the table driven paths

                assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));
                return nullptr;
            }
            break;
        }

        case NI_VectorT128_Sum:
        {
            if (varTypeIsFloating(simdBaseType))
            {
                if (!compOpportunisticallyDependsOn(InstructionSet_SSE3))
                {
                    // Floating-point types require SSE3.HorizontalAdd
                    return nullptr;
                }
            }
            else if (!compOpportunisticallyDependsOn(InstructionSet_SSSE3))
            {
                // Integral types require SSSE3.HorizontalAdd
                return nullptr;
            }
            break;
        }

        case NI_VectorT128_ToScalar:
        case NI_VectorT256_ToScalar:
        {
#if defined(TARGET_X86)
            if (varTypeIsLong(simdBaseType))
            {
                // TODO-XARCH-CQ: It may be beneficial to decompose this operation
                return nullptr;
            }
#endif // TARGET_X86
            break;
        }

        case NI_VectorT128_WithElement:
        case NI_VectorT256_WithElement:
        {
            assert(sig->numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->OperIsConst())
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
                    if (!compExactlyDependsOn(InstructionSet_SSE41))
                    {
                        return nullptr;
                    }
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                {
                    if (!compExactlyDependsOn(InstructionSet_SSE41_X64))
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
        case NI_VectorT128_LoadAligned:
        case NI_VectorT128_LoadAlignedNonTemporal:
        case NI_VectorT128_StoreAligned:
        case NI_VectorT128_StoreAlignedNonTemporal:
        {
            if (!opts.MinOpts())
            {
                // ARM64 doesn't have aligned loads/stores, but aligned simd ops are only validated
                // to be aligned during minopts, so only skip the intrinsic handling if we're minopts
                return nullptr;
            }
            break;
        }

        case NI_VectorT128_WithElement:
        {
            assert(numArgs == 3);
            GenTree* indexOp = impStackTop(1).val;

            if (!indexOp->OperIsConst())
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
#if defined(TARGET_XARCH)
                case NI_VectorT128_get_AllBitsSet:
                case NI_VectorT256_get_AllBitsSet:
                {
                    return gtNewAllBitsSetConNode(retType);
                }

                case NI_VectorT128_get_Count:
                case NI_VectorT256_get_Count:
                {
                    GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
                    countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
                    return countNode;
                }

                case NI_Vector2_get_One:
                case NI_Vector3_get_One:
                case NI_Vector4_get_One:
                case NI_VectorT128_get_One:
                case NI_VectorT256_get_One:
                {
                    return gtNewOneConNode(retType, simdBaseType);
                }

                case NI_Vector2_get_Zero:
                case NI_Vector3_get_Zero:
                case NI_Vector4_get_Zero:
                case NI_VectorT128_get_Zero:
                case NI_VectorT256_get_Zero:
                {
                    return gtNewZeroConNode(retType);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT128_get_AllBitsSet:
                {
                    return gtNewAllBitsSetConNode(retType);
                }

                case NI_VectorT128_get_Count:
                {
                    GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
                    countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
                    return countNode;
                }

                case NI_Vector2_get_One:
                case NI_Vector3_get_One:
                case NI_Vector4_get_One:
                case NI_VectorT128_get_One:
                {
                    return gtNewOneConNode(retType, simdBaseType);
                }

                case NI_Vector2_get_Zero:
                case NI_Vector3_get_Zero:
                case NI_Vector4_get_Zero:
                case NI_VectorT128_get_Zero:
                {
                    return gtNewZeroConNode(retType);
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

        case 1:
        {
            assert(newobjThis == nullptr);

            isOpExplicit |= (intrinsic == NI_VectorT128_op_Explicit);

#if defined(TARGET_XARCH)
            isOpExplicit |= (intrinsic == NI_VectorT256_op_Explicit);
#endif

            if (isOpExplicit)
            {
                // We fold away the cast here, as it only exists to satisfy the
                // type system. It is safe to do this here since the op1 type
                // and the signature return type are both the same TYP_SIMD.

                op1 = impSIMDPopStack(retType, /* expectAddr: */ false, sig->retTypeClass);
                SetOpLclRelatedToSIMDIntrinsic(op1);
                assert(op1->gtType == getSIMDTypeForSize(getSIMDTypeSizeInBytes(sig->retTypeSigClass)));

                return op1;
            }

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            assert(!SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic));

            switch (intrinsic)
            {
#if defined(TARGET_XARCH)
                case NI_Vector2_Abs:
                case NI_Vector3_Abs:
                case NI_Vector4_Abs:
                case NI_VectorT128_Abs:
                case NI_VectorT256_Abs:
                {
                    return gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToInt32:
                case NI_VectorT256_ConvertToInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    NamedIntrinsic convert = (simdSize == 32) ? NI_AVX_ConvertToVector256Int32WithTruncation
                                                              : NI_SSE2_ConvertToVector128Int32WithTruncation;
                    return gtNewSimdHWIntrinsicNode(retType, op1, convert, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToSingle:
                case NI_VectorT256_ConvertToSingle:
                {
                    assert(simdBaseType == TYP_INT);
                    NamedIntrinsic convert =
                        (simdSize == 32) ? NI_AVX_ConvertToVector256Single : NI_SSE2_ConvertToVector128Single;
                    return gtNewSimdHWIntrinsicNode(retType, op1, convert, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Load:
                case NI_VectorT256_Load:
                case NI_VectorT128_LoadUnsafe:
                case NI_VectorT256_LoadUnsafe:
                {
                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    NamedIntrinsic loadIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        loadIntrinsic = NI_AVX_LoadVector256;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        loadIntrinsic = NI_SSE2_LoadVector128;
                    }
                    else
                    {
                        loadIntrinsic = NI_SSE_LoadVector128;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LoadAligned:
                case NI_VectorT256_LoadAligned:
                {
                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    NamedIntrinsic loadIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        loadIntrinsic = NI_AVX_LoadAlignedVector256;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        loadIntrinsic = NI_SSE2_LoadAlignedVector128;
                    }
                    else
                    {
                        loadIntrinsic = NI_SSE_LoadAlignedVector128;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LoadAlignedNonTemporal:
                case NI_VectorT256_LoadAlignedNonTemporal:
                {
                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    // We don't guarantee a non-temporal load will actually occur, so fallback
                    // to regular aligned loads if the required ISA isn't supported.

                    NamedIntrinsic loadIntrinsic = NI_Illegal;
                    bool           isNonTemporal = false;

                    if (simdSize == 32)
                    {
                        if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
                        {
                            loadIntrinsic = NI_AVX2_LoadAlignedVector256NonTemporal;
                            isNonTemporal = true;
                        }
                        else
                        {
                            loadIntrinsic = NI_AVX_LoadAlignedVector256;
                        }
                    }
                    else if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        loadIntrinsic = NI_SSE41_LoadAlignedVector128NonTemporal;
                        isNonTemporal = true;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        loadIntrinsic = NI_SSE2_LoadAlignedVector128;
                    }
                    else
                    {
                        loadIntrinsic = NI_SSE_LoadAlignedVector128;
                    }

                    if (isNonTemporal)
                    {
                        // float and double don't have actual instructions for non-temporal loads
                        // so we'll just use the equivalent integer instruction instead.

                        if (simdBaseType == TYP_FLOAT)
                        {
                            simdBaseJitType = CORINFO_TYPE_INT;
                        }
                        else if (simdBaseType == TYP_DOUBLE)
                        {
                            simdBaseJitType = CORINFO_TYPE_LONG;
                        }
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Negate:
                case NI_VectorT256_Negate:
                case NI_VectorT128_op_UnaryNegation:
                case NI_VectorT256_op_UnaryNegation:
                {
                    return gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_OnesComplement:
                case NI_VectorT256_OnesComplement:
                case NI_VectorT128_op_OnesComplement:
                case NI_VectorT256_op_OnesComplement:
                {
                    return gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_UnaryPlus:
                case NI_VectorT256_op_UnaryPlus:
                {
                    return op1;
                }

                case NI_VectorT128_Sum:
                case NI_VectorT256_Sum:
                {
                    return gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ToScalar:
                {
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector128_ToScalar, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT256_ToScalar:
                {
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector256_ToScalar, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WidenLower:
                case NI_VectorT256_WidenLower:
                {
                    return gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WidenUpper:
                case NI_VectorT256_WidenUpper:
                {
                    return gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT128_Abs:
                {
                    return gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToDouble:
                {
                    assert((simdBaseType == TYP_LONG) || (simdBaseType == TYP_ULONG));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToDouble, simdBaseJitType,
                                                    simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToInt32RoundToZero, simdBaseJitType,
                                                    simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToInt64:
                {
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToInt64RoundToZero,
                                                    simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToSingle:
                {
                    assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToSingle, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToUInt32:
                {
                    assert(simdBaseType == TYP_FLOAT);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_ConvertToUInt32RoundToZero,
                                                    simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ConvertToUInt64:
                {
                    assert(simdBaseType == TYP_DOUBLE);
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_Arm64_ConvertToUInt64RoundToZero,
                                                    simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Load:
                case NI_VectorT128_LoadUnsafe:
                case NI_VectorT128_LoadAligned:
                {
                    // ARM64 doesn't have aligned loads, but aligned loads are only validated to be
                    // aligned during minopts, so the earlier check skips ths intrinsic handling if
                    // we're minopts and we should otherwise treat it as a regular load

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_LoadVector128, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LoadAlignedNonTemporal:
                {
                    // ARM64 has non-temporal loads (LDNP) but we don't currently support them

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_LoadVector128, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Negate:
                case NI_VectorT128_op_UnaryNegation:
                {
                    return gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_OnesComplement:
                case NI_VectorT128_op_OnesComplement:
                {
                    return gtNewSimdUnOpNode(GT_NOT, retType, op1, simdBaseJitType, simdSize,
                                             /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_UnaryPlus:
                {
                    return op1;
                }

                case NI_VectorT128_Sum:
                {
                    return gtNewSimdSumNode(retType, op1, simdBaseJitType, simdSize, /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_ToScalar:
                {
                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_Vector128_ToScalar, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WidenLower:
                {
                    return gtNewSimdWidenLowerNode(retType, op1, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WidenUpper:
                {
                    return gtNewSimdWidenUpperNode(retType, op1, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
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
            if (SimdAsHWIntrinsicInfo::SpillSideEffectsOp1(intrinsic))
            {
                impSpillSideEffect(true, verCurrentState.esStackDepth -
                                             ((newobjThis == nullptr) ? 2 : 1)DEBUGARG(
                                                 "Spilling op1 side effects for SimdAsHWIntrinsic"));
            }

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

            assert(!SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic));

            switch (intrinsic)
            {
#if defined(TARGET_XARCH)
                case NI_Vector2_CreateBroadcast:
                case NI_Vector3_CreateBroadcast:
                case NI_Vector4_CreateBroadcast:
                case NI_VectorT128_CreateBroadcast:
                case NI_VectorT256_CreateBroadcast:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, simdSize,
                                                              /* isSimdAsHWIntrinsic */ true);
                    break;
                }

                case NI_VectorT128_Divide:
                case NI_VectorT256_Divide:
                case NI_VectorT128_op_Division:
                case NI_VectorT256_op_Division:
                {
                    return gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Dot:
                {
                    return gtNewSimdDotProdNode(retType, op1, op2, simdBaseJitType, simdSize,
                                                /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Equals:
                {
                    return gtNewSimdCmpOpNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_EqualsAll:
                case NI_VectorT256_EqualsAll:
                case NI_VectorT128_op_Equality:
                case NI_VectorT256_op_Equality:
                {
                    return gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_EqualsAny:
                case NI_VectorT256_EqualsAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_get_Item:
                case NI_VectorT256_get_Item:
                case NI_VectorT128_GetElement:
                case NI_VectorT256_GetElement:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThan:
                case NI_VectorT256_GreaterThan:
                {
                    return gtNewSimdCmpOpNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanAll:
                case NI_VectorT256_GreaterThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanAny:
                case NI_VectorT256_GreaterThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanOrEqual:
                case NI_VectorT256_GreaterThanOrEqual:
                {
                    return gtNewSimdCmpOpNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanOrEqualAll:
                case NI_VectorT256_GreaterThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanOrEqualAny:
                case NI_VectorT256_GreaterThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThan:
                case NI_VectorT256_LessThan:
                {
                    return gtNewSimdCmpOpNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanAll:
                case NI_VectorT256_LessThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanAny:
                case NI_VectorT256_LessThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanOrEqual:
                case NI_VectorT256_LessThanOrEqual:
                {
                    return gtNewSimdCmpOpNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanOrEqualAll:
                case NI_VectorT256_LessThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanOrEqualAny:
                case NI_VectorT256_LessThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LoadUnsafeIndex:
                case NI_VectorT256_LoadUnsafeIndex:
                {
                    GenTree* tmp;

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                    op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, tmp);
                    op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);

                    NamedIntrinsic loadIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        loadIntrinsic = NI_AVX_LoadVector256;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        loadIntrinsic = NI_SSE2_LoadVector128;
                    }
                    else
                    {
                        loadIntrinsic = NI_SSE_LoadVector128;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op1, loadIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Max:
                case NI_VectorT256_Max:
                {
                    return gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Min:
                case NI_VectorT256_Min:
                {
                    return gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Multiply:
                case NI_VectorT256_Multiply:
                case NI_VectorT128_op_Multiply:
                case NI_VectorT256_op_Multiply:
                {
                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Narrow:
                case NI_VectorT256_Narrow:
                {
                    return gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
                }

                case NI_Vector2_op_Division:
                case NI_Vector3_op_Division:
                {
                    // Vector2/3 div: since the top-most elements will be zero, we end up
                    // performing 0/0 which is a NAN. Therefore, post division we need to set the
                    // top-most elements to zero. This is achieved by left logical shift followed
                    // by right logical shift of the result.

                    // These are 16 byte operations, so we subtract from 16 bytes, not the vector register length.
                    unsigned shiftCount = 16 - simdSize;
                    assert((shiftCount > 0) && (shiftCount <= 16));

                    // retNode = Sse.Divide(op1, op2);
                    GenTree* retNode =
                        gtNewSimdAsHWIntrinsicNode(retType, op1, op2, NI_SSE_Divide, simdBaseJitType, simdSize);

                    // retNode = Sse.ShiftLeftLogical128BitLane(retNode.AsInt32(), shiftCount).AsSingle()
                    retNode =
                        gtNewSimdAsHWIntrinsicNode(retType, retNode, gtNewIconNode(shiftCount, TYP_INT),
                                                   NI_SSE2_ShiftLeftLogical128BitLane, CORINFO_TYPE_INT, simdSize);

                    // retNode = Sse.ShiftRightLogical128BitLane(retNode.AsInt32(), shiftCount).AsSingle()
                    retNode =
                        gtNewSimdAsHWIntrinsicNode(retType, retNode, gtNewIconNode(shiftCount, TYP_INT),
                                                   NI_SSE2_ShiftRightLogical128BitLane, CORINFO_TYPE_INT, simdSize);

                    return retNode;
                }

                case NI_VectorT128_op_LeftShift:
                case NI_VectorT256_op_LeftShift:
                case NI_VectorT128_ShiftLeft:
                case NI_VectorT256_ShiftLeft:
                {
                    return gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_RightShift:
                case NI_VectorT256_op_RightShift:
                case NI_VectorT128_ShiftRightArithmetic:
                case NI_VectorT256_ShiftRightArithmetic:
                {
                    genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;
                    return gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_UnsignedRightShift:
                case NI_VectorT256_op_UnsignedRightShift:
                case NI_VectorT128_ShiftRightLogical:
                case NI_VectorT256_ShiftRightLogical:
                {
                    return gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Store:
                case NI_VectorT256_Store:
                case NI_VectorT128_StoreUnsafe:
                case NI_VectorT256_StoreUnsafe:
                {
                    NamedIntrinsic storeIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        storeIntrinsic = NI_AVX_Store;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        storeIntrinsic = NI_SSE2_Store;
                    }
                    else
                    {
                        storeIntrinsic = NI_SSE_Store;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_StoreAligned:
                case NI_VectorT256_StoreAligned:
                {
                    NamedIntrinsic storeIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        storeIntrinsic = NI_AVX_StoreAligned;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        storeIntrinsic = NI_SSE2_StoreAligned;
                    }
                    else
                    {
                        storeIntrinsic = NI_SSE_StoreAligned;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_StoreAlignedNonTemporal:
                case NI_VectorT256_StoreAlignedNonTemporal:
                {
                    NamedIntrinsic storeIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        storeIntrinsic = NI_AVX_StoreAlignedNonTemporal;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        storeIntrinsic = NI_SSE2_StoreAlignedNonTemporal;
                    }
                    else
                    {
                        storeIntrinsic = NI_SSE_StoreAlignedNonTemporal;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }
#elif defined(TARGET_ARM64)
                case NI_Vector2_CreateBroadcast:
                case NI_Vector3_CreateBroadcast:
                case NI_Vector4_CreateBroadcast:
                case NI_VectorT128_CreateBroadcast:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, simdSize,
                                                              /* isSimdAsHWIntrinsic */ true);
                    break;
                }

                case NI_VectorT128_Divide:
                case NI_VectorT128_op_Division:
                {
                    return gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_EqualsAll:
                case NI_VectorT128_op_Equality:
                {
                    return gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_EqualsAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_get_Item:
                case NI_VectorT128_GetElement:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_GreaterThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_GE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LT, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanOrEqualAll:
                {
                    return gtNewSimdCmpOpAllNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LessThanOrEqualAny:
                {
                    return gtNewSimdCmpOpAnyNode(GT_LE, retType, op1, op2, simdBaseJitType, simdSize,
                                                 /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_LoadUnsafeIndex:
                {
                    GenTree* tmp;

                    if (op1->OperIs(GT_CAST))
                    {
                        // Although the API specifies a pointer, if what we have is a BYREF, that's what
                        // we really want, so throw away the cast.
                        if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                        {
                            op1 = op1->gtGetOp1();
                        }
                    }

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op2->TypeGet());
                    op2 = gtNewOperNode(GT_MUL, op2->TypeGet(), op2, tmp);
                    op1 = gtNewOperNode(GT_ADD, op1->TypeGet(), op1, op2);

                    return gtNewSimdHWIntrinsicNode(retType, op1, NI_AdvSimd_LoadVector128, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Max:
                {
                    return gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Min:
                {
                    return gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize,
                                            /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Multiply:
                case NI_VectorT128_op_Multiply:
                {
                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Narrow:
                {
                    return gtNewSimdNarrowNode(retType, op1, op2, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_LeftShift:
                case NI_VectorT128_ShiftLeft:
                {
                    return gtNewSimdBinOpNode(GT_LSH, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_RightShift:
                case NI_VectorT128_ShiftRightArithmetic:
                {
                    genTreeOps op = varTypeIsUnsigned(simdBaseType) ? GT_RSZ : GT_RSH;
                    return gtNewSimdBinOpNode(op, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_op_UnsignedRightShift:
                case NI_VectorT128_ShiftRightLogical:
                {
                    return gtNewSimdBinOpNode(GT_RSZ, retType, op1, op2, simdBaseJitType, simdSize,
                                              /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Store:
                case NI_VectorT128_StoreUnsafe:
                case NI_VectorT128_StoreAligned:
                {
                    // ARM64 doesn't have aligned stores, but aligned stores are only validated to be
                    // aligned during minopts, so the earlier check skips ths intrinsic handling if
                    // we're minopts and we should otherwise treat it as a regular store

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, NI_AdvSimd_Store, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_StoreAlignedNonTemporal:
                {
                    // ARM64 has non-temporal stores (STNP) but we don't currently support them

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, NI_AdvSimd_Store, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
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

        case 3:
        {
            assert(newobjThis == nullptr);

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

            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
            op3     = getArgForHWIntrinsic(argType, argClass);

            argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2     = getArgForHWIntrinsic(argType, argClass);

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod, newobjThis);

            assert(!SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(intrinsic));

            switch (intrinsic)
            {
#if defined(TARGET_XARCH)
                case NI_VectorT128_ConditionalSelect:
                case NI_VectorT256_ConditionalSelect:
                {
                    return gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_StoreUnsafeIndex:
                case NI_VectorT256_StoreUnsafeIndex:
                {
                    GenTree* tmp;

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                    op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, tmp);
                    op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);

                    NamedIntrinsic storeIntrinsic = NI_Illegal;

                    if (simdSize == 32)
                    {
                        storeIntrinsic = NI_AVX_Store;
                    }
                    else if (simdBaseType != TYP_FLOAT)
                    {
                        storeIntrinsic = NI_SSE2_Store;
                    }
                    else
                    {
                        storeIntrinsic = NI_SSE_Store;
                    }

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, storeIntrinsic, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WithElement:
                case NI_VectorT256_WithElement:
                {
                    return gtNewSimdWithElementNode(retType, op1, op2, op3, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT128_ConditionalSelect:
                {
                    return gtNewSimdCndSelNode(retType, op1, op2, op3, simdBaseJitType, simdSize,
                                               /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_StoreUnsafeIndex:
                {
                    GenTree* tmp;

                    tmp = gtNewIconNode(genTypeSize(simdBaseType), op3->TypeGet());
                    op3 = gtNewOperNode(GT_MUL, op3->TypeGet(), op3, tmp);
                    op2 = gtNewOperNode(GT_ADD, op2->TypeGet(), op2, op3);

                    return gtNewSimdHWIntrinsicNode(retType, op2, op1, NI_AdvSimd_Store, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_WithElement:
                {
                    return gtNewSimdWithElementNode(retType, op1, op2, op3, simdBaseJitType, simdSize,
                                                    /* isSimdAsHWIntrinsic */ true);
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
        }
    }

    if (copyBlkDst != nullptr)
    {
        assert(copyBlkSrc != nullptr);

        // At this point, we have a tree that we are going to store into a destination.
        // TODO-1stClassStructs: This should be a simple store or assignment, and should not require
        // GTF_ALL_EFFECT for the dest. This is currently emulating the previous behavior of
        // block ops.

        GenTree* dest = gtNewBlockVal(copyBlkDst, simdSize);

        dest->gtType = simdType;
        dest->gtFlags |= GTF_GLOB_REF;

        GenTree* retNode = gtNewBlkOpNode(dest, copyBlkSrc, /* isVolatile */ false, /* isCopyBlock */ true);
        retNode->gtFlags |= ((copyBlkDst->gtFlags | copyBlkSrc->gtFlags) & GTF_ALL_EFFECT);

        return retNode;
    }
    assert(copyBlkSrc == nullptr);

    assert(!"Unexpected SimdAsHWIntrinsic");
    return nullptr;
}

#endif // FEATURE_HW_INTRINSICS
