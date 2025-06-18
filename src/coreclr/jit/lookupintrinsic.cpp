#include "corhdr.h"
#include "corjit.h"

#include "lookupintrinsic.h"

#include <assert.h>

// FIXME
#ifndef JITDUMP
#define JITDUMP(...)
#endif

//------------------------------------------------------------------------
// lookupNamedIntrinsic: map method to jit named intrinsic value
//
// Arguments:
//    method -- method handle for method
//
// Return Value:
//    Id for the named intrinsic, or Illegal if none.
//
// Notes:
//    method should have CORINFO_FLG_INTRINSIC set in its attributes,
//    otherwise it is not a named jit intrinsic.
//
NamedIntrinsic NamedIntrinsicLookup::lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method)
{
    const char* className              = nullptr;
    const char* namespaceName          = nullptr;
    const char* enclosingClassNames[2] = {nullptr};
    const char* methodName = m_compHnd->getMethodNameFromMetadata(
        method, &className, &namespaceName, enclosingClassNames, 2
    );

    JITDUMP("Named Intrinsic ");

    if (namespaceName != nullptr)
    {
        JITDUMP("%s.", namespaceName);
    }
    if (enclosingClassNames[1] != nullptr)
    {
        JITDUMP("%s.", enclosingClassNames[1]);
    }
    if (enclosingClassNames[0] != nullptr)
    {
        JITDUMP("%s.", enclosingClassNames[0]);
    }
    if (className != nullptr)
    {
        JITDUMP("%s.", className);
    }
    if (methodName != nullptr)
    {
        JITDUMP("%s", methodName);
    }

    if ((namespaceName == nullptr) || (className == nullptr) || (methodName == nullptr))
    {
        // Check if we are dealing with an MD array's known runtime method
        CorInfoArrayIntrinsic arrayFuncIndex = m_compHnd->getArrayIntrinsicID(method);
        switch (arrayFuncIndex)
        {
            case CorInfoArrayIntrinsic::GET:
                JITDUMP("ARRAY_FUNC_GET: Recognized\n");
                return NI_Array_Get;
            case CorInfoArrayIntrinsic::SET:
                JITDUMP("ARRAY_FUNC_SET: Recognized\n");
                return NI_Array_Set;
            case CorInfoArrayIntrinsic::ADDRESS:
                JITDUMP("ARRAY_FUNC_ADDRESS: Recognized\n");
                return NI_Array_Address;
            default:
                break;
        }

        JITDUMP(": Not recognized, not enough metadata\n");
        return NI_Illegal;
    }

    JITDUMP(": ");

    NamedIntrinsic result = NI_Illegal;

    if (strncmp(namespaceName, "System", 6) == 0)
    {
        namespaceName += 6;

        if (namespaceName[0] == '\0')
        {
            switch (className[0])
            {
                case 'A':
                {
                    if (strcmp(className, "Activator") == 0)
                    {
                        if (strcmp(methodName, "AllocatorOf") == 0)
                        {
                            result = NI_System_Activator_AllocatorOf;
                        }
                        else if (strcmp(methodName, "DefaultConstructorOf") == 0)
                        {
                            result = NI_System_Activator_DefaultConstructorOf;
                        }
                    }
                    else if (strcmp(className, "ArgumentNullException") == 0)
                    {
                        if (strcmp(methodName, "ThrowIfNull") == 0)
                        {
                            result = NI_System_ArgumentNullException_ThrowIfNull;
                        }
                    }
                    else if (strcmp(className, "Array") == 0)
                    {
                        if (strcmp(methodName, "Clone") == 0)
                        {
                            result = NI_System_Array_Clone;
                        }
                        else if (strcmp(methodName, "GetLength") == 0)
                        {
                            result = NI_System_Array_GetLength;
                        }
                        else if (strcmp(methodName, "GetLowerBound") == 0)
                        {
                            result = NI_System_Array_GetLowerBound;
                        }
                        else if (strcmp(methodName, "GetUpperBound") == 0)
                        {
                            result = NI_System_Array_GetUpperBound;
                        }
                    }
                    else if (strcmp(className, "Array`1") == 0)
                    {
                        if (strcmp(methodName, "GetEnumerator") == 0)
                        {
                            result = NI_System_Array_T_GetEnumerator;
                        }
                    }
                    break;
                }

                case 'B':
                {
                    if (strcmp(className, "BitConverter") == 0)
                    {
                        if (strcmp(methodName, "DoubleToInt64Bits") == 0)
                        {
                            result = NI_System_BitConverter_DoubleToInt64Bits;
                        }
                        else if (strcmp(methodName, "DoubleToUInt64Bits") == 0)
                        {
                            result = NI_System_BitConverter_DoubleToInt64Bits;
                        }
                        else if (strcmp(methodName, "Int32BitsToSingle") == 0)
                        {
                            result = NI_System_BitConverter_Int32BitsToSingle;
                        }
                        else if (strcmp(methodName, "Int64BitsToDouble") == 0)
                        {
                            result = NI_System_BitConverter_Int64BitsToDouble;
                        }
                        else if (strcmp(methodName, "SingleToInt32Bits") == 0)
                        {
                            result = NI_System_BitConverter_SingleToInt32Bits;
                        }
                        else if (strcmp(methodName, "SingleToUInt32Bits") == 0)
                        {
                            result = NI_System_BitConverter_SingleToInt32Bits;
                        }
                        else if (strcmp(methodName, "UInt32BitsToSingle") == 0)
                        {
                            result = NI_System_BitConverter_Int32BitsToSingle;
                        }
                        else if (strcmp(methodName, "UInt64BitsToDouble") == 0)
                        {
                            result = NI_System_BitConverter_Int64BitsToDouble;
                        }
                    }
                    break;
                }

                case 'D':
                {
                    if (strcmp(className, "Double") == 0)
                    {
                        result = lookupPrimitiveFloatNamedIntrinsic(method, methodName);
                    }
                    break;
                }

                case 'E':
                {
                    if (strcmp(className, "Enum") == 0)
                    {
                        if (strcmp(methodName, "HasFlag") == 0)
                        {
                            result = NI_System_Enum_HasFlag;
                        }
                    }
                    break;
                }

                case 'G':
                {
                    if (strcmp(className, "GC") == 0)
                    {
                        if (strcmp(methodName, "KeepAlive") == 0)
                        {
                            result = NI_System_GC_KeepAlive;
                        }
                    }
                    break;
                }

                case 'I':
                {
                    if ((strcmp(className, "Int32") == 0) || (strcmp(className, "Int64") == 0) ||
                        (strcmp(className, "IntPtr") == 0))
                    {
                        result = lookupPrimitiveIntNamedIntrinsic(method, methodName);
                    }
                    break;
                }

                case 'M':
                {
                    if ((strcmp(className, "Math") == 0) || (strcmp(className, "MathF") == 0))
                    {
                        result = lookupPrimitiveFloatNamedIntrinsic(method, methodName);
                    }
                    else if (strcmp(className, "MemoryExtensions") == 0)
                    {
                        if (strcmp(methodName, "AsSpan") == 0)
                        {
                            result = NI_System_MemoryExtensions_AsSpan;
                        }
                        else if (strcmp(methodName, "Equals") == 0)
                        {
                            result = NI_System_MemoryExtensions_Equals;
                        }
                        else if (strcmp(methodName, "SequenceEqual") == 0)
                        {
                            result = NI_System_MemoryExtensions_SequenceEqual;
                        }
                        else if (strcmp(methodName, "StartsWith") == 0)
                        {
                            result = NI_System_MemoryExtensions_StartsWith;
                        }
                        else if (strcmp(methodName, "EndsWith") == 0)
                        {
                            result = NI_System_MemoryExtensions_EndsWith;
                        }
                    }
                    break;
                }

                case 'O':
                {
                    if (strcmp(className, "Object") == 0)
                    {
                        if (strcmp(methodName, "GetType") == 0)
                        {
                            result = NI_System_Object_GetType;
                        }
                        else if (strcmp(methodName, "MemberwiseClone") == 0)
                        {
                            result = NI_System_Object_MemberwiseClone;
                        }
                    }
                    break;
                }

                case 'R':
                {
                    if (strcmp(className, "ReadOnlySpan`1") == 0)
                    {
                        if (strcmp(methodName, "get_Item") == 0)
                        {
                            result = NI_System_ReadOnlySpan_get_Item;
                        }
                        else if (strcmp(methodName, "get_Length") == 0)
                        {
                            result = NI_System_ReadOnlySpan_get_Length;
                        }
                    }
                    else if (strcmp(className, "RuntimeType") == 0)
                    {
                        if (strcmp(methodName, "get_IsActualEnum") == 0)
                        {
                            result = NI_System_Type_get_IsEnum;
                        }
                        if (strcmp(methodName, "get_TypeHandle") == 0)
                        {
                            result = NI_System_RuntimeType_get_TypeHandle;
                        }
                    }
                    else if (strcmp(className, "RuntimeTypeHandle") == 0)
                    {
                        if (strcmp(methodName, "ToIntPtr") == 0)
                        {
                            result = NI_System_RuntimeTypeHandle_ToIntPtr;
                        }
                    }
                    break;
                }

                case 'S':
                {
                    if (strcmp(className, "Single") == 0)
                    {
                        result = lookupPrimitiveFloatNamedIntrinsic(method, methodName);
                    }
                    else if (strcmp(className, "Span`1") == 0)
                    {
                        if (strcmp(methodName, "get_Item") == 0)
                        {
                            result = NI_System_Span_get_Item;
                        }
                        else if (strcmp(methodName, "get_Length") == 0)
                        {
                            result = NI_System_Span_get_Length;
                        }
                    }
                    else if (strcmp(className, "SpanHelpers") == 0)
                    {
                        if (strcmp(methodName, "SequenceEqual") == 0)
                        {
                            result = NI_System_SpanHelpers_SequenceEqual;
                        }
                        else if (strcmp(methodName, "Fill") == 0)
                        {
                            result = NI_System_SpanHelpers_Fill;
                        }
                        else if (strcmp(methodName, "ClearWithoutReferences") == 0)
                        {
                            result = NI_System_SpanHelpers_ClearWithoutReferences;
                        }
                        else if (strcmp(methodName, "Memmove") == 0)
                        {
                            result = NI_System_SpanHelpers_Memmove;
                        }
                    }
                    else if (strcmp(className, "String") == 0)
                    {
                        if (strcmp(methodName, "Equals") == 0)
                        {
                            result = NI_System_String_Equals;
                        }
                        else if (strcmp(methodName, "get_Chars") == 0)
                        {
                            result = NI_System_String_get_Chars;
                        }
                        else if (strcmp(methodName, "get_Length") == 0)
                        {
                            result = NI_System_String_get_Length;
                        }
                        else if (strcmp(methodName, "op_Implicit") == 0)
                        {
                            result = NI_System_String_op_Implicit;
                        }
                        else if (strcmp(methodName, "StartsWith") == 0)
                        {
                            result = NI_System_String_StartsWith;
                        }
                        else if (strcmp(methodName, "EndsWith") == 0)
                        {
                            result = NI_System_String_EndsWith;
                        }
                    }
                    else if (strcmp(className, "SZArrayHelper") == 0)
                    {
                        if (strcmp(methodName, "GetEnumerator") == 0)
                        {
                            result = NI_System_SZArrayHelper_GetEnumerator;
                        }
                    }

                    break;
                }

                case 'T':
                {
                    if (strcmp(className, "Type") == 0)
                    {
                        if (strcmp(methodName, "get_IsEnum") == 0)
                        {
                            result = NI_System_Type_get_IsEnum;
                        }
                        else if (strcmp(methodName, "get_IsValueType") == 0)
                        {
                            result = NI_System_Type_get_IsValueType;
                        }
                        else if (strcmp(methodName, "get_IsPrimitive") == 0)
                        {
                            result = NI_System_Type_get_IsPrimitive;
                        }
                        else if (strcmp(methodName, "get_IsGenericType") == 0)
                        {
                            result = NI_System_Type_get_IsGenericType;
                        }
                        else if (strcmp(methodName, "get_IsByRefLike") == 0)
                        {
                            result = NI_System_Type_get_IsByRefLike;
                        }
                        else if (strcmp(methodName, "GetEnumUnderlyingType") == 0)
                        {
                            result = NI_System_Type_GetEnumUnderlyingType;
                        }
                        else if (strcmp(methodName, "GetTypeFromHandle") == 0)
                        {
                            result = NI_System_Type_GetTypeFromHandle;
                        }
                        else if (strcmp(methodName, "GetGenericTypeDefinition") == 0)
                        {
                            result = NI_System_Type_GetGenericTypeDefinition;
                        }
                        else if (strcmp(methodName, "IsAssignableFrom") == 0)
                        {
                            result = NI_System_Type_IsAssignableFrom;
                        }
                        else if (strcmp(methodName, "IsAssignableTo") == 0)
                        {
                            result = NI_System_Type_IsAssignableTo;
                        }
                        else if (strcmp(methodName, "op_Equality") == 0)
                        {
                            result = NI_System_Type_op_Equality;
                        }
                        else if (strcmp(methodName, "op_Inequality") == 0)
                        {
                            result = NI_System_Type_op_Inequality;
                        }
                        else if (strcmp(methodName, "get_TypeHandle") == 0)
                        {
                            result = NI_System_Type_get_TypeHandle;
                        }
                    }
                    break;
                }

                case 'U':
                {
                    if ((strcmp(className, "UInt32") == 0) || (strcmp(className, "UInt64") == 0) ||
                        (strcmp(className, "UIntPtr") == 0))
                    {
                        result = lookupPrimitiveIntNamedIntrinsic(method, methodName);
                    }
                    break;
                }

                default:
                    break;
            }
        }
        else if (namespaceName[0] == '.')
        {
            namespaceName += 1;

#if defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            if (strcmp(namespaceName, "Buffers.Binary") == 0)
            {
                if (strcmp(className, "BinaryPrimitives") == 0)
                {
                    if (strcmp(methodName, "ReverseEndianness") == 0)
                    {
                        if (m_Zbb)
                        {
                            result = NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness;
                        }
                    }
                }
            }
            else
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
                if (strcmp(namespaceName, "Collections.Generic") == 0)
                {
                    if (strcmp(className, "Comparer`1") == 0)
                    {
                        if (strcmp(methodName, "get_Default") == 0)
                        {
                            result = NI_System_Collections_Generic_Comparer_get_Default;
                        }
                    }
                    else if (strcmp(className, "EqualityComparer`1") == 0)
                    {
                        if (strcmp(methodName, "get_Default") == 0)
                        {
                            result = NI_System_Collections_Generic_EqualityComparer_get_Default;
                        }
                    }
                    else if (strcmp(className, "IEnumerable`1") == 0)
                    {
                        if (strcmp(methodName, "GetEnumerator") == 0)
                        {
                            result = NI_System_Collections_Generic_IEnumerable_GetEnumerator;
                        }
                    }
                }
                else if (strcmp(namespaceName, "Numerics") == 0)
                {
                    if (strcmp(className, "BitOperations") == 0)
                    {
                        result = lookupPrimitiveIntNamedIntrinsic(method, methodName);
                    }
                    else
                    {
#ifdef FEATURE_HW_INTRINSICS
                        bool isVectorT = strcmp(className, "Vector`1") == 0;

                        if (isVectorT || (strcmp(className, "Vector") == 0))
                        {
                            if (strncmp(methodName, "System.Runtime.Intrinsics.ISimdVector<System.Numerics.Vector",
                                        60) == 0)
                            {
                                // We want explicitly implemented ISimdVector<TSelf, T> APIs to still be expanded where
                                // possible but, they all prefix the qualified name of the interface first, so we'll
                                // check for that and skip the prefix before trying to resolve the method.

                                if (strncmp(methodName + 60, "<T>,T>.", 7) == 0)
                                {
                                    methodName += 67;
                                }
                            }

                            uint32_t size = m_vectorTByteLength;
                            assert((size == 16) || (size == 32) || (size == 64));

                            const char* lookupClassName = className;

                            switch (size)
                            {
                                case 16:
                                {
                                    lookupClassName = isVectorT ? "Vector128`1" : "Vector128";
                                    break;
                                }

                                case 32:
                                {
                                    lookupClassName = isVectorT ? "Vector256`1" : "Vector256";
                                    break;
                                }

                                case 64:
                                {
                                    lookupClassName = isVectorT ? "Vector512`1" : "Vector512";
                                    break;
                                }

                                default:
                                {
                                    unreached();
                                }
                            }

                            const char* lookupMethodName = methodName;

                            if ((strncmp(methodName, "As", 2) == 0) && (methodName[2] != '\0'))
                            {
                                if (strncmp(methodName + 2, "Vector", 6) == 0)
                                {
                                    if (strcmp(methodName + 8, "Byte") == 0)
                                    {
                                        lookupMethodName = "AsByte";
                                    }
                                    else if (strcmp(methodName + 8, "Double") == 0)
                                    {
                                        lookupMethodName = "AsDouble";
                                    }
                                    else if (strcmp(methodName + 8, "Int16") == 0)
                                    {
                                        lookupMethodName = "AsInt16";
                                    }
                                    else if (strcmp(methodName + 8, "Int32") == 0)
                                    {
                                        lookupMethodName = "AsInt32";
                                    }
                                    else if (strcmp(methodName + 8, "Int64") == 0)
                                    {
                                        lookupMethodName = "AsInt64";
                                    }
                                    else if (strcmp(methodName + 8, "NInt") == 0)
                                    {
                                        lookupMethodName = "AsNInt";
                                    }
                                    else if (strcmp(methodName + 8, "NUInt") == 0)
                                    {
                                        lookupMethodName = "AsNUInt";
                                    }
                                    else if (strcmp(methodName + 8, "SByte") == 0)
                                    {
                                        lookupMethodName = "AsSByte";
                                    }
                                    else if (strcmp(methodName + 8, "Single") == 0)
                                    {
                                        lookupMethodName = "AsSingle";
                                    }
                                    else if (strcmp(methodName + 8, "UInt16") == 0)
                                    {
                                        lookupMethodName = "AsUInt16";
                                    }
                                    else if (strcmp(methodName + 8, "UInt32") == 0)
                                    {
                                        lookupMethodName = "AsUInt32";
                                    }
                                    else if (strcmp(methodName + 8, "UInt64") == 0)
                                    {
                                        lookupMethodName = "AsUInt64";
                                    }
                                }

                                if (lookupMethodName == methodName)
                                {
                                    // There are several other As prefixed APIs
                                    // which represent extension methods for
                                    // Vector2/3/4 Matrix4x4, Quaternion, or Plane
                                    lookupMethodName = nullptr;
                                }
                            }

                            if (lookupMethodName != nullptr)
                            {
                                CORINFO_SIG_INFO sig;
                                m_compHnd->getMethodSig(method, &sig);

                                if (m_lookupHWNamedIntrinsic)
                                    result = m_lookupHWNamedIntrinsic(m_context, &sig, lookupClassName, lookupMethodName,
                                                                    enclosingClassNames[0], enclosingClassNames[1]);
                                else
                                    result = NI_Illegal;
                            }
                        }
#endif // FEATURE_HW_INTRINSICS

                        if (result == NI_Illegal)
                        {
                            // This allows the relevant code paths to be dropped as dead code even
                            // on platforms where FEATURE_HW_INTRINSICS is not supported.

                            if (strcmp(methodName, "get_IsSupported") == 0)
                            {
                                assert(strcmp(className, "Vector`1") == 0);
                                result = NI_IsSupported_Type;
                            }
                            else if (strcmp(methodName, "get_IsHardwareAccelerated") == 0)
                            {
                                result = NI_IsSupported_False;
                            }
                            else if (strcmp(methodName, "get_Count") == 0)
                            {
                                assert(strcmp(className, "Vector`1") == 0);
                                result = NI_Vector_GetCount;
                            }
                            else if (method == m_compMethod)
                            {
                                // For the framework itself, any recursive intrinsics will either be
                                // only supported on a single platform or will be guarded by a relevant
                                // IsSupported check so the throw PNSE will be valid or dropped.

                                result = NI_Throw_PlatformNotSupportedException;
                            }
                            else
                            {
                                // Otherwise mark this as a general intrinsic in the namespace
                                // so we can still get the inlining profitability boost.
                                result = NI_System_Numerics_Intrinsic;
                            }
                        }
                    }
                }
                else if (strncmp(namespaceName, "Runtime.", 8) == 0)
                {
                    namespaceName += 8;

                    if (strcmp(namespaceName, "CompilerServices") == 0)
                    {
                        if (strcmp(className, "RuntimeHelpers") == 0)
                        {
                            if (strcmp(methodName, "CreateSpan") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_RuntimeHelpers_CreateSpan;
                            }
                            else if (strcmp(methodName, "InitializeArray") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray;
                            }
                            else if (strcmp(methodName, "IsKnownConstant") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_RuntimeHelpers_IsKnownConstant;
                            }
                            else if (strcmp(methodName, "IsReferenceOrContainsReferences") == 0)
                            {
                                result =
                                    NI_System_Runtime_CompilerServices_RuntimeHelpers_IsReferenceOrContainsReferences;
                            }
                            else if (strcmp(methodName, "GetMethodTable") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_RuntimeHelpers_GetMethodTable;
                            }
                        }
                        else if (strcmp(className, "AsyncHelpers") == 0)
                        {
                            if (strcmp(methodName, "AsyncSuspend") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_AsyncHelpers_AsyncSuspend;
                            }
                            else if (strcmp(methodName, "Await") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_AsyncHelpers_Await;
                            }
                        }
                        else if (strcmp(className, "StaticsHelpers") == 0)
                        {
                            if (strcmp(methodName, "VolatileReadAsByref") == 0)
                            {
                                result = NI_System_Runtime_CompilerServices_StaticsHelpers_VolatileReadAsByref;
                            }
                        }
                        else if (strcmp(className, "Unsafe") == 0)
                        {
                            if (strcmp(methodName, "Add") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Add;
                            }
                            else if (strcmp(methodName, "AddByteOffset") == 0)
                            {
                                result = NI_SRCS_UNSAFE_AddByteOffset;
                            }
                            else if (strcmp(methodName, "AreSame") == 0)
                            {
                                result = NI_SRCS_UNSAFE_AreSame;
                            }
                            else if (strcmp(methodName, "As") == 0)
                            {
                                result = NI_SRCS_UNSAFE_As;
                            }
                            else if (strcmp(methodName, "AsPointer") == 0)
                            {
                                result = NI_SRCS_UNSAFE_AsPointer;
                            }
                            else if (strcmp(methodName, "AsRef") == 0)
                            {
                                result = NI_SRCS_UNSAFE_AsRef;
                            }
                            else if (strcmp(methodName, "BitCast") == 0)
                            {
                                result = NI_SRCS_UNSAFE_BitCast;
                            }
                            else if (strcmp(methodName, "ByteOffset") == 0)
                            {
                                result = NI_SRCS_UNSAFE_ByteOffset;
                            }
                            else if (strcmp(methodName, "Copy") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Copy;
                            }
                            else if (strcmp(methodName, "CopyBlock") == 0)
                            {
                                result = NI_SRCS_UNSAFE_CopyBlock;
                            }
                            else if (strcmp(methodName, "CopyBlockUnaligned") == 0)
                            {
                                result = NI_SRCS_UNSAFE_CopyBlockUnaligned;
                            }
                            else if (strcmp(methodName, "InitBlock") == 0)
                            {
                                result = NI_SRCS_UNSAFE_InitBlock;
                            }
                            else if (strcmp(methodName, "InitBlockUnaligned") == 0)
                            {
                                result = NI_SRCS_UNSAFE_InitBlockUnaligned;
                            }
                            else if (strcmp(methodName, "IsAddressGreaterThan") == 0)
                            {
                                result = NI_SRCS_UNSAFE_IsAddressGreaterThan;
                            }
                            else if (strcmp(methodName, "IsAddressLessThan") == 0)
                            {
                                result = NI_SRCS_UNSAFE_IsAddressLessThan;
                            }
                            else if (strcmp(methodName, "IsNullRef") == 0)
                            {
                                result = NI_SRCS_UNSAFE_IsNullRef;
                            }
                            else if (strcmp(methodName, "NullRef") == 0)
                            {
                                result = NI_SRCS_UNSAFE_NullRef;
                            }
                            else if (strcmp(methodName, "Read") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Read;
                            }
                            else if (strcmp(methodName, "ReadUnaligned") == 0)
                            {
                                result = NI_SRCS_UNSAFE_ReadUnaligned;
                            }
                            else if (strcmp(methodName, "SizeOf") == 0)
                            {
                                result = NI_SRCS_UNSAFE_SizeOf;
                            }
                            else if (strcmp(methodName, "SkipInit") == 0)
                            {
                                result = NI_SRCS_UNSAFE_SkipInit;
                            }
                            else if (strcmp(methodName, "Subtract") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Subtract;
                            }
                            else if (strcmp(methodName, "SubtractByteOffset") == 0)
                            {
                                result = NI_SRCS_UNSAFE_SubtractByteOffset;
                            }
                            else if (strcmp(methodName, "Unbox") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Unbox;
                            }
                            else if (strcmp(methodName, "Write") == 0)
                            {
                                result = NI_SRCS_UNSAFE_Write;
                            }
                            else if (strcmp(methodName, "WriteUnaligned") == 0)
                            {
                                result = NI_SRCS_UNSAFE_WriteUnaligned;
                            }
                        }
                    }
                    else if (strcmp(namespaceName, "InteropServices") == 0)
                    {
                        if (strcmp(className, "MemoryMarshal") == 0)
                        {
                            if (strcmp(methodName, "GetArrayDataReference") == 0)
                            {
                                result = NI_System_Runtime_InteropService_MemoryMarshal_GetArrayDataReference;
                            }
                        }
                    }
                    else if (strncmp(namespaceName, "Intrinsics", 10) == 0)
                    {
                        // We go down this path even when FEATURE_HW_INTRINSICS isn't enabled
                        // so we can specially handle IsSupported and recursive calls.

                        // This is required to appropriately handle the intrinsics on platforms
                        // which don't support them. On such a platform methods like Vector64.Create
                        // will be seen as `Intrinsic` and `mustExpand` due to having a code path
                        // which is recursive. When such a path is hit we expect it to be handled by
                        // the importer and we fire an assert if it wasn't and in previous versions
                        // of the JIT would fail fast. This was changed to throw a PNSE instead but
                        // we still assert as most intrinsics should have been recognized/handled.

                        // In order to avoid the assert, we specially handle the IsSupported checks
                        // (to better allow dead-code optimizations) and we explicitly throw a PNSE
                        // as we know that is the desired behavior for the HWIntrinsics when not
                        // supported. For cases like Vector64.Create, this is fine because it will
                        // be behind a relevant IsSupported check and will never be hit and the
                        // software fallback will be executed instead.

#ifdef FEATURE_HW_INTRINSICS
                        namespaceName += 10;
                        const char* platformNamespaceName;

#if defined(TARGET_XARCH)
                        platformNamespaceName = ".X86";
#elif defined(TARGET_ARM64)
                        platformNamespaceName = ".Arm";
#else
#error Unsupported platform
#endif

                        if (strncmp(methodName,
                                    "System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector", 70) == 0)
                        {
                            // We want explicitly implemented ISimdVector<TSelf, T> APIs to still be expanded where
                            // possible but, they all prefix the qualified name of the interface first, so we'll check
                            // for that and skip the prefix before trying to resolve the method.

                            if (strncmp(methodName + 70, "64<T>,T>.", 9) == 0)
                            {
                                methodName += 79;
                            }
                            else if ((strncmp(methodName + 70, "128<T>,T>.", 10) == 0) ||
                                     (strncmp(methodName + 70, "256<T>,T>.", 10) == 0) ||
                                     (strncmp(methodName + 70, "512<T>,T>.", 10) == 0))
                            {
                                methodName += 80;
                            }
                        }

                        if ((namespaceName[0] == '\0') || (strcmp(namespaceName, platformNamespaceName) == 0))
                        {
                            CORINFO_SIG_INFO sig;
                            m_compHnd->getMethodSig(method, &sig);

                            if (m_lookupHWNamedIntrinsic)
                                result = m_lookupHWNamedIntrinsic(m_context, &sig, className, methodName,
                                                                enclosingClassNames[0], enclosingClassNames[1]);
                            else
                                result = NI_Illegal;
                        }
#endif // FEATURE_HW_INTRINSICS

                        if (result == NI_Illegal)
                        {
                            // This allows the relevant code paths to be dropped as dead code even
                            // on platforms where FEATURE_HW_INTRINSICS is not supported.

                            if (strcmp(methodName, "get_IsSupported") == 0)
                            {
                                if (strncmp(className, "Vector", 6) == 0)
                                {
                                    assert((strcmp(className, "Vector64`1") == 0) ||
                                           (strcmp(className, "Vector128`1") == 0) ||
                                           (strcmp(className, "Vector256`1") == 0) ||
                                           (strcmp(className, "Vector512`1") == 0));

                                    result = NI_IsSupported_Type;
                                }
                                else
                                {
                                    result = NI_IsSupported_False;
                                }
                            }
                            else if (strcmp(methodName, "get_IsHardwareAccelerated") == 0)
                            {
                                result = NI_IsSupported_False;
                            }
                            else if (strcmp(methodName, "get_Count") == 0)
                            {
                                assert(
                                    (strcmp(className, "Vector64`1") == 0) || (strcmp(className, "Vector128`1") == 0) ||
                                    (strcmp(className, "Vector256`1") == 0) || (strcmp(className, "Vector512`1") == 0));

                                result = NI_Vector_GetCount;
                            }
                            else if (method == m_compMethod)
                            {
                                // For the framework itself, any recursive intrinsics will either be
                                // only supported on a single platform or will be guarded by a relevant
                                // IsSupported check so the throw PNSE will be valid or dropped.

                                result = NI_Throw_PlatformNotSupportedException;
                            }
                            else
                            {
                                // Otherwise mark this as a general intrinsic in the namespace
                                // so we can still get the inlining profitability boost.
                                result = NI_System_Runtime_Intrinsics_Intrinsic;
                            }
                        }
                    }
                }
                else if (strcmp(namespaceName, "StubHelpers") == 0)
                {
                    if (strcmp(className, "StubHelpers") == 0)
                    {
                        if (strcmp(methodName, "GetStubContext") == 0)
                        {
                            result = NI_System_StubHelpers_GetStubContext;
                        }
                        else if (strcmp(methodName, "NextCallReturnAddress") == 0)
                        {
                            result = NI_System_StubHelpers_NextCallReturnAddress;
                        }
                        else if (strcmp(methodName, "AsyncCallContinuation") == 0)
                        {
                            result = NI_System_StubHelpers_AsyncCallContinuation;
                        }
                    }
                }
                else if (strcmp(namespaceName, "Text") == 0)
                {
                    if (strcmp(className, "UTF8EncodingSealed") == 0)
                    {
                        if (strcmp(methodName, "ReadUtf8") == 0)
                        {
                            assert(strcmp(enclosingClassNames[0], "UTF8Encoding") == 0);
                            result = NI_System_Text_UTF8Encoding_UTF8EncodingSealed_ReadUtf8;
                        }
                    }
                }
                else if (strcmp(namespaceName, "Threading") == 0)
                {
                    if (strcmp(className, "Interlocked") == 0)
                    {
                        if (strcmp(methodName, "And") == 0)
                        {
                            result = NI_System_Threading_Interlocked_And;
                        }
                        else if (strcmp(methodName, "Or") == 0)
                        {
                            result = NI_System_Threading_Interlocked_Or;
                        }
                        else if (strcmp(methodName, "CompareExchange") == 0)
                        {
                            result = NI_System_Threading_Interlocked_CompareExchange;
                        }
                        else if (strcmp(methodName, "Exchange") == 0)
                        {
                            result = NI_System_Threading_Interlocked_Exchange;
                        }
                        else if (strcmp(methodName, "ExchangeAdd") == 0)
                        {
                            result = NI_System_Threading_Interlocked_ExchangeAdd;
                        }
                        else if (strcmp(methodName, "MemoryBarrier") == 0)
                        {
                            result = NI_System_Threading_Interlocked_MemoryBarrier;
                        }
                    }
                    else if (strcmp(className, "Thread") == 0)
                    {
                        if (strcmp(methodName, "get_CurrentThread") == 0)
                        {
                            result = NI_System_Threading_Thread_get_CurrentThread;
                        }
                        else if (strcmp(methodName, "get_ManagedThreadId") == 0)
                        {
                            result = NI_System_Threading_Thread_get_ManagedThreadId;
                        }
                        else if (strcmp(methodName, "FastPollGC") == 0)
                        {
                            result = NI_System_Threading_Thread_FastPollGC;
                        }
                    }
                    else if (strcmp(className, "Volatile") == 0)
                    {
                        if (strcmp(methodName, "Read") == 0)
                        {
                            result = NI_System_Threading_Volatile_Read;
                        }
                        else if (strcmp(methodName, "Write") == 0)
                        {
                            result = NI_System_Threading_Volatile_Write;
                        }
                        else if (strcmp(methodName, "ReadBarrier") == 0)
                        {
                            result = NI_System_Threading_Volatile_ReadBarrier;
                        }
                        else if (strcmp(methodName, "WriteBarrier") == 0)
                        {
                            result = NI_System_Threading_Volatile_WriteBarrier;
                        }
                    }
                }
                else if (strcmp(namespaceName, "Threading.Tasks") == 0)
                {
                    if (strcmp(methodName, "ConfigureAwait") == 0)
                    {
                        if (strcmp(className, "Task`1") == 0 || strcmp(className, "Task") == 0 ||
                            strcmp(className, "ValuTask`1") == 0 || strcmp(className, "ValueTask") == 0)
                        {
                            result = NI_System_Threading_Tasks_Task_ConfigureAwait;
                        }
                    }
                }
        }
    }
    else if (strcmp(namespaceName, "Internal.Runtime") == 0)
    {
        if (strcmp(className, "MethodTable") == 0)
        {
            if (strcmp(methodName, "Of") == 0)
            {
                result = NI_Internal_Runtime_MethodTable_Of;
            }
        }
    }

    if (result == NI_Illegal)
    {
        JITDUMP("Not recognized\n");
    }
    else if (result == NI_IsSupported_False)
    {
        JITDUMP("Unsupported - return false");
    }
    else if (result == NI_Throw_PlatformNotSupportedException)
    {
        JITDUMP("Unsupported - throw PlatformNotSupportedException");
    }
    else
    {
        JITDUMP("Recognized\n");
    }
    return result;
}

