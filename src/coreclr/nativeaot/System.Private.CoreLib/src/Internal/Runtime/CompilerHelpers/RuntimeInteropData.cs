// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Runtime;

using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;

namespace Internal.Runtime.CompilerHelpers
{
    internal static class RuntimeInteropData
    {
        public static uint GetStructFieldOffset(RuntimeTypeHandle structureTypeHandle, string fieldName)
        {
            if (TryGetStructData(structureTypeHandle, out _, out NativeParser entryParser))
            {
                uint mask = entryParser.GetUnsigned();
                if ((mask & InteropDataConstants.HasInvalidLayout) != 0)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, Type.GetTypeFromHandle(structureTypeHandle)), nameof(structureTypeHandle));
                }

                if ((mask & InteropDataConstants.HasMarshallers) != 0)
                {
                    // skip the first 4 IntPtrs(3 stubs and size)
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                    entryParser.SkipInteger();
                }

                uint fieldCount = mask >> InteropDataConstants.FieldCountShift;
                for (uint index = 0; index < fieldCount; index++)
                {
                    string name = entryParser.GetString();
                    uint offset = entryParser.GetUnsigned();
                    if (name == fieldName)
                    {
                        return offset;
                    }
                }

                throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, Type.GetTypeFromHandle(structureTypeHandle)), nameof(fieldName));
            }

            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structureTypeHandle)!));
        }

        public static unsafe int GetStructUnsafeStructSize(RuntimeTypeHandle structureTypeHandle)
        {
            MethodTable* structureMT = structureTypeHandle.ToMethodTable();
            if (TryGetStructData(structureTypeHandle, out _, out NativeParser entryParser))
            {
                uint mask = entryParser.GetUnsigned();
                if ((mask & InteropDataConstants.HasInvalidLayout) != 0)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, Type.GetTypeFromHandle(structureTypeHandle)), nameof(structureTypeHandle));
                }

                if ((mask & InteropDataConstants.HasMarshallers) != 0)
                {
                    return (int)entryParser.GetUnsigned();
                }

                // We expect a blittable value type at this point
                Debug.Assert(!structureMT->ContainsGCPointers);
                Debug.Assert(structureMT->IsValueType);
                return (int)structureMT->ValueTypeSize;
            }

            // No interop data.
            // If the type is an interface or a generic type, the reason is likely that.
            Type structureType = Type.GetTypeFromHandle(structureTypeHandle)!;
            if (structureMT->IsInterface || structureMT->IsGeneric)
            {
                throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, structureType));
            }

            // Otherwise assume we miss interop data for the type.
            throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, structureType));
        }

        public static IntPtr GetStructUnmarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            GetMarshallersForStruct(structureTypeHandle, out _, out IntPtr unmarshal, out _);
            return unmarshal;
        }

        public static IntPtr GetStructMarshalStub(RuntimeTypeHandle structureTypeHandle)
        {
            GetMarshallersForStruct(structureTypeHandle, out IntPtr marshal, out _, out _);
            return marshal;
        }

        public static IntPtr GetDestroyStructureStub(RuntimeTypeHandle structureTypeHandle)
        {
            GetMarshallersForStruct(structureTypeHandle, out _, out _, out IntPtr destroyStub);
            return destroyStub;
        }

        public static IntPtr GetForwardDelegateCreationStub(RuntimeTypeHandle delegateTypeHandle)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out _, out _, out IntPtr delegateCreationStub);
            if (delegateCreationStub == IntPtr.Zero)
                throw new NotSupportedException(SR.Format(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle)));
            return delegateCreationStub;
        }

        public static IntPtr GetDelegateMarshallingStub(RuntimeTypeHandle delegateTypeHandle, bool openStaticDelegate)
        {
            GetMarshallersForDelegate(delegateTypeHandle, out IntPtr openStub, out IntPtr closedStub, out _);
            IntPtr pStub = openStaticDelegate ? openStub : closedStub;
            if (pStub == IntPtr.Zero)
                throw new NotSupportedException(SR.Format(SR.DelegateMarshalling_MissingInteropData, Type.GetTypeFromHandle(delegateTypeHandle)));
            return pStub;
        }

        private static unsafe bool TryGetNativeReaderForBlob(TypeManagerHandle module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (RuntimeImports.RhFindBlob(module, (uint)blob, &pBlob, &cbBlob))
            {
                reader = new NativeReader(pBlob, cbBlob);
                return true;
            }

            reader = default(NativeReader);
            return false;
        }

        private static unsafe bool GetMarshallersForDelegate(RuntimeTypeHandle delegateTypeHandle, out IntPtr openStub, out IntPtr closedStub, out IntPtr delegateCreationStub)
        {
            int delegateHashcode = delegateTypeHandle.GetHashCode();
            openStub = IntPtr.Zero;
            closedStub = IntPtr.Zero;
            delegateCreationStub = IntPtr.Zero;

            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                NativeReader delegateMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.DelegateMarshallingStubMap, out delegateMapReader))
                {
                    NativeParser delegateMapParser = new NativeParser(delegateMapReader, 0);
                    NativeHashtable delegateHashtable = new NativeHashtable(delegateMapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = delegateHashtable.Lookup(delegateHashcode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundDelegateType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        if (foundDelegateType.Equals(delegateTypeHandle))
                        {
                            openStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            closedStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            delegateCreationStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static unsafe bool TryGetStructData(RuntimeTypeHandle structTypeHandle, out ExternalReferencesTable externalReferences, out NativeParser entryParser)
        {
            int structHashcode = structTypeHandle.GetHashCode();
            externalReferences = default(ExternalReferencesTable);
            entryParser = default(NativeParser);
            foreach (TypeManagerHandle module in RuntimeAugments.GetLoadedModules())
            {
                NativeReader structMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.StructMarshallingStubMap, out structMapReader))
                {
                    NativeParser structMapParser = new NativeParser(structMapReader, 0);
                    NativeHashtable structHashtable = new NativeHashtable(structMapParser);

                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = structHashtable.Lookup(structHashcode);
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundStructType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        if (foundStructType.Equals(structTypeHandle))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static unsafe void GetMarshallersForStruct(RuntimeTypeHandle structTypeHandle, out IntPtr marshalStub, out IntPtr unmarshalStub, out IntPtr destroyStub)
        {
            marshalStub = IntPtr.Zero;
            unmarshalStub = IntPtr.Zero;
            destroyStub = IntPtr.Zero;

            if (TryGetStructData(structTypeHandle, out ExternalReferencesTable externalReferences, out NativeParser entryParser))
            {
                uint mask = entryParser.GetUnsigned();
                if ((mask & InteropDataConstants.HasInvalidLayout) != 0)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_CannotMarshal, Type.GetTypeFromHandle(structTypeHandle)));
                }

                if ((mask & InteropDataConstants.HasMarshallers) != 0)
                {
                    entryParser.GetUnsigned(); // skip size
                    marshalStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                    unmarshalStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                    destroyStub = externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());
                }
            }
            else
            {
                throw new NotSupportedException(SR.Format(SR.StructMarshalling_MissingInteropData, Type.GetTypeFromHandle(structTypeHandle)));
            }
        }
    }
}
