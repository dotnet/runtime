// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hwintrinsic.h"

#ifdef FEATURE_HW_INTRINSICS

static const HWIntrinsicInfo hwIntrinsicInfoArray[] = {
// clang-format off
#if defined(_TARGET_XARCH_)
#define HARDWARE_INTRINSIC(id, name, isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    {NI_##id, name, InstructionSet_##isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, static_cast<HWIntrinsicFlag>(flag)},
#include "hwintrinsiclistxarch.h"
#elif defined (_TARGET_ARM64_)
#define HARDWARE_INTRINSIC(isa, name, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    {NI_##isa##_##name, #name, InstructionSet_##isa, ival, static_cast<unsigned>(size), numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, static_cast<HWIntrinsicFlag>(flag)},
#include "hwintrinsiclistarm64.h"
#else
#error Unsupported platform
#endif
    // clang-format on
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

CORINFO_CLASS_HANDLE Compiler::gtGetStructHandleForHWSIMD(var_types simdType, var_types simdBaseType)
{
    if (simdType == TYP_SIMD16)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return m_simdHandleCache->Vector128FloatHandle;
            case TYP_DOUBLE:
                return m_simdHandleCache->Vector128DoubleHandle;
            case TYP_INT:
                return m_simdHandleCache->Vector128IntHandle;
            case TYP_USHORT:
                return m_simdHandleCache->Vector128UShortHandle;
            case TYP_UBYTE:
                return m_simdHandleCache->Vector128UByteHandle;
            case TYP_SHORT:
                return m_simdHandleCache->Vector128ShortHandle;
            case TYP_BYTE:
                return m_simdHandleCache->Vector128ByteHandle;
            case TYP_LONG:
                return m_simdHandleCache->Vector128LongHandle;
            case TYP_UINT:
                return m_simdHandleCache->Vector128UIntHandle;
            case TYP_ULONG:
                return m_simdHandleCache->Vector128ULongHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }
#ifdef _TARGET_XARCH_
    else if (simdType == TYP_SIMD32)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return m_simdHandleCache->Vector256FloatHandle;
            case TYP_DOUBLE:
                return m_simdHandleCache->Vector256DoubleHandle;
            case TYP_INT:
                return m_simdHandleCache->Vector256IntHandle;
            case TYP_USHORT:
                return m_simdHandleCache->Vector256UShortHandle;
            case TYP_UBYTE:
                return m_simdHandleCache->Vector256UByteHandle;
            case TYP_SHORT:
                return m_simdHandleCache->Vector256ShortHandle;
            case TYP_BYTE:
                return m_simdHandleCache->Vector256ByteHandle;
            case TYP_LONG:
                return m_simdHandleCache->Vector256LongHandle;
            case TYP_UINT:
                return m_simdHandleCache->Vector256UIntHandle;
            case TYP_ULONG:
                return m_simdHandleCache->Vector256ULongHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }
#endif // _TARGET_XARCH_
#ifdef _TARGET_ARM64_
    else if (simdType == TYP_SIMD8)
    {
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return m_simdHandleCache->Vector64FloatHandle;
            case TYP_INT:
                return m_simdHandleCache->Vector64IntHandle;
            case TYP_USHORT:
                return m_simdHandleCache->Vector64UShortHandle;
            case TYP_UBYTE:
                return m_simdHandleCache->Vector64UByteHandle;
            case TYP_SHORT:
                return m_simdHandleCache->Vector64ShortHandle;
            case TYP_BYTE:
                return m_simdHandleCache->Vector64ByteHandle;
            case TYP_UINT:
                return m_simdHandleCache->Vector64UIntHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
    }
#endif // _TARGET_ARM64_

    return NO_CLASS_HANDLE;
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
NamedIntrinsic HWIntrinsicInfo::lookupId(Compiler*   comp,
                                         const char* className,
                                         const char* methodName,
                                         const char* enclosingClassName)
{
    // TODO-Throughput: replace sequential search by binary search
    InstructionSet isa = lookupIsa(className, enclosingClassName);

    if (isa == InstructionSet_ILLEGAL)
    {
        return NI_Illegal;
    }

    bool isIsaSupported = comp->compSupports(isa) && comp->compSupportsHWIntrinsic(isa);

    if (strcmp(methodName, "get_IsSupported") == 0)
    {
        return isIsaSupported ? NI_IsSupported_True : NI_IsSupported_False;
    }
    else if (!isIsaSupported)
    {
        return NI_Throw_PlatformNotSupportedException;
    }

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
    assert(node != nullptr);

    NamedIntrinsic id  = node->gtHWIntrinsicId;
    GenTree*       op1 = node->gtGetOp1();

    if (op1 == nullptr)
    {
        return nullptr;
    }

    if (op1->OperIsList())
    {
        GenTreeArgList* list = op1->AsArgList();
        GenTree*        last;

        do
        {
            last = list->Current();
            list = list->Rest();
        } while (list != nullptr);

        return last;
    }

    GenTree* op2 = node->gtGetOp2();

    return (op2 == nullptr) ? op1 : op2;
}