//------------------------------------------------------------------------
// lookupPrimitiveFloatNamedIntrinsic: map method to jit named intrinsic value
//
// Arguments:
//    method -- method handle for method
//
// Return Value:
//    Id for the named intrinsic, or Illegal if none.
//
// Notes:
//    method should have CORINFO_FLG_INTRINSIC set in its attributes,
//    otherwise it is not a named jit intrinsic.
//
NamedIntrinsic NamedIntrinsicLookup::lookupPrimitiveFloatNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName)
{
    NamedIntrinsic result = NI_Illegal;

    switch (methodName[0])
    {
        case 'A':
        {
            if (strcmp(methodName, "Abs") == 0)
            {
                result = NI_System_Math_Abs;
            }
            else if (strncmp(methodName, "Acos", 4) == 0)
            {
                methodName += 4;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Acos;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Acosh;
                    }
                }
            }
            else if (strncmp(methodName, "Asin", 4) == 0)
            {
                methodName += 4;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Asin;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Asinh;
                    }
                }
            }
            else if (strncmp(methodName, "Atan", 4) == 0)
            {
                methodName += 4;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Atan;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Atanh;
                    }
                    else if (methodName[0] == '2')
                    {
                        result = NI_System_Math_Atan2;
                    }
                }
            }
            break;
        }

        case 'C':
        {
            if (strcmp(methodName, "Cbrt") == 0)
            {
                result = NI_System_Math_Cbrt;
            }
            else if (strcmp(methodName, "Ceiling") == 0)
            {
                result = NI_System_Math_Ceiling;
            }
            else if (strncmp(methodName, "ConvertToInteger", 16) == 0)
            {
                methodName += 16;

                if (methodName[0] == '\0')
                {
                    result = NI_PRIMITIVE_ConvertToInteger;
                }
                else if (strcmp(methodName, "Native") == 0)
                {
                    result = NI_PRIMITIVE_ConvertToIntegerNative;
                }
            }
            else if (strncmp(methodName, "Cos", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Cos;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Cosh;
                    }
                }
            }
            break;
        }

        case 'E':
        {
            if (strcmp(methodName, "Exp") == 0)
            {
                result = NI_System_Math_Exp;
            }
            break;
        }

        case 'F':
        {
            if (strcmp(methodName, "Floor") == 0)
            {
                result = NI_System_Math_Floor;
            }
            else if (strcmp(methodName, "FusedMultiplyAdd") == 0)
            {
                result = NI_System_Math_FusedMultiplyAdd;
            }
            break;
        }

        case 'I':
        {
            if (strcmp(methodName, "ILogB") == 0)
            {
                result = NI_System_Math_ILogB;
            }
            break;
        }

        case 'L':
        {
            if (strncmp(methodName, "Log", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Log;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == '2')
                    {
                        result = NI_System_Math_Log2;
                    }
                }
                else if (strcmp(methodName, "10") == 0)
                {
                    result = NI_System_Math_Log10;
                }
            }
            break;
        }

        case 'M':
        {
            if (strncmp(methodName, "Max", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Max;
                }
                else if (strncmp(methodName, "Magnitude", 9) == 0)
                {
                    methodName += 9;

                    if (methodName[0] == '\0')
                    {
                        result = NI_System_Math_MaxMagnitude;
                    }
                    else if (strcmp(methodName, "Number") == 0)
                    {
                        result = NI_System_Math_MaxMagnitudeNumber;
                    }
                }
                else if (strcmp(methodName, "Number") == 0)
                {
                    result = NI_System_Math_MaxNumber;
                }
            }
            else if (strncmp(methodName, "Min", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Min;
                }
                else if (strncmp(methodName, "Magnitude", 9) == 0)
                {
                    methodName += 9;

                    if (methodName[0] == '\0')
                    {
                        result = NI_System_Math_MinMagnitude;
                    }
                    else if (strcmp(methodName, "Number") == 0)
                    {
                        result = NI_System_Math_MinMagnitudeNumber;
                    }
                }
                else if (strcmp(methodName, "Number") == 0)
                {
                    result = NI_System_Math_MinNumber;
                }
            }
            else if (strcmp(methodName, "MultiplyAddEstimate") == 0)
            {
                result = NI_System_Math_MultiplyAddEstimate;
            }
            break;
        }

        case 'P':
        {
            if (strcmp(methodName, "Pow") == 0)
            {
                result = NI_System_Math_Pow;
            }
            break;
        }

        case 'R':
        {
            if (strncmp(methodName, "Reciprocal", 10) == 0)
            {
                methodName += 10;

                if (strcmp(methodName, "Estimate") == 0)
                {
                    result = NI_System_Math_ReciprocalEstimate;
                }
                else if (strcmp(methodName, "SqrtEstimate") == 0)
                {
                    result = NI_System_Math_ReciprocalSqrtEstimate;
                }
            }
            else if (strcmp(methodName, "Round") == 0)
            {
                result = NI_System_Math_Round;
            }
            break;
        }

        case 'S':
        {
            if (strncmp(methodName, "Sin", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Sin;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Sinh;
                    }
                }
            }
            else if (strcmp(methodName, "Sqrt") == 0)
            {
                result = NI_System_Math_Sqrt;
            }
            break;
        }

        case 'T':
        {
            if (strncmp(methodName, "Tan", 3) == 0)
            {
                methodName += 3;

                if (methodName[0] == '\0')
                {
                    result = NI_System_Math_Tan;
                }
                else if (methodName[1] == '\0')
                {
                    if (methodName[0] == 'h')
                    {
                        result = NI_System_Math_Tanh;
                    }
                }
            }
            else if (strcmp(methodName, "Truncate") == 0)
            {
                result = NI_System_Math_Truncate;
            }
            break;
        }

        default:
        {
            break;
        }
    }

    return result;
}

