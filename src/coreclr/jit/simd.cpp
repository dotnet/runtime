// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//   SIMD Support
//
// IMPORTANT NOTES AND CAVEATS:
//
// This implementation is preliminary, and may change dramatically.
//
// New JIT types, TYP_SIMDxx, are introduced, and the SIMD intrinsics are created as GT_SIMD nodes.
// Nodes of SIMD types will be typed as TYP_SIMD* (e.g. TYP_SIMD8, TYP_SIMD16, etc.).
//
// Note that currently the "reference implementation" is the same as the runtime dll.  As such, it is currently
// providing implementations for those methods not currently supported by the JIT as intrinsics.
//
// These are currently recognized using string compares, in order to provide an implementation in the JIT
// without taking a dependency on the VM.
// Furthermore, in the CTP, in order to limit the impact of doing these string compares
// against assembly names, we only look for the SIMDVector assembly if we are compiling a class constructor.  This
// makes it somewhat more "pay for play" but is a significant usability compromise.
// This has been addressed for RTM by doing the assembly recognition in the VM.
// --------------------------------------------------------------------------------------

#include "jitpch.h"
#include "simd.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_SIMD

// Intrinsic Id to intrinsic info map
const SIMDIntrinsicInfo simdIntrinsicInfoArray[] = {
#define SIMD_INTRINSIC(mname, inst, id, name, retType, argCount, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, \
                       t10)                                                                                            \
    {SIMDIntrinsic##id, mname, inst, retType, argCount, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10},
#include "simdintrinsiclist.h"
};

//------------------------------------------------------------------------
// getSIMDVectorLength: Get the length (number of elements of base type) of
//                      SIMD Vector given its size and base (element) type.
//
// Arguments:
//    simdSize   - size of the SIMD vector
//    baseType   - type of the elements of the SIMD vector
//
// static
int Compiler::getSIMDVectorLength(unsigned simdSize, var_types baseType)
{
    return simdSize / genTypeSize(baseType);
}

//------------------------------------------------------------------------
// Get the length (number of elements of base type) of SIMD Vector given by typeHnd.
//
// Arguments:
//    typeHnd  - type handle of the SIMD vector
//
int Compiler::getSIMDVectorLength(CORINFO_CLASS_HANDLE typeHnd)
{
    unsigned    sizeBytes   = 0;
    CorInfoType baseJitType = getBaseJitTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
    var_types   baseType    = JitType2PreciseVarType(baseJitType);
    return getSIMDVectorLength(sizeBytes, baseType);
}

//------------------------------------------------------------------------
// Get the preferred alignment of SIMD vector type for better performance.
//
// Arguments:
//    typeHnd  - type handle of the SIMD vector
//
int Compiler::getSIMDTypeAlignment(var_types simdType)
{
    unsigned size = genTypeSize(simdType);

#ifdef TARGET_XARCH
    // Fixed length vectors have the following alignment preference
    // Vector2   = 8 byte alignment
    // Vector3/4 = 16-byte alignment

    // preferred alignment for SSE2 128-bit vectors is 16-bytes
    if (size == 8)
    {
        return 8;
    }
    else if (size <= 16)
    {
        assert((size == 12) || (size == 16));
        return 16;
    }
    else
    {
        assert(size == 32);
        return 32;
    }
#elif defined(TARGET_ARM64)
    // preferred alignment for 64-bit vectors is 8-bytes.
    // For everything else, 16-bytes.
    return (size == 8) ? 8 : 16;
#else
    assert(!"getSIMDTypeAlignment() unimplemented on target arch");
    unreached();
#endif
}

//------------------------------------------------------------------------
// Get, and allocate if necessary, the SIMD temp used for various operations.
// The temp is allocated as the maximum sized type of all operations required.
//
// Arguments:
//    simdType - Required SIMD type
//
// Returns:
//    The temp number
//
unsigned Compiler::getSIMDInitTempVarNum(var_types simdType)
{
    if (lvaSIMDInitTempVarNum == BAD_VAR_NUM)
    {
        JITDUMP("Allocating SIMDInitTempVar as %s\n", varTypeName(simdType));
        lvaSIMDInitTempVarNum                  = lvaGrabTempWithImplicitUse(false DEBUGARG("SIMDInitTempVar"));
        lvaTable[lvaSIMDInitTempVarNum].lvType = simdType;
    }
    else if (genTypeSize(lvaTable[lvaSIMDInitTempVarNum].lvType) < genTypeSize(simdType))
    {
        // We want the largest required type size for the temp.
        JITDUMP("Increasing SIMDInitTempVar type size from %s to %s\n",
                varTypeName(lvaTable[lvaSIMDInitTempVarNum].lvType), varTypeName(simdType));
        lvaTable[lvaSIMDInitTempVarNum].lvType = simdType;
    }
    return lvaSIMDInitTempVarNum;
}

