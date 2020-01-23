// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal unsafe partial class CorInfoImpl
    {
        private struct IntrinsicKey
        {
            public string MethodName;
            public string TypeNamespace;
            public string TypeName;

            public bool Equals(IntrinsicKey other)
            {
                return (MethodName == other.MethodName) &&
                    (TypeNamespace == other.TypeNamespace) &&
                    (TypeName == other.TypeName);
            }

            public override int GetHashCode()
            {
                return MethodName.GetHashCode() +
                    ((TypeNamespace != null) ? TypeNamespace.GetHashCode() : 0) +
                    ((TypeName != null) ? TypeName.GetHashCode() : 0);
            }
        }

        private class IntrinsicEntry
        {
            public IntrinsicKey Key;
            public CorInfoIntrinsics Id;
        }

        private class IntrinsicHashtable : LockFreeReaderHashtable<IntrinsicKey, IntrinsicEntry>
        {
            protected override bool CompareKeyToValue(IntrinsicKey key, IntrinsicEntry value)
            {
                return key.Equals(value.Key);
            }
            protected override bool CompareValueToValue(IntrinsicEntry value1, IntrinsicEntry value2)
            {
                return value1.Key.Equals(value2.Key);
            }
            protected override IntrinsicEntry CreateValueFromKey(IntrinsicKey key)
            {
                Debug.Fail("CreateValueFromKey not supported");
                return null;
            }
            protected override int GetKeyHashCode(IntrinsicKey key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(IntrinsicEntry value)
            {
                return value.Key.GetHashCode();
            }

            public void Add(CorInfoIntrinsics id, string methodName, string typeNamespace, string typeName)
            {
                var entry = new IntrinsicEntry();
                entry.Id = id;
                entry.Key.MethodName = methodName;
                entry.Key.TypeNamespace = typeNamespace;
                entry.Key.TypeName = typeName;
                AddOrGetExisting(entry);
            }
        }

        static IntrinsicHashtable InitializeIntrinsicHashtable()
        {
            IntrinsicHashtable table = new IntrinsicHashtable();

            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sin, "Sin", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sin, "Sin", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cos, "Cos", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cos, "Cos", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cbrt, "Cbrt", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cbrt, "Cbrt", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sqrt, "Sqrt", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sqrt, "Sqrt", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Abs, "Abs", "System", "Math");
            // No System.MathF entry for CORINFO_INTRTINSIC_Abs as System.Math exposes and handles both float and double
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Round, "Round", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Round, "Round", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cosh, "Cosh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Cosh, "Cosh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sinh, "Sinh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Sinh, "Sinh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Tan, "Tan", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Tan, "Tan", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Tanh, "Tanh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Tanh, "Tanh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Asin, "Asin", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Asin, "Asin", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Asinh, "Asinh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Asinh, "Asinh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Acos, "Acos", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Acos, "Acos", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Acosh, "Acosh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Acosh, "Acosh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atan, "Atan", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atan, "Atan", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atan2, "Atan2", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atan2, "Atan2", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atanh, "Atanh", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Atanh, "Atanh", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Log10, "Log10", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Log10, "Log10", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Pow, "Pow", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Pow, "Pow", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Exp, "Exp", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Exp, "Exp", "System", "MathF");
#if !READYTORUN
            // These are normally handled via the SSE4.1 instructions ROUNDSS/ROUNDSD.
            // However, we don't know the ISAs the target machine supports so we should
            // fallback to the method call implementation instead.
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Ceiling, "Ceiling", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Ceiling, "Ceiling", "System", "MathF");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Floor, "Floor", "System", "Math");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Floor, "Floor", "System", "MathF");
#endif
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetChar, null, null, null); // unused
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_GetDimLength, "GetLength", "System", "Array"); // not handled
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Get, "Get", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Address, "Address", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Set, "Set", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StringGetChar, "get_Chars", "System", "String");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StringLength, "get_Length", "System", "String");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InitializeArray, "InitializeArray", "System.Runtime.CompilerServices", "RuntimeHelpers");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetTypeFromHandle, "GetTypeFromHandle", "System", "Type");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_RTH_GetValueInternal, "GetValueInternal", "System", "RuntimeTypeHandle");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_TypeEQ, "op_Equality", "System", "Type");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_TypeNEQ, "op_Inequality", "System", "Type");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Object_GetType, "GetType", "System", "Object");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_GetStubContext, "GetStubContext", "System.StubHelpers", "StubHelpers"); // interop-specific
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr, "GetStubContextAddr", "System.StubHelpers", "StubHelpers"); // interop-specific
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_GetNDirectTarget, "GetNDirectTarget", "System.StubHelpers", "StubHelpers"); // interop-specific
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedAdd32, "Add", System.Threading", "Interlocked"); // unused
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedAdd64, "Add", System.Threading", "Interlocked"); // unused
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd32, "ExchangeAdd", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd64, "ExchangeAdd", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg32, "Exchange", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg64, "Exchange", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg32, "CompareExchange", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg64, "CompareExchange", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_MemoryBarrier, "MemoryBarrier", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetCurrentManagedThread, "GetCurrentThreadNative", "System", "Thread"); // not in .NET Core
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetManagedThreadId, "get_ManagedThreadId", "System", "Thread"); // not in .NET Core
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Ctor, ".ctor", "System", "ByReference`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Value, "get_Value", "System", "ByReference`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Span_GetItem, "get_Item", "System", "Span`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_ReadOnlySpan_GetItem, "get_Item", "System", "ReadOnlySpan`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "EETypePtrOf", "System", "EETypePtr");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "DefaultConstructorOf", "System", "Activator");

            // If this assert fails, make sure to add the new intrinsics to the table above and update the expected count below.
            Debug.Assert((int)CorInfoIntrinsics.CORINFO_INTRINSIC_Count == 54);

            return table;
        }

        static IntrinsicHashtable s_IntrinsicHashtable = InitializeIntrinsicHashtable();

        private CorInfoIntrinsics getIntrinsicID(CORINFO_METHOD_STRUCT_* ftn, byte* pMustExpand)
        {
            var method = HandleToObject(ftn);
            return getIntrinsicID(method, pMustExpand);
        }

        private CorInfoIntrinsics getIntrinsicID(MethodDesc method, byte* pMustExpand)
        {
            if (pMustExpand != null)
                *pMustExpand = 0;

            Debug.Assert(method.IsIntrinsic);

            IntrinsicKey key = new IntrinsicKey();
            key.MethodName = method.Name;

            var metadataType = method.OwningType as MetadataType;
            if (metadataType != null)
            {
                key.TypeNamespace = metadataType.Namespace;
                key.TypeName = metadataType.Name;
            }

            IntrinsicEntry entry;
            if (!s_IntrinsicHashtable.TryGetValue(key, out entry))
                return CorInfoIntrinsics.CORINFO_INTRINSIC_Illegal;

            // Some intrinsics need further disambiguation
            CorInfoIntrinsics id = entry.Id;
            switch (id)
            {
                case CorInfoIntrinsics.CORINFO_INTRINSIC_Abs:
                    {
                        // RyuJIT handles floating point overloads only
                        var returnTypeCategory = method.Signature.ReturnType.Category;
                        if (returnTypeCategory != TypeFlags.Double && returnTypeCategory != TypeFlags.Single)
                            return CorInfoIntrinsics.CORINFO_INTRINSIC_Illegal;
                    }
                    break;
                case CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Get:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Address:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Set:
                    if (!method.OwningType.IsArray)
                        return CorInfoIntrinsics.CORINFO_INTRINSIC_Illegal;
                    break;

                case CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd32:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg32:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg32:
                    {
                        // RyuJIT handles int32 and int64 overloads only
                        var returnTypeCategory = method.Signature.ReturnType.Category;
                        if (returnTypeCategory != TypeFlags.Int32 && returnTypeCategory != TypeFlags.Int64 && returnTypeCategory != TypeFlags.IntPtr)
                            return CorInfoIntrinsics.CORINFO_INTRINSIC_Illegal;

                        // int64 overloads have different ids
                        if (returnTypeCategory == TypeFlags.Int64)
                        {
                            Debug.Assert((int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd32 + 1 == (int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd64);
                            Debug.Assert((int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg32 + 1 == (int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg64);
                            Debug.Assert((int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg32 + 1 == (int)CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg64);
                            id = (CorInfoIntrinsics)((int)id + 1);
                        }
                    }
                    break;

                case CorInfoIntrinsics.CORINFO_INTRINSIC_RTH_GetValueInternal:
#if !READYTORUN
                case CorInfoIntrinsics.CORINFO_INTRINSIC_InitializeArray:
#endif
                case CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Ctor:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Value:
                    if (pMustExpand != null)
                        *pMustExpand = 1;
                    break;

                case CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle:
                    if (pMustExpand != null)
                        *pMustExpand = 1;
                    break;

                case CorInfoIntrinsics.CORINFO_INTRINSIC_Span_GetItem:
                case CorInfoIntrinsics.CORINFO_INTRINSIC_ReadOnlySpan_GetItem:
                    {
                        // RyuJIT handles integer overload only
                        var argumentTypeCategory = method.Signature[0].Category;
                        if (argumentTypeCategory != TypeFlags.Int32)
                            return CorInfoIntrinsics.CORINFO_INTRINSIC_Illegal;
                    }
                    break;

                default:
                    break;
            }

            return id;
        }
    }
}