//------------------------------------------------------------------------
// lookupPrimitiveIntNamedIntrinsic: map method to jit named intrinsic value
//
// Arguments:
//    method -- method handle for method
//
// Return Value:
//    Id for the named intrinsic, or Illegal if none.
//
// Notes:
//    method should have CORINFO_FLG_INTRINSIC set in its attributes,
//    otherwise it is not a named jit intrinsic.
//
NamedIntrinsic NamedIntrinsicLookup::lookupPrimitiveIntNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName)
{
    NamedIntrinsic result = NI_Illegal;

    if (strcmp(methodName, "Crc32C") == 0)
    {
        result = NI_PRIMITIVE_Crc32C;
    }
    else if (strcmp(methodName, "LeadingZeroCount") == 0)
    {
        result = NI_PRIMITIVE_LeadingZeroCount;
    }
    else if (strcmp(methodName, "Log2") == 0)
    {
        result = NI_PRIMITIVE_Log2;
    }
    else if (strcmp(methodName, "PopCount") == 0)
    {
        result = NI_PRIMITIVE_PopCount;
    }
    else if (strcmp(methodName, "RotateLeft") == 0)
    {
        result = NI_PRIMITIVE_RotateLeft;
    }
    else if (strcmp(methodName, "RotateRight") == 0)
    {
        result = NI_PRIMITIVE_RotateRight;
    }
    else if (strcmp(methodName, "TrailingZeroCount") == 0)
    {
        result = NI_PRIMITIVE_TrailingZeroCount;
    }

    return result;
}
