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
    unsigned  sizeBytes = 0;
    var_types baseType  = getBaseTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
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
    else if (size == 32)
    {
        return 32;
    }
    else
    {
        assert(size == 64);
        return 64;
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

#ifdef TARGET_ARM64
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
unsigned Compiler::getFFRegisterVarNum()
{
    if (lvaFfrRegister == BAD_VAR_NUM)
    {
        lvaFfrRegister                  = lvaGrabTemp(false DEBUGARG("Save the FFR value."));
        lvaTable[lvaFfrRegister].lvType = TYP_MASK;
    }
    return lvaFfrRegister;
}
#endif

var_types Compiler::getBaseTypeForPrimitiveNumericClass(CORINFO_CLASS_HANDLE cls)
{
    CorInfoType jitType = info.compCompHnd->getTypeForPrimitiveNumericClass(cls);
    if (jitType == CORINFO_TYPE_UNDEF)
    {
        return TYP_UNDEF;
    }
    return JitType2PreciseVarType(jitType);
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
//    If the size of the struct is already known call structMightRepresentSIMDType
//    to determine if this api needs to be called.
//
//    The type handle passed here can only be used in a subset of JIT-EE calls
//    since it may be called by promotion during AOT of a method that does
//    not version with SPC. See CORINFO_TYPE_LAYOUT_NODE for the contract on
//    the supported JIT-EE calls.
//
// TODO-Throughput: current implementation parses class name to find base type. Change
//         this when we implement  SIMD intrinsic identification for the final
//         product.
//
var_types Compiler::getBaseTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd, unsigned* sizeBytes /*= nullptr */)
{
    if (sizeBytes != nullptr)
    {
        *sizeBytes = 0;
    }

    if ((typeHnd == nullptr) || !isIntrinsicType(typeHnd))
    {
        return TYP_UNDEF;
    }

    const char* namespaceName;
    const char* className = getClassNameFromMetadata(typeHnd, &namespaceName);

    var_types simdBaseType = TYP_UNDEF;
    unsigned  size         = 0;

    if (isNumericsNamespace(namespaceName))
    {
        switch (className[0])
        {
            case 'P':
            {
                if (strcmp(className, "Plane") != 0)
                {
                    return TYP_UNDEF;
                }

                JITDUMP("  Known type Plane\n");

                simdBaseType = TYP_FLOAT;
                size         = 4 * genTypeSize(TYP_FLOAT);
                break;
            }

            case 'Q':
            {
                if (strcmp(className, "Quaternion") != 0)
                {
                    return TYP_UNDEF;
                }

                JITDUMP("  Known type Quaternion\n");

                simdBaseType = TYP_FLOAT;
                size         = 4 * genTypeSize(TYP_FLOAT);
                break;
            }

            case 'V':
            {
                if (strncmp(className, "Vector", 6) != 0)
                {
                    return TYP_UNDEF;
                }

                switch (className[6])
                {
                    case '\0':
                    {
                        JITDUMP(" Found type Vector\n");
                        break;
                    }

                    case '2':
                    {
                        if (className[7] != '\0')
                        {
                            return TYP_UNDEF;
                        }

                        JITDUMP(" Found Vector2\n");

                        simdBaseType = TYP_FLOAT;
                        size         = 2 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '3':
                    {
                        if (className[7] != '\0')
                        {
                            return TYP_UNDEF;
                        }

                        JITDUMP(" Found Vector3\n");

                        simdBaseType = TYP_FLOAT;
                        size         = 3 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '4':
                    {
                        if (className[7] != '\0')
                        {
                            return TYP_UNDEF;
                        }

                        JITDUMP(" Found Vector4\n");

                        simdBaseType = TYP_FLOAT;
                        size         = 4 * genTypeSize(TYP_FLOAT);
                        break;
                    }

                    case '`':
                    {
                        if ((className[7] != '1') || (className[8] != '\0'))
                        {
                            return TYP_UNDEF;
                        }

                        CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                        simdBaseType                    = getBaseTypeForPrimitiveNumericClass(typeArgHnd);

                        if ((simdBaseType < TYP_BYTE) || (simdBaseType > TYP_DOUBLE))
                        {
                            return TYP_UNDEF;
                        }

                        JITDUMP(" Found Vector<%s>\n", varTypeName(simdBaseType));
                        size = getVectorTByteLength();

                        if (size == 0)
                        {
                            return TYP_UNDEF;
                        }
                        break;
                    }

                    default:
                    {
                        return TYP_UNDEF;
                    }
                }
                break;
            }

            default:
            {
                return TYP_UNDEF;
            }
        }
    }
#ifdef FEATURE_HW_INTRINSICS
    else
    {
        size = info.compCompHnd->getClassSize(typeHnd);

        switch (size)
        {
#if defined(TARGET_ARM64)
            case 8:
            {
                if (strcmp(className, "Vector64`1") != 0)
                {
                    return TYP_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseType                    = getBaseTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseType < TYP_BYTE) || (simdBaseType > TYP_DOUBLE))
                {
                    return TYP_UNDEF;
                }

                JITDUMP(" Found Vector64<%s>\n", varTypeName(simdBaseType));
                break;
            }
#endif // TARGET_ARM64

            case 16:
            {
                if (strcmp(className, "Vector128`1") != 0)
                {
                    return TYP_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseType                    = getBaseTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseType < TYP_BYTE) || (simdBaseType > TYP_DOUBLE))
                {
                    return TYP_UNDEF;
                }

                JITDUMP(" Found Vector128<%s>\n", varTypeName(simdBaseType));
                break;
            }

#if defined(TARGET_XARCH)
            case 32:
            {
                if (strcmp(className, "Vector256`1") != 0)
                {
                    return TYP_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseType                    = getBaseTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseType < TYP_BYTE) || (simdBaseType > TYP_DOUBLE))
                {
                    return TYP_UNDEF;
                }

                if (!compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    // We must treat as a regular struct if AVX isn't supported
                    return TYP_UNDEF;
                }

                JITDUMP(" Found Vector256<%s>\n", varTypeName(simdBaseType));
                break;
            }

            case 64:
            {
                if (strcmp(className, "Vector512`1") != 0)
                {
                    return TYP_UNDEF;
                }

                CORINFO_CLASS_HANDLE typeArgHnd = info.compCompHnd->getTypeInstantiationArgument(typeHnd, 0);
                simdBaseType                    = getBaseTypeForPrimitiveNumericClass(typeArgHnd);

                if ((simdBaseType < TYP_BYTE) || (simdBaseType > TYP_DOUBLE))
                {
                    return TYP_UNDEF;
                }

                if (!compOpportunisticallyDependsOn(InstructionSet_AVX512))
                {
                    // We must treat as a regular struct if AVX512 isn't supported
                    return TYP_UNDEF;
                }

                JITDUMP(" Found Vector512<%s>\n", varTypeName(simdBaseType));
                break;
            }
#endif // TARGET_XARCH

            default:
            {
                return TYP_UNDEF;
            }
        }
    }
#endif // FEATURE_HW_INTRINSICS

    if (sizeBytes != nullptr)
    {
        *sizeBytes = size;
    }

    if (simdBaseType != TYP_UNDEF)
    {
        assert(size == info.compCompHnd->getClassSize(typeHnd));
        setUsesSIMDTypes(true);
    }

    return simdBaseType;
}

//------------------------------------------------------------------------
// impSIMDPopStack: Pop a SIMD value from the importer's stack.
//
// Spills calls with return buffers to temps.
//
GenTree* Compiler::impSIMDPopStack()
{
    StackEntry se   = impPopStack();
    GenTree*   tree = se.val;

    assert(varTypeIsSIMDOrMask(tree));

    // Handle calls that may return the struct via a return buffer.
    if (tree->OperIs(GT_CALL, GT_RET_EXPR))
    {
        tree = impNormStructVal(tree, CHECK_SPILL_ALL);
    }

    return tree;
}

#endif // FEATURE_SIMD