//------------------------------------------------------------------------
// isImmOp: Checks whether the HWIntrinsic node has an imm operand
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
// // getArgForHWIntrinsic: pop an argument from the stack and validate its type
//
// Arguments:
//    argType   -- the required type of argument
//    argClass  -- the class handle of argType
//
// Return Value:
//     the validated argument
//
GenTree* Compiler::getArgForHWIntrinsic(var_types argType, CORINFO_CLASS_HANDLE argClass)
{
    GenTree* arg = nullptr;
    if (argType == TYP_STRUCT)
    {
        unsigned int argSizeBytes;
        var_types    base = getBaseTypeAndSizeOfSIMDType(argClass, &argSizeBytes);
        argType           = getSIMDTypeForSize(argSizeBytes);
        assert((argType == TYP_SIMD8) || (argType == TYP_SIMD16) || (argType == TYP_SIMD32));
        arg = impSIMDPopStack(argType);
        assert((arg->TypeGet() == TYP_SIMD8) || (arg->TypeGet() == TYP_SIMD16) || (arg->TypeGet() == TYP_SIMD32));
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
// addRangeCheckIfNeeded: add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic
//
// Arguments:
//    intrinsic  -- intrinsic ID
//    immOP      -- the last operand of the intrinsic that points to the imm-arg
//    mustExpand -- true if the compiler is compiling the fallback(GT_CALL) of this intrinsics
//
// Return Value:
//     add a GT_HW_INTRINSIC_CHK node for non-full-range imm-intrinsic, which would throw ArgumentOutOfRangeException
//     when the imm-argument is not in the valid range
//
GenTree* Compiler::addRangeCheckIfNeeded(NamedIntrinsic intrinsic, GenTree* immOp, bool mustExpand)
{
    assert(immOp != nullptr);
    // Full-range imm-intrinsics do not need the range-check
    // because the imm-parameter of the intrinsic method is a byte.
    // AVX2 Gather intrinsics no not need the range-check
    // because their imm-parameter have discrete valid values that are handle by managed code
    if (mustExpand && !HWIntrinsicInfo::HasFullRangeImm(intrinsic) && HWIntrinsicInfo::isImmOp(intrinsic, immOp)
#ifdef _TARGET_XARCH_
        && !HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic)
#endif
            )
    {
        assert(!immOp->IsCnsIntOrI());
        GenTree* upperBoundNode = gtNewIconNode(HWIntrinsicInfo::lookupImmUpperBound(intrinsic), TYP_INT);
        GenTree* index          = nullptr;
        if ((immOp->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            index = fgInsertCommaFormTemp(&immOp);
        }
        else
        {
            index = gtCloneExpr(immOp);
        }
        GenTreeBoundsChk* hwIntrinsicChk = new (this, GT_HW_INTRINSIC_CHK)
            GenTreeBoundsChk(GT_HW_INTRINSIC_CHK, TYP_VOID, index, upperBoundNode, SCK_RNGCHK_FAIL);
        hwIntrinsicChk->gtThrowKind = SCK_ARG_RNG_EXCPN;
        return gtNewOperNode(GT_COMMA, immOp->TypeGet(), hwIntrinsicChk, immOp);
    }
    else
    {
        return immOp;
    }
}

//------------------------------------------------------------------------
// compSupportsHWIntrinsic: check whether a given instruction set is supported
//
// Arguments:
//    isa - Instruction set
//
// Return Value:
//    true iff the given instruction set is supported in the current compilation.
bool Compiler::compSupportsHWIntrinsic(InstructionSet isa)
{
    return JitConfig.EnableHWIntrinsic() && (featureSIMD || HWIntrinsicInfo::isScalarIsa(isa)) &&
           (
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
    return (category != HW_Category_Special) && (category != HW_Category_Scalar) &&
           HWIntrinsicInfo::RequiresCodegen(intrinsicId) && !HWIntrinsicInfo::HasSpecialImport(intrinsicId);
}

//------------------------------------------------------------------------
// impHWIntrinsic: Import a hardware intrinsic as a GT_HWINTRINSIC node if possible
//
// Arguments:
//    intrinsic -- id of the intrinsic function.
//    method    -- method handle of the intrinsic function.
//    sig       -- signature of the intrinsic call
//
// Return Value:
//    The GT_HWINTRINSIC node, or nullptr if not a supported intrinsic
//
GenTree* Compiler::impHWIntrinsic(NamedIntrinsic        intrinsic,
                                  CORINFO_CLASS_HANDLE  clsHnd,
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

    // NOTE: The following code assumes that for all intrinsics
    // taking an immediate operand, that operand will be last.
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

    if (HWIntrinsicInfo::IsFloatingPointUsed(intrinsic))
    {
        // Set `compFloatingPointUsed` to cover the scenario where an intrinsic is operating on SIMD fields, but
        // where no SIMD local vars are in use. This is the same logic as is used for FEATURE_SIMD.
        compFloatingPointUsed = true;
    }

    // table-driven importer of simple intrinsics
    if (impIsTableDrivenHWIntrinsic(intrinsic, category))
    {
        if ((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromFirstArg(intrinsic) ||
            HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
        {
            CORINFO_ARG_LIST_HANDLE arg = sig->args;

            if ((category == HW_Category_MemoryStore) || HWIntrinsicInfo::BaseTypeFromSecondArg(intrinsic))
            {
                arg = info.compCompHnd->getArgNext(arg);
            }

            CORINFO_CLASS_HANDLE argClass = info.compCompHnd->getArgClass(sig, arg);
            baseType                      = getBaseTypeAndSizeOfSIMDType(argClass);

            if (baseType == TYP_UNKNOWN) // the argument is not a vector
            {
                CORINFO_CLASS_HANDLE tmpClass;
                CorInfoType          corInfoType = strip(info.compCompHnd->getArgType(sig, arg, &tmpClass));

                if (corInfoType == CORINFO_TYPE_PTR)
                {
                    corInfoType = info.compCompHnd->getChildType(argClass, &tmpClass);
                }

                baseType = JITtype2varType(corInfoType);
            }

            assert(baseType != TYP_UNKNOWN);
        }

        unsigned                simdSize = HWIntrinsicInfo::lookupSimdSize(this, intrinsic, sig);
        CORINFO_ARG_LIST_HANDLE argList  = sig->args;
        CORINFO_CLASS_HANDLE    argClass;
        var_types               argType = TYP_UNKNOWN;

        assert(numArgs >= 0);
        if ((HWIntrinsicInfo::lookupIns(intrinsic, baseType) == INS_invalid) ||
            ((simdSize != 8) && (simdSize != 16) && (simdSize != 32)))
        {
            assert(!"Unexpected HW Intrinsic");
            return nullptr;
        }

        GenTreeHWIntrinsic* retNode = nullptr;
        GenTree*            op1     = nullptr;
        GenTree*            op2     = nullptr;

        switch (numArgs)
        {
            case 0:
            {
                retNode = gtNewSimdHWIntrinsicNode(retType, intrinsic, baseType, simdSize);
                break;
            }

            case 1:
            {
                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);
                if ((category == HW_Category_MemoryLoad) && op1->OperIs(GT_CAST))
                {
                    // Although the API specifies a pointer, if what we have is a BYREF, that's what
                    // we really want, so throw away the cast.
                    if (op1->gtGetOp1()->TypeGet() == TYP_BYREF)
                    {
                        op1 = op1->gtGetOp1();
                    }
                }
                retNode = gtNewSimdHWIntrinsicNode(retType, op1, intrinsic, baseType, simdSize);
                break;
            }

            case 2:
            {
                argType = JITtype2varType(
                    strip(info.compCompHnd->getArgType(sig, info.compCompHnd->getArgNext(argList), &argClass)));
                op2 = getArgForHWIntrinsic(argType, argClass);

                op2 = addRangeCheckIfNeeded(intrinsic, op2, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, intrinsic, baseType, simdSize);
                break;
            }

            case 3:
            {
                CORINFO_ARG_LIST_HANDLE arg2 = info.compCompHnd->getArgNext(argList);
                CORINFO_ARG_LIST_HANDLE arg3 = info.compCompHnd->getArgNext(arg2);

                argType      = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg3, &argClass)));
                GenTree* op3 = getArgForHWIntrinsic(argType, argClass);

                op3 = addRangeCheckIfNeeded(intrinsic, op3, mustExpand);

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, arg2, &argClass)));
                op2     = getArgForHWIntrinsic(argType, argClass);

                CORINFO_CLASS_HANDLE op2ArgClass = argClass;

                argType = JITtype2varType(strip(info.compCompHnd->getArgType(sig, argList, &argClass)));
                op1     = getArgForHWIntrinsic(argType, argClass);

                retNode = gtNewSimdHWIntrinsicNode(retType, op1, op2, op3, intrinsic, baseType, simdSize);

#ifdef _TARGET_XARCH_
                if (intrinsic == NI_AVX2_GatherVector128 || intrinsic == NI_AVX2_GatherVector256)
                {
                    assert(varTypeIsSIMD(op2->TypeGet()));
                    retNode->AsHWIntrinsic()->gtIndexBaseType = getBaseTypeOfSIMDType(op2ArgClass);
                }
#endif
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

    return impSpecialIntrinsic(intrinsic, clsHnd, method, sig, mustExpand);
}

#endif // FEATURE_HW_INTRINSICS
