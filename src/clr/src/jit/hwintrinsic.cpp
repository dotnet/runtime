// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"

#ifdef FEATURE_HW_INTRINSICS

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

#endif // FEATURE_HW_INTRINSICS
