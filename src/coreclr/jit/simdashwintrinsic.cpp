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

    unsigned numArgs          = sig->numArgs;
    bool     isInstanceMethod = false;

    if (sig->hasThis())
    {
        numArgs++;
        isInstanceMethod = true;
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
    if (!featureSIMD)
    {
        // We can't support SIMD intrinsics if the JIT doesn't support the feature
        return nullptr;
    }

#if defined(TARGET_XARCH)
    CORINFO_InstructionSet minimumIsa = InstructionSet_SSE2;
#elif defined(TARGET_ARM64)
    CORINFO_InstructionSet minimumIsa = InstructionSet_AdvSimd;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    if (!compOpportunisticallyDependsOn(minimumIsa) || !JitConfig.EnableHWIntrinsic())
    {
        // The user disabled support for the baseline ISA so
        // don't emit any SIMD intrinsics as they all require
        // this at a minimum
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
        argClass        = info.compCompHnd->getArgClass(sig, sig->args);
        simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(argClass, &simdSize);
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
    else if ((clsHnd == m_simdHandleCache->SIMDVectorHandle) && (numArgs != 0))
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

    assert(featureSIMD);
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

    switch (intrinsic)
    {
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
#endif // TARGET_XARCH
        case NI_VectorT128_As:
        {
            unsigned    retSimdSize;
            CorInfoType retBaseJitType = getBaseJitTypeAndSizeOfSIMDType(sig->retTypeSigClass, &retSimdSize);

            if ((retBaseJitType == CORINFO_TYPE_UNDEF) ||
                !varTypeIsArithmetic(JitType2PreciseVarType(retBaseJitType)) || (retSimdSize == 0))
            {
                // We get here if the return type is an unsupported type
                return nullptr;
            }
            break;
        }

#if defined(TARGET_XARCH)
        case NI_VectorT256_get_Item:
        case NI_VectorT128_get_Item:
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
#if defined(TARGET_XARCH)
                case NI_Vector2_get_One:
                case NI_Vector3_get_One:
                case NI_Vector4_get_One:
                case NI_VectorT128_get_One:
                case NI_VectorT256_get_One:
                {
                    switch (simdBaseType)
                    {
                        case TYP_BYTE:
                        case TYP_UBYTE:
                        case TYP_SHORT:
                        case TYP_USHORT:
                        case TYP_INT:
                        case TYP_UINT:
                        {
                            op1 = gtNewIconNode(1, TYP_INT);
                            break;
                        }

                        case TYP_LONG:
                        case TYP_ULONG:
                        {
                            op1 = gtNewLconNode(1);
                            break;
                        }

                        case TYP_FLOAT:
                        case TYP_DOUBLE:
                        {
                            op1 = gtNewDconNode(1.0, simdBaseType);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    return gtNewSimdCreateBroadcastNode(retType, op1, simdBaseJitType, simdSize,
                                                        /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_get_Count:
                case NI_VectorT256_get_Count:
                {
                    GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
                    countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
                    return countNode;
                }
#elif defined(TARGET_ARM64)
                case NI_Vector2_get_One:
                case NI_Vector3_get_One:
                case NI_Vector4_get_One:
                case NI_VectorT128_get_One:
                {
                    switch (simdBaseType)
                    {
                        case TYP_BYTE:
                        case TYP_UBYTE:
                        case TYP_SHORT:
                        case TYP_USHORT:
                        case TYP_INT:
                        case TYP_UINT:
                        {
                            op1 = gtNewIconNode(1, TYP_INT);
                            break;
                        }

                        case TYP_LONG:
                        case TYP_ULONG:
                        {
                            op1 = gtNewLconNode(1);
                            break;
                        }

                        case TYP_FLOAT:
                        case TYP_DOUBLE:
                        {
                            op1 = gtNewDconNode(1.0, simdBaseType);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    return gtNewSimdCreateBroadcastNode(retType, op1, simdBaseJitType, simdSize,
                                                        /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_get_Count:
                {
                    GenTreeIntCon* countNode = gtNewIconNode(getSIMDVectorLength(simdSize, simdBaseType), TYP_INT);
                    countNode->gtFlags |= GTF_ICON_SIMD_COUNT;
                    return countNode;
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

            bool isOpExplicit = (intrinsic == NI_VectorT128_op_Explicit) || (intrinsic == NI_VectorT128_As);

#if defined(TARGET_XARCH)
            isOpExplicit |= (intrinsic == NI_VectorT256_op_Explicit) || (intrinsic == NI_VectorT256_As);
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
                    if (varTypeIsFloating(simdBaseType))
                    {
                        // Abs(vf) = vf & new SIMDVector<float>(0x7fffffff);
                        // Abs(vd) = vf & new SIMDVector<double>(0x7fffffffffffffff);
                        GenTree* bitMask = nullptr;

                        if (simdBaseType == TYP_FLOAT)
                        {
                            static_assert_no_msg(sizeof(float) == sizeof(int));
                            int mask = 0x7fffffff;
                            bitMask  = gtNewDconNode(*((float*)&mask), TYP_FLOAT);
                        }
                        else
                        {
                            assert(simdBaseType == TYP_DOUBLE);
                            static_assert_no_msg(sizeof(double) == sizeof(__int64));

                            __int64 mask = 0x7fffffffffffffffLL;
                            bitMask      = gtNewDconNode(*((double*)&mask), TYP_DOUBLE);
                        }
                        assert(bitMask != nullptr);

                        bitMask = gtNewSimdCreateBroadcastNode(retType, bitMask, simdBaseJitType, simdSize,
                                                               /* isSimdAsHWIntrinsic */ true);

                        intrinsic = isVectorT256 ? NI_VectorT256_op_BitwiseAnd : NI_VectorT128_op_BitwiseAnd;
                        intrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);

                        return gtNewSimdAsHWIntrinsicNode(retType, op1, bitMask, intrinsic, simdBaseJitType, simdSize);
                    }
                    else if (varTypeIsUnsigned(simdBaseType))
                    {
                        return op1;
                    }
                    else if ((simdBaseType != TYP_LONG) && compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                    {
                        return gtNewSimdAsHWIntrinsicNode(retType, op1, NI_SSSE3_Abs, simdBaseJitType, simdSize);
                    }
                    else
                    {
                        GenTree*       tmp;
                        NamedIntrinsic hwIntrinsic;

                        GenTree* op1Dup1;
                        op1 = impCloneExpr(op1, &op1Dup1, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                           nullptr DEBUGARG("Clone op1 for Vector<T>.Abs"));

                        GenTree* op1Dup2;
                        op1Dup1 = impCloneExpr(op1Dup1, &op1Dup2, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                               nullptr DEBUGARG("Clone op1 for Vector<T>.Abs"));

                        // op1 = op1 < Zero
                        tmp         = gtNewSIMDVectorZero(retType, simdBaseJitType, simdSize);
                        hwIntrinsic = isVectorT256 ? NI_VectorT256_LessThan : NI_VectorT128_LessThan;
                        op1 = impSimdAsHWIntrinsicRelOp(hwIntrinsic, clsHnd, retType, simdBaseJitType, simdSize, op1,
                                                        tmp);

                        // tmp = Zero - op1Dup1
                        tmp         = gtNewSIMDVectorZero(retType, simdBaseJitType, simdSize);
                        hwIntrinsic = isVectorT256 ? NI_AVX2_Subtract : NI_SSE2_Subtract;
                        tmp = gtNewSimdAsHWIntrinsicNode(retType, tmp, op1Dup1, hwIntrinsic, simdBaseJitType, simdSize);

                        // result = ConditionalSelect(op1, tmp, op1Dup2)
                        return impSimdAsHWIntrinsicCndSel(clsHnd, retType, simdBaseJitType, simdSize, op1, tmp,
                                                          op1Dup2);
                    }
                    break;
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT128_Abs:
                {
                    assert(varTypeIsUnsigned(simdBaseType));
                    return op1;
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
            CORINFO_ARG_LIST_HANDLE arg2 = isInstanceMethod ? argList : info.compCompHnd->getArgNext(argList);
            argType                      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
            op2                          = getArgForHWIntrinsic(argType, argClass);

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1 = getArgForHWIntrinsic(argType, argClass, isInstanceMethod, newobjThis);

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

                case NI_VectorT128_get_Item:
                case NI_VectorT256_get_Item:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_Vector2_op_Division:
                case NI_Vector3_op_Division:
                {
                    // Vector2/3 div: since the top-most elements will be zero, we end up
                    // perfoming 0/0 which is a NAN. Therefore, post division we need to set the
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

                case NI_VectorT128_Dot:
                {
                    assert((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT));
                    assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));
                    return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, NI_Vector128_Dot, simdBaseJitType, simdSize);
                }

                case NI_VectorT128_Equals:
                case NI_VectorT128_GreaterThan:
                case NI_VectorT128_GreaterThanOrEqual:
                case NI_VectorT128_LessThan:
                case NI_VectorT128_LessThanOrEqual:
                case NI_VectorT256_GreaterThan:
                case NI_VectorT256_GreaterThanOrEqual:
                case NI_VectorT256_LessThan:
                case NI_VectorT256_LessThanOrEqual:
                {
                    return impSimdAsHWIntrinsicRelOp(intrinsic, clsHnd, retType, simdBaseJitType, simdSize, op1, op2);
                }

                case NI_VectorT128_Max:
                case NI_VectorT128_Min:
                case NI_VectorT256_Max:
                case NI_VectorT256_Min:
                {
                    if ((simdBaseType == TYP_BYTE) || (simdBaseType == TYP_USHORT))
                    {
                        GenTree*    constVal  = nullptr;
                        CorInfoType opJitType = simdBaseJitType;
                        var_types   opType    = simdBaseType;

                        NamedIntrinsic opIntrinsic;
                        NamedIntrinsic hwIntrinsic;

                        switch (simdBaseType)
                        {
                            case TYP_BYTE:
                            {
                                constVal        = gtNewIconNode(0x80808080, TYP_INT);
                                opIntrinsic     = NI_VectorT128_op_Subtraction;
                                simdBaseJitType = CORINFO_TYPE_UBYTE;
                                simdBaseType    = TYP_UBYTE;
                                break;
                            }

                            case TYP_USHORT:
                            {
                                constVal        = gtNewIconNode(0x80008000, TYP_INT);
                                opIntrinsic     = NI_VectorT128_op_Addition;
                                simdBaseJitType = CORINFO_TYPE_SHORT;
                                simdBaseType    = TYP_SHORT;
                                break;
                            }

                            default:
                            {
                                unreached();
                            }
                        }

                        GenTree* constVector =
                            gtNewSimdCreateBroadcastNode(retType, constVal, CORINFO_TYPE_INT, simdSize,
                                                         /* isSimdAsHWIntrinsic */ true);

                        GenTree* constVectorDup1;
                        constVector = impCloneExpr(constVector, &constVectorDup1, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                                   nullptr DEBUGARG("Clone constVector for Vector<T>.Max/Min"));

                        GenTree* constVectorDup2;
                        constVectorDup1 =
                            impCloneExpr(constVectorDup1, &constVectorDup2, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                         nullptr DEBUGARG("Clone constVector for Vector<T>.Max/Min"));

                        hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(opIntrinsic, opType);

                        // op1 = op1 - constVector
                        // -or-
                        // op1 = op1 + constVector
                        op1 = gtNewSimdAsHWIntrinsicNode(retType, op1, constVector, hwIntrinsic, opJitType, simdSize);

                        // op2 = op2 - constVectorDup1
                        // -or-
                        // op2 = op2 + constVectorDup1
                        op2 =
                            gtNewSimdAsHWIntrinsicNode(retType, op2, constVectorDup1, hwIntrinsic, opJitType, simdSize);

                        // op1 = Max(op1, op2)
                        // -or-
                        // op1 = Min(op1, op2)
                        hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);
                        op1 = gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);

                        // result = op1 + constVectorDup2
                        // -or-
                        // result = op1 - constVectorDup2
                        opIntrinsic = (opIntrinsic == NI_VectorT128_op_Subtraction) ? NI_VectorT128_op_Addition
                                                                                    : NI_VectorT128_op_Subtraction;
                        hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(opIntrinsic, opType);
                        return gtNewSimdAsHWIntrinsicNode(retType, op1, constVectorDup2, hwIntrinsic, opJitType,
                                                          simdSize);
                    }

                    GenTree* op1Dup;
                    op1 = impCloneExpr(op1, &op1Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for Vector<T>.Max/Min"));

                    GenTree* op2Dup;
                    op2 = impCloneExpr(op2, &op2Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op2 for Vector<T>.Max/Min"));

                    if ((intrinsic == NI_VectorT128_Max) || (intrinsic == NI_VectorT256_Max))
                    {
                        intrinsic = isVectorT256 ? NI_VectorT256_GreaterThan : NI_VectorT128_GreaterThan;
                    }
                    else
                    {
                        intrinsic = isVectorT256 ? NI_VectorT256_LessThan : NI_VectorT128_LessThan;
                    }

                    // op1 = op1 > op2
                    // -or-
                    // op1 = op1 < op2
                    op1 = impSimdAsHWIntrinsicRelOp(intrinsic, clsHnd, retType, simdBaseJitType, simdSize, op1, op2);

                    // result = ConditionalSelect(op1, op1Dup, op2Dup)
                    return impSimdAsHWIntrinsicCndSel(clsHnd, retType, simdBaseJitType, simdSize, op1, op1Dup, op2Dup);
                }

                case NI_VectorT128_op_Multiply:
                {
                    NamedIntrinsic hwIntrinsic = NI_Illegal;
                    GenTree**      broadcastOp = nullptr;

                    if (varTypeIsArithmetic(op1->TypeGet()))
                    {
                        broadcastOp = &op1;
                    }
                    else if (varTypeIsArithmetic(op2->TypeGet()))
                    {
                        broadcastOp = &op2;
                    }

                    if (broadcastOp != nullptr)
                    {
                        *broadcastOp = gtNewSimdCreateBroadcastNode(simdType, *broadcastOp, simdBaseJitType, simdSize,
                                                                    /* isSimdAsHWIntrinsic */ true);
                    }

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        case TYP_USHORT:
                        {
                            hwIntrinsic = NI_SSE2_MultiplyLow;
                            break;
                        }

                        case TYP_INT:
                        case TYP_UINT:
                        {
                            if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                            {
                                hwIntrinsic = NI_SSE41_MultiplyLow;
                            }
                            else
                            {
                                // op1Dup = op1
                                GenTree* op1Dup;
                                op1 = impCloneExpr(op1, &op1Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                                   nullptr DEBUGARG("Clone op1 for Vector<T>.Multiply"));

                                // op2Dup = op2
                                GenTree* op2Dup;
                                op2 = impCloneExpr(op2, &op2Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                                   nullptr DEBUGARG("Clone op2 for Vector<T>.Multiply"));

                                // op1 = Sse2.ShiftRightLogical128BitLane(op1, 4)
                                op1 = gtNewSimdAsHWIntrinsicNode(retType, op1, gtNewIconNode(4, TYP_INT),
                                                                 NI_SSE2_ShiftRightLogical128BitLane, simdBaseJitType,
                                                                 simdSize);

                                // op2 = Sse2.ShiftRightLogical128BitLane(op1, 4)
                                op2 = gtNewSimdAsHWIntrinsicNode(retType, op2, gtNewIconNode(4, TYP_INT),
                                                                 NI_SSE2_ShiftRightLogical128BitLane, simdBaseJitType,
                                                                 simdSize);

                                // op2 = Sse2.Multiply(op2.AsUInt64(), op1.AsUInt64()).AsInt32()
                                op2 = gtNewSimdAsHWIntrinsicNode(retType, op2, op1, NI_SSE2_Multiply,
                                                                 CORINFO_TYPE_ULONG, simdSize);

                                // op2 = Sse2.Shuffle(op2, (0, 0, 2, 0))
                                op2 = gtNewSimdAsHWIntrinsicNode(retType, op2, gtNewIconNode(SHUFFLE_XXZX, TYP_INT),
                                                                 NI_SSE2_Shuffle, simdBaseJitType, simdSize);

                                // op1 = Sse2.Multiply(op1Dup.AsUInt64(), op2Dup.AsUInt64()).AsInt32()
                                op1 = gtNewSimdAsHWIntrinsicNode(retType, op1Dup, op2Dup, NI_SSE2_Multiply,
                                                                 CORINFO_TYPE_ULONG, simdSize);

                                // op1 = Sse2.Shuffle(op1, (0, 0, 2, 0))
                                op1 = gtNewSimdAsHWIntrinsicNode(retType, op1, gtNewIconNode(SHUFFLE_XXZX, TYP_INT),
                                                                 NI_SSE2_Shuffle, simdBaseJitType, simdSize);

                                // result = Sse2.UnpackLow(op1, op2)
                                hwIntrinsic = NI_SSE2_UnpackLow;
                            }
                            break;
                        }

                        case TYP_FLOAT:
                        {
                            hwIntrinsic = NI_SSE_Multiply;
                            break;
                        }

                        case TYP_DOUBLE:
                        {
                            hwIntrinsic = NI_SSE2_Multiply;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    assert(hwIntrinsic != NI_Illegal);
                    return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
                }

                case NI_VectorT256_op_Multiply:
                {
                    NamedIntrinsic hwIntrinsic = NI_Illegal;
                    GenTree**      broadcastOp = nullptr;

                    if (varTypeIsArithmetic(op1->TypeGet()))
                    {
                        broadcastOp = &op1;
                    }
                    else if (varTypeIsArithmetic(op2->TypeGet()))
                    {
                        broadcastOp = &op2;
                    }

                    if (broadcastOp != nullptr)
                    {
                        *broadcastOp = gtNewSimdCreateBroadcastNode(simdType, *broadcastOp, simdBaseJitType, simdSize,
                                                                    /* isSimdAsHWIntrinsic */ true);
                    }

                    switch (simdBaseType)
                    {
                        case TYP_SHORT:
                        case TYP_USHORT:
                        case TYP_INT:
                        case TYP_UINT:
                        {
                            hwIntrinsic = NI_AVX2_MultiplyLow;
                            break;
                        }

                        case TYP_FLOAT:
                        case TYP_DOUBLE:
                        {
                            hwIntrinsic = NI_AVX_Multiply;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    assert(hwIntrinsic != NI_Illegal);
                    return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
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

                case NI_VectorT128_get_Item:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize,
                                                   /* isSimdAsHWIntrinsic */ true);
                }

                case NI_VectorT128_Max:
                case NI_VectorT128_Min:
                {
                    assert((simdBaseType == TYP_LONG) || (simdBaseType == TYP_ULONG));

                    NamedIntrinsic hwIntrinsic;

                    GenTree* op1Dup;
                    op1 = impCloneExpr(op1, &op1Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for Vector<T>.Max/Min"));

                    GenTree* op2Dup;
                    op2 = impCloneExpr(op2, &op2Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op2 for Vector<T>.Max/Min"));

                    intrinsic = (intrinsic == NI_VectorT128_Max) ? NI_VectorT128_GreaterThan : NI_VectorT128_LessThan;

                    // op1 = op1 > op2
                    // -or-
                    // op1 = op1 < op2
                    hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);
                    op1         = gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);

                    // result = ConditionalSelect(op1, op1Dup, op2Dup)
                    return impSimdAsHWIntrinsicCndSel(clsHnd, retType, simdBaseJitType, simdSize, op1, op1Dup, op2Dup);
                }

                case NI_VectorT128_op_Multiply:
                {
                    NamedIntrinsic hwIntrinsic     = NI_Illegal;
                    NamedIntrinsic scalarIntrinsic = NI_Illegal;
                    GenTree**      scalarOp        = nullptr;

                    if (varTypeIsArithmetic(op1->TypeGet()))
                    {
                        // MultiplyByScalar requires the scalar op to be op2
                        std::swap(op1, op2);

                        scalarOp = &op2;
                    }
                    else if (varTypeIsArithmetic(op2->TypeGet()))
                    {
                        scalarOp = &op2;
                    }

                    switch (simdBaseType)
                    {
                        case TYP_BYTE:
                        case TYP_UBYTE:
                        {
                            if (scalarOp != nullptr)
                            {
                                *scalarOp = gtNewSimdCreateBroadcastNode(simdType, *scalarOp, simdBaseJitType, simdSize,
                                                                         /* isSimdAsHWIntrinsic */ true);
                            }

                            hwIntrinsic = NI_AdvSimd_Multiply;
                            break;
                        }

                        case TYP_SHORT:
                        case TYP_USHORT:
                        case TYP_INT:
                        case TYP_UINT:
                        case TYP_FLOAT:
                        {
                            if (scalarOp != nullptr)
                            {
                                hwIntrinsic = NI_AdvSimd_MultiplyByScalar;
                                *scalarOp =
                                    gtNewSimdAsHWIntrinsicNode(TYP_SIMD8, *scalarOp, NI_Vector64_CreateScalarUnsafe,
                                                               simdBaseJitType, 8);
                            }
                            else
                            {
                                hwIntrinsic = NI_AdvSimd_Multiply;
                            }
                            break;
                        }

                        case TYP_DOUBLE:
                        {
                            if (scalarOp != nullptr)
                            {
                                hwIntrinsic = NI_AdvSimd_Arm64_MultiplyByScalar;
                                *scalarOp   = gtNewSimdAsHWIntrinsicNode(TYP_SIMD8, *scalarOp, NI_Vector64_Create,
                                                                       simdBaseJitType, 8);
                            }
                            else
                            {
                                hwIntrinsic = NI_AdvSimd_Arm64_Multiply;
                            }
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    assert(hwIntrinsic != NI_Illegal);
                    return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
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
                    return impSimdAsHWIntrinsicCndSel(clsHnd, retType, simdBaseJitType, simdSize, op1, op2, op3);
                }
#elif defined(TARGET_ARM64)
                case NI_VectorT128_ConditionalSelect:
                {
                    return impSimdAsHWIntrinsicCndSel(clsHnd, retType, simdBaseJitType, simdSize, op1, op2, op3);
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

//------------------------------------------------------------------------
// impSimdAsHWIntrinsicCndSel: Import a SIMD conditional select intrinsic
//
// Arguments:
//    clsHnd          -- class handle containing the intrinsic function.
//    retType         -- the return type of the intrinsic call
//    simdBaseJitType -- the base JIT type of SIMD type of the intrinsic
//    simdSize        -- the size of the SIMD type of the intrinsic
//    op1             -- the first operand of the intrinsic
//    op2             -- the second operand of the intrinsic
//    op3             -- the third operand of the intrinsic
//
// Return Value:
//    The GT_HWINTRINSIC node representing the conditional select
//
GenTree* Compiler::impSimdAsHWIntrinsicCndSel(CORINFO_CLASS_HANDLE clsHnd,
                                              var_types            retType,
                                              CorInfoType          simdBaseJitType,
                                              unsigned             simdSize,
                                              GenTree*             op1,
                                              GenTree*             op2,
                                              GenTree*             op3)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

    assert(featureSIMD);
    assert(retType != TYP_UNKNOWN);
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);
    assert(varTypeIsSIMD(getSIMDTypeForSize(simdSize)));
    assert(op1 != nullptr);
    assert(op2 != nullptr);
    assert(op3 != nullptr);

#if defined(TARGET_XARCH)
    // Vector<T> for the rel-ops covered here requires at least SSE2
    assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));

    // Vector<T>, when 32-bytes, requires at least AVX2
    assert((simdSize != 32) || compIsaSupportedDebugOnly(InstructionSet_AVX2));

    NamedIntrinsic hwIntrinsic;

    GenTree* op1Dup;
    op1 = impCloneExpr(op1, &op1Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                       nullptr DEBUGARG("Clone op1 for Vector<T>.ConditionalSelect"));

    // op2 = op2 & op1
    hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_op_BitwiseAnd, simdBaseType);
    op2         = gtNewSimdAsHWIntrinsicNode(retType, op2, op1, hwIntrinsic, simdBaseJitType, simdSize);

    // op3 = op3 & ~op1Dup
    hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_AndNot, simdBaseType);

    if (SimdAsHWIntrinsicInfo::NeedsOperandsSwapped(NI_VectorT128_AndNot))
    {
        std::swap(op3, op1Dup);
    }

    op3 = gtNewSimdAsHWIntrinsicNode(retType, op3, op1Dup, hwIntrinsic, simdBaseJitType, simdSize);

    // result = op2 | op3
    hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_op_BitwiseOr, simdBaseType);
    return gtNewSimdAsHWIntrinsicNode(retType, op2, op3, hwIntrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, op3, NI_AdvSimd_BitwiseSelect, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

#if defined(TARGET_XARCH)
//------------------------------------------------------------------------
// impSimdAsHWIntrinsicRelOp: Import a SIMD relational operator intrinsic
//
// Arguments:
//    intrinsic       -- id of the intrinsic function.
//    clsHnd          -- class handle containing the intrinsic function.
//    retType         -- the return type of the intrinsic call
//    simdBaseJitType -- the base JIT type of SIMD type of the intrinsic
//    simdSize        -- the size of the SIMD type of the intrinsic
//    op1             -- the first operand of the intrinsic
//    op2             -- the second operand of the intrinsic
//
// Return Value:
//    The GT_HWINTRINSIC node representing the relational operator
//
GenTree* Compiler::impSimdAsHWIntrinsicRelOp(NamedIntrinsic       intrinsic,
                                             CORINFO_CLASS_HANDLE clsHnd,
                                             var_types            retType,
                                             CorInfoType          simdBaseJitType,
                                             unsigned             simdSize,
                                             GenTree*             op1,
                                             GenTree*             op2)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);

    assert(featureSIMD);
    assert(retType != TYP_UNKNOWN);
    assert(varTypeIsIntegral(simdBaseType));
    assert(simdSize != 0);
    assert(varTypeIsSIMD(getSIMDTypeForSize(simdSize)));
    assert(op1 != nullptr);
    assert(op2 != nullptr);
    assert(!SimdAsHWIntrinsicInfo::IsInstanceMethod(intrinsic));

    bool isVectorT256 = (SimdAsHWIntrinsicInfo::lookupClassId(intrinsic) == SimdAsHWIntrinsicClassId::VectorT256);

    // Vector<T> for the rel-ops covered here requires at least SSE2
    assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));

    // Vector<T>, when 32-bytes, requires at least AVX2
    assert(!isVectorT256 || compIsaSupportedDebugOnly(InstructionSet_AVX2));

    switch (intrinsic)
    {
        case NI_VectorT128_Equals:
        case NI_VectorT256_Equals:
        {
            // These ones aren't "special", but they are used by the other
            // relational operators and so are defined for convenience.

            NamedIntrinsic hwIntrinsic = NI_Illegal;

            if (isVectorT256 || ((simdBaseType != TYP_LONG) && (simdBaseType != TYP_ULONG)))
            {
                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);
                assert(hwIntrinsic != intrinsic);
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                hwIntrinsic = NI_SSE41_CompareEqual;
            }
            else
            {
                // There is no direct SSE2 support for comparing TYP_LONG vectors.
                // These have to be implemented in terms of TYP_INT vector comparison operations.
                //
                // tmp = (op1 == op2) i.e. compare for equality as if op1 and op2 are Vector<int>
                // op1 = tmp
                // op2 = Shuffle(tmp, (2, 3, 0, 1))
                // result = BitwiseAnd(op1, op2)
                //
                // Shuffle is meant to swap the comparison results of low-32-bits and high 32-bits of
                // respective long elements.

                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, TYP_INT);
                assert(hwIntrinsic != intrinsic);

                GenTree* tmp = gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, CORINFO_TYPE_INT, simdSize);

                tmp = impCloneExpr(tmp, &op1, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone tmp for Vector<T>.Equals"));

                op2 = gtNewSimdAsHWIntrinsicNode(retType, tmp, gtNewIconNode(SHUFFLE_ZWXY, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);

                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_op_BitwiseAnd, simdBaseType);
                assert(hwIntrinsic != NI_VectorT128_op_BitwiseAnd);
            }
            assert(hwIntrinsic != NI_Illegal);

            return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case NI_VectorT128_GreaterThanOrEqual:
        case NI_VectorT128_LessThanOrEqual:
        case NI_VectorT256_GreaterThanOrEqual:
        case NI_VectorT256_LessThanOrEqual:
        {
            // There is no direct support for doing a combined comparison and equality for integral types.
            // These have to be implemented by performing both halves and combining their results.
            //
            // op1Dup = op1
            // op2Dup = op2
            //
            // op1 = GreaterThan(op1, op2)
            // op2 = Equals(op1Dup, op2Dup)
            //
            // result = BitwiseOr(op1, op2)
            //
            // Where the GreaterThan(op1, op2) comparison could also be LessThan(op1, op2)

            GenTree* op1Dup;
            op1 = impCloneExpr(op1, &op1Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                               nullptr DEBUGARG("Clone op1 for Vector<T>.GreaterThanOrEqual/LessThanOrEqual"));

            GenTree* op2Dup;
            op2 = impCloneExpr(op2, &op2Dup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                               nullptr DEBUGARG("Clone op2 for Vector<T>.GreaterThanOrEqual/LessThanOrEqual"));

            NamedIntrinsic eqIntrinsic = isVectorT256 ? NI_VectorT256_Equals : NI_VectorT128_Equals;

            switch (intrinsic)
            {
                case NI_VectorT128_GreaterThanOrEqual:
                {
                    intrinsic = NI_VectorT128_GreaterThan;
                    break;
                }

                case NI_VectorT128_LessThanOrEqual:
                {
                    intrinsic = NI_VectorT128_LessThan;
                    break;
                }

                case NI_VectorT256_GreaterThanOrEqual:
                {
                    intrinsic = NI_VectorT256_GreaterThan;
                    break;
                }

                case NI_VectorT256_LessThanOrEqual:
                {
                    intrinsic = NI_VectorT256_LessThan;
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            op1 = impSimdAsHWIntrinsicRelOp(eqIntrinsic, clsHnd, retType, simdBaseJitType, simdSize, op1, op2);
            op2 = impSimdAsHWIntrinsicRelOp(intrinsic, clsHnd, retType, simdBaseJitType, simdSize, op1Dup, op2Dup);
            intrinsic = isVectorT256 ? NI_VectorT256_op_BitwiseOr : NI_VectorT128_op_BitwiseOr;

            NamedIntrinsic hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);
            return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
        }

        case NI_VectorT128_GreaterThan:
        case NI_VectorT128_LessThan:
        case NI_VectorT256_GreaterThan:
        case NI_VectorT256_LessThan:
        {
            NamedIntrinsic hwIntrinsic = NI_Illegal;

            if (varTypeIsUnsigned(simdBaseType))
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

                GenTree*    constVal        = nullptr;
                CorInfoType opJitType       = simdBaseJitType;
                var_types   opType          = simdBaseType;
                CorInfoType constValJitType = CORINFO_TYPE_INT;

                switch (simdBaseType)
                {
                    case TYP_UBYTE:
                    {
                        constVal        = gtNewIconNode(0x80808080, TYP_INT);
                        simdBaseJitType = CORINFO_TYPE_BYTE;
                        simdBaseType    = TYP_BYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        constVal        = gtNewIconNode(0x80008000, TYP_INT);
                        simdBaseJitType = CORINFO_TYPE_SHORT;
                        simdBaseType    = TYP_SHORT;
                        break;
                    }

                    case TYP_UINT:
                    {
                        constVal        = gtNewIconNode(0x80000000, TYP_INT);
                        simdBaseJitType = CORINFO_TYPE_INT;
                        simdBaseType    = TYP_INT;
                        break;
                    }

                    case TYP_ULONG:
                    {
                        constVal        = gtNewLconNode(0x8000000000000000);
                        constValJitType = CORINFO_TYPE_LONG;
                        simdBaseJitType = CORINFO_TYPE_LONG;
                        simdBaseType    = TYP_LONG;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                GenTree* constVector = gtNewSimdCreateBroadcastNode(retType, constVal, constValJitType, simdSize,
                                                                    /* isSimdAsHWIntrinsic */ true);

                GenTree* constVectorDup;
                constVector = impCloneExpr(constVector, &constVectorDup, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                           nullptr DEBUGARG("Clone constVector for Vector<T>.GreaterThan/LessThan"));

                NamedIntrinsic hwIntrinsic = isVectorT256 ? NI_AVX2_Subtract : NI_SSE2_Subtract;

                // op1 = op1 - constVector
                op1 = gtNewSimdAsHWIntrinsicNode(retType, op1, constVector, hwIntrinsic, opJitType, simdSize);

                // op2 = op2 - constVector
                op2 = gtNewSimdAsHWIntrinsicNode(retType, op2, constVectorDup, hwIntrinsic, opJitType, simdSize);
            }

            // This should have been mutated by the above path
            assert(varTypeIsIntegral(simdBaseType) && !varTypeIsUnsigned(simdBaseType));

            if (isVectorT256 || (simdBaseType != TYP_LONG))
            {
                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(intrinsic, simdBaseType);
                assert(hwIntrinsic != intrinsic);
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_SSE42))
            {
                hwIntrinsic =
                    (intrinsic == NI_VectorT128_GreaterThan) ? NI_SSE42_CompareGreaterThan : NI_SSE42_CompareLessThan;
            }
            else
            {
                // There is no direct SSE2 support for comparing TYP_LONG vectors.
                // These have to be implemented in terms of TYP_INT vector comparison operations.
                //
                // Let us consider the case of single long element comparison.
                // Say op1 = (x1, y1) and op2 = (x2, y2) where x1, y1, x2, and y2 are 32-bit integers that comprise the
                // longs op1 and op2.
                //
                // GreaterThan(op1, op2) can be expressed in terms of > relationship between 32-bit integers that
                // comprise op1 and op2 as
                //                    =  (x1, y1) > (x2, y2)
                //                    =  (x1 > x2) || [(x1 == x2) && (y1 > y2)]   - eq (1)
                //
                // op1Dup1 = op1
                // op1Dup2 = op1Dup1
                // op2Dup1 = op2
                // op2Dup2 = op2Dup1
                //
                // t = (op1 > op2)                - 32-bit signed comparison
                // u = (op1Dup1 == op2Dup1)       - 32-bit equality comparison
                // v = (op1Dup2 > op2Dup2)        - 32-bit unsigned comparison
                //
                // op1 = Shuffle(t, (3, 3, 1, 1)) - This corresponds to (x1 > x2) in eq(1) above
                // v = Shuffle(v, (2, 2, 0, 0))   - This corresponds to (y1 > y2) in eq(1) above
                // u = Shuffle(u, (3, 3, 1, 1))   - This corresponds to (x1 == x2) in eq(1) above
                // op2 = BitwiseAnd(v, u)         - This corresponds to [(x1 == x2) && (y1 > y2)] in eq(1) above
                //
                // result = BitwiseOr(op1, op2)

                GenTree* op1Dup1;
                op1 = impCloneExpr(op1, &op1Dup1, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone op1 for Vector<T>.GreaterThan/LessThan"));

                GenTree* op1Dup2;
                op1Dup1 = impCloneExpr(op1Dup1, &op1Dup2, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op1 for Vector<T>.GreaterThan/LessThan"));

                GenTree* op2Dup1;
                op2 = impCloneExpr(op2, &op2Dup1, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                   nullptr DEBUGARG("Clone op2 for Vector<T>.GreaterThan/LessThan"));

                GenTree* op2Dup2;
                op2Dup1 = impCloneExpr(op2Dup1, &op2Dup2, clsHnd, (unsigned)CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("Clone op2 Vector<T>.GreaterThan/LessThan"));

                GenTree* t =
                    impSimdAsHWIntrinsicRelOp(intrinsic, clsHnd, retType, CORINFO_TYPE_INT, simdSize, op1, op2);
                GenTree* u = impSimdAsHWIntrinsicRelOp(NI_VectorT128_Equals, clsHnd, retType, CORINFO_TYPE_INT,
                                                       simdSize, op1Dup1, op2Dup1);
                GenTree* v = impSimdAsHWIntrinsicRelOp(intrinsic, clsHnd, retType, CORINFO_TYPE_UINT, simdSize, op1Dup2,
                                                       op2Dup2);

                op1 = gtNewSimdAsHWIntrinsicNode(retType, t, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);

                v = gtNewSimdAsHWIntrinsicNode(retType, v, gtNewIconNode(SHUFFLE_ZZXX, TYP_INT), NI_SSE2_Shuffle,
                                               CORINFO_TYPE_INT, simdSize);
                u = gtNewSimdAsHWIntrinsicNode(retType, u, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                               CORINFO_TYPE_INT, simdSize);

                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_op_BitwiseAnd, simdBaseType);
                op2         = gtNewSimdAsHWIntrinsicNode(retType, v, u, hwIntrinsic, simdBaseJitType, simdSize);

                hwIntrinsic = SimdAsHWIntrinsicInfo::lookupHWIntrinsic(NI_VectorT128_op_BitwiseOr, simdBaseType);
            }
            assert(hwIntrinsic != NI_Illegal);

            return gtNewSimdAsHWIntrinsicNode(retType, op1, op2, hwIntrinsic, simdBaseJitType, simdSize);
        }

        default:
        {
            assert(!"Unexpected SimdAsHWIntrinsic");
            return nullptr;
        }
    }
}
#endif // TARGET_XARCH

#endif // FEATURE_HW_INTRINSICS
