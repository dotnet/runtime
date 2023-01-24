// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//   SIMD Support
//
// IMPORTANT NOTES AND CAVEATS:
//
// This implementation is preliminary, and may change dramatically.
//
// New JIT types, TYP_SIMDxx, are introduced, and the hwintrinsics are created as GT_HWINTRINSC nodes.
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

    if (sizeBytes != nullptr)
    {
        *sizeBytes = 0;
    }

    if ((typeHnd == nullptr) || !isIntrinsicType(typeHnd))
    {
        return CORINFO_TYPE_UNDEF;
    }

    const char* namespaceName;
    const char* className = getClassNameFromMetadata(typeHnd, &namespaceName);

    // fast path search using cached type handles of important types
    CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
    unsigned    size            = 0;

    if (isNumericsNamespace(namespaceName))
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
            JITDUMP("SIMD Candidate Type %s\n", className);

            if (strcmp(className, "Vector`1") == 0)
            {
                size = getSIMDVectorRegisterByteLength();

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseJitType                 = info.compCompHnd->getTypeForPrimitiveNumericClass(typeArgHnd);

                switch (simdBaseJitType)
                {
                    case CORINFO_TYPE_FLOAT:
                        m_simdHandleCache->SIMDFloatHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_INT:
                        m_simdHandleCache->SIMDIntHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_USHORT:
                        m_simdHandleCache->SIMDUShortHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_UBYTE:
                        m_simdHandleCache->SIMDUByteHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_DOUBLE:
                        m_simdHandleCache->SIMDDoubleHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_LONG:
                        m_simdHandleCache->SIMDLongHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_SHORT:
                        m_simdHandleCache->SIMDShortHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_BYTE:
                        m_simdHandleCache->SIMDByteHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_UINT:
                        m_simdHandleCache->SIMDUIntHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_ULONG:
                        m_simdHandleCache->SIMDULongHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_NATIVEINT:
                        m_simdHandleCache->SIMDNIntHandle = typeHnd;
                        break;
                    case CORINFO_TYPE_NATIVEUINT:
                        m_simdHandleCache->SIMDNUIntHandle = typeHnd;
                        break;
                    default:
                        simdBaseJitType = CORINFO_TYPE_UNDEF;
                        break;
                }

                if (simdBaseJitType != CORINFO_TYPE_UNDEF)
                {
                    JITDUMP("  Found type SIMD Vector<%s>\n", varTypeName(JitType2PreciseVarType(simdBaseJitType)));
                }
                else
                {
                    JITDUMP("  Unknown SIMD Vector<T>\n");
                }
            }
            else if (strcmp(className, "Vector2") == 0)
            {
                m_simdHandleCache->SIMDVector2Handle = typeHnd;

                simdBaseJitType = CORINFO_TYPE_FLOAT;
                size            = 2 * genTypeSize(TYP_FLOAT);
                assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                JITDUMP(" Found Vector2\n");
            }
            else if (strcmp(className, "Vector3") == 0)
            {
                m_simdHandleCache->SIMDVector3Handle = typeHnd;

                simdBaseJitType = CORINFO_TYPE_FLOAT;
                size            = 3 * genTypeSize(TYP_FLOAT);
                assert(size == info.compCompHnd->getClassSize(typeHnd));
                JITDUMP(" Found Vector3\n");
            }
            else if (strcmp(className, "Vector4") == 0)
            {
                m_simdHandleCache->SIMDVector4Handle = typeHnd;

                simdBaseJitType = CORINFO_TYPE_FLOAT;
                size            = 4 * genTypeSize(TYP_FLOAT);
                assert(size == roundUp(info.compCompHnd->getClassSize(typeHnd), TARGET_POINTER_SIZE));
                JITDUMP(" Found Vector4\n");
            }
            else if (strcmp(className, "Vector") == 0)
            {
                m_simdHandleCache->SIMDVectorHandle = typeHnd;
                size                                = getSIMDVectorRegisterByteLength();
                JITDUMP(" Found type Vector\n");
            }
        }
    }
#ifdef FEATURE_HW_INTRINSICS
    else
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

        CORINFO_CLASS_HANDLE* pCanonicalHnd = nullptr;
        switch (size)
        {
            case 8:
                pCanonicalHnd = &m_simdHandleCache->CanonicalSimd8Handle;
                break;
            case 12:
                // There is no need for a canonical SIMD12 handle because it is always Vector3.
                break;
            case 16:
                pCanonicalHnd = &m_simdHandleCache->CanonicalSimd16Handle;
                break;
            case 32:
                pCanonicalHnd = &m_simdHandleCache->CanonicalSimd32Handle;
                break;
            default:
                unreached();
        }

        if ((pCanonicalHnd != nullptr) && (*pCanonicalHnd == NO_CLASS_HANDLE))
        {
            *pCanonicalHnd = typeHnd;
        }
    }

    return simdBaseJitType;
}