//----------------------------------------------------------------------------------
// Return the base type and size of SIMD vector type given its type handle.
//
// Arguments:
//    typeHnd   - The handle of the type we're interested in.
//    sizeBytes - out param
//
// Return Value:
//    base type of SIMD vector.
//    sizeBytes if non-null is set to size in bytes.
//
// Notes:
//    If the size of the struct is already known call structSizeMightRepresentSIMDType
//    to determine if this api needs to be called.
//
// TODO-Throughput: current implementation parses class name to find base type. Change
//         this when we implement  SIMD intrinsic identification for the final
//         product.
CorInfoType Compiler::getBaseJitTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd, unsigned* sizeBytes /*= nullptr */)
{
    if (m_simdHandleCache == nullptr)
    {
        if (impInlineInfo == nullptr)
        {
            m_simdHandleCache = new (this, CMK_Generic) SIMDHandlesCache();
        }
        else
        {
            // Steal the inliner compiler's cache (create it if not available).

            if (impInlineInfo->InlineRoot->m_simdHandleCache == nullptr)
            {
                impInlineInfo->InlineRoot->m_simdHandleCache = new (this, CMK_Generic) SIMDHandlesCache();
            }

            m_simdHandleCache = impInlineInfo->InlineRoot->m_simdHandleCache;
        }
    }

    if (typeHnd == nullptr)
    {
        return CORINFO_TYPE_UNDEF;
    }

    // fast path search using cached type handles of important types
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned    size            = 0;

    // TODO - Optimize SIMD type recognition by IntrinsicAttribute
    if (isSIMDClass(typeHnd))
    {
        // The most likely to be used type handles are looked up first followed by
        // less likely to be used type handles
        if (typeHnd == m_simdHandleCache->SIMDFloatHandle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<Float>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_INT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<Int>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDVector2Handle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = 2 * genTypeSize(TYP_FLOAT);
            assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
            JITDUMP("  Known type Vector2\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDVector3Handle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = 3 * genTypeSize(TYP_FLOAT);
            assert(size == info.compCompHnd->getClassSize(typeHnd));
            JITDUMP("  Known type Vector3\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDVector4Handle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = 4 * genTypeSize(TYP_FLOAT);
            assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
            JITDUMP("  Known type Vector4\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDVectorHandle)
        {
            size = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type Vector\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDUShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_USHORT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<ushort>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDUByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UBYTE;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<ubyte>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDDoubleHandle)
        {
            simdBaseJitType = CORINFO_TYPE_DOUBLE;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<Double>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDLongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_LONG;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<Long>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_SHORT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<short>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_BYTE;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<byte>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDUIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UINT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<uint>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDULongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_ULONG;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<ulong>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDNIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEINT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<nint>\n");
        }
        else if (typeHnd == m_simdHandleCache->SIMDNUIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEUINT;
            size            = getSIMDVectorRegisterByteLength();
            JITDUMP("  Known type SIMD Vector<nuint>\n");
        }

        // slow path search
        if (simdBaseJitType == CORINFO_TYPE_UNDEF)
        {
            // Doesn't match with any of the cached type handles.
            // Obtain base type by parsing fully qualified class name.
            //
            // TODO-Throughput: implement product shipping solution to query base type.
            WCHAR  className[256] = {0};
            WCHAR* pbuf           = &className[0];
            int    len            = ArrLen(className);
            info.compCompHnd->appendClassName((char16_t**)&pbuf, &len, typeHnd, true, false, false);
            noway_assert(pbuf < &className[256]);
            JITDUMP("SIMD Candidate Type %S\n", className);

            if (wcsncmp(className, W("System.Numerics."), 16) == 0)
            {
                if (wcsncmp(&(className[16]), W("Vector`1["), 9) == 0)
                {
                    size = getSIMDVectorRegisterByteLength();

                    if (wcsncmp(&(className[25]), W("System.Single"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDFloatHandle = typeHnd;
                        simdBaseJitType                    = CORINFO_TYPE_FLOAT;
                        JITDUMP("  Found type SIMD Vector<Float>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.Int32"), 12) == 0)
                    {
                        m_simdHandleCache->SIMDIntHandle = typeHnd;
                        simdBaseJitType                  = CORINFO_TYPE_INT;
                        JITDUMP("  Found type SIMD Vector<Int>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.UInt16"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDUShortHandle = typeHnd;
                        simdBaseJitType                     = CORINFO_TYPE_USHORT;
                        JITDUMP("  Found type SIMD Vector<ushort>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.Byte"), 11) == 0)
                    {
                        m_simdHandleCache->SIMDUByteHandle = typeHnd;
                        simdBaseJitType                    = CORINFO_TYPE_UBYTE;
                        JITDUMP("  Found type SIMD Vector<ubyte>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.Double"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDDoubleHandle = typeHnd;
                        simdBaseJitType                     = CORINFO_TYPE_DOUBLE;
                        JITDUMP("  Found type SIMD Vector<Double>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.Int64"), 12) == 0)
                    {
                        m_simdHandleCache->SIMDLongHandle = typeHnd;
                        simdBaseJitType                   = CORINFO_TYPE_LONG;
                        JITDUMP("  Found type SIMD Vector<Long>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.Int16"), 12) == 0)
                    {
                        m_simdHandleCache->SIMDShortHandle = typeHnd;
                        simdBaseJitType                    = CORINFO_TYPE_SHORT;
                        JITDUMP("  Found type SIMD Vector<short>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.SByte"), 12) == 0)
                    {
                        m_simdHandleCache->SIMDByteHandle = typeHnd;
                        simdBaseJitType                   = CORINFO_TYPE_BYTE;
                        JITDUMP("  Found type SIMD Vector<byte>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.UInt32"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDUIntHandle = typeHnd;
                        simdBaseJitType                   = CORINFO_TYPE_UINT;
                        JITDUMP("  Found type SIMD Vector<uint>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.UInt64"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDULongHandle = typeHnd;
                        simdBaseJitType                    = CORINFO_TYPE_ULONG;
                        JITDUMP("  Found type SIMD Vector<ulong>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.IntPtr"), 13) == 0)
                    {
                        m_simdHandleCache->SIMDNIntHandle = typeHnd;
                        simdBaseJitType                   = CORINFO_TYPE_NATIVEINT;
                        JITDUMP("  Found type SIMD Vector<nint>\n");
                    }
                    else if (wcsncmp(&(className[25]), W("System.UIntPtr"), 14) == 0)
                    {
                        m_simdHandleCache->SIMDNUIntHandle = typeHnd;
                        simdBaseJitType                    = CORINFO_TYPE_NATIVEUINT;
                        JITDUMP("  Found type SIMD Vector<nuint>\n");
                    }
                    else
                    {
                        JITDUMP("  Unknown SIMD Vector<T>\n");
                    }
                }
                else if (wcsncmp(&(className[16]), W("Vector2"), 8) == 0)
                {
                    m_simdHandleCache->SIMDVector2Handle = typeHnd;

                    simdBaseJitType = CORINFO_TYPE_FLOAT;
                    size            = 2 * genTypeSize(TYP_FLOAT);
                    assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                    JITDUMP(" Found Vector2\n");
                }
                else if (wcsncmp(&(className[16]), W("Vector3"), 8) == 0)
                {
                    m_simdHandleCache->SIMDVector3Handle = typeHnd;

                    simdBaseJitType = CORINFO_TYPE_FLOAT;
                    size            = 3 * genTypeSize(TYP_FLOAT);
                    assert(size == info.compCompHnd->getClassSize(typeHnd));
                    JITDUMP(" Found Vector3\n");
                }
                else if (wcsncmp(&(className[16]), W("Vector4"), 8) == 0)
                {
                    m_simdHandleCache->SIMDVector4Handle = typeHnd;

                    simdBaseJitType = CORINFO_TYPE_FLOAT;
                    size            = 4 * genTypeSize(TYP_FLOAT);
                    assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                    JITDUMP(" Found Vector4\n");
                }
                else if (wcsncmp(&(className[16]), W("Vector"), 6) == 0)
                {
                    m_simdHandleCache->SIMDVectorHandle = typeHnd;
                    size                                = getSIMDVectorRegisterByteLength();
                    JITDUMP(" Found type Vector\n");
                }
                else
                {
                    JITDUMP("  Unknown SIMD Type\n");
                }
            }
        }
    }
#ifdef FEATURE_HW_INTRINSICS
    else if (isIntrinsicType(typeHnd))
    {
        const size_t Vector64SizeBytes  = 64 / 8;
        const size_t Vector128SizeBytes = 128 / 8;
        const size_t Vector256SizeBytes = 256 / 8;

#if defined(TARGET_XARCH)
        static_assert_no_msg(YMM_REGSIZE_BYTES == Vector256SizeBytes);
        static_assert_no_msg(XMM_REGSIZE_BYTES == Vector128SizeBytes);

        if (typeHnd == m_simdHandleCache->Vector256FloatHandle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<float>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256DoubleHandle)
        {
            simdBaseJitType = CORINFO_TYPE_DOUBLE;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<double>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256IntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_INT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<int>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256UIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UINT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<uint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256ShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_SHORT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<short>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256UShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_USHORT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<ushort>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256ByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_BYTE;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<sbyte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256UByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UBYTE;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<byte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256LongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_LONG;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<long>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256ULongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_ULONG;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<ulong>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256NIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEINT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<nint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector256NUIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEUINT;
            size            = Vector256SizeBytes;
            JITDUMP("  Known type Vector256<nuint>\n");
        }
        else
#endif // defined(TARGET_XARCH)
            if (typeHnd == m_simdHandleCache->Vector128FloatHandle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<float>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128DoubleHandle)
        {
            simdBaseJitType = CORINFO_TYPE_DOUBLE;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<double>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128IntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_INT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<int>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128UIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UINT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<uint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128ShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_SHORT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<short>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128UShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_USHORT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<ushort>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128ByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_BYTE;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<sbyte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128UByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UBYTE;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<byte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128LongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_LONG;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<long>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128ULongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_ULONG;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<ulong>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128NIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEINT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<nint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector128NUIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEUINT;
            size            = Vector128SizeBytes;
            JITDUMP("  Known type Vector128<nuint>\n");
        }
        else
#if defined(TARGET_ARM64)
            if (typeHnd == m_simdHandleCache->Vector64FloatHandle)
        {
            simdBaseJitType = CORINFO_TYPE_FLOAT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<float>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64DoubleHandle)
        {
            simdBaseJitType = CORINFO_TYPE_DOUBLE;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<double>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64IntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_INT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<int>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64UIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UINT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<uint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64ShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_SHORT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<short>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64UShortHandle)
        {
            simdBaseJitType = CORINFO_TYPE_USHORT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<ushort>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64ByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_BYTE;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<sbyte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64UByteHandle)
        {
            simdBaseJitType = CORINFO_TYPE_UBYTE;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<byte>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64LongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_LONG;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<long>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64ULongHandle)
        {
            simdBaseJitType = CORINFO_TYPE_ULONG;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<ulong>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64NIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEINT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<nint>\n");
        }
        else if (typeHnd == m_simdHandleCache->Vector64NUIntHandle)
        {
            simdBaseJitType = CORINFO_TYPE_NATIVEUINT;
            size            = Vector64SizeBytes;
            JITDUMP("  Known type Vector64<nuint>\n");
        }
#endif // defined(TARGET_ARM64)

        // slow path search
        if (simdBaseJitType == CORINFO_TYPE_UNDEF)
        {
            // Doesn't match with any of the cached type handles.
            const char*          className   = getClassNameFromMetadata(typeHnd, nullptr);
            CORINFO_CLASS_HANDLE baseTypeHnd = getTypeInstantiationArgument(typeHnd, 0);

            if (baseTypeHnd != nullptr)
            {
                CorInfoType type = info.compCompHnd->getTypeForPrimitiveNumericClass(baseTypeHnd);

                JITDUMP("HW Intrinsic SIMD Candidate Type %s with Base Type %s\n", className,
                        getClassNameFromMetadata(baseTypeHnd, nullptr));

#if defined(TARGET_XARCH)
                if (strcmp(className, "Vector256`1") == 0)
                {
                    size = Vector256SizeBytes;
                    switch (type)
                    {
                        case CORINFO_TYPE_FLOAT:
                            m_simdHandleCache->Vector256FloatHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_FLOAT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<float>\n");
                            break;
                        case CORINFO_TYPE_DOUBLE:
                            m_simdHandleCache->Vector256DoubleHandle = typeHnd;
                            simdBaseJitType                          = CORINFO_TYPE_DOUBLE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<double>\n");
                            break;
                        case CORINFO_TYPE_INT:
                            m_simdHandleCache->Vector256IntHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_INT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<int>\n");
                            break;
                        case CORINFO_TYPE_UINT:
                            m_simdHandleCache->Vector256UIntHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_UINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<uint>\n");
                            break;
                        case CORINFO_TYPE_SHORT:
                            m_simdHandleCache->Vector256ShortHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_SHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<short>\n");
                            break;
                        case CORINFO_TYPE_USHORT:
                            m_simdHandleCache->Vector256UShortHandle = typeHnd;
                            simdBaseJitType                          = CORINFO_TYPE_USHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<ushort>\n");
                            break;
                        case CORINFO_TYPE_LONG:
                            m_simdHandleCache->Vector256LongHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_LONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<long>\n");
                            break;
                        case CORINFO_TYPE_ULONG:
                            m_simdHandleCache->Vector256ULongHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_ULONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<ulong>\n");
                            break;
                        case CORINFO_TYPE_UBYTE:
                            m_simdHandleCache->Vector256UByteHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_UBYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<byte>\n");
                            break;
                        case CORINFO_TYPE_BYTE:
                            m_simdHandleCache->Vector256ByteHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_BYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<sbyte>\n");
                            break;
                        case CORINFO_TYPE_NATIVEINT:
                            m_simdHandleCache->Vector256NIntHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_NATIVEINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<nint>\n");
                            break;
                        case CORINFO_TYPE_NATIVEUINT:
                            m_simdHandleCache->Vector256NUIntHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_NATIVEUINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector256<nuint>\n");
                            break;

                        default:
                            JITDUMP("  Unknown Hardware Intrinsic SIMD Type Vector256<T>\n");
                    }
                }
                else
#endif // defined(TARGET_XARCH)
                    if (strcmp(className, "Vector128`1") == 0)
                {
                    size = Vector128SizeBytes;
                    switch (type)
                    {
                        case CORINFO_TYPE_FLOAT:
                            m_simdHandleCache->Vector128FloatHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_FLOAT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<float>\n");
                            break;
                        case CORINFO_TYPE_DOUBLE:
                            m_simdHandleCache->Vector128DoubleHandle = typeHnd;
                            simdBaseJitType                          = CORINFO_TYPE_DOUBLE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<double>\n");
                            break;
                        case CORINFO_TYPE_INT:
                            m_simdHandleCache->Vector128IntHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_INT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<int>\n");
                            break;
                        case CORINFO_TYPE_UINT:
                            m_simdHandleCache->Vector128UIntHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_UINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<uint>\n");
                            break;
                        case CORINFO_TYPE_SHORT:
                            m_simdHandleCache->Vector128ShortHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_SHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<short>\n");
                            break;
                        case CORINFO_TYPE_USHORT:
                            m_simdHandleCache->Vector128UShortHandle = typeHnd;
                            simdBaseJitType                          = CORINFO_TYPE_USHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<ushort>\n");
                            break;
                        case CORINFO_TYPE_LONG:
                            m_simdHandleCache->Vector128LongHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_LONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<long>\n");
                            break;
                        case CORINFO_TYPE_ULONG:
                            m_simdHandleCache->Vector128ULongHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_ULONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<ulong>\n");
                            break;
                        case CORINFO_TYPE_UBYTE:
                            m_simdHandleCache->Vector128UByteHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_UBYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<byte>\n");
                            break;
                        case CORINFO_TYPE_BYTE:
                            m_simdHandleCache->Vector128ByteHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_BYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<sbyte>\n");
                            break;
                        case CORINFO_TYPE_NATIVEINT:
                            m_simdHandleCache->Vector128NIntHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_NATIVEINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<nint>\n");
                            break;
                        case CORINFO_TYPE_NATIVEUINT:
                            m_simdHandleCache->Vector128NUIntHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_NATIVEUINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector128<nuint>\n");
                            break;

                        default:
                            JITDUMP("  Unknown Hardware Intrinsic SIMD Type Vector128<T>\n");
                    }
                }
#if defined(TARGET_ARM64)
                else if (strcmp(className, "Vector64`1") == 0)
                {
                    size = Vector64SizeBytes;
                    switch (type)
                    {
                        case CORINFO_TYPE_FLOAT:
                            m_simdHandleCache->Vector64FloatHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_FLOAT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<float>\n");
                            break;
                        case CORINFO_TYPE_DOUBLE:
                            m_simdHandleCache->Vector64DoubleHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_DOUBLE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<double>\n");
                            break;
                        case CORINFO_TYPE_INT:
                            m_simdHandleCache->Vector64IntHandle = typeHnd;
                            simdBaseJitType                      = CORINFO_TYPE_INT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<int>\n");
                            break;
                        case CORINFO_TYPE_UINT:
                            m_simdHandleCache->Vector64UIntHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_UINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<uint>\n");
                            break;
                        case CORINFO_TYPE_SHORT:
                            m_simdHandleCache->Vector64ShortHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_SHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<short>\n");
                            break;
                        case CORINFO_TYPE_USHORT:
                            m_simdHandleCache->Vector64UShortHandle = typeHnd;
                            simdBaseJitType                         = CORINFO_TYPE_USHORT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<ushort>\n");
                            break;
                        case CORINFO_TYPE_LONG:
                            m_simdHandleCache->Vector64LongHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_LONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<long>\n");
                            break;
                        case CORINFO_TYPE_ULONG:
                            m_simdHandleCache->Vector64ULongHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_ULONG;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<ulong>\n");
                            break;
                        case CORINFO_TYPE_UBYTE:
                            m_simdHandleCache->Vector64UByteHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_UBYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<byte>\n");
                            break;
                        case CORINFO_TYPE_BYTE:
                            m_simdHandleCache->Vector64ByteHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_BYTE;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<sbyte>\n");
                            break;
                        case CORINFO_TYPE_NATIVEINT:
                            m_simdHandleCache->Vector64NIntHandle = typeHnd;
                            simdBaseJitType                       = CORINFO_TYPE_NATIVEINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<nint>\n");
                            break;
                        case CORINFO_TYPE_NATIVEUINT:
                            m_simdHandleCache->Vector64NUIntHandle = typeHnd;
                            simdBaseJitType                        = CORINFO_TYPE_NATIVEUINT;
                            JITDUMP("  Found type Hardware Intrinsic SIMD Vector64<nuint>\n");
                            break;

                        default:
                            JITDUMP("  Unknown Hardware Intrinsic SIMD Type Vector64<T>\n");
                    }
                }
#endif // defined(TARGET_ARM64)
            }
        }

#if defined(TARGET_XARCH)
        // Even though Vector256 is TYP_SIMD32, if AVX isn't supported, then it must
        // be treated as a regular struct
        if (size == YMM_REGSIZE_BYTES && (simdBaseJitType != CORINFO_TYPE_UNDEF) &&
            !compExactlyDependsOn(InstructionSet_AVX))
        {
            simdBaseJitType = CORINFO_TYPE_UNDEF;
        }
#endif // TARGET_XARCH
    }
#endif // FEATURE_HW_INTRINSICS

    if (sizeBytes != nullptr)
    {
        *sizeBytes = size;
    }

    if (simdBaseJitType != CORINFO_TYPE_UNDEF)
    {
        setUsesSIMDTypes(true);
    }

    return simdBaseJitType;
}

//--------------------------------------------------------------------------------------
// getSIMDIntrinsicInfo: get SIMD intrinsic info given the method handle.
//
// Arguments:
//    inOutTypeHnd    - The handle of the type on which the method is invoked.  This is an in-out param.
//    methodHnd       - The handle of the method we're interested in.
//    sig             - method signature info
//    isNewObj        - whether this call represents a newboj constructor call
//    argCount        - argument count - out pram
//    simdBaseJitType - base JIT type of the intrinsic - out param
//    sizeBytes       - size of SIMD vector type on which the method is invoked - out param
//
// Return Value:
//    SIMDIntrinsicInfo struct initialized corresponding to methodHnd.
//    Sets SIMDIntrinsicInfo.id to SIMDIntrinsicInvalid if methodHnd doesn't correspond
//    to any SIMD intrinsic.  Also, sets the out params inOutTypeHnd, argCount, baseType and
//    sizeBytes.
//
//    Note that VectorMath class doesn't have a base type and first argument of the method
//    determines the SIMD vector type on which intrinsic is invoked. In such a case inOutTypeHnd
//    is modified by this routine.
//
// TODO-Throughput: The current implementation is based on method name string parsing.
//         Although we now have type identification from the VM, the parsing of intrinsic names
//         could be made more efficient.
//
const SIMDIntrinsicInfo* Compiler::getSIMDIntrinsicInfo(CORINFO_CLASS_HANDLE* inOutTypeHnd,
                                                        CORINFO_METHOD_HANDLE methodHnd,
                                                        CORINFO_SIG_INFO*     sig,
                                                        bool                  isNewObj,
                                                        unsigned*             argCount,
                                                        CorInfoType*          simdBaseJitType,
                                                        unsigned*             sizeBytes)
{
    assert(simdBaseJitType != nullptr);
    assert(sizeBytes != nullptr);

    // get simdBaseJitType and size of the type
    CORINFO_CLASS_HANDLE typeHnd = *inOutTypeHnd;
    *simdBaseJitType             = getBaseJitTypeAndSizeOfSIMDType(typeHnd, sizeBytes);

    if (typeHnd == m_simdHandleCache->SIMDVectorHandle)
    {
        // All of the supported intrinsics on this static class take a first argument that's a vector,
        // which determines the simdBaseJitType.
        // The exception is the IsHardwareAccelerated property, which is handled as a special case.
        assert(*simdBaseJitType == CORINFO_TYPE_UNDEF);
        if (sig->numArgs == 0)
        {
            const SIMDIntrinsicInfo* hwAccelIntrinsicInfo = &(simdIntrinsicInfoArray[SIMDIntrinsicHWAccel]);
            if ((strcmp(eeGetMethodName(methodHnd, nullptr), hwAccelIntrinsicInfo->methodName) == 0) &&
                JITtype2varType(sig->retType) == hwAccelIntrinsicInfo->retType)
            {
                // Sanity check
                assert(hwAccelIntrinsicInfo->argCount == 0 && hwAccelIntrinsicInfo->isInstMethod == false);
                return hwAccelIntrinsicInfo;
            }
            return nullptr;
        }
        else
        {
            typeHnd          = info.compCompHnd->getArgClass(sig, sig->args);
            *inOutTypeHnd    = typeHnd;
            *simdBaseJitType = getBaseJitTypeAndSizeOfSIMDType(typeHnd, sizeBytes);
        }
    }

    if (*simdBaseJitType == CORINFO_TYPE_UNDEF)
    {
        JITDUMP("NOT a SIMD Intrinsic: unsupported baseType\n");
        return nullptr;
    }

    var_types simdBaseType = JitType2PreciseVarType(*simdBaseJitType);

    // account for implicit "this" arg
    *argCount = sig->numArgs;
    if (sig->hasThis())
    {
        *argCount += 1;
    }

    // Get the Intrinsic Id by parsing method name.
    //
    // TODO-Throughput: replace sequential search by binary search by arranging entries
    // sorted by method name.
    SIMDIntrinsicID intrinsicId = SIMDIntrinsicInvalid;
    const char*     methodName  = eeGetMethodName(methodHnd, nullptr);
    for (int i = SIMDIntrinsicNone + 1; i < SIMDIntrinsicInvalid; ++i)
    {
        if (strcmp(methodName, simdIntrinsicInfoArray[i].methodName) == 0)
        {
            // Found an entry for the method; further check whether it is one of
            // the supported base types.
            bool found = false;
            for (int j = 0; j < SIMD_INTRINSIC_MAX_BASETYPE_COUNT; ++j)
            {
                // Convention: if there are fewer base types supported than MAX_BASETYPE_COUNT,
                // the end of the list is marked by TYP_UNDEF.
                if (simdIntrinsicInfoArray[i].supportedBaseTypes[j] == TYP_UNDEF)
                {
                    break;
                }

                if (simdIntrinsicInfoArray[i].supportedBaseTypes[j] == simdBaseType)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                continue;
            }

            // Now, check the arguments.
            unsigned int fixedArgCnt    = simdIntrinsicInfoArray[i].argCount;
            unsigned int expectedArgCnt = fixedArgCnt;

            // First handle SIMDIntrinsicInitN, where the arg count depends on the type.
            // The listed arg types include the vector and the first two init values, which is the expected number
            // for Vector2.  For other cases, we'll check their types here.
            if (*argCount > expectedArgCnt)
            {
                if (i == SIMDIntrinsicInitN)
                {
                    if (*argCount == 3 && typeHnd == m_simdHandleCache->SIMDVector2Handle)
                    {
                        expectedArgCnt = 3;
                    }
                    else if (*argCount == 4 && typeHnd == m_simdHandleCache->SIMDVector3Handle)
                    {
                        expectedArgCnt = 4;
                    }
                    else if (*argCount == 5 && typeHnd == m_simdHandleCache->SIMDVector4Handle)
                    {
                        expectedArgCnt = 5;
                    }
                }
                else if (i == SIMDIntrinsicInitFixed)
                {
                    if (*argCount == 4 && typeHnd == m_simdHandleCache->SIMDVector4Handle)
                    {
                        expectedArgCnt = 4;
                    }
                }
            }
            if (*argCount != expectedArgCnt)
            {
                continue;
            }

            // Validate the types of individual args passed are what is expected of.
            // If any of the types don't match with what is expected, don't consider
            // as an intrinsic.  This will make an older JIT with SIMD capabilities
            // resilient to breaking changes to SIMD managed API.
            //
            // Note that from IL type stack, args get popped in right to left order
            // whereas args get listed in method signatures in left to right order.

            int stackIndex = (expectedArgCnt - 1);

            // Track the arguments from the signature - we currently only use this to distinguish
            // integral and pointer types, both of which will by TYP_I_IMPL on the importer stack.
            CORINFO_ARG_LIST_HANDLE argLst = sig->args;

            CORINFO_CLASS_HANDLE argClass;
            for (unsigned int argIndex = 0; found == true && argIndex < expectedArgCnt; argIndex++)
            {
                bool isThisPtr = ((argIndex == 0) && sig->hasThis());

                // In case of "newobj SIMDVector<T>(T val)", thisPtr won't be present on type stack.
                // We don't check anything in that case.
                if (!isThisPtr || !isNewObj)
                {
                    GenTree*  arg     = impStackTop(stackIndex).val;
                    var_types argType = arg->TypeGet();

                    var_types expectedArgType;
                    if (argIndex < fixedArgCnt)
                    {
                        // Convention:
                        //   - intrinsicInfo.argType[i] == TYP_UNDEF - intrinsic doesn't have a valid arg at position i
                        //   - intrinsicInfo.argType[i] == TYP_UNKNOWN - arg type should be same as simdBaseType
                        // Note that we pop the args off in reverse order.
                        expectedArgType = simdIntrinsicInfoArray[i].argType[argIndex];
                        assert(expectedArgType != TYP_UNDEF);
                        if (expectedArgType == TYP_UNKNOWN)
                        {
                            // The type of the argument will be genActualType(*simdBaseType).
                            expectedArgType = genActualType(simdBaseType);
                            argType         = genActualType(argType);
                        }
                    }
                    else
                    {
                        expectedArgType = simdBaseType;
                    }

                    if (!isThisPtr && argType == TYP_I_IMPL)
                    {
                        // The reference implementation has a constructor that takes a pointer.
                        // We don't want to recognize that one.  This requires us to look at the CorInfoType
                        // in order to distinguish a signature with a pointer argument from one with an
                        // integer argument of pointer size, both of which will be TYP_I_IMPL on the stack.
                        // TODO-Review: This seems quite fragile.  We should consider beefing up the checking
                        // here.
                        CorInfoType corType = strip(info.compCompHnd->getArgType(sig, argLst, &argClass));
                        if (corType == CORINFO_TYPE_PTR)
                        {
                            found = false;
                        }
                    }

                    if (varTypeIsSIMD(argType))
                    {
                        argType = TYP_STRUCT;
                    }
                    if (argType != expectedArgType)
                    {
                        found = false;
                    }
                }
                if (argIndex != 0 || !sig->hasThis())
                {
                    argLst = info.compCompHnd->getArgNext(argLst);
                }
                stackIndex--;
            }

            // Cross check return type and static vs. instance is what we are expecting.
            // If not, don't consider it as an intrinsic.
            // Note that ret type of TYP_UNKNOWN means that it is not known apriori and must be same as simdBaseType
            if (found)
            {
                var_types expectedRetType = simdIntrinsicInfoArray[i].retType;
                if (expectedRetType == TYP_UNKNOWN)
                {
                    // JIT maps uint/ulong type vars to TYP_INT/TYP_LONG.
                    expectedRetType = (simdBaseType == TYP_UINT || simdBaseType == TYP_ULONG)
                                          ? genActualType(simdBaseType)
                                          : simdBaseType;
                }

                if (JITtype2varType(sig->retType) != expectedRetType ||
                    sig->hasThis() != simdIntrinsicInfoArray[i].isInstMethod)
                {
                    found = false;
                }
            }

            if (found)
            {
                intrinsicId = (SIMDIntrinsicID)i;
                break;
            }
        }
    }

    if (intrinsicId != SIMDIntrinsicInvalid)
    {
        JITDUMP("Method %s maps to SIMD intrinsic %s\n", methodName, simdIntrinsicNames[intrinsicId]);
        return &simdIntrinsicInfoArray[intrinsicId];
    }
    else
    {
        JITDUMP("Method %s is NOT a SIMD intrinsic\n", methodName);
    }

    return nullptr;
}

/* static */ bool Compiler::vnEncodesResultTypeForSIMDIntrinsic(SIMDIntrinsicID intrinsicId)
{
    switch (intrinsicId)
    {
        case SIMDIntrinsicInit:
        case SIMDIntrinsicSub:
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicCast:
            return true;

        default:
            break;
    }
    return false;
}

// Pops and returns GenTree node from importer's type stack.
// Normalizes TYP_STRUCT value in case of GT_CALL, GT_RET_EXPR and arg nodes.
//
// Arguments:
//    type         -  the type of value that the caller expects to be popped off the stack.
//    expectAddr   -  if true indicates we are expecting type stack entry to be a TYP_BYREF.
//    structHandle -  the class handle to use when normalizing if it is not the same as the stack entry class handle;
//                    this can happen for certain scenarios, such as folding away a static cast, where we want the
//                    value popped to have the type that would have been returned.
//
// Notes:
//    If the popped value is a struct, and the expected type is a simd type, it will be set
//    to that type, otherwise it will assert if the type being popped is not the expected type.

GenTree* Compiler::impSIMDPopStack(var_types type, bool expectAddr, CORINFO_CLASS_HANDLE structHandle)
{
    StackEntry se   = impPopStack();
    typeInfo   ti   = se.seTypeInfo;
    GenTree*   tree = se.val;

    // If expectAddr is true implies what we have on stack is address and we need
    // SIMD type struct that it points to.
    if (expectAddr)
    {
        assert(tree->TypeIs(TYP_BYREF, TYP_I_IMPL));

        if (tree->OperGet() == GT_ADDR)
        {
            tree = tree->gtGetOp1();
        }
        else
        {
            tree = gtNewOperNode(GT_IND, type, tree);
        }
    }

    bool isParam = false;

    // If we are popping a struct type it must have a matching handle if one is specified.
    // - If we have an existing 'OBJ' and 'structHandle' is specified, we will change its
    //   handle if it doesn't match.
    //   This can happen when we have a retyping of a vector that doesn't translate to any
    //   actual IR.
    // - (If it's not an OBJ and it's used in a parameter context where it is required,
    //   impNormStructVal will add one).
    //
    if (tree->OperGet() == GT_OBJ)
    {
        if ((structHandle != NO_CLASS_HANDLE) && (tree->AsObj()->GetLayout()->GetClassHandle() != structHandle))
        {
            // In this case we need to retain the GT_OBJ to retype the value.
            tree->AsObj()->SetLayout(typGetObjLayout(structHandle));
        }
        else
        {
            GenTree* addr = tree->AsOp()->gtOp1;
            if ((addr->OperGet() == GT_ADDR) && isSIMDTypeLocal(addr->AsOp()->gtOp1))
            {
                tree = addr->AsOp()->gtOp1;
            }
        }
    }

    if (tree->OperGet() == GT_LCL_VAR)
    {
        isParam = lvaGetDesc(tree->AsLclVarCommon())->lvIsParam;
    }

    // normalize TYP_STRUCT value
    if (varTypeIsStruct(tree) && ((tree->OperGet() == GT_RET_EXPR) || (tree->OperGet() == GT_CALL) || isParam))
    {
        assert(ti.IsType(TI_STRUCT));

        if (structHandle == nullptr)
        {
            structHandle = ti.GetClassHandleForValueClass();
        }

        tree = impNormStructVal(tree, structHandle, (unsigned)CHECK_SPILL_ALL);
    }

    // Now set the type of the tree to the specialized SIMD struct type, if applicable.
    if (genActualType(tree->gtType) != genActualType(type))
    {
        assert(tree->gtType == TYP_STRUCT);
        tree->gtType = type;
    }
    else if (tree->gtType == TYP_BYREF)
    {
        assert(tree->IsLocal() || (tree->OperGet() == GT_RET_EXPR) || (tree->OperGet() == GT_CALL) ||
               ((tree->gtOper == GT_ADDR) && varTypeIsSIMD(tree->gtGetOp1())));
    }

    return tree;
}

#ifdef TARGET_XARCH
// impSIMDLongRelOpEqual: transforms operands and returns the SIMD intrinsic to be applied on
// transformed operands to obtain == comparison result.
//
// Arguments:
//    typeHnd  -  type handle of SIMD vector
//    size     -  SIMD vector size
//    op1      -  in-out parameter; first operand
//    op2      -  in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params op1, op2 and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDLongRelOpEqual(CORINFO_CLASS_HANDLE typeHnd,
                                                unsigned             size,
                                                GenTree**            pOp1,
                                                GenTree**            pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    // There is no direct SSE2 support for comparing TYP_LONG vectors.
    // These have to be implemented in terms of TYP_INT vector comparison operations.
    //
    // Equality(v1, v2):
    // tmp = (v1 == v2) i.e. compare for equality as if v1 and v2 are vector<int>
    // result = BitwiseAnd(t, shuffle(t, (2, 3, 0, 1)))
    // Shuffle is meant to swap the comparison results of low-32-bits and high 32-bits of respective long elements.

    // Compare vector<long> as if they were vector<int> and assign the result to a temp
    GenTree* compResult = gtNewSIMDNode(simdType, *pOp1, *pOp2, SIMDIntrinsicEqual, CORINFO_TYPE_INT, size);
    unsigned lclNum     = lvaGrabTemp(true DEBUGARG("SIMD Long =="));
    lvaSetStruct(lclNum, typeHnd, false);
    GenTree* tmp = gtNewLclvNode(lclNum, simdType);
    GenTree* asg = gtNewTempAssign(lclNum, compResult);

    // op1 = GT_COMMA(tmp=compResult, tmp)
    // op2 = Shuffle(tmp, 0xB1)
    // IntrinsicId = BitwiseAnd
    *pOp1 = gtNewOperNode(GT_COMMA, simdType, asg, tmp);
    *pOp2 = gtNewSIMDNode(simdType, gtNewLclvNode(lclNum, simdType), gtNewIconNode(SHUFFLE_ZWXY, TYP_INT),
                          SIMDIntrinsicShuffleSSE2, CORINFO_TYPE_INT, size);
    return SIMDIntrinsicBitwiseAnd;
}
#endif // TARGET_XARCH

// Transforms operands and returns the SIMD intrinsic to be applied on
// transformed operands to obtain given relop result.
//
// Arguments:
//    relOpIntrinsicId - Relational operator SIMD intrinsic
//    typeHnd          - type handle of SIMD vector
//    size             - SIMD vector size
//    inOutBaseJitType - base JIT type of SIMD vector
//    pOp1             - in-out parameter; first operand
//    pOp2             - in-out parameter; second operand
//
// Return Value:
//    Modifies in-out params pOp1, pOp2, inOutBaseType and returns intrinsic ID to be applied to modified operands
//
SIMDIntrinsicID Compiler::impSIMDRelOp(SIMDIntrinsicID      relOpIntrinsicId,
                                       CORINFO_CLASS_HANDLE typeHnd,
                                       unsigned             size,
                                       CorInfoType*         inOutBaseJitType,
                                       GenTree**            pOp1,
                                       GenTree**            pOp2)
{
    var_types simdType = (*pOp1)->TypeGet();
    assert(varTypeIsSIMD(simdType) && ((*pOp2)->TypeGet() == simdType));

    assert(isRelOpSIMDIntrinsic(relOpIntrinsicId));

    SIMDIntrinsicID intrinsicID = relOpIntrinsicId;
#ifdef TARGET_XARCH
    CorInfoType simdBaseJitType = *inOutBaseJitType;
    var_types   simdBaseType    = JitType2PreciseVarType(simdBaseJitType);

    if (varTypeIsFloating(simdBaseType))
    {
    }
    else if (varTypeIsIntegral(simdBaseType))
    {
        if ((getSIMDSupportLevel() == SIMD_SSE2_Supported) && simdBaseType == TYP_LONG)
        {
            // There is no direct SSE2 support for comparing TYP_LONG vectors.
            // These have to be implemented interms of TYP_INT vector comparison operations.
            if (intrinsicID == SIMDIntrinsicEqual)
            {
                intrinsicID = impSIMDLongRelOpEqual(typeHnd, size, pOp1, pOp2);
            }
            else
            {
                unreached();
            }
        }
        // SSE2 and AVX direct support for signed comparison of int32, int16 and int8 types
        else if (varTypeIsUnsigned(simdBaseType))
        {
            // Vector<byte>, Vector<ushort>, Vector<uint> and Vector<ulong>:
            // SSE2 supports > for signed comparison. Therefore, to use it for
            // comparing unsigned numbers, we subtract a constant from both the
            // operands such that the result fits within the corresponding signed
            // type.  The resulting signed numbers are compared using SSE2 signed
            // comparison.
            //
            // Vector<byte>: constant to be subtracted is 2^7
            // Vector<ushort> constant to be subtracted is 2^15
            // Vector<uint> constant to be subtracted is 2^31
            // Vector<ulong> constant to be subtracted is 2^63
            //
            // We need to treat op1 and op2 as signed for comparison purpose after
            // the transformation.
            __int64 constVal = 0;
            switch (simdBaseType)
            {
                case TYP_UBYTE:
                    constVal          = 0x80808080;
                    *inOutBaseJitType = CORINFO_TYPE_BYTE;
                    break;
                case TYP_USHORT:
                    constVal          = 0x80008000;
                    *inOutBaseJitType = CORINFO_TYPE_SHORT;
                    break;
                case TYP_UINT:
                    constVal          = 0x80000000;
                    *inOutBaseJitType = CORINFO_TYPE_INT;
                    break;
                case TYP_ULONG:
                    constVal          = 0x8000000000000000LL;
                    *inOutBaseJitType = CORINFO_TYPE_LONG;
                    break;
                default:
                    unreached();
                    break;
            }
            assert(constVal != 0);

            // This transformation is not required for equality.
            if (intrinsicID != SIMDIntrinsicEqual)
            {
                // For constructing const vector use either long or int base type.
                CorInfoType tempBaseJitType;
                GenTree*    initVal;
                if (simdBaseType == TYP_ULONG)
                {
                    tempBaseJitType = CORINFO_TYPE_LONG;
                    initVal         = gtNewLconNode(constVal);
                }
                else
                {
                    tempBaseJitType = CORINFO_TYPE_INT;
                    initVal         = gtNewIconNode((ssize_t)constVal);
                }
                initVal->gtType      = JITtype2varType(tempBaseJitType);
                GenTree* constVector = gtNewSIMDNode(simdType, initVal, SIMDIntrinsicInit, tempBaseJitType, size);

                // Assign constVector to a temp, since we intend to use it more than once
                // TODO-CQ: We have quite a few such constant vectors constructed during
                // the importation of SIMD intrinsics.  Make sure that we have a single
                // temp per distinct constant per method.
                GenTree* tmp = fgInsertCommaFormTemp(&constVector, typeHnd);

                // op1 = op1 - constVector
                // op2 = op2 - constVector
                *pOp1 = gtNewSIMDNode(simdType, *pOp1, constVector, SIMDIntrinsicSub, simdBaseJitType, size);
                *pOp2 = gtNewSIMDNode(simdType, *pOp2, tmp, SIMDIntrinsicSub, simdBaseJitType, size);
            }

            return impSIMDRelOp(intrinsicID, typeHnd, size, inOutBaseJitType, pOp1, pOp2);
        }
    }
#elif !defined(TARGET_ARM64)
    assert(!"impSIMDRelOp() unimplemented on target arch");
    unreached();
#endif // !TARGET_XARCH

    return intrinsicID;
}

//------------------------------------------------------------------------
// getOp1ForConstructor: Get the op1 for a constructor call.
//
// Arguments:
//    opcode     - the opcode being handled (needed to identify the CEE_NEWOBJ case)
//    newobjThis - For CEE_NEWOBJ, this is the temp grabbed for the allocated uninitalized object.
//    clsHnd    - The handle of the class of the method.
//
// Return Value:
//    The tree node representing the object to be initialized with the constructor.
//
// Notes:
//    This method handles the differences between the CEE_NEWOBJ and constructor cases.
//
GenTree* Compiler::getOp1ForConstructor(OPCODE opcode, GenTree* newobjThis, CORINFO_CLASS_HANDLE clsHnd)
{
    GenTree* op1;
    if (opcode == CEE_NEWOBJ)
    {
        op1 = newobjThis;
        assert(newobjThis->gtOper == GT_ADDR && newobjThis->AsOp()->gtOp1->gtOper == GT_LCL_VAR);

        // push newobj result on type stack
        unsigned tmp = op1->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
        impPushOnStack(gtNewLclvNode(tmp, lvaGetRealType(tmp)), verMakeTypeInfo(clsHnd).NormaliseForStack());
    }
    else
    {
        op1 = impSIMDPopStack(TYP_BYREF);
    }
    assert(op1->TypeGet() == TYP_BYREF);
    return op1;
}

//-------------------------------------------------------------------
// Set the flag that indicates that the lclVar referenced by this tree
// is used in a SIMD intrinsic.
// Arguments:
//      tree - GenTree*

void Compiler::setLclRelatedToSIMDIntrinsic(GenTree* tree)
{
    assert(tree->OperIsLocal());
    LclVarDsc* lclVarDsc             = lvaGetDesc(tree->AsLclVarCommon());
    lclVarDsc->lvUsedInSIMDIntrinsic = true;
}

//-------------------------------------------------------------
// Check if two field nodes reference at the same memory location.
// Notice that this check is just based on pattern matching.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//    If op1's parents node and op2's parents node are at the same location, return true. Otherwise, return false

bool areFieldsParentsLocatedSame(GenTree* op1, GenTree* op2)
{
    assert(op1->OperGet() == GT_FIELD);
    assert(op2->OperGet() == GT_FIELD);

    GenTree* op1ObjRef = op1->AsField()->GetFldObj();
    GenTree* op2ObjRef = op2->AsField()->GetFldObj();
    while (op1ObjRef != nullptr && op2ObjRef != nullptr)
    {

        if (op1ObjRef->OperGet() != op2ObjRef->OperGet())
        {
            break;
        }
        else if (op1ObjRef->OperGet() == GT_ADDR)
        {
            op1ObjRef = op1ObjRef->AsOp()->gtOp1;
            op2ObjRef = op2ObjRef->AsOp()->gtOp1;
        }

        if (op1ObjRef->OperIsLocal() && op2ObjRef->OperIsLocal() &&
            op1ObjRef->AsLclVarCommon()->GetLclNum() == op2ObjRef->AsLclVarCommon()->GetLclNum())
        {
            return true;
        }
        else if (op1ObjRef->OperGet() == GT_FIELD && op2ObjRef->OperGet() == GT_FIELD &&
                 op1ObjRef->AsField()->gtFldHnd == op2ObjRef->AsField()->gtFldHnd)
        {
            op1ObjRef = op1ObjRef->AsField()->GetFldObj();
            op2ObjRef = op2ObjRef->AsField()->GetFldObj();
            continue;
        }
        else
        {
            break;
        }
    }

    return false;
}

//----------------------------------------------------------------------
// Check whether two field are contiguous
// Arguments:
//      first - GenTree*. The Type of the node should be TYP_FLOAT
//      second - GenTree*. The Type of the node should be TYP_FLOAT
// Return Value:
//      if the first field is located before second field, and they are located contiguously,
//      then return true. Otherwise, return false.

bool Compiler::areFieldsContiguous(GenTree* first, GenTree* second)
{
    assert(first->OperGet() == GT_FIELD);
    assert(second->OperGet() == GT_FIELD);
    assert(first->gtType == TYP_FLOAT);
    assert(second->gtType == TYP_FLOAT);

    var_types firstFieldType  = first->gtType;
    var_types secondFieldType = second->gtType;

    unsigned firstFieldEndOffset = first->AsField()->gtFldOffset + genTypeSize(firstFieldType);
    unsigned secondFieldOffset   = second->AsField()->gtFldOffset;
    if (firstFieldEndOffset == secondFieldOffset && firstFieldType == secondFieldType &&
        areFieldsParentsLocatedSame(first, second))
    {
        return true;
    }

    return false;
}

//----------------------------------------------------------------------
// areLocalFieldsContiguous: Check whether two local field are contiguous
//
// Arguments:
//    first - the first local field
//    second - the second local field
//
// Return Value:
//    If the first field is located before second field, and they are located contiguously,
//    then return true. Otherwise, return false.
//
bool Compiler::areLocalFieldsContiguous(GenTreeLclFld* first, GenTreeLclFld* second)
{
    assert(first->TypeIs(TYP_FLOAT));
    assert(second->TypeIs(TYP_FLOAT));

    return (first->TypeGet() == second->TypeGet()) &&
           (first->GetLclOffs() + genTypeSize(first->TypeGet()) == second->GetLclOffs());
}

//-------------------------------------------------------------------------------
// Check whether two array element nodes are located contiguously or not.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//      if the array element op1 is located before array element op2, and they are contiguous,
//      then return true. Otherwise, return false.
// TODO-CQ:
//      Right this can only check array element with const number as index. In future,
//      we should consider to allow this function to check the index using expression.

bool Compiler::areArrayElementsContiguous(GenTree* op1, GenTree* op2)
{
    noway_assert(op1->gtOper == GT_INDEX);
    noway_assert(op2->gtOper == GT_INDEX);
    GenTreeIndex* op1Index = op1->AsIndex();
    GenTreeIndex* op2Index = op2->AsIndex();

    GenTree* op1ArrayRef = op1Index->Arr();
    GenTree* op2ArrayRef = op2Index->Arr();
    assert(op1ArrayRef->TypeGet() == TYP_REF);
    assert(op2ArrayRef->TypeGet() == TYP_REF);

    GenTree* op1IndexNode = op1Index->Index();
    GenTree* op2IndexNode = op2Index->Index();
    if ((op1IndexNode->OperGet() == GT_CNS_INT && op2IndexNode->OperGet() == GT_CNS_INT) &&
        op1IndexNode->AsIntCon()->gtIconVal + 1 == op2IndexNode->AsIntCon()->gtIconVal)
    {
        if (op1ArrayRef->OperGet() == GT_FIELD && op2ArrayRef->OperGet() == GT_FIELD &&
            areFieldsParentsLocatedSame(op1ArrayRef, op2ArrayRef))
        {
            return true;
        }
        else if (op1ArrayRef->OperIsLocal() && op2ArrayRef->OperIsLocal() &&
                 op1ArrayRef->AsLclVarCommon()->GetLclNum() == op2ArrayRef->AsLclVarCommon()->GetLclNum())
        {
            return true;
        }
    }
    return false;
}

//-------------------------------------------------------------------------------
// Check whether two argument nodes are contiguous or not.
// Arguments:
//      op1 - GenTree*.
//      op2 - GenTree*.
// Return Value:
//      if the argument node op1 is located before argument node op2, and they are located contiguously,
//      then return true. Otherwise, return false.
// TODO-CQ:
//      Right now this can only check field and array. In future we should add more cases.
//

bool Compiler::areArgumentsContiguous(GenTree* op1, GenTree* op2)
{
    if (op1->OperGet() == GT_INDEX && op2->OperGet() == GT_INDEX)
    {
        return areArrayElementsContiguous(op1, op2);
    }
    else if (op1->OperGet() == GT_FIELD && op2->OperGet() == GT_FIELD)
    {
        return areFieldsContiguous(op1, op2);
    }
    else if (op1->OperIs(GT_LCL_FLD) && op2->OperIs(GT_LCL_FLD))
    {
        return areLocalFieldsContiguous(op1->AsLclFld(), op2->AsLclFld());
    }
    return false;
}

//--------------------------------------------------------------------------------------------------------
// createAddressNodeForSIMDInit: Generate the address node(GT_LEA) if we want to intialize vector2, vector3 or vector4
// from first argument's address.
//
// Arguments:
//      tree - GenTree*. This the tree node which is used to get the address for indir.
//      simdsize - unsigned. This the simd vector size.
//      arrayElementsCount - unsigned. This is used for generating the boundary check for array.
//
// Return value:
//      return the address node.
//
// TODO-CQ:
//      1. Currently just support for GT_FIELD and GT_INDEX, because we can only verify the GT_INDEX node or GT_Field
//         are located contiguously or not. In future we should support more cases.
//      2. Though it happens to just work fine front-end phases are not aware of GT_LEA node.  Therefore, convert these
//         to use GT_ADDR.
GenTree* Compiler::createAddressNodeForSIMDInit(GenTree* tree, unsigned simdSize)
{
    assert(tree->OperGet() == GT_FIELD || tree->OperGet() == GT_INDEX);
    GenTree*  byrefNode  = nullptr;
    GenTree*  startIndex = nullptr;
    unsigned  offset     = 0;
    var_types baseType   = tree->gtType;

    if (tree->OperGet() == GT_FIELD)
    {
        GenTree* objRef = tree->AsField()->GetFldObj();
        if (objRef != nullptr && objRef->gtOper == GT_ADDR)
        {
            GenTree* obj = objRef->AsOp()->gtOp1;

            // If the field is directly from a struct, then in this case,
            // we should set this struct's lvUsedInSIMDIntrinsic as true,
            // so that this sturct won't be promoted.
            // e.g. s.x x is a field, and s is a struct, then we should set the s's lvUsedInSIMDIntrinsic as true.
            // so that s won't be promoted.
            // Notice that if we have a case like s1.s2.x. s1 s2 are struct, and x is a field, then it is possible that
            // s1 can be promoted, so that s2 can be promoted. The reason for that is if we don't allow s1 to be
            // promoted, then this will affect the other optimizations which are depend on s1's struct promotion.
            // TODO-CQ:
            //  In future, we should optimize this case so that if there is a nested field like s1.s2.x and s1.s2.x's
            //  address is used for initializing the vector, then s1 can be promoted but s2 can't.
            if (varTypeIsSIMD(obj) && obj->OperIsLocal())
            {
                setLclRelatedToSIMDIntrinsic(obj);
            }
        }

        byrefNode = gtCloneExpr(tree->AsField()->GetFldObj());
        assert(byrefNode != nullptr);
        offset = tree->AsField()->gtFldOffset;
    }
    else if (tree->OperGet() == GT_INDEX)
    {

        GenTree* index = tree->AsIndex()->Index();
        assert(index->OperGet() == GT_CNS_INT);

        GenTree* checkIndexExpr = nullptr;
        unsigned indexVal       = (unsigned)(index->AsIntCon()->gtIconVal);
        offset                  = indexVal * genTypeSize(tree->TypeGet());
        GenTree* arrayRef       = tree->AsIndex()->Arr();

        // Generate the boundary check exception.
        // The length for boundary check should be the maximum index number which should be
        // (first argument's index number) + (how many array arguments we have) - 1
        // = indexVal + arrayElementsCount - 1
        unsigned arrayElementsCount = simdSize / genTypeSize(baseType);
        checkIndexExpr              = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, indexVal + arrayElementsCount - 1);
        GenTreeArrLen*    arrLen    = gtNewArrLen(TYP_INT, arrayRef, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
        GenTreeBoundsChk* arrBndsChk =
            new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(checkIndexExpr, arrLen, SCK_ARG_RNG_EXCPN);

        offset += OFFSETOF__CORINFO_Array__data;
        byrefNode = gtNewOperNode(GT_COMMA, arrayRef->TypeGet(), arrBndsChk, gtCloneExpr(arrayRef));
    }
    else
    {
        unreached();
    }
    GenTree* address =
        new (this, GT_LEA) GenTreeAddrMode(TYP_BYREF, byrefNode, startIndex, genTypeSize(tree->TypeGet()), offset);
    return address;
}

//-------------------------------------------------------------------------------
// impMarkContiguousSIMDFieldAssignments: Try to identify if there are contiguous
// assignments from SIMD field to memory. If there are, then mark the related
// lclvar so that it won't be promoted.
//
// Arguments:
//      stmt - GenTree*. Input statement node.

void Compiler::impMarkContiguousSIMDFieldAssignments(Statement* stmt)
{
    if (opts.OptimizationDisabled())
    {
        return;
    }
    GenTree* expr = stmt->GetRootNode();
    if (expr->OperGet() == GT_ASG && expr->TypeGet() == TYP_FLOAT)
    {
        GenTree*    curDst            = expr->AsOp()->gtOp1;
        GenTree*    curSrc            = expr->AsOp()->gtOp2;
        unsigned    index             = 0;
        CorInfoType simdBaseJitType   = CORINFO_TYPE_UNDEF;
        unsigned    simdSize          = 0;
        GenTree*    srcSimdStructNode = getSIMDStructFromField(curSrc, &simdBaseJitType, &index, &simdSize, true);

        if (srcSimdStructNode == nullptr || simdBaseJitType != CORINFO_TYPE_FLOAT)
        {
            fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
        }
        else if (index == 0 && isSIMDTypeLocal(srcSimdStructNode))
        {
            fgPreviousCandidateSIMDFieldAsgStmt = stmt;
        }
        else if (fgPreviousCandidateSIMDFieldAsgStmt != nullptr)
        {
            assert(index > 0);
            var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
            GenTree*  prevAsgExpr  = fgPreviousCandidateSIMDFieldAsgStmt->GetRootNode();
            GenTree*  prevDst      = prevAsgExpr->AsOp()->gtOp1;
            GenTree*  prevSrc      = prevAsgExpr->AsOp()->gtOp2;
            if (!areArgumentsContiguous(prevDst, curDst) || !areArgumentsContiguous(prevSrc, curSrc))
            {
                fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
            }
            else
            {
                if (index == (simdSize / genTypeSize(simdBaseType) - 1))
                {
                    // Successfully found the pattern, mark the lclvar as UsedInSIMDIntrinsic
                    if (srcSimdStructNode->OperIsLocal())
                    {
                        setLclRelatedToSIMDIntrinsic(srcSimdStructNode);
                    }

                    if (curDst->OperGet() == GT_FIELD)
                    {
                        GenTree* objRef = curDst->AsField()->GetFldObj();
                        if (objRef != nullptr && objRef->gtOper == GT_ADDR)
                        {
                            GenTree* obj = objRef->AsOp()->gtOp1;
                            if (varTypeIsStruct(obj) && obj->OperIsLocal())
                            {
                                setLclRelatedToSIMDIntrinsic(obj);
                            }
                        }
                    }
                }
                else
                {
                    fgPreviousCandidateSIMDFieldAsgStmt = stmt;
                }
            }
        }
    }
    else
    {
        fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
    }
}

//------------------------------------------------------------------------
// impSIMDIntrinsic: Check method to see if it is a SIMD method
//
// Arguments:
//    opcode     - the opcode being handled (needed to identify the CEE_NEWOBJ case)
//    newobjThis - For CEE_NEWOBJ, this is the temp grabbed for the allocated uninitalized object.
//    clsHnd     - The handle of the class of the method.
//    method     - The handle of the method.
//    sig        - The call signature for the method.
//    memberRef  - The memberRef token for the method reference.
//
// Return Value:
//    If clsHnd is a known SIMD type, and 'method' is one of the methods that are
//    implemented as an intrinsic in the JIT, then return the tree that implements
//    it.
//
GenTree* Compiler::impSIMDIntrinsic(OPCODE                opcode,
                                    GenTree*              newobjThis,
                                    CORINFO_CLASS_HANDLE  clsHnd,
                                    CORINFO_METHOD_HANDLE methodHnd,
                                    CORINFO_SIG_INFO*     sig,
                                    unsigned              methodFlags,
                                    int                   memberRef)
{
    // Exit early if we are not in one of the SIMD types.
    if (!isSIMDClass(clsHnd))
    {
        return nullptr;
    }

    // Exit early if the method is not a JIT Intrinsic (which requires the [Intrinsic] attribute).
    if ((methodFlags & CORINFO_FLG_INTRINSIC) == 0)
    {
        return nullptr;
    }

    // Get base type and intrinsic Id
    CorInfoType              simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned                 size            = 0;
    unsigned                 argCount        = 0;
    const SIMDIntrinsicInfo* intrinsicInfo =
        getSIMDIntrinsicInfo(&clsHnd, methodHnd, sig, (opcode == CEE_NEWOBJ), &argCount, &simdBaseJitType, &size);

    // Exit early if the intrinsic is invalid or unrecognized
    if ((intrinsicInfo == nullptr) || (intrinsicInfo->id == SIMDIntrinsicInvalid))
    {
        return nullptr;
    }

    if (!IsBaselineSimdIsaSupported())
    {
        // The user disabled support for the baseline ISA so
        // don't emit any SIMD intrinsics as they all require
        // this at a minimum. We will, however, return false
        // for IsHardwareAccelerated as that will help with
        // dead code elimination.

        return (intrinsicInfo->id == SIMDIntrinsicHWAccel) ? gtNewIconNode(0, TYP_INT) : nullptr;
    }

    SIMDIntrinsicID simdIntrinsicID = intrinsicInfo->id;
    var_types       simdBaseType;
    var_types       simdType;

    if (simdBaseJitType != CORINFO_TYPE_UNDEF)
    {
        simdBaseType = JitType2PreciseVarType(simdBaseJitType);
        simdType     = getSIMDTypeForSize(size);
    }
    else
    {
        assert(simdIntrinsicID == SIMDIntrinsicHWAccel);
        simdBaseType = TYP_UNKNOWN;
        simdType     = TYP_UNKNOWN;
    }
    bool      instMethod = intrinsicInfo->isInstMethod;
    var_types callType   = JITtype2varType(sig->retType);
    if (callType == TYP_STRUCT)
    {
        // Note that here we are assuming that, if the call returns a struct, that it is the same size as the
        // struct on which the method is declared. This is currently true for all methods on Vector types,
        // but if this ever changes, we will need to determine the callType from the signature.
        assert(info.compCompHnd->getClassSize(sig->retTypeClass) == genTypeSize(simdType));
        callType = simdType;
    }

    GenTree* simdTree   = nullptr;
    GenTree* op1        = nullptr;
    GenTree* op2        = nullptr;
    GenTree* op3        = nullptr;
    GenTree* retVal     = nullptr;
    GenTree* copyBlkDst = nullptr;
    bool     doCopyBlk  = false;

    switch (simdIntrinsicID)
    {
        case SIMDIntrinsicInit:
        case SIMDIntrinsicInitN:
        {
            // SIMDIntrinsicInit:
            //    op2 - the initializer value
            //    op1 - byref of vector
            //
            // SIMDIntrinsicInitN
            //    op2 - list of initializer values stitched into a list
            //    op1 - byref of vector
            IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), argCount - 1);
            bool                 initFromFirstArgIndir = false;

            if (simdIntrinsicID == SIMDIntrinsicInit)
            {
                op2 = impSIMDPopStack(simdBaseType);
                nodeBuilder.AddOperand(0, op2);
            }
            else
            {
                assert(simdIntrinsicID == SIMDIntrinsicInitN);
                assert(simdBaseType == TYP_FLOAT);

                unsigned initCount    = argCount - 1;
                unsigned elementCount = getSIMDVectorLength(size, simdBaseType);
                noway_assert(initCount == elementCount);

                // Build an array with the N values.
                // We must maintain left-to-right order of the args, but we will pop
                // them off in reverse order (the Nth arg was pushed onto the stack last).

                GenTree* prevArg           = nullptr;
                bool     areArgsContiguous = true;
                for (unsigned i = 0; i < initCount; i++)
                {
                    GenTree* arg = impSIMDPopStack(simdBaseType);

                    if (areArgsContiguous)
                    {
                        GenTree* curArg = arg;

                        if (prevArg != nullptr)
                        {
                            // Recall that we are popping the args off the stack in reverse order.
                            areArgsContiguous = areArgumentsContiguous(curArg, prevArg);
                        }
                        prevArg = curArg;
                    }

                    assert(genActualType(arg) == genActualType(simdBaseType));
                    nodeBuilder.AddOperand(initCount - i - 1, arg);
                }

                if (areArgsContiguous && simdBaseType == TYP_FLOAT)
                {
                    // Since Vector2, Vector3 and Vector4's arguments type are only float,
                    // we intialize the vector from first argument address, only when
                    // the simdBaseType is TYP_FLOAT and the arguments are located contiguously in memory
                    initFromFirstArgIndir = true;
                    GenTree*  op2Address  = createAddressNodeForSIMDInit(nodeBuilder.GetOperand(0), size);
                    var_types simdType    = getSIMDTypeForSize(size);
                    op2                   = gtNewOperNode(GT_IND, simdType, op2Address);
                }
            }

            op1 = getOp1ForConstructor(opcode, newobjThis, clsHnd);

            assert(op1->TypeGet() == TYP_BYREF);

            // For integral base types of size less than TYP_INT, expand the initializer
            // to fill size of TYP_INT bytes.
            if (varTypeIsSmallInt(simdBaseType))
            {
                // This case should occur only for Init intrinsic.
                assert(simdIntrinsicID == SIMDIntrinsicInit);

                unsigned baseSize = genTypeSize(simdBaseType);
                int      multiplier;
                if (baseSize == 1)
                {
                    multiplier = 0x01010101;
                }
                else
                {
                    assert(baseSize == 2);
                    multiplier = 0x00010001;
                }

                GenTree* t1 = nullptr;
                if (simdBaseType == TYP_BYTE)
                {
                    // What we have is a signed byte initializer,
                    // which when loaded to a reg will get sign extended to TYP_INT.
                    // But what we need is the initializer without sign extended or
                    // rather zero extended to 32-bits.
                    t1 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(0xff, TYP_INT));
                }
                else if (simdBaseType == TYP_SHORT)
                {
                    // What we have is a signed short initializer,
                    // which when loaded to a reg will get sign extended to TYP_INT.
                    // But what we need is the initializer without sign extended or
                    // rather zero extended to 32-bits.
                    t1 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(0xffff, TYP_INT));
                }
                else
                {
                    // TODO-Casts: this cast is useless.
                    assert(simdBaseType == TYP_UBYTE || simdBaseType == TYP_USHORT);
                    t1 = gtNewCastNode(TYP_INT, op2, false, TYP_INT);
                }

                assert(t1 != nullptr);
                GenTree* t2 = gtNewIconNode(multiplier, TYP_INT);
                op2         = gtNewOperNode(GT_MUL, TYP_INT, t1, t2);

                // Construct a vector of TYP_INT with the new initializer and cast it back to vector of simdBaseType
                simdTree = gtNewSIMDNode(simdType, op2, simdIntrinsicID, CORINFO_TYPE_INT, size);
                simdTree = gtNewSIMDNode(simdType, simdTree, SIMDIntrinsicCast, simdBaseJitType, size);
            }
            else
            {

                if (initFromFirstArgIndir)
                {
                    simdTree = op2;
                    if (op1->AsOp()->gtOp1->OperIsLocal())
                    {
                        // label the dst struct's lclvar is used for SIMD intrinsic,
                        // so that this dst struct won't be promoted.
                        setLclRelatedToSIMDIntrinsic(op1->AsOp()->gtOp1);
                    }
                }
                else
                {
                    simdTree = new (this, GT_SIMD)
                        GenTreeSIMD(simdType, std::move(nodeBuilder), simdIntrinsicID, simdBaseJitType, size);
                }
            }

            copyBlkDst = op1;
            doCopyBlk  = true;
        }
        break;

        case SIMDIntrinsicInitArray:
        case SIMDIntrinsicInitArrayX:
        case SIMDIntrinsicCopyToArray:
        case SIMDIntrinsicCopyToArrayX:
        {
            // op3 - index into array in case of SIMDIntrinsicCopyToArrayX and SIMDIntrinsicInitArrayX
            // op2 - array itself
            // op1 - byref to vector struct

            unsigned int vectorLength = getSIMDVectorLength(size, simdBaseType);
            // (This constructor takes only the zero-based arrays.)
            // We will add one or two bounds checks:
            // 1. If we have an index, we must do a check on that first.
            //    We can't combine it with the index + vectorLength check because
            //    a. It might be negative, and b. It may need to raise a different exception
            //    (captured as SCK_ARG_RNG_EXCPN for CopyTo and Init).
            // 2. We need to generate a check (SCK_ARG_EXCPN for CopyTo and Init)
            //    for the last array element we will access.
            //    We'll either check against (vectorLength - 1) or (index + vectorLength - 1).

            GenTree* checkIndexExpr = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, vectorLength - 1);

            // Get the index into the array.  If it has been provided, it will be on the
            // top of the stack.  Otherwise, it is null.
            if (argCount == 3)
            {
                op3 = impSIMDPopStack(TYP_INT);
                if (op3->IsIntegralConst(0))
                {
                    op3 = nullptr;
                }
            }
            else
            {
                // TODO-CQ: Here, or elsewhere, check for the pattern where op2 is a newly constructed array, and
                // change this to the InitN form.
                // op3 = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, 0);
                op3 = nullptr;
            }

            // Clone the array for use in the bounds check.
            op2 = impSIMDPopStack(TYP_REF);
            assert(op2->TypeGet() == TYP_REF);
            GenTree* arrayRefForArgChk = op2;
            GenTree* argRngChk         = nullptr;
            if ((arrayRefForArgChk->gtFlags & GTF_SIDE_EFFECT) != 0)
            {
                op2 = fgInsertCommaFormTemp(&arrayRefForArgChk);
            }
            else
            {
                op2 = gtCloneExpr(arrayRefForArgChk);
            }
            assert(op2 != nullptr);

            if (op3 != nullptr)
            {
                // We need to use the original expression on this, which is the first check.
                GenTree* arrayRefForArgRngChk = arrayRefForArgChk;
                // Then we clone the clone we just made for the next check.
                arrayRefForArgChk = gtCloneExpr(op2);
                // We know we MUST have had a cloneable expression.
                assert(arrayRefForArgChk != nullptr);
                GenTree* index = op3;
                if ((index->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    op3 = fgInsertCommaFormTemp(&index);
                }
                else
                {
                    op3 = gtCloneExpr(index);
                }

                GenTreeArrLen* arrLen =
                    gtNewArrLen(TYP_INT, arrayRefForArgRngChk, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
                argRngChk = new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(index, arrLen, SCK_ARG_RNG_EXCPN);
                // Now, clone op3 to create another node for the argChk
                GenTree* index2 = gtCloneExpr(op3);
                assert(index != nullptr);
                checkIndexExpr = gtNewOperNode(GT_ADD, TYP_INT, index2, checkIndexExpr);
            }

            // Insert a bounds check for index + offset - 1.
            // This must be a "normal" array.
            SpecialCodeKind op2CheckKind;
            if (simdIntrinsicID == SIMDIntrinsicInitArray || simdIntrinsicID == SIMDIntrinsicInitArrayX)
            {
                op2CheckKind = SCK_ARG_RNG_EXCPN;
            }
            else
            {
                op2CheckKind = SCK_ARG_EXCPN;
            }
            GenTreeArrLen* arrLen =
                gtNewArrLen(TYP_INT, arrayRefForArgChk, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
            GenTreeBoundsChk* argChk =
                new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(checkIndexExpr, arrLen, op2CheckKind);

            // Create a GT_COMMA tree for the bounds check(s).
            op2 = gtNewOperNode(GT_COMMA, op2->TypeGet(), argChk, op2);
            if (argRngChk != nullptr)
            {
                op2 = gtNewOperNode(GT_COMMA, op2->TypeGet(), argRngChk, op2);
            }

            if (simdIntrinsicID == SIMDIntrinsicInitArray || simdIntrinsicID == SIMDIntrinsicInitArrayX)
            {
                op1      = getOp1ForConstructor(opcode, newobjThis, clsHnd);
                simdTree = (op3 != nullptr)
                               ? gtNewSIMDNode(simdType, op2, op3, SIMDIntrinsicInitArray, simdBaseJitType, size)
                               : gtNewSIMDNode(simdType, op2, SIMDIntrinsicInitArray, simdBaseJitType, size);
                copyBlkDst = op1;
                doCopyBlk  = true;
            }
            else
            {
                assert(simdIntrinsicID == SIMDIntrinsicCopyToArray || simdIntrinsicID == SIMDIntrinsicCopyToArrayX);
                op1 = impSIMDPopStack(simdType, instMethod);
                assert(op1->TypeGet() == simdType);

                // copy vector (op1) to array (op2) starting at index (op3)
                simdTree = op1;

                // TODO-Cleanup: Though it happens to just work fine front-end phases are not aware of GT_LEA node.
                // Therefore, convert these to use GT_ADDR .
                copyBlkDst = new (this, GT_LEA)
                    GenTreeAddrMode(TYP_BYREF, op2, op3, genTypeSize(simdBaseType), OFFSETOF__CORINFO_Array__data);
                doCopyBlk = true;
            }
        }
        break;

        case SIMDIntrinsicInitFixed:
        {
            // We are initializing a fixed-length vector VLarge with a smaller fixed-length vector VSmall, plus 1 or 2
            // additional floats.
            //    op4 (optional) - float value for VLarge.W, if VLarge is Vector4, and VSmall is Vector2
            //    op3 - float value for VLarge.Z or VLarge.W
            //    op2 - VSmall
            //    op1 - byref of VLarge
            assert(simdBaseType == TYP_FLOAT);

            GenTree* op4 = nullptr;
            if (argCount == 4)
            {
                op4 = impSIMDPopStack(TYP_FLOAT);
                assert(op4->TypeGet() == TYP_FLOAT);
            }
            op3 = impSIMDPopStack(TYP_FLOAT);
            assert(op3->TypeGet() == TYP_FLOAT);
            // The input vector will either be TYP_SIMD8 or TYP_SIMD12.
            var_types smallSIMDType = TYP_SIMD8;
            if ((op4 == nullptr) && (simdType == TYP_SIMD16))
            {
                smallSIMDType = TYP_SIMD12;
            }
            op2 = impSIMDPopStack(smallSIMDType);
            op1 = getOp1ForConstructor(opcode, newobjThis, clsHnd);

            // We are going to redefine the operands so that:
            // - op3 is the value that's going into the Z position, or null if it's a Vector4 constructor with a single
            // operand, and
            // - op4 is the W position value, or null if this is a Vector3 constructor.
            if (size == 16 && argCount == 3)
            {
                op4 = op3;
                op3 = nullptr;
            }

            simdTree = op2;
            if (op3 != nullptr)
            {
                simdTree = gtNewSimdWithElementNode(simdType, simdTree, gtNewIconNode(2, TYP_INT), op3, simdBaseJitType,
                                                    size, /* isSimdAsHWIntrinsic */ true);
            }
            if (op4 != nullptr)
            {
                simdTree = gtNewSimdWithElementNode(simdType, simdTree, gtNewIconNode(3, TYP_INT), op4, simdBaseJitType,
                                                    size, /* isSimdAsHWIntrinsic */ true);
            }

            copyBlkDst = op1;
            doCopyBlk  = true;
        }
        break;

        case SIMDIntrinsicEqual:
        {
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            SIMDIntrinsicID intrinsicID = impSIMDRelOp(simdIntrinsicID, clsHnd, size, &simdBaseJitType, &op1, &op2);
            simdTree = gtNewSIMDNode(genActualType(callType), op1, op2, intrinsicID, simdBaseJitType, size);
            retVal   = simdTree;
        }
        break;

        case SIMDIntrinsicSub:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseOr:
        {
            // op1 is the first operand; if instance method, op1 is "this" arg
            // op2 is the second operand
            op2 = impSIMDPopStack(simdType);
            op1 = impSIMDPopStack(simdType, instMethod);

            simdTree = gtNewSIMDNode(simdType, op1, op2, simdIntrinsicID, simdBaseJitType, size);
            retVal   = simdTree;
        }
        break;

        // Unary operators that take and return a Vector.
        case SIMDIntrinsicCast:
        {
            op1 = impSIMDPopStack(simdType, instMethod);

            simdTree = gtNewSIMDNode(simdType, op1, simdIntrinsicID, simdBaseJitType, size);
            retVal   = simdTree;
        }
        break;

        case SIMDIntrinsicHWAccel:
        {
            GenTreeIntCon* intConstTree = new (this, GT_CNS_INT) GenTreeIntCon(TYP_INT, 1);
            retVal                      = intConstTree;
        }
        break;

        default:
            assert(!"Unimplemented SIMD Intrinsic");
            return nullptr;
    }

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
    // XArch/Arm64: also indicate that we use floating point registers.
    // The need for setting this here is that a method may not have SIMD
    // type lclvars, but might be exercising SIMD intrinsics on fields of
    // SIMD type.
    //
    // e.g.  public Vector<float> ComplexVecFloat::sqabs() { return this.r * this.r + this.i * this.i; }
    compFloatingPointUsed = true;
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)

    // At this point, we have a tree that we are going to store into a destination.
    // TODO-1stClassStructs: This should be a simple store or assignment, and should not require
    // GTF_ALL_EFFECT for the dest. This is currently emulating the previous behavior of
    // block ops.
    if (doCopyBlk)
    {
        GenTree* dest = new (this, GT_BLK)
            GenTreeBlk(GT_BLK, simdType, copyBlkDst, typGetBlkLayout(getSIMDTypeSizeInBytes(clsHnd)));
        dest->gtFlags |= GTF_GLOB_REF;
        retVal = gtNewBlkOpNode(dest, simdTree,
                                false, // not volatile
                                true); // copyBlock
        retVal->gtFlags |= ((simdTree->gtFlags | copyBlkDst->gtFlags) & GTF_ALL_EFFECT);
    }

    return retVal;
}

#endif // FEATURE_SIMD
