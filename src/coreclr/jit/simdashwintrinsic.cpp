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
                                        GenTree*              newobjThis,
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
        return impSimdAsHWIntrinsicSpecial(intrinsic, clsHnd, sig, retType, simdBaseJitType, simdSize, newobjThis,
                                           mustExpand);
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
            op1     = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

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
            op1     = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

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
                                               GenTree*             newobjThis,
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

    switch (intrinsic)
    {
        case NI_Vector2_MultiplyAddEstimate:
        case NI_Vector3_MultiplyAddEstimate:
        {
            if (BlockNonDeterministicIntrinsics(mustExpand))
            {
                return nullptr;
            }
            break;
        }

#if defined(TARGET_XARCH)
        case NI_Vector2_WithElement:
        case NI_Vector3_WithElement:
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
        case NI_Vector2_WithElement:
        case NI_Vector3_WithElement:
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

        case NI_Vector2_FusedMultiplyAdd:
        case NI_Vector3_FusedMultiplyAdd:
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
        case 1:
        {
            assert(newobjThis == nullptr);

            argType = isInstanceMethod ? simdType
                                       : JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
            op1     = getArgForHWIntrinsic(argType, argClass, isInstanceMethod);

            switch (intrinsic)
            {
                case NI_Vector2_Abs:
                case NI_Vector3_Abs:
                {
                    return gtNewSimdAbsNode(retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Vector2_op_UnaryNegation:
                case NI_Vector3_op_UnaryNegation:
                {
                    return gtNewSimdUnOpNode(GT_NEG, retType, op1, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Sqrt:
                case NI_Vector3_Sqrt:
                {
                    return gtNewSimdSqrtNode(retType, op1, simdBaseJitType, simdSize);
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
                case NI_Vector2_op_Addition:
                case NI_Vector3_op_Addition:
                {
                    return gtNewSimdBinOpNode(GT_ADD, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_CreateBroadcast:
                case NI_Vector3_CreateBroadcast:
                {
                    assert(retType == TYP_VOID);

                    copyBlkDst = op1;
                    copyBlkSrc = gtNewSimdCreateBroadcastNode(simdType, op2, simdBaseJitType, simdSize);
                    break;
                }

                case NI_Vector2_op_Division:
                case NI_Vector3_op_Division:
                {
                    return gtNewSimdBinOpNode(GT_DIV, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Dot:
                case NI_Vector3_Dot:
                {
                    op1 = gtNewSimdDotProdNode(simdType, op1, op2, simdBaseJitType, simdSize);
                    return gtNewSimdGetElementNode(retType, op1, gtNewIconNode(0), simdBaseJitType, simdSize);
                }

                case NI_Vector2_op_Equality:
                case NI_Vector3_op_Equality:
                {
                    return gtNewSimdCmpOpAllNode(GT_EQ, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_GetElement:
                case NI_Vector3_GetElement:
                {
                    return gtNewSimdGetElementNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_op_Inequality:
                case NI_Vector3_op_Inequality:
                {
                    return gtNewSimdCmpOpAnyNode(GT_NE, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Max:
                case NI_Vector3_Max:
                {
                    return gtNewSimdMaxNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Min:
                case NI_Vector3_Min:
                {
                    return gtNewSimdMinNode(retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_op_Multiply:
                case NI_Vector3_op_Multiply:
                {
                    return gtNewSimdBinOpNode(GT_MUL, retType, op1, op2, simdBaseJitType, simdSize);
                }

                case NI_Vector2_op_Subtraction:
                case NI_Vector3_op_Subtraction:
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
                case NI_Vector2_FusedMultiplyAdd:
                case NI_Vector3_FusedMultiplyAdd:
                {
                    return gtNewSimdFmaNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                }

                case NI_Vector2_Lerp:
                case NI_Vector3_Lerp:
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

                case NI_Vector2_MultiplyAddEstimate:
                case NI_Vector3_MultiplyAddEstimate:
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

                case NI_Vector3_CreateFromVector2:
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

                case NI_Vector2_WithElement:
                case NI_Vector3_WithElement:
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

                default:
                {
                    // Some platforms warn about unhandled switch cases
                    // We handle it more generally via the assert and nullptr return below.
                    break;
                }
            }
            break;
        }

        default:
        {
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