// Pops and returns GenTree node from importer's type stack.
// Normalizes TYP_STRUCT value in case of GT_CALL and GT_RET_EXPR.
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
//
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

        tree = gtNewOperNode(GT_IND, type, tree);
    }

    if (tree->OperIsIndir() && tree->AsIndir()->Addr()->OperIs(GT_LCL_VAR_ADDR))
    {
        GenTreeLclVar* lclAddr = tree->AsIndir()->Addr()->AsLclVar();
        LclVarDsc*     varDsc  = lvaGetDesc(lclAddr);
        if (varDsc->TypeGet() == type)
        {
            assert(type != TYP_STRUCT);
            lclAddr->ChangeType(type);
            lclAddr->SetOper(GT_LCL_VAR);

            tree = lclAddr;
        }
    }

    // Handle calls that may return the struct via a return buffer.
    if (varTypeIsStruct(tree) && tree->OperIs(GT_CALL, GT_RET_EXPR))
    {
        assert(ti.IsType(TI_STRUCT));

        if (structHandle == NO_CLASS_HANDLE)
        {
            structHandle = ti.GetClassHandleForValueClass();
        }

        tree = impNormStructVal(tree, structHandle, CHECK_SPILL_ALL);
    }

    // Now set the type of the tree to the specialized SIMD struct type, if applicable.
    if (genActualType(tree->gtType) != genActualType(type))
    {
        assert(tree->gtType == TYP_STRUCT);
        tree->gtType = type;
    }
    else if (tree->gtType == TYP_BYREF)
    {
        assert(tree->IsLocal() || tree->OperIs(GT_RET_EXPR, GT_CALL) ||
               (tree->OperIs(GT_LCL_VAR_ADDR) && varTypeIsSIMD(lvaGetDesc(tree->AsLclVar()))));
    }

    return tree;
}

