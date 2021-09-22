// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Get, "Get", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Address, "Address", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Array_Set, "Set", null, null);
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InitializeArray, "InitializeArray", "System.Runtime.CompilerServices", "RuntimeHelpers");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_RTH_GetValueInternal, "GetValueInternal", "System", "RuntimeTypeHandle");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_Object_GetType, "GetType", "System", "Object");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_GetStubContext, "GetStubContext", "System.StubHelpers", "StubHelpers"); // interop-specific
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr, "GetStubContextAddr", "System.StubHelpers", "StubHelpers"); // interop-specific
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_StubHelpers_NextCallReturnAddress, "NextCallReturnAddress", "System.StubHelpers", "StubHelpers");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedAdd32, "Add", System.Threading", "Interlocked"); // unused
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedAdd64, "Add", System.Threading", "Interlocked"); // unused
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd32, "ExchangeAdd", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXAdd64, "ExchangeAdd", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg32, "Exchange", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedXchg64, "Exchange", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg32, "CompareExchange", "System.Threading", "Interlocked");
            // table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_InterlockedCmpXchg64, "CompareExchange", "System.Threading", "Interlocked"); // ambiguous match
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_MemoryBarrier, "MemoryBarrier", "System.Threading", "Interlocked");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_MemoryBarrierLoad, "LoadBarrier", "System.Threading", "Interlocked");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Ctor, ".ctor", "System", "ByReference`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_ByReference_Value, "get_Value", "System", "ByReference`1");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "EETypePtrOf", "System", "EETypePtr");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "MethodTableOf", "System", "Object");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "DefaultConstructorOf", "System", "Activator");
            table.Add(CorInfoIntrinsics.CORINFO_INTRINSIC_GetRawHandle, "AllocatorOf", "System", "Activator");

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

                default:
                    break;
            }

            return id;
        }
    }
}