//-------------------------------------------------------------------
// Set the flag that indicates that the lclVar referenced by this tree
// is used in a SIMD intrinsic.
// Arguments:
//      tree - GenTree*
//
void Compiler::setLclRelatedToSIMDIntrinsic(GenTree* tree)
{
    assert(tree->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR));
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

        if (op1ObjRef->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR) &&
            (op1ObjRef->AsLclVarCommon()->GetLclNum() == op2ObjRef->AsLclVarCommon()->GetLclNum()))
        {
            return true;
        }

        if (op1ObjRef->OperIs(GT_FIELD) && (op1ObjRef->AsField()->gtFldHnd == op2ObjRef->AsField()->gtFldHnd))
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
//
bool Compiler::areArrayElementsContiguous(GenTree* op1, GenTree* op2)
{
    assert(op1->OperIs(GT_IND) && op2->OperIs(GT_IND));
    assert(!op1->TypeIs(TYP_STRUCT) && (op1->TypeGet() == op2->TypeGet()));

    GenTreeIndexAddr* op1IndexAddr = op1->AsIndir()->Addr()->AsIndexAddr();
    GenTreeIndexAddr* op2IndexAddr = op2->AsIndir()->Addr()->AsIndexAddr();

    GenTree* op1ArrayRef = op1IndexAddr->Arr();
    GenTree* op2ArrayRef = op2IndexAddr->Arr();
    assert(op1ArrayRef->TypeGet() == TYP_REF);
    assert(op2ArrayRef->TypeGet() == TYP_REF);

    GenTree* op1IndexNode = op1IndexAddr->Index();
    GenTree* op2IndexNode = op2IndexAddr->Index();
    if ((op1IndexNode->OperGet() == GT_CNS_INT && op2IndexNode->OperGet() == GT_CNS_INT) &&
        op1IndexNode->AsIntCon()->gtIconVal + 1 == op2IndexNode->AsIntCon()->gtIconVal)
    {
        if (op1ArrayRef->OperIs(GT_FIELD) && op2ArrayRef->OperIs(GT_FIELD) &&
            areFieldsParentsLocatedSame(op1ArrayRef, op2ArrayRef))
        {
            return true;
        }
        else if (op1ArrayRef->OperIs(GT_LCL_VAR) && op2ArrayRef->OperIs(GT_LCL_VAR) &&
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
    if (op1->TypeGet() != op2->TypeGet())
    {
        return false;
    }

    assert(!op1->TypeIs(TYP_STRUCT));

    if (op1->OperIs(GT_IND) && op1->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR) && op2->OperIs(GT_IND) &&
        op2->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR))
    {
        return areArrayElementsContiguous(op1, op2);
    }
    else if (op1->OperIs(GT_FIELD) && op2->OperIs(GT_FIELD))
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
// CreateAddressNodeForSimdHWIntrinsicCreate: Generate the address node if we want to initialize a simd type
// from first argument's address.
//
// Arguments:
//      tree         - The tree node which is used to get the address for indir.
//      simdBaseType - The type of the elements in the SIMD node
//      simdsize     - The simd vector size.
//
// Return value:
//      return the address node.
//
// TODO-CQ:
//      Currently just supports GT_FIELD and GT_IND(GT_INDEX_ADDR), because we can only verify those nodes
//      are located contiguously or not. In future we should support more cases.
//
GenTree* Compiler::CreateAddressNodeForSimdHWIntrinsicCreate(GenTree* tree, var_types simdBaseType, unsigned simdSize)
{
    GenTree*  byrefNode = nullptr;
    unsigned  offset    = 0;
    var_types baseType  = tree->gtType;

    if (tree->OperIs(GT_FIELD))
    {
        GenTree* objRef = tree->AsField()->GetFldObj();
        if ((objRef != nullptr) && objRef->OperIs(GT_LCL_VAR_ADDR))
        {
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
            if (varTypeIsSIMD(lvaGetDesc(objRef->AsLclVar())))
            {
                setLclRelatedToSIMDIntrinsic(objRef);
            }
        }

        byrefNode = gtCloneExpr(tree->AsField()->GetFldObj());
        assert(byrefNode != nullptr);
        offset = tree->AsField()->gtFldOffset;
    }
    else
    {
        assert(tree->OperIs(GT_IND) && tree->AsIndir()->Addr()->OperIs(GT_INDEX_ADDR));

        GenTreeIndexAddr* indexAddr = tree->AsIndir()->Addr()->AsIndexAddr();
        GenTree*          arrayRef  = indexAddr->Arr();
        GenTree*          index     = indexAddr->Index();
        assert(index->IsCnsIntOrI());

        GenTree* checkIndexExpr = nullptr;
        unsigned indexVal       = (unsigned)index->AsIntCon()->gtIconVal;
        offset                  = indexVal * genTypeSize(tree->TypeGet());

        // Generate the boundary check exception.
        // The length for boundary check should be the maximum index number which should be
        // (first argument's index number) + (how many array arguments we have) - 1
        // = indexVal + arrayElementsCount - 1
        unsigned arrayElementsCount = simdSize / genTypeSize(simdBaseType);
        checkIndexExpr              = gtNewIconNode(indexVal + arrayElementsCount - 1);
        GenTreeArrLen*    arrLen    = gtNewArrLen(TYP_INT, arrayRef, (int)OFFSETOF__CORINFO_Array__length, compCurBB);
        GenTreeBoundsChk* arrBndsChk =
            new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(checkIndexExpr, arrLen, SCK_ARG_RNG_EXCPN);

        offset += OFFSETOF__CORINFO_Array__data;
        byrefNode = gtNewOperNode(GT_COMMA, arrayRef->TypeGet(), arrBndsChk, gtCloneExpr(arrayRef));
    }

    GenTree* address = byrefNode;
    if (offset != 0)
    {
        address = gtNewOperNode(GT_ADD, TYP_BYREF, address, gtNewIconNode(offset, TYP_I_IMPL));
    }

    return address;
}

//-------------------------------------------------------------------------------
// impMarkContiguousSIMDFieldAssignments: Try to identify if there are contiguous
// assignments from SIMD field to memory. If there are, then mark the related
// lclvar so that it won't be promoted.
//
// Arguments:
//      stmt - GenTree*. Input statement node.
//
void Compiler::impMarkContiguousSIMDFieldAssignments(Statement* stmt)
{
    if (opts.OptimizationDisabled())
    {
        return;
    }
    GenTree* expr = stmt->GetRootNode();
    if (expr->OperGet() == GT_ASG && expr->TypeGet() == TYP_FLOAT)
    {
        GenTree*    curDst          = expr->AsOp()->gtOp1;
        GenTree*    curSrc          = expr->AsOp()->gtOp2;
        unsigned    index           = 0;
        CorInfoType simdBaseJitType = CORINFO_TYPE_UNDEF;
        unsigned    simdSize        = 0;
        GenTree*    srcSimdLclAddr  = getSIMDStructFromField(curSrc, &simdBaseJitType, &index, &simdSize, true);

        if (srcSimdLclAddr == nullptr || simdBaseJitType != CORINFO_TYPE_FLOAT)
        {
            fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
        }
        else if (index == 0)
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
                    setLclRelatedToSIMDIntrinsic(srcSimdLclAddr);

                    if (curDst->OperIs(GT_FIELD) && curDst->AsField()->IsInstance())
                    {
                        GenTree* objRef = curDst->AsField()->GetFldObj();
                        if (objRef->OperIs(GT_LCL_VAR_ADDR) && varTypeIsStruct(lvaGetDesc(objRef->AsLclVar())))
                        {
                            setLclRelatedToSIMDIntrinsic(objRef);
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
#endif // FEATURE_SIMD
